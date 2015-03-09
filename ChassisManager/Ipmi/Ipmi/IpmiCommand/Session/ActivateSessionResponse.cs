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
    /// Represents the IPMI 'Activate Session' application response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.ActivateSession)]
    internal class ActivateSessionResponse : IpmiResponse
    {
        /// <summary>
        /// Session authentication type.
        /// </summary>
        private AuthenticationType sessionAuthenticationType;

        /// <summary>
        /// Session Id.
        /// </summary>
        private uint sessionId;

        /// <summary>
        /// Initial inbound sequence number (can't be 0).
        /// </summary>
        private uint initialInboundSequenceNumber;

        /// <summary>
        /// Maximum privilege level for this session.
        /// </summary>
        private PrivilegeLevel maximumPrivilegeLevel;

        /// <summary>
        /// Gets and sets the Session authentication type.
        /// </summary>
        /// <value>Session authentication type.</value>
        [IpmiMessageData(0)]
        public byte SessionAuthenticationType
        {
            get { return (byte)this.sessionAuthenticationType; }
            set { this.sessionAuthenticationType = (AuthenticationType)value; }
        }

        /// <summary>
        /// Gets and sets the Session Id.
        /// </summary>
        /// <value>Session Id.</value>
        [IpmiMessageData(1)]
        public uint SessionId
        {
            get { return this.sessionId; }
            set { this.sessionId = value; }
        }

        /// <summary>
        /// Gets and sets the Initial inbound sequence number.
        /// </summary>
        /// <value>Initial inbound sequence number.</value>
        [IpmiMessageData(5)]
        public uint InitialInboundSequenceNumber
        {
            get { return this.initialInboundSequenceNumber; }
            set { this.initialInboundSequenceNumber = value; }
        }

        /// <summary>
        /// Gets and sets the Maximum privilege level for this session.
        /// </summary>
        /// <value>Maximum privilege level for this session.</value>
        [IpmiMessageData(9)]
        public byte MaximumPrivilegeLevel
        {
            get { return (byte)this.maximumPrivilegeLevel; }
            set { this.maximumPrivilegeLevel = (PrivilegeLevel)value; }
        }
    }
}
