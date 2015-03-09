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

    /// <summary>
    /// Represents the IPMI 'Get Chassis Capabilities' chassis response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Chassis, IpmiCommand.GetChassisCapabilities)]
    internal class GetChassisCapabilitiesResponse : IpmiResponse
    {
        /// <summary>
        /// Capabilities Flags.
        /// </summary>
        private byte capabilities;

        /// <summary>
        /// Chassis FRU device address.
        /// </summary>
        private byte fruDeviceAddress;

        /// <summary>
        /// Chassis SDR device address.
        /// </summary>
        private byte sdrDeviceAddress;

        /// <summary>
        /// Chassis SEL device address.
        /// </summary>
        private byte selDeviceAddress;

        /// <summary>
        /// Chassis System Management device address.
        /// </summary>
        private byte systemManagementDeviceAddress;

        /// <summary>
        /// Chassis Bridge device address (optional).
        /// </summary>
        private byte bridgeDeviceAddress = 0x20;

        /// <summary>
        /// Gets and sets the Capabilities Flags.
        /// </summary>
        /// <value>Capabilities Flags.</value>
        [IpmiMessageData(0)]
        public byte Capabilities
        {
            get { return this.capabilities; }
            set { this.capabilities = value; }
        }

        /// <summary>
        /// Gets and sets the Chassis FRU device address.
        /// </summary>
        /// <value>Chassis FRU device address.</value>
        [IpmiMessageData(1)]
        public byte FruDeviceAddress
        {
            get { return this.fruDeviceAddress; }
            set { this.fruDeviceAddress = value; }
        }

        /// <summary>
        /// Gets and sets the Chassis SDR device address.
        /// </summary>
        /// <value>Chassis SDR device address.</value>
        [IpmiMessageData(2)]
        public byte SdrDeviceAddress
        {
            get { return sdrDeviceAddress; }
            set { this.sdrDeviceAddress = value; }
        }

        /// <summary>
        /// Gets and sets the Capabilities Flags.
        /// </summary>
        /// <value>Capabilities Flags.</value>
        [IpmiMessageData(3)]
        public byte SelDeviceAddress
        {
            get { return this.selDeviceAddress; }
            set { this.selDeviceAddress = value; }
        }

        /// <summary>
        /// Gets and sets the Chassis System Management device address.
        /// </summary>
        /// <value>Chassis System Management device address.</value>
        [IpmiMessageData(4)]
        public byte SystemManagementDeviceAddress
        {
            get { return this.systemManagementDeviceAddress; }
            set { this.systemManagementDeviceAddress = value; }
        }

        /// <summary>
        /// Gets and sets the Chassis Bridge device address (optional).
        /// </summary>
        /// <value>Chassis Bridge device address (optional).  Defaults to 0x20.</value>
        [IpmiMessageData(5)]
        public byte BridgeDeviceAddress
        {
            get { return this.bridgeDeviceAddress; }
            set { this.bridgeDeviceAddress = value; }
        }

        /// <summary>
        /// Indicates support for power interlock.
        /// </summary>
        /// <value>True if supported; else false.</value>
        internal bool SupportsPowerInterlock
        {
            get { return (this.Capabilities & 0x08) == 0x08; }
        }

        /// <summary>
        /// Indicates support for diagnostic interrupt (FP NMI).
        /// </summary>
        /// <value>True if supported; else false.</value>
        internal bool SupportsDiagnosticInterrupt
        {
            get { return (this.Capabilities & 0x04) == 0x04; }
        }

        /// <summary>
        /// Indicates support for front panel lockout.
        /// </summary>
        /// <value>True if supported; else false.</value>
        internal bool SupportsFrontPanelLockout
        {
            get { return (this.Capabilities & 0x02) == 0x02; }
        }

        /// <summary>
        /// Indicates support for a physical intrusion sensor.
        /// </summary>
        /// <value>True if supported; else false.</value>
        internal bool SupportsIntrusionSensor
        {
            get { return (this.Capabilities & 0x01) == 0x01; }
        }
    }
}
