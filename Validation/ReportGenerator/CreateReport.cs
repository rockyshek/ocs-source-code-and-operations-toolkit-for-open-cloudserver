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
using System.Text;

namespace Microsoft.GFS.WCS.Test.ReportGenerator
{
    public struct PerfCounterData
    {
        public string CounterName { get; set; }

        public string FriendlyName { get; set; }

        public double MinValue { get; set; }

        public double AvgValue { get; set; }

        public double MaxValue { get; set; }
    }

    public struct CallsData
    {
        public string CounterName { get; set; }

        public string FriendlyName { get; set; }

        public double TotalCalls { get; set; }

        public double TotalFailedCalls { get; set; }

        public double PercentFailure { get; set; }
    }

    internal class CreateReport
    {
        //private ServiceContext sc = null;
        //private const string StatusListName = "ImagingStatus";
        //private readonly string scope = string.Empty;
        //private readonly StreamWriter sw = null;

        internal CreateReport()
        {
            //this.scope = ConfigurationManager.AppSettings["Scope"];
            //this.sc = new ServiceContext(this.scope);
            //this.sw = writer;
        }

        //internal string SubmitRequest(string computerName, string buildType, string skuId, string os, int requestCount, out string computerUri)
        //{
        //    string result = "Passed";
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    try
        //    {
        //        SCCMBuildData sccmBuildData = this.GetWMIData(computerName, buildType, skuId, os);
        //        computerUri = sccmBuildData.ComputerUri.AbsoluteUri;
        //        Ticket ticket = this.CreateTicket(sccmBuildData);
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine("Request Start time: " + System.DateTime.Now);
        //        this.sw.WriteLine(string.Format("*************************************Submitting new request. Request {0} details...*************************************", requestCount));
        //        this.sw.WriteLine("Ticket Id: " + ticket.Header.Uri.ToString());
        //        this.sw.WriteLine("Test client name: " + sccmBuildData.ComputerName);
        //        this.sw.WriteLine("BuildType used for testing: " + buildType);
        //        this.sw.WriteLine("SKUId used for testing: " + skuId);
        //        this.sw.WriteLine("ImageName used for testing: " + os);
        //        DateTime endTime = System.DateTime.Now.AddMinutes(100);
        //        string status = string.Empty;
        //        string statusDetails = string.Empty;
        //        do
        //        {
        //            status = this.GetImagingUpdate(
        //                        ticket,
        //                        sccmBuildData.ComputerUri.AbsoluteUri,
        //                        this.scope,
        //                        out statusDetails);
        //            ////Added temp only for trial run to get log details for automating E2E cases + Retry / Cancel cases
        //            this.sw.WriteLine(Environment.NewLine);
        //            this.sw.WriteLine("***********************************************Getting status******************************************");
        //            this.sw.WriteLine(status);
        //            this.sw.WriteLine(Environment.NewLine);
        //            ////Added temp only for trial run to get log details for automating E2E cases + Retry / Cancel cases
        //            if (status.Contains(",Failed,"))
        //            {
        //                this.sw.WriteLine("Request failed when it was expected to pass");
        //                result = "Failed";
        //                break;
        //            }
        //            if (status.Contains(",Succeeded,"))
        //            {
        //                this.sw.WriteLine("Request succeeded!!");
        //                result = "Passed";
        //                break;
        //            }
        //            Thread.Sleep(60000);
        //        } while (System.DateTime.Now <= endTime);
        //        this.sw.WriteLine("Imaging request result: " + result);
        //        this.sw.WriteLine("Request End time: " + System.DateTime.Now);
        //        this.sw.WriteLine(string.Format("*************************************End of request {0}*************************************", requestCount));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //        computerUri = null;
        //    }
        //    return result;
        //}
        //private SCCMBuildData GetWMIData(string computerName, string buildType, string skuId, string os)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    SCCMBuildData sccmBuildData = null;
        //    try
        //    {
        //        ImagingLookups lookups = new ImagingLookups(this.sc);
        //        var result = lookups.BeginGetComputerInfoViaWMI("Imaging/WMI/" + computerName, null, null);
        //        result.AsyncWaitHandle.WaitOne();
        //        ComputerData computerData = (ComputerData)lookups.EndGetComputerInfoViaWMI(result);
        //        sccmBuildData = new SCCMBuildData();
        //        sccmBuildData.Cdroms = computerData.Cdroms;
        //        sccmBuildData.ComputerDataSource = SCCMBuildDataSourceEnum.MFx;
        //        sccmBuildData.ComputerName = computerData.ComputerName;
        //        //sccmBuildData.AdditionalIPakList = "None";
        //        //sccmBuildData.DataCenter = 
        //        sccmBuildData.Domain = computerData.Domain;
        //        sccmBuildData.DomainOU = computerData.DomainOU;
        //        sccmBuildData.ExtensionData = computerData.ExtensionData;
        //        sccmBuildData.ExtensionPropertyList = computerData.ExtensionPropertyList;
        //        sccmBuildData.HardwareModel = computerData.HardwareModel;
        //        sccmBuildData.IloIpAddress = computerData.IloIpAddress;
        //        sccmBuildData.IsVirtualMachine = computerData.IsVirtualMachine;
        //        sccmBuildData.LocalAdministrators = computerData.LocalAdministrators;
        //        sccmBuildData.Nics = computerData.Nics;
        //        sccmBuildData.OldComputerName = computerData.ComputerName;
        //        sccmBuildData.OperatingSystem = computerData.OperatingSystem;
        //        sccmBuildData.Volumes = computerData.Volumes;
        //        //sccmBuildData.SerialNumber = 
        //        sccmBuildData.StartTSFrom = "R--Begin Imaging";
        //        sccmBuildData.ComputerUri = this.GetComputerUri(sccmBuildData.ComputerName);
        //        sccmBuildData.BuildType = (BuildTypeEnum)Enum.Parse(typeof(BuildTypeEnum), buildType);
        //        sccmBuildData.SKUId = skuId;
        //        sccmBuildData.ImageName = os;
        //        //sccmBuildData.AdditionalIPakList = "None";
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return sccmBuildData;
        //}
        //private Uri GetComputerUri(string computerName)
        //{
        //    Uri computerUri = null;
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    try
        //    {
        //        this.sc.ConnectionMode = ServiceMode.ThroughGateway;
        //        Dictionary<string, string> filter = new Dictionary<string, string>();
        //        filter.Add("Name", computerName);
        //        MFxItemList list = ConfigItem.Find(this.sc, "Computer", filter);
        //        Computer computer = (Computer)list[0];
        //        computerUri = computer.Uri;
        //    }
        //    catch (MFxClientLibraryException ex)
        //    {
        //        if (ex.ErrorList != null)
        //        {
        //            foreach (MFxError mfexerr in ex.ErrorList)
        //            {
        //                Console.WriteLine(ex.Message);
        //            }
        //        }
        //        computerUri = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return computerUri;
        //}
        //private Ticket CreateTicket(SCCMBuildData sccmbuildData)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    ServiceContext serviceContext = new ServiceContext(this.scope);
        //    MFxItemList ticketTemplates = TicketTemplate.Find(serviceContext, new Dictionary<string, string>());
        //    Console.WriteLine(ticketTemplates.Where(template => template.Name == "Imaging").FirstOrDefault().Name);
        //    Ticket ticket = null;
        //    try
        //    {
        //        Console.WriteLine("ticket creation started ...");
        //        string computerName = sccmbuildData.ComputerName;
        //        string buildType = sccmbuildData.BuildType.ToString();
        //        string ticketTitle = "Test In Production: " + sccmbuildData.ComputerName + " " + buildType;
        //        ticket = new Ticket
        //        {
        //            Name = Guid.NewGuid().ToString(),
        //            Header = new TicketHeader
        //            {
        //                Created = DateTime.UtcNow,
        //                Modified = DateTime.UtcNow,
        //                CreatedBy = "Imaging Test Service",
        //                TemplateRef = ticketTemplates.Where(template => template.Name == "Imaging").FirstOrDefault(),
        //                Status = MFx.ClientLibrary.Ticketing.Contracts.TicketStatus.Active,
        //                TicketPriority = MFx.ClientLibrary.Ticketing.Contracts.Priority.Medium,
        //                TicketSeverity = MFx.ClientLibrary.Ticketing.Contracts.Severity.Sev2,
        //                Title = ticketTitle,
        //                Description = ticketTitle,
        //            },
        //        };
        //        ticket.ListElements = new Collection<ListElement>();
        //        if (sccmbuildData == null)
        //        {
        //            Console.WriteLine("SCCMBuildDataCollection is null");
        //        }
        //        ListElement imgListElement = new ListElement
        //        {
        //            ListName = "ServerCollection",
        //            DeletedFlag = false,
        //            Value = SerializeToJsonString(sccmbuildData),
        //            ElementType = sccmbuildData.GetType().ToString()
        //        };
        //        ticket.ListElements.Add(imgListElement);
        //        ImagingStatus imagingStatus = new ImagingStatus();
        //        imagingStatus.StatusCode = ImagingStatusEnum.NewImaging;
        //        ListElement statListElement = new ListElement
        //        {
        //            ListName = "ImagingStatus",
        //            Label = sccmbuildData.ComputerUri.ToString(),
        //            Value = SerializeToJsonString(imagingStatus),
        //            ElementType = imagingStatus.GetType().ToString()
        //        };
        //        ticket.ListElements.Add(statListElement);
        //        if (ticket.Properties == null)
        //        {
        //            ticket.Properties = new Dictionary<string, TicketingProperty>();
        //        }
        //        ticket.Properties.Add("Intent", new TicketingProperty() { Value = "Imaging" });
        //        ticket.Properties.Add("SelectedBuildMode", new TicketingProperty() { Value = buildType });
        //        ticket.LinkedItemLists = new Dictionary<string, Collection<LinkedItem>>();
        //        ticket.LinkedItemLists["Computers"] = new Collection<LinkedItem>();
        //        ticket = ticket.Create(serviceContext);
        //        Console.WriteLine("ticket created with URI..." + ticket.Uri.AbsoluteUri);
        //    }
        //    catch (MFxClientLibraryException ex)
        //    {
        //        Console.WriteLine("ticket created failed with exception" + ex.ErrorList[0].Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return ticket;
        //}

        //private string SerializeToJsonString(object objectToSerialize)
        //{
        //    if (objectToSerialize != null)
        //    {
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            var serializer = new DataContractSerializer(objectToSerialize.GetType());
        //            serializer.WriteObject(ms, objectToSerialize);
        //            ms.Position = 0;
        //            using (StreamReader reader = new StreamReader(ms))
        //            {
        //                var serializedObject = reader.ReadToEnd();
        //                return serializedObject;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        return string.Empty;
        //    }
        //}
        //private string GetImagingUpdate(MFx.ClientLibrary.Ticketing.Ticket ticket, string computerURI, string scope, out string statusDetails)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    StringBuilder statusCodeBuilder = new StringBuilder();
        //    StringBuilder statusDetailsBuilder = new StringBuilder();
        //    try
        //    {
        //        Console.WriteLine("Getting imaging status from ticket with uri: " + ticket.Uri.AbsoluteUri);
        //        ticket = MFx.ClientLibrary.MFxReference.Read(this.sc, ticket.Uri) as Ticket;
        //        Console.WriteLine("Printing ticket ListElement count: " + ticket.ListElements.Count);
        //        IEnumerable<ListElement> statuses = ticket.ListElements.Where(l => !l.DeletedFlag &&
        //        string.Equals(l.ListName, StatusListName, StringComparison.OrdinalIgnoreCase) &&
        //        l.Label.StartsWith(computerURI, StringComparison.OrdinalIgnoreCase));
        //        statusCodeBuilder.Append(",");
        //        statusDetailsBuilder.Append(",");
        //        foreach (ListElement statusListElement in statuses)
        //        {
        //            if (statusListElement.Value.Contains("<ImagingStatus"))
        //            {
        //                statusCodeBuilder.Append(this.DeserializeFromString<ImagingStatus>(statusListElement.Value).StatusCode.ToString());
        //                statusDetailsBuilder.Append(this.DeserializeFromString<ImagingStatus>(statusListElement.Value).StatusDetails.ToString());
        //                if (statusCodeBuilder.Length > 0)
        //                    statusCodeBuilder.Append(",");
        //                if (statusDetailsBuilder.Length > 0)
        //                    statusDetailsBuilder.Append(",");
        //            }
        //        }
        //        Console.WriteLine(Environment.NewLine);
        //        Console.WriteLine("************************Status code************************");
        //        Console.WriteLine(string.Format("Imaging Status code for Computer URI :{0} are  {1}", computerURI, statusCodeBuilder.ToString()));
        //        Console.WriteLine("***********************************************************");
        //        Console.WriteLine(Environment.NewLine);
        //        Console.WriteLine("************************Status code details************************");
        //        Console.WriteLine(string.Format("Imaging Status details for Computer URI :{0} are  {1}", computerURI, statusDetailsBuilder.ToString()));
        //        Console.WriteLine(Environment.NewLine);
        //        Console.WriteLine("*******************************************************************");
        //        statusDetails = statusDetailsBuilder.ToString();
        //        return statusCodeBuilder.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //        statusDetails = string.Empty;
        //        return null;
        //    }
        //}
        //private T DeserializeFromString<T>(string jsonString)
        //{
        //    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
        //    {
        //        var serializer = new DataContractSerializer(typeof(T));
        //        return (T)serializer.ReadObject(ms);
        //    }
        //}
        //public IEnumerable<string> GetRetySteps(string buildType)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    IEnumerable<string> retrySteps = null;
        //    try
        //    {
        //        ImagingLookups lookups = new ImagingLookups(this.sc);
        //        var result = lookups.BeginGetRetrySteps(buildType, null, null);
        //        result.AsyncWaitHandle.WaitOne();
        //        retrySteps = (IEnumerable<string>)lookups.EndGetRetrySteps(result);
        //        foreach (string retryStep in retrySteps)
        //        {
        //            Console.WriteLine(retryStep);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return retrySteps;
        //}
        //public ImagingRequestResponse CancelRequest(string computerUri)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    ImagingRequestResponse imagingRequestResponse = null;
        //    try
        //    {
        //        ImagingLookups lookups = new ImagingLookups(this.sc);
        //        var result = lookups.BeginCancelImagingRequest(computerUri, null, null);
        //        result.AsyncWaitHandle.WaitOne();
        //        imagingRequestResponse = (ImagingRequestResponse)lookups.EndCancelImagingRequest(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return imagingRequestResponse;
        //}
        //internal string GetDomain(string computerName)
        //{
        //    string query = "Win32_NTDomain";
        //    string domainPropertyName = "DomainName";
        //    return this.GetComputerDetails(computerName, query, domainPropertyName);
        //    //return this.GetClientDomain(computerName);
        //}
        //private string GetComputerDetails(string computerName, string query, string propertyName)
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    string computerData = string.Empty;
        //    try
        //    {
        //        string scope = string.Format(@"\\{0}\root\cimv2", computerName);
        //        ConnectionOptions connOptions = new ConnectionOptions();
        //        connOptions.Impersonation = ImpersonationLevel.Impersonate;
        //        connOptions.EnablePrivileges = true;
        //        ManagementScope manScope = new ManagementScope
        //            (String.Format(@"\\{0}\ROOT\CIMV2", computerName), connOptions);
        //        manScope.Connect();
        //        ObjectGetOptions objectGetOptions = new ObjectGetOptions();
        //        ManagementPath managementPath = new ManagementPath(query);
        //        ManagementClass wmi = new ManagementClass
        //            (manScope, managementPath, objectGetOptions);
        //        ManagementObjectCollection allConfigs = wmi.GetInstances();
        //        foreach (ManagementObject configuration in allConfigs)
        //        {
        //            computerData = configuration[propertyName] == null ? string.Empty : configuration[propertyName].ToString();
        //            if (computerData.Length > 0)
        //                Console.WriteLine(string.Format("{0} Name for computer {1} is: {2}", propertyName, computerName, computerData)); //Microsoft Windows Server 2008 R2 Enterprise
        //        }
        //    }
        //    catch (System.UnauthorizedAccessException ex)
        //    {
        //        Console.WriteLine(string.Format("WMI call for query {0} failed with access denied exception with exception details {1}: ",
        //            query, ex.ToString()));
        //    }
        //    catch (Exception ex)
        //    {
        //        computerData = string.Empty;
        //        string message = string.Format("Call to {0} API failed with exception : {1}", methodName, ex.ToString());
        //        Console.WriteLine(message);
        //    }
        //    return computerData;
        //}
        //public DirectoryEntry IsComputerInADGroup()
        //{
        //    string ldapPath = @"LDAP://gme.gbl";
        //    //string ldapPath = @"LDAP://phx.gbl";
        //    DirectoryEntry dr = new DirectoryEntry(ldapPath, @"gme\sbijay", "PSkaEklavya345*");
        //    string searchString = string.Format("(&(objectClass={0}) ({1}={2}))", "computer", "name", "RRMSGIMGCLT02");
        //    var ds = new DirectorySearcher(dr, searchString);
        //    try
        //    {
        //        SearchResult result = ds.FindOne();
        //        if (result == null)
        //        {
        //            return null;
        //        }
        //        return result.GetDirectoryEntry();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(
        //            string.Format(
        //                "Query syntax is invalid with category: '{0}', property: '{1}', objectValue: '{2}'",
        //                "computer",
        //                "name",
        //                "RRMSGIMGCLT02"),
        //            ex);
        //    }
        //}
        //public IList<string> GetSupportedDomains()
        //{
        //    string methodName = System.Reflection.MethodInfo.GetCurrentMethod().Name;
        //    IList<string> supportedDomainList = null;
        //    try
        //    {
        //        ImagingLookups lookups = new ImagingLookups(this.sc);
        //        var result = lookups.BeginGetDomains(null, null);
        //        result.AsyncWaitHandle.WaitOne();
        //        supportedDomainList = (IList<string>)lookups.EndGetDomains(result).ToList();
        //        foreach (string domain in supportedDomainList)
        //        {
        //            Console.WriteLine(domain);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        this.sw.WriteLine(Environment.NewLine);
        //        this.sw.WriteLine(string.Format("Call to API {0} failed with exception: {1}", methodName, ex.ToString()));
        //        this.sw.WriteLine(Environment.NewLine);
        //    }
        //    return supportedDomainList;
        //}
        //public void ExceptionTest()
        //{
        //    try
        //    {
        //        Console.WriteLine("Simulating IO exception");
        //        throw new System.IO.IOException();
        //    }
        //    catch (System.IO.IOException ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //    finally
        //    {
        //        Console.WriteLine("Doing nothing in finally");
        //        Thread.Sleep(6000);
        //    }
        //}
        public PerfCounterData GetValueFromPerfLogFile(string perfLogFilePath, PerfCounterData perfCounterData)
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(perfLogFilePath);
                int index = 0;
                int numberOfLines = 0;
                while (!sr.EndOfStream)
                {
                    string[] sline = sr.ReadLine().Split(',');
                    if (sline != null)
                    {
                        for (int i = 0; i < sline.Length; i++)
                        {
                            string line = sline[i].ToLower();
                            if (line.Contains(perfCounterData.CounterName.ToLower()))
                            {
                                index = i;
                            }
                        }
                    }
                    numberOfLines++;
                }

                perfCounterData.AvgValue = this.GetCounterAvgValue(perfLogFilePath, numberOfLines, index);
                perfCounterData.MinValue = this.GetMinMaxValue(perfLogFilePath, numberOfLines, index, false);
                perfCounterData.MaxValue = this.GetMinMaxValue(perfLogFilePath, numberOfLines, index, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return perfCounterData;
        }

        //To be used for generating latency and throughput result html
        public void GetPerfNumbersTableHtml(string dataFile, string perfLogFilePath, string tableTitle, string resultFilePath)
        {
            Collection<PerfCounterData> perfCounterDataCollection =
                this.GetPerfCounterDataCollection(dataFile);

            perfCounterDataCollection = this.PopulatePerfDataFromPerfLogfile(
                perfCounterDataCollection,
                perfLogFilePath);

            DataTable resultTable = CreateReport.GetPerfRunResultsTable(perfCounterDataCollection);
            CreateReport.GetResultsTableHtml(resultFilePath, tableTitle, resultTable);
        }

        //To be used for generating %failed calls result html
        public void GetCallsAndFailedCallsTableHtml(
            string callsDataFile,
            string failedCallsDataFile,
            string perfLogFilePath,
            string tableTitle,
            string resultFilePath)
        {
            Collection<PerfCounterData> totalCallsPerfCounterDataCollection =
                this.GetPerfCounterDataCollection(callsDataFile);

            totalCallsPerfCounterDataCollection = this.PopulatePerfDataFromPerfLogfile(
                totalCallsPerfCounterDataCollection,
                perfLogFilePath);

            Collection<PerfCounterData> failedCallsPerfCounterDataCollection =
                this.GetPerfCounterDataCollection(failedCallsDataFile);

            failedCallsPerfCounterDataCollection = this.PopulatePerfDataFromPerfLogfile(
                failedCallsPerfCounterDataCollection,
                perfLogFilePath);

            Collection<CallsData> callsAndFailedCallsDataCollection =
                this.UnionCallsAndFailedCallsData(
                    totalCallsPerfCounterDataCollection,
                    failedCallsPerfCounterDataCollection);

            DataTable resultTable = this.GetCallsResultsTable(callsAndFailedCallsDataCollection);
            CreateReport.GetResultsTableHtml(resultFilePath, tableTitle, resultTable);
        }

        internal static void GetResultsTableHtml(string resultFilePath, string tableTitle, DataTable resultTable)
        {
            //Collection<PerfCounterData> perfCounterDataCollection =
            //    this.GetPerfCounterDataCollection(dataFile);
            //perfCounterDataCollection = this.PopulatePerfDataFromPerfLogfile(
            //    perfCounterDataCollection,
            //    perfLogFilePath);
            string htmlString = String.Empty;
            StringBuilder htmlBuilder = new StringBuilder();

            ////Create Top Portion of HTML Document
            htmlBuilder.Append("<html>");
            htmlBuilder.Append("<head>");
            htmlBuilder.Append("<style type=\"text/css\">");
            htmlBuilder.Append("a:hover");
            htmlBuilder.Append("{");
            htmlBuilder.Append("background-color:blue");
            htmlBuilder.Append("}");
            htmlBuilder.Append("</style>");
            htmlBuilder.Append("<title>");
            htmlBuilder.Append("Page-");
            htmlBuilder.Append(Guid.NewGuid().ToString());
            htmlBuilder.Append("</title>");
            htmlBuilder.Append("</head>");
            htmlBuilder.Append("<body>");

            ////add table heading            
            htmlBuilder.Append("<h3>");
            //htmlBuilder.Append("Server end Perf Counter Values");
            htmlBuilder.Append(tableTitle);
            htmlBuilder.Append("</h3>");

            htmlBuilder.Append("<table font-size: small>");

            ////Create Header Row
            htmlBuilder.Append("<thead>");
            htmlBuilder.Append("<tr align='left' valign='top' bgcolor=#488AC7>");

            //DataTable resultTable = this.GetPerfRunResultsTable(perfCounterDataCollection);            

            foreach (DataColumn column in resultTable.Columns)
            {
                htmlBuilder.Append("<th>");
                htmlBuilder.Append(column.ColumnName);
                htmlBuilder.Append("</th>");
            }

            //<th scope="col" id="...">...</th>  
            htmlBuilder.Append("</tr>");
            htmlBuilder.Append("</thead>");

            htmlBuilder.Append("<tfoot>");
            htmlBuilder.Append("<tr>");
            htmlBuilder.Append("<td>");
            htmlBuilder.Append("</td>");
            htmlBuilder.Append("</tr>");
            htmlBuilder.Append("</tfoot>");

            htmlBuilder.Append("<tbody>");

            //htmlBuilder.Append("</tr>");
            //htmlBuilder.Append("<tr border: solid 1px White>");
            //htmlBuilder.Append("</tr>");

            htmlBuilder = CreateReport.GetTableRowsHtml(resultTable, htmlBuilder);

            htmlBuilder = CreateReport.GetBottomPortionOfHtml(htmlBuilder);

            ////Create String to be Returned
            htmlString = htmlBuilder.ToString();

            CreateReport.SaveResultHtmlToDisk(resultFilePath, htmlString);
        }

        private static void SaveResultHtmlToDisk(string path, string resultHtmlString)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream fileStream = File.OpenWrite(path);
            StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(resultHtmlString);
            writer.Close();
        }

        private static DataTable GetPerfRunResultsTable(Collection<PerfCounterData> perfCounterDataCollection)
        {
            DataTable perfRunResultsDataTable = CreateReport.CreatePerfRunTable();
            return CreateReport.AddPerfRunDataToTable(perfRunResultsDataTable, perfCounterDataCollection);
        }

        private static DataTable CreatePerfRunTable()
        {
            DataTable perfRunResultsDataTable = new DataTable();
            perfRunResultsDataTable.Columns.Add("CounterName", typeof(string));
            perfRunResultsDataTable.Columns.Add("Min", typeof(double));
            perfRunResultsDataTable.Columns.Add("Avg", typeof(double));
            perfRunResultsDataTable.Columns.Add("Max", typeof(double));
            return perfRunResultsDataTable;
        }

        private static DataTable AddPerfRunDataToTable(
            DataTable perfRunResultsDataTable,
            Collection<PerfCounterData> perfCounterDataCollection)
        {
            foreach (PerfCounterData perfCounterData in perfCounterDataCollection)
            {
                perfRunResultsDataTable.Rows.Add(
                    perfCounterData.FriendlyName,
                    perfCounterData.MinValue,
                    perfCounterData.AvgValue,
                    perfCounterData.MaxValue);
            }

            return perfRunResultsDataTable;
        }

        private static StringBuilder GetTableRowsHtml(DataTable targetTable, StringBuilder htmlBuilder)
        {
            ////Create Data Rows
            foreach (DataRow myRow in targetTable.Rows)
            {
                htmlBuilder.Append("<tr align='left' valign='top' bgcolor=#BDEDFF>");

                if (targetTable.Columns.Count > 0)
                {
                    foreach (DataColumn targetColumn in targetTable.Columns)
                    {
                        htmlBuilder.Append("<td style='width: 140px' cellspacing='0'>");
                        htmlBuilder.Append(myRow[targetColumn.ColumnName].ToString());
                        htmlBuilder.Append("</td>");
                    }
                }

                htmlBuilder.Append("</tr>");
            }

            return htmlBuilder;
        }

        private static StringBuilder GetBottomPortionOfHtml(StringBuilder htmlBuilder)
        {
            ////Create Bottom Portion of HTML Document
            htmlBuilder.Append("</tbody>");
            htmlBuilder.Append("</table>");
            htmlBuilder.Append("</body>");
            htmlBuilder.Append("</html>");
            return htmlBuilder;
        }

        private double GetCounterAvgValue(string perfLogFilePath, int numberOfLines, int index)
        {
            StreamReader sr = new StreamReader(perfLogFilePath);
            double sum = 0.0;
            int numberOfSamples = 0;
            double avgValue = 0.0;

            ////Note: Iterating from i=1 to skip first row in perflog file
            for (int i = 0; i < numberOfLines; i++)
            {
                string[] sline = sr.ReadLine().Split(',');
                //Console.WriteLine("line number: {0}", i);                
                if (i >= 1)
                {
                    string counterValue = sline[index];
                    counterValue = counterValue.Remove(0, 1);
                    counterValue = counterValue.Remove(counterValue.Length - 1, 1);
                    //Console.WriteLine(latencyValue);
                    if ((counterValue != String.Empty) && (counterValue != null) && (counterValue != " "))
                    {
                        double value = Double.Parse(counterValue);
                        if (value > 0)
                        {
                            sum += value;
                            numberOfSamples++;
                        }
                    }
                }
            }

            //Console.WriteLine("Sum: " + sum);
            //Console.WriteLine("Number of samples: " + numberOfLines);
            avgValue = sum / numberOfLines;

            if (double.IsNaN(avgValue))
            {
                avgValue = 0.0;
            }

            return Math.Round(avgValue, 2);
        }

        private double GetMinMaxValue(string perfLogFilePath, int numberOfLines, int index, bool calculateMax)
        {
            StreamReader sr = new StreamReader(perfLogFilePath);
            int numberOfSamples = 0;
            double value = 0.0;

            ////Note: Iterating from i=1 to skip first row in perflog file
            for (int i = 0; i < numberOfLines; i++)
            {
                string[] sline = sr.ReadLine().Split(',');
                //Console.WriteLine("line number: {0}", i);                
                if (i >= 1)
                {
                    string counterValue = sline[index];
                    counterValue = counterValue.Remove(0, 1);
                    counterValue = counterValue.Remove(counterValue.Length - 1, 1);
                    //Console.WriteLine(latencyValue);
                    if ((counterValue != String.Empty) && (counterValue != null) && (counterValue != " "))
                    {
                        double currentValue = Double.Parse(counterValue);
                        if (calculateMax)
                        {
                            if ((currentValue > 0) && (currentValue > value))
                            {
                                value = currentValue;
                                numberOfSamples++;
                            }
                        }
                        else
                        {
                            if ((currentValue > 0) && (currentValue < value))
                            {
                                value = currentValue;
                                numberOfSamples++;
                            }
                        }
                    }
                }
            }

            if (double.IsNaN(value))
            {
                value = 0.0;
            }

            return Math.Round(value, 2);
        }

        private Collection<PerfCounterData> GetPerfCounterDataCollection(string dataFile)
        {
            Collection<PerfCounterData> perfCounterDataCollection =
                new Collection<PerfCounterData>();
            string text = System.IO.File.ReadAllText(dataFile);
            IEnumerable<Counter> counters = PerfCounterListParser.ParseXml(text);

            foreach (Counter counter in counters)
            {
                PerfCounterData perfCounterData = new PerfCounterData();
                perfCounterData.CounterName = counter.PerfCounterName;
                perfCounterData.FriendlyName = counter.FriendlyName;
                perfCounterDataCollection.Add(perfCounterData);
            }

            return perfCounterDataCollection;
        }

        private DataTable GetCallsResultsTable(Collection<CallsData> callsDataCollection)
        {
            DataTable callResultsDataTable = this.CreateCallResultTable();
            return this.AddCallsDataToTable(callResultsDataTable, callsDataCollection);
        }

        private DataTable CreateCallResultTable()
        {
            DataTable callResultsDataTable = new DataTable();
            callResultsDataTable.Columns.Add("CounterName", typeof(string));
            callResultsDataTable.Columns.Add("Total Calls", typeof(double));
            callResultsDataTable.Columns.Add("Total Failed Calls", typeof(double));
            callResultsDataTable.Columns.Add("% Failure", typeof(double));
            return callResultsDataTable;
        }

        private DataTable AddCallsDataToTable(
            DataTable callsResultsDataTable,
            Collection<CallsData> callsDataCollection)
        {
            foreach (CallsData callsData in callsDataCollection)
            {
                callsResultsDataTable.Rows.Add(
                    callsData.FriendlyName,
                    callsData.TotalCalls,
                    callsData.TotalFailedCalls,
                    callsData.PercentFailure);
            }

            return callsResultsDataTable;
        }

        private Collection<PerfCounterData> PopulatePerfDataFromPerfLogfile(
            Collection<PerfCounterData> perfCounterDataCollection,
            string perfLogFilePath)
        {
            Collection<PerfCounterData> populatedPerfCounterData = new Collection<PerfCounterData>();
            foreach (PerfCounterData perfCounterData in perfCounterDataCollection)
            {
                populatedPerfCounterData.Add(this.GetValueFromPerfLogFile(perfLogFilePath,
                    perfCounterData));
            }

            return populatedPerfCounterData;
        }

        private Collection<CallsData> UnionCallsAndFailedCallsData(
            Collection<PerfCounterData> totalCallsPerfCounterDataCollection,
            Collection<PerfCounterData> failedCallsPerfCounterDataCollection)
        {
            Collection<CallsData> callsAndFailedCallsDataCollection =
                new Collection<CallsData>();
            foreach (PerfCounterData totalCallsPerfCounterData in totalCallsPerfCounterDataCollection)
            {
                PerfCounterData matchingFailedCallsPerfCounterData =
                    failedCallsPerfCounterDataCollection.FirstOrDefault(a => a.CounterName ==
                                                                             totalCallsPerfCounterData.CounterName);

                CallsData callsData = new CallsData();
                callsData.CounterName = totalCallsPerfCounterData.CounterName;
                callsData.FriendlyName = totalCallsPerfCounterData.FriendlyName;
                callsData.TotalCalls = totalCallsPerfCounterData.MaxValue;
                callsData.TotalFailedCalls = matchingFailedCallsPerfCounterData.MaxValue;
                callsData.PercentFailure = Math.Round(((Convert.ToDouble(callsData.TotalFailedCalls) /
                                                        Convert.ToDouble(callsData.TotalCalls)) * 100), 2);
                callsAndFailedCallsDataCollection.Add(callsData);
            }

            return callsAndFailedCallsDataCollection;
        }
    }
}
