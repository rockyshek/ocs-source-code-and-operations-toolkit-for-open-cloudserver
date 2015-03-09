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
    using System.Linq;
    using System.Threading;
    using Microsoft.GFS.WCS.Contracts;

    internal class BladeMezzCommands : CommandBase
    {

        #region Constructors

        internal BladeMezzCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts)
            : base(channel, TestChannelContexts)
        {
            this.currentApi = null;
        }

        #endregion

        #region Helper Variables

        private string currentApi;

        private enum InvalidBladeId : int
        {
            min32 = Int32.MinValue,
            max32 = Int32.MaxValue,
            zero = 0,
            mediumNegative = -12,
            mediumPositive = 25
        };

        private static class LogMessage
        {
            public static string Message { get; set; }

            public enum MessageType
            {
                Success,
                Failure,
                Info
            };

            public static void Log(MessageType type, ref TestResponse response)
            {
                switch (type)
                {
                    case MessageType.Success:
                        {
                            CmTestLog.Success(Message);
                            break;
                        }
                    case MessageType.Failure:
                        {
                            CmTestLog.Failure(Message);
                            response.Result = false;
                            break;
                        }
                    case MessageType.Info:
                        {
                            CmTestLog.Info(Message);
                            break;
                        }
                    default:
                        break;
                }
                response.ResultDescription.Append(Message);
            }
        }

        public class TestCase
        {
            public string TestCaseName { get; private set; }
            public int WorkItemId { get; private set; }

            public TestCase(string name, int workItem)
            {
                this.TestCaseName = name;
                this.WorkItemId = workItem;
            }

            public void LogPassOrFail(ref TestResponse response, string currentApi)
            {
                if (response.Result)
                {
                    LogMessage.Message = string.Format(
                        "{0}: {1} PASSED - WorkItem: {2}\n\n", currentApi, this.TestCaseName, this.WorkItemId);
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
                }
                else
                {
                    LogMessage.Message = string.Format(
                        "{0}: {1} FAILED - WorkItem: {2}\n\n", currentApi, this.TestCaseName, this.WorkItemId);
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                }
            }
        }

        #endregion

        #region GetBladeMezzPassThroughMode Test Cases

        /// <summary>
        /// Test Command: GetBladeMezzPassThroughMode 
        /// The test case verifies: 
        /// all groups can execute command
        /// CommandNotValidForBlade for JBOD
        /// Failure for empty slot
        /// Failure for blade soft-powered off
        /// DevicePoweredOff for blade hard-powered off
        /// ParameterOutOfRange for invalid bladeId 
        /// Correct Pass-Through Mode after toggling SetBladeMezzPassThroughMode
        /// Returns Pass-Through Mode True after restarting blade
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse GetBladeMezzPassThroughModeTest(string specifiedBladeLocations)
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            this.currentApi = "GetBladeMezzPassThroughMode";

            try
            {
                // Get all present blade server, JBOD, and empty locations    
                this.ServerLocations = this.GetServerLocations();
                this.JbodLocations = this.GetJbodLocations();
                this.EmptyLocations = this.GetEmptyLocations();

                // Check if any specific blade locations are to be tested
                CheckSpecifiedBladeLocations(specifiedBladeLocations);

                // Run test cases not specific to blade locations
                VerifyResponseAndAddFailureDescription(
                    ParameterOutOfRangeBladeIdGetBladeMezzPassThroughMode(), response);

                // Run test cases
                for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                {
                    // Run test cases for every server blade location
                    if (ServerLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            AllGroupsCanExecuteGetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            FailureForSoftOffGetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            DevicePoweredOffForHardOffGetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            CorrectModeAfterToggleGetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            TrueAfterRestartGetBladeMezzPassThroughMode(bladeId), response);
                    }
                    // Run test cases for every JBOD location
                    else if (JbodLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            CommandNotValidJBODGetBladeMezzPassThroughMode(bladeId), response);
                    }
                    // Run test cases for every empty location
                    else if (EmptyLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            FailureForEmptyGetBladeMezzPassThroughMode(bladeId), response);
                    }
                }

            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            CmTestLog.End(response.Result);
            return response;
        }



        private TestResponse AllGroupsCanExecuteGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("AllGroupsCanExecuteGetBladeMezzPassThroughMode", 24166);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Can Be Executed By All Users. WorkItemId: {1}.!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Loop through different user types 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();

                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                // Get Pass Through Mode and verify
                getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);

                LogMessage.Message = string.Format(
                        "{0}: Command returned completion code {1} for user {2} and blade Id {3}",
                        this.currentApi, getBladeMezzResponse.completionCode,
                        Enum.GetName(typeof(WCSSecurityRole), roleId), bladeId);

                if (getBladeMezzResponse.completionCode != CompletionCode.Success)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, this.currentApi);

            return response;
        }

        private TestResponse CommandNotValidJBODGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("CommandNotValidJBODGetBladeMezzPassThroughMode", 24167);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns CommandNotValidForBlade for JBOD. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify CommandNotValidForBlade
            getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);
            LogMessage.Message = string.Format(
                    "{0}: Command for JBOD location returned completion code {1} and blade Id {2}",
                    this.currentApi, getBladeMezzResponse.completionCode, bladeId);

            if (getBladeMezzResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);

            return response;
        }

        private TestResponse FailureForEmptyGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForEmptyGetBladeMezzPassThroughMode", 24170);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Failure for Empty Location. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify Failure
            getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);
            LogMessage.Message = string.Format(
                "{0}: Command for empty location with completion code {1} and blade Id {2}",
                    this.currentApi, getBladeMezzResponse.completionCode, bladeId);

            if (getBladeMezzResponse.completionCode != CompletionCode.Failure)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);

            return response;
        }

        private TestResponse FailureForSoftOffGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForSoftOffGetBladeMezzPassThroughMode", 24173);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Failure for Server Blade Soft-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Soft-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetBladeOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Soft-powered off blade {0}. Thread sleeping for {1} seconds...",
                   bladeId, CmConstants.BladePowerOffSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

                // Call API and verify Failure
                getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);
                if (getBladeMezzResponse.completionCode != CompletionCode.Failure)
                {
                    LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is soft-powered off",
                        this.currentApi, getBladeMezzResponse.completionCode, bladeId);
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                }
                else
                {
                    LogMessage.Message = string.Format(
                        "{0}: Command successfully returns completion code Failure after blade {1} is soft-powered off",
                        this.currentApi, bladeId);
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
                }
            }

            // Code clean-up. Soft-power on blade
            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Soft-powered on blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            testCase.LogPassOrFail(ref response, this.currentApi);

            return response;
        }

        private TestResponse DevicePoweredOffForHardOffGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForHardOffGetBladeMezzPassThroughMode", 24175);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns DevicePoweredOff for Server Blade Hard-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Hard-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetPowerOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power Off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Hard-powered Off blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOffSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

                // Call API and verify DevicePoweredOff
                getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is hard-powered Off",
                        this.currentApi, getBladeMezzResponse.completionCode, bladeId);
                if (getBladeMezzResponse.completionCode != CompletionCode.DevicePoweredOff)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            // Test CleanUp: Hard-Power On and Soft-Power On Blade
            bladeResponse = this.TestChannelContext.SetPowerOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power On server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Hard-powered On blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Soft-powered on blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            testCase.LogPassOrFail(ref response, this.currentApi);

            return response;
        }

        private TestResponse ParameterOutOfRangeBladeIdGetBladeMezzPassThroughMode()
        {
            TestCase testCase = new TestCase("ParameterOutOfRangeBladeIdGetBladeMezzPassThroughMode", 24177);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns ParameterOutOfRange for Invalid BladeId. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            foreach (int invalidValue in Enum.GetValues(typeof(InvalidBladeId)))
            {
                getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(invalidValue);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} for invalid Blade Id {2}",
                        this.currentApi, getBladeMezzResponse.completionCode, invalidValue);

                if (getBladeMezzResponse.completionCode != CompletionCode.ParameterOutOfRange)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);

            return response;
        }

        private TestResponse CorrectModeAfterToggleGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("CorrectModeAfterToggleGetBladeMezzPassThroughMode", 24180);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Correct Pass-ThroughMode After Toggling SetBladeMezzPassThroughMode. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "true");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "true");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "true");

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "false");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "false");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "false");

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "true");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "true");

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "true");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "true");

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "false");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "false");

            CallAndVerifySetBladeMezzPassThroughMode(bladeId, ref response, "false");
            CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "false");

            testCase.LogPassOrFail(ref response, currentApi);

            return response;
        }

        private TestResponse TrueAfterRestartGetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("TrueAfterRestartGetBladeMezzPassThroughMode", 24181);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Correct Pass-ThroughMode After Restarting Blade. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeActivePowerCycleResponse = new BladeResponse();
            BladeStateResponse getBladeStateResponse = new BladeStateResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Restart blade
            setBladeActivePowerCycleResponse = this.TestChannelContext.SetBladeActivePowerCycle(bladeId, Convert.ToInt32(CmConstants.OffTimeSec));
            if (setBladeActivePowerCycleResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Failed to restart blade {1} with completion code {2}",
                    this.currentApi, bladeId, setBladeActivePowerCycleResponse.completionCode);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }

            // Sleep Thread for 2 minutes 30 seconds
            CmTestLog.Info(string.Format("Thread sleeping for {0} seconds...",
                TimeSpan.FromSeconds(150)));
            Thread.Sleep(TimeSpan.FromSeconds(150));

            // Verify blade soft-powered on
            getBladeStateResponse = this.TestChannelContext.GetBladeState(bladeId);
            if (getBladeStateResponse.completionCode != CompletionCode.Success ||
                getBladeStateResponse.bladeState != PowerState.ON)
            {
                LogMessage.Message = string.Format(
                        "{0}: Blade {1} did not soft-power ON",
                        this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CallAndVerifyGetBladeMezzPassThroughMode(bladeId, ref response, "true");
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        #endregion

        #region SetBladeMezzPassThroughMode Test Cases

        /// <summary>
        /// Test Command: SetBladeMezzPassThroughMode 
        /// The test case verifies: 
        /// only Admin can execute command
        /// CommandNotValidForBlade for JBOD
        /// Failure for empty slot
        /// Failure for blade soft-powered off
        /// DevicePoweredOff for blade hard-powered off
        /// ParameterOutOfRange for invalid bladeId 
        /// Invalid Request status description returned for invalid passThroughModeEnabled
        /// Command success for capitalization in passThroughModeEnabled parameter
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse SetBladeMezzPassThroughModeTest(string specifiedBladeLocations)
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            this.currentApi = "SetBladeMezzPassThroughMode";

            try
            {
                // Get all present blade server, JBOD, and empty locations    
                this.ServerLocations = this.GetServerLocations();
                this.JbodLocations = this.GetJbodLocations();
                this.EmptyLocations = this.GetEmptyLocations();

                // Check if any specific blade locations are to be tested
                CheckSpecifiedBladeLocations(specifiedBladeLocations);

                // Run test cases not specific to blade locations
                VerifyResponseAndAddFailureDescription(
                    ParameterOutOfRangeBladeIdSetBladeMezzPassThroughMode(), response);

                // Run test cases
                for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                {
                    // Run test cases for every server blade location
                    if (ServerLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            OnlyAdminCanExecuteSetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            FailureForSoftOffSetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            DevicePoweredOffForHardOffSetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            InvalidRequestForInvalidPassThroughSetBladeMezzPassThroughMode(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            VerifySuccessWithCapitalizationOfPassThroughSetBladeMezzPassThroughMode(bladeId), response);
                    }
                    // Run test cases for every JBOD location
                    else if (JbodLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            CommandNotValidJBODSetBladeMezzPassThroughMode(bladeId), response);
                    }
                    // Run test cases for every empty location
                    else if (EmptyLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            FailureForEmptySetBladeMezzPassThroughMode(bladeId), response);
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            CmTestLog.End(response.Result);
            return response;
        }

        private TestResponse OnlyAdminCanExecuteSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("OnlyAdminCanExecuteSetBladeMezzPassThroughMode", 24171);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Can Only Be Executed By WcsCmAdmin Users. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();

            // Loop through different user types 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                try
                {
                    setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, "true");
                    LogMessage.Message = string.Format(
                            "{0}: Command returns completion code {1} for user {2} for blade Id {3}",
                            currentApi, setBladeMezzResponse.completionCode, Enum.GetName(typeof(WCSSecurityRole), roleId), bladeId);

                    if (setBladeMezzResponse.completionCode == CompletionCode.Success &&
                        roleId == WCSSecurityRole.WcsCmAdmin)
                        LogMessage.Log(LogMessage.MessageType.Success, ref response);
                    else
                        LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 400 unauthorize
                    if (ex.Message.Contains("400"))
                    {
                        // Check if 400 error due to WcsAdmin (fail)
                        LogMessage.Message = string.Format("{0}: Command returned Bad Request for user {1} for bladeId {2}",
                            currentApi, Enum.GetName(typeof(WCSSecurityRole), roleId), bladeId);

                        if (roleId != WCSSecurityRole.WcsCmAdmin)
                            LogMessage.Log(LogMessage.MessageType.Success, ref response);
                        else
                            LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        response.Result = false;
                        response.ResultDescription.Append(ex.Message);
                    }
                }
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse CommandNotValidJBODSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("CommandNotValidJBODSetBladeMezzPassThroughMode", 24168);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns CommandNotValidForBlade for JBOD. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify CommandNotValidForBlade
            setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, "true");
            LogMessage.Message = string.Format(
                    "{0}: Command for JBOD location returned completion code {1} and blade Id {2}",
                    this.currentApi, setBladeMezzResponse.completionCode, bladeId);

            if (setBladeMezzResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse FailureForEmptySetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForEmptySetBladeMezzPassThroughMode", 24169);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Failure for Empty Location. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify Failure
            setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, "true");
            LogMessage.Message = string.Format(
                    "{0}: Command failed for empty location with completion code {1} and blade Id {2}",
                    this.currentApi, setBladeMezzResponse.completionCode, bladeId);

            if (setBladeMezzResponse.completionCode != CompletionCode.Failure)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse FailureForSoftOffSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForSoftOffSetBladeMezzPassThroughMode", 27172);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Failure for Server Blade Soft-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Soft-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetBladeOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Hard-powered On blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

                // Call API and verify Failure
                setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, "true");
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is soft-powered off",
                        this.currentApi, setBladeMezzResponse.completionCode, bladeId);

                if (setBladeMezzResponse.completionCode != CompletionCode.Failure)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            // Code clean-up. Soft-power on blade
            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse DevicePoweredOffForHardOffSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("DevicePoweredOffForHardOffSetBladeMezzPassThroughMode", 24176);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Failure for Server Blade Hard-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Hard-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetPowerOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power Off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Hard-powered Off blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOffSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

                // Call API and verify Failure
                setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, "true");
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is hard-powered Off",
                        this.currentApi, setBladeMezzResponse.completionCode, bladeId);

                if (setBladeMezzResponse.completionCode != CompletionCode.DevicePoweredOff)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            // Test CleanUp: Hard-Power On and Soft-Power On Blade
            bladeResponse = this.TestChannelContext.SetPowerOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power On server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Hard-powered On blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Soft-powered ON blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse ParameterOutOfRangeBladeIdSetBladeMezzPassThroughMode()
        {
            TestCase testCase = new TestCase("ParameterOutOfRangeBladeIdSetBladeMezzPassThroughMode", 24178);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns ParameterOutOfRange for Invalid BladeId. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            foreach (int invalidValue in Enum.GetValues(typeof(InvalidBladeId)))
            {
                setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(invalidValue, "true");
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} for invalid blade Id {2}",
                        this.currentApi, setBladeMezzResponse.completionCode, invalidValue);

                if (setBladeMezzResponse.completionCode != CompletionCode.ParameterOutOfRange)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse InvalidRequestForInvalidPassThroughSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("InvalidRequestForInvalidPassThroughSetBladeMezzPassThroughMode", 24197);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Status Description InvalidRequest for Invalid PassThroughModeEnabled. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            Dictionary<string, string> InvalidPassThroughModeEnabled = new Dictionary<string, string>();

            InvalidPassThroughModeEnabled.Add("min64", "-9223372036854775808");
            InvalidPassThroughModeEnabled.Add("max64", "9223372036854775807");
            InvalidPassThroughModeEnabled.Add("invalidString", "abc123_!&");
            InvalidPassThroughModeEnabled.Add("emptyString", string.Empty);
            InvalidPassThroughModeEnabled.Add("invalidTrue1", "ttrue");
            InvalidPassThroughModeEnabled.Add("invalidTrue2", "frue");
            InvalidPassThroughModeEnabled.Add("invalidFalse1", "ffalse");
            InvalidPassThroughModeEnabled.Add("invalidFalse2", "fazse");
            InvalidPassThroughModeEnabled.Add("nullString", null);

            // test SetBladeMezzPassThroughMode passThroughModeEnabled parameter for each invalid value
            foreach (string invalidValue in InvalidPassThroughModeEnabled.Values)
            {
                setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, invalidValue);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} and status description {2} for invalid passThroughModeEnabled {3} for bladeId {4}",
                        currentApi, setBladeMezzResponse.completionCode, setBladeMezzResponse.statusDescription, invalidValue, bladeId);

                if (setBladeMezzResponse.completionCode == CompletionCode.Failure &&
                    setBladeMezzResponse.statusDescription == "Invalid Request")
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse VerifySuccessWithCapitalizationOfPassThroughSetBladeMezzPassThroughMode(int bladeId)
        {
            TestCase testCase = new TestCase("VerifySuccessWithCapitalizationOfPassThroughSetBladeMezzPassThroughMode", 24201);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Completion Code Success for PassThroughModeEnabled With Capitalization. WorkItemIds: 24200 and {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeResponse setBladeMezzResponse = new BladeResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            Dictionary<string, string> CapitalizedPassThroughModeEnabled = new Dictionary<string, string>();

            CapitalizedPassThroughModeEnabled.Add("Value1", "true");
            CapitalizedPassThroughModeEnabled.Add("Value2", "false");
            CapitalizedPassThroughModeEnabled.Add("Value3", "TRUE");
            CapitalizedPassThroughModeEnabled.Add("Value4", "FALSE");
            CapitalizedPassThroughModeEnabled.Add("Value5", "tRue");
            CapitalizedPassThroughModeEnabled.Add("Value6", "faLse");

            foreach (string capitalizedValue in CapitalizedPassThroughModeEnabled.Values)
            {
                setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, capitalizedValue);

                LogMessage.Message = string.Format(
                        "{0}: Command returned Completion Code {1} for PassThroughModeEnabled value {2} for blade Id {3}",
                        this.currentApi, setBladeMezzResponse.completionCode, capitalizedValue, bladeId);

                if (setBladeMezzResponse.completionCode != CompletionCode.Success)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        #endregion

        #region GetBladeMezzAssetInfo Test Cases

        /// <summary>
        /// Test Command: GetBladeMezzAssetInfo
        /// The test case verifies: 
        /// all groups can execute command
        /// CommandNotValidForBlade for JBOD
        /// Failure for empty slot
        /// Failure for blade soft-powered off
        /// DevicePoweredOff for blade hard-powered off
        /// ParameterOutOfRange for invalid bladeId 
        /// Command outputs correct Fru content
        /// Command outputs same Fru content for blade hard-power cycled
        /// Command outputs same Fru content for blade soft-power cycled
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse GetBladeMezzAssetInfoTest(string specifiedBladeLocations)
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            this.currentApi = "GetBladeMezzAssetInfo";

            try
            {
                // Get all present blade server, JBOD, and empty locations    
                this.ServerLocations = this.GetServerLocations();
                this.JbodLocations = this.GetJbodLocations();
                this.EmptyLocations = this.GetEmptyLocations();

                // Check if any specific blade locations are to be tested
                CheckSpecifiedBladeLocations(specifiedBladeLocations);

                // Run test cases not specific to blade locations
                VerifyResponseAndAddFailureDescription(
                    ParameterOutOfRangeBladeIdGetBladeMezzAssetInfo(), response);

                // Run test cases
                for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                {
                    // Run test cases for every server blade location
                    if (ServerLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            AllGroupsCanExecuteGetBladeMezzAssetInfo(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            FailureForSoftOffGetBladeMezzAssetInfo(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            DevicePoweredOffForHardOffGetBladeMezzAssetInfo(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            CorrectFruContentForGetBladeMezzAssetInfo(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            FruContentMatchesAfterHardCycleForGetBladeMezzAssetInfo(bladeId), response);
                        VerifyResponseAndAddFailureDescription(
                            FruContentMatchesAfterSoftCycleForGetBladeMezzAssetInfo(bladeId), response);
                    }
                    // Run test cases for every JBOD location
                    else if (JbodLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            CommandNotValidJBODGetBladeMezzAssetInfo(bladeId), response);
                    }
                    // Run test cases for every empty location
                    else if (EmptyLocations.Contains(bladeId))
                    {
                        VerifyResponseAndAddFailureDescription(
                            FailureForEmptyGetBladeMezzAssetInfo(bladeId), response);
                    }
                }

            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            CmTestLog.End(response.Result);
            return response;
        }

        private TestResponse AllGroupsCanExecuteGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("AllGroupsCanExecuteGetBladeMezzAssetInfo", 27197);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Can Be Executed By All Users. WorkItemId: {1}.!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Loop through different user types 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                BladeMessAssetInfoResponse getMezzAssetInfoResponse = new BladeMessAssetInfoResponse();

                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                // Get Pass Through Mode and verify
                getMezzAssetInfoResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);

                LogMessage.Message = string.Format(
                        "{0}: Command returned completion code {1} for user {2} and blade Id {3}",
                        this.currentApi, getMezzAssetInfoResponse.completionCode,
                        Enum.GetName(typeof(WCSSecurityRole), roleId), bladeId);

                if (getMezzAssetInfoResponse.completionCode != CompletionCode.Success)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse FailureForSoftOffGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForSoftOffGetBladeMezzAssetInfo", 27200);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Failure for Server Blade Soft-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Soft-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetBladeOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Soft-powered off blade {0}. Thread sleeping for {1} seconds...",
                   bladeId, CmConstants.BladePowerOffSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

                // Call API and verify Failure
                getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
                if (getMezzAssetResponse.completionCode != CompletionCode.Failure)
                {
                    LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is soft-powered off",
                        this.currentApi, getMezzAssetResponse.completionCode, bladeId);
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                }
                else
                {
                    LogMessage.Message = string.Format(
                        "{0}: Command successfully returns completion code Failure after blade {1} is soft-powered off",
                        this.currentApi, bladeId);
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
                }
            }

            // Code clean-up. Soft-power on blade
            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Soft-powered on blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse DevicePoweredOffForHardOffGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForHardOffGetBladeMezzAssetInfo", 27452);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns DevicePoweredOff for Server Blade Hard-Powered Off. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Hard-Power Off server blade
            BladeResponse bladeResponse = this.TestChannelContext.SetPowerOff(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power Off server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            }
            else
            {
                CmTestLog.Info(string.Format("Hard-powered Off blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOffSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

                // Call API and verify DevicePoweredOff
                getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} after blade {2} is hard-powered Off",
                        this.currentApi, getMezzAssetResponse.completionCode, bladeId);
                if (getMezzAssetResponse.completionCode != CompletionCode.DevicePoweredOff)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            // Test CleanUp: Hard-Power On and Soft-Power On Blade
            bladeResponse = this.TestChannelContext.SetPowerOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to hard-power On server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Hard-powered On blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            bladeResponse = this.TestChannelContext.SetBladeOn(bladeId);
            if (bladeResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Message = string.Format(
                    "{0}: Unable to soft-power on server blade {1}",
                    this.currentApi, bladeId);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            CmTestLog.Info(string.Format("Soft-powered on blade {0}. Thread sleeping for {1} seconds...",
                bladeId, CmConstants.BladePowerOnSeconds));
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse ParameterOutOfRangeBladeIdGetBladeMezzAssetInfo()
        {
            TestCase testCase = new TestCase("ParameterOutOfRangeBladeIdGetBladeMezzAssetInfo", 27201);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns ParameterOutOfRange for Invalid BladeId. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            foreach (int invalidValue in Enum.GetValues(typeof(InvalidBladeId)))
            {
                getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(invalidValue);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} for invalid Blade Id {2}",
                        this.currentApi, getMezzAssetResponse.completionCode, invalidValue);

                if (getMezzAssetResponse.completionCode != CompletionCode.ParameterOutOfRange)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse CommandNotValidJBODGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("CommandNotValidJBODGetBladeMezzAssetInfo", 27198);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns CommandNotValidForBlade for JBOD. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify CommandNotValidForBlade
            getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
            LogMessage.Message = string.Format(
                    "{0}: Command for JBOD location returned completion code {1} and blade Id {2}",
                    this.currentApi, getMezzAssetResponse.completionCode, bladeId);

            if (getMezzAssetResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse FailureForEmptyGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("FailureForEmptyGetBladeMezzAssetInfo", 27199);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Failure for Empty Location. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify Failure
            getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
            LogMessage.Message = string.Format(
                "{0}: Command for empty location with completion code {1} and blade Id {2}",
                    this.currentApi, getMezzAssetResponse.completionCode, bladeId);

            if (getMezzAssetResponse.completionCode != CompletionCode.Failure)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse CorrectFruContentForGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("CorrectFruContentForGetBladeMezzAssetInfo", 27202);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Correct Fru Information. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse getMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Call API and verify FRU output is correct
            getMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
            VerifyMezzFruContent(getMezzAssetResponse, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse FruContentMatchesAfterHardCycleForGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("FruContentMatchesAfterHardCycleForGetBladeMezzAssetInfo", 27449);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Correct Fru Information After Hard-Power Cycle. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse previousGetMezzAssetResponse = new BladeMessAssetInfoResponse();
            BladeMessAssetInfoResponse currentGetMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            previousGetMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);

            // Hard-Power Off Blade
            BladeResponse setPowerResponse = new BladeResponse();
            setPowerResponse = this.TestChannelContext.SetPowerOff(bladeId);
            LogMessage.Message = string.Format("{0}: Hard-Powered Off Blade {1}",
                this.currentApi, bladeId);
            if (setPowerResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref  response);

            // Thread Sleep
            LogMessage.Message = string.Format("Blade Powered Off. Thread sleeping for {0} seconds...",
                CmConstants.BladePowerOffSeconds);
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds));

            // Hard-Power On Blade
            setPowerResponse = this.TestChannelContext.SetPowerOn(bladeId);
            LogMessage.Message = string.Format("{0}: Hard-Powered On Blade {1}",
                this.currentApi, bladeId);
            if (setPowerResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref  response);

            // Thread Sleep
            LogMessage.Message = string.Format("Blade Powered On. Thread sleeping for {0} seconds...",
                CmConstants.BladePowerOnSeconds);
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            // Get Fru Content after Hard-Power cycle
            currentGetMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
            LogMessage.Message = string.Format(
                "{0}: Command returns completionCode {1} for blade {2}",
                this.currentApi, currentGetMezzAssetResponse.completionCode, bladeId);
            if (currentGetMezzAssetResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Verify Fru Contents are same as before
            VerifyFruStringsMatch(previousGetMezzAssetResponse.bladeNumber.ToString(),
                currentGetMezzAssetResponse.bladeNumber.ToString(), ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaManufactureName,
                currentGetMezzAssetResponse.productAreaManufactureName, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaProductName,
                currentGetMezzAssetResponse.productAreaProductName, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaPartModelNumber,
                currentGetMezzAssetResponse.productAreaPartModelNumber, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaProductVersion,
                currentGetMezzAssetResponse.productAreaProductVersion, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaSerialNumber,
                currentGetMezzAssetResponse.productAreaSerialNumber, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaAssetTag,
                currentGetMezzAssetResponse.productAreaAssetTag, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse FruContentMatchesAfterSoftCycleForGetBladeMezzAssetInfo(int bladeId)
        {
            TestCase testCase = new TestCase("FruContentMatchesAfterSoftCycleForGetBladeMezzAssetInfo", 27450);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Returns Same Fru Information After Soft-Power Cycle. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            BladeMessAssetInfoResponse previousGetMezzAssetResponse = new BladeMessAssetInfoResponse();
            BladeMessAssetInfoResponse currentGetMezzAssetResponse = new BladeMessAssetInfoResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            previousGetMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);

            // Soft-Power Cycle Blade
            BladeResponse setPowerResponse = this.TestChannelContext.SetBladeActivePowerCycle(bladeId, (int)CmConstants.OffTimeSec);
            LogMessage.Message = string.Format("{0}: Soft-Power Cycled Blade {1}",
                this.currentApi, bladeId);
            if (setPowerResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref  response);

            // Thread Sleep
            LogMessage.Message = string.Format("Blade Soft-Power Cycling. Thread sleeping for {0} seconds...",
                (CmConstants.BladePowerOffSeconds + CmConstants.BladePowerOnSeconds));
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOffSeconds + CmConstants.BladePowerOnSeconds));

            // Get Fru Content after Soft-Power cycle
            currentGetMezzAssetResponse = this.TestChannelContext.GetBladeMezzAssetInfo(bladeId);
            LogMessage.Message = string.Format(
                "{0}: Command returns completionCode {1} for blade {2}",
                this.currentApi, currentGetMezzAssetResponse.completionCode, bladeId);
            if (currentGetMezzAssetResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Verify Fru Contents are same as before
            VerifyFruStringsMatch(previousGetMezzAssetResponse.bladeNumber.ToString(),
                currentGetMezzAssetResponse.bladeNumber.ToString(), ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaManufactureName,
                currentGetMezzAssetResponse.productAreaManufactureName, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaProductName,
                currentGetMezzAssetResponse.productAreaProductName, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaPartModelNumber,
                currentGetMezzAssetResponse.productAreaPartModelNumber, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaProductVersion,
                currentGetMezzAssetResponse.productAreaProductVersion, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaSerialNumber,
                currentGetMezzAssetResponse.productAreaSerialNumber, ref response);
            VerifyFruStringsMatch(previousGetMezzAssetResponse.productAreaAssetTag,
                currentGetMezzAssetResponse.productAreaAssetTag, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        #endregion

        #region Helper Methods

        private void CallAndVerifySetBladeMezzPassThroughMode(int bladeId, ref TestResponse response,
            string passThroughModeEnabled)
        {
            BladeResponse setBladeMezzResponse = new BladeResponse();

            setBladeMezzResponse = this.TestChannelContext.SetBladeMezzPassThroughMode(bladeId, passThroughModeEnabled);
            LogMessage.Message = string.Format(
                    "{0}: Command returns completion code {1} for blade Id {2} and passThroughModeEnabled {3}",
                    "SetBladeMezzPassThroughMode", setBladeMezzResponse.completionCode, bladeId, passThroughModeEnabled);

            if (setBladeMezzResponse.completionCode != CompletionCode.Success)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
        }

        private void CallAndVerifyGetBladeMezzPassThroughMode(int bladeId, ref TestResponse response,
            string passThroughModeEnabled)
        {
            BladeMezzPassThroughModeResponse getBladeMezzResponse = new BladeMezzPassThroughModeResponse();

            getBladeMezzResponse = this.TestChannelContext.GetBladeMezzPassThroughMode(bladeId);
            LogMessage.Message = string.Format(
                "{0}: Command returns completion code {1} and passThroughModeEnabled {2} for blade Id {3}",
                "GetBladeMezzPassThroughMode", getBladeMezzResponse.completionCode, 
                getBladeMezzResponse.passThroughModeEnabled, bladeId);

            if (getBladeMezzResponse.completionCode != CompletionCode.Success)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else if (passThroughModeEnabled != null &&
                getBladeMezzResponse.passThroughModeEnabled.ToString().ToLower() != passThroughModeEnabled)
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
        }

        private void CheckSpecifiedBladeLocations(string specifiedLocations)
        {
            if (specifiedLocations != string.Empty)
            {
                ServerLocations = specifiedLocations
                                                    .Split(' ')
                                                    .Select(x => Int32.Parse(x))
                                                    .Where(x => ServerLocations.Contains(x))
                                                    .ToArray();
            }
        }

        private void VerifyResponseAndAddFailureDescription(TestResponse bladeResp, TestResponse response)
        {
            if (!bladeResp.Result)
            {
                response.ResultDescription.Append(bladeResp.ResultDescription);
                response.Result = false;
            }
        }

        private void VerifyMezzFruContent(BladeMessAssetInfoResponse getMezzAssetResponse, ref TestResponse response)
        {
            LogMessage.Message = string.Format(
                    "{0}: Command for Blade {1} returns completionCode {2}",
                    this.currentApi, getMezzAssetResponse.bladeNumber, getMezzAssetResponse.completionCode);

            if (getMezzAssetResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Product Area Manufacturer Name
            VerifyFruStringsMatch("Microsoft", getMezzAssetResponse.productAreaManufactureName, ref response);

            // Product Area Product Name
            VerifyFruStringsMatch("PPFPGA", getMezzAssetResponse.productAreaProductName, ref response);

            // Product Area Part Model Number
            VerifyFruStringsMatch("X900563-001", getMezzAssetResponse.productAreaPartModelNumber, ref response);

            // Product Area Product Version
            VerifyFruStringsMatch("2.0", getMezzAssetResponse.productAreaProductVersion, ref response);

            // Product Area Product Serial Number
            VerifyFruStringLength("Product Serial Number", getMezzAssetResponse.productAreaSerialNumber, 14,
                ref response);

            // Product Area Asset Tag
            VerifyFruStringLength("Product Asset Tag", getMezzAssetResponse.productAreaAssetTag, 7,
                ref response);
        }

        private void VerifyFruStringsMatch(string expectedValue, string actualValue, ref TestResponse response)
        {
            bool stringMatch;

            LogMessage.Message = string.Format(
                "{0}: Expected: {1} - Actual: {2}",
                this.currentApi, expectedValue, actualValue);

            stringMatch = expectedValue.Equals(actualValue);

            if (stringMatch)
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);

            response.Result &= stringMatch;
        }

        private void VerifyFruStringLength(string fruStringName, string fruStringValue,
            int expectedLength, ref TestResponse response)
        {
            bool isCorrectLength;

            LogMessage.Message = string.Format(
                "{0}: Expected {1} characters for {2} - Actual: '{3}' with length {4}",
                this.currentApi, expectedLength, fruStringName, fruStringValue,
                fruStringValue.Length);

            isCorrectLength = fruStringValue.Length <= expectedLength ? true : false;

            if (isCorrectLength)
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);

            response.Result &= isCorrectLength;
        }

        #endregion
    }
}
