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
    /// Represents the IPMI 'Get Device Id' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Application, IpmiCommand.GetDeviceId)]
    internal class GetDeviceIdResponse : IpmiResponse
    {

        /// <summary>
        /// device Id
        /// </summary>
        private byte deviceId;

        /// <summary>
        /// device revision number
        /// </summary>
        private byte deviceRevision;

        /// <summary>
        /// major firmware number
        /// </summary>
        private byte majorFirmware;

        /// <summary>
        /// minor firmware number
        /// </summary>
        private byte minorFirmware;

        /// <summary>
        /// Ipmi Version
        /// </summary>
        private byte ipmiVersion;

        /// <summary>
        /// additional device support
        /// </summary>
        private byte deviceSupport;

        /// <summary>
        /// Manufacturer Id
        /// </summary>
        private byte[] manufacturerId;

        /// <summary>
        /// Product Id
        /// </summary>
        private byte[] productId;

        /// <summary>
        /// Auxiliary Firmware Revision Information
        /// </summary>
        private byte[] auxFwVer;
       
        /// <summary>
        /// BMC Device Id.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte DeviceId
        {
            get { return this.deviceId; }
            set { this.deviceId = value; }  
        }

        /// <summary>
        /// Hardware revision number.
        /// </summary>       
        [IpmiMessageData(1)]
        public byte DeviceRevision
        {
            get { return this.deviceRevision; }
            set { this.deviceRevision = value; }
        }

        /// <summary>
        /// Major Revision. 7-bit.
        /// [7]   Device available: 0=normal operation, 1= device firmware update
        /// [6:0] Major Firmware Revision, binary encoded.
        /// </summary>       
        [IpmiMessageData(2)]
        public byte MajorFirmware
        {
            get { return this.majorFirmware; }
            set { this.majorFirmware = value; }
        }

        /// <summary>
        /// Minor Firmware Revision. BCD encoded.
        /// </summary>       
        [IpmiMessageData(3)]
        public byte MinorFirmware
        {
            get { return this.minorFirmware; }
            set { this.minorFirmware = value; }
        }

        /// <summary>
        /// Ipmi Version Device Support.
        /// </summary>       
        [IpmiMessageData(4)]
        public byte IpmiVersion
        {
            get { return this.ipmiVersion; }
            set { this.ipmiVersion = value; }
        }

        /// <summary>
        /// Additional Device Support.
        /// </summary>       
        [IpmiMessageData(5)]
        public byte DeviceSupport
        {
            get { return this.deviceSupport; }
            set { this.deviceSupport = value; }
        }

        /// <summary>
        /// IANA ManufacturerId:
        /// 0000h = unspecified. FFFFh = reserved. 
        /// </summary>       
        [IpmiMessageData(6,3)]
        public byte[] ManufacturerId
        {
            get { return this.manufacturerId; }
            set { this.manufacturerId = value; }
        }

        /// <summary>
        /// Additional Product Id:
        /// 0000h = unspecified. FFFFh = reserved. 
        /// </summary>       
        [IpmiMessageData(9, 2)]
        public byte[] ProductId
        {
            get { return this.productId; }
            set { this.productId = value; }
        }

        /// <summary>
        /// Auxiliary Firmware Revision Information
        /// Contains the second minor revision number for development 
        /// and test firmware.  
        /// </summary>       
        [IpmiMessageData(11, 4)]
        public byte[] AuxFwVer
        {
            get { return this.auxFwVer; }
            set { this.auxFwVer = value; }
        }
    }
}
