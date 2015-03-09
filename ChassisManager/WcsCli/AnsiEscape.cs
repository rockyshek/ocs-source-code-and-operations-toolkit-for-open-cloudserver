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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;

    internal class AnsiEscape<T>: Vt100Base where T: SerialSession
    {
        /// <summary>
        /// Cursor position tracker
        /// </summary>
        private int _curLeftTracker = 0;
        private int _curTopTracker = 0;

        /// <summary>
        /// Start position tracker, used in conjunction
        /// with cursor position tracker
        /// </summary>
        private volatile bool _start = false;

        /// <summary>
        /// Console payload, in string format.
        /// </summary>
        private string scrData = string.Empty;

        // Capture console window and buffer sizes during initialization
        private int initWindowHeight = Console.WindowHeight;
        private int initWindowWidth = Console.WindowWidth;
        private int initBufferHeight = Console.BufferHeight;
        private int initBufferWidth = Console.BufferWidth;

                /// <summary>
        /// Calling Serial Session Class, object
        /// required for keypress passback.
        /// </summary>
        private T serialSession;

        /// <summary>
        /// VT100 codepage.
        /// </summary>
        private uint codepage = 437;

        /// <summary>
        /// Console window and buffer sizes.
        /// These are the dimensions of the console that is displayed when the serial session is started
        /// </summary>
        private readonly int consoleWidth = 120;
        private readonly int consoleHeight = 25;
        private readonly int consoleBufferWidth = 8192;  // Match windows maximum command length
        private readonly int consoleBufferHeight = 200;

        /// <summary>
        /// Initializes class and sets defaults.
        /// </summary>
        internal AnsiEscape(T session)
        {
            // Clear the Console
            Console.Clear();

            serialSession = session;

            // Set console and buffer sizes. Constrain window size to largest allowed
            base.SetConsoleSize(Math.Min(consoleWidth, Console.LargestWindowWidth),
                                Math.Min(consoleHeight, Console.LargestWindowHeight));
            base.SetConsoleBufferSize(consoleBufferWidth, consoleBufferHeight);

            // set cursor positions to beginning
            PositionLeft = 0;
            PositionTop = 0;

            // set cursor position to default
            base.SetCursorPosition(PositionTop, PositionLeft);

            // capture current code page.
            uint cp = NativeMethods.GetCodePage();
            if (cp > 0)
                codepage = cp;

            // Set the Console Code page to 437
            NativeMethods.SetCodePage(437);

            // pInvoke Call to Disable Console
            // wordwrap.  By default VT100 does not
            // expect wordwrap
            NativeMethods.DisableWordWrap(); // Disable Word Wrap

            // Capture Ctrl+C as key command.
            Console.TreatControlCAsInput = true;

            // enable receive loop.
            SharedFunc.SetSerialSession(true);

        }

        /// <summary>
        /// Rerverts Console back to original state.
        /// </summary>
        internal void RevertConsole()
        {
            // set page code back to original
            NativeMethods.SetCodePage(codepage);

            // re-enable word-wrap
            NativeMethods.EnableWordWrap(); // Enable Word Wrap

            // Disable Ctrl+C capture, allow it termiante.
            Console.TreatControlCAsInput = false;

            // Set console and buffer sizes back to the original state. 
            // Constrain window size to largest allowed.
            base.SetConsoleSize(Math.Min(initWindowWidth, Console.LargestWindowWidth),
                                Math.Min(initWindowHeight, Console.LargestWindowHeight));
            base.SetConsoleBufferSize(initBufferWidth, initBufferHeight);

            // Issue a graceful terminate.
            SharedFunc.SetSerialSession(false);

            //clear console
            Console.Clear();
        }

        /// <summary>
        /// Designed to run continously reading for user
        /// input.  The method intercepts user input
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        internal void ReadConsole(bool enterEncodeCRLF)
        {
            while (SharedFunc.ActiveSerialSession)
            {
                // Loop until keypress is available in the input stream
                while (Console.KeyAvailable == false)
                {
                    // Check that session is not closed by the Receive thread
                    if (!SharedFunc.ActiveSerialSession)
                        return;
                }
                ConsoleKeyInfo keyInf = Console.ReadKey(true);

                if (!IsFunctionKey(keyInf))
                {
                    if (!_start)
                    {
                        _curLeftTracker = Console.CursorLeft;
                        _curTopTracker = Console.CursorTop;
                        _start = true;
                    }

                    if (keyInf.Key == ConsoleKey.Backspace)
                    {
                        if (scrData.Length > 0)
                        {
                            scrData = scrData.Remove((scrData.Length - 1));
                            Console.Write(keyInf.KeyChar);
                            Console.Write(" ");
                            Console.CursorLeft = (Console.CursorLeft - 1);
                        }
                    }
                    else
                    {
                        scrData = scrData + keyInf.KeyChar;
                        Console.Write(keyInf.KeyChar);
                    }
                }
                else
                {
                    SendKeyData(keyInf, enterEncodeCRLF);
                }
            }
        }

        /// <summary>
        /// Check ConsoleKeyInfo for VT100 Function Key
        /// </summary>
        private bool IsFunctionKey(ConsoleKeyInfo keyInfo)
        {
            if ((keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F12
                && keyInfo.Key <= ConsoleKey.F12) ||
                (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                && keyInfo.Key <= ConsoleKey.DownArrow) ||
                (keyInfo.Key == ConsoleKey.Enter) || // if key is Enter
                (keyInfo.Key == ConsoleKey.Escape) || // if key is Escape
                (keyInfo.Key == ConsoleKey.Delete) || // if key is Delete
                (keyInfo.Key == ConsoleKey.Tab) || // if key is Tab
                (keyInfo.Key == ConsoleKey.X &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) || // Ctrl + X
                (keyInfo.Key == ConsoleKey.C &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
                )
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Send encoded payload
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private void SendKeyData(ConsoleKeyInfo keyInfo, bool enterEncodeCRLF)
        {
            byte[] payload = Vt100Encode(keyInfo, enterEncodeCRLF);
            
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (_start)
                {
                    Console.CursorLeft = _curLeftTracker;
                    Console.CursorTop = _curTopTracker;
                    _start = false;
                }
            }

            // send the payload 
            SendPayload(payload);
        }

        /// <summary>
        /// Send the keypress and payload back to the WCS Chassis Manager
        /// using the WCF Channel.
        /// </summary>
        private void SendPayload(byte[] payload)
        {
            serialSession.Send(payload);
        }

        /// <summary>
        /// Encode VT100 escape sequences into function key
        /// enterEncodeCRLF: VT100 encoding for Enter key. True: CR+LF for Enter. False: CR for Enter.
        /// </summary>
        private byte[] Vt100Encode(ConsoleKeyInfo keyInfo, bool enterEncodeCRLF)
        {
            byte[] enc = new byte[3];
            enc[0] = 0x1B; // Esc

            if (keyInfo.Key >= ConsoleKey.F1 // if key between F1 & F4
                && keyInfo.Key <= ConsoleKey.F4)
            {
                enc[1] = 0x4F; // O
                switch (keyInfo.Key)
                {
                    case ConsoleKey.F1:
                        enc[2] = 0x50; // P
                        break;
                    case ConsoleKey.F2:
                        enc[2] = 0x51; // Q
                        break;
                    case ConsoleKey.F3:
                        enc[2] = 0x52; // R
                        break;
                    case ConsoleKey.F4:
                        enc[2] = 0x53; // S
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key >= ConsoleKey.F5 // if key between F5 & F12
                && keyInfo.Key <= ConsoleKey.F12)
            {
                enc = new byte[2];
                enc[0] = 0x1B; // Esc

                switch (keyInfo.Key)
                {
                    case ConsoleKey.F5:
                        enc[1] = 0x35;
                        break;
                    case ConsoleKey.F6:
                        enc[1] = 0x36;
                        break;
                    case ConsoleKey.F7:
                        enc[1] = 0x37;
                        break;
                    case ConsoleKey.F8:
                        enc[1] = 0x38;
                        break;
                    case ConsoleKey.F9:
                        enc[1] = 0x39;
                        break;
                    case ConsoleKey.F10:
                        enc[1] = 0x30;
                        break;
                    case ConsoleKey.F11:
                        enc[1] = 0x2A;
                        break;
                    case ConsoleKey.F12:
                        enc[1] = 0x28;
                        break;
                    default:
                        break;
                }
            }
            else if (keyInfo.Key >= ConsoleKey.LeftArrow // if key is Arrow
                    && keyInfo.Key <= ConsoleKey.DownArrow)
            {
                enc[1] = 0x5B; // bracket

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        enc[2] = 0x41; // A
                        break;
                    case ConsoleKey.DownArrow:
                        enc[2] = 0x42; // B
                        break;
                    case ConsoleKey.RightArrow:
                        enc[2] = 0x43; // C
                        break;
                    case ConsoleKey.LeftArrow:
                        enc[2] = 0x44; // D
                        break;
                    default:
                        break;
                }

            }
            else if (keyInfo.Key == ConsoleKey.Enter) // if key is Enter
            {
                byte enc_length;

                if (enterEncodeCRLF)
                {
                    enc_length = 2;
                    enc = new byte[2] { 0x0D, 0x0A };
                }
                else
                {
                    enc_length = 1;
                    enc = new byte[1] { 0x0D };
                }

                if (scrData != string.Empty && scrData.Length > 0)
                {
                    // get screen data bytes
                    byte[] scrPayload = Encoding.UTF8.GetBytes(scrData);

                    // flush screen data
                    scrData = string.Empty;

                    // create new serialized packet with screen bytes and return payload
                    enc = new byte[(scrPayload.Length + enc_length)];

                    Buffer.BlockCopy(scrPayload, 0, enc, 0, scrPayload.Length);

                    // Add return key
                    enc[scrPayload.Length] = 0x0D;
                    if (enterEncodeCRLF)
                    {
                        enc[(scrPayload.Length + 1)] = 0x0A;
                    }
                }
            }
            else if (keyInfo.Key == ConsoleKey.Delete)
            {
                // ^[3~"
                enc = new byte[4];
                enc[0] = 0x1B;
                enc[1] = 0x5B; // bracket
                enc[2] = 0x33;
                enc[3] = 0x7E;
            }
            else if (keyInfo.Key == ConsoleKey.Escape) // Escape
            {
                enc = new byte[1];
                enc[0] = 0x1B;
            }
            else if (keyInfo.Key == ConsoleKey.X &&
                 keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + X
            {
                // Issue a graceful terminate.
                SharedFunc.SetSerialSession(false);

                // Clear console window
                Console.Clear();
            }
            else if (keyInfo.Key == ConsoleKey.C &&
                keyInfo.Modifiers == ConsoleModifiers.Control) // Ctrl + C
            {
                enc = new byte[1] { 0x03 };
            }
            else if (keyInfo.Key == ConsoleKey.Tab) // TAB
            {
                enc = new byte[1];
                enc[0] = 0x09;
            }

            return enc;
        }
    }
}
