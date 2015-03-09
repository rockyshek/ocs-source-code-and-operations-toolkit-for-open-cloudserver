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

namespace ChassisValidationUtility
{
    /// <summary>
    /// The attribute to describe an option in a command line.
    /// </summary> 
    [AttributeUsage(AttributeTargets.Property)]
    public class CmdOptionAttribute : Attribute
    {
        public char ShortName { get; set; }
        public string LongName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Base class for all option classes used by CmdParser.
    /// </summary>
    public abstract class CmdOptionBase
    {
        /// <summary>
        /// The number of arguments parsed.
        /// </summary>
        public int Parsed { get; internal set; }

        /// <summary>
        /// The raw key-value pair parsed from the command line
        /// </summary>
        public KeyValuePair<String, String>[] RawArgs { get; internal set; }
    }

    /// <summary>
    /// Parses the command line string into an object.
    /// </summary>
    /// <typeparam name="TOption">
    /// An option type in which defines properties for 
    /// all command line options.
    /// </typeparam>
    public class CmdLineParser<TOption> where TOption : CmdOptionBase, new()
    {
        private readonly List<KeyValuePair<CmdOptionAttribute,PropertyInfo>> options;
        private readonly TOption optionObj = new TOption();

        public CmdLineParser()
        {
            // retrieve all properties attributed with CmdOptionAttribute
            // and put them as key-value pairs into a list
            options = (
                from prop in typeof (TOption).GetProperties()
                let attr = prop
                    .GetCustomAttributes(typeof (CmdOptionAttribute), true)
                    .Cast<CmdOptionAttribute>().FirstOrDefault()
                where attr != null
                select new KeyValuePair<CmdOptionAttribute, PropertyInfo>(attr, prop)
                ).ToList();
            // if no property is attributed with CmdOptionAttribute, throw exception
            if (options.Count == 0)
                throw new CommandLineParsingException(typeof (TOption));
        }

        private static readonly char[] optionIndicators = {'-', '/'};
        private static readonly char[] valueSeparators = {',', ';'};

        /// <summary>
        /// Parses the command line string into an object.
        /// </summary>
        /// <param name="commandLine">
        /// The original command line string.
        /// </param>
        /// <exception cref="CommandLineParsingException">
        /// The exception will be thrown if anything goes wrong 
        /// in the parsing process.
        /// </exception>
        public TOption Parse(string commandLine)
        {
            var argIndex = commandLine.IndexOfAny(optionIndicators);
            // no arguments passed
            if (argIndex == -1) 
            {
                optionObj.Parsed = 0;
                return optionObj;
            }
            // extract arguments
            var arguments = ParseArgs(commandLine.Substring(argIndex));
            optionObj.RawArgs = arguments.ToArray();

            foreach (var arg in arguments)
            {
                // find the correct property to set
                var propInfo = (
                    from option in this.options
                    let names = new[] {option.Key.LongName, option.Key.ShortName.ToString()}
                    where names.Contains(arg.Key, StringComparer.InvariantCultureIgnoreCase)
                    select option.Value
                 ).SingleOrDefault();

                // if not found, just continue
                if (propInfo == null) continue;

                try 
                { // try to set the property value
                    if (propInfo.PropertyType.IsArray) 
                    {
                        // if the property is an array, split the argument value into an array
                        // and cast to the target type
                        var type = propInfo.PropertyType.GetElementType();
                        var values = arg.Value.Split(valueSeparators, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(value => value.Trim()).ToArray();
                        var arrayInstance = Array.CreateInstance(type, values.Length);
                        for (var index = 0; index < values.Length; index++)
                            arrayInstance.SetValue(Convert.ChangeType(values[index], type), index);
                        propInfo.SetValue(optionObj, arrayInstance);
                    } 
                    else 
                    {
                        // if no value is provided, regard it as true bool value
                        if (string.IsNullOrWhiteSpace(arg.Value) && propInfo.PropertyType == typeof (bool))
                            propInfo.SetValue(optionObj, true);
                        // cast to the target type
                        else propInfo.SetValue(optionObj, Convert.ChangeType(arg.Value.Trim(), propInfo.PropertyType));
                    }
                } 
                catch (Exception e) 
                {
                    throw new CommandLineParsingException(string.Format("{0} must be Type {1}.", propInfo.Name,
                        propInfo.PropertyType.IsArray ? propInfo.PropertyType.GetElementType() : propInfo.PropertyType));
                }
                optionObj.Parsed++;
            }

            return optionObj;
        }

        #region Regular Expressions
        // each argument must start with - or /
        private const string indicatorRegex          = @"(?<=[-\/])"; 
        // each argument name must be a word (a-zA-Z0-9_)
        private const string nameRegex               = @"(\w*)"; 
        // argument name and value can be sepatated by : or = or spaces; 
        // any leading or ending white spaces will be ignored
        private const string nameValueDelimiterRegex = @"((?:\s*)[:\=\s](?:\s*))"; 
        // argument value must be a word (a-zA-Z0-9_) or must be quoted by "
        // if other charaters need to be included
        private const string singleValueRegex        = @"((""(.[^""]*)"")|(\w[\w\-.]*))";
        // if there are multiple values, they must be separated by , or ;
        // any leading or ending white spaces will be ignored
        private const string valueSeparatorRegex     = @"((?:\s*)[,;](?:\s*))";
        private static readonly string multipleValueRegex = string.Format("{0}({1}{0})*",
            singleValueRegex, valueSeparatorRegex);
        // combine them all. this is the complete regex for a command line argument
        private static readonly string cmdOptionRegex = string.Format("{0}(?<name>{1})({2}(?<value>{3}))?",
            indicatorRegex, nameRegex, nameValueDelimiterRegex, multipleValueRegex);
        #endregion

        /// <summary>
        /// Parses input string into argument key-value pairs
        /// </summary>
        /// <example>
        /// Input string: -input file.txt  will be parsed into
        /// Key: input  Value: file.txt
        /// </example>
        private List<KeyValuePair<String, String>> ParseArgs(string argumentString)
        {
            var parseResult = new List<KeyValuePair<String, String>>();
            try 
            {
                // use regular expression to extrat arguments from the input string
                parseResult.AddRange(
                    from Match match in Regex.Matches(argumentString, cmdOptionRegex)
                    select new KeyValuePair<string, string>(match.Groups["name"].Value,
                        match.Groups["value"].Value.Trim(new[] {'\"'})) //remove quotation
                    );
                return parseResult;
            } 
            catch (Exception e)
            {
                throw new CommandLineParsingException("Error in parsing arguments.", e);
            }
        }  
    }

    /// <summary>
    /// The exception will be thrown when any error happened during the process
    /// of parsing an input command line.
    /// </summary>
    public class CommandLineParsingException : Exception
    {
        public CommandLineParsingException(string message) : base(message) { }

        public CommandLineParsingException(Type optionType) : base(optionType.Name
            + " is not valid for command line option parsing.") { }

        public CommandLineParsingException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
