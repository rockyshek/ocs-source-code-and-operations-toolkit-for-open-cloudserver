// Copyright (c) Microsoft Corporation
// All rights reserved. 
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

namespace Microsoft.GFS.WCS.ChassisManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Threading;

    internal class EmersonPsu : PsuBase
    {
        // Command payload to clear fault indicators
        private byte[] clearBatteryFaultPayload = new byte[2] { 0x00, 0x5A };

        // Command payload to SetBatteryExtendedOperationMode
        private const byte enableBatteryExtendedOperation = 0xAA;
        private const byte disableBatteryExtendedOperation = 0xBB;

        // Passwords to enter and exit in-system firmware upgrade mode
        private byte[] priFwUpgradeModePassword = new byte[2] { 0x34, 0x12 };
        private byte[] secFwUpgradeModePassword = new byte[2] { 0xCD, 0xAB };

        // Firmware image locations containing the model ID
        private string priModelIdOffset = "10180000";
        private string secModelIdOffset = "10300000";
        private const int fwImageDataLineLength = 42;
        private const int fwImageRomPageLineLength = 14;

        // Firmware image start/end locations
        private const string priFwWriteStartOffset = "10180000";
        private const string secFwWriteStartOffset = "10300000";
        private const string priFwReadStartOffset = "10181000";
        private const string secFwReadStartOffset = "10301000";
        private const string priFwEndOffset = "10A7F000";
        private const string secFwEndOffset = "1047F000";
        private const string pageEndOffset = "10FFF000";  // End of ROM page
        private const byte priFwEndPage = 0;
        private const byte secFwEndPage = 1;

        // Waiting time for PSU processes 
        private const int psuEnterProgrammingModeTimeMs = 3000;  // For PSU to turn off output and enter programming mode (T1 in spec)
        private const int psuProgramEraseTimeMs = 2000;          // For PSU to erase and verify erased program memory (T4 in spec)
        // For PSU to handle received I2C packet (T2 in spec)
        private const int psuPriI2CPacketHandleTimeMs = 40;      
        private const int psuSecI2CPacketHandleTimeMs = 1;
        // For PSU to write block of 16x16 bytes (T3 in spec)
        private const int psuPriBlockProgramWriteTimeMs = 50;    
        private const int psuSecBlockProgramWriteTimeMs = 1;
        // For PSU to read lower 8 byte of memory (T5 in spec)
        private const int psuPriByteReadTimeMs = 1;
        private const int psuSecByteReadTimeMs = 1;

        // PSU battery present flag
        private bool batteryPresent = false;

        // Firmware update progress and status
        private FwUpdateStatusEnum fwUpdateStatus = FwUpdateStatusEnum.NotStarted;
        private FwUpdateStageEnum fwUpdateStage = FwUpdateStageEnum.NotStarted;

        /// <summary>
        /// Data for PSU Firmware update
        /// </summary>
        internal struct FirmwareUpdateInfo
        {
            // File path containing firmware image
            internal string fwFilepath;
            // True:  Firmware image is for primary controller. 
            // False: Firmware image is for secondary controller. 
            internal bool primaryImage;  
        }

        /// <summary>
        /// Enumerates the manufacturer-specific PmBus commands supported by the Emerson PSU
        /// </summary>
        internal enum PmBusCommand
        {
            EXTENDED_BATTERY      = 0xC9,
            FAULT_INDICATOR       = 0xC6,
            BATT_POUT             = 0xD5,
            BATT_HEALTH_STATUS    = 0xD6,
            BATT_STATE_OF_CHARGE  = 0xD7,
            BATT_OP_TIME_100_LOAD = 0xE2,
            FW_REVISION           = 0xEF,
            ENTER_ISP             = 0xF0,
            SEND_MEMORY_PAGE      = 0xF8,
            SEND_MODEL_ID         = 0xF9,
            WRITE_MEMORY          = 0xFB,
            FW_UPDATE_STATUS      = 0xFC,
            READ_MEMORY           = 0xFD,
            EXIT_ISP              = 0xFE,
        }

        /// <summary>
        /// Enumerates the response lengths for the manufacturer-specific PmBus commands supported by the Emerson PSU
        /// Need to add one byte for Block Read commands as the first byte is a length indicator
        /// </summary>
        internal enum PmBusResponseLength
        {
            EXTENDED_BATTERY      = 1,
            FAULT_INDICATOR       = 2,
            BATT_POUT             = 2,
            BATT_HEALTH_STATUS    = 2,
            BATT_STATE_OF_CHARGE  = 2,
            BATT_OP_TIME_100_LOAD = 2,
            FW_REVISION           = 9,
            FW_UPDATE_STATUS      = 2,
            READ_MEMORY           = 9,
        }

        /// <summary>
        /// PSU Firmware update status
        /// </summary>
        internal enum PsuFwUpdateStatus
        {
            NoError                 = 0x00, 
            ReceivedChecksumError   = 0x01,
            AddressError            = 0x02,
            BlockNumberError        = 0x03,
            PacketLengthError       = 0x04,
            I2CWriteError           = 0x05,
            I2CReadError            = 0x06,
            UnsupportedCommand      = 0x0A,
            ModelIdError            = 0x0B,
            FlashEraseError         = 0x0C,
            FlashEraseOK            = 0x0D,
            FlashNotErased          = 0x0E,  // Model ID has not been received (program memory hasn’t been erased) before writing to program memory.
            InvalidRecordType       = 0x0F,
            EnteringPrimaryBootloaderMode = 0x50,
            EnteredPrimaryBootloaderMode  = 0x51,
            SendingModelIdToPrimary       = 0x52,
            WritingPrimaryProgramMemory   = 0x55,
            ReadingPrimaryProgramMemory   = 0x56,
            ExitingPrimaryBootloaderMode  = 0x57,
            ExitBootloaderModeError       = 0x12,
            Unknown = 0xFF
        }

        /// <summary>
        /// Overall Firmware update status
        /// </summary>
        internal enum FwUpdateStatusEnum
        {
            NotStarted = 0x00,
            InProgress = 0x01,
            Success = 0x02,
            Failed = 0xFF
        }

        /// <summary>
        /// Detailed internal firmware update stage
        /// </summary>
        internal enum FwUpdateStageEnum
        {
            NotStarted = 0x00,
            ReadFirmwareImageFile = 0x01,
            ExtractModelId = 0x02,
            EnterFirmwareUpgradeMode = 0x03,
            SendModelId = 0x04,
            WriteFirmwareImage = 0x05,
            VerifyFirmwareImage = 0x06,
            ExitFirmwareUpgradeMode = 0x07,
            Completed = 0x08,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmersonPsu"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="batteryPresent">if set to <c>true</c> [battery present].</param>
        internal EmersonPsu(byte deviceId, bool batteryPresent)
            : base(deviceId)
        {
            this.batteryPresent = batteryPresent;
        }

        /// <summary>
        /// Calculates the two's complement checksum for the payload.
        /// The payload used is: (slave address | rwbit) + command code + payload
        /// </summary>
        /// <param name="psuId">The psu identifier.</param>
        /// <param name="commandCode">The command code.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="readCommand">if set to <c>true</c> [read command].</param>
        /// <returns>Checksum</returns>
        private static byte CalculatePayloadChecksum(byte psuId, byte commandCode, byte[] payload, bool readCommand)
        {
            // Create temp payload with slave address and command code
            byte[] tmpPayload = new byte[payload.Length + 2];
            
            // Insert slave address with the read/write bit
            int rwBit = readCommand ? 1 : 0;
            byte slaveAddress;
            SC18IM700.GetSlaveDeviceAddress((byte)DeviceType.Psu, psuId, commandCode, out slaveAddress);
            tmpPayload[0] = (byte)(slaveAddress | rwBit);

            tmpPayload[1] = commandCode;
            Array.Copy(payload, 0, tmpPayload, 2, payload.Length);

            byte checksum = Ipmi.IpmiSharedFunc.TwoComplementChecksum(0, tmpPayload.Length, tmpPayload);
            return checksum;
        }

        # region PSU Control
        /// <summary>
        /// Lock used to affinitize on/off requests, and prevent repeated on/off
        /// </summary>
        private object psuOnOffLocker = new object();

        /// <summary>
        /// Time in seconds where additional power off requests are not permitted.
        /// </summary>
        private int backoff = 30;

        /// <summary>
        /// The last time the Psu was powered off
        /// </summary>
        private DateTime lastPowerOff;


        /// <summary>
        /// Set PSU On/OFF
        /// </summary>
        /// <param name="off">true = OFF, false = ON</param>
        /// <returns>
        /// Completion code success/failure
        /// </returns>
        internal override CompletionCode SetPsuOnOff(bool off)
        {
            if (off)
            {
                if (PowerOffPermitted())
                    return this.SetPsuOnOff(PmBusCommandPayload.POWER_SOFT_OFF);
                else
                    return CompletionCode.CmdFailedNotSupportedInPresentState;
            }
            else
            {
                return this.SetPsuOnOff(PmBusCommandPayload.POWER_ON);
            }
        }

        /// <summary>
        /// Function to determine if a the PSU can be turned off.  The purpose of
        /// this function is to prevent multiple reboots of a PSU in quick succession
        /// </summary>
        private bool PowerOffPermitted()
        {
            bool permitted = false;

            lock (psuOnOffLocker)
            {
                if (DateTime.Now > lastPowerOff.AddSeconds(backoff))
                {
                    lastPowerOff = DateTime.Now;
                    permitted = true;
                }
                else
                {
                    permitted = false;
                }

                return permitted;
            }

        }

        /// <summary>
        /// Set PSU On/OFF
        /// </summary>
        /// <param name="psuId">Psu Id</param>
        /// <param name="cmd">command ON or OFF</param>
        /// <returns>Completion code success/failure</returns>
        private CompletionCode SetPsuOnOff(PmBusCommandPayload payload)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                PsuOnOffResponse response = new PsuOnOffResponse();

                response = (PsuOnOffResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuBytePayloadRequest((byte)Microsoft.GFS.WCS.ChassisManager.PmBusCommand.SET_POWER, (byte)payload,
                    (byte)Microsoft.GFS.WCS.ChassisManager.PmBusResponseLength.SET_POWER), typeof(PsuOnOffResponse));

                // check for completion code 
                if (response.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)response.CompletionCode;
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                Tracer.WriteError("SetPsuOnOff failed with the exception: " + ex);
            }

            return returnPacket;
        }
        #endregion

        #region Battery Methods

        /// <summary>
        /// Sets the battery extended operation mode (enabled? or disabled?).
        /// </summary>
        /// <returns></returns>
        internal override CompletionCode SetBatteryExtendedOperationMode(bool toEnable)
        {
            return SetBatteryExtendedOperationMode(this.PsuId, toEnable);
        }

        /// <summary>
        /// Gets the battery extended operation status (enabled? or disabled?).
        /// </summary
        /// <returns></returns>
        internal override BatteryExtendedOperationStatusPacket GetBatteryExtendedOperationStatus()
        {
            return GetBatteryExtendedOperationStatus(this.PsuId);
        }

        /// <summary>
        /// Gets the Battery Fault Indicator
        /// </summary>
        /// <returns></returns>
        internal BatteryFaultIndicatorPacket GetBatteryFaultIndicator()
        {
            return GetBatteryFaultIndicator(this.PsuId);
        }

        /// <summary>
        /// Clears the Battery Fault Indicator
        /// </summary>
        /// <returns></returns>
        internal CompletionCode ClearBatteryFaultIndicator()
        {
            return ClearBatteryFaultIndicator(this.PsuId);
        }

        /// <summary>
        /// Gets the battery power output (in Watts).
        /// </summary>
        /// <returns></returns>
        internal BatteryPowerPacket GetBatteryPowerOut()
        {
            return GetBatteryPowerOut(this.PsuId);
        }

        /// <summary>
        /// Gets the Battery Health Status
        /// </summary>
        /// <returns></returns>
        internal BatteryHealthStatusPacket GetBatteryHealthStatus()
        {
            return GetBatteryHealthStatus(this.PsuId);
        }

        /// <summary>
        /// Gets the battery charge level (0 to 100%).
        /// </summary>
        /// <returns></returns>
        internal BatteryChargeLevelPacket GetBatteryChargeLevel()
        {
            return GetBatteryChargeLevel(this.PsuId);
        }

        /// <summary>
        /// Set the Battery Extended Operation Mode. 
        /// </summary>
        private CompletionCode SetBatteryExtendedOperationMode(byte psuId, bool toEnable)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            // Command payload to set battery extended operation mode
            byte cmdPayload = ((toEnable) ? enableBatteryExtendedOperation: disableBatteryExtendedOperation);

            try
            {
                PsuChassisResponse myResponse = new PsuChassisResponse();
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuBytePayloadRequest((byte)PmBusCommand.EXTENDED_BATTERY, (byte)cmdPayload, (byte)0), typeof(PsuChassisResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("SetBatteryExtendedOperationMode({0}) failed with CompletionCode ({1})", this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("SetBatteryExtendedOperationMode Exception: " + ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Retrieve the Battery Extended Operation Status. 
        /// </summary>
        private BatteryExtendedOperationStatusPacket GetBatteryExtendedOperationStatus(byte psuId)
        {
            // Initialize return packet 
            BatteryExtendedOperationStatusPacket returnPacket = new BatteryExtendedOperationStatusPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.isEnabled = false;

            try
            {
                BatteryExtendedOperationStatusResponse myResponse = new BatteryExtendedOperationStatusResponse();
                myResponse = (BatteryExtendedOperationStatusResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.EXTENDED_BATTERY, (byte)PmBusResponseLength.EXTENDED_BATTERY), typeof(BatteryExtendedOperationStatusResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryExtendedOperationStatus Failure: Completion Code: {0}", returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    returnPacket.isEnabled = (myResponse.ExtendedOperationStatus == 0xAA) ? true : false;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.isEnabled= false;
                Tracer.WriteError("GetBatteryExtendedOperationStatus Exception: " + ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Gets the battery operation time in seconds under 100% load.
        /// </summary>
        /// <returns></returns>
        internal BatteryOpTimePacket GetBatteryOpTime100Load()
        {
            return GetBatteryOpTime100Load(this.PsuId);
        }

        /// <summary>
        /// Gets the battery status
        /// </summary>
        /// <returns></returns>
        internal override BatteryStatusPacket GetBatteryStatus()
        {
            BatteryStatusPacket infoPacket = new BatteryStatusPacket();
            infoPacket.CompletionCode = CompletionCode.Success;
            bool statusReadFailure = false;

            // Indicate that battery is present
            infoPacket.Presence = (byte)(this.batteryPresent == true? 1 : 0);

            if (this.batteryPresent)
            {
                // Get battery power output
                BatteryPowerPacket batteryPowerPacket = GetBatteryPowerOut();
                if (batteryPowerPacket.CompletionCode != CompletionCode.Success)
                {
                    Tracer.WriteWarning("GetBatteryPowerOut failed for battery: " + this.PsuId);
                    statusReadFailure = true;
                }
                else
                {
                    infoPacket.BatteryPowerOutput = batteryPowerPacket.BatteryPower;
                }

                // Get battery charge level
                BatteryChargeLevelPacket batteryChargeLevelPacket = GetBatteryChargeLevel();
                if (batteryChargeLevelPacket.CompletionCode != CompletionCode.Success)
                {
                    Tracer.WriteWarning("GetBatteryChargeLevel failed for battery: " + this.PsuId);
                    statusReadFailure = true;
                }
                else
                {
                    infoPacket.BatteryChargeLevel = batteryChargeLevelPacket.BatteryChargeLevel;
                }

                // Get battery fault indicator
                BatteryFaultIndicatorPacket batteryFaultIndicatorPacket = GetBatteryFaultIndicator();
                if (batteryFaultIndicatorPacket.CompletionCode != CompletionCode.Success)
                {
                    Tracer.WriteWarning("GetBatteryFaultIndicator failed for battery: " + this.PsuId);
                    statusReadFailure = true;
                }
                else
                {
                    infoPacket.FaultDetected = batteryFaultIndicatorPacket.BatteryFault;
                }
                // Clear fault indicator after reading it. If the fault is still present, the fault bit will still be asserted in the PSU.
                CompletionCode returnPacket = ClearBatteryFaultIndicator();
                if (returnPacket != CompletionCode.Success)
                {
                    Tracer.WriteWarning("ClearBatteryFaultIndicator failed for battery: " + this.PsuId);
                    statusReadFailure = true;
                }
            }
            else
            {
                statusReadFailure = true;
            }

            if (statusReadFailure)
            {
                infoPacket.CompletionCode = CompletionCode.UnspecifiedError;
            }

            return infoPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Battery Fault Indicator. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private BatteryFaultIndicatorPacket GetBatteryFaultIndicator(byte psuId)
        {
            // Initialize return packet 
            BatteryFaultIndicatorPacket returnPacket = new BatteryFaultIndicatorPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;

            try
            {
                BatteryFaultIndicatorResponse myResponse = new BatteryFaultIndicatorResponse();
                myResponse = (BatteryFaultIndicatorResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.FAULT_INDICATOR, (byte)PmBusResponseLength.FAULT_INDICATOR), typeof(BatteryFaultIndicatorResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryFaultIndicator: Failure on PSU {0}. Completion Code: {1}", 
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;

                    // Extract status bits from return word. Only top byte contains valid status
                    byte status = myResponse.BatteryFaultIndicator[0];
                    returnPacket.BatteryFault =         (byte)((status >> 7) & 0x1);
                    returnPacket.UnderVoltage =         (byte)((status >> 6) & 0x1);
                    returnPacket.OverCurrentCharge =    (byte)((status >> 5) & 0x1);
                    returnPacket.OverTemp =             (byte)((status >> 4) & 0x1);
                    returnPacket.CellBalanceNotGood =   (byte)((status >> 3) & 0x1);
                    returnPacket.OverVoltage =          (byte)((status >> 2) & 0x1);
                    returnPacket.OverCurrentDischarge = (byte)((status >> 1) & 0x1);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("GetBatteryFaultIndicator: Psu {0}. Exception: {1}", this.PsuId, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to clear the Battery Fault Indicator. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private CompletionCode ClearBatteryFaultIndicator(byte psuId)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            // Command payload to clear fault indicators
            byte[] cmdPayload = clearBatteryFaultPayload;

            try
            {
                BatteryFaultIndicatorResponse myResponse = new BatteryFaultIndicatorResponse();
                myResponse = (BatteryFaultIndicatorResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuWordPayloadRequest((byte)PmBusCommand.FAULT_INDICATOR, cmdPayload, (byte)0), typeof(BatteryFaultIndicatorResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("ClearBatteryFaultIndicator: Failure on Psu {0}. Completion Code: {1}", 
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("ClearBatteryFaultIndicator: Psu {0}. Exception: {1} ", this.PsuId, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Battery Power output. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private BatteryPowerPacket GetBatteryPowerOut(byte psuId)
        {
            // Initialize return packet 
            BatteryPowerPacket returnPacket = new BatteryPowerPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.BatteryPower = 0;

            byte[] powerValue = new byte[2];
            try
            {
                BatteryPowerOutResponse myResponse = new BatteryPowerOutResponse();               
                myResponse = (BatteryPowerOutResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.BATT_POUT, (byte)PmBusResponseLength.BATT_POUT), typeof(BatteryPowerOutResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryPowerOut: Failure on Psu {0}. Completion Code: {1}", 
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    powerValue = myResponse.BatteryPower;
                    byte[] convertedPowerValue = null;
                    PmBus.PmBusLinearDataFormatConverter(ref powerValue, out convertedPowerValue);
                    powerValue = convertedPowerValue;
                    returnPacket.BatteryPower = System.BitConverter.ToInt32(powerValue, 0);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.BatteryPower = 0;
                Tracer.WriteError("GetBatteryPowerOut: Psu {0}. Exception: {1}", this.PsuId, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Battery Health Status. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private BatteryHealthStatusPacket GetBatteryHealthStatus(byte psuId)
        {
            // Initialize return packet 
            BatteryHealthStatusPacket returnPacket = new BatteryHealthStatusPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;

            try
            {
                BatteryHealthStatusResponse myResponse = new BatteryHealthStatusResponse();
                myResponse = (BatteryHealthStatusResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.BATT_HEALTH_STATUS, (byte)PmBusResponseLength.BATT_HEALTH_STATUS), typeof(BatteryHealthStatusResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryHealthStatus: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;

                    // Extract status bits from return word. 
                    byte statusHigh = myResponse.BatteryHealthStatus[0];
                    byte statusLow = myResponse.BatteryHealthStatus[1];

                    returnPacket.OverChargedAlarm =        (byte)((statusHigh >> 7) & 0x1);
                    returnPacket.TerminateChargeAlarm =    (byte)((statusHigh >> 6) & 0x1);
                    returnPacket.OverTempAlarm =           (byte)((statusHigh >> 4) & 0x1);
                    returnPacket.TerminateDischargeAlarm = (byte)((statusHigh >> 3) & 0x1);
                    returnPacket.RemainingCapacityAlarm =  (byte)((statusHigh >> 1) & 0x1);
                    returnPacket.RemainingTimeAlarm =      (byte)((statusHigh >> 0) & 0x1);

                    returnPacket.Initialized =     (byte)((statusLow >> 7) & 0x1);
                    returnPacket.Discharging =     (byte)((statusLow >> 6) & 0x1);
                    returnPacket.FullyCharged =    (byte)((statusLow >> 5) & 0x1);
                    returnPacket.FullyDischarged = (byte)((statusLow >> 4) & 0x1);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("GetBatteryHealthStatus: Psu {0}. Exception: {1}", this.PsuId, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Battery Charge Level. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private BatteryChargeLevelPacket GetBatteryChargeLevel(byte psuId)
        {
            // Initialize return packet 
            BatteryChargeLevelPacket returnPacket = new BatteryChargeLevelPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.BatteryChargeLevel = 0;

            byte[] chargeLevel = new byte[2];
            try
            {
                BatteryChargeLevelResponse myResponse = new BatteryChargeLevelResponse();
                myResponse = (BatteryChargeLevelResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.BATT_STATE_OF_CHARGE, (byte)PmBusResponseLength.BATT_STATE_OF_CHARGE), typeof(BatteryChargeLevelResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryChargeLevel: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    chargeLevel = myResponse.BatteryChargeLevel;
                    byte[] convertedChargeLevel = null;
                    PmBus.PmBusLinearDataFormatConverter(ref chargeLevel, out convertedChargeLevel);
                    chargeLevel = convertedChargeLevel;
                    returnPacket.BatteryChargeLevel = System.BitConverter.ToInt32(chargeLevel, 0);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.BatteryChargeLevel = 0;
                Tracer.WriteError("GetBatteryChargeLevel: Psu {0}. Exception: {1}", this.PsuId, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Battery Operation Time Under 100% Load. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private BatteryOpTimePacket GetBatteryOpTime100Load(byte psuId)
        {
            // Initialize return packet 
            BatteryOpTimePacket returnPacket = new BatteryOpTimePacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.BatteryOpTime = 0;

            byte[] opTime = new byte[2];
            try
            {
                BatteryOpTimeResponse myResponse = new BatteryOpTimeResponse();
                myResponse = (BatteryOpTimeResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.BATT_OP_TIME_100_LOAD, (byte)PmBusResponseLength.BATT_OP_TIME_100_LOAD), typeof(BatteryOpTimeResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetBatteryOpTime100Load: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    opTime = myResponse.BatteryOpTime;
                    byte[] convertedOpTime = null;
                    PmBus.PmBusLinearDataFormatConverter(ref opTime, out convertedOpTime);
                    opTime = convertedOpTime;
                    returnPacket.BatteryOpTime = System.BitConverter.ToInt32(opTime, 0);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.BatteryOpTime = 0;
                Tracer.WriteError("GetBatteryOpTime100Load: Psu {0}. Exception: {1}", this.PsuId, ex);
            }
            return returnPacket;
        }

        #endregion

        #region Firmware Update Methods

        /// <summary>
        /// Gets the firmware revision
        /// </summary>
        /// <returns></returns>
        internal FirmwareRevisionPacket GetFirmwareRevision()
        {
            return GetFirmwareRevision(this.PsuId);
        }

        /// <summary>
        /// Executes the firmware update
        /// </summary>
        /// <param name="fwFileName">Path to the firmware image file</param>
        /// <returns></returns>
        internal CompletionCode ExecuteFirmwareUpdate(FirmwareUpdateInfo fwUpdateInfo)
        {
            // Queue delegate in thread pool to execute firmware update
            ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteFirmwareUpdate), fwUpdateInfo);

            return CompletionCode.Success;
        }

        // Firmware update progress and status
        internal FwUpdateStatusEnum FwUpdateStatus 
        {
            get { return fwUpdateStatus; }
        }
        internal FwUpdateStageEnum FwUpdateStage
        {
            get { return fwUpdateStage; }
        }

        /// <summary>
        /// Gets the firmware revision. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private FirmwareRevisionPacket GetFirmwareRevision(byte psuId)
        {
            // Initialize return packet 
            FirmwareRevisionPacket returnPacket = new FirmwareRevisionPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.FwRevision = string.Empty;

            try
            {
                GetFirmwareRevisionResponse myResponse = new GetFirmwareRevisionResponse();
                myResponse = (GetFirmwareRevisionResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.FW_REVISION, (byte)PmBusResponseLength.FW_REVISION), typeof(GetFirmwareRevisionResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                    Tracer.WriteWarning("GetFirmwareRevision: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    if (myResponse.FwRevision != null)
                    {
                        // Interpret the received data.
                        // The first byte contains the packet length. Discard the first byte.
                        byte[] interpretedDataPacket = new byte[myResponse.FwRevision.Length - 1];
                        Buffer.BlockCopy(myResponse.FwRevision, 1, interpretedDataPacket, 0, myResponse.FwRevision.Length - 1);

                        returnPacket.FwRevision = Encoding.ASCII.GetString(interpretedDataPacket);
                    }
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("GetFirmwareRevision: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Enter Bootloader mode for in-system firmware upgrade. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        /// <param name="primaryImage">
        /// True:  Firmware image is for primary controller. 
        /// False: Firmware image is for secondary controller. 
        /// </param>
        /// <returns>Completion Code.</returns>
        private CompletionCode EnterFirmwareUpgradeMode(bool primaryImage)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            // Determine password for entering programming mode
            byte[] cmdPayload = primaryImage ? priFwUpgradeModePassword : secFwUpgradeModePassword;

            try
            {
                Tracer.WriteInfo("EnterFirmwareUpgradeMode(): Sending command to PSU {0}", this.PsuId);

                PsuChassisResponse myResponse = new PsuChassisResponse();
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new EnterFirmwareUpgradeModeRequest(this.PsuId, cmdPayload, primaryImage), typeof(PsuChassisResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("EnterFirmwareUpgradeMode: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("EnterFirmwareUpgradeMode: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Exit Bootloader mode and restart firmware. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        /// <returns>Completion Code.</returns>
        private CompletionCode ExitFirmwareUpgradeMode()
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            // Password for exiting programming mode (same for primary and secondary controllers)
            byte[] cmdPayload = secFwUpgradeModePassword;

            try
            {
                Tracer.WriteInfo("ExitFirmwareUpgradeMode(): Sending command to PSU {0}", this.PsuId);

                PsuChassisResponse myResponse = new PsuChassisResponse();
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new ExitFirmwareUpgradeModeRequest(this.PsuId, cmdPayload), typeof(PsuChassisResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("ExitFirmwareUpgradeMode: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("ExitFirmwareUpgradeMode: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Update ROM Page Address for reading program memory. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        /// <param name="romPage">The ROM page.</param>
        /// <returns>Completion Code.</returns>
        private CompletionCode SendRomPage(byte romPage)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                PsuChassisResponse myResponse = new PsuChassisResponse();
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuSendRomPageRequest(this.PsuId, romPage), typeof(PsuChassisResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("SendRomPage: Failure on Psu {0}. Completion Code: {1}", 
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("SendRomPage : Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Send Model_ID to PSU for verification
        /// </summary>
        /// <param name="modelId">The 8-byte PSU model Id extracted from the FW image file.
        /// This ensures target HEX file matches the PSU primary/secondary controller.
        /// </param>
        /// <returns>Completion Code.</returns>
        private CompletionCode SendModelId(byte[] modelId)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                Tracer.WriteInfo("SendModelId(): Sending command to PSU {0}", this.PsuId);

                PsuChassisResponse myResponse = new PsuChassisResponse();                             
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuSendModelIdRequest(this.PsuId, modelId), typeof(PsuChassisResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("SendModelId: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("SendModelId: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Write data to program memory. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        /// <param name="data">The data.
        /// For firmware image data (isDataRomPage == false), the payload format is as follows (offset, length):
        /// - Address offset: (0, 2) - offset to write the data
        /// - Data: (2, 8)        
        /// 
        /// For ROM page data (isDataRomPage == true), the payload format is as follows (offset, length):
        /// - Address bytes: (0, 2)
        /// - Page bytes: (2, 2)        
        /// 
        /// </param>
        /// <param name="isDataRomPage">
        /// True: data[] contains the ROM page data
        /// False: data[] contains FW image data
        /// </param>
        /// <returns>Completion Code.</returns>
        private CompletionCode WriteProgramMemory(byte[] data, bool isDataRomPage)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                PsuChassisResponse myResponse = new PsuChassisResponse();

                if (isDataRomPage)
                { 
                    // Write ROM Page
                    myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, 
                        new PsuWriteProgramMemoryRomPageRequest(this.PsuId, data), typeof(PsuChassisResponse));
                }
                else
                {
                    // Write FW image data
                    myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, 
                        new PsuWriteProgramMemoryFwRequest(this.PsuId, data), typeof(PsuChassisResponse));
                }

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("WriteProgramMemory: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket);
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError("WriteProgramMemory: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }


        /// <summary>
        /// Read program memory. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        /// <param name="address">The address to read</param>
        /// <param name="packetWaitTimeMs">Wait time in ms between each I2C transaction</param>
        /// <returns></returns>
        private ReadProgramMemoryPacket ReadProgramMemory(short address, int packetWaitTimeMs)
        {
            // Initialize return packet 
            ReadProgramMemoryPacket returnPacket = new ReadProgramMemoryPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;

            // Extract address to read
            byte[] cmdPayload = new byte[2] { (byte)((address & 0xFF00) >> 8), (byte)(address & 0xFF) };

            try
            {
                PsuChassisResponse psuChassisResponse = new PsuChassisResponse();
                ReadProgramMemoryResponse response = new ReadProgramMemoryResponse();

                // First send address to read
                psuChassisResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuReadProgramMemoryTargetAddressRequest(this.PsuId, cmdPayload), typeof(PsuChassisResponse));

                // If address was written successfully, read memory data
                if (psuChassisResponse.CompletionCode == 0)
                {
                    // Wait for PSU I2C Packet Handling Time
                    Thread.Sleep(packetWaitTimeMs);
                    response = (ReadProgramMemoryResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                        new PsuRequest((byte)PmBusCommand.READ_MEMORY, (byte)PmBusResponseLength.READ_MEMORY), typeof(ReadProgramMemoryResponse));
                }

                if (response.CompletionCode == 0)
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    returnPacket.Data = new byte[response.Data.Length];
                    Buffer.BlockCopy(response.Data, 0, returnPacket.Data, 0, response.Data.Length);
                }
                else
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("ReadProgramMemory: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("ReadProgramMemory: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Gets the firmware update status. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private FirmwareUpdateStatusPacket GetFirmwareUpdateStatus()
        {
            // Initialize return packet 
            FirmwareUpdateStatusPacket returnPacket = new FirmwareUpdateStatusPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.UpdateStatus = PsuFwUpdateStatus.Unknown;

            try
            {
                GetFirmwareUpdateStatusResponse myResponse = new GetFirmwareUpdateStatusResponse();
                myResponse = (GetFirmwareUpdateStatusResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.FW_UPDATE_STATUS, (byte)PmBusResponseLength.FW_UPDATE_STATUS), typeof(GetFirmwareUpdateStatusResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetFirmwareUpdateStatus: Failure on Psu {0}. Completion Code: {1}",
                        this.PsuId, returnPacket.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;

                    if (Enum.IsDefined(typeof(PsuFwUpdateStatus), (int)myResponse.UpdateStatus))
                    {
                        returnPacket.UpdateStatus = (PsuFwUpdateStatus)myResponse.UpdateStatus; 
                    }                   
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("GetFirmwareUpdateStatus: Psu {0}. Exception: {1}", this.PsuId, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Delegate that executes the firmware update.
        /// </summary>
        /// <param name="stateInfo">
        /// FirmwareUpdateInfo object passed as generic object to WaitCallback method.
        /// </param>
        private void ExecuteFirmwareUpdate(Object stateInfo)
        {
            FirmwareUpdateInfo fwUpdateInfo = (FirmwareUpdateInfo)stateInfo;
            bool enteredFwUpgradeMode = false;
            CompletionCode result = CompletionCode.Success;
            int psuI2CPacketHandleTimeMs = fwUpdateInfo.primaryImage ? psuPriI2CPacketHandleTimeMs : psuSecI2CPacketHandleTimeMs;

            // Update flag to signal that PSU FW update is in progress so that:
            // 1. PSU is not falsely signaled as faulty by PSU monitoring code
            // 2. Other code do not try and access PSU as this can cause FW update failures
            ChassisState.PsuFwUpdateInProgress[this.PsuId - 1] = true;

            lock (ChassisState.psuLock[this.PsuId - 1])
            {
                try
                {
                    fwUpdateStatus = FwUpdateStatusEnum.InProgress;

                    // Read file into memory. The image file is only a few hundred kilobytes so it won't consume much memory.
                    fwUpdateStage = FwUpdateStageEnum.ReadFirmwareImageFile;
                    string[] fwImage = File.ReadAllLines(fwUpdateInfo.fwFilepath);
                    // Remove leading ':'
                    for (int i = 0; i < fwImage.Length; i++)
                    {
                        fwImage[i] = fwImage[i].Remove(0, 1);
                    }

                    // Extract model ID from the firmware image;
                    fwUpdateStage = FwUpdateStageEnum.ExtractModelId;
                    byte[] modelId = ExtractModelId(fwImage, fwUpdateInfo.primaryImage);
                    if (modelId == null)
                    {
                        result = CompletionCode.UnspecifiedError;
                    }

                    // Enter Programming Mode
                    // PSU will verify password, turn off power output and switch to bootloader mode
                    if (result == CompletionCode.Success)
                    {
                        fwUpdateStage = FwUpdateStageEnum.EnterFirmwareUpgradeMode;
                        result = EnterFirmwareUpgradeMode(fwUpdateInfo.primaryImage);
                    }

                    // Model ID Validation
                    // Send model ID to PSU. 
                    // PSU will verify model ID and erase current firmware image
                    if (result == CompletionCode.Success)
                    {
                        // Wait for PSU to turn off output and enter programming mode
                        Thread.Sleep(psuEnterProgrammingModeTimeMs);
                        enteredFwUpgradeMode = true;

                        fwUpdateStage = FwUpdateStageEnum.SendModelId;
                        result = SendModelId(modelId);
                    }

                    // Check status for Model ID verification
                    if (result == CompletionCode.Success)
                    {
                        // Wait for PSU to erase program memory
                        Thread.Sleep(psuProgramEraseTimeMs);

                        FirmwareUpdateStatusPacket response = GetFirmwareUpdateStatus();
                        if (response.CompletionCode != CompletionCode.Success)
                        {
                            result = CompletionCode.UnspecifiedError;
                            Tracer.WriteWarning("ExecuteFirmwareUpdate(): GetFirmwareUpdateStatus failure on PSU " + 
                                this.PsuId + " after sending model ID. Completion Code: " + response.CompletionCode);
                        }
                        else
                        {
                            if (response.UpdateStatus != PsuFwUpdateStatus.FlashEraseOK)
                            {
                                result = CompletionCode.UnspecifiedError;
                                Tracer.WriteWarning("ExecuteFirmwareUpdate(): Model ID verification failed on PSU " +
                                    this.PsuId + " with status: " + response.UpdateStatus.ToString());
                            }
                        }
                    }

                    // Write new firmware image to PSU
                    if (result == CompletionCode.Success)
                    {
                        // Wait for PSU I2C Packet Handling Time
                        Thread.Sleep(psuI2CPacketHandleTimeMs);
                        fwUpdateStage = FwUpdateStageEnum.WriteFirmwareImage;
                        result = WriteFirmwareImage(fwImage, fwUpdateInfo.primaryImage);
                    }

                    // Read back firmware image and verify that the image was written correctly
                    if (result == CompletionCode.Success)
                    {
                        fwUpdateStage = FwUpdateStageEnum.VerifyFirmwareImage;
                        result = VerifyFirmwareImage(fwImage, fwUpdateInfo.primaryImage);
                    }

                    // Exit programming mode
                    if (result == CompletionCode.Success)
                    {
                        fwUpdateStage = FwUpdateStageEnum.ExitFirmwareUpgradeMode;
                        result = ExitFirmwareUpgradeMode();
                    }

                    if (result == CompletionCode.Success)
                    {
                        // Update firmware upgrade status
                        fwUpdateStage = FwUpdateStageEnum.Completed;
                        fwUpdateStatus = FwUpdateStatusEnum.Success;
                    }
                    else
                    {
                        if (enteredFwUpgradeMode)
                        {
                            ExitFirmwareUpgradeMode();
                        }
                        fwUpdateStatus = FwUpdateStatusEnum.Failed;
                        Tracer.WriteWarning("ExecuteFirmwareUpdate failed on PSU " + this.PsuId + 
                            " at stage: " + fwUpdateStage.ToString() + 
                            " with completion code: " + result);
                    }

                    ChassisState.PsuFwUpdateInProgress[this.PsuId - 1] = false;
                    return;
                }
                catch (Exception ex)
                {
                    if (enteredFwUpgradeMode)
                    {
                        ExitFirmwareUpgradeMode();
                    }
                    fwUpdateStatus = FwUpdateStatusEnum.Failed;
                    Tracer.WriteWarning("ExecuteFirmwareUpdate failed on PSU " + this.PsuId + 
                        " at stage: " + fwUpdateStage.ToString() + 
                        " with exception: " + ex.Message);
                    ChassisState.PsuFwUpdateInProgress[this.PsuId - 1] = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Extracts model ID from firmware image
        /// </summary>
        /// <param name="fwImage">String array containing the firmware image</param>
        /// <param name="primaryImage">
        /// True:  Firmware image is for primary controller. 
        /// False: Firmware image is for secondary controller. 
        private byte[] ExtractModelId(string[] fwImage, bool primaryImage)
        {
            // Extract Model ID
            // The model ID is 8 bytes long, and is made up by concatenating the first 2 bytes
            // of each 4 byte word. For example, for the following line
            // in the image file for the secondary image:
            // 10300000504C0000313600003030000048530000C2
            // The model ID is: 0x504C313630304853

            // Find the line containing the model ID in the image file. 
            string tmpStr;
            StringBuilder strBuilder = new StringBuilder();
            string modelIdLocation = primaryImage ? priModelIdOffset : secModelIdOffset;
            // Find index of the model ID
            int dataIdx = Array.FindIndex<string>(fwImage, p => p.StartsWith(modelIdLocation));
            if (dataIdx == -1)
            {
                Tracer.WriteWarning("ExtractModelId: PSU {0}. Cannot find model ID in firmware image file. Primary Image: {1}", 
                    this.PsuId, primaryImage);
                return null;
            }

            tmpStr = fwImage[dataIdx];
            if (string.IsNullOrEmpty(tmpStr) || tmpStr.Length != fwImageDataLineLength)
            {
                return null;
            };
            // Remove checksum and also first 8 characters
            strBuilder.Append(new StringBuilder(tmpStr).Remove(40, 2).Remove(0, 8));

            // Remove characters that are not part of the model ID
            // At this point, strBuilder == 504C0000313600003030000048530000
            strBuilder = strBuilder.Remove(28, 4);
            for (int idx = 0; idx < 3; idx++)
            {
                strBuilder = strBuilder.Remove(20 - idx * 8, 4);
            }

            // Convert from hex string to byte array
            byte[] modelId = Ipmi.IpmiSharedFunc.HexStringToByteArray(strBuilder.ToString());
            Tracer.WriteInfo("ExtractModelId: PSU {0}. Model ID: {1}", this.PsuId, strBuilder.ToString());

            return modelId;
        }

        /// <summary>
        /// Writes firmware image to the PSU
        /// </summary>
        /// <param name="fwImage">String array containing the firmware image</param>
        /// <param name="primaryImage">
        /// True:  Firmware image is for primary controller. 
        /// False: Firmware image is for secondary controller. 
        /// <returns>Completion Code.</returns>
        private CompletionCode WriteFirmwareImage(string[] fwImage, bool primaryImage)
        {
            CompletionCode result = CompletionCode.UnspecifiedError;

            // Determine the start/end offsets for the image file
            string startOffset = primaryImage ? priFwWriteStartOffset : secFwWriteStartOffset;
            string endOffset = primaryImage ? priFwEndOffset : secFwEndOffset;
            byte endRomPage = primaryImage ? priFwEndPage : secFwEndPage;
            // Determine the proper time delays for primary/secondary controller
            int psuI2CPacketHandleTimeMs = primaryImage ? psuPriI2CPacketHandleTimeMs : psuSecI2CPacketHandleTimeMs;
            int psuBlockProgramWriteTimeMs = primaryImage ? psuPriBlockProgramWriteTimeMs : psuSecBlockProgramWriteTimeMs;

            Tracer.WriteInfo("WriteFirmwareImage(): PSU {0}. Primary Image: {1}", this.PsuId, primaryImage);

            // Find index of first line of the firmware image file
            int dataIdx = Array.FindIndex<string>(fwImage, p => p.StartsWith(startOffset));
            if (dataIdx == -1)
            {
                Tracer.WriteWarning("WriteFirmwareImage: PSU {0}. Cannot find start of firmware image file. Primary Image: {1}", 
                    this.PsuId, primaryImage);
                return result;
            }

            int blockIdx = 0;
            string tmpStr;
            StringBuilder fwDataStr;
            byte[] fwDataByteArr;
            byte romPage = 0;

            bool writeComplete = false;
            while (!writeComplete)
            {
                // Check validity of each line of the firmware image file
                tmpStr = fwImage[dataIdx];
                if ((string.IsNullOrEmpty(tmpStr)) || (tmpStr.Length != fwImageDataLineLength))
                {
                    Tracer.WriteWarning("WriteFirmwareImage: PSU {0}. Invalid line in firmware image file. Data index: {1} Line value: {2}",
                        this.PsuId, dataIdx, tmpStr);
                    result = CompletionCode.UnspecifiedError;
                    return result;
                };

                // Extract low/high 8 bytes of the payload and write to PSU
                for (int i = 0; i < 2; i++)
                {
                    // Remove hex checksum, 8 bytes of payload (low or high), record type and data length
                    fwDataStr = new StringBuilder(tmpStr).Remove(40, 2).Remove(24 - 16*i, 16).Remove(6, 2).Remove(0, 2);
                   
                    // Convert to byte array and update low byte of address
                    fwDataByteArr = Ipmi.IpmiSharedFunc.HexStringToByteArray(fwDataStr.ToString());
                    fwDataByteArr[1] = (byte)(fwDataByteArr[1] + (i*8));

                    // Write to PSU
                    result = WriteProgramMemory(fwDataByteArr, false);
                    if (result != CompletionCode.Success)
                    {
                        Tracer.WriteWarning("WriteFirmwareImage: PSU " + this.PsuId + 
                            ". Error writing to program memory: Completion Code: " + result +
                            " Data index: " + dataIdx + " Line value: " + fwDataStr.ToString());
                        return result;
                    }
                    // Wait for PSU I2C Packet Handling Time
                    Thread.Sleep(psuI2CPacketHandleTimeMs);
                }

                dataIdx++;
                blockIdx++;
                if (blockIdx == 16)
                {
                    blockIdx = 0;
                    // Wait for PSU to write block every 16 lines
                    Thread.Sleep(psuBlockProgramWriteTimeMs);
                }

                // Check write status
                FirmwareUpdateStatusPacket response = GetFirmwareUpdateStatus();
                if (response.CompletionCode != CompletionCode.Success)
                {
                    Tracer.WriteWarning("WriteFirmwareImage: PSU " + this.PsuId + 
                        ". GetFirmwareUpdateStatus Failure. Completion Code: " + response.CompletionCode +
                        " Data index: " + dataIdx + " Line value: " + tmpStr);
                    result = response.CompletionCode;
                    return result;
                }
                else
                {
                    if (response.UpdateStatus != PsuFwUpdateStatus.NoError)
                    {
                        Tracer.WriteWarning("WriteFirmwareImage: PSU " + this.PsuId +
                            ". Write failed with status: " + response.UpdateStatus.ToString() +
                            " Data index: " + dataIdx + " Line value: " + tmpStr);
                        result = CompletionCode.UnspecifiedError;
                        return result;
                    }
                }
                // Wait for PSU I2C Packet Handling Time
                Thread.Sleep(psuI2CPacketHandleTimeMs);

                // Check if we are at the end of the ROM page
                if (tmpStr.StartsWith(pageEndOffset))
                {
                    // Next line in firmware image file contains the ROM page update data
                    tmpStr = fwImage[dataIdx];
                    if ((string.IsNullOrEmpty(tmpStr)) || (tmpStr.Length != fwImageRomPageLineLength))
                    {
                        Tracer.WriteWarning("WriteFirmwareImage: PSU " + this.PsuId + 
                            ". Invalid ROM page update line in firmware image file. Data index: " +
                            dataIdx + " Line value: " + tmpStr);
                        result = CompletionCode.UnspecifiedError;
                        return result;
                    };

                    // Remove hex checksum, record type and data length
                    // Convert to byte array and update low byte of address
                    fwDataByteArr = Ipmi.IpmiSharedFunc.HexStringToByteArray(tmpStr.Remove(12, 2).Remove(6, 2).Remove(0, 2));
                    fwDataByteArr[1] = (byte)(fwDataByteArr[1] + 4);

                    // Write to PSU
                    result = WriteProgramMemory(fwDataByteArr, true);
                    if (result != CompletionCode.Success)
                    {
                        Tracer.WriteWarning("WriteFirmwareImage: PSU " + this.PsuId + 
                            ". Error writing ROM page to program memory: Completion Code: " + result +
                            " Data index: " + dataIdx + " Line value: " + tmpStr);
                        return result;
                    }
                    dataIdx++;
                    romPage++;
                    // Wait for PSU I2C Packet Handling Time
                    Thread.Sleep(psuI2CPacketHandleTimeMs);
                }

                // Check if we are at the end of the file
                if ((tmpStr.StartsWith(endOffset)) && (romPage == endRomPage))
                {
                    writeComplete = true;
                }
            }

            result = CompletionCode.Success;
            return result;
        }

        /// <summary>
        /// Verifies that the firmware image in the PSU matches the specified file
        /// </summary>
        /// <param name="fwImage">String array containing the firmware image</param>
        /// <param name="primaryImage">
        /// True:  Firmware image is for primary controller. 
        /// False: Firmware image is for secondary controller. 
        /// </param>
        /// <returns>Completion Code.</returns>
        private CompletionCode VerifyFirmwareImage(string[] fwImage, bool primaryImage)
        {
            CompletionCode result = CompletionCode.UnspecifiedError;

            // Find the start/end offsets for the image file
            string startOffset = primaryImage ? priFwReadStartOffset : secFwReadStartOffset;
            string endOffset = primaryImage ? priFwEndOffset : secFwEndOffset;
            byte endRomPage = primaryImage ? priFwEndPage : secFwEndPage;
            int psuI2CPacketHandleTimeMs = primaryImage ? psuPriI2CPacketHandleTimeMs : psuSecI2CPacketHandleTimeMs;
            int psuByteReadTimeMs = primaryImage ? psuPriByteReadTimeMs : psuSecByteReadTimeMs;

            Tracer.WriteInfo("VerifyFirmwareImage(): PSU {0}. Primary Image: {1}", this.PsuId, primaryImage);

            // Find index of first line of the firmware image file
            int dataIdx = Array.FindIndex<string>(fwImage, p => p.StartsWith(startOffset));
            if (dataIdx == -1)
            {
                Tracer.WriteWarning("VerifyFirmwareImage: PSU {0}. Cannot find start of firmware image file. Primary Image: {1}",
                    this.PsuId, primaryImage);
                return result;
            }

            // Initialize ROM page
            byte romPage = 0;
            result = SendRomPage(romPage);
            if (result != CompletionCode.Success)
            {
                Tracer.WriteWarning("VerifyFirmwareImage: PSU {0}. Error initializing ROM page", this.PsuId);
                return result;
            }
            // Wait for PSU I2C Packet Handling Time
            Thread.Sleep(psuI2CPacketHandleTimeMs);

            string tmpStr;
            StringBuilder fwDataStr;
            bool readComplete = false;
            ReadProgramMemoryPacket readProgramPacket;
            
            // Convert address from string in firmware file to short
            byte[] readAddrArr = Ipmi.IpmiSharedFunc.HexStringToByteArray(startOffset.Substring(2, 4));
            short readAddr = (short)((readAddrArr[0] << 8) | readAddrArr[1]);
            int psuI2CWaitTimeMs;
            
            while (!readComplete)
            {
                tmpStr = fwImage[dataIdx];
                if ((string.IsNullOrEmpty(tmpStr)) || (tmpStr.Length != fwImageDataLineLength))
                {
                    Tracer.WriteWarning("VerifyFirmwareImage: PSU " + this.PsuId + 
                        ". Invalid line in firmware image file. Data index: " + 
                        dataIdx + " Line value: " + tmpStr);
                    result = CompletionCode.UnspecifiedError;
                    return result;
                };

                for (int i = 0; i < 2; i++)
                {
                    // Extract low/high 8 bytes of the payload
                    // Remove hex checksum, 8 bytes of payload (low or high), record type, address offset and data length
                    fwDataStr = new StringBuilder(tmpStr).Remove(40, 2).Remove(24 - 16 * i, 16).Remove(0, 8).Insert(0, "0x");
                    readAddr += (short)(i * 8);

                    // Read image data from PSU and compare to firmware image file
                    psuI2CWaitTimeMs = (i == 0) ? psuI2CPacketHandleTimeMs : psuByteReadTimeMs;
                    readProgramPacket = ReadProgramMemory(readAddr, psuI2CWaitTimeMs);
                    if (readProgramPacket.CompletionCode != CompletionCode.Success)
                    {
                        Tracer.WriteWarning("VerifyFirmwareImage: PSU {0}. Error reading from program memory: Completion Code: {1}",
                            this.PsuId, readProgramPacket.CompletionCode);
                        Tracer.WriteWarning("File line: " + (dataIdx + 1) + 
                            string.Format(" Read address: 0x{0:X}", readAddr) + " Expected data: " + fwDataStr.ToString());

                        result = readProgramPacket.CompletionCode;
                        return result;
                    }
                    else if (!String.Equals(fwDataStr.ToString(), Ipmi.IpmiSharedFunc.ByteArrayToHexString(readProgramPacket.Data))) 
                    {
                        Tracer.WriteWarning("VerifyFirmwareImage: Psu {0}. PSU Data does not match firmware image file.", this.PsuId);
                        Tracer.WriteWarning("File line: " + (dataIdx + 1) + string.Format(" Read address: 0x{0:X}", readAddr) +
                            " PSU Data: " + Ipmi.IpmiSharedFunc.ByteArrayToHexString(readProgramPacket.Data) +
                            " Expected data: " + fwDataStr.ToString());
                        result = CompletionCode.UnspecifiedError;
                        return result;                        
                    }
                    // Wait for PSU byte read time
                    Thread.Sleep(psuByteReadTimeMs);
                }
                dataIdx++;
                readAddr += 0x08;

                // Check if we are at the end of the ROM page
                if (tmpStr.StartsWith(pageEndOffset))
                {
                    // Skip next line in firmware image file since it contains the ROM page update data
                    dataIdx++;
                    readAddr = 0;
                    // Update ROM page in PSU so that we can read the next page
                    romPage++;
                    result = SendRomPage(romPage);
                    if (result != CompletionCode.Success)
                    {
                        Tracer.WriteWarning("VerifyFirmwareImage: Psu " + this.PsuId + 
                            ". Error writing ROM page to program memory: Completion Code: " + result +
                            " Data index: " + dataIdx + " ROM Page: " + romPage);
                        return result;
                    }
                    // Wait for PSU I2C Packet Handling Time
                    Thread.Sleep(psuI2CPacketHandleTimeMs);
                }

                // Check if we are at the end of the file
                if ((tmpStr.StartsWith(endOffset)) && (romPage == endRomPage))
                {
                    readComplete = true;
                }
            }

            result = CompletionCode.Success;
            return result;
        }

        #endregion


        #region Psu Request Classes

        /// <summary>
        /// EnterFirmwareUpgradeMode Request structure.
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class EnterFirmwareUpgradeModeRequest : ChassisRequest
        {
            public EnterFirmwareUpgradeModeRequest(byte psuId, byte[] commandPayload, bool primaryImage)
            {
                byte commandOpCode = (byte)PmBusCommand.ENTER_ISP;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.ExpectedPsuResponseLength = 0;

                // Select pre-calculated PEC based on I2C slave address for PSU
                // and payload. The command code ix 0xF0, and the payload is
                // 0xCDAB for secondary image and 0x1234 for primary image.
                // For example, the command for PSU 1 with secondary image is 0xB0F0CDAB.
                int pec;
                if ((psuId & 0x1) == 1)
                {
                    // PSU 1, 3, 5. I2C slave address = 0xB0
                    pec = primaryImage? 0x27 : 0xA8;
                }
                else
                {
                    // PSU 2, 4, 6. I2C slave address = 0xB2
                    pec = primaryImage? 0x0B : 0x84;
                }
                this.Pec = (byte)pec;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload PEC (Packet Error Check). CRC-8 as defined in section 5.4 of SMBus v2.0 specification
            /// </summary>
            [ChassisMessageData(3)]
            public byte Pec
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(4)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// SendModelId Request structure.
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class PsuSendModelIdRequest : ChassisRequest
        {
            public PsuSendModelIdRequest(byte psuId, byte[] commandPayload)
            {
                byte commandOpCode = (byte)PmBusCommand.SEND_MODEL_ID;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, commandPayload, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(9)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(10)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// PsuWriteProgramMemoryFwRequest Request structure.
        /// The payload format is as follows (offset, length):
        /// - Address offset: (0, 2) - offset to write the data
        /// - Data: (2, 8)
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class PsuWriteProgramMemoryFwRequest : ChassisRequest
        {
            public PsuWriteProgramMemoryFwRequest(byte psuId, byte[] commandPayload)
            {
                byte commandOpCode = (byte)PmBusCommand.WRITE_MEMORY;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, commandPayload, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(11)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(12)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// PsuWriteProgramMemoryRomPageRequest Request structure.
        /// The payload format is as follows (offset, length):
        /// - Address bytes: (0, 2)
        /// - Page bytes: (2, 2)        
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class PsuWriteProgramMemoryRomPageRequest : ChassisRequest
        {
            public PsuWriteProgramMemoryRomPageRequest(byte psuId, byte[] commandPayload)
            {
                byte commandOpCode = (byte)PmBusCommand.WRITE_MEMORY;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, commandPayload, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(5)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(6)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// PsuReadProgramMemoryTargetAddressRequest Request structure.
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class PsuReadProgramMemoryTargetAddressRequest : ChassisRequest
        {
            public PsuReadProgramMemoryTargetAddressRequest(byte psuId, byte[] commandPayload)
            {
                byte commandOpCode = (byte)PmBusCommand.READ_MEMORY;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, commandPayload, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(3)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(4)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// SendRomPage Request structure.
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class PsuSendRomPageRequest : ChassisRequest
        {
            public PsuSendRomPageRequest(byte psuId, byte romPage)
            {
                byte commandOpCode = (byte)PmBusCommand.SEND_MEMORY_PAGE;
                this.PsuCommand = commandOpCode;
                this.Payload = romPage;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, new byte[1] {romPage}, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(2)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(3)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        /// <summary>
        /// ExitFirmwareUpgradeMode Request structure.
        /// </summary>
        [ChassisMessageRequest(FunctionCode.PsuOperations)]
        internal class ExitFirmwareUpgradeModeRequest : ChassisRequest
        {
            public ExitFirmwareUpgradeModeRequest(byte psuId, byte[] commandPayload)
            {
                byte commandOpCode = (byte)PmBusCommand.EXIT_ISP;
                this.PsuCommand = commandOpCode;
                this.Payload = commandPayload;
                this.Checksum = CalculatePayloadChecksum(psuId, commandOpCode, commandPayload, false);
                this.ExpectedPsuResponseLength = 0;
            }

            /// <summary>
            /// Psu command byte to be sent on the wire
            /// </summary>
            [ChassisMessageData(0)]
            public byte PsuCommand
            {
                get;
                set;
            }

            /// <summary>
            /// Psu command payload
            /// </summary>
            [ChassisMessageData(1)]
            public byte[] Payload
            {
                get;
                set;
            }

            /// <summary>
            /// Psu payload checksum
            /// </summary>
            [ChassisMessageData(3)]
            public byte Checksum
            {
                get;
                set;
            }

            /// <summary>
            /// Expected length of the response message
            /// </summary>
            [ChassisMessageData(4)]
            public byte ExpectedPsuResponseLength
            {
                get;
                set;
            }
        }

        #endregion

        #region Psu Response Structures

        /// <summary>
        /// GetBatteryFaultIndicator response structure.
        /// </summary>
        public class BatteryFaultIndicatorPacket : PsuResponseBasePacket
        {
            public byte BatteryFault;
            public byte UnderVoltage;
            public byte OverVoltage;
            public byte OverCurrentCharge;
            public byte OverCurrentDischarge;
            public byte OverTemp;
            public byte CellBalanceNotGood;
        }

        /// <summary>
        /// GetBatteryPowerOut response structure.
        /// </summary>
        public class BatteryPowerPacket : PsuResponseBasePacket
        {
            public double BatteryPower;
        }

        /// <summary>
        /// GetBatteryHealthStatus response structure.
        /// </summary>
        public class BatteryHealthStatusPacket : PsuResponseBasePacket
        {
            public byte OverChargedAlarm;
            public byte TerminateChargeAlarm;
            public byte OverTempAlarm;
            public byte TerminateDischargeAlarm;
            public byte RemainingCapacityAlarm;
            public byte RemainingTimeAlarm;
            public byte Initialized;
            public byte Discharging;
            public byte FullyCharged;
            public byte FullyDischarged;
        }

        /// <summary>
        /// GetBatteryChargeLevel response structure.
        /// </summary>
        public class BatteryChargeLevelPacket : PsuResponseBasePacket
        {
            public double BatteryChargeLevel;
        }

        /// <summary>
        /// GetBatteryOpTime response structure.
        /// </summary>
        public class BatteryOpTimePacket : PsuResponseBasePacket
        {
            public double BatteryOpTime;
        }

        /// <summary>
        /// GetFirmwareRevision response structure.
        /// </summary>
        public class FirmwareRevisionPacket : PsuResponseBasePacket
        {
            public string FwRevision;
        }

        /// <summary>
        /// ReadProgramMemory response structure.
        /// </summary>
        public class ReadProgramMemoryPacket : PsuResponseBasePacket
        {
            public byte[] Data;
        }

        /// <summary>
        /// GetFirmwareUpdateStatus response structure.
        /// </summary>
        public class FirmwareUpdateStatusPacket : PsuResponseBasePacket
        {
            public PsuFwUpdateStatus UpdateStatus;
        }

        #endregion

        #region Psu Response Classes

        /// <summary>
        /// Represents the Psu 'GetBatteryExtendedOperationStatus' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryExtendedOperationStatusResponse : ChassisResponse
        {
            private byte extendedOperationStatus;

            [ChassisMessageData(0, (int)PmBusResponseLength.EXTENDED_BATTERY)]
            public byte ExtendedOperationStatus
            {
                get { return this.extendedOperationStatus; }
                set { this.extendedOperationStatus = value; }
            }
        }

        /// <summary>
        /// Represents the Psu 'GetBatteryFaultIndicator' response message.
        /// Fault	Bit 15	
        ///   1	Any fault bits (bit 9-14 below) will set this register. This bit have the same timing with FAULT pin.
        ///   0	All fault bits (bit 9-14 below) are reset
        /// UVP (Under voltage Protection)	Bit 14	
        ///   1	Battery under voltage is detected
        ///   0	Battery under voltage is not  detected
        /// OCCP (Over charge current protection)	Bit 13	
        ///   1	Battery charge over-current is detected 
        ///   0	Battery charge current is normal 
        /// OTP (Discharge temperature)	Bit 12	
        ///   1	Battery over-temperature is detected
        ///   0	Battery over-temperature is not detected
        /// CBN (Cell balance NG)	Bit 11	
        ///   1	Battery cell balance is not good
        ///   0	Battery cell balance is normal
        /// OVP (Over voltage protection)	Bit 10	
        ///   1	Battery over-voltage is detected
        ///   0	Battery over-voltage is not detected
        /// OCDP (Over current discharge protection)	Bit 9	
        ///   1	Battery discharge over-current is detected
        ///   0	Battery discharge current is normal
        /// Reserved bits	Bit8- Bit 0	Reserved bits
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryFaultIndicatorResponse : ChassisResponse
        {
            private byte[] batteryFaultIndicator;

            [ChassisMessageData(0, (int)PmBusResponseLength.FAULT_INDICATOR)]
            public byte[] BatteryFaultIndicator
            {
                get { return this.batteryFaultIndicator; }
                set { this.batteryFaultIndicator = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'GetBatteryPowerOut' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryPowerOutResponse : ChassisResponse
        {
            private byte[] batteryPower;

            [ChassisMessageData(0, (int)PmBusResponseLength.BATT_POUT)]
            public byte[] BatteryPower
            {
                get { return this.batteryPower; }
                set { this.batteryPower = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'GetBatteryHealthStatus' response message.
        /// Over Charged Alarm	Bit 15	
        ///   1	Full charge or over charge is detected
        ///   0	Stop charging
        /// Terminate Charge Alarm	Bit 14	
        ///   1	At least one of the following conditions.
        ///            *OVER_TEMP_ALARM = 1 at charging
        ///            *Over charge current is detected
        ///            *Cell short error is detected
        ///            *Full Charge is detected
        ///   0	All of set conditions is cleared
        /// Reserved	Bit 13
        /// Over Temp Alarm	Bit 12	
        ///   1	*In charging
        ///            Cell temperature > 53 degree C
        ///            or Cell temperature < 3 degree C
        ///     *Not charging
        ///            Cell temperature  > 50 degree C
        ///            or Cell temperature < 5 degree C
        ///   0	5 degree C <= Cell temperature <= 50 degree C
        /// Terminate Discharge Alarm	Bit 11	
        ///   1	0% or over discharge is detected
        ///   0	Discharge current is no longer detected
        /// Reserved	Bit 10
        /// Remaining Capacity Alarm	Bit 9	
        ///   1	Remaining Capacity <= Remaining Capacity Alarm
        ///   0	Remaining Capacity  > Remaining Capacity Alarm
        /// Remaining Time Alarm	Bit 8	
        ///   1	Average Time to Empty <= Remaining Time Alarm
        ///   0	Average Time to Empty > Remaining Time Alarm
        /// Initialized	Bit 7	
        ///   1	Assembled in battery manufacturer
        ///   0	EEPROM Error is detected
        /// Discharging	Bit 6	
        ///   1	Not charging
        ///   0	In charging
        /// Fully Charged	Bit 5	
        ///   1 Full charge is detected
        ///   0	RSOC <=90% or OCV <= OCV worth 90% of full charged capacity
        /// Fully Discharged	Bit 4	
        ///   1	0% or over discharge is detected
        ///   0	RSOC >= 20%
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryHealthStatusResponse : ChassisResponse
        {
            private byte[] batteryHealthStatus;

            [ChassisMessageData(0, (int)PmBusResponseLength.BATT_HEALTH_STATUS)]
            public byte[] BatteryHealthStatus
            {
                get { return this.batteryHealthStatus; }
                set { this.batteryHealthStatus = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'GetBatteryChargeLevel' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryChargeLevelResponse : ChassisResponse
        {
            private byte[] batteryChargeLevel;

            [ChassisMessageData(0, (int)PmBusResponseLength.BATT_STATE_OF_CHARGE)]
            public byte[] BatteryChargeLevel
            {
                get { return this.batteryChargeLevel; }
                set { this.batteryChargeLevel = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'Get Battery Op Time' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class BatteryOpTimeResponse : ChassisResponse
        {
            private byte[] batteryOpTime;

            [ChassisMessageData(0, (int)PmBusResponseLength.BATT_OP_TIME_100_LOAD)]
            public byte[] BatteryOpTime
            {
                get { return this.batteryOpTime; }
                set { this.batteryOpTime = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'GetFirmwareRevision' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class GetFirmwareRevisionResponse : ChassisResponse
        {
            private byte[] fwRevision;

            [ChassisMessageData(0, (int)PmBusResponseLength.FW_REVISION)]
            public byte[] FwRevision
            {
                get { return this.fwRevision; }
                set { this.fwRevision = value; }

            }
        }

        /// <summary>
        /// Represents the Psu 'ReadProgramMemory' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class ReadProgramMemoryResponse : ChassisResponse
        {
            private byte[] data;
            private byte checksum;

            [ChassisMessageData(0, 8)]
            public byte[] Data
            {
                get { return this.data; }
                set { this.data = value; }
            }

            [ChassisMessageData(8, 1)]
            public byte Checksum
            {
                get { return this.checksum; }
                set { this.checksum = value; }
            }
        }

        /// <summary>
        /// Represents the Psu 'GetFirmwareUpdateStatus' response message.
        /// </summary>
        [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
        internal class GetFirmwareUpdateStatusResponse : ChassisResponse
        {
            private byte updateStatus;
            private byte checksum;

            [ChassisMessageData(0, 1)]
            public byte UpdateStatus
            {
                get { return this.updateStatus; }
                set { this.updateStatus = value; }
            }

            [ChassisMessageData(1, 1)]
            public byte Checksum
            {
                get { return this.checksum; }
                set { this.checksum = value; }
            }
        }

        #endregion

    }
}
