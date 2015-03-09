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
    /// Represents the Node Manager 'Aggregated Send Raw Peci' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.AggregatedSendRawPeci)]
    public class AggSendRawPeciRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// PECI Client Address and interface selection
        /// [7:6] � PECI Interface selection
        /// [5:0] - PECI Client Address 
        /// </summary>
        private byte peciAddress;

        /// <summary>
        /// Write Length (part of PECI standard header)
        /// </summary>
        private byte writeLenght;

        /// <summary>
        /// Read Length (part of PECI standard header)
        /// </summary>
        private byte readLenght;

        /// <summary>
        /// The remaining part of PECI command
        /// </summary>
        private byte[] peciCommands;

        /// <summary>
        /// Initializes a new instance of the AggSendRawPeciRequest class.
        /// </summary>
        internal AggSendRawPeciRequest(NodeManagerPeciInterface peciInterface, byte peciAddress, byte writeLenght, byte readLenght, byte[] peciCommands)
        {
            // peci interface
            byte peciInt = (byte)peciInterface;

            // PECI Client Address and interface selection
            this.peciAddress = (byte)(peciAddress & 0x3F);
            this.peciAddress = (byte)((peciInt << 6) | this.peciAddress);

            // Write Length (part of PECI standard header)
            this.writeLenght = writeLenght;

            // Read Length (part of PECI standard header)
            this.readLenght = readLenght;

            // if null set to zero byte array
            if (peciCommands == null)
                peciCommands = new byte[0];

            // The remaining part of PECI command
            this.peciCommands = peciCommands;
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
        /// PECI Client Address and interface selection
        /// [7:6] � PECI Interface selection
        /// [5:0] - PECI Client Address 
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte PeciAddress
        {
            get { return this.peciAddress; }
        }

        /// <summary>
        /// Write Length (part of PECI standard header)
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte WriteLenght
        {
            get { return this.writeLenght; }
        }

        /// <summary>
        /// Read Length (part of PECI standard header)
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte ReadLenght
        {
            get { return this.readLenght; }
        }

        /// <summary>
        /// The remaining part of PECI command
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte[] PeciCommands
        {
            get { return this.peciCommands; }
        }

    }
}
