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
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// This class object represents a sequence of tests that validate a scenario.
    /// </summary>
    [DataContract]
    public class TestSequence
    {
        /// <summary> List of APIs and respective parameters. </summary>
        //private static readonly Dictionary<string, List<string>> apiParameters =
        //    new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary> Gets or sets SequenceName. </summary>
        [DataMember(IsRequired = true, Order = 1)]
        public string SequenceName { get; set; }

        /// <summary> Gets or sets DelayBetweenSequenceIterations in one thousandth of a second.</summary>
        [DataMember(Order = 2)]
        public uint DelayBetweenSequenceIterationsInMS { get; set; }

        /// <summary> Gets or sets Iterations. </summary>
        [DataMember(Order = 3)]
        public uint SequenceIterations { get; set; }

        /// <summary> Gets or sets a value indicating whether parameters value are rotated between iterations.</summary>
        [DataMember(Order = 4)]
        public bool RotateParametersValueBetweenIterations { get; set; }

        /// <summary> Gets or sets a value indicating whether to use local parameters only.</summary>
        [DataMember(Order = 5)]
        public bool UseLocalParametersOnly { get; set; }

        /// <summary> Gets or sets a value indicating whether to halt sequence on a Missging Parameter. </summary>
        [DataMember(Order = 6)]
        public bool OnMissingParameterHaltSequence { get; set; }

        /// <summary> Gets or sets Tests. </summary>
        [DataMember(IsRequired = true)]
        public List<Test> Tests { get; set; }

        /// <summary> Gets or sets LocalParameters.</summary>
        [DataMember]
        public Dictionary<string, List<string>> LocalParameters { get; set; }

        /// <summary> Gets or sets ApiSla.  If an API does not respond within this timeframe a message is displayed. </summary>
        [DataMember]
        public TimeSpan? ApiSla { get; set; }

        //[IgnoreDataMember]
        //public List<UserCredential> RunAsUserCredentials { get; set; }

        ///// <summary> Gets or sets RunAsUserCredentials. </summary>
        /// <summary> 
        /// Gets or sets RunAsRoles.  
        /// A comma separated list of roles to run the sequence as. 
        /// A * indicates to use all available roles from Batch.
        /// </summary>
        [DataMember]
        public string RunAsRoles { get; set; }

        /// <summary> Sets default values if not specified or out of range. </summary>
        public void SetDefaults()
        {
            this.SequenceIterations = this.SequenceIterations == 0 ? 1 : this.SequenceIterations;
            this.Tests.ForEach(test => test.SetDefaults());
        }

        /// <summary> Runs the Task Sequence Sequentially. </summary>
        /// <param name="endpoint"> Chassis Manager Endpoint. </param>
        /// <param name="globalParameters"> Global parameter values. </param>
        /// <returns> A list of TestRun objects. </returns>
        public List<ResultOfTest> Run(string endpoint, Parameters globalParameters, TimeSpan? apiSlaFromBatch, string userName, string userPassword)
        {
            if (!this.ApiSla.HasValue && apiSlaFromBatch.HasValue)
            {
                this.ApiSla = apiSlaFromBatch;
            }

            var testResults = new List<ResultOfTest>();

            Console.WriteLine("\n***Start: {0} TimeNow:{1}\n", this.SequenceName, DateTime.UtcNow);

            // Merges and/or overlays globalParameters with LocalParameters based on UseLocalParametersOnly.
            var parameters = (Parameters)globalParameters.Clone();
            if (this.LocalParameters != null && this.LocalParameters.Count > 0)
            {
                foreach (var param in this.LocalParameters)
                {
                    parameters[param.Key, !this.UseLocalParametersOnly] = param.Value;
                }
            }

            //Shuffling all parameters by default; Can add a control if needed to turn this feature on/off.
            parameters.Shuffle();

            var requiredParamValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            //this.Tests.ForEach(t => Helper.GetChassisManagerApiParameterList(t.Name).ForEach(p => requiredParamValues[p] = null));
            for (var i = 0; i < this.SequenceIterations; i++)
            {
                // Get new set of values between iterations or if it's first time in the loop.
                if (this.RotateParametersValueBetweenIterations || i == 0)
                {
                    requiredParamValues.Keys.ToList().ForEach(k => requiredParamValues[k] = parameters[k, -1]);
                }

                this.Tests.ForEach(
                    test =>
                    {
                        try
                        {
                            if (!test.SkipTest)
                            {
                                Helper.GetChassisManagerApiParameterList(test.Name).ForEach(p =>
                                {
                                    if (!requiredParamValues.ContainsKey(p))
                                    {
                                        requiredParamValues[p] = parameters[p, -1];
                                    }
                                });
                                var testResult = test.Run(endpoint, this.SequenceName, i, requiredParamValues, this.ApiSla, userName, userPassword);
                                Console.WriteLine(testResult);

                                //// TODO: Validate test results by examining testRun.LastSuccessfulResponse.

                                testResults.Add(testResult);
                            }
                        }
                        catch (KeyNotFoundException ex)
                        {
                            Console.WriteLine("Skipping test {0}; Missing parameter:{1}", test.Name, ex);
                            test.SkipTest = true;
                        }
                    });
            }

            Console.WriteLine("\n***END: {0} Iterations: TimeNow:{1}\n", this.SequenceName, DateTime.UtcNow);
            return testResults;
        }
    }
}
