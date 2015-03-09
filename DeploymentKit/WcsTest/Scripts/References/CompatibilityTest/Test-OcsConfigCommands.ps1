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
# This script runs commands related to configurations
#=================================================================================================================================
Param([ref] $TestDetails = ([ref] $null))

$TestViewConfigCommands     = $true
$TestCompareConfigCommands  = $true
$TestMsInfoCommands         = $true


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

    #=================================================================================
    # Tests the Get-WcsConfig command
    #=================================================================================
    Function Test-GetConfigCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
            [ref]     $Results
        )

        Try
        {
            $TestStartTime     = (get-date)

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #---------------------
            # Display test details
            #---------------------
            Write-Host " This test runs Get-WcsConfig and verifies:

                       `r 1) Command completes without error
                       `r 2) Command returns an XML object

                        `r Full Command : $TestCommand`r`n`r"   

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $Results.Value = Invoke-Expression "$TestCommand -ErrorAction Stop"

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($Results.Value -eq $null)                        { Throw "Return value incorrect.  Expected XML object but received null" }
            If ($Results.Value.GetType().Name -ne 'XmlDocument') { Throw ("Return value incorrect.  Expected XML object but received {0}" -f $Results.Value.GetType().Name) } 
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
    }

    #=================================================================================
    # Tests Compare-WcsConfig command
    #=================================================================================
    Function Test-CompareConfigCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
                      $SourceCfg,
                      $RefCfg,
            [ref]     $Results,
            [int]     $Mismatches    = 0,
            [switch]  $OnlyRefDevices,
            [switch]  $Exact
        )

        Try
        {
            $TestStartTime     = (get-date)

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #---------------------
            # Display test details
            #---------------------
            Write-Host " This test runs Compare-WcsConfig and verifies:

                       `r 1) Command completes without error
                       `r 2) Command returns the number of device mismatches: $Mismatches
                       "  
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Compare-WcsConfig -Source $SourceCfg -RefConfig $RefCfg -Exact:$Exact -OnlyRefDev:$OnlyRefDevices -RefToResults  $Results -ErrorAction Stop

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($Mismatches -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected $Mismatches but received null" }
                Else                          { Throw "Return value incorrect.  Expected $Mismatches but received $ReturnedValue" }
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
    }

    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Log-WcsConfig: Inputs   =   File, Path, Config
    #                 Return   =   0 on success, non-zero on error
    #
    #                 Function = Logs to default directory if not specified (file must be specified)
    #                 Function = Accepts file with no extension, .xml extension, and .config.xml extension
    #                 Function = Log config to file and directory specified
    #                 Function = Verifies Config object type
    #                 Function = Verify LogDirectory alias for Path input
    #-------------------------------------------------------------------------------------------------------------------------------------
    $CurrentConfig = Get-WcsConfig
    $BadConfig     = 123
    $BadConfig2    = [xml] '<?xml version="1.0" encoding="utf-16"?><MyRootNode />'

    $LogPath = "$WCS_CONFIGURATION_DIRECTORY"

    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig basic functionality'       -TestId 'ID-CFG-0010' -TestCommand "Log-WcsConfig -File TestConfig"                               -File  "TestConfig.config.xml"  -SingleFile -Directory "$LogPath"   
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig overwrites existing file'  -TestId 'ID-CFG-0011' -TestCommand "Log-WcsConfig -File TestConfig2.Config.Xml"                   -File  "TestConfig2.config.xml" -SingleFile -Directory "$LogPath"  -CheckOverWrite   
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig with Config input'         -TestId 'ID-CFG-0012' -TestCommand "Log-WcsConfig -File TestConfig3.xml -Config `$CurrentConfig"  -File  "TestConfig3.config.xml" -SingleFile -Directory "$LogPath"   

    $SavedLogPath = "$FULL_TEST_DIRECTORY\ID-CFG-0013-Log-WcsConfig"
              
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig with Path input'        -TestId 'ID-CFG-0013' -TestCommand "Log-WcsConfig -File TestConfig4 -Path $SavedLogPath" -File  "TestConfig4.config.xml" -SingleFile -Directory "$SavedLogPath" -CheckOverWrite

    $LogPath = "$FULL_TEST_DIRECTORY\ID-CFG-0014-Log-WcsConfig"
              
    CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig with Path input'        -TestId 'ID-CFG-0014' -TestCommand "Log-WcsConfig -File TestConfig5 -LogDirectory $LogPath" -File  "TestConfig5.config.xml" -SingleFile -Directory "$LogPath"  
 
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig with invalid Config'       -TestId 'ID-CFG-0015' -TestCommand "Log-WcsConfig -File NoFile -Config `$BadConfig"  -MatchStrings "* aborted: Config input not valid" -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Log-WcsConfig with invalid Config #2'    -TestId 'ID-CFG-0016' -TestCommand "Log-WcsConfig -File NoFile -Config `$BadConfig2" -MatchStrings "* aborted: Config input not valid" -MatchLines 1 -ReturnCode $WCS_RETURN_CODE_FUNCTION_ABORTED
  
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Get-WcsConfig: Inputs   =  File, Path, SkipDriver
    #                 Return   =  XML object with configuration data on success, $null on error
    #
    #                 Function = Reads current configuration if no file specified
    #                 Function = Reads current configuration without driver info if skipdriver specified
    #
    #                 Function = Gets config from file and directory specified
    #                 Function = Gets from default directory if not specified
    #                 Function = Verifies Config object type from file
    #                 Function = Accepts file with no extension, .xml extension, and .config.xml extension
    #                 Function = Verify LogDirectory alias for Path input
    #                 Function = Ignores skipdriver if file specified
    #
    #                 Function = Reports File missing and returns null when file+path incorrect
    #-------------------------------------------------------------------------------------------------------------------------------------
    $SrcFromFile       = $null
    $SrcFromFile2      = $null
    $SrcFromFile3      = $null
    $SkipDDriverConfig = $null

    Test-GetConfigCommand -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig basic functionality'           -TestId 'ID-CFG-0051' -TestCommand " Get-WcsConfig"              -Results ([ref] $CurrentConfig)
    Test-GetConfigCommand -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig with SkipDriver'               -TestId 'ID-CFG-0052' -TestCommand " Get-WcsConfig -SkipDriver"  -Results ([ref] $SkipDDriverConfig)

    $SystemConfig   = $CurrentConfig.clone()

    Test-GetConfigCommand -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig read from file'                -TestId 'ID-CFG-0053' -TestCommand " Get-WcsConfig -File TestConfig"                           -Results ([ref] $SrcFromFile)
    Test-GetConfigCommand -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig read from file with extension' -TestId 'ID-CFG-0054' -TestCommand " Get-WcsConfig -File TestConfig.config.xml"                -Results ([ref] $SrcFromFile2)
    Test-GetConfigCommand -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig read from file and path'       -TestId 'ID-CFG-0055' -TestCommand " Get-WcsConfig -File TestConfig4.xml -Path $SavedLogPath"  -Results ([ref] $SrcFromFile3)

    $LogPath = "$FULL_TEST_DIRECTORY\ID-CFG-0056-WcsConfig"

    New-Item    -Path "$FULL_TEST_DIRECTORY\ID-CFG-0056-WcsConfig" -Force -ItemType Container  -ErrorAction SilentlyContinue | Out-Null
    Set-Content -Path "$LogPath\Badfile.config.xml" -Value '<?xml version="1.0" encoding="utf-16"?><MyRootNode />'

    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig with invalid Config'       -TestId 'ID-CFG-0056' -TestCommand "GEt-WcsConfig -File BadFile -LogDirectory $LogPath "  -MatchStrings "* aborted: File is not a valid config *" -MatchLines 1 -ReturnNull
    CompatibilityTest-CommandError -TestDetails $TestDetails -TestName 'Verify Get-WcsConfig with missing file'         -TestId 'ID-CFG-0057' -TestCommand "GEt-WcsConfig -File NoFile  -LogDirectory $LogPath "  -MatchStrings "* aborted: Could not open config file *" -MatchLines 1 -ReturnNull
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Compare-WcsConfig: Inputs   = RefConfig, SourceConfig, RefToResults, Exact, OnlyRefDevices, Quiet 
    #                     Return   = Number of mismatches
    #
    #                     Function = Returns 0 when RefConfig matches SourceConfig
    #                     Function = Returns 0 when RefConfig matches SourceConfig and Exact specified
    #                     Function = Returns 0 when RefConfig matches SourceConfig and OnlyRefDevices specified
    #
    #                     Function = RefToResults returns a valid configuration object that can be used as an input
    #
    #                     Function = Returns 0 for a single ONEXACT parameter mismatch
    #                     Function = Returns 1 for a single ONEXACT parameter mismatch when Exact specified
    #                     Function = Returns 1 for multiple parameter mismatch on the same device
    #                     Function = Returns 1 for a missing parameter
    #                     Function = Returns 1 for an unexpected parameter 
    #
    #                     Function = Returns 1 for a missing device
    #                     Function = Returns 1 for an unexpected device
    #                     Function = Returns 0 for an unexpected device whenOnlyRefDevices specified
    #-------------------------------------------------------------------------------------------------------------------------------------
    If ($TestCompareConfigCommands)
    {
        $CompareResults   = $null
        $ResultsBuffer    = $null
    
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source and ref the current config'                           -TestId 'ID-CFG--0100' -Source $SystemConfig     -Ref $CurrentConfig      -Results ([ref] $CompareResults)
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source and ref the current config and Exact'                 -TestId 'ID-CFG--0101' -Source $SystemConfig     -Ref $CurrentConfig      -Results ([ref] $CompareResults) -Exact
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source and ref the current config and OnlyRefDevices'        -TestId 'ID-CFG--0102' -Source $SystemConfig     -Ref $CurrentConfig      -Results ([ref] $CompareResults) -OnlyRefDevices

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source output from prior test and ref the current config'    -TestId 'ID-CFG--0103' -Source $CompareResults   -Ref $CurrentConfig      -Results ([ref] $ResultsBuffer)
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source the current and ref the output from prior test'       -TestId 'ID-CFG--0104' -Source $CurrentConfig    -Ref $CompareResults     -Results ([ref] $ResultsBuffer)

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source from log file and ref the current config'             -TestId 'ID-CFG--0105' -Source $SrcFromFile      -Ref $CurrentConfig      -Results ([ref] $ResultsBuffer)
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source the current and ref from another file'                -TestId 'ID-CFG--0106' -Source $SystemConfig     -Ref $SrcFromFile2       -Results ([ref] $ResultsBuffer)
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with source from third log file and ref the current config'       -TestId 'ID-CFG--0107' -Source $SrcFromFile3     -Ref $CurrentConfig      -Results ([ref] $ResultsBuffer)


        $SystemConfig.WcsConfig.BIOS.SerialNumber.Value = "Wrong Number"

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS serial number mismatch'                                 -TestId 'ID-CFG-0110' -Source $SystemConfig     -Ref $CurrentConfig       -Results ([ref] $ResultsBuffer)  
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS serial number mismatch and Exact'                       -TestId 'ID-CFG-0111' -Source $SystemConfig     -Ref $CurrentConfig       -Results ([ref] $ResultsBuffer) -Mismatches 1 -Exact


        $SystemConfig.WcsConfig.BIOS.Version.Value = "Wrong Version"
             
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version mismatch'                                       -TestId 'ID-CFG-0112' -Source $SystemConfig     -Ref $CurrentConfig       -Results ([ref] $ResultsBuffer) -Mismatches 1
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version mismatch and Exact'                             -TestId 'ID-CFG-0113' -Source $SystemConfig     -Ref $CurrentConfig       -Results ([ref] $ResultsBuffer) -Mismatches 1 -Exact
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version mismatch and OnlyRefDevices'                    -TestId 'ID-CFG-0114' -Source $SystemConfig     -Ref $CurrentConfig       -Results ([ref] $ResultsBuffer) -Mismatches 1 -OnlyRefDevices

        $TempVar = $SystemConfig.WcsConfig.BIOS.Version
        $TempVar = $SystemConfig.WcsConfig.BIOS.RemoveChild($TempVar)

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version parameter missing in source'                    -TestId 'ID-CFG-0115' -Source $SystemConfig     -Ref $CurrentConfig     -Results ([ref] $ResultsBuffer) -Mismatches 1
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version parameter missing in source and OnlyRefDevices' -TestId 'ID-CFG-0116' -Source $SystemConfig     -Ref $CurrentConfig     -Results ([ref] $ResultsBuffer) -Mismatches 1 -OnlyRefDevices

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version parameter missing in ref'                       -TestId 'ID-CFG-0117' -Source $CurrentConfig    -Ref $SystemConfig      -Results ([ref] $ResultsBuffer) -Mismatches 1
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS version parameter missing in ref and OnlyRefDevices'    -TestId 'ID-CFG-0118' -Source $CurrentConfig    -Ref $SystemConfig      -Results ([ref] $ResultsBuffer) -OnlyRefDevices

        $TempVar = $SystemConfig.WcsConfig.BIOS
        $TempVar = $SystemConfig.WcsConfig.RemoveChild($TempVar)

        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS device missing in source'                               -TestId 'ID-CFG-0119' -Source $SystemConfig     -Ref $CurrentConfig    -Results ([ref] $ResultsBuffer) -Mismatches 1
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS device missing in ref'                                  -TestId 'ID-CFG-0120' -Source $CurrentConfig    -Ref $SystemConfig     -Results ([ref] $ResultsBuffer) -Mismatches 1
        Test-CompareConfigCommand -TestDetails $TestDetails -TestName 'Verify Compare-WcsConfig with BIOS device missing in ref and OnlyRefDevices'               -TestId 'ID-CFG-0121' -Source $CurrentConfig    -Ref $SystemConfig     -Results ([ref] $ResultsBuffer)  -OnlyRefDevices
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  View-Wcs*:    Inputs   = Config
    #                Return   = null
    #
    #                Function = Displays the correct configuration information
    #                Function = Displays the correct configuration information using the Config input
    #-------------------------------------------------------------------------------------------------------------------------------------
    If ($TestViewConfigCommands)
    {
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsDimm'                      -TestId 'ID-CFG-0150' -TestCommand 'View-WcsDimm'             -FileName 'View-WcsDimm' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsDisk'                      -TestId 'ID-CFG-0151' -TestCommand 'View-WcsDisk'             -FileName 'View-WcsDisk' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsNic'                       -TestId 'ID-CFG-0152' -TestCommand 'View-WcsNic'              -FileName 'View-WcsNic' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHba'                       -TestId 'ID-CFG-0153' -TestCommand 'View-WcsHba'              -FileName 'View-WcsHba' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsProcessor'                 -TestId 'ID-CFG-0154' -TestCommand 'View-WcsProcessor'        -FileName 'View-WcsProcessor' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsFirmware'                  -TestId 'ID-CFG-0155' -TestCommand 'View-WcsFirmware'         -FileName 'View-WcsFirmware' 

        # Cannot test View-WcsConfig in WinPE because hostname is dynamic.  Also cannot test View-WcsDrive because RAM drive size changes slightly from boot to boot

        If (-NOT (CoreLib_IsWinPE))
        {
            CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsConfig'                -TestId 'ID-CFG-0157' -TestCommand 'View-WcsConfig'           -FileName 'View-WcsConfig' 
            CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsDrive'                 -TestId 'ID-CFG-0156' -TestCommand 'View-WcsDrive'            -FileName 'View-WcsDrive' 
        }
        #--------------------------------------------
        #  Repeat test with -Config input parameter
        #--------------------------------------------
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsDimm with Config input'                -TestId 'ID-CFG-0160' -TestCommand 'View-WcsDimm -Config $CurrentConfig'             -FileName 'View-WcsDimm' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsDisk with Config input'                -TestId 'ID-CFG-0161' -TestCommand 'View-WcsDisk -Config $CurrentConfig'             -FileName 'View-WcsDisk' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsNic with Config input'                 -TestId 'ID-CFG-0162' -TestCommand 'View-WcsNic -Config $CurrentConfig'              -FileName 'View-WcsNic' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsHba with Config input'                 -TestId 'ID-CFG-0163' -TestCommand 'View-WcsHba -Config $CurrentConfig'              -FileName 'View-WcsHba' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsProcessor with Config input'           -TestId 'ID-CFG-0164' -TestCommand 'View-WcsProcessor -Config $CurrentConfig'        -FileName 'View-WcsProcessor' 
        CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsFirmware with Config input'            -TestId 'ID-CFG-0165' -TestCommand 'View-WcsFirmware -Config $CurrentConfig'         -FileName 'View-WcsFirmware' 

        # Cannot test in WinPE because hostname is dynamic

        If (-NOT (CoreLib_IsWinPE))
        {
            CompatibilityTest-ViewCommand -TestDetails $TestDetails -TestName 'Verify View-WcsConfig with Config input'          -TestId 'ID-CFG-0166' -TestCommand 'View-WcsConfig -Config $CurrentConfig'           -FileName 'View-WcsConfig' 
        }
    }
    #-------------------------------------------------------------------------------------------------------------------------------------
    #  Log-MsInfo32: Inputs   = LogDirectory
    #                Return   = 0 on success, non-zero on fail
    #
    #                Function = Logs msinfo to default directory if directory not specified
    #                Function = Logs msinfo to directory specified (relative path)
    #                Function = Logs msinfo to directory specified (absolute path)
    #-------------------------------------------------------------------------------------------------------------------------------------
    If (( -Not (CoreLib_IsWinPE)) -and ($TestMsInfoCommands))
    {
        Remove-Item -Path "$WCS_RESULTS_DIRECTORY\Log-MsInfo32\*" -Force -Recurse -ErrorAction SilentlyContinue | Out-Null 

        CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-MsInfo32 basic functionality'   -TestId 'ID-CFG-0200'  -TestCommand "Log-MsInfo32"  -msinfo -MoveTo "$FULL_TEST_DIRECTORY\log-msinfo32"
        CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-MsInfo32 with absolute path'    -TestId 'ID-CFG-0201'  -TestCommand "Log-MsInfo32 -LogDirectory $FULL_TEST_DIRECTORY\ID-CFG-0101-MsInfo"  -File "msinfo32.log" -Directory "$FULL_TEST_DIRECTORY\ID-CFG-0101-MsInfo"  -SingleFile
        CompatibilityTest-CommandAndFiles -TestDetails $TestDetails -TestName 'Verify Log-MsInfo32 with relative path'    -TestId 'ID-CFG-0202'  -TestCommand "Log-MsInfo32 -LogDirectory ID-CFG-0102-MsInfo"                       -File "msinfo32.log" -Directory "ID-CFG-0102-MsInfo" -SingleFile
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



