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

namespace Microsoft.GFS.WCS.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Collections;

    // Define a service contract.
    [ServiceContract]
    public interface IChassisManager
    {
        // Create the method declaration for the contract.
        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "GetChassisInfo?bladeinfo={bladeInfo}&psuInfo={psuInfo}&chassisControllerInfo={chassisControllerInfo}&batteryInfo={batteryInfo}",
        BodyStyle = WebMessageBodyStyle.Bare)]
        ChassisInfoResponse GetChassisInfo(bool bladeInfo, bool psuInfo, bool chassisControllerInfo, bool batteryInfo);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeInfoResponse GetBladeInfo(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllBladesInfoResponse GetAllBladesInfo();

        [OperationContract]
        [WebGet]
        ChassisResponse SetChassisAttentionLEDOn();

        [OperationContract]
        [WebGet]
        ChassisResponse SetChassisAttentionLEDOff();

        [OperationContract]
        [WebGet]
        LedStatusResponse GetChassisAttentionLEDStatus();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeAttentionLEDOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesAttentionLEDOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeAttentionLEDOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesAttentionLEDOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeDefaultPowerStateOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesDefaultPowerStateOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeDefaultPowerStateOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesDefaultPowerStateOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeStateResponse GetBladeDefaultPowerState(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllBladesStateResponse GetAllBladesDefaultPowerState();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetPowerOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllPowerOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetPowerOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllPowerOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeActivePowerCycle(int bladeId, uint offTime);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesActivePowerCycle(uint offTime);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        PowerStateResponse GetPowerState(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllPowerStateResponse GetAllPowerState();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeStateResponse GetBladeState(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllBladesStateResponse GetAllBladesState();

        [OperationContract]
        [WebInvoke(Method = "GET")]
        ChassisResponse SetACSocketPowerStateOn(uint portNo);

        [OperationContract]
        [WebInvoke(Method = "GET")]
        ChassisResponse SetACSocketPowerStateOff(uint portNo);

        [OperationContract]
        [WebInvoke(Method = "GET")]
        ACSocketStateResponse GetACSocketPowerState(uint portNo);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        StartSerialResponse StartBladeSerialSession(int bladeId, int sessionTimeoutInSecs, bool powerOnWait = false);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse StopBladeSerialSession(int bladeId, string sessionToken, bool forceKill = false);
        
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse SendBladeSerialData(int bladeId, string sessionToken, byte[] data);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        SerialDataResponse ReceiveBladeSerialData(int bladeId, string sessionToken);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        StartSerialResponse StartSerialPortConsole(int portId, int sessionTimeoutInSecs, int deviceTimeoutInMsecs, int baudRate);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse StopSerialPortConsole(int portId, string sessionToken, bool forceKill);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse SendSerialPortData(int portId, string sessionToken, byte[] data);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        SerialDataResponse ReceiveSerialPortData(int portId, string sessionToken);

        [OperationContract]
        [WebGet]
        ChassisLogResponse ReadChassisLogWithTimestamp(DateTime startTimestamp, DateTime endTimestamp);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Bare)]
        ChassisLogResponse ReadChassisLog();

        [OperationContract]
        [WebGet(UriTemplate = "/ClearChassisLog")]
        ChassisResponse ClearChassisLog();

        [OperationContract]
        [WebGet]
        ChassisLogResponse ReadBladeLogWithTimestamp(int bladeId, DateTime startTimestamp, DateTime endTimestamp);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisLogResponse ReadBladeLog(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse ClearBladeLog(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladePowerReadingResponse GetBladePowerReading(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllBladesPowerReadingResponse GetAllBladesPowerReading();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladePowerLimitResponse GetBladePowerLimit(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        GetAllBladesPowerLimitResponse GetAllBladesPowerLimit();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladePowerLimit(int bladeId, double powerLimitInWatts);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesPowerLimit(double powerLimitInWatts);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladePowerLimitOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesPowerLimitOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladePowerLimitOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesPowerLimitOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisNetworkPropertiesResponse GetChassisNetworkProperties();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse AddChassisControllerUser(string userName, string passwordString, WCSSecurityRole role);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse RemoveChassisControllerUser(string userName);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse ChangeChassisControllerUserRole(string userName, WCSSecurityRole role);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse ChangeChassisControllerUserPassword(string userName, string newPassword);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisHealthResponse GetChassisHealth(bool bladeHealth, bool psuHealth, bool fanHealth, bool batteryHealth);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeHealthResponse GetBladeHealth(int bladeId, bool cpuInfo, bool memInfo, bool diskInfo, bool pcieInfo, bool sensorInfo, bool temp, bool fruInfo);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BootResponse GetNextBoot(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BootResponse SetNextBoot(int bladeId, BladeBootType bootType, bool uefi, bool persistent, int bootInstance);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Bare)]
        ServiceVersionResponse GetServiceVersion();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Bare)]
        MaxPwmResponse GetMaxPwmRequirement();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Bare)]
        ChassisResponse ResetPsu(int psuId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisAssetInfoResponse GetChassisManagerAssetInfo();
        
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisAssetInfoResponse GetPdbAssetInfo();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeAssetInfoResponse GetBladeAssetInfo(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        MultiRecordResponse SetChassisManagerAssetInfo(string payload);
        
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        MultiRecordResponse SetPdbAssetInfo(string payload);
        
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeMultiRecordResponse SetBladeAssetInfo(int bladeId, string payload);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladePsuAlertDefaultPowerCap(int bladeId, ushort defaultPowerCapInWatts, ushort waitTimeInMsecs);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesPsuAlertDefaultPowerCap(ushort defaultPowerCapInWatts, ushort waitTimeInMsecs);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladePsuAlertDpcResponse GetBladePsuAlertDefaultPowerCap(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesPsuAlertDpcResponse GetAllBladesPsuAlertDefaultPowerCap();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladePsuAlertResponse GetBladePsuAlert(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesPsuAlertResponse GetAllBladesPsuAlert();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladePsuAlert(int bladeId, bool enableProchot, int action, bool removeCap);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        AllBladesResponse SetAllBladesPsuAlert(bool enableProchot, int action, bool removeCap);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BiosPostCode GetPostCode(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladeResponse SetBladeDatasafeOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesResponse SetAllBladesDatasafeOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladeResponse SetBladeDatasafeOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesResponse SetAllBladesDatasafeOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladeResponse SetBladeDatasafePowerOff(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesResponse SetAllBladesDatasafePowerOff();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladeResponse SetBladeDatasafePowerOn(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesResponse SetAllBladesDatasafePowerOn();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladeResponse SetBladeDatasafeActivePowerCycle(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesResponse SetAllBladesDatasafeActivePowerCycle();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeBladePowerStateResponse GetBladeDatasafePowerState(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        DatasafeAllBladesPowerStateResponse GetAllBladesDatasafePowerState();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse UpdatePSUFirmware(int psuId, string fwFilepath, bool primaryImage);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        PsuFirmwareStatus GetPSUFirmwareStatus(int psuId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeMezzPassThroughModeResponse GetBladeMezzPassThroughMode(int bladeId);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeResponse SetBladeMezzPassThroughMode(int bladeId, string passThroughModeEnabled);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse GetSntpServer();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse RestoreSntpServer();

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ChassisResponse SetSntpServer(string primary, string secondary);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        BladeMessAssetInfoResponse GetBladeMezzAssetInfo(int bladeId);


        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        ScanDeviceResponse ScanDevice();
    }

    /// <summary>
    /// Boot type for blades. 
    /// The boot should follow soon (within one minute) after the boot type is set.
    /// </summary>
    public enum BladeBootType : int
    {
        Unknown = 0,
        NoOverride = 1,
        ForcePxe = 2,
        ForceDefaultHdd = 3,
        ForceIntoBiosSetup = 4,
        ForceFloppyOrRemovable = 5
    }


    /// <summary>
    /// Enumerates all the completioncode
    /// </summary>
    public enum CompletionCode : byte
    {
        // Common error codes
        Success = 0x0,
        Failure = 0xFF,
        Timeout = 0xA3,
        Unknown = 0xA4,
        ParameterOutOfRange = 0xA5,
        SerialSessionActive = 0xA6,
        UserAccountExists = 0xA7,
        UserPasswordDoesNotMeetRequirement = 0xA8,
        CommandNotValidForBlade = 0xA9,
        UserNotFound = 0xB0,
        DevicePoweredOff = 0xB1,
        NoActiveSerialSession = 0xB2,
        FanlessChassis = 0xB3,
        WriteFruZeroStartingOffset = 0xB4,
        WriteFruMaxFieldLengthReached = 0xB5,
        WriteFruMaxRecordSizeReached = 0xB6,
        WriteFruZeroWritesRemaining = 0xB7,

        // This response may be thrown for blade power commands (AC power and chipset power) 
        // during a pending datasafe backup operation. The request power command will be 
        // tried after the backup operation is complete (duration mentioned as part of the 
        // ‘DatasafeBladeResponse’ response packet.
        RequestDelayedDueToPendingDatasafeOperation = 0xB8,

        // This response will be thrown for blade power commands (AC power and chipset power)
        // during a pending datasafe backup operation and when battery is discharging and 
        // in critical charge level. This is to ensure that the datasafe operation is uninterrupted
        // and that the blade remains powered off even after the backup is complete (since AC
        // has not returned yet.
        RequestFailedDueToPendingDatasafeOperation = 0xB9,

        CommandNotValidAtThisTime = 0xBF,

        // PSU firmware update in progress
        PSUFirmwareUpdateInProgress = 0xC0,

        // BMC Receive Serial Buffer Overflow
        BmcRxSerialBufferOverflow = 0xC1,

        // Maximum Serial Sessions Reached
        MaxSerialSessionReached = 0xC2,

        /// <summary>
        /// Blade BMC Firmeware is decompressing
        /// </summary>
        FirmwareDecompressing = 0xEE,
    }

    public enum PowerState : byte
    {
        // power state codes
        ON = 0x1,
        OFF = 0x0,
        OnFwDecompress = 0x2,
        NA = 0x3,
    }

    public enum WCSSecurityRole : int
    {
        // WCS Roles
        WcsCmAdmin = 2,
        WcsCmOperator = 1,
        WcsCmUser = 0
    }

    public enum LedState : byte
    {
        // power state codes
        ON = 0x1,
        OFF = 0x0,
        NA = 0x3,
    }

    [DataContract]
    public class SerialDataResponse : ChassisResponse
    {
        [DataMember]
        public byte[] data = new byte[] { };
    }

    /// <summary>
    /// Structure for GetLedStatus
    /// </summary>	
    [DataContract]
    public class LedStatusResponse : ChassisResponse
    {
        [DataMember]
        public LedState ledState = LedState.NA;
    }

    [DataContract]
    public class ChassisNetworkPropertiesResponse : ChassisResponse
    {
        [DataMember]
        public List<ChassisNetworkProperty> chassisNetworkPropertyCollection = new List<ChassisNetworkProperty>();
    }

    [DataContract]
    public class ChassisNetworkProperty : ChassisResponse, IComparable<ChassisNetworkProperty>
    {
        [DataMember(Order = 0)]
        public string macAddress = String.Empty;
        [DataMember(Order = 1)]
        public string ipAddress = String.Empty;
        [DataMember(Order = 2)]
        public string subnetMask = String.Empty;
        [DataMember(Order = 3)]
        public string gatewayAddress = String.Empty;
        [DataMember(Order = 4)]
        public string dnsAddress = String.Empty;
        [DataMember(Order = 5)]
        public string dhcpServer = String.Empty;
        [DataMember(Order = 6)]
        public string dnsDomain = String.Empty;
        [DataMember(Order = 7)]
        public string dnsHostName = String.Empty;      
        [DataMember(Order = 8)]
        public bool dhcpEnabled;
        // None data member.
        internal int PhysicalIndex = -1;

        /// <summary>
        /// Implement IComparable.CompareTo(b) based on Physical Index
        /// </summary>
        public int CompareTo(ChassisNetworkProperty b)
        {
           return this.PhysicalIndex.CompareTo(b.PhysicalIndex);
        }
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class PowerStateResponse : BladeResponse
    {
        [DataMember(Order = 0)]
        public int Decompression = 0;

        [DataMember(Order = 1)]
        public PowerState powerState = PowerState.NA;
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class GetAllPowerStateResponse : ChassisResponse
    {
        [DataMember]
        public List<PowerStateResponse> powerStateResponseCollection = new List<PowerStateResponse>();
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class BladeStateResponse : BladeResponse
    {
        [DataMember]
        public PowerState bladeState = PowerState.NA;
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class GetAllBladesStateResponse : ChassisResponse
    {
        [DataMember]
        public List<BladeStateResponse> bladeStateResponseCollection = new List<BladeStateResponse>();
    }

    /// <summary>
    /// Structure for GetBladePowerReading
    /// </summary>	
    [DataContract]
    public class BladePowerReadingResponse : BladeResponse
    {
        [DataMember]
        public double powerReading;
    }

    /// <summary>
    /// Structure for GetBladePowerLimit
    /// </summary>	
    [DataContract]
    public class BladePowerLimitResponse : BladeResponse
    {
        [DataMember]
        public double powerLimit;

        [DataMember]
        public bool isPowerLimitActive;
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class GetAllBladesPowerReadingResponse : ChassisResponse
    {
        [DataMember]
        public List<BladePowerReadingResponse> bladePowerReadingCollection = new List<BladePowerReadingResponse>();
    }

    /// <summary>
    /// </summary>	
    [DataContract]
    public class GetAllBladesPowerLimitResponse : ChassisResponse
    {
        [DataMember]
        public List<BladePowerLimitResponse> bladePowerLimitCollection = new List<BladePowerLimitResponse>();
    }

    /// <summary>
    /// Structure for getpowerintstate
    /// </summary>	
    [DataContract]
    public class ACSocketStateResponse : ChassisResponse
    {
        [DataMember(Order = 0)]
        public uint portNo;

        [DataMember(Order = 1)]
        public PowerState powerState = PowerState.NA;

    }

    /// <summary>
    /// clrBladelog, clrnclog, ncidon, ncidoff, setBladeponstate, powerinton, powerintoff
    /// </summary>
    [DataContract]
    public class ChassisResponse
    {
        [DataMember(Order = 0)]
        public CompletionCode completionCode = CompletionCode.Unknown;

        [DataMember(Order = 1)]
        public int apiVersion = 1;

        [DataMember(Order = 2)]
        public string statusDescription = String.Empty;

    }

    /// <summary>
    /// Structure for Bladeidon, Bladeidoff, poweron, poweroff, powercycle
    /// </summary>
    [DataContract]
    public class BladeResponse : ChassisResponse
    {
        [DataMember]
        public int bladeNumber;
    }

    /// <summary>
    /// </summary>
    [DataContract]
    public class AllBladesResponse : ChassisResponse
    {
        [DataMember]
        public List<BladeResponse> bladeResponseCollection = new List<BladeResponse>();
    }

    public class GetAllBladesInfoResponse : ChassisResponse
    {
        [DataMember]
        public List<BladeInfoResponse> bladeInfoResponseCollection = new List<BladeInfoResponse>();
    }

    /// <summary>
    /// Response to BladeInfo
    /// </summary>
    [DataContract]
    public class BladeInfo : BladeResponse
    {
        /// <summary>
        /// blade Guid
        /// </summary>
        [DataMember(Order = 0)]
        public Guid bladeGuid = new Guid();

        /// <summary>
        /// blade NAme
        /// </summary>
        [DataMember(Order = 1)]
        public string bladeName = String.Empty;

        /// <summary>
        /// blade Power State (On/Off)
        /// </summary>
        [DataMember(Order = 2)]
        public PowerState powerState = PowerState.NA;

        /// <summary>
        /// blade MAC Addresses
        /// </summary>
        [DataMember(Order = 3)]
        public List<NicInfo> bladeMacAddress = new List<NicInfo>();
    }

    /// <summary>
    /// This class defines the response for PSU information.
    /// </summary>
    [DataContract]
    public class PsuInfo : ChassisResponse
    {
        /// <summary>
        /// PSU ID
        /// </summary>
        [DataMember(Order = 0)]
        public uint id;

        /// <summary>
        /// PSU Serial Number
        /// </summary>
        [DataMember(Order = 1)]
        public string serialNumber = String.Empty;

        /// <summary>
        /// PSU State
        /// </summary>
        [DataMember(Order = 3)]
        public PowerState state = PowerState.NA;

        /// <summary>
        /// PSU Power Reading 
        /// </summary>
        [DataMember(Order = 4)]
        public uint powerOut;
    }

    /// <summary>
    /// This class defines the response for battery information.
    /// </summary>
    [DataContract]
    public class BatteryInfo : ChassisResponse
    {
        /// <summary>
        /// Battery ID
        /// </summary>
        [DataMember(Order = 0)]
        public byte id;

        /// <summary>
        /// Battery Presence.
        /// 0 - No battery present
        /// 1 - Battery present
        /// </summary>
        [DataMember(Order = 1)]
        public byte presence;

        /// <summary>
        /// The battery power output
        /// </summary>
        [DataMember(Order = 2)]
        public double batteryPowerOutput;

        /// <summary>
        /// The battery charge level
        /// </summary>
        [DataMember(Order = 3)]
        public double batteryChargeLevel;

        /// <summary>
        /// The fault detected
        /// </summary>
        [DataMember(Order = 4)]
        public byte faultDetected;
    }

    /// <summary>
    /// This class defines the response for PSU firmware status.
    /// </summary>
    [DataContract]
    public class PsuFirmwareStatus : ChassisResponse
    {
        /// <summary>
        /// Firmware revision. Format is aa.bb.cc, where aa is development stage (D0, P0, P1 … A0); 
        /// bb is primary MCU firmware revision (0 ~ 99); cc is secondary MCU firmware revision (0 ~ 99).
        /// </summary>
        [DataMember(Order = 0)]
        public string fwRevision;

        /// <summary>
        /// Overall firmware update status
        /// </summary>
        [DataMember(Order = 1)]
        public string fwUpdateStatus;

        /// <summary>
        /// Current stage of the firmware update
        /// </summary>
        [DataMember(Order = 2)]
        public string fwUpdateStage;
    }

    /// <summary>
    /// This class defines the response for chassis controller. 
    /// </summary>
    [DataContract]
    public class ChassisControllerInfo : ChassisResponse
    {
        /// <summary>
        /// Chassis Serial Number
        /// </summary>
        [DataMember(Order = 0)]
        public string serialNumber = String.Empty;

        /// <summary>
        /// Chassis Asset Tag
        /// </summary>
        [DataMember(Order = 1)]
        public string assetTag = String.Empty;    
        
        /// <summary>
        /// Chassis Firmware Version
        /// </summary>
        [DataMember(Order = 2)]
        public string firmwareVersion = String.Empty;

        /// <summary>
        /// Chassis Hardware Version
        /// </summary>
        [DataMember(Order = 3)]
        public string hardwareVersion = String.Empty;

        /// <summary>
        /// Chassis Hardware Version
        /// </summary>
        [DataMember(Order = 4)]
        public string softwareVersion = String.Empty;      

        /// <summary>
        /// Time for which the Chassis manager is active.
        /// </summary>
        [DataMember(Order = 5)]
        public string systemUptime = String.Empty;

        /// <summary>
        /// Details about CM network interfaces
        /// </summary>
        [DataMember(Order = 6)]
        public ChassisNetworkPropertiesResponse networkProperties = new ChassisNetworkPropertiesResponse();
    }

    /// <summary>
    /// This class defines response to get info command,
    /// includes information on blade, PSU, batteries and chassis controller
    /// </summary>
    [DataContract]
    public class ChassisInfoResponse : ChassisResponse
    {
        /// <summary>
        /// Chassis controller
        /// </summary>
        [DataMember(Order = 0)]
        public ChassisControllerInfo chassisController = new ChassisControllerInfo();

        /// <summary>
        /// PSU object collection
        /// </summary>
        [DataMember(Order = 1)]
        public List<PsuInfo> psuCollections = new List<PsuInfo>();

        /// <summary>
        /// Blade object collection
        /// </summary>
        [DataMember(Order = 2)]
        public List<BladeInfo> bladeCollections = new List<BladeInfo>();

        /// <summary>
        /// Battery object collection
        /// </summary>
        [DataMember(Order = 3)]
        public List<BatteryInfo> batteryCollections = new List<BatteryInfo>();
    }

    /// <summary>
    /// This class defines response to getBladeinfo command,
    /// collection of blade
    /// </summary>
    [DataContract]
    public class BladeInfoResponse : BladeResponse
    {
        /// <summary>
        /// Blade Type
        /// </summary>
        [DataMember(Order = 0)]
        public String bladeType = String.Empty;

        /// <summary>
        /// Blade Baseboard Serial Number
        /// </summary>
        [DataMember(Order = 1)]
        public string serialNumber = String.Empty;

        /// <summary>
        /// Blade Asset Tag
        /// </summary>
        [DataMember(Order = 2)]
        public string assetTag = String.Empty;

        /// <summary>
        /// Blade Firmware Version
        /// </summary>
        [DataMember(Order = 3)]
        public string firmwareVersion = String.Empty;

        /// <summary>
        /// Blade Hardware Version
        /// </summary>
        [DataMember(Order = 4)]
        public string hardwareVersion = String.Empty;
   
        /// <summary>
        /// Blade MAC Address
        /// </summary>
        [DataMember(Order = 5)]
        public List<NicInfo> macAddress = new List<NicInfo>();
    }

    /// <summary>
    /// This defines the response to readBladelog
    /// </summary>
    [DataContract]
    public class LogResponse : ChassisResponse
    {
        /// <summary>
        /// Event Time collection
        /// </summary>
        [DataMember(Order = 0)]
        public DateTime[] eventTime = new DateTime[] { };

        /// <summary>
        /// Even Description collection
        /// </summary>
        [DataMember(Order = 1)]
        public string[] eventDescription = new string[] { };
    }

    /// <summary>
    /// This defines the response to readChassislog
    /// </summary>
    [DataContract]
    public class ChassisLogResponse : ChassisResponse
    {
        /// <summary>
        /// Event Time collection
        /// </summary>
        [DataMember]
        public List<LogEntry> logEntries = new List<LogEntry>();
    }

    /// <summary>
    /// This class defines log data format
    /// </summary>
    [DataContract]
    public class LogEntry
    {
        [DataMember(Order = 0)]
        public DateTime eventTime = new DateTime();

        [DataMember(Order = 1)]
        public string eventDescription = string.Empty;
    }

    /// <summary>
    /// This class defines the response packet structure for StartBladeSerialSession
    /// </summary>
    [DataContract]
    public class StartSerialResponse : ChassisResponse
    {
        [DataMember]
        public string serialSessionToken = String.Empty;
    }

    /// <summary>
    /// Class structure to capture Health information for entire chassis
    /// </summary>
    [DataContract]
    public class ChassisHealthResponse : ChassisResponse
    {
        [DataMember(Order = 0)]
        public List<BladeShellResponse> bladeShellCollection = new List<BladeShellResponse>();

        [DataMember(Order = 1)]
        public List<FanInfo> fanInfoCollection = new List<FanInfo>();

        [DataMember(Order = 2)]
        public List<PsuInfo> psuInfoCollection = new List<PsuInfo>();

        [DataMember(Order = 3)]
        public List<BatteryInfo> batteryInfoCollection = new List<BatteryInfo>();
    }

    /// <summary>
    /// Contains Blade shell information including Blade Id, Blade Type, and Blade Health Status
    /// </summary>
    [DataContract]
    public class BladeShellResponse : BladeResponse
    {
        [DataMember(Order = 0)]
        public String bladeType = String.Empty;

        [DataMember(Order = 1)]
        public String bladeState = String.Empty;
    }

    [DataContract]
    public class FanInfo : ChassisResponse
    {
        [DataMember(Order = 0)]
        public int fanId;

        [DataMember(Order = 1)]
        public bool isFanHealthy;

        [DataMember(Order = 2)]
        public int fanSpeed;

    }

    /// <summary>
    /// Class structure to capture Health information for each blade
    /// </summary>
    [DataContract]
    public class BladeHealthResponse : BladeResponse
    {
        /// <summary>
        /// Blade Baseboard Serial Number
        /// </summary>
        [DataMember(Order = 0)]
        public string serialNumber = String.Empty;

        /// <summary>
        /// Blade Asset Tag
        /// </summary>
        [DataMember(Order = 1)]
        public string assetTag = String.Empty;

        /// <summary>
        /// Blade Hardware Version
        /// </summary>
        [DataMember(Order = 2)]
        public string hardwareVersion = String.Empty;

        /// <summary>
        /// Product Type
        /// </summary>
        [DataMember(Order = 3)]
        public string productType = String.Empty;   

        [DataMember(Order = 4)]
        public BladeShellResponse bladeShell = new BladeShellResponse();

        [DataMember(Order = 5)]
        public List<ProcessorInfo> processorInfo = new List<ProcessorInfo>();

        [DataMember(Order = 6)]
        public List<MemoryInfo> memoryInfo = new List<MemoryInfo>();

        [DataMember(Order = 7)]
        public List<PCIeInfo> pcieInfo = new List<PCIeInfo>();

        [DataMember(Order = 8)]
        public List<SensorInfo> sensors = new List<SensorInfo>();

        [DataMember(Order = 9)]
        public JbodInfo jbodInfo;

        [DataMember(Order = 10)]
        public JbodDiskStatus jbodDiskInfo;
    
    }

    /// <summary>
    /// Processor information for blades
    /// </summary>
    [DataContract]
    public class ProcessorInfo : ChassisResponse
    {
        public ProcessorInfo()
        { }

        public ProcessorInfo(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public ProcessorInfo(CompletionCode completionCode, byte procId, string type, string state, ushort frequency)
            : this(completionCode)
        {
            this.procId = procId;
            this.procType = type;
            this.state = state;
            this.frequency = frequency;
        }

        // Processor Type
        [DataMember(Order = 0)]
        public byte procId;

        // Processor Type
        [DataMember(Order = 1)]
        public string procType = String.Empty;

        // Processor state
        [DataMember(Order = 2)]
        public string state = String.Empty;

        // processor frequency
        [DataMember(Order = 3)]
        public ushort frequency;
    }

    /// <summary>
    /// Memory information for blades
    /// </summary>
    [DataContract]
    public class MemoryInfo : ChassisResponse
    {
        public MemoryInfo()
        {
        }
        public MemoryInfo(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public MemoryInfo(CompletionCode completionCode, byte dimm, string type, ushort speed,
            ushort capacity, string memVoltage, string status)
            : this(completionCode)
        {
            this.dimm = dimm;

            this.dimmType = type;

            // Memory Speed
            this.speed = speed;

            // Dimm Size
            this.size = capacity;

            this.memVoltage = memVoltage;

            this.status = status;
        }

        // Memory Type
        [DataMember(Order = 0)]
        public byte dimm;

        // Memory Type
        [DataMember(Order = 1)]
        public string dimmType = String.Empty;

        // Status
        [DataMember(Order = 2)]
        public string status = String.Empty;

        // Memory Speed
        [DataMember(Order = 3)]
        public ushort speed;

        // Dimm Size
        [DataMember(Order = 4)]
        public ushort size;

        // Memory Voltage
        [DataMember(Order = 5)]
        public string memVoltage = String.Empty;        
    }

    /// <summary>
    /// PCIe Info for Blades
    /// </summary>
    [DataContract]
    public class PCIeInfo : ChassisResponse
    {
        public PCIeInfo() { }
        public PCIeInfo(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public PCIeInfo(CompletionCode completionCode, byte number, string state, string vendorId, string deviceId, string subSystemId)
            : this(completionCode)
        {
            this.status = state;
            this.pcieNumber = number;
            this.vendorId = vendorId;
            this.deviceId = deviceId;
            this.subSystemId = subSystemId;
        }

        [DataMember(Order = 0)]
        public byte pcieNumber;

        [DataMember(Order = 1)]
        public string vendorId;

        [DataMember(Order = 2)]
        public string deviceId;

        [DataMember(Order = 3)]
        public string subSystemId;

        [DataMember(Order = 4)]
        public string status = string.Empty;
    }

    /// <summary>
    /// Entire JBOD information
    /// </summary>
    [DataContract]
    public class JbodDiskStatus : ChassisResponse
    {
        public JbodDiskStatus() { }
        public JbodDiskStatus(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public JbodDiskStatus(CompletionCode completionCode, byte channel, byte diskCount)
            : this(completionCode)
        {
            this.channel = channel;
            this.diskCount = diskCount;
        }

        [DataMember(Order = 0)]
        public byte channel;

        [DataMember(Order = 1)]
        public byte diskCount;

        [DataMember(Order = 2)]
        public List<DiskInfo> diskInfo = new List<DiskInfo>();
    }

    /// <summary>
    /// Properties in the Get JBOD Disk Status command response
    /// </summary>
    public class JbodInfo : ChassisResponse
    {
        /// <summary>
        /// Properties in the Get Disk information command response
        /// </summary>
        public JbodInfo(CompletionCode completionCode)
        {
            this.completionCode = completionCode;
        }

        /// <summary>
        /// Properties in the Get Disk information command response
        /// </summary>
        public JbodInfo(CompletionCode completionCode, string unit, string reading)
            : this(completionCode)
        {
            this.unit = unit;
            this.reading = reading;
        }

        /// <summary>
        /// Properties in the Get Disk information command response
        /// </summary>
        public JbodInfo()
        {
        }

        /// <summary>
        /// JBOD Disk Channel
        /// </summary>
        [DataMember(Order = 0)]
        public string unit = String.Empty;

        /// <summary>
        /// JBOD Disk Count
        /// </summary>
        [DataMember(Order = 1)]
        public string reading = String.Empty;

    }

    /// <summary>
    /// Hardware Sensor information class
    /// </summary>
    [DataContract]
    public class SensorInfo : ChassisResponse
    {
        public SensorInfo() { }
        public SensorInfo(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public SensorInfo(CompletionCode completionCode, byte sensor, string type, string entityId, string entityInstance,
            string entityType, string reading, string status, string description)
            : this(completionCode)
        {

            this.sensorNumber = sensor;
            this.reading = reading == null ? string.Empty : reading;
            this.entityId = entityId == null ? string.Empty : entityId;
            this.sensorType = type == null ? string.Empty : type;
            this.entityInstance = entityInstance == null ? string.Empty : entityInstance;
            this.entityType = entityType == null ? string.Empty : entityType;
            this.status = status == null ? string.Empty : status;
            this.description = description == null ? string.Empty : description;
        }

        public SensorInfo(CompletionCode completionCode, byte sensor, string type, string reading,
            string status, string description)
            : this(completionCode)
        {
            this.sensorNumber = sensor;
            this.reading = reading == null ? string.Empty : reading;
            this.entityId = entityId == null ? string.Empty : entityId;
            this.sensorType = type == null ? string.Empty : type;
            this.entityInstance = entityInstance == null ? string.Empty : entityInstance;
            this.status = status == null ? string.Empty : status;
            this.description = description == null ? string.Empty : description;
        }

        [DataMember(Order = 0)]
        public byte sensorNumber;

        [DataMember(Order=1)]
        public string sensorType = String.Empty;

        [DataMember(Order = 2)]
        public string status = String.Empty;

        [DataMember(Order=3)]
        public string entityId = String.Empty;

        [DataMember(Order = 4)]
        public string entityInstance = String.Empty;

        [DataMember(Order = 5)]
        public string entityType = String.Empty;

        [DataMember(Order=6)]
        public string reading = String.Empty;

        [DataMember(Order=7)]
        public string description = String.Empty;
    }

    /// <summary>
    /// Disk information for JBOD class
    /// </summary>
    [DataContract]
    public class DiskInfo : ChassisResponse
    {
        public DiskInfo() { }
        public DiskInfo(CompletionCode completionCode)
        { this.completionCode = completionCode; }

        public DiskInfo(CompletionCode completionCode, byte disk, string status)
            : this(completionCode)
        {
            this.diskId = disk;
            this.diskStatus = status;
        }

        [DataMember(Order = 0)]
        public byte diskId;

        [DataMember(Order = 1)]
        public string diskStatus = String.Empty;
    }

    /// <summary>
    /// Nic Information
    /// </summary>
    [DataContract]
    public class NicInfo : ChassisResponse
    {
        [DataMember(Order = 0)]
        public int deviceId;

        [DataMember(Order = 1)]
        public string macAddress = String.Empty;
    }

    [DataContract]
    public class BootResponse : BladeResponse
    {
        [DataMember]
        public BladeBootType nextBoot = BladeBootType.Unknown;
    }

    /// <summary>
    /// Service version information
    /// </summary>
    [DataContract]
    public class ServiceVersionResponse : ChassisResponse
    {
        [DataMember]
        public string serviceVersion;
    }

    /// <summary>
    /// Returns Max Blade PWM Requirement
    /// </summary>
    [DataContract]
    public class MaxPwmResponse : ChassisResponse
    {
        [DataMember]
        public byte maxPwmRequirement;
    }

    /// <summary>
    /// CM FRU areas read/write response
    /// </summary>
    [DataContract]
    public class ChassisAssetInfoResponse : ChassisResponse
    {
        /// <summary>
        /// Chassis Area Part Number
        /// </summary>
        [DataMember(Order = 0)]
        public string chassisAreaPartNumber = string.Empty;

        /// <summary>
        /// Chassis Area Serial Number
        /// </summary>
        [DataMember(Order = 1)]
        public string chassisAreaSerialNumber = string.Empty;

        /// <summary>
        /// Board Area Manufacturer Name
        /// </summary>
        [DataMember(Order = 2)]
        public string boardAreaManufacturerName = string.Empty;

        /// <summary>
        /// Board Area Manufacturer Date
        /// </summary>
        [DataMember(Order = 3)]
        public string boardAreaManufacturerDate = string.Empty;

        /// <summary>
        /// Board Area Product Name
        /// </summary>
        [DataMember(Order = 4)]
        public string boardAreaProductName = string.Empty;

        /// <summary>
        /// Board Area Serial Number
        /// </summary>
        [DataMember(Order = 5)]
        public string boardAreaSerialNumber = string.Empty;

        /// <summary>
        /// Board Area Part Number
        /// </summary>
        [DataMember(Order = 6)]
        public string boardAreaPartNumber = string.Empty;

        /// <summary>
        /// Product Area Manufacturer Name
        /// </summary>
        [DataMember(Order = 7)]
        public string productAreaManufactureName = string.Empty;

        /// <summary>
        /// Product Area Product Name
        /// </summary>
        [DataMember(Order = 8)]
        public string productAreaProductName = string.Empty;

        /// <summary>
        /// Product Area Part/Model Number
        /// </summary>
        [DataMember(Order = 9)]
        public string productAreaPartModelNumber = string.Empty;

        /// <summary>
        /// Product Area Product Version
        /// </summary>
        [DataMember(Order = 10)]
        public string productAreaProductVersion = string.Empty;
        
        /// <summary>
        /// Product Area Serial Number
        /// </summary>
        [DataMember(Order = 11)]
        public string productAreaSerialNumber = string.Empty;
        
        /// <summary>
        /// Product Area Asset Tag
        /// </summary>
        [DataMember(Order = 12)]
        public string productAreaAssetTag = string.Empty;
        
        /// <summary>
        /// Multi Record Area Manufacturer
        /// </summary>
        [DataMember(Order = 13)]
        public string manufacturer = string.Empty;

        /// <summary>
        /// Chassis Manager Service Version
        /// </summary>
        [DataMember(Order = 14)]
        public string serviceVersion = string.Empty;
        
        /// <summary>
        /// Multi Record Area Custom Fields
        /// </summary>
        [DataMember(Order = 15)]
        public List<string> multiRecordFields = new List<string>();
    }

    /// <summary>
    /// Blade FRU areas read/write response
    /// </summary>
    [DataContract]
    public class BladeAssetInfoResponse : BladeResponse
    {
        /// <summary>
        /// Chassis Area Part Number
        /// </summary>
        [DataMember(Order = 0)]
        public string chassisAreaPartNumber = String.Empty;

        /// <summary>
        /// Chassis Area Serial Number
        /// </summary>
        [DataMember(Order = 1)]
        public string chassisAreaSerialNumber = String.Empty;

        /// <summary>
        /// Board Area Manufacturer Name
        /// </summary>
        [DataMember(Order = 2)]
        public string boardAreaManufacturerName = String.Empty;

        /// <summary>
        /// Board Area Manufacturer Date
        /// </summary>
        [DataMember(Order = 3)]
        public string boardAreaManufacturerDate = String.Empty;

        /// <summary>
        /// Board Area Product Name
        /// </summary>
        [DataMember(Order = 4)]
        public string boardAreaProductName = String.Empty;

        /// <summary>
        /// Board Area Serial Number
        /// </summary>
        [DataMember(Order = 5)]
        public string boardAreaSerialNumber = String.Empty;

        /// <summary>
        /// Board Area Part Number
        /// </summary>
        [DataMember(Order = 6)]
        public string boardAreaPartNumber = String.Empty;

        /// <summary>
        /// Product Area Manufacturer Name
        /// </summary>
        [DataMember(Order = 7)]
        public string productAreaManufactureName = String.Empty;

        /// <summary>
        /// Product Area Product Name
        /// </summary>
        [DataMember(Order = 8)]
        public string productAreaProductName = String.Empty;

    /// <summary>
        /// Product Area Part/Model Number
        /// </summary>
        [DataMember(Order = 9)]
        public string productAreaPartModelNumber = String.Empty;

        /// <summary>
        /// Product Area Product Version
        /// </summary>
        [DataMember(Order = 10)]
        public string productAreaProductVersion = String.Empty;

        /// <summary>
        /// Product Area Serial Number
        /// </summary>
        [DataMember(Order = 11)]
        public string productAreaSerialNumber = String.Empty;

        /// <summary>
        /// Product Area Asset Tag
        /// </summary>
        [DataMember(Order = 12)]
        public string productAreaAssetTag = String.Empty;

        /// <summary>
        /// Multi Record Area Manufacturer
        /// </summary>
        [DataMember(Order = 13)]
        public string manufacturer = String.Empty;

        /// <summary>
        /// Multi Record Area Custom Fields
        /// </summary>
        [DataMember(Order = 14)]
        public List<string> multiRecordFields = new List<string>();
    }

    /// <summary>
    /// CM and PDB FRU Multi Record Write response
    /// </summary>
    [DataContract]
    public class MultiRecordResponse : ChassisResponse
    {
        /// <summary>
        /// Multi Record Area Manufacturer
        /// </summary>
        [DataMember(Order = 0)]
        public string manufacturer = String.Empty;

        /// <summary>
        /// Multi Record Area Custom Fields
        /// </summary>
        [DataMember(Order = 1)]
        public List<string> multiRecordFields = new List<string>();
    }

    /// <summary>
    /// Blade FRU Multi Record Write response
    /// </summary>
    [DataContract]
    public class BladeMultiRecordResponse : BladeResponse
    {
        /// <summary>
        /// Multi Record Area Manufacturer
        /// </summary>
        [DataMember(Order = 0)]
        public string manufacturer = String.Empty;

        /// <summary>
        /// Multi Record Area Custom Fields
        /// </summary>
        [DataMember(Order = 1)]
        public List<string> multiRecordFields = new List<string>();
    }

    [DataContract]
    public class AllBladesPsuAlertResponse : ChassisResponse
    {
        [DataMember(Order = 0)]
        public List<BladePsuAlertResponse> bladePsuAlertCollection = new List<BladePsuAlertResponse>();
    }

    [DataContract]
    public class BladePsuAlertResponse : BladeResponse
    {
        /// <summary>
        /// PSU_Alert BMC GPI Status
        /// [7:6] BLADE_EN2 to BMC GPI
        /// </summary>
        [DataMember(Order = 0)]
        public bool PsuAlertGpi;

        /// <summary>
        /// Auto PROCHOT on switch GPI
        /// [5:4] Auto FAST_PROCHOT Enabled
        /// </summary>
        [DataMember(Order = 1)]
        public bool AutoProchotEnabled;

        /// <summary>
        /// BMC PROCHOT on switch GPI
        /// [3:0] BMC FAST_PROCHOT Enabled
        /// </summary>
        [DataMember(Order = 2)]
        public bool BmcProchotEnabled;
    }

    [DataContract]
    public class AllBladesPsuAlertDpcResponse : ChassisResponse
    {
        [DataMember(Order = 0)]
        public List<BladePsuAlertDpcResponse> bladeDpcResponseCollection = new List<BladePsuAlertDpcResponse>();
    }

    [DataContract]
    public class BladePsuAlertDpcResponse : BladeResponse
    {
        /// <summary>
        /// Default Power Cap in Watts
        /// </summary>       
        [DataMember(Order = 0)]
        public ushort DefaultPowerCap;

        /// <summary>
        ///  Time in milliseconds after applying DPC to 
        ///  wait before deasserting the PROCHOT
        /// </summary>
        [DataMember(Order = 1)]
        public ushort WaitTime;

        /// <summary>
        ///  Disable/Enable Default Power Cap on PSU_Alert GPI.
        /// </summary>  
        [DataMember(Order = 2)]
        public bool DefaultCapEnabled;
    }

        [DataContract]
    public class BiosPostCode : BladeResponse
    {
        /// <summary>
        /// Provides the BIOS POST Code for
        /// the current boot
        /// </summary>       
        [DataMember(Order = 0)]
        public string PostCode = string.Empty;

        /// <summary>
        /// Provides the BIOS POST Code on the
        /// boot before the current boot.
        /// </summary>
        [DataMember(Order = 1)]
        public string PreviousPostCode = string.Empty;
    }

    [DataContract]
    public class DatasafeBladeResponse : BladeResponse
    {
        [DataMember]
        public int RemainingDataSafeDurationInSecs = 0;
    }

    [DataContract]
    public class DatasafeAllBladesResponse : ChassisResponse
    {
        [DataMember]
        public List<DatasafeBladeResponse> datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
    }

    [DataContract]
    public class DatasafeBladePowerStateResponse : DatasafeBladeResponse
    {
        [DataMember]
        public bool isDatasafeBackupInProgress = false;
        [DataMember]
        public PowerState bladePowerState = PowerState.NA;
    }

    [DataContract]
    public class DatasafeAllBladesPowerStateResponse : ChassisResponse
    {
        [DataMember]
        public List<DatasafeBladePowerStateResponse> datasafeBladePowerStateResponseCollection = new List<DatasafeBladePowerStateResponse>();
    }

    [DataContract]
    public class BladeMezzPassThroughModeResponse : BladeResponse
    {
        [DataMember]
        public bool passThroughModeEnabled = false; 
    }

    [DataContract]
    public class SntpServerResponse : ChassisResponse
    {
        [DataMember]
        public string SntpServer = string.Empty;
    }

    [DataContract]
    public class ScanObject : ChassisResponse
    {
        [DataMember(Order = 0)]
        public string Attribute = string.Empty;

        [DataMember(Order = 1)]
        public string Value = string.Empty;
    }

    [DataContract]
    public class ScanServices : ChassisResponse
    {
        [DataMember(Order = 0)]
        public string ServiceName = string.Empty;

        [DataMember(Order = 1)]
        public string StartUpType = string.Empty;

        [DataMember(Order = 2)]
        public string CurrentState = string.Empty;

        [DataMember(Order = 3)]
        public string Detail = string.Empty;
    }

    [DataContract]
    public class ChassisManagerServiceProperties : ChassisResponse
    {
        [DataMember(Order = 0)]
        public ServiceVersionResponse ServiceVersion;

        [DataMember(Order = 1)]
        public Dictionary<string, string> ServiceAppConfigSettings = new Dictionary<string,string>();
    }

    [DataContract]
    public class UserGroup : ChassisResponse
    {
        [DataMember(Order= 0)]
        public string Group = string.Empty;

        [DataMember(Order= 1)]
        public List<string> Members = new List<string>();
    }

    [DataContract]
    public class ScanDeviceResponse: ChassisResponse
    {
        /// <summary>
        /// Generic Object
        /// </summary>
        [DataMember(Order = 0)]
        public List<ScanObject> ScanObjects = new List<ScanObject>();       

        /// <summary>
        /// List of services
        /// </summary>
        [DataMember(Order = 1)]
        public List<ScanServices> Services = new List<ScanServices>();

        /// <summary>
        /// User Accounts
        /// </summary>
        [DataMember(Order = 2)]
        public List<string> Users = new List<string>();

        /// <summary>
        /// groups and members
        /// </summary>
        [DataMember(Order = 3)]
        public List<UserGroup> UserGroups = new List<UserGroup>();

        /// <summary>
        /// Get Chassis FRU data
        /// </summary>
        [DataMember(Order = 4)]
        public ChassisAssetInfoResponse ChassisFRU = new ChassisAssetInfoResponse();

        /// <summary>
        /// Get Chassis network settings
        /// </summary>
        [DataMember(Order = 5)]
        public ChassisNetworkPropertiesResponse ChassisNetworkSettings = new ChassisNetworkPropertiesResponse();

        /// <summary>
        /// Get Chassis Manager service properties
        /// </summary>
        [DataMember(Order = 6)]
        public ChassisManagerServiceProperties ChassisManagerServiceProperties = new ChassisManagerServiceProperties();

        /// <summary>
        /// Get Wcscli version
        /// </summary>
        [DataMember(Order = 7)]
        public string WCSCLIVersion = string.Empty;

    }

    [DataContract]
    public class BladeMessAssetInfoResponse : BladeResponse
    {
        /// <summary>
        /// Product Area Manufacturer Name
        /// </summary>
        [DataMember(Order = 1)]
        public string productAreaManufactureName = String.Empty;

        /// <summary>
        /// Product Area Product Name
        /// </summary>
        [DataMember(Order = 2)]
        public string productAreaProductName = String.Empty;

        /// <summary>
        /// Product Area Part/Model Number
        /// </summary>
        [DataMember(Order = 3)]
        public string productAreaPartModelNumber = String.Empty;

        /// <summary>
        /// Product Area Product Version
        /// </summary>
        [DataMember(Order = 4)]
        public string productAreaProductVersion = String.Empty;

        /// <summary>
        /// Product Area Serial Number
        /// </summary>
        [DataMember(Order = 5)]
        public string productAreaSerialNumber = String.Empty;

        /// <summary>
        /// Product Area Asset Tag
        /// </summary>
        [DataMember(Order = 6)]
        public string productAreaAssetTag = String.Empty;
    }

}
