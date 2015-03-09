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
# This script runs a suite of tests to verify Toolkit commands that deal with errors.  These commands are:
#
# Clear-WcsErrors, Pre-WcsTest, Check-WcsErrors, Post-WcsTest, Get-WcsSel, Log-WcsSel, View-WcsSel
# Get-WcsHealth, Log-WcsHealth, View-WcsHealth, and Check-WcsDiagnostics
#
#
# DEPENDENCIES:  1) Only runs on a WCS Compute blade with IPMI compliant BMC
#                2) Reference files for the blade being tests.  Refer to Compatibility Test Setup for instructions on
#                   how to create these files
#
# WARNING:       THIS SCRIPT WILL READ, CLEAR AND WRITE THE BMC SEL.  THE SEL IS BACKED UP AT THE START OF THE TEST AND 
#                CLEARED AT THE END OF THE TEST SUITE.  IF ABORTED IN THE MIDDLE THEN THERE MAY BE FALSE ENTRIES IN THE SEL.
##=================================================================================================================================
Param([ref] $TestDetails = ([ref] $null))

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

        $TestSuiteName = "Test-OcsErrorCommands"

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
    #  Backup all errors before beginning test.  If fails abort test
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'Backup Error Logs with Check-WcsError'
    $TestID            = 'ID-ERR-0002'

    $Directory         =  "$FULL_TEST_DIRECTORY\Backup\Errors" 
    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

    #---------------------
    # Display test details
    #---------------------
    Write-Host " This test backs up the error logs before the test suite runs.
              `r The test runs Check-WcsError and verifies:

              `r   1) Command completes without error
              `r   2) Error logs backed up at:  $Directory 
   
              `r Full Command : Check-WcsError -LogDirectory $Directory`r`n`r"  

    #---------------------
    # Start the test
    #---------------------
    Try
    {
        #--------------------------------------------------------------------------------------
        # Run command being tested
        #--------------------------------------------------------------------------------------
        $ReturnedValue = Check-WcsError -LogDirectory $Directory -ErrorAction Stop  

        #--------------------------------------------------------------------------------------
        # Verify files were created in the directory
        #--------------------------------------------------------------------------------------
        If (-not (VerifyErrorFiles $Directory))
        {
             Throw "Expected files were not found at: $Directory" 
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
        Write-Host -BackgroundColor Red " Aborting $TestSuiteName because could not backup errors"
        Return 1
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Clear-WcsError: Inputs   = None  
    #                  Return   = 0 on success
    #
    #                  Function = Clears BMC SEL and Windows event logs
    #-------------------------------------------------------------------------------------------------------------------------------------
    CompatibilityTest-ClearErrorCommand -TestDetails $TestDetails -TestName 'Verify Clear-WcsError basic functionality'  -TestId 'ID-ERR-005' -TestCommand "Clear-WcsError"  -Directory "$FULL_TEST_DIRECTORY\ID-ERR-0005-Clear-WcsError" -ClearWcsError
    
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Pre-WcsTest:  Inputs   = ResultsDirectory
    #                Return   = 0 on success
    #
    #                Function = Clears BMC SEL and Windows event logs
    #                Function = Logs configuration and errors
    #-------------------------------------------------------------------------------------------------------------------------------------
    $LogPath = "$LOCAL_TEST_DIRECTORY\ID-ERR-0006-PreTest"  
    
    CompatibilityTest-ClearErrorCommand -TestDetails $TestDetails -TestName 'Verify Pre-WcsTest basic functionality'     -TestId 'ID-ERR-006' -TestCommand "Pre-WcsTest -Results $LogPath " -Directory  $LogPath  -PreWcsTest

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Get-WcsSel: Inputs   = HardwareError, RecordType, SensorType, Sensor  
    #              Return   = Array of SEL entries on success, 0 or null on error
    #
    #              Function = Read BMC SEL with input parameters to filter (HardwareError, RecordType, SensorType, Sensor)
    #              Function = Filter input takes array or single value
    #              Function = Returns same entries with decode or nodecode
    #-------------------------------------------------------------------------------------------------------------------------------------
    $Entries = CompatibilityTest-InjectErrors

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel basic functionality'           -TestId 'ID-ERR-0010' -TestCommand "Get-WcsSel"                          -ReturnArray -ReturnCode ($Entries.All - $Entries.HwEventOnly)
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only HW errors'                -TestId 'ID-ERR-0011' -TestCommand "Get-WcsSel -HardwareError"           -ReturnArray -ReturnCode $Entries.HwSelOnly 

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensor type 0xC'          -TestId 'ID-ERR-0012' -TestCommand "Get-WcsSel -SensorType 0xC"          -ReturnArray -ReturnCode $Entries.SensorType0C
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensor 87'                -TestId 'ID-ERR-0013' -TestCommand "Get-WcsSel -Sensor 0x87"             -ReturnArray -ReturnCode $Entries.Sensor87

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensor types array'       -TestId 'ID-ERR-0014' -TestCommand "Get-WcsSel -SensorType @(0xC,0x13)"  -ReturnArray -ReturnCode $Entries.SensorType0Cand13
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensors array'            -TestId 'ID-ERR-0015' -TestCommand "Get-WcsSel -Sensor @(0x87,0xA1)"     -ReturnArray -ReturnCode $Entries.Sensor87andA1

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensor types array of 1'  -TestId 'ID-ERR-0016' -TestCommand "Get-WcsSel -SensorType @(0xC )"      -ReturnArray -ReturnCode $Entries.SensorType0C
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel only sensors array of 1'       -TestId 'ID-ERR-0017' -TestCommand "Get-WcsSel -Sensor @(0x87 )"         -ReturnArray -ReturnCode $Entries.Sensor87

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel return null if empty'          -TestId 'ID-ERR-0018' -TestCommand "Get-WcsSel -Sensor 1"                -ReturnNull

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel with RecordType 2 and sensor 87'                    -TestId 'ID-ERR-0020' -TestCommand "Get-WcsSel -Sensor 0x87 -RecordType  0x2"  -ReturnArray -ReturnCode $Entries.Sensor87
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel with RecordType, SensorType, Sensor '               -TestId 'ID-ERR-0021' -TestCommand "Get-WcsSel -Sensor 0x87 -RecordType  0x2  -SensorType 0xC"  -ReturnArray -ReturnCode $Entries.Sensor87
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel with RecordType, SensorType, Sensor,HardwareError'  -TestId 'ID-ERR-0022' -TestCommand "Get-WcsSel -Sensor 0x87 -RecordType  0x2 -HardwareError -SensorType 0xC"  -ReturnArray -ReturnCode $Entries.Sensor87
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsSel with RecordType 20 and sensor 87'                   -TestId 'ID-ERR-0023' -TestCommand "Get-WcsSel -Sensor 0x87 -RecordType  0x20"  -ReturnNull

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Log-WcsSel: Inputs   = File, LogDirectory,SelEntries 
    #              Return   = 0 on success
    #
    #              Function = Log SEL to file and directory specified
    #              Function = Sets default file and directory if not specified
    #              Function = Logs SelEntries (Check decode and no decode)
    #              Function = Allows extensions .sel.log and .log and none
    #
    #              Function = Verifies SelEntries object type
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestSelEntries     = Get-WcsSel           -ErrorAction Stop

    $BadSelEntries      = 123
    $BadSelEntries2     = @( @{bad="1"}, @{bad="2"} )


    $MoveTo   = "$FULL_TEST_DIRECTORY\Log-WcsSel"

    Remove-Item -Path "$WCS_RESULTS_DIRECTORY\Log-WcsSel\*" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null 
          
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsSel basic functionality'              -TestId 'ID-ERR-0050' -TestCommand "Log-WcsSel -File SEL"                                         -File  "sel"    -SEL  -Move $MoveTo 
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsSel overwrites existing file'         -TestId 'ID-ERR-0051' -TestCommand "Log-WcsSel -File SEL2.sel.log"                                -File  "SEL2"   -SEL  -Move $MoveTo  -CheckOverWrite 
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsSel with SelEntries input'            -TestId 'ID-ERR-0052' -TestCommand "Log-WcsSel -File SEL3.log  -SelEntries `$TestSelEntries"      -File  "SEL3"   -SEL  -Move $MoveTo 

    $LogPath = "$FULL_TEST_DIRECTORY\ID-ERR-0054-Log-WcsSel"

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsSel with LogDirectory input'          -TestId 'ID-ERR-0054' -TestCommand "Log-WcsSel -File SEL4 -LogDirectory $LogPath" -File "SEL4.sel.log" -SingleFile  -Directory "$LogPath" -CheckOverWrite 

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsSel with invalid SelEntries'             -TestId 'ID-ERR-0055' -TestCommand "Log-WcsSel -File SELNo -SelEntries `$BadSelEntries" -MatchStrings "* aborted: SelEntries input not valid"  -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsSel with invalid SelEntries #2'          -TestId 'ID-ERR-0056' -TestCommand "Log-WcsSel -File SELNo -SelEntries `$BadSelEntries2" -MatchStrings "* aborted: SelEntries input not valid" -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Check-WcsError: Inputs   = LogDirectory,IncludeSelFile,ExcludeSelFile,IncludeEventFile,IncludeEventFile 
    #                  Return   = Number of errors found
    #
    #                  Function = Exclude or include BMC SEL and/or Windows System Events as errors
    #                  Function = Log errors in specified location
    #                  Function = Sets default directory if not specified 
    #
    #                  Function = Reports IncludeSelFile missing and returns 1
    #                  Function = Reports ExcludeSelFile missing and returns 1
    #                  Function = Reports IncludeEventFile missing and returns 1
    #                  Function = Reports IncludeEventFile missing and returns 1
    #-------------------------------------------------------------------------------------------------------------------------------------    
    $MoveTo = "$FULL_TEST_DIRECTORY\ID-ERR-0100-Check-WcsError"    

    Remove-Item -Path "$WCS_RESULTS_DIRECTORY\Check-WcsError\*" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError basic functionality'  -TestId 'ID-ERR-0100' -TestCommand "Check-WcsError"  -ReturnCode ($Entries.All -1)  -CheckWcsError -Move $MoveTo 

    $Directory = "$FULL_TEST_DIRECTORY\ID-ERR-0101-Check-WcsError"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError exclude BMC SEL'      -TestId 'ID-ERR-0101' -TestCommand "Check-WcsError -LogDirectory $Directory -excludeSelFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeAllSel.xml" -ReturnCode $Entries.HwEventOnly  -Directory $Directory -CheckWcsError

    $Directory = "$FULL_TEST_DIRECTORY\ID-ERR-0102-Check-WcsError"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError exclude all Events'   -TestId 'ID-ERR-0102' -TestCommand "Check-WcsError -LogDirectory $Directory -excludeEventFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeAllEvents.xml" -ReturnCode $Entries.HwSelOnly  -Directory $Directory -CheckWcsError

    $Directory = "$FULL_TEST_DIRECTORY\ID-ERR-0103-Check-WcsError"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError exclude WHEA'         -TestId 'ID-ERR-0103' -TestCommand "Check-WcsError -LogDirectory $Directory -excludeEventFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeWheaEvents.xml" -ReturnCode $Entries.HwNoWHEA -Directory $Directory -CheckWcsError

    $Directory = "$FULL_TEST_DIRECTORY\ID-ERR-0104-Check-WcsError"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError include SEL'          -TestId 'ID-ERR-0104' -TestCommand "Check-WcsError -LogDirectory $Directory -includeSelFile $WCS_REF_DIRECTORY\ErrorFiles\IncludeAllSel.xml" -ReturnCode $Entries.All -Directory $Directory -CheckWcsError

    $Directory = "$FULL_TEST_DIRECTORY\ID-ERR-0105-Check-WcsError"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Check-WcsError include Boot Events'  -TestId 'ID-ERR-0105' -TestCommand "Check-WcsError -LogDirectory $Directory -includeEventFile $WCS_REF_DIRECTORY\ErrorFiles\IncludeOsBootEvent.xml" -ReturnCode $Entries.HwAddBoot -Directory $Directory -CheckWcsError
     
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Check-WcsError with missing include SEL file'   -TestId 'ID-ERR-0106' -TestCommand "Check-WcsError -includeSelFile NoFile.xml"    -MatchStrings "* aborted: Could not open IncludeSelFile *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Check-WcsError with missing exclude SEL file'   -TestId 'ID-ERR-0107' -TestCommand "Check-WcsError -excludeSelFile NoFile.xml"    -MatchStrings "* aborted: Could not open ExcludeSelFile *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Check-WcsError with missing include event file' -TestId 'ID-ERR-0108' -TestCommand "Check-WcsError -includeEventFile NoFile.xml"  -MatchStrings "* aborted: Could not open IncludeEventFile *" -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Check-WcsError with missing exclude event file' -TestId 'ID-ERR-0109' -TestCommand "Check-WcsError -excludeEventFile NoFile.xml"  -MatchStrings "* aborted: Could not open ExcludeEventFile *" -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Post-WcsTest:   Inputs   = ResultsDirectory,IncludeSelFile,ExcludeSelFile,IncludeEventFile,ExcludeEventFile 
    #                  Return   = Number of errors found
    #
    #                  Function = Exclude or include BMC SEL and/or Windows System Events as errors
    #                  Function = Log errors in specified location
    #                  Function = Log configuration in specified location
    #
    #                  Function = Reports IncludeSelFile missing and returns 1
    #                  Function = Reports ExcludeSelFile missing and returns 1
    #                  Function = Reports IncludeEventFile missing and returns 1
    #                  Function = Reports IncludeEventFile missing and returns 1
    #-------------------------------------------------------------------------------------------------------------------------------------    
    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0150-Post-WcsTest"  

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest basic functionality'  -TestId 'ID-ERR-0150' -TestCommand "Post-WcsTest  -ResultsDirectory $Directory"  -ReturnCode ($Entries.All -1)  -Directory $Directory -PostWcsTest

    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0151-Post-WcsTest"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest exclude BMC SEL'      -TestId 'ID-ERR-0151' -TestCommand "Post-WcsTest -ResultsDirectory $Directory -excludeSelFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeAllSel.xml" -ReturnCode $Entries.HwEventOnly  -Directory $Directory  -PostWcsTest

    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0152-Post-WcsTest"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest exclude all Events'   -TestId 'ID-ERR-0152' -TestCommand "Post-WcsTest -ResultsDirectory $Directory -excludeEventFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeAllEvents.xml" -ReturnCode $Entries.HwSelOnly  -Directory $Directory  -PostWcsTest

    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0153-Post-WcsTest"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest exclude WHEA'         -TestId 'ID-ERR-0153' -TestCommand "Post-WcsTest -ResultsDirectory $Directory -excludeEventFile $WCS_REF_DIRECTORY\ErrorFiles\ExcludeWheaEvents.xml" -ReturnCode $Entries.HwNoWHEA -Directory $Directory  -PostWcsTest

    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0154-Post-WcsTest"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest include SEL'          -TestId 'ID-ERR-0154' -TestCommand "Post-WcsTest -ResultsDirectory $Directory -includeSelFile $WCS_REF_DIRECTORY\ErrorFiles\IncludeAllSel.xml" -ReturnCode $Entries.All -Directory $Directory  -PostWcsTest

    $Directory = "$LOCAL_TEST_DIRECTORY\ID-ERR-0155-Post-WcsTest"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Post-WcsTest include Boot Events'  -TestId 'ID-ERR-0155' -TestCommand "Post-WcsTest -ResultsDirectory $Directory -includeEventFile $WCS_REF_DIRECTORY\ErrorFiles\IncludeOsBootEvent.xml" -ReturnCode $Entries.HwAddBoot -Directory $Directory  -PostWcsTest

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Post-WcsTest with missing include SEL file'   -TestId 'ID-ERR-0156' -TestCommand "Post-WcsTest -includeSelFile NoFile.xml -ResultsDirectory $Directory"    -MatchStrings "* aborted: Could not open IncludeSelFile *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Post-WcsTest with missing exclude SEL file'   -TestId 'ID-ERR-0157' -TestCommand "Post-WcsTest -excludeSelFile NoFile.xml -ResultsDirectory $Directory"    -MatchStrings "* aborted: Could not open ExcludeSelFile *"   -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Post-WcsTest with missing include event file' -TestId 'ID-ERR-0158' -TestCommand "Post-WcsTest -includeEventFile NoFile.xml -ResultsDirectory $Directory"  -MatchStrings "* aborted: Could not open IncludeEventFile *" -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Post-WcsTest with missing exclude event file' -TestId 'ID-ERR-0159' -TestCommand "Post-WcsTest -excludeEventFile NoFile.xml -ResultsDirectory $Directory"  -MatchStrings "* aborted: Could not open ExcludeEventFile *" -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Get-WcsHealth: Inputs   =  NoDeviceMgr, NoHardware, NoFru
    #                 Return   =  Hash table with objects
    #
    #                 Function =  Identifies errors in the FRU
    #                 Function =  Identifies errors in Device Manager (To Do: how to inject?)
    #                 Function =  Identifies errors in the SEL
    #-------------------------------------------------------------------------------------------------------------------------------------

    # Read in Dev Mgr errors if not WinPE

    If (-NOT (CoreLib_IsWinPE))
    {
        $DeviceMgrErrors =  (Get-WmiObject Win32_PnpEntity | Where-Object {$_.ConfigManagerErrorCode -ne 0}).Count
    }
    Else
    {
        $DeviceMgrErrors =  0

    }

    $FruErrors       = 0

    $ExpectedReturnValue = @{ErrorCount=($Entries.HwSelOnly+$FruErrors+$DeviceMgrErrors);FruCount=$FruErrors ;HwCount=$Entries.HwSelOnly;DevMgrCount=$DeviceMgrErrors }

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth basic functionality'           -TestId 'ID-ERR-0200' -TestCommand "Get-WcsHealth"  -ReturnHealth  $ExpectedReturnValue 

    $ExpectedReturnValue = @{ErrorCount=($Entries.HwSelOnly+$DeviceMgrErrors);FruCount=0 ;HwCount=$Entries.HwSelOnly;DevMgrCount=$DeviceMgrErrors }

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth with NoFru'                    -TestId 'ID-ERR-0201' -TestCommand "Get-WcsHealth -NoFru"  -ReturnHealth $ExpectedReturnValue

    $ExpectedReturnValue = @{ErrorCount=($FruErrors+$DeviceMgrErrors);FruCount=$FruErrors ;HwCount=0;DevMgrCount=$DeviceMgrErrors }

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth with NoHardware'               -TestId 'ID-ERR-0202' -TestCommand "Get-WcsHealth -NoHardware"  -ReturnHealth $ExpectedReturnValue

    $ExpectedReturnValue = @{ErrorCount=($Entries.HwSelOnly+$FruErrors);FruCount=$FruErrors ;HwCount=$Entries.HwSelOnly;DevMgrCount=0}

    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth with NoDeviceMgr'               -TestId 'ID-ERR-0203' -TestCommand "Get-WcsHealth -NoDeviceMgr"  -ReturnHealth $ExpectedReturnValue

    # Inject 5 FRU errors and make sure they are all caught
    #-------------------------------------------------------
    Try
    {
            If (0 -ne (Log-WcsFru -File FruBackup -LogDirectory "$FULL_TEST_DIRECTORY\Backup"))
            {
                Throw " SETUP ERROR:  Failed to backup the FRU for tests "
            }

            $OriginalFruData = Get-WcsFruData  -ErrorAction Stop

            If (0 -ne (Update-WcsFru -AssetTag "N/A" -SerialNumber "N/A" -MBSerialNumber "N/A" -ChassisSerialNumber "N/A" -BuildVersion "N/A" -ErrorAction Stop )) 
            { 
                Throw " SETUP ERROR: Failed to update FRU data for tests" 
            }

            $FruErrors = 5

            $ExpectedReturnValue = @{ErrorCount=($Entries.HwSelOnly+$DeviceMgrErrors);FruCount=0 ;HwCount=$Entries.HwSelOnly;DevMgrCount=$DeviceMgrErrors }

            CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth with NoFru'                    -TestId 'ID-ERR-0204' -TestCommand "Get-WcsHealth -NoFru"  -ReturnHealth $ExpectedReturnValue

            $ExpectedReturnValue = @{ErrorCount=($FruErrors+$DeviceMgrErrors);FruCount=$FruErrors ;HwCount=0;DevMgrCount=$DeviceMgrErrors }

            CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Get-WcsHealth with NoHardware'               -TestId 'ID-ERR-0205' -TestCommand "Get-WcsHealth -NoHardware"  -ReturnHealth $ExpectedReturnValue
    
            $ReturnedValue = Update-WcsFruData -DataToWrite $OriginalFruData -FruOffset 0 -ErrorAction Stop

            If (($null -eq $ReturnedValue) -or (0 -eq $ReturnedValue))
            { 
                Throw " SETUP ERROR: Failed to restore FRU data after tests"
            }

    }
    Catch
    {
        Write-Host  "`r`n $_`r"   -BackgroundColor $TKT_HEADER_BACKGROUND_FAIL_COLOR  
        #--------------------------------------------------------------------------------------
        # Increment fail counter
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests++
    }


    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Log-WcsHealth: Inputs   =   NoDeviceMgr, NoHardware, NoFru, File, LogDirectory, SystemHealth
    #                 Return   =   0 on success, non-zero on error
    #
    #
    #              Function = Log health to file and directory specified
    #              Function = Sets default file and directory if not specified
    #              Function = Verifies SystemHealth object type
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestHealthEntries     = Get-WcsHealth           -ErrorAction Stop
    $BadHealth             = 123

    Remove-Item -Path "$WCS_RESULTS_DIRECTORY\Log-WcsHealth\*" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null          

    $MoveTo   = "$FULL_TEST_DIRECTORY\ID-ERR-0020-Log-WcsHealth"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsHealth basic functionality'                 -TestId 'ID-ERR-0250' -TestCommand "Log-WcsHealth"      -Health  -Move $MoveTo 
    
    $MoveTo   = "$FULL_TEST_DIRECTORY\ID-ERR-0021-Log-WcsHealth" 
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsHealth overwrites existing file'            -TestId 'ID-ERR-0251' -TestCommand "Log-WcsHealth -File Health2.Health.log"  -File  "Health2.Health.Log"  -Directory "$WCS_RESULTS_DIRECTORY\Log-WcsHealth" -Move $MoveTo  -CheckOverWrite 
    
    $Directory =   "$FULL_TEST_DIRECTORY\ID-ERR-0022\Log-WcsHealth"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsHealth with NoDeviceMgr, NoFru, NoHardware' -TestId 'ID-ERR-0252' -TestCommand "Log-WcsHealth -File Health3 -LogDirectory $Directory"  -File  "Health3.Health.Log"  -Directory $Directory  -CheckOverWrite 

    $Directory =   "$FULL_TEST_DIRECTORY\ID-ERR-0023\Log-WcsHealth"
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsHealth with SystemHealth'                   -TestId 'ID-ERR-0253' -TestCommand "Log-WcsHealth -File Health4 -LogDirectory $Directory -SystemHealth `$TestHealthEntries"  -File  "Health4.Health.Log"  -Directory $Directory 
 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsHealth with invalid SystemHealth'              -TestId 'ID-ERR-0254' -TestCommand "Log-WcsHealth -File NoFile -SystemHealth `$BadHealth" -MatchStrings "* aborted: SystemHealth input not valid" -MatchLines 1  -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  View-WcsHealth: Inputs   =   NoDeviceMgr, NoHardware, NoFru, Full
    #                  Return   =   Null
    #
    #                  Function =   Dispalys system health
    #-------------------------------------------------------------------------------------------------------------------------------------
    Clear-WcsSel
    $Entries = CompatibilityTest-InjectErrors  
        
    # Cannot test in WinPE because device manager errors will cause fails

    If (-NOT (CoreLib_IsWinPE))
    {
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHealth'                    -TestId 'ID-ERR-0300' -TestCommand 'View-WcsHealth'              -FileName 'View-WcsHealth'                -ViewHealth
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHealth with NoHardware'    -TestId 'ID-ERR-0301' -TestCommand 'View-WcsHealth -NoHardware'  -FileName 'View-WcsHealth-NoHardware'     -ViewHealth
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHealth with NoFru'         -TestId 'ID-ERR-0302' -TestCommand 'View-WcsHealth -NoFru'       -FileName 'View-WcsHealth-NoFru'           -ViewHealth

        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify View-WcsHealth with Full'       -TestId 'ID-ERR-0303' -TestCommand 'View-WcsHealth -Full'    -ReturnNull
    }

    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHealth with NoDeviceMgr'   -TestId 'ID-ERR-0304' -TestCommand 'View-WcsHealth -NoDeviceMgr' -FileName 'View-WcsHealth-NoDeviceMgr'  -CommonRef  -ViewHealth

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Check-WcsDiagnostics: Inputs   =  None
    #                        Return   =  
    #
    #                        Function = Reports configuration mismatch
    #                        Function = Reports health error
    #                        Function = Identifies memory and disk errors with different return codes
    #-------------------------------------------------------------------------------------------------------------------------------------    
    Try
    {
        If (0 -ne (Log-WcsConfig -File RecipeFile))
        {
            Throw " SETUP ERROR: Could not log recipe file for tests "
        }

        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic basic functionality'       -TestId 'ID-ERR-0350' -TestCommand "Check-WcsDiagnostics -RecipeFile RecipeFile"      -ReturnCode 0x70

    
        Clear-WcsSel
        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic no errors'                 -TestId 'ID-ERR-0351' -TestCommand "Check-WcsDiagnostics -RecipeFile RecipeFile"      -ReturnCode  0

        #Inject memory error

        $IpmiData = Invoke-WcsIpmi  0x44 @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA0,0,0) $WCS_STORAGE_NETFN 
        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic with DIMM error'           -TestId 'ID-ERR-0352' -TestCommand "Check-WcsDiagnostics -RecipeFile RecipeFile"      -ReturnCode 0x30

        #Inject processor error

        Clear-WcsSel
        $IpmiData = Invoke-WcsIpmi  0x44 @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xD5,0x6F, 0x0,0xFF,0xFF) $WCS_STORAGE_NETFN 
        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic with other HW error'       -TestId 'ID-ERR-0353' -TestCommand "Check-WcsDiagnostics -RecipeFile RecipeFile"      -ReturnCode 0x10

        Clear-WcsSel

        $BadRecipe = Get-WcsConfig -File RecipeFile
        $BadRecipe.WcsConfig.BIOS.Version.Value = "Wrong Version"

        If (0 -ne (Log-WcsConfig -File BadRecipe -Config $BadRecipe))
        {
            Throw " SETUP ERROR: Could not log recipe file for tests "
        }

        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic with other config error'   -TestId 'ID-ERR-0354' -TestCommand "Check-WcsDiagnostics -RecipeFile BadRecipe"      -ReturnCode 1


        # Add a disk to recipe file

        $ConfigFile  = "$WCS_CONFIGURATION_DIRECTORY\ExtraDisk.config.xml" 

        $ExtraDisk = '
      <Disk CompareResult="NONE" Display="True">
        <Location Value="\\.\PHYSICALDRIVE99" CompareType="Always " CompareResult="NONE" Display="True" />
        <Manufacturer Value="SAMSUNG MZ7WD480HCGM-00003" CompareType="Always " CompareResult="NONE" Display="True" />
        <Model Value="SAMSUNG MZ7WD480HCGM-00003" CompareType="Always " CompareResult="NONE" Display="True" />
        <Size Value="480101368320" CompareType="Percent" CompareResult="NONE" Display="True" />
        <SizeString Value="480.1 GB" CompareType="Never  " CompareResult="NONE" Display="True" />
        <Firmware Value="DXM9103Q" CompareType="Always " CompareResult="NONE" Display="True" />
        <LabelLocation Value="SB-SSD99" CompareType="Always " CompareResult="NONE" Display="True" />
      </Disk>
    </WcsConfig>
        '

        Remove-Item $ConfigFile  -Force -ErrorAction SilentlyContinue | Out-Null

        Get-Content -Path "$WCS_CONFIGURATION_DIRECTORY\RecipeFile.config.xml" | Where-Object {$_ -ne $null} | ForEach-Object { 

            If ($_ -eq '</WcsConfig>')
            {
                Add-Content -Path   $ConfigFile  -Value $ExtraDisk
            }
            Else
            {
                Add-Content -Path   $ConfigFile  -Value $_
            }
        }

        CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic with missing disk error'   -TestId 'ID-ERR-0355' -TestCommand "Check-WcsDiagnostics -RecipeFile ExtraDisk"      -ReturnCode 5


        # Add a disk to recipe file

        $ConfigFile  = "$WCS_CONFIGURATION_DIRECTORY\ExtraDimm.config.xml" 

        $ExtraDisk = '
      <DIMM CompareResult="NONE" Display="True">
        <Location Value="DIMM_Z1" CompareType="Always " CompareResult="NONE" Display="True" />
        <Manufacturer Value="Samsung" CompareType="Always " CompareResult="NONE" Display="True" />
        <Model Value="M393B2G70QH0-YK0" CompareType="Always " CompareResult="NONE" Display="True" />
        <Speed Value="1600" CompareType="Always " CompareResult="NONE" Display="True" />
        <SizeString Value="16.0 GiB" CompareType="Never  " CompareResult="NONE" Display="True" />
        <LabelLocation Value="DIMM Z1" CompareType="Always " CompareResult="NONE" Display="True" />
      </DIMM>
    </WcsConfig>
        '

        Remove-Item $ConfigFile  -Force -ErrorAction SilentlyContinue | Out-Null

        Get-Content -Path "$WCS_CONFIGURATION_DIRECTORY\RecipeFile.config.xml" | Where-Object {$_ -ne $null} | ForEach-Object { 

            If ($_ -eq '</WcsConfig>')
            {
                Add-Content -Path   $ConfigFile  -Value $ExtraDisk
            }
            Else
            {
                Add-Content -Path   $ConfigFile  -Value $_
            }
        }
    }
    Catch
    {
        Write-Host  "`r`n $_`r"   -BackgroundColor $TKT_HEADER_BACKGROUND_FAIL_COLOR  
        #--------------------------------------------------------------------------------------
        # Increment fail counter
        #--------------------------------------------------------------------------------------  
        $TestDetails.Value.FailTests++
    }
    CompatibilityTest-SimpleCommand    -TestDetails $TestDetails -TestName 'Verify Check-WcsDiagnostic with missing dimm error'   -TestId 'ID-ERR-0356' -TestCommand "Check-WcsDiagnostics -RecipeFile ExtraDimm"      -ReturnCode 3


    #-------------------------------------------------------------------------------------------------------------------------------------
    #  View-WcsSel: Inputs   = NoDecode, HardwareError, RecordType, SensorType, Sensor  
    #               Return   = null
    #
    #               Function = Displays BMC SEL with input parameters to filter
    #               Function = Filter input take array or single value
    #               Function = Decode or no decode 
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  - Note PCIe errors have OEM codes so each OEM will be different
    #-------------------------------------------------------------------------------------------------------------------------------------
    Clear-WcsSel

    CompatibilityTest-InjectErrors  -NoPcie | Out-Null

    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel'                    -TestId 'ID-ERR-0400' -TestCommand 'View-WcsSel'                         -FileName 'View-WcsSel'                -ViewSel          -CommonRef
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel with HardwareError' -TestId 'ID-ERR-0401' -TestCommand 'View-WcsSel -Hardware'               -FileName 'View-WcsSel-Hardware'       -ViewSel          -CommonRef
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel with RecordType'    -TestId 'ID-ERR-0402' -TestCommand 'View-WcsSel -RecordType 0x2'         -FileName 'View-WcsSel-RecordType'     -ViewSel          -CommonRef
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel with SensorType'    -TestId 'ID-ERR-0403' -TestCommand 'View-WcsSel -SensorType (0xC,0x99)'  -FileName 'View-WcsSel-SensorType'     -ViewSel          -CommonRef
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel with Sensor'        -TestId 'ID-ERR-0404' -TestCommand 'View-WcsSel -Sensor @(0x87,0x99)'    -FileName 'View-WcsSel-Sensor'         -ViewSel          -CommonRef

    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsSel with NoDecode'      -TestId 'ID-ERR-0405' -TestCommand 'View-WcsSel -NoDecode'               -FileName 'View-WcsSel-Nodecode'       -ViewSelNoDecode  

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  - Note Clear all errors after testing done
    #-------------------------------------------------------------------------------------------------------------------------------------
    CompatibilityTest-ClearErrorCommand -TestDetails $TestDetails -TestName 'Clear all injected errors'  -TestId 'ID-ERR-0500'  -TestCommand "Clear-WcsError"  -Directory "$FULL_TEST_DIRECTORY\ID-ERR-0100-Clear-WcsError"  -ClearWcsError

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
    Return $WCS_RETURN_CODE_UNKNOWN_ERROR
}
