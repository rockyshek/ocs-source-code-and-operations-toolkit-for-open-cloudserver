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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the IPMI 'Get FRU Data' application request message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Storage, IpmiCommand.ReadFruData)]
    internal class GetFruDataResponse : IpmiResponse
    {
        private byte countReturned;

        private byte[] dataReturned;


        /// <summary>
        /// Gets offset to read.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte CountReturned
        {
            get { return this.countReturned; }
            set { this.countReturned = value; }
        }

        /// <summary>
        /// Gets offset to read.
        /// </summary>       
        [IpmiMessageData(1)]
        public byte[] DataReturned
        {
            get { return this.dataReturned; }
            set { this.dataReturned = value; }
        }
    }

    #region FRU types structures

    /// <summary>
    /// FRU Common Header Class. Implements version 1
    /// </summary>
    public class FruCommonHeader
    {
        // size of the FRU common header
        internal const int DataSize = 8;
        const int ImplementedVersion = 1;
        const int CommonHeaderFormatVersionIndex = 0;
        const int ChassisAreaStartingOffsetIndex = 2;
        const int BoardAreaStartingOffsetIndex = 3;
        const int ProductInfoAreaStartingOffsetIndex = 4;
        const int MultiRecordAreaStartingOffsetIndex = 5;
        const int ChecksumIndex = 7;

        public FruCommonHeader(byte[] data)
        {
            // Only bits 0-3 define the version of the common header format
            this.Version = data[FruCommonHeader.CommonHeaderFormatVersionIndex] & 0xf;
            if (this.Version != FruCommonHeader.ImplementedVersion)
            {
                // fru version not supported
                this.CompletionCode = 0xD7;
            }
            else
            {
                this.CompletionCode = 0x00;
                this.ChassisInfoStartingOffset = (ushort)(data[FruCommonHeader.ChassisAreaStartingOffsetIndex] * 8);
                this.BoardAreaStartingOffset = (ushort)(data[FruCommonHeader.BoardAreaStartingOffsetIndex] * 8);
                this.ProductAreaStartingOffset = (ushort)(data[FruCommonHeader.ProductInfoAreaStartingOffsetIndex] * 8);
                this.MultiRecordAreaStartingOffset = (ushort)(data[FruCommonHeader.MultiRecordAreaStartingOffsetIndex] * 8);
                this.Checksum = data[FruCommonHeader.ChecksumIndex];
            }
        }

        public byte CompletionCode { get; protected set; }
        public int Version { get; protected set; }
        public int Checksum { get; protected set; }
        public ushort ChassisInfoStartingOffset { get; protected set; }
        public ushort BoardAreaStartingOffset { get; protected set; }
        public ushort ProductAreaStartingOffset { get; protected set; }
        public ushort MultiRecordAreaStartingOffset { get; protected set; }
    }

    public enum FruByteStringType
    {
        Binary = 0, // 00b
        BcdPlus = 1,    // 01b
        Packed6BitAscii = 2, // 10b
        Text = 3 // 11b ASCII or UNICODE based on the language type 
    }

    public class FruByteString
    {
        internal const byte defaultLang = 0; // default English
        internal const byte EnLang = 25;     // English
        internal byte Language { get; set; }
        internal ushort Length { get { return (ushort)this.Data.Length; } }
        public byte[] Data { get; private set; }
        public FruByteStringType Encoding { get; private set; }
        protected string Text { get; set; }

        #region Construction
        protected FruByteString(byte language, FruByteStringType encoding, byte[] data)
        {
            this.Language = language;
            this.Data = data;
            this.Encoding = encoding;

            Decode(language, data, encoding);
        }

        // default class constructor
        internal FruByteString()
        {
            this.Language = EnLang;
            this.Data = new byte[0];
            this.Encoding = FruByteStringType.Text;
        }

        /// <summary>
        /// Copy FRU String bytes out of Byte Array
        /// </summary>
        internal static FruByteString ReadByteString(byte language, byte[] data, int offset)
        {
            int nextOffset;
            return ReadByteString(language, data, offset, out nextOffset);
        }

        /// <summary>
        /// Copy FRU String bytes out of Byte Array
        /// </summary>
        internal static FruByteString ReadByteString(byte language, byte[] data, int offset, out int nextOffset)
        {
            /*
            The following presents the specification of the type/length byte.
                7:6 - type code
                    00 - binary or unspecified
                    01 - BCD plus (see below)
                    10 - 6-bit ASCII, packed (overrides Language Codes)
                    11 - Interpretation depends on Language Codes. 11b indicates 8-bit ASCII + Latin 1 if
                    the Language Code is English for the area or record containing the field, or 2-byte
                    UNICODE (least significant byte first) if the Language Code is not English. At least
                    two bytes of data must be present when this type is used. Therefore, the length
                    (number of data bytes) will always be >1 if data is present, 0 if data is not present.
                5:0 - number of data bytes.
                    000000 indicates that the field is empty. When the type code is 11b, a length of
                    000001 indicates 'end of fields'. I.e. Type/Length = C1h indicates 'end of fields'.             
            */
            byte lengthInfo = data[offset];
            int length = lengthInfo & 0x3f; // 0011 1111 
            FruByteStringType encoding = (FruByteStringType)(lengthInfo >> 6);
            nextOffset = offset + 1 + length;
            byte[] readData = new byte[length];
            if(data.Length >= nextOffset)
            Array.Copy(data, offset + 1, readData, 0, length);

            return new FruByteString(language, encoding, readData);
        }

        /// <summary>
        /// Return the length of the FRU string in bytes
        /// </summary>
        internal static int LengthInfo(byte lengthInfo)
        {
           // 5:0 - number of data bytes.
           // 000000 indicates that the field is empty. When the type code is 11b, a length of
           // 000001 indicates 'end of fields'. I.e. Type/Length = C1h indicates 'end of fields'
            return (lengthInfo & 0x3f); 
        }

        #endregion

        private void Decode(byte language, byte[] data, FruByteStringType encoding)
        {            
            switch (encoding)
            {
                case FruByteStringType.Binary:
                    this.Text = IpmiSharedFunc.ByteArrayToHexString(data);
                    break;
                case FruByteStringType.BcdPlus:
                    this.Text = IpmiSharedFunc.DecodeBcdPlus(data);
                    break;
                case FruByteStringType.Packed6BitAscii:
                    data = ReplaceNonAsciiChars(data);
                    this.Text = IpmiSharedFunc.DecodePacked6bitAscii(data);
                    break;
                case FruByteStringType.Text:
                    // replace non ASCII characters
                    data = ReplaceNonAsciiChars(data);
                    if ((this.Language == FruByteString.defaultLang) ||
                        (this.Language == FruByteString.EnLang))
                    {
                        this.Text = System.Text.Encoding.ASCII.GetString(data).Trim();
                    }
                    else
                    {
                        this.Text = System.Text.Encoding.Unicode.GetString(data).Trim();
                    }
                    break;
            }
        }

        /// <summary>
        /// Replace ASCII control characters in the ASCII string with zeros.
        /// Control characters cause issues when placing the strings other programmable
        /// scripts such as xml.
        /// </summary>
        private byte[] ReplaceNonAsciiChars(byte[] data)
        {          
            for (int i = 0; i < data.Length; i++)
            {
                // HEX 20 = ASCII space symbol
                if (data[i] < 0x20)
                    data[i] = 0x20;
            }

            return data;
        }

        public override string ToString()
        {
            if (this.Text != null)
                return this.Text;
            else
                return string.Empty;
        }
    }

    /// <summary>
    /// Fru Header Area.
    /// </summary>
    public abstract class FruArea
    {
        internal const byte MultiRecordId = 0xD5;
        const int ImplementedVersion = 1;
        const int VersionIndex = 0;
        const int AreaLengthIndex = 1;
        public int Version { get; protected set; }
        public int Checksum { get; protected set; }
        public byte CompletionCode { get; protected set; }

        public FruArea(byte[] data)
        {
            ReadCommonInfo(data);
        }

        protected void ReadCommonInfo(byte[] data)
        {
            if (data != null)
            {
                if (data.Length > 0)
                {

                    if (data[FruChassisInfo.VersionIndex] == MultiRecordId)
                    {
                        // Only bits 0-3 define the version of the common header format
                        this.Version = data[FruChassisInfo.VersionIndex + 1] & 0xf;
                    }
                    else
                    {
                        // Only bits 0-3 define the version of the common header format
                        this.Version = data[FruChassisInfo.VersionIndex] & 0xf;
                    }

                    // Last byte is the checksum
                    this.Checksum = data[data.Length - 1];
                }

                if (this.Version != FruChassisInfo.ImplementedVersion)
                {
                    CompletionCode = 0xCE;
                }
            }
        }

        static public ushort AreaLength(byte[] data)
        {
            if (data != null)
            {
                if (data[VersionIndex] != FruArea.ImplementedVersion
                    && data[VersionIndex] != FruArea.MultiRecordId)
                {
                    // if the version does not match, return a zero length
                    return 0;
                }

                if ((data[VersionIndex]) == FruArea.MultiRecordId)
                    return (ushort)(data[FruArea.AreaLengthIndex + 1] * 8);
                else
                    return (ushort)(data[FruArea.AreaLengthIndex] * 8);
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// FRU Chassis Info Area
    /// </summary>
    public class FruChassisInfo : FruArea
    {
        const int ChassisTypeIndex = 2;
        const int ChassisPartNumberIndex = 3;
        // Need to be calculated
        int serialNumberIndex = 0;

        public int ChassisType { get; private set; }
        public FruByteString PartNumber { get; private set; }
        public FruByteString SerialNumber { get; private set; }

        public FruChassisInfo(byte[] data)
            : base(data)
        {
            // default constructor
            this.PartNumber = new FruByteString();
            // default constructor
            this.SerialNumber = new FruByteString();

            if (data != null)
            {
                //Cassis type
                if(data.Length >= FruChassisInfo.ChassisTypeIndex)
                this.ChassisType = data[FruChassisInfo.ChassisTypeIndex];

                //Chassis part number
                if (data.Length > FruChassisInfo.ChassisPartNumberIndex)
                {
                    if(data.Length >= FruChassisInfo.ChassisPartNumberIndex + FruByteString.LengthInfo(data[FruChassisInfo.ChassisPartNumberIndex])) 
                    this.PartNumber = FruByteString.ReadByteString(FruByteString.defaultLang, data, FruChassisInfo.ChassisPartNumberIndex, out this.serialNumberIndex);
                }
                //Serial number
                if (data.Length > this.serialNumberIndex)
                {
                    if (data.Length >= this.serialNumberIndex + FruByteString.LengthInfo(data[this.serialNumberIndex]))
                    {
                        this.SerialNumber = FruByteString.ReadByteString(FruByteString.defaultLang, data, this.serialNumberIndex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// FRU Board Info Area
    /// </summary>
    public class FruBoardInfo : FruArea
    {
        const int LanguageCodeIndex = 2;
        const int MfgDateTimeIndex = 3;
        const int ManufacturerIndex = 6;
        int productNameIndex = 0;
        int serialNumberIndex = 0;
        int partNumberIndex = 0;
        int fruFileIdIndex = 0;

        public byte LanguageCode { get; private set; }
        public DateTime MfgDateTime { get; private set; }
        public FruByteString Maufacturer { get; private set; }
        public FruByteString ProductName { get; private set; }
        public FruByteString ProductPartNumber { get; private set; }
        public FruByteString SerialNumber { get; private set; }
        public FruByteString FruFileId { get; private set; }

        public FruBoardInfo(byte[] data)
            : base(data)
        {

            // default constructor
            this.MfgDateTime = new DateTime(1996, 1, 1);
            this.Maufacturer = new FruByteString();
            this.ProductName = new FruByteString();
            this.ProductPartNumber = new FruByteString();
            this.SerialNumber = new FruByteString();
            this.FruFileId = new FruByteString();

            if (data != null)
            {
                // Language code
                if(data.Length > FruBoardInfo.LanguageCodeIndex)
                    this.LanguageCode = data[FruBoardInfo.LanguageCodeIndex];

                if(data.Length > FruBoardInfo.MfgDateTimeIndex)
                {
                    // Mfg. Date / Time 
                    // Number of minutes from 0:00 hrs 1/1/96.
                    // LSbyte first (little endian)
                    long minutes = 0;
                    for (int i = 2; i >= 0; i--)
                    {
                        minutes = (minutes << 8) + data[FruBoardInfo.MfgDateTimeIndex + i];
                    }

                    // Time when computers started to be built ;)
                    DateTime mfgDataTime = new DateTime(1996, 1, 1);

                    // convert minutes in 100s of nanoseconds
                    long ticks = minutes * 60 * 1000 * 1000 * 10;
                    this.MfgDateTime = mfgDataTime.AddTicks(ticks);
                }
                // Board Manufacturer
                if (data.Length > FruBoardInfo.ManufacturerIndex)
                {
                    if (data.Length >= FruBoardInfo.ManufacturerIndex + FruByteString.LengthInfo(data[FruBoardInfo.ManufacturerIndex])) 
                    this.Maufacturer = FruByteString.ReadByteString(this.LanguageCode, data, FruBoardInfo.ManufacturerIndex, out this.productNameIndex);
                }
                // Board Product Name
                if (data.Length > this.productNameIndex)
                {
                    if (data.Length >= this.productNameIndex + FruByteString.LengthInfo(data[this.productNameIndex])) 
                    this.ProductName = FruByteString.ReadByteString(this.LanguageCode, data, this.productNameIndex, out this.serialNumberIndex);
                }
                // Board Serial Number
                if (data.Length > this.serialNumberIndex)
                {
                    if (data.Length >= this.serialNumberIndex + FruByteString.LengthInfo(data[this.serialNumberIndex]))
                        this.SerialNumber = FruByteString.ReadByteString(FruByteString.defaultLang, data, this.serialNumberIndex, out this.partNumberIndex);
                }
                // Board Part Number
                if (data.Length > this.partNumberIndex)
                {
                    if (data.Length >= this.partNumberIndex + FruByteString.LengthInfo(data[this.partNumberIndex]))
                    this.ProductPartNumber = FruByteString.ReadByteString(this.LanguageCode, data, this.partNumberIndex, out this.fruFileIdIndex);
                }
                // FRU File ID
                if (data.Length > this.fruFileIdIndex)
                {
                    if (data.Length >= this.fruFileIdIndex + FruByteString.LengthInfo(data[this.fruFileIdIndex]))
                    this.FruFileId = FruByteString.ReadByteString(FruByteString.defaultLang, data, this.fruFileIdIndex);
                }
            }
        }
    }

    /// <summary>
    /// Fru Product Info
    /// </summary>
    public class FruProductInfo : FruArea
    {
        const int LanguageCodeIndex = 2;
        const int ManufacturerIndex = 3;
        int productNameIndex = 0;
        int productPartModelNumberIndex = 0;
        int productVersionIndex = 0;
        int productSerialNumberIndex = 0;
        int assetTagIndex = 0;
        int fruFileIdIndex = 0;

        public byte LanguageCode { get; private set; }
        public FruByteString ManufacturerName { get; private set; }
        public FruByteString ProductName { get; private set; }
        public FruByteString PartModelNumber { get; private set; }
        public FruByteString ProductVersion { get; private set; }
        public FruByteString SerialNumber { get; private set; }
        public FruByteString AssetTag { get; private set; }
        public FruByteString FruFileId { get; private set; }

        public FruProductInfo(byte[] data)
            : base(data)
        {

            // default constructor
            this.ManufacturerName = new FruByteString();
            this.ProductName = new FruByteString();
            this.PartModelNumber = new FruByteString();
            this.ProductVersion = new FruByteString();
            this.SerialNumber = new FruByteString();
            this.AssetTag = new FruByteString();
            this.FruFileId = new FruByteString();

            if (data != null)
            {
                // Language code
                if(data.Length > FruProductInfo.LanguageCodeIndex)
                    this.LanguageCode = data[FruProductInfo.LanguageCodeIndex];

                // Product Manufacturer
                if (data.Length > FruProductInfo.ManufacturerIndex)
                {
                    if (data.Length >= FruProductInfo.ManufacturerIndex + FruByteString.LengthInfo(data[FruProductInfo.ManufacturerIndex]))
                        this.ManufacturerName = FruByteString.ReadByteString(this.LanguageCode, data, FruProductInfo.ManufacturerIndex, out this.productNameIndex);
                }
                // Product Name
                if (data.Length > this.productNameIndex)
                {
                    if (data.Length >= this.productNameIndex + FruByteString.LengthInfo(data[this.productNameIndex]))
                        this.ProductName = FruByteString.ReadByteString(this.LanguageCode, data, this.productNameIndex, out this.productPartModelNumberIndex);
                }
                // Product Part/Model Number
                if (data.Length > this.productPartModelNumberIndex)
                {
                    if (data.Length >= this.productPartModelNumberIndex + FruByteString.LengthInfo(data[this.productPartModelNumberIndex]))
                        this.PartModelNumber = FruByteString.ReadByteString(this.LanguageCode, data, this.productPartModelNumberIndex, out this.productVersionIndex);
                }
                // Product Version
                if (data.Length > this.productVersionIndex)
                {
                    if (data.Length >= this.productVersionIndex + FruByteString.LengthInfo(data[this.productVersionIndex]))
                        this.ProductVersion = FruByteString.ReadByteString(this.LanguageCode, data, this.productVersionIndex, out this.productSerialNumberIndex);
                }
                // Product Serial Number
                if (data.Length > this.productSerialNumberIndex)
                {
                    if (data.Length >= this.productSerialNumberIndex + FruByteString.LengthInfo(data[this.productSerialNumberIndex]))
                        this.SerialNumber = FruByteString.ReadByteString(FruByteString.defaultLang, data, this.productSerialNumberIndex, out this.assetTagIndex);
                }
                // Product Asset tag
                if (data.Length > this.assetTagIndex)
                {
                    if (data.Length >= this.assetTagIndex + FruByteString.LengthInfo(data[this.assetTagIndex]))
                        this.AssetTag = FruByteString.ReadByteString(this.LanguageCode, data, this.assetTagIndex, out this.fruFileIdIndex);
                }
                // FRU File ID
                if (data.Length > this.fruFileIdIndex)
                {
                    if (data.Length >= this.fruFileIdIndex + FruByteString.LengthInfo(data[this.fruFileIdIndex]))
                        this.FruFileId = FruByteString.ReadByteString(this.LanguageCode, data, this.fruFileIdIndex);
                }
            }
        }
    }

    /// <summary>
    /// Fru Multi Record Info
    /// Record Structure:
    ///     [0] Record Type Id, [1] Format, [2] Record Length, [3] Record Checksum
    ///     [4] Header Checksum, [ Data Record: [5-7] Manufacturer [8] Language Code
    ///     [9] FRU Writes Remaining [10-N] Record Data
    /// </summary>
    public class FruMultiRecordInfo : FruArea
    {
        // index offsets
        const int RecordTypeIdIndex = 0;
        const int RecordFormatIndex = 1;
        const int RecordLengthIndex = 2;
        const int RecordChecksumIndex = 3;
        const int HeaderChecksumIndex = 4;
        const int ManufacturerIndex = 5;
        const int ManufacturerLength = 3;
        const int LanguageCodeIndex = 8;
        const int WritesRemainingIndex = 9;
        const int MaximumFields = 25;
        
        // start of custom fields.
        private int fieldStartIndex = 10;

        // header size
        private int headerSize = 10;
        
        /// <summary>
        /// Manufacturer Id Length
        /// </summary>
        public int ManufacturerIdLen
        {
            get { return ManufacturerLength; }
        }

        /// <summary>
        /// Start index of record data field
        /// </summary>       
        public int FieldStartIndex
        {
            get { return this.fieldStartIndex; }
        }
        
        /// <summary>
        /// Header size in bytes
        /// </summary>       
        public int HeaderSize
        {
            get { return this.headerSize; }
        }

        public byte LanguageCode { get; private set; }
        public byte WritesRemaining { get; private set; }
        public byte RecordTypeId { get; private set; }
        public byte RecordFormat { get; private set; }
        public byte RecordLength { get; private set; }
        public string Manufacturer{ get; private set; }

        public List<FruByteString> Fields = new List<FruByteString>();

        public FruMultiRecordInfo(byte[] data)
            : base(data)
        {

            // default constructor
            this.Manufacturer = string.Empty;

            if (data != null)
            {
                if (data.Length >= HeaderSize) // header length is defined as 10.
                {

                    this.LanguageCode = data[FruMultiRecordInfo.LanguageCodeIndex];
                    this.WritesRemaining = data[FruMultiRecordInfo.WritesRemainingIndex];
                    this.RecordTypeId = data[FruMultiRecordInfo.RecordTypeIdIndex];
                    this.RecordFormat = data[FruMultiRecordInfo.RecordFormatIndex];
                    this.RecordLength = data[FruMultiRecordInfo.RecordLengthIndex];

                    byte[] manufactureArray = new byte[FruMultiRecordInfo.ManufacturerLength];
                    Array.Copy(data, FruMultiRecordInfo.ManufacturerIndex, manufactureArray, 0, FruMultiRecordInfo.ManufacturerLength);
                    this.Manufacturer = IpmiSharedFunc.ByteArrayToHexString(manufactureArray);

                    // add fields
                    if (data.Length > FieldStartIndex)
                    {
                        // start field offset
                        int fieldIndex = FieldStartIndex;

                        // abort counter.
                        int abortCnt = 0;

                        while (fieldIndex < data.Length || abortCnt > MaximumFields)
                        {
                            // data fields are limited to 63 bytes in length, as length is 6 bit encouded in FRU specification.
                            if (data.Length >= fieldIndex + FruByteString.LengthInfo(data[fieldIndex]) && data[fieldIndex] != 0xC1)
                            {
                                Fields.Add(FruByteString.ReadByteString(this.LanguageCode, data, fieldIndex, out fieldIndex));
                            }
                            else
                            {
                                // escape while as fru space has ended.
                                break;
                            }

                            abortCnt++;
                        }                   
                    }

                }
            }
        }
    }

    /// <summary>
    /// Fru Device
    /// </summary>
    public class FruDevice
    {
        private byte _completionCode;
        public int DeviceId { get; protected set; }
        public FruCommonHeader CommonHeader { get; internal set; }
        public FruChassisInfo ChassisInfo { get; protected set; }
        public FruBoardInfo BoardInfo { get; protected set; }
        public FruProductInfo ProductInfo { get; protected set; }
        public FruMultiRecordInfo MultiRecordInfo { get; protected set; }


        /// <summary>
        /// Ipmi Completion Code
        /// </summary>
        public byte CompletionCode
        {
            get {return this._completionCode; }
        }

        public FruDevice(byte completionCode)
        {
            this._completionCode = completionCode;
        }

        public FruDevice(int deviceId,
                            FruCommonHeader commonHeader,
                            byte[] chassisInfoValue,
                            byte[] boardInfoValue,
                            byte[] productInfoValue,
                            byte[] multiRecordInfoValue,
                            byte completionCode)
        {
            this.CommonHeader = commonHeader;
            this.DeviceId = deviceId;
            this._completionCode = completionCode;

            if (chassisInfoValue != null)
            {
                this.ChassisInfo = new FruChassisInfo(chassisInfoValue);
            }

            if (boardInfoValue != null)
            {
                this.BoardInfo = new FruBoardInfo(boardInfoValue);
            }

            if (productInfoValue != null)
            {
                this.ProductInfo = new FruProductInfo(productInfoValue);
            }

            if (multiRecordInfoValue != null)
            {
                this.MultiRecordInfo = new FruMultiRecordInfo(multiRecordInfoValue);
            }

        }

    }

    #endregion FRU types structures
}
