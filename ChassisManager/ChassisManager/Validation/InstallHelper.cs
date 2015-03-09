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
    using System.Linq;
    using System.ServiceProcess;

    /// <summary>
    /// Install helper for gathering FRU.
    /// </summary>
    public static class InstallHelper
    {
        // initialization flag
        private static volatile bool initialized = false;

        // lock object for thread safey.
        private static object locker = new object();

        private static readonly string ServiceName = "ChassisManager";

        /// <summary>
        /// Chassis Manager initialize function
        /// </summary>
        public static void Initialize()
        {
            Tracer.WriteInfo("Chassis Manager InstallHelper Initialization started");
            byte status;

            if (ServiceNotRunning())
            {
                lock (locker)
                {
                    status = ChassisManagerInternal.Initialize();
                    initialized = true;
                }
                if (status != (byte)CompletionCode.Success)
                {
                    Tracer.WriteError("Chassis manager InstallHelper failed to initialize at {0}", DateTime.Now);
                    Dispose();
                }
            }
            else
            {
                throw new Exception("Cannot Initialize, Chassis Manager service is not stopped");
            }
        }

        /// <summary>
        /// Checks the Chassis Manager Service is not already runing
        /// </summary>
        private static bool ServiceNotRunning()
        {
            if (ChassisManagerExist())
            {
                try
                {
                    ServiceController sc = new ServiceController(ServiceName);
                    if (sc.Status == ServiceControllerStatus.Stopped)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    Tracer.WriteError("Chassis Manager InstallHelper ServiceNotRunning failed to get service state: {0}", 
                        ex.ToString());
                    return false;
                }
            }
            else
            {
                return true;
            }

        }

        /// <summary>
        /// Checks if the Chassis Manager service is installed.
        /// </summary>
        private static bool ChassisManagerExist()
        {
            ServiceController[] w32services = ServiceController.GetServices();
            ServiceController cm = w32services.FirstOrDefault(srv => srv.ServiceName == ServiceName);
            return cm != null;
        }

        /// <summary>
        /// Get the Chassis Manager or PDB FRU areas information. 
        /// </summary>
        private static Ipmi.FruDevice GetFruDevice(DeviceType deviceType)
        {
            Ipmi.FruDevice fru = new Ipmi.FruDevice((byte)CompletionCode.UnspecifiedError);

            lock (locker)
            {
                try
                {
                    if (initialized)
                        fru = ChassisState.CmFruData.ReadFru((DeviceType)deviceType);
                    else
                        Tracer.WriteError(
                                " GetFruDevice failed: InstallHelper Uninitialized");
                }
                catch (Exception ex)
                {
                    Tracer.WriteError(
                        " GetFruDevice failed with the exception: " + ex.Message);
                }
            }

            return fru;
        }

        /// <summary>
        /// Get PDB FRU EEPROM Contents
        /// </summary>
        public static Ipmi.FruDevice GetChassisManagerFru()
        {
            return GetFruDevice(DeviceType.ChassisFruEeprom);
        }

        /// <summary>
        /// Get PDB FRU EEPROM Contents
        /// </summary>
        public static Ipmi.FruDevice GetPowerDistributionFru()
        {
            return GetFruDevice(DeviceType.PdbFruEeprom);
        }

        /// <summary>
        /// Release Resources.
        /// </summary>
        public static void Dispose()
        {
            lock (locker)
            {
                // stop internal Chassis threads
                ChassisManagerInternal.Halt();

                // release communication layer resources.
                CommunicationDevice.Release();

                initialized = false;
            }
        }

    }
}
