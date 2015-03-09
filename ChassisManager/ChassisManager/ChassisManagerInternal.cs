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
    using System.Web;
    using System.Threading;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    /// <summary>
    /// Class for Chassis Manager internal commands
    /// </summary>
    public static class ChassisManagerInternal
    {
        #region Variables

        /// <summary>
        /// Defining all class global variables
        /// </summary>
        private static int MaxFanCount = ConfigLoaded.NumFans;       // number of fans in app.cofig
        private static int MaxSledCount = ConfigLoaded.Population;   // number of blades in app.cofig
        
        private static Thread getBladePwmReqtThread;
        private static Thread setFanSpeedThread;
        private static Thread psuMonitorThread;
        // Thread polling intervals (miliseconds)
        private static uint getBladePwmReqtTimePeriodInMilliseconds = 10000;
        private static uint setTimePeriodInMilliseconds = 10000;

        /// <summary>
        /// Define variables needed for internal chassis manager operations
        /// </summary>
        private static byte MaxPWM = (byte)ConfigLoaded.MaxPWM;
        private static byte InputSensor = (byte)ConfigLoaded.InputSensor;
        private static byte PrevFanPWM = (byte)ConfigLoaded.MinPWM;

        /// <summary>
        /// Blade Requirement table contains the fan speeds that are set from querying the blade
        /// </summary>
        private static byte[] BladeRequirementTable = new byte[MaxSledCount];

        #endregion

        #region Initialize Methods

        /// <summary>
        /// Initialize blades state (powered?) and type (compute/JBOD)
        /// </summary>
        private static void BladeInitialize()
        {
            // Get power status of enable pin for each blade and update blade state
            for (byte deviceId = 1; deviceId <= MaxSledCount; deviceId++)
            {
                CheckPowerEnableState(deviceId);
            }

            // Initialize Wcs Blade - TODO: This initialize should return some status
            WcsBladeFacade.Initialize();  // This method just creates IPMI Client Class for each blade.

            Tracer.WriteInfo("BladeInitialize: IPMI Facade Initialized, Number of blades initialized: {0}", WcsBladeFacade.Initialized);

            // check all client initialization status and update state
            Tracer.WriteInfo("BladeInitialize: Checking client status for {0} blades", MaxSledCount);
            for (byte deviceId = 1; deviceId <= MaxSledCount; deviceId++)
            {
                // TODO: How to check initialized status, now that this has become a function
                if (WcsBladeFacade.clients[deviceId].Initialize()) // This method logs on to an IPMI session.
                {
                    // If initialized is true, change state to probation
                    Tracer.WriteInfo("BladeInitialize: State Transition for blade {0}: {1} -> Probation", 
                        deviceId, ChassisState.GetStateName(deviceId));

                    ChassisState.SetBladeState(deviceId, (byte)BladeState.Probation);
                }
                else
                {
                    Tracer.WriteInfo("BladeInitialize: Blade not initialized: Blade {0}", deviceId);
                }
            }

            if (WcsBladeFacade.Initialized > 0)
            {
                // Identify what kind of sleds these are
                for (byte loop = 1; loop <= MaxSledCount; loop++)
                {
                    byte deviceId = WcsBladeFacade.clients[loop].DeviceId;
                    ChassisState.BladeTypeCache[deviceId - 1] = (byte)WcsBladeFacade.clients[loop].BladeClassification;
                }
            }
        }

        /// <summary>
        /// Initialize Communication Device
        /// </summary>
        /// <returns>status byte which indicates whether initialization was successful or not</returns>
        private static byte CommunicationDeviceInitialize()
        {
            byte status = (byte)CompletionCode.UnspecifiedError;

            Tracer.WriteInfo("Initializing Communication Device");
            CompletionCode completionCode = CommunicationDevice.Init();

            #region Comm. Device Initialization Retry
            if (CompletionCodeChecker.Failed(completionCode))
            {
                Tracer.WriteWarning("Initialization failed: {0}", completionCode);
                int loop = 0;

                // Retry 3 times before failing completely
                for (loop = 0; loop < ConfigLoaded.MaxRetries; loop++)
                {
                    Tracer.WriteInfo("Initialization Retry: {0}", loop);

                    completionCode = CommunicationDevice.Init();
                    if (CompletionCodeChecker.Succeeded(completionCode))
                    {
                        break;
                    }
                }

                if (loop == ConfigLoaded.MaxRetries)
                {
                    Tracer.WriteError("Re-attempt at Communication Device Initialization failed with code: {0}", completionCode);
                    return status;
                }
            }
            #endregion

            if (CompletionCodeChecker.Succeeded(completionCode))
            {
                Tracer.WriteInfo("Communication Device Initialization successful..");
            }

            return (byte)CompletionCode.Success;
        }

        /// <summary>
        /// Initialize Chassis constants and configs
        /// </summary>
        internal static byte Initialize()
        {
            byte status = (byte)CompletionCode.UnspecifiedError;
            status = CommunicationDeviceInitialize();

            Tracer.WriteInfo("Initializing chassis state");
            ChassisState.Initialize();
                   
            BladeInitialize();

            if (status == (byte)CompletionCode.Success)
            {
                Tracer.WriteInfo("Starting Monitoring and internal management threads");
                getBladePwmReqtThread = new Thread(new ThreadStart(RunGetAllBladeRequirements));
                setFanSpeedThread = new Thread(new ThreadStart(RunSetDeviceCommands));
                psuMonitorThread = new Thread(new ThreadStart(PsuMonitor.MonitorPsuAlert));

                getBladePwmReqtThread.Start();
                setFanSpeedThread.Start();
                psuMonitorThread.Start();
            }
            
            return status;
        }

        /// <summary>
        /// Halt all threads and stop all activities
        /// </summary>
        internal static void Halt()
        {
             // Stop the internal get and set threads by setting this global variable
            ChassisState.ShutDown = true;

            // Wait for threads to complete their current logic before stopping
            if (getBladePwmReqtThread != null)
            {
                try
                {
                    getBladePwmReqtThread.Join(ConfigLoaded.ThreadJoinTimeout);
                    Tracer.WriteInfo("OnStop: getBladePwmReqtThread thread joined");
                }
                catch (Exception ex)
                {
                    Tracer.WriteError(0, "Halt.getBladePwmReqtThread", ex);
                }
            }

            if (setFanSpeedThread != null)
            {
                try
                {
                    setFanSpeedThread.Join(ConfigLoaded.ThreadJoinTimeout);
                    Tracer.WriteInfo("OnStop: setFanSpeedThread thread joined");
                }
                catch (Exception ex)
                {
                   Tracer.WriteError(0, "Halt.setFanSpeedThread", ex);
                }
            }

            if (psuMonitorThread != null)
            {
                try
                {
                    psuMonitorThread.Join(ConfigLoaded.ThreadJoinTimeout);
                    Tracer.WriteInfo("OnStop: psuMonitorThread thread joined");
                }
                catch (Exception ex)
                {
                    Tracer.WriteError(0, "Halt.psuMonitorThread", ex);
                }
            }
        }

        #endregion
  
        #region Blade PWM Thread Methods & Functions

        /// <summary>
        /// Reinitialize the blade Id and set chassis state
        /// </summary>
        private static void ReInitialize(byte bladeId)
        {
            // Serialize initialize and power behavior per blade
            lock (ChassisState.locker[bladeId - 1])
            {
                ChassisState.FailCount[bladeId - 1] = 0; // reset fail count since we are going to reinitialize the blade

                bool status = WcsBladeFacade.InitializeClient(bladeId); // TODO: no completion code, only byte status returned

                if (status != true)
                {
                    // Initialization failed - move to fail state before retrying again
                    Tracer.WriteInfo("ReInitialize: Failed with code: {0}. State Transition for blade {1}: {2} -> Fail", 
                        status, bladeId, ChassisState.GetStateName(bladeId));

                    ChassisState.SetBladeState((byte)bladeId, (byte)BladeState.Fail);

                    // check power status to see if the blade was manually switched off or removed
                    BladePowerStatePacket response = ChassisState.BladePower[bladeId - 1].GetCachedBladePowerState();

                    // If the blade was turned off, set correct status / TODO: do we need this here?
                    if (response.BladePowerState == (byte)Contracts.PowerState.OFF)
                    {
                        Tracer.WriteInfo("ReInitialize: State Transition for blade {0}: {1} -> HardPowerOff", bladeId,
                            ChassisState.GetStateName(bladeId));

                        ChassisState.SetBladeState(bladeId, (byte)BladeState.HardPowerOff);
                    }
                }
                else
                {
                    // State change: I -> P 
                    Tracer.WriteInfo("ReInitialize: State Transition for blade {0}: {1} -> Probation", bladeId,
                            ChassisState.GetStateName(bladeId));

                    ChassisState.SetBladeState(bladeId, (byte)BladeState.Probation);

                    // Initialize Blade Type (Type might have changed when Blades were reinserted)
                    if (WcsBladeFacade.clients.ContainsKey(bladeId))
                    {
                        ChassisState.BladeTypeCache[bladeId - 1] = (byte)WcsBladeFacade.clients[bladeId].BladeClassification;
                    }
                    else
                    {
                        ChassisState.BladeTypeCache[bladeId - 1] = (byte)BladeType.Unknown;
                    }
                }
            }
        }


        /// <summary>
        /// Thread function for running get blade requirement continuously
        /// </summary>
        private static void RunGetAllBladeRequirements()
        {
            while (true)
            {
                try
                {
                    while (true)
                    {
                        if (ChassisState.ShutDown)
                        {
                            return;
                        }
                        
                        // Get Blade Pwm requirements.
                        GetAllBladePwmRequirements();
                    }
                }
                catch (Exception ex)
                {
                    Tracer.WriteWarning("Chassis Manager RunGetAllBladeRequirements thread encountered an exception " + ex);
                }
            }
        }

        /// <summary>
        /// Function that gets fan speed requirements 
        /// from all blades. It also updates the blade states.
        /// </summary>
        private static void GetAllBladePwmRequirements()
        {
            // Rate is required to timestep over each individual Blade call   
            double rate = (double)getBladePwmReqtTimePeriodInMilliseconds / (double)MaxSledCount;
            double timeDiff = 0;

            for (byte blade = 1; blade <= MaxSledCount; blade++)
            {
                // Handle shutdown state
                if (ChassisState.ShutDown)
                {
                    return;
                }

                // default PWM setting
                byte PWM = (byte)ConfigLoaded.MinPWM;

                // Query blade type from IPMI layer
                ChassisState.BladeTypeCache[blade - 1] = (byte)WcsBladeFacade.clients[blade].BladeClassification;

                // wait for rate limiter which includes the previous time difference for sensor get, and then issue get fan requirement

                double sleepTime = rate - timeDiff;

                if (sleepTime > rate)
                {
                    sleepTime = rate;
                }
                if (sleepTime > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(sleepTime));
                }

                Tracer.WriteInfo("GetBladeRequirement called at {0} for BladeId {1} (state: {2})", DateTime.Now, blade,
                    ChassisState.GetStateName(blade));

                // Check for the condition where known state is hardpoweroff, but someone plugged a new blade in
                if (ChassisState.GetBladeState(blade) == (byte)BladeState.HardPowerOff)
                {
                    CheckPowerEnableState(blade);
                }

                // Log Start time 
                DateTime startTime = DateTime.Now;

                #region Check fail State -> Initialize

                // If blade was in Fail state
                if (ChassisState.GetBladeState(blade) == (byte)BladeState.Fail)
                {
                    // If failed count is greater than a maximum value, we move it to Initialization state
                    if (ChassisState.FailCount[blade - 1] > ConfigLoaded.MaxFailCount)
                    {
                        // Move to Initialization state so that this blade could be reinitialized
                        Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for blade {0}: {1} -> Initialization", blade,
                            ChassisState.GetStateName(blade));
                        ChassisState.SetBladeState(blade, (byte)BladeState.Initialization);
                    }
                    else
                    {
                        // Moving out of Fail state - First we use a light-weight get GUID to check whether the blade is there.
                        // do not allow retries on Get System Guid
                        DeviceGuid guid = WcsBladeFacade.GetSystemGuid(blade, false);
                        if (guid.CompletionCode == (byte)CompletionCode.Success)
                        {
                            Tracer.WriteInfo("GetAllBladePwmRequirements: GUID present for blade {0}, GUID: {1}", blade, guid.Guid.ToString());

                            DeviceGuid cachedGuid = WcsBladeFacade.GetCachedGuid(blade);

                            if (guid.Guid == cachedGuid.Guid)
                            {
                                // Change state to Probation and assume the system was in fail due to timeout.
                                Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for blade {0}: {1} -> Probation", blade,
                                    ChassisState.GetStateName(blade));
                                ChassisState.SetBladeState(blade, (byte)BladeState.Probation);
                            }
                            else
                            {
                                // Change state to Initialization as the device has changed.
                                Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for blade {0}: {1} -> Probation", blade,
                                    ChassisState.GetStateName(blade));
                                ChassisState.SetBladeState(blade, (byte)BladeState.Initialization);
                            }

                        }
                        else
                        {
                            Tracer.WriteInfo("GetAllBladePwmRequirements: Get System GUID returns a bad completion status: {0}", guid.CompletionCode);
                        }
                    }

                    // Increase time spent in Fail state everytime we are in this state
                    ChassisState.FailCount[blade - 1]++;
                }

                #endregion

                #region Move Initialize -> Probation

                // Handles Initialization
                if (ChassisState.GetBladeState(blade) == (byte)BladeState.Initialization)
                {
                    BladePowerStatePacket powerstate = ChassisState.BladePower[blade - 1].GetCachedBladePowerState();

                    if (powerstate.CompletionCode == 0)
                    {
                        if (powerstate.DecompressionTime == 0)
                        {
                            // Will result in Hard Power off or Probation
                            ReInitialize(blade);
                        }
                    }
                }

                #endregion


                // Normal operation - possible states are probation or healthy
                if (ChassisState.GetBladeState(blade) == (byte)BladeState.Probation ||
                    ChassisState.GetBladeState(blade) == (byte)BladeState.Healthy)
                {
                    #region Jbod (no sensor reading)

                    if (ChassisState.GetBladeType(blade) == (byte)BladeType.Jbod)
                    {
                        // Do not allow retries on system guid.
                        DeviceGuid guid = WcsBladeFacade.GetSystemGuid(blade, false);
                        if (guid.CompletionCode == (byte)CompletionCode.Success)
                        {
                            Tracer.WriteInfo("GetAllBladePwmRequirements: GUID present for JBOD {0}, GUID: {1}", 
                                blade, guid.Guid.ToString());

                            // Change state to Healthy
                            if (ChassisState.GetBladeState(blade) == (byte)BladeState.Probation)
                            {
                                Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for JBOD {0}: {1} -> Healthy", 
                                    blade, ChassisState.GetStateName(blade));
                                ChassisState.SetBladeState(blade, (byte)BladeState.Healthy);
                            }

                        }
                        else
                        {
                            Tracer.WriteInfo("GetAllBladePwmRequirements: Get System GUID for JBOD {0} failed with status {1}", 
                                blade, guid.CompletionCode);
                            // Set it to failed state, where we will retry guids and reinitialize if needed
                            Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for JBOD {0}: {1} -> Fail", 
                                blade, ChassisState.GetStateName(blade));
                            ChassisState.SetBladeState(blade, (byte)BladeState.Fail);
                        }

                        // No need to check for sensor reading, just continue
                        continue;
                    }
                    
                    #endregion

                    #region Server -> Get PWM move to Healthy or move to Fail

                    // Call temperature reading list command
                    SensorReading Temps = WcsBladeFacade.GetSensorReading((byte)blade, (byte)ConfigLoaded.InputSensor, PriorityLevel.System);

                    if (Temps.CompletionCode != (byte)CompletionCode.Success)
                    {
                        Tracer.WriteWarning("GetAllBladePwmRequirements: BladeId: {0} - GetSensorReading for temperature failed with code {1:X}", 
                            blade, Temps.CompletionCode);

                        // Move to Fail state if no readings were obtained
                        Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for blade {0}: {1} -> Fail", blade,
                            ChassisState.GetStateName(blade));

                        ChassisState.SetBladeState(blade, (byte)BladeState.Fail);
                    }
                    else
                    {
                        Tracer.WriteInfo("GetAllBladePwmRequirements: #### BladeId = " + blade + " Sensor id= " + ConfigLoaded.InputSensor + 
                            " Sensor reading = " + Temps.Reading + " Raw = " + Temps.RawReading + 
                            ", LowerNonCritical= " + ConfigLoaded.SensorLowThreshold + ", UpperNonCritical= " + ConfigLoaded.SensorHighThreshold);

                        // Handle state logic if needed
                        // Probation state should be shifted to Healthy since there was no timeout, & sensorread succeeded
                        if (ChassisState.GetBladeState(blade) == (byte)BladeState.Probation)
                        {
                            // Change state to healthy
                            Tracer.WriteInfo("GetAllBladePwmRequirements: State Transition for blade {0}: {1} -> Healthy", 
                                blade, ChassisState.GetStateName(blade));

                            ChassisState.SetBladeState(blade, (byte)BladeState.Healthy);
                            ChassisState.FailCount[blade - 1] = 0; // reset the fail count

                            // When a blade transitions to 'Healthy' state, enable/disable default blade operations
                            EnableDisableDefaultBladeOperations(blade);
                        }

                        if (ConfigLoaded.InputSensor != 1) // Non-PWM sensor.
                        {
                            PWM = GetPwmFromTemperature(Temps.Reading,
                                     ConfigLoaded.SensorLowThreshold,
                                     ConfigLoaded.SensorHighThreshold);
                        }
                        else
                        {
                            // PWM should never be higher or lower than the threshold.
                            if (Temps.Reading < ConfigLoaded.MinPWM || Temps.Reading > ConfigLoaded.MaxPWM)
                            {
                                Tracer.WriteWarning("PWM value " + Temps.Reading + " on blade " + blade +
                                " is out of range (lowThreshold: " + ConfigLoaded.MinPWM + 
                                " - highThreshold: " + ConfigLoaded.MaxPWM);

                                PWM = (byte)ConfigLoaded.MinPWM;
                            }
                            else
                            {
                                PWM = (byte)Temps.Reading;
                            }

                        }

                        Tracer.WriteInfo("PWM value on blade {0} for Sensor {1} = {2}", blade, InputSensor, PWM);
                    }

                    #endregion
                }

                // write value into requirements table
                BladeRequirementTable[blade - 1] = PWM;

                // Log end time and capture time of execution for sensor get command
                DateTime endTime = DateTime.Now;
                timeDiff = endTime.Subtract(startTime).TotalMilliseconds; // convert time difference into milliseconds
            }
        }

        /// <summary>
        /// Checks the power enable state of the blade and changes state accordingly
        /// </summary>
        /// <param name="deviceId"></param>
        private static void CheckPowerEnableState(byte deviceId)
        {
            // Serialize power behavior
            lock (ChassisState.locker[deviceId - 1])
            {
                BladePowerStatePacket response = ChassisState.BladePower[deviceId - 1].GetCachedBladePowerState();

                if (response.CompletionCode != CompletionCode.Success)
                {
                    Tracer.WriteInfo("CheckPowerEnableState: Blade {0} Power Enable state read failed (Completion Code: {1:X})",
                        deviceId, response.CompletionCode);
                }
                else
                {
                    if (response.BladePowerState == (byte)Contracts.PowerState.ON)
                    {
                        if (ChassisState.GetBladeState((byte)deviceId) == (byte)BladeState.HardPowerOff)
                        {
                            // Blade is powered on, move to initialization state
                            Tracer.WriteInfo("CheckPowerEnableState: State Transition for blade {0}: {1} -> Initialization", 
                                deviceId, ChassisState.GetStateName(deviceId));
                            ChassisState.SetBladeState((byte)deviceId, (byte)BladeState.Initialization);
                        }
                    }
                    else if (response.BladePowerState == (byte)Contracts.PowerState.OFF)
                    {
                        if (ChassisState.GetBladeState((byte)deviceId) != (byte)BladeState.HardPowerOff)
                        {
                            // Blade is powered off, move to PowerOff state
                            Tracer.WriteInfo("CheckPowerEnableState: State Transition for blade {0}: {1} -> HardPowerOff", 
                                deviceId, ChassisState.GetStateName(deviceId));

                            ChassisState.SetBladeState((byte)deviceId, (byte)BladeState.HardPowerOff);
                        }
                    }
                    else
                    {
                        Tracer.WriteInfo("CheckPowerEnableState: Getting out of else block");
                        // TODO: do we need to do anything for state that is NA
                    }
                }
            }
        }

        /// <summary>
        /// Converts inlet temperature to PWM value
        /// </summary>
        /// <param name="temperature"></param>
        /// <returns></returns>
        private static byte GetPwmFromTemperature(double temperature, double lowThreshold, double highThreshold)
        {
            byte PWM = (byte)ConfigLoaded.MinPWM; // set to min as default

            if (lowThreshold >= highThreshold)
            {
                Tracer.WriteWarning("Low Threshold Temperature is greater or equal compared to high threshold");
                return PWM;
            }
            // PWM should never be higher or lower than the threshold.
            if (temperature < lowThreshold || temperature > highThreshold)
            {
                Tracer.WriteWarning("Temperature value {0} is out of range (lowThreshold {1} - highThreshold {2})",
                    temperature, lowThreshold, highThreshold);
                return PWM;
            }

            // Find PWM corresponding to temperature value from low threshold and range value
            // Linear extrapolation requires current value, range for consideration and the low-threshold so that 
            // we can compute the PWM (as a value between 20-100)
            if (temperature <= highThreshold)
            {
                // These thresholds are read from threshold values in SDR record
                double range = highThreshold - lowThreshold;
                double value = ConfigLoaded.MinPWM + ((temperature - lowThreshold) / range) * (MaxPWM - ConfigLoaded.MinPWM);
                PWM = (byte)value;

                // Reset to MinPWM if calculated PWM is lower than MinPWM
                if (PWM < ConfigLoaded.MinPWM)
                {
                    PWM = (byte)ConfigLoaded.MinPWM;
                }

                // Set PWM to MaxPWM if calculated PWM is more than MaxPWM
                if (PWM > MaxPWM)
                {
                    PWM = MaxPWM;
                }
            }

            return PWM;
        }

        #endregion

        #region Chassis FanSpeed, Watchdog, LED, DatasafeOperation & Psu Control Thread Method

        /// <summary>
        /// Gets maximum value from the Blade requirement table
        /// </summary>
        /// <returns></returns>
        private static byte GetMaxRequirement()
        {
            HelperFunction.MaxPwmRequirement = BladeRequirementTable.Max();
            return HelperFunction.MaxPwmRequirement;
        }

        /// <summary>
        /// Thread functions to run the set commands
        /// </summary>
        private static void RunSetDeviceCommands()
        {
           while (true)
            {
                try
                {
                    if (ChassisState.ShutDown)
                    {
                        return;
                    }

                    if (ConfigLoaded.EnableFan)
                    {
                        // Step 1. Set All Fan Speeds if EnableFan is set.
                        SetAllFanSpeeds();
                    }

                    // Step 2. Check Serial Console Inactivity.
                    CheckBladeConsoleInactivity();
                        
                    // Step 3. Reset Watch Dog Timer
                    ResetWatchDog();

                    // Step 4. Set Attention Leds
                    SetAttentionLeds();
                }
                catch (Exception ex)
                {
                    Tracer.WriteWarning("Chassis Manager RunSetDeviceCommands thread encountered an exception " + ex);
                }
            }
        }

        #region Fan Speed Control

        /// <summary>
        /// Gets the current status of all fans and returns number of fans that is working as expected
        /// </summary>
        private static int GetAllFanStatus()
        {
            int countStatus = 0;
            for (int numFans = 0; numFans < ChassisState.Fans.Length; numFans++)
            {
                if (ChassisState.Fans[numFans].GetFanStatus())
                {
                    countStatus++;
                }
            }
            return countStatus;
        }

        /// <summary>
        /// Sets the chassis fan speed 
        /// </summary>
        private static void SetAllFanSpeeds()
        {
            // rate limiter for setting thread
            Thread.Sleep(TimeSpan.FromMilliseconds(setTimePeriodInMilliseconds));

            // Get max requirement from the bladerequirement table
            byte maxFanRequest = GetMaxRequirement();

            Tracer.WriteInfo("Max value got from Blade table = {0} (at {1})", maxFanRequest, DateTime.Now);

            // Check Fan Status and get number of working fans
            int numFansWorking = GetAllFanStatus();

            // Handle one fan failure
            if (numFansWorking == MaxFanCount - 1)
            {
                // Alert that one fan has failed!
                Tracer.WriteError("Fan failure. Scaling fan speed up proportionally to handle one fan failure.");

                // Denote fan failure in chassis
                ChassisState.FanFailure = true;

                double conversion = (double)MaxFanCount / (double)(MaxFanCount - 1);
                maxFanRequest = (byte)(conversion * maxFanRequest);
            }
            else if (numFansWorking < MaxFanCount - 1)
            {
                // Set fan speed to max for fan failures more than N-1
                maxFanRequest = MaxPWM; // this is to set at max speed
                Tracer.WriteError("More than 1 Fans failed");

                // Denote that this is a fan failure in chassis
                ChassisState.FanFailure = true;
            }
            else
            {
                // All fans are working fine - check rear attention LED and if on, turn it off (by setting fanFailure to false)
                ChassisState.FanFailure = false;
            }

            // Do altitude correction
            maxFanRequest = (byte)((1 + ConfigLoaded.AltitudeCorrectionFactor * (int)(ConfigLoaded.Altitude / 1000)) * maxFanRequest);

            // Bound fan request to the maximum possible
            if (maxFanRequest > MaxPWM)
            {
                maxFanRequest = MaxPWM;
            }

            // Enable Ramp Down in smaller steps
            if (PrevFanPWM >= maxFanRequest + 2 * ConfigLoaded.StepPWM)
            {
                maxFanRequest = (byte)(PrevFanPWM - ConfigLoaded.StepPWM);
            }

            // Set fan speed for all fans - setting one fan device is enough, since setfanspeed is for all fan devices
            byte status = ChassisState.Fans[0].SetFanSpeed(maxFanRequest);

            // Trace the speed of fan
            Tracer.WriteInfo("Fan speed = " + ChassisState.Fans[0].GetFanSpeed());

            if (status != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("SetFanSpeed failed with Completion Code: {0:X}", status);
            }
            else
            {
                Tracer.WriteInfo("Fan Speed set to {0}", maxFanRequest);
            }

            // Store current fan PWM in PrevFanPWM for next iteration
            PrevFanPWM = maxFanRequest;
        }
        
        #endregion

        #region Enabling/disabling default blade operations

        private static void EnableDisableDefaultBladeOperations(int bladeId)
        {
            // TODO: Check blade type etc and Kill any serial session
            // TODO: Add trace log messages
            
            // Check to see if the blade is hard powered off
            BladePowerStatePacket response = ChassisState.BladePower[bladeId - 1].GetBladePowerState();
            if (response.CompletionCode != CompletionCode.Success)
            {
                // Log error here, and proceed to check blade state since we still want to check BMC soft power status
                // even if blade enable read failed for whatever reason
                Tracer.WriteError("EnableDisableDefaultBladeOperations: Blade {0} Power Enable state read failed (Completion Code: {1:X})",
                    bladeId, response.CompletionCode);
            }
            else if (response.BladePowerState == (byte)Contracts.PowerState.OFF)
            {
                // If blade is hard powered off, no further processing is necessary
                return;
            }

            // If the blade is a Jbod, return since the operations done in this method do not apply for Jbods
            if (ChassisState.GetBladeType((byte)bladeId) == (byte)BladeType.Jbod)
            {
                Tracer.WriteInfo("EnableDisableDefaultBladeOperations (Blade#{0}): Ignoring since it is a Jbod", bladeId);
                return;
            }

            DatasafeOperationSupport.ProcessDatasafeAction(bladeId, ConfigLoaded.DatasafeOperationsEnabled ? DatasafeActions.EnableDatasafe : 
                DatasafeActions.DisableDatasafe);

            if(ConfigLoaded.PsuAlertMonitorEnabled)
                WcsBladeFacade.ActivatePsuAlert((byte)bladeId, true, BmcPsuAlertAction.ProcHotAndDpc, true);
            else
                WcsBladeFacade.ActivatePsuAlert((byte)bladeId, false, BmcPsuAlertAction.NoAction, true);
            
        }

        #endregion 

        #region Process Critical Battery Capacity 

        /// <summary>
        /// This method is invoked if battery status reaches a certain critical charge level.
        /// It triggers datasafe (NVDIMM) backup for all blades.
        /// </summary>
        /// <param name="stateInfo">The state information.</param>
        internal static void ProcessCriticalBatteryStatus(Object stateInfo)
        {
            // Enable extended battery mode operation in PSUs (we are not sure if this is already done)
            for (int psuId = 1; psuId <= ConfigLoaded.NumPsus; psuId++)
            {
                ChassisState.Psu[psuId-1].SetBatteryExtendedOperationMode(true);
            }

            // Trigger NvDimm backup for all blades
            for (int bladeId = 1; bladeId <= ConfigLoaded.Population; bladeId++)
            {
                DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.BatteryLowCapacityPanic);
            }
        }

        #endregion


        #region WatchDog Reset

        /// <summary>
        /// Resets Chassis Manager Fan -> High Watch dog timer
        /// </summary>
        private static void ResetWatchDog()
        {
            // Reset WatchDogTimer every TimePeriod after setting fan speeds
            ChassisState.Wdt.ResetWatchDogTimer();

            Tracer.WriteInfo("WatchDogTimer was reset");
        }

        #endregion

        #region Serial Console Inactivity Control

        /// <summary>
        /// Checks Blade Serial Sessions and Serial Port Console Sessions for inactivity.
        /// Closes inactive sessions to prevent conditions where users cannot open new sessions
        /// due to old sessions being abandoned.
        /// Receive and transmit commands are considered to be activity and will keep the session alive.
        /// </summary>
        private static void CheckBladeConsoleInactivity()
        {
            foreach (KeyValuePair<int, BladeSerialSessionMetadata> bladeSerialObject in ChassisState.BladeSerialMetadata)
            {
                bladeSerialObject.Value.BladeSerialSessionInactivityCheck();
            }

            for (int numPorts = 0; numPorts < ConfigLoaded.MaxSerialConsolePorts; numPorts++)
            {
                ChassisState.SerialConsolePortsMetadata[numPorts].SerialPortConsoleInactivityCheck(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(numPorts));
            }
        }

        #endregion

        #region Chassis LED Control

        /// <summary>
        /// Set the Chassis Manager attention LED based on Fan and 
        /// PSU status in the Chassis Manager State Class
        /// </summary>
        private static void SetAttentionLeds()
        {
            // Set Rear Attention LED
            if ((ConfigLoaded.EnableFan && ChassisState.FanFailure) || ChassisState.PsuFailure)
            {
                // Set rear attention LED On
                if (GetRearAttentionLedStatus() != (byte)LedStatus.On)
                {
                    SetRearAttentionLedStatus((byte)LedStatus.On);
                }
            }
            else
            {
                // Set rear attention LED Off
                if (GetRearAttentionLedStatus() != (byte)LedStatus.Off)
                {
                    SetRearAttentionLedStatus((byte)LedStatus.Off);
                }
            }
        }

        /// <summary>
        /// Sets the rear attention LED to on or off based on ledState param (0 - off, 1 - on)
        /// </summary>
        /// <param name="ledState"></param>
        private static byte SetRearAttentionLedStatus(byte ledState)
        {
            byte completionCode = (byte)CompletionCode.UnspecifiedError;

            if (ledState == (byte)LedStatus.On)
            {
                completionCode = ChassisState.AttentionLed.TurnLedOn();
                Tracer.WriteInfo("Internal setRearAttentionLEDStatus - LEDOn Return: {0:X}", completionCode);
            }
            if (ledState == (byte)LedStatus.Off)
            {
                completionCode = ChassisState.AttentionLed.TurnLedOff();
                Tracer.WriteInfo("Internal setRearAttentionLEDStatus - LEDOff Return: {0:X}", completionCode);
            }
            if (completionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("Internal setRearAttentionLEDStatus error - completion code: {0:X}", completionCode);
            }
            return completionCode;
        }

        /// <summary>
        /// Gets the current status of rear attention LED for the chassis
        /// </summary>
        /// <returns></returns>
        private static byte GetRearAttentionLedStatus()
        {
            // Gets the LED status response
            Contracts.LedStatusResponse ledStatus = new Contracts.LedStatusResponse();
            ledStatus = ChassisState.AttentionLed.GetLedStatus();

            if (ledStatus.completionCode != Contracts.CompletionCode.Success)
            {
                Tracer.WriteWarning("Internal getRearAttentionLedStatus - getting status failed with Completion Code {0:X}",
                    ledStatus.completionCode);
                return (byte)LedStatus.NA;
            }
            else
            {
                return (byte)ledStatus.ledState;
            }
        }

        #endregion

        #endregion
    }
}



        
