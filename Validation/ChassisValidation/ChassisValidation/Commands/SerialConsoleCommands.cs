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
    using Microsoft.GFS.WCS.Contracts;
    using System.Text;
    using System.Threading;
    using System.Configuration;

    internal class SerialConsoleCommands : CommandBase
    {
        internal SerialConsoleCommands(IChassisManager channel)
            : base(channel)
        {
        }

        internal SerialConsoleCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts)
            : base(channel, testChannelContexts)
        {
        }

        /// <summary>
        /// Test Command: StartBladeSerialSession, StopBladeSerialSession. 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// The serial session can be successfully started on a server blade;
        /// The serial session can be successfully stopped provided the session token.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse StartStopBladeSerialSessionTest()
        {
            CmTestLog.Start();

            TestResponse response = new TestResponse();
            TestResponse verifySerialSessionResponse = null;
            StartSerialResponse startSerialSessionResponse = null;
            ChassisResponse serialSessionStopResponse = null;
            string sessionToken = string.Empty;
            BladeInfoResponse bladeInfoResponse = null;

            // get all server blade locations
            this.ServerLocations = this.GetServerLocations();
            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            int emptyBladeId = -1;
            // pick up a random server blade
            int bladeId = ServerLocations.RandomOrDefault();
            CmTestLog.Info(string.Format("Picked a random server blade# {0} for test", bladeId));

            // pick up a random Empty blade
            if (EmptyLocations.Length > 0)
            {
                emptyBladeId = EmptyLocations.RandomOrDefault();
            CmTestLog.Info(string.Format("Picked a random empty blade# {0} for test", emptyBladeId));
            }
            else
            {
                CmTestLog.Warning("There are no empty blades to use for testing");
            }
            // kill any existing serial session first
            CmTestLog.Info(string.Format("Kill all existing serial session for Blade# {0}", bladeId));
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, null, true);

            if (!(CompletionCode.NoActiveSerialSession == serialSessionStopResponse.completionCode || CompletionCode.Success == serialSessionStopResponse.completionCode))
            {
                response.Result &= false;
                response.ResultDescription.Append(serialSessionStopResponse.statusDescription);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }

            // [Test Case: 1826]StartBladeSerialSession:  Verify that the command does NOT starts a serial session with a blade index that is OutOfRange
            CmTestLog.Info(string.Format("Trying to start a serial session for invalidBlade# {0}", CmConstants.InvalidBladeId));
            startSerialSessionResponse = this.Channel.StartBladeSerialSession(CmConstants.InvalidBladeId, CmConstants.SerialTimeoutSeconds);

            // Verify ParameterOutOfRange CompletionCode & null serial session token for invalidBlade
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, startSerialSessionResponse.completionCode, "Received ParameterOutOfRange completion code for Blade# " + CmConstants.InvalidBladeId))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to Receive ParameterOutOfRange completion code for Blade# " + CmConstants.InvalidBladeId);
            }
            if (!ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(startSerialSessionResponse.serialSessionToken), "Received null serial session token"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive null serial session token for invalidBlade# " + CmConstants.InvalidBladeId);
            }

            if (emptyBladeId != -1)
            {
            // [Test Case: 1827]StartBladeSerialSession:  Verify that the command does NOT start a serial session with empty slot index
            CmTestLog.Info(string.Format("Trying to start a serial session for empty blade# {0}", emptyBladeId));
            startSerialSessionResponse = this.Channel.StartBladeSerialSession(emptyBladeId, CmConstants.SerialTimeoutSeconds);

            // Verify Failure CompletionCode & null serial session token for emptyBlade
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, startSerialSessionResponse.completionCode, "Received Faiure completion code for EmptyBlade# " + emptyBladeId))
            {
                    response.Result &= false;
                    response.ResultDescription.Append("\nFailed to Receive Faiure completion code for EmptyBlade# " + emptyBladeId);
            }
            if (!ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(startSerialSessionResponse.serialSessionToken), "Received null serial session token"))
            {
                    response.Result &= false;
                    response.ResultDescription.Append("\nFailed to receive null serial session token for emptyBlade# " + emptyBladeId);
                }
            }
            // start blade serial session 
            CmTestLog.Info(string.Format("Trying to start a serial session to Blade# {0}", bladeId));
            
            verifySerialSessionResponse = this.VerifyStartBladeSerialSession(response, bladeId, ref startSerialSessionResponse, out sessionToken);
            response.Result &= verifySerialSessionResponse.Result;
            response.ResultDescription.Append(verifySerialSessionResponse.ResultDescription);

            // [Test Case: 1829]StopBladeSerialSession: Verify that the command stops a given serial session when presented BladeIndex and SessionToken
            CmTestLog.Info(string.Format("Trying to stop the serial session to Blade# {0}", bladeId));
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, sessionToken);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, serialSessionStopResponse.completionCode, "Serial session stopped"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to stop serial session, Completion Code returned: " + startSerialSessionResponse.completionCode);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }

            // [Test Case: 1830] StopBladeSerialSession:  Verify that the command indicates no active session when stopping a non-existent session
            CmTestLog.Info(string.Format("Trying to stop the serial session with non-existent session"));
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, sessionToken);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.NoActiveSerialSession, serialSessionStopResponse.completionCode, "Received NoActiveSerialSession Completion code"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive NoActiveSerialSession completion code for a non-existent session token, Completion Code Returned: " + serialSessionStopResponse.completionCode);
            }

            // [Test Case: 2063] StartBladeSerialSession:  Verify session is automatically killed when default session timeout (300 seconds) elapses
            CmTestLog.Info(string.Format("Verifying session is automatically killed when default session timeout (300 seconds) elapses to Blade# {0}", bladeId));

            verifySerialSessionResponse = this.VerifyStartBladeSerialSession(response, bladeId, ref startSerialSessionResponse, out sessionToken);
            response.Result &= verifySerialSessionResponse.Result;
            response.ResultDescription.Append(verifySerialSessionResponse.ResultDescription);

            CmTestLog.Info("Trying to start blade serial session when session is already active");
            startSerialSessionResponse = this.Channel.StartBladeSerialSession(bladeId, CmConstants.SerialTimeoutSeconds);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, startSerialSessionResponse.completionCode, "Received SerialSessionActive completion code"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive SerialSessionActive completion code, Completion code returned: " + serialSessionStopResponse.completionCode);
            }

            CmTestLog.Info("Sleeping six minutes waiting for Serial session to timeout");
            Thread.Sleep(TimeSpan.FromMinutes(6));
            
            CmTestLog.Info("Verifying Serial session timeout out");
            verifySerialSessionResponse = this.VerifyStartBladeSerialSession(response, bladeId, ref startSerialSessionResponse, out sessionToken);
            response.Result &= verifySerialSessionResponse.Result;
            response.ResultDescription.Append(verifySerialSessionResponse.ResultDescription);

            if (response.Result == true)
            {
                CmTestLog.Success("Serial session started successfully after default session timeout");
            }
            else
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to start new serial session after a previous one expired, Completion Code Returned: " + serialSessionStopResponse.completionCode);
            }

            // [Test Case: 2065] StartBladeSerialSession: Any user IPMI request 
            CmTestLog.Info("Trying to GetBladeInfo when serial session is active for bladeId# " + bladeId);
            bladeInfoResponse = this.Channel.GetBladeInfo(bladeId);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeInfoResponse.completionCode, "Successfully received blade information"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive Success completion code for getbladeInfo, Completion code returned: " + bladeInfoResponse.completionCode);
            }

            startSerialSessionResponse = this.Channel.StartBladeSerialSession(bladeId, CmConstants.SerialTimeoutSeconds);

            // [Test Case: 6180] StopBladeSerialSession: Verify missing Forcekill parameter requires valid sessionToken option
            CmTestLog.Info(string.Format("Trying to stop the serial session invalid session token and no force kill"));
            string invalidSessionToken = sessionToken + sessionToken;
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, invalidSessionToken);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, serialSessionStopResponse.completionCode, "Received SerialSessionActive Completion code"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive SerialSessionActive for invalid session token and no force kill, Completion code returned: " + serialSessionStopResponse.completionCode);
            }

            // [Test Case: 6181] StopBladeSerialSession: Verify false ForceKill parameter does NOT bypass sessionToken option
            CmTestLog.Info(string.Format("Trying to stop the serial session invalid session token and False force kill"));
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, invalidSessionToken, false);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, serialSessionStopResponse.completionCode, "Received SerialSessionActive Completion code"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to receive SerialSessionActive for invalid session token and false force kill, Completion code returned: " + serialSessionStopResponse.completionCode);
            }

            // [Test Case: 6182] StopBladeSerialSession: Verify true ForceKill parameter bypasses sessionToken option and closes serial session
            CmTestLog.Info(string.Format("Trying to stop the serial session invalid session token and true force kill"));
            serialSessionStopResponse = this.Channel.StopBladeSerialSession(bladeId, invalidSessionToken, true);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, serialSessionStopResponse.completionCode, "Serial session stopped"))
            {
                response.Result &= false;
                response.ResultDescription.Append("\nFailed to stop serial session for invalid session token and true force kill, Completion code returned: " + serialSessionStopResponse.completionCode);
            }

            // test ended
            CmTestLog.End(response.Result);
            return response;
        }

        /// <summary>
        ///A functional test For StartBladeSerialSession against all blades. This requires a full chassis.
        ///</summary>
        public TestResponse AllBladesSerialSessionFunctional()
        {
            CmTestLog.Start();
            CmTestLog.Info("Functional test for StartBladeSerialSession against 24 blades. This requires a full chassis.");
            TestResponse response = new TestResponse();

            StartSerialResponse startSerialResponse = null;

            CmTestLog.Info("Trying to power on all blades");
            if (!SetPowerState(PowerState.ON))
            {
                response.Result = false;
                response.ResultDescription.Append("Failed to power on all blades");
                CmTestLog.End(response.Result);
                return response;
            }

            for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
            {
                // kill any existing serial session & Start blade serial session 
                startSerialResponse = StartBladeSerialSessionAndKillExistingSession(ref response, startSerialResponse, bladeId);

                byte[] payload = Encoding.ASCII.GetBytes(CmConstants.SerialCommand);

                // Send blade serial data & Verify Completion Code
                ChassisResponse sendDataResponse = this.Channel.SendBladeSerialData(bladeId, startSerialResponse.serialSessionToken, payload);

                if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, sendDataResponse.completionCode, "Send blade serial data"))
                {
                    response.Result = false;
                    response.ResultDescription.Append("\nFailed to send blade serial data. Failure is: " + sendDataResponse.completionCode);
                }

                // ReceiveBladeSerialData & Verify Completion Code and data content 
                SerialDataResponse receiveDataResponse = this.Channel.ReceiveBladeSerialData(bladeId, startSerialResponse.serialSessionToken);

                if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, receiveDataResponse.completionCode, "Received blade serial data"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nFailed to receive blade serial data, completion code returned: " + receiveDataResponse.completionCode);
                    CmTestLog.Failure(response.ResultDescription.ToString());
                    return response;
                }

                // Converting response byte[] array data to string
                string str = System.Text.Encoding.Default.GetString(receiveDataResponse.data);

                if (!str.Contains(CmConstants.ResponseContent))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nFailed verifying the content of the response. \nActual Content is : " + str + " \nExpected Content: " + CmConstants.ResponseContent);
                }
            }

            CmTestLog.Info("\n!!!!!!!!! Finished execution of StartBladeSerialSession.");

            // end of the test
            CmTestLog.End(response.Result);
            return response;
        }

        private StartSerialResponse StartBladeSerialSessionAndKillExistingSession(ref TestResponse response, StartSerialResponse startSerialResponse, int bladeId)
        {
            // kill any existing serial session first
            CmTestLog.Info(string.Format("Kill all existing serial session for Blade# {0}", bladeId));
            ChassisResponse killSession = this.Channel.StopBladeSerialSession(bladeId, null, true);
            if (!(CompletionCode.NoActiveSerialSession == killSession.completionCode || CompletionCode.Success == killSession.completionCode))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to kill existing session for blade# " + bladeId);
            }

            // start blade serial session 
            CmTestLog.Info(string.Format("Trying to start a serial session for Blade# {0}", bladeId));
            string sessionToken;
            TestResponse testResponse = this.VerifyStartBladeSerialSession(response, bladeId, ref startSerialResponse, out sessionToken);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);
            return startSerialResponse;
        }

        /// <summary>
        /// Verifies StartBladeSerialSession, StopBladeSerialSession, SendBladeSerialData and ReceiveBladeSerialData by all users
        /// Bad Request for WcsUser and WcsOperator
        /// Success for WcsAdmin
        /// </summary>
        /// <returns>Returns true if all check points pass</returns>
        public TestResponse StartStopSendReceiveBladeSerialSessionByAllUsersTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            // get all server blade locations
            this.ServerLocations = this.GetServerLocations();
            // pick up a random server blade
            int bladeId = ServerLocations.RandomOrDefault();
            CmTestLog.Info(string.Format("Picked a random server blade# {0} for test", bladeId));

            VerifyResponeAndAddFailureDescription(this.StartBladeSerialSessionByAllUsers(bladeId), response);            
            VerifyResponeAndAddFailureDescription(this.StopBladeSerialSessionByAllUsers(bladeId), response);
            VerifyResponeAndAddFailureDescription(this.SendBladeSerialDataByAllUsers(bladeId),  response);
            VerifyResponeAndAddFailureDescription(this.ReceiveBladeSerialDataByAllUsers(bladeId), response); 

            CmTestLog.End(response.Result);
            return response;
        }

        private void VerifyResponeAndAddFailureDescription(TestResponse bladeResp, TestResponse response )
        {
            if (!bladeResp.Result)
            {
                response.ResultDescription.Append(bladeResp.ResultDescription);
                response.Result = false;
            }
        }
        // [Test Case: 2557] StartBladeSerialSession: Verify users from WcsCmAdmin are able to execute the command
        // [Test Case: 2558] StartBladeSerialSession: Verify users from WcsCmOperator group are NOT able to execute the command
        // [Test Case: 2559] StartBladeSerialSession: Verify users from WcsCmUser group are NOT able to execute the command
        private TestResponse StartBladeSerialSessionByAllUsers(int bladeId)
        {
           CmTestLog.Info("\n !!!!!!Verifying StartBladeSerialSession By All Users.!!!!!!");
            TestResponse response = new TestResponse();

            // Loop through different user types and start blade serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    StartSerialResponse startResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to StartBladeSerialSession with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    startResponse = this.TestChannelContext.StartBladeSerialSession(bladeId, CmConstants.SerialTimeoutSeconds);

                    TestResponse testResponse = this.VerifyStartBladeSerialSessionByUser(response, roleId, startResponse);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, " StartBladeSerialSession returned Bad Request for user " + roleId.ToString())) 
                        {
                            response.Result = false;
                            response.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        response.Result = false;
                        response.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(response.Result);
            return response;
        }

        // [Test Case: 2562] StopBladeSerialSession: Verify users from WcsCmAdmin are able to execute the command
        // [Test Case: 2560] StopBladeSerialSession: Verify users from WcsCmUser group are NOT able to execute the command
        // [Test Case: 2561] StopBladeSerialSession: Verify users from WcsCmOperator group are NOT able to execute the command
        private TestResponse StopBladeSerialSessionByAllUsers(int bladeId)
        {
            CmTestLog.Info("\n !!!!!!Verifying StopBladeSerialSession By All Users.!!!!!!");
            TestResponse response = new TestResponse();

            // Loop through different user types and start blade serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to StopBladeSerialSession with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    ChassisResponse stopResponse = this.TestChannelContext.StopBladeSerialSession(bladeId, null,true);

                    TestResponse testResponse = this.VerifyStopBladeSerialSessionByUser(response, roleId, stopResponse);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, " StopBladeSerialSession returned Bad Request for user " + roleId.ToString()))
                        {
                            response.Result = false;
                            response.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        response.Result = false;
                        response.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(response.Result);
            return response;
        }
        
        // [Test Case: 2564] SendBladeSerialData: Verify users from WcsCmOperator group are NOT able to execute the command
        // [Test Case: 2565] SendBladeSerialData: Verify users from WcsCmUser group are NOT able to execute the command        
        // [Test Case: 2563] SendBladeSerialData: Verify users from WcsCmAdmin are able to execute the command
        private TestResponse SendBladeSerialDataByAllUsers(int bladeId)
        {
            CmTestLog.Info("\n !!!!!!Verifying SendBladeSerialData By All Users.!!!!!!");
            TestResponse response = new TestResponse();

            StartSerialResponse startSerialResponse = null;
            startSerialResponse = StartBladeSerialSessionAndKillExistingSession(ref response, startSerialResponse, bladeId);

            byte[] payload = Encoding.ASCII.GetBytes(CmConstants.SerialCommand);

            // Loop through different user types and start blade serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SendBladeSerialData with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    ChassisResponse sendResponse = this.TestChannelContext.SendBladeSerialData(bladeId, startSerialResponse.serialSessionToken, payload);

                    TestResponse testResponse = this.VerifySendBladeSerialDataByUser(response, roleId, sendResponse);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, "SendBladeSerialData returned Bad Request for user" + roleId.ToString()))
                        {
                            response.Result = false;
                            response.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        response.Result = false;
                        response.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(response.Result);
            return response;
        }

         // [Test Case: 2568] ReceiveBladeSerialData: Verify users from WcsCmAdmin are able to execute the command
        // [Test Case: 2575] ReceiveSerialPortData: Verify users member of the WcsCmUser group are not able to execute the command
        // [Test Case: 2576] ReceiveSerialPortData: Verify only users member of Operator or Admin is able to execute the command
        private TestResponse ReceiveBladeSerialDataByAllUsers(int bladeId)
        {
            CmTestLog.Info("\n !!!!!! Verifying ReceiveBladeSerialData By All Users.!!!!!!");
            TestResponse response = new TestResponse();

            // Start Blade Serial Session
            StartSerialResponse startSerialResponse = null;
            startSerialResponse = StartBladeSerialSessionAndKillExistingSession(ref response, startSerialResponse, bladeId);

            byte[] payload = Encoding.ASCII.GetBytes(CmConstants.SerialCommand);

            // Send Blade Serial Data
            ChassisResponse sendDataResponse = this.Channel.SendBladeSerialData(bladeId, startSerialResponse.serialSessionToken, payload);
            
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, sendDataResponse.completionCode, "Send blade serial data"))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to send blade serial data. Failure is: " + sendDataResponse.completionCode);
            }

            // Loop through different user types and start blade serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to ReceiveBladeSerialData with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // ReceiveBladeSerialData 
                    SerialDataResponse receiveDataResponse = this.TestChannelContext.ReceiveBladeSerialData(bladeId, startSerialResponse.serialSessionToken);

                    TestResponse testResponse = this.VerifyReceiveBladeSerialDataByUser(response, roleId, receiveDataResponse);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, "ReceiveBladeSerialData returned Bad Request for user" + roleId.ToString()))
                        {
                            response.Result = false;
                            response.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        response.Result = false;
                        response.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(response.Result);
            return response;
        }

        private TestResponse VerifyStartBladeSerialSessionByUser(TestResponse response, WCSSecurityRole roleId, StartSerialResponse startResponse)
        {
            if (!(startResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to start serial session with WcsAdmin User {0}, completion code returned: {1} ", roleId, startResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((startResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User/Operator is able to start blade serial session {0}", roleId, "completion code returned: {1} ", startResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue((startResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startResponse.completionCode),
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        private TestResponse VerifyStopBladeSerialSessionByUser(TestResponse response, WCSSecurityRole roleId, ChassisResponse stopResponse)
        {
            if (!((CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin)))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to stop serial session with WcsAdmin User {0}, completion code returned: {1} ", roleId, stopResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User/Operator is able to stop blade serial session {0}", roleId, "completion code returned: {1} ", stopResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue((CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode),
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        private TestResponse VerifySendBladeSerialDataByUser(TestResponse response, WCSSecurityRole roleId, ChassisResponse sendResponse)
        {
            if (!((CompletionCode.Success == sendResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin)))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to send blade serial data with WcsAdmin User {0}, completion code returned: {1} ", roleId, sendResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((CompletionCode.Success == sendResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User/Operator is able to send blade serial data {0}", roleId, "completion code returned: {1} ", sendResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue(CompletionCode.Success == sendResponse.completionCode,
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        private TestResponse VerifyReceiveBladeSerialDataByUser(TestResponse response, WCSSecurityRole roleId, SerialDataResponse receiveDataResponse)
        {
            if (!((CompletionCode.Success == receiveDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin)))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to receive blade serial data with WcsAdmin User {0}, completion code returned: {1} ", roleId, receiveDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((CompletionCode.Success == receiveDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User/Operator is able to receive blade serial data {0}", roleId, "completion code returned: {1} ", receiveDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue(CompletionCode.Success == receiveDataResponse.completionCode,
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        /// <summary>
        /// Verifies StartBladeSerialSession: Checks Completion Code: Success and Get session Token from response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="bladeId"></param>
        /// <param name="startResponse"></param>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        private TestResponse VerifyStartBladeSerialSession(TestResponse response, int bladeId, ref StartSerialResponse startResponse, out string sessionToken)
        {
            sessionToken = string.Empty;
            startResponse = this.Channel.StartBladeSerialSession(bladeId, CmConstants.SerialTimeoutSeconds);

            // Verify Completion code & Session Token
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, startResponse.completionCode, "Serial session started"))
            {
                response.Result = false;
                response.ResultDescription.Append(String.Format("\nFailed to start serial session with blade# {0}, completion code returned: {1}", bladeId, startResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            sessionToken = startResponse.serialSessionToken;
            if (!ChassisManagerTestHelper.IsTrue(!string.IsNullOrEmpty(sessionToken), "Serial session token received for blade# " + bladeId))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to receive serial session token, Session token returned: " + startResponse.serialSessionToken);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }

            CmTestLog.Success("Successfully started blade serial session & received session token for blade# "+ bladeId);
            return response;
        }

        /// <summary>
        /// Test Command: StartPortSerialSession, StopPortSerialSession. 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// The serial session can be successfully started on port;
        /// The serial session can be successfully stopped provided the session token.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse StartStopSerialPortConsoleTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();

            // get all server blade locations
            this.ServerLocations = this.GetServerLocations();

            // pick up a random server blade
            int bladeId = ServerLocations.RandomOrDefault();
            CmTestLog.Info(string.Format("Picked a random server blade# {0} for test", bladeId));

            StartSerialResponse startBladeSerialResponse = null;
            StartSerialResponse startPortSerialResponse = null;

            // kill any existing serial session first
            CmTestLog.Info(string.Format("Kill all existing serial session for port# {0}", CmConstants.COMPortId));
            ChassisResponse killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, null, true);
            if (!(CompletionCode.NoActiveSerialSession == killSession.completionCode || CompletionCode.Success == killSession.completionCode))
            {
                response.Result = false;
                response.ResultDescription.Append(killSession.statusDescription);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }

            CmTestLog.Info(string.Format("Kill all existing serial session for port# {0}", CmConstants.SecondCOMPortId));
            ChassisResponse killSession2 = this.Channel.StopSerialPortConsole(CmConstants.SecondCOMPortId, null, true);
            if (!(CompletionCode.NoActiveSerialSession == killSession2.completionCode || CompletionCode.Success == killSession2.completionCode))
            {
                response.Result = false;
                response.ResultDescription.Append(killSession2.statusDescription);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            
            // [Test Case: 4568] StartSerialPortConsole: Verify start serial session with a invalid port index
            for (int InvalidPortId = 3; InvalidPortId <= 6; InvalidPortId++)
            {
                CmTestLog.Info(string.Format("Trying to start a port serial console for invalidPortNumber# {0}", InvalidPortId));
                startPortSerialResponse = this.Channel.StartSerialPortConsole(InvalidPortId, CmConstants.SessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);

                if (InvalidPortId == 3 || InvalidPortId == 4)
                {
                    // Verify ParameterOutOfRange CompletionCode & null serial session token for COM3 & COM4
                    if (!ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, startPortSerialResponse.completionCode, "Received ParameterOutOfRange completion code for Port# " + InvalidPortId))
                    {
                        response.Result = false;
                        response.ResultDescription.Append(" Failed to Receive ParameterOutOfRange completion code for Port# " + InvalidPortId);
                    }
                    if (!ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(startPortSerialResponse.serialSessionToken), "Received null serial session token"))
                    {
                        response.Result = false;
                        response.ResultDescription.Append(" Failed to receive null serial session token for invalidPortNumber# " + InvalidPortId);
                    }
                }
                else
                {
                    // Verify Failure CompletionCode & null serial session token for COM5 & COM6
                    if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, startPortSerialResponse.completionCode, "Received Failure completion code for Port# " + InvalidPortId))
                    {
                        response.Result = false;
                        response.ResultDescription.Append(" Failed to Receive Failure completion code for Port# " + InvalidPortId);
                    }
                    if (!ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(startPortSerialResponse.serialSessionToken), "Received null serial session token"))
                    {
                        response.Result = false;
                        response.ResultDescription.Append(" Failed to receive null serial session token for invalidPort# " + InvalidPortId);
                    }
                }
            }

            // [Test Case 2071] Verify we allow simultaneous blade and console serial session
            // [Test Case 4566] StartSerialPortConsole: Verify incoming serial connection to a port is allowed even if a blade serial session is open
            // kill any existing serial session & Start blade serial session 
            startBladeSerialResponse = StartBladeSerialSessionAndKillExistingSession(ref response, startBladeSerialResponse, bladeId);

            // [Test Case: 4559] StartSerialPortConsole: Verify session timeout based on sessionTimeoutInSecs value
            CmTestLog.Info(string.Format("Verifying session is automatically killed based on sessionTimeoutInsecs value PortId# {0}", CmConstants.COMPortId));

            // start serial port Console 
            CmTestLog.Info(string.Format("Trying to start a port serial console for Port# {0}", CmConstants.COMPortId));
            startPortSerialResponse = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.SessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);

            TestResponse testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialResponse);
            response.Result &= testResponse.Result;

            CmTestLog.Info("Trying to start serial port console when session is already active");
            startPortSerialResponse = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.SessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, startPortSerialResponse.completionCode, "Received SerialSessionActive completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive SerialSessionActive completion code, Completion code returned: " + startPortSerialResponse.completionCode);
            }
            
            Thread.Sleep(TimeSpan.FromSeconds(80));
            startPortSerialResponse = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.SessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);
            testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialResponse);
            response.Result &= testResponse.Result;
            if (response.Result == true)
            {
                CmTestLog.Success("Port Serial session started successfully after complete given sessionTimeoutInsecs#: " + CmConstants.SessionTimeoutInSecs);
            }
             // [Test Case: 4585] StopSerialPortConsole: Verify default ForceKill parameter does NOT bypass sessionToken option
            // [Test Case: 4586] StopSerialPortConsole: Verify false ForceKill parameter does NOT bypass sessionToken option
            CmTestLog.Info(string.Format("Trying to stop the serial port console invalid session token and no force kill"));
            string invalidSessionToken = startPortSerialResponse.serialSessionToken + startPortSerialResponse.serialSessionToken;
            killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, invalidSessionToken, false);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, killSession.completionCode, "Received SerialSessionActive Completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive SerialSessionActive for invalid session token and no force kill, Completion code returned: " + killSession.completionCode);
            }

            // [Test Case: 4591] StopSerialPortConsole: Verify true ForceKill parameter bypasses sessionToken option and closes serial session
            CmTestLog.Info(string.Format("Trying to stop the serial session invalid session token and true force kill"));
            killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, invalidSessionToken, true);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, killSession.completionCode, "Serial session stopped"))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to stop serial session for invalid session token and true force kill, Completion code returned: " + killSession.completionCode);
            }

            // [Test Case: 4561] StartSerialPortConsole: Verify session times out after default seconds when no sessionTimeoutInSecs option is used
            CmTestLog.Info("Trying to start serial port console & Verify session times out after default seconds when no sessionTimeoutInSecs option is used");
            startPortSerialResponse = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.NosessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);

            testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialResponse);
            response.Result &= testResponse.Result;

            SerialDataResponse receiveSerialPortData = this.Channel.ReceiveSerialPortData(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, receiveSerialPortData.completionCode, "Received TimeOut completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive TimeOut completion code, Completion code returned: " + receiveSerialPortData.completionCode);
            }

            Thread.Sleep(TimeSpan.FromMinutes(3));

            receiveSerialPortData = this.Channel.ReceiveSerialPortData(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken);
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.NoActiveSerialSession, receiveSerialPortData.completionCode, "Received NoActiveSerialSession completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive NoActiveSerialSession completion code, Completion code returned: " + receiveSerialPortData.completionCode);
            }
            else
            {
                CmTestLog.Success("Port Serial Session is timed out after default seconds when no sessionTimeoutInSecs option is used , defult sessiontimeoutInSec#  " + CmConstants.DefaultsessionTimeoutInSecs);
            }

            // [Test Case: 4621] StopSerialPortConsole: Verify command failure after default timeout seconds
            // [Test Case: 4584] StopSerialPortConsole: Verify command failure after port serial session already closed
            CmTestLog.Info("Trying to stop serial port console after default timeout seconds & with no session is active");
            killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken, false);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.NoActiveSerialSession, killSession.completionCode, "Received NoActiveSerialSession completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive NoActiveSerialSession completion code, Completion code returned: " + killSession.completionCode);
            }

            // [Test Case: 2067] Synchronious start of multiple serial sessions: Verify no race condition when multiple requests are sent to start serial session.
            StartSerialResponse startPortSerialConsole1 = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.NosessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);
            string port1SessionToken = startPortSerialConsole1.serialSessionToken;
            testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialConsole1);
            response.Result &= testResponse.Result;

            startPortSerialConsole1 = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.NosessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.SerialSessionActive, startPortSerialConsole1.completionCode, "Received SerialSessionActive Completion code"))
            {
                response.Result = false;
                response.ResultDescription.Append("\n Failed to receive SerialSessionActive for start serial port console when it is already active, Completion code returned: " + startPortSerialConsole1.completionCode);
            }

            StartSerialResponse startPortSerialConsole2 = this.Channel.StartSerialPortConsole(CmConstants.SecondCOMPortId, CmConstants.NosessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);
            testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialConsole2);
            response.Result &= testResponse.Result;

            // [Test Case: 4583] StopSerialPortConsole: Verify command success only for valid sessionToken
            // [Test Case: 4601] StopSerialPortConsole: Verify command only closes port serial session on specified port and no other port
            killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, port1SessionToken, false);
            if (!((ChassisManagerTestHelper.AreEqual(CompletionCode.Success, killSession.completionCode, "Serial session stopped")) && (ChassisManagerTestHelper.AreEqual(CompletionCode.Success, startPortSerialConsole2.completionCode, "Serial Session Active"))))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to stop port serial session when closes on specified port, Completion code returned: " + killSession.completionCode);
            }

            // kill any existing blade serial session first
            CmTestLog.Info(string.Format("Kill all existing serial session for Blade# {0}", bladeId));
            ChassisResponse killBladeSession = this.Channel.StopBladeSerialSession(bladeId, null, true);
            if (!(CompletionCode.NoActiveSerialSession == killBladeSession.completionCode || CompletionCode.Success == killBladeSession.completionCode))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to kill existing session for blade# " + bladeId);
            }

            // [Test Case: 2073] Verify when a serial session is active, system COM 4 IPMI commands should not be interrupted
            CmTestLog.Info("Trying to GetBladeInfo when port serial session is active for bladeId# " + bladeId);
            BladeInfoResponse bladeResponse = this.Channel.GetBladeInfo(bladeId);
            if (!((ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeResponse.completionCode, "Received Success Completion Code for GetBladeInfo")) && (ChassisManagerTestHelper.AreEqual(CompletionCode.Success,startPortSerialConsole2.completionCode,"Serial Session Active"))))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive Success completion code for getbladeInfo, Completion code returned: " + bladeResponse.completionCode);
            }

            // [Test Case: 2074] Verify When a serial session is active, all COM 3 commands can be executed
            CmTestLog.Info("Trying to GetChassisInfo when port serial session is active ");
            ChassisInfoResponse chassisinfoResponse = this.Channel.GetChassisInfo(true, true, true, true);
            if (!((ChassisManagerTestHelper.AreEqual(CompletionCode.Success, chassisinfoResponse.completionCode, "Received Success Completion Code for GetChassisInfo")) && (ChassisManagerTestHelper.AreEqual(CompletionCode.Success, startPortSerialConsole2.completionCode, "Serial Session Active"))))
            {
                response.Result = false;
                response.ResultDescription.Append(" Failed to receive Success completion code for GetChassisInfo , Completion code returned: " + chassisinfoResponse.completionCode);
            }
            
            CmTestLog.End(response.Result);
            return response;
        }


        /// <summary>
        /// Verifies StartSerialPortConsole, StopSerialPortConsole, SendSerialPortData and ReceiveSerialPortData by all users
        /// Bad Request for WcsUser 
        /// Success for WcsAdmin and WcsOperator
        /// </summary>
        /// <returns>Returns true if all check points pass</returns>
        public TestResponse StartStopSendReceivePortSerialConsoleByAllUsersTest()
        {
            CmTestLog.Start();
            TestResponse testRunResponse = new TestResponse();

            VerifyResponeAndAddFailureDescription(this.StartSerialPortConsoleByallUsers(), testRunResponse);
            VerifyResponeAndAddFailureDescription(this.StopSerialPortConsoleByallUsers(), testRunResponse);
            VerifyResponeAndAddFailureDescription(this.SendSerialPortDataByallUsers(), testRunResponse);
            VerifyResponeAndAddFailureDescription(this.ReceiveSerialPortDataByallUsers(), testRunResponse);

            CmTestLog.End(testRunResponse.Result);
            return testRunResponse;
        }

        // [Test Case: 2570] StartSerialPortConsole: Verify only users member of WcsCmOperator or WcsCmAdmin are able to execute the command
        private TestResponse StartSerialPortConsoleByallUsers()
        {
            CmTestLog.Info("\n !!!!!!Verifying StartSerialPortConsole By All Users.!!!!!!");
            TestResponse testRunResponse = new TestResponse();

            // Loop through different user types and start port serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    StartSerialResponse startSerialPortResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to StartSerialPortConsole with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    startSerialPortResponse = this.TestChannelContext.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.SessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);

                    TestResponse testMethodResponse = this.VerifyStartSerialPortConsoleByUser(testRunResponse, roleId, startSerialPortResponse);
                    testRunResponse.Result &= testMethodResponse.Result;
                    testRunResponse.ResultDescription.Append(testMethodResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, " StartSerialPortConsole returned Bad Request for user " + roleId.ToString()))
                        {
                            testRunResponse.Result = false;
                            testRunResponse.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        testRunResponse.Result = false;
                        testRunResponse.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(testRunResponse.Result);
            return testRunResponse;
        }

        // [Test Case: 4582] StopSerialPortConsole: Verify only users member of WcsCmAdmin and WcsCmOperator are able to execute the command
        private TestResponse StopSerialPortConsoleByallUsers()
        {
            CmTestLog.Info("\n !!!!!!Verifying StopSerialPortConsole By All Users.!!!!!!");
            TestResponse testRunResponse = new TestResponse();

            // Loop through different user types and stop port serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    ChassisResponse stopSerialPortResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to StopSerialPortConsole with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    stopSerialPortResponse = this.TestChannelContext.StopSerialPortConsole(CmConstants.COMPortId, null, true);

                    TestResponse testMethodResponse = this.VerifyStopSerialPortConsoleByUser(testRunResponse, roleId, stopSerialPortResponse);
                    testRunResponse.Result &= testMethodResponse.Result;
                    testRunResponse.ResultDescription.Append(testMethodResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, " StopSerialPortConsole returned Bad Request for user " + roleId.ToString()))
                        {
                            testRunResponse.Result = false;
                            testRunResponse.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        testRunResponse.Result = false;
                        testRunResponse.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(testRunResponse.Result);
            return testRunResponse;
        }

        // [Test Case: 2573] SendSerialPortData: Verify only users member of Operator or Admin is able to execute the command
        // [Test Case: 2574] SendSerialPortData: Verify users member of the WcsCmUser group are not able to execute the command
        private TestResponse SendSerialPortDataByallUsers()
        {
            CmTestLog.Info("\n !!!!!!Verifying SendSerialPortData By All Users.!!!!!!");
            TestResponse testRunResponse = new TestResponse();

            StartSerialResponse startPortSerialResponse = null;
            startPortSerialResponse = StartPortSerialSessionAndKillExistingSession(ref testRunResponse);

            byte[] payload = Encoding.ASCII.GetBytes(CmConstants.SerialCommand);

            // Loop through different user types and send serial port data 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SendSerialPortData with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    ChassisResponse sendSerialPortDataResponse = this.TestChannelContext.SendSerialPortData(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken, payload);

                    TestResponse testResponse = this.VerifySendPortSerialDataByUser(testRunResponse, roleId, sendSerialPortDataResponse);
                    testRunResponse.Result &= testResponse.Result;
                    testRunResponse.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, "SendSerialPortData returned Bad Request for user" + roleId.ToString()))
                        {
                            testRunResponse.Result = false;
                            testRunResponse.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        testRunResponse.Result = false;
                        testRunResponse.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(testRunResponse.Result);
            return testRunResponse;
        }

        // [Test Case: 2575] ReceiveSerialPortData: Verify users member of the WcsCmUser group are not able to execute the command
        // [Test Case: 2576] ReceiveSerialPortData: Verify only users member of Operator or Admin is able to execute the command
        private TestResponse ReceiveSerialPortDataByallUsers()
        {
            CmTestLog.Info("\n !!!!!! Verifying ReceiveSerialPortData By All Users.!!!!!!");
            TestResponse testRunResponse = new TestResponse();

            // Start port Serial Session
            StartSerialResponse startPortSerialResponse = null;
            startPortSerialResponse = StartPortSerialSessionAndKillExistingSession(ref testRunResponse);

            byte[] payload = Encoding.ASCII.GetBytes(CmConstants.SerialCommand);

            // Send Serial Port Data
            ChassisResponse sendPortSerialDataResponse = this.Channel.SendSerialPortData(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken, payload);

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, sendPortSerialDataResponse.completionCode, "Send serial port data"))
            {
                testRunResponse.Result = false;
                testRunResponse.ResultDescription.Append("\nFailed to send serial port data. Failure is: " + sendPortSerialDataResponse.completionCode);
            }

            // Loop through different user types and start blade serial session
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to ReceiveSerialPortData with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // ReceiveSerialPortData 
                    SerialDataResponse receivePortSerialDataResponse = this.TestChannelContext.ReceiveSerialPortData(CmConstants.COMPortId, startPortSerialResponse.serialSessionToken);

                    TestResponse testResponse = this.VerifyReceiveSerialPortDataByUser(testRunResponse, roleId, receivePortSerialDataResponse);
                    testRunResponse.Result &= testResponse.Result;
                    testRunResponse.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        // Test failed, http response should contain http 401 error
                        if (!ChassisManagerTestHelper.IsTrue(true, "ReceiveSerialPortData returned Bad Request for user" + roleId.ToString()))
                        {
                            testRunResponse.Result = false;
                            testRunResponse.ResultDescription.Append(ex.Message);
                        }
                    }
                    else
                    {
                        testRunResponse.Result = false;
                        testRunResponse.ResultDescription.Append(ex.Message);
                    }
                }
            }

            CmTestLog.End(testRunResponse.Result);
            return testRunResponse;
        }

        private StartSerialResponse StartPortSerialSessionAndKillExistingSession(ref TestResponse response)
        {
            // kill any existing serial session first
            CmTestLog.Info(string.Format("Kill all existing serial session for ComePort# {0}", CmConstants.COMPortId));
            ChassisResponse killSession = this.Channel.StopSerialPortConsole(CmConstants.COMPortId, null, true);
            if (!(CompletionCode.NoActiveSerialSession == killSession.completionCode || CompletionCode.Success == killSession.completionCode))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to kill existing session for blade# " + CmConstants.COMPortId);
            }

            // start port serial session 
            CmTestLog.Info(string.Format("Trying to start port serial session for Port# {0}", CmConstants.COMPortId));
            StartSerialResponse startPortSerialResponse = this.Channel.StartSerialPortConsole(CmConstants.COMPortId, CmConstants.NosessionTimeoutInSecs, CmConstants.DeviceTimeoutInMsecs, CmConstants.BaudRate);
            TestResponse testResponse = this.VerifyStartSerialPortConsole(response, startPortSerialResponse);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);
            return startPortSerialResponse;
        }

        private TestResponse VerifyStartSerialPortConsoleByUser(TestResponse testRunResponse, WCSSecurityRole roleId, StartSerialResponse startSerialPortResponse)
        {
            if (!(startSerialPortResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startSerialPortResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                testRunResponse.Result = false;
                testRunResponse.ResultDescription.Append(string.Format("\nFailed to start serial session with WcsAdmin/WcsOperator User {0}, completion code returned: {1} ", roleId, startSerialPortResponse.completionCode));
                return testRunResponse;
            }
            else if ((startSerialPortResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startSerialPortResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser))
            {
                testRunResponse.Result = false;
                testRunResponse.ResultDescription.Append(string.Format("User is able to start blade serial session {0}", roleId, "completion code returned: {1} ", startSerialPortResponse.completionCode));
                return testRunResponse;
            }
            else
            {
                testRunResponse.Result &= ChassisManagerTestHelper.IsTrue((startSerialPortResponse.completionCode == CompletionCode.Success || CompletionCode.SerialSessionActive == startSerialPortResponse.completionCode),
                     string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return testRunResponse;
        }

        private TestResponse VerifyStopSerialPortConsoleByUser(TestResponse response, WCSSecurityRole roleId, ChassisResponse stopResponse)
        {
            if (!(CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to stop Port serial session with WcsAdmin/WcsOperator User {0}, completion code returned: {1} ", roleId, stopResponse.completionCode));
                return response;
            }
            else if ((CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("WcsUser is able to stop port serial session {0}", roleId, "completion code returned: {1} ", stopResponse.completionCode));
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue((CompletionCode.NoActiveSerialSession == stopResponse.completionCode || CompletionCode.Success == stopResponse.completionCode),
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        private TestResponse VerifySendPortSerialDataByUser(TestResponse response, WCSSecurityRole roleId, ChassisResponse sendPortSerialDataResponse)
        {
            if (!(CompletionCode.Success == sendPortSerialDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to send port serial data with WcsAdmin/WcsOperator User {0}, completion code returned: {1} ", roleId, sendPortSerialDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((CompletionCode.Success == sendPortSerialDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User is able to send port serial data {0}", roleId, "completion code returned: {1} ", sendPortSerialDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue(CompletionCode.Success == sendPortSerialDataResponse.completionCode,
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        private TestResponse VerifyReceiveSerialPortDataByUser(TestResponse response, WCSSecurityRole roleId, SerialDataResponse receiveDataResponse)
        {
            if (!(CompletionCode.Success == receiveDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("\nFailed to receive serial port data with WcsAdmin/WcsOperator User {0}, completion code returned: {1} ", roleId, receiveDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else if ((CompletionCode.Success == receiveDataResponse.completionCode) && (roleId == WCSSecurityRole.WcsCmUser))
            {
                response.Result = false;
                response.ResultDescription.Append(string.Format("User is able to receive serial port data {0}", roleId, "completion code returned: {1} ", receiveDataResponse.completionCode));
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            else
            {
                response.Result &= ChassisManagerTestHelper.IsTrue(CompletionCode.Success == receiveDataResponse.completionCode,
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return response;
        }

        /// <summary>
        /// Verifies StartPortSerialConsole: Checks Completion Code: Success and Get session Token from response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="bladeId"></param>
        /// <param name="startResponse"></param>
        /// <param name="sessionToken"></param>
        /// <returns></returns>
        private TestResponse VerifyStartSerialPortConsole(TestResponse response, StartSerialResponse startResponse)
        {
            // Verify Completion code & Session Token
            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, startResponse.completionCode, "Port serial console started"))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to start port serial console, completion code returned: " + startResponse.completionCode);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            if (!ChassisManagerTestHelper.IsTrue(!string.IsNullOrEmpty(startResponse.serialSessionToken), "Serial session token received"))
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to receive serial session token, Session token returned: " + startResponse.serialSessionToken);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }

            CmTestLog.Success("Successfully started port serial console & received session token");
            return response;
        }
    }
}
