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

namespace ChassisValidation
{
    public class ConsoleLogger : ILogger
    {
        public void WriteLine(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Error :
                    this.WriteConsole(message, ConsoleColor.Red) ;
                    break;
                case LogLevel.Warning:
                    this.WriteConsole(message, ConsoleColor.Yellow);
                    break;
                case LogLevel.Success:
                    this.WriteConsole(message, ConsoleColor.Green);
                    break;
                case LogLevel.Notice :
                    this.WriteConsole(message, ConsoleColor.DarkCyan) ;
                    break;
                case LogLevel.Verbose :
                    this.WriteConsole(message, ConsoleColor.Gray) ;
                    break;
                case LogLevel.Debug :
                    this.WriteConsole(message, ConsoleColor.Gray) ;
                    break;
                default :
                    this.WriteConsole(message, ConsoleColor.White) ;
                    break;
            }
        }

        private void WriteConsole(string message, ConsoleColor color) 
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
