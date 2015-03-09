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
    using System.Collections.Generic;
    using Microsoft.GFS.WCS.Contracts;
    using System.Threading;
    using System.Linq;
    using System.Text;

    internal static class BladePowerCommands
    {
        /// <summary>
        /// BladeOff commands switches off blade through IPMI (soft blade off)
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns></returns>
        internal static bool BladeOff(int bladeId)
        {
            bool powerOffStatus = false;

            // Soft power enable
            byte softStatus = WcsBladeFacade.SetPowerState((byte)bladeId, Ipmi.IpmiPowerState.Off);
            Tracer.WriteInfo("Soft poweroff status " + softStatus);

            if (softStatus != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("Blade Soft Power Off Failed with Completion Code {0:X}", softStatus);
            }
            else
            {
                powerOffStatus = true;
            }
            return powerOffStatus;
        }

        /// <summary>
        /// Internal method to power cycle specified blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <param name="offTime">time for which the blades will be powered off in seconds</param>
        /// <returns>true/false indicating if blade operation was success/failure</returns>
        internal static bool PowerCycle(int bladeId, uint offTime)
        {
            Tracer.WriteInfo("Received PowerCycle({0},{1})", bladeId, offTime);
            bool powerStatus = false;

            bool intervalStatus = WcsBladeFacade.SetPowerCycleInterval((byte)bladeId, (byte)offTime);
            if (intervalStatus != true)
            {
                Tracer.WriteWarning("Blade PowerCycle Interval setting failed with Completion code {0:X}", intervalStatus);
                return powerStatus;
            }

            byte status = WcsBladeFacade.SetPowerState((byte)bladeId, Ipmi.IpmiPowerState.Cycle);
            Tracer.WriteInfo("PowerCycle: SetPowerState Return: {0}", status);

            // We want the blade to always power on when it receives a Power Cycle command.
            // Some BMC implementations may not turn on the blade for Power Cycle if the blade
            // is in the OFF state, and will return 0xD5 (Request parameter(s) not supported
            // in present state) as recommended in the IPMI standard.
            // Check for 0xD5 and manually turn on the blade.
            if (status == 0xD5)
            {
                // Check that the blade is actually off
                Ipmi.SystemStatus powerState = WcsBladeFacade.GetChassisState((byte)bladeId);

                Tracer.WriteInfo("PowerCycle: GetChassisState Return: {0}, Blade State: {1}", powerState.CompletionCode, powerState.PowerState.ToString());

                if (powerState.CompletionCode != 0)
                {
                    Tracer.WriteError("PowerCycle: GetChassisState Failed with Completion code {0:X}", powerState.CompletionCode);
                }
                else
                {
                    if (powerState.PowerState == Ipmi.IpmiPowerState.Off)
                    {
                        // Set blade on
                        if (BladeOn(bladeId))
                        {
                            Tracer.WriteInfo("PowerCycle: {0} SetBladeOn(): Blade soft power set to ON", bladeId);
                            powerStatus = true;
                        }
                        else
                        {
                            Tracer.WriteError("PowerCycle: {0} SetBladeOn(): Failed to set Blade soft power ON", bladeId);
                        }
                    }
                    else
                    {
                        // The blade is ON but the BMC returns 0xD5. Return error.
                        Tracer.WriteError("SetPowerState returned 0xD5 but blade is not Off. Blade state: {0}", powerState.PowerState);
                    }
                }
            }          
            else if (status != 0)
            {
                Tracer.WriteWarning("Blade PowerCycle Failed with Completion code {0:X}", status);
            }
            else
            {
                powerStatus = true;
            }

            return powerStatus;
        }

        /// <summary>
        /// bladeOn - turns the blade on through IPMI, once BMC is powered on (Soft power on)
        /// </summary>
        /// <param name="bladeId"></param>
        /// <returns></returns>
        internal static bool BladeOn(int bladeId)
        {
            bool powerOnStatus = false;

            // Soft power enable
            byte softStatus = WcsBladeFacade.SetPowerState((byte)bladeId, Ipmi.IpmiPowerState.On);
            Tracer.WriteInfo("Soft poweron status " + softStatus);

            if (softStatus != (byte)CompletionCode.Success)
            {
                Tracer.WriteWarning("Blade Soft Power On Failed with Completion Code {0:X}", softStatus);
            }
            else
            {
                powerOnStatus = true;
            }
            return powerOnStatus;
        }

        /// <summary>
        /// Internal operation to call both hard power on (soft power on is not exposed to the user)
        /// </summary>
        /// <param name="bladeId">Blade ID</param>
        /// <returns>True/false for success/failure</returns>
        internal static bool PowerOn(int bladeId)
        {
            Tracer.WriteInfo("Received poweron({0})", bladeId);
            bool powerOnStatus = false;

            BladePowerStatePacket bladePowerSwitchStatePacket = new BladePowerStatePacket();
            CompletionCode status;

            // Hard Power enable
            // Serialize setting of state and actual code logic 
            lock (ChassisState.locker[bladeId - 1])
            {
                BladePowerStatePacket currState = ChassisState.BladePower[bladeId - 1].GetBladePowerState();

                if (currState.CompletionCode != CompletionCode.Success
                    || (currState.BladePowerState == (byte)Contracts.PowerState.OFF))
                {
                    // No return here, because we still want to return a BMC state on the fall through,
                    // if Blade enable read fails for whatever reason
                    Tracer.WriteWarning("PowerOn: Blade {0} Power Enable state read failed (Completion Code: {1:X})", bladeId, currState.CompletionCode);

                    bladePowerSwitchStatePacket = ChassisState.BladePower[bladeId - 1].SetBladePowerState((byte)PowerState.ON);
                    status = bladePowerSwitchStatePacket.CompletionCode;
                    Tracer.WriteInfo("Hard poweron status " + status);

                    if (status == CompletionCode.Success)
                    {
                        // Hard power on status is true, so Blade should be set to Initialization state on success
                        Tracer.WriteInfo("PowerOn: State Transition for blade {0}: {1} -> Initialization", bladeId,
                            ChassisState.GetStateName((byte)bladeId));

                        ChassisState.SetBladeState((byte)bladeId, (byte)BladeState.Initialization);
                        powerOnStatus = true;
                    }
                    else
                    {
                        Tracer.WriteWarning("PowerOn: Hard Power On failed for BladeId {0} with code {1:X}", bladeId, status);
                    }
                }
                else
                {
                    powerOnStatus = true; // the blade was already powered on, so we dont power it on again
                }
            }
            return powerOnStatus;
        }

        /// <summary>
        /// Internal method to Power off blade
        /// </summary>
        /// <param name="bladeId">Blade ID(1-48)</param>
        /// <returns>true/false if operation was success/failure</returns>
        internal static bool PowerOff(int bladeId)
        {
            Tracer.WriteInfo("Received poweroff({0})", bladeId);
            bool powerOffStatus = false;

            BladePowerStatePacket bladePowerSwitchStatePacket = new BladePowerStatePacket();

            // Serialize power off and power on, on the same lock variable per blade, so we prevent inconsistent power state behavior
            lock (ChassisState.locker[bladeId - 1])
            {
                bladePowerSwitchStatePacket = ChassisState.BladePower[bladeId - 1].SetBladePowerState((byte)PowerState.OFF);
                CompletionCode status = bladePowerSwitchStatePacket.CompletionCode;

                // Sleep for specified amount of time after blade hard power off to prevent hardware inconsistent state 
                // - hot-swap controller not completely draining its capacitance leading to inconsistent power state issues
                Thread.Sleep(ConfigLoaded.WaitTimeAfterBladeHardPowerOffInMsecs);

                Tracer.WriteInfo("PowerOff: Return: {0}", status);

                if (status != CompletionCode.Success)
                {
                    Tracer.WriteError("PowerOff: Blade Hard Power Off Failed with Completion code {0:X}", status);
                    powerOffStatus = false;
                }
                else
                {
                    powerOffStatus = true;
                    // set state to Hard Power Off
                    Tracer.WriteInfo("PowerOff: State Transition for blade {0}: {1} -> HardPowerOff", bladeId,
                        ChassisState.GetStateName((byte)bladeId));

                    ChassisState.SetBladeState((byte)bladeId, (byte)BladeState.HardPowerOff);
                    ChassisState.PowerFailCount[bladeId - 1] = 0;
                    // Clear blade type and cache
                    ChassisState.BladeTypeCache[bladeId - 1] = (byte)BladeType.Unknown;
                    WcsBladeFacade.ClearBladeClassification((byte)bladeId);
                }
            }
            return powerOffStatus;
        }

    }
}
