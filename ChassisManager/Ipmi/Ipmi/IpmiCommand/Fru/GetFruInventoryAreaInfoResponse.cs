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
    /// Represents the IPMI 'Get FRU Inventory Info' application request message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Storage, IpmiCommand.GetFruInventoryAreaInfo)]
    internal class GetFruInventoryAreaInfoResponse : IpmiResponse
    {

        private byte offSetMS;

        private byte offSetLS;

        private byte accessType;

        /// <summary>
        /// Gets offset to read.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte OffSetLS
        {
            get { return this.offSetLS; }
            set { this.offSetLS= value; }
        }

        /// <summary>
        /// Gets offset to read.
        /// </summary>       
        [IpmiMessageData(1)]
        public byte OffSetMS
        {
            get { return this.offSetMS; }
            set { this.offSetMS = value; }
        }

        /// <summary>
        /// Gets access type.
        /// </summary>
        /// <values>
        /// 0b = device is accessed by bytes
        /// 1b = device is accessed by word
        /// </values>
        [IpmiMessageData(3)]
        public byte AccessType
        {
            get { return this.accessType; }
            set { this.accessType = value; }
        }

    }
}
