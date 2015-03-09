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

 

#-----------------------------------------------------------------------------------------------------------------------
# This file defines functions specific to the Quanta Mt Rainier compute blade for WCS that allow:
#
#  1. Decoding system specific SEL entries
#  2. Displaying physical location of components
#
#-----------------------------------------------------------------------------------------------------------------------

$SystemDefined_EventErrors = $null

#-----------------------------------------------------------------------------------------------------------------------
# Helper function that converts DIMM number to location
#-----------------------------------------------------------------------------------------------------------------------
# Mt Rainier has 12 DIMMS.  This maps DIMM number to physical location.  It also maps the device locator property
# from Win32_PhysicalMemory to physical location because it does not match the board silkscreen.
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_GetDimmLocation()
{   
    
     Write-Output 'DIMM N/A' 
    
}
#-----------------------------------------------------------------------------------------------------------------------
# Decode of Mt Rainier specific SEL entries.  Refer to the Mt Rainier BIOS and BMC specifications for details
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_DecodeSelEntry() 
{
    Param
    ( 
        [Parameter(Mandatory=$true)] [ref] $SelEntry, 
                                           $LastSelEntry
    )

    Try
    {
        IpmiLib_DecodeSelEntry  ($SelEntry) 
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
    
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
    }
}
#-------------------------------------------------------------------------------------
# Helper function that gets disk location
#-------------------------------------------------------------------------------------
Function DefinedSystem_GetDiskLocation()
{
    Param
    (
        [Parameter(Mandatory=$true)]    $DiskInfo,
        [Parameter(Mandatory=$true)]    $EnclosureId,
        [Parameter(Mandatory=$true)]    $SlotId
    )

    Write-Output $DiskInfo.DeviceId
}
#-----------------------------------------------------------------------------------------------------------------------
# Helper function that gets the base FRU inforamtion
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_GetFruInformation()
{     
    [CmdletBinding()]

    Param( )
    Throw "Reading FRU information is not supported on this system type"
}
