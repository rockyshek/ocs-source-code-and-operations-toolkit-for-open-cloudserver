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
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;

namespace Microsoft.GFS.WCS.Test.ReportGenerator
{
    public struct ClientAPIPerfNumbers
    {
        public string ApiName { get; set; }

        public double MinLatency { get; set; }

        public double AvgLatency { get; set; }

        public double MaxLatency { get; set; }

        public int TotalCalls { get; set; }

        public double Throughput { get; set; }
    }

    public struct ClientAPIPercentageFailure
    {
        public string ApiName { get; set; }

        public int TotalCalls { get; set; }

        public int TotalFailedCalls { get; set; }

        public double PercentageFailure { get; set; }
    }

    public struct APIErrorDetails
    {
        public string ApiName { get; set; }

        public Collection<ErrorDetails> ErrorDetails { get; set; }
    }

    public struct ErrorDetails
    {
        public string ErrorMessage { get; set; }

        public string ErrorTime { get; set; }
    }

    public class ClientReport
    {
        public void GenerateClientReport(
            string clientCallXml,
            int testDurationInSec,
            string targetFolder,
            string targetFileNamePrefix)
        {
            if (targetFileNamePrefix == null)
            {
                targetFileNamePrefix = string.Empty;
            }

            IEnumerable<ResultOfTest> clientCallsDataCollection =
                this.GetTestResultsCollection(clientCallXml);

            var results = clientCallsDataCollection.GroupBy(clientCallsData => clientCallsData.ApiName);

            Collection<ClientAPIPerfNumbers> clientApiLatencyData =
                new Collection<ClientAPIPerfNumbers>();

            Collection<ClientAPIPercentageFailure> clientApiPercentageFailureData =
                new Collection<ClientAPIPercentageFailure>();

            Collection<APIErrorDetails> clientErrorDetails =
                new Collection<APIErrorDetails>();

            foreach (var apiDetails in results)
            {
                clientApiLatencyData.Add(this.GetClientApiLatencyData(apiDetails, testDurationInSec));
                clientApiPercentageFailureData.Add(this.GetClientApiFailureData(apiDetails));
                clientErrorDetails.Add(this.GetErrorDetails(apiDetails));
            }

            DataTable resultTable = this.GetClientLatencyTable(clientApiLatencyData);
            CreateReport.GetResultsTableHtml(
                Path.Combine(targetFolder, string.Format("{0}ClientLatency.html", targetFileNamePrefix)),
                "WCF Service APIs - Client side - Latency (Sec)",
                resultTable);

            resultTable = this.GetClientThroughputTable(clientApiLatencyData);
            CreateReport.GetResultsTableHtml(
                Path.Combine(targetFolder, string.Format("{0}ClientThroughput.html", targetFileNamePrefix)),
                "WCF Service APIs - Client side - Throughput (Calls per sec)",
                resultTable);

            resultTable = this.GetClientCallsFailureRateTable(clientApiPercentageFailureData);
            CreateReport.GetResultsTableHtml(
                Path.Combine(targetFolder, string.Format("{0}ClientCalls.html", targetFileNamePrefix)),
                "WCF Service APIs - Client side - Calls",
                resultTable);

            resultTable = this.GetClientErrorTable(clientErrorDetails);
            CreateReport.GetResultsTableHtml(
                Path.Combine(targetFolder, string.Format("{0}ClientError.html", targetFileNamePrefix)),
                "WCF Service APIs - Client side - Error Details",
                resultTable);
        }

        private IEnumerable<ResultOfTest> GetTestResultsCollection(string clientCallXml)
        {
            string text = System.IO.File.ReadAllText(clientCallXml);
            return ClientCallsXmlParser.ParseXml(text);
        }

        private ClientAPIPerfNumbers GetClientApiLatencyData(IEnumerable<ResultOfTest> apiDetails, int testDurationInSec)
        {
            double sum = 0.0;
            int count = 0;
            ClientAPIPerfNumbers clientApiPerfNumbers = new ClientAPIPerfNumbers();

            foreach (ResultOfTest apiDetail in apiDetails)
            {
                count++;
                clientApiPerfNumbers.ApiName = apiDetail.ApiName;

                if ((apiDetail.TotalExecutionTime != String.Empty) &&
                    (apiDetail.TotalExecutionTime != null) &&
                    (apiDetail.TotalExecutionTime != " "))
                {
                    double value = Double.Parse(apiDetail.TotalExecutionTime);
                    if (value > 0)
                    {
                        sum += value;
                    }

                    if (value > clientApiPerfNumbers.MaxLatency)
                    {
                        clientApiPerfNumbers.MaxLatency = value;
                    }

                    if (value < clientApiPerfNumbers.MinLatency)
                    {
                        clientApiPerfNumbers.MinLatency = value;
                    }
                }
            }

            clientApiPerfNumbers.AvgLatency = sum / count;

            if (double.IsNaN(clientApiPerfNumbers.AvgLatency))
            {
                clientApiPerfNumbers.AvgLatency = 0.0;
            }

            clientApiPerfNumbers.MinLatency = Math.Round(clientApiPerfNumbers.MinLatency, 2);
            clientApiPerfNumbers.AvgLatency = Math.Round(clientApiPerfNumbers.AvgLatency, 2);
            clientApiPerfNumbers.MaxLatency = Math.Round(clientApiPerfNumbers.MaxLatency, 2);
            clientApiPerfNumbers.TotalCalls = count;
            clientApiPerfNumbers.Throughput = Math.Round((Convert.ToDouble(count) / Convert.ToDouble(testDurationInSec)), 2);
            return clientApiPerfNumbers;
        }

        private ClientAPIPercentageFailure GetClientApiFailureData(
            IEnumerable<ResultOfTest> apiDetails)
        {
            int count = 0;
            ClientAPIPercentageFailure clientApiPercentageFailure =
                new ClientAPIPercentageFailure();

            foreach (ResultOfTest apiDetail in apiDetails)
            {
                count++;
                clientApiPercentageFailure.ApiName = apiDetail.ApiName;
                if (apiDetail.State.Contains("Failed"))
                {
                    clientApiPercentageFailure.TotalFailedCalls++;
                }
            }

            clientApiPercentageFailure.TotalCalls = count;
            clientApiPercentageFailure.PercentageFailure =
                Math.Round((Convert.ToDouble(clientApiPercentageFailure.TotalFailedCalls) /
                            Convert.ToDouble(clientApiPercentageFailure.TotalCalls)) * 100, 2);

            return clientApiPercentageFailure;
        }

        private APIErrorDetails GetErrorDetails(IEnumerable<ResultOfTest> apiDetails)
        {
            APIErrorDetails clientApiErrorDetails =
                new APIErrorDetails();

            clientApiErrorDetails.ErrorDetails = new Collection<ErrorDetails>();
            foreach (ResultOfTest apiDetail in apiDetails)
            {
                clientApiErrorDetails.ApiName = apiDetail.ApiName;
                if (apiDetail.State.Contains("Failed"))
                {
                    ErrorDetails errorDetails = new ErrorDetails();
                    errorDetails.ErrorMessage = apiDetail.ErrorMessage;
                    errorDetails.ErrorTime = apiDetail.StartTime;
                    clientApiErrorDetails.ErrorDetails.Add(errorDetails);
                }
            }

            return clientApiErrorDetails;
        }

        private DataTable GetClientLatencyTable(Collection<ClientAPIPerfNumbers> clientApiLatencyData)
        {
            DataTable clientLatencyResultsDataTable = this.CreateClientLatencyResultsTable();
            return this.AddClientLatencyDataToTable(clientLatencyResultsDataTable, clientApiLatencyData);
        }

        private DataTable CreateClientLatencyResultsTable()
        {
            DataTable clientLatencyResultsDataTable = new DataTable();
            clientLatencyResultsDataTable.Columns.Add("APIName", typeof(string));
            clientLatencyResultsDataTable.Columns.Add("Min", typeof(double));
            clientLatencyResultsDataTable.Columns.Add("Avg", typeof(double));
            clientLatencyResultsDataTable.Columns.Add("Max", typeof(double));
            return clientLatencyResultsDataTable;
        }

        private DataTable AddClientLatencyDataToTable(
            DataTable clientLatencyResultsDataTable,
            Collection<ClientAPIPerfNumbers> clientApiLatencyData)
        {
            foreach (ClientAPIPerfNumbers clientApiPerfNumber in clientApiLatencyData)
            {
                clientLatencyResultsDataTable.Rows.Add(
                    clientApiPerfNumber.ApiName,
                    clientApiPerfNumber.MinLatency,
                    clientApiPerfNumber.AvgLatency,
                    clientApiPerfNumber.MaxLatency);
            }

            return clientLatencyResultsDataTable;
        }
        
        private DataTable GetClientThroughputTable(Collection<ClientAPIPerfNumbers> clientApiLatencyData)
        {
            DataTable clientLatencyResultsDataTable = this.CreateClientThroughputResultsTable();
            return this.AddClientThroughputDataToTable(clientLatencyResultsDataTable, clientApiLatencyData);
        }

        private DataTable CreateClientThroughputResultsTable()
        {
            DataTable clientThroughputResultsDataTable = new DataTable();
            clientThroughputResultsDataTable.Columns.Add("APIName", typeof(string));
            clientThroughputResultsDataTable.Columns.Add("Throughput", typeof(double));
            return clientThroughputResultsDataTable;
        }

        private DataTable AddClientThroughputDataToTable(
            DataTable clientThroughputResultsDataTable,
            Collection<ClientAPIPerfNumbers> clientApiThroughputyData)
        {
            foreach (ClientAPIPerfNumbers clientApiPerfNumber in clientApiThroughputyData)
            {
                clientThroughputResultsDataTable.Rows.Add(
                    clientApiPerfNumber.ApiName,
                    clientApiPerfNumber.Throughput);
            }

            return clientThroughputResultsDataTable;
        }
        
        private DataTable GetClientCallsFailureRateTable(
            Collection<ClientAPIPercentageFailure> clientApiPercentageFailureData)
        {
            DataTable clientApiPercentageFailureResultsDataTable = this.CreateClientApiPercentageResultsTable();
            return this.AddClientApiPercentageFailureDataToTable(clientApiPercentageFailureResultsDataTable, clientApiPercentageFailureData);
        }

        private DataTable CreateClientApiPercentageResultsTable()
        {
            DataTable clientApiPercentageFailureResultsDataTable = new DataTable();
            clientApiPercentageFailureResultsDataTable.Columns.Add("APIName", typeof(string));
            clientApiPercentageFailureResultsDataTable.Columns.Add("Total Calls", typeof(int));
            clientApiPercentageFailureResultsDataTable.Columns.Add("Total Failed Calls", typeof(int));
            clientApiPercentageFailureResultsDataTable.Columns.Add("%Failure", typeof(double));
            return clientApiPercentageFailureResultsDataTable;
        }

        private DataTable AddClientApiPercentageFailureDataToTable(
            DataTable clientApiPercentageFailureResultsDataTable,
            Collection<ClientAPIPercentageFailure> clientApiPercentageFailureData)
        {
            foreach (ClientAPIPercentageFailure clientApiPercentageFailure in clientApiPercentageFailureData)
            {
                clientApiPercentageFailureResultsDataTable.Rows.Add(
                    clientApiPercentageFailure.ApiName,
                    clientApiPercentageFailure.TotalCalls,
                    clientApiPercentageFailure.TotalFailedCalls,
                    clientApiPercentageFailure.PercentageFailure);
            }

            return clientApiPercentageFailureResultsDataTable;
        }

        private DataTable GetClientErrorTable(Collection<APIErrorDetails> clientErrorDetails)
        {
            DataTable clientAPIerrorResultsDataTable = this.CreateClientApiErrorResultsTable();
            return this.AddClientApiErrorDataToTable(clientAPIerrorResultsDataTable, clientErrorDetails);
        }

        private DataTable CreateClientApiErrorResultsTable()
        {
            DataTable clientApiErrorResultsDataTable = new DataTable();
            clientApiErrorResultsDataTable.Columns.Add("APIName", typeof(string));
            clientApiErrorResultsDataTable.Columns.Add("API Call Time", typeof(string));
            clientApiErrorResultsDataTable.Columns.Add("Error Message", typeof(string));
            return clientApiErrorResultsDataTable;
        }

        private DataTable AddClientApiErrorDataToTable(
            DataTable clientApiErrorResultsDataTable,
            Collection<APIErrorDetails> clientApiErrorData)
        {
            foreach (APIErrorDetails clientApiError in clientApiErrorData)
            {
                foreach (ErrorDetails errorDetails in clientApiError.ErrorDetails)
                {
                    clientApiErrorResultsDataTable.Rows.Add(
                        clientApiError.ApiName,
                        errorDetails.ErrorTime,
                        errorDetails.ErrorMessage);
                }
            }

            return clientApiErrorResultsDataTable;
        }
    }
}
