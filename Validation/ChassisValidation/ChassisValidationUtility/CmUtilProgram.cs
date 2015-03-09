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
using System.Linq;

namespace ChassisValidationUtility
{
    class CmUtilOption : CmdOptionBase
    {
        [CmdOption(LongName = "RunAll", ShortName = 'a',
        Description = "Run all test cases.")]
        public bool RunAll { get; set; }

        [CmdOption(LongName = "RunTests", ShortName = 't',
        Description = "Run single or multiple test cases.")]
        public string[] RunTests { get; set; }

        [CmdOption(LongName = "RunBatch", ShortName = 'b',
        Description = "Run test cases in a batch file.")]
        public string RunBatch { get; set; }

        [CmdOption(LongName = "Help", ShortName = 'h',
        Description = "Show command help.")]
        public bool Help { get; set; }
    }

    class CmUtilProgram
    {
        private static readonly string appName = AppDomain.CurrentDomain.FriendlyName;

        static void Main()
        {
            try
            {
                Run(Environment.CommandLine);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception happened: {0}", e);
            }
            // Console.ReadKey();
        }

        static void Run(string commandLine)
        {
            CmUtilOption option = null;

            try
            {
                option = new CmdLineParser<CmUtilOption>().Parse(commandLine);
            }
            catch (CommandLineParsingException e)
            {
                Console.WriteLine(e.Message);
                ShowUsage();
                return;
            }

            if (option.Parsed == 0)
            {
                ShowUsage();
                return;
            }

            if (option.Parsed != 1)
            {
                Console.WriteLine("Only one option is allowed.");
                ShowUsage();
                return;
            }

            if (option.Help) ShowUsage();
            else if (option.RunAll) CmTestRunner.RunAllTestCases();
            else if (option.RunBatch != null) CmTestRunner.RunFromBatch(option.RunBatch);
            else if (option.RunTests != null) CmTestRunner.RunTestCases(option.RunTests);
        }

        static void ShowUsage()
        {
            Console.WriteLine("\n\rNAME\n\r{0} - Chassis validation utility.", appName);
            Console.WriteLine("\n\rSYNTAX\n\r{0} [-RunAll] [-RunTests] [-RunBatch] [-Help]", appName);
            Console.WriteLine("\n\rOPTIONS");
            Console.WriteLine(string.Join(Environment.NewLine, (
                from prop in typeof(CmUtilOption).GetProperties()
                let attr = prop.GetCustomAttributes(typeof(CmdOptionAttribute), true)
                               .Cast<CmdOptionAttribute>().FirstOrDefault()
                where attr != null
                select string.Format("-{0} -{1}  {2}",
                    attr.LongName.PadRight(10), attr.ShortName, attr.Description)
                )));
            Console.WriteLine("\n\rREMARKS\n\rAll available test cases: ");
            Console.WriteLine(string.Join(Environment.NewLine, CmTestRunner.GetAllTestCases()));
        }
    }
}
