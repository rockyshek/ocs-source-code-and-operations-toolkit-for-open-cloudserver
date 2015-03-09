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
    /// Represents the IPMI 'Get Disk Status Command for WCS JBOD' OEM request message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Oem, IpmiCommand.GetDiskStatus)]
    class GetDiskStatusResponse : IpmiResponse
    {
        private byte channel;

        private byte diskcount;

        private byte[] statusData;

        /// <summary>
        /// Disk Controller Channel
        /// </summary>       
        [IpmiMessageData(0)]
        public byte Channel
        {
            get { return this.channel; }
            set { this.channel = value; }
        }

        /// <summary>
        /// Disk Count on Controller
        /// </summary>       
        [IpmiMessageData(1)]
        public byte DiskCount
        {
            get { return this.diskcount; }
            set { this.diskcount = value; }
        }

        /// <summary>
        /// Disk Status Data
        /// Each byte = [7-6]:  Disk Status (0 = Normal, 1 = Failed, 2 = Error)
        ///             [5-0]:  Disk #: Number/Location Id
        /// </summary>       
        [IpmiMessageData(2)]
        public byte[] StatusData
        {
            get { return this.statusData; }
            set { this.statusData = value; }
        }
    }
}
