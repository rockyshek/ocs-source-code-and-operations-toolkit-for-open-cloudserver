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

namespace Microsoft.GFS.WCS.ChassisManager
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Represents a client session to a computer via sled over the network.
    /// </summary>
    public abstract class ChassisSendReceive
    {   
        #region Send/Receive Methods

        /// <summary>
        /// Generics method SendReceive for easier use
        /// </summary>
        public T SendReceive<T>(DeviceType deviceType, byte deviceId, ChassisRequest chassisRequest) where T : ChassisResponse
        {
            // call SendReceive
            return (T)SendReceive(deviceType, deviceId, chassisRequest, typeof(T));
        }


        /// <summary>
        /// Send Receive chassis messages
        /// </summary>
        public ChassisResponse SendReceive(DeviceType deviceType, byte deviceId, ChassisRequest chassisRequest, Type responseType, byte priority = (byte)PriorityLevel.User)
        {
            // Serialize the OMC request into bytes.
            byte[] chassisRequestMessage = chassisRequest.GetBytes();
            byte[] chassisResponseMessage;

            CommunicationDevice.SendReceive((PriorityLevel)priority, (byte)deviceType, deviceId, chassisRequestMessage, out chassisResponseMessage);

            // Create the response based on the provided type and message bytes.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            ChassisResponse chassisResponse = (ChassisResponse)constructorInfo.Invoke(new Object[0]);

            // Expected Packet Format:
            //        4            5-6       N         
            // |Completion Code|Byte Count|Payload|
            //       0 byte       2 byte    3+ byte
            if (chassisResponseMessage.Length >= 3)
            {
                chassisResponse.Initialize(chassisResponseMessage, chassisResponseMessage.Length);
            }
            else
            {
                chassisResponse.CompletionCode = (byte)CompletionCode.ResponseNotProvided;
            }
            // Response to the OMC request message.
            return chassisResponse;
        }

        #endregion
    }
}
