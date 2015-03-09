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
    /// Represents the Node Manager 'Set Policy' request message.
    /// </summary>
    [NodeManagerMessageRequest(NodeManagerFunctions.NodeManager, NodeManagerCommand.SetPolicy)]
    public class SetPolicyRequest : NodeManagerRequest
    {
        /// <summary>
        /// Intel Manufacture Id
        /// </summary>
        private readonly byte[] manufactureId = { 0x57, 0x01, 0x00 };

        /// <summary>
        /// Domain Id
        /// [0:3] Domain Id
        /// [4]   Policy enabled.
        /// [5:7] Reserved. Write as 00.
        /// </summary>
        private byte domainId;

        /// <summary>
        /// Policy Id
        /// </summary>
        private byte policyId;


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
        /// Initializes a new instance of the SetPolicyRequest class.
        /// </summary>
        /// <param name="domainId">Node Manager Domain Id</param>
        /// <param name="policyEnabled">True if policy should be enabled by default</param>
        /// <param name="policyId">Unique Id of policy</param>
        /// <param name="policyType">Policy trigger target [Temp, Power, boot time]</param>
        /// <param name="action">Add/Remove policy</param>
        /// <param name="correction">Policy correction aggression</param>
        /// <param name="persistent">True if policy persists through Node Manager resets</param>
        /// <param name="exceptionAction">Action that occurs when policy gets violated</param>
        /// <param name="targetLimit">Depending on Policy Type, this value is Power Limt in Watts or % of throttle</param>
        /// <param name="correctionTime">Time in ms corrective action must be taking before exception action</param>
        /// <param name="triggerLimit">Depends on Policy Type.  This value should be temperature in celsius or time in 1/10th of a second</param>
        /// <param name="statisticReporting">Reporting only. The number of seconds that the measured power will be averaged and reported. [Note: does not alter control]</param>
        internal SetPolicyRequest(NodeManagerDomainId domainId, bool policyEnabled, byte policyId, 
            NodeManagerPolicyType policyType, NodeManagerPolicyAction action, 
            NodeManagerPowerCorrection correction, bool persistent, NodeManagerPolicyExceptionAction exceptionAction,
            ushort targetLimit, uint correctionTime, ushort triggerLimit, ushort statisticReporting)
        {
            this.domainId = (byte)domainId;

            if(policyEnabled)
                this.domainId = (byte)(this.domainId | 0x10);
       
            this.policyId = policyId;

            this.policyType = (byte)policyType;

            // policy action.
            if(action == NodeManagerPolicyAction.Add)
                this.policyType = (byte)(this.policyType | 0x10);

            // correction action
            this.policyType = (byte)(this.policyType | (byte)correction);

            // persistence
            if(!persistent)
                this.policyType = (byte)(this.policyType | 0x80);

            // exception action if policy cannot be kept within correction time.
            this.exceptionAction = (byte)exceptionAction;

            // target limit.
            this.targetLimit = targetLimit;

            // bits 8:15 are reserved  BootTimePolicy
            if (policyType == NodeManagerPolicyType.BootTimePolicy)
                this.targetLimit = (ushort)(this.targetLimit & 0xff);

            // set the correction time.
            this.correctionTime = correctionTime;

            // correction time override for boot time policy
            if (policyType == NodeManagerPolicyType.BootTimePolicy)
                this.correctionTime = 0;

            // trigger limit
            this.triggerLimit = triggerLimit;

            // trigger limit override for boot time policy
            if (policyType == NodeManagerPolicyType.BootTimePolicy)
                this.triggerLimit = 0;

            // statistics reporting
            this.statisticReporting = statisticReporting;

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
        /// Domain Id
        /// </summary>
        [NodeManagerMessageData(3)]
        public byte DomainId
        {
            get { return domainId; }
        }

        /// <summary>
        /// Policy Id
        /// </summary>
        [NodeManagerMessageData(4)]
        public byte PolicyId
        {
            get { return policyId; }
        }

        /// <summary>
        /// Policy Type [0:3] Action [4] Correction [5:6] Persistence [7]
        /// </summary>
        [NodeManagerMessageData(5)]
        public byte PolicyType
        {
            get { return policyType; }
        }

        /// <summary>
        /// Node Manager Exception Action.
        /// </summary>
        [NodeManagerMessageData(6)]
        public byte ExceptionAction
        {
            get { return exceptionAction; }
        }

        /// <summary>
        /// Target limit depends on Policy Type [0:3]
        ///     0, 1, 3:    Power Limt in Watts
        ///     2:          Thorttle level of platform in % [whereby 100% is maximum throttling]
        ///     4:          Power Profile during boot mode.       
        /// </summary>
        [NodeManagerMessageData(7)]
        public ushort TargetLimit
        {
            get { return targetLimit; }
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
            get { return correctionTime; }
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
            get { return triggerLimit; }
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
            get { return statisticReporting; }
        }

    }
}
