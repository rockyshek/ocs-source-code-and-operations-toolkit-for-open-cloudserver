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
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ChassisValidation
{
    public abstract class ChassisManagerRestClientBase : ChassisManagerClientProxyBase
    {
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
        /// Makes a REST request given the REST API name and the parameters.
        /// </summary>
        /// <typeparam name="TResponse">
        /// The response type.
        /// </typeparam>
        protected override TResponse MakeRequest<TResponse>(string httpMethod, string apiName,
            IDictionary<string, object> apiParams)
        {
            if (string.IsNullOrWhiteSpace(apiName))
            {
                throw new ArgumentNullException("apiName");
            }
            if (this.ServiceUri == null)
            {
                throw new Exception("ServiceUri cannot be null");
            }

            // construct request uri
            string queryString = this.GetQueryStringParams(apiParams);

            var requestUri = new UriBuilder(this.ServiceUri)
            {
                Path = apiName,
                Query = queryString
            }.Uri;

            var request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.UserAgent = Dns.GetHostName();
            request.Method = httpMethod;

            if (this.Timeout != default(int))
            {
                request.Timeout = this.Timeout;
            }
            if (this.Credential != null)
            {
                request.PreAuthenticate = true;
                request.Credentials = this.Credential;
            }

            Log.Debug("CmRestProxy", string.Format("Request: {0}", requestUri.ToString()));
            
            // ignore CM service certificate validation errors
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, errors) => true;

            // get response from CM service
            object responseObject = null;
            var serializer = new DataContractSerializer(typeof (TResponse));
            var response = (HttpWebResponse)request.GetResponse();
            
            using (var stream = response.GetResponseStream())
            {
                responseObject = serializer.ReadObject(stream);
            }

            // make sure the returned object is not null
            // CM service should never return empty response
            if (responseObject == null)
            {
                throw new SerializationException();
            }
            
            // write the response to string for logging
            var responseString = new StringBuilder();
            using (var writer = XmlWriter.Create(responseString))
            {
                serializer.WriteObject(writer, responseObject);
            }
            Log.Debug("CmRestProxy", string.Format("Response: {0}", responseString.ToString()));

            return (TResponse)responseObject;
        }

        private string GetQueryStringParams(IDictionary<string, object> apiParams)
        {
            StringBuilder queryStr = new StringBuilder();

            foreach (string key in apiParams.Keys)
            {
                if (key == "data")
                {
                    byte[] payload = (byte[])apiParams[key];
                    queryStr.Append(string.Format("{0}={1}&", key, Convert.ToBase64String(payload)));

                }
                else
                {
                    queryStr.Append(string.Format("{0}={1}&", key, apiParams[key]));
                }
            }
            //Trim the last & character
            if (queryStr.Length > 0 && queryStr[queryStr.Length - 1] == '&')
            {
                queryStr = queryStr.Remove(queryStr.Length - 1, 1);
            }

            return queryStr.ToString();
        }
    }
}
