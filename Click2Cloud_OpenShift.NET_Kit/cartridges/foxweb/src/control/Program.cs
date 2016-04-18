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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections;

namespace Control
{
    class Program
    {
        static string domain = ConfigurationManager.AppSettings["AdminDomain"];
        static string admin = ConfigurationManager.AppSettings["AdminUserName"];
        static string password = ConfigurationManager.AppSettings["AdminPassword"];

        static string logFile = string.Empty;

        static int Main(string[] args)
        {
            logFile = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_PRIMARY_CARTRIDGE_DIR"), "log", "controllog.txt");

            try
            {
                switch (args[0])
                {
                    case "start":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge START Event");
                            StartCartridge();
                            break;
                        }
                    case "stop":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge STOP Event");
                            StopCartridge();
                            break;
                        }
                    case "status":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge STATUS Event");
                            CartridgeStatus();
                            break;
                        }
                    case "reload":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge RELOAD Event");
                            ReloadCartridge();
                            break;
                        }
                    case "restart":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge RESTART Event");
                            RestartCartridge();
                            break;
                        }
                    case "build":
                        {
                            LogEvent(EventType.DEBUG, "Cartridge BUILD Event");
                            Build();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch(Exception ex)
            {
                LogEvent(EventType.ERROR, "Error occured in Control" + Environment.NewLine
                    + "Error = " + ex.Message + Environment.NewLine
                    + "StackTrace = " + ex.StackTrace);
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }

        private static void StartCartridge()
        {
            if (ApplicationStatus())
            {
                LogEvent(EventType.DEBUG, "Cartridge already running");
                Console.WriteLine("Cartridge already running");
                return;
            }

            LogEvent(EventType.DEBUG, "Starting the FoxWeb cartridge");
            Console.WriteLine("Starting the FoxWeb cartridge");

            CreateApplication();
        }

        private static void StopCartridge()
        {
            if (!ApplicationStatus())
            {
                LogEvent(EventType.DEBUG, "Cartridge not running");
                Console.WriteLine("Cartridge not running");
                return;
            }

            LogEvent(EventType.DEBUG, "Stoping the FoxWeb cartridge");
            Console.WriteLine("Stoping the FoxWeb cartridge");

            DeleteApplication();
        }

        private static void CartridgeStatus()
        {
            string applicationName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");
            Console.WriteLine("Retrieving cartridge");

            if (ApplicationStatus())
                ClientResult("Application is running");
            else
                ClientResult("Application is either stopped or inaccessible");

        }

        private static void ReloadCartridge()
        {
            Console.WriteLine("Reloading cartridge");
            RestartCartridge();
        }

        private static void RestartCartridge()
        {
            Console.WriteLine("Restarting cartridge");
            StopCartridge();
            StartCartridge();
        }

        private static void Build()
        {            
            foreach (string sln in Directory.GetFiles(Environment.GetEnvironmentVariable("OPENSHIFT_REPO_DIR"), "*.sln"))
            {
                Process p = new Process();
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";
                p.StartInfo.Arguments = string.Format("{0} /t:Rebuild", sln);
                p.StartInfo.WorkingDirectory = Environment.GetEnvironmentVariable("OPENSHIFT_REPO_DIR");
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += new DataReceivedEventHandler(delegate(object sender, DataReceivedEventArgs args) { Console.WriteLine(args.Data); });
                p.ErrorDataReceived += new DataReceivedEventHandler(delegate(object sender, DataReceivedEventArgs args) { Console.Error.WriteLine(args.Data); });
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    throw new Exception("Build failed");
                }
            }
        }

        #region FoxWebClient Methods

        private static void CreateApplication()
        {
            LogEvent(EventType.DEBUG, "Creating application in IIS");
            string error = string.Empty;
            string result = ExecCmd("create", out  error);

            if (string.IsNullOrEmpty(error))
                LogEvent(EventType.DEBUG, "Application created sucessfully in IIS");
        }

        private static void DeleteApplication()
        {
            LogEvent(EventType.DEBUG, "Deleting application from IIS");
            string error = string.Empty;
            string result = ExecCmd("delete", out  error);

            if (string.IsNullOrEmpty(error))
                LogEvent(EventType.DEBUG, "Application deleted sucessfully from IIS");
        }

        private static bool ApplicationStatus()
        {
            string error = string.Empty;
            string result = ExecCmd("status", out  error);

            if (!string.IsNullOrEmpty(result))
            {
                if (result.Contains("NOT-RUNNING"))
                    return false;
                else if (result.Contains("RUNNING"))
                    return true;
            }
            return false;
        }

        private static void SetEnvironment(ProcessStartInfo psi)
        {
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                //Set Environement variable for FoxWeb Client
                if (!psi.EnvironmentVariables.ContainsKey(envVar.Key.ToString()))
                    psi.EnvironmentVariables.Add(envVar.Key.ToString(), envVar.Value.ToString());
                else
                    psi.EnvironmentVariables[envVar.Key.ToString()] = envVar.Value.ToString();

                //Set Envrironment Variables for Current User
                Environment.SetEnvironmentVariable(envVar.Key.ToString(), envVar.Value.ToString(), EnvironmentVariableTarget.User);
            }
        }

        private static string ExecCmd(string action, out string error)
        {
            string argument = string.Format("'{0}'", action.ToUpper());
            LogEvent(EventType.DEBUG, "Executing FoxWebClient with Arguments = " + argument);

            ProcessStartInfo psi = new ProcessStartInfo("FoxWebClient.exe", argument);
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            var secure = new SecureString();
            foreach (char c in password)
                secure.AppendChar(c);

            psi.Domain = domain;
            psi.UserName = admin;
            psi.Password = secure;

            SetEnvironment(psi);

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.Start();

            string result = proc.StandardOutput.ReadToEnd();
            while (!proc.StandardOutput.EndOfStream)
                result += proc.StandardOutput.ReadLine();

            error = string.Empty;
            while (!proc.StandardOutput.EndOfStream)
                error += proc.StandardError.ReadLine();


            proc.Close();

            if (!string.IsNullOrEmpty(result))
                LogEvent(EventType.DEBUG, "FoxWebClient Result = " + result);
            if (!string.IsNullOrEmpty(error))
                LogEvent(EventType.DEBUG, "FoxWebClient Error = " + error);

            return result;
        }

        #endregion

        #region Client Result

        private static void ClientResult(string text)
        {
            ClientOut("CLIENT_RESULT", text);
        }

        private static void ClientOut(string type, string output)
        {
            foreach(string line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                string text = string.Format("{0}: {1}", type, line);
                Console.WriteLine(text);
            }
        }

        #endregion

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
                eventType.ToString(), "openshift.winnode.foxweb.control", message) + Environment.NewLine);
        }

        #endregion

    }
}
