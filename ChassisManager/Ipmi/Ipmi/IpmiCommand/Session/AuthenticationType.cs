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
    /// Ipmi Authentication algorithms
    /// </summary>
    public enum CipherSuite
    {
        /// <summary>
        /// Authentication = None, Integrity = None, Confidentiality = None
        /// </summary>
        None = 0,

        /// <summary>
        /// Authentication = HMAC-SHA1, Integrity = None, Confidentiality = None
        /// </summary>        
        Sha1NoneNone = 1,

        /// <summary>
        /// Authentication = HMAC-SHA1, Integrity = HMAC-SHA1-96, Confidentiality = None
        /// </summary>
        Sha1Sha1None = 2,

        /// <summary>
        /// Authentication = HMAC-SHA1, Integrity = HMAC-SHA1-96, Confidentiality = AES
        /// </summary>
        Sha1Sha1Aes = 3,

        /// <summary>
        /// Authentication = HMAC-MD5, Integrity = None, Confidentiality = None
        /// </summary>
        MD5NoneNone = 6,

        /// <summary>
        /// Authentication = HMAC-MD5, Integrity = HMAC-MD5, Confidentiality = None
        /// </summary>
        MD5MD5None = 7,

        /// <summary>
        /// Authentication = HMAC-MD5, Integrity = HMAC-MD5, Confidentiality = AES
        /// </summary>
        MD5MD5Aes = 8,

        /// <summary>
        /// Authentication = HMAC-MD5, Integrity = MD5-128, Confidentiality = None
        /// </summary>
        MD5MD5128 = 11,

        /// <summary>
        /// Authentication = None, Integrity = MD5-128, Confidentiality = AES
        /// </summary>
        MD5MD5128Aes = 12
    }

    /// <summary>
    /// ipmi v1.5 authentication algorithms
    /// </summary>
    public enum AuthenticationType
    {
        None = 0,

        Straight = 4
    }

    /// <summary>
    /// ipmi v2.0 authentication algorithms 
    /// </summary>
    public enum RmcpAuthentication : byte
    {
        None = 0x00,

        HMACSHA1 = 0x01,

        HMACMD5 = 0x02,

        HMACSHA256 = 0x03
    }

    /// <summary>
    /// ipmi v2.0 integrity algorithms 
    /// </summary>
    public enum RmcpIntegrity : byte
    {
        None = 0x00,

        HMACSHA196 = 0x01,

        HMACMD5128 = 0x02,

        MD5128 = 0x03,

        HMACSHA256128 = 0x04

    }

    /// <summary>
    /// ipmi v2.0 confidentiality algorithms 
    /// </summary>
    public enum RmcpConfidentiality : byte
    {
        None = 0x00,

        AESCBC128 = 0x01
    }
}
