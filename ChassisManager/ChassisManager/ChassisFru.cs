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
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    /// <summary>
    /// Class for CM Fru Read and Write
    /// </summary>
    public class ChassisFru : ChassisSendReceive
    {
        /// <summary>
        /// Default Device Id, 1 for FRU EEPROM
        /// as there is no device Id dependency
        /// </summary>
        readonly byte DeviceId = 0x01;
       
        internal FruDevice ReadFru(DeviceType deviceType, bool maxLenght = false)
        {
            byte countToRead = 8; //FRU common header size

            ChassisFruReadResponse fruResponse =
                SendReceive<ChassisFruReadResponse>(deviceType, this.DeviceId, (new ChassisFruReadRequest(0, 0, countToRead)));

            if (fruResponse.CompletionCode == 0x00)
            {

                FruCommonHeader commonHeader = new FruCommonHeader(fruResponse.DataReturned);

                ushort areaOffset;

                byte[] chassisInfo = null;
                byte[] boardInfo = null;
                byte[] productInfo = null;
                byte[] multiRecordInfo = null;
                byte completionCode = fruResponse.CompletionCode;

                areaOffset = commonHeader.ChassisInfoStartingOffset;
                if (areaOffset != 0)
                {
                    chassisInfo = ReadFruAreaBytes(deviceType, this.DeviceId, areaOffset, maxLenght, out completionCode);
                }

                areaOffset = commonHeader.BoardAreaStartingOffset;
                if (areaOffset != 0)
                {
                    boardInfo = ReadFruAreaBytes(deviceType, this.DeviceId, areaOffset, maxLenght, out completionCode);
                }

                areaOffset = commonHeader.ProductAreaStartingOffset;
                if (areaOffset != 0)
                {
                    productInfo = ReadFruAreaBytes(deviceType, this.DeviceId, areaOffset, maxLenght, out completionCode);
                }

                areaOffset = commonHeader.MultiRecordAreaStartingOffset;
                if (areaOffset != 0)
                {
                    multiRecordInfo = ReadFruAreaBytes(deviceType, this.DeviceId, areaOffset, maxLenght, out completionCode);
                }


                return new FruDevice(this.DeviceId,
                                        commonHeader,
                                        chassisInfo,
                                        boardInfo,
                                        productInfo,
                                        multiRecordInfo,
                                        completionCode);
            }
            else
            {
                return new FruDevice(fruResponse.CompletionCode);
            }
        }

        /// <summary>
        /// Returns a byte array with all bytes from a specific area of the fru: Chassis, Baseboard, Product
        /// </summary>
        private byte[] ReadFruAreaBytes(DeviceType deviceType, byte deviceId, ushort offset, bool maxLenght, out byte completionCode)
        {
            byte countToRead = 0x10;
            byte loOffset;
            byte hiOffset;

            List<byte> areaBytes = new List<byte>();

            IpmiSharedFunc.SplitWord(offset, out loOffset, out hiOffset);

            ushort totalDataRead = countToRead;
            ChassisFruReadRequest fruRequest =
                new ChassisFruReadRequest(loOffset, hiOffset, countToRead);

            ChassisFruReadResponse fruResponse = SendReceive<ChassisFruReadResponse>(deviceType, deviceId, (fruRequest));

            completionCode = fruResponse.CompletionCode;

            if (completionCode == (byte)CompletionCode.Success)
            {
                ushort dataSize = FruArea.AreaLength(fruResponse.DataReturned);
                totalDataRead = Math.Min(countToRead, dataSize);
                IpmiSharedFunc.AppendArrayToList(fruResponse.DataReturned, areaBytes, totalDataRead);
                offset += totalDataRead;
                int pass = 0;
                const int readLimit = 12;

                while (dataSize > totalDataRead && pass <= readLimit)
                {
                    IpmiSharedFunc.SplitWord(offset, out loOffset, out hiOffset);

                    if (!maxLenght)
                        countToRead = (byte)Math.Min(countToRead, dataSize - totalDataRead);
                    else
                        countToRead = (byte)Math.Min(byte.MaxValue, dataSize - totalDataRead);

                    fruRequest = new ChassisFruReadRequest(loOffset, hiOffset, countToRead);
                    // send request for more data
                    fruResponse = SendReceive<ChassisFruReadResponse>(deviceType, deviceId, (fruRequest));
                    totalDataRead += countToRead;
                    offset += countToRead;

                    completionCode = fruResponse.CompletionCode;

                    if (completionCode == 0x00)
                    {
                        IpmiSharedFunc.AppendArrayToList(fruResponse.DataReturned, areaBytes, countToRead);
                    }
                    else
                    {
                        break;
                    }

                    pass++;
                }

                if (pass > 12)
                {
                    completionCode = (byte)CompletionCode.InvalidIterationCount;
                }
            }

            return areaBytes.ToArray();
        }

        /// <summary>
        /// Write to Chassis Fru - (Important) note that this function enables write to any offset 
        /// Offset checks ensures that we are within permissible limits, but cannot enforce semantics within those limits
        /// Length checks validity of packet, however empty fields in packet are responsibility of writing function
        /// User level priority since this is not an internal call
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public CompletionCode WriteChassisFru(ushort offset, ushort length, 
            byte[] packet, DeviceType deviceType)
        {

            ChassisFruWriteResponse response = new ChassisFruWriteResponse();
            response.CompletionCode = (byte)CompletionCode.UnspecifiedError;

            try
            {
                response = (ChassisFruWriteResponse)this.SendReceive(deviceType, 
                    this.DeviceId, new ChassisFruWriteRequest(offset, length, packet),
                 typeof(ChassisFruWriteResponse), (byte)PriorityLevel.User);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("ChassisFru.WriteChassisFru() Write had an exception with paramaters: Offset: {0} Length: {1} Packet: {2} DeviceType: {3} Exception: {4}",
                    offset, length, (packet == null ? "Null packet" : Ipmi.IpmiSharedFunc.ByteArrayToHexString(packet)), deviceType.ToString(), ex));
            }

            if (response.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("ChassisFru.WriteChassisFru() write failed with completion code {0:X}", response.CompletionCode);
            }
         
            return (CompletionCode)response.CompletionCode;
        }

        /// <summary>
        /// Request Format for Chassis Fru read
        /// </summary>
        [ChassisMessageRequest(FunctionCode.ReadEeprom)]
        internal class ChassisFruReadRequest : ChassisRequest
        {
            [ChassisMessageData(0)]
            public byte lowOffset
            {
                get;
                set;
            }

            [ChassisMessageData(1)]
            public byte highOffset
            {
                get;
                set;
            }

            [ChassisMessageData(2)]
            public byte lowLength
            {
                get;
                set;
            }

            [ChassisMessageData(3)]
            public byte highLength
            {
                get;
                set;
            }

            public ChassisFruReadRequest(byte offSetLS, byte offSetMS, byte readCount)
            {
                this.lowOffset = offSetLS;
                this.highOffset = offSetMS;
                this.lowLength = readCount;
            }

        }

        
        /// <summary>
        /// Response for CM Fru Read
        /// </summary>
        [ChassisMessageResponse(FunctionCode.ReadEeprom)]
        public class ChassisFruReadResponse : ChassisResponse
        {
            // TODO - fix this
            private byte[] dataReturned = new byte[256];

            [ChassisMessageData(0)]
            public byte[] DataReturned
            {
                get { return this.dataReturned; }
                set { this.dataReturned = value; }
            }
        }

        [ChassisMessageRequest(FunctionCode.WriteEeprom)]
        internal class ChassisFruWriteRequest : ChassisRequest
        {

            [ChassisMessageData(0)]
            public byte lowOffset
            {
                get;
                set;
            }

            [ChassisMessageData(1)]
            public byte highOffset
            {
                get;
                set;
            }

            [ChassisMessageData(2)]
            public byte lowLength
            {
                get;
                set;
            }

            [ChassisMessageData(3)]
            public byte highLength
            {
                get;
                set;
            }

            [ChassisMessageData(4)]
            public byte[] dataToWrite
            {
                get;
                set;
            }


            public ChassisFruWriteRequest(ushort offset, ushort length, byte[] dataToWrite)
            {
                byte lowOffset, highOffset, lowLength, highLength;

                IpmiSharedFunc.SplitWord(offset, out lowOffset, out highOffset);

                IpmiSharedFunc.SplitWord(length, out lowLength, out highLength);

                this.lowOffset = lowOffset;
                this.highOffset = highOffset;
                this.lowLength = lowLength;
                this.highLength = highLength;
                this.dataToWrite = dataToWrite;
            }
        }

        [ChassisMessageResponse(FunctionCode.WriteEeprom)]
        internal class ChassisFruWriteResponse : ChassisResponse
        {
        }
    }
}
