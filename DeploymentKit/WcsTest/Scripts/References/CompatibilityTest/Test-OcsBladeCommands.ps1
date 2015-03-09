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
Param($NumberOfCycles=1)

#---------------------------------------------------------------------------------------------------------------------------------
# This script tests the functionality of all Toolkit commands that run on a server blade.  It doesn't test commands that run on
# a jump box such as Invoke-WcsRest,  Invoke-WcsCommand, etc.
#
# The one EXCEPTION is this script does not check Update commands.  Update commands must be validated manually or from the jump box
#---------------------------------------------------------------------------------------------------------------------------------
Try
{
    $Invocation                   = (Get-Variable MyInvocation -Scope 0).Value
    $WcsScriptDirectory           =  Split-Path $Invocation.MyCommand.Path  
    $WCS_BASE_DIRECTORY           =  Split-Path (Split-Path (Split-Path $WcsScriptDirectory -Parent) -Parent) -Parent
    $SCRIPT_FILE                  =  $Invocation.MyCommand.Name 

    #----------------------------------------------------
    # include the test library if not already included
    #----------------------------------------------------
    . "$WCS_BASE_DIRECTORY\Scripts\References\CompatibilityTest\CompatibilityTestLibrary.ps1"

    $TestSuiteName = "Test-OcsBladeCommands"
 
    $TestDirectory = ("{0}\{1}\{2}" -f $WCS_RESULTS_DIRECTORY,$TestSuiteName,(BaseLib_SimpleDate))
 
    $TestDetails = @{ TestCount=1;
                                PassTests=0;
                                FailTests=0;
                                TestSuiteName       = $TestSuiteName;
                                TestDirectory       = $TestDirectory;
                                TestSuiteStartTime  = (Get-Date);
                                ScriptFile          = $SCRIPT_FILE ;
                                LocalTestDirectory  = ''
                                TranscriptFile      = "$TestDirectory\Transcript-$TestSuiteName.log" }

    #---------------------------------------------------------
    # Setup then start the test suite
    #---------------------------------------------------------
    CompatibilityTest-StartTestSuite -TestDetails ([ref] $TestDetails) -StandAlone:$true 

    #---------------------------------------------------------------------------------
    #  Run the first cycle every time
    #---------------------------------------------------------------------------------
    $TestDetails.TestDirectory="$TestDirectory\Cycle1"
 
    #---------------------------------------------------------------------------------
    # Abort test if the base commands fails because server is not in a testable state
    #---------------------------------------------------------------------------------
    If (0 -ne (Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsBaseCommands.ps1 ([ref] `$TestDetails)"))
    {
        Throw "Base commands failed so aborting test"
    }

    Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsErrorCommands.ps1   ([ref] `$TestDetails)" | Out-Null
    Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsConfigCommands.ps1  ([ref] `$TestDetails)" | Out-Null 
    Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsFRUCommands.ps1     ([ref] `$TestDetails)" | Out-Null
    Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsStressCommands.ps1  ([ref] `$TestDetails)" | Out-Null

    #---------------------------------------------------------------------------------
    #  Run more cycles if specified.
    #---------------------------------------------------------------------------------
    For ($Cycle=2; $Cycle -le $NumberOfCycles; $Cycle++)
    {
        $TestDetails.TestDirectory="$TestDirectory\Cycle$Cycle"

        $ErrorCount = $TestDetails.FailTests
        Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsErrorCommands.ps1   ([ref] `$TestDetails)" | Out-Null
        Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsConfigCommands.ps1  ([ref] `$TestDetails)" | Out-Null
        Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsFRUCommands.ps1     ([ref] `$TestDetails)" | Out-Null
        Invoke-Expression "$COMPATIBILITY_TEST_DIRECTORY\Test-OcsStressCommands.ps1  ([ref] `$TestDetails)" | Out-Null

        #---------------------------------------------------------------------------------
        # If no errors then delete cycle logs.  This is because the logs will take up too
        # much space if doing many cycles.  Note have to change directory so not in the 
        # directory to be deleted (otherwise can't delete it)
        #
        # If cycle had any errors then keep all logs for that cycle, even the passed logs
        #---------------------------------------------------------------------------------
        IF ($ErrorCount -eq $TestDetails.FailTests) 
        {
            Set-Location -Path $TestDirectory
            [System.IO.Directory]::SetCurrentDirectory($TestDirectory)

            Write-Host ("Deleting {0} logs because cycle passed" -f $TestDetails.TestDirectory)
            Remove-Item ("{0}" -f $TestDetails.TestDirectory) -Recurse -Force -ErrorAction SilentlyContinue | Out-Null 
        }
    }
    #---------------------------------------------------------------------------------
    #  Display Test Results
    #---------------------------------------------------------------------------------
    CompatibilityTest-EndTestSuite -TestDetails ([ref] $TestDetails) -StandAlone:$true -ScriptFile $SCRIPT_FILE

    #---------------------------------------------------------------------------------
    #  Return the number of failed tests (0 indicates a pass)
    #---------------------------------------------------------------------------------
    Return   $TestDetails.FailTests
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
    Return 1
}
