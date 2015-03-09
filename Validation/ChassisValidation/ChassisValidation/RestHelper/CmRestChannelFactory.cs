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

using System;
using System.Linq;
using System.Net;

namespace ChassisValidation
{
    /// <summary>
    /// The factory class to generate REST channel instances.
    /// The instance will be derived from CmRestClientBase and implement
    /// TChannel interface.
    /// </summary>
    /// <typeparam name="TChannel">
    /// The interface including all the APIs.
    /// </typeparam>
    public class CmRestChannelFactory<TChannel>
    {
        public CmRestChannelFactory(string serviceUri)
        {
            this.ServiceUri = new Uri(serviceUri);
        }

        /// <summary>
        /// The URI of the REST service endpoint.
        /// </summary>
        public Uri ServiceUri { get; set; }

        /// <summary>
        /// The timeout value in milliseconds for waiting the response
        /// </summary>
        public Int32 Timeout { get; set; }

        /// <summary>
        /// The client credential.
        /// </summary>
        public NetworkCredential Credential { get; set; }

        /// <summary>
        /// Create a channel instance which implements the TChannel interface.
        /// </summary>
        /// <returns>
        /// An instance which implements the TChannel interface.
        /// </returns>
        public TChannel CreateChannel()
        {
            var type = ChassisManagerRestProxyGenerator.CreateType(typeof(TChannel), typeof(ChassisManagerRestClientBase));
            var obj = Activator.CreateInstance(type);
            var client = (ChassisManagerRestClientBase)obj;
            
            // pass over properties needed to make REST requests
            client.ServiceUri = this.ServiceUri;
            client.Credential = this.Credential;
            client.Timeout = this.Timeout;

            return (TChannel)obj;
        }
    }
}
