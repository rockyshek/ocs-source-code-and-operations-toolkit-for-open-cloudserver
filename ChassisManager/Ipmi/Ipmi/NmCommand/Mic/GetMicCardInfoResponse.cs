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

    using System.Collections.Generic;

    /// <summary>
    /// Represents the Node Manager 'Get MIC Card Info' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.Application, NodeManagerCommand.GetMicCardInfo)]
    public class GetMicCardInfoResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        ///  Total number of MIC devices detected.
        ///  The following bytes are only returned if the specified 
        ///  management-capable card is detected by the BMC.
        /// </summary>
        private byte micDevices;

        /// <summary>
        /// Command Protocol Detection Support
        /// [7:4 ] � Reserved
        /// [3] � MCTP over SMBus
        /// [2] � IPMI on PCIe SMBus (refer to IPMI 2.0 spec)
        /// [1] � IPMB
        /// [0] � Unknown
        /// </summary>
        private byte protocolSupport;

        /// <summary>
        /// Command Protocols Supported by Card
        /// [7:4 ] � Reserved
        /// [3] � MCTP over SMBus
        /// [2] � IPMI on PCIe SMBus
        /// [1] � IPMB
        /// [0] � Unknown
        /// </summary>
        private byte cardProtocolSupport;

        /// <summary>
        /// List derrived from protocolSupport
        /// </summary>
        private List<NodeManagerMicProtocol> commandProtocolDetection = new List<NodeManagerMicProtocol>();

        /// <summary>
        /// List derrived from cardProtocolSupport
        /// </summary>
        private List<NodeManagerMicProtocol> commandProtocolCard = new List<NodeManagerMicProtocol>();

        /// <summary>
        /// Address/Protocol/Bus#
        /// [7:6] Address Type
        ///     00b � Bus/Slot/Address
        /// [3:0] Bus Number � Identifies SMBus interface on which 
        /// the MIC device was detected
        /// </summary>
        private byte address;

        /// <summary>
        /// Slot Number � identifies PCIe slot in which the MIC 
        /// device is inserted.
        /// </summary>
        private byte slotNumber;

        /// <summary>
        /// Slave Address - the I2C slave address (8 bit �write� 
        /// address) of the MIC device
        /// </summary>
        private byte slaveAddress;

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
        ///  Total number of MIC devices detected.
        ///  The following bytes are only returned if the specified 
        ///  management-capable card is detected by the BMC.
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte MicDeviceCount
        {
            get { return this.micDevices; }
            set { this.micDevices = value; }
        }

        /// <summary>
        /// Command Protocol Detection Support
        /// [7:4 ] � Reserved
        /// [3] � MCTP over SMBus
        /// [2] � IPMI on PCIe SMBus (refer to IPMI 2.0 spec)
        /// [1] � IPMB
        /// [0] � Unknown
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte RawProtocolSupport
        {
            get { return this.protocolSupport; }
            set { this.protocolSupport = value; }
        }

        /// <summary>
        /// Command Protocols Supported by Card
        /// [7:4 ] � Reserved
        /// [3] � MCTP over SMBus
        /// [2] � IPMI on PCIe SMBus
        /// [1] � IPMB
        /// [0] � Unknown
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte RawCardProtocolSupport
        {
            get { return this.cardProtocolSupport; }
            set { this.cardProtocolSupport = value; }
        }

        /// <summary>
        /// Address/Protocol/Bus#
        /// [7:6] Address Type
        ///     00b � Bus/Slot/Address
        /// [3:0] Bus Number � Identifies SMBus interface on which 
        /// the MIC device was detected
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte RawAddress
        {
            get { return this.address; }
            set { this.address = value; }
        }

        /// <summary>
        /// Slot Number � identifies PCIe slot in which the MIC 
        /// device is inserted.
        /// </summary>
        [NodeManagerMessageData(7)]
        public byte SlotNumber
        {
            get { return this.slotNumber; }
            set { this.slotNumber = value; }
        }

        /// <summary>
        /// Slave Address - the I2C slave address (8 bit �write� 
        /// address) of the MIC device
        /// </summary>
        [NodeManagerMessageData(8)]
        public byte SlaveAddress
        {
            get { return this.slaveAddress; }
            set { this.slaveAddress = value; }
        }

        /// <summary>
        /// Address Type
        /// </summary>
        public byte AddressType
        {
            get
            {
                // [7:6] Address Type
                return (byte)((this.address >> 6) & 0x03);
            }
        }

        /// <summary>
        /// Bus Number � Identifies SMBus interface on which 
        /// the MIC device was detected 
        /// </summary>
        public byte BusNumber
        {
            get
            {
                // [3:0] Bus Number
                return (byte)(this.address & 0x0F);
            }
        }

        /// <summary>
        /// Command Protocol Detection Support
        /// </summary>
        internal List<NodeManagerMicProtocol> CommandProtocolDetection
        {
            get {
                    return GetCommandProtocolDetection(); 
                }
        }

        /// <summary>
        /// Command Protocol Card Support
        /// </summary>
        internal List<NodeManagerMicProtocol> CommandProtocolCard
        {
            get
            {
                return GetCommandProtocolCard();
            }
        }


        /// <summary>
        /// Populate the list of Command Protocol Detection Support
        /// </summary>
        /// <returns></returns>
        private List<NodeManagerMicProtocol> GetCommandProtocolDetection()
        {
            // if count, greater than zero the function has already been called.
            if (this.commandProtocolDetection.Count == 0)
            {
                if ((byte)(this.protocolSupport & 0x01) == 0x01)
                    this.commandProtocolDetection.Add(NodeManagerMicProtocol.Unknown);
                
                if ((byte)(this.protocolSupport & 0x02) == 0x02)
                    this.commandProtocolDetection.Add(NodeManagerMicProtocol.IPMB);

                if ((byte)(this.protocolSupport & 0x04) == 0x04)
                    this.commandProtocolDetection.Add(NodeManagerMicProtocol.IPMI_PCIe_SMBus);

                if ((byte)(this.protocolSupport & 0x08) == 0x08)
                    this.commandProtocolDetection.Add(NodeManagerMicProtocol.MCTP_SMBus);
            }

            return this.commandProtocolDetection; 
        }

        /// <summary>
        /// Populate the list of Command Protocols Supported by Card
        /// </summary>
        /// <returns></returns>
        private List<NodeManagerMicProtocol> GetCommandProtocolCard()
        {
            // if count, greater than zero the function has already been called.
            if (this.commandProtocolCard.Count == 0)
            {
                if ((byte)(this.cardProtocolSupport & 0x01) == 0x01)
                    this.commandProtocolCard.Add(NodeManagerMicProtocol.Unknown);

                if ((byte)(this.cardProtocolSupport & 0x02) == 0x02)
                    this.commandProtocolCard.Add(NodeManagerMicProtocol.IPMB);

                if ((byte)(this.cardProtocolSupport & 0x04) == 0x04)
                    this.commandProtocolCard.Add(NodeManagerMicProtocol.IPMI_PCIe_SMBus);

                if ((byte)(this.cardProtocolSupport & 0x08) == 0x08)
                    this.commandProtocolCard.Add(NodeManagerMicProtocol.MCTP_SMBus);
            }

            return this.commandProtocolDetection;
        }



    }
}
