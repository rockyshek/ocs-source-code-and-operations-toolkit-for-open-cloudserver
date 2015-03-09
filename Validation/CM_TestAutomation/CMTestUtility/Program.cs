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

namespace Microsoft.GFS.WCS.Test
{
    using System;
    using System.Reflection;
    using Consoler;

    /// <summary> Program class having Main for CM Test Utility. </summary>
    public class Program
    {
        /// <summary> Main entry point for Console CM Test Utility. </summary>
        /// <param name="args"> Command line arguments. </param>
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine();
                Consolery.Run(
                    "Provides commands for Test Automation.",
                    Assembly.GetExecutingAssembly(),
                    args);
            }
            catch (CommandLineArgumentException ex)
            {
                Console.WriteLine(
                    string.Format("Argument '{0}' has invalid value specified: {1}", ex.ParamName, ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine("The command failed with the following exception: {0}", ex);
                object originalStackTrace = ex.Data["OriginalStackTrace"];
                if (originalStackTrace != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Original stack trace:");
                    Console.WriteLine(originalStackTrace);
                }
            }
        }
    }
}
