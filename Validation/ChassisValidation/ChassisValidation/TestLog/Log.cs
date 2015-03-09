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
using System.Text;

namespace ChassisValidation
{
    public class Log
    {
        private static LogLevel level = LogLevel.Info;

        public static LogLevel Level
        {
            get
            {
                return level;
            }
            set
            {
                level = value;
            }
        }

        private static readonly List<ILogger> loggers = new List<ILogger>();

        public static ICollection<ILogger> Loggers
        {
            get
            {
                return loggers;
            }
        }

        public static void Error(string message, Exception ex = null)
        {
            if (level <= LogLevel.Error)
            {
                DoLog(LogLevel.Error, null, message, ex);
            }
        }

        public static void Error(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Error)
            {
                DoLog(LogLevel.Error, category, message, ex);
            }
        }

        public static void Notice(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Notice)
            {
                DoLog(LogLevel.Notice, category, message, ex);
            }
        }

        public static void Notice(string message, Exception ex = null)
        {
            if (level <= LogLevel.Notice)
            {
                DoLog(LogLevel.Notice, null, message, ex);
            }
        }

        public static void Info(string message, Exception ex = null)
        {
            if (level <= LogLevel.Info)
            {
                DoLog(LogLevel.Info, null, message, ex);
            }
        }

        public static void Info(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Info)
            {
                DoLog(LogLevel.Info, category, message, ex);
            }
        }

        public static void Success(string message, Exception ex = null)
        {
            if (level <= LogLevel.Success)
            {
                DoLog(LogLevel.Success, null, message, ex);
            }
        }

        public static void Success(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Success)
            {
                DoLog(LogLevel.Success, category, message, ex);
            }
        }

        public static void Warning(string message, Exception ex = null)
        {
            if (level <= LogLevel.Warning)
            {
                DoLog(LogLevel.Warning, null, message, ex);
            }
        }

        public static void Warning(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Warning)
            {
                DoLog(LogLevel.Warning, category, message, ex);
            }
        }
        public static void Verbose(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Verbose)
            {
                DoLog(LogLevel.Verbose, category, message, ex);
            }
        }

        public static void Debug(string category, string message, Exception ex = null)
        {
            if (level <= LogLevel.Debug)
            {
                DoLog(LogLevel.Debug, category, message, ex);
            }
        }

        protected static void DoLog(LogLevel logLevel, String category, String message, Exception exception)
        {
            const int exceptionLeftPadding = 9;

            var builder = new StringBuilder();

            builder.AppendFormat("{0}  ", DateTime.Now.ToString("T"));

            if (!string.IsNullOrWhiteSpace(category))
            {
                builder.AppendFormat("[{0}] ", category);
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.Append(message);
            }

            if (exception != null)
            {
                builder.Append(Environment.NewLine);
                builder.Append(' ', exceptionLeftPadding);
                builder.Append(exception.ToString());
            }

            var stringLine = builder.ToString();

            // write log to each logger
            loggers.ForEach(logger =>
            {
                if (logger != null)
                {
                    logger.WriteLine(logLevel, stringLine);
                }
            });
        }
    }
}
