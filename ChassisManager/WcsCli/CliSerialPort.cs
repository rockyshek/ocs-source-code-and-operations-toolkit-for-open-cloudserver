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
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    internal static class CliSerialPort
    {
        private static SerialPort _serialPort = new SerialPort();

        // Variables for serial input/output 
        private static List<byte> _userCommandInput = new List<byte>();
        private static MemoryStream _memoryStreamConsoleOut = new MemoryStream();

        // lock variable for protecting the read/writes to _userCommandInput
        private static Object _lockObject = new object();

        // Signal variable 
        private static AutoResetEvent _waitSerialUserInput = new AutoResetEvent(false);

        // Timeout value for serial read/write
        private static int _timeout = 500;

        private static volatile bool ignoreEchoForNextByte = false;
        private static volatile bool addToBuffer = true;
        private static volatile bool commandProcessingActive = false;

        // Initialize a history object to hold 20 commands history
        private const int history_size = 20;
        private static CommandHistory historySerial = new CommandHistory(20);
        
        // Serial client window width
        private const int window_width = 80;

        // Check if there is a command in progress, if yes discard any inputs on serial port.
        internal static bool CommandProcessingActive
        {
            get { return commandProcessingActive; }
            set { commandProcessingActive = value; }
        }

        internal static bool CliSerialOpen(string _comPort, int _baudRate)
        {
            try
            {
                // Make these serial port paramters configurable
                _serialPort.PortName = _comPort;
                _serialPort.BaudRate = _baudRate;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                _serialPort.ReadTimeout = _timeout; // Since we are using ReadExisting this timeout value does not matter
                _serialPort.WriteTimeout = _timeout;
                _serialPort.Open();

                // Set up the event handler when we receive data on the serial port
                _serialPort.DataReceived += SerialInputReceivedHandler;                
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failure while opening serial port: " + ex.ToString());
                }
                return false;
            }

            try
            {
                // Redirect console output to a memory stream object
                StreamWriter serialWriter = new StreamWriter(_memoryStreamConsoleOut);
                serialWriter.AutoFlush = true;
                Console.SetOut(serialWriter);
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failure while console redirect: " + ex.ToString());
                }
                return false;
            }

            return true;
        }

        internal static void CliSerialClose()
        {
            try
            {
                _serialPort.DataReceived -= SerialInputReceivedHandler;

                // Recover the standard output stream so that a completion message can be displayed.
                StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);

                _waitSerialUserInput.Dispose();
                _memoryStreamConsoleOut.Dispose();
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failure while closing serial port: " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// This is a blocking call, which waits until a serial user input is received 
        /// </summary>
        /// <returns>Return the user entered byte array</returns>
        internal static Byte[] ReadUserInputBytesFromSerial()
        {
            while (true)
            {
                Byte[] outData = null;

                // If the requested user input data is already available, return it
                // Take a lock since we are accessing the shared _userCommandInput byte array
                lock (_lockObject)
                {
                    if (_userCommandInput != null && _userCommandInput.Count > 0)
                    {
                        outData = _userCommandInput.ToArray();
                        _userCommandInput.Clear();
                        return outData;
                    }
                }
                // Else wait for the data to become available
                // Wait until the user enters a command on the serial line
                _waitSerialUserInput.WaitOne();
            }
        }

        /// <summary>
        /// This is a blocking call, which waits until a serial user input command is received 
        /// </summary>
        /// <returns>Return the user entered command string </returns>
        internal static string ReadUserInputStringFromSerial(char[] delimiter)
        {
            while (true)
            {
                String outData = null;

                try
                {
                    // If the requested user input data is already available, return it
                    // Take a lock since we are accessing the shared _userCommandInput byte array
                    lock (_lockObject)
                    {
                        if (_userCommandInput != null && _userCommandInput.Count > 0)
                        {
                            // Remove backspace characters from the command input
                            string tempString = Encoding.ASCII.GetString(_userCommandInput.ToArray());
                            outData = grabRequestedData(ref tempString, delimiter);
                            
                            // Since you have consumed the data, clear the incoming buffer
                            _userCommandInput.Clear();

                            // If you have not fully consumed the data, leave the rest in the buffer
                            if (tempString != null)
                            {
                                Byte[] tempByteArray = Encoding.ASCII.GetBytes(tempString);
                                for (int i = 0; i < tempByteArray.Length; i++)
                                {
                                    _userCommandInput.Add(tempByteArray[i]);
                                }
                            }
                        }
                        if (outData != null)
                        {
                            // When a valid user entered input command line is obtained.. move the serial console to the next line..
                            // This will prevent the serial client terminal to overwrite on the same command line.. 
                            _serialPort.WriteLine("");
                            return outData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Program.logWriter != null)
                    {
                        Program.logWriter.WriteLine("Failure at ReadUserInputFromSerial.. " + ex.Message);
                    }
                    return null;
                }
                // Else wait for the data to become available
                // Wait until the user enters a command on the serial line
                _waitSerialUserInput.WaitOne();
            }
        }

        /// <summary>
        /// Method for writing the input byte array to serial
        /// </summary>
        /// <param name="myData"></param>
        internal static void WriteBytestoSerial(byte[] myData)
        {
                _serialPort.Write(myData, 0, myData.Length);
        }

        /// <summary>
        /// Write console output (from the memory stream object) to serial  
        /// </summary>
        internal static void WriteConsoleOutToSerial()
        {
            if (_memoryStreamConsoleOut != null)
            {
                // First write a carriage return to take the cursor to the beginning of the new line
                _serialPort.Write(new byte[] { 0x0D }, 0, 1);

                var bytes = _memoryStreamConsoleOut.ToArray();

                try
                {
                    // Sending 256 bytes at a time.. It looks like writing a larger buffer in _serialPort.write will not work
                    int index = 0;
                    int length = 256;
                    while (true)
                    {
                        if (index + length < bytes.Length)
                        {
                            _serialPort.Write(bytes, index, length);
                        }
                        else
                        {
                            _serialPort.Write(bytes, index, bytes.Length - index);
                            break;
                        }
                        index += length;
                    }

                    // Clear the memory stream object and set the pointer to the beginning of the stream
                    _memoryStreamConsoleOut.SetLength(0);
                    _memoryStreamConsoleOut.Seek(0, SeekOrigin.Begin);
                }
                catch (Exception ex)
                {
                    if (Program.logWriter != null)
                    {

                        Program.logWriter.WriteLine("Failure while writing console out to serial. " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler will be called when any data is available on the serial port
        /// Note that we may have received one or more bytes when this event handler is called
        /// We need to make sure that we consume the data at the granularity we are interested in (either a carriage return or new line)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SerialInputReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Append the currentUserInputString to the already existing and unconsumed user input data    
                lock (_lockObject)
                {
                    int byteThresholdValue = 3;
                    if ((sender as SerialPort).BytesToRead > byteThresholdValue)
                    {
                        byte[] range = new byte[(sender as SerialPort).BytesToRead];

                        //consume all bytes at once
                        (sender as SerialPort).Read(range, 0, (sender as SerialPort).BytesToRead);

                         // Add to the buffer only when there is no command in progress or blade serial session is active
                        if (CommandProcessingActive == false || SharedFunc.ActiveSerialSession)
                        {                            
                            foreach (byte value in range)
                            {
                                if (!SharedFunc.ActiveSerialSession)
                                {
                                    // If escape character found, discard rest of the input
                                    if (value == 0x1b)
                                    {
                                        break;
                                    }
                                }

                                (sender as SerialPort).Write(new byte[] { value }, 0, 1);
                                _userCommandInput.Add(value);
                            }
                        } 
                    }
                    else
                    {
                        // if active serial session, capture function keys and encode
                        if (SharedFunc.ActiveSerialSession)
                        {
                            // Capture function keys F5-F12 and encode them before adding to buffer
                            if ((sender as SerialPort).BytesToRead == 3)
                            {
                                byte[] range = new byte[(sender as SerialPort).BytesToRead];

                                //consume all bytes at once
                                (sender as SerialPort).Read(range, 0, (sender as SerialPort).BytesToRead);

                                // if Function keys F5-F12 (byte sequence ^[OT to ^[O[)
                                if (range[0] == 0x1b && range[1] == 0x4F)
                                {
                                    switch (range[2])
                                    {
                                            //F5
                                        case 0x54:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x35;
                                            break;
                                            //F6
                                        case 0x55:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x36;
                                            break;
                                            //F7
                                        case 0x56:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x37;
                                            break;
                                            //F8
                                        case 0x57:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x38;
                                            break;
                                            //F9
                                        case 0x58:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x39;
                                            break;
                                            //F10
                                        case 0x59:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x30;
                                            break;
                                            //F11
                                        case 0x5A:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x2A;
                                            break;
                                            //F12
                                        case 0x5B:
                                            range = new byte[2];
                                            range[0] = 0x1b;
                                            range[1] = 0x28;
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                foreach (byte value in range)
                                {
                                    (sender as SerialPort).Write(new byte[] { value }, 0, 1);
                                    _userCommandInput.Add(value);
                                }
                            }
                        }

                        // Get everything that the user has entered on the serial line
                        for (int i = 0; i < (sender as SerialPort).BytesToRead; i++)
                        {
                            byte outbyte = (byte)(sender as SerialPort).ReadByte();

                            // Add to the buffer only when there is no command in progress or blade serial session is active
                            if (CommandProcessingActive == false || SharedFunc.ActiveSerialSession)
                            {

                                // Reset add to buffer, in case we had a escape sequence earlier
                                addToBuffer = true;

                                // Echo only if there is no active blade serial session, this functionality will be disabled for blade serial session.
                                if (!SharedFunc.ActiveSerialSession)
                                {
                                    //if backspace, remove last byte from input buffer if buffer not null
                                    if (outbyte == 0x7F)
                                    {
                                        // remove only if there is any data in the buffer
                                        if (_userCommandInput.Count > 0)
                                        {
                                            addToBuffer = false;

                                            // Check if we have multi-line command
                                            if (_userCommandInput.Count % window_width == (window_width - (WcsCliConstants.consoleString.Length + 1)))
                                            {
                                                // handle backspace for multi-line commands, by re-positioning cursor
                                                HandleMultilineBackspaces();

                                                // ignore this backspace
                                                ignoreEchoForNextByte = true;
                                            }

                                            _userCommandInput.RemoveAt(_userCommandInput.Count - 1);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    // Exclude ASCII control codes from adding to input buffer
                                    if ((outbyte >= 0x01 && outbyte <= 0x1F) && outbyte != 0x0A && outbyte != 0x0D)
                                    {
                                        addToBuffer = false;
                                    }

                                    // Echo back bytes to serial, characters in the range (0x20 to 0x7E) and '\b', '\n'
                                    // backspace: 0x7F, 
                                    // escape sequence:  0x1b,
                                    // newline: 0x0A                          
                                    if (outbyte == 0x0A || outbyte == 0x09 || outbyte == 0x7F || outbyte == 0x1b || outbyte == 0x7E || (outbyte >= 0x20 && outbyte <= 0x7E) || (ignoreEchoForNextByte && (outbyte == 0x02 || outbyte == 0x03)))
                                    {
                                        // Check for tab
                                        if (outbyte == 0x09)
                                        {
                                            HandleTab();
                                            addToBuffer = false;
                                        }
                                        // Check for excape sequence '[' after '^' and don't echo back, set next byte to not echo
                                        else if (ignoreEchoForNextByte && outbyte == 0x5b)
                                        {
                                            ignoreEchoForNextByte = true;
                                            addToBuffer = false;
                                        }
                                        //Check for excape sequence '^', set next byte to not echo
                                        else if (outbyte == 0x1b)
                                        {
                                            ignoreEchoForNextByte = true;
                                            addToBuffer = false;
                                        }
                                        else
                                        {
                                            // Echo if not set to ignore.
                                            if (!ignoreEchoForNextByte)
                                            {
                                                EchoBytesToSerial(outbyte);
                                            }
                                            else
                                            {
                                                // Don't add to buffer
                                                addToBuffer = false;

                                                if (UpArrowPresent(outbyte) || DownArrowPresent(outbyte) || LeftArrowPresent(outbyte) || RightArrowPresent(outbyte))
                                                {

                                                    // If Up or Down arrow keys are entered, display command history
                                                    ProcessArrowKeys(outbyte);
                                                }
                                                else
                                                {
                                                    FlushPortBuffers();
                                                }

                                                // reset flag
                                                ignoreEchoForNextByte = false;

                                            } // end of internal else loop
                                        } // End of outer else loop
                                    } // end of if condition checking for escape characters  
                                } // end of check for if condition for blade serial session

                                // Add to buffer.For blade serial connection active flag is true by default
                                if (addToBuffer)
                                {
                                    _userCommandInput.Add(outbyte);
                                }
                            } // end of check for command processing active

                        } // end of For loop
                    }
                } // end of Lock 
            } // end of Try 

            catch (TimeoutException)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Event handler: Serial read timeout..");
                }
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Event handler: Failure while reading from serial port: " + ex.ToString());
                }

                ignoreEchoForNextByte = false;
            }

            // Signal to let the program consume the user input data
            _waitSerialUserInput.Set();
        }

        /// <summary>
        /// Echo bytes to Serial
        /// <param name="outbyte">Byte to output</param>
        /// </summary> 
        private static void EchoBytesToSerial(byte outbyte)
        {
            try
            {
                // Echo back the input byte.
                _serialPort.Write(new byte[] { outbyte }, 0, 1);
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("EchoBytesToSerial: Failure to write to serial.. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// This method clears the screen till wcscli prompt when an UP or Down arrow is received.
        /// </summary>
        /// <param name="countOfCharacters">Count of input characters</param>
        internal static void Clear(int countOfCharacters)
        {
            int count = countOfCharacters;
            int line = 0;

            // if single line command
            if (count <= (window_width - (WcsCliConstants.consoleString.Length + 1)))
            {
                // send backspaces.
                for (int c = 0; c < count; c++)
                {
                    EchoBytesToSerial(0x7F);
                }
            }
            else
            {
                // remove the console window width ( except the 8 characters for WCSCLI prompt)
                int countNew = count - (window_width - (WcsCliConstants.consoleString.Length + 1));

                // count the number of lines we need to delete
                while (countNew > 0)
                {
                    line = line + 1;
                    countNew = countNew - window_width;
                }

                // get the count back
                countNew = countNew + window_width;

                // clear the last line.
                for (int c = 0; c < countNew; c++)
                {
                    EchoBytesToSerial(0x7F);
                }

                // Move cursor up n lines up
                for (int l = 0; l < line; l++)
                {
                    // Echo characters'^[A' which are to move up a line.
                    EchoBytesToSerial(0x1b);
                    EchoBytesToSerial(0x5b);
                    EchoBytesToSerial(0x41);
                }

                // Skip wcscli prompt
                // Echo characters '[8C' to move right 8 characters to skip wcscli prompt
                EchoBytesToSerial(0x1b);
                EchoBytesToSerial(0x5b);
                EchoBytesToSerial(0x38);
                EchoBytesToSerial(0x43);


                // clear screen from cursor right
                // Echo characters '^[J' to clear screen right of wcscli prompt
                EchoBytesToSerial(0x1b);
                EchoBytesToSerial(0x5b);
                EchoBytesToSerial(0x4A);
            }
        }

        /// <summary>
        /// Add to history if not null
        /// </summary>
        /// <param name="data">Command to add</param>
        /// <returns></returns>
        internal static void HandleHistory(string data)
        {
            if (!String.IsNullOrEmpty(data) && !String.IsNullOrWhiteSpace(data) && !SharedFunc.ActiveSerialSession)
            {
               InitializeHistory(data);               
            }
        }

        /// <summary>
        /// Returns true/false if the given text contains Up Arrow character
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool UpArrowPresent(byte outbyte)
        {
            return ((int)outbyte == 65) ? true : false;
        }

        /// <summary>
        /// Returns true/false if the given byte contains Down Arrow character
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool DownArrowPresent(byte outbyte)
        {
            return ((int)outbyte == 66) ? true : false;
        }

        /// <summary>
        /// Returns true/false if the given byte contains Left Arrow character
        /// </summary>
        /// <param name="outbyte"></param>
        /// <returns></returns>
        internal static bool LeftArrowPresent(byte outbyte)
        {
            return ((int)outbyte == 68) ? true : false;
        }

        /// <summary>
        /// Returns true/false if the given byte contains Right Arrow character
        /// </summary>
        /// <param name="outbyte"></param>
        /// <returns></returns>
        internal static bool RightArrowPresent(byte outbyte)
        {
            return ((int)outbyte == 67) ? true : false;
        }

        /// <summary>
        /// Clear screen and display history for commands for Arrow keys.
        /// </summary>
        internal static void ProcessArrowKeys(byte outbyte)
        {
            // if '65' then UP arror, else if '66' then down arrow.
            if (UpArrowPresent(outbyte) || DownArrowPresent(outbyte))
            {
                if (UpArrowPresent(outbyte) && historySerial.PreviousAvailable() || DownArrowPresent(outbyte))
                {
                    //clear the previous command
                    Clear(_userCommandInput.Count);

                    //Clear the input buffer
                    _userCommandInput.Clear();

                    byte[] outputData = null;

                    if (UpArrowPresent(outbyte))
                    {
                        string previous = GetPrevious();
                        if (!String.IsNullOrEmpty(previous))
                        {
                            // get previous command                      
                            outputData = Encoding.ASCII.GetBytes(previous);
                        }
                    }
                    else
                    {
                        // get next command
                        string next = GetNext();
                        if (!String.IsNullOrEmpty(next))
                        {
                            outputData = Encoding.ASCII.GetBytes(next);
                        }
                    }

                    // Write to serial if there are any bytes.
                    if (outputData != null)
                    {
                        // write command to output 
                        WriteBytestoSerial(outputData);
                    }
                }
            } // end of loop check for Up arrow & Down arrow
        }

        internal static void FlushPortBuffers()
        {
            if (_serialPort != null)
            {
                _serialPort.DiscardOutBuffer();
                _serialPort.DiscardInBuffer();
            }
        }

        /// <summary>
        /// Handle tab auto completion
        /// </summary>
        private static void HandleTab()
        {  
            // Get the word last entered in buffer separated by a space.
            string text = Encoding.UTF8.GetString(_userCommandInput.ToArray(), 0, _userCommandInput.Count);
            string prefix = null;
            if (text.ToString().Contains(' '))
            {
                prefix = text.ToString().Split(' ').Last();
            }
            else
            {
                prefix = text.ToString();
            }

            // Send the word string to Auto tab handler
            string[] completions = TabHelper.GetTabOptions(prefix.ToLower());

            if (completions == null)
                return;

            int ncompletions = completions.Length;
            if (ncompletions == 0)
                return;

            // if there is just one matching string, print it to string.
            if (completions.Length == 1)
            {
                InsertTextAtCursor(completions[0]);
                AddToBuffer(completions[0]);
            }
            else
            {
                int last = -1;

                // find the first character that differs from user entered string
                for (int p = 0; p < completions[0].Length; p++)
                {
                    char c = completions[0][p];

                    for (int i = 1; i < ncompletions; i++)
                    {
                        if (completions[i].Length < p)
                            goto mismatch;

                        if (completions[i][p] != c)
                        {
                            goto mismatch;
                        }
                    }
                    last = p;
                }
            mismatch:
                // if first time and there is string that matches, add to screen
                // example: User entered wcscli -getc followed by tab
                // we can complete it by wcscli -getchassis and also provide other 
                // options:  -getchassisinfo -getchassishealth -getchassisattentionledstatus -getchassismanagerstatus -getchassismanagerassetinfo
                // following code handles above scenario
                if (last != -1)
                {
                    InsertTextAtCursor(completions[0].Substring(0, last + 1));
                    AddToBuffer(completions[0].Substring(0, last + 1));
                }

                // Insert New line
                EchoBytesToSerial(0x0A);

                for (int i = 0; i < _userCommandInput.Count + (WcsCliConstants.consoleString.Length + 1); i++)
                {
                    EchoBytesToSerial(0x1b);
                    EchoBytesToSerial(0x5b);
                    EchoBytesToSerial(0x44);
                }

                int count = 0;

                // Print all options to screen
                foreach (string s in completions)
                {
                    InsertTextAtCursor(prefix);
                    count = count + prefix.Length;
                    InsertTextAtCursor(s);
                    count = count + s.Length;
                    InsertTextAtCursor(new string(' ', 1));
                    count = count + 1;
                }

                // Calculate cursor position 
                int charToRemove = count % window_width;                

                // Insert New line
                EchoBytesToSerial(0x0A);

                // Move cursor left
                for (int i = 0; i < charToRemove; i++)
                {
                    EchoBytesToSerial(0x1b);
                    EchoBytesToSerial(0x5b);
                    EchoBytesToSerial(0x44);
                }

                // print WCSCLI prompt
                InsertTextAtCursor(WcsCliConstants.consoleString + " " + "");
                InsertTextAtCursor(Encoding.UTF8.GetString(_userCommandInput.ToArray(), 0, _userCommandInput.Count));
            }
        }

        /// <summary>
        /// Write given string to serial client
        /// </summary>
        /// <param name="text">text to write</param>
        private static void InsertTextAtCursor(string text)
        {
            byte[] outputData = null;
            outputData = Encoding.ASCII.GetBytes(text);

            WriteBytestoSerial(outputData);
        }

        /// <summary>
        /// Add given string to input buffer 
        /// </summary>
        /// <param name="text">String to add to buffer</param>
        private static void AddToBuffer(string text)
        {
            byte[] outputData = null;
            if (String.IsNullOrEmpty(text))
            {
                return;
            }
            outputData = Encoding.ASCII.GetBytes(text);

            // add the string to buffer
            for (int c = 0; c < outputData.Length; c++)
            {
                _userCommandInput.Add(outputData[c]);
            }
        }

        /// <summary>
        /// Initilaize history
        /// </summary>
        internal static void InitializeHistory(string initial)
        {
            historySerial.Append(initial);
            historySerial.CursorToEnd();
        }

        /// <summary>
        /// Get next available value from command history
        /// </summary>
        /// <returns></returns>
        internal static string GetNext()
        {
            if (!historySerial.NextAvailable())
            {
                historySerial.CursorToEnd();
                return null;
            }

            string next = historySerial.Next();
            AddToBuffer(next);
            historySerial.Update(Encoding.UTF8.GetString(_userCommandInput.ToArray(), 0, _userCommandInput.Count));

            return next;
        }

        /// <summary>
        /// Get previous value from command history
        /// </summary>
        /// <returns></returns>
        internal static string GetPrevious()
        {
            string previous = historySerial.Previous();
            AddToBuffer(previous);
            historySerial.Update(Encoding.UTF8.GetString(_userCommandInput.ToArray(), 0, _userCommandInput.Count));
            return previous;
        }

        /// <summary>
        /// Clear history cache
        /// </summary>
        internal static void ClearHistory()
        {
            try
            {
                if (historySerial != null)
                {
                    historySerial.Clear();
                }
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failed to clear history " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Clear client window
        /// </summary>
        internal static void ClearWindow()
        {
            //Clear entire screen, by sending escape sequence 'Esc[2J'
            EchoBytesToSerial(0x1b);
            EchoBytesToSerial(0x5b);
            EchoBytesToSerial(0x32);
            EchoBytesToSerial(0x4A);

            // Move cursor to upper left corner of screen, by sending escape sequence 'Esc[H'
            EchoBytesToSerial(0x1b);
            EchoBytesToSerial(0x5b);
            EchoBytesToSerial(0x48);
        }

        /// <summary>
        /// Method to position cursor for multi-line backspaces
        /// </summary>
        internal static void HandleMultilineBackspaces()
        {
            // Move cursor to end of current line.
            for (int a = 0; a < 80; a++)
            {
                EchoBytesToSerial(0x20);
            }

            // Echo characters'^[A' which are to move up a line.
            EchoBytesToSerial(0x1b);
            EchoBytesToSerial(0x5b);
            EchoBytesToSerial(0x41);

            // Clear screen right of cursor
            // Echo characters '^[J' to clear screen right of cursor
            EchoBytesToSerial(0x1b);
            EchoBytesToSerial(0x5b);
            EchoBytesToSerial(0x4A);
        }

        /// <summary>
        /// Clear user buffer, when exiting from session
        /// </summary>
        internal static void ClearInputBuffer()
        {
            try
            {
                lock (_lockObject)
                {
                    _userCommandInput.Clear();
                }
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failed to clear input buffer " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Signal carriage return. Used to exit from waiting for user input during a blade serial session.
        /// </summary>
        internal static void SignalCarriageReturn()
        {
            try
            {
                lock (_lockObject)
                {
                    // Add carriage return character and signal user input is available
                    _userCommandInput.Add(0x0D);
                    _waitSerialUserInput.Set();
                }
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failed to signal carriage return " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Grab the first occurrence of the delimited string/data and return it
        /// Remove the returned string/data from the "data" argument
        /// </summary>
        /// <param name="data"></param>
        /// <param name="delimit"></param>
        /// <returns></returns>
        private static string grabRequestedData(ref string data, char[] delimit)
        {
            try
            {
                if (data == null)
                {
                    return null;
                }

                if (delimit.Length == 0)
                {
                    return data;
                }

                string[] parts = data.Split(delimit);

                // If the delimiter is not present in the input string, return null
                if (parts.Length == 1 && parts[0].Equals(data, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                else // else return the first occurrence of the delimited string
                {
                    string tempString = null;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        tempString += parts[i];
                    }
                    
                    if (tempString.Length > 0)
                        data = tempString;
                    else
                        data = null;

                    return parts[0];
                }
            }
            catch (Exception ex)
            {
                if (Program.logWriter != null)
                {
                    Program.logWriter.WriteLine("Failure in grabRequestedData().. " + ex.Message);
                }
                return null;
            }
        }
    }
}
