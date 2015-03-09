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
    using System;
    using System.Collections;

    /// <summary>
    /// Represents the Node Manager 'CPU IA MSR Read' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.CpuIaMsrRead)]
    public class CpuIaMsrReadRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Cpu Number
        /// [7:2] � Reserved.
        /// [1:0] � CPU number (starting from 0).
        /// </summary>
        private byte cpuNumber;

        /// <summary>
        /// Thread Id
        /// </summary>
        private byte threadId;

        /// <summary>
        /// PCI Address:
        /// [31:28] � Reserved.
        /// [27:20] � Bus Number.
        /// [19:15] � Device Number.
        /// [14:12] � Function Number.
        /// [11:0]  � Register Address.
        /// </summary>
        private byte[] pciAddress = new byte[4];

        /// <summary>
        /// Read Length � number of bytes to read 
        /// [7:2] � Reserved.
        /// [1:0] � Read Length � number of bytes to read: 
        ///         0 � Reserved � shouldn�t be used.
        ///         1 � 1 byte.
        ///         2 � 2 bytes (word).
        ///         3 � 4 bytes (double word)
        /// </summary>
        private byte readLenght;

        /// <summary>
        /// Initializes a new instance of the CpuIaMsrReadRequest class.
        /// </summary>
        internal CpuIaMsrReadRequest(byte cpuNumber, byte threadId, ushort msrAddress, byte readLenght)
            : this(cpuNumber, threadId, BitConverter.GetBytes(msrAddress), readLenght)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CpuIaMsrReadRequest class.
        /// </summary>
        internal CpuIaMsrReadRequest(byte cpuNumber, byte threadId, byte[] msrAddress, byte readLenght)
        {
            this.cpuNumber = (byte)(cpuNumber & 0x03);

            this.threadId = threadId;
                        
            BitArray address = new BitArray(pciAddress);

            // register address byte 1 [0-7].
            IpmiSharedFunc.UpdateBitArray(ref address, 0, 7, msrAddress[0]);
            
            // register address byte 2 [8-11]
            IpmiSharedFunc.UpdateBitArray(ref address, 8, 15, msrAddress[1]);

            // copy all bits to byte array
            address.CopyTo(pciAddress, 0);

            this.readLenght = (byte)(readLenght & 0x03);
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
        /// Cpu Number
        /// [7:2] � Reserved.
        /// [1:0] � CPU number (starting from 0).
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte CpuNumber
        {
            get { return this.cpuNumber; }
        }

        /// <summary>
        /// PCI Address
        /// </summary>
        [NodeManagerMessageData(4,4)]
        public byte[] PciAddress
        {
            get { return this.pciAddress; }
        }

        /// <summary>
        /// Read Lenght
        /// </summary>
        [NodeManagerMessageData(8)]
        public byte ReadLenght
        {
            get { return this.readLenght; }
        }

    }
}
