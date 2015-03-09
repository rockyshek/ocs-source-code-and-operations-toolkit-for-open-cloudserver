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
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.GFS.WCS.Contracts;

    internal class AssetManagementCommands : CommandBase
    {
        internal AssetManagementCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts) : base(channel, TestChannelContexts)
        {
        }

        internal AssetManagementCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts, Dictionary<string, string> TestEnvironment) : base(channel, TestChannelContexts, TestEnvironment)
        {
        }

        // Constant strings
        private static class AssetManagementConstants
        {
            internal static string valid20CharStringWithSpaces = Uri.EscapeDataString("   This is a test   ");
            internal const int fruWritesRemaining = 255;
            internal const string valid1CharString = "X";
            internal const string valid56CharString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1289";
            internal const string invalid62CharString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        }

        /// <summary>
        /// Test Command: GetChassisManagerAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is displayed
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetChassisManagerAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "GetChassisManagerAssetInfo";

            try
            {
                // get chassis manager asset info : WorkItem(4702)
                CmTestLog.Info(string.Format("Calling {0}", currentApi));

                this.VerifyCmOrPdbAssetInfo(ref allPassed, true, (int)WCSSecurityRole.WcsCmAdmin, null);
                
                if (allPassed)
                {
                    // Log WorkItem 4702
                    ChassisManagerTestHelper.IsTrue(allPassed, string.Format("{0}: Command returns Completion Code Success and returns correct Fru information", currentApi));
                }
                else
                {
                    CmTestLog.Failure(string.Format("{0}: Command failed and additional tests will not be done for this API", currentApi));
                    CmTestLog.End(false);
                    return false;
                }

                // GetBladeAssetInfo returns completion code Success with correct FRU information for all users if server blade : WorkItem(4720)
                bool allUsersPassed = true;
                bool userPassed = true;

                foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                {
                    CmTestLog.Info(string.Format("Calling {0} for user {1}", currentApi, TestUser));
                    userPassed = true;

                    // verify CM FRU information
                    this.VerifyCmOrPdbAssetInfo(ref userPassed, true, (int)TestUser, null);

                    allUsersPassed &= userPassed;
                    ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: executes for user {1}", currentApi, TestUser));
                }
                ChassisManagerTestHelper.IsTrue(allUsersPassed, string.Format("{0}: Command passes for all users", currentApi));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: GetPdbAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is displayed
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetPdbAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "GetPdbAssetInfo";

            try
            {
                // get pdb asset info : WorkItem(4704)
                CmTestLog.Info(string.Format("Calling {0}", currentApi));
                ChassisAssetInfoResponse pdbAssetInfo = this.Channel.GetPdbAssetInfo();

                this.VerifyCmOrPdbAssetInfo(ref allPassed, false, (int)WCSSecurityRole.WcsCmAdmin, null);

                if (allPassed)
                {
                    // Log WorkItem 4704 
                    ChassisManagerTestHelper.IsTrue(allPassed, string.Format("{0}: Command returns Completion Code Success and returns correct Fru information", currentApi));
                }
                else
                {
                    CmTestLog.Failure(string.Format("{0}: Command failed and additional tests will not be done for this API", currentApi));
                    CmTestLog.End(false);
                    return false;
                }

                // GetBladeAssetInfo returns completion code Success with correct FRU information for all users if server blade : WorkItem(4722)
                bool allUsersPassed = true;
                bool userPassed = true;

                foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                {
                    CmTestLog.Info(string.Format("Calling GetPdbAssetInfo for user {0}", TestUser));
                    userPassed = true;

                    // verify Pdb FRU information
                    this.VerifyCmOrPdbAssetInfo(ref userPassed, false, (int)TestUser, null);

                    allUsersPassed &= userPassed;
                    ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: executes for user {1}", currentApi, TestUser));
                }
                ChassisManagerTestHelper.IsTrue(allUsersPassed, string.Format("{0}: Command passes for all users", currentApi));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is displayed
        /// </summary>
        public bool GetBladeAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            bool bladePassed;
            string currentApi = "GetBladeAssetInfo";

            this.EmptyLocations = null;
            this.JbodLocations = null;

            try
            {
                this.EmptyLocations = this.GetEmptyLocations();
                this.JbodLocations = this.GetJbodLocations();
            
                for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                {
                    CmTestLog.Info(string.Format("Calling {0}", currentApi));
                    BladeAssetInfoResponse bladeAssetInfo = this.Channel.GetBladeAssetInfo(bladeId);
                    bladePassed = false;

                    // GetBladeAssetInfo returns completion code Failure if empty slot
                    if (this.EmptyLocations != null && this.EmptyLocations.Contains(bladeId))
                    {
                        bladePassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeAssetInfo.completionCode,
                            string.Format("{1}: Completion Code Failure for bladeId {0} - Empty slot", bladeId.ToString(), currentApi));
                        allPassed &= bladePassed;

                        continue;
                    }
                    // GetBladeAssetInfo returns completion code CommandNotValidForBlade if JBOD : WorkItem(8683)
                    else if (this.JbodLocations != null && this.JbodLocations.Contains(bladeId))
                    {
                        bladePassed = ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeAssetInfo.completionCode,
                            string.Format("GetBladeAssetInfo: Completion Code CommandNotValidForBlade for bladeId {0} - JBOD", bladeId.ToString()));
                        allPassed &= bladePassed;
                        ChassisManagerTestHelper.IsTrue(bladePassed, string.Format("{1} Completion Code CommandNotValidForBlade for bladeId {0} - JBOD", bladeId.ToString(), currentApi));
                        continue;
                    }
                    // is server blade
                    else
                    {
                        // GetBladeAssetInfo returns completion code Success and correct Fru information if server blade : WorkItem(4703)
                        this.VerifyBladeAssetInfo(ref bladePassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, null);
                        
                        ChassisManagerTestHelper.IsTrue(bladePassed, string.Format("{1}: Completion Code Success and correct fru information for bladeId {0}", bladeId.ToString(), currentApi));
                        allPassed &= bladePassed;

                        // GetBladeAssetInfo returns completion code Success and correct FRU information if server blade is hard power cycled : WorkItem(8685)
                        if (bladePassed)
                        {
                            // Hard Power Cycle Blade (Power Off -> Power On)
                            if (!this.SetPowerState(PowerState.OFF, bladeId))
                            {
                                CmTestLog.Failure(string.Format("Power Off failed for bladeId {0}", bladeId));
                                continue;
                            }

                            if (!this.SetPowerState(PowerState.ON, bladeId))
                            {
                                CmTestLog.Failure(string.Format("Power On failed for bladeId {0}", bladeId));
                                continue;
                            }
                            else
                            {
                                CmTestLog.Info(string.Format("Power Cycle success for bladeId {0}", bladeId));
                            }

                            CmTestLog.Info("Calling GetBladeAssetInfo after hard power cycle");
                            bladeAssetInfo = this.Channel.GetBladeAssetInfo(bladeId);

                            // Verify blade Fru information
                            this.VerifyBladeAssetInfo(ref bladePassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, null);

                            ChassisManagerTestHelper.IsTrue(bladePassed, string.Format("{1}: Command passed after hard-power cycle for bladeId {0}", bladeId.ToString(), currentApi));
                            allPassed &= bladePassed;
                        }

                        // GetBladeAssetInfo returns completion code Success with correct FRU information for all users if server blade : WorkItem(4721)
                        bool allUsersPassed = true;
                        foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                        {
                            CmTestLog.Info(string.Format("Calling GetBladeAssetInfo for user {0}", TestUser));

                            this.VerifyBladeAssetInfo(ref bladePassed, bladeId, (int)TestUser, null);
                            
                            allUsersPassed &= bladePassed;
                            ChassisManagerTestHelper.IsTrue(bladePassed, string.Format("GetBladeAssetInfo executes for user {0}", TestUser));
                        }
                        ChassisManagerTestHelper.IsTrue(allUsersPassed, string.Format("{0}: All users can execute the command for bladeId {1}", currentApi, bladeId));
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: SetChassisManagerAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is changed 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetChassisManagerAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "SetChassisManagerAssetInfo";

            MultiRecordResponse setCmAssetInfoResponse = null;
            ChassisAssetInfoResponse cmAssetInfo = null;         

            try
            { 
                // SetChassisManagerAssetInfo returns completion code Success with correct FRU information for WcsCmAdmin users : WorkItem(4822)
                bool allUsersPassed = true;
                bool userPassed = false;
                foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                {
                    CmTestLog.Info(string.Format("Calling {0} for user {1}", currentApi, TestUser));
                    
                    try
                    {
                        this.TestChannelContext = this.ListTestChannelContexts[(int)TestUser];
                        
                        setCmAssetInfoResponse = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);

                        // verify Completion Code Success
                        userPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, setCmAssetInfoResponse.completionCode,
                        string.Format("{0}: finished successfully using user {1}", currentApi, TestUser));

                        // verify SetChassisManagerAssetInfo has set the Fru
                        if (userPassed)
                        {
                            cmAssetInfo = this.TestChannelContext.GetChassisManagerAssetInfo();
                            this.VerifyCmOrPdbAssetInfo(ref userPassed, true, (int)TestUser, string.Empty);
                        }

                        ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: Successfully set FRU information using user {1}", currentApi, TestUser));

                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("400") && TestUser != WCSSecurityRole.WcsCmAdmin)
                        {
                            userPassed = true;
                            ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: Command returns correct exception for user {1}", currentApi, TestUser));
                        }
                        else
                        {
                            ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                            allPassed = false;
                            return allPassed;
                        }
                    }

                    allUsersPassed &= userPassed;
                
                }
                ChassisManagerTestHelper.IsTrue(allUsersPassed, string.Format("{0}: All users can execute the command", currentApi));
                allPassed &= allUsersPassed;

                // Verify SetChassisManagerAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(8709)
                // Verify SetChassisManagerAssetInfo command should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4711)
                VerifyCmOrPdbFruWritesRemaining(ref allPassed, true);

                if (allPassed)
                {
                    // Verify SetChassisManagerAssetInfo can only set first two fields of payload : WorkItem(4705)
                    bool testPassed = true;

                    string payload = AssetManagementConstants.valid1CharString;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.valid1CharString, AssetManagementConstants.valid1CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid1CharString,valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = AssetManagementConstants.valid20CharStringWithSpaces;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.valid20CharStringWithSpaces, AssetManagementConstants.valid20CharStringWithSpaces);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces,valid20CharStringWithSpaces'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1},{2}", 
                        AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,valid56CharString,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},,{1}", 
                        AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},,", AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,,'", currentApi));
                    allPassed &= testPassed;

                    // Verify SetChassisManagerAssetInfo sets truncated fields if fields are more than 56 characters : WorkItem(4708)
                    testPassed = true;

                    payload = AssetManagementConstants.invalid62CharString;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format(",{0}", AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload ',invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},", AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.valid1CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid1CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid1CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.valid56CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid56CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, true, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: SetPdbAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is changed 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetPdbAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "SetPdbAssetInfo";

            MultiRecordResponse setPdbAssetInfoResponse = null;
            ChassisAssetInfoResponse pdbAssetInfo = null;

            try
            {
                // SetPdbAssetInfo returns completion code Success with correct FRU information for WcsCmAdmin users : WorkItem(4823)
                bool allUsersPassed = true;
                bool userPassed = false;
                foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                {
                    CmTestLog.Info(string.Format("Calling {0} for user {1}", currentApi, TestUser));

                    try
                    {
                        this.TestChannelContext = this.ListTestChannelContexts[(int)TestUser];
                        
                        setPdbAssetInfoResponse = this.TestChannelContext.SetPdbAssetInfo(string.Empty);

                        // verify Completion Code Success
                        userPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, setPdbAssetInfoResponse.completionCode,
                        string.Format("SetPdbAssetInfo: Completion Code Success for user {0}", TestUser));

                        // verify SetChassisManagerAssetInfo has set the Fru
                        if (userPassed)
                        {
                            pdbAssetInfo = this.Channel.GetPdbAssetInfo();
                            this.VerifyCmOrPdbAssetInfo(ref userPassed, false, (int)TestUser, string.Empty);
                        }

                        ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: Command sets Fru correctly for user {1}", currentApi, TestUser));
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("400") && TestUser != WCSSecurityRole.WcsCmAdmin)
                        {
                            userPassed = true;
                            ChassisManagerTestHelper.IsTrue(userPassed, string.Format("{0}: Command returns correct exception for user {1}", currentApi, TestUser));
                        }
                        else
                        {
                            ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                            allPassed = false;
                            return allPassed;
                        }
                    }

                    allUsersPassed &= userPassed;

                }
                ChassisManagerTestHelper.IsTrue(allUsersPassed, string.Format("{0}: All users can execute the command", currentApi));
                allPassed &= allUsersPassed;

                // Verify SetPdbAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(10173)
                // Verify SetPdbAssetInfo command should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4713)
                VerifyCmOrPdbFruWritesRemaining(ref allPassed, false);

                if (allPassed)
                {
                    // Verify SetPdbAssetInfo can only set first two fields of payload : WorkItem(4707)
                    bool testPassed = true;

                    string payload = AssetManagementConstants.valid1CharString;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.valid1CharString, AssetManagementConstants.valid1CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid1CharString,valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = AssetManagementConstants.valid20CharStringWithSpaces;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.valid20CharStringWithSpaces, AssetManagementConstants.valid20CharStringWithSpaces);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces,valid20CharStringWithSpaces'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1},{2}", 
                        AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,valid56CharString,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},,{1}", 
                        AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},,", AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command passes for payload 'valid56CharString,,'", currentApi));
                    allPassed &= testPassed;

                    // Verify SetPdbAssetInfo sets truncated fields if fields are more than 56 characters : WorkItem(4710)
                    testPassed = true;

                    payload = AssetManagementConstants.invalid62CharString;
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format(",{0}", AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload ',invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},", AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.valid1CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid1CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid1CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid1CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.valid56CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid56CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", 
                        AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid56CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid56CharString'", currentApi));
                    allPassed &= testPassed;

                    payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.invalid62CharString);
                    this.VerifySetCmOrPdbAssetInfo(ref testPassed, false, (int)WCSSecurityRole.WcsCmAdmin, payload);
                    ChassisManagerTestHelper.IsTrue(testPassed,
                        string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,invalid62CharString'", currentApi));
                    allPassed &= testPassed;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: SetBladeAssetInfo 
        /// The test case verifies:
        /// The command returns a success completion code;
        /// Correct FRU Information is changed 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetBladeAssetInfoTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "SetBladeAssetInfo";

            BladeMultiRecordResponse setBladeAssetInfoResponse = null;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            try
            {
                for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                {
                    this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

                    setBladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(bladeId, string.Empty);

                    if (this.EmptyLocations != null && this.EmptyLocations.Contains(bladeId))
                    {
                        allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, setBladeAssetInfoResponse.completionCode,
                            string.Format("{1}: Completion Code Failure for bladeId {0} - Empty slot", bladeId.ToString(), currentApi));
                        continue;
                    }
                    // GetBladeAssetInfo returns completion code CommandNotValidForBlade if JBOD : WorkItem(8684)
                    else if (this.JbodLocations != null && this.JbodLocations.Contains(bladeId))
                    {
                        allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, setBladeAssetInfoResponse.completionCode,
                            string.Format("{1}: Completion Code CommandNotValidForBlade for bladeId {0} - JBOD", bladeId.ToString(), currentApi));
                        continue;
                    }
                    else // is server blade
                    {
                        // SetBladeAssetInfo returns completion code Success with correct FRU information for WcsCmAdmin users if server blade : WorkItem(4755)
                        bool bladePassed = true;
                        bool userPassed = false;
                        foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                        {
                            CmTestLog.Info(string.Format("Calling {0} for user {1}", currentApi, TestUser));

                            try
                            {
                                this.TestChannelContext = this.ListTestChannelContexts[(int)TestUser];
                                
                                setBladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(bladeId, string.Empty);

                                // verify Completion Code Success
                                userPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, setBladeAssetInfoResponse.completionCode,
                                    string.Format("{0}: Completion Code Success for user {1} and blade {2}", currentApi, TestUser, bladeId));

                                // verify SetBladeAssetInfo has set the Fru
                                if (userPassed)
                                {
                                    this.VerifyBladeAssetInfo(ref userPassed, bladeId, (int)TestUser, string.Empty);
                                }

                                ChassisManagerTestHelper.IsTrue(userPassed,
                                    string.Format("{0}: Command sets Fru correctly for user {1} and blade {2}", currentApi, TestUser, bladeId));
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("400") && TestUser != WCSSecurityRole.WcsCmAdmin)
                                {
                                    userPassed = true;
                                    ChassisManagerTestHelper.IsTrue(userPassed,
                                        string.Format("{0}: Command returns correct exception for user {1} and blade {2}", currentApi, TestUser, bladeId));
                                }
                                else
                                {
                                    ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                                    allPassed = false;
                                    return allPassed;
                                }
                            }

                            bladePassed &= userPassed;
                        }

                        allPassed &= bladePassed;

                        // Verify SetBladeAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(10174)
                        // Verify SetBladeAssetInfo should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4712)
                        VerifyBladeFruWritesRemaining(ref allPassed, bladeId);

                        if (bladePassed)
                        {
                            // Verify SetBladeAssetInfo can only set first two fields of payload : WorkItem(4706)
                            bool testPassed = true;

                            string payload = AssetManagementConstants.valid1CharString;
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid1CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", 
                                AssetManagementConstants.valid1CharString, AssetManagementConstants.valid1CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid1CharString,valid1CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = AssetManagementConstants.valid20CharStringWithSpaces;
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}",
                                AssetManagementConstants.valid20CharStringWithSpaces, AssetManagementConstants.valid20CharStringWithSpaces);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid20CharStringWithSpaces,valid20CharStringWithSpaces'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1},{2}", 
                                AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid56CharString,valid56CharString,valid56CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},,{1}", 
                                AssetManagementConstants.valid56CharString, AssetManagementConstants.valid56CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid56CharString,,valid56CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},,", AssetManagementConstants.valid56CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command passes for payload 'valid56CharString,,'", currentApi));
                            allPassed &= testPassed;

                            // Verify SetBladeAssetInfo sets truncated fields if fields are more than 56 characters : WorkItem(4709)
                            testPassed = true;

                            payload = AssetManagementConstants.invalid62CharString;
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format(",{0}", AssetManagementConstants.invalid62CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload ',invalid62CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},", AssetManagementConstants.invalid62CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", AssetManagementConstants.valid1CharString, AssetManagementConstants.invalid62CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid1CharString,invalid62CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid1CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid1CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", AssetManagementConstants.valid56CharString, AssetManagementConstants.invalid62CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'valid56CharString,invalid62CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.valid56CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,valid56CharString'", currentApi));
                            allPassed &= testPassed;

                            payload = string.Format("{0},{1}", AssetManagementConstants.invalid62CharString, AssetManagementConstants.invalid62CharString);
                            this.VerifySetBladeAssetInfo(ref testPassed, bladeId, (int)WCSSecurityRole.WcsCmAdmin, payload);
                            ChassisManagerTestHelper.IsTrue(testPassed,
                                string.Format("{0}: Command truncates fields greater than 56 characters for payload 'invalid62CharString,invalid62CharString'", currentApi));
                            allPassed &= testPassed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, string.Format("Exception: {0}", ex.Message));
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        private void VerifyCmOrPdbAssetInfo(ref bool cmOrPdbPassed, bool verifyingCmAssetInfo, int user, string payload)
        {
            int maxStringLength = 56;
            string propertyValue;
            string currentApi = verifyingCmAssetInfo ? "GetChassisManagerAssetInfo" : "GetPdbAssetInfo";
            ChassisAssetInfoResponse cmOrPdbAssetInfo = new ChassisAssetInfoResponse();

            this.TestChannelContext = this.ListTestChannelContexts[user];

            CmTestLog.Info(string.Format("Calling " + currentApi));

            if (verifyingCmAssetInfo)
            {
                cmOrPdbAssetInfo = this.TestChannelContext.GetChassisManagerAssetInfo();
            }
            else
            {
                cmOrPdbAssetInfo = this.TestChannelContext.GetPdbAssetInfo();
            }

            // get completion code success
            cmOrPdbPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmOrPdbAssetInfo.completionCode,
                string.Format("{0}: Completion Code Success", currentApi));

            if (cmOrPdbPassed)
            {
                // Get CM or PDB Fru XML Sample
                string fruDefFileDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
                string fruFileName = verifyingCmAssetInfo ? Path.Combine(fruDefFileDir, "ChassisManagerFruSample.xml") : Path.Combine(fruDefFileDir, "PdbFruSample.xml");

                if (!File.Exists(fruFileName))
                {
                    throw new ApplicationException(string.Format("{0}: Sample Xml file is NOT found under the path {1}", currentApi, fruDefFileDir));
                }

                // Extract CmFruSample Xml file
                XmlReader cmOrPdbFruXml = XmlReader.Create(fruFileName);

                cmOrPdbFruXml.ReadToFollowing("chassisAreaPartNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.chassisAreaPartNumber,
                    string.Format("{0}: Received Chassis Area Part Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("chassisAreaSerialNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.chassisAreaSerialNumber,
                    string.Format("{0}: Received Chassis Area Serial Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("boardAreaManufacturerName");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.boardAreaManufacturerName,
                    string.Format("{0}: Received Board Area Manufacturer Name", currentApi));

                cmOrPdbFruXml.ReadToFollowing("boardAreaManufacturerDate");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                Match propertyMatch = Regex.Match(propertyValue, @"0[1-9]|[1-12]/0[0-9]|[0-31]/[1-2][09][01789]\d\s\d?\d:\d?\d:\d?\d\s[aApP][mM]");
                cmOrPdbPassed &= ChassisManagerTestHelper.IsTrue(propertyMatch.Success,
                    string.Format("{0}: Received Board Area Manufacturer Date", currentApi));

                cmOrPdbFruXml.ReadToFollowing("boardAreaProductName");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.boardAreaProductName,
                    string.Format("{0}: Received Board Area Product Name", currentApi));

                cmOrPdbFruXml.ReadToFollowing("boardAreaSerialNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.boardAreaSerialNumber,
                    string.Format("{0}: Received Board Area Serial Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("boardAreaPartNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.boardAreaPartNumber,
                    string.Format("{0}: Received Board Area Part Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaManufactureName");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaManufactureName,
                    string.Format("{0}: Received Product Area Manufacture Name", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaProductName");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaProductName,
                    string.Format("{0}: Received Product Area Product Name", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaPartModelNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaPartModelNumber,
                    string.Format("{0}: Received Product Area Part Model Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaProductVersion");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaProductVersion,
                    string.Format("{0}: Received Product Area Product Version", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaSerialNumber");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaSerialNumber,
                    string.Format("{0}: Received Product Area Serial Number", currentApi));

                cmOrPdbFruXml.ReadToFollowing("productAreaAssetTag");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.productAreaAssetTag,
                    string.Format("{0}: Received Product Area Asset Tag", currentApi));

                cmOrPdbFruXml.ReadToFollowing("manufacturer");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.manufacturer,
                    string.Format("{0}: Received Manufacturer", currentApi));

                cmOrPdbFruXml.ReadToFollowing("serviceVersion");
                propertyValue = cmOrPdbFruXml.ReadElementContentAsString();
                cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, cmOrPdbAssetInfo.serviceVersion,
                    string.Format("{0}: Received Service Version", currentApi));

                // Verify multirecord fields are set
                if (payload != null)
                {
                    string[] payLoadFields = payload.Split(',').Select(field => field.Trim()).ToArray();
                    
                    int fieldCount = 0;
                    string expectedField = null;

                    // Verify payLoad matches MultiRecordFields (only first two fields allowed)
                    if (cmOrPdbAssetInfo.multiRecordFields.Count() <= 2)
                    {
                        foreach (string actualField in cmOrPdbAssetInfo.multiRecordFields)
                        {
                            if (fieldCount >= 2)
                                continue;
                            
                            if (payLoadFields[fieldCount].Length > maxStringLength)
                            {
                                expectedField = payLoadFields[fieldCount].Substring(0, maxStringLength);
                            }
                            else
                            {
                                expectedField = payLoadFields[fieldCount];
                            }

                            if (expectedField == AssetManagementConstants.valid20CharStringWithSpaces)
                                expectedField = "This is a test";

                            cmOrPdbPassed &= ChassisManagerTestHelper.AreEqual(expectedField, actualField,
                                string.Format("{0}{1}", currentApi, string.Format(": Received Field {0} '{1}'", fieldCount.ToString(), payLoadFields[fieldCount])));
                            fieldCount++;
                        }
                    }
                    else
                    {
                        CmTestLog.Failure(string.Format("{0}: Command exceeded number of MultiRecord Fields allowed", currentApi));
                    }
                }

                // Close XmlReader
                cmOrPdbFruXml.Close();
            }
        }

        private void VerifyBladeAssetInfo(ref bool bladePassed, int bladeId, int user, string payload)
        {
            bool isTrue;
            int maxStringLength = 56;
            string currentApi = "GetBladeAssetInfo";
            string propertyValue;

            BladeAssetInfoResponse bladeAssetInfo = new BladeAssetInfoResponse();

            this.TestChannelContext = this.ListTestChannelContexts[user];

            CmTestLog.Info("Calling " + currentApi);

            bladeAssetInfo = this.TestChannelContext.GetBladeAssetInfo(bladeId);

            // get completion code success
            bladePassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeAssetInfo.completionCode,
                string.Format(currentApi + ": Completion Code Success for bladeId {0}", bladeId.ToString()));

            if (bladePassed)
            {
                // Get Blade Fru XML Sample
                string fruDefFileDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
                string fruFileName = Path.Combine(fruDefFileDir, "BladeFruSample.xml");

                if (!File.Exists(fruFileName))
                {
                    throw new ApplicationException(string.Format("{0}: Sample Xml file is NOT found under the path {1}",
                        currentApi, fruDefFileDir));
                }

                // Extract BladeFruSample Xml file
                XmlReader bladeFruXml = XmlReader.Create(fruFileName);

                bladePassed &= ChassisManagerTestHelper.AreEqual(bladeId, bladeAssetInfo.bladeNumber,
                    string.Format(currentApi + ": Received Blade Number for bladeId {0}", bladeId));

                bladeFruXml.ReadToFollowing("chassisAreaPartNumber");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.chassisAreaPartNumber,
                    string.Format(currentApi + ": Received Chassis Area Part Number for blade {0}", bladeId));

                isTrue = bladeAssetInfo.chassisAreaSerialNumber.Length <= 16;
                bladePassed &= ChassisManagerTestHelper.IsTrue(isTrue,
                    string.Format(currentApi + ": Received Chassis Area Serial Number for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("boardAreaManufacturerName");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.boardAreaManufacturerName,
                    string.Format(currentApi + ": Received Board Area Manufacturer Name for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("boardAreaManufacturerDate");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                Match propertyMatch = Regex.Match(propertyValue, @"0[1-9]|[1-12]/0[0-9]|[0-31]/[1-2][09][01789]\d\s\d?\d:\d?\d:\d?\d\s[aApP][mM]");
                bladePassed &= ChassisManagerTestHelper.IsTrue(propertyMatch.Success,
                    string.Format(currentApi + ": Received Board Area Manufacturer Date for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("boardAreaProductName");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.boardAreaProductName,
                    string.Format(currentApi + ": Received Board Area Product Name for blade {0}", bladeId));

                isTrue = bladeAssetInfo.boardAreaSerialNumber.Length <= 16;
                bladePassed &= ChassisManagerTestHelper.IsTrue(isTrue,
                    string.Format(currentApi + ": Received Board Area Serial Number for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("boardAreaPartNumber");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.boardAreaPartNumber,
                    string.Format(currentApi + ": Received Board Area Part Number for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("productAreaManufactureName");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.productAreaManufactureName,
                    string.Format(currentApi + ": Received Product Area Manufacture Name for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("productAreaProductName");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.productAreaProductName,
                    string.Format(currentApi + ": Received Product Area Product Name for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("productAreaPartModelNumber");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.productAreaPartModelNumber,
                    string.Format("GetBladeAssetInfo: Received Product Area Part Model Number for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("productAreaProductVersion");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.productAreaProductVersion,
                    string.Format(currentApi + ": Received Product Area Product Version for blade {0}", bladeId));

                isTrue = bladeAssetInfo.productAreaSerialNumber.Length <= 16;
                bladePassed &= ChassisManagerTestHelper.IsTrue(isTrue,
                    string.Format(currentApi + ": Received Product Area Serial Number for blade {0}", bladeId));

                isTrue = bladeAssetInfo.productAreaAssetTag.Length <= 10;
                bladePassed &= ChassisManagerTestHelper.IsTrue(isTrue,
                    string.Format(currentApi + ": Received Product Area Asset Tag for blade {0}", bladeId));

                bladeFruXml.ReadToFollowing("manufacturer");
                propertyValue = bladeFruXml.ReadElementContentAsString();
                bladePassed &= ChassisManagerTestHelper.AreEqual(propertyValue, bladeAssetInfo.manufacturer,
                    string.Format(currentApi + ": Received Manufacturer for blade {0}", bladeId));

                // Verify multirecord fields are set
                if (payload != null)
                {
                    string[] payLoadFields = payload.Split(',').Select(field => field.Trim()).ToArray();

                    int fieldCount = 0;
                    string expectedField = null;

                    // Verify payLoad matches MultiRecordFields (only first two fields allowed)
                    if (bladeAssetInfo.multiRecordFields.Count() <= 2)
                    {
                        foreach (string actualField in bladeAssetInfo.multiRecordFields)
                        {
                            if (fieldCount >= 2)
                                continue;

                            if (payLoadFields[fieldCount].Length > maxStringLength)
                                expectedField = payLoadFields[fieldCount].Substring(0, maxStringLength);
                            else
                                expectedField = payLoadFields[fieldCount];

                            if (expectedField == AssetManagementConstants.valid20CharStringWithSpaces)
                                expectedField = "This is a test";

                            bladePassed &= ChassisManagerTestHelper.AreEqual(expectedField, actualField,
                                string.Format(currentApi + ": Received Field {0} '{1}'", fieldCount.ToString(), payLoadFields[fieldCount]));
                            fieldCount++;
                        }
                    }
                    else
                    {
                        CmTestLog.Failure(currentApi + ": Command exceeded number of MultiRecord Fields allowed");
                    }
                }

                // Close Xml Reader
                bladeFruXml.Close();
            }
        }

        private void VerifySetCmOrPdbAssetInfo(ref bool allPassed, bool verifyingSetCmAssetInfo, int user, string payload)
        {
            bool testPassed = true;
            string currentApi = verifyingSetCmAssetInfo ? "SetChassisManagerAssetInfo" : "SetPdbAssetInfo";
            MultiRecordResponse setCmOrPdbAssetInfo;

            // Verify SetChassisManagerAssetInfo can only set first two fields of payload
            this.TestChannelContext = this.ListTestChannelContexts[user];
            CmTestLog.Info(string.Format("{0}: payload is '{1}'", currentApi, payload));

            if (verifyingSetCmAssetInfo)
            {
                setCmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(payload);
            }
            else
            {
                setCmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(payload);
            }

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, setCmOrPdbAssetInfo.completionCode,
                string.Format("{0}: Completion Code Success", currentApi));
            allPassed &= testPassed;

            if (testPassed)
            {
                this.VerifyCmOrPdbAssetInfo(ref testPassed, verifyingSetCmAssetInfo, user, payload);
            }
            allPassed &= testPassed;
        }

        private void VerifySetBladeAssetInfo(ref bool allPassed, int bladeId, int user, string payload)
        {
            bool testPassed = true;
            string currentApi = "SetBladeAssetInfo";
            BladeMultiRecordResponse setBladeAssetInfo;

            // Verify SetChassisManagerAssetInfo can only set first two fields of payload
            this.TestChannelContext = this.ListTestChannelContexts[user];
            CmTestLog.Info(string.Format("{0}: payload is '{1}' for blade {2}", currentApi, payload, bladeId));

            setBladeAssetInfo = this.TestChannelContext.SetBladeAssetInfo(bladeId, payload);

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, setBladeAssetInfo.completionCode,
                string.Format("{0}: Completion Code Success", currentApi));
            allPassed &= testPassed;

            if (testPassed)
            {
                this.VerifyBladeAssetInfo(ref testPassed, bladeId, user, payload);
            }
            allPassed &= testPassed;
        }

        private void VerifyCmOrPdbFruWritesRemaining(ref bool allPassed, bool verifyingSetCmAssetInfo)
        {
            bool testPassed;
            string currentApi = verifyingSetCmAssetInfo ? "SetChassisManagerAssetInfo" : "SetPdbAssetInfo";
            MultiRecordResponse cmOrPdbAssetInfo = new MultiRecordResponse();

            // Initialize FRU Writes Remaining Dictionary KeyValue Pair
            Dictionary<string, string> fruWritesRemainingKeyValue = new Dictionary<string, string>
            {
                {"ResetMultiRecordFruWritesRemaining", "0"}
            };

            // Set Channel for WcsCmAdmin User
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }


            // Fail FRU Writes Remaining before starting test
            int writeCount = 0;
            while (cmOrPdbAssetInfo.completionCode != CompletionCode.WriteFruZeroWritesRemaining &&
                writeCount < (AssetManagementConstants.fruWritesRemaining + 1))
            {
                if (verifyingSetCmAssetInfo)
                    cmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);
                else
                    cmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(string.Empty);

                if ((cmOrPdbAssetInfo.completionCode != CompletionCode.Success)
                    && (cmOrPdbAssetInfo.completionCode != CompletionCode.WriteFruZeroWritesRemaining))
                {
                    CmTestLog.Failure(string.Format("{0}: Command returned Completion Code {1} while trying to return {2}",
                        currentApi, Enum.GetName(typeof(CompletionCode), cmOrPdbAssetInfo.completionCode), "WriteFruZeroWritesRemaining"));
                    allPassed = false;
                    return;
                }
                writeCount++;
            }
            if (cmOrPdbAssetInfo.completionCode != CompletionCode.WriteFruZeroWritesRemaining)
            {
                CmTestLog.Failure(string.Format("{0}: Command returned Completion Code {1} while trying to return {2}",
                        currentApi, Enum.GetName(typeof(CompletionCode), cmOrPdbAssetInfo.completionCode), "WriteFruZeroWritesRemaining"));
                allPassed = false;
                return;
            }

            CmTestLog.Info(string.Format("{0}: FRU Writes Remaining set to 0", currentApi));

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            // Verify SetChassisManagerAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(8709)
            // Verify SetPdbAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(10173)
            
            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "1"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "1";
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            if (verifyingSetCmAssetInfo)
                cmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);
            else
                cmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(string.Empty);

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmOrPdbAssetInfo.completionCode,
                string.Format("{0}: Command resets number of writes to default value", currentApi));
            allPassed &= testPassed;

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            if (!allPassed)
            {
                RestartCmService(currentApi);
                return;
            }

            // Starting test: Set command fails for attempting to set payload with 0 writes remaining
            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "0"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "0";

            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            // Verify SetChassisManagerAssetInfo command should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4711)
            // Verify SetPdbAssetInfo command should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4713)
            for (int callApiCount = 0; callApiCount < 255; callApiCount++)
            {
                if (verifyingSetCmAssetInfo)
                    cmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);
                else
                    cmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(string.Empty);

                if (cmOrPdbAssetInfo.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure(string.Format("{0}: Command returns Completion Code {1} on callApiCount {2} before {3}",
                        currentApi, Enum.GetName(typeof(CompletionCode), cmOrPdbAssetInfo.completionCode),
                        callApiCount, "WriteFruZeroWritesRemaining"));
                    allPassed = false;
                    return;
                }
            }

            if (verifyingSetCmAssetInfo)
                cmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);
            else
                cmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(string.Empty);

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.WriteFruZeroWritesRemaining, cmOrPdbAssetInfo.completionCode,
                string.Format("{0}: Command returns Completion Code {1} after 256 calls",
                currentApi, "WriteFruZeroWritesRemaining"));
            allPassed &= testPassed;

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "1"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "1";
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            if (verifyingSetCmAssetInfo)
                cmOrPdbAssetInfo = this.TestChannelContext.SetChassisManagerAssetInfo(string.Empty);
            else
                cmOrPdbAssetInfo = this.TestChannelContext.SetPdbAssetInfo(string.Empty);
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmOrPdbAssetInfo.completionCode,
                string.Format("{0}: FRU Writes Remaining reset", currentApi));

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
            }

            return;
        }

        private void VerifyBladeFruWritesRemaining(ref bool allPassed, int BladeId)
        {
            bool testPassed;
            string currentApi = "SetBladeAssetInfo";
            BladeMultiRecordResponse bladeAssetInfoResponse = new BladeMultiRecordResponse();

            // Initialize FRU Writes Remaining Dictionary KeyValue Pair
            Dictionary <string, string> fruWritesRemainingKeyValue = new Dictionary<string,string>
            {
                {"ResetMultiRecordFruWritesRemaining", "0"}
            };

            // Set Channel for WcsCmAdmin User
            this.TestChannelContext = this.ListTestChannelContexts[(int)WCSSecurityRole.WcsCmAdmin];

            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            // Fail FRU Writes Remaining before starting test
            int writeCount = 0;
            while (bladeAssetInfoResponse.completionCode != CompletionCode.WriteFruZeroWritesRemaining && 
                writeCount < (AssetManagementConstants.fruWritesRemaining + 1))
            {
                bladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(BladeId, string.Empty); 

                if ((bladeAssetInfoResponse.completionCode != CompletionCode.Success)
                    && (bladeAssetInfoResponse.completionCode != CompletionCode.WriteFruZeroWritesRemaining))
                {
                    CmTestLog.Failure(string.Format("{0}: Command returned Completion Code {1} while trying to return {2} for blade {3}",
                        currentApi, Enum.GetName(typeof(CompletionCode), bladeAssetInfoResponse.completionCode), 
                        "WriteFruZeroWritesRemaining", BladeId));
                    allPassed = false;
                    return;
                }
                writeCount++;
            }
            if (bladeAssetInfoResponse.completionCode != CompletionCode.WriteFruZeroWritesRemaining)
            {
                CmTestLog.Failure(string.Format("{0}: Command returned Completion Code {1} while trying to return {2} for blade {3}",
                        currentApi, Enum.GetName(typeof(CompletionCode), bladeAssetInfoResponse.completionCode),
                        "WriteFruZeroWritesRemaining", BladeId));
                allPassed = false;
                return;
            }

            CmTestLog.Info(string.Format("{0}: FRU Writes Remaining set to 0 for blade {1}", currentApi, BladeId));

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            // Verify SetBladeAssetInfo resets number of writes to default value after configuring App.Config : WorkItem(10174)
            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "1"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "1";
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            bladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(BladeId, string.Empty);

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeAssetInfoResponse.completionCode,
                string.Format("{0}: Command resets number of writes to default value for blade {1}",
                currentApi, BladeId));
            allPassed &= testPassed;

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            if (!allPassed)
            {
                RestartCmService(currentApi);
                return;
            }

            // Starting test: SetBladeAssetInfo fails for attempting to set payload with 0 writes remaining
            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "0"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "0";

            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            // Verify SetBladeAssetInfo should return WritFruZeroWritesRemaining after 256 iterations : WorkItem(4712)
            for (int callApiCount = 0; callApiCount < 255; callApiCount++)
            {
                bladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(BladeId, string.Empty);

                if (bladeAssetInfoResponse.completionCode != CompletionCode.Success)
                {
                    CmTestLog.Failure(string.Format("{0}: Command returns Completion Code {1} on callApiCount {2} for blade {3} before {4}",
                        currentApi, Enum.GetName(typeof(CompletionCode), bladeAssetInfoResponse.completionCode), 
                        callApiCount, BladeId, "WriteFruZeroWritesRemaining"));
                    allPassed = false;
                    return;
                }
            }

            bladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(BladeId, string.Empty);

            testPassed = ChassisManagerTestHelper.AreEqual(CompletionCode.WriteFruZeroWritesRemaining, bladeAssetInfoResponse.completionCode,
                string.Format("{0}: Command returns Completion Code {1} after 256 calls for blade {2}",
                currentApi, "WriteFruZeroWritesRemaining", BladeId));
            allPassed &= testPassed;

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            // Set App.Config key "ResetMultiRecordFruWritesRemaining" to value "1"
            fruWritesRemainingKeyValue["ResetMultiRecordFruWritesRemaining"] = "1";
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, false))
            {
                CmTestLog.Failure(string.Format("{0}: Setting App.Config failed for KeyValue '{1},{2}'",
                    currentApi, fruWritesRemainingKeyValue.Keys.First(), fruWritesRemainingKeyValue.Values.First()));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
                return;
            }

            bladeAssetInfoResponse = this.TestChannelContext.SetBladeAssetInfo(BladeId, string.Empty);
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeAssetInfoResponse.completionCode,
                string.Format("{0}: FRU Writes Remaining reset for blade {1}", currentApi, BladeId));

            // Restore App.Config with Original Values
            if (!ConfigureAppConfig(fruWritesRemainingKeyValue, true))
            {
                CmTestLog.Failure(string.Format("{0}: App.Config cleanup failed", currentApi));
                allPassed = false;
                return;
            }

            // Restart CM Service
            if (!RestartCmService(currentApi))
            {
                allPassed = false;
            }

            return;
        }
    }
}
