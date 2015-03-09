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

namespace Microsoft.GFS.WCS.Test.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Stores parameters to be used for CM REST APIs.
    /// </summary>
    [DataContract(Namespace = "http://schemas.microsoft.com/Microsoft/GFS/WCS/Test/TestDef")]
    public class Parameters : ICloneable
    {
        /// <summary> Dictionary of parameters. </summary>
        [DataMember]
        public readonly Dictionary<string, List<string>> parameters;

        /// <summary> Empty list to be used when no value is found. </summary>
        private static readonly List<string> emptyList = new List<string>();

        /// <summary> Dictionary of # of times a parameter has been served. </summary>
        private readonly Dictionary<string, int> served;

        /// <summary>Initializes a new instance of the Parameters class.</summary>
        /// <param name="mergeOn"> Determines behavior of indexer assignments. </param>
        public Parameters(bool mergeOn = false)
        {
            this.parameters = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);
            this.served = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>Initializes a new instance of the Parameters class.</summary>
        /// <param name="parameters">Dictionary of parameters.</param>
        public Parameters(Dictionary<string, List<string>> parameters)
        {
            this.parameters = parameters;
            this.served = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary> Gets Names of parameters. </summary>
        public List<string> Names
        {
            get
            {
                return this.parameters.Keys.ToList();
            }
        }

        /// <summary> Indexer to all values pertaining to parameter Name. </summary>
        /// <param name="paramName"> Name of parameter to return values for. </param>
        /// <param name="mergeOn"> Whether to merge a value or overlay it. </param>
        /// <returns> An enumeration of value strings in format 'key=value'. </returns>
        public List<string> this[string paramName, bool mergeOn = false]
        {
            get
            {
                return this.parameters.ContainsKey(paramName)
                       ? this.parameters[paramName]
                       : Parameters.emptyList;
            }

            set
            {
                if (this.parameters.ContainsKey(paramName) && mergeOn)
                {
                    this.parameters[paramName] = this.parameters[paramName].Union(value).Distinct().ToList();
                }
                else
                {
                    this.parameters[paramName] = value;
                }
            }
        }

        /// <summary> Indexer to next value pertaining to parameter Name. </summary>
        /// <param name="paramName">Name of parameter to return values for.</param>
        /// <param name="index">
        ///  A valid range between 0 to count of values or any negative value for next available value.
        /// </param>
        /// <returns> A string value.</returns>
        public string this[string paramName, int index]
        {
            get
            {
                if (!this.parameters.ContainsKey(paramName))
                {
                    throw new KeyNotFoundException(paramName);
                }

                lock (this.parameters[paramName])
                {
                    var count = this.parameters[paramName].Count;
                    if (count == 0 || index >= count)
                    {
                        return null;
                    }

                    if (index < 0)
                    {
                        lock (this.served)
                        {
                            this.served.TryGetValue(paramName, out index);
                            this.served[paramName] = index + 1;
                        }
                    }

                    return this.parameters[paramName][index % count];
                }
            }
        }

        /// <summary> Gets global parameters through discovery, well known or preconfigured values. </summary>
        /// <returns>A Parameters object having all values to be used for testing. </returns>
        public static Parameters GetSampleParameters()
        { 
            var sampleParameters = new Parameters();
            sampleParameters["portNo"] = new List<string>() { "1", "2", "3" };
            sampleParameters["bladeId"] = new List<string>()
            {
                "1", "2", "3", "4", "5", "6", "7", "8", "9",
                "10", "11", "13", "14", "15", "16", "17", "18",
                "19", "20", "21", "22", "23", "24"
            };
            sampleParameters["powerLimitInWatts"] = new List<string>() { "200", "500", "900" };
            sampleParameters["offTime"] = new List<string>() { "10", "15", "61" };
            sampleParameters["bladeInfo"] = new List<string>() { "true", "false" };
            sampleParameters["startTimestamp"] = new List<string>() { System.DateTime.Now.AddHours(-2).ToString() };
            sampleParameters["endTimestamp"] = new List<string>() { System.DateTime.MaxValue.ToString() };
            sampleParameters["userName"] = new List<string>() { "testUser" };
            sampleParameters["role"] = new List<string>() { "wcsCMAdmin", "wcsCMOperator", "wcsCMUser" };
            sampleParameters["newPassword"] = new List<string>() { "NewP7890wd^" };
            sampleParameters["passwordString"] = new List<string>() { "pa$$1234Pwd!" };
            sampleParameters["psuInfo"] = new List<string>() { "true", "false" };
            sampleParameters["bladeHealth"] = new List<string>() { "true", "false" };
            sampleParameters["psuHealth"] = new List<string>() { "true", "false" };
            sampleParameters["fanHealth"] = new List<string>() { "true", "false" }; 
            sampleParameters["batteryHealth"] = new List<string>() { "true", "false" };
            sampleParameters["batteryInfo"] = new List<string>() { "true", "false" };
            sampleParameters["cpuInfo"] = new List<string>() { "true", "false" };
            sampleParameters["memInfo"] = new List<string>() { "true", "false" };
            sampleParameters["diskInfo"] = new List<string>() { "true", "false" };
            sampleParameters["pcieInfo"] = new List<string>() { "true", "false" };
            sampleParameters["sensorInfo"] = new List<string>() { "true", "false" };
            sampleParameters["temp"] = new List<string>() { "true", "false" };
            sampleParameters["fruInfo"] = new List<string>() { "true", "false" };
            sampleParameters["chassisControllerInfo"] = new List<string>() { "true", "false" };
            sampleParameters["bootType"] = new List<string>() { "NoOverride", "ForcePxe", "ForceDefaultHdd", "ForceIntoBiosSetup", "ForceFloppyOrRemovable" };
            sampleParameters["uefi"] = new List<string>() { "true", "false" };
            sampleParameters["persistent"] = new List<string>() { "true", "false" };
            sampleParameters["bootInstance"] = new List<string>() { "0" };
            sampleParameters["sessionTimeoutInSecs"] = new List<string> { "30", "120", "300" };
            sampleParameters["powerOnWait"] = new List<string> { "true", "false" };
            sampleParameters["sessionToken"] = new List<string> { "1234567891234567" };
            sampleParameters["forceKill"] = new List<string> { "true" };
            return sampleParameters;
        }

        /// <summary>
        /// Shuffles all parameter values.
        /// </summary>
        public void Shuffle()
        {
            foreach (var paramName in this.parameters.Keys)
            {
                this.Shuffle(paramName);
            }
        }

        /// <summary>
        /// Shuffles values of a given parameter.
        /// </summary>
        /// <param name="paramName"> Name of parameter to shuffle values of. </param>
        public void Shuffle(string paramName)
        {
            if (!this.parameters.ContainsKey(paramName))
            {
                throw new KeyNotFoundException(paramName);
            }
            else
            {
                this.parameters[paramName].Shuffle();
            }
        }

        /// <summary> Clones the Parameters values. </summary>
        /// <returns>An object of type Parameters</returns>
        public object Clone()
        {
            var clone = new Parameters();
            if (this.parameters.Any())
            {
                this.parameters.ToList().ForEach(p => clone[p.Key] = new List<string>(p.Value));
            }

            return clone;
        }
    }
}
