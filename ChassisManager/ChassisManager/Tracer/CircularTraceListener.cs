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

namespace Microsoft.GFS.WCS.ChassisManager
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// This class defines a circular trace listener
    /// </summary>
    public class CircularTraceListener : XmlWriterTraceListener
    {
        /// <summary>
        /// Circular stream
        /// </summary>
        private CircularStream m_stream = null;

        /// <summary>
        /// lock object to make the trace listener thread safe
        /// </summary>
        private Object TraceLockObject = new Object();

        #region Member Functions

        /// <summary>
        /// Determine if number of bytes written is greater than max size, if yes switch stream.
        /// </summary>
        private void DetermineOverQuota()
        {
            //If we're past the Quota, flush, then switch files
      
            if (m_stream.IsOverQuota)
            {
                base.Flush();
                m_stream.SwitchFiles();
            }
        }

        #endregion

        #region XmlWriterTraceListener Functions

        public CircularTraceListener(CircularStream stream)
            : base(stream)
        {
            this.m_stream = stream;
        }

        /// <summary>
        /// Tracelistener is thread safe here -all key operations are performed from a lock.
        /// Trace class does a get on this property, if listener is not thread safe it takes a global lock 
        /// which can be a performance bottleneck. hence to avoid that set UseGlobalLock property to false in app.config and 
        /// make tracelistener thread safe instead.
        /// </summary>
        public override bool IsThreadSafe
        {
            get
            {
                return true;
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            lock (this.TraceLockObject)
            {
                this.DetermineOverQuota();
                base.TraceEvent(eventCache, source, eventType, id);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            lock (this.TraceLockObject)
            {
                this.DetermineOverQuota();
                base.TraceEvent(eventCache, source, eventType, id, format, args);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            lock (this.TraceLockObject)
            {
                this.DetermineOverQuota();
                base.TraceEvent(eventCache, source, eventType, id, message);
            }
        }

        /// <summary>
        /// Clear trace log
        /// </summary>
        /// <returns></returns>
        public bool ClearTrace()
        {
            lock (TraceLockObject)
            {
                return (m_stream.Clear());
            }
        }

        /// <summary>
        /// Get current file path
        /// </summary>
        /// <returns></returns>
        public string GetFilePath()
        {
            lock (TraceLockObject)
            {
                return m_stream.GetCurrentFilePath();
            }
        }
        
        /// <summary>
        /// Get path for all log files
        /// </summary>
        /// <returns></returns>
        public string[] GetAllFilePaths()
        {
            lock (TraceLockObject)
            {
                return m_stream.GetAllFilePaths();
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (TraceLockObject)
            {
                m_stream.Dispose();
                base.Dispose(disposing);
            }
        }

        #endregion

    }

    public class CircularStream : System.IO.Stream
    {
        private FileStream[] fStream = null;
        private String[] fPath = null;
        private long dataWritten = 0;
        private int fileQuota = 0;
        private int currentFile = 0;
        private string stringWritten = string.Empty;

        /// <summary>
        /// Inititialize a new filestream, using provided filename or default.
        /// </summary>
        /// <param name="fileName"></param>
        public CircularStream(string fileName, int maxFileSize)
        {
            try
            {
                // MaxFileSize is in KB in the configuration file, convert to bytes
                this.fileQuota = maxFileSize * 1024;

                string filePath = Path.GetDirectoryName(fileName);
                string fileBase = Path.GetFileNameWithoutExtension(fileName);
                string fileExt = Path.GetExtension(fileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = AppDomain.CurrentDomain.BaseDirectory;
                }

                fPath = new String[2];

                //Add 00 and 01 to FileNames and open streams
                fPath[0] = Path.Combine(filePath, fileBase + "00" + fileExt);
                fPath[1] = Path.Combine(filePath, fileBase + "01" + fileExt);

                fStream = new FileStream[2];
                fStream[0] = new FileStream(fPath[0], FileMode.Create);

                if (Tracer.chassisManagerEventLog != null)
                    Tracer.chassisManagerEventLog.WriteEntry("Circular stream created");
            }
            catch (IOException ex)
            {
                if (Tracer.chassisManagerEventLog != null)
                    Tracer.chassisManagerEventLog.WriteEntry("Trace/user Logging cannot be done. Exception: " + ex);
            }
        }

        /// <summary>
        /// Switch files
        /// </summary>
        public void SwitchFiles()
        {
            try
            {
                //Close current file, open next file (deleting its contents)                         
                dataWritten = 0;
                fStream[currentFile].Dispose();

                currentFile = (currentFile + 1) % 2;

                fStream[currentFile] = new FileStream(fPath[currentFile], FileMode.Create);
            }
            catch (Exception ex)
            {
                if (Tracer.chassisManagerEventLog != null)
                    Tracer.chassisManagerEventLog.WriteEntry("Trace/user Logging cannot be done. Exception: " + ex);
            }
        }

        /// <summary>
        /// Get trace current file path
        /// </summary>
        /// <returns>Current trace file path</returns>
        public string GetCurrentFilePath()
        {
            try
            {
                return fPath[currentFile];
            }
            catch (Exception ex)
            {
                Trace.TraceError("exception occured while fetching current File path" + ex);
                return null;
            }
        }

        /// <summary>
        /// Get path of all version of log files
        /// </summary>
        /// <returns>Path(s) of all version of log files</returns>
        internal string[] GetAllFilePaths()
        {
            string[] filePaths = new string[2];
            try
            {
                filePaths[0] = fPath[(currentFile+1)%2];
                filePaths[1] = fPath[currentFile];
                return filePaths;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while fetching the non-current File path.. " + ex);
                return null;
            }
        }

        /// <summary>
        /// Property IsOverQuota
        /// </summary>
        public bool IsOverQuota
        {
            get
            {
                return (dataWritten >= fileQuota);
            }

        }

        /// <summary>
        /// Property CanRead
        /// </summary>
        public override bool CanRead
        {
            get
            {
                try
                {
                    return fStream[currentFile].CanRead;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while getting Filestream CanRead property" + ex);
                    return true;
                }
            }
        }

        /// <summary>
        /// Property CanSeek
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                try
                {
                    return fStream[currentFile].CanSeek;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while getting Filestream CanSeek property" + ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Get Filestream Length
        /// </summary>
        public override long Length
        {
            get
            {
                try
                {
                    return fStream[currentFile].Length;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while getting Filestream length property" + ex);
                    return -1;
                }
            }
        }

        /// <summary>
        /// Get/set Filestream position
        /// </summary>
        public override long Position
        {
            get
            {
                try
                {
                    return fStream[currentFile].Position;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while getting Filestream Position property" + ex);
                    return -1;
                }
            }
            set
            {
                try
                {
                    fStream[currentFile].Position = Position;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while setting Filestream Position property" + ex);
                }
            }
        }

        /// <summary>
        /// Property CanWrite
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                try
                {
                    return fStream[currentFile].CanWrite;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("exception occured while getting Filestream CanWrite property" + ex);
                    return true;
                }
            }
        }

        /// <summary>
        /// Flush filestream
        /// </summary>
        public override void Flush()
        {
            try
            {
                 fStream[currentFile].Flush();
            }
            catch (Exception ex)
            {
                Trace.TraceError("exception occured while Filestream Flush " + ex);
            }
        }

        /// <summary>
        /// Clear filestream
        /// </summary>
        public bool Clear()
        {
            bool success = false;
            try
            {
                if (fStream[currentFile] != null)
                {
                    fStream[currentFile].SetLength(0);
                    this.dataWritten = 0;
                    success = true;
                }
                // If the other non-current file exist, clear the contents in that file as well.. 
                // SetLength will not work if the stream object has been closed (not active)
                if (fStream[(currentFile + 1) % 2] != null)
                {
                    System.IO.File.WriteAllText(fPath[(currentFile + 1) % 2],string.Empty);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while clearing trace log " + ex);
            }
            return success;
        }

        /// <summary>
        /// Filestream seek operation
        /// </summary>
        /// <param name="offset">offset</param>
        /// <param name="origin">start</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            try
            {
                return fStream[currentFile].Seek(offset, origin);
            }
            catch (Exception ex)
            {
                Trace.TraceError("exception occured while Filestream seek operation " + ex);
                return -1;
            }
        }

        /// <summary>
        /// Filestream set length to given value
        /// </summary>
        /// <param name="value">given length value</param>
        public override void SetLength(long value)
        {
            try
            {
               fStream[currentFile].SetLength(value);
            }
            catch (Exception ex)
            {
                Trace.TraceError("exception occured while Filestream set length operation " + ex);
            }
        }

        /// <summary>
        /// Write to filestream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                fStream[currentFile].Write(buffer, offset, count);
                dataWritten += count;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to write to filestream" + ex);
            }
        }

        /// <summary>
        /// Read from filestream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                 return fStream[currentFile].Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to read from filestream" + ex);
                return -1;
            }
        }

        /// <summary>
        /// Close filestream
        /// </summary>
        public override void Close()
        {
            try
            {
                fStream[currentFile].Close();
            }
            catch (Exception ex)
            {
                Tracer.chassisManagerEventLog.WriteEntry("Failed to close trace log. Exception: " + ex);

            }
        }

        /// <summary>
        /// Dispose the filestream
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (fStream != null)
            {
                fStream[currentFile].Dispose();
                fStream = null;
            }

            base.Dispose();
        }

    }

}
