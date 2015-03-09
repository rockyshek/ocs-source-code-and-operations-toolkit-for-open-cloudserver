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
    using System.Text;
    using System.ServiceModel;
    using System.Security.Principal;
    using System.ServiceModel.Channels;
    using System.Xml;
    using System.ServiceModel.Web;
    using System.Net;

    // This class inherits from SeriveAuthorizationManager and its overridden checkaccess method will be called before any API execution
    public class MyServiceAuthorizationManager : ServiceAuthorizationManager
    {
        public override bool CheckAccess(OperationContext operationContext, ref Message message)
        {
            // Open the request message using an xml reader
            XmlReader xr = OperationContext.Current.IncomingMessageHeaders.GetReaderAtHeader(0);

                // Split the URL at the API name--Parameters junction indicated by the '?' character - taking the first string will ignore all parameters
                string[] urlSplit = xr.ReadElementContentAsString().Split('/');
                // Extract just the API name and rest of the URL, which will be the last item in the split using '/'
                string[] apiSplit = urlSplit[3].Split('?');
                // Logging the username and API name
                Tracer.WriteUserLog(apiSplit[0] + " request from user: " + operationContext.ServiceSecurityContext.WindowsIdentity.Name);

                // get client IP address
                MessageProperties prop = operationContext.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpoint =
                    prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                string ip = endpoint.Address;

                // Get IPV4 address
                ip = ChassisManagerUtil.GetIPv4Address(ip);
                if(!String.IsNullOrEmpty(ip))
                {
                    Tracer.WriteUserLog("Client IP address : " + ip); 
                }
        

            // If the most-privileged-role that this user belongs has access to this api, then allow access, otherwise deny access
            // Returning true will allow the user to execute the actual API function; Returning false will deny access to the user
            // TODO: May be we should send back a HTTP error code; will include this after shivi checks in her code
            if (ChassisManagerSecurity.GetCurrentUserMostPrivilegedRole() <= ChassisManagerSecurity.GetCurrentApiLeastPrivilegedRole(apiSplit[0]))
            {
                Tracer.WriteUserLog("CheckAccess: Authorized");
                return true;
            }
            else
            {
                Tracer.WriteUserLog("CheckAccess: NOT Authorized");
                return false;
            }
        }        
    }

      
    static internal class ChassisManagerSecurity
    {

        /// <summary>
        /// Dictionary that holds the mapping between the APIs and the least privileged role with access to that API
        /// That is, if an API is mapped to WcsCmOperator, it automatically authorize WcsCmAdmin as well but not WcsCmUser 
        /// OrdinalIgnoreCase is used to make the api names comparison case insensitive
        /// </summary>
        static Dictionary<string, authorizationRole> apiNameLeastPrivilegeRoleMap = new Dictionary<string, authorizationRole>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// WCS Chassis Manager User Roles
        /// </summary>
        public enum authorizationRole : int
        {
            WcsCmAdmin = 0,
            WcsCmOperator = 1,
            WcsCmUser = 2,
            WcsCmUnAuthorized = 3,
        }

        /// <summary>
        /// Class Constructor.
        /// </summary>
        static ChassisManagerSecurity()
        {
            //Initializing the API-UserRole authorization mapping
            apiNameLeastPrivilegeRoleMap.Add("GetChassisInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetChassisAttentionLEDOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetChassisAttentionLEDOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetChassisAttentionLEDStatus", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeAttentionLEDOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesAttentionLEDOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeAttentionLEDOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesAttentionLEDOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDefaultPowerStateOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDefaultPowerStateOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDefaultPowerStateOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDefaultPowerStateOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeDefaultPowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesDefaultPowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetPowerOn", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllPowerOn", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetPowerOff", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllPowerOff", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeOn", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesOn", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeOff", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesOff", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeActivePowerCycle", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesActivePowerCycle", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetPowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllPowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetACSocketPowerStateOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetACSocketPowerStateOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetACSocketPowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("StartBladeSerialSession", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("StopBladeSerialSession", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SendBladeSerialData", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("ReceiveBladeSerialData", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("StartSerialPortConsole", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("StopSerialPortConsole", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SendSerialPortData", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("ReceiveSerialPortData", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("ReadChassisLogWithTimestamp", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("ReadChassisLog", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("ClearChassisLog", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("ReadBladeLogWithTimestamp", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("ReadBladeLog", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("ClearBladeLog", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetBladePowerReading", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesPowerReading", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladePowerLimit", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesPowerLimit", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetBladePowerLimit", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesPowerLimit", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladePowerLimitOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesPowerLimitOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladePowerLimitOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesPowerLimitOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetChassisNetworkProperties", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("AddChassisControllerUser", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("RemoveChassisControllerUser", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("ChangeChassisControllerUserRole", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("ChangeChassisControllerUserPassword", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetChassisHealth", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeHealth", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetNextBoot", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetNextBoot", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetServiceVersion", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetMaxPwmRequirement", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("ResetPsu", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetChassisManagerAssetInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetPdbAssetInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeAssetInfo", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetChassisManagerAssetInfo", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetPdbAssetInfo", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeAssetInfo", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetBladePsuAlertDefaultPowerCap", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesPsuAlertDefaultPowerCap", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetBladePsuAlertDefaultPowerCap", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesPsuAlertDefaultPowerCap", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladePsuAlert", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesPsuAlert", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetBladePsuAlert", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesPsuAlert", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetPostCode", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDatasafeOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDatasafeOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDatasafeOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDatasafeOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDatasafePowerOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDatasafePowerOff", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDatasafePowerOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDatasafePowerOn", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeDatasafeActivePowerCycle", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("SetAllBladesDatasafeActivePowerCycle", authorizationRole.WcsCmOperator);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeDatasafePowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetAllBladesDatasafePowerState", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("UpdatePSUFirmware", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetPSUFirmwareStatus", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("GetBladeMezzPassThroughMode", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("SetBladeMezzPassThroughMode", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("GetSntpServer", authorizationRole.WcsCmUser);
            apiNameLeastPrivilegeRoleMap.Add("RestoreSntpServer", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("SetSntpServer", authorizationRole.WcsCmAdmin);
            apiNameLeastPrivilegeRoleMap.Add("ScanDevice", authorizationRole.WcsCmAdmin);

            apiNameLeastPrivilegeRoleMap.Add("GetBladeMezzAssetInfo", authorizationRole.WcsCmUser);
        }

        /// <summary>
        /// Returns the least privileged role with access to this API 
        /// </summary>
        static internal authorizationRole GetCurrentApiLeastPrivilegedRole(string apiName)
        {
            try
            {
                authorizationRole val = authorizationRole.WcsCmUnAuthorized;
                // If api name do not map to any of the mapped roles, then by default make it accessible only by WcsCmAdmin role 
                if (!apiNameLeastPrivilegeRoleMap.TryGetValue(apiName, out val))
                {
                    Tracer.WriteWarning("GetCurrentApiLeastPrivilegedRole: There is no role mapping for the api " + apiName);
                    return authorizationRole.WcsCmAdmin;
                }
                Tracer.WriteInfo("Requested API's minimum privilege requirement: " + val); 
                return val;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("Api-to-Role Mapping Exception was thrown: " + ex);
                // Return as UnAuthorized if there is an exception, then by default make this API accessible only by WcsCmAdmin role 
                Tracer.WriteInfo("Requested API's minimum privilege requirement (after exception):  " + authorizationRole.WcsCmAdmin);
                return authorizationRole.WcsCmAdmin;
            }
        }
        
        /// <summary>
        /// Returns the most privileged role this user belongs too
        /// </summary>
        static internal authorizationRole GetCurrentUserMostPrivilegedRole()
        {
            try
            {
                ServiceSecurityContext context = OperationContext.Current.ServiceSecurityContext;
                WindowsIdentity windowsIdentity = context.WindowsIdentity;
                var principal = new WindowsPrincipal(windowsIdentity);
                
                // Extract domain + role names to check for access privilege
                // The first item before '\' is the domain name - extract it
                string[] usernameSplit = windowsIdentity.Name.Split('\\');
                // Apend role names after the domain name
                string wcscmadminRole = usernameSplit[0] + "\\" + "WcsCmAdmin";
                string wcscmoperatorRole = usernameSplit[0] + "\\" + "WcsCmOperator";
                string wcscmuserRole = usernameSplit[0] + "\\" + "WcsCmUser";

                if (principal.IsInRole("Administrators"))
                {
                    Tracer.WriteUserLog("User({0}) belongs to Administrators group and hence belongs to WcsCmAdmin privilege role", windowsIdentity.Name);
                    return authorizationRole.WcsCmAdmin;
                }

                // Is user in local WcsCmAdmin group or domain's WcsCmAdmin group?
                if (principal.IsInRole("WcsCmAdmin") || principal.IsInRole(wcscmadminRole))
                {
                    Tracer.WriteUserLog("User({0}) belongs to WcsCmAdmin privilege role", windowsIdentity.Name);
                    return authorizationRole.WcsCmAdmin;
                }

                // Is user in local WcsCmOperator group or domain's WcsCmOperator group?
                if (principal.IsInRole("WcsCmOperator") || principal.IsInRole(wcscmoperatorRole))
                {
                    Tracer.WriteUserLog("User({0}) belongs to WcsCmOperator privilege role", windowsIdentity.Name);
                    return authorizationRole.WcsCmOperator;
                }

                // Is user in local WcsCmUser group or domain's WcsCmUser group?
                if (principal.IsInRole("WcsCmUser") || principal.IsInRole(wcscmuserRole))
                {
                    Tracer.WriteUserLog("User({0}) belongs to WcsCmUser privilege role", windowsIdentity.Name);
                    return authorizationRole.WcsCmUser;
                }
                // User not mapped to any standard roles
                Tracer.WriteWarning("GetCurrentUserMostPrivilegedRole: Current user({0}) not mapped to the standard WCS roles", windowsIdentity.Name);
                Tracer.WriteUserLog("GetCurrentUserMostPrivilegedRole: Current user({0}) not mapped to the standard WCS roles", windowsIdentity.Name);
            }
            catch (Exception ex)
            {
                Tracer.WriteError("User Authorization check exception  was thrown: " + ex);
            }
            
            // Return as unauthorized if the user do not belong to any of the category or if there is an exception
            return authorizationRole.WcsCmUnAuthorized;
        }
    }
}
