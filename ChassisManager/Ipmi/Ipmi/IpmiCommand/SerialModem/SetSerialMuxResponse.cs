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
    /// Represents the IPMI 'Set Serial/Modem Mux Command' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Application, IpmiCommand.SetSerialModelMux)]
    internal class SetSerialMuxResponse : IpmiResponse
    {

        internal void GetMux()
        {
            //[7] -  	0b = requests to switch mux to system are allowed 
            //          1b = requests to switch mux to system are blocked 
            if ((byte)(muxSetting & 0x80) == 0x00)
                muxSwitchAllowed = true;
            //[6] -  	0b = requests to switch mux to BMC are allowed 
            //          1b = requests to switch mux to BMC are blocked 
            if ((byte)(muxSetting & 0x40) == 0x00)
                requestToBmcAllowed = true;
            //[3] -  	0b = no alert presently in progress 
            //          1b = alert in progress on channel 
            if ((byte)(muxSetting & 0x08) == 0x08)
                alertInProgress = true;
            //[2] -  	0b = no IPMI or OEM messaging presently active on channel 
            //          1b = IPMI or OEM messaging session active on channel 
            if ((byte)(muxSetting & 0x04) == 0x04)
                messagingActive = true;
            //[1] -  	0b = request was rejected 
            //          1b = request was accepted (see note, below) or switch was forced 
            //          present mux setting 
            if ((byte)(muxSetting & 0x02) == 0x02)
                requestAccepted = true;
            //[0] -  	0b = mux is set to system (system can transmit and receive) 
            //          1b = mux is set to BMC  (BMC can transmit. System can neither 
            //          transmit nor receive) 
            if ((byte)(muxSetting & 0x01) == 0x00)
                muxSetToSystem = true;
        }

        /// <summary>
        /// [7] -  	0b = requests to switch mux to system are allowed 
        ///         1b = requests to switch mux to system are blocked 
        /// </summary>
        private bool muxSwitchAllowed = false;

        /// <summary>
        /// [6] -  	0b = requests to switch mux to BMC are allowed 
        ///         1b = requests to switch mux to BMC are blocked 
        /// </summary>
        private bool requestToBmcAllowed = false;

        /// <summary>
        /// [3] -  	0b = no alert presently in progress 
        ///         1b = alert in progress on channel 
        /// </summary>
        private bool alertInProgress = false;

        /// <summary>
        /// [2] -  	0b = no IPMI or OEM messaging presently active on channel 
        ///         1b = IPMI or OEM messaging session active on channel 
        /// </summary>
        private bool messagingActive = false;

        /// <summary>
        /// [1] -  	0b = request was rejected 
        ///         1b = request was accepted (see note, below) or switch was forced 
        /// </summary>
        private bool requestAccepted = false;

        /// <summary>
        /// [0] -  	0b = mux is set to system (system can transmit and receive) 
        ///         1b = mux is set to BMC  (BMC can transmit. System can neither 
        ///         transmit nor receive) 
        /// </summary>
        private bool muxSetToSystem = false;

        /// <summary>
        /// Mux Setting
        /// </summary>
        private byte muxSetting;

        /// <summary>
        /// Mux Setting
        /// </summary>
        [IpmiMessageData(0)]
        public byte MuxSetting
        {
            get { return (byte)this.muxSetting; }
            set { this.muxSetting = value; }
        }

        /// <summary>
        /// [7] -  	0b = requests to switch mux to system are allowed 
        ///         1b = requests to switch mux to system are blocked 
        /// </summary>
        internal bool MuxSwitchAllowed
        {
            get { return this.muxSwitchAllowed; }   
        }

        /// <summary>
        /// [6] -  	0b = requests to switch mux to BMC are allowed 
        ///         1b = requests to switch mux to BMC are blocked 
        /// </summary>
        internal bool RequestToBmcAllowed
        {
            get { return this.requestToBmcAllowed; }   
        }

        /// <summary>
        /// [3] -  	0b = no alert presently in progress 
        ///         1b = alert in progress on channel 
        /// </summary>
        internal bool AlertInProgress
        {
            get { return this.alertInProgress; }   
        }

        /// <summary>
        /// [2] -  	0b = no IPMI or OEM messaging presently active on channel 
        ///         1b = IPMI or OEM messaging session active on channel 
        /// </summary>
        internal bool MessagingActive
        {
            get { return this.messagingActive; }   
        }

        /// <summary>
        /// [1] -  	0b = request was rejected 
        ///         1b = request was accepted (see note, below) or switch was forced 
        /// </summary>
        internal bool RequestAccepted
        {
            get { return this.requestAccepted; }   
        }

        /// <summary>
        /// [0] -  	0b = mux is set to system (system can transmit and receive) 
        ///         1b = mux is set to BMC  (BMC can transmit. System can neither 
        ///         transmit nor receive) 
        /// </summary>
        internal bool MuxSetToSystem
        {
            get { return this.muxSetToSystem; }   
        }

    }
}
