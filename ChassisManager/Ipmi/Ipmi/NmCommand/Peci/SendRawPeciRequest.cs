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

    /// <summary>
    /// Represents the Node Manager 'Send Raw Peci' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.SendRawPeci)]
    public class SendRawPeciRequest : NodeManagerRequest
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
        private byte[] peciCommand;

        /// <summary>
        /// Initializes a new instance of the SendRawPeciRequest class.
        /// </summary>
        internal SendRawPeciRequest(NodeManagerPeciInterface peciInterface, byte peciAddress, byte writeLenght, byte readLenght, byte[] peciCommand )
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
            if (peciCommand == null)
                peciCommand = new byte[0];

            // The remaining part of PECI command
            this.peciCommand = peciCommand;
        }

       
        /// <summary>
        /// Returns the PECI Power Load for MSR Power Capping
        /// </summary>
        /// <param name="targetLimit">Power Limit number</param>
        /// <param name="watts">Power Limit value</param>
        /// <param name="correctionTime">Time Window</param>
        private byte[] GetMsrPayload(MsrTargetPowerLimit targetLimit, ushort watts, byte correctionTime)
        {
            byte lockval = 0x7F;

            byte[] PeciPayload = new byte[9];

            PeciPayload[0] = 0xA5; // codes understood by Intel ME FW
            PeciPayload[1] = 0x00; // only domain zero supported.
            PeciPayload[2] = (byte)targetLimit; // target limit (PL1 or PL2)
            PeciPayload[3] = 0x00;
            PeciPayload[4] = 0x00;

            Buffer.BlockCopy(PowerLimitMsr(watts, correctionTime), 0, PeciPayload, 5, 4);

            if (targetLimit == MsrTargetPowerLimit.PowerLimt2)
                PeciPayload[8] = (byte)(PeciPayload[8] & lockval);

            return PeciPayload;
        }

        private byte[] PowerLimitMsr(ushort watts, byte correctionTime)
        {
            byte enableLimit = 0x01;
            byte clampLimit = 0x01;

            byte[] msr = new byte[4];

            Buffer.BlockCopy(BitConverter.GetBytes(watts << 3), 0, msr, 0, 2);
            msr[1] = (byte)(msr[1] | enableLimit << 7);
            msr[2] = clampLimit;
            msr[2] = (byte)(msr[2] | ((correctionTime << 1) & 0xFE));

            return msr;
        }

        /// <summary>
        /// Send Raw PECI request to power cap CPU.
        /// </summary>
        internal SendRawPeciRequest(PeciTargetCpu Cpu, MsrTargetPowerLimit powerlimit, ushort watts, byte correctionTime)
        {

            this.peciAddress = (byte)Cpu; // PECI Client Address and interface selection
            this.writeLenght = 0x0A;      // Write Length (part of PECI standard header)
            this.readLenght = 0x01;       // Read Length (part of PECI standard header)
            this.peciCommand = GetMsrPayload(powerlimit, watts, correctionTime); // PECI Payload.

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
        public byte[] PeciCommand
        {
            get { return this.peciCommand; }
        }

    }
}
