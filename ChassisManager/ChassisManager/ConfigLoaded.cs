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

    using Config = System.Configuration.ConfigurationManager;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System.Xml;
    using System.IO;
    using System;

    static class ConfigLoaded
    {
        /// <summary>
        /// WCF WebOperationContext constant for cache control
        /// </summary>
        public const string CacheControl = "Cache-Control";

        /// <summary>
        /// WCF WebOperationContext constant for no caching
        /// </summary>
        public const string NoCache = "no-cache";


        /// <summary>
        /// Chassis Manager blade population count.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int Population;

        /// <summary>
        /// Maximum number of serial console sessions.
        /// </summary>
        internal static readonly uint MaxSerialConsoleSessions;

        /// <summary>
        /// Chassis Manager number of serial console ports (like TOR switches).  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int MaxSerialConsolePorts;       
       
        /// <summary>
        /// Multi Record Area FRU Manufacturer ID (IANA specification based with predefined value 311)
        /// </summary>
        internal static readonly byte[] MultiRecordFruManufacturerId = new byte[3];

        /// <summary>
        /// Option to reset Multi Record FRU writes remaining to default
        /// </summary>
        internal static bool ResetMultiRecordFruWritesRemaining;

        /// <summary>
        /// Serial Port Read/Write Timeout in ms.  Set & retrieved from the App.Config.
        /// </summary>
        internal static readonly int SerialTimeout;

        /// <summary>
        /// Gpio Master Chip consecutive error limit before the chip is reset.
        /// </summary>
        internal static readonly int GpioErrorLimit;

        /// <summary>
        /// Timeout for Serial Console Port - used by SerialPortConsoleMetadata class.  
        /// from the App.Config.
        /// </summary>
        internal static readonly int SerialPortConsoleClientSessionInactivityTimeoutInSecs;
        
        /// <summary>
        /// Timeout for Serial Console Port Device Communication send/receive calls - used by SerialPortConsoleMetadata class.  
        /// from the App.Config.
        /// </summary>
        internal static readonly int SerialPortConsoleDeviceCommunicationTimeoutInMsecs;

        /// <summary>
        /// Timeout for Blade serial session - used by BladeSerialSessionMetadata class.  
        /// from the App.Config.
        /// </summary>
        internal static readonly uint BladeSerialTimeout;

        /// <summary>
        /// BMC login user name.  Set & retrieved from 
        /// the App.Config.
        /// </summary>
        internal static string BmcUserName = string.Empty;

        /// <summary>
        /// BMC user key.  Set & retrieved from 
        /// the App.Config.
        /// </summary>
        internal static string BmcUserKey = string.Empty;

        /// <summary>
        /// BMC session inactivity time-out.  This setting increase the BMC session to time-out. 
        /// The value is calculated by increments of 30 seconds.  The default value is 6, which 
        /// corresponds to 3 minutes.
        /// </summary>
        internal static int BmcSessionTime = 6;

        /// <summary>
        /// Number of fans in chassis.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int NumFans;

        /// <summary>
        /// Number of PSUs in chassis.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int NumPsus;

        /// <summary>
        /// Number of batteries in chassis.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int NumBatteries;

        /// <summary>
        /// Number of Nics in Blades.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int NumNicsPerBlade;

        /// <summary>
        /// Number of power switches in chassis.  Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int NumPowerSwitches;

        /// <summary>
        /// Wait time after AC socket power off in milliseconds. Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int WaitTimeAfterACSocketPowerOffInMsecs;

        /// <summary>
        /// Wait time after Blade hard power off in milliseconds. Set & retrieved
        /// from the App.Config.
        /// </summary>
        internal static readonly int WaitTimeAfterBladeHardPowerOffInMsecs;

        /// <summary>
        /// Trace log file path
        /// </summary>
        internal static readonly string TraceLogFilePath;

        /// <summary>
        /// Trace log file size
        /// </summary>
        internal static readonly int TraceLogFileSize;

        /// <summary>
        /// User log file path
        /// </summary>
        internal static readonly string UserLogFilePath;

        /// <summary>
        /// User log file size
        /// </summary>
        internal static readonly int UserLogFileSize;

        /// <summary> 
        /// User log max returned entries 
        /// </summary> 
        internal static readonly int UserLogMaxEntries; 

        /// <summary>
        /// Maximum value for PWM setting (This should be equal to 100)
        /// </summary>
        internal static readonly int MaxPWM;

        /// <summary>
        /// Minimum value for PWM setting
        /// </summary>
        internal static readonly int MinPWM;

        /// <summary>
        /// Step value for PWM - ramp down policy
        /// </summary>
        internal static readonly int StepPWM;

        /// <summary>
        /// InputSensor number - varies based on HW implementation
        /// </summary>
        internal static readonly int InputSensor;

        /// <summary>
        /// InputSensor number - varies based on HW implementation
        /// </summary>
        internal static readonly int InletSensor;

        /// <summary>
        /// Enable Inlet Temprature Sensor Offset
        /// </summary>
        internal static readonly bool EnableInletOffSet;

        /// <summary>
        /// LowThreshold for the sensor 
        /// </summary>
        internal static readonly int SensorLowThreshold;

        /// <summary>
        /// High Threshold for the sensor
        /// </summary>
        internal static readonly int SensorHighThreshold;

        /// <summary>
        /// AltitudeCorrectionFactor - based on HW Spec for CM
        /// </summary>
        internal static readonly float AltitudeCorrectionFactor;

        /// <summary>
        /// Altitude - varies based on location - integer (feet above sea level)
        /// </summary>
        internal static readonly int Altitude;

        /// <summary>
        /// Maximum number of retries allowed
        /// </summary>
        internal static readonly int MaxRetries;

        /// <summary>
        /// LED high value
        /// </summary>
        internal static readonly int LEDHigh;

        /// <summary>
        /// Min blade power limit value
        /// </summary>
        internal static readonly int MinPowerLimit;

        /// <summary>
        /// Max blade power limit value
        /// </summary
        internal static readonly int MaxPowerLimit;

        /// <summary>
        /// Power Limit Correction (enforcement granularity) Time in Milliseconds
        /// </summary
        internal static readonly int SetPowerLimitCorrectionTimeInMilliseconds;

        /// <summary>
        /// Maximum Failure Count before trying reinitialization
        /// </summary>
        internal static readonly int MaxFailCount;

        /// <summary>
        /// Service timeout in minutes
        /// </summary>
        internal static readonly double ServiceTimeoutInMinutes;

        /// <summary>
        /// Max work queue length of the port manager
        /// </summary>
        internal static readonly int MaxPortManagerWorkQueueLength;
 
        internal static int CmServicePortNumber;
        internal static string SslCertificateName;
        internal static bool EnableSslEncryption;
        internal static bool EnableFan;

        // List of event log strings. Loaded from EventDataStrings.xml.  This xml file 
        // is similar to a traditional resource file
        internal static List<EventLogData> EventStrings;

        // Event log formatting seperator used to break-up strings. Loaded in EventDataStrings.xml
        internal static readonly string EventLogStrSeparator;

        // Event log formatting string Event Data 1-3 bytes. Loaded in EventDataStrings.xml
        internal static readonly string EventData;

        // Event log formatting space. Loaded in EventDataStrings.xml
        internal static readonly string EventLogStrSpacer;

        // Event log string for SensorType. Loaded in EventDataStrings.xml
        internal static readonly string EventLogStrSensor;

        // Unknown string loaded from EventDataStrings.xml
        internal static readonly string Unknown;

        // Enable Battery Monitoring
        internal static readonly bool BatteryMonitoringEnabled;

        // Enable Psu Alert
        internal static readonly bool PsuAlertMonitorEnabled;

        // Enable Datasafe APIs
        internal static readonly bool DatasafeOperationsEnabled;
        
        // Enable PowerAlertDrivenPowerCap APIs
        internal static readonly bool PowerAlertDrivenPowerCapAPIsEnabled;

        // Default Power Cap Automatic Deassertion
        internal static readonly bool DpcAutoDeassert;

        // Default Psu Alert Thread monitoring interval.
        internal static readonly int PsuAlertPollInterval;

        // Default Psu monitoring interval for traditional PSU polling.
        internal static readonly int PsuPollInterval;

        // Force Emerson PSU Instantiation
        internal static readonly bool ForceEmersonPsu;
        
        // Number of NvDimm installed per blade
        internal static readonly int NvDimmPerBlade;

        // NvDimm Trigger Failure Count allowed before taking action
        internal static readonly int NvDimmTriggerFailureCount;

        // NvDimm Trigger Failure Action:
        // 0 = Nohting
        // 1 = Blade_EN Off.
        internal static readonly int NvDimmTriggerFailureAction;

        // Flag to enable processing critical battery status
        internal static readonly bool ProcessBatteryStatus;

        // Battery charge level threshold
        internal static readonly double BatteryChargeLevelThreshold;

        // Delay between ADR_COMPLETE and blade power off (seconds)
        internal static readonly int AdrCompleteDelay;

        // NVDIMM Present Power-off Delay (seconds)
        internal static readonly int NvDimmPresentPowerOffDelay;

        // Battery discharge time in seconds after which NVDIMM backup is triggered.
        internal static readonly int BatteryDischargeTimeInSecs;

        // Blade Serial Session Power On wait interval between retry
        internal static readonly int SerialSessionPowerOnWait;

        // Blade Serial Session Power On Retry
        internal static readonly int SerialSessionPowerOnRetry;

        // Time required for BMC to respond after power on
        internal static readonly int FirmwareDecompressionTime;

        // Timeout in seconds for thread wait.
        internal static readonly TimeSpan ThreadJoinTimeout;

        /// <summary>
        /// Class Constructor.
        /// </summary>
        static ConfigLoaded()
        {

            // check app.config for Join Timeout in seconds
            int joinTimeout;
            int.TryParse(Config.AppSettings["JoinTimeout"], out joinTimeout);
            ThreadJoinTimeout = joinTimeout == 0 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(joinTimeout);

            // check app.config for Population, if population is not found
            // in the app.config default the value to 24.
            int.TryParse(Config.AppSettings["Population"], out Population);
            Population = Population == 0 ? 24 : Population;

            // check app.config for MaxSerialConsoleSessions, if MaxSerialConsoleSessions is not found
            // in the app.config default the value to 12.
            uint.TryParse(Config.AppSettings["MaxSerialConsoleSessions"], out MaxSerialConsoleSessions);
            MaxSerialConsoleSessions = MaxSerialConsoleSessions == 0 ? 12 : MaxSerialConsoleSessions;

            // check app.config for MaxSerialConsolePorts, if MaxSerialConsolePorts is not found
            // in the app.config default the value to 1.
            int.TryParse(Config.AppSettings["MaxSerialConsolePorts"], out MaxSerialConsolePorts);
            MaxSerialConsolePorts = MaxSerialConsolePorts == 0 ? 1 : MaxSerialConsolePorts;

            // Time for BMC to decompress firmware and start responding after power on. 
            // This is the firmware decompression time.
            int.TryParse(Config.AppSettings["FirmwareDecompressionTime"], out FirmwareDecompressionTime);
            FirmwareDecompressionTime = FirmwareDecompressionTime == 0 ? 28 : FirmwareDecompressionTime;

            int manufacturerId;
            int.TryParse(Config.AppSettings["MultiRecordFruManufacturerId"], out manufacturerId);
            manufacturerId = manufacturerId == 0 ? 311 : manufacturerId;
            ConvertIanaManufactureId(manufacturerId);

            int fruWritesRemaining = 0;
            int.TryParse(Config.AppSettings["ResetMultiRecordFruWritesRemaining"], out fruWritesRemaining);
            ResetMultiRecordFruWritesRemaining = fruWritesRemaining == 0 ? false : true;

            int.TryParse(Config.AppSettings["SerialPortConsoleClientSessionInactivityTimeoutInSecs"], out SerialPortConsoleClientSessionInactivityTimeoutInSecs);
            SerialPortConsoleClientSessionInactivityTimeoutInSecs = SerialPortConsoleClientSessionInactivityTimeoutInSecs == 0 ? 120 : SerialPortConsoleClientSessionInactivityTimeoutInSecs;

            int.TryParse(Config.AppSettings["SerialPortConsoleDeviceCommunicationTimeoutInMsecs"], out SerialPortConsoleDeviceCommunicationTimeoutInMsecs);
            SerialPortConsoleDeviceCommunicationTimeoutInMsecs = SerialPortConsoleDeviceCommunicationTimeoutInMsecs == 0 ? 100 : SerialPortConsoleDeviceCommunicationTimeoutInMsecs;

            // Default to 300 seconds if app.config setting is 0, negative or an invalid string
            uint.TryParse(Config.AppSettings["BladeSerialTimeout"], out BladeSerialTimeout);
            BladeSerialTimeout = BladeSerialTimeout == 0 ? 300 : BladeSerialTimeout;

            int.TryParse(Config.AppSettings["SerialTimeout"], out SerialTimeout);
            SerialTimeout = SerialTimeout == 0 ? 100 : SerialTimeout;

            int.TryParse(Config.AppSettings["GpioErrorLimit"], out GpioErrorLimit);
            GpioErrorLimit = GpioErrorLimit == 0 ? 3 : GpioErrorLimit;

            // check app.config for BmcSessionTime.  if not found, the default value
            // is 6.
            int.TryParse(Config.AppSettings["BmcSessionTime"], out BmcSessionTime);
            BmcSessionTime = BmcSessionTime == 0 ? 6 : BmcSessionTime;

            // check app.config for BmcUserName, if BmcUserName is not found
            // in the app.config default the value to root.
            BmcUserName = Config.AppSettings["BmcUserName"];
            BmcUserName = string.IsNullOrEmpty(BmcUserName) ? "root" : BmcUserName;

            // check app.config for BmcUserKey, if BmcUserName is not found
            // in the app.config default the value to root.
            BmcUserKey = Config.AppSettings["BmcUserKey"];
            BmcUserKey = string.IsNullOrEmpty(BmcUserKey) ? "root" : BmcUserKey;

            // check app.config for NumFans, if NumFans is not found
            // in the app.config default the value to 6.
            int.TryParse(Config.AppSettings["NumFans"], out NumFans);
            NumFans = NumFans < 0 ? 6 : NumFans;

            // check app.config for NumPsus, if NumPsus is not found
            // in the app.config default the value to 6.
            int.TryParse(Config.AppSettings["NumPsus"], out NumPsus);
            NumPsus = NumPsus < 0 ? 6 : NumPsus;

            // check app.config for NumBatteries, if NumBatteries is not found
            // in the app.config default the value to 6.
            int.TryParse(Config.AppSettings["NumBatteries"], out NumBatteries);
            NumBatteries = NumBatteries < 0 ? 6 : NumBatteries;

            // check app.config for NumNicsPerBlade, if not found
            // in the app.config default the value to 2.
            int.TryParse(Config.AppSettings["NumNicsPerBlade"], out NumNicsPerBlade);
            NumNicsPerBlade = NumNicsPerBlade == 0 ? 2 : NumNicsPerBlade;

            // check app.config for NumPowerSwitches, if NumPowerSwitches is not found
            // in the app.config default the value to 2.
            int.TryParse(Config.AppSettings["NumPowerSwitches"], out NumPowerSwitches);
            NumPowerSwitches = NumPowerSwitches < 0 ? 0 : NumPowerSwitches;

            // check app.config for WaitTimeAfterACSocketPowerOffInMsecs, if not found
            // in the app.config default the value to 1000 ms.
            int.TryParse(Config.AppSettings["WaitTimeAfterACSocketPowerOffInMsecs"], out WaitTimeAfterACSocketPowerOffInMsecs);
            WaitTimeAfterACSocketPowerOffInMsecs = WaitTimeAfterACSocketPowerOffInMsecs == 0 ? 1000 : WaitTimeAfterACSocketPowerOffInMsecs;

            // check app.config for WaitTimeAfterBladeHardPowerOffInMsecs, if not found
            // in the app.config default the value to 100 ms.
            int.TryParse(Config.AppSettings["WaitTimeAfterBladeHardPowerOffInMsecs"], out WaitTimeAfterBladeHardPowerOffInMsecs);
            WaitTimeAfterBladeHardPowerOffInMsecs = WaitTimeAfterBladeHardPowerOffInMsecs == 0 ? 100 : WaitTimeAfterBladeHardPowerOffInMsecs;

            // check app.config for ChassisManagerTraceFilePath, if not found
            // in the app.config default the value to C:\ChassisManagerTrace.txt.
            TraceLogFilePath = Config.AppSettings["TraceLogFilePath"];
            TraceLogFilePath = string.IsNullOrEmpty(TraceLogFilePath) ? @"C:\ChassisManagerTrace.txt" : TraceLogFilePath;

            // check app.config for ChassisManagerTraceFileSize, if not found
            // in the app.config default the value to 100 KB.
            int.TryParse(Config.AppSettings["TraceLogFileSize"], out TraceLogFileSize);
            TraceLogFileSize = TraceLogFileSize == 0 ? 100 : TraceLogFileSize;

            // check app.config for ChassisManagerTraceFilePath, if not found
            // in the app.config default the value to C:\ChassisManagerTrace.txt.
            UserLogFilePath = Config.AppSettings["UserLogFilePath"];
            UserLogFilePath = string.IsNullOrEmpty(UserLogFilePath) ? @"C:\ChassisManagerUserLog.txt" : UserLogFilePath;

            // check app.config for ChassisManagerUserFileSize, if not found
            // in the app.config default the value to 100 KB.
            int.TryParse(Config.AppSettings["UserLogFileSize"], out UserLogFileSize);
            UserLogFileSize = UserLogFileSize == 0 ? 100 : UserLogFileSize;

            // check app.config for UserLogMaxEntries, if not found 
            // in the app.config default the value to 50 entries. 
            int.TryParse(Config.AppSettings["UserLogMaxEntries"], out UserLogMaxEntries); 
            UserLogMaxEntries = UserLogMaxEntries == 0 ? 50 : UserLogMaxEntries; 

            // check app.config for MaxPWM, if it is not found
            // in the app.config default the value to 100.
            int.TryParse(Config.AppSettings["MaxPWM"], out MaxPWM);
            MaxPWM = MaxPWM == 0 ? 100 : MaxPWM;

            // check app.config for MinPWM, if it is not found
            // in the app.config default the value to 20.
            int.TryParse(Config.AppSettings["MinPWM"], out MinPWM);
            MinPWM = MinPWM == 0 ? 20 : MinPWM;

            // check app.config for StepPWM, if it is not found
            // in the app.config default the value to 10.
            int.TryParse(Config.AppSettings["StepPWM"], out StepPWM);
            StepPWM = StepPWM == 0 ? 10 : StepPWM;

            // check app.config for InputSensor, if it is not found
            // in the app.config default the value to 1.
            int.TryParse(Config.AppSettings["InputSensor"], out InputSensor);
            InputSensor = InputSensor == 0 ? 1 : InputSensor;

            // check app.config for Inlet, if it is not found
            // in the app.config default the value to 176.
            int.TryParse(Config.AppSettings["InletSensor"], out InletSensor);
            InletSensor = InletSensor == 0 ? 176 : InletSensor;

            // check app.config for Inlet Offset, if it is not found
            // in the app.config default the value to false.
            int InletOffSet = 0;
            int.TryParse(Config.AppSettings["EnableInletOffSet"], out InletOffSet);
            EnableInletOffSet = InletOffSet == 0 ? false : true;

            // check app.config for SensorLowThreshold, if it is not found
            // in the app.config default the value to 0.
            int.TryParse(Config.AppSettings["SensorLowThreshold"], out SensorLowThreshold);
            SensorLowThreshold = SensorLowThreshold == 0 ? 0 : SensorLowThreshold;

            // check app.config for SensorHighThreshold, if it is not found
            // in the app.config default the value to 100.
            int.TryParse(Config.AppSettings["SensorHighThreshold"], out SensorHighThreshold);
            SensorHighThreshold = SensorHighThreshold == 0 ? 100 : SensorHighThreshold;

            // check app.config for AltitudeCorrectionFactor, if it is not found
            // in the app.config default the value to 0.032 (3.2%).
            float.TryParse(Config.AppSettings["AltitudeCorrectionFactor"], out AltitudeCorrectionFactor);
            AltitudeCorrectionFactor = AltitudeCorrectionFactor == 0 ? (float)0.032 : AltitudeCorrectionFactor;

            // check app.config for Altitude, if it is not found
            // in the app.config default the value to 0 (0 feet above sea level).
            int.TryParse(Config.AppSettings["Altitude"], out Altitude);
            Altitude = Altitude == 0 ? 0 : Altitude;

            // check app.config for MaxRetries, if it is not found
            // in the app.config default the value to 3.
            int.TryParse(Config.AppSettings["MaxRetries"], out MaxRetries);
            MaxRetries = MaxRetries == 0 ? 3 : MaxRetries;

            // check app.config for LEDHigh, if it is not found
            // in the app.config default the value to 255.
            int.TryParse(Config.AppSettings["LEDHigh"], out LEDHigh);
            LEDHigh = LEDHigh == 0 ? 255 : LEDHigh;

            // check app.config for MinPowerLimit, if it is not found
            // in the app.config default the value to 120W.
            int.TryParse(Config.AppSettings["MinPowerLimit"], out MinPowerLimit);
            MinPowerLimit = MinPowerLimit == 0 ? 120 : MinPowerLimit;

            // check app.config for MaxPowerLimit, if it is not found
            // in the app.config default the value to 1000W.
            int.TryParse(Config.AppSettings["MaxPowerLimit"], out MaxPowerLimit);
            MaxPowerLimit = MaxPowerLimit == 0 ? 1000 : MaxPowerLimit;

            // check app.config for SetPowerLimitCorrectionTimeInMilliseconds, if it is not found
            // in the app.config default the value to 6000ms.
            int.TryParse(Config.AppSettings["SetPowerLimitCorrectionTimeInMilliseconds"], out SetPowerLimitCorrectionTimeInMilliseconds);
            SetPowerLimitCorrectionTimeInMilliseconds = SetPowerLimitCorrectionTimeInMilliseconds == 0 ? 6000 : SetPowerLimitCorrectionTimeInMilliseconds;

            // check app.config for MaxFailCount, if it is not found
            // in the app.config default the value to 0.
            int.TryParse(Config.AppSettings["MaxFailCount"], out MaxFailCount);
            MaxFailCount = MaxFailCount == 0 ? 0 : MaxFailCount;

            // check app.config for MaxPortManagerWorkQueueLength, if it is not found
            // in the app.config default the value to 10.
            int.TryParse(Config.AppSettings["MaxPortManagerWorkQueueLength"], out MaxPortManagerWorkQueueLength);
            MaxPortManagerWorkQueueLength = MaxPortManagerWorkQueueLength == 0 ? 20 : MaxPortManagerWorkQueueLength;

            // check app.config for ServiceTimeoutInMinutes, if it is not found
            // in the app.config default the value to 2 minutes.
            double.TryParse(Config.AppSettings["ServiceTimeoutInMinutes"], out ServiceTimeoutInMinutes);
            ServiceTimeoutInMinutes = ServiceTimeoutInMinutes == 0 ? 2 : ServiceTimeoutInMinutes;

            // check app.config for CMServicePortNumber, if it is not found
            // in the app.config default the value to 8000.
            int.TryParse(Config.AppSettings["CmServicePortNumber"], out CmServicePortNumber);
            CmServicePortNumber = CmServicePortNumber < 1 ? 8000 : CmServicePortNumber;

            // check app.config for SslCertificateName, if not found
            // in the app.config default the value to "CMServiceServer".
            SslCertificateName = Config.AppSettings["SslCertificateName"];
            SslCertificateName = string.IsNullOrEmpty(SslCertificateName) ? @"CMServiceServer" : SslCertificateName;

            // check app.config for EnableSslEncryption, if not found
            // in the app.config default the value to true/enable.
            int tempSslEncrypt = 1;
            int.TryParse(Config.AppSettings["EnableSslEncryption"], out tempSslEncrypt);
            EnableSslEncryption = tempSslEncrypt == 0 ? false : true;

            // Check App.config for EnableFan, if not found, default to true/enable.
            int tempEnableFan = 1;
            int.TryParse(Config.AppSettings["EnableFan"], out tempEnableFan);
            EnableFan = tempEnableFan == 0 ? false : true;

            // Check App.config for EnableBatteryMonitoring
            int batteryMonitoring;
            int.TryParse(Config.AppSettings["EnableBatteryMonitoring"], out batteryMonitoring);
            BatteryMonitoringEnabled = (batteryMonitoring == 0 ? false : true);

            // Check App.config for EnablePsuAlert
            int psuAlert;
            int.TryParse(Config.AppSettings["EnablePsuAlertMonitoring"], out psuAlert);
            PsuAlertMonitorEnabled = (psuAlert == 0 ? false : true);

            // Check App.config for EnableDatasafeAPIs
            int datasafeOperations;
            int.TryParse(Config.AppSettings["EnableDatasafeAPIs"], out datasafeOperations);
            DatasafeOperationsEnabled = (datasafeOperations == 0 ? false : true);

            // Check App.config for EnablePowerAlertDrivenPowerCapAPIs
            int powerAlertAPIs;
            int.TryParse(Config.AppSettings["EnablePowerAlertDrivenPowerCapAPIs"], out powerAlertAPIs);
            PowerAlertDrivenPowerCapAPIsEnabled = (powerAlertAPIs == 0 ? false : true);

            // Check App.config for DpcAutoDeassert
            int dpcAuto;
            int.TryParse(Config.AppSettings["DpcAutoDeassert"], out dpcAuto);
            DpcAutoDeassert = (dpcAuto == 0 ? false : true);

            int.TryParse(Config.AppSettings["PsuAlertPollInterval"], out PsuAlertPollInterval);
            PsuAlertPollInterval = PsuAlertPollInterval < 3 ? 3 : PsuAlertPollInterval;     
       
            int.TryParse(Config.AppSettings["PsuPollInterval"], out PsuPollInterval);
            PsuPollInterval = PsuPollInterval < 10 ? 10 : PsuPollInterval;

            // Flag for forcing instantiation of unknown PSUs to Emerson PSUs.
            int forceEmersonPsuFlag = 0;
            int.TryParse(Config.AppSettings["ForceEmersonPsu"], out forceEmersonPsuFlag);
            ForceEmersonPsu = forceEmersonPsuFlag == 0 ? false : true;

            int.TryParse(Config.AppSettings["NvDimmPerBlade"], out NvDimmPerBlade);
            NvDimmPerBlade = NvDimmPerBlade == 0 ? 2 : NvDimmPerBlade; 

            int.TryParse(Config.AppSettings["NvDimmTriggerFailureCount"], out NvDimmTriggerFailureCount);
            NvDimmTriggerFailureCount = NvDimmTriggerFailureCount < 1 ? 1 : NvDimmTriggerFailureCount; 

            int.TryParse(Config.AppSettings["NvDimmTriggerFailureAction"], out NvDimmTriggerFailureAction);
            NvDimmTriggerFailureAction = NvDimmTriggerFailureAction < 0 ? 0 : NvDimmTriggerFailureAction;
 
            int.TryParse(Config.AppSettings["AdrCompleteDelay"], out AdrCompleteDelay);
            AdrCompleteDelay = AdrCompleteDelay == 0 ? 5 : AdrCompleteDelay;

            int.TryParse(Config.AppSettings["NvDDimmPresentPowerOffDelay"], out NvDimmPresentPowerOffDelay);
            NvDimmPresentPowerOffDelay = NvDimmPresentPowerOffDelay == 0 ? 160 : NvDimmPresentPowerOffDelay;

            // check app.config for ProcessBatteryStatus
            // if not found in the app.config default the value to false.
            int tempProcessBatteryStatus = 0;
            int.TryParse(Config.AppSettings["ProcessBatteryStatus"], out tempProcessBatteryStatus);
            ProcessBatteryStatus = tempProcessBatteryStatus == 0 ? false : true;

            // check app.config for BatteryChargeLevelThreshold
            // if not found in the app.config default the value to 50 percent.
            double.TryParse(Config.AppSettings["BatteryChargeLevelThreshold"], out BatteryChargeLevelThreshold);
            BatteryChargeLevelThreshold = BatteryChargeLevelThreshold == 0 ? 50 : BatteryChargeLevelThreshold;

            // check app.config for BatteryDischargeTimeInSecs
            // if not found in the app.config default the value to 35 seconds.
            int.TryParse(Config.AppSettings["BatteryDischargeTimeInSecs"], out BatteryDischargeTimeInSecs);
            BatteryDischargeTimeInSecs = BatteryDischargeTimeInSecs == 0 ? 35 : BatteryDischargeTimeInSecs;

            // check app.config for SerialSessionPowerOnWait
            // if not found in the app.config default the value to 5000 ms.
            int.TryParse(Config.AppSettings["SerialSessionPowerOnWait"], out SerialSessionPowerOnWait);
            SerialSessionPowerOnWait = SerialSessionPowerOnWait == 0 ? 5000 : SerialSessionPowerOnWait;

            // check app.config for SerialSessionPowerOnRetry
            // if not found in the app.config default the value to 10.
            int.TryParse(Config.AppSettings["SerialSessionPowerOnRetry"], out SerialSessionPowerOnRetry);
            SerialSessionPowerOnRetry = SerialSessionPowerOnRetry == 0 ? 10 : SerialSessionPowerOnRetry;
            
            // check app.config for EventLogXml, if not found
            // in the app.config default the value to EventDataStrings.xml
            string evtLogFile = string.Empty;
            evtLogFile = Config.AppSettings["EventLogXml"];
            evtLogFile = string.IsNullOrEmpty(evtLogFile) ? @"EventDataStrings.xml" : evtLogFile;

            // format event log strings dictionary
            Dictionary<string, string> formatStrings = new Dictionary<string, string>();

            try
            {
                FileStream fs = new FileStream(evtLogFile, FileMode.Open, FileAccess.Read);

                XmlDocument XmlEventLogDoc = new XmlDocument();
                
                // load xml document
                XmlEventLogDoc.Load(fs);

                // convert xml document into class objects
                EventStrings = XmlToObject("EventLogTypeCode", XmlEventLogDoc);

                // populate format event log strings dictionary
                XmlFormatStrings("EventLogStrings", formatStrings, XmlEventLogDoc);

            }
            catch (System.Exception ex)
            {
                Tracer.WriteWarning(string.Format("ERROR: Could not load Event Log Strings from {0}", evtLogFile));
                Tracer.WriteError(0, "ConfigLoaded.Configloaded", ex);

                // set event strings to default empty list.
                EventStrings = new List<EventLogData>();
            }

            if (formatStrings.ContainsKey("EventData"))
            {
                EventData = formatStrings["EventData"];
            }
            else
            {
                EventData = string.Empty;
            }

            if (formatStrings.ContainsKey("Separator"))
            {
                EventLogStrSeparator = formatStrings["Separator"];
            }
            else
            {
                EventLogStrSeparator = string.Empty;
            }

            if (formatStrings.ContainsKey("Space"))
            {
                EventLogStrSpacer = formatStrings["Space"];
            }
            else
            {
                EventLogStrSpacer = string.Empty;
            }

            if (formatStrings.ContainsKey("SensorType"))
            {
                EventLogStrSensor = formatStrings["SensorType"];
            }
            else
            {
                EventLogStrSensor = string.Empty;
            }

            if (formatStrings.ContainsKey("Unknown"))
            {
                Unknown = formatStrings["Unknown"];
            }
            else
            {
                Unknown = string.Empty;
            }    

        }

        /// <summary>
        /// Converts integer to IANA 3 byte Manufactuer Id
        /// </summary>
        private static void ConvertIanaManufactureId(int manufacturerId)
        {
            MultiRecordFruManufacturerId[0] = (byte)(manufacturerId >> 16);
            MultiRecordFruManufacturerId[1] = (byte)(manufacturerId >> 8);
            MultiRecordFruManufacturerId[2] = (byte)manufacturerId;        
        }

        #region EventLog

        private static Dictionary<byte, string> MemoryMap = new Dictionary<byte, string>(16){
            {0x01, "A1"},
            {0x02, "A2"},
            {0x03, "B1"},
            {0x04, "B2"},
            {0x05, "C1"},
            {0x06, "C2"},
            {0x07, "D1"},
            {0x08, "D2"},
            {0x09, "E1"},
            {0x0A, "E2"},
            {0x0B, "F1"},
            {0x0C, "F2"},
            {0x0D, "G1"},
            {0x0E, "G2"},
            {0x0F, "H1"},
            {0x10, "H2"}
        };            

        private static List<EventLogData> XmlToObject(string xmlTag, XmlDocument xmlEventLogDoc)
        {
            List<EventLogData> response = new List<EventLogData>();

            try
            {
                XmlNodeList rootNodeList = xmlEventLogDoc.GetElementsByTagName(xmlTag);

                if (rootNodeList.Count > 0)
                {
                    // root level node: EventLogTypeCode
                    foreach (XmlNode root in rootNodeList)
                    {
                        // GenericEvent/SensorSpecificEvent
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            EventLogMsgType clasification = GetEventLogClass(node.Name.ToString());

                            XmlNodeList firstTierNodes = node.ChildNodes;

                            // enumerate first level child nodes
                            foreach (XmlNode firstNode in firstTierNodes)
                            {
                                int number = Convert.ToInt32(firstNode.Attributes["Number"].Value.ToString());
                                string description = firstNode.Attributes["ReadingClass"].Value.ToString();

                                XmlNodeList secondTierNodes = firstNode.ChildNodes;

                                // enumerate second level xml nodes
                                foreach (XmlNode secondNode in secondTierNodes)
                                {
                                    int offset = Convert.ToInt32(secondNode.Attributes["Number"].Value.ToString());
                                    string message = secondNode.Attributes["Description"].Value.ToString();

                                    EventLogData respObj = new EventLogData(number, offset, clasification, message, description);

                                    XmlNodeList thirdTierNodes = secondNode.ChildNodes;

                                    if (thirdTierNodes.Count > 0)
                                    {
                                        // enumerate third level xml nodes
                                        foreach (XmlNode extension in thirdTierNodes)
                                        {
                                            int id = Convert.ToInt32(extension.Attributes["Number"].Value.ToString());
                                            string detail = extension.Attributes["Description"].Value.ToString();

                                            respObj.AddExtension(id, detail);
                                        } // enumerate third level xml nodes
                                    }

                                    response.Add(respObj);
                                } // enumerate second level xml nodes
                            } // enumerate first level child nodes
                        } // GenericEvent/SensorSpecificEvent
                    }
                }
                else
                {
                    Tracer.WriteWarning("ERROR: Could not load Event Log Strings, could not find xml root node in file");
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("ERROR: Could not load Event Log Strings. Error: {0}", ex.ToString()));
            }

            return response;
        }

        private static void XmlFormatStrings(string xmlTag, Dictionary<string, string> formatStrings, XmlDocument xmlEventLogDoc)
        {
            try
            {
                XmlNodeList rootNodeList = xmlEventLogDoc.GetElementsByTagName(xmlTag);

                // root level node: EventLogStrings
                foreach (XmlNode root in rootNodeList)
                {
                    // EventString
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        string key = node.Attributes["Name"].Value.ToString();
                        string val = node.Attributes["String"].Value.ToString();
                        formatStrings.Add(key, val);
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("ERROR: Could not load Event Log Format Strings. Error: {0}", ex.ToString()));
            }

        }

        /// <summary>
        /// Gets the event log class. The event type codes are described in the IPMI 2.0 spec, section 42.1.
        /// </summary>
        /// <param name="name">The event class name.</param>
        /// <returns></returns>
        private static EventLogMsgType GetEventLogClass(string name)
        {
            if (name.ToLowerInvariant() == "thresholdevent")
                return EventLogMsgType.Threshold;
            else if (name.ToLowerInvariant() == "discreteevent")
                return EventLogMsgType.Discrete;
            else if (name.ToLowerInvariant() == "sensorspecificevent")
                return EventLogMsgType.SensorSpecific;
            else if (name.ToLowerInvariant() == "oemevent")
                return EventLogMsgType.Oem;
            else if (name.ToLowerInvariant() == "oemtimestampedevent")
                return EventLogMsgType.OemTimestamped;
            else if (name.ToLowerInvariant() == "oemnontimestampedevent")
                return EventLogMsgType.OemNonTimeStamped;
            else
                return EventLogMsgType.Unspecified;
        }

        internal static EventLogData GetEventLogData(Ipmi.EventLogMsgType eventType, int number, int offset)
        {
            EventLogData logDataQuery =
            (from eventLog in EventStrings
             where eventLog.Number == number
             && eventLog.OffSet == offset
             && eventLog.MessageClass == eventType
             select eventLog).FirstOrDefault();

            if (logDataQuery != null)
            {
                // Create new instance of EventLogData to avoid overwriting EventMessage and Description 
                EventLogData tempData = new EventLogData(logDataQuery.Number, logDataQuery.OffSet, logDataQuery.MessageClass,
                    string.Copy(logDataQuery.EventMessage), string.Copy(logDataQuery.Description));
                return tempData;
            }
            else
            {
                return new EventLogData();
            }
        }

        /// <summary>
        /// Returns the actual DIMM Number of WCS blades
        /// </summary>
        /// <param name="dimm">Dimm Index</param>
        /// <returns>WCS DIMM Name</returns>
        internal static string GetDimmNumber(byte dimm)
        {
            if (MemoryMap.ContainsKey(dimm))
                return MemoryMap[dimm];
            else
                return ConfigLoaded.Unknown;
        }

        #endregion

        #region GetAllSettings

        /// <summary>
        /// Get all app config settings
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetAllSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();

            try
            {
                string[] keys = Config.AppSettings.AllKeys;

                foreach (string key in keys)
                {
                    settings.Add(key, Config.AppSettings.Get(key).ToString());
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(string.Format("Unable to read all key value settings from App config {0}", e.ToString()));
            }

            return settings;
        }
        #endregion

    }
}
