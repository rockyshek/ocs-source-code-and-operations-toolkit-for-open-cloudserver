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
using System.Reflection.Emit;
using System.ServiceModel.Web;

namespace ChassisValidation
{
    /// <summary>
    /// Generate a new Type on the fly.
    /// </summary>
    internal class ChassisManagerRestProxyGenerator
    {
        // cache the types already generated
        private static readonly Dictionary<ChassisManagerRestProxyKey, Type> typeCache = new Dictionary<ChassisManagerRestProxyKey, Type>();

        internal static Type CreateType(Type interfaceType, Type baseType)
        {
            Type type = null;

            // if the type is already generated, just return it
            var key = new ChassisManagerRestProxyKey(interfaceType, baseType);
            if (typeCache.TryGetValue(key, out type))
            {
                return type;
            }

            // create a new type
            var assemblyName = new AssemblyName(Guid.NewGuid().ToString("N").ToUpper());
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, string.Concat(assemblyName.Name, ".dll"));
            var typeBuilder = moduleBuilder.DefineType(string.Concat(interfaceType.Name, assemblyName.Name), TypeAttributes.Public);
            
            typeBuilder.SetParent(baseType);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            ImplementInterfaceMethods(interfaceType, baseType, typeBuilder);
            type = typeBuilder.CreateType();
       
            return type;
        }

        /// <summary>
        /// Implements all the methods in the interface.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface to be implemented.
        /// </param>
        /// <param name="baseType">
        /// The base type of the generated type.
        /// </param>
        /// <param name="typeBuilder">
        /// A TypeBuilder instance.
        /// </param>
        private static void ImplementInterfaceMethods(Type interfaceType, Type baseType, TypeBuilder typeBuilder)
        {
            // cache some MethodInfo for the convenience of ILGenerator emitting method calls.
            MethodInfo dictionaryAddMethodInfo = typeof(Dictionary<String, Object>).GetMethod("Add");
            MethodInfo makeRequestGenericMethodInfo = baseType.GetMethod("MakeRequest",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // implement each method defined in the interface
            foreach (var methodInfo in interfaceType.GetMethods())
            {
                // get all the parameters of the current method
                var paramInfoArray = methodInfo.GetParameters();
                var methodBuilder = typeBuilder.DefineMethod(
                    name : methodInfo.Name,
                    attributes : MethodAttributes.Public | MethodAttributes.Virtual, // virtual is required
                    returnType : methodInfo.ReturnType,
                    parameterTypes: paramInfoArray.Select(_ => _.ParameterType).ToArray());
                var ilGenerator = methodBuilder.GetILGenerator();
                
                // declare a local variable (say params) to store all parameters in a Dictionary
                var paramsLb = ilGenerator.DeclareLocal(typeof(Dictionary<String, Object>));
                // set the generic type in MakeRequest as the method return type defined in the interface
                var makeRequestMethodInfo = makeRequestGenericMethodInfo.MakeGenericMethod(methodInfo.ReturnType);
                // create a new Dictionary object ans store it in the local variable
                ilGenerator.Emit(OpCodes.Newobj, typeof(Dictionary<String, Object>).GetConstructor(Type.EmptyTypes));
                ilGenerator.Emit(OpCodes.Stloc, paramsLb);
           
                var i = 0;
                // store all the method parameters as key-value pairs
                foreach (var parameterInfo in paramInfoArray)
                { 
                    i++; // load the args from arg_1, since arg_0 is reserved for 'this'
                    // generate IL functioning like: params.Add(paramName, paramValue)
                    // load the params local variable onto the stack
                    ilGenerator.Emit(OpCodes.Ldloc, paramsLb);
                    // load the current parameter name onto the stack
                    ilGenerator.Emit(OpCodes.Ldstr, parameterInfo.Name);
                    // load the current parameter value onto the stack
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                    // convert value type to object reference, or the value will not be passed correctly
                    if (parameterInfo.ParameterType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, parameterInfo.ParameterType);
                    }
                    // call Dictionary.Add method, add the objects on the stack into the Dictionary
                    ilGenerator.Emit(OpCodes.Callvirt, dictionaryAddMethodInfo);
                }

                // prepare to call MakeRequest method defined in the base class
                // load 'this' object onto the stack
                ilGenerator.Emit(OpCodes.Ldarg_0); // arg_0 is 'this'
                // retrieve the WebInvokeAttribute of the method
                var webInvokeAttr = methodInfo.GetCustomAttributes(typeof (WebInvokeAttribute), true)
                                              .Cast<WebInvokeAttribute>()
                                              .FirstOrDefault();
                // retrive the http method; if not found, use GET as default
                var httpMethod = webInvokeAttr == null ? "GET" : webInvokeAttr.Method;
                // load the http method onto the stack
                ilGenerator.Emit(OpCodes.Ldstr, httpMethod);
                // use the method name as the api name
                var apiName = methodInfo.Name;
                // load the api name onto the stack               
                ilGenerator.Emit(OpCodes.Ldstr, apiName);
                // load the params onto the stack
                ilGenerator.Emit(OpCodes.Ldloc, paramsLb);
                // call this.MakeRequest method, with the objects on the stack 
                ilGenerator.Emit(OpCodes.Callvirt, makeRequestMethodInfo);
                // return from the current method
                ilGenerator.Emit(OpCodes.Ret);
            }
        }
    }
}
