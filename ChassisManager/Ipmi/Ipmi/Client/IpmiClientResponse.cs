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
    using System.Text;
    using System.Collections;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public abstract class ResponseBase
    {
        // ipmi completion code
        private byte _completionCode;

        /// <summary>
        /// Completion Code
        /// </summary>
        public byte CompletionCode
        {
            get { return this._completionCode; }
            internal set { this._completionCode = value; }
        }

        internal abstract void SetParamaters(byte[] param);
    
    }

    #region IPMI Commands

    /// <summary>
    /// Response to Device Guid Command
    /// </summary>
    public class DeviceGuid : ResponseBase
    {
        private Guid guid;

        public DeviceGuid(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        public DeviceGuid(byte completionCode, Guid guid)
        {
            base.CompletionCode = completionCode;
            this.Guid = guid;
        }

        internal override void SetParamaters(byte[] param)
        {
            if (base.CompletionCode == 0x00)
            {
                this.Guid = new Guid(param);
            }
        
        }

        /// <summary>
        /// Device Guid
        /// </summary>
        public Guid Guid
        {
            get { return this.guid; }
            private set { this.guid = value; }
        }

    }

    /// <summary>
    /// Response to BMC Firmware Command
    /// </summary>
    public class BmcFirmware : ResponseBase
    {
        private string _firmware;

        public BmcFirmware(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0x00)
            {
                // major firmware version [6:0] + . +  minor firmware version
                this.SetParamaters(data[0], data[1]);
            }
        }

        internal void SetParamaters(byte major, byte minor)
        {
            if (base.CompletionCode == 0x00)
            {
                // major firmware version [6:0] + . +  minor firmware version
                this.Firmware = (major & 0x7F).ToString("X2", CultureInfo.InvariantCulture) +
                    Convert.ToChar(0x2E) + (minor).ToString("X2", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// BMC Firmware
        /// </summary>
        public string Firmware
        {
            get { return this._firmware; }
            set { this._firmware = value; }
        }
    }

    /// <summary>
    /// Get Device Id command response
    /// </summary>
    public class BmcDeviceId : ResponseBase
    {
        private string firmware;

        private uint manufacturerId;

        private ushort productId;

        public BmcDeviceId(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetDeviceIdResponse response =
                (GetDeviceIdResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetDeviceIdResponse));

            this.SetParamaters(response.MajorFirmware, response.MinorFirmware, response.AuxFwVer,
                response.ManufacturerId, response.ProductId);
        }

        internal void SetParamaters(byte major, byte minor, byte[] auxFwVer, byte[] manufacturer, byte[] productId)
        {
            // major firmware version [6:0] + . +  minor firmware version (BCD encoded)
            this.Firmware = (major & 0x7F).ToString("d2", CultureInfo.InvariantCulture) +
                "." + ((minor & 0xF0) >> 4).ToString("d", CultureInfo.InvariantCulture) +
                (minor & 0xF).ToString("d", CultureInfo.InvariantCulture);

            // Add auxiliary FW version if it is non-zero
            if (auxFwVer != null)
            {
                if (auxFwVer[0] != 0)
                {
                    this.Firmware += "." + auxFwVer[0].ToString("d2", CultureInfo.InvariantCulture);
                }
            }

                // convert 3 byte oem id into integer using bitwise operation
            this.ManufacturerId = (uint)(manufacturer[0] + (manufacturer[1] << 8) + (manufacturer[2] << 16));

                this.ProductId = BitConverter.ToUInt16(productId, 0);       
        }

        /// <summary>
        /// BMC Firmware
        /// </summary>
        public string Firmware
        {
            get { return this.firmware; }
            private set { this.firmware = value; }
        }

        /// <summary>
        /// BMC ManufacturerId
        /// </summary>
        public uint ManufacturerId
        {
            get { return this.manufacturerId; }
            private set { this.manufacturerId = value; }
        }

        /// <summary>
        /// BMC Product Id
        /// </summary>
        public ushort ProductId
        {
            get { return this.productId; }
            private set { this.productId = value; }
        }
    }

    /// <summary>
    /// Response to System Power State
    /// </summary>
    public class SystemPowerState : ResponseBase
    {
        internal IpmiPowerState state;

        internal PowerRestoreOption powerOnPolicy;

        public SystemPowerState(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        { }

        /// <summary>
        /// System Power State
        /// </summary>
        public IpmiPowerState PowerState
        {
            get { return state; }
        }

        public PowerRestoreOption PowerOnPolicy
        {
            get { return this.powerOnPolicy; }
        }
    }

    /// <summary>
    /// Response to Get Chassis Status command
    /// </summary>
    public class SystemStatus : ResponseBase
    {
        // current power state
        private IpmiPowerState powerstate;

        // AC power restore policy
        private PowerRestoreOption powerOnPolicy;

        // previous power event cause
        private PowerEvent lastPowerEvent;

        // identity led supported (default = false)
        private bool identitySupported = false;
        
        // identity led state
        private IdentityState identityState;

        public SystemStatus(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetChassisStatusResponse response =
                (GetChassisStatusResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetChassisStatusResponse));

            if (base.CompletionCode == 0x00)
                this.SetParamaters(response.CurrentPowerState, response.LastPowerEvent, response.MiscellaneousChassisState);
        }

        internal void SetParamaters(byte currentPowerState, byte lastEvent, byte miscState)
        {
            if (base.CompletionCode == 0)
            {

                #region Power State

                // [0] Power is on, 1 = On, 0 = Off
                byte state = Convert.ToByte((currentPowerState & 0x01));

                // Translate the current power state into an enumeration.
                switch (state)
                {
                    case 0x00:
                        this.powerstate = IpmiPowerState.Off;
                        break;
                    case 0x01:
                        this.powerstate = IpmiPowerState.On;
                        break;
                    default:
                        this.powerstate = IpmiPowerState.Invalid;
                        break;
                }

                #endregion

                #region Power Policy

                state = Convert.ToByte((currentPowerState & 0x60) >> 5);

                // Translate the state into Power on Policy.
                switch (state)
                {
                    case 0x00:
                        this.powerOnPolicy = PowerRestoreOption.StayOff;
                        break;
                    case 0x01:
                        this.powerOnPolicy = PowerRestoreOption.PreviousState;
                        break;
                    case 0x02:
                        this.powerOnPolicy = PowerRestoreOption.AlwaysPowerUp;
                        break;
                    default:
                        this.powerOnPolicy = PowerRestoreOption.Unknown;
                        break;
                }

                #endregion

                #region Power Fault

                // [7:5] -  reserved
                // [4]   -   1b = last ‘Power is on’ state was entered via IPMI command 
                // [3]   -   1b = last power downcaused by power fault
                // [2]   -   1b = last power down caused by a power interlockbeing activated 
                // [1]   -   1b = last power down caused by a Power overload
                // [0]   -   1b = AC failed
                state = Convert.ToByte((lastEvent & 0x1F));

                switch (state)
                {
                    case 0x00:
                        this.lastPowerEvent = PowerEvent.ACfailed;
                        break;
                    case 0x01:
                        this.lastPowerEvent = PowerEvent.PowerOverload;
                        break;
                    case 0x02:
                        this.lastPowerEvent = PowerEvent.PowerInterlockActive;
                        break;
                    case 0x03:
                        this.lastPowerEvent = PowerEvent.PowerFault;
                        break;
                    case 0x04:
                        this.lastPowerEvent = PowerEvent.IpmiSetState;
                        break;
                    default:
                        this.lastPowerEvent = PowerEvent.Unknown;
                        break;
                }

                #endregion

                #region Identity LED

                // [7:4] -  reserved
                // [6] -    1b = Chassis Identify command and state info supported (Optional)
                //          0b = Chassis Identify command support unspecified via this 
                //          command.
                byte identitySupport = Convert.ToByte((miscState & 0x40) >> 6);

                if (identitySupport == 0x01)
                    this.identitySupported = true;

                // [5:4] -  Chassis Identify State.  Mandatory when bit [6] = 1b, reserved (return 
                // as 00b) otherwise.Returns the present chassis identify state. Refer to 
                // the Chassis Identify command for more info.
                // 00b = chassis identify state = Off
                // 01b = chassis identify state = Temporary (timed) On
                // 10b = chassis identify state = Indefinite On
                // 11b = reserved

                byte Identity = Convert.ToByte((miscState & 0x30) >> 4);

                switch (Identity)
                {
                    case 0x00:
                        this.identityState = IdentityState.Off;
                        break;
                    case 0x01:
                        this.identityState = IdentityState.TemporaryOn;
                        break;
                    case 0x02:
                        this.identityState = IdentityState.On;
                        break;
                    default:
                        this.identityState = IdentityState.Unknown;
                        break;
                }

                #endregion

            }
            else
            {
                this.powerstate = IpmiPowerState.Invalid;
            }          
        }

        /// <summary>
        /// System Power State
        /// </summary>
        public IpmiPowerState PowerState
        {
            get { return this.powerstate; }
            internal set { this.powerstate = value; }
        }

        /// <summary>
        /// AC Power Restore Policy
        /// </summary>
        public PowerRestoreOption PowerOnPolicy
        {
            get { return this.powerOnPolicy; }
        }

        /// <summary>
        /// Previous Power Down
        /// </summary>
        public PowerEvent LastPowerEvent
        {
            get { return this.lastPowerEvent; }
        }

        /// <summary>
        /// Chassis Identity LED State Supported
        /// </summary>
        public bool IdentitySupported
        {
            get { return this.identitySupported; }
        }

        /// <summary>
        /// Chassis Identity LED State
        /// </summary>
        public IdentityState IdentityState
        {
            get { return this.identityState; }
        }
    }

    /// <summary>
    /// Resposne to Power On Hours Command
    /// </summary>
    public class PowerOnHours : ResponseBase
    {
        private int _hours;

        public PowerOnHours(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
            {
                this.Hours = BitConverter.ToInt32(data, 0);
            }
        }

        /// <summary>
        /// Power On Hours
        /// </summary>
        public int Hours
        {
            get { return this._hours; }
            private set { this._hours = value; }
        }

    }

    /// <summary>
    /// Properties in the Set Power Restore Policy command response
    /// </summary>
    public class PowerRestorePolicy : ResponseBase
    {
        internal List<PowerRestoreOption> _restorePolicy = new List<PowerRestoreOption>();

        /// <summary>
        /// Properties in the Set Power Restore Policy command response
        /// </summary>
        public PowerRestorePolicy(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0x00)
            {
                if ((data[0] & 0x01) == 0x01)
                    this._restorePolicy.Add(PowerRestoreOption.StayOff);

                if ((data[0] & 0x02) == 0x02)
                    this._restorePolicy.Add(PowerRestoreOption.PreviousState);

                if ((data[0] & 0x04) == 0x04)
                    this._restorePolicy.Add(PowerRestoreOption.AlwaysPowerUp);
            }
        }

        /// <summary>
        /// Power Restore Policy
        /// </summary>
        public List<PowerRestoreOption> SupportedOptions
        {
            get { return this._restorePolicy; }
        }

    }

    /// <summary>
    /// Response to Get Channel Authentication Capabilities Command
    /// </summary>
    public class ChannelAuthenticationCapabilities : ResponseBase
    {
        /// <summary>
        /// Current Channel Number
        /// </summary>
        private byte _channelNumber;

        /// <summary>
        /// 1b = IPMI v2.0+ extended capabilities available. See Extended 
        /// Capabilities field, below. 
        /// 0b = IPMI v1.5 support only.
        /// </summary>
        private IpmiVersion _authentication;

        /// <summary>
        /// [5] -  OEM proprietary (per OEM identified by the IANA OEM ID in 
        ///        the RMCP Ping Response) 
        /// [4] -  straight password / key 
        /// [3] -  reserved 
        /// [2] -  MD5 
        /// [1] -  MD2 
        /// [0] -  none  
        /// </summary>
        private List<AuthenticationType> _authTypes = new List<AuthenticationType>();

        /// <summary>
        ///  false = KgAllZero
        ///  true  = KgNoneZero
        /// </summary>
        private bool _kGStatus;

        /// <summary>
        /// Per Message Authentication
        /// </summary>
        private bool _messageAuthentication;

        /// <summary>
        /// User Level Authentication
        /// </summary>
        private bool _userLevelAuthentication;

        /// <summary>
        /// Non Null User Id Enabled.
        /// </summary>
        private bool _nonNullUserId;

        /// <summary>
        /// Nul User Id
        /// </summary>
        private bool _nullUserId;

        /// <summary>
        /// Anonymous Login
        /// </summary>
        private bool _anonymousLogon;

        /// <summary>
        /// OEM Id
        /// </summary>
        private int _oemId;

        /// <summary>
        /// Auxiliary Data
        /// </summary>
        private byte _auxiliaryData;

        /// <summary>
        /// Ipmi Channel Support
        /// </summary>
        private List<IpmiVersion> _ChannelSupport = new List<IpmiVersion>(2);

        /// <summary>
        /// Set Ipmi Authentication Type
        /// </summary>
        private void SetAuthType(IpmiVersion version)
        {
            this._authentication = version;
        }

        /// <summary>
        /// Add Ipmi Authentication Type
        /// </summary>
        private void AddAuthType(AuthenticationType authType)
        {
            this._authTypes.Add(authType);
        }

        /// <summary>
        /// Set Non Null & Null User Loging Flags
        /// </summary>
        private void SetUserId(byte non_null_user, byte null_UserId, byte anonymous_Login)
        {
            this._nonNullUserId = Convert.ToBoolean(non_null_user);
            this._nullUserId = Convert.ToBoolean(null_UserId);
            this._anonymousLogon = Convert.ToBoolean(anonymous_Login);
        }

        /// <summary>
        /// Add Ipmi Channel Protocol Suppor
        /// </summary>
        private void AddChannelSupport(IpmiVersion support)
        {
            this._ChannelSupport.Add(support);
        }

        /// <summary>
        /// User Level Authentication
        /// </summary>
        private void SetMessageAuth(bool userLevelAuthentication,
            bool perMessageAuthenticatoin, bool kgStatus)
        {
            this._kGStatus = kgStatus;
            this._messageAuthentication = perMessageAuthenticatoin;
            this._userLevelAuthentication = userLevelAuthentication;
        }

        /// <summary>
        /// Set Oem Payload data
        /// </summary>
        private void SetOemData(int oemId, byte auxiliary)
        {
            this._oemId = oemId;
            this._auxiliaryData = auxiliary;
        }

        public ChannelAuthenticationCapabilities(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetChannelAuthenticationCapabilitiesResponse response =
            (GetChannelAuthenticationCapabilitiesResponse)IpmiSharedFunc.ConvertResponse(data,
                typeof(GetChannelAuthenticationCapabilitiesResponse));

            this.SetParamaters(response.ChannelNumber, response.AuthenticationTypeSupport1,
                    response.AuthenticationTypeSupport2, response.ExtendedCapabilities,
                    response.OemId, response.OemData);
        }

        internal void SetParamaters(byte channel, byte authSupOne, byte authSupTwo,
            byte extCapabilities, byte[] oemId, byte oemData)
        {
            // set the current channel number
            this._channelNumber = channel;

            if (base.CompletionCode == 0)
            {
                // Get authentication support message field 1 and split
                byte[] authSupport1 = IpmiSharedFunc.ByteSplit(authSupOne, new int[3] { 7, 6, 0 });

                // [0] bmc supports v1.5 only
                // [1] bmc v2.0 capabilities available
                if (authSupport1[0] == 0x00)
                {
                    this.SetAuthType(IpmiVersion.V15);
                }
                else
                {
                    this.SetAuthType(IpmiVersion.V20);
                }

                // Convert bmc auth types byte to an array for BitArray breakdown.
                byte[] bmcAuthTypes = new byte[1];
                bmcAuthTypes[0] = authSupport1[2];
                // bmc supported authentication types
                BitArray authenticatonTypes = new BitArray(bmcAuthTypes);
                // only validate client supported authentication types
                // propose an auth type in order of client perference: (MD5, straight, None).

                if (authenticatonTypes[4]) // Straight password
                    this.AddAuthType(AuthenticationType.Straight);
                if (authenticatonTypes[0]) // None
                    this.AddAuthType(AuthenticationType.None);

                // Get authentication support message field 2 and split
                byte[] authSupport2 = IpmiSharedFunc.ByteSplit(authSupTwo, new int[5] { 7, 5, 4, 3, 0 });
                bool kgStatus = false;
                bool perMessage = false;
                bool userAuth = false;

                // [0] one key login required
                // [1] two key login required
                if (authSupport2[1] == 0x01)
                {
                    kgStatus = true;
                }
                // Per Message Authentication
                if (authSupport2[2] == 0x01)
                {
                    perMessage = true;
                }
                // User Authentication
                if (authSupport2[3] == 0x01)
                {
                    userAuth = true;
                }

                // set authentication
                this.SetMessageAuth(userAuth, perMessage, kgStatus);

                // [2] - 1b = Non-null usernames enabled. (One or more users are enabled that have non-null usernames). 
                // [1] - 1b = Null usernames enabled (One or more users that have a null username, but non-null password, are presently enabled) 
                // [0] - 1b = Anonymous Login enabled (A user that has a null username and null password is presently enabled) 
                byte anonymous_Login = (byte)(authSupport2[4] & 0x01);
                byte null_User = (byte)(authSupport2[4] & 0x02);
                byte non_nullUsers = (byte)(authSupport2[4] & 0x04);

                this.SetUserId(non_nullUsers, null_User, anonymous_Login);

                // Get authentication support message field 3 and split
                // [7:2] reserved
                // [1] 1b = supports ipmi v2 connections
                // [0] 1b = supprots ipmi v1.5 connections
                byte[] authSupport3 = IpmiSharedFunc.ByteSplit(extCapabilities, new int[3] { 2, 1, 0 });
                if (authSupport3[1] == 0x01)
                {
                    this.AddChannelSupport(IpmiVersion.V20);
                }

                if (authSupport3[2] == 0x01)
                {
                    this.AddChannelSupport(IpmiVersion.V15);
                }

                // convert 3 byte oem id into integer using bitwise operation
                int oem = ((oemId[0] << 0) + (oemId[1] << 8) + (oemId[2] << 16));

                byte auxiliaryData = oemData;

                this.SetOemData(oem, auxiliaryData);
            }
        }

        /// <summary>
        /// 1b = IPMI v2.0+ extended capabilities available. See Extended 
        /// Capabilities field, below. 
        /// 0b = IPMI v1.5 support only.
        /// </summary>
        public IpmiVersion Authentication
        {
            get { return this._authentication; }
        }

        /// <summary>
        /// [5] -  OEM proprietary (per OEM identified by the IANA OEM ID in 
        ///        the RMCP Ping Response) 
        /// [4] -  straight password / key 
        /// [3] -  reserved 
        /// [2] -  MD5 
        /// [1] -  MD2 
        /// [0] -  none  
        /// </summary>
        public List<AuthenticationType> AuthTypes
        {
            get { return this._authTypes; }
        }

        /// <summary>
        ///  false = KgAllZero
        ///  true  = KgNoneZero
        /// </summary>
        public bool KGStatus
        {
            get { return this._kGStatus; }
        }

        /// <summary>
        /// Per Message Authentication
        /// </summary>
        public bool MessageAuthentication
        {
            get { return this._messageAuthentication; }
        }

        /// <summary>
        /// User Level Authentication
        /// </summary>
        public bool UserLevelAuthentication
        {
            get { return this._userLevelAuthentication; }
        }

        /// <summary>
        /// Non Null User Id Enabled.
        /// </summary>
        public bool NonNullUserId
        {
            get { return this._nonNullUserId; }
        }

        /// <summary>
        /// Null User Id
        /// </summary>
        public bool NullUserId
        {
            get { return this._nullUserId; }
        }

        /// <summary>
        /// Anonymous Login
        /// </summary>
        public bool AnonymousLogOn
        {
            get { return this._anonymousLogon; }
        }

        /// <summary>
        /// Ipmi Channel Support
        /// </summary>
        public List<IpmiVersion> ChannelSupport
        {
            get { return this._ChannelSupport; }
        }

        /// <summary>
        /// OEM Id
        /// </summary>
        public int OemId
        {
            get { return this._oemId; }
        }

        /// <summary>
        /// Auxiliary Data
        /// </summary>
        public byte AuxiliaryData
        {
            get { return this._auxiliaryData; }
        }

        /// <summary>
        /// Returns the Current Channel Number
        /// </summary>
        public byte ChannelNumber
        {
            get { return this._channelNumber; }
        }

    }

    /// <summary>
    /// Response to Get Channel Info Command
    /// </summary>
    public class ChannelInfo : ResponseBase
    {
        /// <summary>
        /// Channel number.
        /// </summary>
        private byte _channelNumber;

        /// <summary>
        /// Channel Medium.
        /// </summary>
        private byte _channelMedium;

        /// <summary>
        /// Channel Protocol
        /// </summary>
        private byte _channelProtocol;

        /// <summary>
        /// Number of Sessions Supported
        /// </summary>
        private byte _numberOfSessions;


        /// <summary>
        /// Channel Session Support
        ///     00b = channel is session-less
        ///     01b = channel is single-session
        ///     10b = channel is multi-session
        ///     11b = channel is session-based
        /// </summary>
        private byte _channelSessionSupport;

        public ChannelInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetChannelInfoResponse response =
            (GetChannelInfoResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetChannelInfoResponse));

            if (response.CompletionCode == 0)
            {
                this.SetParamaters(
                                    response.ChannelNumber,
                                    response.ChannelMedium,
                                    response.ChannelProtocol,
                                    response.ChannelSessionSupport,
                                    response.NumberOfSessions);
            }

        }

        internal void SetParamaters(byte channelNumber, byte channelMedium,
            byte channelProtocol, byte channelSession, byte sessions)
        {
            this.ChannelNumber = channelNumber;
            this.ChannelMedium = channelMedium;
            this.ChannelProtocol = channelProtocol;
            this.ChannelSessionSupport = channelSession;
            this.NumberOfSessions = sessions;
        }

        /// <summary>
        /// Gets and sets the Actual Channel number.
        /// </summary>
        /// <value>Channel number.</value>
        public byte ChannelNumber
        {
            get { return this._channelNumber; }
            private set { this._channelNumber = value; }
        }

        /// <summary>
        /// Channel Medium.
        /// </summary>
        public byte ChannelMedium
        {
            get { return this._channelMedium; }
            private set { this._channelMedium = value; }
        }

        /// <summary>
        /// Channel Protocol
        /// </summary>
        public byte ChannelProtocol
        {
            get { return this._channelProtocol; }
            private set { this._channelProtocol = value; }
        }

        /// <summary>
        /// Channel Session Support
        ///     00b = channel is session-less
        ///     01b = channel is single-session
        ///     10b = channel is multi-session
        ///     11b = channel is session-based
        /// </summary>
        public byte ChannelSessionSupport
        {
            get { return (byte)(this._channelSessionSupport); }
            private set { this._channelSessionSupport = value; }
        }

        /// <summary>
        /// Number of sessions
        /// </summary>
        public byte NumberOfSessions
        {
            get { return (byte)(this._numberOfSessions); }
            private set { this._numberOfSessions = value; }
        }

    }

    /// <summary>
    /// Properties in the Get/Set Serial Mux command response
    /// </summary>
    public class SerialMuxSwitch : ResponseBase
    {
        /// <summary>
        /// [7] -  	0b = requests to switch mux to system are allowed 
        ///         1b = requests to switch mux to system are blocked 
        /// </summary>
        private bool _muxSwitchAllowed = false;

        /// <summary>
        /// [6] -  	0b = requests to switch mux to BMC are allowed 
        ///         1b = requests to switch mux to BMC are blocked 
        /// </summary>
        private bool _requestToBmcAllowed = false;

        /// <summary>
        /// [3] -  	0b = no alert presently in progress 
        ///         1b = alert in progress on channel 
        /// </summary>
        private bool _alertInProgress = false;

        /// <summary>
        /// [2] -  	0b = no IPMI or OEM messaging presently active on channel 
        ///         1b = IPMI or OEM messaging session active on channel 
        /// </summary>
        private bool _messagingActive = false;

        /// <summary>
        /// [1] -  	0b = request was rejected 
        ///         1b = request was accepted (see note, below) or switch was forced 
        /// </summary>
        private bool _requestAccepted = false;

        /// <summary>
        /// [0] -  	0b = mux is set to system (system can transmit and receive) 
        ///         1b = mux is set to BMC  (BMC can transmit. System can neither 
        ///         transmit nor receive) 
        /// </summary>
        private bool _muxSetToSystem = false;

        /// <summary>
        /// Properties in the Get/Set Serial Mux command response
        /// </summary>
        public SerialMuxSwitch(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            SetSerialMuxResponse response =
            (SetSerialMuxResponse)IpmiSharedFunc.ConvertResponse(data, typeof(SetSerialMuxResponse));

            if (response.CompletionCode == 0)
            {
                response.GetMux();

                this.SetParamaters(
                        response.AlertInProgress,
                        response.MessagingActive,
                        response.MuxSetToSystem,
                        response.MuxSwitchAllowed,
                        response.RequestAccepted,
                        response.RequestToBmcAllowed
                        );
            }
        }

        internal void SetParamaters(bool alertInProgress, bool messagingActive, bool muxSetToSystem,
            bool muxSwitchAllowed, bool requestAccepted, bool requestToBmcAllowed)
        {
            this._alertInProgress = alertInProgress;
            this._messagingActive = messagingActive;
            this._muxSetToSystem = muxSetToSystem;
            this._muxSwitchAllowed = muxSwitchAllowed;
            this._requestAccepted = requestAccepted;
            this._requestToBmcAllowed = requestToBmcAllowed;
        }

        /// <summary>
        /// false = requests to switch mux to system are allowed 
        /// true = requests to switch mux to system are blocked 
        /// </summary>
        public bool MuxSwitchAllowed
        {
            get { return this._muxSwitchAllowed; }
        }

        /// <summary>
        /// false = requests to switch mux to BMC are allowed 
        /// true =  requests to switch mux to BMC are blocked 
        /// </summary>
        public bool RequestToBmcAllowed
        {
            get { return this._requestToBmcAllowed; }
        }

        /// <summary>
        /// false = no alert presently in progress 
        /// true =  alert in progress on channel 
        /// </summary>
        public bool AlertInProgress
        {
            get { return this._alertInProgress; }
        }

        /// <summary>
        /// false = no IPMI or OEM messaging presently active on channel 
        /// true =  IPMI or OEM messaging session active on channel 
        /// </summary>
        public bool MessagingActive
        {
            get { return this._messagingActive; }
        }

        /// <summary>
        /// false = request was rejected 
        /// true =  request was accepted (see note, below) or switch was forced 
        /// </summary>
        public bool RequestAccepted
        {
            get { return this._requestAccepted; }
        }

        /// <summary>
        /// false = mux is set to system (system can transmit and receive) 
        /// true =  mux is set to BMC  (BMC can transmit. System can neither 
        ///         transmit nor receive) 
        /// </summary>
        public bool MuxSetToSystem
        {
            get { return this._muxSetToSystem; }
        }
    }

    /// <summary>
    /// Properties in the Master Write Read command response
    /// </summary>
    public class SmbusWriteRead : ResponseBase
    {
        // ReadWrite Response
        string message = string.Empty;
        byte[] rawData = {};

        /// <summary>
        /// Properties in the Master Write Read command response
        /// </summary>
        public SmbusWriteRead(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
            {
                this.message = IpmiSharedFunc.ByteArrayToHexString(data);
                this.rawData = data;
            }
            else
            {
                this.message = string.Empty;
                this.rawData = new byte[0];
            }
        }

        /// <summary>
        /// Message Response Data
        /// </summary>
        public string MessageData
        {
            get { return this.message; }
        }

        /// <summary>
        /// Message Data in Byte Array
        /// </summary>
        internal byte[] RawData
        {
            get { return this.rawData; }
    }
    }

    /// <summary>
    /// Resposne to Get Next Boot Command
    /// </summary>
    public class NextBoot : ResponseBase
    {
        private BootType _bootDevice = BootType.Unknown;

        public NextBoot(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
            {
                byte flags = (byte)(((byte)(data[1] & 0x3C)) >> 2);

                switch (flags)
                {
                    case 0:
                        _bootDevice = BootType.NoOverride;
                        break;
                    case 1:
                        _bootDevice = BootType.ForcePxe;
                        break;
                    case 2:
                        _bootDevice = BootType.ForceDefaultHdd;
                        break;
                    case 3:
                        _bootDevice = BootType.ForceDefaultHddSafeMode;
                        break;
                    case 4:
                        _bootDevice = BootType.ForceDefaultDiagPartition;
                        break;
                    case 5:
                        _bootDevice = BootType.ForceDefaultDvd;
                        break;
                    case 6:
                        _bootDevice = BootType.ForceIntoBiosSetup;
                        break;
                    case 15:
                        _bootDevice = BootType.ForceFloppyOrRemovable;
                        break;
                    default:
                        _bootDevice = BootType.Unknown;
                        break;
                }
            }

        }

        /// <summary>
        /// Boot Device
        /// </summary>
        public BootType BootDevice
        {
            get { return this._bootDevice; }
            internal set { this._bootDevice = value; }
        }

    }

    #endregion

    #region User

    /// <summary>
    /// Response to User Privilege Command
    /// </summary>
    public class UserPrivilege : ResponseBase
    {
        private PrivilegeLevel _privilege;

        public UserPrivilege(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            Privilege = (PrivilegeLevel)Convert.ToInt32(data[0]);
        }

        /// <summary>
        /// Privilege Level
        /// </summary>
        public PrivilegeLevel Privilege
        {
            get { return this._privilege; }
            private set { this._privilege = value; }
        }

    }

    /// <summary>
    /// Response to User Name Command
    /// </summary>
    public class UserName : ResponseBase
    {
        private string _userName;

        public UserName(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            // Trim padded '\0' from User Name
            this.TextName = System.Text.ASCIIEncoding.ASCII.GetString(data).TrimEnd('\0');
        }

        /// <summary>
        /// User Name
        /// </summary>
        public string TextName
        {
            get { return this._userName; }
            set { this._userName = value; }
        }

    }

    
    #endregion

    #region DCMI Power

    /// <summary>
    /// Properties in the Get Power Limit DCMI command response
    /// </summary>
    public class PowerLimit : ResponseBase
    {
        private bool _activeLimit;
        private short _limitValue;
        private short _samplingPeriod;
        private byte _correctionAction;
        private int _rawCorrectionTime;
        private TimeSpan _correctionTime;

        public PowerLimit(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Convert Ipmi Response data into the Response object
        /// </summary>
        internal override void SetParamaters(byte[] data)
        {
            GetDcmiPowerLimitResponse response = 
                (GetDcmiPowerLimitResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetDcmiPowerLimitResponse));

            this.SetParamaters(response.PowerLimit, response.SamplingPeriod, response.ExceptionActions, response.CorrectionTime);
        }

        /// <summary>
        /// Set the Class Paramaters
        /// </summary>
        internal void SetParamaters(byte[] powerlimit, byte[] samplingPeriod, byte exceptionActions, byte[] correctionTime)
        {
            if (this.CompletionCode == 0 || this.CompletionCode == 0x80)
            {
                // power limit in watts
                this._limitValue = BitConverter.ToInt16(powerlimit, 0);

                // sampling period in seconds
                this._samplingPeriod = BitConverter.ToInt16(samplingPeriod, 0);

                // exception action (actions, taken if the Power limit exceeded and cannot be 
                // controlled within the correction time limit)
                this._correctionAction = Convert.ToByte(exceptionActions);

                // time span of correction time.  value given in ms, convert to days, hours, minutes, seconds, milliseconds
                this._correctionTime = new TimeSpan(0, 0, 0, 0, BitConverter.ToInt16(correctionTime, 0));

                this._rawCorrectionTime = BitConverter.ToInt32(correctionTime, 0);

                if (this.CompletionCode == 0x80)
                {
                    // 0x80 indicates there is no limit applied
                    this._activeLimit = false;

                    // swtich completion code to zero. we do not want
                    // to report No Active Limit Set as failure.
                    base.CompletionCode = 0;
                }
                else
                {
                    this._activeLimit = true;
                }
            }       
        }

        /// <summary>
        /// Indicates whether a system power limit is active
        /// </summary>
        public bool ActiveLimit
        {
            get { return this._activeLimit; }
        }

        /// <summary>
        /// Provides the power limit reading in watts
        /// </summary>
        public short LimitValue
        {
            get { return this._limitValue; }
        }

        /// <summary>
        /// The system statistics sampling period
        /// </summary>
        public short SamplingPeriod
        {
            get { return this._samplingPeriod; }
        }

        /// <summary>
        /// The time allowed for the system to enfoce a power limit
        /// before corrective action is taken
        /// </summary>
        public TimeSpan CorrectionTime
        {
            get { return this._correctionTime; }
        }

        /// <summary>
        /// The time allowed for the system to enfoce a power limit
        /// before corrective action is taken
        /// </summary>
        public int RawCorrectionTime
        {
            get { return this._rawCorrectionTime; }
        }

        /// <summary>
        /// Action taken should the system fail to enfoce a power limit
        /// within the correction time:
        /// 0    = No Action
        /// 1    = Shutdown system
        /// 2-10 = OEM defined actions
        /// </summary>
        public byte CorrectionAction
        {
            get { return this._correctionAction; }
        }
    }

    /// <summary>
    /// Properties in the Activate Power Limit DCMI command.
    /// </summary>
    public class ActivePowerLimit : ResponseBase
    {
        private bool limitSet;

        /// <summary>
        /// Properties in the Activate Power Limit DCMI command.
        /// </summary>
        public ActivePowerLimit(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
                this.limitSet = true;
            else
                this.limitSet = false;

        }

        /// <summary>
        /// Indicates the power limit has been succesfully set
        /// </summary>
        public bool LimitSet
        {
            get { return this.limitSet; }
        }
    }

    /// <summary>
    /// Properties in the Get Power Reading DCMI command response
    /// </summary>
    public class PowerReading : ResponseBase
    {
        private bool powerSupport;
        private short present;
        private short maximum;
        private short minimum;
        private short average;
        private short timeUnit;
        private int timeNumber;
        private uint statistics;

        /// <summary>
        /// Properties in the Get Power Reading DCMI command response
        /// </summary>
        public PowerReading(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
            {
                // system does support power readings
                this.PowerSupport = true;

                // Current power reading (DCMI spec)
                this.present = BitConverter.ToInt16(data, 0);
                // Minimum power reading (DCMI spec)
                this.minimum = BitConverter.ToInt16(data, 2);
                // Maximum power reading (DCMI spec)
                this.maximum = BitConverter.ToInt16(data, 4);
                // Average power reading (DCMI spec)
                this.average = BitConverter.ToInt16(data, 6);

                // No rolling averages are supplied with standard power statistics 
                // instead the statistics reporting time period is converted to 
                // minutes and used in place of the rolling average.
                this.timeNumber = unchecked(((BitConverter.ToInt32(data, 8)) / 1000) / 60);

                // set time sample to minutes
                this.timeUnit = 1;

                this.statistics = BitConverter.ToUInt32(data, 8);
            }
        }

        /// <summary>
        /// Indicates whether power readings are supported
        /// on the platform
        /// </summary>
        public bool PowerSupport
        {
            get { return this.powerSupport; }
            internal set { this.powerSupport = value; }
        }

        /// <summary>
        /// Present system level power reading in watts
        /// </summary>
        public short Present
        {
            get { return this.present; }
        }

        /// <summary>
        /// Maximum system level power reading in watts
        /// over the given sampling period
        /// </summary>
        public short Maximum
        {
            get { return this.maximum; }
        }

        /// <summary>
        /// Minimum system level power reading in watts
        /// over the given sampling period
        /// </summary>
        public short Minimum
        {
            get { return this.minimum; }
        }

        /// <summary>
        /// Average system level power reading in watts
        /// over the given sampling period
        /// </summary>
        public short Average
        {
            get { return this.average; }
        }

        /// <summary>
        /// Sampling time unit values:
        /// 0 = Seconds
        /// 1 = Minutes
        /// 2 = Hours
        /// 4 = Days
        /// </summary>
        public short TimeUnit
        {
            get { return this.timeUnit; }
            internal set { this.timeUnit = value; }
        }

        /// <summary>
        /// Samping time number is the total count of time units
        /// in the samping period
        /// </summary>
        public int TimeNumber
        {
            get { return this.timeNumber; }
            internal set { this.timeNumber = value; }
        }

        /// <summary>
        /// Raw Reading Statistics
        /// </summary>
        public uint Statistics
        {
            get { return this.statistics; }
        }

    }

    #endregion

    #region FRU

    /// <summary>
    /// Response to Write Fru Data Command.
    /// </summary>
    public class WriteFruDevice : ResponseBase
    {
        private byte bytesWritten;

        /// <summary>
        /// Write Fru Data Command Response
        /// </summary>
        public WriteFruDevice(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            if (base.CompletionCode == 0)
                bytesWritten = data[0];
        }

        /// <summary>
        /// Indicates the number of bytes written
        /// </summary>
        public byte BytesWritten
        {
            get { return this.bytesWritten; }
        }

    }

    /// <summary>
    /// Class that supports the Get Sdr Repository Info command.
    /// </summary>
    public class FruInventoryArea : ResponseBase
    {
        internal ushort fruSize;

        internal bool accessedByBytes = false;

        /// <summary>
        /// Initialize class
        /// </summary>
        public FruInventoryArea(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        { }

        /// <summary>
        /// Fru Size
        /// </summary>
        public ushort FruSize
        {
            get { return this.fruSize; }
        }

        /// <summary>
        /// Accessed By Bytes
        /// If false, access is by WORD
        /// </summary>
        public bool AccessedByBytes
        {
            get { return this.accessedByBytes; }
        }
    }

    #endregion

    #region Jbod

    /// <summary>
    /// Properties in the Get JBOD Disk Status command response
    /// </summary>
    public class DiskStatusInfo : ResponseBase
    {
        // indicates JBOD disk channel
        private byte _channel;

        // indicates JBOD disk count
        private byte _diskcount;

        // indicates disks Id and disk status
        private Dictionary<byte, DiskStatus> disks = new Dictionary<byte, DiskStatus>();

        /// <summary>
        /// Properties in the Get Disk Status command response
        /// </summary>
        public DiskStatusInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetDiskStatusResponse response =
            (GetDiskStatusResponse)IpmiSharedFunc.ConvertResponse(data, 
            typeof(GetDiskStatusResponse));

            if (response.CompletionCode == 0)
            {
                this.SetParamaters(response.Channel, response.DiskCount,
                    response.StatusData);
            }
        }

        internal void SetParamaters(byte channel, byte diskcount, byte[] diskInfo)
        {
            if (base.CompletionCode == 0)
            {
                this._channel = channel;
                this._diskcount = diskcount;

                foreach (byte disk in diskInfo)
                {
                    // Get the status byte
                    int status = (int)((disk & 0xC0) >> 6);

                    // initialize the disk status to unknown
                    DiskStatus diskStatus = DiskStatus.Unknown;

                    // change the disk status if it is in the enum.
                    if (Enum.IsDefined(typeof(DiskStatus), status))
                    {
                        diskStatus = (DiskStatus)status;
                    }

                    // add the disk status to the response list
                    this.disks.Add((byte)(disk & 0x3F), diskStatus);
                }
            }
        
        }

        /// <summary>
        /// JBOD Disk Channel
        /// </summary>
        public byte Channel
        {
            get { return this._channel; }
        }

        /// <summary>
        /// JBOD Disk Count
        /// </summary>
        public byte DiskCount
        {
            get { return this._diskcount; }
        }

        /// <summary>
        /// Indicates Disk Status
        /// </summary>
        public Dictionary<byte, DiskStatus> DiskState
        {
            get { return this.disks; }
        }
    }

    /// <summary>
    /// Properties in the Get JBOD Disk Status command response
    /// </summary>
    public class DiskInformation : ResponseBase
    {
        // indicates JBOD unit of measurement
        private SensorUnitTypeCode _unit = SensorUnitTypeCode.Unspecified;

        // indicates JBOD disk count
        private string _reading = string.Empty;

        /// <summary>
        /// Properties in the Get Disk information command response
        /// </summary>
        public DiskInformation(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetDiskInfoResponse response =
            (GetDiskInfoResponse)IpmiSharedFunc.ConvertResponse(data, 
            typeof(GetDiskInfoResponse));

            if (response.CompletionCode == 0)
            {
                this.SetParamaters(response.Unit, response.Multiplier, response.Reading);
            }

        }

        internal void SetParamaters(byte unit, byte multiplier, byte[] diskReading)
        {
            if (base.CompletionCode == 0)
            {
                // set the ipmi sensor unit type.
                if (Enum.IsDefined(typeof(SensorUnitTypeCode), unit))
                {
                    this._unit = (SensorUnitTypeCode)unit;
                }
                else
                {
                    this._unit = SensorUnitTypeCode.Unspecified;
                }

                bool negative = false;

                // check for negative reading
                if ((multiplier & 0x80) > 0)
                {
                    negative = true;
                }

                byte multiply = (byte)(multiplier & 0x7F);

                // zero multiplier is
                // not valid.
                if (multiply == 0)
                    multiply = 1;

                int reading = ((int)diskReading[1] * multiply);

                // invert the value to return minus.
                if (negative)
                {
                    reading = (reading * -1);
                }

                // return the converted reading reading, and the LS
                // reading value.
                this._reading = (reading + "." + diskReading[0]);
            }        
        }

        /// <summary>
        /// JBOD Disk Channel
        /// </summary>
        public SensorUnitTypeCode Unit
        {
            get { return this._unit; }
        }

        /// <summary>
        /// JBOD Disk Count
        /// </summary>
        public string Reading
        {
            get { return this._reading; }
        }

    }

    #endregion

    #region OEM Commands

    /// <summary>
    /// Processor Info command response
    /// </summary>
    public class ProcessorInfo : ResponseBase
    {
        // Processor Type
        private ProcessorType _type;

        // Processor state
        private ProcessorState _state;

        // processor frequency
        private ushort _frequency;

        /// <summary>
        /// Properties in the Get Processor information command response
        /// </summary>
        public ProcessorInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetProcessorInfoResponse response =
            (GetProcessorInfoResponse)IpmiSharedFunc.ConvertResponse(data,
            typeof(GetProcessorInfoResponse));

            this.SetParamaters(response.Frequency, response.ProcessorType,
                response.ProcessorState);

        }

        internal void SetParamaters(ushort frequency, byte processorType, byte processorState)
        {
            if (base.CompletionCode == 0)
            {
                // set the response frequency
                this.Frequency = frequency;

                // set the processor type.
                if (Enum.IsDefined(typeof(ProcessorType), processorType))
                {
                    this.ProcessorType = (ProcessorType)processorType;
                }
                else
                {
                    this.ProcessorType = ProcessorType.Unknown;
                }

                // set the processor state.
                if (Enum.IsDefined(typeof(ProcessorState), processorState))
                {
                    this.ProcessorState = (ProcessorState)processorState;
                }
                else
                {
                    this.ProcessorState = ProcessorState.Unknown;
                }

            }
        }

        /// <summary>
        /// Processor Type
        /// </summary>
        public ProcessorType ProcessorType
        {
            get { return this._type; }
            private set { this._type = value; }
        }

        /// <summary>
        /// Processor State
        /// </summary>
        public ProcessorState ProcessorState
        {
            get { return this._state; }
            private set { this._state = value; }
        }

        /// <summary>
        /// Processor Frequency
        /// </summary>
        public ushort Frequency
        {
            get { return this._frequency; }
            private set { this._frequency = value; }
        }
    }

    /// <summary>
    /// Memory Info command response
    /// </summary>
    public class MemoryInfo : ResponseBase
    {
        // Dimm Type
        private MemoryType _type;

        // Dimm Speed
        private ushort _speed;

        // Dimm Size
        private ushort _size;

        // Actual Memory Speed
        private bool _actualSpeed;

        /// <summary>
        /// Memory Voltage
        /// </summary>
        private string _memVoltage;

        /// <summary>
        /// Memory Status
        /// </summary>
        private MemoryStatus _status;

        /// <summary>
        /// Properties in the Get Memory information command response
        /// </summary>
        public MemoryInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Memory Voltage
        /// </summary>
        private Dictionary<byte, string> memVoltageLevels = new Dictionary<byte, string>() 
        { 
            {0, "1.5V"},
            {1, "1.35V"},
            {0xff, "Unknown"}
        };

        internal override void SetParamaters(byte[] data)
        {
            GetMemoryInfoResponse response = (GetMemoryInfoResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetMemoryInfoResponse));

            this.SetParamaters(response.MemorySpeed, response.MemorySize, response.RunningSpeed,
                response.MemoryType, response.Voltage, response.Status);
        }

        internal void SetParamaters(ushort memorySpeed, ushort memorySize, byte runningSpeed, byte memType, byte voltage, byte status)
        {
            if (base.CompletionCode == 0)
            {
                // set memory Speed
                this.Speed = memorySpeed;

                // set memory size
                this.MemorySize = memorySize;

                // Dimm Running at Actual Speed
                this.RunningActualSpeed = Convert.ToBoolean(runningSpeed);

                // Memory Type
                if (Enum.IsDefined(typeof(MemoryType), memType))
                {
                    this.MemoryType = (MemoryType)memType;
                }
                else
                {
                    this.MemoryType = MemoryType.Unknown;
                }

                // set the memory voltage
                string voltageStr = "";
                if (memVoltageLevels.TryGetValue(voltage, out voltageStr))
                {
                    this.Voltage = voltageStr;
                }
                else 
                {
                    this.Voltage = "Unknown";
                }

                // set the memory status
                if (Enum.IsDefined(typeof(MemoryStatus), status))
                {
                    this.Status = (MemoryStatus)status;
                }
                else
                {
                    this.Status = MemoryStatus.Unknown;
                }
            }

        }

        /// <summary>
        /// Memory Type
        /// </summary>
        public MemoryType MemoryType
        {
            get { return this._type; }
            private set { this._type = value; }
        }

        /// <summary>
        /// Dimm Speed
        /// </summary>
        public ushort Speed
        {
            get { return this._speed; }
            private set { this._speed = value; }
        }

        /// <summary>
        /// Dimm Size
        /// </summary>
        public ushort MemorySize
        {
            get { return this._size; }
            private set { this._size = value; }
        }

        /// <summary>
        /// Dimm Running Actual Speed
        /// </summary>
        public bool RunningActualSpeed
        {
            get { return this._actualSpeed; }
            private set { this._actualSpeed = value; }
        }

        /// <summary>
        /// Dimm Voltage
        /// </summary>
        public string Voltage
        {
            get { return this._memVoltage; }
            private set { this._memVoltage = value; }
        }

        /// <summary>
        /// Memory Status
        /// </summary>
        public MemoryStatus Status
        {
            get { return this._status; }
            private set { this._status = value; }
        }
    }

    /// <summary>
    /// Memory Index command response
    /// </summary>
    public class MemoryIndex : ResponseBase
    {

        // Dimm Speed
        private int _slotCount;

        // Dimm Presense Map
        private Dictionary<int, bool> _map = new Dictionary<int, bool>();

        /// <summary>
        /// Properties in the Get Memory information command response
        /// </summary>
        public MemoryIndex(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetMemoryIndexResponse response =
        (GetMemoryIndexResponse)IpmiSharedFunc.ConvertResponse(data,
        typeof(GetMemoryIndexResponse));

            this.SetParamaters(response.SlotCount, response.Presence);
        }

        internal void SetParamaters(byte count, BitArray presence)
        {
            if (base.CompletionCode == 0)
            {
                // DIMM slot count
                this._slotCount = (int)count;

                for (int i = 0; i < presence.Count; i++)
                {
                    // add 1 to avoid zero on physical DIMM count.
                    _map.Add((i + 1), presence[i]);
                }
            }
        }

        /// <summary>
        /// Slot Count
        /// </summary>
        public int SlotCount
        {
            get { return this._slotCount; }
            private set { this._slotCount = value; }
        }

        /// <summary>
        /// Dimm Presense Map
        /// </summary>
        public Dictionary<int, bool> PresenceMap
        {
            get { return this._map; }
            private set { this._map = value; }
        }
    }

    /// <summary>
    /// PCIe Info command response
    /// </summary>
    public class PCIeInfo : ResponseBase
    {
        // PCIe standard specifies that 0xffff for VendorId and SubsystemVendorId 
        // indicates no device present
        private const ushort pcieNotPresentVendorId = 0xffff;

        // PCIe Index
        private byte _slotIndex;

        // PCIe Vendor Id
        private string _vendorId;

        // PCIe Device Id
        private string _deviceId;

        // PCIe SubSystemId
        private string _subSystemId;

        // PCIe State
        private PCIeState _state = PCIeState.Unknown;

        /// <summary>
        /// Properties in the Get PCIe information command response
        /// </summary>
        public PCIeInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetPCIeInfoResponse response =
                (GetPCIeInfoResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetPCIeInfoResponse));

            if (response.CompletionCode == 0x00)
            {
                    this.SetParamaters(PCIeState.Present, response.VendorId, response.DeviceId,
                    response.SubsystemVendorId, response.SubSystemId);
                }
            }

        internal void SetParamaters(PCIeState state, ushort vendorId, ushort deviceId, ushort subsystemVendorId, ushort subsystemId)
        {
            byte[] shortArr = new byte[2];
            byte[] intArr = new byte[4];

            if ((vendorId == pcieNotPresentVendorId) && (subsystemVendorId == pcieNotPresentVendorId))
            {
                this._state = PCIeState.NotPresent;
                this.VendorId = "0";
                this.DeviceId = "0";
                this.SubsystemId = "0";
            }
            else
            {
            // Slot State
            this._state = state;

            // Vendor Id
            IpmiSharedFunc.SplitWord(vendorId, out shortArr[1], out shortArr[0]);
            this.VendorId = IpmiSharedFunc.ByteArrayToHexString(shortArr);

            // Device Id
            IpmiSharedFunc.SplitWord(deviceId, out shortArr[1], out shortArr[0]);
            this.DeviceId = IpmiSharedFunc.ByteArrayToHexString(shortArr);

            // Combine Subsystem Vendor ID and Subsystem ID from response to form PCIe Subsystem ID
            IpmiSharedFunc.SplitWord(subsystemVendorId, out intArr[3], out intArr[2]);
            IpmiSharedFunc.SplitWord(subsystemId, out intArr[1], out intArr[0]);
            this.SubsystemId = IpmiSharedFunc.ByteArrayToHexString(intArr);
        }
        }

        /// <summary>
        /// PCIe Index
        /// </summary>
        public byte SlotIndex
        {
            get { return this._slotIndex; }
            internal set { this._slotIndex = value; }
        }

        /// <summary>
        /// PCIe Card State
        /// </summary>
        public PCIeState CardState
        {
            get { return this._state; }
            private set { this._state = value; }
        }


        /// <summary>
        /// PCIe VendorId
        /// </summary>
        public string VendorId
        {
            get { return this._vendorId; }
            private set { this._vendorId = value; }
        }

        /// <summary>
        /// PCIe DeviceId
        /// </summary>
        public string DeviceId
        {
            get { return this._deviceId; }
            private set { this._deviceId = value; }
        }

        /// <summary>
        /// PCIe SubSystemId
        /// </summary>
        public string SubsystemId
        {
            get { return this._subSystemId; }
            private set { this._subSystemId = value; }
        }
    }

    /// <summary>
    /// PCIe Map command response
    /// </summary>
    public class PCIeMap : ResponseBase
    {
        // PCIe Presence Map
        private ushort _presenceMap;

        /// <summary>
        /// Properties in the Get PCIe information command response
        /// </summary>
        public PCIeMap(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetPCIeInfoMapResponse response =
                (GetPCIeInfoMapResponse)IpmiSharedFunc.ConvertResponse(data, typeof(GetPCIeInfoMapResponse));

            if (response.CompletionCode == 0x00)
            {
                this.SetParamaters(response.PciePresenceLsb, response.PciePresenceMsb);
            }
        }

        internal void SetParamaters(byte pciePresenceLsb, byte pciePresenceMsb)
        {
            // Presence Map
            this.PresenceMap = (ushort)((pciePresenceMsb << 8) | pciePresenceLsb);
        }

    /// <summary>
        /// PCIe Presence Map
        /// </summary>
        public ushort PresenceMap
        {
            get { return this._presenceMap; }
            private set { this._presenceMap = value; }
        }
    }


    /// <summary>
    /// NIC Info command response
    /// </summary>
    public class NicInfo : ResponseBase
    {
        // Nic Index
        private int _index;

        // MAC Address
        private string _mac = string.Empty;

        /// <summary>
        /// Properties in the Get Nic information command response
        /// </summary>
        public NicInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            // if success attempt to parse the mac address
            if (base.CompletionCode == 0)
            {
                // parse the MAC address
                for (int i = 0; i < data.Length; i++)
                {
                    // convert bytes to their Hex value
                    this.MacAddress += data[i].ToString("X2", CultureInfo.InvariantCulture);

                    if (i != data.Length - 1)
                        this.MacAddress += ":";
                }
            }
        }

        /// <summary>
        /// Nic Number
        /// </summary>
        public int DeviceId
        {
            get { return this._index; }
            internal set { this._index = value; }
        }

        /// <summary>
        /// MAC Address
        /// </summary>
        public string MacAddress
        {
            get { return this._mac; }
            private set { this._mac = value; }
        }
    }

    /// <summary>
    /// Returns Local Energy Storage data.
    /// </summary>
    public class EnergyStorage : ResponseBase
    {
        /// <summary>
        /// Energy Storage Presence
        /// </summary>
        private byte presence;

        /// <summary>
        /// Energy Storage State
        /// </summary>
        private EnergyStorageState state;

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
        /// Initialize class
        /// </summary>
        public EnergyStorage(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(byte presence, byte state, byte scalingFactor, ushort bladeBackupEnergy, byte nvdimmBackupEnergy, ushort rollingCounter)
        {
            this.presence = presence;
            this.state = (EnergyStorageState)state;
            this.scalingFactor = scalingFactor;
            this.bladeBackupEnergy = bladeBackupEnergy;
            this.nvdimmBackupEnergy = nvdimmBackupEnergy;
            this.rollingCounter = rollingCounter;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        /// <param name="param"></param>
        internal override void SetParamaters(byte[] param)
        {
            SetParamaters(
                            (byte)(param[0] & 0x03),         // presence
                            (byte)((param[0] & 0x1C) >> 2),  // state
                            param[2],                        // scaling factor
                            BitConverter.ToUInt16(param, 3), // Blade backup energy
                            param[5],                        // NVDIMM backup energy
                            BitConverter.ToUInt16(param, 6)  // rolling counter
                            );

        }

        /// <summary>
        /// Energy Storage Presence
        /// </summary>       
        public byte Presence
        {
            get { return this.presence; }
        }

        /// <summary>
        /// Energy Storage State
        /// </summary>       
        public EnergyStorageState EnergyState
        {
            get { return this.state; }
        }

        /// <summary>
        /// Scaling factor for energy in Joules
        /// </summary>       
        public byte ScalingFactor
        {
            get { return this.scalingFactor; }
        }

        /// <summary>
        /// Backup energy for the blade
        /// (in step size indicated by scalingFactor)
        /// </summary>
        public ushort BladeBackupEnergy
        {
            get { return this.bladeBackupEnergy; }
        }

        /// <summary>
        /// Backup energy for each NVDIMM
        /// (in step size indicated by scalingFactor)
        /// </summary>
        public byte NvdimmBackupEnergy
        {
            get { return this.nvdimmBackupEnergy; }
        }

        /// <summary>
        /// Rolling counter in seconds
        /// </summary>       
        public ushort RollingCounter
        {
            get { return this.rollingCounter; }
        }   
    }

    /// <summary>
    /// PSU Alert
    /// </summary>
    public class PsuAlert : ResponseBase
    {

        private bool psuAlertGpi;

        private bool autoProchotEnabled;

        private bool bmcProchotEnabled;

        /// <summary>
        /// Initialize class
        /// </summary>
        public PsuAlert(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        internal override void SetParamaters(byte[] param)
        {

            if ((param[0] & 0x40) == 0x40)
                psuAlertGpi = true;

            if ((param[0] & 0x10) == 0x10)
                autoProchotEnabled = true;


            if ((param[0] & 0x01) == 0x01)
                bmcProchotEnabled = true;
        }

        /// <summary>
        /// Set response class parmaters given values
        /// </summary>
        internal void SetParamaters(bool psuAlertGpi, bool autoProchotEnabled, bool bmcProchotEnabled)
        {
            this.psuAlertGpi = psuAlertGpi;
            this.autoProchotEnabled = autoProchotEnabled;
            this.bmcProchotEnabled = bmcProchotEnabled;

        }


        /// <summary>
        /// PSU_Alert BMC GPI Status
        /// [7:6] BLADE_EN2 to BMC GPI
        /// </summary>
        public bool PsuAlertGpi
        {
            get
            {
                return psuAlertGpi;
            }
        }

        /// <summary>
        /// Auto PROCHOT on switch GPI
        /// [5:4] Auto FAST_PROCHOT Enabled
        /// </summary>
        public bool AutoProchotEnabled
        {
            get
            {
                return autoProchotEnabled;
            }
        }

        /// <summary>
        /// BMC PROCHOT on switch GPI
        /// [3:0] BMC FAST_PROCHOT Enabled
        /// </summary>
        public bool BmcProchotEnabled
        {
            get
            {
                return bmcProchotEnabled;
            }
        }
    
    }

    /// <summary>
    /// BIOS POST Code
    /// </summary>
    public class BiosCode : ResponseBase
    {

        private string bioscode = string.Empty;

        /// <summary>
        /// Initialize class
        /// </summary>
        public BiosCode(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        internal override void SetParamaters(byte[] param)
        {
            GetBiosCodeResponse response =
            (GetBiosCodeResponse)IpmiSharedFunc.ConvertResponse(param,
                typeof(GetBiosCodeResponse));

            this.SetParamaters(response.PostCode);
        }

        /// <summary>
        /// Set response given fromatted payload data.
        /// </summary>
        internal void SetParamaters(string code)
        {
            bioscode = code;
        }

        /// <summary>
        /// BIOS Port 80 Code
        /// </summary>
        public string PostCode
        {
            get { return bioscode; }
        }

    }

    /// <summary>
    /// Default Power Cap (DCP)
    /// </summary>
    public class DefaultPowerLimit : ResponseBase
    {

        /// <summary>
        /// Default Power Cap in Watts
        /// </summary>   
        private ushort dpc; 

        /// <summary>
        ///  Time in milliseconds after applying DPC to 
        ///  wait before deasserting the PROCHOT
        /// </summary>    
        private ushort delay;

        /// <summary>
        ///  Disable/Enable Default Power Cap on PSU_Alert GPI.
        /// </summary>   
        private bool dpcEnabled;


        /// <summary>
        /// Initialize class
        /// </summary>
        public DefaultPowerLimit(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(ushort dpc, ushort delay, bool dpcEnabled)
        {
            this.dpc = dpc;
            this.delay = delay;
            this.dpcEnabled = dpcEnabled;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        /// <param name="param"></param>
        internal override void SetParamaters(byte[] param)
        {
            GetDefaultPowerLimitResponse response =
        (GetDefaultPowerLimitResponse)IpmiSharedFunc.ConvertResponse(param,
        typeof(GetDefaultPowerLimitResponse));

            bool dpcActive = response.DefaultCapEnabled == 0x01 ? true : false;

            this.SetParamaters(response.DefaultPowerCap, response.WaitTime, dpcActive);

        }
        
        /// <summary>
        /// Default Power Cap in Watts
        /// </summary>       
        public ushort DefaultPowerCap
        {
            get { return this.dpc; }
        }

        /// <summary>
        ///  Time in milliseconds after applying DPC to 
        ///  wait before deasserting the PROCHOT
        /// </summary>    
        public ushort WaitTime
        {
            get { return this.delay; }
        }

        /// <summary>
        ///  Disable/Enable Default Power Cap on PSU_Alert GPI.
        /// </summary>   
        public bool DefaultCapEnabled
        {
            get { return this.dpcEnabled; }
        }
    
    }

    /// <summary>
    /// NVDIMM Trigger Response
    /// </summary>
    public class NvDimmTrigger : ResponseBase
    {
        // Manual Trigger Asserted
        private bool manualTriggerAsserted;

        // Adr Trigger.  Initialized to unknown.
        private NvDimmTriggerAction adrTrigger = NvDimmTriggerAction.Unknown;
        
        /// Delay between ADR_COMPLETE and blade power off (seconds)
        private int adrCompleteDelay = -1;
        
        // ADR_COMPLETE Power-off Delay Remaining Time
        // Time remaining before power is turned off (seconds).
        private int adrCompleteTimeRemaining = -1;
        
        // ADR_COMPLETE Status
        private byte adrComplete;

        // NVDIMM Present Power-off Delay
        private int nvdimmPresentPowerOffDelay = -1;

        // NVDIMM Present Power-off Delay Remaining Time
        // Countdown timer value that starts from the value specified in the NVDIMM Present Power-off Delay field.
        private int nvdimmPresentTimeRemaining = -1;

        /// <summary>
        /// Initialize class
        /// </summary>
        public NvDimmTrigger(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        /// <param name="param"></param>
        internal override void SetParamaters(byte[] param)
        {
            GetNvDimmTriggerResponse response =
        (GetNvDimmTriggerResponse)IpmiSharedFunc.ConvertResponse(param,
        typeof(GetNvDimmTriggerResponse));

            this.SetParamaters(response.ManualTriggerAsserted, this.adrComplete, response.AdrTriggerType, response.AdrCompletePowerOffDelay, 
                response.NvdimmPresentPowerOffDelay, response.AdrCompleteTimeRemaining, response.NvdimmPresentTimeRemaining);
        }

        /// <summary>
        /// Set response using formatted payload data
        /// </summary>
        internal void SetParamaters(bool manualTrigger, byte adrComplete, NvDimmTriggerAction action, byte adrCompleteDelay, 
            byte nvdimmPresentPoweroffDelay, byte adrCompleteTimeRemaining, byte nvdimmPresentTimeRemaining)
        {
            this.manualTriggerAsserted = manualTrigger;
            this.adrComplete = adrComplete;
            this.adrTrigger = action;
            this.adrCompleteDelay = (int)adrCompleteDelay;
            this.adrCompleteTimeRemaining = (int)adrCompleteTimeRemaining;
            this.nvdimmPresentPowerOffDelay = nvdimmPresentPoweroffDelay;
            this.nvdimmPresentTimeRemaining = nvdimmPresentTimeRemaining;            
        }

        /// <summary>
        /// NVDIMM Trigger Asserted
        /// </summary>
        public bool ManualTriggerAsserted
        {
            get
            {
                return manualTriggerAsserted;
            }
        }

        /// <summary>
        /// Adr Trigger 
        /// </summary>
        public NvDimmTriggerAction AdrTriggerType
        {
            get
            {
                if (Enum.IsDefined(typeof(NvDimmTriggerAction), adrTrigger))
                    return (NvDimmTriggerAction)adrTrigger;
                else
                    return NvDimmTriggerAction.Unknown;
            }
        }

        /// <summary>
        /// Time Remaining until Adr backup is complete
        /// </summary>
        public int AdrCompleteTimeRemaining
        {
            get { return this.adrCompleteTimeRemaining; } 
        }

        /// <summary>
        /// Total time ADR requires to complete backup
        /// </summary>
        public int AdrCompleteDelay
        {
            get { return this.adrCompleteDelay; }
        }

        /// <summary>
        /// ADR_COMPLETE Status
        /// 00h = ADR_COMPLETE deasserted
        /// 01h = ADR_COMPLETE asserted
        /// </summary>
        public byte AdrComplete
        {
            get { return this.adrComplete; }
            set { this.adrComplete = value; }
        }

        // NVDIMM Present Power-off Delay
        public int NvdimmPresentPowerOffDelay
        {
            get { return this.nvdimmPresentPowerOffDelay; }
            set { this.nvdimmPresentPowerOffDelay = value; }
        }

        // NVDIMM present power-off delay time remaining
        public int NvdimmPresentTimeRemaining
        {
            get { return this.nvdimmPresentTimeRemaining; }
            set { this.nvdimmPresentTimeRemaining = value; }
        }
    }

    /// <summary>
    /// Start Serial Session
    /// </summary>
    public class StartSerialSession : ResponseBase
    {

        /// <summary>
        /// Session Status
        /// </summary>   
        private bool sessionStatus;

        /// <summary>
        /// Initialize class
        /// </summary>
        public StartSerialSession(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(byte sessionStatus)
        {
            this.sessionStatus = sessionStatus == 0x01 ? true : false;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        /// <param name="param"></param>
        internal override void SetParamaters(byte[] param)
        {
            StartSerialSessionResponse response =
        (StartSerialSessionResponse)IpmiSharedFunc.ConvertResponse(param,
        typeof(StartSerialSessionResponse));

            this.sessionStatus = response.SessionStatus == 0x01 ? true : false;

        }

        /// <summary>
        /// Session Status
        /// </summary>       
        public bool SessionActivated
        {
            get { return this.sessionStatus; }
        }

    }

    /// <summary>
    /// Stop Serial Session
    /// </summary>
    public class StopSerialSession : ResponseBase
    {
        /// <summary>
        /// Initialize class
        /// </summary>
        public StopSerialSession(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        {
        }

    }


    /// <summary>
    /// Receive Serial Data
    /// </summary>
    public class ReceiveSerialData : ResponseBase
    {
        ushort payloadLength;

        byte[] payload = { };

        /// <summary>
        /// Initialize class
        /// </summary>
        public ReceiveSerialData(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(ushort payloadLength, byte[] payload)
        {
            this.payloadLength = payloadLength;
            this.payload = payload;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        internal override void SetParamaters(byte[] param)
        {
            ReceiveSerialDataResponse response =
            (ReceiveSerialDataResponse)IpmiSharedFunc.ConvertResponse(param,
            typeof(ReceiveSerialDataResponse));

            this.SetParamaters(response.PayloadLength, 
                response.Payload);
        }

        /// <summary>
        /// Payload Lenght
        /// </summary>       
        public ushort PayloadLength
        {
            get { return this.payloadLength; }
        }

        /// <summary>
        /// Serial Console Payload
        /// </summary>       
        public byte[] Payload
        {
            get { return this.payload; }
            set { this.payload = value; }
        }

    }

    /// <summary>
    /// Send Serial Data
    /// </summary>
    public class SendSerialData : ResponseBase
    {
        /// <summary>
        /// Initialize class
        /// </summary>
        public SendSerialData(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        {
        }

    }

    /// <summary>
    /// Thermal Control Response
    /// </summary>
    public class ThermalControl : ResponseBase
    {
        byte status;

        byte tmargin;

        /// <summary>
        /// Initialize class
        /// </summary>
        public ThermalControl(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(byte status, byte tmargin)
        {
            this.status = status;
            this.tmargin = tmargin;
        }

        /// <summary>
        /// Set response class parmaters given raw payload data
        /// </summary>
        internal override void SetParamaters(byte[] param)
        {
            ThermalControlResponse response =
            (ThermalControlResponse)IpmiSharedFunc.ConvertResponse(param,
            typeof(ThermalControlResponse));

            this.SetParamaters(response.Status,
                response.Tmargin);
        }

        /// <summary>
        /// Enable/Disabled Status
        /// </summary>       
        public byte Status
        {
            get { return this.status; }
        }

        /// <summary>
        /// T-Margin (throttling trigger point)
        /// </summary>       
        public byte Tmargin
        {
            get { return this.tmargin; }
            set { this.tmargin = value; }
        }

    }

    #endregion

    #region Bridge Classes

    /// <summary>
    /// Class that supports the Send Message / Get Message command.
    /// </summary>
    public class BridgeMessage : ResponseBase
    {
        /// <summary>
        /// Response message payload.
        /// </summary>
        private byte[] messageData;

        /// <summary>
        /// Initialize class
        /// </summary>
        public BridgeMessage(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        {
            this.messageData = param;
        }

        /// <summary>
        /// Response message payload.
        /// </summary>
        public byte[] MessageData
        {
            get { return this.messageData; }
            set { this.messageData = value; }
        }
    }

    /// <summary>
    /// Class that supports the Get Message Flags command.
    /// </summary>
    public class MessageFlags : ResponseBase
    {

        /// <summary>
        /// Response message payload.
        /// </summary>
        private byte messageAvail;

        /// <summary>
        /// Receive Buffer full
        /// </summary>
        private byte bufferFull;

        /// <summary>
        /// Watch Dog pre-timeout interrupt
        /// </summary>
        private byte watchDogTimeout;

        /// <summary>
        /// OEM 1 Data Available
        /// </summary>
        private byte oem1;

        /// <summary>
        /// OEM 2 Data Available
        /// </summary>
        private byte oem2;

        /// <summary>
        /// OEM 3 Data Available
        /// </summary>
        private byte oem3;

        /// <summary>
        /// Initialize class
        /// </summary>
        public MessageFlags(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        /// <summary>
        /// Set class properties
        /// </summary>
        internal void SetParamaters(byte messageAvail, byte bufferFull, byte watchDog, byte oem1, byte oem2, byte oem3)
        {
            this.messageAvail = messageAvail;
            this.bufferFull = bufferFull;
            this.watchDogTimeout = watchDog;
            this.oem1 = oem1;
            this.oem2 = oem2;
            this.oem3 = oem3;
        }

        /// <summary>
        /// Set class properties
        /// </summary>
        internal override void SetParamaters(byte[] param)
        {
            this.messageAvail = (byte)(param[0] & 0x01);

            this.bufferFull = (byte)((param[0] & 0x02) >> 1);

            this.watchDogTimeout = (byte)((param[0] & 0x08) >> 3);

            this.oem1 = (byte)((param[0] & 0x20) >> 5);

            this.oem2 = (byte)((param[0] & 0x40) >> 6);

            this.oem3 = (byte)((param[0] & 0x80) >> 7);
        }

        /// <summary>
        /// Receive Message Available
        /// </summary>
        public byte MessageAvailable
        {
            get { return this.messageAvail; }
        }

        /// <summary>
        /// Receive Buffer full
        /// </summary>
        public byte BufferFull
        {
            get { return this.bufferFull; }
        }

        /// <summary>
        /// Watch Dog pre-timeout interrupt
        /// </summary>
        public byte WatchDogTimeout
        {
            get { return this.watchDogTimeout; }
        }

        /// <summary>
        /// OEM 1 Data Available
        /// </summary>
        public byte OEM1
        {
            get { return this.oem1; }
        }

        /// <summary>
        /// OEM 2 Data Available
        /// </summary>
        public byte OEM2
        {
            get { return this.oem2; }
        }

        /// <summary>
        /// OEM 3 Data Available
        /// </summary>
        public byte OEM3
        {
            get { return this.oem3; }
        }
    }

    /// <summary>
    /// Class that supports the Send Message / Get Message command.
    /// </summary>
    public class BridgeChannelReceive : ResponseBase
    {
        /// <summary>
        /// Channel to send the message.
        /// </summary>
        private byte channel;

        /// <summary>
        /// Channel Enable/Disable State.
        /// </summary>
        private byte channelState;

        /// <summary>
        /// Initialize class
        /// </summary>
        public BridgeChannelReceive(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal void SetParamaters(byte chennel, byte channelsate)
        {
            this.channel = chennel;
            this.channelState = channelsate;
        }

        internal override void SetParamaters(byte[] param)
        {
            this.channel = param[0];
            this.channelState = param[1];
        }

        /// <summary>
        /// Channel to send the request message.
        /// </summary>
        [IpmiMessageData(0)]
        public byte Channel
        {
            get { return this.channel; }
            set { this.channel = (byte)(value & 0x0f); }
        }

        /// <summary>
        /// Channel State
        /// </summary>
        [IpmiMessageData(1)]
        public byte ChannelState
        {
            get { return this.channelState; }
            set { this.channelState = (byte)(value & 0x01); }
        }
    }

    #endregion

    #region SEL & SDR Classes

    /// <summary>
    /// Class that supports the Get SEL Info command.
    /// </summary>
    public class SystemEventLogInfo : ResponseBase
    {
        private string _version;

        private int _entries;

        private int _space;

        private DateTime _lastUpdate;

        private DateTime _lastCleared;

        /// <summary>
        /// Initialize class
        /// </summary>
        public SystemEventLogInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            SelInfoResponse response =
                (SelInfoResponse)IpmiSharedFunc.ConvertResponse(data, typeof(SelInfoResponse));

            if (base.CompletionCode == 0x00)
            {
                this.SetParamaters(response.SELVersion,
                                                    response.MSByte,
                                                    response.LSByte,
                                                    response.SelFeeSpace,
                                                    response.LastAdded,
                                                    response.LastRemoved);
            }
        }

        internal void SetParamaters(byte version, byte msByte, byte lsByte, byte[] spaceFree, byte[] lastAdded, byte[] lastRemoved)
        {
            if (base.CompletionCode == 0x00)
            {
                // Sel Version ByteArray
                byte[] SelVersionArray = IpmiSharedFunc.ByteSplit(version, new int[2] { 4, 0 });
                // LS Version Bit [7:4]
                byte VersionLs = SelVersionArray[0];
                // MS Version Bit [3:0]
                byte VersionMs = SelVersionArray[1];

                // SEL Version Number
                this._version = ((int)VersionMs + "." + (int)VersionLs);

                // Number of Events in SEL
                this._entries = (msByte << 8) + lsByte;

                // Default free space in Bytes
                int freeSpace = 65536;

                // Get Real Free Space
                byte[] defaultfreeSpace = new byte[2] { 0xFF, 0xFF };

                if (spaceFree[0] != defaultfreeSpace[0] && spaceFree[1] != defaultfreeSpace[1])
                {
                    // FreeSpace LS byte First
                    byte[] FreeSpaceBytes = spaceFree;
                    freeSpace = (FreeSpaceBytes[1] << 8) + FreeSpaceBytes[0];
                }

                // add free space to class object
                this._space = freeSpace;

                // Convert byte[] to int using Shift operation
                int lastAddedSeconds = lastAdded[0] + (lastAdded[1] << 8) + (lastAdded[2] << 16) + (lastAdded[3] << 24);

                // calculate last entry added date
                this._lastUpdate = IpmiSharedFunc.SecondsOffSet(lastAddedSeconds);

                // Convert byte[] to int using Shift operation
                int lastRemovedSeconds = lastRemoved[0] + (lastRemoved[1] << 8) + (lastRemoved[2] << 16) + (lastRemoved[3] << 24);

                // calculate last entry removed date
                this._lastCleared = IpmiSharedFunc.SecondsOffSet(lastRemovedSeconds);
            }

        }

        /// <summary>
        /// SEL number
        /// </summary>
        public string Version
        {
            get { return this._version; }
        }

        /// <summary>
        /// Number of SEL record entries;
        /// </summary>
        public int Entries
        {
            get { return this._entries; }
        }

        /// <summary>
        /// SEL free space in KB;
        /// </summary>
        public int FreeSpace
        {
            get { return this._space; }
        }

        /// <summary>
        /// Date and time the SEL was last updated
        /// </summary>
        public DateTime LastUpdate
        {
            get { return this._lastUpdate; }
        }

        /// <summary>
        /// Date and time the SEL was last cleared
        /// </summary>
        public DateTime LastCleared
        {
            get { return this._lastCleared; }
        }

    }

    /// <summary>
    /// Collection of SEL records (string formatted).
    /// </summary>
    public class SystemEventLog : ResponseBase
    {
        public SystemEventLog(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] param)
        { }

        private Collection<SystemEventLogMessage> _eventLog = new Collection<SystemEventLogMessage>();

        public Collection<SystemEventLogMessage> EventLog
        { get { return this._eventLog; } }

    }

    /// <summary>
    /// System Event Log (SEL) message class.
    /// </summary>
    public class SystemEventLogMessage : ResponseBase
    {
        // Record ID
        private ushort _recordId;

        // Record Type
        private byte _recordType;

        // SEL event format
        private EventMessageFormat _eventFormat;

        // event date/time
        private DateTime _eventDate;

        // Generator ID
        private byte[] _generatorId = new byte[2];

        // event message format version
        private MsgVersion _eventVersion;

        // sensor type (voltage, temp, processor, etc)
        private SensorType _sensorType;

        // raw sensor Type
        private byte _rawSensorType;

        // sensor number
        private byte _sensorNumber;

        // event direction (assertion/deassertion)
        private EventDir _eventDir;

        // ipmi event type
        private byte _eventType;

        // event message data
        private EventData _eventMessage;

        // raw event payload as a hex string
        private string _eventPayload = string.Empty;

        /// <summary>
        /// Raw unconverted ipmi Payload bytes
        /// </summary>
        private byte[] _rawPayload = new byte[3];

        public SystemEventLogMessage(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        { }

        /// <summary>
        /// Raw ipmi message payload. 3 byte event
        /// payload.
        /// </summary>
        internal byte[] RawPayload
        {
            get { return this._rawPayload; }
            set { this._rawPayload = value; }
        }

        /// <summary>
        /// Raw ipmi message payload event type.
        /// </summary>
        internal byte EventTypeCode
        {
            get { return this._eventType; }
            set { this._eventType = value; }
        }

        /// <summary>
        /// Raw ipmi message payload event type.
        /// </summary>
        internal byte RawSensorType
        {
            get { return this._rawSensorType; }
            set { this._rawSensorType = value; }
        }

        /// <summary>
        /// Byte split event message.
        /// </summary>
        internal EventData EventMessage
        {
            get { return this._eventMessage; }
            set { this._eventMessage = value; }
        }

        #region public properties

        // Record ID
        public ushort RecordId
        {
            get { return this._recordId; }
            internal set { this._recordId = value; }
        }

        // Record Type
        public byte RecordType
        {
            get { return this._recordType; }
            internal set { this._recordType = value; }
        }

        // SEL event type
        public EventMessageFormat EventFormat
        {
            get { return this._eventFormat; }
            internal set { this._eventFormat = value; }
        }

        // event date/time
        public DateTime EventDate
        {
            get { return this._eventDate; }
            internal set { this._eventDate = value; }
        }

        // event message format
        public MsgVersion EventVersion
        {
            get { return this._eventVersion; }
            internal set { this._eventVersion = value; }
        }

        // sensor type (voltage, temp, processor, etc)
        public SensorType SensorType
        {
            get { return this._sensorType; }
            internal set { this._sensorType = value; }
        }

        // Generator ID
        public byte[] GeneratorId
        {
            get { return this._generatorId; }
            internal set { this._generatorId = value; }
        }

        // sensor number
        public byte SensorNumber
        {
            get { return this._sensorNumber; }
            internal set { this._sensorNumber = value; }
        }

        // event direction (assertion/deassertion)
        public EventDir EventDir
        {
            get { return this._eventDir; }
            internal set { this._eventDir = value; }
        }

        public string EventPayload
        {
            get { return this._eventPayload; }
            internal set { this._eventPayload = value; }
        }

        #endregion

    }

    /// <summary>
    /// Response to Get SEL Time Command
    /// </summary>
    public class GetEventLogTime : ResponseBase
    {
        /// <summary>
        /// SEL Time
        /// </summary>
        private DateTime _time;

        public GetEventLogTime(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            SelTimeResponse response =
            (SelTimeResponse)IpmiSharedFunc.ConvertResponse(data, typeof(SelTimeResponse));

            if (response.CompletionCode == 0)
                this.SetParamaters(response.Time);
        }

        internal void SetParamaters(DateTime time)
        {
            this.EventLogTime = time;
        }

        /// <summary>
        /// Gets and sets the SEL Time.
        /// </summary>
        public DateTime EventLogTime
        {
            get { return this._time; }
            private set { this._time = value; }
        }
    }

    /// <summary>
    /// Class that supports the Get Sdr Repository Info command.
    /// </summary>
    public class SdrRepositoryInfo : ResponseBase
    {
        private string _version;

        private int _entries;

        private int _freeSpace;

        private DateTime _lastUpdate = DateTime.Now;

        private DateTime _lastCleared = DateTime.Now;

        /// <summary>
        /// Initialize class
        /// </summary>
        public SdrRepositoryInfo(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            GetSdrRepositoryInfoResponse response =
            (GetSdrRepositoryInfoResponse)IpmiSharedFunc.ConvertResponse(data,
            typeof(GetSdrRepositoryInfoResponse));

            if (response.CompletionCode == 0)
                this.SetParamaters(response.SdrVersion, response.MSByte, response.LSByte,
                    response.SdrFeeSpace, response.LastAdded, response.LastRemoved);

        }

        internal void SetParamaters(byte sdrVersion, byte msByte, byte lsByte, byte[] sdrFreeSpace, byte[] lastAdded, byte[] lastRemoved)
        {
            if (base.CompletionCode == 0x00)
            {
                // Sel Version ByteArray
                byte[] SdrVersionArray = IpmiSharedFunc.ByteSplit(sdrVersion, new int[2] { 4, 0 });
                // LS Version Bit [7:4]
                byte VersionLs = SdrVersionArray[0];
                // MS Version Bit [3:0]
                byte VersionMs = SdrVersionArray[1];

                // Sdr Version Number
                this.Version = ((int)VersionMs + "." + (int)VersionLs);

                // Number of Events in Sdr
                this.Entries = IpmiSharedFunc.GetShort(lsByte, msByte);

                // Default free space in Bytes
                int freeSpace = BitConverter.ToUInt16(sdrFreeSpace, 0);

                // add free space to class object
                this.FreeSpace = freeSpace;

                // Convert byte[] to int using Shift operation
                int lastAddedSeconds = lastAdded[0] + (lastAdded[1] << 8) + (lastAdded[2] << 16) + (lastAdded[3] << 24);

                // calculate last entry added date
                this.LastUpdate = IpmiSharedFunc.SecondsOffSet(lastAddedSeconds);

                // Convert byte[] to int using Shift operation
                int lastRemovedSeconds = lastRemoved[0] + (lastRemoved[1] << 8) + (lastRemoved[2] << 16) + (lastRemoved[3] << 24);

                // calculate last entry removed date
                this.LastCleared = IpmiSharedFunc.SecondsOffSet(lastRemovedSeconds);
            }
        }

        /// <summary>
        /// Sdr Version Number
        /// </summary>
        public string Version
        {
            get { return this._version; }
            private set { this._version = value; }
        }

        /// <summary>
        /// Number of Sdr entries;
        /// </summary>
        public int Entries
        {
            get { return this._entries; }
            private set { this._entries = value; }
        }

        /// <summary>
        /// Sdr free space in KB;
        /// </summary>
        public int FreeSpace
        {
            get { return this._freeSpace; }
            private set { this._freeSpace = value; }
        }

        /// <summary>
        /// Date and time the Sdr was last updated
        /// </summary>
        public DateTime LastUpdate
        {
            get { return this._lastUpdate; }
            private set { this._lastUpdate = value; }
        }

        /// <summary>
        /// Date and time the Sdr was last cleared
        /// </summary>
        public DateTime LastCleared
        {
            get { return this._lastCleared; }
            private set { this._lastCleared = value; }
        }
    }


    /// <summary>
    /// Numeric Sensor Reading Class
    /// </summary>
    public class SensorReading : ResponseBase
    {
        /// <summary>
        /// raw reading byte
        /// </summary>
        private byte rawreading = 0;

        /// <summary>
        /// sensor thresholds
        /// </summary>
        private bool hasThresholds = false;

        /// <summary>
        /// converted reading
        /// </summary>
        private double converted = 0;

        /// <summary>
        /// Sensor Number
        /// </summary>
        private byte sensorNumber;

        /// <summary>
        /// Event/Reading type code
        /// </summary>
        private byte eventTypeCode;

        /// <summary>
        /// Sensor state byte.
        /// </summary>
        private byte eventState = 0xFF;

        /// <summary>
        /// sensor event state desription.
        /// </summary>
        private string eventStateDesc = string.Empty;

        /// <summary>
        /// sensor event state extension.
        /// </summary>
        private byte eventStateExtension;

        /// <summary>
        /// upper non-recoverable threshold value
        /// </summary>
        private double thresholdUpperNonRecoverable;

        /// <summary>
        /// upper critical threshold value
        /// </summary>
        private double thresholdUppercritical;

        /// <summary>
        /// upper non-critical threshold value
        /// </summary>
        private double thresholdUpperNoncritical;

        /// <summary>
        /// lower non-recoverable threshold value
        /// </summary>
        private double thresholdLowerNonRecoverable;

        /// <summary>
        /// lower critical threshold value
        /// </summary>
        private double thresholdLowercritical;

        /// <summary>
        /// </summary>
        private double thresholdLowerNoncritical;

        /// <summary>
        /// Sensor Description
        /// </summary>
        private string description = string.Empty;

        /// <summary>
        /// Initialize class
        /// </summary>
        public SensorReading(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        /// lower non-critical threshold value
        {
            SensorReadingResponse response =
                (SensorReadingResponse)IpmiSharedFunc.ConvertResponse(data, typeof(SensorReadingResponse));

            this.SetParamaters(response.SensorReading, response.SensorStatus, response.StateOffset, response.OptionalOffset);

        }


        internal void SetParamaters(byte reading, byte status, byte state, byte optionalState)
        {

            if (base.CompletionCode == 0x00)
            {

                // set the raw sensor reading
                this.SetReading(reading);

                byte[] statusByteArray = new byte[1];
                statusByteArray[0] = status;

                BitArray sensorStatusBitArray = new BitArray(statusByteArray);
                bool eventMsgEnabled = sensorStatusBitArray[7];
                bool sensorScanEnabled = sensorStatusBitArray[6];
                bool readingUnavailable = sensorStatusBitArray[5];
                this.EventState = state;
                this.EventStateExtension = optionalState;
            }


        }

        /// <summary>
        /// Convert Sensor Reading into Converted Reading
        /// </summary>
        public void ConvertReading(SensorMetadataBase sdr)
        {
            if (sdr != null)
            {
                if (sdr.GetType() == typeof(FullSensorRecord))
                {
                    FullSensorRecord record = (FullSensorRecord)sdr;

                    if (record.IsNumeric)
                    {
                        this.converted = record.ConvertReading(rawreading);

                        if (record.ThresholdReadable > 0)
                            hasThresholds = true;

                        SetThresholds(record);
                    }
                }
            }
        }

        /// <summary>
        /// Convert Sensor Reading into Converted using supplied factors
        /// </summary>
        public void ConvertReading(byte[] factors, FullSensorRecord sdr)
        {
            if (sdr != null)
            {
                if (sdr.IsNumeric)
                {
                    this.converted = sdr.ConvertReading(this.rawreading, factors);

                    if (sdr.ThresholdReadable > 0)
                        hasThresholds = true;

                    SetThresholds(sdr);
                }
            }
        }

        /// <summary>
        /// Set Upper Thresholds
        /// </summary>
        private void SetThresholds(FullSensorRecord sdr)
        {
            // Upper thresholds
            this.thresholdUpperNonRecoverable = sdr.ThresholdUpperNonRecoverable;
            this.thresholdUppercritical = sdr.ThresholdUpperCritical;
            this.thresholdUpperNoncritical = sdr.ThresholdUpperNonCritical;
            // Lower thresholds
            this.thresholdLowerNonRecoverable = sdr.ThresholdLowerNonRecoverable;
            this.thresholdLowercritical = sdr.ThresholdLowerCritical;
            this.thresholdLowerNoncritical = sdr.ThresholdLowerNonCritical;
        }

        /// <summary>
        /// Set the Raw Reading byte
        /// </summary>
        public void SetReading(byte rawReading)
        {
            this.rawreading = rawReading;
        }

        /// <summary>
        /// Set the Event State.
        /// Used to enumerate the sensor state based on bytes 4 and 5 of the Get Sensor Reading response.
        /// </summary>
        public void SetEventState(byte eventState)
        {
            this.eventState = eventState;
        }

        /// <summary>
        /// Sensor Reading
        /// </summary>
        public double Reading
        {
            get { return this.converted; }
            internal set { this.converted = value; }
        }

        /// <summary>
        /// Raw Analog Byte
        /// </summary>
        public byte RawReading
        {
            get { return this.rawreading; }
            internal set { this.rawreading = value; }
        }

        /// <summary>
        /// Sensor State Description
        /// </summary>
        public string EventDescription
        {
            get { return this.eventStateDesc; }
            internal set { this.eventStateDesc = value; }
        }

        /// <summary>
        /// Sensor State
        /// </summary>
        public byte EventState
        {
            get { return this.eventState; }
            internal set { this.eventState = value; }
        }

        /// <summary>
        /// Sensor State Option Extension
        /// </summary>
        public byte EventStateExtension
        {
            get { return this.eventStateExtension; }
            internal set { this.eventStateExtension = value; }
        }

        /// <summary>
        /// upper non-recoverable threshold value
        /// </summary>
        public double ThresholdUpperNonRecoverable
        {
            get { return this.thresholdUpperNonRecoverable; }
        }

        /// <summary>
        /// upper critical threshold value
        /// </summary>
        public double ThresholdUpperCritical
        {
            get { return this.thresholdUppercritical; }
        }

        /// <summary>
        /// upper non-critical threshold value
        /// </summary>
        public double ThresholdUpperNoncritical
        {
            get { return this.thresholdUpperNoncritical; }
        }

        /// <summary>
        /// lower non-recoverable threshold value
        /// </summary>
        public double ThresholdLowerNonRecoverable
        {
            get { return this.thresholdLowerNonRecoverable; }
        }

        /// <summary>
        /// lower critical threshold value
        /// </summary>
        public double ThresholdLowerCritical
        {
            get { return this.thresholdLowercritical; }
        }

        /// <summary>
        /// lower non-critical threshold value
        /// </summary>
        public double ThresholdLowerNoncritical
        {
            get { return this.thresholdLowerNoncritical; }
        }

        /// <summary>
        /// Indicates sensor has thresholds values
        /// </summary>
        public bool HasThreasholds
        {
            get { return this.hasThresholds; }
            internal set { this.hasThresholds = value; }
        }

        /// <summary>
        /// Sensor Number
        /// </summary>
        public byte SensorNumber
        {
            get { return this.sensorNumber; }
            internal set { this.sensorNumber = value; }
        }

        /// <summary>
        /// Event/Reading Type Code
        /// </summary>
        public byte EventTypeCode
        {
            get { return this.eventTypeCode; }
            internal set { this.eventTypeCode = value; }
        }

        /// <summary>
        /// Sensor Description
        /// </summary>
        public string Description
        {
            get { return this.description; }
            internal set { this.description = value; }
        }
    }

    /// <summary>
    /// Sensor Type Code Class
    /// </summary>
    public class SensorTypeCode : ResponseBase
    {
        /// <summary>
        /// Sensor type
        /// </summary>
        private byte _sensorType;

        /// <summary>
        /// sensor event type Code.
        /// </summary>
        private byte _typeCode;

        /// <summary>
        /// Initialize class
        /// </summary>
        public SensorTypeCode(byte completionCode)
        {
            base.CompletionCode = completionCode;
        }

        internal override void SetParamaters(byte[] data)
        {
            SensorTypeResponse response =
                (SensorTypeResponse)IpmiSharedFunc.ConvertResponse(data, typeof(SensorTypeResponse));

            this.SetParamaters(response.SensorType, response.EventTypeCode);

        }


        internal void SetParamaters(byte sensorType, byte eventTypeCode)
        {

            if (base.CompletionCode == 0x00)
            {
                this._sensorType = sensorType;
                this._typeCode = eventTypeCode;
            }


        }

        /// <summary>
        /// Sensor Type
        /// </summary>
        public byte SensorType
        {
            get { return this._sensorType; }
            internal set { this._sensorType = value; }
        }

        /// <summary>
        /// Sensor Event Reading Type Code
        /// </summary>
        public byte EventTypeCode
        {
            get { return this._typeCode; }
            internal set { this._typeCode = value; }
        }
    }

    #endregion

}
