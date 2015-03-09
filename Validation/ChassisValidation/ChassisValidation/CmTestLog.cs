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
    public class CmTestLog : Log
    {
        public static void Start(string message = null, [CallerMemberName]
                                 string testName = null) 
        {
            Log.Notice(testName, message ?? "TEST STARTED");
        }

        public static void End(string message = null, [CallerMemberName]
                               string testName = null) 
        {
            Log.Notice(testName, string.Format("{0}{1}", message ?? "TEST COMPLETED", Environment.NewLine));
        }

        public static void End(bool passed, [CallerMemberName]
                               string testName = null)
        {
            if (passed)
            {
                Log.Success(testName, string.Format("TEST PASSED{0}", Environment.NewLine));
            }
            else
            {
                Log.Error(testName, string.Format("TEST FAILED{0}", Environment.NewLine));
            }
        }

        public static void Info(string message, [CallerMemberName]
                                string testName = null)
        {
            Log.Info(testName, message);
        }

        public static void Notice(string message, [CallerMemberName]
                                string testName = null)
        {
            Log.Notice(testName, message);
        }
        public static void Warning(string message, [CallerMemberName]
                                   string testName = null)
        {
            Log.Warning(testName, string.Format("Warning: {0}", message));
        }

        public static void Failure(string message, [CallerMemberName]
                                   string testName = null)
        {
            Log.Error(testName, string.Format("Failure: {0}", message));
        }

        public static void Success(string message, [CallerMemberName]
                                   string testName = null) 
        {
            Log.Success(testName, string.Format("Success: {0}", message));
        }

        public static void Exception(Exception exception, [CallerMemberName]
                                     string testName = null)
        {
            Log.Error(testName, "Exception happened", exception);
        }

        public static void Verbose(string message, [CallerMemberName]
                                   string testName = null) 
        {
            Log.Verbose(testName, string.Format("Verbose: {0}", message));
        }
    }
    // This class will create one log file for each execution
}
