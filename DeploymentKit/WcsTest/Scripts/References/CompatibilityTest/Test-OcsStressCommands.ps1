#=================================================================================================================================
# Copyright (c) Microsoft Corporation
# All rights reserved. 
# MIT License
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
# (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
# merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
# furnished to do so, subject to the following conditions:
# The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
# LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
# OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#=================================================================================================================================

#=================================================================================================================================
# This script runs a suite of tests that verify the stress commands
#
# RULES  :  Check each command's return code, input arguments, error handling, and functionality
#=================================================================================================================================
Param([ref] $TestDetails = ([ref] $null),[int]$TestTimeInMin=1)

Try
{
    $Invocation                   = (Get-Variable MyInvocation -Scope 0).Value
    $WcsScriptDirectory           =  Split-Path $Invocation.MyCommand.Path  
    $WCS_BASE_DIRECTORY           =  Split-Path (Split-Path (Split-Path $WcsScriptDirectory -Parent) -Parent) -Parent
    $SCRIPT_FILE                  =  $Invocation.MyCommand.Name 

    #----------------------------------------------------
    # Include the test library if not already included
    #----------------------------------------------------
    . "$WCS_BASE_DIRECTORY\Scripts\References\CompatibilityTest\CompatibilityTestLibrary.ps1"

    #---------------------------------------------------------
    # If running standalone set the test details to defaults
    #---------------------------------------------------------
    if ($TestDetails.Value -eq $null)
    {   
        $Standalone = $true

        $TestSuiteName = "Test-OcsConfigCommands"

        $TestDirectory = ("{0}\{1}\{2}" -f $WCS_RESULTS_DIRECTORY,$TestSuiteName,(BaseLib_SimpleDate))

        $TestDetails.Value = @{ TestCount          = 1;
                                PassTests          = 0;
                                FailTests          = 0;
                                ScriptFile          = $SCRIPT_FILE ;
                                TestSuiteName      = $TestSuiteName;
                                TestDirectory      = $TestDirectory ;
                                LocalTestDirectory = ''
                                TestSuiteStartTime = (Get-Date);
                                TranscriptFile     = "$TestDirectory\Transcript-$TestSuiteName.log" }
    }
    Else
    {
        $TestDetails.Value.ScriptFile = $SCRIPT_FILE
        $Standalone = $false
    }
    #---------------------------------------------------------
    # Setup then start the test suite
    #---------------------------------------------------------
    CompatibilityTest-StartTestSuite -TestDetails $TestDetails -StandAlone:$Standalone  

    $FULL_TEST_DIRECTORY  = $TestDetails.Value.TestDirectory
    $LOCAL_TEST_DIRECTORY = $TestDetails.Value.LocalTestDirectory

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Run-IOmeter:   Inputs   =  TimeInMin, LogDirectory, Full, NoWait (RunAsChild)
    #                 Return   =  Number of errors
    #
    #                 Function = Runs IOmeter for the time specified
    #                 Function = Logs files in the directory specified
    #                 Function = Logs files in the default directory if not specified
    #                 Function = Identifies errors in the IOmeter log file
    #                 Function = Identifies if IOmeter hangs
    #                 Function = Identifies if IOmeter exits early
    #                 Function = Can run in NoWait mode with Verify-IOmeter command
    #                 Function = Tests boot drive if -Full specified
    #
    #                 Function = Verify-IOmeter reports missing LogDirectory and returns non-zero integer
    #-------------------------------------------------------------------------------------------------------------------------------------
    If ( -Not (CoreLib_IsWinPE))
    {
        $Directory = "$WCS_RESULTS_DIRECTORY\Run-IOmeter"

        Remove-Item  $Directory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
       
        $MoveTo = "$FULL_TEST_DIRECTORY\ID-STRESS-0001-IOmeter"
        CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-Iometer basic functionality'              -TestId 'ID-STRESS-0001' -TestCommand "Run-Iometer -TimeInMin $TestTimeInMin" -Directory $Directory -MoveTo $MoveTo 
        
        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0002-IOmeter"
        CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-Iometer with Full and LogDirectory inputs' -TestId 'ID-STRESS-0002' -TestCommand "Run-Iometer -TimeInMin $TestTimeInMin -Full -Logdirectory $Directory"  

        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0003-IOmeter"
        CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-Iometer with NoWait and LogDirectory inputs' -TestId 'ID-STRESS-0003' -RunCommand "Run-Iometer -TimeInMin $TestTimeInMin -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-Iometer -Logdirectory $Directory" -TimeInMin $TestTimeInMin 

        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0004-IOmeter"
        CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-Iometer fails on hang condition'             -TestId 'ID-STRESS-0004' -RunCommand "Run-Iometer -TimeInMin 10 -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-Iometer -Logdirectory $Directory"  -InjectHang

        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0005-IOmeter"
        CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-Iometer fails when log file has errors'      -TestId 'ID-STRESS-0005' -RunCommand "Run-Iometer -TimeInMin 1  -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-Iometer -Logdirectory $Directory"  -TimeInMin 2 -InjectIOmeterError

        CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Verify-IOmeter with missing LogDirectory'        -TestId 'ID-STRESS-0006' -TestCommand "Verify-IOmeter -LogDirectory \Wcstest\NoDir"    -MatchStrings "* aborted: Could not open log directory *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Run-DiskSpeed: Inputs   =  TimeInMin, LogDirectory, Full, NoWait (RunAsChild)
    #                 Return   =  Number of errors
    #
    #                 Function = Runs DiskSpeed for the time specified
    #                 Function = Logs files in the directory specified
    #                 Function = Logs files in the default directory if not specified
    #                 Function = Identifies errors in the DiskSpeed log file
    #                 Function = Identifies if DiskSpeed hangs
    #                 Function = Identifies if DiskSpeed exits
    #                 Function = Can run in NoWait mode with Verify-DiskSpeed command
    #                 Function = Tests boot drive if -Full specified
    #-------------------------------------------------------------------------------------------------------------------------------------
    $Directory = "$WCS_RESULTS_DIRECTORY\Run-DiskSpeed"

    Remove-Item  $Directory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
       
    $MoveTo = "$FULL_TEST_DIRECTORY\ID-STRESS-0010-DiskSpeed"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-DiskSpeed basic functionality'               -TestId 'ID-STRESS-0010' -TestCommand "Run-DiskSpeed -TimeInMin $TestTimeInMin" -Directory $Directory -MoveTo $MoveTo 
        
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0011-DiskSpeed"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-DiskSpeed with Full and LogDirectory inputs' -TestId 'ID-STRESS-0011' -TestCommand "Run-DiskSpeed -TimeInMin $TestTimeInMin -Full -LogDirectory $Directory" -Directory $Directory  
    
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0012-DiskSpeed"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-DiskSpeed with NoWait and LogDirectory inputs' -TestId 'ID-STRESS-0012' -RunCommand "Run-DiskSpeed -TimeInMin $TestTimeInMin -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-DiskSpeed -Logdirectory $Directory" -TimeInMin $TestTimeInMin 

    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0013-DiskSpeed"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-DiskSpeed fails on hang condition'             -TestId 'ID-STRESS-0013' -RunCommand "Run-DiskSpeed -TimeInMin 10 -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-DiskSpeed -Logdirectory $Directory"    -InjectHang

    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0014-DiskSpeed"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-DiskSpeed fails when log file has errors'      -TestId 'ID-STRESS-0014' -RunCommand "Run-DiskSpeed -TimeInMin 1  -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-DiskSpeed -Logdirectory $Directory"  -TimeInMin 2 -InjectDiskSpeedError

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Verify-DiskSpeed with missing LogDirectory'        -TestId 'ID-STRESS-0015' -TestCommand "Verify-DiskSpeed -LogDirectory \Wcstest\NoDir"    -MatchStrings "* aborted: Could not open log directory *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Run-Prime95: Inputs   =  TimeInMin, LogDirectory, LimitThread , NoWait (RunAsChild)
    #                 Return   =  Number of errors
    #
    #                 Function = Runs Prime95 for the time specified
    #                 Function = Logs files in the directory specified
    #                 Function = Logs files in the default directory if not specified
    #                 Function = Identifies errors in the Prime95 log file
    #                 Function = Identifies if Prime95 exits
    #                 Function = Can run in NoWait mode with Verify-Prime95 command
    #                 Function = Tests LimitThread input
    #-------------------------------------------------------------------------------------------------------------------------------------
    $Directory = "$WCS_RESULTS_DIRECTORY\Run-Prime95"

    Remove-Item  $Directory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
       
    $MoveTo = "$FULL_TEST_DIRECTORY\ID-STRESS-0020-Prime95"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-Prime95 basic functionality'              -TestId 'ID-STRESS-0020' -TestCommand "Run-Prime95 -TimeInMin $TestTimeInMin" -Directory $Directory -MoveTo $MoveTo 
        
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0021-Prime95"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-Prime95 with LimitThread and LogDirectory inputs' -TestId 'ID-STRESS-0021' -TestCommand "Run-Prime95 -TimeInMin $TestTimeInMin -LimitThread 2 -LogDirectory $Directory" -Directory $Directory  
    
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0022-Prime95"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-Prime95 with NoWait and LogDirectory inputs' -TestId 'ID-STRESS-0022' -RunCommand "Run-Prime95 -TimeInMin $TestTimeInMin -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-Prime95 -Logdirectory $Directory" -TimeInMin $TestTimeInMin 

    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0023-Prime95"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-Prime95 fails when log file has errors'      -TestId 'ID-STRESS-0023' -RunCommand "Run-Prime95 -TimeInMin 1  -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-Prime95 -Logdirectory $Directory"  -TimeInMin 2 -InjectPrime95Error

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Verify-Prime95 with missing LogDirectory'        -TestId 'ID-STRESS-0024' -TestCommand "Verify-Prime95 -LogDirectory \Wcstest\NoDir"    -MatchStrings "* aborted: Could not open log directory *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED


    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Run-QuickStress: Inputs   =  TimeInMin, LogDirectory, Full, IOmeter, NoWait (RunAsChild)
    #                   Return   =  Number of errors
    #
    #                 Function = Runs QuickStress for the time specified
    #                 Function = Logs files in the directory specified
    #                 Function = Logs files in the default directory if not specified
    #                 Function = Identifies errors in the QuickStress log file
    #                 Function = Identifies if QuickStress hangs
    #                 Function = Identifies if QuickStress exits
    #                 Function = Can run in NoWait mode with Verify-QuickStress command
    #                 Function = Tests boot drive if -Full specified
    #
    #                 Function = Reports File missing and returns null when file+path incorrect
    #-------------------------------------------------------------------------------------------------------------------------------------
    $Directory = "$WCS_RESULTS_DIRECTORY\Run-QuickStress"

    Remove-Item  $Directory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
       
    $MoveTo = "$FULL_TEST_DIRECTORY\ID-STRESS-0030-QuickStress"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-QuickStress basic functionality'               -TestId 'ID-STRESS-0030' -TestCommand "Run-QuickStress -TimeInMin $TestTimeInMin" -Directory $Directory -MoveTo $MoveTo 
        
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0031-QuickStress"
    CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-QuickStress with Full and LogDirectory inputs'  -TestId 'ID-STRESS-0031' -TestCommand "Run-QuickStress -TimeInMin $TestTimeInMin -Full -LogDirectory $Directory" -Directory $Directory  
  
    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0032-QuickStress"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-QuickStress with NoWait and LogDirectory inputs' -TestId 'ID-STRESS-0032' -RunCommand "Run-QuickStress -TimeInMin $TestTimeInMin -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-QuickStress -Logdirectory $Directory" -TimeInMin $TestTimeInMin 

    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0033-QuickStress"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-QuickStress fails on hang condition'             -TestId 'ID-STRESS-0033' -RunCommand "Run-QuickStress -TimeInMin 10 -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-QuickStress -Logdirectory $Directory"    -InjectHang

    $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0034-QuickStress"
    CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-QuickStress fails when log file has errors'      -TestId 'ID-STRESS-0034' -RunCommand "Run-QuickStress -TimeInMin 1  -NoWait -Logdirectory $Directory" -VerifyCommand "Verify-QuickStress -Logdirectory $Directory"  -TimeInMin 2 -InjectQuickStressError

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Verify-QuickStress with missing LogDirectory'        -TestId 'ID-STRESS-0035' -TestCommand "Verify-QuickStress -LogDirectory \Wcstest\NoDir"    -MatchStrings "* aborted: Could not open log directory *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #---------------------------------------------------------------------------------
    # IOMETER DOES NOT RUN ON WINPE
    #---------------------------------------------------------------------------------
    If ( -Not (CoreLib_IsWinPE))
    {
        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0035-QuickStress"
        CompatibilityTest-SimpleCommand  -TestDetails $TestDetails -TestName 'Verify Run-QuickStress with Full and LogDirectory inputs'  -TestId 'ID-STRESS-0035' -TestCommand "Run-QuickStress -TimeInMin $TestTimeInMin -IOmeter -LogDirectory $Directory"  

        $Directory = "$FULL_TEST_DIRECTORY\ID-STRESS-0036-QuickStress"
        CompatibilityTest-StressNoWait -TestDetails $TestDetails -TestName 'Verify Run-QuickStress with NoWait and LogDirectory inputs' -TestId 'ID-STRESS-0036' -RunCommand "Run-QuickStress -TimeInMin $TestTimeInMin -NoWait -IOmeter -Logdirectory $Directory" -VerifyCommand "Verify-QuickStress -IOmeter -Logdirectory $Directory" -TimeInMin $TestTimeInMin 
    }



    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Run-DiskTest:  Inputs   =  Type, Disk
    #                 Return   =  Number of disks that failed test
    #
    # Add the phyerror detection capability!!!
    #-------------------------------------------------------------------------------------------------------------------------------------


    #---------------------------------------------------------------------------------
    #  Display Test Results
    #---------------------------------------------------------------------------------
    CompatibilityTest-EndTestSuite -TestDetails $TestDetails -StandAlone:$Standalone -ScriptFile $SCRIPT_FILE

    #---------------------------------------------------------------------------------
    #  Return the number of failed tests (0 indicates a pass)
    #---------------------------------------------------------------------------------
    Return   $TestDetails.Value.FailTests

}
#---------------------------------------------------------------------------------
#  Exception handler for unknown errors (return non-zero to indicate fail)
#---------------------------------------------------------------------------------
Catch
{
    If ($_.ErrorDetails -eq $null) 
    {   
        Try
        {
            $Position = $_.Exception.ErrorRecord.InvocationInfo.PositionMessage
        }
        Catch
        {
            $Position = $_.InvocationInfo.PositionMessage 
        }
        Write-Host -ForegroundColor Red  ("`r`nEXCEPTION: {0}`r`n{1}`r`n`r" -f $_.Exception.Message,$Position)
    }
    $TestDetails.Value.FailTests++
    Return 1
}






