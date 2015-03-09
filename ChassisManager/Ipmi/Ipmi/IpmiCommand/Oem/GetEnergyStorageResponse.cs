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
    /// Represents the IPMI 'Get Energy Storage' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemGroup, IpmiCommand.GetEnergyStorage)]
    internal class GetEnergyStorageResponse : IpmiResponse
    {
        /// <summary>
        /// Energy Storage Presence
        /// </summary>
        private byte presence;

        /// <summary>
        /// Energy Storage State
        /// </summary>
        private byte state;
        
        /// <summary>
        /// Scaling factor for energy in Joules
        /// </summary>
        private byte scalingFactor;

        /// <summary>
        /// Backup energy for the blade
        /// (in step size indicated by scalingFactor)
        /// </summary>
        private ushort bladeBackupEnergy;

        /// <summary>
        /// Backup energy for each NVDIMM
        /// (in step size indicated by scalingFactor)
        /// </summary>
        private byte nvdimmBackupEnergy;

        /// <summary>
        /// Rolling counter in seconds
        /// </summary>
        private ushort rollingCounter;

        /// <summary>
        /// Energy Storage Presence
        /// </summary>       
        [IpmiMessageData(0)]
        public byte Presence
        {
            get { return this.presence; }
            set { this.presence = (byte)(value & 0x03); }
        }

        /// <summary>
        /// Energy Storage State
        /// </summary>       
        [IpmiMessageData(0)]
        public byte EnergyState
        {
            get { return this.state; }
            set { this.state = (byte)((value & 0x1C) >> 2); }
        }

        /// <summary>
        /// Reserved
        /// </summary>       
        [IpmiMessageData(1)]
        public byte Reserved
        {
            get { return 0x00; }
            set { }
        }

        /// <summary>
        /// Scaling factor for energy in Joules
        /// </summary>       
        [IpmiMessageData(2)]
        public byte ScalingFactor
        {
            get { return this.scalingFactor; }
            set { this.scalingFactor = value; }
        }

        /// <summary>
        /// Backup energy for the blade
        /// (in step size indicated by scalingFactor)
        /// </summary>
        [IpmiMessageData(3)]
        public ushort BladeBackupEnergy
        {
            get { return this.bladeBackupEnergy; }
            set { this.bladeBackupEnergy = value; }
        }

        /// <summary>
        /// Backup energy for each NVDIMM
        /// (in step size indicated by scalingFactor)
        /// </summary>
        [IpmiMessageData(5)]
        public byte NvdimmBackupEnergy
        {
            get { return this.nvdimmBackupEnergy; }
            set { this.nvdimmBackupEnergy = value; }
        }

        /// <summary>
        /// Rolling counter in seconds
        /// </summary>       
        [IpmiMessageData(6)]
        public ushort RollingCounter
        {
            get { return this.rollingCounter; }
            set { this.rollingCounter = value; }
        }
    }
}
