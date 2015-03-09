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

namespace Microsoft.GFS.WCS.Test.Commands.Framework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Principal;

    using Microsoft.GFS.WCS.Test;
    using Microsoft.GFS.WCS.Test.Consoler;
    using Microsoft.GFS.WCS.Test.Framework;

    /// <summary> Commands for manipulating Framework. </summary>
    [ConsoleSuite]
    public static class Samples
    {
        /// <summary> Creates a sample Batch File. </summary>
        /// <param name="batchFile"> Name of the file where sample batch is to be saved. </param>
        [Action("Creates a Sample Batch File.")]
        public static void CreateSampleBatchFile(
            [Optional("SampleBatch.xml", Description = "Name of Batch File. Default=SampleBatch.xml")] string batchFile)
        {
            var batch = new TestBatch()
            {
                Name = Path.GetFileNameWithoutExtension(batchFile),
                Duration = TimeSpan.FromSeconds(145),
                ApiSla = TimeSpan.FromSeconds(100),
                GlobalParameters = Parameters.GetSampleParameters(),
                UserCredentials = new List<UserCredential>()
                {
                    new UserCredential()
                    {
                        Role = "Admin",
                        UserName = "myuserName",
                        Password = "mypassword"
                    }
                },

                TestSequences = new List<TestSequence>()
                {
                    new TestSequence()
                    {
                        SequenceName = "Run My API 1 and 2 using List",
                        SequenceIterations = 100,
                        DelayBetweenSequenceIterationsInMS = 0,
                        RotateParametersValueBetweenIterations = true,
                        ApiSla = TimeSpan.FromSeconds(1000),
                        RunAsRoles = "*",
                        UseLocalParametersOnly = true,

                        Tests = new List<Test>()
                        {
                            new Test()
                            {
                                Name = "myApi-1",
                                Iterations = 10,                                
                                DelayBetweenIterationsInMS = 2
                            },
                            new Test()
                            {
                                Name = "myLongRunningApi-2",
                                DelayBetweenIterationsInMS = 2,
                                ApiSla = TimeSpan.FromSeconds(10000)
                            }
                        },

                        LocalParameters = new Dictionary<string, List<string>>()
                        {
                            { "param1", new List<string>() { "P1val1", "P1val2" } },
                            { "param2", new List<string>() { "P2val1", "P2val2" } }
                        }
                    }
                }
            };

            Helper.SaveToFile(batch, batchFile);

            // Load and re-Save to grab all default values.
            var batchLoaded = Helper.LoadFromFile<TestBatch>(batchFile);
            batchLoaded.SetDefaults();
            Helper.SaveToFile(batchLoaded, batchFile);
        }

        /// <summary> Creates a sample Batch File. </summary>
        /// <param name="batchRunResultFile"> Name of the file where sample batch run is to be saved. </param>
        [Action("Creates a Sample Result File.")]
        public static void CreateSampleBatchRunResultFile(
            [Optional("SampleResultOfTestBatch.xml",
                Description = "Name of Result File. Default=SampleResultOfTestBatch.xml")]
            string batchRunResultFile)
        {
            var batchRun = new ResultOfTestBatch(batchRunResultFile, "http://MyChassis:8000")
            {
                BatchStartTime = DateTime.UtcNow,
                BatchEndTime = DateTime.UtcNow,
                BatchState = TestRunState.RanSuccessfully
            };

            batchRun.TestResults.Add(new ResultOfTest()
            {
                Name = "MyAPi1",
                IterationsExecutedSuccessfully = 1,
                RestUri = "http://MyChassis:8000/api1",
                SequenceInstance = 1,
                SequenceName = "MyTestSequence",
                StartTime = DateTime.UtcNow,
                State = TestRunState.RanSuccessfully,
                TotalExecutionTime = TimeSpan.FromTicks(1)
            });

            var start = DateTime.UtcNow;

            batchRun.TestResults.Add(new ResultOfTest()
            {
                Name = "MyAPi2",
                IterationsExecutedSuccessfully = 1,
                RestUri = "http://MyChassis:8000/api2",
                SequenceInstance = 1,
                SequenceName = "MyTestSequence",
                StartTime = start,
                State = TestRunState.RanSuccessfully,
                TotalExecutionTime = DateTime.UtcNow.Subtract(start),
            });

            Helper.SaveToFile(batchRun, batchRunResultFile);
        }



    }
}
