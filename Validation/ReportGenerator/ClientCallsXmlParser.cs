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
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.GFS.WCS.Test.ReportGenerator
{
    public class ClientCallsXmlParser
    {
        public static IEnumerable<ResultOfTest> ParseXml(string xml)
        {
            XNamespace namespaceWcs = @"http://schemas.datacontract.org/2004/07/Microsoft.GFS.WCS.Test.Framework";
            XDocument xDocument = XDocument.Parse(xml);
            IEnumerable<ResultOfTest> result = from resultOfTest in xDocument.Descendants(namespaceWcs + "ResultOfTest")
                                               select new ResultOfTest()
                                               {
                                                   ErrorMessage = resultOfTest.Element(namespaceWcs + "ErrorMessage").SafeElementValue(),
                                                   RestUri = resultOfTest.Element(namespaceWcs + "RestUri").SafeElementValue(),
                                                   ApiName = ClientCallsXmlParser.GetApiName(resultOfTest.Element(namespaceWcs + "RestUri").SafeElementValue()),
                                                   StartTime = ClientCallsXmlParser.GetTrimmedValue(
                                                       resultOfTest.Element(namespaceWcs + "StartTime").SafeElementValue(),
                                                       'T',
                                                       'Z'),
                                                   State = resultOfTest.Element(namespaceWcs + "State").SafeElementValue(),
                                                   TotalExecutionTime = ClientCallsXmlParser.GetExecutionTimeInSec(
                                                       resultOfTest.Element(namespaceWcs + "TotalExecutionTime").SafeElementValue())
                                               };
            return result;
        }

        private static string GetApiName(string restUri)
        {
            string[] uriElements = restUri.Split('/');
            return uriElements[3].Split('?')[0];
        }

        private static string GetTrimmedValue(string value, char startChar, char endChar)
        {
            int startIndex = value.IndexOf(startChar);
            int endIndex = value.IndexOf(endChar);
            return value.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        private static string GetExecutionTimeInSec(string value)
        {
            int startIndex = value.IndexOf('T');
            int minIndex = value.IndexOf('M');
            int endIndex = value.IndexOf('S');
            double min = 0.0;
            double sec = 0;
            if (minIndex >= 0)
            {
                min = Convert.ToDouble(value.Substring(startIndex + 1, minIndex - startIndex - 1));
                sec = Convert.ToDouble(value.Substring(minIndex + 1, endIndex - minIndex - 1));
            }
            else
            {
                sec = Convert.ToDouble(value.Substring(startIndex + 1, endIndex - startIndex - 1));
            }

            return (min * 60 + sec).ToString();
        }
    }

    public class ResultOfTest
    {
        public string ErrorMessage { get; set; }

        public string RestUri { get; set; }

        public string ApiName { get; set; }

        public string StartTime { get; set; }

        public string State { get; set; }

        public string TotalExecutionTime { get; set; }

        public string ToString(int testResultIndex = 0)
        {
            string result = string.Format("{0}{1}{2}{3}{4}{5}{6}", string.Format("TestResult:{0}\n\n", testResultIndex), string.Format("ErrorMessage:{0}\n", this.ErrorMessage), string.Format("RestUri:{0}\n", this.RestUri), string.Format("APIName:{0}\n", this.ApiName), string.Format("StartTime:{0}\n", this.StartTime), string.Format("State:{0}\n", this.State), string.Format("TotalExecutionTime:{0}\n", this.TotalExecutionTime));

            return result;
        }
    }
}
