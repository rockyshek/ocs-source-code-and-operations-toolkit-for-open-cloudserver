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
    /// Represents the IPMI 'Start Serial Session' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.StartSerialSession)]
    internal class StartSerialSessionRequest : IpmiRequest
    {
        /// <summary>
        /// [0]   = Flush Buffer
        /// [7:1] = Inactivity Timeout in 30-second increments. 1-based
        /// </summary>
        private readonly byte messagePayload;


        /// <summary>
        /// Initialize instance of the class.
        /// </summary>
        /// <param name="flushBuffer">Flush the internal Console Buffer</param>
        /// <param name="timeoutInSecs">Session timeout in seconds. Zero implies no console session timeout</param>
        /// </summary>  
        internal StartSerialSessionRequest(bool flushBuffer, int timeoutInSecs)
        {
            // Calculate timeout in 30-second intervals. Round up to next 30 seconds.
            int timeoutIncrement = timeoutInSecs / 30;
            if ((timeoutInSecs % 30) != 0)
                timeoutIncrement++;
            
            byte payload = (byte)(timeoutIncrement << 1);

            if (flushBuffer)
                payload = (byte)(payload | 0x01);

            this.messagePayload = payload;

        }

        /// <summary>
        /// [0]   = Flush Buffer
        /// [7:1] = Inactivity Timeout in 30-second increments. 1-based.
        /// </summary>       
        [IpmiMessageData(0)]
        public byte MessagePayload
        {
            get { return this.messagePayload; }

        }

    }
}
