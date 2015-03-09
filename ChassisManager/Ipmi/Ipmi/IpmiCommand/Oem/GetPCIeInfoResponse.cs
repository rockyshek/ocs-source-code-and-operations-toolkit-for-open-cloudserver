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
    /// Represents the IPMI 'Get PCIe Info Command' response message with a non-zero index.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetPCIeInfo)]
    class GetPCIeInfoResponse : IpmiResponse
    {
        /// <summary>
        /// Vendor Id
        /// </summary>
        private ushort _vendorId;

        /// <summary>
        /// Device Id
        /// </summary>
        private ushort _deviceId;

        /// <summary>
        /// Subsystem Vendor Id
        /// </summary>
        private ushort _subsystemVendorId;

        /// <summary>
        /// Subsystem Id
        /// </summary>
        private ushort _subSystemId;

        /// <summary>
        /// Vendor Id
        /// </summary>       
        [IpmiMessageData(0)]
        public ushort VendorId
        {
            get { return this._vendorId; }
            set { this._vendorId = value; }
        }

        /// <summary>
        /// Device Id
        /// </summary>       
        [IpmiMessageData(2)]
        public ushort DeviceId
        {
            get { return this._deviceId; }
            set { this._deviceId = value; }
        }

        /// <summary>
        /// Subsystem Vendor Id
        /// </summary>       
        [IpmiMessageData(4)]
        public ushort SubsystemVendorId
        {
            get { return this._subsystemVendorId; }
            set { this._subsystemVendorId = value; }
        }

        /// <summary>
        /// Subsystem ID
        /// </summary>       
        [IpmiMessageData(6)]
        public ushort SubSystemId
        {
            get { return this._subSystemId; }
            set { this._subSystemId = value; }
        }
    }

    /// <summary>
    /// Represents the IPMI 'Get PCIe Info Command' response message with zero index.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetPCIeInfo)]
    class GetPCIeInfoMapResponse : IpmiResponse
    {
        /// <summary>
        /// PCIe Presence bytes
        /// </summary>
        private byte _pciePresenceLsb;
        private byte _pciePresenceMsb;

        /// <summary>
        /// PCIe Presence Map LSB
        /// </summary>       
        [IpmiMessageData(0)]
        public byte PciePresenceLsb
        {
            get { return this._pciePresenceLsb; }
            set { this._pciePresenceLsb = value; }
        }

        /// <summary>
        /// PCIe Presence Map MSB
        /// </summary>       
        [IpmiMessageData(1)]
        public byte PciePresenceMsb
        {
            get { return this._pciePresenceMsb; }
            set { this._pciePresenceMsb = value; }
        }
    }
}
