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
    /// Represents the DCMI 'Get DCMI Capabilities' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Dcgrp, IpmiCommand.DcmiCapability)]
    internal class GetDcmiCapabilitiesResponse : IpmiResponse
    {
        /// <summary>
        /// Group Extension.
        /// </summary>
        private byte groupExtension;

        /// <summary>
        /// Specification Conformance (first byte).
        /// </summary>
        private byte specificationMajorVersion;

        /// <summary>
        /// Specification Conformance (second byte).
        /// </summary>
        private byte specificationMinorVersion;

        /// <summary>
        /// Parameter Revision.
        /// </summary>
        private byte parameterRevision;

        /// <summary>
        /// Response Data. Depends on Request Selector
        /// </summary>
        private byte[] responseData;

        /// <summary>
        /// Gets and sets the Group Extension.
        /// </summary>
        /// <value>Group Extension.</value>
        [IpmiMessageData(0)]
        public byte GroupExtension
        {
            get { return this.groupExtension; }
            set { this.groupExtension = value; }
        }

        /// <summary>
        /// Gets and sets the Specification Major Version (first byte).
        /// </summary>
        /// <value>Specification Conformance (first byte).</value>
        [IpmiMessageData(1)]
        public byte SpecificationMajorVersion
        {
            get { return this.specificationMajorVersion; }
            set { this.specificationMajorVersion = value; }
        }

        /// <summary>
        /// Gets and sets the Specification Major Version (second byte).
        /// </summary>
        /// <value>Specification Conformance (second byte).</value>
        [IpmiMessageData(2)]
        public byte SpecificationMinorVersion
        {
            get { return this.specificationMinorVersion; }
            set { this.specificationMinorVersion = value; }
        }

        /// <summary>
        /// Gets and sets the ParameterRevision (reserved).
        /// </summary>
        /// <value>ParameterRevision(reserved).</value>
        [IpmiMessageData(3)]
        public byte ParameterRevision
        {
            get { return this.parameterRevision; }
            set { this.parameterRevision = value; }
        }

        /// <summary>
        /// Gets and sets the Response Data.
        /// </summary>
        /// <value>Capabilities Response Data.</value>
        [IpmiMessageData(4)]
        public byte[] ResponseData
        {
            get { return this.responseData; }
            set { this.responseData = value; }
        }
    }
}
