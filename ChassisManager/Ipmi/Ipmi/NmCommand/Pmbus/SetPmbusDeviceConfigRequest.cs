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
    /// Represents the Node Manager 'Set PMBUS Device Configuration' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.Application, NodeManagerCommand.SetPmbusDeviceConfig)]
    public abstract class SetPmbusDeviceConfigBase : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Device Index
        /// [0:4] = PMBUS-enabled Device Index 
        /// [5] � Reserved. Write as 0.
        /// [7:6] � Device address format. 
        ///     0h � Standard device address
        ///     1h � Extended device address
        ///     3h � Common configuration
        ///     Other � reserved
        /// </summary>
        private byte devIndex;

        /// <summary>
        /// Initializes a new instance of the SetPmbusDeviceConfigBase class.
        /// </summary>
        internal SetPmbusDeviceConfigBase(byte deviceIndex, byte addressType)
        {
            // [0:4] = PMBUS-enabled Device Index 
            this.devIndex = deviceIndex;

            // [5] � Reserved. Write as 0.
            this.devIndex = (byte)(this.devIndex & 0x1F);

            // [7:6] � Device address format.
            this.devIndex = (byte)(this.devIndex | addressType);

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


    }

    /// <summary>
    /// Represents the Node Manager 'Set PMBUS Device Configuration' for standard PMBUS addresses.
    /// </summary>
    public class SetPmbusStandardDeviceConfigRequest : SetPmbusDeviceConfigBase
    {

        /// <summary>
        /// Device Index: [7:6] � Device address format. 
        /// 0h � Standard device address
        /// 1h � Extended device address
        /// 3h � Common configuration
        /// </summary>
        private const byte standardFormat = 0x00;

        /// <summary>
        /// SMBUS address.
        /// [0] � Reserved. Write as 0b.
        /// [1:7] � 7 bit PSU SMBUS address. Set to 00h if the device is not present
        /// </summary>
        private byte smbusAddress;

        /// <summary>
        ///  MUX Address
        ///  [0:5] = Mux Address
        ///  [6] � Disabled State
        ///     0 � PMBUS device is enabled and may be polled for readings by Intel NM
        ///     1 � PMBUS device is disabled and should not be poled for readings, but 
        ///     access to the device may be available using PMBUS Proxy 
        ///  [7] � Device Mode:
        ///     1 � the device is installed and lack of power readings should be 
        ///     reported to Management Console using Intel Node Manager Health Event
        ///     0 (default) � the device is installed or may be attached in the future
        /// </summary>
        private byte muxAddress;
                    
        /// <summary>
        /// Initializes a new instance of the SetPmbusStandardDeviceConfigRequest class.
        /// </summary>
        internal SetPmbusStandardDeviceConfigRequest(byte deviceIndex, byte smbusAddress, byte muxAddress, 
             bool disablePolling, bool reportHealth)
            : base(deviceIndex, standardFormat)
        {

            // SMBUS address.
            // [0] � Reserved. Write as 0b.
            // [1:7] � 7 bit PSU SMBUS address. Set to 00h if the device is not present
            this.smbusAddress = (byte)(smbusAddress << 1);

            // MUX address 
            // [0:5] = Mux address 
            this.muxAddress = (byte)(muxAddress & 0x3F);

            // [6] � Disabled State
            //  0 � PMBUS device is enabled and may be polled for readings by Intel NM
            //  1 � PMBUS device is disabled and should not be poled for readings
            if (disablePolling)
                this.muxAddress = (byte)(this.muxAddress | 0x40);

            // [7] � Device Mode:
            //  1 � the device is installed and lack of power readings should be 
            //    reported by Node Manager Health Event
            //  0 (default) � the device is installed or may be attached in the future
            if (reportHealth)
                this.muxAddress = (byte)(this.muxAddress | 0x80);

        }

        /// <summary>
        /// SMBUS address.
        /// [0] � Reserved. Write as 0b.
        /// [1:7] � 7 bit PSU SMBUS address. Set to 00h if the device is not present
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte SmbusAddress
        {
            get { return this.smbusAddress; }
        }

        /// <summary>
        ///  MUX Address
        ///  [0:5] = Mux Address
        ///  [6] � Disabled State
        ///     0 � PMBUS device is enabled and may be polled for readings by Intel NM
        ///     1 � PMBUS device is disabled and should not be poled for readings, but 
        ///     access to the device may be available using PMBUS Proxy 
        ///  [7] � Device Mode:
        ///     1 � the device is installed and lack of power readings should be 
        ///     reported to Management Console using Intel Node Manager Health Event
        ///     0 (default) � the device is installed or may be attached in the future
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte MuxAddress
        {
            get { return this.muxAddress; }
        }    
    }

    /// <summary>
    /// Represents the Node Manager 'Set PMBUS Device Configuration' for Extended PMBUS addresses.
    /// </summary>
    public class SetPmbusExtendedDeviceConfigRequest : SetPmbusDeviceConfigBase
    {

        /// <summary>
        /// Device Index: [7:6] � Device address format. 
        /// 0h � Standard device address
        /// 1h � Extended device address
        /// 3h � Common configuration
        /// </summary>
        private const byte extendedFormat = 0x40;

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
        /// target smbus Address 
        /// [7:1] � 7-bit SMBUS address.
        /// [0] � Reserved. Write as 0b.
        /// </summary>
        private byte smbusAddress;
        
        /// <summary>
        /// MUX Address 
        /// [7:1] � 7-bit SMBUS address for SMBUS MUX or 0 for MGPIO controlled.  
        /// [0] � Reserved. Write as 0b
        /// </summary>
        private byte muxAddress;

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
        /// Device configuration
        /// [0] � Disabled State
        ///  0 � PMBUS device is enabled and may be polled for readings
        ///  1 � PMBUS device is disabled and should not be poled for readings, 
        ///      but access to the device may be available using PMBUS Proxy
        /// [1] � Device Mode:
        ///  1 � the device is installed and lack of power readings should be 
        ///      reported to Management Console Node Manager Health Event
        ///  0   (default) � the device is installed or may be attached in the future.
        /// [7:2] = Reserved. Write as 000000b.
        /// </summary>
        private byte deviceConfig;
   
        /// <summary>
        /// Initializes a new instance of the SetPmbusExtendedDeviceConfigRequest class.
        /// </summary>
        internal SetPmbusExtendedDeviceConfigRequest(byte deviceIndex, NodeManagerSensorBus sensorBus, byte smbusAddress,
            byte muxAddress, byte muxChannel, bool useMuxConfig, bool disablePolling, bool reportHealth)
            : base(deviceIndex, extendedFormat)
        {
            // Sensor Bus 
            this.sensorBus = (byte)sensorBus;

            // [7:1] � 7-bit SMBUS address.
            // [0] � Reserved. Write as 0b 
            this.smbusAddress = (byte)(smbusAddress << 1);

            // MUX Address 
            // [7:1] � 7-bit SMBUS address for SMBUS MUX or 0 for MGPIO controlled.  
            // [0] � Reserved. Write as 0b
            this.muxAddress = (byte)(muxAddress << 1);

            // MUX channel selection 
            this.muxChannel = muxChannel;

            if (useMuxConfig)
                this.configureMux = 0x01;

            // Device configuration
            // [0] � Disabled State
            //  0 � PMBUS device is enabled and may be polled for readings
            //  1 � PMBUS device is disabled and should not be poled for readings, 
            //      but access to the device may be available using PMBUS Proxy
            if (disablePolling)
                this.deviceConfig = 0x01;

            // [1] � Device Mode:
            //  1 � the device is installed and lack of power readings should be 
            //      reported to Management Console Node Manager Health Event
            //  0   (default) � the device is installed or may be attached in the future.
            if (reportHealth)
                this.deviceConfig = (byte)(this.deviceConfig | 0x02);

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


        [NodeManagerMessageData(5)]
        public byte SmbusAddress
        {
            get { return this.smbusAddress; }
        }

        /// <summary>
        /// MUX Address 
        /// [7:1] � 7-bit SMBUS address for SMBUS MUX or 0 for MGPIO controlled.  
        /// [0] � Reserved. Write as 0b
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte MuxAddress
        {
            get { return this.muxAddress; }
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
        /// Device configuration
        /// [0] � Disabled State
        ///  0 � PMBUS device is enabled and may be polled for readings
        ///  1 � PMBUS device is disabled and should not be poled for readings, 
        ///      but access to the device may be available using PMBUS Proxy
        /// [1] � Device Mode:
        ///  1 � the device is installed and lack of power readings should be 
        ///      reported to Management Console Node Manager Health Event
        ///  0   (default) � the device is installed or may be attached in the future.
        /// [7:2] = Reserved. Write as 000000b.
        /// </summary>
        [NodeManagerMessageData(9)]
        public byte DeviceConfiguration
        {
            get { return this.deviceConfig; }
        }


    
    }

    /// <summary>
    /// Represents the Node Manager 'Set PMBUS Device Configuration' for Common PMBUS addresses.
    /// </summary>
    public class SetPmbusCommonDeviceConfigRequest : SetPmbusDeviceConfigBase
    {

        /// <summary>
        /// Device Index: [7:6] � Device address format. 
        /// 0h � Standard device address
        /// 1h � Extended device address
        /// 3h � Common configuration
        /// </summary>
        private const byte commonFormat = 0xC0;

        /// <summary>
        /// PSU redundancy mode
        /// 0b (default) - Full N+1 redundancy if this PSU is present (SmaRT 
        /// functionality will be automatically disabled if at least 2 PSUs are on) 
        /// 1b - nonredundant PSU 
        /// [7:1] = Reserved. Write as 0 
        /// </summary>
        private byte psuRedundancy;

        /// <summary>
        /// Reserved. Write as 00h.
        /// </summary>
        private const byte reserved = 0x00;

        /// <summary>
        /// Initializes a new instance of the SetPmbusCommonDeviceConfigRequest class.
        /// </summary>
        internal SetPmbusCommonDeviceConfigRequest(byte deviceIndex, bool redundantPsu)
            : base(deviceIndex, commonFormat)
        {

            // PSU redundancy mode
            // 0b (default) - Full N+1 redundancy if this PSU is present
            // 1b - nonredundant PSU 
            if (!redundantPsu)
                this.psuRedundancy = 0x01;
        }

        /// <summary>
        /// PSU redundancy mode
        ///     0b (default) - Full N+1 redundancy if this PSU is present
        ///     1b - nonredundant PSU 
        /// [7:1] = Reserved. Write as 0 
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte RedundantPsu
        {
            get { return this.psuRedundancy; }
        }

        /// <summary>
        /// Reserved. Write as 00h.
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte Reserved
        {
            get { return reserved; }
        }
    }
}
