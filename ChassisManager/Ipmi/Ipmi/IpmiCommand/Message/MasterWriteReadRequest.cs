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
    /// Represents the IPMI 'Send Master Write-Read' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.MasterReadWrite)]
    internal class MasterWriteReadRequest : IpmiRequest
    {

        /// <summary>
        /// Channel to send the message.
        /// </summary>
        private readonly byte channel;

        /// <summary>
        /// Slave Id to send the message.
        /// </summary>
        private readonly byte slaveId;

        /// <summary>
        /// Read Count.  1 based. 0 = no bytes to read.  
        /// The maximum read count should be at least 34 bytes
        /// </summary>
        private readonly byte readCount;

        /// <summary>
        /// Data to write.
        /// </summary>
        private byte[] writeData;

        /// <summary>
        /// Initializes a new instance of the MasterWriteReadRequest class.
        /// BusType true = private bus
        /// BysType false = public bus
        /// Channel is zero based.
        /// </summary>
        internal MasterWriteReadRequest(byte channel, byte slaveId, byte readCount, byte[] writeData, bool busType = true)
        {           
            // bus type: 0 = public, 1 = private.
            if (busType)
            {
                // private bus
                this.channel = (byte)(this.channel | 0x01);

                // channel = [3:1] 
                this.channel = (byte)(this.channel | ((byte)(channel & 0x3) << 1));
            }
            else
            {
                // channel type is used.
                this.channel = (byte)(channel << 4);
            }

            this.slaveId = slaveId;
            this.readCount = readCount;
            this.writeData = writeData;
        }

        /// <summary>
        /// Channel to send the request message.
        /// </summary>
        [IpmiMessageData(0)]
        public byte Channel
        {
            get { return this.channel; }
        }

        /// <summary>
        /// Slave Id to send the message.
        /// </summary>
        [IpmiMessageData(1)]
        public byte SlaveId
        {
            get { return this.slaveId; }
        }

        /// <summary>
        /// Read Count.  1 based. 0 = no bytes to read.  
        /// The maximum read count should be at least 34 bytes
        /// </summary>
        [IpmiMessageData(2)]
        public byte ReadCount
        {
            get { return this.readCount; }
        }

        /// <summary>
        /// Data to write.
        /// </summary>
        [IpmiMessageData(3)]
        public byte[] WriteData
        {
            get { return this.writeData; }
        }

    }
}
