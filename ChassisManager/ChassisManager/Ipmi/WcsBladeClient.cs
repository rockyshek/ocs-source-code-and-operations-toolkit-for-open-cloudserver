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
    using System;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi.NodeManager;

    internal sealed class WcsBladeClient : IpmiClientNodeManager
    {
        # region Private Variables

        // Serial Ipmi Packet Start Byte
        private const byte startByte = 0xA0;
        // Serial Ipmi Packet Stop byte
        private const byte stopByte = 0xA5;
        // Serial Ipmi Packet data escape
        private const byte dataEscape = 0xAA;

        #region Lock Objects

        /// <summary>
        /// locker object for accessing global resources.
        /// </summary>
        private object reqSeqLock = new object();

        /// <summary>
        /// Locker object for modifying the client state
        /// Client state is used for debug status of the client
        /// at any given time.
        /// </summary>
        private object stateLock = new object();

        /// <summary>
        /// Locker object for modifying the client cache
        /// </summary>
        private object cacheLock = new object();

        /// <summary>
        /// Counter for Serial Timeout.  Reset on success.
        /// </summary>
        private uint errCnt = 0;

        #endregion

        // default client connection status.
        private IpmiClientState status = IpmiClientState.Disconnected;

        // initialize session Id
        private uint sessionId = 0;

        // default Async sequence number
        private byte ipmiAsycRqSeq = 0x31;

        private Guid bladeGuid;

        /// <summary>
        /// Gets and RMCP+ rqSeq used for mapping Async sent messages.
        /// </summary>
        /// <value>RMCP+ .</value>
        private byte IpmiAsycRqSeq
        {
            // rqSeq is a 6 bit number, the byte is shared with the 2 bit LUN.
            // permitted values include 1h to 30h for sync IpmiReqSeq. 31h -> 3Fh are 
            // reserved for async commands. The ranges need to be kept separate to 
            // distinguish stale responses in buffers.
            get { return this.ipmiAsycRqSeq; }
            set { this.ipmiAsycRqSeq = (value > 0x3F || value < 0x31 ? (byte)0x31 : value); }
        }

        /// <summary>
        /// Double byte charactors to replace ipmi escape charactors.
        /// See IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        /// See IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        private readonly List<EscapeCharactor> escChars = new List<EscapeCharactor>(5)
        {
            new EscapeCharactor(0xAA, new byte[2]{0xAA, 0xBA}),
            new EscapeCharactor(0xA0, new byte[2]{0xAA, 0xB0}),
            new EscapeCharactor(0xA5, new byte[2]{0xAA, 0xB5}),
            new EscapeCharactor(0xA6, new byte[2]{0xAA, 0xB6}),
            new EscapeCharactor(0x1B, new byte[2]{0xAA, 0x3B})
        };

        // blade device Id
        private readonly byte deviceId;

        // blade type 
        private BladeType bladeType;

        // Blade Sensor Data Record
        private ConcurrentDictionary<int, SensorMetadataBase> sensorMetaData = new ConcurrentDictionary<int, SensorMetadataBase>();

        // Sensor One Metadata Record
        private ConcurrentDictionary<int, FullSensorRecord> pwmSensorMetaData = new ConcurrentDictionary<int, FullSensorRecord>();

        #endregion

        # region Internal Variables

        /// <summary>
        /// blade Device Id
        /// </summary>
        internal byte DeviceId
        {
            get
            {
                return this.deviceId;
            }
        }

        /// <summary>
        /// Blade Guid
        /// </summary>
        internal Guid BladeGuid
        {
            get
            {
                lock (cacheLock)
                {
                    return this.bladeGuid;
                }
            }
        }

        /// <summary>
        /// Blade Device Type
        /// </summary>
        internal BladeType BladeClassification
        {
            get
            {
                lock (cacheLock)
                {
                    return this.bladeType;
                }
            }
        }

        /// <summary>
        /// Ipmi consecutive communication error counter.
        /// </summary>
        internal uint CommError
        {
            get
            {
                lock (cacheLock)
                {
                    return this.errCnt;
                }
            }
            private set
            {
                lock (cacheLock)
                {
                    // acceptable range 0 - 2,147,483,647 (integer)
                    { this.errCnt = (value > 2147483647 ? 0 : value); }
                }
            }
        }

        /// <summary>
        /// Sensor Data Record
        /// </summary>
        internal ConcurrentDictionary<int, SensorMetadataBase> SensorDataRecords
        {
            get { return this.sensorMetaData; }
        }

        /// <summary>
        /// Sensor Data Record
        /// </summary>
        private ConcurrentDictionary<int, FullSensorRecord> PwmSensorMetaData
        {
            get { return this.pwmSensorMetaData; }
        }

        #endregion

        #region Ipmi Escape Framing

        /// <summary>
        /// Replace serial framing charactors on outbound payload with 
        /// substatute byte sequence: 
        ///         IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        ///         IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        public byte[] ReplaceFrameChars(byte[] payload)
        {
            // initialize dictionary for tracking positions of frame charactors
            SortedDictionary<int, EscapeCharactor> instances = new SortedDictionary<int, EscapeCharactor>();

            // generate list for tracking positions
            List<int> positions = new List<int>();

            // array resize increase
            int len = 0;

            // array indexer
            int index = 0;

            // array offset
            int offset = 0;

            // array incrementer
            int increase = 0;

            // iterate the frame charactors
            foreach (EscapeCharactor esc in escChars)
            {
                // use IndexOf to detect a single occurance of the frame charactor
                // if a single instance is detected, search for more.
                if (IpmiSharedFunc.GetInstance(payload, esc.Frame) >= 0)
                {
                    // list all positions of the frame char
                    positions = GetFramePositions(payload, esc.Frame);

                    // for each position found, added it to the dictionary
                    // for tracking the bit.
                    foreach (int occurance in positions)
                    {
                        instances.Add(occurance, esc);
                    }
                }
            }

            // if instances of frame charactors have been found
            // enter into the replacement method.
            if (instances.Count > 0)
            {
                len = (payload.Length + instances.Count);
                byte[] newPayload = new byte[len];
                {
                    // reset indexers
                    index = 0; offset = 0; increase = 0;
                    foreach (KeyValuePair<int, EscapeCharactor> esc in instances)
                    {
                        // copy in the original byte array, up to the first frame char
                        Buffer.BlockCopy(payload, index, newPayload, offset, (esc.Key - index));

                        // set offset + byte offset 
                        // every pass adds 1 byte to increase
                        offset = esc.Key + increase;

                        // copy in the replacement escape charactor array.
                        Buffer.BlockCopy(esc.Value.Replace, 0, newPayload, offset, esc.Value.Replace.Length);

                        // add 1 byte to the offset, as byte 1 
                        // in esc.Value.replace always overwrites,
                        // payload[index]
                        increase++;

                        // offset + 2 byte offset
                        offset = (esc.Key + increase + 1);

                        // add 1 to index, to index past itself.
                        index = (esc.Key + 1);
                    }
                    // copy remaining bytes into the new array
                    Buffer.BlockCopy(payload, index, newPayload, offset, (payload.Length - index));
                }

                // copy the remaining payload bytes.
                payload = newPayload;
            }

            return payload;
        }

        /// <summary>
        /// Replace serial escape charactors on received payload with 
        /// substatute byte sequence: 
        ///         IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        ///         IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        public byte[] ReplaceEscapeChars(byte[] payload)
        {
            // initialize dictionary for tracking positions of escape charactors
            SortedDictionary<int, EscapeCharactor> instances = new SortedDictionary<int, EscapeCharactor>();

            // generate list for tracking positions
            List<int> positions = new List<int>();

            // array resize increase
            int len = 0;

            // array indexer
            int index = 0;

            // array offset
            int offset = 0;

            // iterate the escape charactors
            foreach (EscapeCharactor esc in escChars)
            {
                // use IndexOf to detect a single occurance of the escape charactor
                // if a single instance is detected, search for more.
                if (IpmiSharedFunc.GetInstance(payload, esc.Replace) >= 0)
                {
                    // list all positions of the escape char
                    positions = GetEscapePositions(payload, esc.Replace);

                    // for each position found, added it to the dictionary
                    // for tracking the bit.
                    foreach (int occurance in positions)
                    {
                        instances.Add(occurance, esc);
                    }
                }
            }

            // if instances of escape charactors have been found
            // enter into the replacement method.
            if (instances.Count > 0)
            {
                // lenght is payload minus the count of two byte escape sequences.
                len = (payload.Length - instances.Count);
                byte[] newPayload = new byte[len];
                {
                    // reset indexers
                    index = 0; offset = 0;
                    foreach (KeyValuePair<int, EscapeCharactor> esc in instances)
                    {
                        // copy in the original byte array, up to the first escape char
                        Buffer.BlockCopy(payload, index, newPayload, offset, (esc.Key - index));

                        // increment offset the size of bytes copied
                        offset += (esc.Key - index);

                        // increase the index based the 2 byte escape sequence
                        index = (esc.Key + 2);

                        // replace escape charactors with frame charactor
                        newPayload[offset] = esc.Value.Frame;

                        // increase the offset for this new byte
                        offset++;
                    }

                    // copy remaining bytes into the new array
                    Buffer.BlockCopy(payload, index, newPayload, offset, (payload.Length - index));
                }

                // copy the remaining payload bytes.
                payload = newPayload;
            }

            return payload;
        }

        /// <summary>
        /// Detect escape charactors in payload
        /// </summary>
        /// <param name="payload">ipmi unframed payload</param>
        /// <param name="pattern">escape pattern</param>
        /// <returns>List of position integers</returns>
        private List<int> GetEscapePositions(byte[] payload, byte[] pattern)
        {
            List<int> indexes = new List<int>();

            // remove 1 from payload.lenght to avoid buffer overrun.
            for (int i = 0; i < (payload.Length - 1); i++)
            {
                if (pattern[0] == payload[i] && pattern[1] == payload[i + 1])
                {
                    indexes.Add(i);
                }
            }
            return indexes;
        }

        /// <summary>
        /// Detect escape charactors in payload
        /// </summary>
        /// <param name="payload">ipmi unframed payload</param>
        /// <param name="pattern">escape pattern</param>
        /// <returns>List of position integers</returns>
        private List<int> GetFramePositions(byte[] payload, byte pattern)
        {
            List<int> indexes = new List<int>();

            int index = 0, pos = 0;

            for (int i = 0; i < payload.Length; i++)
            {
                // returns -1 when index is not found
                pos = IpmiSharedFunc.GetInstance(payload, pattern, index);

                index = (pos + 1);

                if (pos >= 0)
                    indexes.Add(pos);

                if (pos == -1)
                    break;
            }
            return indexes;
        }

        /// <summary>
        /// Add Start & Stop Serial Framing Charactors.
        /// </summary>
        public void AddStartStopFrame(ref byte[] payload)
        {
            payload[0] = startByte;
            payload[(payload.Length - 1)] = stopByte;
        }

        #endregion

        #region Close, LogOff & Dispose

        /// <summary>
        /// Closes the connection to the BMC device. This is the preferred method of closing any open 
        /// connection.
        /// </summary>
        public void Close()
        {
            this.Close(false);
        }

        /// <summary>
        /// Closes the connection to the BMC device. This is the preferred method of closing any open 
        /// connection.
        /// </summary>
        /// <param name="hardClose">
        /// true to close the socket without closing the IPMI session; otherwise false.
        /// </param>
        public void Close(bool hardClose)
        {
            if (hardClose == false)
            {
                this.LogOff();
            }

            this.SetClientState(IpmiClientState.Disconnected);
        }

        ~WcsBladeClient()
        {
            this.Close(true);

        }

        /// <summary>
        /// End an authenticated session with the BMC.
        /// </summary>
        public void LogOff()
        {
            uint session = this.GetSession();

            if (session != 0)
            {
                this.IpmiSendReceive(
                    new CloseSessionRequest(session),
                    typeof(CloseSessionResponse), false);
                SetSession(0);
            }
        }

        #endregion

        #region Connect & Logon

        /// <summary>
        /// Connects the client to the serial ipmi bmc on specified computer.
        /// </summary>
        /// <param name="hostName">Host computer to access via ipmi over serial.</param>
        public bool Initialize()
        {
            Tracer.WriteInfo("Initializing Blade {0}", DeviceId);
            bool guidChanged = false;

            DeviceGuid deviceGuid = this.SysPriGetSystemGuid();

            if (deviceGuid.CompletionCode == (byte)CompletionCode.Success)
            {
                if (bladeGuid == null)
                {
                    lock (cacheLock)
                        this.bladeGuid = deviceGuid.Guid;
                }
                else
                {
                    lock (cacheLock)
                    {
                        if (!deviceGuid.Guid.Equals(this.bladeGuid))
                        {
                            guidChanged = true;
                            this.bladeGuid = deviceGuid.Guid;
                        }
                    }
                }

                if (guidChanged)
                {
                    // clear full sensor meta data
                    SensorDataRecords.Clear();

                    // clear pwm sensor meta data
                    PwmSensorMetaData.Clear();
                }

                // Attempt to Identify the blade. (Note: This command by default does not allow retry).
                ChannelAuthenticationCapabilities response = this.GetAuthenticationCapabilities(PrivilegeLevel.Administrator, PriorityLevel.System, false);

                if (response.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Auxilary data is used to identify the blade
                    BladeType bladeType;

                    if (Enum.IsDefined(typeof(BladeType), response.AuxiliaryData))
                    {
                        bladeType = (BladeType)response.AuxiliaryData;
                    }
                    else
                    {
                        bladeType = BladeType.Unknown;
                    }

                    // indicates whether device has guid
                    bool hasGuid = false;

                    // Set the blade Type.                
                    lock (cacheLock)
                    {
                        this.bladeType = bladeType;
                    }

                    // IEB blade = 0x06, Storage blade = 0x05, Compute blade = 0x04.
                    switch (bladeType)
                    {
                        case BladeType.Server:
                            {
                                this.SetClientState(IpmiClientState.AuthenticatingChallenge);

                                // Step 1: Log On
                                bool logon = LogOn();

                                // Step 3: Signal Blade as initialized
                                if (logon)
                                {
                                    // client state set in logon method
                                    // signal initialization was completed.
                                    Tracer.WriteInfo("Blade " + this.DeviceId.ToString() + " initialized = true");

                                    return true;
                                }
                                else
                                {
                                    Tracer.WriteInfo("Blade " + this.DeviceId.ToString() + " initialized = false");

                                    return false;
                                }
                            }
                        case BladeType.Jbod: // Storage Blade.
                            // Get the blade guid
                            {
                                this.SetClientState(IpmiClientState.AuthenticatingChallenge);

                                if (deviceGuid.CompletionCode == (byte)CompletionCode.Success)
                                {
                                    hasGuid = true;
                                }

                                if (hasGuid)
                                {
                                    this.SetClientState(IpmiClientState.Authenticated);

                                    Tracer.WriteInfo(this.DeviceId.ToString() + " Initialized = true");

                                    return true;
                                }
                                else
                                {
                                    Tracer.WriteInfo(this.DeviceId.ToString() + " Initialized = false, could not get Guid");

                                    return false;
                                }
                            }
                        default:
                            Tracer.WriteError("Unknown Device Type, ChannelAuthenticationCapabilities failed for device Id: {0} Method: {1} Ipmi CompletionCode {2}", DeviceId.ToString(),
                                typeof(ChannelAuthenticationCapabilities).ToString(), IpmiSharedFunc.ByteToHexString(response.CompletionCode));
                            return false;
                    }
                }
                else
                {
                    // signal initializaiton failed
                    this.SetClientState(IpmiClientState.Invalid);
                    Tracer.WriteError("Device Initialization failed for device Id: {0} Method: {1} Ipmi CompletionCode {2}", DeviceId.ToString(),
                        typeof(ChannelAuthenticationCapabilities).ToString(), IpmiSharedFunc.ByteToHexString(response.CompletionCode));

                    return false;
                }
            }
            else
            {
                // Set the blade Type.                
                lock (cacheLock)
                {
                    this.bladeType = BladeType.Unknown;
                }

                return false;
            }

        }

        /// <summary>
        /// Start an authenticated session with the BMC.
        /// </summary>
        public bool LogOn(PriorityLevel priority = PriorityLevel.User)
        {
            bool response = true;

            try
            {
                if (base.IpmiUserId == null || base.IpmiPassword == null)
                {
                    Tracer.WriteError(this.DeviceId, DeviceType.Server, new NullReferenceException("IpmiUserId & IpmiPassword"));
                    // set to empty string
                    base.IpmiUserId = string.Empty;
                    // set to empty password
                    base.IpmiPassword = string.Empty;
                }


                // set the Ipmi Privilege level
                base.IpmiPrivilegeLevel = PrivilegeLevel.Administrator;

                // session challenge. This command does not allow retry.
                GetSessionChallengeResponse challenge =
                    (GetSessionChallengeResponse)this.IpmiSendReceive(
                        new GetSessionChallengeRequest(AuthenticationType.Straight, base.IpmiUserId),
                        typeof(GetSessionChallengeResponse), priority, false);

                if (challenge.CompletionCode == (byte)CompletionCode.Success)
                {

                    // set client state to session challenge
                    this.SetClientState(IpmiClientState.SessionChallenge);

                    // ipmi authentication code / user password logon.
                    byte[] authCode = IpmiSharedFunc.AuthCodeSingleSession(challenge.TemporarySessionId,
                                                                                challenge.ChallengeStringData,
                                                                                AuthenticationType.Straight,
                                                                                base.IpmiPassword);

                    // Session Activation.See: IPMI Table   22-21, Activate Session Command
                    // Note: This command does not allow re-try
                    ActivateSessionResponse activateResponse =
                        (ActivateSessionResponse)this.IpmiSendReceive(
                            new ActivateSessionRequest(AuthenticationType.Straight, base.IpmiPrivilegeLevel, authCode, 1),
                            typeof(ActivateSessionResponse), priority, false);

                    if (activateResponse.CompletionCode != 0)
                    {
                        response = false;
                    }

                    // set the session id for the remainder of the session
                    this.SetSession(activateResponse.SessionId);

                    // initialize the ipmi message sequence number to zero
                    ResetReqSeq();

                    // set client state to authenticated. client state
                    // is used for socket and RMCP payload type control
                    this.SetClientState(IpmiClientState.Authenticated);

                    // set session privilege level. This command does not allow retry by default
                    SetSessionPrivilegeLevelResponse privilege =
                        (SetSessionPrivilegeLevelResponse)this.IpmiSendReceive(
                        new SetSessionPrivilegeLevelRequest(PrivilegeLevel.Administrator),
                        typeof(SetSessionPrivilegeLevelResponse), priority, false);

                    if (privilege.CompletionCode != 0)
                    {
                        response = false;
                    }
                }
                else
                {
                    response = false;

                    // Trace the info for the state.
                    Tracer.WriteInfo(response.GetType() + " Failed State: " + base.ClientState.ToString());

                    // client failed to connect, session lost.
                    this.SetClientState(IpmiClientState.Disconnected);
                }

            }
            catch (Exception ex)
            {
                response = false;

                // Trace the info for the state.
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "LogOn", ex.ToString()));
            }

            return response;
        }

        #endregion

        #region Send/Receive

        /// <summary>
        /// Generics method IpmiSendReceive for easier use
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ipmiRequest"></param>
        /// <returns></returns>
        internal override T IpmiSendReceive<T>(IpmiRequest ipmiRequest)
        {
            return (T)this.IpmiSendReceive(ipmiRequest, typeof(T));
        }

        internal override IpmiResponse IpmiSendReceive(IpmiRequest ipmiRequest, Type responseType, bool allowRetry = true)
        {
            return this.IpmiSendReceive(ipmiRequest, responseType, PriorityLevel.User, allowRetry);
        }

        /// <summary>
        /// Send Receive Ipmi messages
        /// </summary>
        private IpmiResponse IpmiSendReceive(IpmiRequest ipmiRequest, Type responseType, PriorityLevel priority, bool allowRetry = true)
        {
            // Get the request sequence.  This should be incremented
            // for every request/response pair.
            byte reqSeq = GetReqSeq();

            // Serialize the IPMI request into bytes.
            byte[] ipmiRequestMessage = this.ReplaceFrameChars(ipmiRequest.GetBytes(IpmiTransport.Serial, reqSeq));

            // inject start/stop frame bytes.
            AddStartStopFrame(ref ipmiRequestMessage);

            byte[] messageResponse = { };
            byte[] ipmiResponseMessage = { };
            byte completionCode = (byte)CompletionCode.UnspecifiedError; // Initialize as non-zero (0xff = Unspecified Error).

            // Send the ipmi message over serial.
            CommunicationDevice.SendReceive(priority, (byte)DeviceType.Server, deviceId, ipmiRequestMessage, out messageResponse);

            // format the received message
            ProcessReceivedMessage(messageResponse, out ipmiResponseMessage, out completionCode);

            string message = string.Format("DeviceId: {0} Ipmi Send Cmd: {1} Cmd Bytes: {2} Ipmi Response {3} IPMI Response Cmd: {4} ",
            DeviceId,
            ipmiRequest.GetType().ToString(),
            IpmiSharedFunc.ByteArrayToHexString(ipmiRequestMessage),
            IpmiSharedFunc.ByteArrayToHexString(messageResponse),
            IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));

            Tracer.WriteIpmiInfo(message);

            // messageResponse no longer needed.  data copied to ipmiResponseMessage by ProcessReceivedMessage().
            messageResponse = null;


            // Create the response based on the provided type.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            IpmiResponse ipmiResponse = (IpmiResponse)constructorInfo.Invoke(new Object[0]);

            // check serial protocol completion code
            if (completionCode == (byte)CompletionCode.Success)
            {
                // if serial protocol completion code is successful (0x00).
                // set the packet response completion code to be the ipmi
                // completion code.
                ipmiResponse.CompletionCode = ipmiResponseMessage[7];                
            }
            else
            {
                // if the ipmi request reported a time-out response, it is
                // possible the session was terminated unexpectedly.  try to
                // re-establish the session.
                if (completionCode == (byte)CompletionCode.IpmiTimeOutHandShake && allowRetry)
                {
                    // Issue a Retry
                    return LoginRetry(ipmiRequest, responseType, completionCode, priority);
                }
                else
                {
                    // if the Chassis Manager completion code is
                    // unsuccessful, set the ipmi completion code
                    // to the Chassis Manager completion code.
                    ipmiResponse.CompletionCode = completionCode;
                }
            }

            // Initialize response message if IPMI request processed successfully
            // Get Power Limit can also return completion code 0x80 for a normal response. Handle it here.
            if (ipmiResponse.CompletionCode == (byte)CompletionCode.Success ||
                (ipmiRequest.GetType() == typeof(GetDcmiPowerLimitRequest) && ipmiResponse.CompletionCode == 0x80))
            {
                try
                {
                    ipmiResponse.Initialize(IpmiTransport.Serial, ipmiResponseMessage, ipmiResponseMessage.Length, reqSeq);
                    ipmiResponseMessage = null; // response message nolonger needed
                    // reset the communication error counter.
                    CommError = 0;
                }
                catch (Exception ex)
                {
                    // set an exception code for invalid data in ipmi data field, as the packet could
                    // not be converted by the InitializeSerial method.
                    ipmiResponse.CompletionCode = (byte)CompletionCode.IpmiInvalidDataFieldInRequest;

                    message = string.Format("DeviceId: {0} Ipmi Send Cmd: {1} Cmd Bytes: {2} Ipmi Response {3} Exception: {4} ",
                    DeviceId,
                    ipmiRequest.GetType().ToString(),
                    IpmiSharedFunc.ByteArrayToHexString(ipmiRequestMessage),
                    IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage),
                    ex.ToString());

                    Tracer.WriteError(message);
                }
            }
            else if (ipmiResponse.CompletionCode == (byte)CompletionCode.IpmiCmdFailedInsufficientPrivLevel && allowRetry) // Catch Ipmi prevelege loss and perform login retry.
            {
                // Issue a re-logon and command retry as Ipmi completion code 
                // D4h indicates session prevelege level issue.
                return LoginRetry(ipmiRequest, responseType, ipmiResponse.CompletionCode, priority);
            }
            else
            {
                // throw ipmi/dcmi response exception with a custom string message and the ipmi completion code
                message = string.Format("DeviceId: {0} Request Type: {1} Response Packet: {2} Completion Code {3}", DeviceId, ipmiRequest.GetType().ToString(),
                    IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage), IpmiSharedFunc.ByteToHexString(ipmiResponse.CompletionCode));

                Tracer.WriteError(message);
            }

            // Response to the IPMI request message.
            return ipmiResponse;
        }

        /// <summary>
        /// Ipmi Send Message
        /// </summary>
        private Dictionary<byte, CompletionCode> IpmiAsyncSend(IpmiRequest ipmiRequest, PriorityLevel priority)
        {

            Dictionary<byte, CompletionCode> response = new Dictionary<byte, CompletionCode>(ConfigLoaded.Population);

            // Get the request sequence.  This should be incremented
            // for every request/response pair.
            byte reqSeq = GetAsycReqSeq();

            // Serialize the IPMI request into bytes.
            byte[] ipmiRequestMessage = this.ReplaceFrameChars(ipmiRequest.GetBytes(IpmiTransport.Serial, reqSeq));

            // inject start/stop frame bytes.
            AddStartStopFrame(ref ipmiRequestMessage);

            Tracer.WriteIpmiInfo(ipmiRequest.GetType().ToString());
            Tracer.WriteIpmiInfo(IpmiSharedFunc.ByteArrayToHexString(ipmiRequestMessage));

            byte[] messageResponse = { };
            byte[] ipmiResponseMessage = { };

            // Send the ipmi mssage over serial.
            CommunicationDevice.SendReceive(priority, (byte)DeviceType.Server, this.DeviceId, ipmiRequestMessage, out messageResponse);

            // enumerate responses and add to dictonary.
            for (int i = 0; i < messageResponse.Length; i++)
            {
                response.Add(messageResponse[i], (CompletionCode)messageResponse[i + 1]);
                i++;
            }

            return response;
        }

        /// <summary>
        /// Attempts to re-authenticate with the BMC if the session is dropped.
        /// </summary>
        private IpmiResponse LoginRetry(IpmiRequest ipmiRequest, Type responseType, byte completionCode, PriorityLevel priority)
        {
            CommError++;

            Tracer.WriteWarning(string.Format("DeviceId: {0} WcsBladeClient Ipmi LoginRetry for command {1}. Blade device retry counter: {2}",
                                DeviceId, ipmiRequest.GetType().ToString(),
                                CommError));

            // return resposne
            IpmiResponse response;

            // Attempt to Identify the blade.  (Note: This command does not allow re-try)
            ChannelAuthenticationCapabilities auth = this.GetAuthenticationCapabilities(PrivilegeLevel.Administrator, priority, false);

            // if get channel authentication succeeds, check if the blade is a compute blade.  If so, re-establish
            // the session and re-execute the command
            if (auth.CompletionCode == (byte)CompletionCode.Success)
            {

                // Auxilary data is used to identify the blade
                BladeType bladeType;

                if (Enum.IsDefined(typeof(BladeType), auth.AuxiliaryData))
                {
                    bladeType = (BladeType)auth.AuxiliaryData;
                }
                else
                {
                    bladeType = BladeType.Unknown;
                }

                // Set the blade Type.                
                lock (cacheLock)
                {
                    this.bladeType = bladeType;
                }

                // re-issue original command.                   
                response = IpmiSendReceive(ipmiRequest, responseType, priority, false);

                // if timing-out the issue maybe session releated caveat, 
                // GetSessionInfo cannot be checked if there is no session!
                if (response.CompletionCode == (byte)CompletionCode.IpmiTimeOutHandShake ||
                    response.CompletionCode == (byte)CompletionCode.IpmiCmdFailedInsufficientPrivLevel)
                {
                    // if compute blade, try logon and retry.
                    if (auth.AuxiliaryData == (byte)BladeType.Server)
                    {
                        this.SetClientState(IpmiClientState.Connecting);

                        // login back in.
                        this.LogOn(priority);

                        // re-issue original command.                   
                        return response = IpmiSendReceive(ipmiRequest, responseType, priority, false);
                    }
                }
                else
                {
                    return response;
                }
            }

            // re-create the original response and return it.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            response = (IpmiResponse)constructorInfo.Invoke(new Object[0]);
            // set the original response code.
            response.CompletionCode = completionCode;

            return response;
        }

        /// <summary>
        /// Process packed received from serial transport class.
        /// </summary>
        /// <param name="message">Message bytes.</param>
        private void ProcessReceivedMessage(byte[] message, out byte[] ipmiResponseMessage, out byte completionCode)
        {
            completionCode = message[0];

            // check completion code
            if (completionCode == (byte)CompletionCode.Success && message.Length > 3)
            {
                // strip the 3 byte validation message received from the 
                // transport class.
                ipmiResponseMessage = new byte[message.Length - 3];

                // copy response packet into response array
                Buffer.BlockCopy(message, 3, ipmiResponseMessage, 0, (message.Length - 3));
                message = null;

                // Ipmi message header is 7 bytes.
                if (ipmiResponseMessage.Length >= 7)
                {
                    // check resAddr
                    if (ipmiResponseMessage[1] == 0x8F || ipmiResponseMessage[1] == 0x81)
                    {
                        // replace escape characters
                        ipmiResponseMessage = this.ReplaceEscapeChars(ipmiResponseMessage);
                        // Validate checksum before passing packet as valid.
                        if (!ValidateCRC(ipmiResponseMessage))
                        {
                            completionCode = (byte)CompletionCode.IpmiCmdFailedIllegalParameter;
                        }
                    }
                    else
                    {
                        completionCode = (byte)CompletionCode.CannotReturnRequestedDataBytes;
                        Tracer.WriteError("Response did contain ipmi packet {0}", IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                    }

                }
                else
                {
                    completionCode = (byte)CompletionCode.IpmiInvalidRequestDataLength;
                    Tracer.WriteError("Response did contain ipmi packet {0}", IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                }
            }
            else
            {
                ipmiResponseMessage = message;

                if (completionCode != 0)
                {
                    Tracer.WriteError("Malformed Packet Length. No Ipmi payload: {0}", IpmiSharedFunc.ByteArrayToHexString(message));
                }
                else
                {
                    Tracer.WriteError("Invalid response received: {0}", IpmiSharedFunc.ByteArrayToHexString(message));
                }
            }
        }

        /// <summary>
        /// Validate the payload checksum.  The function code checksum
        /// and rqAdd is not important to the serial client.
        /// </summary>
        private bool ValidateCRC(byte[] message)
        {
            byte checksum = IpmiSharedFunc.TwoComplementChecksum(4, (message.Length - 2), message);

            // Compare checksum
            if (message[(message.Length - 2)] == checksum)
            {
                return true;
            }
            else
            {
                Tracer.WriteWarning("CheckSum Mismatch: " + Ipmi.IpmiSharedFunc.ByteArrayToHexString(message) + " Checksum: " + checksum);
                return false;
            }
        }

        /// <summary>
        /// Client Connection Status
        /// </summary>
        protected override void SetClientState(IpmiClientState newClientState)
        {
            IpmiClientState status = GetClientState();
            if (status != newClientState)
            {
                Tracer.WriteInfo("Blade client State Changed from {0} to {1}", status.ToString(), newClientState.ToString());

                lock (stateLock)
                {
                    this.status = newClientState;
                }
            }
        }

        #endregion

        #region ThreadSafe Methods

        /// <summary>
        /// Returns the state of the Current Client.
        /// State locks are used for debugging.
        /// 
        /// </summary>
        public IpmiClientState GetClientState()
        {
            lock (stateLock)
                return this.status;
        }

        /// <summary>
        /// Clear Blade Device Type
        /// </summary>
        internal void ClearBladeClassification()
        {
            lock (cacheLock)
            {
                this.bladeType = BladeType.Unknown;
            }
        }

        /// <summary>
        /// Gets a unique ReqSeq for each Ipmi message
        /// </summary>
        private byte GetReqSeq()
        {
            lock (reqSeqLock)
            {
                return base.IpmiRqSeq++;
            }
        }

        /// <summary>
        /// Gets a unique ReqSeq for Async Ipmi message
        /// </summary>
        private byte GetAsycReqSeq()
        {
            lock (reqSeqLock)
            {
                return this.IpmiAsycRqSeq++;
            }
        }

        /// <summary>
        /// Resets the ReqSeq to zero and return it.
        /// </summary>
        private void ResetReqSeq()
        {
            lock (reqSeqLock)
            {
                base.IpmiRqSeq = 1;
            }
        }

        /// <summary>
        /// Gets the current session number
        /// </summary>
        private uint GetSession()
        {
            lock (cacheLock)
            {
                return this.sessionId;
            }
        }

        /// <summary>
        /// Resets the session back to zero
        /// </summary>
        private uint SetSession(uint number = 0)
        {
            lock (cacheLock)
            {
                this.sessionId = number;
                return number;
            }
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Initialize class specifying device address
        /// </summary>
        public WcsBladeClient(byte deviceId)
        {
            this.deviceId = deviceId;
        }

        /// <summary>
        /// Initialize class specifying device address
        /// </summary>
        public WcsBladeClient(byte deviceId, string userName, string password)
        {
            this.deviceId = deviceId;
            base.IpmiUserId = userName;
            base.IpmiPassword = password;
        }

        #endregion

        #region SDR Support

        /// <summary>
        /// Create local copy of Sdr, in threadsafe concurrent dictionary
        /// </summary>
        private void GetSensorDataRecords()
        {
            if (!SensorDataRecords.IsEmpty)
                SensorDataRecords.Clear();

            try
            {
                int sdrKey;
                // This command does not allow logon retry.
                foreach (SensorMetadataBase record in this.GetSensorMetaData(PriorityLevel.System, false))
                {
                    // Concat Sensor Owner ID and Sensor Number to create unique identifier for sensor
                    sdrKey = (record.SensorOwnerId << 8) | record.SensorNumber;
                    SensorDataRecords.AddOrUpdate(sdrKey, record, (key, oldValue) => record);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSensorDataRecords", ex.ToString()));
            }
        }

        /// <summary>
        /// Create local copy of Sdr for sensor 1 in threadsafe concurrent dictionary
        /// </summary>
        private void GetFirstSensorDataRecord()
        {
            if (!PwmSensorMetaData.IsEmpty)
                PwmSensorMetaData.Clear();

            try
            {
                int sdrKey;
                // should only add full sensor data records. This command does not allow logon retry.
                foreach (FullSensorRecord record in this.GetFirstSdr(PriorityLevel.System, true))
                {
                    // Concat Sensor Owner ID and Sensor Number to create unique identifier for sensor
                    sdrKey = (record.SensorOwnerId << 8) | record.SensorNumber;
                    PwmSensorMetaData.AddOrUpdate(sdrKey, record, (key, oldValue) => record);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetFirstSensorDataRecord", ex.ToString()));
            }
        }

        /// <summary>
        /// Get Sensor Description 
        /// </summary>
        public string GetSensorDescription(byte SensorOwnerId, byte SensorNumber)
        {
            string unknownSensor = "Unknown";
            int sdrKey = (SensorOwnerId << 8) | SensorNumber;

            if ((SensorOwnerId == (byte)IpmiSensorOwnerId.Bmc) && (SensorNumber == 0x01))
            {
                if (PwmSensorMetaData.Count <= 0)
                    GetFirstSensorDataRecord();

                FullSensorRecord sdr;
                if (this.PwmSensorMetaData.TryGetValue(sdrKey, out sdr))
                {
                    return sdr.Description;
                }
                else
                {
                    return unknownSensor;
                }
            }
            else
            {
                if (SensorDataRecords.Count <= 0)
                    GetSensorDataRecords();

                SensorMetadataBase sdr;
                if (this.SensorDataRecords.TryGetValue(sdrKey, out sdr))
                {
                    return sdr.Description;
                }
                else
                {
                    return unknownSensor;
                }
            }
        }

        /// <summary>
        /// Get Sensor Reading 
        /// </summary>
        public SensorReading GetSensorReading(byte SensorNumber, PriorityLevel priority = PriorityLevel.User)
        {
            // Sensor Owner is BMC for Get Sensor Reading
            int sdrKey = (byte)IpmiSensorOwnerId.Bmc << 8 | SensorNumber;

            if (SensorNumber == 0x01)
            {
                if (PwmSensorMetaData.Count <= 0)
                    GetFirstSensorDataRecord();

                FullSensorRecord sdr;
                if (this.PwmSensorMetaData.TryGetValue(sdrKey, out sdr))
                {
                    return this.GetSensorReading(SensorNumber, sdr.RawSensorType, priority);
                }
                else
                {
                    return new SensorReading((byte)CompletionCode.IpmiInvalidDataFieldInRequest);
                }
            }
            else
            {
                if (SensorDataRecords.Count <= 0)
                    GetSensorDataRecords();

                SensorMetadataBase sdr;
                if (this.SensorDataRecords.TryGetValue(sdrKey, out sdr))
                {
                    return this.GetSensorReading(SensorNumber, sdr.RawSensorType, priority);
                }
                else
                {
                    return new SensorReading((byte)CompletionCode.IpmiInvalidDataFieldInRequest);
                }
            }
        }

        /// <summary>
        /// Get Sensor Reading 
        /// </summary>
        public override SensorReading GetSensorReading(byte SensorNumber, byte SensorType)
        {
            // this method is require to override the base class.
            return this.GetSensorReading(SensorNumber, SensorType, PriorityLevel.User);
        }

        /// <summary>
        /// Get Sensor Reading 
        /// </summary>
        public SensorReading GetSensorReading(byte SensorNumber, byte SensorType, PriorityLevel priority = PriorityLevel.User)
        {
            try
            {
                // Get Event/Reading Type Code and read sensor
                SensorTypeCode typeCode = GetSensorType(SensorNumber);
                SensorReading reading = this.SensorReading(SensorNumber, typeCode.EventTypeCode, priority);

                // Sensor Owner is BMC for Get Sensor Reading
                int sdrKey = (byte)IpmiSensorOwnerId.Bmc << 8 | SensorNumber;

                if (reading.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Sensor number 1 should be PWM sensor, this should also be listed as the first
                    // sensor data record in the sdr.  It is an optimisation that this sensor is kept
                    // separately as it means the entire SDR does not need to be parsed upon initialization.
                    if (SensorNumber == 0x01)
                    {
                        if (PwmSensorMetaData.Count <= 0)
                            GetFirstSensorDataRecord();

                        FullSensorRecord sdr;
                        if (this.PwmSensorMetaData.TryGetValue(sdrKey, out sdr))
                        {
                            reading.ConvertReading(sdr);

                            reading.Description = sdr.Description;
                            // Get Event/Reading Description
                            reading.EventDescription = GetSensorStateString(SensorType, sdr.EventTypeCode, reading.EventState);
                        }
                    }
                    else
                    {
                        // if no cache exists, build it.
                        if (SensorDataRecords.Count <= 0)
                        {
                            GetSensorDataRecords();
                        }

                        SensorMetadataBase sdr;
                        if (this.SensorDataRecords.TryGetValue(sdrKey, out sdr))
                        {
                            if (sdr.GetType() == typeof(FullSensorRecord))
                                reading.ConvertReading(sdr);

                            if (SensorNumber == ConfigLoaded.InletSensor && ConfigLoaded.EnableInletOffSet)
                            {
                                BmcDeviceId deviceId = GetDeviceId(priority);

                                if (deviceId.CompletionCode == 0x00)
                                {
                                    WcsBladeIdentity id = new WcsBladeIdentity(deviceId.ManufacturerId, deviceId.ProductId);
                                    if (HelperFunction.InletTempOffsets.ContainsKey(id))
                                    {
                                        // deduct the ODM/OEM inlet sensor offset for accurate inlet sensor reporting
                                        reading.Reading = (reading.Reading - HelperFunction.InletTempOffsets[id].Offset);
                                    }
                                }
                            }

                            reading.Description = sdr.Description;
                            // Get Event Description
                            reading.EventDescription = GetSensorStateString(SensorType, sdr.EventTypeCode, reading.EventState);
                        }
                        else
                        {
                            AppendSensorTypeCode(SensorNumber, ref reading);
                        }
                    }
                }
                return reading;
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSensorReading", ex.ToString()));

                return new SensorReading((byte)CompletionCode.IpmiResponseNotProvided);
            }

        }

        /// <summary>
        /// Appends Sensor Type Code to Sensor Event Description.
        /// </summary>
        private void AppendSensorTypeCode(byte sensorNumber, ref SensorReading reading)
        {
            SensorTypeCode typeCode = GetSensorType(sensorNumber);

            if (typeCode.CompletionCode == 0x00)
            {
                reading.EventDescription = GetSensorStateString(typeCode.SensorType, typeCode.EventTypeCode, reading.EventState);
            }
        }

        /// <summary>
        /// Returns the event message corresponding to the Sensor Type,
        /// Event/Reading Type Code, and event state
        /// </summary>
        /// <returns></returns>
        private string GetSensorStateString(byte sensorType, byte eventTypeCode, byte eventState)
        {
            EventLogData evtData = new EventLogData();
            EventLogMsgType eventMsgType = EventLogMsgType.Unspecified;

            if (eventState == 0xFE)
            {
                // Reading unavailable
                evtData.EventMessage = "Sensor reading unavailable (entity not present or sensor update not complete)";
            }
            // Event/Reading Type Code Classification
            else if (eventTypeCode == 0x01)
            {
                // Threshold Sensors
                if ((eventState == 0xFF) || (eventState > 5))
                {
                    // No threshold state asserted in byte 4 of Get Sensor Reading
                    evtData.EventMessage = "No Threshold Event Asserted";
                }
                else
                {
                    // Map response status from Get Sensor Reading to the definitions
                    // in Table 42-, Generic Event/Reading Type Codes in the IPMI specification
                    byte mappedEventState = (byte)(eventState * 2);
                    if ((eventState >= 3) && (eventState <= 5))
                    {
                        mappedEventState++;
                    }
                    // Read and format the sensor event description. Display N/A for the trigger reading and threshold since we 
                    // do not get those values from the Get Sensor Reading command. The trigger reading and thresholds
                    // are available for SEL entries only.
                    eventMsgType = EventLogMsgType.Threshold;
                    evtData = ConfigLoaded.GetEventLogData(eventMsgType, (int)eventTypeCode, (int)mappedEventState);
                    evtData.EventMessage = string.Format(evtData.EventMessage, "N/A", "N/A");
                }
            }
            else if ((eventTypeCode >= 0x02) && (eventTypeCode <= 0x0C))
            {
                // Generic Discrete Sensors
                if ((eventState == 0xFF) || (eventState > 14))
                {
                    // No discrete state asserted in bytes 4 and 5 of Get Sensor Reading
                    evtData.EventMessage = "No Discrete State Asserted";
                }
                else
                {
                    // Read sensor event description
                    eventMsgType = EventLogMsgType.Discrete;
                    evtData = ConfigLoaded.GetEventLogData(eventMsgType, (int)eventTypeCode, (int)eventState);
                }
            }
            else if (eventTypeCode == 0x6f)
            {
                // Sensor-Specific Discrete Sensors
                if ((eventState == 0xFF) || (eventState > 14))
                {
                    // No discrete state asserted in bytes 4 and 5 of Get Sensor Reading
                    evtData.EventMessage = "No Sensor-Specific State Asserted";
                }
                else
                {
                    // Sensor-specific sensors use the sensor type
                    // as the Event/Reading type code.
                    eventTypeCode = sensorType;
                    eventMsgType = EventLogMsgType.SensorSpecific;
                    // Read sensor event description
                    evtData = ConfigLoaded.GetEventLogData(eventMsgType, (int)eventTypeCode, (int)eventState);
                }
            }

            return evtData.EventMessage;
        }

        #endregion

        #region Blade Hardware Status

        /// <summary>
        /// Partial Blade Info
        /// </summary>
        public BladeStatusInfo GetBladeInfo()
        {
            BladeStatusInfo response = new BladeStatusInfo((byte)CompletionCode.Success);

            response.BladeType = this.BladeClassification.ToString();

            response.DeviceId = this.DeviceId;

            try
            {
                DeviceGuid guid = this.GetSystemGuid(true);

                // if we can't get the guid we should get out.
                if (guid.CompletionCode == (byte)CompletionCode.Success)
                {
                    response.BladeGuid = guid.Guid;

                    SystemStatus pwrState = GetChassisState();
                    if (pwrState.CompletionCode == (byte)CompletionCode.Success)
                    {
                        response.PowerState = pwrState.PowerState.ToString();

                        if (pwrState.IdentitySupported)
                        {
                            response.LedStatus = pwrState.IdentityState.ToString();
                        }
                        else
                        {
                            response.LedStatus = IdentityState.Unknown.ToString();
                        }
                    }
                    else
                    {
                        response.CompletionCode = pwrState.CompletionCode;
                    }

                    BmcDeviceId id = this.GetDeviceId();

                    if (id.CompletionCode == (byte)CompletionCode.Success)
                    {
                        response.BmcFirmware = id.Firmware;
                    }
                    else
                    {
                        response.CompletionCode = id.CompletionCode;
                    }

                    FruDevice fruData = GetFruDeviceInfo(true);

                    if (fruData.CompletionCode == (byte)CompletionCode.Success)
                    {
                        response.SerialNumber = fruData.ProductInfo.SerialNumber.ToString();
                        response.AssetTag = fruData.ProductInfo.AssetTag.ToString();
                        response.HardwareVersion = fruData.ProductInfo.ProductName.ToString();
                        //response.location = fru.ProductInfo.
                    }
                    else
                    {
                        response.CompletionCode = fruData.CompletionCode;
                    }
                }
                else
                {
                    response.CompletionCode = guid.CompletionCode;
                }
            }
            catch (Exception ex)
            {
                if (response.CompletionCode == (byte)CompletionCode.Success)
                    response.CompletionCode = (byte)CompletionCode.UnspecifiedError;

                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetBladeInfo", ex.ToString()));
                return new BladeStatusInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }

            return response;
        }

        /// <summary>
        /// Hardware Info All
        /// </summary>
        public HardwareStatus GetHardwareInfo()
        {
            return this.GetHardwareInfo(true, true, true, true, true,
                                        true, true, true, true);
        }

        /// <summary>
        /// Get Hardware Information
        /// </summary>
        public HardwareStatus GetHardwareInfo(bool proc, bool mem, bool disk, bool me, bool temp, bool power, bool fru, bool pcie, bool misc)
        {
            try
            {
                // Get GUID has to succeed for this command to proceed,
                // otherwise we waste time waiting for blank slots to time out.
                DeviceGuid guid = this.GetSystemGuid(true);

                if (guid.CompletionCode == (byte)CompletionCode.Success)
                {
                    // Attempt to Identify the blade. (Note: This command by default does not allow retry).
                    ChannelAuthenticationCapabilities response = this.GetAuthenticationCapabilities(PrivilegeLevel.Administrator, PriorityLevel.User, true);

                    // Auxilary data is used to identify the blade
                    BladeType bladeType;

                    if (response.CompletionCode == (byte)CompletionCode.Success)
                    {

                        if (Enum.IsDefined(typeof(BladeType), response.AuxiliaryData))
                        {
                            bladeType = (BladeType)response.AuxiliaryData;
                        }
                        else
                        {
                            bladeType = BladeType.Unknown;
                        }
                    }
                    else
                    {
                        bladeType = BladeType.Unknown;
                    }

                    if (bladeType == BladeType.Server)
                    {
                        ComputeStatus hwStatus = new ComputeStatus(response.CompletionCode, proc, mem, fru, misc);

                        // add guid
                        hwStatus.BladeGuid = guid.Guid;

                        // add blade type
                        hwStatus.BladeType = bladeType.ToString();

                        // add device Id
                        hwStatus.DeviceId = this.DeviceId;

                        // temp sensors
                        List<SensorMetadataBase> tempSensors = new List<SensorMetadataBase>();

                        // processors numbers
                        List<byte> processor = new List<byte>();

                        // intel ME
                        List<SensorMetadataBase> meModule = new List<SensorMetadataBase>();

                        // Processor/Memory Sensors
                        List<SensorMetadataBase> hwSensors = new List<SensorMetadataBase>();

                        // Get Sensor Data Repository
                        if (SensorDataRecords.Count <= 0)
                            GetSensorDataRecords();

                        // Iterate over the SDR and collect sensors owned by BMC
                        foreach (KeyValuePair<int, SensorMetadataBase> sdr in SensorDataRecords)
                        {
                            if (sdr.Value.SensorOwnerId == (byte)IpmiSensorOwnerId.Bmc)
                            {
                                // Collect physical processor sensors so that we can get the processor information later
                                if (sdr.Value.EntityType == IpmiEntityType.Physical
                                    && sdr.Value.EntityId == IpmiEntityId.Processor)
                                {
                                    processor.Add((byte)sdr.Value.EntityInstance);
                                }

                                // Collect all sensors
                                if (sdr.Value.EntityType == IpmiEntityType.Physical
                                    && sdr.Value.EntityId == IpmiEntityId.MgmtCntrlFirmware)
                                {
                                    // ME sensors
                                    meModule.Add(sdr.Value);
                                }
                                else if ((sdr.Value.GetType() == typeof(FullSensorRecord) ||
                                          sdr.Value.GetType() == typeof(CompactSensorRecord))
                                         && sdr.Value.SensorType == SensorType.Temperature)
                                {
                                    // Temperature sensors. Sensors with full sensor records contain analog readings. 
                                    // Compact sensor records contain discrete states only.
                                    tempSensors.Add(sdr.Value);
                                }
                                else if (sdr.Value.EntityId != IpmiEntityId.Oem)
                                {
                                    // Other sensors, e.g. voltage, logging, power.
                                    // Do not read OEM sensors (ADR_Trigger, SM_Alert, NVDIMM)
                                    // since they are meant for SEL logging only
                                    hwSensors.Add(sdr.Value);
                                }
                            }
                        }

                        if (proc)
                        {
                            // Get Processor Information for each distinct Entity Instance
                            foreach (byte processNo in SupportFunctions.FilterDistinct<byte>(processor))
                            {
                                ProcessorInfo procinf = GetProcessorInfo(processNo);

                                hwStatus.ProcInfo.Add(processNo, procinf);

                                if (procinf.CompletionCode != (byte)CompletionCode.Success)
                                {
                                    hwStatus.PartialError = procinf.CompletionCode;
                                }
                            }
                        }

                        if (mem)
                        {
                            MemoryIndex memIndex = GetMemoryIndex();

                            if (memIndex.CompletionCode == 0)
                            {
                                for (int i = 0; i < memIndex.SlotCount; i++)
                                {
                                    int index = (i + 1);

                                    if (memIndex.PresenceMap[index])
                                    {
                                        MemoryInfo meminf = GetMemoryInfo((byte)index);
                                        hwStatus.MemInfo.Add((byte)index, meminf);

                                        if (meminf.CompletionCode != (byte)CompletionCode.Success)
                                        {
                                            hwStatus.PartialError = meminf.CompletionCode;
                                        }
                                    }
                                    else
                                    {
                                        MemoryInfo meminf = new MemoryInfo(0x00);
                                        meminf.SetParamaters(0x00, 0x00, 0x00, (byte)MemoryType.Unknown,
                                            0xff, (byte)MemoryStatus.NotPresent);
                                        hwStatus.MemInfo.Add((byte)index, meminf);
                                    }
                                }
                            }
                            else
                            {
                                hwStatus.PartialError = memIndex.CompletionCode;

                            }
                        }

                        if (pcie)
                        {
                            // Get PCIe presence map
                            PCIeMap pcieMap = GetPCIeMap();
                            
                            if (pcieMap.CompletionCode == (byte)CompletionCode.Success)
                            {
                                for (byte i = 1; i <= 16; i++)
                                {
                                    // Get PCIe information for slots that have devices
                                    if ((pcieMap.PresenceMap & (0x1 << (i - 1))) != 0)
                                    {
                                        PCIeInfo pcieInf = GetPCIeInfo(i);
                                        if (pcieInf.CompletionCode == (byte)CompletionCode.Success)
                                        {
                                            hwStatus.PcieInfo.Add(i, pcieInf);
                                        }
                                    }
                                }
                            }
                        }

                        if (me)
                        {
                            // add hardware sensors to the list.
                            foreach (SensorMetadataBase sensor in meModule)
                            {
                                SensorReading reading = GetSensorReading(sensor.SensorNumber, sensor.RawSensorType, PriorityLevel.User);

                                hwStatus.HardwareSdr.Add(new HardwareSensor(sensor, reading));

                                if (reading.CompletionCode != (byte)CompletionCode.Success)
                                {
                                    hwStatus.PartialError = reading.CompletionCode;
                                }
                            }
                        }

                        if (temp)
                        {
                            // Get Temp Information for each list entity.
                            foreach (SensorMetadataBase sensor in tempSensors)
                            {
                                SensorReading reading = GetSensorReading(sensor.SensorNumber, sensor.RawSensorType, PriorityLevel.User);

                                hwStatus.TempSensors.Add(new HardwareSensor(sensor, reading));

                                if (reading.CompletionCode != (byte)CompletionCode.Success)
                                {
                                    hwStatus.PartialError = reading.CompletionCode;
                                }
                            }
                        }

                        if (misc)
                        {
                            // Get hardware sensor readings
                            foreach (SensorMetadataBase sensor in hwSensors)
                            {
                                SensorReading reading = GetSensorReading(sensor.SensorNumber, sensor.RawSensorType, PriorityLevel.User);

                                hwStatus.HardwareSdr.Add(new HardwareSensor(sensor, reading));

                                if (reading.CompletionCode != (byte)CompletionCode.Success)
                                {
                                    hwStatus.PartialError = reading.CompletionCode;
                                }
                            }
                        }

                        if (power)
                        {
                            // Get Power Reading for Blade
                            List<PowerReading> readings = GetPowerReading();
                            if (readings[0].CompletionCode != (byte)CompletionCode.Success)
                            {
                                hwStatus.PartialError = readings[0].CompletionCode;
                            }

                            hwStatus.Power = readings[0];
                        }

                        if (fru)
                        {
                            AddFruData<ComputeStatus>(ref hwStatus, true);
                        }

                        return hwStatus;
                    }
                    else if (bladeType == BladeType.Jbod)
                    {
                        JbodStatus hwStatus = new JbodStatus(response.CompletionCode, proc, mem,
                            disk, fru, misc);

                        // add guid
                        hwStatus.BladeGuid = guid.Guid;

                        // add blade type
                        hwStatus.BladeType = bladeType.ToString();

                        // add device Id
                        hwStatus.DeviceId = this.DeviceId;

                        if (disk)
                        {
                            hwStatus.DiskStatus = GetDiskStatus();
                        }

                        if (temp)
                        {
                            hwStatus.DiskInfo = GetDiskInfo();
                        }

                        if (fru)
                        {
                            AddFruData<JbodStatus>(ref hwStatus, false);
                        }

                        return hwStatus;
                    }
                    else
                    {
                        return new UnknownBlade((byte)CompletionCode.UnknownBladeType);
                    }
                }
                else
                {
                    return new UnknownBlade(guid.CompletionCode);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetHardwareInfo", ex.ToString()));
                return new UnknownBlade((byte)CompletionCode.UnspecifiedError);
            }
        }

        /// <summary>
        /// appends fru data to the GetHardwareInfo command
        /// </summary>
        private void AddFruData<T>(ref T hwStatus, bool optimize) where T : HardwareStatus
        {
            // Get Fru Data
            FruDevice fruData = GetFruDeviceInfo(optimize);

            if (fruData.CompletionCode == (byte)CompletionCode.Success)
            {
                if (fruData.ProductInfo != null)
                {
                    hwStatus.SerialNumber = fruData.ProductInfo.SerialNumber.ToString();
                    hwStatus.AssetTag = fruData.ProductInfo.AssetTag.ToString();
                    hwStatus.HardwareVersion = fruData.ProductInfo.ProductName.ToString();
                }
            }
            else
            {
                hwStatus.PartialError = fruData.CompletionCode;
            }
        }

        #endregion

        #region Ipmi Commands

        /// <summary>
        /// Get Sensor Type for the IPMI Sensor.
        /// </summary>
        public override SensorTypeCode GetSensorType(byte sensorNumber)
        {
            try
            {
                return base.GetSensorType(sensorNumber);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSensorType", ex.ToString()));

                return new SensorTypeCode((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Fru Device info
        /// </summary>
        public override FruDevice GetFruDeviceInfo(bool maxLenght = false)
        {
            try
            {
                return base.GetFruDeviceInfo(0, maxLenght);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetFruDeviceInfo", ex.ToString()));

                return new FruDevice((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Queries BMC for the currently set boot device.
        /// </summary>
        /// <returns>Flags indicating the boot device.</returns>
        public override NextBoot GetNextBoot()
        {

            try
            {
                return base.GetNextBoot();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetNextBoot", ex.ToString()));

                return new NextBoot((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// The helper for several boot type setting methods, as they
        /// essentially send the same sequence of messages.
        /// </summary>
        /// <param name="bootType">The desired boot type.</param>
        public override NextBoot SetNextBoot(BootType bootType, bool uefi, bool persistent, byte bootInstance = 0x00, bool requireCommit = false)
        {

            try
            {
                return base.SetNextBoot(bootType, uefi, persistent, bootInstance, requireCommit);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetNextBoot", ex.ToString()));

                return new NextBoot((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Write Fru Data Command.  Note:
        ///     The command writes the specified byte or word to the FRU Inventory Info area. This is a ‘low level’ direct 
        ///     interface to a non-volatile storage area. The interface does not interpret or check any semantics or 
        ///     formatting for the data being written.  The offset used in this command is a ‘logical’ offset that may or may not 
        ///     correspond to the physical address. For example, FRU information could be kept in FLASH at physical address 1234h, 
        ///     however offset 0000h would still be used with this command to access the start of the FRU information.
        ///     
        ///     IPMI FRU device data (devices that are formatted per [FRU]) as well as processor and DIMM FRU data always starts 
        ///     from offset 0000h unless otherwise noted.
        /// </summary>
        public override WriteFruDevice WriteFruDevice(int deviceId, ushort offset, byte[] payload)
        {

            try
            {
                return base.WriteFruDevice(deviceId, offset, payload);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "WriteFruDevice", ex.ToString()));

                return new WriteFruDevice((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Write Fru Data to Baseboard containing BMC FRU.
        /// </summary>
        public override WriteFruDevice WriteFruDevice(ushort address, byte[] payload)
        {

            try
            {
                return base.WriteFruDevice(0, address, payload);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "WriteFruDevice", ex.ToString()));

                return new WriteFruDevice((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        ///  Get Sensor Data Repository. Returns SDR Info.
        /// </summary>
        public override SdrCollection GetSdr()
        {
            try
            {
                return base.GetSdr();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSdr", ex.ToString()));

                return new SdrCollection((byte)CompletionCode.IpmiResponseNotProvided);
            }

        }

        /// <summary>
        ///  Get Sensor Data Repository Information Incrementally. Returns SDR Info.
        /// </summary>
        public override SdrCollection GetSdrIncrement()
        {
            try
            {
                return base.GetSdrIncrement();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSdrIncrement", ex.ToString()));

                return new SdrCollection((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Physically identify the computer by using a light or sound.
        /// </summary>
        /// <param name="interval">Identify interval in seconds or 255 for indefinite.</param>
        public override bool Identify(byte interval)
        {
            try
            {
                return base.Identify(interval);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "Identify", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Set the Power Cycle interval.
        /// </summary>
        /// <param name="interval">Identify interval in seconds or 255 for indefinite.</param>
        public bool SetPowerCycleInterval(byte interval)
        {
            try
            {
                return SetPowerOnTime(interval);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetPowerCycleInterval", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Set the computer power state.
        /// </summary>
        /// <param name="powerState">Power state to set.</param>
        public override byte SetPowerState(IpmiPowerState powerState)
        {
            try
            {
                return base.SetPowerState(powerState);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetPowerState", ex.ToString()));

                return (byte)CompletionCode.IpmiResponseNotProvided;
            }
        }

        /// <summary>
        /// Gets BMC firmware revision.  Returns HEX string.
        /// </summary>
        /// <returns>firmware revision</returns>
        public override BmcFirmware GetFirmware()
        {
            try
            {
                return base.GetFirmware();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetFirmware", ex.ToString()));

                return new BmcFirmware((byte)CompletionCode.IpmiResponseNotProvided);
            }

        }

        /// <summary>
        /// Get the Power-On-Hours (POH) of the host computer.
        /// </summary>
        /// <returns>System Power On Hours.</returns>
        /// <remarks> Specification Note: Power-on hours shall accumulate whenever the system is in 
        /// the operational (S0) state. An implementation may elect to increment power-on hours in the S1 
        /// and S2 states as well.
        /// </remarks>
        public override PowerOnHours PowerOnHours()
        {
            try
            {
                return base.PowerOnHours();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "PowerOnHours", ex.ToString()));

                return new PowerOnHours((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Queries BMC for the GUID of the system.
        /// </summary>
        /// <returns>GUID reported by Baseboard Management Controller.</returns>
        public DeviceGuid GetSystemGuid(PriorityLevel priority, bool retry = false)
        {
            try
            {
                if (priority == PriorityLevel.System)
                {
                    return this.SysPriGetSystemGuid();
                }
                else
                {
                    return base.GetSystemGuid(retry);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSystemGuid", ex.ToString()));

                return new DeviceGuid((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Reset SEL Log
        /// </summary>
        public override bool ClearSel()
        {
            try
            {
                return base.ClearSel();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ClearSel", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Recursively retrieves System Event Log entries.
        /// </summary>
        public override SystemEventLog GetSel()
        {
            try
            {
                return base.GetSel();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSel", ex.ToString()));

                return new SystemEventLog((byte)CompletionCode.IpmiResponseNotProvided);
            }

        }

        /// <summary>
        ///  Get System Event Log Information. Returns SEL Info.
        /// </summary>
        public override SystemEventLogInfo GetSelInfo()
        {
            try
            {
                return base.GetSelInfo();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSelInfo", ex.ToString()));

                return new SystemEventLogInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }


        }

        /// <summary>
        /// Gets the SDR, but only temprature sensors.  This method has a performance improvement over
        /// getting the entire SDR.  Approximately 12 second to 3 second reduction.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public SdrCollection GetSensorMetaData(PriorityLevel priority, bool retry)
        {
            try
            {
                return this.GetSensorDataRecords(priority, retry);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSensorMetaData", ex.ToString()));

                return new SdrCollection((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Device Id Command
        /// </summary>
        public BmcDeviceId GetDeviceId(PriorityLevel priority)
        {
            try
            {
                if (priority == PriorityLevel.System)
                {
                    return SysPriGetDeviceId();
                }
                else
                {
                    return base.GetDeviceId();
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetDeviceId", ex.ToString()));

                return new BmcDeviceId((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Set Chassis Power Restore Policy.
        /// </summary>
        public override PowerRestorePolicy SetPowerRestorePolicy(PowerRestoreOption policyOption)
        {
            try
            {
                return base.SetPowerRestorePolicy(policyOption);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetPowerRestorePolicy", ex.ToString()));

                return new PowerRestorePolicy((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get the current advanced state of the host computer.
        /// </summary>
        /// <returns>ImpiPowerState enumeration.</returns>
        /// <devdoc>
        /// Originally used the 'Get ACPI Power State' message to retrieve the power state but not supported
        /// by the Arima's Scorpio IPMI card with firmware 1.10.00610100.  The 'Get Chassis Status' message
        /// returns the correct information for all IPMI cards tested.
        /// </devdoc>
        public override SystemStatus GetChassisState()
        {
            try
            {
                return base.GetChassisState();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetChassisState", ex.ToString()));

                return new SystemStatus((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Sets the BMC serial port time out in 30 second increments.
        /// Exampe:  A paramater of 2 = 1 minute.  A paramater of 4 = 2 minutes
        /// </summary>
        public bool SetSerialTimeOut(byte time)
        {
            try
            {
                return base.SetSerialConfig<SerialConfig.SessionTimeout>(new SerialConfig.SessionTimeout(time));
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetSerialTimeOut", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Set serial port termination paramater
        /// </summary>
        public bool SetSerialTermination(bool dcd = false, bool timeout = false)
        {
            try
            {
                return base.SetSerialConfig<SerialConfig.SessionTermination>(new SerialConfig.SessionTermination(dcd, timeout));
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetSerialTermination", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Serial Console Timeout in seconds.  Zero indicates read error
        /// </summary>
        public int GetSerialTimeOut()
        {
            try
            {
                return base.GetSerialConfig<SerialConfig.SessionTimeout>(new SerialConfig.SessionTimeout()).TimeOut;
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetSerialTimeOut", ex.ToString()));

                return 0;
            }
        }

        /// <summary>
        /// Get JBOD Disk Status
        /// </summary>
        public override DiskStatusInfo GetDiskStatus()
        {
            try
            {
                return base.GetDiskStatus();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetDiskStatus", ex.ToString()));

                return new DiskStatusInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get JBOD Disk Info
        /// </summary>
        public override DiskInformation GetDiskInfo()
        {
            try
            {
                return base.GetDiskInfo();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetDiskInfo", ex.ToString()));

                return new DiskInformation((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get JBOD Disk Info
        /// </summary>
        public override DiskInformation GetDiskInfo(byte channel, byte disk)
        {
            try
            {
                return base.GetDiskInfo(channel, disk);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetDiskInfo", ex.ToString()));

                return new DiskInformation((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        #endregion

        #region Forced overrides

        /// <summary>
        /// Queries BMC for the GUID of the system. With System Level Priority
        /// </summary>
        /// <returns>GUID reported by Baseboard Management Controller.</returns>
        private DeviceGuid SysPriGetSystemGuid()
        {
            GetSystemGuidRequest req = new GetSystemGuidRequest();

            GetSystemGuidResponse response =
                (GetSystemGuidResponse)this.IpmiSendReceive(req, typeof(GetSystemGuidResponse), PriorityLevel.System, false);

            DeviceGuid responseObj = new DeviceGuid(response.CompletionCode);

            if (response.CompletionCode == (byte)CompletionCode.Success)
            {
                responseObj.SetParamaters(response.Guid);
            }

            return responseObj;
        }

        /// <summary>
        /// Gets Device Id.  Returns HEX string, performed with System Level priority
        /// </summary>
        /// <returns>firmware revision</returns>
        private BmcDeviceId SysPriGetDeviceId()
        {
            // Get Device Id
            GetDeviceIdResponse response = (GetDeviceIdResponse)this.IpmiSendReceive(new GetDeviceIdRequest(),
              typeof(GetDeviceIdResponse), PriorityLevel.System);

            BmcDeviceId responseObj = new BmcDeviceId(response.CompletionCode);

            if (response.CompletionCode == (byte)CompletionCode.Success)
            {
                responseObj.SetParamaters(response.MajorFirmware, response.MinorFirmware, response.AuxFwVer,
                    response.ManufacturerId, response.ProductId);
            }

            return responseObj;
        }

        /// <summary>
        ///  Get Sensor Data Repository. Returns SDR Info.
        /// </summary>
        private SdrCollection GetSensorDataRecords(PriorityLevel priority, bool retry = true)
        {
            // Default Record Off Set
            int offSet = 0;

            // Number of Bytes to Read. 0xFF for entire record.
            byte bytesToRead = 0xFF;

            // SDR RecordId (0000h for entry point)
            ushort recordId = 0;

            // Last SDR RecordId (aborts event log Loop)
            ushort lastRecordId = 65535;

            // securely step out of the while.
            int pass = 0;

            // create sdr record collection for raw SDR records.
            IpmiSdrCollection records = new IpmiSdrCollection();

            // reserve the SDR for partial reads
            ReserveSdrResponse reserve = (ReserveSdrResponse)this.IpmiSendReceive(
            new ReserveSdrRequest(), typeof(ReserveSdrResponse), priority, retry);

            if (reserve.CompletionCode == (byte)CompletionCode.Success)
            {

                // reserved LS byte
                byte reserveLs = reserve.ReservationLS;

                // reserved MS byte
                byte reserveMs = reserve.ReservationMS;

                // retrieve all records while connected by recursively calling the SDR entry command 
                // we limit SDR to 255 records.  If pass hits 300 there's something wrong.
                while (recordId != lastRecordId && pass < 300)
                {
                    // create SDR record
                    SdrRecord sdr = new SdrRecord();
                    {
                        // get the SDR record
                        GetSdrPartialResponse response = (GetSdrPartialResponse)this.IpmiSendReceive(
                        new GetSdrPartialRequest(reserveLs, reserveMs, recordId, offSet, bytesToRead), typeof(GetSdrPartialResponse), priority, retry);

                        if (response.CompletionCode == (byte)CompletionCode.Success)
                        {
                            sdr.completionCode = response.CompletionCode;

                            // set record id
                            sdr.RecordId = new byte[2] { response.RecordData[1], response.RecordData[0] };

                            // set the record version
                            sdr.RecordVersion = response.RecordData[2];

                            // set record type
                            sdr.RecordType = response.RecordData[3];

                            // set record length
                            sdr.RecordLenght = response.RecordData[4];

                            // set the record data to record data
                            sdr.RecordData = response.RecordData;

                            // update the record Id (signals loop exit)
                            recordId = BitConverter.ToUInt16(new byte[2] { response.RecordIdMsByte, response.RecordIdLsByte }, 0);
                        }
                        else
                        {
                            sdr.completionCode = response.CompletionCode;
                            break;
                        }
                    }

                    pass++;

                    // add the record to the collection
                    records.Add(sdr);
                }
            }

            // return collection
            SdrCollection sdrMessages = new SdrCollection();

            // check response collection holds values
            if (records.Count > 0)
            {
                // sdr version array
                byte[] verarr = new byte[2];

                // record id
                short id;

                foreach (SdrRecord record in records)
                {
                    if (record.completionCode == (byte)CompletionCode.Success)
                    {
                        // set the sdr collection completion code to indicate a failure occurred
                        sdrMessages.completionCode = record.completionCode;

                        // record Id
                        id = BitConverter.ToInt16(record.RecordId, 0);

                        // populate version array
                        Buffer.BlockCopy(IpmiSharedFunc.ByteSplit(record.RecordVersion, new int[2] { 4, 0 }), 0, verarr, 0, 2);

                        string sVersion = Convert.ToUInt16(verarr[1]).ToString() + "." + Convert.ToInt16(verarr[0]).ToString();

                        // set version
                        Decimal version = 0;
                        // sdr record version number
                        if (decimal.TryParse(sVersion, out version)) { }

                        base.GetSdrMetaData(id, version, record.RecordType, record, ref sdrMessages);
                    }
                    // set the sdr completion code to indicate a failure occurred
                    sdrMessages.completionCode = record.completionCode;
                }
            }

            return sdrMessages;
        }

        /// <summary>
        /// Gets Sensor Reading
        /// </summary>
        private SensorReading SensorReading(byte SensorNumber, byte EventTypeCode, PriorityLevel priority)
        {
            SensorReadingResponse response = (SensorReadingResponse)this.IpmiSendReceive(
            new SensorReadingRequest(SensorNumber), typeof(SensorReadingResponse), priority, true);

            SensorReading respObj = new SensorReading(response.CompletionCode);
            respObj.SensorNumber = SensorNumber;
            respObj.EventTypeCode = EventTypeCode;

            if (response.CompletionCode == (byte)CompletionCode.Success)
            {
                byte[] statusByteArray = new byte[1];
                statusByteArray[0] = response.SensorStatus;

                BitArray sensorStatusBitArray = new BitArray(statusByteArray);
                bool eventMsgEnabled = sensorStatusBitArray[7];
                bool sensorScanEnabled = sensorStatusBitArray[6];
                bool readingUnavailable = sensorStatusBitArray[5];

                byte[] stateByteArray = new byte[1];
                stateByteArray[0] = response.StateOffset;

                BitArray stateBitArray = new BitArray(stateByteArray);

                string SensorState = string.Empty;

                // No reading available
                if (readingUnavailable)
                {
                    respObj.SetReading(0);
                    respObj.SetEventState(0xFE);
                    return respObj;
                }

                // set the raw sensor reading
                respObj.SetReading(response.SensorReading);

                #region Threshold Event
                if (EventTypeCode == 0x01)
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        if (stateBitArray[i])
                            respObj.SetEventState((byte)i);
                    }
                }

                #endregion

                #region Discrete Events (Generic, Sensor-specific and OEM)
                else if (((EventTypeCode >= 0x02) && (EventTypeCode <= 0x0C)) ||
                          (EventTypeCode == 0x6F) ||
                         ((EventTypeCode >= 0x70) && (EventTypeCode <= 0x7F)))
                {
                    for (int i = 0; i <= 7; i++)
                    {
                        if (stateBitArray[i])
                            respObj.SetEventState((byte)i);
                    }

                    if (response.OptionalOffset != 0x00)
                    {
                        byte[] optionalByteArray = new byte[1];
                        optionalByteArray[0] = response.OptionalOffset;

                        BitArray optionalBitArray = new BitArray(optionalByteArray);

                        for (int i = 0; i <= 6; i++)
                        {
                            if (optionalBitArray[i])
                                respObj.SetEventState((byte)(i + 8));
                        }
                    }
                }
                #endregion

                #region Unspecified Event
                else
                {
                    // Unspecified
                }
                #endregion
            }

            return respObj;
        }

        /// <summary>
        ///  Get Sensor Data Repository for sensor one.. Returns SDR Info.
        /// </summary>
        private SdrCollection GetFirstSdr(PriorityLevel priority, bool retry = true)
        {
            // Default Record Off Set
            int offSet = 0;

            // Number of Bytes to Read. 0xFF for entire record.
            byte bytesToRead = 0xFF;

            // SDR RecordId (0000h for entry point)
            ushort recordId = 0;

            // return collection
            SdrCollection sdrMessages = new SdrCollection();

            // reserve the SDR for partial reads
            ReserveSdrResponse reserve = (ReserveSdrResponse)this.IpmiSendReceive(
            new ReserveSdrRequest(), typeof(ReserveSdrResponse), priority, retry);

            if (reserve.CompletionCode == (byte)CompletionCode.Success)
            {
                // create SDR record
                SdrRecord sdr = new SdrRecord();
                {
                    // get the SEL record
                    GetSdrPartialResponse response = (GetSdrPartialResponse)this.IpmiSendReceive(
                    new GetSdrPartialRequest(reserve.ReservationLS, reserve.ReservationMS, recordId, offSet, bytesToRead), typeof(GetSdrPartialResponse), priority, retry);

                    if (response.CompletionCode == (byte)CompletionCode.Success)
                    {
                        sdr.completionCode = response.CompletionCode;

                        // set record id
                        sdr.RecordId = new byte[2] { response.RecordData[1], response.RecordData[0] };

                        // set the record version
                        sdr.RecordVersion = response.RecordData[2];

                        // set record type
                        sdr.RecordType = response.RecordData[3];

                        // set record lenght
                        sdr.RecordLenght = response.RecordData[4];

                        // set the record data to record data
                        sdr.RecordData = response.RecordData;

                        // update the record Id (signals loop exit)
                        recordId = BitConverter.ToUInt16(new byte[2] { response.RecordIdMsByte, response.RecordIdLsByte }, 0);

                        // set the sdr collection completion code to indicate a failure occurred
                        sdrMessages.completionCode = sdr.completionCode;

                        // sdr version array
                        byte[] verarr = new byte[2];

                        // record Id
                        short id = BitConverter.ToInt16(sdr.RecordId, 0);

                        // populate version array
                        Buffer.BlockCopy(IpmiSharedFunc.ByteSplit(response.RecordData[2], new int[2] { 4, 0 }), 0, verarr, 0, 2);

                        string sVersion = Convert.ToUInt16(verarr[1]).ToString() + "." + Convert.ToInt16(verarr[0]).ToString();

                        // set version
                        Decimal version = 0;
                        // sdr record version number
                        decimal.TryParse(sVersion, out version);

                        base.GetSdrMetaData(id, version, sdr.RecordType, sdr, ref sdrMessages);

                    }
                    else
                    {
                        sdr.completionCode = response.CompletionCode;
                    }
                }
            }

            return sdrMessages;
        }

        /// <summary>
        /// Negotiates the ipmi version and sets client accordingly. Also sets the authentication type for V1.5
        /// </summary>
        public ChannelAuthenticationCapabilities GetAuthenticationCapabilities(PrivilegeLevel privilegeLevel, PriorityLevel priority, bool retry = false)
        {
            // Get Channel Authentication Capabilities
            GetChannelAuthenticationCapabilitiesResponse response =
                (GetChannelAuthenticationCapabilitiesResponse)this.IpmiSendReceive(
                    new GetChannelAuthenticationCapabilitiesRequest(0x0E, privilegeLevel),
                    typeof(GetChannelAuthenticationCapabilitiesResponse), priority, retry);

            ChannelAuthenticationCapabilities authCapabilities = new ChannelAuthenticationCapabilities(response.CompletionCode);

            if (response.CompletionCode == (byte)CompletionCode.Success)
            {

                authCapabilities.SetParamaters(response.ChannelNumber,
                    response.AuthenticationTypeSupport1,
                response.AuthenticationTypeSupport2, response.ExtendedCapabilities,
                response.OemId, response.OemData);
            }

            return authCapabilities;
        }

        #endregion

        #region Dcmi Commands

        /// <summary>
        /// DCMI Get Power Limit Command
        /// </summary>
        public override PowerLimit GetPowerLimit()
        {
            try
            {
                return base.GetPowerLimit();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetPowerLimit", ex.ToString())); ;

                return new PowerLimit((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// MasterWriteRead Command
        /// </summary>
        public override SmbusWriteRead MasterWriteRead(byte channel, byte slaveId, byte readCount, byte[] writeData)
        {
            try
            {
                return base.MasterWriteRead(channel, slaveId, readCount, writeData);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "MasterWriteRead", ex.ToString())); ;

                return new SmbusWriteRead((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// DCMI Set Power Limit Command
        /// </summary>
        public override ActivePowerLimit SetPowerLimit(short watts, int correctionTime, byte action, short samplingPeriod)
        {
            try
            {
                return base.SetPowerLimit(watts, correctionTime, action, samplingPeriod);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetPowerLimit", ex.ToString()));

                return new ActivePowerLimit((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// DCMI Get Power Reading Command
        /// </summary>
        public override List<PowerReading> GetPowerReading()
        {
            try
            {
                return base.GetPowerReading();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetPowerReading", ex.ToString()));
                return new List<PowerReading>(1) { new PowerReading((byte)CompletionCode.IpmiResponseNotProvided) };
            }
        }

        /// <summary>
        /// Activate/Deactivate DCMI power limit
        /// </summary>
        /// <param name="enable">Activate/Deactivate</param>
        public override bool ActivatePowerLimit(bool enable)
        {
            try
            {
                return base.ActivatePowerLimit(enable);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ActivatePowerLimit", ex.ToString()));

                return false;
            }
        }

        #endregion

        #region Bridge Commands

        /// <summary>
        /// Send sync Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="slaveId">Channel Slave Id</param>
        /// <param name="messageData">Message payload</param>
        public override BridgeMessage SendMessage(byte channel, byte slaveId, byte[] requestMessage)
        {
            try
            {
                return base.SendMessage(channel, slaveId, requestMessage);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SendMessage", ex.ToString()));

                return new BridgeMessage((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Message Flags
        /// </summary>
        public override MessageFlags GetMessageFlags()
        {
            try
            {
                return base.GetMessageFlags();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetMessageFlags", ex.ToString()));

                return new MessageFlags((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Read Event Message Buffer
        /// </summary>
        public override BridgeMessage ReadEventMessageBuffer()
        {
            try
            {
                return base.ReadEventMessageBuffer();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ReadEventMessageBuffer", ex.ToString()));

                return new BridgeMessage((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Message Response
        /// </summary>
        public override BridgeMessage GetMessage()
        {
            try
            {
                return base.GetMessage();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetMessage", ex.ToString()));

                return new BridgeMessage((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get the channel state for bridging commands
        /// </summary>
        /// <param name="channel">Channel number to check</param>
        /// <param name="enabled">Channel Disabled = 0x00, Channel Enabled = 0x001</param>
        public override BridgeChannelReceive BridgeChannelEnabled(byte channel)
        {
            try
            {
                return base.BridgeChannelEnabled(channel);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "BridgeChannelEnabled", ex.ToString()));

                return new BridgeChannelReceive((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Enable or Disable the Ipmi Bridge Channel
        /// </summary>
        /// <param name="channel">Channel number to enable</param>
        /// <param name="enabled">Enabled = true, Disabled = false</param>
        public override BridgeChannelReceive EnableDisableBridgeChannel(byte channel, bool enabled)
        {
            try
            {
                return base.EnableDisableBridgeChannel(channel, enabled);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "EnableDisableBridgeChannel", ex.ToString()));

                return new BridgeChannelReceive((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        #endregion

        #region Node Manager Commands

        /// <summary>
        /// Send sync Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="slaveId">Channel Slave Id</param>
        /// <param name="messageData">Message payload</param>
        public override SendNodeMangerMessage SendNodeManagerRequest<T>(byte channel, byte slaveId, NodeManagerRequest requestMessage)
        {
            try
            {
                return base.SendNodeManagerRequest<T>(channel, slaveId, requestMessage);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SendNodeManagerRequest", ex.ToString()));

                return new SendNodeMangerMessage((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Send Async Bridge Command
        /// </summary>
        /// <param name="channel">Channel to send command (Intel ME = 6)</param>
        /// <param name="messageData">Message payload</param>
        public override GetNodeMangerMessage GetNodeManagerMessage(byte rqSeq, byte channel, Type responseMessage)
        {
            try
            {
                return base.GetNodeManagerMessage(rqSeq, channel, responseMessage);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetNodeManagerMessage", ex.ToString()));

                return new GetNodeMangerMessage((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        #endregion

        #region Ipmi Broadcast

        /// <summary>
        /// Sends an Ipmi Request to every blade in the chassis
        /// </summary>
        internal Dictionary<byte, CompletionCode> BroadcastIpmiRequest<T>(T request) where T : IpmiRequest
        {
            // Send request to every blade
            Dictionary<byte, CompletionCode> response = this.IpmiAsyncSend(request, PriorityLevel.System);

            return response;
        }

        #endregion

        #region OEM IPMI Commands

        /// <summary>
        /// Get Processor Information
        /// </summary>
        public override ProcessorInfo GetProcessorInfo(byte processor)
        {
            try
            {
                return base.GetProcessorInfo(processor);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetProcessorInfo", ex.ToString()));

                return new ProcessorInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Memory Information
        /// </summary>
        public override MemoryInfo GetMemoryInfo(byte dimm)
        {
            try
            {
                return base.GetMemoryInfo(dimm);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetMemoryInfo", ex.ToString()));

                return new MemoryInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get PCIe Information
        /// </summary>
        public override PCIeInfo GetPCIeInfo(byte device)
        {
            try
            {
                return base.GetPCIeInfo(device);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetPCIeInfo", ex.ToString()));

                return new PCIeInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Nic Information
        /// </summary>
        public override NicInfo GetNicInfo(byte device)
        {
            try
            {
                return base.GetNicInfo(device);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetNicInfo", ex.ToString()));

                return new NicInfo((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Activate/Deactivate Psu Alert
        /// </summary>
        public override bool ActivatePsuAlert(bool enableAutoProcHot, BmcPsuAlertAction bmcAction, bool removeCap)
        {
            try
            {
                return base.ActivatePsuAlert(enableAutoProcHot, bmcAction, removeCap);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ActivatePsuAlert", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Read BIOS Error Code
        /// </summary>
        /// <param name="version">0 = Current, 1 = Previous</param>
        public override BiosCode GetBiosCode(byte version)
        {
            try
            {
                return base.GetBiosCode(version);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetBiosCode", ex.ToString()));

                return new BiosCode((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Default Power Cap
        /// </summary>
        public override DefaultPowerLimit GetDefaultPowerCap()
        {
            try
            {
                return base.GetDefaultPowerCap();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetDefaultPowerCap", ex.ToString()));

                return new DefaultPowerLimit((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get NvDimm Trigger
        /// </summary>
        public override NvDimmTrigger GetNvDimmTrigger()
        {
            try
            {
                return base.GetNvDimmTrigger();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetNvDimmTrigger", ex.ToString()));

                return new NvDimmTrigger((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Get Psu Alert
        /// </summary>
        public override PsuAlert GetPsuAlert()
        {
            try
            {
                return base.GetPsuAlert();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "GetPsuAlert", ex.ToString()));

                return new PsuAlert((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Set the DPC for PSU_ALERT actions
        /// </summary>
        public override bool SetDefaultPowerLimit(ushort defaultPowerCap, ushort waitTime, bool enableCapping)
        {
            try
            {
                return base.SetDefaultPowerLimit(defaultPowerCap, waitTime, enableCapping);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetDefaultPowerLimit", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Sets the NvDimm Trigger
        /// </summary>
        public override bool SetNvDimmTrigger(NvDimmTriggerAction trigger, bool assertTrigger,
            byte adrCompleteDelay, byte nvdimmPresentPoweroffDelay)
        {
            try
            {
                return base.SetNvDimmTrigger(trigger, assertTrigger, adrCompleteDelay, nvdimmPresentPoweroffDelay);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetNvDimmTrigger", ex.ToString()));

                return false;
            }

        }

        /// <summary>
        /// Set Psu Alert
        /// </summary>
        /// <returns></returns>
        public override bool SetPsuAlert(bool enable)
        {
            try
            {
                return base.SetPsuAlert(enable);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SetPsuAlert", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Enables the output of KCS and Serial command trace debug messages in the BMC diagnostic debug console
        /// </summary>
        public override bool BmcDebugEnable(BmcDebugProcess process, bool enable)
        {
            try
            {
                return base.BmcDebugEnable(process, enable);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "BmcDebugEnable", ex.ToString()));

                return false;
            }
        }

        /// <summary>
        /// Start BMC serial Session
        /// </summary>
        /// <param name="flushBuffer">Flush the current BMC data buffer</param>
        /// <param name="timeoutInSecs">Session timeout in seconds. Zero implies no console session timeout</param>
        /// <returns>StartSerialSession response object</returns>
        public override StartSerialSession StartSerialSession(bool flushBuffer, int timeoutInSecs)
        {
            try
            {
                return base.StartSerialSession(flushBuffer, timeoutInSecs);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "StartSerialSession", ex.ToString()));

                return new StartSerialSession((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Stop Serial Console Session
        /// </summary>
        public override StopSerialSession StopSerialSession()
        {
            try
            {
                return base.StopSerialSession();
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "StopSerialSession", ex.ToString()));

                return new StopSerialSession((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Send Serial Data
        /// </summary>
        public override SendSerialData SendSerialData(ushort length, byte[] payload)
        {
            try
            {
                return base.SendSerialData(length, payload);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "SendSerialData", ex.ToString()));

                return new SendSerialData((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Receive Serial Data
        /// </summary>
        public override ReceiveSerialData ReceiveSerialData(ushort length)
        {
            try
            {
                return base.ReceiveSerialData(length);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ReceiveSerialData", ex.ToString()));

                return new ReceiveSerialData((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        /// <summary>
        /// Thermal Control
        /// Function:
        ///     0 - Get Status of Thermal Control feature in the BMC
        ///     1 – Set Thermal Control Feature enabled 
        ///     2 – Set Thermal Control Feature disabled
        /// </summary>
        public override ThermalControl ThermalControl(ThermalControlFunction function)
        {
            try
            {
                return base.ThermalControl(function);
            }
            catch (Exception ex)
            {
                Tracer.WriteError(String.Format("BladeId: {0} Method: {1} Exception: {2}", this.DeviceId, "ThermalControl", ex.ToString()));

                return new ThermalControl((byte)CompletionCode.IpmiResponseNotProvided);
            }
        }

        #endregion

    }
}
