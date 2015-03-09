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
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a bad completion code from a IPMI response message.
    /// </summary>
    [Serializable()]
    public class IpmiResponseException : IpmiException
    {
        /// <summary>
        /// Non-zero completion code returned within a IPMI response message.
        /// </summary>
        private readonly byte completionCode;

        /// <summary>
        /// default constructor
        /// </summary>
        public IpmiResponseException(){
        }

        /// <summary>
        /// Initializes a new instance of the IpmiResponseException class.
        /// </summary>
        /// <remarks>Completion code returned within a IPMI response message.</remarks>
        public IpmiResponseException(byte completionCode)
        {           
            this.completionCode = completionCode;
        } 

        /// <summary>
        /// Initializes a new instance of the IpmiResponseException class with the
        /// specified string.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IpmiResponseException(string message, byte completionCode)
            : base(message)
        {
            this.completionCode = completionCode;
        }

        /// <summary>
        /// Initializes a new instance of the IpmiResponseException class with the
        /// specified string.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IpmiResponseException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the IpmiResponseException class with a
        /// specified error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the innerException
        /// parameter is not a null reference, the current exception is raised in a catch block
        /// that handles the inner exception.
        /// </param>
        public IpmiResponseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the IpmiResponseException class with
        /// serialization information.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
        protected IpmiResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }


        /// <summary>
        /// Gets the completion code.
        /// </summary>
        /// <value>IPMI response message completion code.</value>
        public byte CompletionCode
        {
            get { return this.completionCode; }
        }
    }
}
