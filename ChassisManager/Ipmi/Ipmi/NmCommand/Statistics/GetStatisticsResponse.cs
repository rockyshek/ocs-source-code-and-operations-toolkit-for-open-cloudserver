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
    /// Represents the Node Manager 'Get Statistics' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetStatistics)]
    public class GetStatisticsResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Crrent value
        /// </summary>
        private ushort currentVal;

        /// <summary>
        /// Minimum value
        /// </summary>
        private ushort minimumVal;

        /// <summary>
        /// Maximum value
        /// </summary>
        private ushort maximumVal;

        /// <summary>
        /// Average value
        /// </summary>
        private ushort averageVal;

        /// <summary>
        /// Time Stamp
        /// </summary>
        private uint timestamp;

        /// <summary>
        /// Statistics Reporting period.
        /// </summary>
        private uint statisticsReporting;

        /// <summary>
        /// Domain Id | Policy State
        /// </summary>
        private byte domainId;

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
        /// Current Value
        /// </summary>
        [NodeManagerMessageData(3)]
        public ushort CurrentValue
        {
            get { return this.currentVal; }
            set { this.currentVal = value; }
        }

        /// <summary>
        /// Minimum Value
        /// </summary>
        [NodeManagerMessageData(5)]
        public ushort MinimumValue
        {
            get { return this.minimumVal; }
            set { this.minimumVal = value; }
        }

        /// <summary>
        /// Maximum Value
        /// </summary>
        [NodeManagerMessageData(7)]
        public ushort MaximumValue
        {
            get { return this.maximumVal; }
            set { this.maximumVal = value; }
        }

        /// <summary>
        /// Average Value
        /// </summary>
        [NodeManagerMessageData(9)]
        public ushort AverageValue
        {
            get { return this.averageVal; }
            set { this.averageVal = value; }
        }

        /// <summary>
        /// Maximum Value
        /// </summary>
        [NodeManagerMessageData(11)]
        public uint TimeStamp
        {
            get { return this.timestamp; }
            set { this.timestamp = value; }
        }

        /// <summary>
        /// Statistics Reporting
        /// </summary>
        [NodeManagerMessageData(15)]
        public uint StatisticsReporting
        {
            get { return this.statisticsReporting; }
            set { this.statisticsReporting = value; }
        }

        /// <summary>
        /// Domain Id | Policy State
        /// </summary>
        [NodeManagerMessageData(19)]
        public byte DomainIdPolicyState
        {
            get { return this.domainId; }
            set { this.domainId = value; }
        }

        /// <summary>
        /// Domain Id
        /// </summary>
        public byte DomainId
        {
            get { return (byte)(this.domainId & 0x0f); }
        }

        /// <summary>
        /// Policy State
        /// </summary>
        public byte PolicyState
        {
            get { return (byte)((this.domainId >> 4 )& 0x01); }
        }

        /// <summary>
        /// Policy Operational State
        /// </summary>
        public byte PolicyOperational
        {
            get { return (byte)((this.domainId >> 5) & 0x01); }
        }

        /// <summary>
        /// Policy Measurement State
        /// </summary>
        public byte MeasurementState
        {
            get { return (byte)((this.domainId >> 6) & 0x01); }
        }

        /// <summary>
        /// Policy Activation State
        /// </summary>
        public byte PolicyActivationState
        {
            get { return (byte)((this.domainId >> 7) & 0x01); }
        }


    }
}
