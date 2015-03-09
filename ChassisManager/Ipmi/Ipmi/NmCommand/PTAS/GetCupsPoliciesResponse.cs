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
    /// Represents the Node Manager 'Get CUPS Policies' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetCupsPolicies)]
    public class GetCupsPoliciesResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Policy enable
        /// [0] 0 - Policy disabled
        ///     1 - Policy enabled
        /// [1:7] Reserved.
        /// </summary>
        private byte enable;

        /// <summary>
        /// Policy Type
        /// [0:6] - Reserved
        /// [7] Policy storage option
        /// 0 - persistent storage (policy is saved to nonvolatile memory)
        /// 1 - volatile memory is used for storing the policy
        /// </summary>
        private byte policyType;

        /// <summary>
        /// Policy Excursion Actions
        /// [0] 0 - No action
        ///     1 - Send alert
        /// [1:7] - Reserved
        /// </summary>
        private byte policyExcursionAction;

        /// <summary>
        /// CUPS Threshold
        /// </summary>
        private ushort cupsThreshold;

        /// <summary>
        /// Averaging Window (in seconds)
        /// </summary>
        private ushort avgWindow;

        /// <summary>
        /// Contains lowest valid Policy ID that is higher than Policy ID specified in the request.
        /// Only valid if Completion Code = 80h (Policy ID Invalid)
        /// </summary>
        private byte nextValidPolicyID;

        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        [NodeManagerMessageData(0, 3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
            set { this.manufactureId = value; }
        }

        /// <summary>
        /// Policy enable
        /// [0] 0 - Policy disabled
        ///     1 - Policy enabled
        /// [1:7] Reserved.
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte Enable
        {
            get { return this.enable; }
            set { this.enable = (byte)(value & 0x1); }
        }

        /// <summary>
        /// Contains lowest valid Policy ID that is higher than Policy ID specified in the request.
        /// Only valid if Completion Code = 80h (Policy ID Invalid)
        /// This shares the same byte as the enable byte.
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte NextValidPolicyID
        {
            get { return this.nextValidPolicyID; }
            set { this.nextValidPolicyID = value; }
        }

        /// <summary>
        /// Policy Type
        /// [0:6] - Reserved
        /// [7] Policy storage option
        /// 0 - persistent storage (policy is saved to nonvolatile memory)
        /// 1 - volatile memory is used for storing the policy
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte PolicyType
        {
            get { return this.policyType; }
            set { this.policyType = (byte)(value & 0x80); }
        }

        /// <summary>
        /// Policy Excursion Actions
        /// [0] 0 - No action
        ///     1 - Send alert
        /// [1:7] - Reserved
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte PolicyExcursionAction
        {
            get { return this.policyExcursionAction; }
            set { this.policyExcursionAction = (byte)(value & 0x1); }
        }

        /// <summary>
        /// CUPS Threshold
        /// </summary>
        [NodeManagerMessageData(6)]
        public ushort CupsThreshold
        {
            get { return this.cupsThreshold; }
            set { this.cupsThreshold = value; }
        }

        /// <summary>
        /// Averaging Window (in seconds)
        /// </summary>
        [NodeManagerMessageData(8)]
        public ushort AvgWindow
        {
            get { return this.avgWindow; }
            set { this.avgWindow = value; }
        }
    }
}
