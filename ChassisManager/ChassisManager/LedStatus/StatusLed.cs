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
    /// Class for LED (turn on/off, status commands are supported)
    /// </summary>
    public class StatusLed : ChassisSendReceive
    {

        // default device Id for this command.
        private readonly byte deviceId = 0x01;

        /// <summary>
        /// Turns the Chassis Status LED on
        /// </summary>
        internal byte TurnLedOn()
        {
            LedOnResponse response = (LedOnResponse) this.SendReceive(DeviceType.RearAttentionLed, deviceId, new TurnOnLed(),
                typeof(LedOnResponse), (byte)PriorityLevel.User);

            if (response.CompletionCode != 0)
            {
                return response.CompletionCode;
            }
            else
            {
                return 0;
            }
        }


        /// <summary>
        /// Turns off LED
        /// </summary>
        internal byte TurnLedOff()
        {
            LedOffResponse response = (LedOffResponse) this.SendReceive(DeviceType.RearAttentionLed, deviceId, new TurnOffLed(),
                typeof(LedOffResponse), (byte)PriorityLevel.User);
            
            if (response.CompletionCode != 0)
            {
                return response.CompletionCode;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// get LED status
        /// </summary>
        internal Contracts.LedStatusResponse GetLedStatus()
        {
            Contracts.LedStatusResponse response = new Contracts.LedStatusResponse();
            response.completionCode = Contracts.CompletionCode.Failure;
            response.ledState = Contracts.LedState.NA;
            
            LedStatusResponse ledStatus = (LedStatusResponse)this.SendReceive(DeviceType.RearAttentionLed, deviceId, new LedStatusRequest(),
                typeof(LedStatusResponse), (byte)PriorityLevel.User);

            if (ledStatus.CompletionCode != 0)
            {
                return response;
            }
            else
            {
                if (ledStatus.LedStatus == 0)
                {
                    response.ledState = Contracts.LedState.OFF;
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else if (ledStatus.LedStatus == 1)
                {
                    response.ledState = Contracts.LedState.ON;
                    response.completionCode = Contracts.CompletionCode.Success;
                }
                else
                {
                    response.ledState = Contracts.LedState.NA;
                    response.completionCode = Contracts.CompletionCode.Failure;
                }
                return response;
            }
        }

        #region Led Request Structures

        [ChassisMessageRequest(FunctionCode.GetLedStatus)]
        internal class LedStatusRequest : ChassisRequest
        {
        }

        /// <summary>
        /// Turn on LED - only Command needed
        /// </summary>
        [ChassisMessageRequest(FunctionCode.TurnOnLed)]
        internal class TurnOnLed : ChassisRequest
        {
        }

        /// <summary>
        /// Reset request
        /// </summary>
        [ChassisMessageRequest(FunctionCode.TurnOffLed)]
        internal class TurnOffLed : ChassisRequest
        {
        }

        #endregion

        #region Led Response Structures

        [ChassisMessageResponse(FunctionCode.GetLedStatus)]
        internal class LedStatusResponse : ChassisResponse
        {
            private byte ledStatus;

            [ChassisMessageData(0)]
            public byte LedStatus
            {
                get { return this.ledStatus; }
                set { this.ledStatus = value; }
            }
        }

        /// <summary>
        /// Empty response for LED
        /// </summary>
        [ChassisMessageResponse(FunctionCode.TurnOnLed)]
        internal class LedOnResponse : ChassisResponse
        {
        }

        [ChassisMessageResponse(FunctionCode.TurnOffLed)]
        internal class LedOffResponse : ChassisResponse
        {
        }

        #endregion

    }
}
