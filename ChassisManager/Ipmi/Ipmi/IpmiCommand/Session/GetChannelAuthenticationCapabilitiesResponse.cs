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
    /// Represents the IPMI 'Get Channel Authentication Capabilities' application response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetChannelAuthenticationCapabilities)]
    internal class GetChannelAuthenticationCapabilitiesResponse : IpmiResponse
    {
        /// <summary>
        /// Channel number.
        /// </summary>
        private byte channelNumber;

        /// <summary>
        /// Authentication Type Support (first byte).
        /// </summary>
        private byte authenticationTypeSupport1;

        /// <summary>
        /// Authentication Type Support (second byte).
        /// </summary>
        private byte authenticationTypeSupport2;

        /// <summary>
        /// Authentication Type Support (third byte).
        /// </summary>
        private byte extendedCapabilities;

        /// <summary>
        /// OEM Id.
        /// </summary>
        private byte[] oemId;

        /// <summary>
        /// OEM Data.
        /// </summary>
        private byte oemData;

        /// <summary>
        /// Gets and sets the Channel number.
        /// </summary>
        /// <value>Channel number.</value>
        [IpmiMessageData(0)]
        public byte ChannelNumber
        {
            get { return this.channelNumber; }
            set { this.channelNumber = value; }
        }

        /// <summary>
        /// Gets and sets the Authentication Type Support (first byte).
        /// </summary>
        /// <value>Authentication Type Support (first byte).</value>
        [IpmiMessageData(1)]
        public byte AuthenticationTypeSupport1
        {
            get { return this.authenticationTypeSupport1; }
            set { this.authenticationTypeSupport1 = value; }
        }

        /// <summary>
        /// Gets and sets the Authentication Type Support (second byte).
        /// </summary>
        /// <value>Authentication Type Support (second byte).</value>
        [IpmiMessageData(2)]
        public byte AuthenticationTypeSupport2
        {
            get { return this.authenticationTypeSupport2; }
            set { this.authenticationTypeSupport2 = value; }
        }

        /// <summary>
        /// Gets and sets the Authentication Type Support (third byte).
        /// </summary>
        /// <value>Authentication Type Support (third byte).</value>
        [IpmiMessageData(3)]
        public byte ExtendedCapabilities
        {
            get { return this.extendedCapabilities; }
            set { this.extendedCapabilities = value; }
        }

        /// <summary>
        /// Gets and sets the OEM Id.
        /// </summary>
        /// <value>OEM Id and Data.</value>
        [IpmiMessageData(4,3)]
        public byte[] OemId
        {
            get { return this.oemId; }
            set { this.oemId = value; }
        }

        /// <summary>
        /// OEM auxiliary data
        /// </summary>
        /// <value>OEM auxiliary data</value>
        [IpmiMessageData(7)]
        public byte OemData
        {
            get { return this.oemData; }
            set { this.oemData = value; }
        }
    }
}
