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
    /// Represents the IPMI 'Get Memory Info Request' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.GetMemoryInfo)]
    internal class GetMemoryInfoRequest : IpmiRequest
    {

        /// <summary>
        /// Processor device index.  Default = 0x01
        /// </summary>
        private readonly byte _dimm;

        /// <summary>
        /// Get Memory Info Request.  Index 1 based.
        /// </summary>
        internal GetMemoryInfoRequest(byte dimm)
        { this._dimm = (dimm == 0 ? (byte)1 : dimm); }

        /// <summary>
        /// Get Memory Presence Info, 0x00 alters the return type.
        /// </summary>
        protected GetMemoryInfoRequest()
        { this._dimm = 0x00; }


        /// <summary>
        /// DIMM Number
        /// </summary>       
        [IpmiMessageData(0)]
        public byte DIMM
        {
            get { return this._dimm; }

        }
    }

    /// <summary>
    /// Represents the IPMI 'Get Memory Index Request' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.GetMemoryInfo)]
    internal class GetMemoryIndexRequest : GetMemoryInfoRequest
    {
        internal GetMemoryIndexRequest() : base()
        { 
        }
    }

}
