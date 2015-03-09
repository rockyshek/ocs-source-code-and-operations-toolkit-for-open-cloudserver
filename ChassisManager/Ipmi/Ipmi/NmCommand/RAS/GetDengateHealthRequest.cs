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
    /// Represents the Node Manager 'Get Dengate Health Status' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.GetDengateHealthStatus)]
    public class GetDengateHealthRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Tag: only relevant for Health Status Type = 3, 4, 5, 
        /// and 6 (shall always be set to zero for other Health Status Type 
        /// values)
        /// Bit [15] � reserved � must be zero
        /// </summary>
        private byte[] tag = new byte[2];

        /// <summary>
        /// Dengate Health Type
        /// </summary>
        private byte dengateReqType;

        /// <summary>
        /// Physical address: only relevant for Health Status 
        /// Type = 6 (reserved otherwise)
        /// </summary>
        private byte[] physicalAddress;

        /// <summary>
        /// Reserved
        /// </summary>
        private readonly byte reserved = 0x00;

        /// <summary>
        /// Initializes a new instance of the GetDengateHealthRequest class.
        /// </summary>
        internal GetDengateHealthRequest(NodeManagerDengateHealth dengateReqType, ulong physicalAddress)
        {
            this.dengateReqType = (byte)dengateReqType;

            this.physicalAddress = BitConverter.GetBytes(physicalAddress);
        }

        /// <summary>
        /// Initializes a new instance of the GetDengateHealthRequest class.
        /// </summary>
        internal GetDengateHealthRequest(NodeManagerDengateHealth dengateReqType, ushort tag, ulong physicalAddress)
            : this(dengateReqType, BitConverter.GetBytes(tag), physicalAddress)
        {
        }

        /// <summary>
        /// Initializes a new instance of the GetDengateHealthRequest class.
        /// </summary>
        internal GetDengateHealthRequest(NodeManagerDengateHealth dengateReqType, byte[] tag, ulong physicalAddress)
            : this(dengateReqType, physicalAddress)
        {
            this.dengateReqType = (byte)dengateReqType;

            if(tag != null)
            {
                int lenght = tag.Length;
                
                if(lenght > this.tag.Length)
                    lenght = this.tag.Length;

                Buffer.BlockCopy(tag, 0, this.tag, 0, lenght);

                // Bit [15] � reserved � must be zero
                this.tag[1] = (byte)(this.tag[1] & 0x7F);
            }
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
        /// Tag: only relevant for Health Status Type = 3, 4, 5, 
        /// and 6 (shall always be set to zero for other Health Status Type 
        /// values)
        /// Bit [15] � reserved � must be zero
        /// </summary>
        [NodeManagerMessageData(3,2)]
        public byte[] Tag
        {
            get { return this.tag; }
        }

        /// <summary>
        /// Dengate Health Type
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte DengateReqType
        {
            get { return this.dengateReqType; }
        }

        /// <summary>
        /// Physical address: only relevant for Health Status 
        /// Type = 6 (reserved otherwise)
        /// </summary>
        [NodeManagerMessageData(6,8)]
        public byte[] PhysicalAddress
        {
            get { return this.physicalAddress; }
        }

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        [NodeManagerMessageData(14)]
        public byte Reserved
        {
            get { return this.reserved; }
        }

    }
}
