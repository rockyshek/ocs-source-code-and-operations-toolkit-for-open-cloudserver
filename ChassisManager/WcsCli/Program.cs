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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.IO;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.GFS.WCS.WcsCli
{
    internal class Program
    {
        // Log file for tracing - TODO extend tracer class
        public static StreamWriter logWriter;

        public const string ServiceName = "WcscliSerialService";
        public const string LogName = "WcscliLogEvent";

        public static ConsoleOperations consoleObj;
                
        /// <summary>
        /// Windows event log for wcscli service
        /// </summary> 
        public static EventLog WcscliEventLog;
                
        public class WcscliSerialService: ServiceBase
        {
            private String _defaultCliServiceComPort = "COM2";
            private int _defaultCliServiceBaudRate = 115200;
            
            private Thread _serviceThread = new Thread(ContinuousSerialUserInputCapture);
            internal static bool _continue = true;

            public WcscliSerialService()
            {
                try
                {
                    _defaultCliServiceComPort = ConfigurationManager.AppSettings.Get("COMPortName");
                }
                catch
                {
                    // Cannot determine the COM port to start the service.. Exiting
                    Environment.Exit(-1);
                }

                try
                {
                    ServiceName = ConfigurationManager.AppSettings.Get("ServiceName") + _defaultCliServiceComPort;
                }
                catch
                {
                    // if service name is not present in the app.config, use the default service name
                    ServiceName = "wcscli" + _defaultCliServiceComPort;
                }

                try
                {
                    // Create windows system event log, if not exists already
                    // Please note that servicename here also has default CLI port added to it.
                    if (!EventLog.SourceExists(ServiceName))
                    {
                        EventLog.CreateEventSource(ServiceName, LogName);
                    }
                    WcscliEventLog = new EventLog();
                    WcscliEventLog.Source = ServiceName;
                    WcscliEventLog.Log = LogName;
                }
                catch(Exception)
                {
                    WcscliEventLog = null;
                }

                try
                {
                    // Create trace log
                    logWriter = new StreamWriter(@"C:\" + ServiceName + "_TraceLog.txt");
                    logWriter.AutoFlush = true;
                }
                catch (Exception ex)
                {
                    // write to event log if not null
                    if (WcscliEventLog != null)
                    {
                        WcscliEventLog.WriteEntry("Failed to create trace log, exception: " + ex, EventLogEntryType.Error);
                }
                }

                try
                {
                    _defaultCliServiceBaudRate = Convert.ToInt32(ConfigurationManager.AppSettings.Get("COMPortBaudRate"));
                }
                catch(Exception ex)
                {
                    if (logWriter != null)
                    {
                        logWriter.WriteLine("Failure reading config File to determine the baud rate.. Using the default 115200" + ex.Message);
                    }
                    _defaultCliServiceBaudRate = 115200;
                }
            }

            protected override void OnStart(string[] args)
            {
                // Wcscli Serial Service specific code goes here - starts
                try
                {
                    if (!CliSerialPort.CliSerialOpen(_defaultCliServiceComPort, _defaultCliServiceBaudRate))
                    {
                        // Print failure and exit the service
                        if (logWriter != null)
                        {
                            logWriter.WriteLine("Fault when serial port CliSerialOpen is called..");
                        }
                        Environment.Exit(-1);
                    }
                    logWriter.WriteLine("Succesfully started service with name ({0}) at Port ({1})", ServiceName, _defaultCliServiceComPort);
                }
                catch (IOException e)
                {
                    if (logWriter != null)
                    {
                        logWriter.WriteLine("Fault (IOException) at service start " + e.Message);
                    }
                }
                catch (Exception ex)
                {
                    if (logWriter != null)
                    {
                        logWriter.WriteLine("Fault at service start " + ex.ToString());
                    }
                }
                // Wcscli Serial Service specific code goes here - ends

                // onstart code here
                _serviceThread.Start();
                if (logWriter != null)
                {
                    logWriter.WriteLine("Service thread started.. ");
                }

                // populate network controller index.
                Contracts.SharedFunc.EnumerateControllers();
            }

            /// <summary>
            /// Parses the input entered by the user over serial
            /// Calls WcsCliCmProxy class for processing the input
            /// </summary>
            private static void ContinuousSerialUserInputCapture()
            {
                String inputString = null;

                while (_continue)
                {
                    // Blocking call that will wait until the user enters a command delimited by carriage return or new line
                    inputString = CliSerialPort.ReadUserInputStringFromSerial(new char[] { '\n', '\r' });

                    if (inputString != null)
                    {
                        CliSerialPort.HandleHistory(inputString);

                        CliSerialPort.CommandProcessingActive = true;

                        if (inputString.Equals("exit", StringComparison.InvariantCultureIgnoreCase) || inputString.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                        {
                            inputString = "wcscli ";
                            inputString += "-" + WcsCliConstants.terminateCmConnection;
                        }

                        if (inputString.ToLower().Contains(" -v"))
                        {
                            inputString = "wcscli ";
                            inputString += "-" + WcsCliConstants.establishCmConnection;
                            inputString += " -v";
                        }
                        
                        // Execute the command
                        WcsCliCmProxy.InteractiveParseUserCommandGetCmResponse(true, inputString);
                    }
                    Console.Write(WcsCliConstants.consoleString + " " + "");

                    // Write response data from console out to serial
                    CliSerialPort.WriteConsoleOutToSerial();
                    CliSerialPort.CommandProcessingActive = false;

                } // While loop ends
            }

            protected override void OnStop()
            {
                CliSerialPort.CliSerialClose();
                
                _continue = false;
                if (_serviceThread != null)
                    _serviceThread.Join(2000);
                if (_serviceThread != null)
                    _serviceThread.Abort();
                
                if (logWriter != null)
                {
                    logWriter.WriteLine("The END!");
                    logWriter.Close();
                }
            }
        }

        // If '-b' or '-v' is specified as a WCSCLI command line parameter, then do not enter continuous interactive mode.
        // Instead quit the application after executing the respective command.
        internal static bool isBatchOrVersionCmd = false;

        /// <summary>
        /// Main program where the console command-line user interface starts
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            bool runApplication = false;

            // Check if we run as application or a service
            if (args != null)
            {
                // List of valid command line parameters
                List<string> validParams = new List<string> {"-h", "-p", "-s", "-u", "-x", "-b", "-v"};
                for (int i = 0; i < args.Length; i++)
                {
                    // If the command line argument contains a valid parameter, run as application
                    foreach (string validParam in validParams)
                    {
                        if ((args[i].Trim().Equals(@validParam, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            runApplication = true;
                            break;
                        }
                    }
                    // Found a valid flag
                    if (runApplication)
                        break;
                }
            }

            if ((!runApplication) && (!Environment.UserInteractive))
            {
                // running as service
                using (var service = new WcscliSerialService())
                    ServiceBase.Run(service);
            }
            else 
            {
                // Command-line specific code goes here 
                Console.Title = "Chassis Manager Command-line Interface.";
                WcsCliCmProxy.InteractiveParseUserCommandGetCmResponse(false, ConvertConsoleCommandLineArgsToCommandInput(args));
                if (isBatchOrVersionCmd)
                {
                    // If batch file is specified as input then do not get in to interactive mode.. quit.. 
                    return;
                }
                if (!WcsCli2CmConnectionManager.TestConnectionToCmService())
                {
                    Console.WriteLine("Please try again by executing \"{0}\" \n", WcsCliConstants.wcscliConsoleParameterHelp);
                    return;
                }
                ContinuousConsoleUserInputCapture();
            }
        }

        /// <summary>
        /// Parses the input entered by the user over console
        /// Calls WcsCliCmProxy class for processing the input
        /// </summary>
        private static void ContinuousConsoleUserInputCapture()
        {
            String inputString = null;
            bool _tobreak = false;
            consoleObj = new ConsoleOperations();

            while (true)
            {
                inputString = consoleObj.ProcessInput();
                
                if (inputString == null)
                        continue;
                                
                if (inputString.Equals("exit", StringComparison.InvariantCultureIgnoreCase) || 
                        inputString.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    inputString = "wcscli";
                    inputString += " ";
                    inputString += "-" + WcsCliConstants.terminateCmConnection;
                    _tobreak = true;
                }
                if (inputString.ToLower().Contains(" -v"))
                {
                    inputString = "wcscli ";
                    inputString += "-" + WcsCliConstants.establishCmConnection;
                    inputString += " -v";
                }

                WcsCliCmProxy.InteractiveParseUserCommandGetCmResponse(false,inputString);

                if (_tobreak)
                    break;
            } // While loop ends
            return;
        }

        /// <summary>
        /// Method to convert command line arguments to a string array for command validation
        /// </summary>
        /// <param name="args">arguments for host, port, ssl otipn, batch input</param>
        /// <returns></returns>
        private static string ConvertConsoleCommandLineArgsToCommandInput(string[] args)
        {
            string inputString;

            if(args != null)
            {
                inputString = "wcscli";
                inputString += " ";
                inputString += "-" + WcsCliConstants.establishCmConnection;
                inputString += " ";
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Trim().Equals(@"-h", StringComparison.InvariantCultureIgnoreCase))
                    {
                        args[i] = "-m";
                    }
                    if (args[i].Trim().Equals(@"-b", StringComparison.InvariantCultureIgnoreCase) ||
                            args[i].Trim().Equals(@"-v", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isBatchOrVersionCmd = true;
                        args[i] = args[i].Trim().ToLower();
                    }
                    inputString += args[i];
                    inputString += " ";
                }

                return inputString;
            }
            else
            {
                return null;
            }
        }
    } // class program ends
} // namespace WcsCli ends

