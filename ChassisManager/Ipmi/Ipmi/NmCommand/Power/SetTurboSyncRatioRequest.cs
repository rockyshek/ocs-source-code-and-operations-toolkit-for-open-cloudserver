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
    /// Represents the Node Manager 'Set Turbo Sync Ratio' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.SetTurboSyncRatio)]
    public abstract class SetTurboSyncRatioRequest : NodeManagerRequest
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
        /// FFh � apply settings to all active cores configuration 
        /// Others � Setting should be applied to configuration of given active 
        /// cores number
        /// </summary>
        private byte activeCores;

        /// <summary>
        /// Turbo Ratio Limit. 
        ///     00h � restore default settings 
        ///     Others � Turbo Ratio Limit to set
        /// </summary>
        private byte turboRatio;

        /// <summary>
        /// Initializes a new instance of the SetTurboSyncRatioRequest class.
        /// </summary>
        internal SetTurboSyncRatioRequest(byte socketNumber, byte activeCoreConfig, byte turboRatioLimit)
        {
            // CPU Socket Number
            this.socketNo = (byte)socketNumber;

            // Active cores configuration 
            this.activeCores = (byte)activeCoreConfig;

            // Turbo Ratio Limit. 
            this.turboRatio = (byte)turboRatioLimit;
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
        /// FFh � apply settings to all active cores configuration 
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte ActiveCoreConfiguration
        {
            get { return this.activeCores; }
        }

        /// <summary>
        /// Turbo Ratio Limit. 
        ///     00h � restore default settings 
        ///     Others � Turbo Ratio Limit to set
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte TurboRatioLimit
        {
            get { return this.turboRatio; }
        }


    }

    /// <summary>
    /// Sets the Turbo Ratio Limit on all active CPU cores
    /// </summary>
    public class SetAllCoreTurboSyncRatioRequest : SetTurboSyncRatioRequest
    { 
        /// <summary>
        /// Sets the Turbo Ratio Limit on all active CPU cores
        /// </summary>
        public SetAllCoreTurboSyncRatioRequest(byte turboRatioLimit)
            : base(0xFF, 0xFF, turboRatioLimit)
        { 
            
        }
    }

    /// <summary>
    /// Sets the Turbo Ratio Limit on specified active cores.
    /// </summary>
    public class SetIndividualCoreTurboSyncRatioRequest : SetTurboSyncRatioRequest
    { 
        /// <summary>
        /// Sets the Turbo Ratio Limit on all active CPU cores
        /// </summary>
        public SetIndividualCoreTurboSyncRatioRequest(byte socketNumber, byte activeCoreConfig, byte turboRatioLimit)
            : base(socketNumber, activeCoreConfig, turboRatioLimit)
        { 
            
        }
    }

    /// <summary>
    /// Resets Turbo Sync on all CPU to Default Settings
    /// </summary>
    public class ResetTurboSyncRatioRequest : SetTurboSyncRatioRequest
    {
        /// <summary>
        /// Resets Turbo Sync on all CPU to Default Settings
        /// </summary>
        public ResetTurboSyncRatioRequest()
            : base(0xFF, 0xFF, 0x00)
        { 
            
        }
    
    }

}
