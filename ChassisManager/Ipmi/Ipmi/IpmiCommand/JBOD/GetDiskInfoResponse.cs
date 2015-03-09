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
    [IpmiMessageResponse(IpmiFunctions.Oem, IpmiCommand.GetDiskInfo)]
    class GetDiskInfoResponse : IpmiResponse
    {
        /// <summary>
        /// Ipmi Unit of Measurement
        /// </summary>
        private byte _unit;

        /// <summary>
        /// Multiplier byte
        /// </summary>
        private byte _multiplier;

        /// <summary>
        /// Reading Byte Array
        /// </summary>
        private byte[] _reading;

        /// <summary>
        /// Reading Unit
        /// </summary>       
        [IpmiMessageData(0)]
        public byte Unit
        {
            get { return this._unit; }
            set { this._unit = value; }
        }

        /// <summary>
        /// Disk Reading Multiplier:
        /// [7] 1b = negative multiplier 
        ///     0b = positive multiplier 
        ///[6-0] Reading MS byte multiplier 
        /// </summary>       
        [IpmiMessageData(1)]
        public byte Multiplier
        {
            get { return this._multiplier; }
            set { this._multiplier = value; }
        }

        /// <summary>
        /// Disk/JBOD Reading:
        ///     UInt16 numeric value.
        /// </summary>       
        [IpmiMessageData(2)]
        public byte[] Reading
        {
            get { return this._reading; }
            set { this._reading = value; }
        }
    }
}
