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
    using System.Linq;
    using System.Threading;
    using Microsoft.GFS.WCS.Contracts;

    internal class PsuFirmwareCommands : CommandBase
    {
        #region Constructors

        internal PsuFirmwareCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts)
            : base(channel, TestChannelContexts)
        {
            lesPsusInChassis = ConfigurationManager.AppSettings["LesOrNonLesPsus"].ToString();
            priLesFwFilePath = ConfigurationManager.AppSettings["PriLesFwFilePath"].ToString();
            priNonLesFwFilePath = ConfigurationManager.AppSettings["PriNonLesFwFilePath"].ToString();
            secLesFwFilePath = ConfigurationManager.AppSettings["SecLesFwFilePath"].ToString();
            secNonLesFwFilePath = ConfigurationManager.AppSettings["SecNonLesFwFilePath"].ToString();
        }

        internal PsuFirmwareCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts, Dictionary<string, string> TestEnvironment)
            : base(channel, TestChannelContexts, TestEnvironment)
        {
            lesPsusInChassis = ConfigurationManager.AppSettings["LesOrNonLesPsus"].ToString();
            priLesFwFilePath = ConfigurationManager.AppSettings["PriLesFwFilePath"].ToString();
            priNonLesFwFilePath = ConfigurationManager.AppSettings["PriNonLesFwFilePath"].ToString();
            secLesFwFilePath = ConfigurationManager.AppSettings["SecLesFwFilePath"].ToString();
            secNonLesFwFilePath = ConfigurationManager.AppSettings["SecNonLesFwFilePath"].ToString();
        }

        #endregion

        #region Constants

        private static bool primaryImage = true;
        private static bool secondaryImage = false;

        #endregion

        #region Helper Variables

        private string lesPsusInChassis { get; set; }
        private string priLesFwFilePath { get; set; }
        private string secLesFwFilePath { get; set; }
        private string priNonLesFwFilePath { get; set; }
        private string secNonLesFwFilePath { get; set; }

        private string currentApi { get; set; }

        #endregion

        #region Helper Classes

        private enum InvalidPsuId : int
        {
            min32 = Int32.MinValue,
            max32 = Int32.MaxValue,
            zero = 0,
            mediumNegative = -3,
            mediumPositive = 7
        };

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

        private class UpdatePsu
        {
            public PsuFirmwareStatus psuFwStatus;
            public ChassisResponse psuFwUpdateResponse;
            public string psuFwFilePath;
            public bool primaryImage;
            public bool passed;
            public int primaryUpdateSuccessCount;
            public int secondaryUpdateSuccessCount;
            public int index;

            public UpdatePsu(int idx)
            {
                psuFwStatus = new PsuFirmwareStatus();
                psuFwUpdateResponse = new ChassisResponse();
                psuFwFilePath = string.Empty;
                primaryImage = false;
                passed = true;
                primaryUpdateSuccessCount = 0;
                secondaryUpdateSuccessCount = 0;
                index = idx;
            }

            public UpdatePsu(int idx, string fwFilePath, bool priImage)
            {
                psuFwStatus = new PsuFirmwareStatus();
                psuFwUpdateResponse = new ChassisResponse();
                psuFwFilePath = fwFilePath;
                primaryImage = priImage;
                passed = true;
                primaryUpdateSuccessCount = 0;
                secondaryUpdateSuccessCount = 0;
                index = idx;
            }
        }

        #endregion

        #region GetPsuFirmwareStatus Test Cases

        /// <summary>
        /// Test Command: GetPsuFirmwareStatus
        /// The test case verifies: 
        /// all groups can execute command
        /// ParameterOutOfRange for invalid psuId 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse GetPsuFirmwareStatusTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            this.currentApi = "GetPsuFirmwareStatus";

            try
            {
                // Run test cases that don't require valid psu Id
                VerifyResponseAndAddFailureDescription(
                    ParameterOutOfRangePsuIdGetPsuFirmwareStatus(), response);

                // Run test cases that require valid psu Id        
                VerifyResponseAndAddFailureDescription(
                    AllGroupsCanExecuteGetPsuFirmwareStatus(), response);

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

        private TestResponse AllGroupsCanExecuteGetPsuFirmwareStatus()
        {
            TestCase testCase = new TestCase("AllGroupsCanExecuteGetPsuFirmwareStatus", 22696);

            CmTestLog.Info(string.Format("\n !!!!!!Verifying {0} Can Be Executed By All Users. WorkItemId: {1}.!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random Psu
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0]);

            // Loop through different user types 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                // Get Pass Through Mode and verify
                psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);

                LogMessage.Message = string.Format(
                        "{0}: Command returned completion code {1} for user {2} and psu Id {3}",
                        this.currentApi, psu.psuFwStatus.completionCode,
                        Enum.GetName(typeof(WCSSecurityRole), roleId), psu.index);

                if (psu.psuFwStatus.completionCode != CompletionCode.Success)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);

                // Verify Correct psuId returned by GetPsuFirmwareStatus
                LogMessage.Message = string.Format(
                    "{0}: Command returned psu Id {0} for expected psu Id {1}",
                    this.currentApi, psu.psuFwStatus.id, psu.index);

                if (psu.psuFwStatus.id != psu.index)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse ParameterOutOfRangePsuIdGetPsuFirmwareStatus()
        {
            TestCase testCase = new TestCase("ParameterOutOfRangePsuIdGetPsuFirmwareStatus", 22804);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns ParameterOutOfRange for Invalid PsuId. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            PsuFirmwareStatus getPsuFwResponse = new PsuFirmwareStatus();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            foreach (int invalidValue in Enum.GetValues(typeof(InvalidPsuId)))
            {
                getPsuFwResponse = this.TestChannelContext.GetPSUFirmwareStatus(invalidValue);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} for invalid Psu Id {2}",
                        this.currentApi, getPsuFwResponse.completionCode, invalidValue);

                if (getPsuFwResponse.completionCode != CompletionCode.ParameterOutOfRange)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        #endregion

        #region UpdatePsuFirmwareStatus Test Cases

        /// <summary>
        /// Test Command: UpdatePsuFirmware
        /// The test case verifies: 
        /// Command Updates Primary Fw when primaryImage true, secondary Fw when false
        /// Only Admin users can execute the command
        /// Psu does not output power during update
        /// ParameterOutOfRange completion code for invalid Psu Id values
        /// 
        /// Les Test Cases:
        /// Psu Fw file updates for primary image
        /// Psu Fw file updates for secondary image
        /// Psu Fw file updates primary image for multiple Psus
        /// Psu Fw file updates primary and secondary image for multiple Psus
        /// Psu Fw file updates secondary image for multiple Psus
        /// Psu Fw file updates for primary or secondary image but not both
        /// Invalid Psu Fw file fails update
        /// ForceEmersonPsu App Config Key updates image for previously failed Psu due to CM service restart
        /// 
        /// Non-Les Test Cases:
        /// Psu Fw file updates for primary image
        /// Psu Fw file updates for secondary image
        /// Psu Fw file updates primary image for multiple Psus
        /// Psu Fw file updates primary and secondary image for multiple Psus
        /// Psu Fw file updates secondary image for multple Psus
        /// Psu Fw file updates for primary or secondary image but not both
        /// Invalid Psu Fw file fails update
        /// ForceEmersonPsu App Config Key updates image for prebiously failed Psy due to CM service restart
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse UpdatePsuFirmwareTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();
            this.currentApi = "UpdatePsuFirmware";

            try
            {

                // Run test cases
                VerifyResponseAndAddFailureDescription(
                    SuccessfulPriSecUpdateForPrimaryImageTrueFalseForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    PsuDoesNotOutputPowerForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    OnlyAdminCanExecuteUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    ParameterOutOfRangePsuIdUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulPriFwUpdateForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulSecFwUpdateForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulMultiplePriFwUpdatesForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulMultipleSecFwUpdatesForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulMultiplePriSecFwUpdatesForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    SuccessfulPriOrSecNotBothFwUpdateForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    InvalidPsuFwFileUpdateFailForUpdatePsuFirmware(), response);
                VerifyResponseAndAddFailureDescription(
                    ForceEmersonPsuFwUpdateAfterCmServiceRestartForUpdatePsuFirmware(), response);
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

        private TestResponse SuccessfulPriSecUpdateForPrimaryImageTrueFalseForUpdatePsuFirmware()
        {
            TestCase testCase = new TestCase("SuccessfulPriSecUpdateForPrimaryImageTrueFalseForUpdatePsuFirmware", 22711);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Updates Primary Image When PrimaryImage True, Secondary Image When False. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Ids
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(2, ref psuIds, ref response))
                return response;

            // Initialize multiple UpdatePsu elements and add to psuList
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                psuList.Add(new UpdatePsu(psuIds[0], this.priLesFwFilePath, primaryImage));
                psuList.Add(new UpdatePsu(psuIds[1], this.secLesFwFilePath, secondaryImage));
            }
            else
            {
                psuList.Add(new UpdatePsu(psuIds[0], this.priNonLesFwFilePath, primaryImage));
                psuList.Add(new UpdatePsu(psuIds[1], this.secNonLesFwFilePath, secondaryImage));
            }

            // Verify Psu Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse PsuDoesNotOutputPowerForUpdatePsuFirmware()
        {
            TestCase testCase = new TestCase("PsuDoesNotOutputPowerForUpdatePsuFirmware", 22840);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Causes Psu To Not Output Power During Fw Update. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random Psu
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], this.priLesFwFilePath, primaryImage);

            // Run test case using WcsCmAdmin
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                    "{0}: GetPsuFirmwareStatus for PSU {1} before update - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            // Verify PSU is available for update
            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                psu.psuFwStatus.fwUpdateStatus == "InProgress")
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Update PSU
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index,
                    psu.psuFwFilePath, psu.primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 1 minute 
            LogMessage.Message = "Thread sleeping for 1 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(1));

            // Check Psu Power Output is 0 using GetChassisHealth
            ChassisHealthResponse getChassisHealthResponse = new ChassisHealthResponse();
            getChassisHealthResponse = this.TestChannelContext.GetChassisHealth(false, true, false, false);
            PsuInfo psuHealth = getChassisHealthResponse.psuInfoCollection
                .FirstOrDefault(x => x.id == psu.index);

            LogMessage.Message = string.Format(
                "{0}: GetChassisHealth for Psu {1} - completionCode {2} and powerOut {3}",
                this.currentApi, psuHealth.id, getChassisHealthResponse.completionCode,
                psuHealth.powerOut);

            if (getChassisHealthResponse.completionCode == CompletionCode.Success &&
                psuHealth.powerOut == 0)
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse OnlyAdminCanExecuteUpdatePsuFirmware()
        {
            TestCase testCase = new TestCase("OnlyAdminCanExecuteUpdatePsuFirmware", 22713);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Can Only Be Executed By WcsCmAdmin Users. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random Psu
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0]);

            // Loop through different user types 
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                try
                {
                    string fwFilePath = lesPsusInChassis == "1" ? priLesFwFilePath : priNonLesFwFilePath;
                    psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index, fwFilePath, primaryImage);

                    LogMessage.Message = string.Format(
                            "{0}: Command returns completion code {1} for user {2} for psu Id {3}",
                            currentApi, psu.psuFwUpdateResponse.completionCode,
                            Enum.GetName(typeof(WCSSecurityRole), roleId), psu.index);

                    if (psu.psuFwUpdateResponse.completionCode == CompletionCode.Success &&
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
                        LogMessage.Message = string.Format("{0}: Command returned Bad Request for user {1} for psuId {2}",
                            currentApi, Enum.GetName(typeof(WCSSecurityRole), roleId), psu.index);

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

        private TestResponse ParameterOutOfRangePsuIdUpdatePsuFirmware()
        {
            TestCase testCase = new TestCase("ParameterOutOfRangePsuIdUpdatePsuFirmware", 22773);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns ParameterOutOfRange for Invalid PsuId. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            ChassisResponse updatePsuFwResponse = new ChassisResponse();
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];
            string fwFilePath = lesPsusInChassis == "1" ? priLesFwFilePath : priNonLesFwFilePath;

            foreach (int invalidValue in Enum.GetValues(typeof(InvalidPsuId)))
            {
                updatePsuFwResponse = this.TestChannelContext.UpdatePSUFirmware(invalidValue, fwFilePath, primaryImage);
                LogMessage.Message = string.Format(
                        "{0}: Command returns completion code {1} for invalid Psu Id {2}",
                        this.currentApi, updatePsuFwResponse.completionCode, invalidValue);

                if (updatePsuFwResponse.completionCode != CompletionCode.ParameterOutOfRange)
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
            }

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulPriFwUpdateForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22716;
                fwFilePath = this.priLesFwFilePath;
            }
            else
            {
                testCaseId = 22714;
                fwFilePath = this.priNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("SuccessfulPriFwUpdateForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Successful Primary Psu Fw Image Update for Psu. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Id 
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], fwFilePath, primaryImage);

            // Initialize List to contain UpdatePsu elements
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            psuList.Add(psu);

            // Verify Primary Psu Fw Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulSecFwUpdateForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22745;
                fwFilePath = this.secLesFwFilePath;
            }
            else
            {
                testCaseId = 22746;
                fwFilePath = this.secNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("SuccessfulSecFwUpdateForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Successful Secondary Psu Fw Image Update for Psu. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Id 
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], fwFilePath, secondaryImage);

            // Initialize List to contain UpdatePsu elements
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            psuList.Add(psu);

            // Verify Secondary Psu Fw Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulMultiplePriFwUpdatesForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22756;
                fwFilePath = this.priLesFwFilePath;
            }
            else
            {
                testCaseId = 22766;
                fwFilePath = this.priNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("SuccessfulMultiplePriFwUpdatesForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Successful Primary Psu Fw Image Update for Multiple Psus. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Ids
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(3, ref psuIds, ref response))
                return response;

            // Initialize multiple UpdatePsu elements and add to psuList
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            foreach (int psuId in psuIds)
                psuList.Add(new UpdatePsu(psuId, fwFilePath, primaryImage));

            // Verify Primary Psu Fw Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulMultipleSecFwUpdatesForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22764;
                fwFilePath = this.secLesFwFilePath;
            }
            else
            {
                testCaseId = 22767;
                fwFilePath = this.secNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("SuccessfulMultipleSecFwUpdatesForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Successful Secondary Psu Fw Image Update for Multiple Psus. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Ids
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(3, ref psuIds, ref response))
                return response;

            // Initialize multiple UpdatePsu elements and add to psuList
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            foreach (int psuId in psuIds)
                psuList.Add(new UpdatePsu(psuId, fwFilePath, secondaryImage));

            // Verify Secondary Psu Fw Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulMultiplePriSecFwUpdatesForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            if (lesPsusInChassis == "1") // currently testing LES PSU
                testCaseId = 22765;
            else
                testCaseId = 22768;

            TestCase testCase = new TestCase("SuccessfulMultiplePriSecFwUpdatesForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Returns Successful Psu Fw Image Updates for Multiple Primary and Secondary Fw Psus. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Ids
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(3, ref psuIds, ref response))
                return response;

            // Initialize multiple UpdatePsu elements and add to psuList
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            if (lesPsusInChassis == "1")
            {
                psuList.Add(new UpdatePsu(psuIds[0], this.priLesFwFilePath, primaryImage));
                psuList.Add(new UpdatePsu(psuIds[1], this.secLesFwFilePath, secondaryImage));
                psuList.Add(new UpdatePsu(psuIds[2], this.priLesFwFilePath, primaryImage));
            }
            else
            {
                psuList.Add(new UpdatePsu(psuIds[0], this.priNonLesFwFilePath, primaryImage));
                psuList.Add(new UpdatePsu(psuIds[1], this.secNonLesFwFilePath, secondaryImage));
                psuList.Add(new UpdatePsu(psuIds[2], this.priNonLesFwFilePath, primaryImage));
            }

            // Verify Primary and Secondary Psu Fw Update
            VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, currentApi);
            return response;
        }

        private TestResponse SuccessfulPriOrSecNotBothFwUpdateForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22848;
                fwFilePath = this.priLesFwFilePath;
            }
            else
            {
                testCaseId = 22864;
                fwFilePath = this.priNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("SuccessfulMultiplePriSecFwUpdatesForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Only Allows Successful Primary Or Secondary Psu Fw Image To Be Updated, Not Both. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Id 
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], fwFilePath, primaryImage);

            // Run test case using WcsCmAdmin
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                    "{0}: GetPsuFirmwareStatus for PSU {1} before update - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            // Verify PSU is available for update
            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                psu.psuFwStatus.fwUpdateStatus == "InProgress")
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Update PSU
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index,
                    psu.psuFwFilePath, psu.primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 5 minutes 
            LogMessage.Message = "Thread sleeping for 5 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Verify PSU is still updating
            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                    "{0}: GetPsuFirmwareStatus for PSU {1} after 5 minutes - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            // Verify PSU is still updating
            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                psu.psuFwStatus.fwUpdateStatus != "InProgress")
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Attempt to update PSU with secondary image and verify Failure
            if (lesPsusInChassis == "1")
                psu.psuFwFilePath = this.secLesFwFilePath;
            else
                psu.psuFwFilePath = this.secNonLesFwFilePath;
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index, psu.psuFwFilePath, secondaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Failure)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 7 more minutes
            LogMessage.Message = "Thread sleeping for 7 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(7));

            // Verify successful Primary PSU Fw Update
            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                "{0}: GetPsuFWStatus for Psu {1} - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                    ((psu.psuFwStatus.fwUpdateStatus != "Success")
                    && (psu.psuFwStatus.fwUpdateStage != "Completed")))
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Verify Successful Secondary PSU Fw Update if Primary was successful
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            psuList.Add(psu);
            if (response.Result)
                VerifyPsuFwUpdate(ref psuList, ref response);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse InvalidPsuFwFileUpdateFailForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22896;
                fwFilePath = this.secLesFwFilePath;
            }
            else
            {
                testCaseId = 22909;
                fwFilePath = this.secNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("InvalidPsuFwFileUpdateFailForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Fails Update for Invalid Psu Fw File. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Id 
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], fwFilePath, primaryImage);

            // Run test case using WcsCmAdmin
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                    "{0}: GetPsuFirmwareStatus for PSU {1} before update - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            // Verify PSU is available for update
            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                psu.psuFwStatus.fwUpdateStatus == "InProgress")
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Update Psu with secondary fw image and true value for primaryImage
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index, psu.psuFwFilePath, primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 5 minutes 
            LogMessage.Message = "Thread sleeping for 5 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Verify Psu Fw Update has failed
            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                "{0}: GetPsuFirmwareStatus for PSU {1} - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            if (psu.psuFwStatus.completionCode == CompletionCode.Success &&
                psu.psuFwStatus.fwUpdateStatus == "Failed" &&
                psu.psuFwStatus.fwUpdateStage == "SendModelId")
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            // Update Psu with invalid psu image and true value for primaryImage
            string invalidFwFilePath = ConfigurationManager.AppSettings["InvalidPsuFwFilePath"].ToString();

            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index, invalidFwFilePath, primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 5 minutes 
            LogMessage.Message = "Thread sleeping for 5 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Verify Psu Fw Update has failed
            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                "{0}: GetPsuFirmwareStatus for PSU {1} - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            if (psu.psuFwStatus.completionCode == CompletionCode.Success &&
                psu.psuFwStatus.fwUpdateStatus == "Failed" &&
                psu.psuFwStatus.fwUpdateStage == "ExtractModelId")
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        private TestResponse ForceEmersonPsuFwUpdateAfterCmServiceRestartForUpdatePsuFirmware()
        {
            // Change test case Id and fwFilePath if testing Les or Non-Les Psus
            int testCaseId = -1;
            string fwFilePath;
            if (lesPsusInChassis == "1") // currently testing LES PSU
            {
                testCaseId = 22934;
                fwFilePath = this.priLesFwFilePath;
            }
            else
            {
                testCaseId = 22935;
                fwFilePath = this.priNonLesFwFilePath;
            }

            TestCase testCase = new TestCase("ForceEmersonPsuFwUpdateAfterCmServiceRestartForUpdatePsuFirmware", testCaseId);

            CmTestLog.Info(string.Format(
                "\n !!!!!!Verifying {0} Requires ForceEmersonPsu App Config Key for Psu Fw Update After CM Service Restart. WorkItemId: {1}!!!!!!",
                this.currentApi, testCase.WorkItemId));
            TestResponse response = new TestResponse();

            // Get random valid Psu Id
            int[] psuIds = null;
            if (!ReturnSingleOrMultipleRandomPsus(1, ref psuIds, ref response))
                return response;
            UpdatePsu psu = new UpdatePsu(psuIds[0], fwFilePath, primaryImage);

            // Initialize FRU Writes Remaining Dictionary KeyValue Pair
            Dictionary<string, string> forceEmersonPsuKeyValue = new Dictionary<string, string>
            {
                {"ForceEmersonPsu", "0"}
            };

            // configure app.config and restart CM
            ConfigureAppConfig(forceEmersonPsuKeyValue, false);
            RestartCmService(this.currentApi);

            // Run test case using WcsCmAdmin
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
            LogMessage.Message = string.Format(
                    "{0}: GetPsuFirmwareStatus for PSU {1} before update - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                    this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                    psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

            // Verify PSU is available for update
            if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                psu.psuFwStatus.fwUpdateStatus == "InProgress")
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Update PSU
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index,
                    psu.psuFwFilePath, psu.primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }
            else
                LogMessage.Log(LogMessage.MessageType.Success, ref response);

            // Sleep for 5 minutes 
            LogMessage.Message = "Thread sleeping for 5 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Restart CM Service
            LogMessage.Message = "Restarting CM Service";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            RestartCmService(this.currentApi);

            // Verify Psu Fw Update has failed
            psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index, psu.psuFwFilePath, psu.primaryImage);
            LogMessage.Message = string.Format(
                    "{0}: Psu {1} returned completion code {2} and status description {3}",
                    this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                    psu.psuFwUpdateResponse.statusDescription);

            if (psu.psuFwUpdateResponse.completionCode == CompletionCode.Failure &&
                psu.psuFwUpdateResponse.statusDescription == "UpdatePSUFirmware() only supported on Emerson PSU.")
                LogMessage.Log(LogMessage.MessageType.Success, ref response);
            else
            {
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return response;
            }

            // Configure ForceEmersonPsu App Config Key
            forceEmersonPsuKeyValue["ForceEmersonPsu"] = "1";
            ConfigureAppConfig(forceEmersonPsuKeyValue, false);
            RestartCmService(this.currentApi);

            // Verify ForceEmersonPsu allows for successful Psu Fw Update
            List<UpdatePsu> psuList = new List<UpdatePsu>();
            psuList.Add(psu);
            VerifyPsuFwUpdate(ref psuList, ref response);

            // Code Clean-Up: restore App.Config in CM
            ConfigureAppConfig(null, true);

            testCase.LogPassOrFail(ref response, this.currentApi);
            return response;
        }

        #endregion

        #region Stress Tests

        public bool UpdatePsuFirmwareStressTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "UpdatePsuFirmware";
            int numberOfCycles = Int32.Parse(ConfigurationManager.AppSettings["PsuStressCycleCount"].ToString());

            try
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

                // Get 4 random psu locations
                int[] psuIds = null;
                TestResponse response = new TestResponse();
                if (!ReturnSingleOrMultipleRandomPsus(4, ref psuIds, ref response))
                    return false;

                UpdatePsu psu1 = new UpdatePsu(psuIds[0]);
                UpdatePsu psu2 = new UpdatePsu(psuIds[1]);
                UpdatePsu psu3 = new UpdatePsu(psuIds[2]);
                UpdatePsu psu4 = new UpdatePsu(psuIds[3]);

                bool primaryOrSecondaryImage;
                string fwFilePath = string.Empty;
                string[] fwFilePaths = new string[2];

                if (lesPsusInChassis == "1")
                {
                    fwFilePaths[0] = priLesFwFilePath;
                    fwFilePaths[1] = secLesFwFilePath;
                }
                else
                {
                    fwFilePaths[0] = priNonLesFwFilePath;
                    fwFilePaths[1] = secNonLesFwFilePath;
                }

                for (int cycleCount = 0; cycleCount < numberOfCycles; cycleCount++)
                {
                    fwFilePath = fwFilePaths[cycleCount % 2];
                    primaryOrSecondaryImage = fwFilePath == fwFilePaths[0] ? primaryImage : secondaryImage;

                    psu1.passed = true;
                    psu2.passed = true;
                    psu3.passed = true;
                    psu4.passed = true;

                    // Sleep for 30 seconds before starting each test cycle
                    CmTestLog.Info(string.Format("\n{0}: Sleeping for 30 seconds before starting test cycle {1} ...",
                        currentApi, cycleCount));
                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    CmTestLog.Info(string.Format("{0}: Update Psu FW Test {1} for Psu {2}, Psu {3}, Psu {4}, Psu {5} for fwFilePath {6}",
                        currentApi, cycleCount, psu1.index, psu2.index, psu3.index, psu4.index, fwFilePath));

                    // Update Psu
                    psu1.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu1.index, fwFilePath, primaryOrSecondaryImage);
                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    // Update second Psu
                    psu2.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu2.index, fwFilePath, primaryOrSecondaryImage);
                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    // Update thrid Psu
                    psu3.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu3.index, fwFilePath, primaryOrSecondaryImage);
                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    // Update fourth Psu
                    psu4.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu4.index, fwFilePath, primaryOrSecondaryImage);

                    // Verify response
                    if (psu1.psuFwUpdateResponse.completionCode != CompletionCode.Success &&
                        psu2.psuFwUpdateResponse.completionCode != CompletionCode.Success &&
                        psu3.psuFwUpdateResponse.completionCode != CompletionCode.Success &&
                        psu4.psuFwUpdateResponse.completionCode != CompletionCode.Success)
                    {
                        CmTestLog.Failure(string.Format(
                            "{0}: Command returned Completion Code {1} for Psu {2}, Completion Code {3} for Psu {4}, Completion Code {5} for Psu {6}, Completion Code {7} for Psu {8}",
                            currentApi, psu1.psuFwUpdateResponse.completionCode, psu1.index, psu2.psuFwUpdateResponse.completionCode, psu2.index,
                            psu3.psuFwUpdateResponse.completionCode, psu3.index, psu4.psuFwUpdateResponse.completionCode, psu4.index));
                        allPassed = false;
                        continue;
                    }

                    // Sleep 5 minutes
                    CmTestLog.Info("Thread sleeping for 5 minutes..");
                    Thread.Sleep(TimeSpan.FromMinutes(5));

                    // GetPsuFwStatus and verify test is progressing correctly
                    PrintGetPsuFwStatus(ref psu1, true, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu2, true, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu3, true, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu4, true, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);

                    if (!(psu1.passed || psu1.passed || psu3.passed || psu4.passed))
                    {
                        CmTestLog.Info("\n");
                        continue;
                    }

                    // Sleep 7 minutes
                    CmTestLog.Info("Thread sleeping for 7 minutes..");
                    Thread.Sleep(TimeSpan.FromMinutes(7));

                    // GetPsuFwStatus and verify test completed successfully
                    PrintGetPsuFwStatus(ref psu1, false, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu2, false, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu3, false, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);
                    PrintGetPsuFwStatus(ref psu4, false, primaryOrSecondaryImage, currentApi, fwFilePath, ref allPassed);

                    CmTestLog.Info("\n");

                }

                if (psu1.primaryUpdateSuccessCount != 0 || psu2.primaryUpdateSuccessCount != 0 ||
                    psu3.primaryUpdateSuccessCount != 0 || psu4.primaryUpdateSuccessCount != 0)
                {
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Primary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu1.primaryUpdateSuccessCount, psu1.index, fwFilePaths[0]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Primary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu2.primaryUpdateSuccessCount, psu2.index, fwFilePaths[0]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Primary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu3.primaryUpdateSuccessCount, psu3.index, fwFilePaths[0]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Primary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu4.primaryUpdateSuccessCount, psu4.index, fwFilePaths[0]));
                }

                if (psu1.secondaryUpdateSuccessCount != 0 || psu2.secondaryUpdateSuccessCount != 0 ||
                    psu3.secondaryUpdateSuccessCount != 0 || psu4.secondaryUpdateSuccessCount != 0)
                {
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Secondary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu1.secondaryUpdateSuccessCount, psu1.index, fwFilePaths[1]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Secondary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu2.secondaryUpdateSuccessCount, psu2.index, fwFilePaths[1]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Secondary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu3.secondaryUpdateSuccessCount, psu3.index, fwFilePaths[1]));
                    CmTestLog.Info(string.Format("{0}: {1} Updates successful for Secondary Les Fw Update for Psu {2}\nfw: {3}",
                        currentApi, psu4.secondaryUpdateSuccessCount, psu4.index, fwFilePaths[1]));
                }

                StartStopCmService("stop");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        #endregion

        #region Helper Functions

        private void VerifyPsuFwUpdate(ref List<UpdatePsu> psuList, ref TestResponse response)
        {
            // initialize return
            bool allPassed = true;

            // Initialize fw revision lists
            List<string> previousRevisionList = new List<string>();
            List<string> updatedRevisionList = new List<string>();

            // Run test using WcsAdmin
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Get Psu Fw Status for every PSU in list
            for (int index = 0; index < psuList.Count; index++)
            {
                UpdatePsu psu = psuList[index];
                psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);
                LogMessage.Message = string.Format(
                        "{0}: GetPsuFirmwareStatus for PSU {1} before update - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                        this.currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                        psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage);

                // Verify PSU is available for update
                if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                    psu.psuFwStatus.fwUpdateStatus == "InProgress")
                {
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                    psu.passed = false;
                    previousRevisionList.Add(null);
                }
                else
                {
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);
                    previousRevisionList.Add(psu.psuFwStatus.fwRevision);
                }
                psuList[index] = psu;
            }

            // Verify at least one Psu available for update testing
            bool allFailed = true;
            psuList.ForEach(psu => allFailed &= !psu.passed);
            if (allFailed)
            {
                LogMessage.Message = "{0}: No Psu is available for update.";
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return;
            }

            // Update Psus
            for (int index = 0; index < psuList.Count; index++)
            {
                UpdatePsu psu = psuList[index];
                if (!psu.passed)
                {
                    LogMessage.Message = string.Format(
                        "{0}: Psu {1} not ready for update, skipping.",
                        this.currentApi, psu.index);
                    LogMessage.Log(LogMessage.MessageType.Info, ref response);
                    continue;
                }

                psu.psuFwUpdateResponse = this.TestChannelContext.UpdatePSUFirmware(psu.index,
                    psu.psuFwFilePath, psu.primaryImage);
                LogMessage.Message = string.Format(
                        "{0}: Psu {1} returned completion code {2} and status description {3}",
                        this.currentApi, psu.index, psu.psuFwUpdateResponse.completionCode,
                        psu.psuFwUpdateResponse.statusDescription);

                if (psu.psuFwUpdateResponse.completionCode != CompletionCode.Success)
                {
                    LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                    psu.passed = false;
                }
                else
                    LogMessage.Log(LogMessage.MessageType.Success, ref response);

                psuList[index] = psu;
            }

            // Verify at least one Psu available for update testing
            allFailed = true;
            psuList.ForEach(psu => allFailed &= !psu.passed);
            if (allFailed)
            {
                LogMessage.Message = "{0}: All Psus failed to update.";
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return;
            }

            // Sleep for 5 minutes 
            LogMessage.Message = "Thread sleeping for 5 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(5));

            // Verify Psus are being updated After 5 minutes
            for (int index = 0; index < psuList.Count; index++)
            {
                UpdatePsu psu = psuList[index];

                // Only verify Psus that are being updated
                if (!psu.passed)
                    continue;

                PrintGetPsuFwStatus(ref psu, true, psu.primaryImage, this.currentApi, psu.psuFwFilePath, ref allPassed);
                psuList[index] = psu;
            }

            // Verify at least one Psu available for update testing
            allFailed = true;
            psuList.ForEach(psu => allFailed &= !psu.passed);
            if (allFailed)
            {
                LogMessage.Message = "{0}: All Psus failed to update after 5 minutes.";
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return;
            }

            // Sleep for 7 more minutes
            LogMessage.Message = "Thread sleeping for 7 minutes...";
            LogMessage.Log(LogMessage.MessageType.Info, ref response);
            Thread.Sleep(TimeSpan.FromMinutes(7));

            // Verify Psus have been updated
            for (int index = 0; index < psuList.Count; index++)
            {
                UpdatePsu psu = psuList[index];

                // Only verify Psus that were being updated
                if (!psu.passed)
                    continue;

                PrintGetPsuFwStatus(ref psu, false, psu.primaryImage, this.currentApi, psu.psuFwFilePath, ref allPassed);
                psuList[index] = psu;

                // Compare previous and updated fw revisions
                string[] previousRevision = previousRevisionList[index].Split('.');
                string[] currentRevision = psu.psuFwStatus.fwRevision.Split('.');

                if (psu.primaryImage) // outside digits must remain the same (ie xx.AB.xx)
                {
                    LogMessage.Message = string.Format(
                        "{0}: Primary Fw Revision for Psu {1} - Before: {2} After: {3}",
                        this.currentApi, psu.index, previousRevisionList[index], psu.psuFwStatus.fwRevision);

                    if ((previousRevision[0] == currentRevision[0] &&
                        previousRevision[2] == currentRevision[2]) ||
                        previousRevisionList[index].Contains('?'))
                        LogMessage.Log(LogMessage.MessageType.Success, ref response);
                    else
                    {
                        LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                        psu.passed = false;
                    }
                }
                else // middle digits must remain the same (ie AB.xx.CD)
                {
                    LogMessage.Message = string.Format(
                        "{0}: Secondary Fw Revision for Psu {1} - Before: {2} After: {3}",
                        this.currentApi, psu.index, previousRevisionList[index], psu.psuFwStatus.fwRevision);

                    if (previousRevision[1] == currentRevision[1] ||
                        previousRevisionList[index].Contains('?'))
                        LogMessage.Log(LogMessage.MessageType.Success, ref response);
                    else
                    {
                        LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                        psu.passed = false;
                    }
                }
            }

            // Update response 
            if (!allPassed)
                response.Result = false;
        }

        private void PrintGetPsuFwStatus(ref UpdatePsu psu, bool verifyAfter5Min, bool primaryImage,
            string currentApi, string fwFilePath, ref bool allPassed)
        {
            // Get Psu Firmware Status 
            psu.psuFwStatus = this.TestChannelContext.GetPSUFirmwareStatus(psu.index);

            CmTestLog.Info(string.Format(
                "{0}: GetPsuFWStatus for Psu {1} - fwRevision: {2} completionCode: {3} fwUpdateStatus: {4} fwUpdateStage: {5}",
                currentApi, psu.index, psu.psuFwStatus.fwRevision, psu.psuFwStatus.completionCode,
                psu.psuFwStatus.fwUpdateStatus, psu.psuFwStatus.fwUpdateStage));

            if (verifyAfter5Min)
            {
                // Verify Psu Update is progressing as expected
                if (((psu.psuFwStatus.completionCode != CompletionCode.Success)
                    && (psu.psuFwStatus.completionCode != CompletionCode.PSUFirmwareUpdateInProgress))
                    || ((psu.psuFwStatus.fwUpdateStatus != "InProgress")
                    && (psu.psuFwStatus.fwUpdateStatus != "Success")))
                {
                    CmTestLog.Failure(string.Format("{0}: Psu {1} failed update after 5 minutes\nfw: {2}",
                        currentApi, psu.index, fwFilePath));
                    psu.passed = false;
                    allPassed = false;
                }
            }
            else
            {
                // Verify Psu Fw Update Complete
                if (psu.psuFwStatus.completionCode != CompletionCode.Success ||
                    ((psu.psuFwStatus.fwUpdateStatus != "Success")
                    && (psu.psuFwStatus.fwUpdateStage != "Completed")))
                {
                    CmTestLog.Failure(string.Format("{0}: Psu {1} failed update after 12 minutes\nfw: {2}",
                        currentApi, psu.index, fwFilePath));
                    psu.passed = false;
                    allPassed = false;
                }
                else
                {
                    if (primaryImage)
                        psu.primaryUpdateSuccessCount++;
                    else
                        psu.secondaryUpdateSuccessCount++;
                }
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

        private bool ReturnSingleOrMultipleRandomPsus(int numberOfRandomPsus, ref int[] psuArray, ref TestResponse response)
        {
            List<int> psuIdList = GetPsuLocations().ToList();

            // make sure there is at least one extra Psu to power chassis
            if (psuIdList.Count < (numberOfRandomPsus + 1))
            {
                LogMessage.Message = string.Format(
                        "{0}: There is not enough Psus to run tests.",
                        this.currentApi);
                LogMessage.Log(LogMessage.MessageType.Failure, ref response);
                return false;
            }

            while (psuIdList.Count != numberOfRandomPsus)
            {
                psuIdList.Remove(ChassisManagerTestHelper.RandomOrDefault(psuIdList.ToArray()));
            }
            psuArray = psuIdList.ToArray();
            return true;
        }

        #endregion

    }
}
