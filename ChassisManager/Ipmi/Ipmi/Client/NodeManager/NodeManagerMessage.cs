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
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Node Manager message encapsulation class.  serialize NodeManager request/response message into bytes.
    /// </summary>
    public abstract class NodeManagerMessage
    {
        #region Private Variables

        /// <summary>
        /// NodeManagerMessageAttribute instance attached to this NodeManagerMessage instance.
        /// </summary>
        private readonly NodeManagerMessageAttribute nodeManagerMessageAttribute;

        /// <summary>
        /// Node Manager CompletionCode
        /// </summary>
        private byte completionCode;

        /// <summary>
        /// Node Manager data specific to the current message.
        /// </summary>
        private byte[] data;
        
        #endregion

        /// <summary>
        /// Gets the Node Manager data specific to the current message.
        /// </summary>
        /// <value>Node Manager data specific to the current message.</value>
        internal byte[] Data
        {
            get { return this.data; }
        }


        /// <summary>
        /// Gets the Node Manager function of this message.
        /// </summary>
        /// <value>The Node Manager function.</value>
        /// <remarks>Used for generating the Node Manager header over-the-wire bytes.</remarks>
        internal virtual NodeManagerFunctions NodeManagerFunction
        {
            get { return this.nodeManagerMessageAttribute.NodeManagerFunctions; }
        }

        /// <summary>
        /// Gets the Node Manager command within the scope of the message Node Manager function.
        /// </summary>
        /// <value>The Node Manager command within the scope of the message Node Manager function.</value>
        /// <remarks>Used for generating the NodeManager header over-the-wire bytes.</remarks>
        internal virtual NodeManagerCommand NodeManagerCommand
        {
            get { return this.nodeManagerMessageAttribute.NodeManagerCommand; }
        }

        /// <summary>
        /// Node Manager message completion code
        /// </summary>
        internal byte CompletionCode
        {
            get { return this.completionCode; }
            set { this.completionCode = value; }
        }
        
        /// <summary>
        /// Initialize class
        /// </summary>
        internal NodeManagerMessage()
        {
            NodeManagerMessageAttribute[] attributes =
                (NodeManagerMessageAttribute[])this.GetType().GetCustomAttributes(typeof(NodeManagerMessageAttribute), true);
            if (attributes.Length != 1)
            {
                throw new InvalidOperationException();
            }

            this.nodeManagerMessageAttribute = attributes[0];
        }



        #region NodeManager

               
        /// <summary>
        /// Initialize class with NodeManagerMessage and message, main entry point.
        /// </summary>
        protected NodeManagerMessage(byte[] message, int length)
        {

            int offset = 0;

            // Node Manager message framing
            byte start = message[offset];
            byte rqAddr = message[offset + 1];
            byte netFnAndrqLun = message[offset + 2];
            // byte checksum1 = message[offset + 3];
            byte rsAddr = message[offset + 4];
            byte rqSeqAndrsLun = message[offset + 5];
            byte command = message[offset + 6];

            // completion code
            this.completionCode = message[offset + 7];

            // serial stop char
            byte stop = message[length - 1];

            //  8 bytes =   byte[0] reqAdd, byte[1] netFnAndrqLun, byte[2] checksum1
            //              byte[3] rsAddr, byte[4] rqSeqAndrsLun, byte[5] command, byte[6] completionCode
            //              byte[7] checksum.
            int dataLength = (length - 8);

            // set data array
            this.data = new byte[dataLength];
            
            // copy message payload into data array with
            // (offset + 7) to strip Node Manager message framing bits.
            Buffer.BlockCopy(message, offset + 7, this.data, 0, dataLength);
        }
        
        internal void InitializeNodeManager(byte[] message, int length, byte reqSeq)
        {
            // offset data lenght
            int offset = 0;

            // get the message data
            this.data = new byte[length];
            Buffer.BlockCopy(message, offset, this.data, 0, length);

            if (this.CompletionCode == 0)
            {
                foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
                {
                    NodeManagerMessageDataAttribute[] attributes =
                        (NodeManagerMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(NodeManagerMessageDataAttribute), true);

                    if (attributes.Length > 0)
                    {
                        if (propertyInfo.PropertyType == typeof(byte))
                        {
                            if (attributes[0].Offset < this.data.Length)
                            {
                                propertyInfo.SetValue(this, this.data[attributes[0].Offset], null);
                            }
                        }
                        else if (propertyInfo.PropertyType == typeof(ushort))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToUInt16(this.data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(short))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToInt16(this.data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(uint))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToUInt32(this.data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(int))
                        {
                            propertyInfo.SetValue(this, BitConverter.ToInt32(this.data, attributes[0].Offset), null);
                        }
                        else if (propertyInfo.PropertyType == typeof(byte[]))
                        {
                            int propertyLength = attributes[0].Length;

                            if (propertyLength == 0)
                            {
                                propertyLength = data.Length - attributes[0].Offset;
                            }

                            if (attributes[0].Offset < data.Length)
                            {
                                byte[] propertyData = new byte[propertyLength];
                                Buffer.BlockCopy(data, attributes[0].Offset, propertyData, 0, propertyData.Length);

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
        /// Convert the Node Manager request meeting into a byte stream for transmission 
        /// to the BMC over Serial.
        /// [Byte 0]    [Byte 1]    [Byte 2]    [Byte 3]
        /// [StrChar]   [rsAddr]    [netFn]     [chksum]
        /// [Byte 4]    [Byte 5]    [Byte 6]    [Byte 7]    [Byte 8]    [Byte 9]
        /// [rqAddr]    [rqSeq]     [cmd]       [payload]   [cksum]     [stop]
        /// </summary>
        /// <param name="Node ManagerClient">Node ManagerClient instance.</param>
        /// <returns>byte array representing the Serial data to send.</returns>
        internal byte[] GetNodeManagerBytes(byte slaveId, byte rqSeq, bool trackResponse)
        {
            int dataLength = 0;

            #region dataLenght

            foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
            {
                NodeManagerMessageDataAttribute[] attributes2 =
                    (NodeManagerMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(NodeManagerMessageDataAttribute), true);

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

            #endregion

            // 07 bytes =   byte[0] slave Id, byte[1] netFnAndrqLun, byte[2] checksum1
            //              byte[3] rqAddr, byte[4] rqSeqAndrsLun, byte[5] command, byte[x] payload
            //              byte[6] checksum
            int messageLength = 7 + dataLength;

            byte[] messagedata = new byte[messageLength];

            // Encapsulate Node Manager message
            #region Message header

            messagedata[0] = slaveId;  // slave Id
            messagedata[1] = (byte)((byte)this.NodeManagerFunction << 2);  // netFN
            messagedata[2] = 0x00;  // checksum zero'd for now.  Set after data added
            messagedata[3] = 0x20; // for BMC when bridging to Intel ME.
            if (trackResponse)
                messagedata[4] = (byte)(((byte)rqSeq << 2) | 0x02); // rqSeq/rqLun = 10.
            else
                messagedata[4] = (byte)((byte)rqSeq << 2); // rqSeq/rqLun = 0x00.
            messagedata[5] = (byte)this.NodeManagerCommand; // cmd

            #endregion
            
            // Node Manager message data
            #region Get message data.

            if (dataLength > 0)
            {
                data = new byte[dataLength];

                foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
                {
                    NodeManagerMessageDataAttribute[] attributes =
                        (NodeManagerMessageDataAttribute[])propertyInfo.GetCustomAttributes(typeof(NodeManagerMessageDataAttribute), true);

                    if (attributes.Length > 0)
                    {
                        if (propertyInfo.PropertyType == typeof(byte))
                        {
                            data[attributes[0].Offset] = (byte)propertyInfo.GetValue(this, new Object[0]);
                        }
                        else if (propertyInfo.PropertyType == typeof(ushort))
                        {
                            byte[] raw = BitConverter.GetBytes((ushort)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, data, attributes[0].Offset, 2);
                        }
                        else if (propertyInfo.PropertyType == typeof(short))
                        {
                            byte[] raw = BitConverter.GetBytes((short)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, data, attributes[0].Offset, 2);
                        }
                        else if (propertyInfo.PropertyType == typeof(uint))
                        {
                            byte[] raw = BitConverter.GetBytes((uint)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, data, attributes[0].Offset, raw.Length);
                        }
                        else if (propertyInfo.PropertyType == typeof(int))
                        {
                            byte[] raw = BitConverter.GetBytes((int)propertyInfo.GetValue(this, new Object[0]));
                            Buffer.BlockCopy(raw, 0, data, attributes[0].Offset, raw.Length);
                        }
                        else if (propertyInfo.PropertyType == typeof(byte[]))
                        {
                            byte[] raw = (byte[])propertyInfo.GetValue(this, new Object[0]);
                            Buffer.BlockCopy(raw, 0, data, attributes[0].Offset, raw.Length);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                }

                // Add Node Manager message data
                // dataIndex + 6 offsets encapsulated Node Manager message
                Buffer.BlockCopy(data, 0, messagedata, 6, data.Length);
            }

            #endregion

            // Add checksum
            #region Message checksum

            // NodeManager message encapsulation format checksum 1
            // Set checksum 1: Byte 0 = rsAdd, 
            // Byte 1 = netFn, Byte 2 = Checksum
            messagedata[2] = IpmiSharedFunc.TwoComplementChecksum(0, 2, messagedata);

            // Node Manager message encapsulation format checksum 2
            // checksum 2 begins after checksum 1. 
            // checksum 2 calculated from rqAddr to end of data lenght.
            messagedata[(6 + dataLength)] = IpmiSharedFunc.TwoComplementChecksum(3, (6 + dataLength), messagedata);

            #endregion

            return messagedata;
        }

        /// <summary>
        /// Get Serial basic message bytes for transmission.
        /// </summary>
        protected virtual int GetNodeManagerBytes(byte[] message, int offset, int length)
        {
            Debug.Assert(false);
            return 0;
        }

        #endregion

    }
}
