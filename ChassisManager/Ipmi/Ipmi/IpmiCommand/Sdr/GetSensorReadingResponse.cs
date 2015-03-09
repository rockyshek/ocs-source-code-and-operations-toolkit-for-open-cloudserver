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

namespace Microsoft.GFS.WCS.ChassisManager.Ipmi
{

    /// <summary>
    /// Represents the IPMI 'Get Sensor Reading' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.Sensor, IpmiCommand.SensorReading)]
    internal  class SensorReadingResponse : IpmiResponse
    {
        /// <summary>
        /// Sensor Reading.
        /// </summary>
        private byte sensorReading;
        
        /// <summary>
        /// Sensor Status.
        /// </summary>
        private byte sensorStatus;

        /// <summary>
        /// Threshold / Discrete Offset.
        /// </summary>
        private byte stateOffset;

        /// <summary>
        /// Optional Discrete Offset.
        /// </summary>
        private byte optionalOffset;

        /// <summary>
        /// Gets and sets the Sensor Reading.
        /// </summary>
        /// <value>Sensor Reading.</value>
        [IpmiMessageData(0)]
        public byte SensorReading
        {
            get { return this.sensorReading; }
            set { this.sensorReading = value; }
        }

        /// <summary>
        /// Gets and sets the Sensor Status.
        /// </summary>
        /// <value>Sensor Status.</value>
        [IpmiMessageData(1)]
        public byte SensorStatus
        {
            get { return this.sensorStatus; }
            set { this.sensorStatus = value; }
        }

        /// <summary>
        /// Gets and sets the State OffSet.
        /// (Only applies to Threshold/Discrete)
        /// </summary>
        /// <value>State Offset.</value>
        [IpmiMessageData(2)]
        public byte StateOffset
        {
            get { return this.stateOffset; }
            set { this.stateOffset = value; }
        }

        /// <summary>
        /// Gets and sets the Optional OffSet.
        /// </summary>
        /// <value>Optional Offset.</value>
        [IpmiMessageData(3)]
        public byte OptionalOffset
        {
            get { return this.optionalOffset; }
            set { this.optionalOffset = value; }
        }
    }
}
