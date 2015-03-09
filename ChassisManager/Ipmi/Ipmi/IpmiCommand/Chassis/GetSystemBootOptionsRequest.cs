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
    /// Represents the IPMI 'Get System Boot Options' request message. See
    /// IPMI, 28.13 .
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Chassis,
        IpmiCommand.GetSystemBootOptions)]
    internal class GetSystemBootOptionsRequest : IpmiRequest
    {
        private byte parameterSelector;
        private byte setSelector;
        private byte blockSelector = 0x00; // by standard, currently always 0

        internal GetSystemBootOptionsRequest(byte parameterSelector,
            byte setSelector)
        {
            // TODO bit 7 of parameterSelector is reserved, should we check
            // that?
            this.parameterSelector = parameterSelector;
            this.setSelector = setSelector;
        }

        [IpmiMessageData(0)]
        public byte ParameterSelector
        {
            get { return this.parameterSelector; }
        }

        [IpmiMessageData(1)]
        public byte SetSelector
        {
            get { return this.setSelector; }
        }

        [IpmiMessageData(2)]
        public byte BlockSelector
        {
            get { return this.blockSelector; }
        }
    }
}
