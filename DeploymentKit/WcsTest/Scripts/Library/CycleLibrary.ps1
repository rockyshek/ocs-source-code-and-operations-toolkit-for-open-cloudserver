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


#-----------------------------------------------------------------------------------------
# Constants specific to this script
#-----------------------------------------------------------------------------------------
Set-Variable  -Name WCS_CYCLE_CONFIGURATION_MISMATCH              -Value ([byte] 0X1)            -Option ReadOnly -Force
Set-Variable  -Name WCS_CYCLE_UNEXPECTED_ERROR                    -Value ([byte] 0X2)            -Option ReadOnly -Force
Set-Variable  -Name WCS_CYCLE_RUN_ERROR                           -Value ([byte] 0X4)            -Option ReadOnly -Force

Set-Variable  -Name WCS_CYCLE_UNKNOWN_ERROR                       -Value ([byte] 0XFF)           -Option ReadOnly -Force

Set-Variable  -Name WCS_CYCLE_SENSOR                              -Value ([byte] 0X0F)           -Option ReadOnly -Force
Set-Variable  -Name WCS_CYCLE_SENSORTYPE                          -Value ([byte] 0XC0)           -Option ReadOnly -Force
Set-Variable  -Name WCS_CYCLE_OEMCODE                             -Value ([byte] 0X70)           -Option ReadOnly -Force

Set-Variable  -Name WCS_CYCLE_START_STRING                        -Value " Cycle start time and last boot and cycle end time"   -Option ReadOnly -Force
Set-Variable  -Name WCS_BOOT_START_STRING                         -Value " Boot start time"                                     -Option ReadOnly -Force

Set-Variable  -Name WCS_STARTUP_BAT_FILE                          -Value "$WCS_OS_STARTUP_DIRECTORY\Cycle-WcsStartup.Bat"     -Option ReadOnly -Force

#-------------------------------------------------------------------------------------------
# Helper function that returns true if autologin enabled and username and password not null
#-------------------------------------------------------------------------------------------
Function AutoLoginEnabled()
{
    Try
    {
        Return ( (    1 -eq (Get-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name AutoAdminLogon  -ErrorAction Stop ).AutoAdminLogon)   -and `
                 ($null -ne (Get-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name DefaultUsername -ErrorAction Stop ).DefaultUsername)  -and `
                 ($null -ne (Get-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name DefaultPassword -ErrorAction Stop ).DefaultPassword) )
    }
    Catch
    {
        Return $false
    }
}
#-------------------------------------------------------------------------------------
# Set-AutoLogin 
#-------------------------------------------------------------------------------------
Function Set-AutoLogin()
{
<#
  .SYNOPSIS
   Enables autologin by writing registry with local administrator username and password

  .DESCRIPTION
   Enables autologin. Autologin must be enabled for cycle testing so the 
   system will automatically login and run the startup batch file
   
   The script input is the local administrator username and password.  The scripts
   then writes values into the registry at this location:

      HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon

   The registry can also be written directly via regedit.  For more info
   on autologin see MSDN 

   The script is not for use with DOMAIN accounts since it is not recommended to
   use DOMAIN accounts for cycle testing.

  .EXAMPLE
   Set-Autologin -User Administrator -Password MyAdminPassword

  .EXAMPLE
   Set-Autologin  

   If User and Password omitted then script prompts for them

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Cycle
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param(
                [Parameter(Mandatory=$true)]  [string]  $User,
                [Parameter(Mandatory=$true)]  [string]  $Password
    )

    If (CoreLib_IsWinPE)
    {
        Write-Host -ForegroundColor Red -NoNewline "This function does not run in the WinPE OS"
        Return
    }

    If (($null -eq $User) -or ($null -eq $Password))
    {
        Write-Host "User and Password cannot be null`r"
        Return $null
    }
    Set-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name AutoAdminLogon  -Value          1
    Set-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name DefaultUsername -Value      $User
    Set-ItemProperty  "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\winlogon" -Name DefaultPassword -Value  $Password
}
#-------------------------------------------------------------------------------------
# Cycle-OsReboot 
#-------------------------------------------------------------------------------------
Function Cycle-OsReboot()
{
<#
  .SYNOPSIS
   Cycles system using OS reboot command

  .DESCRIPTION
   Cycles system using shutdown.exe /r.  
  
   On each cycle:
     (1) If a command was specified with -Run it is run
     (2) The config is read and compared against a reference config.  
     (3) The Windows System Event Log and BMC SEL are checked for suspect errors.
    
   The boot time can also be verified on each cycle to be within a user specified maximum
   boot time.

   By default the results are logged in <InstallDir>\Results\Cycle-OsReboot\<Date-Time>\
   Note the default <InstallDir> is \WcsTest

   RUN CYCLE-OSREBOOT WITH THE SAME ACCOUNT USED FOR AUTO-LOGIN.  
   DO NOT LOGIN WITH ANOTHER ACCOUNT WHILE RUNNING

   To run Cycle-OsReboot the following must be setup beforehand:

       1.  Autologin must be enabled.  To enable run "Set-Autologin" or write the registry
           directly. 
       
       2.  A reference configuration file must exist.  To generate a reference config file 
           run "Log-WcsConfig Reference".  Before generating a config file
           verify the current configuration is correct.
   
   Before each reboot there is a 30 second pause where the user can hit <Enter> to stop the 
   test.  
   
  .EXAMPLE
   Cycle-OsReboot -NumberOfCycles 200  

   Executes 200 OS reboot cycles and checks the config each cycle against Reference config.
   If config doesn't match and StopOnFail specified then stops the test

  .EXAMPLE
   Cycle-OsReboot -NumberOfCycles 200 -Run 'QuickStress -TimeInMin 15'

   Executes 200 OS reboot cycles.  Runs QuickStress for 15 minutes at start of each cycle.

  .EXAMPLE
   Cycle-OsReboot -NumberOfCycles 200 -MaxBootTime 5.25

   Executes 200 OS reboot cycles. Verifies boot time between each cycle is less than 5 minutes
   and 15 seconds.  Fails any cycle where the boot time is longer.

  .PARAMETER NumberOfCycles
   Number of cycles to run

  .PARAMETER ReferenceConfig
   Reference config which is compared on each cycle. Searches the \<InstallDir>\Configurations
   directory for the file specified.   
   
   If not specified uses default config:
   
   <InstallDir>\Configurations\Reference

   Note the default <InstallDir> is \WcsTest.  

  .PARAMETER LogDirectory
   Logs results in this directory. If not specified logs results in:
   
    <InstallDir\Results\<FunctionName>\<DateTime>

  .PARAMETER IncludeSelFile
   XML file that contains SEL entries to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeSelFile
   XML file that contains SEL entries to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER IncludeEventFile
   XML file that contains Windows System Events to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeEventFile
   XML file that contains Windows System Events to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER MaxBootTime
   Specifies the maximum allowed boot time for each cycle.  Expressed as a floating
   point.  For example, -MaxBootTime 4.5 specifies a boot time of 4 minutes 30 seconds.

   If not specified then boot time is not checked.

  .PARAMETER Run
   Command or script to run at start of each cycle before configuration and 
   error checks.   If returns anything other than 0 then the cycle fails.

   The command/script must accept the -LogDirectory input parameter but the
   -LogDirectory input parameter MUST NOT be specified.  Example command:

       Run-QuickStress -TimeInMin 5

   The above command will be appended with -LogDirectory <cycle directory> so the 
   quickstress results are stored in the same directory as the other cycle results.

  .PARAMETER StopOnFail
   If specified script stops when a failure occurs

  .PARAMETER CompareRefOnly
   If specified script the configuration comparison use the -OnlyRefDevice switch
   instead of the -Exact switch.  This is to allow a generic recipe file to be used
   instead of doing an exact match

  .PARAMETER Running
   For internal use only.  Do not specify.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Cycle
   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param(
                [Parameter(Mandatory=$true,Position=0)]  [int]     $NumberOfCycles,
                [Parameter(Mandatory=$false)]            [string]  $RefConfig         =  'Reference',
                [Parameter(Mandatory=$false)]            [string]  $LogDirectory      =  '',
                [Parameter(Mandatory=$false)]            [string]  $IncludeSelFile    =  '',
                [Parameter(Mandatory=$false)]            [string]  $ExcludeSelFile    =  '',
                [Parameter(Mandatory=$false)]            [string]  $IncludeEventFile  =  '',
                [Parameter(Mandatory=$false)]            [string]  $ExcludeEventFile  =  '',
                [Parameter(Mandatory=$false)]            [string]  $Run               =  '',
                [Parameter(Mandatory=$false)]            [float]   $MaxBootTime       =  0,
                                                         [switch]  $StopOnFail,
                                                         [switch]  $CompareRefOnly,
                                                         [switch]  $Running
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Setup vars and constants
        #-------------------------------------------------------
        $ConfigResult       = $null
        $ErrorCount         = 0
        $CurrentFail        = 0
        $CurrentPass        = 0

        $LogDirectory       = BaseLib_GetLogDirectory $LogDirectory $FunctionInfo.Name

        $SUMMARY_FILE       = "$LogDirectory\OsReboot-Summary.log"
        $COUNT_FILE         = "$LogDirectory\OsReboot-Count.log"
        $CSV_FILE           = "$LogDirectory\OsReboot-CsvSummary.csv"

        $IncludeExcludeArgs = ''

        If ($IncludeSelFile -ne '')   { $IncludeExcludeArgs += " -IncludeSelFile $IncludeSelFile" }
        If ($ExcludeSelFile -ne '')   { $IncludeExcludeArgs += " -ExcludeSelFile $ExcludeSelFile" }
        If ($IncludeEventFile -ne '') { $IncludeExcludeArgs += " -IncludeEventFile $IncludeEventFile" }
        If ($ExcludeEventFile -ne '') { $IncludeExcludeArgs += " -ExcludeEventFile $ExcludeEventFile" }
        #------------------------------------------------
        # Verify not running in WinPE
        #------------------------------------------------
        If (CoreLib_IsWinPE)
        {
            Throw "This function does not run in the WinPE OS"
        }
        #-----------------------------------------------------------
        # If starting make the setup .bat file and check autologin
        #----------------------------------------------------------- 
        If (-NOT $Running)
        {
            #-----------------------------------------------------------
            # Display and log info on starting the cycling
            #-----------------------------------------------------------
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value (" {0}" -f $MyInvocation.Line.Trim())  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_CYCLE_START_STRING               -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         
            CoreLib_WriteLog -Value  " Log directory $LogDirectory"        -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            #-----------------------------------------------------------
            # Start the CSV file 
            #-----------------------------------------------------------
            Set-Content -Value 'CycleNumber,CycleTime(Min),BootTime(Min),Result' -Path $CSV_FILE
            #-----------------------------------------------------------
            # Check for the config file before starting
            #-----------------------------------------------------------
            If (-NOT (Test-Path "$WCS_CONFIGURATION_DIRECTORY\$RefConfig.config.xml" ))
            {
                Throw "Could not find configuration file '$WCS_CONFIGURATION_DIRECTORY\$RefConfig.config.xml'" 
            }
            Else
            {
                #-----------------------------------------------------------
                # Now check the configuration matches the reference config
                #-----------------------------------------------------------
                If ($CompareRefOnly)
                {
                    $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig $RefConfig) -RefToResults ([ref] $ConfigResult) -OnlyRefDevices -Quiet
                }
                Else
                {
                    $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig $RefConfig) -RefToResults ([ref] $ConfigResult) -Exact -Quiet
                }
                #-----------------------------------------------------------
                # If mismatches found abort but log mismatches first
                #-----------------------------------------------------------
                If (($null -eq $Mismatches) -or (0 -ne $Mismatches))
                { 
                    Log-WcsConfig -Config $ConfigResult -File 'ConfigMisMatch' -Path $LogDirectory
                    Throw "Configuration file does not match current configuration. See results in $LogDirectory"
                }
                #-----------------------------------------------------------
                # Copy config file to log directory for reference only
                #-----------------------------------------------------------
                Copy-Item "$WCS_CONFIGURATION_DIRECTORY\$RefConfig.config.xml" "$LogDirectory" -ErrorAction SilentlyContinue | Out-Null
            }
            #-----------------------------------------------------------
            # Verify autologin enabled
            #-----------------------------------------------------------
            If (-NOT (AutoLoginEnabled))
            {
                Throw " Autlogin is not enabled.  Please enable Autologin using regedit or Set-Autologin" 
            }
            #-----------------------------------------------------------
            # Setup the startup file for next cycle
            #-----------------------------------------------------------
            Remove-Item "$WCS_OS_STARTUP_DIRECTORY\Cycle-*.bat" -Force -ErrorAction SilentlyContinue | Out-Null

            $CommandToRun = "powershell -command . $WCS_SCRIPT_DIRECTORY\wcsscripts.ps1;cycle-osreboot  $NumberOfCycles -LogDirectory $LogDirectory  -StopOnFail:`$$StopOnFail -Running $IncludeExcludeArgs -Run '$Run' -MaxBoot $MaxBootTime -CompareRefOnly:`$$CompareRefOnly" 
      
            Set-Content -Value  $CommandToRun  -Path $WCS_STARTUP_BAT_FILE

            $CurrentCycle = 0
        }
        #-----------------------------------------------------------
        # Else get the current cycle
        #-----------------------------------------------------------
        Else
        {
            If (-NOT (Test-Path $WCS_STARTUP_BAT_FILE))
            {
                Throw "Did not find the startup bat file. DO NOT USE -Running"
            }
            #-------------------------------------------------------------------
            #  Get the current counts from the file
            #-------------------------------------------------------------------
            If (-NOT (Test-Path $COUNT_FILE))
            {
                Throw "Aborting script because could not find the count file '$COUNT_FILE'"   
            }

            $CycleFileContent = (Get-Content $COUNT_FILE)
            
            $CurrentCycle =  ([int] $CycleFileContent.split()[0]) + 1
            $CurrentFail  =  [int] $CycleFileContent.split()[1]
            $CurrentPass  =  [int] $CycleFileContent.split()[2]
            #-------------------------------------------------------------------
            # Calculate the last cycle and boot times
            #-------------------------------------------------------------------
            $LastCycleStartTime = $null
            $LastBootStartTime  = $null

            Get-Content -Path $SUMMARY_FILE | Where-Object {$_ -ne $null} | ForEach-Object {

                If ($_.Contains($WCS_CYCLE_START_STRING))
                {
                    [datetime] $LastCycleStartTime = ($_.Substring(1,$_.IndexOf(']')-1) -as [datetime])
                }
                If ($_.Contains($WCS_BOOT_START_STRING))
                {
                    [datetime] $LastBootStartTime   = ($_.Substring(1,$_.IndexOf(']')-1) -as [datetime])
                }
            }
            If (($null -eq $LastCycleStartTime) -or ($null -eq $LastBootStartTime))
            {
                Throw "Aborting script because could not find the last boot time or cycle time in the summary file"  
            }
 
            $ThisCycleStartTime = Get-Date
            $MaxCycleTimeInMin     = ($ThisCycleStartTime - $LastCycleStartTime).TotalMinutes
            $MaxBootTimeInMin      = ($ThisCycleStartTime - $LastBootStartTime).TotalMinutes  
            #-------------------------------------------------------------------
            #  Display and log the cycle start and last cycle times
            #-------------------------------------------------------------------
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                             -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value   " Cycle $CurrentCycle of $NumberOfCycles total cycles "     -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                             -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_CYCLE_START_STRING                                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE            
            CoreLib_WriteLog -Value  (" Last Cycle Time:  {0:F3} (Minutes)" -f $MaxCycleTimeInMin )  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            #-------------------------------------------------------------------
            #  If boot time specified verify in the range
            #-------------------------------------------------------------------
            If ((0 -ne $MaxBootTime) -and ($MaxBootTimeInMin -gt $MaxBootTime))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value  (" Last Boot Time:   {0:F3} (Minutes) - FAIL.  Longer then specified {1:F3}" -f $MaxBootTimeInMin,$MaxBootTime) -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE -PassThru | Write-Host
            }
            Else
            {
                CoreLib_WriteLog -Value  (" Last Boot Time:   {0:F3} (Minutes)" -f $MaxBootTimeInMin )   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            }
            CoreLib_WriteLog -Value  '' -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  
        }
        #----------------------------------------------------------------------------------
        # If start of test just backup and clear event logs, else backup and check for 
        # event logs for errors and check the configuration too
        #----------------------------------------------------------- ----------------------
        If (0 -eq $CurrentCycle) 
        {
            If (0 -ne (Clear-WcsError ))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value 'Could not clear error logs'   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
        }
        Else
        {
            #-----------------------------------------------------------
            # Make a folder for the logs
            #----------------------------------------------------------- 
            $CycleDirectory = ("$LogDirectory\Cycle{0}" -f $CurrentCycle)

            New-Item $CycleDirectory -ItemType Container -ErrorAction SilentlyContinue | Out-Null

            #-----------------------------------------------------------
            # Run command or script specified
            #----------------------------------------------------------- 
            If ('' -ne $Run)    
            {
                Try
                {
                    $RunDirectory  = "$CycleDirectory\Run"

                    $Results = (Invoke-Expression -Command "$Run -LogDirectory $RunDirectory" -ErrorAction Stop)

                    If ((-NOT ($Results -is [int])) -or ($Results -ne 0))
                    {
                        $ErrorCount++
                        CoreLib_WriteLog -Value (" Run '{0}' failed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
                    } 
                    Else
                    {
                        CoreLib_WriteLog -Value (" Run '{0}' passed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE   
                    }

                }
                Catch
                {
                    $ErrorCount++
                    CoreLib_WriteLog -Value (" Run '{0}' had exception" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
                }
            }
            #-----------------------------------------------------------
            # Check for errors
            #-----------------------------------------------------------        
            $ErrorDirectory = "$CycleDirectory\Check-WcsError"   
              
            $EventErrors = Check-WcsError -LogDirectory $ErrorDirectory -IncludeEventFile $IncludeEventFile -ExcludeEventFile $ExcludeEventFile -IncludeSelFile $IncludeSelFile -ExcludeSelFile $ExcludeSelFile 

            If (0 -ne $EventErrors)
            {
                $ErrorCount++
                CoreLib_WriteLog -Value " Check-WcsError found errors"  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
            Else
            {
                CoreLib_WriteLog -Value " Check-WcsError passed"  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  
            }


            If (0 -ne (Clear-WcsError ))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value ' Could not clear error logs'   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
            #-----------------------------------------------------------
            # Compare configurations
            #-----------------------------------------------------------             
            If ($CompareRefOnly)
            {
                $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig $RefConfig) -RefToResults ([ref] $ConfigResult) -OnlyRefDevices
            }
            Else
            {
                $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig $RefConfig) -RefToResults ([ref] $ConfigResult) -Exact
            }

            If (0 -ne $Mismatches)
            {
                $ErrorCount++
                CoreLib_WriteLog -Value " Configuration check found mismatches"  -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Log-WcsConfig -Config $ConfigResult -File "ConfigMisMatch" -Path $CycleDirectory
            }
            Else
            {
                CoreLib_WriteLog -Value " Configuration check passed"  -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   
            }
        }
        #-----------------------------------------------------------
        # Write the result
        #-----------------------------------------------------------
        If ($CurrentCycle -ne 0)
        {
            CoreLib_WriteLog -Value ' ' -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host

            If  ($ErrorCount -eq 0) 
            { 
                $CurrentPass++
                CoreLib_WriteLog -Value (" CYCLE {0} PASSED" -f $CurrentCycle)   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Add-Content -Value ("{0},{1:F3},{2:F3},PASS" -f $CurrentCycle,$MaxCycleTimeInMin,$MaxBootTimeInMin) -Path $CSV_FILE
            }
            Else                    
            { 
                $CurrentFail++
                CoreLib_WriteLog -Value (" CYCLE {0} FAILED" -f $CurrentCycle)   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Add-Content -Value ("{0},{1:F3},{2:F3},FAIL" -f $CurrentCycle,$MaxCycleTimeInMin,$MaxBootTimeInMin) -Path $CSV_FILE

            }
        }

        Set-Content -Value "$CurrentCycle $CurrentFail $CurrentPass" -Path $COUNT_FILE 
        #-----------------------------------------------------------
        # If reached the number of cycles then stop
        #-----------------------------------------------------------
        If ($CurrentCycle -ge $NumberOfCycles)  
        {
            CoreLib_WriteLog -Value  ''                                                                            -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host


            Remove-Item $COUNT_FILE           -Force -ErrorAction SilentlyContinue | Out-Null
            Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
            Return  $WCS_RETURN_CODE_SUCCESS  
        }
        #-----------------------------------------------------------
        # If have errors and stop on fail then stop
        #-----------------------------------------------------------
        ElseIf  (($ErrorCount -ne 0) -and ($StopOnFail))
        {
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " CYCLE TEST ABORTED BY STOP ON FAIL"                                          -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                            -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host

 
            Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
            Remove-Item $COUNT_FILE           -Force -ErrorAction SilentlyContinue | Out-Null
            Return 1 
        }
        #-----------------------------------------------------------
        # Reboot - Give the user 30 seconds to abort 
        #-----------------------------------------------------------
        else
        {
            Write-Host  "`n`r`n`r Hit <ENTER> to abort testing`r`n`r`n"

            For ($TimeOut=0;$TimeOut -lt 60;$TimeOut++)
            {
                Start-Sleep -Milliseconds 500
                if ([console]::KeyAvailable)
                {
                    [console]::ReadLine()
                    CoreLib_WriteLog -Value " User aborted testing."   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                    Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
                    Return $WCS_RETURN_CODE_INCOMPLETE 
                }
            }       
            CoreLib_WriteLog  -Value $WCS_BOOT_START_STRING    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE                  
            shutdown.exe /r /t 1   | Out-Null        
        }

        Return $WCS_RETURN_CODE_SUCCESS  
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null

        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo

        CoreLib_WriteLog -Value $_.ErrorDetails -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  

        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }
            
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR
    }
}

#-------------------------------------------------------------------------------------
# Cycle-WcsCheck
#-------------------------------------------------------------------------------------
Function Cycle-WcsCheck()
{
<#
  .SYNOPSIS
   Checks configuration, BMC SEL, and Windows System Event Log for errors

  .DESCRIPTION
   This command allows config and error checking during cycle testing.  On each boot the 
   config is read and compared against a reference config.  In addition, the BMC SEL and 
   Windows System Event Log are checked for errors.
    
   By default the results are logged in <InstallDir>\Results\Cycle-WcsCheck\<Date-Time>\
   Note the default <InstallDir> is \WcsTest

   To run Cycle-WcsCheck the following must be setup beforehand:

       1.  Autologin must be enabled.  To enable run "Set-Autologin" or write the registry
           directly. 
       
       2.  A reference configuration file must exist.  To generate a reference config file 
           run "Log-WcsConfig Reference".  Before generating a config file
           verify the current configuration is correct.
          
       3.  The following command must be placed in a batch file in the startup directory of

            "\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\Cycle-WcsStartup.Bat" 

            Powershell -Command ". \WcsTest\Scripts\WcsScripts.ps1 ; Cycle-WcsCheck"

            NOTE: It is likely that you will want to exclude events such as unexpected
            power loss in the Windows System Event Log.  To do this modify the file:

            Powershell -Command ". \WcsTest\Scripts\WcsScripts.ps1 ; Cycle-WcsStartup -ExcludeEventFile <path>"

            NOTE:  If not installed in \WcsTest then change the path above to match the 
            actual install directory.  
            
            PLEASE SEE THE EXAMPLE Cycle-WcsStartup.bat file in 
            
            <InstallDir>\Scripts\References\CycleBatFiles

        4.  Before starting the test clear all errors using Clear-WcsError command

   IMPORTANT:

        1.  After testing is complete delete the startup file:

            "\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\Cycle-WcsStartup.Bat" 

        2.  This test writes specific entries into the SEL so the status can be read from the CM.
            The entries use sensor 0x0F, sensor type 0xC0, and an OEM code 0x70 in Event DIR/Type

  .PARAMETER LogDirectory
   Logs results in this directory. If not specified logs results in:
   
    <InstallDir\Results\<FunctionName>\<DateTime>

  .PARAMETER IncludeSelFile
   XML file that contains SEL entries to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeSelFile
   XML file that contains SEL entries to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER IncludeEventFile
   XML file that contains Windows System Events to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeEventFile
   XML file that contains Windows System Events to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER CompareRefOnly
   If specified script the configuration comparison use the -OnlyRefDevice switch
   instead of the -Exact switch.  This is to allow a generic recipe file to be used
   instead of doing an exact match

  .PARAMETER Run
   Command or script to run at start of each cycle before configuration and 
   error checks.   If returns anything other than 0 then the cycle fails.

   The command/script must accept the -LogDirectory input parameter but the
   -LogDirectory input parameter MUST NOT be specified.  Example command:

       Run-QuickStress -TimeInMin 5

   The above command will be appended with -LogDirectory <cycle directory> so the 
   quickstress results are stored in the same directory as the other cycle results.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Cycle

   #>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param(      
                [Parameter(Mandatory=$false)] [string]  $LogDirectory      =  '',
                [Parameter(Mandatory=$false)] [string]  $IncludeSelFile    =  '',
                [Parameter(Mandatory=$false)] [string]  $ExcludeSelFile    =  '',
                [Parameter(Mandatory=$false)] [string]  $IncludeEventFile  =  '',
                [Parameter(Mandatory=$false)] [string]  $ExcludeEventFile  =  '',
                [Parameter(Mandatory=$false)] [string]  $Run               =  '',
                                              [switch]  $CompareRefOnly
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Setup vars and constants
        #-------------------------------------------------------
        $ConfigResult            = $null
        [byte] $ReturnCode       = 0
        $LogDirectory            = BaseLib_GetLogDirectory $LogDirectory  $FunctionInfo.Name
        $SUMMARY_FILE            = "$LogDirectory\Cycle-Summary.log"   
        #-------------------------------------------------------------------
        #  Display Message
        #-------------------------------------------------------------------
        CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value (" {0}" -f $MyInvocation.Line.Trim())  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value  " Log directory $LogDirectory"        -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host                   
        #-----------------------------------------------------------
        # Run command or script specified
        #----------------------------------------------------------- 
        If ('' -ne $Run)    
        {
            Try
            {
                $RunDirectory  = "$LogDirectory\Run"

                $Results = (Invoke-Expression -Command "$Run -LogDirectory $RunDirectory" -ErrorAction Stop)

                If ((-NOT ($Results -is [int])) -or ($Results -ne 0))
                {
                    $ReturnCode = $ReturnCode -bor $WCS_CYCLE_RUN_ERROR
                    CoreLib_WriteLog -Value (" Run '{0}' failed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
                } 
                Else
                {
                    CoreLib_WriteLog -Value (" Run '{0}' passed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE   
                }
            }
            Catch
            {
                $ReturnCode = $ReturnCode -bor $WCS_CYCLE_RUN_ERROR
                CoreLib_WriteLog -Value (" Run '{0}' had exception" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
        }
        #-----------------------------------------------------------
        # Check the configuration against the reference
        #-----------------------------------------------------------            
        If ($CompareRefOnly)
        {
            $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig Reference) -RefToResults ([ref] $ConfigResult) -OnlyRefDevices -ErrorAction Stop
        }
        Else
        {
            $Mismatches = Compare-WcsConfig -RefConfig (Get-WcsConfig Reference) -RefToResults ([ref] $ConfigResult) -Exact -ErrorAction Stop
        }

        If (0 -ne $Mismatches)
        {
            $ReturnCode = $ReturnCode -bor $WCS_CYCLE_CONFIGURATION_MISMATCH 

            CoreLib_WriteLog -Value ' Configuration check found mismatches' -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  

            Log-WcsConfig -Config $ConfigResult -File "ConfigMisMatch" -Path $LogDirectory
        }
        Else
        {
            CoreLib_WriteLog -Value  ' Configuration check passed'  -Function $FunctionInfo.Name   -LogFile $SUMMARY_FILE  
        } 
        #-----------------------------------------------------------
        # Check for errors
        #----------------------------------------------------------- 
        $ErrorDirectory  = "$LogDirectory\Check-Error"

        $ErrorCount = Check-WcsError -LogDirectory $ErrorDirectory -ErrorAction Stop  -IncludeEventFile $IncludeEventFile -ExcludeEventFile $ExcludeEventFile -IncludeSelFile $IncludeSelFile -ExcludeSelFile $ExcludeSelFile 
        
        If (0 -ne $ErrorCount)
        {
            $ReturnCode = $ReturnCode -bor $WCS_CYCLE_UNEXPECTED_ERROR

            CoreLib_WriteLog -Value " Check-WcsError found errors"  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE   
        }
        Else
        {
            CoreLib_WriteLog -Value  " Check-WcsErrors passed" -Function $FunctionInfo.Name   -LogFile $SUMMARY_FILE 
        } 
        #-----------------------------------------------------------
        # Clear errors
        #-----------------------------------------------------------   
        If (0 -ne (Clear-WcsError))
        {
           Throw 'Could not clear error logs' 
        }
        #------------------------------------------------------------------------------------------
        # Write results to the SEL. If fail then won't get entry that indicates cycle completed
        #----------------------------------------------------------- ------------------------------
        [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  $WCS_CYCLE_SENSORTYPE, $WCS_CYCLE_SENSOR ,$WCS_CYCLE_OEMCODE, 0x5a, 0xa5, $ReturnCode)
           
        $IpmiData = Invoke-WcsIpmi  0x44 $RequestData $WCS_STORAGE_NETFN -ErrorAction SilentlyContinue
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo

        CoreLib_WriteLog -Value $_.ErrorDetails -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  

        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        #------------------------------------------------------------------------------------------
        # Write results to the SEL. If fail then won't get entry that indicates cycle completed
        #----------------------------------------------------------- ------------------------------
        [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  $WCS_CYCLE_SENSORTYPE, $WCS_CYCLE_SENSOR ,$WCS_CYCLE_OEMCODE, 0x5a, 0xa5, $WCS_CYCLE_UNKNOWN_ERROR)
           
        $IpmiData = Invoke-WcsIpmi  0x44 $RequestData $WCS_STORAGE_NETFN -ErrorAction SilentlyContinue
    }
}

#-------------------------------------------------------------------------------------
# Cycle-WcsBladePower 
#-------------------------------------------------------------------------------------
Function Cycle-WcsBladePower()
{
<#
  .SYNOPSIS
   Cycles the power to blades in a chassis

  .DESCRIPTION
   Cycles power (chipset or full power) to all the blades in a chassis.  This must be run on the 
   Chassis Manager in the chassis to be tested.
    
   By default the results are logged in <InstallDir>\Results\Cycle-WcsBladePower\<Date-Time>\
   Note the default <InstallDir> is \WcsTest

   This command uses the default chassis manager credentials.  To change these credentials
   use the Set-WcsChassisCredential command.

   SSL Setting will be detected automatically based on Chassis Manager Config file

   THIS REQUIRES SETUP ON EVERY BLADE TO TEST:

       1.  Autologin must be enabled.  To enable run "Set-Autologin" or write the registry
           directly. 
       
       2.  A reference configuration file must exist.  To generate a reference config file 
           run "Log-WcsConfig Reference".  Before generating a config file
           verify the current configuration is correct.
             
       3.  Delete any old cycle bat files in the startup directory (ie: Cycle-*.bat)

       4.  The following command must be placed in a batch file in the startup directory:

            "\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\Cycle-WcsStartup.Bat" 

            Powershell -Command ". \WcsTest\Scripts\WcsScripts.ps1 ; Cycle-WcsCheck 
              -ExcludeEventFile C:\WcsTest\Scripts\References\ErrorFiles\ExcludePowerLossEvent.xml"

            NOTE:  If not installed in \WcsTest then change the path above to match the 
            actual install directory.  An example of this batch file is also in 
            
            <InstallDir>\Scripts\References\CycleBatFiles

       5.  Wcs Test Tools must be installed on each blade

       6.  Before starting the test clear all errors using Clear-WcsError command

   IMPORTANT:

        1.  After testing is complete delete the startup file:

            "\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\Cycle-WcsStartup.Bat" 

        2.  This test writes specific entries into the SEL so the status can be read from the CM.
            The entries use sensor 0x0F, sensor type 0xC0, and an OEM code 0x70 in Event DIR/Type
              
  .PARAMETER NumberOfCycles
   The number of cycles to power cycle the blades

  .PARAMETER LogDirectory
   Logs results in this directory.  
   If not specified defaults to <InstallDir\Results\<FunctionName>\<DateTime>

  .PARAMETER FullPower
   Full power includes standby power to the blade.  The default is to cycle chipset power
   without standby power

  .PARAMETER StopOnFail
   If specified script stops when a failure occurs

  .PARAMETER BladeList
   If specified then tests only the blades in the list.  To specify testing only blades in slots
   12,13, and 14:

   -BladeList  @(12,13,14)

    If not specified then tests all blades that respond to the ClearBladeLog command.

  .PARAMETER SSL
   If specified the script uses SSL protocol for REST commands

  .PARAMETER OnTimeInSec
   Time to wait after power on

  .PARAMETER OffTimeInSec
   Time to wait after power off


  .PARAMETER OffTimeStartTimeInSec
   Start time in seconds to begin sweeping off time.  This is the off time for the first cycle.\
   Must OffTimeStartTimeInSec, OffTimeEndTimeInSec, OffTimeIncrementInMs to sweep the offtime

  .PARAMETER OffTimeEndTimeInSec
   End time in seconds to begin sweeping off time.  This is the longest off time that will be 
   used.  The cycle after this off time goes back to the start time.

   Must OffTimeStartTimeInSec, OffTimeEndTimeInSec, OffTimeIncrementInMs to sweep the offtime

  .PARAMETER OffTimeIncrementInMs
   Increment to sweep the off time in MILLISECONDS.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Cycle

   #>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param(
                [Parameter(Mandatory=$false)]            [string]  $NumberOfCycles  = 1,
                [Parameter(Mandatory=$false)]            [string]  $LogDirectory    = '',
                [Parameter(Mandatory=$false)]            [int]     $OnTimeInSec     = 600,
                [Parameter(Mandatory=$false)]            [int]     $OffTimeInSec    = 30,
                [Parameter(Mandatory=$false)]            [int[]]   $BladeList       = @(),

                                                         [int]     $OffTimeStartTimeInSec  = -1,
                                                         [int]     $OffTimeEndTimeInSec    = -1 ,
                                                         [int]     $OffTimeIncrementInMs   = -1 ,

                                                         [switch]  $FullPower,
                                                         [switch]  $StopOnFail
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Create the log directory
        #-------------------------------------------------------
        $LogDirectory            =  BaseLib_GetLogDirectory $LogDirectory  $FunctionInfo.Name
        $SUMMARY_FILE            = "$LogDirectory\CycleBladePower-Summary.log"
        $CSV_FILE                = "$LogDirectory\CycleBladePower-CsvSummary.csv"

		$CurrentFail             = 0
        $CurrentPass             = 0

        $SweepTimeInMs           = -1

        If (($OffTimeStartTimeInSec -ne -1) -or ( $OffTimeEndTimeInSec -ne -1) -or  ($OffTimeIncrementInMs -ne -1))
        {
            $SweepTimeInMs = $OffTimeStartTimeInSec * 1000

            If (($OffTimeStartTimeInSec -eq -1) -or ( $OffTimeEndTimeInSec -eq -1) -or  ($OffTimeIncrementInMs -eq -1))
            {
                Throw "Must specify OffTime Start, End and Increment if sweeping off time"
            }
        }
        #-------------------------------------------------------
        # Set the type of cycling
        #-------------------------------------------------------
        If ($FullPower)
        {
            $OnCommand       = 'SetPowerOn?BladeId='  
            $OffCommand      = 'SetPowerOff?BladeId='  

            $SweepOnCommand  = 'SetAllPowerOn'
            $SweepOffCommand = 'SetAllPowerOff'
        }
        Else
        {
            $OnCommand       = 'SetBladeOn?BladeId='   
            $OffCommand      = 'SetBladeOff?BladeId='  

            $SweepOnCommand  = 'SetAllBladeOn'
            $SweepOffCommand = 'SetAllBladeOff'
        }
        #-------------------------------------------------------------------
        #  Display and log header info
        #-------------------------------------------------------------------
        CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value (" {0}" -f $MyInvocation.Line.Trim())  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
        CoreLib_WriteLog -Value  (" Log directory $LogDirectory")      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
		#-------------------------------------------------------
		# Determine the SSL setting to use
        #-------------------------------------------------------
		$ChassisConfig	= [XML] (Get-Content $WCS_CHASSIS_MANAGER_CONFIG_FILE_PATH )
		$SSLValue		= ( $ChassisConfig.configuration.appsettings.SelectNodes("add") | Where-Object { $_.Key -eq "EnableSslEncryption" }).value
	
        If ($SSLValue -eq 1 ) 
        {
            $SSL = $true
        } 
        Else 
        {
            $SSL = $false
        }     		
        CoreLib_WriteLog -Value  (" SSL Setting is {0} " -f $SSL) -Function $FunctionInfo.Name	-LogFile $SUMMARY_FILE	-PassThru | Write-Verbose
        #-------------------------------------------------------
        # Did not specify any blades with -BladeList parameter
        #-------------------------------------------------------
        If (0 -eq $BladeList.Count)
        {
            #-------------------------------------------------------
            # Check all blades, test all that respond
            #-------------------------------------------------------
            For ($Blade = 1; $Blade -le $WCS_BLADES_PER_CHASSIS; $Blade++)
            {
                $Response = Invoke-WcsRest -TargetList localhost -Command "ClearBladeLog?bladeid=$Blade" -SSL:$SSL -ErrorAction SilentlyContinue

                If (($Response -ne $null) -and ($Response.BladeResponse.CompletionCode -eq 'Success'))
                {
                    CoreLib_WriteLog -Value " Testing blade $Blade"    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Verbose 
                    $BladeList += $Blade  
                }
            }
        }
        #-------------------------------------------------------
        # Blades specified with -BladeList parameter
        #-------------------------------------------------------
        Else
        {
            #-------------------------------------------------------
            # Only clear logs of blade specified
            #-------------------------------------------------------
            For ($Blade = 0; $Blade -lt $Bladelist.Count; $Blade++)
            {
                $BladeIndex = $BladeList[$Blade]   

                $Response = Invoke-WcsRest -TargetList localhost -Command "ClearBladeLog?bladeid=$BladeIndex" -SSL:$SSL -ErrorAction SilentlyContinue

                If (($Response -ne $null) -and ($Response.BladeResponse.CompletionCode -eq 'Success'))
                {
                    CoreLib_WriteLog -Value " Testing blade $BladeIndex"    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Verbose 
                }
                Else
                {
                     Throw "Blade $BladeIndex failed to clear blade log.  Aborting test " 
                }
            }
        }
		#-------------------------------------------------------
        # Verify if None of the Blade connected to Chassis
        #-------------------------------------------------------
        If ( $Bladelist.Count -eq 0 ) # Need to unit test
        {
            Throw "NONE of the Blades are Connected/Working.  Aborting test"
        }
        #-----------------------------------------------------------
        # Start the CSV file 
        #-----------------------------------------------------------
        $CsvString = "CycleNumber,Result"

        $Bladelist | Where-Object {$_ -ne $null} | ForEach-Object {  $CsvString += (",Blade{0}" -f $_) }

        Set-Content -Value $CsvString  -Path $CSV_FILE

        CoreLib_WriteLog -Value (" Testing {0} blades" -f  $Bladelist.Count) -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Host
        #-------------------------------------------
        # Loop for the number of cycles specified
        #-------------------------------------------
        For ($Cycle = 1; $Cycle -le $NumberOfCycles; $Cycle++) 
        {
            $FoundError = $false
            #-------------------------------------------------------------------
            #  Display Message
            #-------------------------------------------------------------------
            CoreLib_WriteLog -Value  ' '                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host        
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  " Cycle $Cycle of $NumberOfCycles total cycles"    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            #-------------------------------------------
            # Power off then on
            #-----------------------------------------
            CoreLib_WriteLog -Value  (" Power off at {0} and waiting {1} seconds" -f (Get-Date -Format G),$OffTimeInSec) -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host


            If ($SweepTimeInMs -ne -1)
            {
                $Response = Invoke-WcsRest -TargetList localhost -Command "$SwwepOffCommand" -ErrorAction Stop -SSL:$SSL 

                Start-Sleep -MilliSeconds $SweepTimeInMs

                $Response = Invoke-WcsRest -TargetList localhost -Command "$SweepOnCommand" -ErrorAction Stop -SSL:$SSL 

                $SweepTimeInMs += $OffTimeIncrementInMs

                If ($SweepTimeInMs -gt ($OffTimeEndTimeInSec *1000))
                {
                    $SweepTimeInMs = $OffTimeStartTimeInSec * 1000
                }
            }
            Else
            {
                For ($Blade = 0; $Blade -lt $Bladelist.Count; $Blade++)
                {
                    $BladeIndex = $BladeList[$Blade]   
                    $Response = Invoke-WcsRest -TargetList localhost -Command "$OffCommand$BladeIndex" -ErrorAction Stop -SSL:$SSL 
                }

                Start-Sleep -Seconds $OffTimeInSec

                CoreLib_WriteLog -Value  (" Power on at {0} and waiting {1} seconds" -f (Get-Date -Format G),$OnTimeInSec) -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host

                For ($Blade = 0; $Blade -lt $Bladelist.Count; $Blade++)
                {
                    $BladeIndex = $BladeList[$Blade]   
                    $Response = Invoke-WcsRest -TargetList localhost -Command "$OnCommand$BladeIndex" -ErrorAction Stop -SSL:$SSL 
                }
            }

            Start-Sleep -Seconds $OnTimeInSec 
 
            CoreLib_WriteLog -Value  " Power cycle complete" -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
 
            #-----------------------------------------
            # Read, parse and clear blade logs
            #-----------------------------------------
            $CsvString = ''

            For ($Blade = 0; $Blade -lt $Bladelist.Count; $Blade++)
            {
                $BladeIndex = $BladeList[$Blade]   

                $ChildLogDirectory = "$LogDirectory\BladeSlot$BladeIndex"

                New-Item $ChildLogDirectory -ItemType Container -Force -ErrorAction SilentlyContinue | Out-Null   
                #--------------------        
                # Read the blade log
                #--------------------       
                $SEL = [xml] (Invoke-WcsRest -TargetList localhost -Command "ReadBladeLog?bladeid=$BladeIndex" -ErrorAction Stop -SSL:$SSL )

                #---------------------------        
                # Save the blade SEL as xml
                #---------------------------        
                $SEl.Save("$ChildLogDirectory\SEL-Cycle$Cycle.xml")

                #-----------------------------------
                # Check if blade responded correctly
                #-----------------------------------
                If ($SEL.ChassisLogResponse.CompletionCode -ne 'Success')
                {
                    CoreLib_WriteLog  " Blade $BladeIndex FAILED! Could not read the SEL"    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Host    
                    $CsvString += ",FAIL" 
                    $FoundError = $true                           
                }
                Else
                {
                    $FoundResult = $false
                    #-------------------------------------
                    # Parse the SEL
                    #-------------------------------------
                    $Sel.ChassisLogResponse.logentries.ChildNodes | Where-Object {$_ -ne $null} | ForEach-Object {

                        #-------------------------------------------------------------
                        # If contains specific sensor an error occurred on the blade
                        #-------------------------------------------------------------
                        If ( ($_.EventDescription.Contains('OEM Event  |  OEM Event.  |  Sensor Type: Unknown  |  Sensor Name:   |  Sensor Number: 15  '))  -or 
                             ($_.EventDescription.Contains('OEM Event  |  OEM Event.  |  Sensor Type: Unknown  |  Sensor Name: Unknown  |  Sensor Number: 15 '))) 
                        {
                            $FoundResult = $true
						    $EventDesc = $_.EventDescription
                            If (($_.EventDescription.Contains('Error Code: 0x5AA500'))  -or ($_.EventDescription.Contains('Event Data (1-3): 0x5AA500')))
                            {
                                CoreLib_WriteLog -Value " Blade $BladeIndex PASSED" -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Host -ForegroundColor Green    
                                $CsvString += ",PASS" 
                            }
                            Else
                            {
								CoreLib_WriteLog -Value " Blade $BladeIndex FAILED! Blade booted but SEL entry indicates error." -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Host -ForegroundColor Red
                                $CsvString += ",FAIL" 
                                $FoundError = $true 
                            }
                        }
                        #-------------------------
                        # Append to SEL log file
                        #-------------------------
                        Add-Content -Value $_.EventDescription -Path "$ChildLogDirectory\SEL-AllCycles.log"
                    }
                    #-------------------------------------
                    # Verify found the correct SEL entry
                    #-------------------------------------
                    If (-NOT $FoundResult) 
                    {
                        CoreLib_WriteLog -Value " Blade $BladeIndex FAILED! Did not find cycle test result in SEL. Possible hang or slow boot." -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru |  Write-Host  -ForegroundColor Red      
                        
                        $FoundError = $true 
                        $CsvString += ",FAIL" 

                    }
                    #----------------------
                    # Clear the SEL
                    #----------------------
                    $Response = Invoke-WcsRest -TargetList localhost -Command "ClearBladeLog?bladeid=$BladeIndex" -ErrorAction Stop  -SSL:$SSL 
                }
            }
            #----------------------------
            # Stop if found an error
            #----------------------------
            If ($FoundError)
            {
                $CurrentFail++
                CoreLib_WriteLog -Value  ''                       -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                CoreLib_WriteLog -Value " CYCLE $Cycle FAILED"    -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host

                Add-Content -Value "$Cycle,FAIL$CsvString" -Path $CSV_FILE

                If ($StopOnFail)
                {       
                    break
                }
            }
            Else
            {
                $CurrentPass++
                CoreLib_WriteLog -Value  ''                      -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                CoreLib_WriteLog -Value " CYCLE $Cycle PASSED"   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host

                Add-Content -Value "$Cycle,PASS$CsvString" -Path $CSV_FILE

            }
        }
        #-----------------------------------------------------------
        # If reached the number of cycles then stop
        #-----------------------------------------------------------
        If ($Cycle -ge $NumberOfCycles)  
        {
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " Remove Cycle-WcsStartup.bat in the blades startup folder "                       -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host

            Return  $WCS_RETURN_CODE_SUCCESS  
        }
        #-----------------------------------------------------------
        # If have errors and stop on fail then stop
        #-----------------------------------------------------------
        ElseIf  (($CurrentFail -ne 0) -and ($StopOnFail))
        {
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " CYCLE TEST ABORTED BY STOP ON FAIL"                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                                  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " Remove Cycle-WcsStartup.bat in the blades startup folder "                       -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                                -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            Return 1 
        }

    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo

        CoreLib_WriteLog -Value $_.ErrorDetails -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE   
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }
            
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR
    }
}
#-------------------------------------------------------------------------------------
# Cycle-WcsUpdate 
#-------------------------------------------------------------------------------------
Function Cycle-WcsUpdate() {

   <#
  .SYNOPSIS
   Cycles between two WCS update scripts

  .DESCRIPTION
   Cycles between two versions, typically N and N-1 BIOS.  OS reboots are done between the updates.
  
   On each cycle:
     (1) If a command was specified with -Run it is run
     (2) The config is read and compared against a reference config.  
     (3) The Windows System Event Log and BMC SEL are checked for suspect errors.
     (4) The update is run

   The boot and cycle time can also be verified on each cycle to be within a user specified maximum
   time.  The boot time is the reboot time which does not include the time for the update and checks.
   The cycle time is the entire time to complete one cycle.

   By default results are stored in <InstallDir>\Results\Cycle-WcsUpdate\<Date-Time>\ directory
   Note the default <InstallDir> is \WcsTest

   RUN CYCLE-WCSUPDATE WITH THE SAME ACCOUNT USED FOR AUTO-LOGIN
   DO NOT LOGIN WITH ANOTHER ACCOUNT WHILE RUNNING

   To run this command the following must be setup beforehand:

       1.  Autologin must be enabled.  To enable run "Set-Autologin" or write the registry
           directly. 
       
       2.  A folder for each version of the update located under <InstallDir>\Scripts\Updates
           For example, \WcsTest\Scripts\Updates\BIOS\3B05 for BIOS version 3B05
          
       3.  A reference configuration file must exist for both versions of updates.  To generate 
           a reference config file run "Log-WcsConfig Update -Path <path>" 
           where <path> is the complete path to the update folder such as...

            \WcsTest\Scripts\Updates\BIOS\3B05\

            GENERATE THE REFERENCE CONFIG FILES WITH THE CORRECT CONFIG!! 

       4.  An WcsUpdate.ps1 in each  folder that does the complete  update.  

   Before each reboot there is a 30 second pause where the user can hit <Enter> to stop the 
   test.  

  .EXAMPLE
   Cycle-WcsUpdate -NumberOfCycles 200 -NewUpdate BIOS\3b07 -OldUpdate BIOS\3b05

   Executes 200 BIOS update and OS reboot cycles going between BIOS 3b05 and 3b07   

  .PARAMETER NumberOfCycles
   Number of cycles to run

  .PARAMETER NewUpdate
   Directory name of the new update.  Script updates to new update on odd counts.
   The complete path is <InstallDir>\Scripts\Updates\$NewUpdate

  .PARAMETER OldUpdate
   Directory name of the old update to flash.  Script updates to old update on even counts
   The complete path is <InstallDir>\Scripts\Updates\$OldUpdate

  .PARAMETER LogDirectory
   Logs results in this directory. If not specified logs results in:
   
    <InstallDir\Results\<FunctionName>\<DateTime>

  .PARAMETER IncludeSelFile
   XML file that contains SEL entries to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeSelFile
   XML file that contains SEL entries to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER IncludeEventFile
   XML file that contains Windows System Events to include as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER ExcludeEventFile
   XML file that contains Windows System Events to exclude as suspect errors

   See <InstallDir>\Scripts\References for example file

  .PARAMETER StopOnFail
   If specified script stops when a failure occurs

  .PARAMETER MaxCycleTime
   Specifies the maximum allowed  time for each cycle.  Expressed as a floating
   point.  For example, -MaxCycleTime 4.5 specifies a boot time of 4 minutes 30 seconds.

   Cycle time include the time to run the update, check the configuration and errors.

   Boot time is ony the time for the reboot or power cycles and does not include the time 
   for the update and other checks.

   If not specified then cycle time is not checked.

  .PARAMETER MaxBootTime
   Specifies the maximum allowed boot time for each cycle.  Expressed as a floating
   point.  For example, -MaxBootTime 4.5 specifies a boot time of 4 minutes 30 seconds.

   If not specified then boot time is not checked.

  .PARAMETER Run
   Command or script to run at start of each cycle before configuration and 
   error checks.   If returns anything other than 0 then the cycle fails.

   The command/script must accept the -LogDirectory input parameter but the
   -LogDirectory input parameter MUST NOT be specified.  Example command:

       Run-QuickStress -TimeInMin 5

   The above command will be appended with -LogDirectory <cycle directory> so the 
   quickstress results are stored in the same directory as the other cycle results.

  .PARAMETER StopOnFail
   If specified script stops when a failure occurs

  .PARAMETER CompareRefOnly
   If specified script the configuration comparison use the -OnlyRefDevice switch
   instead of the -Exact switch.  This is to allow a generic recipe file to be used
   instead of doing an exact match

  .PARAMETER Running
   For internal use only.  Do not specify.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Cycle

   #>
        
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param(
                [Parameter(Mandatory=$true,Position=0)]  [int]     $NumberOfCycles,
                [Parameter(Mandatory=$true)]             [string]  $NewUpdate,
                [Parameter(Mandatory=$true)]             [string]  $OldUpdate,
                [Parameter(Mandatory=$false)]            [string]  $LogDirectory      =  '',
                [Parameter(Mandatory=$false)]            [string]  $IncludeSelFile    =  '',
                [Parameter(Mandatory=$false)]            [string]  $ExcludeSelFile    =  '',
                [Parameter(Mandatory=$false)]            [string]  $IncludeEventFile  =  '',
                [Parameter(Mandatory=$false)]            [string]  $ExcludeEventFile  =  '',
                [Parameter(Mandatory=$false)]            [string]  $Run               =  '',
                [Parameter(Mandatory=$false)]            [float]   $MaxBootTime       =  0,
                [Parameter(Mandatory=$false)]            [float]   $MaxCycleTime      =  0,
                                                         [switch]  $StopOnFail,
                                                         [switch]  $CompareRefOnly,
                                                         [switch]  $Running
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Setup vars and constants
        #-------------------------------------------------------
        $ConfigResult       = $null
        $ErrorCount         = 0
        $CurrentFail        = 0
        $CurrentPass        = 0

        $LogDirectory       = BaseLib_GetLogDirectory $LogDirectory $FunctionInfo.Name

        $SUMMARY_FILE       = "$LogDirectory\WcsUpdate-Summary.log"
        $COUNT_FILE         = "$LogDirectory\WcsUpdate-Count.log"
        $CSV_FILE           = "$LogDirectory\WcsUpdate-CsvSummary.csv"
        $OldUpdatePath      = "$WCS_UPDATE_DIRECTORY\$OldUpdate"
        $NewUpdatePath      = "$WCS_UPDATE_DIRECTORY\$NewUpdate"

        $IncludeExcludeArgs = ''

        If ($IncludeSelFile -ne '')   { $IncludeExcludeArgs += " -IncludeSelFile $IncludeSelFile" }
        If ($ExcludeSelFile -ne '')   { $IncludeExcludeArgs += " -ExcludeSelFile $ExcludeSelFile" }
        If ($IncludeEventFile -ne '') { $IncludeExcludeArgs += " -IncludeEventFile $IncludeEventFile" }
        If ($ExcludeEventFile -ne '') { $IncludeExcludeArgs += " -ExcludeEventFile $ExcludeEventFile" }
        #------------------------------------------------
        # Verify not running in WinPE
        #------------------------------------------------
        If (CoreLib_IsWinPE)
        {
            Throw "This function does not run in the WinPE OS"
        }
        #-----------------------------------------------------------
        # If starting make the setup .bat file and check autologin
        #----------------------------------------------------------- 
        If (-NOT $Running)
        {
            #-----------------------------------------------------------
            # Display and log info on starting the cycling
            #-----------------------------------------------------------
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value (" {0}" -f $MyInvocation.Line.Trim())  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_CYCLE_START_STRING               -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         
            CoreLib_WriteLog -Value  " Log directory $LogDirectory"        -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE         -PassThru | Write-Host
            #-----------------------------------------------------------
            # Start the CSV file 
            #-----------------------------------------------------------
            Set-Content -Value 'CycleNumber,CycleTime(Min),BootTime(Min),Result' -Path $CSV_FILE
            #-----------------------------------------------------------
            # Check for the config files before starting
            #-----------------------------------------------------------
            If (-NOT (Test-Path "$OldUpdatePath\Update.config.xml" ))
            {
                Throw "Could not find configuration file '$OldUpdatePath\Update.config.xml'"  
            }
            If (-NOT (Test-Path "$NewUpdatePath\Update.config.xml" ))
            {
                Throw "Could not find configuration file '$NewUpdatePath\Update.config.xml'"  
            }
            #-----------------------------------------------------------
            # Verify autologin enabled
            #-----------------------------------------------------------
            If (-NOT (AutoLoginEnabled))
            {
                Throw " Autlogin is not enabled.  Please enable Autologin using regedit or Set-Autologin" 
            }
            #-----------------------------------------------------------
            # Setup the startup file for next cycle
            #-----------------------------------------------------------
            Remove-Item "$WCS_OS_STARTUP_DIRECTORY\Cycle-*.bat" -Force -ErrorAction SilentlyContinue | Out-Null

            $CommandToRun = "powershell -command . $WCS_SCRIPT_DIRECTORY\wcsscripts.ps1;cycle-WcsUpdate  $NumberOfCycles -new $NewUpdate -old $OldUpdate -LogDirectory $LogDirectory  -StopOnFail:`$$StopOnFail -Running $IncludeExcludeArgs -Run '$Run' -MaxBoot $MaxBootTime -MaxCycle $MaxCycleTime -CompareRefOnly:`$$CompareRefOnly" 
      
            Set-Content -Value  $CommandToRun  -Path $WCS_STARTUP_BAT_FILE

            $CurrentCycle = 0
        }
        #-----------------------------------------------------------
        # Else get the current cycle
        #-----------------------------------------------------------
        Else
        {
            If (-NOT (Test-Path $WCS_STARTUP_BAT_FILE))
            {
                Throw "Did not find the startup bat file. DO NOT USE -Running"
            }
            #-------------------------------------------------------------------
            #  Get the current counts from the file
            #-------------------------------------------------------------------
            If (-NOT (Test-Path $COUNT_FILE))
            {
                Throw "Aborting script because could not find the count file '$COUNT_FILE'"   
            }

            $CycleFileContent = (Get-Content $COUNT_FILE)
            
            $CurrentCycle =  ([int] $CycleFileContent.split()[0]) + 1
            $CurrentFail  =  [int] $CycleFileContent.split()[1]
            $CurrentPass  =  [int] $CycleFileContent.split()[2]
            #-------------------------------------------------------------------
            # Calculate the last cycle and boot times
            #-------------------------------------------------------------------
            $LastCycleStartTime = $null
            $LastBootStartTime  = $null

            Get-Content -Path $SUMMARY_FILE | Where-Object {$_ -ne $null} | ForEach-Object {

                If ($_.Contains($WCS_CYCLE_START_STRING))
                {
                    [datetime] $LastCycleStartTime = ($_.Substring(1,$_.IndexOf(']')-1) -as [datetime])
                }
                If ($_.Contains($WCS_BOOT_START_STRING))
                {
                    [datetime] $LastBootStartTime   = ($_.Substring(1,$_.IndexOf(']')-1) -as [datetime])
                }
            }
            If (($null -eq $LastCycleStartTime) -or ($null -eq $LastBootStartTime))
            {
                Throw "Aborting script because could not find the last boot time or cycle time in the summary file"  
            }
 
            $ThisCycleStartTime = Get-Date
            $MaxCycleTimeInMin     = ($ThisCycleStartTime - $LastCycleStartTime).TotalMinutes
            $MaxBootTimeInMin      = ($ThisCycleStartTime - $LastBootStartTime).TotalMinutes  
            #-------------------------------------------------------------------
            #  Display and log the cycle start and last cycle times
            #-------------------------------------------------------------------
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                             -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value   " Cycle $CurrentCycle of $NumberOfCycles total cycles "     -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                             -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_CYCLE_START_STRING                                      -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE            
            #-------------------------------------------------------------------
            #  If cycle time specified verify in the range
            #-------------------------------------------------------------------
            If ((0 -ne $MaxCycleTime) -and ($MaxCycleTimeInMin -gt $MaxCycleTime))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value  (" Last Cycle Time:   {0:F3} Minutes - FAIL.  Longer then specified {1:F3}" -f $MaxCycleTimeInMin,$MaxCycleTime) -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE -PassThru | Write-Host
            }
            Else
            {
                CoreLib_WriteLog -Value  (" Last Cycle Time:   {0:F3} Minutes" -f $MaxCycleTimeInMin )   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            }
            #-------------------------------------------------------------------
            #  If boot time specified verify in the range
            #-------------------------------------------------------------------
            If ((0 -ne $MaxBootTime) -and ($MaxBootTimeInMin -gt $MaxBootTime))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value  (" Last Boot Time:    {0:F3} Minutes - FAIL.  Longer then specified {1:F3}" -f $MaxBootTimeInMin,$MaxBootTime) -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE -PassThru | Write-Host
            }
            Else
            {
                CoreLib_WriteLog -Value  (" Last Boot Time:    {0:F3} Minutes" -f $MaxBootTimeInMin )   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            }

            CoreLib_WriteLog -Value  '' -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  
        }
        #----------------------------------------------------------------------------------------------
        # If even count then update to the old update,  So on first cycle (0) updates to the old update
        #----------------------------------------------------------------------------------------------
        If (0 -eq ($CurrentCycle % 2))
        {
            $RefUpdateConfig       = Get-WcsConfig Update -Path $OldUpdatePath
            $UpdateArgs            = "$NewUpdatePath\$WCS_UPDATE_SCRIPTFILE"
        }
        Else
        {
            $RefUpdateConfig       = Get-WcsConfig Update -Path $NewUpdatePath
            $UpdateArgs            = "$OldUpdatePath\$WCS_UPDATE_SCRIPTFILE"
        }
        #----------------------------------------------------------------------------------
        # If start of test just backup and clear event logs, else backup and check for 
        # event logs for errors and check the configuration too
        #----------------------------------------------------------- ----------------------
        If (0 -eq $CurrentCycle) 
        {
            If (0 -ne (Clear-WcsError ))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value 'Could not clear error logs'   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
        }
        Else
        {
            #-----------------------------------------------------------
            # Make a folder for the logs
            #----------------------------------------------------------- 
            $CycleDirectory = ("$LogDirectory\Cycle{0}" -f $CurrentCycle)

            New-Item $CycleDirectory -ItemType Container -ErrorAction SilentlyContinue | Out-Null

            #-----------------------------------------------------------
            # Run command or script specified
            #----------------------------------------------------------- 
            If ('' -ne $Run)    
            {
                Try
                {
                    $RunDirectory  = "$CycleDirectory\Run"

                    $Results = (Invoke-Expression -Command "$Run -LogDirectory $RunDirectory" -ErrorAction Stop)

                    If ((-NOT ($Results -is [int])) -or ($Results -ne 0))
                    {
                        $ErrorCount++
                        CoreLib_WriteLog -Value (" Run '{0}' failed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
                    } 
                    Else
                    {
                        CoreLib_WriteLog -Value (" Run '{0}' passed" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE   
                    }

                }
                Catch
                {
                    $ErrorCount++
                    CoreLib_WriteLog -Value (" Run '{0}' had exception" -f  $Run)  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
                }
            }
            #-----------------------------------------------------------
            # Check for errors
            #-----------------------------------------------------------        
            $ErrorDirectory = "$CycleDirectory\Check-WcsError"   
              
            $EventErrors = Check-WcsError -LogDirectory $ErrorDirectory -IncludeEventFile $IncludeEventFile -ExcludeEventFile $ExcludeEventFile -IncludeSelFile $IncludeSelFile -ExcludeSelFile $ExcludeSelFile 

            If (0 -ne $EventErrors)
            {
                $ErrorCount++
                CoreLib_WriteLog -Value " Check-WcsError found errors"  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
            Else
            {
                CoreLib_WriteLog -Value " Check-WcsError passed"  -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  
            }


            If (0 -ne (Clear-WcsError ))
            {
                $ErrorCount++
                CoreLib_WriteLog -Value ' Could not clear error logs'   -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  -PassThru | Write-Host
            }
            #-----------------------------------------------------------
            # Compare configurations
            #-----------------------------------------------------------             
            If ($CompareRefOnly)
            {
                $Mismatches = Compare-WcsConfig -RefConfig  $RefUpdateConfig -RefToResults ([ref] $ConfigResult) -OnlyRefDevices
            }
            Else
            {
                $Mismatches = Compare-WcsConfig -RefConfig  $RefUpdateConfig  -RefToResults ([ref] $ConfigResult) -Exact
            }

            If (0 -ne $Mismatches)
            {
                $ErrorCount++
                CoreLib_WriteLog -Value " Configuration check found mismatches"  -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Log-WcsConfig -Config $ConfigResult -File "ConfigMisMatch" -Path $CycleDirectory
            }
            Else
            {
                CoreLib_WriteLog -Value " Configuration check passed"  -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   
            }
        }
        #-----------------------------------------------------------
        # Update  
        #----------------------------------------------------------- 
        CoreLib_WriteLog -Value " Starting update '$UpdateArgs'" -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE  -PassThru | Write-Host  

        $ReturnCode = & "$UpdateArgs"

        CoreLib_WriteLog -Value ' ' -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host

        If (0 -ne $ReturnCode)
        {
            $ErrorCount++ 
            CoreLib_WriteLog -Value " Update failed. Returned code $ReturnCode" -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE  -PassThru | Write-Host  
        }
        Else
        {
            CoreLib_WriteLog -Value " Update passed" -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE  -PassThru | Write-Host  
        }
        #-----------------------------------------------------------
        # Write the result
        #-----------------------------------------------------------
        If ($CurrentCycle -ne 0)
        {
            CoreLib_WriteLog -Value ' ' -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host

            If  ($ErrorCount -eq 0) 
            { 
                $CurrentPass++
                CoreLib_WriteLog -Value (" CYCLE {0} PASSED" -f $CurrentCycle)   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Add-Content -Value ("{0},{1:F3},{2:F3},PASS" -f $CurrentCycle,$MaxCycleTimeInMin,$MaxBootTimeInMin) -Path $CSV_FILE
            }
            Else                    
            { 
                $CurrentFail++
                CoreLib_WriteLog -Value (" CYCLE {0} FAILED" -f $CurrentCycle)   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                Add-Content -Value ("{0},{1:F3},{2:F3},FAIL" -f $CurrentCycle,$MaxCycleTimeInMin,$MaxBootTimeInMin) -Path $CSV_FILE
            }
        }

        Set-Content -Value "$CurrentCycle $CurrentFail $CurrentPass" -Path $COUNT_FILE 
        #-----------------------------------------------------------
        # If reached the number of cycles then stop
        #-----------------------------------------------------------
        If ($CurrentCycle -ge $NumberOfCycles)  
        {
            CoreLib_WriteLog -Value  ''                                                                            -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host

            Remove-Item $COUNT_FILE           -Force -ErrorAction SilentlyContinue | Out-Null 
            Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
            Return  $WCS_RETURN_CODE_SUCCESS  
        }
        #-----------------------------------------------------------
        # If have errors and stop on fail then stop
        #-----------------------------------------------------------
        ElseIf  (($ErrorCount -ne 0) -and ($StopOnFail))
        {
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " CYCLE TEST ABORTED BY STOP ON FAIL"                                          -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  ''                                                                            -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value " [FINAL CYCLE RESULTS]  PASS: $CurrentPass  FAIL: $CurrentFail "              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host
            CoreLib_WriteLog -Value  $WCS_HEADER_LINE                                                              -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE    -PassThru | Write-Host

            Remove-Item $COUNT_FILE           -Force -ErrorAction SilentlyContinue | Out-Null 
            Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
            Return 1 
        }
        #-----------------------------------------------------------
        # Reboot - Give the user 30 seconds to abort 
        #-----------------------------------------------------------
        else
        {
            Write-Host  "`n`r`n`r Hit <ENTER> to abort testing`r`n`r`n"

            For ($TimeOut=0;$TimeOut -lt 60;$TimeOut++)
            {
                Start-Sleep -Milliseconds 500
                if ([console]::KeyAvailable)
                {
                    [console]::ReadLine()
                    CoreLib_WriteLog -Value " User aborted testing."   -Function $FunctionInfo.Name -LogFile $SUMMARY_FILE   -PassThru | Write-Host
                    Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null
                    Return $WCS_RETURN_CODE_INCOMPLETE 
                }
            }       
            CoreLib_WriteLog  -Value $WCS_BOOT_START_STRING    -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE                  
            shutdown.exe /r /t 1   | Out-Null        
        }

        Return $WCS_RETURN_CODE_SUCCESS  
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        Remove-Item $WCS_STARTUP_BAT_FILE -Force -ErrorAction SilentlyContinue | Out-Null

        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo

        CoreLib_WriteLog -Value $_.ErrorDetails -Function $FunctionInfo.Name  -LogFile $SUMMARY_FILE  

        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }
            
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR
    }
}
