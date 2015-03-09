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
    /// Represents the DCMI 'Get Power Reading' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Dcgrp, IpmiCommand.DcmiGetLimit)]
    internal class GetDcmiPowerLimitResponse : IpmiResponse
    {
        /// <summary>
        /// Group Extension (0xDC).
        /// </summary>
        private byte groupExtension;

        /// <summary>
        /// 2 bytes reserved for future use
        /// </summary>
        private byte[] reserved1;

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
        /// 2 bytes reserved for future use
        /// </summary>
        private byte[] reserved2;

        /// <summary>
        /// 2 bytes sampling period in seconds
        /// </summary>
        private byte[] samplingPeriod;

        /// <summary>
        /// Group Extension
        /// </summary>
        /// <value>0xDC</value>
        [IpmiMessageData(0)]
        public byte GroupExtension
        {
            get { return this.groupExtension; }
            set { this.groupExtension = value; }
        }

        /// <summary>
        /// Reserved in the spec for future use.
        /// </summary>
        [IpmiMessageData(1,2)]
        public byte[] Reserved1
        {
            get { return this.reserved1; }
            set { this.reserved1 = value; }
        }

        /// <summary>
        /// Exception Actions: 0 = none, 1 = reboot, 2-10 = OEM
        /// </summary>
        /// <value>Exception Action.</value>
        [IpmiMessageData(3)]
        public byte ExceptionActions
        {
            get { return this.exceptionActions; }
            set { this.exceptionActions = value; }
        }

        /// <summary>
        /// Power Limit in watts.
        /// </summary>
        /// <value>Maximum Power.</value>
        [IpmiMessageData(4,2)]
        public byte[] PowerLimit
        {
            get { return this.powerLimit; }
            set { this.powerLimit = value; }
        }

        /// <summary>
        /// Correction time is the maximum time taken to limit the power, 
        /// otherwise exception action will be taken as configured.
        /// </summary>
        /// <value>CorrectionTime in ms.</value>
        [IpmiMessageData(6,4)]
        public byte[] CorrectionTime
        {
            get { return this.correctionTime; }
            set { this.correctionTime = value; }
        }

        /// <summary>
        /// Reserved in the spec for future use.
        /// </summary>
        [IpmiMessageData(10, 2)]
        public byte[] Reserved2
        {
            get { return this.reserved2; }
            set { this.reserved2 = value; }
        }

        /// <summary>
        /// Sampling period in seconds.
        /// </summary>
        [IpmiMessageData(12, 2)]
        public byte[] SamplingPeriod
        {
            get { return this.samplingPeriod; }
            set { this.samplingPeriod = value; }
        }
    }
}
