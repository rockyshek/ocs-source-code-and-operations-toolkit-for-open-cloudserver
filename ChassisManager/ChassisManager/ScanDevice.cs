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

namespace Microsoft.GFS.WCS.ChassisManager
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.DirectoryServices;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Win32;


    public static class ScanDevice
    {
        /// <summary>
        /// wmi namespace
        /// </summary>
        public static string NameSpace = "CIMV2";

        /// <summary>
        /// Management scope
        /// </summary>
        public static ManagementScope Scope;

        /// <summary>
        /// Windows Firewall Key Path
        /// </summary>
        public static string WindowsFirewallKeyPath = @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile";

        /// <summary>
        /// Windows Update Agent Key Path
        /// </summary>
        public static string WindowsUpdateAgentkeyPath = @"Software\Policies\Microsoft\Windows\WindowsUpdate\AU";

        /// <summary>
        /// Windows Firewall Key Name
        /// </summary>
        public static string WindowsFirewallKeyName = "EnableFirewall";

        /// <summary>
        /// Windows Update Agent Key Name
        /// </summary>
        public static string WindowsUpdateAgentKeyName = "AUOptions";

        /// <summary>
        /// Boot status policy. 
        /// </summary>
        public enum BootStatusPolicy : int
        {
            BootStatusPolicyDisplayAllFailures = 0,  // Display all boot failures.
            BootStatusPolicyIgnoreAllFailures = 1, // Ignore all boot failures.
            BootStatusPolicyIgnoreShutdownFailures = 2, // Ignore all shutdown failures.
            BootStatusPolicyIgnoreBootFailures = 3, // Ignore all boot failures.
            BootStatusPolicyIgnoreCheckpointFailures = 4, // Ignore checkpoint failures.
            BootStatusPolicyDisplayShutdownFailures = 5,  // Display shutdown failures.
            BootStatusPolicyDisplayBootFailures = 6,  // Display boot failures.
            BootStatusPolicyDisplayCheckpointFailures = 7, // Display checkpoint failures.
            BootStatusPolicyUnknown = 8 // Unknown
        }


        /// <summary>
        /// Initilaize the management scope
        /// </summary>
        public static void InitializeScope()
        {
            Scope = new ManagementScope(String.Format("\\\\{0}\\root\\{1}", "localhost", NameSpace), null);
            Scope.Connect();
        }

        /// <summary>
        /// Get list of all services with the properties.
        /// </summary>
        /// <returns></returns>
        public static List<Contracts.ScanServices> ListAllServices()
        {
            List<Contracts.ScanServices> services = new List<Contracts.ScanServices>();

            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Service");

                using (ManagementObjectSearcher objSearch = new ManagementObjectSearcher(Scope, query))
                {
                    ManagementObjectCollection objCollection = objSearch.Get();

                    foreach (ManagementObject service in objCollection)
                    {
                        Contracts.ScanServices sc = new Contracts.ScanServices();

                        sc.ServiceName = service["DisplayName"].ToString();
                        sc.CurrentState = service["State"].ToString();
                        sc.StartUpType = service["StartMode"].ToString();
                        if (service["Description"] != null)
                        {
                            sc.Detail = service["Description"].ToString();
                        }
                        sc.completionCode = Contracts.CompletionCode.Success;
                        services.Add(sc);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return services;
        }

        /// <summary>
        /// Scan device and list all users.
        /// </summary>
        /// <returns>List of users</returns>
        public static List<String> ListAllUsers()
        {
            List<string> users = new List<string>();
            
            string caption = string.Empty;
            string enabled = string.Empty;
            string password = string.Empty;

            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_UserAccount");

                using (ManagementObjectSearcher objSearch = new ManagementObjectSearcher(Scope, query))
                {
                    ManagementObjectCollection objCollection = objSearch.Get();

                    foreach (ManagementObject user in objCollection)
                    {
                        caption = user["Caption"].ToString();
                        enabled = user["Disabled"].ToString();
                        password = user["PasswordExpires"].ToString();

                        users.Add(string.Format("User: {0} \t Account Disabled: {1}   Password Expires: {2}", caption, enabled, password));
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return users;
        }

        /// <summary>
        /// Scan device and list all groups.
        /// </summary>
        /// <returns>List of groups</returns>
        public static List<Contracts.UserGroup> ListAllGroupsAndMembers()
        {
            List<Contracts.UserGroup> userGroups = new List<Contracts.UserGroup>();

            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Group WHERE LocalAccount = TRUE");

                using (ManagementObjectSearcher objSearch = new ManagementObjectSearcher(Scope, query))
                {
                    ManagementObjectCollection objCollection = objSearch.Get();

                    foreach (ManagementObject group in objCollection)
                    {
                        Contracts.UserGroup grp = new Contracts.UserGroup();
                        grp.Group = group["Caption"].ToString();
                        grp.Members = GetAllMembersInGroup(group["Domain"].ToString(), group["Name"].ToString());
                        grp.completionCode = Contracts.CompletionCode.Success;
                        userGroups.Add(grp);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return userGroups;
        }

        /// <summary>
        /// Get all members in a group
        /// </summary>
        /// <param name="domain">domain</param>
        /// <param name="grpName">group name</param>
        /// <returns>list of members</returns>
        public static List<string> GetAllMembersInGroup(string domain, string grpName)
        {
            List<string> members = new List<string>();

            try
            {
                ObjectQuery query = new ObjectQuery("select * from Win32_GroupUser where GroupComponent=\"Win32_Group.Domain='" + domain + "',Name='" + grpName + "'\"");
               
                using (ManagementObjectSearcher objSearch = new ManagementObjectSearcher(Scope, query))
                {
                    ManagementObjectCollection objCollection = objSearch.Get();

                    foreach (ManagementObject obj in objCollection)
                    {
                        ManagementPath path = new ManagementPath(obj["PartComponent"].ToString());

                        // get the user object
                        ManagementObject objnew = new ManagementObject();
                        objnew.Path = path;
                        objnew.Get();

                        // Add member to list
                        members.Add(objnew["Caption"].ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return members;
        }

        /// <summary>
        /// Detect if windows firewall is enabled/disabled
        /// </summary>
        public static bool IsWindowsFirewallEnabled()
        {
            bool isFirewallEnabled= false;
            try
            {
                // Check if firewall enabled
                if(ReadRegistry<int>(WindowsFirewallKeyPath, WindowsFirewallKeyName) == 1)
                    isFirewallEnabled = true;
                
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return isFirewallEnabled;
        }

        /// <summary>
        /// Check if windows update are enabled/disabled
        /// AUOptions 
        ///    • 0 = Never check for Updates.
        /// </summary>
        /// <returns></returns>
        public static bool IsWindowsUpdateEnabled()
        {
            bool flag = false;

            try
            {
                // Check if windows update enabled
                if (ReadRegistry<int>(WindowsUpdateAgentkeyPath, WindowsUpdateAgentKeyName) != 0)
                    flag = true;
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }
            return flag;
        }

        /// <summary>
        /// Check if Chassis Manager SSL certificate is installed.
        /// </summary>
        /// <returns>True/False</returns>
        public static bool IsChassisManagerSSLCertInstalled()
        {
            bool isCertInstalled = false;
            
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                // look for Chassis Manager certificate
                X509Certificate2Collection foundCerts = store.Certificates.Find(X509FindType.FindBySubjectName, ConfigLoaded.SslCertificateName, false);

                if (foundCerts.Count != 0)
                {
                    // Matching certificate found
                    isCertInstalled = true;
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }                
            finally
            {
                store.Close();
            }

            return isCertInstalled;
        }

        public static bool IsEMSEnabled()
        {
            bool isEMSEnabled = false;

            try
            {
                // {9dea862c-5cdd-4e70-acc1-f32b344d4795} is the GUID of the System BcdStore
                ManagementObject scopeObj = new ManagementObject(new ManagementScope(@"root\WMI"), new ManagementPath("root\\WMI:BcdObject.Id=\"{9dea862c-5cdd-4e70-acc1-f32b344d4795}\",StoreFilePath=\"\""), null);

                ManagementBaseObject elementObj = null;
                elementObj = scopeObj.GetMethodParameters("GetElement");

                // Get the list of IDs from 0x24000001, which is a BCD constant: BcdBootMgrObjectList_DisplayOrder
                elementObj["Type"] = ((UInt32)0x24000001);
                ManagementBaseObject BCDObj = scopeObj.InvokeMethod("GetElement", elementObj, null);
                ManagementBaseObject value = ((ManagementBaseObject)(BCDObj.Properties["Element"].Value));

                // Get list of Ids
                string[] idList = (string[])value.GetPropertyValue("Ids");

                // Define the Management object used to access the WMI info from BCD store
                scopeObj = new ManagementObject(new ManagementScope(@"root\WMI"), new ManagementPath("root\\WMI:BcdObject.Id=\"" + idList[0] + "\",StoreFilePath=\"\""), null);

                elementObj = scopeObj.GetMethodParameters("GetElement");

                // BcdOSLoaderBoolean_EmsEnabled (0x260000b0)
                // The EMS enabled setting. The element data format is BcdBooleanElement.

                elementObj["Type"] = ((UInt32)0x260000b0);
                BCDObj = scopeObj.InvokeMethod("GetElement", elementObj, null);
                value = ((ManagementBaseObject)(BCDObj.Properties["Element"].Value));

                // try get EMS enabled setting
                Boolean.TryParse(value.GetPropertyValue("boolean").ToString(), out isEMSEnabled);

                // Dispose unmanaged objects
                if (scopeObj != null)
                    scopeObj.Dispose();
                if (elementObj != null)
                    elementObj.Dispose();
                if (BCDObj != null)
                    BCDObj.Dispose();
                if (value != null)
                    value.Dispose();
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return isEMSEnabled;
        }

        /// <summary>
        /// Get Boot Status Poilcy
        /// </summary>
        /// <returns></returns>
        public static BootStatusPolicy GetBootStatusPolicy()
        {
            // initialize to unknown
            BootStatusPolicy policy = BootStatusPolicy.BootStatusPolicyUnknown;

            try
            {
                // {9dea862c-5cdd-4e70-acc1-f32b344d4795} is the GUID of the System BcdStore
                ManagementObject scopeObj = new ManagementObject(new ManagementScope(@"root\WMI"), new ManagementPath("root\\WMI:BcdObject.Id=\"{9dea862c-5cdd-4e70-acc1-f32b344d4795}\",StoreFilePath=\"\""), null);

                ManagementBaseObject elementObj = null;
                elementObj = scopeObj.GetMethodParameters("GetElement");

                // Get the list of IDs from 0x24000001, which is a BCD constant: BcdBootMgrObjectList_DisplayOrder
                elementObj["Type"] = ((UInt32)0x24000001);
                ManagementBaseObject BCDObj = scopeObj.InvokeMethod("GetElement", elementObj, null);
                ManagementBaseObject value = ((ManagementBaseObject)(BCDObj.Properties["Element"].Value));
                
                // Get list of Ids
                string[] idList = (string[])value.GetPropertyValue("Ids");

                // Define the Management object used to access the WMI info from BCD store
                scopeObj = new ManagementObject(new ManagementScope(@"root\WMI"), new ManagementPath("root\\WMI:BcdObject.Id=\"" + idList[0] + "\",StoreFilePath=\"\""), null);

                elementObj = scopeObj.GetMethodParameters("GetElement");

                // 0x250000E0 is a BCD constant: BcdOSLoaderInteger_BootStatusPolicy
                // The boot status policy. The element data format is BcdIntegerElement.

                elementObj["Type"] = ((UInt32)0x250000E0);
                BCDObj = scopeObj.InvokeMethod("GetElement", elementObj, null);
                value = ((ManagementBaseObject)(BCDObj.Properties["Element"].Value));

                int bootStatusPolicy;
                // try get boot policy setting
                Int32.TryParse(value.GetPropertyValue("Integer").ToString(), out bootStatusPolicy);

                // Convert to Enum
                policy = (BootStatusPolicy)bootStatusPolicy;

                // Dispose unmanaged objects
                if (scopeObj != null)
                    scopeObj.Dispose();
                if (elementObj != null)
                    elementObj.Dispose();
                if (BCDObj != null)
                    BCDObj.Dispose();
                if (value != null)
                    value.Dispose();
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return policy;
        }

        /// <summary>
        /// Read registry setting
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyPath"></param>
        /// <param name="keyName"></param>
        static T ReadRegistry<T>(string keyPath, string keyName)
        {
            try
            {
                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.ReadKey);
                T response = (T)regkey.GetValue(keyName);

                // dispose of unmanaged object
                if (regkey != null)
                    regkey.Dispose();

                return response;
            }
            catch (Exception e)
            {
                Tracer.WriteError(String.Format("ReadRegistry<T>: Exception {0} Trace {1}", e.Message, e.StackTrace));
            }

            return default(T);

        }

        /// <summary>
        /// Get Wcscli assembly version
        /// </summary>
        /// <returns></returns>
        public static string GetWcsCliVersion()
        {
            string wcscliVersion = null;
            string wcscliPath = null;
            try
            {
                wcscliPath = GetWcscliPath();
                if(!string.IsNullOrWhiteSpace(wcscliPath))
                {
                    wcscliVersion = AssemblyName.GetAssemblyName(wcscliPath).Version.ToString();
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(string.Format("GetWcsCliVersion: Exception: {0} Trace {1}", e.Message, e.StackTrace));
            }

            return wcscliVersion;
        }

        /// <summary>
        /// Get wcscli exe path from wcscli service properties
        /// </summary>
        /// <returns>Wcscli path</returns>
        public static string GetWcscliPath()
        {
            string path = null;
            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Service where Name= 'WcscliCOM5'");
                ManagementObjectSearcher objSearch = new ManagementObjectSearcher(Scope, query);
                ManagementObjectCollection objCollection = objSearch.Get();

                foreach (ManagementObject service in objCollection)
                {
                    path  = service["PathName"].ToString();
                }

                if (objSearch != null)
                    objSearch.Dispose();

                if (objCollection != null)
                    objCollection.Dispose();
            }
            catch (Exception e)
            {
                Tracer.WriteError(string.Format("GetWcscliPath: Exception: {0} Trace {1}", e.Message, e.StackTrace));
            }

            return path;
        }

    }
}
