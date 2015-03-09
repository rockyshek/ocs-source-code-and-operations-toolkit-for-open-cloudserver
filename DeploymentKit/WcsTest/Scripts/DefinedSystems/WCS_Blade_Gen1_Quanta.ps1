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
$IANA_ENTERPRISE_ID        = 0x1c4c

#-----------------------------------------------------------------------------------------------------------------------
# Helper function that converts DIMM number to location
#-----------------------------------------------------------------------------------------------------------------------
# Mt Rainier has 12 DIMMS.  This maps DIMM number to physical location.  It also maps the device locator property
# from Win32_PhysicalMemory to physical location because it does not match the board silkscreen.
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_GetDimmLocation()
{   
    Param( [Parameter(Mandatory=$true)] $DimmId )

    Switch($DimmId)
    {
         1 { Write-Output 'DIMM A1'; break }
         2 { Write-Output 'DIMM A2'; break }
         3 { Write-Output 'DIMM B1'; break }
         4 { Write-Output 'DIMM B2'; break }
         5 { Write-Output 'DIMM C1'; break }
         6 { Write-Output 'DIMM C2'; break }
         7 { Write-Output 'DIMM D1'; break }
         8 { Write-Output 'DIMM D2'; break }
         9 { Write-Output 'DIMM E1'; break }
        10 { Write-Output 'DIMM E2'; break }
        11 { Write-Output 'DIMM F1'; break }
        12 { Write-Output 'DIMM F2'; break }

        CHANNELA_DIMM1 { Write-Output 'DIMM A1'; break }
        CHANNELA_DIMM2 { Write-Output 'DIMM A2'; break }
        CHANNELB_DIMM1 { Write-Output 'DIMM B1'; break }
        CHANNELB_DIMM2 { Write-Output 'DIMM B2'; break }
        CHANNELC_DIMM1 { Write-Output 'DIMM C1'; break }
        CHANNELC_DIMM2 { Write-Output 'DIMM C2'; break }
        CHANNELD_DIMM1 { Write-Output 'DIMM D1'; break }
        CHANNELD_DIMM2 { Write-Output 'DIMM D2'; break }
        CHANNELE_DIMM1 { Write-Output 'DIMM E1'; break }
        CHANNELE_DIMM2 { Write-Output 'DIMM E2'; break }
        CHANNELF_DIMM1 { Write-Output 'DIMM F1'; break }
        CHANNELF_DIMM2 { Write-Output 'DIMM F2'; break }

        Default        { Write-Output 'DIMM N/A'; break }
    }
}
#-----------------------------------------------------------------------------------------------------------------------
# Decode of Mt Rainier specific SEL entries.  Refer to the Mt Rainier BIOS and BMC specifications for details
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_DecodeSelEntry() 
{
    Param
    ( 
        [Parameter(Mandatory=$true)]  [ref] $SelEntry,
        [Parameter(Mandatory=$false)]       $LastSelEntry=$null
    )

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
                # Sensor Type 07h - Processor Sensor
                #----------------------------------------------------
                0x07
                {
                    $SelEntry.Value.HardwareError = $true
                                     
                    Switch ($SelEntry.Value.Sensor)
                    {
                        0xD3 
                        { 
                            $SelEntry.Value.Location = 'Processor 0' 
                            $SelEntry.Value.Event    = 'Thermal Trip'
                            $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                            break 
                        }
                        0xD4  
                        { 
                            $SelEntry.Value.Location = 'Processor 1' 
                            $SelEntry.Value.Event    = 'Thermal Trip'
                            $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                            break 
                        }
                        0xD5
                        { 
                            $SelEntry.Value.Location = 'Processor' 
                            $SelEntry.Value.Event    = 'IERR'
                            $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                            break 
                        }                        
                        0xA7 
                        { 
                            $SelEntry.Value.Location = 'Processor'
                            $SelEntry.Value.Event    = 'IIO Error/MCE'
                            $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                            break 
                        }   
                        #-----------------------------------------------------------
                        # QPI error
                        #-----------------------------------------------------------
                        0x9D 
                        { 
                            #-----------------------------------------------------------
                            # Using the location defined in WCS-Softwarse-Blade-API.doc
                            #-----------------------------------------------------------
                            Switch (($SelEntry.Value.EventData3 -band 0x0F))
                            {
                                1       { $SelEntry.Value.Location   = 'Processor 0'    ; break }
                                2       { $SelEntry.Value.Location   = 'Processor 1'    ; break } 
                                4       { $SelEntry.Value.Location   = 'Processor 2'    ; break } 
                                8       { $SelEntry.Value.Location   = 'Processor 3'    ; break } 
                                Default { $SelEntry.Value.Location   = 'Processor'      ; break } 
                            }    
                                                            
                            If (($SelEntry.Value.EventData3 -band 0x10) -eq 0) { $SelEntry.Value.Event = 'QPI 0 ' }
                            Else                                               { $SelEntry.Value.Event = 'QPI 1 ' }
                            #-----------------------------------------------------------
                            # Correctable/Uncorrectable defined in IPMI
                            #-----------------------------------------------------------
                            If   (($SelEntry.Value.EventData1 -band 0x0F) -eq 0x0B) { $SelEntry.Value.Event += 'uncorrectable error'  }
                            Else                                                    { $SelEntry.Value.Event += 'correctable error'    }  

                            $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))                                                    
                            break 
                        }                                             
                        Default 
                        { 
                            IpmiLib_DecodeSelEntry  $SelEntry                            
                        }
                    }              
                }
                #----------------------------------------------------
                # Sensor Type 0Ch - Memory Sensor
                #----------------------------------------------------
                0x0C
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      =  (DefinedSystem_GetDimmLocation $SelEntry.Value.EventData3)

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0x0       { $SelEntry.Value.Event   = 'correctable ECC'               ; break }
                        0x1       { $SelEntry.Value.Event   = 'uncorrectable ECC'             ; break } 
                        0x5       { $SelEntry.Value.Event   = 'correctable ECC Limit Reached' ; break } 
                        Default   { $SelEntry.Value.Event   = 'memory event' }
                    }
                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                }    
                #----------------------------------------------------
                # Sensor Type 13h - Critical Interrupt sensor Type
                #----------------------------------------------------
                0x13
                {
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = 'BOARD/ADAPTER'
                    
                    If ( $SelEntry.Value.Sensor -eq 0xA1)
                    {

                        If (($LastSelEntry -ne $NULL) -and ($LastSelEntry.Sensor -eq 0xA1) -and ($LastSelEntry.SensorType -eq 0x13))
                        {
                            $SelEntry.Value.Event   = 'PCIe (#2)' 

                            $SelEntry.Value.Decode  += ("{1} First Error: {2:X2} Second Error: {3:X2}   {5}" -f $SelEntry.Value.Location,$SelEntry.Value.Event,$SelEntry.Value.EventData3, (($SelEntry.Value.EventData2 -band 0xF8) / 8), ($SelEntry.Value.EventData2 -band 0x7), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                        }
                        Else
                        {
                            Switch (($SelEntry.Value.EventData1 -band 0x0F))
                            {
                                0x4       { $SelEntry.Value.Event   = 'PCI PERR'                 ; break }
                                0x5       { $SelEntry.Value.Event   = 'PCI SERR'                 ; break } 
                                0x7       { $SelEntry.Value.Event   = 'correctable bus error'    ; break } 
                                0x8       { $SelEntry.Value.Event   = 'uncorrectable bus error'  ; break } 
                                0xA       { $SelEntry.Value.Event   = 'fatal bus error'          ; break } 
                                Default   { $SelEntry.Value.Event   = 'critical interrupt' }
                            }

                            $SelEntry.Value.Decode  += ("PCIe {1} Bus:{2:X2} Dev:{3:X2} Fun:{4}   {5}" -f $SelEntry.Value.Location,$SelEntry.Value.Event,$SelEntry.Value.EventData3, (($SelEntry.Value.EventData2 -band 0xF8) / 8), ($SelEntry.Value.EventData2 -band 0x7), (IpmiLib_FormatSensorRecordData $SelEntry.Value))
                        }
                    }
                    Else
                    {
                        IpmiLib_DecodeSelEntry  $SelEntry
                    }
                    Break
                }
                #---------------------------------------------------------
                # Sensor Type D2h - NVDIMM error
                #---------------------------------------------------------
                0xD2
                { 
                    $SelEntry.Value.HardwareError = $true
                    $SelEntry.Value.Location      = (DefinedSystem_GetDimmLocation $SelEntry.Value.EventData3)

                    Switch (($SelEntry.Value.EventData1 -band 0x0F))
                    {
                        0x0       { $SelEntry.Value.Event   = 'NVDIMM controller error'  ; break }
                        0x1       { $SelEntry.Value.Event   = 'NVDIMM restore failed'    ; break } 
                        0x3       { $SelEntry.Value.Event   = 'NVDIMM backup failed'     ; break } 
                        0x4       { $SelEntry.Value.Event   = 'NVDIMM erase failed'      ; break } 
                        0x6       { $SelEntry.Value.Event   = 'NVDIMM arm failed'        ; break } 
                        Default   { $SelEntry.Value.Event   = 'NVDIMM error' }
                    }

                    $SelEntry.Value.Decode   += ("{0,-$WCS_IPMI_DECODE_FIELD_LENGTH} {1}" -f ("{0} {1}" -f $SelEntry.Value.Location, $SelEntry.Value.Event), (IpmiLib_FormatSensorRecordData $SelEntry.Value))                                                                                           
                } 
                #----------------------------------------------------
                # Decode all other sensor types
                #----------------------------------------------------            
                Default
                {
                    IpmiLib_DecodeSelEntry  $SelEntry 
                }
            }
        }
        #----------------------------------------------------
        # Decode OEM timestamp record type
        #----------------------------------------------------
        ElseIf (($SelEntry.Value.RecordType -eq 0xC0) -and  ($SelEntry.Value.ManufacturerId -eq $IANA_ENTERPRISE_ID))
        {
            $VID         =   [uint16] ("0x{0}" -f $SelEntry.Value.OemTimestampRecord.Substring( ($SelEntry.Value.OemTimestampRecord.Length - 4),4))
            $DID         =   [uint16] ("0x{0}" -f $SelEntry.Value.OemTimestampRecord.Substring( ($SelEntry.Value.OemTimestampRecord.Length - 8),4))

            $SelEntry.Value.HardwareError = $true
            $SelEntry.Value.Event = 'PCIe Error'

            If ($VID -eq $LSI_VENDOR_ID)
            {
                $SelEntry.Value.Location  = 'HBA adapter'
            }
            ElseIf ($VID -eq $MELLANOX_VENDOR_ID)
            {
                $SelEntry.Value.Location  = 'NIC adapter'
            }
            ElseIf ($FPGA_VENDOR_IDS -contains ("{0:X4}-{1:X4}" -f $VID,$DID))
            {
                $SelEntry.Value.Location  = 'FPGA adapter'
            }
            Else
            {
                $SelEntry.Value.Location  = 'BOARD'
            }

            $SelEntry.Value.Decode  += ("{0} {1} VID: {2:X4} DID: {3:X4} Data(2-1): {5} {4}" -f $SelEntry.Value.Location,$SelEntry.Value.Event,$VID,$DID,`
                                                                                                $SelEntry.Value.OemTimestampRecord.Substring( ($SelEntry.Value.OemTimestampRecord.Length - 10),2),`
                                                                                                $SelEntry.Value.OemTimestampRecord.Substring( ($SelEntry.Value.OemTimestampRecord.Length - 12),2))
        }

        #----------------------------------------------------
        # Use generic decode for all others
        #----------------------------------------------------
        Else
        {
            IpmiLib_DecodeSelEntry  ($SelEntry) 
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
    
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
    }
}
#-------------------------------------------------------------------------------------
# Helper function that gets disk location
#-------------------------------------------------------------------------------------
# Mt Rainier has 4 HDD on server blade and 10 more on JBOD.  Must check if using an
# LSI adapter to determine the drive location.
#-------------------------------------------------------------------------------------
Function DefinedSystem_GetDiskLocation()
{
    Param
    (
        [Parameter(Mandatory=$true)]                       $DiskInfo,
        [Parameter(Mandatory=$true)]                       $EnclosureId,
        [Parameter(Mandatory=$true)]                       $SlotId
    )

    Try
    {
        $LabelLocation  = $DiskInfo.DeviceId

        #--------------------------------------------------------------------------------------------------------------------------
        # If Azure 3.1 Configuration
        #--------------------------------------------------------------------------------------------------------------------------
        If ((Get-WmiObject Win32_ComputerSystem).Model -eq 'C1020')
        {
            #--------------------------------------------------------------------------------------------------------------------------
            # Has 2 SSD connected to PCH on top of SSD4 and SSD2
            #--------------------------------------------------------------------------------------------------------------------------
            If ($DiskInfo.InterfaceType -eq "IDE")
            {
                Switch($DiskInfo.SCSIBus)
                {
                    0 { $LabelLocation = "SB-4-Top" ; break}
                    1 { $LabelLocation = "SB-2-Top" ; break}
                    default  {break}
                }
            }
            #--------------------------------------------------------------------------------------------------------------------------
            # Has 3 SSD and 1 HDD connected to LSI 9207
            #--------------------------------------------------------------------------------------------------------------------------
            Else
            {
                Switch($DiskInfo.SCSITargetId)
                {
                    0 { $LabelLocation = "SB-5" ; break}
                    1 { $LabelLocation = "SB-4" ; break}
                    2 { $LabelLocation = "SB-3" ; break}
                    3 { $LabelLocation = "SB-2" ; break}
                    default  {break}
                }

            }
        }
        #--------------------------------------------------------------------------------------------------------------------------
        # Else Exchange Configuration
        #--------------------------------------------------------------------------------------------------------------------------
        Else
        {
            #--------------------------------------------------------------------------------------------------------------------------
            # If IDE interface then connected directory to PCH and SCSIBus is the same as SATA port # which is same as the label #
            #--------------------------------------------------------------------------------------------------------------------------
            If ($DiskInfo.InterfaceType -eq "IDE")
            {
                Return ("SB-{0}" -f $DiskInfo.SCSIBus)             
            }
            #--------------------------------------------------------------------------------------------------------------------------
            # If SCSI interface and have LSI disk info then lookup the label location based on enclosure/slot IDs
            #--------------------------------------------------------------------------------------------------------------------------
            ElseIf ($DiskInfo.InterfaceType -eq "SCSI")
            {                
                # Map enclosure/slot ID to labels - For Mt Rainier/LSI 9270 only
                #----------------------------------------------------------------
                If ($EnclosureId -eq 252)
                {
                    Switch($SlotId)
                    {
                        0 { $LabelLocation = "SB-2" ; break}
                        1 { $LabelLocation = "SB-3" ; break}
                        2 { $LabelLocation = "SB-4" ; break}
                        3 { $LabelLocation = "SB-5" ; break}
                        default  {break}
                    }

                }
                ElseIf (($EnclosureId -ne 0) -and ($EnclosureId -ne $WCS_NOT_AVAILABLE))
                {
                    $LabelLocation = ("DB-{0}" -f $SlotId) 
                }    
            }
        }        
        Return $LabelLocation        

    }
    Catch
    {
        Return $LabelLocation
    }
}
#-----------------------------------------------------------------------------------------------------------------------
# Helper function that gets the base FRU inforamtion
#-----------------------------------------------------------------------------------------------------------------------
Function DefinedSystem_GetFruInformation()
{   
    $Model = (Get-WmiObject Win32_ComputerSystem).Model # Either C1000 or C1020

    If ($Model -eq 'C1020')
    {
        Write-Output (Get-WcsFru -File 'Fru_v0.03'  -LogDirectory "$WCS_REF_DIRECTORY\FruTemplates" )
    } 
    Else
    {
        Write-Output (Get-WcsFru -File 'Fru_v0.02'  -LogDirectory "$WCS_REF_DIRECTORY\FruTemplates" )
    }

}
