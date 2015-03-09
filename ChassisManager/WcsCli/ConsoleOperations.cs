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
using System.Threading;

namespace Microsoft.GFS.WCS.WcsCli
{
    internal class ConsoleOperations
    {
        #region internal variables

        /// <summary>
        /// Current thread
        /// </summary>
        Thread currentProcessingThread;

        /// <summary>
        /// The text being written
        /// </summary>
        StringBuilder buffer;

        /// <summary>
        /// Prompt to display
        /// </summary>
        string prompt;

        /// <summary>
        /// Current cursor position
        /// </summary>
        int cursorPtr;

        /// <summary>
        /// Flag that indicates if processing completed
        /// </summary>
        bool isProcessingCompleted = false;

        /// <summary>
        /// History object to store command history
        /// </summary>
        CommandHistory cmdhistory;


        #endregion        

        /// <summary>
        /// 
        /// </summary>
        internal ConsoleOperations()
        {
            // Initialize WCSCLI prompt
            prompt = WcsCliConstants.consoleString + " ";
            
            // Initialize command history
            cmdhistory = new CommandHistory(20);
        }

        #region Console processing

        /// <summary>
        /// This method processes input received on the console
        /// </summary>
        /// <returns></returns>
        public string ProcessInput()
        {
            currentProcessingThread = Thread.CurrentThread;
            Console.CancelKeyPress += InterruptProcess;

            isProcessingCompleted = false;

            cmdhistory.CursorToEnd();
            DisplayText("");
            cmdhistory.Append("");

            while (!isProcessingCompleted)
            {
                try
                {
                    Handlekeys();
                }
                catch (ThreadAbortException)
                {
                    HandleException();
                }
            }

            Console.WriteLine();

            Console.CancelKeyPress -= InterruptProcess;

            if (buffer == null)
            {
                return null;
            }

            string result = buffer.ToString();
            if (result != "")
                cmdhistory.Accept(result);
            else
                cmdhistory.RemoveLast();
            
            return result;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initial"></param>
        void DisplayText(string textToDisplay)
        {
            buffer = new StringBuilder(textToDisplay);
            cursorPtr = buffer.Length;
            Console.Write(prompt);
            Console.Write(textToDisplay);
            
        }

        /// <summary>
        /// Set text on the console window
        /// </summary>
        /// <param name="newtext"></param>
        void SetText(string newtext)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            DisplayText(newtext);
        }

        /// <summary>
        /// Write text to console
        /// </summary>
        /// <param name="textToWrite"></param>
        void WriteText(string textToWrite)
        {
            Console.Write(textToWrite);
            for (int i = 0; i < textToWrite.Length; i++)
            {
                buffer = buffer.Insert(cursorPtr, textToWrite[i]);
                cursorPtr++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Handlekeys()
        {
            ConsoleKeyInfo consoleKey;

            while (!isProcessingCompleted)
            {
                bool isKeyHandled = false;
                consoleKey = Console.ReadKey(true);
                if (consoleKey.Key == ConsoleKey.Escape)
                {
                    buffer.Length = 0;
                    ProcessingComplete();
                    break;
                }

                // handle the key enetered
                switch (consoleKey.Key)
                {
                    case ConsoleKey.Backspace:  isKeyHandled = true;
                                                HandleBackspace();
                                                break;
                    case ConsoleKey.Delete:     isKeyHandled = true;
                                                HandleDeleteChar();
                                                break;
                    case ConsoleKey.LeftArrow: isKeyHandled = true;
                                                HandleLeftArrow();
                                                break;
                    case ConsoleKey.RightArrow: isKeyHandled = true;
                                                HandleRightArrow();
                                                break;
                    case ConsoleKey.DownArrow: isKeyHandled = true;
                                                HandleDownArrow();
                                                break;
                    case ConsoleKey.UpArrow:    isKeyHandled = true;
                                                HandleUpArrow();
                                                break;
                    case ConsoleKey.Enter:      isKeyHandled = true;
                                                ProcessingComplete();
                                                break;
                    case ConsoleKey.Tab:        isKeyHandled = true;
                                                HandleTab();
                                                break;
                    case ConsoleKey.Escape:     isKeyHandled = true;
                                                HandleEscape();
                                                break;
                }

                // Check if key was handled
                if (isKeyHandled)
                {
                    continue;
                }
                else if (consoleKey.KeyChar != (char)0)
                {
                    InsertToBuffer(consoleKey.KeyChar);
                }
            }
        }

        /// <summary>
        /// Insert character to input buffer
        /// </summary>
        /// <param name="c">Char to add</param>
        void InsertToBuffer(char c)
        {
            int line = 0;

            // Insert to buffer
            buffer = buffer.Insert(cursorPtr, c);
            cursorPtr++;

            // get the previous cursor position
            int prevCursorPosition = Console.CursorLeft;

            // If max window size is reached, start writing on the next line
            if (buffer.Length > (Console.BufferWidth - prompt.Length))
            {
                // Calculate the number of lines based on the buffer length
                line = ((buffer.Length + prompt.Length) / Console.BufferWidth);

                // If text exceeds the current line, calculate number of lines, else default value is 0
                if (cursorPtr > (Console.BufferWidth - prompt.Length))
                {
                    if (Console.CursorTop - line >= 0)
                    {
                        // write the prompt and the current line
                        Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                    }
                }
                else
                {
                    // write the prompt and the current line
                    Console.SetCursorPosition(prompt.Length, Console.CursorTop);
                }
                Console.Write(buffer);
            }
            else
            {
                Console.SetCursorPosition(prompt.Length, Console.CursorTop);
                Console.Write(buffer);
            }

            // Set to previous cursor position + 1
            if (prevCursorPosition + 1 < Console.BufferWidth)
            {
                if (cursorPtr < (Console.BufferWidth - prompt.Length))
                {
                    if (Console.CursorTop - line >= 0 && (prevCursorPosition + 1) < Console.BufferWidth)
                    {
                        Console.SetCursorPosition(prevCursorPosition + 1, Console.CursorTop - line);
                    }
                }
                else
                {
                    if (prevCursorPosition + 1 < Console.BufferWidth)
                    {
                        Console.SetCursorPosition(prevCursorPosition + 1, Console.CursorTop);
                    }
                }

            }
            else
            {
                Console.SetCursorPosition(0, Console.CursorTop);
            }
        }

        #endregion

        #region Handle keys

        /// <summary>
        /// Handle left arrow key
        /// </summary>
        void HandleLeftArrow()
        {
            if (cursorPtr == 0 || cursorPtr == cursorPtr -1)
                return;
            cursorPtr = cursorPtr - 1;

            // For multi-line we need to jump to previous line if left arrow is issued at the cursor position 0, 
            // therefore we need to check before decrementing the cursor 
            if (Console.CursorLeft - 1 >= 0)
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
            else
            {
                if (Console.CursorTop - 1 >= 0)
                {
                    Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                }
            }
        }

        /// <summary>
        /// Handle right arrow key
        /// </summary>
        void HandleRightArrow()
        {
            if (cursorPtr == buffer.Length)
                return;
            cursorPtr = cursorPtr + 1;

            // For multi-line we need to move cursor to next line if we reach end of current line, 
            // therefore we need to check before incrementing the cursor if it is less than console window width
            if (Console.CursorLeft + 1 < Console.BufferWidth)
            {
                Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
            }
            else
            {
                if (Console.CursorTop + 1 <= Console.BufferHeight)
                {
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                }
            }
        }

        /// <summary>
        /// Handle UP arrow key to get the previous command executed
        /// </summary>
        void HandleUpArrow()
        {
            if (!cmdhistory.PreviousAvailable())
            {
                return;
            }

            cmdhistory.Update(buffer.ToString());

            string value = cmdhistory.Previous();

            if (value != null)
            {
                 // If max window size is reached, start writing on the next line
                if (buffer.Length > (Console.WindowWidth - prompt.Length))
                {
                    // Calculate the number of lines based on the buffer length and window size
                    int line = ((buffer.Length + prompt.Length) / Console.BufferWidth);

                    if (cursorPtr > (Console.BufferWidth - prompt.Length))
                    {
                        ClearConsoleLine();
                        if (Console.CursorTop - line >= 0)
                        {
                            Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                        }
                        ClearConsoleLine();
                    }
                    else
                    {
                        if (Console.CursorTop + 1 <= Console.BufferHeight)
                        {
                            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop + 1);
                        }
                        ClearConsoleLine();
                        if (Console.CursorTop - 1 >= 0)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                        }
                        if (Console.CursorTop - line >= 0)
                        {
                            Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                        }
                    }                
                }
                else
                {
                    ClearConsoleLine();
                }
                SetText(value);
            }
        }

        /// <summary>
        /// Handle DOWN arrow key to get the next command executed
        /// </summary>
        void HandleDownArrow()
        {
            if (!cmdhistory.NextAvailable())
            {
                return;
            }

            cmdhistory.Update(buffer.ToString());

            string value = cmdhistory.Next();

            if (value != null)
            {
                // If max window size is reached, start writing on the next line
                if (buffer.Length > (Console.BufferWidth - prompt.Length))
                {
                    // Calculate the number of lines based on the buffer length and window size
                    int line = ((buffer.Length + prompt.Length) / Console.BufferWidth);

                    if (cursorPtr > (Console.BufferWidth - prompt.Length))
                    {
                        ClearConsoleLine();
                        if (Console.CursorTop - line >= 0)
                        {
                            Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                        }
                        ClearConsoleLine();
                    }
                    else
                    {
                        if (Console.CursorTop + 1 <= Console.BufferHeight)
                        {
                            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop + 1);
                        }
                        ClearConsoleLine();
                        if (Console.CursorTop - 1 >= 0)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                        }
                        if (Console.CursorTop - line >= 0)
                        {
                            Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                        }
                    }
                }
                else
                {
                    ClearConsoleLine();
                }
                SetText(value);
            }
        }

        /// <summary>
        /// Indicates processing completed
        /// </summary>
        void ProcessingComplete()
        {
            this.isProcessingCompleted = true;
        }

        /// <summary>
        /// Handle backspace key
        /// </summary>
        void HandleBackspace()
        {
            int line = 0;
            if (cursorPtr == 0)
                return;

            int prevCursorPtrValue = cursorPtr;
            int prevCursorLeft = Console.CursorLeft;

            // Calculate the number of lines based on the buffer length and window size
            line = ((buffer.Length + prompt.Length) / Console.BufferWidth);

            if((prompt.Length + buffer.Length) == Console.BufferWidth && prevCursorLeft == 0)
            {
                if (Console.CursorTop - 1 >= 0)
                {
                    Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                    Console.Write(" ");
                    Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                    line = line - 1;
                }
            }
            else
            {
                // print backspace and erase the last character from console
                Console.Write("\b");
                Console.Write(" ");
                Console.Write("\b");

                if (prevCursorPtrValue < (Console.BufferWidth - prompt.Length))
                {
                    line = 0;
                }
            }
           
            // clear console line
            if (cursorPtr < (Console.BufferWidth - prompt.Length) && buffer.Length > (Console.BufferWidth - prompt.Length))
            {
                if (Console.CursorTop + 1 < Console.BufferHeight)
                {
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop + 1);
                }
                ClearConsoleLine();

                if (Console.CursorTop - 1 >= 0)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
            }
            else
            {
                ClearConsoleLine();
            }

            // Remove from buffer
            buffer.Remove(--cursorPtr, 1);

            // Reposition cursor to start writing the text
            if (Console.CursorTop - line >= 0)
            {
                Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
            }

            SetText(buffer.ToString());

            if (prevCursorLeft - 1 >= 0)
            {
                // Move up 'n' lines to the previous cursor position - 1
                if (prevCursorPtrValue < (Console.BufferWidth - prompt.Length) && buffer.Length >= (Console.BufferWidth - prompt.Length))
                {
                    if (Console.CursorTop - 1 >= 0)
                    {
                        Console.SetCursorPosition(prevCursorLeft - 1, Console.CursorTop - 1);
                    }
                }
                else
                {
                    Console.SetCursorPosition(prevCursorLeft - 1, Console.CursorTop);
                }
            }
            else
            {
                if(Console.CursorTop -1 >= 0)
                {
                    if(buffer.Length > (Console.BufferWidth -prompt.Length))
                    {                        
                        Console.CursorTop--;
                    }

                    Console.SetCursorPosition(Console.BufferWidth- 1, Console.CursorTop);
                }
            }

            cursorPtr = prevCursorPtrValue - 1;
           
        }

        /// <summary>
        /// Delete a character from console
        /// </summary>
        void HandleDeleteChar()
        {
            // Check if there is no input
            if (buffer.Length == 0)
            {
                return;
            }
            // Check if buffer length is same as cursor position
            else if (buffer.Length == cursorPtr)
            {
                // Do nothing
                return;
            }
            else
            {
                buffer.Remove(cursorPtr, 1);

                int prevPosition = Console.CursorLeft;

                // Calculate the number of lines based on the buffer length and window size
                int line = ((buffer.Length + prompt.Length) / Console.BufferWidth);

                // Reposition cursor to start writing the text
                if (cursorPtr < (Console.BufferWidth - prompt.Length))
                {
                    if(buffer.Length < (Console.BufferWidth - prompt.Length))
                    {
                        ClearConsoleLine();
                    }
                    else
                    {
                        if (Console.CursorTop + 1 <= Console.BufferHeight)
                        {
                            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop + 1);
                        }
                        ClearConsoleLine();
                        if (Console.CursorTop - 1 >= 0)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                        }
                    }

                    Console.SetCursorPosition(prompt.Length, Console.CursorTop);

                    SetText(buffer.ToString());
                    if (Console.CursorTop - line >= 0)
                    {
                        Console.SetCursorPosition(prevPosition, Console.CursorTop - line);
                    }
                    cursorPtr = prevPosition - prompt.Length;
                }
                else
                {
                    ClearConsoleLine();
                    if (Console.CursorTop - line >= 0)
                    {
                        Console.SetCursorPosition(prompt.Length, Console.CursorTop - line);
                    }

                    SetText(buffer.ToString());
                    Console.SetCursorPosition(prevPosition, Console.CursorTop);
                    cursorPtr = (prevPosition - prompt.Length) + Console.BufferWidth * line;
                }
            }
        }

        /// <summary>
        /// Handle Tab key, by autocompleting the command
        /// </summary>
        void HandleTab()
        {
            string searchWord;

            if (buffer.ToString().Contains(' '))
            {
                searchWord = buffer.ToString().Split(' ').Last();
            }
            else
            {
                searchWord = buffer.ToString();
            }

            // Send the word string to Auto tab handler
            string[] results = TabHelper.GetTabOptions(searchWord.ToLower());

            if (results == null || results.Length == 0)
                return;

            int count = results.Length;
            bool flagDone = false;

            // if there is just one matching string, print it to string.
            if (results.Length == 1)
            {
                WriteText(results[0]);
            }
            else
            {
                int countDisplayedOptions = -1;
                int p = 0;

                // find the first character that differs from user entered string
                for (p = 0; p < results[0].Length; p++)
                {
                    char c = results[0][p];

                    for (int i = 1; i < count; i++)
                    {
                        if (results[i].Length < p)
                        {   
                            DisplayTabOptions(countDisplayedOptions, results, searchWord);
                            flagDone = true;
                            break;
                        }

                        if (results[i][p] != c)
                        {
                            DisplayTabOptions(countDisplayedOptions, results, searchWord);
                            flagDone = true;
                            break;
                        }
                    }

                    if (!flagDone)
                    {
                        countDisplayedOptions = p;
                    }
                    else
                    {
                        break;
                    }
                }

                // Handle the case when the command string is a subset of another existing command string
                // Eg. setbladepowerlimit is a subset of setbladepowerlimiton/off
                if (!flagDone)
                {
                    countDisplayedOptions = p - 1;
                    DisplayTabOptions(countDisplayedOptions, results, searchWord);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        void DisplayTabOptions(int options,string[] results, string searchWord )
        {
            if (options != -1)
            {
                WriteText(results[0].Substring(0, options + 1));
            }
            Console.WriteLine();
            int countPerline = 3;
            int cntr = 0;
            foreach (string s in results)
            {
                Console.Write(searchWord);
                Console.Write(s);
                Console.Write(' ');
                cntr++;
                if (cntr == countPerline)
                {
                    Console.WriteLine();
                    cntr = 0;
                }
            }
            Console.WriteLine();
            Console.WriteLine();
            DisplayText(buffer.ToString());
        }

        void HandleEscape()
        {
            Console.Clear();
        }

        #endregion

        #region Command history operations

        /// <summary>
        /// Clear history buffer
        /// </summary>
        public void ClearConsoleHistory()
        {
            try
            {
                lock (this)
                {
                    if (cmdhistory != null)
                    {
                        // call internal method to clear history
                        cmdhistory.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to clear history for WCSCLI console: " + ex);
            }
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Clear console line
        /// </summary>
        private void ClearConsoleLine()
        {
            // Get current line
            int currentLineCursor = Console.CursorTop;

            Console.SetCursorPosition(0, currentLineCursor);

            // clear line
            Console.Write(new string(' ', Console.BufferWidth -1));

            Console.SetCursorPosition(0, currentLineCursor);
        }

        /// <summary>
        /// Handle thread exception
        /// </summary>
        private void HandleException()
        {
            // Abort the thread
            Thread.ResetAbort();

            // End the process if there is an exception
            currentProcessingThread.Abort();
        }
        
        #endregion

        #region end processing

        /// <summary>
        /// This method processes the interupt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        void InterruptProcess(object sender, ConsoleCancelEventArgs a)
        {
            // Do not abort our program:
            a.Cancel = true;

            // Interrupt the editor
            currentProcessingThread.Abort();
        }

        #endregion

    }


}
