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

    static class WcsBladeMezz
    {
        static byte channel = 0x01; // 0-based
        static byte slaveId = 0xEE; // slave address of Pikes Peak I2C Controller

        /// <summary>
        /// Send I2C/Smbus Payloads to BMC via MasterWriteRead for reading Pass-Through Mode
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="passThroughMode"></param>
        /// <param name="apiName"></param>
        /// <returns>CompletionCode</returns>
        internal static CompletionCode GetMezzPassThroughMode(int bladeId, out bool passThroughMode, string apiName)
        {
            // initialize return fields
            CompletionCode getMezzPassThroughCompletionCode = CompletionCode.UnknownState;
            passThroughMode = false;

            // Initialize I2C/Smbus payloads
            byte count = 0x01; // bytes to read
            byte[] writeData = { 0x02 }; // { starting address }

            try
            {
                // Send I2C/Smbus Payload to BMC for reading pass-through byte
                Ipmi.SmbusWriteRead sendSmbusPayloadResponse = SendSmbusPayload((byte)bladeId, channel, slaveId, count, writeData, apiName);
                if (sendSmbusPayloadResponse.CompletionCode == (byte)CompletionCode.Success &&
                    sendSmbusPayloadResponse.RawData.Length == (int)count)
                {
                    // Get Pass-Through Mode from response byte (bit 1 of response byte)
                    if ((sendSmbusPayloadResponse.RawData[0] & 0x02) == 0x02)
                        passThroughMode = false;
                    else
                        passThroughMode = true;

                    // Set Completion Code Success since no errors found
                    getMezzPassThroughCompletionCode = CompletionCode.Success;
                }
                else
                {
                    Tracer.WriteError(string.Format(
                        "{0}(): MasterWriteRead returned unexpected payload data or did not return Success Completion Code for bladeId {1} (Data: {2})",
                            apiName, bladeId, sendSmbusPayloadResponse.MessageData));
                    getMezzPassThroughCompletionCode = CompletionCode.I2cErrors;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("Exception occured in {0}() for blade {1}: {2}", apiName, bladeId, ex));
                getMezzPassThroughCompletionCode = CompletionCode.UnspecifiedError;
            }

            return getMezzPassThroughCompletionCode;
        }

        /// <summary>
        /// Send I2C/Smbus Payloads to BMC via MasterWriteRead for setting Pass-Through Mode
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="passThroughMode"></param>
        /// <param name="apiName"></param>
        /// <returns>CompletionCode</returns>
        internal static CompletionCode SetMezzPassThroughMode(int bladeId, bool passThroughMode, string apiName)
        {
            // Initialize return fields
            CompletionCode setMezzPassThroughCompletionCode = CompletionCode.UnknownState;

            // Initialize I2C/Smbus payloads 
            byte count = 0x01; // bytes to read (0x00 means write)
            byte[] writeData = { 0x02 }; // { starting address }

            try
            {
                // Send I2C/Smbus Payload to BMC for reading pass-through byte before re-writing
                Ipmi.SmbusWriteRead sendSmbusPayloadResponse = SendSmbusPayload((byte)bladeId, channel, slaveId, count, writeData, apiName);
                if (sendSmbusPayloadResponse.CompletionCode == (byte)CompletionCode.Success
                    && sendSmbusPayloadResponse.RawData.Length == (int)count)
                {
                    // Configure writeData based on passThroughMode
                    if (passThroughMode)
                    {
                        // Configure writeData to activate Pass-Through Mode (smbusPayloadByte bit 0 = 1)
                        writeData = new byte[2] { writeData[0], (byte)(sendSmbusPayloadResponse.RawData[0] | 0x01) }; // { starting address, write byte }
                    }
                    else
                    {
                        // Configure writeData to turn off Pass-Through Mode (smbusPayloadByte bit 0 = 0)
                        writeData = new byte[2] { writeData[0], (byte)(sendSmbusPayloadResponse.RawData[0] & 0xFE) }; // { starting address, write byte }
                    }

                    // Send I2C/Smbus Payload to BMC for setting pass-through mode on FPGA Mezz
                    sendSmbusPayloadResponse = SendSmbusPayload((byte)bladeId, channel, slaveId, 0x00, writeData, apiName);
                    if (sendSmbusPayloadResponse.CompletionCode != (byte)CompletionCode.Success)
                        setMezzPassThroughCompletionCode = CompletionCode.I2cErrors;
                    else
                        setMezzPassThroughCompletionCode = CompletionCode.Success;
                }
                else
                {
                    Tracer.WriteError(string.Format(
                        "{0}(): MasterWriteRead returned unexpected payload data or did not return Success Completion Code for bladeId {1} (Data: {2})",
                            apiName, bladeId, sendSmbusPayloadResponse.MessageData));
                    setMezzPassThroughCompletionCode = CompletionCode.I2cErrors;
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(string.Format("Exception occured in {0}() for blade {1}: {2}", apiName, bladeId, ex));
                setMezzPassThroughCompletionCode = CompletionCode.UnspecifiedError;
            }

            return setMezzPassThroughCompletionCode;
        }

        /// <summary>
        /// Read Tray Mezz Fru EEPROM
        /// </summary>
        internal static Ipmi.FruDevice GetMezzFruEeprom(int bladeId)
        {
            byte fruId = 0xA2;
            byte bytesToRead = 0x08;
            string cmd = "GetMezzFruEeprom";

            Ipmi.SmbusWriteRead response = SendSmbusPayload(bladeId, channel, fruId, bytesToRead, new byte[2] { 0x00, 0x00 }, cmd);

            if (response.CompletionCode == 0x00)
            {

                Ipmi.FruCommonHeader commonHeader = new Ipmi.FruCommonHeader(response.RawData);

                byte[] chassisInfo = null;
                byte[] boardInfo = null;
                byte[] productInfo = null;
                byte[] multiRecordInfo = null;
                byte completionCode = response.CompletionCode;

                ushort areaOffset = commonHeader.ChassisInfoStartingOffset;
                if (areaOffset != 0)
                {
                    chassisInfo = ReadFruAreaBytes(bladeId, channel, fruId, areaOffset, false, out completionCode);
                }

                areaOffset = commonHeader.BoardAreaStartingOffset;
                if (areaOffset != 0)
                {
                    boardInfo = ReadFruAreaBytes(bladeId, channel, fruId, areaOffset, false, out completionCode);
                }

                areaOffset = commonHeader.ProductAreaStartingOffset;
                if (areaOffset != 0)
                {
                    productInfo = ReadFruAreaBytes(bladeId, channel, fruId, areaOffset, false, out completionCode);
                }

                areaOffset = commonHeader.MultiRecordAreaStartingOffset;
                if (areaOffset != 0)
                {
                    multiRecordInfo = ReadFruAreaBytes(bladeId, channel, fruId, areaOffset, false, out completionCode);
                }

                return new Ipmi.FruDevice(bladeId,
                    commonHeader,
                    chassisInfo,
                    boardInfo,
                    productInfo,
                    multiRecordInfo,
                    completionCode);

            }
            else
            {
                return new Ipmi.FruDevice(response.CompletionCode);
            }
        }

        /// <summary>
        /// Returns a byte array with all bytes from a specific area of the fru: Chassis, Baseboard, Product
        /// </summary>
        private static byte[] ReadFruAreaBytes(int bladeId, byte channel, byte fruId, ushort offset, bool maxLenght, out byte completionCode)
        {
            byte countToRead = 0x10;
            byte loOffset;
            byte hiOffset;
            string cmd = "MezzReadFruAreaBytes";

            List<byte> areaBytes = new List<byte>();

            Ipmi.IpmiSharedFunc.SplitWord(offset, out loOffset, out hiOffset);

            ushort totalDataRead = countToRead;
            Ipmi.SmbusWriteRead response = SendSmbusPayload(bladeId, channel, fruId, countToRead, new byte[2] { hiOffset, loOffset }, cmd);

            completionCode = response.CompletionCode;

            if (completionCode == 0x00)
            {
                ushort dataSize = Ipmi.FruArea.AreaLength(response.RawData);
                totalDataRead = Math.Min(countToRead, dataSize);
                Ipmi.IpmiSharedFunc.AppendArrayToList(response.RawData, areaBytes, totalDataRead);
                offset += totalDataRead;
                int pass = 0;
                const int readLimit = 12;

                while (dataSize > totalDataRead && pass <= readLimit)
                {
                    Ipmi.IpmiSharedFunc.SplitWord(offset, out loOffset, out hiOffset);

                    if (!maxLenght)
                        countToRead = (byte)Math.Min(countToRead, dataSize - totalDataRead);
                    else
                        countToRead = (byte)Math.Min(byte.MaxValue, dataSize - totalDataRead);

                    response = SendSmbusPayload(bladeId, channel, fruId, countToRead, new byte[2] { hiOffset, loOffset }, cmd);

                    totalDataRead += countToRead;
                    offset += countToRead;

                    completionCode = response.CompletionCode;

                    if (completionCode == 0x00)
                    {
                        Ipmi.IpmiSharedFunc.AppendArrayToList(response.RawData, areaBytes, countToRead);
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
        /// Send Smbus payload to Fpga Mezz via MasterWriteRead
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="channel"></param>
        /// <param name="slaveId"></param>
        /// <param name="count"></param>
        /// <param name="writeData"></param>
        /// <param name="cmd"></param>
        /// <returns>SmbusWriteRead</returns>
        private static Ipmi.SmbusWriteRead SendSmbusPayload(int bladeId, byte channel, byte slaveId, byte count, byte[] writeData, string cmd)
        {   
            Ipmi.SmbusWriteRead sendSmbusPayloadResponse = WcsBladeFacade.MasterWriteRead((byte)bladeId, channel, slaveId, count, writeData);
            if (sendSmbusPayloadResponse.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError(string.Format("{0}(): MasterWriteRead failed for bladeId {1} with completion code {2}",
                    cmd, bladeId, sendSmbusPayloadResponse.CompletionCode));
            }

            return sendSmbusPayloadResponse;
        }
                
    }
}
