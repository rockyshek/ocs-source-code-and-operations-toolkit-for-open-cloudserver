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
using System.Linq;

namespace CMTestAutomationInterface.Controller
{
    internal class ChassisManagerUserOptions : ChassisManagerOptionBase
    {
        [ChassisManagerOption(LongOptionName = "Help", ShortOptionName = 'h', OptionDescription = "Show command help.")]
        public bool Help { get; set; }

        [ChassisManagerOption(LongOptionName = "RunBatches", ShortOptionName = 'a', OptionDescription = "Run all test cases.")]
        public bool RunBatches { get; set; }

        [ChassisManagerOption(LongOptionName = "RunBatch", ShortOptionName = 'b', OptionDescription = "Run single or multiple test cases.")]
        public bool RunBatch { get; set; }

        [ChassisManagerOption(LongOptionName = "RunFuncBvt", ShortOptionName = 't', OptionDescription = "Run functional Bvt test.")]
        public bool RunFuncBvt { get; set; }

        [ChassisManagerOption(LongOptionName = "VerifyCSpec", ShortOptionName = 'v', OptionDescription = "Verify Chassis specification.")]
        public bool VerifyCSpec { get; set; }

        [ChassisManagerOption(LongOptionName = "GetCmApiInfo", ShortOptionName = 'c', OptionDescription = "Get Chassis Manager Api Info.")]
        public bool GetCmApiInfo { get; set; }

        [ChassisManagerOption(LongOptionName = "FixCounterNames", ShortOptionName = 'n', OptionDescription = "Fix Counter Names.")]
        public bool FixCounterNames { get; set; }

        [ChassisManagerOption(LongOptionName = "SummaryReports", ShortOptionName = 'r', OptionDescription = "Create Summary Report From Batch Results.")]
        public bool SummaryReports { get; set; }

        [ChassisManagerOption(LongOptionName = "SampleBatchFile", ShortOptionName = 's', OptionDescription = "Run Create sample Batch File.")]
        public bool SampleBatchFile { get; set; }
    }
}
