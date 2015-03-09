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
    /// Represents the IPMI 'Set User Password' application request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.SetUserPassword, 22)]
    internal class SetUserPasswordRequest : IpmiRequest
    {
        /// <summary>
        /// User Id.
        /// [7] Password lenght
        /// [5:0] UserId
        /// </summary>
        private byte userId;

        /// <summary>
        /// Password, 20 byte max for IPMI V2.0 RMCP, 16 byte for IPMI v1.5
        /// </summary>
        private byte[] password;

        /// <summary>
        /// Disable user.
        /// </summary>
        public const byte OperationDisableUser = 0;

        /// <summary>
        /// Enable user.
        /// </summary>
        public const byte OperationEnableUser = 1;

        /// <summary>
        /// Set Password.
        /// </summary>
        public const byte OperationSetPassword = 2;

        /// <summary>
        /// Test Password.
        /// </summary>
        public const byte OperationTestPassword = 3;

        /// <summary>
        /// Operation to perform.
        /// </summary>
        public readonly byte operation;

        /// <summary>
        /// Initializes a new instance of the SetUserPassword class.
        /// </summary>
        public SetUserPasswordRequest(byte userId, byte operation, byte[] password)
        {
            this.userId = userId;
            this.operation = operation;
            this.password = password;
        }

        /// <summary>
        /// Set the password for a specific user id.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte UserId
        {
            get { return this.userId; }
        }


        /// <summary>
        /// Set the password operation.
        /// </summary>       
        [IpmiMessageData(1)]
        public byte Operation
        {
            get { return this.operation; }
        }


        /// <summary>
        /// Set the password.
        /// 20 byte password.
        /// </summary>       
        [IpmiMessageData(2, 20)]
        public byte[] Password
        {
            get { return this.password; }
        }
    }
}
