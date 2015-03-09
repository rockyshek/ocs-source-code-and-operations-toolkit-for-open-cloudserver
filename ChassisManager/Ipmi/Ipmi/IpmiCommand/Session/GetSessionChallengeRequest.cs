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
    /// Represents the IPMI 'Get Session Challenge' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetSessionChallenge, 17)]
    internal class GetSessionChallengeRequest : IpmiRequest
    {
        /// <summary>
        /// Challenge authentication type (MD5 by default).
        /// </summary>
        private readonly byte challengeAuthenticationType = 0x02;

        /// <summary>
        /// User name.
        /// </summary>
        private readonly byte[] UserId;

        /// <summary>
        /// Initializes a new instance of the GetSessionChallengeRequest class.
        /// </summary>
        /// <param name="authenticationType">Authentication type for challenge.</param>
        /// <param name="userid">Userid or null.</param>
        internal GetSessionChallengeRequest(AuthenticationType authenticationType, string userId)
        {
            this.challengeAuthenticationType = (byte)authenticationType;

            this.UserId = new byte[16];

            if (!string.IsNullOrEmpty(userId))
            {
                for (int i = 0; i < userId.Length; i++)
                {
                    this.UserId[i] = (byte)userId[i];
                }
            }
        }

        /// <summary>
        /// Gets the Challenge authentication type (always MD5).
        /// </summary>
        /// <value>Challenge authentication type (always MD5).</value>
        [IpmiMessageData(0)]
        public byte ChallengeAuthenticationType
        {
            get { return this.challengeAuthenticationType; }
        }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        /// <value>16 byte array representing the user name or all 0's for null.</value>
        [IpmiMessageData(1)]
        public byte[] UserName
        {
            get { return this.UserId; }
        }
    }
}
