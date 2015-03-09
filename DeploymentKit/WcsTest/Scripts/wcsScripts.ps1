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
$Global:WcsTestToolsVersion                  = "2.00.0000"

$Invocation                   = (Get-Variable MyInvocation -Scope 0).Value
$WcsScriptDirectory           =  Split-Path $Invocation.MyCommand.Path  
$WCS_BASE_DIRECTORY           =  Split-Path $WcsScriptDirectory -Parent
$WCS_BASE_DIRECTORY_NO_DRIVE  =  Split-Path $WCS_BASE_DIRECTORY -NoQualifier

#-------------------------------------------------------------------
#  Include all other libraries
#-------------------------------------------------------------------
. "$WCS_BASE_DIRECTORY\Scripts\Library\CoreLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\ConstantDefines.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\CredentialLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\BaseLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\ConfigLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\StressLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\CycleLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\RemoteLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\CommLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\ErrorLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\IpmiLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\TestLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\RaidLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\LsiLibrary.ps1"
. "$WCS_BASE_DIRECTORY\Scripts\Library\MellanoxLibrary.ps1"
#-------------------------------------------------------------------
#  Check running on valid system in valid directory
#-------------------------------------------------------------------
If ($WCS_BASE_DIRECTORY.Contains(' '))
{
    Write-Host -Foregroundcolor Red  "These scripts do not support install directory with a space in the path`r`n`r"
    Return
}

#-------------------------------------------------------------------
#  Load the library specific to this system
#-------------------------------------------------------------------
. "$WCS_BASE_DIRECTORY\Scripts\Library\SystemLookup.ps1"

$Global:ThisSystem = Lookup-WcsSystem -ErrorAction SilentlyContinue

. "$WCS_BASE_DIRECTORY\Scripts\DefinedSystems\${Global:ThisSystem}.ps1"

#---------------------------------------------------------------------------------
#  Start a new Transcript.   Ignore errors because ISE doesn't allow transcript
#  and generates an error.
#---------------------------------------------------------------------------------
try { Stop-Transcript -ErrorAction Stop | Out-Null } Catch {}

try { Start-Transcript -Path ("$WCS_RESULTS_DIRECTORY\Transcript.log") -Append -ErrorAction Stop | Out-Null } Catch {}

#---------------------------------------------------------------------------------
#  Clears error for a fresh start
#---------------------------------------------------------------------------------
$Error.Clear()

#-------------------------------------------------------------------
#  Start in the base directory
#-------------------------------------------------------------------
cd $WCS_BASE_DIRECTORY

#-------------------------------------------------------------------------------------
# This function is run in the Wcs Test Tools WinPE image when the command Start-WcsTools
# is run.  It logs all system information in a directory for later review and then
# displays the system health.  It is primarily for DC Ops teams doing debug
#
# DO NOT CHANGE THIS FUNCTION NAME because called in WinPE image
#
#-------------------------------------------------------------------------------------
Function Start-WcsToolsForWinPeDebug()
{  
    Try
    {
        $AssetId = 'Unknown'

        $LogError = $false
        #-------------------------------------------------------------------
        # Takes a while to save config so display a message
        #------------------------------------------------------------------- 
        cls
        Write-Host "Reading and saving the system configuration, BMC SEL, and error summary...`r"

        #-----------------------------------------------------------------------
        # Get configuration first because need FRU info to make directory name
        #----------------------------------------------------------------------- 
        $CurrentConfig = Get-WcsConfig

        If (($CurrentConfig.WcsConfig.FRU.ProductAsset.Value -ne $WCS_NOT_AVAILABLE) -and ($CurrentConfig.WcsConfig.FRU.ProductAsset.Value -ne ''))
        {
            $AssetId =  $CurrentConfig.WcsConfig.FRU.ProductAsset.Value
        }
        #-----------------------------------------------------------------------
        # Create the log directory
        #----------------------------------------------------------------------- 
        $LogFileDirectory  = ("$WCS_RESULTS_DIRECTORY\Asset_{0}-{1}" -f  $AssetId,(Baselib_SimpleDate))

        New-Item -Path $LogFileDirectory -ItemType Container -ErrorAction Stop | Out-Null

        #---------------------------------------------------------------------------------
        #  Start a new Transcript specific for this debug session
        #---------------------------------------------------------------------------------
        try { Stop-Transcript -ErrorAction Stop | Out-Null } Catch {}

        try { Start-Transcript -Path ("$LogFileDirectory\Transcript.log" -f (BaseLib_SimpleDate)) -ErrorAction Stop | Out-Null } Catch {}

        #-----------------------------------------------------------------------
        # Log the config, SEL, and errors
        #----------------------------------------------------------------------- 
        Log-WcsConfig -Path         $LogFileDirectory -File 'Configuration' -Config $CurrentConfig  | Out-Null
        Log-WcsHealth -LogDirectory $LogFileDirectory -File 'SystemErrors' | Out-Null
        Log-WcsSel    -LogDirectory $LogFileDirectory -File 'BMC'          | Out-Null  # Ignore errors in case BMC not in the system
    }
    Catch
    {
        $LogError = $true
    }

    cls
    Write-Host " WCS Test Tools Version : ${Global:WcsTestToolsVersion}`r"
    Write-Host " Compute Blade Asset    : $AssetId`r"
    Write-Host " Log directory          : $LogFileDirectory`r"
    If ($LogError) 
    {
        Write-Host " Had errors logging files.  Check the Transcript file for details. `r`n`r"
    }
    Else
    {
        Write-Host "`r"
    }

    View-WcsHealth -NoDeviceMgr
}

