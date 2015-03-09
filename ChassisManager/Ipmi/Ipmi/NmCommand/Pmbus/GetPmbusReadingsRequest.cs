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
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.GetPmbusReadings)]
    public class GetPmbusReadingsRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Device Index
        /// [0:4] = PMBUS-enabled Device Index 
        /// [5:7] = Reserved. Write as 000b
        /// </summary>
        private byte devIndex;

        /// <summary>
        /// History index 
        /// [0:3] = History index. Supported values 0x00 -0x09 � to retrieve 
        ///         history samples and 0x0f to retrieve current samples 
        /// [7:4] � Page number � used only for devices which support pages. 
        /// For others Reserved.
        /// </summary>
        private byte historyIndex;

        /// <summary>
        /// First Register Offset 
        /// [7:4] - Reserved. Write as 00000b.
        /// [3:0] - First Register Offse
        /// </summary>
        private byte firstRegisterOffset;

        /// <summary>
        /// Initializes a new instance of the GetPmbusReadingsRequest (history) class.
        /// </summary>
        internal GetPmbusReadingsRequest(byte deviceIndex, byte historyIndex, byte firstRegisterOffset)
        {
            // [0:4] = PMBUS-enabled Device Index 
            this.devIndex = (byte)(deviceIndex & 0x1F);

            // History index. Supported values 0x00 -0x09 � to retrieve 
            // history samples and 0x0f to retrieve current samples 
            this.historyIndex = (byte)(historyIndex & 0x0F);

            // [7:4] - Reserved. Write as 00000b.
            // [3:0] - First Register Offset
            this.firstRegisterOffset = (byte)(firstRegisterOffset & 0x0F);
        }

        /// <summary>
        /// Initializes a new instance of the GetPmbusReadingsRequest (current samples) class.
        /// </summary>
        internal GetPmbusReadingsRequest(byte deviceIndex, byte firstRegisterOffset)
        {
            // [0:4] = PMBUS-enabled Device Index 
            this.devIndex = (byte)(deviceIndex & 0x1F);

            // History index. Supported values 0x00 -0x09 � to retrieve 
            // history samples and 0x0f to retrieve current samples 
            this.historyIndex = 0x0F;

            // [7:4] - Reserved. Write as 00000b.
            // [3:0] - First Register Offset
            this.firstRegisterOffset = (byte)(firstRegisterOffset & 0x0F);
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
        /// Device Index
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte DeviceIndex
        {
            get { return this.devIndex; }
        }

        /// <summary>
        /// History index
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte HistoryIndex
        {
            get { return this.historyIndex; }
        }

        /// <summary>
        ///  First Register Offset 
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte FirstRegisterOffset
        {
            get { return this.firstRegisterOffset; }
        }


    }
}
