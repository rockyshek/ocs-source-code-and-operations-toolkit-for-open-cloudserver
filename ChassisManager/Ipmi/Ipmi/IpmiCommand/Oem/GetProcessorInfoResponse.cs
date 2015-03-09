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
    /// Represents the IPMI 'Get Processor Info Command' request message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetProcessorInfo)]
    class GetProcessorInfoResponse : IpmiResponse
    {
        /// <summary>
        /// Processor Type
        /// </summary>
        private byte _type;

        /// <summary>
        /// Processor Frequency
        /// </summary>
        private ushort _frequency;

        /// <summary>
        /// Processor State
        /// </summary>
        private byte _state;

        /// <summary>
        /// Processor Type
        /// </summary>       
        [IpmiMessageData(0)]
        public byte ProcessorType
        {
            get { return this._type; }
            set { this._type = value; }
        }

        /// <summary>
        /// Processor Frequency 
        /// </summary>       
        [IpmiMessageData(1)]
        public ushort Frequency
        {
            get { return this._frequency; }
            set { this._frequency = value; }
        }

        /// <summary>
        /// Processor State
        /// </summary>       
        [IpmiMessageData(3)]
        public byte ProcessorState
        {
            get { return this._state; }
            set { this._state = value; }
        }
    }
}
