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
    /// Represents the IPMI 'Set Default Power Limit' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.SetDefaultPowerLimit)]
    internal class SetDefaultPowerLimitRequest : IpmiRequest
    {
        
        /// <summary>
        /// Default power limit in Watts
        /// </summary>
        private readonly ushort dpc;

        /// <summary>
        /// Delay time after applying DPC before deasserting
        /// Fast PROCHOT.  Default 100ms
        /// </summary>
        private readonly ushort delay;

        /// <summary>
        /// Enable/Disable DPC when PSU_Alert GPI is asserted
        /// </summary>
        private readonly byte enableDpc;


        /// <summary>
        /// Initialize instance of the class.
        /// </summary>
        /// <param name="dpc">Default Power Cap</param>
        /// <param name="waitTime">Delay after DPC before removing PROCHOT</param>
        /// <param name="enableCapping">Removes Default Power Cap</param>
        internal SetDefaultPowerLimitRequest(ushort defaultPowerCap, ushort waitTime, bool enableCapping)
        {
            this.dpc = defaultPowerCap;

            this.delay = waitTime;

            if (enableCapping)
                enableDpc = 0x01;
        }

        /// <summary>
        /// Default Power Cap
        /// </summary>       
        [IpmiMessageData(0)]
        public ushort DefaultPowerCap
        {
            get { return this.dpc; }

        }

        /// <summary>
        ///  Time in milliseconds after applying DPC to 
        ///  wait before deasserting the PROCHOT
        /// </summary>       
        [IpmiMessageData(2)]
        public ushort WaitTime
        {
            get { return this.delay; }

        }

        /// <summary>
        ///  Disable/Enable Default Power Cap on PSU_Alert GPI.
        /// </summary>       
        [IpmiMessageData(4)]
        public byte DefaultCapEnabled
        {
            get { return this.enableDpc; }

        }

    }
}
