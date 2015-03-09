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
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Ipmi message encapsulation class. 
    /// </summary>
    internal abstract class IpmiMessage
    {
        #region Private Variables

        /// <summary>
        /// IpmiMessageAttribute instance attached to this IpmiMessage instance.
        /// </summary>
        private readonly IpmiMessageAttribute ipmiMessageAttribute;

        /// <summary>
        /// valid ipmi response received.
        /// </summary>
        private bool ipmiResponseReceived = true;

        /// <summary>
        /// Ipmi CompletionCode
        /// </summary>
        private byte completionCode;

        internal byte[] Data
        {
            get;
            private set;
        }

       
        #endregion

        /// <summary>
        /// Valid Ipmi UpdResponse received.
        /// </summary>
        internal bool IpmiResponseReceived
        {
            get { return this.ipmiResponseReceived; }
        }

        /// <summary>
        /// Gets the IPMI function of this message.
        /// </summary>
        /// <value>The IPMI function.</value>
        /// <remarks>Used for generating the IPMI header over-the-wire bytes.</remarks>
        internal virtual IpmiFunctions IpmiFunction
        {
            get { return this.ipmiMessageAttribute.IpmiFunctions; }
        }

        /// <summary>
        /// Gets the IPMI command within the scope of the message IPMI function.
        /// </summary>
        /// <value>The IPMI command within the scope of the message IPMI function.</value>
        /// <remarks>Used for generating the IPMI header over-the-wire bytes.</remarks>
        internal virtual IpmiCommand IpmiCommand
        {
            get { return this.ipmiMessageAttribute.IpmiCommand; }
        }

        /// <summary>
        /// Ipmi message completion code
        /// </summary>
        internal byte CompletionCode
        {
            get { return this.completionCode; }
            set { this.completionCode = value; }
        }
        
        /// <summary>
        /// Initialize class
        /// </summary>
        internal IpmiMessage()
        {
            IpmiMessageAttribute[] attributes =
                (IpmiMessageAttribute[])this.GetType().GetCustomAttributes(typeof(IpmiMessageAttribute), true);
            if (attributes.Length != 1)
            {
                throw new InvalidOperationException();
            }

            this.ipmiMessageAttribute = attributes[0];
        }

        /// <summary>
        /// Gets payload lenght
        /// </summary>
        /// <returns></returns>
        private int GetPayloadLenght()
        {
            int dataLength = 0;

            foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
            {
                IpmiMessageDataAttribute[] attributes2 =
                    (IpmiMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(IpmiMessageDataAttribute), true);

                if (attributes2.Length > 0)
                {
                    if (propertyInfo.PropertyType == typeof(byte))
                    {
                        dataLength += 1;
                    }
                    else if (propertyInfo.PropertyType == typeof(ushort))
                    {
                        dataLength += 2;
                    }
                    else if (propertyInfo.PropertyType == typeof(short))
                    {
                        dataLength += 2;
                    }
                    else if (propertyInfo.PropertyType == typeof(int))
                    {
                        dataLength += 4;
                    }
                    else if (propertyInfo.PropertyType == typeof(uint))
                    {
                        dataLength += 4;
                    }
                    else if (propertyInfo.PropertyType == typeof(byte[]))
                    {
                        byte[] bytes = (byte[])propertyInfo.GetValue(this, null);
                        dataLength += bytes.Length;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
            }

            return dataLength;

        }

        /// <summary>
        /// Method to conver payload.
        /// </summary>
        /// <param name="dataLength">payload lenght</param>
        /// <returns>payload size</returns>
        private byte[] GetPayload(int dataLength)
        {
            // Ipmi message data
            byte[] payload = new byte[dataLength];

            if (dataLength > 0)
            {
                foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
                {
                    IpmiMessageDataAttribute[] attributes =
                        (IpmiMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(IpmiMessageDataAttribute), true);

                    if (attributes.Length > 0)
                    {
                        if (propertyInfo.PropertyType == typeof(byte))
                        {
                            payload[attributes[0].Offset] = (byte)propertyInfo.GetValue(this, new Object[0]);
                        }
                        else if (propertyInfo.PropertyType == typeof(ushort))
                        {
                            byte[] raw = BitConverter.GetBytes((ushort)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, payload, attributes[0].Offset, 2);
                        }
                        else if (propertyInfo.PropertyType == typeof(short))
                        {
                            byte[] raw = BitConverter.GetBytes((short)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, payload, attributes[0].Offset, 2);
                        }
                        else if (propertyInfo.PropertyType == typeof(uint))
                        {
                            byte[] raw = BitConverter.GetBytes((uint)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, payload, attributes[0].Offset, raw.Length);
                        }
                        else if (propertyInfo.PropertyType == typeof(int))
                        {
                            byte[] raw = BitConverter.GetBytes((int)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, payload, attributes[0].Offset, raw.Length);
                        }
                        else if (propertyInfo.PropertyType == typeof(byte[]))
                        {
                            byte[] raw = (byte[])propertyInfo.GetValue(this, new Object[0]);
                            Buffer.BlockCopy(raw, 0, payload, attributes[0].Offset, raw.Length);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                }
            }

            return payload;
        }

        /// <summary>
        /// Set the response properties
        /// </summary>
        /// <param name="message">raw ipmi message</param>
        /// <param name="offset">offset into the message</param>
        /// <param name="dataLength">payload lenght</param>
        private void SetProperties(byte[] message, int offset, int dataLength)
        {
            // get the message data
            Data = new byte[dataLength];

            Buffer.BlockCopy(message, offset, Data, 0, dataLength);

            if (this.CompletionCode == 0)
            {
                foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
                {
                    IpmiMessageDataAttribute[] attributes =
                        (IpmiMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(IpmiMessageDataAttribute), true);

                    if (attributes.Length > 0)
                    {
                        if (propertyInfo.PropertyType == typeof(byte))
                        {
                            if (attributes[0].Offset < Data.Length)
                            {
                                propertyInfo.SetValue(this, Data[attributes[0].Offset], null);
                            }
                        }
                        else if (propertyInfo.PropertyType == typeof(ushort))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToUInt16(Data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(short))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToInt16(Data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(uint))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToUInt32(Data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(int))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToInt32(Data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(byte[]))
                        {
                            int propertyLength = attributes[0].Length;

                            if (propertyLength == 0)
                            {
                                propertyLength = Data.Length - attributes[0].Offset;
                            }

                            if (attributes[0].Offset < Data.Length)
                            {
                                byte[] propertyData = new byte[propertyLength];
                                Buffer.BlockCopy(Data, attributes[0].Offset, propertyData, 0, propertyData.Length);

                                propertyInfo.SetValue(this, propertyData, null);
                            }
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Initialize Ipmi response messages and sets response properties.
        /// </summary>
        /// <param name="transport">Ipmi transport framing</param>
        /// <param name="message">Ipmi message payload</param>
        /// <param name="length">Ipmi message payload lenght</param>
        /// <param name="reqSeq">Ipmi request sequence number</param>
        internal void Initialize(IpmiTransport transport, byte[] message, int length, byte reqSeq)
        {   
            // offset for data indexing
            int offset = 0;
            int dataLength = length;

            if (transport == IpmiTransport.Serial)
            {
                // index past serial frame byte [start byte]
                offset++;

                // ipmi encapsulation header
                byte rqAddr = message[offset++];            // rdAddr
                byte netFnrqLun = message[offset++];        // netFn
                byte checksum1 = message[offset++];         // checksum 1
                byte rsAddr = message[offset++];            // rsAddr
                byte rqSeqrsLun = message[offset++];        // rqSeq/rqLun
                byte command = message[offset++];           // cmd
                this.completionCode = message[offset++];    // completion code

                // update sequence number once there's a receive match
                if (((byte)rqSeqrsLun >> 2) != reqSeq)
                    this.completionCode = 0xCF;

                // Deduct 10 bytes for message header and frame bytes.
                // 10 bytes =   byte[0] start, byte[1] reqAdd, byte[2] netFnAndrqLun, byte[3] checksum1
                //              byte[5] rsAddr, byte[6] rqSeqAndrsLun, byte[7] command, byte[8] completionCode
                //              byte[9] checksum, byte[10] end
                dataLength = (length - 10);

                // checksum 2
                byte checksum2 = message[(offset + dataLength)];
            }
            else if (transport == IpmiTransport.Wmi)
            {
                // completion code
                this.completionCode = message[offset];

                // index past Completion Code byte
                offset++;

                // remove 1 for the completion code byte.
                dataLength--;
            }

            // set response properties.
            SetProperties(message, offset, dataLength);
        }

        /// <summary>
        /// Convert the IPMI request meeting into a byte stream for transmission 
        /// to the BMC over Serial.
        /// [Byte 0]    [Byte 1]    [Byte 2]    [Byte 3]
        /// [StrChar]   [rsAddr]    [netFn]     [chksum]
        /// [Byte 4]    [Byte 5]    [Byte 6]    [Byte 7]    [Byte 8]    [Byte 9]
        /// [rqAddr]    [rqSeq]     [cmd]       [payload]   [cksum]     [stop]
        /// </summary>
        /// <param name="IpmiRqSeq">Sequence id for message.</param>
        /// <returns>byte array representing the Serial data to send.</returns>
        internal byte[] GetBytes(IpmiTransport transport, byte IpmiRqSeq)
        {
            // payload lenght
            int dataLength = GetPayloadLenght();

            // payload bytes
            byte[] payload = GetPayload(dataLength);

            // response message lenght
            int messageLength = dataLength;

            if (transport == IpmiTransport.Serial)
            {
                // Add 9 bytes for 7 byte header and start + stop byte
                // 09 bytes =   byte[1] start, byte[2] rsAdd, byte[3] netFnAndrqLun, byte[4] checksum1
                //              byte[5] rqAddr, byte[6] rqSeqAndrsLun, byte[7] command, byte[x] payload
                //              byte[8] checksum, byte[09] stop.
                messageLength += 9;
            }
            else if (transport == IpmiTransport.Wmi)
            {
                // Add 5 bytes for Wmi paramaters
                // 05 bytes =   byte[1] = "Command", byte[2] = "NetworkFunction", byte[3] = "LUN"
                //              byte[4] = "RequestDataSize" byte[5] = "ResponderAddress" byte[x] payload
                messageLength += 5;
            }

            byte[] messagedata = new byte[messageLength];
            
            if (transport == IpmiTransport.Serial)
            {
                // Step 1. Add Message Header.
                #region Message header

                messagedata[1] = 0x20;  // rsAddr
                messagedata[2] = (byte)((byte)this.IpmiFunction << 2);  // netFN
                messagedata[3] = 0x00;  // checksum zero'd for now.  Set after data.
                messagedata[4] = 0x81; // for serial can be 0x8F;
                messagedata[5] = (byte)((byte)IpmiRqSeq << 2); // rqSeq/rqLun
                messagedata[6] = (byte)this.IpmiCommand; // cmd

                #endregion

                // Step 2. Add payload Data.
                // Add ipmi message data
                // dataIndex + 7 offsets encapsulated ipmi message header & start charactor
                Buffer.BlockCopy(payload, 0, messagedata, 7, payload.Length);

                // Step 3. Add Message Checksum
                #region Message checksum

                // Set checksum 1: Byte 0 = Start Char, Byte 1 = rsAdd, 
                // Byte 2 = netFn, Byte 3 = Checksum
                messagedata[3] = IpmiSharedFunc.TwoComplementChecksum(1, 3, messagedata);

                // Ipmi message encapsulation format checksum 2
                // checksum 2 begins after checksum 1. 
                // checksum 2 calculated from rqAddr to end of data lenght.
                messagedata[(7 + dataLength)] = IpmiSharedFunc.TwoComplementChecksum(4, (7 + dataLength), messagedata);

                #endregion
            }
            else if(transport == IpmiTransport.Wmi)
            {
                // Fill in input parameter values
                messagedata[0] = (byte)this.IpmiCommand;
                messagedata[1] = (byte)this.IpmiFunction;
                messagedata[2] = 0x00;
                messagedata[3] = (byte)dataLength;
                messagedata[4] = 32;

                // Step 2. Add payload Data.
                // Add ipmi message data
                // dataIndex + 5 offsets encapsulated ipmi message header above
                Buffer.BlockCopy(payload, 0, messagedata, 5, payload.Length);
                
            }

            return messagedata;
        }
    }
}
