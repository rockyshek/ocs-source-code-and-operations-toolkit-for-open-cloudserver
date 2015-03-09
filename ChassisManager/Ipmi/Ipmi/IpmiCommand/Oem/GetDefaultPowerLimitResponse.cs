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
    /// Represents the IPMI 'Get Default Power Limit' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetDefaultPowerLimit)]
    internal class GetDefaultPowerLimitResponse : IpmiResponse
    {
        /// <summary>
        /// Default Power Cap in Watts
        /// </summary>
        private ushort dpc;

        /// <summary>
        /// Delay interval in milliseconds between activating
        /// the DPC and deasserting the PROCHOT
        /// </summary>
        private ushort delay;

        /// <summary>
        /// Default Power Cap enabled/disabled
        /// </summary>
        private byte dpcEnabled;

        /// <summary>
        /// Default Power Cap in Watts
        /// </summary>       
        [IpmiMessageData(0)]
        public ushort DefaultPowerCap
        {
            get { return this.dpc; }
            set { this.dpc = value; }
        }

        /// <summary>
        ///  Time in milliseconds after applying DPC to 
        ///  wait before deasserting the PROCHOT
        /// </summary>    
        [IpmiMessageData(2)]
        public ushort WaitTime
        {
            get { return this.delay; }
            set { this.delay = value; }
        }

        /// <summary>
        ///  Disable/Enable Default Power Cap on PSU_Alert GPI.
        /// </summary>   
        [IpmiMessageData(4)]
        public byte DefaultCapEnabled
        {
            get { return this.dpcEnabled; }
            set { this.dpcEnabled = value; }
        }
    }
}
