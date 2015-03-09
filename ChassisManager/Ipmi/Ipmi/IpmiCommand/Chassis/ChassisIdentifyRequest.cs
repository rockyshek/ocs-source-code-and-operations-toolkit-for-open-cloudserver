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
    /// Represents the IPMI 'Chassis Identify' chassis request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Chassis, IpmiCommand.ChassisIdentify, 2)]
    internal class ChassisIdentifyRequest : IpmiRequest
    {
        /// <summary>
        /// Identify interval in seconds.
        /// </summary>
        private readonly byte interval;

        /// <summary>
        /// Initializes a new instance of the ChassisIdentifyRequest class.
        /// </summary>
        /// <param name="interval">Identify interval in seconds.</param>
        internal ChassisIdentifyRequest(byte interval)
        {
            this.interval = interval;
        }

        /// <summary>
        /// Gets the interval in seconds.
        /// </summary>
        [IpmiMessageData(0)]
        public byte Interval
        {
            get { return this.interval; }
        }

        /// <summary>
        /// Gets the interval in seconds.
        /// </summary>
        [IpmiMessageData(1)]
        public byte ForceOn
        {
            get { return (this.interval == 0xFF) ? (byte)0x01 : (byte)0x00; }
        }
    }
}
