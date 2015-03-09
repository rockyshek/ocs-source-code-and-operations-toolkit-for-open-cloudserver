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
    /// Represents the IPMI 'Get Chassis Status' response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Chassis, IpmiCommand.GetChassisStatus)]
    internal class GetChassisStatusResponse : IpmiResponse
    {
        /// <summary>
        /// Current power state.
        /// </summary>
        private byte currentPowerState;

        /// <summary>
        /// Last power event.
        /// </summary>
        private byte lastPowerEvent;

        /// <summary>
        /// Miscellaneous chassis state.
        /// </summary>
        private byte miscellaneousChassisState;

        /// <summary>
        /// Front panel button capabilities and disable/enable status (optional).
        /// </summary>
        private byte frontPanelButton;

        /// <summary>
        /// Gets and sets the Current power state.
        /// </summary>
        /// <value>Current power state.</value>
        [IpmiMessageData(0)]
        public byte CurrentPowerState
        {
            get { return this.currentPowerState; }
            set { this.currentPowerState = value; }
        }

        /// <summary>
        /// Gets and sets the Last power event.
        /// </summary>
        /// <value>Last power event.</value>
        [IpmiMessageData(1)]
        public byte LastPowerEvent
        {
            get { return this.lastPowerEvent; }
            set { this.lastPowerEvent = value; }
        }

        /// <summary>
        /// Gets and sets the Miscellaneous chassis state.
        /// </summary>
        /// <value>Miscellaneous chassis state.</value>
        [IpmiMessageData(2)]
        public byte MiscellaneousChassisState
        {
            get { return this.miscellaneousChassisState; }
            set { this.miscellaneousChassisState = value; }
        }

        /// <summary>
        /// Gets and sets the Front panel button capabilities and disable/enable status (optional).
        /// </summary>
        /// <value>Front panel button capabilities and disable/enable status (optional).</value>
        [IpmiMessageData(3)]
        public byte FrontPanelButton
        {
            get { return this.frontPanelButton; }
            set { this.frontPanelButton = value; }
        }
    }
}
