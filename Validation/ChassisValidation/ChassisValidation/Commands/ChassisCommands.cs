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
    using Microsoft.GFS.WCS.Contracts;

    internal class ChassisCommands : CommandBase
    {
        internal ChassisCommands(IChassisManager channel) : base(channel)
        {
        }

        internal ChassisCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts) : base(channel, testChannelContexts)
        {
        }

        /// <summary>
        /// Test Command: GetChassisHealth. The test case verifies:
        /// The command returns a success completion code;
        /// The command returns all information for blades, fans, and PSUs;
        /// All blades, fans and PSUs are in corrent numbers and in healthy state.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetChassisHealthTest()
        {
            CmTestLog.Start();

            bool testPassed = true;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            // Loop through different user types and get chassis health
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    CmTestLog.Info(string.Format("Get Chassis Health with user type {0} \n", Enum.GetName(typeof(WCSSecurityRole), roleId)));                           
                    testPassed = this.GetChassisHealth(testPassed, roleId);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }
            }

            return testPassed;
        }

        public bool GetChassisHealth(bool allPassed, WCSSecurityRole roleId)
        {
            try
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    // Test for chassishealth components are returned when all params are true : WorkItem(3373 & 2746)
                    CmTestLog.Info("Verifying chassis health information when all params are true");
                    ChassisHealthResponse chassisHealth = this.TestChannelContext.GetChassisHealth(true, true, true, true);
                    allPassed &= this.VerifyChassisHealth(allPassed, chassisHealth);
                    CmTestLog.Info("!!!! Finished verification of Getchassishealth when all params are true !!!!\n");

                    // Test for chassishealth with no params : WorkItem(4776)
                    CmTestLog.Info("Verifying chassis health information when all params are false OR no Params");
                    chassisHealth = this.TestChannelContext.GetChassisHealth(false, false, false, false);
                    allPassed &= this.VerifyChassisHealth(allPassed, chassisHealth);
                    CmTestLog.Info("!!!! Finished verification of Getchassishealth with no params !!!!\n");

                    // Test for chassishealth get only bladeshell information : WorkItem(3157)
                    CmTestLog.Info("Verifying chassis health information for only Blade Health param is true");
                    chassisHealth = this.TestChannelContext.GetChassisHealth(true, false, false, false);
                    allPassed &= this.VerifyOnlyChassisBladeHealth(allPassed, chassisHealth);
                    CmTestLog.Info("!!!! Finished verification of Getchassishealth for only BladeHealth !!!!\n");

                    // Test for chassishealth get only PSU's health information : WorkItem(3159)
                    CmTestLog.Info("Verifying chassis health information for only psuHealth param is true");
                    chassisHealth = this.TestChannelContext.GetChassisHealth(false, true, false, false);
                    allPassed &= this.VerifyOnlyChassisPsuHealth(allPassed, chassisHealth);
                    CmTestLog.Info("!!!! Finished verification of Getchassishealth for only psuHealth !!!!\n");

                    // Test for chassishealth get only Fan's health information : WorkItem(3158)
                    CmTestLog.Info("Verifying chassis health information for only fan Health param is true");
                    chassisHealth = this.TestChannelContext.GetChassisHealth(false, false, true, false);
                    allPassed &= this.VerifyOnlyChassisFanHealth(allPassed, chassisHealth);
                    CmTestLog.Info("!!!! Finished verification of Getchassishealth for only fanHealth !!!!");
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed &= false;
            }
            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test GetChassisInfo for all possible parameters
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse GetChassisInfoTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();

            this.ServerLocations = this.GetServerLocations();
            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            try
            {
                ChassisInfoResponse chassisInfo = new ChassisInfoResponse();

                // Test GetChassisInfo, get all information
                CmTestLog.Info("Verifying chassis information when all params are true");
                chassisInfo = this.Channel.GetChassisInfo(true, true, true, true);

                // define an object of TestResponse class for test results
                TestResponse testResponse = this.VerifyChassisInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);

                CmTestLog.Info("Finished verification of GetchassisInfo when all information was returned blade, PSU, battery and chassis controller \n");

                // Test GetChassisInfo with no params 
                CmTestLog.Info("Verifying chassis information when all params are false OR no Params\n");
                chassisInfo = this.Channel.GetChassisInfo(false, false, false, false);

                testResponse = this.VerifyChassisInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);
                 
                CmTestLog.Info("Finished verification of GetchassisInfo with no params \n");

                // Test for GetChassisInfo with only blade info
                CmTestLog.Info("Verifying chassis information for only Blade, bladeInfo param is true\n");
                chassisInfo = this.Channel.GetChassisInfo(true, false, false, false);

                testResponse = this.VerifyOnlyChassisBladeInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);

                CmTestLog.Info("Finished verification of GetchassisInfo for only Blade information \n");

                // Test for GetChassisInfo for only PSU information
                CmTestLog.Info("Verifying chassis information for only psuInfo param is true");
                chassisInfo = this.Channel.GetChassisInfo(false, true, false, false);

                testResponse = this.VerifyOnlyChassisPsuInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);
                
                CmTestLog.Info("Finished verification of GetChassisInfo for only PSU information \n");

                // Test for GetChassisInfo for only Battery information
                CmTestLog.Info("Verifying chassis information for only batteryInfo param is true\n");
                chassisInfo = this.Channel.GetChassisInfo(false, false, false, true);

                testResponse = this.VerifyOnlyChassisBatteryInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);

                CmTestLog.Info("Finished verification of GetChassisInfo for only battery information \n");

                // Test for GetChassisInfo for only chassis controller information
                CmTestLog.Info("Verifying chassis information for only chassisControllerInfo param is true\n");
                chassisInfo = this.Channel.GetChassisInfo(false, false, true, false);

                testResponse = this.VerifyOnlyChassisControllerInfo(chassisInfo);
                response.Result &= testResponse.Result;
                response.ResultDescription.Append(testResponse.ResultDescription);
               
                CmTestLog.Info("Finished verification of GetChassisInfo for only chassis controller information \n");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result &= false;
                response.ResultDescription.Append(ex.Message);
            }

            CmTestLog.End(response.Result);
            return response;
        }

        /// <summary>
        /// Test GetChassisInfo for all possible parameters by different users
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public TestResponse GetChassisInfoByAllUserTest()
        {
            CmTestLog.Start();
            TestResponse response = new TestResponse();

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();
            
            // Loop through different user roles
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            { 
                try
                {
                    TestResponse testResponse = this.GetChassisInfoByAllUsers(roleId);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    response.Result &= false;
                    response.ResultDescription.Append(ex.Message);
                }
            }

            return response;
        }


        /// <summary>
        /// Test Get and Set Chassis LED by different users
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetGetChassisLedOnOffTestByAllUserTest()
        {
            CmTestLog.Start();

            bool testPassed = true;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();
            
            // Loop through different user types and get power Reading
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    testPassed = this.SetGetChassisLedOnOffTest(roleId);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }
            }

            return testPassed;
        }
        /// <summary>
        /// A test for Set and Get Chassis LED
        /// </summary>
        public bool SetGetChassisLedOnOffTest(WCSSecurityRole roleId)
        {
            CmTestLog.Start();
            ChassisResponse chassisResponse;
            bool testPassed = false;

            CmTestLog.Info("Starting execution of SetChassisLEDOnOffTest.");
            try
            {
                // Use the Domain User Channel
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    chassisResponse = this.TestChannelContext.GetChassisAttentionLEDStatus();
                    if (this.TestChannelContext.GetChassisAttentionLEDStatus().completionCode != CompletionCode.Success)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to Get the LED status.");
                        return false;
                    }
                    testPassed = true;

                    chassisResponse = this.TestChannelContext.SetChassisAttentionLEDOn();

                    if (chassisResponse.completionCode != CompletionCode.Success || this.TestChannelContext.GetChassisAttentionLEDStatus().ledState != LedState.ON)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to set the LED ON.");
                        return false;
                    }

                    //Turn On again
                    chassisResponse = this.TestChannelContext.SetChassisAttentionLEDOn();

                    if (chassisResponse.completionCode != CompletionCode.Success || this.TestChannelContext.GetChassisAttentionLEDStatus().ledState != LedState.ON)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to set the LED ON.");
                        return false;
                    }
                    // Turn off
                    chassisResponse = this.TestChannelContext.SetChassisAttentionLEDOff();

                    if (chassisResponse.completionCode != CompletionCode.Success || this.TestChannelContext.GetChassisAttentionLEDStatus().ledState != LedState.OFF)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to set the LED OFF.");
                        return false;
                    }
                    //turn off again
                    chassisResponse = this.TestChannelContext.SetChassisAttentionLEDOff();

                    if (chassisResponse.completionCode != CompletionCode.Success || this.TestChannelContext.GetChassisAttentionLEDStatus().ledState != LedState.OFF)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to set the LED OFF.");
                        return false;
                    }
                    //turn on from Off
                    chassisResponse = this.TestChannelContext.SetChassisAttentionLEDOn();

                    if (chassisResponse.completionCode != CompletionCode.Success || this.TestChannelContext.GetChassisAttentionLEDStatus().ledState != LedState.ON)
                    {
                        CmTestLog.Failure("SetChassisLEDOnOffTest: Failed to set the LED ON.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Check error is due to permission HTTP 400 unauthorize
                if (roleId == WCSSecurityRole.WcsCmUser && !ex.Message.Contains("400"))
                {
                    // Test failed, http response should contain http 401 error
                    CmTestLog.Failure("We are expecting 400 error due to user not having rights to set LED");
                }
                if(!testPassed)
                {
                    CmTestLog.Failure("All users have the right to GetChassisAttentionLedStatus. User from the follwoing role failed: " + roleId);
                }
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                return false;
            }
            return true;            
        }

        private static bool VerifyPsuHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            CmTestLog.Info("Verifying PSU health state");
            allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.NumPsus, chassisHealth.psuInfoCollection.Count, "Verified the number of PSUs is correct");

            foreach (var psu in chassisHealth.psuInfoCollection)
            {
                if (ConfigurationManager.AppSettings["TimeoutPSU"] != psu.id.ToString())
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, psu.completionCode, string.Format("PSU# {0} returns {1}", psu.id, psu.completionCode));
                    allPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, psu.state, string.Format("PSU# {0} power state is {1}", psu.id, psu.state));
                }
                else
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, psu.completionCode, string.Format("PSU# {0} returns Completion code {1} instead of the expected Timeout", psu.id, psu.completionCode));
                }
            }
            return allPassed;
        }

        private static bool VerifyFanHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            CmTestLog.Info("Verifying Fan health state");
            allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.NumFans, chassisHealth.fanInfoCollection.Count, "Verified the number of fans");

            foreach (var fan in chassisHealth.fanInfoCollection)
            {
                if (ConfigurationManager.AppSettings["FailedFan"] != fan.fanId.ToString())
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, fan.completionCode, string.Format("Fan# {0} returns {1}", fan.fanId, fan.completionCode));
                    allPassed &= ChassisManagerTestHelper.IsTrue(fan.isFanHealthy, string.Format("Fan# {0} healthy state is {1}", fan.fanId, fan.isFanHealthy));
                }
                else
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(false, fan.isFanHealthy, string.Format("Fan# {0} health status is {1} instead of the expected false", fan.fanId, fan.isFanHealthy));
                    allPassed &= ChassisManagerTestHelper.AreEqual(0, fan.fanSpeed, string.Format("Fan# {0} speed is reported as {1} instead of the expected 0", fan.fanId, fan.fanSpeed));
                }
            }
            return allPassed;
        }


        private bool VerifyBatteriesHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            int MaxNumberOfBatteries = 6;
            CmTestLog.Info("Verifying Battery health state");
            allPassed &= ChassisManagerTestHelper.AreEqual(MaxNumberOfBatteries, chassisHealth.batteryInfoCollection.Count, "Verified the number of Batteris is correct");

            if (CmConstants.NumBatteries == 0)
            {
                foreach (var battery in chassisHealth.batteryInfoCollection)
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Unknown, battery.completionCode, string.Format("Battery# {0} returned Completeion code {1}", battery.id, battery.completionCode));
                    allPassed &= ChassisManagerTestHelper.AreEqual(0, battery.batteryPowerOutput, string.Format("Battery# {0} returned BatteryPowerOut {1}", battery.id, battery.batteryPowerOutput));
                    allPassed &= ChassisManagerTestHelper.AreEqual(0, battery.batteryChargeLevel, string.Format("Battery# {0} returned BatteryChargeLevel {1}", battery.id, battery.batteryChargeLevel));
                    allPassed &= ChassisManagerTestHelper.AreEqual(0, battery.faultDetected, string.Format("Battery# {0} returned FaultDetected {1}", battery.id, battery.faultDetected));
                }
            }
            else
            {
                foreach (var battery in chassisHealth.batteryInfoCollection)
                {
                    if (ConfigurationManager.AppSettings["FailedBattery"] != battery.id.ToString())
                    {
                        allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, battery.completionCode, string.Format("Battery# {0} returned Completeion code {1}", battery.id, battery.completionCode));
                        allPassed &= ChassisManagerTestHelper.AreNotEqual(0, battery.batteryPowerOutput, string.Format("Battery# {0} returned BatteryPowerOut {1}", battery.id, battery.batteryPowerOutput));
                        allPassed &= ChassisManagerTestHelper.AreNotEqual(0, battery.batteryChargeLevel, string.Format("Battery# {0} returned BatteryChargeLevel {1}", battery.id, battery.batteryChargeLevel));
                        allPassed &= ChassisManagerTestHelper.AreNotEqual(0, battery.faultDetected, string.Format("Battery# {0} returned FaultDetected {1}", battery.id, battery.faultDetected));
                    }
                    else
                    {
                        allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, battery.completionCode, string.Format("Battery# {0} returned Completeion code {1} instead of the expected Failure state", battery.id, battery.completionCode));
                    }
                }
            }
            return allPassed;
        }

        private static bool VerifyBatteryHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            CmTestLog.Info("Verifying Battery health state");
            allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.NumBatteries, chassisHealth.psuInfoCollection.Count, "Verified the number of Batteris is correct");

            foreach (var battery in chassisHealth.psuInfoCollection)
            {
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, battery.completionCode, string.Format("Battery# {0} returns {1}", battery.id, battery.completionCode));
                allPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, battery.state, string.Format("Battery# {0} power state is {1}", battery.id, battery.state));
            }
            return allPassed;
        }

        /// <summary>
        /// Verify chassis PSU info
        /// </summary>
        /// <param name="allPassed">Flag indicating success/failure</param>
        /// <param name="chassisInfo">Chassis info response</param>
        /// <returns>returns success/failure</returns>
        private static TestResponse VerifyChassisPsuInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            CmTestLog.Info("Verifying PSU info");

            if (!ChassisManagerTestHelper.AreEqual(CmConstants.NumPsus, chassisInfo.psuCollections.Count, "Verified the number of PSUs is correct"))
            {
                response.Result = false;
                response.ResultDescription.Append("\n VerifyChassisBatteryInfo : Number of PSUs do not match the config value");
            }

            foreach (var psu in chassisInfo.psuCollections)
            {
                if (!ChassisManagerTestHelper.AreEqual(PowerState.ON, psu.state, string.Format("PSU# {0} power state is {1}", psu.id, psu.state)))
                {
                    response.ResultDescription.Append(string.Format("\nPSU# {0} power state is {1}", psu.id, psu.state));
                    response.Result &= false;
                }
            }
            return response;
        }

        /// <summary>
        /// Verify chassis battery info
        /// </summary>
        /// <param name="allPassed">Flag indicating success/failure</param>
        /// <param name="chassisInfo">Chassis info response</param>
        /// <returns>returns success/failure</returns>
        private static TestResponse VerifyChassisBatteryInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            CmTestLog.Info("Verifying battery info");
            if (CmConstants.NumBatteries == 0)
            {
                foreach (var battery in chassisInfo.batteryCollections)
                {
                    if (battery.completionCode != CompletionCode.Unknown || battery.presence != 0)
                    {
                        response.ResultDescription.Append(string.Format("\n Battery# {0} returns {1} when it should be Unknown", battery.id, battery.completionCode));
                        response.ResultDescription.Append(string.Format("Battery# {0} returns Presence value as {1}", battery.id, battery.presence));
                        response.Result &= false;
                    }
                }
            }
            else
            {
                foreach (var battery in chassisInfo.batteryCollections)
                {
                    if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, battery.completionCode, string.Format("Battery# {0} returns {1}", battery.id, battery.completionCode)))
                    {
                        response.ResultDescription.Append(string.Format("\n Battery# {0} returns {1}", battery.id, battery.completionCode));
                        response.Result &= false;
                    }
                }
            }
            return response;
        }

        /// <summary>
        /// Verify chassis controller info
        /// </summary>
        /// <param name="allPassed">Flag indicating success/failure</param>
        /// <param name="chassisInfo">Chassis info response</param>
        /// <returns>returns success/failure</returns>
        private static TestResponse VerifyChassisControllerInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            CmTestLog.Info("Verifying chassis controller info");

            if (!ChassisManagerTestHelper.AreEqual(CompletionCode.Success, chassisInfo.chassisController.completionCode, "Chassis controller returns success"))
            {
                response.Result = false;
                response.ResultDescription.Append("\n Verify Chassis controller info failed with completion code: " + chassisInfo.chassisController.completionCode);
             }
            return response;
        }

        private TestResponse GetChassisInfoByAllUsers(WCSSecurityRole roleId)
        {
            TestResponse response = new TestResponse();
            try
            {
                // Use the Domain User Channel
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    ChassisInfoResponse chassisInfo = new ChassisInfoResponse();

                    // Test GetChassisInfo, get all information
                    CmTestLog.Info("Verifying chassis information when all params are true");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(true, true, true, true);

                    TestResponse testResponse = this.VerifyChassisInfo(chassisInfo);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);

                    CmTestLog.Info("Finished verification of GetchassisInfo when all information was returned blade, PSU, battery and chassis controller \n");

                    // Test GetChassisInfo with no params 
                    CmTestLog.Info("Verifying chassis information when all params are false OR no Params\n");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(false, false, false, false);

                    testResponse = this.VerifyChassisInfo(chassisInfo);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                    
                    CmTestLog.Info("Finished verification of GetchassisInfo with no params \n");

                    // Test for GetChassisInfo with only blade info
                    CmTestLog.Info("Verifying chassis information for only Blade, bladeInfo param is true\n");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(true, false, false, false);

                    testResponse = this.VerifyOnlyChassisBladeInfo(chassisInfo);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);
                    
                    CmTestLog.Info("Finished verification of GetchassisInfo for only Blade information \n");

                    // Test for GetChassisInfo for only PSU information
                    CmTestLog.Info("Verifying chassis information for only psuInfo param is true\n");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(false, true, false, false);

                    testResponse = this.VerifyOnlyChassisPsuInfo(chassisInfo);
                    response.Result &= testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);

                    CmTestLog.Info("Finished verification of GetChassisInfo for only PSU information \n");

                    // Test for GetChassisInfo for only Battery information
                    CmTestLog.Info("Verifying chassis information for only batteryInfo param is true\n");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(false, false, false, true);

                    testResponse = this.VerifyOnlyChassisBatteryInfo(chassisInfo);
                    response.Result = testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);

                    CmTestLog.Info("Finished verification of GetChassisInfo for only battery information \n");

                    // Test for GetChassisInfo for only chassis controller information
                    CmTestLog.Info("Verifying chassis information for only chassisControllerInfo param is true\n");
                    chassisInfo = this.TestChannelContext.GetChassisInfo(false, false, true, false);

                    testResponse = this.VerifyOnlyChassisControllerInfo(chassisInfo);
                    response.Result = testResponse.Result;
                    response.ResultDescription.Append(testResponse.ResultDescription);

                    CmTestLog.Info("Finished verification of GetChassisInfo for only chassis controller information \n");
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result &= false;
                response.ResultDescription.Append(ex.Message);
            }
            return response;
        }

        private bool VerifyChassisHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");

            if (chassisHealth.completionCode != CompletionCode.Success)
            {
                CmTestLog.Failure("Failed to get chassis health");
                return false;
            }
            CmTestLog.Success("Successfully get chassis health");

            allPassed = this.VerifyChassisBladeHealth(allPassed, chassisHealth);

            allPassed = VerifyFanHealth(allPassed, chassisHealth);

            allPassed = VerifyPsuHealth(allPassed, chassisHealth);
            return allPassed;
        }

        private bool VerifyChassisBladeHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            CmTestLog.Info("Verifying blade shell health state");
            allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.Population, chassisHealth.bladeShellCollection.Count, "Verified the number of blades in the chassis");

            foreach (var shell in chassisHealth.bladeShellCollection)
            {
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, shell.completionCode, string.Format("Blade# {0} returns success", shell.bladeNumber));
                // [TFS WorkItem: 3375] GetChassishealth: Command shows blade failures when they exist
                if (this.EmptyLocations != null && this.EmptyLocations.Contains(shell.bladeNumber))
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.HealthyBladeState, shell.bladeState, string.Format("{0} Blade# {1} is not healthy", shell.bladeState, shell.bladeNumber));
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.UnKnownBladeType, shell.bladeType, string.Format("{0} Blade# {1} is Unknown", shell.bladeType, shell.bladeNumber));
                }
                else if (this.JbodLocations != null && this.JbodLocations.Contains(shell.bladeNumber))
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.HealthyBladeState, shell.bladeState, string.Format("{0} Blade# {1} is in good healthy", shell.bladeState, shell.bladeNumber));
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.JbodBladeType, shell.bladeType, string.Format("{0} Blade# {1} is JBOD", shell.bladeType, shell.bladeNumber));
                }
                else
                {
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.HealthyBladeState, shell.bladeState, string.Format("{0} Blade# {1} is in good healthy", shell.bladeState, shell.bladeNumber));
                    allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.ServerBladeType, shell.bladeType, string.Format("{0} Blade# {1} is Server ", shell.bladeType, shell.bladeNumber));
                }
            }
            return allPassed;
        }

        /// <summary>
        /// Test Command: GetChassisHealth: Get Only bladeHealth information (bladehealth=true). Test Verifies:
        /// Command returns completion code Success;
        /// Command returns full blade shell collection and verifies population,bladeType and bladeState
        /// Command returns Empty for Fan, PSU and Battery information.
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private bool VerifyOnlyChassisBladeHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            try
            {
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");

                if (chassisHealth.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure("Failed to get chassis health");
                    return false;
                }
                CmTestLog.Success("Successfully get chassis health");

                allPassed = this.VerifyChassisBladeHealth(allPassed, chassisHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.fanInfoCollection.Count == 0, "Fan information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.psuInfoCollection.Count == 0, "PSU information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.batteryInfoCollection.Count == 0, "Battery information is empty");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetChassisHealth: Get Only Psu's information (psuhealth=true). Test Verifies:
        /// Command returns completion code Success;
        /// Command returns full psu's information
        /// Command returns Empty for bladeshell,Fan and Battery information.
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private bool VerifyOnlyChassisPsuHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            try
            {
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");
                if (chassisHealth.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure("Failed to get chassis health");
                    return false;
                }
                CmTestLog.Success("Successfully get chassis health");

                allPassed = VerifyPsuHealth(allPassed, chassisHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.bladeShellCollection.Count == 0, "BladeShell information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.fanInfoCollection.Count == 0, "Fan information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.batteryInfoCollection.Count == 0, "Battery information is empty");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetChassisHealth: Get Only Fan's information (fanHealth=true). Test Verifies:
        /// Command returns completion code Success;
        /// Command returns full Fan's information
        /// Command returns Empty for bladeshell,Psu and Battery information.
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private bool VerifyOnlyChassisFanHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            try
            {
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");
                if (chassisHealth.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure("Failed to get chassis health");
                    return false;
                }
                CmTestLog.Success("Successfully get chassis health");

                allPassed = VerifyFanHealth(allPassed, chassisHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.bladeShellCollection.Count == 0, "BladeShell information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.psuInfoCollection.Count == 0, "PSU information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.batteryInfoCollection.Count == 0, "Battery information is empty");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        private bool VerifyOnlyChassisBatteriesHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            try
            {
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");
                if (CmConstants.NumBatteries == 0)
                {
                    if (chassisHealth.completionCode != CompletionCode.Unknown)
                    {
                        CmTestLog.Failure(string.Format("With no batteries the completion code is expected to be Unknow when it is {0}", chassisHealth.completionCode));
                        return false;
                    }
                }
                else
                {
                    if (chassisHealth.completionCode != CompletionCode.Success)
                    {
                        CmTestLog.Failure(string.Format("Chassis health is expected to return success but completion code returned is {0}", chassisHealth.completionCode));
                        return false;
                    }
                }
                CmTestLog.Success("Successfully get chassis health");
                
                allPassed = VerifyBatteriesHealth(allPassed, chassisHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.bladeShellCollection.Count == 0, "BladeShell information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.psuInfoCollection.Count == 0, "PSU information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.fanInfoCollection.Count == 0, "Fan information is empty");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetChassisHealth: Get Only Batteries information (BatteryInfo=true). Test Verifies:
        /// Command returns completion code Success;
        /// Command returns full batteries information
        /// Command returns Empty for bladeshell,Fan and PSU information.
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private bool VerifyOnlyChassisBatteryHealth(bool allPassed, ChassisHealthResponse chassisHealth)
        {
            try
            {
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth != null, "Received chasssis health ");
                if (chassisHealth.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure("Failed to get chassis health");
                    return false;
                }
                CmTestLog.Success("Successfully get chassis health");

                allPassed = VerifyBatteryHealth(allPassed, chassisHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.bladeShellCollection.Count == 0, "BladeShell information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.fanInfoCollection.Count == 0, "Fan information is empty");
                allPassed &= ChassisManagerTestHelper.IsTrue(chassisHealth.psuInfoCollection.Count == 0, "PSU information is empty");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Verify chassis info
        /// </summary>
        /// <param name="allPassed">Flag indicating success/failure</param>
        /// <param name="chassisInfo">Chassis info response</param>
        /// <returns>returns success/failure</returns>
        private TestResponse VerifyChassisInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            response.Result = ChassisManagerTestHelper.IsTrue(chassisInfo != null, "Received chasssis information ");

            // Return if chassis information is null
            if (response.Result == false)
            {
                response.ResultDescription.Append("\nReceived chassis object is null");
                return response;
            }

            if (chassisInfo.completionCode != CompletionCode.Success)
            {
                response.Result = false;
                response.ResultDescription.Append("\nFailed to get chassis information, completion code returned: " + chassisInfo.completionCode);
                CmTestLog.Failure(response.ResultDescription.ToString());
                return response;
            }
            CmTestLog.Success("Successfully received chassis info");

            TestResponse testResponse = VerifyChassisBladeInfo(chassisInfo);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);

            testResponse = VerifyChassisPsuInfo(chassisInfo);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);

            testResponse = VerifyChassisBatteryInfo(chassisInfo);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);

            testResponse = VerifyChassisControllerInfo(chassisInfo);
            response.Result &= testResponse.Result;
            response.ResultDescription.Append(testResponse.ResultDescription);

            return response;
        }

        /// <summary>
        /// Verify chassis blade info
        /// </summary>
        /// <param name="allPassed">Flag indicating success/failure</param>
        /// <param name="chassisInfo">Chassis info response</param>
        /// <returns>returns success/failure</returns>
        private TestResponse VerifyChassisBladeInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            CmTestLog.Info("Verifying blade info");
            response.Result = ChassisManagerTestHelper.AreEqual(CmConstants.Population, chassisInfo.bladeCollections.Count, "Verified the number of blades in the chassis");
            if (response.Result == false)
            {
                response.ResultDescription.Append("\nChassis blade info verification failed : Number of blades in the chassis do not match with the config value");
            }
            foreach (var bladeInfo in chassisInfo.bladeCollections)
            {
                if (!EmptyLocations.Contains(bladeInfo.bladeNumber))
                {
                response.Result &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeInfo.completionCode, string.Format("Blade# {0} returns {1}", bladeInfo.bladeNumber, bladeInfo.completionCode));
                if (response.Result == false)
                {
                    response.ResultDescription.Append(string.Format("\nFailed to get blade " + bladeInfo.bladeNumber + " information, completion code returned: " + bladeInfo.completionCode));
                }
            }
            }
            return response;
        }

        /// <summary>
        /// Test Command: GetChassisInfo: Get Only blade information (bladeInfo=true). 
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisInfo"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private TestResponse VerifyOnlyChassisBladeInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();
            try
            {
                response.Result = ChassisManagerTestHelper.IsTrue(chassisInfo != null, "Received chasssis information ");

                // Return if chassis information is null
                if (response.Result == false)
                {
                    response.ResultDescription.Append("\nReceived chassis object is null");
                    return response;
                }

                if (chassisInfo.completionCode != CompletionCode.Success)
                {
                    response.Result = false;
                    response.ResultDescription.Append("\nFailed to get chassis information with only bladeInfo set to true, completion code returned: " + chassisInfo.completionCode);
                    CmTestLog.Failure(response.ResultDescription.ToString());
                    return response;
                }
                CmTestLog.Success("Successfully received chassis information");

                response = this.VerifyChassisBladeInfo(chassisInfo);

                // Verify battery info is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.batteryCollections.Count == 0, "Battery information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisBladeInfo failed: Battery information is not empty");
                }

                // Verify PSU information is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.psuCollections.Count == 0, "PSU information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisBatteryInfo failed: PSU information is not empty");
                }

                // Verify chassis controller info is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.chassisController == null, "Chassis controller information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisBatteryInfo failed: Chassis Controller information is not empty");
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Test Command: GetChassisInfo: Get Only PSU information (psuInfo=true). 
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisInfo"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private TestResponse VerifyOnlyChassisPsuInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            try
            {
                response.Result = ChassisManagerTestHelper.IsTrue(chassisInfo != null, "Received chasssis information ");

                // Return if chassis information is null
                if (response.Result == false)
                {
                    response.ResultDescription.Append("\nReceived chassis object is null");
                    return response;
                }

                if (chassisInfo.completionCode != CompletionCode.Success)
                {
                    response.Result = false;
                    response.ResultDescription.Append("\nFailed to get chassis information with only PSUInfo set to true, completion code returned: " + chassisInfo.completionCode);
                    CmTestLog.Failure(response.ResultDescription.ToString());
                    return response;
                }
                CmTestLog.Success("Successfully get chassis info");

                response = VerifyChassisPsuInfo(chassisInfo);

                // Verify blade information is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.bladeCollections.Count == 0, "Blade information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisPSUInfo failed: Blade information is not empty");
                }

                // Verify battery info is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.batteryCollections.Count == 0, "Battery information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append(response.ResultDescription.Append("\nVerifyOnlyChassisPSUInfo failed: Battery information is not empty"));
                }

                // Verify chassis controller info is empty
                if (!ChassisManagerTestHelper.IsTrue(chassisInfo.chassisController == null, "Chassis controller information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisPSUInfo failed: Chassis Controller information is not empty");
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Test Command: GetChassisInfo: Get Only Battery information (batteryInfo=true). 
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private TestResponse VerifyOnlyChassisBatteryInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            try
            {
                response.Result = ChassisManagerTestHelper.IsTrue(chassisInfo != null, "Received chasssis info ");

                // Return if chassis information is null
                if (response.Result == false)
                {
                    response.ResultDescription.Append("\nReceived chassis object is null");
                    return response;
                }

                if (CmConstants.NumBatteries == 0)
                {
                    foreach (var battery in chassisInfo.batteryCollections)
                    {
                        if (battery.completionCode != CompletionCode.Unknown || battery.presence != 0)
                        {
                            response.ResultDescription.Append(string.Format("\n Battery# {0} returns {1} when it should be Unknown", battery.id, battery.completionCode));
                            response.ResultDescription.Append(string.Format("Battery# {0} returns Presence value as {1}", battery.id, battery.presence));
                            response.Result &= false;
                            return response;
                        }
                    }

                    response.Result = true;
                    response.ResultDescription.Append("\nSuccessfully verified response for case of non present batteries.");
                    CmTestLog.Success(response.ResultDescription.ToString());
                    return response;
                }
                else
                {
                    if (chassisInfo.completionCode != CompletionCode.Success)
                    {
                        response.Result = false;
                        response.ResultDescription.Append("\nFailed to get chassis information with only Chassis Battery Info set to true, completion code returned: " + chassisInfo.completionCode);
                        CmTestLog.Failure(response.ResultDescription.ToString());
                        return response;
                    }
                    CmTestLog.Success("Successfully received chassis info");

                    response = VerifyChassisBatteryInfo(chassisInfo);

                    // Verify blade information is empty
                    if (!ChassisManagerTestHelper.IsTrue(chassisInfo.bladeCollections.Count == 0, "Blade information is empty"))
                    {
                        response.Result &= false;
                        response.ResultDescription.Append("\nVerifyOnlyChassisBatteryInfo failed: Blade information is not empty");
                    }

                    // Verify PSU information is empty
                    if (!ChassisManagerTestHelper.IsTrue(chassisInfo.psuCollections.Count == 0, "PSU information is empty"))
                    {
                        response.Result &= false;
                        response.ResultDescription.Append("\nVerifyOnlyChassisBatteryInfo failed: PSU information is not empty");
                    }

                    // Verify chassis controller info is empty
                    if (!ChassisManagerTestHelper.IsTrue(chassisInfo.chassisController == null, "Chassis controller information is empty"))
                    {
                        response.Result &= false;
                        response.ResultDescription.Append("\nVerifyOnlyChassisBatteryInfo failed: Chassis Controller information is not empty");
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Test Command: GetChassisInfo: Get Only Chassis controller information (chassisControllerInfo=true). 
        /// </summary>
        /// <param name="allPassed"></param>
        /// <param name="chassisHealth"></param>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        private TestResponse VerifyOnlyChassisControllerInfo(ChassisInfoResponse chassisInfo)
        {
            TestResponse response = new TestResponse();

            try
            {
                response.Result = ChassisManagerTestHelper.IsTrue(chassisInfo != null, "Received chasssis info ");

                // Return if chassis information is null
                if (response.Result == false)
                {
                    response.ResultDescription.Append("\nChassis information received is null");
                    return response;
                }

                if (chassisInfo.completionCode != CompletionCode.Success)
                {
                    response.Result = false;
                    response.ResultDescription.Append("\nFailed to get chassis information with only Chassis Controller Info set to true, completion code returned: " + chassisInfo.completionCode);
                    CmTestLog.Failure(response.ResultDescription.ToString());
                    return response;
                }

                CmTestLog.Success("Successfully received chassis info");

                response = VerifyChassisControllerInfo(chassisInfo);

                // Verify blade information is empty
                if(!ChassisManagerTestHelper.IsTrue(chassisInfo.bladeCollections.Count == 0, "Blade information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisControllerInfo failed: Blade information is not empty");
                }

                // Verify PSU information is empty
                if(!ChassisManagerTestHelper.IsTrue(chassisInfo.psuCollections.Count == 0, "PSU information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisControllerInfo failed: PSU information is not empty");
                }
                
                // Verify battery info is empty
                if(!ChassisManagerTestHelper.IsTrue(chassisInfo.batteryCollections.Count == 0, "Battery information is empty"))
                {
                    response.Result &= false;
                    response.ResultDescription.Append("\nVerifyOnlyChassisControllerInfo failed: Battery information is not empty");
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                response.Result = false;
                response.ResultDescription.Append(ex.Message);
            }

            return response;
        }
    }
}
