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
    /// <summary>
    /// Represents the IPMI 'Get SEL Entry' request message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Storage, IpmiCommand.GetSelEntry, 6)]
    internal class SelEntryRequest : IpmiRequest
    {
        /// <summary>
        /// Reservation Id.
        /// </summary> 
        private ushort reservationId;

        /// <summary>
        /// SEL RecordID
        /// </summary> 
        private ushort recordId;

        /// <summary>
        /// SEL Entry Offste.
        /// </summary> 
        private byte offset;

        /// <summary>
        /// Number of bytes to read. 0xFF for entire record.
        /// </summary> 
        private byte readbytes = 0xFF;

        /// <summary>
        /// Initializes a new instance of the SelEntryRequest class.
        /// </summary>
        internal SelEntryRequest(ushort reserveId, ushort record, byte offset)
        {
            this.reservationId = reserveId;
            this.recordId = record;
            this.offset = offset;
        }

        /// <summary>
        /// Gets reservation Id
        /// </summary>       
        [IpmiMessageData(0)]
        public ushort ReservationId
        {
            get { return this.reservationId; }

        }

        /// <summary>
        /// SEL RecordID
        /// </summary>       
        [IpmiMessageData(2)]
        public ushort RecordId
        {
            get { return this.recordId; }

        }

        /// <summary>
        /// SEL Offset
        /// </summary>       
        [IpmiMessageData(4)]
        public byte OffSet
        {
            get { return this.offset; }

        }

        /// <summary>
        /// Number of bytes to read.
        /// </summary>       
        [IpmiMessageData(5)]
        public byte ReadBytes
        {
            get { return this.readbytes; }

        }
    }
}
