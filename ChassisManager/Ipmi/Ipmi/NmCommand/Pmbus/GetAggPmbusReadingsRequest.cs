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
    /// Represents the Node Manager 'Get Pmbus Readings' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.GetAggPmbusReadings)]
    public class GetAggPmbusReadingsRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Register Offset 
        /// [0:3] = Offset of the register 
        /// [4:7] = Page number � used only for devices which support pages. 
        ///         For others Reserved
        /// </summary>
        private byte registerOffset;

        /// <summary>
        /// Device Indexes
        /// Each byte should contain Device Index
        /// </summary>
        private byte[] deviceIndexes;

        /// <summary>
        /// Initializes a new instance of the GetAggPmbusReadingsRequest (history) class.
        /// </summary>
        internal GetAggPmbusReadingsRequest(byte registerOffset, byte pageNumber, byte[] deviceIndexes)
        {

            // Register Offset 
            // [0:3] = Offset of the register 
            this.registerOffset = (byte)(registerOffset & 0x0F);

            /// Register Offset 
            /// [4:7] = Page number � used only for devices which support pages. 
            this.registerOffset = (byte)(this.registerOffset | (byte)((pageNumber & 0x0F) << 4));

            // Each byte should contain Device Index
            if (deviceIndexes != null)
                this.deviceIndexes = deviceIndexes;
            else
                this.deviceIndexes = new byte[0];
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
        /// Register Offset 
        /// [0:3] = Offset of the register 
        /// [4:7] = Page number � used only for devices which support pages. 
        ///         For others Reserved
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte RegisterOffset
        {
            get { return this.registerOffset; }
        }

        /// <summary>
        /// Device Indexes
        /// Each byte should contain Device Index
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte[] DeviceIndexes
        {
            get { return this.deviceIndexes; }
        }
    }
}
