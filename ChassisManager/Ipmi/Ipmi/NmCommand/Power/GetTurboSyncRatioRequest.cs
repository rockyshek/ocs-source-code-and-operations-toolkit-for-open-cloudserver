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
    /// Represents the Node Manager 'Get Turbo Sync Ratio' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetTurboSyncRatio)]
    public abstract class GetTurboSyncRatioRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// CPU Socket Number
        /// 00h � 07h � CPU socket number for which current settings should 
        ///       be read. Supported value could depend on system configuration. 
        /// 08h � FEh � reserved. 
        /// FFh � all sockets will return common maximum settings. 
        /// </summary>
        private byte socketNo;

        /// <summary>
        /// Active cores configuration 
        /// 00h � reserved.
        /// Others � Setting should be applied to configuration of given active 
        /// cores number
        /// </summary>
        private byte activeCores;

        /// <summary>
        /// Initializes a new instance of the GetTurboSyncRatioRequest class.
        /// </summary>
        public GetTurboSyncRatioRequest(byte socketNumber, byte activeCoreConfig)
        {
            // CPU Socket Number
            this.socketNo = (byte)socketNumber;

            // Active cores configuration 
            this.activeCores = (byte)activeCoreConfig;
        }

        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        [NodeManagerMessageData(0,3)]
        public byte[] ManufactureId
        {
            get { return this.manufactureId; }
        }

        /// <summary>
        /// CPU Socket Number
        /// 00h � 07h � CPU socket number for which current settings should 
        /// be read. Supported value could depend on system configuration. 
        /// 08h � FEh � reserved. 
        /// FFh � all sockets will return common maximum settings. 
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte CpuSocketNumber
        {
            get { return this.socketNo; }
        }

        /// <summary>
        /// Active cores configuration
        /// 00h � reserved.
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte ActiveCoreConfiguration
        {
            get { return this.activeCores; }
        }
    }

    /// <summary>
    /// Sets the Turbo Ratio Limit on all active CPU cores
    /// </summary>
    public class GetAllCoreTurboSyncRatioRequest : GetTurboSyncRatioRequest
    { 
        /// <summary>
        /// Get the Turbo Ratio Limit on all active CPU cores
        /// </summary>
        public GetAllCoreTurboSyncRatioRequest()
            : base(0xFF, 0xFF)
        { 
            
        }
    }

    /// <summary>
    /// Sets the Turbo Ratio Limit on specified active cores.
    /// </summary>
    public class GetIndividualCoreTurboSyncRatioRequest : GetTurboSyncRatioRequest
    { 
        /// <summary>
        /// Get the Turbo Ratio Limit on specified CPU cores
        /// </summary>
        public GetIndividualCoreTurboSyncRatioRequest(byte socketNumber, byte activeCoreConfig)
            : base(socketNumber, activeCoreConfig)
        { 
            
        }
    }
}
