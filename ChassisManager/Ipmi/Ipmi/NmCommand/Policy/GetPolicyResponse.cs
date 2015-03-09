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
    /// Represents the Node Manager 'Get Policy' response message.
    /// </summary>
    [NodeManagerMessageResponse(NodeManagerFunctions.NodeManager, NodeManagerCommand.GetPolicy)]
    public class GetPolicyResponse : NodeManagerResponse
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private byte[] manufactureId;

        /// <summary>
        /// Domain Id
        /// [0:3] Domain Id
        /// [4]   Policy enabled.
        /// [5:7] Reserved. Write as 00.
        /// </summary>
        private byte domainId;

        /// <summary>
        /// Policy Type [0:3] Action [4] Correction [5:6] Persistence [7]
        /// </summary>
        private byte policyType;


        /// <summary>
        /// Node Manager Exception Action.
        /// </summary>
        private byte exceptionAction;


        /// <summary>
        /// Target limit depends on Policy Type [0:3]
        ///     0, 1, 3:    Power Limt in Watts
        ///     2:          Thorttle level of platform in % [whereby 100% is maximum throttling]
        ///     4:          Power Profile during boot mode.       
        /// </summary>
        private ushort targetLimit;

        /// <summary>
        /// Correction Time Limit:  the maximum time in ms that node manager must take corrective
        /// action to bring the platform back to the specified power limit before taking the
        /// policy exception action.
        /// 
        /// If the policy type defines Boot Time Policy the correction limit will be overridden 
        /// to zero.
        /// </summary>
        private uint correctionTime;

        /// <summary>
        /// Trigger Limit depends on Policy Type [0:3]
        ///     0: Trigger value will be ignored
        ///     1: Trigger value should defined temperature in Celsius.
        ///     2: Trigger should define time in 1/10th of a second.
        ///     3: Trigger should be in 1/10th of second after reset or startup.
        ///     4: Trigger Limit is not applicable for boot time and will be overridden to zero.  
        /// </summary>
        private ushort triggerLimit;

        /// <summary>
        ///  Statistics Reporting Period in seconds. The number of seconds that the measured power 
        ///  will be averaged over for the purpose of reporting statistics. Note that this value 
        ///  is different from the period that Node Manager uses for maintaining an average for the 
        ///  purpose of power control.
        /// </summary>
        private ushort statisticReporting;

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
        /// Domain Id
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte DomainId
        {
            get { return (byte)(this.domainId & 0x0f); }
            set { this.domainId = value; }
        }

        /// <summary>
        /// Policy Enabled
        /// </summary>
        public bool PolicyEnabled
        {
            get { if((byte)(this.domainId & 0x10) == 0x10)
                    return true;
                  else 
                    return false;
            }
        }

        /// <summary>
        /// Per domain policy enabled
        /// </summary>
        public bool PerDomainPolicyEnabled
        {
            get
            {
                if ((byte)(this.domainId & 0x20) == 0x20)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Global Policy control enabled
        /// </summary>
        public bool GlobalPolicyControlEnabled
        {
            get
            {
                if ((byte)(this.domainId & 0x40) == 0x40)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Dcmi Policy control enabled
        /// </summary>
        public bool DcmiPolicyControlEnabled
        {
            get
            {
                if ((byte)(this.domainId & 0x80) == 0x80)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Policy Type
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte PolicyType
        {
            get { return (byte)(this.policyType & 0x0f); }
            set { this.policyType = value; }
        }

        /// <summary>
        /// CPU Power Correction Aggression
        /// </summary>
        public byte PowerCorrection
        {
            get
            {
                return (byte)((this.policyType >> 6) & 0x03);
            }
        }

        /// <summary>
        ///  Policy Persistence
        /// </summary>
        public bool Persistent
        {
            get
            {
                if ((byte)(this.policyType & 0x80) == 0x80)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Node Manager Exception Action.
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte ExceptionAction
        {
            get { return (byte)(this.exceptionAction & 0x03); }
            set { this.exceptionAction = value; }
        }

        /// <summary>
        /// Target limit depends on Policy Type [0:3]
        ///     0, 1, 3:    Power Limt in Watts
        ///     2:          Thorttle level of platform in % [whereby 100% is maximum throttling]
        ///     4:          Power Profile during boot mode.       
        /// </summary>
        [NodeManagerMessageData(7)]
        public ushort PowerLimit
        {
            get { return this.targetLimit; }
            set { this.targetLimit = value; }
        }

        /// <summary>
        /// Correction Time Limit:  the maximum time in ms that node manager must take corrective
        /// action to bring the platform back to the specified power limit before taking the
        /// policy exception action.
        /// 
        /// If the policy type defines Boot Time Policy the correction limit will be overridden 
        /// to zero.
        /// </summary>
        [NodeManagerMessageData(9)]
        public uint CorrectionTime
        {
            get { return this.correctionTime; }
            set { this.correctionTime = value; }
        }

        /// <summary>
        /// Trigger Limit depends on Policy Type [0:3]
        ///     0: Trigger value will be ignored
        ///     1: Trigger value should defined temperature in Celsius.
        ///     2: Trigger should define time in 1/10th of a second.
        ///     3: Trigger should be in 1/10th of second after reset or startup.
        ///     4: Trigger Limit is not applicable for boot time and will be overridden to zero.  
        /// </summary>
        [NodeManagerMessageData(13)]
        public ushort TriggerLimit
        {
            get { return this.triggerLimit; }
            set { this.triggerLimit = value; }
        }

        /// <summary>
        ///  Statistics Reporting Period in seconds. The number of seconds that the measured power 
        ///  will be averaged over for the purpose of reporting statistics. Note that this value 
        ///  is different from the period that Node Manager uses for maintaining an average for the 
        ///  purpose of power control.
        /// </summary>
        [NodeManagerMessageData(15)]
        public ushort StatisticReporting
        {
            get { return this.statisticReporting; }
            set { this.statisticReporting = value; }
        }

    }
}
