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
    public static class Testing
    {
        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="batchDirectory"> Directory where the batches reside. </param>
        /// <param name="chassisManagerEndPoint"> CM Url to run batch against. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="UserPassword"> Password for the user we are using to connect to the CM. </param>
        [Action("Runs all Batches named *Batch.xml in from given Directory.")]
        public static void RunBatches(
            [Required(Description = "BatchDirectory")] string batchDirectory,
            [Required(Description = "CM URL")] string chassisManagerEndPoint,
            [Optional("Admin", Description = "User Name")] string userName,
            [Optional("$pl3nd1D", Description = "User Password")] string userPassword)
        {
            var framework = new CmTestWithFramework();
            framework.RunAllFrameworkBatches(batchDirectory, chassisManagerEndPoint, userName, userPassword);
        }

        /// <summary>
        /// Runs all batches from specified directory against specified CM.
        /// </summary>
        /// <param name="batchDefinitionFile"> Directory where the batches reside. </param>
        /// <param name="chassisManagerEndPoint"> CM Url to run batch against. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="UserPassword"> Password for the user we are using to connect to the CM. </param>
        [Action("Runs specified Batch.")]
        public static void RunBatch(
            [Required(Description = "BatchFile")] string batchDefinitionFile,
            [Required(Description = "CM URL")] string chassisManagerEndPoint,
            [Optional("Admin", Description = "User Name")] string userName,
            [Optional("$pl3nd1D", Description = "User Password")] string userPassword)
        {
            var framework = new CmTestWithFramework();
            framework.RunFrameworkBatch(batchDefinitionFile, chassisManagerEndPoint, userName, userPassword);
        }

        /// <summary>
        /// <param name="chassisManagerEndPoint"> CM Url to run batch against. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="UserPassword"> Password for the user we are using to connect to the CM. </param>
        [Action("Gets all hardware Information.")]
        public static void VerifyChassisSpec(            
            [Required(Description = "CM URL")] string chassisManagerEndPoint,
            [Optional("C:\\VS Projects\\Mt Rainier\\Manageability\\Developement\\CM_TestAutomation\\CMVerificationBatches\\BingSKU.xml",Description = "ChassisInfoXML")] string SKUDefinitionFileName,
            [Optional("", Description = "User Name")] string userName,
            [Optional("", Description = "User Password")] string userPassword)
        {
            var framework = new CmTestWithFramework();
            framework.VerifyChassisSpec(chassisManagerEndPoint, SKUDefinitionFileName, userName, userPassword);
        }

        /// <summary>
        /// All parameters are taken from the config file of the CM_TestAutomation project. </param>
        /// <param name="UserName"> User name to use to connect to the target CM. </param>
        /// <param name="UserPassword"> Password for the user we are using to connect to the CM. </param>
        [Action("Runs subset of the full functional collection of tests")]
        public static void RunFunctionalBVT(
            [Required(Description = "CM URL")] string chassisManagerEndPoint,
            [Optional("C:\\VS Projects\\Mt Rainier\\Manageability\\Developement\\CM_TestAutomation\\CMVerificationBatches\\BingSKU.xml", Description = "ChassisInfoXML")] string SKUDefinitionFileName,
            [Optional("", Description = "User Name")] string userName,
            [Optional("",Description = "User Password")] string userPassword)
        {
            var framework = new CmTestWithFramework();
            framework.RunFunctionalBVT(chassisManagerEndPoint, SKUDefinitionFileName, userName, userPassword);
        }
    }
}
