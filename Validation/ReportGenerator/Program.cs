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

namespace Microsoft.GFS.WCS.Test.ReportGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Parse client call xml for 03 - https - 10 threads - 1 hour
            string clientCallXml = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\https___DVTCM03_8000-TrialPerfRunBatch.Results.xml";
            int testDurationInSec = 3600;
            string targetFolder = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\";
            ClientReport clientReport = new ClientReport();
            clientReport.GenerateClientReport(clientCallXml, testDurationInSec, targetFolder, "03 - https - 10 threads - 1 hour - ");

            //Parse client call xml for 03 - https - 1 thread - 1 hour
            clientCallXml = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\https___DVTCM03_8000-TrialPerfRunBatch.Results.xml";
            testDurationInSec = 3600;
            targetFolder = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\";
            clientReport = new ClientReport();
            clientReport.GenerateClientReport(clientCallXml, testDurationInSec, targetFolder, "03 - https - 1 thread - 1 hour - ");

            //string fileName2 = "ImagingTestInProduction - " + "test" + ".txt";
            //string logFilePath2 = @"C:\Temp\" + fileName2;
            //CreateLogFile(logFilePath2);
            //StreamWriter sw2 = new StreamWriter(logFilePath2);
            CreateReport createReport = new CreateReport();
            //ch.ExceptionTest();

            //Parse perflog file to generate latency data for 03 - https - 10 threads - 1 hour
            string dataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\LatencyPerfCounters.xml";
            string perfLogFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\Perflogs\Server\DVTCM03_20121120-000002\Performance Counter - Copy.csv";
            string latencyResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\03 - https - 10 threads - 1 hour - LatencyData.html";
            string tableTitle = "WCF Service APIs - Server End - Latency (Sec)";
            createReport.GetPerfNumbersTableHtml(dataFile, perfLogFilePath, tableTitle, latencyResultFilePath);

            //Parse perflog file to generate throughput data for 03 - https - 10 threads - 1 hour
            dataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\ThroughputPerfCounters.xml";
            tableTitle = "WCF Service APIs - Server End - Throughput (Calls / sec)";
            string throughputResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\03 - https - 10 threads - 1 hour - ThroughputCounters.html";
            createReport.GetPerfNumbersTableHtml(dataFile, perfLogFilePath, tableTitle, throughputResultFilePath);

            //Parse perflog file to generate Calls data for 03 - https - 10 threads - 1 hour            
            string callsDataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\TotalCallsPerfCounters.xml";
            string failedCallsDataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\FailedCallsPerfCounters.xml";
            tableTitle = "WCF Service APIs - Server End - Calls";
            string callsResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 10 threads - 1 hour\03 - https - 10 threads - 1 hour - CallsData.html";
            createReport.GetCallsAndFailedCallsTableHtml(callsDataFile, failedCallsDataFile, perfLogFilePath, tableTitle, callsResultFilePath);

            //Parse perflog file to generate latency data for 03 - https - 1 thread - 1 hour
            dataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\LatencyPerfCounters.xml";
            perfLogFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\PerfLogs\Server\DVTCM03_20121120-000003\Performance Counter.csv";
            latencyResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\03 - https - 1 thread - 1 hour - LatencyData.html";
            tableTitle = "WCF Service APIs - Server End - Latency (Sec)";
            createReport.GetPerfNumbersTableHtml(dataFile, perfLogFilePath, tableTitle, latencyResultFilePath);

            //Parse perflog file to generate throughput data for 03 - https - 1 thread - 1 hour
            dataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\ThroughputPerfCounters.xml";
            tableTitle = "WCF Service APIs - Server End - Throughput (Calls / sec)";
            throughputResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\03 - https - 1 thread - 1 hour - ThroughputCounters.html";
            createReport.GetPerfNumbersTableHtml(dataFile, perfLogFilePath, tableTitle, throughputResultFilePath);

            //Parse perflog file to generate Calls data for 03 - https - 1 thread - 1 hour            
            callsDataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\TotalCallsPerfCounters.xml";
            failedCallsDataFile = @"C:\Users\sbijay\Documents\Visual Studio 2012\Projects\WCS\Mt Rainier\Manageability\Developement\ReportGenerator\FailedCallsPerfCounters.xml";
            tableTitle = "WCF Service APIs - Server End - Calls";
            callsResultFilePath = @"C:\Users\sbijay\Desktop\WCS Perf\Perf run results\03 - https - 1 thread - 1 hour\03 - https - 1 thread - 1 hour - CallsData.html";
            createReport.GetCallsAndFailedCallsTableHtml(callsDataFile, failedCallsDataFile, perfLogFilePath, tableTitle, callsResultFilePath);
        }
    }
}
