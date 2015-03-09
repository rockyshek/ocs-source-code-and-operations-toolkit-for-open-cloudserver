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
    /// <summary>
    /// Represents the DCMI 'Set Power Limit' request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Dcgrp, IpmiCommand.DcmiSetLimit)]
    internal class SetDcmiPowerLimitRequest : IpmiRequest
    {
        /// <summary>
        /// Group Extension byte.  Always 0xDC
        /// </summary> 
        private byte groupextension = 0xDC;

        /// <summary>
        /// Reserved in spec for future use
        /// </summary> 
        private byte[] reserved1 = { 0x00, 0x00, 0x00 };

        /// <summary>
        /// Exception Actions: 0 = none, 1 = reboot, 2-10 = OEM
        /// </summary>
        private byte exceptionActions;

        /// <summary>
        /// 2 byte prower limit in Watts.
        /// </summary>
        private byte[] powerLimit;

        /// <summary>
        /// 4 byte correction time in ms.
        /// </summary>
        private byte[] correctionTime;

        /// <summary>
        /// Reserved in spec for future use
        /// </summary>
        private byte[] reserved2 = { 0x00, 0x00 };

        /// <summary>
        /// 2 bytes sampling period in seconds
        /// </summary>
        private byte[] samplingPeriod;

        /// <summary>
        /// Initializes a new instance of the SetDcmiPowerLimitRequest class.
        /// </summary>
        internal SetDcmiPowerLimitRequest(short watts, int correctionTime, byte action, short samplingPeriod)
        {
            this.powerLimit = BitConverter.GetBytes(watts);
            this.correctionTime = BitConverter.GetBytes(correctionTime);
            this.samplingPeriod = BitConverter.GetBytes(samplingPeriod);
            this.exceptionActions = action;
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
        /// Reserved in spec for future use
        /// </summary>       
        [IpmiMessageData(1,3)]
        public byte[] Reserved1
        {
            get { return this.reserved1; }

        }

        /// <summary>
        /// Exception Action 
        /// </summary>       
        [IpmiMessageData(4)]
        public byte ExceptionAction
        {
            get { return this.exceptionActions; }
        }

        /// <summary>
        /// Power limit in watts
        /// </summary>       
        [IpmiMessageData(5,2)]
        public byte[] PowerLimit
        {
            get { return this.powerLimit; }
        }

        /// <summary>
        /// Correction time is the maximum time taken to limit the power, 
        /// otherwise exception action will be taken as configured.
        /// </summary>
        /// <value>CorrectionTime in ms.</value>
        [IpmiMessageData(7, 4)]
        public byte[] CorrectionTime
        {
            get { return this.correctionTime; }
        }

        /// <summary>
        /// Reserved in spec for future use
        /// </summary>
        [IpmiMessageData(11, 2)]
        public byte[] Reserved2
        {
            get { return this.reserved2; }
        }

        /// <summary>
        /// Sampling period in seconds.
        /// </summary>
        [IpmiMessageData(13, 2)]
        public byte[] SamplingPeriod
        {
            get { return this.samplingPeriod; }
        }

    }
}
