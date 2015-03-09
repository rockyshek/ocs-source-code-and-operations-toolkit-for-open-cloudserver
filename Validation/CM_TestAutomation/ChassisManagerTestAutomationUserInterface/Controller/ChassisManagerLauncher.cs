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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.GFS.WCS.Test;
using Microsoft.GFS.WCS.Test.Framework;

namespace CMTestAutomationInterface.Controller
{
    internal class ChassisManagerLauncher
    {
        private readonly static string userName = ConfigurationManager.AppSettings["UserName"];
        private readonly static string passWord = ConfigurationManager.AppSettings["Password"];

        internal ChassisManagerLauncher()
        { 
        }

        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="bDir"> Directory where the batches reside. </param>
        /// <param name="endPt"> CM Url to run batch against. </param>        
        public static void RunBatches(string bDir, string endPt)
        {
            string user = userName;
            string pass = passWord;

            var framework = new CmTestWithFramework();
            framework.RunAllFrameworkBatches(bDir, endPt, user, pass);
        }

        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="bDir"> Directory where the batches reside. </param>
        /// <param name="endPt"> CM Url to run batch against. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void RunBatches(string bDir, string endPt, string user, string pass)
        {
            var framework = new CmTestWithFramework();
            framework.RunAllFrameworkBatches(bDir, endPt, user, pass);
        }

        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="bDeFi"> Directory where the batches reside. </param>
        /// <param name="endPt"> CM Url to run batch against. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void RunBatch(string bDFile, string endPt)
        {
            string user = userName;
            string pass = passWord;

            var framework = new CmTestWithFramework();
            framework.RunFrameworkBatch(bDFile, endPt, user, pass);
        }

        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="bDFile"> Directory where the batches reside. </param>
        /// <param name="endPt"> CM Url to run batch against. </param>
        /// <param name="user"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void RunBatch(string bDFile, string endPt, string user, string pass)
        {
            var framework = new CmTestWithFramework();
            framework.RunFrameworkBatch(bDFile, endPt, user, pass);
        }

        /// <summary>
        /// All parameters are taken from the config file of the CM_TestAutomation project. </param>           
        public static void RunFuncBvt(string endPt, string skuFile)
        {
            string user = userName;
            string pass = passWord;

            var framework = new CmTestWithFramework();
            framework.RunFunctionalBvt(endPt, skuFile, user, pass);
        }

        /// <summary>
        /// All parameters are taken from the config file of the CM_TestAutomation project. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void RunFuncBvt(string endPt, string skuFile, string user, string pass)
        {
            var framework = new CmTestWithFramework();
            framework.RunFunctionalBvt(endPt, skuFile, user, pass);
        }

        /// <summary>
        /// <param name="endPt"> CM Url to run batch against. </param>
        /// <param name="user"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void VerifyCSpec(string endPt, string skuFile)
        {
            string user = userName;
            string pass = passWord;

            var framework = new CmTestWithFramework();
            framework.VerifyChassisSpec(endPt, skuFile, user, pass);
        }

        /// <summary>
        /// <param name="endPt"> CM Url to run batch against. </param>
        /// <param name="user"> User name to use to connect to the target CM. </param>
        /// <param name="pass"> Password for the user we are using to connect to the CM. </param>        
        public static void VerifyCSpec(string endPt, string skuFile, string user, string pass)
        {
            var framework = new CmTestWithFramework();
            framework.VerifyChassisSpec(endPt, skuFile, user, pass);
        }

        /// <summary> Prints List of CM APIs or signature of a given API. </summary>
        /// <param name="apiName"> Name of API to get information for. </param>
        public static void GetCmApiInfo()
        { 
            Console.WriteLine(Helper.DumpAllChassisManagerApi());           
        }

        /// <summary> Prints List of CM APIs or signature of a given API. </summary>
        /// <param name="apiName"> Name of API to get information for. </param>
        public static void GetCmApiInfo(string apiName)
        { 
            Console.WriteLine(Helper.DumpChassisManagerApi(apiName));
        }

        /// <summary>
        /// Update the file counter based on the 
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        public static void FixCounterNames(string inFile, string outFile)
        {
            string fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
            string fileName = Path.Combine(fileDirectory, "APICounterMapping.xml");

            var map = Helper.LoadFromFile<Dictionary<string, string>>(fileName);
            var contents = File.ReadLines(inFile).ToList();
            if (contents.Any())
            {
                var header = contents[0];
                header = header.Replace("Chass19.IChas09.", string.Empty);
                header = header.Replace("Chass19.IChassisManager.", string.Empty);
                header = header.Replace("HTTPS:||LOCALHOST:8000|", "");

                foreach (var mapping in map)
                {
                    header = header.Replace(mapping.Key, mapping.Value);
                }

                contents[0] = header;
                File.WriteAllLines(outFile, contents);
                Console.WriteLine(string.Format("Output file {0} created with fixed counter names", outFile));
            }
        }
         /// <summary>
        /// Create summary report from batch test result
        /// </summary>
        /// <param name="saveResult"></param>
        /// <param name="Userby"></param>
        public static void SummaryReports(string batchResultFileName)
        {
            string sortBy = "NAME";
            SummaryReports(batchResultFileName, sortBy);
        }

        /// <summary>
        /// Create summary report from batch test result
        /// </summary>
        /// <param name="saveResult"></param>
        /// <param name="Userby"></param>
        public static void SummaryReports(string batchResultFileName, string by)
        {
            by = by.Trim().ToUpperInvariant();
            var errorResults = new List<ResultOfTest>();
            var batchResult = Helper.LoadFromFile<ResultOfTestBatch>(batchResultFileName);
            var summary = new List<string>() {" API,Role,TotalCount,SUCCESS,FAIL,1stTime(s),MinTime(s),MaxTime(s),AvgTime(s)"};
            var distinctApisTested = by == "NAME"
                ? batchResult.TestResults.Select(r => r.Name).Distinct().OrderBy(name => name).ToList()
                : batchResult.TestResults.OrderBy(r => r.Name).ThenBy(r => r.RestUri.Length).ThenBy(r => r.RestUri).Select(r => r.RestUri).Distinct().ToList();
            Parallel.ForEach(Enumerable.Range(0, distinctApisTested.Count()),
                idx =>
                {
                    var apiTested = distinctApisTested[idx];
                    var apiSummary = new List<string>();
                    var apiResults = batchResult.TestResults.Where(r => by == "NAME" ? r.Name == apiTested : r.RestUri == apiTested);
                    var userPrefixes = apiResults.Select(
                        r =>  r.RunAsUserName == null ? "null" :
                            r.RunAsUserName.StartsWith("test", StringComparison.InvariantCultureIgnoreCase) 
                            ? r.RunAsUserName.Substring(0, r.RunAsUserName.Length - 1) 
                            : r.RunAsUserName).Distinct().OrderBy(userName => userName);
                    foreach (var userPrefix in userPrefixes)
                    {
                        var role = userPrefix;
                        if (role.StartsWith("test", StringComparison.InvariantCultureIgnoreCase))
                        {
                            role = role.Substring(4);
                        }

                        if (role.Length > 4 && userPrefix.EndsWith("user", StringComparison.InvariantCultureIgnoreCase))
                        {
                            role = role.Substring(0, role.Length - 4);
                        }

                        double firstTime = 0, average = 0, minTime = 0, maxTime = 0;
                        int totalCount = 0, successCount = 0, failCount = 0;
                        var userResults = apiResults.Where(r => (r.RunAsUserName == null && userPrefix == "null") || r.RunAsUserName.StartsWith(userPrefix));
                        var okResults = userResults.Where(t => t.IterationsExecutedSuccessfully > 0);
                        var nonOkResults = userResults.Except(okResults);

                        failCount = nonOkResults.Count();
                        totalCount = userResults.Count();                        
                        successCount = okResults.Count();

                        if (okResults.Any())
                        {
                            firstTime = okResults.OrderBy(t => t.StartTime).First().AverageExecutionTime.TotalSeconds;
                            average = okResults.Average(t => t.AverageExecutionTime.TotalSeconds);
                            minTime = okResults.Min(t => t.AverageExecutionTime.TotalSeconds);
                            maxTime = okResults.Max(t => t.AverageExecutionTime.TotalSeconds);
                        }

                        apiTested = string.Format("{0:000} {1}", idx, apiTested.Split('/').Last());
                        apiSummary.Add(string.Format(
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                            apiTested,
                            role,
                            totalCount,                                                        
                            successCount,
                            failCount,
                            firstTime,
                            minTime,
                            maxTime,
                            average));
                    }
                    try
                    {
                        summary.AddRange(apiSummary);
                    }catch (System.ArgumentException ex)
                    {
                        summary.AddRange(apiSummary);
                    }
                });

            summary.Sort();
            File.WriteAllLines(Path.GetFullPath(batchResultFileName) + "Summary.csv", summary);
            Helper.SaveToFile(errorResults, Path.GetFullPath(batchResultFileName) + "ErrorResults.xml");        
        }

        /// <summary> Creates a sample Batch File. </summary>
        /// <param name="batchFile"> Name of the file where sample batch is to be saved. </param>
        public static void SampleBatchFile(string batchFile)
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
        public static void CreateSampleBatchRunResultFile(string batchRunResultFile)
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
