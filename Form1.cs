using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MyMap
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            SetWebBrowserFeatures(11);
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            webBrowser1.Navigate(Application.StartupPath + "/map.html");
            webBrowser1.ObjectForScripting = this;

            DataTable dt = Scaler.Access.GetRecords("select * from m_merchant order by m_addr", 0, 0);
            foreach (DataRow dr in dt.Rows)
            {
                listView1.Items.Add(new ListViewItem(new string[] { dr["id"].ToString(), dr["m_name"].ToString(), dr["m_addr"].ToString().Replace("山东省","").Replace(dr["m_area"].ToString(), "").TrimStart('市') }));
            }
        }
        public void Test(string message)
        {
            MessageBox.Show(message, "client code");
        }
        public string json()
        {
            return "ddddddddddddddddddd";
        }

        private void btn_import_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel格式文件|*.xls;*.xlsx";
            string path = Config.Read("LastOpen");
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                openFileDialog.InitialDirectory = new DirectoryInfo(path).FullName;
            }
            openFileDialog.RestoreDirectory = false;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                path = openFileDialog.FileName;
                Config.Write("LastOpen", path);
                if (!string.IsNullOrEmpty(path))
                {
                    MerchantImport(path);
                    listView1.Items.Clear();

                    DataTable dt = Scaler.Access.GetRecords("select * from m_merchant order by m_addr", 0, 0);
                    foreach (DataRow dr in dt.Rows)
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { dr["id"].ToString(), dr["m_name"].ToString(), dr["m_addr"].ToString().Replace("山东省", "").Replace(dr["m_area"].ToString(), "").TrimStart('市') }));
                    }
                }
            }
        }

        public string list_in_map(string x1, string x2, string y1, string y2)
        {
            //string sql = "select m_name, m_lat, m_lng, id from m_merchant m where m_lng>=" + x1 + " And m_lng<=" + x2 + " And m_lat>=" + y1 + " And m_lat<=" + y2;
            string sql = "select m_name, m_lat, m_lng, id from m_merchant";
            DataTable dt = Scaler.Access.GetRecords(sql, 0, 0);

            StringBuilder sb = new StringBuilder();
            if (dt != null && dt.Rows.Count > 0)
            {
                sb.Append("{\"count\":").Append(dt.Rows.Count).Append(", \"data\":[");
                foreach (DataRow dr in dt.Rows)
                {
                    //sb.Append("{\"m_name\":\"").Append(dr["m_name"]).Append("(").Append(dr["m_term_num"]).Append("台)\",\"ID\":").Append(dr["id"]).Append(",\"m_pos\":\"").Append(dr["pos"]).Append("\",\"lat\":").Append(dr["m_map_lat"]).Append(",\"lng\":").Append(dr["m_map_lng"]).Append("},");
                    sb.Append("{\"m_name\":\"").Append(dr["m_name"]).Append("\",\"ID\":").Append(dr["id"]).Append(",\"lat\":").Append(dr["m_lat"]).Append(",\"lng\":").Append(dr["m_lng"]).Append("},");
                }
                sb = sb.Remove(sb.Length - 1, 1);
                sb.Append("]}");
            }
            else
            {
                sb.Append("{\"count\":").Append(0).Append(", \"data\":[]}");
            }
            return sb.ToString();
        }

        private void update_status(int i, int total)
        {
            if (i == total)
            {
                toolStripStatusLabel2.Text = "已导入" + total;
            }
            else
            {
                toolStripStatusLabel2.Text = string.Format("正在导入 {0}/{1}", i, total);
            }
            Application.DoEvents();
        }
        private string MerchantImport(string path)
        {
            string strConn;
            strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path +
                      ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=1;\";";
            var conn = new System.Data.OleDb.OleDbConnection(strConn);
            var bOpen = true;
            try
            {
                conn.Open();
            }
            catch
            {
                bOpen = false;
            }

            if (!bOpen)
            {
                try
                {
                    strConn = "Provider = Microsoft.Ace.OleDb.12.0; Persist Security Info = False; " + "data source = " + path + "; Extended Properties = 'Excel 12.0; HDR=yes; IMEX=1'";
                    conn = new System.Data.OleDb.OleDbConnection(strConn);
                    conn.Open();
                }
                catch (Exception err)
                {
                    return string.Format("Excel表打开失败，{0}", err.Message.ToString());
                }
            }
            DataTable sheetNames = conn.GetOleDbSchemaTable
                (System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            string Sql = "select * from [" + sheetNames.Rows[0][2] + "]";
            var myCommand = new System.Data.OleDb.OleDbCommand();
            myCommand.Connection = conn;
            myCommand.CommandText = Sql;
            var adapter = new System.Data.OleDb.OleDbDataAdapter(myCommand);
            var dset = new DataSet();
            adapter.Fill(dset);
            conn.Close();
            DataTable dt = dset.Tables[0];
            int iok = 0;

            string[] fileds = new string[] { "商户名", "地区", "区县", "地址" };
            foreach (string field in fileds)
            {
                if (!dt.Columns.Contains(field))
                {
                    return string.Format("Excel表缺少“{0}”列", field);
                }
            }
            int i = 0, total = dt.Rows.Count;
            foreach (DataRow dr in dt.Rows)
            {
                i++;
                string m_name = dr["商户名"].ToString().Trim();
                m_name = string.IsNullOrEmpty(dr["商户名"].ToString().Trim()) ? "未知" : m_name;
                string area = dr["地区"].ToString().Trim();
                string dist = dr["区县"].ToString().Trim();
                string address = dr["地址"].ToString().Trim().Replace("山东省", "");
                if (!string.IsNullOrEmpty(area))
                {
                    address = address.Replace(area, "");
                }
                if (!string.IsNullOrEmpty(dist))
                {
                    address = address.Replace(dist, "");
                }
                address = address.Replace("#N/A", "").Trim();

                if (!string.IsNullOrEmpty(address))
                {
                    NameValueCollection nv = new NameValueCollection();
                    nv["m_name"] = m_name;
                    nv["m_area"] = area;
                    nv["m_dist"] = dist;
                    nv["m_addr"] = "山东省" + area + dist + address;
                    string[] latlng = getLatLng("山东省" + area + address);
                    if (latlng != null)
                    {
                        nv["m_lat"] = latlng[0];
                        nv["m_lng"] = latlng[1];
                    }
                    Scaler.Access.ExecuteSql(Scaler.MyForm.GetInsertSQL("m_merchant", nv));
                    update_status(i, total);
                }
                else
                    continue;
            }
            return "ok" + iok + "条记录已导入！";

        }

        public string[] getLatLng(string stress)
        {
            Thread.Sleep(1000);
            Scaler.Http http = new Scaler.Http();
            string url = "http://apis.map.qq.com/jsapi?qt=geoc&addr=" + Scaler.Common.StrEncode(stress) + "&output=jsonp&pf=jsapi&ref=jsapi&cb=qq.maps._svcb3.geocoder0";
            string content = http.GetHTML_WithEncode(url, "", "", "", "GET", Encoding.UTF8, Encoding.Default);

            int l = content.IndexOf("(");
            if (l > -1)
                content = content.Substring(l + 1);
            if (content.EndsWith(")"))
            {
                content = content.Remove(content.Length - 1, 1);
            }
            try
            {
                Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(content);
                Newtonsoft.Json.Linq.JToken objValue;
                if (obj.TryGetValue("status", out objValue))
                {
                    return null;
                }

                string lng = obj["detail"]["pointx"].ToString();
                string lat = obj["detail"]["pointy"].ToString();

                return new string[] { lat, lng };
            }
            catch
            {
                return null;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确实要删除全部标注", "确定删除？", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Scaler.Access.ExecuteSql("delete from m_merchant");
                listView1.Items.Clear();
            }
        }
        public void listviewitem_active(string id)
        {
            foreach (ListViewItem li in listView1.Items)
            {
                if (li.SubItems[0].Text == id)
                {
                    listView1.EnsureVisible(li.Index);
                    listView1.Focus();
                    li.Selected = true;
                    li.Focused = true;
                    break;
                }
            }
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            webBrowser1.Document.InvokeScript("findLabel", new string[] { listView1.SelectedItems[0].SubItems[0].Text });
        }

        private void splitter1_DragDrop(object sender, DragEventArgs e)
        {
            MessageBox.Show("ddd");
        }

        private void splitter1_Move(object sender, EventArgs e)
        {
            panel1.Location = new Point(splitter1.Location.X + splitter1.Width, 0);
            webBrowser1.Width = splitter1.Location.X - webBrowser1.Location.X;
        }


        /// <summary>  
        /// 修改注册表信息来兼容当前程序  
        ///   
        /// </summary>  
        static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            //不晓得设置有什么用  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);


            //Registry.SetValue(featureControlRegKey + "FEATURE_AJAX_CONNECTIONEVENTS",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_WEBOC_DOCUMENT_ZOOM",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_NINPUT_LEGACYMODE",  
            //    appName, 0, RegistryValueKind.DWord);  
        }
        /// <summary>  
        /// 获取浏览器的版本  
        /// </summary>  
        /// <returns></returns>  
        static int GetBrowserVersion()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            //如果小于7  
            if (browserVersion < 7)
            {
                throw new ApplicationException("不支持的浏览器版本!");
            }
            return browserVersion;
        }
        /// <summary>  
        /// 通过版本得到浏览器模式的值  
        /// </summary>  
        /// <param name="browserVersion"></param>  
        /// <returns></returns>  
        static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }
    }
}
