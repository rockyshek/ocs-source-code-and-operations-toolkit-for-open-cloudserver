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
    /// Represents the IPMI 'Reserve SEL' response message.
    /// </summary>
    [IpmiMessageRequest(IpmiFunctions.Storage, IpmiCommand.SelReserve)]
    internal class SelReserveResponse : IpmiResponse
    {

        /// <summary>
        /// Reservation Id LS (Least Significant Byte).
        /// </summary>  
        private byte reservationIdLS;


        /// <summary>
        /// Reservervation Id MS (Most Significant byte).
        /// </summary>  
        private byte reservationIdMS;


        /// <summary>
        /// Gets reservationId LS byte (Least Significant byte).
        /// </summary>       
        [IpmiMessageData(0)]
        public byte ReservationIdLS
        {
            get { return this.reservationIdLS; }
            set { this.reservationIdLS = value; }
        }

        /// <summary>
        /// Gets reservationId MS byte (Most Significant byte).
        /// </summary>       
        [IpmiMessageData(1)]
        public byte ReservationIdMS
        {
            get { return this.reservationIdMS; }
            set { this.reservationIdMS = value; }
           
        }
    }
}
