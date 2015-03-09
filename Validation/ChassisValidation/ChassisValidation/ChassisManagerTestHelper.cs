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

using System;
using System.Runtime.CompilerServices;

namespace ChassisValidation
{
    internal static class ChassisManagerTestHelper
    {
        private static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        ///     Checks if two values are equal. The method logs a success if the two values are equal,
        ///     or it logs a failure.
        /// </summary>
        /// <typeparam name="T">The type of the two values to be compared.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to be logged.</param>
        /// <returns>True if the two values are equal; false, otherwise.</returns>
        internal static bool AreEqual<T>(T expected, T actual, string SuccessMessage, [CallerMemberName]
                                         string testName = null)
        {
            if (expected.Equals(actual))
            {
                CmTestLog.Success(SuccessMessage, testName);
                return true;
            }
            CmTestLog.Failure(string.Format("(Expected: {0}, Actual: {1}) ", expected, actual), testName);
            return false;
        }

        /// <summary>
        ///     Checks if two values are NOT equal. The method logs a success if the two values are NOT equal,
        ///     or it logs a failure.
        /// </summary>
        /// <typeparam name="T">The type of the two values to be compared.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to be logged.</param>
        /// <returns>True if the two values are equal; false, otherwise.</returns>
        internal static bool AreNotEqual<T>(T expected, T actual, string SuccessMessage, [CallerMemberName]
                                         string testName = null)
        {
            if (!expected.Equals(actual))
            {
                CmTestLog.Success(SuccessMessage, testName);
                return true;
            }
            CmTestLog.Failure(string.Format("Verifying NOT Equalt Expected and Actual are both: {1}) ", actual), testName);
            return false;
        }
        /// <summary>
        ///     Checks if a given condition is true. The method logs a success if the condition is true,
        ///     or it logs a failure.
        /// </summary>
        /// <param name="condition">The condition to be checked.</param>
        /// <param name="message">The message to be logged.</param>
        /// <returns>True if the condition holds; false, otherwise.</returns>
        internal static bool IsTrue(bool condition, string message, [CallerMemberName]
                                    string testName = null)
        {
            if (condition)
            {
                CmTestLog.Success(message, testName);
                return true;
            }
            CmTestLog.Failure(string.Format(string.Format("The following Condition is false: {0} ", message)), testName);
            return false;
        }

        /// <summary>
        ///     An extension to an array object. Returns a random element from the array.
        ///     If the array is empty, the default value of the element type will be returned.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <returns>An random element in the array; or the default value of the element type.</returns>
        internal static T RandomOrDefault<T>(this T[] array)
        {
            return array.Length > 0 ? array[random.Next(array.Length)] : default(T);
        }

        /// <summary>
        ///     Generates a random interger between the given range.
        /// </summary>
        internal static int RandomInteger(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }

        internal static bool AreEqual(string propertyValue, int p1, string p2)
        {
            throw new NotImplementedException();
        }
    }
}
