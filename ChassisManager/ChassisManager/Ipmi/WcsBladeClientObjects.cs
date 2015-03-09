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

namespace Microsoft.GFS.WCS.ChassisManager
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    /// <summary>
    /// Respone to GetbladeInfo
    /// </summary>
    public class BladeStatusInfo
    {
        /// <summary>
        /// Completion Code
        /// </summary>
        private byte completionCode;

        /// <summary>
        /// Blade Type
        /// </summary>
        private string bladeType;

        /// <summary>
        /// Blade Device Id
        /// </summary>
        private byte deviceId;

        /// <summary>
        /// blade Guid
        /// </summary>
        private Guid bladeGuid;

        /// <summary>
        /// blade Name
        /// </summary>
        private string bladeName = string.Empty;

        /// <summary>
        /// Power State
        /// </summary>
        private string powerState = string.Empty;

        /// <summary>
        /// blade Firmware
        /// </summary>
        private string bladeFirmware = string.Empty;

        /// <summary>
        /// Hardware Version
        /// </summary>
        private string hardware = string.Empty;

        /// <summary>
        /// Bmc Firmware
        /// </summary>
        private string bmcFirmware = string.Empty;

        /// <summary>
        /// blade Serial No
        /// </summary>
        private string serialNo = string.Empty;

        /// <summary>
        /// blade Asset Tag
        /// </summary>
        private string assetTag = string.Empty;

        /// <summary>
        /// Protuct Type
        /// </summary>
        private string product = string.Empty;

        /// <summary>
        /// blade Location
        /// </summary>
        private string location = string.Empty;

        /// <summary>
        /// Blade Led Status
        /// </summary>
        private string ledStatus = string.Empty;

        /// <summary>
        /// Initialize the blade Info Class
        /// </summary>
        public BladeStatusInfo(byte completionCode)
        {
            this.completionCode = completionCode;
        }

        /// <summary>
        /// Completion Code
        /// </summary>
        public byte CompletionCode
        {
            get { return this.completionCode; }
            internal set { this.completionCode = value; }
        }

        /// <summary>
        /// Device Id
        /// </summary>
        public byte DeviceId
        {
            get { return this.deviceId; }
            internal set { this.deviceId = value; }
        }

        /// <summary>
        /// Blade Type
        /// </summary>
        public string BladeType
        {
            get { return this.bladeType; }
            internal set { this.bladeType = value; }
        }

        /// <summary>
        /// Blade Guid
        /// </summary>
        public Guid BladeGuid
        {
            get { return this.bladeGuid; }
            internal set { this.bladeGuid = value; }
        }

        /// <summary>
        /// 
        /// Blade Name
        /// </summary>
        public string BladeName
        {
            get { return this.bladeName; }
            internal set { this.bladeName = value; }
        }

        /// <summary>
        /// Blade Power State (On/Off)
        /// </summary>
        public string PowerState
        {
            get { return this.powerState; }
            internal set { this.powerState = value; }
        }

        /// <summary>
        /// BMC Firmware Version
        /// </summary>
        public string BmcFirmware
        {
            get { return this.bmcFirmware; }
            internal set { this.bmcFirmware = value; }
        }

        /// <summary>
        /// Blade Baseboard Serial Number
        /// </summary>
        public string SerialNumber
        {
            get { return this.serialNo; }
            internal set { this.serialNo = value; }
        }

        /// <summary>
        /// Blade Asset Tag
        /// </summary>
        public string AssetTag
        {
            get { return this.assetTag; }
            internal set { this.assetTag = value; }
        }

        /// <summary>
        /// Protuct Type
        /// </summary>
        public string ProductType
        {
            get { return this.product; }
            internal set { this.product = value; }
        }

        /// <summary>
        /// blade Location
        /// </summary>
        public string Location
        {
            get { return this.location; }
            internal set { this.location = value; }
        }

        /// <summary>
        /// Blade Led status On
        /// </summary>
        public string LedStatus
        {
            get { return this.ledStatus; }
            internal set { this.ledStatus = value; }
        }

        public string HardwareVersion
        {
            get { return this.hardware; }
            internal set { this.hardware = value; }
        }

    }

    /// <summary>
    /// Hardware Status Base Class
    /// </summary>
    public abstract class HardwareStatus
    {
        /// <summary>
        /// Blade Type
        /// </summary>
        private string bladeType;

        /// <summary>
        /// Blade Device Id
        /// </summary>
        private byte deviceId;

        /// <summary>
        /// command completion code
        /// </summary>
        private byte completionCode;

        /// <summary>
        /// command partial error code
        /// </summary>
        private byte partialError;

        /// <summary>
        /// Hardware Version
        /// </summary>
        private string hardware = string.Empty;

        /// <summary>
        /// blade Serial No
        /// </summary>
        private string serialNo = string.Empty;

        /// <summary>
        /// blade Asset Tag
        /// </summary>
        private string assetTag = string.Empty;

        /// <summary>
        /// Protuct Type
        /// </summary>
        private string product = string.Empty;

        /// <summary>
        /// blade Guid
        /// </summary>
        private Guid bladeGuid;

        /// <summary>
        /// Indicates processor information
        /// is contained within the response
        /// </summary>
        protected bool hasProc;

        /// <summary>
        /// Indicates memory information
        /// is contained within the response
        /// </summary>
        protected bool hasMem;

        /// <summary>
        /// Indicates FRU information
        /// is contained within the response
        /// </summary>
        protected bool hasFru;

        /// <summary>
        /// Indicates disk information
        /// is contained within the response
        /// </summary>
        protected bool hasDisk;

        /// <summary>
        /// Indicates pcie and other HW information
        /// is contained within the response
        /// </summary>
        protected bool hasMisc;

        /// <summary>
        /// Initialize the Exception Hardware Status Class
        /// </summary>
        public HardwareStatus(byte completionCode)
        {
            this.completionCode = completionCode;
        }

        /// <summary>
        /// Completion Code
        /// </summary>
        public byte CompletionCode
        {
            get { return this.completionCode; }
            private set { this.completionCode = value; }
        }

        /// <summary>
        /// Partial Error
        /// </summary>
        public byte PartialError
        {
            get { return this.partialError; }
            internal set { this.partialError = value; }
        }

        /// <summary>
        /// Device Id
        /// </summary>
        public byte DeviceId
        {
            get { return this.deviceId; }
            internal set { this.deviceId = value; }
        }

        /// <summary>
        /// Blade Type
        /// </summary>
        public string BladeType
        {
            get { return this.bladeType; }
            internal set { this.bladeType = value; }
        }

        /// <summary>
        /// Blade Guid
        /// </summary>
        public Guid BladeGuid
        {
            get { return this.bladeGuid; }
            internal set { this.bladeGuid = value; }
        }

        /// <summary>
        /// Blade Baseboard Serial Number
        /// </summary>
        public string SerialNumber
        {
            get { return this.serialNo; }
            internal set { this.serialNo = value; }
        }

        /// <summary>
        /// Blade Asset Tag
        /// </summary>
        public string AssetTag
        {
            get { return this.assetTag; }
            internal set { this.assetTag = value; }
        }

        /// <summary>
        /// Protuct Type
        /// </summary>
        public string ProductType
        {
            get { return this.product; }
            internal set { this.product = value; }
        }

        /// <summary>
        /// Blade Hardware Version
        /// </summary>
        public string HardwareVersion
        {
            get { return this.hardware; }
            internal set { this.hardware = value; }
        }

    }

    /// <summary>
    /// Provides Quick overview of Compute
    /// blade hardware status
    /// </summary>
    public class ComputeStatus : HardwareStatus
    {

        /// <summary>
        /// MemoryInfo collection
        /// </summary>
        private Dictionary<byte, MemoryInfo> meminfo = new Dictionary<byte, MemoryInfo>();

        /// <summary>
        /// ProcessorInfo collection
        /// </summary>
        private Dictionary<byte, ProcessorInfo> procinfo = new Dictionary<byte, ProcessorInfo>();

        /// <summary>
        /// PCIeInfo collection
        /// </summary>
        private Dictionary<byte, PCIeInfo> pcieinfo = new Dictionary<byte, PCIeInfo>();

        /// <summary>
        /// Temp Sensor Reading Collection
        /// </summary>
        private List<HardwareSensor> tempSensors = new List<HardwareSensor>();

        /// <summary>
        /// Hardware Sensor Reading Collection
        /// </summary>
        private List<HardwareSensor> hwSensors = new List<HardwareSensor>();

        /// <summary>
        /// Intel ME Sensor Reading Collection
        /// </summary>
        private List<HardwareSensor> mgmtEngine = new List<HardwareSensor>();

        /// <summary>
        /// Blade Power Reading
        /// </summary>
        private PowerReading power;

         /// <summary>
        /// Initialize the Exception Hardware Status Class
        /// </summary>
        public ComputeStatus(byte completionCode) 
            : base(completionCode)
        {
            base.hasProc = false;
            base.hasMem = false;
            base.hasFru = false;
            base.hasMisc = false;
        }
       
        /// <summary>
        /// Initialize the Hardware Status Class
        /// </summary>
        public ComputeStatus(byte completionCode, bool proc, bool mem, bool fru, bool misc)
            : base(completionCode)
        {
            base.hasProc = proc;
            base.hasMem = mem;
            base.hasFru = fru;
            base.hasMisc = misc;
        }

        /// <summary>
        /// Blade Memory Information
        /// </summary>
        public Dictionary<byte, MemoryInfo> MemInfo
        { get { return this.meminfo; } }

        /// <summary>
        /// Blade Processor Information
        /// </summary>
        public Dictionary<byte, ProcessorInfo> ProcInfo
        { get { return this.procinfo; } }

        /// <summary>
        /// Blade PCIeInfo Information
        /// </summary>
        public Dictionary<byte, PCIeInfo> PcieInfo
        { get { return this.pcieinfo; } }

        /// <summary>
        /// Blade Temp Sensor Information
        /// </summary>
        public List<HardwareSensor> TempSensors
        { get { return this.tempSensors; } }

        /// <summary>
        /// Hardware Sensor Readings
        /// </summary>
        public List<HardwareSensor> HardwareSdr
        { get { return this.hwSensors; } }

        /// <summary>
        /// Intel ME Sensor Readings
        /// </summary>
        public List<HardwareSensor> ManagementEngine
        { get { return this.mgmtEngine; } }

        /// <summary>
        /// Blade Power Reading
        /// </summary>
        public PowerReading Power
        {   
            get { return this.power; }
            internal set { this.power = value; }
        }

    }

    /// <summary>
    /// Provides Quick overview of Jbod Hardware
    /// status
    /// </summary>
    public class JbodStatus : HardwareStatus
    {
        // disk info with default constructor
        private DiskInformation _diskInfo = new DiskInformation(0xA1);
        // disk info with default constructor
        private DiskStatusInfo _diskStatus = new DiskStatusInfo(0xA1);

        /// <summary>
        /// Initialize the Hardware Status Class
        /// </summary>
        public JbodStatus(byte completionCode)
            : base(completionCode)
        {
            base.hasProc = false;
            base.hasMem = false;
            base.hasDisk = false;
            base.hasFru = false;
            base.hasMisc = false;
        }

        /// <summary>
        /// Initialize the Hardware Status Class
        /// </summary>
        public JbodStatus(byte completionCode, bool proc, bool mem,
           bool disk, bool fru, bool misc)
            : base(completionCode)
        {
            base.hasProc = false; // not support on Jbod
            base.hasMem = false;  // not support on Jbod
            base.hasDisk = disk;
            base.hasFru = fru;
            base.hasMisc = misc;
        }

        public DiskInformation DiskInfo
        {
            get { return this._diskInfo; }
            internal set { this._diskInfo = value; }
        }

        public DiskStatusInfo DiskStatus
        {
            get { return this._diskStatus; }
            internal set { this._diskStatus = value; }
        }

    }

    /// <summary>
    /// Initialize the UnknownBlade Hardware Status Class
    /// </summary>
    public class UnknownBlade : HardwareStatus
    { 
        /// <summary>
        /// Initialize the UnknownBlade Hardware Status Class
        /// </summary>
        public UnknownBlade(byte completionCode)
            : base(completionCode)
        {
            base.hasProc = false;
            base.hasMem = false;
            base.hasDisk = false;
            base.hasFru = false;
            base.hasMisc = false;
        }
    }

    /// <summary>
    /// Hardware Sensor Reading.
    /// </summary>
    public class HardwareSensor
    {
        internal HardwareSensor(SensorMetadataBase sdr, SensorReading reading)
        {
            this.Sdr = sdr;
            this.Reading = reading;
        }

        public readonly SensorMetadataBase Sdr;
        public readonly SensorReading Reading;
    }

    /// <summary>
    /// static support functions
    /// </summary>
    internal static class SupportFunctions
    { 
        /// <summary>
        /// Filters a list for distinct values
        /// </summary>
        internal static List<T> FilterDistinct<T>(List<T> inputList)
        {
            var uniqueValue = inputList.Distinct();

            return uniqueValue.ToList();
        }
    }

    /// <summary>
    /// Blade Type
    /// </summary>
    internal enum BladeType : byte
    {
        Unknown = 0x00,
        Server = 0x04,
        Jbod = 0x05,
        IEB = 0x06
    }

    public enum HardwareType
    { 
        All = 0,
        Processor = 1,
        Memory = 2,
        Nic = 3,
        Disk = 4,
        ME = 5
    }

    /// <summary>
    /// Offset deducted from inlet temprature sensor readings.
    /// </summary>
    internal class InletTempOffset
    {
        // exponent A
        private double expA;
        // exponent B
        private double expB;
        // exponent offset
        private double expOff;
        // power value
        private int pow;
        // indicates simple value
        private bool simple;

        internal InletTempOffset(double exponentA, int power, double exponentB, double offset, bool simple = false)
        {
            this.expA = exponentA;
            this.expB = exponentB;
            this.pow = power;
            this.expOff = offset;
            this.simple = simple;
        }

        internal double Offset
        {
            get {
                    if (this.simple)
                    {
                        if (HelperFunction.MaxPwmRequirement >= 90)
                            return this.expOff;
                        else
                            return (expA - (expB * HelperFunction.MaxPwmRequirement));
                    }
                    else
                    {
                        return ((expA * Math.Pow(HelperFunction.MaxPwmRequirement, pow)) - (expB * HelperFunction.MaxPwmRequirement)) + expOff;
                    }
                }
        }
    }

    /// <summary>
    /// ManufacturerId and Product Id of Blade
    /// </summary>
    internal struct WcsBladeIdentity
    {
        internal readonly uint ManufactureId;
        internal readonly ushort ProductId;

        internal WcsBladeIdentity(uint manufactureId, ushort productId)
        { 
            this.ManufactureId = manufactureId;
            this.ProductId = productId;
        }
    }

}
