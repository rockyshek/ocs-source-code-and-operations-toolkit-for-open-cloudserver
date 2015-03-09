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
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.GFS.WCS.WcsCli
{
    static class NativeMethods 
    {
        /// <summary>
        /// ForeGround Color Intensity
        /// </summary>
        private const int FR_INTENSITY = 0x00000008;

        /// <summary>
        /// Background Color Intensity
        /// </summary>
        private const int BK_INTENSITY = 0x00000080;

        /// <summary>
        /// Console output handle
        /// </summary>
        private const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// Console input handle
        /// </summary>
        private const int STD_INPUT_HANDLE = -10;

        /// <summary>
        /// Console Output handle
        /// </summary>
        private static IntPtr hConsoleOut;

        /// <summary>
        /// Console Input handle
        /// </summary>
        private static IntPtr hConsoleIn;

        /// <summary>
        /// Gets the pointer to the current console window
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", EntryPoint = "WriteConsole", SetLastError = true,
        CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WriteConsole(IntPtr hConsoleOutput, string lpBuffer,
        uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten,
        IntPtr lpReserved);

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true,
        CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint="GetConsoleScreenBufferInfo",
        SetLastError=true, CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput,
                         ref CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", EntryPoint="SetConsoleTextAttribute",
        SetLastError=true, CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
        private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ushort wAttributes);

        [DllImport("kernel32.dll", EntryPoint = "SetConsoleMode",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleMode",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, ref ushort mode);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleCP",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern uint GetConsoleCP();

        [DllImport("kernel32.dll", EntryPoint = "SetConsoleCP",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetConsoleCP(uint codePage);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleOutputCP",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern uint GetConsoleOutputCP();

        [DllImport("kernel32.dll", EntryPoint = "SetConsoleOutputCP",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetConsoleOutputCP(uint codePage); 
 
        [StructLayout(LayoutKind.Sequential)] 
        private struct COORD
         {
            short X;
            short Y;
         }
            
        [StructLayout(LayoutKind.Sequential)] 
        private struct SMALL_RECT
         {
            short Left;
            short Top;
            short Right;
            short Bottom;
         }

        [StructLayout(LayoutKind.Sequential)] 
        private struct CONSOLE_SCREEN_BUFFER_INFO
         {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public ushort wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
         }

        // Constructor.
        static NativeMethods()
        {
           // Get Console Handles.
           hConsoleOut = GetStdHandle(STD_OUTPUT_HANDLE);
           hConsoleIn = GetStdHandle(STD_INPUT_HANDLE);
        }

        /// <summary>
        /// Adds forground color intensity
        /// </summary>
        internal static void AddIntensity()
        {
            CONSOLE_SCREEN_BUFFER_INFO ConsoleInfo = new CONSOLE_SCREEN_BUFFER_INFO();
            GetConsoleScreenBufferInfo(hConsoleOut, ref ConsoleInfo);
            SetConsoleTextAttribute(hConsoleOut, (ushort)(ConsoleInfo.wAttributes | FR_INTENSITY));
        }

        /// <summary>
        /// Decreases forground color intensity
        /// </summary>
        internal static void RemoveIntensity()
        {
            CONSOLE_SCREEN_BUFFER_INFO ConsoleInfo = new CONSOLE_SCREEN_BUFFER_INFO();
            GetConsoleScreenBufferInfo(hConsoleOut, ref ConsoleInfo);
            SetConsoleTextAttribute(hConsoleOut, (ushort)(ConsoleInfo.wAttributes & (~FR_INTENSITY)));
        }

        /// <summary>
        /// Write payload to the Console.
        /// </summary>
        internal static uint WriteConsole(string payload)
        {
            uint written;
            WriteConsole(hConsoleOut, payload, (uint)payload.Length, out written, IntPtr.Zero);
            return written;
        }

        /// <summary>
        /// Enables Console Word Wrap
        /// </summary>
        internal static void EnableWordWrap()
        {
            if (!SetConsoleMode(hConsoleOut, 3))
            {
                Debug.WriteLine("EnableWordWrap Error Attempting: SetConsoleMode");
            }
        }

        /// <summary>
        /// Disables Console Word Wrap
        /// </summary>
        internal static void DisableWordWrap()
        {
            if (!SetConsoleMode(hConsoleOut, 1))
            {
                Debug.WriteLine("DisableWordWrap Error Attempting: SetConsoleMode");
            }
        }

        /// <summary>
        /// Set Console Code Page
        /// </summary>
        internal static void SetCodePage(uint codepage)
        {
            if (!SetConsoleOutputCP(codepage))
            {
                Debug.WriteLine("SetConsoleOutputCP Error Attempting: SetConsoleOutputCP");
            }
        }

        /// <summary>
        ///  Get Current Code Page
        /// </summary>
        internal static uint GetCodePage()
        {
            return GetConsoleOutputCP();
        }



      }
}
