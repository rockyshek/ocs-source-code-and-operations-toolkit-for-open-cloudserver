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
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.GFS.WCS.ChassisManager.Ipmi;

    public static class HelperFunction
    {
        /// <summary>
        /// Known Inlet sensor offsets needed by Manufacturer Id and product.
        /// </summary>
        internal static Dictionary<WcsBladeIdentity, InletTempOffset> InletTempOffsets = new Dictionary<WcsBladeIdentity, InletTempOffset>(2) 
        {
            {new WcsBladeIdentity(7244, 1030), new InletTempOffset(13, 0, 0.1, 4, true)},
            {new WcsBladeIdentity(40092, 0), new InletTempOffset(18.831, 2, 33.597, 19.124)} //TODO:  Change Product Id to: 1030 when firmware aligns.
        };


        /// <summary>
        /// Stores Max Pwm Requirement.  Used for Data Center AHU integration.
        /// </summary>
        public static volatile byte MaxPwmRequirement;

        /// <summary>
        /// Generates the text representation of an array of bytes
        /// </summary>
        /// <param name="Bytes"></param>
        /// <returns></returns>
        public static string ByteArrayToText(byte[] byteArray)
        {
            return IpmiSharedFunc.ByteArrayToHexString(byteArray);
        }

        /// <summary>
        /// Byte to Hex string representation
        /// </summary>  
        public static string ByteToHexString(byte Bytes)
        {
            return IpmiSharedFunc.ByteToHexString(Bytes);
        }

        /// <summary>
        /// Convert string to byte array
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>byte array representing string</returns>
        internal static byte[] GetBytes(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            return bytes;
        }
    }
}
