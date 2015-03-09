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
param([switch] $RunDebug)
#-----------------------------------------------------------------------------------------------------
# WinPEBootloader  
#-----------------------------------------------------------------------------------------------------
# This script searches all logical drives for the WCS Test Tool scripts and then...
# ... loads the scripts
# ... runs the WinPe debug function
#
# This script is part of the WinPE image and is run from the RAM drive after boot
# Keep this script simple and small
#-----------------------------------------------------------------------------------------------------
$FoundWinPeDrive = $false

Try
{
    #-----------------------------------------------------------------------------------------------------
    # Check each drive, use the first one found
    #-----------------------------------------------------------------------------------------------------
    Get-WmiObject Win32_LogicalDisk | ForEach-Object {

        #-----------------------------------------------------------------------------------------------------
        # WinPE USB drives have a Volume name of WINPE.  Wcs Test Tools WinPE images also place a file in the 
        # \WcsTest directory to identify the drive contains the Wcs Test Tools
        #-----------------------------------------------------------------------------------------------------
        If (($_.VolumeName -eq 'WINPE') -and  (Test-Path ("{0}\WcsTest\WcsTest_WinPEImage.txt" -f $_.DeviceId)))
        {       
            $FoundWinPeDrive = $true

            #-----------------------------------------------
            # Change the working directory to the new drive
            #-----------------------------------------------
            Set-Location $_.DeviceId
            [System.IO.Directory]::SetCurrentDirectory($_.DeviceId)

            #-----------------------------------------------
            # Load the scripts
            #-----------------------------------------------
            . ("{0}\wcstest\scripts\wcsscripts.ps1" -f $_.DeviceId)

            #-----------------------------------------------
            # Run the Win PE debug function
            #-----------------------------------------------
            If ($RunDebug) { Start-WcsToolsForWinPeDebug }

            #-----------------------------------------------
            # Don't bother checking the rest of the drives
            #-----------------------------------------------
            break
        }
    }
    #-----------------------------------------------------------------------------------------------------
    # Display error message if did not find drive.  Must be corrupted image
    #-----------------------------------------------------------------------------------------------------
    If (-NOT $FoundWinPeDrive)
    {
        Write-Host 'Could not find WinPE drive'
        Write-Host 'Possible corrupted or invalid WinPE image'
    }
}
#-----------------------------------------------------------------------------------------------------
# Display error message if exception occurred.  Must be corrupted image
#-----------------------------------------------------------------------------------------------------
Catch
{
    Write-Host 'Unknown error occurred during the loading of the scripts'
    Write-Host 'Possible corrupted or invalid WinPE image'
}
