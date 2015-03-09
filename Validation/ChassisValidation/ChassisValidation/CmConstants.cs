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

using System.Collections.Generic;
namespace ChassisValidation
{
    internal static class CmConstants
    {
        /// <summary>
        /// Represents a server blade.
        /// </summary>
        public const string ServerBladeType = "Server";

        /// <summary>
        /// Represents a JBOD blade.
        /// </summary>
        public const string JbodBladeType = "Jbod";

        /// <summary>
        /// Represents a Empty blade.
        /// </summary>
        public const string EmptyBladeType = "";

        /// <summary>
        /// Represents a Empty blade.
        /// </summary>
        public const string UnKnownBladeType = "Unknown";

        /// <summary>
        /// Indicates a bla
        /// de is healthy.
        /// </summary>
        public const string HealthyBladeState = "ON";

        /// <summary>
        /// Number of blades in chassis.
        /// </summary>
        public const int Population = 24;

        /// <summary>
        /// Number of power switches (aka AC sockets) in chassis.
        /// </summary>
        public const int NumPowerSwitches = 3;

        /// <summary>
        /// Number of fans in chassis.
        /// </summary>
        public const int NumFans = 6;

        /// <summary>
        /// Number of PSUs in chassis.
        /// </summary>
        public const int NumPsus = 6;

        /// <summary>
        /// Number of batteries in chassis
        /// </summary>
        public const int NumBatteries = 0;

        /// <summary>
        /// Timeout value for a serial session in seconds.
        /// </summary>
        public const int SerialTimeoutSeconds = 300;

        /// <summary>
        /// Timeout value for an HTTP request in seconds.
        /// </summary>
        public const int RequestTimeoutSeconds = 300;

        /// <summary>
        /// The time duration needed to power on a blade in seconds.
        /// </summary>
        public const int BladePowerOnSeconds = 60;

        /// <summary>
        /// The time duration needed to power off a blade in seconds.
        /// </summary>
        public const int BladePowerOffSeconds = 30;

        /// <summary>
        /// Invalid blade: Not in between 1-24 range.
        /// </summary>
        public const int InvalidBladeId = 28;

        /// <summary>
        /// Invalid blade: Negative value.
        /// </summary>
        public const int InvalidNegtiveBladeId = -2;

        /// <summary>
        /// Default powerr state value for Empty blade
        /// </summary>
        public const string EmptyDefautState = "NA";

        /// <summary>
        /// Used by connection context id for Domain user
        /// </summary>
        public const int TestConnectionDomainUserId = 10;

        /// <summary>
        /// Used by connection context id for Local Id
        /// </summary>
        public const int TestConnectionLocalUserId = 20;

        /// <summary>
        /// OffTime for Active powercycle
        /// </summary>
        public const uint OffTime = 100;

        /// <summary>
        /// Negative off time for active power cycle
        /// </summary>
        public const int ngtveOffTime = -100;

        /// <summary>
        /// More than 255 seconds is invalid offltime.
        /// </summary>
        public const uint InvalidOffTime = 260;

        /// <summary>
        /// Offtime value 
        /// </summary>
        public const int OffTimeSec = 30;

        /// <summary>
        /// The time duration needed to start/stop Chassis Manager service
        /// </summary>
        public const int CmServiceStartStopSeconds = 30;

        /// <summary>
        /// Log count for after clear logs and readchassisLog
        /// </summary>
        public const int LogCount = 5;

        /// <summary>
        /// Number of log entries should be 50 
        /// </summary>
        public const int LogEntries = 50;

        /// <summary>
        /// Response content for ReceiveBladeSerialData
        /// </summary>
        public const string ResponseContent = "Channel management commands";

        /// <summary>
        /// Command string for SendBladeSerialData
        /// </summary>
        public const string SerialCommand = "?\r\n";

        /// <summary>
        /// PortId for PortSerialConsole
        /// </summary>
        public const int COMPortId = 1;

        public const int SecondCOMPortId = 2;

        /// <summary>
        /// Baud rate for port console
        /// </summary>
        public const int BaudRate = 115200;

        /// <summary>
        /// Sesssion Timeout in secs for PortSerialSession
        /// </summary>
        public const int SessionTimeoutInSecs = 60;

        /// <summary>
        /// No session time out for PortSerialsession
        /// </summary>
        public const int NosessionTimeoutInSecs = 0;

        /// <summary>
        /// DefaultsessionTimeoutInSecs for PortSerialSession
        /// </summary>
        public const int DefaultsessionTimeoutInSecs = 120;

        public const int DeviceTimeoutInMsecs = 0;
    }
}
