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
    /// Represents the IPMI 'Activate/Deastivate Psu Alert' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.ActivatePsuAlert)]
    internal class ActivePsuAlertRequest : IpmiRequest
    {
        /// <summary>
        /// Drives GPIOF7
        /// [1:0]
        ///     0 = GPIOF7, disables BLADE_EN2 signal
        ///     1 = GPIOF7, enabling BLADE_EN2 signal
        /// [5:2]
        ///     0 = No Action upon BLADE_EN2 Signal
        ///     1 = Asserts GPIO31 on the PCH and FAST_PROCHOT 
        ///         when BLADE_EN2 GPIOH6 on the BMC is asserted.
        ///         Then Implements default power cap.
        ///     2 = No PROCHOT, just set default power cap.
        /// </summary>
        private readonly byte autoProcHot;

        /// <summary>
        /// 00 = Leave current Default Power limit if set.
        /// 01 = Remove current Default Power limit. 
        ///      Forces re-arm, unmasking the BLADE_EN2 GPIOH6 on the BMC.
        /// </summary>
        private readonly byte removeDpc;


        /// <summary>
        /// Initialize instance of the class.
        /// </summary>
        /// <param name="enableAutoProcHot">Enables Automatic ProcHot</param>
        /// <param name="bmcAction">Bmc Action on PSU_Alert GPI</param>
        /// <param name="removeCap">Removes Default Power Cap</param>
        internal ActivePsuAlertRequest(bool enableAutoProcHot, BmcPsuAlertAction bmcAction, bool removeCap)
        {
            if (enableAutoProcHot)
                autoProcHot = 0x01;
            
            // Bmc Action is bit shifed in the enum
            autoProcHot = (byte)(autoProcHot | (byte)bmcAction);

            if (removeCap)
                removeDpc = 0x01;
        }

        /// <summary>
        /// Byte representations of bitmask for Automatic PSU_Alert
        /// Fast PROCHOT function.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte AutoProcHot
        {
            get { return this.autoProcHot; }

        }

        /// <summary>
        ///  Modify Default Power Cap
        /// </summary>       
        [IpmiMessageData(1)]
        public byte RemoveDpc
        {
            get { return this.removeDpc; }

        }

    }
}
