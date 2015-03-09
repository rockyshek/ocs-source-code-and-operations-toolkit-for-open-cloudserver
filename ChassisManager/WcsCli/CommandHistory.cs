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

namespace Microsoft.GFS.WCS.WcsCli
{
    /// <summary>
    /// This class stores WCSCLI commands executed
    /// </summary>
    public class CommandHistory
    {
        /// <summary>
        /// String array to store history
        /// </summary>
        string[] cmdhistory;

        /// <summary>
        /// Head 
        /// </summary>
        int head;

        /// <summary>
        /// Tail end of the array
        /// </summary>
        int tail;

        /// <summary>
        /// Cursor to indicate current record in array
        /// </summary>
        int cursor;
        
        /// <summary>
        /// Total count of elements in history
        /// </summary>
        int count;

        /// <summary>
        /// Max size of array
        /// </summary>
        int Size;

        public CommandHistory(int size)
        {
            // Initialize array to the max size specified
            cmdhistory = new string[size];

            // Initialize size
            Size = size;

            // Initialize to 0 when the array is empty
            head = tail = cursor = 0;
        }

        /// <summary>
        /// Appends an element to history
        /// </summary>
        /// <param name="s"></param>
        public void Append(string s)
        {
            if (head < Size - 1)
            {
                cmdhistory[head] = s;
                head = (head + 1) % cmdhistory.Length;
                if (head == tail)
                    tail = (tail + 1) % cmdhistory.Length;
                if (count != cmdhistory.Length)
                    count++;
            }
            else if (head == (Size - 1))
            {
                cmdhistory[head] = s;
                head = head + 1;
                count++;
            }
            else
            {
                // delete an element from tail
                List<string> historyList = cmdhistory.ToList();
                historyList.RemoveAt(0);

                // append gievn string
                historyList.Add(s);

                // convert back to array for traversing
                cmdhistory = historyList.ToArray();
            }

        }

        public void CursorToEnd()
        {
            if (head == tail)
                return;

            cursor = head;
        }

        /// <summary>
        /// Removes the last element from history
        /// </summary>
        public void RemoveLast()
        {
            head = head - 1;
            if (head < 0)
                head = cmdhistory.Length - 1;
        }

        /// <summary>
        /// Accept the given element
        /// </summary>
        /// <param name="s"></param>
        public void Accept(string s)
        {
            int t = head - 1;
            if (t < 0)
                t = cmdhistory.Length - 1;

            cmdhistory[t] = s;
        }

        /// <summary>
        /// Update the current line to history
        /// </summary>
        /// <param name="s"></param>
        public void Update(string s)
        {
            if (cursor <= (Size - 1))
            {
                cmdhistory[cursor] = s;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    // delete an element from tail
                    List<string> historyList = cmdhistory.ToList();
                    historyList.RemoveAt(0);

                    // append gievn string
                    historyList.Add(s);

                    // convert back to array for traversing
                    cmdhistory = historyList.ToArray();
                }
            }
        }

        /// <summary>
        /// Check if previous executed is available
        /// </summary>
        /// <returns></returns>
        public bool PreviousAvailable()
        {
            if (count == 0 || cursor == tail)
                return false;

            return true;
        }

        /// <summary>
        /// Check if next is available
        /// </summary>
        /// <returns></returns>
        public bool NextAvailable()
        {
            int next = cursor + 1;
            if (count == 0 || next >= head)
                return false;

            return true;
        }

        /// <summary>
        /// Get Previous value
        /// </summary>
        /// <returns></returns>
        public string Previous()
        {
            if (!PreviousAvailable())
                return null;

            cursor--;
            if (cursor < 0)
                cursor = cmdhistory.Length - 1;

            return cmdhistory[cursor];
        }

        /// <summary>
        /// Get next value
        /// </summary>
        /// <returns></returns>
        public string Next()
        {
            if (!NextAvailable())
                return null;

            cursor = (cursor + 1) % cmdhistory.Length;
            return cmdhistory[cursor];
        }

        /// <summary>
        /// Increment cursor 
        /// </summary>
        /// <returns></returns>
        public void IncrementCursor()
        {
            if (cursor + 1 >= cmdhistory.Length)
            {
                cursor = 0;
            }
            else
            {
                cursor = cursor + 1;
            }
        }

        /// <summary>
        /// Clear history cache
        /// </summary>
        public void Clear()
        {
            // Clear array.
            Array.Clear(cmdhistory, 0, cmdhistory.Length);

            // Reset pointers.
            head = tail = cursor = 0;
        }
    }
}
