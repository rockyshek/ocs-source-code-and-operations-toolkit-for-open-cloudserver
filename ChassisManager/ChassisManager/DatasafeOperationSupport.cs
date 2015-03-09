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
    using Microsoft.GFS.WCS.Contracts;
    using System.Collections.Generic;
    using System.Threading;

    static class DatasafeOperationSupport
    {
        # region Internal variables

        /// <summary>
        /// Handle for ThreadPool.RegisterWaitForSingleObject
        /// </summary>
        public class RegisteredHandle
        {
            public RegisteredWaitHandle RegisterHandle = null;
        }

        /// <summary>
        /// Handle for controlling 
        /// </summary>
        private static RegisteredHandle handle = new RegisteredHandle();

        private class DataSafeControl
        {
            internal DatasafeActions action;
            internal DateTime timeInAction;
        }

        /// <summary>
        /// The number of blades in the chassis
        /// </summary>
        private static int numBlades = ConfigLoaded.Population;

        /// <summary>
        /// Indicates for each blade the action that needs to be performed once backup is complete
        /// </summary>
        private static Dictionary<int, DataSafeControl> bladeDatasafeControl = new Dictionary<int, DataSafeControl>();

        // Per-blade lock to ensure atomic datasafe operation
        // Note that blade IDs start from 1, but this array using 0-based indexing for the blades
        private static readonly object[] bladeActionLock = new object[numBlades];

        // Wait handle to coordinate executing the specified delegate method
        private static AutoResetEvent pendingDatasafeEvent = new AutoResetEvent(false);

        private static int forceTriggerTimeoutInSecs= 200;
        private static int forcePowerActionTimeoutInSecs = 400;

        #endregion

        static DatasafeOperationSupport()
        {
            try
            {
                // Initialize member objects
                for (int i = 0; i < ConfigLoaded.Population; i++)
                {
                    bladeActionLock[i] = new object();

                    DataSafeControl defaultDatasafeControlValue = new DataSafeControl();
                    defaultDatasafeControlValue.action = DatasafeActions.DoNothing;
                    defaultDatasafeControlValue.timeInAction = DateTime.Now;
                    bladeDatasafeControl.Add(i + 1, defaultDatasafeControlValue);
                }

                handle.RegisterHandle = ThreadPool.RegisterWaitForSingleObject(
                  pendingDatasafeEvent, new WaitOrTimerCallback(LoopAndProcessPendingDatasafeCommands), handle, -1, false);
            }
            catch (Exception ex)
            {
                Tracer.WriteError("DatasafeOperationSupport static constructor encountered an exception " + ex);
            }
        }

        # region Internal methods

        /// <summary>
        /// Processes the datasafe action.
        /// This method will be called by each of DataSafe REST commands in ChassisManager.cs to process the Datasafe commands
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        internal static DatasafeBladeStatus ProcessDatasafeAction(int bladeId, DatasafeActions action)
        {
            DatasafeBladeStatus response = new DatasafeBladeStatus();

            lock (bladeActionLock[bladeId - 1])
            {
                // If no datasafe operation is pending on this blade, then proceed to process the new datasafe operation
                if (GetDatasafeAction(bladeId).action == DatasafeActions.DoNothing || action==DatasafeActions.DoNothing)
                {
                    // Process datasafe action
                    response = ProcessDatasafeActionHelper(bladeId, action);

                    // If the datasafe operation is asynchronous or pending
                    if (response.status == DatasafeCommandsReturnStatus.CommandDelayed)
                    {
                        // Set the new datasafe action for this blade 
                        SetDatasafeAction(bladeId, action);
                        // Also, trigger the delegate to process the pending/asynchronous datasafe operation
                        pendingDatasafeEvent.Set();
                    }
                    else // If the datasafe operation is synchronous, then return the response packet
                    {
                        // TODO: Should we set the datasafe action to DoNothing here
                        // SetDatasafeAction(bladeId, DatasafeActions.DoNothing);
                    }
                }
                else // There is already an existing pending datasafe operation for this blade. Do not accept new commands. Return failure
                {
                    response.status = DatasafeCommandsReturnStatus.CommandFailed;
                }

                return response;
            }
        }

        # endregion

        # region Private Methods

        /// <summary>
        /// Sets the datasafe action.
        /// Caller is responsible for holding bladeActionLock before calling this method
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="action">The datasafe action.</param>
        private static void SetDatasafeAction(int bladeId, DatasafeActions action, DateTime pendingSince = default(DateTime))
        {
            try
            {
                // Set datasafe action for this blade
                if (bladeDatasafeControl.ContainsKey(bladeId))
                {
                    bladeDatasafeControl[bladeId].action = action;
                    if (DateTime.Compare(pendingSince, default(DateTime)) == 0)
                    {
                        bladeDatasafeControl[bladeId].timeInAction = DateTime.Now;
                    }
                    else
                    {
                        bladeDatasafeControl[bladeId].timeInAction = pendingSince;
                    }

                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("DatasafeOperationSupport SetDatasafeAction encountered an exception " + ex);
            }
        }

        /// <summary>
        /// Gets the datasafe action.
        /// Caller is responsible for holding bladeActionLock before calling this method
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <returns>The current datasafe action for the blade.</returns>
        private static DataSafeControl GetDatasafeAction(int bladeId)
        {
            DataSafeControl datasafeAction = null;
            try
            {
                if (!bladeDatasafeControl.TryGetValue(bladeId, out datasafeAction))
                {
                    Tracer.WriteWarning("GetDatasafeAction: dictionary object corresponding to bladeId({0}) not found.", bladeId);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("DatasafeOperationSupport GetDatasafeAction encountered an exception " + ex);
            }
            return datasafeAction;
        }

        /// <summary>
        /// Single point entry method for all datasafe command execution and ADR triggers via CM (including battery panic)
        /// Per-blade actions are executed atomically
        /// </summary>
        /// <param name="bladeId">target bladeId</param>
        /// <param name="command">Command that triggered ADR</param>
        /// <param name="command">Use BatteryLowCapacityPanic when battery discharging and critical capacity</param>
        /// <param name="command">Use DoNothing for default and when battery transitions from discharging to charging</param>
        /// Return DatasafeCommandsReturnStatus.CommandExecuted if command successfully executed 
        /// Return DatasafeCommandsReturnStatus.CommandDelayed if command successfully delayed
        /// Return DatasafeCommandsReturnStatus.CommandFailed if command execution failed
        private static DatasafeBladeStatus ProcessDatasafeActionHelper(int bladeId, DatasafeActions command)
        {
            DatasafeBladeStatus response = new DatasafeBladeStatus();
            response.status = DatasafeCommandsReturnStatus.CommandFailed;

            // Backup status variables
            NvdimmBackupStatus backupStatus = NvdimmBackupStatus.Unknown;
            int nvdimmPresentTimeRemaining;
            byte adrCompleteDelay;
            byte nvdimmPresentPowerOffDelay;

            // Get Backup status
            bool getBackupStatusOK = GetBackupStatus(bladeId, out backupStatus, out nvdimmPresentTimeRemaining,
                out adrCompleteDelay, out nvdimmPresentPowerOffDelay);

            if (getBackupStatusOK)
            {
                response.isBackupPending = backupStatus == NvdimmBackupStatus.Pending ? true : false;
                response.remainingBackupDuration = nvdimmPresentTimeRemaining;
                response.status = DatasafeCommandsReturnStatus.CommandExecuted;
                Tracer.WriteInfo("ProcessDatasafeActionHelper:  bladeId({0}), action({1}), nvdimmPresentTimeRemaining({2})", bladeId, command.ToString(), nvdimmPresentTimeRemaining);
            }
            else
            {
                Tracer.WriteWarning("ProcessDatasafeActionHelper: GetBackupStatus failed for bladeId({0}), action({1})", bladeId, command.ToString());
                response.status = DatasafeCommandsReturnStatus.CommandFailed;
            }

            // DoNothing command will be used to get status of datasafe backup operation 
            if (command == DatasafeActions.DoNothing)
            {
                return response;
            }

            switch (command)
            {
                case DatasafeActions.PowerCycle:
                case DatasafeActions.PowerOff:
                case DatasafeActions.BladeOff:
                case DatasafeActions.BatteryLowCapacityPanic:

                    // If backup is complete (or timeout), execute the blade power command 
                    if (backupStatus == NvdimmBackupStatus.Complete)
                    {
                        Tracer.WriteInfo("ProcessDatasafeActionHelper: bladeId({0}), Execute action({1})", bladeId, command.ToString());
                        response.status = ExecuteBladePowerCommand(bladeId, command) ? DatasafeCommandsReturnStatus.CommandExecuted :
                            DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // If 'forcePowerActionTimeoutInSecs' time has elapsed since the initiation of backup, force blade power action - MAY LOSE DATA
                    else if (GetDatasafeAction(bladeId).timeInAction != DateTime.MaxValue && GetDatasafeAction(bladeId).timeInAction.Add(TimeSpan.FromSeconds(forcePowerActionTimeoutInSecs)) < DateTime.Now)
                    {
                        Tracer.WriteWarning("ProcessDatasafeActionHelper: bladeId({0}) datasafe operation pending for ({1})s.. Forcing nvdimm power action.",
                            bladeId, forcePowerActionTimeoutInSecs);
                        response.status = ExecuteBladePowerCommand(bladeId, command) ? DatasafeCommandsReturnStatus.CommandExecuted :
                           DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // Backup is NOT pending OR Not sure if backup is pending (or timeout), send ADR trigger and initiate backup
                    else if (backupStatus == NvdimmBackupStatus.NotPending || backupStatus == NvdimmBackupStatus.Unknown)
                    {
                        Tracer.WriteInfo("ProcessDatasafeActionHelper: SetNvdimmTrigger bladeId({0}), action({1})", bladeId, command.ToString());
                        response.status = WcsBladeFacade.SetNvDimmTrigger((byte)bladeId, Ipmi.NvDimmTriggerAction.PchAdrGpi, true, adrCompleteDelay,
                            nvdimmPresentPowerOffDelay) ? DatasafeCommandsReturnStatus.CommandDelayed : DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // Backup has been pending for a long time -- 'forceTriggerTimeoutInSecs' -- force NVDIMM trigger
                    else if (backupStatus == NvdimmBackupStatus.Pending &&
                         (GetDatasafeAction(bladeId).timeInAction != DateTime.MaxValue && GetDatasafeAction(bladeId).timeInAction.Add(TimeSpan.FromSeconds(forceTriggerTimeoutInSecs)) < DateTime.Now))
                    {
                        Tracer.WriteWarning("ProcessDatasafeActionHelper: bladeId({0}) datasafe operation pending for ({1})s.. Forcing nvdimm trigger.",
                            bladeId, forceTriggerTimeoutInSecs);
                        response.status = WcsBladeFacade.SetNvDimmTrigger((byte)bladeId, Ipmi.NvDimmTriggerAction.PchAdrGpi, true, adrCompleteDelay,
                            nvdimmPresentPowerOffDelay) ? DatasafeCommandsReturnStatus.CommandDelayed : DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // Backup is pending, send a command delayed message back to the user
                    else if (backupStatus == NvdimmBackupStatus.Pending)
                    {
                        Tracer.WriteInfo("ProcessDatasafeActionHelper: bladeId({0}), delay action({1})", bladeId, command.ToString());
                        response.status = DatasafeCommandsReturnStatus.CommandDelayed;
                    }
                    else
                    {
                        Tracer.WriteWarning("ProcessDatasafeActionHelper: bladeId({0}) Unreachable code action({1}) backupstatus({2})", 
                            bladeId, command.ToString(), backupStatus.ToString());
                    }

                    break;

                case DatasafeActions.PowerOn:
                case DatasafeActions.BladeOn:

                    // Backup is complete, NOT pending OR Not sure if backup is pending, execute on command
                    if (backupStatus == NvdimmBackupStatus.Complete || backupStatus == NvdimmBackupStatus.NotPending || backupStatus == NvdimmBackupStatus.Unknown)
                    {
                        Tracer.WriteInfo("ProcessDatasafeActionHelper: bladeId({0}), Execute action({1})", bladeId, command.ToString());
                        // Just execute command for PowerOn and BladeOn. 
                        response.status = ExecuteBladePowerCommand(bladeId, command) ? DatasafeCommandsReturnStatus.CommandExecuted :
                                DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // Backup has been pending for a long time -- 'forcePowerActionTimeoutInSecs' -- force blade power action - MAY LOSE DATA
                    else if (GetDatasafeAction(bladeId).timeInAction != DateTime.MaxValue &&
                        GetDatasafeAction(bladeId).timeInAction.Add(TimeSpan.FromSeconds(forcePowerActionTimeoutInSecs)) < DateTime.Now)
                    {
                        Tracer.WriteWarning("ProcessDatasafeActionHelper: bladeId({0}) datasafe operation pending for ({1})s.. Forcing nvdimm power action.",
                            bladeId, forcePowerActionTimeoutInSecs);
                        response.status = ExecuteBladePowerCommand(bladeId, command) ? DatasafeCommandsReturnStatus.CommandExecuted :
                           DatasafeCommandsReturnStatus.CommandFailed;
                    }
                    // If backup is pending, send a command delayed message back to the user
                    else if(backupStatus==NvdimmBackupStatus.Pending)
                    {
                        Tracer.WriteInfo("ProcessDatasafeActionHelper: bladeId({0}), delay action({1})", bladeId, command.ToString());
                        response.status = DatasafeCommandsReturnStatus.CommandDelayed;
                    }
                    else
                    {
                        Tracer.WriteWarning("ProcessDatasafeActionHelper: bladeId({0}) Unreachable code action({1}) backupstatus({2})",
                            bladeId, command.ToString(), backupStatus.ToString());
                    }

                    break;

                case DatasafeActions.EnableDatasafe:

                    WcsBladeFacade.SetNvDimmTrigger((byte)bladeId, Ipmi.NvDimmTriggerAction.PchAdrGpi, false,
                            adrCompleteDelay, nvdimmPresentPowerOffDelay);
                    break;

                case DatasafeActions.DisableDatasafe:

                    WcsBladeFacade.SetNvDimmTrigger((byte)bladeId, Ipmi.NvDimmTriggerAction.Disabled, false,
                            adrCompleteDelay, nvdimmPresentPowerOffDelay);
                    break;

                default:
                    break;
            }

            return response;
        }

        /// <summary>
        /// Gets the backup status.
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="backupStatus">The backup status.</param>
        /// <param name="nvdimmPresentTimeRemaining">The NVDIMM present time remaining.</param>
        /// <param name="adrCompleteDelay">The ADR complete delay.</param>
        /// <param name="nvdimmPresentPowerOffDelay">The NVDIMM present power off delay.</param>
        /// <returns>true: Backup status obtained successfully from blade.
        ///          false: Failed to obtain backup status from blade</returns>
        private static bool GetBackupStatus(int bladeId, out NvdimmBackupStatus backupStatus,
            out int nvdimmPresentTimeRemaining, out byte adrCompleteDelay, out byte nvdimmPresentPowerOffDelay)
        {
            // Get ADR trigger status. SendReceive() in SerialPortManager will disable safe mode
            // for NVDIMM commands so this command will not be blocked by any existing serial sessions
            NvDimmTrigger getTrigger = WcsBladeFacade.GetNvDimmTrigger((byte)bladeId);

            // Command executed successfully
            if (getTrigger.CompletionCode == 0x00)
            {
                // Determine backup status
                if (getTrigger.AdrComplete == 0x01)
                {
                    if (getTrigger.NvdimmPresentTimeRemaining == 0)
                    {
                        backupStatus = NvdimmBackupStatus.Complete;
                    }
                    else
                    {
                        backupStatus = NvdimmBackupStatus.Pending;
                    }
                }
                else
                {
                    backupStatus = NvdimmBackupStatus.NotPending;
                }

                nvdimmPresentTimeRemaining = getTrigger.NvdimmPresentTimeRemaining;
                adrCompleteDelay = (byte)getTrigger.AdrCompleteDelay;
                nvdimmPresentPowerOffDelay = (byte)getTrigger.NvdimmPresentPowerOffDelay;

                Tracer.WriteInfo("GetBackupStatus timeRemain ({0}), adrCompDelay ({1}), powerOffDelay ({2})",
                    nvdimmPresentTimeRemaining, adrCompleteDelay, nvdimmPresentPowerOffDelay);

                return true;
            }
            else
            {
                backupStatus = NvdimmBackupStatus.Unknown;
                nvdimmPresentTimeRemaining = ConfigLoaded.NvDimmPresentPowerOffDelay;
                adrCompleteDelay = (byte)ConfigLoaded.AdrCompleteDelay;
                nvdimmPresentPowerOffDelay = (byte)ConfigLoaded.NvDimmPresentPowerOffDelay;

                return false;
            }
        }

        /// <summary>
        /// Executes the blade power command for power control
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="command">AdrTrigger command</param>
        /// <returns></returns>
        private static bool ExecuteBladePowerCommand(int bladeId, DatasafeActions command)
        {
            switch (command)
            {
                case DatasafeActions.BatteryLowCapacityPanic:
                    return BladePowerCommands.PowerOff(bladeId);
                case DatasafeActions.PowerOff:
                    return BladePowerCommands.PowerOff(bladeId);
                case DatasafeActions.PowerOn:
                    return BladePowerCommands.PowerOn(bladeId);
                case DatasafeActions.PowerCycle:
                    return BladePowerCommands.PowerCycle(bladeId, 0);
                case DatasafeActions.BladeOff:
                    return BladePowerCommands.BladeOff(bladeId);
                case DatasafeActions.BladeOn:
                    return BladePowerCommands.BladeOn(bladeId);
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// This method loops to process pending DataSafe commands until
        /// all pending commands are processed.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="timedOut"></param>
        private static void LoopAndProcessPendingDatasafeCommands(Object state, bool timedOut)
        {
            bool moreAction = true;
            DataSafeControl currentBladeControl;
            DatasafeBladeStatus response;

            // Loop and process pending datasafe commands while there are remaining actions (datasafe commands)
            while (moreAction)
            {
                moreAction = false;
                for (int bladeId = 1; bladeId <= numBlades; bladeId++)
                {
                    lock (bladeActionLock[bladeId - 1])
                    {
                        currentBladeControl = GetDatasafeAction(bladeId);
                        if (currentBladeControl.action != DatasafeActions.DoNothing)
                        {
                            // Execute datasafe action for the blade
                            response = ProcessDatasafeActionHelper(bladeId, currentBladeControl.action);

                            if (response.status == DatasafeCommandsReturnStatus.CommandDelayed)
                            {
                                // Indicate that there are more actions to process
                                moreAction = true;
                            }
                            else if (response.status == DatasafeCommandsReturnStatus.CommandExecuted)
                            {
                                // Command executed successfully. Reset action for the blade
                                Tracer.WriteInfo("LoopAndProcessPendingDatasafeCommands: Action {0} executed for bladeId {1}",
                                    currentBladeControl.action, bladeId);
                                SetDatasafeAction(bladeId, DatasafeActions.DoNothing, DateTime.MaxValue);
                            }
                            else if (response.status == DatasafeCommandsReturnStatus.CommandFailed)
                            {
                                // If command failed, something has gone wrong. Clear action for the blade and log error
                                Tracer.WriteError("LoopAndProcessPendingDatasafeCommands: Action {0} failed for bladeId {1}",
                                    currentBladeControl.action, bladeId);
                                SetDatasafeAction(bladeId, DatasafeActions.DoNothing, DateTime.MaxValue);
                            }
                        }
                    }
                }

                if (ChassisState.ShutDown)
                {
                    // The state object must be cast to the correct type, because the 
                    // signature of the WaitOrTimerCallback delegate specifies type 
                    // Object.
                    RegisteredHandle handle = (RegisteredHandle)state;

                    // If the callback method executes because the WaitHandle is 
                    // signaled, stop future execution of the callback method 
                    // by unregistering the WaitHandle. 
                    if (handle.RegisterHandle != null)
                        handle.RegisterHandle.Unregister(null);

                    break;
                }

                Thread.Sleep(1000);
            }
        }

        # endregion
    }

    /// <summary>
    /// Datasafe commands return status
    /// </summary>
    internal enum DatasafeCommandsReturnStatus : byte
    {
        CommandExecuted = 0x0,
        CommandDelayed = 0x1,
        CommandFailed = 0x2,
    }

    /// <summary>
    /// Datasafe command processing status
    /// </summary>
    internal class DatasafeBladeStatus
    {
        internal DatasafeCommandsReturnStatus status = DatasafeCommandsReturnStatus.CommandFailed;
        internal bool isBackupPending = false;
        internal int remainingBackupDuration = 0;
    }

    /// <summary>
    /// Datasafe Actions
    /// </summary>
    internal enum DatasafeActions
    {
        /// <summary>
        /// Do-nothing command  (Default and value should be 0) 
        /// </summary>
        DoNothing = 0,

        /// <summary>
        /// Trigger ADR due to low battery capacity
        /// </summary>
        BatteryLowCapacityPanic = 1,

        /// <summary>
        /// Hard power OFF (Blade Enable)
        /// </summary>
        PowerOff = 2,

        /// <summary>
        /// Hard power On (Blade Enable)
        /// </summary>
        PowerOn = 3,

        /// <summary>
        /// IPMI Power cycle
        /// </summary>
        PowerCycle = 4,

        /// <summary>
        /// IPMI Soft Power Off
        /// </summary>
        BladeOff = 5,

        /// <summary>
        /// IPMI Soft Power On
        /// </summary>
        BladeOn = 6,

        /// <summary>
        /// Enable datasafe operations (ADR trigger) 
        /// </summary>
        EnableDatasafe = 7,

        /// <summary>
        /// Disable datasafe operations (ADR trigger) 
        /// </summary>
        DisableDatasafe = 8,
    }

    /// <summary>
    /// NvDimm backup status
    /// </summary>
    internal enum NvdimmBackupStatus
    {
        /// <summary>
        /// Uknown if backup is pending
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// Backup not pending
        /// </summary>
        NotPending = 0,

        /// <summary>
        /// Backup pending
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Backup complete
        /// </summary>
        Complete = 2
    }

}
