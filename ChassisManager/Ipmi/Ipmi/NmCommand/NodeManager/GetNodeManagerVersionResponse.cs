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
    /// Represents the Node Manager 'Get Version' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetVersion)]
    public class GetNodeManagerVersionResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Version Support
        /// </summary>
        private byte versionSupport;

        /// <summary>
        /// IPMI interface version 
        /// 01h � IPMI version 1.0
        /// 02h � IPMI version 2.0
        /// </summary>
        private byte ipmiVersion;

        /// <summary>
        /// Patch version (binary encoded).
        /// </summary>
        private byte patchVersion;

        /// <summary>
        /// Major version
        /// </summary>
        private byte majorVersion;

        /// <summary>
        /// Minor version
        /// </summary>
        private byte minorVersion;

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
        /// Version Support
        /// 01h � NM 1.0 one power policy.
        /// 02h � NM 2.5 multiple policies and thermal triggers for power policy.
        /// 03h � NM 2.0 multiple policies and thermal triggers for power policy.
        /// 04h � NM 2.5
        /// 05h � FFh � Reserved for future use.
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte VersionSupport
        {
            get { return this.versionSupport; }
            set { this.versionSupport = value; }
        }

        /// <summary>
        /// Ipmi Version
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte IpmiVersion
        {
            get { return this.ipmiVersion; }
            set { this.ipmiVersion = value; }
        }

        /// <summary>
        /// Patch Version
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte PatchVersion
        {
            get { return this.patchVersion; }
            set { this.patchVersion = value; }
        }

        /// <summary>
        /// Major Version
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte MajorVersion
        {
            get { return this.majorVersion; }
            set { this.majorVersion = value; }
        }

        /// <summary>
        /// Minor Version
        /// </summary>
        [NodeManagerMessageData(7)]
        public byte MinorVersion
        {
            get { return this.minorVersion; }
            set { this.minorVersion = value; }
        }

    }
}
