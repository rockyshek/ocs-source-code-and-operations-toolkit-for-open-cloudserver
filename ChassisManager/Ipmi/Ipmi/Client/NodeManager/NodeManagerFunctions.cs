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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager
{
    using System;

    /// <summary>
    /// Defines Node Manager network function codes.
    /// </summary>
    [FlagsAttribute]
    internal enum NodeManagerFunctions
    {
        /// <summary>
        /// NodeManager.
        /// </summary>
        NodeManager = 46,

        /// <summary>
        /// Application.
        /// </summary>
        Application = 6,
    }

    /// <summary>
    /// Node Manager Commands
    /// </summary>
    internal enum NodeManagerCommand
    {

        #region NodeManager

        /// <summary>
        /// Get Intel NM firmware version. Major firmware revision and Minor 
        /// firmware revision unambiguously identify firmware release.
        /// </summary>
        GetVersion = 0xCA,

        /// <summary>
        /// Force ME Firmware Recovery
        /// </summary>
        ForceMeRecovery = 0xDF,

        #endregion

        #region Policy

        /// <summary>
        /// Enable/Disable Policy.
        /// </summary>
        PolicyControl = 0xC0,

        /// <summary>
        /// Set Node Manager Policy
        /// </summary>
        SetPolicy = 0xC1,

        /// <summary>
        /// Get Node Manager Policy
        /// </summary>
        GetPolicy = 0xC2,

        /// <summary>
        /// Set Node Manager Alert Threshold
        /// </summary>
        SetPolicyAlertThreshold = 0xC3,

        /// <summary>
        /// Get Node Manager Alert Threshold
        /// </summary>
        GetPolicyAlertThreshold = 0xC4,

        /// <summary>
        /// Get Node Manager Limiting Policy Id
        /// </summary>
        GetLimitingPolicyId = 0xF2,

        /// <summary>
        /// Set Turbo Synchronization Ratio
        /// </summary>
        SetTurboSyncRatio = 0xCC,

        /// <summary>
        /// Get Turbo Synchronization Ratio
        /// </summary>
        GetTurboSyncRatio = 0xCD,


        #endregion
        
        #region Statistics

        /// <summary>
        /// Reset Node Manager Statistics
        /// </summary>
        ResetStatistics = 0xC7,

        /// <summary>
        /// Get Node Manager Statistics
        /// </summary>
        GetStatistics = 0xC8,

        #endregion

        #region Power

        /// <summary>
        /// Set total power budget for the CPUs. This command is optional and may be 
        /// unavailable on certain implementations. This command controls the platform 
        /// power limit using aggressive settings.
        /// 
        /// Note: �Set Total Power Budget� function is only accessible if the 
        /// Intel NM policy control feature is disabled.
        /// </summary>
        SetTotalPowerBudget = 0xD0,

        /// <summary>
        /// Get total power budget for the CPUs.
        /// </summary>
        GetTotalPowerBudget = 0xD1,

        /// <summary>
        /// Set the Min/Max power consumption ranges. This 
        /// information is preserved in the persistent storage.
        /// </summary>
        SetPowerDrawRange = 0xCB,

        #endregion

        #region Peci

        /// <summary>
        /// Send Raw PECI
        /// </summary>
        SendRawPeci = 0x40,

        /// <summary>
        /// Aggregated Send Raw PECI
        /// </summary>
        AggregatedSendRawPeci = 0x41,

        #endregion

        #region Cpu

        /// <summary>
        /// Returns CPU config data passed by BIOS
        /// </summary>
        GetHostCpuData = 0xEA,

        /// <summary>
        /// This command provides read access to the package 
        /// Configuration Space" that is maintained by the CPU
        /// </summary>
        CpuPackageConfigRead = 0x42,

        /// <summary>
        /// This command provides write access to the package 
        /// Configuration Space" that is maintained by the CPU
        /// </summary>
        CpuPackageConfigWrite = 0x43,

        /// <summary>
        /// The command reads from PCI configuration 
        /// space of selected CPU
        /// </summary>
        CpuPciConfigRead = 0x44,

        /// <summary>
        /// The command writes a value to PCI 
        /// configuration space of selected CPU
        /// </summary>
        CpuPciConfigWrite = 0x45,

        /// <summary>
        /// This command provides read access to the IA MSR space
        /// </summary>
        CpuIaMsrRead = 0x46,

        #endregion

        #region PMBus

        /// <summary>
        /// This command sends one PMBUS command to the 
        /// specified address. Address is validated against 
        /// factory presets.
        /// </summary>
        SendRawPmbus = 0xD9,

        /// <summary>
        /// Allows reconfiguring a PMBUS device.
        /// </summary>
        SetPmbusDeviceConfig = 0xF3,

        /// <summary>
        /// Allows reading PMBUSenabled Device Configuration
        /// </summary>
        GetPmbusDeviceConfig = 0xF4,

        /// <summary>
        /// This command retrieves values of group of monitored registers 
        /// retrieved from single PSU device. Maximum 8 values can be retrieved
        /// </summary>
        GetPmbusReadings = 0xF5,

        /// <summary>
        /// This command reads the same register value from a group of 
        /// PMBus-enabled devices. Up to 8 values can be read.
        /// </summary>
        GetAggPmbusReadings = 0xF6,

        #endregion

        #region RAS

        /// <summary>
        /// This command allows BMC to 
        /// discover the Dengate capabilities
        /// </summary>
        GetDengateCapabilities = 0xB7,

        /// <summary>
        /// This command is used to retrieve 
        /// the Dengate Health Status
        /// </summary>
        GetDengateHealthStatus = 0xBA,

        #endregion

        #region PTAS

        /// <summary>
        /// This command allows BMC to 
        /// discover the PTAS-CUPS capabilities
        /// </summary>
        GetCupsCapabilities = 0x64,

        /// <summary>
        /// This command allows BMC to 
        /// retrieve the CUPS data
        /// </summary>
        GetCupsData = 0x65,

        /// <summary>
        /// This command allows BMC to 
        /// set the PTAS-CUPS configuration
        /// </summary>
        SetCupsConfiguration = 0x66,

        /// <summary>
        /// This command allows BMC to 
        /// get the PTAS-CUPS configuration
        /// </summary>
        GetCupsConfiguration = 0x67,

        /// <summary>
        /// This command allows BMC to 
        /// set the PTAS-CUPS policy
        /// </summary>
        SetCupsPolicies = 0x68,

        /// <summary>
        /// This command allows BMC to 
        /// get the PTAS-CUPS policy
        /// </summary>
        GetCupsPolicies = 0x69,

        #endregion

        #region Mic

        /// <summary>
        /// ME Sends an IPMB command to MIC devices
        /// </summary>
        MicIpmbRequest = 0xE2,

        /// <summary>
        /// Command returns information about Management 
        /// capable PCIe cards
        /// </summary>
        GetMicCardInfo = 0xE3,
                
        #endregion

        #region Application

        /// <summary>
        /// Reboots Intel ME without resetting host platform
        /// </summary>
        ColdReset = 0x02,

        #endregion

    }

    /// <summary>
    /// Defines Node Manager Policy.
    /// </summary>
    internal enum NodeManagerPolicy
    {
        /// <summary>
        /// Disable policy control for all power domains
        /// </summary>
        GlobalDisablePolicy = 0x00,

        /// <summary>
        /// Enable policy control for all power domains
        /// </summary>
        GlobalEnablePolicy = 0x01,

        /// <summary>
        /// Disable policy for a given domain
        /// </summary>
        PerDomainDisable = 0x02,

        /// <summary>
        /// Enable policy for a given domain
        /// </summary>
        PerDomainEnable = 0x03,

        /// <summary>
        /// Disable policy for given domain and policy
        /// </summary>
        PerPolicyDisable = 0x04,

        /// <summary>
        /// Enable policy for given domain and policy
        /// </summary>
        PerPolicyEnable = 0x05,    
    }

    /// <summary>
    /// Node Manager Domain Id
    /// </summary>
    internal enum NodeManagerDomainId
    { 
        /// <summary>
        /// Entire Platform
        /// </summary>
        Platform = 0x00,

        /// <summary>
        /// CPU subsystem
        /// </summary>
        Processor = 0x01,

        /// <summary>
        /// Memory subsystem
        /// </summary>
        Memory = 0x02,

        /// <summary>
        /// HIgh power I/O subsystem
        /// </summary>
        SystemIO  = 0x03
    }

    /// <summary>
    /// Node Manager Policy Types
    /// </summary>
    internal enum NodeManagerPolicyType
    { 
        /// <summary>
        /// Inlet Temperature Limit Policy Trigger in Celsius
        /// </summary>
        Inlet_Temprature =0x01,

        /// <summary>
        /// Missing Power Reading Timeout in 1/10th of a second.
        /// </summary>
        MissingPowerReading = 0x02,

        /// <summary>
        /// Time After Platform Reset Trigger in 1/10th of a second
        /// </summary>
        TimeAfterPlatformReset = 0x03,

        /// <summary>
        /// Boot Time Policy.  This policy will apply the power policy at boot time.
        /// Note:  This type of boot policy can be applied only to domain Id Platform.
        /// </summary>
        BootTimePolicy = 0x04,
    }

    /// <summary>
    /// Node Manager Policy Action
    /// </summary>
    internal enum NodeManagerPolicyAction
    { 
        /// <summary>
        /// Removes policy and corresponding alterts thresholds.
        /// </summary>
        Remove = 0x00,

        /// <summary>
        /// Adds/Modifies power policy.
        /// </summary>
        Add = 0x01
    }

    /// <summary>
    /// CPU Power Correction Aggression
    /// </summary>
    internal enum NodeManagerPowerCorrection
    { 
        /// <summary>
        /// Automatic Mode [default].  Usage of T0States
        /// depends on Shutdown System bit in Policy Exception Action.
        /// </summary>
        Automatic = 0x00,

        /// <summary>
        /// Force nonaggressive does no allow T-States and memory throttling.
        /// </summary>
        Nonaggressive = 0x20,

        /// <summary>
        /// Force aggressive T-states and memory throggling are used.
        /// </summary>
        ForceAggressive = 0x40,
    }

    /// <summary>
    /// Policy Exception Actions performed if policy cannot be maintained within
    /// Correction time.
    /// </summary>
    internal enum NodeManagerPolicyExceptionAction
    { 
        SendAlert = 0x00,
        Shutdown = 0x01
    }

    /// <summary>
    /// Statistics Modes.  Modes for per policy statistics 
    /// are correlated to policy trigger type
    /// </summary>
    internal enum NodeManagerStatistics
    {
        // Global power statistics in [Watts] 
        GlobalPower = 0x01,
        // Global inlet temperature statistics in [Celsius] 
        GlobalTemperature = 0x02,
        // Per policy power statistics in [Watts] 
        PolicyPower = 0x11,
        // Per policy trigger statistics in [Celsius]. 
        PolicyTrigger = 0x12,
        //Per policy throttling statistics in [%]. 
        PolicyThrottling = 0x13,
        // Global Host Unhandled Requests statistics. 
        UnhandledRequests = 0x1B,
        // Global Host Response Time statistics. 
        ResponseTime = 0x1C,
        // Global CPU throttling statistics. 
        Cputhrottling = 0x1D,
        // Global memory throttling statistics. 
        MemoryThrottling = 0x1E,
        // Global Host Communication Failure statistics 
        CommunicationFailure = 0x1F,
    }

    /// <summary>
    /// Node Manager Firmware Recovery options.
    /// </summary>
    internal enum NodeManagerRecovery
    { 
        /// <summary>
        /// Restart using recovery firmware
        /// </summary>
        RecoveryFirmware = 0x01,

        /// <summary>
        /// Restart using factory defaults
        /// </summary>
        RestoreFactoryDefault = 0x02,
    }

    /// <summary>
    /// Node Manager to PECI Interface
    /// </summary>
    internal enum NodeManagerPeciInterface
    { 
        /// <summary>
        /// Inband first, if failure try serial
        /// </summary>
        Both    = 0x00,

        /// <summary>
        /// Inband interface to PECI only
        /// </summary>
        Inband  = 0x01,

        /// <summary>
        /// Serial Interface to PECI only
        /// </summary>
        Serial  = 0x02,
    }

    /// <summary>
    /// Node Manager MIC Command Protocol Support
    /// </summary>
    internal enum NodeManagerMicProtocol
    { 
        /// <summary>
        /// MCTP over SMBus
        /// </summary>
        MCTP_SMBus = 0x03,

        /// <summary>
        /// IPMI on PCIe SMBus
        /// </summary>
        IPMI_PCIe_SMBus = 0x02,

        /// <summary>
        /// IPMB
        /// </summary>
        IPMB = 0x01,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0x00,
    }

    /// <summary>
    /// SMBUS message transaction type
    /// </summary>
    internal enum NodeManagerSmbusTransactionType
    {
        SEND_BYTE = 0x00,
        READ_BYTE = 0x02,
        WRITE_BYTE = 0x04,
        READ_WORD = 0x06,
        WRITE_WORD = 0x08,
        BLOCK_READ = 0x0A,
        BLOCK_WRITE = 0x0C,
        BLOCK_WRITE_READ_PROC_CALL = 0x10,
    }

    /// <summary>
    /// Node Manager PMBUS Sensor Bus
    /// </summary>
    internal enum NodeManagerSensorBus
    {
        SMBUS   = 0x00,
        SMLINK0 = 0x01,
        SMLINK1 = 0x02,
        SMLINK2 = 0x03,
        SMLINK3 = 0x04,
        SMLINK4 = 0x05,
    }

    /// <summary>
    /// Dengate Health Status type
    /// </summary>
    internal enum NodeManagerDengateHealth
    { 
        Retirement_Watchdog = 0x00,
        IERR_Crash_Dump = 0x03,
        FRU_System_Address = 0x06,
        Clear_IERR_Crash_Dump = 0x07,
    }

    /// <summary>
    /// CUPS Policy Domain ID
    /// </summary>
    internal enum NodeManagerCupsPolicyDomainId
    {
        CoreDomain = 0x00,
        IoDomain = 0x01,
        MemoryDomain = 0x02,
    }

    /// <summary>
    /// CUPS Policy Target ID
    /// </summary>
    internal enum NodeManagerCupsPolicyTargetId
    {
        Bmc = 0x00,
        RemoteConsole = 0x01,
    }

    /// <summary>
    /// CUPS Policy Type
    /// </summary>
    internal enum NodeManagerCupsPolicyType
    {
        Persistent = 0x00,
        Volatile = 0x01,
    }

    /// <summary>
    /// Node Manager PECI Interface CPU Address.
    /// </summary>
    internal enum PeciTargetCpu
    {
        Cpu1 = 0x30,
        Cpu2 = 0x31,
        Cpu3 = 0x32,
        Cpu4 = 0x33
    }

    /// <summary>
    /// Node Manager MSR Power Limit
    /// </summary>
    internal enum MsrTargetPowerLimit
    {
        PowerLimt1 = 0x1A,
        PowerLimt2 = 0x1B,
    }
}
