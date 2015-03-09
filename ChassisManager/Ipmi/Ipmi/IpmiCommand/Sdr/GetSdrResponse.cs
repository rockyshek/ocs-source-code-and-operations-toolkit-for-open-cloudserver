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
    /// Represents the IPMI 'Get SDR' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Storage, IpmiCommand.GetSdr)]
    internal class GetSdrResponse : IpmiResponse
    {
        /// <summary>
        /// Next Record Id LS byte.
        /// </summary> 
        private byte recordIdLsByte;

        /// <summary>
        /// Next Record Id MS Byte.
        /// </summary> 
        private byte recordIdMsByte;

        /// <summary>
        /// Record ID.
        /// </summary> 
        private byte[] recordId;

        /// <summary>
        /// SDR Verions.
        /// </summary> 
        private byte sdrVersion;

        /// <summary>
        /// Record Type.
        /// </summary> 
        private byte recordType;

        /// <summary>
        /// Record Length.
        /// </summary> 
        private byte recordLength;

        /// <summary>
        /// Record Data.
        /// </summary> 
        private byte[] recordData;
         
        /// <summary>
        /// Next Record ID LS byte
        /// </summary>       
        [IpmiMessageData(0)]
        public byte RecordIdLsByte
        {
            get { return this.recordIdLsByte; }
            set { this.recordIdLsByte = value; }
        }

        /// <summary>
        /// Next Record ID MS byte
        /// </summary>       
        [IpmiMessageData(1)]
        public byte RecordIdMsByte
        {
            get { return this.recordIdMsByte; }
            set { this.recordIdMsByte = value; }
        }
        
        /// <summary>
        /// SDR Record Id
        /// </summary>       
        [IpmiMessageData(2,2)]
        public byte[] RecordId
        {
            get { return this.recordId; }
            set { this.recordId = value; }
        }

        /// <summary>
        /// Record Version
        /// </summary>       
        [IpmiMessageData(4)]
        public byte SdrVersion
        {
            get { return this.sdrVersion; }
            set { this.sdrVersion = value; }
        }
        
        /// <summary>
        /// Record Type
        /// </summary>       
        [IpmiMessageData(5)]
        public byte RecordType
        {
            get { return this.recordType; }
            set { this.recordType = value; }
        }
        
        /// <summary>
        /// Record Lenght
        /// </summary>       
        [IpmiMessageData(6)]
        public byte RecordLenght
        {
            get { return this.recordLength; }
            set { this.recordLength = value; }
        }
   
        /// <summary>
        /// Record Data
        /// </summary>       
        [IpmiMessageData(7)]
        public byte[] RecordData
        {
            get { return this.recordData; }
            set { this.recordData = value; }
        }
    }
}
