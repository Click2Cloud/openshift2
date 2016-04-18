#region Copyright ©2016, Click2Cloud Inc. - All Rights Reserved
/* ------------------------------------------------------------------- *
*                            Click2Cloud Inc.                          *
*                  Copyright ©2016 - All Rights reserved               *
*                                                                      *
*                                                                      *
*  Copyright © 2016 by Click2Cloud Inc. | www.click2cloud.net          *
*  All rights reserved. No part of this publication may be reproduced, *
*  stored in a retrieval system or transmitted, in any form or by any  *
*  means, photocopying, recording or otherwise, without prior written  *
*  consent of Click2cloud Inc.                                         *
*                                                                      *
*                                                                      *
* -------------------------------------------------------------------  */
#endregion Copyright ©2016, Click2Cloud Inc. - All Rights Reserved

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxWebClient
{
    class RegistryHelper
    {
        #region Private Variables

        private static string _registryName = "Software\\Wow6432Node\\Aegis Group\\FoxWeb\\CurrentVersion\\VirtualRoots";

        private static int _ipStartRange = 100;
        private static int _ipEndRange = 255;
        private static string _ipAddressFormat = "127.0.0.{0}";

        private static string _regAddTemplate = "Windows Registry Editor Version 5.00"
                            + Environment.NewLine
                            + "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Aegis Group\\FoxWeb\\CurrentVersion\\VirtualRoots]"
                            + Environment.NewLine
                            + "\"{0}/{1}\"=\"-\"";

        private static string _regRemoveTemplate = "Windows Registry Editor Version 5.00"
                            + Environment.NewLine
                            + "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Aegis Group\\FoxWeb\\CurrentVersion\\VirtualRoots]"
                            + Environment.NewLine
                            + "\"{0}/{1}\"=-";

        private string _applicationDNS = string.Empty;
        private string _ipAddress = string.Empty;

        #endregion

        #region Member Constructor

        internal RegistryHelper(string applicationDNS)
        {
            this._applicationDNS = applicationDNS;
        }

        #endregion

        #region Private Methods

        private string GetNewIPAddress()
        {
            int selectedIPAddress = _ipStartRange;
            dynamic usedIPAddresses = null;

            RegistryKey key = Registry.LocalMachine.OpenSubKey(_registryName);
            if (key != null)
            {
                string[] subkeys = key.GetValueNames();
                usedIPAddresses = new int[subkeys.Length];

                if (subkeys != null)
                    for (int i = 0; i <= subkeys.Length - 1; i++)
                    {
                        int ipadd = 0;
                        int.TryParse(subkeys[i].Split('.')[subkeys[i].Split('.').Length - 1], out ipadd);
                        if (ipadd != 0 && ipadd >= _ipStartRange && ipadd <= _ipEndRange)
                            usedIPAddresses[i] = ipadd;
                    }
            }

            if (usedIPAddresses == null)
                return string.Format(_ipAddressFormat, selectedIPAddress);

            else if (usedIPAddresses.Length == 0 || ((int[])usedIPAddresses).Where(vm => vm == 0).Count() == usedIPAddresses.Length)
                return string.Format(_ipAddressFormat, selectedIPAddress);

            if (usedIPAddresses.Length != null)
            {
                //Sort Used IP address array
                Array.Sort(usedIPAddresses);

                //Remove all 0 elements
                usedIPAddresses = ((int[])usedIPAddresses).Where(vm => vm != 0).ToArray();

                //Get start element if it is some values missing use first element
                if (usedIPAddresses[0] > _ipStartRange)
                {
                    selectedIPAddress = usedIPAddresses[0] - 1;
                    return string.Format(_ipAddressFormat, selectedIPAddress);
                }

                for (int i = 0; i <= usedIPAddresses.Length - 1; i++)
                {
                    if (((i + 1) == usedIPAddresses.Length) || (usedIPAddresses[i] + 1 != usedIPAddresses[i + 1]))
                    {
                        selectedIPAddress = usedIPAddresses[i] + 1;
                        return string.Format(_ipAddressFormat, selectedIPAddress);
                    }
                }
            }

            //TODO: Check this value
            return string.Format(_ipAddressFormat, selectedIPAddress);
        }

        private string GetIPAddressByAppName()
        {
            int selectedIPAddress = 0;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(_registryName);
            if (key != null)
            {
                string[] subkeys = key.GetValueNames();

                if (subkeys != null)
                    for (int i = 0; i <= subkeys.Length - 1; i++)
                    {
                        if (subkeys[i].ToUpper().Contains(this._applicationDNS.ToUpper()))
                        {
                            int.TryParse(subkeys[i].Split('.')[subkeys[i].Split('.').Length - 1], out selectedIPAddress);
                            if (selectedIPAddress != 0)
                                return string.Format(_ipAddressFormat, selectedIPAddress);
                        }
                    }
            }

            return string.Empty;
        }

        #endregion

        #region Internal Methods

        internal void CreateRegEntry()
        {
            this._ipAddress = GetNewIPAddress();

            //Create Reg file
            string tempRegFileName = "TempRegEdit_Add.reg";
            File.WriteAllText(tempRegFileName, string.Format(_regAddTemplate, this._applicationDNS, this._ipAddress));

            //Execute Reg File
            Process regeditProcess = Process.Start("regedit.exe", "/s " + tempRegFileName);
            regeditProcess.WaitForExit();

            //Delete file
            File.Delete(tempRegFileName);
        }

        internal void RemoveRegEntry()
        {
            this._ipAddress = GetIPAddressByAppName();

            //Create Reg file
            string tempRegFileName = "TempRegEdit_Remove.reg";
            File.WriteAllText(tempRegFileName, string.Format(_regRemoveTemplate, this._applicationDNS, this._ipAddress));

            //Execute Reg File
            Process regeditProcess = Process.Start("regedit.exe", "/s " + tempRegFileName);
            regeditProcess.WaitForExit();

            //Delete file
            File.Delete(tempRegFileName);
        }

        
        #endregion


    }
}
