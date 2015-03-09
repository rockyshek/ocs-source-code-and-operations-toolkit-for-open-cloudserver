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

    internal class PowerCommands : CommandBase
    {
        internal double BladePowerLowerLimit = 120;
        internal double BladePowerUpperLimit = 1000;

        internal PowerCommands(IChassisManager channel) :
            base(channel)
        {
        }

        internal PowerCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts) :
            base(channel, testChannelContexts)
        {
        }

        internal PowerCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts,
            Dictionary<string, string> testEnvironment) :
                base(channel, testChannelContexts, testEnvironment)
        {
        }

   
        /// <summary>
        ///     Basic functional test for PowerCommand
        /// </summary>
        /// <returns></returns>
        public Boolean PowerCommandBasicValidationTest()
        {
            return (
                GetAllBladesPowerReadingTest() &&
                SetAllBladesPowerLimit() &&
                SetBladePowerLimit() &&
                GetAllBladesPowerLimit() &&
                GetBladePowerReadingByAllUserTest() &&
                GetBladePowerLimitByAllUserTest() &&
                SetBladePowerLimitAdminOperUserTest() &&
                SetBladePowerLimitOnOffAdminOperUserTest());
        }


        /// <summary>
        ///     GetAllBladesPowerReading: Command runs but shows not valid for JBOD blades
        ///     Setup Instructions:
        ///     ========================
        ///     none
        ///     Steps:
        ///     ========================
        ///     1- Open browser URL:http://
        ///     <targetCMName>
        ///         :8000/GetAllBladesPowerReading
        ///         2- When prompted use WcsAdmin
        /// </summary>
        /// <returns></returns>
        protected Boolean GetAllBladesPowerReadingTest()
        {
            CmTestLog.Start();
            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            bool testPassed = true;

            int bladeIndex = 1;
            try
            {
                GetAllBladesPowerReadingResponse allBladesPowerReadingResponse = Channel.GetAllBladesPowerReading();

                foreach (
                    BladePowerReadingResponse bPowerReadingResponse in
                        allBladesPowerReadingResponse.bladePowerReadingCollection)
                {
                    if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                    {
                        if (bPowerReadingResponse.completionCode != CompletionCode.Success)
                        {
                            CmTestLog.Failure("Failed to get blade power reading.");
                            testPassed = false;
                        }
                    }
                    else
                    {
                        if (JbodLocations.Contains(bladeIndex))
                        {
                            if (bPowerReadingResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                            {
                                CmTestLog.Failure(
                                    "This must have been a JBOD. it must fail with commandNotValidForBlade.");
                                testPassed = false;
                            }
                        }
                    }
                    bladeIndex++;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }


        /// <summary>
        ///     SetAllBladesPowerLimit: Verify command returns completion code ParameterOutofRange if power limit is not valid
        ///     Setup Instructions:
        ///     ========================
        ///     1- Best practice is to close and reopen the browser to clear cached credentials.
        ///     2-
        ///     <aValidBladePowerLimitInWatts>
        ///         is between 120-1000.
        ///         3-
        ///         <NotaValidBladePowerLimitInWatts>
        ///             is less than 120 and greater than 1000
        ///             Steps:
        ///             ========================
        ///             1- Open browser URL:http://
        ///             <targetCMName>
        ///                 :8000/SetAllBladesPowerLimit?powerLimitInWatts=
        ///                 <NotaValidBladePowerLimitInWatts>
        ///                     2- When prompted use WcsAdmin
        /// </summary>
        /// <returns></returns>
        protected Boolean SetAllBladesPowerLimit()
        {
            CmTestLog.Start();

            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            Boolean testPassed = true;

            int powerLimitValue = 777;
            AllBladesResponse allbResponse;
            GetAllBladesPowerLimitResponse allbladesPowerLimitResponse;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            // Power on blades and soft-power on
            AllBladesResponse response = this.TestChannelContext.SetAllPowerOn();
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "SetAllPowerOn failed: Completion Code - " +
                    response.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            // Sleep thread
            CmTestLog.Info("Thread sleeping for {0} seconds...",
                CmConstants.BladePowerOnSeconds.ToString());
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            response = this.TestChannelContext.SetAllBladesOn();
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "SetAllBladesOn failed: Completion Code - " +
                    response.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            // Sleep thread
            CmTestLog.Info("Thread sleeping for {0} seconds...",
                CmConstants.BladePowerOnSeconds.ToString());
            Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));

            allbResponse = Channel.SetAllBladesPowerLimit(powerLimitValue);
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = "\n!!!Failed to set blade power limit for blade# " + bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbResponse = Channel.SetAllBladesPowerLimitOn();
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = "\n!!!Failed to set blade power limit ON for blade# " + bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbladesPowerLimitResponse = Channel.GetAllBladesPowerLimit();

            foreach (
                BladePowerLimitResponse bPowerLimitResponse in allbladesPowerLimitResponse.bladePowerLimitCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bPowerLimitResponse.completionCode != CompletionCode.Success ||
                        powerLimitValue != bPowerLimitResponse.powerLimit)
                    {
                        failureMessage = "\n!!!Failed to get blade power limit for blade# " + bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bPowerLimitResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                bladeIndex++;
            }
            //reset blade counter
            bladeIndex = 1;
            allbResponse = Channel.SetAllBladesPowerLimitOff();
            foreach (BladeResponse bResponse in allbResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success)
                    {
                        failureMessage = "\n!!!Failed to set blade power limit OFF for blade# " + bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "\n!!!This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                bladeIndex++;
            }

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     SetBladePowerLimit: Verify command returns completion code ParameterOutofRange if power limit is not valid
        ///     Setup Instructions:
        ///     ========================
        ///     1- Best practice is to close and reopen the browser to clear cached credentials.
        ///     2-
        ///     <aValidBladePowerLimitInWatts>
        ///         is between 120-1000.
        ///         3-
        ///         <NotaValidBladePowerLimitInWatts>
        ///             is less than 120 and greater than 1000
        ///             Steps:
        ///             ========================
        ///             1- Open browser URL:http://
        ///             <targetCMName>
        ///                 :8000/SetBladePowerLimit?powerLimitInWatts=
        ///                 <NotaValidBladePowerLimitInWatts>
        ///                     2- When prompted use WcsAdmin
        /// </summary>
        /// <returns></returns>
        protected Boolean SetBladePowerLimit()
        {
            CmTestLog.Start();
            Boolean testPassed = true;
            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            BladeResponse bResponse;
            BladePowerLimitResponse bPowerLimitResponse;
            double powerLimitValue = 500;

            Boolean isServer = false;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= CmConstants.Population)
            {                
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                    isServer = true;
                else
                    bladeIndex++;
            }

            if (bladeIndex > CmConstants.Population)
            {
                failureMessage = "\n!!!Failed to find a server blade to run the test.";
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }


            bResponse = Channel.SetBladePowerLimit(bladeIndex, 999999);
            if (bResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage =
                    "\n!!!Set Blade Power limit Did not return a parameterOutOfRange when setting to MaxValue.";
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bResponse = Channel.SetBladePowerLimit(bladeIndex, -99999);
            if (bResponse.completionCode != CompletionCode.ParameterOutOfRange)
            {
                failureMessage =
                    "\n!!!Set Blade Power limit Did not return a parameterOutOfRange when setting to MinValue.";
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bResponse = Channel.SetBladePowerLimit(bladeIndex, powerLimitValue);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit failed with response " + bResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bResponse = Channel.SetBladePowerLimitOn(bladeIndex);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit ON failed with response " + bResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bPowerLimitResponse = Channel.GetBladePowerLimit(bladeIndex);
            if (bPowerLimitResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit failed with response " + bPowerLimitResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bPowerLimitResponse = Channel.GetBladePowerLimit(bladeIndex);
            if (bPowerLimitResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit failed with response " + bPowerLimitResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bPowerLimitResponse = Channel.GetBladePowerLimit(bladeIndex);
            if (bPowerLimitResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit failed with response " + bPowerLimitResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bResponse = Channel.SetBladePowerLimitOff(bladeIndex);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit OFF failed with response " + bResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            bResponse = Channel.SetBladePowerLimitOff(bladeIndex);
            if (bResponse.completionCode != CompletionCode.Success)
            {
                failureMessage = "\n!!!Set Blade Power limit OFF failed with response " + bResponse.completionCode;
                CmTestLog.Failure(failureMessage);
                testPassed = false;
            }

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Basic functional test for getAll Blade power limit.
        /// </summary>
        /// <returns></returns>
        protected Boolean GetAllBladesPowerLimit()
        {
            CmTestLog.Start();

            Boolean testPassed = true;
            int bladeIndex = 1;
            Boolean isServer = false;

            double powerLimitValue = 777;

            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            BladeResponse bladeResponse = null;

            // User WcsAdmin Connection
            TestChannelContext = TestChannelContext = ListTestChannelContexts[(int) WCSSecurityRole.WcsCmAdmin];

            //make sure you pick a server and not a JBOD
            while (!isServer && bladeIndex <= CmConstants.Population)
            {              
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                    isServer = true;
                else
                    bladeIndex++;
            }


            // Start set PowerLimit on selected blade
            if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
            {
                bladeResponse = TestChannelContext.SetBladePowerLimit(bladeIndex, powerLimitValue);

                if (bladeResponse.completionCode == CompletionCode.Success)
                {
                    // Set the PowerLimitOn
                    bladeResponse = TestChannelContext.SetBladePowerLimitOn(bladeIndex);

                    // Set the powerlimit Off
                    bladeResponse = TestChannelContext.SetBladePowerLimitOff(bladeIndex);

                    // Verify if BladePowerLimit is set GetAllBladesPowerLimit
                    // Get Power limit by different user types
                    GetAllBladesPowerLimitResponse allBladesPowerLimitResponse =
                        TestChannelContext.GetAllBladesPowerLimit();
                    foreach (
                        BladePowerLimitResponse bPowerLimitResponse in
                            allBladesPowerLimitResponse.bladePowerLimitCollection)
                    {
                        if (bPowerLimitResponse.bladeNumber == bladeIndex &&
                            bPowerLimitResponse.powerLimit != powerLimitValue)
                        {
                            // This power limit should remain same                            
                            CmTestLog.Failure("Get all power limit failed, after calling set powerlimit.");
                            testPassed = false;
                        }
                    }
                }
            }

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }


        /// <summary>
        ///     Verify all user groups are able to execute following commands
        ///     GetAllBladesPowerReading
        ///     GetBladePowerReading
        /// </summary>
        /// <returns></returns>
        protected Boolean GetBladePowerReadingByAllUserTest()
        {
            CmTestLog.Start();

            Boolean testPassed = true;
            int bladeIndex = 1;

            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            BladePowerReadingResponse bladepowerReading;

            // Loop through different user types and get power Reading
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                // Use different user contect WcsCM group role and send Rest request to update password                                                  
                TestChannelContext = ListTestChannelContexts[(int) roleId];

                GetAllBladesPowerReadingResponse allBladesPowerReadingResponse =
                    TestChannelContext.GetAllBladesPowerReading();
                foreach (
                    BladePowerReadingResponse bPowerReadingResponse in
                        allBladesPowerReadingResponse.bladePowerReadingCollection)
                {
                    bladeIndex = bPowerReadingResponse.bladeNumber;

                    if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                    {
                        if (bPowerReadingResponse.completionCode != CompletionCode.Success)
                        {
                            CmTestLog.Failure("Failed to get blade power reading.");
                            testPassed = false;
                        }

                        // this must be Blade, call out for GetBladePowerReading
                        bladepowerReading = TestChannelContext.GetBladePowerReading(bladeIndex);
                        if (bladepowerReading.completionCode != CompletionCode.Success)
                        {
                            CmTestLog.Failure("Reading power reading failed.");
                            testPassed = false;
                        }
                    }
                    else
                    {
                        if (JbodLocations.Contains(bladeIndex))
                        {
                            if (bPowerReadingResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                            {
                                CmTestLog.Failure(
                                    "This must have been a JBOD. it must fail with commandNotValidForBlade.");
                                testPassed = false;
                            }
                        }
                        else if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                        {
                            // Set request on JBOD and should get the CommandNotValidForBlade
                            bladepowerReading = TestChannelContext.GetBladePowerReading(bladeIndex);
                            if (bladepowerReading.completionCode != CompletionCode.CommandNotValidForBlade)
                            {
                                CmTestLog.Failure("Reading power reading failed.");
                                testPassed = false;
                            }
                        }
                    }
                } // Loop through Blade responses collection.

                //reset the blade counter
                bladeIndex = 1;
            } //Loop for different connection                            

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }


        /// <summary>
        ///     Verify all user groups are able to execute following commands
        ///     GetAllBladesPowerLimit
        ///     GetBladePowerLimit
        /// </summary>
        /// <returns></returns>
        protected Boolean GetBladePowerLimitByAllUserTest()
        {
            CmTestLog.Start();
            Boolean testPassed = true;
            int bladeIndex = 1;
            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            BladePowerLimitResponse bladepowerLimit;

            // Loop through different user types and get power limit
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                // Use different user contect WcsCM group role and send Rest request to update password                                                  
                TestChannelContext = ListTestChannelContexts[(int) roleId];

                // Get Power limit by different user types
                GetAllBladesPowerLimitResponse allBladesPowerLimitResponse = TestChannelContext.GetAllBladesPowerLimit();
                foreach (
                    BladePowerLimitResponse bPowerLimitResponse in allBladesPowerLimitResponse.bladePowerLimitCollection
                    )
                {
                    bladeIndex = bPowerLimitResponse.bladeNumber;

                    if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                    {
                        if (bPowerLimitResponse.completionCode != CompletionCode.Success &&
                            (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                        {
                            CmTestLog.Failure("Failed to get blade power reading.");
                            testPassed = false;
                        }

                        // this must be Blade, call out for GetBladePowerReading
                        bladepowerLimit = TestChannelContext.GetBladePowerLimit(bladeIndex);
                        if (bladepowerLimit.completionCode != CompletionCode.Success)
                        {
                            CmTestLog.Failure("Get power limit failed.");
                            testPassed = false;
                        }
                        else if (bladepowerLimit.completionCode == CompletionCode.Success &&
                                 (bladepowerLimit.powerLimit < BladePowerLowerLimit ||
                                  bladepowerLimit.powerLimit > BladePowerUpperLimit))
                        {
                            CmTestLog.Failure("Power Limit outside of limit");
                            testPassed = false;
                        }
                    }
                    else
                    {
                        if (JbodLocations.Contains(bladeIndex))
                        {
                            if (bPowerLimitResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                            {
                                CmTestLog.Failure(
                                    "This must have been a JBOD. it must fail with commandNotValidForBlade.");
                                testPassed = false;
                            }
                        }

                        if (!EmptyLocations.Contains(bladeIndex))
                        {
                            // this must be JBOD, call out for GetBladePowerReading
                            bladepowerLimit = TestChannelContext.GetBladePowerLimit(bladeIndex);
                            if (bladepowerLimit.completionCode != CompletionCode.CommandNotValidForBlade)
                            {
                                CmTestLog.Failure("Get power limit failed.");
                                testPassed = false;
                            }
                        }
                    }
                } //Blade powerlimit

                //reset the blade counter
                bladeIndex = 1;
            } //Loop for different connection

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify only users member of Operator or Admin is able to execute following command
        ///     SetAllBladesPowerLimit
        ///     SetBladePowerLimit
        /// </summary>
        /// <returns></returns>
        protected Boolean SetBladePowerLimitAdminOperUserTest()
        {
            CmTestLog.Start("SetBladePowerLimitAdminOperUserTest");
            Boolean testPassed = true;
            int bladeIndex = 1;
            int powerLimitValue = 777;
            string failureMessage = string.Empty;

            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();

            AllBladesResponse allbladeResponse;

            try
            {
                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    // Use different user contect WcsCM group role and send Rest request to update password                                                  
                    TestChannelContext = ListTestChannelContexts[(int) roleId];
                    try
                    {
                        SetBladPowerLimitByAdminOperator(ref testPassed, ref bladeIndex, powerLimitValue,
                            ref failureMessage, roleId, out allbladeResponse);
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting HTTP response 401 rather than 400.");
                        }
                    }
                    //reset the blade counter
                    bladeIndex = 1;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify only users member of Operator or Admin is able to execute following command
        /// </summary>
        /// <param name="testPassed"></param>
        /// <param name="bladeIndex"></param>
        /// <param name="powerLimitValue"></param>
        /// <param name="failureMessage"></param>
        /// <param name="roleId"></param>
        /// <param name="allbladeResponse"></param>
        private void SetBladPowerLimitByAdminOperator(ref Boolean testPassed, ref int bladeIndex, int powerLimitValue,
            ref string failureMessage, WCSSecurityRole roleId, out AllBladesResponse allbladeResponse)
        {
            BladeResponse bladeResponse;

            allbladeResponse = TestChannelContext.SetAllBladesPowerLimit(powerLimitValue);
            foreach (BladeResponse bResponse in allbladeResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = string.Format("Failed to set blade power limit for blade# {0} by {1}",
                            bladeIndex, roleId);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bResponse.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser))
                    {
                        failureMessage =
                            string.Format("wcsUser successfully to set blade power limit for blade# {0}", bladeIndex);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }

                    // This must be valid blade, try to set the set power limit
                    bladeResponse = TestChannelContext.SetBladePowerLimit(bladeIndex, powerLimitValue);
                    if (bladeResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = "Admin/Operator Failed to set power limit.";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bladeResponse.completionCode == CompletionCode.Success &&
                             roleId == WCSSecurityRole.WcsCmUser)
                    {
                        failureMessage = "WcsUser shouldn't allow to set the power limit.";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        Console.WriteLine(failureMessage);
                        testPassed = false;
                    }
                    if (!EmptyLocations.Contains(bladeIndex))
                    {
                        // This must be Jbod, send requests for set power limit
                        bladeResponse = TestChannelContext.SetBladePowerLimit(bladeIndex, powerLimitValue);
                        if (bladeResponse.completionCode == CompletionCode.Success &&
                            (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                        {
                            failureMessage = "It must fail with commandNotValidForBlade for blade";
                            Console.WriteLine(failureMessage);
                            testPassed = false;
                        }
                    }
                }
                bladeIndex++;
            }
        }

        /// <summary>
        ///     Verify only users member of Operator or Admin is able to execute following command
        ///     SetAllBladesPowerLimitOn
        ///     SetBladePowerLimitOn
        ///     SetAllBladesPowerLimitOff
        ///     SetBladePowerLimitOff
        /// </summary>
        /// <returns></returns>
        internal Boolean SetBladePowerLimitOnOffAdminOperUserTest()
        {
            CmTestLog.Start("SetBladePowerLimitAdminOperUserTest");
            Boolean testPassed = true;
            int bladeIndex = 1;
            string failureMessage = string.Empty;

            EmptyLocations = GetEmptyLocations();
            JbodLocations = GetJbodLocations();


            // Loop through different user connections
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                // Use different user contect WcsCM group role and send Rest request to update password                                                  
                TestChannelContext = ListTestChannelContexts[(int) roleId];

                try
                {
                    SetBladePowerLimitOnOff(ref testPassed, ref bladeIndex, ref failureMessage, roleId);
                }
                catch (Exception e)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!e.Message.Contains("401"))
                    {
                        // Test failed, http response should contain http 401 error
                        CmTestLog.Failure("We are expecting HTTP response 401 rather than 400.");
                    }
                }
                //reset the blade counter
                bladeIndex = 1;
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify only users member of Operator or Admin is able to execute following command
        ///     SetAllBladesPowerLimitOn
        ///     SetBladePowerLimitOn
        ///     SetAllBladesPowerLimitOff
        ///     SetBladePowerLimitOff
        /// </summary>
        /// <returns></returns>
        private void SetBladePowerLimitOnOff(ref Boolean testPassed, ref int bladeIndex, ref string failureMessage,
            WCSSecurityRole roleId)
        {
            BladeResponse bladeResponse;
            AllBladesResponse allbladeResponse;

            allbladeResponse = TestChannelContext.SetAllBladesPowerLimitOn();
            foreach (BladeResponse bResponse in allbladeResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = string.Format("Failed to set blade power limit On for blade# {0} by {1}",
                            bladeIndex, roleId);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bResponse.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser))
                    {
                        failureMessage = string.Format(
                            "WcsUser successfully set blade power limit for blade# {0} ", bladeIndex);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }

                    // This must be valid blade, try to set the set power limit Off
                    bladeResponse = TestChannelContext.SetBladePowerLimitOn(bladeIndex);
                    if (bladeResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = "Admin/Operator failed to set power limit off";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bladeResponse.completionCode == CompletionCode.Success &&
                             roleId == WCSSecurityRole.WcsCmUser)
                    {
                        failureMessage = "WcsUser successfully allow to set the power limit On.";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    if (!EmptyLocations.Contains(bladeIndex))
                    {
                        bladeResponse = TestChannelContext.SetBladePowerLimitOn(bladeIndex);
                        if (bladeResponse.completionCode == CompletionCode.Success &&
                            (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                        {
                            failureMessage = "This is JBOD. it must fail with commandNotValidForBlade for blade";
                            CmTestLog.Failure(failureMessage);
                            testPassed = false;
                        }
                    }
                }
                bladeIndex++;
            }

            // Reset blade
            bladeIndex = 1;

            // Set the Blade Power off
            allbladeResponse = TestChannelContext.SetAllBladesPowerLimitOff();
            foreach (BladeResponse bResponse in allbladeResponse.bladeResponseCollection)
            {
                if (!JbodLocations.Contains(bladeIndex) && !EmptyLocations.Contains(bladeIndex))
                {
                    if (bResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = string.Format("Failed to set blade power limit Off for blade# {0} by {1}",
                            bladeIndex, roleId);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bResponse.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser))
                    {
                        failureMessage =
                            string.Format("WcsUser successfully to set blade power limit Off for blade# {0} ",
                                bladeIndex);
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }

                    // This must be valid blade, try to set the set power limit On
                    bladeResponse = TestChannelContext.SetBladePowerLimitOn(bladeIndex);
                    if (bladeResponse.completionCode != CompletionCode.Success &&
                        (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                    {
                        failureMessage = "";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    else if (bladeResponse.completionCode == CompletionCode.Success &&
                             roleId == WCSSecurityRole.WcsCmUser)
                    {
                        failureMessage = "WcsUser shouldn't allow to set the power limit Off.";
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                }
                else
                {
                    if (JbodLocations.Contains(bladeIndex) &&
                        bResponse.completionCode != CompletionCode.CommandNotValidForBlade)
                    {
                        failureMessage =
                            "This must have been a JBOD. it must fail with commandNotValidForBlade for blade# " +
                            bladeIndex;
                        CmTestLog.Failure(failureMessage);
                        testPassed = false;
                    }
                    if (!EmptyLocations.Contains(bladeIndex))
                    {
                        bladeResponse = TestChannelContext.SetBladePowerLimitOn(bladeIndex);
                        if (bladeResponse.completionCode == CompletionCode.Success &&
                            (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
                        {
                            failureMessage = "This is a JBOD. It must fail with commandNotValidForBlade for blade# " +
                                             bladeIndex;
                            CmTestLog.Failure(failureMessage);
                            testPassed = false;
                        }
                    }
                }
                bladeIndex++;
            }
        }

        #region SetGetPowerState Tests

        /// <summary>
        ///     Test Commands: GetPowerState, SetPowerOn,SetPowerOff. The test case verifies:
        ///     The command returns completion code success;
        ///     The command returns correct power state when power is on;
        ///     The command returns correct power state when power is off.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetGetPowerStateTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            // get server and jbod locations
            int[] serverLocations, jbodLocations;
            if (!GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations) ||
                !GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                CmTestLog.Failure("Cannot get server or jbod locations");
                CmTestLog.End(false);
                return false;
            }
            this.EmptyLocations = this.GetEmptyLocations();

            // [TFS WorkItem: 1706]  SetAllPowerOff powers off All blades at ones
            CmTestLog.Info("Trying to power OFF all blades");
            if (!SetPowerState(PowerState.OFF))
            {
                CmTestLog.Failure("Cannot power OFF all blades");
                CmTestLog.End(false);
                return false;
            }
            // [TFS WorkItem: 4536] GetPowerState: Command toggles using SetPowerOn/Off
            // [TFS WorkItem: 3980] Verify when blade Power is ON
            if (serverLocations.Length > 0)
            {
                int randServerBladeId = serverLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random server location# {0} ", randServerBladeId));
                testPassed = VerifyPowerStateOn(testPassed, randServerBladeId);

            }
            // [TFS WorkItem: 3992] GetPowerState: Verify JBOD blade power state can be read
            // [TFS WorkItem: 3379 & 3994] PowerOn Command: Turn on of JBODs & SetPowerOn: Turns a JBOD power ON
            if (jbodLocations.Length > 0)
            {
                int randJbodBladeId = jbodLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random Jbod location# {0} ", randJbodBladeId));
                testPassed = VerifyPowerStateOn(testPassed, randJbodBladeId);
            }
            // [TFS WorkItem: 3981 & 1710]Verify power can be ON when a blade is not present & SetPowerOn: SetPowerOn when blade is not present
            if (this.EmptyLocations.Length > 0)
            {
                int randEmptyBladeId = EmptyLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random Empty location# {0}", randEmptyBladeId));
                testPassed = VerifyPowerStateOn(testPassed, randEmptyBladeId);
            }

            // [TFS WorkItem: 3968] SetAllBladesOn: SetAllBladesOn when Chassis has blades on, off and missing.
            CmTestLog.Info("Trying to power ON all blades");
            if (!SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot power ON all blades");
                CmTestLog.End(false);
                return false;
            }

            // [TFS WorkItem: 1712] SetPowerOn: Request power on when blade is already on.
            int randomBlade = ChassisManagerTestHelper.RandomInteger(1, 25);
            testPassed = VerifyPowerStateOn(testPassed, randomBlade);

            // [TFS WorkItem: 4536] GetPowerState: Command toggles using SetPowerOn/Off
            // [TFS WorkItem: 3979] GetPowerState: Verify when blade Power is OFF
            if (serverLocations.Length > 0)
            {
                int randServerBladeId = serverLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random server location# {0} ", randServerBladeId));
                testPassed = VerifyPowerStateOff(testPassed, randServerBladeId);
            }
            // [TFS WorkItem: 3992] GetPowerState: Verify JBOD blade power state can be read
            // [TFS WorkItem: 3997 & 2750] SetPowerOff: SetPowerOff for JBODs & SetPowerOff: Turns a JBOD power OFF
            if (jbodLocations.Length > 0)
            {
                int randJbodBladeId = jbodLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random Jbod location# {0} ", randJbodBladeId));
                testPassed = VerifyPowerStateOff(testPassed, randJbodBladeId);
            }
            // [TFS WorkItem: 3982] GetPowerState: Verify power can be OFF when a blade is not present
            if (this.EmptyLocations.Length > 0)
            {
                int randEmptyBladeId = EmptyLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Trying to get random Empty location# {0}", randEmptyBladeId));
                testPassed = VerifyPowerStateOff(testPassed, randEmptyBladeId);
            }

            // [TFS WorkItem: 1716] SetPowerOn: SetPowerOn when blade number is a (Positive) blade index
            BladeResponse state = Channel.SetPowerOn(CmConstants.InvalidBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, state.completionCode,
                "Received ParameterOutOfRange for invalid +ve blade# " + CmConstants.InvalidBladeId);

            // [TFS WorkItem: 1715] SetPowerOn: SetPowerOn when blade number is a  (Negative) blade index
            state = Channel.SetPowerOn(CmConstants.InvalidNegtiveBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, state.completionCode,
                "Received ParameterOutOfRange for invalid -ve blade# " + CmConstants.InvalidNegtiveBladeId);

            // [TFS WorkItem: 2393] SetPowerOff: Verify Only PowerOn & PowerOff actions succeed. GetPowerState succeeds & reports OFF but all other APIs fail
            foreach (int blade in serverLocations)
            {
                testPassed = VerifyPowerStateOff(testPassed, blade);
                // Randomly execute other API commands after power off the blade
                CmTestLog.Info("Trying to SetBladeOn when blade power state is OFF");
                BladeResponse response = Channel.SetBladeOn(blade);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.DevicePoweredOff, response.completionCode,
                    "Received DevicePoweredOff blade# " + blade);
                break;
            }

            // Test clean-up
            CmTestLog.Info("Test Complete: powering on all blades");
            powerOnAndSetAllBladesOn();

            CmTestLog.End(testPassed);
            return testPassed;
        }

        private bool VerifyPowerStateOn(bool testPassed, int bladeId)
        {
            CmTestLog.Info("Trying to power on blade");
            if (!SetPowerState(PowerState.ON, bladeId))
            {
                CmTestLog.Failure(string.Format("Can not power ON Blade# {0} ", bladeId));
                CmTestLog.End(false);
                return false;
            }

            PowerStateResponse state = Channel.GetPowerState(bladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, state.completionCode,
                "Get power state for blade# " + bladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, state.powerState,
                string.Format("Verify Blade# {0} is powered ON", state.bladeNumber));
            return testPassed;
        }

        private bool VerifyPowerStateOff(bool testPassed, int bladeId)
        {
            CmTestLog.Info("Trying to power OFF blade");
            if (!SetPowerState(PowerState.OFF, bladeId))
            {
                CmTestLog.Failure(string.Format("Can not power OFF Blade# {0} ", bladeId));
                CmTestLog.End(false);
                return false;
            }

            PowerStateResponse state = Channel.GetPowerState(bladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, state.completionCode,
                "Power state for blade# " + bladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, state.powerState,
                string.Format("Verify Blade# {0} is powered OFF", state.bladeNumber));
            return testPassed;
        }

        /// <summary>
        ///     Test Command: GetAllPowerState, SetAllPowerOff, SetAllPowerOn. The test case verifies:
        ///     The command returns completion code success;
        ///     The command returns correct power state when power is on;
        ///     The command returns correct power state when power is off.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetGetAllPowerStateTest()
        {
            CmTestLog.Start();
            // [TFS WorkItem: 1978] Verify all blades can turn OFF->ON->OFF including JBOD blades
            //[TFS WorkItem: 1714] SetAllPowerOn when Chassis has blades ON and MISSING.
            // [TFS WorkItem: 1711] SetAllPowerOn powers all present blades on.
            // test PowerOn state
            CmTestLog.Info("Trying to power on all blades");
            if (!SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot power on all blades");
                CmTestLog.End(false);
                return false;
            }

            bool testPassed = true;
            CmTestLog.Info("Trying to retrieve power state of all blades");
            GetAllPowerStateResponse response1 = Channel.GetAllPowerState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response1.completionCode, "Get all power state");
            foreach (PowerStateResponse state in response1.powerStateResponseCollection)
            {
                CmTestLog.Info("Checking power state for Blade# " + state.bladeNumber);
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, state.powerState,
                    string.Format("Verify Blade# {0} is powered ON", state.bladeNumber));
            }
            // [TFS WorkItem: 1713] SetAllPowerOn when Chassis has blades ON and OFF.
            int bladeId = ChassisManagerTestHelper.RandomInteger(1, 25);
            CmTestLog.Info("Trying to power off single blade");
            if (!SetPowerState(PowerState.OFF, bladeId))
            {
                CmTestLog.Failure(string.Format("Can not power off Blade# {0} ", bladeId));
                CmTestLog.End(false);
                return false;
            }

            CmTestLog.Info("Trying to power on all blades");
            if (!SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot power on all blades");
                CmTestLog.End(false);
                return false;
            }

            CmTestLog.Info("Trying to retrieve power state of all blades");
            response1 = Channel.GetAllPowerState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response1.completionCode, "Get all power state");
            foreach (PowerStateResponse state in response1.powerStateResponseCollection)
            {
                CmTestLog.Info("Checking power state for Blade# " + state.bladeNumber);
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, state.powerState,
                    string.Format("Verify Blade# {0} is powered ON", state.bladeNumber));
            }

            // [TFS WorkItem:1709] SetAllPowerOff all blades when some are missing
            // test PowerOff state
            CmTestLog.Info("Trying to power off all blades");
            if (!SetPowerState(PowerState.OFF))
            {
                CmTestLog.Failure("Cannot power off all blades");
                CmTestLog.End(false);
                return false;
            }

            CmTestLog.Info("Trying to retrieve power state of all blades");
            GetAllPowerStateResponse response2 = Channel.GetAllPowerState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response2.completionCode, "Get all power state");
            foreach (PowerStateResponse state in response2.powerStateResponseCollection)
            {
                CmTestLog.Info("Checking power state for Blade# " + state.bladeNumber);
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, state.powerState,
                    string.Format("Verify Blade# {0} is powered OFF", state.bladeNumber));
            }

            // Test clean-up
            CmTestLog.Info("Test complete: powering on all blades");
            powerOnAndSetAllBladesOn();

            CmTestLog.End(testPassed);
            return testPassed;
        }

        #endregion

        #region HELPER Classes

        /// <summary>
        ///     Helper for test for powerOn
        ///     takes the status of the blades before calling the method. Status can be ON, OFF or Unknown.
        ///     returns pass or fail string.
        /// </summary>
        public string HelpSetAllPowerOnTest()
        {
            const int MaxNumBlades = CmConstants.Population;
            PowerStateResponse bladeState = null;

            //PowerOn all blades one by one and verify along the way.
            for (int bladeId = 1; bladeId <= MaxNumBlades; bladeId++)
            {
                Channel.SetPowerOn(bladeId);
                Thread.Sleep(1000); //wait for a sec
            }

            Thread.Sleep(50000);

            //Make sure that all blades are powered On
            for (int bladeId = 1; bladeId <= MaxNumBlades; bladeId++)
            {
                bladeState = Channel.GetPowerState(bladeId);
                if (bladeState.powerState != PowerState.ON)
                {
                    return "fail";
                }
            }

            return "Pass";
        }

        /// <summary>
        ///     Helper for test for poweroff
        ///     takes the status of the blades before calling the method. Status can be ON, OFF or Unknown.
        ///     returns pass or fail string.
        /// </summary>
        public string HelpSetAllPowerOffTest()
        {
            const int MaxNumBlades = CmConstants.Population;
            PowerStateResponse bladeState = null;

            //Powerff all blades one by one and verify along the way.

            for (int bladeId = 1; bladeId <= MaxNumBlades; bladeId++)
            {
                Channel.SetPowerOff(bladeId);
                Thread.Sleep(100); //wait for a sec
            }

            Thread.Sleep(10000);

            //Make sure that all blades are powered Off
            for (int bladeId = 1; bladeId <= MaxNumBlades; bladeId++)
            {
                bladeState = Channel.GetPowerState(bladeId);
                if (bladeState.powerState != PowerState.OFF)
                {
                    return "fail";
                }
            }

            return "Pass";
        }

        private void powerOnAndSetAllBladesOn()
        {
            // SetPowerOn all blades
            CmTestLog.Info("Powering ON all blades");
            AllBladesResponse setAllOnResponse = new AllBladesResponse();
            setAllOnResponse = this.TestChannelContext.SetAllPowerOn();

            if (setAllOnResponse.completionCode != CompletionCode.Success)
            {
                CmTestLog.Failure("SetAllPowerOn: Command returned completionCode " +
                    setAllOnResponse.completionCode);
                return;
            }
            else
            {
                CmTestLog.Info(string.Format("Powered On all blades. Thread sleeping for {0} seconds...",
                    CmConstants.BladePowerOnSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));
            }

            // SetAllBladesOn
            CmTestLog.Info("Soft-powering ON all blades");
            setAllOnResponse = this.TestChannelContext.SetAllBladesOn();

            if (setAllOnResponse.completionCode != CompletionCode.Success)
            {
                CmTestLog.Failure("SetAllBladesOn: Command returned completionCode " +
                    setAllOnResponse.completionCode);
            }
            else
            {
                CmTestLog.Info(string.Format("Soft-powered On all blades. Thread sleeping for {0} seconds...",
                    CmConstants.BladePowerOnSeconds));
                Thread.Sleep(TimeSpan.FromSeconds(CmConstants.BladePowerOnSeconds));
            }
        }

        #endregion

    }
}
