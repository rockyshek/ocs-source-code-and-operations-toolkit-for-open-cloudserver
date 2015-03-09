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
using System.Text;

namespace Microsoft.GFS.WCS.WcsCli
{
    class TabHelper
    {
        private static string previousPrefix = string.Empty;
        public static string[] GetTabOptions(string word)
        { 
            var stringArray = new string[]
            {
                "wcscli", 
                // Infrastructure commands
                "-getserviceversion", "-getchassisinfo", "-getchassishealth", "-getbladeinfo", "-getbladehealth", 
                "-updatepsufw", "-getpsufwstatus",
                
                // Blade Management commands
                 "-getpowerstate", "-setpoweron", "-setpoweroff", "-getbladestate", "-setbladeon", "-setbladeoff", "-setbladedefaultpowerstate",
                 "-getbladedefaultpowerstate", "-setbladeactivepowercycle", "-getnextboot", "-setnextboot", "-setbladeattentionledon",
                 "-setbladeattentionledoff", "-readbladelog", "-clearbladelog", "-getbladepowerreading", "-getbladepowerlimit",
                 "-setbladepowerlimit", "-setbladepowerlimiton", "-setbladepowerlimitoff", "-setdatasafebladeon", "-setdatasafepoweron",
                 "-setdatasafebladeoff", "-setdatasafepoweroff", "-setbladedatasafeactivepowercycle", "-getbladedatasafepowerstate",
                 "-getbladebiospostcode", "-setbladepsualertdpc", "-getbladepsualertdpc", "-getbladepsualert", "-activatedeactivatebladepsualert",
                 "-getbladeassetinfo", "-getBladeMezzAssetInfo", "-setbladeassetinfo", "-getblademezzpassthroughmode", "-setblademezzpassthroughmode",

               // Chassis Management commands
                 "-getchassisattentionledstatus", "-setchassisattentionledon", "-setchassisattentionledoff", "-readchassislog", "-clearchassislog",
                 "-getacsocketpowerstate", "-setacsocketpowerstateon", "-setacsocketpowerstateoff", "-getchassismanagerassetinfo",
                 "-getpdbassetinfo", "-setchassismanagerassetinfo", "-setpdbassetinfo",

               // Local commands, CLI serial mode only
                 "-setnic", "-getnic", "-clear",

               // Chassis Manager service configuration commands 
                 "-startchassismanager", "-stopchassismanager", "-getchassismanagerstatus", "-enablechassismanagerssl", "-disablechassismanagerssl",

               // User Management Commands
                 "-adduser", "-changeuserrole", "-changeuserpwd", "-removeuser",

               // Serial Session Commands
                 "-startbladeserialsession", "-stopbladeserialsession", "-startportserialsession", "-stopportserialsession", "-establishcmconnection",
                 "-terminatecmconnection"
            };

            var TabResults = stringArray.ToList().Where(a => a.StartsWith(word)).Select(s => s.Substring(word.Length)).ToArray();
            Array.Sort(TabResults, StringComparer.InvariantCultureIgnoreCase);

            return TabResults.ToArray();
        }
    }
}
