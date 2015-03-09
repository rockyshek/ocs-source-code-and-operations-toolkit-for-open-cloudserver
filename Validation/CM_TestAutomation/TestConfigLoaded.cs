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

namespace Microsoft.GFS.WCS.Test
{
    using Config = System.Configuration.ConfigurationManager;

    /// <summary>
    /// Configuration to be tested.
    /// </summary>
    internal static class TestConfigLoaded
    {
        /// <summary>
        /// Chassis Manager IP Address
        /// from the Test_App.Config.
        /// </summary>
        internal static readonly string CmipAddress;

        /// <summary>
        /// Chassis Manager Subnet Mask
        /// from the Test_App.Config.
        /// </summary>
        internal static readonly string CMSubnetMask;

        /// <summary>
        /// Chassis Manager Mac Address.
        ///  from the Test_App.Config.
        /// </summary>
        internal static readonly string CMMacAddress;

        /// <summary>
        /// Chassis Manager BiosName.
        /// from the Test_App.Config.
        /// </summary>
        internal static readonly string CMBiosName;

        /// <summary>
        /// Chassis Manager Domain name.
        ///  from the Test_App.Config.
        /// </summary>
        internal static readonly string CMDomain;

        /// <summary>
        /// Chassis Manager url value.
        ///  from the Test_App.Config.
        /// </summary>
        internal static readonly string CM_URL;

        /// <summary>
        /// Chassis Manager blade population count.  Set and retrieved
        /// from the Test_App.Config.
        /// </summary>
        internal static readonly int Population;

        /// <summary>
        /// Chassis number of fans.
        ///  from the Test_App.Config.
        /// </summary>
        internal static readonly int NumFans;

        /// <summary>
        /// Chassis Number of PSUs.
        /// from the Test_App.Config.
        /// </summary>
        internal static readonly int NumPSUs;
        
        /// <summary>
        /// Chassis number of power switches.
        ///  from the Test_App.Config.
        /// </summary>
        internal static readonly int NumPowerSwitches;

        internal static readonly string firmwareVersion;
        internal static readonly string hardwareVersion;

        internal static readonly int ServerDiskCount;
        internal static readonly int JBODDiskCount;
        /// <summary>
        /// Blade Memory Information.
        /// </summary> 
        internal static readonly int TotalDIMMCount;
        internal static readonly int PopulatedDIMMCount;
        internal static readonly string DIMMType;
        internal static readonly string DIMMVoltage;
        internal static readonly int DIMMsize;
        internal static readonly int DIMMspeed;
        
        /// <summary>
        /// Blade PCI Information.
        /// </summary>
        internal static readonly int PCIeCount;
        internal static readonly int PCIedeviceId;
        internal static readonly int PCIesubSystemId;
        internal static readonly int PCIesystemId;
        internal static readonly int PCIevendorId;

        internal static readonly int MellanoxPCIedeviceId;
        internal static readonly int MellanoxPCIesubSystemId;
        internal static readonly int MellanoxPCIesystemId;
        internal static readonly int MellanoxPCIevendorId;
        /// <summary>
        /// Blade Processor Information.
        /// </summary>
        internal static readonly int ProcCount;
        internal static readonly int Procfrequency;
        internal static readonly string ProcType;

        /// <summary>
        /// Initializes static members of the TestConfigLoaded class.
        /// </summary>
        static TestConfigLoaded()
        {
            // check Test_App.config for CM IP address, if not found
            // in the Test_App.config default the value to Zeros string.
            CmipAddress = Config.AppSettings["CMIPAddress"].ToString();
            CmipAddress = CmipAddress == "0.0.0.0" ? "root" : CmipAddress;

            // check Test_App.config for CM Subnet Mask, if not found
            // in the Test_App.config default the value to zeros string.
            CMSubnetMask = Config.AppSettings["CMSubnetMask"].ToString();
            CMSubnetMask = CMSubnetMask == "0.0.0.0" ? "root" : CMSubnetMask;

            // check Test_App.config for CM Mac address, if not found
            // in the app.config default the value to zeros string.
            CMMacAddress = Config.AppSettings["CMMacAddress"].ToString();
            CMMacAddress = CMMacAddress == "0.0.0.0" ? "root" : CMMacAddress;

            // check Test_App.config for CM BIOS name, if not found
            // in the Test_App.config default the value to Not Specified string.
            CMBiosName = Config.AppSettings["CMBiosName"].ToString();
            CMBiosName = CMBiosName == "Not Specified" ? "root" : CMBiosName;

            // check Test_App.config for CM Domain name, if not found
            // in the Test_App.config default the value to Unknown string.
            CMDomain = Config.AppSettings["CMDomain"].ToString();
            CMDomain = CMDomain == "Unknown" ? "root" : CMDomain;

            // check Test_App.config for CM URL value, if not found
            // in the Test_App.config default the value to localhost 8000 string.
            CM_URL = Config.AppSettings["CM_URL"].ToString();
            CM_URL = CM_URL == "http://Localhost:8000" ? "root" : CM_URL;

            // check Test_App.config for Population, if population is not found
            // in the Test_App.config default the value to 24.
            int.TryParse(Config.AppSettings["Population"], out Population);
            Population = Population == 0 ? 24 : Population;

            // check Test_App.config for NumFans, if NumFans is not found
            // in the Test_App.config default the value to 6.
            int.TryParse(Config.AppSettings["NumFans"], out NumFans);
            NumFans = NumFans == 0 ? 6 : NumFans;

            // check Test_App.config for NumPsus, if NumPsus is not found
            // in the Test_App.config default the value to 6.
            int.TryParse(Config.AppSettings["NumPsus"], out NumPSUs);
            NumPSUs = NumPSUs == 0 ? 6 : NumPSUs;

            // check Test_App.config for NumPowerSwitches, if NumPowerSwitches is not found
            // in the Test_App.config default the value to 2.
            int.TryParse(Config.AppSettings["NumPowerSwitches"], out NumPowerSwitches);
            NumPowerSwitches = NumPowerSwitches == 0 ? 3 : NumPowerSwitches;

            //Firmware information
            firmwareVersion = Config.AppSettings["firmwareVersion"].ToString();
            firmwareVersion = firmwareVersion == "Unknown" ? "4.01" : firmwareVersion;

            hardwareVersion = Config.AppSettings["hardwareVersion"].ToString();
            hardwareVersion = hardwareVersion == "Unknown" ? "WCS Mt.Glacier" : hardwareVersion;

            // Server disk information
            int.TryParse(Config.AppSettings["ServerDiskCount"], out ServerDiskCount);
            ServerDiskCount = ServerDiskCount == 0 ? 4 : ServerDiskCount;

            // Server disk information
            int.TryParse(Config.AppSettings["JBODDiskCount"], out JBODDiskCount);
            JBODDiskCount = JBODDiskCount == 0 ? 10 : JBODDiskCount;

            // Blade Memory Information.
            int.TryParse(Config.AppSettings["TotalDIMMCount"], out TotalDIMMCount);
            TotalDIMMCount = TotalDIMMCount == 0 ? 12 : TotalDIMMCount;

            int.TryParse(Config.AppSettings["PopulatedDIMMCount"], out PopulatedDIMMCount);
            PopulatedDIMMCount = PopulatedDIMMCount == 0 ? 12 : PopulatedDIMMCount;

            DIMMType = Config.AppSettings["DIMMType"].ToString();
            DIMMType = DIMMType == "Unknown" ? "DDR3" : DIMMType;
                        
            DIMMVoltage = Config.AppSettings["DIMMVoltage"].ToString();
            DIMMVoltage = DIMMVoltage == "Unknown" ? "DDR3" : DIMMVoltage;

            int.TryParse(Config.AppSettings["DIMMsize"], out DIMMsize);
            DIMMsize = DIMMsize == 0 ? 16384 : DIMMsize;

            int.TryParse(Config.AppSettings["DIMMspeed"], out DIMMspeed);
            DIMMspeed = DIMMspeed == 0 ? 1333 : DIMMspeed;

            // Blade PCI Information.
            int.TryParse(Config.AppSettings["PCIeCount"], out PCIeCount);
            PCIeCount = PCIeCount == 0 ? 1333 : PCIeCount;
              
            int.TryParse(Config.AppSettings["PCIedeviceId"], out PCIedeviceId);
            PCIedeviceId = PCIedeviceId == 0 ? 4099 : PCIedeviceId;

            int.TryParse(Config.AppSettings["PCIesystemId"], out PCIesystemId);
            PCIesystemId = PCIesystemId == 0 ? 5421 : PCIesystemId;

            int.TryParse(Config.AppSettings["PCIesubSystemId"], out PCIesubSystemId);
            PCIesubSystemId = PCIesubSystemId == 0 ? 35221 : PCIesubSystemId;

            int.TryParse(Config.AppSettings["PCIevendorId"], out PCIevendorId);
            PCIevendorId = PCIevendorId == 0 ? 5555 : PCIevendorId;

            //Mellanox PCIe Information
            int.TryParse(Config.AppSettings["MellanoxPCIedeviceId"], out MellanoxPCIedeviceId);
            MellanoxPCIedeviceId = MellanoxPCIedeviceId == 0 ? 5463 : MellanoxPCIedeviceId;

            int.TryParse(Config.AppSettings["MellanoxPCIesystemId"], out MellanoxPCIesystemId);
            MellanoxPCIesystemId = MellanoxPCIesystemId == 0 ? 5421 : MellanoxPCIesystemId;

            int.TryParse(Config.AppSettings["MellanoxPCIesubSystemId"], out MellanoxPCIesubSystemId);
            MellanoxPCIesubSystemId = MellanoxPCIesubSystemId == 0 ? 35221 : MellanoxPCIesubSystemId;

            int.TryParse(Config.AppSettings["MellanoxPCIevendorId"], out MellanoxPCIevendorId);
            MellanoxPCIevendorId = MellanoxPCIevendorId == 0 ? 32902 : MellanoxPCIevendorId;

            // Blade Processor Information.
            int.TryParse(Config.AppSettings["ProcCount"], out ProcCount);
            ProcCount = ProcCount == 0 ? 2 : ProcCount;

            int.TryParse(Config.AppSettings["Procfrequency"], out Procfrequency);
            Procfrequency = Procfrequency == 0 ? 2100 : Procfrequency;

            ProcType = Config.AppSettings["ProcType"].ToString();
            ProcType = ProcType == "Unknown" ? "IntelCorei3" : ProcType;
        }
    }
}
