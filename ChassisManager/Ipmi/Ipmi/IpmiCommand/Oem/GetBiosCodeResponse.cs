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

    /// <summary>
    /// Represents the IPMI 'Get BIOS Code' application response message.
    /// </summary>
    [IpmiMessageResponse(IpmiFunctions.OemCustomGroup, IpmiCommand.GetBiosCode)]
    internal class GetBiosCodeResponse : IpmiResponse
    {
        /// <summary>
        /// BIOS Post Code
        /// </summary>
        private byte[] postCode = {};

        /// <summary>
        /// BIOS POST codes in hexadecimal
        /// </summary>       
        [IpmiMessageData(0)]
        public byte[] RawPostCode
        {
            get { return this.postCode; }
            set { this.postCode = value; }
        }

        /// <summary>
        /// BIOS Port 80 Code
        /// </summary>
        public string PostCode
        {
            get 
            {
                if (postCode != null)
                {
                    StringBuilder result = new StringBuilder();

                    if (postCode.Length > 0)
                    {
                        foreach (byte b in postCode)
                        {
                            result.Append(string.Format("{0:X2} ", b));
                        }
                    }

                    return result.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

    }
}
