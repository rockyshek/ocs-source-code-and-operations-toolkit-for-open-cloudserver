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

namespace Microsoft.GFS.WCS.Test.Consoler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Command line interface engine.
    /// </summary>
    public class Consolery
    {
        /// <summary>
        /// Invokes one of methods marked with <see cref="ActionAttribute"/> based
        /// on command line arguments.
        /// </summary>
        /// <param name="description">Tool description.</param>
        /// <param name="assembly">Assembly which contains commands implementation.
        /// Commands are marked with <see cref="ActionAttribute"/>.</param>
        /// <param name="args">Command line arguments as is.</param>
        public static void Run(string description, Assembly assembly, string[] args)
        {
            IList<MethodInfo> commands = EnumerateCommands(assembly);
            if (args.Length == 0)
            {
                ShowUsage(description, commands);
                return;
            }

            if (args[0] == "help")
            {
                if (args.Length != 2)
                {
                    ShowUsage(description, commands);
                }
                else
                {
                    ShowCommandUsage(description, commands, args[1]);
                }

                return;
            }

            MethodInfo method = GetMethodByCommandName(commands, args[0]);
            if (method == null)
            {
                Console.WriteLine("Unknown command '{0}'", args[0]);
                return;
            }

            try
            {
                Invoke(method, args.Skip(1).ToArray());
            }
            catch (ConsolerException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Enumerates methods which will be invoked from command line.
        /// </summary>
        /// <param name="assembly">Assembly to scan for methods.</param>
        /// <returns>List of <see cref="MethodInfo"/> which can be invoked from command line.</returns>
        private static IList<MethodInfo> EnumerateCommands(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(ConsoleSuiteAttribute), true).Length > 0)
                .SelectMany(type => type.GetMethods())
                .Where(method => method.GetCustomAttributes(typeof(ActionAttribute), true).Length > 0)
                .OrderBy(method => method.Name)
                .ToList();
        }

        /// <summary>
        /// Invokes specified method with specified arguments.
        /// </summary>
        /// <param name="method">Method to be invoked.</param>
        /// <param name="args">Command line arguments.</param>
        private static void Invoke(MethodInfo method, string[] args)
        {
            ParameterInfo[] parameterInfos = method.GetParameters();
            var paramValues = new object[parameterInfos.Length];
            var usedParams = new bool[args.Length];

            for (int i = 0; i < paramValues.Length; i++)
            {
                var required = GetAttribute<RequiredAttribute>(parameterInfos[i]);
                var optional = GetAttribute<OptionalAttribute>(parameterInfos[i]);

                if (required != null || optional == null)
                {
                    if (args.Length <= i)
                    {
                        string message = string.Format(
                            "Required argument '{0}' is missing.",
                            parameterInfos[i].Name);
                        throw new ConsolerException(message);
                    }

                    paramValues[i] = GetValue(parameterInfos[i], args[i], true);

                    usedParams[i] = true;
                }
                else
                {
                    paramValues[i] = GetOptionalValue(parameterInfos[i], args, usedParams, optional);
                }
            }

            if (usedParams.Where(prm => !prm).Any())
            {
                string unusedParams = string.Join(", ", args.Where((a, index) => !usedParams[index]).ToArray());
                string message = string.Format("More arguments passed than expected ({0}).", unusedParams);
                throw new ConsolerException(message);
            }

            try
            {
                method.Invoke(null, paramValues);
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.Data["OriginalStackTrace"] = ex.InnerException.StackTrace;
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets optional value of a method parameter.
        /// </summary>
        /// <param name="paramInfo">Optional parameter which value to find.</param>
        /// <param name="args">Command line arguments.</param>
        /// <param name="usedArgs">Set of parameters used in method call. Updated by this method.</param>
        /// <param name="optional"><see cref="OptionalAttribute"/> for the parameter.</param>
        /// <returns>Optional parameter value.</returns>
        private static object GetOptionalValue(
            ParameterInfo paramInfo,
            string[] args,
            bool[] usedArgs,
            OptionalAttribute optional)
        {
            string valuePrefix = string.Format("/{0}:", paramInfo.Name);

            var unusedIndices = new List<int>();
            for (int i = 0; i < usedArgs.Length; i++)
            {
                if (!usedArgs[i] && args[i].StartsWith(valuePrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    unusedIndices.Add(i);
                }
            }

            if (unusedIndices.Count > 1)
            {
                string msg = string.Format("Two values for argument '{0}' present. Please resolve.", paramInfo.Name);
                throw new ConsolerException(msg);
            }

            if (unusedIndices.Count == 1)
            {
                usedArgs[unusedIndices.First()] = true;
                return GetValue(paramInfo, args[unusedIndices.First()].Substring(valuePrefix.Length), false);
            }

            if (optional.DefaultValue != null &&
                !paramInfo.ParameterType.IsAssignableFrom(optional.DefaultValue.GetType()))
            {
                string message = string.Format(
                    "Default value '{0}' cannot be assigned to parameter '{1}'",
                    optional.DefaultValue,
                    paramInfo.Name);

                throw new ConsolerException(message);
            }

            if (optional.DefaultValue == null && paramInfo.ParameterType.IsValueType)
            {
                string message = string.Format(
                    "Default value 'null' cannot be assigned to parameter '{0}'",
                    paramInfo.Name);

                throw new ConsolerException(message);
            }

            return optional.DefaultValue;
        }

        /// <summary>
        /// Converts string value to the one can be passed as a parameter to method.
        /// </summary>
        /// <param name="param">Parameter which will accept the value.</param>
        /// <param name="strValue">String parameter value.</param>
        /// <param name="isRequired">Whether the parameter is required.</param>
        /// <returns>Value that can be passed as parameter value.</returns>
        private static object GetValue(ParameterInfo param, string strValue, bool isRequired)
        {
            try
            {
                if (isRequired && strValue.StartsWith("/") && strValue.Contains(":"))
                {
                    string format = "Required parameter '{0}' has value '{1}' which looks like an optional parameter." +
                                    " Have you mixed something?";

                    Console.WriteLine(string.Format(format, param.Name, strValue));
                }

                return Convert.ChangeType(strValue, param.ParameterType);
            }
            catch (InvalidCastException)
            {
                string message = string.Format(
                    "Value '{0}' cannot be converted to type '{1}' of parameter '{2}'",
                    strValue,
                    param.ParameterType.Name,
                    param.Name);

                throw new ConsolerException(message);
            }
        }

        /// <summary>
        /// Retrieves method from console suite by command name.
        /// </summary>
        /// <param name="commands">List of command line methods.</param>
        /// <param name="name">Method name.</param>
        /// <returns>Method to be invoked, if any.</returns>
        private static MethodInfo GetMethodByCommandName(IList<MethodInfo> commands, string name)
        {
            return commands
                .Where(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
        }

        /// <summary>
        /// Shows usage for specified command.
        /// </summary>
        /// <param name="description">Tool description.</param>
        /// <param name="commands">List of command line commands.</param>
        /// <param name="name">Command name.</param>
        private static void ShowCommandUsage(string description, IList<MethodInfo> commands, string name)
        {
            MethodInfo method = GetMethodByCommandName(commands, name);
            if (method == null)
            {
                ShowUsage(description, commands);
                return;
            }

            ActionAttribute attrib = method
                .GetCustomAttributes(typeof(ActionAttribute), false)
                .Cast<ActionAttribute>()
                .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine("\t" + attrib.Description);
            Console.WriteLine();

            ParameterInfo[] parameterInfos = method.GetParameters();
            var paramNames = new List<string>();
            var descriptions = new List<string>();
            foreach (ParameterInfo param in parameterInfos)
            {
                var required = GetAttribute<RequiredAttribute>(param);
                var optional = GetAttribute<OptionalAttribute>(param);

                string paramName;
                string actionDescription;
                if (required != null || optional == null)
                {
                    paramName = param.Name;
                    actionDescription = required != null ? required.Description : string.Empty;
                }
                else
                {
                    paramName = string.Format("[/{0}:<value>]", param.Name);
                    actionDescription = optional.Description;
                }

                paramNames.Add(string.Format("{0}", paramName));
                descriptions.Add(actionDescription);
            }

            Console.WriteLine(
                "\tUsage: {0} {1} {2}",
                Process.GetCurrentProcess().ProcessName,
                method.Name,
                string.Join(" ", paramNames.ToArray()));

            Console.WriteLine();

            int maxLength = paramNames.Count > 0 ? paramNames.Max(pn => pn.Length) : 0;
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                Console.WriteLine(string.Format("\t\t{0}\t{1}", paramNames[i].PadRight(maxLength), descriptions[i]));
            }
        }

        /// <summary>
        /// Shows usage.
        /// </summary>
        /// <param name="description">Description of the tool.</param>
        /// <param name="commands">List with command line methods.</param>
        private static void ShowUsage(string description, IList<MethodInfo> commands)
        {
            Console.WriteLine("\t" + description);

            Console.WriteLine();
            Console.WriteLine("\tAvailable commands:");
            Console.WriteLine();

            if (commands.Count == 0)
            {
                return;
            }

            int maxLength = commands.Max(m => m.Name.Length);
            foreach (MethodInfo actionMethod in commands)
            {
                string name = actionMethod.Name.PadRight(maxLength);

                string actionDescription = GetActionAttribute(actionMethod).Description;

                Console.WriteLine("\t\t{0}\t{1}", name, actionDescription);
            }

            Console.WriteLine();

            Console.WriteLine(
                "\tType '{0} help <command>' to get help for an individual command.",
                Process.GetCurrentProcess().ProcessName);
        }

        /// <summary>
        /// Retrieves <see cref="ActionAttribute"/> for a method. It must be present.
        /// </summary>
        /// <param name="actionMethod">Method for which to retrieve attribute.</param>
        /// <returns>Instance of <see cref="ActionAttribute"/>.</returns>
        private static ActionAttribute GetActionAttribute(MethodInfo actionMethod)
        {
            return actionMethod
                .GetCustomAttributes(typeof(ActionAttribute), false)
                .Cast<ActionAttribute>()
                .First();
        }

        /// <summary>
        /// Retrieves attribute of specified type for method parameter.
        /// </summary>
        /// <typeparam name="T">Type of attribute to retrieve.</typeparam>
        /// <param name="parameter">Parameter for which to retrieve attribute.</param>
        /// <returns>Attribute instance, if any.</returns>
        private static T GetAttribute<T>(ParameterInfo parameter) where T : Attribute
        {
            return parameter.GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault();
        }
    }
}
