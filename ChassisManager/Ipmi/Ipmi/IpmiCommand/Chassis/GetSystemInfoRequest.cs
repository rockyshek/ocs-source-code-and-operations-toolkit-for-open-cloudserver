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
    /// Represents the IPMI 'Chassis Control' chassis request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Application, IpmiCommand.GetSystemInfoParameters)]
    internal class GetSystemInfoRequest : IpmiRequest
    {

        /// <summary>
        /// Pramater Revision [Request Byte 1].
        /// </summary>
        private byte _getParamater = 0x00;

        /// <summary>
        /// Paramater Selector
        /// </summary>
        private byte _selector;

        /// <summary>
        /// Set Selector
        /// </summary>
        private byte _setSelector;

        /// <summary>
        /// Block Selector
        /// </summary>
        private byte _blockSelector;


        /// <summary>
        /// Initializes a new instance of the GetSystemInfoRequest class.
        /// </summary>
        /// <param name="operation">Operation to perform.</param>
        internal GetSystemInfoRequest(byte selector, byte setSelector = 0x00, byte blockSelector = 0x00)
        {

            this._selector = selector;
            
            this._setSelector = setSelector;

            this._blockSelector = blockSelector;
        }

        /// <summary>
        /// Gets the operation to perform.
        /// </summary>
        [IpmiMessageData(0)]
        public byte GetParameter
        {
            get { return this._getParamater; }
        }

        /// <summary>
        /// Gets the operation to perform.
        /// </summary>
        [IpmiMessageData(1)]
        public byte Selector
        {
            get { return this._selector; }
        }

        /// <summary>
        /// Gets the operation to perform.
        /// </summary>
        [IpmiMessageData(2)]
        public byte SetSelector
        {
            get { return this._setSelector; }
        }

        /// <summary>
        /// Gets the operation to perform.
        /// </summary>
        [IpmiMessageData(3)]
        public byte BlockSelector
        {
            get { return this._blockSelector; }
        }
    }
}
