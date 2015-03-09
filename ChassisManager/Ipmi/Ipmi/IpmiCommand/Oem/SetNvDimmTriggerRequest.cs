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
    /// Represents the IPMI 'Set NVDIMM Trigger' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.SetNvDimmTrigger)]
    internal class SetNvDimmTriggerRequest : IpmiRequest
    {
        /// <summary>
        /// [1:0] ADR_TRIGGER (GPIO37) on PCH.
        ///     00h = Disabled
        ///     01h = ADR_TRIGGER - GPIO37.  Switch connected.
        ///     02h = SMI Trigger - GPIO06.  Manual BMC trigger Enabled.
        /// </summary>
        private readonly byte adrTrigger;

        /// <summary>
        /// [1:0] Set Trigger
        ///     00 = Deassert Trigger
        ///     01 = Assert Trigger
        /// </summary>
        private readonly byte trigger;

        /// <summary>
        /// Delay between ADR_COMPLETE and blade power off (seconds)
        /// Range: 00h – FFh
        /// </summary>
        private byte adrCompleteDelay;

        /// <summary>
        /// [7:0] NVDIMM Present Power-off Delay
        /// Range: 00h - FFh
        /// </summary>
        private byte nvdimmPresentPowerOffDelay;

        /// <summary>
        /// Initialize instance of the class.
        /// </summary>
        internal SetNvDimmTriggerRequest(NvDimmTriggerAction triggerType, bool assertTrigger, 
            byte adrDelay, byte nvdimmDelay)
        {
            // unknown is an enum response type only. if user
            // sets the trigger to unknown it will be disabled.
            if (triggerType == NvDimmTriggerAction.Unknown)
                triggerType = NvDimmTriggerAction.Disabled;

            this.adrTrigger = (byte)triggerType;

            if (assertTrigger)
                this.trigger = 0x01;

            this.adrCompleteDelay = adrDelay;
            this.nvdimmPresentPowerOffDelay = nvdimmDelay;
        }

        /// <summary>
        /// NVDIMM Trigger Mechanism.
        /// 0 =  Do nothing upon NVDIMM Trigger. HW switch on 
        ///      the mainboard will be disabled.  Signal loss
        ///      from the HSC comparator will not result in 
        ///      ADR trigger on the PCH
        /// 1 =  Automatically assert PCH GPI37 upon signal loss.
        ///      HW switch on the mainboard will be enabled.  
        ///      Signal loss from the HSC comparator will result 
        ///      in ADR trigger.
        /// 2 =  Bmc will trigger SMI routine in the BIOS.  The
        ///      command must be executed manually with this bit
        ///      set for the manual SMI.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte AdrTriggerMechanism
        {
            get { return this.adrTrigger; }

        }

        /// <summary>
        /// Manual Trigger
        ///     0 = Deassert Trigger
        ///     1 = Assert Trigger
        /// </summary>
        [IpmiMessageData(1)]
        public byte ManualTrigger
        {
            get { return this.trigger; }

        }

        /// <summary>
        /// Delay between ADR_COMPLETE and blade power off (seconds)
        /// Range: 00h – FFh
        /// </summary>
        [IpmiMessageData(2)]
        public byte AdrCompletePowerOffDelay
        {
            get { return this.adrCompleteDelay; }
            set { this.adrCompleteDelay = value; }
        }

        /// <summary>
        /// [7:0] NVDIMM Present Power-off Delay
        /// Range: 00h - FFh
        /// </summary>
        [IpmiMessageData(3)]
        public byte NvdimmPresentPowerOffDelay
        {
            get { return this.nvdimmPresentPowerOffDelay; }
            set { this.nvdimmPresentPowerOffDelay = value; }
        }
    }
}
