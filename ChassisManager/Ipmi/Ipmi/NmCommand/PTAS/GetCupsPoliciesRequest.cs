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
    /// Represents the Node Manager 'Get CUPS Policies' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetCupsPolicies)]
    public class GetCupsPoliciesRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Reserved
        /// </summary>
        private byte reserved = 0;

        /// <summary>
        /// CUPS Policy ID
        /// [0:3] Domain Identifier
        /// 0x00 - Core Domain
        /// 0x01 - IO Domain
        /// 0x02 - Memory Domain
        /// [4:7] Target Identifier
        /// 0x00 - BMC
        /// 0x01 - Remote Console
        /// </summary>
        private byte policyID;

        /// <summary>
        /// Initializes a new instance of the GetCupsPoliciesRequest class.
        /// </summary>
        internal GetCupsPoliciesRequest(NodeManagerCupsPolicyDomainId policyDomainId, NodeManagerCupsPolicyTargetId policyTargetId)
        {
            /// CUPS Policy ID
            // Bits [0:3] is the Domain Identifier
            byte tempPolicyID = (byte)((byte)policyDomainId & 0xf);
            // Bits [4:7] is the Target Identifier
            tempPolicyID = (byte)(tempPolicyID | (((byte)policyTargetId & 0xf) << 4));

            this.policyID = tempPolicyID;
        }

        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        [NodeManagerMessageData(0,3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
        }

        /// <summary>
        /// Reserved
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte Reserved
        {
            get { return reserved; }
        }

        /// <summary>
        /// CUPS Policy ID
        /// [0:3] Domain Identifier
        /// 0x00 - Core Domain
        /// 0x01 - IO Domain
        /// 0x02 - Memory Domain
        /// [4:7] Target Identifier
        /// 0x00 - BMC
        /// 0x01 - Remote Console
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte PolicyID
        {
            get { return policyID; }
        }
    }
}
