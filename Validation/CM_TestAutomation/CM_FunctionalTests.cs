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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.GFS.WCS.Contracts;

namespace Microsoft.GFS.WCS.Test
{
    public class CM_FunctionalTests
    {
        internal ChannelFactory<IChassisManager> ServiceChannel;
        internal IChassisManager Channel;
        internal List<int> JbodLocations = new List<int> { };
        internal List<int> EmptySlots = new List<int> { };
        internal List<string> AllBladesMacs = new List<string> { };
        internal int ChassisPopulation = 24;
        internal string DefaultCMUrl = "http://CMMTHDVT01:8000";
        private readonly uint numPowerSwitches = 3;
        private readonly uint numFans = 6;
        private readonly uint numPSUs = 6;
        private readonly uint jbodDiskCount = 10;
        private readonly string serviceVersion = "2.0.0.0";
        private readonly int serialTimeoutInSecs = 300;

        public CM_FunctionalTests()
        {
            WebHttpBinding bd = new WebHttpBinding();
            bd.SendTimeout = TimeSpan.FromMinutes(2);

            if (this.DefaultCMUrl.ToLower().Contains("https"))
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
                bd.Security.Mode = WebHttpSecurityMode.Transport;
                bd.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
                this.ServiceChannel = new ChannelFactory<IChassisManager>(bd, this.DefaultCMUrl);
            }
            else
            {
                bd.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
                bd.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
                this.ServiceChannel = new ChannelFactory<IChassisManager>(bd, this.DefaultCMUrl);
            }

            this.ServiceChannel.Endpoint.Behaviors.Add(new WebHttpBehavior());
            this.Channel = this.ServiceChannel.CreateChannel();
        }

        public CM_FunctionalTests(string cMUrl, string uName, string uPassword)
        {
            WebHttpBinding bd = new WebHttpBinding();
            bd.SendTimeout = TimeSpan.FromMinutes(2);

            if (cMUrl.ToLower().Contains("https"))
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
                bd.Security.Mode = WebHttpSecurityMode.Transport;
                bd.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
                this.ServiceChannel = new ChannelFactory<IChassisManager>(bd, cMUrl);
            }
            else
            {
                bd.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
                bd.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
                this.ServiceChannel = new ChannelFactory<IChassisManager>(bd, cMUrl);
            }

            this.ServiceChannel.Endpoint.Behaviors.Add(new WebHttpBehavior());

            // Check if user credentials are specified, if not use default
            if (!string.IsNullOrEmpty(uName))
            {
                // Set user credentials specified
                this.ServiceChannel.Credentials.Windows.ClientCredential =
                    new System.Net.NetworkCredential(uName, uPassword);            
            }
            this.Channel = this.ServiceChannel.CreateChannel();
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate. 
        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        //Test AddChassisControllerUser
        public TestsResultResponse AddChassisControllerUserTest()
        {
            Console.WriteLine("\n!!!!!!!!!! Starting execution of AddChassisControllerUserTest");
                     
            string failureMessage = string.Empty;
            ChassisResponse response = new ChassisResponse();
            
            string adminUserName = "testAdminUser";
            string operatorUserName = "testOperatorUser";
            string userName = "testUser";

            string adminPass1 = "AdminPass1";
            string adminPass2 = "AdminPass2";
            string operatorPass1 = "OperatorPass1";
            string operatorPass2 = "OperatorPass2";
            string userPass1 = "UserPass1";
            string userPass2 = "UserPass2";

            WCSSecurityRole adminRole = WCSSecurityRole.WcsCmAdmin;
            WCSSecurityRole operatorRole = WCSSecurityRole.WcsCmOperator;
            WCSSecurityRole userRole = WCSSecurityRole.WcsCmUser;

            //Remove user Doesn't exist
            response = this.Channel.RemoveChassisControllerUser(adminUserName);
            if (response.completionCode != CompletionCode.UserNotFound || !response.statusDescription.Equals("User not found"))
            {
                failureMessage = "\n!!!Failed when removing a non existant user";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Change user Password and role when user doesn't exist
            response = this.Channel.ChangeChassisControllerUserPassword(adminUserName, adminPass1);
            if (response.completionCode != CompletionCode.UserNotFound || !response.statusDescription.Equals("User not found"))
            {
                failureMessage = "\n!!!Failed when changing password for a non existant user";

                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            response = this.Channel.ChangeChassisControllerUserRole(adminUserName, adminRole);
            if (response.completionCode != CompletionCode.UserNotFound || !response.statusDescription.Equals("User name provided cannot be found"))
            {
                failureMessage = "\n!!!Failed when changing user role of a non existant user";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Add different users
            response = this.Channel.AddChassisControllerUser(adminUserName, adminPass1, adminRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when adding an admin user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }

            response = this.Channel.AddChassisControllerUser(operatorUserName, operatorPass1, operatorRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when adding a an Operator user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }
            response = this.Channel.AddChassisControllerUser(userName, userPass1, userRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when adding a an new user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //Change user passwords
            response = this.Channel.ChangeChassisControllerUserPassword(adminUserName, adminPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing Admin password.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            response = this.Channel.ChangeChassisControllerUserPassword(operatorUserName, operatorPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing Operator password.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            response = this.Channel.ChangeChassisControllerUserPassword(userName, userPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing User password.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Change user roles
            response = this.Channel.ChangeChassisControllerUserRole(adminUserName, userRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing User Role.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }

            response = this.Channel.ChangeChassisControllerUserRole(operatorUserName, adminRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing User Role.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            response = this.Channel.ChangeChassisControllerUserRole(userName, operatorRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when changing User Role.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Remove all users
            response = this.Channel.RemoveChassisControllerUser(adminUserName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when removing Admin user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }

            response = this.Channel.RemoveChassisControllerUser(operatorUserName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when removing Operator user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            response = this.Channel.RemoveChassisControllerUser(userName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed when removing a user.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);  
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ChassisControllerUser tests.............";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage); 
        }

        /// <summary>
        /// A test for ClearBladeLog
        /// </summary>
        public TestsResultResponse ClearBladeLogTest()
        {
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage;

            Console.WriteLine("\n!!!!!!!!! Started execution of ClearBladeLogTest");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Make sure blade is reacheable
            this.Channel.SetPowerOn(randomBlade);
            System.Threading.Thread.Sleep(50000);

            ChassisLogResponse bladeLogs = this.Channel.ReadBladeLog(randomBlade);
            BladeResponse bResponse = null;

            if (CompletionCode.Success != bladeLogs.completionCode)
            {
                failureMessage = "\n!!!Failed to Read blade log";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bResponse = this.Channel.ClearBladeLog(randomBlade);

            if (CompletionCode.Success != bResponse.completionCode)
            {
                failureMessage = "\n!!!Failed to clear blade log";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            
            bResponse = this.Channel.ClearBladeLog(randomBlade);
            if (CompletionCode.Success != bResponse.completionCode)
            {
                failureMessage = "\n!!!Failed to clear blade log";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");            
            failureMessage = "\n!!!!!!!!! Successfully finished execution of  Clear Blade log tests.............";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        //Test for clearChassisLogs
        public TestsResultResponse Clearchassislogtest()
        {
            ChassisLogResponse cMLogsResponse;
            ChassisResponse cMResponse;
            int logCount = 0;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Started running of clearchassislogtest");
            try
            {
                cMLogsResponse = this.Channel.ReadChassisLog();
                logCount = cMLogsResponse.logEntries.Count; 
                if (cMLogsResponse.completionCode != CompletionCode.Success || logCount == 0)
                {
                    failureMessage = "\n!!!!!!!!! Failed when reading the chassis logs for first time. size is possibly too big";
                    Console.WriteLine(failureMessage);                    
                }
            }
            catch (Exception)
            {
                logCount = 50; //setting to a high value. The exception is due to log size being big
            }

            cMResponse = this.Channel.ClearChassisLog();
            if (cMResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!!!!!!! Failed clearing chassis logs";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }

            cMLogsResponse = this.Channel.ReadChassisLog();

            if (cMLogsResponse.logEntries.Count > logCount)
            {
                failureMessage = "\n!!!!!!!!! Failed when reading the chassis logs after clear action.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);                
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of Clear Chassis log tests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);                
        }

        public TestsResultResponse ACSocketPowerTest()
        {
            ChassisResponse acSocketResponse = null;
            ACSocketStateResponse acSocketPower = null;
            uint numACSocket = this.numPowerSwitches;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Started execution of ACSocketPowerTest.");

            for (uint testedACSocket = 1; testedACSocket <= numACSocket; testedACSocket++)
            {
                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedACSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!Failed to power off from unknown state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedACSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("\n!!!Failed to get power state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedACSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!Failed to power off AC socket when it is already off for socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedACSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("\n!!!Failed to get power state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOn(testedACSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!Failed to power ON AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedACSocket);
                if (acSocketPower.powerState != PowerState.ON)
                {
                    failureMessage = string.Format("\n!!!Failed to get power state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOn(testedACSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!Failed to power ON AC socket when it is already ON for AC Socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedACSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!Failed to power off AC socket from ON state for AC Socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedACSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("\n!!!Failed to get power state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedACSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("\n!!!Failed to get power state for AC socket#{0}", testedACSocket);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            //Test for invalid parameters

            acSocketResponse = this.Channel.SetACSocketPowerStateOn(0);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("\n!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            acSocketResponse = this.Channel.SetACSocketPowerStateOn(9999);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("\n!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            acSocketResponse = this.Channel.SetACSocketPowerStateOn(4);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("\n!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ACSocketPowerTests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for GetAllPowerState
        /// </summary>
        public TestsResultResponse GetAllPowerStateTest()
        {
            int MaxNumBlades = (byte)this.ChassisPopulation;
            GetAllPowerStateResponse allPowerStates;
            string powerActionResponse;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Started execution of GetAllPowerStateTest.");
            powerActionResponse = this.HelpSetAllPowerOnTest();

            if (powerActionResponse.Equals("fail"))
            {
                failureMessage = "\n!!!failed during power on";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            allPowerStates = this.Channel.GetAllPowerState();

            foreach (PowerStateResponse bPowerState in allPowerStates.powerStateResponseCollection)
            {
                if (bPowerState.powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!Failed to GetAllPowerState when testing all On states";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);           
                }
            }
            //Test PowerOff state
            powerActionResponse = this.HelpSetAllPowerOffTest();

            if (powerActionResponse.Equals("fail"))
            {
                failureMessage = "\n!!!Failed during power on";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            allPowerStates = this.Channel.GetAllPowerState();

            foreach (PowerStateResponse bPowerState in allPowerStates.powerStateResponseCollection)
            {
                if (bPowerState.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("\n!!!Failed to GetAllPowerState when testing All Off states. failure with blade# {0}", bPowerState.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of GetAllPowerStateTests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A STRESS test for Power actions and PowerState
        /// </summary>
        public TestsResultResponse StressPowerActionsTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            string failureMessage = string.Empty;
            List<CompletionCode> successSerialSessions = new List<CompletionCode>();
            List<CompletionCode> failedSerialSessions = new List<CompletionCode>();
            List<string> powerOnStates = new List<string>();
            List<string> powerOffStates = new List<string>();
            StartSerialResponse serialResponse;
            int randomBlade = 0;

            Console.WriteLine("\n!!!!!!!!! Started execution of GetAllPowerStateTest.");
            
            for (int i = 1; i < 500; i++)
            {
                randomBlade = new Random().Next(1, 9);

                this.Channel.SetPowerOff(randomBlade);
                powerOffStates.Add(this.Channel.GetPowerState(randomBlade).powerState.ToString());

                serialResponse = this.Channel.StartBladeSerialSession(randomBlade, 300);
                failedSerialSessions.Add(serialResponse.completionCode);

                this.Channel.SetPowerOn(randomBlade);
                powerOnStates.Add(this.Channel.GetPowerState(randomBlade).powerState.ToString());
                System.Threading.Thread.Sleep(45000);

                serialResponse = this.Channel.StartBladeSerialSession(randomBlade, 300);
                successSerialSessions.Add(serialResponse.completionCode);
                this.Channel.StopBladeSerialSession(randomBlade, serialResponse.serialSessionToken);
               
                foreach (string state in powerOnStates)
                {
                    if (!state.Equals(PowerState.ON.ToString()))
                    {
                        failureMessage += string.Format("\n!!!The power state for blade# {0} was not reported ON when it should be.", randomBlade);
                    }
                }
                powerOnStates.Clear();

                foreach (string state in powerOffStates)
                {
                    if (!state.Equals(PowerState.OFF.ToString()))
                    {
                        failureMessage += string.Format("\n!!!The power state for blade# {0} was not reported Off when it should be.", randomBlade);
                    }
                }
                powerOffStates.Clear();

                foreach (CompletionCode response in successSerialSessions)
                {
                    if (response != CompletionCode.Success)
                    {
                        failureMessage += string.Format("\n!!!The start serial session should pass for blade# {0} It failed with {1}", randomBlade, response);
                    }
                }
                successSerialSessions.Clear();

                foreach (CompletionCode response in failedSerialSessions)
                {
                    if (response != CompletionCode.DevicePoweredOff)
                    {
                        failureMessage += string.Format("\n!!!The start serial session should fail with DevicePowerOff for blade# {0} it failed with: {1}", randomBlade, response);
                    }
                }
                failedSerialSessions.Clear();
            }

            if (!failureMessage.Equals(string.Empty))
            {
                failureMessage += "\n!!!!!!!!! Finished stress of Power actions the above failures.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            failureMessage = "\n!!!!!!!!! Successfully finished execution of GetAllPowerStateTests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for GetBladeDefaultPowerState
        /// </summary>
        public TestsResultResponse BladeDefaultPowerStateTest()
        {
            BladeStateResponse bDefaultPowerStateResponse;
            BladeStateResponse bState;
            BladeResponse setDfltpState;

            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Started execution of BladeDefaultPowerStateTest");

            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test on";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Make sure blade is reacheable
            this.Channel.SetPowerOn(randomBlade);
            Thread.Sleep(50000);
            this.Channel.SetBladeDefaultPowerStateOff(randomBlade);
            this.Channel.SetPowerOff(randomBlade);
            Thread.Sleep(5000);
            this.Channel.SetPowerOn(randomBlade);
            Thread.Sleep(80000);
            if (this.Channel.GetPowerState(randomBlade).powerState != PowerState.ON)
            {
                failureMessage = "\n!!!Failed During poweron";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bState = this.Channel.GetBladeState(randomBlade);
            if (bState.bladeState != PowerState.OFF)
            {
                failureMessage = string.Format("\n!!!Blade failed to stay off as its the default state for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(randomBlade);
            if (bDefaultPowerStateResponse.bladeState != PowerState.OFF)
            {
                failureMessage = string.Format("\n!!!Default blade power state is not correct. State should be OFF for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.SetBladeDefaultPowerStateOn(randomBlade);
            this.Channel.SetPowerOff(randomBlade);
            Thread.Sleep(5000);
            this.Channel.SetPowerOn(randomBlade);
            Thread.Sleep(80000);

            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(randomBlade);
            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(randomBlade);
            bState = this.Channel.GetBladeState(randomBlade);
            if (bDefaultPowerStateResponse.bladeState != PowerState.ON)
            {
                failureMessage = string.Format("\n!!!Default blade power state is not correct. State should be ON for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            if (bState.bladeState != PowerState.ON)
            {
                failureMessage = string.Format("\n!!!Blade failed to Power On as its the default state for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Negative tests - SetBladeDefaultPowerStateOn

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOn(0);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateON Did not return a parameterOutOfRange when bladeId is 0.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOn(-1);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateON Did not return a parameterOutOfRange when bladeId is -1.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOn(25);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateON Did not return a parameterOutOfRange when bladeId is 25.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //SetDFLTPState = this.channel.SetBladeDefaultPowerStateOn(9999);
            //if (SetDFLTPState.completionCode != CompletionCode.ParameterOutOfRange)
            //{
            //    failureMessage = "\n!!!SetBladeDefaultPowerStateON Did not return a parameterOutOfRange when bladeId is 9999.";
            //    Console.WriteLine(failureMessage);
            //    return new TestsResultResponse(executionResult.Failed, failureMessage);
            //}

            //Negative tests - SetBladeDefaultPowerStateOff

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOff(0);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateOff Did not return a parameterOutOfRange when bladeId is 0.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOff(-1);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateOff Did not return a parameterOutOfRange when bladeId is -1.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            setDfltpState = this.Channel.SetBladeDefaultPowerStateOff(25);
            if (setDfltpState.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!SetBladeDefaultPowerStateOff Did not return a parameterOutOfRange when bladeId is 25.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //SetDFLTPState = this.channel.SetBladeDefaultPowerStateOff(9999);
            //if (SetDFLTPState.completionCode != CompletionCode.ParameterOutOfRange)
            //{
            //    failureMessage = "\n!!!SetBladeDefaultPowerStateOFF Did not return a parameterOutOfRange when bladeId is 9999.";
            //    Console.WriteLine(failureMessage);
            //    return new TestsResultResponse(executionResult.Failed, failureMessage);
            //}

            //Negative tests - GetBladeDefaultPowerState

            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(0);
            if (bDefaultPowerStateResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!GetBladeDefaultPowerState Did not return a parameterOutOfRange when bladeId is 0.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(-1);
            if (bDefaultPowerStateResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!GetBladeDefaultPowerState Did not return a parameterOutOfRange when bladeId is -1.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bDefaultPowerStateResponse = this.Channel.GetBladeDefaultPowerState(25);
            if (bDefaultPowerStateResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!GetBladeDefaultPowerState Did not return a parameterOutOfRange when bladeId is 25.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //bDefaultPowerStateResponse = this.channel.GetBladeDefaultPowerState(9999);
            //if (bDefaultPowerStateResponse.completionCode != CompletionCode.ParameterOutOfRange)
            //{
            //    failureMessage = "\n!!!GetBladeDefaultPowerState Did not return a parameterOutOfRange when bladeId is 9999.";
            //    Console.WriteLine(failureMessage);
            //    return new TestsResultResponse(executionResult.Failed, failureMessage);
            //}

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of BladeDefaultPowerstateTests.";           
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse CheckChassisInfo(string SKUXMLFile = "Default")
        {
            ChassisInfoResponse chassisInfo = new ChassisInfoResponse();
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of CheckChassisInfo tests.");
            chassisInfo = this.Channel.GetChassisInfo(true, true, true, true);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get all chassis components information";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            
            chassisInfo = this.Channel.GetChassisInfo(true, false, false, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when asking for only blade information";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(true, true, false, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when excluding Chassis controller info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(true, false, true, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when excluding PSU info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(false, true, true, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when excluding Blades info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(false, true, false, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when including only PSU info.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(false, false, true, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when asking only for Chassis controller info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            chassisInfo = this.Channel.GetChassisInfo(false, false, false, false);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when excluding all info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            chassisInfo = this.Channel.GetChassisInfo(false, false, false, true);
            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to get chassis information when asking only for Batteries info";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of CheckChassisInfo tests";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse VerifyBladesInfo(string skuxmlFile, int index = 1701)
        {
            GetAllBladesInfoResponse allBladesInfo = new GetAllBladesInfoResponse();
            string failureMessage = string.Empty;
            string firmwareVer;
            string hardmwareVer;
                        
            //read the expected values for firmware and hardware versions
            string skuDefinition = skuxmlFile;
            using (XmlReader xr = XmlReader.Create(skuDefinition))
            {
                xr.ReadToFollowing("firmwareVersion");
                firmwareVer = xr.ReadString();
                xr.ReadToFollowing("hardwareVersion");
                hardmwareVer = xr.ReadString();
            }
                
            //Verify blade info
            Console.WriteLine("\n!!!!!!!!! Starting verificatgion of blades info");

            // Only health for specified blade is needed.
            if (index != 1701)
            {
                allBladesInfo.bladeInfoResponseCollection.Add(this.Channel.GetBladeInfo(index));
            }
            else
            {
                allBladesInfo = this.Channel.GetAllBladesInfo();
            }
            foreach (BladeInfoResponse binfo in allBladesInfo.bladeInfoResponseCollection)
            {
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && binfo.completionCode != CompletionCode.Success)
                {
                    failureMessage = "\n!!! Blade Information failure: Failed to get blade information.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && binfo.bladeType.Equals("Server") && !(binfo.firmwareVersion.Equals(firmwareVer)))
                {
                    failureMessage = string.Format("\n!!! Blade Information failure: Failed to verifiy Firmware version for blade# {0}", binfo.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && binfo.bladeType.Equals("Server") && binfo.hardwareVersion != hardmwareVer)
                {
                    failureMessage = string.Format("\n!!! Blade Information failure: Failed to verify Hardware version for blade# {0}", binfo.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && binfo.macAddress.First<NicInfo>().completionCode != CompletionCode.Success && binfo.bladeType.Equals("Server"))
                {
                    failureMessage = string.Format("\n!!! Blade Information failure: failed to get NicInfo for blade# {0}", binfo.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && string.IsNullOrEmpty(binfo.macAddress.First<NicInfo>().macAddress) && binfo.bladeType.Equals("Server"))
                {
                    failureMessage = string.Format("\n!!! Blade Information failure: Failed to verify macaddress for blade# {0}", binfo.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                if (!this.EmptySlots.Contains(binfo.bladeNumber) && string.IsNullOrEmpty(binfo.serialNumber))
                {
                    failureMessage = string.Format("\n!!! Blade Information failure: Failed to verify SerialNumber for blade# {0}", binfo.bladeNumber);
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of CheckBladesinfo tests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse CheckChassisHealth()
        {
            ChassisHealthResponse cMHealthResponse = this.Channel.GetChassisHealth(true, true, true, true);
            bool allChassisHealthPass = true;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting verificatgion of The chassis health");

            //Verify Chassis population
            if (cMHealthResponse.bladeShellCollection.Count != this.ChassisPopulation)
            {
                failureMessage = "\n!!! Chassis health failure: The number of reported blades in the chassis in not right.";
                Console.WriteLine(failureMessage);
            }

            //Verify health of every blade
            foreach (BladeShellResponse shellResponse in cMHealthResponse.bladeShellCollection)
            {
                if (!this.EmptySlots.Contains(shellResponse.bladeNumber) && !shellResponse.bladeState.Equals("Healthy"))
                {
                    failureMessage += "\n!!! Chassis health failure: Blade is not in healthy state.";
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                }
            }

            //Verify fan health
            if (cMHealthResponse.fanInfoCollection.Count != this.numFans)
            {
                failureMessage += "\n!!! Chassis health failure: The number of reported fans in the chassis in not right.";
                Console.WriteLine(failureMessage);
                allChassisHealthPass = false;
            }

            foreach (FanInfo fanInfo in cMHealthResponse.fanInfoCollection)
            {
                if (fanInfo.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!! Chassis health failure: Failed to get health information about fan # {0}", fanInfo.fanId);
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                }
                if (fanInfo.isFanHealthy != true)
                {
                    failureMessage = string.Format("\n!!! Chassis health failure: fan is not in healthy state; fan # {0}", fanInfo.fanId);
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                }
            }

            //Verify PSU health
            if (cMHealthResponse.psuInfoCollection.Count != this.numPSUs)
            {
                failureMessage += "\n!!! Chassis health failure: The number of reported PSUs in the chassis in not right.";
                Console.WriteLine(failureMessage);
                allChassisHealthPass = false;
            }

            foreach (PsuInfo psuInfo in cMHealthResponse.psuInfoCollection)
            {
                if (psuInfo.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!! Chassis health failure: Failed to get health information about PSU # {0}", psuInfo.id);
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                }
                if (!psuInfo.state.ToString().Equals("ON"))
                {
                    failureMessage += string.Format("\n!!! Chassis health failure: PSU is not in healthy state. PSU # {0}", psuInfo.id);
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                }
                if (psuInfo.serialNumber.Equals(string.Empty))
                {
                    failureMessage += string.Format("\n!!! Chassis health failure: Serial Number is empty for PSU # {0}", psuInfo.id);
                    Console.WriteLine(failureMessage);
                    allChassisHealthPass = false;
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }
            if (allChassisHealthPass == false)
            {
                Console.WriteLine("\n--------------------------------");
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            else
            {
                Console.WriteLine("\n++++++++++++++++++++++++++++++++");
                failureMessage += "\n!!!!!!!!! Successfully finished execution of CheckChassisHealth tests.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
            }
        }

        public TestsResultResponse VerifyBladesHealth(string skuxmlFile, int index = 1701)
        {
            bool allHealthPass = true;
            List<BladeHealthResponse> allBladesHealthresponse = new List<BladeHealthResponse>();
            string failureMessage = string.Empty;
            string propertyValue;
            
            string skuDefinition = skuxmlFile;
            
            // Only health for specified blade is needed.
            if (index != 1701)
            {
                Console.WriteLine("\n!!!!!!!!! Starting verification of Blade {0} health", index);
                allBladesHealthresponse.Add(this.Channel.GetBladeHealth(index, true, true, true, true, true, true, true));
            }
            //We need to get health for all blades
            else
            {
                Console.WriteLine("\n!!!!!!!!! Starting verification of all Blades health");
                for (int bladeid = 1; bladeid <= this.ChassisPopulation; bladeid++)
                {
                    if (!this.EmptySlots.Contains(bladeid))
                    {
                        allBladesHealthresponse.Add(this.Channel.GetBladeHealth(bladeid, true, true, true, true, true, true, true));
                    }
                }
            }

            foreach (BladeHealthResponse bHealthResponse in allBladesHealthresponse)
            {
                //bladeShell
                if (bHealthResponse.bladeShell.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!! Blade health failure: Failed to get bootShell information for blade #{0}", bHealthResponse.bladeNumber);
                    Console.WriteLine(failureMessage);
                    allHealthPass = false;
                }
                if (bHealthResponse.bladeShell.bladeState != "Healthy")
                {
                    failureMessage += "\n!!! Blade health failure: Blade in Non healthy state.";
                    allHealthPass = false;
                }

                //Verify Disk Information for JBODs
                if (bHealthResponse.bladeShell.bladeType.ToUpper().Equals("JBOD"))
                {
                    if (bHealthResponse.jbodDiskInfo.diskCount != this.jbodDiskCount)
                    {
                        failureMessage += "\n!!! Blade health failure: JBOD Disk count is not right.";
                        allHealthPass = false;
                    }
                    if (bHealthResponse.bladeShell.bladeState != "Healthy")
                    {
                        failureMessage += "\n!!! Blade health failure: JBOD is not healthy.";
                        allHealthPass = false;
                    }
                    foreach (DiskInfo JBDiskInfo in bHealthResponse.jbodDiskInfo.diskInfo)
                    {
                        if (JBDiskInfo.completionCode != CompletionCode.Success)
                        {
                            failureMessage += "\n!!! Blade health failure: Failed in getting JBOD Disk Information.";
                            allHealthPass = false;
                        }
                        if (JBDiskInfo.diskStatus != "Normal")
                        {
                            failureMessage += "\n!!! Blade health failure: JBOD Disk State is not Normal.";
                            allHealthPass = false;
                        }
                    }
                }
                //Verify information about server blades
                else
                {
                    //Disk information
                    /* XmlReader bladeDiskInfo = XmlReader.Create(SKUDefinition);
                    foreach (DiskInfo ServerDiskInfo in bHealthResponse.bladeDisk)
                    {
                    if (ServerDiskInfo.completionCode != CompletionCode.Success)
                    {
                    failureMessage += "\n!!! Blade health failure: Failed in getting Server Disk Information for blade:Disk# " + bHealthResponse.bladeNumber + ServerDiskInfo.diskId;
                    allHealthPass = false;
                    }
                    bladeDiskInfo.ReadToFollowing("diskStatus");
                    PropertyValue = bladeDiskInfo.ReadElementContentAsString();
                    if (ServerDiskInfo.diskStatus != PropertyValue)
                    {
                    failureMessage += "\n!!! Blade health failure: DiskStatus failed for blade:Disk# " + bHealthResponse.bladeNumber + ":" + ServerDiskInfo.diskId;
                    allHealthPass = false;
                    }
                    bladeDiskInfo.MoveToNextAttribute();
                    }
                    * */
                    //Memory Information
                    XmlReader memInfo = XmlReader.Create(skuDefinition);
                    
                    foreach (MemoryInfo serverDimmInfo in bHealthResponse.memoryInfo)
                    {
                        if (serverDimmInfo.completionCode != CompletionCode.Success)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in getting Server DIMM Information for blade# {0}", bHealthResponse.bladeNumber);
                            allHealthPass = false;
                        }
                       
                        memInfo.ReadToFollowing("dimmType");
                        propertyValue = memInfo.ReadElementContentAsString();                        
                        if (serverDimmInfo.dimmType != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying DIMM Type for blade:DIMM {0}:{1}", bHealthResponse.bladeNumber, serverDimmInfo.dimm); 
                            allHealthPass = false;
                        }

                        memInfo.ReadToFollowing("status");
                        propertyValue = memInfo.ReadElementContentAsString();
                        if (serverDimmInfo.status != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying DIMM Status for blade:DIMM {0}:{1}", bHealthResponse.bladeNumber, serverDimmInfo.dimm);
                            allHealthPass = false;
                        }

                        memInfo.ReadToFollowing("speed");
                        propertyValue = memInfo.ReadElementContentAsString();
                        if (serverDimmInfo.speed != ushort.Parse(propertyValue))
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying DIMM Speed for blade:DIMM {0}:{1}", bHealthResponse.bladeNumber, serverDimmInfo.dimm);
                            allHealthPass = false;
                        }
             
                        memInfo.ReadToFollowing("size");
                        propertyValue = memInfo.ReadElementContentAsString();
                        if (serverDimmInfo.size != ushort.Parse(propertyValue))
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying DIMM Size for blade:DIMM {0}:{1}", bHealthResponse.bladeNumber, serverDimmInfo.dimm);
                            allHealthPass = false;
                        }

                        memInfo.ReadToFollowing("memVoltage");
                        propertyValue = memInfo.ReadElementContentAsString();
                        if (serverDimmInfo.memVoltage != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying DIMM Voltage for blade:DIMM {0}:{1}", bHealthResponse.bladeNumber, serverDimmInfo.dimm);
                            allHealthPass = false;
                        }

                        memInfo.MoveToNextAttribute();
                    }

                    //PCIe Information 
                    XmlReader pCIeInfo = XmlReader.Create(skuDefinition);

                    foreach (PCIeInfo serverPCIeInfo in bHealthResponse.pcieInfo)
                    {
                        if (serverPCIeInfo.completionCode != CompletionCode.Success)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in getting Server PCIe Information for blade:PCIeNumber {0}:{1}", bHealthResponse.bladeNumber, serverPCIeInfo.pcieNumber);
                            allHealthPass = false;
                        }

                        pCIeInfo.ReadToFollowing("vendorId");
                        propertyValue = pCIeInfo.ReadElementContentAsString();

                        if (serverPCIeInfo.vendorId != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying PCIe VendorId for blade:PCIeNumber {0}:{1}", bHealthResponse.bladeNumber, serverPCIeInfo.pcieNumber);
                            allHealthPass = false;
                        }

                        pCIeInfo.ReadToFollowing("deviceId");
                        propertyValue = pCIeInfo.ReadElementContentAsString();

                        if (serverPCIeInfo.deviceId != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying PCIe DeviceId for blade:PCIeNumber {0}:{1}", bHealthResponse.bladeNumber, serverPCIeInfo.pcieNumber);
                            allHealthPass = false;
                        }
                                                
                        //PCIeInfo.ReadToFollowing("systemId");
                        //PropertyValue = PCIeInfo.ReadElementContentAsString();
                        //if (ServerPCIeInfo.systemId != ushort.Parse(PropertyValue))
                        //{
                        //    failureMessage += "\n!!! Blade health failure: Failed in verifying SystemId for blade:PCIeNumber " + bHealthResponse.bladeNumber + ":" + ServerPCIeInfo.pcieNumber;
                        //    allHealthPass = false;
                        //}                        

                        pCIeInfo.ReadToFollowing("subSystemId");
                        propertyValue = pCIeInfo.ReadElementContentAsString();
                        if (serverPCIeInfo.subSystemId != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying SubsystemId for blade:PCIeNumber {0}:{1}", bHealthResponse.bladeNumber, serverPCIeInfo.pcieNumber);
                            allHealthPass = false;
                        }

                        pCIeInfo.ReadToFollowing("status");
                        propertyValue = pCIeInfo.ReadElementContentAsString();
                        if (serverPCIeInfo.status != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying PCIe status for blade:PCIeNumber {0}:{1}", bHealthResponse.bladeNumber, serverPCIeInfo.pcieNumber);
                            allHealthPass = false;
                        }

                        pCIeInfo.MoveToNextAttribute();
                    }

                    //Processor Information
                    XmlReader processorInfo = XmlReader.Create(skuDefinition);
                   
                    foreach (ProcessorInfo ServerProcInfo in bHealthResponse.processorInfo)
                    {
                        if (ServerProcInfo.completionCode != CompletionCode.Success)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in getting Server Processor Information for blade# {0}", bHealthResponse.bladeNumber);
                            allHealthPass = false;
                        }

                        processorInfo.ReadToFollowing("procType");
                        propertyValue = processorInfo.ReadElementContentAsString();
                        if (ServerProcInfo.procType != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying Server Processor Type for blade:procId {0}:{1}", bHealthResponse.bladeNumber, ServerProcInfo.procId);
                            allHealthPass = false;
                        }

                        processorInfo.ReadToFollowing("state");
                        propertyValue = processorInfo.ReadElementContentAsString();
                        if (ServerProcInfo.state != propertyValue)
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying Server Processor Presence for blade:procId {0}:{1}", bHealthResponse.bladeNumber, ServerProcInfo.procId);
                            allHealthPass = false;
                        }

                        processorInfo.ReadToFollowing("frequency");
                        propertyValue = processorInfo.ReadElementContentAsString();
                        if (ServerProcInfo.frequency != ushort.Parse(propertyValue))
                        {
                            failureMessage += string.Format("\n!!! Blade health failure: Failed in verifying Server Processor Frequency for blade:procId {0}:{1}", bHealthResponse.bladeNumber, ServerProcInfo.procId);
                            allHealthPass = false;
                        }

                        processorInfo.MoveToNextAttribute();
                    }
                }
            }
            
            if (allHealthPass == false)
            {
                Console.WriteLine("\n--------------------------------");
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            else
            {
                Console.WriteLine("\n++++++++++++++++++++++++++++++++");
                failureMessage += "\n!!!!!!!!! Successfully finished execution of CheckBladesHealth tests.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
            }
        }

        /// <summary>
        /// A test for GetAllBladesPowerLimit
        /// </summary>
        public TestsResultResponse AllBladesPowerLimitTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            int powerLimitValue = 777;
            AllBladesResponse allbResponse;
            GetAllBladesPowerLimitResponse allbladesPowerLimitResponse;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of AllBladesPowerLimitTest.");
            this.Channel.SetAllPowerOn();
            this.Channel.SetAllBladesOn();

            allbResponse = this.Channel.SetAllBladesPowerLimit(powerLimitValue);
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!Failed to set blade power limit for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbResponse = this.Channel.SetAllBladesPowerLimitOn();
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!Failed to set blade power limit ON for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbladesPowerLimitResponse = this.Channel.GetAllBladesPowerLimit();

            foreach (BladePowerLimitResponse bPowerLimitResponse in allbladesPowerLimitResponse.bladePowerLimitCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bPowerLimitResponse.completionCode != CompletionCode.Success || powerLimitValue != bPowerLimitResponse.powerLimit)
                    {
                        failureMessage = string.Format("\n!!!Failed to get blade power limit for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bPowerLimitResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbResponse = this.Channel.SetAllBladesPowerLimitOff();
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!Failed to set blade power limit OFF for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of AllBladesPowerLmitTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetBladePowerLimitOnOff
        /// </summary>
        public TestsResultResponse SetBladePowerLimitOnOffTest()
        {
            BladeResponse bResponse;
            BladePowerLimitResponse bPowerLimitResponse;
            double powerLimitValue = 500;
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetBladePowerLimitOnOffTest.");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Make sure the blade is on
            this.Channel.SetPowerOn(randomBlade);
            System.Threading.Thread.Sleep(50000);
            this.Channel.SetBladeOn(randomBlade);
            System.Threading.Thread.Sleep(30000);

            bResponse = this.Channel.SetBladePowerLimit(randomBlade, double.MaxValue);
            if (bResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!Set Blade Power limit Did not return a parameterOutOfRange when setting to MaxValue.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            
            bResponse = this.Channel.SetBladePowerLimit(randomBlade, double.MinValue);
            if (bResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = "\n!!!Set Blade Power limit Did not return a parameterOutOfRange when setting to MinValue.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bResponse = this.Channel.SetBladePowerLimit(randomBlade, powerLimitValue);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bResponse = this.Channel.SetBladePowerLimitOn(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit ON failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bPowerLimitResponse = this.Channel.GetBladePowerLimit(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bPowerLimitResponse = this.Channel.GetBladePowerLimit(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bPowerLimitResponse = this.Channel.GetBladePowerLimit(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bResponse = this.Channel.SetBladePowerLimitOff(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit OFF failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bResponse = this.Channel.SetBladePowerLimitOff(randomBlade);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Set Blade Power limit OFF failed with response {0}", bResponse.completionCode);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage += "\n!!!!!!!!! Successfully finished execution of SetBladePowerLimitOnOffTests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse GetBladePowerReadingTest()
        {
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of GetBladePowerReadingTest");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= (byte)this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > (byte)this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            BladePowerReadingResponse powerReadingresponse = new BladePowerReadingResponse();

            //make sure blade is reacheable
            this.Channel.SetPowerOn(randomBlade);
            powerReadingresponse = this.Channel.GetBladePowerReading(randomBlade);
            if (powerReadingresponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!Request to read blade power failed for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of GetBladePowerReadingTests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for GetAllBladesPowerReading
        /// </summary>
        public TestsResultResponse GetAllBladesPowerReadingTest()
        {
            string bladePower;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of GetAllBladesPowerReadingTest.");
            bladePower = this.HelpSetAllPowerOnTest();

            if (bladePower.Equals("fail"))
            {
                failureMessage = "\n!!!failed during power on.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            GetAllBladesPowerReadingResponse allBladesPowerReadingResponse = this.Channel.GetAllBladesPowerReading();

            foreach (BladePowerReadingResponse bPowerReadingResponse in allBladesPowerReadingResponse.bladePowerReadingCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bPowerReadingResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = "\n!!!Failed to get blade power reading.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex))
                    {
                        if (bPowerReadingResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                        {
                            failureMessage = "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade.";
                            Console.WriteLine(failureMessage);
                            return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                        }
                    }
                }
                bladeIndex++;
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of GetAllBladesPowerReadingTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse GetChassisNetworkPropertiesTest()
        {
            ChassisNetworkPropertiesResponse cMPropertiesResponse;
            cMPropertiesResponse = this.Channel.GetChassisNetworkProperties();
            int index = 0;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of GetChassisNetworkPropertiesTest");

            ChassisNetworkProperty[] arrayCMPropertiesResponse = cMPropertiesResponse.chassisNetworkPropertyCollection.ToArray();

            for (int i = 0; i < arrayCMPropertiesResponse.Length; i++)
            {
                if (arrayCMPropertiesResponse[i].dhcpEnabled == true)
                {
                    index = i;
                }
            }

            if (arrayCMPropertiesResponse[index].completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Faield to get chassis network properties.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            if (arrayCMPropertiesResponse[index].dnsHostName != TestConfigLoaded.CMBiosName)
            {
                failureMessage = "\n!!!CM bios name was not verified.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            if (arrayCMPropertiesResponse[index].dnsDomain != TestConfigLoaded.CMDomain)
            {
                failureMessage = "\n!!!CM domain was not verified.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            if (arrayCMPropertiesResponse[index].ipAddress != TestConfigLoaded.CmipAddress)
            {
                failureMessage = "\n!!!CM IP @ was not verified.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            if (arrayCMPropertiesResponse[index].subnetMask != TestConfigLoaded.CMSubnetMask)
            {
                failureMessage = "\n!!!CM subnet was not verified.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            if (arrayCMPropertiesResponse[index].macAddress != TestConfigLoaded.CMMacAddress)
            {
                failureMessage = "\n!!!CM Mac @ was not verified.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of GetChassisNetworkPropertiesTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse ReadBladeLogTest()
        {
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of ReadBladeLogTest.");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            ChassisLogResponse bladeLogs;

            //make sure blade is reacheable
            this.Channel.SetPowerOn(randomBlade);
            bladeLogs = this.Channel.ReadBladeLog(randomBlade);

            if (bladeLogs.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to read logs.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Clear logs and read again
            this.Channel.ClearBladeLog(randomBlade);
            bladeLogs = this.Channel.ReadBladeLog(randomBlade);

            if (bladeLogs.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to read logs.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //clear logs again.
            this.Channel.ClearBladeLog(randomBlade);

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ReadBladeLogTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse ReadBladeLogWithTimestampTest()
        {
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of ReadBladeLogWithTimestampTest");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to read logs.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            ChassisLogResponse bladeLogs;

            //make sure blade is reacheable
            this.Channel.SetPowerOn(randomBlade);
            bladeLogs = this.Channel.ReadBladeLogWithTimestamp(randomBlade, System.DateTime.MinValue, System.DateTime.Now);

            if (bladeLogs.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to read logs.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.ClearBladeLog(randomBlade);
            this.Channel.ClearBladeLog(randomBlade);

            bladeLogs = this.Channel.ReadBladeLogWithTimestamp(randomBlade, System.DateTime.MinValue, System.DateTime.MinValue);

            if (bladeLogs.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to read logs.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ReadBladeLogWithTimestampTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse ReadChassisLogTest()
        {
            ChassisLogResponse CMLogReponse;
            int logSize = 0;
            int minLogSize = 4;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of ReadChassisLogTest");
            //Make sure all commands are logged in the file. Have an assert for each command
            this.Channel.ClearChassisLog();
            CMLogReponse = this.Channel.ReadChassisLog();
            logSize = CMLogReponse.logEntries.Count;

            if (logSize != minLogSize)
            {
                failureMessage = "\n!!!The user log file was just cleared. It should have had only one command entry.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            if (!(CMLogReponse.logEntries[logSize - 1].eventDescription.Contains("ReadChassisLog")))
            {
                failureMessage = "\n!!!Not the expected readChassisLog command.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            
            this.Channel.ReadChassisLog();
            this.Channel.SetChassisAttentionLEDOn();
            this.Channel.SetChassisAttentionLEDOff();

            //wait for logs to show in the log file
            Thread.Sleep(10000);

            CMLogReponse = this.Channel.ReadChassisLog();

            logSize = CMLogReponse.logEntries.Count;

            //the last action must be the "ReadChassisLog()" on the CM. If not we will fail the test.
            //This will require that the CM doesn't receive any other user commands.
            //We are logging three extra line about: 1- the user requesting 2- Group the user belongs to. 3- Authorized or not
            if (!CMLogReponse.logEntries[logSize - 1].eventDescription.Contains("ReadChassisLog"))
            {
                failureMessage = "\n!!!ReadChassisLog is not present.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            if (!(CMLogReponse.logEntries[logSize - 5].eventDescription.Contains("SetChassisAttentionLEDOff")))
            {
                failureMessage = "\n!!!SetChassisAttentionLEDOff is not present.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            if (!(CMLogReponse.logEntries[logSize - 9].eventDescription.Contains("SetChassisAttentionLEDOn")))
            {
                failureMessage = "\n!!!SetChassisAttentionLEDOn is not present.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ReadChassisLogTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        public TestsResultResponse ReadChassisLogWithTimestampTest()
        {
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of ReadChassisLogWithTimestampTest");
            //need to clear the chassis log so the response is not too big
            this.Channel.ClearChassisLog();

            //Add couple more calls
            this.Channel.SetChassisAttentionLEDOn();
            this.Channel.SetChassisAttentionLEDOff();

            ChassisLogResponse cMLogReponse = this.Channel.ReadChassisLog();

            DateTime start = DateTime.Now.AddDays(-1);            
            DateTime end = DateTime.Now.AddDays(1);
            ChassisLogResponse cMLogReponseWithTime = this.Channel.ReadChassisLogWithTimestamp(start, end);

            bool timestart = cMLogReponse.logEntries[1].eventTime > start;
            bool timeEnd = cMLogReponse.logEntries[cMLogReponse.logEntries.Count - 1].eventTime < end;
            
            int logSize = cMLogReponseWithTime.logEntries.Count;

            if (!cMLogReponseWithTime.logEntries[0].eventDescription.Contains("SetChassisAttentionLEDOn"))
            {
                failureMessage = "\n!!!Failed to read the CM logs with a time range.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //This will require that the CM doesn't receive any other user commands.
            if (!cMLogReponseWithTime.logEntries[logSize - 1].eventDescription.Contains("ReadChassisLog"))
            {
                failureMessage = "\n!!!Failed to read the CM logs with a time range.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of ReadChassisLogWithTimestampTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetBladeActivePowerCycle
        /// </summary>
        public TestsResultResponse SetBladeActivePowerCycleTest()
        {
            BladeStateResponse response = new BladeStateResponse();
            string failureMessage = string.Empty;

            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetBladeActivePowerCycleTest.");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server machine to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.SetPowerOn(randomBlade);
            System.Threading.Thread.Sleep(50000);
            this.Channel.SetBladeOn(randomBlade);
            System.Threading.Thread.Sleep(50000);

            //Make sure blade is on to start with
            response = this.Channel.GetBladeState(randomBlade);
            if (response.bladeState != PowerState.ON)
            {
                failureMessage = "\n!!!Blade should be ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //PowerCycle blade
            this.Channel.SetBladeActivePowerCycle(randomBlade, 20);
            System.Threading.Thread.Sleep(10000);

            response = this.Channel.GetBladeState(randomBlade);
            if (response.bladeState != PowerState.OFF)
            {
                failureMessage = string.Format("\n!!!Blade should still be off for blade# {0}", randomBlade);
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            System.Threading.Thread.Sleep(20000);
            response = this.Channel.GetBladeState(randomBlade);
            if (response.bladeState != PowerState.ON)
            {
                failureMessage = "\n!!!Blade should be already turned ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetBladeActivePowerCycleTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetAllBladesActivePowerCycle
        /// </summary>
        public TestsResultResponse SetAllBladesActivePowerCycleTest()
        {
            GetAllBladesStateResponse allBladesStateResponse;
            AllBladesResponse allbladesResponse;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            this.Channel.SetAllPowerOn();
            System.Threading.Thread.Sleep(50000);
            this.Channel.SetAllBladesOn();
            System.Threading.Thread.Sleep(30000);

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetAllBladesActivePowerCycleTest.");
            allBladesStateResponse = this.Channel.GetAllBladesState();
            //Make sure blade is on to start with
            foreach (BladeStateResponse bStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bStateResponse.bladeState != PowerState.ON)
                    {
                        failureMessage = string.Format("\n!!!Power cycle failed: Failure getting blades state for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This is a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset bladeIndex
            bladeIndex = 1;
            //Powercycle all blades together
            allbladesResponse = this.Channel.SetAllBladesActivePowerCycle(100);
            System.Threading.Thread.Sleep(10);
            allBladesStateResponse = this.Channel.GetAllBladesState();
            //Make sure powercycle is successful for all blades
            foreach (BladeResponse bResponse in allbladesResponse.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))                    
                {
                    if (bResponse.completionCode != CompletionCode.Success || allBladesStateResponse.bladeStateResponseCollection[bladeIndex].bladeState != PowerState.OFF)
                    {
                        failureMessage = string.Format("\n!!!Blade powercycle failed for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset bladeIndex
            bladeIndex = 1;

            System.Threading.Thread.Sleep(10000);
            allBladesStateResponse = this.Channel.GetAllBladesState();
            foreach (BladeStateResponse bStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bStateResponse.bladeState != PowerState.OFF)
                    {
                        failureMessage = string.Format("\n!!!Blade should still be off for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            //reset bladeIndex
            bladeIndex = 1;
            System.Threading.Thread.Sleep(100000);
            //Get the blade state after the timeout is finished
            allBladesStateResponse = this.Channel.GetAllBladesState();
            foreach (BladeStateResponse bStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bStateResponse.bladeState != PowerState.ON)
                    {
                        failureMessage = string.Format("\n!!!Blade should be already turned ON for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# {0}", bladeIndex);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetAllBladesActivePowerCycleTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetAllPowerOnOff
        /// </summary>
        public TestsResultResponse SetAllPowerOnOffTest()
        {
            AllBladesResponse allBladesResponse;
            int maxNumBlades = (byte)this.ChassisPopulation;
            int bladeid = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetAllPowerOnOffTest.");
            allBladesResponse = this.Channel.SetAllPowerOff();

            foreach (BladeResponse bResponse in allBladesResponse.bladeResponseCollection)
            {
                if (bResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = "PowerOff failed.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeid++;
            }

            //verify Off state
            for (int i = 1; i <= maxNumBlades; i++)
            {
                if (this.Channel.GetPowerState(i).powerState != PowerState.OFF)
                {
                    failureMessage = "\n!!!Failed during power state verification after powerOff.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }
            bladeid = 1;
            allBladesResponse = this.Channel.SetAllPowerOff();

            foreach (BladeResponse bResponse in allBladesResponse.bladeResponseCollection)
            {
                if (bResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = "\n!!!SetAllPowerOff verification failed.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeid++;
            }

            //verify Off state
            for (int i = 1; i <= maxNumBlades; i++)
            {
                if (this.Channel.GetPowerState(i).powerState != PowerState.OFF)
                {
                    failureMessage = "\n!!!Failed during power state verification after setAllPowerOff.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            bladeid = 1;
            allBladesResponse = this.Channel.SetAllPowerOn();

            foreach (BladeResponse bResponse in allBladesResponse.bladeResponseCollection)
            {
                if (bResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = "\n!!!Failed during power state verification after SetAllPowerOn.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeid++;
            }

            Thread.Sleep(50000);
            for (int i = 1; i <= maxNumBlades; i++)
            {
                if (this.Channel.GetPowerState(i).powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!Failed during power state verification after powerOn.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            bladeid = 1;
            allBladesResponse = this.Channel.SetAllPowerOn();
            foreach (BladeResponse bResponse in allBladesResponse.bladeResponseCollection)
            {
                if (bResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = "\n!!!SetAllPowerOn verification failed.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeid++;
            }

            Thread.Sleep(50000);
            for (int i = 1; i <= maxNumBlades; i++)
            {
                if (this.Channel.GetPowerState(i).powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!Failed during power state verification after powerOn.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetAllPowerOnOff.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetAllBladesDefaultPowerStateOnOff
        /// </summary>
        public TestsResultResponse SetAllBladesDefaultPowerStateOnOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            int bladeIndex = 1;
            GetAllPowerStateResponse allBladespowerResponse;
            GetAllBladesStateResponse allBladesStateResponse;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetAllBladesDefaultPowerStateOnOffTest.");
            Thread.Sleep(50000);

            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();

            foreach (BladeStateResponse bladeDefaultPowerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeDefaultPowerStateResponse.bladeState != PowerState.OFF)
                    {
                        failureMessage = "\n!!!GetAllBladesDefaultPowerState Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bladeDefaultPowerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", bladeDefaultPowerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            Thread.Sleep(100000);

            // Make sure power is on
            allBladespowerResponse = this.Channel.GetAllPowerState();
            bladeIndex = 1;

            foreach (PowerStateResponse powerStateResponse in allBladespowerResponse.powerStateResponseCollection)
            {
                if (powerStateResponse.powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!GetAllPowerState Operation failed.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeIndex++;
            }

            // Make sure Blades are OFF
            allBladesStateResponse = this.Channel.GetAllBladesState();
            bladeIndex = 1;

            foreach (BladeStateResponse powerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (powerStateResponse.bladeState != PowerState.OFF)
                    {
                        failureMessage = "\n!!!GetAllBladesState Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && powerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", powerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            //Make sure the default power state is still set to OFF after restart.
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            bladeIndex = 1;
            foreach (BladeStateResponse bladeDefaultPowerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeDefaultPowerStateResponse.bladeState != PowerState.OFF)
                    {
                        failureMessage = "\n!!!GetAllBladesDefaultPowerState 2 Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bladeDefaultPowerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", bladeDefaultPowerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }


            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            bladeIndex = 1;
            foreach (BladeStateResponse bladeDefaultPowerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeDefaultPowerStateResponse.bladeState != PowerState.ON)
                    {
                        failureMessage = "\n!!!SetAllBladesDefaultPowerStateOn Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bladeDefaultPowerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", bladeDefaultPowerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            Thread.Sleep(100000);

            // Make sure power is on
            allBladespowerResponse = this.Channel.GetAllPowerState();
            bladeIndex = 1;

            foreach (PowerStateResponse powerStateResponse in allBladespowerResponse.powerStateResponseCollection)
            {
                if (powerStateResponse.powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!GetAllPowerState Operation failed.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                bladeIndex++;
            }

            // Make sure Blades are ON
            allBladesStateResponse = this.Channel.GetAllBladesState();
            //rest bladeIndex
            bladeIndex = 1;

            foreach (BladeStateResponse powerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (powerStateResponse.bladeState != PowerState.ON)
                    {
                        failureMessage = "\n!!!GetAllBladesState Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && powerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", powerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            //reset bladeIndex
            bladeIndex = 1;
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();

            foreach (BladeStateResponse bladeDefaultPowerStateResponse in allBladesStateResponse.bladeStateResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeDefaultPowerStateResponse.bladeState != PowerState.ON)
                    {
                        failureMessage = "\n!!!GetAllBladesDefaultPowerState Operation failed.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (this.JbodLocations.Contains(bladeIndex) && bladeDefaultPowerStateResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = string.Format("\n!!!Running against a JBOD returned{0}", bladeDefaultPowerStateResponse.completionCode);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetAllBladesDefaultPowerStateOnOffTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetAllBladesAttentionLEDOnOff
        /// </summary>
        public TestsResultResponse SetAllBladesAttentionLedOnOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            AllBladesResponse allBladesLedResponses;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetAllBladesAttentionLEDOnOffTest.");
            //Power all blades On in case they are off
            this.Channel.SetAllPowerOn();
            Thread.Sleep(50000);

            allBladesLedResponses = this.Channel.SetAllBladesAttentionLEDOff();
            foreach (BladeResponse bladeLedResponse in allBladesLedResponses.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeLedResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!SetAllBladesAttentionLEDOff Failed for blade# {0}", bladeIndex.ToString());
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (bladeLedResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            //reset bladeIndex
            bladeIndex = 1;
            allBladesLedResponses = this.Channel.SetAllBladesAttentionLEDOn();
            foreach (BladeResponse bladeLedResponse in allBladesLedResponses.bladeResponseCollection)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    if (bladeLedResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!SetAllBladesAttentionLEDOn Failed for blade# {0}", bladeIndex.ToString());
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                else
                {
                    if (bladeLedResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage = "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
                bladeIndex++;
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetAllBladesAttentionLEDOnOffTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for Set and Get BladeLEDOn
        /// </summary>
        public TestsResultResponse BladeLedTest()
        {
            BladeResponse bladeLedResponse;
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of BladeLEDTest");
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server machine to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //Power blade On in case it is off
            this.Channel.SetPowerOn(randomBlade);
            Thread.Sleep(50000);

            bladeLedResponse = this.Channel.SetBladeAttentionLEDOn(randomBlade);
            if (bladeLedResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Turn On again
            bladeLedResponse = this.Channel.SetBladeAttentionLEDOn(randomBlade);

            if (bladeLedResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            System.Threading.Thread.Sleep(5000);

            // Turn off
            bladeLedResponse = this.Channel.SetBladeAttentionLEDOff(randomBlade);

            if (bladeLedResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to set the LED OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //turn off again
            bladeLedResponse = this.Channel.SetBladeAttentionLEDOff(randomBlade);

            if (bladeLedResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to set the LED OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //turn on from Off
            bladeLedResponse = this.Channel.SetBladeAttentionLEDOn(randomBlade);

            if (bladeLedResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of BladeLEDTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for SetChassisLEDOnOff
        /// </summary>
        public TestsResultResponse SetChassisLedOnOffTest()
        {
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetChassisLEDOnOffTest.");
            //Turn ON
            ChassisResponse chassisResponse = this.Channel.SetChassisAttentionLEDOn();

            if (chassisResponse.completionCode != CompletionCode.Success || this.Channel.GetChassisAttentionLEDStatus().ledState != LedState.ON)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Turn On again
            chassisResponse = this.Channel.SetChassisAttentionLEDOn();

            if (chassisResponse.completionCode != CompletionCode.Success || this.Channel.GetChassisAttentionLEDStatus().ledState != LedState.ON)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            // Turn off
            chassisResponse = this.Channel.SetChassisAttentionLEDOff();

            if (chassisResponse.completionCode != CompletionCode.Success || this.Channel.GetChassisAttentionLEDStatus().ledState != LedState.OFF)
            {
                failureMessage = "\n!!!Failed to set the LED OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //turn off again
            chassisResponse = this.Channel.SetChassisAttentionLEDOff();

            if (chassisResponse.completionCode != CompletionCode.Success || this.Channel.GetChassisAttentionLEDStatus().ledState != LedState.OFF)
            {
                failureMessage = "\n!!!Failed to set the LED OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //turn on from Off
            chassisResponse = this.Channel.SetChassisAttentionLEDOn();

            if (chassisResponse.completionCode != CompletionCode.Success || this.Channel.GetChassisAttentionLEDStatus().ledState != LedState.ON)
            {
                failureMessage = "\n!!!Failed to set the LED ON.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetChassisLEDOnOffTest.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for poweroff
        /// </summary>
        public TestsResultResponse SetPowerOnOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            string powerOffBlades;
            string powerOnBlades;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting SetPowerOnOffTest");
            //Test power OFF from unknown state of blades.
            powerOffBlades = this.HelpSetAllPowerOffTest();
            if (powerOffBlades.Equals("fail"))
            {
                failureMessage = "\n!!!Power off test failed when Powering off from unknown state of blades.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Test power OFF from when all blades are off
            powerOffBlades = this.HelpSetAllPowerOffTest();
            if (powerOffBlades.Equals("fail"))
            {
                failureMessage = "\n!!!Power off test failed when Powering off blades that are already in Off state.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Power on all blades.
            powerOnBlades = this.HelpSetAllPowerOnTest();
            if (powerOnBlades.Equals("fail"))
            {
                failureMessage = "\n!!!Failed to bring blades to ON state from OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //Power on all blades from ON state.
            powerOnBlades = this.HelpSetAllPowerOnTest();
            if (powerOnBlades.Equals("fail"))
            {
                failureMessage = "\n!!!Failed to bring blades to ON state from OFF.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            powerOffBlades = this.HelpSetAllPowerOffTest();
            if (powerOffBlades.Equals("fail"))
            {
                failureMessage = "Power off test failed when Powering off blades that are already in Off state.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //Power on all blades to test turning them off from On state in reverse order.
            powerOnBlades = this.HelpSetAllPowerOnTest();
            if (powerOnBlades.Equals("fail"))
            {
                failureMessage = "\n!!!Failed to bring blades ON to test Turning them off.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            //PowerOFF all blades in reverse order one by one and verify along the way.
            for (int bladeId = maxNumBlades; bladeId >= 1; bladeId--)
            {
                this.Channel.SetPowerOff(bladeId);
                Thread.Sleep(1000);//wait for a sec
                if (this.Channel.GetPowerState(bladeId).powerState != PowerState.OFF)
                {
                    failureMessage = "\n!!!Failed to powerOff blade.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                for (int index = 1; index < bladeId; index++)
                {
                    if (this.Channel.GetPowerState(index).powerState != PowerState.ON)
                    {
                        failureMessage = "\n!!!Blade should still be off.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
            }

            //powerOn in reverse order one by one and verify along the way.
            //PowerOFF all blades in reverse order one by one and verify along the way.
            for (int bladeId = maxNumBlades; bladeId >= 1; bladeId--)
            {
                this.Channel.SetPowerOn(bladeId);
            }
            Thread.Sleep(50000);

            //Make sure that all blades are powered ON
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                if (this.Channel.GetPowerState(bladeId).powerState != PowerState.ON)
                {
                    failureMessage = "\n!!!Failed in verifying power on state after power on in reverse.";
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetPowerOff.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        /// A test for setBladeOff
        /// </summary>
        public TestsResultResponse SetBladeOnOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetBladOnOffTest.");
            //Make sure there is power running to the blades
            string powerOn;
            string bladeOn;
            string bladeOff;

            powerOn = this.HelpSetAllPowerOnTest();
            if (powerOn.Equals("fail"))
            {
                failureMessage = "\n!!!Powering blades on failed.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Test blades OFF from unknown state of blades.
            bladeOff = this.HelpSetBladeOffTest();
            if (bladeOff.Equals("fail"))
            {
                failureMessage = "\n!!!Blade off test failed when Powering off from ON state of blades.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }
            
            //Test Blade OFF when all blades are in off state
            bladeOff = this.HelpSetBladeOffTest();
            if (bladeOff.Equals("fail"))
            {
                failureMessage = "\n!!!Blade off test failed when Powering off blades that are already in Off state.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Blade on all blades from Off state.
            bladeOn = this.HelpSetBladeOnTest();
            if (bladeOn.Equals("fail"))
            {
                failureMessage = "\n!!!Failed to bring blades on to test Turning them off.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Blade ON all blades from ON state.
            bladeOn = this.HelpSetBladeOnTest();
            if (bladeOn.Equals("fail"))
            {
                failureMessage = "\n!!!Failed to bring blades ON from ON state.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            bladeOff = this.HelpSetBladeOffTest();
            if (bladeOff.Equals("fail"))
            {
                failureMessage = "Power off test failed when Powering off blades that are already in Off state.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            //Test powerOn in reverse.
            for (int bladeId = maxNumBlades; bladeId >= 1; bladeId--)
            {
                this.Channel.SetBladeOn(bladeId);
            }
            Thread.Sleep(10000);
            //Make sure that all blades are still ON
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                if (!this.JbodLocations.Contains(bladeId) && !this.EmptySlots.Contains(bladeId))
                {
                    if (this.Channel.GetBladeState(bladeId).bladeState != PowerState.ON)
                    {
                        failureMessage = "\n!!!Failed during PowerON in reverse.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
            }

            //PowerOFF all blades in reverse order.
            for (int bladeId = maxNumBlades; bladeId >= 1; bladeId--)
            {
                this.Channel.SetBladeOff(bladeId);
                Thread.Sleep(1000);
            }

            Thread.Sleep(10000);

            //Make sure that all blades are still OFF
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                if (!this.JbodLocations.Contains(bladeId) && !this.EmptySlots.Contains(bladeId))
                {
                    if (this.Channel.GetBladeState(bladeId).bladeState != PowerState.OFF)
                    {
                        failureMessage = "\n!!!Failed during PowerOff in reverse.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
            }
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetPowerOff tests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///A test for SetNextBoot
        ///</summary>
        public TestsResultResponse SetNextBootTest()
        {
            int randomBlade = 0;
            bool isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            Console.WriteLine("\n!!!!!!!!! Starting execution of SetNextBootTest.");
            //Make sure blade is reacheable
            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                randomBlade = new Random().Next(1, (byte)this.ChassisPopulation);
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.SetPowerOn(randomBlade);
            System.Threading.Thread.Sleep(50000);

            BootResponse bBootType;
            List<BladeBootType> bladeBootTypes = new List<BladeBootType>();
            int index = 1;
            foreach (BladeBootType testedBootType in Enum.GetValues(typeof(BladeBootType)))
            {
                //Doing the same setting twice to make sure we are handling this properly.
                if (testedBootType.ToString() != BladeBootType.Unknown.ToString())
                {
                    //set to persistent.
                    bBootType = this.Channel.SetNextBoot(randomBlade, testedBootType, false, false, 0);
                    if (bBootType.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!Failed to set non persistant boot type to: {0}", testedBootType.ToString());
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                    
                    bBootType = this.Channel.GetNextBoot(randomBlade);
                    if (testedBootType.ToString() != bBootType.nextBoot.ToString())
                    {
                        failureMessage = "\n!!!The Non persistent boot type did not match what it was set to.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }

                    //set to non persistent.
                    bBootType = this.Channel.SetNextBoot(randomBlade, testedBootType, false, true, 1);
                    if (bBootType.completionCode != CompletionCode.Success)
                    {
                        failureMessage = string.Format("\n!!!Failed to set Persistent boot type to: {0}", testedBootType.ToString());
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }

                    //Make sure if no restart happens it keeps its value.
                    bBootType = this.Channel.GetNextBoot(randomBlade);
                    if (testedBootType.ToString() != bBootType.nextBoot.ToString())
                    {
                        failureMessage = "\n!!!The boot type did not match what it was set to.";
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                    //Make sure it loses its value after restart
                    this.Channel.SetBladeActivePowerCycle(randomBlade, 0);
                    System.Threading.Thread.Sleep(60000);
                    bBootType = this.Channel.GetNextBoot(randomBlade);
                    if (testedBootType.ToString() != BladeBootType.ForcePxe.ToString() && testedBootType.ToString() != bBootType.nextBoot.ToString())
                    {
                        failureMessage = string.Format("\n!!!The boot type did not match what it was set to before power cycle.{0} vs. {1} this is round# {2}", testedBootType.ToString(), bBootType.nextBoot.ToString(), index);
                        Console.WriteLine(failureMessage);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }
            }
            this.Channel.SetNextBoot(randomBlade, BladeBootType.NoOverride, false, true, 0);
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully finished execution of SetNextBoot tests.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///A functional test For StartBladeSerialSession
        ///</summary>
        public TestsResultResponse BladeSerialSessionFunctional()
        {
            string failureMessage = string.Empty;
            StartSerialResponse serialResponse = null;
            byte[] serialData = new byte[1000];
            SerialDataResponse receiveDataResponse;
            ChassisResponse sendDataResponse;
            List<string> commands = new List<string>();
            string responseContent = "Channel management commands";

            bool isServer = false;
            int bladeIndex = 1;

            Console.WriteLine("\n!!!!!!!!! Started execution of StartBladeSerialSession");

            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.SetPowerOn(bladeIndex);
            System.Threading.Thread.Sleep(50);
            commands.Add("?\r\n");

            byte[] payload = Encoding.ASCII.GetBytes(commands[0]);

            serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, this.serialTimeoutInSecs);
            sendDataResponse = this.Channel.SendBladeSerialData(bladeIndex, serialResponse.serialSessionToken, payload);

            if (sendDataResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = string.Format("\n!!!!!!!!! Failed to send blade serial data. Failure is: {0}", sendDataResponse.completionCode);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            string response = "";
            receiveDataResponse = this.Channel.ReceiveBladeSerialData(bladeIndex, serialResponse.serialSessionToken);
            int index = 1;
            while (string.IsNullOrEmpty(response) && index < 5)
            {
                System.Threading.Thread.Sleep(10000);
                sendDataResponse = this.Channel.SendBladeSerialData(bladeIndex, serialResponse.serialSessionToken, payload);
                receiveDataResponse = this.Channel.ReceiveBladeSerialData(bladeIndex, serialResponse.serialSessionToken);
                response += System.Text.Encoding.Default.GetString(receiveDataResponse.data);
                index++;
            }

            if (!response.Contains(responseContent))
            {
                failureMessage = string.Format("\n!!!!!!!!! Failed verifying the content of the response. Containt is : {0} CompletionCode is: {1}", response, receiveDataResponse.completionCode);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n!!!!!!!!! Finished execution of StartBladeSerialSession.");
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully verified Service version.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///A functional test For StartBladeSerialSession against all blades. This requires a full chassis.
        ///</summary>
        public TestsResultResponse AllBladesSerialSessionFunctional()
        {
            string failureMessage = string.Empty;
            ChassisResponse chassisResponse = null;
            StartSerialResponse serialResponse = null;
            byte[] serialData = new byte[1000];
            SerialDataResponse receiveDataResponse;
            ChassisResponse sendDataResponse;
            List<string> commands = new List<string>();
            string responseContent = "Channel management commands";

            this.Channel.SetAllPowerOn();
            System.Threading.Thread.Sleep(40);

            for (int bladeIndex = 1; bladeIndex <= this.ChassisPopulation; bladeIndex++)
            {
                // Kill any existing serial session first
                chassisResponse = this.Channel.StopBladeSerialSession(bladeIndex, null, true);
                if (bladeIndex == 1 && chassisResponse.completionCode != CompletionCode.NoActiveSerialSession)
                {
                    failureMessage = string.Format("\n!!!!!!!!! There are no active serial session with Blade 1. We received: {0}", chassisResponse.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                else
                {
                    if (chassisResponse.completionCode != CompletionCode.NoActiveSerialSession)
                    {
                        failureMessage = string.Format("\n!!!!!!!!! Trying to stop active serial session failed. We received: {0}", chassisResponse.completionCode);
                        return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                    }
                }

                commands.Add("?\r\n");

                byte[] payload = Encoding.ASCII.GetBytes(commands[0]);

                serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, this.serialTimeoutInSecs);
                sendDataResponse = this.Channel.SendBladeSerialData(bladeIndex, serialResponse.serialSessionToken, payload);

                if (sendDataResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("\n!!!!!!!!! Failed to send blade serial data. Failure is: {0}", sendDataResponse.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                
                string response = "";
                receiveDataResponse = this.Channel.ReceiveBladeSerialData(bladeIndex, serialResponse.serialSessionToken);
                int index = 1;
                while (string.IsNullOrEmpty(response) && index < 5)
                {
                    System.Threading.Thread.Sleep(10000);
                    sendDataResponse = this.Channel.SendBladeSerialData(bladeIndex, serialResponse.serialSessionToken, payload);
                    receiveDataResponse = this.Channel.ReceiveBladeSerialData(bladeIndex, serialResponse.serialSessionToken);
                    response += System.Text.Encoding.Default.GetString(receiveDataResponse.data);
                    index++;
                }

                if (!response.Contains(responseContent))
                {
                    failureMessage = string.Format("\n!!!!!!!!! Failed verifying the content of the response. Containt is : {0} CompletionCode is: {1}", response, receiveDataResponse.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }
            Console.WriteLine("\n!!!!!!!!! Finished execution of StartBladeSerialSession.");
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully verified Service version.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///A functional and stress test For StartBladeSerialSession
        ///</summary>
        public TestsResultResponse StressBladeSerialSession()
        {
            string failureMessage = string.Empty;
            ChassisResponse stopSerResponse = null;
            StartSerialResponse serialResponse = null;

            BladeResponse setLedOnAction;
            BladeResponse setLedOffAction;

            bool isServer = false;
            int bladeIndex = 1;

            Console.WriteLine("\n!!!!!!!!! Started execution of StartBladeSerialSession");

            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= this.ChassisPopulation)
            {
                if (!this.JbodLocations.Contains(bladeIndex) && !this.EmptySlots.Contains(bladeIndex))
                {
                    isServer = true;
                }
                else
                {
                    bladeIndex++;
                }
            }

            if (bladeIndex > this.ChassisPopulation)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            this.Channel.SetPowerOn(bladeIndex);
            System.Threading.Thread.Sleep(40);
            // Kill any existing serial session first

            serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, this.serialTimeoutInSecs);
            //Run Open and close stress
            for (int i = 1; i <= 500; i++)
            {
                // Start new session
                stopSerResponse = this.Channel.StopBladeSerialSession(bladeIndex, null, true);
                serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, 0);

                failureMessage = string.Format("Start Stop no wait: I was running iteration number {0} against blade # {1}", i, bladeIndex);
                if (stopSerResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to STOP serial session during open close serial stress.Stop1 resposne is:{0}", stopSerResponse.completionCode);
                    //return new TestsResultResponse(executionResult.Failed, failureMessage);
                }

                if (serialResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to open serial session during open close serial stress. Completion Code: {0}", serialResponse.completionCode);
                    //return new TestsResultResponse(executionResult.Failed, failureMessage);
                }
            }

            // Run open and close stress with wait for timeout
            stopSerResponse = this.Channel.StopBladeSerialSession(bladeIndex, null, true);
            for (int i = 1; i <= 20; i++)
            {
                // Start new session
                serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, this.serialTimeoutInSecs);
                System.Threading.Thread.Sleep(360000);
                stopSerResponse = this.Channel.StopBladeSerialSession(bladeIndex, null, true);

                failureMessage = string.Format("Start, wait and stop: I was running iteration number {0} against blade # {1}", i, bladeIndex);

                if (serialResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to open serial session during open close serial stress with wait time. failure is: {0}", serialResponse.completionCode);
                    //return new TestsResultResponse(executionResult.Failed, failureMessage);
                }

                if (stopSerResponse.completionCode != CompletionCode.NoActiveSerialSession)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed: Stop serial session should have failed with NoActiveSerialSession failure was: {0}", stopSerResponse.completionCode);
                    //return new TestsResultResponse(executionResult.Failed, failureMessage);
                }
            }

            for (int loop = 1; loop <= 100; loop++)
            {
                //Run a blade command
                setLedOnAction = this.Channel.SetBladeAttentionLEDOn(bladeIndex);

                // Start new session
                serialResponse = this.Channel.StartBladeSerialSession(bladeIndex, this.serialTimeoutInSecs);
                stopSerResponse = this.Channel.StopBladeSerialSession(bladeIndex, null, true);
                //Run a blade Command
                setLedOffAction = this.Channel.SetBladeAttentionLEDOff(bladeIndex);

                failureMessage = string.Format("Mux switching: I was running iteration number {0} against blade # {1}", loop, bladeIndex);
                if (serialResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to open serial session with completion code: {0}", serialResponse.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                if (stopSerResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed: Stop serial session failed during serial Mux switch with: {0}", stopSerResponse.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                if (setLedOnAction.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to SetBladeLEDOn before opening a session with completion code: {0}", setLedOnAction.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }

                if (setLedOffAction.completionCode != CompletionCode.Success)
                {
                    failureMessage += string.Format("\n!!!!!!!!! failed to SetBladeLEDOff after opening a session with completion code: {0}", setLedOffAction.completionCode);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
            }

            Console.WriteLine("\n!!!!!!!!! Started execution of StartBladeSerialSession.");
            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully verified Service version.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///A test for Get CM service Version
        ///</summary>
        public TestsResultResponse GetServiceVersionTest()
        {
            string failureMessage = string.Empty;
            ServiceVersionResponse svcVersion = this.Channel.GetServiceVersion();

            if (!svcVersion.serviceVersion.Equals(this.serviceVersion))
            {
                failureMessage = string.Format("\n!!!Service Version did not much the expected Version. We got back: {0}", svcVersion.ToString());
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully verified Service version.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }

        /// <summary>
        ///Helper for test for powerOn
        ///takes the status of the blades before calling the method. Status can be ON, OFF or Unknown.
        ///returns pass or fail string.
        /// </summary>
        public string HelpSetAllPowerOnTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            PowerStateResponse bladeState = null;

            //PowerOn all blades one by one and verify along the way.

            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                this.Channel.SetPowerOn(bladeId);
                Thread.Sleep(1000);//wait for a sec
            }

            Thread.Sleep(50000);

            //Make sure that all blades are powered On
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                bladeState = this.Channel.GetPowerState(bladeId);
                if (bladeState.powerState != PowerState.ON)
                {
                    return "fail";
                }
            }

            return "Pass";
        }

        /// <summary>
        ///Helper for test for poweroff
        ///takes the status of the blades before calling the method. Status can be ON, OFF or Unknown.
        ///returns pass or fail string.
        /// </summary>
        public string HelpSetAllPowerOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            PowerStateResponse bladeState = null;

            //Powerff all blades one by one and verify along the way.

            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                this.Channel.SetPowerOff(bladeId);
                Thread.Sleep(100);//wait for a sec
            }

            Thread.Sleep(10000);

            //Make sure that all blades are powered Off
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                bladeState = this.Channel.GetPowerState(bladeId);
                if (bladeState.powerState != PowerState.OFF)
                {
                    return "fail";
                }
            }

            return "Pass";
        }

        /// <summary>
        ///Helper for test for  blade powerOn
        ///returns pass or fail string.
        /// </summary>
        public string HelpSetBladeOnTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;
            BladeStateResponse bladeState = null;

            //PowerOFF all blades one by one and verify along the way.

            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                this.Channel.SetBladeOn(bladeId);
            }

            Thread.Sleep(10000);

            //Make sure that all blades are still OFF
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                bladeState = this.Channel.GetBladeState(bladeId);
                if (!this.JbodLocations.Contains(bladeId) && !this.EmptySlots.Contains(bladeId) && bladeState.bladeState != PowerState.ON)
                {
                    return "fail";
                }
            }
            return "Pass";
        }

        /// <summary>
        ///Helper for test for poweroff
        ///returns pass or fail string.
        /// </summary>
        public string HelpSetBladeOffTest()
        {
            int maxNumBlades = (byte)this.ChassisPopulation;

            //PowerOFF all blades one by one and verify along the way.
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                this.Channel.SetBladeOff(bladeId);
            }

            Thread.Sleep(10000);

            //Make sure that all blades are still OFF
            for (int bladeId = 1; bladeId <= maxNumBlades; bladeId++)
            {
                BladeStateResponse getBladeState = this.Channel.GetBladeState(bladeId);
                if (!this.JbodLocations.Contains(bladeId) && !this.EmptySlots.Contains(bladeId) && getBladeState.bladeState != PowerState.OFF)
                {
                    return "fail";
                }
            }
            return "Pass";
        }

        internal TestsResultResponse BladePowercycle(int bladeId, string bladeIPAddress, int durationInMinutes)
        {
            Stopwatch stopwatch = new Stopwatch();
            int timeoutTimerInMilliSecs = durationInMinutes * 60000;
            int pingWaitTime = 360000; //in Milliseconds.
            Ping bladePing = new Ping();
            PingReply bladePingReply;
            string failureMessage;
            int cyclingCounter = 0;

            this.Channel.SetPowerOn(bladeId);
            this.Channel.SetBladeDefaultPowerStateOn(bladeId);

            bladePingReply = bladePing.Send(bladeIPAddress, pingWaitTime);

            if (bladePingReply.Status != IPStatus.Success)
            {
                Console.WriteLine("\n++++++++++++++++++++++++++++++++");
                failureMessage = "\n!!!!!!!!! Failed to start the cycling test. Blade is not responding.";
                Console.WriteLine(failureMessage);
                return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
            }

            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < timeoutTimerInMilliSecs)
            {
                this.Channel.SetPowerOff(bladeId);
                Thread.Sleep(1000);
                this.Channel.SetPowerOn(bladeId);
                bladePingReply = bladePing.Send(bladeIPAddress, pingWaitTime); //wait five minutes for blade to respond.
                bladePingReply = bladePing.Send(bladeIPAddress, 6000);
                if (bladePingReply.Status != IPStatus.Success)
                {
                    Console.WriteLine("\n++++++++++++++++++++++++++++++++");
                    failureMessage = string.Format("\n!!!!!!!!! Failed during cycling after {0} cycles", cyclingCounter.ToString());
                    Console.WriteLine(failureMessage);
                    return new TestsResultResponse(ExecutionResult.Failed, failureMessage);
                }
                cyclingCounter++;
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "\n!!!!!!!!! Successfully verified cycling stress test.";
            Console.WriteLine(failureMessage);
            return new TestsResultResponse(ExecutionResult.Passed, failureMessage);
        }
    }
}
