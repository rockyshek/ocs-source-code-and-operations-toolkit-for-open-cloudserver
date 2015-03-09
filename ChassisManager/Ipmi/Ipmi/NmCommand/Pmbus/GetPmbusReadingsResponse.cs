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
    /// Represents the Node Manager 'Get PMBUS Readings' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.Application, NodeManagerCommand.GetPmbusReadings)]
    public class GetPmbusReadingsResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Timestamp
        /// </summary>
        private uint timestamp;

        /// <summary>
        /// Register Value of Monitored Register [First Register Offset]
        /// For READ_EIN and READ_EOUT this field contains value converted to Watts
        /// </summary>
        private ushort registerValue;

        /// <summary>
        /// Length of the response depends on number of monitored registers. 
        /// Bytes + are used only if the PMBUS-enabled device is monitored 
        /// for the sensors
        /// </summary>
        private byte[] registerOffsets;

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
        /// Timestamp
        /// </summary>
        [NodeManagerMessageData(3)]
        public uint TimeStamp
        {
            get { return this.timestamp; }
            set { this.timestamp = value; }
        }

        /// <summary>
        /// Register Value of Monitored Register [First Register Offset]
        /// For READ_EIN and READ_EOUT this field contains value converted to Watts
        /// </summary>
        [NodeManagerMessageData(7)]
        public ushort RegisterValue
        {
            get { return this.registerValue; }
            set { this.registerValue = value; }
        }

        /// <summary>
        /// Length of the response depends on number of monitored registers. 
        /// Bytes + are used only if the PMBUS-enabled device is monitored 
        /// for the sensors
        /// </summary>
        [NodeManagerMessageData(9)]
        public byte[] RegisterOffsets
        {
            get { return this.registerOffsets; }
            set { this.registerOffsets = value; }
        }




    }
}
