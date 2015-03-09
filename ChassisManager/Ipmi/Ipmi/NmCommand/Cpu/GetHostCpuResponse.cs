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
    /// Represents the Node Manager 'Get Host Cpu Data' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetHostCpuData)]
    public class GetHostCpuResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Host CPU data 
        /// [7] � Set to 1 if End of POST notification was received.
        /// [6:5] � Reserved. Write as 00b.
        /// [4] � Set to 1 if Host CPU discovery data provided with that command is valid. 
        /// [3] � Set to 1 if Intel NM already activated regular power limiting policies after 
        ///       Host startup.
        /// [2:0] � Reserved. Write as 000b
        /// </summary>
        private byte hostCpuData;

        /// <summary>
        /// Number of P-states supported by the current platform CPU configuration
        /// 0 � If P-states are disabled by the user.
        /// 1 � If CPU does not support more P-states or in the multiprocessor environment 
        ///     some processors installed on board do not match the lowest number processor power
        ///     consumption parameters.
        /// 2 � 255 � Actual number of supported P-states by the lowest number processor. 
        /// </summary>
        private byte pStateSupport;

        /// <summary>
        /// Number of T-states supported by the current platform CPU configuration
        /// 0 � If T-states are disabled by the user.
        /// 1 � 255 � Actual number of supported T-states by the lowest number processor. 
        /// </summary>
        private byte tStateSupport;

        /// <summary>
        /// Number of installed processor packages. This value is calculated as a 
        /// number of all sockets with CPU package present
        /// </summary>
        private byte noProcessors;

        /// <summary>
        /// Processor Discovery Data for the lowest number processor in LSByte-first order. 
        /// Turbo power current Limit MSR 1Ach for the lowest number processor passed by BIOS
        /// </summary>
        private byte[] procDiscoveryData1;

        /// <summary>
        /// Processor Discovery Data 2 for the lowest number processor in LSByte-first order. 
        /// Platform Info MSR 0Ceh for the lowest number processor passed by BIOS
        /// </summary>
        private byte[] procDiscoveryData2;

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
        /// Host CPU data 
        /// [7] � Set to 1 if End of POST notification was received.
        /// [6:5] � Reserved. Write as 00b.
        /// [4] � Set to 1 if Host CPU discovery data provided with that command is valid. 
        /// [3] � Set to 1 if Intel NM already activated regular power limiting policies after 
        ///       Host startup.
        /// [2:0] � Reserved. Write as 000b
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte HostCpuData
        {
            get { return this.hostCpuData; }
            set { this.hostCpuData = value; }
        }

        /// <summary>
        /// Number of P-states supported by the current platform CPU configuration
        /// 0 � If P-states are disabled by the user.
        /// 1 � If CPU does not support more P-states or in the multiprocessor environment 
        ///     some processors installed on board do not match the lowest number processor power
        ///     consumption parameters.
        /// 2 � 255 � Actual number of supported P-states by the lowest number processor. 
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte PstateCount
        {
            get { return this.pStateSupport; }
            set { this.pStateSupport = value; }
        }

        /// <summary>
        /// Number of T-states supported by the current platform CPU configuration
        /// 0 � If T-states are disabled by the user.
        /// 1 � 255 � Actual number of supported T-states by the lowest number processor. 
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte TstateCount
        {
            get { return this.tStateSupport; }
            set { this.tStateSupport = value; }
        }

        /// <summary>
        /// Number of installed processor packages. This value is calculated as a 
        /// number of all sockets with CPU package present
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte ProcessorPackages
        {
            get { return this.noProcessors; }
            set { this.noProcessors = value; }
        }

        /// <summary>
        /// Processor Discovery Data for the lowest number processor in LSByte-first order. 
        /// Turbo power current Limit MSR 1Ach for the lowest number processor passed by BIOS
        /// </summary>
        [NodeManagerMessageData(7, 8)]
        public byte[] ProcessorDiscoveryData1
        {
            get { return this.procDiscoveryData1; }
            set { this.procDiscoveryData1 = value; }
        }

        /// <summary>
        /// Processor Discovery Data 2 for the lowest number processor in LSByte-first order. 
        /// Platform Info MSR 0Ceh for the lowest number processor passed by BIOS
        /// </summary>
        [NodeManagerMessageData(15, 8)]
        public byte[] ProcessorDiscoveryData2
        {
            get { return this.procDiscoveryData2; }
            set { this.procDiscoveryData2 = value; }
        }

    }
}
