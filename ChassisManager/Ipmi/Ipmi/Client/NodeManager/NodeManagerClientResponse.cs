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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager
{
    /// <summary>
    /// Class that supports the Send Message / Get Message command.
    /// </summary>
    public class SendNodeMangerMessage : ResponseBase
    {
        /// <summary>
        /// Response message payload.
        /// </summary>
        private byte[] messageData;

        /// <summary>
        /// Request Sequence Number
        /// </summary>
        private byte rqSeq;


        private NodeManagerResponse response;

        /// <summary>
        /// Initialize class
        /// </summary>
        public SendNodeMangerMessage(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        {
            this.messageData = param;
        }

        internal void SetParamaters(byte[] messageData, byte rqSep)
        {
            this.messageData = messageData;
            this.rqSeq = rqSep;
        }

        /// <summary>
        /// Response message payload.
        /// </summary>
        public byte[] MessageData
        {
            get { return this.messageData; }
            set { this.messageData = value; }
        }

        /// <summary>
        /// Request message sequence.
        /// </summary>
        public byte RqSeq
        {
            get { return this.rqSeq; }
            internal set { this.rqSeq = value; }
        }

        /// <summary>
        /// Response message payload.
        /// </summary>
        public NodeManagerResponse Response
        {
            get { return this.response; }
            internal set { this.response = value; }
        }
    }

    /// <summary>
    /// Class that supports the Send Message / Get Message command.
    /// </summary>
    public class GetNodeMangerMessage : ResponseBase
    {
        /// <summary>
        /// Response message payload.
        /// </summary>
        private byte[] messageData;

        /// <summary>
        /// Request Sequence Number
        /// </summary>
        private NodeManagerResponse response;

        /// <summary>
        /// Initialize class
        /// </summary>
        public GetNodeMangerMessage(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        {
            this.messageData = param;
        }

        internal void SetParamaters(byte[] messageData, NodeManagerResponse response)
        {
            this.messageData = messageData;
            this.response = response;
        }

        /// <summary>
        /// Response message payload.
        /// </summary>
        public byte[] MessageData
        {
            get { return this.messageData; }
            set { this.messageData = value; }
        }

        /// <summary>
        /// Response message payload.
        /// </summary>
        public NodeManagerResponse Response
        {
            get { return this.response; }
            internal set { this.response = value; }
        }
    }

}
