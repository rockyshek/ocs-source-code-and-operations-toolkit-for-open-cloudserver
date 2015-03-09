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
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    using System.Collections.Generic;

    /// <summary>
    /// Chassis State class for OnStart, OnStop methods and for State management
    /// </summary>
    internal static class ChassisState
    {
        /// <summary>
        /// Indicates a fan failure has occured
        /// </summary>
        internal static bool FanFailure
        {
            get { return fanFailure; }
            set { fanFailure = value; }
        }

        /// <summary>
        /// Indicates a Psu failure has occured.
        /// </summary>
        internal static bool PsuFailure
        {
            get { return psuFailure; }
            set { psuFailure = value; }
        }

        /// <summary>
        /// Indicates a Chassis Manager Service Shutdown in progress.
        /// </summary>
        internal static bool ShutDown
        {
            get { return shutDown; }
            set { shutDown = value; }
        }

        #region private vars

        private static volatile byte[] bladeState = new byte[ConfigLoaded.Population];
        private static bool fanFailure = false;
        private static bool psuFailure = false;
        private static bool shutDown = false;

        // Internal database of battery status
        private static ChassisEnergyStorageStatus[] ChassisEnergyStorage = new ChassisEnergyStorageStatus[ConfigLoaded.NumBatteries];

        // lock variable for ensuring energy storage writes and reads are atomic
        private static object energyStorageLock = new object();

        #endregion

        #region internal vars

        // lock variable for ensuring state write and read are atomic
        internal static object[] locker = new object[ConfigLoaded.Population];

        // serial Console Metadata.
        internal static SerialConsoleMetadata[] SerialConsolePortsMetadata;

        // Blade Console Metadata
        internal static Dictionary<int, BladeSerialSessionMetadata> BladeSerialMetadata = new Dictionary<int, BladeSerialSessionMetadata>();

        // chassis AC Power Socket collection
        internal static AcSocket[] AcPowerSockets;

        // psu collection
        internal static PsuBase[] Psu = new PsuBase[(uint)ConfigLoaded.NumPsus];

        // Locks for ensuring that PSUs are not monitored during FW update
        // Since FW update takes around 11 minutes (primary controller) and 8 minutes (secondary controller)
        // we only lock the PSU that is being updated
        internal static bool[] PsuFwUpdateInProgress = new bool[ConfigLoaded.NumPsus];
        internal static object[] psuLock = new object[ConfigLoaded.NumPsus];

        // fan collection
        internal static Fan[] Fans = new Fan[(uint)ConfigLoaded.NumFans];

        // Blade Power Switch (aka. Blade_EN1)
        internal static BladePowerSwitch[] BladePower = new BladePowerSwitch[ConfigLoaded.Population];

        // Chassis WatchDogTimer
        internal static WatchDogTimer Wdt = new WatchDogTimer(1);

        // Global count of Power supplies
        internal static int MaxPsuCount = ConfigLoaded.NumPsus;

        // Global Instance of Blade State Failures
        internal static byte[] FailCount = new byte[ConfigLoaded.Population];

        internal static byte[] PowerFailCount = new byte[ConfigLoaded.Population];

        // Global Instance Cache of Blade Types
        internal static byte[] BladeTypeCache = new byte[ConfigLoaded.Population];

        // Global Instance Chassis Manager Attention LED
        internal static StatusLed AttentionLed = new StatusLed();

        // Global Instance Chassis Manager read object
        internal static ChassisFru CmFruData = new ChassisFru();

        // Global Instance of PsuAlert
        internal static PsuAlertSignal PsuAlert = new PsuAlertSignal();

        // Global Flag for Active PSU Alert Signal
        internal static bool PsuAlertActive
        {
            get;
            private set;
        }

        #endregion

        private static PsuModel ConvertPsuModelNumberToPsuModel(string psuModelNmber)
        {
            switch (psuModelNmber)
            {
                // Delta PSU. ASCII: "DPS-1200MB-1 C "
                case "44-50-53-2D-31-32-30-30-4D-42-2D-31-20-43-20":
                    return PsuModel.Delta;
                // Emerson 1425W Model. ASCII: "700-011719-0000"
                case "37-30-30-2D-30-31-31-37-31-39-2D-30-30-30-30":
                    return PsuModel.EmersonWithLes;
                // Emerson 1600W LES Model. ASCII: "PL1600H        "
                case "50-4C-31-36-30-30-48-20-20-20-20-20-20-20-20":
                    return PsuModel.EmersonWithLes;
                // Emerson 1600W non-LES Model. ASCII: "PS1600H        "
                case "50-53-31-36-30-30-48-20-20-20-20-20-20-20-20":
                    return PsuModel.EmersonNonLes;
                default:
                    return PsuModel.Default;
            }
        }

        /// <summary>
        /// Identifies the PSU vendor at each psu slot using the modelnumber API of the PsuBase class
        /// (Assumes all PSU vendors implement the MFR_MODEL Pmbus command)
        /// Based on the model number, we bind the Psu class object to the corresponding child (vendor) class object
        /// </summary>
        private static void PsuInitialize()
        {
            for (uint psuIndex = 0; psuIndex < ConfigLoaded.NumPsus; psuIndex++)
            {
                // Initially create instance of the base class
                // Later.. based on the psu model number, we create the appropriate psu class object
                Psu[psuIndex] = new PsuBase((byte)(psuIndex + 1));

                PsuModelNumberPacket modelNumberPacket = new PsuModelNumberPacket();
                modelNumberPacket = Psu[psuIndex].GetPsuModel();
                string psuModelNumber = modelNumberPacket.ModelNumber;
                PsuModel model = ConvertPsuModelNumberToPsuModel(psuModelNumber);

                switch (model)
                {
                    case PsuModel.Delta:
                        Psu[psuIndex] = new DeltaPsu((byte)(psuIndex + 1));
                        Tracer.WriteInfo("Delta PSU identified at slot-{0}, Model Number: {1}", psuIndex + 1, psuModelNumber);
                        break;
                    case PsuModel.EmersonWithLes:
                        Psu[psuIndex] = new EmersonPsu((byte)(psuIndex + 1), true);
                        Tracer.WriteInfo("Emerson with LES PSU identified at slot-{0}, Model Number: {1}", psuIndex + 1, psuModelNumber);
                        break;
                    case PsuModel.EmersonNonLes:
                        Psu[psuIndex] = new EmersonPsu((byte)(psuIndex + 1), false);
                        Tracer.WriteInfo("Emerson non-LES PSU identified at slot-{0}, Model Number: {1}", psuIndex + 1, psuModelNumber);
                        break;
                    default:
                        // Unknown PSUs
                        if (ConfigLoaded.ForceEmersonPsu)
                        {
                            // Force to Emerson PSU without LES to enable FW update methods in the EmersonPsu class to be called.
                            // This is useful when a previous FW update did not complete successfully.
                            // The PSU stays in bootloader mode and does not recognize the 
                            // MFR_MODEL command and will return all 0xFFs for this command.
                            Psu[psuIndex] = new EmersonPsu((byte)(psuIndex + 1), false);
                            Tracer.WriteWarning("Forcing PSU instantiation to Emerson non-LES PSU at slot-{0}, Model Number: {1}",
                                psuIndex + 1, psuModelNumber);
                        }
                        else
                        {
                            Psu[psuIndex] = new PsuBase((byte)(psuIndex + 1));
                            Tracer.WriteInfo("Unidentified PSU at slot-{0}, Model Number: {1}. Default to base PSU class.", psuIndex + 1, psuModelNumber);

                        }
                        break;
                }
                
                // Enable/disable Battery Extended Operation Mode
                Psu[psuIndex].SetBatteryExtendedOperationMode(ConfigLoaded.DatasafeOperationsEnabled ? true: false);
            }
        }

        /// <summary>
        /// Initialize watch dog timer
        /// </summary>
        private static void WatchDogTimerInitialize()
        {
            Tracer.WriteInfo("Initializing Watchdog Timer");
            Wdt.EnableWatchDogTimer();
        }

        internal static void Initialize()
        {
            // For each blade
            for (int i = 0; i < ConfigLoaded.Population; i++)
            {
                // Initialize lock object for performing atomic blade power operations
                locker[i] = new object();

                // Initialize Hard Power On/Off Switches (Blade Enable)
                BladePower[i] = new BladePowerSwitch(Convert.ToByte(i + 1));
                // Get Actual Power State
                BladePower[i].GetBladePowerState();

                // Create Blade Serial Console Metadata objects and initialize token, timestamp and associated member objects using class constructor
                BladeSerialMetadata.Add(i+1, new BladeSerialSessionMetadata(i+1));
            }

            // For each PSU
            for (int i = 0; i < ConfigLoaded.NumPsus; i++)
            {
                psuLock[i] = new object();
            }
            PsuInitialize();

            // For each serial port
            // Create Serial Console Port Metadata objects and initialize the sessiontoken and timestamp using the class constructor
            SerialConsolePortsMetadata = new SerialConsoleMetadata[(uint)ConfigLoaded.MaxSerialConsolePorts];
            for (int numPorts = 0; numPorts < (int)ConfigLoaded.MaxSerialConsolePorts; numPorts++)
            {
                SerialConsolePortsMetadata[numPorts] = new SerialConsoleMetadata(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(numPorts), DateTime.Now);
            }

            // Create AC Power Socket objects 
            AcPowerSockets = new AcSocket[(uint)ConfigLoaded.NumPowerSwitches];
            for (int numSockets = 0; numSockets < (int)ConfigLoaded.NumPowerSwitches; numSockets++)
            {
                AcPowerSockets[numSockets] = new AcSocket((byte)(numSockets + 1));
            }

            WatchDogTimerInitialize();

            // Create fan objects
            for (int fanId = 0; fanId < (int)ConfigLoaded.NumFans; fanId++)
            {
                Fans[fanId] = new Fan((byte)(fanId + 1));
            }
        }

        /// <summary>
        /// Gets the string value of state for the enum
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        internal static string GetStateName(byte deviceId)
        {
            lock (ChassisState.locker[deviceId - 1])
            {
                return Enum.GetName(typeof(BladeState), ChassisState.bladeState[deviceId - 1]);
            }
        }

        /// <summary>
        /// Gets the string name of the Blade Type (compute, JBOD, Xbox etc)
        /// </summary>
        /// <param name="bladeType"></param>
        /// <returns></returns>
        internal static string GetBladeTypeName(byte bladeType)
        {
            if (Enum.IsDefined(typeof(BladeTypeName), bladeType))
            {
                return Enum.GetName(typeof(BladeTypeName), bladeType);
            }
            else
            {
                return BladeTypeName.Unknown.ToString();
            }
        }

        /// <summary>
        /// gets the blade state for each blade
        /// </summary>
        /// <param name="bladeId"></param>
        internal static byte GetBladeState(byte bladeId)
        {
            lock (ChassisState.locker[bladeId - 1])
            {
                return bladeState[bladeId - 1];
            }
        }

        /// <summary>
        /// Sets the blade state for each blade
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="state"></param>
        internal static void SetBladeState(byte bladeId, byte state)
        {
            lock (ChassisState.locker[bladeId - 1])
            {
                bladeState[bladeId - 1] = state;
            }
        }

        /// <summary>
        /// Gets the blade type (compute, JBOD etc)
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns></returns>
        internal static byte GetBladeType(byte bladeId)
        {
            return BladeTypeCache[bladeId - 1];
        }

        /// <summary>
        /// Accessor Method to Set Psu Alert Signal
        /// </summary>
        internal static void SetPsuAlert(bool psuAlert)
        {
            PsuAlertActive = psuAlert;
        }

        /// <summary>
        /// Sets the energy storage status in the internal database
        /// This method updates the database in an atomic manner
        /// </summary>
        /// <param name="battId">The battery identifier.</param>
        /// <param name="newStatus">The new energy storage status.</param>
        /// <returns>True: Status set successfully.
        ///          False: Failed to set status.
        /// </returns>
        internal static bool SetEnergyStorageStatus(int battId, ChassisEnergyStorageStatus newStatus)
        {
            if (battId > ConfigLoaded.NumBatteries)
            {
                return false;
            }

            lock (energyStorageLock)
            {
                ChassisEnergyStorage[battId - 1] = newStatus;
            }
            return true;
        }

        /// <summary>
        /// Gets the energy storage status in the internal database
        /// This method reads the database in an atomic manner
        /// </summary>
        /// <param name="battId">The battery identifier.</param>
        /// <returns>Chassis energy storage status for the battery ID</returns>
        internal static ChassisEnergyStorageStatus GetEnergyStorageStatus(int battId)
        {
            ChassisEnergyStorageStatus currStatus = new ChassisEnergyStorageStatus(false, EnergyStorageState.Unknown, 0, 0, false);

            if (battId > ConfigLoaded.NumBatteries)
            {
                Tracer.WriteError(string.Format("BatteryId {0} is out of range", battId));
            }
            else
            {
                lock (energyStorageLock)
                {
                    currStatus = ChassisEnergyStorage[battId - 1];
                }
            }

            return currStatus;
        }

    }

    /// <summary>
    /// Chassis Energy Storage Status
    /// </summary>
    internal class ChassisEnergyStorageStatus
    {
        /// <summary>
        /// Battery present or not
        /// </summary>
        internal bool Present;

        /// <summary>
        /// Battery Storage State
        /// </summary>
        internal EnergyStorageState State;

        /// <summary>
        /// Percentage Charge
        /// </summary>
        internal double PercentCharge;

        /// <summary>
        /// Power output
        /// </summary>
        internal double PowerOut;

        /// <summary>
        /// Time Stamp
        /// </summary>
        internal bool FaultDetected;

        /// <summary>
        /// Set Energy Storage State
        /// </summary>
        internal ChassisEnergyStorageStatus(bool present, EnergyStorageState state, double percentCharge, double powerOut, bool faultDetected)
        {
            this.Present = present;
            this.State = state;
            this.PercentCharge = percentCharge;
            this.PowerOut = powerOut;
            this.FaultDetected = faultDetected;
        }
    }
}
