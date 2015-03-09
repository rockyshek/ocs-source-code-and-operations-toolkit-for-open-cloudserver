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
# This script runs a suite of tests that verify Toolkit FRU command functionality.
#
#
# DEPENDENCIES:  1) Only runs on a WCS Compute blade with IPMI FRU
#                2) Blade FRU content (fields) must comply with the latest WCS Compute Blade FRU specification
#                3) Reference files for the blade being tests.  Refer to Compatibility Test Setup for instructions on
#                   how to create these files
#
# WARNING:       THIS SCRIPT WILL READ AND WRITE THE FRU, ABORTING THE TEST IN THE MIDDLE MAY CORRUPT THE FRU
#                THE ORIGINAL FRU DATA IS SAVED BEFORE THE TEST BEGINS SO IT CAN BE RESTORED
#=================================================================================================================================
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

        $TestSuiteName = "Test-OcsFruCommands"

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

    #----------------------------------------------------
    # Setup variables used throughout this suite
    #----------------------------------------------------
    $BackupFruFile           = 'FruBackup'
    $BackupDirectory         = ("{0}\Backup\FRU" -f $TestDetails.Value.TestDirectory)

    #-------------------------------------------------------------------------------------------------------------------------------------
    # PRETEST - Backup the FRU 
    #-------------------------------------------------------------------------------------------------------------------------------------
    $TestStartTime     = Get-Date

    $TestName          = 'TEST SETUP - Backup Fru'
    $TestID            = 'ID-FRU-0001'

    $TestLogFile       = "$BackupDirectory\$BackupFruFile.fru.log" 
    #--------------------
    # Display test header
    #--------------------
    CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++) -TestName $TestName 

    #---------------------
    # Display test details
    #---------------------
    Write-Host " 
 This test backs up the FRU before testing begins.
    
 FRU backup:    $TestLogFile 
 `r"
    #---------------------
    # Start the test
    #---------------------
    Try
    {
        #--------------------------------------------------------------------------------------
        # Run command being tested
        #--------------------------------------------------------------------------------------
        $ReturnedValue = Log-WcsFru -File $BackupFruFile -LogDirectory $BackupDirectory -ErrorAction Stop  

        #--------------------------------------------------------------------------------------
        # Verify return value matches expected
        #--------------------------------------------------------------------------------------
        If (0 -ne  $ReturnedValue)
        {
            If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
            Else                          { Throw "Return value incorrect.  Expected 0 but received $ReturnedValue" }
        }
        #--------------------------------------------------------------------------------------
        # Verify log files created 
        #--------------------------------------------------------------------------------------  
        If (-not (Test-Path $TestLogFile))
        {
            Throw ("Log file not found: {0}" -f $TestLogFile)
        }
        Else
        {
            Write-Host " Found Log File: $TestLogFile `r`n`r"
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
        # If could not backup FRU then abort test
        #--------------------------------------------------------------------------------------  
        Write-Host -BackgroundColor Red " Aborting $SCRIPT_FILE because could not backup FRU`r"
        Return 1
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Get-WcsFruData: Inputs   = FruOffset, NumberOfBytes, DeviceId, File, LogDirectory
    #                  Return   = On success an array of bytes, on error $null
    #
    #                  Function = Returns entire FRU if FruOffset and NumberOfBytes not specified
    #                  Function = Returns only FRU specified with FRUOffset and NumberOfBytes
    #                  Function = If NumberOfBytes not specified returns data from FRUOffset to end of FRU
    #
    #                  Function = Reads current FRU data if no file specified
    #
    #                  Function = Gets FRU data from file and directory specified
    #                  Function = Gets from default directory if not specified
    #                  Function = Verifies FRU object type from file
    #                  Function = Accepts file with no extension, .log extension, and .fru.log extension
    #
    #                  Function = Reports error if FruOffset/NumberOfBytes greater then FRU size
    #                  Function = Reports File missing and returns null when file+path incorrect
    #-------------------------------------------------------------------------------------------------------------------------------------

    # Save the original FRU data for comparison and restoration later in the script
    #------------------------------------------------------------------------------
    Try
    {
        $OriginalFruData = Get-WcsFruData -ErrorAction Stop
    }
    Catch
    {
        Throw "TEST SETUP ERROR:  Could not read initial FRU data.  Aborting test"
    }

    # Verify FRU data read from local system matches the original data
    #-------------------------------------------------------------------
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData basic functionality'            -TestId  'ID-FRU-0010'                                                        -MatchFruBytes  $OriginalFruData  
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData with FruOffset/NumberOfBytes'   -TestId  'ID-FRU-0011' -Offset 0   -NumberOfBytes  $OriginalFruData.Count     -MatchFruBytes  $OriginalFruData  
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial read'                   -TestId  'ID-FRU-0012' -Offset 32  -NumberOfBytes 1                           -MatchFruBytes  $OriginalFruData[32..33]
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial read #2'                -TestId  'ID-FRU-0013' -Offset 32  -NumberOfBytes ($OriginalFruData.Count-32) -MatchFruBytes  $OriginalFruData[32..($OriginalFruData.Count-1)]
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial read no NumberOfBytes'  -TestId  'ID-FRU-0014' -Offset 32                                             -MatchFruBytes  $OriginalFruData[32..($OriginalFruData.Count-1)]
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial read no FruOffset'      -TestId  'ID-FRU-0015'             -NumberOfBytes 16                          -MatchFruBytes  $OriginalFruData[0..15]

    # Verify FRU data read from file matches the original data
    #-------------------------------------------------------------------     
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData read from file'                      -TestId  'ID-FRU-0020'                                 -MatchFruBytes  $OriginalFruData                                  -File $BackupFruFile           -LogDirectory $BackupDirectory
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial read from file'              -TestId  'ID-FRU-0021' -Offset 32  -NumberOfBytes 1    -MatchFruBytes  $OriginalFruData[32..33]                          -File "$BackupFruFile.log"     -LogDirectory $BackupDirectory
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial file read no NumberOfBytes'  -TestId  'ID-FRU-0022' -Offset 32                      -MatchFruBytes  $OriginalFruData[32..($OriginalFruData.Count-1)]  -File "$BackupFruFile.fru.log" -LogDirectory $BackupDirectory
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData partial file read no FruOffset'      -TestId  'ID-FRU-0023'             -NumberOfBytes 16   -MatchFruBytes  $OriginalFruData[0..15]                           -File $BackupFruFile           -LogDirectory $BackupDirectory
   
    # Verify error displayed if try and read above the FRU size
    #----------------------------------------------------------
    $Offset            = $OriginalFruData.Count + 1 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Get-WcsFruData with FruOffset too high'                -TestId 'ID-FRU-0030' -TestCommand "Get-WcsFruData -FruOffset $Offset "  -MatchStrings "* aborted: FruOffset * exceeds the FRU size *"   -MatchLines 1  -ReturnNull
    
    $Offset            = $OriginalFruData.Count + 1 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Get-WcsFruData file read with FruOffset too high'      -TestId 'ID-FRU-0031' -TestCommand "Get-WcsFruData -FruOffset $Offset -File $BackupFruFile -LogDirectory $BackupDirectory "  -MatchStrings "* aborted: FruOffset * exceeds the FRU size *"   -MatchLines 1  -ReturnNull
 
    $Offset            = $OriginalFruData.Count - 32
    $NumberOfBytes     = 33
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Get-WcsFruData with FruOffset+NumberOfBytes too high'  -TestId 'ID-FRU-0032' -TestCommand "Get-WcsFruData -FruOffset $Offset -NumberOfBytes $NumberOfBytes"  -MatchStrings "* aborted: FruOffset * exceeds the FRU size *"   -MatchLines 1  -ReturnNull

    # Verify error displayed if FRU file has invalid FRU data
    #-----------------------------------------------------------
    $BadFile = "TempFile.fru.log"
    Set-Content -Path "$WCS_RESULTS_DIRECTORY\$BadFile"  -Value "No XML file" -ErrorAction Stop | Out-Null
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Get-WcsFruData identifies file has invalid FRU data'   -TestId 'ID-FRU-0033' -TestCommand "Get-WcsFruData -File $BadFile -LogDirectory $WCS_RESULTS_DIRECTORY"  -MatchStrings "* aborted: Fru not valid format"   -MatchLines 1  -ReturnNull
    Remove-Item  -Path "$WCS_RESULTS_DIRECTORY\$BadFile"  -Force -ErrorAction Stop | Out-Null 
 
    # Verify error displayed if file doesn't exist
    #--------------------------------------------- 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Get-WcsFruData identifies missing file'         -TestId 'ID-FRU-0034' -TestCommand "Get-WcsFruData -File NoFile" -MatchStrings "* aborted: Could not open FRU file *" -MatchLines 1 -ReturnNull

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Log-WcsFru: Inputs   = File, LogDirectory, DeviceID, Fru
    #              Return   = 0 on success, non-zero on failure
    #
    #             Function = Logs to default directory if not specified (file must be specified)
    #             Function = Accepts file with no extension, .log extension, and .fru.log extension
    #             Function = Log FRU to file and directory specified
    #             Function = Verifies FRU object type
    #             Function = Read local FRU if FRU not specified
    #-------------------------------------------------------------------------------------------------------------------------------------

    # Verify logs to default directory
    #-----------------------------------
    $DefaultDirectory = "$WCS_RESULTS_DIRECTORY\Log-WcsFru" 
    Remove-Item $DefaultDirectory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsFru basic functionality'                   -TestId 'ID-FRU-0100'  -TestCommand "Log-WcsFru -File DefaultTestFile"  -File "DefaultTestFile.fru.log" -Directory $DefaultDirectory
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Get-WcsFruData default directory'                     -TestId 'ID-FRU-0101' -MatchFruBytes  $OriginalFruData  
    Remove-Item $DefaultDirectory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null

    # Verify logs with different inputs
    #-----------------------------------
    $MoveTo = "$FULL_TEST_DIRECTORY\ID-FRU-0100-Log-WcsFru"

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsFru basic functionality'                   -TestId 'ID-FRU-0102'  -TestCommand "Log-WcsFru"    -Fru   -MoveTo $MoveTo
 
    $Directory = "$FULL_TEST_DIRECTORY\ID-FRU-0101-Log-WcsFru"

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsFru with LogDirectory and file overwrite'  -TestId 'ID-FRU-0103'  -TestCommand "Log-WcsFru -File FruWithExtension -LogDirectory $Directory" -File "FruWithExtension.fru.log" -Directory $Directory -SingleFile -CheckOverWrite
    
    $Directory = "$FULL_TEST_DIRECTORY\ID-FRU-0102-Log-WcsFru"    
    
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsFru with log extension'                    -TestId 'ID-FRU-0104'  -TestCommand "Log-WcsFru -File FruWithLogExtension.Log -LogDirectory $Directory" -File "FruWithLogExtension.fru.log" -Directory $Directory -SingleFile  

    $ReadFru = Get-WcsFru
    $Directory = "$FULL_TEST_DIRECTORY\ID-FRU-0103-Log-WcsFru"    
    
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsFru with FRU data input'                   -TestId 'ID-FRU-0105'  -TestCommand "Log-WcsFru -File FruDataInput.Log -LogDirectory $Directory -Fru `$ReadFru" -File "FruDataInput.fru.log" -Directory $Directory -SingleFile 

    # Verify error displayed when FRU object is not valid
    #----------------------------------------------------- 
    $BadFru = 123 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsFru with bad FRU input'                       -TestId 'ID-FRU-0106' -TestCommand "Log-WcsFru -Fru `$BadFru" -MatchStrings "* aborted: Fru not valid format*" -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Update-WcsFruData: Inputs   = FruOffset, DataToWrite 
    #                     Return   = On success number of bytes written, on error $null
    #
    #                     Function = Updates only FRU specified with FRUOffset and DataToWrite
    #
    #                     Function = Reports error if FruOffset/DataToWrite greater then FRU size
    #-------------------------------------------------------------------------------------------------------------------------------------
    CompatibilityTest-SimpleCommand      -TestDetails $TestDetails -TestName 'Verify Update-WcsFruData partial update'         -TestId 'ID-FRU-0150' -TestCommand  "Update-WcsFruData -FruOffset 16 -DataToWrite @(0x34,0x34)" -ReturnCode 2
    
    $BadFruData = $OriginalFruData.Clone()
    $BadFruData[16] = 0x34
    $BadFruData[17] = 0x34
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Verify partial update'                           -TestId  'ID-FRU-0150b'       -MatchFruBytes  $BadFruData  


    CompatibilityTest-SimpleCommand      -TestDetails $TestDetails -TestName 'Verify Update-WcsFruData partial update #2'      -TestId 'ID-FRU-0151' -TestCommand  "Update-WcsFruData -FruOffset 32 -DataToWrite @(0x22,0x44)" -ReturnCode 2
    $BadFruData = $OriginalFruData.Clone()
    $BadFruData[16] = 0x34
    $BadFruData[17] = 0x34
    $BadFruData[32] = 0x22
    $BadFruData[33] = 0x44
    CompatibilityTest-GetFruDataCommand  -TestDetails $TestDetails -TestName 'Verify partial update'                           -TestId  'ID-FRU-0151b'       -MatchFruBytes  $BadFruData  


    CompatibilityTest-SimpleCommand      -TestDetails $TestDetails -TestName 'Verify Update-WcsFruData basic functionality'    -TestId 'ID-FRU-0152' -TestCommand  "Update-WcsFruData -FruOffset 0 -DataToWrite `$OriginalFruData" -ReturnCode $OriginalFruData.Count

    # Verify error displayed if try and write above the FRU size
    #----------------------------------------------------------
    $Offset            = $OriginalFruData.Count + 1
    $NumberOfBytes     = 32
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Update-WcsFruData with FruOffset + DataToWrite too high' -TestId 'ID-FRU-0153' -TestCommand "Get-WcsFruData -FruOffset $Offset -NumberOfBytes $NumberOfBytes"   -MatchStrings "* aborted: FruOffset * exceeds the FRU size *"   -MatchLines 1  -ReturnNull

    $Offset            = $OriginalFruData.Count - 32
    $NumberOfBytes     = 33
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Update-WcsFruData with FruOffset + DataToWrite too high' -TestId 'ID-FRU-0154' -TestCommand "Get-WcsFruData -FruOffset $Offset -NumberOfBytes $NumberOfBytes"   -MatchStrings "* aborted: FruOffset * exceeds the FRU size *"   -MatchLines 1  -ReturnNull

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Get-WcsFru: Inputs   = File, LogDirectory, DeviceID
    #              Return   = XML object with FRU data on success, $null on failure
    #
    #             Function = Reads from default directory if not specified (file must be specified)
    #             Function = Accepts file with no extension, .log extension, and .fru.log extension
    #             Function = Read FRU from file and directory specified
    #             Function = Verifies FRU object type if read from file
    #             Function = Read local FRU if file not specified
    #
    #             Function = Reports File missing and returns null when file+path incorrect
    #-------------------------------------------------------------------------------------------------------------------------------------
    CompatibilityTest-GetFruCommand  -TestDetails $TestDetails -TestName 'Get-WcsFru basic functionality'           -TestId  'ID-FRU-0200' -MatchFruBytes  $OriginalFruData
    CompatibilityTest-GetFruCommand  -TestDetails $TestDetails -TestName 'Get-WcsFru with File and LogDirectory'    -TestId  'ID-FRU-0201' -MatchFruBytes  $OriginalFruData  -File $BackupFruFile           -LogDirectory $BackupDirectory
    CompatibilityTest-GetFruCommand  -TestDetails $TestDetails -TestName 'Get-WcsFru with .log extension'           -TestId  'ID-FRU-0202' -MatchFruBytes  $OriginalFruData  -File "$BackupFruFile.log"     -LogDirectory $BackupDirectory

    Log-WcsFru -File  "$BackupFruFile.fru.log" -ErrorAction Stop | Out-Null
    CompatibilityTest-GetFruCommand  -TestDetails $TestDetails -TestName 'Get-WcsFru default directory'       -TestId  'ID-FRU-0203' -MatchFruBytes  $OriginalFruData -File "$BackupFruFile.fru.log"
    Remove-Item $DefaultDirectory -Force -Recurse -ErrorAction SilentlyContinue | Out-Null

    # Verify error displayed if FRU file has invalid FRU data
    #-----------------------------------------------------------
    $BadFile = "TempFile.fru.log"
    Set-Content -Path "$WCS_RESULTS_DIRECTORY\$BadFile"   -Value "No XML file" -ErrorAction Stop | Out-Null
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Get-WcsFru identifies bad file'      -TestId 'ID-FRU-0204' -TestCommand "Get-WcsFru -File $BadFile -LogDirectory $WCS_RESULTS_DIRECTORY"  -MatchStrings "* aborted: Fru not valid format"   -MatchLines 1  -ReturnNull
    Remove-Item  -Path "$WCS_RESULTS_DIRECTORY\$BadFile"  -Force -ErrorAction Stop | Out-Null 

    # Verify error displayed if file doesn't exist
    #---------------------------------------------   
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Get-WcsFru with missing file' -TestId 'ID-CFG-0205' -TestCommand "Get-WcsFru -File NoFile "  -MatchStrings "* aborted: Could not open FRU file *" -MatchLines 1 -ReturnNull

    #----------------------------------------------------------------------------------------------------------------------------------------------------
    #  Update-WcsFru: Inputs   = TemplateFile, NoMerge, ChassisSerial, ChassisCustom1, ChassisCustom2, BoardSerial, PRoductAsset, ProductCustom1
    #                            ProductCustom2, PRoductCustom3
    #                 Return   = 0 on success, non-zero on error
    #
    #                Function = Updates FRU fields specified
    #                Function = Updates FRU with template file, merges with current fields 
    #                Function = Updates FRU with template file, DOES NOT merge with current fields if NoMerge specified
    #                Function = Updates FRU with template file, merges with current fields and specified inputs
    #                Function = Accepts file with no extension, .log extension, and .fru.log extension
    #
    #                Function = Reports File missing and returns null when file+path incorrect for template file
    #                Function = Reports warning if fields greater then maximum
    #---------------------------------------------------------------------------------------------------------------------------------------------------

    # Helper function for testing update-wcsfru 
    #--------------------------------------------
    Function MakeUpdateFruCommand($Fields,$Extension='')
    {
        Write-Output ("Update-WcsFru -ChassisSerial '{0}' -ChassisCustom1 '{1}' -ChassisCustom2 '{2}' -BoardSerial '{3}' -ProductSerial '{4}' -ProductAsset '{5}' -ProductCustom1 '{6}' -ProductCustom2 '{7}' -ProductCustom3 '{8}' -BoardMinutes {9} {10}" -f 
                            $Fields.ChassisSerial,$Fields.ChassisCustom1,$Fields.ChassisCustom2,  
                            $Fields.BoardSerial,   
                            $Fields.ProductSerial,$Fields.ProductAsset,$Fields.ProductCustom1,$Fields.ProductCustom2,$Fields.ProductCustom3,$Fields.BoardMinutes,$Extension)

    }
    # Must reset FRU to original contents before running the VIEW command tests
    #--------------------------------------------------------------------------
    Try
    {
        $BytesWritten =  Update-WcsFruData -FruOffset 0 -DataToWrite $OriginalFruData -ErrorAction Stop
        If ($BytesWritten -ne $OriginalFruData.Count) { Throw }
    }
    Catch
    {
        Write-Host -ForegroundColor Yellow "`r`nTEST SETUP ERROR: Could not reset FRU data to original values`r`n`r"
    }

    # Read the current FRU info
    #--------------------------------------------------------------------------
    Try
    {
        $CurrentFruInfo      = ipmilib_GetBmcFru  -ErrorAction Stop
        $TemplateFru         = DefinedSystem_GetFruInformation  -ErrorAction Stop
        $OriginalFruData     = Get-WcsFruData -ErrorAction Stop

        $CurrentTemplateFile = $TemplateFru.WcsFrudata.FruFileName.Value
    }
    Catch
    {
        Write-Host -ForegroundColor Yellow "`r`nTEST SETUP ERROR: Could not read FRU data for Update-WcsFru tests`r`n`r"
    }
    # Setup hash tables to use later in the test

    # First one is the fields in the template file for the blade
    #--------------------------------------------------------------------------
    $TemplateFields = @{  ChassisSerial  = $TemplateFru.WcsFruData.Chassis.ChassisSerial.Value;
                          ChassisCustom1 = $TemplateFru.WcsFruData.Chassis.ChassisCustom1.Value;
                          ChassisCustom2 = $TemplateFru.WcsFruData.Chassis.ChassisCustom2.Value;
                          BoardMinutes   = [int] $TemplateFru.WcsFruData.Board.BoardMinutes.Value;
                          BoardSerial    = $TemplateFru.WcsFruData.Board.BoardSerial.Value;
                          ProductSerial  = $TemplateFru.WcsFruData.Product.ProductSerial.Value;
                          ProductAsset   = $TemplateFru.WcsFruData.Product.ProductAsset.Value;
                          ProductCustom1 = $TemplateFru.WcsFruData.Product.ProductCustom1.Value;
                          ProductCustom2 = $TemplateFru.WcsFruData.Product.ProductCustom2.Value;
                          ProductCustom3 = $TemplateFru.WcsFruData.Product.ProductCustom3.Value;
                     }

    # Orignal fields for compare and restoration
    #-------------------------------------------------------      
    $OriginalFields = @{  ChassisSerial  = $CurrentFruInfo.ChassisSerial.Value;
                          ChassisCustom1 = $CurrentFruInfo.ChassisCustom1.Value;
                          ChassisCustom2 = $CurrentFruInfo.ChassisCustom2.Value;
                          BoardMinutes    = [int] $CurrentFruInfo.BoardMinutes.Value;
                          BoardSerial    = $CurrentFruInfo.BoardSerial.Value;
                          ProductSerial  = $CurrentFruInfo.ProductSerial.Value;
                          ProductAsset   = $CurrentFruInfo.ProductAsset.Value;
                          ProductCustom1 = $CurrentFruInfo.ProductCustom1.Value;
                          ProductCustom2 = $CurrentFruInfo.ProductCustom2.Value;
                          ProductCustom3 = $CurrentFruInfo.ProductCustom3.Value;
                     }

    # Fields to be written
    #-------------------------------------------------------      
    $NewFields = @{    ChassisSerial  = 'Z'*$TemplateFru.WcsFruData.Chassis.ChassisSerial.MaxLength;
                       ChassisCustom1 = 'Z'*$TemplateFru.WcsFruData.Chassis.ChassisCustom1.MaxLength;
                       ChassisCustom2 = 'Z'*$TemplateFru.WcsFruData.Chassis.ChassisCustom2.MaxLength;
                       BoardMinutes   = 0;
                       BoardSerial    = 'Z'*$TemplateFru.WcsFruData.Board.BoardSerial.MaxLength;
                       ProductSerial  = 'Z'*$TemplateFru.WcsFruData.Product.ProductSerial.MaxLength;
                       ProductAsset   = 'Z'*$TemplateFru.WcsFruData.Product.ProductAsset.MaxLength;
                       ProductCustom1 = 'Z'*$TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength;
                       ProductCustom2 = 'Z'*$TemplateFru.WcsFruData.Product.ProductCustom2.MaxLength;
                       ProductCustom3 = 'Z'*$TemplateFru.WcsFruData.Product.ProductCustom3.MaxLength;
                     }

    # Fields are defined one byte longer then allowed
    #-------------------------------------------------------
    $LongFields = @{   ChassisSerial  = 'Z'*(([int] $TemplateFru.WcsFruData.Chassis.ChassisSerial.MaxLength)+1);
                       ChassisCustom1 = 'Z'*(([int] $TemplateFru.WcsFruData.Chassis.ChassisCustom1.MaxLength)+1);
                       ChassisCustom2 = 'Z'*(([int] $TemplateFru.WcsFruData.Chassis.ChassisCustom2.MaxLength)+1);
                       BoardSerial    = 'Z'*(([int] $TemplateFru.WcsFruData.Board.BoardSerial.MaxLength)+1);
                       BoardMinutes   = 0;
                       ProductSerial  = 'Z'*(([int] $TemplateFru.WcsFruData.Product.ProductSerial.MaxLength)+1);
                       ProductAsset   = 'Z'*(([int] $TemplateFru.WcsFruData.Product.ProductAsset.MaxLength)+1);
                       ProductCustom1 = 'Z'*(([int] $TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength)+1);
                       ProductCustom2 = 'Z'*(([int] $TemplateFru.WcsFruData.Product.ProductCustom2.MaxLength)+1);
                       ProductCustom3 = 'Z'*(([int] $TemplateFru.WcsFruData.Product.ProductCustom3.MaxLength)+1);
                     }

    # If one field padded then all padded
    #-------------------------------------
    If ($TemplateFru.WcsFruData.Chassis.ChassisSerial.Pad -eq 'True')
    {
        
        $PaddedFields = @{  ChassisSerial  = 'AA' + ' '*($TemplateFru.WcsFruData.Chassis.ChassisSerial.MaxLength-2);
                            ChassisCustom1 = 'AA' + ' '*($TemplateFru.WcsFruData.Chassis.ChassisCustom1.MaxLength-2);
                            ChassisCustom2 = 'AA' + ' '*($TemplateFru.WcsFruData.Chassis.ChassisCustom2.MaxLength-2);
                            BoardSerial    = 'AA' + ' '*($TemplateFru.WcsFruData.Board.BoardSerial.MaxLength-2);
                            BoardMinutes    = 0;
                            ProductSerial  = 'AA' + ' '*($TemplateFru.WcsFruData.Product.ProductSerial.MaxLength-2);
                            ProductAsset   = 'AA' + ' '*($TemplateFru.WcsFruData.Product.ProductAsset.MaxLength-2);
                            ProductCustom1 = 'AA' + ' '*($TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength-2);
                            ProductCustom2 = 'AA' + ' '*($TemplateFru.WcsFruData.Product.ProductCustom2.MaxLength-2);
                            ProductCustom3 = 'AA' + ' '*($TemplateFru.WcsFruData.Product.ProductCustom3.MaxLength-2);
                         }
        If ($TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength -eq 1) 
        { 
            $PaddedFields.ProductCustom1 =  'A' + ' '*($TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength-1);
        }

    }
    Else
    {
        $PaddedFields = @{  ChassisSerial  = 'AA';
                            ChassisCustom1 = 'AA';
                            ChassisCustom2 = 'AA';
                            BoardMinutes   = 0;
                            BoardSerial    = 'AA';
                            ProductSerial  = 'AA';
                            ProductAsset   = 'AA';
                            ProductCustom1 = 'AA';
                            ProductCustom2 = 'AA';
                            ProductCustom3 = 'AA';
                         }
        If ($TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength -eq 1) 
        { 
            $PaddedFields.ProductCustom1 = 'A'
        }

    }   
    
                            
    # Verify basic functionality
    #-----------------------------------------------------------
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru basic functionality'               -TestId  'ID-FRU-0300' -TestCommand (MakeUpdateFruCommand $NewFields) -Match $NewFields

    # Verify updating one field does not change others
    #-----------------------------------------------------------
    $NewFields.ChassisSerial = 'A'*$TemplateFru.WcsFruData.Chassis.ChassisSerial.MaxLength

    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru update only Chassis Serial Number' -TestId  'ID-FRU-0301' -TestCommand ('Update-WcsFru -ChassisSerial {0}' -f  $NewFields.ChassisSerial ) -Match $NewFields

    $NewFields.ChassisSerial = 'Z'*$TemplateFru.WcsFruData.Chassis.ChassisSerial.MaxLength

    # Reset fields.  Check multirecord area too
    #-----------------------------------------------------------
    $TestFruData = $OriginalFruData.Clone()

    If ($TemplateFru.WcsFruData.MultiRecord.Length -ne 0)
    {
        $TestOffset = (8*$OriginalFruData[5] + 64)
        If (0x55 -eq $OriginalFruData[$TestOffset]) { $TestFruData[$TestOffset]  = 0xAA}
        Else                                        { $TestFruData[$TestOffset]  = 0x55}
        Try
        {    
            $BytesWritten = Update-WcsFruData -FruOffset $TestOffset -DataToWrite $TestFruData[$TestOffset] -ErrorAction Stop        
            If ($BytesWritten -ne 1) { Throw }
        }
        Catch
        {
            Write-Host -ForegroundColor Yellow "`r`nTEST SETUP ERROR: Could not update FRU multirecord data`r`n`r"
        }
    }
                      
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru restore original fields'       -TestId  'ID-FRU-0302'    -TestCommand (MakeUpdateFruCommand $OriginalFields) -Match $OriginalFields
    CompatibilityTest-GetFruDataCommand -TestDetails $TestDetails -TestName 'Verify all FRU bytes after fields restored'  -TestId  'ID-FRU-0303'    -MatchFruBytes  $TestFruData 

    # Verify short fields are padded with spaces
    #-------------------------------------------
    If ($TemplateFru.WcsFruData.Product.ProductCustom1.MaxLength -eq 1)
    {
        $TestCommand = 'Update-WcsFru -ChassisSerial AA -BoardSerial AA -ProductSerial AA -ProductAsset AA -ChassisCustom1 AA -ChassisCustom2 AA -ProductCustom1 A -ProductCustom2 AA -ProductCustom3 AA -BoardMinutes 0'
    }
    Else
    {
        $TestCommand = 'Update-WcsFru -ChassisSerial AA -BoardSerial AA -ProductSerial AA -ProductAsset AA -ChassisCustom1 AA -ChassisCustom2 AA -ProductCustom1 AA -ProductCustom2 AA -ProductCustom3 AA -BoardMinutes 0'

    }
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru verify field padding' -TestId  'ID-FRU-0304' -TestCommand $TestCommand -Match $PaddedFields

    # Update with template file (mutliple extensions)
    #----------------------------------------------------
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru with template file'    -TestId  'ID-FRU-0305' -TestCommand "Update-WcsFru -TemplateFile $CurrentTemplateFile"         -Match $PaddedFields
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru with template.fru.log' -TestId  'ID-FRU-0306' -TestCommand "Update-WcsFru -TemplateFile $CurrentTemplateFile.fru.log" -Match $PaddedFields

    #Update with template file -nomerge
    #--------------------------------------
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru with NoMerge'                        -TestId  'ID-FRU-0307' -TestCommand "Update-WcsFru -TemplateFile $CurrentTemplateFile.log -NoMerge"   -Match $TemplateFields

    # Update with template file and fields specified at same time
    #--------------------------------------------------------------
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru with template and fields specified'   -TestId  'ID-FRU-0308' -TestCommand (MakeUpdateFruCommand $OriginalFields -Template $CurrentTemplateFile) -Match $OriginalFields

    # Attempt to update all fields with values too long and verify truncated
    #---------------------------------------------------------------------------
    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru verify fields truncated if too long'  -TestId  'ID-FRU-0309' -TestCommand (MakeUpdateFruCommand $LongFields) -Match $NewFields

    # Verify can write to FRU even if all 0s
    #-----------------------------------------
    $ZeroFruData = New-Object 'byte[]'  $OriginalFruData.Count
    Try
    {
        $BytesWritten = Update-WcsFruData -FruOffset 0 -DataToWrite $ZeroFruData
        If ($BytesWritten -ne $OriginalFruData.Count) { Throw }
    }
    Catch
    {
        Write-Host -ForegroundColor Yellow "`r`nTEST SETUP ERROR: Could not zero out FRU data`r`n`r"
    }

    CompatibilityTest-UpdateFruCommand  -TestDetails $TestDetails -TestName 'Update-WcsFru restore zeroed FRU to original values' -TestId  'ID-FRU-0310' -TestCommand (MakeUpdateFruCommand $OriginalFields "-NoMerge -Template $CurrentTemplateFile") -Match $OriginalFields
    CompatibilityTest-GetFruDataCommand -TestDetails $TestDetails -TestName 'Verify all FRU bytes after fields restored'          -TestId  'ID-FRU-0311'    -MatchFruBytes  $OriginalFruData 

    # Verify missing template file detected
    #--------------------------------------
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Update-WcsFru identifies missing file' -TestId 'ID-FRU-0312' -TestCommand "Update-WcsFru -TemplateFile NoFile" -MatchStrings "* aborted: Could not open FRU file *" -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Update-WcsFruChecksum:  Inputs   = ChecksumStartOffset, ChecksumEndOffset, ChecksumOffset, DeviceId
    #                          Return   = 0 on success, non-zero or on error
    #
    #                          Function = Updates checksum byte for range specified
    #-------------------------------------------------------------------------------------------------------------------------------------
    $ChassisStart     = $OriginalFruData[2]*8
    $BoardStart       = $OriginalFruData[3]*8
    $ProductStart     = $OriginalFruData[4]*8
    
    If ($OriginalFruData[5]*8 -eq 0)
    {
        $MultiRecordStart = $OriginalFruData.Count
    }
    Else
    {
        $MultiRecordStart = $OriginalFruData[5]*8
    }
    CompatibilityTest-UpdateFruChecksum  -TestDetails $TestDetails -TestName 'Update-WcsFruChecksum basic functionality'  -TestId  'ID-FRU-0350' -StartOffset  $ChassisStart  -ChecksumOffset ($BoardStart -1)       -ChecksumEndOffset ($BoardStart -2)
    CompatibilityTest-UpdateFruChecksum  -TestDetails $TestDetails -TestName 'Update-WcsFruChecksum board checksum'       -TestId  'ID-FRU-0351' -StartOffset  $BoardStart    -ChecksumOffset ($ProductStart -1)     -ChecksumEndOffset ($ProductStart -2)
    CompatibilityTest-UpdateFruChecksum  -TestDetails $TestDetails -TestName 'Update-WcsFruChecksum product checksum'     -TestId  'ID-FRU-0352' -StartOffset  $ProductStart  -ChecksumOffset ($MultiRecordStart -1) -ChecksumEndOffset ($MultiRecordStart -2)

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  View-WcsFru: Inputs   = Full, Config 
    #               Return   = null
    #
    #               Function = Displays key FRU fields
    #               Function = Displays all FRU fields when -Full specified
    #               Function = Display same FRU information when -Config specified
    #-------------------------------------------------------------------------------------------------------------------------------------

    # Must reset FRU to original contents before running the VIEW command tests
    #--------------------------------------------------------------------------
    Try
    {
        $BytesWritten =  Update-WcsFruData -FruOffset 0 -DataToWrite $OriginalFruData -ErrorAction Stop
        If ($BytesWritten -ne $OriginalFruData.Count) { Throw }
    }
    Catch
    {
        Write-Host -ForegroundColor Yellow "`r`nTEST SETUP ERROR: Could not reset FRU data to original values`r`n`r"
    }

    # These tests compare the commands console output against a reference file. 
    #------------------------------------------------------------------------------------------------------------
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsFru'                    -TestId 'ID-FRU-0400' -TestCommand 'View-WcsFru'                                  -FileName 'View-WcsFru'                     
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsFru with Full input'    -TestId 'ID-FRU-0401' -TestCommand 'View-WcsFru -Full'                            -FileName 'View-WcsFru-Full'                  
    CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsFru with Config input'  -TestId 'ID-FRU-0402' -TestCommand 'View-WcsFru -Full -Config (Get-WcsConfig)'    -FileName 'View-WcsFru-Full'                  

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
    #---------------------------------------------------------------------------------
    #  Log error and attempt to reset the FRU!
    #---------------------------------------------------------------------------------
    $TestDetails.Value.FailTests++

    $BytesWritten =  Update-WcsFruData -FruOffset 0 -DataToWrite $OriginalFruData
    #---------------------------------------------------------------------------------
    #  Display Test Results
    #---------------------------------------------------------------------------------
    CompatibilityTest-EndTestSuite -TestDetails $TestDetails -StandAlone:$Standalone -ScriptFile $SCRIPT_FILE


    Return $WCS_RETURN_CODE_UNKNOWN_ERROR
}
