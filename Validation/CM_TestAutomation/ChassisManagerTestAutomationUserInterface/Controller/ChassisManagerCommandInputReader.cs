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
using System.Text.RegularExpressions;

namespace CMTestAutomationInterface.Controller
{
    /// <summary>
    /// Parses the command line string into an object.
    /// </summary>
    /// <typeparam name="TOption">
    /// An option type in which defines properties for 
    /// all command line options.
    /// </typeparam>
    public class ChassisManagerCommandInputReader<TOption> where TOption : ChassisManagerOptionBase, new()
    {
        private static readonly char[] optionIndicators = { '-' };

        private static readonly string multipleValueRegex = string.Format("{0}({1}{0})*",
            SingleValueRegex, ValueSeparatorRegex);
        // combine them all. this is the complete regex for a command line argument
        private static readonly string cmdOptionRegex = string.Format("{0}(?<name>{1})({2}(?<value>{3}))?",
            IndicatorRegex, NameRegex, NameValueDelimiterRegex, multipleValueRegex);

        // each argument must start with - or /
        private const string IndicatorRegex = @"(?<=[-\/])";
        // each argument name must be a word (a-zA-Z0-9_)
        private const string NameRegex = @"(\w*)";
        // argument name and value can be sepatated by : or = or spaces; 
        // any leading or ending white spaces will be ignored
        private const string NameValueDelimiterRegex = @"((?:\s*)[:\=\s](?:\s*))";
        // argument value must be a word (a-zA-Z0-9_) or must be quoted by "
        // if other charaters need to be included
        private const string SingleValueRegex = @"((""(.[^""]*)"")|(\w[\w\-.]*))";
        // if there are multiple values, they must be separated by , or ;
        // any leading or ending white spaces will be ignored
        private const string ValueSeparatorRegex = @"((?:\s*)[,;](?:\s*))";
        private readonly List<KeyValuePair<ChassisManagerOptionAttribute, PropertyInfo>> options;
        private readonly TOption optionObj = new TOption();
        
        public ChassisManagerCommandInputReader()
        {
            // retrieve all properties attributed with CmdOptionAttribute
            // and put them as key-value pairs into a list
            this.options = (
                            from prop in typeof(TOption).GetProperties()
                            let attr = prop
                                           .GetCustomAttributes(typeof(ChassisManagerOptionAttribute), true)
                                           .Cast<ChassisManagerOptionAttribute>()
                                           .FirstOrDefault()
                            where attr != null
                            select new KeyValuePair<ChassisManagerOptionAttribute, PropertyInfo>(attr, prop)).ToList();
            // if no property is attributed with CmdOptionAttribute, throw exception
            if (this.options.Count == 0)
            {
                throw new InputReaderExceptionHandlerException(typeof(TOption));
            }
        }

        /// <summary>
        /// Parses the command line string into an object.
        /// </summary>
        /// <param name="userInputParameters">
        /// The original command line string.
        /// </param>
        /// <exception cref="CommandLineParsingException">
        /// The exception will be thrown if anything goes wrong 
        /// in the parsing process.
        /// </exception>
        public TOption ReadInput(string userInputParameters)
        {
            var argIndex = userInputParameters.IndexOfAny(optionIndicators);
            // no arguments passed
            if (argIndex == -1)
            {
                this.optionObj.InputProcessed = 0;
                return this.optionObj;
            }
            // extract arguments
            var arguments = this.ReadInputParameters(userInputParameters.Substring(argIndex));
            this.optionObj.UserInputsArgs = arguments.ToArray();
            this.optionObj.CommandOption = new List<string>();
            this.optionObj.CommandVaue = new Dictionary<string, string>();

            foreach (var arg in arguments)
            {
                // find the correct property to set
                var propInfo = (
                                from option in this.options
                                let names = new[] { option.Key.LongOptionName, option.Key.ShortOptionName.ToString() }
                                where names.Contains(arg.Key, StringComparer.InvariantCultureIgnoreCase)
                                select option.Value).SingleOrDefault();

                // if not found, just continue
                if (propInfo == null)
                {
                    if (arg.Key != string.Empty)
                    {
                        this.optionObj.CommandVaue.Add(arg.Key, arg.Value);
                    }

                    continue;
                }

                try
                {
                    if (arg.Key != "help" && ((System.Reflection.MemberInfo)(propInfo)).Name.ToLower() != "help")
                    {
                        this.optionObj.CommandOption.Add(((System.Reflection.MemberInfo)(propInfo)).Name);
                    }

                    // if no value is provided, regard it as true bool value
                    if (string.IsNullOrWhiteSpace(arg.Value) && propInfo.PropertyType == typeof(bool))
                    {
                        propInfo.SetValue(this.optionObj, true);
                    }
                    // cast to the target type
                    else
                    {
                        propInfo.SetValue(this.optionObj, Convert.ChangeType(arg.Value.Trim(), propInfo.PropertyType));
                    }
                }
                catch (Exception)
                {
                    throw new InputReaderExceptionHandlerException(string.Format("{0} must be Type {1}.", propInfo.Name,
                        propInfo.PropertyType.IsArray ? propInfo.PropertyType.GetElementType() : propInfo.PropertyType));
                }
              
                this.optionObj.InputProcessed++;
            }

            return this.optionObj;
        }

        /// <summary>
        /// Parses input string into argument key-value pairs
        /// </summary>
        /// <example>
        /// Input string: -input file.txt  will be parsed into
        /// Key: input  Value: file.txt
        /// </example>
        private List<KeyValuePair<String, String>> ReadInputParameters(string argumentString)
        {
            var readInputResult = new List<KeyValuePair<String, String>>();
            try
            {
                //use regular expression to extrat arguments from the input string
                readInputResult.AddRange(
                                         from Match match in Regex.Matches(argumentString, cmdOptionRegex)
                                         select new KeyValuePair<string, string>(match.Groups["name"].Value,
                                             match.Groups["value"].Value.Trim(new[] { '\"' })));
                //remove quotation

                return readInputResult;
            }
            catch (Exception e)
            {
                throw new InputReaderExceptionHandlerException("Error in parsing arguments.", e);
            }
        }
    }
}
