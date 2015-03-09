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
    /// Represents the IPMI 'Set Serial/Modem Mux Command' request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Transport, IpmiCommand.SetSerialModelMux)]
    class SetSerialMuxRequest : IpmiRequest
    {

        /// <summary>
        /// Channel Number
        /// [7:4] Reserved
        /// [3:0] Channel Number
        /// </summary>    
        byte channel;

        /// <summary>
        /// [7:4] Reserved
        /// [3:0] Channel Number
        /// Mux Setting
        /// </summary> 
        byte muxSetting;

        /// <summary>
        /// Set Serial/Modem Mux Command
        /// </summary>
        public SetSerialMuxRequest(byte channel, MuxSwtich mux)
        {
            // [7:4] Reserved
            // [3:0] Channel Number
            this.channel = (byte)(channel & 0x0F);
            this.muxSetting = (byte)mux;                           
        }

        /// <summary>
        /// Set Serial/Modem Mux Command
        /// </summary>
        public SetSerialMuxRequest(MuxSwtich mux)
        {
            // Channel number (0x0E == current channel this request was issued on).
            this.channel = 0x0E;
            this.muxSetting = (byte)mux;
        }    

        /// <summary>
        /// Sets the Channel Number.
        /// </summary>
        [IpmiMessageData(0)]
        public byte Channel
        {
            get { return this.channel; }
        }


        /// <summary>
        /// Mux Setting.
        /// </summary>
        [IpmiMessageData(1)]
        public byte MuxSetting
        {
            get { return this.muxSetting; }
        }
    }
}
