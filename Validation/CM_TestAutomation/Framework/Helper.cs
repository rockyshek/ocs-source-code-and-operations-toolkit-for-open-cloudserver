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
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.GFS.WCS.Contracts;

    /// <summary>
    /// This static class has helper methods used by various parts for Test Framework.
    /// </summary>
    public static class Helper
    {
        /// <summary> List of APIs and respective parameters. </summary>
        private static readonly Dictionary<string, Tuple<Type, List<string>>> Apis =
            new Dictionary<string, Tuple<Type, List<string>>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary> Gets APIs and its parameters. </summary>
        /// <returns> List of APIs </returns>
        public static List<string> GetChassisManagerApiList()
        {
            if (!Apis.Any())
            {
                GetApiListAndParameters(typeof(IChassisManager));
            }

            return Apis.Keys.ToList();
        }

        /// <summary> Gets APIs return Type. </summary>
        /// <param name="api"> Name of API for which to get parameters of. </param>
        /// <returns> Type of return value of given API. </returns>
        public static Type GetChassisManagerApiReturnType(string api)
        {
            if (GetChassisManagerApiList().Contains(api, StringComparer.InvariantCultureIgnoreCase))
            {
                return Apis[api].Item1;
            }

            throw new ArgumentException(string.Format("api '{0}' Not found", api));
        }

        /// <summary> Gets APIs and its parameters. </summary>
        /// <param name="api"> Name of API for which to get parameters of. </param>
        /// <returns> List of parameters for given API. </returns>
        public static List<string> GetChassisManagerApiParameterList(string api)
        {
            if (GetChassisManagerApiList().Contains(api, StringComparer.InvariantCultureIgnoreCase))
            {
                return Apis[api].Item2;
            }

            throw new ArgumentException(string.Format("api '{0}' Not found", api));
        }

        /// <summary> Dumps all APIs and respective parameters of IChassisManager interface. </summary>
        /// <returns> A string having all API and parameters listed. </returns>
        public static string DumpAllChassisManagerApi()
        {
            var dump = new StringBuilder();
            GetChassisManagerApiList().ForEach(api => dump.Append(DumpChassisManagerApi(api)));
            return dump.ToString();
        }

        /// <summary> Dumps APIs and respective parameters of IChassisManager interface. </summary>
        /// <param name="api"> Name of api to get info of. </param>
        /// <returns> A string having all API and parameters listed. </returns>
        public static string DumpChassisManagerApi(string api)
        {
            return string.Format(
                "{0} {1}({2})\n",
                Helper.GetChassisManagerApiReturnType(api),
                api,
                string.Join(",", Helper.GetChassisManagerApiParameterList(api)));
        }

        /// <summary> Saves TestBatch object to a file. </summary>
        /// <param name="theObj"> The object instance to be serialized into fileName. </param>
        /// <param name="fileName"> Name of file to save TestBatch to. </param>
        public static void SaveToFile(object theObj, string fileName)
        {
            try
            {
                Console.WriteLine("Details of [{0}] being saved to {1}", theObj, fileName);
                var serializer = new DataContractSerializer(theObj.GetType());
                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    serializer.WriteObject(stream, theObj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to Save {0} to {1}\n{2}", theObj.GetType(), fileName, ex);
            }
        }

        /// <summary> Loads TestBatch object from a file. </summary>
        /// <typeparam name="TDeserializeType"> Type of object to Deserialize from file. </typeparam>
        /// <param name="fileName"> Name of file to load TestBatch from. </param>
        /// <returns> An object of type T. </returns>
        public static TDeserializeType LoadFromFile<TDeserializeType>(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var serializer = new DataContractSerializer(typeof(TDeserializeType));
                var retObject = (TDeserializeType)serializer.ReadObject(stream);
                // Console.WriteLine("Loaded Object {0} from file {1}", typeof(TDeserializeType), fileName);
                return retObject;
            }
        }

        /// <summary>
        /// Shuffles a list in random order.
        /// </summary>
        /// <typeparam name="T"> Type of List. </typeparam>
        /// <param name="list"> List of values to be randomly ordered. </param>
        public static void Shuffle<T>(this IList<T> list)
        {
            var provider = new RNGCryptoServiceProvider();
            var n = list.Count;
            while (n > 1)
            {
                var box = new byte[1];
                do
                {
                    provider.GetBytes(box);
                }
                while (!(box[0] < n * (Byte.MaxValue / n)));
                var k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary> Gets APIs and its parameters. </summary>
        /// <param name="contract"> The type having the contrtactual APIs. </param>        
        private static void GetApiListAndParameters(Type contract)
        {
            var contractMethodNames = contract.GetMethods().Select(m => m.Name);
            lock (Apis)
            {
                contract.GetMethods().Where(m => contractMethodNames.Contains(m.Name)).ToList().ForEach(
                    mi =>
                    { 
                        var pis = mi.GetParameters();
                        Apis[mi.Name] = new Tuple<Type, List<string>>(
                            mi.ReturnType,
                            pis.Any() ? pis.Select(pi => pi.Name).ToList() : new List<string>());
                    });
            }
        }
    }
}
