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
    using System.Threading;
    using System.Xml;
    using Microsoft.GFS.WCS.Contracts;

    internal class BladeCommands : CommandBase
    {
        private readonly string skuDefinitionFile;            

        internal BladeCommands(IChassisManager channel) : base(channel)
        {
            string skuDefFileDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
            this.skuDefinitionFile = Path.Combine(skuDefFileDir, "BladeHealthSample.xml");
            
            if (!File.Exists(this.skuDefinitionFile))
            {
                throw new ApplicationException(string.Format("BladeHealthSample.xml file is NOT found under the path:{0}", skuDefFileDir));
            }
        }

        internal BladeCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts) : base(channel, testChannelContexts)
        {
            string skuDefFileDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
            this.skuDefinitionFile = Path.Combine(skuDefFileDir, "BladeHealthSample.xml");

            if (!File.Exists(this.skuDefinitionFile))
            {
                throw new ApplicationException(string.Format("BladeHealthSample.xml file is NOT found under the path:{0}", skuDefFileDir));
            }
        }

        /// <summary>
        /// Test Command: GetAllBladesInfo. The test case verifies:
        /// The command completion code is a success;
        /// The command returns all blades info in the chassis (server, jbod and Empty);
        /// The command returns failure for power-off blades, and returns success for
        /// blade-off blades.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetAllBladesInfoTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();
            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and get all blades info
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    CmTestLog.Info(string.Format("GetAllBladesInfo with user type {0} \n", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    testPassed = this.GetAllBladesInfo(testPassed, roleId);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }
            }

            return testPassed;
        }

        public bool GetAllBladesInfo(bool testPassed, WCSSecurityRole roleId)
        {
            try
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    // get all blades info
                    GetAllBladesInfoResponse allBladesInfoResponse = null;
                    CmTestLog.Info("Trying to get all blades information");
                    allBladesInfoResponse = this.TestChannelContext.GetAllBladesInfo();
                    if (allBladesInfoResponse != null && allBladesInfoResponse.completionCode == CompletionCode.Success)
                    {
                        testPassed &= ChassisManagerTestHelper.IsTrue(allBladesInfoResponse != null, "Received GetAllBladesInfo response");
                        testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesInfoResponse.completionCode, "GetAllBladesInfo returns success as the completion code");
                    }
                    else
                    {
                        testPassed = false;
                        return testPassed;
                    }
                    testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.Population, allBladesInfoResponse.bladeInfoResponseCollection.Count,
                        string.Format("The response contains information for all {0} slots", CmConstants.Population));

                    // WorkItem(2839)
                    CmTestLog.Info("Verifying each blade info response");
                    foreach (var bladeInfo in allBladesInfoResponse.bladeInfoResponseCollection)
                    {
                        if (this.EmptyLocations != null && this.EmptyLocations.Contains(bladeInfo.bladeNumber))
                        {
                            testPassed = this.VerifyEmptybladeInfo(testPassed, bladeInfo);
                        }
                        // If the slot is JBOD: WorkItem(2748)
                        else if (this.JbodLocations != null && this.JbodLocations.Contains(bladeInfo.bladeNumber))
                        {
                            testPassed = this.VerifyJbodBladeInfo(testPassed, bladeInfo);
                        }
                        else
                        {
                            testPassed = this.VerifyServerBladeInfo(testPassed, bladeInfo);
                        }
                    }

                    if (!testPassed)
                    {
                        CmTestLog.End(false);
                        return false; // if failed here, no need to continue
                    }

                    int[] nonEmptyBlades;
                    int powerOffBladeId;
                    this.PowerOffRandomBlade(allBladesInfoResponse, out nonEmptyBlades, out powerOffBladeId);

                    // verify the blade powered off returns a failure
                    CmTestLog.Info("Trying to get all blades information");
                    allBladesInfoResponse = this.TestChannelContext.GetAllBladesInfo();
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.DevicePoweredOff, allBladesInfoResponse.bladeInfoResponseCollection
                                                                                                                          .Single(blade => blade.bladeNumber == powerOffBladeId)
                                                                                                                          .completionCode, "Power-off blade returns DevicePoweredOff");

                    // blade off a random blade
                    int bladeOffBladeId;
                    this.BladeOffRandomBlade(nonEmptyBlades, out bladeOffBladeId);

                    // verify the blade with state off still returns success
                    CmTestLog.Info("Trying to get all blades information");
                    allBladesInfoResponse = this.TestChannelContext.GetAllBladesInfo();
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesInfoResponse.bladeInfoResponseCollection
                                                                                                                 .Single(blade => blade.bladeNumber == bladeOffBladeId)
                                                                                                                 .completionCode, "Blade-off blade returns Success");
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
        /// Test Command: GetBladeInfo. The test case verifies:
        /// The command completion code is a success for server and JBOD;
        /// The command returns TimeOut for Empty Slots);
        /// The command returns Devicepowered off for power-off blades, and returns success for
        /// blade-off blades.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetBladeInfoTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();
            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and get blade info
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    CmTestLog.Info(string.Format("GetBladeInfo with user type {0} \n", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    testPassed &= this.GetBladeInfo(testPassed, roleId);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }
            }

            return testPassed;
        }

        public bool GetBladeInfo(bool testPassed, WCSSecurityRole roleId)
        {
            try
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    // TFS WorkItem(1895)
                    BladeInfoResponse bladeInfo = null;
                    // get blade information for each blade
                    for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                    {
                        // TFS WorkItem(1897)
                        if (this.EmptyLocations != null && this.EmptyLocations.Contains(bladeId))
                        {
                            bladeInfo = this.TestChannelContext.GetBladeInfo(bladeId);
                            testPassed = this.VerifyEmptybladeInfo(testPassed, bladeInfo);
                        }
                        // TFS WorkItem(2747)
                        else if (this.JbodLocations != null && this.JbodLocations.Contains(bladeId))
                        {
                            bladeInfo = this.TestChannelContext.GetBladeInfo(bladeId);
                            testPassed = this.VerifyJbodBladeInfo(testPassed, bladeInfo);
                        }
                        else
                        {
                            bladeInfo = this.TestChannelContext.GetBladeInfo(bladeId);
                            testPassed = this.VerifyServerBladeInfo(testPassed, bladeInfo);
                        }
                        if (!testPassed)
                        {
                            CmTestLog.End(false);
                            return false; // if failed here, no need to continue
                        }
                    }

                    // verify the blade for out of range bladeId
                    CmTestLog.Info("Trying to get information for invalid blade");
                    bladeInfo = this.TestChannelContext.GetBladeInfo(CmConstants.InvalidBladeId);
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, bladeInfo.completionCode, "Inavlid blade returns ParameterOutOfRange");

                    if (!testPassed)
                    {
                        CmTestLog.End(false);
                        return false; // if failed here, no need to continue
                    }

                    // TFS WorkItem(2854)
                    // Power-off a random blade
                    int powerOffBladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Trying to power off Blade# {0}", powerOffBladeId));

                    if (!this.SetPowerState(PowerState.OFF, powerOffBladeId))
                    {
                        return false;
                    }

                    // verify the blade powered off returns a DevicePoweredOFF
                    CmTestLog.Info("Trying to get blade information for powered off blade");
                    bladeInfo = this.TestChannelContext.GetBladeInfo(powerOffBladeId);
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.DevicePoweredOff, bladeInfo.completionCode, "Power-off blade returns DevicePoweredOff");

                    // blade off a random blade
                    int bladeOffBladeId;
                    this.BladeOffRandomBlade(ServerLocations, out bladeOffBladeId);

                    // verify the blade with state off still returns success
                    CmTestLog.Info("Trying to get blade information");
                    bladeInfo = this.TestChannelContext.GetBladeInfo(bladeOffBladeId);
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeInfo.completionCode, "Blade-off blade returns Success");
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
        /// Test Command: GetBladeHealth and it takes bladeId all the parameters as true. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// The command returns full health information for server ;
        /// The command returns full health information for jbod ;
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetBladeHealthTest()
        {
            CmTestLog.Start();
            bool allPassed = true;

            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            // Loop through different user types and get blade health
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    CmTestLog.Info(string.Format("GetBladeHealth with user type {0} \n", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPassed = this.GetBladeHealth(allPassed, roleId);
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    allPassed = false;
                }
            }

            return allPassed;
        }

        public bool GetBladeHealth(bool allPassed, WCSSecurityRole roleId)
        {
            try
            {
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];
                if (this.TestChannelContext != null)
                {
                    // get health information for each blade
                    for (int bladeId = 1; bladeId <= CmConstants.Population; bladeId++)
                    {
                        // If the slot is empty [WorkItem(3372)]
                        if (this.EmptyLocations != null && this.EmptyLocations.Contains(bladeId))
                        {
                            CmTestLog.Info(string.Format("Slot# {0} is empty", bladeId));
                            allPassed = this.BladeHealthForEmptyBlades(ref allPassed, bladeId);
                        }
                        // If the slot is JBOD: WorkItem(3371)
                        else if (this.JbodLocations != null && this.JbodLocations.Contains(bladeId))
                        {
                            CmTestLog.Info(string.Format("Slot# {0} is JBOD", bladeId));
                            allPassed = this.VerifyJbodBladeHealth(allPassed, bladeId);

                            // Test for only DiskInformation for JBOD : WorkItem(3445)
                            allPassed = this.GetOnlyDiskInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of disk information for JBOD !!!!\n");
                        }
                        else
                        {
                            // Testing all blade health components are returned: WorkItem(4775)
                            CmTestLog.Info(string.Format("Verifying health information when all params are true for Blade# {0}", bladeId));
                            BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, true, true, true, true, true, true, true);

                            allPassed = this.GetServerHealth(allPassed, bladeHealth, bladeId);
                            CmTestLog.Info("!!!! Finished verification of Getbladehealth when all params are true !!!!\n");

                            //Test for all blade health components are returned when all params are false : WorkItem(3370)
                            CmTestLog.Info(string.Format("Verifying health information when all params are false for Blade# {0}", bladeId));
                            bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, false, false, false, false, false, false);
                            allPassed = this.GetServerHealth(allPassed, bladeHealth, bladeId);
                            CmTestLog.Info("!!!! Finished verification of Getbladehealth when all params are False !!!!\n");

                            //Test for only processor information: WorkItem(3443 & 4655)
                            allPassed = this.GetOnlyProcessorInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of processor information for GetBladeHealth !!!!\n");

                            //Test for only memory information : WorkItem(3444)
                            allPassed = this.GetOnlyMemoryInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!!  Finished verification of Memory information for GetBladeHealth !!!!\n");

                            //Test for only PCIe Information : WorkItem(3446)
                            allPassed = this.GetOnlyPCIeInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of PCIe information for GetBladeHealth !!!!\n");

                            // Test for only sensor information : WorkItem(3447)
                            allPassed = this.GetOnlySensorInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of sensor information for GetBladeHealth !!!!\n");

                            // Test for only temparature information : WorkItem(4656)
                            allPassed = this.GetOnlyTemperatureInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of temparature information for GetBladeHealth !!!!\n");

                            //Test for only FRU Information :  WorkItem(3452)
                            allPassed = this.GetOnlyFruInformationBladeHealth(allPassed, bladeId);
                            CmTestLog.Info("!!!! Finished verification of FRU information for GetBladeHealth !!!!\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        public bool CheckBladesPowerOn()
        {
            if (!this.SetPowerState(PowerState.ON) || !this.SetBladeState(PowerState.ON))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Test Command: GetBladeHealth for Empty Slots. This verifies:
        /// Command returns completion code TimeOut and BladeShell is unknown.
        /// verify cpuInfo,memInfo,diskInfo,pcieInfo,sensorInfo,fruInfo--> Should be empty
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool BladeHealthForEmptyBlades(ref bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Verifying health information for Empty Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, true, true, true, true, true, true, true);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, bladeHealth.completionCode,
                    string.Format("Received response with completion code Timeout for empty blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.bladeShell != null, string.Format("Received bladeshell for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Unknown, bladeHealth.bladeShell.completionCode,
                    string.Format("Received Unknown blade shell information for empty blade# {0}", bladeHealth.bladeNumber));

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for Empty blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for Empty blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for Empty blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for Empty blade# {0}", bladeHealth.bladeNumber));

                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only Processor information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full processor information for server.
        /// Command returns Empty for these: memInfo,diskInfo,pcieInfo,sensorInfo,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyProcessorInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format(" Verifying only processor information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, true, false, false, false, false, false, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                XmlReader bladehealthInfo = XmlReader.Create(this.skuDefinitionFile);

                this.VerifyProcessorInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only Memory information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full Memory information for server.
        /// Command returns Empty for these: cpuInfo,diskInfo,pcieInfo,sensorInfo,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyMemoryInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Verifying only memory information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, true, false, false, false, false, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                XmlReader bladehealthInfo = XmlReader.Create(this.skuDefinitionFile);

                this.VerifyMemoryInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only Disk information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full disk information for server.
        /// Command returns Empty for these: cpuInfo,memInfo,pcieInfo,sensorInfo,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyDiskInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Verifying only Disk information for JBOD Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, false, true, false, false, false, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                allPassed &= ChassisManagerTestHelper.AreEqual(Convert.ToInt32(ConfigurationManager.AppSettings["JbodCount"]), bladeHealth.jbodDiskInfo.diskCount, "JBOD disk count check");
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeHealth.jbodDiskInfo.completionCode,
                    string.Format("Received JbodDiskInfo with completion code success for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodDiskInfo.diskInfo.Aggregate(true, (b, info) => b & CompletionCode.Success == info.completionCode),
                    string.Format("Received all disk information for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodDiskInfo.diskInfo.Aggregate(true, (b, info) => b & "Normal" == info.diskStatus),
                    string.Format("Received all JBOD disk state is Normal for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodInfo == null, string.Format("Received null jbodInfo for blade# {0}", bladeHealth.bladeNumber));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only PCIe information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full pcieInfo information for server.
        /// Command returns Empty for these: cpuInfo,memInfo,diskinfo,sensorInfo,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyPCIeInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Trying to get only PCIe information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.Channel.GetBladeHealth(bladeId, false, false, false, true, false, false, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                XmlReader bladehealthInfo = XmlReader.Create(this.skuDefinitionFile);

                this.VerifyPCIeInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for blade# {0}", bladeHealth.bladeNumber));
                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only Sensor information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full Sensor information for server.
        /// Command returns Empty for these: cpuInfo,memInfo,diskinfo,PCIe,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlySensorInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Trying to get only Sensor information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, false, false, false, true, false, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors != null, string.Format("Received sensor Info for blade# {0}", bladeHealth.bladeNumber));
                this.VerifySensorInformation(ref allPassed, bladeHealth);

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only Temparature information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns full Temparature information for server.
        /// Command returns Empty for these: cpuInfo,memInfo,diskinfo,sensorInfo,PCIe,fruInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyTemperatureInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Trying to get only temparature information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, false, false, false, false, true, false);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

                this.VerifySensorInformation(ref allPassed, bladeHealth);

                this.VerifyFruInformationEmpty(ref allPassed, bladeHealth);
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetBladeHealth: Get Only FRU information. The test case verifies:
        /// The command returns completion code success;
        /// The blade shell is in healthy state;
        /// Command returns Serialnumber and Hardwareversion information for server.
        /// Command returns Empty for these: cpuInfo,memInfo,diskInfo,pcieInfo,sensorInfo
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool GetOnlyFruInformationBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Trying to get only FRU information for Server Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, false, false, false, false, false, false, true);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

                this.VerifyBladeShellInformation(ref allPassed, bladeHealth);
                XmlReader bladehealthInfo = XmlReader.Create(this.skuDefinitionFile);

                this.VerifyServerFruInformation(ref allPassed, bladeHealth, bladeId, bladehealthInfo);

                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for blade# {0}", bladeHealth.bladeNumber));
                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        /// <summary>
        /// Test Command: GetAllBladesState; SetAllBladesOn; SetAllBladesOff. The test case verifies:
        /// The command returns completion code success on server blades;
        /// The command returns CommandNotValidForBlade on JBOD blades;
        /// The command returns Failure on Empty blades;
        /// The command returns correct blade state when a blade is on;
        /// The command returns correct blade state when a blade is off.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>        
        public bool SetGetAllBladesStateTest()
        {
            CmTestLog.Start();

            // get server and jbod locations
            int[] serverLocations, jbodLocations;
            if (!this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations) ||
                !this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                CmTestLog.Failure("Cannot get server or jbod locations");
                CmTestLog.End(false);
                return false;
            }

            // test blade state on
            CmTestLog.Info("Trying to set all blades state to on");
            if (!this.SetBladeState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all blades on");
                CmTestLog.End(false);
                return false;
            }

            bool testPassed = true;
            GetAllBladesStateResponse response;

            CmTestLog.Info("Trying to get the blade states of all blades");
            response = this.Channel.GetAllBladesState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode, "Get all blade states");

            CmTestLog.Info("Veritying blade state for all blades");
            foreach (var bladeState in response.bladeStateResponseCollection)
            {
                // verify server blade
                if (serverLocations.Contains(bladeState.bladeNumber))
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeState.completionCode, string.Format("Server blade# {0} returns Success", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, bladeState.bladeState, string.Format("Server blade# {0} has blade state ON", bladeState.bladeNumber));
                }
                // verity jbod blade
                // [TFS WorkItem: 2725 & 2723] SetAllBladesOn: Command is not valid to run on JBOD blade &  GetAllBladesState: Command is not valid to run on JBOD blade
                else if (jbodLocations.Contains(bladeState.bladeNumber))
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeState.completionCode,
                        string.Format("JBOD blade# {0} returns CommandNotValidForBlade", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, bladeState.bladeState, string.Format("JBOD blade# {0} has blade state NA", bladeState.bladeNumber));
                }
                // Verify empty blade
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, bladeState.completionCode, string.Format("Empty blade# {0} returns TimeOut", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, bladeState.bladeState, string.Format("Empty blade# {0} has blade state NA", bladeState.bladeNumber));
                }
            }

            // test blade state off
            CmTestLog.Info("Trying to set all blade states to off");
            if (!this.SetBladeState(PowerState.OFF))
            {
                CmTestLog.Failure("Cannot set all blades off");
                CmTestLog.End(false);
                return false;
            }

            // [TFS WorkItem: 1775] GetAllBladesState: Verify command returns valid ON/OFF states only for blades present in chassis
            CmTestLog.Info("Trying to retrieve the blade states of all blades");
            response = this.Channel.GetAllBladesState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode,
                "Get all blade states");

            CmTestLog.Info("Veritying blade state for all blades");
            foreach (var bladeState in response.bladeStateResponseCollection)
            {
                // verify server blade
                if (serverLocations.Contains(bladeState.bladeNumber))
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeState.completionCode, string.Format("Server blade# {0} returns Success", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, bladeState.bladeState, string.Format("Server blade# {0} has blade state OFF", bladeState.bladeNumber));
                }
                // verify jbod blade
                // [TFS WorkItem: 2726] SetAllBladesOff: Command is not valid to run on JBOD blade
                else if (jbodLocations.Contains(bladeState.bladeNumber))
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeState.completionCode,
                        string.Format("JBOD blade# {0} returns CommandNotValidForBlade", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, bladeState.bladeState, string.Format("JBOD blade# {0} has blade state NA", bladeState.bladeNumber));
                }
                // Verify Empty blade
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, bladeState.completionCode, string.Format("Empty blade# {0} returns TimeOut", bladeState.bladeNumber));
                    testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, bladeState.bladeState, string.Format("Empty blade# {0} has blade state NA", bladeState.bladeNumber));
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: GetBladeState; SetBladeOn; SetBladeOff. The test case verifies:
        /// The command returns completion code success on server blades;
        /// The command returns CommandNotValidForBlade on JBOD blades;
        /// The command returns Failure on empty blades;
        /// The command returns correct blade state when a blade is on;
        /// The command returns correct blade state when a blade is off.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>  
        public bool SetGetBladeStateTest()
        {
            CmTestLog.Start();
            bool testPassed = true;
         
            // get server and jbod locations
            int[] serverLocations, jbodLocations;
            if (!this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations) ||
                !this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                CmTestLog.Failure("Cannot get server or jbod locations");
                CmTestLog.End(false);
                return false;
            }

            this.EmptyLocations = this.GetEmptyLocations();

            int bladeId;
            BladeResponse bladeOn;
            BladeResponse bladeOff;
            BladeStateResponse getBladeState;

            // [TFS WorkItem: 2399] GetBladeState: Verify when Blade is ON
            if (serverLocations.Length > 0)
            {
                CmTestLog.Info("Location is \n ******************* Server ******************** ");
                CmTestLog.Info("Trying to SetBladeOn for server and verifying GetBladeState");

                this.VerifySetBladeOn(serverLocations, out bladeId, out bladeOn);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeOn.completionCode, string.Format("Set Blade On for blade# {0}", bladeId));

                getBladeState = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, getBladeState.completionCode, string.Format("Received get blade state for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, getBladeState.bladeState, string.Format("Server blade# {0} has blade state ON", getBladeState.bladeNumber));

                // [TFS WorkItem: 2398] GetBladeState: Verify command returns OFF state when blade is OFF but receiving AC power
                CmTestLog.Info("Trying to SetBladeOff for server. Verifying GetBladeState and GetPowerSatate");
                bladeOff = this.Channel.SetBladeOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, getBladeState.completionCode, string.Format("Received set blade state for blade# {0}", bladeId));

                // [TFS WorkItem: 2253] SetBladeOff: SetBladeOff when blade is already off
                bladeOff = this.Channel.SetBladeOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, getBladeState.completionCode, string.Format("Received set blade state when blade is already off for blade# {0}", bladeId));

                Thread.Sleep(TimeSpan.FromSeconds(30));
                getBladeState = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, getBladeState.completionCode, string.Format("Received get blade state for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, getBladeState.bladeState, string.Format("Server blade# {0} has blade state OFF", getBladeState.bladeNumber));

                // [TFS WorkItem: 4002] SetBladeOff: Verify command does not affect power to the blade
                var powerState = this.Channel.GetPowerState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, powerState.completionCode, string.Format("Received power state for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, powerState.powerState, string.Format("Server blade# {0} has power state ON", powerState.bladeNumber));

                // [TFS WorkItem: 1769] GetBladeState: Verify command fails gracefully when blade is not receiving AC power
                CmTestLog.Info("Trying to Power off the blade slot and verifying GetBladeState");
                var powerOff = this.Channel.SetPowerOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, powerOff.completionCode, string.Format("Set power off for blade# {0}", bladeId));

                getBladeState = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.DevicePoweredOff, getBladeState.completionCode, string.Format("Received DevicePoweredOff for blade# {0}", bladeId));
            }

            if (jbodLocations.Length > 0)
            {
                CmTestLog.Info("Location is \n ******************** JBOD *********************");
                // [TFS WorkItem: 2724] SetBladeOn: Command is not valid to run on JBOD blade
                CmTestLog.Info("Trying to set blade ON for JBOD and verifying GetBladeState for JBOD");
                this.VerifySetBladeOn(jbodLocations, out bladeId, out bladeOn);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeOn.completionCode, string.Format("Received CommandNotValidForBlade for Jbod# {0}", bladeId));

                // [TFS WorkItem: 2722] GetBladeState: Command is not valid to run on JBOD blade
                getBladeState = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, getBladeState.completionCode, string.Format("Received CommandNotValidForBlade for JBOD# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, getBladeState.bladeState, string.Format("JBOD blade# {0} has blade state NA", getBladeState.bladeNumber));

                // [TFS WorkItem: 2727] SetBladeOff: Command is not valid to run on JBOD blade
                CmTestLog.Info("Trying to set blade off for JBOD");
                bladeOff = this.Channel.SetBladeOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeOff.completionCode, string.Format("Received CommandNotValidForBlade for Jbod# {0}", bladeId));
            }

            if (this.EmptyLocations.Length > 0)
            {
                CmTestLog.Info("Location is \n ******************** Empty Slot ******************");
                // [TFS WorkItem: 4626] SetBladeOn: Verify when blade is not present
                CmTestLog.Info("\nTrying to set blade ON for empty slot and verifying GetBladeState for empty blade");
                this.VerifySetBladeOn(EmptyLocations, out bladeId, out bladeOn);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeOn.completionCode, string.Format("Received Failure for empty blade# {0}", bladeId));

                // [TFS WorkItem: 1776] GetBladeState: GetBladeState doesn't fail when there are no blades present
                getBladeState = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, getBladeState.completionCode, string.Format("Received TimeOut for empty blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.NA, getBladeState.bladeState, string.Format("empty blade# {0} has blade state NA", getBladeState.bladeNumber));

                // [TFS WorkItem: 4003] SetBladeOff: Verify when blade is not present
                CmTestLog.Info("Trying to set blade OFF for Empty");
                bladeOff = this.Channel.SetBladeOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeOff.completionCode, string.Format("Received Failure for empty blade# {0}", bladeId));
            }

            // [TFS WorkItem: 2167] SetBladeOn: SetBladeOn when blade number is a (Positive) blade index
            bladeOn = this.Channel.SetBladeOn(CmConstants.InvalidBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, bladeOn.completionCode, string.Format("Received ParameterOutOfRange for invalid +ve blade# {0}", CmConstants.InvalidBladeId));

            // [TFS WorkItem: 3969] SetBladeOn: SetBladeOn when blade number is a (Negative) blade index
            bladeOn = this.Channel.SetBladeOn(CmConstants.InvalidNegtiveBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, bladeOn.completionCode, string.Format("Received ParameterOutOfRange for invalid -ve blade# {0}", CmConstants.InvalidNegtiveBladeId));

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: SetAllBladesDefaultPowerStateOff, SetAllBladesDefaultPowerStateOn,
        /// and GetAllBladesDefaultPowerState. The test case verifies:
        /// The commands return completion code success;
        /// When default power state is set to off, all blade states stay off when power is back on;
        /// When default power state is set to on, all blade states are on when power is back on;
        /// The default power state value stays unchanged even after the blade is restarted. 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetGetAllBladesDefaultPowerStateTest()
        {
            CmTestLog.Start();

            // make sure all blades are powered on
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.End(false);
                return false;
            }

            bool testPassed = true;
            AllBladesResponse allBladesResponse = null;
            GetAllBladesStateResponse allBladesStateResponse = null;

            // test SetAllBladesDefaultPowerStateOff [WorkItem: 2719]
            CmTestLog.Info("Trying to set all blades default power state to off");
            allBladesResponse = this.Channel.SetAllBladesDefaultPowerStateOff();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesResponse.completionCode, "Set all blades default power state to off");

            // [WorkItem: 2721]
            CmTestLog.Info("Trying to get all blades default power states");
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesStateResponse.completionCode, "Get all blades default power states");
            CmTestLog.Info("Verifying all blades default power state are off");
            testPassed &= this.VerifyBladeState(PowerState.OFF, allBladesStateResponse.bladeStateResponseCollection);

            // power blades off then on. they must not get powered to full OS
            CmTestLog.Info("Power all blades off and then on");
            this.SetPowerState(PowerState.OFF);
            this.SetPowerState(PowerState.ON);

            CmTestLog.Info("Verifying all blade states are off");
            allBladesStateResponse = this.Channel.GetAllBladesState();
            testPassed &= this.VerifyBladeState(PowerState.OFF, allBladesStateResponse.bladeStateResponseCollection);

            CmTestLog.Info("Verifying all blades default power state are still off after restart");
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            testPassed &= this.VerifyBladeState(PowerState.OFF, allBladesStateResponse.bladeStateResponseCollection);

            // test SetAllBladesDefaultPowerStateOn [WorkItem: 2718]
            CmTestLog.Info("Trying to set all blades default power state to on");
            allBladesResponse = this.Channel.SetAllBladesDefaultPowerStateOn();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesResponse.completionCode, "Set all blades default power state to on");

            CmTestLog.Info("Trying to get all blades default power states");
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, allBladesStateResponse.completionCode, "Get all blades default power states");
            CmTestLog.Info("Verifying all blades default power state are on");
            testPassed &= this.VerifyBladeState(PowerState.ON, allBladesStateResponse.bladeStateResponseCollection);

            // power blades off then on. they must now get powered to full OS
            CmTestLog.Info("Power all blades off and then on");
            this.SetPowerState(PowerState.OFF);
            this.SetPowerState(PowerState.ON);

            CmTestLog.Info("Verifying all blade states are on");
            allBladesStateResponse = this.Channel.GetAllBladesState();
            testPassed &= this.VerifyBladeState(PowerState.ON, allBladesStateResponse.bladeStateResponseCollection);

            CmTestLog.Info("Verifying all blades default power state are still on after restart");
            allBladesStateResponse = this.Channel.GetAllBladesDefaultPowerState();
            testPassed &= this.VerifyBladeState(PowerState.ON, allBladesStateResponse.bladeStateResponseCollection);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: SetBladeDefaultPowerStateOff, SetBladeDefaultPowerStateOn,
        /// and GetBladeDefaultPowerState. The test case verifies:
        /// The commands return completion code success;
        /// When default power state is set to off, blade state stay off when power is back on;
        /// When default power state is set to on, blade state stay on when power is back on;
        /// The default power state value does not change even after the blade is restarted. 
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetGetBladeDefaultPowerStateTest()
        {
            CmTestLog.Start();
            bool testPassed = true;
            
            this.ServerLocations = this.GetServerLocations();
            this.EmptyLocations = this.GetEmptyLocations();
            this.JbodLocations = this.GetJbodLocations();

            if (ServerLocations.Length > 0)
            {
                int randomServerBlade = ServerLocations.RandomOrDefault();
                testPassed = this.VerifyServerSetGetBladeDefaultPowerState(testPassed, randomServerBlade); 
            }
            if (JbodLocations.Length > 0)
            {
                int randomJbod = JbodLocations.RandomOrDefault();
                testPassed = this.VerifyJbodSetGetBladeDefaultState(testPassed, randomJbod);
            }
            // [TFS WorkItem: 1811] Given a valid bladeId (1-24) when the Blade is not present.
            if (EmptyLocations.Length > 0)
            {
                int radEmptySlot = EmptyLocations.RandomOrDefault();
                testPassed = this.VerifyEmptyBladeSetGetDefaultPowerState(testPassed, radEmptySlot);
            }

            // [TFS WorkItem:1812] Get default power state for a single invalid blade index (Negative #i.e.: -value)
            BladeStateResponse bladeStateRes = this.Channel.GetBladeDefaultPowerState(CmConstants.InvalidNegtiveBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, bladeStateRes.completionCode, 
                string.Format("Received ParameterOuOfRange CC for negative bladeId# {0}", bladeStateRes.bladeNumber));
            testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.EmptyDefautState, bladeStateRes.bladeState.ToString(), 
                string.Format("Received NA blade type for blade# {0}", bladeStateRes.bladeNumber));

            // [TFS WorkItem:1813] Get default power state for a single invalid blade index (Positive # ex.:  25)
            bladeStateRes = this.Channel.GetBladeDefaultPowerState(CmConstants.InvalidBladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, bladeStateRes.completionCode, 
                string.Format("Received ParameterOuOfRange CC for Invalid bladeId# {0}", bladeStateRes.bladeNumber));
            testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.EmptyDefautState, bladeStateRes.bladeState.ToString(), 
                string.Format("Received NA blade type for blade# {0}", bladeStateRes.bladeNumber));

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        public bool SetPowerActionsByAllUsersTest()
        {
            return (
                    this.SetAllPowerOnByAllUsers() &&
                    this.SetAllPowerOffByAllUsers() &&
                    this.SetPowerOnByAllUsers() &&
                    this.SetPowerOffByAllUsers() &&
                    this.SetAllBladesOnByAllUsers() &&
                    this.SetAllBladesOffByAllUsers() &&
                    this.SetBladeOnByAllUsers() &&
                    this.SetBladeOffByAllUsers() &&
                    this.SetAllBladesActivePowerCycleByAllUsers() &&
                    this.SetBladeActivePowerCycleByAllUsers() &&
                    this.SetAllBladesDefaultPowerStateOnByAllUsers() &&
                    this.SetAllBladesDefaultPowerStateOffByAllUsers() &&
                    this.SetBladeDefaultPowerStateOn() &&
                    this.SetBladeDefaultPowerStateOff());       
        }

        // [TFS WorkItem:3985] SetAllPowerOn: Verify only users member of Admin is able to execute the command
        public bool SetAllPowerOnByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;
           
            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllPowerOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllPowerOn();

                    testPassed = this.VerifySetAllBladePower(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                   // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetAllPowerOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2534] SetAllPowerOff: Verify only users member of Admin is able to execute the command
        public bool SetAllPowerOffByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllPowerOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllPowerOff();

                    testPassed = this.VerifySetAllBladePower(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetAllPowerOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2538] SetAllBladesOn: Verify only users member of Admin is able to execute the command
        public bool SetAllBladesOnByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power on");
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all power on");
                CmTestLog.End(false);
                return false;
            }

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllBladesOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllBladesOn();

                    testPassed = this.VerifySetAllBladePower(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetAllBladesOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2542] SetAllBladesOff: Verify only users member of Admin is able to execute the command
        public bool SetAllBladesOffByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power on");
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all power on");
                CmTestLog.End(false);
                return false;
            }

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllBladesOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllBladesOn();

                    testPassed = this.VerifySetAllBladePower(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetAllBladesOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2546] SetAllBladesActivePowerCycle: Verify only users member of Admin is able to execute the command
        public bool SetAllBladesActivePowerCycleByAllUsers()
        {
            CmTestLog.Start();

            // make sure all blades are powered on
            if (!this.SetPowerState(PowerState.ON) || !this.SetBladeState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot power on all blades");
                CmTestLog.End(false);
                return false;
            }
           
            bool testPassed = true;
            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllBladesActivePowerCycle with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    var offTime = TimeSpan.FromSeconds(CmConstants.OffTime);
                    allPowerResponse = this.TestChannelContext.SetAllBladesActivePowerCycle(Convert.ToInt32(offTime.TotalSeconds));

                    testPassed = this.VerifySetAllBladePower(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetAllBladesActivePowerCycle returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2529] SetPowerOn: Verify only users member of Admin is able to execute the command
        public bool SetPowerOnByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetPowerOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    int getRandomBlade = ChassisManagerTestHelper.RandomInteger(1, CmConstants.Population + 1);
                    CmTestLog.Info(string.Format("Trying to SetPowerOn for bladeId# {0}", getRandomBlade));

                    bladeRes = this.TestChannelContext.SetPowerOn(getRandomBlade);

                    testPassed = this.VerifySetBladePower(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetPowerOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:3988] SetPowerOff: Verify only Admin is able to execute the command
        public bool SetPowerOffByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetPowerOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    int getRandomBlade = ChassisManagerTestHelper.RandomInteger(1, CmConstants.Population + 1);
                    CmTestLog.Info(string.Format("Trying to SetPowerOff for bladeId# {0}", getRandomBlade));

                    bladeRes = this.TestChannelContext.SetPowerOff(getRandomBlade);

                    testPassed = this.VerifySetBladePower(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetPowerOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2537] SetBladeOn: Verify only users member of Admin is able to execute the command
        public bool SetBladeOnByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;
          
            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetBladeOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // pick up a random server blade
                    int bladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                    CmTestLog.Info(string.Format("Trying to SetBladeOn for bladeId# {0}", bladeId));

                    bladeRes = this.TestChannelContext.SetBladeOn(bladeId);

                    testPassed = this.VerifySetBladePower(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetBladeOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2541] SetBladeOff: Verify only users member of Admin is able to execute the command
        public bool SetBladeOffByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetBladeOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // pick up a random server blade
                    int bladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                    CmTestLog.Info(string.Format("Trying to SetBladeOff for bladeId# {0}", bladeId));

                    bladeRes = this.TestChannelContext.SetBladeOff(bladeId);

                    testPassed = this.VerifySetBladePower(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetBladeOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2545] SetBladeActivePowerCycle: Verify only users member of Admin is able to execute the command
        public bool SetBladeActivePowerCycleByAllUsers()
        {
            CmTestLog.Start();

            this.ServerLocations = this.GetServerLocations();

            bool testPassed = true;
            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse setBladeActivePowerCycleResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetBladeActivePowerCycle with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // pick up a random server blade
                    int bladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                    CmTestLog.Info(string.Format("Trying to SetBladeActivePowerCycle for bladeId# {0}", bladeId));
                    var offTime = TimeSpan.FromSeconds(CmConstants.OffTime);

                    setBladeActivePowerCycleResponse = this.TestChannelContext.SetBladeActivePowerCycle(bladeId, Convert.ToInt32(offTime.TotalSeconds));

                    testPassed = this.VerifySetBladePower(ref testPassed, roleId, setBladeActivePowerCycleResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && roleId != WCSSecurityRole.WcsCmAdmin)
                    {
                        // Test failed, http response should contain http 401 error
                        ChassisManagerTestHelper.IsTrue(true, " SetBladeActivePowerCycle returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2521] SetAllBladesDefaultPowerStateOn: Verify only users member of Operator or Admin is able to execute the command
        public bool SetAllBladesDefaultPowerStateOnByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power on");
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all power on");
                CmTestLog.End(false);
                return false;
            }

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllBladesDefaultPowerStateOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllBladesDefaultPowerStateOn();

                    testPassed = this.VerifySetAllBladesDefaultPowerState(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                       ChassisManagerTestHelper.IsTrue(true, " SetAllBladesDefaultPowerStateOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2525] SetAllBladesDefaultPowerStateOff: Verify only users member of Operator or Admin is able to execute the command
        public bool SetAllBladesDefaultPowerStateOffByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power on");
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all power on");
                CmTestLog.End(false);
                return false;
            }

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    AllBladesResponse allPowerResponse;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetAllBladesDefaultPowerStateOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                    allPowerResponse = this.TestChannelContext.SetAllBladesDefaultPowerStateOff();

                    testPassed = this.VerifySetAllBladesDefaultPowerState(ref testPassed, roleId, allPowerResponse);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        ChassisManagerTestHelper.IsTrue(true, " SetAllBladesDefaultPowerStateOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2519] SetBladeDefaultPowerStateOn: Verify only users member of Operator or Admin is able to execute the command
        public bool SetBladeDefaultPowerStateOn()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetBladeDefaultPowerStateOn with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // pick up a random server blade
                    int bladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                    CmTestLog.Info(string.Format("Trying to SetBladeDefaultPowerStateOn for bladeId# {0}", bladeId));

                    bladeRes = this.TestChannelContext.SetBladeDefaultPowerStateOn(bladeId);

                    testPassed = this.VerifySetBladeDefaultPowerState(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        ChassisManagerTestHelper.IsTrue(true, " SetBladeDefaultPowerStateOn returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2522] SetBladeDefaultPowerStateOff: Verify only users member of Operator or Admin is able to execute the command
        public bool SetBladeDefaultPowerStateOff()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power on
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    BladeResponse bladeRes;
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to SetBladeDefaultPowerStateOff with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                    // pick up a random server blade
                    int bladeId = this.ServerLocations.RandomOrDefault();
                    CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                    CmTestLog.Info(string.Format("Trying to SetBladeDefaultPowerStateOff for bladeId# {0}", bladeId));

                    bladeRes = this.TestChannelContext.SetBladeDefaultPowerStateOff(bladeId);

                    testPassed = this.VerifySetBladeDefaultPowerState(ref testPassed, roleId, bladeRes);
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 401 unauthorize
                    if (!ex.Message.Contains("401") && (roleId != WCSSecurityRole.WcsCmAdmin || roleId != WCSSecurityRole.WcsCmOperator))
                    {
                        ChassisManagerTestHelper.IsTrue(true, " SetBladeDefaultPowerStateOff returned Bad Request for user " + roleId.ToString());
                    }
                    else
                    {
                        ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                        testPassed = false;
                    }
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        public bool GetPowerActionsByAllUsersTest()
        {
            return (
                    this.GetPowerStateByAllUsers() &&
                    this.GetBladeStateByAllUsers() &&
                    this.GetAllBladesStateByAllUsers() &&
                    this.GetAllBladesDefaultPowerStateByAllUsers() &&
                    this.GetBladeDefaultPowerStateByAllUsers());
        }

        // [TFS WorkItem:3991] GetPowerState: Verify all user groups are able to execute the command
        public bool GetPowerStateByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to GetPowerState with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                CmTestLog.Info("Trying to get power state for random blade");
                int bladeId = ChassisManagerTestHelper.RandomInteger(1, CmConstants.Population + 1);
                PowerStateResponse stateRes = this.TestChannelContext.GetPowerState(bladeId);
                if (stateRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get power state With User {0}", roleId));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(stateRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                }
            }
           
            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2550] GetBladeState: Verify all user groups are able to execute the command
        public bool GetBladeStateByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to GetPowerState with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                // pick up a random server blade
                int bladeId = this.ServerLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                BladeStateResponse stateRes = this.TestChannelContext.GetBladeState(bladeId);
                if (stateRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get power state With User {0}", roleId));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(stateRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2551] GetAllBladesState: Verify all user groups are able to execute the command
        public bool GetAllBladesStateByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power on");
            if (!this.SetPowerState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all power on");
                CmTestLog.End(false);
                return false;
            }

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to GetPowerState with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                GetAllBladesStateResponse stateRes = this.TestChannelContext.GetAllBladesState();
                if (stateRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get All Blade state With User {0}", roleId));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(stateRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2527] GetAllBladesDefaultPowerState: Verify all user groups are able to execute the command
        public bool GetAllBladesDefaultPowerStateByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            CmTestLog.Info("Trying to set all Power and blades on");
            if (!this.SetPowerState(PowerState.ON) || !this.SetBladeState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot set all blades power on");
                CmTestLog.End(false);
                return false;
            }
            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to GetAllBladesDefultPowerState with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                GetAllBladesStateResponse stateRes = this.TestChannelContext.GetAllBladesDefaultPowerState();

                if (stateRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get All Blades Defult PowerState With User {0}", roleId));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(stateRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        // [TFS WorkItem:2526] GetBladeDefaultPowerState: Verify all user groups are able to execute the command
        public bool GetBladeDefaultPowerStateByAllUsers()
        {
            CmTestLog.Start();
            bool testPassed = true;

            this.ServerLocations = this.GetServerLocations();

            // Loop through different user types and set power actions
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to GetAllBladesDefultPowerState with user type {0} ", Enum.GetName(typeof(WCSSecurityRole), roleId)));

                int bladeId = this.ServerLocations.RandomOrDefault();
                CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

                BladeStateResponse stateRes = this.TestChannelContext.GetBladeDefaultPowerState(bladeId);

                if (stateRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get All Blades Defult PowerState With User {0}", roleId));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(stateRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: SetAllBladesActivePowerCycle. The test case verifies:
        /// The command returns completion code success;
        /// The blades can be successfully power cycled and stay off during offTime;
        /// The blades will be back on after offTime.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetAllBladesActivePowerCycleTest()
        {
            CmTestLog.Start();

            // make sure all blades are powered on
            if (!this.SetPowerState(PowerState.ON) || !this.SetBladeState(PowerState.ON))
            {
                CmTestLog.Failure("Cannot power on all blades");
                CmTestLog.End(false);
                return false;
            }

            bool allPassed = true;
            GetAllBladesStateResponse bladeStates = null;

            // make sure all blades are on to start with
            CmTestLog.Info("Verifying the initial state for all blades");
            bladeStates = this.Channel.GetAllBladesState();
            allPassed &= this.VerifyBladeState(PowerState.ON, bladeStates.bladeStateResponseCollection);
            
            // set power cycle for all blades
            var offTime = TimeSpan.FromSeconds(CmConstants.OffTime);
            CmTestLog.Info("Trying to set blade active power cycle for all blades");
            var response = this.Channel.SetAllBladesActivePowerCycle(Convert.ToInt32(offTime.TotalSeconds));
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode, string.Format("Set all blades active power cycle with off time {0} seconds", offTime.TotalSeconds));

            // all blades should be off during off time
            Thread.Sleep(TimeSpan.FromTicks(offTime.Ticks / 2));
            CmTestLog.Info("Verifying all blades are off");
            bladeStates = this.Channel.GetAllBladesState();
            allPassed &= this.VerifyBladeState(PowerState.OFF, bladeStates.bladeStateResponseCollection);

            // [TFS WorkItem: 1806] SetAllBladesActivePowerCycle: Verify adding (Few Seconds) wait time before power on
            // all blades should be back on after timeout
            Thread.Sleep(offTime.Add(TimeSpan.FromSeconds(10)));
            CmTestLog.Info("Verifying all blades are back on");
            bladeStates = this.Channel.GetAllBladesState();
            allPassed &= this.VerifyBladeState(PowerState.ON, bladeStates.bladeStateResponseCollection);

            // [TFS WorkItem: 2267] SetAllBladesActivePowerCycle: Verify ParameterOutOfRange using offtime greater than 255
            offTime = TimeSpan.FromSeconds(CmConstants.InvalidOffTime);
            CmTestLog.Info("Trying to set blade active power cycle for all blades with invalid offtime# " + CmConstants.InvalidOffTime);
           
            response = this.Channel.SetAllBladesActivePowerCycle(Convert.ToInt32(offTime.TotalSeconds));
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, response.completionCode, "Received ParameterOutOfRange to SetAllBladesActivePowerCycle with inavlid off time\n");
            
            // [TFS WorkItem: 2266] SetAllBladesActivePowerCycle: Verify using a NEGATIVE offtime
            offTime = TimeSpan.FromSeconds(CmConstants.ngtveOffTime);
            CmTestLog.Info("Trying to set blade active power cycle for all blades with negative offtime of " + offTime);
            try
            {
               response = this.Channel.SetAllBladesActivePowerCycle(Convert.ToInt32(offTime.TotalSeconds));
            }
            catch (OverflowException ex)
            {
                //  Verify "server encountered an error processing the request"
                CmTestLog.Success("Received OverFlowExcpetion for for negative offtime");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            // Verify command fails and blade state is not affected
            Thread.Sleep(2000);
            CmTestLog.Info("Verifying all blades state not affected after set negative off time");
            bladeStates = this.Channel.GetAllBladesState();
            allPassed &= this.VerifyBladeState(PowerState.ON, bladeStates.bladeStateResponseCollection);

            // end of the test
            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test Command: SetBladeActivePowerCycle. This verifies
        /// Completion code success for Server and verfiies that powercycle.
        /// CommandNotValidForBlade for JBOD
        /// Failure for Empty
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool SetBladeActivePowerCycleTest()
        {
            CmTestLog.Start();

            // get server and jbod locations
            int[] serverLocations, jbodLocations;
            if (!this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations) ||
                !this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                CmTestLog.Failure("Cannot get server or jbod locations");
                CmTestLog.End(false);
                return false;
            }

            this.EmptyLocations = this.GetEmptyLocations();

            bool testPassed = true;
            GetAllBladesStateResponse bladeStates = null;
            BladeResponse powerCycleResponse = null;
            BladeStateResponse bladeStateRes = null;
            int randomBlade;

            // make sure all blades are on to start with
            CmTestLog.Info("Verifying the initial state for all blades");
            bladeStates = this.Channel.GetAllBladesState();
            testPassed &= this.VerifyBladeState(PowerState.ON, bladeStates.bladeStateResponseCollection);

            // powercycle Command: powercycle for Server Blade
            if (serverLocations.Length > 0)
            {
                CmTestLog.Info(" !!!!!!!!!! Trying to set blade active power cycle for Server !!!!!!!!!!");
                CmTestLog.Info("Trying to get random server blade");
                randomBlade = serverLocations.RandomOrDefault();
                //PowerCycle blade
                powerCycleResponse = this.Channel.SetBladeActivePowerCycle(randomBlade, CmConstants.OffTimeSec);

                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, powerCycleResponse.completionCode, string.Format("Received Success for bladeId# {0} \n", randomBlade));

                if (!testPassed)
                {
                    CmTestLog.End(false);
                    return false; // if failed here, no need to continue
                }

                System.Threading.Thread.Sleep(10000);

                bladeStateRes = this.Channel.GetBladeState(randomBlade);
                if (bladeStateRes.bladeState != PowerState.OFF)
                {
                    CmTestLog.Failure(string.Format("!!!Blade should still be off for blade# {0}", randomBlade));
                    CmTestLog.End(false);
                    return false;
                }

                System.Threading.Thread.Sleep(30000);
                bladeStateRes = this.Channel.GetBladeState(randomBlade);
                if (bladeStateRes.bladeState != PowerState.ON)
                {
                    CmTestLog.Failure(string.Format("!!!Blade should be already turned ON for bladeId# {0}", randomBlade));
                    CmTestLog.End(false);
                    return false;
                }
            }
            // [TFS WorkItem: 2728] SetBladeActivePowerCycle: Verify command is not valid to run on JBOD blade
            if (jbodLocations.Length > 0)
            {
                CmTestLog.Info(" !!!!!!!!!! Trying to set blade active power cycle for JBOD !!!!!!!!!!");
                randomBlade = jbodLocations.RandomOrDefault();
                CmTestLog.Info("Trying to get random JBOD blade");

                //PowerCycle blade
                powerCycleResponse = this.Channel.SetBladeActivePowerCycle(randomBlade, CmConstants.OffTimeSec);

                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, powerCycleResponse.completionCode,
                    string.Format("Received CommandNotValidForBlade for Jbod bladeId# {0}\n", randomBlade));
            }
            // [TFS WorkItem: 1805] SetBladeActivePowerCycle: Verify an empty slot index fails
            if (this.EmptyLocations.Length > 0)
            {
                CmTestLog.Info("!!!!!!!!!! Trying to set blade active power cycle for Empty blade !!!!!!!!!!");
                randomBlade = this.EmptyLocations.RandomOrDefault();
                CmTestLog.Info("Trying to get random empty blade");

                //PowerCycle blade
                powerCycleResponse = this.Channel.SetBladeActivePowerCycle(randomBlade, CmConstants.OffTimeSec);

                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, powerCycleResponse.completionCode, string.Format("Received Failure for empty bladeId# {0}\n", randomBlade));
            }

            // [TFS WorkItem: 2265] SetBladeActivePowerCycle: Verify invalid bladeId fails
            CmTestLog.Info(" !!!!!!!!!! Trying to set blade active power cycle for invalid blade !!!!!!!!!!");
            //PowerCycle blade
            powerCycleResponse = this.Channel.SetBladeActivePowerCycle(CmConstants.InvalidBladeId, CmConstants.OffTimeSec);

            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.ParameterOutOfRange, powerCycleResponse.completionCode, 
                string.Format("Received ParameterOutOfRange for invalid bladeId# {0}", CmConstants.InvalidBladeId));

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        private void BladeOffRandomBlade(int[] nonEmptyBlades, out int bladeOffBladeId)
        {
            bladeOffBladeId = nonEmptyBlades.RandomOrDefault();
            CmTestLog.Info(string.Format("Trying to set blade state of Blade# {0} to off", bladeOffBladeId));
            
            if (!this.SetPowerState(PowerState.ON, bladeOffBladeId))
            {
                throw new ApplicationException("Not able to power ON the blade");
            }
            
            if (!this.SetBladeState(PowerState.OFF, bladeOffBladeId))
            {
                throw new ApplicationException("Not able to Blade Off the blade");
            }
        }

        private void PowerOffRandomBlade(GetAllBladesInfoResponse allBladesInfoResponse, out int[] nonEmptyBlades, out int powerOffBladeId)
        {
            // power off a random blade
            nonEmptyBlades = allBladesInfoResponse
                                                  .bladeInfoResponseCollection
                                                  .Where(blade => !string.IsNullOrWhiteSpace(blade.bladeType))
                                                  .Select(blade => blade.bladeNumber)
                                                  .ToArray();

            powerOffBladeId = nonEmptyBlades.RandomOrDefault();
            CmTestLog.Info(string.Format("Trying to power off Blade# {0}", powerOffBladeId));

            if (!this.SetPowerState(PowerState.OFF, powerOffBladeId))
            {
                throw new ApplicationException("Not able to power off the blade");
            }
        }

        private bool VerifyServerBladeInfo(bool testPassed, BladeInfoResponse bladeInfo)
        {
            PowerStateResponse pState = this.Channel.GetPowerState(bladeInfo.bladeNumber);
            if (pState.powerState != PowerState.ON)
            {
                CmTestLog.Warning("Blade is Powered off. Powering ON blade# " + bladeInfo.bladeNumber);
                this.Channel.SetPowerOn(bladeInfo.bladeNumber);
                System.Threading.Thread.Sleep(35000);
                bladeInfo = this.Channel.GetBladeInfo(bladeInfo.bladeNumber);
            }
            if(bladeInfo.bladeType != "Server")
            {
                BladeStateResponse bs = this.Channel.GetBladeState(bladeInfo.bladeNumber);
                PowerStateResponse ps = this.Channel.GetPowerState(bladeInfo.bladeNumber);
                CmTestLog.Failure("the blade type is not a server. Please verify the blade type and call right blade info verification against slot# " + bladeInfo.bladeNumber + " Blade type is " + bladeInfo.bladeType );
                return false;
            }

            CmTestLog.Info(string.Format("Location# {0} is a Server", bladeInfo.bladeNumber));
            
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeInfo.completionCode, string.Format("{0} Blade# {1} returns success completion code", bladeInfo.bladeType, bladeInfo.bladeNumber));
            testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.ServerBladeType, bladeInfo.bladeType, "Received Server blade type");
            testPassed &= ChassisManagerTestHelper.IsTrue(!string.IsNullOrEmpty(bladeInfo.serialNumber), "Nic information section is populated for blade# " + bladeInfo.bladeNumber);
            testPassed &= ChassisManagerTestHelper.AreEqual(ConfigurationManager.AppSettings["FirmwareVersion"], bladeInfo.firmwareVersion, "Successfully verified firmwareVersion for blade#" + bladeInfo.bladeNumber);
            testPassed &= ChassisManagerTestHelper.AreEqual(ConfigurationManager.AppSettings["HardwareVersion"], bladeInfo.hardwareVersion, "Successfully verified hardwareVersion for blade#" + bladeInfo.bladeNumber);

            int numberOfNics = 0;

            if (bladeInfo.macAddress.Count != 0)
            {
                foreach (NicInfo nicInfo in bladeInfo.macAddress)
                {
                    if (nicInfo.statusDescription != "Not Present")
                    {
                        numberOfNics++;
                        testPassed &= ChassisManagerTestHelper.AreEqual(nicInfo.completionCode, CompletionCode.Success, "Received Success completion code for Nic# " + nicInfo.deviceId);
                        testPassed &= ChassisManagerTestHelper.IsTrue(!string.IsNullOrEmpty(nicInfo.macAddress), "macAddress is populated for blade# " + bladeInfo.bladeNumber);
                    }
                }
                testPassed &= ChassisManagerTestHelper.AreEqual(ConfigurationManager.AppSettings["NicCount"], numberOfNics.ToString(), "Nic information failed with wrong nic number");
            }
            else
            {
                CmTestLog.Info("Received macaddress count is null/zero");
                testPassed = false;
            }
            return testPassed;
        }

        private bool VerifyJbodBladeInfo(bool testPassed, BladeInfoResponse bladeInfo)
        {
            if(bladeInfo.bladeType.ToLower() != "jbod")
            {
                CmTestLog.Warning(String.Format("The provided blade# {0} is not for a JBOD. We cannot check JBOD information against submitted blade", bladeInfo.bladeNumber));
                testPassed = false;
                return testPassed;
            }
            CmTestLog.Info(string.Format("Location# {0} is Jbod", bladeInfo.bladeNumber));
            
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeInfo.completionCode, "Received success completion code for JBOD");
            testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.JbodBladeType, bladeInfo.bladeType, "Received JBOD blade type");
            string serialNumber = ConfigurationManager.AppSettings[bladeInfo.bladeNumber.ToString()];
            testPassed &= ChassisManagerTestHelper.AreEqual(bladeInfo.serialNumber, serialNumber, "Received Serial number");
            testPassed &= ChassisManagerTestHelper.AreEqual(ConfigurationManager.AppSettings["JbodFirmwareVersion"], bladeInfo.firmwareVersion, "Successfully verified firmwareVersion");
            testPassed &= ChassisManagerTestHelper.AreEqual(ConfigurationManager.AppSettings["JbodHardwareVersion"], bladeInfo.hardwareVersion, "Successfully verified hardwareVersion");
            
            if (bladeInfo.macAddress.Count != 0)
            {
                foreach (var nicInfo in bladeInfo.macAddress)
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(nicInfo.completionCode, CompletionCode.Failure, "Received Failure completion code for Nic#" + nicInfo.deviceId);
                    testPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(nicInfo.macAddress), "Received empty macAddress ");
                }
            }
            else
            {
                CmTestLog.Info("Received macaddress count is null/zero");
                testPassed = false;
            }

            return testPassed;
        }

        private bool VerifyEmptybladeInfo(bool testPassed, BladeInfoResponse bladeInfo)
        {
            CmTestLog.Info(string.Format("Location# {0} is empty", bladeInfo.bladeNumber));
            
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, bladeInfo.completionCode, "Received TimeOut completion code for empty blade");
            testPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(bladeInfo.bladeType), "Received empty blade Type");
            testPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(bladeInfo.serialNumber), "Received empty serialNumber");
            testPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(bladeInfo.firmwareVersion), "Received empty firmwareVersion");
            testPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrEmpty(bladeInfo.hardwareVersion), "Received empty hardwareVersion");
            testPassed &= ChassisManagerTestHelper.IsTrue(bladeInfo.macAddress.Count == 0, "Received empty macAddress");
            
            return testPassed;
        }
        
        private bool GetServerHealth(bool allPassed, BladeHealthResponse bladeHealth, int bladeId)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth != null, string.Format("Received bladehealth for blade# {0}", bladeHealth.bladeNumber));

            this.VerifyBladeShellInformation(ref allPassed, bladeHealth);

            XmlReader bladehealthInfo = XmlReader.Create(this.skuDefinitionFile);
            
            // verify server blade information
            if (bladeHealth.bladeShell.bladeType.Equals(CmConstants.ServerBladeType))
            {
                this.VerifyServerFruInformation(ref allPassed, bladeHealth, bladeId, bladehealthInfo);

                this.VerifyProcessorInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifyMemoryInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifyPCIeInformation(ref allPassed, bladeHealth, bladehealthInfo);

                this.VerifySensorInformation(ref allPassed, bladeHealth);

                this.VerifyServerDiskInfo(ref allPassed, bladeHealth);
            }

            return allPassed;
        }

        private void VerifyServerFruInformation(ref bool allPassed, BladeHealthResponse bladeHealth, int bladeId, XmlReader bladehealthInfo)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(!string.IsNullOrWhiteSpace(bladeHealth.serialNumber), string.Format("Serial number is not empty for blade# {0}", bladeHealth.bladeNumber));

            //string serialNumber = ConfigurationManager.AppSettings[bladeId.ToString()];
            //allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.serialNumber == serialNumber, string.Format("Received SerialNumber for blade# {0} is {1} was expecting {2}", bladeHealth.bladeNumber, bladeHealth.serialNumber, serialNumber));

            bladehealthInfo.ReadToFollowing("hardwareVersion");
            string propertyValue = bladehealthInfo.ReadElementContentAsString();
            allPassed &= ChassisManagerTestHelper.IsTrue(propertyValue == bladeHealth.hardwareVersion, string.Format("Received hardware version for blade# {0}", bladeHealth.bladeNumber));
        }

        private void VerifySensorInformation(ref bool allPassed, BladeHealthResponse bladeHealth)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors != null, string.Format("Received sensor Info for blade# {0}", bladeHealth.bladeNumber));
           
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Aggregate(true, (b, info) => b & CompletionCode.Success == info.completionCode),
                string.Format("Received all sensors information for blade# {0}", bladeHealth.bladeNumber));
        }

        private void VerifyFruInformationEmpty(ref bool allPassed, BladeHealthResponse bladeHealth)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrWhiteSpace(bladeHealth.serialNumber), string.Format("Serial number is empty for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.IsTrue(string.IsNullOrWhiteSpace(bladeHealth.hardwareVersion), string.Format("HardWare Version is empty for blade# {0}", bladeHealth.bladeNumber));
        }

        private void VerifyServerDiskInfo(ref bool allPassed, BladeHealthResponse bladeHealth)
        {
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodInfo == null, string.Format("Received null jbodInfo for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodDiskInfo == null, string.Format("Received null jbodDiskInfo for blade# {0}", bladeHealth.bladeNumber));
        }

        private void VerifyBladeShellInformation(ref bool allPassed, BladeHealthResponse bladeHealth)
        {
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeHealth.completionCode, string.Format("Received response with completion code success for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.bladeShell != null, string.Format("Received bladeshell for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeHealth.bladeShell.completionCode, string.Format("Received blade shell information for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.HealthyBladeState, bladeHealth.bladeShell.bladeState, string.Format("The blade is in healthy state bladeId# {0}", bladeHealth.bladeNumber));
        }

        private bool VerifyJbodBladeHealth(bool allPassed, int bladeId)
        {
            try
            {
                CmTestLog.Info(string.Format("Trying to get health information for JBOD Blade#: {0}", bladeId));
                BladeHealthResponse bladeHealth = this.TestChannelContext.GetBladeHealth(bladeId, true, true, true, true, true, true, true);

                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeHealth.jbodInfo.completionCode, string.Format("Received JbodInfo with completion code success for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeHealth.jbodDiskInfo.completionCode, string.Format("Received JbodDiskInfo with completion code success for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodDiskInfo.diskInfo.Aggregate(true, (b, info) => b & CompletionCode.Success == info.completionCode),
                    string.Format("Received all disk information for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.jbodDiskInfo.diskInfo.Aggregate(true, (b, info) => b & "Normal" == info.diskStatus),
                    string.Format("Received all JBOD disk state as Normal for blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo.Count == 0, string.Format("Processor information is empty for JBOD blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo.Count == 0, string.Format("Memory information is empty for JBOD blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo.Count == 0, string.Format("PCIE information is empty for JBOD blade# {0}", bladeHealth.bladeNumber));
                allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.sensors.Count == 0, string.Format("Sensors information is empty for JBOD blade# {0}", bladeHealth.bladeNumber));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                allPassed = false;
            }

            return allPassed;
        }

        private void VerifyPCIeInformation(ref bool allPassed, BladeHealthResponse bladeHealth, XmlReader bladehealthInfo)
        {
            string propertyValue;

            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.pcieInfo != null, string.Format("Received PCIe Info for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.AreEqual(Convert.ToInt32(ConfigurationManager.AppSettings["PCIeCount"]), bladeHealth.pcieInfo.Count, "PCIe's count check");

            foreach (PCIeInfo serverPCIeInfo in bladeHealth.pcieInfo)
            {
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, serverPCIeInfo.completionCode,
                    string.Format("Received CompletionCode success for Server PCIe Information for blade:PCIeNumber {0}:{1}", bladeHealth.bladeNumber, serverPCIeInfo.pcieNumber));

                bladehealthInfo.ReadToFollowing("vendorId");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverPCIeInfo.vendorId,
                    string.Format("Received PCIe VendorId for blade:PCIeNumber {0}:{1}", bladeHealth.bladeNumber, serverPCIeInfo.pcieNumber));

                bladehealthInfo.ReadToFollowing("deviceId");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverPCIeInfo.deviceId,
                    string.Format("Received PCIe DeviceId for blade:PCIeNumber {0}:{1}", bladeHealth.bladeNumber, serverPCIeInfo.pcieNumber));

                bladehealthInfo.ReadToFollowing("subSystemId");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverPCIeInfo.subSystemId,
                    string.Format("Received SubsystemId for blade:PCIeNumber {0}:{1}", bladeHealth.bladeNumber, serverPCIeInfo.pcieNumber));

                bladehealthInfo.ReadToFollowing("status");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverPCIeInfo.status,
                    string.Format("Received PCIe status for blade:PCIeNumber {0}:{1}", bladeHealth.bladeNumber, serverPCIeInfo.pcieNumber));
            }
        }

        private void VerifyMemoryInformation(ref bool allPassed, BladeHealthResponse bladeHealth, XmlReader bladehealthInfo)
        {
            string propertyValue;

            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.memoryInfo != null, string.Format("Received memory Info for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.AreEqual(Convert.ToInt32(ConfigurationManager.AppSettings["DIMMsCount"]), bladeHealth.memoryInfo.Count, "DIMMs count check");

            foreach (MemoryInfo serverDimmInfo in bladeHealth.memoryInfo)
            {
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, serverDimmInfo.completionCode, string.Format("Received CompletionCode success for Server DIMM Information for blade# {0}", bladeHealth.bladeNumber));

                bladehealthInfo.ReadToFollowing("dimmType");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverDimmInfo.dimmType, string.Format("Received DIMM Type for blade:DIMM {0}:{1}", bladeHealth.bladeNumber, serverDimmInfo.dimm));

                bladehealthInfo.ReadToFollowing("status");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverDimmInfo.status, string.Format("Received DIMM Status for blade:DIMM {0}:{1}", bladeHealth.bladeNumber, serverDimmInfo.dimm));

                bladehealthInfo.ReadToFollowing("speed");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverDimmInfo.speed, string.Format("Received DIMM speed for blade:DIMM {0}:{1}", bladeHealth.bladeNumber, serverDimmInfo.dimm));

                bladehealthInfo.ReadToFollowing("size");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverDimmInfo.size, string.Format("Received DIMM size for blade:DIMM {0}:{1}", bladeHealth.bladeNumber, serverDimmInfo.dimm));

                bladehealthInfo.ReadToFollowing("memVoltage");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverDimmInfo.memVoltage, string.Format("Received DIMM memVoltage for blade:DIMM {0}:{1}", bladeHealth.bladeNumber, serverDimmInfo.dimm));
            }
        }

        private void VerifyProcessorInformation(ref bool allPassed, BladeHealthResponse bladeHealth, XmlReader bladehealthInfo)
        {
            string propertyValue;

            allPassed &= ChassisManagerTestHelper.IsTrue(bladeHealth.processorInfo != null, string.Format("Received processor Info for blade# {0}", bladeHealth.bladeNumber));
            allPassed &= ChassisManagerTestHelper.AreEqual(Convert.ToInt32(ConfigurationManager.AppSettings["ProcessorCount"]), bladeHealth.processorInfo.Count, "Processor's count check");

            foreach (ProcessorInfo serverProcInfo in bladeHealth.processorInfo)
            {
                allPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, serverProcInfo.completionCode,
                    string.Format("Received CompletionCode success for Server Processor Information for blade# {0}", bladeHealth.bladeNumber));

                bladehealthInfo.ReadToFollowing("procType");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverProcInfo.procType, string.Format("Received Server Processor Type for blade:procId {0}:{1}", bladeHealth.bladeNumber, serverProcInfo.procId));

                bladehealthInfo.ReadToFollowing("state");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverProcInfo.state, string.Format("Received Server Processor state for blade:procId {0}:{1}", bladeHealth.bladeNumber, serverProcInfo.procId));

                bladehealthInfo.ReadToFollowing("frequency");
                propertyValue = bladehealthInfo.ReadElementContentAsString();
                allPassed &= ChassisManagerTestHelper.AreEqual(propertyValue, serverProcInfo.frequency,
                    string.Format("Received Server Processor Frequency for blade:procId {0}:{1}", bladeHealth.bladeNumber, serverProcInfo.procId));
            }
        }

        private void VerifySetBladeOn(int[] bladeLocations, out int bladeId, out BladeResponse bladeOn)
        {
            int randSlot = ChassisManagerTestHelper.RandomInteger(0, bladeLocations.Length);
            bladeId = bladeLocations[randSlot];
           
            bladeOn = this.Channel.SetBladeOn(bladeId);
        }

        private bool VerifyServerSetGetBladeDefaultPowerState(bool testPassed, int bladeId)
        {
            CmTestLog.Info(string.Format("Location# {0} is Server", bladeId));

            BladeStateResponse bladeStateRes = null;
            BladeResponse bladeRes = null;
            try
            {
                // [TFS WorkItem 1816] power Off Blade and check GetBladeDefaultPowerState: Fails gracefully when Power is off. 
                CmTestLog.Info("Verifying blade default power state when power is OFF for blade slot");
                CmTestLog.Info(string.Format("Power off the blade slot {0}", bladeId));
                this.SetPowerState(PowerState.OFF, bladeId);
                
                bladeStateRes = this.Channel.GetBladeDefaultPowerState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.DevicePoweredOff, bladeStateRes.completionCode, string.Format("Received Device Powered OFF for blade# {0}", bladeId));

                // [TFS WorkItem:1810]: Given a valid bladeid returns the default power state of the blade when the blade is OFF (BMC is on)
                CmTestLog.Info("Request to default power state to be OFF  when the blade is OFF (BMC is on)");
                CmTestLog.Info(string.Format("Power ON the blade slot {0}", bladeId));
                this.SetPowerState(PowerState.ON, bladeId);
                bladeRes = this.Channel.SetBladeDefaultPowerStateOff(bladeId);

                CmTestLog.Info(string.Format("Power OFF the blade {0}", bladeId));
                this.SetBladeState(PowerState.OFF, bladeId);

                //  [TFS WorkItem:1820]:SetBladeDefaultPowerStateOff: Given a Valid Blade Index request to default power state to be OFF when the current state is OFF.
                CmTestLog.Info("Setting the default power state to OFF");
                bladeRes = this.Channel.SetBladeDefaultPowerStateOff(bladeId);

                CmTestLog.Info(string.Format("Verifying default power state of the blade# {0}", bladeId));
                bladeStateRes = this.Channel.GetBladeDefaultPowerState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeStateRes.completionCode, "Received blade default power state");
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, bladeStateRes.bladeState, "Received blade default power state");

                // [TFS WorkItem:1818 & 1819] SetBladeDefaultPowerStateOn: Given a Valid Blade Index request to default power state to be ON when the current state is OFF
                CmTestLog.Info("Request to default power state to be ON when the current blade state is OFF");
                bladeRes = this.Channel.SetBladeDefaultPowerStateOn(bladeId);

                bladeStateRes = this.Channel.GetBladeDefaultPowerState(bladeId);
                CmTestLog.Info("Verifying blade default power state ON");
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeStateRes.completionCode, string.Format("Received Success GetBladeDefaultPowerState for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, bladeStateRes.bladeState, "Received blade default power state");

                // Make sure the blade stays OFF
                CmTestLog.Info("Verifying blade default power state ON");
                bladeStateRes = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeStateRes.completionCode, string.Format("Received Success GetBladestate for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.OFF, bladeStateRes.bladeState, string.Format("Received blade state for blade# {0}", bladeId));

                // [TFS WorkItem: 1817]SetBladeDefaultPowerStateOn: Given a Valid Blade Index request to default power state to be ON when the current state is ON
                CmTestLog.Info("Request to default power state to be ON when the blade state is ON");
                CmTestLog.Info(string.Format("Power ON the blade {0}", bladeId));
                this.SetBladeState(PowerState.ON, bladeId);
                Thread.Sleep(TimeSpan.FromSeconds(2));
                bladeRes = this.Channel.SetBladeDefaultPowerStateOn(bladeId);                
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeRes.completionCode, "Success Set Deafult Power State ON");
                // Make sure the default power state ON
                bladeStateRes = this.Channel.GetBladeDefaultPowerState(bladeId);
                CmTestLog.Info("Verifying blade default power state ON");
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeStateRes.completionCode, "Received success blade default power state");
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, bladeStateRes.bladeState, string.Format("Received default power state for blade# {0}", bladeId));

                // Make sure the blade stays ON
                CmTestLog.Info("Verifying that blade stays ON");
                bladeStateRes = this.Channel.GetBladeState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, bladeStateRes.completionCode, string.Format("Received Success GetBladestate for blade# {0}", bladeId));
                testPassed &= ChassisManagerTestHelper.AreEqual(PowerState.ON, bladeStateRes.bladeState, string.Format("Received blade state for blade# {0}", bladeId));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }
            return testPassed;
        }

        private bool VerifyJbodSetGetBladeDefaultState(bool testPassed, int bladeId)
        {
            try
            {
                BladeStateResponse bladeState = null;
                BladeResponse bladeRes = null;

                CmTestLog.Info(string.Format("Location# {0} is JBOD", bladeId));
                // TFS WorkItem(2715)
                bladeRes = this.Channel.SetBladeDefaultPowerStateOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeRes.completionCode, 
                    string.Format("Received SetBladeDefaultPowerStateOff CC for blade# {0}", bladeRes.bladeNumber));

                // TFS WorkItem(2714)
                bladeRes = this.Channel.SetBladeDefaultPowerStateOn(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeRes.completionCode, 
                    string.Format("Received SetBladeDefaultPowerStateOn CC for blade# {0}", bladeRes.bladeNumber));
                bladeState = this.Channel.GetBladeDefaultPowerState(bladeId);

                // TFS WorkItem(2720)
                bladeState = this.Channel.GetBladeDefaultPowerState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, bladeState.completionCode, 
                    string.Format("Received GetBladeDefaultPowerState CC for blade# {0}", bladeState.bladeNumber));
                testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.EmptyDefautState, bladeState.bladeState.ToString(), string.Format("Received NA blade type for blade# {0}", bladeState.bladeNumber));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }
            return testPassed;
        }

        private bool VerifyEmptyBladeSetGetDefaultPowerState(bool testPassed, int bladeId)
        {
            try
            {
                BladeStateResponse bladeState = null;
                BladeResponse bladeRes = null;

                CmTestLog.Info(string.Format("Location# {0} is empty", bladeId));
                bladeRes = this.Channel.SetBladeDefaultPowerStateOff(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeRes.completionCode, 
                    string.Format("Received SetBladeDefaultPowerStateOff CC Failure for blade# {0}", bladeRes.bladeNumber));

                bladeRes = this.Channel.SetBladeDefaultPowerStateOn(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeRes.completionCode, 
                    string.Format("Received SetBladeDefaultPowerStateOn CC Failure for blade# {0}", bladeRes.bladeNumber));

                bladeState = this.Channel.GetBladeDefaultPowerState(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Failure, bladeState.completionCode, 
                    string.Format("Received GetBladeDefaultPowerState CC Failure for blade# {0}", bladeState.bladeNumber));
                testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.EmptyDefautState, bladeState.bladeState.ToString(), string.Format("Received NA blade type for blade# {0}", bladeState.bladeNumber));
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }
            return testPassed;
        }

        private bool VerifySetAllBladesDefaultPowerState(ref bool testPassed, WCSSecurityRole roleId, AllBladesResponse allPowerResponse)
        {
            if (allPowerResponse.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                CmTestLog.Failure(string.Format("Cannot set all blades default power state With User {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else if (allPowerResponse.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser))
            {
                CmTestLog.Failure(string.Format("User is able to set all blades default power state {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else
            {
                testPassed &= ChassisManagerTestHelper.AreEqual(allPowerResponse.completionCode, CompletionCode.Success, 
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return testPassed;
        }

        private bool VerifySetBladeDefaultPowerState(ref bool testPassed, WCSSecurityRole roleId, BladeResponse bladeRes)
        {
            if (bladeRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator))
            {
                CmTestLog.Failure(string.Format("Cannot Set blade default Power state using user type {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else if (bladeRes.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser))
            {
                CmTestLog.Failure(string.Format("User is able to set blade default power state {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else
            {
                testPassed &= ChassisManagerTestHelper.AreEqual(bladeRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return testPassed;
        }

        private bool VerifySetBladePower(ref bool testPassed, WCSSecurityRole roleId, BladeResponse bladeRes)
        {
            if (bladeRes.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin))
            {
                CmTestLog.Failure(string.Format("Cannot Set blade Power using WcsAdmin User {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else if (bladeRes.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                CmTestLog.Failure(string.Format("User/Operator is able to set blade power {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else
            {
                testPassed &= ChassisManagerTestHelper.AreEqual(bladeRes.completionCode, CompletionCode.Success, string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return testPassed;
        }

        private bool VerifySetAllBladePower(ref bool testPassed, WCSSecurityRole roleId, AllBladesResponse allPowerResponse)
        {
            if (allPowerResponse.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin))
            {
                CmTestLog.Failure(string.Format("Cannot set all blades power With WcsAdmin User {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else if (allPowerResponse.completionCode == CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmUser || roleId == WCSSecurityRole.WcsCmOperator))
            {
                CmTestLog.Failure(string.Format("User/Operator is able to set all blades power {0}", roleId));
                CmTestLog.End(false);
                return false;
            }
            else
            {
                testPassed &= ChassisManagerTestHelper.AreEqual(allPowerResponse.completionCode, CompletionCode.Success, 
                    string.Format("Received success with user {0}", Enum.GetName(typeof(WCSSecurityRole), roleId)));
            }
            return testPassed;
        }
    }
}
