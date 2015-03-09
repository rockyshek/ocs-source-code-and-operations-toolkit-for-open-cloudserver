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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager
{

    /// <summary>
    /// Represents the Node Manager 'Get Dengate Health Status' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.Application, NodeManagerCommand.GetDengateHealthStatus)]
    public class GetDengateHealthStatusResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;
        
        /// <summary>
        /// Tag
        /// Bits [0:14] - Tag � Same as in the request
        /// Bit[15] � Last fragment indicator
        ///      0 � there are subsequent records to be retrieved; the 
        ///          requestor sets Tag=0 in the initial request and iterates until 
        ///          ME returns �last fragment indicator� =1
        ///      1 � this is the last record
        /// </summary>
        private ushort tag;

        /// <summary>
        /// Health Status Type
        /// </summary>
        private byte healthStatus;

        /// <summary>
        /// Health Status type specific.
        /// </summary>
        private byte[] responseData;

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        [NodeManagerMessageData(0, 3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
            set { this.manufactureId = value; }
        }

        /// <summary>
        /// Tag
        /// Bits [0:14] - Tag � Same as in the request
        /// Bit[15] � Last fragment indicator
        ///      0 � there are subsequent records to be retrieved; the 
        ///          requestor sets Tag=0 in the initial request and iterates until 
        ///          ME returns �last fragment indicator� =1
        ///      1 � this is the last record
        /// </summary>
        [NodeManagerMessageData(3)]
        public ushort Tag
        {
            get { return this.tag; }
            set { this.tag = value; }
        }

        /// <summary>
        /// Health Status Type
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte HealthStatus
        {
            get { return this.healthStatus; }
            set { this.healthStatus = value; }
        }

        /// <summary>
        /// Response Data is Health Status type specific.
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte[] ResponseData
        {
            get { return this.responseData; }
            set { this.responseData = value; }
        }

    }
}
