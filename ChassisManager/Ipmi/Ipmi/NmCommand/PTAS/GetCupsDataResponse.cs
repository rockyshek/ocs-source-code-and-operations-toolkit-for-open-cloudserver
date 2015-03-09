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
    /// Represents the Node Manager 'Get CUPS Data' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetCupsData)]
    public class GetCupsDataResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// CPU CUPS Value/CUPS Value
        /// </summary>
        private ushort cpuCupsValue;

        /// <summary>
        /// Memory CUPS Value
        /// </summary>
        private ushort memCupsValue;

        /// <summary>
        /// IO CUPS Value
        /// </summary>
        private ushort ioCupsValue;

        /// <summary>
        /// Reserved
        /// </summary>
        private ushort reserved;

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
        /// CPU CUPS Value/CUPS Value
        /// </summary>
        [NodeManagerMessageData(3, 2)]
        public ushort CpuCupsValue
        {
            get { return this.cpuCupsValue; }
            set { this.cpuCupsValue = value; }
        }

        /// <summary>
        /// Memory CUPS Value
        /// </summary>
        [NodeManagerMessageData(5, 2)]
        public ushort MemCupsValue
        {
            get { return this.memCupsValue; }
            set { this.memCupsValue = value; }
        }

        /// <summary>
        /// IO CUPS Value
        /// </summary>
        [NodeManagerMessageData(7, 2)]
        public ushort IoCupsValue
        {
            get { return this.ioCupsValue; }
            set { this.ioCupsValue = value; }
        }

        /// <summary>
        /// Reserved byte
        /// </summary>
        [NodeManagerMessageData(9,2)]
        public ushort Reserved
        {
            get { return this.reserved; }
            set { this.reserved = value; }
        }
    }
}
