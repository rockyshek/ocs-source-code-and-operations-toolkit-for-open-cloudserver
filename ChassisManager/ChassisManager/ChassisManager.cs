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
    using System.Diagnostics;
    using System.DirectoryServices;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Reflection;
    using System.ServiceModel.Web;
    using System.Threading;
    using Microsoft.GFS.WCS.Contracts;

    /// <summary>
    /// This class implements the service contract.
    /// </summary>
    public class ChassisManager : IChassisManager
    {
        // Class constructor
        public ChassisManager()
        {
            // Sets Web Response to be not-cache-able by client.
            WebOperationContext.Current.OutgoingResponse.Headers.Add(ConfigLoaded.CacheControl, ConfigLoaded.NoCache);
        }

        /// <summary>
        /// Get Chassis Manager product version
        /// </summary>
        /// <returns>service product version</returns>
        public Contracts.ServiceVersionResponse GetServiceVersion()
        {
            Contracts.ServiceVersionResponse serviceVersion = new ServiceVersionResponse();
            Tracer.WriteUserLog("Invoked GetServiceVersion");
            Tracer.WriteInfo("Received GetServiceVersion");

            serviceVersion.serviceVersion = null;
            serviceVersion.completionCode = Contracts.CompletionCode.Unknown;
            serviceVersion.statusDescription = String.Empty;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fileVersionInfo.ProductVersion;
                serviceVersion.serviceVersion = version;
                serviceVersion.completionCode = Contracts.CompletionCode.Success;
            }
            catch (Exception ex)
            {
                serviceVersion.completionCode = Contracts.CompletionCode.Failure;
                serviceVersion.statusDescription = String.Format("GetServiceVersion failed with exception: {0}", ex.Message);
                Tracer.WriteError("GetServiceVersion failed with exception" + ex);
            }

            return serviceVersion;

        }

        #region Chassis Network properties

        /// <summary>
        ///  Get chassis network properties
        /// </summary>
        /// <returns>Response packet containing network properties</returns>
        public ChassisNetworkPropertiesResponse GetChassisNetworkProperties()
        {
            string[] ipAddresses, subnets, gateways = null;
            string dnsHostName, dhcpServer, dnsDomain, macAddress = null;
            int physicalIndex;
            
            bool dhcpEnabled = true;

            ChassisNetworkPropertiesResponse response = new ChassisNetworkPropertiesResponse();
            response.chassisNetworkPropertyCollection = new List<ChassisNetworkProperty>();

            Tracer.WriteInfo("Received GetChassisNetworkProperties()");
            Tracer.WriteUserLog("Invoked GetChassisNetworkProperties()");
        

            // Set default completion code to unknown.
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            try
            {

                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    if (!string.IsNullOrEmpty((string)mo["MACAddress"]))
                    {
                        int logicalIndex = -1;
                        uint index = (uint)mo["Index"];
                        if (index >= 0)
                        {
                            logicalIndex = (int)index;
                        }
                        else
                        {
                            // Failed to verify Index
                            Console.WriteLine(@"Could not obtain network controller interface Index.");
                            response.completionCode = Contracts.CompletionCode.Failure;
                            return response;
                        }

                        physicalIndex = Contracts.SharedFunc.NetworkCtrlPhysicalIndex(logicalIndex);

                        // Configure specified network interface
                        if (physicalIndex > -1)
                        {
                            // If interface has IP enabled
                            if ((bool)mo["ipEnabled"])
                            {
 
                                ipAddresses = (string[])mo["IPAddress"];
                                subnets = (string[])mo["IPSubnet"];
                                gateways = (string[])mo["DefaultIPGateway"];
                                dnsHostName = (string)mo["DNSHostName"];
                                dhcpServer = (string)mo["DHCPServer"];
                                dnsDomain = (string)mo["DNSDomain"];
                                macAddress = (string)mo["MACAddress"];
                                dhcpEnabled = (bool)mo["DHCPEnabled"];

                                // response object
                                ChassisNetworkProperty cr = new ChassisNetworkProperty();

                                if (ipAddresses != null)
                                {
                                    for (int i = 0; i < ipAddresses.Count(); i++)
                                    {
                                        if (ChassisManagerUtil.CheckIpFormat(ipAddresses[i]))
                                        {
                                            if (string.IsNullOrEmpty(cr.ipAddress))
                                                cr.ipAddress = ipAddresses[i].ToString();
                                            else
                                                cr.ipAddress += ", " + ipAddresses[i].ToString();
                                        }
                                    }
                                }

                                if (subnets != null)
                                {
                                    for (int i = 0; i < subnets.Count(); i++)
                                    {
                                        if (string.IsNullOrEmpty(cr.subnetMask))
                                            cr.subnetMask = subnets[i].ToString();
                                        else
                                            cr.subnetMask += ", " + subnets[i].ToString();
                                    }
                                }

                                if (gateways != null)
                                {
                                    for (int i = 0; i < gateways.Count(); i++)
                                    {
                                        if (string.IsNullOrEmpty(cr.gatewayAddress))
                                            cr.gatewayAddress = gateways[i].ToString();
                                        else
                                            cr.gatewayAddress += ", " + gateways[i].ToString();
                                    }
                                }

                                cr.dhcpServer = dhcpServer;
                                cr.dnsDomain = dnsDomain;
                                cr.dnsHostName = dnsHostName;
                                cr.macAddress = macAddress;
                                cr.dhcpEnabled = dhcpEnabled;
                                cr.completionCode = Contracts.CompletionCode.Success;
                                cr.PhysicalIndex = physicalIndex;
                                response.chassisNetworkPropertyCollection.Add(cr);
                            }
                            else // all other interfaces (with ip not enables)
                            {
                                macAddress = (string)mo["MACAddress"];
                                // Populating interfaces only with valid mac addresses - ignoring loopback and other virtual interfaces
                                if (macAddress != null)
                                {
                                    ChassisNetworkProperty cr = new ChassisNetworkProperty();
                                    cr.macAddress = macAddress;
                                    cr.completionCode = Contracts.CompletionCode.Success;
                                    cr.PhysicalIndex = physicalIndex;
                                    response.chassisNetworkPropertyCollection.Add(cr);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("GetChassisNetworkProperties failed with exception:" + ex.Message);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("GetChassisNetworkProperties failed with exception:" + ex.Message);
            }

            // sort by physical address
            response.chassisNetworkPropertyCollection.Sort();

            response.completionCode = Contracts.CompletionCode.Success;
            return response;
        }

        #endregion

        #region Chassis User Control

        /// <summary>
        /// Method to add chassis controller user
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="passwordString">password</param>
        /// <returns>Response indicating if add user was success/failure</returns>
        public Contracts.ChassisResponse AddChassisControllerUser(string userName, string passwordString, Contracts.WCSSecurityRole role)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked AddChassisControllerUser(UserName: {0}, role: {1})", userName, role.ToString());
            try
            {
                // password never expires flag.
                int neverExpire = 0x10000;

                // Check if security role is valid
                if (!Enum.IsDefined(typeof(WCSSecurityRole), role))
                {
                    Tracer.WriteError("AddChassisControllerUser: Invalid security role " + role.ToString());
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "Input security role is invalid";
                    return response;
                }

                // Return BadRequest if any data is missing.
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(passwordString))
                {
                    Tracer.WriteError("AddChassisControllerUser: Invalid input parameters.");
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "Username or Password is null or empty";
                    return response;
                }

                userName = userName.Trim();
                passwordString = passwordString.Trim();

                if (userName == null || passwordString == null)
                {
                    Tracer.WriteError("AddChassisControllerUser: Invalid input parameters.");

                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "Username or Password is null or empty";
                    return response;
                }

                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry NewUser = AD.Children.Add(userName, "user");

                // create account
                NewUser.Invoke("SetPassword", new object[] { passwordString });
                NewUser.Invoke("Put", new object[] { "Description", "WcsCli chassis manager request" });
                NewUser.CommitChanges();

                // update properteis for password to never expire.
                int userProperties = (int)NewUser.Properties["userFlags"].Value;
                NewUser.Properties["userFlags"].Value = userProperties | neverExpire;
                NewUser.CommitChanges();

                DirectoryEntry grp;
                // Find group, if not exists, create
                grp = ChassisManagerUtil.FindGroupIfNotExistsCreate(role);

                if (grp != null)
                {
                    grp.Invoke("Add", new object[] { NewUser.Path.ToString() });

                    Tracer.WriteInfo("AddChassisControllerUser: User Account Created Successfully");
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    Tracer.WriteInfo("AddChassisControllerUser: Failed to create account, failed to add user to group");
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = String.Format("AddChassisControllerUser: Failed to create account, failed to add user to group");
                }

                return response;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("AddChassisControllerUser: failed with exception: " + ex);

                // check if password did not meet the requirements, display appropriate message to user.
                if (ex.ToString().Contains("0x800708C5") || ex.ToString().Contains("password does not meet"))
                {
                    response.completionCode = Contracts.CompletionCode.UserPasswordDoesNotMeetRequirement;
                    response.statusDescription = "User password does not meet requirement";
                }
                else if (ex.ToString().Contains("0x800708B0"))
                {
                    response.completionCode = Contracts.CompletionCode.UserAccountExists;
                    response.statusDescription = "User account already exists";
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = String.Format("AddChassisControllerUser failed. Unknown Error.");
                }
                return response;
            }
        }

        /// <summary>
        /// Method to change chassis controller user role
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="role">WCS Security role</param>
        /// <returns>Chassis Response indicating if the update user settings was a success/failure</returns>
        public Contracts.ChassisResponse ChangeChassisControllerUserRole(string userName, WCSSecurityRole role)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            DirectoryEntry grp;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog(String.Format("Invoked ChangeChassisControllerUserRole(userName: {0}, role: {1})", userName, role.ToString()));
            try
            {
                // Validate input parameters

                // Check if input user security role is valid
                if (!Enum.IsDefined(typeof(WCSSecurityRole), role))
                {
                    Tracer.WriteError("ChangeChassisControllerUser: Invalid security role " + role.ToString());
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "Input security role is invalid";
                    return response;
                }

                userName = userName.Trim();

                if (userName == null)
                {
                    Tracer.WriteError("ChangeChassisControllerUserRole: Invalid input parameters.");
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "User name provided is null";
                    return response;
                }

                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry myEntry = AD.Children.Find(userName, "user");

                // Remove user from other WCS security group , if it exists in them
                // This step is required as if the user permissions are decreased from 
                // admin to user, then he should no longer be in admin role.Similar with operator to user.

                if (role != WCSSecurityRole.WcsCmAdmin)
                {
                    ChassisManagerUtil.RemoveUserFromWCSSecurityGroups(userName, WCSSecurityRole.WcsCmAdmin);
                }

                if (role != WCSSecurityRole.WcsCmOperator)
                {
                    ChassisManagerUtil.RemoveUserFromWCSSecurityGroups(userName, WCSSecurityRole.WcsCmOperator);
                }

                if (role != WCSSecurityRole.WcsCmUser)
                {
                    ChassisManagerUtil.RemoveUserFromWCSSecurityGroups(userName, WCSSecurityRole.WcsCmUser);
                }

                // Add if user does not already exists in the given group
                if (!ChassisManagerUtil.CheckIfUserExistsInGroup(userName, role))
                {
                    // Find group if not exists create new
                    grp = ChassisManagerUtil.FindGroupIfNotExistsCreate(role);

                    if (grp != null)
                    {
                        // Add user to group
                        grp.Invoke("Add", new object[] { myEntry.Path.ToString() });
                        grp.CommitChanges();
                        grp.Close();
                    }
                    else
                    {
                        Tracer.WriteError("ChangeChassisControllerUserRole: Failed to change user role, failed to find/add group");
                        response.completionCode = Contracts.CompletionCode.Failure;
                        response.statusDescription = String.Format("ChangeChassisControllerUserRole: Failed to change user role, failed to find/add group");
                        return response;
                    }
                }

                Tracer.WriteInfo("ChangeChassisControllerUserRole: Role changed successfully");
                response.completionCode = Contracts.CompletionCode.Success;
                return response;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("ChangeChassisControllerUserRole: failed with exception: " + ex);

                // user already belongs to the role, we don't need any action hence consider it success
                if (ex.ToString().Contains("The specified account name is already a member of the group"))
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                // check if password did not meet the requirements, display appropriate message to user.
                else if (ex.ToString().Contains("0x800708C5") || ex.ToString().Contains("password does not meet"))
                {
                    response.completionCode = Contracts.CompletionCode.UserPasswordDoesNotMeetRequirement;
                    response.statusDescription = "User password does not meet system requirements";
                }
                // check the exception code for user not found
                else if (ex.ToString().Contains("0x800708AD"))
                {
                    response.completionCode = Contracts.CompletionCode.UserNotFound;
                    response.statusDescription = "User name provided cannot be found";
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = String.Format("ChangeChassisControllerUserRole failed. Unknown Error.");
                }
                return response;
            }
        }

        /// <summary>
        /// Method to change chassis controller user password to given values
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Chassis Response indicating if user password change was a success/failure</returns>
        public Contracts.ChassisResponse ChangeChassisControllerUserPassword(string userName, string newPassword)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked ChangeChassisControllerUserPassword(userName: {0})", userName);
            try
            {
                userName = userName.Trim();
                newPassword = newPassword.Trim();

                if (userName == null || newPassword == null)
                {
                    Tracer.WriteError("ChangeChassisControllerUserPassword: Invalid input parameters.");
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "User name or password is null";
                    return response;
                }

                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry myEntry = AD.Children.Find(userName, "user");

                if (myEntry != null)
                {
                    myEntry.Invoke("SetPassword", new object[] { newPassword });
                    myEntry.CommitChanges();
                    Tracer.WriteInfo("ChangeChassisControllerUserPassword: Password changed Successfully for user: {0}", userName);
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    Tracer.WriteError("ChangeChassisControllerUserPassword: Failed to change user password, User: {0} does not exists",
                        userName);
                }
                return response;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("ChangeChassisControllerUserPassword: failed with exception: " + ex);
                response.completionCode = Contracts.CompletionCode.Failure;

                // check if password did not meet the requirements, display appropriate message to user.
                if (ex.ToString().Contains("0x800708C5") || ex.ToString().Contains("password does not meet"))
                {
                    response.completionCode = Contracts.CompletionCode.UserPasswordDoesNotMeetRequirement;
                    response.statusDescription = "User password does not meet system requirements";
                }
                // check the exception code for user not found
                else if (ex.ToString().Contains("0x800708AD"))
                {
                    response.completionCode = Contracts.CompletionCode.UserNotFound;
                    response.statusDescription = "User not found";
                }
                else
                {
                    response.statusDescription = String.Format("ChangeChassisControllerUserPassword failed. Unknown Error.");
                }
                return response;
            }
        }

        /// <summary>
        /// Method to remove user. **TO-DO* Authenticate who can Add/delete user.
        /// </summary>
        /// <param name="userName">User Name</param>
        /// <returns>Chassis Response to indicate if reomve user operation was success/failure</returns>
        public Contracts.ChassisResponse RemoveChassisControllerUser(string userName)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked RemoveChassisControllerUser(userName: {0})", userName);
            try
            {
                userName = userName.Trim();

                if (userName == null)
                {
                    Tracer.WriteError("RemoveChassisControllerUser: Invalid input parameters.");
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    response.statusDescription = "Username is null";
                    return response;
                }

                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry myEntry = AD.Children.Find(userName, "user");
                AD.Children.Remove(myEntry);
                Tracer.WriteInfo("RemoveChassisControllerUser: User Account deleted Successfully");
                response.completionCode = Contracts.CompletionCode.Success;

                return response;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("RemoveChassisControllerUser: failed with exception: " + ex);

                // check the exception code for password did not meet the requirements, display appropriate message to user.
                if (ex.ToString().Contains("0x800708C5") || ex.ToString().Contains("password does not meet"))
                {
                    response.completionCode = Contracts.CompletionCode.UserPasswordDoesNotMeetRequirement;
                    response.statusDescription = "User password does not meet system requirements";
                }
                // check the exception code for user not found
                else if (ex.ToString().Contains("0x800708AD"))
                {
                    response.completionCode = Contracts.CompletionCode.UserNotFound;
                    response.statusDescription = "User not found";
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = String.Format("RemoveChassisControllerUser failed. Unknown Error.");
                }
                return response;
            }
        }

        #endregion

        #region Blade & Chassis Info

        /// <summary>
        /// Get Chassis Information
        /// </summary>
        /// <param name="bladeInfo">Set to True to get blade info </param>
        /// <param name="psuInfo">Set to True to get PSU info</param>
        /// <param name="chassisControllerInfo">Set to True to get chassis controller info</param>
        /// <param name="batteryInfo">Set to True to get battery info</param>
        /// <returns>Response packet for Chassis Info</returns>
        public ChassisInfoResponse GetChassisInfo(bool bladeInfo, bool psuInfo, bool chassisControllerInfo, bool batteryInfo)
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteInfo("Received GetChassisInfo bladeInfo = {0}, psuInfo = {1}, chassisControllerInfo = {2})", bladeInfo, psuInfo, chassisControllerInfo);
            Tracer.WriteInfo("                        batteryInfo = {0}", batteryInfo);
            Tracer.WriteUserLog("Received GetChassisInfo bladeInfo = {0}, psuInfo = {1}, chassisControllerInfo = {2})", bladeInfo, psuInfo, chassisControllerInfo);
            Tracer.WriteUserLog("                        batteryInfo = {0}", batteryInfo);

            // Check for the scenario where none of the params are specified or where all params are set to false 
            // return everything in that case.
            if (bladeInfo == false && psuInfo == false && chassisControllerInfo == false && batteryInfo == false)
            {
                bladeInfo = true;
                psuInfo = true;
                chassisControllerInfo = true;
                batteryInfo = true;
            }

            // Server side class structure to populate blade, psu and battery information
            ChassisInfoResponse cip = new ChassisInfoResponse();
            cip.completionCode = Contracts.CompletionCode.Unknown;
            cip.statusDescription = String.Empty;

            // Initialize to empty collections to begin with
            cip.bladeCollections = new List<BladeInfo>();
            cip.chassisController = null;

            if (bladeInfo)
            {
                // Loop to populate blade information for requested number of blades
                for (int loop = 1; loop <= MaxbladeCount; loop++)
                {
                    try
                    {
                        BladeInfo bladeInstance = new BladeInfo();
                        bladeInstance.bladeNumber = loop;

                        // initialize completion code to unknown to start with
                        bladeInstance.completionCode = Contracts.CompletionCode.Unknown;

                        BladeStateResponse bladeResponse = new BladeStateResponse();
                        Tracer.WriteInfo("Calling Get blade active power state");
                        bladeResponse = GetBladeState(loop);

                        bladeInstance.completionCode = bladeResponse.completionCode;

                        // Even if one succeeds, we set the function completion code to success
                        if (bladeInstance.completionCode == Contracts.CompletionCode.Success)
                        {
                            cip.completionCode = Contracts.CompletionCode.Success;
                        }
                        else if (bladeInstance.completionCode == Contracts.CompletionCode.FirmwareDecompressing)
                        {
                            cip.completionCode = Contracts.CompletionCode.FirmwareDecompressing;
                            cip.statusDescription = "Blade firmware is decompressing. Data could not be retrieved, for one or more blades";
                        }
                        else
                        {
                            // If not already set to success, set to failure, because something actually failed here
                            if (cip.completionCode != Contracts.CompletionCode.Success)
                            {
                                cip.completionCode = Contracts.CompletionCode.Failure;
                                cip.statusDescription = "Blade info could not be retrieved, for one or more blades";
                            }
                        }

                        Contracts.PowerState powerResponse = bladeResponse.bladeState;
                        Tracer.WriteInfo("powerResponse received");

                        // Get Blade Power State 
                        if (powerResponse == PowerState.ON)
                        {
                            bladeInstance.powerState = PowerState.ON;
                        }
                        else if (powerResponse == PowerState.OFF)
                        {
                            bladeInstance.powerState = PowerState.OFF;
                        }
                        else
                        {
                            bladeInstance.powerState = PowerState.NA;
                        }

                        if (bladeInstance.completionCode == Contracts.CompletionCode.Success)
                        {
                            // Get GUID, force session logon if session timed out.
                            Ipmi.DeviceGuid devGuid = WcsBladeFacade.GetSystemGuid((byte)loop, true);

                            if (devGuid.CompletionCode == (byte)CompletionCode.Success)
                            {
                                bladeInstance.bladeGuid = devGuid.Guid;
                                cip.completionCode = Contracts.CompletionCode.Success;
                            }
                            else
                            {
                                Tracer.WriteWarning("GetSystemGuid failed with Completion Code {0}", devGuid.CompletionCode);
                                bladeInstance.bladeGuid = System.Guid.Empty;

                                // If completion code not already set to success, set to failure, because something actually failed here
                                if (cip.completionCode != Contracts.CompletionCode.Success)
                                {
                                    cip.completionCode = Contracts.CompletionCode.Failure;
                                    cip.statusDescription = "Blade info could not be retrieved, for one or more blades";
                                }
                            }

                            // Any success is sufficient for this function, so only if we did not succeed, we set new value to completionCode
                            if (bladeInstance.completionCode != Contracts.CompletionCode.Success)
                            {
                                bladeInstance.completionCode =
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping(devGuid.CompletionCode);
                            }

                            // BMC Mac address should be added as a list
                            bladeInstance.bladeMacAddress = new List<NicInfo>();

                            for (byte i = 0; i < ConfigLoaded.NumNicsPerBlade; i++)
                            {
                                Ipmi.NicInfo ipmiNicInfo = WcsBladeFacade.GetNicInfo((byte)loop, (byte)(i + 1));

                                if (ipmiNicInfo.CompletionCode != (byte)CompletionCode.Success &&
                                    ipmiNicInfo.CompletionCode != (byte)CompletionCode.IpmiInvalidDataFieldInRequest)
                                {
                                    Tracer.WriteError("Nic {0} from Blade {1} returned an error code: {2}", i, loop, ipmiNicInfo.CompletionCode);
                                }
                                Contracts.NicInfo nicInfo = GetNicInfoObject(ipmiNicInfo);
                                bladeInstance.bladeMacAddress.Add(nicInfo);
                            }

                        }
                        else
                        {
                            bladeInstance.bladeGuid = System.Guid.Empty;
                            
                            // BMC Mac address should be added as a list
                            bladeInstance.bladeMacAddress = new List<NicInfo>();
                            Ipmi.NicInfo ipmiNicInfo = new Ipmi.NicInfo((byte)CompletionCode.FirmwareDecompressing);
                            Contracts.NicInfo nicInfo = GetNicInfoObject(ipmiNicInfo);
                            bladeInstance.bladeMacAddress.Add(nicInfo);
                        }


                        // bladename is BladeId
                        bladeInstance.bladeName = String.Concat("BLADE", loop);

                        // Add blade to list
                        cip.bladeCollections.Add(bladeInstance);

                    }
                    catch (Exception ex)
                    {
                        Tracer.WriteUserLog("GetChassisInfo (Blade portion) failed for blade {0} with exception: {1}", loop, ex.Message);
                        cip.completionCode = Contracts.CompletionCode.Failure;
                        cip.statusDescription = String.Format("GetChassisInfo (Blade portion) failed for blade {0} with exception: {1}", loop, ex.Message);
                    }
                }
            }

            if (psuInfo)
            {
                // Get the PSU Info.
                cip.psuCollections = GetPsuInfo();

                // if the master object is not successful, check child objects
                if (cip.completionCode != Contracts.CompletionCode.Success)
                {
                    // if Psu status received any positive results, return success.
                    foreach (PsuInfo psu in cip.psuCollections)
                    {
                        // if any children are successful, set master to success.
                        if (psu.completionCode == Contracts.CompletionCode.Success)
                        {
                            cip.completionCode = Contracts.CompletionCode.Success;
                            break; // once a match has been found escape foreach
                        }
                    }

                    // if master completion code is still unknown, replace with failure.
                    if (cip.completionCode == Contracts.CompletionCode.Unknown)
                    {
                        if (ConfigLoaded.NumPsus > 0)
                            cip.completionCode = Contracts.CompletionCode.Failure;
                        else
                        {
                            cip.completionCode = Contracts.CompletionCode.Success;
                            cip.statusDescription += "\nPower Supply monitoring not supported in sku configuration.";
                        }
                    }
                }
            }

            if (batteryInfo)
            {
                // Get the battery info
                cip.batteryCollections = GetBatteryInfo();

                // if the master object is not successful, check child objects
                if (cip.completionCode != Contracts.CompletionCode.Success)
                {
                    // if battery status received any positive results, return success.
                    foreach (BatteryInfo battery in cip.batteryCollections)
                    {
                        // if any children are successful, set master to success.
                        if (battery.completionCode == Contracts.CompletionCode.Success)
                        {
                            cip.completionCode = Contracts.CompletionCode.Success;
                            break; // once a match has been found escape foreach
                        }
                    }

                    // if master completion code is still unknown, replace with failure.
                    if (cip.completionCode == Contracts.CompletionCode.Unknown)
                    {
                        if (ConfigLoaded.NumBatteries > 0)
                            cip.completionCode = Contracts.CompletionCode.Failure;
                        else
                        {
                            cip.completionCode = Contracts.CompletionCode.Success;
                            cip.statusDescription += "\nBattery monitoring not supported in sku configuration.";
                        }
                    }
                }
            }

            // Chassis Info should be read by reading the Fru device
            if (chassisControllerInfo)
            {
                try
                {
                    //Populate chassis controller data
                    cip.chassisController = new ChassisControllerInfo();
                    ServiceVersionResponse version = GetServiceVersion();

                    if (version.completionCode == Contracts.CompletionCode.Success)
                    {
                        cip.chassisController.softwareVersion = version.serviceVersion;
                    }
                    else
                    {
                        cip.chassisController.softwareVersion = string.Format("Unable to obtain: Completion Code: {0}.", version.completionCode);
                    }

                    // get chassis network properties
                    cip.chassisController.networkProperties = new ChassisNetworkPropertiesResponse();
                    cip.chassisController.networkProperties = GetChassisNetworkProperties();
                    // Populate chassis IP address
                    if (cip.chassisController.networkProperties != null)
                    {
                        cip.chassisController.completionCode = Contracts.CompletionCode.Success;
                        cip.completionCode = Contracts.CompletionCode.Success;
                    }
                    else
                    {
                        Tracer.WriteInfo("GetChassisInfo - failed to get chassis network properties");
                        if (cip.chassisController.completionCode != Contracts.CompletionCode.Success)
                        {
                            cip.chassisController.completionCode = Contracts.CompletionCode.Failure;
                            cip.chassisController.statusDescription = String.Format("GetChassisInfo - failed to get chassis network properties");
                        }
                        if (cip.completionCode != Contracts.CompletionCode.Success)
                        {
                            cip.completionCode = Contracts.CompletionCode.Failure;
                            cip.statusDescription = "Failed to get chassis information";
                        }
                    }

                    cip.chassisController.systemUptime = (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString();

                    // Default Chassis details before reading FRU later
                    cip.chassisController.firmwareVersion = "NA";
                    cip.chassisController.hardwareVersion = "NA";
                    cip.chassisController.serialNumber = "NA";
                    cip.chassisController.assetTag = "NA";

                    Ipmi.FruDevice fruDevice = ChassisState.CmFruData.ReadFru(DeviceType.ChassisFruEeprom);

                    if (fruDevice.CompletionCode == (byte)CompletionCode.Success)
                    {
                        cip.chassisController.completionCode = Contracts.CompletionCode.Success;
                        cip.completionCode = Contracts.CompletionCode.Success;

                        cip.chassisController.firmwareVersion = fruDevice.ProductInfo.ProductVersion.ToString();
                        cip.chassisController.hardwareVersion = fruDevice.ProductInfo.Version.ToString();
                        cip.chassisController.serialNumber = fruDevice.ProductInfo.SerialNumber.ToString();
                        cip.chassisController.assetTag = fruDevice.ProductInfo.AssetTag.ToString();
                    }
                    else
                    {
                        Tracer.WriteWarning("CM Fru Read failed with completion code: {0:X}", fruDevice.CompletionCode);
                        if (cip.chassisController.completionCode != Contracts.CompletionCode.Success)
                        {
                            cip.chassisController.completionCode = Contracts.CompletionCode.Failure;
                            cip.chassisController.statusDescription =
                                String.Format("CM Fru Read failed with completion code: {0:X}", fruDevice.CompletionCode);
                        }
                        if (cip.completionCode != Contracts.CompletionCode.Success)
                        {
                            cip.completionCode = Contracts.CompletionCode.Failure;
                            cip.statusDescription = "Failed to get chassis information";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tracer.WriteUserLog(" GetChassisInfo failed with exception: " + ex.Message);
                    if (cip.completionCode != Contracts.CompletionCode.Success)
                    {
                        cip.completionCode = Contracts.CompletionCode.Failure;
                        cip.statusDescription = String.Format(" GetChassisInfo failed with exception: " + ex.Message);
                    }
                }
            }

            Tracer.WriteInfo("Return: GetChassisInfo returned, Number of Blades: {0}, Number of PSUs: {1}", cip.bladeCollections.Count(),
                cip.psuCollections.Count());

            return cip;
        }

        /// <summary>
        /// Get Blade info for given Blade ID
        /// </summary>
        /// <param name="bladeId">Blade ID (1-48)</param>
        /// <returns>Blade info response</returns>
        public BladeInfoResponse GetBladeInfo(int bladeId)
        {
            byte MaxBladeCount = (byte)ConfigLoaded.Population;

            // Server side class structure to populate blade information
            BladeInfoResponse bip = new BladeInfoResponse();
            bip.bladeNumber = bladeId;
            bip.statusDescription = String.Empty;
            bip.completionCode = Contracts.CompletionCode.Unknown;

            Tracer.WriteInfo("Received GetBladeInfo({0})", bladeId);

            Tracer.WriteUserLog("Invoked GetBladeInfo({0})", bladeId);

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeInfo", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                bip.completionCode = varResponse.completionCode;
                bip.statusDescription = varResponse.statusDescription;
                return bip;
            }


            // Get the blade information from Chassis Controller
            BladeStatusInfo bladeInfo = WcsBladeFacade.GetBladeInfo((byte)bladeId);

            if (bladeInfo.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("GetBladeInfo failed for blade: {0}, with Completion Code: {1}", bladeId,
                     Ipmi.IpmiSharedFunc.ByteToHexString((byte)bladeInfo.CompletionCode));
                bip.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)bladeInfo.CompletionCode);
                bip.statusDescription = bip.completionCode.ToString();
            }
            else
            {
                // Populate the returned object
                bip.completionCode = Contracts.CompletionCode.Success;
                bip.bladeNumber = bladeId;
                bip.firmwareVersion = bladeInfo.BmcFirmware;
                bip.hardwareVersion = bladeInfo.HardwareVersion;
                bip.serialNumber = bladeInfo.SerialNumber;
                bip.assetTag = bladeInfo.AssetTag;

                if (Enum.IsDefined(typeof(BladeTypeName), ChassisState.GetBladeType((byte)bladeId)))
                {
                    bip.bladeType = Enum.GetName(typeof(BladeTypeName), ChassisState.GetBladeType((byte)bladeId));
                }
                else
                {
                    bip.bladeType = BladeTypeName.Unknown.ToString();
                }

                // BMC Mac address should be added as a list
                bip.macAddress = new List<NicInfo>();

                for (int i = 0; i < ConfigLoaded.NumNicsPerBlade; i++)
                {
                    Ipmi.NicInfo ipmiNicInfo = WcsBladeFacade.GetNicInfo((byte)bladeId, (byte)(i + 1));

                    if (ipmiNicInfo.CompletionCode != (byte)CompletionCode.Success &&
                        ipmiNicInfo.CompletionCode != (byte)CompletionCode.IpmiInvalidDataFieldInRequest)
                    {
                        Tracer.WriteError("Nic {0} from Blade {1} returned an error code: {2}", i, bladeId, ipmiNicInfo.CompletionCode);
                    }
                    NicInfo nicInfo = GetNicInfoObject(ipmiNicInfo);
                    bip.macAddress.Add(nicInfo);
                }
            }

            return bip;
        }

        /// <summary>
        /// Get information for all blades
        /// </summary>
        /// <returns>Array of blade info response</returns>
        public GetAllBladesInfoResponse GetAllBladesInfo()
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteInfo("Received GetAllBladesInfo()");
            Tracer.WriteUserLog("Invoked GetAllBladesInfo()");

            // Server side class structure to populate blade information
            GetAllBladesInfoResponse responses = new GetAllBladesInfoResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeInfoResponseCollection = new List<BladeInfoResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            Tracer.WriteInfo("GetAllBladesInfo: Processing Blades ");

            // Loop to populate blade information for requested number of blades
            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1; // we need to get for all blades

                //Call getBladeInfo for the Blade ID
                responses.bladeInfoResponseCollection.Add(this.GetBladeInfo(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeInfoResponseCollection[loop].completionCode;

            }

            Tracer.WriteInfo("GetAllBladesInfo: Completed populating for Blades");

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;

            return responses;
        }

        #endregion

        #region Chassis & Blade Health

        /// <summary>
        /// Returns Chassis Health including Fan Speed and Health, PSU Health, Battery Health and Blade Type and Health
        /// </summary>
        /// <returns></returns>
        public Contracts.ChassisHealthResponse GetChassisHealth(bool bladeHealth = false, bool psuHealth = false, bool fanHealth = false, bool batteryHealth = false)
        {
            Tracer.WriteInfo("Received GetChassisHealth bladeHealth = {0}, psuHealth = {1}, fanHealth = {2})", bladeHealth, psuHealth, fanHealth);
            Tracer.WriteInfo("                        batteryHealth = {0}", batteryHealth);
            Tracer.WriteUserLog("Received GetChassisHealth bladeHealth = {0}, psuHealth = {1}, fanHealth = {2})", bladeHealth, psuHealth, fanHealth);
            Tracer.WriteUserLog("                        batteryHealth = {0}", batteryHealth);

            // If all options are not given by user, then default to providing all information
            if (!bladeHealth && !psuHealth && !fanHealth && !batteryHealth)
            {
                bladeHealth = psuHealth = fanHealth = batteryHealth = true;
            }

            ChassisHealthResponse response = new ChassisHealthResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (bladeHealth)
            {
                response.bladeShellCollection = new List<BladeShellResponse>();

                // Populate Blade Shell information (Type, Internal State)
                for (byte i = 1; i <= ConfigLoaded.Population; i++)
                {
                    BladeShellResponse br = new BladeShellResponse();
                    br.completionCode = Contracts.CompletionCode.Success;
                    br.bladeNumber = i;
                    br.bladeState = FunctionValidityChecker.CheckBladeStateValidity(i).PowerStatus.ToString();
                    br.bladeType = ChassisState.GetBladeTypeName(ChassisState.GetBladeType(i));
                    response.bladeShellCollection.Add(br);

                }

                response.completionCode = Contracts.CompletionCode.Success; // Always success if bladeinfo requested, since reading static variable
                Tracer.WriteInfo("Populated Blade Shell information, state and type for blades");
            }

            // Get Psu health information
            if (psuHealth)
            {
                response.psuInfoCollection = GetPsuInfo();

                // if the master object is not successful, check child objects
                if (response.completionCode != Contracts.CompletionCode.Success)
                {
                    // if it received any positive results, return success.
                    foreach (PsuInfo psu in response.psuInfoCollection)
                    {
                        // if any children are successful, set master to success.
                        if (psu.completionCode == Contracts.CompletionCode.Success)
                        {
                            response.completionCode = Contracts.CompletionCode.Success;
                            break; // once a match has been found escape foreach
                        }
                    }
                }

                if (ConfigLoaded.NumPsus == 0)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    response.statusDescription += "\nPower Supply monitoring not supported in sku configuration.";
                }
            }

            // Get battery health information
            if (batteryHealth)
            {
                // Get the battery info
                response.batteryInfoCollection = GetBatteryInfo();

                // if the master object is not successful, check child objects
                if (response.completionCode != Contracts.CompletionCode.Success)
                {
                    // if battery status received any positive results, return success.
                    foreach (BatteryInfo battery in response.batteryInfoCollection)
                    {
                        // if any children are successful, set master to success.
                        if (battery.completionCode == Contracts.CompletionCode.Success)
                        {
                            response.completionCode = Contracts.CompletionCode.Success;
                            break; // once a match has been found escape foreach
                        }
                    }
                }

                if (ConfigLoaded.NumBatteries == 0)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    response.statusDescription += "\nBattery monitoring not supported in sku configuration.";
                }
            }

            // Get Fan Health Information
            if (fanHealth)
            {
                response.fanInfoCollection = GetFanInfo();

                // if the master object is not successful, check child objects
                if (response.completionCode != Contracts.CompletionCode.Success)
                {
                    // if it received any positive results, return success.
                    foreach (FanInfo fan in response.fanInfoCollection)
                    {
                        // if any children are successful, set master to success.
                        if (fan.completionCode == Contracts.CompletionCode.Success ||
                            fan.completionCode == Contracts.CompletionCode.FanlessChassis)
                        {
                            response.completionCode = Contracts.CompletionCode.Success;
                            break; // once a match has been found escape foreach
                        }
                    }
                }

                if (ConfigLoaded.NumFans == 0)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    response.statusDescription += "\nFan monitoring not supported in sku configuration.";
                }
            }

            return response;
        }

        /// <summary>
        /// Returns Blade Information including Blade Type, and State Information, Processor information, Memory information
        /// PCie information and Hard Disk information (JBOD only)
        /// </summary>
        /// <returns></returns>
        public Contracts.BladeHealthResponse GetBladeHealth(int bladeId, bool cpuInfo = false, bool memInfo = false, bool diskInfo = false,
            bool pcieInfo = false, bool sensorInfo = false, bool tempInfo = false, bool fruInfo = false)
        {
            Tracer.WriteInfo("Received GetBladeHealth({0})", bladeId);
            Tracer.WriteUserLog("Invoked GetBladeHealth({0})", bladeId);
            BladeHealthResponse response = new BladeHealthResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeHealth", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Initialize BladeShellResponse object
            BladeShellResponse shellResp = new BladeShellResponse();
            shellResp.bladeNumber = bladeId;

            // If all options are false (default values), then return everything except sensor info.
            // Since the sensor info takes a long time to read, don't read it unless the user explicitly requests it.
            if (!cpuInfo && !memInfo && !diskInfo && !pcieInfo && !sensorInfo && !tempInfo && !fruInfo)
            {
                cpuInfo = memInfo = diskInfo = pcieInfo = tempInfo = fruInfo = true;
            }

            try
            {
                // proc, mem, disk, me, temp, power, fru, pcie, misc
                HardwareStatus hardwareStatus = WcsBladeFacade.GetHardwareInfo((byte)bladeId, cpuInfo, memInfo,
                    diskInfo, false, tempInfo, false, fruInfo, pcieInfo, sensorInfo);

                Type hwType = hardwareStatus.GetType();

                response.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)hardwareStatus.CompletionCode);

                if (hardwareStatus.CompletionCode != (byte)CompletionCode.Success)
                {
                    Tracer.WriteError("GetBladeHealth: Hardware status command failed with completion code: {0:X}", hardwareStatus.CompletionCode);
                }
                else
                {
                    if (hardwareStatus.CompletionCode != (byte)Contracts.CompletionCode.Success)
                    {
                        shellResp.completionCode = Contracts.CompletionCode.Failure;
                        shellResp.statusDescription = Contracts.CompletionCode.Failure + ": Internal Error";
                    }
                    else
                    {
                        shellResp.completionCode = Contracts.CompletionCode.Success;
                    }

                    if (hwType == typeof(ComputeStatus))
                    {
                        ComputeStatus hwResponse = (ComputeStatus)hardwareStatus;

                        shellResp.completionCode = Contracts.CompletionCode.Success;
                        shellResp.bladeNumber = bladeId;
                        shellResp.bladeState = FunctionValidityChecker.CheckBladeStateValidity((byte)bladeId).PowerStatus.ToString();
                        shellResp.bladeType = hwResponse.BladeType;

                        // generate processor info
                        response.processorInfo = new List<ProcessorInfo>();
                        // populate processor info if required
                        if (cpuInfo)
                            foreach (KeyValuePair<byte, Ipmi.ProcessorInfo> instance in hwResponse.ProcInfo)
                            {
                                if (instance.Value.CompletionCode == (byte)CompletionCode.Success)
                                    response.processorInfo.Add(new ProcessorInfo(Contracts.CompletionCode.Success, instance.Key, instance.Value.ProcessorType.ToString(),
                                        instance.Value.ProcessorState.ToString(), instance.Value.Frequency));
                                else
                                    response.processorInfo.Add(new ProcessorInfo(
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)instance.Value.CompletionCode)));

                            }

                        // generate memory info
                        response.memoryInfo = new List<MemoryInfo>();
                        // populate memory info if required
                        if (memInfo)
                        {
                            foreach (KeyValuePair<byte, Ipmi.MemoryInfo> instance in hwResponse.MemInfo)
                            {
                                if (instance.Value.CompletionCode == (byte)CompletionCode.Success)
                                {
                                    if (instance.Value.Status == Ipmi.MemoryStatus.NotPresent)
                                    {
                                        response.memoryInfo.Add(new MemoryInfo(Contracts.CompletionCode.Success, instance.Key,
                                        instance.Value.Status.ToString(), instance.Value.Speed, instance.Value.MemorySize,
                                        instance.Value.Status.ToString(), instance.Value.Status.ToString()));
                                    }
                                    else
                                    {
                                        response.memoryInfo.Add(new MemoryInfo(Contracts.CompletionCode.Success, instance.Key,
                                            instance.Value.MemoryType.ToString(), instance.Value.Speed, instance.Value.MemorySize,
                                            instance.Value.Voltage, instance.Value.Status.ToString()));
                                    }
                                }
                                else
                                    response.memoryInfo.Add(new MemoryInfo(
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)instance.Value.CompletionCode)));
                            }
                        }

                        // initialize PcieInfo
                        response.pcieInfo = new List<PCIeInfo>();
                        // populate PcieInfo if required.
                        if (pcieInfo)
                        {
                            foreach (KeyValuePair<byte, Ipmi.PCIeInfo> instance in hwResponse.PcieInfo)
                            {
                                if (instance.Value.CompletionCode == (byte)CompletionCode.Success)
                                    response.pcieInfo.Add(new PCIeInfo(Contracts.CompletionCode.Success, instance.Key, instance.Value.CardState.ToString(),
                                        instance.Value.VendorId, instance.Value.DeviceId, instance.Value.SubsystemId));
                                else
                                    response.pcieInfo.Add(new PCIeInfo(
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)instance.Value.CompletionCode)));
                            }

                        }

                        // initialize disk info
                        response.sensors = new List<SensorInfo>();
                        // add hardware sensor info if required.
                        if (sensorInfo)
                        {
                            foreach (HardwareSensor sensor in hwResponse.HardwareSdr)
                            {
                                if (sensor.Sdr.CompletionCode == (byte)CompletionCode.Success)
                                    response.sensors.Add(new SensorInfo(Contracts.CompletionCode.Success, sensor.Sdr.SensorNumber, sensor.Sdr.SensorType.ToString(), sensor.Sdr.EntityId.ToString(),
                                        sensor.Sdr.EntityInstance.ToString(), sensor.Sdr.EntityType.ToString(), sensor.Reading.Reading.ToString(), sensor.Reading.EventDescription, sensor.Sdr.Description));
                                else
                                    response.sensors.Add(new SensorInfo(
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)sensor.Sdr.CompletionCode)));
                            }

                        }
                        // add temp sensor info if required.
                        if (tempInfo)
                        {
                            foreach (HardwareSensor sensor in hwResponse.TempSensors)
                            {
                                if (sensor.Sdr.CompletionCode == (byte)CompletionCode.Success)
                                    response.sensors.Add(new SensorInfo(Contracts.CompletionCode.Success, sensor.Sdr.SensorNumber, sensor.Sdr.SensorType.ToString(), sensor.Sdr.EntityId.ToString(),
                                        sensor.Sdr.EntityInstance.ToString(), sensor.Sdr.EntityType.ToString(), sensor.Reading.Reading.ToString(), sensor.Reading.EventDescription, sensor.Sdr.Description));
                                else
                                    response.sensors.Add(new SensorInfo(
                                        ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)sensor.Sdr.CompletionCode)));
                            }

                        }
                        // append fru info if required
                        AppendFruInfo(ref response, hwResponse, fruInfo);
                    }
                    else if (hwType == typeof(JbodStatus))
                    {
                        JbodStatus hwResponse = (JbodStatus)hardwareStatus;

                        shellResp.bladeNumber = bladeId;
                        shellResp.bladeState = FunctionValidityChecker.CheckBladeStateValidity((byte)bladeId).PowerStatus.ToString();
                        shellResp.bladeType = hwResponse.BladeType;
                        if (diskInfo)
                        {
                            if (hwResponse.CompletionCode == (byte)CompletionCode.Success)
                            {
                                response.jbodDiskInfo = new JbodDiskStatus(Contracts.CompletionCode.Success, hwResponse.DiskStatus.Channel,
                                    hwResponse.DiskStatus.DiskCount);
                                response.jbodDiskInfo.diskInfo = new List<DiskInfo>();
                                foreach (KeyValuePair<byte, Ipmi.DiskStatus> instance in hwResponse.DiskStatus.DiskState)
                                {
                                    response.jbodDiskInfo.diskInfo.Add(new DiskInfo(Contracts.CompletionCode.Success, instance.Key, instance.Value.ToString()));
                                }
                            }
                            else
                            {
                                response.jbodDiskInfo = new JbodDiskStatus(ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)hwResponse.CompletionCode));
                            }
                        }

                        if (tempInfo)
                        {
                            if (hwResponse.DiskInfo.CompletionCode == (byte)CompletionCode.Success)
                                response.jbodInfo = new JbodInfo(Contracts.CompletionCode.Success, hwResponse.DiskInfo.Unit.ToString(),
                                    hwResponse.DiskInfo.Reading);
                            else
                                response.jbodInfo = new JbodInfo(ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)hwResponse.DiskInfo.CompletionCode));
                        }
                        // append fru info if required
                        AppendFruInfo(ref response, hwResponse, fruInfo);
                    }
                    else if (hwType == typeof(UnknownBlade))
                    {
                        UnknownBlade hwResponse = (UnknownBlade)hardwareStatus;
                        // return errored response.

                        shellResp.bladeNumber = bladeId;
                        shellResp.bladeState = FunctionValidityChecker.CheckBladeStateValidity((byte)bladeId).PowerStatus.ToString();
                        shellResp.bladeType = hwResponse.BladeType;

                        // append fru info if required
                        AppendFruInfo(ref response, hwResponse, false);
                    }
                    response.bladeShell = shellResp;
                }
            }
            catch (Exception ex)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("Exception while processing Hardware Status of Blade {0}: {1}", bladeId, ex);
            }
            return response;
        }

        #endregion

        #region LED Control & Status

        /// <summary>
        /// Switch chassis Attention LED On
        /// </summary>
        /// <returns>Chassis Response packet</returns>
        public Contracts.ChassisResponse SetChassisAttentionLEDOn()
        {
            Tracer.WriteInfo("Received SetChassisAttentionLEDOn()");

            Tracer.WriteUserLog("Invoked SetChassisAttentionLEDOn()");

            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Turn On the Attention LED.
            byte status = ChassisState.AttentionLed.TurnLedOn();

            Tracer.WriteInfo("SetChassisAttentionLEDOn Return: {0}", status);

            if (status != (byte)Contracts.CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("Chassis attention LED turn on failed with Completion Code: {0:X}", status);
                response.statusDescription = String.Format("Chassis attention LED turn on failed with Completion Code: {0:X}", status);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Chassis attention LED is turned ON successfully");
            }

            return response;
        }

        /// <summary>
        /// Switch chassis Attention LED Off
        /// </summary>
        /// <returns>Chassis Response Success/Failure</returns>
        public Contracts.ChassisResponse SetChassisAttentionLEDOff()
        {
            Tracer.WriteInfo("Received SetChassisAttentionLEDOff()");

            Tracer.WriteUserLog("Invoked SetChassisAttentionLEDOff()");

            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Turn off the Chasssis Attention LED.
            byte status = ChassisState.AttentionLed.TurnLedOff();

            Tracer.WriteInfo("Return: {0}", status);

            if (status != (byte)Contracts.CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("Chassis attention LED turn on failed with Completion Code: {0:X}", status);
                response.statusDescription = String.Format("Chassis attention LED turn on failed with Completion Code: {0:X}", status);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Chassis attention LED is turned OFF successfully");
            }

            return response;
        }

        /// <summary>
        /// Switch blade Attention LED On
        /// </summary>
        /// <param name="bladeId">Blade ID (1-48)</param>
        /// <returns>Blade Response Packet with status Success/Failure.</returns>
        public BladeResponse SetBladeAttentionLEDOn(int bladeId)
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;
            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = string.Empty;
            Tracer.WriteUserLog("Invoked SetBladeAttentionLEDOn({0})", bladeId);
            Tracer.WriteInfo("Received SetBladeAttentionLEDOn({0})", bladeId);

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeAttentionLEDOn", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (SetStatusLedOn(bladeId))
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Blade attention LED is turned ON successfully for blade: " + bladeId);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                Tracer.WriteError("Blade attention LED Failed to turn ON for blade:" + bladeId);
            }
            return response;
        }

        /// <summary>
        /// Switch blade Attention LED On for all blades
        /// </summary>
        /// <returns>Blade ResponsePacket with status Success/Failure.</returns>
        public AllBladesResponse SetAllBladesAttentionLEDOn()
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            Tracer.WriteUserLog("Invoked SetAllBladesAttentionLEDOn");
            Tracer.WriteInfo("Received SetAllBladesAttentionLEDOn");

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetBladeAttentionLEDOn(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Internal command to turn on blade LED for individual blade
        /// </summary>
        /// <param name="bladeId">Blade ID (1- 48)</param>
        /// <returns>Return true/false if operation was success/failure</returns>
        private bool SetStatusLedOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetStatusLedOn({0})", bladeId);

            byte LEDHigh = (byte)ConfigLoaded.LEDHigh;

            bool status = WcsBladeFacade.Identify((byte)bladeId, LEDHigh);

            Tracer.WriteInfo("Return: {0}", status);

            if (status)
            {
                Tracer.WriteInfo("blade status LED turn on successfully for bladeId: " + bladeId);
                return status;
            }
            else
            {
                Tracer.WriteError("blade status LED turn on failed for bladeId: " + bladeId);
                return status;
            }
        }

        /// <summary>
        /// Switch blade Attention LED Off 
        /// </summary>
        /// <param name="bladeId">Blade ID (1-48)</param>
        /// <returns>Return blade response true/false if operation was success/failure</returns>
        public BladeResponse SetBladeAttentionLEDOff(int bladeId)
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;
            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            Tracer.WriteUserLog("Invoked SetBladeAttentionLEDOff(bladeId: {0})", bladeId);
            Tracer.WriteInfo("Received SetBladeAttentionLEDOff(bladeId: {0})", bladeId);

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeAttentionLEDOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (SetBladeLedOff(bladeId))
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Blade attention LED turn off successfully for blade:" + bladeId);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                Tracer.WriteError("Blade attention LED turn on failed for blade: " + bladeId);
            }
            return response;
        }

        /// <summary>
        /// Switch all blades Attention LED Off 
        /// </summary>
        /// <returns>Return true/false if operation was success/failure</returns>
        public AllBladesResponse SetAllBladesAttentionLEDOff()
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteUserLog("Invoked SetAllBladesAttentionLEDOff()");
            Tracer.WriteInfo("Received SetAllBladesAttentionLEDOff()");

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetBladeAttentionLEDOff(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Internal method to set blade LED off
        /// </summary>
        /// <param name="bladeId">Blade ID (1-48)</param>
        /// <returns>true/false if operation was success/failure</returns>
        private bool SetBladeLedOff(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeLedOff()");

            bool status = WcsBladeFacade.Identify((byte)bladeId, (byte)0);

            Tracer.WriteInfo("Return: {0}", status);

            if (status)
            {
                Tracer.WriteInfo("Blade attention LED turn off succeeded for bladeId: " + bladeId);
                return status;
            }
            else
            {
                Tracer.WriteError("Blade attention LED turn off failed for bladeId: " + bladeId);
                return status;
            }
        }

        /// <summary>
        /// Get Chassis Attention LED Status
        /// </summary>
        /// <returns>LED status response</returns>
        public LedStatusResponse GetChassisAttentionLEDStatus()
        {
            LedStatusResponse response = new LedStatusResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            Tracer.WriteInfo("Received GetChassisAttentionLEDStatus API");
            Tracer.WriteUserLog("Invoked GetChassisAttentionLEDStatus API");

            // Get Chassis Status LED.
            response = ChassisState.AttentionLed.GetLedStatus();

            if (response.completionCode != (byte)Contracts.CompletionCode.Success)
            {
                Tracer.WriteError("Chassis Attention LED status failed with Completion Code: {0:X}", response.completionCode);
            }
            else
            {
                if (response.ledState == LedState.ON)
                {
                    Tracer.WriteInfo("Chassis AttentionLED status: ON");
                }
                else if (response.ledState == LedState.OFF)
                {
                    Tracer.WriteInfo("Chassis AttentionLED status: OFF");
                }
                else
                {
                    Tracer.WriteInfo("Chassis AttentionLED status: NA");
                }
            }

            response.completionCode =
                ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)response.completionCode);
            if (response.completionCode != Contracts.CompletionCode.Success)
            {
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        #endregion

        #region AC Power Socket Control

        /// <summary>
        /// Turn on AC socket within the chassis 
        /// </summary>
        /// <param name="portNo">Port no corresponding to the AC sockets internal to the chassis like TOR switches</param>
        /// <returns>Chassis Response success/failure</returns>
        public Contracts.ChassisResponse SetACSocketPowerStateOn(uint portNo)
        {
            Tracer.WriteInfo("Received SetACSocketPowerStateOn({0})", portNo);

            Tracer.WriteUserLog("Invoked SetACSocketPowerStateOn(portNo: {0})", portNo);

            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Validate port number
            if (!ChassisManagerUtil.CheckPortValidity(portNo))
            {
                Tracer.WriteError("SetACSocketPowerStateOn: Input port number is invalid {0}", portNo);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "Port Number is invalid";
                return response;
            }

            AcSocket acSocket = ChassisState.AcPowerSockets[portNo - 1];
            byte status = acSocket.turnOnAcSocket();

            Tracer.WriteInfo("Return: {0}", status);

            if (status != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("AC Socket Turn On Failed with Completion code {0:X}", status);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("AC Socket Turn On Failed with Completion code {0:X}", status);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            return response;
        }

        /// <summary>
        /// Turn off AC socket within the chassis 
        /// </summary>
        /// <param name="portNo">Port no corresponding to the AC sockets internal to the chassis like TOR switches</param>
        /// <returns>Chassis Response success/failure.</returns>
        public Contracts.ChassisResponse SetACSocketPowerStateOff(uint portNo)
        {
            Tracer.WriteInfo("Received SetACSocketPowerStateOff({0})", portNo);

            Tracer.WriteUserLog("Invoked SetACSocketPowerStateOff(portNo: {0})", portNo);

            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Validate port number
            if (!ChassisManagerUtil.CheckPortValidity(portNo))
            {
                Tracer.WriteError("SetACSocketPowerStateOff: Input port number is invalid {0}", portNo);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "Port Number is invalid";
                return response;
            }

            AcSocket acSocket = ChassisState.AcPowerSockets[portNo - 1];
            byte status = acSocket.turnOffAcSocket();

            Tracer.WriteInfo("Return: {0}", status);

            if (status != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("AC Socket Turn Off Failed with Completion code {0:X}", status);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("AC Socket Turn Off Failed with Completion code {0:X}", status);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            return response;
        }

        /// <summary>
        /// Get power status of AC socket within the chassis 
        /// </summary>
        /// <param name="portNo">Port no corresponding to the AC sockets internal to the chassis like TOR switches</param>
        /// <returns>AC Socket power state.</returns>
        public ACSocketStateResponse GetACSocketPowerState(uint portNo)
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteInfo("Received GetACSocketPowerState({0})", portNo);

            Tracer.WriteUserLog("Invoked GetACSocketPowerState(portNo: {0})", portNo);

            ACSocketStateResponse response = new ACSocketStateResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            response.portNo = portNo;
            response.powerState = new PowerState();

            // Validate port number
            if (!ChassisManagerUtil.CheckPortValidity(portNo))
            {
                Tracer.WriteError("GetACSocketPowerState: Input port number is invalid {0}", portNo);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "Port Number is invalid";
                return response;
            }

            AcSocket acSocket = ChassisState.AcPowerSockets[portNo - 1];
            PowerState status = acSocket.getAcSocketStatus();

            Tracer.WriteInfo("Return: {0}", status);

            if (status == PowerState.NA)
            {
                Tracer.WriteError("AC Socket Get Status Failed with Completion code {0:X}", status);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("AC Socket Get Status Failed with Completion code {0:X}", status);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.powerState = status;
            }
            return response;
        }

        #endregion

        #region Power Suppy Unit Control

        /// <summary>
        /// Power On specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade Response, indicates success/failure.</returns>
        public BladeResponse SetPowerOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetPowerOn(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetPowerOn(bladeId: {0})", bladeId);

            BladeResponse response = new BladeResponse();
            byte maxbladeCount = (byte)ConfigLoaded.Population;
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Check for correct blade id
            if (ChassisManagerUtil.CheckBladeId(bladeId) == CompletionCode.InvalidBladeId)
            {
                Tracer.WriteWarning("Invalid blade Id {0}", bladeId);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            if (BladePowerCommands.PowerOn(bladeId))
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Successfully set power to ON for blade: " + bladeId);
            }
            else
            {
                Tracer.WriteError("Failed to set power to ON for blade: " + bladeId);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }
            return response;
        }

        /// <summary>
        /// Power On all blades
        /// </summary>
        /// <returns>Array of blade responses, one for each blade. Indicates success/failure.</returns>
        public AllBladesResponse SetAllPowerOn()
        {
            byte maxBladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxBladeCount];

            Tracer.WriteInfo("Invoked SetAllPowerOn()");
            Tracer.WriteUserLog("Invoked SetAllPowerOn()");

            for (int loop = 0; loop < maxBladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetPowerOn(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Power Off specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID (1-48)</param>
        /// <returns>Blade Response indicating success/failure</returns>
        public BladeResponse SetPowerOff(int bladeId)
        {
            BladeResponse response = new BladeResponse();
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteInfo("Received SetPowerOff(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetPowerOff(bladeId: {0})", bladeId);

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Check for correct blade id
            if (ChassisManagerUtil.CheckBladeId(bladeId) == CompletionCode.InvalidBladeId)
            {
                Tracer.WriteWarning("Invalid blade Id {0}", bladeId);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            // Kill active serial session if target blade matches.
            ChassisState.BladeSerialMetadata[bladeId].StopBladeSerialSession(bladeId, null, true);

            if (BladePowerCommands.PowerOff(bladeId))
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("Successfully set power to OFF for blade: " + bladeId);
            }
            else
            {
                Tracer.WriteError("Failed to set power to OFF for blade: " + bladeId);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }
            return response;
        }

        /// <summary>
        /// Power OFF all blades
        /// </summary>
        /// <returns>Array of Blade Responses indicating success/failure</returns>
        public AllBladesResponse SetAllPowerOff()
        {
            byte maxBladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxBladeCount];

            Tracer.WriteUserLog("Invoked SetAllPowerOff()");

            for (int loop = 0; loop < maxBladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetPowerOff(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Set Soft Blade power ON
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public BladeResponse SetBladeOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeOn(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeOn(bladeId: {0})", bladeId);

            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeOn", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (BladePowerCommands.BladeOn(bladeId))
            {
                Tracer.WriteInfo("SetBladeOn({0}): Blade soft power set to ON", bladeId);
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                Tracer.WriteError("SetBladeOn({0}): Failed to set Blade soft power ON", bladeId);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Set soft blade power limit ON for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesOn()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteInfo("Received SetAllBladesOn()");
            Tracer.WriteUserLog("Invoked SetAllBladesOn()");
            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladeResponseCollection.Add(SetBladeOn(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("SetAllBladesOn failed with exception" + ex);
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = Contracts.CompletionCode.Failure + ": " + ex.Message;
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Set blade soft power OFF for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public BladeResponse SetBladeOff(int bladeId)
        {
            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;

            Tracer.WriteInfo("Received SetBladeOff(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeOff(bladeId: {0})", bladeId);

            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (BladePowerCommands.BladeOff(bladeId))
            {
                Tracer.WriteInfo("SetBladeOff({0}): Blade soft power set to OFF", bladeId);
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                Tracer.WriteError("SetBladeOff({0}): Failed to set Blade soft power to OFF", bladeId);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Set blade soft power OFF for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesOff()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteInfo("Received SetAllBladesOff()");
            Tracer.WriteUserLog("Invoked SetAllBladesOff()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladeResponseCollection.Add(SetBladeOff(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesOff failed with exception" + ex);
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Power cycle specified blade
        /// </summary>
        /// <param name="bladeId">if bladeId id -1, then bladeId not provided</param>
        /// <param name="offTime">time for which the blades will be powered off in seconds</param>
        /// <returns>Blade response indicating if blade operation was success/failure</returns>
        public BladeResponse SetBladeActivePowerCycle(int bladeId, uint offTime)
        {
            Tracer.WriteUserLog("Invoked SetBladeActivePowerCycle(bladeId: {0}, offTime: {1})", bladeId, offTime);

            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeActivePowerCycle", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Check that the power off time is valid for a byte value since the IPMI command takes a byte value
            if (offTime > Byte.MaxValue)
            {
                Tracer.WriteWarning("SetBladeActivePowerCycle failed, Invalid power off time: {0}", offTime);

                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            if (BladePowerCommands.PowerCycle(bladeId, offTime))
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Power cycle all blades
        /// </summary>
        /// <param name="offTime">time for which the blades will be powered off in seconds</param>
        /// <returns>Collection of Blade responses indicating if blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesActivePowerCycle(uint offTime)
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            Tracer.WriteUserLog(" Invoked SetAllBladesActivePowerCycle(offTime: {0})", offTime);

            // Check that the power off time is valid for a byte value since the IPMI command takes a byte value
            if (offTime > Byte.MaxValue)
            {
                Tracer.WriteWarning("SetAllBladesActivePowerCycle failed, Invalid power off time: {0}", offTime);

                responses.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                responses.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return responses;
            }

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;

                responses.bladeResponseCollection.Add(SetBladeActivePowerCycle(bladeId, offTime));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Update firmware on specified PSU
        /// </summary>
        /// <param name="psuId">The psu identifier.</param>
        /// <param name="fwFilepath">The firmware image file path. The file must be in hex format.</param>
        /// <param name="primaryImage">
        /// True:  Firmware image is for primary controller. 
        /// False: Firmware image is for secondary controller. 
        /// <returns>
        /// Response indicating if firmware upgrade was started successfully.
        /// </returns>
        public Contracts.ChassisResponse UpdatePSUFirmware(int psuId, string fwFilepath, bool primaryImage)
        {
            Tracer.WriteUserLog("Invoked UpdatePSUFirmware(psuId: {0}, fwFilepath: {1}, primaryImage: {2})", psuId, fwFilepath, primaryImage);

            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Check PSU ID
            if (ChassisManagerUtil.CheckPsuId(psuId) != CompletionCode.Success)
            {
                Tracer.WriteWarning("UpdatePSUFirmware failed. Invalid PSU Id: {0}", psuId);

                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            // Firmware upgrade only valid for Emerson PSU
            if (!(ChassisState.Psu[psuId - 1] is EmersonPsu))
            {
                Tracer.WriteWarning("UpdatePSUFirmware failed. PSU {0} is not an Emerson PSU.", psuId);

                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = "UpdatePSUFirmware() only supported on Emerson PSU.";
                return response;
            }

            // If firmware update is already in progress, return error
            if (ChassisState.PsuFwUpdateInProgress[psuId - 1])
            {
                response.completionCode = Contracts.CompletionCode.PSUFirmwareUpdateInProgress;
                response.statusDescription = "PSU firmware update in progress";
                return response;
            }

            try
            {
                Tracer.WriteInfo("UpdatePSUFirmware: Updating PSU: {0} with file {1}. primaryImage: {2}", psuId, fwFilepath, primaryImage);
                
                // Check that firmware image file can be opened for reading.
                // If file does not exist or cannot be read, the method will throw an exception
                File.OpenRead(fwFilepath);
                
                // Build firmware update data
                EmersonPsu.FirmwareUpdateInfo fwUpdateInfo = new EmersonPsu.FirmwareUpdateInfo();
                fwUpdateInfo.fwFilepath = fwFilepath;
                fwUpdateInfo.primaryImage = primaryImage;

                // Execute firmware update
                EmersonPsu emersonPsu = (EmersonPsu)ChassisState.Psu[(psuId - 1)];
                CompletionCode completionCode = emersonPsu.ExecuteFirmwareUpdate(fwUpdateInfo);
                if (completionCode == CompletionCode.Success)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = response.completionCode.ToString();
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteWarning("UpdatePSUFirmware failed with exception: " + ex.Message);

                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString() + ": " + ex.Message;
                return response;
            }

            return response;
        }

        /// <summary>
        /// Gets the PSU firmware status
        /// </summary>
        /// <param name="psuId">The psu identifier.</param>
        /// <returns>
        /// PSU FW status
        /// </returns>
        public Contracts.PsuFirmwareStatus GetPSUFirmwareStatus(int psuId)
        {
            Tracer.WriteUserLog("Invoked GetPSUFirmwareStatus(psuId: {0})", psuId);

            Contracts.PsuFirmwareStatus response = new Contracts.PsuFirmwareStatus();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Assign default values for response
            response.fwRevision = String.Empty;
            response.fwUpdateStatus = String.Empty;
            response.fwUpdateStage = String.Empty;

            // Check PSU ID
            if (ChassisManagerUtil.CheckPsuId(psuId) != CompletionCode.Success)
            {
                Tracer.WriteWarning("GetPSUFirmwareStatus failed. Invalid PSU Id: {0}", psuId);

                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            // Firmware status only valid for Emerson PSU
            if (!(ChassisState.Psu[psuId - 1] is EmersonPsu))
            {
                Tracer.WriteWarning("GetPSUFirmwareStatus failed. PSU {0} is not an Emerson PSU.", psuId);

                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = "GetPSUFirmwareStatus() only supported on Emerson PSU.";
                return response;
            }

            try
            {
                EmersonPsu emersonPsu = (EmersonPsu)ChassisState.Psu[(psuId - 1)];

                // Get firmware status
                response.fwUpdateStatus = emersonPsu.FwUpdateStatus.ToString();
                response.fwUpdateStage = emersonPsu.FwUpdateStage.ToString();
                response.completionCode = Contracts.CompletionCode.Success;

                // Only read firmware revision if update is not in progress. 
                // Otherwise the revision is not valid
                if (emersonPsu.FwUpdateStatus != EmersonPsu.FwUpdateStatusEnum.InProgress)
                {
                    EmersonPsu.FirmwareRevisionPacket returnPacket = emersonPsu.GetFirmwareRevision();
                    if (returnPacket.CompletionCode == CompletionCode.Success)
                    {
                        response.fwRevision = returnPacket.FwRevision;
                        response.completionCode = Contracts.CompletionCode.Success;
                    }
                    else
                    {
                        string msg = string.Format("GetPSUFirmwareStatus: GetFirmwareRevision() failed with completion code: {0}",
                            returnPacket.CompletionCode);
                        Tracer.WriteError(msg);

                        response.completionCode =
                            ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)returnPacket.CompletionCode);
                        response.statusDescription = msg;
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteWarning("GetPSUFirmwareStatus failed with exception: " + ex.Message);

                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString() + ": " + ex.Message;
                return response;
            }

            return response;
        }
        #endregion

        #region Blade Power State

        /// <summary>
        /// Get power state of all blades
        /// </summary>
        /// <returns>Collection of Blade State response packets</returns>
        public GetAllPowerStateResponse GetAllPowerState()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            GetAllPowerStateResponse responses = new GetAllPowerStateResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.powerStateResponseCollection = new List<PowerStateResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];
            uint bladeCount = MaxbladeCount;

            Tracer.WriteUserLog("Invoked GetAllPowerState()");
            Tracer.WriteInfo("Invoked GetAllPowerState()");

            for (int loop = 0; loop < bladeCount; loop++)
            {
                responses.powerStateResponseCollection.Add(GetPowerState(loop + 1));
                Tracer.WriteInfo("Blade power state: ", responses.powerStateResponseCollection[loop].powerState);

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.powerStateResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Get power state for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade active power state response</returns>
        public PowerStateResponse GetPowerState(int bladeId)
        {
            Tracer.WriteInfo("Received GetPowerState({0})", bladeId);
            Tracer.WriteUserLog("Invoked GetPowerState(bladeid: {0})", bladeId);

            PowerStateResponse responsePowerState = new PowerStateResponse();
            responsePowerState.completionCode = Contracts.CompletionCode.Unknown;
            responsePowerState.statusDescription = String.Empty;
            responsePowerState.bladeNumber = bladeId;
            responsePowerState.powerState = Contracts.PowerState.NA;

            // Check for blade id
            if (ChassisManagerUtil.CheckBladeId(bladeId) == CompletionCode.InvalidBladeId)
            {
                Tracer.WriteWarning("Invalid blade Id {0}", bladeId);
                responsePowerState.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                responsePowerState.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return responsePowerState;
            }

            // Get Power State
            BladePowerStatePacket response = ChassisState.BladePower[bladeId - 1].GetBladePowerState();

            if (response.CompletionCode != CompletionCode.Success)
            {
                Tracer.WriteError("GetPowerState: Blade {0} Power Enable state read failed (Completion Code: {1:X})", bladeId, response.CompletionCode);
                responsePowerState.powerState = Contracts.PowerState.NA;
                responsePowerState.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)response.CompletionCode);
                responsePowerState.statusDescription = responsePowerState.completionCode.ToString();
            }
            else
            {
                responsePowerState.completionCode = Contracts.CompletionCode.Success;

                if (response.BladePowerState == (byte)Contracts.PowerState.ON)
                {
                    responsePowerState.powerState = Contracts.PowerState.ON;
                    responsePowerState.Decompression = response.DecompressionTime;
                    responsePowerState.statusDescription = string.Format("Blade Power is On, firmware decompressed");

                    if (response.DecompressionTime > 0)
                    {
                        responsePowerState.powerState = Contracts.PowerState.OnFwDecompress;
                        responsePowerState.Decompression = response.DecompressionTime;
                        responsePowerState.statusDescription = string.Format("Blade On.  Firmware Decompression Time Remaining: {0}", response.DecompressionTime);
                    }

                    Tracer.WriteInfo("GetPowerState: Blade is receiving AC Outlet power");
                }
                else if (response.BladePowerState == (byte)Contracts.PowerState.OFF)
                {
                    responsePowerState.powerState = Contracts.PowerState.OFF;
                    responsePowerState.Decompression = response.DecompressionTime;
                    responsePowerState.statusDescription = string.Format("Blade power is Off");
                    Tracer.WriteInfo("GetPowerState: Blade is NOT receiving AC Outlet power");
                }
                else
                {
                    responsePowerState.powerState = Contracts.PowerState.NA;
                    responsePowerState.statusDescription = string.Format("Blade Power is Unknown");
                    Tracer.WriteWarning("GetPowerState: Unknown power state");
                }
            }

            return responsePowerState;
        }

        /// <summary>
        /// Get Blade soft power state
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade state response class object</returns>
        public BladeStateResponse GetBladeState(int bladeId)
        {
            Tracer.WriteInfo("Received GetBladeState({0})", bladeId);

            Tracer.WriteUserLog("Invoked GetBladeState(bladeid: {0})", bladeId);

            BladeStateResponse stateResponse = new BladeStateResponse();
            stateResponse.bladeNumber = bladeId;
            stateResponse.bladeState = Contracts.PowerState.NA;
            stateResponse.completionCode = Contracts.CompletionCode.Unknown;
            stateResponse.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeState", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                stateResponse.completionCode = varResponse.completionCode;
                stateResponse.statusDescription = varResponse.statusDescription;
                return stateResponse;
            }

            // Check to see if the blade enable itself is OFF - then the BMC power state does not matter
            BladePowerStatePacket response = ChassisState.BladePower[bladeId - 1].GetBladePowerState();

            if (response.CompletionCode != CompletionCode.Success)
            {
                // No return here, because we still want to return a BMC state on the fall through,
                // if Blade enable read fails for whatever reason
                Tracer.WriteWarning("GetBladeState: Blade {0} Power Enable state read failed (Completion Code: {1:X})", bladeId, response.CompletionCode);
                stateResponse.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)response.CompletionCode);
                stateResponse.statusDescription = stateResponse.completionCode.ToString();
            }
            else
            {
                // Only if the blade enable is OFF, we return that status, for anything else we have to read BMC status
                if (response.PowerStatus == Contracts.PowerState.OFF)
                {
                    // Since we do not know if a blade is present in that slot or not, we return NA as power state
                    stateResponse.bladeState = Contracts.PowerState.OFF;

                    stateResponse.completionCode = Contracts.CompletionCode.Success;

                    return stateResponse;
                }
                else if (response.PowerStatus == Contracts.PowerState.ON)
                {                                      
                    if (response.DecompressionTime > 0)
                    {
                        stateResponse.completionCode = Contracts.CompletionCode.Success;
                        stateResponse.bladeState = PowerState.OnFwDecompress;
                        stateResponse.statusDescription = string.Format("Blade On.  Firmware Decompression Time Remaining: {0}", response.DecompressionTime);
                        return stateResponse;
                    }
                }
            }

            Ipmi.SystemStatus powerState = WcsBladeFacade.GetChassisState((byte)bladeId);

            Tracer.WriteInfo("GetBladeState {0} Return: {1}, Blade State: {2}", bladeId, powerState.CompletionCode, powerState.PowerState.ToString());

            if (powerState.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("GetBladeState Failed with Completion code {0:X}", powerState.CompletionCode);
                stateResponse.bladeState = Contracts.PowerState.NA;
                stateResponse.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)powerState.CompletionCode);
                stateResponse.statusDescription = stateResponse.completionCode.ToString();
            }
            else
            {
                stateResponse.completionCode = Contracts.CompletionCode.Success;
                if (powerState.PowerState == Ipmi.IpmiPowerState.On)
                {
                    stateResponse.bladeState = Contracts.PowerState.ON;
                }
                else
                {
                    stateResponse.bladeState = Contracts.PowerState.OFF;
                }
            }
            return stateResponse;
        }

        /// <summary>
        /// Get all blades soft power state
        /// </summary>
        /// <returns>Blade state response class object</returns>
        public GetAllBladesStateResponse GetAllBladesState()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            GetAllBladesStateResponse responses = new GetAllBladesStateResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeStateResponseCollection = new List<BladeStateResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];
            uint bladeCount = MaxbladeCount;

            Tracer.WriteUserLog("Invoked GetAllBladesState()");
            Tracer.WriteInfo("Invoked GetAllBladesState()");

            for (int loop = 0; loop < bladeCount; loop++)
            {
                responses.bladeStateResponseCollection.Add(GetBladeState(loop + 1));
                Tracer.WriteInfo("Blade state: ", responses.bladeStateResponseCollection[loop].bladeState);

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeStateResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Datasafe Set blade soft power OFF for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public DatasafeBladeResponse SetBladeDatasafeOff(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDatasafeOff(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDatasafeOff(bladeId: {0})", bladeId);

            DatasafeBladeResponse response = new DatasafeBladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("SetBladeDatasafeOff: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "Datasafe commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDatasafeOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Check blade state. Only process command if blade is on
            BladeStateResponse bladeStateResponse = GetBladeState(bladeId);
            if (bladeStateResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = bladeStateResponse.completionCode;
                response.statusDescription = bladeStateResponse.statusDescription;
                return response;
            }
            else
            {
                // If blade is already soft off, return success. Hard power off is checked in ValidateRequest().
                if (bladeStateResponse.bladeState == PowerState.OFF)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    return response;
                }
            }

            // Call ProcessDatasafeAction() so that the current action (command) can be added to the set of pending
            // actions and the processing loop for the pending actions appropriately kicked-off
            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.BladeOff);
            if (datasafeResponse == null)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return response;
            }

            if (datasafeResponse.isBackupPending)
            {
                response.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                response.RemainingDataSafeDurationInSecs = 0;
            }
            
            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandDelayed)
            {
                response.completionCode = Contracts.CompletionCode.RequestDelayedDueToPendingDatasafeOperation;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandFailed)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Datasafe Set blade soft power OFF for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public DatasafeAllBladesResponse SetAllBladesDatasafeOff()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesResponse responses = new DatasafeAllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteInfo("Received SetAllBladesDatasafeOff()");
            Tracer.WriteUserLog("Invoked SetAllBladesDatasafeOff()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.datasafeBladeResponseCollection.Add(SetBladeDatasafeOff(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.datasafeBladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesDatasafeOff failed with exception" + ex);
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Datasafe Set blade soft power ON for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public DatasafeBladeResponse SetBladeDatasafeOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDatasafeOn(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDatasafeOn(bladeId: {0})", bladeId);

            DatasafeBladeResponse response = new DatasafeBladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("SetBladeDatasafeOn: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "Datasafe commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDatasafeOn", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Call ProcessDatasafeAction() so that the current action (command) can be added to the set of pending
            // actions and the processing loop for the pending actions appropriately kicked-off
            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.BladeOn);
            if (datasafeResponse == null)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return response;
            }

            if (datasafeResponse.isBackupPending)
            {
                response.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                response.RemainingDataSafeDurationInSecs = 0;
            }
            
            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandDelayed)
            {
                response.completionCode = Contracts.CompletionCode.RequestDelayedDueToPendingDatasafeOperation;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandFailed)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Datasafe Set blade soft power ON for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public DatasafeAllBladesResponse SetAllBladesDatasafeOn()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesResponse responses = new DatasafeAllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteInfo("Received SetAllBladesDatasafeOn()");
            Tracer.WriteUserLog("Invoked SetAllBladesDatasafeOn()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.datasafeBladeResponseCollection.Add(SetBladeDatasafeOn(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.datasafeBladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesDatasafeOn failed with exception" + ex);
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Datasafe Power Off specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade Response, indicates success/failure.</returns>
        public DatasafeBladeResponse SetBladeDatasafePowerOff(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDatasafePowerOff(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDatasafePowerOff(bladeId: {0})", bladeId);

            DatasafeBladeResponse response = new DatasafeBladeResponse();
            byte maxbladeCount = (byte)ConfigLoaded.Population;
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("SetBladeDatasafePowerOff: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "Datasafe commands are not enabled for this blade";
                return response;
            }

            // Datasafe APIs only valid for compute blades. ValidateRequest() also checks if blade is already hard powered off.
            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDatasafePowerOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Check blade state. Only process command if blade is on
            BladeStateResponse bladeStateResponse = GetBladeState(bladeId);
            if (bladeStateResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = bladeStateResponse.completionCode;
                response.statusDescription = bladeStateResponse.statusDescription;
                return response;
            }
            else
            {
                // If blade is already soft off, do not need any Nvdimm processing. 
                // Set hard power off.
                if (bladeStateResponse.bladeState == PowerState.OFF)
                {
                    // Set hard power off.
                    if (BladePowerCommands.PowerOff(bladeId))
                    {
                        response.completionCode = Contracts.CompletionCode.Success;
                        Tracer.WriteInfo("Successfully set power to OFF for blade: " + bladeId);
                    }
                    else
                    {
                        Tracer.WriteError("Failed to set power to OFF for blade: " + bladeId);
                        response.completionCode = Contracts.CompletionCode.Failure;
                        response.statusDescription = response.completionCode.ToString();
                    }
                    return response;
                }
            }

            // Call ProcessDatasafeAction() so that the current action (command) can be added to the set of pending
            // actions and the processing loop for the pending actions appropriately kicked-off
            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.PowerOff);
            if (datasafeResponse == null)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return response;
            }

            if (datasafeResponse.isBackupPending)
            {
                response.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                response.RemainingDataSafeDurationInSecs = 0;
            }

            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandDelayed)
            {
                response.completionCode = Contracts.CompletionCode.RequestDelayedDueToPendingDatasafeOperation;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandFailed)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Datasafe Power Off all blades
        /// </summary>
        /// <returns>Array of blade responses, one for each blade. Indicates success/failure.</returns>
        public DatasafeAllBladesResponse SetAllBladesDatasafePowerOff()
        {
            byte maxBladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesResponse responses = new DatasafeAllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxBladeCount];

            Tracer.WriteInfo("Invoked SetAllBladesDatasafePowerOff()");
            Tracer.WriteUserLog("Invoked SetAllBladesDatasafePowerOff()");

            for (int loop = 0; loop < maxBladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.datasafeBladeResponseCollection.Add(SetBladeDatasafePowerOff(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.datasafeBladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Datasafe Power On specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade Response, indicates success/failure.</returns>
        public DatasafeBladeResponse SetBladeDatasafePowerOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDatasafePowerOn(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDatasafePowerOn(bladeId: {0})", bladeId);

            DatasafeBladeResponse response = new DatasafeBladeResponse();
            byte maxbladeCount = (byte)ConfigLoaded.Population;
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("SetBladeDatasafePowerOn: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "Datasafe commands are not enabled for this blade";
                return response;
            }

            // Check for correct blade id
            if (ChassisManagerUtil.CheckBladeId(bladeId) == CompletionCode.InvalidBladeId)
            {
                Tracer.WriteWarning("SetBladeDatasafePowerOn. Invalid blade Id {0}", bladeId);
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            // Datasafe APIs only valid for compute blades
            if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
            {
                Tracer.WriteWarning("SetBladeDatasafePowerOn({0}) failed, Invalid blade Type", bladeId);

                response.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                response.statusDescription = Contracts.CompletionCode.CommandNotValidForBlade.ToString();
                return response;
            }

            // Call ProcessDatasafeAction() so that the current action (command) can be added to the set of pending
            // actions and the processing loop for the pending actions appropriately kicked-off
            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.PowerOn);
            if (datasafeResponse == null)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return response;
            }

            if (datasafeResponse.isBackupPending)
            {
                response.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                response.RemainingDataSafeDurationInSecs = 0;
            }

            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandDelayed)
            {
                response.completionCode = Contracts.CompletionCode.RequestDelayedDueToPendingDatasafeOperation;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandFailed)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Datasafe Power On all blades
        /// </summary>
        /// <returns>Array of blade responses, one for each blade. Indicates success/failure.</returns>
        public DatasafeAllBladesResponse SetAllBladesDatasafePowerOn()
        {
            byte maxBladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesResponse responses = new DatasafeAllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxBladeCount];

            Tracer.WriteInfo("Invoked SetAllBladesDatasafePowerOn()");
            Tracer.WriteUserLog("Invoked SetAllBladesDatasafePowerOn()");

            for (int loop = 0; loop < maxBladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.datasafeBladeResponseCollection.Add(SetBladeDatasafePowerOn(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.datasafeBladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }


        /// <summary>
        /// Datasafe Power cycle specified blade
        /// </summary>
        /// <param name="bladeId">if bladeId id -1, then bladeId not provided</param>
        /// <returns>Blade response indicating if blade operation was success/failure</returns>
        public DatasafeBladeResponse SetBladeDatasafeActivePowerCycle(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDatasafeActivePowerCycle(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDatasafeActivePowerCycle(bladeId: {0})", bladeId);

            DatasafeBladeResponse response = new DatasafeBladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("SetBladeDatasafeActivePowerCycle: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "Datasafe commands are not enabled for this blade";
                return response;
            }

            DatasafeActions action = DatasafeActions.PowerCycle;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDatasafeActivePowerCycle", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Check blade state. Only process command if blade is on
            BladeStateResponse bladeStateResponse = GetBladeState(bladeId);
            if (bladeStateResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = bladeStateResponse.completionCode;
                response.statusDescription = bladeStateResponse.statusDescription;
                return response;
            }
            else
            {
                // If blade is already soft off, change action to blade on so that NvdimmSupport class 
                // does not trigger ADR before executing a power cycle.
                // Hard power off is checked in ValidateRequest().
                if (bladeStateResponse.bladeState == PowerState.OFF)
                {
                    action = DatasafeActions.BladeOn;
                }
            }

            // Call ProcessDatasafeAction() so that the current action (command) can be added to the set of pending
            // actions and the processing loop for the pending actions appropriately kicked-off
            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, action);
            if (datasafeResponse == null)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return response;
            }

            if (datasafeResponse.isBackupPending)
            {
                response.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                response.RemainingDataSafeDurationInSecs = 0;
            }

            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandDelayed)
            {
                response.completionCode = Contracts.CompletionCode.RequestDelayedDueToPendingDatasafeOperation;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandFailed)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Datasafe Power cycle all blades
        /// </summary>
        /// <returns>Collection of Blade responses indicating if blade operation was success/failure</returns>
        public DatasafeAllBladesResponse SetAllBladesDatasafeActivePowerCycle()
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesResponse responses = new DatasafeAllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladeResponseCollection = new List<DatasafeBladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            Tracer.WriteInfo(" Received SetAllBladesDatasafeActivePowerCycle");
            Tracer.WriteUserLog(" Invoked SetAllBladesDatasafeActivePowerCycle");

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;

                responses.datasafeBladeResponseCollection.Add(SetBladeDatasafeActivePowerCycle(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.datasafeBladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Get Datasafe Blade power state
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade state response class object</returns>
        public DatasafeBladePowerStateResponse GetBladeDatasafePowerState(int bladeId)
        {
            Tracer.WriteInfo("Received GetBladeDatasafePowerState({0})", bladeId);

            Tracer.WriteUserLog("Invoked GetBladeDatasafePowerState(bladeid: {0})", bladeId);

            DatasafeBladePowerStateResponse stateResponse = new DatasafeBladePowerStateResponse();
            stateResponse.bladeNumber = bladeId;
            stateResponse.isDatasafeBackupInProgress = false;
            stateResponse.bladePowerState = Contracts.PowerState.NA;
            stateResponse.completionCode = Contracts.CompletionCode.Unknown;
            stateResponse.statusDescription = String.Empty;

            if (!ConfigLoaded.DatasafeOperationsEnabled)
            {
                Tracer.WriteInfo("GetBladeDatasafePowerState: User requested API not enabled in app.config");
                stateResponse.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                stateResponse.statusDescription = "Datasafe commands are not enabled for this blade";
                return stateResponse;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeDatasafePowerState", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                stateResponse.completionCode = varResponse.completionCode;
                stateResponse.statusDescription = varResponse.statusDescription;
                return stateResponse;
            }

            // Check to see if the blade enable itself is OFF - then the BMC power state does not matter
            BladePowerStatePacket response = ChassisState.BladePower[bladeId - 1].GetBladePowerState();

            if (response.CompletionCode != CompletionCode.Success)
            {
                // No return here, because we still want to return a BMC state on the fall through,
                // if Blade enable read fails for whatever reason
                Tracer.WriteWarning("GetBladeDatasafePowerState: Blade {0} Power Enable state read failed (Completion Code: {1:X})", 
                    bladeId, response.CompletionCode);
                stateResponse.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)response.CompletionCode);
                stateResponse.statusDescription = stateResponse.completionCode.ToString();
            }
            else
            {
                // Only if the blade enable is OFF, we return that status, for anything else we have to read BMC status
                if (response.BladePowerState == (byte)Contracts.PowerState.OFF)
                {
                    // Since we do not know if a blade is present in that slot or not, we return NA as power state
                    // TODO: This was supposed to return OFF status, and not NA
                    stateResponse.bladePowerState = Contracts.PowerState.NA;

                    stateResponse.completionCode = Contracts.CompletionCode.Success;

                    return stateResponse;
                }
            }

            Ipmi.SystemStatus powerState = WcsBladeFacade.GetChassisState((byte)bladeId);
            Tracer.WriteInfo("Return: {0}, Blade State: {1}", powerState.CompletionCode, powerState.PowerState.ToString());

            if (powerState.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("GetBladeState Failed with Completion code {0:X}", powerState.CompletionCode);
                stateResponse.bladePowerState = Contracts.PowerState.NA;
                stateResponse.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)powerState.CompletionCode);
                stateResponse.statusDescription = stateResponse.completionCode.ToString();
                return stateResponse;
            }
            else // if success
            {
                stateResponse.completionCode = Contracts.CompletionCode.Success;
                if (powerState.PowerState == Ipmi.IpmiPowerState.On)
                {
                    stateResponse.bladePowerState = Contracts.PowerState.ON;
                }
                else
                {
                    stateResponse.bladePowerState = Contracts.PowerState.OFF;
                }
            }

            DatasafeBladeStatus datasafeResponse = DatasafeOperationSupport.ProcessDatasafeAction(bladeId, DatasafeActions.DoNothing);

            if (datasafeResponse == null)
            {
                stateResponse.completionCode = Contracts.CompletionCode.Failure;
                stateResponse.statusDescription = Contracts.CompletionCode.Failure.ToString();
                return stateResponse;
            }

            stateResponse.isDatasafeBackupInProgress = datasafeResponse.isBackupPending;
            if (datasafeResponse.isBackupPending)
            {
                stateResponse.RemainingDataSafeDurationInSecs = datasafeResponse.remainingBackupDuration;
            }
            else
            {
                stateResponse.RemainingDataSafeDurationInSecs = 0;
            }
            if (datasafeResponse.status == DatasafeCommandsReturnStatus.CommandExecuted)
            {
                stateResponse.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                stateResponse.completionCode = Contracts.CompletionCode.Failure;
                stateResponse.statusDescription = stateResponse.completionCode.ToString();
            }

            return stateResponse;
        }

        /// <summary>
        /// Get all blades datasafe power state
        /// </summary>
        /// <returns>Blade state response class object</returns>
        public DatasafeAllBladesPowerStateResponse GetAllBladesDatasafePowerState()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            DatasafeAllBladesPowerStateResponse responses = new DatasafeAllBladesPowerStateResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.datasafeBladePowerStateResponseCollection = new List<DatasafeBladePowerStateResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];
            uint bladeCount = MaxbladeCount;

            Tracer.WriteUserLog("Invoked GetAllBladesDatasafePowerState()");
            Tracer.WriteInfo("Invoked GetAllBladesDatasafePowerState()");

            for (int loop = 0; loop < bladeCount; loop++)
            {
                responses.datasafeBladePowerStateResponseCollection.Add(GetBladeDatasafePowerState(loop + 1));
                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.datasafeBladePowerStateResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        #endregion

        #region Default Blade Power State

        /// <summary>
        /// Sets the default blade board power state ON
        /// Indicates whether the system should be powered on or kept shutdown after power comes back to the system
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade Response success/failure.</returns>
        public BladeResponse SetBladeDefaultPowerStateOn(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDefaultPowerStateOn({0})", bladeId);

            Tracer.WriteUserLog("Invoked SetBladeDefaultPowerStateOn({0})", bladeId);

            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDefaultPowerStateOn", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            Ipmi.PowerRestoreOption powerState = Ipmi.PowerRestoreOption.AlwaysPowerUp;

            Tracer.WriteInfo("SetBladeDefaultPowerStateOn for Blade: ", bladeId);

            Ipmi.PowerRestorePolicy powerPolicy = WcsBladeFacade.SetPowerRestorePolicy((byte)bladeId, powerState);

            if (powerPolicy.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("SetBladeDefaultPowerStateOn failed with completion code: {0:X}", powerPolicy.CompletionCode);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Tracer.WriteInfo("SetBladeDefaultPowerStateOn succeeded for Blade: ", bladeId);
            }
            return response;
        }

        /// <summary>
        /// Sets the default blade board power state ON for all blades
        /// Indicates whether the system should be powered on or kept shutdown after power comes back to the system
        /// </summary>
        /// <returns>rray of blade responses, one for each blade. Indicates success/failure.</returns>
        public AllBladesResponse SetAllBladesDefaultPowerStateOn()
        {
            Tracer.WriteInfo("Received SetAllBladesDefaultPowerStateOn");
            Tracer.WriteUserLog("Invoked SetAllBladesDefaultPowerStateOn()");

            byte maxbladeCount = (byte)ConfigLoaded.Population;
            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetBladeDefaultPowerStateOn(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Sets the default blade board power state Off
        /// Indicates whether the system should be powered on or kept shutdown after power comes back to the system
        /// </summary>
        /// <param name="bladeId">Blade ID ( 1-24)</param>
        /// <returns>Blade Response success/failure.</returns>
        public BladeResponse SetBladeDefaultPowerStateOff(int bladeId)
        {
            Tracer.WriteInfo("Received SetBladeDefaultPowerStateOff(BladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked SetBladeDefaultPowerStateOff(BladeId: {0})", bladeId);

            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeDefaultPowerStateOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            Ipmi.PowerRestoreOption powerState = Ipmi.PowerRestoreOption.StayOff;
            Tracer.WriteInfo("SetBladeDefaultPowerStateOff for Blade: ", bladeId);
            Ipmi.PowerRestorePolicy powerPolicy = WcsBladeFacade.SetPowerRestorePolicy((byte)bladeId, powerState);

            if (powerPolicy.CompletionCode == (byte)CompletionCode.Success)
            {
                Tracer.WriteInfo("SetBladeDefaultPowerStateOff succededed for blade: ", bladeId);
                response.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                Tracer.WriteError("SetBladeDefaultPowerStateOff failed with completion code: {0:X}, for blade: {1}", powerPolicy.CompletionCode, bladeId);
            }
            return response;
        }

        /// <summary>
        /// Sets the default blade board power state Off for all blades
        /// Indicates whether the system should be powered on or kept shutdown after power comes back to the system
        /// </summary>
        /// <returns>Array of blade responses, one for each blade. Indicates success/failure.</returns>
        public AllBladesResponse SetAllBladesDefaultPowerStateOff()
        {
            Tracer.WriteInfo("Received SetAllBladesDefaultPowerStateOff");
            Tracer.WriteUserLog("Invoked SetAllBladesDefaultPowerStateOff()");
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeResponseCollection.Add(SetBladeDefaultPowerStateOff(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Returns the default blade board power state
        /// </summary>
        /// <param name="bladeId">Blade ID (1-24)</param>
        /// <returns>Blade State Response packet</returns>
        public BladeStateResponse GetBladeDefaultPowerState(int bladeId)
        {
            Tracer.WriteInfo("Received GetBladeDefaultPowerState({0})", bladeId);

            Tracer.WriteUserLog("Invoked GetBladeDefaultPowerState({0})", bladeId);

            BladeStateResponse response = new BladeStateResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeDefaultPowerState", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            Ipmi.SystemStatus powerPolicy = WcsBladeFacade.GetChassisState((byte)bladeId);

            if (powerPolicy.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("GetBladeDefaultPowerState failed with completion code:"
                    + Ipmi.IpmiSharedFunc.ByteToHexString((byte)powerPolicy.CompletionCode));
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                response.bladeState = PowerState.NA;
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                Ipmi.PowerRestoreOption currentSetting = powerPolicy.PowerOnPolicy;

                switch (currentSetting)
                {
                    case Microsoft.GFS.WCS.ChassisManager.Ipmi.PowerRestoreOption.StayOff:
                        response.bladeState = PowerState.OFF;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeId: {0}) return is OFF", bladeId);
                        break;
                    case Microsoft.GFS.WCS.ChassisManager.Ipmi.PowerRestoreOption.AlwaysPowerUp:
                        response.bladeState = PowerState.ON;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeID: {0}) return is ON", bladeId);
                        break;
                    case Microsoft.GFS.WCS.ChassisManager.Ipmi.PowerRestoreOption.GetCurrentPolicy:
                        response.bladeState = PowerState.NA;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeID: {0}) return is curr policy", bladeId);
                        break;
                    case Microsoft.GFS.WCS.ChassisManager.Ipmi.PowerRestoreOption.PreviousState:
                        response.bladeState = PowerState.NA;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeID: {0}) return is prev state", bladeId);
                        break;
                    case Microsoft.GFS.WCS.ChassisManager.Ipmi.PowerRestoreOption.Unknown:
                        response.bladeState = PowerState.NA;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeID: {0}) return is unknown", bladeId);
                        break;
                    default:
                        response.bladeState = PowerState.NA;
                        Tracer.WriteInfo("GetBladeDefaultPowerState(BladeID: {0}) return is NA", bladeId);
                        break;
                }
            }

            return response;
        }

        /// <summary>
        /// Returns the default blade board power state (On or Off) for all blades
        /// </summary>
        /// <returns>Array of blade state response, one for each blade.</returns>
        public GetAllBladesStateResponse GetAllBladesDefaultPowerState()
        {
            byte maxbladeCount = (byte)ConfigLoaded.Population;

            Tracer.WriteInfo("Received GetAllBladesDefaultPowerState()");

            Tracer.WriteUserLog("Invoked GetAllBladesDefaultPowerState()");

            GetAllBladesStateResponse responses = new GetAllBladesStateResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeStateResponseCollection = new List<BladeStateResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[maxbladeCount];

            for (int loop = 0; loop < maxbladeCount; loop++)
            {
                int bladeId = loop + 1;
                responses.bladeStateResponseCollection.Add(GetBladeDefaultPowerState(bladeId));

                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeStateResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        #endregion

        #region Blade Next Boot

        /// <summary>
        /// GetNextBoot - gets the next boot device
        /// </summary>
        /// <returns></returns>
        public Contracts.BootResponse GetNextBoot(int bladeId)
        {
            BootResponse response = new BootResponse();
            Tracer.WriteUserLog("Invoked GetNextBoot({0})", bladeId);
            Tracer.WriteInfo("Received GetNextBoot({0})", bladeId);

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetNextBoot", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Issue IPMI command
            Ipmi.NextBoot nextBoot = WcsBladeFacade.GetNextBoot((byte)bladeId);

            if (nextBoot.CompletionCode != (byte)Contracts.CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("GetNextBoot failed with completion code: {0:X}", nextBoot.CompletionCode);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.nextBoot = ChassisManagerUtil.GetContractsBootType(nextBoot.BootDevice);
            }

            response.completionCode =
                            ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)nextBoot.CompletionCode);
            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        /// <summary>
        /// SetNextBoot - sets the next boot device
        /// </summary>
        /// <returns></returns>
        public Contracts.BootResponse SetNextBoot(int bladeId, Contracts.BladeBootType bootType, bool uefi, bool persistent, int bootInstance = 0)
        {
            BootResponse response = new BootResponse();
            Tracer.WriteUserLog("Invoked SetNextBoot({0})", bladeId);
            Tracer.WriteInfo("Received SetNextBoot({0})", bladeId);

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetNextBoot", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // SetNextBoot should not be set to unknown. The unknown value is used only by getnextboot API
            if (bootType == Contracts.BladeBootType.Unknown)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString() + " Cannot be set to Unknown.";
                return response;
            }

            // Issue IPMI Command after checking
            if (Enum.IsDefined(typeof(Ipmi.BootType), ChassisManagerUtil.GetIpmiBootType(bootType)))
            {
                Ipmi.NextBoot nextBoot = WcsBladeFacade.SetNextBoot((byte)bladeId, ChassisManagerUtil.GetIpmiBootType(bootType), uefi, persistent, (byte)bootInstance);

                if (nextBoot.CompletionCode == (byte)Contracts.CompletionCode.Success)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    response.nextBoot = bootType;
                }
                else
                {
                    Tracer.WriteError("SetNextBoot failed with Completion Code {0:X}", nextBoot.CompletionCode);
                }

                response.completionCode =
                            ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)nextBoot.CompletionCode);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("Boot Type {0} not defined in Boot Order Type", bootType);
            }

            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        #endregion

        #region Blade & Chassis Logs

        /// <summary>
        /// Read Chassis Log
        /// </summary>
        /// <returns>returns logPacket structure poluated. If null then failure</returns>
        public ChassisLogResponse ReadChassisLog()
        {
            ChassisLogResponse response = new ChassisLogResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            response.logEntries = new List<LogEntry>();
            Tracer.WriteUserLog("Invoked ReadChassisLog()");
            Tracer.WriteInfo("Invoked ReadChassisLog()");
            
            try
            {
                response.logEntries = UserLogXmllinqHelper.GetFilteredLogEntries(DateTime.MinValue, DateTime.MaxValue, ConfigLoaded.UserLogMaxEntries);
                if (response.logEntries != null)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = "No log entries returned";
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(e.Message);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("ReadChassisLog failed with exception:{0} ", e.Message);
            }
            return response;
        }

        /// <summary>
        /// Read chassis log with Timestamp
        /// </summary>
        /// <param name="start">Start Timestamp</param>
        /// <param name="end">End Timestamp</param>
        /// <returns>Chassis Log</returns>
        public ChassisLogResponse ReadChassisLogWithTimestamp(DateTime start, DateTime end)
        {
            ChassisLogResponse response = new ChassisLogResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            response.logEntries = new List<LogEntry>();
            Tracer.WriteUserLog("Invoked ReadChassisLogWithTimestamp()");
            Tracer.WriteInfo("Invoked ReadChassisLogWithTimestamp()");
            DateTime endTimeWhenTimeofDayNotPresented = end;
            DateTime startTimeWhenTimeofDayNotPresented = start;

            // Check that end is later than start
            if(DateTime.Compare(start, end)==0 && start==default(DateTime))
            {
                endTimeWhenTimeofDayNotPresented = DateTime.MaxValue;
                startTimeWhenTimeofDayNotPresented = DateTime.MinValue;
            }
            else if (DateTime.Compare(start, end) >= 0)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "endTimestamp must be later or equal to startTimestamp";
                return response;
            }

            // When the user do not present time of the day in the 'end' parameter, it defaults to beginning of the day 00:00:00 hours 
            // The assumed intention of the user is to get all logs from the 'end' date, therefore collect logs until end of the day (23:59:59 hours) 
            if (endTimeWhenTimeofDayNotPresented.Hour == 0 && 
                endTimeWhenTimeofDayNotPresented.Minute == 0 &&
                endTimeWhenTimeofDayNotPresented.Second == 0)
            {
                    endTimeWhenTimeofDayNotPresented = endTimeWhenTimeofDayNotPresented.Add(new TimeSpan(23, 59, 59));
            }
            
            try
            {
                response.logEntries = UserLogXmllinqHelper.GetFilteredLogEntries(startTimeWhenTimeofDayNotPresented, endTimeWhenTimeofDayNotPresented, ConfigLoaded.UserLogMaxEntries);
                if (response.logEntries != null)
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = "No log entries returned within the requested time period";
                }
            }
            catch (Exception e)
            {
                Tracer.WriteError(e.Message);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("ReadChassisLog failed with exception:{0} ", e.Message);
            }
            return response;
        }

        /// <summary>
        /// Clear chassis log
        /// </summary>
        /// <returns>1 indicates success. 0 indicates failure.</returns>
        public Contracts.ChassisResponse ClearChassisLog()
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            string filePath = ConfigLoaded.UserLogFilePath;
            Tracer.WriteUserLog("Invoked ClearChassisLog()");

            // Initialize to failure to start with
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            try
            {
                if (Tracer.ClearUserLog())
                {
                    response.completionCode = Contracts.CompletionCode.Success;
                    Tracer.WriteInfo("Cleared chassis log");
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.Failure;
                    response.statusDescription = "Failed to clear chassis log";
                    Tracer.WriteError("Failed to clear chassis log");
                }
            }
            catch (IOException ex)
            {
                Tracer.WriteError("ClearChassisLog Error " + ex.Message);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("ClearChassisLog Error " + ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Read Blade Log for specified blade with timestamp
        /// TO DO M2 - Modify to include timestamp
        /// </summary>
        /// <param name="bladeId">Blade ID</param>
        /// <param name="startTimestamp">Start Timestamp</param>
        /// <param name="endTimestamp">End Timestamp</param>
        /// <returns>Blade log for specified blade</returns>
        public ChassisLogResponse ReadBladeLogWithTimestamp(int bladeId, DateTime startTimestamp, DateTime endTimestamp)
        {
            Tracer.WriteUserLog("Invoked ReadBladeLogWithTimestamp(bladeId: {0}, StartTimestamp: {1}, EndTimestamp: {2}", bladeId, startTimestamp, endTimestamp);

            ChassisLogResponse response = new ChassisLogResponse();
            ChassisLogResponse filteredResponse = new ChassisLogResponse();
            filteredResponse.logEntries = new List<LogEntry>();
            filteredResponse.completionCode = Contracts.CompletionCode.Unknown;
            filteredResponse.statusDescription = String.Empty;

            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("ReadBladeLogWithTimestamp", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            // Check that endTimestamp is equal or later than startTimestamp
            if (DateTime.Compare(startTimestamp, endTimestamp) > 0)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "endTimestamp must be later or equal to startTimestamp";
                return response;
            }

            // When the user does not present time of the day in the 'end' parameter, it defaults to beginning of the day 00:00:00 hours 
            // The assumed intention of the user is to get all logs from the 'end' date, therefore collect logs until end of the day (23:59:59 hours) 
            DateTime endTimestampAdjusted = endTimestamp;
            if (endTimestamp.Hour == 0 && endTimestamp.Minute == 0 && endTimestamp.Second == 0)
            {
                endTimestampAdjusted = endTimestampAdjusted.Add(new TimeSpan(23, 59, 59));
            } 

            response = ReadBladeLog(bladeId);
            try
            {
                if (response.completionCode == Contracts.CompletionCode.Success)
                {
                    for (int i = 0; i < response.logEntries.Count(); i++)
                    {
                        if (response.logEntries[i].eventTime >= startTimestamp && response.logEntries[i].eventTime <= endTimestampAdjusted)
                        {
                            filteredResponse.logEntries.Add(response.logEntries[i]);
                        }
                    }
                    filteredResponse.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    filteredResponse.completionCode = Contracts.CompletionCode.Failure;
                    filteredResponse.statusDescription = Contracts.CompletionCode.Failure.ToString();
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("ReadLogWithTimestamp failed for BladeID:{0}, with the exception {1}", bladeId, ex.Message);
                filteredResponse.completionCode = Contracts.CompletionCode.Failure;
                filteredResponse.statusDescription = Contracts.CompletionCode.Failure.ToString() + ": " + ex.Message;
            }
            return filteredResponse;
        }

        /// <summary>
        /// Read blade log, for given blade number
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>returns logPacket structure poluated. If null then failure</returns>
        public ChassisLogResponse ReadBladeLog(int bladeId)
        {
            Tracer.WriteInfo("Received Readbladelog({0})", bladeId);
            Tracer.WriteUserLog("Invoked ReadBladelog(bladeId: {0})", bladeId);

            // Blade spec limits number of SEL entries to 226 
            uint maxSelEntries = 226;
            // Initialize response
            ChassisLogResponse selLog = new ChassisLogResponse();
            selLog.logEntries = new List<LogEntry>();
            selLog.completionCode = Contracts.CompletionCode.Unknown;
            selLog.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("ReadBladeLog", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                selLog.completionCode = varResponse.completionCode;
                selLog.statusDescription = varResponse.statusDescription;
                return selLog;
            }

            Ipmi.SystemEventLog selRecord = WcsBladeFacade.GetSel((byte)bladeId);

            Tracer.WriteInfo("Blade {0} ReadBladeLog Return: {1}", bladeId, selRecord.CompletionCode);

            if (selRecord.CompletionCode != 0 && selRecord.CompletionCode != 0xCB)
            {
                Tracer.WriteWarning("ReadBladeLog on blade {0} failed with completion code {1:X}", bladeId, selRecord.CompletionCode);
                selLog.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)selRecord.CompletionCode);
                selLog.statusDescription = selLog.completionCode.ToString();
            }
            else
            {
                try
                {
                    // Event time and description lists
                    List<DateTime> eventTimeList = new List<DateTime>();
                    List<string> eventDescriptionList = new List<string>();
                    int startingIndex = 0;
                    ushort minRecordId = ushort.MaxValue;

                    Ipmi.SystemEventLogMessage eventLog;
                    string eventDescription;
                    string spacer = ConfigLoaded.EventLogStrSpacer +
                                    ConfigLoaded.EventLogStrSeparator +
                                    ConfigLoaded.EventLogStrSpacer;

                    // Separate log entries into time and description arrays
                    for (int index = 0; index < selRecord.EventLog.Count; index++)
                    {
                        if (index >= maxSelEntries)
                        {
                            break;
                        }

                        // Extract event data
                        eventLog = selRecord.EventLog[index];
                        EventLogData eventLogData = ExtractEventMessage(eventLog);

                        // Format event description according to record type
                        if (eventLog.EventFormat == Ipmi.EventMessageFormat.SystemEvent)
                        {
                            // Get sensor details.
                            string sensor = string.Format(ConfigLoaded.EventLogStrSensor,
                                                          eventLog.SensorType.ToString(),
                                                          WcsBladeFacade.GetSensorDescription((byte)bladeId, eventLog.GeneratorId[0],
                                                                                              eventLog.SensorNumber), eventLog.SensorNumber);
                            // Get Event Data Message
                            string eventData = string.Format(ConfigLoaded.EventData, eventLog.EventPayload);

                            // Add entry to array
                            eventDescription = (
                                // Record ID and Type
                                "RecordID: " + eventLog.RecordId +
                                " Record Type: " + string.Format("0x{0:X}", eventLog.RecordType) + spacer +
                                // Generator ID
                                "GenID: " + Ipmi.IpmiSharedFunc.ByteArrayToHexString(eventLog.GeneratorId) + spacer +
                                // Description and message
                                eventLogData.Description + spacer +
                                eventLogData.EventMessage + spacer +
                                // Sensor Type and Number
                                sensor + spacer + eventLog.EventDir.ToString() + spacer +
                                // Event Data
                                eventData);
                        }
                        else if (eventLog.EventFormat == Ipmi.EventMessageFormat.OemTimeStamped)
                        {
                            // Add entry to array
                            eventDescription = (
                                // Record ID and Type
                                "RecordID: " + eventLog.RecordId +
                                " Record Type: " + string.Format("0x{0:X}", eventLog.RecordType) + spacer +
                                // Description and message
                                eventLogData.Description + spacer +
                                eventLogData.EventMessage);
                        }
                        else if (eventLog.EventFormat == Ipmi.EventMessageFormat.OemNonTimeStamped)
                        {
                            // Add entry to array
                            eventDescription = (
                                // Record ID and Type
                                "RecordID: " + eventLog.RecordId +
                                " Record Type: " + string.Format("0x{0:X}", eventLog.RecordType) + spacer +
                                // Description and message
                                eventLogData.Description + spacer +
                                eventLogData.EventMessage);
                        }
                        else 
                        {
                            eventDescription = string.Empty;
                        }

                        // Track starting index of the entry with the smallest Record ID
                        if (eventLog.RecordId < minRecordId)
                        {
                            minRecordId = eventLog.RecordId;
                            startingIndex = index;
                        }
                        // Add event time to list
                        eventTimeList.Add(eventLog.EventDate);
                        eventDescriptionList.Add(eventDescription);
                    }

                    // Add SEL entries to response starting with the one with the smallest Record ID
                    int entryIdx = startingIndex;
                    for (int entryAdded = 0; entryAdded < eventTimeList.Count; entryAdded++)
                    {
                        LogEntry logEntry = new LogEntry();
                        logEntry.eventTime = eventTimeList[entryIdx];
                        logEntry.eventDescription = eventDescriptionList[entryIdx];
                        selLog.logEntries.Add(logEntry);
                        
                        // Go to next entry and handle wraparound
                        entryIdx++;
                        if (entryIdx == eventTimeList.Count)
                            entryIdx = 0;
                    }

                    selLog.completionCode = Contracts.CompletionCode.Success;
                    Tracer.WriteInfo("ReadBladeLog on blade " + bladeId + " returned " + selLog.logEntries.Count() + 
                        " entries out of " + selRecord.EventLog.Count + " found on BMC");
                }
                catch (Exception ex)
                {
                    Tracer.WriteError("ReadBladeLog on blade " + bladeId + " failed with exception: " + ex);
                    selLog.completionCode = Contracts.CompletionCode.Failure;
                    selLog.statusDescription = selLog.completionCode.ToString() + ": " + ex.Message;
                }
            }
            return selLog;
        }

        /// <summary>
        /// Clear blade log
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating clear log operation was success/failure</returns>
        public BladeResponse ClearBladeLog(int bladeId)
        {
            Tracer.WriteUserLog(" Invoked ClearBladeLog(bladeID: {0})", bladeId);

            byte MaxbladeCount = (byte)ConfigLoaded.Population;
            Tracer.WriteInfo("Received clearbladelog({0})", bladeId);

            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("ClearBladeLog", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            bool status = WcsBladeFacade.ClearSel((byte)bladeId);
            if (status != true)
            {
                Tracer.WriteWarning("Clear SEL log failed");
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                return response;
            }
            response.completionCode = Contracts.CompletionCode.Success;
            return response;
        }

        #endregion

        #region Blade Power Reading

        /// <summary>
        /// Get blade power reading for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response containing the power reading</returns>
        public BladePowerReadingResponse GetBladePowerReading(int bladeId)
        {
            Tracer.WriteInfo("Invoked GetBladePowerReading(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked GetBladePowerReading(bladeId: {0})", bladeId);

            BladePowerReadingResponse response = new BladePowerReadingResponse();
            response.bladeNumber = bladeId;
            response.powerReading = -1;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladePowerReading", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            List<Ipmi.PowerReading> myPowerReading = new List<Ipmi.PowerReading>();
            myPowerReading = WcsBladeFacade.GetPowerReading((byte)bladeId);

            if (myPowerReading == null || myPowerReading.Count == 0 || myPowerReading[0].CompletionCode != 0 || myPowerReading[0].PowerSupport == false)
            {
                Tracer.WriteError("GetPowerReading:(" + bladeId + ") Error reading power ");
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                return response;
            }
            response.powerReading = myPowerReading[0].Present;
            response.completionCode = Contracts.CompletionCode.Success;
            Tracer.WriteInfo("GetPowerReading:(" + bladeId + ") Avg " + myPowerReading[0].Average + " Curr " + myPowerReading[0].Present + " Support " + myPowerReading[0].PowerSupport);

            return response;
        }

        /// <summary>
        /// Get power reading for all blades
        /// </summary>
        /// <returns>Array of blade responses containing the power reading</returns>
        public GetAllBladesPowerReadingResponse GetAllBladesPowerReading()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;
            GetAllBladesPowerReadingResponse responses = new GetAllBladesPowerReadingResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladePowerReadingCollection = new List<BladePowerReadingResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteUserLog("Invoked GetAllBladesPowerReading()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladePowerReadingCollection.Add(GetBladePowerReading((int)bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladePowerReadingCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("GetAllBladesPowerReading Exception" + ex);
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        #endregion

        #region Blade Power Limit

        /// <summary>
        /// Get blade power limit for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response containing the blade power limit value</returns>
        public BladePowerLimitResponse GetBladePowerLimit(int bladeId)
        {
            Tracer.WriteUserLog("Invoked GetBladePowerLimit(bladeId: {0})", bladeId);

            BladePowerLimitResponse response = new BladePowerLimitResponse();
            response.bladeNumber = bladeId;
            response.powerLimit = -1;

            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladePowerLimit", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            Ipmi.PowerLimit myPowerLimit = WcsBladeFacade.GetPowerLimit((byte)(bladeId));

            if (myPowerLimit == null) // If the return packet is null, then return with failure
            {
                Tracer.WriteError("GetBladePowerLimit failed for blade({0}) with Unspecified Error.", bladeId);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }
            else if (myPowerLimit.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("GetBladePowerLimit failed for blade({0}) with CompletionCode({1})", bladeId, myPowerLimit.CompletionCode);
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
            }
            else // Both active and inactive power limit will reach this code
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.isPowerLimitActive = (bool)myPowerLimit.ActiveLimit;
                response.powerLimit = (double)myPowerLimit.LimitValue;
                Tracer.WriteInfo("GetBladePowerLimit(" + bladeId + "):" + " CC: " + myPowerLimit.CompletionCode + " LimitVal: " + myPowerLimit.LimitValue + " Active: " + myPowerLimit.ActiveLimit);
            }
            return response;
        }

        /// <summary>
        /// Get power limit value for all blades
        /// </summary>
        /// <returns>Array of blade responses containing the blade power limit values</returns>
        public GetAllBladesPowerLimitResponse GetAllBladesPowerLimit()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            GetAllBladesPowerLimitResponse responses = new GetAllBladesPowerLimitResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladePowerLimitCollection = new List<BladePowerLimitResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            responses.bladePowerLimitCollection = new List<BladePowerLimitResponse>();

            Tracer.WriteUserLog("Invoked GetAllBladesPowerLimit()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladePowerLimitCollection.Add(GetBladePowerLimit(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladePowerLimitCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("GetAllBladesPowerLimit Exception");
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Set blade power limit to given value for the specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <param name="powerLimitInWatts">Power limit to set</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public BladeResponse SetBladePowerLimit(int bladeId, double powerLimitInWatts)
        {
            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            // Keeping these method specific values as local variables
            const int retryCount = 3;
            byte retryCompletionCode = Convert.ToByte(CompletionCode.IpmiNodeBusy);
            const int retrySnoozeTimeInMilliseconds = 250;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladePowerLimit", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (ChassisManagerUtil.CheckBladePowerLimit(powerLimitInWatts))
            {
                Tracer.WriteUserLog("Invoked SetBladePowerLimit( bladeId: {0}, powerLimitInWatts: {1})", bladeId, powerLimitInWatts);
                Tracer.WriteInfo("Invoked SetBladePowerLimit( bladeId: {0}, powerLimitInWatts: {1})", bladeId, powerLimitInWatts);

                for (int iteration = 0; iteration < retryCount; iteration++)
                {
                    // Note: Action is set to 0 (do nothing). Setting action to 1 will cause blade shutdown upon power limit violation
                    // Note: 6 sec correction time and 1 sec sampling period is the minimum time period that works
                    Ipmi.ActivePowerLimit myActiveLimit = WcsBladeFacade.SetPowerLimit((byte)(bladeId), (short)powerLimitInWatts, 
                        ConfigLoaded.SetPowerLimitCorrectionTimeInMilliseconds, 0, 1);
                    Tracer.WriteInfo("SetPowerLimit-Iteration({0}): SetLimit ({1}) CompletionCode ({2}).", iteration + 1, 
                        myActiveLimit.LimitSet, myActiveLimit.CompletionCode);

                    if (myActiveLimit.CompletionCode == (byte)CompletionCode.Success)
                    {
                        Tracer.WriteInfo("SetPowerLimit-Iteration({0}) SUCCESS: Bladeid ({1}) CompletionCode ({2}).", iteration + 1,
                            bladeId, myActiveLimit.CompletionCode);

                        response.completionCode = Contracts.CompletionCode.Success;
                    }
                    else if (myActiveLimit.CompletionCode == retryCompletionCode)
                    {
                        Tracer.WriteWarning("SetPowerLimit-Iteration({0}) RETRY: Bladeid ({1}) CompletionCode ({2}).", iteration + 1,
                            bladeId, myActiveLimit.CompletionCode);

                        // Not sure if this is needed.. snooze time with random backoff delay to avoid congestion 
                        Thread.Sleep(iteration * retrySnoozeTimeInMilliseconds);
                        continue; //Go to the next retry iteration 
                    }
                    else
                    {
                        Tracer.WriteError("SetPowerLimit-Iteration({0}) FAILURE: Bladeid ({1}) CompletionCode ({2}).", iteration + 1,
                            bladeId, myActiveLimit.CompletionCode);

                        response.completionCode = Contracts.CompletionCode.Failure;
                        response.statusDescription = response.completionCode.ToString();
                    }
                    break;
                }
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Set all blades power limit to the given value
        /// </summary>
        /// <param name="powerLimitInWatts">Power limit to set</param>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesPowerLimit(double powerLimitInWatts)
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteUserLog("Invoked SetAllBladesPowerLimit(powerLimitInWatts: {0})", powerLimitInWatts);
            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    responses.bladeResponseCollection.Add(SetBladePowerLimit((int)(index + 1), powerLimitInWatts));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesPowerLimit Exception" + ex.Message);
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Set power limit ON for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public BladeResponse SetBladePowerLimitOn(int bladeId)
        {
            BladeResponse response = new BladeResponse();
            response.bladeNumber = bladeId;

            Tracer.WriteUserLog("Invoked SetBladePowerLimitOn(bladeId: {0})", bladeId);

            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladePowerLimitOn", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (!WcsBladeFacade.ActivatePowerLimit((byte)(bladeId), true))
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                return response;
            }
            Tracer.WriteInfo("ActivatePowerLimit({0}): Activated", bladeId);
            response.completionCode = Contracts.CompletionCode.Success;

            return response;
        }

        /// <summary>
        /// Set active power limit ON for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesPowerLimitOn()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteUserLog("Invoked SetAllBladesPowerLimitOn()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladeResponseCollection.Add(SetBladePowerLimitOn(bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesPowerLimitOn Exception");
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Set power limit OFF for specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>Blade response indicating blade operation was success/failure</returns>
        public BladeResponse SetBladePowerLimitOff(int bladeId)
        {
            BladeResponse response = new BladeResponse();

            Tracer.WriteUserLog("Invoked SetBladePowerLimitOff(bladeId: {0})", bladeId);

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladePowerLimitOff", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            if (!WcsBladeFacade.ActivatePowerLimit((byte)(bladeId), false))
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = response.completionCode.ToString();
                return response;
            }
            Tracer.WriteInfo("ActivatePowerLimit({0}): Deactivated", bladeId);
            response.completionCode = Contracts.CompletionCode.Success;

            return response;
        }

        /// <summary>
        /// Set power limit OFF for all blades
        /// </summary>
        /// <returns>Array of blade responses indicating blade operation was success/failure</returns>
        public AllBladesResponse SetAllBladesPowerLimitOff()
        {
            byte MaxbladeCount = (byte)ConfigLoaded.Population;

            AllBladesResponse responses = new AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = string.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[MaxbladeCount];

            Tracer.WriteUserLog("Invoked SetAllBladesPowerLimitOff()");

            try
            {
                for (int index = 0; index < ConfigLoaded.Population; index++)
                {
                    int bladeId = index + 1;
                    responses.bladeResponseCollection.Add(SetBladePowerLimitOff((int)bladeId));

                    // Set the internal blade response to the blade completion code.
                    bladeInternalResponseCollection[index] = responses.bladeResponseCollection[index].completionCode;
                }
            }
            catch (Exception ex)
            {
                responses.completionCode = Contracts.CompletionCode.Failure;
                responses.statusDescription = responses.completionCode.ToString() + ": " + ex.Message;
                Tracer.WriteError("SetAllBladesPowerLimitOff Exception");
                return responses;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        #endregion

        #region Blade Serial Session

        /// <summary>
        /// Starts serial session on a blade
        /// </summary>
        /// <returns> Returns information about the new session created including the session token. If null then failure</returns>
        public StartSerialResponse StartBladeSerialSession(int bladeId, int sessionTimeoutInSecs, bool powerOnWait)
        {
            StartSerialResponse response = new StartSerialResponse();
            response.serialSessionToken = null;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteInfo("Received StartBladeSerialSession(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked StartBladeSerialSession(bladeId: {0})", bladeId);

            if (ChassisManagerUtil.CheckBladeId((byte)bladeId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("StartBladeSerialSession failed: Blade ID: {0} out of range ", bladeId);
                return response;
            }

            if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
            {
                response.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                response.statusDescription = response.statusDescription;
                return response;
            }

            if (!BladeSerialSessionMetadata.CheckBladeSerialSessionTimeout(sessionTimeoutInSecs))
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription = "Session Timeout out of range";
                return response;
            }

            if (powerOnWait)
            {
                // Power on blade and keep trying to open a serial session
                BladeResponse powerOn = SetPowerOn(bladeId);

                if (powerOn.completionCode == Contracts.CompletionCode.Success)
                {
                    int retry = 0;

                    while (retry < ConfigLoaded.SerialSessionPowerOnRetry)
                    {
                        response = ChassisState.BladeSerialMetadata[bladeId].StartBladeSerialSession(bladeId, sessionTimeoutInSecs);

                        // if the response times out, sleep and retry.
                        if (response.completionCode == Contracts.CompletionCode.FirmwareDecompressing)
                        {
                            BladePowerStatePacket powerPacket = ChassisState.BladePower[bladeId - 1].GetCachedBladePowerState();

                            if (powerPacket.CompletionCode == CompletionCode.Success)
                            {
                                // sleep for five seconds
                                Thread.Sleep(TimeSpan.FromSeconds(powerPacket.DecompressionTime));
                            }
                            else
                            {
                                // sleep for five seconds
                                Thread.Sleep(ConfigLoaded.SerialSessionPowerOnWait);     
                            }

                            retry++;
                        }
                        else
                        {
                            // if StartBladeSerialSession fails or completes successfully for any other reson, 
                            // then just exit the loop.  Or if it succeeds exit the loop.
                            break;
                        }
                    }
                }
                else
                {
                    response.completionCode = Contracts.CompletionCode.DevicePoweredOff;
                    response.statusDescription = "Device Failed to Power On.";
                }
            }
            else
            {

                Contracts.ChassisResponse chassisResponse = CheckBladeAndFirmwareState((byte)bladeId);
                if (chassisResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    response.completionCode = chassisResponse.completionCode;
                    response.statusDescription = chassisResponse.statusDescription;
                    return response;
                }

                response = ChassisState.BladeSerialMetadata[bladeId].StartBladeSerialSession(bladeId, sessionTimeoutInSecs);
                if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
                {
                    Tracer.WriteInfo("StartBladeSerialSession succeeded for bladeId: " + bladeId);
                }
                else
                {
                    Tracer.WriteError("StartBladeSerialSession failed for bladeId: {0} with completion code: {1}", bladeId, response.completionCode.ToString());
                }
            }


            return response;
        }

        /// <summary>
        /// Stops serial session on a blade
        /// </summary>
        public Contracts.ChassisResponse StopBladeSerialSession(int bladeId, string sessionToken, bool forceKill)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteInfo("Received StopBladeSerialSession(bladeId: {0})", bladeId);
            Tracer.WriteUserLog("Invoked StopBladeSerialSession(bladeId: {0})", bladeId);

                if (ChassisManagerUtil.CheckBladeId((byte)bladeId) != (byte)CompletionCode.Success)
                {
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;

                    Tracer.WriteWarning("StopBladeSerialSession failed: Blade ID: {0} out of range: ", bladeId);
                    return response;
                }

                response = CheckBladeAndFirmwareState((byte)bladeId);
            if (response.completionCode != Contracts.CompletionCode.Success)
                {
                    return response;
                }

                if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
                {
                    response.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                    return response;
                }

            response = ChassisState.BladeSerialMetadata[bladeId].StopBladeSerialSession(bladeId, sessionToken, forceKill);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("StopBladeSerialSession succeeded for bladeId: " + bladeId);
            }
            else
            {
                Tracer.WriteError("StopBladeSerialSession failed for bladeId: {0} with completion code: {1}",
                    bladeId, response.completionCode.ToString());
            }

            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        /// <summary>
        /// Send data to a blade serial session
        /// </summary>
        public Contracts.ChassisResponse SendBladeSerialData(int bladeId, string sessionToken, byte[] data)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteInfo("Received SendBladeSerialData(bladeId: {0}) API", bladeId);
            Tracer.WriteUserLog("Invoked SendBladeSerialData(bladeId: {0}) API", bladeId);

            if (data == null || ChassisManagerUtil.CheckBladeId((byte)bladeId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("SendBladeSerialData failed : Blade ID: {0} out of range: ", bladeId);
                return response;
            }

            response = CheckBladeAndFirmwareState((byte)bladeId);
            if (response.completionCode != Contracts.CompletionCode.Success)
            {
                return response;
            }

            if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
            {
                response.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                return response;
            }

            response = ChassisState.BladeSerialMetadata[bladeId].SendBladeSerialData(bladeId, sessionToken, data);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("SendBladeSerialdata succeeded for bladeId: " + bladeId);
            }
            else
            {
                Tracer.WriteError("SendBladeSerialData failed for bladeId: {0} with completion code: {1}",
                    bladeId, response.completionCode.ToString());
            }

            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        /// <summary>
        /// Receive data from a blade serial session
        /// </summary>
        public SerialDataResponse ReceiveBladeSerialData(int bladeId, string sessionToken)
        {
            SerialDataResponse response = new SerialDataResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteInfo("Received ReceiveBladeSerialData(bladeId: {0}) API", bladeId);
            Tracer.WriteUserLog("Invoked ReceiveBladeSerialData(bladeId: {0}, sessionToken: {1}) API", bladeId, sessionToken);

            if (ChassisManagerUtil.CheckBladeId((byte)bladeId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("ReceiveBladeSerialData failed: Blade ID: {0} out of range: ", bladeId);
                return response;
            }

            Contracts.ChassisResponse chassisResponse = CheckBladeAndFirmwareState((byte)bladeId);
            if (chassisResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = chassisResponse.completionCode;
                response.statusDescription = chassisResponse.statusDescription;
                return response;
            }

            if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
            {
                response.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                return response;
            }

            response = ChassisState.BladeSerialMetadata[bladeId].ReceiveBladeSerialData(bladeId, sessionToken);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("ReceiveBladeSerialData succeeded for bladeId: " + bladeId);
            }
            else if (response.completionCode == Contracts.CompletionCode.Timeout)
            {
                Tracer.WriteInfo("ReceiveBladeSerialData: No data received from bladeId: {0} (expected timeout).", bladeId);
            }
            else if (response.completionCode == Contracts.CompletionCode.BmcRxSerialBufferOverflow)
            {
                Tracer.WriteInfo("ReceiveBladeSerialData: BMC Rx Serial Buffer Overflow on blade: {0}", bladeId);
            }
            else
            {
                Tracer.WriteError("ReceiveBladeSerialData failed for bladeId: {0} with completion code: {1}", bladeId, response.completionCode.ToString());
            }

            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        #endregion

        #region Chassis Serial Port

        /// <summary>
        /// Starts serial console port session on the specified serial port
        /// 
        /// </summary>
        /// <returns> Returns information about the new session created including the exit key sequence. If null then failure</returns>
        public StartSerialResponse StartSerialPortConsole(int portId, int sessionTimeoutInSecs, int deviceTimeoutInMsecs, int baudRate)
        {
            StartSerialResponse response = new StartSerialResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            response.serialSessionToken = null;

            Tracer.WriteUserLog("Invoked StartSerialPortConsole(portId: {0})", portId);
            Tracer.WriteInfo("Received StartSerialPortConsole(portId: {0})", portId);

            if (ChassisManagerUtil.CheckSerialConsolePortId((byte)portId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("StartSerialPortConsole failed: Port ID out of range " + portId);
                response.statusDescription = String.Format("StartSerialPortConsole failed: Port ID out of range " + portId);
                return response;
            }

            ChassisManagerUtil.BaudRateStruct baudRateVal = ChassisManagerUtil.ValidateAndGetBaudRate(baudRate);
            if (!baudRateVal.isValid)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("StartSerialPortConsole failed: Baud rate ({0}) out of range" + baudRate);
                response.statusDescription = String.Format("StartSerialPortConsole failed: Baud rate ({0}) out of range " + baudRate);
                return response;
            }

            int portIndex = ChassisManagerUtil.GetSerialConsolePortIndexFromId(portId);
            response = ChassisState.SerialConsolePortsMetadata[portIndex].StartSerialPortConsole(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(portIndex), sessionTimeoutInSecs, deviceTimeoutInMsecs, baudRateVal.baudRate);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("StartSerialPortConsole succeeded for portId: " + portId);
            }
            else
            {
                Tracer.WriteError("StartSerialPortConsole: failed for portId: {0} and baudRate: {1} with completion code: {2}", portId, baudRate, response.completionCode.ToString());
                response.statusDescription = String.Format("StartSerialPortConsole: failed for portId: {0} and baudRate: {1} with completion code: {1}", portId, baudRate, response.completionCode.ToString());
            }
            return response;
        }

        /// <summary>
        /// Stops serial session on a port
        /// </summary>
        public Contracts.ChassisResponse StopSerialPortConsole(int portId, string sessionToken, bool forceKill)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked StopSerialPortConsole(portId: {0}, sessionToken: {1})", portId, sessionToken);
            Tracer.WriteInfo("Received StopSerialPortConsole({0})", portId);

            if (ChassisManagerUtil.CheckSerialConsolePortId((byte)portId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("StopSerialPortConsole failed: Port ID out of range " + portId);
                response.statusDescription = String.Format("StopSerialPortConsole failed: Port ID out of range " + portId);
                return response;
            }

            int portIndex = ChassisManagerUtil.GetSerialConsolePortIndexFromId(portId);
            response = ChassisState.SerialConsolePortsMetadata[portIndex].StopSerialPortConsole(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(portIndex), sessionToken, forceKill);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("StopSerialPortConsole succeeded for portId: " + portId);
            }
            else
            {
                Tracer.WriteError("StopSerialPortConsole: failed for portId: {0} with completion code: {1}", portId, response.completionCode.ToString());
                response.statusDescription = String.Format("StopSerialPortConsole: failed for portId: {0} with completion code: {1}", portId, response.completionCode.ToString());
            }
            return response;
        }

        /// <summary>
        /// Send data to a port serial session
        /// </summary>
        public Contracts.ChassisResponse SendSerialPortData(int portId, string sessionToken, byte[] data)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked SendSerialPortData(portId: {0}, sessionToken: {1}, data: {2})", portId, sessionToken, data);
            Tracer.WriteInfo("Received SendSerialPortData({0})", portId);

            if (ChassisManagerUtil.CheckSerialConsolePortId((byte)portId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                response.statusDescription =
                    String.Format("SendSerialPortData failed: Port ID out of range: {0}", portId);
                Tracer.WriteWarning("SendSerialPortData failed: Port ID out of range: {0}", portId);
                return response;
            }

            int portIndex = ChassisManagerUtil.GetSerialConsolePortIndexFromId(portId);
            response = ChassisState.SerialConsolePortsMetadata[portIndex].SendSerialPortData(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(portIndex), sessionToken, data);

            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("SendSerialPortData succeeded for portId: " + portId);
            }
            else
            {
                Tracer.WriteError("SendSerialPortData: failed for portId: {0} with completion code: {1} ", portId, response.completionCode.ToString());
                response.statusDescription = String.Format("SendSerialPortData: failed for portId: {0} with completion code: {1} ", portId, response.completionCode.ToString());
            }
            return response;
        }

        /// <summary>
        /// Receive data from a port serial session
        /// </summary>
        public SerialDataResponse ReceiveSerialPortData(int portId, string sessionToken)
        {
            SerialDataResponse response = new SerialDataResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            Tracer.WriteUserLog("Invoked ReceiveSerialPortData(portId: {0}, sessionToken: {1})", portId, sessionToken);
            Tracer.WriteInfo("Received ReceiveSerialPortData({0})", portId);

            if (ChassisManagerUtil.CheckSerialConsolePortId((byte)portId) != (byte)CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                Tracer.WriteWarning("ReceiveSerialPortData failed: ParameterOutOfRange for Port ID " + portId);
                response.statusDescription = String.Format("ReceiveSerialPortData failed: ParameterOutOfRange for Port ID " + portId);
                return response;
            }

            int portIndex = ChassisManagerUtil.GetSerialConsolePortIndexFromId(portId);
            response = ChassisState.SerialConsolePortsMetadata[portIndex].ReceiveSerialPortData(ChassisManagerUtil.GetSerialConsolePortIdFromIndex(portIndex), sessionToken);

            // Set Http code status
            if (ChassisManagerUtil.CheckCompletionCode(response.completionCode))
            {
                Tracer.WriteInfo("ReceiveSerialPortData succeeded for portId: " + portId);
            }
            else if (response.completionCode == Contracts.CompletionCode.Timeout)
            {
                Tracer.WriteInfo("ReceiveSerialPortdata: No data to be received from portId: {0} (expected timeout).", portId);
                response.statusDescription = String.Format("ReceiveSerialPortdata: No data to be received from portId: {0} (expected timeout).", portId);
            }
            else
            {
                Tracer.WriteError("ReceiveSerialPortData: failed for portId: {0} with completion code: {1}", portId, response.completionCode.ToString());
                response.statusDescription = String.Format("ReceiveSerialPortData: failed for portId: {0} with completion code: {1}", portId, response.completionCode.ToString());
            }
            return response;
        }

        #endregion

        #region Undocumented Commands

        /// <summary>
        /// Get the Post Code for the Blade
        /// </summary>
        public BiosPostCode GetPostCode(int bladeId)
        {
            Tracer.WriteInfo("Received GetPostCode({0})", bladeId);
            Tracer.WriteUserLog("Invoked GetPostCode(bladeid: {0})", bladeId);

            BiosPostCode postCodeResponse = new BiosPostCode();
            postCodeResponse.bladeNumber = bladeId;
            postCodeResponse.completionCode = Contracts.CompletionCode.Unknown;
            postCodeResponse.statusDescription = String.Empty;

            Contracts.ChassisResponse varResponse = ValidateRequest("GetPostCode", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                postCodeResponse.completionCode = varResponse.completionCode;
                postCodeResponse.statusDescription = varResponse.statusDescription;
                return postCodeResponse;
            }

            // Get the current BIOS POST code
            Ipmi.BiosCode response = WcsBladeFacade.GetBiosCode((byte)bladeId, 0x00);

            if (response.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("GetPostCode for blade: {0} failed failed (Completion Code: {1:X})", bladeId, response.CompletionCode);
                postCodeResponse.completionCode =
                    ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)response.CompletionCode);
                postCodeResponse.statusDescription = postCodeResponse.completionCode.ToString();
            }
            else
            {
                // Set response
                postCodeResponse.PostCode = response.PostCode;
                postCodeResponse.completionCode = Contracts.CompletionCode.Success;

                // Get the previous BIOS POST code
                response = WcsBladeFacade.GetBiosCode((byte)bladeId, 0x01);

                // Provide previous POST code if available.  Note: After Blade_EN is deasserted it is not
                // expected for this POST code to be available. Therefore it is not considered
                // an error condition to not return a previous POST code.
                if (response.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Set response
                    postCodeResponse.PreviousPostCode = response.PostCode;
                }
                else
                {
                    postCodeResponse.PreviousPostCode = string.Format("Previous Code not available. Response Code: {0:X}", response.CompletionCode);
                }
            }

            return postCodeResponse;
        }

        /// <summary>
        /// returns the max PWM requirement.  Undocumented command used for
        /// data center AHU control integration
        /// </summary>
        public Contracts.MaxPwmResponse GetMaxPwmRequirement()
        {
            Contracts.MaxPwmResponse response = new Contracts.MaxPwmResponse();

            if (HelperFunction.MaxPwmRequirement > 0)
                response.completionCode = Contracts.CompletionCode.Success;
            else
                response.completionCode = Contracts.CompletionCode.Unknown;

            response.maxPwmRequirement = HelperFunction.MaxPwmRequirement;

            return response;
        }

        /// <summary>
        /// Turns a power supply off and on
        /// </summary>
        public Contracts.ChassisResponse ResetPsu(int psuId)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();

            if (ConfigLoaded.NumPsus > 0)
            {
                if (psuId < 0 || psuId > ConfigLoaded.NumPsus)
                {
                    response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                    return response;
                }

                // Step 1: Turn Psu off
                byte completionCode = (byte)ChassisState.Psu[(psuId - 1)].SetPsuOnOff(true);
                if (completionCode != (byte)CompletionCode.Success)
                {
                    Tracer.WriteWarning("Error on psu power off: PsuId {0} CompletionCode: 0x{1:X2}",
                        psuId, completionCode);

                    response.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping(completionCode);
                }
                else
                {
                    // subsequent commands are permitted to change upon failure.
                    response.completionCode = Contracts.CompletionCode.Success;
                }

                // Step 2: Turn Psu On
                completionCode = (byte)ChassisState.Psu[(psuId - 1)].SetPsuOnOff(false);
                if (completionCode != (byte)CompletionCode.Success)
                {
                    Tracer.WriteWarning("Error on psu power on: PsuId {0} CompletionCode: 0x{1:X2}",
                        psuId, completionCode);

                    response.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping(completionCode);
                }
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription += "\nPower Supply control not supported in sku configuration.";
            }

            return response;

        }

        /// <summary>
        /// Set SNTP Time Service
        /// </summary>
        public Contracts.ChassisResponse SetSntpServer(string primary, string secondary)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();

            Tracer.WriteInfo("Received SetSntpServer({0}, {1})", primary, secondary);
            Tracer.WriteUserLog("Received SetSntpServer({0}, {1})", primary, secondary);

            CompletionCode code = Sntp.SetNtpServer(new Sntp.NtpServerName(primary, secondary));

            if (code == CompletionCode.InvalidDataFieldInRequest)
            { 
                response.completionCode = Contracts.CompletionCode.Unknown;
                response.statusDescription = "Null values and zero Ip address not supported";
            }
            else
            {
                response.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)code);
            }

            return response;
        }

        /// <summary>
        /// Restore SNTP Time Service
        /// </summary>
        public Contracts.ChassisResponse RestoreSntpServer()
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();

            Tracer.WriteInfo("Received RestoreSntpServer()");
            Tracer.WriteUserLog("Received RestoreSntpServer()");

            CompletionCode code = Sntp.RestoreNtpServerDefault();

            if (code == CompletionCode.CannotExecuteRequestInvalidState)
            {
                response.completionCode = Contracts.CompletionCode.Unknown;
                response.statusDescription = "Win32Time service not responding";
            }
            else
            {
                response.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)code);
            }

            return response;
        }

        /// <summary>
        /// Get SNTP Time Service
        /// </summary>
        public Contracts.ChassisResponse GetSntpServer()
        {
            Contracts.SntpServerResponse sntpResponse = new Contracts.SntpServerResponse();

            Tracer.WriteInfo("Received GetSntpServer()");
            Tracer.WriteUserLog("Received GetSntpServer()");

            string response = string.Empty;
            CompletionCode code = Sntp.GetNtpServer(Sntp.RegisteryKey.NtpServer, out response);
            sntpResponse.SntpServer = response;

            if (code == CompletionCode.UnspecifiedError)
            {
                sntpResponse.completionCode = Contracts.CompletionCode.Unknown;
                sntpResponse.statusDescription = "Error retriving Win32 Sntp Settings";
            }
            else
            {
                sntpResponse.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)code);
            }

            return sntpResponse;
        }

        /// <summary>
        /// Scan device and get the settings
        /// </summary>
        /// <returns>object populated with the device settings</returns>
        public ScanDeviceResponse ScanDevice()
        {
            String message = "Received request to ScanDevice()";

            Tracer.WriteInfo(message);
            Tracer.WriteUserLog(message);

            // Initialize Response
            ScanDeviceResponse response = new ScanDeviceResponse();

            try
            {
                // Initialize Scope
                Microsoft.GFS.WCS.ChassisManager.ScanDevice.InitializeScope();

                // Populate all services on the device
                response.Services = Microsoft.GFS.WCS.ChassisManager.ScanDevice.ListAllServices();

                // Populate all users on the device
                response.Users = Microsoft.GFS.WCS.ChassisManager.ScanDevice.ListAllUsers();

                // Populate all groups after scanning the device
                response.UserGroups = Microsoft.GFS.WCS.ChassisManager.ScanDevice.ListAllGroupsAndMembers();

                // Get windows firewall setting
                ScanObject firewallSettingObj = new ScanObject();
                firewallSettingObj.Attribute = "Windows Firewall Enable Status";
                firewallSettingObj.Value = Microsoft.GFS.WCS.ChassisManager.ScanDevice.IsWindowsFirewallEnabled().ToString();
                if (!String.IsNullOrEmpty(firewallSettingObj.Value))
                {
                    firewallSettingObj.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    firewallSettingObj.completionCode = Contracts.CompletionCode.Failure;
                    firewallSettingObj.statusDescription = "Unable to get Firewall settings";
                }
                response.ScanObjects.Add(firewallSettingObj);

                // Get windows update setting
                ScanObject windowsUpdateSettingObj = new ScanObject();
                windowsUpdateSettingObj.Attribute = "Windows Update Enable Status";
                windowsUpdateSettingObj.Value = Microsoft.GFS.WCS.ChassisManager.ScanDevice.IsWindowsUpdateEnabled().ToString();
                if (!String.IsNullOrEmpty(windowsUpdateSettingObj.Value))
                {
                    windowsUpdateSettingObj.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    windowsUpdateSettingObj.completionCode = Contracts.CompletionCode.Failure;
                    windowsUpdateSettingObj.statusDescription = "Unable to get windows update settings";
                }
                response.ScanObjects.Add(windowsUpdateSettingObj);

                // Get EMS enabled setting
                ScanObject EMSObj = new ScanObject();
                EMSObj.Attribute = "EMS Enable Status";
                EMSObj.Value = Microsoft.GFS.WCS.ChassisManager.ScanDevice.IsEMSEnabled().ToString();
                if (!String.IsNullOrEmpty(EMSObj.Value))
                {
                    EMSObj.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    EMSObj.completionCode = Contracts.CompletionCode.Failure;
                    EMSObj.statusDescription = "Unable to get EMS settings";
                }
                response.ScanObjects.Add(EMSObj);

                // Get BootStatusPolicy
                ScanObject bootStatusPolicyObj = new ScanObject();
                bootStatusPolicyObj.Attribute = "Boot Status Policy";
                bootStatusPolicyObj.Value = Microsoft.GFS.WCS.ChassisManager.ScanDevice.GetBootStatusPolicy().ToString();
                if (!String.IsNullOrEmpty(bootStatusPolicyObj.Value))
                {
                    bootStatusPolicyObj.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    bootStatusPolicyObj.completionCode = Contracts.CompletionCode.Failure;
                    bootStatusPolicyObj.statusDescription = "Unable to get Boot Status Policy settings";
                }
                response.ScanObjects.Add(bootStatusPolicyObj);

                // Check if Chassis Manager SSL certificate is installed
                ScanObject chassisManagerSSLCert = new ScanObject();
                chassisManagerSSLCert.Attribute = "Chassis Manager SSl certificate Install Status";
                chassisManagerSSLCert.Value = Microsoft.GFS.WCS.ChassisManager.ScanDevice.IsChassisManagerSSLCertInstalled().ToString();
                if (!String.IsNullOrEmpty(chassisManagerSSLCert.Value))
                {
                    chassisManagerSSLCert.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    chassisManagerSSLCert.completionCode = Contracts.CompletionCode.Failure;
                    chassisManagerSSLCert.statusDescription = "Unable to get Chassis Manager SSL certificate install status";
                }
                response.ScanObjects.Add(chassisManagerSSLCert);

                // Get Chassis FRU data
                response.ChassisFRU = GetChassisAssetInfo(DeviceType.ChassisFruEeprom);

                // Get Chassis Network settings
                response.ChassisNetworkSettings = GetChassisNetworkProperties();

                // Get Chassis Manager service properties
                response.ChassisManagerServiceProperties = new ChassisManagerServiceProperties();
                response.ChassisManagerServiceProperties.ServiceVersion = GetServiceVersion();
                response.ChassisManagerServiceProperties.ServiceAppConfigSettings = ConfigLoaded.GetAllSettings();

                // If failed to fetch service version or app config settings
                if (response.ChassisManagerServiceProperties.ServiceVersion.completionCode == Contracts.CompletionCode.Success ||
                    response.ChassisManagerServiceProperties.ServiceAppConfigSettings.Any())
                {
                    response.ChassisManagerServiceProperties.completionCode = Contracts.CompletionCode.Success; 
                }
                else
                {
                    response.ChassisManagerServiceProperties.completionCode = Contracts.CompletionCode.Failure;
                    response.ChassisManagerServiceProperties.statusDescription = "Failed to fetch Chassis Manager service properties";
                }
                

                // Get Wcscli version
                response.WCSCLIVersion = Microsoft.GFS.WCS.ChassisManager.ScanDevice.GetWcsCliVersion();

                // If we reach here without exception, set completion code to success
                response.completionCode = Contracts.CompletionCode.Success;
            }
            catch (Exception ex)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                response.statusDescription = String.Format("ScanDevice failed with exception: {0}", ex.Message);
                Tracer.WriteError("ScanDevice failed with exception" + ex);
            }
            
            return response;
        }

        #region PSU ALERT

        /// <summary>
        /// Activate Deactivate PSU ALERT Action against the given blade
        /// </summary>
        public Contracts.BladeResponse SetBladePsuAlert(int bladeId, bool enableProchot, int action, bool removeCap)
        {
            Contracts.BladeResponse response = new Contracts.BladeResponse();
            Tracer.WriteUserLog("Invoked SetBladePsuAlert({0})", bladeId);
            Tracer.WriteInfo("Received SetBladePsuAlert({0})", bladeId);

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            if (!ConfigLoaded.PowerAlertDrivenPowerCapAPIsEnabled)
            {
                Tracer.WriteInfo("SetBladePsuAlert: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "PSU alert driven power cap commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladePsuAlert", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            BladeResponse activatePsuAlert = BladeActivateDeactivatePsuAlert(bladeId, enableProchot, action, removeCap);

            response.completionCode = activatePsuAlert.completionCode;
            response.statusDescription = response.completionCode.ToString();

            return response;
        }

        /// <summary>
        /// Activate Deactivate PSU ALERT Action against all blades.
        /// </summary>
        public Contracts.AllBladesResponse SetAllBladesPsuAlert(bool enableProchot, int action, bool removeCap)
        {
            Contracts.AllBladesResponse responses = new Contracts.AllBladesResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = String.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[ConfigLoaded.Population];

            Tracer.WriteUserLog("Invoked SetAllBladesPsuAlert");
            Tracer.WriteInfo("Received SetAllBladesPsuAlert");

            for (int loop = 0; loop < ConfigLoaded.Population; loop++)
            {
                responses.bladeResponseCollection.Add(SetBladePsuAlert(loop + 1, enableProchot, action, removeCap));
                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Get the Status of PSU ALERT for all blades
        /// </summary>
        public AllBladesPsuAlertResponse GetAllBladesPsuAlert()
        {
            AllBladesPsuAlertResponse responses = new AllBladesPsuAlertResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = String.Empty;
            responses.bladePsuAlertCollection = new List<BladePsuAlertResponse>(); 
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[ConfigLoaded.Population];
            Tracer.WriteUserLog("Invoked GetAllBladesPsuAlert");
            Tracer.WriteInfo("Received GetAllBladesPsuAlert");

            for (int loop = 0; loop < ConfigLoaded.Population; loop++)
            {
                responses.bladePsuAlertCollection.Add(GetBladePsuAlert(loop + 1));
                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladePsuAlertCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Get the Status of Blade PSU ALERT
        /// </summary>
        public BladePsuAlertResponse GetBladePsuAlert(int bladeId)
        {
            BladePsuAlertResponse response = new BladePsuAlertResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            Tracer.WriteUserLog("Invoked GetBladePsuAlert({0})", bladeId);
            Tracer.WriteInfo("Received GetBladePsuAlert({0})", bladeId);

            if (!ConfigLoaded.PowerAlertDrivenPowerCapAPIsEnabled)
            {
                Tracer.WriteInfo("GetBladePsuAlert: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "PSU alert driven power cap commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladePsuAlert", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            BladePsuAlertResponse psuAlert = GetPsuAlert(bladeId);

            response = psuAlert;
            response.completionCode =
                ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)psuAlert.completionCode);
            response.statusDescription = response.completionCode.ToString();

            return response;
        }

        /// <summary>
        /// Get PSU ALERT Default Power Cap
        /// </summary>
        public BladePsuAlertDpcResponse GetBladePsuAlertDefaultPowerCap(int bladeId)
        {
            BladePsuAlertDpcResponse response = new BladePsuAlertDpcResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            Tracer.WriteUserLog("Invoked GetBladePsuAlertDefaultPowerCap({0})", bladeId);
            Tracer.WriteInfo("Received GetBladePsuAlertDefaultPowerCap({0})", bladeId);

            if (!ConfigLoaded.PowerAlertDrivenPowerCapAPIsEnabled)
            {
                Tracer.WriteInfo("GetBladePsuAlertDefaultPowerCap: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "PSU alert driven power cap commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("GetBladePsuAlertDefaultPowerCap", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            BladePsuAlertDpcResponse dpcResponse = GetPsuAlertDefaultPowerCap(bladeId);

            // Assign response and map completion code
            response = dpcResponse;
            response.completionCode =
                ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)dpcResponse.completionCode);
            response.statusDescription = response.completionCode.ToString();

            return response;
        }

        /// <summary>
        /// Get All Blades PSU ALERT Default Power Cap
        /// </summary>
        public AllBladesPsuAlertDpcResponse GetAllBladesPsuAlertDefaultPowerCap()
        {
            AllBladesPsuAlertDpcResponse responses = new AllBladesPsuAlertDpcResponse();
            responses.completionCode = Contracts.CompletionCode.Unknown;
            responses.statusDescription = String.Empty;
            responses.bladeDpcResponseCollection = new List<BladePsuAlertDpcResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[ConfigLoaded.Population];
            Tracer.WriteUserLog("Invoked GetAllBladesPsuAlertDefaultPowerCap");
            Tracer.WriteInfo("Received GetAllBladsePsuAlertDefaultPowerCap for all blades");

            for (int loop = 0; loop < ConfigLoaded.Population; loop++)
            {
                responses.bladeDpcResponseCollection.Add(GetBladePsuAlertDefaultPowerCap(loop + 1));
                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeDpcResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        /// <summary>
        /// Get PSU ALERT Default Power Cap
        /// </summary>
        private BladePsuAlertDpcResponse GetPsuAlertDefaultPowerCap(int bladeId)
        {
            BladePsuAlertDpcResponse response = new BladePsuAlertDpcResponse();
            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            Tracer.WriteUserLog("Invoked GetBladePsuAlertDefaultPowerCap({0})", bladeId);
            Tracer.WriteInfo("Received GetBladePsuAlertDefaultPowerCap({0})", bladeId);

            // Issue IPMI command
            Ipmi.DefaultPowerLimit dpc = WcsBladeFacade.GetDefaultPowerCap((byte)bladeId);

            if (dpc.CompletionCode != (byte)Contracts.CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("GetBladePsuAlertDefaultPowerCap failed with completion code: {0:X}", dpc.CompletionCode);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.DefaultCapEnabled = dpc.DefaultCapEnabled;
                response.WaitTime = dpc.WaitTime;
                response.DefaultPowerCap = dpc.DefaultPowerCap;
            }

            response.completionCode =
                            ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)dpc.CompletionCode);
            response.statusDescription = response.completionCode.ToString();
            return response;
        }

        /// <summary>
        /// Set PSU ALERT Default Power Cap
        /// </summary>
        public Contracts.BladeResponse SetBladePsuAlertDefaultPowerCap(int bladeId, ushort defaultPowerCapInWatts, ushort waitTimeInMsecs)
        {
            Contracts.BladeResponse response = new Contracts.BladeResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;
            response.bladeNumber = bladeId;

            Tracer.WriteUserLog("Invoked SetBladePsuAlertDefaultPowerCap({0})", bladeId);
            Tracer.WriteInfo("Received SetBladePsuAlertDefaultPowerCap({0})", bladeId);

            if (!ConfigLoaded.PowerAlertDrivenPowerCapAPIsEnabled)
            {
                Tracer.WriteInfo("SetBladePsuAlertDefaultPowerCap: User requested API not enabled in app.config");
                response.completionCode = Contracts.CompletionCode.CommandNotValidAtThisTime;
                response.statusDescription = "PSU alert driven power cap commands are not enabled for this blade";
                return response;
            }

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladePsuAlertDefaultPowerCap", bladeId);
            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                response.completionCode = varResponse.completionCode;
                response.statusDescription = varResponse.statusDescription;
                return response;
            }

            BladeResponse setDpc = SetPsuAlertDefaultPowerCap(bladeId, defaultPowerCapInWatts, waitTimeInMsecs);

            response.completionCode = setDpc.completionCode;
            response.statusDescription = response.completionCode.ToString();

            return response;
        }

        /// <summary>
        /// Set PSU ALERT Default Power Cap for all blades
        /// </summary>
        public Contracts.AllBladesResponse SetAllBladesPsuAlertDefaultPowerCap(ushort defaultPowerCapInWatts, ushort waitTimeInMsecs)
        {
            Contracts.AllBladesResponse responses = new Contracts.AllBladesResponse();
            responses.statusDescription = String.Empty;
            responses.bladeResponseCollection = new List<BladeResponse>();
            Contracts.CompletionCode[] bladeInternalResponseCollection = new Contracts.CompletionCode[ConfigLoaded.Population];
            Tracer.WriteUserLog("Invoked SetAllBladesPsuAlertDefaultPowerCap");
            Tracer.WriteInfo("Received SetAllBladesPsuAlertDefaultPowerCap for all blades");

            for (int loop = 0; loop < ConfigLoaded.Population; loop++)
            {
                responses.bladeResponseCollection.Add(SetBladePsuAlertDefaultPowerCap(loop + 1, defaultPowerCapInWatts, waitTimeInMsecs));
                // Set the internal blade response to the blade completion code.
                bladeInternalResponseCollection[loop] = responses.bladeResponseCollection[loop].completionCode;
            }

            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            varResponse = ChassisManagerUtil.ValidateAllBladeResponse(bladeInternalResponseCollection);
            responses.completionCode = varResponse.completionCode;
            responses.statusDescription = varResponse.statusDescription;
            return responses;
        }

        #region PSU ALERT Support

        /// <summary>
        /// Set PSU ALERT Default Power Cap
        /// </summary>
        private Contracts.BladeResponse SetPsuAlertDefaultPowerCap(int bladeId, ushort defaultPowerCap, ushort waitTime)
        {
            Contracts.BladeResponse response = new Contracts.BladeResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.bladeNumber = bladeId;
            response.statusDescription = String.Empty;

            // Issue IPMI command
            bool setDpc = WcsBladeFacade.SetDefaultPowerLimit((byte)bladeId, defaultPowerCap, waitTime);

            if (!setDpc)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("SetBladePsuAlertDefaultPowerCap failed");
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.statusDescription = response.completionCode.ToString();
            }

            return response;
        }

        /// <summary>
        /// Get the Status of PSU ALERT
        /// </summary>
        private BladePsuAlertResponse GetPsuAlert(int bladeId)
        {
            BladePsuAlertResponse response = new BladePsuAlertResponse();

            response.bladeNumber = bladeId;
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.statusDescription = String.Empty;

            // Issue IPMI command
            Ipmi.PsuAlert psuAlert = WcsBladeFacade.GetPsuAlert((byte)bladeId);

            if (psuAlert.CompletionCode != (byte)Contracts.CompletionCode.Success)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("GetPsuAlert failed with completion code: {0:X}", psuAlert.CompletionCode);
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
                response.AutoProchotEnabled = psuAlert.AutoProchotEnabled;
                response.BmcProchotEnabled = psuAlert.BmcProchotEnabled;
                response.PsuAlertGpi = psuAlert.PsuAlertGpi;
            }

            response.completionCode =
                            ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)psuAlert.CompletionCode);
            response.statusDescription = response.completionCode.ToString();
            return response;

        }

        /// <summary>
        /// Activate Deactivate PSU ALERT Action
        /// </summary>
        private Contracts.BladeResponse BladeActivateDeactivatePsuAlert(int bladeId, bool enableProchot, int action, bool removeCap)
        {
            Contracts.BladeResponse response = new Contracts.BladeResponse();
            response.completionCode = Contracts.CompletionCode.Unknown;
            response.bladeNumber = bladeId;
            response.statusDescription = String.Empty;

            Ipmi.BmcPsuAlertAction bmcAction = Ipmi.BmcPsuAlertAction.NoAction;

            if (action == 0)
                bmcAction = Ipmi.BmcPsuAlertAction.NoAction;
            else if (action == 1)
                bmcAction = Ipmi.BmcPsuAlertAction.DpcOnly;
            else if (action == 2)
                bmcAction = Ipmi.BmcPsuAlertAction.ProcHotAndDpc;
            else
            {
                response.statusDescription = CompletionCode.ParameterOutOfRange.ToString();
                return response;
            }

            // Issue IPMI command
            bool activePsuAlert = WcsBladeFacade.ActivatePsuAlert((byte)bladeId, enableProchot, bmcAction, removeCap);

            if (!activePsuAlert)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Tracer.WriteError("ActivateDeactivatePsuAlert failed");
            }
            else
            {
                response.completionCode = Contracts.CompletionCode.Success;
            }

            response.statusDescription = response.completionCode.ToString();

            return response;
        }

        #endregion

        #endregion

        #endregion

        #region Assset Management

        /// <summary>
        /// Get the CM or PDB FRU areas information. The FRU area consists of 3 parts:
        /// (i) Chassis Info Area; (ii) Board Info Area; and (iii) Product Info Area.
        /// deviceType parameter uniquely identifies CM and PDB based on their
        /// unique FRU EEPROM addresses.
        /// </summary>
        /// <returns>AssetInfoResponse</returns>
        private ChassisAssetInfoResponse GetChassisAssetInfo(DeviceType deviceType)
        {
            ChassisAssetInfoResponse assetInfo = new ChassisAssetInfoResponse();

            ServiceVersionResponse version = GetServiceVersion();
            
            if (version.completionCode == Contracts.CompletionCode.Success)
            {
                assetInfo.serviceVersion = version.serviceVersion;
            }

            try
            {
                Ipmi.FruDevice fruDevice = ChassisState.CmFruData.ReadFru((DeviceType)deviceType);

                if (fruDevice.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Populate Chassis Info Area parameters
                    assetInfo.chassisAreaPartNumber = fruDevice.ChassisInfo.PartNumber.ToString();
                    assetInfo.chassisAreaSerialNumber = fruDevice.ChassisInfo.SerialNumber.ToString();

                    // Populate Board Info Area parameters
                    assetInfo.boardAreaManufacturerName = fruDevice.BoardInfo.Maufacturer.ToString();
                    assetInfo.boardAreaManufacturerDate = fruDevice.BoardInfo.MfgDateTime.ToString();
                    assetInfo.boardAreaProductName = fruDevice.BoardInfo.ProductName.ToString();
                    assetInfo.boardAreaSerialNumber = fruDevice.BoardInfo.SerialNumber.ToString();
                    assetInfo.boardAreaPartNumber = fruDevice.BoardInfo.ProductPartNumber.ToString();

                    // Populate Product Info Area parameters
                    assetInfo.productAreaManufactureName = fruDevice.ProductInfo.ManufacturerName.ToString();
                    assetInfo.productAreaProductName = fruDevice.ProductInfo.ProductName.ToString();
                    assetInfo.productAreaPartModelNumber = fruDevice.ProductInfo.PartModelNumber.ToString();
                    assetInfo.productAreaProductVersion = fruDevice.ProductInfo.ProductVersion.ToString();
                    assetInfo.productAreaSerialNumber = fruDevice.ProductInfo.SerialNumber.ToString();
                    assetInfo.productAreaAssetTag = fruDevice.ProductInfo.AssetTag.ToString();

                    // Populate Multi Record Info Area parameters
                    if (fruDevice.MultiRecordInfo != null)
                    {
                        // Populate Multi Record Info Area parameters
                        assetInfo.manufacturer = fruDevice.MultiRecordInfo.Manufacturer.ToString();

                        List<Ipmi.FruByteString> fields = fruDevice.MultiRecordInfo.Fields.ToList();
                        foreach (Ipmi.FruByteString field in fields)
                        {
                            assetInfo.multiRecordFields.Add(field.ToString());
                        }
                    }
                    else
                    {
                        Tracer.WriteError("GetChassisAssetInfo(). FRU Multi Record Area is empty.");
                    }

                    assetInfo.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                     Tracer.WriteError("GetChassisAssetInfo() FRU Read failed with completion code: {0:X}",
                            fruDevice.CompletionCode);
                    assetInfo.completionCode = Contracts.CompletionCode.Failure;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(
                    " GetChassisAssetInfo failed with the exception: " + ex.Message);
                if (assetInfo.completionCode != Contracts.CompletionCode.Success)
                {
                    assetInfo.completionCode = Contracts.CompletionCode.Failure;
                    assetInfo.statusDescription = String.Format("GetChassisAssetInfo() failed with unknown error");
                }
            }

            return assetInfo;
        }

        /// <summary>
        /// Get Chassis Manager FRU Areas Information
        /// </summary>
        public ChassisAssetInfoResponse GetChassisManagerAssetInfo()
        {
            Tracer.WriteInfo("Invoked GetChassisManagerAssetInfo()");
            return GetChassisAssetInfo(DeviceType.ChassisFruEeprom);
        }

        /// <summary>
        /// Get PDB FRU Areas Information
        /// </summary>
        /// <returns></returns>
        public ChassisAssetInfoResponse GetPdbAssetInfo()
        {
            Tracer.WriteInfo("Invoked GetPdbAssetInfo()");
            return GetChassisAssetInfo(DeviceType.PdbFruEeprom);
        }

        /// <summary>
        /// Gets the Blade FRU areas information. The FRU area consists of 3 parts:
        /// (i) Chassis Info Area; (ii) Board Info Area; and (iii) Product Info Area.
        /// </summary>
        /// <returns>AssetInfoResponse</returns>
        public BladeAssetInfoResponse GetBladeAssetInfo(int bladeId)
        {

            Tracer.WriteInfo("Invoked GetBladeAssetInfo() for blade Id {0}", bladeId);
            BladeAssetInfoResponse assetInfo = new BladeAssetInfoResponse();
            try
            {
                assetInfo.bladeNumber = bladeId;
                assetInfo.statusDescription = String.Empty;
                assetInfo.completionCode = Contracts.CompletionCode.Unknown;

                Contracts.ChassisResponse varResponse = ValidateRequest("GetBladeAssetInfo", bladeId);

                if (varResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    assetInfo.completionCode = varResponse.completionCode;
                    assetInfo.statusDescription = varResponse.statusDescription;
                    return assetInfo;
                }

                Ipmi.FruDevice fruData = WcsBladeFacade.GetFruDeviceInfo((byte)bladeId);
                if (fruData == null)
                {
                    Tracer.WriteError("GetBladeAssetInfo(). FruDevice fruData object is null for blade {0}. Aborting... ", bladeId);
                    assetInfo.completionCode = Contracts.CompletionCode.Failure;
                    assetInfo.statusDescription = assetInfo.completionCode.ToString();
                    return assetInfo;
                }

                if (fruData.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Populate Chassis Info Area parameters
                    assetInfo.chassisAreaPartNumber = fruData.ChassisInfo.PartNumber.ToString();
                    assetInfo.chassisAreaSerialNumber = fruData.ChassisInfo.SerialNumber.ToString();

                    // Populate Board Info Area parameters
                    assetInfo.boardAreaManufacturerName = fruData.BoardInfo.Maufacturer.ToString();
                    assetInfo.boardAreaManufacturerDate = fruData.BoardInfo.MfgDateTime.ToString();
                    assetInfo.boardAreaProductName = fruData.BoardInfo.ProductName.ToString();
                    assetInfo.boardAreaSerialNumber = fruData.BoardInfo.SerialNumber.ToString();
                    assetInfo.boardAreaPartNumber = fruData.BoardInfo.ProductPartNumber.ToString();

                    // Populate Product Info Area parameters
                    assetInfo.productAreaManufactureName = fruData.ProductInfo.ManufacturerName.ToString();
                    assetInfo.productAreaProductName = fruData.ProductInfo.ProductName.ToString();
                    assetInfo.productAreaPartModelNumber = fruData.ProductInfo.PartModelNumber.ToString();
                    assetInfo.productAreaProductVersion = fruData.ProductInfo.ProductVersion.ToString();
                    assetInfo.productAreaSerialNumber = fruData.ProductInfo.SerialNumber.ToString();
                    assetInfo.productAreaAssetTag = fruData.ProductInfo.AssetTag.ToString();

                    // Populate Multi Record Info Area parameters
                    if (fruData.MultiRecordInfo != null)
                    {
                        assetInfo.manufacturer = fruData.MultiRecordInfo.Manufacturer.ToString();

                        List<Ipmi.FruByteString> fields = fruData.MultiRecordInfo.Fields.ToList();
                        foreach (Ipmi.FruByteString field in fields)
                        {
                            assetInfo.multiRecordFields.Add(field.ToString());
                        }
                    }
                    else
                    {
                        Tracer.WriteError("GetBladeAssetInfo(). FRU Multi Record Area is empty for blade Id{0}", bladeId);
                    }

                    assetInfo.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    Tracer.WriteError("GetBladeAssetInfo(). Blade Fru Read failed with completion code: {0:X}", fruData.CompletionCode);
                    assetInfo.completionCode = Contracts.CompletionCode.Failure;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(
                    "GetBladeAssetInfo() failed with the exception: " + ex.Message);
                if (assetInfo.completionCode != Contracts.CompletionCode.Success)
                {
                    assetInfo.completionCode = Contracts.CompletionCode.Failure;
                    assetInfo.statusDescription = String.Format("GetBladeAssetInfo() failed with unknown error.");
                }
            }

            return assetInfo;
        }

        /// <summary>
        /// Set the Multi Record Area portion of CM or PDB FRU from user provided data
        /// </summary>
        /// <param name="recordData"></param>
        /// <returns>MultiRecordResponse</returns>
        private MultiRecordResponse SetChassisAssetInfo(string recordData, DeviceType deviceType)
        {
            // For both chassis manager and PDB, the deviceId is 1
            byte deviceId = 1;

            // Write to CM or PDB Multi Record Area of FRU
            MultiRecordResponse status = WriteMultiRecordFru(deviceId, recordData, deviceType);
            return status;
        }

        /// <summary>
        /// Set the Multi Record Area portion of Chassis Manager from user provided record data
        /// </summary>
        /// <param name="recordData"></param>
        /// <returns></returns>
        public MultiRecordResponse SetChassisManagerAssetInfo(string recordData)
        {
            Tracer.WriteInfo("Invoked SetChassisManagerAssetInfo()");
            return SetChassisAssetInfo(recordData, DeviceType.ChassisFruEeprom);
        }

        /// <summary>
        /// Set the Multi Record Area portion of PDB from user provided record data
        /// </summary>
        /// <param name="recordData"></param>
        /// <returns></returns>
        public MultiRecordResponse SetPdbAssetInfo(string recordData)
        {
            Tracer.WriteInfo("Invoked SetPdbAssetInfo()");
            return SetChassisAssetInfo(recordData, DeviceType.PdbFruEeprom);
        }

        /// <summary>
        /// Set the Multi Record Area portion of Blade FRU from user provided data
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="recordData"></param>
        /// <returns>MultiRecordResponse</returns>
        public BladeMultiRecordResponse SetBladeAssetInfo(int bladeId, string recordData)
        {
            BladeMultiRecordResponse setBladeAsset = new BladeMultiRecordResponse();

            Tracer.WriteInfo("Invoked SetBladeAssetInfo() for blade Id {0}", bladeId);

            Contracts.ChassisResponse varResponse = ValidateRequest("SetBladeAssetInfo", bladeId);
            setBladeAsset.bladeNumber = bladeId;
            setBladeAsset.statusDescription = String.Empty;
            setBladeAsset.completionCode = Contracts.CompletionCode.Unknown;

            if (varResponse.completionCode != Contracts.CompletionCode.Success)
            {
                setBladeAsset.completionCode = varResponse.completionCode;
                setBladeAsset.statusDescription = varResponse.statusDescription;
                return setBladeAsset;
            }

            // Write to Blade Multi Record Area of FRU
            MultiRecordResponse status = WriteMultiRecordFru(bladeId, recordData, DeviceType.Server);

            if (status.completionCode == (byte)Contracts.CompletionCode.Success)
            {
                setBladeAsset.completionCode = Contracts.CompletionCode.Success;
            }
            else
            {
                setBladeAsset.completionCode = status.completionCode;
                setBladeAsset.statusDescription = status.statusDescription;
            }

            return setBladeAsset;
        }

        /// <summary>
        /// Prepares and writes the Multi Record area portion of FRU for blade, CM and Pdb.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="recordData"></param>
        /// <param name="deviceType"></param>
        /// <returns></returns>
        private MultiRecordResponse WriteMultiRecordFru(int deviceId, string recordData, DeviceType deviceType)
        {
            Ipmi.FruCommonHeader commonHeader = null;
            Ipmi.FruMultiRecordInfo multiRecordInfo = null;

            CompletionCode completionCode = CompletionCode.UnspecifiedError;

            switch (deviceType)
            {
                case DeviceType.Server:
                    {
                        Ipmi.FruDevice fruData = WcsBladeFacade.GetFruDeviceInfo((byte)deviceId);
                        completionCode = (CompletionCode)fruData.CompletionCode;
                        commonHeader = fruData.CommonHeader;
                        multiRecordInfo = fruData.MultiRecordInfo;
                    }
                    break;
                case DeviceType.ChassisFruEeprom:
                case DeviceType.PdbFruEeprom:
                    {
                        Ipmi.FruDevice fruData = ChassisState.CmFruData.ReadFru(deviceType);
                        completionCode = (CompletionCode)fruData.CompletionCode;
                        commonHeader = fruData.CommonHeader;
                        multiRecordInfo = fruData.MultiRecordInfo;
                    }
                    break;
                default:
                    Tracer.WriteInfo("WriteMultiRecordFru() Unknown device type: {0} CompletionCode: {1}", 
                        deviceType.ToString(), completionCode.ToString());
                    break;
            }

            MultiRecordResponse setAsset = new MultiRecordResponse();

            try
            {
                if (completionCode == (byte)CompletionCode.Success)
                {
                    if (multiRecordInfo == null || commonHeader == null)
                    {
                        Tracer.WriteError("WriteMultiRecordFru(). multiRecordInfo and/or commonHeader is null. Aborting...");
                        setAsset.completionCode = Contracts.CompletionCode.Failure;
                        setAsset.statusDescription = setAsset.completionCode.ToString();
                        return setAsset;
                    }

                    // Abort if the starting offset of MultiRecordArea is 0, 
                    // suggesting that no Multi Record Area provision is made
                    if (commonHeader.MultiRecordAreaStartingOffset == 0)
                    {
                        Tracer.WriteError(@"WriteMultiRecordFru(). MultiRecordAreaStartingOffset is zero.
                                           Fru does not have Multi Record Area provisioned.");
                        setAsset.completionCode = Contracts.CompletionCode.WriteFruZeroStartingOffset;
                        setAsset.statusDescription = setAsset.completionCode.ToString();
                        return setAsset;
                    }

                    int idx = 0; // offset for different Multi Record Area fields (including header)
                    byte[] header = new byte[multiRecordInfo.HeaderSize];
                    byte maxWritesRemaining = 255;
                    bool writesRemainingReset = false; // to track if writes remaining has been set to the max value

                    // When starting offset of Multi Record Area is not 0, and both Record ID, Record Format is not set,
                    // it implies that the Multi Record Area is being written for the first time.
                    // The Multi Record Area format is as follows: [0] Record Type ID, [1] Record Format, 
                    // [2] Record Length, [3] Record Checksum, [4] Header Checksum, [5-7] Manufacturer ID, 
                    // [8] Language Code, [9] Writes Remaining, [10-N]: Record Data
                    // 0x1 is the header record format as per the specification.
                    if (commonHeader.MultiRecordAreaStartingOffset != 0 && 
                        multiRecordInfo.RecordTypeId != Ipmi.FruMultiRecordInfo.MultiRecordId &&
                        multiRecordInfo.RecordFormat != (byte)1)
                    {
                        // We are writing to Multi Record Area for the first time.
                        // Prepare and populate the Multi Record Header.
                        // We increment idx depending on the length of each header field. See IPMI FRU Specs. for
                        // details on the header format including length and offset of fields.

                        // Add Record Type Id at index [0]
                        header[idx++] = Ipmi.FruMultiRecordInfo.MultiRecordId;

                        // Add Record Format at index [1]
                        header[idx++] = 0x1;

                        // Add Writes Remaining at index [9]. This is custom defined maximum number of writes allowed.
                        header[idx + 7] = maxWritesRemaining; // writes remaining index is at index [9]
                        writesRemainingReset = true;
                    }
                    else // There is already an existing Multi Record Area portion of FRU
                    {
                        // Abort, if number of writes remaining is not greater than 0
                        if (!(multiRecordInfo.WritesRemaining > 0))
                        {
                            if (!ConfigLoaded.ResetMultiRecordFruWritesRemaining)
                            {
                                Tracer.WriteInfo("WriteMultiRecordFru(). Maximum allowable writes limit reached. Aborting...");
                                setAsset.completionCode = Contracts.CompletionCode.WriteFruZeroWritesRemaining;
                                setAsset.statusDescription = setAsset.completionCode.ToString();
                                return setAsset;
                            }
                            else
                            {
                                Tracer.WriteInfo("WriteMutliRecordFru(). Resetting the maximum writes remaining to default");
                                header[idx + 9] = maxWritesRemaining;
                                writesRemainingReset = true;
                            }
                        }

                        // Add Record Type Id at index [0]
                        header[idx++] = multiRecordInfo.RecordTypeId;

                        // Read from existing Record Format at index [1]
                        header[idx++] = multiRecordInfo.RecordFormat;
                    }

                    byte[] record = PrepareRecordData(recordData);
                    
                    if (record == null)
                    {
                        record = new byte[] { }; // create a dummy empty record byte array
                    }

                    // Add Record Length at index [2]
                    // This is for 8-bit ASCII + Latin 1.
                    int manufacturerIdLen = multiRecordInfo.ManufacturerIdLen; // Specification defined Manufacturer ID length

                    int recordLength = (multiRecordInfo.HeaderSize + record.Length);

                    // If the length is not multiple of 8, extend length to next multiple of 8
                    if ((recordLength) % 8 != 0)
                        recordLength += recordLength % 8;

                    // The length has to be multiple of 8 bytes
                    header[idx++] = (byte)(recordLength / 8);

                    // Add Record Checksum [3]
                    header[idx++] = Ipmi.IpmiSharedFunc.TwoComplementChecksum(0, record.Length, record);

                    // Add Header Checksum [4]
                    header[idx++] = Ipmi.IpmiSharedFunc.TwoComplementChecksum(0, header.Length, header);

                    // Add Manufacturer ID at indices [5-7]
                    // Manufacturer ID is pre-defined and set to Microsoft
                    byte[] manufacturerIdArray = ConfigLoaded.MultiRecordFruManufacturerId;

                    System.Buffer.BlockCopy(manufacturerIdArray, 0, header, idx, manufacturerIdArray.Length);

                    idx += manufacturerIdLen;

                    // Add Language Code at index [8]
                    // 0 represents 'English' language code
                    header[idx++] = 0x0;

                    // Decrement WritesRemaining at index [9] if WritesRemaining is not reset to max value
                    if (!writesRemainingReset && multiRecordInfo.WritesRemaining > 0)
                    {
                        header[idx++] = (byte)(multiRecordInfo.WritesRemaining - 1);
                    }

                    // Add Record Data to Multi Record Area
                    byte[] writePayLoad = new byte[header.Length + record.Length];

                    System.Buffer.BlockCopy(header, 0, writePayLoad, 0, header.Length);

                    System.Buffer.BlockCopy(record, 0, writePayLoad, header.Length, record.Length);
                    
                    // Maximum allowable bytes for the Multi Record Area portion (max. length allowed based on specification)
                    // It includes 10 bytes for the header, + 1 length byte, + 56 bytes for the max field size, 
                    // + 1 length byte, + 56 bytes for the second max field size, + 1 end of field byte. 
                    // This totals as 125 bytes. However, the MultiRecord Area length needs to be a multiple of 8 bytes. 
                    // So if maxBytes < 128, the entire payload size follows the limit. 
                    // Increasing higher than this number though won't change anything because the entire payload size
                    // to send to the FRU has already been created. Thus, we are keeping it at 126.
                    const int maxBytes = 128;
                    if (writePayLoad.Length < maxBytes)
                    {
                        ushort offset = commonHeader.MultiRecordAreaStartingOffset;

                        int allowedWrites = 16;
                        int writes = 0;
                        int writeIdx = 0;
                        int maxPayLoadSize = 16;
                        ushort writeOffset = offset;

                        try
                        {
                            // Since, Ipmi send/receive operates at granularity of 16 bytes chunk, we iterate over the 
                            // entire payload length to send the full payload.
                            while ((writeOffset < writePayLoad.Length + offset) && (allowedWrites > writes))
                            {
                                ushort diff = (ushort)(writePayLoad.Length - writeIdx);

                                if (diff < maxPayLoadSize)
                                    maxPayLoadSize = diff;

                                byte[] payLoad = new byte[maxPayLoadSize]; // writes to Ipmi are at the granularity of 16 bytes payload

                                System.Buffer.BlockCopy(writePayLoad, writeIdx, payLoad, 0, payLoad.Length);
                                if (deviceType == DeviceType.ChassisFruEeprom || deviceType == DeviceType.PdbFruEeprom)
                                {
                                    completionCode = ChassisState.CmFruData.WriteChassisFru(writeOffset,
                                        (ushort)payLoad.Length, payLoad, deviceType);
                                }
                                else if (deviceType == DeviceType.Server)
                                {

                                    byte rawCompletionCode = WcsBladeFacade.WriteFruDevice((byte)deviceId, writeOffset,
                                        payLoad).CompletionCode;

                                    // if write fru failed
                                    if (rawCompletionCode != (byte)CompletionCode.Success)
                                    {
                                        if (Enum.IsDefined(typeof(CompletionCode), rawCompletionCode))
                                        {
                                            // convert Ipmi completion code byte to known Chassis Manager
                                            // completion code enum.
                                            completionCode = (CompletionCode)rawCompletionCode;
                                        }
                                        else
                                        {
                                            // unable to convert Ipmi completion code "byte" to Chassis Manager
                                            // completion code enum.
                                            completionCode = CompletionCode.IpmiInvalidCommand;
                                        }
                                    }
                                    // else successful completion code
                                    else 
                                    {
                                        completionCode = CompletionCode.Success;
                                    }
                                }
                                else
                                {
                                    completionCode = CompletionCode.UnspecifiedError;
                                    Tracer.WriteError(string.Format(
                                        "WriteMultiRecordFru() Unknown Device Type. The device type: {0}", deviceType));
                                    break;
                                }

                                if (completionCode != CompletionCode.Success)
                                {
                                    Tracer.WriteInfo("WriteMultiRecordFru(). FRU Write unsuccessful, so breaking out of write while loop");
                                    break;
                                }

                                // Increment offset/index to account for the next 16 byte chunk of payload
                                writeOffset += (ushort)maxPayLoadSize;
                                writeIdx += maxPayLoadSize;
                                writes++;

                                if (writes > allowedWrites)
                                {
                                    Tracer.WriteInfo(@"WriteMultiRecordFru(). Number of FRU writes have been exhausted. 
                                                       The data is greater than writes by max allowed writes");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Tracer.WriteError("WriteMultiRecordFru(). Exception occcurred while writing to FRU: " + ex);
                        }

                        if (completionCode == CompletionCode.Success)
                        {
                            setAsset.completionCode = Contracts.CompletionCode.Success;
                        }
                        else
                        {
                            setAsset.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)completionCode);
                            setAsset.statusDescription = setAsset.completionCode.ToString();
                        }
                    }
                    else
                    {
                        Tracer.WriteError("WriteMultiRecordFru(). Maximum bytes reached and cannot write.");
                        setAsset.completionCode = Contracts.CompletionCode.WriteFruMaxRecordSizeReached;
                        setAsset.statusDescription = setAsset.completionCode.ToString();
                        return setAsset;
                    }
                }
                else
                {
                    Tracer.WriteInfo(string.Format("WriteMultiRecordFru(). Write FRU returned non-success completion code: {0}",
                        completionCode.ToString()));
                    setAsset.completionCode = Contracts.CompletionCode.Failure;
                    return setAsset;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("WriteMultiRecordFru(). Exception occurred in WriteMultiRecordFru method with details " + ex.Message);
                setAsset.completionCode = Contracts.CompletionCode.Failure;
                setAsset.statusDescription = setAsset.completionCode.ToString();
            }

            return setAsset;
        }

        /// <summary>
        /// Prepares the Record Data portion of Multi Record FRU from user
        /// provided payload information
        /// </summary>
        /// <param name="payLoad"></param>
        /// <returns></returns>
        private static byte[] PrepareRecordData(string payLoad)
        {
            const int maxFieldLen = 56;
            byte encoding = 0xC0;
            int idx = 0;

            try
            {
                // Populate Record Data
                string[] fields = payLoad.Split(',');
                int maxFields = 2; // We only allow maximum 128 bytes for Multi Record FRU, and each field is of max length 56 bytes
                int fieldsCount = fields.Count();
                if (fieldsCount > maxFields)
                {
                    Tracer.WriteInfo("PrepareRecordData(). Maximum 2 fields allowed, considering only first 2 fields, ignoring the rest");
                    fieldsCount = maxFields;
                }

                int count = 0;
                // Count the number of characters in all fields of Multi Area Records 
                for (int i = 0; i < fieldsCount; i++)
                {
                    count += fields[i].ToCharArray().Count();
                }

                // Check if count exceeds maximum length of all multirecord fields
                if (count > (maxFieldLen * maxFields))
                {
                    count = maxFieldLen * maxFields;
                }

                byte[] recordData = new byte[count + fieldsCount + 1]; // +1 is for the 0xC1 encoding at the end of records

                for (int i = 0; i < fieldsCount; i++)
                {
                    string field = fields[i];
                    // Only consider field with size not exceeding maxFieldLen
                    if (field.Length > maxFieldLen)
                    {
                        Tracer.WriteInfo("PrepareRecordData(). Field length is greater than allowed 56 bytes");

                        // Truncate the field to only keep the first 56 bytes
                        // Extract first 28 characters (equal to 28*2=56 bytes)
                        field = field.Substring(0, maxFieldLen);
                    }

                    // Add field length
                    recordData[idx++] = (byte)(field.Length | encoding);

                    byte[] fieldArray = HelperFunction.GetBytes(field);

                    System.Buffer.BlockCopy(fieldArray, 0, recordData, idx, fieldArray.Length);
                    idx += fieldArray.Length;
                }

                // Add C1h at end of Record Data
                recordData[idx++] = 0xC1;

                return recordData;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("Exception occurred in PrepareRecordData(): " + ex);
                return null;
            }
        }

        #endregion

        #region Blade Mezz Commands

        /// <summary>
        /// Get the Pass-through Mode for FPGA Mezzanine
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns>BladeMezzPassThroughModeResponse</returns>
        public BladeMezzPassThroughModeResponse GetBladeMezzPassThroughMode(int bladeId)
        {
            string apiName = "GetBladeMezzPassThroughMode";

            // Initialize Response
            Tracer.WriteInfo(string.Format("Received {0}(bladeId: {1})", apiName, bladeId));
            Tracer.WriteUserLog(string.Format("Invoked {0}(bladeId: {1})", apiName, bladeId));

            BladeMezzPassThroughModeResponse getPassThroughResponse = new BladeMezzPassThroughModeResponse();
            getPassThroughResponse.bladeNumber = bladeId;

            try
            {
                // Validate Request
                Contracts.ChassisResponse varResponse = ValidateRequest(apiName, bladeId);
                if (varResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    getPassThroughResponse.completionCode = varResponse.completionCode;
                    getPassThroughResponse.statusDescription = varResponse.statusDescription;
                    return getPassThroughResponse;
                }

                // Invoke GetMezzPassThroughMode to send MasterWriteRead IPMI command to blade BMC
                bool passThroughMode;
                getPassThroughResponse.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping(
                    (byte)WcsBladeMezz.GetMezzPassThroughMode(bladeId, out passThroughMode, apiName));

                // Set passThroughModeEnabled
                getPassThroughResponse.passThroughModeEnabled = passThroughMode;
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("Exception occured in {0}(): {1}", apiName, ex));
                getPassThroughResponse.completionCode = Contracts.CompletionCode.Failure;
                getPassThroughResponse.statusDescription = getPassThroughResponse.completionCode.ToString() + ": " + ex.Message;
            }

            return getPassThroughResponse;
        }

        /// <summary>
        /// Get the Blade Mezz FRU EEPROM
        /// </summary>
        /// <param name="bladeId">Target Blade Id</param>
        /// <returns>GetBladeMezzFru</returns>
        public BladeMessAssetInfoResponse GetBladeMezzAssetInfo(int bladeId)
        {
            string cmd = "GetBladeMezzAssetInfo";

            // Initialize Response
            Tracer.WriteInfo(string.Format("Received {0}(bladeId: {1})", cmd, bladeId));
            Tracer.WriteUserLog(string.Format("Invoked {0}(bladeId: {1})", cmd, bladeId));

            BladeMessAssetInfoResponse bladeMezz = new BladeMessAssetInfoResponse();
            bladeMezz.bladeNumber = bladeId;

            try
            {
                // Validate Request
                Contracts.ChassisResponse varResponse = ValidateRequest(cmd, bladeId);
                if (varResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    bladeMezz.completionCode = varResponse.completionCode;
                    bladeMezz.statusDescription = varResponse.statusDescription;
                    return bladeMezz;
                }

                // Invoke GetBladeMezzFru to send MasterWriteRead IPMI command to blade BMC
                Ipmi.FruDevice fruDevice = WcsBladeMezz.GetMezzFruEeprom(bladeId);

                if (fruDevice.CompletionCode == (byte)CompletionCode.Success)
                {
                    if (fruDevice.ProductInfo.ManufacturerName != null)
                        bladeMezz.productAreaManufactureName = fruDevice.ProductInfo.ManufacturerName.ToString();

                    if (fruDevice.ProductInfo.AssetTag != null)
                        bladeMezz.productAreaAssetTag = fruDevice.ProductInfo.AssetTag.ToString();

                    if (fruDevice.ProductInfo.PartModelNumber != null)
                        bladeMezz.productAreaPartModelNumber = fruDevice.ProductInfo.PartModelNumber.ToString();

                    if (fruDevice.ProductInfo.SerialNumber != null)
                        bladeMezz.productAreaSerialNumber = fruDevice.ProductInfo.SerialNumber.ToString();

                    if (fruDevice.ProductInfo.ProductVersion != null)
                        bladeMezz.productAreaProductVersion = fruDevice.ProductInfo.ProductVersion.ToString();

                    if (fruDevice.ProductInfo.ProductName != null)
                        bladeMezz.productAreaProductName = fruDevice.ProductInfo.ProductName.ToString();
                }
                
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("Exception occured in {0}(): {1}", cmd, ex));
                bladeMezz.completionCode = Contracts.CompletionCode.Failure;
                bladeMezz.statusDescription = bladeMezz.completionCode.ToString() + ": " + ex.Message;
            }

            return bladeMezz;
        }

        /// <summary>
        /// Set the Pass-through Mode for FPGA Mezzanine
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="passThroughModeEnabled"></param>
        /// <returns>BladeResponse</returns>
        public BladeResponse SetBladeMezzPassThroughMode(int bladeId, string passThroughModeEnabled)
        {
            string apiName = "SetBladeMezzPassThroughMode";
            bool passThroughMode;

            // Initialize Response
            Tracer.WriteInfo(string.Format("Received {0}(bladeId: {1})", apiName, bladeId));
            Tracer.WriteUserLog(string.Format("Invoked {0}(bladeId: {1})", apiName, bladeId));
            BladeResponse setPassThroughResponse = new BladeResponse();
            setPassThroughResponse.bladeNumber = bladeId;

            try
            {
                // Verify passThroughModeEnabled input is either "true" or "false"
                passThroughModeEnabled = passThroughModeEnabled != null ? // check if string is null
                    passThroughModeEnabled.ToLower() : string.Empty; // convert to lower case : set as empty string
                if (passThroughModeEnabled == "true")
                    passThroughMode = true;
                else if (passThroughModeEnabled == "false")
                    passThroughMode = false;
                else
                {
                    setPassThroughResponse.completionCode = Contracts.CompletionCode.Failure;
                    setPassThroughResponse.statusDescription = "Invalid Request";
                    return setPassThroughResponse;
                }
                
                // Validate Request
                Contracts.ChassisResponse varResponse = ValidateRequest(apiName, bladeId);
                if (varResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    setPassThroughResponse.completionCode = varResponse.completionCode;
                    setPassThroughResponse.statusDescription = varResponse.statusDescription;
                    return setPassThroughResponse;
                }

                // Invoke SetMezzPassThroughMode to send MasterWriteRead IPMI command to blade BMC
                // in order to set FPGA Mezzanine Pass-Through Mode
                setPassThroughResponse.completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping(
                    (byte)WcsBladeMezz.SetMezzPassThroughMode(bladeId, passThroughMode, apiName));

            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("Exception occured in {0}(): {1}", apiName, ex));
                setPassThroughResponse.completionCode = Contracts.CompletionCode.Failure;
                setPassThroughResponse.statusDescription = setPassThroughResponse.completionCode.ToString() + ": " + ex.Message;
            }

            return setPassThroughResponse;
        }

        #endregion

        #region Support Functions

        /// <summary>
        /// Standalone Command to get Power Consumption Info,  
        /// used with Data Center Power Monitoring integration
        /// </summary>
        private List<PsuInfo> GetPsuInfo()
        {
            // Create Response Collection
            List<PsuInfo> response = new List<PsuInfo>(ConfigLoaded.NumPsus);

            try
            {
                for (int psuId = 0; psuId < ConfigLoaded.NumPsus; psuId++)
                {
                    // Step 1: Create PsuInfo Response object
                    Contracts.PsuInfo psuInfo = new Contracts.PsuInfo();
                    psuInfo.id = (uint)(psuId + 1);

                    // Add object to list.
                    response.Add(psuInfo);

                    // If PSU firmware update in progress, do not read status
                    if (ChassisState.PsuFwUpdateInProgress[psuId])
                    {
                        response[psuId].completionCode = Contracts.CompletionCode.PSUFirmwareUpdateInProgress;
                        response[psuId].statusDescription = "PSU firmware update in progress";
                        continue;
                    }

                    lock (ChassisState.psuLock[psuId])
                    {
                        // subsequent commands are permitted to change upon failure.
                        response[psuId].completionCode = Contracts.CompletionCode.Success;

                        // Step 2:  Get Psu Power
                        PsuPowerPacket psuPower = ChassisState.Psu[psuId].GetPsuPower();
                        if (psuPower.CompletionCode != CompletionCode.Success)
                        {
                            Tracer.WriteWarning("GetPsuPower failed for psu: " + psuInfo.id);
                            response[psuId].powerOut = 0;
                            response[psuId].completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)psuPower.CompletionCode);
                            response[psuId].statusDescription = psuPower.CompletionCode.ToString();
                        }
                        else
                        {
                            response[psuId].powerOut = (uint)psuPower.PsuPower;
                        }

                        // Step 3: Get Psu Serial Number
                        PsuSerialNumberPacket serialNumberPacket = new PsuSerialNumberPacket();
                        serialNumberPacket = ChassisState.Psu[psuId].GetPsuSerialNumber();
                        if (serialNumberPacket.CompletionCode != CompletionCode.Success)
                        {
                            Tracer.WriteWarning("GetPsuSerialNumber failed for psu: " + psuInfo.id);
                            response[psuId].serialNumber = string.Empty;
                            response[psuId].completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)serialNumberPacket.CompletionCode);
                            response[psuId].statusDescription = serialNumberPacket.CompletionCode.ToString();
                        }
                        else
                        {
                            response[psuId].serialNumber = serialNumberPacket.SerialNumber;
                        }

                        // Step 4: Get Psu Status
                        PsuStatusPacket psuStatusPacket = new PsuStatusPacket();
                        psuStatusPacket = ChassisState.Psu[psuId].GetPsuStatus();
                        if (psuStatusPacket.CompletionCode != CompletionCode.Success)
                        {
                            Tracer.WriteWarning("GetPsuStatus failed for psu " + psuInfo.id);
                            response[psuId].state = PowerState.NA;
                            response[psuId].completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)psuStatusPacket.CompletionCode);
                            response[psuId].statusDescription = psuStatusPacket.CompletionCode.ToString();
                        }
                        else
                        {
                            if (psuStatusPacket.PsuStatus == (byte)Contracts.PowerState.OFF)
                            {
                                response[psuId].state = PowerState.OFF;
                            }
                            else if (psuStatusPacket.PsuStatus == (byte)Contracts.PowerState.ON)
                            {
                                response[psuId].state = PowerState.ON;
                            }
                            else
                            {
                                response[psuId].state = PowerState.NA;
                            }
                        }
                    } // lock...
                } // for...
            }
            catch (Exception ex)
            {
                Tracer.WriteError("GetPsuInfo failed with exception: {0}", ex);
            }

            return response;

        }

        /// <summary>
        /// Standalone Command to get Battery Info
        /// </summary>
        private List<BatteryInfo> GetBatteryInfo()
        {
            // Create Response Collection
            List<BatteryInfo> response = new List<BatteryInfo>(ConfigLoaded.NumBatteries);

            try
            {
                for (int batteryId = 0; batteryId < ConfigLoaded.NumBatteries; batteryId++)
                {
                    // Step 1: Create BatteryInfo Response object
                    Contracts.BatteryInfo batteryInfo = new Contracts.BatteryInfo();
                    batteryInfo.id = (byte)(batteryId + 1);

                    // Add object to list
                    response.Add(batteryInfo);

                    // If PSU firmware update in progress, do not read status
                    if (ChassisState.PsuFwUpdateInProgress[batteryId])
                    {
                        response[batteryId].completionCode = Contracts.CompletionCode.PSUFirmwareUpdateInProgress;
                        response[batteryId].statusDescription = "PSU firmware update in progress";
                        continue;
                    }

                    lock (ChassisState.psuLock[batteryId])
                    {
                        // Subsequent commands are permitted to change the completion upon failure
                        response[batteryId].completionCode = Contracts.CompletionCode.Success;

                        // Step 2: Get battery status
                        BatteryStatusPacket statusPacket = ChassisState.Psu[batteryId].GetBatteryStatus();
                        // Indicate battery presence regardless of completion status
                        response[batteryId].presence = statusPacket.Presence;

                        if (statusPacket.CompletionCode != CompletionCode.Success)
                        {
                            Tracer.WriteWarning("Get battery info failed for battery: " + batteryInfo.id);
                            response[batteryId].completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)statusPacket.CompletionCode);
                            response[batteryId].statusDescription = statusPacket.CompletionCode.ToString();
                        }
                        else
                        {
                            response[batteryId].batteryPowerOutput = statusPacket.BatteryPowerOutput;
                            response[batteryId].batteryChargeLevel = statusPacket.BatteryChargeLevel;
                            response[batteryId].faultDetected = statusPacket.FaultDetected;
                        }
                    } // lock...
                } // for...
            }
            catch (Exception ex)
            {
                Tracer.WriteError("GetBatteryInfo failed with exception: {0}", ex);
            }

            return response;
        }

        /// <summary>
        /// Gets Fan Infomation
        /// </summary>
        private List<FanInfo> GetFanInfo()
        {
            List<FanInfo> response = new List<FanInfo>(ConfigLoaded.NumFans);

            try
            {
                // Populate Fan Status and Fan Reading
                for (int fanId = 0; fanId < ConfigLoaded.NumFans; fanId++)
                {
                    FanInfo fanInfo = new FanInfo();
                    fanInfo.fanId = (fanId + 1);

                    response.Add(fanInfo);

                    if (!ConfigLoaded.EnableFan)
                    {
                        // no need to enumerate all fans.  escape the for loop and return.
                        response[fanId].completionCode = Contracts.CompletionCode.FanlessChassis;
                        break;
                    }
                    else
                    {
                        FanSpeedResponse fanSpeed = ChassisState.Fans[fanId].GetFanSpeed();

                        if (fanSpeed.CompletionCode != (byte)CompletionCode.Success)
                        {
                            Tracer.WriteWarning("Error getting fan speed on: FanId {0} CompletionCode: 0x{1:X2}",
                            (fanId + 1), fanSpeed.CompletionCode);

                            response[fanId].fanSpeed = 0;
                            response[fanId].isFanHealthy = false;
                            response[fanId].completionCode = ChassisManagerUtil.GetContractsCompletionCodeMapping((byte)fanSpeed.CompletionCode);
                            response[fanId].statusDescription = fanSpeed.CompletionCode.ToString();
                        }
                        else
                        {
                            response[fanId].completionCode = Contracts.CompletionCode.Success;

                            response[fanId].fanSpeed = fanSpeed.Rpm;

                            if (fanSpeed.Rpm > 0)
                                response[fanId].isFanHealthy = true;
                            else
                                response[fanId].isFanHealthy = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(" GetFanInfo failed with exception: {0}", ex);
            }

            return response;
        }

        /// <summary>
        /// Appends Fru for GetBladeHealth function.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="hwresp"></param>
        /// <param name="append"></param>
        private void AppendFruInfo(ref Contracts.BladeHealthResponse response, HardwareStatus hwresp, bool append)
        {
            // append fru info if required.
            if (append)
            {
                response.serialNumber = hwresp.SerialNumber;
                response.assetTag = hwresp.AssetTag;
                response.productType = hwresp.ProductType;
                response.hardwareVersion = hwresp.HardwareVersion;
            }
            else
            {
                response.serialNumber = string.Empty;
                response.assetTag = string.Empty;
                response.productType = string.Empty;
                response.hardwareVersion = string.Empty;
            }
        }

        /// <summary>
        /// Converts NicInfo into contracts object
        /// </summary>
        /// <param name="ipmiNicInfo"></param>
        /// <returns></returns>
        internal Contracts.NicInfo GetNicInfoObject(Ipmi.NicInfo ipmiNicInfo)
        {
            Contracts.NicInfo nicInfoObject = new Contracts.NicInfo();
            if (ipmiNicInfo.CompletionCode == (byte)CompletionCode.Success)
            {
                nicInfoObject.completionCode = Contracts.CompletionCode.Success;
            }
            // IpmiInvalidDataFieldInRequest is returned when a NIC that is not present in the system is requested.
            else if (ipmiNicInfo.CompletionCode == (byte)CompletionCode.IpmiInvalidDataFieldInRequest)
            {
                nicInfoObject.completionCode = Contracts.CompletionCode.Success;
                nicInfoObject.statusDescription = "Not Present";
            }
            else if (ipmiNicInfo.CompletionCode == (byte)Contracts.CompletionCode.FirmwareDecompressing)
            {
                nicInfoObject.completionCode = Contracts.CompletionCode.FirmwareDecompressing;
                nicInfoObject.statusDescription = "Blade Firmware still decompressing, cannot read data at this time";
            }
            // Else an unkown error occured.
            else
            {
                nicInfoObject.completionCode = Contracts.CompletionCode.Failure;
                nicInfoObject.statusDescription = Contracts.CompletionCode.Failure.ToString() + ": Internal error";
            }

            nicInfoObject.deviceId = ipmiNicInfo.DeviceId;
            nicInfoObject.macAddress = ipmiNicInfo.MacAddress;

            return nicInfoObject;
        }

        /// <summary>
        /// Gets the status string from Enum
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        internal string GetDiskInfoStatusString(byte status)
        {
            return Enum.GetName(typeof(DiskStatus), status);
        }

        /// <summary>
        /// Extract SEL Text Strings
        /// </summary>
        private EventLogData ExtractEventMessage(Ipmi.SystemEventLogMessage eventLog)
        {
            Ipmi.EventLogMsgType classification = eventLog.EventMessage.MessageType;

            string spacer = ConfigLoaded.EventLogStrSpacer +
                            ConfigLoaded.EventLogStrSeparator +
                            ConfigLoaded.EventLogStrSpacer;

            switch (classification)
            {
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.Threshold:
                    {
                        Ipmi.ThresholdEvent log = (Ipmi.ThresholdEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, log.EventTypeCode, log.ReadingOffset);

                        logData.EventMessage = string.Format(logData.EventMessage, log.TriggerReading, log.TriggerThreshold);

                        return logData;
                    }
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.Discrete:
                    {
                        Ipmi.DiscreteEvent log = (Ipmi.DiscreteEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, log.EventTypeCode, log.ReadingOffset);

                        logData.EventMessage = string.Format(logData.EventMessage, log.EventPayload[1], log.EventPayload[2]);

                        return logData;
                    }
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.SensorSpecific:
                    {
                        Ipmi.DiscreteEvent log = (Ipmi.DiscreteEvent)eventLog.EventMessage;

                        // Sensor Specific Event Types use the SensorType for indexing the TypeCode.
                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, log.SensorType,
                            log.ReadingOffset);

                        // create exceptions to logging for DIMM number lookup, as opposed to reporting DIMM index.
                        if (log.SensorType == 12 || (log.SensorType == 16 && log.ReadingOffset == 0))
                        {
                            // dimm number is unknown at first.
                            string dimmNumber = ConfigLoaded.Unknown;

                            // Get WCS DIMM number
                            if (log.SensorType == 12)
                                dimmNumber = ConfigLoaded.GetDimmNumber(log.EventPayload[2]);
                            else
                                dimmNumber = ConfigLoaded.GetDimmNumber(log.EventPayload[1]);

                            logData.EventMessage = string.Format(logData.EventMessage, dimmNumber);
                        }
                        else if (eventLog.SensorType == Ipmi.SensorType.CriticalInterrupt && (log.ReadingOffset == 0x07 || log.ReadingOffset == 0x08 || log.ReadingOffset == 0x0A))
                        {
                            // Correctable, uncorrectable and fatal bus errors
                            logData.EventMessage = string.Format(logData.EventMessage, (byte)(((byte)log.EventPayload[1] >> 3) & 0x1F),
                                                                                       ((byte)log.EventPayload[1] & 0x07), log.EventPayload[2]);
                        }
                        else
                        {
                            logData.EventMessage = string.Format(logData.EventMessage, log.EventPayload[1],
                                log.EventPayload[2]);
                        }

                        string extension = logData.GetExtension(log.EvtByte2Reading);

                        if (extension != string.Empty)
                        {
                            logData.EventMessage = (logData.EventMessage +
                                               spacer +
                                               extension);
                        }

                        return logData;
                    }
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.Oem:
                    {
                        Ipmi.OemEvent log = (Ipmi.OemEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, 0, 0);

                        return logData;
                    }
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.OemTimestamped:
                    {
                        Ipmi.OemTimeStampedEvent log = (Ipmi.OemTimeStampedEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, 0, 0);

                        // Format OEM Timestamped SEL Record
                        logData.EventMessage = string.Format(logData.EventMessage, string.Format("0x{0:X}", log.ManufacturerID),
                            Ipmi.IpmiSharedFunc.ByteArrayToHexString(log.OemDefined));

                        return logData;
                    }
                case Microsoft.GFS.WCS.ChassisManager.Ipmi.EventLogMsgType.OemNonTimeStamped:
                    {
                        Ipmi.OemNonTimeStampedEvent log = (Ipmi.OemNonTimeStampedEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, 0, 0);

                        // Format OEM Non-timestamped SEL Record
                        logData.EventMessage = string.Format(logData.EventMessage, Ipmi.IpmiSharedFunc.ByteArrayToHexString(log.OemDefined));

                        return logData;
                    }
                default:
                    {
                        Ipmi.UnknownEvent log = (Ipmi.UnknownEvent)eventLog.EventMessage;

                        EventLogData logData = ConfigLoaded.GetEventLogData(classification, 0, 0);

                        return logData;
                    }
            }
        }

        /// <summary>
        /// Adds string description to Chassis Response status description
        /// </summary>
        private Contracts.ChassisResponse ValidateRequest(string cmd, int bladeId)
        {
            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();

            // Check blade ID is valid
            if (ChassisManagerUtil.CheckBladeId(bladeId) == CompletionCode.InvalidBladeId)
            {
                Tracer.WriteWarning("{0} failed, Invalid blade Id {1}", cmd, bladeId);

                varResponse.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                varResponse.statusDescription = Contracts.CompletionCode.ParameterOutOfRange.ToString();
                return varResponse;
            }

            if (!FunctionValidityChecker.CheckBladeTypeValidity((byte)bladeId))
            {
                Tracer.WriteWarning("{0} failed, Invalid blade Type {1}", cmd, bladeId);

                varResponse.completionCode = Contracts.CompletionCode.CommandNotValidForBlade;
                varResponse.statusDescription = Contracts.CompletionCode.CommandNotValidForBlade.ToString();
                return varResponse;
            }

            return CheckBladeAndFirmwareState((byte)bladeId);
        }

        /// <summary>
        /// Check Blade Firmware State
        /// </summary>
        /// <param name="bladeId"></param>
        private Contracts.ChassisResponse CheckBladeAndFirmwareState(byte bladeId)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();

            BladePowerStatePacket powerState = FunctionValidityChecker.CheckBladeStateValidity(bladeId);

            if (powerState.PowerStatus == PowerState.OFF)
            {
                Tracer.WriteInfo("Blade: {0} Current Power State: {1}", bladeId, powerState.PowerStatus);

                response.completionCode = Contracts.CompletionCode.DevicePoweredOff;
                response.statusDescription = Contracts.CompletionCode.DevicePoweredOff.ToString();
                return response;
            }
            else if (powerState.PowerStatus == PowerState.ON)
            {
                if (powerState.DecompressionTime > 0)
                {
                    response.completionCode = Contracts.CompletionCode.FirmwareDecompressing;
                    response.statusDescription = string.Format("Decompression Time Remaining {0}", powerState.DecompressionTime);
                    return response;
                }
            }

            response.completionCode = Contracts.CompletionCode.Success;
            response.statusDescription = "Command Completed Successfully";
            return response;
        }

        #endregion
    }
}

