using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Reflection;

namespace Less2Css
{
    public partial class Less2CssService : ServiceBase
    {
        private Alacra.Network.SysLog sysLog = new Alacra.Network.SysLog();
        //private string nodejsPath = @"c:\Program Files\nodejs\node.exe";
        //private string lesscPath = @"C:\Program Files\nodejs\node_modules\less\bin\lessc";
        private string less2cssExecPath = @"c:\Program Files\nodejs\node_modules\.bin\lessc.cmd";
        private ProcessStartInfo psi = new ProcessStartInfo();
        private Assembly asm = Assembly.GetExecutingAssembly();
        private string fileVersion = null;
        private Version version = null;
        private string errorData = "";

        public Less2CssService()
        {
            InitializeComponent();
            psi.FileName = less2cssExecPath;
            psi.EnvironmentVariables["path"] += @";c:\Program Files\nodejs\";   // add path to nodejs for successful execution
            fileVersion = FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
            version = asm.GetName().Version;
        }

        protected override void OnStart(string[] args)
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "Less2Css have started. " + version.ToString());
        }
        
        protected override void OnStop()
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "Less2Css have stopped.");
        }

        private void fileSystemWatcher_Event(object sender, System.IO.FileSystemEventArgs e)
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "File System Watcher event Changed - " + e.ChangeType);
            string outputFilePath = Path.ChangeExtension(e.FullPath, "css");
            string errorFilePath = Path.ChangeExtension(e.FullPath, "err");

            if (File.Exists(errorFilePath))
            {
                try
                {
                    File.Delete(errorFilePath);
                }
                catch (Exception) {}
            }

            psi.Arguments = string.Format("-x \"{0}\" > \"{1}\"", e.FullPath, outputFilePath);


            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;

            errorData = "";
            var p = Process.Start(psi);

            p.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);
            p.BeginErrorReadLine();
            

            if (p.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds) == false)
            {
                sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Error, "Conversion timed out");
            }

            if (!string.IsNullOrEmpty(errorData))
            {
                sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Debug, "Error: " + errorData);
                using (StreamWriter sw = new StreamWriter(errorFilePath))
                {
                    sw.WriteLine(errorData);
                }
            }
        }
              
        void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            errorData += e.Data;
        }

        private void fileSystemWatcher_Event(object sender, System.IO.RenamedEventArgs e)
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "File System Watcher event Renamed" + e.ChangeType);
            string outputFilePath = Path.ChangeExtension(e.FullPath, "css");

        }

        private void fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "File System Watcher event Created - " + e.ChangeType);
            string outputFilePath = Path.ChangeExtension(e.FullPath, "css");
        }

        private void fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            sysLog.Log(Alacra.Network.SysLog.SeverityLevel.Informational, "File System Watcher event Deleted - " + e.ChangeType);
            string outputFilePath = Path.ChangeExtension(e.FullPath, "css");
        }
    }
}
