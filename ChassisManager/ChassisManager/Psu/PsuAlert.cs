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

namespace Microsoft.GFS.WCS.ChassisManager
{
    /// <summary>
    /// Chassis PDB PSU_ALERT.  PCA9535C (0x48) Pin 4.
    /// </summary>
    class PsuAlertSignal : ChassisSendReceive
    {

        /// <summary>
        /// Get Psu Alert
        /// </summary>
        internal PsuAlertSignalResponse GetPsuAlertSignal()
        {
            PsuAlertSignalResponse psuAlert = (PsuAlertSignalResponse)this.SendReceive(DeviceType.PsuAlertInput, 0xff,
                new PsuAlertSignalRequest(), typeof(PsuAlertSignalResponse), (byte)PriorityLevel.System);

            return psuAlert;
        }

        /// <summary>
        /// Set Psu Alert On
        /// </summary>
        internal SetPsuAlertOnResponse SetPsuAlertOn()
        {
            SetPsuAlertOnResponse psuAlert = (SetPsuAlertOnResponse)this.SendReceive(DeviceType.PsuAlertOutput, 0xff,
                new SetPsuAlertOnRequest(), typeof(SetPsuAlertOnResponse), (byte)PriorityLevel.System);

            return psuAlert;
        }

        /// <summary>
        /// Set Psu Alert Off
        /// </summary>
        internal SetPsuAlertOffResponse SetPsuAlertOff()
        {
            SetPsuAlertOffResponse psuAlert = (SetPsuAlertOffResponse)this.SendReceive(DeviceType.PsuAlertOutput, 0xff,
                new SetPsuAlertOffRequest(), typeof(SetPsuAlertOffResponse), (byte)PriorityLevel.System);

            return psuAlert;
        }

        /// <summary>
        /// Set Psu Alert Off
        /// </summary>
        internal GetPsuAlertOnOffResponse GetPsuAlertOnOffStatus()
        {
            GetPsuAlertOnOffResponse psuAlert = (GetPsuAlertOnOffResponse)this.SendReceive(DeviceType.PsuAlertOutput, 0xff,
                new GetPsuAlertOnOffRequest(), typeof(GetPsuAlertOnOffResponse), (byte)PriorityLevel.System);

            return psuAlert;
        }

    }

    /// <summary>
    /// Chassis PDB PSU_ALERT. Request Message.
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuAlertInput)]
    internal class PsuAlertSignalRequest : ChassisRequest
    {
    }

    /// <summary>
    /// Chassis PDB PSU_ALERT. Response Message.
    /// </summary>
    [ChassisMessageResponse(FunctionCode.PsuAlertInput)]
    internal class PsuAlertSignalResponse : ChassisResponse
    {
        // Psu Alert Status
        private byte status;

        /// <summary>
        /// Psu Alert Status
        /// </summary>
        [ChassisMessageData(0)]
        public byte Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

        /// <summary>
        /// If Completion Code is zero,
        /// this value returns the status
        /// of PSU Alert.
        /// </summary>
        public bool PsuAlertActive
        {
            get { return (status == 0x01 ? true : false); }
        }

    }

    /// <summary>
    /// Chassis PDB Set PSU_ALERT On. Request Message.
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuAlertOuputOn)]
    internal class SetPsuAlertOnRequest : ChassisRequest
    {
    }

    /// <summary>
    /// Chassis PDB Set PSU_ALERT On. Response Message.
    /// </summary>
    [ChassisMessageResponse(FunctionCode.PsuAlertOuputOn)]
    internal class SetPsuAlertOnResponse : ChassisResponse
    {
        // Psu Alert Status
        private byte status;

        /// <summary>
        /// Psu Alert Status
        /// </summary>
        [ChassisMessageData(0)]
        public byte Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

    }

    /// <summary>
    /// Chassis PDB Set PSU_ALERT On. Request Message.
    /// </summary>
    [ChassisMessageRequest(FunctionCode.PsuAlertOuputOff)]
    internal class SetPsuAlertOffRequest : ChassisRequest
    {
    }

    /// <summary>
    /// Chassis PDB Set PSU_ALERT Off. Response Message.
    /// </summary>
    [ChassisMessageResponse(FunctionCode.PsuAlertOuputOff)]
    internal class SetPsuAlertOffResponse : ChassisResponse
    {
        // Psu Alert Status
        private byte status;

        /// <summary>
        /// Psu Alert Status
        /// </summary>
        [ChassisMessageData(0)]
        public byte Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

    }

    /// <summary>
    /// Get Chassis PDB SET PSU_ALERT On/Off. Request Message.
    /// </summary>
    [ChassisMessageRequest(FunctionCode.GetPsuAlertOuput)]
    internal class GetPsuAlertOnOffRequest : ChassisRequest
    {
    }

    /// <summary>
    /// Get Chassis PDB SET PSU_ALERT On/Off. Request Message.
    /// </summary>
    [ChassisMessageResponse(FunctionCode.GetPsuAlertOuput)]
    internal class GetPsuAlertOnOffResponse : ChassisResponse
    {
        // Set Psu Alert Status pin
        private byte status;

        /// <summary>
        // Set Psu Alert Status pin
        /// </summary>
        [ChassisMessageData(0)]
        public byte Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

        /// <summary>
        /// If Completion Code is zero,
        /// this value returns the status
        /// of Set PSU Alert pin.
        /// </summary>
        public bool IsPsuAlertSet
        {
            get { return (status == 0x01 ? true : false); }
        }

    }

}
