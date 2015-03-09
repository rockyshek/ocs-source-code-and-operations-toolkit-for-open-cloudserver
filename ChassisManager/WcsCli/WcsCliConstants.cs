// Copyright (c) Microsoft Corporation
// All rights reserved. 
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

namespace Microsoft.GFS.WCS.WcsCli
{
    // Class for all constants 
    internal static class WcsCliConstants
    {
        #region Chassis Infrastructure Commands
        public const string getserviceversion = "getserviceversion";
        public const string getChassisInfo = "getchassisinfo";
        public const string getChassisHealth = "getchassishealth";
        public const string getBladeInfo = "getbladeinfo";
        public const string getBladeHealth = "getbladehealth";
        public const string updatePsuFirmware = "updatepsufw";
        public const string getPsuFirmwareStatus = "getpsufwstatus";
        #endregion

        #region Blade Management Commands
        public const string getBladeHardPowerState = "getpowerstate";
        public const string setPowerOn = "setpoweron";
        public const string setPowerOff = "setpoweroff";
        public const string getBladeSoftPowerState = "getbladestate";
        public const string setBladeOn = "setbladeon";
        public const string setBladeOff = "setbladeoff";
        public const string setBladeDefaultPowerState = "setbladedefaultpowerstate";
        public const string getBladeDefaultPowerState = "getbladedefaultpowerstate";
        public const string setBladeActivePowerCycle = "setbladeactivepowercycle";
        public const string getnextboot = "getnextboot";
        public const string setnextboot = "setnextboot";
        public const string setBladeAttentionLEDOn = "setbladeattentionledon";
        public const string setBladeAttentionLEDOff = "setbladeattentionledoff";
        public const string readBladeLog = "readbladelog";
        public const string clearBladeLog = "clearbladelog";
        public const string getpowerreading = "getbladepowerreading";
        public const string getpowerlimit = "getbladepowerlimit";
        public const string setpowerlimit = "setbladepowerlimit";
        public const string setBladePowerLimitOn = "setbladepowerlimiton";
        public const string setBladePowerLimitOff = "setbladepowerlimitoff";
        public const string dataSafeBladeOn = "setbladedatasafeon";
        public const string dataSafePowerOn = "setdatasafepoweron";
        public const string dataSafeBladeOff = "setbladedatasafeoff";
        public const string dataSafePowerOff = "setdatasafepoweroff";
        public const string dataSafePowerCycle = "setbladedatasafeactivepowercycle";
        public const string getBladeDataSafePowerState = "getbladedatasafepowerstate";
        public const string getBladeBiosPostCode = "getbladebiospostcode";
        public const string setBladePsuAlertDpc = "setbladepsualertdpc";
        public const string getBladePsuAlertDpc = "getbladepsualertdpc";
        public const string getBladePsuAlert = "getbladepsualert";
        public const string activateDeactivateBladePsuAlert = "activatedeactivatebladepsualert";
        public const string getBladeAssetInfo = "getbladeassetinfo";
        public const string getBladeMezzAssetInfo = "getBladeMezzAssetInfo";
        public const string setBladeAssetInfo = "setbladeassetinfo";
        public const string getBladeMezzPassThroughMode = "getblademezzpassthroughmode";
        public const string setBladeMezzPassThroughMode = "setblademezzpassthroughmode";
        #endregion

        #region Chassis Management Commands
        public const string getChassisAttentionLEDStatus = "getchassisattentionledstatus";
        public const string setChassisAttentionLEDOn = "setchassisattentionledon";
        public const string setChassisAttentionLEDOff = "setchassisattentionledoff";
        public const string readChassisLog = "readchassislog";
        public const string clearChassisLog = "clearchassislog";
        public const string getACSocketPowerState = "getacsocketpowerstate";
        public const string setACSocketPowerStateOn = "setacsocketpowerstateon";
        public const string setACSocketPowerStateOff = "setacsocketpowerstateoff";
        public const string getCMAssetInfo = "getchassismanagerassetinfo";
        public const string getPDBAssetInfo = "getpdbassetinfo";
        public const string setCMAssetInfo = "setchassismanagerassetinfo";
        public const string setPDBAssetInfo = "setpdbassetinfo";
        #endregion

        #region Local Commands (WCSCLI Serial Mode Only)
        public const string setNetworkProperties = "setnic";
        public const string getNetworkProperties = "getnic";
        public const string clear = "clear";
        #endregion

        #region Chassis Manager Service Configuration Commands (WCSCLI Serial Mode Only)
        public const string startchassismanager = "startchassismanager";
        public const string stopchassismanager = "stopchassismanager";
        public const string getchassismanagerstatus = "getchassismanagerstatus";
        public const string enablessl = "enablechassismanagerssl";
        public const string disablessl = "disablechassismanagerssl";
        #endregion

        #region User Management Commands
        public const string adduser = "adduser";
        public const string changeuserrole = "changeuserrole";
        public const string changeuserpassword = "changeuserpwd";
        public const string removeuser = "removeuser";
        #endregion

        #region Serial Session Commands
        public const string startBladeSerialSession = "startBladeSerialSession";
        public const string stopBladeSerialSession = "stopBladeSerialSession";
        public const string startPortSerialSession = "startPortSerialSession";
        public const string stopPortSerialSession = "stopPortSerialSession";
        public const string establishCmConnection = "establishCmConnection";
        public const string terminateCmConnection = "terminateCmConnection";
        #endregion

        #region Command Processing Constants
        public const string WcsCli = "wcscli";
        public const string help = "h";
        public const string consoleString = "WcsCli#";
        public const char argIndicatorVar = '-';
        public static readonly string invalidCommandString = commandFailure + " Error: Invalid Command.";
        public static readonly string argsMissingString = commandFailure + " Error: Required arguments missing.";
        public static readonly string argsSyntaxIncorrect = commandFailure + " Error: Incorrect argument syntax.";
        public static readonly string argsOutOfRange = commandFailure + " Error: Parameter out of range.";
        #endregion

        #region Command Responses
        public const string commandFailure = "Command Failed. ";
        public static readonly string decompressing = commandFailure + " Firmware Decompressing. ";
        public static readonly string defaultTimeout = " Try again in 30 seconds. ";
        public static readonly string commandComplete = " Command Completed. ";
        public static readonly string commandTimeout = commandFailure + "Response: Timeout, Blade/Switch is unreachable";
        public static readonly string commandUserAccountExists = commandFailure + "User account already exists";
        public static readonly string commandUserNotFound = commandFailure + "User not found";
        public static readonly string commandUserPwdDoesNotMeetReq = commandFailure + "User password does not meet system requirements";
        public static readonly string commandSerialSessionActive = commandFailure + "Serial Session Active";
        public static readonly string commandSuccess = "Command Success: ";
        public static readonly string dataFetchError = commandFailure + "Error in fetching data";
        public static readonly string unknownError = commandFailure + "Unknown Error. Error Detail: ";
        public static readonly string bladeStateUnknown = commandFailure + "Unreachable, it is turned OFF or not present";
        public static readonly string serviceResponseEmpty = commandFailure + "Response received from ChassisManager service is NULL/Empty";
        #endregion

        #region Output Headers
        public const string getChassisInfoComputeNodesHeader = "== Compute Nodes ==";
        public const string getChassisInfoPowerSuppliesHeader = "== Power Supplies ==";
        public const string getChassisInfoBatteriesHeader = "== Batteries ==";
        public const string getChassisInfoChassisControllerHeader = "== Chassis Controller ==";
        public const string getBladeInfoControllerHeader = "== Blade Controller Info ==";
        public const string fanHealthHeader = "== Fan Health ==";
        public const string bladeHeathHeader = "== Blade Health ==";
        public const string psuFwStatusHeader = "== PSU Firmware Status ==";
        public const string cpuInfo = "== CPU Information ==";
        public const string memoryInfo = "== Memory Information ==";
        public const string diskInfo = "== Disk information ==";
        public const string pcieInfo = "== PCIE Information ==";
        public const string sensorInfo = "== Sensor Information ==";
        public const string tempSensorInfo = "== Temp Sensor information ==";
        public const string fruInfo = "== FRU Information ==";
        public const string psuHealthHeader = "== PSU Health ==";
        public const string batteryHealthHeader = "== Battery Health ==";
        public const string macAddressesInfoHeader = "== MAC Address ==";
        public const string getBladeInfoComputeNodeHeader = "== Compute Node Info ==";
        public const string readChassisLogHeader = "== Chassis Controller Log ==";
        public const string readBladeLogHeader = "== Blade Controller Log ==";
        #endregion

        #region Misc constants used in commands
        public const uint powercycleOfftime = 0;  // Interval between power off and on for power cycling commands
        public const uint readBladeLogNumberOfEntries = 100;  // Number of blade log entries to read
        public const string BladeTypeCompute = "Server";
        public const string BladeTypeJBOD = "Jbod";      
        public const string SensorTypeTemp = "Temperature";
        #endregion

        #region Help strings

        public const string WcsCliHelp =
@"--------------------------------------------------------------------------------
Chassis infrastructure command line interface
--------------------------------------------------------------------------------

wcscli -getserviceversion              Get Chassis Manager service version.

wcscli -getchassisinfo                 Get information about
                                       blades, power supplies, batteries and
                                       Chassis Manager.

wcscli -getchassishealth               Get health status for blades, power
                                       supplies, batteries and fan

wcscli -getbladeinfo                   Get information about blade.

wcscli -getbladehealth                 Get health information for blade.
                                       Includes CPU info, Memory info,
                                       Disk info (JBOD only), 
                                       PCIE info, Sensor info and Fru Info.
                                       This information can be requested
                                       separately using command options.

wcscli -updatepsufw                    Update PSU firmware.

wcscli -getpsufwstatus                 Get PSU firmware revision and 
                                       update status.

--------------------------------------------------------------------------------
Blade management commands
--------------------------------------------------------------------------------

wcscli -getpowerstate                  Return the powered
                                       on/off state of blade.

wcscli -setpoweron                     Power on (Active power state) 
                                       blade.

wcscli -setpoweroff                    Power off (Active power state)
                                       blade.

wcscli -getbladestate                  Returns the blade soft power state.

wcscli -setbladeon                     Blade soft Power ON. 

wcscli -setbladeoff                    Blade soft Power OFF. 

wcscli -setbladedefaultpowerstate      Set the default power on
                                       state of a blade.

wcscli -getbladedefaultpowerstate      Get the default power on state of 
                                       a blade.

wcscli -setbladeactivepowercycle       Power cycle blade.

wcscli -getnextboot                    This command gets the pending boot order
                                       to be applied to the next time the blade
                                       boots.  Once the boot order is applied,
                                       GetNextBoot will return the default
                                       value of NoOverRide.

wcscli -setnextboot                    Set first boot order type for a blade.

wcscli -setbladeattentionledon         Turn on the blue ID LED
                                       on each blade.

wcscli -setbladeattentionledoff        Turn off the blue ID LED
                                       on each blade.

wcscli -readbladelog                   Read Blade log.

wcscli -clearbladelog                  Clear logs for a blade.

wcscli -getbladepowerreading           Get power reading for blade(s) in Watts.

wcscli -getbladepowerlimit             Get power limit for blade(s) in Watts.

wcscli -setbladepowerlimit             Set power limit for blade(s) in Watts.

wcscli -setbladepowerlimiton           Activate power limit for blade(s).

wcscli -setbladepowerlimitoff          Deactivate power limit for blade(s). 

wcscli -setbladedatasafeon             Datasafe blade soft Power ON. 

wcscli -setdatasafepoweron             Datasafe Power ON (Active power state)
                                       blade. 

wcscli -setbladedatasafeoff            Datasafe blade soft Power OFF. 

wcscli -setdatasafepoweroff            Datasafe Power OFF (Active power state)
                                       blade. 

wcscli -setbladedatasafeactivepowercycle Datasafe Power cycle blade.

wcscli -getbladedatasafepowerstate     Get blade DataSafe power state.

wcscli -getbladebiospostcode           Get blade BIOS post code.

wcscli -setbladepsualertdpc            Sets psu alert default power cap for 
                                       the blade.

wcscli -getbladepsualertdpc            Gets psu alert default power cap for 
                                       the blade.

wcscli -getbladepsualert               Gets psu alert for the blade.

wcscli -activatedeactivatebladepsualert  Activate/Deactivate blade Psu Alert.

wcscli -getbladeassetinfo               Gets Blade FRU areas information.

wcscli -getblademezzassetinfo           Gets Blade Tray Mezz FRU areas information.

wcscli -setbladeassetinfo               Writes to Blade FRU Multi Record Area.

wcscli -getblademezzpassthroughmode     Get Pass Through Mode from Blade Tray Mezz.

wcscli -setblademezzpassthroughmode     Set Pass Through Mode on Blade Tray Mezz.

--------------------------------------------------------------------------------
Chassis management commands
--------------------------------------------------------------------------------

wcscli -getchassisattentionledstatus   Get the status of the LED (on/off)
                                       on the front of the 
                                       Chassis Manager.

wcscli -setchassisattentionledon       Turn on the blue ID light
                                       on the front of the 
                                       Chassis Manager.

wcscli -setchassisattentionledoff      Turns off the blue ID light
                                       on the front of the 
                                       Chassis Manager.

wcscli -readchassislog                 Read Persistent Log
                                       from the Chassis Controller (with
                                       timestamp based querying).

wcscli -clearchassislog                Clear Persistent Log
                                       from the Chassis Controller.

wcscli -getacsocketpowerstate          Get the AC Socket current
                                       on/off state of the Chassis.

wcscli -setacsocketpowerstateon        Turn on the AC socket (TOR Switches)
                                       of the Chassis.

wcscli -setacsocketpowerstateoff       Turn off the AC socket (TOR Switches)
                                       of the Chassis.

wcscli -getchassismanagerassetinfo     Gets Chassis Manager FRU areas
                                       information.

wcscli -getpdbassetinfo                Gets PDB FRU areas information.

wcscli -setchassismanagerassetinfo     Writes to Chassis Manager FRU Multi 
                                       Record Area.

wcscli -setpdbassetinfo                Writes to PDB FRU Multi Record Area.


--------------------------------------------------------------------------------
Local Commands (Only available in WCSCLI Serial Mode):
--------------------------------------------------------------------------------

wcscli -getnic                         Get chassis network configuration.

wcscli -setnic                         Set chassis manager network properties.

wcscli -clear                          Clear user comamnd history, also clears
                                       the client window.

--------------------------------------------------------------------------------
Chassis Manager Service Configuration Commands
(Only available in WCSCLI Serial Mode):
--------------------------------------------------------------------------------

wcscli -startchassismanager            Start Chassis Manager service.
        
wcscli -stopchassismanager             Stop Chassis Manager service.
        
wcscli -getchassismanagerstatus        Get Chassis Manager service status.

wcscli -enablechassismanagerssl        Enables SSL for Chassis Manager service.

wcscli -disablechassismanagerssl       Disables SSL for Chassis Manager service. 

--------------------------------------------------------------------------------
User Management Commands

Please note: These commands will be deprecated in the future.
--------------------------------------------------------------------------------

wcscli -adduser                        Add chassis controller user.

wcscli -changeuserrole                 Change chassis controller user role.

wcscli -changeuserpwd                  Change chassis controller user password.

wcscli -removeuser                     Remove chassis controller user.

--------------------------------------------------------------------------------
Serial Session commands
--------------------------------------------------------------------------------

wcscli -startbladeserialsession        Start serial session to a blade.

wcscli -startportserialsession         Start serial session to devices connected
                                       to COM ports.

wcscli -stopbladeserialsession         Force kill any existing blade serial
                                       session for the given blade id.

wcscli -stopportserialsession          Force kill existing serial session on
                                       given port.

wcscli -establishCmConnection          Create a connection to the CM service.

wcscli -terminateCmConnection          Terminate a connection to the CM service.

";

        #region Chassis Infrastructure Commands Help
        public const string getServiceVersionHelp = @"
Usage: wcscli -getserviceversion [-h]
        -h - help; display the correct syntax
        ";

        public const string getChassisInfoHelp = @"
Usage: wcscli -getchassisinfo [-s] [-p] [-c] [-t] [-h]
        -s - show blade information
        -p - show power information
        -c - show chassis information
        -t - show battery information
        -h - help; display the correct syntax
        ";

        public const string getChassisHealthHelp = @"
Usage: wcscli -getchassishealth [-b] [-p] [-f] [-t] [-h]
        -b - show blade health
        -p - show Psu health
        -f - show fan health
        -t - show battery health
        -h - help; display the correct syntax
        ";

        public const string getbladeinfoHelp = @"
Usage: wcscli -getbladeinfo [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string getBladeHealthHelp = @"
Usage: wcscli -getbladehealth [-i <blade_index>] [-q] [-m] [-d] [-p] [-s] 
                              [-t] [-f] [-h]
        blade_index - the target blade number. Typically 1-24
        -q - Blade CPU Information
        -m - Blade Memory Information
        -d - JBOD Disk Information
        -p - Blade PCIE Information
        -s - Blade Sensor Information
        -t - Temperature Sensor Information
        -f - Blade Fru Information
        -h - help; display the correct syntax
        ";

        public const string updatePsuFwHelp = @"
Usage: wcscli -updatepsufw [-i <psuId>] [-f <filePath>] [-p <isPrimaryImage>] 
                           [-h]
        psuId - the target PSU number to update. Typically 1-6
        -f - path to firmware image file
        -p - 1: Firmware image file is for primary controller.
             0: Firmware image file is for secondary controller.
        -h - help; display the correct syntax
        ";

        public const string getPsuFwStatusHelp = @"
Usage: wcscli -getpsufwstatus [-i <psuId>] [-h]
        psuId - the target PSU number. Typically 1-6
        -h - help; display the correct syntax
        ";

        #endregion

        # region Blade Management Commands Help
        public const string getBladeHardPowerStateHelp = @"
Usage: wcscli -getpowerstate[-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setpoweronHelp = @"
Usage: wcscli -setpoweron [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setpoweroffHelp = @"        
Usage: wcscli -setpoweroff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades.
        -h - help; display the correct syntax
        ";

        public const string getBladeSoftPowerStateHelp = @"
Usage: wcscli -getbladestate[-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladeonHelp = @"
Usage: wcscli -setbladeon [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladeoffHelp = @"        
Usage: wcscli -setbladeoff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades.
        -h - help; display the correct syntax
        ";

        public const string setbladedefaultpowerstateHelp = @"
Usage: wcscli -setbladedefaultpowerstate [-i <blade_index>] -s <state>[-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -s - state: can be 0 (stay off) or 1 (power on)
        -h - help; display the correct syntax
        ";

        public const string getbladedefaultpowerstateHelp = @"
Usage: wcscli -getbladedefaultpowerstate [-i <blade_index>] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladeactivepowercycleHelp = @"
Usage: wcscli -setbladeactivepowercycle [-i <blade_index>] | [-a] 
                                        [-t <off_time>] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -t - the time interval in seconds for how long to wait before powering 
             the blade back on; this is an optional parameter; if not specified,
             the default interval is 0 seconds.
             Maximum value is 255 seconds.
        -h - help; display the correct syntax
        ";

        public const string getnextbootHelp = @"
Usage: wcscli -getnextboot [-i <blade_index>]
        blade_index - the target blade number. Typically 1-24
        ";

        public const string setnextbootHelp = @"
Usage: wcscli -setnextboot [-i <blade_index>]  [-t] <boot_type> [-m] <mode>  
                           [-p] <is_persistent> [-n] <boot_instance> [-h]
        blade_index - the target blade number. Typically 1-24
        -t - boot_type : 1. NoOverRide, 2. ForcePxe, 3. ForceDefaultHdd, 
                         4. ForceIntoBiosSetup, 5. ForceFloppyOrRemovable
        -m - 0 - legacy, 1 - uefi
        -p - is this a persistent setting (set value 1) or 
             one-time setting (set value 0)
        -n - instance number of the boot device. (Eg. 0 or 1 for NIC
             if there are two NICs)
        ";

        public const string setbladeattentionledonHelp = @"
Usage: wcscli -setbladeattentionledon [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladeattentionledoffHelp = @"
Usage: wcscli -setbladeattentionledoff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string readbladelogHelp = @"
Usage: wcscli -readbladelog [-i <blade_index>] [-n <entries_count>] [-h]
        blade_index - the target blade number. Typically 1-24
        -n - how many of the most recent entries to report;
             this is an optional parameter; 
        -h - help; display the correct syntax
        ";

        public const string clearbladelogHelp = @"
Usage: wcscli -clearbladelog [-i <blade_index>] [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string getbladepowerreadingHelp = @"
Usage: wcscli -getbladepowerreading [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - do for all blades
        -h - help; display the correct syntax
        ";

        public const string getbladebpowerlimitHelp = @"
Usage: wcscli -getbladepowerlimit [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladepowerlimitHelp = @"
Usage: wcscli -setbladepowerlimit [-i <blade_index> | -a] -l <power_limit> [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -l - power limit per blade in Watts
        -h - help; display the correct syntax
        ";

        public const string setbladepowerlimitOnHelp = @"
Usage: wcscli -setbladepowerlimiton [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string setbladepowerlimitoffHelp = @"
Usage: wcscli -setbladepowerlimitoff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string dataSafeSetBladeOnHelp = @"        
Usage: wcscli -setbladedatasafeon [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string dataSafeSetPowerOnHelp = @"        
Usage: wcscli -setdatasafepoweron [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string dataSafeSetBladeOffHelp = @"        
Usage: wcscli -setbladedatasafeoff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string dataSafeSetPoweroffHelp = @"        
Usage: wcscli -setdatasafepoweroff [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string dataSafeSetBladeActivePowerCycleHelp = @"
Usage: wcscli -setbladedatasafeactivepowercycle [-i <blade_index> | [-a] [-h]
       blade_index - the target blade number. Typically 1-24
       -a - all connected blades
       -h - help; display the correct syntax
        ";

        public const string getBladeDataSafePowerStateHelp = @"
Usage: wcscli -getbladedatasafepowerstate[-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string getBiosPostCodeHelp = @"
Usage: wcscli -getbladebiospostcode [-i <blade_index> [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string setPsuAlertDpcHelp = @"
Usage: wcscli -setbladepsualertdpc [[-i <blade_index> [-c <default_powercap>] 
                                   [-t <wait_time>]] | 
                                   [[-a]  [-c <default_powercap>] 
                                   [-t <wait_time>]] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -c - psu alert default power cap
        -t - the time interval in milliseconds for how long to wait before
             deasserting PROCHOT
        -h - help; display the correct syntax
        ";

        public const string getPsuAlertDpcHelp = @"
Usage: wcscli -getbladepsualertdpc [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
";
        public const string getPsuAlertHelp = @"
Usage: wcscli -getbladepsualert [-i <blade_index> | -a] [-h]
        blade_index - the target blade number. Typically 1-24
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string activateDeactivatePsuAlertHelp = @"
Usage: wcscli -activatedeactivatebladepsualert [[-i <blade_index> 
                                               [-c <enable_prochot>] 
                                               [-d <action>]
                                               [-r <remove_cap>]] | 
                                               [[-a] [-c <enable_prochot>] 
                                               [-d <action>] [-r <remove_cap>]] 
                                               [-h] 
        blade_index - the target blade number. Typically 1-24
        -c - enable/disable prochot
        -d - integer representing action to be taken
        -r - whether or not to remove power cap
        -a - all connected blades
        -h - help; display the correct syntax
        ";

        public const string getBladeAssetInfoHelp = @"
Usage: wcscli -getbladeassetinfo [-i <blade_index>] [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string getBladeMezzAssetInfoHelp = @"
Usage: wcscli -getBladeMezzAssetInfo [-i <blade_index>] [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string setBladeAssetInfoHelp = @"
Usage: wcscli -setbladeassetinfo [-i <blade_index>] -p <payload> [-h]
        blade_index - the target blade number. Typically 1-24
        -p - data to be written to blade FRU's Multi Record Area
        -h - help; display the correct syntax
        ";

        public const string getBladeMezzPassThroughModeHelp = @"
Usage: wcscli -getblademezzpassthroughmode [-i <blade_index>] [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string setBladeMezzPassThroughModeHelp = @"
Usage: wcscli -setblademezzpassthroughmode [-i <blade_index>] 
                                           [-m <pass_through_mode_enabled>]
                                           [-h]
        blade_index - the target blade number. Typically 1-24
        pass_through_mode_enabled - 'true' or 'false'
        -h - help; display the correct syntax
        ";
        #endregion

        # region Chassis Management Commands Help
        public const string getchassisattentionledstatusHelp = @"
Usage: wcscli -getchassisattentionledstatus [-h]
        Displays the status of the chassis LED (On/Off).
        -h - help; display the correct syntax
        ";

        public const string setchassisattentionledonHelp = @"
Usage: wcscli -setchassisattentionledon [-h]
        -h - help; display the correct syntax
        ";

        public const string setchassisattentionledoffHelp = @"
Usage: wcscli -setchassisattentionledoff [-h]
        -h - help; display the correct syntax
        ";

        public const string readchassislogHelp = @"
Usage: wcscli -readchassislog [-s <startDate>] [-e <endDate>] [-h]
        -s - Optional start date for the log entries (format: YYYY:MM:DD)
        -e - Optional end date for the log entries (format: YYYY:MM:DD)
        -h - help; display the correct syntax
        ";

        public const string clearchassislogHelp = @"
Usage: wcscli -clearchassislog [-h]
        -h - help; display the correct syntax
        ";

        public const string getacsocketpowerstateHelp = @"
Usage: wcscli -getacsocketpowerstate [-p <port_number>] | [-h]
        port_number - port number user wants to get i.e. 1, 2 or 3
        -h - help; display the correct syntax
        ";

        public const string setacsocketpowerstateonHelp = @"
Usage: wcscli -setacsocketpowerstateon [-p <port_number>] | [-h]
        port_number - port number user wants to turn on i.e. 1, 2 or 3
        -h - help; display the correct syntax
        ";

        public const string setacsocketpowerstateoffHelp = @"
Usage: wcscli -setacsocketpowerstateoff [-p <port_number>] | [-h]
        port_number - port number user wants to turn off i.e. 1, 2 or 3
        -h - help; display the correct syntax
        ";

        public const string getCMAssetInfoHelp = @"
Usage: wcscli �getchassismanagerassetinfo [-h] 
        -h - help; display the correct syntax 
        ";

        public const string getPDBAssetInfoHelp = @"
Usage: wcscli �getpdbassetinfo [-h]
        -h - help; display the correct syntax 
        ";

        public const string setCMAssetInfoHelp = @"
Usage: wcscli -setchassismanagerassetinfo -p <payload> [-h] 
        payload - data to be written to Chassis Manager FRU Multi Record Area
        -h - help; display the correct syntax
        ";

        public const string setPDBAssetInfoHelp = @"
Usage: wcscli -setpdbassetinfo -p <payload> [-h] 
        payload - data to be written to PDB FRU Multi Record Area
        -h - help; display the correct syntax
        ";
        #endregion
        
        # region Local Commands Help (WCSCLI Serial Mode Only)
        public const string getNetworkPropertiesHelp = @"

-------------------- (Only available in WCSCLI Serial Mode) -------------------- 

Usage: wcscli �getnic [-h]
        -h - help; display the correct syntax 
        Note that due to WMI caching, if new values are not configured 
        for the subnet mask, gateway IP, or DNS servers, the old values 
        will be shown. 
        ";

        public const string setNetworkPropertiesHelp = @"

-------------------- (Only available in WCSCLI Serial Mode) -------------------- 

Usage: wcscli -setnic [-a] <IP addr source DHCP/STATIC -Required!> 
                      [-i] <IP address (Required for Static IP)> 
                      [-m] <subnetmask (Required for Static IP)>
                      [-g] <gateway> [-p] <primary DNS> [-d] <secondary DNS> 
                      [-t] <network interface number> 
                      [-h]
        -a - IP addr source DHCP/Static
        -i - IP address of the chassis controller (Required for Static IP. 
             Not used for DHCP.)
        -m - subnet mask of the chassis controller (Required for Static IP. 
             Not used for DHCP.)
        -g - gateway of the chassis controller (Optional for Static IP. 
             Not used for DHCP.)
             Note that the gateway IP can be cleared by switching to 
             DHCP and then back to static IP.
        -p - primary DNS server address for the chassis controller (Optional)
        -d - secondary DNS server address for the chassis controller (Optional.
             Only valid if primary DNS is also specified.)
        -t - network interface controller number to configure 
             (0-index. Optional). Default to 0 if not specified
        -h - help; display the correct syntax 
        ";

        public const string clearHelp = @"

-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli �clear [-h]
        Clears user command history and clears display screen
        -h - help; display the correct syntax
         ";

        #endregion
        
        # region Chassis Manager Service Configuration Commands Help (WCSCLI Serial Mode Only)
        public const string startchassismanagerHelp = @"
-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli -startchassismanager [-h]
        -h - help; display the correct syntax
        ";

        public const string stopchassismanagerHelp = @"
-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli -stopchassismanager [-h]
        -h - help; display the correct syntax
        ";

        public const string getchassismanagerstatusHelp = @"
-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli -getchassismanagerstatus [-h]
        -h - help; display the correct syntax
        ";

        public const string enablesslHelp = @"
-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli �enablechassismanagerssl [-h]
        -h - help; display the correct syntax 
        ";

        public const string disablesslHelp = @"
-------------------- (Only available in WCSCLI Serial Mode) -------------------- 
Usage: wcscli �disablechassismanagerssl [-h]
        -h - help; display the correct syntax 
        ";
        #endregion
        
        # region User Management Commands Help
        public const string adduserHelp = @"
Usage: wcscli �adduser [�u <username>] [-p <password>] [-a|-o|-r]  [-h] 
        username - the username for the new user 
        password - the password for the new user. 
        Select one of the WCS Security role for the user (Mandatory):
        -a - Admin Role
        -o - Operator Role
        -r - User Role
        -h - help; display the correct syntax 
        ";

        public const string changeuserroleHelp = @"
Usage: wcscli �changeuserrole [�u <username>] [-a|-o|-r] [-h] 
        Select one of the following user roles:
        -a - Admin privilege
        -o - Operator privilege
        -r - User privilege
        -h - help; display the correct syntax 
        ";

        public const string changeUserPwdHelp = @"
Usage: wcscli �changeuserpwd [�u <username>] [-p <new password>]
        -u - Username
        -p - <new password> New password
        -h - help; display the correct syntax 
        ";

        public const string removeuserHelp = @"
Usage: wcscli �removeuser [�u <username>] [-h] 
        username - the username for the new user 
        -h - help; display the correct syntax 
        ";
        #endregion
        
        # region Serial Session Commands Help
        public const string startBladeSerialSessionHelp = @"
Usage: wcscli -startbladeserialsession [-i <blade_index>] 
                                       [-s <session_timeout_in_secs>] [-h]
        blade_index - the target blade number. Typically 1-24
        -s - Session timeout in secs. Maximum is 3600 seconds (60 minutes)
        -h - help; display the correct syntax
        ";

        public const string startPortSerialSessionHelp = @"
Usage: wcscli -startPortSerialSession [-i <Port_index>]
                                      [-d <device_timeout_in_millisecs>] 
                                      [-r <baud_rate>
           (75,110,300,1200,2400,4800,9600,19200,38400,57600,115200)] [-h]
        -i Port number. The number of the COM port the device is connected to.
       Enter 1 for COM1, 2 for COM2. 
        -d - Device timeout in millisecs
        -r - Baud rate
        -h - help; display the correct syntax
        ";

        public const string stopBladeSerialSessionHelp = @"
Usage: wcscli -stopbladeserialsession [-i <blade_index>]
                                      [-h]
        blade_index - the target blade number. Typically 1-24
        -h - help; display the correct syntax
        ";

        public const string stopPortSerialSessionHelp = @"
Usage: wcscli -stopportserialsession -i <port_no> [-h]
        -i - terminate all active sessions on given port.
        -h - help; display the correct syntax
        ";
        
        public const string establishCmConnectionHelp = @"
Usage: wcscli -establishCmConnection -m <host_name> -p <port> -s <SSL_option> 
                                    -u <username> -x <password> 
                                    [-b] <batchfileName>
        -m - host_name - Specify Host name for Chassis manager 
             (for serial connection, localhost is assumed)
        -p - port - Specify a valid Port to connect to for Chassis Manager 
             (default is 8000)
        -s - Select Chassis Manager (CM)'s SSL Encryption mode 
             (enabled/disabled?) 
             Enter 0 if CM is not configured to use SSL encrytion 
             (SSL disabled in CM)
            Enter 1 if CM requires SSl Encryption (SSL enabled in CM) 
        -u - & -x specify user credentials -- username and password -- 
             to connect to CM service
        -b - Optional batch file option (not supported in serial mode).
        -v - Get CLI version information
        -h - help
        ";

        public const string terminateCmConnectionHelp = @"
Usage: wcscli -terminateCmConnection [-h]
        -h - help
        ";
        #endregion

        public const string wcscliConsoleParameterHelp = @"
Usage: wcscli.exe -h <host_name> -p <port> -s <SSL_option> -u <username> 
                  -x <password> [-b] <batchfileName>
        -h - host_name - Specify Host name for Chassis manager 
            (for serial connection, localhost is assumed)
        -p - port - Specify a valid Port to connect to for Chassis Manager 
            (default is 8000)
        -s - Select Chassis Manager (CM)'s SSL Encryption mode 
             (enabled/disabled?) 
             Enter 0 if CM is not configured to use SSL encrytion 
             (SSL disabled in CM)
            Enter 1 if CM requires SSl Encryption (SSL enabled in CM)
        -u & -x - specify user credentials -- username and password -- 
             to connect to CM service.
        -b - Optional batch file option.
        -v - Get CLI version information
        -h - help
        ";        
        #endregion

    }
}
