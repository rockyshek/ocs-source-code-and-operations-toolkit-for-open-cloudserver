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
    /// Defines a class as an IPMI message.
    /// </summary>
    internal abstract class IpmiMessageAttribute : Attribute
    {
        /// <summary>
        /// IPMI message function.
        /// </summary>
        private readonly IpmiFunctions function;

        /// <summary>
        /// IPMI message command within the current function.
        /// </summary>
        private readonly IpmiCommand command;

        /// <summary>
        /// IPMI message lenght within the current function.
        /// </summary>
        private readonly int dataLength;

        /// <summary>
        /// Initializes a new instance of the IpmiMessageAttribute class.
        /// </summary>
        /// <param name="function">IPMI message function.</param>
        /// <param name="command">IPMI message command.</param>
        protected IpmiMessageAttribute(IpmiFunctions function, IpmiCommand command)
        {
            this.function = function;
            this.command = command;
        }

        /// <summary>
        /// Initializes a new instance of the IpmiMessageAttribute class.
        /// </summary>
        /// <param name="function">IPMI message function.</param>
        /// <param name="command">IPMI message command.</param>
        /// <param name="dataLength">IPMI message data length.</param>
        protected IpmiMessageAttribute(IpmiFunctions function, IpmiCommand command, int dataLength)
        {
            this.function = function;
            this.command = command;
            this.dataLength = dataLength;
        }

        /// <summary>
        /// Gets the IPMI message function.
        /// </summary>
        internal IpmiFunctions IpmiFunctions
        {
            get { return this.function; }
        }

        /// <summary>
        /// Gets the IPMI message command.
        /// </summary>
        internal IpmiCommand IpmiCommand
        {
            get { return this.command; }
        }
    }
}
