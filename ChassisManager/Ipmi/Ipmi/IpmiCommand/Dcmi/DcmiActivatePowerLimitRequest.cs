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
    /// Represents the DCMI 'Activate/Deactivate Power Limit' request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Dcgrp, IpmiCommand.DcmiActivateLimit)]
    internal class DcmiActivatePowerLimitRequest : IpmiRequest
    {
        /// <summary>
        /// Group Extension byte.  Always 0xDC
        /// </summary> 
        private byte groupExtension = 0xDC;

        /// <summary>
        /// Power Limit Activation (0x00 = Deactivate 0x01 = Activate) 
        /// </summary> 
        private byte powerLimitActivation;

        /// <summary>
        /// 2 bytes reserved for future use
        /// </summary>
        private byte[] reservation = { 0x00, 0x00 };

        /// <summary>
        /// Initializes a new instance of the DcmiActivatePowerLimitRequest class.
        /// </summary>
        internal DcmiActivatePowerLimitRequest(bool enable)
        {
            if (enable)
            {
                // Activate
                this.powerLimitActivation = 0x01;
            }
            else
            {
                // Deactivate
                this.powerLimitActivation = 0x00;
            }
        }

        /// <summary>
        /// Group Extension byte
        /// </summary>       
        [IpmiMessageData(0)]
        public byte GroupExtension
        {
            get { return this.groupExtension; }

        }

        /// <summary>
        /// Reserved
        /// </summary>       
        [IpmiMessageData(1)]
        public byte PowerLimitActivation
        {
            get { return this.powerLimitActivation; }

        }

        /// <summary>
        /// Exception Action 
        /// </summary>       
        [IpmiMessageData(2,2)]
        public byte[] Reservation
        {
            get { return this.reservation; }
        }

    }
}
