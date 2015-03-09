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

    using System;

    /// <summary>
    /// Represents the IPMI 'SetEnergyStorage' OEM request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.OemGroup, IpmiCommand.SetEnergyStorage)]
    internal class SetEnergyStorageRequest : IpmiRequest
    {
        /// <summary>
        /// Energy Storage State
        /// </summary>
        private readonly byte state;

        /// <summary>
        /// Scaling factor for energy in Joules
        /// </summary>
        private readonly byte scalingFactor;

        /// <summary>
        /// Backup energy for the blade
        /// (in step size indicated by scalingFactor)
        /// </summary>
        private readonly ushort bladeBackupEnergy;

        /// <summary>
        /// Backup energy for each NVDIMM
        /// (in step size indicated by scalingFactor)
        /// </summary>
        private readonly byte nvdimmBackupEnergy;

        /// <summary>
        /// Rolling counter in seconds
        /// </summary>
        private readonly ushort rollingCounter;

        /// <summary>
        /// Set Energy Storage Constructor
        /// </summary>
        internal SetEnergyStorageRequest(bool batteryPresent, EnergyStorageState state, byte scalingFactor, 
            ushort bladeEnergy, byte nvdimmEnergy)
        {
            // Battery presence and state
            if (batteryPresent)
                this.state = 0x01;

            this.state = (byte)(this.state | ((byte)state << 2));

            // Energy scaling factor
            this.scalingFactor = scalingFactor;

            // Blade and NVDIMM energy
            this.bladeBackupEnergy = bladeEnergy;
            this.nvdimmBackupEnergy = nvdimmEnergy;

            // Rolling counter in seconds
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            double totalSeconds = DateTime.UtcNow.Subtract(epoch).TotalSeconds;
            this.rollingCounter = (ushort)totalSeconds;
        }

        /// <summary>
        /// Energy Storage State
        /// </summary>       
        [IpmiMessageData(0)]
        public byte State
        {
            get { return this.state; }
        }

        /// <summary>
        /// Reserved byte
        /// </summary>       
        [IpmiMessageData(1)]
        public byte Reserved
        {
            get { return 0x00; }
        }

        /// <summary>
        /// Scaling factor for energy in Joules
        /// </summary>       
        [IpmiMessageData(2)]
        public byte ScalingFactor
        {
            get { return this.scalingFactor; }
        }

        /// <summary>
        /// Backup energy for the blade
        /// (in step size indicated by scalingFactor)
        /// </summary>
        [IpmiMessageData(3)]
        public ushort BladeBackupEnergy
        {
            get { return this.bladeBackupEnergy; }
        }

        /// <summary>
        /// Backup energy for each NVDIMM
        /// (in step size indicated by scalingFactor)
        /// </summary>
        [IpmiMessageData(5)]
        public byte NvdimmBackupEnergy
        {
            get { return this.nvdimmBackupEnergy; }
        }

        /// <summary>
        /// Rolling counter in seconds
        /// </summary>       
        [IpmiMessageData(6)]
        public ushort RollingCounter
        {
            get { return this.rollingCounter; }
        }
    }
}
