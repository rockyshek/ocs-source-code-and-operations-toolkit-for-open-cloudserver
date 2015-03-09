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

namespace Microsoft.GFS.WCS.ChassisManager
{
    using System;

    /// <summary>
    /// Class for Watch Dog Timer (only enable and reset commands are supported)
    /// </summary>
    public class WatchDogTimer : ChassisSendReceive
    {
        protected byte _deviceId;

        /// <summary>
        /// Constructor for WatchDogTimer
        /// </summary>
        /// <param name="deviceId"></param>
        public WatchDogTimer(byte deviceId)
        {
            this._deviceId = deviceId;
        }

        /// <summary>
        /// Public function that calls internal watchdog enable function
        /// </summary>
        public void EnableWatchDogTimer()
        {
            EnableWatchDogTimer(this._deviceId);
        }
        /// <summary>
        /// Enables the WatchDog Timer during initialization
        /// </summary>
        private void EnableWatchDogTimer(byte deviceId)
        {
            this.SendReceive(DeviceType.WatchDogTimer, deviceId, new WatchDogEnable(),
                typeof(WatchDogEnableResponse), (byte) PriorityLevel.System);
        }

        /// <summary>
        /// Public function calls internal reset watchdog timer
        /// </summary>
        public void ResetWatchDogTimer()
        {
            ResetWatchDogTimer(this._deviceId);
        }
        /// <summary>
        /// Resets the WatchDog timer when called
        /// </summary>
        private void ResetWatchDogTimer(byte deviceId)
        {
            this.SendReceive(DeviceType.WatchDogTimer, deviceId, new WatchDogReset(),
                typeof(WatchDogResetResponse), (byte) PriorityLevel.System);
        }

        /// <summary>
        /// Enable chassis request - only Command needed
        /// </summary>
        [ChassisMessageRequest(FunctionCode.EnableWatchDogTimer)]
        internal class WatchDogEnable : ChassisRequest
        {
        }

        /// <summary>
        /// Reset request
        /// </summary>
        [ChassisMessageRequest(FunctionCode.ResetWatchDogTimer)]
        internal class WatchDogReset : ChassisRequest
        {
        }

        /// <summary>
        /// Empty response for watchdog timer enable
        /// </summary>
        [ChassisMessageResponse(FunctionCode.EnableWatchDogTimer)]
        internal class WatchDogEnableResponse : ChassisResponse
        {
        }

        /// <summary>
        /// Empty response for watchdog timer reset
        /// </summary>
        [ChassisMessageResponse(FunctionCode.ResetWatchDogTimer)]
        internal class WatchDogResetResponse : ChassisResponse
        {
        }
    }
}
