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
    using System.Threading;
    using System.Collections;
    using System.IO.Ports;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// This file has everything related to Pmbus SPEC
    /// The idea is to decouple the device layer from PmBus specific details
    /// Any changes in PMbus spec (say a command opcode) will only affect this file
    
    /// <summary>
    /// Enumerates the PmBus commands supported
    /// </summary>
    internal enum PmBusCommand
    {
        Invalid = 0x0,
        STATUS_WORD = 0x79,
        STATUS_VOUT = 0x7A,
        STATUS_IOUT = 0x7B,
        STATUS_INPUT = 0x7C,
        STATUS_TEMPERATURE = 0x7D,
        STATUS_CML = 0x7E,
        READ_POUT = 0x96,
        MFR_SERIAL = 0x9E,
        CLEAR_FAULTS = 0x03,
        MFR_MODEL = 0x9A,
        SET_POWER = 0x01,
    }

    /// <summary>
    /// Enum values supports parameters for PmbusCommand
    /// </summary>
    internal enum PmBusCommandPayload
    {
        /// <summary>
        /// Power On payload for PmbusCommand.SET_POWER
        /// </summary>
        POWER_ON  = 0x80,

        /// <summary>
        /// Power Soft Off (with sequencing) payload for PmbusCommand.SET_POWER
        /// </summary>
        POWER_SOFT_OFF = 0x40,

        /// <summary>
        /// Power Off payload for PmbusCommand.SET_POWER
        /// </summary>
        POWER_OFF = 0x00
    }

    /// <summary>
    /// Enumerates the PmBus commands response length
    /// </summary>
    internal enum PmBusResponseLength
    {
        Invalid = 0,
        STATUS_WORD = 2,
        STATUS_VOUT = 1,
        STATUS_IOUT = 1,
        STATUS_INPUT = 1,
        STATUS_TEMPERATURE = 1,
        STATUS_CML = 1,
        READ_POUT = 2,
        MFR_SERIAL = 15,
        CLEAR_FAULTS = 0,
        MFR_MODEL = 16,
        SET_POWER = 0
    }


    internal static class PmBus
    {
        /// <summary>
        /// Enumerates the PmBus transaction types 
        /// </summary>
        private enum TransactionType
        {
            ReadByte = 0,
            ReadWord,
            ReadBlock,
            WriteByte,
            WriteWord,
            WriteBlock,
            RwByte,
            RwWord,
            RwBlock,
            ReadSerial,
        }

        /// <summary>
        /// This table contains key (commandCode) / value (numDataBytes) pairs
        /// </summary>
        static private Dictionary<PmBusCommand, TransactionType> commandTable = new Dictionary<PmBusCommand, TransactionType>()
        {
            {PmBusCommand.STATUS_WORD, TransactionType.ReadWord},
            {PmBusCommand.READ_POUT, TransactionType.ReadWord},
            {PmBusCommand.MFR_SERIAL, TransactionType.ReadSerial},
            {PmBusCommand.CLEAR_FAULTS, TransactionType.WriteByte},
            {PmBusCommand.SET_POWER, TransactionType.WriteByte}
        };

        /// <summary>
        /// Get the number of data bytes associated with the command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        static internal CompletionCode GetNumberOfDataBytesForCommand(PmBusCommand command, out byte numDataBytes)
        {
            numDataBytes = 0;
            TransactionType trType;
            if (commandTable.ContainsKey(command))
            {
                trType = commandTable[command];
                numDataBytes = GetNumberfOfDataBytesFromTransactionType(trType);
                return CompletionCode.Success;
            }
            return CompletionCode.InvalidCommand;
        }

        /// <summary>
        /// From the raw PSU status byte array, extract the relevant status information
        /// </summary>
        /// <param name="psuStatus">input raw status byte array</param>
        /// <param name="powerGoodByte">output status byte - 1 indicates good, 0 indicates something is bad with the PSU</param>
        /// <returns>True if PSU status is good, else return false.</returns>
        static internal bool ExtractPowerGoodFromPsuStatus(byte[] psuStatus, out byte powerGoodByte)
        {
            // Read the high byte. In the PMBus specification, the lowest order byte is sent first,
            // so the high byte is the second byte in the response
            powerGoodByte = psuStatus[1];
            // Get the 4th bit of the high byte which is the POWER_GOOD# signal. 
            // See the STATUS_WORD register in the PMBus II specification
            powerGoodByte = (byte)((powerGoodByte & 0x08) >> 3);

            // The POWER_GOOD# bit is negated, so invert the return bit.
            if (powerGoodByte == 1) 
                powerGoodByte = 0;
            else if (powerGoodByte == 0)
                powerGoodByte = 1;
           
            // Return false if there are any other faults
            if (psuStatus[0] != 0 || psuStatus[1] != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Extract Psu Model Number from the PmBus response
        /// </summary>
        /// <param name="dataPacketReceived"></param>
        /// <param name="interpretedDataPacket"></param>
        static internal void PsuModelNumberParser(ref byte[] dataPacketReceived, out byte[] interpretedDataPacket)
        {
            // Interpret the received data.
            // The first byte contains the packet length. Discard the first byte.
            interpretedDataPacket = new byte[dataPacketReceived.Length - 1];
            Buffer.BlockCopy(dataPacketReceived, 1, interpretedDataPacket, 0, dataPacketReceived.Length - 1);
        }

        /// <summary>
        /// Extract Psu Serial Number from the PmBus response
        /// </summary>
        /// <param name="dataPacketReceived"></param>
        /// <param name="interpretedDataPacket"></param>
        static internal void PsuSerialNumberParser(ref byte[] dataPacketReceived, out byte[] interpretedDataPacket)
        {
            // Interpret the received data.
            // The first byte contains the packet length. Discard the first byte.
            interpretedDataPacket = new byte[dataPacketReceived.Length - 1];
            Buffer.BlockCopy(dataPacketReceived, 1, interpretedDataPacket, 0, dataPacketReceived.Length - 1);
        }        

        /// <summary>
        /// Convert the Linear Data Format based on the linear conversion model in the PMBus spec Rev. 1.1 Section 7.1
        /// </summary>
        /// <param name="dataPacketReceived">The data packet received.</param>
        /// <param name="interpretedDataPacket">The converted data packet.</param>
        static internal void PmBusLinearDataFormatConverter(ref byte[] dataPacketReceived, out byte[] interpretedDataPacket)
        {
            // Step 1: Interpret the received data
            byte dataHighByte;
            byte dataLowByte;
            int NinTwosComplement;
            int YinTowsComplement;
            int N;
            int Y;
            int convertedValue;
            const int bitCountOfN = 5;
            const int bitCountOfY = 11;
            const byte numMSBsToEncodeN = bitCountOfN;

            dataHighByte = dataPacketReceived[1];
            dataLowByte = dataPacketReceived[0];

            NinTwosComplement = dataHighByte >> (8 - numMSBsToEncodeN);
            YinTowsComplement = (BitwiseOperationUtil.MaskOffMSBs(dataHighByte, numMSBsToEncodeN) << 8) | dataLowByte;

            N = NumberConversionUtil.ConvertFromTwosComplement(NinTwosComplement, bitCountOfN);
            Y = NumberConversionUtil.ConvertFromTwosComplement(YinTowsComplement, bitCountOfY);

            // Converted Value = Y * pow(2, N)
            convertedValue = (int)((double)Y * Math.Pow(2, N));
            interpretedDataPacket = BitConverter.GetBytes(convertedValue);
        }

        /// <summary>
        /// Get the number of data bytes associated with the transaction type
        /// </summary>
        /// <param name="trType"></param>
        /// <returns></returns>
        static private byte GetNumberfOfDataBytesFromTransactionType(TransactionType trType)
        {
            byte numDataBytes = 0;
            switch (trType)
            {
                case TransactionType.ReadWord:
                    numDataBytes = 2;
                    break;
                case TransactionType.ReadSerial:
                    numDataBytes = 15;
                    break;
                case TransactionType.WriteByte:
                    numDataBytes = 0;
                    break;
                default:
                    Tracer.WriteError("Unsupported transaction type: {0}", trType);
                    break;
            }
            return numDataBytes;
        }
    }
}
