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
    
    internal class AcSocketPowerCommands : CommandBase
    {
        internal double BladePowerLowerLimit = 120;
        internal double BladePowerUpperLimit = 1000;

        internal AcSocketPowerCommands(IChassisManager channel) : base(channel)
        {
        }

        internal AcSocketPowerCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts) : base(channel, testChannelContexts)
        {
        }

        /// <summary>
        ///     Basic functional test for ACSocketPowerCommand
        /// </summary>
        /// <returns></returns>
        public bool AcSocketPowerCommandBasicValidationTest()
        {
            return (this.AcSocketPowerTest() &&
                    this.AcSocketPowerGetByAllUserTest() &&
                    this.AcSocketPowerSetByAdminOperatorTest());
        }

        /// <summary>
        ///     Basic functional validation test for AC SocketPower
        /// </summary>
        /// <returns></returns>
        protected bool AcSocketPowerTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            string failureMessage;

            ChassisResponse acSocketResponse = null;
            ACSocketStateResponse acSocketPower = null;
            uint NumACSocket = CmConstants.NumPowerSwitches;

            Console.WriteLine("!!!!!!!!! Started execution of ACSocketPowerTest.");

            for (int testedAcSocket = 1; testedAcSocket <= NumACSocket; testedAcSocket++)
            {
                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("!!!Failed to power off from unknown state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("!!!Failed to power off AC socket when it is already off for socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOn(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("!!!Failed to power ON AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                if (acSocketPower.powerState != PowerState.ON)
                {
                    failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOn(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("!!!Failed to power ON AC socket when it is already ON for AC Socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success)
                {
                    failureMessage = string.Format("!!!Failed to power off AC socket from ON state for AC Socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }

                acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                if (acSocketPower.powerState != PowerState.OFF)
                {
                    failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }
            }

            //Test for invalid parameters
            acSocketResponse = this.Channel.SetACSocketPowerStateOn(0);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            acSocketResponse = this.Channel.SetACSocketPowerStateOn(9999);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            acSocketResponse = this.Channel.SetACSocketPowerStateOn(4);
            if (acSocketResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage = string.Format("!!!Failed During SetACSocketPowerStateOn(0), response is: {0}", acSocketResponse.completionCode);
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "!!!!!!!!! Successfully finished execution of ACSocketPowerTests.";
            Console.WriteLine(failureMessage);

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     GetACSocketPowerState: Verify that all users can execute the command
        /// </summary>
        /// <returns></returns>
        protected bool AcSocketPowerGetByAllUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            string failureMessage;

            ChassisResponse acSocketResponse = null;
            ACSocketStateResponse acSocketPower = null;
            uint numAcSocket = CmConstants.NumPowerSwitches;

            Console.WriteLine("!!!!!!!!! Started execution of ACSocketPowerTest.");

            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                //Change test connection to different role
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                for (int testedAcSocket = 1; testedAcSocket <= numAcSocket; testedAcSocket++)
                {
                    // Turn off ACSocket Power state
                    acSocketResponse = this.Channel.SetACSocketPowerStateOff(testedAcSocket);

                    if (acSocketResponse.completionCode == CompletionCode.Success)
                    {
                        // Check to see if socket power state is return correctly
                        acSocketPower = this.TestChannelContext.GetACSocketPowerState(testedAcSocket);
                        if (acSocketPower.powerState != PowerState.OFF)
                        {
                            failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                            CmTestLog.Failure(failureMessage);
                            testPassed = false;
                        }
                    }

                    // Turn on ACSocket Power state
                    acSocketResponse = this.Channel.SetACSocketPowerStateOn(testedAcSocket);

                    if (acSocketResponse.completionCode == CompletionCode.Success)
                    {
                        // Check to see if socket power state is return correctly
                        acSocketPower = this.TestChannelContext.GetACSocketPowerState(testedAcSocket);
                        if (acSocketPower.powerState != PowerState.ON)
                        {
                            failureMessage = string.Format("!!!Failed to get power state for AC socket#{0}", testedAcSocket);
                            CmTestLog.Failure(failureMessage);
                            testPassed = false;
                        }
                    }
                }
            }

            failureMessage = "!!!!!!!!! Successfully finished execution of ACSocketPowerTests.";
            Console.WriteLine(failureMessage);

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     SetACSocketPowerStateOff: Verify that only Operator and Admin can execute the command
        /// </summary>
        /// <returns></returns>
        protected bool AcSocketPowerSetByAdminOperatorTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            string failureMessage = string.Empty;
            const uint NumACSocket = CmConstants.NumPowerSwitches;

            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                try
                {
                    this.AcSocketSetGetValidation(ref testPassed, ref failureMessage, NumACSocket, roleId);
                }
                catch (Exception e)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!e.Message.Contains("401") && roleId == WCSSecurityRole.WcsCmUser)
                    {
                        // Test failed, http response should contain http 401 error
                        CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                    }
                }
            }

            Console.WriteLine("\n++++++++++++++++++++++++++++++++");
            failureMessage = "!!!!!!!!! Successfully finished execution of ACSocketPowerTests.";
            Console.WriteLine(failureMessage);

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     SetACSocketPowerStateOff: Verify that only Operator and Admin can execute the command
        /// </summary>
        /// <param name="testPassed"></param>
        /// <param name="failureMessage"></param>
        /// <param name="numAcSocket"></param>
        /// <param name="roleId"></param>
        private void AcSocketSetGetValidation(ref bool testPassed, ref string failureMessage, uint numAcSocket,
            WCSSecurityRole roleId)
        {
            ChassisResponse acSocketResponse = null;
            ACSocketStateResponse acSocketPower = null;

            // Use different user context
            this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

            for (int testedAcSocket = 1; testedAcSocket <= numAcSocket; testedAcSocket++)
            {
                // Turn On ACSocket
                acSocketResponse = this.TestChannelContext.SetACSocketPowerStateOn(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success &&
                    (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                {
                    failureMessage =
                        string.Format("!!!Failed to power ON AC socket when it is already ON for AC Socket# {0}",
                            testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }
                else if (acSocketResponse.completionCode == CompletionCode.Success &&
                         (roleId == WCSSecurityRole.WcsCmUser))
                {
                    failureMessage =
                        string.Format("User is not allow to called out to SetACSocketPowerStateOn {0} Socket# {1}",
                            roleId, testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }
                else
                {
                    // Verify power state
                    acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                    if (acSocketPower.powerState != PowerState.ON)
                    {
                        failureMessage =
                            string.Format(
                                "!!!Failed to power ON AC socket when it is already ON for AC Socket# {0}",
                                testedAcSocket);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }

                // Turn  off ACSocket 
                acSocketResponse = this.TestChannelContext.SetACSocketPowerStateOff(testedAcSocket);
                if (acSocketResponse.completionCode != CompletionCode.Success &&
                    (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                {
                    failureMessage =
                        string.Format("!!!Failed to power ON AC socket when it is already ON for AC Socket# {0}",
                            testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }
                else if (acSocketResponse.completionCode == CompletionCode.Success && roleId == WCSSecurityRole.WcsCmUser)
                {
                    failureMessage =
                        string.Format(
                            "User is not allow to called out to SetACSocketPowerStateOff {0} Socket# {1}", roleId,
                            testedAcSocket);
                    CmTestLog.Failure(failureMessage);
                    testPassed = false;
                }
                else
                {
                    // Verify power state
                    acSocketPower = this.Channel.GetACSocketPowerState(testedAcSocket);
                    if (acSocketPower.powerState != PowerState.OFF)
                    {
                        failureMessage =
                            string.Format(
                                "!!!Failed to power ON AC socket when it is already ON for AC Socket# {0}",
                                testedAcSocket);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
            }// end of for loop
        }
    }
}
