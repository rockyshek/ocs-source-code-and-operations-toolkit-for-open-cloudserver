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
    /// Represents the IPMI 'Get Sdr Repository Info' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Storage, IpmiCommand.GetSdrRepositoryInfo)]
    internal class GetSdrRepositoryInfoResponse : IpmiResponse
    {
        /// <summary>
        /// Sdr Version Number.
        /// </summary>  
        private byte sdrversion;

        /// <summary>
        /// Reservervation Id Ls (Least Significant byte).
        /// </summary>  
        private byte lsbyte;

        /// <summary>
        /// Reservervation Id MS (Most Significant byte).
        /// </summary>
        private byte msbyte;

        /// <summary>
        /// Sdr Free space.
        /// </summary>
        private byte[] freespace;

        /// <summary>
        /// Most Recent Entry TimeStamp.
        /// </summary>
        private byte[] lastadded;

        /// <summary>
        /// Most Recent Record Delete/Clear TimeStamp.
        /// </summary>
        private byte[] lastremoved;

        /// <summary>
        /// Gets Sdr Version Number.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte SdrVersion
        {
            get { return this.sdrversion; }
            set { this.sdrversion = value; }
        }

        /// <summary>
        /// Number of Sdr Records (Least Significant byte).
        /// </summary>       
        [IpmiMessageData(1)]
        public byte LSByte
        {
            get { return this.lsbyte; }
            set { this.lsbyte = value; }
        }

        /// <summary>
        /// Number of Sdr Records (Most Significant Byte).
        /// </summary>       
        [IpmiMessageData(2)]
        public byte MSByte
        {
            get { return this.msbyte; }
            set { this.msbyte = value; }

        }

        /// <summary>
        /// Free Space (Least Significant Byte).
        /// </summary> 
        [IpmiMessageData(3, 2)]
        public byte[] SdrFeeSpace
        {
            get { return this.freespace; }
            set { this.freespace = value; }

        }

        /// <summary>
        /// Most Recent Entry time.
        /// </summary> 
        [IpmiMessageData(5, 4)]
        public byte[] LastAdded
        {
            get { return this.lastadded; }
            set { this.lastadded = value; }

        }

        /// <summary>
        /// Last Entry Delete/Clear.
        /// </summary> 
        [IpmiMessageData(9, 4)]
        public byte[] LastRemoved
        {
            get { return this.lastremoved; }
            set { this.lastremoved = value; }

        }
    }
}
