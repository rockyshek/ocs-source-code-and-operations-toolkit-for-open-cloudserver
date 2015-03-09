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
    using System.Net;
    using System.Security;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.GFS.WCS.Test;
    using Microsoft.GFS.WCS.Test.Consoler;
    using Microsoft.GFS.WCS.Test.Framework;

    /// <summary> Commands for manipulating Framework. </summary>
    [ConsoleSuite]
    public static class Reporting
    {
        [Action("Replaces all shortened names of counters with full API names.")]
        public static void FixCounterNames(
            [Required(Description = "Input File Name")] string inFile,
            [Required(Description = "Output File Name")] string outFile)
        {
            var map = Helper.LoadFromFile<Dictionary<string, string>>("CounterToApiMap.xml");
            var contents = File.ReadLines(inFile).ToList();
            if (contents.Any())
            {
                var header = contents[0];
                header = header.Replace("Chass19.IChas09.", string.Empty);
                header = header.Replace("Chass19.IChassisManager.", string.Empty);
                header = header.Replace("HTTPS:||LOCALHOST:8000|", "");

                foreach(var mapping in map)
                {
                    header = header.Replace(mapping.Key, mapping.Value);
                }

                contents[0] = header;
                File.WriteAllLines(outFile, contents);
                Console.WriteLine(string.Format("Output file {0} created with fixed counter names", outFile));
            }
        }

        [Action("Creates a Report From BatchResults XML file.")]
        public static void CreateSummaryReportFromBatchResults(
            [Required(Description = "BatchFile")] string batchResultFileName,
            [Optional("Name", Description = "Name of Batch File. Default=SampleBatch.xml")] string by)
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
    }
}
