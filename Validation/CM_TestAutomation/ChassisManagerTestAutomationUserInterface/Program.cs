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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CMTestAutomationInterface.Controller;

namespace CMTestAutomationInterface
{
    internal class Program
    {
        private static readonly string appName = AppDomain.CurrentDomain.FriendlyName;

        private static void Main(string[] args)
        {
            StringBuilder helpfileOutput = new StringBuilder();

            try
            {
                Run(Environment.CommandLine);
            }
            catch (Exception e)
            { 
                ShowCommandUsage(string.Empty);

                
                helpfileOutput.AppendLine("================================");
                helpfileOutput.AppendFormat("Unexpected exception happened: {0}", e);
                helpfileOutput.AppendLine("================================");
                Console.WriteLine(helpfileOutput);
            }
        }

        private static void Run(string commandLine)
        {
            ChassisManagerUserOptions userOption = null;

            try
            {
                userOption = new ChassisManagerCommandInputReader<ChassisManagerUserOptions>().ReadInput(commandLine);
            }
            catch (InputReaderExceptionHandlerException e)
            {
                Console.WriteLine(e.Message);
                ShowUsage();
                return;
            }

            if (userOption.InputProcessed == 0)
            {
                ShowUsage();
                return;
            }

            if (userOption.CommandOption.Distinct().ToList().Count() >= 1 && userOption.Help)
            {
                // Display command help for individual command
                // Execute the User option
                if (userOption.CommandOption.Count() > 0)
                {
                    List<string> distinct = userOption.CommandOption.Distinct().ToList();

                    foreach (string value in distinct)
                    {
                        ShowCommandUsage(value);
                    }
                }
            }
            else if (userOption.Help)
            {
                ShowUsage();
            }
            else
            {
                // Loop through and execute the command                
                List<string> distinct = userOption.CommandOption.Distinct().ToList();

                foreach (string value in distinct)
                {
                    //verify user input
                    if (IsValidateParameter(value, userOption.CommandVaue))
                    {
                        CommandFetcher.ExecuteUserCommand(value, userOption.CommandVaue);
                    }
                    else
                    {
                        ShowCommandUsage(value);
                    }
                }
            }
        }

        /// <summary>
        /// Check each parameter 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="userInput"></param>
        /// <returns></returns>
        private static bool IsValidateParameter(string methodName, Dictionary<string, string> userInput)
        {
            bool validateParameter = false;           

            // get all public static methods of ChassisManagerLauncher type
            MethodInfo[] methodInfos = typeof(ChassisManagerLauncher).GetMethods(BindingFlags.Public |
                                                                                 BindingFlags.Static);
                        
            // sort methods by name
            methodInfos = Array.FindAll(methodInfos, i => i.Name == methodName);

            if (userInput.Count == 0)
            {
                // check if method with no parameter
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    if (methodInfo.GetParameters().Count() == 0)
                    {
                        validateParameter = true;
                    }
                }
            }
            else
            {
                foreach (var p in userInput)
                {
                    if (VerifyParameter(methodInfos, p))
                    {
                        validateParameter = true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return validateParameter;
        }

        private static bool VerifyParameter(MethodInfo[] methodInfos, KeyValuePair<string, string> p)
        {
            bool isTrue = false;

            // Loop eachmethod names
            foreach (MethodInfo methodInfo in methodInfos)
            {
                foreach (ParameterInfo param in methodInfo.GetParameters()) // name and parametertype
                {
                    if (  EqualsIgnoreCase( param.Name, p.Key))
                    {
                        isTrue = true;
                    }
                }
            }

            return isTrue;
        }

        private static bool EqualsIgnoreCase(string source, string target)
        {
            return string.Equals(source, target, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Display usage for each user options
        /// </summary>
        /// <param name="commandName"></param>
        private static void ShowCommandUsage(string commandName)
        {
            StringBuilder helpfileOutput = new StringBuilder();

            // get all public static methods of ChassisManagerLauncher type
            MethodInfo[] methodInfos = typeof(ChassisManagerLauncher).GetMethods(BindingFlags.Public |
                                                                                 BindingFlags.Static);
            if (commandName != string.Empty)
            {
                // sort methods by name
                methodInfos = Array.FindAll(methodInfos, i => i.Name == commandName);
            }
            else
            {
                // sort methods by name
                Array.Sort(methodInfos,
                    delegate(MethodInfo i, MethodInfo j) { return i.Name.CompareTo(j.Name); });
            }
          

            helpfileOutput.AppendLine("================================");
            helpfileOutput.AppendLine();                        

            // write method names
            foreach (MethodInfo methodInfo in methodInfos)
            {
                helpfileOutput.AppendFormat("{0} -{1} ", appName, methodInfo.Name);
                                                
                foreach (ParameterInfo param in methodInfo.GetParameters()) // name and parametertype
                {
                    helpfileOutput.AppendFormat(" -{0}:\"[Value]\" ", param.Name);
                }

                helpfileOutput.AppendLine();                
            }

            Console.WriteLine(helpfileOutput);
        }

        /// <summary>
        /// Display usage for 
        /// </summary>
        private static void ShowUsage()
        {
            StringBuilder helpfileOutput = new StringBuilder();
            helpfileOutput.AppendLine();

            helpfileOutput.AppendFormat("Name: {0} - Chassis Stress Test Utility.", appName);
            helpfileOutput.AppendLine();
            helpfileOutput.AppendFormat("Syntax: {0}  -Help or -h ", appName);
            helpfileOutput.AppendLine();
            helpfileOutput.AppendFormat("{0}  -[option]:\"Value\"", appName);
            helpfileOutput.AppendLine();
            helpfileOutput.AppendFormat("Options ");
            helpfileOutput.AppendLine();
            helpfileOutput.AppendFormat(string.Join(Environment.NewLine, (
                                                                from prop in typeof(ChassisManagerUserOptions).GetProperties()
                                                                let attr = prop.GetCustomAttributes(typeof(ChassisManagerOptionAttribute), true)
                                                                               .Cast<ChassisManagerOptionAttribute>()
                                                                               .FirstOrDefault()
                                                                where attr != null
                                                                select string.Format("-{0}       -{1}           {2}",
                                                                    attr.LongOptionName.PadRight(15), attr.ShortOptionName, attr.OptionDescription))));
            Console.WriteLine(helpfileOutput);
        }
    }
}
