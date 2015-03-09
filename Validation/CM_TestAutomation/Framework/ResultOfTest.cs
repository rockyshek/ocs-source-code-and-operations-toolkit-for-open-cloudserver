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

namespace Microsoft.GFS.WCS.Test.Framework
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Xml;

    /// <summary>
    /// This class object represents a batch of tests to run against a Chassis Manager.
    /// </summary>
    [DataContract]
    public class ResultOfTest
    {
        /// <summary> Gets or sets Name. </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary> Gets FirstSuccessfulResponse. </summary>
        [DataMember]
        public string FirstSuccessfulResponse { get; private set; }

        /// <summary> Gets LastSuccessfulResponse. </summary>
        [DataMember]
        public string LastSuccessfulResponse { get; private set; }

        /// <summary> Gets FailedResponse. </summary>
        [DataMember]
        public string FailedResponse { get; private set; }

        /// <summary> Gets FailedResponseStatusCode. </summary>
        [DataMember]
        public HttpStatusCode? FailedResponseStatusCode { get; private set; }

        /// <summary> Gets or sets SequenceName. </summary>
        [DataMember]
        public string SequenceName { get; set; }

        /// <summary> Gets or sets RunAsUserName. </summary>
        [DataMember]
        public string RunAsUserName { get; set; }

        /// <summary> Gets or sets SequenceInstance. </summary>
        [DataMember]
        public int SequenceInstance { get; set; }

        /// <summary> Gets or sets RestUri. </summary>
        [DataMember]
        public string RestUri { get; set; }

        /// <summary> Gets or sets State.</summary>
        [DataMember]
        public TestRunState State { get; set; }

        /// <summary> Gets or sets State.</summary>
        [DataMember]
        public DateTime? StartTime { get; set; }

        /// <summary> Gets or sets TotalExecutionTime.</summary>
        [DataMember]
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary> Gets or sets IterationsExecuted.</summary>
        [DataMember]
        public uint IterationsExecutedSuccessfully { get; set; }

        /// <summary> Gets or sets ErrorMessage.</summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary> Gets AverageExecutionTime. </summary>
        [DataMember]
        public TimeSpan AverageExecutionTime { get; set; }

        /// <summary>
        /// Outputs key properties for easy display.
        /// </summary>
        /// <returns> A formatted string having key properties of object. </returns>
        public override string ToString()
        {
            var errMsg = this.FailedResponseStatusCode == null
                         ? string.Empty
                         : string.Format("{0}:{1}", this.FailedResponseStatusCode, this.ErrorMessage);
            return string.Format(
                "{0}\t{1}\t{2}; (x{3}={4}s) \t{5}",
                this.Name,
                this.RestUri,
                this.State,
                this.IterationsExecutedSuccessfully,
                this.TotalExecutionTime,
                errMsg);
        }

        /// <summary>
        /// Process HttpWeb response.
        /// </summary>
        /// <param name="response"> A Http web response object to be processed. </param>
        public void ProcessResponse(HttpWebResponse response)
        {
            if (response == null)
            {
                this.FailedResponse = "null Response";
                this.ErrorMessage = "null Response";
                this.State = TestRunState.RunFailed;
                return;
            }
            try
            {
                using (XmlReader xmlResult = new XmlTextReader(response.GetResponseStream()))
                {
                    xmlResult.ReadToFollowing("completionCode");
                    if (!xmlResult.ReadString().Equals("Success"))
                    {
                        this.State = TestRunState.RunFailed;
                        return;
                    }
                    else
                    {
                        this.State = TestRunState.RanSuccessfully;
                        this.IterationsExecutedSuccessfully++;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
