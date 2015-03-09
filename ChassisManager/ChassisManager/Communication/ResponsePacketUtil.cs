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

    internal static class ResponsePacketUtil
    {
        /// <summary>
        /// Generate a response packet with an empty payload
        /// </summary>
        /// <param name="completionCode"></param>
        /// <param name="responsePacket"></param>
        internal static void GenerateResponsePacket(CompletionCode completionCode, out byte[] responsePacket)
        {
            byte[] payload = null;
            GenerateResponsePacket(completionCode, 0, ref payload, out responsePacket);
        }

        internal static void GenerateResponsePacket(CompletionCode completionCode, ref byte[] payload, out byte[] responsePacket)
        {
            if (payload == null)
            {
                GenerateResponsePacket(completionCode, out responsePacket);
            }
            else
            {
                GenerateResponsePacket(completionCode, payload.Length, ref payload, out responsePacket);
            }
        }

        /// <summary>
        /// Generate a response packet with a non-empty payload
        /// </summary>
        /// <param name="completionCode"></param>
        /// <param name="payLoadLengthInByte"></param>
        /// <param name="payload"></param>
        /// <param name="responsePacket"></param>
        internal static void GenerateResponsePacket(CompletionCode completionCode, int payLoadLengthInByte, ref byte[] payload, out byte[] responsePacket)
        {
            const int byteCountSegmentLengthInByte = 2;
            if (payLoadLengthInByte == 0)
            {
                responsePacket = new byte[3];
                responsePacket[0] = (byte)completionCode;
                responsePacket[1] = 0;
                responsePacket[2] = 0;
            }
            else
            {
                byte[] byteCountSegment = BitConverter.GetBytes((short)payLoadLengthInByte);
                responsePacket = new byte[payLoadLengthInByte + 3];
                responsePacket[0] = (byte)completionCode;
                Buffer.BlockCopy(byteCountSegment, 0, responsePacket, 1, byteCountSegmentLengthInByte);
                Buffer.BlockCopy(payload, 0, responsePacket, 3, payLoadLengthInByte);
            }
        }
    }
}
