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
    /// Represents the IPMI 'Get Message Flags' application response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetMessageFlags)]
    internal class GetMessageFlagsResponse : IpmiResponse
    {

        /// <summary>
        /// Message Flags
        /// </summary>
        private byte flags;

        /// <summary>
        /// Channel Number
        /// </summary>
        [IpmiMessageData(0)]
        public byte Flags
        {
            get { return this.flags; }
            set { this.flags = value;}
        }

        /// <summary>
        /// Receive Message Available
        /// </summary>
        public byte MessageAvailable
        {
            get { return (byte)(this.flags & 0x01); }
        }

        /// <summary>
        /// Receive Buffer full
        /// </summary>
        public byte BufferFull
        {
            get { return (byte)((this.flags & 0x02) >> 1); }
        }

        /// <summary>
        /// Watch Dog pre-timeout interrupt
        /// </summary>
        public byte WatchDogTimeout
        {
            get { return (byte)((this.flags & 0x08) >> 3); }
        }

        /// <summary>
        /// OEM 1 Data Available
        /// </summary>
        public byte OEM1
        {
            get { return (byte)((this.flags & 0x20) >> 5); }
        }

        /// <summary>
        /// OEM 2 Data Available
        /// </summary>
        public byte OEM2
        {
            get { return (byte)((this.flags & 0x40) >> 6); }
        }

        /// <summary>
        /// OEM 3 Data Available
        /// </summary>
        public byte OEM3
        {
            get { return (byte)((this.flags & 0x80) >> 7); }
        }
    }
}
