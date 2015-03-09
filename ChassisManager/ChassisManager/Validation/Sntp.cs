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
    using System.Text.RegularExpressions;
    using Microsoft.Win32;
    using System.ServiceProcess;

    static class Sntp
    {
        /// <summary>
        /// Locker object for atomic execution
        /// </summary>
        private static object locker = new object();

        /// <summary>
        /// Target Registry Value Names
        /// </summary>
        internal enum RegisteryKey
        {
            /// <summary>
            /// String NTP Server List
            /// </summary>
            NtpServer,

            /// <summary>
            /// String Type: NT5DS or NTP
            /// </summary>
            Type,

            /// <summary>
            /// NTP polling Interval:
            ///     DomainInterval   = "3,600";
            ///     StandardInterval = "604,800";
            /// </summary>
            SpecialPollInterval
        }

        /// <summary>
        /// Registry Key Static Values.
        /// </summary>
        private static class RegisteryKeyValues
        {
            /// <summary>
            /// Domain joined client NTP type
            /// </summary>
            public const string DomainNtpType = "NT5DS";

            /// <summary>
            /// Standard NTP Type
            /// </summary>
            public const string StandardNtpType = "NTP";

            /// <summary>
            /// NTP polling Interval for Domain joined clients
            /// </summary>
            public const string DomainInterval = "3600";

            /// <summary>
            /// NTP polling Interval for standalone clients
            /// </summary>
            public const string StandardInterval = "604800";
        }

        /// <summary>
        /// NTP Server Name
        /// </summary>
        internal class NtpServerName
        {

            /// <summary>
            /// Returns NTP Sever Name
            /// </summary>
            public string NtpServer
            {
                get;
                private set;
            }

            /// <summary>
            /// Class constructor requires NTP Server Ip Address
            /// </summary>
            /// <param name="priNtpAddress">Primary NTP Address</param>
            /// <param name="secNtpAddress">Secondary NTP address</param>
            internal NtpServerName(string primaryAddress, string secondaryAddress)
            {
                string ntpAddressList = string.Empty;

                ValidateNtpAddress(primaryAddress, ref ntpAddressList);

                ValidateNtpAddress(secondaryAddress, ref ntpAddressList);

                this.NtpServer = ntpAddressList.Trim();
            }

        }

        /// <summary>
        /// Registry String Definitions for W32Time
        /// </summary>
        private static class ControlStrings
        {

            internal static TimeSpan ServiceWaitTime = TimeSpan.FromSeconds(5);

            // Time Service Name
            internal const string W32TimeService = "W32Time";

            // Registry Key
            internal const string RestoreKey = "time.windows.com,0x9";

            // Registry Subkey for W32Time Parameters
            internal const string W32TimeKey = @"SYSTEM\CurrentControlSet\Services\W32Time\Parameters";

            // Registry Subkey for W32Time Special Poll Interval Parameters
            internal const string W32TimeIntervalKey = @"SYSTEM\CurrentControlSet\Services\W32Time\TimeProviders\NtpClient";

        }

        /// <summary>
        /// Validates Ip Address in a string and appends 0x1
        /// </summary>
        private static void ValidateNtpAddress(string ipAddress, ref string ntpAddressList)
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // filter IP addresses that do not start with zero
                Regex filter = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

                foreach (Match address in filter.Matches(ipAddress))
                {
                    if (!string.IsNullOrEmpty(address.Value))
                    {
                        // standalone should have 0x1, domain will override.
                        // NtpServer REF: http://technet.microsoft.com/en-us/library/cc773263(v=ws.10).aspx
                        ntpAddressList += ntpAddressList + address.Value + ",0x1 ";
                    }
                }
            }
        }

        /// <summary>
        /// Retrives NTP Servers
        /// </summary>
        internal static CompletionCode GetNtpServer(RegisteryKey registryKey, out string response)
        {
            lock (locker)
            {
                try
                {
                    RegistryKey regkey = Registry.LocalMachine.OpenSubKey(ControlStrings.W32TimeKey, RegistryKeyPermissionCheck.ReadSubTree,
                    System.Security.AccessControl.RegistryRights.ReadKey);
                    response = (string)regkey.GetValue(RegistryKeyName(registryKey));

                    // dispose of unmanaged object
                    if (regkey != null)
                        regkey.Dispose();
                }
                catch (Exception ex)
                {
                    Tracer.WriteError("Chassis Manager GetNtpServer failed with Exception: {0}", ex);
                    response = string.Empty;
                    return CompletionCode.UnspecifiedError;
                }

            }

            if (!string.IsNullOrEmpty(response))
            {
                // replace flag for clean output.
                response = response.Replace(",0x9", "");
            }
            
            return CompletionCode.Success;
        }

        /// <summary>
        /// Sets NTP Servers
        /// </summary>
        internal static CompletionCode SetNtpServer(NtpServerName ntpServer)
        {
            if (!string.IsNullOrEmpty(ntpServer.NtpServer))
            {
                return SetNtpServer(ntpServer.NtpServer);
            }
            else
            {
                // Ip Addresses were zero based or incorrect format.
                return CompletionCode.InvalidDataFieldInRequest;
            }
        }

        /// <summary>
        /// Restore the default registry key
        /// </summary>
        internal static CompletionCode RestoreNtpServerDefault()
        {
            return SetNtpServer(ControlStrings.RestoreKey);
        }

        /// <summary>
        /// Sets NTP Servers
        /// </summary>
        private static CompletionCode SetNtpServer(string ntpServer)
        {
            lock (locker)
            {
                try
                {
                    RegistryKey regkey = Registry.LocalMachine.OpenSubKey(ControlStrings.W32TimeKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                    System.Security.AccessControl.RegistryRights.WriteKey);
                    regkey.SetValue(RegistryKeyName(RegisteryKey.NtpServer), ntpServer, RegistryValueKind.String);
                    regkey.SetValue(RegistryKeyName(RegisteryKey.Type), RegisteryKeyValues.StandardNtpType, RegistryValueKind.String);
                    regkey.Close();

                    RegistryKey intervalKey = Registry.LocalMachine.OpenSubKey(ControlStrings.W32TimeIntervalKey, RegistryKeyPermissionCheck.ReadWriteSubTree,
                    System.Security.AccessControl.RegistryRights.WriteKey);

                    intervalKey.SetValue(RegistryKeyName(RegisteryKey.SpecialPollInterval), RegisteryKeyValues.StandardInterval, RegistryValueKind.DWord);
                    intervalKey.Close();

                    // dispose of unmanaged object
                    if (regkey != null)
                        regkey.Dispose();

                    // dispose of unmanaged object
                    if (intervalKey != null)
                        intervalKey.Dispose();

                    return RestartTimeService();

                }
                catch (Exception ex)
                {
                    Tracer.WriteError("Chassis Manager GetNtpServer failed with Exception: {0}", ex);
                    return CompletionCode.UnspecifiedError;
                }
            }
        }

        /// <summary>
        /// Restart the Windows Time Service
        /// </summary>
        private static CompletionCode RestartTimeService()
        {
            ServiceController sc = new ServiceController(ControlStrings.W32TimeService);

            if (sc != null)
            {
                // stop the time service
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, ControlStrings.ServiceWaitTime);
                }

                // start the time service
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, ControlStrings.ServiceWaitTime);

                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        if (sc != null)
                            sc.Dispose();

                        return CompletionCode.Success;
                    }
                }

                if (sc != null)
                    sc.Dispose();

                return CompletionCode.CannotExecuteRequestInvalidState;
            }
            else
            {
                Tracer.WriteError("Chassis Manager RestartTimeService unable to obtain Time Service handle");
                return CompletionCode.UnspecifiedError;
            }
        }

        /// <summary>
        /// Converts Enum value to string value
        /// </summary>
        private static string RegistryKeyName(RegisteryKey key)
        {
            return key.ToString();
        }

    }
}
