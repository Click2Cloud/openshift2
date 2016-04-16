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

using Microsoft.Web.Administration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FoxWebClient
{
    class Program
    {
        private static ServerManager serverMgr = new ServerManager();
        private static Site site = null;

        internal static string logFile = string.Empty;

        static void Main(string[] args)
        {
            try
            {
                logFile = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_PRIMARY_CARTRIDGE_DIR"), "log", "foxwebclientlog.txt");

                string strAction = args[0].Replace("'", string.Empty).ToUpper();

                switch (strAction)
                {
                    case "CREATE":
                        {
                            //Create Application and Application Pool in IIS with Prison user account Identity
                            CreateApplication();

                            //Add Registry Entry to add new Virtual Server in FoxWeb Server
                            CreateRegistryEntry();

                            //Restart Fox Web Service to reflect changes
                            RestartFoxWeb();
                        }
                        break;
                    case "DELETE":
                        {
                            //Delete Application and Application Pool from IIS
                            DeleteApplication();

                            //Removing Registry Entry 
                            RemoveRegistryEntry();

                            //Restart Fox Web Service to reflect changes
                            RestartFoxWeb();
                        }
                        break;
                    case "STATUS":
                        {
                            string strStatus = string.Empty;
                            if (ApplicationStatus())
                                strStatus = "RUNNING";
                            else
                                strStatus = "NOT-RUNNING";
                            Console.WriteLine(strStatus);

                            LogEvent(EventType.DEBUG, "Application is " + strStatus);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogEvent(EventType.ERROR, "Error occured in FoxWeb Client" + Environment.NewLine
                    + "Error = " + ex.Message + Environment.NewLine
                    + "StackTrace = " + ex.StackTrace);

                Console.WriteLine("Error Occured in FoxWeb Client." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private static void CreateApplication()
        {
            if (ApplicationExist())
            {
                LogEvent(EventType.DEBUG, "Application already exist in IIS.");
                Console.WriteLine("Application already exist in IIS.");
                return;
            }

            LogEvent(EventType.DEBUG, "Creating Application in IIS");

            string userName = Environment.GetEnvironmentVariable("USERNAME");
            string password = ReadPassword();
            string applicationName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");
            string virtualPath = Environment.GetEnvironmentVariable("OPENSHIFT_REPO_DIR");
            int appPort = int.Parse(Environment.GetEnvironmentVariable("OPENSHIFT_FOXWEB_PORT"));

            //Add Site
            site = serverMgr.Sites.Add(applicationName, virtualPath, appPort);

            //Add Application Pool
            serverMgr.ApplicationPools.Add(applicationName);

            ApplicationPool appPool = serverMgr.ApplicationPools[applicationName];

            //Assign identity to a custom user account
            appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
            appPool.ProcessModel.UserName = userName;
            appPool.ProcessModel.Password = password;
            appPool.ProcessModel.LoadUserProfile = true;

            serverMgr.ApplicationPools[applicationName].Enable32BitAppOnWin64 = true;
            site.ApplicationDefaults.ApplicationPoolName = applicationName;

            //Commit Changes to IIS
            serverMgr.CommitChanges();

            LogEvent(EventType.DEBUG, "Application and Application Pool created successfully in IIS");
        }

        private static string ReadPassword()
        {
            string userVarsDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".prison");
            string fileName = Path.Combine(userVarsDir, "PRISON_PASSWORD");
            return File.ReadAllText(fileName);
        }

        private static void DeleteApplication()
        {
            if (!ApplicationExist())
            {
                Console.WriteLine("Application is not available in IIS.");
                LogEvent(EventType.DEBUG, "Application is not available in IIS.");
                return;
            }

            string applicationName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");

            //remove Site
            site = serverMgr.Sites[applicationName];
            if (site != null)
            {
                LogEvent(EventType.DEBUG, "Deleting Site from IIS.");
                serverMgr.Sites.Remove(site);
            }

            //removed Application Pool
            ApplicationPool appPool = serverMgr.ApplicationPools[applicationName];
            if (appPool != null)
            {
                LogEvent(EventType.DEBUG, "Removing Application Pool from IIS.");
                serverMgr.ApplicationPools.Remove(appPool);
            }

            //Commit Changes to IIS
            serverMgr.CommitChanges();
            LogEvent(EventType.DEBUG, "Application and Application Pool Deleted successfully from IIS.");
        }

        private static bool ApplicationExist()
        {
            return serverMgr.Sites[Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS")] != null;
        }

        private static bool ApplicationStatus()
        {
            LogEvent(EventType.DEBUG, "Getting application status");

            site = serverMgr.Sites[Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS")];
            if (site == null)
                return false;
            if (site.State == ObjectState.Started)
                return true;
            return false;
        }


        private static void CreateRegistryEntry()
        {
            LogEvent(EventType.DEBUG, "Adding Registry Enttry");

            RegistryHelper regHelper = new RegistryHelper(Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS"));
            regHelper.CreateRegEntry();

            LogEvent(EventType.DEBUG, "Registry Entry added successfully.");
        }

        private static void RemoveRegistryEntry()
        {
            LogEvent(EventType.DEBUG, "Removing Registry Enttry");

            RegistryHelper regHelper = new RegistryHelper(Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS"));
            regHelper.RemoveRegEntry();

            LogEvent(EventType.DEBUG, "Registry Entry removed successfully.");
        }

        internal static void RestartFoxWeb()
        {
            LogEvent(EventType.DEBUG, "Restarting Foxweb Service");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.Start();

            using (StreamWriter sw = proc.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(@"net stop FoxWeb");
                    sw.WriteLine(@"net start FoxWeb");
                }
            }

            string result = proc.StandardOutput.ReadToEnd();
            while (!proc.StandardOutput.EndOfStream)
                result += proc.StandardOutput.ReadLine();

            string error = string.Empty;
            while (!proc.StandardOutput.EndOfStream)
                error += proc.StandardError.ReadLine();

            proc.Close();

            if (string.IsNullOrEmpty(error))
                LogEvent(EventType.DEBUG, "Foxweb service restrated successfully");
            else
                LogEvent(EventType.ERROR, "Error occured while restarting foxweb service"
                    + Environment.NewLine + "Result = " + result
                    + Environment.NewLine + "Error = " + error);
        }

        #region Log Event

        private enum EventType
        {
            DEBUG,
            ERROR,
            INFO
        }

        private static void LogEvent(EventType eventType, string message)
        {
            File.AppendAllText(logFile, string.Format("{0}|{1}|{2}|{3}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"),
                eventType.ToString(), "openshift.winnode.foxweb.foxwebclient", message) + Environment.NewLine);
        }

        #endregion

    }
}
