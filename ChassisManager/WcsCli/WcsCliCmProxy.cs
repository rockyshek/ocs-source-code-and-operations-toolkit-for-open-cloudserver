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
    using System;
    using System.Collections.Generic;

    internal static class WcsCliCmProxy
    {
        /// <summary>
        /// This dictionary object maps the command name to the command object
        /// Note that command objects for every command is created during initialization - CommandInitializer()
        /// </summary>
        static Dictionary<String, command> commandMap = new Dictionary<String, command>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Static constructor for initializing the command class objects
        /// </summary>
        static WcsCliCmProxy()
        {
            CommandInitializer();
        }

        /* Exposed methods */

        /// <summary>
        /// Get WCSCLI user commands, translate them to corresponding CM REST API calls 
        /// (via the command class), prints CM Response output
        /// Figure out if this is a local machine command or a CM service command
        /// Prompt for CM service credentials to talk to the CM if required
        /// Uses ConnectionManager class to establish WCF connection
        /// </summary>
        /// <param name="inputString"></param>
        internal static void InteractiveParseUserCommandGetCmResponse(bool isSerialClient, string inputString)
        {
            char[] cmdDelimiters = { ' ' }; 
            string[] inputSubString = null;
            command mappedCommand = null;

            try
            {
                string inputStringLower = inputString.ToLower();

                // Check if it is SetChassisManagerAssetInfo or SetPDBAssetInfo or SetBladeAssetInfo command
                if ((inputStringLower.Contains(WcsCliConstants.setCMAssetInfo) || inputStringLower.Contains(WcsCliConstants.setPDBAssetInfo) || inputStringLower.Contains(WcsCliConstants.setBladeAssetInfo)) 
                    && inputStringLower.Contains("-p") )
                {
                    // Parse the payload for set asset commands
                    inputSubString = ParseSetAssetInfo(inputString);
                   
                }
                else
                {
                    // Get all individual user-entered arguments as separate strings
                    // StringSplitOptions.RemoveEmptyEntries removes extra spaces 
                    inputSubString = inputString.Split(cmdDelimiters, StringSplitOptions.RemoveEmptyEntries);
                }
         
                // Prevents parsing more than maxArgCount arguments - DOS attack scenario
                if (inputSubString.Length > command.maxArgCount)
                {
                    Console.Write(WcsCliConstants.consoleString + " " + WcsCliConstants.invalidCommandString);
                    return;
                }

                // Command string should have at least two argument strings 
                if (inputSubString.Length <= 1)
                {
                    if (inputSubString.Length == 0)
                    {
                        return;
                    }
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                    return;
                }

                // The command string should start with "WcsCli"
                if (!inputSubString[0].Equals(WcsCliConstants.WcsCli, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                    return;
                }

                // The first argument which is the command name length must be smaller than indicated by command.maxArgLength
                if (inputSubString[1].Length > command.maxArgLength || inputSubString[1][0] != WcsCliConstants.argIndicatorVar)
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                    return;
                }

                // Map the user-entered command name to the corresponding command object created by CommandInitializer() function
                mappedCommand = new command();
                if (commandMap.TryGetValue(inputSubString[1].Remove(0, 1), out mappedCommand) != true)
                {
                    // Handle unknown command
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                    return;
                }
                mappedCommand.isSerialClient = isSerialClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            try
            {
                // (re)Allocate argVal for the mappedCommand - it has to get a new space for each time command is executed
                mappedCommand.argVal = new Dictionary<char, dynamic>();

                // Execute the commandImplementation() function corresponding to the command entered by the user
                // Command is executed only when the argument syntax is correct as checked by isArgSyntaxCorrect()
                // IsArgSyntaxCorrect() also extracts the argument indicator and value and adds them into mappedCommand.argVal
                if (IsArgSyntaxCorrect(mappedCommand, inputSubString))
                {
                    if (mappedCommand.argVal.ContainsKey('h'))
                    {
                        // If the command is establishCmConnection and it is a console client.. do not display help.. since the h option is used for hostname in the console.. 
                        if (!(mappedCommand.name.Equals(WcsCliConstants.establishCmConnection, StringComparison.InvariantCultureIgnoreCase) 
                            && isSerialClient == false))
                        {
                            Console.WriteLine(mappedCommand.helpString);    
                        }
                    }
                    else
                    {
                        // If this is a CM command, check if a connection has already been established, otherwise prompt for connection
                        if (mappedCommand.isCmServiceCommand)
                        {
                            if (!WcsCli2CmConnectionManager.IsCmServiceConnectionActive)
                            {
                                Console.WriteLine("Please connect to CM service using the \"{0}\" command and try again.", WcsCliConstants.establishCmConnection);
                                Console.WriteLine(WcsCliConstants.establishCmConnectionHelp);
                                return;
                            }
                        }

                        if (AreAllArgsPresent(mappedCommand))
                        {
                            // Execute the command
                            mappedCommand.commandImplementation();

                            // Let us allow batch only for the internal gethostportssloption command to avoid unintentional/intentional recursion
                            if (mappedCommand.name.Equals(WcsCliConstants.establishCmConnection, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // If the command has a batch parameter - process the commands in the batch file
                                if (mappedCommand.argVal.ContainsKey('b'))
                                {
                                    if (isSerialClient)
                                    {
                                        Console.WriteLine("The batch -b option is not supported in serial mode..");
                                        return;
                                    }
                                    dynamic batchFile = null;
                                    if (mappedCommand.argVal.TryGetValue('b', out batchFile))
                                    {
                                        uint batchInputFileLinesIndex = 0;
                                        string[] batchInputFileLines = System.IO.File.ReadAllLines((string)batchFile);
                                        while (batchInputFileLinesIndex < batchInputFileLines.Length)
                                        {
                                            try
                                            {
                                                // Read one command at a time
                                                inputString = batchInputFileLines[batchInputFileLinesIndex];
                                                batchInputFileLinesIndex++;
                                                if (inputString == null)
                                                    continue;

                                                // Recursive call for executing command in the batch file
                                                InteractiveParseUserCommandGetCmResponse(isSerialClient, inputString);
                                            }
                                            catch (Exception)
                                            {
                                                // skip this entry in the batch file
                                                Console.WriteLine("Error in parsing batch file. Skipped entries");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error in getting batch file name.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If the command is establishCmConnection and it is a console client.. do not display help.. since the h option is used for hostname in the console.. 
                            if (!(mappedCommand.name.Equals(WcsCliConstants.establishCmConnection, StringComparison.InvariantCultureIgnoreCase) &&
                                isSerialClient == false))
                            {
                                Console.WriteLine(WcsCliConstants.argsMissingString);
                                Console.WriteLine(mappedCommand.helpString);
                            }
                            else if(isSerialClient == false)
                            {
                                // Even for establishCmconnection command we need to display args missing message, 
                                // since 'h' option is used for hostname in console as mentioned above so not displaying comamnd help.
                                Console.WriteLine(WcsCliConstants.argsMissingString);
                            }
                        }
                    }
                }
                else
                {
                    // If the command is establishCmConnection and it is a console client.. do not display help.. since the h option is used for hostname in the console.. 
                    if (!(mappedCommand.name.Equals(WcsCliConstants.establishCmConnection, StringComparison.InvariantCultureIgnoreCase) && 
                        isSerialClient == false))
                    {
                        Console.WriteLine(WcsCliConstants.argsSyntaxIncorrect);
                        Console.WriteLine(mappedCommand.helpString);
                    }
                    else if(isSerialClient == false)
                    {
                        // Even for establishCmconnection command we need to display args missing message, 
                        // since 'h' option is used for hostname in console as mentioned above so not displaying comamnd help.
                        Console.WriteLine(WcsCliConstants.argsSyntaxIncorrect);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(WcsCliConstants.unknownError + ex.Message);
            }
        }

        /* Private member functions */
        /// <summary>
        /// Creates object for each individual command
        /// Corresponding command constructor initializes command specific parameters
        /// Also adds each command name and command object to command dictionary
        /// </summary>
        private static void CommandInitializer()
        {
        #region Chassis Infrastructure Commands
            GetServiceVersion getServiceVersionCmd = new GetServiceVersion();
            commandMap.Add(WcsCliConstants.getserviceversion, getServiceVersionCmd);

            // Get Chassis Info
            GetChassisInfo getChassisInfoCmd = new GetChassisInfo();
            commandMap.Add(WcsCliConstants.getChassisInfo, getChassisInfoCmd);

            GetChassisHealth getChassisHealthCmd = new GetChassisHealth();
            commandMap.Add(WcsCliConstants.getChassisHealth, getChassisHealthCmd);

            // Get Blade Info
            GetBladeInfo getBladeInfoCmd = new GetBladeInfo();
            commandMap.Add(WcsCliConstants.getBladeInfo, getBladeInfoCmd);

            GetBladeHealth getBladeHealthCmd = new GetBladeHealth();
            commandMap.Add(WcsCliConstants.getBladeHealth, getBladeHealthCmd);

            // PSU Firmware 
            UpdatePsuFirmware updatePsuFirmwareCmd = new UpdatePsuFirmware();
            commandMap.Add(WcsCliConstants.updatePsuFirmware, updatePsuFirmwareCmd);

            GetPsuFirmwareStatus getPsuFirmwareStatusCmd = new GetPsuFirmwareStatus();
            commandMap.Add(WcsCliConstants.getPsuFirmwareStatus, getPsuFirmwareStatusCmd);

        #endregion

        #region Blade Management Commands
            // Get Power State
            GetBladeHardPowerState getBladeHardPowerStateCmd = new GetBladeHardPowerState();
            commandMap.Add(WcsCliConstants.getBladeHardPowerState, getBladeHardPowerStateCmd);

            SetPowerOn poweronCmd = new SetPowerOn();
            commandMap.Add(WcsCliConstants.setPowerOn, poweronCmd);

            SetPowerOff poweroffCmd = new SetPowerOff();
            commandMap.Add(WcsCliConstants.setPowerOff, poweroffCmd);

            GetBladeSoftPowerState getBladeSoftPowerStateCmd = new GetBladeSoftPowerState();
            commandMap.Add(WcsCliConstants.getBladeSoftPowerState, getBladeSoftPowerStateCmd);

            SetBladeOn bladeOnCmd = new SetBladeOn();
            commandMap.Add(WcsCliConstants.setBladeOn, bladeOnCmd);

            SetBladeOff bladeOffCmd = new SetBladeOff();
            commandMap.Add(WcsCliConstants.setBladeOff, bladeOffCmd);

            // Blade Default Power On state
            SetBladeDefaultPowerState setBladeDefaultPowerStateCmd = new SetBladeDefaultPowerState();
            commandMap.Add(WcsCliConstants.setBladeDefaultPowerState, setBladeDefaultPowerStateCmd);

            GetBladeDefaultPowerState getBladeDefaultPowerStateCmd = new GetBladeDefaultPowerState();
            commandMap.Add(WcsCliConstants.getBladeDefaultPowerState, getBladeDefaultPowerStateCmd);

            SetBladeActivePowerCycle powercycleCmd = new SetBladeActivePowerCycle();
            commandMap.Add(WcsCliConstants.setBladeActivePowerCycle, powercycleCmd);

            GetNextBoot getNextBootCmd = new GetNextBoot();
            commandMap.Add(WcsCliConstants.getnextboot, getNextBootCmd);

            SetNextBoot setNextBootCmd = new SetNextBoot();
            commandMap.Add(WcsCliConstants.setnextboot, setNextBootCmd);

            // Blade Attention LED on
            SetBladeAttentionLEDOn setBladeAttentionLEDOnCmd = new SetBladeAttentionLEDOn();
            commandMap.Add(WcsCliConstants.setBladeAttentionLEDOn, setBladeAttentionLEDOnCmd);

            // Blade Attention LED off
            SetBladeAttentionLEDOff setBladeAttentionLEDOffCmd = new SetBladeAttentionLEDOff();
            commandMap.Add(WcsCliConstants.setBladeAttentionLEDOff, setBladeAttentionLEDOffCmd);

            // Read Blade Log
            ReadBladeLog readBladeLogCmd = new ReadBladeLog();
            commandMap.Add(WcsCliConstants.readBladeLog, readBladeLogCmd);

            // Clear Blade Log
            ClearBladeLog clearBladeLogCmd = new ClearBladeLog();
            commandMap.Add(WcsCliConstants.clearBladeLog, clearBladeLogCmd);

            getpowerreading getpowerreadingCmd = new getpowerreading();
            commandMap.Add(WcsCliConstants.getpowerreading, getpowerreadingCmd);

            getpowerlimit getpowerlimitCmd = new getpowerlimit();
            commandMap.Add(WcsCliConstants.getpowerlimit, getpowerlimitCmd);

            setpowerlimit setpowerlimitCmd = new setpowerlimit();
            commandMap.Add(WcsCliConstants.setpowerlimit, setpowerlimitCmd);

            SetBladeActivePowerLimitOn activatepowerlimitCmd = new SetBladeActivePowerLimitOn();
            commandMap.Add(WcsCliConstants.setBladePowerLimitOn, activatepowerlimitCmd);

            SetBladeActivePowerLimitOff deactivatepowerlimitCmd = new SetBladeActivePowerLimitOff();
            commandMap.Add(WcsCliConstants.setBladePowerLimitOff, deactivatepowerlimitCmd);

            SetDataSafeBladeOn dataSafeBladeOnCmd = new SetDataSafeBladeOn();
            commandMap.Add(WcsCliConstants.dataSafeBladeOn, dataSafeBladeOnCmd);
            
            SetDataSafePowerOn dataSafePowerOnCmd = new SetDataSafePowerOn();
            commandMap.Add(WcsCliConstants.dataSafePowerOn, dataSafePowerOnCmd);

            SetDataSafeBladeOff dataSafeBladeOffCmd = new SetDataSafeBladeOff();
            commandMap.Add(WcsCliConstants.dataSafeBladeOff, dataSafeBladeOffCmd);

            SetDataSafePowerOff dataSafePowerOffCmd = new SetDataSafePowerOff();
            commandMap.Add(WcsCliConstants.dataSafePowerOff, dataSafePowerOffCmd);
            
            SetDataSafeBladeActivePowerCycle dataSafePowerCycleCmd = new SetDataSafeBladeActivePowerCycle();
            commandMap.Add(WcsCliConstants.dataSafePowerCycle, dataSafePowerCycleCmd);

            GetBladeDataSafePowerState getBladeDataSafePowerStateCmd = new GetBladeDataSafePowerState();
            commandMap.Add(WcsCliConstants.getBladeDataSafePowerState, getBladeDataSafePowerStateCmd);

            GetBladeBiosPostCode getBiosPostCodeCmd = new GetBladeBiosPostCode();
            commandMap.Add(WcsCliConstants.getBladeBiosPostCode, getBiosPostCodeCmd);

            SetBladePsuAlertDefaultPowerCap setPsuAlertDpcCmd = new SetBladePsuAlertDefaultPowerCap();
            commandMap.Add(WcsCliConstants.setBladePsuAlertDpc, setPsuAlertDpcCmd);
            
            GetBladePsuAlertDpc getPsuAlertDpcCmd = new GetBladePsuAlertDpc();
            commandMap.Add(WcsCliConstants.getBladePsuAlertDpc, getPsuAlertDpcCmd);
            
            GetBladePsuAlert getPsuAlertCmd = new GetBladePsuAlert();
            commandMap.Add(WcsCliConstants.getBladePsuAlert, getPsuAlertCmd);

            ActivateDeactivateBladePsuAlert activateDeactivatePsuAlertCmd = new ActivateDeactivateBladePsuAlert();
            commandMap.Add(WcsCliConstants.activateDeactivateBladePsuAlert, activateDeactivatePsuAlertCmd);

            GetBladeAssetInfo getBladeAssetInfoCmd = new GetBladeAssetInfo();
            commandMap.Add(WcsCliConstants.getBladeAssetInfo, getBladeAssetInfoCmd);

            GetBladeMezzAssetInfo getBladeMezzAssetInfoCmd = new GetBladeMezzAssetInfo();
            commandMap.Add(WcsCliConstants.getBladeMezzAssetInfo, getBladeMezzAssetInfoCmd);
           
            SetBladeAssetInfo setBladeAssetInfoCmd = new SetBladeAssetInfo();
            commandMap.Add(WcsCliConstants.setBladeAssetInfo, setBladeAssetInfoCmd);

            GetBladeMezzPassThroughMode getBladeMezzPassThroughModeCmd = new GetBladeMezzPassThroughMode();
            commandMap.Add(WcsCliConstants.getBladeMezzPassThroughMode, getBladeMezzPassThroughModeCmd);

            SetBladeMezzPassThroughMode setBladeMezzPassThroughModeCmd = new SetBladeMezzPassThroughMode();
            commandMap.Add(WcsCliConstants.setBladeMezzPassThroughMode, setBladeMezzPassThroughModeCmd);
        #endregion

        #region Chassis Management Commands
            // Chassis Attention LED Status
            GetChassisAttentionLEDStatus getChassisAttentionLEDStatusCmd = new GetChassisAttentionLEDStatus();
            commandMap.Add(WcsCliConstants.getChassisAttentionLEDStatus, getChassisAttentionLEDStatusCmd);

            // Chassis Attention LED On
            SetChassisAttentionLEDOn setChassisAttentionLEDCmd = new SetChassisAttentionLEDOn();
            commandMap.Add(WcsCliConstants.setChassisAttentionLEDOn, setChassisAttentionLEDCmd);

            // Chassis Attention LED Off
            SetChassisAttentionLEDOff setChassisAttentionLEDOffCmd = new SetChassisAttentionLEDOff();
            commandMap.Add(WcsCliConstants.setChassisAttentionLEDOff, setChassisAttentionLEDOffCmd);

            ReadChassisLog readChassisLogCmd = new ReadChassisLog();
            commandMap.Add(WcsCliConstants.readChassisLog, readChassisLogCmd);

            ClearChassisLog clearChassisLogCmd = new ClearChassisLog();
            commandMap.Add(WcsCliConstants.clearChassisLog, clearChassisLogCmd);

            // Get AC socket power state
            GetACSocketPowerState getACSocketPowerStateCmd = new GetACSocketPowerState();
            commandMap.Add(WcsCliConstants.getACSocketPowerState, getACSocketPowerStateCmd);

            // Set AC socket power state On
            SetACSocketPowerStateOn setACSocketPowerStateOnCmd = new SetACSocketPowerStateOn();
            commandMap.Add(WcsCliConstants.setACSocketPowerStateOn, setACSocketPowerStateOnCmd);

            // Set AC socket power state Off
            SetACSocketPowerStateOff setACSocketPowerStateOffCmd = new SetACSocketPowerStateOff();
            commandMap.Add(WcsCliConstants.setACSocketPowerStateOff, setACSocketPowerStateOffCmd);

            GetChassisAssetInfo getChassisAssetInfoCmd = new GetChassisAssetInfo();
            commandMap.Add(WcsCliConstants.getCMAssetInfo, getChassisAssetInfoCmd);

            GetPDBAssetInfo getPDBAssetInfoCmd = new GetPDBAssetInfo();
            commandMap.Add(WcsCliConstants.getPDBAssetInfo, getPDBAssetInfoCmd);
            
            SetChassisManagerAssetInfo setCMAssetInfoCmd = new SetChassisManagerAssetInfo();
            commandMap.Add(WcsCliConstants.setCMAssetInfo, setCMAssetInfoCmd);
            
            SetPDBAssetInfo setPDBAssetInfoCmd = new SetPDBAssetInfo();
            commandMap.Add(WcsCliConstants.setPDBAssetInfo, setPDBAssetInfoCmd);
        #endregion

        #region Local Commands (WCSCLI Serial Mode Only)
            SetNetworkProperties setNetworkPropertiesCmd = new SetNetworkProperties();
            setNetworkPropertiesCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.setNetworkProperties, setNetworkPropertiesCmd);

            GetNetworkProperties getNetworkPropertiesCmd = new GetNetworkProperties();
            getNetworkPropertiesCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.getNetworkProperties, getNetworkPropertiesCmd);

            Clear clearCmd = new Clear();
            clearCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.clear, clearCmd);
        #endregion

        #region Chassis Manager Service Configuration Commands (WCSCLI Serial Mode Only)
            StartChassisManagerService startCMServiceCmd = new StartChassisManagerService();
            startCMServiceCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.startchassismanager, startCMServiceCmd);

            StopChassisManagerService stopCMServiceCmd = new StopChassisManagerService();
            stopCMServiceCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.stopchassismanager, stopCMServiceCmd);

            GetCMServiceStatus getCMServiceStatusCmd = new GetCMServiceStatus();
            getCMServiceStatusCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.getchassismanagerstatus, getCMServiceStatusCmd);

            EnableSSL enableSslCmd = new EnableSSL();
            enableSslCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.enablessl, enableSslCmd);

            DisableSSL disableSslCmd = new DisableSSL();
            disableSslCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.disablessl, disableSslCmd);
        #endregion

        #region User Management Commands
            adduser adduserCmd = new adduser();
            commandMap.Add(WcsCliConstants.adduser, adduserCmd);

            ChangeUserRole changeuserCmd = new ChangeUserRole();
            commandMap.Add(WcsCliConstants.changeuserrole, changeuserCmd);

            ChangeUserPassword changeUserPwd = new ChangeUserPassword();
            commandMap.Add(WcsCliConstants.changeuserpassword, changeUserPwd);

            removeuser removeuserCmd = new removeuser();
            commandMap.Add(WcsCliConstants.removeuser, removeuserCmd);
        #endregion

        #region Serial Session Commands
            startBladeSerialSession startBladeSerialSessionCmd = new startBladeSerialSession();
            commandMap.Add(WcsCliConstants.startBladeSerialSession, startBladeSerialSessionCmd);

            StopBladeSerialSession stopBladeSerialSessionCmd = new StopBladeSerialSession();
            commandMap.Add(WcsCliConstants.stopBladeSerialSession, stopBladeSerialSessionCmd);

            startPortSerialSession startPortSerialSessionCmd = new startPortSerialSession();
            commandMap.Add(WcsCliConstants.startPortSerialSession, startPortSerialSessionCmd);

            StopPortSerialSession stopPortSerialSessionCmd = new StopPortSerialSession();
            commandMap.Add(WcsCliConstants.stopPortSerialSession, stopPortSerialSessionCmd);

            EstablishConnectionToCm establishCmConnectionCmd = new EstablishConnectionToCm();
            establishCmConnectionCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.establishCmConnection, establishCmConnectionCmd);

            TerminateCmConnection terminateCmConnectionCmd = new TerminateCmConnection();
            terminateCmConnectionCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.terminateCmConnection, terminateCmConnectionCmd);
        #endregion

            help helpCmd = new help();
            helpCmd.isCmServiceCommand = false;
            commandMap.Add(WcsCliConstants.help, helpCmd);

            return;
        }

        /// <summary>
        /// Parsing the arguments for correct syntax 
        /// Also checks for argument parameter syntax type
        /// </summary>
        /// <param name="mappedCommand"> class object corresponding to user entered command </param>
        /// <param name="inputSubString"> represents all user-entered arguments and their values </param>
        /// <returns></returns>
        private static bool IsArgSyntaxCorrect(command mappedCommand, string[] inputSubString)
        {
            uint index = 2;
            bool isSyntaxCorrect = true;

            // Iterate over all arguments and check for correctness
            while (index < inputSubString.Length && inputSubString[index] != null)
            {
                isSyntaxCorrect = true;
                // All argument indicators start with a '-'
                if (inputSubString[index][0] != WcsCliConstants.argIndicatorVar)
                {
                    Console.WriteLine("Invalid Argument Indicator. Use '-' to indicate argument.");
                    isSyntaxCorrect = false;
                    break;
                }

                // Argument indicator must be '-' followed by a single character
                if (inputSubString[index].Length != 2)
                {
                    Console.Write("Argument Indicator missing. ");
                    isSyntaxCorrect = false;
                    break;
                }

                try
                {
                    // Extract the argument indicator character
                    // Convert the argument indicator to lower case as we look in the arg spec dictionary to find the key-value pair.
                    // All args in Arg spec are in lower case.
                    char charIndicator = Char.ToLower(inputSubString[index][1]);

                    Type argType;
                    // Find the type, 'argType' of the argument indicated by charIndicator
                    // The if block will be execute for correct argument Indicators 
                    if (mappedCommand.argSpec != null && mappedCommand.argSpec.TryGetValue(charIndicator, out argType))
                    {
                        // All arguments have either zero or one parameter 
                        // if argType is null for this indicator, then this argument has zero/no parameter 
                        if (argType == null)
                        {
                            if (!mappedCommand.argVal.ContainsKey(charIndicator))
                            {
                                mappedCommand.argVal.Add(charIndicator, null);
                            }
                            index++;
                            continue;
                        }
                        else // this else block is executed for arguments that carry a single parameter
                        {
                            index++;

                            if (index < inputSubString.Length && inputSubString[index] != null)
                            {
                                if (inputSubString[index].Length > command.maxArgLength)
                                {
                                    isSyntaxCorrect = false;
                                    break;
                                }

                                if (!mappedCommand.argVal.ContainsKey(charIndicator))
                                {
                                    // Convert the argument parameter to the corresponding type indicated by argType and add it to argVal
                                    mappedCommand.argVal.Add(charIndicator, Convert.ChangeType(inputSubString[index], argType));
                                }

                                // note that index is incremented twice in this block - one for the argument indicator and another for the argument parameter
                                index++;
                                continue;
                            }
                            else
                            {
                                Console.WriteLine("Required parameters missing.");
                                isSyntaxCorrect = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Argument Indicator.");
                        isSyntaxCorrect = false;
                        break;
                    }
                } //try block ends

                // Exception handling in case of convert failure - say string is entered in place of a interger argument
                // Exception must also be called if argVal.Add is done with duplicate entries
                catch (InvalidCastException ex)
                {
                    isSyntaxCorrect = false;
                    Console.WriteLine("Invalid argument type. " + ex.Message);
                    break;
                }
                catch (ArgumentException ex)
                {
                    isSyntaxCorrect = false;
                    Console.WriteLine("Invalid argument. " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    isSyntaxCorrect = false;
                    Console.WriteLine("Invalid argument. " + ex.Message);
                    break;
                }
            } // while loop ends

            return isSyntaxCorrect;
        }

        /// <summary>
        /// checks if all mandatory and conditionally optional arguments present
        /// </summary>
        /// <param name="myCommand"> class object corresponding to user entered command </param>
        /// <returns>true if user entered parameters adhere to the command specification</returns>
        private static bool AreAllArgsPresent(command myCommand)
        {
            bool found = true;

            // this should not happen
            if (myCommand == null)
                return false;

            // Nothing to check here
            if (myCommand.argVal == null || myCommand.conditionalOptionalArgs == null)
                return true;

            foreach (KeyValuePair<char, char[]> pair in myCommand.conditionalOptionalArgs)
            {
                found = false;
                if (myCommand.argVal.ContainsKey(pair.Key))
                {
                    found = true;
                }
                else if (pair.Value != null) // If there are other alternative args for this arg
                {
                    foreach (char optArg in pair.Value)
                    {
                        if (myCommand.argVal.ContainsKey(optArg))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found == false)
                    break;
            }

            return found;
        } // function areAllArgsPresent ends

        /// <summary>
        /// Parse Asset info command input payload
        /// TO DO : Move this to Regex parsing in Q3
        /// </summary>
        private static string[] ParseSetAssetInfo(string inputString)
        {
            string[] commandString = null;
            List<string> inputStringFragments = new List<string>();

            // split the input on dash (-)
            commandString = inputString.Split(new Char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in commandString)
            {
                // check if the string contains parameter 'p' and the payload
                if (s[0] == 'p')
                {
                    // add the parameter
                    inputStringFragments.Add("-p");

                    // check if there is a payload, if yes add it to list
                    // Please note we do not fail here if there is no payload specified as the calling method will take care of that.
                    if (s.Length > 2)
                    {
                        inputStringFragments.Add(s.Substring(2));
                    }
                }
                else
                {
                    // Split the string on space
                    string[] tempString = s.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // The first string will be the argument (-i, ..) other than 'wcscli'
                    // include a dash(-) if not wcscli and add to list
                    if (tempString[0] == WcsCliConstants.WcsCli)
                    {
                        inputStringFragments.Add(tempString[0]);
                    }
                    else
                    {
                        inputStringFragments.Add("-" + tempString[0]);
                    }

                    // Check if the argument is followed by a value, if yes add it to list.
                    // Please note we do not fail here if there is no value specified as the calling method will take care of that.
                    if (tempString.Length > 1)
                    {
                        inputStringFragments.Add(tempString[1]);
                    }
                }
            }

            // get the final array of strings
            return inputStringFragments.ToArray();
        }
    }
}
