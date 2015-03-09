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

#==================================================================================
#  This is an example file showing how to call other update files to update the CM
#  programmables.  You must add your own update scripts and utilities for your 
#  system.
#==================================================================================

#--------------------------------------------------------------------------------------------------------------------------
# Updates Chassis Manager service
#--------------------------------------------------------------------------------------------------------------------------
$Invocation                =  (Get-Variable MyInvocation -Scope 0).Value
$ThisScriptDirectory       =  (Split-Path $Invocation.MyCommand.Path).ToLower()
$ThisScriptDirectory       =   Split-Path $ThisScriptDirectory  -NoQualifier

$RootUpdateDirectory       = '\updates'
#--------------------------------------------------------------------------------------------------------------------------
# If libraries have not been loaded then load them
#--------------------------------------------------------------------------------------------------------------------------
If ($WCS_UPDATE_ACTION -eq $null)
{
    If ($ThisScriptDirectory.LastIndexOf($RootUpdateDirectory) -ne -1) 
    { 
       $BasePath = "{0}" -f $ThisScriptDirectory.Substring(0,$ThisScriptDirectory.LastIndexOf($RootUpdateDirectory))  

        .  "$BasePath\wcsScripts.ps1"
    }
    Else
    {
       Write-Host "Did not find the root update directory '$RootUpdateDirectory'"
       Return $WCS_RETURN_CODE_UNKNOWN_ERROR
    }
}
#----------------------------------------------------------------------
# Constants specific to this update script
#----------------------------------------------------------------------
$UPDATE_NAME             = 'Chassis Manager'
 
#----------------------------------------------------------------------
# Start a transcript locally.  If one already started then will fail
# and continue to use the original
#----------------------------------------------------------------------
Try
{
   Start-Transcript -Append -Path ".\UpdateTranscript.log" | Out-Null
}
Catch
{
    #NOP:  If fail then a transcript already active from calling function
}
 
#----------------------------------------------------------------------
# Main script
#----------------------------------------------------------------------
Try
{
    $RebootRequired     = $false       # Used to track if any action required at end of update
    $PowerCycleRequired = $false       # Ditto

    #---------------------------------------------------- 
    # Verify a Quanta Chassis Manager (BIOS T6MC*)
    #---------------------------------------------------- 
    $BiosInfo = Get-WmiObject Win32_BIOS

    If (-NOT $BiosInfo.SMBIOSBIOSVersion.StartsWith("T6MC")) 
    { 
        Write-Host -ForegroundColor Red  "`tCannot update service because system is not a WCS Chassis Manager`r`n"
        Return $WCS_RETURN_CODE_NOP
    }
    #---------------------------------------------------- 
    # Display update information for user
    #---------------------------------------------------- 
    Write-Host  ("`r`n$WCS_HEADER_LINE `r`n $UPDATE_NAME update started `r`n$WCS_HEADER_LINE `r"-f (Get-Date)) 

    #----------------------------------------------------------------------
    # Update the Service
    #----------------------------------------------------------------------
    $ReturnCode = & "$ThisScriptDirectory\Service\$WCS_UPDATE_SCRIPTFILE"  
    #----------------------------------------------------------------------
    # Service must return SUCCESS error code since not reboot or power cycle required
    #----------------------------------------------------------------------
    If ($ReturnCode -ne 0)           
    { 
        Write-Host -ForegroundColor Red  ("$WCS_HEADER_LINE `r`n $UPDATE_NAME UPDATE DID NOT COMPLETE: Service Update returned code {0} `r`n$WCS_HEADER_LINE `r"-f $ReturnCode)
        Return $ReturnCode 
    }
    #----------------------------------------------------------------------
    # Update the environment variable and take action if specified
    #----------------------------------------------------------------------
    Write-Host  ("`r`n$WCS_HEADER_LINE `r`n $UPDATE_NAME update completed `r`n$WCS_HEADER_LINE`r"-f (Get-Date)) 

    Return $WCS_RETURN_CODE_SUCCESS
}
#----------------------------------------------------------------------
# Catch any unknown errors and return error
#----------------------------------------------------------------------
Catch
{
    Write-Host -ForegroundColor Red "Unknown error - Exiting `r"  
    Write-Host -ForegroundColor Red  $_     
    Return $WCS_RETURN_CODE_UNKNOWN_ERROR
}
