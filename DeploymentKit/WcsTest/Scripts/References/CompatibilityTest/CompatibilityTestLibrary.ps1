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

If (-Not (Test-Path variable:COMPATIBILITY_TEST_DIRECTORY))
{

    #--------------------------------------------------------------
    # Include the libraries if they have not already been loaded
    #--------------------------------------------------------------
    If (-Not (Test-Path variable:WCS_RESULTS_DIRECTORY))
    {
        . \WcsTest\Scripts\wcsScripts.ps1     
    }

    #-------------------------------------------------------------------
    # Read board serial number to defined test reference files location
    #-------------------------------------------------------------------
    Try
    {
        $TempFru                  = IpmiLib_GetBmcFru  -ErrorAction Stop 
        $TEST_REF_FILE_DIRECTORY  = ("$WCS_BASE_DIRECTORY\Results\RefFiles\{0}" -f $TempFru.BoardSerial.Value.Trim())
    }
    Catch
    {
        $TEST_REF_FILE_DIRECTORY  = "$WCS_BASE_DIRECTORY\Results\RefFiles\Unknown"
    }

    $COMPATIBILITY_TEST_DIRECTORY   = "$WCS_REF_DIRECTORY\CompatibilityTest"
    $COMPATIBILITY_BLADE_TEST_SUITE = "\wcstest\scripts\references\compatibilitytest\Test-OcsBladeCommands.ps1" 


    $WCS_TRANSCRIPT_COPY            = "$WCS_RESULTS_DIRECTORY\TempTranscriptCopy.log"
    $WCS_BINARY_FILELIST            = "$COMPATIBILITY_TEST_DIRECTORY\BinaryFileList.log"
    #-------------------------------------------------------------------
    # Setup script variables
    #-------------------------------------------------------------------
    $TKT_HEADER_LINE_WIDTH             = 120
    $TKT_HEADER_FOREGROUND_COLOR       = 'Black'
    $TKT_HEADER_BACKGROUND_COLOR       = 'Gray'
    $TKT_HEADER_BACKGROUND_PASS_COLOR  = 'DarkGreen'
    $TKT_HEADER_BACKGROUND_FAIL_COLOR  = 'Red'

    $MISSING_FILE_ERROR_MESSAGE        = "*: Could not open file '*'"
       
    #-------------------------------------------------------------------
    # Helper function that writes a colored box at start of every test
    #-------------------------------------------------------------------
    Function CompatibilityTest-TestBox([string]$TestInfo)
    { 
        Write-Host "`r"
        Write-Host ("+{0}+`r" -f ("-"*$TKT_HEADER_LINE_WIDTH ))      -Foreground $TKT_HEADER_FOREGROUND_COLOR   -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("|{0,-$TKT_HEADER_LINE_WIDTH}|`r" -f $TestInfo)  -Foreground $TKT_HEADER_FOREGROUND_COLOR   -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("+{0}+`r" -f ("-"*$TKT_HEADER_LINE_WIDTH ))      -Foreground $TKT_HEADER_FOREGROUND_COLOR   -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host "`r"
    }
    #---------------------------------------------------------------------------------
    # Helper functions to display info when a test starts
    #---------------------------------------------------------------------------------
    Function CompatibilityTest-TestHeader()
    {
        [CmdletBinding()] 
        Param
        (
            [Parameter(Mandatory=$true)]  [int]      $TestCounter,
            [Parameter(Mandatory=$true)]  [string]   $TestName,
            [Parameter(Mandatory=$true)]  [string]   $TestId,
            [Parameter(Mandatory=$true)]  [string]   $ScriptFile

        )
        CompatibilityTest-TestBox (" [START TEST #{0,-3}] {1}" -f $TestCounter, $TestName) 
 
        Write-Host (" TEST ID: {0} ({1})`r" -f $TestId,$ScriptFile)
        Write-Host (" TIME:    {0} `r" -f (Get-Date -Format G))

        Write-Host "`r"  
    }
    #---------------------------------------------------------------------------------
    # Helper function that displays info when test script starts within a test suite
    #---------------------------------------------------------------------------------
    Function CompatibilityTest-TestScriptHeader()
    {
        [CmdletBinding()] 
        Param
        (
            [Parameter(Mandatory=$true)]  [string] $File,
            [Parameter(Mandatory=$true)]  [string] $LogDirectory

        )
        $FieldWidth = $TKT_HEADER_LINE_WIDTH-21

        Write-Host ("+{0}+`r" -f ("-"*$TKT_HEADER_LINE_WIDTH)) 
        Write-Host ("| Script           :  {0,-$FieldWidth}|`r" -f $File)  
        Write-Host ("| Started          :  {0,-$FieldWidth}|`r" -f (get-date))  
        Write-Host ("| Log Directory    :  {0,-$FieldWidth}|`r" -f $LogDirectory)  
        Write-Host ("+{0}+`r" -f ("-"*$TKT_HEADER_LINE_WIDTH))  
        Write-Host "`r`n`r`n"

    }
    #---------------------------------------------------------------------------------
    # Helper function that displays info when test suite starts
    #---------------------------------------------------------------------------------
    Function CompatibilityTest-TestSuiteHeader()
    {
        [CmdletBinding()] 
        Param
        (
            [Parameter(Mandatory=$true)]  [string] $TestSuiteName,
            [Parameter(Mandatory=$true)]  [string] $TranscriptFile,
            [Parameter(Mandatory=$true)]  [string] $LogDirectory,
            [Parameter(Mandatory=$false)] [string] $RemoteFile=''

        )
        $FieldWidth = $TKT_HEADER_LINE_WIDTH-21

        Write-Host ("+{0}+`r" -f ("="*$TKT_HEADER_LINE_WIDTH))                                          -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR

        Write-Host ("|{0,-$TKT_HEADER_LINE_WIDTH}|`r" -f ($TestSuiteName + " TEST SUITE"))              -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("|{0,-$TKT_HEADER_LINE_WIDTH}|`r" -f " ")                                           -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("| Toolkit Version  :  {0,-$FieldWidth}|`r" -f $Global:WcsTestToolsVersion)         -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("| Started          :  {0,-$FieldWidth}|`r" -f (get-date))                          -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("| System           :  {0,-$FieldWidth}|`r" -f (hostname))                          -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("| Log Directory    :  {0,-$FieldWidth}|`r" -f $LogDirectory)                       -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host ("| Transcript       :  {0,-$FieldWidth}|`r" -f (split-path $TranscriptFile -Leaf))  -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        If ($RemoteFile -ne '')
        {
            Write-Host ("| Remote Files     :  {0,-$FieldWidth}|`r" -f $RemoteFile)                     -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        }
        Write-Host ("+{0}+`r" -f ("="*$TKT_HEADER_LINE_WIDTH))                                          -Foreground $TKT_HEADER_FOREGROUND_COLOR -Background $TKT_HEADER_BACKGROUND_COLOR
        Write-Host "`r`n`r`n"

    }
    #---------------------------------------------------------------------------------
    # Helper function to setup the start of a test suite
    #---------------------------------------------------------------------------------
    Function CompatibilityTest-StartTestSuite()
    {
        [CmdletBinding()] 

        Param
        ( 
            [ref]     $TestDetails, 
            [switch]  $Standalone
        )

        #----------------------------------------------------
        # Setup the directories and go to test directory
        #----------------------------------------------------
        If ($TestDetails.Value.TestDirectory.StartsWith($WCS_RESULTS_DIRECTORY))
        {
            $TestDetails.Value.LocalTestDirectory = $TestDetails.Value.TestDirectory.substring(($WCS_RESULTS_DIRECTORY.Length+1)) 
        }
        Else
        {
            Throw ("Illegal test directory specified {0}.  Must start with $WCS_RESULTS_DIRECTORY" -f $TestDetails.Value.TestDirectory )
        }

        New-Item -Path $TestDetails.Value.TestDirectory -ItemType Container -Force -ErrorAction SilentlyContinue | Out-Null

        Set-Location -Path $TestDetails.Value.TestDirectory
        [System.IO.Directory]::SetCurrentDirectory($TestDetails.Value.TestDirectory)

        #----------------------------------------------------
        # Display banner and start transcript
        #----------------------------------------------------
        If ($Standalone)
        {
            #----------------------------------------------------
            # Setup Transcript File
            #----------------------------------------------------
            try { Stop-Transcript -ErrorAction Stop | Out-Null } Catch {}
            try { Start-Transcript -Path $TestDetails.Value.TranscriptFile -Append -ErrorAction Stop | Out-Null } Catch {}

            #----------------------------------------------------
            # Display the full Test Header
            #----------------------------------------------------
            CompatibilityTest-TestSuiteHeader -TestSuiteName $TestDetails.Value.TestSuiteName  -TranscriptFile $TestDetails.Value.TranscriptFile   -LogDirectory $TestDetails.Value.TestDirectory
        }
        Else
        {
            #----------------------------------------------------
            # Display the small Test Header
            #----------------------------------------------------
            CompatibilityTest-TestScriptHeader -File $TestDetails.Value.ScriptFile  -LogDirectory $TestDetails.Value.TestDirectory
        }

    }
    #---------------------------------------------------------------------------------
    # Helper functions to pass a toolkit test
    #---------------------------------------------------------------------------------
    Function Pass-CompatibilityTest()
    {
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]  [datetime]   $StartTime,
            [Parameter(Mandatory=$false)] [string]     $ResultDescription=''
        )

        Write-Host "`r`n [END TEST]  Result   : TEST PASSED`r"  
               
        If ('' -ne $ResultDescription)
        {
            Write-Host  "             $ResultDescription`r"
        }
        Write-Host     ("             Test Time: {0:f3} seconds`r" -f ((Get-Date)-$StartTime).TotalSeconds)     
        Write-Host "`r"

        Return 1
    }
    #---------------------------------------------------------------------------------
    # Helper functions to fail a toolkit test
    #---------------------------------------------------------------------------------
    Function Fail-CompatibilityTest()
    {
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]  [datetime]   $StartTime,
            [Parameter(Mandatory=$false)] [string]     $ResultDescription = '',
            [Parameter(Mandatory=$false)]              $Exception  = $null
        )

        If ('' -ne $ResultDescription)
        {
            Write-Host  ("             $ResultDescription`r")
        }

        If ($null -ne $Exception)
        {
            If ($Exception.ErrorDetails -eq $null) 
            {   
                Try
                {
                    $Position = $Exception.Exception.ErrorRecord.InvocationInfo.PositionMessage
                }
                Catch
                {
                    $Position = $Exception.InvocationInfo.PositionMessage 
                }
                Write-Host ("`r`nEXCEPTION: {0}`r`n{1}`r`n`r" -f $Exception.Exception.Message,$Position)
            }
            Else
            {
                Write-Host $Exception.ErrorDetails.MEssage
            }
        }

        Write-Host  "`r`n [END TEST]  Result   : **TEST FAILED**`r"   -BackgroundColor $TKT_HEADER_BACKGROUND_FAIL_COLOR  
        Write-Host     ("             Test Time: {0:f3} seconds`r" -f ((Get-Date)-$StartTime).TotalSeconds)   
        Write-Host "`r"

        Return 1
    }
   
    #---------------------------------------------------------------------------------
    # Helper function that cleans up at end of test suite
    #---------------------------------------------------------------------------------
    Function CompatibilityTest-EndTestSuite()
    {
        [CmdletBinding()] 
        Param
        (
                 [ref]    $TestDetails,
                [switch] $StandAlone,
                [string] $ScriptFile
        )
        If ($Standalone)
        {
            If ($TestDetails.Value.FailTests -ne 0)
            {
                 $Color = $TKT_HEADER_BACKGROUND_FAIL_COLOR
            }
            Else
            {
                 $Color = $TKT_HEADER_BACKGROUND_PASS_COLOR
            }

            Write-Host ("+{0}+`r" -f ("="*100))  -Background $Color
            Write-Host ("| {0,-99}|`r" -f ($TestDetails.Value.TestSuiteName + " RESULTS"))   -Background $Color
            Write-Host  ("| {0,-99}|`r" -f " " ) -Background $Color
            Write-Host  ("| PASSING TESTS    {0,-82}|`r" -f $TestDetails.Value.PassTests)   -Background $Color
            Write-Host  ("| FAILING TESTS    {0,-82}|`r" -f $TestDetails.Value.FailTests)   -Background $Color
            Write-Host  ("| {0,-99}|`r" -f " " ) -Background $Color

            Write-Host  ("| {0,-99}|`r" -f ("TOTAL TEST TIME  {0:N0} seconds ({1:N1} minutes)" -f ((Get-Date)-$TestDetails.Value.TestSuiteStartTime).TotalSeconds,((Get-Date)-$TestDetails.Value.TestSuiteStartTime).TotalMinutes)  )  -Background $Color

            Write-Host ("+{0}+`r" -f ("="*100))  -Background $Color
        }

        Remove-Item $WCS_TRANSCRIPT_COPY  -Force -ErrorAction SilentlyContinue | Out-Null
    }

    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to be called from a jump box to execute the Compatibility Blade Command tests on one or more remote blades.
    # ... This suite runs most Toolkit commands to verify functionality of each command.  There are some commmands that are not tested in 
    # ... this suite:  Cycle-OsReboot, Cycle-WCsBladePower, Cycle-WcsUpdate, and Update-WcsConfig
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-RemoteBladeTestSuite()
    { 
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]   [ref]     $TestDetails,
                                           [switch]  $PoshVersion2
        )

        $TestStartTime     = Get-Date
        $TestName          = 'Remote Execution of Compatibility Blade Command Test Suite'
        $TestID            = 'ID-REMOTE-0001'

        #--------------------
        # Display test header
        #--------------------
        CompatibilityTest-TestHeader -ScriptFile $TestDetails.Value.ScriptFile -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

        #---------------------
        # Display test details
        #---------------------
        Write-Host " This test runs the Compatibility Blade Command Test Suite and verifies no errors are reported `r"

        #---------------------
        # Start the test
        #---------------------
        Try
        {
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            If ($PoshVersion2)
            {
                $CurrentVersion         = $WCS_POWERSHELL_BINARY
                $WCS_POWERSHELL_BINARY  = "Powershell -Ver 2"  
            }
            
            $ReturnedValue = Invoke-WcsScript -TargetList $TestDetails.Value.RemoteTargets -Script $COMPATIBILITY_BLADE_TEST_SUITE -WaitTime 4000
            
            If ($PoshVersion2)
            {
                $WCS_POWERSHELL_BINARY = $CurrentVersion 
            }
            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
                Else                          
                { 
                    Throw " $ReturnedValue blades failed the Compatibility Blade Command Test Suite" 
                }
            }
            #--------------------------------------------------------------------------------------
            # Test passed so display results
            #--------------------------------------------------------------------------------------  
            $TestDetails.Value.PassTests += Pass-CompatibilityTest -StartTime $TestStartTime  

            Return 0
        }
        Catch
        {
            #--------------------------------------------------------------------------------------
            # Test failed so display results
            #--------------------------------------------------------------------------------------  
            $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime  -Exception $_

            Return 1
        }
    }
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to be called from a jump box to execute a single test run of Cycle-OsReboot, Cycle-WCsBladePower, 
    # ... or Cycle-WcsUpdate tests on one or more remote blades. 
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-RemoteCycleCommands()
    { 
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]   [ref]     $TestDetails,
            [Parameter(Mandatory=$false)]  [int]     $WaitTimeInSec=1800,
            [Parameter(Mandatory=$false)]  [string]  $FilePath  = '',
            [Parameter(Mandatory=$false)]  [int]     $ExpectedFail,
            [Parameter(Mandatory=$false)]  [int]     $ExpectedPass,
                                           [string]  $CmTarget,
                                           [switch]  $PowerCycle
        )
        #---------------------
        # Start the test
        #---------------------
        Try
        {
            $TestStartTime     = Get-Date
            $FoundError        = $false
            $BladeTargets      = $TestDetails.Value.RemoteTargets

            If ($PowerCycle)
            {
                $FilePath          = split-path ("{0}\CycleBladePower-Summary.log" -f $TestDetails.Value.LogDirectory) -NoQualifier
            }
            Else
            {
                $FilePath          = split-path ("{0}\OsReboot-Summary.log" -f $TestDetails.Value.LogDirectory) -NoQualifier
            }
            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $TestDetails.Value.ScriptFile -TestID $TestDetails.Value.ID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestDetails.Value.Name  

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test remotely runs a cycle command on one or more compute blades and verifies on each blade:

    1) $ExpectedPass cycles passed 
    2) $ExpectedFail cycles failed

 Log file on each blade:  $FilePath

 Full Command: $($TestDetails.Value.Command)  `r`n`r"   
    
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            If ($PowerCycle)
            {
                $ReturnedValue = Invoke-WcsCommand -TargetList $TestDetails.Value.RemoteTargets -Command "Clear-WcsError"

                If (0 -ne $ReturnedValue)
                {
                    Throw " Failed to clear errors on $ReturnedValue blade targets`r"
                }
                Write-Host " Waiting $WaitTimeInSec seconds for cycle test to complete`r" 

                $ReturnedValue = Invoke-WcsCommand -TargetList $CmTarget -Command "ChCred -u $($Global:ChassisManagerCredential.UserName) -pass ''$($Global:ChassisManagerCredential.GetNetworkCredential().Password)'';$($TestDetails.Value.Command)"  -WaitTimeInSec  $WaitTimeInSec  -Chassis  

                If ($ExpectedFail -ne 0)
                {
                    $ReturnedValue = 0  #Ignore return code if expect fail and rely on log file checks
                }

            }
            Else
            {             
                $ReturnedValue = Invoke-WcsCommand -TargetList $TestDetails.Value.RemoteTargets -Command $TestDetails.Value.Command  
            }
            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne $ReturnedValue)
            {
                $FoundError = $true
                Write-Host  " Cycle command return value $ReturnedValue does not match expected 0`r"  
            }
            #--------------------------------------------------------------------------------------
            # Wait for blade targets to complete test
            #--------------------------------------------------------------------------------------
            If (-not $PowerCycle)
            {
                Write-Host " Waiting $WaitTimeInSec seconds for cycle test to complete`r" 
                Start-Sleep -Seconds  $WaitTimeInSec 
            }
            #---------------------------------------------------------------
            # Ping all systems to verify powered on and connected to network
            #---------------------------------------------------------------
            $ReturnedValue = Ping-WcsSystem -TargetList $TestDetails.Value.RemoteTargets  

            If (0 -ne $ReturnedValue)
            {
                $FoundError = $true
                Write-Host  " Could not ping $ReturnCode systems.`r"  
            }
            #---------------------------------------------------------------
            # Display the results
            #---------------------------------------------------------------
            If ($FilePath -ne '')
            {
                If ($PowerCycle)
                {
                    $LogFileTarget =  $CmTarget 
                }
                Else
                {
                    $LogFileTarget = $TestDetails.Value.RemoteTargets
                }
                $LogFileTarget  | ForEach-Object {

                    $FailedTest       = $True
                    $NumberOfFailures = -1
                    $NumberOfPasses   = -1

                    Get-Content -Path "\\$_\c`$\$FilePath"  | ForEach-Object {

                        If ($_ -clike "*FINAL CYCLE RESULTS*")
                        {                       
                            $NumberOfFailures = $_.Substring($_.IndexOf("FAIL:")).split()[1] 
                            $NumberOfPasses   = $_.Substring($_.IndexOf("PASS:")).split()[1] 
                        
                            If (($NumberOfFailures -eq $ExpectedFail) -and ($NumberOfPasses -eq $ExpectedPass)) 
                            { 
                                $FailedTest = $false 
                            }                        
                        }
                    }
                    If ($FailedTest)
                    {
                        Write-Host " Target $_ failed cycle test.  Passed cycled: $NumberOfPasses  Failed Cycles: $NumberOfFailures`r" 
                        $FoundError = $true
                    }
                }                
            }
            If ($FoundError)
            {
                #--------------------------------------------------------------------------------------
                # Test failed so display results
                #--------------------------------------------------------------------------------------  
                $TestDetails.Value.FailTests += Fail-CompatibilityTest -StartTime $TestStartTime
                Return
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to be called from a jump box to execute multiple tests of the Cycle-OsReboot command on one or more 
    # ... remote boxes
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-RemoteRebootCommand()
    { 
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]  [ref] $TestDetails
        )
        Try
        {
            $BladeTargets = $TestDetails.Value.RemoteTargets

            #-------------------------------------------------------
            # Get calling details for debug
            #-------------------------------------------------------
            $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

            #-------------------------------------------------------
            # Setup vars
            #-------------------------------------------------------    
            [int] $WaitDelayInSeconds  = (60*7)  # 7 minutes per cycle

            #-------------------------------------------------------
            # PRE-TEST SETUP
            #-------------------------------------------------------    
            CompatibilityTest-TestBox " PRE-TEST SETUP - REMOTE CYCLE-OSREBOOT"

            Write-Host " Creating reference configuration files...`r"

            $ReturnCode = Invoke-WcsCommand -TargetList $BladeTargets -Command "Log-WcsConfig Reference" 

            If (0 -ne $ReturnCode)
            {
                Throw  "$ReturnCode blades failed to create the reference file for Toolkit reboot test`r"
            } 
           
            Write-Host " Setting AutoLogin...`r" 
            $ReturnCode = Invoke-WcsCommand -TargetList $BladeTargets -Command "Set-AutoLogin -User $($Global:BladeCredential.UserName) -Password ''$($Global:BladeCredential.GetNetworkCredential().Password)`''" 

            If (0 -ne $ReturnCode)
            {
                Throw "$ReturnCode blades failed to setup autologin"
            }
            #-------------------------------------------------------------------------------------------------------------------------------------
            #  Cycle-OsReboot: Inputs   = NumberOfCycles, RefConfig, LogDirectory, IncludeSelFile,ExcludeSelFile,IncludeEventFile,ExcludeEventFile
            #                             MaxBootTime, StopOnFail, CompareRefOnly
            #                  Return   = 0 on success, non-zero on failure (for each cycle)
            #
            #                  Function = Executes NumberOfCycles reboots
            #                  Function = On each boot checks against a configuration file. If RefConfig not specified uses default
            #                  Function = If CompareRefOnly specified does configuration with -OnlyRefDevices switch so can use generic recipe
            #                  Function = Creates logs in the LogDirecotry
            #                  Function = Stops cycle testing on failure if StopOnFail specified
            #                  Function = Logs a failure if boot time greater than MaxBootTime
            #
            #                  Function = Exclude or include BMC SEL and/or Windows System Events as errors
            #
            #                  Function = Reports IncludeSelFile missing and returns 1  (NOTE: error messages checked locally on blade)
            #                  Function = Reports ExcludeSelFile missing and returns 1
            #                  Function = Reports IncludeEventFile missing and returns 1
            #                  Function = Reports ExcludeEventFile missing and returns 1
            #-------------------------------------------------------------------------------------------------------------------------------------
            # Test basic functionality
            #------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0051"
            $TestDetails.Value.Name           = "Cycle-OsReboot basic functionality" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 2 -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 0  -ExpectedPass 2

            # Test with StopOnFail and RefConfig inputs (test passes so won't stop)
            #------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0052"
            $TestDetails.Value.Name           = "Cycle-OsReboot with StopOnFail, RefConfig, and LogDirectory" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 1 -StopOnFail -LogDirectory {0} -RefConfig Reference" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 0  -ExpectedPass 1

            # Test with StopOnFail and inject error so test stops.  Use IncludeSel input to inject error
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0053"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify StopOnFail.  Inject error with IncludeSel" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-FAIL" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 2 -StopOnFail -LogDirectory {0} -IncludeSel $WCS_REF_DIRECTORY\errorfiles\IncludeBootSel.xml" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 1  -ExpectedPass 0

            # Test with IncludeEvent to inject a failure
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0054"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify IncludeEvent" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-FAIL" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 1 -LogDirectory {0}  -IncludeEvent $WCS_REF_DIRECTORY\errorfiles\IncludeOsBootEvent.xml" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 1  -ExpectedPass 0

            # Test with -ExcludeEvent.  Must inject an error prior to test so the test can exlude it
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0056"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify ExcludeEvent" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 1 -LogDirectory {0}  -ExcludeSel $WCS_REF_DIRECTORY\errorfiles\ExcludeMemorySel.xml" -f $TestDetails.Value.LogDirectory)

            $ReturnCode = Copy-WcsFile -TargetList $BladeTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -LocalFile "a_InjectWhea.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

            If (0 -ne $ReturnCode)
            {
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder"      
                $TestDetails.Value.FailedTests++ 
            }
            Else
            {
                CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 0 -ExpectedPass 1
            }
            Remove-WcsRemoteFile -TargetList $BladeTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\a_*.bat"      | Out-Null
            Remove-WcsRemoteFile -TargetList $BladeTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle*.bat"   | Out-Null

            # Test MaxBootTime by setting it lower then real boot time
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0057"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify max boot time" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-FAIL" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 2 -LogDirectory {0} -MaxBootTime 0.5 " -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 2  -ExpectedPass 0

            # Test Run Command by running QuickStress on each cycle
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0058"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify run command" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 2 -LogDirectory {0} -Run ''Run-QuickStress -Time 1''" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*3)  -ExpectedFail 0  -ExpectedPass 2

            # Test with -CompareRefOnly and -RefConfig option and different configuration file
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0059"
            $TestDetails.Value.Name           = "Cycle-OsReboot verify CompareRefOnly using RefConfig" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-OsReboot\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-OsReboot -NumberOfCycles 2 -StopOnFail -LogDirectory {0} -RefConfig ShortReference -CompareRefOnly" -f $TestDetails.Value.LogDirectory)

            $ReturnCode = Copy-WcsFile -TargetList $BladeTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -LocalFile "ShortReference.config.xml" -RemoteDirectory $WCS_CONFIGURATION_DIRECTORY
            If (0 -ne $ReturnCode)
            {
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to configuration folder"      
                $TestDetails.Value.FailedTests++ 
            }
            Else
            {
                CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 0  -ExpectedPass 2
            }

            Return 0
        }
        Catch
        {
            $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
            #----------------------------------------------
            # Take action (do nothing if SilentlyContinue)
            #---------------------------------------------- 
            If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
            ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }

            Return $WCS_RETURN_CODE_UNKNOWN_ERROR
        }
    }
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to be called from a jump box to execute multiple tests of the Cycle-WcsBladePower command on one or more 
    # ... remote boxes
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-RemotePowerCycleCommand()
    { 
        [CmdletBinding()] 

        Param
        (
            [Parameter(Mandatory=$true)]   [ref]     $TestDetails,
            [Parameter(Mandatory=$true)]             $BladeTargetsCm,
            [Parameter(Mandatory=$true)]             $BladeTargetsSlots
        )
        Try
        {
            #-------------------------------------------------------
            # Get calling details for debug
            #-------------------------------------------------------
            $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

            #-------------------------------------------------------
            # Setup vars
            #-------------------------------------------------------    
            [int] $WaitDelayInSeconds  = (60*12)  # 12 minutes per cycle

            #-------------------------------------------------------
            # PRE-TEST SETUP
            #-------------------------------------------------------    
            CompatibilityTest-TestBox " PRE-TEST SETUP - REMOTE CYCLE-WCSBLADEPOWER"

            Write-Host " Creating reference configuration files...`r"

            $ReturnCode = Invoke-WcsCommand -TargetList $TestDetails.Value.RemoteTargets -Command "Log-WcsConfig Reference" 

            If (0 -ne $ReturnCode)
            {
                Throw  "$ReturnCode blades failed to create the reference file for Toolkit reboot test`r"
            } 
 
            Write-Host " Creating bad reference configuration files...`r"

            $ReturnCode = Invoke-WcsCommand -TargetList $TestDetails.Value.RemoteTargets -Command '$src=gcfg reference;$src.WcsConfig.System.ComputerName.Value=$WCS_NOT_AVAILABLE;lcfg -Config $src -File BadReference' 

            If (0 -ne $ReturnCode)
            {
                Throw  "$ReturnCode blades failed to create bad reference file for Toolkit reboot test`r"
            } 

            Write-Host "`r`n Setting AutoLogin...`r" 
            $ReturnCode = Invoke-WcsCommand -TargetList $TestDetails.Value.RemoteTargets -Command "Set-AutoLogin -User $($Global:BladeCredential.UserName) -Password ''$($Global:BladeCredential.GetNetworkCredential().Password)''" 

            If (0 -ne $ReturnCode)
            {
                Throw "$ReturnCode blades failed to setup autologin"
            }

            Write-Host "` Copying files blade startup folder`r`n`r" 

            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\a_*.bat"          | Out-Null
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle-wcs*.bat"   | Out-Null

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir "$WCS_REF_DIRECTORY\CycleBatFiles" -localFile "Cycle-WcsStartup.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

            If (0 -ne $ReturnCode)
            {
                Throw "Could not copy file to startup folder on $ReturnCode blades"       
            }

            #-------------------------------------------------------------------------------------------------------------------------------------
            #  Cycle-WcsBladePower: Inputs   = NumberOfCycles, LogDirectory, OnTimeInSec, OffTimeInSec, BladeList, FullPower, StopOnFail
            #                                   OffTimeStartTimeInSec,   OffTimeEndTimeInSec, OffTimeIncrementInMs  
            #                       Return   = 0 on success, non-zero on failure (for each cycle)  
            #                       
            #  Cycle-WcsCheck:      Inputs   = LogDirectory, IncludeSelFile,ExcludeSelFile,IncludeEventFile,ExcludeEventFile
            #                                   Run, CompareRefOnly
            #                       Return   = 0 on success, non-zero on failure (for each cycle)
            #

            #                  Function = Executes NumberOfCycles reboots
            #                  Function = On each boot checks against a configuration file. If RefConfig not specified uses default
            #                  Function = If CompareRefOnly specified does configuration with -OnlyRefDevices switch so can use generic recipe
            #                  Function = Creates logs in the LogDirecotry
            #                  Function = Stops cycle testing on failure if StopOnFail specified
            #                  Function = Logs a failure if boot time greater than MaxBootTime
            #
            #                  Function = Exclude or include BMC SEL and/or Windows System Events as errors
            #
            #                  Function = Reports IncludeSelFile missing and returns 1  (NOTE: error messages checked locally on blade)
            #                  Function = Reports ExcludeSelFile missing and returns 1
            #                  Function = Reports IncludeEventFile missing and returns 1
            #                  Function = Reports ExcludeEventFile missing and returns 1
            #-------------------------------------------------------------------------------------------------------------------------------------
 
            # Test basic functionality
            #------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0101"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower basic functionality" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 2 -BladeList $BladeTargetsSlots -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 0  -ExpectedPass 2  -PowerCycle -CmTarget $BladeTargetsCm

            # Test with StopOnFail and RefConfig inputs (test passes so won't stop)
            #------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0102"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower with StopOnFail and Full" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -Num 2 -Full -Stop -LogDirectory {0} -BladeList $BladeTargetsSlots"  -f $TestDetails.Value.LogDirectory)

            CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*2)  -ExpectedFail 0  -ExpectedPass 2  -PowerCycle -CmTarget $BladeTargetsCm
 
            # Test with StopOnFail and inject error so test stops.  Use IncludeSel input to inject error in Cycle-WcsCheck
            #---------------------------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0103"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower verify StopOnFail.  Inject error with IncludeSel" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-FAIL" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 2 -BladeList $BladeTargetsSlots -StopOnFail -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle-wcs*.bat"   | Out-Null
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\a_*.bat"          | Out-Null

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -localFile "Cycle-WcsIncludeSel.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

            If (0 -ne $ReturnCode)
            {  
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder`r"      
                $TestDetails.Value.FailedTests++                  
            }
            Else
            {
                CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 1  -ExpectedPass 0 -PowerCycle -CmTarget $BladeTargetsCm  
            }
 
            # Test with IncludeEvent to inject a failure
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0104"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower verify IncludeEvent" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-FAIL" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 1 -BladeList $BladeTargetsSlots -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle-wcs*.bat"   | Out-Null

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -localFile "Cycle-WcsIncludeEvent.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 
            If (0 -ne $ReturnCode)
            {  
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder`r"      
                $TestDetails.Value.FailedTests++                  
            }
            Else
            {
                CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 1  -ExpectedPass 0 -PowerCycle -CmTarget $BladeTargetsCm
            }
 
            # Test with -ExcludeSel.  Must inject an error prior to test so the test can exlude it
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0105"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower verify ExcludeSel" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 1 -BladeList $BladeTargetsSlots -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle-wcs*.bat"   | Out-Null

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -localFile "Cycle-WcsExcludeSel.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

            If (0 -ne $ReturnCode)
            {
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder for test  `r"      
                $TestDetails.Value.FailedTests++    
            }
            Else
            {
                $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -LocalFile "a_InjectMemorySel.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

                If (0 -ne $ReturnCode)
                {
                    Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder  `r"      
                    $TestDetails.Value.FailedTests++ 
                }
                Else
                {
                    CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 0 -ExpectedPass 1 -PowerCycle -CmTarget $BladeTargetsCm
                }
            }
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\a_*.bat"      | Out-Null
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle*.bat"   | Out-Null

 

            # Test Run Command by running QuickStress on each cycle
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0106"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower verify run command" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 2 -BladeList $BladeTargetsSlots -LogDirectory {0} " -f $TestDetails.Value.LogDirectory)

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -localFile "Cycle-WcsRunStress.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

            If (0 -ne $ReturnCode)
            {  
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder`r"      
                $TestDetails.Value.FailedTests++                  
            }
            Else
            {
                CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec ($WaitDelayInSeconds*3)  -ExpectedFail 0 -ExpectedPass 2 -PowerCycle -CmTarget $BladeTargetsCm  
            }
 
            # Test with -CompareRefOnly and -RefConfig option and different configuration file
            #---------------------------------------------------------------------------------------------
            $TestDetails.Value.ID             = "ID-REMOTE-0107"
            $TestDetails.Value.Name           = "Cycle-WcsBladePower verify CompareRefOnly" 
            $TestDetails.Value.LogDirectory   = ("$WCS_RESULTS_DIRECTORY\Cycle-WcsBladePower\{0}-PASS" -f $TestDetails.Value.ID)
            $TestDetails.Value.Command        = ("Cycle-WcsBladePower -NumberOfCycles 1 -StopOnFail -BladeList $BladeTargetsSlots  -LogDirectory {0}" -f $TestDetails.Value.LogDirectory)

            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle-wcs*.bat"   | Out-Null

            $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -LocalFile "Reference.config.xml" -RemoteDirectory $WCS_CONFIGURATION_DIRECTORY
 
            If (0 -ne $ReturnCode)
            {
                Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to configuration folder for test  `r"      
                $TestDetails.Value.FailedTests++    
            }
            Else
            {
                $ReturnCode = Copy-WcsFile -TargetList $TestDetails.Value.RemoteTargets -LocalDir $COMPATIBILITY_TEST_DIRECTORY -LocalFile "Cycle-WcsShortConfig.bat" -RemoteDirectory $WCS_OS_STARTUP_DIRECTORY 

                If (0 -ne $ReturnCode)
                {
                    Write-Host -ForegroundColor Red "TEST SETUP ERROR: Could not copy file to startup folder  `r"      
                    $TestDetails.Value.FailedTests++ 
                }
                Else
                {
                    CompatibilityTest-RemoteCycleCommands -TestDetails $TestDetails  -WaitTimeInSec $WaitDelayInSeconds  -ExpectedFail 0 -ExpectedPass 1 -PowerCycle -CmTarget $BladeTargetsCm
                }
            }
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\a_*.bat"      | Out-Null
            Remove-WcsRemoteFile -TargetList $TestDetails.Value.RemoteTargets -RemoteFile "$WCS_OS_STARTUP_DIRECTORY\cycle*.bat"   | Out-Null
 
            Return 0
        }
        Catch
        {
            $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
            #----------------------------------------------
            # Take action (do nothing if SilentlyContinue)
            #---------------------------------------------- 
            If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
            ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }

            Return $WCS_RETURN_CODE_UNKNOWN_ERROR
        }
    }
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the error handling of a command.  It checks the return value and the error message displayed on the
    # ...console
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-CommandError()
    {

        Param
        (
            [Parameter(Mandatory=$true)] [ref]       $TestDetails,
            [Parameter(Mandatory=$true)] [string]    $TestName,
            [Parameter(Mandatory=$true)] [string]    $TestId,
            [Parameter(Mandatory=$true)] [string]    $TestCommand,
            [Parameter(Mandatory=$true)] [string[]]  $MatchStrings,
            [Parameter(Mandatory=$true)] [int[]]     $MatchLines,
                                         [switch]    $ReturnNull,
                                         [int]       $ReturnCode=0
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
            Write-Host "
 This test runs command with invalid input parameters and verifies:

    1) Command displays correct error message
    2) Command returns null
    
 Full Command: $TestCommand

 Command's Console Output --->`r"  
       
            #--------------------------------------------------------------------------------------
            # Save the end point of transcript file before command so can get the command's output
            # ...Make sure a line return proceeds this command to avoid partial lines
            #--------------------------------------------------------------------------------------
            [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$WCS_TRANSCRIPT_COPY,$true)
            $TranscriptStart = ([system.io.file]::ReadAllLines($WCS_TRANSCRIPT_COPY) ).Length  

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$TestCommand "
 
            #--------------------------------------------------------------------------------------
            # Save the command's output
            #--------------------------------------------------------------------------------------
            [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$WCS_TRANSCRIPT_COPY  ,$true)
            $FileContent = [system.io.file]::ReadAllLines($WCS_TRANSCRIPT_COPY)  

            If ($FileContent.Length -gt  $TranscriptStart)
            {
                $FileContent = ($FileContent[$TranscriptStart..($FileContent.Length-1)])
            }
            Else
            {
                Throw "No console output found"
            }

            Write-Host "<--- End Console Output`r`n"
            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($ReturnNull -and ($null -ne  $ReturnedValue))
            {
                Throw "Return value incorrect.  Expected null but received $ReturnedValue"
            }

            If (-Not $ReturnNull -and ($ReturnCode -ne  $ReturnedValue))
            {
                Throw "Return value incorrect.  Expected $ReturnCode but received $ReturnedValue"
            }
            #--------------------------------------------------------------------------------------
            # Verify console output 
            #-------------------------------------------------------------------------------------- 
            If ($MatchLines.Count -ne $MatchStrings.Count)
            {
                Throw "Number of matchlines different then matchstrings"
            }

            For ($Index=0;$Index-lt$MatchLines.Count;$Index++)
            {
                If ($MatchLines[$Index] -ge $FileContent.Count)
                {
                    Throw ("Line {0} exceeds lines in console output {1}" -f $MatchLines[$Index],$FileContent.Count)
                }
                If ($FileContent[ $MatchLines[$Index] ] -cnotlike $MatchStrings[$Index])
                {
                    Throw ("Line {0} doesn't match. Expected '{1}' read '{2}'" -f $MatchLines[$Index],$MatchStrings[$Index],$FileContent[ $MatchLines[$Index] ] )
                }
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the return value and console output of a View-Wcs* command.
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-ViewCommand()
    {

        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
            [string]  $Filename,
            [switch]  $ViewSel,
            [switch]  $ViewSelNoDecode,
            [switch]  $CommonRef,
            [switch]  $ViewHealth

        )

        Try
        {
            $TestStartTime     = (get-date)

            #-----------------------------------------------------------------
            # Define the log file and the reference file based on CommonRef
            #-----------------------------------------------------------------
            If ($CommonRef) 
            { 
                New-Item -Path       ("{0}\CommonViewCommandLogs" -f $TestDetails.Value.TestDirectory) -ItemType Container -Force -ErrorAction SilentlyContinue | Out-Null
                $TestLogFile       = ("{0}\CommonViewCommandLogs\$Filename.log" -f $TestDetails.Value.TestDirectory) 

                $ViewReferenceFile = "$COMPATIBILITY_TEST_DIRECTORY\$Filename.log" 
            }
            Else            
            { 
                New-Item -Path      ("{0}\ViewCommandLogs" -f $TestDetails.Value.TestDirectory) -ItemType Container -Force -ErrorAction SilentlyContinue | Out-Null
                $TestLogFile       = ("{0}\ViewCommandLogs\$Filename.log" -f $TestDetails.Value.TestDirectory) 

                $ViewReferenceFile = "$TEST_REF_FILE_DIRECTORY\$Filename.log"      
            }

            #------------------------
            # Display test header
            #------------------------
            CompatibilityTest-TestHeader -ScriptFile $TestDetails.Value.ScriptFile -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test runs '$TestCommand' and verifies:

   1) Command completes without error
   2) Command returns null
   3) Console output matches expected output
    
 Console output log   : $TestLogFile
 Reference output log : $ViewReferenceFile

 $TestCommand's Console Output --->`r"  
       
            #--------------------------------------------------------------------------------------
            # Save the end point of transcript file before command so can get the command's output
            # ...Make sure a line return proceeds this command to avoid partial lines
            #--------------------------------------------------------------------------------------
            [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$WCS_TRANSCRIPT_COPY,$true)
            $TranscriptStart = ([system.io.file]::ReadAllLines($WCS_TRANSCRIPT_COPY) ).Length  

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$TestCommand -ErrorAction Stop"
 
            #--------------------------------------------------------------------------------------
            # Save the command's output
            #--------------------------------------------------------------------------------------
            [system.io.file]::Copy($TestDetails.Value.TranscriptFile,$WCS_TRANSCRIPT_COPY  ,$true)
            $FileContent = [system.io.file]::ReadAllLines($WCS_TRANSCRIPT_COPY)  
            
            Write-Host "<--- End Console Output`r`n"
                        
            If ($FileContent.Length -gt  $TranscriptStart)
            {
                $FileContent = ($FileContent[$TranscriptStart..($FileContent.Length-1)])
            }
            Else
            {
                Throw "No console output found"
            }

            If ($ViewSel)
            {
                For ($Loop=0;$Loop-lt $FileContent.Length; $Loop++)
                {
                    $FileContent[$Loop] = $FileContent[$Loop].Substring($FileContent[$Loop].IndexOf(']')+1).TrimStart()
                }
            }

            ElseIf ($ViewSelNoDecode)
            {
                For ($Loop=0;$Loop-lt $FileContent.Length; $Loop++)
                {
                    If ($FileContent[$Loop].Length -gt 41) { $FileContent[$Loop] = $FileContent[$Loop].Substring(41).TrimStart() }
                }
            }
            ElseIf ($ViewHealth)
            {
                For ($Loop=0;$Loop-lt $FileContent.Length; $Loop++)
                {
                     If (($FileContent[$Loop].IndexOf('[') -ne -1) -and ($FileContent[$Loop].IndexOf(']') -ne -1)) 
                     { 
                        $FileContent[$Loop] = $FileContent[$Loop].Remove($FileContent[$Loop].IndexOf('['), $FileContent[$Loop].IndexOf(']')-$FileContent[$Loop].IndexOf('[')+1).TrimEnd() 
                     }
                }
            }
            #--------------------------------------------------------------------------------------
            # Save console output to the log file
            #--------------------------------------------------------------------------------------
            [system.io.file]::WriteAllLines($TestLogFile, $FileContent)

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($null -ne  $ReturnedValue)
            {
                Throw "Return value incorrect.  Expected null but received $ReturnedValue"
            }
            #--------------------------------------------------------------------------------------
            # Verify console output 
            #--------------------------------------------------------------------------------------  
            If (-Not (Test-Path $ViewReferenceFile))
            {
                Throw "Could not verify console output because reference log missing: $ViewReferenceFile"
            }
            Else
            {
                $CompareConsole = Compare-Object $(Get-Content $ViewReferenceFile  -Encoding Ascii) $(Get-Content $TestLogFile -Encoding Ascii)

                If ($null -ne $CompareConsole)
                {
                    Write-Host " Mismatch found between console output => and reference file <=`r`n`r" -ForegroundColor Red

                    $CompareConsole | Format-Table -HideTableHeaders  

                    Throw "Command console output does not match reference file"
                }
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of a simple toolkit command
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-SimpleCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
            [int]     $ReturnCode  = 0,
                      $ReturnHealth = $null,
            [switch]  $ReturnNull,
            [switch]  $ReturnArray,
            [string]  $Directory ='',
            [string]  $MoveTo = ''
        )

        Try
        {
            $TestStartTime     = (get-date)

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $TestDetails.Value.ScriptFile -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #---------------------
            # Display test details
            #---------------------
            Write-Host  "
 This test runs a command and verifies:

   1) Command completes without error
   2) Command returns the expected value (return code, null, or object)

 Full Command : $TestCommand`r`n`r"   

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$TestCommand -ErrorAction Stop"

            If ($ReturnArray)
            {
                $ReturnedValue  =  $ReturnedValue.Count
            }
            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If  (-not $ReturnNull  -and ($ReturnedValue -eq $null))
            {
                Throw "Command return value incorrect.  Expected $ReturnCode but received null" 
            }
            ElseIf ($ReturnNull)
            {
                If ($ReturnedValue -ne $null) { Throw "Command return value incorrect.  Expected null but received $ReturnedValue" }
            }
            ElseIf ($ReturnHealth -ne $null)
            {
                If ($ReturnedValue.ErrorCount -ne $ReturnHealth.ErrorCount)
                {
                    Throw ("Command return value incorrect.  Expected error count {0} but received {1}" -f $ReturnHealth.ErrorCount,$ReturnedValue.ErrorCount)
                }
                If ($ReturnedValue.FruErrors.Count -ne $ReturnHealth.FruCount)
                {
                    Throw ("Command return value incorrect.  Expected FRU error count {0} but received {1}" -f $ReturnHealth.FruCount,$ReturnedValue.FruErrors.Count)
                }
                If ($ReturnedValue.HardwareErrors.Count -ne $ReturnHealth.HwCount)
                {
                    Throw ("Command return value incorrect.  Expected hardware error count {0} but received {1}" -f $ReturnHealth.HwCount,$ReturnedValue.HardwareErrors.Count)
                }
                If ($ReturnedValue.DeviceManagerErrors.Count -ne $ReturnHealth.DevMgrCount)
                {
                    Throw ("Command return value incorrect.  Expected device mgr error count {0} but received {1}" -f $ReturnHealth.DevMgrCount,$ReturnedValue.DeviceManagerErrors.Count )
                }
            }
            ElseIf ($ReturnCode -ne  $ReturnedValue)
            {
                Throw "Command return value incorrect.  Expected $ReturnCode but received $ReturnedValue" 
            }
            #--------------------------------------------------------------------------------------
            # Move files
            #--------------------------------------------------------------------------------------
            If (($MoveTo -ne '') -and ($Directory -ne ''))
            {
                New-Item $MoveTo -Force -ErrorAction SilentlyContinue -ItemType Container | Out-Null
                Copy-Item   "$Directory\*" $MoveTo -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
                Remove-Item "$Directory"           -Recurse -Force -ErrorAction SilentlyContinue | Out-Null                
 
                Write-Host " All log files moved from default location to $MoveTo"
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
    #---------------------------------------------------------------------------------
    # Helper function that finds all expected files logged by Check-WcsError and Post-WcsTest
    #---------------------------------------------------------------------------------
    Function VerifyErrorFiles([string] $directory)
    {
        Try
        {     
            If ( -Not (CoreLib_IsWinPE))
            {       
                Return ((Test-Path "$Directory\System.evt") -And (Test-Path "$Directory\Application.evt") -And (Test-Path "$Directory\bmc-sel-entries.sel.log") `
                        -And (Test-Path "$Directory\bmc-sel-entries.decoded.sel.log")-And (Test-Path "$Directory\check-wcserror-summary.log"))

            }
            Else
            {
                Return ((Test-Path "$Directory\bmc-sel-entries.sel.log")  -And (Test-Path "$Directory\bmc-sel-entries.decoded.sel.log")-And (Test-Path "$Directory\check-wcserror-summary.log"))
            }
        }
        Catch
        {
            Return $false
        }
    }
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of a toolkit command and the file(s) it creates
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-CommandAndFiles()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
            [string]  $File              = '',
            [string]  $Directory         = '',
            [int]     $ReturnCode        = 0,
            [switch]  $CheckWcsError,
            [switch]  $PostWcsTest,
            [switch]  $SingleFile,
            [switch]  $CheckOverWrite,
            [switch]  $msinfo,
            [string]  $MoveTo = '',
            [switch]  $SEL,
            [switch]  $FRU,
            [switch]  $Health

        )

        Try
        {
            $TestStartTime     = (get-date)
            $DummyString       = "I'm a dummy file"

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #---------------------
            # Display test details
            #---------------------
            Write-Host  " This test runs a command and verifies:

                       `r   1) Command completes without error
                       `r   2) Command returns $ReturnCode
                       `r   3) Files are created as expected

                       `r Full Command : $TestCommand`r`n`r"   

            #--------------------------------------------------------------------------------------
            # Create bogus file if CheckOverWrite specified
            #--------------------------------------------------------------------------------------
            $FilePath = "$Directory\$File"


            If ($CheckOverWrite -and  $SingleFile)
            {
                New-Item -Path $Directory -ItemType Container -Force -ErrorAction SilentlyContinue | Out-Null
                Set-Content -Path $FilePath -Value $DummyString -ErrorAction Stop
            }
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$TestCommand -ErrorAction Stop"

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($ReturnCode -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected $ReturnCode but received null" }
                Else                          { Throw "Return value incorrect.  Expected $ReturnCode but received $ReturnedValue" }
            }

            #--------------------------------------------------------------------------------------
            # Verify logs
            #--------------------------------------------------------------------------------------
             If ($PostWcsTest)
            {
                    $LogDirectory = (Get-ChildItem "$WCS_RESULTS_DIRECTORY\$Directory\Post-Test\Check-WcsError*").FullName

                    If ($null -eq $LogDirectory)
                    {
                        Throw "Log directory not found: $WCS_RESULTS_DIRECTORY\$Directory\Post-Test\Check-WcsError*"
                    }

                    $CfgDirectory = (Get-ChildItem "$WCS_RESULTS_DIRECTORY\$Directory\Post-Test\Get-WcsConfig*").FullName

                    If ($null -eq $CfgDirectory)
                    {
                        Throw "Log directory not found: $WCS_RESULTS_DIRECTORY\$Directory\Post-Test\Get-WcsConfig*"
                    }
 
                    If (-NOT (Test-Path "$CfgDirectory\posttestconfig.config.xml"))
                    {
                        Throw "File not found: $CfgDirectory\posttestconfig.config.xml"
                    }

                If (-NOT (VerifyErrorFiles $LogDirectory))
                {
                    Throw "One or more files were not found in: $LogDirectory "
                }

            }
            ElseIf ($CheckWcsError)
            {
                If ($Directory -eq '')
                {

                    $LogDirectory  = "$WCS_RESULTS_DIRECTORY\Check-WcsError"

                    $TestDirectory = (Get-ChildItem "$LogDirectory\*").FullName

                    If (-NOT (VerifyErrorFiles $TestDirectory))
                    {
                        Throw "One or more files were not found in: $TestDirectory "
                    }
                }
                Else
                {
                    If (-NOT (VerifyErrorFiles $Directory))
                    {
                        Throw "One or more files were not found in: $Directory "
                    }
                }
            }
            ElseIf ($SingleFile)
            {
                If ((Test-Path $FilePath))
                {
                    If ($CheckOverWrite)
                    {
                        If ((Get-Content -Path $FilePath) -eq $DummyString)
                        {
                            Throw "Did not overwrite file"
                        }
                    }
                    Write-Host " Found configuration log file: $FilePath`r`n`r"
                }
                Else
                {
                    Throw "Did not find log file: $FilePath"
                }
            }
            ElseIf ($MsInfo)
            {
                $LogDirectory = "$WCS_RESULTS_DIRECTORY\log-msinfo32"
                $MsInfoDir    = (Get-ChildItem "$LogDirectory\*").FullName

                If ($MsInfoDir -eq $null)
                {
                    Throw "MS Info directory not found: $LogDirectory\*"
                }
 
                If (-NOT (Test-Path "$MsInfoDir\msinfo32.log"))
                {
                    Throw "File not found: $MsInfoDir\msinfo32.log"
                }           
            }
            ElseIf ($SEL)
            {

                $LogDirectory = "$WCS_RESULTS_DIRECTORY\log-WcsSel"

                $FileOne = "$LogDirectory\$File.sel.log"
                $FileTwo = "$LogDirectory\$File.decoded.sel.log"

                If ($LogDirectory -eq $null)
                {
                    Throw "Log-WcsSel directory not found: $LogDirectory"
                }
 
                If (-NOT (Test-Path $FileOne))
                {
                    Throw "File not found: $FileOne"
                }           
                If (-NOT (Test-Path $FileTwo))
                {
                    Throw "File not found: $FileTwo"
                }                
            }

            ElseIf ($Fru)
            {

                $LogDirectory = "$WCS_RESULTS_DIRECTORY\log-WcsFru"

                $FileOne = (Get-ChildItem "$LogDirectory\fruinfo*").FullName

                If ($LogDirectory -eq $null)
                {
                    Throw "Log-WcsFru directory not found: $LogDirectory"
                }
 
                If (-NOT (Test-Path $FileOne))
                {
                    Throw "File not found: $FileOne"
                }                          
            }
            ElseIf ($Health)
            {

                $LogDirectory = "$WCS_RESULTS_DIRECTORY\log-WcsHealth"

                $FileOne = (Get-ChildItem "$LogDirectory\health-*").FullName

                If ($LogDirectory -eq $null)
                {
                    Throw "Log-WcsHealth directory not found: $LogDirectory"
                }
 
                If (-NOT (Test-Path $FileOne))
                {
                    Throw "File not found: $FileOne"
                }                          
            }
            Else
            {
                $LogDirectory = $Directory
            }
            #--------------------------------------------------------------------------------------
            # Move files
            #--------------------------------------------------------------------------------------
            If ($MoveTo -ne '')
            {
                New-Item $MoveTo -Force -ErrorAction SilentlyContinue -ItemType Container | Out-Null

                If ($SingleFile)
                {
                    Copy-Item   $FilePath $MoveTo  -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
                    Remove-Item $FilePath                    -Force -ErrorAction SilentlyContinue | Out-Null                
                }
                Else
                {
                    Copy-Item   "$LogDirectory\*" $MoveTo -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
                    Remove-Item "$LogDirectory"           -Recurse -Force -ErrorAction SilentlyContinue | Out-Null                
                }
                Write-Host " All log files moved from default location to $MoveTo"
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of a Clear-WcsError and Pre-WcsTest
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-ClearErrorCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
            [string]  $Directory='',
            [switch]  $ClearWcsError,
            [switch]  $PreWcsTest

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
            Write-Host  "
 This test runs a command and verifies:

    1) Command completes without error
    2) Command returns 0
    3) BMC SEL and Windows System Event log are cleared

 Full Command : $TestCommand`r`n`r"   

            #--------------------------------------------------------------------------------------
            # Inject errors into BMC SEL and Windows Event Log
            #--------------------------------------------------------------------------------------
            CompatibilityTest-InjectTwoErrors

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$TestCommand -ErrorAction Stop" 

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
                Else                          { Throw "Return value incorrect.  Expected 0 but received $ReturnedValue" }
            }

            #--------------------------------------------------------------------------------------
            # Verify logs
            #--------------------------------------------------------------------------------------
            If ($PreWcsTest)
            {
                $LogDirectory = (Get-ChildItem "$WCS_RESULTS_DIRECTORY\$Directory\Pre-Test\Get-WcsConfig*").FullName

                If ($null -eq $LogDirectory)
                {
                    Throw "Log directory not found: $WCS_RESULTS_DIRECTORY\$Directory\Pre-Test\Get-WcsConfig*"
                }
 
                If (-NOT (Test-Path "$LogDirectory\pretestconfig.config.xml"))
                {
                    Throw "File not found: $LogDirectory\pretestconfig.config.xml"
                }

                $Directory = "$WCS_RESULTS_DIRECTORY\$Directory\Check-WcsError"
            }
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Check-WcsError -LogDirectory $Directory -ErrorAction Stop  

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
                Else                          { Throw "Return value incorrect.  Expected 0 but received $ReturnedValue" }
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

    Function CompatibilityTest-InjectTwoErrors()
    {
        If ( -Not (CoreLib_IsWinPE))
        {
            Write-EventLog -LogName System -Source Microsoft-Windows-WHEA-Logger -EventId 1222 -Message 'Test WHEA Message' -EntryType Warning  -ErrorAction Stop
        }


        $IpmiData = Invoke-WcsIpmi  0x44 @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA0,0,0) $WCS_STORAGE_NETFN 

        If ($IpmiData[0] -ne 0)
        {
            (Throw "InjectTwoErrors failed with IPMI completion code 0x{0:X2}" -f $IpmiData[0])
        }
    }
    #----------------------------------------------------------------------- 
    # Helper function to inject SEL entries
    #----------------------------------------------------------------------- 
    Function InjectErrorEntry()
    {  
        Param
        (
            [byte[]] $RequestData
        )
        Try
        {
            $IpmiData = Invoke-WcsIpmi 0x44 $RequestData  $WCS_STORAGE_NETFN 

            If ($IpmiData[0] -ne 0) 
            {
                Throw (" IPMI command returned {0}" -f $IpmiData[0])
            }
        }
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
        }
    }

    #-------------------------------------------------------------------------------------------------
    #  Injects errors into SEL for validation 
    #
    #  <<TO DO: Update to inject every single type of hardware error >>
    #-------------------------------------------------------------------------------------------------
    Function CompatibilityTest-InjectErrors()
    {
        Param
        (
            [switch] $NoPciE
        )

        If ( -Not (CoreLib_IsWinPE))
        {
            $NumberEventHwEntries      = 3
            $NumberBootEntries         = 2
            $NumberWheaEvents          = 2
            #-----------------------------------------------------------------------------------------
            # Inject corr and uncorrectable WHEA entries and bugcheck event 1001
            #-----------------------------------------------------------------------------------------
            Write-EventLog -LogName System -Source Microsoft-Windows-WHEA-Logger -EventId 1222 -Message 'Test WHEA Message' -EntryType Warning  -ErrorAction Stop
            Write-EventLog -LogName System -Source Microsoft-Windows-WHEA-Logger -EventId 1222 -Message 'Test WHEA Message' -EntryType Error    -ErrorAction Stop
            Write-EventLog -LogName System -Source Bugcheck -EventId 1001 -Message 'Test Bugcheck Message' -EntryType Error                     -ErrorAction Stop


            Write-EventLog -LogName System -Source 'Microsoft-Windows-Kernel-General' -EventId 12 -Message 'Test Message' -EntryType Information -ErrorAction Stop
            Write-EventLog -LogName System -Source 'Microsoft-Windows-Kernel-General' -EventId 12 -Message 'Test Message' -EntryType Information -ErrorAction Stop
           
        }
        Else
        {
            $NumberEventHwEntries       = 0
            $NumberBootEntries          = 0
            $NumberWheaEvents           = 0
        }
    

        If ($NoPCIe) 
        { 
            $PCIeErrors           = 0
            $SensorType0Cand13    = 34 #27 + 7
            $Sensor87andA1        = 24 
        
        }
        Else         
        { 
            $PCIeErrors           =  9
            $SensorType0Cand13    = 39   # 27 + 12
            $Sensor87andA1        = 29
        }


        $Sensor87                   = 24
        $SensorType0C               = 27

        $NumberSelHwEntries         = 69 + $PCIeErrors

        $NumberSelEntries           = $NumberSelHwEntries + 1
        $NumberErrorEntries         = $NumberSelHwEntries + $NumberEventHwEntries

        $NumberEntriesWithBoot      = $NumberErrorEntries   + $NumberBootEntries  
        $NumberHwEntriesNoWhea      = $NumberErrorEntries   - $NumberWheaEvents
                 
        $NumberEntries              = $NumberErrorEntries + 1   # One entry for SEL cleared
          
        #========================================================================
        # Standard IPMI entries likely HW errors (34 errors)
        #========================================================================

        #----------------------------------------------------------------------- 
        # Sensor Type 0x01 - Temperature errors (out of threshold, in threshold)
        #----------------------------------------------------------------------- 
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x01,0xCC,0x01, 0x50,0xB9,0xE1)
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x01,0xCC,0x81, 0x50,0xB9,0xA9)
 
        #-------------------------------------------------------------------
        # Sensor Type 0x02 - Voltage errors (out of threshold, in threshold)
        #-------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x02,0xCC,0x01, 0x50,0xB9,0xE1)
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x02,0xCC,0x81, 0x50,0xB9,0xA9)
 
        #-------------------------------------------------------------------
        # Sensor Type 0x03 - Current errors (out of threshold, in threshold)
        #-------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x03,0xCC,0x01, 0x50,0xB9,0xE1)
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x03,0xCC,0x81, 0x50,0xB9,0xA9)

        #-------------------------------------------------------------------
        # Sensor Type 0x04 - Fan errors (out of threshold, in threshold)
        #-------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x04,0xCC,0x01, 0x50,0xB9,0xE1)
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0x20,0,4,  0x04,0xCC,0x81, 0x50,0xB9,0xA9)


        #-----------------------------------------------------------------------------------
        # Sensor Type 0x07 - Processors errors (unless Event Data 1 lowest nibble 7 or 9)
        #-----------------------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x0,0xFF,0xFF)   #IERR
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x1,0xFF,0xFF)   #Thermtrip
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x2,0xFF,0xFF)   #FRB1
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x3,0xFF,0xFF)   #FRB2
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x4,0xFF,0xFF)   #FRB3
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x5,0xFF,0xFF)   #Config error
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0x6,0xFF,0xFF)   #SMBIOS
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0xA,0xFF,0xFF)   #Throttled
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0xB,0xFF,0xFF)   #Uncorrectable MCERR
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xCC,0x6F, 0xC,0xFF,0xFF)   #Correctable MCERR

        #-----------------------------------------------------------------------------------
        # Sensor Type 0x0C - Memory errors  
        #-----------------------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0xCC,0x6F, 0x0,0xFF,0xFF)   #Correctable ECC
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0xCC,0x6F, 0x1,0xFF,0xFF)   #UnCorrectable ECC
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0xCC,0x6F, 0x3,0xFF,0xFF)   # All others displayed as Memory event
    
        #-----------------------------------------------------------------------------------
        # Sensor Type 0x0D - Drive errors  
        #-----------------------------------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0D,0xCC,0x6F, 0x1,0xFF,0xFF)   # Drive fault
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0D,0xCC,0x6F, 0x2,0xFF,0xFF)   # Predictive failure
    
        #---------------------------------------------------------
        # Sensor Type 0Fh - System Firmware Progress (POST error)
        #---------------------------------------------------------    
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0F,0x9E,0x6F, 0xA0,0x55,0xAA)  

        #----------------------------------------------------
        # Sensor Type 13h - Critical Interupt
        #----------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x4,0xFF,0xFF)   # PCI PERR
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x5,0xFF,0xFF)   # PCI SERR
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x7,0xFF,0xFF)   # Bus corr
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x8,0xFF,0xFF)   # Bus Uncorr
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x9,0xFF,0xFF)   # NMI
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0xA,0xFF,0xFF)   # Bus fatal
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xCC,0x6F, 0x1,0xFF,0xFF)   # All others critical interupt

        #----------------------------------------------------
        # Sensor Type 19h - Chipset
        #---------------------------------------------------- 
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x19,0xCC,0x6F, 0x0,0x0,0x0)   # Soft power failure
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x19,0xCC,0x6F, 0x1,0x0,0x0)   # Chipset therm trip

        #---------------------------------------------------- 
        # Sensor Type 23h - Watchdog timer
        #---------------------------------------------------- 
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x23,0xCC,0x6F, 0xC1,0x01,0xFF)  # All are watchdog timer

        #========================================================================
        # Standard WCS entries likely HW errors (44)
        #========================================================================


        #-----------------------------------------------------------------------------------
        # Sensor Type 0x0C - Memory errors - Inject 12 corr and 12 uncorr ECC errors 
        #-----------------------------------------------------------------------------------
        For ($Dimm=1;$Dimm -le 12; $Dimm++)
        {
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA0,0,$Dimm)  
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA1,0,$Dimm)  
        }

        #-----------------------------------------------------------
        # Inject Processor QPI Errors - Uncorrectable 
        #-----------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0x9D,0x6F, 0xAB,0x0,0x1)  

        #-----------------------------------------------------------
        # Inject Processor QPI Errors - Correctable  
        #---------------------------------------------------------- 
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0x9D,0x6F, 0xAC,0x0,0x1)  

        #-------------------------------
        # Inject Processor 1 errors
        #-------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0xD5,0x6F, 0x0,0xFF,0xFF)  

        #-----------------------------------------------------------
        # Inject Processor QPI Errors - Uncorrectable 
        #-----------------------------------------------------------
        InjectErrorEntry   @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0x9D,0x6F, 0xAB,0x0,0x12)

        #-----------------------------------------------------------
        # Inject Processor QPI Errors - Correctable  
        #-----------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x07,0x9D,0x6F, 0xAC,0x0,0x12)  

        #-----------------------------------------------------------
        # Inject NVDIMM error  
        #-----------------------------------------------------------
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA0,0x0,0x1)  # NVDIMM controller error
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA1,0x0,0x1)  # NVDIMM restore failed
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA3,0x0,0x1)  # NVDIMM backup failed
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA4,0x0,0x1)  # NVDIMM erase failed
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA6,0x0,0x1)  # NVDIMM arm failed
        InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0xD2,0x5,0x6F, 0xA9,0x0,0x1)  # All others NVDIMM error

        If (-not $NoPCIe) 
        { 

            #------------------------------------
            # Inject PCIe error - NIC
            #------------------------------------
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xA1,0x6F, 0xA7,0x9,0x00)

            InjectErrorEntry  @(0,0,0xc0, 0,0,0,0, ($IANA_ENTERPRISE_ID -band 0xFF), ((($IANA_ENTERPRISE_ID -band 0xFF00) / 0x100)-band 0xFF) ,0,  0xB3, 0x15, 0x00,0x11,0x70,0x71)

            #------------------------------------
            # Inject PCIe error - HBA
            #------------------------------------
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xA1,0x6F, 0xA7,0x9,0x00)

            InjectErrorEntry  @(0,0,0xc0, 0,0,0,0, ($IANA_ENTERPRISE_ID -band 0xFF), ((($IANA_ENTERPRISE_ID -band 0xFF00) / 0x100)-band 0xFF) ,0,  0x00, 0x10, 0x00,0x11,0x70,0x71)

            #------------------------------------
            # Inject PCIe error - FPGA
            #------------------------------------
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xA1,0x6F, 0xA7,0x9,0x00)

            InjectErrorEntry  @(0,0,0xc0, 0,0,0,0, ($IANA_ENTERPRISE_ID -band 0xFF), ((($IANA_ENTERPRISE_ID -band 0xFF00) / 0x100)-band 0xFF) ,0,  0x55, 0x55, 0x00,0x11,0x70,0x71)


            #-------------------------------------
            # Inject PCIe error - Old WCS format
            #-------------------------------------
            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xA1,0x6F, 0xA7,0x9,0x00)

            InjectErrorEntry  @(0,0,2, 0,0,0,0, 0,1,4,  0x13,0xA1,0x6F, 0xA7,0x70,0x71)

            InjectErrorEntry  @(0,0,0xc0, 0,0,0,0, ($IANA_ENTERPRISE_ID -band 0xFF), ((($IANA_ENTERPRISE_ID -band 0xFF00) / 0x100)-band 0xFF) ,0,  0xB3, 0x15, 0x00,0x11,0xFF,0xFF)

        }
        #========================================================================
        ## OEM specific errors injected here
        #========================================================================

        Return @{ 
                   All               = $NumberEntries;
                   HwSelOnly         = $NumberSelHwEntries;
                   HwEventOnly       = $NumberEventHwEntries;
                   HwNoWHEA          = $NumberHwEntriesNoWhea;
                   HwAddBoot         = $NumberEntriesWithBoot;
                   Sensor87          = $sensor87;
                   SensorType0C      = $SensorType0c;
                   Sensor87andA1     = $Sensor87andA1;
                   SensorType0Cand13 = $SensorType0Cand13
               }
    }
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of the Update-WcsFruChecksum command
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-UpdateFruChecksum()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [int16]   $Checksumoffset,
            [int16]   $ChecksumStartOffset,
            [int16]   $ChecksumEndOffset
        )

        Try
        {
            $TestStartTime     = Get-Date
            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test runs Update-WcsFruChecksum and verifies:

     1) Command completes without error
     2) Command updates checksum to correct value
     3) Command returns 0
   
 Full Command : Update-WcsFruChecksum -ChecksumOffset  $ChecksumOffset  -ChecksumStartOffset  $ChecksumStartOffset -ChecksumEndOffset $ChecksumEndOffset`r`n`r"  

            #--------------------------------------------------------------------------------------
            # Get the current FRU data
            #--------------------------------------------------------------------------------------
            $CurrentFruData = Get-WcsFruData -ErrorAction Stop

            $OriginalChecksum = $CurrentFruData[$ChecksumOffset] 
            #--------------------------------------------------------------------------------------
            # Change checksum value and write back
            #--------------------------------------------------------------------------------------
            If (0x55 -eq $OriginalChecksum) { $CurrentFruData[$ChecksumOffset]  = 0xAA }
            Else                            { $CurrentFruData[$ChecksumOffset]  = 0x55 }

            $BytesWritten = Update-WcsFruData -FruOffset 0 -DataToWrite $CurrentFruData -ErrorAction Stop

            #--------------------------------------------------------------------------------------
            # Update checksum value and compare
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Update-WcsFruChecksum -ChecksumOffset  $ChecksumOffset  -ChecksumStartOffset  $ChecksumStartOffset -ChecksumEndOffset $ChecksumEndOffset

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
                Else                          { Throw "Return value incorrect.  Expected 0 but received $ReturnedValue" }
            }
            #--------------------------------------------------------------------------------------
            # Verify checksum back to original value
            #--------------------------------------------------------------------------------------
            $CurrentFruData    = Get-WcsFruData -ErrorAction Stop

            If ($OriginalChecksum -ne $CurrentFruData[$ChecksumOffset])
            {
                Throw ("Checksum incorrect.  Expected {0} read {1}" -f $OriginalChecksum,$CurrentFruData[$ChecksumOffset])
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of the Update-WcsFru  command
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-UpdateFruCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $TestCommand,
                      $Match
        )

        Try
        {
            $TestStartTime     = Get-Date
            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test runs Update-WcsFru and verifies:

    1) Command completes without error
    2) Command returns 0
    3) FRU fields updated as expected

 Full Command : $TestCommand `r`n`r"
            #--------------------------------------------------------------------------------------
            # Read current FRU values
            #--------------------------------------------------------------------------------------
            #$CurrentFru = IpmiLib_GetBmcFru

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression ("$TestCommand -ErrorAction Stop  ")

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Return value incorrect.  Expected 0 but received null" }
                Else                          { Throw "Return value incorrect.  Expected 0 but received $ReturnedValue" }
            }
            #--------------------------------------------------------------------------------------
            # Verify update changed the fields
            #--------------------------------------------------------------------------------------  
            $NewFru = IpmiLib_GetBmcFru

            If ($NewFru.ChassisSerial.Value -ne $Match.ChassisSerial)
            {
                Throw (" Chassis Serial Number '{0}' doesn't match expected '{1}'" -f $NewFru.ChassisSerial.Value,$Match.ChassisSerial)    
            }
            If ($NewFru.ChassisCustom1.Value -ne $Match.ChassisCustom1)
            {
                Throw (" Chassis Custom1 '{0}' doesn't match expected '{1}'" -f $NewFru.ChassisCustom1.Value,$Match.ChassisCustom1)    
            }        
            If ($NewFru.ChassisCustom2.Value -ne $Match.ChassisCustom2)
            {
                Throw (" Chassis Custom2 '{0}' doesn't match expected '{1}'" -f $NewFru.ChassisCustom2.Value,$Match.ChassisCustom2)    
            }

            If ($NewFru.BoardMinutes.Value -ne $Match.BoardMinutes)
            {
                Throw (" Board Minutes  '{0}' doesn't match expected '{1}'" -f $NewFru.BoardMinutes.Value,$Match.BoardMinutes)    
            }
            If ($NewFru.BoardSerial.Value -ne $Match.BoardSerial)
            {
                Throw (" Board Serial Number '{0}' doesn't match expected '{1}'" -f $NewFru.BoardSerial.Value,$Match.BoardSerial)    
            }


            If ($NewFru.ProductSerial.Value -ne $Match.ProductSerial) 
            {
                Throw (" Product Serial Number '{0}' doesn't match expected '{1}'" -f $NewFru.ProductSerial.Value,$Match.ProductSerial)     
            }
            If ($NewFru.ProductAsset.Value -ne $Match.ProductAsset)
            {
                Throw (" Product Asset '{0}' doesn't match expected '{1}'" -f $NewFru.ProductAsset.Value,$Match.ProductAsset)  
            }
            If ($NewFru.ProductCustom1.Value -ne $Match.ProductCustom1)
            {
                Throw (" ProductCustom1 '{0}' doesn't match expected '{1}'" -f $NewFru.ProductCustom1.Value,$Match.ProductCustom1)   
            }
            If ($NewFru.ProductCustom2.Value -ne $Match.ProductCustom2)
            {
                Throw (" ProductCustom2 '{0}' doesn't match expected '{1}'" -f $NewFru.ProductCustom2.Value,$Match.ProductCustom2)   
            } 
            If ($NewFru.ProductCustom3.Value -ne $Match.ProductCustom3)
            {
                Throw (" ProductCustom3 '{0}' doesn't match expected '{1}'" -f $NewFru.ProductCustom3.Value,$Match.ProductCustom3)   
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of the GetFruData command
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-GetFruDataCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [int]     $Offset        = -1,
            [int]     $NumberOfBytes = -1,
            [byte[]]  $MatchFruBytes
        )

        Try
        {

            $TestStartTime     = Get-Date
            #--------------------
            # Setup the command args
            #--------------------
            If ($Offset -ne -1) 
            { 
                $FruoffsetArg = "-FruOffset $Offset" 
            }
            Else                
            { 
                $FruoffsetArg = ''
            }

            If ($NumberOfBytes -ne -1) 
            { 
                $NumberOfBytesArg = "-NumberOfBytes $NumberOfBytes"
            }
            Else                       
            { 
                $NumberOfBytesArg = ''
                $NumberOfBytes    =  $MatchFruBytes.Count 
            }

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test runs Get-WcsFruData and verifies:
             
    1) Command completes without error
    2) Command returns number of bytes requested
    3) Bytes returned match expected values
    4) FruOffset and NumberOfBytes inputs work as expected
   
 Full Command : Get-WcsFruData $FruoffsetArg $NumberOfBytesArg`r`n`r"  

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReadPartialFruData = @(Invoke-Expression "Get-WcsFruData $FruoffsetArg $NumberOfBytesArg -ErrorAction Stop" )

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($null -eq  $ReadPartialFruData)
            {
                Throw "Return value incorrect.  Expected  $NumberOfBytes but received null"
            }

            If ($ReadPartialFruData.Count -ne $NumberOfBytes)
            {
                Throw ("Return value returned wrong number of bytes {0} expected {1}" -f $ReadPartialFruData.Count,$NumberOfBytes)
            }

            For ($Offset=0;$Offset -lt $NumberOfBytes; $Offset++)
            {
                If ($ReadPartialFruData[$Offset] -ne $MatchFruBytes[$Offset])
                {
                    Throw ("FRU mismatch found read 0x{0:X2} expected 0x{1:X2} at offset $Offset" -f $ReadPartialFruData[$Offset],$MatchFruBytes[$Offset])
                }
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
    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify the functionality of the GetFru command
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-GetFruCommand()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $File = '',
            [string]  $LogDirectory = '',
            [byte[]]  $MatchFruBytes
        )

        Try
        {

            $TestStartTime     = Get-Date
            #--------------------
            # Setup test args
            #--------------------
            If ($File -eq '') { $FileArg = '' }
            Else              { $FileArg = "-File $File" }
            If ($LogDirectory -eq '') { $LogArg = '' }
            Else                      { $LogArg = "-LogDirectory $LogDirectory" }

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName  

            #---------------------
            # Display test details
            #---------------------
            Write-Host "
 This test runs Get-WcsFru and verifies:

    1) Command completes without error
    2) Fru matches expected value
    3) Returns the correct FRU object
   
 Full Command : Get-WcsFru $FileArg $LogArg `r`n`r"  

            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $FruObject = Invoke-Expression ("Get-WcsFru $FileArg $LogArg -ErrorAction Stop")
 
            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($null -eq  $FruObject)
            {
                Throw "Return value incorrect.  Expected FRU data but received null"
            }    
            #--------------------------------------------------------------------------------------
            # Save this original FRU data for use later in this script $OriginalFruData
            #--------------------------------------------------------------------------------------
            $ReadFruData = @()

            $FruObject.WcsFruData.Offsets.ChildNodes | Where-Object {$_ -ne $null} | ForEach-Object { $ReadFruData += ([byte] $_.Byte) }

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($null -eq  $FruObject)
            {
                Throw "Return value incorrect.  Expected  $NumberOfBytes but received null"
            }

            If ($ReadFruData.Count -ne $MatchFruBytes.Count)
            {
                Throw ("Return value returned wrong number of bytes {0} expected {1}" -f $ReadFruData.Count,$MatchFruBytes.Count)
            }

            For ($Index=0;$Index -lt $ReadFruData.Count; $Index++)
            {
                If ($ReadFruData[$Index] -ne $MatchFruBytes[$Index])
                {
                    Throw ("FRU mismatch found read 0x{0:X2} expected 0x{1:X2} at offset $Index" -f $ReadFruData[$Index],$MatchFruBytes[$Index])
                }
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

    #---------------------------------------------------------------------------------------------------------------------------------------
    # This function is written to verify that stress commands can identify errros like the stress application hang
    # or have errors in their log files
    #---------------------------------------------------------------------------------------------------------------------------------------
    Function CompatibilityTest-StressNoWait()
    {
        Param
        (
            [ref]     $TestDetails,
            [string]  $TestName,
            [string]  $TestId,
            [string]  $RunCommand,
            [string]  $VerifyCommand,
            [int]     $TestTimeInMin = 1,
            [switch]  $InjectHang,
            [switch]  $InjectIOmeterError,
            [switch]  $InjectDiskSpeedError,
            [switch]  $InjectPrime95Error,
            [switch]  $InjectQuickStressError
        )
        Try
        {
            $TestStartTime     = (get-date)

            #--------------------
            # Display test header
            #--------------------
            CompatibilityTest-TestHeader -ScriptFile $SCRIPT_FILE -TestID $TestID -TestCount ($TestDetails.Value.TestCount++)  -TestName  $TestName

            #----------------------------------------------------------------
            # Display test details.  Different details based on InjectHang
            #----------------------------------------------------------------
            If ($InjectHang)
            {
                Write-Host  " This test runs a stress command and injects a hang condition then verifies the command FAILS!!`r`n`r"
            }
            Else
            {
                Write-Host  "
 This test runs stress commands and verifies:

    1) Command completes without error
    2) Command returns 0

 Full Command : $RunCommand
                $VerifyCommand`r`n`r"   
            }
            #--------------------------------------------------------------------------------------
            # Run command being tested
            #--------------------------------------------------------------------------------------
            $ReturnedValue = Invoke-Expression "$RunCommand -ErrorAction Stop"

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If (0 -ne  $ReturnedValue)
            {
                If ($ReturnedValue -eq $null) { Throw "Run command return value incorrect.  Expected 0 but received null" }
                Else                          { Throw "Run command return value incorrect.  Expected 0 but received $ReturnedValue" }
            }

            #--------------------------------------------------------------------------------------
            # Wait for application to finish
            #--------------------------------------------------------------------------------------
            If ($InjectHang)
            {
                Start-Sleep 20
            }
            Else
            {
                Start-Sleep (60*$TestTimeInMin)
            }

            If      ($InjectIOmeterError)     { Copy-Item "$COMPATIBILITY_TEST_DIRECTORY\iometer_results.csv" $Directory }
            ElseIf  ($InjectDiskSpeedError)   { Copy-Item "$COMPATIBILITY_TEST_DIRECTORY\DiskSpd_PHYSICALDRIVE1.Err" $Directory }
            ElseIf  ($InjectPrime95Error)     { Copy-Item "$COMPATIBILITY_TEST_DIRECTORY\prime95_results.txt" "$WCS_BASE_DIRECTORY\Scripts\Binaries\Prime95\results.txt" -Force  }
            ElseIf  ($InjectQuickStressError) { Copy-Item "$COMPATIBILITY_TEST_DIRECTORY\prime95_results.txt" "$WCS_BASE_DIRECTORY\Scripts\Binaries\Prime95\results.txt" -Force }

            #--------------------------------------------------------------------------------------
            # Run Verify command
            #--------------------------------------------------------------------------------------
            $ReturnedValue =Invoke-Expression "$VerifyCommand " -ErrorAction Stop  

            #--------------------------------------------------------------------------------------
            # Verify return value matches expected
            #--------------------------------------------------------------------------------------
            If ($InjectHang -or $InjectIOmeterError -or  $InjectDiskSpeedError -or $InjectPrime95Error -or $InjectQuickStressError)
            {
                If (0 -eq $ReturnedValue)
                {
                    Throw "Verify command return value incorrect.  Expected non-zero but received 0"
                }

            }
            Else
            {
                If (0 -ne  $ReturnedValue)
                {
                    If ($ReturnedValue -eq $null) { Throw "Verify command return value incorrect.  Expected 0 but received null" }
                    Else                          { Throw "Verify command return value incorrect.  Expected 0 but received $ReturnedValue" }
                }
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
}
