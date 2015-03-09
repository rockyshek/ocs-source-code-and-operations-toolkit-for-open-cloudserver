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

using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ChassisValidation
{
    [TestClass]
    public class ChassisManagerRestTest
    {
        private static readonly string cmUrl = ConfigurationManager.AppSettings["CM_URL"];
        private static readonly string adminUserName = ConfigurationManager.AppSettings["AdminUserName"];
        private static readonly string password = ConfigurationManager.AppSettings["Password"];

        private static readonly string testDomainTestUser = ConfigurationManager.AppSettings["LabDomainTestUser"];
        private static readonly string testDomainName = ConfigurationManager.AppSettings["LabDomainName"];

        private static CmRestTest adminUserCmTest;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ChassisValidation.Commands.AutoUserManagement.PrepareCMAutoUsers(new Uri(cmUrl).Host, password);
            adminUserCmTest = new CmRestTest(cmUrl, adminUserName, password, testDomainTestUser, testDomainName);
        }

        [TestMethod]
        public void GetChassisInfoTest()
        {
            TestResponse response = adminUserCmTest.GetChassisInfoTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Get Info Actions\Blade")]
        public void GetAllBladesInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.GetAllBladesInfoTest());
        }

        [TestMethod]
        public void GetBladeInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.GetBladeInfoTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Get Health Actions\CM")]
        [TestCategory("BVT")]
        public void GetChassisHealthTest()
        {
            Assert.IsTrue(adminUserCmTest.GetChassisHealthTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Get Health Actions\CM")]
        [TestCategory("BVT")]
        public void SetChassisLedOnOffTest()
        {
            Assert.IsTrue(adminUserCmTest.ChassisLedOnOffTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Get Health Actions\Blade")]
        [TestCategory("BVT")]
        public void GetBladeHealthTest()
        {
            Assert.IsTrue(adminUserCmTest.GetBladeHealthTest());
        }

        [TestMethod]
        public void ReadClearBladeLogTest()
        {
            Assert.IsTrue(adminUserCmTest.ReadClearBladeLogTest());
        }

        [TestMethod]
        public void ReadBladeLogWithTimestampTest()
        {
            Assert.IsTrue(adminUserCmTest.ReadBladeLogWithTimestampTest());
        }

        [TestMethod]
        public void ReadChassisLogTest()
        {
            Assert.IsTrue(adminUserCmTest.ReadChassisLogTest());
        }

        [TestMethod]
        public void ClearChassisLogTest()
        {
            Assert.IsTrue(adminUserCmTest.ClearChassisLogTest());
        }

        [TestMethod]
        public void ReadChassisLogWithTimestampTest()
        {
            Assert.IsTrue(adminUserCmTest.ReadChassisLogWithTimestampTest());
        }


        [TestMethod]
        public void UserLogsTest()
        {
            Assert.IsTrue(adminUserCmTest.UserLogsTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Hard Power")]
        public void SetGetAllPowerStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetAllPowerStateTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Hard Power")]
        public void SetGetPowerStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetPowerStateTest());
        }

        [TestMethod]
        public void SetGetNextBootTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetNextBootTest());
        }

        [TestMethod]
        [Description(@"Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Serial Session\Blade")]
        public void StartStopBladeSerialSessionTest()
        {
            TestResponse response = adminUserCmTest.StartStopBladeSerialSessionTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Serial Session\Blade")]
        public void AllBladesSerialSessionFunctionalTest()
        {
            TestResponse response = adminUserCmTest.AllBladesSerialSessionFunctionalTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Serial Session\Blade")]
        public void StartStopSendReceiveBladeSerialSessionByAllUsersTest()
        {
            TestResponse response = adminUserCmTest.StartStopSendReceiveBladeSerialSessionByAllUsersTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Serial Session\Console")]
        public void StartStopSerialPortConsoleTest()
        {
            TestResponse response = adminUserCmTest.StartStopSerialPortConsoleTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Serial Session\Console")]
        public void StartStopSendReceivePortSerialConsoleByAllUsersTest()
        {
            TestResponse response = adminUserCmTest.StartStopSendReceivePortSerialConsoleByAllUsersTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Power Cycle")]
        public void SetBladeActivePowerCycleTest()
        {
            Assert.IsTrue(adminUserCmTest.SetBladeActivePowerCycleTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Power Cycle")]
        public void SetAllBladesActivePowerCycleTest()
        {
            Assert.IsTrue(adminUserCmTest.SetAllBladesActivePowerCycleTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Soft Power")]
        public void SetGetAllBladesStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetAllBladesStateTest());
        }

        [TestMethod]
        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Soft Power")]
        public void SetGetBladeStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetBladeStateTest());
        }

        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Default Power State")]
        [TestMethod]
        public void SetGetAllBladesDefaultPowerStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetAllBladesDefaultPowerStateTest());
        }

        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power\Default Power State")]
        [TestMethod]
        public void SetGetBladeDefaultPowerStateTest()
        {
            Assert.IsTrue(adminUserCmTest.SetGetBladeDefaultPowerStateTest());
        }

        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power")]
        [TestMethod]
        public void SetPowerActionsByAllUsersTest()
        {
            Assert.IsTrue(adminUserCmTest.SetPowerActionsByAllUsersTest());
        }

        [Description(@"TFS Area Path: Mt Rainier\Microsoft\Test\Chassis Manager\Functional Testing\Rest API Commands\Power Actions\Blade Power")]
        [TestMethod]
        public void GetPowerActionsByAllUsersTest()
        {
            Assert.IsTrue(adminUserCmTest.GetPowerActionsByAllUsersTest());
        }

        [TestMethod]
        public void GetChassisManagerAssetInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.GetChassisManagerAssetInfoTest());
        }

        [TestMethod]
        public void GetPdbAssetInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.GetPdbAssetInfoTest());
        }

        [TestMethod]
        public void GetBladeAssetInfo()
        {
            Assert.IsTrue(adminUserCmTest.GetBladeAssetInfoTest());
        }

        [TestMethod]
        public void SetChassisManagerAssetInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.SetChassisManagerAssetInfoTest());
        }

        [TestMethod]
        public void SetPdbAssetInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.SetPdbAssetInfoTest());
        }

        [TestMethod]
        public void SetBladeAssetInfoTest()
        {
            Assert.IsTrue(adminUserCmTest.SetBladeAssetInfoTest());
        }

        [TestMethod]
        public void ChassisManagerLogsTest()
        {
            Assert.IsTrue(adminUserCmTest.ChassisManagerLogsTest());
        }

        [TestMethod]
        public void GetBladeMezzPassThroughModeTest()
        {
            TestResponse response = adminUserCmTest.GetBladeMezzPassThroughModeTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        public void SetBladeMezzPassThroughModeTest()
        {
            TestResponse response = adminUserCmTest.SetBladeMezzPassThroughModeTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        public void GetBladeMezzAssetInfoTest()
        {
            TestResponse response = adminUserCmTest.GetBladeMezzAssetInfoTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        public void GetPsuFirmwareStatusTest()
        {
            TestResponse response = adminUserCmTest.GetPsuFirmwarestatusTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        public void UpdatePsuFirmwareTest()
        {
            TestResponse response = adminUserCmTest.UpdatePsuFirmwareTest();
            Assert.IsTrue(response.Result, response.ResultDescription.ToString());
        }

        [TestMethod]
        public void UpdatePsuFirmwareStressTest()
        {
            Assert.IsTrue(adminUserCmTest.UpdatePsuFirmwareStressTest());
        }
    }
}
