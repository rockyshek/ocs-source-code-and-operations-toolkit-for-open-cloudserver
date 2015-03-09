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
    using System.Threading;
    using System.Collections;
    using System.IO.Ports;
    using System.Diagnostics;
    using System.Collections.Generic;

    internal static class DeviceIdChecker
    {
        /// <summary>
        /// Validate the device ID. Device ID should start from 1 for all devices
        /// </summary>
        /// <param name="deviceType"></param>
        /// <param name="logicalDeviceId"></param>
        /// <returns></returns>
        static internal bool IsValidLogicalDeviceId(byte deviceType, byte logicalDeviceId)
        {
            bool bIsValid = false;
            switch (deviceType)
            {
                case (byte)DeviceType.Fan:
                    bIsValid = (logicalDeviceId > 0 && logicalDeviceId <= ConfigLoaded.NumFans);
                    break;
                case (byte)DeviceType.Psu:
                    bIsValid = (logicalDeviceId > 0 && logicalDeviceId <= ConfigLoaded.NumPsus);
                    break;
                case (byte)DeviceType.Power:
                    // Fall through. Blade power switch and servers 
                    // have the same ID range
                case (byte)DeviceType.Server:
                    bIsValid = (logicalDeviceId > 0 && logicalDeviceId <= ConfigLoaded.Population);
                    break;
                case (byte)DeviceType.PowerSwitch:
                    bIsValid = (logicalDeviceId > 0 && logicalDeviceId <= ConfigLoaded.NumPowerSwitches);
                    break;
                case (byte)DeviceType.SerialPortConsole:
                    // TODO: the number of the serial port devices should be specified 
                    // in the configuration file
                    bIsValid = (logicalDeviceId > 0 && logicalDeviceId <= ConfigLoaded.MaxSerialConsolePorts);
                    break;
                case (byte)DeviceType.WatchDogTimer:
                case (byte)DeviceType.FanCage:
                case (byte)DeviceType.StatusLed:
                case (byte)DeviceType.PsuAlertInput:
                case (byte)DeviceType.PsuAlertOutput:
                case (byte)DeviceType.RearAttentionLed:
                case (byte)DeviceType.ChassisFruEeprom:
                case (byte)DeviceType.PdbFruEeprom:
                    // The devices above do not have a device ID
                    bIsValid = true;
                    break;
                default:
                    Tracer.WriteError("Invalid logical device ID (type: {0}, id: {1}) in SendReceive", deviceType, logicalDeviceId);
                    break;
            }
            return bIsValid;
        }
    }
}
