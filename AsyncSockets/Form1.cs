using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsyncSockets
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // The OnTaskCompleted event callback is setup in Form1_Load.
        private Chilkat.Socket m_clientSock = new Chilkat.Socket();



        // Connect to an HTTP server asynchronously, send an HTTP GET request (async)
        // and receive the response (also async).
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Starting Async Client Example...\r\n";

            // Connect to some website asynchronously.
            Chilkat.Task task = m_clientSock.ConnectAsync("www.bonairefishing.com", 80, false, 10000);
            task.UserData = "connect";
            task.Run();
        }

        // This is called from m_clientSock_OnTaskCompleted and is therefore in the background thread.
        private void sendHttpGet()
        {
            string httpGetRequest = "GET / HTTP/1.1\r\nHost: www.bonairefishing.com\r\n\r\n";

            Chilkat.Task task = m_clientSock.SendStringAsync(httpGetRequest);
            task.UserData = "sendHttpGet";
            task.Run();
        }

        // This is called from m_clientSock_OnTaskCompleted and is therefore in the background thread.
        private void readHttpResponseHeader()
        {
            Chilkat.Task task = m_clientSock.ReceiveUntilMatchAsync("\r\n\r\n");
            task.UserData = "readResponseHeader";
            task.Run();
        }

        // This is called from m_clientSock_OnTaskCompleted and is therefore in the background thread.
        private void readHttpResponseBody(int contentLength)
        {
            Chilkat.Task task = m_clientSock.ReceiveBytesNAsync((uint)contentLength);
            task.UserData = "readResponseBody";
            task.Run();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Unlock all of Chilkat, and discard the Chilkat.Global object.
            // Once unlocked, all subsequent Chilkat objects are created already unlocked.
            Chilkat.Global glob = new Chilkat.Global();
            bool success = glob.UnlockBundle("Anything for 30-day trial");
            if (!success) MessageBox.Show("30-day trial has ended.");

            m_clientSock.OnTaskCompleted += m_clientSock_OnTaskCompleted;
        }


        // When we're in a background thread, we should update UI elements in the foreground thread.
        private void fgAppendToTextBox1(string s)
        {
            this.Invoke((MethodInvoker)delegate
            {
                textBox1.Text += s;
            });
        }

        // Remember, this callback happens in a background thread...
        void m_clientSock_OnTaskCompleted(object sender, Chilkat.TaskCompletedEventArgs args)
        {
            if (args.Task.UserData.Equals("connect"))
            {
                if (args.Task.GetResultBool() == false)
                {
                    fgAppendToTextBox1("Connect failed!\r\n");
                    fgAppendToTextBox1(args.Task.ResultErrorText);
                    return;
                }

                // This was our ConnectAsync call that completed...
                fgAppendToTextBox1("Connect completed.\r\n");
                sendHttpGet();
            }
            else if (args.Task.UserData.Equals("sendHttpGet"))
            {
                if (args.Task.GetResultBool() == false)
                {
                    fgAppendToTextBox1("Send GET failed!\r\n");
                    fgAppendToTextBox1(args.Task.ResultErrorText);
                    return;
                }

                fgAppendToTextBox1("GET request sent.\r\n");
                readHttpResponseHeader();
            }
            else if (args.Task.UserData.Equals("readResponseHeader"))
            {
                if (args.Task.TaskSuccess == false)
                {
                    fgAppendToTextBox1("Read response header failed!\r\n");
                    fgAppendToTextBox1(args.Task.ResultErrorText);
                    return;
                }

                fgAppendToTextBox1("Received response header.\r\n");
                string receivedString = args.Task.GetResultString();
                fgAppendToTextBox1(receivedString);

                // The receivedString contains the HTTP start line followed by the MIME response header.
                // (for this example, discard the start line)
                int endOfFirstLine = receivedString.IndexOf("\r\n");
                string responseHeader = receivedString.Substring(endOfFirstLine + 2);

                // Parse the MIME to find out the size of the HTTP response body.
                Chilkat.Mime mime = new Chilkat.Mime();
                mime.LoadMime(responseHeader);
                string strContentLength = mime.GetHeaderField("Content-Length");
                if (strContentLength == null)
                {
                    fgAppendToTextBox1("No Content-Length header in the response!\r\n");
                    return;
                }
                int contentLength = Convert.ToInt32(strContentLength);

                readHttpResponseBody(contentLength);
            }
            else if (args.Task.UserData.Equals("readResponseBody"))
            {
                if (args.Task.TaskSuccess == false)
                {
                    fgAppendToTextBox1("Read response body failed!\r\n");
                    fgAppendToTextBox1(args.Task.ResultErrorText);
                    return;
                }

                fgAppendToTextBox1("Received response body.\r\n");
                byte[] responseBody = args.Task.GetResultBytes();
                // For this example, assume utf-8.
                string responseBodyStr = System.Text.Encoding.UTF8.GetString(responseBody);
                fgAppendToTextBox1(responseBodyStr);
                fgAppendToTextBox1("\r\nHTTP GET Completed Successfully\r\n");

            }

        }
    }
}
