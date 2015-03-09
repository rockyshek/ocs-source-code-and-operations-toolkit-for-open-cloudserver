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

using System;
using System.Collections.Generic;
using System.DirectoryServices;
using Microsoft.GFS.WCS.Contracts;

namespace ChassisValidation
{
    internal class UserManagementCommands : CommandBase
    {
        private const string DefaultLocalTestUser = "LocalTestUser";
        private readonly string defaultAdminPassword;
        private readonly string defaultAdminUserName;
        private readonly string defaultCmName;
        private readonly string labDomainName;
        
        // There is only one test domain account
        private readonly string labDomainTestUser;        

        internal UserManagementCommands(IChassisManager channel) : base(channel)
        {
        }

        internal UserManagementCommands(IChassisManager channel, Dictionary<int, IChassisManager> testChannelContexts,
            Dictionary<string, string> testEnvironment) : base(channel, testChannelContexts, testEnvironment)
        {
            this.defaultCmName = new Uri(this.TestEnvironmentSetting["CMURL"]).Host;
            this.defaultAdminPassword = this.TestEnvironmentSetting["DefaultPassword"];

            this.labDomainTestUser = this.TestEnvironmentSetting["LabDomainTestUser"];
            this.labDomainName = this.TestEnvironmentSetting["LabDomainName"];           

            this.defaultAdminUserName = "admin";
        }

        /// <summary>
        ///     Basic function validation test for User commands
        /// </summary>
        /// <returns></returns>
        public bool UserMangementBasicValidationTest()
        {
            return (
                    this.AddChassisControllerUserTest() &&
                    this.AddChassisControllerUserByAdminOperUsersTest() &&
                    this.RemoveChassisControllerUserTest() &&
                    this.ChangeChassisControllerUserPasswordbyAdminTest() &&
                    this.ChangeChassisControllerUserRolebyAdminTest() &&
                    this.ChangeChassisControllerUserPasswordReqTest() &&
                    this.AddChassisControllerValidateRolesTest() &&
                    this.ChangeChassisControllerUserRolesIdTest() &&
                    this.VerifyLocalNonAdminUserTest() &&
                    this.VerifyLocalNonWcsCmAdminUserTest() &&
                    this.VerifyLocalNonWcsCmOpUserTest() &&
                    this.VerifyLocalNonWcsCmUserTest() &&
                    this.VerifyDomainUserWcsCmAdminOpUserTest() &&
                    this.VerifyDomainUserWcsCmAdminUserTest() &&
                    this.VerifyDomainOpUserTest());
        }

        /// <summary>
        ///     This test was imported from CM_Automation
        /// </summary>
        /// <returns></returns>
        protected bool AddChassisControllerUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            Console.WriteLine("!!!!!!!!!! Starting execution of AddChassisControllerUserTest");

            string failureMessage;

            const string AdminUserName = "testAdminUser";
            const string OperatorUserName = "testOperatorUser";
            const string UserName = "testUser";

            const string AdminPass1 = "AdminPass1";
            const string AdminPass2 = "AdminPass2";
            const string OperatorPass1 = "OperatorPass1";
            const string OperatorPass2 = "OperatorPass2";
            const string UserPass1 = "UserPass1";
            const string UserPass2 = "UserPass2";

            const WCSSecurityRole AdminRole = WCSSecurityRole.WcsCmAdmin;
            const WCSSecurityRole OperatorRole = WCSSecurityRole.WcsCmOperator;
            const WCSSecurityRole UserRole = WCSSecurityRole.WcsCmUser;

            //Remove user Doesn't exist
            ChassisResponse response = this.Channel.RemoveChassisControllerUser(AdminUserName);
            if (response.completionCode != CompletionCode.UserNotFound ||
                !response.statusDescription.Equals("User not found"))
            {
                failureMessage = "!!!Failed when removing a non existant user";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            //Change user Password and role when user doesn't exist
            response = this.Channel.ChangeChassisControllerUserPassword(AdminUserName, AdminPass1);
            if (response.completionCode != CompletionCode.UserNotFound ||
                !response.statusDescription.Equals("User not found"))
            {
                failureMessage = "!!!Failed when changing password for a non existant user";

                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.ChangeChassisControllerUserRole(AdminUserName, AdminRole);
            if (response.completionCode != CompletionCode.UserNotFound ||
                !response.statusDescription.Equals("User name provided cannot be found"))
            {
                failureMessage = "!!!Failed when changing user role of a non existant user";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            //Add different users
            response = this.Channel.AddChassisControllerUser(AdminUserName, AdminPass1, AdminRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when adding an admin user.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.AddChassisControllerUser(OperatorUserName, OperatorPass1, OperatorRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when adding a an Operator user.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }
            response = this.Channel.AddChassisControllerUser(UserName, UserPass1, UserRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when adding a an new user.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }
            //Change user passwords
            response = this.Channel.ChangeChassisControllerUserPassword(AdminUserName, AdminPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing Admin password.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.ChangeChassisControllerUserPassword(OperatorUserName, OperatorPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing Operator password.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.ChangeChassisControllerUserPassword(UserName, UserPass2);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing User password.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            //Change user roles
            response = this.Channel.ChangeChassisControllerUserRole(AdminUserName, UserRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing User Role.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.ChangeChassisControllerUserRole(OperatorUserName, AdminRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing User Role.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.ChangeChassisControllerUserRole(UserName, OperatorRole);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when changing User Role.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            //Remove all users
            response = this.Channel.RemoveChassisControllerUser(AdminUserName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when removing Admin user.";
                Console.WriteLine(failureMessage);
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.RemoveChassisControllerUser(OperatorUserName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when removing Operator user.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            response = this.Channel.RemoveChassisControllerUser(UserName);
            if (response.completionCode != CompletionCode.Success)
            {
                failureMessage = "!!!Failed when removing a user.";
                CmTestLog.Info(failureMessage);
                testPassed = false;
            }

            failureMessage = "!!!!!!!!! Successfully finished execution of ChassisControllerUser tests.............";
            CmTestLog.Info(failureMessage);
            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     AddChassisControllerUserByAdminOperUsersTest: Verify only users from WcsCmAdmin group is able to execute the
        ///     command.
        ///     Prerequisites (tools/hw/sw needed)
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network.
        ///     If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions:
        ///     1- Login and ensure the the CM under test does not have a user named testUser, testUser1, or testUser2 for this
        ///     test
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        /// </summary>
        /// <returns>True, iff WcsCMAdmin group user can execute this command. Otherwise false.</returns>
        protected bool AddChassisControllerUserByAdminOperUsersTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            const string TestCaseUser = "WcsTestCaseUser";
            try
            {
                CmTestLog.Info("Starting TEST AddControllerUser Test");

                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    try
                    {
                        // Use the Domain User Channel
                        this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                        // Try to add new test user using different user context
                        ChassisResponse restRspnsRslt = this.TestChannelContext.AddChassisControllerUser(TestCaseUser,
                            this.defaultAdminPassword,
                            roleId);

                        if (restRspnsRslt.completionCode == CompletionCode.Success &&
                            roleId != WCSSecurityRole.WcsCmAdmin)
                        {
                            string failureMessage = string.Format("Failed to Add User: User Type {0}", roleId);

                            CmTestLog.Failure(failureMessage);

                            testPassed = false;
                            // Remove user that was added successfully
                            // Test was successfully Remove users
                            restRspnsRslt = this.Channel.RemoveChassisControllerUser(TestCaseUser);
                        }
                        else if (restRspnsRslt.completionCode != CompletionCode.Success &&
                                 restRspnsRslt.completionCode == CompletionCode.UserAccountExists)
                        {
                            // This is expected if user is already there and Admin user is trying to add user
                            restRspnsRslt = this.Channel.RemoveChassisControllerUser(TestCaseUser);
                        }
                        else if (restRspnsRslt.completionCode == CompletionCode.Success)
                        {
                            // Test was successful Remove users
                            restRspnsRslt = this.Channel.RemoveChassisControllerUser(TestCaseUser);
                        }
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                        }
                    }
                }
                CmTestLog.Info("Completed TEST AddControllerUser Test");

                // Clean up
                this.Channel.RemoveChassisControllerUser(TestCaseUser);
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
        ///     RemoveChassisControllerUser: Verify only users from WcsCmAdmin are able to execute the command.
        ///     Prerequisites (tools/hw/sw needed)
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        /// </summary>
        /// <returns>True, iff WcsCMAdmin group user can execute this command. Otherwise false.</returns>
        protected bool RemoveChassisControllerUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            const string TestCaseUser = "WcsTestCaseUser";

            try
            {
                CmTestLog.Info("Starting set up Remove Chassis Controller User ");

                //Create test users
                this.ResetTestControlUser(TestCaseUser, false);

                CmTestLog.Info("Completed Set up RemoveChassis Dummy Users");

                CmTestLog.Info("Starting RemoveChassisControllerUserTest Chassis Controller User ");

                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    try
                    {
                        // Use different user context
                        this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                        // try to delete Remove users
                        foreach (WCSSecurityRole groupId in Enum.GetValues(typeof (WCSSecurityRole)))
                        {
                            // Make call to remove test user
                            ChassisResponse restRspnsRslt =
                                this.TestChannelContext.RemoveChassisControllerUser(string.Format("{0}{1}", TestCaseUser,
                                    (int)groupId));

                            if (restRspnsRslt.completionCode == CompletionCode.Success &&
                                roleId != WCSSecurityRole.WcsCmAdmin)
                            {
                                CmTestLog.Failure(string.Format("Failed: Remove User by wrong user type {0}", roleId));
                                testPassed = false;
                            }
                            // Keep going test is successful                          
                        }

                        // Reset control Remove user and run the test again
                        this.ResetTestControlUser(TestCaseUser, false);
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                        }
                    }
                }

                // Cleanup all dummy users
                this.ResetTestControlUser(TestCaseUser, true);
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
        ///     ChangeChassisControllerUserPassword: Verify only users from WcsCmAdmin are able to execute the command
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns>True, iff WcsCMAdmin group user can execute this command. Otherwise false.</returns>
        protected bool ChangeChassisControllerUserPasswordbyAdminTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                const string TestCaseUser = "WcsTestCaseUser";

                ChassisResponse restRspnsRslt;

                CmTestLog.Info("Starting set up Add Chassis Controller User ");
                CmTestLog.Info("Starting remove automation test users accounts");

                //Add Test  case users
                try
                {
                    restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword,
                        WCSSecurityRole.WcsCmUser);
                    if (restRspnsRslt.completionCode != CompletionCode.Success &&
                        restRspnsRslt.completionCode == CompletionCode.UserAccountExists)
                    {
                        //Remove user and recreate
                        this.Channel.RemoveChassisControllerUser(TestCaseUser);
                        //Recreate test case user 
                        this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword, WCSSecurityRole.WcsCmUser);
                    }
                }
                catch
                {
                    // Test setup Ignor any error                    
                }

                // Use different user context to make calls
                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    // Login as different Test users WcsCM group role and send Rest request to update password        
                    // Use the default CM users
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    try
                    {
                        //Try to change password for test User
                        restRspnsRslt = this.TestChannelContext.ChangeChassisControllerUserPassword(TestCaseUser,
                            this.defaultAdminPassword);

                        // Only WcsCMAdmin can change the password
                        if (roleId == WCSSecurityRole.WcsCmAdmin)
                        {
                            //Success keep going
                            testPassed = true;
                        }
                        else if ((restRspnsRslt.completionCode == CompletionCode.Success) &&
                                 (roleId != WCSSecurityRole.WcsCmAdmin))
                        {
                            // Functional validation failed
                            ChassisManagerTestHelper.IsTrue(false, "Functional Validation Failed.");

                            CmTestLog.Failure(string.Format("Non WcsAdmin user were able to change role, {0}",
                                roleId));
                        }
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                        }
                    }
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
        ///     ChangeChassisControllerUserRole: Verify only users from WcsCmAdmin are able to execute the command
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns>True, iff WcsCMAdmin group user can execute this command. Otherwise false.</returns>
        protected bool ChangeChassisControllerUserRolebyAdminTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            const string TestCaseUser = "WcsTestCaseUser";

            var restRspnsRslt = new ChassisResponse();

            try
            {
                //Create TestUsers that change password
                restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword,
                    WCSSecurityRole.WcsCmUser);

                if (restRspnsRslt.completionCode != CompletionCode.Success &&
                    restRspnsRslt.completionCode == CompletionCode.UserAccountExists)
                {
                    // Reset user
                    this.Channel.RemoveChassisControllerUser(TestCaseUser);

                    this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword, WCSSecurityRole.WcsCmUser);
                }

                // Run the test
                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    // Use different user contect WcsCM group role and send Rest request to update password                                                  
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    try
                    {
                        // Try to Change to different Role
                        foreach (WCSSecurityRole groupID in Enum.GetValues(typeof (WCSSecurityRole)))
                        {
                            //Try to change WcsRole
                            restRspnsRslt = this.TestChannelContext.ChangeChassisControllerUserRole(TestCaseUser, groupID);

                            // Only WcsCMAdmin can change the password
                            if ((restRspnsRslt.completionCode == CompletionCode.Success) &&
                                (roleId != WCSSecurityRole.WcsCmAdmin))
                            {
                                // Functional validation failed
                                CmTestLog.Failure("Test case change role by WcsAdmin Validation Failed.");
                                testPassed = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            // Clean up Test user
            this.Channel.RemoveChassisControllerUser(TestCaseUser);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     ChangeChassisControllerUserPassword: Verify completion code UserPasswordDoesNotMeetRequirement if password not
        ///     valid
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns>True, iff User password meet requirement to update password. Otherwise false.</returns>
        protected bool ChangeChassisControllerUserPasswordReqTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                const string TestCaseUser = "WcsTestCaseUser";

                var restRspnsRslt = new ChassisResponse();

                //Create TestcaseUsers that change password
                restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword,
                    WCSSecurityRole.WcsCmUser);

                if (restRspnsRslt.completionCode != CompletionCode.Success &&
                    restRspnsRslt.completionCode == CompletionCode.UserAccountExists)
                {
                    // Reset test case user
                    this.Channel.RemoveChassisControllerUser(TestCaseUser);
                    //Create TestcaseUsers that change password
                    restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword,
                        WCSSecurityRole.WcsCmUser);
                }

                // Try to change password
                try
                {
                    //Test Case, update user password using admin channel
                    restRspnsRslt = this.Channel.ChangeChassisControllerUserPassword(TestCaseUser, this.defaultAdminPassword);
                    if (restRspnsRslt.completionCode != CompletionCode.Success)
                    {
                        testPassed = false;
                    }

                    //This case must fail.
                    restRspnsRslt = this.Channel.ChangeChassisControllerUserPassword(TestCaseUser, string.Empty);
                    if (restRspnsRslt.completionCode == CompletionCode.Failure &&
                        restRspnsRslt.completionCode != CompletionCode.UserPasswordDoesNotMeetRequirement)
                    {
                        testPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }

                // Cleanup Test
                this.Channel.RemoveChassisControllerUser(TestCaseUser);
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
        ///     AddChassisControllerUser: Verify only role names and corresponding integers can be used for role parameter
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool AddChassisControllerValidateRolesTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                var restRspnsRslt = new ChassisResponse();

                const string TestCaseUser = "WcsTestCaseUser";

                CmTestLog.Info("Starting set up Add Chassis Controller User ");
                CmTestLog.Info("Starting remove automation test users accounts");

                // Try to Add user with proper Role
                foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                {
                    try
                    {
                        restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword, roleId);
                        if (restRspnsRslt.completionCode != CompletionCode.Success)
                        {
                            testPassed = false;
                        }//test failed                    
                        else
                        {
                            this.Channel.RemoveChassisControllerUser(TestCaseUser);
                        }// Test passed remove test user
                    }
                    catch (Exception ex)
                    {
                        ChassisManagerTestHelper.IsTrue(false, ex.Message);
                        testPassed = false;
                    }
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
        ///     ChangeChassisControllerUserRole: Verify only role names and corresponding integers can be used for role parameter
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool ChangeChassisControllerUserRolesIdTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                const string TestCaseUser = "WcsTestCaseUser";

                //Create TestUsers that change password
                ChassisResponse restRspnsRslt = this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword,
                    WCSSecurityRole.WcsCmUser);

                if (restRspnsRslt.completionCode != CompletionCode.Success &&
                    restRspnsRslt.completionCode == CompletionCode.UserAccountExists)
                {
                    // Reset test case user
                    this.Channel.RemoveChassisControllerUser(TestCaseUser);

                    this.Channel.AddChassisControllerUser(TestCaseUser, this.defaultAdminPassword, WCSSecurityRole.WcsCmUser);
                }

                // Try to Change user with proper Role
                try
                {
                    // Try to Change to different Role
                    foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
                    {
                        //Test Case need to pass
                        restRspnsRslt = this.Channel.ChangeChassisControllerUserRole(TestCaseUser, roleId);
                        if (restRspnsRslt.completionCode != CompletionCode.Success)
                        {
                            testPassed = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ChassisManagerTestHelper.IsTrue(false, ex.Message);
                    testPassed = false;
                }

                // Cleanup Test
                this.Channel.RemoveChassisControllerUser(TestCaseUser);
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
        ///     Verify Local non admin user to the system is not able to execute any API when not added to a user group.
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyLocalNonAdminUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                // Create Local user and make call
                if (this.CreateLocalTestUser(DefaultLocalTestUser, defaultAdminPassword, defaultCmName))
                {
                    // Use the Domain User Channel
                    this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionLocalUserId];

                    try
                    {
                        // This should fail
                        ChassisResponse restRspnsRslt = this.TestChannelContext.GetServiceVersion();

                        if (restRspnsRslt.completionCode == CompletionCode.Success)
                        {
                            testPassed = false;
                            CmTestLog.Info("Local User: verify VerifyLocalNonAdminUserTest failed.");
                        }
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error.");
                        }
                    }
                }

                // Remove Test Local User
                this.RemoveLocalUser(DefaultLocalTestUser, defaultCmName);
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
        ///     Verify Local non admin user to the system is able to execute all APIs when added to a WcsCmAdmin user group.
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyLocalNonWcsCmAdminUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                // Create Local user and make call
                if (this.CreateLocalTestUser(DefaultLocalTestUser, defaultAdminPassword, defaultCmName))
                {
                    // Add Test local user to group
                    if (this.LocalTestUserToWcsCMroup(DefaultLocalTestUser, defaultCmName, WCSSecurityRole.WcsCmAdmin))
                    {
                        // Use the Domain User Channel
                        this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionLocalUserId];

                        // Execute test
                        ChassisResponse restRspnsRslt = this.TestChannelContext.GetServiceVersion();
                        if (restRspnsRslt.completionCode != CompletionCode.Success)
                        {
                            testPassed = false;
                            CmTestLog.Failure("Local User: verify VerifyLocalNonAdminUserTest failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            //Test cleanup
            this.RemoveLocalUser(DefaultLocalTestUser, defaultCmName);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify Local non admin user to the system is able to execute Operator APIs when added to a WcsCmOperator user
        ///     group.
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyLocalNonWcsCmOpUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                // Create Local user and make call
                if (this.CreateLocalTestUser(DefaultLocalTestUser, defaultAdminPassword, defaultCmName))
                {
                    // Add Test local user to group
                    if (this.LocalTestUserToWcsCMroup(DefaultLocalTestUser, defaultCmName, WCSSecurityRole.WcsCmOperator))
                    {
                        // Use the Domain User Channel
                        this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionLocalUserId];

                        ChassisResponse restRspnsRslt = this.TestChannelContext.GetServiceVersion();
                        if (restRspnsRslt.completionCode != CompletionCode.Success)
                        {
                            testPassed = false;
                            CmTestLog.Failure("Local User: verify VerifyLocalNonOperatorUserTest failed ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            //Test cleanup
            this.RemoveLocalUser(DefaultLocalTestUser, defaultCmName);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify Local non admin user to the system is able to execute User APIs when added to a WcsCmUser group.
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyLocalNonWcsCmUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                // Create Local user and make call
                if (this.CreateLocalTestUser(DefaultLocalTestUser, defaultAdminPassword, defaultCmName))
                {
                    // Add Test local user to group
                    if (this.LocalTestUserToWcsCMroup(DefaultLocalTestUser, defaultCmName, WCSSecurityRole.WcsCmUser))
                    {
                        // Use the Domain User Channel
                        this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionLocalUserId];

                        ChassisResponse restRspnsRslt = this.TestChannelContext.GetServiceVersion();
                        if (restRspnsRslt == null)
                        {
                            throw new ArgumentNullException("restRspnsRslt");
                        }
                        if (restRspnsRslt.completionCode != CompletionCode.Success)
                        {
                            testPassed = false;
                            CmTestLog.Failure("Local User: verify VerifyLocalNonOperatorUserTest failed ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                testPassed = false;
            }

            //Test cleanup
            this.RemoveLocalUser(DefaultLocalTestUser, defaultCmName);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        ///     Verify Domain user belongs to the WcsCMAdmin and WcsCMOperator groups. User is able to execute all APIs
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyDomainUserWcsCmAdminOpUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                if (this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmAdmin) &&
                    this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmOperator))
                {
                    // Use the Domain User Channel
                    this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionDomainUserId];

                    ChassisResponse restRspnsRslt = this.Channel.GetServiceVersion();
                    if (restRspnsRslt.completionCode != CompletionCode.Success)
                    {
                        string failureMessage = "Local User: verify VerifyLocalNonOperatorUserTest failed ";
                        testPassed = false;
                        CmTestLog.Failure(failureMessage);
                    }
                }

                //Test Clean
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmAdmin);
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmOperator);
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
        ///     Verify Domain user belongs to the WcsCMAdmin and WcsCMUser groups. User is able to execute all APIs
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyDomainUserWcsCmAdminUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                if (this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmAdmin) &&
                    this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmUser))
                {
                    // Use the Domain User Channel
                    this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionDomainUserId];

                    ChassisResponse restRspnsRslt = this.TestChannelContext.GetServiceVersion();
                    if (restRspnsRslt == null)
                    {
                        throw new ArgumentNullException("restRspnsRslt");
                    }
                    if (restRspnsRslt.completionCode != CompletionCode.Success)
                    {
                        string failureMessage = "Local User: verify VerifyLocalNonOperatorUserTest failed ";
                        testPassed = false;
                        CmTestLog.Failure(failureMessage);
                    }
                }

                //Test Clean
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmAdmin);
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmUser);
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
        ///     Verify Domain user belongs to the WcsCMOperator and WcsCMUser groups. User is not able to execute APIs that require
        ///     admin rights
        ///     Prerequisites (tools/hw/sw needed)
        ///     =========================
        ///     1- Chassis Manager Service is installed and running on the target CM under test.
        ///     2- Can use internet explorer from the CM or from a remote machine able to communicate with the CM through the
        ///     network. If SSL is enabled on the test CM use https instead of what is shown here.
        ///     Setup instructions
        ///     ===================
        ///     1- Login and ensure the the CM under test does not have a user named testUser for this test.
        ///     2- Ensure that the CM under test has a user named WcsAdmin added to group WcsCmAdmin for this test
        ///     3- Ensure that the CM under test has a user named WcsOperator added to group WcsCmOperator for this test
        ///     4- Ensure that the CM under test has a user named WcsUser added to group WcsCmUser for this test
        ///     5- Ensure <aValidPassword> and <aDifferentValidPassword> conform to the Windows user complexity requirements.
        /// </summary>
        /// <returns></returns>
        protected bool VerifyDomainOpUserTest()
        {
            CmTestLog.Start();
            bool testPassed = true;

            try
            {
                if (this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmUser) &&
                    this.AddDomainUserToLocalWcsRole(WCSSecurityRole.WcsCmOperator))
                {
                    // Use the Domain User Channel
                    this.TestChannelContext = this.ListTestChannelContexts[CmConstants.TestConnectionDomainUserId];

                    try
                    {
                        ChassisResponse restRspnsRslt = this.TestChannelContext.SetAllPowerOn();
                        if (restRspnsRslt.completionCode == CompletionCode.Success)
                        {
                            string failureMessage = "Local User: verify VerifyLocalNonOperatorUserTest failed ";
                            testPassed = false;
                            CmTestLog.Failure(failureMessage);
                        }
                    }
                    catch (Exception e)
                    {
                        // Check error is due to permission HTTP 401 unauthorize
                        if (!e.Message.Contains("401"))
                        {
                            // Test failed, http response should contain http 401 error
                            CmTestLog.Failure("We are expecting 401 error, but we received 400 instead.");
                        }
                    }
                }

                //Test Clean
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmUser);
                this.RemoveDomainuserFromLocalWcsRole(labDomainTestUser, WCSSecurityRole.WcsCmOperator);
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
        ///     Create local test user account during the test
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userPassword"></param>
        /// <param name="testCmUri"></param>
        /// <returns></returns>
        private bool CreateLocalTestUser(string userName, string userPassword, string testCmUri)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            if (userPassword == null)
            {
                throw new ArgumentNullException("userPassword");
            }
            if (testCmUri == null)
            {
                throw new ArgumentNullException("testCmUri");
            }
            try
            {
                using (
                var ad = new DirectoryEntry(string.Format("WinNT://{0},computer", testCmUri), this.defaultAdminUserName,
                    this.defaultAdminPassword))
                {
                    DirectoryEntry newUser = ad.Children.Add(userName, "user");
                    newUser.Invoke("SetPassword", new object[] { userPassword });
                    newUser.Invoke("Put", new object[] { "Description", "Test User for Test cases" });
                    newUser.CommitChanges();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Add local test user account to WcsCM group.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="testCmUri"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private bool LocalTestUserToWcsCMroup(string userName, string testCmUri, WCSSecurityRole roleId)
        {
            try
            {
                using (
                var ad = new DirectoryEntry(string.Format("WinNT://{0},computer", testCmUri), this.defaultAdminUserName,
                    this.defaultAdminPassword))
                {
                    DirectoryEntries users = ad.Children;
                    DirectoryEntry user = users.Find(userName);

                    DirectoryEntry grp = ad.Children.Find(roleId.ToString(), "group");
                    grp.Invoke("Add", new object[] { user.Path });
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Clean up test users account from Local machine
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="testCmUri"></param>
        /// <returns></returns>
        private bool RemoveLocalUser(string userName, string testCmUri)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            if (testCmUri == null)
            {
                throw new ArgumentNullException("testCmUri");
            }
            try
            {
                //Delete user from Local machine
                using (
                var ad = new DirectoryEntry(string.Format("WinNT://{0},computer", testCmUri), this.defaultAdminUserName,
                    this.defaultAdminPassword))
                {
                    DirectoryEntries users = ad.Children;
                    DirectoryEntry user = users.Find(userName);
                    users.Remove(user);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private bool AddDomainUserToLocalWcsRole(WCSSecurityRole roleId)
        {
            try
            {
                //Add domain test user to local Wcs group
                using (
                var localMachine = new DirectoryEntry(string.Format("WinNT://{0},computer", this.defaultCmName),
                    this.defaultAdminUserName, this.defaultAdminPassword))
                {
                    string userPath = string.Format("WinNT://{0}.Lab/{1},user", this.labDomainName, this.labDomainTestUser);
                    string groupPath = string.Format("WinNT://{0}/{1},group", this.defaultCmName, roleId);
                    using (DirectoryEntry dComUsersGrp = localMachine.Children.Find(roleId.ToString(), "group"))
                    {
                        dComUsersGrp.Invoke("Add", userPath);
                        dComUsersGrp.CommitChanges();
                        Console.WriteLine("Domain account added Successfully");
                    }
                }
            }
            catch
            {
                //Shouldn't be here, user must be already there.
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Remove test domain user from Local WcsCM group
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private bool RemoveDomainuserFromLocalWcsRole(string userName, WCSSecurityRole roleId)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            try
            {
                //Remove domain test user to local Wcs group
                using (
                var localMachine = new DirectoryEntry(string.Format("WinNT://{0},computer", this.defaultCmName),
                    this.defaultAdminUserName, this.defaultAdminPassword))
                {
                    string userPath = string.Format("WinNT://{0}.Lab/{1},user", this.labDomainName, userName);

                    string groupPath = string.Format("WinNT://{0}/{1},group", this.defaultCmName, roleId);

                    using (DirectoryEntry dComUsersGrp = localMachine.Children.Find(roleId.ToString(), "group"))
                    {
                        // Delete Domain user from a group
                        dComUsersGrp.Invoke("Remove", new object[] { userPath });
                        dComUsersGrp.CommitChanges();
                        Console.WriteLine("Domain account removed Successfully");
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Clean up test case user
        /// </summary>
        /// <returns></returns>
        private void ResetTestControlUser(string userName, bool deleteAll)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            var restRspnsRslt = new ChassisResponse();

            // Create test dummy accounts to be removed
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof (WCSSecurityRole)))
            {
                try
                {
                    if (deleteAll)
                    {
                        this.Channel.RemoveChassisControllerUser(string.Format("{0}{1}", userName, (int)roleId));
                    }
                    else
                    {
                        // Use admin connection to add users
                        this.Channel.AddChassisControllerUser(string.Format("{0}{1}", userName, (int)roleId),
                            this.defaultAdminPassword, roleId);

                        if (restRspnsRslt.completionCode != CompletionCode.UserAccountExists)
                        {
                            // Reset users
                            this.Channel.RemoveChassisControllerUser(string.Format("{0}{1}", userName, (int)roleId));

                            restRspnsRslt =
                                this.Channel.AddChassisControllerUser(string.Format("{0}{1}", userName, (int)roleId),
                                    this.defaultAdminPassword, roleId);
                        }
                    }
                }
                catch
                {
                    // ignore error
                }
            }
        }
    }
}
