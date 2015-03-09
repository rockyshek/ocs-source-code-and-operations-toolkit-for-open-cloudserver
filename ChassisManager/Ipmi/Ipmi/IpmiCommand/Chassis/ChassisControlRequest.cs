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
    /// Represents the IPMI 'Chassis Control' chassis request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Chassis, IpmiCommand.ChassisControl, 1)]
    internal class ChassisControlRequest : IpmiRequest
    {
        /// <summary>
        /// Power down.
        /// </summary>
        internal const byte OperationPowerDown = 0;

        /// <summary>
        /// Power up.
        /// </summary>
        internal const byte OperationPowerUp = 1;

        /// <summary>
        /// Power cycle.
        /// </summary>
        internal const byte OperationPowerCycle = 2;

        /// <summary>
        /// Hard reset.
        /// </summary>
        internal const byte OperationHardReset = 3;

        /// <summary>
        /// Diagnostic interrupt.
        /// </summary>
        internal const byte OperationDiagnosticInterrupt = 4;

        /// <summary>
        /// Soft shutdown.
        /// </summary>
        internal const byte OperationSoftShutdown = 5;

        /// <summary>
        /// Operation to perform.
        /// </summary>
        private readonly byte operation;

        /// <summary>
        /// Initializes a new instance of the ChassisControlRequest class.
        /// </summary>
        /// <param name="operation">Operation to perform.</param>
        internal ChassisControlRequest(byte operation)
        {
            this.operation = operation;
        }

        /// <summary>
        /// Gets the operation to perform.
        /// </summary>
        [IpmiMessageData(0)]
        public byte Operation
        {
            get { return this.operation; }
        }
    }
}
