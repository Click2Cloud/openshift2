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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using Microsoft.SharePoint.Administration;
using System.ServiceModel;
using System.Threading;
//using Microsoft.VisualStudio.SharePoint;

namespace install
{
    class Program
    {
        private static string fbaAdminUserName;
        private static string fbaAdminPassword;

        private static string logDir = string.Empty;
        private static string filePath = string.Empty;

        private static string webTemplate = string.IsNullOrEmpty(ConfigurationManager.AppSettings["SharePointWebTemplate"]) 
            ? null : ConfigurationManager.AppSettings["SharePointWebTemplate"];

        private const string sucessMessage = @"
Microsoft SharePoint 2013 application provisioning has been completed. Please make note of admin credential to login to application after provisioning:
    
   login username: {0}
         password: {1}   

 NOTE: You can change your admin password from application later.

";

        static int Main(string[] args)
        {
            string sharePointDir = Environment.GetEnvironmentVariable("OPENSHIFT_SHAREPOINT_DIR");
            logDir = Path.Combine("C:\\openshift", "log", Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME"));
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            filePath = Path.Combine(logDir, Guid.NewGuid() + "-INSTALL-SCRIPT-LOGS.txt");

            LogEvent("---Install Script started---");

            bool isAdminCreated = GenerateWebApplicationAdmin();

            if (isAdminCreated)
            {
                LogEvent("Admin is created successfully.");

                string envDir = sharePointDir.Replace("sharepoint", ".env");
                try
                {
                    Environment.SetEnvironmentVariable("OPENSHIFT_SHAREPOINT_ADMIN_USERNAME", fbaAdminUserName);
                    System.IO.File.AppendAllText(Path.Combine(envDir, "OPENSHIFT_SHAREPOINT_ADMIN_USERNAME"), fbaAdminUserName);

                    Environment.SetEnvironmentVariable("OPENSHIFT_SHAREPOINT_ADMIN_PASSWORD", fbaAdminPassword);
                    System.IO.File.AppendAllText(Path.Combine(envDir, "OPENSHIFT_SHAREPOINT_ADMIN_PASSWORD"), fbaAdminPassword);
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }

                LogEvent("Running script to create web application.");

                if (CreateSPWebApplication())
                {
                    LogEvent("Creating Application.....");

                    string text = string.Format(sucessMessage, fbaAdminUserName, fbaAdminPassword);
                    ClientResult(text);

                    LogEvent("Application is created.....");
                }
                else
                {
                    LogEvent("Error occured while creating SharePoint Web Application");
                    return 1;
                }
                return 0;
            }
            else
            {
                LogEvent("Error occured while creating SharePoint Web Application");
                return 1;
            }
        }

        static int tryCount = 0;
        private static bool GenerateWebApplicationAdmin()
        {
            try
            {
                tryCount++;
                if (tryCount >= 11)
                    return false;

                fbaAdminPassword = GenerateRandomString(12, true, true, true, true);
                fbaAdminUserName = "admin_oo_" + GenerateRandomString(7, true, true, true, false);
                string email = fbaAdminUserName + "@rgenos.com";

                install.FBAMembershipService.FBAUserClient client = new install.FBAMembershipService.FBAUserClient();
                string result = client.AddUser(fbaAdminUserName, email, fbaAdminPassword);

                if (result == "User created Successfully.")
                    return true;
                else
                    return GenerateWebApplicationAdmin();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private static bool CreateSPWebApplication()
        {
            try
            {
                //string appName = "OPENSHIFT New Site Collection";
                //string url = "testapplication10.openshift.com";
                //fbaAdminUserName = "admin";

                int portNo = int.Parse(Environment.GetEnvironmentVariable("OPENSHIFT_SHAREPOINT_PORT"));
                string appName = "OPENSHIFT-" + Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
                string dns = Environment.GetEnvironmentVariable("OPENSHIFT_APP_DNS");

                #region Aync Call Approach

                //if (!string.IsNullOrEmpty(webTemplate))
                //{
                //    Task.Run(() =>
                //    {
                //        install.SharePointService.SharePointClient client = new install.SharePointService.SharePointClient();

                //        ((BasicHttpBinding)client.Endpoint.Binding).MaxReceivedMessageSize = int.MaxValue;
                //        ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferSize = int.MaxValue;
                //        ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferPoolSize = int.MaxValue;

                //        client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 60, 00);

                //        string response = client.CreateSPSiteCollectionAsynchronous(appName, dns, webTemplate, fbaAdminUserName, fbaAdminPassword);

                //    });

                //    Thread.Sleep(6000);

                //    return true;
                //}

                #endregion

                #region Sync Call Approach

                //else
                {
                    install.SharePointService.SharePointClient client = new install.SharePointService.SharePointClient();

                    ((BasicHttpBinding)client.Endpoint.Binding).MaxReceivedMessageSize = int.MaxValue;
                    ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferSize = int.MaxValue;
                    ((BasicHttpBinding)client.Endpoint.Binding).MaxBufferPoolSize = int.MaxValue;

                    client.Endpoint.Binding.SendTimeout = new TimeSpan(0, 60, 00);

                    string response = client.CreateSPSiteCollection(appName, dns, null, fbaAdminUserName);

                    if (response == "SUCCESS")
                        return true;
                    else
                        return false;

                }

                #endregion

            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }

        }

        private static void client_CreateSPSiteCollectionCompleted(object sender, SharePointService.CreateSPSiteCollectionCompletedEventArgs e)
        {
            var result = e.Result;
            Console.WriteLine(result);
            Console.ReadLine();
        }

        private static string GenerateRandomString(int stringLength, bool allowIntegers, bool allowCapitalLetters, bool allowSmallLetters, bool allowSpecialCharacters)
        {
            string NewString = "";

            string allowedChars = "";
            if (allowIntegers)
                allowedChars += "1,2,3,4,5,6,7,8,9,0";
            if (allowCapitalLetters)
                allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";
            if (allowSmallLetters)
                allowedChars += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";
            if (allowSpecialCharacters)
                allowedChars += "~,!,@,#,$,%,^,&,*,+,?";

            char[] sep = { ',' };

            string[] arr = allowedChars.Split(sep);
            string IDString = "";
            string temp = "";

            Random rand = new Random();

            for (int i = 0; i < stringLength; i++)
            {
                temp = arr[rand.Next(0, arr.Length)];
                IDString += temp;
                NewString = IDString;
            }

            return NewString;

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
