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
using System.DirectoryServices;
using System.Globalization;
using System.Runtime.InteropServices;
using Config = System.Configuration.ConfigurationManager;

namespace ChassisValidation.Commands
{
    public static class AutoUserManagement
    {
        // String that passes the WcsSecurity RoleID constant along with the user from the app.config
        private readonly static string _cmAdmin    = "2_" + Config.AppSettings["AutoAdmin"];
        private readonly static string _cmOperator = "1_" + Config.AppSettings["AutoOperator"];
        private readonly static string _cmUser     = "0_" + Config.AppSettings["AutoUser"];

        // Destroy users is available but currently not being used.
        public static bool DestroyCMAutoUsers(string CMTarget)
        {
            bool allDeleted = true;

            allDeleted &= RemoveAutoUserFromCM(_cmAdmin, CMTarget);
            allDeleted &= RemoveAutoUserFromCM(_cmOperator, CMTarget);
            allDeleted &= RemoveAutoUserFromCM(_cmUser, CMTarget);
            
            if (allDeleted)
            {
                Log.Success("All automation users successfully deleted.");
                return true;
            }
            else
            {
                Log.Error("One or more automation users failed to delete.");
                return false;
            }
        }

        public static bool PrepareCMAutoUsers(string CMTarget, string WcsTestPassword)
        {
            bool allPresent = true;
            // assumes administrator
            allPresent &= CreateLocalUser(_cmAdmin, CMTarget, WcsTestPassword, "WCS CM Automation Administrator level account");
            allPresent &= CreateLocalUser(_cmOperator, CMTarget, WcsTestPassword, "WCS CM Automation Operator level account");
            allPresent &= CreateLocalUser(_cmUser, CMTarget, WcsTestPassword, "WCS CM Automation User level account");
            if (allPresent)
            {
                Log.Success("All automation users successfully created or were already present.");
                return true;
            }
            else
            {
                Log.Error("One or more automation users failed to be created.");
                return false;
            }
        }

        // Ref http://msdn.microsoft.com/en-us/library/system.directoryservices.directoryentries.add(v=vs.110).aspx
        private static bool CreateLocalUser(string username, string machineName, string password, string description)
        {
            var roleID = int.Parse(username.Split('_')[0]);
            var userName = username.Split('_')[1];
            try
            {
                DirectoryEntry WcsGroup = null;
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + machineName + ",computer", "admin", password);
                DirectoryEntry NewUser = AD.Children.Add(userName, "user");
                NewUser.Invoke("SetPassword", new object[] { password });
                NewUser.Invoke("Put", new object[] { "Description", description });
                NewUser.Invoke("Put", new object[] { "UserFlags", 0x10000 + 0x0040 });
                try
                {
                    NewUser.CommitChanges();
                    Log.Success(String.Format("\"{0}\" created successfully", userName));
                }
                catch (COMException ex)
                {
                    return (ex.Message.Contains("exists") ? true : false);
                }

                // Switch on the roleID to determine the group to add the user to
                switch (roleID)
                {
                    case 2: // Add to admin group
                        try
                        {
                            string wcsAdminGroupPath = String.Format("WinNT://{0}/{1},group", machineName, "WcsCmAdmin");
                            if (DirectoryEntry.Exists(wcsAdminGroupPath))
                            {
                                WcsGroup = new DirectoryEntry(wcsAdminGroupPath);
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                try
                                {
                                    WcsGroup.Invoke("Add", new object[] { userPath });
                                    Log.Success(String.Format("\"{0}\" added successfully to WcsCmAdmin group.", userName));
                                }
                                catch (System.Reflection.TargetInvocationException ex)
                                {
                                    //If the user is "already" in the group return "true" fast
                                    return (ex.Message.Contains("already") ? true : false);
                                }
                            }
                            else
                            {
                                DirectoryEntry NewGroup = AD.Children.Add("WcsCmAdmin", "group");
                                NewGroup.Invoke("Put", new object[] { "Description", "WCS Chassis Manager Administrators" });
                                NewGroup.CommitChanges();
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                NewGroup.Invoke("Add", new object[] { userPath });
                            }

                        }
                        catch (System.DirectoryServices.DirectoryServicesCOMException ex)
                        {
                            //If the user is "already" in the group return "true" fast
                            return (ex.Message.Contains("already") ? true : false);
                        }
                        finally
                        {
                            if (null != WcsGroup) WcsGroup.Dispose();
                            if (null != NewUser) NewUser.Dispose();
                        }
                        break;
                    case 1: // Add to operator group
                        try
                        {
                            string wcsAdminGroupPath = String.Format("WinNT://{0}/{1},group", machineName, "WcsCmOperator");
                            if (DirectoryEntry.Exists(wcsAdminGroupPath))
                            {
                                WcsGroup = new DirectoryEntry(wcsAdminGroupPath);
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                try
                                {
                                    WcsGroup.Invoke("Add", new object[] { userPath });
                                    Log.Success(String.Format("\"{0}\" added successfully to WcsOperator group.", userName));
                                }
                                catch (System.Reflection.TargetInvocationException ex)
                                {
                                    //If the user is "already" in the group return "true" fast
                                    return (ex.Message.Contains("already") ? true : false);
                                }
                            }
                            else
                            {
                                DirectoryEntry NewGroup = AD.Children.Add("WcsCmOperator", "group");
                                NewGroup.Invoke("Put", new object[] { "Description", "WCS Chassis Manager Operators" });
                                NewGroup.CommitChanges();
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                NewGroup.Invoke("Add", new object[] { userPath });
                            }

                        }
                        catch (System.DirectoryServices.DirectoryServicesCOMException ex)
                        {
                            //If the user is "already" in the group return "true" fast
                            return (ex.Message.Contains("already") ? true : false);
                        }
                        finally
                        {
                            if (null != WcsGroup) WcsGroup.Dispose();
                            if (null != NewUser) NewUser.Dispose();
                        }
                        break;
                    case 0: // Add to user group
                        try
                        {
                            string wcsAdminGroupPath = String.Format("WinNT://{0}/{1},group", machineName, "WcsCmUser");
                            if (DirectoryEntry.Exists(wcsAdminGroupPath))
                            {
                                WcsGroup = new DirectoryEntry(wcsAdminGroupPath);
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                try
                                {
                                    WcsGroup.Invoke("Add", new object[] { userPath });
                                    Log.Success(string.Format("\"{0}\" added successfully to WcsUser group.", userName));
                                }
                                catch (System.Reflection.TargetInvocationException ex)
                                {
                                    //If the user is "already" in the group return "true" fast
                                    return (ex.Message.Contains("already") ? true : false);
                                }
                            }
                            else
                            {
                                DirectoryEntry NewGroup = AD.Children.Add("WcsCmUser", "group");
                                NewGroup.Invoke("Put", new object[] { "Description", "WCS Chassis Manager Users" });
                                NewGroup.CommitChanges();
                                string userPath = String.Format(CultureInfo.CurrentUICulture, "WinNT://{0},user", userName);
                                NewGroup.Invoke("Add", new object[] { userPath });
                            }

                        }
                        catch (System.DirectoryServices.DirectoryServicesCOMException ex)
                        {
                            //Do Something with --> ex.Message.ToString();
                            return (ex.Message.Contains("already") ? true : false);
                        }
                        finally
                        {
                            if (null != AD) AD.Dispose();
                            if (null != WcsGroup) WcsGroup.Dispose();
                            if (null != NewUser) NewUser.Dispose();
                        }
                        break;
                    default:
                        break;
                }
                return true;
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException ex)
            {
                //Do Something with --> ex.Message.ToString();
                Log.Error(string.Format("Error adding \"{0}\" to CM, Error:{1}", userName, ex.Message));
                return false;
            }
        }

        // Ref http://msdn.microsoft.com/en-us/library/system.directoryservices.directoryentries.remove(v=vs.110).aspx
        private static bool RemoveAutoUserFromCM(string usernametoremove, string machineName)
        {
            // Split out the userName
            var userName = usernametoremove.Split('_')[1];

            DirectoryEntry AD = new DirectoryEntry("WinNT://" + machineName + ",computer");
            DirectoryEntries daChildren = AD.Children;
            var userToRemove = daChildren.Find(userName, "User");

            try
            {
                daChildren.Remove(userToRemove);
                //Do Something with --> ex.Message.ToString();
                Log.Success(string.Format("User {0} removed successfully.", userName));
                return true;
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException ex)
            {
                //Do Something with --> ex.Message.ToString();
                Log.Error(string.Format("Unable to remove \"{0}\" from CM, Error:{1}", userName, ex.Message));
                return false;
            }
            finally
            {
                if (null != AD) AD.Dispose();
            }
        }  
    }
}
