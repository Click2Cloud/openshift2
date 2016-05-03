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
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Control
{
    class Program
    {
        static string logDir = string.Empty;
        static string filePath = string.Empty;

        static int Main(string[] args)
        {
            string sharePointDir = Environment.GetEnvironmentVariable("OPENSHIFT_SHAREPOINT_DIR");
            logDir = Path.Combine("C:\\openshift", "log", Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME"));
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            filePath = Path.Combine(logDir, Guid.NewGuid() + "-CONTROL-SCRIPT-LOGS.txt");

            LogEvent("---Control Script started--- Event Started "+ args[0] + " at" + DateTime.Now);

            try
            {
                switch (args[0])
                {
                    case "start":
                        {
                            StartCartridge();
                            break;
                        }
                    case "stop":
                        {
                            StopCartridge();
                            break;
                        }
                    case "status":
                        {
                            CartridgeStatus();
                            break;
                        }
                    case "reload":
                        {
                            ReloadCartridge();
                            break;
                        }
                    case "restart":
                        {
                            RestartCartridge();
                            break;
                        }
                    case "build":
                        {
                            Build();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                LogEvent("Control executed completely");
            }
            catch (Exception ex)
            {
                LogError(ex);
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }

        private static void StartCartridge()
        {

        }

        private static void StopCartridge()
        {
            string dns = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");

            control.SharePointService.SharePointClient client = new control.SharePointService.SharePointClient();

            ((BasicHttpBinding)client.Endpoint.Binding).MaxReceivedMessageSize = int.MaxValue;
            ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferSize = int.MaxValue;
            ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferPoolSize = int.MaxValue;

            client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 60, 00);

            client.DeleteSPSiteCollection(dns);
        }

        private static void CartridgeStatus()
        {
        }

        private static void ReloadCartridge()
        {
        }

        private static void RestartCartridge()
        {
        }

        private static void Build()
        {
           
        }

        private static void ClientResult(string text)
        {
            ClientOut("CLIENT_RESULT", text);
        }

        private static void ClientOut(string type, string output)
        {
            foreach (string line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                string text = string.Format("{0}: {1}", type, line);
                Console.WriteLine(text);
            }
        }

        private static void LogError(Exception ex)
        {
            try
            {
                System.IO.File.AppendAllLines(filePath, new List<string>() { string.Empty });
                List<string> message = new List<string>();
                message.Add("--------- ERROR -----------------");
                message.Add("Message: " + ex.Message);
                message.Add("Stack Trace: " + ex.StackTrace);
                message.Add("---------------------------------");
                System.IO.File.AppendAllLines(filePath, message);
                System.IO.File.AppendAllLines(filePath, new List<string>() { string.Empty });
            }
            catch (Exception) { }
        }

        private static void LogEvent(string message)
        {
            try
            {
                System.IO.File.AppendAllLines(filePath, new List<string>() { string.Empty });
                System.IO.File.AppendAllLines(filePath, new List<string>() { message });
                System.IO.File.AppendAllLines(filePath, new List<string>() { string.Empty });
            }
            catch (Exception) { }
        }
    }
}
