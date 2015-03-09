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
    /// Represents the Node Manager 'Get CUPS Configuration' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetCupsConfiguration)]
    public class GetCupsConfigurationResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacturer Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// CUPS Enable Status
        /// [0] 0 - Disable CUPS until next. 
        ///     1 - Enable CUPS
        /// [1:7] Reserved.
        /// </summary>
        private byte enable;

        /// <summary>
        /// CUPS Load Factor valid Domain mask
        /// [0] - Core Load Factor Mask
        /// [1] - IO Load Factor Mask
        /// [2] - Memory Load Factor Mask
        /// [3:7] Reserved
        /// </summary>
        private byte loadFactorMask;

        /// <summary>
        /// Core Load Factor
        /// </summary>
        private ushort coreLoadFactor;

        /// <summary>
        /// IO Load Factor
        /// </summary>
        private ushort ioLoadFactor;

        /// <summary>
        /// Memory Load Factor
        /// </summary>
        private ushort memLoadFactor;

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
        /// CUPS Enable Status
        /// [0] 0 - Disable CUPS until next. 
        ///     1 - Enable CUPS
        /// [1:7] Reserved.
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte Enable
        {
            get { return this.enable; }
            set { this.enable = (byte)(value & 0x1); }
        }

        /// <summary>
        /// CUPS Load Factor valid Domain mask
        /// [0] - Core Load Factor Mask
        /// [1] - IO Load Factor Mask
        /// [2] - Memory Load Factor Mask
        /// [3:7] Reserved
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte LoadFactorMask
        {
            get { return this.loadFactorMask; }
            set { this.loadFactorMask = value; }
        }

        /// <summary>
        /// Core Load Factor
        /// </summary>
        [NodeManagerMessageData(5,2)]
        public ushort CoreLoadFactor
        {
            get { return this.coreLoadFactor; }
            set { this.coreLoadFactor = value; }
        }

        /// <summary>
        /// IO Load Factor
        /// </summary>
        [NodeManagerMessageData(7, 2)]
        public ushort IoLoadFactor
        {
            get { return this.ioLoadFactor; }
            set { this.ioLoadFactor = value; }
        }

        /// <summary>
        /// Memory Load Factor
        /// </summary>
        [NodeManagerMessageData(9, 2)]
        public ushort MemLoadFactor
        {
            get { return this.memLoadFactor; }
            set { this.memLoadFactor = value; }
        }

        /// <summary>
        /// The core load factor mask.
        /// </summary>
        public byte CoreLoadFactorMask
        {
            // Core Load Factor Mask = Bit 0 of loadFactorMask
            get { return (byte)(this.loadFactorMask & 0x1); }
        }

        /// <summary>
        /// The IO load factor mask.
        /// </summary>
        public byte IoLoadFactorMask
        {
            // IO Load Factor Mask = Bit 1 of loadFactorMask
            get { return (byte)((this.loadFactorMask & 0x2) >> 1); }
        }

        /// <summary>
        /// The memory load factor mask.
        /// </summary>
        public byte MemLoadFactorMask
        {
            // Memory Load Factor Mask = Bit 2 of loadFactorMask
            get { return (byte)((this.loadFactorMask & 0x4) >> 2); }
        }
    }
}
