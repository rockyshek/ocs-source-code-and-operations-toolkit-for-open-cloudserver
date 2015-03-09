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
    /// Represents the IPMI 'Get Psu Alert' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetPsuAlert)]
    internal class GetPsuAlertResponse : IpmiResponse
    {

        byte alertStatus;

        /// <summary>
        /// Alert Status
        /// </summary>       
        [IpmiMessageData(0)]
        public byte AlertStatus
        {
            get { return this.alertStatus; }
            set { this.alertStatus = value; }
        }

        /// <summary>
        /// PSU_Alert BMC GPI Status
        /// [7:6] BLADE_EN2 to BMC GPI
        /// </summary>
        public bool PsuAlertGpi
        {
            get { 
                    if((alertStatus & 0x40) == 0x40)
                        return true;
                    else 
                        return false;
                }
        }

        /// <summary>
        /// Auto PROCHOT on switch GPI
        /// [5:4] Auto FAST_PROCHOT Enabled
        /// </summary>
        public bool AutoProchotEnabled
        {
            get
            {
                if ((alertStatus & 0x10) == 0x10)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// BMC PROCHOT on switch GPI
        /// [3:0] BMC FAST_PROCHOT Enabled
        /// </summary>
        public bool BmcProchotEnabled
        {
            get
            {
                if ((alertStatus & 0x01) == 0x01)
                    return true;
                else
                    return false;
            }
        }



    }
}
