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

#---------------------------------------------------------------------------------------------------------------------------------
# This script runs the basic set of commands needed to run the other test suites.  If any test fails in this suite then
# it is not expected to run other suites afterwards
#---------------------------------------------------------------------------------------------------------------------------------
Param([ref] $TestDetails = ([ref] $null) )

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

        $TestSuiteName = "Test-OcsBaseCommands"

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
    #  View-WcsVersion: Inputs   = None
    #                   Return   = null
    #
    #                   Function = Displays Toolkit version
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'View-WcsVersion'
    $TestID            = 'ID-BASE-0001'

    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

    #---------------------
    # Display test details
    #---------------------
    Write-Host " This test runs View-WcsVersion and verifies:

                `r   1) Command runs with error
                `r   2) Command returns null
                `r   3) Command displays the Toolkit version in expected format

                `r Full Command : View-WcsVersion

                `r View-WcsVersion's Console Output --->`r"
    #---------------------
    # Start the test
    #---------------------
    Try
    {
        #--------------------------------------------------------------------------------------
        # Save the end point of transcript file before command so can get the command's output
        #--------------------------------------------------------------------------------------
        $TempFile = $WCS_TRANSCRIPT_COPY

        [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$TempFile ,$true)
        $TranscriptStart = ([system.io.file]::ReadAllLines($TempFile)).Length

        #--------------------------------------------------------------------------------------
        # Run command being tested
        #--------------------------------------------------------------------------------------
        $ReturnedValue = View-WcsVersion -ErrorAction Stop  

        #--------------------------------------------------------------------------------------
        # Save the command's output
        #--------------------------------------------------------------------------------------
        [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$TempFile ,$true)
        $CommandConsoleOutput = [system.io.file]::ReadAllLines($TempFile)  
        $CommandConsoleOutput = ($CommandConsoleOutput[$TranscriptStart..($CommandConsoleOutput.Length-1)])

        Write-Host "<--- End Console Output`r`n"
        #--------------------------------------------------------------------------------------
        # Verify return value matches expected
        #--------------------------------------------------------------------------------------
        If ($null -ne  $ReturnedValue)
        {
            Throw "Return value incorrect.  Expected null but received $ReturnedValue"
        }
        #--------------------------------------------------------------------------------------
        # Verify command output
        #--------------------------------------------------------------------------------------
        If (-not ($CommandConsoleOutput[0] -clike "*OCS Operations Toolkit Version *$Global:WcsTestToolsVersion*"))
        {
            Throw ("Error message {0}`n Does not match expected {1}" -f ($CommandConsoleOutput[0]) ,"*OCS Operations Toolkit Version *$Global:WcsTestToolsVersion*" )
        }
        #--------------------------------------------------------------------------------------
        # Test passed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.PassTests += Pass-CompatibilityTest -StartTime $TestStartTime  
    }
    Catch
    {
        #--------------------------------------------------------------------------------------
        # Test failed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime  -Exception $_
    }

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Verify files in \wcstest\scripts\binaries are installed.  Some of these are third party apps that must be installed by the 
    #  end user.  This makes it likely to have missing files so this checks for them.
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'Verify required files are installed'
    $TestID            = 'ID-BASE-0002'

    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

    #---------------------
    # Display test details
    #---------------------
    Write-Host " This test verifies all required files are installed in: $WCS_BINARY_DIRECTORY`r`n`r"  

    #---------------------
    # Start the test
    #---------------------
    Try
    {
        $FailedTest = $False
        Get-Content -Path $WCS_BINARY_FILELIST -ErrorAction Stop | ForEach-Object {
    
            If (-not (Test-Path $_))
            {
                Write-Host "Did not find file: $_"
                $FailedTest = $true
            }
        }
        If ($FailedTest)
        {
            Throw "Files required for operation were not found"
        }
        #--------------------------------------------------------------------------------------
        # Test passed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.PassTests += Pass-CompatibilityTest -StartTime $TestStartTime     
    }
    Catch
    {
        #--------------------------------------------------------------------------------------
        # Test failed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime  -Exception $_

        #--------------------------------------------------------------------------------------
        # If could not backup errors then abort test
        #--------------------------------------------------------------------------------------  
        Write-Host -BackgroundColor Red " Aborting $TestSuiteName because could not run IPMI command"
        Return 1
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Lookup-WcsSystem: Inputs   = None
    #                    Return   = System type
    #
    #                    Function = Identifies the system type test is running on
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'Verify Lookup-System'
    $TestID            = 'ID-BASE-0003'
    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

    #---------------------
    # Display test details
    #---------------------
    Write-Host " This test verifies the system can be identified`r`n`r"  

    #---------------------
    # Start the test
    #---------------------
    Try
    {
        #--------------------------------------------------------------------------------------
        # Run command being tested
        #--------------------------------------------------------------------------------------
        If ('Unknown' -eq (Lookup-WcsSystem -ErrorAction SilentlyContinue))
        {
            Throw ("Could not identify the system")
        } 
        #--------------------------------------------------------------------------------------
        # Test passed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.PassTests += Pass-CompatibilityTest -StartTime $TestStartTime  
    }
    Catch
    {
        #--------------------------------------------------------------------------------------
        # Test failed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime  -Exception $_

        #--------------------------------------------------------------------------------------
        # If could not backup errors then abort test
        #--------------------------------------------------------------------------------------  
        Write-Host -BackgroundColor Red " Aborting $TestSuiteName because could required base functionality failed"
        Return 1
    }

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Invoke-WcsIpmi: Inputs   = Command, RequestData, NetworkFunction, LUN
    #                  Return   = Response bytes from BMC
    #
    #                  Function = BMC responds to IPMI request
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'Verify Invoke-WcsIpmi basic functionality'
    $TestID            = 'ID-BASE-0004'
    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

    #---------------------
    # Display test details
    #---------------------
    Write-Host " This test verifies IPMI functionality by reading the BMC version`r`n`r"  

    #---------------------
    # Start the test
    #---------------------
    Try
    {
        #--------------------------------------------------------------------------------------
        # Run command being tested
        #--------------------------------------------------------------------------------------
        $IpmiData = Invoke-WcsIpmi  0x1 @() $WCS_APP_NETFN -ErrorAction Stop

        If (0 -ne $IpmiData[0])
        {
            Throw ("Invoke-WcsIpmi returned completion code {0}" -f $IpmiData[0])
        } 
        #--------------------------------------------------------------------------------------
        # Test passed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.PassTests += Pass-CompatibilityTest -StartTime $TestStartTime  
    }
    Catch
    {
        #--------------------------------------------------------------------------------------
        # Test failed so display results
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime  -Exception $_

        #--------------------------------------------------------------------------------------
        # If could not backup errors then abort test
        #--------------------------------------------------------------------------------------  
        Write-Host -BackgroundColor Red " Aborting $TestSuiteName because could required base functionality failed"
        Return 1
    }

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
