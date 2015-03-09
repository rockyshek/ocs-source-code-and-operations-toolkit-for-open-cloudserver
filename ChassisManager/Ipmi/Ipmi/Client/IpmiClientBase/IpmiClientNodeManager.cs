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
    using System.Reflection;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager;

    internal abstract class IpmiClientNodeManager : IpmiClientExtended
    {
        /// <summary>
        /// Send sync Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="slaveId">Channel Slave Id</param>
        /// <param name="messageData">Message payload</param>
        public virtual SendNodeMangerMessage SendNodeManagerRequest<T>(byte channel, byte slaveId, NodeManagerRequest requestMessage) where T : NodeManagerResponse
        {
            return SendNodeManagerRequest(channel, slaveId, true, requestMessage, typeof(T));
        }

        /// <summary>
        /// Send sync Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="slaveId">Channel Slave Id</param>
        /// /// <param name="trackRequest">Trace Message</param>
        /// <param name="messageData">Message payload</param>
        public virtual SendNodeMangerMessage SendNodeManagerRequest(byte channel, byte slaveId, bool trackRequest, NodeManagerRequest requestMessage, Type responseMessage)
        {
            byte rqSeq = NodeManagerRqSeq();
            byte[] nodeManagerMessage = requestMessage.GetNodeManagerBytes(slaveId, rqSeq, trackRequest);

            SendMessageResponse msgResponse = (SendMessageResponse)this.IpmiSendReceive(
              new SendMessageRequest(channel, trackRequest, nodeManagerMessage),
              typeof(SendMessageResponse));

            SendNodeMangerMessage response = new SendNodeMangerMessage(msgResponse.CompletionCode);

            if (response.CompletionCode == 0x00)
            {

                // Create the response based on the provided type.
                ConstructorInfo constructorInfo = responseMessage.GetConstructor(Type.EmptyTypes);
                NodeManagerResponse nodeManagerResponse = (NodeManagerResponse)constructorInfo.Invoke(new Object[0]);

                // set the message data.
                response.MessageData = msgResponse.MessageData;

                try
                {
                    nodeManagerResponse.InitializeNodeManager(msgResponse.MessageData, msgResponse.MessageData.Length, rqSeq);
                    response.Response = nodeManagerResponse;
                }
                catch (Exception ex)
                {
                    IpmiSharedFunc.WriteTrace(string.Format("Send Message error converting payload. Exception occured converting packet: {0}", ex));

                    // set an exception code for invalid data in ipmi data field, as the packet could
                    nodeManagerResponse.CompletionCode = 0xD6; // IpmiCmdFailedIllegalParameter
                }

            }

            return response;
        }


        /// <summary>
        /// Generics method GetNodeManagerMessage for easier use
        /// </summary>
        public virtual T GetNodeManagerMessage<T>(byte rqSeq, byte channel) where T : GetNodeMangerMessage
        {
            return (T)GetNodeManagerMessage(rqSeq, channel, typeof(T));
        }


        /// <summary>
        /// Send Async Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="messageData">Message payload</param>
        public virtual GetNodeMangerMessage GetNodeManagerMessage(byte rqSeq, byte channel, Type responseMessage)
        {

            GetMessageResponse getMsg = (GetMessageResponse)this.IpmiSendReceive(
            new GetMessageRequest(), typeof(GetMessageResponse));

            GetNodeMangerMessage response = new GetNodeMangerMessage(getMsg.CompletionCode);

            // Create the response based on the provided type.
            ConstructorInfo constructorInfo = responseMessage.GetConstructor(Type.EmptyTypes);
            NodeManagerResponse nodeManagerResponse = (NodeManagerResponse)constructorInfo.Invoke(new Object[0]);

            if (getMsg.CompletionCode == 0x00)
            {
                // set the message data.
                response.MessageData = getMsg.MessageData;

                if (getMsg.MessageData != null)
                {
                    if (getMsg.MessageData.Length > 6)
                    {
                        nodeManagerResponse.CompletionCode = getMsg.MessageData[6];
                    }
                    else
                    {
                        nodeManagerResponse.CompletionCode = 0xC9; // IpmiParameterOutOfRange
                    }
                }
                else
                {
                    nodeManagerResponse.CompletionCode = 0xCE; // IpmiResponseNotProvided
                }
            }
            else
            {
                nodeManagerResponse.CompletionCode = getMsg.CompletionCode;
            }

            if (nodeManagerResponse.CompletionCode == 0x00)
            {
                try
                {
                    nodeManagerResponse.InitializeNodeManager(getMsg.MessageData, getMsg.MessageData.Length, rqSeq);
                    response.Response = nodeManagerResponse;
                }
                catch (Exception)
                {
                    // set an exception code for invalid data in ipmi data field, as the packet could
                    nodeManagerResponse.CompletionCode = 0xD6; // IpmiCmdFailedIllegalParameter
                }
            }

            return response;
        }
    }
}
