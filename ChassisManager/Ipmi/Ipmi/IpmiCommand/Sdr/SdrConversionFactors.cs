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
    using System.Collections;

    class SdrConversionFactors
    {

        /// <summary>
        /// M, 10 bit 2'scomplient, signed
        /// </summary>
        private int m = 0;           
        
        /// <summary>
        /// B, 10 bit 2'scomplient, signed
        /// </summary>
        private int b = 0;
        
        /// <summary>
        /// R exp, 4 bits 2's complement signed 
        /// </summary>
        private int rexp = 0;        
        
        /// <summary>
        /// B exp, 4 bits 2's complement signed
        /// </summary>
        private int bexp = 0;
        
        /// <summary>
        /// Tolerance, 6 bit unsigned
        /// </summary>
        private int tolerance = 0; 
        
        /// <summary>
        /// Accruacy, 10 bit unsigned.
        /// </summary>
        private int accuracy = 0; 
        
        /// <summary>
        /// Accuracy exp, 2 bits unsigned
        /// </summary>
        private int accuracyExp = 0;

        /// <summary>
        /// Analog (numeric) data format
        /// </summary>
        private byte signature;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputArray"></param>
        public SdrConversionFactors(byte[] data, byte signature)
        {
            // m: 10 bit 2's complement, signed.          
            this.m = data[0] | ((data[1] & 0xC0) << 2);
            this.m = TwosComplement(m, 10);

            // m: 10 bit 2's complement, signed.
            this.b = data[2] | ((data[3] & 0xC0) << 2);
            this.b = TwosComplement(b, 10);

            // r exp: [7:4] 4 bits 2's complement signed
            this.rexp = (data[5] >> 4) & 0x0F;
            this.rexp = TwosComplement(rexp, 4);

            // b exp: [3:0] 4 bits 2's complement signed
            this.bexp = data[5] & 0x0F;
            this.bexp = TwosComplement(bexp, 4);

            // tolerance = 6 bit unsigned
            this.tolerance = data[1] & 0x3f;

            // accruacy: 10 bit unsigned.  
            this.accuracy = ((data[3] & 0x3f) | ((data[4] & 0xf0) << 4));

            // accuracy exp: [3:2] 2 bits unsigned
            this.accuracyExp = (data[4] >> 2) & 0x3;

            this.signature = signature;
        }

        /// <summary>
        /// 
        /// </summary>
        public SdrConversionFactors()
        {
        }

        /// <summary>
        /// M constant multiplier
        /// </summary>
        public int M
        {
            get { return this.m; }
        }

        /// <summary>
        /// B additive offset
        /// </summary>
        public int B
        {
            get { return this.b; }
        }

        /// <summary>
        /// Result exponent
        /// </summary>
        public int Rexp
        {
            get { return this.rexp; }
        }

        /// <summary>
        /// B offset exponent
        /// </summary>
        public int Bexp
        {
            get { return this.rexp; }
        }

        /// <summary>
        /// Tolerance
        /// </summary>
        public int Tolerance
        {
            get { return this.tolerance; }
        }

        /// <summary>
        /// Accuracy
        /// </summary>
        public int Accuracy
        {
            get { return this.accuracy; }
        }

        /// <summary>
        /// Accuracy exponent
        /// </summary>
        public int AccuracyExp
        {
            get { return this.accuracyExp; }
        }

        /// <summary>
        /// converts sensor reading to units using the conversion formula
        /// </summary>
        /// <param name="value">raw sensor reading</param>
        /// <returns>reading in sensor specific units</returns>
        public Double ConvertReading(double value)
        {
            return ((m * value) + (b * Math.Pow(10, bexp))) * Math.Pow(10, rexp);
        }

        /// <summary>
        /// formats raw sensor reading per the SDR data format field
        /// full sensor record byte 21 bits 7:6.
        /// </summary>
        /// <param name="rawReading">sensor reading</param>
        /// <param name="dataFormat">SDR data format field</param>
        /// <returns></returns>
        public int FormatReading(int rawReading)
        {
            // default return value
            int value = 0;

            // switch between unsigned, 1's complement signed
            // or 2's complement signed
            switch (signature)
            {
                case 0x00:  // unsigned
                    value = rawReading;
                    break;
                case 0x01:  // 1's complement signed
                    value = OnesComplement(rawReading, 8);
                    break;
                case 0x02:  // 2's complement signed
                    value = TwosComplement(rawReading, 8);
                    break;
                default:
                    break;
            }

            return value;
        }

        /// <summary>
        /// Gets two's complement of a signed integer
        /// </summary>
        /// <param name="number">signed integer (value) </param>
        /// <param name="index">number of bits in the signed int</param>
        /// <returns>two's complement of a signed int</returns>
        internal static int TwosComplement(int number, int bits)
        {
            // zero count offset
            bits--;

            // convert to bit array.
            BitArray compArray = new BitArray(BitConverter.GetBytes(number));

            // a true most significant bit (MSB) indicates a negative 
            // nubmer and all bits must be flipped (~).
            if (compArray.Get(bits))
            {
                // invert all bits (complement)
                compArray.Not();

                // set all unused bits to false.
                for (int i = bits; i < compArray.Length; i++)
                {
                    compArray[i] = false;
                }

                // convert bitarray to integer, using copyto.
                int[] retunArray = new int[1];
                compArray.CopyTo(retunArray, 0);

                // return negative complement
                // +1 for two's complement
                return -(retunArray[0] + 1);
            }

            // positive complement = binary representation
            return number;
        }

        /// <summary>
        /// Gets one's complement of a signed integer
        /// </summary>
        /// <param name="number">signed integer (value) </param>
        /// <param name="index">number of bits in the signed int</param>
        /// <returns>one's complement of a signed int</returns>
        internal static int OnesComplement(int number, int bits)
        {
            // zero count offset
            bits--;

            // convert to bit array.
            BitArray compArray = new BitArray(BitConverter.GetBytes(number));

            // a true most significant bit (MSB) indicates a negative 
            // nubmer and all bits must be flipped (~).
            if (compArray.Get(bits))
            {
                // invert all bits (complement)
                compArray.Not();

                // set all unused bits to false.
                for (int i = bits; i < compArray.Length; i++)
                {
                    compArray[i] = false;
                }

                // convert bitarray to integer, using copyto.
                int[] retunArray = new int[1];
                compArray.CopyTo(retunArray, 0);

                // return negative complement
                return -(retunArray[0]);
            }

            // positive complement = binary representation
            return number;
        }

    }
}
