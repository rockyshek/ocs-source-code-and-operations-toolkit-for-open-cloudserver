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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Represents the IPMI 'Get Channel Info' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetChannelInfo, 2)]
    internal class GetChannelInfoResponse : IpmiResponse
    {

        /// <summary>
        /// Channel number.
        /// </summary>
        private byte channelNumber;

        /// <summary>
        /// Channel Medium.
        /// </summary>
        private byte channelMedium;

        /// <summary>
        /// Channel Protocol
        /// </summary>
        private byte channelProtocol;

        /// <summary>
        /// Session Support
        /// </summary>
        private byte sessionSupport;

        /// <summary>
        /// Gets and sets the Actual Channel number.
        /// </summary>
        /// <value>Channel number.</value>
        [IpmiMessageData(0)]
        public byte ChannelNumber
        {
            get { return this.channelNumber; }
            set { this.channelNumber = (byte)(value & 0x0F); }
        }

        /// <summary>
        /// Channel Medium.
        /// </summary>
        [IpmiMessageData(1)]
        public byte ChannelMedium
        {
            get { return this.channelMedium; }
            set { this.channelMedium = value; }
        }

        /// <summary>
        /// Channel Protocol
        /// </summary>
        [IpmiMessageData(2)]
        public byte ChannelProtocol
        {
            get { return this.channelProtocol; }
            set { this.channelProtocol = value; }
        }

        /// <summary>
        /// Channel Protocol
        /// </summary>
        [IpmiMessageData(3)]
        public byte SessionSupport
        {
            get { return this.sessionSupport; }
            set { this.sessionSupport = value; }
        }

        /// <summary>
        /// Channel Session Support
        ///     00b = channel is session-less
        ///     01b = channel is single-session
        ///     10b = channel is multi-session
        ///     11b = channel is session-based
        /// </summary>
        internal byte ChannelSessionSupport
        {
            get { return (byte)(this.sessionSupport & 0xC0); }
        }

        /// <summary>
        /// Number of sessions
        /// </summary>
        internal byte NumberOfSessions
        {
            get { return (byte)(this.sessionSupport & 0x3F); }
        }
    }
}
