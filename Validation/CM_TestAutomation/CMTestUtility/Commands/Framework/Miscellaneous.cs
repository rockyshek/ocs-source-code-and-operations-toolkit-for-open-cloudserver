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

namespace Microsoft.GFS.WCS.Test.Commands.Framework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Principal;

    using Microsoft.GFS.WCS.Test;
    using Microsoft.GFS.WCS.Test.Consoler;
    using Microsoft.GFS.WCS.Test.Framework;

    /// <summary> Commands for manipulating Framework. </summary>
    [ConsoleSuite]
    public static class Miscellaneous
    {
        /// <summary> Prints List of CM APIs or signature of a given API. </summary>
        /// <param name="apiName"> Name of API to get information for. </param>
        [Action("Prints List of CM APIs or signature of a given API.")]
        public static void GetCmApiInfo(
            [Optional(null,
                Description = "API name. Returns List of API if not specified.")]
            string apiName)
        {
            if (string.IsNullOrWhiteSpace(apiName))
            {
                Console.WriteLine(Helper.DumpAllChassisManagerApi());
            }
            else
            {
                Console.WriteLine(Helper.DumpChassisManagerApi(apiName));
            }
        }
    }
}
