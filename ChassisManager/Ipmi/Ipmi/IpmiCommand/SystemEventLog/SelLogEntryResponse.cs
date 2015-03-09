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
    /// Represents the IPMI 'Get SEL Entry' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Storage, IpmiCommand.GetSelEntry)]
    internal class SelEntryResponse : IpmiResponse
    {
        /// <summary>
        /// Next SEL RecordID
        /// </summary> 
        private byte[] nextRecordId;

        /// <summary>
        /// SEL Entry Offste.
        /// </summary> 
        private byte[] selentry;
         
        /// <summary>
        /// Next SEL RecordID 
        /// </summary>       
        [IpmiMessageData(0,2)]
        public byte[] NextRecordId
        {
            get { return this.nextRecordId; }
            set { this.nextRecordId = value; }

        }
        
        /// <summary>
        /// SEL Record
        /// </summary>       
        [IpmiMessageData(2,16)]
        public byte[] SelEntry
        {
            get { return this.selentry; }
            set { this.selentry = value; }

        }
    }
}
