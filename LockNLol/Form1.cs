using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace locknlol
{
    public partial class Form1 : Form
    {

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public uint dbch_size;
            public uint dbch_devicetype;
            public uint dbch_reserved;
        }

        private const uint DBT_DEVTYP_VOLUME = 0x00000002;
        private const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const ushort DBTF_MEDIA = 0x0001; // Media in drive changed.
        private const ushort DBTF_NET = 0x0002; // Network drive is changed.

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_VOLUME
        {
            public uint dbcv_size;
            public uint dbcv_devicetype;
            public uint dbcv_reserved;
            public uint dbcv_unitmask;
            public ushort dbcv_flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public uint dbcc_size;
            public uint dbcc_devicetype;
            public uint dbcc_reserved;
            public Guid dbcc_classguid;

            // To get value from lParam of WM_DEVICECHANGE, this length must be longer than 1.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        private bool AllowDisplay = false;

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(AllowDisplay ? value : AllowDisplay);
        }

        string[] lockDevices = new string[] {  };



        public Form1()
        {
            InitializeComponent();
            DeviceNotification.RegisterUsbDeviceNotification(this.Handle, false);
            RefreshConfigFile();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            if (m.Msg == DeviceNotification.WmDeviceChange)
            {
                switch ((int)m.WParam)
                {
                    case DeviceNotification.DbtDeviceRemoveComplete:
                        Write("Removed!");
                        HandleMessage(m.LParam, true);
                        Write("---");
                        break;
                    case DeviceNotification.DbtDeviceArrival:
                        Write("Added!");
                        HandleMessage(m.LParam, false);
                        Write("---");
                        break;
                }
            }
        }

        private void HandleMessage(IntPtr lParam, bool removed)
        {
            var hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_HDR));
            var deviceInterface = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));

            uint devType = hdr.dbch_devicetype;
            string devName = deviceInterface.dbcc_name;

            // 6e8cf83&0&Yubico_YubiKey
            Write(devType.ToString());
            Write($"dbcc_name = {deviceInterface.dbcc_name}");
            Write($"dbcc_devicetype = {deviceInterface.dbcc_devicetype}");
            Write($"dbcc_classguid = {deviceInterface.dbcc_classguid}");

            // ACS_ACR122U_PICC_Interface_0_SCFILTER_CID_8073c021c057597562694b65ff
            // \\?\USB#VID_2109&PID_8887#40AN
            // 446c009&0&Yubico_YubiKey

            if (removed) {
                foreach(string lockDev in lockDevices)
                {
                    if (Regex.IsMatch(devName, lockDev)) {
                        WriteLog($"+ \"{devName}\" matches /{lockDev}/");
                        if(chkEnabled.Checked)
                            LockWorkStation();
                    }
                    else
                        WriteLog($"- \"{devName}\" doesn't match /{lockDev}/");
                }
            } 

        }

        void RefreshConfigFile()
        {
            string path = Path.Combine(AssemblyDirectory, "locknlol.conf");
            if (File.Exists(path))
                this.lockDevices = File.ReadAllLines(path);
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        void WriteLog(string Message) { 
            textBox1.AppendText(Message + Environment.NewLine);
        }

        void Write(string Message)
        {
            if(chkLog.Checked)
                WriteLog(Message);
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.AllowDisplay = true;
            this.Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            RefreshConfigFile();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
