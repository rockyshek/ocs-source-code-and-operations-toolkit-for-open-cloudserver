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
    public class PerfCounterListParser
    {
        public static IEnumerable<Counter> ParseXml(string xml)
        {
            XDocument xDocument = XDocument.Parse(xml);
            IEnumerable<Counter> result = from counter in xDocument.Descendants("Counter")
                                          select new Counter()
                                          {
                                              PerfCounterName = counter.Element("PerfCounterName").SafeElementValue(),
                                              FriendlyName = counter.Element("FriendlyName").SafeElementValue()
                                          };
            return result;
        }
    }

    public class Counter
    {
        public string PerfCounterName { get; set; }

        public string FriendlyName { get; set; }

        public string ToString(int counterIndex = 0)
        {
            string result = string.Format("{0}{1}{2}", string.Format("CounterIndex:{0}\n\n", counterIndex), string.Format("PerfCounterName:{0}\n", this.PerfCounterName), string.Format("FriendlyName:{0}\n", this.FriendlyName));

            return result;
        }
    }

    internal static class XmlElementExtension
    {
        public static string SafeElementValue(this XElement element)
        {
            if (element != null)
            {
                return element.Value;
            }

            return String.Empty;
        }
    }
}
