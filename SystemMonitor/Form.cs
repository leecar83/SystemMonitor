using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SystemMonitor
{
    public partial class Form : System.Windows.Forms.Form
    {
        #region DLL Imports and Constants
        private const String NETWORKREGKEYNAME = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles";
        private const String LOGDIRECTORY = @"C:\StoreSys\Applications\Logs\";

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private extern static int QueryDisplayConfig([In] uint flags, ref uint numPathArrayElements, IntPtr pathArray, ref uint numModeArrayElements, IntPtr modeArray, out IntPtr currentTopologyId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private extern static int GetDisplayConfigBufferSizes([In] uint flags, [Out] out uint numPathArrayElements, [Out] out uint numModeArrayElements);

        private const uint SIZE_OF_DISPLAYCONFIG_PATH_INFO = 72;
        private const uint SIZE_OF_DISPLAYCONFIG_MODE_INFO = 64;

        private const uint SDC_TOPOLOGY_INTERNAL = 1;
        private const uint SDC_TOPOLOGY_CLONE = 2;
        private const uint SDC_TOPOLOGY_EXTEND = 4;
        private const uint SDC_TOPOLOGY_EXTERNAL = 0x00000008;
        private const uint SDC_TOPOLOGY_SUPPLIED = 0x00000010;

        private const uint QDC_ALL_PATHS = 1;
        private const uint QDC_ONLY_ACTIVE_PATHS = 2;
        private const uint QDC_DATABASE_CURRENT = 4;
        #endregion

        public Form()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            setupLogFolder();
            log("Starting Up");
            backgroundWorker1.RunWorkerAsync();
            backgroundWorker2.RunWorkerAsync();
        }

        private void Form_Load(object sender, EventArgs e)
        {
    
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            setNetworkType();
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            setDisplayTopology();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }

        private void setNetworkType()
        {
            RegistryKey localKey = null;
            try
            {
                log("Setting all networks to Private");
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                localKey = Registry.LocalMachine.OpenSubKey(NETWORKREGKEYNAME);
                foreach (String subKey in localKey.GetSubKeyNames())
                {
                    //Registry.SetValue(localKey + @"\" + subKey, "Category", 1);
                    log(subKey + " Set to Private Network");
              
                }
            }
            catch (Exception ex)
            {
                log(ex.Message);
            }
            finally
            {
                localKey.Dispose();
            }
        }

        private void setDisplayTopology()
        {
            try
            {
                log("Checking Display Topology");
                int i = Screen.AllScreens.Count();
                log(i.ToString() + " display(s) connected");
                if (i > 1)
                {
                    Topology current = getCurrentTopology();
                    log("Current display topology is " + current.ToString());
                    if(current == Topology.Extend)
                    {
                        log("Attempting to switch to clone mode");
                        //Process proc = Process.Start("DisplaySwitch", "/clone");
                        //proc.WaitForExit();
                        log("Waiting for 15sec DisplaySwitch to work");
                        Thread.Sleep(15000);
                        current = getCurrentTopology();
                        log("Display topology set to " + current.ToString());
                    }   
                }
            }
            catch (Exception ex)
            {
                log(ex.Message);
            }
        }

        private Topology getCurrentTopology()
        {
            uint numPathArrayElements;
            uint numModeArrayElements;
            IntPtr pPathArray;
            IntPtr pModeArray;
            IntPtr pTopology = IntPtr.Zero;
            Topology topology = Topology.Unknown;

            int ret = GetDisplayConfigBufferSizes(QDC_ALL_PATHS, out numPathArrayElements, out numModeArrayElements);

            if (ret == 0)
            {
                pPathArray = Marshal.AllocHGlobal((Int32)(numPathArrayElements * SIZE_OF_DISPLAYCONFIG_PATH_INFO));
                pModeArray = Marshal.AllocHGlobal((Int32)(numModeArrayElements * SIZE_OF_DISPLAYCONFIG_MODE_INFO));

                ret = QueryDisplayConfig(QDC_DATABASE_CURRENT, ref numPathArrayElements, pPathArray, ref numModeArrayElements, pModeArray, out pTopology);
            }

            if (pTopology == (IntPtr)1)
            {
                topology = Topology.Internal;
            }
            else if (pTopology == (IntPtr)2)
            {
                topology = Topology.Clone;
            }
            else if (pTopology == (IntPtr)4)
            {
                topology = Topology.Extend;
            }
            else if (pTopology == (IntPtr)0x00000008)
            {
                topology = Topology.External;
            }
            else if (pTopology == (IntPtr)0x00000010)
            {
                topology = Topology.Supplied;
            }

            return topology;
        }

        private void setupLogFolder()
        {
            try
            {
                if(!Directory.Exists(LOGDIRECTORY))
                {
                    Directory.CreateDirectory(LOGDIRECTORY);
                }
            }
            catch(Exception ex)
            {

            }
        }

        private static void log(String LogEntry, String LogFile = @"C:\StoreSys\Applications\Logs\")
        {
            String strLogFileName = DateTime.Now.ToString("yyyyMMdd") + @".log";
            LogFile += strLogFileName;
            try
            {
                lock (LogFile)
                {
                    using (StreamWriter streamWriter = new StreamWriter(LogFile, true))
                    {
                        streamWriter.WriteLine(DateTime.Now.ToString() + " " + LogEntry + "...");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
        }

        enum Topology
        {
            Internal,
            Clone,
            Extend,
            External,
            Supplied,
            Unknown
        };

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
