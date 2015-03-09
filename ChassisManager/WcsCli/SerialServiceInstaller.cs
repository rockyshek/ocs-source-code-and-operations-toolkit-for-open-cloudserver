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
    using System.ComponentModel;
    using System.Configuration;
    using System.Configuration.Install;
    using System.Reflection;
    using System.ServiceProcess;

    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class WcscliSerialServiceInstaller: Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        /// <summary>
        /// Intaller method.. Service name should match with that provided in the service class
        /// </summary>
        public WcscliSerialServiceInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            service = new ServiceInstaller();
            //service.ServiceName = "WcscliSerialService";
            service.ServiceName = GetConfigurationValue("ServiceName") + GetConfigurationValue("COMPortName");
            this.service.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            Installers.Add(process);
            Installers.Add(service);
        }

        /// <summary>
        /// This function gets the service name parameter from the app.config file
        /// The code takes care of finding the correct path (assemnbly path) of the app.config at installation time
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetConfigurationValue(string key)
        {
            Assembly service = Assembly.GetAssembly(typeof(WcscliSerialServiceInstaller));
            Configuration config = ConfigurationManager.OpenExeConfiguration(service.Location);
            if (config.AppSettings.Settings[key] != null)
            {
                return config.AppSettings.Settings[key].Value;
            }
            else
            {
                throw new IndexOutOfRangeException 
                    ("We do not have the queried key: " + key);
            }
        }
    }
}
