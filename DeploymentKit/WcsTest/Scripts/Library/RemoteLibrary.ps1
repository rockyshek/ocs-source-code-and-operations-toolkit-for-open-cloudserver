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

Set-Alias -Name chcred  -Value Set-WcsChassisCredential
Set-Alias -Name blcred  -Value Set-WcsBladeCredential

#-------------------------------------------------------------------------------------
# Internal Helper Function: Gets a valid target from ip, hostname, or object
#-------------------------------------------------------------------------------------
Function GetValidTarget($Target,$PrependHttp=$false,$SSL=$false)
{
    Switch($Target.GetType())
    {
        "string"
        { 
            If ($PrependHttp)
            {
                If ($SSL) { $Target = "https:\\{0}" -f $Target  }
                Else      { $Target = "http:\\{0}"  -f $Target  }
            }
            Write-Verbose "Adding string target '$Target'"
            Return $Target 
            break 
        }
        "hashtable"  
        {
            If ($Target.ContainsKey($WCS_TYPE))
            {
                If ($Target.IP -ne $WCS_NOT_AVAILABLE) 
                { 
                    If ($PrependHttp)
                    {
                        If ($Target.SSL) { $Target = "https:\\{0}" -f $Target.IP }
                        Else             { $Target = "http:\\{0}"  -f $Target.IP }
                        Write-Verbose ("Adding target '{0}'" -f $Target)
                        Return $Target
                    }
                    else
                    {
                        Write-Verbose ("Adding target '{0}'" -f $Target.IP)
                        Return $Target.IP
                    }
                }
                Else
                {
                    Write-Verbose ("Adding target '{0}'" -f $Target.Hostname)
                    Return $Target.Hostname
                }
            }
            Write-Verbose ("Invalid target '{0}' did not have WcsType" -f $Target)
            Return $null
        }
        default
        {
            Write-Verbose ("Invalid target '{0}' has type '{1}'" -f $Target, $Target.GetType())
            Return $null
        }
    }
}
#-------------------------------------------------------------------------------------
# Internal Helper Function: Gets a list of valid targets
#-------------------------------------------------------------------------------------
function GetTargets($TargetList,$PrependHttp=$false,$SSL=$false)
{
    If ($null -eq $TargetList) 
    { 
        Write-Host -ForegroundColor Red  "GetTargets: No targets provided"
        Return $null
    }

    If ($TargetList.GetType().BaseType.Name -eq "Array")
    {
        Write-Verbose ("Adding array of {0} targets " -f $TargetList.Count)
        $ReturnTargets = @()

        ForEach ($Target in $TargetList)
        {
            $NewTarget = (GetValidTarget $Target $PrependHttp $SSL)

            If ($null -eq $NewTarget)
            {
                Write-Host -ForegroundColor Red  ("GetTargets: Target '{0}' not a valid target in list" -f $Target)
                Return $null
            }
            Else
            {
                $ReturnTargets += $NewTarget
            }
        }
    }
    Else
    {
        $NewTarget = (GetValidTarget $TargetList $PrependHttp $SSL)
        If ($null -eq $NewTarget)
        {
            Write-Host -ForegroundColor Red  ("GetTargets: Target '{0}' not a valid target" -f $TargetList)
            Return $null
        }
        $ReturnTargets = [array] $NewTarget
    }  
    Return $ReturnTargets 
}
#-------------------------------------------------------------------------------------
# Internal Helper Function: Copies an entire directory and it's children
#-------------------------------------------------------------------------------------
Function CopyWcsDirectory()
{
    Param
    (
        [string] $Source,
        [string] $Dest,
        [string] $Files= ''
    )
    Try
    {
        If (($Files -eq '') -or ($Files -eq '*.*') -or ($Files -eq '*'))
        {
            Write-Verbose " RoboCopy.exe $Source $Dest /E /R:1 /W:0 /ndl /nfl /njh /njs "
            RoboCopy.exe $Source $Dest  /E /R:1 /W:0 /ndl /nfl /njh /njs | Out-Null
        }
        Else
        {
            Write-Verbose " RoboCopy.exe $Source $Dest $Files /R:1 /W:0 /ndl /nfl /njh /njs "
            RoboCopy.exe $Source $Dest $Files /R:1 /W:0 /ndl /nfl /njh /njs | Out-Null
        }

        If ($LASTEXITCODE -gt 7)  
        { 
            Throw "Robocopy failed with return code $LASTEXITCODE" 
        }

        Return 0
    }
    Catch
    {
        Return 1
    }
}

#-------------------------------------------------------------------------------------
# Internal Helper Function: Reads IP addresses from a file
#-------------------------------------------------------------------------------------
Function Get-IpFromFile($FileName)
{
    Return Get-Content -Path $FileName -ErrorAction SilentlyContinue | Where-Object { $_.Trim() -ne "" }
}
#-------------------------------------------------------------------------------------
# Gets the credentials to use for accessing WCS Chassis Manager 
#-------------------------------------------------------------------------------------
Function Set-WcsChassisCredential()
{
 <#
  .SYNOPSIS
   Sets the credentials to use to access WCS Chassis Manager

  .DESCRIPTION
   Sets the credentials to use to access WCS Chassis Manager for the current 
   session.  Must be re-entered each time PowerShell starts.

  .EXAMPLE
   Set-WcsChassisCredential -SetDefault

   Restores the credentials to the defaults

  .EXAMPLE
   Set-WcsChassisCredential -User 'Admin' -Password 'NewPassword'

   Sets new credentials that will be used for all commands that remotely access
   or communicate with chassis managers.

  .PARAMETER SetDefault
   Sets the credentials to the default credentials

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote
 
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true)] [string]  $User,
        [Parameter(Mandatory=$true)] [string]  $Password,
                                     [switch]  $SetDefault
    )
    If ($SetDefault) { $Global:ChassisManagerCredential = $Global:ChassisManagerCredential  }
    Else             { $Global:ChassisManagerCredential =  new-object system.management.automation.pscredential($User,  (ConvertTo-SecureString $Password -asPlainText  -Force))}
}

#-------------------------------------------------------------------------------------
# Gets the credentials to use for accessing WCS Blades 
#-------------------------------------------------------------------------------------
Function Set-WcsBladeCredential()
{
 <#
  .SYNOPSIS
   Sets the credentials to use to access WCS Blades

  .DESCRIPTION
   Sets the credentials to use to access WCS Blades for the current session.     
   Must be re-entered each time PowerShell starts.

  .EXAMPLE
   Set-WcsBladeCredential -SetDefault

   Restores the credentials to the defaults

  .EXAMPLE
   Set-WcsBladeCredential -User 'Admin' -Password 'NewPassword'

   Sets new credentials that will be used for all commands that remotely access
   or communicate with WCS blades.

  .PARAMETER SetDefault
   Sets the credentials to the default credentials

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true)] [string]  $User,
        [Parameter(Mandatory=$true)] [string]  $Password,
                                     [switch]  $SetDefault
    )
    If ($SetDefault) { $Global:BladeCredential = $Global:BladeDefaultCredential  }
    Else             { $Global:BladeCredential =  new-object system.management.automation.pscredential($User,  (ConvertTo-SecureString $Password -asPlainText  -Force))}
}

#-------------------------------------------------------------------------------------
# Invoke-WcsCommand 
#-------------------------------------------------------------------------------------
Function Invoke-WcsCommand()
{    
<#
  .SYNOPSIS
   Runs a WCS command on one or more remote WCS systems (targets)

  .DESCRIPTION
   Runs a WCS command on one or more remote WCS systems (targets).  Targets can be 
   specified by their IP address or hostname  
   
   All targets must use the same credentials, be accessible on the network, and have
   the OS configured for remote execution.  

   The remote systems must have the Toolkit installed in the default location: c:\WcsTest

  .EXAMPLE
   Invoke-WcsCommand -TargetList $BladeList -Command 'Log-WcsConfig (Get-WcsConfig) ConfigFile' 

   The above command logs the configuration of all systems in $BladeList to their
   <InstallDir>\Configurations directory

  .PARAMETER TargetList
   List of remote targets to run the script on.  Targets can be specified with an IP address  
   or hostname. Examples:

   192.168.200.10
   @(192.168.200.10, 192.168.200.11)
   @('host1','host2')

  .PARAMETER Command
   Command to run enclosed in single quotes.  

  .PARAMETER Chassis
   If specified uses the default chassis manager credentials.  If not specified uses the default
   blade credentials.

  .PARAMETER WaitTimeInSec
   Time to wait for the command to complete in seconds.  If the command does not complete before
   WaitTimeInSec then an error is generated.

  .PARAMETER CommandResults
   Reference to an array holding the exit code returned by the command for each target.  Commands return
   0 when pass and non-zero when fail.  The array index is the same as the TargetList index.  For example, the 
   result of TargetList[3] is CommandResults[3].

  .PARAMETER Transcript
   Transcript filename to create on remote target.  If not specified logs to default transcript file.

  .OUTPUTS
   Returns number of errors found.  A target that returns an error for the command is considered one error.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true)]                     $TargetList,
        [Parameter(Mandatory=$true)]      [string]       $Command,
        [Parameter(Mandatory=$false)]     [switch]       $Chassis,
        [Parameter(Mandatory=$false)]     [int]          $WaitTimeInSec = 300,
        [Parameter(Mandatory=$false)]     [ref]          $CommandResults,
        [Parameter(Mandatory=$false)]     [string]       $Transcript=''
    )

    Try
    {        
        $ErrorCount           = 0
        $MaxCommandCharacters = 200

        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Check length of command
        #-------------------------------------------------------
        If (($Command.Length) -gt $MaxCommandCharacters)
        {
            Throw ("The number of characters in Command must be less than {0} but found {1} characters" -f $MaxCommandCharacters,$Command.Length) 
        }
        #-------------------------------------------------------
        # Get the credentials
        #-------------------------------------------------------
        If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
        Else          { $Credential = $Global:BladeCredential           }
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password                 
        #-------------------------------------------------------
        # Get the IPs from the target list
        #-------------------------------------------------------
        $TargetIpList = [array] (GetTargets $TargetList)
        
        If ($CommandResults -ne $null) { $CommandResults.Value = @() }

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 
        #---------------------------------------------------------------------
        # Create full path to the script file
        #---------------------------------------------------------------------
        $Script      = "\WcsTest\Scripts\invoke-remote.ps1"

        If ($Transcript -ne '') 
        {
            $Transcript = "-Transcript $Transcript"
        }
        #-------------------------------------------------------
        # Start script on each target, don't wait for it to finish
        #--------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $PsCommand =  ("\\{0} -i -s -accepteula -u {1} -p {2} {4} -ExecutionPolicy RemoteSigned -command exit(. $Script '$Command' $Transcript)" -f $TargetIpList[$IpAddress],$UserName,$Password,$WCS_SET_EXECUTION_TEMPFILE,$WCS_POWERSHELL_BINARY)
            Write-Verbose ("Running psexec command '{0}'" -f $PsCommand )
            $MyProcess[$IpAddress], $MyProcessOut[$IpAddress], $MyProcessErr[$IpAddress] =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand 
        }
        #-------------------------------------------------------
        # Wait
        #--------------------------------------------------------
        For ($Timeout=0;$TimeOut -lt (2*$WaitTimeInSec); $Timeout++)
        {
            $AllExited = $true

            For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
            {
                If ($MyProcess[$IpAddress].HasExited -eq $false) { $AllExited = $false }
            }
            If ($AllExited) 
            {
                Write-Verbose "All processes exited within timeout`r"
                break
            }
            
            Start-Sleep -Milliseconds 500
        }
        #-------------------------------------------------------
        # Check the command finished
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess[$IpAddress]   $MyProcessOut[$IpAddress]   $MyProcessErr[$IpAddress]  0 -IgnoreExitCode

            If ($CommandResults -ne $null) { $CommandResults.Value += $ExitCode }

            If (0 -ne $ExitCode)   
            {  
                $ErrorCount++
                If ($ExitCode -eq 1326)
                {
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Failed to complete command, returned code {1}.  Access denied`r" -f $TargetIpList[$IpAddress],$ExitCode) 
                }
                Else
                {
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Command returned error code {1}`r" -f $TargetIpList[$IpAddress],$ExitCode) 
                }
            }
            Else                   
            {  
                Write-Verbose ("`t`t[{0}] Command completed" -f $TargetIpList[$IpAddress]) 
            }
        }

        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Invoke-WcsScript 
#-------------------------------------------------------------------------------------
Function Invoke-WcsScript()
{    
<#
  .SYNOPSIS
   Runs a PowerShell script on one or more remote systems (targets)

  .DESCRIPTION
   Runs a PowerShell script on one or more remote systems (targets).  Targets must be 
   specified by their IP address or hostname
   
   All targets must use the same credentials, be accessible on the network, and have
   the OS configured for remote execution.  

   The remote systems must have the Toolkit installed in the default location: c:\WcsTest

  .EXAMPLE
   Invoke-WcsScript -TargetList $ChassisManagerList -Script \WcsTest\BIOS\3A07\Update  

   Runs the c:\WcsTest\BIOS\3A07\Update.ps1 script on all systems listed in the 
   variable $ChassisManagerList

  .PARAMETER TargetList
   List of remote targets to run the script on.  Targets can be specified with an IP address  
   or hostname. Examples:

   192.168.200.10
   @(192.168.200.10, 192.168.200.11)
   @('host1','host2')

  .PARAMETER Script
   Absolute path to script to run. Accepts script names with or without .ps1 extension.  
   For example, to run \WcsTest\bios\WcsUpdate.ps1 use one of:

      -script \WcsTest\bios\WcsUpdate 
      -script \WcsTest\bios\WcsUpdate.ps1

  .PARAMETER ScriptArgs
   Arguments to be passed to the script.  If multiple arguments surround in quotes.  For example:

      -ScriptArgs '-arg1 value -arg2 value2'

  .PARAMETER Chassis
   If specified uses the default chassis manager credentials.  If not specified uses the default
   blade credentials.

  .PARAMETER WaitTimeInSec
   Time to wait for the command to complete

  .PARAMETER ScriptResults
   Reference to an array holding the exit code returned by the script for each target.  Scripts typically return
   0 when pass and non-zero when fail.  The array index is the same as the TargetList index.  For example, the 
   result of TargetList[3] is ScriptResults[3].

  .OUTPUTS
   Returns number of errors found.  A target that returns an error for the command is considered one error.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support
    Param
    ( 
        [Parameter(Mandatory=$true)]                    $TargetList,
        [Parameter(Mandatory=$true)]     [string]       $Script,
        [Parameter(Mandatory=$false)]    [string]       $ScriptArgs  = "",
        [Parameter(Mandatory=$false)]    [switch]       $Chassis,
        [Parameter(Mandatory=$false)]    [int]          $WaitTimeInSec = 300,
        [Parameter(Mandatory=$false)]    [ref]          $ScriptResults
    )

    Try
    {        
        $ErrorCount          = 0
        $MaxScriptCharacters = 200
        $Command             = "$Script $ScriptArgs"
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation 

        #-------------------------------------------------------
        # Check length of scripts/script args
        #-------------------------------------------------------
        If (($Command.Length) -gt $MaxScriptCharacters)
        {
            Throw ("The number of characters in Script and ScriptArgs must be less than {0} but found {1} characters" -f $MaxScriptCharacters,$Command.Length) 
        }
        #-------------------------------------------------------
        # Get the credentials
        #-------------------------------------------------------
        If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
        Else          { $Credential = $Global:BladeCredential           }
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password                 
        #-------------------------------------------------------
        # Get the IPs from the target list
        #-------------------------------------------------------
        $TargetIpList = [array] (GetTargets $TargetList)
        
        If ($ScriptResults -ne $null) { $ScriptResults.Value = @() }

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 
        #---------------------------------------------------------------------
        # Create full path to the script file, add .ps1 extension if missing
        #---------------------------------------------------------------------
        If (-NOT $Script.ToLower().EndsWith(".ps1")) { $Script += ".ps1" }

        #-------------------------------------------------------
        # Start script on each target, don't wait for it to finish
        #--------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $PsCommand =  ("\\{0} -i -s -accepteula -u {1} -p {2} {4} -ExecutionPolicy RemoteSigned -command `"exit(. $Script $ScriptArgs)`"" -f $TargetIpList[$IpAddress],$UserName,$Password,$WCS_SET_EXECUTION_TEMPFILE,$WCS_POWERSHELL_BINARY)
            Write-Verbose ("Running psexec command '{0}'" -f $PsCommand )
            $MyProcess[$IpAddress], $MyProcessOut[$IpAddress], $MyProcessErr[$IpAddress] =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand 
        }
        #-------------------------------------------------------
        # Wait
        #--------------------------------------------------------
        For ($Timeout=0;$TimeOut -lt (2*$WaitTimeInSec); $Timeout++)
        {
            $AllExited = $true

            For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
            {
                If ($MyProcess[$IpAddress].HasExited -eq $false) { $AllExited = $false }
            }
            If ($AllExited) {break}
            
            Start-Sleep -Milliseconds 500
        }
        #-------------------------------------------------------
        # Check the command finished
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess[$IpAddress]   $MyProcessOut[$IpAddress]   $MyProcessErr[$IpAddress]  0 -IgnoreExitCode

            If ($ScriptResults -ne $null) { $ScriptResults.Value += $ExitCode }

            If (0 -ne $ExitCode)   
            {  
                $ErrorCount++
                If ($ExitCode -eq 1326)
                {
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Failed to complete command, returned code {1}.  Access denied`r" -f $TargetIpList[$IpAddress],$ExitCode) 
                }
                Else
                {
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Command returned error code {1}`r" -f $TargetIpList[$IpAddress],$ExitCode) 
                }
            }
            Else                   
            {  
                Write-Verbose ("`t`t[{0}] Command completed" -f $TargetIpList[$IpAddress]) 
            }
        }
        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Map-WcsDrives
#-------------------------------------------------------------------------------------
Function Map-WcsDrive()
{ 
<#
  .SYNOPSIS
   Runs maps drives to allow for file copy

  .DESCRIPTION
   Maps drives using net use to allow file copy

  .PARAMETER TargetIpList
   List of remote targets to map the drives on.  Targets can be specified with an IP address  
   or hostname. Examples:

   192.168.200.10
   @(192.168.200.10, 192.168.200.11)
   @('host1','host2')

  .PARAMETER ReMapDrives
   If specified then deletes the network connection before mapping

  .PARAMETER Chassis
   If specified uses the default chassis manager credentials.  If not specified uses the default
   blade credentials.

  .OUTPUTS
   Returns 0 on success.  Non-zero on failure.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

#>
    Param
    (
        [Parameter(Mandatory=$true)]  [string[]]  $TargetIpList,
        [Parameter(Mandatory=$false)] [string]    $DrivePath  = '',
                                      [switch]    $ReMapDrives,
                                      [switch]    $Chassis
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Setup the variables
        #-------------------------------------------------------
        If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
        Else          { $Credential = $Global:BladeCredential           }
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password   
                              
        $NetUseStatus = @{}

        $TempLogFile = "$WCS_RESULTS_DIRECTORY\netuse.log"
        $TempErrFile = "$WCS_RESULTS_DIRECTORY\netuse-err.log"
         
        #--------------------------------------------------------------------
        # Verify service is running
        #--------------------------------------------------------------------
        $NetService = Get-Service Server -ErrorAction SilentlyContinue

        If (($null -eq $NetService) -or ($NetService.Status -ne 'Running'))
        {
            Write-Verbose "Starting server service"
            Start-Service Server -ErrorAction Stop
        }

        #--------------------------------------------------------------------
        # If ReMapDrives not specified then read the drives already mapped
        #--------------------------------------------------------------------
        If ( (Host).Version.Major -lt 3) 
        {
            $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList 'use' -Wait -PassThru -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile -ErrorAction Stop 
        }
        Else
        {
            $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList 'use' -Wait -PassThru -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile  -ErrorAction Stop -WindowStyle Hidden
        }

        Get-Content -Path $TempLogFile  -ErrorAction Stop | Where-Object {$_ -ne $null} | ForEach-Object {

            If ($_.Trim().EndsWith("Microsoft Windows Network"))
            {
                $Fields = ($_ -split '\s+')

                If ($Fields.Count -eq 5)     { $NetUseRemoteDrive = $Fields[1]  }
                ElseIf ($Fields.Count -eq 6) { $NetUseRemoteDrive = $Fields[2]  }   

                $NetUseLocalDrive = $Fields[1] 

                If( $NetUseStatus.ContainsKey($NetUseRemoteDrive)) { $NetUseStatus[$NetUseRemoteDrive] += $NetUseLocalDrive }
                Else                                               { $NetUseStatus[$NetUseRemoteDrive] = @($NetUseLocalDrive) }
            }
        }

        #-------------------------------------------------------
        # Map the drive with net use
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $DriveToMap = ("\\{0}$DrivePath" -f $TargetIpList[$IpAddress]) 

            Write-Verbose "Drive to map $DriveToMap"

            If ($ReMapDrives)
            {
                If ( $NetUseStatus.ContainsKey($DriveToMap)) 
                {
                    $NetUseStatus[$DriveToMap] | Where-Object {$_ -ne $null} | ForEach-Object { 
                    
                        Write-Verbose "Deleting $_"
                        If ( (Host).Version.Major -lt 3) 
                        {
                            $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList "use $_ /DELETE /Y" -Wait -PassThru -ErrorAction Stop   -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile 
                        }
                        Else
                        {
                            $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList "use $_ /DELETE /Y" -Wait -PassThru -ErrorAction Stop    -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile  -WindowStyle Hidden
                        }
                
                    }                
     
                }
                Else 
                {
                    Write-Verbose "Deleting $DriveToMap "

                    If ( (Host).Version.Major -lt 3) 
                    {
                        $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList "use $DriveToMap /DELETE /Y" -Wait -PassThru -ErrorAction Stop   -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile 
                    }
                    Else
                    {
                        $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList "use $DriveToMap /DELETE /Y" -Wait -PassThru -ErrorAction Stop    -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile  -WindowStyle Hidden
                    }
                }
                
            }

            If ($ReMapDrives -or (-not $NetUseStatus.ContainsKey($DriveToMap)) )
            {
                $CommandArgs = ("use $DriveToMap  {1} /USER:{2} /PERSISTENT:no" -f $TargetIpList[$IpAddress],$Password,$UserName) 
                
                Write-Verbose "net $CommandArgs"

                If ( (Host).Version.Major -lt 3) 
                {
                    $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList $CommandArgs -Wait -PassThru -ErrorAction SilentlyContinue  -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile 
                }
                Else
                {
                    $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList $CommandArgs -Wait -PassThru -ErrorAction SilentlyContinue  -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile  -WindowStyle Hidden
                }
                If (0 -ne $NetStartProcess.ExitCode) 
                { 
                    Write-host "Trying retry"
                    
                    If ( (Host).Version.Major -lt 3) 
                    {
                        $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList $CommandArgs -Wait -PassThru -ErrorAction Stop  -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile 
                    }
                    Else
                    {
                        $NetStartProcess = Start-Process -FilePath net.exe -ArgumentList $CommandArgs -Wait -PassThru -ErrorAction Stop  -RedirectStandardOutput $TempLogFile -RedirectStandardError $TempErrFile  -WindowStyle Hidden
                    }
                }

                If (0 -eq $NetStartProcess.ExitCode) 
                { 
                    Write-Verbose ("`[{0}] mapped the drive" -f $DriveToMap) 
                }
                Else                   
                { 
                    Throw ("[{0}] Failed to map drive with net use" -f $DriveToMap) 
                }
            }
            else
            {
                Write-Verbose "already mapped"
            }
        }
        Return 0
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Copy-WcsFile 
#-------------------------------------------------------------------------------------
Function Copy-WcsFile()
{  
<#
  .SYNOPSIS
   Copies files to one or more remote systems (targets)

  .DESCRIPTION
   Copies files to one or more remote systems (targets)

  .EXAMPLE
   Copy-WcsFile -TargetList $ListOfIpAddresses -LocalFile Updates\BIOS_3A07\*.*  -Clean

   Copies the files from local directory <InstallDir>\Updates\BIOS_3A07 to all systems
   listed in $ListOfIpAddresses.  Deletes the files in the remote directories before copying.

   Default <InstallDir> is \WcsTest.

  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')

  .PARAMETER LocalFile
   File(s) to copy to the remote systems
  
  .PARAMETER RemoteDirectory
   Directory on remote systems to copy files to.  If not specified then the remote directory
   is the same as the local directory   

   .PARAMETER Chassis
   If specified uses the default chassis manager account.  If not specified uses the default
   blade account.

   .PARAMETER Clean
   If specified deletes the entire directory before copying

   .PARAMETER CopyResults
   Reference to an array holding the result for each target. If copy was successful returns 0, if not 
   successful returns non-zero.  The array index is the same as the TargetList index.  For example, the 
   result of TargetList[3] is CopyResults[3].

  .PARAMETER ReMap
   If specified then deletes the network connection before mapping it

  .OUTPUTS
   Returns number of errors found where one error is one target that was not copied to.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support  

    Param
    ( 
        [Parameter(Mandatory=$true)]  [array]    $TargetList,
        [Parameter(Mandatory=$true)]  [string]   $LocalDirectory,
        [Parameter(Mandatory=$false)] [string]   $LocalFile = '',
        [Parameter(Mandatory=$false)] [string]   $RemoteDirectory='',
                                      [switch]   $Chassis,
                                      [switch]   $Clean,
        [Parameter(Mandatory=$false)] [ref]      $CopyResults,
                                      [switch]   $Remap
    )


    Try
    {  
        $ErrorCount = 0
        $Credential  = $null

        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation 
        
        #-------------------------------------------------------
        # Get the credentials
        #-------------------------------------------------------
        If ($null -eq $Credential)
        {
            If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
            Else          { $Credential = $Global:BladeCredential           }
        }                  
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password   
                              
        #-------------------------------------------------------
        # Setup vars and constants
        #-------------------------------------------------------
        If (-NOT (Test-Path "$LocalDirectory\$LocalFile")) 
        {
            Write-Host -ForegroundColor Red  "`tCould not find local file '$LocalDirectory\$LocalFile'.  Aborting.`r"
            Return $WCS_RETURN_CODE_GENERIC_ERROR
        }

        If ('' -eq $RemoteDirectory) 
        {  
            $RemoteDirectory = Split-Path $LocalDirectory  -NoQualifier -Resolve -ErrorAction Stop
        }
        Else
        {
            $RemoteDrive     = Split-Path $RemoteDirectory -Qualifier    -ErrorAction SilentlyContinue        
            $RemoteDirectory = Split-Path $RemoteDirectory -NoQualifier  -ErrorAction Stop
        }
        If ($null -eq $RemoteDrive)
        {
            $RemoteDrive = 'c'
        }
        Else
        {
            $RemoteDrive = $RemoteDrive.Trim(':')
        }      
        
        $TargetIpList = [array] (GetTargets $TargetList) 

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 
        #-------------------------------------------------------
        # Map the drives
        #-------------------------------------------------------
        If (0 -ne (Map-WcsDrive -Target $TargetIpList -DrivePath '\c$' -Remap:$Remap -Chassis:$Chassis))
        {
            Throw "Could not map drives"
        }

        #-------------------------------------------------------
        # Copy latest files
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $DestinationDirectory = ("\\{0}\$RemoteDrive`$$RemoteDirectory" -f $TargetIpList[$IpAddress])

            Try
            {
                If ($Clean)
                {
                    Write-Verbose "Deleting $DestinationDirectory"
                    Remove-Item  $DestinationDirectory  -Recurse -Force  -ErrorAction SilentlyContinue | Out-Null
                }

                Write-Verbose "Copying $LocalDirectory\$LocalFile to $DestinationDirectory "

                If (0 -ne  (CopyWcsDirectory $LocalDirectory $DestinationDirectory $LocalFile )) 
                { 
                    Throw "Copy failed" 
                }

                #------------------------------------------------------------------
                # Unblock-File only available in PowerShell version 3.0 and later
                #------------------------------------------------------------------
                If ( (Host).Version.Major -lt 3) 
                {
                    Get-ChildItem $DestinationDirectory -include *.exe -Recurse -ErrorAction SilentlyContinue | Where-Object {$_ -ne $null} | ForEach-Object { 
                        Set-ItemProperty $_.FullName -Name IsReadOnly -Value $false
                        cmd /c "echo.>$($_.FullName):Zone.Identifier" 
                    }
                }
                Else
                {
                    Get-ChildItem $DestinationDirectory -include *.exe -Recurse -ErrorAction SilentlyContinue | Unblock-File 
                }

                If ($CopyResults -ne $null) 
                { 
                    $CopyResults.Value += 0 
                }     
                Write-Host ("`t`t[{0}] files copied`r" -f $TargetIpList[$IpAddress]) 
            }
            Catch
            {
                If ($CopyResults -ne $null) { $CopyResults.Value += 1 }
                Write-Verbose $_
                $ErrorCount++
                Write-Host -ForegroundColor Red   ("`t`t[{0}] Could not copy the files. Check directory name and credentials`r" -f $TargetIpList[$IpAddress]) 
            }
        }

        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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

#-------------------------------------------------------------------------------------
# Copy-WcsRemoteFile 
#-------------------------------------------------------------------------------------
Function Copy-WcsRemoteFile()
{  
<#
  .SYNOPSIS
   Copies file(s) from the one or more remote targets (systems) to the local system

  .DESCRIPTION
   Copies file(s) from the one or more remote targets (systems) to the local system

   By default files are copied to:
   
     c:\WcsTest\RemoteFiles\<Target> directory

   but of LocalSubDirectory specified then files are copied to 

     c:\WcsTest\RemoteFiles\<LocalSubDirectory>\<Target> 

   Wildcards can be specified for the RemoteFile parameter.  If RemoteFile is a 
   directory then all files in the directory are copied 

  .EXAMPLE
   Copy-WcsRemoteFile -TargetList $ListOfIpAddresses -RemoteFile \WcsTest\Scripts\WcsScripts.ps1  

   Copies the c:\WcsTest\Scripts\WcsScripts.ps1 file from each remote system
   listed in $ListOfIpAddresses to the local system here...
   
   \<InstallDir>\RemoteFiles\<Target>\c-drive\WcsTest\Scripts\WcsScripts.ps1 

   Where <InstallDir> is \WcsTest by default

  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')

  .PARAMETER RemoteFile
   RemoteFile(s) to copy from the remote targets

   .PARAMETER Chassis
   If specified uses the default chassis manager credentials.  If not specified uses the default
   blade credentials.

   .PARAMETER Clean
   If specified deletes the entire local systems directory before copying

   .PARAMETER LocalSubDirectory
   If specified copies files to ..\RemoteFiles\<LocalSubDirectory>

   .PARAMETER CopyResults
   Reference to an array holding the result for each target. If copy was successful returns 0, if not 
   successful returns non-zero.  The array index is the same as the TargetList index.  For example, the 
   result of TargetList[3] is CopyResults[3].

  .PARAMETER ReMap
   If specified then deletes the network connection before mapping it

  .OUTPUTS
   Returns number of errors found where one error is one target that was not copied from.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support  

    Param
    ( 
        [Parameter(Mandatory=$true)]  [array]         $TargetList,
        [Parameter(Mandatory=$false)] [string]        $RemoteFile="*.*",
        [Parameter(Mandatory=$true)]  [string]        $RemoteDirectory,         #absolute path
        [Parameter(Mandatory=$false)] [ref]           $CopyResults,
        [Parameter(Mandatory=$false)] [string]        $LocalSubDirectory='',
                                      [switch]        $Chassis,
                                      [switch]        $Clean,
                                      [switch]        $ReMap
    )
    Try
    {  
        $ErrorCount = 0
        $Credential  = $null
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Get the credentials
        #-------------------------------------------------------
        If ($null -eq $Credential)
        {
            If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
            Else          { $Credential = $Global:BladeCredential           }
        }                  
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password                      
        #-------------------------------------------------------
        # Get the IPs from the target list
        #-------------------------------------------------------              
        $TargetIpList = [array] (GetTargets $TargetList) 

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 

        $SkipCopy     = New-Object 'System.Object[]'   $TargetIpList.Count 

        $SkipCopy | Where-Object {$_ -ne $null} | ForEach-Object { $_ = $false }

        #-------------------------------------------------------
        # Setup the remote file path.
        #-------------------------------------------------------              
        $RemoteDrive = Split-Path $RemoteDirectory -Qualifier -ErrorAction SilentlyContinue
        If ($null -eq $RemoteDrive)
        {
            $RemoteDrive = 'c'
        }
        Else
        {
            $RemoteDrive = $RemoteDrive.Trim(':')
        }
 
        $RemoteDirectory = Split-Path $RemoteDirectory -NoQualifier

        #-------------------------------------------------------
        # Map the drives
        #-------------------------------------------------------
        If (0 -ne (Map-WcsDrive -Target $TargetIpList -DrivePath '\c$'  -Remap:$Remap -Chassis:$Chassis))
        {
            Throw "Could not map drives"
        }

        #-------------------------------------------------------
        # Copy latest files
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If (-not $SkipCopy[$IpAddress])
            {
                Try
                {


                    $CopySourceDir = ("\\{0}\$RemoteDrive`$$RemoteDirectory" -f $TargetIpList[$IpAddress])

                    $LocalDirectory = Split-Path $RemoteDirectory  -NoQualifier  -ErrorAction Stop

    
                    If ($LocalSubDirectory -ne '')
                    {
                        $CopyDestDir = ("$WCS_REMOTE_RESULTS_DIRECTORY\$LocalSubDirectory\{0}\$RemoteDrive-Drive$LocalDirectory" -f  $TargetIpList[$IpAddress])

                    }
                    Else
                    {
                        $CopyDestDir = ("$WCS_REMOTE_RESULTS_DIRECTORY\{0}\$RemoteDrive-Drive$LocalDirectory" -f  $TargetIpList[$IpAddress])
                    }


                    If ($Clean)
                    {
                        Write-Verbose "Deleting $CopyDestDir"
                        Remove-Item  $CopyDestDir  -Recurse -Force  -ErrorAction SilentlyContinue | Out-Null
                    }
  
                    If (0 -ne (CopyWcsDirectory  $CopySourceDir  $CopyDestDir $RemoteFile))   
                    { 
                        Throw "Copy failed" 
                    }

                    If ($CopyResults -ne $null) { $CopyResults.Value += 0 }   
                             
                    Write-Host ("`t`t[{0}] files copied`r" -f $TargetIpList[$IpAddress]) 
                }
                Catch
                {
                    If ($CopyResults -ne $null) { $CopyResults.Value += 1 }

                    $ErrorCount++
                    Write-Host -ForegroundColor Red     ("`t`t[{0}] Could not copy the files.  Check directory name and credentials`r" -f $TargetIpList[$IpAddress]) 
                }
           }
        }

        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Remove-WcsRemoteFile 
#-------------------------------------------------------------------------------------
Function Remove-WcsRemoteFile()
{  
<#
  .SYNOPSIS
   Removes files from the one or more remote targets to local system 

  .DESCRIPTION
   Removes files from one or more remote targets (systems).  If the -RemoteFile
   is a directory then all files in that directory are removed.  Wildcards
   can be specified.

   Targets can be specified as IP addresses or hostnames

   If -RemoteFile doesn't specify a drive then assumes c:

  .EXAMPLE
   Remove-WcsRemoteFile -TargetList $ListOfIpAddresses -RemoteFile \WcsTest\Scripts

   Removes all files under c:\WcsTest\Scripts on each remote system (target) specified
   in the $ListOfIpAddresses variable

  .EXAMPLE
   Remove-WcsRemoteFile -TargetList 192.168.200.14 -RemoteFile d:\temp\readme.txt

   Removes d:\temp\readme.txt on 192.168.200.14

  .EXAMPLE
   Remove-WcsRemoteFile -TargetList  @('host1','host2') -RemoteFile \WcsTest\re*.txt

   Removes all files called re*.txt in the c:\WcsTest directory of the two systems
   with hostnames host1 and host2
   
  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')

  .PARAMETER RemoteFile
   File(s) to remove on remote system.  Use wildcard to remove multiple file.  For example
   \WcsTest\*.* removes all files under \WcsTest.  If drive not specified uses c:.  If
    a directory specified all files in the directory are removed.

   .PARAMETER Chassis
   If specified uses the default chassis manager credentials.  If not specified uses the default
   blade credentials.

  .PARAMETER ReMap
   If specified then deletes the network connection before mapping

  .OUTPUTS
   Returns number of errors found where one error is one target where remove failed

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support  

    Param
    ( 
        [Parameter(Mandatory=$true)] [array]         $TargetList,
        [Parameter(Mandatory=$true)] [string]        $RemoteFile,
                                     [switch]        $Chassis,
                                     [switch]        $ReMap
    )
    Try
    {  
        $ErrorCount = 0
        $Credential  = $null
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Get the credentials
        #-------------------------------------------------------
        If ($Chassis) { $Credential = $Global:ChassisManagerCredential  }
        Else          { $Credential = $Global:BladeCredential           }
        
        $UserName =  $Credential.UserName
        $Password =  $Credential.GetNetworkCredential().Password                      
        #-------------------------------------------------------
        # Get the IPs from the target list
        #-------------------------------------------------------              
        $TargetIpList = [array] (GetTargets $TargetList) 

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 

        $SkipRemove     = New-Object 'System.Object[]'   $TargetIpList.Count 

        $SkipRemove | Where-Object {$_ -ne $null} | ForEach-Object { $_ = $false }

        #-------------------------------------------------------
        # Setup the remote file path.
        #-------------------------------------------------------              
        $RemoteDrive = Split-Path $RemoteFile -Qualifier -ErrorAction SilentlyContinue
        If ($null -eq $RemoteDrive)
        {
            $RemoteDrive = 'c'
        }
        Else
        {
            $RemoteDrive = $RemoteDrive.Trim(':')
        }

        $RemoteFile = Split-Path $RemoteFile -NoQualifier
        If ((Test-Path $RemoteFile -PathType Container))
        {
            $RemoteFile += "\*.*"
        }
       #-------------------------------------------------------
        # Map the drives
        #-------------------------------------------------------
        If (0 -ne (Map-WcsDrive -Target $TargetIpList -DrivePath "\$RemoteDrive`$"  -Remap:$Remap -Chassis:$Chassis))
        {
            Throw "Could not map drives"
        }
<#
        #-------------------------------------------------------
        # Read the drives already mapped
        #-------------------------------------------------------
        $NetUseStatus = @{}
        net use | Where-Object {$_ -ne $null} | ForEach-Object {

            If ($_.Trim().EndsWith("Microsoft Windows Network"))
            {
               $NetUseStatus[ ($_ -split '\s+')[1] ] = ($_ -split '\s+')[0]     
               Write-Verbose ("net use '{0}' - Status {1} "-f   ($_ -split '\s+')[1] ,($_ -split '\s+')[0])
            }
        }
        $DriveMapped = New-Object 'System.Object[]'   $TargetIpList.Count  
                               
        #-------------------------------------------------------
        # If not already there start net server
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $MapDrive = ("\\{0}\$RemoteDrive`$" -f $TargetIpList[$IpAddress])
            
            Write-Verbose ("Checking '{0}'" -f $MapDrive)

            If ($NetUseStatus.ContainsKey($MapDrive) -and (Test-Path $MapDrive)) 
            { 
                $DriveMapped[$IpAddress] = $true 
                Write-Verbose ("{0} drive mapped" -f $MapDrive)
            }
            Else
            { 
                $DriveMapped[$IpAddress] = $false                
                $PsCommand =  ("\\{0} -accepteula -u {1} -p {2} net start /Y Server " -f $TargetIpList[$IpAddress],$UserName,$Password)
                Write-Verbose ("Running psexec command '{0}'" -f $PsCommand )
                $MyProcess[$IpAddress], $MyProcessOut[$IpAddress], $MyProcessErr[$IpAddress] =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand
            }
        }
        #-------------------------------------------------------
        # Wait for net start 
        #-------------------------------------------------------
        For ($Timeout=0;$TimeOut -lt 360; $Timeout++)
        {
            $AllExited = $true

            For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
            {
                If (-NOT $DriveMapped[$IpAddress] -and  $MyProcess[$IpAddress].HasExited -eq $false) { $AllExited = $false }
            }
            If ($AllExited) {break}
            
            Start-Sleep -Milliseconds 500
        }

        #-------------------------------------------------------
        # Check that net start finished
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If (-NOT $DriveMapped[$IpAddress])
            {
                $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess[$IpAddress]   $MyProcessOut[$IpAddress]   $MyProcessErr[$IpAddress]  0  -IgnoreExitCode   

                # Exit code of 2 means service already running so that is also OK

                If ((0 -ne $ExitCode) -and (2 -ne $ExitCode))   
                { 
                    $ErrorCount++
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Failed to start net server`r" -f $TargetIpList[$IpAddress]) 
                }
                Else                   
                { 
                    Write-Verbose ("`t`t[{0}] started net server" -f $TargetIpList[$IpAddress]) 
                }
            }
        }

        #-------------------------------------------------------
        # Map the drive with net use
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If (-NOT $DriveMapped[$IpAddress])
            {
                $MyProcess[$IpAddress], $MyProcessOut[$IpAddress], $MyProcessErr[$IpAddress] =   BaseLib_StartProcess "net.exe" ("use \\{0}\$RemoteDrive`$ {1} /USER:{2} /PERSISTENT:no" -f $TargetIpList[$IpAddress],$Password,$UserName) 
                
                $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess[$IpAddress]   $MyProcessOut[$IpAddress]   $MyProcessErr[$IpAddress]  -IgnoreExitCode

                If (0 -ne $ExitCode) 
                { 
                    $ErrorCount++
                     Write-Host -ForegroundColor Red   ("`t`t[{0}] Could not copy files. Failed to map drive with net use `r" -f $TargetIpList[$IpAddress]) 
                     $SkipRemove[$IpAddress] = $true
                }
                Else                   
                { 
                    Write-Verbose ("`t`t[{0}] started net use" -f $TargetIpList[$IpAddress]) 
                }
            }
        }
#>
        #-------------------------------------------------------
        # Remove the files
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If (-not $SkipRemove[$IpAddress])
            {
                Write-Verbose ("Deleting \\{0}\$RemoteDrive`$$RemoteFile" -f $TargetIpList[$IpAddress])
                Remove-Item  ("\\{0}\$RemoteDrive`$$RemoteFile" -f $TargetIpList[$IpAddress])  -Recurse -Force  -ErrorAction SilentlyContinue | Out-Null
           }
        }

        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Ping-WcsSystem
#-------------------------------------------------------------------------------------
Function Ping-WcsSystem()
{  
<#
  .SYNOPSIS
   Pings one or more remote targets (systems)

  .DESCRIPTION
   Pings one or more remote targets (systems) and returns the number of systems that do not reply
   to the ping

   TargetList is a list of IP addresses and/or hostnames

  .EXAMPLE
   $UnresponsiveSystems = Ping-WcsSystem -TargetList $ListOfIpAddresses    

   Pings each system in $ListOfIpAddresses then stores the number of systems that did not 
   respond to the ping in the $UnresponsiveSystems variable

  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')

  .OUTPUTS
   Returns number of systems that did not reply to the ping.  Returns 0 if all systems
   replied to the ping.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote
#>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support  

    Param
    ( 
        [Parameter(Mandatory=$true)] [array] $TargetList
    )

    Try
    {  
        $ErrorCount = 0
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
                     
        #-------------------------------------------------------
        # Get the IPs from the target list
        #-------------------------------------------------------              
        $TargetIpList = [array] (GetTargets $TargetList) 

        $MyProcess    = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessErr = New-Object 'System.Object[]'   $TargetIpList.Count 
        $MyProcessOut = New-Object 'System.Object[]'   $TargetIpList.Count 

        #-------------------------------------------------------
        # Start to ping them all
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $MyProcess[$IpAddress], $MyProcessOut[$IpAddress], $MyProcessErr[$IpAddress] =   BaseLib_StartProcess "ping" ("{0} -n 3 " -f $TargetIpList[$IpAddress]) 
        }
 
        #-------------------------------------------------------
        # Get the ping results
        #-------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess[$IpAddress]   $MyProcessOut[$IpAddress]   $MyProcessErr[$IpAddress]  -IgnoreExitCode

            If ((0 -ne $ExitCode) -or (-not $Output.Contains("Lost = 0")))
            { 
                $ErrorCount++
                If  ($ErrorActionPreference -ne 'SilentlyContinue') 
                { 
                    Write-Host -ForegroundColor Red   ("`t`t[{0}] Could not ping `r" -f $TargetIpList[$IpAddress]) 
                }
            }
            Else                   
            { 
                Write-Verbose ("`t`t[{0}] ping successful" -f $TargetIpList[$IpAddress]) 
            }            
        }
        Return $ErrorCount
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#----------------------------------------------------------------------------------------------
# Get-Subnet 
#----------------------------------------------------------------------------------------------
Function Get-Subnet() {

   <#
  .SYNOPSIS
   Gets dynamic IP and MAC addresses on a subnet

  .DESCRIPTION
   Gets dynamic IP and MAC addresses on a subnet. If subnet not specified then
   uses 192.168.xxx.xxx

   Returns a hash table with IP addresses as value and MAC as key and another
   hash table with MAC addresses as value and IP as key

  .EXAMPLE
   $IpAddressByMac,$MacAddressByIp = Get-Subnet

   Reads the current server configuration and stores in $IpAddressByMac,$MacAddressByIp

   $IpAddressByMac[$MacAddress] returns the IP associated with $MacAddress
   $MacAddressByIP[$IPAddress] returns the MAC associated with $IpAddress

  .PARAMETER Subnet
   Subnet to get

  .OUTPUTS
   Two hash tables, one with MAC addresses and other with IP addresses

   #>

    [CmdletBinding()]

    Param
    ( 
        [string] $Subnet       = "192.168" 
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #--------------------------------------------------------
        # Get the interface to search based on specified subnet
        #--------------------------------------------------------
        $NicInterface = $null

        (arp -a) | Where-Object { $_.Contains("Interface:") } | ForEach-Object {
                         
            Write-Verbose "Found NIC interface $_`r" 

            if ( ($_.split()[1]).StartsWith($Subnet))
            {
                if ($null -ne $NicInterface)
                {
                    Throw ("Host PC has more than one private network {0} {1}" -f $NicInterface, ($_.split()[1]))
                }
                $NicInterface = ($_.split()[1])               
            }
        }
        if ($null -eq $NicInterface)
        {
            Throw "Did not find NIC interface"
        }

        Write-Verbose ("Using NIC interface: $NicInterface`r")
        #-------------------------------------------------------
        # Get the dynamic IP and MAC addresses on the interface
        #-------------------------------------------------------
        $SubnetIpAddresses  = @{}
        $SubnetMacAddresses = @{}

        (arp -a -N $NicInterface) | Where-Object { $_ -ne $null } | ForEach-Object {
        
            $InputLine = ($_ -split '\s+')

            If (($InputLine.Count -ge 4) -and  ($InputLine[3] -eq "dynamic"))
            {
                #---------------------------------------------------------
                # Create hash table where key is the MAC and value is IP
                #---------------------------------------------------------
                $MacAddress = (($_ -split '\s+')[2]).ToString()
                $IpAddress  = (($_ -split '\s+')[1]).ToString()
                $SubnetMacAddresses[$IpAddress]= $MacAddress
                $SubnetIpAddresses[$MacAddress]= $IpAddress

                Write-Verbose ("Adding IP address {0} with MAC {1}`r" -f $IpAddress, $MacAddress)
            }
        }
        Write-Output  $SubnetIpAddresses,$SubnetMacAddresses
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }
    }
}

#----------------------------------------------------------------------------------------------
# Get-WcsChassis
#----------------------------------------------------------------------------------------------
Function Get-WcsChassis()
{
   <#
  .SYNOPSIS
   Returns WCS chassis managers on a network subnet [Internal Evaluation Only]

  .DESCRIPTION
   Searches the specified subnet for all chassis managers.  If not subnet specified then
   searches the default subnet

   Returns one or more chassis manager XML objects.  To view chassis managers on a 
   subnet use View-WcsChassis

  .EXAMPLE
   $AllChassis = Get-WcsChassis

   Stores all chassis managers on default subnet in the variable $AllChassis

  .EXAMPLE
   $ChassisManagers = Get-WcsChassis -subnet "192.168.200"

   Stores all chassis managers on 192.168.200 subnet into the $ChassisManagers
   variable. 

  .PARAMETER Subnet
   IPV4 subnet to search. For example "192.168.200".  

  .PARAMETER Credential
   Powershell Credentials to use.  If not specified uses the default.

  .PARAMETER TimeoutInMs
   Time to wait for an IP address to respond to REST command

  .PARAMETER Quiet
   If specified then suppresses output.  Useful when running inside another script that
   manages output

  .PARAMETER NoDrive
   If specified then does not map the c: of the chassis manager

  .OUTPUTS
   Array of chassis manager objects

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (
        [Parameter(Mandatory=$false,Position=0)] [string]        $Subnet          = "192.168",
        [Parameter(Mandatory=$false)]                            $Credential      = $Global:ChassisManagerCredential,
        [Parameter(Mandatory=$false)]            [int]           $TimeOutInMs     = 15000,
                                                 [switch]        $Quiet
   )
 
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        $ChassisManagers = @()

        #-----------------------------------------
        # Get the IP addresses
        #-----------------------------------------
        $IpAddressByMac,$MacAddressesByIp  = Get-Subnet -Subnet $Subnet

        If ($null -eq $IpAddressByMac)
        {
            Write-Host -ForegroundColor Red   ("Did not find any IP address on subnet '$Subnet'`r")
            Return $null
        }
        $IpAddresses = $IpAddressByMac.Values
        Write-Verbose  ("Looking for chassis managers on subnet $Subnet`r")

        #---------------------------------------------------------------------------------------------------------------------------
        # For each IP ask if it is chassis manager, suppress error messages since some IP are not chassis managers and will error
        #---------------------------------------------------------------------------------------------------------------------------
        $httpsResponders  = [array] ( Invoke-WcsRest  -Target $IpAddresses  -Command "GetServiceVersion?" -TimeoutInMs $TimeoutInMs -Asynchronous  -SSL  -ErrorAction SilentlyContinue )
        $httpResponders   = [array] ( Invoke-WcsRest  -Target $IpAddresses  -Command "GetServiceVersion?" -TimeoutInMs 5000         -Asynchronous        -ErrorAction SilentlyContinue )
        #-----------------------------------------------------------------------
        # Check which IP addresses responded, add those that responded to list
        #----------------------------------------------------------------------
        $Index = 0

        $IpAddresses | Where-Object { $_ -ne $null } |  ForEach-Object {
             
            if (($null -ne $httpResponders) -and ($null -ne $httpResponders[$Index]))
            {

# Add check for valid response

                $ChassisManagers     += $WCS_CHASSISMANAGER_OBJECT.Clone()  # Clone hashtables in powershell so copy values not ref
                $CurrentChassis       =  $ChassisManagers.Count - 1

                $ChassisManagers[$CurrentChassis].IP             = $_
                $ChassisManagers[$CurrentChassis].ActiveMAC      = $MacAddressesByIp[$_]
                $ChassisManagers[$CurrentChassis].Service        = $httpResponders[$Index].ServiceVersionResponse.ServiceVersion
                $ChassisManagers[$CurrentChassis].SSL            = $false
                
                Write-Verbose  ("Found chassis manager at {0}  MAC {1} Version {2} `r" -f $_,$MacAddressesByIp[$_],$httpResponders[$Index].ServiceVersionResponse.ServiceVersion)
            }

            if (($null -ne $httpsResponders) -and ($null -ne $httpsResponders[$Index]))
            {
                $ChassisManagers     += $WCS_CHASSISMANAGER_OBJECT.Clone()  # Clone hashtables in powershell so copy values not ref
                $CurrentChassis       =  $ChassisManagers.Count - 1
# Add check for valid response

                $ChassisManagers[$CurrentChassis].IP             = $_
                $ChassisManagers[$CurrentChassis].ActiveMAC      = $MacAddressesByIp[$_]
                $ChassisManagers[$CurrentChassis].Service        = $httpsResponders[$Index].ServiceVersionResponse.ServiceVersion
                $ChassisManagers[$CurrentChassis].SSL            = $true

                Write-Verbose  ("Found chassis manager at {0} (https) MAC {1} Version {2}`r" -f $_,$MacAddressesByIp[$_],$httpsResponders[$Index].ServiceVersionResponse.ServiceVersion)
            }
            $Index++
        }        
        #----------------------------------------------------------------------
        # If not chassis managers found then return
        #----------------------------------------------------------------------
        If ($null -eq $ChassisManagers) 
        {
            Write-Host -ForegroundColor Red   ("Did not find any chassis managers on subnet '$Subnet'`r")
            Return $null
        }

        #----------------------------------------------------------------------
        # Get additional chassis information 
        #----------------------------------------------------------------------
        $ChassisInfoResponse   = [array] (Invoke-WcsRest -Target $ChassisManagers -Command "GetChassisInfo" -Async)

        If ($null -eq $ChassisInfoResponse) 
        {
            Write-Host -ForegroundColor Red   ("Failed to get ChassisInfo`r")
            Return $null
        }

        $ChassisHealthResponse = [array] (Invoke-WcsRest -Target $ChassisManagers -Command "GetChassisHealth" -Async )

        If ($null -eq $ChassisHealthResponse) 
        {
            Write-Host -ForegroundColor Red   ("Failed to get ChassisHealth`r")
            Return $null
        }


        $Chassis = 0
        $ChassisManagersIP = @()
        $ChassisManagers | Where { $_ -ne $null} | ForEach-Object { $ChassisManagersIP += $_.IP }

        $ChassisManagersIP | Where-Object { $_ -ne  $null }  | ForEach-Object {

            $ChassisManagers[$Chassis].Info     =  $ChassisInfoResponse[$Chassis] 
            $ChassisManagers[$Chassis].Health   =  $ChassisHealthResponse[$Chassis] 

 # check for null response here too

            $ChassisManagers[$Chassis].AssetTag =  $ChassisManagers[$Chassis].Info.ChassisInfoResponse.ChassisController.AssetTag   
            $ChassisManagers[$Chassis].MAC1     =  ($ChassisManagers[$Chassis].Info.ChassisInfoResponse.ChassisController.NetworkProperties.ChassisNetworkPropertyCollection.ChassisNetworkProperty[0].MacAddress).Replace(':','-').ToLower()
            $ChassisManagers[$Chassis].MAC2     =  ($ChassisManagers[$Chassis].Info.ChassisInfoResponse.ChassisController.NetworkProperties.ChassisNetworkPropertyCollection.ChassisNetworkProperty[1].MacAddress ).Replace(':','-').ToLower()

            If ($ChassisManagers[$Chassis].MAC1  -eq  $ChassisManagers[$Chassis].ActiveMAC)
            {
                $ChassisManagers[$Chassis].HostName = $ChassisManagers[$Chassis].Info.ChassisInfoResponse.ChassisController.NetworkProperties.ChassisNetworkPropertyCollection.ChassisNetworkProperty[0].dnsHostName
            }
            ElseIf ($ChassisManagers[$Chassis].MAC2 -eq  $ChassisManagers[$Chassis].ActiveMAC)
            {
                $ChassisManagers[$Chassis].HostName = $ChassisManagers[$Chassis].Info.ChassisInfoResponse.ChassisController.NetworkProperties.ChassisNetworkPropertyCollection.ChassisNetworkProperty[1].dnsHostName
            }
            Else
            {
                $ChassisManagers[$Chassis].Error = "ERROR: Could not match MACs. "
            }
 
            $UserName =  $ChassisManagerCredential.UserName
            $Password =  $ChassisManagerCredential.GetNetworkCredential().Password   

            $MyProcess , $MyProcessOut , $MyProcessErr  =   BaseLib_StartProcess "net.exe" ("use \\{0}\c`$ /DELETE /Yes" -f $_ )        
            $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess  $MyProcessOut  $MyProcessErr -IgnoreExitCode

            $MyProcess , $MyProcessOut , $MyProcessErr  =   BaseLib_StartProcess "net.exe" ("use \\{0}\c`$ {1} /USER:{2} /PERSISTENT:no" -f $_ ,$Password,$UserName)        
            $ExitCode,$Output = BaseLib_GetProcessOutput $MyProcess  $MyProcessOut  $MyProcessErr -IgnoreExitCode -TimeoutInMs 60000

            If (0 -eq $ExitCode) 
            {
                $ChassisManagers[$Chassis].Drive   =  ("\\{0}\c`$" -f $_)
            }
            Else
            {
                $ChassisManagers[$Chassis].Error += "ERROR: Could not map drive. "
            }

            $Chassis++
        }

        If (-NOT $Quiet) { View-WcsChassis -ChassisManagers $ChassisManagers | Out-Null }


        Return ([array] $ChassisManagers) 
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }
    }
}

#----------------------------------------------------------------------------------------------
# Get-WcsBlade
#----------------------------------------------------------------------------------------------
Function Get-WcsBlade()
{
   <#
  .SYNOPSIS
   Returns all WCS blades connected to one ore more chassis [Internal Evaluation Only] 

  .DESCRIPTION
   Returns the blades connected to the specified chassis. If no chassis specified searches
   the default subnet for all chassis and then returns the blades connected to them,

  .EXAMPLE
   Get-WcsBlade

   Returns all chassis managers on default subnet in XML object

  .EXAMPLE
   Get-WcsBlade -Chassis $CM0

   Returns all blades connected to $CM0

  .PARAMETER ChassisManager
   One or more chassis manager objects

  .PARAMETER SubnetIpAddresses
   Hash table with IP and MAC addresses for a subnet returned by the Get-Subnet command

  .OUTPUTS
   Array of blade objects

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support
    Param
    (                
        [Parameter(Mandatory=$false,Position=0)] [array]         $ChassisManagers = $null,
        [Parameter(Mandatory=$false,Position=1)] [string]        $Subnet          = "192.168",
        [Parameter(Mandatory=$false)]                            $Credential      = $Global:BladeCredential,
                                                 [switch]        $Quiet,
                                                 [switch]        $Full
    )
 
    Try
    {
        $Blades = @()
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
 

        $SubnetIPAddresses,$SubnetMacAddresses = Get-Subnet $Subnet
        If ($null -eq $SubnetIPAddresses)
        {
            Write-Host -ForegroundColor Red   ("No IP addresses found`r")
            Return @()
        }

        #-----------------------------------------
        # If not given chassis managers find them
        #-----------------------------------------
        If ($null -eq $ChassisManagers)
        {
            $ChassisManagers = [array] ( Get-WcsChassis -Quiet -Subnet $Subnet)
            If ($null -eq $ChassisManagers)        
            {           
                Write-Host -ForegroundColor Red   ("No chassis managers found`r")
                Return @()
            }
        }      

        Write-Host (" Found {0} chassis managers`r" -f $ChassisManagers.Count)
        #-----------------------------------------------------
        # For each slot on each chassis check for a blade
        #-----------------------------------------------------
        $IP    = New-Object 'object[,]'   $ChassisManagers.Count,$WCS_BLADES_PER_CHASSIS

        $TotalBladeIndex = 0

        For ($Chassis = 0; $Chassis -lt $ChassisManagers.Count; $Chassis++)
        {
            If ($ChassisManagers[$Chassis].WcsObject -ne $WCS_TYPE_CHASSIS)
            {
                Write-Host -ForegroundColor Red  "Illegal chassis type`r"
                Return @()
            }


            For ($BladeSlot=1;$BladeSlot -le $WCS_BLADES_PER_CHASSIS; $BladeSlot++)
            {    
                
                $BladeIP    = $null
                $BladeIndex = $BladeSlot - 1 #Slots count from 1 but arrays count from 0

                $BladeId    = $ChassisManagers[$Chassis].Info.ChassisInfoResponse.BladeCollections.BladeInfo[$BladeIndex].BladeNumber
                $BladeState = $ChassisManagers[$Chassis].Health.ChassisHealthResponse.BladeShellCollection.BladeShellResponse[ ($BladeId - 1 )].BladeState
                        
                $IP[$Chassis,$BladeIndex] = $WCS_NOT_AVAILABLE
                
                If ($Full)
                {
                    $Blades                                 += $WCS_BLADE_OBJECT.Clone()
                    $TotalBladeIndex                         = $Blades.Count - 1

                    $Blades[$TotalBladeIndex].Slot           = $BladeId
                    $Blades[$TotalBladeIndex].ChassisMac     = $ChassisManagers[$Chassis].ActiveMAC
                    $Blades[$TotalBladeIndex].State          = $BladeState
                    $Blades[$TotalBladeIndex].ChassisId      = ("{0} at  IP {1}   {3}" -f $ChassisManagers[$Chassis].Hostname,$ChassisManagers[$Chassis].IP, $ChassisManagers[$Chassis].ActiveMAC,$ChassisManagers[$Chassis].Error)
                }

                If ("Healthy" -eq  $BladeState) {   

                        $MAC = ($ChassisManagers[$Chassis].Info.ChassisInfoResponse.BladeCollections.BladeInfo[$BladeIndex].BladeMacAddress.NicInfo[0].MacAddress).Replace(":","-").ToLower()
                        $BladeIp = $SubnetIPAddresses[$MAC]

                        if ($null -eq $BladeIp)
                        {
                            $MAC = ($ChassisManagers[$Chassis].Info.ChassisInfoResponse.BladeCollections.BladeInfo[$BladeIndex].BladeMacAddress.NicInfo[1].MacAddress).Replace(":","-").ToLower()

                            $BladeIp = $SubnetIPAddresses[$MAC.Replace(":","-").ToLower()]
                        }

                        if ($null -ne $BladeIp)
                        {
                            if ($null -ne (Test-Connection $BladeIP -count 1 -ErrorAction SilentlyContinue))
                            {
                                If (-NOT $Full)
                                {
                                    $Blades                                 += $WCS_BLADE_OBJECT.Clone()
                                    $TotalBladeIndex                         = $Blades.Count - 1
                                    $Blades[$TotalBladeIndex].Slot           = $BladeId
                                    $Blades[$TotalBladeIndex].ChassisMac     = $ChassisManagers[$Chassis].ActiveMAC
                                    $Blades[$TotalBladeIndex].State          = $BladeState
                                    $Blades[$TotalBladeIndex].ChassisId      =  ("{0} at  IP {1}   {3}" -f $ChassisManagers[$Chassis].Hostname,$ChassisManagers[$Chassis].IP, $ChassisManagers[$Chassis].ActiveMAC,$ChassisManagers[$Chassis].Error)

 
                                }

                                $Blades[$TotalBladeIndex].IP             = $BladeIP
                                $Blades[$TotalBladeIndex].MAC            = $MAC
                                $IP[$Chassis,$BladeIndex] = $BladeIP
                            }
                        }
                        


                }

            }
        }     
        #-----------------------------------------
        # Restore the previous display mode
        #-----------------------------------------
        If (-NOT $Quiet) { View-WcsBlade $Blades}

        Write-Output ([array] $Blades)

    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }

        Write-Output @()
    }
 
}
#----------------------------------------------------------------------------------------------
# View-WcsBlade
#----------------------------------------------------------------------------------------------
Function View-WcsBlade()
{
   <#
  .SYNOPSIS
   Displays blades connected to one or more chassis [Internal Evaluation Only]

  .DESCRIPTION
   Displays information on blades connected to the chassis manager specified.  If no
   chassis manager specified searches subnet for all chassis managers and then all
   blades connected to each chassis.

  .EXAMPLE
   View-WcsBlade

   Displays all WcsBlade connected to all chassis managers on default subnet  

  .EXAMPLE
   View-WcsBlade -Chassis $MyChassis

   Displays all blades connected to chassis manager defined in $MyChassis variable

  .PARAMETER ChassisManagers
   Array of chassis manager xml objects returned by the Get-WcsChassis command

  .PARAMETER Subnet
   IPV4 subnet to search if no chassis managers specified

   #>
    
    [CmdletBinding()]

    Param
    (
        [Parameter(Mandatory=$false,Position=0)] [array]         $Blades          = $null,
        [Parameter(Mandatory=$false)]            [array]         $ChassisManagers = $null,
        [Parameter(Mandatory=$false)]            [string]        $Subnet          = "192.168",
        [Parameter(Mandatory=$false)]                            $BladeCredential = $Global:BladeCredential,
        [Parameter(Mandatory=$false)]                            $Credential      = $Global:ChassisManagerCredential,
                                                 [switch]        $Full
    )
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #---------------------------------------------------------
        # If blades provided use them and ignore CM and subnet
        #---------------------------------------------------------
        If ($null -eq $Blades)
        {
            #-----------------------------------------
            # If CM not specified find them
            #-----------------------------------------
            If ($null -eq $ChassisManagers)
            {
                Write-Host ("`tFinding chassis managers on {0}`r" -f $Subnet) 
                [array] $ChassisManagers =  (Get-WcsChassis -Quiet -Subnet $Subnet -Credential $Credential)
                If ($null -eq $ChassisManagers)
                {
                    Write-Host "`t`tNo chassis managers found`r"
                    Return
                }
            }
            Else
            {
                For ($Chassis = 0; $Chassis -lt $ChassisManagers.Count; $Chassis++) 
                {
                    If ($ChassisManagers[$Chassis].WcsObject -ne $WCS_TYPE_CHASSIS)
                    {
                        Throw "Illegal chassis type found"                      
                    }
                }
            }

            Write-Host ("`tFinding blades on {0} chassis managers`r" -f $ChassisManagers.Count) 
            #-----------------------------------------
            # Get all blades on all chassis
            #-----------------------------------------
            [array] $Blades = Get-WcsBlade -ChassisManager $ChassisManagers -Quiet -Full:$full -Credential $Global:BladeCredential
        }
        #-----------------------------------------
        # Display all blades on all chassis
        #-----------------------------------------
        $CurrentChassis = " "
        $CurrentChassisCount = 0

        If ($null -eq $Blades) { return $null}

        For ($Blade = 0; $Blade -lt $Blades.Count; $Blade++)
        {    

            If ($Blades[$Blade].WcsObject -ne $WCS_TYPE_BLADE)
            {
                Write-Host -ForegroundColor Red  "Illegal blade type`r"
                Return $null
            }
            If ($Blades[$Blade].ChassisId -ne $CurrentChassis)
            {
                $CurrentChassis  =$Blades[$Blade].ChassisId 
                If ($CurrentChassis.Contains("ERROR"))
                {
                    Write-Host -ForegroundColor Red  "`r`n`tCM[$CurrentChassisCount]  $CurrentChassis  `r`n`r"
                }
                Else
                {
                    Write-Host "`r`n`tCM[$CurrentChassisCount]  $CurrentChassis  `r`n`r"
                }
                $CurrentChassisCount++
            }        
            Write-Host ("`t`tBlade[{0,3}]  {1,-16}  Slot: {2,2}    IP: {3,-18} MAC: {4,-18}    State: {5,-15}   Model: {6}`r" -f $Blade,$Blades[$Blade].Hostname,$Blades[$Blade].Slot, $Blades[$Blade].IP,$Blades[$Blade].MAC,$Blades[$Blade].State,$Blades[$Blade].Type ) 
        }
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }
    }
}

#----------------------------------------------------------------------------------------------
# View-WcsChassis
#----------------------------------------------------------------------------------------------
Function View-WcsChassis()
{
   <#
  .SYNOPSIS
   Displays chassis manager information [Internal Evaluation Only]

  .DESCRIPTION
   Displays chassis manager information for the chassis specified.  If no
   chassis manager specified searches subnet for all chassis managers.

   Searches for all chassis manager on 192.168 by default

   Uses ARP table to find list of CM to try

  .EXAMPLE
   View-WcsChassis

   Displays all chassis managers on default subnet  

  .EXAMPLE
   View-WcsChassis -subnet "192.168.200"

   Displays all chassis managers on 192.168.200 subnet 

  .EXAMPLE
   View-WcsChassis $Chassis0

   Displays chassis managers in the $Chassis0 variable

  .PARAMETER ChassisManagers
   Array of chassis manager xml objects returned by the Get-WcsChassis command

  .PARAMETER Subnet
   IPV4 subnet to search if no chassis managers specified

   #>
      
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (

        [Parameter(Mandatory=$false,Position=0)] [array]         $ChassisManagers = $null,
        [Parameter(Mandatory=$false,Position=1)] [string]        $Subnet          = "192.168",
        [Parameter(Mandatory=$false)]                            $Credential      = $Global:ChassisManagerCredential
    )
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-----------------------------------------
        # If CM not specified find them
        #-----------------------------------------
        If ($null -eq $ChassisManagers)
        {
            [array] $ChassisManagers = Get-WcsChassis -Quiet -Subnet $Subnet -Credential  $Credential  
            If ($null -eq $ChassisManagers)
            {
                Write-Host "`n`tNo chassis managers found`r"
                Return 
            }
        }
        #-----------------------------------------
        # Display all chassis
        #-----------------------------------------
        Write-Host " `r"

        For ($Chassis = 0; $Chassis -lt $ChassisManagers.Count; $Chassis++)
        {       
            If ($ChassisManagers[$Chassis].WcsObject -ne $WCS_TYPE_CHASSIS)
            {
                Throw "Illegal chassis type found"
            }       
            If ($null -eq $ChassisManagers[$Chassis].Error)
            {
                Write-Host ("`tCM[$Chassis]  {5} at IP: {1,-17} MAC: {2,-18}   Service: {3}   Asset: {4}`r" -f $ChassisManagers[$Chassis].Position, $ChassisManagers[$Chassis].IP,$ChassisManagers[$Chassis].ActiveMAC,$ChassisManagers[$Chassis].Service,$ChassisManagers[$Chassis].AssetTag,$ChassisManagers[$Chassis].Hostname)
            }
            Else
            {
                Write-Host -ForegroundColor Red  ("`tCM[$Chassis]  {6} at IP: {1,-17} MAC: {2,-18}   Service: {3}   Asset: {4}  {5}`r" -f $ChassisManagers[$Chassis].Position, $ChassisManagers[$Chassis].IP,$ChassisManagers[$Chassis].ActiveMAC,$ChassisManagers[$Chassis].Service,$ChassisManagers[$Chassis].AssetTag,$ChassisManagers[$Chassis].Error,$ChassisManagers[$Chassis].Hostname)
            }
        }
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red  $_.ErrorDetails }
    }
}
#-------------------------------------------------------------------------------------
# Reboot-WcsChassis
#-------------------------------------------------------------------------------------
Function Reboot-WcsChassis()
{
   <#
  .SYNOPSIS
   Sends reboot command to one or more WCS chassis

  .DESCRIPTION
   Sends reboot command to one or more WCS chassis

  .EXAMPLE
   Reboot-WcsChassis -TargetList $ChassisManagerIp

  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

   #>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true,Position=0)] $TargetList 
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get targets and credentials
        #-------------------------------------------------------
        $TargetIpList = [array] (GetTargets $TargetList)

        $UserName =  $ChassisManagerCredential.UserName
        $Password =  $ChassisManagerCredential.GetNetworkCredential().Password  
        #------------------------------------------------------------------------
        # For each target reboot, don't wait for response because of the reboot
        #------------------------------------------------------------------------             
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            $PsCommand =  ("\\{0} -accepteula -u $UserName  -p $Password shutdown /r /t 5" -f $TargetIpList[$IpAddress])
            
            Write-Verbose ("Running psexec command '{0}'`r" -f $PsCommand )
            $MyProcess,$MyProcessOut,$MyProcessErr =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand 
        }
        Return $WCS_RETURN_CODE_SUCCESS
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Reboot-WcsBlade
#-------------------------------------------------------------------------------------
Function Reboot-WcsBlade()
{
   <#
  .SYNOPSIS
   Sends reboot command to one or more WCS blades

  .DESCRIPTION
   Sends reboot command to one or more WCS blades

   Uses default blade credentials. Use Set-WcsBladeCredential to change credentials

   The command may return before the blade starts the reboot.  Wait at least
   120 seconds for the  reboot to start after the command returns.

  .EXAMPLE
   Reboot-WcsBlade -TargetList $BladeIp -WinPE

   Sends WinPE reboot command (wpeutil reboot) to all systems listed in variable $BladeIp

  .EXAMPLE
   Reboot-WcsBlade -TargetList @('system001','system002') 

   Sends Windows reboot (shutdown /r /t 5) command to hostnames system001 and system002
   
  .EXAMPLE
   Reboot-WcsBlade -TargetList $BladeIp

  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')
  
  .PARAMETER WinPE
   If specified sends the WinPE reboot command instead of the Windows shutdown command

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

   #>

    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true,Position=0)]           $TargetList, 
                                                 [switch] $WinPE 
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get targets and credentials
        #-------------------------------------------------------
        $TargetIpList = [array] (GetTargets $TargetList)

        $UserName =  $BladeCredential.UserName
        $Password =  $BladeCredential.GetNetworkCredential().Password  

        #------------------------------------------------------------------------
        # For each target reboot, don't wait for response because of the reboot
        #------------------------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If ($WinPE)
            {
                $PsCommand =  ("\\{0} -accepteula -u $UserName  -p $Password Wpeutil reboot" -f $TargetIpList[$IpAddress])
            }
            Else
            {
                $PsCommand =  ("\\{0} -accepteula -u $UserName  -p $Password shutdown /r /t 5" -f $TargetIpList[$IpAddress])
            }

            Write-Verbose ("Running psexec command '{0}'`r" -f $PsCommand )
            $MyProcess,$MyProcessOut,$MyProcessErr =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand 
        }
        Return $WCS_RETURN_CODE_SUCCESS
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
#-------------------------------------------------------------------------------------
# Shutdown-WcsBlade
#-------------------------------------------------------------------------------------
Function Shutdown-WcsBlade()
{
   <#
  .SYNOPSIS
   Sends shutdown command to one or more remote targets (blade systems)

  .DESCRIPTION
   Sends shutdown command to one or more targets (blade systems)

   Uses default blade credentials. Use Set-WcsBladeCredential to change credentials

   The command may return before the blade completes the shutdown.  Wait at least
   120 seconds for shutdown to occur after the command returns.

  .EXAMPLE
   Shutdown-WcsBlade -TargetList $BladeIp -WinPE

   Sends WinPE shutdown command (winpue shutdown) to all systems listed in variable $BladeIp

  .EXAMPLE
   Shutdown-WcsBlade -TargetList @('system001','system002') 

   Sends Windows shutdown (shutdown /s /t 5) command to hostnames system001 and system002
   
  .PARAMETER TargetList
   List of remote targets.  Targets can be specified with an IP address or hostname. 
   Examples:

   192.168.200.10
   @('192.168.200.10', '192.168.200.11')
   @('host1','host2')
  
  .PARAMETER WinPE
   If specified sends the WinPE shutdown command instead of the Windows shutdown command

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .COMPONENT
   WCS

  .FUNCTIONALITY
   Remote

   #>

    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [Parameter(Mandatory=$true,Position=0)]           $TargetList, 
                                                 [switch] $WinPE 
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get targets and credentials
        #-------------------------------------------------------
        $TargetIpList = [array] (GetTargets $TargetList)

        $UserName =  $BladeCredential.UserName
        $Password =  $BladeCredential.GetNetworkCredential().Password  
        #------------------------------------------------------------------------
        # For each target shutdown, don't wait for response because of the shutdown
        #------------------------------------------------------------------------
        For ($IpAddress=0;$IpAddress -lt $TargetIpList.Count; $IpAddress++)
        {
            If ($WinPE)
            {
                $PsCommand =  ("\\{0} -accepteula -u $UserName  -p $Password Wpeutil shutdown" -f $TargetIpList[$IpAddress])
            }
            Else
            {
                $PsCommand =  ("\\{0} -accepteula -u $UserName  -p $Password shutdown /s /t 5" -f $TargetIpList[$IpAddress])
            }

            Write-Verbose ("Running psexec command '{0}'`r" -f $PsCommand )
            $MyProcess,$MyProcessOut,$MyProcessErr =   BaseLib_StartProcess $WCS_PSEXEC64_BINARY $PsCommand 
        }
        Return $WCS_RETURN_CODE_SUCCESS
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
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
