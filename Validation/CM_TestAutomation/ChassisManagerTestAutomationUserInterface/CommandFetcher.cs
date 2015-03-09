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
using CMTestAutomationInterface.Controller;

namespace CMTestAutomationInterface
{
    /// <summary>
    /// A very simple test engine to run test cases.
    /// </summary>
    public static class CommandFetcher
    { 
        public static void ExecuteUserCommand(string command, Dictionary<string, string> userInputsArgs)
        {
            ChassisManagerLauncher launcher = new ChassisManagerLauncher();
            
            List<KeyValuePair<string, string>> methodParam = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, string> parm in userInputsArgs)
            {
                if (parm.Key != command && parm.Key != string.Empty)
                {
                    methodParam.Add(new KeyValuePair<string, string>(CorrectParameterName(command,parm.Key) , parm.Value));
                }
            }

            try
            {
                launcher.GetType().InvokeMember(command, BindingFlags.InvokeMethod, null, launcher,
                    methodParam.Select(d => d.Value).ToArray(),
                    null, null, methodParam.Select(d => d.Key).ToArray());
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.Data["OriginalStackTrace"] = ex.InnerException.StackTrace;
                throw ex.InnerException;
            }
        }

        /// <summary>
        ///  this will take care of lower case sentivity
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        private static string CorrectParameterName(string methodName, string parameterName)
        {

            // get all public static methods of ChassisManagerLauncher type
            MethodInfo[] methodInfos = typeof(ChassisManagerLauncher).GetMethods(BindingFlags.Public |
                                                                                 BindingFlags.Static);

            // sort methods by name
            methodInfos = Array.FindAll(methodInfos, i => i.Name == methodName);
                        
            // Loop each method names Find first match
            foreach (MethodInfo methodInfo in methodInfos)
            {
                foreach (ParameterInfo param in methodInfo.GetParameters()) // name and parametertype
                {
                    if (string.Equals(parameterName, param.Name, StringComparison.InvariantCultureIgnoreCase))//  EqualsIgnoreCase(param.Name, p.Key))
                    {
                        return param.Name;
                    }
                }
            }


            return parameterName;
        }
    }
}
