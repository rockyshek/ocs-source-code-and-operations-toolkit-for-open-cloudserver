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
    /// Represents the IPMI 'Activate Session' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.ActivateSession, 22)]
    internal class ActivateSessionRequest : IpmiRequest
    {
        /// <summary>
        /// Session authentication type.
        /// </summary>
        private readonly AuthenticationType sessionAuthenticationType;

        /// <summary>
        /// Maximum privilege level for this session.
        /// </summary>
        private readonly PrivilegeLevel maximumPrivilegeLevel;

        /// <summary>
        /// Challenge string from Get Session Challenge message.
        /// For multi-session channels: (e.g. LAN channel):
        /// Challenge String data from corresponding Get Session Challenge response.
        /// For single-session channels that lack session header (e.g. serial/modem in 
        /// Basic Mode):
        /// Clear text password or AuthCode. See Ipmi: 22.17.1, AuthCode Algorithms.
        /// </summary>
        private readonly byte[] sessionChallengeString;

        /// <summary>
        /// Initial outbound sequence number (can't be 0).
        /// </summary>
        private readonly uint initialOutboundSequenceNumber;

        /// <summary>
        /// Initializes a new instance of the ActivateSessionRequest class.
        /// </summary>
        /// <param name="sessionAuthenticationType">Session authentication type.</param>
        /// <param name="maximumPrivilegeLevel">Maximum privilege level for this session.</param>
        /// <param name="sessionChallengeString">Challenge string from Get Session Challenge message.</param>
        /// <param name="initialOutboundSequenceNumber">Initial outbound sequence number.</param>
        internal ActivateSessionRequest(
            AuthenticationType sessionAuthenticationType, 
            PrivilegeLevel maximumPrivilegeLevel,
            byte[] sessionChallengeString,
            uint initialOutboundSequenceNumber)
        {
            this.sessionAuthenticationType = sessionAuthenticationType;
            this.maximumPrivilegeLevel = maximumPrivilegeLevel;
            this.sessionChallengeString = sessionChallengeString;
            this.initialOutboundSequenceNumber = initialOutboundSequenceNumber;
        }

        /// <summary>
        /// Gets the Session authentication type.
        /// </summary>
        /// <value>Session authentication type.</value>
        [IpmiMessageData(0)]
        public byte SessionAuthenticationType
        {
            get { return (byte)this.sessionAuthenticationType; }
        }

        /// <summary>
        /// Gets the Maximum privilege level for this session.
        /// </summary>
        /// <value>Maximum privilege level for this session.</value>
        [IpmiMessageData(1)]
        public byte MaximumPrivilegeLevel
        {
            get { return (byte)this.maximumPrivilegeLevel; }
        }

        /// <summary>
        /// Gets the Challenge string from Get Session Challenge message.
        /// </summary>
        /// <value>Challenge string from Get Session Challenge message.</value>
        [IpmiMessageData(2, 16)]
        public byte[] SessionChallengeString
        {
            get { return this.sessionChallengeString; }
        }

        /// <summary>
        /// Gets the Initial outbound sequence number.
        /// </summary>
        /// <value>Initial outbound sequence number.</value>
        [IpmiMessageData(18)]
        public uint InitialOutboundSequenceNumber
        {
            get { return this.initialOutboundSequenceNumber; }
        }
    }
}
