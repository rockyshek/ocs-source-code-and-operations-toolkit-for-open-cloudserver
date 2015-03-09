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


#=====================================================================================================
#
# This file contains a function that reads BIOS and FRU information to determine the type of system
# the scripts are running on.  The function returns a string that identifies the system.  This string
# must match the file  in <installdir>\Scripts\DefinedSystem\ that defines the system.
#
# The string must also match the directory name in Scripts\Updates\<recipe>
#
#=====================================================================================================

#-------------------------------------------------------------------
#  Define WCS System types
#-------------------------------------------------------------------
Set-Variable -Name UNKNOWN_SYSTEM           -Value 'UNKNOWN'                 -Option ReadOnly -Force

Set-Variable -Name WCS_SYSTEM_QUANTA_CM1    -Value "CM_GEN1_QUANTA"          -Option ReadOnly -Force
Set-Variable -Name WCS_SYSTEM_QUANTA_CM2    -Value "CM_GEN2_QUANTA"          -Option ReadOnly -Force

Set-Variable -Name WCS_SYSTEM_QUANTA_BLADE1 -Value "WCS_BLADE_GEN1_QUANTA"   -Option ReadOnly -Force

Set-Variable -Name WCS_SYSTEM_QUANTA_BLADE2 -Value "WCS_BLADE_GEN2_QUANTA"   -Option ReadOnly -Force
Set-Variable -Name WCS_SYSTEM_WIWYNN_BLADE2 -Value "WCS_BLADE_GEN2_WIWYNN"   -Option ReadOnly -Force
Set-Variable -Name WCS_SYSTEM_DELL_BLADE2   -Value "WCS_BLADE_GEN2_DELL"     -Option ReadOnly -Force

#-------------------------------------------------------------------
#  Define the default system type as unknown
#-------------------------------------------------------------------
$Global:DefaultSystemType = $UNKNOWN_SYSTEM

#-------------------------------------------------------------------------------------
# Forces the default system type to value specified.  This allows the setting of a 
# system type even if the FRU or BIOS or incorrect.  Note this must be done every
# time a Powershell session is started
#-------------------------------------------------------------------------------------
Function Force-WcsSystem()
{
    [CmdletBinding()]
    Param
    ( 
            [Parameter(Mandatory=$true)]
            [ValidateSet("CM_GEN1_QUANTA","WCS_BLADE_GEN1_QUANTA","WCS_BLADE_GEN2_QUANTA","WCS_BLADE_GEN2_WIWYNN","WCS_BLADE_GEN2_DELL" )] 
            [string] $Type
    )

    #------------------------------------------------------------
    # Set the global default system type to the new system type
    #------------------------------------------------------------
    Try
    {
        $Global:DefaultSystemType = $Type
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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }
    }                
}
#-------------------------------------------------------------------------------------
# Gets the WCS System Type based on BIOS and FRU information
#
# If cannot read the system info returns the default system (Unknown unless changed)
#-------------------------------------------------------------------------------------
Function Lookup-WcsSystem()
{
    [CmdletBinding()]
    Param
    ( 
         [Parameter(Mandatory=$false)] $BiosInfo=$null,
         [Parameter(Mandatory=$false)]  $FruInfo=$null
    )

    $CurrentSystem = $Global:DefaultSystemType   

    Try
    {

        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Read the BIOS info
        #-------------------------------------------------------
        If ($null -eq $BiosInfo) 
        { 
            $BiosInfo = Get-WmiObject Win32_BIOS  
        }

        Write-Verbose ("SystemLookup: BIOS version:{0}`r" -f $BiosInfo.SMBIOSBIOSVersion)
        #---------------------------------------------------- 
        # Quanta Chassis Manager (BIOS T6MC*)
        #---------------------------------------------------- 
        If ($BiosInfo.SMBIOSBIOSVersion.StartsWith("T6MC")) 
        { 
            $CurrentSystem = $WCS_SYSTEM_QUANTA_CM1 
        }
        #---------------------------------------------------- 
        # Quanta WCS Blade (BIOS: T6M_* )
        #---------------------------------------------------- 
        ElseIf ($BiosInfo.SMBIOSBIOSVersion.StartsWith("T6M_"))
        {
            If ($null -eq $FruInfo) 
            { 
                $FruInfo = IpmiLib_GetBmcFru 
            }

            If ($null -eq $FruInfo) 
            {
                Write-Verbose ("SystemLookup: No FRU info found`r")                 
            }    
            Else
            {
                Write-Verbose ("SystemLookup: FRU productname {0}, model {1} '`r" -f $FruInfo.ProductName.Value,$FruInfo.ProductModel.Value)

                If ((($FruInfo.ProductName.Value.Trim() -eq "C1000") -OR (($FruInfo.ProductName.Value.Trim() -eq "C1020"))) -AND ($FruInfo.ProductModel.Value.Trim() -eq "X873095-001"))   
                { 
                     $CurrentSystem = $WCS_SYSTEM_QUANTA_BLADE1 
                }
            }
        }
        #---------------------------------------------------- 
        # Quanta WCS Gen2 Blade (BIOS: C1031* )
        #---------------------------------------------------- 
        ElseIf ($BiosInfo.SMBIOSBIOSVersion.StartsWith("C1031"))
        {
            If ($null -eq $FruInfo) 
            { 
                $FruInfo = IpmiLib_GetBmcFru 
            }

            If ($null -eq $FruInfo) 
            {
                Write-Verbose ("SystemLookup: No FRU info found`r")                 
            }    
            Else
            {
                Write-Verbose ("SystemLookup: FRU productname {0}, model {1} '`r" -f $FruInfo.ProductName.Value,$FruInfo.ProductModel.Value)

                If (($FruInfo.ProductName.Value.Trim() -eq "C1030Q"  ) -AND ($FruInfo.ProductModel.Value.Trim() -eq "X892646-001"))   
                { 
                     $CurrentSystem = $WCS_SYSTEM_QUANTA_BLADE2
                }
            }
        }
        #---------------------------------------------------- 
        # WiWynn WCS Gen2 Blade (BIOS: C1032* )
        #---------------------------------------------------- 
        ElseIf ($BiosInfo.SMBIOSBIOSVersion.StartsWith("C1032"))
        {
            If ($null -eq $FruInfo) 
            { 
                $FruInfo = IpmiLib_GetBmcFru 
            }

            If ($null -eq $FruInfo) 
            {
                Write-Verbose ("SystemLookup: No FRU info found`r")                 
            }    
            Else
            {
                Write-Verbose ("SystemLookup: FRU productname {0}, model {1} '`r" -f $FruInfo.ProductName.Value,$FruInfo.ProductModel.Value)

                If (($FruInfo.ProductName.Value.Trim() -eq "C1030W"  ) -AND ($FruInfo.ProductModel.Value.Trim() -eq "X905452-001"))   
                { 
                     $CurrentSystem = $WCS_SYSTEM_WIWYNN_BLADE2
                }
            }
        }
        #---------------------------------------------------- 
        # DELL WCS Gen2 Blade (BIOS: C1033* )
        #---------------------------------------------------- 
        ElseIf ($BiosInfo.SMBIOSBIOSVersion.StartsWith("C1033"))
        {
            If ($null -eq $FruInfo) 
            { 
                $FruInfo = IpmiLib_GetBmcFru 
            }

            If ($null -eq $FruInfo) 
            {
                Write-Verbose ("SystemLookup: No FRU info found`r")                 
            }    
            Else
            {
                Write-Verbose ("SystemLookup: FRU productname {0}, model {1} '`r" -f $FruInfo.ProductName.Value,$FruInfo.ProductModel.Value)

                If (($FruInfo.ProductName.Value.Trim() -eq "C1030D"  ) -AND ($FruInfo.ProductModel.Value.Trim() -eq "WW9TT"))   
                { 
                     $CurrentSystem = $WCS_SYSTEM_DELL_BLADE2
                }
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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        $CurrentSystem = $UNKNOWN_SYSTEM 
    }
        
    Write-Verbose ("SystemLookup: Returned system '{0}'`r" -f $CurrentSystem)
    Write-Output $CurrentSystem         
}
