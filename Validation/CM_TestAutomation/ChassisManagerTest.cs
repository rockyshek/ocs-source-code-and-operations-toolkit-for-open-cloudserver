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

using Microsoft.GFS.WCS.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Microsoft.GFS.WCS.Test;

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

using System.Xml.Schema;
//-----------------------------------------------------------------------
// <copyright file="ChassisManagerTest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: InternalsVisibleTo("CMTestUtility.exe")]

namespace CM_TestAutomation
{
    /// <summary>
    /// This is a test class for ChassisManagerTest and is intended
    /// to contain all ChassisManagerTest Unit Tests
    /// </summary>
    [TestClass]
    public class ChassisManagerTest
    {
        internal ChannelFactory<IChassisManager> serviceChannel;
        internal IChassisManager channel;
        internal List<int> JBODLocations = new List<int> {};
        internal List<int> emptySlots = new List<int> {24};
        internal List<string> allBladesMacs = new List<string> {};
        internal string SKUDefinitionFile = "C:\\Maabidi\\Developement\\CM_TestAutomation\\CMVerificationBatches\\V1.5.2Test.xml";
        CM_FunctionalTests tests = new CM_FunctionalTests("Http://CMMTHDVT01:8000","Admin", "$pl3nd1D");


        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// A test for AddChassisControllerUser
        /// </summary>
        [TestMethod]
        public void ChassisControllerUserTest()
        {
            TestsResultResponse testPassed = tests.AddChassisControllerUserTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ClearBladeLog
        /// </summary>
        [TestMethod]
        public void ClearBladeLogTest()
        {
            TestsResultResponse testPassed = tests.ClearBladeLogTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ClearChassisLog
        /// </summary>
        [TestMethod]
        public void clearchassislogtest()
        {
            TestsResultResponse testPassed = tests.Clearchassislogtest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ACSocketPower
        /// </summary>
        [TestMethod]
        public void ACSocketPowerTest()
        {
            TestsResultResponse testPassed = tests.ACSocketPowerTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetAllPowerState
        /// </summary>
        [TestMethod]
        public void GetAllPowerStateTest()
        {
            TestsResultResponse testPassed = tests.GetAllPowerStateTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetBladeDefaultPowerState
        /// </summary>
        [TestMethod]
        public void GetSetBladeDefaultPowerStateTest()
        {
            TestsResultResponse testPassed = tests.BladeDefaultPowerStateTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetBladeInfo.        
        /// </summary>
        [TestMethod]
        public void GetBladeInfoTest(int bladeId)
        {

            int randomBlade = 0;
            int bladeIndex = 1;
            bool isServer = false;
            while (!isServer && bladeIndex <= TestConfigLoaded.Population)
            {
                randomBlade = new Random().Next(1, (byte)TestConfigLoaded.Population);
                if (!this.JBODLocations.Contains(bladeIndex))
                    isServer = true;
                else
                    bladeIndex++;
            }
            TestsResultResponse testPassed = tests.VerifyBladesInfo(SKUDefinitionFile, randomBlade);
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetAllBladesInfo
        /// </summary>
        [TestMethod]
        public void GetAllBladesInfoTest()
        {
            TestsResultResponse testPassed = tests.VerifyBladesInfo(SKUDefinitionFile);
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        ///A test for GetBladeHealth. Only one blade instance
        ///</summary>
        [TestMethod]
        public void GetBladeHealthTest()
        {
            int randomBlade = 0;
            int bladeIndex = 1;
            bool isServer = false;
            while (!isServer && bladeIndex <= TestConfigLoaded.Population)
            {
                randomBlade = new Random().Next(1, (byte)TestConfigLoaded.Population);
                if (!this.JBODLocations.Contains(randomBlade))
                    isServer = true;
                else
                    bladeIndex++;
            }
            TestsResultResponse testPassed = tests.VerifyBladesHealth(SKUDefinitionFile, index: randomBlade);
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        /// <summary>
        ///A test for GetAllBladesHealth
        ///</summary>
        [TestMethod]
        public void GetAllBladesHealthTest()
        {
            TestsResultResponse testPassed = tests.VerifyBladesHealth(SKUDefinitionFile);
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        
        /// <summary>
        ///A test for GetChassisHealth
        ///</summary>
        [TestMethod()]
        public void GetChassisHealthTest()
        {
            TestsResultResponse testPassed = tests.CheckChassisHealth();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

       
        /// <summary>
        /// A test for GetAllBladesPowerLimit
        /// </summary>
        [TestMethod]
        public void AllBladesPowerLimitTest()
        {
            TestsResultResponse testPassed = tests.AllBladesPowerLimitTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetBladePowerReading
        /// </summary>
        [TestMethod]
        public void GetBladePowerReadingTest()
        {
            TestsResultResponse testPassed = tests.GetBladePowerReadingTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        /// <summary>
        /// A test for GetAllBladesPowerReading
        /// </summary>
        [TestMethod]
        public void GetAllBladesPowerReadingTest()
        {
            TestsResultResponse testPassed = tests.GetAllBladesPowerReadingTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for GetChassisInfo
        /// </summary>
        [TestMethod]
        public void GetChassisInfoTest()
        {
            TestsResultResponse testPassed = tests.CheckChassisInfo();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        /// <summary>
        /// A test for GetChassisNetworkProperties
        /// </summary>
        [TestMethod]
        public void GetChassisNetworkPropertiesTest()
        {
            TestsResultResponse testPassed = tests.GetChassisNetworkPropertiesTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for set and get BladeDefaultPowerState
        /// </summary>
        [TestMethod]
        public void BladeDefaultPowerStateTest()
        {
            TestsResultResponse testPassed = tests.BladeDefaultPowerStateTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        /// <summary>
        /// A test for ReadBladeLog 
        /// </summary>
        [TestMethod]
        public void ReadBladeLogTest()
        {
            TestsResultResponse testPassed = tests.ReadBladeLogTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ReadBladeLogWithTimestamp
        /// </summary>
        [TestMethod]
        public void ReadBladeLogWithTimestampTest()
        {
            TestsResultResponse testPassed = tests.ReadBladeLogWithTimestampTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ReadChassisLog
        /// </summary>
        [TestMethod]
        public void ReadChassisLogTest()
        {
            TestsResultResponse testPassed = tests.ReadChassisLogTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for ReadChassisLogWithTimestamp
        /// </summary>
        [TestMethod]
        public void ReadChassisLogWithTimestampTest()
        {
            TestsResultResponse testPassed = tests.ReadChassisLogWithTimestampTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetAllBladesActivePowerCycle
        /// </summary>
        [TestMethod]
        public void SetAllBladesActivePowerCycleTest()
        {
            TestsResultResponse testPassed = tests.SetAllBladesActivePowerCycleTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetAllPowerOff
        /// </summary>
        [TestMethod]
        public void SetAllPowerOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetAllPowerOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetAllBladesDefaultPowerStateOn
        /// </summary>
        [TestMethod]
        public void SetAllBladesDefaultPowerStateOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetAllBladesDefaultPowerStateOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetAllBladesLEDOnOff
        /// </summary>
        [TestMethod]
        public void SetAllBladesAttentionLEDOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetAllBladesAttentionLedOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        
        /// <summary>
        /// A test for SetBladePowerLimitOn
        /// </summary>
        [TestMethod]
        public void SetBladePowerLimitOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetBladePowerLimitOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetBladeActivePowerCycle
        /// </summary>
        [TestMethod]
        public void SetBladeActivePowerCycleTest()
        {
            TestsResultResponse testPassed = tests.SetBladeActivePowerCycleTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for Set and Get BladeLEDOn
        /// </summary>
        [TestMethod]
        public void BladeLEDTest()
        {
            TestsResultResponse testPassed = tests.BladeLedTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for SetChassisLEDOn
        /// </summary>
        [TestMethod]
        public void SetChassisLEDOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetChassisLedOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for poweroff
        /// </summary>
        [TestMethod]
        public void SetPowerOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetPowerOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for setBladeOff
        /// </summary>
        [TestMethod]
        public void SetBladeOnOffTest()
        {
            TestsResultResponse testPassed = tests.SetBladeOnOffTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        
        /// <summary>
        ///A test for SetNextBoot
        ///</summary>
        [TestMethod()]
        public void SetNextBootTest()
        {
            TestsResultResponse testPassed = tests.SetNextBootTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        
        /// <summary>
        /// A test for StressBladeSerialSession
        /// </summary>
        [TestMethod]
        public void BladeSerialSessionFunctional()
        {
            TestsResultResponse testPassed = tests.BladeSerialSessionFunctional();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for StressbladePowerActions
        /// </summary>
        [TestMethod]
        public void StressBladePowerActions()
        {
            TestsResultResponse testPassed = tests.StressPowerActionsTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }

        /// <summary>
        /// A test for StressBladeSerialSession
        /// </summary>
        [TestMethod]
        public void StressStartStopBladeSerialSession()
        {
            TestsResultResponse testPassed = tests.StressBladeSerialSession();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }
        /// <summary>
        ///A test for GetServiceVersion
        ///</summary>
        [TestMethod()]
        public void GetServiceVersionTest()
        {
            TestsResultResponse testPassed = tests.GetServiceVersionTest();
            Assert.AreEqual(ExecutionResult.Passed, testPassed.result, testPassed.ResultDescription);
        }


        /// <summary>
        ///A test for Blade hard power cycling
        ///</summary>
        //[TestMethod()]
        //public void BladePowerScyclingTest()
        //{
        //    TestsResultResponse testPassed = tests.BladePowercycle(powerCycleTestBlade, powerCycleTestBladeIP, powerCycleTestPeriod);
        //    Assert.AreEqual(executionResult.Passed, testPassed.result, testPassed.resultDescription);
        //}
    }       
}
