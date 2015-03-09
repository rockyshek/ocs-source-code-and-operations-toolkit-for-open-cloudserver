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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary> This class object represents a batch of tests to run against a Chassis Manager. </summary>
    [DataContract]
    public class TestBatch
    {
        /// <summary> Absolute maximum limit of number of parallel threads to spin off for testing. </summary>
        private const int MaximumParallelThreadsLimit = 1000;

        /// <summary> Gets or sets Name of the Batch. </summary>
        [DataMember(IsRequired = true, Order = 1)]
        public string Name { get; set; }

        /// <summary> Gets or sets MaximumParallelThreads. </summary>
        [DataMember(Order = 2)]
        public uint MaximumParallelThreads { get; set; }

        /// <summary> Gets or sets TestSequences. </summary>
        [DataMember(IsRequired = true, Order = 3)]
        public IEnumerable<TestSequence> TestSequences { get; set; }

        /// <summary> Gets or sets Duration. </summary>
        [DataMember(Order = 4)]
        public TimeSpan? Duration { get; set; }

        /// <summary> Gets or sets SaveResultFrequency. </summary>
        [DataMember(Order = 5)]
        public TimeSpan? SaveResultFrequency { get; set; }

        /// <summary> Gets or sets GlobalParameters. </summary>
        [DataMember]
        public Parameters GlobalParameters { get; set; }

        /// <summary> Gets or sets ApiSla.  If an API does not respond within this timeframe a message is displayed. </summary>
        [DataMember]
        public TimeSpan? ApiSla { get; set; }

        /// <summary> Gets or sets ShuffleSequences. </summary>
        [DataMember]
        public bool? ShuffleSequences { get; set; }

        /// <summary> Gets or sets UserCredentials. </summary>
        [DataMember]
        public List<UserCredential> UserCredentials { get; set; }

        /// <summary> Gets or sets BatchDefinitionFile where the tests were loaded from. </summary>
        [IgnoreDataMember]
        public string BatchDefinitionFile { get; set; }

        /// <summary>
        /// Loads a batch definition from XML file.
        /// </summary>
        /// <param name="batchDefinitionFile"> Name of the XML file. </param>
        /// <returns> A batch definition object. </returns>
        public static TestBatch LoadBatch(string batchDefinitionFile)
        {
            TestBatch batch = null;
            try
            {
                batch = Helper.LoadFromFile<TestBatch>(batchDefinitionFile);
                batch.BatchDefinitionFile = batchDefinitionFile;
                batch.SetDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to load Batch:{0};\r\n{1}", batchDefinitionFile, ex);
                throw;
            }

            return batch;
        }

        /// <summary> Sets default values if not specified or out of range. </summary>
        public void SetDefaults()
        {
            // Ensures MaximumParallelThreads value is between 1 - MaximumParallelThreadsLimit constant.
            this.MaximumParallelThreads = this.MaximumParallelThreads < 1
                                          ? 1
                                          : this.MaximumParallelThreads > MaximumParallelThreadsLimit
                                            ? MaximumParallelThreadsLimit
                                            : this.MaximumParallelThreads;
            if (this.TestSequences != null)
            {
                this.TestSequences.ToList().ForEach(ts => ts.SetDefaults());
            }

            if (!this.SaveResultFrequency.HasValue)
            {
                this.SaveResultFrequency = TimeSpan.FromSeconds(60);
            }

            if (!this.ShuffleSequences.HasValue)
            {
                this.ShuffleSequences = false;
            }
        }

        /// <summary> Runs all tests. </summary>
        /// <param name="globalParameters"> Global Parameters. </param>
        /// <param name="batchResults"> Results of batch. </param>
        /// <returns> Result of Test Batch. </returns>
        public void Run(Parameters globalParameters, ResultOfTestBatch batchResults, string userName, string userPassword)
        {
            if (this.GlobalParameters == null)
            {
                this.GlobalParameters = globalParameters;
            }

            var cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = (int)this.MaximumParallelThreads,
                CancellationToken = cts.Token
            };

            var includedApis = this.TestSequences.SelectMany(ts => ts.Tests).Select(test => test.Name).Distinct();
            var implementedApis = Helper.GetChassisManagerApiList();
            batchResults.ExcludedApis = implementedApis.Where(api => !includedApis.Contains(api));
            batchResults.NonExistingApis = includedApis.Where(api => !implementedApis.Contains(api));
            batchResults.BatchStartTime = DateTime.UtcNow;
            batchResults.BatchState = TestRunState.Running;
            batchResults.Save();

            var keepLooping = false;
            Timer loopingtimer;
            if (this.Duration.HasValue)
            {
                keepLooping = true;
                loopingtimer = new Timer(this.Duration.Value.TotalMilliseconds);
                loopingtimer.Elapsed += delegate(object sender, ElapsedEventArgs args)
                {
                    keepLooping = false;
                    Console.WriteLine("Batch time expired, Ending Batch.");
                    cts.Cancel();
                };

                loopingtimer.Start();
            }

            Timer saveTimer = null;
            if (this.SaveResultFrequency.HasValue)
            {
                saveTimer = new Timer(this.SaveResultFrequency.Value.TotalMilliseconds);
                saveTimer.Elapsed += delegate(object sender, ElapsedEventArgs args)
                {
                    batchResults.Save();
                };

                saveTimer.Start();
            }

            //// Set credentials to run under for each sequence.
            //Parallel.ForEach(this.TestSequences,
            //        ts =>
            //            {
            //                ts.RunAsUserCredentials = new List<UserCredential>();
            //                if (this.UserCredentials != null && !string.IsNullOrWhiteSpace(ts.RunAsRoles))
            //                {
            //                    var runAsUsers = ts.RunAsRoles.Split(',');
            //                    if (runAsUsers.Contains("*"))
            //                    {
            //                        ts.RunAsUserCredentials.AddRange(this.UserCredentials);
            //                    }
            //                    else
            //                    {
            //                        ts.RunAsUserCredentials.AddRange(this.UserCredentials.Where(
            //                            crd => runAsUsers.Contains(crd.Role, StringComparer.InvariantCultureIgnoreCase)));
            //                    }
            //                }

            //                if (!ts.RunAsUserCredentials.Any())
            //                {
            //                    ts.RunAsUserCredentials.Add(null); // Force use of default credentials if none specified;
            //                }
            //            });

            do
            {
                var sequencesToRun = this.TestSequences.ToList();
                if (this.ShuffleSequences.HasValue && this.ShuffleSequences.Value)
                {
                    sequencesToRun.Shuffle();
                }

                var loopResult = Parallel.ForEach(
                    sequencesToRun,
                    parallelOptions,
                    ts =>
                    {
                        var tsResults = ts.Run(
                            batchResults.ChassisManagerEndPoint,
                            this.GlobalParameters,
                            this.ApiSla,
                            userName,
                            userPassword);
                        lock (batchResults.TestResults)
                        {
                            batchResults.TestResults.AddRange(tsResults);
                        }

                        batchResults.Save();
                    });

                batchResults.BatchState = loopResult.IsCompleted &&
                                          batchResults.TestResults.All(t => t.State == TestRunState.RanSuccessfully)
                                          ? TestRunState.RanSuccessfully
                                          : TestRunState.RunFailed;
            }
            while (keepLooping);
            
            batchResults.BatchEndTime = DateTime.UtcNow;
        }
    }
}
