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
    /// Represents the IPMI 'Get Channel Authentication Capabilities' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetChannelCipherSuites, 3)]
    internal class GetChannelCipherSuitesRequest : IpmiRequest
    {
        /// <summary>
        /// Channel number (0x0E == current channel this request was issued on).
        /// </summary>
        private readonly byte channelNumber = 0x0E;

        /// <summary>
        /// Payload Type
        /// (0x00) IPMI
        /// (0x01) SOL
        /// </summary>
        private byte payloadType;

        /// <summary>
        /// List index (0x00 - 0x3F)
        /// 0x00 = first set of 16 
        /// </summary>
        private readonly byte cipherIndex = 0x00;

        /// <summary>
        /// Initializes a new instance of the GetChannelCipherSuitesRequest class.
        /// </summary>
        /// <param name="IpmiPayloadType">Ipmi Payload Type.</param>
        internal GetChannelCipherSuitesRequest(IpmiPayloadType payload)
        {
            this.payloadType = (byte)payload;
        }

        /// <summary>
        /// Gets the Channel number.
        /// </summary>
        /// <value>Channel number.</value>
        [IpmiMessageData(0)]
        public byte ChannelNumber
        {
            get { return this.channelNumber; }
        }

        /// <summary>
        /// Gets the Ipmi pay load type.
        /// </summary>
        /// <value>IpmiPayloadType</value>
        [IpmiMessageData(1)]
        public byte PayloadType
        {
            get { return this.payloadType; }
        }

        /// <summary>
        /// Cipher suite index (0x00 = first 16)
        /// </summary>
        /// <value>Cipher suite index</value>
        [IpmiMessageData(2)]
        public byte CipherIndex
        {
            get { return this.cipherIndex; }
        }
    }
}

