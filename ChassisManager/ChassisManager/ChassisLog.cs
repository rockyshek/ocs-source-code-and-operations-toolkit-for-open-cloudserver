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

namespace Microsoft.WCS.ChassisManager
{
    /// <summary>
    /// ChassisLog.userLog(myString): User events logging - string input 
    /// ChassisLog.traceLog(myString): Full debug tracing including method-name/file-name/line-number - string input
    /// </summary>
    static internal class ChassisLog
    {
        // TODO: Move this to constants class
        const string traceLogFilePath = @"C:\ChassisManagerTraceLog.txt";
        const string userLogFilePath = @"C:\ChassisManagerUserLog.txt";

        private static System.Diagnostics.TextWriterTraceListener ChassisManagerTraceLog;
        private static System.Diagnostics.TextWriterTraceListener ChassisManagerUserLog;
        private static System.IO.FileStream cmLogTraceFile;
        private static System.IO.FileStream cmLogUserFile;

        // static constructor 
        static ChassisLog()
        {
            // Creates the text file that the trace listener will write to
            // Creates the new trace listener.
            try
            {
# if TRACE_LOG
                cmLogTraceFile = new System.IO.FileStream(traceLogFilePath, System.IO.FileMode.Append);
                ChassisManagerTraceLog = new System.Diagnostics.TextWriterTraceListener(cmLogTraceFile);
# endif
            }
            catch (System.Security.SecurityException e)
            {
                System.Console.WriteLine("Trace Logging cannot be done. Security Exception " + e);
            }
            catch (System.IO.IOException e)
            {
                System.Console.WriteLine("Trace Logging cannot be done. IO Exception " + e);
            }
            catch (System.ArgumentNullException e)
            {
                System.Console.WriteLine("Trace Logging cannot be done. Exception " + e);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Trace Logging cannot be done. Exception " + e);
            }

            try
            {
# if USER_LOG
                cmLogUserFile = new System.IO.FileStream(userLogFilePath, System.IO.FileMode.Append);
                ChassisManagerUserLog = new System.Diagnostics.TextWriterTraceListener(cmLogUserFile);
# endif
            }
            catch (System.Security.SecurityException e)
            {
                System.Console.WriteLine("User Logging cannot be done. Security Exception " + e);
            }
            catch (System.IO.IOException e)
            {
                System.Console.WriteLine("User Logging cannot be done. IO Exception " + e);
            }
            catch (System.ArgumentNullException e)
            {
                System.Console.WriteLine("User Logging cannot be done. Exception " + e);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("User Logging cannot be done. Exception " + e);
            }
        }

        static internal void traceLog(string logString)
        {
            try
            {
# if TRACE_LOG
                // Logs a trace of called method(s) name, filename(s), and, line number(s) information.
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
                string stackIndent = "";
                for (int i = 1; i < st.FrameCount; i++)
                {
                    System.Diagnostics.StackFrame sf = st.GetFrame(i);
                    ChassisManagerTraceLog.WriteLine("");
                    ChassisManagerTraceLog.WriteLine(stackIndent + " Method: " + sf.GetMethod().ToString());
                    ChassisManagerTraceLog.WriteLine(stackIndent + " File: " + sf.GetFileName());
                    ChassisManagerTraceLog.WriteLine(stackIndent + " Line Number: " + sf.GetFileLineNumber().ToString());
                    stackIndent += "  ";
                }
                ChassisManagerTraceLog.WriteLine("");
                // Print the passed input string
                ChassisManagerTraceLog.WriteLine(System.DateTime.Now.ToString() + " " + logString);
                ChassisManagerTraceLog.Flush();
# endif
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Trace Logging cannot be done. Exception " + e);
            }
        }

        static internal void userLog(string logString)
        {
            try
            {
            # if USER_LOG
                            ChassisManagerUserLog.WriteLine(System.DateTime.Now.ToString() + " " + logString);
                            ChassisManagerUserLog.Flush();
            # endif
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("User Logging cannot be done. Exception " + e);
            }
        }
    }
}
