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

namespace ChassisValidation
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Threading;
    using Microsoft.GFS.WCS.Contracts;

    abstract class CommandBase
    {
        public IChassisManager Channel;

        public IChassisManager TestChannelContext;

        public Dictionary<int, IChassisManager> ListTestChannelContexts = new Dictionary<int, IChassisManager>();
        public Dictionary<string, string> TestEnvironmentSetting = new Dictionary<string, string>();

        protected GetAllBladesInfoResponse allBladesInfo = null;// cache the blades info 

        protected readonly string defaultCMName;
        protected readonly string defaultAdminUserName;
        protected readonly string defaultAdminPassword;

        internal CommandBase(IChassisManager channel)
        {
            this.Channel = channel;
            this.TestChannelContext = channel;
        }

        internal CommandBase(IChassisManager channel, Dictionary<int, IChassisManager> listTestChannelContexts)
        {
            this.Channel = channel;
            this.TestChannelContext = channel;
            this.ListTestChannelContexts = listTestChannelContexts;
        }

        internal CommandBase(IChassisManager channel, Dictionary<int, IChassisManager> listTestChannelContexts, Dictionary<string, string> listCMEnvironmentSetting)
        {
            this.Channel = channel;
            this.TestChannelContext = channel;
            this.ListTestChannelContexts = listTestChannelContexts;
            this.TestEnvironmentSetting = listCMEnvironmentSetting;

            this.defaultCMName = new Uri(this.TestEnvironmentSetting["CMURL"]).Host;
            this.defaultAdminUserName = "admin";
            this.defaultAdminPassword = this.TestEnvironmentSetting["DefaultPassword"];
        }

        protected enum DwLogonType
        {
            Interactive = 2,
            Network = 3,
            Batch = 4,
            Service = 5,
            Unlock = 7,
            NetworkClearText = 8,
            NewCredentials = 9
        }

        protected enum DwLogonProvider
        {
            Default = 0,
            WinNT35 = 1,
            WinNT40 = 2,
            WinNT50 = 3
        }

        protected int[] EmptyLocations { get; set; }

        protected int[] JbodLocations { get; set; }

        protected int[] ServerLocations { get; set; }

        protected int[] FailedServers { get; set; }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr token);

        /// <summary>
        /// This method returns list of empty slot for CM
        /// </summary>
        /// <returns>Returns list of empty slots if CM have any empty slots; return null otherwise.</returns>
        protected int[] GetEmptyLocations()
        {
            int[] emptyLocations;
            if (this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.EmptyBladeType), out emptyLocations))
            {
                return emptyLocations;
            }
            CmTestLog.End(false, "Did NOT find empty locations");
            return null;
        }

        /// <summary>
        /// This method returns list of server locations for CM
        /// </summary>
        /// <returns>Returns list of server locations; return null otherwise.</returns>
        protected int[] GetServerLocations()
        {
            int[] serverLocations;
            if (this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations))
            {
                return serverLocations;
            }
            CmTestLog.End(false, "Did NOT find server locations");
            return null;
        }

        /// <summary>
        /// This method returns list of JBOD slot for CM
        /// </summary>
        /// <returns>Returns list of JBOD if CM have any; return null otherwise.</returns>
        protected int[] GetJbodLocations()
        {
            int[] jbodLocations;
            if (this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                return jbodLocations;
            }
            CmTestLog.End(false, "Did NOT find server locations");
            return null;
        }

        /// <summary>
        /// This method returns list of populated Psu slots for CM
        /// </summary>
        /// <param name="testName"></param>
        /// <returns>Returns list of Psus if CM has any; returns null otherwise.</returns>
        protected int[] GetPsuLocations([CallerMemberName] string testName = null)
        {
            ChassisInfoResponse chassisInfoResponse = this.Channel.GetChassisInfo(false, true, false, true);
            if (chassisInfoResponse.completionCode != CompletionCode.Success)
            {
                CmTestLog.Failure("Failed to get Psu info using GetChassisInfo", testName);
                return null;
            }
            else
                CmTestLog.Success("Successfully received Psu info using GetChassisInfo", testName);

            string testingLes = ConfigurationManager.AppSettings["LesOrNonLesPsus"].ToString();

            int[] psuLocations = chassisInfoResponse
                                            .psuCollections
                                            .Where(psu => psu.state.Equals(PowerState.ON))
                                            .Select(psu => (int)psu.id)
                                            .ToArray();

            if (testingLes == "1")
            {
                psuLocations = chassisInfoResponse
                                            .batteryCollections
                                            .Where(battery => battery.presence.Equals((byte)1) && psuLocations.Contains((int)battery.id))
                                            .Select(battery => (int)battery.id)
                                            .ToArray();
            }

            return psuLocations;
        }

        /// <summary>
        /// Verifies that all BladeStateResponses in the given collection are the same as the expectedState.
        /// If a blade is a server, this method verifies the server has the same state as the expectedState; if a blade
        /// is a jbod, this method ignores the expectedState parameter and verifies the blade returns CommandNotValidForBlade.
        /// If there is only one response in the collection and it is from an empty slot, the method returns false; 
        /// in other cases, empty slots in the collection will just be ignored.
        /// </summary>
        protected bool VerifyBladeState(PowerState expectedState, IEnumerable<BladeStateResponse> bladeStates,
            [CallerMemberName]
            string testName = null)
        {
            GetAllBladesInfoResponse allBlades;
            if (!this.GetAllBladesInfo(out allBlades, testName))
            {
                return false;
            }

            try
            {
                var bladeStateCollection = new List<BladeStateResponse>(bladeStates);
                bool serverResult = true, jbodResult = true;
                int bladeCount = 0, JbodCount = 0;

                foreach (var state in bladeStateCollection)
                {
                    // current blade info
                    var bladeInfo = allBlades.bladeInfoResponseCollection.Single(info => info.bladeNumber == state.bladeNumber);

                    if (bladeInfo.bladeType.ToLower().Equals("server"))
                    {
                        bladeCount++;
                    }

                    if (bladeInfo.bladeType.ToLower().Equals("jbod"))
                    {
                        JbodCount++;
                    }

                    // verify server blade
                    if (bladeInfo.bladeType.Equals(CmConstants.ServerBladeType) && state.bladeState != expectedState)
                    {
                        serverResult = false;
                        CmTestLog.Failure(string.Format("Server Blade# {0} state is not as expected (Expected: {1}, Actual: {2})",
                            state.bladeNumber, expectedState, state.bladeState), testName);
                    }
                    // verify jbod blade
                    else if (bladeInfo.bladeType.Equals(CmConstants.JbodBladeType) && state.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        jbodResult = false;
                        CmTestLog.Failure(string.Format("JBOD Blade# {0} completion code is not correct (Expected: {1}, Actual: {2})",
                            state.bladeNumber, CompletionCode.CommandNotValidForBlade, state.completionCode), testName);
                    }
                }

                if (bladeCount > 0 && serverResult)
                {
                    CmTestLog.Success(string.Format("Verified server blades are {0}", expectedState), testName);
                }
                else
                {
                    CmTestLog.Warning("There were no blades to run test against", testName);
                }

                if (JbodCount > 0 && jbodResult)
                {
                    CmTestLog.Success("Verified JBODs return CommandNotValidForBlade", testName);
                }
                else
                {
                    CmTestLog.Warning("There were no JBODs to run test against", testName);
                }

                return serverResult && jbodResult;
            }
            catch (Exception e)
            {
                CmTestLog.Exception(e, testName);
                return false;
            }
        }

        /// <summary>
        /// Sets power state. If powerState is ON, power on the blade; otherwise, power off the blade.
        /// If bladeId is specified, change power state to that specific blade; otherwise, the change
        /// will be made to all blades.
        /// </summary>
        protected bool SetPowerState(PowerState state, int bladeId = -1, [CallerMemberName]
                                     string testName = null)
        {
            try
            {
                var message = string.Format("Set {1} to power state {0}", state,
                    bladeId > 0 ? string.Format("Blade {0}", bladeId) : "all blades");
                CmTestLog.Info(string.Concat("Trying to ", message), testName);
                ChassisResponse response;
                if (bladeId > 0)
                {
                    response = state == PowerState.ON
                               ? this.Channel.SetPowerOn(bladeId)
                               : this.Channel.SetPowerOff(bladeId);
                }
                else
                {
                    response = state == PowerState.ON
                               ? this.Channel.SetAllPowerOn()
                               : this.Channel.SetAllPowerOff();
                }

                var result = ChassisManagerTestHelper.AreEqual(CompletionCode.Success,
                    response.completionCode, message, testName);

                if (state == PowerState.OFF)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));
                }

                return result;
            }
            catch (Exception e)
            {
                CmTestLog.Exception(e, testName);
                return false;
            }
        }

        /// <summary>
        /// Sets blade state. If state is set to ON, set the blade to on; otherwise, set the blade to off.
        /// If bladeId is specified, change blade state to that specific blade; otherwise, the change
        /// will be made to all blades.
        /// </summary>
        protected bool SetBladeState(PowerState state, int bladeId = -1, [CallerMemberName]
                                     string testName = null)
        {
            try
            {
                var message = string.Format("Set {0} to {1} state", state,
                    bladeId > 0 ? string.Format("Blade {0}", bladeId) : "all blades");
                CmTestLog.Info(string.Concat("Trying to ", message), testName);
                ChassisResponse response;
                if (bladeId > 0)
                {
                    response = state == PowerState.ON
                               ? this.Channel.SetBladeOn(bladeId)
                               : this.Channel.SetBladeOff(bladeId);
                }
                else
                {
                    response = state == PowerState.ON
                               ? this.Channel.SetAllBladesOn()
                               : this.Channel.SetAllBladesOff();
                }

                var result = ChassisManagerTestHelper.AreEqual(CompletionCode.Success,
                    response.completionCode, message, testName);
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

                return result;
            }
            catch (Exception e)
            {
                CmTestLog.Exception(e, testName);
                return false;
            }
        }

        /// <summary>
        /// Gets the locations for blades which satisfy a given condition.
        /// </summary>
        protected bool GetBladeLocations(Func<BladeInfoResponse, bool> predicate, out int[] locations,
            [CallerMemberName]
            string testName = null)
        {
            GetAllBladesInfoResponse allBladesInfoResponse;
            if (!this.GetAllBladesInfo(out allBladesInfoResponse, testName))
            {
                locations = null;
                return false;
            }
            locations = allBladesInfoResponse
                                             .bladeInfoResponseCollection
                                             .Where(predicate)
                                             .Select(blade => blade.bladeNumber)
                                             .ToArray();
            return true;
        }


        protected bool GetAllBladesInfo(out GetAllBladesInfoResponse allBladesInfoResponse,
            [CallerMemberName]
            string testName = null)
        {
            // if the blade info has already been cached
            // just return it.
            if (this.allBladesInfo != null)
            {
                allBladesInfoResponse = this.allBladesInfo;
                return true;
            }

            try
            {
                CmTestLog.Info("Trying to get the information for all blades", testName);

                if (!this.SetPowerState(PowerState.ON) || !this.SetBladeState(PowerState.ON))
                {
                    allBladesInfoResponse = null;
                    return false;
                }
                allBladesInfoResponse = this.Channel.GetAllBladesInfo();
                if (CompletionCode.Success != allBladesInfoResponse.completionCode)
                {
                    CmTestLog.Failure("Failed to get all blades info", testName);
                    return false;
                }
                CmTestLog.Success("Get all blades info successfully", testName);
                this.allBladesInfo = allBladesInfoResponse; // save it
                return true;
            }
            catch (Exception e)
            {
                CmTestLog.Exception(e, testName);
                allBladesInfoResponse = null;
                return false;
            }
        }
        
        /// <summary>
        /// Configures App.Config of Chassis Manager service by taking as input key value pairs from dictionary. 
        /// keys in dictionary are the keys in App.Config and values are the value in App.Config that 
        /// are to be changed for each key. ConfigureAppConfig backs up the original App.Config and should be
        /// restored using cleanUp parameter after testing with ConfigureAppConfig is complete.
        /// </summary>
        /// <example>
        /// Example 1: Change Value of Key "NumFans" in CM service App.Config to "5"
        /// ConfigureAppConfig(new Dictionary<string, string>() {{"NumFans", "5"}}, false);
        /// Example 2: Restore original CM service App. Config
        /// ConfigureAppConfig(null, true);
        /// </example>
        /// <remarks>
        /// ConfigureAppConfig does not restart the CM service after configuring App.Config. Use method RestartCmService.
        /// </remarks>
        /// <param name="appConfigKeyValuePairs"></param>
        /// <param name="cleanUp"></param>
        /// <param name="testName"></param>
        /// <returns></returns>
        protected bool ConfigureAppConfig(Dictionary<string, string> appConfigKeyValuePairs, bool cleanUp,
            [CallerMemberName]
            string testName = null)
        {
            bool configurationSuccess = true;

            try
            {
                string configFilePath = string.Format("{0}{1}{2}", @"\\", this.defaultCMName, @"\c$\ChassisManager\Microsoft.GFS.WCS.ChassisManager.exe.config");
                string backupConfigFilePath = string.Format("{0}{1}{2}", @"\\", this.defaultCMName, @"\c$\ChassisManager\Microsoft.GFS.WCS.ChassisManager.exe.config.backup");
                string modifiedConfigFilePath = string.Format("{0}modifiedConfig.config", Path.Combine(Directory.GetCurrentDirectory(), "TestData"));

                IntPtr token = IntPtr.Zero;

                // Impersonate remote user in order to copy/modify files
                bool successLogon = LogonUser(this.defaultAdminUserName, this.defaultCMName, this.defaultAdminPassword,
                    (int)DwLogonType.NewCredentials, (int)DwLogonProvider.WinNT50, ref token);

                if (successLogon)
                {
                    CmTestLog.Info("LogonUser: User successfully created");

                    // Impersonate user
                    using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(token))
                    {
                        if (!cleanUp)
                        {
                            // Verify input KeyValue pairs is not empty
                            if (appConfigKeyValuePairs.Count < 1)
                            {
                                CmTestLog.Failure("Requested App Config Key Value Pairs is empty");
                                return false;
                            }

                            // Delete modified file path if it already exists
                            if (File.Exists(modifiedConfigFilePath))
                            {
                                File.Delete(modifiedConfigFilePath);
                                CmTestLog.Info("Temporary App.Config already exists. Deleting...");
                            }

                            // Delete backup file path if it already exists
                            if (File.Exists(backupConfigFilePath))
                            {
                                File.Delete(backupConfigFilePath);
                                CmTestLog.Info("Backup App.Config already exists. Deleting...");
                            }

                            // Copy original App.Config to temporary path and backup original
                            File.Copy(configFilePath, modifiedConfigFilePath);
                            File.Move(configFilePath, backupConfigFilePath);

                            // Initialize App.Config in config object to prep for modification
                            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                            fileMap.ExeConfigFilename = modifiedConfigFilePath;
                            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                            // Modify KeyValue pairs in App.Config to requested input KeyValue pairs
                            foreach (KeyValuePair<string, string> xmlKeyValue in appConfigKeyValuePairs)
                            {
                                if (config.AppSettings.Settings.AllKeys.Contains(xmlKeyValue.Key))
                                {
                                    config.AppSettings.Settings[xmlKeyValue.Key].Value = xmlKeyValue.Value;
                                    CmTestLog.Success(string.Format(string.Format("Changed key:{0} to new value:{1} in App.Config",
                                        xmlKeyValue.Key, xmlKeyValue.Value)));
                                    configurationSuccess &= true;
                                }
                                else
                                {
                                    CmTestLog.Failure(string.Format("Could not find key:{0} in App.Config", xmlKeyValue.Key));
                                    configurationSuccess = false;
                                }
                            }

                            // Save modified App.Config
                            config.Save();
                            System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                            // Copy over modified App.Config to original file path
                            if (!File.Exists(configFilePath))
                            {
                                File.Copy(modifiedConfigFilePath, configFilePath);
                            }
                            else
                            {
                                CmTestLog.Failure("App.Config cannot be renamed, and so, cannot be replaced by modified App.Config");
                                return false;
                            }

                            File.Delete(modifiedConfigFilePath);
                        }
                        else
                        {
                            // Revert back to original App.Config using backup file during CleanUp
                            CmTestLog.Info("configureAppConfig CleanUp: Replacing modified App.Config with original App.Config");

                            if (!File.Exists(backupConfigFilePath))
                            {
                                CmTestLog.Failure("Backup App.Config does not exist");
                                return false;
                            }
                            else
                            {
                                if (File.Exists(configFilePath))
                                {
                                    CmTestLog.Info("Modified App.Config file exists - deleting");
                                    File.Delete(configFilePath);
                                }

                                File.Move(backupConfigFilePath, configFilePath);

                            }

                            CmTestLog.Success("configureAppConfig: Clean up successful");
                        }

                        // Revert back to original user
                        context.Undo();
                    }
                }
                else
                {
                    CmTestLog.Failure("UserLogon: User failed to be created");
                    return false;
                }
            }
            catch (Exception e)
            {
                CmTestLog.Exception(e, testName);
                return false;
            }

            return configurationSuccess;
        }

        /// <summary>
        /// Starts or stops the CM service
        /// </summary>
        /// <example>
        /// Example 1: start the CM service
        /// StartStopCmService("start");
        /// Example 2: stop the CM service
        /// StartStopCmService("stop");
        /// </example>
        /// <param name="startStopService"></param>
        /// <param name="testName"></param>
        /// <param name="retryStartStop"></param>
        /// <returns></returns>
        protected bool StartStopCmService(string startStopService, [CallerMemberName] string testName = null, 
            bool retryStartStop = true)
        {
            bool startStopSuccess = true;

            try
            {
                string cmServiceName = "chassismanager";
                startStopService = startStopService.ToLower();

                if (startStopService == "start")
                {
                    CmTestLog.Info(string.Format("Trying to start Chassis Manager service on {0}", this.defaultCMName));
                }
                else if (startStopService == "stop")
                {
                    CmTestLog.Info(string.Format("Trying to stop Chassis Manager service on {0}", this.defaultCMName));
                }
                else
                {
                    CmTestLog.Failure("startStopService action not defined to 'start' or 'stop' service");
                    return false;
                }

                // Initialize object to specify all settings for WMI connection
                ConnectionOptions serviceConnectOptions = new ConnectionOptions();
                serviceConnectOptions.Username = string.Format("{0}\\{1}", this.defaultCMName, this.defaultAdminUserName);
                serviceConnectOptions.Password = this.defaultAdminPassword;

                // Initialize object to represent scope of management operations 
                ManagementScope serviceScope = new ManagementScope(string.Format("{0}{1}{2}", @"\\", this.defaultCMName, @"\root\cimv2"));
                serviceScope.Options = serviceConnectOptions;

                // Define WMI query to execute on CM
                SelectQuery query = new SelectQuery(string.Format("select * from Win32_service where name = '{0}'", cmServiceName));

                using (ManagementObjectSearcher serviceSearcher = new ManagementObjectSearcher(serviceScope, query))
                {
                    ManagementObjectCollection serviceCollection = serviceSearcher.Get();
                    foreach (ManagementObject service in serviceCollection)
                    {
                        if (startStopService == "start")
                        {
                            if (service["Started"].Equals(true))
                            {
                                CmTestLog.Success("Chassis Manager service already started");
                                startStopSuccess &= true;
                                continue;
                            }
                            else if (service.GetPropertyValue("State").ToString() == "Stopped")
                            {
                                // Start the service
                                CmTestLog.Info(string.Format("Trying to start Chassis Manager service ..."));
                                
                                service.InvokeMethod("StartService", null);
                                
                                CmTestLog.Info(string.Format("Chassis Manager service is starting. Sleeping for {0} seconds",
                                    CmConstants.CmServiceStartStopSeconds));
                                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.CmServiceStartStopSeconds));
                                startStopSuccess &= true;
                                continue;
                            }
                            else
                            {
                                CmTestLog.Info("Chassis Manager service not in start or stop state");
                                startStopSuccess &= false;
                            }
                        }
                        else if (startStopService == "stop")
                        {
                            if (service["Started"].Equals(true))
                            {
                                // Stop the service
                                CmTestLog.Info(string.Format("Trying to stop Chassis Manager service ..."));
                                
                                service.InvokeMethod("StopService", null);
                                
                                CmTestLog.Info(string.Format("Stopping Chassis Manager service. Sleeping for {0} seconds",
                                    CmConstants.CmServiceStartStopSeconds));
                                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.CmServiceStartStopSeconds));
                                startStopSuccess &= true;
                                continue;
                            }
                            else if (service.GetPropertyValue("State").ToString() == "Stopped")
                            {
                                CmTestLog.Success("Chassis Manager service already stopped");
                                startStopSuccess &= true;
                                continue;
                            }
                            else
                            {
                                CmTestLog.Info("Chassis Manager service not in start or stop state");
                                startStopSuccess &= false;
                            }
                        }
                        else
                        {
                            CmTestLog.Failure("startStopService action not defined to 'start' or 'stop' service");
                            return false;
                        }
                    }
                }
            }
            catch (COMException ce)
            {
                //if we fail for RPC server not being available we need to try one more time
                if (ce.Message.Contains("0x800706BA") && retryStartStop == true)
                {
                    CmTestLog.Info("startStopCmService failed with COMException 0x800706BA. Retrying...");
                    startStopSuccess = StartStopCmService(startStopService, null, false);
                }
                else
                    throw new Exception("Unexpected COMException occurred or retry failed", ce);
            }
            catch (Exception e)
            {
                CmTestLog.Failure("Start or Stop chassis manager service has failed with an exception.");
                CmTestLog.Exception(e, testName);
                return false;
            }
            return startStopSuccess;
        }

        /// <summary>
        /// Restart the CM service
        /// </summary>
        /// <param name="currentApi"></param>
        /// <param name="testName"></param>
        /// <returns></returns>
        protected bool RestartCmService(string currentApi, [CallerMemberName] string testName = null)
        {
            try
            {
                if (!StartStopCmService("stop"))
                    return false;

                if (!StartStopCmService("start"))
                    return false;

                CmTestLog.Success(string.Format("{0}: Successfully restarted Chassis Manager service",
                    currentApi));
                return true;
            }
            catch (Exception e)
            {
                CmTestLog.Failure(string.Format("{0}: Restart Chassis Manager service has failed with an exception.",
                    currentApi));
                CmTestLog.Exception(e, testName);
                return false;
            }
        }
    }
}
