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
    /// Represents the Node Manager 'Reset Statistics' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.ResetStatistics)]
    public class ResetStatisticsRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Mode
        /// [0:4] Mode
        /// [5:7] Reserved. Write as 00.
        /// </summary>
        private byte mode;

        /// <summary>
        /// Domain Id
        /// [0:3] Domain Id
        /// [4:7] Reserved. Write as 00.
        /// </summary>
        private byte domainId;

        /// <summary>
        /// Policy Id
        /// </summary>
        private byte policyId;

        /// <summary>
        /// Initializes a new instance of the GetStatisticsRequest class.
        /// </summary>
        internal ResetStatisticsRequest(NodeManagerDomainId domainId, NodeManagerStatistics mode, byte policyId)
        {
            this.domainId = (byte)domainId;
            this.mode = (byte)mode;
            this.policyId = policyId;
        }

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        [NodeManagerMessageData(0,3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
        }

        /// <summary>
        /// Mode
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte Mode
        {
            get { return this.mode; }
        }

        /// <summary>
        /// Domain Id
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte DomainId
        {
            get { return this.domainId; }
        }

        /// <summary>
        /// Policy Id
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte PolicyId
        {
            get { return this.policyId; }
        }
    }
}
