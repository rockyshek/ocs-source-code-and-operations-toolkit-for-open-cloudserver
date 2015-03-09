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

#-------------------------------------------------------------------------------------------------
# Gets info from a Mellanox adapter  
#-------------------------------------------------------------------------------------------------
Function Get-MellanoxInfo()
{ 
    [CmdletBinding()]
    Param()

    $MellanoxInfo = @{      Device          = $WCS_NOT_AVAILABLE; 
                            UefiVersion     = $WCS_NOT_AVAILABLE; 
                            PxeVersion      = $WCS_NOT_AVAILABLE; 
                            FirmwareVersion = $WCS_NOT_AVAILABLE;                   
 
                            Output          = '';  # Combined standard output and standard error from flint                         
                         }
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Define vars and constants
        #-------------------------------------------------------
        $StdOutBuilder  = New-Object System.Text.StringBuilder(16384)
        $StdErrBuilder  = New-Object System.Text.StringBuilder(1024)
        #-------------------------------------------------------
        # Read the device type using mst
        #-------------------------------------------------------
        $STDOUT_FILE    = "$WCS_RESULTS_DIRECTORY\mellanox_Info.out.txt"
        $STDERR_FILE    = "$WCS_RESULTS_DIRECTORY\mellanox_Info.err.txt"
        #-------------------------------------------------------------------------------------------------
        # If app not installed return 
        #-------------------------------------------------------------------------------------------------
        If (-NOT (Test-Path "$WCS_BINARY_DIRECTORY\Mellanox\mst.exe"))
        {
            Return $MellanoxInfo
        }
        #-------------------------------------------------------------------------------------------------
        # Delete temp files if exist
        #-------------------------------------------------------------------------------------------------
        Remove-Item $STDOUT_FILE -ErrorAction SilentlyContinue | Out-Null 
        Remove-Item $STDERR_FILE -ErrorAction SilentlyContinue | Out-Null 
        #------------------------------------------------------------------
        # WindowStyle only available in PowerShell version 3.0 and later
        #------------------------------------------------------------------
        If ( (Host).Version.Major -lt 3) 
        {
            $Mst = Start-Process -RedirectStandardOutput $STDOUT_FILE -RedirectStandardError $STDERR_FILE -FilePath "$WCS_BINARY_DIRECTORY\Mellanox\mst.exe" -ArgumentList "status" -Wait -PassThru  
        }
        Else
        {
            $Mst = Start-Process -RedirectStandardOutput $STDOUT_FILE -RedirectStandardError $STDERR_FILE -FilePath "$WCS_BINARY_DIRECTORY\Mellanox\mst.exe" -ArgumentList "status" -Wait -PassThru -WindowStyle Hidden
        }
        #-------------------------------------------------------
        # If failed to get device ID return blank object
        #-------------------------------------------------------
        If ($Mst.ExitCode -ne 0)
        {
            Write-Verbose ("Mst returned error code {0}`r" -f $Mst.ExitCode)
            Return $MellanoxInfo
        }

        $MellanoxOutput = (Get-Content $STDOUT_FILE)  

        $Mellanox_Device_Id = $MellanoxOutput[2]
        #-------------------------------------------------------
        # Read the info using device id from above
        #-------------------------------------------------------
        #------------------------------------------------------------------
        # WindowStyle only available in PowerShell version 3.0 and later
        #------------------------------------------------------------------
        If ( (Host).Version.Major -lt 3) 
        {
            $Flint = Start-Process -RedirectStandardOutput $STDOUT_FILE -RedirectStandardError $STDERR_FILE -FilePath "$WCS_BINARY_DIRECTORY\Mellanox\flint.bat" -ArgumentList "-d $Mellanox_Device_Id q" -Wait -PassThru  
        }
        Else
        {
            $Flint = Start-Process -RedirectStandardOutput $STDOUT_FILE -RedirectStandardError $STDERR_FILE -FilePath "$WCS_BINARY_DIRECTORY\Mellanox\flint.bat" -ArgumentList "-d $Mellanox_Device_Id q" -Wait -PassThru -WindowStyle Hidden
        }
        #-------------------------------------------------------
        # If failed to get info return blank object
        #-------------------------------------------------------
        If ($Flint.ExitCode -ne 0)
        {
            Write-Verbose ("Flint returned error code {0}`r" -f $Flint.ExitCode)
            Return $MellanoxInfo
        }
 
        Get-Content $STDOUT_FILE | Where-Object {$_ -ne $null} | ForEach-Object { 
            $Line = $_
            $StdOutBuilder.Append($Line + "`r") | Out-Null

            Switch ($Line.Split(':')[0].Trim())
            {
                "Device ID"            { $MellanoxInfo.Device            = $Line.Split(':')[1].Trim(); break}
                "FW Version"           { $MellanoxInfo.FirmwareVersion   = $Line.Split(':')[1].Trim(); break}
               
                default { break }      
            } 

            If ($Line.Contains("type=PXE")) 
            {
                $Line.Split() | Where-Object {$_ -ne $null} | ForEach-Object { 
            
                    If ($_.StartsWith("version=")) { $MellanoxInfo.PxeVersion = $_.Replace("version=","") }
                }
            }
            If ($Line.Contains("type=UEFI")) 
            {
                $Line.Split() | Where-Object {$_ -ne $null} | ForEach-Object { 

                    If ($_.StartsWith("version=")) { $MellanoxInfo.UefiVersion = $_.Replace("version=","") }
                }
            }
        }

        #-------------------------------------------------------------------------------------------------
        # Save the error output
        #-------------------------------------------------------------------------------------------------
        Get-Content $STDERR_FILE | Where-Object {$_ -ne $null} | ForEach-Object { 

            $Line = $_
            $StdErrBuilder.Append($Line + "`r") | Out-Null
        }
        #-------------------------------------------------------------------------------------------------
        # Merge standard out and standard error
        #-------------------------------------------------------------------------------------------------
        $MellanoxInfo.Output  = ("`r`n *** Standard Output for 'Flint.bat'  ***`r`n`r`n{0} " -f $StdOutBuilder.ToString() )
        $MellanoxInfo.Output += ("`r`n *** Standard Error for 'Flint.bat' ***  `r`n`r`n{0} " -f $StdErrBuilder.ToString() )
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
    #-------------------------------------------------------------------------------------------------
    # Remove the temp files
    #-------------------------------------------------------------------------------------------------
    Remove-Item $STDOUT_FILE -ErrorAction SilentlyContinue | Out-Null 
    Remove-Item $STDERR_FILE -ErrorAction SilentlyContinue | Out-Null 

    Return $MellanoxInfo
}
