﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfTaskManager
{
    public class TaskManagerViewModel : ViewModelBase
    {
        //[DllImport("User32.dll")]
        //public static extern Int32 SetForegroundWindow(int hWnd);

        private ObservableCollection<ProcessItem> processCollection;
        public ObservableCollection<ProcessItem> ProcessCollection { get => processCollection; set => Set(ref processCollection, value); }

        private ProcessItem selItem;
        public ProcessItem SelItem { get => selItem; set => Set(ref selItem, value); }

        private string startProcess;
        public string StartProcess { get => startProcess; set => Set(ref startProcess, value); }

        private Timer tm;
        private string RemoteIP { get; set; } = "192.168.5.65";

        public TaskManagerViewModel()
        {
            
            ProcessCollection = new ObservableCollection<ProcessItem>();

            foreach (var item in Process.GetProcesses(RemoteIP))
            {
                try
                {
                    var newItem = new ProcessItem();
                    newItem.Pid = item.Id;
                    newItem.Name = item.ProcessName;
                   
                    ProcessCollection.Add(newItem);

                }
                catch (Exception) { }

                tm = new Timer(LoadProcesses, null, 0, 5000);

            }
        }

   
        public void LoadProcesses(object sender)
        {
            var newColl = new ObservableCollection<ProcessItem>();

            foreach (var item in Process.GetProcesses(RemoteIP))
            {
                try
                {
                    if (ProcessCollection.Where(i => i.Pid == item.Id).Any())
                    {
                        var oldItem = ProcessCollection.Where(i => i.Pid == item.Id).Single();
                        newColl.Add(oldItem);
                    }
                    else
                    {
                        var newItem = new ProcessItem();
                        newItem.Pid = item.Id;
                        newItem.Name = item.ProcessName;
                        newColl.Add(newItem);
                    }
                }
                catch (Exception) { }
            }
            
            Application.Current.Dispatcher.Invoke( () =>
            {
                ProcessCollection = newColl;
            });

        }

      
        private RelayCommand addCommand;
        public RelayCommand AddCommand
        {
            get => addCommand ?? (addCommand = new RelayCommand(
                () =>
                {
                    if (!String.IsNullOrEmpty(StartProcess))
                    {
                        try
                        {
                            //InitiateProcessOnRemoteMachine();

                            ConnectionOptions theConnection = new ConnectionOptions();
                            ManagementScope manScope = new ManagementScope("\\\\" + RemoteIP + "\\root\\cimv2", theConnection);
                            manScope.Connect();
                            ObjectGetOptions objectGetOptions = new ObjectGetOptions();
                            ManagementPath managementPath = new ManagementPath("Win32_Process");
                            ManagementClass processClass = new ManagementClass(manScope, managementPath, objectGetOptions);
                            ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
                            inParams["CommandLine"] = StartProcess;
                            ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, null);


                            //var par2 = outParams["processId"].ToString();
                            //int prId = int.Parse(par2);

                            //var proc = Process.GetProcessById(prId, RemoteIP);
                            //proc.StartInfo.UseShellExecute = false;

                            //var hwd = proc.MainWindowHandle;
                            //SetForegroundWindow(hwd.ToInt32());

                            //var handle = Process.GetCurrentProcess().MainWindowHandle;
                            //Process.Start("notepad.exe").WaitForInputIdle();
                            //SetForegroundWindow(handle.ToInt32());


                            StartProcess = "";
                            ProcessCollection.Clear();
                            LoadProcesses(null);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            ));
        }

        private RelayCommand endCommand;
        public RelayCommand EndCommand
        {
            get => endCommand ?? (endCommand = new RelayCommand(
                () =>
                {
                    if (SelItem == null)
                        MessageBox.Show("Select any process");
                    else
                    {
                        Process p = Process.GetProcessById(SelItem.Pid, RemoteIP);
                        new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "TaskKill.exe",
                                Arguments = string.Format("/pid {0} /s {1}", p.Id, p.MachineName),
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true
                            }
                        }.Start();

                    }
                }
            ));
        }

        //private static void InitiateProcessOnRemoteMachine() //string _remoteMachineName, string executableFileCompletePath, string ParamForExeFile)
        //{

        //    string psexec = @"c:\psexec\psexec.exe"; //Path to psexec
        //                                             //If we need to pass some parameter then we need to pass it with the exe enclose in double coutes else can just use path to executable
        //    string command = "notepad.exe";//executableFileCompletePath + " \"" + ParamForExeFile + "\"";
        //    string node = @"\\" + "192.168.5.65"; //_remoteMachineName;
        //    // string arguments = "-i " + node + " -u " + "UserName" + " -p " + "password" + " " + command; //parameters
        //    string arguments = "-i " + node + " " + command;
        //    //Console.WriteLine(arguments); //The arguments ready for psexec

        //    Process Psexec = new Process();
        //    Psexec.StartInfo.FileName = psexec;
        //    Psexec.StartInfo.Arguments = "-accepteula";
        //    Psexec.Start();
        //    Thread.Sleep(3000);

        //    Process GenTCM = new Process();
        //    GenTCM.StartInfo.FileName = psexec;
        //    GenTCM.StartInfo.Arguments = arguments;
        //    GenTCM.StartInfo.UseShellExecute = false;
        //    GenTCM.StartInfo.RedirectStandardError = true;
        //    GenTCM.Start();
        //    int intTimeOut = 100;
        //    int Flag = 0;
        //    while (GenTCM.HasExited == false)
        //    {
        //        Thread.Sleep(1000);
        //        Flag = Flag + 1;
        //        if (intTimeOut == Flag)
        //        {
        //            Process[] psexecProcess = Process.GetProcessesByName("psexec");
        //            foreach (Process p in psexecProcess)
        //            { p.Kill(); }
        //            break;
        //        }
        //    }
        //    string actualOutput = "";
        //    string errorOutput = GenTCM.StandardError.ReadToEnd();
        //    if (errorOutput.Contains("error code") && !errorOutput.Contains("error code 0"))
        //    {
        //        string[] output = errorOutput.Split(new[] { '\n' });
        //        errorOutput = output[5];
        //        // Console.WriteLine("[ERROR] " + errorOutput + "\r\n");
        //        MessageBox.Show("[ERROR] " + errorOutput);
        //    }
        //    else
        //    {
        //        string[] output = errorOutput.Split(new[] { '\n' });
        //        actualOutput = output[5];
        //        actualOutput = actualOutput.Replace("\r", "");
        //        //  Console.WriteLine("[SUCCESS] TCM Command is launched.\r\n" + actualOutput);
        //        MessageBox.Show("[SUCCESS] TCM Command is launched. " + actualOutput);
        //    }

        //}

    }
}
