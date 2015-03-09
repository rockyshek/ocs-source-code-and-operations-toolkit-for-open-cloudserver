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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{
    using System;

    /// <summary>
    /// Defines IPMI network function codes.
    /// </summary>
    [FlagsAttribute]
    internal enum IpmiFunctions
    {
        /// <summary>
        /// Chassis.
        /// </summary>
        Chassis = 0,

        /// <summary>
        /// Bridge.
        /// </summary>
        Bridge = 2,

        /// <summary>
        /// Sensor.
        /// </summary>
        Sensor = 4,

        /// <summary>
        /// Application.
        /// </summary>
        Application = 6,

        /// <summary>
        /// Firmware.
        /// </summary>
        Firmware = 8,

        /// <summary>
        /// Storage.
        /// </summary>
        Storage = 10,

        /// <summary>
        /// Transport.
        /// </summary>
        Transport = 12,

        /// <summary>
        /// DCMI.
        /// </summary>
        Dcgrp = 44,

        /// <summary>
        /// OEM
        /// </summary>
        Oem = 46,

        /// <summary>
        /// Oem/Group Vendor Specific
        /// </summary>
        OemGroup = 48,

        /// <summary>
        /// Oem Custom Group
        /// </summary>
        OemCustomGroup = 50,

        /// <summary>
        /// RMCP+ Session Setup.
        /// </summary>
        SessionSetup = 4096,
    }

    /// <summary>
    /// Defines IPMI message format based on transport type.
    /// </summary>
    internal enum IpmiTransport
    { 
        /// <summary>
        /// IPMI over Serial
        /// </summary>
        Serial = 0x00,

        /// <summary>
        /// IPMI over LAN
        /// </summary>
        Lan = 0x01,

        /// <summary>
        /// IPMI over KCS
        /// </summary>
        Wmi = 0x02,
    }
}
