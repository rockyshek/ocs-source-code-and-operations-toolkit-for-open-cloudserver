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
    /// Represents the IPMI 'Set User Access ' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.SetUserAccess, 4)]
    class SetUserAccessRequest : IpmiRequest
    {

        /// <summary>
        /// Max number of sessions allowed.
        /// </summary>    
        byte sessionLimit;

        /// <summary>
        /// User Id.
        /// </summary> 
        byte userId;


        /// <summary>
        /// Priviledge level for a User
        /// </summary> 
        byte userLimits;

        /// <summary>
        /// Request byte 1 for SetUserAccessRequest
        /// </summary> 
        byte requestByte1;


        public SetUserAccessRequest(byte userId, byte userLimit, byte requestbyte1, byte sessionLimit)
        {           

            this.userId = userId;
            this.userLimits = userLimit;            
            this.sessionLimit = sessionLimit;
            this.requestByte1 = requestbyte1;                        

        }

        

        /// <summary>
        /// Gets request byte 1 for SetUserAccessRequest.
        /// </summary>
        [IpmiMessageData(0)]
        public byte RequestByte1
        {
            get { return this.requestByte1; }
        }


        /// <summary>
        /// Gets the User Id.
        /// </summary>
        [IpmiMessageData(1)]
        public byte UserId
        {
            get { return this.userId; }
        }


        /// <summary>
        /// Gets the user limit\level.
        /// </summary>
        [IpmiMessageData(2)]
        public byte UserLimits
        {
            get { return this.userLimits; }
        }


        /// <summary>
        /// Sets the Session Limit.
        /// </summary>
        [IpmiMessageData(3)]
        public byte SessionLimit
        {
            get { return this.sessionLimit; }
        }
    }
}
