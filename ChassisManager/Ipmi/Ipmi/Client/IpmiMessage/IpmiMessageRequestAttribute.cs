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
    using System;

    /// <summary>
    /// Defines a class as an IPMI request message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    sealed internal class IpmiMessageRequestAttribute : IpmiMessageAttribute
    {
        /// <summary>
        /// Initializes a new instance of the IpmiMessageRequestAttribute class.
        /// </summary>
        /// <param name="function">IPMI message function.</param>
        /// <param name="command">IPMI message command.</param>
        public IpmiMessageRequestAttribute(IpmiFunctions function, IpmiCommand command)
            : base(function, command, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the IpmiMessageRequestAttribute class.
        /// </summary>
        /// <param name="function">IPMI message function.</param>
        /// <param name="command">IPMI message command.</param>
        /// <param name="dataLength">IPMI message data length.</param>
        public IpmiMessageRequestAttribute(IpmiFunctions function, IpmiCommand command, int dataLength)
            : base(function, command, dataLength)
        {
        }
    }
}
