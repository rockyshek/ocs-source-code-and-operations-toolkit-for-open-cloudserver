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

namespace Microsoft.GFS.WCS.Contracts
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Management;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Shared Funtions
    /// </summary>
    public sealed class SharedFunc
    {
        #region Private Properties

        /// <summary>
        /// Cache list of controller objects so it does not need to be repeatadly retrived
        /// </summary>
        private static List<NetWorkController> controllers = new List<NetWorkController>();

        /// <summary>
        /// Lock objecdt for accessing controllers.
        /// </summary>
        private static object locker = new object();

        /// <summary>
        /// Filter for Location Information registry string
        /// </summary>
        private static readonly string PciFormat = @"(?<bus>(\d,\d,\d))";

        /// <summary>
        /// Filter for PCI Bus Information
        /// </summary>
        private static readonly string PciIndex = @"\d+";

        /// <summary>
        /// PCIe Registry location information.
        /// </summary>
        private static readonly string RegLocalMachineKey = @"SYSTEM\CurrentControlSet\Enum\";

        /// <summary>
        /// Registry String value containing PCIe location information.
        /// </summary>
        private static readonly string RegStringValue = "LocationInformation";

        /// <summary>
        /// Wmi Class Property Names.
        /// </summary>
        private enum WmiPropertyName
        {
            Index,
            MACAddress,
            PNPDeviceID
        }

        /// <summary>
        /// WMI Physical Network Adapter Query.
        /// </summary>
        private static readonly string PhysicalAdapterQuery = "select * from Win32_NetworkAdapter where PhysicalAdapter = True";

        #endregion

        #region Private Functions

        /// <summary>
        /// This Network Controller Class contains physical location information
        /// about the network controller.  
        /// 
        /// The class object assigns a physical index to the network controller,
        /// in additoin to the windows assigned index (logical).
        /// 
        /// In Windows, the index assigned to the network controller does 
        /// not correspond to the physical port mapping on the baseboard.  DSM
        /// Function 7 can provide the intended index, but unless the device
        /// driver uses the DSM function, the network controllers will be
        /// installed and indexed in the order they are received by Windows.
        /// 
        /// The solution is to enumerate the PCI bus information, the Bus
        /// information can be obtained from the SetupDiGetDeviceRegistryProperty
        /// API or Microsoft.Win32 Registry classes.
        /// </summary>
        private class NetWorkController : IComparable<NetWorkController>
        {
            /// <summary>
            /// Network Controller Index assigned by Windows
            /// </summary>
            public uint Index { get; set; }

            /// <summary>
            /// Network Controller Windows PnpDeviceId
            /// </summary>
            public string PnpDeviceId { get; set; }

            /// <summary>
            /// Network Controller MAC address
            /// </summary>
            public string MacAddress { get; set; }

            /// <summary>
            /// Network Controller PCI Index
            /// </summary>
            public int PciBus { get; private set; }

            /// <summary>
            /// Network Controller PCI Device
            /// </summary>
            public int PciDevice { get; private set; }

            /// <summary>
            /// Network Controller PCI Function
            /// </summary>
            public int PciFunction { get; private set; }

            /// <summary>
            /// Network Controller Physical Index
            /// </summary>
            public int PhysicalIndex { get; set; }

            /// <summary>
            /// Valid Network Controller
            /// </summary>
            public bool ValidNetworkCtrl { get; set; }

            /// <summary>
            /// Class Constructor
            /// </summary>
            public NetWorkController(bool valid)
            {
                this.ValidNetworkCtrl = valid;
            }

            /// <summary>
            /// Implement IComparable.CompareTo(b)
            /// </summary>
            public int CompareTo(NetWorkController b)
            {
                if (this.PciBus.CompareTo(b.PciBus) == 0)
                {
                    if (this.PciDevice.CompareTo(b.PciDevice) == 0)
                        return this.PciFunction.CompareTo(b.PciFunction);
                    else
                        return this.PciDevice.CompareTo(b.PciDevice);
                }
                else
                    return this.PciBus.CompareTo(b.PciBus);
            }

            /// <summary>
            ///  Sets the Network Controller PCIe Bus Values.
            /// </summary>
            internal void SetPciInfo(string bus, string device, string function)
            {
                int value;
                if (!string.IsNullOrEmpty(bus) && !string.IsNullOrEmpty(device) && !string.IsNullOrEmpty(function))
                {
                    if (!int.TryParse(bus, out value))
                        ValidNetworkCtrl = false;
                    else
                        this.PciBus = value;

                    if (!int.TryParse(device, out value))
                        ValidNetworkCtrl = false;
                    else
                        this.PciDevice = value;

                    if (!int.TryParse(function, out value))
                        ValidNetworkCtrl = false;
                    else
                        this.PciFunction = value;
                }
            }

        }

        /// <summary>
        /// Extracts PCIe Bus information from the registry, based on the NIC
        /// PNPDeviceID.
        /// </summary>
        private static void GetPciLocationInfo(string pnpDeviceId, ref NetWorkController nic)
        {
            Regex pciFormat = new Regex(PciFormat);
            Regex pciIndex = new Regex(PciIndex);
            string key = RegLocalMachineKey + pnpDeviceId;
            string location = string.Empty;

            try
            {
                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(key, RegistryKeyPermissionCheck.ReadSubTree,
                    System.Security.AccessControl.RegistryRights.ReadKey);
                if (null != regkey)
                {
                    location = (string)regkey.GetValue(RegStringValue);
                }
            }
            catch (Exception)
            {
                nic.ValidNetworkCtrl = false;
            }

            if (!string.IsNullOrEmpty(location))
            {
                Match pciInfo = pciFormat.Match(location);

                if (pciInfo.Success)
                {
                    MatchCollection pciLocation = pciIndex.Matches(pciInfo.Groups["bus"].Value.ToString());

                    if (pciLocation.Count == 3)
                        nic.SetPciInfo(pciLocation[0].Value, pciLocation[1].Value, pciLocation[2].Value);
                    else
                        nic.ValidNetworkCtrl = false;
                }
                else
                {
                    nic.ValidNetworkCtrl = false;
                }
            }
            else
            {
                nic.ValidNetworkCtrl = false;
            }
        }

        /// <summary>
        /// Get Network Controller PCIe location
        /// </summary>
        private static List<NetWorkController> GetNetworkControllers()
        {
            ManagementObjectSearcher objadapter = new ManagementObjectSearcher(PhysicalAdapterQuery);
            ManagementObjectCollection adapters = objadapter.Get();

            if (adapters != null)
            {
                // enumerate management controllers with valid properties
                foreach (ManagementBaseObject adapterInstance in adapters)
                {
                    NetWorkController nic = new NetWorkController(true);

                    if (adapterInstance[WmiPropertyName.Index.ToString()] != null)
                    {
                        nic.Index = (uint)adapterInstance[WmiPropertyName.Index.ToString()];
                    }
                    else
                    {
                        nic.ValidNetworkCtrl = false;
                    }

                    if (adapterInstance[WmiPropertyName.MACAddress.ToString()] != null)
                    {
                        nic.MacAddress = (string)adapterInstance[WmiPropertyName.MACAddress.ToString()];
                    }
                    else
                    {
                        nic.ValidNetworkCtrl = false;
                    }

                    if (adapterInstance[WmiPropertyName.PNPDeviceID.ToString()] != null)
                    {
                        GetPciLocationInfo((string)adapterInstance[WmiPropertyName.PNPDeviceID.ToString()], ref nic);
                    }
                    else
                    {
                        nic.ValidNetworkCtrl = false;
                    }

                    if (nic.ValidNetworkCtrl)
                    {
                        controllers.Add(nic);
                    }
                }
            }

            if (objadapter != null)
            {
                objadapter.Dispose();
            }

            if (adapters != null)
            {
                adapters.Dispose();
            }

            // Quick sort controllers by pcie bus, then deviceId
            controllers.Sort();

            // Update physical Nic index based on Pcie bus and deviceId 
            for (int i = 0; i < controllers.Count; i++)
            {
                controllers[i].PhysicalIndex = i;
            }

            return controllers;
        }

        #endregion

        /// <summary>
        /// Private constructor to prevent the compiler from automatically creating a public one.
        /// </summary>
        private SharedFunc() { }

        /// <summary>
        /// Populates network controllers.  Should be called by Wcscli when operating as a service
        /// </summary>
        public static void EnumerateControllers()
        {
            lock (locker)
            {
                GetNetworkControllers();
            }
        }

        /// <summary>
        /// Given the physical index of a controller, the function returns
        /// the Windows Logical Index mapping.
        /// </summary>
        public static int NetworkCtrlLogicalIndex(int physicalIndex)
        {
            foreach (NetWorkController controller in controllers)
            {
                if (controller.PhysicalIndex == physicalIndex)
                    return (int)controller.Index;
            }

            return -1;
        }

        /// <summary>
        /// Given the logical Windows index of a controller, the function returns
        /// the Physical Index mapping.
        /// </summary>
        public static int NetworkCtrlPhysicalIndex(int logicalIndex)
        {
            foreach (NetWorkController controller in controllers)
            {
                if (controller.Index == logicalIndex)
                    return (int)controller.PhysicalIndex;
            }

            return -1;
        }

    }

}

