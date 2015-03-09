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
    using System.IO.Ports;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// Ipmi Serial Client [Mode Basic]
    /// </summary>
    internal sealed class IpmiSerialClient : IpmiClientNodeManager, IDisposable
    {

        # region Private Variables

        // ipmi forwarded command NetFn
        private const byte ipmiFwd = 0x34;
        // Serial Ipmi Packet Start Byte
        private const byte startByte = 0xA0;
        // Serial Ipmi Packet Stop byte
        private const byte stopByte = 0xA5;
        // Serial Ipmi Packet data escape
        private const byte dataEscape = 0xAA;
        // Serial Ipmi Packet data escape
        private const byte dataHandShake = 0xA6;
        // Serial IPMI Receive Sterial Data Request NetFn
        private const byte receiveSerialData = 0x2D;

        // prevents redundant dispose calls.
        private bool disposed = false;

        // Serial COM Port Name
        private string comPort;
        
        // Serial Baud Rate
        private int baudRate;
        
        // Serial Parity
        private Parity parity;
        
        // Serial Data Bits
        private int dataBits;

        // Serial Stop Bits
        private StopBits stopBits;

        // Serial Port
        private SerialPort serialPort;

        /// <summary>
        /// Represents the current IPMI Session Id.
        /// </summary>
        private uint ipmiSessionId;

        /// <summary>
        /// Overrides command retrys
        /// </summary>
        private bool allowRetry;

        /// <summary>
        /// locker object for accessing global resources.
        /// </summary>
        private object reqSeqLock = new object();

        /// <summary>
        /// Locker object for modifying the client cache
        /// </summary>
        private object cacheLock = new object();

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

        #endregion

        # region Internal Variables

        /// <summary>
        /// Gets and sets the current IPMI Session Id.
        /// </summary>
        /// <value>IPMI Session Id.</value>
        internal uint IpmiSessionId
        {
            get { lock (cacheLock) { return this.ipmiSessionId; } }
            set { lock (cacheLock) { this.ipmiSessionId = value; } }
        }

        /// <summary>
        ///  Serial COM Port Name
        /// </summary>
        internal string ClientPort
        {
            get {return this.comPort; }
            set {this.comPort = value;}
        }

        // Serial Baud Rate
        internal int ClientBaudRate
        {
            get {return this.baudRate; }
            set {this.baudRate = value;}
        }

        // Serial Parity
        internal Parity ClientParity
        {
            get {return this.parity; }
            set {this.parity = value;}
        }

        // Serial Data Bits
        internal int ClientDataBits
        {
            get {return this.dataBits; }
            set {this.dataBits = value;}
        }

        // Serial Stop Bits
        internal StopBits ClientStopBits
        {
            get {return this.stopBits; }
            set {this.stopBits = value;}
        }

        /// <summary>
        /// Override Ipmi Command Retrys.  Default = false
        /// false = Allows retrys if command has retry.
        /// true = Prevents all retrys.
        /// </summary>
        internal bool OverRideRetry
        {
            get { return this.allowRetry; }
            set { this.allowRetry = value; }
        }

        #endregion

        #region Ipmi Escape Framing

        /// <summary>
        /// Replace serial framing charactors on outbound payload with 
        /// substatute byte sequence: 
        ///         IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
        ///         IPMI 2.0: 14.4.2 - Data Byte Escaping 
        /// </summary>
        internal byte[] ReplaceFrameChars(byte[] payload)
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
                        offset = (esc.Key + increase +1);

                        // add 1 to index, to index past itself.
                        index = (esc.Key +1);
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
        internal byte[] ReplaceEscapeChars(byte[] payload)
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
        private static List<int> GetEscapePositions(byte[] payload, byte[] pattern)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < (payload.Length -1); i++)
            {
                if (pattern[0] == payload[i] && pattern[1] == payload[i+1])
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
        private static List<int> GetFramePositions(byte[] payload, byte pattern)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] == pattern)
                {
                   indexes.Add(i);
                }
            }

            return indexes;
        }

        internal void SerialWrite(byte[] payload)
        {
            if (this.serialPort != null)
            {
                try
                {
                    if (this.serialPort.IsOpen)
                    {
                        serialPort.Write(payload, 0, payload.Length);
                    }
                    else
                    {
                        IpmiSharedFunc.WriteTrace("Error: Data Write Serial Port Closed");
                    }
                }
                catch (Exception ex)
                {

                    IpmiSharedFunc.WriteTrace("Data Write exception occured when reading serial console data: " + ex.Message.ToString());
                }
                
            }
            else
            {
                IpmiSharedFunc.WriteTrace("Error: Data Write Serial Port Null");
            }
        }

        internal byte[] SerialRead()
        {
            byte[] nothing = new byte[0];
            if (this.serialPort != null)
            {
                try
                {
                    byte[] buffer = new byte[512];
                    int readBytes;

                    if (serialPort.BytesToRead > 0)
                    {
                        readBytes = serialPort.Read(buffer, 0, buffer.Length);

                        byte[] payload = new byte[readBytes];
                        Buffer.BlockCopy(buffer, 0, payload, 0, readBytes);

                        return payload;
                    }
                }
                catch (Exception ex)
                {
                    IpmiSharedFunc.WriteTrace("Data Receive exception occured when reading serial console data: " + ex.Message.ToString());
                }

            }
            else
            {
                IpmiSharedFunc.WriteTrace("Serial Console is not opened");
            }

            return nothing;
        }

        /// <summary>
        /// Add Start & Stop Serial Framing Charactors.
        /// </summary>
        internal static void AddStartStopFrame(ref byte[] payload)
        {
            payload[0] = startByte;
            payload[(payload.Length -1)] = stopByte;
        }

        #endregion

        #region Close, LogOff & Dispose

        /// <summary>
        /// Closes the connection to the BMC device. This is the preferred method of closing any open 
        /// connection.
        /// </summary>
        internal void Close()
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
        internal void Close(bool hardClose)
        {
            if (hardClose == false)
            {
                this.LogOff();
            }

            this.SetClientState(IpmiClientState.Disconnected);
        }

        /// <summary>
        /// Releases all resources held by this IpmiClient instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IpmiSerialClient()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Close(false);
                }

                if (this.serialPort != null)
                {
                    this.serialPort.Close();
                    this.serialPort = null;
                }

                // new shared cleanup logic
                disposed = true;
            }
        }


        /// <summary>
        /// End an authenticated session with the BMC.
        /// </summary>
        internal void LogOff()
        {
            if (this.IpmiSessionId != 0)
            {
                this.IpmiSendReceive(
                    new CloseSessionRequest(this.IpmiSessionId),
                    typeof(CloseSessionResponse), false);
                this.IpmiSessionId = 0;
            }
        }

        #endregion

        #region Connect & Logon

        /// <summary>
        /// Connect to bmc serial port using default connection
        /// paramaters:
        ///     BaudRate = 115200
        ///     Parity = None
        ///     Data Bits = 8
        ///     Stop Bits = 1
        /// </summary>
        internal void Connect(string comPort)
        {
            this.ClientPort = comPort;
            this.ClientBaudRate = 115200;
            this.ClientDataBits = 8;
            this.ClientParity = Parity.None;
            this.ClientStopBits = StopBits.One;
            this.Connect();
        }

        /// <summary>
        /// Connect to bmc serial port using specifying connection
        /// paramaters.
        /// </summary>
        internal void Connect(string comPort, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.ClientPort = comPort;
            this.ClientBaudRate = baudRate;
            this.ClientDataBits = dataBits;
            this.ClientParity = parity;
            this.ClientStopBits = stopBits;
            this.Connect();
        }

        /// <summary>
        /// Connects the client to the serial ipmi bmc on specified computer.
        /// </summary>
        /// <param name="hostName">Host computer to access via ipmi over serial.</param>
        internal void Connect()
        {
            if (this.ClientState != IpmiClientState.Disconnected)
            {
                throw new InvalidOperationException();
            }

            base.SetClientState(IpmiClientState.Connecting);

            // Set serial port configuration paramaters.
            this.serialPort = new SerialPort(this.comPort, this.baudRate, this.parity, this.dataBits, this.stopBits);
            // Rts required
            this.serialPort.RtsEnable = true;
            // Set the read/write timeouts
            this.serialPort.ReadTimeout = (int)base.Timeout;
            // Write timeout
            this.serialPort.WriteTimeout = 100;
            // Write timeout
            this.serialPort.ReadBufferSize = 1024;

            // set no handshake.
            this.serialPort.Handshake = Handshake.None;

            try
            {
                // attempt to open the serial port
                this.serialPort.Open();
            }
            catch (Exception)
            {                
                throw;
            }

            base.SetClientState(IpmiClientState.Connected);

        }

        private void LogOn()
        {
            this.LogOn(base.IpmiUserId, base.IpmiPassword);
        }

        /// <summary>
        /// Start an authenticated session with the BMC.
        /// </summary>
        /// <param name="userid">Account userid to authenticate with.</param>
        /// <param name="password">Account password to authenticate with.</param>
        /// <remarks>Only supports administrator sessions.</remarks>
        internal bool LogOn(string userId, string password)
        {
            bool response = true;

            // temp will remove with debugging
            byte[] IpmiChallengeStringData = { };

            // set the user id & password
            base.IpmiUserId = userId;
            base.IpmiPassword = password;

            // set the client maximum previlege level
            base.IpmiPrivilegeLevel = PrivilegeLevel.Administrator;

            // set client state to session challenge
            base.SetClientState(IpmiClientState.SessionChallenge);

            // set the proposed v1.5 authentication type to MD5 
            // MD5 = the highest mandatory level in v1.5 and v2.0
            base.IpmiProposedAuthenticationType = AuthenticationType.Straight;

            // initialize the ipmi 1.5 authentication type to zero (none)
            this.IpmiAuthenticationType = AuthenticationType.None;

            // session challenge
            GetSessionChallengeResponse challenge =
                (GetSessionChallengeResponse)this.IpmiSendReceive(
                    new GetSessionChallengeRequest(base.IpmiProposedAuthenticationType, base.IpmiUserId),
                    typeof(GetSessionChallengeResponse), false);

            if (challenge.CompletionCode != 0)
            {
                response = false;
            }

            // set challenge string
            IpmiChallengeStringData = challenge.ChallengeStringData;

            // set temporary session id
            this.IpmiSessionId = challenge.TemporarySessionId;

            // set client state to activate session
            base.SetClientState(IpmiClientState.ActivateSession);

            // switch the v1.5 authentication type to the negotiated authentication type.
            base.IpmiAuthenticationType = base.IpmiProposedAuthenticationType;

            // ipmi authentication code / user password logon.
            byte[] authCode = IpmiSharedFunc.AuthCodeSingleSession(this.IpmiSessionId,
                                                                        IpmiChallengeStringData,
                                                                        base.IpmiAuthenticationType,
                                                                        base.IpmiPassword);

            // Session Activation.See: IPMI Table   22-21, Activate Session Command
            ActivateSessionResponse activateResponse =
                (ActivateSessionResponse)this.IpmiSendReceive(
                    new ActivateSessionRequest(this.IpmiAuthenticationType, base.IpmiPrivilegeLevel, authCode, 1),
                    typeof(ActivateSessionResponse), false);

            if (activateResponse.CompletionCode != 0)
            {
                response = false;
            }

            // set the session id for the remainder of the session
            this.IpmiSessionId = activateResponse.SessionId;

            // initialize the ipmi message sequence number to zero
            ResetReqSeq();

            // set client state to authenticated. client state
            // is used for socket and RMCP payload type control
            base.SetClientState(IpmiClientState.Authenticated);

            // set session privilege level
            SetSessionPrivilegeLevelResponse privilege = (SetSessionPrivilegeLevelResponse)this.IpmiSendReceive(
            new SetSessionPrivilegeLevelRequest(PrivilegeLevel.Administrator),
                typeof(SetSessionPrivilegeLevelResponse), false);

            if (privilege.CompletionCode != 0)
            {
                response = false;
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
            return (T)this.IpmiSendReceive(ipmiRequest, typeof(T), false);
        }

        /// <summary>
        /// Send Receive Ipmi messages
        /// </summary>
        internal override IpmiResponse IpmiSendReceive(IpmiRequest ipmiRequest, Type responseType, bool allowRetry = true)
        {
            // Get the request sequence.  This should be incremented
            // for every request/response pair.
            byte reqSeq = GetReqSeq();

            // Serialize the IPMI request into bytes.
            byte[] ipmiRequestMessage = this.ReplaceFrameChars(ipmiRequest.GetBytes(IpmiTransport.Serial, reqSeq));

            // inject start/stop frame bytes.
            AddStartStopFrame(ref ipmiRequestMessage);

            IpmiSharedFunc.WriteTrace("Sending: " + ipmiRequest.GetType().ToString() + " Seq: " + reqSeq.ToString() + ", " + 
                IpmiSharedFunc.ByteArrayToHexString(ipmiRequestMessage));
            
            byte[] messageResponse = { };
            byte[] ipmiResponseMessage = { };
            byte completionCode;

            // Send the ipmi mssage over serial.
            this.SendReceive(ipmiRequestMessage, out messageResponse);

            // Create the response based on the provided type.
            ConstructorInfo constructorInfo = responseType.GetConstructor(Type.EmptyTypes);
            IpmiResponse ipmiResponse = (IpmiResponse)constructorInfo.Invoke(new Object[0]);

            if (messageResponse != null)
            {
                if (messageResponse.Length <= 1)
                {
                    if (messageResponse.Length == 1)
                        ipmiResponse.CompletionCode = messageResponse[0];
                    else
                        ipmiResponse.CompletionCode = 0xA3;

                    return ipmiResponse;
                }
                else
                {
                    // format the received message
                    ProcessReceivedMessage(messageResponse, out ipmiResponseMessage, out completionCode);
                }

                // messageResponse no longer needed.  data copied to ipmiResponseMessage by ProcessReceivedMessage().
                messageResponse = null;

                // check serial protocol completion code
                if (completionCode == 0x00)
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
                    if (completionCode == 0xBE && allowRetry && !OverRideRetry)
                    {
                        // Issue a Retry
                        ipmiResponse = LoginRetry(ipmiRequest, responseType, completionCode);
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
                if (ipmiResponse.CompletionCode == 0x00 ||
                    (ipmiRequest.GetType() == typeof(GetDcmiPowerLimitRequest) && ipmiResponse.CompletionCode == 0x80))
                {
                    try
                    {
                        ipmiResponse.Initialize(IpmiTransport.Serial, ipmiResponseMessage, ipmiResponseMessage.Length, reqSeq);
                        ipmiResponseMessage = null; // response message nolonger needed
                        // reset the communication error counter.
                    }
                    catch (Exception ex)
                    {
                        // set an exception code for invalid data in ipmi data field, as the packet could
                        // not be converted by the InitializeSerial method.
                        ipmiResponse.CompletionCode = 0xCC;

                        IpmiSharedFunc.WriteTrace(string.Format("Method: {0} Response Packet Completion Code: {1} Exception {2}",
                                                ipmiRequest.GetType().ToString(),
                                                IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage),
                                                ex.ToString()));
                    }
                }
                else if (ipmiResponse.CompletionCode == 0xD4 && allowRetry && !OverRideRetry) // Catch Ipmi prevelege loss and perform login retry.
                {
                    // Issue a re-logon and command retry as Ipmi completion code 
                    // D4h indicates session prevelege level issue.
                    ipmiResponse = LoginRetry(ipmiRequest, responseType, ipmiResponse.CompletionCode);
                }
                else
                {
                    // throw ipmi/dcmi response exception with a custom string message and the ipmi completion code
                    IpmiSharedFunc.WriteTrace(string.Format("Request Type: {0} Response Packet: {1} Completion Code {2}", ipmiRequest.GetType().ToString(),
                        IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage), IpmiSharedFunc.ByteToHexString(ipmiResponse.CompletionCode)));
                }
            }
            else
            {
                ipmiResponse.CompletionCode = 0xA3;
                // throw ipmi/dcmi response exception with a custom string message and the ipmi completion code
                IpmiSharedFunc.WriteTrace(string.Format("Request Type: {0} Response Packet: NULL Completion Code 0xA3", ipmiRequest.GetType().ToString()));
            }

            // Response to the IPMI request message.
            return ipmiResponse;
        }

        /// <summary>
        /// Attempts to re-authenticate with the BMC if the session is dropped.
        /// </summary>
        private IpmiResponse LoginRetry(IpmiRequest ipmiRequest, Type responseType, byte completionCode)
        {

            IpmiSharedFunc.WriteTrace(string.Format("Ipmi Logon retry for command {0}.",
                                ipmiRequest.GetType().ToString()));

            this.SetClientState(IpmiClientState.Connecting);

            // return resposne
            IpmiResponse response;

            // Attempt to Identify the blade.  (Note: This command does not allow re-try)
            ChannelAuthenticationCapabilities auth = GetAuthenticationCapabilities(PrivilegeLevel.Administrator, false);

            // if get channel authentication succeeds, check if the blade is a compute blade.  If so, re-establish
            // the session and re-execute the command
            if (auth.CompletionCode == 0)
            {
                if (auth.AuxiliaryData == 0x04)
                {
                    this.LogOn();

                    // re-issue original command.                   
                    return response = IpmiSendReceive(ipmiRequest, responseType, false);
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
        /// Read until receiving the stop byte (or timeout)
        /// </summary>
        private void SendReceive(byte[] ipmiRequestMessage, out byte[] messageResponse)
        {
            this.SendData(ipmiRequestMessage);

            // byte 5 is always the sequence byte.
            ReceiveData(ipmiRequestMessage[5], out messageResponse);
            
        }

        private void SendData(byte[] ipmiRequestMessage)
        {
            this.serialPort.DiscardInBuffer();

            this.serialPort.Write(ipmiRequestMessage, 0, ipmiRequestMessage.Length);
        }

        private void ReceiveData(byte sequence, out byte[] messageResponse)
        {
            List<byte> receivedBytes = new List<byte>();
            int maxsize = 268;

            List<byte> garbageBytes = new List<byte>();

            const int maxGarbageDataByteCount = 128;
            int garbageDataByteCount = 0;

            // indicates the Bmc responded with buffers clear, ready for command.
            bool hasHandShake = false;

            // used for Ipmi Cmd Fwd.
            bool ackReceived = false;

            // flags start byte was detected.
            bool startReceived = false;

            // index to netFn
            int netFnIndex = 6;

            // if the sequence is data byte escaped, the netFN will be +1
            if (sequence == dataEscape)
                netFnIndex = 7;

            // default timeout message
            messageResponse = new byte[1] { 0xBE };

            while (true)
            {
                try
                {
                    int receivedData = serialPort.ReadByte();

                    if (receivedData == -1)
                    {
                        messageResponse = new byte[1] { 0xAC };
                        return;
                    }

                    if (receivedData == startByte)
                    {
                        // flush list encase response was partially received
                        // before another Start Byte sequence was detected
                        receivedBytes.Clear();

                        // set flag to start adding bytes to list
                        startReceived = true;
                    }
                    else if (receivedData == dataHandShake)
                    {
                        hasHandShake = true;
                    }

                    if (startReceived)
                    {
                        // add the byte to the list
                        byte receivedDataInByte = (byte)receivedData;
                        receivedBytes.Add(receivedDataInByte);

                        // if the 268 byte buffer is exceeded
                        // and response is not receiveSerialData, then abort
                        if (receivedBytes.Count > 7)
                        {
                            if (receivedBytes.Count > maxsize && receivedBytes[netFnIndex] != receiveSerialData)
                            {
                                IpmiSharedFunc.WriteTrace(string.Format("Received data packet size is too big {0}:", 
                                    IpmiSharedFunc.ByteArrayToHexString(receivedBytes.ToArray())));
                                    messageResponse = new byte[1] { 0xBE };
                                return;
                            }
                        }
                        
                    }
                    else
                    {
                        garbageBytes.Add((byte)receivedData);
                        // Discard all the incoming garbage bytes until receiving the start byte.
                        // If receiving too many garbage data bytes, return with error code to ensure
                        // forward progress
                        garbageDataByteCount++;
                        if (garbageDataByteCount > maxGarbageDataByteCount)
                        {
                            IpmiSharedFunc.WriteTrace(string.Format("Received data max garbage data count {0} garbage data: {1}:",
                            garbageDataByteCount,  IpmiSharedFunc.ByteArrayToHexString(garbageBytes.ToArray())));
                            messageResponse = new byte[1] { 0xAB };
                            return;
                        }
                    }


                    if (receivedData == stopByte)
                    {

                        // If the stop byte has been received, validate the sequence Id 
                        // serialize the packet and return it with the success code
                        // Received data packet: [startByte][data1]...[dataN][stopByte]
                        if (receivedBytes.Count >= 7)
                        {   
   
                            // ensure packet sequence matches. it is possible late ipmi responses
                            // from previous commands can enter the UART buffer, provided the M700
                            // selection is directed at the same server.
                            if (receivedBytes[5] == sequence)
                            {
                                // if the command is a forwarded command over serial
                                // the first response will be an ack.  second response
                                // will be the actual response.
                                if (receivedBytes[netFnIndex] == ipmiFwd && receivedBytes[netFnIndex+1] == 0x00 && !ackReceived)
                                {
                                    IpmiSharedFunc.WriteTrace(string.Format("Cmd Forward Confirmation: {0}", receivedBytes.ToArray()));

                                    // Flush the list of collected bytes.
                                    receivedBytes.Clear();

                                    // Set the start by to false to begin collecting bytes again.
                                    startReceived = false;

                                    // flag ipmi ack has been received.
                                    ackReceived = true;

                                }
                                else
                                {
                                    messageResponse = receivedBytes.ToArray();
                                    return;
                                }
                            }
                            // Wrong ipmi payload has been received. Mismatched SequenceId
                            else
                            {
                                // Flush the list of collected bytes.
                                receivedBytes.Clear();

                                // Set the start by to false to begin collecting bytes again.
                                startReceived = false;
                            }
                        }
                        else
                        {
                            // if stop byte is received and packet lenght is malformed,
                            // return payload with failed response.
                            // if the stop byte is received, process the message.
                            messageResponse = receivedBytes.ToArray();

                            IpmiSharedFunc.WriteTrace("Malformed Response Packet: " + IpmiSharedFunc.ByteArrayToHexString(messageResponse));

                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TimeoutException)
                    {
                        if (hasHandShake)
                        {
                            messageResponse = new byte[1] { 0xBE };
                            IpmiSharedFunc.WriteTrace("Serial Port Timeout with handshake" + ex.Message.ToString());
                            IpmiSharedFunc.WriteTrace("Dumping Data in Receive Buffer: " + IpmiSharedFunc.ByteArrayToHexString(receivedBytes.ToArray()));
                        }
                        else
                        {
                            messageResponse = new byte[1] { 0xA3 };
                            IpmiSharedFunc.WriteTrace("Serial Port Timeout without" + ex.Message.ToString());
                            IpmiSharedFunc.WriteTrace("Dumping Data in Receive Buffer: " + IpmiSharedFunc.ByteArrayToHexString(receivedBytes.ToArray()));
                        }
                    }                    
                    return;
                }
            }
        }

        /// <summary>
        /// Flushes serial buffers (SerialPort.BaseStream.Flush())
        /// </summary>
        private void FlushBuffers()
        {
            this.serialPort.DiscardInBuffer();
            this.serialPort.DiscardOutBuffer();
        }

        /// <summary>
        /// Writes an escape byte to the serial port to cause the BMC
        /// to switch to Serial Console.
        /// </summary>
        /// <param name="escape"></param>
        internal void SendSerialEscapeCharactor(byte escape = 0xAA)
        {
            lock (this)
            {
                serialPort.BaseStream.WriteByte(escape);
            }
        }

        /// <summary>
        /// Process packed received from serial transport class.
        /// </summary>
        /// <param name="message">Message bytes.</param>
        private void ProcessReceivedMessage(byte[] message, out byte[] ipmiResponseMessage, out byte completionCode)
        {
            if (message.Length > 7)
            {
                // replace escape charactors
                message = this.ReplaceEscapeChars(message);

                completionCode = message[7];

                // Detect and ignore Serial/Modem Active Messages.
                // the responder’s address byte should be set 
                // to 81h, which is the software ID (SWID) for 
                // remote console software Or 0x8F for Serial Console.
                IpmiSharedFunc.WriteTrace("Received: " + IpmiSharedFunc.ByteArrayToHexString(message));
            }
            else
            {
                completionCode = 0xC7;

                IpmiSharedFunc.WriteTrace("Received: No response.");
            }

            ipmiResponseMessage = message;

            // check completion code
            if (message.Length > 1)
            {
                // strip the 3 byte validation message received from the 
                // transport class.
                ipmiResponseMessage = new byte[message.Length];

                // copy response packet into respones array
                Buffer.BlockCopy(message, 0, ipmiResponseMessage, 0, (message.Length));
                message = null;

                // Ipmi message heard is 7 bytes.
                if (ipmiResponseMessage.Length >= 7)
                {
                    // check resAddr
                    if (ipmiResponseMessage[1] == 0x8F || ipmiResponseMessage[1] == 0x81)
                    {
                        // Validate checsume before passing packet as valid.
                        if (!ValidateCRC(ipmiResponseMessage))
                        {
                            completionCode = 0xD6;
                        }
                    }
                    else
                    {
                        completionCode = 0xAA;
                        IpmiSharedFunc.WriteTrace("Response did contain ipmi packet: " + IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                    }

                }
                else
                {
                    completionCode = 0xC7;
                    IpmiSharedFunc.WriteTrace("Response did contain ipmi packet: " + IpmiSharedFunc.ByteArrayToHexString(ipmiResponseMessage));
                }
            }
            else
            {
                if (completionCode != 0)
                    IpmiSharedFunc.WriteTrace("Non-zero completion code, no Ipmi payload: " + IpmiSharedFunc.ByteArrayToHexString(message));
            }
        }

        /// <summary>
        /// Validate the payload checksum.  The function code checksum
        /// and rqAdd is not important to the serial client.
        /// </summary>
        private static bool ValidateCRC(byte[] message)
        {
            byte checksum = IpmiSharedFunc.TwoComplementChecksum(4, (message.Length - 2), message);
            // Compare checksum
            if (message[(message.Length - 2)] == checksum)
            {
                return true;
            }
            else
            {
                IpmiSharedFunc.WriteTrace("CheckSum Mismatch: " + message[(message.Length - 2)] + " " + checksum);
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Double byte charactors to replace ipmi escape charactors.
    /// See IPMI 2.0: 14.4.1 - Basic Mode Packet Framing
    /// See IPMI 2.0: 14.4.2 - Data Byte Escaping 
    /// </summary>
    internal class EscapeCharactor
    {
        internal byte Frame;
        internal byte[] Replace;

        internal EscapeCharactor(byte frame, byte[] replace)
        {
            this.Frame = frame;
            this.Replace = replace;
        }
    }  
}
