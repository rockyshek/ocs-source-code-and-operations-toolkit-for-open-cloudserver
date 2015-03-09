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
    using System.Linq;
    using Microsoft.GFS.WCS.Contracts;

    internal class getpowerreading: command
    {
        internal getpowerreading()
        {
            this.name = WcsCliConstants.getpowerreading;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getbladepowerreadingHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladePowerReadingResponse myResponse = new BladePowerReadingResponse();
            GetAllBladesPowerReadingResponse myResponses = new GetAllBladesPowerReadingResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesPowerReading();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladePowerReading((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladePowerReadingCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladePowerReadingCollection[index].bladeNumber, null, myResponses.bladePowerReadingCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponses.bladePowerReadingCollection[index].bladeNumber + ": Power Reading: " + myResponses.bladePowerReadingCollection[index].powerReading + " Watts");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponse.bladeNumber + ": Power Reading: " + myResponse.powerReading + " Watts");
                }
            }
        }
    }

    internal class getpowerlimit: command
    {
        internal getpowerlimit()
        {
            this.name = WcsCliConstants.getpowerlimit;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.getbladebpowerlimitHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladePowerLimitResponse myResponse = new BladePowerLimitResponse();
            GetAllBladesPowerLimitResponse myResponses = new GetAllBladesPowerLimitResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.GetAllBladesPowerLimit();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.GetBladePowerLimit((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladePowerLimitCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladePowerLimitCollection[index].bladeNumber, null, myResponses.bladePowerLimitCollection[index], false))
                    {
                        string activeStr = (myResponses.bladePowerLimitCollection[index].isPowerLimitActive)? ", Active." : ", Not active.";
                        Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponses.bladePowerLimitCollection[index].bladeNumber + ": Power Limit: "
                            + myResponses.bladePowerLimitCollection[index].powerLimit + " Watts" + activeStr);
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    string activeStr = (myResponse.isPowerLimitActive) ? ", Active." : ", Not active.";
                    Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponse.bladeNumber + ": Power Limit: " 
                        + myResponse.powerLimit + " Watts" + activeStr);
                }
            }
        }

    }

    internal class setpowerlimit: command
    {
        internal setpowerlimit()
        {
            this.name = WcsCliConstants.setpowerlimit;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('l', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladepowerlimitHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
            this.conditionalOptionalArgs.Add('l', null);
        }

        internal override void commandImplementation()
        {
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            dynamic myLimit = null;
            dynamic mySledId = null;
            this.argVal.TryGetValue('l', out myLimit);

            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesPowerLimit((double)myLimit);
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    this.argVal.TryGetValue('i', out mySledId);
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladePowerLimit((int)mySledId, (double)myLimit);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": Power Limit Set");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponse.bladeNumber + ": Power Limit Set");
                }
            }
        }
    }

    internal class SetBladeActivePowerLimitOn: command
    {
        internal SetBladeActivePowerLimitOn()
        {
            this.name = WcsCliConstants.setBladePowerLimitOn;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladepowerlimitOnHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesPowerLimitOn();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladePowerLimitOn((int)mySledId);
                }
            }
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": Power Limit ON");
                    }
                }
            }
            else
            {
                if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                   Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponse.bladeNumber + ": Power Limit ON");
                }
            }
        }
    }

    internal class SetBladeActivePowerLimitOff: command
    {
        internal SetBladeActivePowerLimitOff()
        {
            this.name = WcsCliConstants.setBladePowerLimitOff;
            this.argSpec.Add('i', Type.GetType("System.UInt32"));
            this.argSpec.Add('a', null);
            this.argSpec.Add('h', null);
            this.helpString = WcsCliConstants.setbladepowerlimitoffHelp;

            this.conditionalOptionalArgs.Add('i', new char[] { 'a' });
        }

        internal override void commandImplementation()
        {
            uint sledId = 1;
            BladeResponse myResponse = new BladeResponse();
            AllBladesResponse myResponses = new AllBladesResponse();
            try
            {
                if (this.argVal.ContainsKey('a'))
                {
                    myResponses = WcsCli2CmConnectionManager.channel.SetAllBladesPowerLimitOff();
                }
                else if (this.argVal.ContainsKey('i'))
                {
                    dynamic mySledId = null;
                    this.argVal.TryGetValue('i', out mySledId);
                    sledId = (uint)mySledId;
                    myResponse = WcsCli2CmConnectionManager.channel.SetBladePowerLimitOff((int)mySledId);
                }
            }          
            catch (Exception ex)
            {
                SharedFunc.ExceptionOutput(ex);
                return;
            }

            if ((this.argVal.ContainsKey('a') && myResponses == null) || myResponse == null)
            {
                Console.WriteLine(WcsCliConstants.serviceResponseEmpty);
                return;
            }

            if (this.argVal.ContainsKey('a'))
            {
                for (int index = 0; index < myResponses.bladeResponseCollection.Count(); index++)
                {
                    if (ResponseValidation.ValidateBladeResponse(myResponses.bladeResponseCollection[index].bladeNumber, null, myResponses.bladeResponseCollection[index], false))
                    {
                        Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponses.bladeResponseCollection[index].bladeNumber + ": Power Limit OFF");
                    }
                }
            }
            else
            {
                 if (ResponseValidation.ValidateBladeResponse(myResponse.bladeNumber, null, myResponse, false))
                {
                    Console.WriteLine(WcsCliConstants.commandSuccess + "Blade " + myResponse.bladeNumber + ": Power Limit OFF");
                }
            }
        }

    }

   
    }
