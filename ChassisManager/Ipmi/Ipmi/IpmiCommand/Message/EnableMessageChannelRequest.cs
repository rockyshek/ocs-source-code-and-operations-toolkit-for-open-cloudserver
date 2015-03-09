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
    /// Represents the IPMI 'Enable Message Channel Receive Message' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.EnableMessageChannelReceive)]
    internal class EnableMessageChannelReceiveRequest : IpmiRequest
    {

        /// <summary>
        /// Channel to send the message.
        /// </summary>
        private readonly byte channel;

        /// <summary>
        /// Channel Enable/Disable State.
        /// </summary>
        private byte channelState;

        /// <summary>
        /// Initializes a new instance of the EnableMessageChannelReceiveRequest class.
        /// </summary>
        /// <param name="channel">Channel to enable/disable.</param>
        /// <param name="enableMessageReceive">Channel Enable/Disable State.</param>
        internal EnableMessageChannelReceiveRequest(byte channel, bool enableMessageReceive)
        {
            this.channel = (byte)(channel & 0x0f);

            if(enableMessageReceive)
                this.channelState = 0x01; // 01b = enable channel
            else
                this.channelState = 0x00; // 00b = disable channel
        }

        /// <summary>
        /// Initializes a new instance of the EnableMessageChannelReceiveRequest class.
        /// </summary>
        /// <param name="channel">Channel to enable/disable.</param>
        internal EnableMessageChannelReceiveRequest(byte channel)
        {
            this.channel = (byte)(channel & 0x0f);

            // 10b = get channel enable/disable state
            this.channelState = 0x02;
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
        /// Channel State
        /// </summary>
        [IpmiMessageData(1)]
        public byte ChannelState
        {
            get { return this.channelState; }
        }

    }
}
