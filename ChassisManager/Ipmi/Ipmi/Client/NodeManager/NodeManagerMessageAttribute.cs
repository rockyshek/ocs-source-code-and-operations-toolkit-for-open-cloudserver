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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager
{
    using System;

    /// <summary>
    /// Defines a class as an Node Manager message.
    /// </summary>
    internal abstract class NodeManagerMessageAttribute : Attribute
    {
        /// <summary>
        /// Node Manager message function.
        /// </summary>
        private readonly NodeManagerFunctions function;

        /// <summary>
        /// Node Manager message command within the current function.
        /// </summary>
        private readonly NodeManagerCommand command;

        /// <summary>
        /// Node Manager message lenght within the current function.
        /// </summary>
        private readonly int dataLength;

        /// <summary>
        /// Initializes a new instance of the NodeManagerMessageAttribute class.
        /// </summary>
        /// <param name="function">Node Manager message function.</param>
        /// <param name="command">Node Manager message command.</param>
        protected NodeManagerMessageAttribute(NodeManagerFunctions function, NodeManagerCommand command)
        {
            this.function = function;
            this.command = command;
        }

        /// <summary>
        /// Initializes a new instance of the NodeManagerMessageAttribute class.
        /// </summary>
        /// <param name="function">Node Manager message function.</param>
        /// <param name="command">Node Manager message command.</param>
        /// <param name="dataLength">Node Manager message data length.</param>
        protected NodeManagerMessageAttribute(NodeManagerFunctions function, NodeManagerCommand command, int dataLength)
        {
            this.function = function;
            this.command = command;
            this.dataLength = dataLength;
        }

        /// <summary>
        /// Gets the Node Manager message function.
        /// </summary>
        internal NodeManagerFunctions NodeManagerFunctions
        {
            get { return this.function; }
        }

        /// <summary>
        /// Gets the Node Manager message command.
        /// </summary>
        internal NodeManagerCommand NodeManagerCommand
        {
            get { return this.command; }
        }
    }
}
