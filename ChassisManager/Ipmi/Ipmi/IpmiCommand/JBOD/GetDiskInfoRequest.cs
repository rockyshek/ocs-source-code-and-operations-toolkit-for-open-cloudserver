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
    /// Represents the IPMI 'Get Disk Info Command for WCS JBOD' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Oem, IpmiCommand.GetDiskInfo)]
    internal class GetDiskInfoRequest : IpmiRequest
    {

        /// <summary>
        /// JBOD Expander Channel.  Default = 0x00
        /// </summary>
        private readonly byte _channel = 0x00;

        /// <summary>
        /// JBOD Disk Number.  Default = 0x00,
        /// which indicates individual disks are 
        /// not supported, JBOD information is
        /// returned instead.
        /// </summary>
        private readonly byte _disk = 0x00;

        /// <summary>
        /// Get Disk Info Request
        /// </summary>
        internal GetDiskInfoRequest()
        { }

        /// <summary>
        /// Initialize Get Disk Info Request
        /// </summary>
        /// <param name="channel">JBOD Channel Number</param>
        /// <param name="disk">Disk Number</param>
        internal GetDiskInfoRequest(byte channel, byte disk)
        {
            this._channel = channel;
            this._disk = disk;
        }

        /// <summary>
        /// Channel Byte
        /// </summary>       
        [IpmiMessageData(0)]
        public byte Channel
        {
            get { return this._channel; }

        }

        /// <summary>
        /// Disk Byte
        /// </summary>       
        [IpmiMessageData(1)]
        public byte Disk
        {
            get { return this._disk; }

        }

    }
}
