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
# FRU Constants
#-----------------------------------------------------------------------------------------------------------------------
Set-Variable  -Name WCS_BLADE_FRU_READ_SIZE    -Value  16  -Option ReadOnly -Force
Set-Variable  -Name WCS_BLADE_FRU_WRITE_SIZE   -Value  8   -Option ReadOnly -Force
Set-Variable  -Name WCS_ALLOWED_FRU_RETRIES    -Value  3   -Option ReadOnly -Force

  
#-------------------------------------------------------------------
#  Define IPMI Constants
#-------------------------------------------------------------------
Set-Variable  -Name WCS_CHASSIS_NETFN        -Value  ([byte] 0x00) -Option ReadOnly -Force
Set-Variable  -Name WCS_BRIDGE_NETFN         -Value  ([byte] 0x02) -Option ReadOnly -Force
Set-Variable  -Name WCS_SENSOR_NETFN         -Value  ([byte] 0x04) -Option ReadOnly -Force
Set-Variable  -Name WCS_APP_NETFN            -Value  ([byte] 0x06) -Option ReadOnly -Force
Set-Variable  -Name WCS_FW_NETFN             -Value  ([byte] 0x08) -Option ReadOnly -Force
Set-Variable  -Name WCS_STORAGE_NETFN        -Value  ([byte] 0x0A) -Option ReadOnly -Force
Set-Variable  -Name WCS_TRANSPORT_NETFN      -Value  ([byte] 0x0C) -Option ReadOnly -Force
Set-Variable  -Name WCS_OEM_NETFN            -Value  ([byte] 0x30) -Option ReadOnly -Force

Set-Variable  -Name  IPMI_COMPLETION_CODE_NORMAL -Value ([byte] 0) -Option ReadOnly -Force

Set-Variable  -Name  WCS_IPMI_DECODE_FIELD_LENGTH -Value  46 -Option ReadOnly -Force
#-----------------------------------------------------------------------------------------------------------------------
# SEL Constants
#-----------------------------------------------------------------------------------------------------------------------
Set-Variable  -Name SEL_ENTRY  -Value    @{ RecordId                = $WCS_NOT_AVAILABLE;
                                            RecordType              = $WCS_NOT_AVAILABLE;
                                            TimeStamp               = $WCS_NOT_AVAILABLE;
                                            TimeStampDecoded        = $WCS_NOT_AVAILABLE;
                                            GeneratorId             = $WCS_NOT_AVAILABLE;
                                            ManufacturerId          = $WCS_NOT_AVAILABLE;
                                            EventMessageVersion     = $WCS_NOT_AVAILABLE;
                                            OemNonTimestampRecord   = $WCS_NOT_AVAILABLE;
                                            OemTimestampRecord      = $WCS_NOT_AVAILABLE;
                                            SensorType              = $WCS_NOT_AVAILABLE;
                                            Sensor                  = $WCS_NOT_AVAILABLE;
                                            EventDirType            = $WCS_NOT_AVAILABLE;
                                            EventData1              = $WCS_NOT_AVAILABLE;
                                            EventData2              = $WCS_NOT_AVAILABLE;
                                            EventData3              = $WCS_NOT_AVAILABLE;
                                            Location                = $WCS_NOT_AVAILABLE;
                                            Event                   = '';
                                            
                                            NoDecode                = '';
                                            Decode                  = '';

                                            HardwareError           = $false;
                                         } -Option ReadOnly -Force


#-------------------------------------------------------------------
#  Define IPMI Globals (used to speed up multiple IPMI accesses)
#-------------------------------------------------------------------
# $Global:CimSession    = $null
$Global:IpmiInstance  = $null

#----------------------------------------------------------------------------------------------
# Helper function that decodes completion code byte into readable string format
#
# Refer to Completion Codes in the IPMI V2.0 specification for details
#----------------------------------------------------------------------------------------------
Function IpmiLib_DecodeCompletionCode([byte] $CompletionCode)
{   
    If ($CompletionCode -eq 0)       { Write-Output 'Command completed normally' ; Return }
    If ($CompletionCode -eq 0xC0)    { Write-Output 'Node busy' ; Return }
    If ($CompletionCode -eq 0xC1)    { Write-Output 'Invalid command' ; Return }
    If ($CompletionCode -eq 0xC2)    { Write-Output 'Invalid command for given LUN' ; Return }
    If ($CompletionCode -eq 0xC3)    { Write-Output 'Timeout'   ; Return }
    If ($CompletionCode -eq 0xC4)    { Write-Output 'Out of space' ; Return }
    If ($CompletionCode -eq 0xC5)    { Write-Output 'Invalid or cancelled reservation ID' ; Return }
    If ($CompletionCode -eq 0xC6)    { Write-Output 'Request data truncated' ; Return }
    If ($CompletionCode -eq 0xC7)    { Write-Output 'Request data length invalid' ; Return }
    If ($CompletionCode -eq 0xC8)    { Write-Output 'Request data field length limit exceeded' ; Return }
    If ($CompletionCode -eq 0xC9)    { Write-Output 'Parameter out of range' ; Return }
    If ($CompletionCode -eq 0xCA)    { Write-Output 'Cannot return number of requested bytes' ; Return }
    If ($CompletionCode -eq 0xCB)    { Write-Output 'Requested sensor, data, or record not present' ; Return }
    If ($CompletionCode -eq 0xCC)    { Write-Output 'Invalid data field in request' ; Return }
    If ($CompletionCode -eq 0xCD)    { Write-Output 'Command illegal for specified sensor or record type' ; Return }
    If ($CompletionCode -eq 0xCE)    { Write-Output 'Command response could not be provided' ; Return }
    If ($CompletionCode -eq 0xCF)    { Write-Output 'Cannot execute duplicate request' ; Return }

    If ($CompletionCode -eq 0xD0)    { Write-Output 'Response not provided. SDR repository in update mode' ; Return }
    If ($CompletionCode -eq 0xD1)    { Write-Output 'Response not provided.  Device in firmware update mode' ; Return }
    If ($CompletionCode -eq 0xD2)    { Write-Output 'Response not provided. BMC init in progress' ; Return }
    If ($CompletionCode -eq 0xD3)    { Write-Output 'Destination unavailable' ; Return }
    If ($CompletionCode -eq 0xD4)    { Write-Output 'Insufficient privilege' ; Return }
    If ($CompletionCode -eq 0xD5)    { Write-Output 'Command not supported in present state' ; Return }
    If ($CompletionCode -eq 0xD6)    { Write-Output 'Parameter is illegal because sub-function unavailable' ; Return }

    If ($CompletionCode -eq 0xff)    { Write-Output 'Unspecified' ; Return }

    If (($CompletionCode -ge 0x01) -and ($CompletionCode -le 0x7E))   { Write-Output 'Device specific OEM code' ; Return }
    If (($CompletionCode -ge 0x80) -and ($CompletionCode -le 0xBE))   { Write-Output 'Command specific code' ; Return }
  
    Write-Output 'Reserved'
}
 
#----------------------------------------------------------------------------------------------
# Helper function to get an ipmi instance, returns null if IPMI not running
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetIpmiInstance()
{
    [CmdletBinding()]
    Param()

    Try
    {
        $Global:IpmiInstance  = Get-WmiObject -Namespace root/wmi -Class Microsoft_IPMI
    }
    Catch
    {
        $Global:IpmiInstance = $null
        Throw "[GetIpmiInstance] ERROR: Could not open IPMI communication with BMC. Verify system contains a BMC."
    }
}
#----------------------------------------------------------------------------------------------
# Invokes an IPMI command 
#----------------------------------------------------------------------------------------------
Function Invoke-WcsIpmi()
{
   <#
  .SYNOPSIS
   Invokes an IPMI command  

  .DESCRIPTION
   Invokes an IPMI command on a WCS blade

   Refer to the IPMI specification for details on using IPMI including
   the command values, input bytes and return bytes
 
  .EXAMPLE

   $ResponseBytes = Invoke-WcsIpmi 0x1 @() $WCS_APP_NETFN 

   Sets $ResponseBytes with response from command 0x1 (get BMC version)

  .PARAMETER Command
   Byte containing the IPMI command

  .PARAMETER RequestData
   Byte array containing the request data for the command

  .PARAMETER NetworkFunction
   IPMI network function to use:

   WCS_CHASSIS_NETFN, WCS_BRIDGE_NETFN, WCS_SENSOR_NETFN 
   WCS_APP_NETFN, WCS_FW_NETFN, WCS_STORAGE_NETFN, WCS_TRANSPORT_NETFN  

  .PARAMETER LUN
   LUN (logical unit) to use

  .OUTPUTS
   On success returns array of [byte] contains response data from the IPMI command

   On error returns $null

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI
  
   #>   
    Param
    ( 
        [Parameter(Mandatory=$true,Position=0)]  [byte]   $Command,
        [Parameter(Mandatory=$false,Position=1)] [byte[]] $RequestData=@(),
        [Parameter(Mandatory=$false,Position=2)] [byte]   $NetworkFunction=$WCS_APP_NETFN,
        [Parameter(Mandatory=$false,Position=3)] [byte]   $LUN=0
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #----------------------------------------------------------------------------------------------
        # Check if have an IPMI instance, if not then get one
        #----------------------------------------------------------------------------------------------
        If ($null -eq $Global:IpmiInstance) 
        { 
            IpmiLib_GetIpmiInstance -ErrorAction Stop  
        }
        #----------------------------------------------------------------------------------------------
        # Send the command and return the data
        #----------------------------------------------------------------------------------------------
        $IpmiResponseData = $Global:IpmiInstance.RequestResponse($NetworkFunction,$LUN,$Global:IpmiInstance.BMCAddress,$Command,([uint32] $RequestData.Length),$RequestData) 

        Return ,$IpmiResponseData.ResponseData 
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
#-----------------------------------------------------------------------------------------------------------------------
# Get-WcsFruData 
#-----------------------------------------------------------------------------------------------------------------------
Function Get-WcsFruData() 
{
   <#
  .SYNOPSIS
   Gets (reads) bytes from an IPMI based FRU or file

  .DESCRIPTION 
   Returns an array of bytes from an IPMI based FRU or file.  If a file is 
   specified then reads the file else reads the local system's FRU.

   If FruOffset and NumberOfBytes not specified then gets the entire FRU

   If NumberOfBytes set as -1 then gets the FRU data from FruOffset to the 
   end of the FRU

   The FruOffset and NumberOfBytes cannot exceed the size of the FRU

   If File is specified then gets the FRU data from File.  Only files
   created with the Log-WcsFru command can be read with this command.
   
   .PARAMETER FruOffset
   Specifies the offset to begin reading the FRU.  Default is 0.

   .PARAMETER NumberOfBytes
   Specifies the number of bytes to read. Default is -1.  If -1 specified
   then reads from FruOffset to the end of the FRU.

   .PARAMETER DeviceId
   Specifies the device ID of the FRU to read.  Default is 0.
   
  .PARAMETER File
   Name of the file to get the FRU data.  If not specified reads local system's
   FRU data 

  .PARAMETER LogDirectory
   Directory to read the file.  Default directory is <InstallDir>\Results\Log-WcsFru

   .OUTPUTS
   On success returns array of [byte] where each entry is a FRU byte

   On error returns $null

  .EXAMPLE
   $FruData = Get-WcsFruData 

   Reads the entire FRU and stores in the variable $FruData as an array of bytes.
   
   To display the byte at offset 0:  $FruData[0]
   To display bytes 16 thru 31:      $FruData[16..31]

  .EXAMPLE
   $FruData = Get-WcsFruData -FruOffset 16 -NumberOfBytes 8 

   Saves 8 bytes from FRU offset 16 (hex 0x10) to FRU offset 24 into $FruData as 
   an array of bytes.

   To display the first byte (which is offset 16 in the FRU): $FruData[0]
   
  .EXAMPLE
   $FruData = Get-WcsFruData -File MyFru

   Reads the FRU data from \WcsTest\Results\Log-WcsFru\MyFru.fru.log and stores
   in $FruData as an array of bytes.

   This example is the same as:

      $FruData = Get-WcsFruData -File MyFru.fru.log
      $FruData = Get-WcsFruData -File MyFru.log

  .EXAMPLE
   $FruData = Get-WcsFruData -File MyFru -LogDirectory \FruArchive

   Reads the FRU data from \FruArchive\MyFru.fru.log

  .EXAMPLE
   Log-WcsFru -File MyFruBackup

   $FruData = Get-WcsFruData -File MyFruBackup

   Update-WcsFruData -FruOffset 0 -DataToWrite $FruData

   Saves the FRU to \WcsTest\Results\MyFruBackup.fru.log then reads it 
   into the variable $FruData as an array of bytes.  Updates the FRU
   with the data from the file.

   Note there is no Log-WcsFruData command. The Log-WcsFru creates 
   files that can be read by Get-wcsFruData and Get-WcsFru

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
        [Parameter(Mandatory=$false)]   [uint16] $FruOffset      = 0,
        [Parameter(Mandatory=$false)]   [int16]  $NumberOfBytes  = -1,
        [Parameter(Mandatory=$false)]   [byte]   $DeviceId       = 0,
        [Parameter(Mandatory=$false)]   [String] $File           ='',
        [Parameter(Mandatory=$false)]   [String] $LogDirectory   = "$WCS_RESULTS_DIRECTORY\Log-WcsFru"
    )

    $FruAsBytes   = @()  # Contains the bytes read from the FRU

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get FRU from file
        #-------------------------------------------------------
        If ($File -ne '')
        {
            $RawFilePath = (GetFruFile -Function $FunctionInfo.Name -File $File -logDirectory $LogDirectory )
            
            If ($null -eq $RawFilePath)
            {
                Return $null
            }

            Try
            {
                $xmlFru  =  [xml] (Get-Content $RawFilePath -ErrorAction Stop)
            }
            Catch
            {
                $xmlFru  = $null
            }
            If (-not (IsValidFru  -Function $FunctionInfo.Name $xmlFru))
            {
                Return $null
            }

            $xmlFru.WcsFruData.Offsets.ChildNodes | Where-Object {$_ -ne $null} | ForEach-Object { $FruAsBytes += ([byte] $_.Byte) }

            $FruSize = $FruAsBytes.Count

            Write-Verbose ("Read {0} bytes from {1}" -f $FruSize,$RawFilePath) 
        }
        
        #-------------------------------------------------------
        # If not reading from file then get FRU size
        #-------------------------------------------------------
        Else
        {
            [byte]    $IpmiCommand = 0x10
            [byte[]]  $RequestData = @($DeviceId)

            $IpmiData    = Invoke-WcsIpmi $IpmiCommand $RequestData $WCS_STORAGE_NETFN -ErrorAction Stop

            If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
            { 
                Throw ("Get FRU Info command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
            }
            [uint16] $FruSize = $IpmiData[2]*0x100 + $IpmiData[1]

            Write-Verbose ("FRU ID {0} size is 0x{1:X4}`r" -f  $DeviceId,$FruSize)
        }
        #----------------------------------------------------------------------
        # If NumberOfBytes is -1 set it to read to the end of FRU
        #----------------------------------------------------------------------
        If ($NumberOfBytes -eq -1)
        {
            $NumberOfBytes = $FruSize - $FruOffset 
        }
        #--------------------------------------
        # Check that not reading above FRU size
        #--------------------------------------
        If (($FruOffset -gt $FruSize) -or (($FruOffset+$NumberOfBytes) -gt $FruSize))
        {
            Write-Host ("`r`n {3} aborted: FruOffset 0x{0:X4} or FruOffset+NumberOfBytes 0x{1:X4} exceeds the FRU size 0x{2:X4}" -f  $FruOffset,($FruOffset+$NumberOfBytes),$FruSize,$FunctionInfo.Name) -ForegroundColor Yellow
            return $null
        }
        #--------------------------------------
        # Read the FRU if no file specified
        #--------------------------------------
        If ($File -eq '')
        {
            For ([uint16] $Offset=$FruOffset; $Offset -lt ($FruOffset + $NumberOfBytes); $Offset += $WCS_BLADE_FRU_READ_SIZE)
            {
                #-------------------------------------------------------------------
                # Make sure would not exceed FRU size with default read length
                #-------------------------------------------------------------------
                If (($Offset + $WCS_BLADE_FRU_READ_SIZE) -ge ($FruOffset + $NumberOfBytes)) 
                {
                    $ReadLength = [byte] (($FruOffset + $NumberOfBytes) - $Offset)
                }
                Else
                {
                    $ReadLength = [byte] $WCS_BLADE_FRU_READ_SIZE
                }        
                #--------------------------------------------------------
                # Setup the request data for IPMI command
                #--------------------------------------------------------    
                $OffsetLSB = [byte] ( $Offset -band 0xFF)
                $OffsetMSB = [byte] (($Offset -band 0xFF00) / 0x100 )

                $RequestData = @($DeviceId,$OffsetLSB,$OffsetMSB,$ReadLength)
                $IpmiCommand = 0x11

                Write-Verbose ("[] FRU read at offset 0x{0:X4} (0x{1:X2}{2:X2}), Number of bytes {3}`r" -f $Offset,$OffsetMSB,$OffsetLSB,$ReadLength)
                #--------------------------------------------------------
                # Must use retries if FRU busy (completion code 0x81)
                #-------------------------------------------------------- 
                For ($Retries=0;$Retries -lt $WCS_ALLOWED_FRU_RETRIES ; $Retries++)
                {
                    $IpmiData = Invoke-WcsIpmi  $IpmiCommand $RequestData $WCS_STORAGE_NETFN -ErrorAction Stop

                    If ($IpmiData[0] -eq $IPMI_COMPLETION_CODE_NORMAL)
                    {
                        break
                    } 
                    ElseIf ($IpmiData[0] -eq 0x81)
                    {
                        Write-Verbose ("[] FRU Read command at {0} returned completion code 0x{1:X2} indicating FRU busy`r" -f $Offset,$IpmiData[0])
                        Start-Sleep -Milliseconds 30
                    }
                    Else
                    {
                        Throw ("FRU Read command at {0} returned completion code 0x{1:X2} {2}" -f $Offset,$IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
                    }
                }
                #--------------------------------------------------------
                # Return $null if could not read the entire FRU
                #--------------------------------------------------------
                If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
                { 
                    Throw ("FRU read command failed to read FRU {0} times in a row. Last completion code 0x{1:X2} {2]" -f $WCS_ALLOWED_FRU_RETRIES,$IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
                }
                #--------------------------------------------------------
                # Return $null if could not read the entire FRU
                #--------------------------------------------------------
                If ($IpmiData[1] -ne $ReadLength) 
                { 
                    Throw ("FRU read command returned wrong number of bytes.  Expected {0} but returned {1}" -f  $ReadLength ,$IpmiData[1])
                }
                #--------------------------------------------------------
                # Strip first two bytes (Completion Code, Count Returned)
                #--------------------------------------------------------
                For ($ByteCount=2; $ByteCount -lt (2 + $ReadLength); $ByteCount++)
                {
                    $FruAsBytes  += [byte] $IpmiData[$ByteCount]
                }
            }
        }
        #--------------------------------------
        # Return the data
        #--------------------------------------
        Write-Output $FruAsBytes
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

#-----------------------------------------------------------------------------------------------------------------------
# Update-WcsFruData 
#-----------------------------------------------------------------------------------------------------------------------
Function Update-WcsFruData() 
{
   <#
  .SYNOPSIS
   Updates FRU data using IPMI Write FRU commands

  .DESCRIPTION
   Sets the FRU data specified using IPMI Write FRU commands.  Specify
   the FRU offset, FRU device ID, and bytes to write

   The FruOffset and number of bytes to write cannot exceed the size of the FRU
   
   .PARAMETER FruOffset
   Specifies the offset to begin reading the FRU.  Default is 0.

   .PARAMETER DataToWrite
   Data to write as an array of byte

   .PARAMETER DeviceId
   Specifies the device ID of the FRU to read.  Default is 0.

   .OUTPUTS
   On success returns number of bytes written

   On error returns 0 or $null

  .EXAMPLE
   Update-WcsFruData -FruOffset 0x16 -DataToWrite ([byte[]] 0,1,2,3)

   Writes 4 bytes (0,1,2,3) at FRU offset 0x16

  .EXAMPLE
   $FruData = Get-WcsFruData 

   $FruData[32] = 0x55
   
   Update-WcsFruData -FruOffset 0 -DataToWrite $FruData

   Reads the FRU data into $FruData, changes the value at offset
   32 and writes the data back.

   Note this example doesn't show how to update the FRU checksum.

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
        [Parameter(Mandatory=$true)]  [uint16] $FruOffset,
        [Parameter(Mandatory=$true)]  [byte[]] $DataToWrite,
        [Parameter(Mandatory=$false)] [byte]   $DeviceId = 0 
    )

    [int] $DataBytesWritten = 0

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #--------------------------------------
        # Get the size of the FRU 
        #--------------------------------------
        [byte]    $IpmiCommand = 0x10
        [byte[]]  $RequestData = @($DeviceId)

        [byte[]]  $IpmiData    = Invoke-WcsIpmi $IpmiCommand $RequestData $WCS_STORAGE_NETFN -ErrorAction Stop

        If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
        { 
            Throw ("Get FRU Info command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
        }
        [uint16] $FruSize = $IpmiData[2]*0x100 + $IpmiData[1]

        Write-Verbose ("[] FRU ID {0} size is 0x{1:X4}`r" -f  $DeviceId,$FruSize)

        #--------------------------------------
        # Check that not writing above FRU size
        #--------------------------------------
        [uint16]  $Length         = $DataToWrite.Count
        [uint16]  $RemainingBytes = $DataToWrite.Count

        If (($FruOffset+$Length) -gt $FruSize)
        {
            Write-Host ("`r`n {3} aborted: FruOffset 0x{0:X4} or FruOffset+NumberOfBytes 0x{1:X4} exceeds the FRU size 0x{2:X4}" -f  $FruOffset,($FruOffset+$Length),$FruSize,$FunctionInfo.Name) -ForegroundColor Yellow
            return $null
        }
        #--------------------------------------
        # Now write the FRU
        #--------------------------------------
        For ([int16] $Offset=$FruOffset; $Offset -lt ($FruOffset + $Length); $Offset += $WCS_BLADE_FRU_WRITE_SIZE)
        {
            $RemainingBytes = ($FruOffset + $Length) - $Offset 
            #-------------------------------------------------------------------
            # Check if remaining bytes less then max write size
            #-------------------------------------------------------------------
            If ($WCS_BLADE_FRU_WRITE_SIZE -ge $RemainingBytes) 
            {
                $LoopWriteData = [byte[]] $DataToWrite[($Offset-$FruOffset)..($Length - 1)]
            }
            Else
            {
                $LoopWriteData = [byte[]] $DataToWrite[($Offset-$FruOffset)..(($Offset-$FruOffset) + $WCS_BLADE_FRU_WRITE_SIZE - 1)]
            }        
            #--------------------------------------------------------
            # Setup the request data for IPMI command
            #--------------------------------------------------------    
            $OffsetLSB = [byte] ( $Offset -band 0xFF)
            $OffsetMSB = [byte] (($Offset -band 0xFF00) / 0x100)
        
            $IpmiCommand = 0x12
            $RequestData = @($DeviceId,$OffsetLSB,$OffsetMSB) + $LoopWriteData

            Write-Verbose ("Write FRU offset 0x{0:X4} 0x{1:X2}{2:X2}, length {3}`r" -f $Offset,$OffsetMSB,$OffsetLSB,$LoopWriteData.Count)
            #--------------------------------------------------------
            # Must use retries if FRU busy (completion code 0x81)
            #-------------------------------------------------------- 
            For ($Retries=0;$Retries -lt $WCS_ALLOWED_FRU_RETRIES ; $Retries++)
            {
                $IpmiData = Invoke-WcsIpmi  $IpmiCommand $RequestData $WCS_STORAGE_NETFN 

                If ($IpmiData[0] -eq $IPMI_COMPLETION_CODE_NORMAL)
                {
                    break
                } 
                ElseIf ($IpmiData[0] -eq 0x81)
                {
                    Write-Verbose ("[] FRU write command at {0} returned completion code 0x{1:X2} indicating FRU busy`r" -f $Offset,$IpmiData[0])
                    Start-Sleep -Milliseconds 30
                }
                Else
                {
                    Throw ("FRU write command at {0} returned completion code 0x{1:X2} {2}" -f $Offset,$IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
                }
            }
            #--------------------------------------------------------
            # Return $null if could not read the entire FRU
            #--------------------------------------------------------
            If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
            { 
                Throw ("FRU read command failed to read FRU {0} times in a row. Last completion code 0x{1:X2} {2]" -f $WCS_ALLOWED_FRU_RETRIES,$IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )

            }
            #--------------------------------------------------------
            # Return $null if could not read the entire FRU
            #--------------------------------------------------------
            If ($IpmiData[1] -ne $LoopWriteData.Count) 
            { 
                Throw ("FRU write command returned wrong number of bytes.  Expected {0} but returned {1}" -f  $LoopWriteData.Count ,$IpmiData[1])
            }
            Else
            {
                $DataBytesWritten += $LoopWriteData.Count
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
    }
    #--------------------------------------
    # Return the data
    #--------------------------------------
    Write-Output $DataBytesWritten
} 

#-----------------------------------------------------------------------------------------------------------------------
# Log-WcsFru
#-----------------------------------------------------------------------------------------------------------------------
Function Log-WcsFru() 
{
   <#
  .SYNOPSIS
   Logs FRU information to a file in xml format

  .DESCRIPTION
   Logs FRU information to a file in xml format.  If Fru is not specified
   then the local FRU is read and logged.

   The file format is specific to the Toolkit and can be read with the Get-WcsFru
   or Get-WcsFruData commands.

  .PARAMETER Fru
   Configuration xml object to save to the file.  If not specified then reads then
   logs the local FRU information.

  .PARAMETER File
   Name of the file to log the data into.  Default file name FruInfo-<dateTime>.fru.log

  .PARAMETER LogDirectory
   Directory to log the file.  Default directory is <InstallDir>\Results\Log-WcsFru

  .PARAMETER DeviceId
   FRU device ID to read.  Defaults to 0.
   
  .EXAMPLE
   Log-WcsFru -File MyFruData

   Saves the FRU data into the file \<InstallDir>\Results\Log-WcsFru\MyFruData.fru.log

  .EXAMPLE
   Log-WcsFru -File MyFruData -LogDirectory \wcsTest\Results\MyFru

   Saves the FRU data into the file \wcsTest\Results\MyFru\MyFruData.fru.log

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error
  
  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (
        [Parameter(Mandatory=$false,Position=0)] [String] $File         = ("FruInfo-{0}" -f (BaseLib_SimpleDate)),
        [Parameter(Mandatory=$false)]            [String] $LogDirectory = "$WCS_RESULTS_DIRECTORY\Log-WcsFru",
        [Parameter(Mandatory=$false)]            [int]    $DeviceId     = 0,
        [Parameter(Mandatory=$false)]                     $Fru          = $null
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
            
        #-------------------------------------------------------
        # If FruData not specified then read it
        #-------------------------------------------------------
        If ($Fru -eq $null) 
        { 
            $Fru = Get-WcsFru -DeviceId $DeviceId -ErrorAction Stop

            If ($Fru -eq $null)
            {
                Write-Host ("`r`n {0}: Could not read the FRU with Get-WcsFru command")
                Return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }
        }
        #-------------------------------------------------------
        # Check for valid FRU
        #-------------------------------------------------------
        If (-not (IsValidFru  -Function $FunctionInfo.Name $Fru))
        {
                Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        #-------------------------------------------------------
        # Create directory if doesn't exist
        #-------------------------------------------------------
        if (-NOT (Test-Path $LogDirectory -PathType Container)) 
        { 
            New-Item  $LogDirectory -ItemType Container -ErrorAction Stop | Out-Null 
        }

        $File = $File.ToLower()

        if ($File.EndsWith(".log"))    { $File =  $File.Remove($File.Length - ".log".Length)  }
        if ($File.EndsWith(".fru"))    { $File =  $File.Remove($File.Length - ".fru".Length)  }

        $RawFilePath      = Join-Path $LogDirectory ($File + ".fru.log")  

        Remove-Item $RawFilePath -ErrorAction SilentlyContinue -Force | Out-Null
        #-------------------------------------------------------
        # Save the file
        #-------------------------------------------------------
        $Fru.Save($RawFilePath) 
                 
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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
    }
}    

#-----------------------------------------------------------------------------------------------------------------------
# Update-WcsFruChecksum 
#-----------------------------------------------------------------------------------------------------------------------
Function Update-WcsFruChecksum() 
{
   <#
  .SYNOPSIS
   Updates checksum for a range of FRU data using IPMI Write FRU commands

  .DESCRIPTION
   Updates checksum for a range of FRU data using IPMI Write FRU commands

  .EXAMPLE
   Update-WcsFruChecksum -ChecksumStartOffset 8 -ChecksumEndOffset 62 -ChecksumEndOffset 63

   Writes checksum at offset 63 for the FRU range 8 to 62
   
  .PARAMETER ChecksumOffset
   Offset to write the checksum

  .PARAMETER ChecksumStartOffset
   Start of range to calculate the checksum

  .PARAMETER ChecksumEndOffset
   End of range to calculate the checksum

  .PARAMETER DeviceId
   Specifies the device ID of the FRU to read.  Default is 0.

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
        [Parameter(Mandatory=$true)]  [uint16] $ChecksumOffset,
        [Parameter(Mandatory=$true)]  [uint16] $ChecksumStartOffset,
        [Parameter(Mandatory=$true)]  [uint16] $ChecksumEndOffset,
        [Parameter(Mandatory=$false)] [byte]   $DeviceId = 0 
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get FRU data
        #-------------------------------------------------------
        $Checksum = 0

        Get-WcsFruData -FruOffset $ChecksumStartOffset -NumberOfBytes (1 + $ChecksumEndOffset - $ChecksumStartOffset) -DeviceId $DeviceId -ErrorAction Stop | Where-Object {$_ -ne $null} | ForEach-Object { 

            $Checksum += $_      
        }
        
        $ByteChecksum = [byte] ($Checksum -band 0xFF)
        $ByteChecksum = [byte] (((0xFF - $ByteChecksum) + 1) -band 0xFF)

        Write-Verbose ("The checksum {0}`r" -f $ByteChecksum)
        $BytesWritten = Update-WcsFruData -FruOffset $ChecksumOffset -DataToWrite @([byte] $ByteChecksum ) -DeviceID $DeviceId -ErrorAction Stop

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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        Return 1
    }
}

#-------------------------------------------------------------------------------------
  # Helper function for Get-WcsFru that adds fields to FRU XML object
#-------------------------------------------------------------------------------------
Function WriteFruFieldElement()
{
    Param
    (
        $xmlwriter,
        $Field
    )

    $xmlwriter.WriteStartElement($Field.Name)

    $xmlwriter.WriteAttributeString('Value',       $Field.Value)      
    $xmlwriter.WriteAttributeString('Alias',       $Field.Alias)           
    $xmlwriter.WriteAttributeString('MaxLength',   $Field.MaxLength)      
    $xmlwriter.WriteAttributeString('Pad',         $Field.Pad)      
        
    $xmlwriter.WriteEndElement()
    
} 

#-------------------------------------------------------------------------------------
# Helper function for FRU commands that gets the path to a FRU file
#-------------------------------------------------------------------------------------
Function GetFruFile()
{
    Param
    (
        $FunctionName,
        $File,
        $LogDirectory
    )
      
    $File = $File.ToLower()

    if ($File.EndsWith(".log"))    { $File =  $File.Remove($File.Length - ".log".Length)  }
    if ($File.EndsWith(".fru"))    { $File =  $File.Remove($File.Length - ".fru".Length)  }

    $RawFilePath      = Join-Path $LogDirectory ($File + ".fru.log") 
    #-------------------------------------------------------
    # Verify file exists  
    #-------------------------------------------------------
    If (-not (Test-Path $RawFilePath))
    {
        Write-Host ("`r`n {0} aborted: Could not open FRU file '{1}'`r" -f $FunctionName,$RawFilePath) -ForegroundColor Yellow
        Return $null
    }
    Return $RawFilePath
}
#-------------------------------------------------------------------------------------
# Helper function for FRU commands to verify FRU xml object correct type and version
# Supported version is 2.0
#-------------------------------------------------------------------------------------
Function IsValidFru()
{
    Param
    (
        $FunctionName,
        $xmlFru
    )
#    Try { $TypeVersion = $xmlFru.GetEnumerator()|('WcsFruData').Version } catch { $TypeVersion = $null }
    Try { $TypeVersion = $xmlFru.WcsFruData.Version } catch { $TypeVersion = $null }

    If ($TypeVersion -eq '1.0')
    {
        Write-Host ("`r`n {0} aborted: Fru file type version 1.0 is no longer supported with this command.  File '{1}'`r" -f $FunctionName,$RawFilePath) -ForegroundColor Yellow
        Return $false
    }
    ElseIf ($TypeVersion -ne '2.0')
    {
        Write-Host ("`r`n {0} aborted: Fru not valid format`r" -f $FunctionName) -ForegroundColor Yellow
        Return $false
    }

    Return $True
}
#-----------------------------------------------------------------------------------------------------------------------
# Get-WcsFru 
#-----------------------------------------------------------------------------------------------------------------------
Function Get-WcsFru() 
{
   <#
  .SYNOPSIS
   Gets FRU information for WCS compliant FRU  

  .DESCRIPTION
   Gets the FRU information from either the file specified or the local FRU.  The FRU
   must comply with the WCS specification for compute blade FRUs.

   The information is returned as an XML object.  The XML object contains the raw
   FRU bytes and WCS fields.
   
   For example to view the byte at offset 32:

        (Get-WcsFru).WcsFruData.Offsets.Offset[32].Byte

   Note that reading the raw FRU bytes is easier with the Get-WcsFruData command.
 
   To view the Chassis Serial number value and max allowed length:
   
        (Get-WcsFru).WcsFruData.Chassis.ChassisSerialNumber.Value
        (Get-WcsFru).WcsFruData.Chassis.ChassisSerialNumber.MaxLength       

  .EXAMPLE
   $FruData = Get-WcsFru

   Stores the current FRU information in $FruData

  .EXAMPLE
   $FruData = Get-WcsFru -File SavedFruInfo -Logdirectory \wcstest\results\fru

   Stores the FRU information in \wcstest\results\fru\SavedFruInfo.fru.log
   into $FruData
   
  .EXAMPLE
   $ChassisSerialNumber = (Get-WcsFru).WcsFruData.Chassis.ChassisSerialNumber.Value

   Gets the chassis serial number from the FRU

  .PARAMETER File
   Name of the file to get the FRU data.  If not specified reads local system's
   FRU information 

  .PARAMETER LogDirectory
   Directory to read the file.  Default directory is <InstallDir>\Results\Log-WcsFru

   .PARAMETER DeviceId
   Specifies the device ID of the FRU to read.  Default is 0.

   .OUTPUTS
   On success returns array of [byte] where each entry is a FRU byte

   On error returns $null

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
        [Parameter(Mandatory=$false,Position=0)] [String] $File         ='',
        [Parameter(Mandatory=$false)]            [String] $LogDirectory = "$WCS_RESULTS_DIRECTORY\Log-WcsFru",
        [Parameter(Mandatory=$false)]            [byte]   $DeviceId     = 0
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Get FRU from file
        #-------------------------------------------------------
        If ($File -ne '')
        {
            $RawFilePath = (GetFruFile -Function $FunctionInfo.Name -File $File -logDirectory $LogDirectory )
            
            If ($null -eq $RawFilePath)
            {
                Return $null
            }
            Try
            {
                $xmlFru  =  [xml] (Get-Content $RawFilePath -ErrorAction Stop)
            }
            Catch
            {
                $xmlFru = $null
            }
            If (-not (IsValidFru  -Function $FunctionInfo.Name $xmlFru))
            {
                Return $null
            }

            Write-Output $xmlFru 
        }
        #-------------------------------------------------------
        # Read local FRU
        #-------------------------------------------------------
        Else
        {
            #-------------------------------------------------------------------------------------
            # Read the current FRU bytes
            #-------------------------------------------------------------------------------------  
            $FruAsBytes = Get-WcsFruData -ErrorAction Stop        

            #-------------------------------------------------------------------------------------
            # Build the XML object from the raw data bytes
            #-------------------------------------------------------------------------------------
            $myBuilder = New-Object System.Text.StringBuilder(350000)
            $xmlwriter = [system.xml.xmlwriter]::Create($myBuilder)

            $ChassisStart     = 8* $FruAsBytes[2]
            $BoardStart       = 8* $FruAsBytes[3]
            $ProductStart     = 8* $FruAsBytes[4]


            If ($FruAsBytes[5] -eq 0)
            {
                $MultiRecordStart =$FruAsBytes.Count
            }
            Else
            {
                $MultiRecordStart = 8* $FruAsBytes[5]
            }

            $DecodedFru = IpmiLib_GetBmcFru -FruAsBytes $FruAsBytes
            #-------------------------------------------------------------------------------------
            # Add extra fields from template file
            #-------------------------------------------------------------------------------------
            $FruTemplate  =  DefinedSystem_GetFruInformation  -ErrorAction Stop

            If ($FruTemplate -eq $null) { return $null }
            
            $FruTemplate.WcsFruData.Chassis.ChildNodes | ForEach-Object {

                $DecodedFru[$_.Name].MaxLength = $_.MaxLength
                $DecodedFru[$_.Name].Pad       = $_.Pad
                $DecodedFru[$_.Name].Alias     = $_.Alias
            }

            $FruTemplate.WcsFruData.Board.ChildNodes | ForEach-Object {

                $DecodedFru[$_.Name].MaxLength = $_.MaxLength
                $DecodedFru[$_.Name].Pad       = $_.Pad
                $DecodedFru[$_.Name].Alias     = $_.Alias
            }

            $FruTemplate.WcsFruData.Product.ChildNodes | ForEach-Object {

                $DecodedFru[$_.Name].MaxLength = $_.MaxLength
                $DecodedFru[$_.Name].Pad       = $_.Pad
                $DecodedFru[$_.Name].Alias     = $_.Alias
            }

            #-------------------------------------------------------------------------------------
            # Add the header elements
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('WcsFruData')
            $xmlwriter.WriteAttributeString('Version','2.0')

            $xmlwriter.WriteStartElement('FruSize')
            $xmlwriter.WriteAttributeString('Value',$FruAsBytes.Count)  
            $xmlwriter.WriteEndElement()

            $xmlwriter.WriteStartElement('FruHost')
            $xmlwriter.WriteAttributeString('Value','ComputeBlade')  
            $xmlwriter.WriteEndElement()

            #-------------------------------------------------------------------------------------
            # Add the chassis fields
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('Chassis')
            $xmlwriter.WriteAttributeString('Length',($BoardStart-$ChassisStart))  

            WriteFruFieldElement $xmlwriter  $DecodedFru.ChassisPartNumber
            WriteFruFieldElement $xmlwriter  $DecodedFru.ChassisSerial

            If ( @('FRU v0.01') -notcontains $DecodedFru.BoardFruFileId.Value )
            {
                WriteFruFieldElement $xmlwriter  $DecodedFru.ChassisCustom1
                WriteFruFieldElement $xmlwriter  $DecodedFru.ChassisCustom2
            }
            $xmlwriter.WriteEndElement()
            #-------------------------------------------------------------------------------------
            # Add the board fields
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('Board')
            $xmlwriter.WriteAttributeString('Length',($ProductStart-$BoardStart))

            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardMinutes
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardMfgDate
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardManufacturer
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardName
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardSerial
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardPartNumber
            WriteFruFieldElement $xmlwriter  $DecodedFru.BoardFruFileId

            $xmlwriter.WriteEndElement()
            #-------------------------------------------------------------------------------------
            # Add the product fields
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('Product')
            $xmlwriter.WriteAttributeString('Length',($MultiRecordStart-$ProductStart))

            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductManufacturer
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductName
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductModel
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductVersion
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductSerial
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductAsset
            WriteFruFieldElement $xmlwriter  $DecodedFru.ProductFruFileId
            
            If ( @('FRU v0.01') -notcontains $DecodedFru.BoardFruFileId.Value  )
            {
                WriteFruFieldElement $xmlwriter  $DecodedFru.ProductCustom1
                WriteFruFieldElement $xmlwriter  $DecodedFru.ProductCustom2
                WriteFruFieldElement $xmlwriter  $DecodedFru.ProductCustom3
            }
            $xmlwriter.WriteEndElement()
            #-------------------------------------------------------------------------------------
            # Add the multirecord field
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('MultiRecord')
            $xmlwriter.WriteAttributeString('Length',$DecodedFru.MultiRecordAreaBytes.Length)
            $xmlwriter.WriteEndElement()
            
            #-------------------------------------------------------------------------------------
            # Add the raw bytes
            #-------------------------------------------------------------------------------------
            $xmlwriter.WriteStartElement('Offsets')

            $Offset = 0

            $FruAsBytes | Where-Object {$_ -ne $null} |  ForEach-Object {            

                If ($_ -ge 0x20) { $CharString = ([char] $_) }
                Else             { $CharString = '' }

                $xmlwriter.WriteStartElement('Offset')
                $xmlwriter.WriteAttributeString('Value',$Offset++)  
                $xmlwriter.WriteAttributeString('Byte',("0x{0:X2}" -f $_))  
                $xmlwriter.WriteAttributeString('Char',$CharString)  
                $xmlwriter.WriteEndElement()
            }

            $xmlwriter.WriteEndElement()        
            $xmlwriter.WriteEndElement()
            $xmlwriter.Close()
        
            $FruConfig = New-Object system.Xml.xmldocument
            $FruConfig.LoadXml( $myBuilder.ToString() )           
    
            Write-Output $FruConfig
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
    }
}   
#-----------------------------------------------------------------------------------------------------------------------
# Helper function for Update-WcsFru that adds a FRU field to raw data byte array
#-----------------------------------------------------------------------------------------------------------------------
Function AddFruField()
{
    Param
    (
        [string] $RequestedField,
                 $CurrentFruField,
                 $TemplateFruField,
        [switch] $NoMerge
    )

    #---------------------------------------------------------------------------------------------------
    # If requested field not specified use current value unless NoMerge set or original value missing
    #---------------------------------------------------------------------------------------------------
    If ($RequestedField  -eq '')  
    { 
        If ($NoMerge -or ($CurrentFruField.Length -eq 0 )) { $RequestedField  = $TemplateFruField.Value  }
        Else                                               { $RequestedField  = $CurrentFruField.Value   }
    }
    #---------------------------------------------------------------------------------------------------
    # Check field for under and over length
    #---------------------------------------------------------------------------------------------------
    If ($RequestedField.Length -gt $TemplateFruField.MaxLength)
    {
        Write-Host (" Update-WcsFru: Field {0} value '{1}' length {2} greater then allowed length {3}. Field truncated to max allowed`r" -f $TemplateFruField.NAme,$RequestedField, $RequestedField.Length, $TemplateFruField.MaxLength) -ForegroundColor Yellow
        $RequestedField = $RequestedField.Substring(0,$TemplateFruField.MaxLength)
    }
    ElseIf ($TemplateFruField.Pad -eq 'True')
    {
        $RequestedField  += (' ' * ($TemplateFruField.MaxLength - $RequestedField.Length)) 
    }
    #---------------------------------------------------------------------------------------------------
    # Output IPMI FRU length byte then field as array of bytes
    #---------------------------------------------------------------------------------------------------            
    Write-Output ([byte] ($RequestedField.Length + 0xC0))

    $RequestedField.ToCharArray() | ForEach-Object { Write-Output ([byte] $_) }
}
#-----------------------------------------------------------------------------------------------------------------------
# Update-WcsFru
#-----------------------------------------------------------------------------------------------------------------------
Function Update-WcsFru() 
{
   <#
  .SYNOPSIS
   Updates a WCS compliant FRU 

  .DESCRIPTION
   Updates information in a WCS compliant FRU. FRU fields can be updated
   one at a time or multiple at once.  

   User configurable fields such as serial number can be updated using the
   input parameters. All fields can be updated using a template file. 

   When a template file is specified the command will merge the template fields with
   the current user configurable fields so user information is not lost. If the current
   fields cannot be read then the generic template fields are used (unless the input
   parameter are specified)

   If the -NoMerge switch is specified then the user configurable fields from
   the template file are used (unless the input parameters are specified).

   All user configurable inputs are strings except for BoardMinutes.  BoardMinutes is   
   the board manufacturing time expressed as the number of minutes after Jan 1, 1996.
   To display the board manufacturing date:

   (get-date -Year 1996 -Month 1 -Day 1 -Hour 0 -Minute 0 -Second 0).AddMinutes($BoardMinutes)

  .EXAMPLE
   Update-WcsFru -ProductAsset MS12345 -ProductSerial SN01234567890

   Updates the asset tag and product serial number

  .EXAMPLE
   Update-WcsFru -TemplateFile FRU_V0.06

   Updates FRU with FRU_V0.06 template file and the current user
   configurable fields.

  .EXAMPLE
   Update-WcsFru -TemplateFile FRU_V0.06 -NoMerge

   Updates FRU with FRU_V0.06 template file and ignores the current
   user configurable fields

  .EXAMPLE
   Update-WcsFru -TemplateFile FRU_V0.06 -ProductSerial 123456

   Updates FRU with FRU_V0.06 template file and the current user
   configurable fields except for ProductSerial which is set to 123456


  .PARAMETER ChassisSerial 
   Chassis serial number.

  .PARAMETER ChassisCustom1
   Chassis custom field #1 

  .PARAMETER ChassisCustom2
   Chassis custom field #2

  .PARAMETER BoardSerial
   Motherboard serial number.   

  .PARAMETER BoardMinutes
   Motherboard manufacturing date in minutes from Jan 1, 1996 

  .PARAMETER ProductSerial
   Product serial number 

  .PARAMETER ProductAsset
   Asset tag.  

  .PARAMETER ProductCustom1
   Product custom field #1

  .PARAMETER ProductCustom2
   Product custom field #2

  .PARAMETER ProductCustom3
   Product custom field #3

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (

        [Parameter(Mandatory=$false)][Alias('ChassisSerialNumber')][ValidateLength(2,100)]
        [String] $ChassisSerial  = '',

        [Parameter(Mandatory=$false)][Alias('ChassisMfgData')][ValidateLength(2,100)]
        [String] $ChassisCustom1 = '',

        [Parameter(Mandatory=$false)][Alias('Integrator')][ValidateLength(1,100)]
        [String] $ChassisCustom2 = '',
        
        [Parameter(Mandatory=$false)][Alias('MBSerialNumber')][Alias('BoardSerialNumber')][ValidateLength(2,100)]
        [String] $BoardSerial  = '',

        [Parameter(Mandatory=$false)]
        [int] $BoardMinutes  = -1,

        [Parameter(Mandatory=$false)][Alias('AssetTag')][ValidateLength(2,100)]
        [String] $ProductAsset = '',
        
        [Parameter(Mandatory=$false)][Alias('SerialNumber')][Alias('ProductSerialNumber')][ValidateLength(2,100)]
        [String] $ProductSerial  = '',

        [Parameter(Mandatory=$false)][Alias('ManufacturerCode')][ValidateLength(1,100)]
        [String] $ProductCustom1 = '',

        [Parameter(Mandatory=$false)] [ValidateLength(2,100)]
        [String] $ProductCustom2 = '',

        [Parameter(Mandatory=$false)][Alias('BuildVersion')][ValidateLength(2,100)]
        [String] $ProductCustom3 = '',

        [Parameter(Mandatory=$false)]
        [String] $TemplateFile='',

        [switch] $NoMerge
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Load the template  
        #-------------------------------------------------------
        If ('' -eq $TemplateFile)
        {            
            $FruTemplate  =  DefinedSystem_GetFruInformation  -ErrorAction Stop
        }
        Else
        {
            $LogDirectory = "$WCS_REF_DIRECTORY\FruTemplates"
            $RawFilePath  = (GetFruFile -Function $FunctionInfo.Name -File $TemplateFile -logDirectory $LogDirectory )
            
            If ($null -eq $RawFilePath)
            {
                Return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }

            $FruTemplate  = Get-WcsFru -File $TemplateFile  -LogDirectory $LogDirectory -ErrorAction Stop
        }
        #-------------------------------------------------------
        # Verify valid FRU xml object
        #-------------------------------------------------------
        If (-not (IsValidFru  -Function $FunctionInfo.Name $FruTemplate))
        {
            Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        #--------------------------------------------------------------
        # Check fields are valid for FRU version
        #--------------------------------------------------------------
        If ( @('FRU v0.01') -contains $FruTemplate.WcsFruData.Board.BoardFruFileId.Value )
        {
            If ($ChassisCustom1 -ne '')
            {
                Write-Host ("`r`n {0} aborted: ChassisCustom1 cannot be specified for FRU version 0.01" -f $FunctionInfo.Name)
                return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }   
            If ($ChassisCustom2 -ne '')
            {
                Write-Host ("`r`n {0} aborted: ChassisCustom2 cannot be specified for FRU version 0.01" -f $FunctionInfo.Name)
                return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }   
            If ($ProductCustom1 -ne '')
            {
                Write-Host ("`r`n {0} aborted: ProductCustom1 cannot be specified for FRU version 0.01" -f $FunctionInfo.Name)
                return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }   
            If ($ProductCustom3 -ne '')
            {
                Write-Host ("`r`n {0} aborted: ProductCustom3 cannot be specified for FRU version 0.01" -f $FunctionInfo.Name)
                return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }              
        } 
        #----------------------------------------------------------------------------------------
        # Read the current FRU info so can merge the unique fields with the template
        #----------------------------------------------------------------------------------------
        If (-not $NoMerge)
        {
            Try
            {
                $FruInfo = IpmiLib_GetBmcFru -ErrorAction Stop
            }
            Catch
            {
                Write-Host ("`r`n {0} aborted: Could not read local FRU to merge field.  To skip merge run with -NoMerge input" -f $FunctionInfo.Name)
                return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }
        }
        Else
        {
            $FruInfo =    @{ 

                                      Location              = 'Location'  

                                      ChassisPartNumber     = @{Name='ChassisPartNumber'  ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisSerial         = @{Name='ChassisSerial'      ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisCustom1        = @{Name='ChassisCustom1'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisCustom2        = @{Name='ChassisCustom2'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      BoardMfgDate          = @{Name='BoardMfgDate'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardMinutes          = @{Name='BoardMinutes'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardManufacturer     = @{Name='BoardManufacturer'  ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardName             = @{Name='BoardName'          ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      BoardSerial           = @{Name='BoardSerial'        ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardPartNumber       = @{Name='BoardPartNumber'    ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardFruFileId        = @{Name='BoardFruFileId'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      ProductManufacturer   = @{Name='ProductManufacturer';Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      ProductName           = @{Name='ProductName'        ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductModel          = @{Name='ProductModel'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductVersion        = @{Name='ProductVersion'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductSerial         = @{Name='ProductSerial'      ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductAsset          = @{Name='ProductAsset'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductFruFileId      = @{Name='ProductFruFileId'   ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      ProductCustom1        = @{Name='ProductCustom1'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductCustom2        = @{Name='ProductCustom2'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductCustom3        = @{Name='ProductCustom3'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      
                                      MultiRecordAreaBytes  = @{Name='MultiRecordAreaBytes';Value=[byte[]] @() ;Length=0 }                                        
                                   }   
        }

        $ChassisStart     = 8                                                        
        $BoardStart       = $ChassisStart + $FruTemplate.WcsFruData.Chassis.Length    
        $ProductStart     = $BoardStart   + $FruTemplate.WcsFruData.Board.Length     
        $MultiRecordStart = $ProductStart + $FruTemplate.WcsFruData.Product.Length   

        If ($FruTemplate.WcsFruData.MultiRecord.Length -eq 0)
        {
            [byte[]] $RawFruBytes = @(1,0,($ChassisStart/8),($BoardStart/8),($ProductStart/8),0,0,0,1,8,0x17)
        }
        Else
        {
            [byte[]] $RawFruBytes = @(1,0,($ChassisStart/8),($BoardStart/8),($ProductStart/8),($MultiRecordStart/8),0,0,1,8,0x17)
        }

        #---------------------------------------------------------
        # Add chassis fields
        #---------------------------------------------------------
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ChassisPartNumber  -Template  $FruTemplate.WcsFruData.Chassis.ChassisPartNumber -NoMerge:$true
        $RawFruBytes += AddFruField -Requested $ChassisSerial  -Current $FruInfo.ChassisSerial      -Template  $FruTemplate.WcsFruData.Chassis.ChassisSerial     -NoMerge:$NoMerge

        If ( @('FRU v0.01') -notcontains $FruTemplate.WcsFruData.Board.BoardFruFileId.Value )
        {
            $RawFruBytes += AddFruField -Requested $ChassisCustom1 -Current $FruInfo.ChassisCustom1     -Template  $FruTemplate.WcsFruData.Chassis.ChassisCustom1    -NoMerge:$NoMerge
            $RawFruBytes += AddFruField -Requested $ChassisCustom2 -Current $FruInfo.ChassisCustom2     -Template  $FruTemplate.WcsFruData.Chassis.ChassisCustom2    -NoMerge:$NoMerge
        }

        $RawFruBytes += 0xC1                                                           
        $RawFruBytes += New-Object 'byte[]' ($BoardStart - $RawFruBytes.Count)    # Pad with 0 until board start
        #---------------------------------------------------------
        # Add Board fields
        #---------------------------------------------------------
        $RawFruBytes += @(1,0xA,0x19)

        If ($BoardMinutes -eq -1)
        {
            If ($NoMerge -or ($FruInfo.BoardMinutes.Value  -eq $WCS_NOT_AVAILABLE)) { $Minutes =  $FruTemplate.WcsFruData.Board.BoardMinutes.Value  }
            Else                                                                    { $Minutes =  $FruInfo.BoardMinutes.Value                       }                  
        }
        Else
        {
            $Minutes = $BoardMinutes
        }

        $RawFruBytes +=   $Minutes -band 0xFF
        $RawFruBytes += (($Minutes -band 0xFFFF00)  / 0x100)   -band 0xFF
        $RawFruBytes +=  (($Minutes -band 0xFF0000) / 0x10000)  -band 0xFF   

        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.BoardManufacturer  -Template  $FruTemplate.WcsFruData.Board.BoardManufacturer  -NoMerge:$true
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.BoardName          -Template  $FruTemplate.WcsFruData.Board.BoardName          -NoMerge:$true
        $RawFruBytes += AddFruField -Requested $BoardSerial    -Current $FruInfo.BoardSerial        -Template  $FruTemplate.WcsFruData.Board.BoardSerial        -NoMerge:$NoMerge
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.BoardPartNumber    -Template  $FruTemplate.WcsFruData.Board.BoardPartNumber    -NoMerge:$true
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.BoardFruFileId     -Template  $FruTemplate.WcsFruData.Board.BoardFruFileId     -NoMerge:$true

        $RawFruBytes += 0xC1                                                           
        $RawFruBytes += New-Object 'byte[]' ($ProductStart - $RawFruBytes.Count)    # Pad with 0
         
        #---------------------------------------------------------
        # Add Product fields
        #---------------------------------------------------------
        $RawFruBytes += @(1,0xD,0x19)

        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ProductManufacturer -Template  $FruTemplate.WcsFruData.Product.ProductManufacturer  -NoMerge:$true
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ProductName        -Template  $FruTemplate.WcsFruData.Product.ProductName           -NoMerge:$true
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ProductModel       -Template  $FruTemplate.WcsFruData.Product.ProductModel          -NoMerge:$true
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ProductVersion     -Template  $FruTemplate.WcsFruData.Product.ProductVersion    -NoMerge:$true
        $RawFruBytes += AddFruField -Requested $ProductSerial  -Current $FruInfo.ProductSerial      -Template  $FruTemplate.WcsFruData.Product.ProductSerial     -NoMerge:$NoMerge
        $RawFruBytes += AddFruField -Requested $ProductAsset   -Current $FruInfo.ProductAsset       -Template  $FruTemplate.WcsFruData.Product.ProductAsset      -NoMerge:$NoMerge
        $RawFruBytes += AddFruField -Requested ''              -Current $FruInfo.ProductFruFileId   -Template  $FruTemplate.WcsFruData.Product.ProductFruFileId  -NoMerge:$true

        If ( @('FRU v0.01') -notcontains $FruTemplate.WcsFruData.Board.BoardFruFileId.Value )
        {
            $RawFruBytes += AddFruField -Requested $ProductCustom1 -Current $FruInfo.ProductCustom1     -Template  $FruTemplate.WcsFruData.Product.ProductCustom1    -NoMerge:$NoMerge
            $RawFruBytes += AddFruField -Requested $ProductCustom2 -Current $FruInfo.ProductCustom2     -Template  $FruTemplate.WcsFruData.Product.ProductCustom2    -NoMerge:$NoMerge
            $RawFruBytes += AddFruField -Requested $ProductCustom3 -Current $FruInfo.ProductCustom3     -Template  $FruTemplate.WcsFruData.Product.ProductCustom3    -NoMerge:$NoMerge
        }

        $RawFruBytes += 0xC1                                                           
        $RawFruBytes += New-Object 'byte[]' ($MultiRecordStart - $RawFruBytes.Count)    # Pad with 0  
        #---------------------------------------------------------
        # Add Multi-Record or pad with 0 if not there
        #---------------------------------------------------------
        If ($FruTemplate.WcsFruData.MultiRecord.Length -ne 0)
        {
            If ((-not $NoMerge) -and ($FruInfo.MultiRecordAreaBytes.Length -eq $FruTemplate.WcsFruData.Multirecord.length))
            {
                $RawFruBytes += $FruInfo.MultiRecordAreaBytes.Value
            }
            Else
            {
                $RawFruBytes += @(0xD5,1,2,0x48,0xE0,0,1,0x37,0,0xFF,0xC0,0xC1)
                $RawFruBytes += New-Object 'byte[]' ($FruTemplate.WcsFruData.FruSize.Value - $RawFruBytes.Count)    # Pad with 0  
            }
        }
        Else
        {
            $RawFruBytes += New-Object 'byte[]' ($FruTemplate.WcsFruData.FruSize.Value - $RawFruBytes.Count) 
        }
        #---------------------------------------------------------
        # Update the FRU with the new checksums
        #---------------------------------------------------------
        $BytesWritten = Update-WcsFruData -FruOffset 0 -DataToWrite $RawFruBytes
        
        If ($FruTemplate.WcsFruData.FruSize.Value -ne $BytesWritten)
        {
            Throw "FRU write length not consistent with template file.  Write $BytesWritten but expected $($FruTemplate.WcsFruData.FruSize.Value )"
        }

        $BytesWritten = Update-WcsFruChecksum -ChecksumOffset  7                     -ChecksumStartOffset 0              -ChecksumEndOffset   6
        $BytesWritten = Update-WcsFruChecksum -ChecksumOffset  ($BoardStart-1)       -ChecksumStartOffset $ChassisStart  -ChecksumEndOffset  ($BoardStart-2)
        $BytesWritten = Update-WcsFruChecksum -ChecksumOffset  ($ProductStart-1)     -ChecksumStartOffset $BoardStart    -ChecksumEndOffset  ($ProductStart-2)
        $BytesWritten = Update-WcsFruChecksum -ChecksumOffset  ($MultiRecordStart-1) -ChecksumStartOffset $ProductStart  -ChecksumEndOffset  ($MultiRecordStart-2)

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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        Return $WCS_RETURN_CODE_FUNCTION_ABORTED
    }
}    

#-------------------------------------------------------------------------------------
# Get-WcsSel
#-------------------------------------------------------------------------------------
Function Get-WcsSel() {

   <#
  .SYNOPSIS
   Gets the BMC SEL entries

  .DESCRIPTION
   Gets the BMC SEL entries. Can filter the entries by Sensor, RecordType,
   and SensorType.  These values can be specified as an array of bytes to
   filter by more than one value

   The NoDecode switch will display the raw SEL data and not attempt to decode
   any entries. 

   If HardwareError specified then only displays entries likely to be caused
   by hardware issues.

   To view the SEL entries use View-WcsSel.   

  .EXAMPLE
   $MyEntries = Get-WcsSel

   Stores the SEL entries in the $MyEntries variable

  .EXAMPLE
   $MyEntries = Get-WcsSel  -HardwareError

   Gets only SEL entries likely to be caused by hardware failure

  .EXAMPLE
   $MyEntries = Get-WcsSel  -SensorType @(0x87,0x13)

   Gets only SEL entries where Sensor Type is 0x87 or 0x13

  .PARAMETER HardwareError
   If specified gets only hardware errors

  .PARAMETER RecordType
   If specified only returns entries with a RecordType that is listed 
   in the specified RecordType 

  .PARAMETER SensorType
   If specified only returns entries with a SensorType that is listed 
   in the specified SensorType 

  .PARAMETER Sensor
   If specified only returns entries with a Sensor that is listed 
   in the specified Sensor 
   
   .OUTPUTS
   On success returns an array of SEL entries

   On error returns 0 or $null

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [switch]  $HardwareError,
        [byte[]]  $RecordType= $Null,  
        [byte[]]  $SensorType= $null,  
        [byte[]]  $Sensor=$null
    ) 

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #--------------------------------------------------------
        # Open IPMI communication to BMC if not already opened
        #--------------------------------------------------------
        IpmiLib_GetIpmiInstance  -ErrorAction Stop

        #------------------------------------------------------
        # Read all the SEL events, first entry is always 0000
        #------------------------------------------------------
        $SelEntries  = @()    # Array to hold the Sel entries that meet input parameters
        $LastEntry   = $null  # Holds the previous SEL entry
        $SelEntry    = $null  # Holds the current SEL entry
        $SelCount    = 0      # Counts number of entries
        
        $FirstSelEntry = $true

        [byte] $CurrentIdLSB =  0
        [byte] $CurrentIdMSB =  0

        #------------------------------------------------------
        # Get the number of SEL entries
        #------------------------------------------------------
        [byte]    $IpmiCommand = 0x40   # Get SELinfo Command
        [byte[]]  $RequestData = @()
            
        $IpmiData = Invoke-WcsIpmi  $IpmiCommand $RequestData $WCS_STORAGE_NETFN -ErrorAction Stop

        If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
        { 
            Throw ("Get SEL Info command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
        }

        $NumberOfEntries = $IpmiData[3]*0x100 + $IpmiData[2]
            
        #------------------------------------------------------
        # Last entry has a next entry of 0xFFFF
        #------------------------------------------------------
        while ((0xFF -ne $CurrentIdLSB) -and (0xFF -ne $CurrentIdMSB))
        {
            Write-Verbose ("Getting entry 0x{0:X2}{1:X2}`r" -f $CurrentIdMSB,$CurrentIdLSB)

            [byte]    $IpmiCommand = 0x43   # Get SEL entry Command
            [byte[]]  $RequestData = @(0,0,$CurrentIdLSB,$CurrentIdMSB,0,0xFF)
            
            $IpmiData = Invoke-WcsIpmi  $IpmiCommand $RequestData $WCS_STORAGE_NETFN -ErrorAction Stop

            If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
            { 
                Throw ("Get SEL Entry command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
            }
            #------------------------
            # Check SEL count
            #------------------------
            $SelCount++

            If ($SelCount -gt $NumberOfEntries)
            {
                Throw ("Found too many SEL entries. Expected: {0}.  Suspect SEL corruption" -f $NumberOfEntries)
            }

            #------------------------
            # Save ID for next entry
            #------------------------
            $CurrentIdLSB = $IpmiData[1]
            $CurrentIdMSB = $IpmiData[2]

            Write-Verbose ("Next entry 0x{0:X2}{1:X2}`r" -f $CurrentIdMSB,$CurrentIdLSB)
            #--------------------------------------------
            # Add SEL entry to the array of SEL entries
            #--------------------------------------------
            $SelEntry     = $SEL_ENTRY.Clone()
 
            $SelEntry.RecordId    =  [string] ("{0:X2}{1:X2}" -f $IpmiData[4],$IpmiData[3])
            $SelEntry.RecordType  =  [byte]    ("0x{0:X2}"       -f $IpmiData[5] )           
            #---------------------------------------------------------------
            # Sensor entry - Decode timestamp and parse the sensor fields
            #---------------------------------------------------------------
            If ($SelEntry.RecordType -eq 0x02)
            {                
                $SelEntry.Timestamp             =  [uint32] ("0x{0:X2}{1:X2}{2:X2}{3:X2}" -f $IpmiData[9],$IpmiData[8],$IpmiData[7],$IpmiData[6]) 
                $SelEntry.TimeStampDecoded      = IpmiLib_DecodeTimestamp $SelEntry.Timestamp

                $SelEntry.GeneratorId           = [uint16] ("0x{0:X2}{1:X2}" -f $IpmiData[10],$IpmiData[11])
                $SelEntry.EventMessageVersion   = [byte] ("0x{0:X2}"       -f $IpmiData[12])
                $SelEntry.SensorType            = [byte] ("0x{0:X2}"       -f $IpmiData[13])
                $SelEntry.Sensor                = [byte] ("0x{0:X2}"       -f $IpmiData[14])
                $SelEntry.EventDirType          = [byte] ("0x{0:X2}"       -f $IpmiData[15])
                $SelEntry.EventData1            = [byte] ("0x{0:X2}"       -f $IpmiData[16])
                $SelEntry.EventData2            = [byte] ("0x{0:X2}"       -f $IpmiData[17])
                $SelEntry.EventData3            = [byte] ("0x{0:X2}"       -f $IpmiData[18])

                $SelEntry.NoDecode              = ("{0:X4} RecordType: 0x{1:X2} TimeStamp: {2:X8} {3}" -f $SelEntry.RecordID,$SelEntry.RecordType,$SelEntry.TimeStamp, (IpmiLib_FormatSensorRecordData $SelEntry))
            }
            #--------------------------------------------------
            # OEM timestamp entry - Decode timestamp
            #--------------------------------------------------
            ElseIf (($SelEntry.RecordType -ge 0xC0) -and ($SelEntry.RecordType -le 0xDF))
            {
                $SelEntry.Timestamp         =  [uint32] ("0x{0:X2}{1:X2}{2:X2}{3:X2}" -f $IpmiData[9],$IpmiData[8],$IpmiData[7],$IpmiData[6] ) 
                $SelEntry.TimeStampDecoded  =  IpmiLib_DecodeTimestamp $SelEntry.Timestamp
                $SelEntry.ManufacturerId    =  [uint32] ("0x{0:X2}{1:X2}{2:X2}" -f $IpmiData[12],$IpmiData[11],$IpmiData[10] ) 

                $SelEntry.OemTimestampRecord  = ""

                For ($ByteIndex = 18; $ByteIndex -ge 13; $ByteIndex--)
                {
                    $SelEntry.OemTimestampRecord += ("{0:X2}" -f $IpmiData[$ByteIndex])
                }

                $SelEntry.NoDecode = ("{0:X4} RecordType: 0x{1:X2} TimeStamp: {2:X8} MfgId: 0x{3:X6}  OEM Data (16-11): 0x{4}"-f $SelEntry.RecordID,$SelEntry.RecordType,$SelEntry.TimeStamp,$SelEntry.ManufacturerId,$SelEntry.OemTimestampRecord )
            }
            ElseIf (($SelEntry.RecordType -ge 0xE0) -and ($SelEntry.RecordType -le 0xFF))
            {
                $SelEntry.OemNonTimestampRecord  = ""

                For ($ByteIndex = 18; $ByteIndex -ge 6; $ByteIndex--)
                {
                        $SelEntry.OemNonTimestampRecord += ("{0:X2}" -f $IpmiData[$ByteIndex])
                }
                $SelEntry.NoDecode  = ("{0:X4} RecordType: 0x{1:X2} OEM Data (16-4): 0x{3}" -f $SelEntry.RecordID,$SelEntry.RecordType,$SelEntry.OemNonTimestampRecord )
            }
            Else 
            {
                $UnknownRecordType = ("Unknown Record Type: 0x{0:X2}  OEM Data (16-4): 0x" -f $SelEntry.RecordType)
                For ($ByteIndex = 18; $ByteIndex -ge 6; $ByteIndex--)
                {
                         $UnknownRecordType  += ("{0:X2}" -f $IpmiData[$ByteIndex])
                }
                $SelEntry.NoDecode            = ("{0:X4} {1}" -f $SelEntry.RecordID,$UnknownRecordType)
            }

            #--------------------------------------------------
            # Decode the SEL entry (add readable description)
            #--------------------------------------------------
            If ($FirstSelEntry) 
            { 
                DefinedSystem_DecodeSelEntry ([ref] $SelEntry) 
            }
            Else
            {
                DefinedSystem_DecodeSelEntry ([ref] $SelEntry)  $LastEntry
            }
            #--------------------------------------------------
            # Add to the array if meet input requirements
            #--------------------------------------------------
            If (  (($RecordType -eq $null) -or ( $RecordType -contains $SelEntry.RecordType )) -and
                  (($SensorType -eq $null) -or ( $SensorType -contains $SelEntry.SensorType)) -and
                  (($Sensor     -eq $null) -or ($Sensor -contains $SelEntry.Sensor))    -and
                  (-NOT $HardwareError -or ($HardwareError  -and $SelEntry.HardwareError))    
            )    
            {
                $SelEntries   += $SelEntry.Clone()
            }

            $LastEntry     = $SelEntry.Clone()
            $FirstSelEntry = $false

        }
        #------------------- 
        # Output the array
        #-------------------
        Write-Output $SelEntries 
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
# Log-WcsSel
#-------------------------------------------------------------------------------------
Function Log-WcsSel() {

   <#
  .SYNOPSIS
   Logs the BMC SEL entries

  .DESCRIPTION
   Logs the BMC SEL entries.  If SelEntries not specified then reads the local
   SEL and logs all entries

   This commands logs two files (1) the undecoded version with a .sel.log extension
   and (2) the decoded version with the decoded.sel.log extension.

  .PARAMETER SelEntries
   Array of SEL entries created with the Get-WcsSel command

  .PARAMETER File
   Name of the file to log the data into.  Default file name SelEntries-<dateTime>.sel.log

  .PARAMETER LogDirectory
   Directory to log the file.  Default directory is <InstallDir>\Results\Log-WcsSel

  .EXAMPLE
   Log-WcsSel

   Logs the BMC SEL entries in 
   
      \<InstallDir>\Log-WcsSel\Selentries-<Date-Time>.sel.log
      \<InstallDir>\Log-WcsSel\Selentries-<Date-Time>.decoded.sel.log

   Where <InstallDir> is typically \WcsTest and <Date-Time> is the date and time the 
   command was run.
   
  .EXAMPLE
   Log-WcsSel -File MySelBackup  -LogDirectory \WcsTest\REsults

   Logs the BMC SEL entries in 
   
      \WcsTest\Results\MySelBackup.sel.log
      \WcsTest\Results\MySelBackup.decoded.sel.log

  .EXAMPLE
    $Sensor87 = Get-WcsSel -Sensor 0x87

    Log-WcsSel -SelEntries $Sensor87  -File Sensor87  -LogDirectory \WcsTest\Results

   Logs only the BMC SEL entries for sensor 87 in:
   
      \WcsTest\Results\Sensor87.sel.log
      \WcsTest\Results\Sensor87.decoded.sel.log

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error
  
  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>

    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (
        [Parameter(Mandatory=$false,Position=0)] [String] $File         = ("SelEntries-{0}" -f (BaseLib_SimpleDate)),
                                                 [String] $LogDirectory = "$WCS_RESULTS_DIRECTORY\Log-WcsSel",
                                                          $SelEntries   = $null
    )
    
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Check SelEntries object
        #-------------------------------------------------------
        If ($SelEntries -ne $null) 
        {
            $InvalidInput= $true
            If (($SelEntries.GetType().Name -eq 'Hashtable') -and $SelEntries.ContainsKey('NoDecode'))
            {
                $InvalidInput = $false
            }
            ElseIf (($SelEntries.GetType().Name -eq 'Object[]') -and $SelEntries[0].ContainsKey('NoDecode'))
            {
                $InvalidInput = $false
            }

            If ($InvalidInput)
            {
                Write-Host ("`r`n {0} aborted: SelEntries input not valid`r" -f $FunctionInfo.Name) -ForegroundColor Yellow
                Return $WCS_RETURN_CODE_FUNCTION_ABORTED
            }
        }

        #-------------------------------------------------------
        # Create directory if doesn't exist
        #-------------------------------------------------------
        if (-NOT (Test-Path $LogDirectory -PathType Container)) { New-Item  $LogDirectory -ItemType Container | Out-Null }

        $File = $File.ToLower()

        if ($File.EndsWith(".log"))    { $File =  $File.Remove($File.Length - ".log".Length)     }
        if ($File.EndsWith(".sel"))    { $File =  $File.Remove($File.Length - ".sel".Length)  }

        $RawFilePath      = Join-Path $LogDirectory ($File + ".sel.log")  
        $DecodedFilePath  = Join-Path $LogDirectory ($File + ".decoded.sel.log")  

        Remove-Item $RawFilePath      -ErrorAction SilentlyContinue -Force | Out-Null
        Remove-Item $DecodedFilePath  -ErrorAction SilentlyContinue -Force | Out-Null
                    
        If ($null -eq $SelEntries) { $SelEntries = Get-WcsSel }

        $SelEntries | Where-Object {$_ -ne $null} | ForEach-Object {
            Add-Content -Path  $RawFilePath        -Value $_.NoDecode
            Add-Content -Path  $DecodedFilePath    -Value $_.Decode
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
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        Return $WCS_RETURN_CODE_UNKNOWN_ERROR
    }

}

#-------------------------------------------------------------------------------------
# Clear-WcsSel
#-------------------------------------------------------------------------------------
Function Clear-WcsSel() {

   <#
  .SYNOPSIS
   Clears the BMC SEL  

  .DESCRIPTION
   Clears the BMC SEL then waits for the SEL erase to complete.

  .EXAMPLE
   Clear-WcsSel
   
   .OUTPUTS
   Always returns $null

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>

    [CmdletBinding()]

    Param( )
    
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Wait for SEL erase to complete
        #-------------------------------------------------------
        $SelEraseComplete = $False
        $TimeOut          = 30

        #-----------------------------------------------------------------
        # Need new reservation ID because clear SEL command cancels it
        #-----------------------------------------------------------------
        [byte[]]  $IpmiData = Invoke-WcsIpmi  0x42 @() $WCS_STORAGE_NETFN -ErrorAction Stop

        If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
        { 
            Throw ("Reserve SEL command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
        }
        $ReservationMSB = $IpmiData[2]
        $ReservationLSB = $IpmiData[1]

        #-----------------------------------------------------------------
        # Clear SEL command 
        #-----------------------------------------------------------------
        [byte[]]  $RequestData = @($ReservationLSB,$ReservationMSB,0X43,0X4C,0X52,0xAA)

        $IpmiData = Invoke-WcsIpmi  0x47 $RequestData  $WCS_STORAGE_NETFN -ErrorAction Stop
        
        If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
        { 
            Throw ("Clear SEL command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
        }
        #-----------------------------------------------------------------
        # Wait for clear to complete
        #-----------------------------------------------------------------
        For ($WaitTime = 0; $WaitTime -lt $TimeOut; $WaitTime++)
        {
            #-----------------------------------------------------------------
            # Need new reservation ID because clear SEL command cancels it
            #-----------------------------------------------------------------
            [byte[]]  $IpmiData = Invoke-WcsIpmi  0x42 @() $WCS_STORAGE_NETFN -ErrorAction Stop

            If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
            { 
                Throw ("Reserve SEL command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
            }
            $ReservationMSB = $IpmiData[2]
            $ReservationLSB = $IpmiData[1]

            #-----------------------------------------------------------------
            # Get SEL status
            #-----------------------------------------------------------------
            [byte[]]  $RequestData = @($ReservationLSB,$ReservationMSB,0X43,0X4C,0X52,0x00)

            $IpmiData = Invoke-WcsIpmi  0x47 $RequestData  $WCS_STORAGE_NETFN -ErrorAction Stop

            If ($IpmiData[0] -ne $IPMI_COMPLETION_CODE_NORMAL) 
            { 
                Throw ("Clear SEL command returned completion code 0x{0:X2} {1}" -f $IpmiData[0],(ipmiLib_DecodeCompletionCode $IpmiData[0]) )
            }
            If (($IpmiData[1] -band 0x0F) -eq 0x1) 
            {
                $SelEraseComplete = $true
                break 
            }
            #-----------------------------------------------------------------
            # Wait one second before trying again
            #-----------------------------------------------------------------
            Start-Sleep -Seconds 1

         }
         #-----------------------------------------------------------------
         # If failed to complete throw error
         #-----------------------------------------------------------------
         If (-NOT $SelEraseComplete) 
         {
            Throw "SEL erase did complete in $TimeOut seconds"
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
    }
} 

#-------------------------------------------------------------------------------------
# View-WcsSel
#-------------------------------------------------------------------------------------
Function View-WcsSel() {

   <#
  .SYNOPSIS
   Views the BMC SEL entries

  .DESCRIPTION
   Views the BMC SEL entries. Can filter the entries by Sensor, RecordType,
   and SensorType.  These values can be specified as an array of bytes to
   filter by more than one value

   The NoDecode switch will display the raw SEL data and not attempt to decode
   any entries. 

   If HardwareError specified then only displays entries likely to be caused
   by hardware issues.

  .EXAMPLE
   View-WcsSel
   
   Displays all BMC SEL entries

  .EXAMPLE
   View-WcsSel  -HardwareError

   Displays only SEL entries likely to be caused by hardware failure

  .EXAMPLE
   View-WcsSel  -SensorType @(0x87,0x13)

   Displays only SEL entries where Sensor Type is 0x87 or 0x13

  .EXAMPLE
   View-WcsSel  -SensorType @(0x87) -Sensor @(0xCC) -NoDecoded

   Displays only SEL entries where Sensor Type is 0x87 and the Sensor
   is 0xCC.  Displays the entries in the raw format.

  .PARAMETER NoDecode
   If specified does not decode the SEL entries

  .PARAMETER HardwareError
   If specified gets only hardware errors

  .PARAMETER RecordType
   If specified only returns entries with a RecordType that is listed 
   in the specified RecordType 

  .PARAMETER SensorType
   If specified only returns entries with a SensorType that is listed 
   in the specified SensorType 

  .PARAMETER Sensor
   If specified only returns entries with a Sensor that is listed 
   in the specified Sensor 
   
   .OUTPUTS
   Always returns $null

  .COMPONENT
   WCS

  .FUNCTIONALITY
   IPMI

   #>
    
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    ( 
        [switch]  $NoDecode, 
        [switch]  $HardwareError,
        [byte[]]  $RecordType = $Null,  
        [byte[]]  $SensorType = $null,  
        [byte[]]  $Sensor     = $null
    )
    
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Get the entries
        #-------------------------------------------------------
        $SelEntries = @( Get-WcsSel -RecordType $RecordType -SensorType $SensorType -Sensor $Sensor -HardwareError:$HardwareError -ErrorAction Stop )

        If ($SelEntries.Count -eq 0 )
        {
            Write-Host " View-WcsSel found no entries in the SEL`r`n`r"
        }
        Else
        {
            $SelEntries| Where-Object {$_ -ne $null} | ForEach-Object {

                If ($NoDecode) { Write-Host ("{0}`r" -f $_.NoDecode) }
                Else           { Write-Host ("{0}`r" -f $_.Decode)   }
            }
        }
        Write-Host "`r"
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


#-----------------------------------------------------------------------------------------------------------------------
# Formats record type 0x02 into a readable (undecoded) string
#-----------------------------------------------------------------------------------------------------------------------
Function IpmiLib_FormatSensorRecordData($SelEntry) 
{
    $FormattedData = ''

    $FormattedData += ("GenID: {0:X4} EvMRev: {1:X2} "                  -f $SelEntry.GeneratorId,$SelEntry.EventMessageVersion)
    $FormattedData += ("SensorType: {0:X2} Sensor: {1:X2} "              -f $SelEntry.SensorType,$SelEntry.Sensor)
    $FormattedData += ("EventDirType: {0:X2} EventData(3-1): {1:X2} {2:X2} {3:X2} " -f $SelEntry.EventDirType,$SelEntry.EventData3,$SelEntry.EventData2,$SelEntry.EventData1)
    
    Write-Output $FormattedData
}

#-----------------------------------------------------------------------------------------------------------------------
# Converts the IPMI timestamp to a readable string of 23 characters
#
# See the Timestamp Format section of IPMI v2.0 specification for details
#-----------------------------------------------------------------------------------------------------------------------
Function IpmiLib_DecodeTimestamp([uint32] $TimeStamp)
{
    If ($TimeStamp -eq 0xFFFFFFFF) 
    {
        $DecodeTimeStamp = '[Invalid TimeStamp]'
    }
    ElseIf ($TimeStamp -le 0x20000000) 
    {
        $DecodeTimeStamp = ("[PreInit 0x{0:X8}sec]" -f $TimeStamp)
    }
    Else
    {
        $ConvertedDate   = (get-date -Year 1970 -Month 1 -Day 1 -Hour 0 -Minute 0 -Second 0).AddSeconds($Timestamp)
        $DecodeTimeStamp = ("[{0} {1}]" -f $ConvertedDate.ToShortDateString(),$ConvertedDate.ToLongTimeString())
    }

    Write-Output ("{0,-24}" -f $DecodeTimeStamp)
}
#-----------------------------------------------------------------------------------------------------------------------
# Generic decode of IPMI SEL entries
#
# See the IPMI v2.0 specification for details
#-----------------------------------------------------------------------------------------------------------------------
Function IpmiLib_DecodeSelEntry([ref] $SelEntry) 
{
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        $SelEntry.Value.Decode   =  ("{0} {1} " -f $SelEntry.Value.RecordId, $SelEntry.Value.TimeStampDecoded)
        $SelEntry.Value.Event         = ''
        #----------------------------------------------------
        # Decode sensor record type
        #----------------------------------------------------
        If ($SelEntry.Value.RecordType -eq 0x02)
        {
            #----------------------------------------------------
            # Then decode by sensor type
            #----------------------------------------------------
            Switch ($SelEntry.Value.SensorType)
            {
                #----------------------------------------------------
                # Sensor Type 01h - Temperature Sensor
                #----------------------------------------------------
                0x01
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'System'

                    Switch ($SelEntry.Value.EventDirType)
                    {
                        0x01    {  $SelEntry.Value.Event = 'Temperature exceeded threshold' ; break }
                        0x81    {  $SelEntry.Value.Event = 'Temperature within threshold'   ; break }
                        Default {  $SelEntry.Value.Event = 'Temperature event' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f $SelEntry.Value.Event, (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 02h - Voltage Sensor
                #----------------------------------------------------
                0x02
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'BOARD'

                    Switch ($SelEntry.Value.EventDirType)
                    {
                        0x01    {  $SelEntry.Value.Event = 'Voltage exceeded threshold' ; break }
                        0x81    {  $SelEntry.Value.Event = 'Voltage within threshold'   ; break }
                        Default {  $SelEntry.Value.Event = 'Voltage event' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f $SelEntry.Value.Event, (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 03h - Current Sensor
                #----------------------------------------------------
                0x03
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'System'

                    Switch ($SelEntry.Value.EventDirType)
                    {
                        0x01    {  $SelEntry.Value.Event = 'Current exceeded threshold' ; break }
                        0x81    {  $SelEntry.Value.Event = 'Current within threshold'   ; break }
                        Default {  $SelEntry.Value.Event = 'Current event' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f $SelEntry.Value.Event, (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 04h - Fan Sensor
                #----------------------------------------------------
                0x04
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'System'

                    Switch ($SelEntry.Value.EventDirType)
                    {
                        0x01    {  $SelEntry.Value.Event = 'Fan exceeded threshold' ; break }
                        0x81    {  $SelEntry.Value.Event = 'Fan within threshold'   ; break }
                        Default {  $SelEntry.Value.Event = 'Fan event' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f $SelEntry.Value.Event, (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 07h - Processor Sensor
                #----------------------------------------------------
                0x07
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'Processor'

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0       { $SelEntry.Value.Event   = 'IERR'         ; break }
                        1       { $SelEntry.Value.Event   = 'Thermal Trip' ; break } 
                        2       { $SelEntry.Value.Event   = 'FRB1'         ; break } 
                        3       { $SelEntry.Value.Event   = 'FRB2'         ; break } 
                        4       { $SelEntry.Value.Event   = 'FRB3'         ; break } 
                        5       { $SelEntry.Value.Event   = 'configuration error'         ; break } 
                        6       { $SelEntry.Value.Event   = 'SMBIOS error'                ; break } 
                        7       { $SelEntry.Value.Event   = 'presence detected'            ; $SelEntry.Value.HardwareError = $false ;  break } 
                        8       { $SelEntry.Value.Event   = 'disabled'                     ; break } 
                        9       { $SelEntry.Value.Event   = 'terminator presence detected'           ; $SelEntry.Value.HardwareError = $false ; break } 
                        0xA     { $SelEntry.Value.Event   = 'automatically throttled'      ; break } 
                        0xB     { $SelEntry.Value.Event   = 'uncorrectable machine check'  ; break } 
                        0xC     { $SelEntry.Value.Event   = 'correctable machine check'    ; break } 

                        Default { $SelEntry.Value.Event   = 'Processor event' }
                    }   
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 0Ch - Memory Sensor
                #----------------------------------------------------
                0x0C
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'DIMM'

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0x0       { $SelEntry.Value.Event   = 'correctable ECC'               ; break }
                        0x1       { $SelEntry.Value.Event   = 'uncorrectable ECC'             ; break } 
                        0x5       { $SelEntry.Value.Event   = 'correctable ECC limit reached' ; break } 
                        Default   { $SelEntry.Value.Event   = 'memory error' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 0Dh - Disk Sensor
                #----------------------------------------------------
                0x0D
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'Disk'

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0x1       { $SelEntry.Value.Event   = 'fault'               ; break }
                        0x2       { $SelEntry.Value.Event   = 'predictive fail'     ; break } 
                        Default   { $SelEntry.Value.HardwareError = $false }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #---------------------------------------------------------
                # Sensor Type 0Fh - System Firmware Progress (POST error)
                #---------------------------------------------------------
                0x0F
                { 
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'System'                         
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f 'POST Error', (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 10h - Event logging disabled
                #----------------------------------------------------
                0x10
                { 
                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0       { $SelEntry.Value.Event   = ("Correctable memory logging disabled on DIMM {0}" -f $SelEntry.Value.EventData1)   ; break }
                        1       { $SelEntry.Value.Event   = ("Event logging disabled for specific type.  EvtData2 {0] EvtData3 {1}" -f $SelEntry.Value.EventData2, $SelEntry.Value.EventData3); break } 
                        2       { $SelEntry.Value.Event   = 'SEL cleared'                         ; break } 
                        3       { $SelEntry.Value.Event   = 'Event logging disabled'              ; break } 
                        4       { $SelEntry.Value.Event   = 'SEL full'                            ; break } 
                        5       { $SelEntry.Value.Event   = ("SEL almost full.  EvtData3 {0}" -f $SelEntry.Value.EventData3)                             ; break } 
                        6       { $SelEntry.Value.Event   = ("Correctable Machine Check logging disabled.  EvtData2 {0] EvtData3 {1}" -f $SelEntry.Value.EventData2, $SelEntry.Value.EventData3);  ; break } 

                        Default { $SelEntry.Value.Event   = 'Event logging disabled' }
                    }        
                    $SelEntry.Value.Decode   += $SelEntry.Value.Event                               
                }
                #----------------------------------------------------
                # Sensor Type 13h - Critical Interupt
                #----------------------------------------------------
                0x13
                { 
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'Board/Adapter'

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                                0x4       { $SelEntry.Value.Event   = 'PCI PERR'                 ; break }
                                0x5       { $SelEntry.Value.Event   = 'PCI SERR'                 ; break } 
                                0x7       { $SelEntry.Value.Event   = 'correctable bus error'    ; break } 
                                0x8       { $SelEntry.Value.Event   = 'uncorrectable bus error'  ; break } 
                                0xA       { $SelEntry.Value.Event   = 'fatal bus error'          ; break } 
                                Default   { $SelEntry.Value.Event   = 'critical interrupt' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))                                           
                }
                #----------------------------------------------------
                # Sensor Type 19h - Chipset
                #----------------------------------------------------
                0x19
                { 
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'Board'

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                                0x1       { $SelEntry.Value.Event   = 'Chipset thermal trip'       ; break }
                                Default   { $SelEntry.Value.Event   = 'Chipset soft power failure' }
                    }

                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }
                #----------------------------------------------------
                # Sensor Type 1Fh - Base OS Boot/Installation  
                #----------------------------------------------------
                0x1F
                {
                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0       { $SelEntry.Value.Event   = 'A: boot complete'                       ; break }
                        1       { $SelEntry.Value.Event   = 'C: boot complete'                       ; break }
                        2       { $SelEntry.Value.Event   = 'PXE boot complete'                      ; break }
                        3       { $SelEntry.Value.Event   = 'Diagnostic boot complete'               ; break } 
                        4       { $SelEntry.Value.Event   = 'CD-ROM boot complete'                   ; break } 
                        5       { $SelEntry.Value.Event   = 'ROM boot complete'                      ; break } 
                        6       { $SelEntry.Value.Event   = 'Base OS boot complete'                  ; break } 
                        7       { $SelEntry.Value.Event   = 'Base OS/Hypervisor install started'     ; break } 
                        8       { $SelEntry.Value.Event   = 'Base OS/Hypervisor install completed'   ; break } 
                        9       { $SelEntry.Value.Event   = 'Base OS/Hypervisor install aborted'     ; break } 
                        0xA     { $SelEntry.Value.Event   = 'Base OS/Hypervisor install failed'      ; break } 

                        Default { $SelEntry.Value.Event   = 'Base OS Boot/Installation status' }
                    }   
                    $SelEntry.Value.Decode   += $SelEntry.Value.Event                                           
                }
                #----------------------------------------------------
                # Sensor Type 20h - OS Stop/Shutdown
                #----------------------------------------------------
                0x20
                {
                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0       { $SelEntry.Value.Event   = 'OS critical stop during OS load'           ; break }
                        1       { $SelEntry.Value.Event   = 'OS run-time critical stop'                 ; break }
                        2       { $SelEntry.Value.Event   = 'OS graceful stop'                          ; break }
                        3       { $SelEntry.Value.Event   = 'OS graceful shutdown'                      ; break } 
                        4       { $SelEntry.Value.Event   = 'OS Stop/Shutdown | Soft shutdown by PEF'   ; break } 
                        5       { $SelEntry.Value.Event   = 'OS Stop/Shutdown | Agent not responding'   ; break } 

                        Default { $SelEntry.Value.Event   = 'OS Stop/Shutdown' }
                    }     
                    $SelEntry.Value.Decode   += $SelEntry.Value.Event                                         
                }  
                #----------------------------------------------------
                # Sensor Type 23h - Watchdog timer
                #----------------------------------------------------
                0x23
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'System'

                    Switch (($SelEntry.Value.EventData2 -band 0x0F))
                    {

                        1       { $SelEntry.Value.Event   = 'BIOS FRB2 watchdog timeout'                ; break }
                        2       { $SelEntry.Value.Event   = 'BIOS POST watchdog timeout'                ; break }
                        3       { $SelEntry.Value.Event   = 'OS load watchdog timeout'                  ; break } 
                        4       { $SelEntry.Value.Event   = 'OS/SMS watchdog timeout'                   ; break } 
                        5       { $SelEntry.Value.Event   = 'OEM watchdog timeout'                      ; break } 

                        Default { $SelEntry.Value.Event   = 'Watchdog timeout' }
                    }      
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f $SelEntry.Value.Event, (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                                                           
                }                     
                #----------------------------------------------------
                # Decode all other sensor types
                #----------------------------------------------------            
                Default
                {
                      $SelEntry.Value.Decode   += IpmiLib_FormatSensorRecordData $SelEntry.Value 
                }
            }
        }
        #----------------------------------------------------
        # Decode OEM timestamp record type
        #----------------------------------------------------
        ElseIf (($SelEntry.Value.RecordType -ge 0xC0) -and ($SelEntry.Value.RecordType -le 0xDF))
        {
            $SelEntry.Value.Decode   += $SelEntry.Value.NoDecode
        }
        #----------------------------------------------------
        # Decode OEM non-timestamp record type
        #----------------------------------------------------
        ElseIf (($SelEntry.Value.RecordType -ge 0xE0) -and ($SelEntry.Value.RecordType -le 0xFF))
        {
            $SelEntry.Value.Decode   += $SelEntry.Value.NoDecode
        }
        #----------------------------------------------------
        # Decode illegal type
        #----------------------------------------------------
        Else
        {
            $SelEntry.Value.Decode = $SelEntry.Value.NoDecode
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
    }
}


#----------------------------------------------------------------------------------------------
#  This function gets BMC version information
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetBmcVersion()
{
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
 
    )

    Try
    {
        Write-Debug "IpmiLib_GetBmcVersion called"

        [byte[]]  $RequestData = @()
            
        $IpmiData = Invoke-WcsIpmi  0x1 $RequestData $WCS_APP_NETFN -ErrorAction Stop

        If (0 -ne $IpmiData[0])
        {
            Throw  
        }

        #-------------------------------------------------------------------------------------
        # Change format to match clarifications in the WCS specifiations.   
        # Also added the aux firmware version if present and not 0 
        #-------------------------------------------------------------------------------------        
        If (($IpmiData.Count -ge 16) -and (0 -ne ($IpmiData[12]+$IpmiData[13]+$IpmiData[14]+$IpmiData[15])))
        {
            Write-Output ("{0}.{1}{2} (Aux 0x{3:X2}{4:X2}{5:X2}{6:X2})" -f ($IpmiData[3] -band 0x7F),(($IpmiData[4] / 0x10) -band 0xF), ($IpmiData[4] -band 0x0F),$IpmiData[12],$IpmiData[13],$IpmiData[14],$IpmiData[15] )
        }
        Else
        {
            Write-Output ("{0}.{1}{2}" -f ($IpmiData[3] -band 0x7F),(($IpmiData[4] / 0x10) -band 0xF), ($IpmiData[4] -band 0x0F) )
        }

    }
    Catch
    {
        Write-Output $WCS_NOT_AVAILABLE

    }
}
#----------------------------------------------------------------------------------------------
#  This function gets the CPLD version information
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetCpldVersion()
{
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
 
    )

    Try
    {
        [byte[]]  $RequestData = @(3)

        $ipmiData = Invoke-WcsIpmi  0x17 $RequestData $WCS_OEM_NETFN -ErrorAction Stop
        
        If (0 -ne $IpmiData[0])
        {
            Throw  
        }

        $CpldVersion = ("{0:X2}{1:X2}" -f $ipmiData[2],$ipmiData[1])
 
        Write-Output $CpldVersion

    }
    Catch
    {
        Write-Output $WCS_NOT_AVAILABLE
    }
}
#----------------------------------------------------------------------------------------------
#  This function gets BMC FRU version information
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetBmcFruVersion()
{
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support

    Param
    (   
 
    )

    Try
    {
        [byte[]]  $RequestData = @(0,87,0,12)
            
        $ipmiData = Invoke-WcsIpmi  0x11 $RequestData $WCS_STORAGE_NETFN  -ErrorAction Stop
        
        If (0 -ne $IpmiData[0])
        {
            Throw  
        }

        $FruVersion = ""

        For ($Loop=2; $Loop -lt 12; $Loop++)
        {
            $FruVersion += [char] $ipmiData[$Loop]
        }
        Write-Output $FruVersion
    }
    Catch
    {
        Write-Output $WCS_NOT_AVAILABLE

    }
}
#----------------------------------------------------------------------------------------------
#  Helper function for getting a FRU field
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetFruStringField($Start, $FruAsBytes, $FruAsString, $CheckLength=-1, [switch] $AllowC1)
{
    $Field  = $WCS_NOT_AVAILABLE
    $Length = $FruAsBytes[($Start -1)] -band  0x3F
    $Type   = $FruAsBytes[($Start -1)] -band  0xC0

    If ($Type -ne 0xC0) 
    { 
       Throw ("FRU FORMAT ERROR: Field type/length code at offset {0} (0x{0:X2}) is invalid: {1} (0x{1:X2})" -f ($Start -1), $FruAsBytes[($Start -1)])
    }
    If (($Length -eq 1) -and (-NOT $AllowC1)) 
    { 
       Throw ("FRU FORMAT ERROR: Unexpected end of field found at offset {0} (0x{0:X2})" -f ($Start -1))
    }
    
    If (($CheckLength -ne -1) -and ($CheckLength -ne $Length))
    {
        $Field  = $WCS_NOT_AVAILABLE
    }
    ElseIf  ($Length -ne 0)  
    { 
       $Field  = $FruAsString.Substring($Start,$Length)
    }   
    Else
    {
        $Field  = $WCS_NOT_AVAILABLE
    } 
    Write-Output @{Length=$Length;Field=$Field}
}
#----------------------------------------------------------------------------------------------
#  This function decodes the BMC FRU data into WCS defined fields
#  This function based on WCS blade FRU specification which defines the fields and sequence
#----------------------------------------------------------------------------------------------
Function IpmiLib_GetBmcFru()
{
    [CmdletBinding()] 

    Param
    (   
        $FruAsBytes = $null
    )

#$BmcFru = $WCS_COMPUTE_BLADE_FRU_OBJECT.Clone()
    $BmcFru =         @{ 

                                      Location              = 'Location'  

                                      ChassisPartNumber     = @{Name='ChassisPartNumber'  ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisSerial         = @{Name='ChassisSerial'      ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisCustom1        = @{Name='ChassisCustom1'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ChassisCustom2        = @{Name='ChassisCustom2'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      BoardMfgDate          = @{Name='BoardMfgDate'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardMinutes          = @{Name='BoardMinutes'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardManufacturer     = @{Name='BoardManufacturer'  ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardName             = @{Name='BoardName'          ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      BoardSerial           = @{Name='BoardSerial'        ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardPartNumber       = @{Name='BoardPartNumber'    ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      BoardFruFileId        = @{Name='BoardFruFileId'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      ProductManufacturer   = @{Name='ProductManufacturer';Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      ProductName           = @{Name='ProductName'        ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductModel          = @{Name='ProductModel'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductVersion        = @{Name='ProductVersion'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductSerial         = @{Name='ProductSerial'      ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductAsset          = @{Name='ProductAsset'       ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductFruFileId      = @{Name='ProductFruFileId'   ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}

                                      ProductCustom1        = @{Name='ProductCustom1'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductCustom2        = @{Name='ProductCustom2'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE}
                                      ProductCustom3        = @{Name='ProductCustom3'     ;Value=$WCS_NOT_AVAILABLE;Alias=$WCS_NOT_AVAILABLE;Length=0;MaxLength=$WCS_NOT_AVAILABLE;Pad=$WCS_NOT_AVAILABLE} 
                                      
                                      MultiRecordAreaBytes  = @{Name='MultiRecordAreaBytes';Value=[byte[]] @() ;Length=0 }                                        
                                   }    
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #--------------------------------------------
        # Setup the variables
        #--------------------------------------------
        $FruAsString      = "" 
        $AllowLengthOfOne = $false

        #--------------------------------------------
        # Read the entire FRU if data not provided
        #--------------------------------------------
        If ($FruAsBytes -eq $null)
        {
            $FruAsBytes = Get-WcsFruData -ErrorAction Stop
        }
        #------------------------------------------------------------------------------------------
        # Create a string of characters for human readable info.  If not a char replace with space
        #------------------------------------------------------------------------------------------
        $FruAsBytes | Where-Object {$_ -ne $null} | ForEach-Object { 
        
            If ( $_ -as [char] )
            {
                $ThisChar = ([char] $_)
            }
            Else
            {
                $ThisChar = ([char] ' ')
            }

            $FruAsString += $ThisChar
        }

        #-----------------------------------------------------------------------------------
        # Get the board fields since it contains the FRU version (same for all versions)
        #-----------------------------------------------------------------------------------
        $Start               = 8*$FruAsBytes[3] + 3
        $Length              = 3        
        
        $BmcFru.BoardMinutes.Value      = $FruAsBytes[$Start] + 0x100 * $FruAsBytes[($Start+1)] + 0x10000 * $FruAsBytes[($Start+2)]
        $BmcFru.BoardMinutes.Length     = 3           
        
        $BmcFru.BoardMfgDate.Value = (get-date -Year 1996 -Month 1 -Day 1 -Hour 0 -Minute 0 -Second 0).AddMinutes($BmcFru.BoardMinutes.Value)
        
        $Start                    = 8*$FruAsBytes[3] + 7
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.BoardManufacturer.Value     = $Info.Field
        $BmcFru.BoardManufacturer.Length    = $Info.Length

        $Start                      += $Info.Length+1
        $Info                       = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.BoardName.Value     = $Info.Field
        $BmcFru.BoardName.Length    = $Info.Length
        
        $Start                   += $Info.Length+1
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString  
        
        $BmcFru.BoardSerial.Value     = $Info.Field
        $BmcFru.BoardSerial.Length     = $Info.Length
        
        $Start                   += $Info.Length+1
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.BoardPartNumber.Value     = $Info.Field
        $BmcFru.BoardPartNumber.Length    = $Info.Length

        $Start                   += $Info.Length+1
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.BoardFruFileId.Value     = $Info.Field
        $BmcFru.BoardFruFileId.ValLengthue     = $Info.Length
        #---------------------------------------------------------------------------
        # If version 0.01, 0.02, 0.03, 0.04,or 0.5 then allow illegal field lengths of 1
        #---------------------------------------------------------------------------
        $AllowLengthOfOne =  ( @('FRU v0.01','FRU v0.02','FRU v0.03','FRU v0.04','FRU v0.05') -contains $BmcFru.BoardFruFileId.Value)

        #----------------------------------------------------------------
        # Get the chassis info fields  
        #----------------------------------------------------------------
        $Start                    = 8*$FruAsBytes[2] + 4
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.ChassisPartNumber.Value     = $Info.Field
        $BmcFru.ChassisPartNumber.Length     = $Info.Length

        $Start                   += $Info.Length+1
        $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString  

        $BmcFru.ChassisSerial.Value     = $Info.Field
        $BmcFru.ChassisSerial.Length     = $Info.Length

        Try
        {
            #------------------------------------------------
            # The following fields do not exist in v0.01
            #------------------------------------------------
            If ($BmcFru.BoardFruFileId.Value -ne 'FRU v0.01')
            {
                $Start                   += $Info.Length+1
                $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString  

                $BmcFru.ChassisCustom1.Value     = $Info.Field
                $BmcFru.ChassisCustom1.Length    = $Info.Length

                $Start                   += $Info.Length+1
                $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString -AllowC1:$AllowLengthOfOne

                $BmcFru.ChassisCustom2.Value     = $Info.Field
                $BmcFru.ChassisCustom2.Length    = $Info.Length
           }
        }
        Catch
        {
            If ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Yellow ("{0}`r" -f  $_ ) }
        }
        #----------------------------------------------------------------
        # Get the product info fields
        #----------------------------------------------------------------
        $Start                       = 8*$FruAsBytes[4] + 4
        $Info                        = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

        $BmcFru.ProductManufacturer.Value     = $Info.Field
        $BmcFru.ProductManufacturer.Length     = $Info.Length
        Try
        {

            $Start                      += $Info.Length+1
            $Info                        = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

            $BmcFru.ProductName.Value     = $Info.Field
            $BmcFru.ProductName.Length     = $Info.Length

            $Start                      += $Info.Length+1      
            $Info                        = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

            $BmcFru.ProductModel.Value     = $Info.Field
            $BmcFru.ProductModel.Length     = $Info.Length

            $Start                      += $Info.Length+1
            $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString  -AllowC1:$AllowLengthOfOne

            $BmcFru.ProductVersion.Value     = $Info.Field
            $BmcFru.ProductVersion.Length     = $Info.Length

            $Start                   += $Info.Length+1
            $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString   

            $BmcFru.ProductSerial.Value     = $Info.Field
            $BmcFru.ProductSerial.Length     = $Info.Length

            $Start                   += $Info.Length+1
            $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString  
             
            $BmcFru.ProductAsset.Value     = $Info.Field
            $BmcFru.ProductAsset.Length     = $Info.Length

            $Start                   += $Info.Length+1
            $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString -AllowC1:$AllowLengthOfOne

            $BmcFru.ProductFruFileId.Value     = $Info.Field
            $BmcFru.ProductFruFileId.Length     = $Info.Length


            #------------------------------------------------
            # The following fields do not exist in v0.01
            #------------------------------------------------
            If ($BmcFru.BoardFruFileId.Value -ne 'FRU v0.01')
            {
                $Start                   += $Info.Length+1
                $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString -AllowC1:$AllowLengthOfOne

                $BmcFru.ProductCustom1.Value     = $Info.Field
                $BmcFru.ProductCustom1.Length     = $Info.Length

                $Start                   += $Info.Length+1
                $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString

                $BmcFru.ProductCustom2.Value     = $Info.Field
                $BmcFru.ProductCustom2.Length     = $Info.Length

                $Start                   += $Info.Length+1
                $Info                     = IpmiLib_GetFruStringField $Start $FruAsBytes $FruAsString   

                $BmcFru.ProductCustom3.Value     = $Info.Field
                $BmcFru.ProductCustom3.Length     = $Info.Length
            }
     
        }
        Catch
        {
            If ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Yellow ("{0}`r" -f  $_ ) }
        }
       
        #----------------------------------------------------------------
        # Get the multi-record area
        #----------------------------------------------------------------
        $Start = 8*$FruAsBytes[5] 
        
        If ($Start -ne 0)
        {
            $BmcFru.MultiRecordAreaBytes.Length     =  $FruAsBytes.Length - $Start
            $BmcFru.MultiRecordAreaBytes.Value      =  $FruAsBytes[$Start .. ($FruAsBytes.Length-1)]
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
    }
    Write-Output $BmcFru
}


#----------------------------------------------------------------------------------------------
#  This function cycles power
#----------------------------------------------------------------------------------------------
Function Cycle-WcsPower()
{
    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #Set Interval to 30 seconds
                    
        [byte[]]  $RequestData = @(0x1E)
        
        $IpmiData = Invoke-WcsIpmi  0xB $RequestData $WCS_CHASSIS_NETFN -ErrorAction Stop

        If (0 -ne $IpmiData[0])
        {
            Throw ("Set Interval command returned IPMI completion code: {0} {1} " -f $IpmiData[0],(IpmiLib_DecodeCompletionCode $IpmiData[0]))
        }

        #Cycle power

        [byte[]]  $RequestData = @(0x2)

        $IpmiData = Invoke-WcsIpmi  0x2 $RequestData $WCS_CHASSIS_NETFN -ErrorAction Stop

        If (0 -ne $IpmiData[0])
        {
            Throw ("Power cycle command returned IPMI completion code: {0} {1} " -f $IpmiData[0],(IpmiLib_DecodeCompletionCode $IpmiData[0]))
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
    }
}

#----------------------------------------------------------------------------------------------
#  This function adds sel entries for ECC errors - Under development
#----------------------------------------------------------------------------------------------
Function IpmiLib_AddEccErrors()
{
    Try
    {
        #---------------------------------------
        # Add correctable and uncorrectable ECC 
        #---------------------------------------
        For ($Dimm=1;$Dimm -le 12; $Dimm++)
        {
            [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA0,0,$Dimm)
           
            $IpmiData = Invoke-WcsIpmi  0x44 $RequestData $WCS_STORAGE_NETFN 

            [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA1,0,$Dimm)
           
            $IpmiData = Invoke-WcsIpmi  0x44 $RequestData $WCS_STORAGE_NETFN 
        }

        Write-Output 0

    }
    Catch
    {
        Write-Output 1
    }
}
#----------------------------------------------------------------------------------------------
# Helper function to merge fields into FRU data
#----------------------------------------------------------------------------------------------
Function MergeFruField([byte[]]$FruData,[int]$Offset,[string] $Field)
{
    for ($Index=0;$Index -lt $Field.Length; $Index++)
    {
        # Make sure don't exceed the size of the array
        
        If (($Offset+$Index) -ge $FruData.Count)
        {
            break
        }

        $FruData[($Offset+$Index)] = [byte] $Field[$Index]
    }
    Write-Output $FruData

}
#----------------------------------------------------------------------------------------------
# Helper function that turns on attention LED on blade
#----------------------------------------------------------------------------------------------
Function IpmiLib_ChassisIdentifyOn()
{
    [CmdletBinding()]

    Param( )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Turn the LED on
        #-------------------------------------------------------           
        $IpmiData = Invoke-WcsIpmi  0x4  @(0,1) $WCS_CHASSIS_NETFN 

        If (0 -ne $IpmiData[0])
        {
            Throw ("Returned IPMI completion code: {0} {1} " -f $IpmiData[0],(IpmiLib_DecodeCompletionCode $IpmiData[0]))
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
    }
}

#----------------------------------------------------------------------------------------------
# Helper function that turns off attention LED on blade
#----------------------------------------------------------------------------------------------
Function IpmiLib_ChassisIdentifyOff()
{
    [CmdletBinding()]

    Param( )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        #-------------------------------------------------------
        # Turn the LED off
        #-------------------------------------------------------            
        $IpmiData = Invoke-WcsIpmi  0x4 @(0,0) $WCS_CHASSIS_NETFN 
        
        If (0 -ne $IpmiData[0])
        {
            Throw ("Returned IPMI completion code: {0} {1} " -f $IpmiData[0],(IpmiLib_DecodeCompletionCode $IpmiData[0]))
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
    }    
}

