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
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// PsuBase is the base class for derived Psu device classes
    /// </summary>
    internal class PsuBase : ChassisSendReceive
    {   
        /// <summary>
        ///  Device Type
        /// </summary>
        private static DeviceType psuDeviceType;

        /// <summary>
        /// Device Id for the Psu 
        /// </summary>
        private byte psuId;

        internal PsuBase(byte deviceId)
        {
            this.psuId = deviceId;
            psuDeviceType = DeviceType.Psu;
        }

        internal DeviceType PsuDeviceType
        {
            get { return psuDeviceType; }
        }

        internal byte PsuId
        {
            get { return psuId; }
        }

        #region Virtual Methods

        /// <summary>
        /// Gets the Psu Model Number
        /// </summary>
        /// <returns></returns>
        internal virtual PsuModelNumberPacket GetPsuModel()
        {
            return GetPsuModel(this.PsuId);
        }

        /// <summary>
        /// Gets the Psu Serial Number
        /// </summary>
        /// <returns></returns>
        internal virtual PsuSerialNumberPacket GetPsuSerialNumber()
        {
            return GetPsuSerialNumber(this.PsuId);
        }

        /// <summary>
        /// Gets the Psu Status
        /// </summary>
        /// <returns></returns>
        internal virtual PsuStatusPacket GetPsuStatus()
        {
            return GetPsuStatus(this.PsuId);
        }

        /// <summary>
        /// Log the Psu detailed fault status registers in the trace log
        /// </summary>
        /// <returns></returns>
        internal virtual PsuResponseBasePacket LogPsuFaultStatus()
        {
            return LogPsuFaultStatus(this.PsuId);
        }

        /// <summary>
        /// Gets the Psu Power usage.
        /// </summary>
        /// <returns></returns>
        internal virtual PsuPowerPacket GetPsuPower()
        {
            return GetPsuPower(this.PsuId);
        }

        /// <summary>
        /// Clears Psu Error Status.
        /// </summary>
        /// <returns></returns>
        internal virtual CompletionCode SetPsuClearFaults()
        {
            return SetPsuClearFaults(this.PsuId);
        }

        /// <summary>
        /// Set PSU On/OFF
        /// </summary>
        /// <param name="off">true = OFF, false = ON</param>
        /// <returns>Completion code success/failure</returns>
        internal virtual CompletionCode SetPsuOnOff(bool off)
        {
            return CompletionCode.CmdFailedNotSupportedInPresentState;
        }

        /// <summary>
        /// Gets the battery status
        /// </summary>
        /// <returns></returns>
        internal virtual BatteryStatusPacket GetBatteryStatus()
        {
            BatteryStatusPacket infoPacket = new BatteryStatusPacket();
            
            // Indicate no battery present by default
            infoPacket.Presence = 0;
            infoPacket.CompletionCode = CompletionCode.UnspecifiedError;

            return infoPacket;
        }

        /// <summary>
        /// Set Battery Extended Operation Mode
        /// </summary>
        /// <param name="psuId"></param>
        /// <param name="toEnable"></param>
        /// <returns></returns>
        internal virtual CompletionCode SetBatteryExtendedOperationMode(bool toEnable)
        {
            CompletionCode completionCode = CompletionCode.UnspecifiedError;
            return completionCode;
        }

        /// <summary>
        /// Get Battery Extended Operation Mode
        /// </summary>
        /// <param name="psuId"></param>
        /// <param name="toEnable"></param>
        /// <returns></returns>
        internal virtual BatteryExtendedOperationStatusPacket GetBatteryExtendedOperationStatus()
        {
            BatteryExtendedOperationStatusPacket responsePacket = new BatteryExtendedOperationStatusPacket();
            responsePacket.CompletionCode = CompletionCode.UnspecifiedError;
            responsePacket.isEnabled = false;
            return responsePacket;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Attempts to retrieve the Psu Model Number.  This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private PsuModelNumberPacket GetPsuModel(byte psuId)
        {
            // Initialize return packet 
            PsuModelNumberPacket returnPacket = new PsuModelNumberPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.ModelNumber = "";

            try
            {
                PsuModelResponse myResponse = new PsuModelResponse();
                myResponse = (PsuModelResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, new PsuRequest((byte)PmBusCommand.MFR_MODEL,(byte)PmBusResponseLength.MFR_MODEL), typeof(PsuModelResponse));
                
                // check for completion code 
                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    if (myResponse.PsuModelNumber != null)
                    {
                        byte[] inModelNumber = myResponse.PsuModelNumber;
                        byte[] outModelNumber = null;
                        PmBus.PsuModelNumberParser(ref inModelNumber, out outModelNumber);
                        returnPacket.ModelNumber = System.BitConverter.ToString(outModelNumber, 0);
                    }
                    else
                    {
                        returnPacket.ModelNumber = "";
                    }
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.ModelNumber = "";
                Tracer.WriteError(this.PsuId, DeviceType.Psu, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Psu Serial Number. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private PsuSerialNumberPacket GetPsuSerialNumber(byte psuId)
        {
            PsuSerialNumberPacket returnPacket = new PsuSerialNumberPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.SerialNumber = "";

            try
            {
                PsuSerialResponse myResponse = new PsuSerialResponse();
                myResponse = (PsuSerialResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, new PsuRequest((byte)PmBusCommand.MFR_SERIAL,(byte)PmBusResponseLength.MFR_SERIAL), typeof(PsuSerialResponse));

                // check for completion code 
                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    if (myResponse.PsuSerialNumber != null)
                    {
                        byte[] inSerialNumber = myResponse.PsuSerialNumber;
                        byte[] outSerialNumber = null;
                        PmBus.PsuSerialNumberParser(ref inSerialNumber, out outSerialNumber);
                        returnPacket.SerialNumber = System.BitConverter.ToString(outSerialNumber, 0);
                    }
                    else
                    {
                        returnPacket.SerialNumber = "";
                    }
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.SerialNumber = "";
                Tracer.WriteError(this.PsuId, DeviceType.Psu, ex);
            }

            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Psu Power. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private PsuPowerPacket GetPsuPower(byte psuId)
        {
            // Initialize return packet 
            PsuPowerPacket returnPacket = new PsuPowerPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.PsuPower = 0;

            byte[] powerValue = new byte[2];
            try
            {
                PsuPowerResponse myResponse = new PsuPowerResponse();
                myResponse = (PsuPowerResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, new PsuRequest((byte)PmBusCommand.READ_POUT,(byte)PmBusResponseLength.READ_POUT), typeof(PsuPowerResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    powerValue = myResponse.PsuPower;
                    byte[] convertedPowerValue = null;
                    PmBus.PmBusLinearDataFormatConverter(ref powerValue, out convertedPowerValue);
                    powerValue = convertedPowerValue;
                    returnPacket.PsuPower = System.BitConverter.ToInt32(powerValue, 0);
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.PsuPower = 0;
                Tracer.WriteError(this.PsuId, DeviceType.Psu, ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Psu Status. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private PsuStatusPacket GetPsuStatus(byte psuId)
        {
            // Initialize return packet 
            PsuStatusPacket returnPacket = new PsuStatusPacket();
            returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
            returnPacket.PsuStatus = 0;
            returnPacket.FaultPresent = false;

            try
            {       
                PsuStatusResponse myResponse = new PsuStatusResponse();
                myResponse = (PsuStatusResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, 
                    new PsuRequest((byte)PmBusCommand.STATUS_WORD,(byte)PmBusResponseLength.STATUS_WORD), typeof(PsuStatusResponse));

                if (myResponse.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)myResponse.CompletionCode;
                    Tracer.WriteWarning("GetPsuStatus Failure for PSU {0}: Completion Code: {1}", psuId, myResponse.CompletionCode);
                }
                else
                {
                    returnPacket.CompletionCode = CompletionCode.Success;
                    byte varStatus;
                    byte[] psuStatus = myResponse.PsuStatus;
                    
                    // If there are any other faults, indicate fault is present and log the full status word
                    if (!PmBus.ExtractPowerGoodFromPsuStatus(psuStatus, out varStatus))
                    {
                        Tracer.WriteWarning("GetPsuStatus: Psu ({0}) STATUS_WORD is non-zero: " +
                            "(High Byte: {1} Low Byte: {2}) (See STATUS_WORD register in PmBusII Manual)", 
                            this.PsuId, 
                            System.Convert.ToString(psuStatus[1], 2).PadLeft(8, '0'), 
                            System.Convert.ToString(psuStatus[0], 2).PadLeft(8, '0'));

                        returnPacket.FaultPresent = true;
                    }

                    returnPacket.PsuStatus = varStatus;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                returnPacket.PsuStatus = 0;
                Tracer.WriteError("GetPsuStatus Exception: " + ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to retrieve the Psu fault status registers. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private PsuResponseBasePacket LogPsuFaultStatus(byte psuId)
        {
            // Initialize return packet 
            PsuResponseBasePacket returnPacket = new PsuResponseBasePacket();
            returnPacket.CompletionCode = CompletionCode.Success;

            try
            {
                PsuCommandByteResponse response = new PsuCommandByteResponse();

                // STATUS_VOUT
                response = (PsuCommandByteResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.STATUS_VOUT, (byte)PmBusResponseLength.STATUS_VOUT), typeof(PsuCommandByteResponse));
                if (response.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("LogPsuFaultStatus: Failed to read STATUS_VOUT on PSU {0}: Completion Code: {1}", psuId, response.CompletionCode);
                }
                else
                {
                    Tracer.WriteWarning("LogPsuFaultStatus: PSU {0} STATUS_VOUT: {1}", psuId, System.Convert.ToString(response.PsuByteResponse, 2).PadLeft(8, '0'));
                }

                // STATUS_IOUT
                response = (PsuCommandByteResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.STATUS_IOUT, (byte)PmBusResponseLength.STATUS_IOUT), typeof(PsuCommandByteResponse));
                if (response.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("LogPsuFaultStatus: Failed to read STATUS_IOUT on PSU {0}: Completion Code: {1}", psuId, response.CompletionCode);
                }
                else
                {
                    Tracer.WriteWarning("LogPsuFaultStatus: PSU {0} STATUS_IOUT: {1}", psuId, System.Convert.ToString(response.PsuByteResponse, 2).PadLeft(8, '0'));
                }

                // STATUS_INPUT
                response = (PsuCommandByteResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.STATUS_INPUT, (byte)PmBusResponseLength.STATUS_INPUT), typeof(PsuCommandByteResponse));
                if (response.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("LogPsuFaultStatus: Failed to read STATUS_INPUT on PSU {0}: Completion Code: {1}", psuId, response.CompletionCode);
                }
                else
                {
                    Tracer.WriteWarning("LogPsuFaultStatus: PSU {0} STATUS_INPUT: {1}", psuId, System.Convert.ToString(response.PsuByteResponse, 2).PadLeft(8, '0'));
                }

                // STATUS_TEMPERATURE
                response = (PsuCommandByteResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.STATUS_TEMPERATURE, (byte)PmBusResponseLength.STATUS_TEMPERATURE), typeof(PsuCommandByteResponse));
                if (response.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("LogPsuFaultStatus: Failed to read STATUS_TEMPERATURE on PSU {0}: Completion Code: {1}", psuId, response.CompletionCode);
                }
                else
                {
                    Tracer.WriteWarning("LogPsuFaultStatus: PSU {0} STATUS_TEMPERATURE: {1}", psuId, System.Convert.ToString(response.PsuByteResponse, 2).PadLeft(8, '0'));
                }

                // STATUS_CML
                response = (PsuCommandByteResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                    new PsuRequest((byte)PmBusCommand.STATUS_CML, (byte)PmBusResponseLength.STATUS_CML), typeof(PsuCommandByteResponse));
                if (response.CompletionCode != 0)
                {
                    returnPacket.CompletionCode = (CompletionCode)response.CompletionCode;
                    Tracer.WriteWarning("LogPsuFaultStatus: Failed to read STATUS_CML on PSU {0}: Completion Code: {1}", psuId, response.CompletionCode);
                }
                else
                {
                    Tracer.WriteWarning("LogPsuFaultStatus: PSU {0} STATUS_CML: {1}", psuId, System.Convert.ToString(response.PsuByteResponse, 2).PadLeft(8, '0'));
                }
            }
            catch (System.Exception ex)
            {
                returnPacket.CompletionCode = CompletionCode.UnspecifiedError;
                Tracer.WriteError("LogPsuFaultStatus Exception: " + ex);
            }
            return returnPacket;
        }

        /// <summary>
        /// Attempts to clear the Psu error status. This method
        /// calls down to the Chassis Manager with SendReceive
        /// </summary>
        private CompletionCode SetPsuClearFaults(byte psuId)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                PsuChassisResponse myResponse = new PsuChassisResponse();
                myResponse = (PsuChassisResponse)this.SendReceive(this.PsuDeviceType, this.PsuId, new PsuRequest((byte)PmBusCommand.CLEAR_FAULTS, (byte)PmBusResponseLength.CLEAR_FAULTS), typeof(PsuChassisResponse));

                // check for completion code 
                if (myResponse.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)myResponse.CompletionCode;
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                returnPacket = CompletionCode.UnspecifiedError;
                Tracer.WriteError(this.PsuId, DeviceType.Psu, ex);
            }

            return returnPacket;
        }

        #endregion

    }

    #region Psu Response Structures
    
    /// <summary>
    /// Base PSU response packet
    /// </summary>
    public class PsuResponseBasePacket
    {
        public CompletionCode CompletionCode;
    }

    public class PsuModelNumberPacket : PsuResponseBasePacket
    {
        public string ModelNumber;
    }

    public class PsuSerialNumberPacket : PsuResponseBasePacket
    {
        public string SerialNumber;
    }

    public class PsuStatusPacket : PsuResponseBasePacket
    {
        public byte PsuStatus;
        public bool FaultPresent;
    }

    public class PsuPowerPacket : PsuResponseBasePacket
    {
        public double PsuPower;
    }

    /// <summary>
    /// Battery Status
    /// </summary>
    public class BatteryStatusPacket : PsuResponseBasePacket
    {
        public byte Presence;
        public double BatteryPowerOutput;
        public double BatteryChargeLevel;
        public byte FaultDetected;
    }

    /// <summary>
    /// GetBatteryExtendedOperationStatus response structure.
    /// </summary>
    public class BatteryExtendedOperationStatusPacket : PsuResponseBasePacket
    {
        public bool isEnabled;
    }

    #endregion

    #region Psu Request Structures

    /// <summary>
    /// Represents the Psu single master request message
    /// The first byte of the payload indicates the PSU pmbus command op code
    /// The second byte of the payload indicates the expected number of bytes to be read back from the PSU
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuOperations)]
    internal class PsuRequest : ChassisRequest
    {
        public PsuRequest(byte commandOpCode, byte expResponseLength)
        {
            this.PsuCommand = commandOpCode;
            this.ExpectedPsuResponseLength = expResponseLength;
        }

        /// <summary>
        /// Psu command byte to be send on the wire
        /// </summary>
        [ChassisMessageData(0)]
        public byte PsuCommand
        {
            get;
            set;
        }

        /// <summary>
        /// Expected length of the response message
        /// </summary>
        [ChassisMessageData(1)]
        public byte ExpectedPsuResponseLength
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents the Psu single master request message with command payload
    /// The first byte of the payload indicates the PSU pmbus command op code
    /// The second byte of the payload indicates the PSU pmbus command payload
    /// The third byte of the payload indicates the expected number of bytes to be read back from the PSU
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuOperations)]
    internal class PsuBytePayloadRequest : ChassisRequest
    {
        public PsuBytePayloadRequest(byte commandOpCode, byte commandPayload, byte expResponseLength)
        {
            this.PsuCommand = commandOpCode;
            this.Payload = commandPayload;
            this.ExpectedPsuResponseLength = expResponseLength;
        }

        /// <summary>
        /// Psu command byte to be send on the wire
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
        /// Expected length of the response message
        /// </summary>
        [ChassisMessageData(2)]
        public byte ExpectedPsuResponseLength
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents the Psu single master request message with two byte command payload
    /// The first byte of the payload indicates the PSU pmbus command op code
    /// The second and third bytes of the payload indicates the PSU pmbus command payload
    /// The fourth byte of the payload indicates the expected number of bytes to be read back from the PSU
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuOperations)]
    internal class PsuWordPayloadRequest : ChassisRequest
    {
        public PsuWordPayloadRequest(byte commandOpCode, byte[] commandPayload, byte expResponseLength)
        {
            this.PsuCommand = commandOpCode;
            this.Payload = commandPayload;
            this.ExpectedPsuResponseLength = expResponseLength;
        }

        /// <summary>
        /// Psu command byte to be send on the wire
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
        /// Expected length of the response message
        /// </summary>
        [ChassisMessageData(3)]
        public byte ExpectedPsuResponseLength
        {
            get;
            set;
        }
    }

    #endregion

    #region Psu Response Classes

    /* Since Response packet structures will be interpreted 
       differently depending on requested PSU functionality, 
       create separate response classes */

    /// <summary>
    /// Represents the Psu response message with zero payload
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuChassisResponse : ChassisResponse
    {
    }

    /// <summary>
    /// Represents the Psu 'Get Status' response message.
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuStatusResponse : ChassisResponse
    {
        private byte[] psuStatus;

        [ChassisMessageData(0, (int)PmBusResponseLength.STATUS_WORD)] // We are only interested in the high byte
        public byte[] PsuStatus
        {
            get { return this.psuStatus; }
            set { this.psuStatus = value; }
        }
    }

    /// <summary>
    /// Represents the Psu 'Get Model' response message.
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuModelResponse : ChassisResponse
    {
        private byte[] psuModelNumber;

        [ChassisMessageData(0, (int)PmBusResponseLength.MFR_MODEL)]
        public byte[] PsuModelNumber
        {
            get { return this.psuModelNumber; }
            set { this.psuModelNumber = value; }
        }
    }

    /// <summary>
    /// Represents the Psu 'Get Serial' response message.
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuSerialResponse : ChassisResponse
    {
        private byte[] psuSerialNumber;

        [ChassisMessageData(0, (int)PmBusResponseLength.MFR_SERIAL)]
        public byte[] PsuSerialNumber
        {
            get { return this.psuSerialNumber; }
            set { this.psuSerialNumber = value; }
        }
    }

    /// <summary>
    /// Represents the Psu 'Get Power' response message.
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuPowerResponse : ChassisResponse
    {
        private byte[] psuPower;

        [ChassisMessageData(0, (int)PmBusResponseLength.READ_POUT)]
        public byte[] PsuPower
        {
            get { return this.psuPower; }
            set { this.psuPower = value; }

        }

    }

    /// <summary>
    /// Represents the Psu ON/OFF response message.
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuOnOffResponse : ChassisResponse
    {
        /// <summary>
        /// Psu State:
        ///    80 = On
        ///     0 = Off
        /// </summary>
        private byte psuState;

        /// <summary>
        /// Psu State:
        ///     1 = On
        ///     0 = Off
        /// </summary>
        [ChassisMessageData(0)]
        public byte PsuState
        {
            get { return this.psuState; }
            set { this.psuState = value; }
        }
    }

    /// <summary>
    /// Represents the generic Psu byte response message
    /// </summary>
    [ChassisMessageResponse((byte)FunctionCode.PsuOperations)]
    internal class PsuCommandByteResponse : ChassisResponse
    {
        private byte psuByteResponse;

        [ChassisMessageData(0)]
        public byte PsuByteResponse
        {
            get { return this.psuByteResponse; }
            set { this.psuByteResponse = value; }
        }
    }

    #endregion

}
