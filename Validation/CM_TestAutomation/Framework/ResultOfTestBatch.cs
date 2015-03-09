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
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// This class object represents Execution of a batch and its results.
    /// </summary>
    [DataContract]
    public class ResultOfTestBatch
    {
        /// <summary> Initializes a new instance of the ResultOfTestBatch class. </summary>
        /// <param name="name"> Name of Batch. </param>
        /// <param name="chassisManagerEndPoint">Chassis Manager endpoint.</param>
        public ResultOfTestBatch(string name, string chassisManagerEndPoint)
        {
            this.Name = name;
            this.ChassisManagerEndPoint = chassisManagerEndPoint;
            this.TestResults = new List<ResultOfTest>();
            this.ExcludedApis = new List<string>();
            this.BatchState = TestRunState.NotStarted;
        }

        /// <summary> Gets Name. </summary>
        [DataMember]
        public string Name { get; private set; }

        /// <summary> Gets ChassisManagerEndPoint. </summary>
        [DataMember]
        public string ChassisManagerEndPoint { get; private set; }

        /// <summary> Gets or sets BatchStartTime. </summary>
        [DataMember]
        public DateTime? BatchStartTime { get; set; }

        /// <summary> Gets or sets BatchEndTime. </summary>
        [DataMember]
        public DateTime? BatchEndTime { get; set; }

        /// <summary> Gets or sets BatchEndTime. </summary>
        [DataMember]
        public TestRunState BatchState { get; set; }

        /// <summary> Gets or sets ExcludedApis. </summary>
        [DataMember]
        public IEnumerable<string> ExcludedApis { get; set; }

        /// <summary> Gets or sets NonExistingApis. </summary>
        [DataMember]
        public IEnumerable<string> NonExistingApis { get; set; }

        /// <summary> Gets TestRuns. </summary>
        [DataMember]
        public List<ResultOfTest> TestResults { get; private set; }

        /// <summary> Converts object to string.</summary>
        /// <returns> A formatted string. </returns>
        public override string ToString()
        {
            return string.Format(
                "BatchRun: Name={0}, State={1}, Chassis={2}, Start={3}, TimeNow={4}, TotalTestsExecuted={5}, Succ={6}",
                this.Name,
                this.BatchState,
                this.ChassisManagerEndPoint,
                this.BatchStartTime,
                DateTime.UtcNow,
                this.TestResults.Count,
                this.TestResults.Where(t => t.IterationsExecutedSuccessfully != 0));
        }

        /// <summary> Saves results to file. </summary>
        public void Save()
        {
            lock (this.TestResults)
            {
                var fName = string.Format("{0}-{1}.Results.xml", this.ChassisManagerEndPoint, this.Name);
                fName = Path.GetInvalidFileNameChars().Aggregate(fName, (ch, invalid) => ch.Replace(invalid, '_'));
                Helper.SaveToFile(this, fName);
            }
        }
    }
}
