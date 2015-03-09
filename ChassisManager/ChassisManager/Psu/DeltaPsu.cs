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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal class DeltaPsu : PsuBase
    {
        /// <summary>
        /// Lock used to affinitize on/off requests, and prevent repeated on/off
        /// </summary>
        private object locker = new object();

        /// <summary>
        /// Time in seconds where additional power off requests are not permitted.
        /// </summary>
        private int backoff = 30;

        /// <summary>
        /// Time since the last time the Psu was powered off
        /// </summary>
        private DateTime lastPowerOff;

        /// <summary>
        /// Function to determine if a the PSU can be turned off.  The purpose of
        /// this function is to prevent multiple reboots of a PSU in quick succession
        /// </summary>
        private bool PowerOffPermitted()
        {
            bool permitted = false;

            lock (locker)
            {
                if (DateTime.Now > lastPowerOff.AddSeconds(backoff))
                {
                    lastPowerOff = DateTime.Now;
                    permitted = true;
                }
                else
                {
                    permitted = false;
                }

                return permitted;
            }

        }

        /// <summary>
        /// Initializes instance of the class.
        /// </summary>
        /// <param name="deviceId"></param>
        internal DeltaPsu(byte deviceId)
            : base(deviceId)
        {
        }

        internal override CompletionCode SetPsuOnOff(bool off)
        {
            if (off)
            {
                if (PowerOffPermitted())
                    return this.SetPsuOnOff(PmBusCommandPayload.POWER_OFF);
                else
                    return CompletionCode.CmdFailedNotSupportedInPresentState;
            }
            else
            {
                return this.SetPsuOnOff(PmBusCommandPayload.POWER_ON);
            }
        }


        /// <summary>
        /// Set PSU On/OFF
        /// </summary>
        /// <param name="psuId">Psu Id</param>
        /// <param name="cmd">command ON or OFF</param>
        /// <returns>Completion code success/failure</returns>
        private CompletionCode SetPsuOnOff(PmBusCommandPayload payload)
        {
            CompletionCode returnPacket = new CompletionCode();
            returnPacket = CompletionCode.UnspecifiedError;

            try
            {
                PsuOnOffResponse response = new PsuOnOffResponse();

                response = (PsuOnOffResponse)this.SendReceive(this.PsuDeviceType, this.PsuId,
                new PsuBytePayloadRequest((byte)PmBusCommand.SET_POWER, (byte)payload, (byte)PmBusResponseLength.SET_POWER), typeof(PsuOnOffResponse));

                // check for completion code 
                if (response.CompletionCode != 0)
                {
                    returnPacket = (CompletionCode)response.CompletionCode;
                }
                else
                {
                    returnPacket = CompletionCode.Success;
                }
            }
            catch (System.Exception ex)
            {
                Tracer.WriteError("SetPsuOnOff failed with the exception: " + ex);
            }

            return returnPacket;
        }

    }
}
