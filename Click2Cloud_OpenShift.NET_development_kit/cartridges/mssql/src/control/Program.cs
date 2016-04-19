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

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Control
{
    
    class Program
    {

        private const int sleepSeconds = 3000;
        private const string sqlQueryCreateDB = @"IF NOT EXISTS(select * from sys.databases where name='{0}') CREATE DATABASE [{0}]";

        private const string sucessMessage = @"
Microsoft SQL Server {0} database added.  Please make note of database server details to login through Microsoft SQL Server Management Studio:
    
    database host: {1}
    database port: {2}
   login username: {3}
         password: {4}   
    database name: {5}
    
 NOTE: Please change password on first login.

";

        static string pidFile;
        static string instanceType;
        static string version;
        static string dbHostName;
        static string dbOwnerName = string.Empty;

        static string myLogFile = string.Empty;
        static int Main(string[] args)
        {
            try
            {
                string mssqlDir = Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DIR");
                string myLog = Path.Combine(mssqlDir, "mylog");
                Directory.CreateDirectory(myLog);

                myLogFile = Path.Combine(myLog, "mssqlevents.txt");

                System.IO.File.AppendAllText(myLogFile, "-- Main Started --\n");

                version = ConfigurationManager.AppSettings["Version"];
                instanceType = ConfigurationManager.AppSettings["InstanceType"];
                dbHostName = Environment.GetEnvironmentVariable("OPENSHIFT_GEAR_DNS");

                System.IO.File.AppendAllText(myLogFile, string.Format("\nversion: {0}, Instance Type: {1}, Gear DNS: {2}\n", version, instanceType, dbHostName));

                pidFile = Path.Combine(Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DIR"), "run", "mssql.pid");
                System.IO.File.AppendAllText(myLogFile, string.Format("\npid File :{0}\n", pidFile));

                Environment.SetEnvironmentVariable("MSSQL_PID_FILE", pidFile);

                System.IO.File.AppendAllText(myLogFile, string.Format("\nEvent is :{0}\n", args[0]));
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
                    default:
                        {
                            break;
                        }
                }

                System.IO.File.AppendAllText(myLogFile, "\n-- Switch function executed --\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(myLogFile, string.Format("\nError : {0}, \nTrace: {1}\n", ex.Message, ex.StackTrace));

                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }

        private static void StartCartridge()
        {
            if (ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Cartridge already running");
                return;
            }
            Console.WriteLine(string.Format("Startring MSSQL {0} cartridge", version));
            System.IO.File.AppendAllText(myLogFile, string.Format("Startring MSSQL {0} cartridge", version));

            string mssqlDir = Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DIR");
            string logDir = Path.Combine(mssqlDir, "log");
            Directory.CreateDirectory(logDir);

            // set variables
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dbPort = Environment.GetEnvironmentVariable("OPENSHIFT_MSSQL_DB_PORT");
            string instanceName = string.Format("Instance{0}", dbPort);
            string instanceDir = Path.Combine(currentDir, string.Format("{0}.{1}", instanceType, instanceName));
            string dbName = Environment.GetEnvironmentVariable("OPENSHIFT_APP_NAME");
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            string password = File.ReadAllText(Path.Combine(instanceDir, "sqlpasswd"));

            //File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_USERNAME", mssqlDir), username);
            //File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_PASSWORD", mssqlDir), password);

            //SMP:
            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_HOST", mssqlDir), dbHostName);
            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_PORT", mssqlDir), dbPort);
            dbOwnerName = "admin_" + username.Split('_').Last();

            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_USERNAME", mssqlDir), dbOwnerName);
            //File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_SA_PASSWORD", mssqlDir), password);

            string dbOwnerPassword = GeneratePassword();
            File.WriteAllText(string.Format(@"{0}\env\OPENSHIFT_MSSQL_DB_PASSWORD", mssqlDir), dbOwnerPassword);

            ////////////////////////////////


            //build registry file
            string registryFile = Path.Combine(currentDir, "sqlserver.reg");
            WriteTemplate(Path.Combine(currentDir, string.Format(@"..\versions\{0}\sqlserver.reg.template", version)), registryFile, instanceName, currentDir, dbPort);

            //import registry file
            RunProcess(@"cmd.exe", "/C reg import " + registryFile + " /reg:64", "Error while importing registry file");

            //start SQL server service
            ProcessStartInfo sqlserver = new ProcessStartInfo();
            sqlserver.WindowStyle = ProcessWindowStyle.Hidden;
            sqlserver.FileName = @"cmd.exe";
            sqlserver.Arguments = string.Format(@"/c {0}\mssql\binn\sqlservr.exe -c -s {1} 1>>{2}\\stdout.log 2>>{2}\stderr.log", instanceDir, instanceName, logDir);
            Process sqlProcess = Process.Start(sqlserver);

            //create application database
            string connectionString = string.Format(@"server=127.0.0.1,{0}; database=master; User Id=sa; Password={1}; connection timeout=30", dbPort, password);
            bool success = false;


            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Thread.Sleep(sleepSeconds);
                        sqlConnection.Open();
                        using (SqlCommand sqlCmd = new SqlCommand(string.Format(sqlQueryCreateDB, dbName)))
                        {
                            sqlCmd.Connection = sqlConnection;
                            sqlCmd.ExecuteNonQuery();
                        }

                        //SMP:
                        if (CreateLogin(dbOwnerName, dbOwnerPassword, connectionString, dbName))
                            success = true;

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.ToString());
                    }
                }
            }

            if (!success)
            {
                throw new Exception("Cannot connect to SQL Server instance");
            }


            //string text = string.Format(sucessMessage, version, password, dbName, dbHostName, dbPort);
            string text = string.Format(sucessMessage, version, dbHostName, dbPort, dbOwnerName, dbOwnerPassword, dbName);
            //TODO:Change it after fixing issue
            //string text = string.Format(sucessMessage, version, dbHostName, dbPort, "sa", password, dbName);

            ClientResult(text);

            Console.WriteLine(sqlProcess.Id);
            File.WriteAllText(pidFile, sqlProcess.Id.ToString());
        }

        //SMP:
        private static string GeneratePassword()
        {
            string PasswordLength = "12";
            string NewPassword = "";

            string allowedChars = "";
            allowedChars = "1,2,3,4,5,6,7,8,9,0";
            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";
            allowedChars += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";
            allowedChars += "~,!,@,#,$,%,^,&,*,+,?";

            char[] sep = { ',' };

            string[] arr = allowedChars.Split(sep);

            string IDString = "";

            string temp = "";

            Random rand = new Random();

            for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)
            {
                temp = arr[rand.Next(0, arr.Length)];
                IDString += temp;
                NewPassword = IDString;
            }

            return NewPassword;

        }

        //SMP: Code added on 19-01-2015 to create login user
        private static bool CreateLogin(string userName, string password, string connectionString, string databaseName)
        {
            bool isLoginCreated = false;
            try
            {
                ServerConnection serverConnection = new ServerConnection(new SqlConnection(connectionString));
                Server sqlServerInstance = new Server(serverConnection);

                Login loginUser = new Login(sqlServerInstance, userName);
                loginUser.DefaultDatabase = "Master";
                loginUser.LoginType = LoginType.SqlLogin;

                loginUser.PasswordExpirationEnabled = true;
                loginUser.PasswordPolicyEnforced = true;

                loginUser.Create(password, LoginCreateOptions.MustChange);

                loginUser.Enable();

                Database database = sqlServerInstance.Databases[databaseName];

                User sqlServerUser = new User(database, userName);
                sqlServerUser.UserType = UserType.SqlLogin;
                sqlServerUser.Login = userName;

                sqlServerUser.Create();

                sqlServerUser.AddToRole("db_owner");

                isLoginCreated = true;
            }
            catch (Exception ex)
            {
                ClientError("Error Message: " + ex.Message + "\n Error Stack: " + ex.StackTrace + "\n Original Message:" + ex.ToString());
            }

            return isLoginCreated;
        }


        private static void StopCartridge()
        {
            if (ProcessRunning("cmd", pidFile))
            {
                Console.WriteLine("Stopping");
                int processId = int.Parse(File.ReadAllText(pidFile));
                Process.Start("taskkill", string.Format("/F /T /PID {0}", processId)).WaitForExit();
                File.Delete(pidFile);
            }
            else
            {
                Console.WriteLine("Cartridge is not running");
            }
        }

        private static void CartridgeStatus()
        {
            Console.WriteLine("Retrieving cartridge");
            if (ProcessRunning("cmd", pidFile))
            {
                ClientResult("Application is running");
            }
            else
            {
                ClientResult("Application is either stopped or inaccessible");
            }
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

        private static bool ProcessRunning(string processName, string pidFile)
        {
            if (!File.Exists(pidFile))
            {
                return false;
            }

            int processId = int.Parse(File.ReadAllText(pidFile));
            Process process = Process.GetProcesses().Where(m => m.Id == processId && m.ProcessName == processName).FirstOrDefault();
            if (process == null)
            {
                return false;
            }
            return true;
        }

        private static void ClientError(string text)
        {
            ClientOut("CLIENT_ERROR", text);
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

        private static void WriteTemplate(string inFile, string outFile, string instanceName, string baseDir, string tcpPort)
        {
            baseDir = baseDir.Replace(@"\", @"\\");
            string content = File.ReadAllText(inFile, System.Text.Encoding.ASCII).Replace("${InstanceName}", instanceName).Replace("${BaseDir}", baseDir).Replace("${tcpPort}", tcpPort);
            File.WriteAllText(outFile, content, System.Text.Encoding.ASCII);

            System.IO.File.AppendAllText(myLogFile, "\nWrite Template: " + content);
        }

        private static void RunProcess(string processFile, string arguments, string exception)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.FileName = processFile;
            processInfo.Arguments = arguments;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            Process process = Process.Start(processInfo);
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());

            System.IO.File.AppendAllText(myLogFile, "\nRun Process: " + process.StandardOutput.ReadToEnd());
            if (process.ExitCode != 0)
            {
                throw new Exception(string.Format("{0}: {1}", exception, process.StandardError.ReadToEnd()));
            }
        }
    }
}