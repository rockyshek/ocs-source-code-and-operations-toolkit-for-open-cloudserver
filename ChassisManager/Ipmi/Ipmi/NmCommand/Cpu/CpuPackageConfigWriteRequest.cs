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
    /// Represents the Node Manager 'Cpu Package Config Write' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.CpuPackageConfigWrite)]
    public class CpuPackageConfigWriteRequest : NodeManagerRequest
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
        /// PCS Index
        /// </summary>
        private byte pcsIndex;

        /// <summary>
        ///  Parameter Number (WORD)
        /// </summary>
        private ushort parameterNumber;

        /// <summary>
        ///  Parameter
        /// </summary>
        private byte[] parameter = new byte[2];

        /// <summary>
        /// Byte 8 � Write Length � number of bytes to write 
        /// [7:2] � Reserved.
        /// [1:0] � write Length � number of bytes to read: 
        ///         0 � Reserved � shouldn�t be used.
        ///         1 � 1 byte.
        ///         2 � 2 bytes (word).
        ///         3 � 4 bytes (double word)
        /// </summary>
        private byte writeLenght;

        /// <summary>
        /// Data to be written to CPU. Length of this data (1B, 2B, 4B) 
        /// depends on Write Length value included in Byte 8 of this request
        /// </summary>
        private byte[] cpuData;

        /// <summary>
        /// Initializes a new instance of the CpuPackageConfigWriteRequest class.
        /// </summary>
        internal CpuPackageConfigWriteRequest(byte cpuNumber, byte pcsIndex, 
            ushort parameterNo, byte[] parameter, byte writeLenght, byte[] cpuData)
        {
            this.cpuNumber = (byte)(cpuNumber & 0x03);
            this.pcsIndex = pcsIndex;
            this.parameterNumber = parameterNo;

            if (parameter != null)
            {
                if (parameter.Length == 2)
                {
                    this.parameter[0] = parameter[0];
                    this.parameter[1] = parameter[1];
                }
            }

            this.writeLenght = (byte)(writeLenght & 0x03);

            this.cpuData = cpuData;
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
        /// PCS Index
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte PcsIndex
        {
            get { return this.pcsIndex; }
        }

        /// <summary>
        /// Parameter Number
        /// </summary>
        [NodeManagerMessageData(5)]
        public ushort ParameterNumber
        {
            get { return this.parameterNumber; }
        }

        /// <summary>
        /// Parameter
        /// </summary>
        [NodeManagerMessageData(7,2)]
        public byte[] Parameter
        {
            get { return this.parameter; }
        }

        /// <summary>
        /// Write Lenght
        /// </summary>
        [NodeManagerMessageData(9)]
        public byte WriteLenght
        {
            get { return this.writeLenght; }
        }

        /// <summary>
        /// Write Data
        /// </summary>
        [NodeManagerMessageData(10)]
        public byte[] CpuData
        {
            get { return this.cpuData; }
        }

    }
}
