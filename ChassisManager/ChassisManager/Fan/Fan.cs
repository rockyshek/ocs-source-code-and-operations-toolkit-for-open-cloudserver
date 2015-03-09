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

    /// <summary>
    /// Fan Class
    /// </summary>
    internal class Fan : ChassisSendReceive
    {
        /// <summary>
        /// device Id
        /// </summary>
        private byte deviceId;

        /// <summary>
        ///  Device Type
        /// </summary>
        private DeviceType deviceType;
        
        public Fan(byte deviceId)
        {
            // set the type as Fan
            this.deviceType = DeviceType.Fan;

            // set the device Id
            this.deviceId = deviceId;
        }

        #region Fan Commands

        /// <summary>
        /// Gets the fan speed in Rpm
        /// </summary>
        internal FanSpeedResponse GetFanSpeed()
        {
            return GetFanSpeed(this.deviceId);
        }

        /// <summary>
        /// Gets Fan speed in RPM
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        internal FanSpeedResponse GetFanSpeed(byte deviceId)
        {
            // Get Fan Requirement
            FanSpeedResponse response = (FanSpeedResponse)this.SendReceive(deviceType, deviceId, new FanSpeedRequest(),
              typeof(FanSpeedResponse), (byte) PriorityLevel.System);

            if (response.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("GetFanSpeed - error getting fan speed, completion code: {0:X}", response.CompletionCode);
            }
            
            return response;
        }

        /// <summary>
        /// Sets the RPM of the fan
        /// </summary>
        /// <param name="RPM"></param>
        /// <returns></returns>
        public byte SetFanSpeed(byte pwm)
        {
            return SetFanSpeed(this.deviceId, pwm);
        }

        /// <summary>
        /// Sets RPM for a particular Fan
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="RPM"></param>
        /// <returns></returns>
        public byte SetFanSpeed(byte deviceId, byte PWM)
        {
            // Set fan speed and return set value
            FanSetResponse response = (FanSetResponse)this.SendReceive(deviceType, deviceId, new FanSetRpmRequest(PWM),
              typeof(FanSetResponse), (byte)PriorityLevel.System);

            return response.CompletionCode;
        }

        /// <summary>
        /// Gets status of fan. Calls GetFanSpeed internally to check if fan has a particular RPM 
        /// </summary>
        /// <returns></returns>
        public bool GetFanStatus()
        {
            FanSpeedResponse fanspeed = GetFanSpeed(this.deviceId);
            if (fanspeed.CompletionCode == (byte)CompletionCode.Success 
                && fanspeed.Rpm != 0)
            {
                return true;
            }

            // Log fan failure
            Tracer.WriteError("GetFanStatus: Fan {0} error. Completion code: {1}  RPM (only valid if Completion Code is 0): {2}", 
                this.deviceId, fanspeed.CompletionCode, fanspeed.Rpm);
            return false;
        }
        
        #endregion
        
    }
}
