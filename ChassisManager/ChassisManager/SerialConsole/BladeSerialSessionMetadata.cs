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
    using System.Collections.Generic;

    internal class BladeSerialSessionMetadata
    {
        #region Member objects associated with each blade

        private Object locker = new Object();
        private int bladeId;
        private string sessionToken;
        private DateTime lastActivity;
        /// <summary>
        /// Local mutable blade serial session timeout value.
        /// This value is set to the user specified timeout on success in the StartBladeSerialSession and
        /// reset to zero on success in the StopBladeserialsession. This ensures correct setting of the
        /// bladeSerialTimeout parameter in App.config.
        /// </summary>
        private int mutableBladeSerialTimeout = 0;

        /// <summary>
        /// Timeout for Blade serial session.
        /// Return default value set in app.config if there is no user-specified value.
        /// </summary>
        private int TimeoutBladeSerialSessionInSecs
        {
            get
            {
                lock (locker)
                {
                    if (mutableBladeSerialTimeout == 0)
                        return (int)ConfigLoaded.BladeSerialTimeout;
                    else
                        return mutableBladeSerialTimeout;
                }
            }

            set
            {
                lock (locker)
                {
                    mutableBladeSerialTimeout = value;
                }
            }
        }

        #endregion

        # region Constant values declaration

        // Internal session token for blade with no active serial session
        private const string inactiveBladeSerialSessionToken = "-1111";

        // Completion Codes
        private const int noSessionActive = 0x9A;
        private const int bmcBufferOverflow = 0x9B;
        private const int sessionAlreadyActive = 0xFD;

        // Maximum session timeout value
        // The StartSerialSession IPMI comand uses 7 bits to encode
        // the timeout in 30-second intervals. The maximum
        // supported is 0x7f * 30 = 3810. Round to 3600 seconds (60 minutes).
        private const int maxSessionTimeoutInSecs = 3600;

        // Maximum payload length for each SendSerialData command.
        // (60 * 1024 bytes = 61440 bytes)
        private const int maxSerialSendPayloadLength = 61440;
        // Buffer length to read for each ReceiveSerialData command (1024 bytes)
        // Since the baud rate is 115200, each read will take
        // 1024 / (115200/8) ~= 71ms per transfer.
        // Need to keep transfer time less than the CM timeout value.
        private const ushort readSerialDataBufferLength = 0x400;

        #endregion

        #region Static member variables

        private static Object serialSessionBladeListLock = new object();
        private static List<int> serialSessionBladeList = new List<int>();
        private static uint maxParallelBladeSerialSessions = ConfigLoaded.MaxSerialConsoleSessions;
        private enum UpdateListOption { DoNothing, Add, Remove };

        #endregion

        #region Private methods

        /// <summary>
        /// Atomic method to make changes (add/remove) to the list of blades with active serial session
        /// The method can also be used to return the number of active serial sessions with the DoNothing UpdateListOption
        /// </summary>
        /// <param name="bladeId"></param>
        /// <param name="option">UpdateListOption {DoNothing, Add, Remove}</param>
        /// <returns>The number of blades with active session after the requested change operation</returns>
        /// <returns>A value of -1 denotes failure to perform the requested change operation</returns>
        private int UpdateBladeToSerialSessionBladeList(int bladeId, UpdateListOption option = UpdateListOption.DoNothing)
        {
            lock (serialSessionBladeListLock)
            {
                if (option == UpdateListOption.Add)
                {
                    if (!serialSessionBladeList.Contains(bladeId) && serialSessionBladeList.Count < maxParallelBladeSerialSessions)
                        serialSessionBladeList.Add(bladeId);
                    else
                        return -1;
                }
                else if (option == UpdateListOption.Remove)
                {
                    if (serialSessionBladeList.Contains(bladeId))
                        serialSessionBladeList.Remove(bladeId);
                    else
                        return -1;
                }
                else // UpdateListOptions.DoNothing
                {
                    // Do nothing. Used to return the number of active serial sessions.
                }
                return serialSessionBladeList.Count;
            }
        }

        private bool ResetMetadata()
        {
            lock (locker)
            {
                sessionToken = inactiveBladeSerialSessionToken;
                lastActivity = DateTime.MinValue;
                // Reset mutableBladeSerialTimeout to 0 to account for default or user provided session timeout value
                mutableBladeSerialTimeout = 0;
                return true;
            }
        }

        /// <summary>
        /// Check if there was serial session activity within the timeout interval
        /// </summary>
        /// <param name="bound">The time boundary</param>
        /// <returns></returns>
        private bool IsSerialSessionActivityTimedOut()
        {
            // Determine the boundary for the timeout interval
            TimeSpan span = new TimeSpan(0, 0, TimeoutBladeSerialSessionInSecs);
            DateTime lastActivityBoundary = DateTime.Now.Subtract(span);

            lock (locker)
            {
                if (DateTime.Compare(lastActivity, lastActivityBoundary) < 0)
                {
                    // The last session activity was earlier than the boundary. 
                    // Signal inactivity timeout
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private CompletionCode CompareAndSwapMetadata(string currToken, string newToken)
        {
            return CompareAndSwapMetadata(currToken, newToken, this.lastActivity);
        }

        private CompletionCode CompareAndSwapMetadata(string currToken, string newToken, DateTime newActivityTime)
        {
            lock (locker)
            {
                // Compare tokens. Equals() method takes care of condition where both tokens are null.
                if (string.Equals(sessionToken, currToken))
                {
                    lastActivity = newActivityTime;
                    sessionToken = newToken;
                    return CompletionCode.Success;
                }
                else
                    return CompletionCode.UnspecifiedError;
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Constructor method
        /// </summary>
        /// <param name="bladeId"></param>
        internal BladeSerialSessionMetadata(int bladeId)
        {
            lock (locker)
            {
                this.bladeId = bladeId;
                this.sessionToken = inactiveBladeSerialSessionToken;
                this.lastActivity = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Checks that the blade serial session timeout value is valid
        /// </summary>
        /// <param name="timeoutInSecs"></param>
        /// <returns></returns>
        internal static bool CheckBladeSerialSessionTimeout(int timeoutInSecs)
        {
            return ((timeoutInSecs >= 0) && (timeoutInSecs <= maxSessionTimeoutInSecs)) ? true : false;
        }

        /// <summary>
        /// Function to be called periodically by the chassisinternal monitoring thread 
        /// to check for serial session inactivity
        /// </summary>
        internal void BladeSerialSessionInactivityCheck()
        {
            // If there is no active serial session on this blade, just return
            if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                inactiveBladeSerialSessionToken) == CompletionCode.Success)
            {
                return;
            }

            // If the session has been inactive for longer than the timeout value, kill the session
            if (IsSerialSessionActivityTimedOut())
            {
                Tracer.WriteWarning("BladeSerialSessionInactivityCheck: Session stopped for inactivity on blade Id {0}", this.bladeId);

                // Force the serial session to close on this blade
                Contracts.ChassisResponse sessionResponse = StopBladeSerialSession(this.bladeId, null, true);
                if (sessionResponse.completionCode != Contracts.CompletionCode.Success)
                {
                    Tracer.WriteError("BladeSerialSessionInactivityCheck: StopBladeSerialSession() Error. Completion Code: {0}",
                        sessionResponse.completionCode);
                }
            }
        }

        /// <summary>
        /// Starts the blade serial session.
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="timeoutInSecs">The timeout in secs.</param>
        /// <returns></returns>
        internal Contracts.StartSerialResponse StartBladeSerialSession(int bladeId, int timeoutInSecs)
        {
            Contracts.StartSerialResponse response = new Contracts.StartSerialResponse();
            response.completionCode = Contracts.CompletionCode.Failure;
            response.serialSessionToken = null;
            Tracer.WriteInfo("BladeSerialSessionMetadata StartBladeSerialSession(bladeId: {0})", bladeId);

            // If there is an existing Blade serial session (indicated by a valid sessionToken), 
            // return failure with appropriate completion code
            if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                inactiveBladeSerialSessionToken) != CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata. StartBladeSerialSession(bladeId: {0}): " +
                    "Start failed because of existing active session.", bladeId);
                response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                return response;
            }

            // Add the blade to the serial session active blades list if the capacity is not violated
            if (UpdateBladeToSerialSessionBladeList(this.bladeId, UpdateListOption.Add) == -1)
            {
                // Check if maximum number of serial sessions have been reached
                int numSessions = UpdateBladeToSerialSessionBladeList(this.bladeId, UpdateListOption.DoNothing);
                if (numSessions >= maxParallelBladeSerialSessions)
                {
                    Tracer.WriteError("BladeSerialSessionMetadata. StartBladeSerialSession: " +
                        "Max sessions reached. Number of sessions: " + numSessions +
                        ". Could not add Blade " + this.bladeId + " to serial session active blades list. ", this.bladeId);
                    response.completionCode = Contracts.CompletionCode.MaxSerialSessionReached;
                }
                else
                {
                    Tracer.WriteError("BladeSerialSessionMetadata. StartBladeSerialSession: " +
                        "Could not add Blade {0} to serial session active blades list.", this.bladeId);
                    response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                }               
                return response;
            }

            // Start the serial session. Use default timeout in app.config if user specified value is zero.
            int adjustedTimeout = (timeoutInSecs == 0) ? (int)ConfigLoaded.BladeSerialTimeout : timeoutInSecs;
            Ipmi.StartSerialSession startSession = WcsBladeFacade.StartSerialSession((byte)bladeId, true, adjustedTimeout);

            if (startSession.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.StartBladeSerialSession(bladeId: {0}): StartSerialSession Failed", bladeId);

                if (startSession.CompletionCode == sessionAlreadyActive)
                    response.completionCode = Contracts.CompletionCode.SerialSessionActive;
            }
            else
            {
                byte[] randomNumber = new byte[8];
                new System.Security.Cryptography.RNGCryptoServiceProvider().GetNonZeroBytes(randomNumber);

                // Initialize Blade Serial Session MetaData - this function does this ATOMICally
                // If there is an already existing Blade serial session (indicated by a valid bladeId and a valid sessionToken), return failure with appropriate completion code
                if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                    BitConverter.ToString(randomNumber), DateTime.Now) != CompletionCode.Success)
                {
                    Tracer.WriteError("BladeSerialSessionMetadata. StartBladeSerialSession(bladeId: {0}): " +
                        "Start failed because of existing active session.", bladeId);
                    response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                }
                else
                {
                    response.serialSessionToken = BitConverter.ToString(randomNumber);
                    // Initialize TimeoutBladeSerialSessionInSecs with user defined session timeout
                    TimeoutBladeSerialSessionInSecs = timeoutInSecs;
                    response.completionCode = Contracts.CompletionCode.Success;
                }
            }

            if (response.completionCode != Contracts.CompletionCode.Success)
            {
                // Remove the blade from the list since serial session is not successfully established
                if (UpdateBladeToSerialSessionBladeList(this.bladeId, UpdateListOption.Remove) == -1)
                    Tracer.WriteError("BladeSerialSessionMetadata.StartBladeSerialSession: " +
                        "Could not remove Blade {0} from the serial session active blades list.", this.bladeId);
            }

            return response;
        }

        /// <summary>
        /// Sends the blade serial data.
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="sessionToken">The session token.</param>
        /// <param name="data">The data to be sent</param>
        /// <returns></returns>
        internal Contracts.ChassisResponse SendBladeSerialData(int bladeId, string sessionToken, byte[] data)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Failure;
            Tracer.WriteInfo("BladeSerialSessionMetadata.SendBladeSerialData(bladeId: {0})", bladeId);

            // If there is NOT an already existing Blade serial session (indicated by a invalid sessionToken), 
            // return failure with appropriate completion code
            if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                inactiveBladeSerialSessionToken) == CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.SendBladeSerialData(bladeId: {0}): " +
                    "Send failed because of no active session.", bladeId);
                response.completionCode = Contracts.CompletionCode.NoActiveSerialSession;
                return response;
            }

            // If the session token is valid for this blade, update the activity time
            if (CompareAndSwapMetadata(sessionToken, sessionToken, DateTime.Now) != CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.SendBladeSerialData(bladeId: {0}): " +
                    "Send failed because session token provided does not match blade token.", bladeId);
                response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                return response;
            }

            // Check data length
            if (data.Length > maxSerialSendPayloadLength)
            {
                response.completionCode = Contracts.CompletionCode.ParameterOutOfRange;
                return response;
            }

            // Send the data to the blade
            Ipmi.SendSerialData sendSerial = WcsBladeFacade.SendSerialData((byte)bladeId, (ushort)data.Length, data);

            if (sendSerial.CompletionCode == noSessionActive)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.SendBladeSerialData(bladeId: {0}): " +
                    "BMC has no active serial session.", bladeId);
                response.completionCode = Contracts.CompletionCode.NoActiveSerialSession;
                return response;
            }
            else if (sendSerial.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.SendBladeSerialData(bladeId: {0}): Error. Completion Code: {1}",
                    bladeId, sendSerial.CompletionCode);
                return response;
            }
            response.completionCode = Contracts.CompletionCode.Success;
            return response;
        }

        /// <summary>
        /// Receives the blade serial data.
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="sessionToken">The session token.</param>
        /// <returns></returns>
        internal Contracts.SerialDataResponse ReceiveBladeSerialData(int bladeId, string sessionToken)
        {
            Contracts.SerialDataResponse response = new Contracts.SerialDataResponse();
            response.completionCode = Contracts.CompletionCode.Failure;
            Tracer.WriteInfo("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0})", bladeId);

            // If there is NOT an already existing Blade serial session (indicated by invalid sessionToken), 
            // return failure with appropriate completion code
            if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                inactiveBladeSerialSessionToken) == CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0}): " +
                    "Receive failed because of no active session.", bladeId);
                response.completionCode = Contracts.CompletionCode.NoActiveSerialSession;
                return response;
            }

            // If this blade Id does not currently hold the serial session, return failure
            if (CompareAndSwapMetadata(sessionToken, sessionToken) != CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0}): " +
                    "Receive failed because session token provided does not match blade token.", bladeId);
                response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                return response;
            }

            // Read serial data from the blade
            Ipmi.ReceiveSerialData receiveSerial = WcsBladeFacade.ReceiveSerialData((byte)bladeId, readSerialDataBufferLength);

            if (receiveSerial.CompletionCode == noSessionActive)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0}): " +
                    "BMC has no active serial session.", bladeId);
                response.completionCode = Contracts.CompletionCode.NoActiveSerialSession;
                return response;
            }
            else if (receiveSerial.CompletionCode == bmcBufferOverflow)
            {
                Tracer.WriteWarning("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0}): " +
                    "BMC buffer has overflowed.", bladeId);
                response.completionCode = Contracts.CompletionCode.BmcRxSerialBufferOverflow;
                response.statusDescription = "BMC buffer overflow occurred.";
                return response;
            }
            else if (receiveSerial.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.ReceiveBladeSerialData(bladeId: {0}): Error. Completion Code: {1}",
                    bladeId, receiveSerial.CompletionCode);
                return response;
            }
            response.data = receiveSerial.Payload;
            response.completionCode = Contracts.CompletionCode.Success;

            return response;
        }

        /// <summary>
        /// Stops the blade serial session.
        /// </summary>
        /// <param name="bladeId">The blade identifier.</param>
        /// <param name="sessionToken">The session token.</param>
        /// <param name="forceKill">if set to <c>true</c> [force kill].</param>
        /// <returns></returns>
        internal Contracts.ChassisResponse StopBladeSerialSession(int bladeId, string sessionToken, bool forceKill = false)
        {
            Contracts.ChassisResponse response = new Contracts.ChassisResponse();
            response.completionCode = Contracts.CompletionCode.Failure;
            Tracer.WriteInfo("BladeSerialSessionMetadata.Received StopBladeSerialSession(bladeId: {0})", bladeId);

            // If forceKill is false, check that there is an active session and that 
            // the session token provided by the user matches
            // the session token for the blade before closing the session.
            // Otherwise, just attempt to close the session on this blade. The forcekill
            // allows the user to forcibly close a session in case a session is opened and the CM service is restarted.
            // The session token is lost in the CM service but the BMC session will remain open.
            if (!forceKill)
            {
                // If there is not an already existing Blade serial session (indicated by an invalid sessionToken), 
                // return failure with appropriate completion code
                if (CompareAndSwapMetadata(inactiveBladeSerialSessionToken,
                        inactiveBladeSerialSessionToken) == CompletionCode.Success)
                {
                    Tracer.WriteError("BladeSerialSessionMetadata. StopBladeSerialSession() on bladeId " +
                        bladeId + " failed because of no active session.");
                    response.completionCode = Contracts.CompletionCode.NoActiveSerialSession;
                    return response;
                }

                // Only kill session if the token provided matches the current token
                if (CompareAndSwapMetadata(sessionToken, sessionToken) != CompletionCode.Success)
                {
                    response.completionCode = Contracts.CompletionCode.SerialSessionActive;
                    return response;
                }
            }

            // Stops the blade serial session
            Ipmi.StopSerialSession stopSession = WcsBladeFacade.StopSerialSession((byte)bladeId);

            if (stopSession.CompletionCode != (byte)CompletionCode.Success)
            {
                Tracer.WriteError("BladeSerialSessionMetadata.StopBladeSerialSession(bladeId: {0}): Error. Completion Code: {1}",
                    bladeId, stopSession.CompletionCode);
            }

            if (!ResetMetadata())
            {
                Tracer.WriteError("BladeSerialSessionMetadata.StopBladeSerialSession(bladeId: {0}): Unable to reset metadata");
            }

            // Remove the blade from the list since serial session is closed
            if (UpdateBladeToSerialSessionBladeList(this.bladeId, UpdateListOption.Remove) == -1)
                Tracer.WriteError("BladeSerialSessionMetadata. StopBladeSerialSession: " +
                    "Could not remove Blade {0} from the serial session active blades list.", this.bladeId);

            response.completionCode = Contracts.CompletionCode.Success;

            return response;
        }

        #endregion
    }
}
