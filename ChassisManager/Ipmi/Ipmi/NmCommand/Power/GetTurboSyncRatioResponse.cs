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
    /// Represents the Node Manager 'Get Turbo Sync Ratio' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetTurboSyncRatio)]
    public class GetTurboSyncRatioResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Turbo Ratio Limit
        /// When socket number and/or active core 
        /// configurations are set to 0xFF � valid current
        /// ratio if all selected active core configurations are synchronized with 
        /// the same value, 0 when there is no synchronization
        /// </summary>
        private byte turboRationLimit;

        /// <summary>
        /// Default Turbo Ratio Limit 
        /// When socket number and/or active core 
        /// configurations are set to 0xFF � valid default 
        /// ratio if all selected active core configurations are synchronized with 
        /// the same value, 0 when there is no synchronization
        /// </summary>
        private byte defaultTurboRationLimit;

        /// <summary>
        /// Maximum Turbo Ratio Limit 
        /// In case of socket number set to FFh this is 
        /// maximum Turbo Ratio Limit that could be set on all CPUs.
        /// </summary>
        private byte maxTurboRationLimit;

        /// <summary>
        /// Minimum Turbo Ratio Limit 
        /// In case of socket number set to FFh this is 
        /// minimum Turbo Ratio Limit that could be set on all CPUs.
        /// </summary>
        private byte minTurboRationLimit;

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        [NodeManagerMessageData(0, 3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
            set { this.manufactureId = value; }
        }

        /// <summary>
        /// Turbo Ratio Limit
        /// When socket number and/or active core 
        /// configurations are set to 0xFF � valid current
        /// ratio if all selected active core configurations are synchronized with 
        /// the same value, 0 when there is no synchronization
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte TurboRationLimit
        {
            get { return this.turboRationLimit; }
            set { this.turboRationLimit = value; }
        }

        /// <summary>
        /// Default Turbo Ratio Limit 
        /// When socket number and/or active core 
        /// configurations are set to 0xFF � valid default 
        /// ratio if all selected active core configurations are synchronized with 
        /// the same value, 0 when there is no synchronization
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte DefaultTurboRationLimit
        {
            get { return this.defaultTurboRationLimit; }
            set { this.defaultTurboRationLimit = value; }
        }

        /// <summary>
        /// Maximum Turbo Ratio Limit 
        /// In case of socket number set to FFh this is 
        /// maximum Turbo Ratio Limit that could be set on all CPUs.
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte MaximumTurboRationLimit
        {
            get { return this.maxTurboRationLimit; }
            set { this.maxTurboRationLimit = value; }
        }

        /// <summary>
        /// Minimum Turbo Ratio Limit 
        /// In case of socket number set to FFh this is 
        /// minimum Turbo Ratio Limit that could be set on all CPUs.
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte MinimumTurboRationLimit
        {
            get { return this.minTurboRationLimit; }
            set { this.minTurboRationLimit = value; }
        }


    }
}
