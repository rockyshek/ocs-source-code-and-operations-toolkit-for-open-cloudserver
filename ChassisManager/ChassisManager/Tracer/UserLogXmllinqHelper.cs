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

namespace Microsoft.GFS.WCS.ChassisManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.ServiceModel;
    using Microsoft.GFS.WCS.Contracts;
    using System.IO;
    using System.Globalization;

    public static class UserLogXmllinqHelper
    {
        /// <summary>
        /// Unified API for querying user chassis audit logs - both timestamp and maxEntries used as input
        /// </summary>
        /// <param name="filterStartTime"></param>
        /// <param name="filterEndTime"></param>
        /// <param name="maxEntries"></param>
        /// <returns>Returns list of user log when success else returns null.</returns>
        public static List<LogEntry> GetFilteredLogEntries(DateTime filterStartTime, DateTime filterEndTime, int maxEntries)
        {
            if (Tracer.GetCurrentUserLogFilePath() == null)
                return null;

            try
            {
                List<LogEntry> timestampFilteredEntries = new List<LogEntry>();

                // Parse the log entries from each user log file 
                // Note that these files could simultaneously be modified (switch primary, delete content etc)
                foreach (string filepath in Tracer.GetAllUserLogFilePaths())
                {
                    if (!File.Exists(filepath))
                    {
                        Tracer.WriteInfo("UserLogXmllinqHelper.GetFilteredLogEntries(): Skipping file ({0}) since it does not exist.");
                        continue;
                    }

                    using (FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (BufferedStream bstream = new BufferedStream(fileStream))
                    using (StreamReader reader = new StreamReader(bstream))
                    {
                        int index = 0;
                        const int count = 2048; // Reading 2K characters at a time to alleviate memory pressure
                        // Splitting file with arbitrary chunks size may result in chopped 'partial' XML data which is saved in this variable
                        // This data will be merged with the subsequent (consecutive) XML data 
                        string prevEntry = null;

                        while (!reader.EndOfStream)
                        {
                            char[] localbuffer = new char[count];
                            reader.Read(localbuffer, index, count);
                            string myData = new string(localbuffer);
                            myData = prevEntry + myData;
                            string[] subStrings = System.Text.RegularExpressions.Regex.Split(myData, @"ApplicationData>");
                            if (subStrings.Length < 1)
                                break;
                            prevEntry = subStrings[subStrings.Length - 1];

                            for (int i = 0; i < subStrings.Length - 1; i++)
                            {
                                string str = subStrings[i];
                                if (str.Length > 2 && str.Trim().EndsWith("</"))
                                {
                                    string currentEntry = (str.Remove(str.Length - 2));
                                    string[] tokens = currentEntry.Trim().Split(new char[] { ',' });
                                    LogEntry timestampFilteredEntry = new LogEntry();
                                    if (DateTime.TryParse(tokens[0], out timestampFilteredEntry.eventTime))
                                    {
                                        timestampFilteredEntry.eventTime = DateTime.ParseExact(tokens[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                                        timestampFilteredEntry.eventDescription = currentEntry.Replace(tokens[0] + ",", "");
                                        // Add this entry to the list only when the timestamp falls withing the parameter input range
                                        if(timestampFilteredEntry.eventTime >= filterStartTime && timestampFilteredEntry.eventTime <= filterEndTime)
                                            timestampFilteredEntries.Add(timestampFilteredEntry);
                                    }
                                    else
                                    {
                                        Tracer.WriteWarning("GetFilteredLogEntries(): Reading Chassis user log - ignoring entry '({0})' due to unparse-able date ", tokens[0]);
                                        // Skipping the entry since date is not parse-able
                                    }
                                }
                            }
                            prevEntry = subStrings[subStrings.Length - 1];
                        }
                    }
                }

                timestampFilteredEntries.Reverse();
                return timestampFilteredEntries.Take(maxEntries).ToList();
            }
            catch (Exception e)
            {
                Tracer.WriteError("GetFilteredLogEntries(): Reading Chassis user log exception " + e.Message);
                return null;
            }
        }
    }
}
