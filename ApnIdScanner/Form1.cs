using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ApnIdScanner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void scanResult(Boolean isSuccess, int id)
        {
            if (isSuccess && this.IsHandleCreated)
            {
                this.BeginInvoke((Action)delegate()
                {
                    String url = "http://ib.adnxs.com/ttj?id={0}";
                    url = string.Format(url, id.ToString());
                    textBox3.AppendText(url + "\r\n");
                });
            }
        }


        /// <summary>
        /// 扫描ID
        /// </summary>
        public class scanId
        {
            /// <summary>
            /// 广告ID
            /// </summary>
            int _id;

            public delegate void scanResultDelegate(Boolean isSuccess, int id);

            public event scanResultDelegate onScanResultEvent;

            public scanId(int id, scanResultDelegate dlgt)
            {
                _id = id;
                onScanResultEvent += dlgt;
            }

            public void run(object obj)
            {
                String url = "http://ib.adnxs.com/ttj?id={0}";
                url = string.Format(url, _id.ToString());
                if (GetHTTPPage(url).Length > 0)
                {
                    if (onScanResultEvent != null)
                    {
                        onScanResultEvent(true, _id);
                    }
                } 
                else
                {
                    if (onScanResultEvent != null)
                    {
                        onScanResultEvent(false, _id);
                    }
                }
            }

            private string GetHTTPPage(string url)
            {
                HttpWebRequest request = null;
                HttpWebResponse response = null;
                StreamReader reader = null;
                try
                {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1";
                    request.Timeout = 30000;
                    request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    request.Proxy = null;
                    request.AllowAutoRedirect = true;
                    response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK && response.ContentLength < 1024 * 1024)
                    {
                        Stream stream = response.GetResponseStream();
                        stream.ReadTimeout = 30000;
                        if (response.ContentEncoding == "gzip")
                        {
                            reader = new StreamReader(new GZipStream(stream, CompressionMode.Decompress), Encoding.Default);
                        }
                        else
                        {
                            reader = new StreamReader(stream, Encoding.Default);
                        }
                        string html = reader.ReadToEnd();
                        return html;
                    }
                }
                catch(Exception ex)
                {
                    //System.Console.WriteLine(ex.Message);
                    return "";
                }
                finally
                {
                    if (response != null)
                    {
                        response.Close();
                        response = null;
                    }
                    if (reader != null)
                        reader.Close();
                    if (request != null)
                        request = null;
                }
                return "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button1.Text = "扫描中...";
            ThreadPool.SetMaxThreads(150, 512); //最大线程150
            int begin = int.Parse(textBox1.Text);
            int end = int.Parse(textBox2.Text);
            for (int i = begin; i <= end; i++ )
            {
                scanId scan = new scanId(i, new scanId.scanResultDelegate(scanResult));
                ThreadPool.QueueUserWorkItem(new WaitCallback(scan.run));
            }
        }
        
    }
}
