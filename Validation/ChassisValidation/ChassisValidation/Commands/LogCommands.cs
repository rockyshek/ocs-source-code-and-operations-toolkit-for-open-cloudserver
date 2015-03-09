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
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.IO;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.GFS.WCS.Contracts;

    class LogCommands : CommandBase
    {
        #region Fields

        private readonly string defaultCMName;
        private readonly string defaultAdminUserName;
        private readonly string defaultAdminPassword;

        #endregion

        internal LogCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts) :
            base(channel, TestChannelContexts)
        {

        }

        internal LogCommands(IChassisManager channel, Dictionary<int, IChassisManager> TestChannelContexts, Dictionary<string, string> TestEnvironment) :
            base(channel, TestChannelContexts, TestEnvironment)
        {
            defaultCMName = new Uri(TestEnvironmentSetting["CMURL"]).Host;
            defaultAdminUserName = "admin";
            defaultAdminPassword = TestEnvironmentSetting["DefaultPassword"];
        }

        /// <summary>
        /// Test Command: ReadBladeLog, ClearBladeLog. The test case verifies:
        /// The command returns completion code success on server blades;
        /// ReadBladeLog succeeds even after all logs are cleared.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ReadClearBladeLogTest()
        {
            CmTestLog.Start();
            ChassisLogResponse readLogResponse;
            BladeResponse clearLogResponse;
            bool testPassed = true;
            int[] serverLocations, jbodLocations;
            if (!this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType), out serverLocations) ||
                 !this.GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.JbodBladeType), out jbodLocations))
            {
                CmTestLog.Failure("Cannot find a server/ Jbod blade to execute automation against");
                CmTestLog.End(false);
                return false;
            }

            if (serverLocations == null || serverLocations.Length == 0)
            {
                CmTestLog.Warning("There are no server blades to execute the test against.");
            }
            else
            {
                int bladeId = serverLocations.RandomOrDefault();
                CmTestLog.Success("Found server blade at location: " + bladeId);

                CmTestLog.Info("Power on Blade# " + bladeId);
                var powerOnResponse = this.Channel.SetPowerOn(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, powerOnResponse.completionCode, string.Format("Blade# {0} is powered on", bladeId));

                CmTestLog.Info("Read logs from Blade# " + bladeId);
                readLogResponse = this.Channel.ReadBladeLog(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, readLogResponse.completionCode, "Received Read logs from Blade# " + bladeId);

                CmTestLog.Info("Clear logs on Blade# " + bladeId + " and read again");
                clearLogResponse = this.Channel.ClearBladeLog(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, clearLogResponse.completionCode, "Logs on Blade# " + bladeId + " is cleared");

                readLogResponse = this.Channel.ReadBladeLog(bladeId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, readLogResponse.completionCode, "Read logs from Blade# " + bladeId);
            }
            
            // [TFS WorkItem: 2730] ReadBladeLog: Verify command is not valid to run on JBOD blade
            if (jbodLocations == null || jbodLocations.Length == 0)
            {
                CmTestLog.Warning("There are no JBODs to execute the test against.");
            }
            else
            {
                int JbodId = jbodLocations.RandomOrDefault();
                CmTestLog.Success("Found JBOD blade at location " + JbodId);
                CmTestLog.Info("Power on Blade# " + JbodId);
                var powerOnResponse = this.Channel.SetPowerOn(JbodId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, powerOnResponse.completionCode, string.Format("JBOD at location# {0} is powered on", JbodId));

                CmTestLog.Info("Trying to read logs for JBOD");
                readLogResponse = this.Channel.ReadBladeLog(JbodId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, readLogResponse.completionCode, "Received CommandNotValidForBlade to Read logs for JBOD# " + JbodId);

                // [TFS WorkItem: 2731] ClearBladeLog: Verify command is not valid to run on JBOD blade
                CmTestLog.Info("Trying to clear logs for JBOD");
                clearLogResponse = this.Channel.ClearBladeLog(JbodId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.CommandNotValidForBlade, clearLogResponse.completionCode, "Received CommandNotValidForBlade to clear logs for JBOD# " + JbodId);
            }

            this.EmptyLocations = this.GetEmptyLocations();
            if (EmptyLocations == null || EmptyLocations.Length == 0)
            {
                CmTestLog.Warning("There are no Empty slots to execute the test against.");
            }
            else
            {
                int slotId = EmptyLocations.RandomOrDefault();
                CmTestLog.Success("Found empty slot at location " + slotId);
                CmTestLog.Info("Trying to read logs for Empty location");
                readLogResponse = this.Channel.ReadBladeLog(slotId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, readLogResponse.completionCode, "Received Timeout to Read logs for emoty slot# " + slotId);

                // [TFS WorkItem: 2731] ClearBladeLog: Verify command is not valid to run on JBOD blade
                CmTestLog.Info("Trying to clear logs for empty slot");
                clearLogResponse = this.Channel.ClearBladeLog(slotId);
                testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Timeout, clearLogResponse.completionCode, "Received Timeout to clear logs for empty slot# " + slotId);
            }
            CmTestLog.End(testPassed);
            return true;
        }

        /// <summary>
        /// Test Command: ReadBladeLogWithTimestamp. The test case verifies:
        /// The command returns completion code success;
        /// The command succeeds even after all logs are cleared.
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ReadBladeLogWithTimestampTest()
        {
            CmTestLog.Start();

            // get all server blade locations
            int[] serverLocations;
            if (!GetBladeLocations(blade => blade.bladeType.Equals(CmConstants.ServerBladeType),
                out serverLocations) || serverLocations.Length == 0)
            {
                CmTestLog.Failure("Cannot find any server blade");
                CmTestLog.End(false);
                return false;
            }
            // pick up a random server blade
            var bladeId = serverLocations.RandomOrDefault();
            CmTestLog.Info(string.Format("Pick up a random server blade# {0} for test", bladeId));

            bool testPassed = true;
            ChassisResponse response;

            // read logs from the blade
            CmTestLog.Info("Trying to read blade log with timestamp from Blade# " + bladeId);
            response = this.Channel.ReadBladeLogWithTimestamp(bladeId, DateTime.MinValue, DateTime.Now);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode, "Logs read from Blade# " + bladeId);

            // clear the logs and read again
            CmTestLog.Info("Trying to clear the logs on Blade# " + bladeId);
            response = this.Channel.ClearBladeLog(bladeId);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode, "Logs cleared on Blade# " + bladeId);

            CmTestLog.Info("Trying to read logs again from Blade# " + bladeId);
            response = this.Channel.ReadBladeLogWithTimestamp(bladeId, DateTime.MinValue, DateTime.Now);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, response.completionCode, "Logs read from Blade# " + bladeId);

            // end of the test
            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: ReadChassisLog 
        /// The test case verifies: All users can execute command
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ReadChassisLogTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentApi = "ReadChassisLog";

            try
            {
                // Verify all users can execute command : WorkItem(2580)
                bool userPassed;

                foreach (WCSSecurityRole TestUser in Enum.GetValues(typeof(WCSSecurityRole)))
                {
                    CmTestLog.Info("Calling " + currentApi + " for user type " + TestUser);
                    userPassed = true;

                    VerifyReadChassisLog(ref userPassed, TestUser);

                    ChassisManagerTestHelper.IsTrue(userPassed,
                        currentApi + ": Command executed for user " + TestUser);

                    allPassed &= userPassed;
                }
                ChassisManagerTestHelper.IsTrue(allPassed,
                    currentApi + ": All users can execute the command");
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, ex.Message);
                ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                    currentApi + ": Chassis Manager service started");
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        public bool ClearChassisLogTest()
        {
            return (
                this.ClearChassisLog() &&
                this.ClearChassisLogByAllUsers());
        }

        /// <summary>
        /// Test Command: ClearChassisLog 
        /// Test case verifies: Completion code success
        /// Verifies Log entries count after clear chassis log and read chassislog
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ClearChassisLog()
        {
            CmTestLog.Start();
            bool testPassed = true;
            ChassisLogResponse cmLogsResponse;
            ChassisResponse cmResponse;

            CmTestLog.Info(" !!!!!! Started running of clearchassislogtest !!!!!! ");

            cmResponse = this.Channel.ClearChassisLog();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmResponse.completionCode, "Received for clear chassis log");

            CmTestLog.Info("Verifying log entries after clear the chassis logs");
            cmLogsResponse = this.Channel.ReadChassisLog();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmResponse.completionCode, "Received for read chassis log");

            testPassed &= ChassisManagerTestHelper.AreEqual(CmConstants.LogCount, cmLogsResponse.logEntries.Count, "Received for log entries count");

            CmTestLog.End(testPassed);
            return testPassed;
        }
        /// <summary>
        /// Test Command: ClearChassisLog. Test case verifies:
        /// The command returns completion code success for WcsAdmin;
        /// Bad Request for WcsOperator and WcsUser
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        // [TFS WorkItem:2585] ClearChassisLog: Verify ONLY users from WcsCmAdmin are able to execute the command
        public bool ClearChassisLogByAllUsers()
        {
            CmTestLog.Start();

            bool testPassed = true;
            // Loop through different user types and ReadChassisLogWithTimestamp
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {
                try
                {
                    // Use different user context
                    this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                    CmTestLog.Info(string.Format("Trying to ClearChassisLog with user type {0} ", roleId.ToString()));

                    ChassisResponse clearChassisLog = this.TestChannelContext.ClearChassisLog();

                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, clearChassisLog.completionCode, "Received ClearChassisLog for user " + roleId.ToString());
                }
                catch (Exception ex)
                {
                    // Check error is due to permission HTTP 400 bad request
                    if (ex.Message.Contains("400") && roleId == WCSSecurityRole.WcsCmUser)
                    {
                        ChassisManagerTestHelper.IsTrue(true, "ClearChassisLog returned Bad Request for user " + roleId.ToString());
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

        public bool ReadChassisLogWithTimestampTest()
        {
            return (
                this.ReadChassisLogWithTimestamp() &&
                this.ReadChassisLogWithTimestampByAllUsers());
        }
        /// <summary>
        /// Test Command: ReadChassisLogWithTimestamp. The test case verifies:
        /// The command returns completion code success;
        /// Verifies all the entries with in the time stamp
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ReadChassisLogWithTimestamp()
        {
            CmTestLog.Start();
            bool testPassed = true;

            ChassisLogResponse cmLogReponse = null;
            ChassisLogResponse CMLogReponseWithTime = null;
            CmTestLog.Info("!!!!!!!!! Starting execution of ReadChassisLogWithTimestampTest !!!!!!!!!");

            cmLogReponse = this.Channel.ReadChassisLog();
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, cmLogReponse.completionCode, "Received for read chassis log");

            // Make sure log entries should not  be more than 50
            testPassed &= ChassisManagerTestHelper.IsTrue(cmLogReponse.logEntries.Count <= CmConstants.LogEntries, string.Format("Received {0} log entries", cmLogReponse.logEntries.Count));

            DateTime invalidStart = DateTime.Now.AddDays(1);
            DateTime invalidEnd = DateTime.Now.AddDays(2);
            CMLogReponseWithTime = this.Channel.ReadChassisLogWithTimestamp(invalidStart, invalidEnd);
            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, CMLogReponseWithTime.completionCode, "Received success for ReadChassisLogWithTimestamp");
            testPassed &= ChassisManagerTestHelper.IsTrue(CMLogReponseWithTime.logEntries.Count == 0, "Received zero entries for invalid timestamp");

            //need to clear the chassis log so the response is not too big
            ChassisResponse response = this.Channel.ClearChassisLog();

            //Add couple more calls
            this.Channel.SetChassisAttentionLEDOn();
            this.Channel.SetChassisAttentionLEDOff();

            cmLogReponse = this.Channel.ReadChassisLog();

            DateTime start = DateTime.Now.AddDays(-1);
            DateTime end = DateTime.Now.AddDays(1);
            CMLogReponseWithTime = this.Channel.ReadChassisLogWithTimestamp(start, end);

            testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, CMLogReponseWithTime.completionCode, "Received success for ReadChassisLogWithTimestamp");

            testPassed &= ChassisManagerTestHelper.IsTrue(cmLogReponse.logEntries[1].eventTime > start, "Log entires start time");
            testPassed &= ChassisManagerTestHelper.IsTrue(cmLogReponse.logEntries[cmLogReponse.logEntries.Count - 1].eventTime < end, "Log entires end time");

            int logSize = CMLogReponseWithTime.logEntries.Count;

            if (!CMLogReponseWithTime.logEntries[logSize - 1].eventDescription.Contains("SetChassisAttentionLEDOn"))
            {
                CmTestLog.Failure("!!!Failed to read the CM logs with a time range.");
                return false;
            }
            //This will require that the CM doesn't receive any other user commands.
            if (!CMLogReponseWithTime.logEntries[0].eventDescription.Contains("ReadChassisLog"))
            {
                CmTestLog.Failure("!!!Failed to read the CM logs with a time range.");
                return false;
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test Command: ReadChassisLogWithTimestamp. Test case verifies:
        /// The command returns completion code success for WcsAdmin;
        /// Bad Request for WcsOperator and WcsUser
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        // [TFS WorkItem:2577] ReadChassisLogWithTimestamp: Verify all users from are able to execute the command
        public bool ReadChassisLogWithTimestampByAllUsers()
        {
            CmTestLog.Start();

            bool testPassed = true;
            // Loop through different user types and ReadChassisLogWithTimestamp
            foreach (WCSSecurityRole roleId in Enum.GetValues(typeof(WCSSecurityRole)))
            {

                ChassisLogResponse chassisLog;
                // Use different user context
                this.TestChannelContext = this.ListTestChannelContexts[(int)roleId];

                CmTestLog.Info(string.Format("Trying to ReadChassisLogWithTimestamp with user type {0} ", roleId.ToString()));

                chassisLog = this.TestChannelContext.ReadChassisLogWithTimestamp(DateTime.MinValue, DateTime.Now);

                if (chassisLog.completionCode != CompletionCode.Success && (roleId == WCSSecurityRole.WcsCmAdmin || roleId == WCSSecurityRole.WcsCmOperator || roleId == WCSSecurityRole.WcsCmUser))
                {
                    CmTestLog.Failure(string.Format("Cannot Get Read ChassisLogs WithTimestamp With User {0}", roleId.ToString()));
                    CmTestLog.End(false);
                    return false;
                }
                else
                {
                    testPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, chassisLog.completionCode, "Received ReadChassisLogWithTimestamp for user " + roleId.ToString());
                }
            }

            CmTestLog.End(testPassed);
            return testPassed;
        }

        /// <summary>
        /// Test: Chassis Manager Logs
        /// The test case verifies:
        /// Chassis Manager Log exists
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool ChassisManagerLogsTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentTest = "ChassisManagerLogs";

            try
            {
                string logParentDirectory = @"\\" + defaultCMName + @"\c$\";
                bool foundChassisManagerTraceLog = false;

                IntPtr token = IntPtr.Zero;

                // Impersonate remote user 
                bool successLogon = LogonUser(defaultAdminUserName, defaultCMName, defaultAdminPassword,
                    (int)DwLogonType.NewCredentials, (int)DwLogonProvider.WinNT50, ref token);

                if (successLogon)
                {
                    using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(token))
                    {
                        // Verify presence of User Log : WorkItem(2271)
                        if (!Directory.Exists(logParentDirectory))
                        {
                            CmTestLog.Failure(currentTest + ": Directory to Chassis Manager Trace Log files does not exist");
                            return false;
                        }

                        foreach (string filePath in Directory.GetFiles(logParentDirectory))
                        {
                            Match fileMatch = Regex.Match(filePath, @"ChassisManagerTraceLog0[01]\.svclog");

                            if (fileMatch.Success)
                            {
                                CmTestLog.Success(currentTest + ": Verified presence of Chassis Manager Trace Log " + filePath);
                                foundChassisManagerTraceLog = true;
                                break;
                            }
                        }

                        allPassed &= foundChassisManagerTraceLog;

                        // Revert back to original user
                        context.Undo();
                    }
                }
                else
                {
                    CmTestLog.Failure("UserLogon: User failed to be created");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        /// <summary>
        /// Test: User Logs 
        /// The test case verifies:
        /// User Log exists
        /// </summary>
        /// <returns>True if all check-points pass; false, otherwise.</returns>
        public bool UserLogsTest()
        {
            CmTestLog.Start();
            bool allPassed = true;
            string currentTest = "UserLogs";

            try
            {
                string logParentDirectory = @"\\" + defaultCMName + @"\c$\";
                bool foundUserLog = false;

                IntPtr token = IntPtr.Zero;

                // Impersonate remote user 
                bool successLogon = LogonUser(defaultAdminUserName, defaultCMName, defaultAdminPassword,
                    (int)DwLogonType.NewCredentials, (int)DwLogonProvider.WinNT50, ref token);

                if (successLogon)
                {
                    using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(token))
                    {
                        // Verify presence of User Log : WorkItem(2271)
                        if (!Directory.Exists(logParentDirectory))
                        {
                            CmTestLog.Failure(currentTest + ": Directory to User Log files does not exist");
                            return false;
                        }

                        foreach (string filePath in Directory.GetFiles(logParentDirectory))
                        {
                            Match fileMatch = Regex.Match(filePath, @"ChassisManagerUserLog0[01]\.svclog");

                            if (fileMatch.Success)
                            {
                                CmTestLog.Success(currentTest + ": Verified presence of User Log " + filePath);
                                foundUserLog = true;
                                break;
                            }
                        }

                        allPassed &= foundUserLog;

                        // Revert back to original user
                        context.Undo();
                    }
                }
                else
                {
                    CmTestLog.Failure("UserLogon: User failed to be created");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ChassisManagerTestHelper.IsTrue(false, "Exception: " + ex.Message);
                allPassed = false;
            }

            CmTestLog.End(allPassed);
            return allPassed;
        }

        private void VerifyReadChassisLog(ref bool chassisPassed, WCSSecurityRole user)
        {
            string currentApi = "ReadChassisLog";
            string logParentDirectory = @"\\" + defaultCMName + @"\c$\";
            string[] userLogPaths = new string[] { null, null };

            ChassisLogResponse chassisLog = new ChassisLogResponse();
            CmTestLog.Info("TestChannelContext user " + (int)user);
            this.TestChannelContext = this.ListTestChannelContexts[(int)user];

            chassisLog = this.TestChannelContext.ReadChassisLog();

            chassisPassed &= ChassisManagerTestHelper.AreEqual(CompletionCode.Success, chassisLog.completionCode,
                currentApi + ": Completion Code success for user " + user.ToString());

            if (StartStopCmService("stop"))
                CmTestLog.Success(currentApi + ": Stopped Chassis Manager Service");
            else
            {
                CmTestLog.Failure(currentApi +
                    ": Unable to stop Chassis Manager Service. Will not check log entry contents");
                chassisPassed = false;
                return;
            }

            // Check Log entries are populated in ChassisLogResponse and not greater than 50 entries
            if (chassisLog.logEntries.Count < 1)
            {
                CmTestLog.Failure(currentApi + ": Command does not return Log Entries");
                chassisPassed = false;
                ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                    currentApi + ": Stopped Chassis Manager Service");
                return;
            }
            else if (chassisLog.logEntries.Count > 50)
            {
                CmTestLog.Failure(currentApi + ": Command returns more than 50 Log Entries");
                chassisPassed = false;
                ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                    currentApi + ": Stopped Chassis Manager Service");
                return;
            }
            else
                CmTestLog.Success(currentApi + ": Command returns between 1 and 50 Log Entries");

            IntPtr token = IntPtr.Zero;

            // Impersonate remote user 
            bool successLogon = LogonUser(defaultAdminUserName, defaultCMName, defaultAdminPassword,
                (int)DwLogonType.NewCredentials, (int)DwLogonProvider.WinNT50, ref token);

            if (successLogon)
            {
                using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(token))
                {
                    // Verify that User Logs exist to compare log entries with ChassisLogResponse
                    if (!Directory.Exists(logParentDirectory))
                    {
                        CmTestLog.Failure(currentApi + ": Directory to User Log files does not exist");
                        chassisPassed = false;
                        ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                            currentApi + ": Stopped Chassis Manager Service");
                        return;
                    }

                    foreach (string filePath in Directory.GetFiles(logParentDirectory))
                    {
                        Match fileMatch00 = Regex.Match(filePath, @"ChassisManagerUserLog00\.svclog");
                        Match fileMatch01 = Regex.Match(filePath, @"ChassisManagerUserLog01\.svclog");

                        if (fileMatch00.Success)
                            userLogPaths[0] = filePath;
                        else if (fileMatch01.Success)
                            userLogPaths[1] = filePath;
                    }

                    if (userLogPaths[0] == null && userLogPaths[1] == null)
                    {
                        CmTestLog.Failure(currentApi + ": Could not find user logs");
                        chassisPassed = false;
                        ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                            currentApi + ": Started Chassis Manager Service");
                        return;
                    }

                    // Compare and match log entries in ChassisLogResponse to User Logs in Chassis Manager
                    int entryCount = 0;
                    bool allEntriesPassed = true;
                    foreach (LogEntry entry in chassisLog.logEntries)
                    {
                        if (entry.eventDescription == null && entry.eventTime == null)
                        {
                            CmTestLog.Failure(currentApi +
                                string.Format(": Log Entry {0} returns no data for either eventDescription or eventTime or both", entryCount));
                            allEntriesPassed = false;
                            entryCount++;
                            continue;
                        }

                        // Find log entry in either UserLog00 or UserLog01
                        int userLogCount = 0;
                        bool userLogEntryFound = false;
                        string propertyValue;
                        foreach (string userLogPath in userLogPaths)
                        {
                            if (userLogPath == null)
                            {
                                CmTestLog.Info(currentApi + string.Format(": User Log {0} does not exist", userLogCount));
                                userLogCount++;
                                continue;
                            }

                            XmlReaderSettings xmlSettings = new XmlReaderSettings();
                            xmlSettings.ConformanceLevel = ConformanceLevel.Fragment;

                            XmlReader userLogReader = XmlReader.Create(userLogPath, xmlSettings);

                            try
                            {
                                while (!userLogEntryFound)
                                {
                                    while (userLogReader.Read())
                                    {
                                        if (userLogReader.Name == "ApplicationData")
                                            break;
                                    }

                                    if (userLogReader.Name != "ApplicationData")
                                    {
                                        userLogReader.Close();
                                        break;
                                    }

                                    // Read User Log Entry and condition both strings for comparison
                                    propertyValue = userLogReader.ReadElementContentAsString();
                                    propertyValue = propertyValue.Replace(@"\", "");
                                    propertyValue = propertyValue.Replace(@"(", "");
                                    propertyValue = propertyValue.Replace(@")", "");
                                    entry.eventDescription = entry.eventDescription.Replace(@"\", "");
                                    entry.eventDescription = entry.eventDescription.Replace(@"(", "");
                                    entry.eventDescription = entry.eventDescription.Replace(@")", "");

                                    Match eventTimeMatch = Regex.Match(propertyValue,
                                        entry.eventTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    Match eventDescriptionMatch = Regex.Match(propertyValue, entry.eventDescription);

                                    if (eventTimeMatch.Success && eventDescriptionMatch.Success)
                                    {
                                        CmTestLog.Success(currentApi +
                                            string.Format(": Found eventTime match and eventDescription match for entry {0} in user log {1}", entryCount, userLogCount));
                                        userLogEntryFound = true;
                                    }
                                }
                            }
                            catch (Exception exc)
                            {
                                if (exc.Message.Contains(@"Not enough )'s"))
                                {
                                    CmTestLog.Info(currentApi + string.Format(": Entry {0} throwing exception 'Not enough )'s' in User Log {1}", entryCount, userLogCount));
                                    userLogCount++;
                                    continue;
                                }
                                else
                                    throw new Exception(exc.Message);
                            }

                            if (!userLogEntryFound)
                            {
                                CmTestLog.Info(currentApi + string.Format(": User Log {0} does not contain entry {1}", userLogCount, entryCount));
                                userLogReader.Close();
                                userLogCount++;
                                continue;
                            }

                            userLogReader.Close();
                            userLogCount++;
                        }

                        if (!userLogEntryFound)
                        {
                            CmTestLog.Failure(currentApi + string.Format(": Entry {0} was not found in either user logs", entryCount));
                            allEntriesPassed = false;
                            entryCount++;
                            continue;
                        }

                        chassisPassed &= allEntriesPassed;
                        entryCount++;
                    }
                    ChassisManagerTestHelper.IsTrue(allEntriesPassed,
                        currentApi + string.Format(": All Log Entries passed", entryCount));

                    // Revert back to original user
                    context.Undo();
                }
            }
            else
            {
                CmTestLog.Failure("UserLogon: User failed to be created");
                chassisPassed = false;
            }

            ChassisManagerTestHelper.IsTrue(StartStopCmService("start"),
                currentApi + ": Started Chassis Manager Service");
        }

    }
}
