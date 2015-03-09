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
    using System.Collections.Generic;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    public enum EvenLogClass
    {
        Unknown = 0,
        Discrete = 1,
        SensorSpecific = 2,
        OEM = 3
    }

    /// <summary>
    /// IPMI System Event Log string class
    /// </summary>
    public class EventLogData
    {
        private int number;
        private int offset;
        private EventLogMsgType messageClass;
        private string message = string.Empty;
        private string description = string.Empty;
        private Dictionary<int, string> extension = new Dictionary<int, string>();

        public EventLogData(int number, int offset, EventLogMsgType eventLogType, string message, string description)
        {
            this.number = number;
            this.offset = offset;
            this.messageClass = eventLogType;
            this.message = message;
            this.description = description;
        }

        public EventLogData()
        {
        }    

        /// <summary>
        /// Add Extension string value to dictionary object
        /// </summary>
        internal void AddExtension(int Id, string detail)
        {
            if (!extension.ContainsKey(Id))
                extension.Add(Id, detail);
        }

        /// <summary>
        /// Event Message String Number
        /// </summary>
        public int Number
        {
            get { return this.number; }
            internal set { this.number = value; }
        }

        /// <summary>
        /// Event Message String Offset
        /// </summary>
        public int OffSet
        {
            get { return this.offset; }
            internal set { this.offset = value; }
        }

        /// <summary>
        /// Event Message Classification
        /// </summary>
        public EventLogMsgType MessageClass
        {
            get { return this.messageClass; }
            internal set { this.messageClass = value; }
        }

        /// <summary>
        /// Event Message String
        /// </summary>
        public string EventMessage
        {
            get { return this.message; }
            internal set { this.message = value; }
        }

        /// <summary>
        /// Event Message Description
        /// </summary>
        public string Description
        {
            get { return this.description; }
            internal set { this.description = value; }
        }

        /// <summary>
        /// Event Message Extension
        /// </summary>
        public string GetExtension(int Id)
        {
            if (extension.ContainsKey(Id))
                return extension[Id];
            else
                return string.Empty;
        }

    }
}
