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
    /// Represents the IPMI 'Set Psu Alert' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.SetPsuAlert)]
    internal class SetPsuAlertRequest : IpmiRequest
    {
        /// <summary>
        /// Drives GPIOF6 for simulation
        /// 00h = Deassert PSU_ALERT GPI (BLADE_EN2 to BMC)
        /// 01h = Assert PSU_ALERT GPI (BLADE_EN2 to BMC)
        /// </summary>
        private readonly byte assert;

        /// <summary>
        /// Initialize instance of the class.  This command is for testing BMC 
        /// functionality when the PSU_ALERT GPI is asserted
        /// </summary>
        /// <param name="Assert PSU_ALERT">Enables/Disables PSU_Alert</param>
        internal SetPsuAlertRequest(bool enablePsuAlert)
        {
            if (enablePsuAlert)
                assert = 0x01;
        }

        /// <summary>
        /// Assert/Deassert PSU_ALERT GPI (BLADE_EN2 to BMC)
        /// </summary>       
        [IpmiMessageData(0)]
        public byte PsuAlertEnabled
        {
            get { return this.assert; }

        }
    }
}
