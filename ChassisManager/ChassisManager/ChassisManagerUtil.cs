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
    using System.Net;
    using System.Text;
    using System.Xml;
    using System.ServiceModel;
    using Microsoft.GFS.WCS.Contracts;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
    using System.DirectoryServices.AccountManagement;

    /// <summary>
    /// This class defines Chassis Manager utility methods.
    /// </summary>
    internal static class ChassisManagerUtil
    {
        /// <summary>
        /// Mapping between Contracts BootType and Ipmi BootType
        /// Used in GetNextBoot and SetNextBoot APIs associated with blades 
        /// </summary>
        /// <param name="bootType"></param>
        /// <returns></returns>
        internal static Contracts.BladeBootType GetContractsBootType(Ipmi.BootType bootType)
        {
            if (bootType == Ipmi.BootType.NoOverride)
                return Contracts.BladeBootType.NoOverride;
            else if (bootType == Ipmi.BootType.ForcePxe)
                return Contracts.BladeBootType.ForcePxe;
            else if (bootType == Ipmi.BootType.ForceDefaultHdd)
                return Contracts.BladeBootType.ForceDefaultHdd;
            else if (bootType == Ipmi.BootType.ForceIntoBiosSetup)
                return Contracts.BladeBootType.ForceIntoBiosSetup;
            else if (bootType == Ipmi.BootType.ForceFloppyOrRemovable)
                return Contracts.BladeBootType.ForceFloppyOrRemovable;
            else
                return Contracts.BladeBootType.Unknown;
        }

        /// <summary>
        /// Mapping between Contracts BootType and Ipmi BootType
        /// Used in GetNextBoot and SetNextBoot APIs associated with blades 
        /// </summary>
        /// <param name="bootType"></param>
        /// <returns></returns>
        internal static Ipmi.BootType GetIpmiBootType(Contracts.BladeBootType bootType)
        {
            if (bootType == Contracts.BladeBootType.NoOverride)
                return Ipmi.BootType.NoOverride;
            else if (bootType == Contracts.BladeBootType.ForcePxe)
                return Ipmi.BootType.ForcePxe;
            else if (bootType == Contracts.BladeBootType.ForceDefaultHdd)
                return Ipmi.BootType.ForceDefaultHdd;
            else if (bootType == Contracts.BladeBootType.ForceIntoBiosSetup)
                return Ipmi.BootType.ForceIntoBiosSetup;
            else if (bootType == Contracts.BladeBootType.ForceFloppyOrRemovable)
                return Ipmi.BootType.ForceFloppyOrRemovable;
            else
                return Ipmi.BootType.Unknown;
        }

        /// <summary>
        /// Checking portId range
        /// </summary>
        /// <param name="portId"></param>
        /// <returns></returns>
        internal static byte CheckSerialConsolePortId(byte portId)
        {
            int portIndex = GetSerialConsolePortIndexFromId(portId);
            if (portIndex == -1 || portIndex >= ConfigLoaded.MaxSerialConsolePorts)
            {
                return (byte)CompletionCode.InvalidBladeId;
            }
            else
            {
                return (byte)CompletionCode.Success;
            }

        }


        /// <summary>
        /// Checking bladeId range
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns></returns>
        internal static CompletionCode CheckBladeId(int bladeId)
        {
            if (bladeId < 1 || bladeId > ConfigLoaded.Population)
            {
                return CompletionCode.InvalidBladeId;
            }
            else
            {
                return CompletionCode.Success;
            }

        }

        /// <summary>
        /// Checking psuId range
        /// </summary>
        /// <param name="psuId">The psu identifier.</param>
        /// <returns>
        /// CompletionCode.Success - psuId is within valid range.
        /// CompletionCode.ParameterOutOfRange - psuId is out of valid range.
        /// </returns>
        internal static CompletionCode CheckPsuId(int psuId)
        {
            if (psuId < 1 || psuId > ConfigLoaded.NumPsus)
            {
                return CompletionCode.ParameterOutOfRange;
            }
            else
            {
                return CompletionCode.Success;
            }
        }

        /// <summary>
        /// Internal method to check IP format 
        /// </summary>
        /// <param name="IpAddresses">IP address in string format</param>
        /// <returns>True if IP format is correct, else false</returns>
        internal static bool CheckIpFormat(string IpAddresses)
        {
            System.Net.IPAddress ipAdd;
            if (System.Net.IPAddress.TryParse(IpAddresses, out ipAdd))
            {
                if (ipAdd.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the given port number is valid
        /// </summary>
        /// <param name="Port">Port number</param>
        /// <returns>Bool true/false</returns>
        public static bool CheckPortValidity(uint portNum)
        {
            //Check port number range, if valid then return true
            if (portNum > 0 && portNum <= ConfigLoaded.NumPowerSwitches)
            {
                return true;
            }

            // Return false for everything else.
            return false;
        }

        /// <summary>
        /// Get IPV4 address for the client machine
        /// </summary>
        /// <param name="hostNameOrAddress">hostname or IP</param>
        /// <returns></returns>
        public static string GetIPv4Address(string hostNameOrAddress)
        {
            try
            {
                // Get the list of IP addresses for the specified host
                IPAddress[] aIPHostAddresses = Dns.GetHostAddresses(hostNameOrAddress);

                // First try to find a real IPV4 address in the list
                foreach (IPAddress ipHost in aIPHostAddresses)
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ipHost.ToString();

                // If that didn't work, try to lookup the IPV4 addresses for IPV6 addresses in the list
                foreach (IPAddress ipHost in aIPHostAddresses)
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        IPHostEntry ihe = Dns.GetHostEntry(ipHost);
                        foreach (IPAddress ipEntry in ihe.AddressList)
                            if (ipEntry.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                return ipEntry.ToString();
                    }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("failed to get client IP address: " + ex);
            }
            return null;
        }

        /// <summary>
        /// Check Blade Power limit is in acceptable range
        /// </summary>
        /// <param name="powerLimitInWatts"></param>
        /// <returns></returns>
        internal static bool CheckBladePowerLimit(double powerLimitInWatts)
        {
            bool success = false;

            if (powerLimitInWatts >= ConfigLoaded.MinPowerLimit && powerLimitInWatts <= ConfigLoaded.MaxPowerLimit)
            {
                success = true;
            }

            return success;

        }

        internal struct BaudRateStruct
        {
            public bool isValid;
            public BaudRate baudRate;
        };

        /// <summary>
        /// Checks for the baud rate parameter to be (i) non-zero positive integer
        /// and (ii) baud rate value exists in the enum BaudRate.
        /// More checks can be added as required.
        /// </summary>
        /// <param name="baudRate"></param>
        /// <returns></returns>
        internal static BaudRateStruct ValidateAndGetBaudRate(int baudRate)
        {
            BaudRateStruct retVal;
            retVal.isValid = false;
            retVal.baudRate = BaudRate.Rate_9600;

            BaudRate[] values = (BaudRate[])Enum.GetValues(typeof(BaudRate));
            List<int> baudRates = new List<int>();

            foreach (BaudRate value in values)
            {
                int index = value.ToString().IndexOf('_');
                baudRates.Add(Convert.ToInt32(value.ToString().Substring(index + 1)));
            }
            
            int idx = baudRates.IndexOf(baudRate);

            if (!baudRates.Contains(baudRate))
            {
                if (baudRate == 0)
                {
                    retVal.isValid = true;
                    retVal.baudRate = BaudRate.Rate_9600;
                }
            }
            else
            {
                retVal.isValid = true;
                if (baudRate > 0 && idx != -1)
                {
                    retVal.baudRate = values[idx];
                }
                else
                {
                    retVal.baudRate = BaudRate.Rate_9600;
                }
            }
            return retVal;
        }

        internal static int GetSerialConsolePortIndexFromId(int portId)
        {
            if (portId == 1)
                return 0;
            else if (portId == 2)
                return 1;
            else if (portId == 5)
                return 2;
            else if (portId == 6)
                return 3;
            else return -1;
        }

        internal static int GetSerialConsolePortIdFromIndex(int index)
        {
            if (index == 0)
                return 1;
            else if (index == 1)
                return 2;
            else if (index == 2)
                return 5;
            else if (index == 3)
                return 6;
            else return -1;
        }

        /// <summary>
        /// Create a new WCS group for given user role
        /// </summary>
        /// <param name="role">WCS user role</param>
        /// <param name="desc">Group description</param>
        internal static DirectoryEntry CreateNewGroup(Contracts.WCSSecurityRole role)
        {
            // Note: No additional try/catch block needed here as it is being invoked 
            // from inside a try/catch block.               
            DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            DirectoryEntry newGroup = AD.Children.Add(role.ToString(), "group");
            newGroup.Invoke("Put", new object[] { "Description", "WCS group" });
            newGroup.CommitChanges();
            return newGroup;
        }

        /// <summary>
        /// Check if given user exists in the group provided
        /// </summary>
        /// <param name="uname">User name</param>
        /// <param name="role">Group</param>
        /// <returns>bool value true/false</returns>
        internal static bool CheckIfUserExistsInGroup(string uname, WCSSecurityRole role)
        {
            DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            DirectorySearcher search = new DirectorySearcher("WinNT://" + Environment.MachineName + ",computer");
            bool userExistsInGrp = false;
            try
            {
                DirectoryEntry myEntry = AD.Children.Find(uname, "user");
                DirectoryEntry grp = AD.Children.Find(role.ToString(), "group");
                if (grp != null)
                {
                    if (grp.Properties["member"].Contains(myEntry))
                    {
                        Tracer.WriteInfo("user exists in group :" + role.ToString());
                        userExistsInGrp = true;
                    }
                }
            }
            // This exception is thrown when group does not exist
            catch (System.Runtime.InteropServices.COMException)
            {
                // Return false if group cannot be found
                userExistsInGrp = false;
            }

            return userExistsInGrp;
        }

        /// <summary>
        /// Remove user from given group. This is required
        /// when user role is changes
        /// </summary>
        /// <returns></returns>
        internal static void RemoveUserFromWCSSecurityGroups(string uname, WCSSecurityRole role)
        {
            Tracer.WriteInfo("RemoveUserFromWCSSecurityGroups: user: {0}, role: {1}", uname, role.ToString());
            DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            DirectoryEntry myEntry = AD.Children.Find(uname, "user");
            try
            {
                if (myEntry != null)
                {
                    DirectoryEntry grp = AD.Children.Find(role.ToString(), "group");
                    if (grp != null)
                    {
                        grp.Invoke("Remove", new object[] { myEntry.Path.ToString() });
                        grp.CommitChanges();
                        grp.Close();

                        Tracer.WriteInfo("RemoveUserFromWCSSecurityGroups: Removed uname: {0} from role: {1}", uname, role.ToString());
                    }
                }
            }
            catch (DirectoryServicesCOMException)
            {
                // Do nothing if group cannot be found/ if user is not a member of given group. 
                // If group does not exists we don't need to remove user from it.
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Do nothing if group cannot be found/ if user is not a member of given group. 
                // If group does not exists we don't need to remove user from it.
            }
            catch (Exception)
            {
                // Do nothing if group cannot be found/ if user is not a member of given group. 
                // If group does not exists we don't need to remove user from it.
            }
        }

        /// <summary>
        /// Find group with given name, if not exists create
        /// </summary>
        /// <param name="role">Group name</param>
        /// <returns>Group DirectorEntry</returns>
        internal static DirectoryEntry FindGroupIfNotExistsCreate(WCSSecurityRole role)
        {
            try
            {
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry grp = AD.Children.Find(role.ToString(), "group");
                if (grp == null)
                {
                    ChassisManagerUtil.CreateNewGroup(role);
                }

                return grp;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Tracer.WriteInfo(" Group: {0} not found , creating new", role.ToString());
                return (ChassisManagerUtil.CreateNewGroup(role));
            }
        }

        /// <summary>
        /// This method validates blade response for GetAll* APIs
        /// If even one blade response is not failure/timeout, return success.
        /// If all Blade response are failure/timeout, return failure
        /// </summary>
        /// <returns>Success if even one blade response is not failure/timeout, else retruns Failure</returns>
        public static Contracts.ChassisResponse ValidateAllBladeResponse(Contracts.CompletionCode[] responseCollection)
        {
            Contracts.ChassisResponse varResponse = new Contracts.ChassisResponse();
            if (responseCollection != null)
            {
                // Check blade response for all blades, if even one blade has a valid response (other than failure/timeout..)
                // then return true. Else return false after cheicng all blades.
                for (int loop = 0; loop < responseCollection.Length; loop++)
                {
                    if (CheckCompletionCode(responseCollection[loop]))
                    {
                        varResponse.statusDescription = "Fetched result successfully";
                        varResponse.completionCode = Contracts.CompletionCode.Success;
                        return varResponse;
                    }
                }
            }

            // None of the blades has a valid response, return false
            varResponse.statusDescription = "Failure in fetching result";
            varResponse.completionCode = Contracts.CompletionCode.Failure;
            return varResponse;
        }

        /// <summary>
        /// This checks for valid completion code for all functions
        /// </summary>
        /// <param name="responseCompletionCode"></param>
        /// <returns></returns>
        public static bool CheckCompletionCode(Contracts.CompletionCode responseCompletionCode)
        {
            if (responseCompletionCode == Contracts.CompletionCode.Success)
            {
                return true;
            }
            return false;

        }

        /// <summary>
        /// This function provides the mapping between internal completion code and the ones that Contracts exposes to the user
        /// </summary>
        /// <param name="functionCompletionCode"></param>
        /// <returns></returns>
        public static Contracts.CompletionCode GetContractsCompletionCodeMapping(byte functionCompletionCode)
        {
            // Success case
            if (functionCompletionCode == (byte)CompletionCode.Success)
            {
                return Contracts.CompletionCode.Success;
            }
            // Timeout case
            if (functionCompletionCode == (byte)CompletionCode.IpmiTimeout ||
                functionCompletionCode == (byte)CompletionCode.Timeout ||
                functionCompletionCode == (byte)CompletionCode.IpmiTimeOutHandShake)
            {
                return Contracts.CompletionCode.Timeout;
            }

            // Command Not support at present time
            if (functionCompletionCode == (byte)CompletionCode.CmdNotSupportAtPresentTime)
            {
                return Contracts.CompletionCode.CommandNotValidAtThisTime;
            }

            // Unknown case
            if (functionCompletionCode == (byte)CompletionCode.UnspecifiedError)
            {
                return Contracts.CompletionCode.Unknown;
            }

            // Return failure for any other case
            return Contracts.CompletionCode.Failure;
        }

    }

    /// <summary>
    /// Checks if the API call is valid for the particular blade - we need to build that validity map here
    /// </summary>
    static internal class FunctionValidityChecker
    {

        // Dictionary that maps API names to blade types that they are not valid for
        static Dictionary<string, BladeType> invalidBladeFunction = new Dictionary<string, BladeType>(StringComparer.OrdinalIgnoreCase);

        static FunctionValidityChecker()
        {
            //Initializing the mapping for commands and blade types for which they are not valid
            // 'All' commands
            invalidBladeFunction.Add("SetAllBladesAttentionLEDOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesAttentionLEDOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDefaultPowerStateOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDefaultPowerStateOff", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesDefaultPowerState", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesActivePowerCycle", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesState", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesPowerReading", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesPowerLimit", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesPowerLimit", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesPowerLimitOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesPowerLimitOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesPsuAlertDefaultPowerCap", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesPsuAlertDefaultPowerCap", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesPsuAlert", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesPsuAlert", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDatasafeOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDatasafeOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDatasafePowerOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDatasafePowerOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetAllBladesDatasafeActivePowerCycle", BladeType.Jbod);
            invalidBladeFunction.Add("GetAllBladesDatasafePowerState", BladeType.Jbod);
            
            // Individual functions
            invalidBladeFunction.Add("SetBladeAttentionLEDOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeAttentionLEDOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDefaultPowerStateOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDefaultPowerStateOff", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeDefaultPowerState", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeActivePowerCycle", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeState", BladeType.Jbod);
            invalidBladeFunction.Add("ReadBladeLogWithTimestamp", BladeType.Jbod);
            invalidBladeFunction.Add("ReadBladeLog", BladeType.Jbod);
            invalidBladeFunction.Add("ClearBladeLog", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladePowerReading", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladePowerLimit", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladePowerLimit", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladePowerLimitOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladePowerLimitOff", BladeType.Jbod);
            invalidBladeFunction.Add("GetNextBoot", BladeType.Jbod);
            invalidBladeFunction.Add("SetNextBoot", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeAssetInfo", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeAssetInfo", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladePsuAlertDefaultPowerCap", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladePsuAlertDefaultPowerCap", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladePsuAlert", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladePsuAlert", BladeType.Jbod);
            invalidBladeFunction.Add("GetPostCode", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDatasafeOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDatasafeOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDatasafePowerOff", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDatasafePowerOn", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeDatasafeActivePowerCycle", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeDatasafePowerState", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeMezzPassThroughMode", BladeType.Jbod);
            invalidBladeFunction.Add("SetBladeMezzPassThroughMode", BladeType.Jbod);
            invalidBladeFunction.Add("GetBladeMezzAssetInfo", BladeType.Jbod);
        }

        /// <summary>
        /// Checks the validity of the current blade id passed
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns></returns>
        public static bool CheckBladeTypeValidity(byte bladeId)
        {
            try
            {
                // If the blade type is unknown execute blade commands anyway
                // TODO: This will be modified after the bug (TFS Bug:7391) in blade state management is fixed
                // The current bug in state mgmt may potentially report a blade as unknown even when it is a valid blade
                if (ChassisState.GetBladeType(bladeId) == (byte) BladeType.Unknown)
                {
                    Tracer.WriteInfo("Blade{0}'s type is Unknown but sending the command to the blade anyway.", bladeId);
                    return true;
                }

                // Open the request message using an xml reader
                XmlReader xr = OperationContext.Current.IncomingMessageHeaders.GetReaderAtHeader(0);

                // Split the URL at the API name--Parameters junction indicated by the '?' character - taking the first string will ignore all parameters
                string[] urlSplit = xr.ReadElementContentAsString().Split('/');
                // Extract just the API name and rest of the URL, which will be the last item in the split using '/'
                string[] apiSplit = urlSplit[3].Split('?');

                BladeType val = BladeType.Unknown;
                invalidBladeFunction.TryGetValue(apiSplit[0], out val);
                
                // If the blade type does not support this function, return false, so we can send back useful info to user
                if ((byte)val == ChassisState.GetBladeType(bladeId))
                {
                    Tracer.WriteWarning("Command {0} not valid for Blade id {1}, Blade Type {2}",
                        apiSplit[0], bladeId, ChassisState.GetBladeTypeName((byte)val));

                    return false;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("Checking Blade Type validity encountered an exception" + ex);
                // We decide to go ahead and issue the command to the blade if the blade type check fails with exception,
                // This is done in order to not penalize a user command based on some failure in checking 
                // The command might fail eventually, but with an unhelpful error message
            }
            return true;
        }

        /// <summary>
        /// Checks the state of the current device/blade id passed, return true if the device is not power off
        /// </summary>
        public static BladePowerStatePacket CheckBladeStateValidity(byte bladeId)
        {
            return ChassisState.BladePower[bladeId - 1].GetCachedBladePowerState();
        }
    }

}
