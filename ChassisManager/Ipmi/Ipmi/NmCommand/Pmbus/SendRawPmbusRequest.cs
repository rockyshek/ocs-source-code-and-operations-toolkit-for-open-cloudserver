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
    /// Represents the Node Manager 'Send Raw PMBUS' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.SendRawPmbus)]
    public abstract class SendRawPmbusBase : NodeManagerRequest
    {

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Byte 4 � Flags 
        /// [7] = 1b � Enable PEC.
        /// [6] = 1b � Do not report PEC errors in Completion Code.
        /// [5:4] � Device address format. 
        ///     0h � Standard device address
        ///     1h � Extended device address
        ///     Other � reserved. 
        /// [3:1] � SMBUS message transaction type
        ///     0h � SEND_BYTE.
        ///     1h � READ_BYTE.
        ///     2h � WRITE_BYTE.
        ///     3h � READ_WORD.
        ///     4h � WRITE_WORD.
        ///     5h � BLOCK_READ.
        ///     6h � BLOCK_WRITE.
        ///     7h � BLOCK_WRITE_READ_PROC_CALL.
        /// [0] � Reserved. Write as 0b
        /// </summary>
        private byte flags;

        /// <summary>
        /// Target PSU Address 
        /// [7:1] �  7-bit PSU SMBUS address
        /// [0] � reserved should be set to 0
        /// </summary>
        protected byte targetPsuAddress;

        /// <summary>
        /// MUX PSU address 
        /// [0:5] = Mux address 
        /// [6:7] = Reserved. Write as 00000b
        /// </summary>
        protected byte muxPsuAddress;

        /// <summary>
        /// Byte 7 Reserved for standard 
        /// Byte 10 Reserved for extended
        /// </summary>
        protected readonly byte reserved = 0x00;

        /// <summary>
        /// Write Length
        /// </summary>
        protected byte writeLenght;

        /// <summary>
        /// Read Length. This filed is used to validate if the slave 
        /// returns proper number of bytes
        /// </summary>
        protected byte readLenght;

        /// <summary>
        /// PMBUS command
        /// </summary>
        protected byte[] pmBusCommand;

        /// <summary>
        /// Initializes a new instance of the SendRawPmBusRequest class.
        /// </summary>
        internal SendRawPmbusBase(bool enablePec, NodeManagerSmbusTransactionType transactionType, 
            byte psuAddress, byte writeLenght, byte readLenght, byte[] pmbusCmd, byte addressFormat)
        {
            if (enablePec)
                this.flags = 0xC0;

            this.flags = (byte)(this.flags & addressFormat);

            // Flags: [3:1] � SMBUS message transaction type
            // The NodeManagerSmbusTransactionType enum has already been bit shifted.
            this.flags = (byte)(this.flags & (byte)transactionType);

            // Target PSU Address 
            // [7:1] �  7-bit PSU SMBUS address
            // [0] � reserved should be set to 0
            this.targetPsuAddress = (byte)(psuAddress << 1);

            // Write Length
            this.writeLenght = writeLenght;

            // Read Length
            this.readLenght = readLenght;

            // PMBUS Command
            if (pmbusCmd != null)
                this.pmBusCommand = pmbusCmd;
            else
                this.pmBusCommand = new byte[0];

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
        /// Byte 4 � Flags 
        /// [7] = 1b � Enable PEC.
        /// [6] = 1b � Do not report PEC errors in Completion Code.
        /// [5:4] � Device address format. 
        ///     0h � Standard device address
        ///     1h � Extended device address
        ///     Other � reserved. 
        /// [3:1] � SMBUS message transaction type
        ///     0h � SEND_BYTE.
        ///     1h � READ_BYTE.
        ///     2h � WRITE_BYTE.
        ///     3h � READ_WORD.
        ///     4h � WRITE_WORD.
        ///     5h � BLOCK_READ.
        ///     6h � BLOCK_WRITE.
        ///     7h � BLOCK_WRITE_READ_PROC_CALL.
        /// [0] � Reserved. Write as 0b
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte Flags
        {
            get { return this.flags; }
        }
    }

    /// <summary>
    /// Represents the Node Manager 'Send Raw PMBUS' request message for standard PMBUS addresses.
    /// </summary>
    public class SendRawPmbusStandardRequest : SendRawPmbusBase
    {

        /// <summary>
        /// Flags byte: [5:4] � Device address format. 
        /// 0h � Standard device address
        /// 1h � Extended device address
        /// </summary>
        private const byte standardFormat = 0x00;
    
        /// <summary>
        /// Initializes a new instance of the SendRawPmbusStandardRequest class.
        /// </summary>
        internal SendRawPmbusStandardRequest(bool enablePec, NodeManagerSmbusTransactionType transactionType, 
            byte psuAddress, byte muxPsuAddress, byte writeLenght, byte readLenght, byte[] pmbusCmd)
            : base(enablePec, transactionType, psuAddress, writeLenght, readLenght, pmbusCmd, standardFormat)
        {
            // MUX PSU address 
            // [0:5] = Mux address 
            // [6:7] = Reserved. Write as 00000b
            base.muxPsuAddress = (byte)(muxPsuAddress & 0x3F);
        }

        /// <summary>
        /// Target PSU Address 
        /// [7:1] �  7-bit PSU SMBUS address
        /// [0] � reserved should be set to 0
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte TargetPsuAddress
        {
            get { return base.targetPsuAddress; }
        }

        /// <summary>
        /// MUX PSU address 
        /// [0:5] = Mux address 
        /// [6:7] = Reserved. Write as 00000b
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte MuxPsuAddress
        {
            get { return base.muxPsuAddress; }
        }

        /// <summary>
        /// Reserved
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte Reserved
        {
            get { return base.reserved; }
        }

        /// <summary>
        /// Write Lenght
        /// </summary>
        [NodeManagerMessageData(7)]
        public byte WriteLenght
        {
            get { return base.writeLenght; }
        }

        /// <summary>
        /// Read Lenght
        /// </summary>
        [NodeManagerMessageData(8)]
        public byte ReadLenght
        {
            get { return base.readLenght; }
        }

        /// <summary>
        /// PMBUS command
        /// </summary>
        [NodeManagerMessageData(9)]
        public byte[] PmbusCommand
        {
            get { return base.pmBusCommand; }
        }
    
    }

    /// <summary>
    /// Represents the Node Manager 'Send Raw PMBUS' request message for extended PMBUS addresses.
    /// </summary>
    public class SendRawPmbusExtendedRequest : SendRawPmbusBase
    {

        /// <summary>
        /// Flags byte: [5:4] � Device address format. 
        /// 0h � Standard device address
        /// 1h � Extended device address
        /// </summary>
        private const byte extendedFormat = 0x30;

        /// <summary>
        /// Sensor Bus 
        /// 00h  SMBUS  
        /// 01h SMLINK0 
        /// 02h SMLINK1 
        /// 03h SMLINK2 
        /// 04h SMLINK3 
        /// 05h SMLINK4 
        /// Other - reserved
        /// </summary>
        private byte sensorBus;

        /// <summary>
        /// MUX channel selection   
        /// This field indicates which lines of MUX should be enabled
        /// </summary>
        private byte muxChannel;

        /// <summary>
        /// MUX configuration state 
        /// [0] � MUX support 
        ///  0 � ignore MUX configuration (MUX not present) 
        ///  1 � use MUX configuration 
        /// </summary>
        private byte configureMux;
   
        /// <summary>
        /// Initializes a new instance of the SendRawPmbusStandardRequest class.
        /// </summary>
        internal SendRawPmbusExtendedRequest(bool enablePec, NodeManagerSmbusTransactionType transactionType, NodeManagerSensorBus sensorBus, 
            byte psuAddress, byte muxPsuAddress, byte muxChannel, bool configureMux, byte writeLenght, byte readLenght, byte[] pmbusCmd)
            : base(enablePec, transactionType, psuAddress, writeLenght, readLenght, pmbusCmd, extendedFormat)
        {
            // Sensor Bus 
            this.sensorBus = (byte)sensorBus;

            // 7-bit SMBUS address for SMBUS MUX or 0 for MGPIO controlled.  
            base.muxPsuAddress = (byte)(muxPsuAddress << 1);

            // MUX channel selection 
            this.muxChannel = muxChannel;

            if (configureMux)
                this.configureMux = 0x01;
        }

        /// <summary>
        /// Sensor Bus 
        /// 00h  SMBUS  
        /// 01h SMLINK0 
        /// 02h SMLINK1 
        /// 03h SMLINK2 
        /// 04h SMLINK3 
        /// 05h SMLINK4 
        /// Other - reserved
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte SensorBus
        {
            get { return this.sensorBus; }
        }

        /// <summary>
        /// Target PSU Address 
        /// [7:1] � 7-bit SMBUS address.
        /// [0] � Reserved. Write as 0b.
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte TargetPsuAddress
        {
            get { return base.targetPsuAddress; }
        }

        /// <summary>
        /// MUX Address
        /// [7:1] � 7-bit SMBUS address for SMBUS MUX or 0 for 
        /// MGPIO controlled.
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte MuxPsuAddress
        {
            get { return base.muxPsuAddress; }
        }

        /// <summary>
        /// MUX channel selection
        /// This field indicates which lines of MUX should be enabled
        /// </summary>
        [NodeManagerMessageData(7)]
        public byte MuxChannel
        {
            get { return this.muxChannel; }
        }

        /// <summary>
        ///MUX configuration state 
        /// [0] � MUX support 
        ///  0 � ignore MUX configuration (MUX not present) 
        ///  1 � use MUX configuration 
        /// </summary>
        [NodeManagerMessageData(8)]
        public byte MuxConfiguration
        {
            get { return this.configureMux; }
        }

        /// <summary>
        /// Reserved
        /// </summary>
        [NodeManagerMessageData(9)]
        public byte Reserved
        {
            get { return base.reserved; }
        }

        /// <summary>
        /// Write Lenght
        /// </summary>
        [NodeManagerMessageData(10)]
        public byte WriteLenght
        {
            get { return base.writeLenght; }
        }

        /// <summary>
        /// Read Lenght
        /// </summary>
        [NodeManagerMessageData(11)]
        public byte ReadLenght
        {
            get { return base.readLenght; }
        }

        /// <summary>
        /// PMBUS command
        /// </summary>
        [NodeManagerMessageData(12)]
        public byte[] PmbusCommand
        {
            get { return base.pmBusCommand; }
        }


    
    }
}
