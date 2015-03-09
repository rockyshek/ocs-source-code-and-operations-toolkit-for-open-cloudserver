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
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;
    using Microsoft.GFS.WCS.Contracts;
    using System.Net.NetworkInformation;
    using System.Net;
    using System.Management;
    using System.Net.Sockets;
    using System.ServiceProcess;
    using System.Text;

    /// <summary>
    /// Parent generic command class
    /// </summary>
    internal class command
    {
        internal string name; // Command name

        // Specification for the command argument/values. 
        // Includes a character to indicate the argument and Type of the expected parameter values
        internal Dictionary<char, Type> argSpec;

        internal Dictionary<char, dynamic> argVal; // Actual user entered command arguments and values
        internal Dictionary<char, char[]> conditionalOptionalArgs; // Set of arg indicators among which at least one has to be specified
        internal static uint maxArgCount = 64; // hardcoded maximum number of arguments - argument indicator and the value are counted as two
        internal static uint maxArgLength = 256; // hardcoded maximum length of arguments parameters and command  
        internal string helpString;

        // Indicates whether the client is serial or console - will be provided at runtime by Program class
        internal bool isSerialClient = false;

        // Indicates whether this command is a chassis manager command (true) or a local command (false). 
        // This needs to be statically populated in commandInitializer in WcsCliCmProxy.cs 
        internal bool isCmServiceCommand = true;

        // virtual command specific implementation function - will get overrided by inherited individual commands
        internal virtual void commandImplementation()
        {
        }

        // constructor of the parent command class
        internal command()
        {
            this.argSpec = new Dictionary<char, Type>();
            this.argVal = new Dictionary<char, dynamic>();
            this.conditionalOptionalArgs = new Dictionary<char, char[]>();
            this.helpString = "";
        }

        /// <summary>
        /// Prints the tab separated strings.
        /// </summary>
        /// <param name="inputStrings">The input strings.</param>
        internal static void printTabSeparatedStrings(List<String> inputStrings)
        {
            for (int myIndex = 0; myIndex < inputStrings.Count; myIndex++)
            {
                if (myIndex != 0)
                {
                    Console.Write(" ");
                }
                Console.Write(inputStrings[myIndex]);
                Console.Write("\t");
                if (myIndex < inputStrings.Count - 1)
                {
                    Console.Write("|");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// This method displays error message based on the completion code returned by user.
        /// </summary>
        /// <param name="completionCode">API completion code</param>
        protected void printErrorMessage(Contracts.CompletionCode completionCode, ChassisResponse response)
        {
            if (completionCode == Contracts.CompletionCode.Failure)
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
                return;
            }
            if (completionCode == Contracts.CompletionCode.Timeout)
            {
                Console.WriteLine(WcsCliConstants.commandTimeout);
                return;
            }
            if (completionCode == Contracts.CompletionCode.FirmwareDecompressing)
            {
                Console.WriteLine(WcsCliConstants.decompressing + (string.IsNullOrEmpty(response.statusDescription) ? WcsCliConstants.defaultTimeout : response.statusDescription));
                return;
            }
            if (completionCode != Contracts.CompletionCode.Success)
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " Completion code: {0}", completionCode);
                return;
            }
        }
    }

    /// <summary>
    /// GetChassisInfo command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class GetChassisInfo : command
    {
        internal GetChassisInfo()
        {
            this.name = WcsCliConstants.getChassisInfo;
            this.argSpec.Add('s', null);
            this.argSpec.Add('p', null);
            this.argSpec.Add('c', null);
            this.argSpec.Add('t', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getChassisInfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            bool noArgSpecified = false;
            if (!(this.argVal.ContainsKey('c') || this.argVal.ContainsKey('s') || this.argVal.ContainsKey('p') || this.argVal.ContainsKey('t')))
            {
                noArgSpecified = true;
            }
            Contracts.ChassisInfoResponse myPacket = new Contracts.ChassisInfoResponse();
            try
            {
                if (noArgSpecified)
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetChassisInfo(true, true, true, true);
                }
                else
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetChassisInfo(this.argVal.ContainsKey('s'), this.argVal.ContainsKey('p'),
                                                                                 this.argVal.ContainsKey('c'), this.argVal.ContainsKey('t'));
                }
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (myPacket == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }
            if (myPacket.completionCode == Contracts.CompletionCode.Failure)
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
                return;
            }
            if (myPacket.completionCode == Contracts.CompletionCode.Timeout)
            {
                Console.WriteLine(WcsCliConstants.commandTimeout);
                return;
            }
            if (myPacket.completionCode != Contracts.CompletionCode.Success)
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " Completion code: {0}", myPacket.completionCode);
                return;
            }
            // Display output 
            List<string> myStrings = new List<string>();
            Console.WriteLine(WcsCliConstants.commandSuccess);
            if (noArgSpecified == true || this.argVal.ContainsKey('s'))
            {
                // bladeCollections output
                if (myPacket.bladeCollections == null)
                {
                    Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                    return;
                }
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.getChassisInfoComputeNodesHeader);
                myStrings.Add("#"); myStrings.Add("Name\t"); myStrings.Add("GUID\t\t\t\t"); myStrings.Add("State");
                myStrings.Add("BMC MAC\t\t\t\t\t\t\t\t\t\t"); myStrings.Add("Completion Code");
                printTabSeparatedStrings(myStrings);

                foreach (BladeInfo gs in myPacket.bladeCollections)
                {
                    myStrings.RemoveAll(item => (1 == 1));
                    if (gs != null)
                    {
                        myStrings.Add(gs.bladeNumber.ToString());

                        if (gs.bladeName != null)
                        {
                            if (gs.bladeName.ToString().Length < 6)
                            {
                                myStrings.Add(gs.bladeName.ToString() + " ");
                            }
                            else
                            {
                                myStrings.Add(gs.bladeName.ToString());
                            }
                        }
                        else
                        {
                            myStrings.Add("");
                        }

                        if (gs.bladeGuid != null)
                        {
                            myStrings.Add(gs.bladeGuid.ToString());
                        }
                        else
                        {
                            myStrings.Add("");
                        }

                        if (gs.powerState == Contracts.PowerState.ON)
                        {
                            myStrings.Add("On");
                        }
                        else if (gs.powerState == Contracts.PowerState.OFF)
                        {
                            myStrings.Add("Off");
                        }
                        else if (gs.powerState == Contracts.PowerState.OnFwDecompress)
                        {
                            myStrings.Add("On");
                        }
                        else
                        {
                            myStrings.Add("--");
                        }

                        if (gs.bladeMacAddress != null)
                        {
                            if (gs.bladeMacAddress.Count == 1)
                            {
                                myStrings.Add("--\t\t\t\t\t");
                                myStrings.Add("--\t\t\t\t\t");
                            }
                            else
                            {
                                foreach (NicInfo info in gs.bladeMacAddress)
                                {
                                    if (!string.IsNullOrEmpty(info.macAddress))
                                    {
                                        myStrings.Add("DeviceID: " + info.deviceId + " MAC Address: " + info.macAddress);
                                    }
                                    else
                                    {
                                        myStrings.Add("--\t\t\t\t\t");
                                    }
                                }
                            }
                        }
                        else
                        {
                            myStrings.Add("");
                        }

                        myStrings.Add(gs.completionCode.ToString());

                    }
                    printTabSeparatedStrings(myStrings);
                }
            }

            if (noArgSpecified == true || this.argVal.ContainsKey('p'))
            {
                // psuCollections output
                if (myPacket.psuCollections == null)
                {
                    Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                    return;
                }
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.getChassisInfoPowerSuppliesHeader);
                myStrings.RemoveAll(item => (1 == 1));
                myStrings.Add("#"); myStrings.Add("Serial Num\t\t\t\t"); myStrings.Add("State"); myStrings.Add("Pout (W)");
                myStrings.Add("Completion Code");
                printTabSeparatedStrings(myStrings);

                foreach (PsuInfo gp in myPacket.psuCollections)
                {
                    myStrings.RemoveAll(item => (1 == 1));
                    if (gp != null)
                    {
                        myStrings.Add(gp.id.ToString());

                        if (gp.serialNumber != null)
                        {
                            if (!string.IsNullOrEmpty(gp.serialNumber))
                            {
                                myStrings.Add(gp.serialNumber.ToString());
                            }
                            else
                            {
                                myStrings.Add("\t\t\t\t\t");
                            }
                        }
                        else
                        {
                            myStrings.Add("");
                        }

                        if (gp.serialNumber != null)
                        {
                            if (gp.state == Contracts.PowerState.ON)
                            {
                                myStrings.Add("On");
                            }
                            else if (gp.state == Contracts.PowerState.OFF)
                            {
                                myStrings.Add("Off");
                            }
                            else
                            {
                                myStrings.Add("NA");
                            }
                        }
                        else
                        {
                            myStrings.Add("");
                        }

                        myStrings.Add(gp.powerOut.ToString() + '\t');

                        myStrings.Add(gp.completionCode.ToString());
                    }
                    printTabSeparatedStrings(myStrings);
                }
            }

            // Print battery info
            if (noArgSpecified == true || this.argVal.ContainsKey('t'))
            {
                // batteryCollections output
                if (myPacket.batteryCollections == null)
                {
                    Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                    return;
                }
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.getChassisInfoBatteriesHeader);
                myStrings.RemoveAll(item => (1 == 1));
                myStrings.Add("#"); myStrings.Add("Present"); myStrings.Add("Pout (W)"); myStrings.Add("Charge (%)"); myStrings.Add("Faulty");
                myStrings.Add("Completion Code");
                printTabSeparatedStrings(myStrings);

                foreach (BatteryInfo batteryInfo in myPacket.batteryCollections)
                {
                    myStrings.RemoveAll(item => (1 == 1));
                    if (batteryInfo != null)
                    {
                        myStrings.Add(batteryInfo.id.ToString());
                        myStrings.Add(batteryInfo.presence.ToString() + '\t');
                        myStrings.Add(batteryInfo.batteryPowerOutput.ToString() + '\t');
                        myStrings.Add(batteryInfo.batteryChargeLevel.ToString() + '\t');
                        myStrings.Add(batteryInfo.faultDetected.ToString() + '\t');
                        myStrings.Add(batteryInfo.completionCode.ToString());
                    }
                    printTabSeparatedStrings(myStrings);
                }
            }

            if (noArgSpecified == true || this.argVal.ContainsKey('c'))
            {
                // Chassis Manager
                if (myPacket.chassisController == null)
                {
                    Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                    return;
                }
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.getChassisInfoChassisControllerHeader);
                Console.WriteLine("Firmware Version\t: " + ((myPacket.chassisController.firmwareVersion != null) ? myPacket.chassisController.firmwareVersion : ""));
                Console.WriteLine("Hardware Version\t: " + ((myPacket.chassisController.hardwareVersion != null) ? myPacket.chassisController.hardwareVersion : ""));
                Console.WriteLine("Software Version\t: " + ((myPacket.chassisController.softwareVersion != null) ? myPacket.chassisController.softwareVersion : ""));
                Console.WriteLine("Serial Number\t\t: " + ((myPacket.chassisController.serialNumber != null) ? myPacket.chassisController.serialNumber : ""));
                Console.WriteLine("Asset Tag\t\t: " + ((myPacket.chassisController.assetTag != null) ? myPacket.chassisController.assetTag : ""));
                if (myPacket.chassisController.systemUptime != null)
                {
                    Console.WriteLine("System Uptime\t\t: " + myPacket.chassisController.systemUptime);
                }
                else
                {
                    Console.WriteLine("System Uptime\t\t: " + " ");
                }
                Console.WriteLine();

                if (myPacket.chassisController.networkProperties != null)
                {
                    for (int i = 0; i < myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection.Count; i++)
                    {
                        Console.WriteLine("N/W Interface {0}:", i);
                        Console.WriteLine("\tIP Address\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].ipAddress);
                        Console.WriteLine("\tMAC Address\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].macAddress);
                        Console.WriteLine("\tDHCP Server\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].dhcpServer);
                        Console.WriteLine("\tDHCP Enabled\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].dhcpEnabled.ToString());
                        Console.WriteLine("\tDNS Address\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].dnsAddress);
                        Console.WriteLine("\tDNS Domain\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].dnsDomain);
                        Console.WriteLine("\tDNS Hostname\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].dnsHostName);
                        Console.WriteLine("\tGateway Address\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].gatewayAddress);
                        Console.WriteLine("\tSubnet Mask\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].subnetMask);
                        Console.WriteLine("\tCompletion Code\t\t: " + myPacket.chassisController.networkProperties.chassisNetworkPropertyCollection[i].completionCode);
                    }
                }
                Console.WriteLine("Completion Code\t: " + myPacket.chassisController.completionCode.ToString());
            }
        }
    }

    /// <summary>
    /// GetBladeInfo command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class GetBladeInfo : command
    {
        internal GetBladeInfo()
        {
            this.name = WcsCliConstants.getBladeInfo;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });

            this.helpString = WcsCliConstants.getbladeinfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeInfoResponse myPacket = new BladeInfoResponse();
            GetAllBladesInfoResponse myPackets = new GetAllBladesInfoResponse();

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myPackets = WcsCli2CmConnectionManager.channel.GetAllBladesInfo();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    myPacket = WcsCli2CmConnectionManager.channel.GetBladeInfo((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myPackets == null) || (myPacket == null))
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myPackets.bladeInfoResponseCollection.Count(); index++)
                {
                    Console.WriteLine("======================= Blade {0} ========================", myPackets.bladeInfoResponseCollection[index].bladeNumber);
                    printGetBladeInfoPacket(myPackets.bladeInfoResponseCollection[index]);
                }
            }
            else
            {
                printGetBladeInfoPacket(myPacket);
            }
        }

        /// <summary>
        /// Prints the getBladeInfo packet.
        /// </summary>
        /// <param name="myPacket">My packet.</param>
        void printGetBladeInfoPacket(BladeInfoResponse myPacket)
        {
            if (myPacket.completionCode == Contracts.CompletionCode.Failure)
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
            }
            else if (myPacket.completionCode == Contracts.CompletionCode.Timeout)
            {
                Console.WriteLine(WcsCliConstants.commandTimeout);
            }
            else if (myPacket.completionCode == Contracts.CompletionCode.Unknown)
            {
                Console.WriteLine(WcsCliConstants.bladeStateUnknown);
            }
            else if (myPacket.completionCode == Contracts.CompletionCode.FirmwareDecompressing)
            {
                Console.WriteLine(WcsCliConstants.decompressing + (string.IsNullOrEmpty(myPacket.statusDescription) ? WcsCliConstants.defaultTimeout : myPacket.statusDescription));
            }
            else if (myPacket.completionCode != Contracts.CompletionCode.Success)
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " Completion code: {0}", myPacket.completionCode.ToString());
            }
            else if (myPacket.completionCode == Contracts.CompletionCode.Success)
            {
                // Display output 
                List<string> myStrings = new List<string>();

                // bladeCollections output
                if (myPacket == null)
                {
                    Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                    return;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.getBladeInfoComputeNodeHeader);
                    Console.WriteLine("Firmware Version\t: " + myPacket.firmwareVersion);
                    Console.WriteLine("Hardware Version\t: " + myPacket.hardwareVersion);
                    Console.WriteLine("Serial Number\t\t: " + myPacket.serialNumber);
                    Console.WriteLine("Asset Tag\t\t: " + myPacket.assetTag);
                    Console.WriteLine("");
                    if (myPacket.macAddress != null)
                    {
                        Console.WriteLine(WcsCliConstants.macAddressesInfoHeader);

                        foreach (NicInfo ni in myPacket.macAddress)
                        {
                            Console.WriteLine("Device Id\t: " + ni.deviceId);
                            Console.WriteLine("MAC Address\t: " + ni.macAddress + "\n");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine(WcsCliConstants.dataFetchError);
            }
        }
    }

    /// <summary>
    /// GetChassisHealth command class - derives from parent command class.
    /// </summary>
    internal class GetChassisHealth : command
    {
        internal GetChassisHealth()
        {
            this.name = WcsCliConstants.getChassisHealth;
            this.argSpec.Add('b', null);
            this.argSpec.Add('p', null);
            this.argSpec.Add('f', null);
            this.argSpec.Add('t', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getChassisHealthHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            bool noArgSpecified = false;
            Contracts.ChassisHealthResponse myPacket = new Contracts.ChassisHealthResponse();

            if (!(this.argVal.ContainsKey('b') || this.argVal.ContainsKey('p') || this.argVal.ContainsKey('f') || this.argVal.ContainsKey('t')))
            {
                noArgSpecified = true;
            }
            try
            {
                if (noArgSpecified)
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetChassisHealth(true, true, true, true);
                }
                else
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetChassisHealth(this.argVal.ContainsKey('b'), this.argVal.ContainsKey('p'),
                                                                                   this.argVal.ContainsKey('f'), this.argVal.ContainsKey('t'));
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            // If service response is null display error message to user & return
            if (myPacket == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            // If completion code is failure/timeout/!Success display appropriate error message and return
            if (myPacket.completionCode == Contracts.CompletionCode.Failure
                || myPacket.completionCode == Contracts.CompletionCode.Timeout
                || myPacket.completionCode != Contracts.CompletionCode.Success)
            {
                this.printErrorMessage(myPacket.completionCode, myPacket);
                return;
            }

            Console.WriteLine(WcsCliConstants.commandSuccess);

            // if no arguments are specified or if Blade health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('b'))
            {
                if (myPacket.bladeShellCollection != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.bladeHeathHeader);

                    // For each blade shell object in the collection, display blade health data
                    foreach (BladeShellResponse bsr in myPacket.bladeShellCollection)
                    {
                        Console.WriteLine("Blade Id\t: " + bsr.bladeNumber);
                        Console.WriteLine("Blade State\t: " + bsr.bladeState);
                        Console.WriteLine("Blade Type\t: " + bsr.bladeType);
                        Console.WriteLine("Blade Completion Code\t: " + bsr.completionCode);
                        Console.WriteLine("");
                    }
                }
                else
                {
                    // If blade response from service is empty display error message 
                    Console.WriteLine("Blade Health: " + WcsCliConstants.serviceResponseEmpty);
                }

            }

            // if no arguments are specified or if Psu health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('p'))
            {
                if (myPacket.psuInfoCollection != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.psuHealthHeader);

                    // For each Psu info object in the collection, display Psu data
                    foreach (PsuInfo pi in myPacket.psuInfoCollection)
                    {
                        Console.WriteLine("Psu Id\t\t\t: " + pi.id);
                        Console.WriteLine("Psu Serial Number\t: " + pi.serialNumber);
                        Console.WriteLine("Psu State\t\t: " + pi.state.ToString());
                        Console.WriteLine("PSU Power Out\t\t: " + pi.powerOut);
                        Console.WriteLine("Psu Completion Code\t: " + pi.completionCode);
                        Console.WriteLine("");
                    }
                }
                else
                {
                    // If Psu response from service is empty display error message 
                    Console.WriteLine("PSU Health: " + WcsCliConstants.serviceResponseEmpty);
                }
            }

            // if no arguments are specified or if battery health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('t'))
            {
                if (myPacket.batteryInfoCollection != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.batteryHealthHeader);

                    // For each battery info object in the collection, display battery data
                    foreach (BatteryInfo bi in myPacket.batteryInfoCollection)
                    {
                        Console.WriteLine("Battery Id\t\t: " + bi.id);
                        Console.WriteLine("Battery Present\t\t: " + bi.presence);
                        Console.WriteLine("Battery Power Out\t: " + bi.batteryPowerOutput);
                        Console.WriteLine("Battery Charge Level\t: " + bi.batteryChargeLevel);
                        Console.WriteLine("Battery Fault Detected\t: " + bi.faultDetected);
                        Console.WriteLine("Battery Completion Code\t: " + bi.completionCode);
                        Console.WriteLine("");
                    }
                }
                else
                {
                    // If battery response from service is empty display error message 
                    Console.WriteLine("Battery Health: " + WcsCliConstants.serviceResponseEmpty);
                }
            }

            // if no arguments are specified or if Fan health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('f'))
            {
                if (myPacket.fanInfoCollection != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.fanHealthHeader);

                    // If there are no fans present, print this message
                    if (myPacket.fanInfoCollection.Count() == 0)
                    {
                        Console.WriteLine("No fan data available");
                        return;
                    }

                    // For each Fan info object in the collection, display Fan data
                    foreach (FanInfo fi in myPacket.fanInfoCollection)
                    {
                        if (fi.completionCode == Contracts.CompletionCode.Success)
                        {
                            Console.WriteLine("Fan Id\t\t\t: " + fi.fanId);
                            Console.WriteLine("Fan Speed\t\t: " + fi.fanSpeed);
                            Console.WriteLine("Fan Status\t\t: " + (fi.isFanHealthy == true ? "Healthy" : "Unhealthy"));
                            Console.WriteLine("Fan Completion Code\t: " + fi.completionCode);
                            Console.WriteLine("");
                        }
                        else if (fi.completionCode == Contracts.CompletionCode.FanlessChassis)
                        {
                            Console.WriteLine("Fan Id\t\t\t: " + fi.fanId);
                            Console.WriteLine("Fanless Chassis");
                            Console.WriteLine("Fan Completion Code\t: " + fi.completionCode);
                            Console.WriteLine("");
                        }
                    }
                }
                else
                {
                    // If Fan response from service is empty display error message 
                    Console.WriteLine("Fan Health: " + WcsCliConstants.serviceResponseEmpty);
                    Console.WriteLine("No fan data found.");
                }
            }
            // Since, we are printing completion code separately at the end of all blades (-b option), don't print again
            if (!this.argVal.ContainsKey('b'))
            {
                Console.WriteLine("Completion Code: " + myPacket.completionCode);
            }
        }
    }

    /// <summary>
    /// This command is called to terminate connection to CM
    /// </summary>
    internal class TerminateCmConnection : command
    {
        internal TerminateCmConnection()
        {
            this.name = WcsCliConstants.terminateCmConnection;
            this.helpString = WcsCliConstants.terminateCmConnectionHelp;
            this.argSpec.Add('h', null);
        }

        internal override void commandImplementation()
        {
            try
            {
                // clear command history while ending the session.
                CliSerialPort.ClearHistory();

                WcsCli2CmConnectionManager.TerminateConnectionToCmService();
                Console.WriteLine("Connection to CM terminated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection to CM failed.");
                Console.WriteLine("Exception is " + ex);
            }
        }
    }

    // <summary>
    /// This command is called at the start of application to get host, port, 
    /// SSL encryption option, batch file option from user.
    /// This method sets the config parameters.
    /// </summary>
    internal class EstablishConnectionToCm : command
    {
        internal EstablishConnectionToCm()
        {
            this.name = WcsCliConstants.establishCmConnection;
            this.argSpec.Add('m', Type.GetType("System.String"));
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('s', Type.GetType("System.UInt32"));
            this.argSpec.Add('u', Type.GetType("System.String"));
            this.argSpec.Add('x', Type.GetType("System.String"));
            this.argSpec.Add('b', Type.GetType("System.String"));
            this.argSpec.Add('v', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.establishCmConnectionHelp;

            char[] optionalList = new char[] { 'v', 'h' };
            this.conditionalOptionalArgs.Add('p', optionalList);
            this.conditionalOptionalArgs.Add('s', optionalList);
            this.conditionalOptionalArgs.Add('u', optionalList);
            this.conditionalOptionalArgs.Add('x', optionalList);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            dynamic hostname = null;
            dynamic port = null;
            dynamic sslOption = null;
            dynamic username = null;
            dynamic password = null;

            string tempHostname = null;
            int tempPortno;
            bool tempSslEnabled = false;
            string tempUsername = null;
            string tempPassword = null;

            try
            {
                // version information is requested
                if (this.argVal.ContainsKey('v'))
                {
                    // display CLI version
                    Console.WriteLine("WCSCLI version: " + WcsCli2CmConnectionManager.GetCLIVersion());
                    // If version info is requested, do not process anything else.. just exit the command.. 
                    return;
                }
                // If this is a serial (local) client, CM hostname is assumed as localhost
                if (this.isSerialClient)
                {
                    this.argVal['m'] = "localhost";
                }
                // If hostname is not provided, CM hostname is assumed as localhost
                if (!this.argVal.TryGetValue('m', out hostname))
                {
                    hostname = "localhost";
                }
                if (this.argVal.TryGetValue('p', out port) && this.argVal.TryGetValue('s', out sslOption))
                {
                    tempHostname = (string)hostname;
                    tempPortno = (int)port;
                    if ((int)sslOption == 1)
                    {
                        tempSslEnabled = true;
                    }
                    else
                    {
                        tempSslEnabled = false;
                    }

                    // check if both username and password are specified, else use default credentials
                    if (this.argVal.TryGetValue('u', out username) && this.argVal.TryGetValue('x', out password))
                    {
                        tempUsername = (string)username;
                        tempPassword = (string)password;
                    }
                    else
                    {
                        Console.WriteLine("Please provide valid user credentials");
                        if (isSerialClient)
                        {
                            Console.WriteLine(this.helpString);
                        }

                        return;
                    }
                    // Validate hostname and Port, if not valid fail early
                    if (WcsCli2CmConnectionManager.ValidateHostNameAndPort(tempHostname, tempPortno))
                    {
                        // If hostname and port are valid, try to establish a connection
                        WcsCli2CmConnectionManager.CreateConnectionToService(tempHostname, tempPortno, tempSslEnabled, tempUsername, tempPassword);

                        // Test connection to service
                        if (!WcsCli2CmConnectionManager.TestConnectionToCmService())
                        {
                            Console.WriteLine("Connection to CM is not successful. \n");
                        }
                        else
                        {
                            Console.WriteLine("Connection to CM succeeded. \n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Connection to CM is not successful. \n");
                    }
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                    if (isSerialClient)
                    {
                        Console.WriteLine(this.helpString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(WcsCliConstants.invalidCommandString + ex.Message);
                return;
            }
        }
    }

    /// <summary>
    /// GetBladeHealth command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class GetBladeHealth : command
    {
        internal GetBladeHealth()
        {
            this.name = WcsCliConstants.getBladeHealth;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('q', null);
            this.argSpec.Add('m', null);
            this.argSpec.Add('d', null);
            this.argSpec.Add('p', null);
            this.argSpec.Add('s', null);
            this.argSpec.Add('t', null);
            this.argSpec.Add('f', null);
            this.argSpec.Add('h', null);

            this.conditionalOptionalArgs.Add('i', null);

            this.helpString = WcsCliConstants.getBladeHealthHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeHealthResponse myPacket = new BladeHealthResponse();
            bool noArgSpecified = false;
            dynamic bladeId = null;

            if (!(this.argVal.ContainsKey('q') || this.argVal.ContainsKey('m') || this.argVal.ContainsKey('d') || this.argVal.ContainsKey('p')
                || this.argVal.ContainsKey('s') || this.argVal.ContainsKey('t') || this.argVal.ContainsKey('f')))
            {
                noArgSpecified = true;
            }

            try
            {
                this.argVal.TryGetValue('i', out bladeId);

                if (noArgSpecified)
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetBladeHealth((int)bladeId, true, true, true, true, true, true, true);
                }
                else
                {
                    myPacket = WcsCli2CmConnectionManager.channel.GetBladeHealth((int)bladeId, this.argVal.ContainsKey('q'), this.argVal.ContainsKey('m'),
                       this.argVal.ContainsKey('d'), this.argVal.ContainsKey('p'), this.argVal.ContainsKey('s'), this.argVal.ContainsKey('t'), this.argVal.ContainsKey('f'));
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            // If service response is null display error message to user & return
            if (myPacket == null || myPacket.bladeShell == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }
            // If completion code is failure/timeout/!Success display appropriate error message and return
            if (myPacket.completionCode == Contracts.CompletionCode.Failure
                || myPacket.completionCode == Contracts.CompletionCode.Timeout
                || myPacket.completionCode != Contracts.CompletionCode.Success)
            {
                this.printErrorMessage(myPacket.completionCode, myPacket);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("== Blade " + bladeId + " Health Information ==");
            Console.WriteLine("Blade ID\t: " + myPacket.bladeShell.bladeNumber);
            Console.WriteLine("Blade State\t: " + myPacket.bladeShell.bladeState);
            Console.WriteLine("Blade Type\t: " + myPacket.bladeShell.bladeType);
            Console.WriteLine("");

            // if no arguments are specified or if Processor health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('q'))
            {
                if (myPacket.processorInfo != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.cpuInfo);

                    // For each processor object in the collection, display processor health data
                    foreach (ProcessorInfo pri in myPacket.processorInfo)
                    {
                        Console.WriteLine("Processor Id\t\t: " + pri.procId);
                        Console.WriteLine("Processor Type\t\t: " + pri.procType);
                        Console.WriteLine("Processor Frequency\t: " + pri.frequency + " MHz");
                        Console.WriteLine("");
                    }
                }
                else if (myPacket.processorInfo == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    // If blade response from service is empty display error message 
                    Console.WriteLine("Blade Processor Information: " + WcsCliConstants.serviceResponseEmpty);
                }

            }
            // if no arguments are specified or if Memory health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('m'))
            {
                if (myPacket.memoryInfo != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.memoryInfo);

                    // For each memory info object in the collection, display Memory data
                    foreach (MemoryInfo mi in myPacket.memoryInfo)
                    {
                        Console.WriteLine("Dimm\t\t: " + mi.dimm);
                        Console.WriteLine("Dimm Type\t: " + mi.dimmType);
                        Console.WriteLine("Memory Voltage\t: " + mi.memVoltage);
                        Console.WriteLine("Size\t\t: " + mi.size);
                        Console.WriteLine("Speed\t\t: " + mi.speed);
                        Console.WriteLine("Memory Status\t: " + mi.status);
                        Console.WriteLine("Memory Completion code: " + mi.completionCode.ToString());
                        Console.WriteLine("");
                    }
                }
                else if (myPacket.processorInfo == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    // If response from service is empty display error message 
                    Console.WriteLine("Blade memory Information: " + WcsCliConstants.serviceResponseEmpty);
                }
            }
            // if no arguments are specified or if Disk health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('d'))
            {
                // Disk info is only supported for JBOD
                if (myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeJBOD && myPacket.jbodDiskInfo != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.diskInfo);
                    Console.WriteLine("JBOD Disk Count\t\t: " + myPacket.jbodDiskInfo.diskCount);
                    Console.WriteLine("JBOD Disk Channel\t: " + myPacket.jbodDiskInfo.channel);
                    Console.WriteLine("Disk CompletionCode\t: " + myPacket.jbodDiskInfo.completionCode.ToString());
                    Console.WriteLine("");
                    foreach (DiskInfo di in myPacket.jbodDiskInfo.diskInfo)
                    {
                        Console.WriteLine("== Disk " + di.diskId + " ==");
                        Console.WriteLine("JBOD Disk ID\t\t: " + di.diskId);
                        Console.WriteLine("JBOD Disk Status\t: " + di.diskStatus);
                        Console.WriteLine("");
                    }
                }
                else if (myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeJBOD && myPacket.jbodDiskInfo == null)
                {
                    // If disk response from service is empty display error message 
                    Console.WriteLine("Blade health: Disk Information: " + WcsCliConstants.serviceResponseEmpty);
                }
            }
            // if no arguments are specified or if PCIE health is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('p'))
            {
                if (myPacket.pcieInfo != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.pcieInfo);

                    // For each PCIE info object in the collection, display PCIE data
                    foreach (PCIeInfo pci in myPacket.pcieInfo)
                    {
                        Console.WriteLine("PCIE Slot Number\t: " + pci.pcieNumber);
                        Console.WriteLine("PCIE Vendor Id\t\t: " + pci.vendorId);
                        Console.WriteLine("PCIE Device Id\t\t: " + pci.deviceId);
                        Console.WriteLine("PCIE Subsystem Id\t: " + pci.subSystemId);
                        Console.WriteLine("PCIE Card State\t\t: " + pci.status);
                        Console.WriteLine("PCIE Completion Code\t: " + pci.completionCode.ToString());
                        Console.WriteLine("");
                    }
                }
                else if (myPacket.processorInfo == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    // If PCIE response from service is empty display error message 
                    Console.WriteLine("Blade PCIE Information: " + WcsCliConstants.serviceResponseEmpty);
                }
            }

            // if no arguments are specified or if Sensor info is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('s'))
            {
                if (myPacket.sensors != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.sensorInfo);

                    // For each Sensor info object in the collection, display Sensor data
                    foreach (SensorInfo si in myPacket.sensors)
                    {
                        // Print out hardware sensor information except temperature sensors
                        if (si.sensorType != WcsCliConstants.SensorTypeTemp)
                        {
                            Console.WriteLine("Sensor Number\t\t: 0x{0:X}", si.sensorNumber);
                            Console.WriteLine("Sensor Type\t\t: " + si.sensorType);
                            Console.WriteLine("Sensor Reading\t\t: " + si.reading);
                            Console.WriteLine("Sensor Description\t: " + si.description);
                            Console.WriteLine("Sensor Entity ID\t: " + si.entityId);
                            Console.WriteLine("Sensor Entity Instance\t: " + si.entityInstance);
                            Console.WriteLine("Sensor Status\t\t: " + si.status);
                            Console.WriteLine("Sensor CompletionCode\t: " + si.completionCode.ToString());
                            Console.WriteLine("");
                        }
                    }
                }
                else if (myPacket.sensors == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    // If Sensor response from service is empty display error message 
                    Console.WriteLine("Blade Sensor Information: " + WcsCliConstants.serviceResponseEmpty);
                }
            }

            // if no arguments are specified or if temp Sensor info is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('t'))
            {
                if (myPacket.sensors != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.tempSensorInfo);

                    // For each Sensor info object in the collection, display Sensor data
                    foreach (SensorInfo si in myPacket.sensors)
                    {
                        // Print out temperature sensor information only
                        if (si.sensorType == WcsCliConstants.SensorTypeTemp)
                        {
                            Console.WriteLine("Sensor Number\t\t: 0x{0:X}", si.sensorNumber);
                            Console.WriteLine("Sensor Type\t\t: " + si.sensorType);
                            Console.WriteLine("Sensor Reading\t\t: " + si.reading);
                            Console.WriteLine("Sensor Description\t: " + si.description);
                            Console.WriteLine("Sensor Entity ID\t: " + si.entityId);
                            Console.WriteLine("Sensor Entity Instance\t: " + si.entityInstance);
                            Console.WriteLine("Sensor Status\t\t: " + si.status);
                            Console.WriteLine("Sensor CompletionCode\t: " + si.completionCode.ToString());
                            Console.WriteLine("");
                        }
                    }
                }
                else if (myPacket.jbodInfo != null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeJBOD)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.tempSensorInfo);

                    // Temp information for JBOD
                    Console.WriteLine("Unit\t: " + myPacket.jbodInfo.unit);
                    Console.WriteLine("Reading\t: " + myPacket.jbodInfo.reading);
                }
                else if ((myPacket.sensors == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute) ||
                         (myPacket.jbodInfo == null && myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeJBOD))
                {
                    // If Sensor response from service is empty display error message 
                    Console.WriteLine("Temperature Sensor Information: " + WcsCliConstants.serviceResponseEmpty);
                }
            }

            // if no arguments are specified or if FRU information is requested
            if (noArgSpecified == true || this.argVal.ContainsKey('f'))
            {
                if (myPacket != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(WcsCliConstants.fruInfo);
                    Console.WriteLine("Blade Serial Number\t: " + myPacket.serialNumber);
                    Console.WriteLine("Blade Asset Tag\t\t: " + myPacket.assetTag);
                    Console.WriteLine("Blade Product Type\t: " + myPacket.productType);
                    Console.WriteLine("Blade Hardware Version\t: " + myPacket.hardwareVersion);
                    Console.WriteLine("");
                }
                else if (myPacket.processorInfo == null && (myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeCompute ||
                    myPacket.bladeShell.bladeType == WcsCliConstants.BladeTypeJBOD))
                {
                    // If FRU response from service is empty display error message 
                    Console.WriteLine("Blade FRU Info: " + WcsCliConstants.serviceResponseEmpty);
                }
            }
        }
    }

    /// <summary>
    /// UpdatePsuFirmware command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class UpdatePsuFirmware : command
    {
        internal UpdatePsuFirmware()
        {
            this.name = WcsCliConstants.updatePsuFirmware;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('f', Type.GetType("System.String"));
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.updatePsuFwHelp;

            this.conditionalOptionalArgs.Add('i', null);
            this.conditionalOptionalArgs.Add('f', null);
            this.conditionalOptionalArgs.Add('p', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            ChassisResponse response = new ChassisResponse();
            dynamic psuId = null;
            dynamic fwFilepath = null;
            dynamic primaryImage = null;

            try
            {
                if (this.argVal.TryGetValue('i', out psuId) &&
                    this.argVal.TryGetValue('f', out fwFilepath) &&
                    this.argVal.TryGetValue('p', out primaryImage))
                {
                    if ((primaryImage == 0) || (primaryImage == 1))
                    {
                        bool isPrimaryImage = ((int)primaryImage == 0) ? false : true;
                        response = WcsCli2CmConnectionManager.channel.UpdatePSUFirmware((int)psuId, (string)fwFilepath, isPrimaryImage);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.argsOutOfRange);
                        return;
                    }
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.argsMissingString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            string message = "Use wcscli -getpsufwstatus to check the status of the firmware update.";
            if (!ResponseValidation.ValidateResponse(message, response, true))
            {
                Console.WriteLine(response.statusDescription);
            }
        }
    }

    /// <summary>
    /// GetPsuFirmwareStatus command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class GetPsuFirmwareStatus : command
    {
        internal GetPsuFirmwareStatus()
        {
            this.name = WcsCliConstants.getPsuFirmwareStatus;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);

            this.conditionalOptionalArgs.Add('i', null);

            this.helpString = WcsCliConstants.getPsuFwStatusHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            dynamic psuId = null;
            PsuFirmwareStatus response = new PsuFirmwareStatus();

            try
            {
                this.argVal.TryGetValue('i', out psuId);
                response = WcsCli2CmConnectionManager.channel.GetPSUFirmwareStatus((int)psuId);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, response, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.psuFwStatusHeader);
                Console.WriteLine("Firmware Revision\t: " + response.fwRevision);
                Console.WriteLine("Firmware Update Status\t: " + response.fwUpdateStatus);
                Console.WriteLine("Firmware Update Stage\t: " + response.fwUpdateStage);
                Console.WriteLine("Completion Code\t\t: " + response.completionCode);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine(response.statusDescription);
            }
        }
    }

    /// <summary>
    /// help command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class help : command
    {
        internal help()
        {
            this.name = WcsCliConstants.help;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Console.WriteLine(WcsCliConstants.WcsCliHelp);
        }
    }

    /// <summary>
    /// Get Chassis Attention LED class
    /// </summary>
    internal class GetChassisAttentionLEDStatus : command
    {
        internal GetChassisAttentionLEDStatus()
        {
            this.name = WcsCliConstants.getChassisAttentionLEDStatus;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getchassisattentionledstatusHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            LedStatusResponse myResponse = new LedStatusResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.GetChassisAttentionLEDStatus();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " Chassis LED: " + myResponse.ledState);
            }
        }
    }

    /// <summary>
    /// Set Chassis Attention LED On class
    /// </summary>
    internal class SetChassisAttentionLEDOn : command
    {
        internal SetChassisAttentionLEDOn()
        {
            this.name = WcsCliConstants.setChassisAttentionLEDOn;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setchassisattentionledonHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.SetChassisAttentionLEDOn();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse(null, myResponse, true);
        }
    }

    /// <summary>
    /// Set Chassis Attention LED Off class
    /// </summary>
    internal class SetChassisAttentionLEDOff : command
    {
        internal SetChassisAttentionLEDOff()
        {
            this.name = WcsCliConstants.setChassisAttentionLEDOff;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setchassisattentionledoffHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.SetChassisAttentionLEDOff();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse(null, myResponse, true);
        }
    }

    /// <summary>
    /// Set Blade Attention LED On class
    /// </summary>
    internal class SetBladeAttentionLEDOn : command
    {
        internal SetBladeAttentionLEDOn()
        {
            this.name = WcsCliConstants.setBladeAttentionLEDOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladeattentionledonHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesAttentionLEDOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladeAttentionLEDOn((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": Attention LED ON");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": Attention LED ON");
                }
            }
        }
    }

    /// <summary>
    /// Set Blade Attention LED Off class
    /// </summary>
    internal class SetBladeAttentionLEDOff : command
    {
        internal SetBladeAttentionLEDOff()
        {
            this.name = WcsCliConstants.setBladeAttentionLEDOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladeattentionledoffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesAttentionLEDOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladeAttentionLEDOff((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": Attention LED OFF");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": Attention LED OFF");
                }
            }
        }
    }

    /// <summary>
    /// Set Blade Default Power State
    /// </summary>
    internal class SetBladeDefaultPowerState : command
    {
        internal SetBladeDefaultPowerState()
        {
            this.name = WcsCliConstants.setBladeDefaultPowerState;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('s', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladedefaultpowerstateHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
            this.conditionalOptionalArgs.Add('s', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            dynamic myState = null;
            dynamic mySledId = null;
            this.argVal.TryGetValue('s', out myState);

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    if ((uint)myState == 0)
                    {
                        myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDefaultPowerStateOff();
                    }
                    else if ((uint)myState == 1)
                    {
                        myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDefaultPowerStateOn();
                    }
                    else
                    {
                        Console.WriteLine("Invalid power state.");
                        return;
                    }
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    this.argVal.TryGetValue('i', out mySledId);
                    if ((uint)myState == 0)
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDefaultPowerStateOff((int)mySledId);
                    }
                    else if ((uint)myState == 1)
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDefaultPowerStateOn((int)mySledId);
                    }
                    else
                    {
                        Console.WriteLine("Invalid power state.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        if ((uint)myState == 0)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": OFF");
                        }
                        else if ((uint)myState == 1)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": ON");
                        }
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {

                    if ((uint)myState == 0)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": OFF");
                    }
                    else if ((uint)myState == 1)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": ON");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get Blade Default Power State
    /// </summary>
    internal class GetBladeDefaultPowerState : command
    {
        internal GetBladeDefaultPowerState()
        {
            this.name = WcsCliConstants.getBladeDefaultPowerState;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getbladedefaultpowerstateHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            dynamic mySledId = null;
            BladeStateResponse myResponse = new BladeStateResponse();
            GetAllBladesStateResponse myResponses = new GetAllBladesStateResponse();
            uint sledId = 1;
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesDefaultPowerState();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladeDefaultPowerState((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeStateResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeStateResponseCollection[index].bladeNumber, null, myResponses.bladeStateResponseCollection[index], false))
                    {
                        if (myResponses.bladeStateResponseCollection[index].bladeState == Contracts.PowerState.ON)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeStateResponseCollection[index].bladeNumber + " Default Power State: ON");
                        }
                        else if (myResponses.bladeStateResponseCollection[index].bladeState == Contracts.PowerState.OFF)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeStateResponseCollection[index].bladeNumber + " Default Power State: OFF");
                        }
                        else
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeStateResponseCollection[index].bladeNumber + " Default Power State: --");
                        }
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {

                    if (myResponse.bladeState == Contracts.PowerState.ON)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + " Default Power State: ON");
                    }
                    else if (myResponse.bladeState == Contracts.PowerState.OFF)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + " Default Power State: OFF");
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + " Default Power State: --");
                    }
                }
            }
        }
    }

    // TODO: Do we need wait until poweron? ask Matt Eason since this is supported 
    // Try catch in all functions - code gets bloated
    /// <summary>
    /// Set Blade Hard Power On class
    /// </summary>
    internal class SetPowerOn : command
    {
        internal SetPowerOn()
        {
            this.name = WcsCliConstants.setPowerOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setpoweronHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllPowerOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetPowerOn((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": ON");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": ON");
                }
            }
        }
    }

    /// <summary>
    /// Set Blade Soft Power On class
    /// </summary>
    internal class SetBladeOn : command
    {
        internal SetBladeOn()
        {
            this.name = WcsCliConstants.setBladeOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladeonHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladeOn((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": ON");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": ON");
                }
            }
        }
    }

    /// <summary>
    /// Set Blade Hard Power Off class
    /// </summary>
    internal class SetPowerOff : command
    {
        internal SetPowerOff()
        {
            this.name = WcsCliConstants.setPowerOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setpoweroffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllPowerOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetPowerOff((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": OFF");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": OFF");
                }
            }
        }
    }

    /// <summary>
    /// Set Blade Soft Power Off class
    /// </summary>
    internal class SetBladeOff : command
    {
        internal SetBladeOff()
        {
            this.name = WcsCliConstants.setBladeOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladeoffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladeOff((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": OFF");

                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": OFF");
                }
            }
        }

    }

    /// <summary>
    /// Set Blade Soft Power Cycle class
    /// </summary>
    internal class SetBladeActivePowerCycle : command
    {
        internal SetBladeActivePowerCycle()
        {
            this.name = WcsCliConstants.setBladeActivePowerCycle;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('t', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladeactivepowercycleHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse allBladesResponse = new AllBladesResponse();
            dynamic myOffTime = null;

            if (this.argVal.ContainsKey('t'))
            {
                this.argVal.TryGetValue('t', out myOffTime);
            }
            else
            {
                myOffTime = WcsCliConstants.powercycleOfftime;
            }

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    allBladesResponse = WcsCli2CmConnectionManager.channel.SetAllBladesActivePowerCycle((uint)myOffTime);
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladeActivePowerCycle((int)mySledId, (uint)myOffTime);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && allBladesResponse == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                if (allBladesResponse.completionCode == Contracts.CompletionCode.Success)
                {
                    for (int index = 0; index < allBladesResponse.bladeResponseCollection.Count(); index++)
                    {
                        if (ResponseValidation.ValidateBladeResponse(allBladesResponse.bladeResponseCollection[index].bladeNumber, null, allBladesResponse.bladeResponseCollection[index], false))
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + allBladesResponse.bladeResponseCollection[index].bladeNumber + ": OK");
                        }
                    }
                }
                else
                {
                    // Display error if not Success/Unknown
                    Console.WriteLine(WcsCliConstants.commandFailure + " Completion code: " + allBladesResponse.completionCode.ToString());
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, true);
            }
        }
    }

    /// <summary>
    /// Get Blade Hard Power State class
    /// </summary>
    internal class GetBladeHardPowerState : command
    {
        internal GetBladeHardPowerState()
        {
            this.name = WcsCliConstants.getBladeHardPowerState;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeHardPowerStateHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            PowerStateResponse myResponse = new PowerStateResponse();
            GetAllPowerStateResponse myResponses = new GetAllPowerStateResponse();

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllPowerState();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.GetPowerState((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.powerStateResponseCollection.Count(); index++)
                {

                    if (ResponseValidation.ValidateBladeResponse(myResponses.powerStateResponseCollection[index].bladeNumber, null, myResponses.powerStateResponseCollection[index], false))
                    {
                        if (myResponses.powerStateResponseCollection[index].completionCode == Contracts.CompletionCode.Success)
                        {
                            if (myResponses.powerStateResponseCollection[index].powerState == Contracts.PowerState.ON)
                            {
                                Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponses.powerStateResponseCollection[index].bladeNumber + ": ON");
                            }
                            else if (myResponses.powerStateResponseCollection[index].powerState == Contracts.PowerState.OFF)
                            {
                                Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponses.powerStateResponseCollection[index].bladeNumber + ": OFF");
                            }
                            else if (myResponses.powerStateResponseCollection[index].powerState == Contracts.PowerState.OnFwDecompress)
                            {
                                Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponses.powerStateResponseCollection[index].bladeNumber + ": ON - Firmware Decompressing");
                            }
                            else
                            {
                                Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponses.powerStateResponseCollection[index].bladeNumber + ": --");
                            }
                        }
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    if (myResponse.powerState == Contracts.PowerState.ON)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponse.bladeNumber + ": ON");
                    }
                    else if (myResponse.powerState == Contracts.PowerState.OFF)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponse.bladeNumber + ": OFF");
                    }
                    else if (myResponse.powerState == Contracts.PowerState.OnFwDecompress)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponse.bladeNumber + ": ON - Firmware Decompressing");
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade Active Power State " + myResponse.bladeNumber + ": --");
                    }
                }
            }
        }

    }

    /// <summary>
    /// Get Blade Soft Power State class
    /// </summary>
    internal class GetBladeSoftPowerState : command
    {
        internal GetBladeSoftPowerState()
        {
            this.name = WcsCliConstants.getBladeSoftPowerState;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeSoftPowerStateHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeStateResponse myResponse = new BladeStateResponse();
            GetAllBladesStateResponse myResponses = new GetAllBladesStateResponse();

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesState();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladeState((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeStateResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeStateResponseCollection[index].bladeNumber, null, myResponses.bladeStateResponseCollection[index], false))
                    {
                        if (myResponses.bladeStateResponseCollection[index].bladeState == Contracts.PowerState.ON)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponses.bladeStateResponseCollection[index].bladeNumber + ": ON");
                        }
                        else if (myResponses.bladeStateResponseCollection[index].bladeState == Contracts.PowerState.OFF)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponses.bladeStateResponseCollection[index].bladeNumber + ": OFF");
                        }
                        else if (myResponses.bladeStateResponseCollection[index].bladeState == Contracts.PowerState.OnFwDecompress)
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponses.bladeStateResponseCollection[index].bladeNumber + ": ON - Firmware Decompressing");
                        }
                        else
                        {
                            Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponses.bladeStateResponseCollection[index].bladeNumber + ": --");
                        }
                    }
                }
            }
            else
            {

                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    if (myResponse.bladeState == Contracts.PowerState.ON)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponse.bladeNumber + ": ON");
                    }
                    else if (myResponse.bladeState == Contracts.PowerState.OFF)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponse.bladeNumber + ": OFF");
                    }
                    else if (myResponse.bladeState == Contracts.PowerState.OnFwDecompress)
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponse.bladeNumber + ": ON - Firmware Decompressing");
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade State " + myResponse.bladeNumber + ": --");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set Socket Power On class
    /// </summary>
    internal class SetACSocketPowerStateOn : command
    {
        internal SetACSocketPowerStateOn()
        {
            this.name = WcsCliConstants.setACSocketPowerStateOn;
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setacsocketpowerstateonHelp;

            this.conditionalOptionalArgs.Add('p', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic myPortNo = null;
            uint portNo = 0;

            try
            {
                this.argVal.TryGetValue('p', out myPortNo);
                portNo = (uint)myPortNo;
                myResponse = WcsCli2CmConnectionManager.channel.SetACSocketPowerStateOn(portNo);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse(null, myResponse, true);
        }
    }

    /// <summary>
    /// Set Socket Power Off class
    /// </summary>
    internal class SetACSocketPowerStateOff : command
    {
        internal SetACSocketPowerStateOff()
        {
            this.name = WcsCliConstants.setACSocketPowerStateOff;
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setacsocketpowerstateoffHelp;

            this.conditionalOptionalArgs.Add('p', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic myPortNo = null;
            uint portNo = 0;

            try
            {
                this.argVal.TryGetValue('p', out myPortNo);
                portNo = (uint)myPortNo;
                myResponse = WcsCli2CmConnectionManager.channel.SetACSocketPowerStateOff((uint)myPortNo);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse(null, myResponse, true);
        }
    }

    /// <summary>
    /// Get AC Socket Power State class
    /// </summary>
    internal class GetACSocketPowerState : command
    {
        internal GetACSocketPowerState()
        {
            this.name = WcsCliConstants.getACSocketPowerState;
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getacsocketpowerstateHelp;

            this.conditionalOptionalArgs.Add('p', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            ACSocketStateResponse myResponse = new ACSocketStateResponse();
            dynamic myPortNo = null;
            uint portNo = 0;
            try
            {
                this.argVal.TryGetValue('p', out myPortNo);
                portNo = (uint)myPortNo;
                myResponse = WcsCli2CmConnectionManager.channel.GetACSocketPowerState(portNo);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.Write(WcsCliConstants.commandSuccess);
                int index = (int)myPortNo;
                if (myResponse.powerState == PowerState.ON)
                {
                    Console.WriteLine("ON");
                }
                else if (myResponse.powerState == PowerState.OFF)
                {
                    Console.WriteLine("OFF");
                }
                else
                {
                    Console.WriteLine("--");
                }
            }
        }
    }

    /// <summary>
    /// Abstract class for VT100 serial session commands
    /// </summary>
    internal abstract class SerialSession : command
    {
        /// <summary>
        /// local class cache locker object
        /// </summary>
        protected readonly object _locker = new object();

        /// <summary>
        /// session string / serial session cookie.
        /// </summary>
        protected string _sessionString = string.Empty;

        /// <summary>
        /// Access session string.
        /// </summary>
        protected string SessionString
        {
            get { lock (_locker) { return this._sessionString; } }
            set { lock (_locker) { this._sessionString = value; } }
        }

        // notice that read thread has ended.
        protected ManualResetEvent ended = new ManualResetEvent(false);

        /// <summary>
        /// Terminates polling thread for VT100
        /// </summary>
        protected void TerminateSession()
        {
            SharedFunc.SetSerialSession(false);

            if (!this.isSerialClient)
            {
                //clear history and console
                Console.Clear();
            }
            else
            {
                // clear the screen and input buffer while exiting.
                CliSerialPort.ClearWindow();
                CliSerialPort.ClearInputBuffer();

                CliSerialPort.FlushPortBuffers();
                CliSerialPort.SignalCarriageReturn();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        protected void ErrorMessage(string message, bool flush, bool terminate = false)
        {
            // terminate the vt100
            if (terminate)
            {
                // Terminate the serial session and clean up
                TerminateSession();

                if (!this.isSerialClient)
                {
                    if (flush)
                    {
                        Console.Clear();
                    }
                }

                terminationMessage = message;
            }
            else
            {
                // display the message on screen.
                Console.WriteLine(WcsCliConstants.commandFailure + " " + message);
            }
        }

        protected string terminationMessage = string.Empty;

        internal abstract void Send(byte[] payload);

        protected abstract void Receive();
    }

    /// <summary>
    /// StartBladeSerialSession class
    /// </summary>
    internal class startBladeSerialSession : SerialSession, IDisposable
    {
        // Track whether Dispose has been called. 
        private bool _disposed = false;

        // Default blade serial session timeout value to be used when the user does not specify it.
        // Its value is set to 0 here as this will get automatically translated to the default blade serial session
        // timeout value as specified in ChassisManager's App.config.
        private readonly int _defaultTimeout = 0;

        internal startBladeSerialSession()
        {
            this.name = WcsCliConstants.startBladeSerialSession;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('s', Type.GetType("System.Int32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.startBladeSerialSessionHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        private AnsiEscape<startBladeSerialSession> _vt100;

        /// <summary>
        /// WCS Blade Id
        /// </summary>
        private int bladeId = 0;

        /// <summary>
        /// VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private bool enterEncodeCRLF = true;

        /// <summary>
        /// Switches to VT100 mode.
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private void SetupVt100(bool enterEncodeCRLF)
        {
            _vt100 = new AnsiEscape<startBladeSerialSession>(this);

            // start the read on another thread
            Thread receiver = new Thread(new ThreadStart(Receive));
            receiver.Start();

            // This method blocks.  The only way to exit is kill the process
            // or press Ctrl + X
            _vt100.ReadConsole(enterEncodeCRLF);

            // wait at most for 3 seconds for the read thread to end.
            ended.WaitOne(3000);

            // undo all VT100 console changes. fall through will reach this method.
            _vt100.RevertConsole();

            // dispose of the Vt100 class.
            _vt100 = null;

            // write the tear down message
            Console.WriteLine();
            Console.WriteLine(terminationMessage);
            Console.WriteLine();
        }

        /// <summary>
        /// Establish blade serial session over Serial
        /// This method blocks. 
        /// Get user input and send it to the blade and also
        /// print out serial data from blade
        /// </summary>
        private void EstablishBladeSerialSessionOverSerialClient()
        {
            // start the read on another thread.
            Thread receiver = new Thread(new ThreadStart(Receive));
            receiver.Start();

            // Read continually until activeRead signal escapes.
            while (SharedFunc.ActiveSerialSession)
            {
                Byte[] userEnteredData = CliSerialPort.ReadUserInputBytesFromSerial();
                // Check if the data has ctrl-X and exit 
                for (int i = 0; i < userEnteredData.Length; i++)
                {
                    // Snoop to see if the user entered Ctrl+X,
                    // which is 0x18 in VT100 encoding
                    if ((int)userEnteredData[i] == 24)
                    {
                        // Signal the receive thread to stop
                        SharedFunc.SetSerialSession(false);

                        // clear the screen and input buffer while exiting.
                        CliSerialPort.ClearWindow();
                        CliSerialPort.ClearInputBuffer();

                        CliSerialPort.FlushPortBuffers();
                        break;
                    }
                }
                Send(userEnteredData);
            }
        }

        internal override void Send(byte[] payload)
        {
            try
            {
                // check session string and active write permission
                if (SessionString != string.Empty && SharedFunc.ActiveSerialSession && payload != null && payload.Length > 0)
                {
                    ChassisResponse response = WcsCli2CmConnectionManager.channel.SendBladeSerialData(bladeId, SessionString, payload);

                    if (response.completionCode != CompletionCode.Success)
                    {
                        // signals the session failed.
                        string msg = string.Format("Data Send Error failed with Completion Code: {0}.  See User Log for further information",
                            response.completionCode);

                        ErrorMessage(msg, true, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // signals the session failed.
                ErrorMessage(string.Format("Sending Data failed with Exception: {0}.  "
                    , ex.Message.ToString()), true, true);

                // revert console if exception
                _vt100.RevertConsole();
            }
        }

        /// <summary>
        /// Receives data over serial session
        /// </summary>
        protected override void Receive()
        {
            // check session string
            if (SessionString != string.Empty)
            {
                // Read continuously until session is closed
                while (SharedFunc.ActiveSerialSession)
                {
                    try
                    {
                        SerialDataResponse response = WcsCli2CmConnectionManager.channel.ReceiveBladeSerialData(bladeId, SessionString);

                        if (response.completionCode == CompletionCode.Success &&
                            response.data != null)
                        {
                            if (this.isSerialClient)
                            {
                                CliSerialPort.WriteBytestoSerial(response.data);
                            }
                            else
                            {
                                _vt100.SplitAnsiEscape(response.data);
                            }
                        }
                        // if completion code is anything other than a timeout (which is expected 
                        // when there is no data), or a BMC buffer overflow
                        // return an error message and kill the serial session
                        else if ((response.completionCode != CompletionCode.Success) &&
                            (response.completionCode != CompletionCode.Timeout) &&
                            (response.completionCode != CompletionCode.BmcRxSerialBufferOverflow))
                        {
                            // signals the session failed.
                            string msg = string.Format("Receiving Data failed with Completion Code: {0}.  See User Log for further information.", 
                                response.completionCode);
                            ErrorMessage(msg, true, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // signals the session failed.
                        ErrorMessage(string.Format("Receiving Data failed with Exception: {0}.  "
                            , ex.Message.ToString()), true, true);

                        // revert console if exception
                        _vt100.RevertConsole();
                    }
                }
            }
            // signal the read thread has ended.
            ended.Set();
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            StartSerialResponse myResponse = new StartSerialResponse();
            BladeInfoResponse bladeInfo = new BladeInfoResponse();

            try
            {
                if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    dynamic sessionTimeoutInSecs = null;
                    this.argVal.TryGetValue('i', out mySledId);

                    // Determine blade type
                    bladeInfo = WcsCli2CmConnectionManager.channel.GetBladeInfo((int)mySledId);
                    if (bladeInfo.completionCode == Contracts.CompletionCode.Success)
                    {
                        if (bladeInfo.bladeType == WcsCliConstants.BladeTypeCompute)
                        {
                            // Compute blade needs CR+LF for Enter key
                            enterEncodeCRLF = true;
                        }
                        else if (bladeInfo.bladeType == WcsCliConstants.BladeTypeJBOD)
                        {
                            // JBOD only needs CR for Enter key
                            enterEncodeCRLF = false;
                        }

                        // Open serial session
                        if (this.argVal.TryGetValue('s', out sessionTimeoutInSecs))
                        {
                            myResponse = WcsCli2CmConnectionManager.channel.StartBladeSerialSession((int)mySledId,
                                (int)sessionTimeoutInSecs);
                        }
                        else
                        {
                            myResponse = WcsCli2CmConnectionManager.channel.StartBladeSerialSession((int)mySledId,
                                _defaultTimeout);
                        }
                    }
                    else
                    {
                        myResponse.completionCode = bladeInfo.completionCode;
                        myResponse.statusDescription = bladeInfo.statusDescription;
                    }

                    // set blade Id
                    bladeId = (int)mySledId;
                }

                if (myResponse.completionCode == Contracts.CompletionCode.Success)
                {
                    if (myResponse.serialSessionToken != null)
                    {
                        // set the serial cache
                        SessionString = myResponse.serialSessionToken;

                        if (bladeId > 0)
                        {
                            // Console client
                            if (!this.isSerialClient)
                            {
                                // Automatically start into VT100 mode.
                                // This is a blocking method.
                                SetupVt100(enterEncodeCRLF);
                            }
                            else // Serial client
                            {
                                SharedFunc.SetSerialSession(true);
                                EstablishBladeSerialSessionOverSerialClient();
                            }
                            // When the setup ends, call a close session by default
                            // as the session should be destroyed.
                            WcsCli2CmConnectionManager.channel.StopBladeSerialSession(bladeId, SessionString);

                            // Display that serial session has ended
                            Console.WriteLine("Blade serial session ended..\n");
                        }
                        else
                        {
                            ErrorMessage("Failed to start serial session due to conversion of blade Id", false, false);
                        }
                        return;
                    }
                }
                else if (myResponse.completionCode == Contracts.CompletionCode.Failure)
                {
                    Console.WriteLine(WcsCliConstants.commandFailure);
                    return;
                }
                else if (myResponse.completionCode == Contracts.CompletionCode.Timeout)
                {
                    Console.WriteLine(WcsCliConstants.commandTimeout);
                    return;
                }
                else if (myResponse.completionCode == Contracts.CompletionCode.SerialSessionActive)
                {
                    Console.WriteLine(WcsCliConstants.commandSerialSessionActive);
                    return;
                }
                else if (myResponse.completionCode == Contracts.CompletionCode.FirmwareDecompressing)
                {
                    Console.WriteLine(WcsCliConstants.decompressing + (string.IsNullOrEmpty(myResponse.statusDescription) ? WcsCliConstants.defaultTimeout : myResponse.statusDescription));
                    return;
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.commandFailure + string.Format(" Completion Code: {0}", myResponse.completionCode));
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
            }

        }

        // Use C# destructor syntax for finalization code. 
        // This destructor will run only if the Dispose method 
        // does not get called. 
        // It gives your base class the opportunity to finalize. 
        // Do not provide destructors in types derived from this class.
        ~startBladeSerialSession()
        {
            // Do not re-create Dispose clean-up code here. 
            // Calling Dispose(false) is optimal in terms of 
            // readability and maintainability.
            Dispose(false);
        }

        // Implement IDisposable. 
        // A derived class should not be able to override this method. 
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. 
        // If disposing equals true, the method has been called directly 
        // or indirectly by a user's code. Managed and unmanaged resources 
        // can be disposed. 
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed. 
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    if (_vt100 != null)
                        _vt100 = null;
                }

                if (ended != null)
                    ended.Dispose();

                // Note disposing has been done.
                _disposed = true;
            }
        }


        public int sessionTimeoutInSecs { get; set; }
    }

    /// <summary>
    /// StopBladeSerialSession class
    /// </summary>
    internal class StopBladeSerialSession : command
    {
        internal StopBladeSerialSession()
        {
            this.name = WcsCliConstants.stopBladeSerialSession;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.stopBladeSerialSessionHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            try
            {
                dynamic bladeId = null;
                if (this.argVal.TryGetValue('i', out bladeId))
                {
                    // Force termination of any active serial session on the specified blade. 
                    ChassisResponse response = WcsCli2CmConnectionManager.channel.StopBladeSerialSession((int)bladeId, null, true);
                    CliSerialPort.ClearWindow();

                    ResponseValidation.ValidateResponse("StopBladeSerialSession completed successfully.", response, true);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.argsMissingString);
        }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }
        }
    }

    /// <summary>
    /// StartPortSerialSession class
    /// </summary>
    internal class startPortSerialSession : SerialSession, IDisposable
    {
        // Track whether Dispose has been called. 
        private bool _disposed = false;

        internal startPortSerialSession()
        {
            this.name = WcsCliConstants.startPortSerialSession;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('d', Type.GetType("System.Int32"));
            this.argSpec.Add('r', Type.GetType("System.Int32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.startPortSerialSessionHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        private AnsiEscape<startPortSerialSession> _vt100;

        /// <summary>
        /// WCS swtich port Id
        /// </summary>
        private int portId = 0;

        /// <summary>
        /// VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private const bool enterEncodeCRLF = true;  // Only one encoding used for port serial session.

        /// <summary>
        /// Switches to VT100 mode.
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private void SetupVt100(bool enterEncodeCRLF)
        {
            _vt100 = new AnsiEscape<startPortSerialSession>(this);

            // start the read on another threa.
            Thread receiver = new Thread(new ThreadStart(Receive));
            receiver.Start();

            // This method blocks.  The only way to exit is kill the process
            // or press Ctrl + X
            _vt100.ReadConsole(enterEncodeCRLF);

            // wait at most for 3 seconds for the read thread to end.
            ended.WaitOne(3000);

            // undo all VT100 console changes. fall through will reach this method.
            _vt100.RevertConsole();

            // dispose of the Vt100 class.
            _vt100 = null;

            // write the tear down message
            Console.WriteLine();
            Console.WriteLine(terminationMessage);
            Console.WriteLine();

        }

        /// <summary>
        /// Establish port serial session over Serial
        /// </summary>
        private void EstablishPortSerialSessionOverSerialClient()
        {
            // start the read on another thread.
            Thread receiver = new Thread(new ThreadStart(Receive));
            receiver.Start();

            // Read continually until activeRead signal escapes.
            while (SharedFunc.ActiveSerialSession)
            {
                // This method blocks. Get user input and send it to the blade
                Byte[] userEnteredData = CliSerialPort.ReadUserInputBytesFromSerial();
                // Check if the data has ctrl-X and exit 
                for (int i = 0; i < userEnteredData.Length; i++)
                {
                    // Snoop to see if the user entered Ctrl+X,
                    // which is 0x18 in VT100 encoding
                    if ((int)userEnteredData[i] == 24)
                    {
                        // Signal the receive thread to stop
                        SharedFunc.SetSerialSession(false);

                        // Stop the serial session
                        StopPortSerialSession stopCmd = new StopPortSerialSession();
                        stopCmd.isSerialClient = true;
                        stopCmd.argVal.Add('i', portId);
                        stopCmd.commandImplementation();

                        // clear the screen and input buffer while exiting.
                        CliSerialPort.ClearWindow();
                        CliSerialPort.ClearInputBuffer();

                        CliSerialPort.FlushPortBuffers();

                        break;
                    }
                }
                Send(userEnteredData);
            }
        }

        internal override void Send(byte[] payload)
        {
            // check session string and active write permission
            if (SessionString != string.Empty && SharedFunc.ActiveSerialSession && payload != null && payload.Length > 0)
            {
                ChassisResponse response = WcsCli2CmConnectionManager.channel.SendSerialPortData(portId, SessionString, payload);

                if ((int)response.completionCode != 0)
                {

                    // signals the session failed.
                    string msg = string.Format("Data Send Error failed with Completion Code: {0}.  See User Log for further information", 
                        response.completionCode);
                    ErrorMessage(msg, true, true);
                }

            }

        }

        protected override void Receive()
        {
            // check session string
            if (SessionString != string.Empty)
            {
                // Read continually until activeRead singal escapes.
                while (SharedFunc.ActiveSerialSession)
                {
                    try
                    {
                        SerialDataResponse response = WcsCli2CmConnectionManager.channel.ReceiveSerialPortData(portId, SessionString);

                        if (response.completionCode == 0 && response.data != null)
                        {
                            if (this.isSerialClient)
                            {
                                CliSerialPort.WriteBytestoSerial(response.data);
                            }
                            else
                            {
                                _vt100.SplitAnsiEscape(response.data);
                            }
                        }
                        // if completion code is anything other than a timeout (which is expected 
                        // when there is no data), return an error message and kill the serial session
                        else if (((int)response.completionCode != 0) &&
                                 ((int)response.completionCode != 0xA3))
                        {
                            // signals the session failed.
                            ErrorMessage(string.Format("Receiving Data failed with Completion Code: {0}.  See User Log for further information", 
                                response.completionCode), true, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // signals the session failed.
                        ErrorMessage(string.Format("Receiving Data failed with Exception: {0}.  ", 
                            ex.Message.ToString()), true, true);
                    }
                }
            }
            else
            {
                // signals the session failed.
                ErrorMessage("Serial Session was not correctly activated", true, true);
            }

            // signal the read thread has ended.
            ended.Set();
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            StartSerialResponse myResponse = new StartSerialResponse();
            try
            {
                if (this.argVal.ContainsKey('i'))
                {
                    dynamic myportId = null;
                    int sessionTimeoutInSecs = 0; // initializing it to 0 since we want to use the default value for this and not override it here
                    dynamic deviceTimeoutInMSecs = null;
                    dynamic baudRate = null;
                    this.argVal.TryGetValue('i', out myportId);
                    this.argVal.TryGetValue('d', out deviceTimeoutInMSecs);
                    this.argVal.TryGetValue('r', out baudRate);

                    // If the baud rate is not specified by the user, we use the default
                    if (!this.argVal.TryGetValue('r', out baudRate))
                    {
                        baudRate = 9600; // default baud rate when the user does not specify
                    }

                    if (!this.argVal.TryGetValue('d', out deviceTimeoutInMSecs))
                    {
                        deviceTimeoutInMSecs = 0;
                    }

                    myResponse = WcsCli2CmConnectionManager.channel.StartSerialPortConsole((int)myportId,
                        (int)sessionTimeoutInSecs, (int)deviceTimeoutInMSecs, (int)baudRate);

                    // set port Id
                    portId = (int)myportId;
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (myResponse.completionCode == Contracts.CompletionCode.Success)
            {
                if (myResponse.serialSessionToken != null)
                {
                    // set the serial cache
                    SessionString = myResponse.serialSessionToken;

                    if (portId > 0)
                    {
                        // Console client
                        if (!this.isSerialClient)
                        {
                            // Automatically start into VT100 mode.
                            // This is a blocking method.
                            SetupVt100(enterEncodeCRLF);
                        }
                        else // Serial client
                        {
                            SharedFunc.SetSerialSession(true);
                            EstablishPortSerialSessionOverSerialClient();
                        }

                        // When the setup ends, call a close session by default
                        // as the session should be destroyed.
                        WcsCli2CmConnectionManager.channel.StopSerialPortConsole(portId, SessionString, false);

                        // Display that serial session has ended
                        Console.WriteLine("Port serial session ended..\n");
                    }
                    else
                    {
                        ErrorMessage("failed to start serial session due to conversion of port Id", false, false);
                    }

                    return;
                }
            }
            ResponseValidation.ValidateResponse(null, myResponse, false);
        }

        // Use C# destructor syntax for finalization code. 
        // This destructor will run only if the Dispose method 
        // does not get called. 
        // It gives your base class the opportunity to finalize. 
        // Do not provide destructors in types derived from this class.
        ~startPortSerialSession()
        {
            // Do not re-create Dispose clean-up code here. 
            // Calling Dispose(false) is optimal in terms of 
            // readability and maintainability.
            Dispose(false);
        }

        // Implement IDisposable. 
        // A derived class should not be able to override this method. 
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. 
        // If disposing equals true, the method has been called directly 
        // or indirectly by a user's code. Managed and unmanaged resources 
        // can be disposed. 
        // If disposing equals false, the method has been called by the 
        // runtime from inside the finalizer and you should not reference 
        // other objects. Only unmanaged resources can be disposed. 
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    if (_vt100 != null)
                        _vt100 = null;
                }

                if (ended != null)
                    ended.Dispose();

                // Note disposing has been done.
                _disposed = true;
            }
        }

        }

    /// <summary>
    /// StopPortSerialSession class
    /// </summary>
    internal class StopPortSerialSession : command
    {
        internal StopPortSerialSession()
        {
            this.name = WcsCliConstants.stopPortSerialSession;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.stopPortSerialSessionHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            dynamic portId = null;

            try
            {
                if (this.argVal.TryGetValue('i', out portId))
                {
                    WcsCli2CmConnectionManager.channel.StopSerialPortConsole((int)portId, null, true);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.argsMissingString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }
        }
    }

    /// <summary>
    /// Read Chassis Log class
    /// </summary>
    internal class ReadChassisLog : command
    {
        internal ReadChassisLog()
        {
            this.name = WcsCliConstants.readChassisLog;
            this.argSpec.Add('h', null);
            this.argSpec.Add('s', Type.GetType("System.String"));
            this.argSpec.Add('e', Type.GetType("System.String"));
            this.helpString = WcsCliConstants.readchassislogHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            ChassisLogResponse myResponse = new ChassisLogResponse();

            dynamic startDateString = null;
            dynamic endDateString = null;
            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();

            // Initialize the start date for querying logs - default to start of .NET time when not provided by user
            if (!this.argVal.TryGetValue('s', out startDateString))
            {
                startDate = DateTime.MinValue;
            }
            else
            {
                DateTime.TryParse(((string)startDateString).Replace(":", "-"), out startDate);
            }

            // Initialize the end date for querying logs - default to end of .NET time when not provided by user
            if (!this.argVal.TryGetValue('e', out endDateString))
            {
                endDate = DateTime.MaxValue;
            }
            else
            {
                DateTime.TryParse(((string)endDateString).Replace(":", "-"), out endDate);
            }

            // Call appropriate chassis user log API based on presented inputs
            try
            {
                if (this.argVal.ContainsKey('s') || this.argVal.ContainsKey('e'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ReadChassisLogWithTimestamp(startDate, endDate);
                }
                else
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ReadChassisLog();
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                List<string> myStrings = new List<string>();
                Console.WriteLine(WcsCliConstants.readChassisLogHeader);
                myStrings.Add("Timestamp\t"); myStrings.Add("Entry");
                printTabSeparatedStrings(myStrings);
                foreach (LogEntry lg in myResponse.logEntries)
                {
                    myStrings.RemoveAll(item => (1 == 1));
                    myStrings.Add(lg.eventTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    myStrings.Add(lg.eventDescription.ToString());
                    printTabSeparatedStrings(myStrings);
                }
            }
        }
    }

    /// <summary>
    /// Clear Chassis Log class
    /// </summary>
    internal class ClearChassisLog : command
    {
        internal ClearChassisLog()
        {
            this.name = WcsCliConstants.clearChassisLog;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.clearchassislogHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.ClearChassisLog();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Clear log completed successfully", myResponse, true);
        }
    }

    /// <summary>
    /// Read blade log class
    /// </summary>
    internal class ReadBladeLog : command
    {
        internal ReadBladeLog()
        {
            this.name = WcsCliConstants.readBladeLog;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('n', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.readbladelogHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            ChassisLogResponse myResponse = new ChassisLogResponse();
            dynamic myNumberEntries = null;

            if (!this.argVal.TryGetValue('n', out myNumberEntries))
            {
                myNumberEntries = WcsCliConstants.readBladeLogNumberOfEntries;
            }

            try
            {
                if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    myResponse = WcsCli2CmConnectionManager.channel.ReadBladeLog((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                // Print header
                List<string> myStrings = new List<string>();
                Console.WriteLine(WcsCliConstants.readBladeLogHeader);
                myStrings.Add("Timestamp\t"); myStrings.Add("Entry");
                printTabSeparatedStrings(myStrings);

                // Print all SEL entries up to the number specified by the user
                int numEntriesToPrint = ((int)myNumberEntries > myResponse.logEntries.Count) ?
                                        myResponse.logEntries.Count : (int)myNumberEntries;
                for (int index = 0; index < numEntriesToPrint; index++)
                {
                    myStrings.RemoveAll(item => (1 == 1));
                    myStrings.Add(myResponse.logEntries[index].eventTime.ToString());
                    myStrings.Add(myResponse.logEntries[index].eventDescription);
                    printTabSeparatedStrings(myStrings);
                }
            }
        }
    }

    /// <summary>
    /// Clear blade log class
    /// </summary>
    internal class ClearBladeLog : command
    {
        internal ClearBladeLog()
        {
            this.name = WcsCliConstants.clearBladeLog;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.clearbladelogHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeResponse myResponse = new BladeResponse();

            try
            {
                dynamic mySledId = null;
                this.argVal.TryGetValue('i', out mySledId);
                myResponse = WcsCli2CmConnectionManager.channel.ClearBladeLog((int)mySledId);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, true);
        }
    }

    /// <summary>
    /// Add User class
    /// </summary>
    internal class adduser : command
    {
        internal adduser()
        {
            this.name = WcsCliConstants.adduser;
            this.argSpec.Add('u', Type.GetType("System.String"));
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('o', null);
            this.argSpec.Add('r', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.adduserHelp;

            this.conditionalOptionalArgs.Add('u', null);
            this.conditionalOptionalArgs.Add('p', null);
            this.conditionalOptionalArgs.Add('a', new char[] { 'r', 'o' });
            this.conditionalOptionalArgs.Add('o', new char[] { 'a', 'r' });
            this.conditionalOptionalArgs.Add('r', new char[] { 'a', 'o' });
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic uname = null;
            dynamic pword = null;

            try
            {
                if (this.argVal.TryGetValue('u', out uname) && this.argVal.TryGetValue('p', out pword) && this.argVal.ContainsKey('a'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.AddChassisControllerUser((string)uname, (string)pword, WCSSecurityRole.WcsCmAdmin);

                }
                else if (this.argVal.TryGetValue('u', out uname) && this.argVal.TryGetValue('p', out pword) && this.argVal.ContainsKey('o'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.AddChassisControllerUser((string)uname, (string)pword, WCSSecurityRole.WcsCmOperator);
                }
                else if (this.argVal.TryGetValue('u', out uname) && this.argVal.TryGetValue('p', out pword) && this.argVal.ContainsKey('u'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.AddChassisControllerUser((string)uname, (string)pword, WCSSecurityRole.WcsCmUser);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Chassis Manager User Added", myResponse, true);
        }
    }

    /// <summary>
    /// Change user role class
    /// </summary>
    internal class ChangeUserRole : command
    {
        internal ChangeUserRole()
        {
            this.name = WcsCliConstants.changeuserrole;
            this.argSpec.Add('u', Type.GetType("System.String"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('o', null);
            this.argSpec.Add('r', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.changeuserroleHelp;

            this.conditionalOptionalArgs.Add('u', null);
            this.conditionalOptionalArgs.Add('a', new char[] { 'r', 'o' });
            this.conditionalOptionalArgs.Add('o', new char[] { 'a', 'r' });
            this.conditionalOptionalArgs.Add('r', new char[] { 'a', 'o' });
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic uname = null;

            try
            {
                if (this.argVal.TryGetValue('u', out uname) && this.argVal.ContainsKey('a'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ChangeChassisControllerUserRole((string)uname, WCSSecurityRole.WcsCmAdmin);
                }
                else if (this.argVal.TryGetValue('u', out uname) && this.argVal.ContainsKey('o'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ChangeChassisControllerUserRole((string)uname, WCSSecurityRole.WcsCmOperator);
                }
                else if (this.argVal.TryGetValue('u', out uname) && this.argVal.ContainsKey('r'))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ChangeChassisControllerUserRole((string)uname, WCSSecurityRole.WcsCmUser);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Chassis Manager User Role Changed", myResponse, true);
        }
    }

    /// <summary>
    /// Change user password class
    /// </summary>
    internal class ChangeUserPassword : command
    {
        internal ChangeUserPassword()
        {
            this.name = WcsCliConstants.changeuserpassword;
            this.argSpec.Add('u', Type.GetType("System.String"));
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.changeUserPwdHelp;

            this.conditionalOptionalArgs.Add('u', null);
            this.conditionalOptionalArgs.Add('p', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic uname = null;
            dynamic newpword = null;

            try
            {
                if (this.argVal.TryGetValue('u', out uname) && this.argVal.TryGetValue('p', out newpword))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.ChangeChassisControllerUserPassword((string)uname, (string)newpword);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Chassis Manager User Password Changed", myResponse, true);
        }
    }

    /// <summary>
    /// Remove user class
    /// </summary>
    internal class removeuser : command
    {
        internal removeuser()
        {
            this.name = WcsCliConstants.removeuser;
            this.argSpec.Add('u', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.removeuserHelp;

            this.conditionalOptionalArgs.Add('u', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.ChassisResponse myResponse = new Contracts.ChassisResponse();
            dynamic uname = null;

            try
            {
                if (this.argVal.TryGetValue('u', out uname))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.RemoveChassisControllerUser((string)uname);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Chassis Manager User Removed", myResponse, true);
        }
    }

    /// <summary>
    /// Network Interface class
    /// </summary>
    static internal class NetworkInterfaces
    {
        static uint numberInterfaces = 2;

        public class NetworkPropertiesResponse
        {
            public List<NetworkProperty> NetworkPropertyCollection = new List<NetworkProperty>();
            public CompletionCode completionCode;
        }

        public class NetworkProperty
        {
            public uint index;
            public string description = String.Empty;
            public string serviceName = String.Empty;
            public string macAddress = String.Empty;
            public bool ipEnabled;
            public string[] ipAddress;
            public string[] subnetMask;
            public string[] gatewayAddress;
            public bool dhcpEnabled;
            public string dhcpServer = String.Empty;
            public string dnsHostName = String.Empty;
            public string dnsDomain = String.Empty;
            public string[] dnsSearchOrder;
            public string winsPrimary = String.Empty;
            public string winsSecondary = String.Empty;
            public CompletionCode completionCode;
        }

        /// <summary>
        /// Get Network parameters
        /// </summary>
        static internal NetworkPropertiesResponse GetNetworkProperties()
        {
            NetworkPropertiesResponse response = new NetworkPropertiesResponse();
            response.NetworkPropertyCollection = new List<NetworkProperty>();

            // Create management class object using the Win32_NetworkAdapterConfiguration
            // class to retrieve different attributes of the network adapters
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");

            // Create ManagementObjectCollection to retrieve the attributes
            ManagementObjectCollection moc = mc.GetInstances();

            // Set default completion code to unknown.
            response.completionCode = Contracts.CompletionCode.Unknown;

            try
            {
                foreach (ManagementObject mo in moc)
                {
                    // Physical network adapters have MAC addresses. Other Network Adapters will have null MAC addresses.
                    if (!string.IsNullOrEmpty((string)mo["MACAddress"]))
                    {
                        NetworkProperty cr = new NetworkProperty();
                        cr.index = (uint)mo["Index"];
                        cr.description = (string)mo["Description"];
                        cr.serviceName = (string)mo["ServiceName"];
                        cr.macAddress = (string)mo["MACAddress"];
                        cr.ipEnabled = (bool)mo["IPEnabled"];
                        cr.ipAddress = (string[])mo["IPAddress"];
                        cr.subnetMask = (string[])mo["IPSubnet"];
                        cr.gatewayAddress = (string[])mo["DefaultIPGateway"];
                        cr.dhcpEnabled = (bool)mo["DHCPEnabled"];
                        cr.dhcpServer = (string)mo["DHCPServer"];
                        cr.dnsHostName = (string)mo["DNSHostName"];
                        cr.dnsDomain = (string)mo["DNSDomain"];
                        cr.dnsSearchOrder = (string[])mo["DNSServerSearchOrder"];
                        cr.winsPrimary = (string)mo["WINSPrimaryServer"];
                        cr.winsSecondary = (string)mo["WINSSecondaryServer"];
                        cr.completionCode = Contracts.CompletionCode.Success;
                        response.NetworkPropertyCollection.Add(cr);
                    }
                }
            }
            catch (Exception ex)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Console.WriteLine("Get NIC properties failed with message " + ex.Message);
                return response;
            }

            response.completionCode = Contracts.CompletionCode.Success;
            return response;
        }

        static bool checkIpFormat(string IpAddress)
        {
            System.Net.IPAddress ipAdd;
            if (System.Net.IPAddress.TryParse(IpAddress, out ipAdd))
            {
                if (ipAdd.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Set Network parameters
        /// </summary>
        static internal NetworkPropertiesResponse SetNetworkProperties(
            string ipSource, string IpAddress, string SubnetMask,
            string DNSPrimary, string DNSSecondary, string Gateway, uint interfaceNumber)
        {

            NetworkPropertiesResponse response = new NetworkPropertiesResponse();
            // Set default completion code to unknown.
            response.completionCode = Contracts.CompletionCode.Unknown;

            if (interfaceNumber >= numberInterfaces)
            {
                Console.WriteLine("Invalid Network Interface number. Network Interface properties cannot be changed.");
                response.completionCode = Contracts.CompletionCode.Failure;
                return response;
            }

            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    // Physical network adapters have MAC addresses. Other Network Adapters will have null MAC addresses.
                    if (!string.IsNullOrEmpty((string)mo["MACAddress"]))
                    {
                        int logicalIndex = -1;
                        uint index = (uint)mo["Index"];
                        if (index >= 0)
                        {
                            logicalIndex = (int)index;
                        }
                        else
                        {
                            // Failed to verify Index
                            Console.WriteLine(@"Could not obtain network controller interface Index.");
                            response.completionCode = Contracts.CompletionCode.Failure;
                            return response;
                        }

                        // Configure specified network interface
                        if ((int)interfaceNumber == Contracts.SharedFunc.NetworkCtrlPhysicalIndex(logicalIndex))
                        {
                            // Check parameters before doing any configuration
                            // Trim white spaces from IP addresses and check format
                            bool ipCheck = true;
                            if (!string.IsNullOrEmpty(DNSPrimary))
                            {
                                DNSPrimary = DNSPrimary.Trim();
                                ipCheck = ipCheck & checkIpFormat(DNSPrimary);

                                // Secondary DNS is only configured if primary DNS is specified
                                if (!string.IsNullOrEmpty(DNSSecondary))
                                {
                                    DNSSecondary = DNSSecondary.Trim();
                                    ipCheck = ipCheck & checkIpFormat(DNSSecondary);
                                }
                            }
                            if (!ipCheck)
                            {
                                Console.WriteLine(@"Invalid DNS Server Parameters. Network interface properties cannot be changed.");
                                response.completionCode = Contracts.CompletionCode.Failure;
                                return response;
                            }

                            // Configure network properties for DHCP or static IP
                            if (string.Equals(ipSource, "DHCP",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("Configuring DHCP ...");
                                ManagementBaseObject enableDHCP = mo.InvokeMethod("EnableDHCP", null, null);
                                response.completionCode = checkWin32_CmdReturnVal((uint)enableDHCP["returnValue"], "EnableDHCP");
                            }
                            else if (string.Equals(ipSource, "STATIC",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                // Trim white spaces from IP addresses and check format
                                bool staticIpCheck = true;
                                IpAddress = IpAddress.Trim();
                                SubnetMask = SubnetMask.Trim();

                                staticIpCheck = staticIpCheck & checkIpFormat(IpAddress);
                                staticIpCheck = staticIpCheck & checkIpFormat(SubnetMask);
                                if (!string.IsNullOrEmpty(Gateway))
                                {
                                    Gateway = Gateway.Trim();
                                    staticIpCheck = staticIpCheck & checkIpFormat(Gateway);
                                }
                                if (!staticIpCheck)
                                {
                                    Console.WriteLine(@"Invalid Input Network Parameters. Network interface properties cannot be changed.");
                                    response.completionCode = Contracts.CompletionCode.Failure;
                                    return response;
                                }

                                Console.WriteLine("Configuring static IP address ...");
                                object enableStatic =
                                    mo.InvokeMethod("EnableStatic",
                                                    new object[] {new string[] { IpAddress },
                                                                  new string[] { SubnetMask } });
                                response.completionCode = checkWin32_CmdReturnVal((uint)enableStatic, "EnableStatic");

                                // Get the SetGateways method parameters
                                ManagementBaseObject newGateWay =
                                    mo.GetMethodParameters("SetGateways");
                                newGateWay["DefaultIPGateway"] = null;
                                if (!string.IsNullOrEmpty(Gateway))
                                {
                                    // Set Gateway to user specified value
                                    newGateWay["DefaultIPGateway"] = new string[] { Gateway };
                                    newGateWay["GatewayCostMetric"] = new int[] { 1 };
                                }
                                if (response.completionCode == Contracts.CompletionCode.Success)
                                {
                                    ManagementBaseObject setGateways =
                                        mo.InvokeMethod("SetGateways", newGateWay, null);
                                    response.completionCode = checkWin32_CmdReturnVal((uint)setGateways["returnValue"], "SetGateways");
                                }
                            }
                            else
                            {
                                // Not DHCP and not static IP
                                Console.WriteLine(@"Invalid IP Source type.");
                                response.completionCode = Contracts.CompletionCode.Failure;
                                return response;
                            }

                            if (response.completionCode == Contracts.CompletionCode.Success)
                            {
                                // Retrieve Set DNS Server method
                                ManagementBaseObject newDNS =
                                    mo.GetMethodParameters("SetDNSServerSearchOrder");
                                newDNS["DNSServerSearchOrder"] = null;

                                // Set DNS Server Search Order if specified
                                if (!string.IsNullOrEmpty(DNSPrimary))
                                {
                                    string[] DNSServerSearchOrder;
                                    // Secondary DNS is only configured if primary DNS is specified
                                    if (!string.IsNullOrEmpty(DNSSecondary))
                                    {
                                        DNSServerSearchOrder = new string[] { DNSPrimary, DNSSecondary };
                                    }
                                    else
                                    {
                                        DNSServerSearchOrder = new string[] { DNSPrimary };
                                    }
                                    newDNS["DNSServerSearchOrder"] = DNSServerSearchOrder;
                                }

                                // Set DNS Server Search order
                                ManagementBaseObject setDNS =
                                    mo.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                                response.completionCode = checkWin32_CmdReturnVal((uint)setDNS["returnValue"], "SetDNSServerSearchOrder");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.completionCode = Contracts.CompletionCode.Failure;
                Console.WriteLine(WcsCliConstants.commandFailure + " Exception encountered: " + ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Checks the return value from the Win32_NetworkAdapterConfiguration  method
        /// </summary>
        /// <param name="returnValue">The return value.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns>Completion code corresponding to the return value</returns>
        static private CompletionCode checkWin32_CmdReturnVal(uint returnValue, string methodName)
        {
            CompletionCode returnCode = Contracts.CompletionCode.Success;

            if (returnValue == 0)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + string.Format("{0} completed successfully.", methodName));
            }
            else if (returnValue == 1)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + string.Format("{0} completed successfully. Reboot required for settings to take effect.", methodName));
            }
            else if (returnValue == 84)
            {
                returnCode = Contracts.CompletionCode.Failure;
                Console.WriteLine(WcsCliConstants.commandFailure + string.Format("{0} failed with return value {1}. IP not enabled on adapter.", methodName, returnValue));
                Console.WriteLine("Network adapter must be connected to the network for enabling DHCP or setting DNS servers.");
            }
            else
            {
                returnCode = Contracts.CompletionCode.Failure;
                Console.WriteLine(WcsCliConstants.commandFailure + string.Format("{0} failed with return value {1}", methodName, returnValue));
            }
            return returnCode;
        }
    }

    /// <summary>
    /// Get NIC configuration class
    /// </summary>
    internal class GetNetworkProperties : command
    {
        internal GetNetworkProperties()
        {
            this.name = WcsCliConstants.getNetworkProperties;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getNetworkPropertiesHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode. Use native windows APIs for getting NIC info.");
                return;
            }

            NetworkInterfaces.NetworkPropertiesResponse myResponse = new NetworkInterfaces.NetworkPropertiesResponse();
            try
            {
                myResponse = NetworkInterfaces.GetNetworkProperties();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            // Always print out CLI command name and completion status so that
            // drivers that depend on parsing the text response can determine the 
            // success or failure of the command
            if (myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                Console.WriteLine(WcsCliConstants.commandFailure + " wcscli -getnic: " + Contracts.CompletionCode.Failure);
            }
            else if (myResponse.completionCode != Contracts.CompletionCode.Success)
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " wcscli -getnic: " + myResponse.completionCode);
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);

                foreach (NetworkInterfaces.NetworkProperty res in myResponse.NetworkPropertyCollection)
                {
                    if (res.completionCode == Contracts.CompletionCode.Success)
                    {
                        Console.WriteLine("N/W Interface {0}: ", Contracts.SharedFunc.NetworkCtrlPhysicalIndex((int)res.index));
                        Console.WriteLine("\tIndex\t\t\t: " + res.index);
                        Console.WriteLine("\tDescription\t\t: " + res.description);
                        Console.WriteLine("\tServiceName\t\t: " + res.serviceName);
                        Console.WriteLine("\tMAC Address\t\t: " + res.macAddress);
                        Console.WriteLine("\tIP Enabled\t\t: " + res.ipEnabled.ToString());
                        Console.WriteLine("\tDHCP Enabled\t\t: " + res.dhcpEnabled.ToString());
                        Console.WriteLine("\tDHCP Server\t\t: " + res.dhcpServer);

                        Dictionary<string, string[]> netProp = new Dictionary<string, string[]>()
                                                           {{"IP Address", res.ipAddress},
                                                            {"Subnet Mask", res.subnetMask},
                                                            {"Gateway Address", res.gatewayAddress},
                                                            {"DNS Servers", res.dnsSearchOrder}};
                        foreach (KeyValuePair<string, string[]> pair in netProp)
                        {
                            Console.Write("\t" + pair.Key + "\t\t:");
                            if (pair.Value != null)
                            {
                                Console.Write(" {");
                                for (int idx = 0; idx < pair.Value.Length; idx++)
                                {
                                    Console.Write("{0}", pair.Value[idx]);
                                    if (idx < (pair.Value.Length - 1))
                                    {
                                        Console.Write(", ");
                                    }
                                }
                                Console.Write("}");
                            }
                            Console.WriteLine("");
                        }

                        Console.WriteLine("\tDNS Hostname\t\t: " + res.dnsHostName);
                        Console.WriteLine("\tDNS Domain\t\t: " + res.dnsDomain);
                        Console.WriteLine("\tWINS Primary\t\t: " + res.winsPrimary);
                        Console.WriteLine("\tWINS Secondary\t\t: " + res.winsSecondary);
                        // Always print completion code as the last item since other drivers
                        // use this to look for the complete set of information for one NIC
                        Console.WriteLine("\tCompletion Code\t\t: " + res.completionCode);
                        Console.WriteLine();

                    }
                }
            }
            Console.WriteLine("Completion Code: " + myResponse.completionCode);
        }
    }

    /// <summary>
    /// Set NIC configuration class
    /// </summary>
    internal class SetNetworkProperties : command
    {
        internal SetNetworkProperties()
        {
            this.name = WcsCliConstants.setNetworkProperties;
            this.argSpec.Add('i', Type.GetType("System.String"));
            this.argSpec.Add('m', Type.GetType("System.String"));
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('d', Type.GetType("System.String"));
            this.argSpec.Add('g', Type.GetType("System.String"));
            this.argSpec.Add('a', Type.GetType("System.String"));
            this.argSpec.Add('t', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setNetworkPropertiesHelp;

            this.conditionalOptionalArgs.Add('a', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            NetworkInterfaces.NetworkPropertiesResponse myResponse = new NetworkInterfaces.NetworkPropertiesResponse();
            dynamic ipSource = null;
            dynamic newIp = null;
            dynamic netMask = null;
            dynamic primaryDNS = null;
            dynamic secondaryDNS = null;
            dynamic newGateway = null;
            dynamic interfaceId = null;
            try
            {
                // Get IP address source (static/DHCP)
                this.argVal.TryGetValue('a', out ipSource);

                if (string.Equals((string)ipSource, "STATIC",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (this.argVal.TryGetValue('i', out newIp) &&
                        this.argVal.TryGetValue('m', out netMask))
                    {
                        this.argVal.TryGetValue('p', out primaryDNS);
                        this.argVal.TryGetValue('d', out secondaryDNS);
                        this.argVal.TryGetValue('g', out newGateway);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.invalidCommandString);
                        Console.WriteLine(WcsCliConstants.commandFailure + " All required parameters not supplied");
                        return;
                    }
                }

                // Get network interface ID
                if (!this.argVal.TryGetValue('t', out interfaceId))
                {
                    interfaceId = 0; // set the default network interface id as 0
                }

                // Set network properties
                myResponse = NetworkInterfaces.SetNetworkProperties(
                    (string)ipSource, (string)newIp, (string)netMask,
                    (string)primaryDNS, (string)secondaryDNS, (string)newGateway, (uint)interfaceId);
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            // Always print out CLI command name and completion status so that
            // drivers that depend on parsing the text response can determine the 
            // success or failure of the command
            if (myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                Console.WriteLine(WcsCliConstants.commandFailure + " wcscli -setnic: " + Contracts.CompletionCode.Failure);
            }
            else if (myResponse.completionCode == CompletionCode.Success)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + "wcscli -setnic: " + myResponse.completionCode);
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " wcscli -setnic: " + myResponse.completionCode);
            }
        }
    }

    /// <summary>
    /// Clear history and clear window. Only supported in WcsCLI serial mode.
    /// </summary>
    internal class Clear : command
    {
        internal Clear()
        {
            this.name = WcsCliConstants.clear;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.clearHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            // reset history buffer
            CliSerialPort.ClearHistory();

            // clear screen
            CliSerialPort.ClearWindow();
        }
    }

    /// <summary>
    /// GetServiceVersion class
    /// </summary>
    internal class GetServiceVersion : command
    {
        internal GetServiceVersion()
        {
            this.name = WcsCliConstants.getserviceversion;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getServiceVersionHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            ServiceVersionResponse myResponse = new ServiceVersionResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.GetServiceVersion();
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " Chassis Manager Service version: " + myResponse.serviceVersion);
            }
        }


    }

    /// <summary>
    /// GetNextBoot class
    /// </summary>
    internal class GetNextBoot : command
    {
        internal GetNextBoot()
        {
            this.name = WcsCliConstants.getnextboot;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getnextbootHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.BootResponse myResponse = new Contracts.BootResponse();
            dynamic bladeId = null;

            try
            {
                if (this.argVal.TryGetValue('i', out bladeId))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.GetNextBoot((int)bladeId);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + "Next boot is " + myResponse.nextBoot);
            }

        }
    }

    /// <summary>
    /// SetNextBoot class
    /// </summary>
    internal class SetNextBoot : command
    {
        internal SetNextBoot()
        {
            this.name = WcsCliConstants.setnextboot;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('t', Type.GetType("System.UInt32"));
            this.argSpec.Add('m', Type.GetType("System.UInt32"));
            this.argSpec.Add('p', Type.GetType("System.UInt32"));
            this.argSpec.Add('n', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setnextbootHelp;

            this.conditionalOptionalArgs.Add('i', null);
            this.conditionalOptionalArgs.Add('t', null);
            this.conditionalOptionalArgs.Add('m', null);
            this.conditionalOptionalArgs.Add('p', null);
            this.conditionalOptionalArgs.Add('n', null);
        }

        internal Contracts.BladeBootType getBootType(int varType)
        {
            //1. NoOverRide = 0x00,
            if (varType == 1)
                return Contracts.BladeBootType.NoOverride;
            //2. ForcePxe = 0x04,
            else if (varType == 2)
                return Contracts.BladeBootType.ForcePxe;
            //3. ForceDefaultHdd = 0x08,
            else if (varType == 3)
                return Contracts.BladeBootType.ForceDefaultHdd;
            //4. ForceIntoBiosSetup = 0x0c,
            else if (varType == 4)
                return Contracts.BladeBootType.ForceIntoBiosSetup;
            //5. ForceFloppyOrRemovable = 0x10,
            else if (varType == 5)
                return Contracts.BladeBootType.ForceFloppyOrRemovable;
            //X. Unknown = 0xff
            else
                return Contracts.BladeBootType.Unknown;
        }

        internal bool getIsPersistent(int varPersist)
        {
            if (varPersist <= 0)
                return false;
            else
                return true;
        }

        internal bool getIsUefi(int uefi)
        {
            if (uefi <= 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            Contracts.BootResponse myResponse = new Contracts.BootResponse();
            dynamic bootType = null;
            dynamic bladeId = null;
            dynamic uefi = null;
            dynamic isPersistent = null;
            dynamic bootInstance = null;

            try
            {
                if (this.argVal.TryGetValue('i', out bladeId) && this.argVal.TryGetValue('t', out bootType) && this.argVal.TryGetValue('m', out uefi) && this.argVal.TryGetValue('p', out isPersistent) && this.argVal.TryGetValue('n', out bootInstance))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.SetNextBoot((int)bladeId, getBootType((int)bootType), getIsUefi((int)uefi), (bool)getIsPersistent((int)isPersistent), (int)bootInstance);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.invalidCommandString);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " Next boot is " + myResponse.nextBoot);
            }

        }
    }

    /// <summary>
    /// Start Chassis Manager service class
    /// </summary>
    internal class StartChassisManagerService : command
    {
        internal StartChassisManagerService()
        {
            this.name = WcsCliConstants.startchassismanager;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.startchassismanagerHelp;
        }

        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            bool status = WcsCli2CmConnectionManager.StartChassisManager();
            if (status)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + "chassismanager service successfully started");
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
            }
        }
    }

    /// <summary>
    /// Stop Chassis Manager service class
    /// </summary>
    internal class StopChassisManagerService : command
    {
        internal StopChassisManagerService()
        {
            this.name = WcsCliConstants.stopchassismanager;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.stopchassismanagerHelp;
        }

        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            bool status = WcsCli2CmConnectionManager.StopChassisManager();
            if (status == true)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " chassismanager service successfully stopped.");
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
            }
        }
    }

    /// <summary>
    /// Get Chassis Manager service status class
    /// </summary>
    internal class GetCMServiceStatus : command
    {
        internal GetCMServiceStatus()
        {
            this.name = WcsCliConstants.getchassismanagerstatus;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getchassismanagerstatusHelp;
        }

        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            try
            {
                ServiceController controller = new ServiceController("chassismanager");
                Console.WriteLine(WcsCliConstants.commandSuccess + "chassismanager service status: " + controller.Status);
            }
            catch (Exception)
            {
                Console.WriteLine(WcsCliConstants.commandFailure + " chassismanager service status query failed");
            }
        }
    }

    /// <summary>
    /// Enable SSL class
    /// </summary>
    internal class EnableSSL : command
    {
        internal EnableSSL()
        {
            this.name = WcsCliConstants.enablessl;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.enablesslHelp;
        }

        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            bool status = WcsCli2CmConnectionManager.SetSSL(true);
            if (status)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " Successfully enabled SSL in the chassismanager service.");
                Console.WriteLine("");
                Console.WriteLine("You will need to establish connection to the CM again via ({0}) command to run any commands..", WcsCliConstants.establishCmConnection);
                Console.WriteLine("");
                Console.WriteLine(WcsCliConstants.establishCmConnectionHelp);
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
            }
        }
    }

    /// <summary>
    /// Disable SSL class
    /// </summary>
    internal class DisableSSL : command
    {
        internal DisableSSL()
        {
            this.name = WcsCliConstants.disablessl;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.disablesslHelp;
        }

        internal override void commandImplementation()
        {
            if (!this.isSerialClient)
            {
                Console.WriteLine("Command only supported in serial WcsCli client mode.");
                return;
            }

            bool status = WcsCli2CmConnectionManager.SetSSL(false);
            if (status)
            {
                Console.WriteLine(WcsCliConstants.commandSuccess + " Successfully disabled SSL in the chassismanager service.");
                Console.WriteLine("");
                Console.WriteLine("You will need to establish connection to the CM again via ({0}) command to run any commands..", WcsCliConstants.establishCmConnection);
                Console.WriteLine("");
                Console.WriteLine(WcsCliConstants.establishCmConnectionHelp);
            }
            else
            {
                Console.WriteLine(WcsCliConstants.commandFailure);
            }
        }
    }

    /// <summary>
    /// Gets Chassis Manager FRU areas information
    /// </summary>
    internal class GetChassisAssetInfo : command
    {
        internal GetChassisAssetInfo()
        {
            this.name = WcsCliConstants.getCMAssetInfo;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getCMAssetInfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            ChassisAssetInfoResponse myResponse = new ChassisAssetInfoResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.GetChassisManagerAssetInfo();
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                Console.WriteLine();
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Chassis Manager Chassis Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Chassis Part Number = " + myResponse.chassisAreaPartNumber);
                Console.WriteLine(
                    "Chassis Serial Number = " + myResponse.chassisAreaSerialNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Chassis Manager Board Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Board Manufacturer Name = " + myResponse.boardAreaManufacturerName);
                Console.WriteLine(
                    "Board Manufacturer Date = " + myResponse.boardAreaManufacturerDate);
                Console.WriteLine(
                    "Board Product Name = " + myResponse.boardAreaProductName);
                Console.WriteLine(
                    "Board Serial Number = " + myResponse.boardAreaSerialNumber);
                Console.WriteLine(
                    "Board Part Number = " + myResponse.boardAreaPartNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Chassis Manager Product Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Product Manufacturer Name = " + myResponse.productAreaManufactureName);
                Console.WriteLine(
                    "Product Product Name = " + myResponse.productAreaProductName);
                Console.WriteLine(
                    "Product Part/Model Number = " + myResponse.productAreaPartModelNumber);
                Console.WriteLine(
                    "Product Version = " + myResponse.productAreaProductVersion);
                Console.WriteLine(
                    "Product Serial Number = " + myResponse.productAreaSerialNumber);
                Console.WriteLine("PD Product Asset Tag = "
                    + myResponse.productAreaAssetTag);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Chassis Manager Multi Record Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Manufacturer = " + myResponse.manufacturer);
                Console.WriteLine("Custom Fields");
                Console.WriteLine(string.Join("\n", myResponse.multiRecordFields.ToArray()));
            }
        }
    }

    /// <summary>
    /// Gets PDB FRU areas information
    /// </summary>
    internal class GetPDBAssetInfo : command
    {
        internal GetPDBAssetInfo()
        {
            this.name = WcsCliConstants.getPDBAssetInfo;
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getPDBAssetInfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            ChassisAssetInfoResponse myResponse = new ChassisAssetInfoResponse();
            try
            {
                myResponse = WcsCli2CmConnectionManager.channel.GetPdbAssetInfo();
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateResponse(null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                Console.WriteLine();
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Power Distribution Board (PDB) Chassis Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Chassis Part Number = " + myResponse.chassisAreaPartNumber);
                Console.WriteLine(
                    "Chassis Serial Number = " + myResponse.chassisAreaSerialNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Power Distribution Board (PDB) Board Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Board Manufacturer Name = " + myResponse.boardAreaManufacturerName);
                Console.WriteLine(
                    "Board Manufacturer Date = " + myResponse.boardAreaManufacturerDate);
                Console.WriteLine(
                    "Board Product Name = " + myResponse.boardAreaProductName);
                Console.WriteLine(
                    "Board Serial Number = " + myResponse.boardAreaSerialNumber);
                Console.WriteLine(
                    "Board Part Number = " + myResponse.boardAreaPartNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Power Distribution Board (PDB) Product Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Product Manufacturer Name = " + myResponse.productAreaManufactureName);
                Console.WriteLine(
                    "Product Product Name = " + myResponse.productAreaProductName);
                Console.WriteLine(
                    "Product Part/Model Number = " + myResponse.productAreaPartModelNumber);
                Console.WriteLine(
                    "Product Version = " + myResponse.productAreaProductVersion);
                Console.WriteLine(
                    "Product Serial Number = " + myResponse.productAreaSerialNumber);
                Console.WriteLine("PD Product Asset Tag = "
                    + myResponse.productAreaAssetTag);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Power Distribution Board (PDB) Multi Record Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Manufacturer = " + myResponse.manufacturer);
                Console.WriteLine("Custom Fields");
                Console.WriteLine(string.Join("\n", myResponse.multiRecordFields.ToArray()));
            }
        }
    }

    /// <summary>
    /// Gets Blade FRU areas information
    /// </summary>
    internal class GetBladeAssetInfo : command
    {
        internal GetBladeAssetInfo()
        {
            this.name = WcsCliConstants.getBladeAssetInfo;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeAssetInfoHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            BladeAssetInfoResponse myResponse = new BladeAssetInfoResponse();
            try
            {
                if (!(this.argVal.ContainsKey('i')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No blade ID specified, please look at command help.");
                    return;
                }
                else
                {
                    dynamic bladeId = null;
                    this.argVal.TryGetValue('i', out bladeId);
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladeAssetInfo((int)bladeId);
                }
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                Console.WriteLine();
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Blade Chassis Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Chassis Part Number = " + myResponse.chassisAreaPartNumber);
                Console.WriteLine(
                    "Chassis Serial Number = " + myResponse.chassisAreaSerialNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Blade Board Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Board Manufacturer Name = " + myResponse.boardAreaManufacturerName);
                Console.WriteLine(
                    "Board Manufacturer Date = " + myResponse.boardAreaManufacturerDate);
                Console.WriteLine(
                    "Board Product Name = " + myResponse.boardAreaProductName);
                Console.WriteLine(
                    "Board Serial Number = " + myResponse.boardAreaSerialNumber);
                Console.WriteLine(
                    "Board Part Number = " + myResponse.boardAreaPartNumber);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Blade Product Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Product Manufacturer Name = " + myResponse.productAreaManufactureName);
                Console.WriteLine(
                    "Product Product Name = " + myResponse.productAreaProductName);
                Console.WriteLine(
                    "Product Part/Model Number = " + myResponse.productAreaPartModelNumber);
                Console.WriteLine(
                    "Product Version = " + myResponse.productAreaProductVersion);
                Console.WriteLine(
                    "Product Serial Number = " + myResponse.productAreaSerialNumber);
                Console.WriteLine("PD Product Asset Tag = "
                    + myResponse.productAreaAssetTag);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();

                Console.WriteLine("Multi Record Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Manufacturer = " + myResponse.manufacturer);
                Console.WriteLine("Custom Fields");
                Console.WriteLine(string.Join("\n", myResponse.multiRecordFields.ToArray()));
            }
        }
    }

    /// <summary>
    /// Gets Blade Mezz FRU areas information
    /// </summary>
    internal class GetBladeMezzAssetInfo : command
    {
        internal GetBladeMezzAssetInfo()
        {
            this.name = WcsCliConstants.getBladeMezzAssetInfo;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeMezzAssetInfoHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            BladeMessAssetInfoResponse myResponse = new BladeMessAssetInfoResponse();
            try
            {
                if (!(this.argVal.ContainsKey('i')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No blade ID specified, please look at command help.");
                    return;
                }
                else
                {
                    dynamic bladeId = null;
                    this.argVal.TryGetValue('i', out bladeId);
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladeMezzAssetInfo((int)bladeId);
                }
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess);
                Console.WriteLine();
                Console.WriteLine("Blade Product Info Area");
                Console.WriteLine("--------------------------------------");
                Console.WriteLine(
                    "Product Manufacturer Name = " + myResponse.productAreaManufactureName);
                Console.WriteLine(
                    "Product Product Name = " + myResponse.productAreaProductName);
                Console.WriteLine(
                    "Product Part/Model Number = " + myResponse.productAreaPartModelNumber);
                Console.WriteLine(
                    "Product Version = " + myResponse.productAreaProductVersion);
                Console.WriteLine(
                    "Product Serial Number = " + myResponse.productAreaSerialNumber);
                Console.WriteLine("PD Product Asset Tag = "
                    + myResponse.productAreaAssetTag);
                Console.WriteLine("--------------------------------------");
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Write to Chassis Manager Multi Record FRU Area
    /// </summary>
    internal class SetChassisManagerAssetInfo : command
    {
        internal SetChassisManagerAssetInfo()
        {
            this.name = WcsCliConstants.setCMAssetInfo;
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setCMAssetInfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            MultiRecordResponse myResponse = new MultiRecordResponse();
            try
            {
                dynamic payload = null;

                if (!(this.argVal.ContainsKey('p')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No record data specified, please look at command help");
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('p', out payload);
                }

                myResponse = WcsCli2CmConnectionManager.channel.SetChassisManagerAssetInfo((string)payload);
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Chassis Manager's FRU has been written successfully", myResponse, true);
        }
    }

    /// <summary>
    /// Write to PDB Multi Record FRU Area
    /// </summary>
    internal class SetPDBAssetInfo : command
    {
        internal SetPDBAssetInfo()
        {
            this.name = WcsCliConstants.setPDBAssetInfo;
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setPDBAssetInfoHelp;
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            MultiRecordResponse myResponse = new MultiRecordResponse();
            try
            {
                dynamic payload = null;

                if (!(this.argVal.ContainsKey('p')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No record data specified, please look at command help");
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('p', out payload);
                }

                myResponse = WcsCli2CmConnectionManager.channel.SetPdbAssetInfo((string)payload);
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("PDB's FRU has been written successfully", myResponse, true);
        }
    }

    /// <summary>
    /// Write to Blade Multi Record FRU Area
    /// </summary>
    internal class SetBladeAssetInfo : command
    {
        internal SetBladeAssetInfo()
        {
            this.name = WcsCliConstants.setBladeAssetInfo;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('p', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setBladeAssetInfoHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            BladeMultiRecordResponse myResponse = new BladeMultiRecordResponse();
            try
            {
                dynamic bladeId = null;
                dynamic payload = null;

                if (!(this.argVal.ContainsKey('i')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No blade ID specified, please look at command help");
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('i', out bladeId);
                }

                if (!(this.argVal.ContainsKey('p')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No record data specified, please look at command help");
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('p', out payload);
                }

                myResponse = WcsCli2CmConnectionManager.channel.SetBladeAssetInfo(
                    (int)bladeId, (string)payload);
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Blade's FRU has been written successfully", myResponse, true);
        }
    }

    /// <summary>
    /// SetDataSafe(All)Blade(s)On command class
    /// </summary>
    internal class SetDataSafeBladeOn : command
    {
        internal SetDataSafeBladeOn()
        {
            this.name = WcsCliConstants.dataSafeBladeOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.dataSafeSetBladeOnHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladeResponse myResponse = new DatasafeBladeResponse();
            DatasafeAllBladesResponse myResponses = new DatasafeAllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDatasafeOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDatasafeOn((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladeResponseCollection == null))
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "DataSafe soft power ON", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "DataSafe soft power ON", myResponse, true);
            }
        }
    }

    /// <summary>
    /// SetDataSafe(All)Blade(s)Off command class
    /// </summary>
    internal class SetDataSafeBladeOff : command
    {
        internal SetDataSafeBladeOff()
        {
            this.name = WcsCliConstants.dataSafeBladeOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.dataSafeSetBladeOffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladeResponse myResponse = new DatasafeBladeResponse();
            DatasafeAllBladesResponse myResponses = new DatasafeAllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDatasafeOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDatasafeOff((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladeResponseCollection == null))
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "DataSafe blade soft power OFF", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "DataSafe blade soft power OFF", myResponse, true);
            }
        }
    }

    /// <summary>
    /// SetDataSafe(All)PowerOn command class
    /// </summary>
    internal class SetDataSafePowerOn : command
    {
        internal SetDataSafePowerOn()
        {
            this.name = WcsCliConstants.dataSafePowerOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.dataSafeSetPowerOnHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladeResponse myResponse = new DatasafeBladeResponse();
            DatasafeAllBladesResponse myResponses = new DatasafeAllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDatasafePowerOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDatasafePowerOn((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladeResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "DataSafe power ON", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "DataSafe power ON", myResponse, true);
            }
        }
    }

    /// <summary>
    /// SetDataSafe(All)PowerOff command class
    /// </summary>
    internal class SetDataSafePowerOff : command
    {
        internal SetDataSafePowerOff()
        {
            this.name = WcsCliConstants.dataSafePowerOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.dataSafeSetPoweroffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladeResponse myResponse = new DatasafeBladeResponse();
            DatasafeAllBladesResponse myResponses = new DatasafeAllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDatasafePowerOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDatasafePowerOff((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladeResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "DataSafe power OFF", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "DataSafe power OFF", myResponse, true);
            }
        }
    }

    /// <summary>
    /// SetDataSafe(All)Blade(s)ActivePowerCycle class
    /// </summary>
    internal class SetDataSafeBladeActivePowerCycle : command
    {
        internal SetDataSafeBladeActivePowerCycle()
        {
            this.name = WcsCliConstants.dataSafePowerCycle;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.dataSafeSetBladeActivePowerCycleHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladeResponse myResponse = new DatasafeBladeResponse();
            DatasafeAllBladesResponse myResponses = new DatasafeAllBladesResponse();

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesDatasafeActivePowerCycle();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladeDatasafeActivePowerCycle((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladeResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "DataSafe active power cycle set successfully", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "DataSafe active power cycle set successfully", myResponse, true);
            }
        }
    }

    /// <summary>
    /// Get(All)Blade(s)DataSafePowerState command class
    /// </summary>
    internal class GetBladeDataSafePowerState : command
    {
        internal GetBladeDataSafePowerState()
        {
            this.name = WcsCliConstants.getBladeDataSafePowerState;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeDataSafePowerStateHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            DatasafeBladePowerStateResponse myResponse = new DatasafeBladePowerStateResponse();
            DatasafeAllBladesPowerStateResponse myResponses = new DatasafeAllBladesPowerStateResponse();

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesDatasafePowerState();
                }

                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.GetBladeDatasafePowerState((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.datasafeBladePowerStateResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.datasafeBladePowerStateResponseCollection)
                {
                    if (ResponseValidation.ValidateBladeResponse(response.bladeNumber, null, response, false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + " DataSafe blade power state = " + response.bladePowerState);
                        Console.WriteLine("DataSafe power backup in progress?: " + response.isDatasafeBackupInProgress);
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + string.Format(" DataSafe blade power state = " + myResponse.bladePowerState));
                    Console.WriteLine("DataSafe power backup in progress?: " + myResponse.isDatasafeBackupInProgress);
                }
            }
        }
    }

    /// <summary>
    /// Set(All)PsuAlertDefaultPowerCap command class
    /// </summary>
    internal class SetBladePsuAlertDefaultPowerCap : command
    {
        internal SetBladePsuAlertDefaultPowerCap()
        {
            this.name = WcsCliConstants.setBladePsuAlertDpc;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('c', Type.GetType("System.UInt32"));
            this.argSpec.Add('t', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setPsuAlertDpcHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            dynamic bladeId = null;
            dynamic defaultPowerCap = null;
            dynamic waitTime = null;

            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a') && this.argVal.TryGetValue('c', out defaultPowerCap) &&
                    this.argVal.TryGetValue('t', out waitTime))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesPsuAlertDefaultPowerCap(
                        (ushort)defaultPowerCap, (ushort)waitTime);
                }
                else if (this.argVal.TryGetValue('i', out bladeId) && this.argVal.TryGetValue('c', out defaultPowerCap) &&
                    this.argVal.TryGetValue('t', out waitTime))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladePsuAlertDefaultPowerCap((int)bladeId,
                        (ushort)defaultPowerCap, (ushort)waitTime);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.bladeResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.bladeResponseCollection)
                {
                    ResponseValidation.ValidateBladeResponse(response.bladeNumber, "psu alert default power cap set", response, true);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "psu alert default power cap set", myResponse, true);
            }
        }
    }

    /// <summary>
    /// Get(All)PsuAlertDefaultPowerCap command class
    /// </summary>
    internal class GetBladePsuAlertDpc : command
    {
        internal GetBladePsuAlertDpc()
        {
            this.name = WcsCliConstants.getBladePsuAlertDpc;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getPsuAlertDpcHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladePsuAlertDpcResponse myResponse = new BladePsuAlertDpcResponse();
            AllBladesPsuAlertDpcResponse myResponses = new AllBladesPsuAlertDpcResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesPsuAlertDefaultPowerCap();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.GetBladePsuAlertDefaultPowerCap((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.bladeDpcResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.bladeDpcResponseCollection)
                {
                    if (ResponseValidation.ValidateBladeResponse(response.bladeNumber, null, response, false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + string.Format(" Default Power Cap in Watts for blade: {0} = {1}",
                               response.bladeNumber, response.DefaultPowerCap));
                        Console.WriteLine("Time in milliseconds to wait before de-asserting the PROCHOT for blade: {0} = {1}",
                            response.bladeNumber, response.WaitTime);
                        Console.WriteLine("Default Power Cap Enabled/Disabled for blade: {0} = {1}",
                            response.bladeNumber, response.DefaultCapEnabled);
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + string.Format(" Default Power Cap in Watts for blade: {0} = {1}",
                       myResponse.bladeNumber, myResponse.DefaultPowerCap));
                    Console.WriteLine("Time in milliseconds to wait before de-asserting the PROCHOT for blade: {0} = {1}",
                        myResponse.bladeNumber, myResponse.WaitTime);
                    Console.WriteLine("Default Power Cap Enabled/Disabled for blade: {0} = {1}",
                        myResponse.bladeNumber, myResponse.DefaultCapEnabled);
                }
            }
        }
    }

    /// <summary>
    /// Get(All)PsuAlert command class
    /// </summary>
    internal class GetBladePsuAlert : command
    {
        internal GetBladePsuAlert()
        {
            this.name = WcsCliConstants.getBladePsuAlert;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getPsuAlertHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladePsuAlertResponse myResponse = new BladePsuAlertResponse();
            AllBladesPsuAlertResponse myResponses = new AllBladesPsuAlertResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesPsuAlert();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.GetBladePsuAlert((int)bladeId);
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.bladePsuAlertCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                foreach (var response in myResponses.bladePsuAlertCollection)
                {
                    if (ResponseValidation.ValidateBladeResponse(response.bladeNumber, "PSU ALERT BMC GPI Status", response, false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess +
                            string.Format(" PSU Alert BMC GPI Status for blade: {0} = {1}",
                            response.bladeNumber, response.PsuAlertGpi));
                        Console.WriteLine("Auto PROCHOT on switch GPI Enabled/Disabled for blade: {0} = {1}",
                            response.bladeNumber, response.AutoProchotEnabled);
                        Console.WriteLine("BMC PROCHOT on switch GPI Enabled/Disabled for blade: {0} = {1}",
                            response.bladeNumber, response.BmcProchotEnabled);
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "PSU ALERT BMC GPI Status", myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess +
                        string.Format(" PSU Alert BMC GPI Status for blade: {0} = {1}",
                        myResponse.bladeNumber, myResponse.PsuAlertGpi));
                    Console.WriteLine("Auto PROCHOT on switch GPI Enabled/Disabled for blade: {0} = {1}",
                        myResponse.bladeNumber, myResponse.AutoProchotEnabled);
                    Console.WriteLine("BMC PROCHOT on switch GPI Enabled/Disabled for blade: {0} = {1}",
                        myResponse.bladeNumber, myResponse.BmcProchotEnabled);
                }
            }
        }
    }

    /// <summary>
    /// GetBladeBiosPostCode command class to get the post code for the blade
    /// </summary>
    internal class GetBladeBiosPostCode : command
    {
        internal GetBladeBiosPostCode()
        {
            this.name = WcsCliConstants.getBladeBiosPostCode;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBiosPostCodeHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// </summary>
        internal override void commandImplementation()
        {
            BiosPostCode myResponse = new BiosPostCode();
            try
            {
                dynamic bladeId = null;
                if (this.argVal.TryGetValue('i', out bladeId))
                {
                    myResponse = WcsCli2CmConnectionManager.channel.GetPostCode((int)bladeId);
                }
                else
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No blade ID specified, please look at command help.");
                    return;
                }
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "BIOS POST Code", myResponse, false))
            {
                Console.WriteLine(WcsCliConstants.commandSuccess +
                    " BIOS POST code for the previous boot (hex):\n" + myResponse.PreviousPostCode);
                Console.WriteLine();
                Console.WriteLine(WcsCliConstants.commandSuccess +
                    " BIOS POST code for the current boot (hex):\n" + myResponse.PostCode);

            }
        }
    }

    /// <summary>
    /// ActivateDeactivate(All)PsuAlert command class
    /// </summary>
    internal class ActivateDeactivateBladePsuAlert : command
    {
        internal ActivateDeactivateBladePsuAlert()
        {
            this.name = WcsCliConstants.activateDeactivateBladePsuAlert;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('c', null);
            this.argSpec.Add('d', Type.GetType("System.UInt32"));
            this.argSpec.Add('r', null);
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.activateDeactivatePsuAlertHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();

            dynamic bladeId = null;
            dynamic action = null;

            try
            {
                if (!this.argVal.TryGetValue('d', out action))
                {
                    action = 0; // default NoAction
                }

                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesPsuAlert(
                        this.argVal.ContainsKey('c'), (int)action, this.argVal.ContainsKey('r'));
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    if (this.argVal.TryGetValue('i', out bladeId))
                    {
                        myResponse = WcsCli2CmConnectionManager.channel.SetBladePsuAlert((int)bladeId,
                            this.argVal.ContainsKey('c'), (int)action, this.argVal.ContainsKey('r'));
                    }
                    else
                    {
                        Console.WriteLine(WcsCliConstants.commandFailure +
                            " No blade ID specified, please look at command help.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || (this.argVal.ContainsKey('a') &&
                myResponses.bladeResponseCollection == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, "activate/deactivate PSU alert done", myResponses.bladeResponseCollection[index]);
                }
            }
            else
            {
                ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, "activate/deactivate PSU alert done", myResponse);
            }
        }
    }

    /// <summary>
    /// GetBladeMezzPassThroughMode command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class GetBladeMezzPassThroughMode : command
    {
        internal GetBladeMezzPassThroughMode()
        {
            this.name = WcsCliConstants.getBladeMezzPassThroughMode;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getBladeMezzPassThroughModeHelp;

            this.conditionalOptionalArgs.Add('i', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeMezzPassThroughModeResponse myResponse = new BladeMezzPassThroughModeResponse();

            try
            {
                if (this.argVal.ContainsKey('i'))
                {
                    dynamic bladeId = null;
                    this.argVal.TryGetValue('i', out bladeId);
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladeMezzPassThroughMode((int)bladeId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if (myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('i'))
            {

                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    if (myResponse.passThroughModeEnabled == true)
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": Pass Through Mode Enabled");
                    else
                        Console.WriteLine(WcsCliConstants.commandSuccess + " Blade " + myResponse.bladeNumber + ": Pass Through Mode Not Enabled");
                }
            }
        }
    }

    /// <summary>
    /// SetBladeMezzPassThroughMode command class - derives from parent command class
    /// Constructor initializes command argument indicators and argument type
    /// </summary>
    internal class SetBladeMezzPassThroughMode : command
    {
        internal SetBladeMezzPassThroughMode()
        {
            this.name = WcsCliConstants.setBladeMezzPassThroughMode;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('m', Type.GetType("System.String"));
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setBladeMezzPassThroughModeHelp;

            this.conditionalOptionalArgs.Add('i', null);
            this.conditionalOptionalArgs.Add('m', null);
        }

        /// <summary>
        /// command specific implementation 
        /// argVal command class member has all user-entered command argument indicators and parameter values
        /// Currently just prints all argument indicators and argument values
        /// </summary>
        internal override void commandImplementation()
        {
            BladeResponse myResponse = new BladeResponse();

            try
            {
                dynamic bladeId = null;
                dynamic modeEnabled = null;

                if (!(this.argVal.ContainsKey('i')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No blade ID specified, please look at command help");
                    Console.WriteLine(WcsCliConstants.setBladeMezzPassThroughModeHelp);
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('i', out bladeId);
                }

                if (!(this.argVal.ContainsKey('m')))
                {
                    Console.WriteLine(WcsCliConstants.commandFailure +
                        " No pass through mode enabled specified, please look at command help");
                    Console.WriteLine(WcsCliConstants.setBladeMezzPassThroughModeHelp);
                    return;
                }
                else
                {
                    this.argVal.TryGetValue('m', out modeEnabled);
                }

                myResponse = WcsCli2CmConnectionManager.channel.SetBladeMezzPassThroughMode(
                    (int)bladeId, (string)modeEnabled);
            }

            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            ResponseValidation.ValidateResponse("Pass Through Mode on Blade Tray Mezz has been successfully set", myResponse, true);
        }
    }
}
