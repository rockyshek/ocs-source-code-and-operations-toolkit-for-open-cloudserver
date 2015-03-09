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
    /// Represents the IPMI 'Get User Access' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Application, IpmiCommand.GetUserAccess)]
    internal class GetUserAccessResponse : IpmiResponse
    {
        ///<summary>
        /// Maximum number of User Ids.
        /// </summary>
        private byte maxUsers;

        ///<summary>
        /// Number of enabled User Ids.
        /// </summary>
        private byte enabledUsers;

        ///<summary>
        /// Privilege level of a given User Id.
        /// </summary>
        private byte accessLevel;

        ///<summary>
        /// Number of fixed names
        /// </summary>
        private byte fixedNames;

        ///<summary>
        /// Link Authorization
        /// </summary>
        private byte linkAuth;

        ///<summary>
        /// IPMI Messaging
        /// </summary>
        private byte ipmiMessaging;

        /// <summary>
        /// Gets and sets the Max Users.
        /// </summary>
        [IpmiMessageData(0)]
        public byte MaxUsers
        {
            get { return this.maxUsers; }
            set { this.maxUsers = value; }
        }

        /// <summary>
        /// Gets count of enabled users.
        /// </summary>
        /// <value>Session Handle.</value>
        [IpmiMessageData(1)]
        public byte EnabledUsers
        {
            get { return this.enabledUsers; }
            set { this.enabledUsers = (byte)(value & 0x1F); }
        }

        /// <summary>
        /// Gets Count of User Ids with fixed names.
        /// </summary>
        [IpmiMessageData(2)]
        public byte FixedNames
        {
            get { return this.fixedNames; }
            set { this.fixedNames = (byte)(value & 0x1F); }
        }

        /// <summary>
        /// Link authentication.
        /// </summary>  
        [IpmiMessageData(3)]
        public byte LinkAuth
        {
            get { return this.linkAuth; }
            set { this.linkAuth = (byte)((value & 0x20) >> 5); }

        }

        /// <summary>
        /// Ipmi Messaging.
        /// </summary>      
        [IpmiMessageData(3)]
        public byte IpmiMessaging
        {
            get { return this.ipmiMessaging; }
            set { this.ipmiMessaging = (byte)((value & 0x10)  >> 4); }

        }

        /// <summary>
        /// Access Level.
        /// </summary>
        /// <returns>
        /// 0h = reserved       
        /// 1h = CALLBACK level
        /// 2h = USER level
        /// 3h = OPERATOR level
        /// 4h = ADMINISTRATOR level
        /// 5h = OEM Proprietary level
        /// </returns>
        [IpmiMessageData(3)]
        public byte AccessLevel
        {
            get { return this.accessLevel; }
            set { this.accessLevel = (byte)(value & 0x0F); }

        }
    }
}
