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
    /// <summary>
    /// Represents the DCMI 'Get Power Reading' request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Dcgrp, IpmiCommand.DcmiPowerReading)]
    internal class GetDcmiPowerReadingRequest : IpmiRequest
    {
        /// <summary>
        /// Group Extension byte.  Always 0xDC
        /// </summary> 
        private byte groupextension = 0xDC;

        /// <summary>
        /// Mode byte.
        /// </summary> 
        private byte readingMode;

        /// <summary>
        /// Rolling Average byte.
        /// </summary> 
        private byte rollingAverage;

        /// <summary>
        /// byte 3 is currently reserved.
        /// </summary> 
        private byte reserved = 0x00;

        /// <summary>
        /// Initializes a new instance of the GetDcmiPowerReadingRequest class.
        /// </summary>
        internal GetDcmiPowerReadingRequest(byte readingMode, byte rollingAverage)
        {
            this.rollingAverage = rollingAverage;
            this.readingMode = readingMode;
        }

        /// <summary>
        /// Group Extension byte
        /// </summary>       
        [IpmiMessageData(0)]
        public byte GroupExtension
        {
            get { return this.groupextension; }

        }

        /// <summary>
        /// Mode byte
        /// </summary>       
        [IpmiMessageData(1)]
        public byte ReadingMode
        {
            get { return this.readingMode; }

        }

        /// <summary>
        /// Sets Rolling Average
        /// </summary>       
        [IpmiMessageData(2)]
        public byte RollingAverage
        {
            get { return this.rollingAverage; }

        }

        /// <summary>
        /// Reserved
        /// </summary>       
        [IpmiMessageData(3)]
        public byte Reserved
        {
            get { return this.reserved; }

        }
    }
}
