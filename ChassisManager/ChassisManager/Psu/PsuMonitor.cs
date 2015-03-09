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
    using System.Diagnostics;
    using System.Threading;
    using System.Collections.Generic;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    static class PsuMonitor
    {
        /// <summary>
        /// Times battery discharge
        /// </summary>
        internal static Stopwatch BatteryDischargeTimer = new Stopwatch();

        /// <summary>
        /// The psu poll timer. Used to keep track of the last time the PSU was polled
        /// </summary>
        private static int psuPollTimer = 0;

        /// <summary>
        /// The energy storage scaling factor in Joules
        /// </summary>
        private const int ENERGY_STORAGE_SCALING_JOULES = 50;

        /// <summary>
        /// Battery max output power in watts
        /// </summary>
        private const int BATT_POUT_MAX = 1425;

        /// <summary>
        /// Battery output power in watts for extended operation
        /// </summary>
        private const int BATT_POUT_EXTENDED = 75;
        
        /// <summary>
        /// Battery operating time at 100% load.
        /// Default value 65 secs from PSU specification.
        /// </summary>
        internal const int BATT_OP_TIME_100_LOAD = 65;

        /// <summary>
        /// Battery extended operating time at 75W.
        /// PSU will shutdown after this time.
        /// </summary>
        internal const int BATT_OP_TIME_75W_LOAD = 200;


        /// <summary>
        /// PSU Fault Status Codes
        /// </summary>
        enum PsuAlertFaultStatus
        {
            PsuNoOutputPower,
            PsuFault,
            PsuNotPresent,
            BatteryFault,
            BatteryStateUnknown,
            PsuClearFaultFailed,
            PsuPowerOff,
            PsuOnBattery,
            Unknown,
        }

        /// <summary>
        /// PSU Fault Classifications
        /// </summary>
        enum PsuAlertFaultType
        {
            PsuFailure,       // PSU failed to respond or has been turned off
            PsuFaultPresent,  // PSU STATUS_WORD shows fault present
            OnBattery,        // PSU is running on battery
            BatteryFault,     // Battery health status or fault indicator shows fault present
        }

        #region Internal Methods

        /// <summary>
        /// Monitors PSU and takes remedial actions.  Called by PSU_ALERT thread.
        /// </summary>
        internal static void MonitorPsuAlert()
        {
            if (ConfigLoaded.NumPsus > 0)
            {
                try
                {
                    while (true)
                    {
                        if (ChassisState.ShutDown)
                        {
                            Tracer.WriteWarning("Psu Monitoring Thread Exiting");
                            return;
                        }

                        // Monitor PSU
                        MonitorPsuAlertHelper();
                    }
                }
                catch (Exception ex)
                {
                    Tracer.WriteWarning("Chassis Manager MonitorPsuAlert thread encountered an exception " + ex);
                }
            }
        }

        /// <summary>
        /// PSU Monitoring Helper Function
        /// </summary>
        private static void MonitorPsuAlertHelper()
        {
            // times execution of each pass.
            Stopwatch timer = new Stopwatch(); 

            int timeTaken = 0;

            // determine whether to do efficient PSU ALERT monitoring, or
            // traditional PSU polling.
            if (ConfigLoaded.PsuAlertMonitorEnabled)
            {
                timer.Start();

                PsuAlertSignalResponse psuAlert = ChassisState.PsuAlert.GetPsuAlertSignal();

                if (psuAlert.CompletionCode == 0x00)
                {
                    // check if the global psu alert state needs to be updated.
                    if (psuAlert.PsuAlertActive != ChassisState.PsuAlertActive)
                        ChassisState.SetPsuAlert(psuAlert.PsuAlertActive);

                    if (psuAlert.PsuAlertActive)
                    {
                        // Step 1: When in PSU Alert, check and try resolve PSU Alerts.
                        Dictionary<byte, PsuAlertFaultStatus> psuRemediate = PsuInvestigateAndRemediate();

                        // Step 2: Update Blade DPC, if needed
                        PsuAlertUpdateBladeState(psuRemediate);
                    }
                    else
                    {
                        // Check to poll PSUs at slower polling interval
                        // or wait for pollTimer
                        PsuPollAndRemediate(psuPollTimer, out psuPollTimer);
                    }
                }
                else
                {
                    Tracer.WriteError("MonitorPsuAlert unable to get PsuAlert Signal. Defaulting to polling method. CompletionCode: 0x{0:X2}" +
                        psuAlert.CompletionCode);

                    // Check to poll PSUs at slower polling interval
                    // or wait for pollTimer
                    PsuPollAndRemediate(psuPollTimer, out psuPollTimer);
                }

                timeTaken = timer.Elapsed.Seconds;

                // increment polling timer.
                psuPollTimer += timeTaken;

                if (timeTaken < ConfigLoaded.PsuAlertPollInterval)  // psu alert poll
                {
                    // sleep until next pass.
                    Thread.Sleep(TimeSpan.FromSeconds(ConfigLoaded.PsuAlertPollInterval - timeTaken));
                }

                // reset the timer to zero;
                timer.Restart();
            }
            else
            {
                timer.Start();

                // check for PSU errors and attempt to resolve
                PsuInvestigateAndRemediate();

                // wait polling interval to expire.
                timeTaken = timer.Elapsed.Seconds;

                if (timeTaken < ConfigLoaded.PsuPollInterval)
                {
                    // sleep before next pass.
                    Thread.Sleep(TimeSpan.FromSeconds(ConfigLoaded.PsuPollInterval - timeTaken));
                }

                // reset the timer to zero;
                timer.Restart();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// PSU Polling and Remediation of PSU Failures.
        /// </summary>
        private static void PsuPollAndRemediate(int psuPollTimer, out int waitTime)
        {

            if (psuPollTimer >= ConfigLoaded.PsuPollInterval)
            {
                // Step 1: On regular poll interval check and try resolve PSU failures.
                Dictionary<byte, PsuAlertFaultStatus> psuRemediate = PsuInvestigateAndRemediate();

                // Step 2: Make sure DPC is not set if there are no PSU errors.
                PsuAlertUpdateBladeState(psuRemediate);

                // reset poll timer wait time.
                waitTime = 0;
            }
            else
            {
                waitTime = psuPollTimer;
            }
                   
        }

        /// <summary>
        /// Investigate and Remediate PSU issues
        /// </summary>
        private static Dictionary<byte, PsuAlertFaultStatus> PsuInvestigateAndRemediate()
        {
            // Step 1: Check PSU and battery status
            Dictionary<byte, PsuAlertFaultType> psuFaults = PsuAlertInvestigate();

            // Step 2: Remediate any PSU and battery issues
            Dictionary<byte, PsuAlertFaultStatus> psuRemediate = PsuAlertRemediate(psuFaults);

            // PsuFailure = true, causes Chassis LED to get illuminated
            if (psuRemediate.Count > 0)
            {
                ChassisState.PsuFailure = true;
            }
            else
            {
                ChassisState.PsuFailure = false;
            }

            return psuRemediate;
        }

        /// <summary>
        /// Checks for faults on each PSU
        /// </summary>
        private static Dictionary<byte, PsuAlertFaultType> PsuAlertInvestigate()
        {
            Dictionary<byte, PsuAlertFaultType> failures = new Dictionary<byte, PsuAlertFaultType>();

            // Check status for all PSU
            foreach (PsuBase psu in ChassisState.Psu)
            {
                // If firmware update is in progress, skip this PSU
                if (ChassisState.PsuFwUpdateInProgress[psu.PsuId - 1])
                {
                    continue;
                }

                lock (ChassisState.psuLock[psu.PsuId - 1])
                {
                    PsuStatusPacket psuStatus = psu.GetPsuStatus();
                    if (psuStatus.CompletionCode != CompletionCode.Success)
                    {
                        Tracer.WriteError("PsuAlertInvestigate: GetPsuStatus on PSU ({0}) failed with return code {1}",
                            psu.PsuId, psuStatus.CompletionCode);
                        failures.Add(psu.PsuId, PsuAlertFaultType.PsuFailure);
                    }
                    else
                    {
                        if (psuStatus.PsuStatus != (byte)Contracts.PowerState.ON)
                        {
                            // PSU is completely turned off
                            failures.Add(psu.PsuId, PsuAlertFaultType.PsuFailure);
                        }
                        else if ((ConfigLoaded.BatteryMonitoringEnabled) && (ChassisState.Psu[psu.PsuId - 1] is EmersonPsu))
                        {
                            // Check battery status for Emerson PSU
                            EmersonPsu emersonPsu = (EmersonPsu)psu;

                            // Get battery status and health
                            BatteryStatusPacket battStatus = emersonPsu.GetBatteryStatus();
                            EmersonPsu.BatteryHealthStatusPacket battHealth = emersonPsu.GetBatteryHealthStatus();

                            if ((battStatus.CompletionCode == CompletionCode.Success) &&
                                (battHealth.CompletionCode == CompletionCode.Success))
                            {
                                // Update chassis energy storage variables
                                bool batteryPresent = (battStatus.Presence == 1) ? true : false;
                                bool batteryFault = (battStatus.FaultDetected == 1) ? true : false;
                                EnergyStorageState battState = EnergyStorageState.Unknown;

                                if (batteryPresent)
                                {
                                    if (batteryFault)
                                    {
                                        // Battery Fault Detected. 
                                        failures.Add(emersonPsu.PsuId, PsuAlertFaultType.BatteryFault);
                                        Tracer.WriteError("PsuAlertInvestigate Battery Fault Detected. PsuId {0}", emersonPsu.PsuId);
                                    }
                                    else // if no fault detected, check if system is on battery.
                                    {
                                        // Determine battery state
                                        if (battHealth.Discharging == 0)
                                        {
                                            battState = EnergyStorageState.Charging;

                                            // We are charging, reset the timer.
                                            if (BatteryDischargeTimer.IsRunning)
                                            {
                                                BatteryDischargeTimer.Reset();
                                            }
                                        }
                                        else
                                        {
                                            // Emerson stated that we can have discharging even when on AC since the charger will have hysteresis.
                                            // Hence we need to check Discharging and Battery Power Output to determine if we are on Battery.
                                            if (battStatus.BatteryPowerOutput != 0)
                                            {
                                                battState = EnergyStorageState.Discharging;
                                            }
                                            else
                                            {
                                                battState = EnergyStorageState.Floating;
                                            }
                                        }

                                        if (battState == EnergyStorageState.Discharging)
                                        {
                                            // Start the timer if not already running.
                                            if (!BatteryDischargeTimer.IsRunning)
                                            {
                                                BatteryDischargeTimer.Start();
                                            }

                                            // Psu Battery is Discharging. System is on battery.
                                            // Log it as a failure for processing in PsuAlertRemediate() which is called outside this method
                                            failures.Add(emersonPsu.PsuId, PsuAlertFaultType.OnBattery);
                                            Tracer.WriteInfo("PsuAlertInvestigate Psu Battery discharging.  PsuId {0}", emersonPsu.PsuId);
                                        }
                                    }
                                }
                                else
                                {
                                    Tracer.WriteInfo("PsuAlertInvestigate, no battery present for Psu: {0}", emersonPsu.PsuId);
                                }

                                // Update chassis energy storage values
                                ChassisEnergyStorageStatus chassisEnergyStatus =
                                    new ChassisEnergyStorageStatus(batteryPresent, battState,
                                        battStatus.BatteryChargeLevel, battStatus.BatteryPowerOutput, batteryFault);
                                if (!ChassisState.SetEnergyStorageStatus(emersonPsu.PsuId, chassisEnergyStatus))
                                {
                                    Tracer.WriteError(
                                        string.Format("PsuAlertInvestigate: SetEnergyStorageStatus failed for BatteryId {0}", emersonPsu.PsuId));
                                }
                            }
                            else
                            {
                                // Failed to get battery status or health. Log as battery fault
                                failures.Add(emersonPsu.PsuId, PsuAlertFaultType.BatteryFault);
                                Tracer.WriteError("PsuAlertInvestigate failed to get Battery Status. PsuId {0} Status Completion Code: {1}  Health Completion Code: {2}",
                                    emersonPsu.PsuId, battStatus.CompletionCode, battHealth.CompletionCode);
                            }
                        }

                        // If PSU is on and there are no battery faults, check if other faults are present
                        // Add PSU to failure list so that we can log it in PsuAlertRemediate()
                        if ((!failures.ContainsKey(psu.PsuId)) && (psuStatus.FaultPresent))
                        {
                            failures.Add(psu.PsuId, PsuAlertFaultType.PsuFaultPresent);
                        }
                    }
                } // lock...
            } // foreach...

            return failures;
        }

        /// <summary>
        /// Attempt to resolve Psu Faults
        /// </summary>
        private static Dictionary<byte, PsuAlertFaultStatus> PsuAlertRemediate(Dictionary<byte, PsuAlertFaultType> psuFailures)
        {
            Dictionary<byte, PsuAlertFaultStatus> failedPsu = new Dictionary<byte, PsuAlertFaultStatus>();

            foreach (KeyValuePair<byte, PsuAlertFaultType> psu in psuFailures)
            {
                // If firmware update is in progress, skip this PSU
                if (ChassisState.PsuFwUpdateInProgress[psu.Key - 1])
                {
                    continue;
                }

                lock (ChassisState.psuLock[psu.Key - 1])
                {
                    // Log PSU faults
                    ChassisState.Psu[psu.Key - 1].LogPsuFaultStatus();

                    // Clear PSU faults, which will clear PSU_ALERT
                    CompletionCode clearAlert = ClearPsuFault(psu.Key);
                    if (clearAlert != CompletionCode.Success)
                    {
                        // PSU clear faults failed. Log failure and continue to next PSU.
                        failedPsu.Add(psu.Key, PsuAlertFaultStatus.PsuClearFaultFailed);
                        Tracer.WriteError("PsuAlertRemediate: ClearPsuFault failed on PsuId: {0}", psu.Key);
                        continue;
                    }

                    if (psu.Value == PsuAlertFaultType.PsuFailure)
                    {
                        // Check that the PSU is on
                        PsuStatusPacket psuStatus = ChassisState.Psu[psu.Key - 1].GetPsuStatus();
                        if (psuStatus.CompletionCode != CompletionCode.Success)
                        {
                            failedPsu.Add(psu.Key, PsuAlertFaultStatus.PsuFault);
                            Tracer.WriteError("PsuAlertRemediate: GetPsuStatus on PSU ({0}) failed with return code {1}",
                                psu.Key, psuStatus.CompletionCode);
                        }
                        else
                        {
                            if (psuStatus.PsuStatus == (byte)Contracts.PowerState.ON)
                            {
                                // Check PSU power output
                                PsuPowerPacket power = ChassisState.Psu[psu.Key - 1].GetPsuPower();
                                if ((power.CompletionCode == CompletionCode.Success) && (power.PsuPower != 0))
                                {
                                    Tracer.WriteInfo("PsuStatus clear faults succeeded.  Psu: {0} drawing power: {1} Watts",
                                        psu.Key, power.PsuPower);
                                }
                                else
                                {
                                    // PSU is not outputting power.
                                    failedPsu.Add(psu.Key, PsuAlertFaultStatus.PsuNoOutputPower);
                                    Tracer.WriteError("PsuAlertRemediate failed Psu.  PsuId: {0} Psu Error State: {1}",
                                        psu.Key, PsuAlertFaultStatus.PsuNoOutputPower.ToString());
                                }
                            }
                            else
                            {
                                // PSU is turned off.
                                failedPsu.Add(psu.Key, PsuAlertFaultStatus.PsuPowerOff);
                                Tracer.WriteError("PsuAlertRemediate failed Psu.  PsuId: {0} Psu Error State: {1}",
                                    psu.Key, PsuAlertFaultStatus.PsuPowerOff.ToString());
                            }
                        }
                    }
                    else if ((ConfigLoaded.BatteryMonitoringEnabled) && (ChassisState.Psu[(psu.Key - 1)] is EmersonPsu))
                    {
                        // convert psu from base class object
                        EmersonPsu emersonPsu = (EmersonPsu)ChassisState.Psu[(psu.Key - 1)];

                        if (psu.Value == PsuAlertFaultType.BatteryFault)
                        {
                            // clear battery fault status
                            CompletionCode clearFault = emersonPsu.ClearBatteryFaultIndicator();

                            if (clearFault == CompletionCode.Success)
                            {
                                EmersonPsu.BatteryFaultIndicatorPacket faultIndicator = emersonPsu.GetBatteryFaultIndicator();

                                if (faultIndicator.BatteryFault == 1)
                                {
                                    if (!failedPsu.ContainsKey(emersonPsu.PsuId))
                                    {
                                        // Psu Clear faults did not succeed.
                                        failedPsu.Add(psu.Key, PsuAlertFaultStatus.BatteryFault);
                                    }
                                    Tracer.WriteError("PsuAlertRemediate failed to clear battery fault. PsuId: {0} Battery Error State: {1}",
                                        psu.Key, PsuAlertFaultStatus.BatteryFault.ToString());
                                }
                            }
                        }
                        else if (psu.Value == PsuAlertFaultType.OnBattery && ConfigLoaded.NumBatteries > 0)
                        {
                            // Check if we need to trigger delegate to process battery status
                            if (ConfigLoaded.ProcessBatteryStatus)
                            {
                                double sumBatteryChargeLevel = 0;
                                ChassisEnergyStorageStatus status = null;

                                // list to store battery charge levels
                                List<string> batteryStates = new List<string>();

                                // battery present or not, set to true if even one battery is present.
                                // default to false
                                bool isBatteryPresent = false;

                                // Calculate average battery charge level
                                for (int index = 1; index <= ConfigLoaded.NumBatteries; index++)
                                {
                                    status = ChassisState.GetEnergyStorageStatus((byte)index);

                                    // Add to the list battery charge levels
                                    batteryStates.Add(status.State.ToString());

                                    // If even one battery is present, set flag to true
                                    if (status.Present)
                                    {
                                        isBatteryPresent = true;
                                    }

                                    // If battery state is not unknown, add up the charge level.
                                    if (status.State != EnergyStorageState.Unknown)
                                    {
                                        sumBatteryChargeLevel += status.PercentCharge;
                                    }
                                }

                                double avgChargeLevel = (sumBatteryChargeLevel / ConfigLoaded.NumBatteries);

                                // Process battery status if battery discharge time is greater than the allowed discharge time 
                                // from app.config( default 35 seconds) or Average battery charge level is below a given threshold value.
                                if (BatteryDischargeTimer.Elapsed > new System.TimeSpan(0, 0, ConfigLoaded.BatteryDischargeTimeInSecs)
                                    || avgChargeLevel < ConfigLoaded.BatteryChargeLevelThreshold)
                                {
                                    // Invoke method to trigger NVDIMM backup for critical battery status
                                    ThreadPool.QueueUserWorkItem(new WaitCallback(ChassisManagerInternal.ProcessCriticalBatteryStatus));
                                }

                                // Calculate backup energy available per blade and per NVDIMM
                                double bladeEnergy = (ConfigLoaded.NumPsus * BATT_POUT_MAX * BATT_OP_TIME_100_LOAD * avgChargeLevel) /
                                    ConfigLoaded.Population;
                                double nvdimmEnergy = (ConfigLoaded.NumPsus * BATT_POUT_EXTENDED * BATT_OP_TIME_75W_LOAD) /
                                    (ConfigLoaded.Population * ConfigLoaded.NvDimmPerBlade);
                                // Scale the values
                                bladeEnergy = bladeEnergy / ENERGY_STORAGE_SCALING_JOULES;
                                nvdimmEnergy = nvdimmEnergy / ENERGY_STORAGE_SCALING_JOULES;

                                // Send battery status to BMC, check returned completion code for success
                                Dictionary<byte, CompletionCode> results = WcsBladeFacade.BroadcastSetEnergyStorage
                                    (isBatteryPresent, GetBatteryStateToBroadcast(batteryStates), ENERGY_STORAGE_SCALING_JOULES, (ushort)bladeEnergy, (byte)nvdimmEnergy);

                                // Check if broadcast failed for any blade, if yes log error.
                                for (int index = 1; index <= ConfigLoaded.Population; index++)
                                {
                                    CompletionCode code;

                                    if (results.TryGetValue((byte)index, out code))
                                    {
                                        // If completion code returned is not success
                                        if (code != CompletionCode.Success)
                                        {
                                            Tracer.WriteError("PsuMonitor: ProcessBatteryStatus: " +
                                                "Failed to update battery status to BMC for blade: " + index +
                                                ", completion code returned: " + code);
                                        }
                                    }
                                    else
                                    {
                                        // If blade entry does not exist.
                                        Tracer.WriteError("PsuMonitor: ProcessBatteryStatus : " +
                                            "Failed to update battery status to BMC for blade: " + index);
                                    }
                                }
                            }
                        }
                    }
                } // lock...
            } // foreach...

            return failedPsu;
        }

        private static void PsuAlertUpdateBladeState(Dictionary<byte, PsuAlertFaultStatus> psuError)
        {
            if (ConfigLoaded.DpcAutoDeassert)
            {
                if (psuError.Count == 0)
                {

                    RemoveAllBladeDpc();
                }

                // TODO: Consider adding a backoff.
                // TODO: Refine remediation actions for removing DPC in later revision
            }
        }

        /// <summary>
        /// Get the battery state to broadcast given the list of current battery states.
        /// </summary>
        /// <param name="batteryStates">List of current battery states</param>
        /// <returns>Battery state to broadcast to BMC</returns>
        private static EnergyStorageState GetBatteryStateToBroadcast(List<string> batteryStates)
        {
            // Determine battery state to broadcast to BMC, first look for state Discharging, then Charging, then Floating, else send Unknown.
            if (batteryStates.Contains(EnergyStorageState.Discharging.ToString()))
            {
                return EnergyStorageState.Discharging;
            }
            else if (batteryStates.Contains(EnergyStorageState.Charging.ToString()))
            {
                return EnergyStorageState.Charging;
            }
            else if (batteryStates.Contains(EnergyStorageState.Floating.ToString()))
            {
                return EnergyStorageState.Floating;
            }
            else
            {
               return EnergyStorageState.Unknown;
            }
        }

        /// <summary>
        /// Method removes DPC from powered on servers
        /// </summary>
        private static void RemoveAllBladeDpc()
        {
            for (int bladeIndex = 0; bladeIndex < ConfigLoaded.Population; bladeIndex++)
            {
                byte bladeId = (byte)(bladeIndex + 1);

                if (ChassisState.BladePower[bladeIndex].GetCachedBladePowerState().BladePowerState == 0x01) // PowerState.On
                {
                    // Spec 1.86 adds optimization there by current state of PsuAlert
                    // does not need to be queried to disable 
                    PsuAlert psuAlert = WcsBladeFacade.GetPsuAlert(bladeId);

                    if (psuAlert.CompletionCode == 0x00)
                    {
                        BmcPsuAlertAction bmcAction = BmcPsuAlertAction.DpcOnly;

                        if (psuAlert.BmcProchotEnabled)
                        {
                            bmcAction = BmcPsuAlertAction.ProcHotAndDpc;
                        }

                        // disable DPC.
                        WcsBladeFacade.ActivatePsuAlert(bladeId, psuAlert.AutoProchotEnabled, bmcAction, true);
                    }
                }
            }
        }

        /// <summary>
        /// Clears Psu Fault condition
        /// </summary>
        private static CompletionCode ClearPsuFault(byte psuId)
        {
            CompletionCode clearFaultCompletionCode = ChassisState.Psu[psuId - 1].SetPsuClearFaults();
            if (clearFaultCompletionCode != CompletionCode.Success)
            {
                Tracer.WriteError("ClearPsuFault for PSU {0} failed. Completion code({1})", psuId, clearFaultCompletionCode);
            }
            else
            {
                Tracer.WriteInfo("ClearPsuFault for PSU {0} succeeded.", psuId);
            }

            return clearFaultCompletionCode;
        }

        #endregion

    }
}
