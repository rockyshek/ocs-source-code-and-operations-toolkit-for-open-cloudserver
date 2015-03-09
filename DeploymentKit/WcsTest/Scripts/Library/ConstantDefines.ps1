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
Set-Variable  -Name WCS_EXCHANGE_RULES                       -Value $False  -Option ReadOnly -Force
Set-Variable  -Name WCS_DEFAULT_RECIPE                       -Value ([string] 'Azure')  -Option ReadOnly -Force

#----------------------------------------------------------------------------------------------
#  Define global readonly variables for all scripts
#
#  Prefix with WCS_ to avoid collisiions.  
#  Set to ReadOnly instead of Constant so can reload scripts
#---------------------------------------------------------------------------------------------- 
Set-Variable  -Name WCS_UPDATE_VERSION_KEY                   -Value  '$WCS_UPDATE_VERSION'        -Option ReadOnly -Force
Set-Variable  -Name WCS_UPDATE_SCRIPTFILE                    -Value  'WcsUpdate.ps1'              -Option ReadOnly -Force
  
Set-Variable  -Name WCS_SET_EXECUTION_TEMPFILE               -Value  "\WcsTest\EP.txt"                       -Option ReadOnly -Force
Set-Variable  -Name WCS_SCRIPT_DIRECTORY                     -Value  "$WCS_BASE_DIRECTORY\Scripts"           -Option ReadOnly -Force
Set-Variable  -Name WCS_SCRIPT_FILE_DIRECTORY                -Value  "$WCS_BASE_DIRECTORY\Scripts\Library"   -Option ReadOnly -Force
Set-Variable  -Name WCS_BINARY_DIRECTORY                     -Value  "$WCS_BASE_DIRECTORY\Scripts\Binaries"  -Option ReadOnly -Force
Set-Variable  -Name WCS_RESULTS_DIRECTORY                    -Value  "$WCS_BASE_DIRECTORY\Results"           -Option ReadOnly -Force
Set-Variable  -Name WCS_REMOTE_RESULTS_DIRECTORY             -Value  "$WCS_BASE_DIRECTORY\RemoteFiles"       -Option ReadOnly -Force
Set-Variable  -Name WCS_REF_DIRECTORY                        -Value  "$WCS_BASE_DIRECTORY\Scripts\References"        -Option ReadOnly -Force
Set-Variable  -Name WCS_DOC_DIRECTORY                        -Value  "$WCS_BASE_DIRECTORY\Scripts\References\Documentation"        -Option ReadOnly -Force
Set-Variable  -Name WCS_UPDATE_DIRECTORY                     -Value  "$WCS_BASE_DIRECTORY\Scripts\Updates"           -Option ReadOnly -Force
Set-Variable  -Name WCS_CONFIGURATION_DIRECTORY              -Value  "$WCS_BASE_DIRECTORY\Configurations"    -Option ReadOnly -Force
 
Set-Variable  -Name WCS_OS_STARTUP_DIRECTORY                 -Value   "\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp"    -Option ReadOnly -Force

Set-Variable  -Name WCS_COMPUTER_NAME                        -Value (Get-WmiObject Win32_ComputerSystem).Name  -Option ReadOnly -Force

Set-Variable  -Name WCS_MILLISECONDS_IN_SECONDS              -Value ([int] 1000)  -Option ReadOnly -Force

Set-Variable  -Name WCS_BYTES_IN_MB                          -Value ([int] 1000*1000)  -Option ReadOnly -Force
Set-Variable  -Name WCS_BYTES_IN_GB                          -Value ([int] 1000*1000*1000)  -Option ReadOnly -Force
Set-Variable  -Name WCS_BYTES_IN_TB                          -Value ([int] 1000*1000*1000*1000)  -Option ReadOnly -Force

Set-Variable  -Name WCS_BYTES_IN_MIB                         -Value ([int] 1024*1024)  -Option ReadOnly -Force
Set-Variable  -Name WCS_BYTES_IN_GIB                         -Value ([int] 1024*1024*1024)  -Option ReadOnly -Force
Set-Variable  -Name WCS_BYTES_IN_TIB                         -Value ([int] 1024*1024*1024*1024)  -Option ReadOnly -Force

Set-Variable  -Name WCS_BLADES_PER_CHASSIS                   -Value ([int] 24)  -Option ReadOnly -Force

Set-Variable  -Name WCS_TEST_PASS_SIGNATURE                  -Value "TEST PASSED!! (SIGNATURE) TEST PASSED!!"  -Option ReadOnly -Force

Set-Variable  -Name WCS_DISK_SPEED_BINARY                    -Value "$WCS_BINARY_DIRECTORY\Diskspd\DiskSpd.exe"  -Option ReadOnly -Force
Set-Variable  -Name WCS_PSEXEC64_BINARY                      -Value "$WCS_BINARY_DIRECTORY\psexec.exe"           -Option ReadOnly -Force

Set-Variable  -Name WCS_POWERSHELL_BINARY                    -Value "Powershell"  -Option ReadOnly -Force

Set-Variable  -Name WCS_SMARTCTL_BINARY                      -Value "$WCS_BINARY_DIRECTORY\SMART\SmartCtl.exe"   -Option ReadOnly -Force

Set-Variable  -Name WCS_PRIME95_BINARY                       -Value "$WCS_BINARY_DIRECTORY\Prime95\prime95.exe"  -Option ReadOnly -Force
Set-Variable  -Name WCS_PRIME95_CONFIG                       -Value "$WCS_BINARY_DIRECTORY\Prime95\prime.txt"  -Option ReadOnly -Force
Set-Variable  -Name WCS_PRIME95_LOCAL_CONFIG                 -Value "$WCS_BINARY_DIRECTORY\Prime95\local.txt"  -Option ReadOnly -Force
Set-Variable  -Name WCS_PRIME95_RESULTS_FILE                 -Value "$WCS_BINARY_DIRECTORY\Prime95\results.txt"  -Option ReadOnly -Force

Set-Variable  -Name WCS_IO_TEST_FILE                         -Value "$WCS_REF_DIRECTORY\DataFiles\iobw.tst"  -Option ReadOnly -Force
Set-Variable  -Name WCS_IOMETER_CONFIG                       -Value "$WCS_BINARY_DIRECTORY\iometer\stress.icf"  -Option ReadOnly -Force
Set-Variable  -Name WCS_IOMETER_RESULTS_FILE                 -Value "iometer_results.csv"
Set-Variable  -Name WCS_IOMETER_BINARY                       -Value "$WCS_BINARY_DIRECTORY\iometer\iometer.exe"  -Option ReadOnly -Force

Set-Variable  -Name WCS_QUICKSTRESS_RESULTS_FILE             -Value "QuickStress.log"  -Option ReadOnly -Force
Set-Variable  -Name WCS_CHASSIS_MANAGER_CONFIG_FILE_PATH	 -Value "C:\ChassisManager\Microsoft.GFS.WCS.ChassisManager.exe.config" -Option ReadOnly -Force

#-------------------------------------------------------------------------------------
# Define configuration constants
#-------------------------------------------------------------------------------------
Set-Variable  -Name WCS_CFG_RESULT           -Value  @{ NONE="NONE";FOUND="FOUND";MISSING="CONFIG MISMATCH - MISSING";SKIPPED="SKIPPED";MATCH="MATCH";MISMATCH="CONFIG MISMATCH - ";UNEXPECTED="CONFIG MISMATCH - UNEXPECTED"}  -Option ReadOnly -Force
Set-Variable  -Name WCS_CFG_COMPARE          -Value  @{ ALWAYS="Always ";NEVER="Never  "; ON_EXACT="OnExact"; PERCENT="Percent"}  -Option ReadOnly -Force
Set-Variable  -Name WCS_CFG_XML              -Value  @{ TRUE = "True" ; FALSE = "False"}  -Option ReadOnly -Force
Set-Variable  -Name WCS_CFG_DISPLAY          -Value  @{ TRUE = "True" ; FALSE = "False"}  -Option ReadOnly -Force

Set-Variable  -Name WCS_TYPE                                 -Value "WcsObject"  -Option ReadOnly -Force
Set-Variable  -Name WCS_TYPE_CHASSIS                         -Value "Chassis"  -Option ReadOnly -Force
Set-Variable  -Name WCS_TYPE_BLADE                           -Value "Blade"  -Option ReadOnly -Force


Set-Variable  -Name WCS_BLADE_OBJECT              -Value   @{   IP         = $WCS_NOT_AVAILABLE ;
                                       MAC        = $WCS_NOT_AVAILABLE ;
                                       Hostname   = $WCS_NOT_AVAILABLE ;
                                       AssetTag   = $WCS_NOT_AVAILABLE ;
                                       Slot       = $WCS_NOT_AVAILABLE ;
                                       RackId     = $WCS_NOT_AVAILABLE ;
                                       ChassisMac = $WCS_NOT_AVAILABLE ;
                                       State      = $WCS_NOT_AVAILABLE ;
                                       Type       = $WCS_NOT_AVAILABLE ;
                                       Drive      = $WCS_NOT_AVAILABLE ;
                                       ChassisId  = $WCS_NOT_AVAILABLE ;
                                       Error      = $null ;

                                       WcsObject  = $WCS_TYPE_BLADE 

                                  }   -Option ReadOnly -Force

Set-Variable  -Name WCS_CHASSISMANAGER_OBJECT  -Value        @{  IP           = $WCS_NOT_AVAILABLE ;
                                        MAC1         = $WCS_NOT_AVAILABLE ;
                                        MAC2         = $WCS_NOT_AVAILABLE ;
                                        ActiveMac    = $WCS_NOT_AVAILABLE ;
                                        Hostname     = $WCS_NOT_AVAILABLE ;
                                        AssetTag     = $WCS_NOT_AVAILABLE ;
                                        Service      = $WCS_NOT_AVAILABLE ;
                                        Position     = $WCS_NOT_AVAILABLE ;
                                        RackId       = $WCS_NOT_AVAILABLE ;
                                        SSL          = $WCS_NOT_AVAILABLE ;
                                        Info         = $WCS_NOT_AVAILABLE ;
                                        Health       = $WCS_NOT_AVAILABLE ;
                                        LogDirectory = $WCS_NOT_AVAILABLE ;
                                        Recipe       = $WCS_NOT_AVAILABLE ;
                                        Drive        = $WCS_NOT_AVAILABLE ;
                                        Error        = $null;

                                        WcsObject    = $WCS_TYPE_CHASSIS
                                     }   -Option ReadOnly -Force

Set-Variable  -Name WCS_COMPUTE_BLADE_FRU_OBJECT  -Value        @{ 

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
                                   }   -Option ReadOnly -Force  

#-------------------------------------------------------------------
#  Define PCI constants
#-------------------------------------------------------------------
Set-Variable  -Name LSI_VENDOR_ID          -Value  ([uint16] 0x1000)  -Option ReadOnly -Force
Set-Variable  -Name MELLANOX_VENDOR_ID     -Value  ([uint16] 0x15B3)  -Option ReadOnly -Force
Set-Variable  -Name INTEL_VENDOR_ID        -Value  ([uint16] 0x8086)  -Option ReadOnly -Force
Set-Variable  -Name FPGA_VENDOR_IDS        -Value  @("0x1414-0xB100","0x1414-0xB101","0x1A03-0x1150","0x8086-0x2F02","0x8086-0x2F04","0x8086-0x2F06","0x8086-0x2F08")  -Option ReadOnly -Force


