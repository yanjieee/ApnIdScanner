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
using System.Collections;

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

            _tmp++;
            if (_tmp >= _count)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke((Action)delegate()
                    {
                        button1.Enabled = true;
                        button1.Text = "开始扫描";
                        textBox3.AppendText("扫描结束\r\n");
                    });
                }
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
                String url = "http://ib.adnxs.com/tt?id={0}";
                url = string.Format(url, _id.ToString());
                String html = GetHTTPPage(url);
                if (html.Length > 0)
                {
                    url = GetMid(html, "http://", "'");
                    url = "http://" + url + "&bdref=null&bdtop=true&bdifs=1";
                    url += "&id" + GetMid(html, "&id", "\"");
                    html = GetHTTPPage(url);

                    if (html.Length > 0)
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
                else
                {
                    if (onScanResultEvent != null)
                    {
                        onScanResultEvent(false, _id);
                    }
                }
            }

            private String GetMid(String input, String s, String e)
            {
                int pos = input.IndexOf(s);
                if (pos == -1)
                {
                    return "";
                }

                pos += s.Length;

                int pos_end = 0;
                if (e == "")
                {
                    pos_end = input.Length;
                }
                else
                {
                    pos_end = input.IndexOf(e, pos);
                }

                if (pos_end == -1)
                {
                    return "";
                }

                return input.Substring(pos, pos_end - pos);
            }

            private CookieContainer CC = new CookieContainer();

            private void BugFix_CookieDomain(CookieContainer cookieContainer)
            {
                System.Type _ContainerType = typeof(CookieContainer);
                Hashtable table = (Hashtable)_ContainerType.InvokeMember("m_domainTable",
                                           System.Reflection.BindingFlags.NonPublic |
                                           System.Reflection.BindingFlags.GetField |
                                           System.Reflection.BindingFlags.Instance,
                                           null,
                                           cookieContainer,
                                           new object[] { });
                ArrayList keys = new ArrayList(table.Keys);
                foreach (string keyObj in keys)
                {
                    string key = (keyObj as string);
                    if (key[0] == '.')
                    {
                        string newKey = key.Remove(0, 1);
                        table[newKey] = table[keyObj];
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
                    request.CookieContainer = CC;
                    request.KeepAlive = false;
                    request.AllowAutoRedirect = true;
                    response = (HttpWebResponse)request.GetResponse();
                    BugFix_CookieDomain(CC);
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

        private int _count = 0; //扫描个数
        private int _tmp = 0;   //计数器，当前扫描了几个

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button1.Text = "扫描中...";
            ThreadPool.SetMaxThreads(150, 512); //最大线程150
            ServicePointManager.DefaultConnectionLimit = 512;   //HTTP最大并发数  
            int begin = int.Parse(textBox1.Text);
            int end = int.Parse(textBox2.Text);
            _count = end - begin + 1;
            _tmp = 0;
            for (int i = begin; i <= end; i++ )
            {
                scanId scan = new scanId(i, new scanId.scanResultDelegate(scanResult));
                ThreadPool.QueueUserWorkItem(new WaitCallback(scan.run));
            }
        }

        private void textBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            String temp = "====扫描范围:" + textBox1.Text + "~" + textBox2.Text + "====\r\n";
            Clipboard.SetDataObject(temp + textBox3.Text.Replace("扫描结束", ""));
            MessageBox.Show("已复制到剪切板");
        }
        
    }
}
