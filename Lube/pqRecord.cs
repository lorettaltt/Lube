using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using INIFILE;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data.SQLite;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Lube
{
    public partial class pqRecord : Form
    {
        SerialPort sp1 = new SerialPort();
        //sp1.ReceivedBytesThreshold = 1;//只要有1个字符送达端口时便触发DataReceived事件 
        string line = "";
        string temp = "";

        // 申明要使用的dll和api
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        private System.Diagnostics.Process softKey;

        public pqRecord()
        {
            InitializeComponent();
        }
       
        SQLiteDataReader dataReader;
        SQLiteConnection conn = SqliteHelper.GetConnection();
        #region ListView初始化状态
        private void Fill()
        {

            ListViewItem lviTestInfo = new ListViewItem();
            lviTestInfo.SubItems.Clear();

            lviTestInfo.SubItems[0].Text = dataReader["pqId"].ToString();
            lviTestInfo.SubItems.Add(dataReader["eqpId"].ToString());
            lviTestInfo.SubItems.Add(dataReader["operator"].ToString());
            lviTestInfo.SubItems.Add(dataReader["sampleId"].ToString());
            lviTestInfo.SubItems.Add(dataReader["pqValue"].ToString());
            lviTestInfo.SubItems.Add(dataReader["date"].ToString());

            lvTestInfoSearch.Items.Add(lviTestInfo);

        }
        private void FormState()
        {

            lvTestInfoSearch.Items.Clear();
            string sql = "select * from DB_PqInfo";

            try
            {

                dataReader = SqliteHelper.ExecuteReader(sql);
                while (dataReader.Read())
                {
                    Fill();

                }

                dataReader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                dataReader.Close();
                SQLiteConnection conn = SqliteHelper.GetConnection();
                conn.Close();
            }

        }

        #endregion
        //加载
        private void Form1_Load(object sender, EventArgs e)
        {
            FormState();
            INIFILE.Profile.LoadProfile();//加载所有

            // 预置波特率
            switch (Profile.G_BAUDRATE)
            {
                case "300":
                    cbBaudRate.SelectedIndex = 0;
                    break;
                case "600":
                    cbBaudRate.SelectedIndex = 1;
                    break;
                case "1200":
                    cbBaudRate.SelectedIndex = 2;
                    break;
                case "2400":
                    cbBaudRate.SelectedIndex = 3;
                    break;
                case "4800":
                    cbBaudRate.SelectedIndex = 4;
                    break;
                case "9600":
                    cbBaudRate.SelectedIndex = 5;
                    break;
                case "19200":
                    cbBaudRate.SelectedIndex = 6;
                    break;
                case "38400":
                    cbBaudRate.SelectedIndex = 7;
                    break;
                case "115200":
                    cbBaudRate.SelectedIndex = 8;
                    break;
                default:
                    {
                        MessageBox.Show("波特率预置参数错误。");
                        return;
                    }
            }

            //预置波特率
            switch (Profile.G_DATABITS)
            {
                case "5":
                    cbDataBits.SelectedIndex = 0;
                    break;
                case "6":
                    cbDataBits.SelectedIndex = 1;
                    break;
                case "7":
                    cbDataBits.SelectedIndex = 2;
                    break;
                case "8":
                    cbDataBits.SelectedIndex = 3;
                    break;
                default:
                    {
                        MessageBox.Show("数据位预置参数错误。");
                        return;
                    }

            }
            //预置停止位
            switch (Profile.G_STOP)
            {
                case "1":
                    cbStop.SelectedIndex = 0;
                    break;
                case "1.5":
                    cbStop.SelectedIndex = 1;
                    break;
                case "2":
                    cbStop.SelectedIndex = 2;
                    break;
                default:
                    {
                        MessageBox.Show("停止位预置参数错误。");
                        return;
                    }
            }

            //预置校验位
            switch (Profile.G_PARITY)
            {
                case "NONE":
                    cbParity.SelectedIndex = 0;
                    break;
                case "ODD":
                    cbParity.SelectedIndex = 1;
                    break;
                case "EVEN":
                    cbParity.SelectedIndex = 2;
                    break;
                default:
                    {
                        MessageBox.Show("校验位预置参数错误。");
                        return;
                    }
            }

            //检查是否含有串口
            string[] str = SerialPort.GetPortNames();
            if (str == null)
            {
                MessageBox.Show("本机没有串口！", "Error");
                return;
            }

            //添加串口项目
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {//获取有多少个COM口
                //System.Diagnostics.Debug.WriteLine(s);
                cbSerial.Items.Add(s);
            }

            //串口设置默认选择项
            cbSerial.SelectedIndex = 1;         //note：获得COM9口，但别忘修改
            cbBaudRate.SelectedIndex = 5;
            // cbDataBits.SelectedIndex = 3;
            // cbStop.SelectedIndex = 0;
            //  cbParity.SelectedIndex = 0;
            sp1.BaudRate = 9600;

            Control.CheckForIllegalCrossThreadCalls = false;    //这个类中我们不检查跨线程的调用是否合法(因为.net 2.0以后加强了安全机制,，不允许在winform中直接跨线程访问控件的属性)
            sp1.DataReceived += new SerialDataReceivedEventHandler(sp1_DataReceived);
            //sp1.ReceivedBytesThreshold = 1;
           // string name=UserHelper.loginName;

            //准备就绪              
            sp1.DtrEnable = true;
            sp1.RtsEnable = true;
            //设置数据读取超时为1秒
            sp1.ReadTimeout = 1000;

            sp1.Close();
        }

        void sp1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dt = DateTime.Now.ToShortDateString();
            // DateTime dt2;
            if (sp1.IsOpen)     //此处可能没有必要判断是否打开串口，但为了严谨性，我还是加上了
            {
                //输出当前时间
                //dt = DateTime.Now;
                txtReceive.Text = "";
                //txtReceive.Text += dt.GetDateTimeFormats('f')[0].ToString() + "\r\n";
                txtReceive.SelectAll();
                txtReceive.SelectionColor = Color.Blue;         //改变字体的颜色

                byte[] byteRead = new byte[sp1.BytesToRead];    //BytesToRead:sp1接收的字符个数

                sp1.Read(byteRead, 0, byteRead.Length); //注意：回车换行必须这样写，单独使用"\r"和"\n"都不会有效果
                                                        //这是用以显示字符串
                string strRcv = null;
                for (int i = 0; i < byteRead.Length; i++)
                {
                    strRcv += ((char)Convert.ToInt32(byteRead[i]));
                }

                txtReceive.Text += strRcv + "\r\n";             //显示信息
                //txtReceive.Text = "4,10:45,28/01/2017,SRM-201701-426+00012";
                String[] re = new String[10];
                String[] temp = new String[100];
                // String[] key = { "batch_id", "time", "date", "sample", "result" };
                string test = txtReceive.Text.Trim();
                String[] str1 = Regex.Split(test, ",", RegexOptions.IgnoreCase);
                //result.Text = str1.Length.ToString();
                re[0] = str1[0];        // batchId
                String[] str2 = str1[3].Split('+');
                re[1] = str2[0];        //SampleID
                re[2] = str2[1];       //value

                string sql = String.Format(@"insert into DB_PqInfo(eqpId,operator,sampleId,pqValue,date) values ('{0}','{1}','{2}','{3}','{4}')", re[0].Trim(), UserHelper.loginName, re[1], re[2], dt);
                try
                {
                    int result2 = SqliteHelper.ExecuteSql(sql);

                    if (result2 > 0)
                    {
                        //MessageBox.Show("添加记录成功！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                    else
                    {
                        MessageBox.Show("添加记录失败！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();

                }

            }
            else
            {
                MessageBox.Show("请打开某个串口", "错误提示");
            }
        }

        //退出按钮
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //关闭时事件
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            INIFILE.Profile.SaveProfile();
            sp1.Close();
        }

        //开关按钮
        private void btnSwitch_Click_1(object sender, EventArgs e)
        {

            //serialPort1.IsOpen
            if (!sp1.IsOpen)
            {
                try
                {
                    //设置串口号
                    string serialName = cbSerial.SelectedItem.ToString();
                    sp1.PortName = serialName;

                    //设置各“串口设置”
                    string strBaudRate = cbBaudRate.Text;
                    string strDateBits = cbDataBits.Text;
                    string strStopBits = cbStop.Text;
                    Int32 iBaudRate = Convert.ToInt32(strBaudRate);
                    Int32 iDateBits = Convert.ToInt32(strDateBits);

                    sp1.BaudRate = iBaudRate;       //波特率
                    sp1.DataBits = iDateBits;       //数据位
                    switch (cbStop.Text)            //停止位
                    {
                        case "1":
                            sp1.StopBits = StopBits.One;
                            break;
                        case "1.5":
                            sp1.StopBits = StopBits.OnePointFive;
                            break;
                        case "2":
                            sp1.StopBits = StopBits.Two;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }
                    switch (cbParity.Text)             //校验位
                    {
                        case "无":
                            sp1.Parity = Parity.None;
                            break;
                        case "奇校验":
                            sp1.Parity = Parity.Odd;
                            break;
                        case "偶校验":
                            sp1.Parity = Parity.Even;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }

                    if (sp1.IsOpen == true)//如果打开状态，则先关闭一下
                    {
                        sp1.Close();
                    }
                    //状态栏设置
                    tsSpNum.Text = "串口号：" + sp1.PortName + "|";
                    tsBaudRate.Text = "波特率：" + sp1.BaudRate + "|";
                    tsDataBits.Text = "数据位：" + sp1.DataBits + "|";
                    tsStopBits.Text = "停止位：" + sp1.StopBits + "|";
                    tsParity.Text = "校验位：" + sp1.Parity + "|";

                    //设置必要控件不可用
                    cbSerial.Enabled = false;
                    cbBaudRate.Enabled = false;
                    cbDataBits.Enabled = false;
                    cbStop.Enabled = false;
                    cbParity.Enabled = false;

                    sp1.Open();     //打开串口
                    btnSwitch.Text = "关闭串口";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error:" + ex.Message, "Error");
                    tmSend.Enabled = false;
                    return;
                }
            }
            else
            {
                //状态栏设置
                tsSpNum.Text = "串口号：未指定|";
                tsBaudRate.Text = "波特率：未指定|";
                tsDataBits.Text = "数据位：未指定|";
                tsStopBits.Text = "停止位：未指定|";
                tsParity.Text = "校验位：未指定|";
                //恢复控件功能
                //设置必要控件不可用
                cbSerial.Enabled = true;
                cbBaudRate.Enabled = true;
                cbDataBits.Enabled = true;
                cbStop.Enabled = true;
                cbParity.Enabled = true;

                sp1.Close();                    //关闭串口
                btnSwitch.Text = "打开串口";
                tmSend.Enabled = false;         //关闭计时器
            }
        }

        private void btnSave_Click_1(object sender, EventArgs e)
        {
            //设置各“串口设置”
            string strBaudRate = cbBaudRate.Text;
            string strDateBits = cbDataBits.Text;
            string strStopBits = cbStop.Text;
            Int32 iBaudRate = Convert.ToInt32(strBaudRate);
            Int32 iDateBits = Convert.ToInt32(strDateBits);

            Profile.G_BAUDRATE = iBaudRate + "";       //波特率
            Profile.G_DATABITS = iDateBits + "";       //数据位
            switch (cbStop.Text)            //停止位
            {
                case "1":
                    Profile.G_STOP = "1";
                    break;
                case "1.5":
                    Profile.G_STOP = "1.5";
                    break;
                case "2":
                    Profile.G_STOP = "2";
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }
            switch (cbParity.Text)             //校验位
            {
                case "无":
                    Profile.G_PARITY = "NONE";
                    break;
                case "奇校验":
                    Profile.G_PARITY = "ODD";
                    break;
                case "偶校验":
                    Profile.G_PARITY = "EVEN";
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }

            //保存设置
            // public static string G_BAUDRATE = "1200";//给ini文件赋新值，并且影响界面下拉框的显示
            //public static string G_DATABITS = "8";
            //public static string G_STOP = "1";
            //public static string G_PARITY = "NONE";
            Profile.SaveProfile();
        }

        private void rbRcvStr_CheckedChanged(object sender, EventArgs e)
        {

        }


        //开关按钮
        private void btnSwitch_Click(object sender, EventArgs e)
        {
            //serialPort1.IsOpen
            if (!sp1.IsOpen)
            {
                try
                {
                    //设置串口号
                    string serialName = cbSerial.SelectedItem.ToString();
                    sp1.PortName = serialName;

                    //设置各“串口设置”
                    string strBaudRate = cbBaudRate.Text;
                    string strDateBits = cbDataBits.Text;
                    string strStopBits = cbStop.Text;
                    Int32 iBaudRate = Convert.ToInt32(strBaudRate);
                    Int32 iDateBits = Convert.ToInt32(strDateBits);

                    sp1.BaudRate = iBaudRate;       //波特率
                    sp1.DataBits = iDateBits;       //数据位
                    switch (cbStop.Text)            //停止位
                    {
                        case "1":
                            sp1.StopBits = StopBits.One;
                            break;
                        case "1.5":
                            sp1.StopBits = StopBits.OnePointFive;
                            break;
                        case "2":
                            sp1.StopBits = StopBits.Two;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }
                    switch (cbParity.Text)             //校验位
                    {
                        case "无":
                            sp1.Parity = Parity.None;
                            break;
                        case "奇校验":
                            sp1.Parity = Parity.Odd;
                            break;
                        case "偶校验":
                            sp1.Parity = Parity.Even;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }

                    if (sp1.IsOpen == true)//如果打开状态，则先关闭一下
                    {
                        sp1.Close();
                    }
                    //状态栏设置
                    tsSpNum.Text = "串口号：" + sp1.PortName + "|";
                    tsBaudRate.Text = "波特率：" + sp1.BaudRate + "|";
                    tsDataBits.Text = "数据位：" + sp1.DataBits + "|";
                    tsStopBits.Text = "停止位：" + sp1.StopBits + "|";
                    tsParity.Text = "校验位：" + sp1.Parity + "|";

                    //设置必要控件不可用
                    cbSerial.Enabled = false;
                    cbBaudRate.Enabled = false;
                    cbDataBits.Enabled = false;
                    cbStop.Enabled = false;
                    cbParity.Enabled = false;

                    sp1.Open();     //打开串口
                    btnSwitch.Text = "关闭串口";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error:" + ex.Message, "Error");
                    tmSend.Enabled = false;
                    return;
                }
            }
            else
            {
                //状态栏设置
                tsSpNum.Text = "串口号：未指定|";
                tsBaudRate.Text = "波特率：未指定|";
                tsDataBits.Text = "数据位：未指定|";
                tsStopBits.Text = "停止位：未指定|";
                tsParity.Text = "校验位：未指定|";
                //恢复控件功能
                //设置必要控件不可用
                cbSerial.Enabled = true;
                cbBaudRate.Enabled = true;
                cbDataBits.Enabled = true;
                cbStop.Enabled = true;
                cbParity.Enabled = true;

                sp1.Close();                    //关闭串口
                btnSwitch.Text = "打开串口";
                tmSend.Enabled = false;         //关闭计时器
            }
        }

        private void btnClear_Click_1(object sender, EventArgs e)
        {
            txtReceive.Text = "";       //清空文本
        }

        private void btnExit_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void txtSecond_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            string patten = "[0-9]|\b"; //“\b”：退格键
            Regex r = new Regex(patten);
            Match m = r.Match(e.KeyChar.ToString());

            if (m.Success)
            {
                e.Handled = false;   //没操作“过”，系统会处理事件    
            }
            else
            {
                e.Handled = true;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            FormState();
        }

        private void pqRecord_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private bool VaildataInput()
        {
            //if (testSN.Text == "")
            //{
            //    MessageBox.Show("请输入序列号！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            //    return false;
            //}
            //else if (testNo.Text == "")
            //{
            //    MessageBox.Show("请输入测试编号！", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            //    return false;
            //}

            return true;
        }


        private void button1_Click(object sender, EventArgs e)
        {/*
          //  SQLiteDataReader dataReader;
            if (VaildataInput())
            {
                if (batchId.Text != "" && time1.Text == "" && time2.Text == "" && date.Text == "" && sample.Text == "")
                {

                    string sql = String.Format(@"select * from DB_PqInfo where eqpId = '{0}'", batchId.Text.Trim());
                    
                    //SQLiteConnection conn = SqliteHelper.GetConnection();
                    dataReader = SqliteHelper.ExecuteReader(sql);
                    lvTestInfoSearch.Items.Clear();
                    while (dataReader.Read())
                    {
                        Fill();
                    }
                    dataReader.Close();
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();
                }
            }
            if (VaildataInput())
            {
                if (sample.Text != "")
                {
                    string sql = String.Format(@"select * from DB_PqInfo where sampleId= '{0}'", sample.Text);

                    dataReader = SqliteHelper.ExecuteReader(sql);
                    lvTestInfoSearch.Items.Clear();
                    while (dataReader.Read())
                    {
                        Fill();
                    }
                    dataReader.Close();
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();
                }
            }
            if (VaildataInput())
            {
                if (time1.Text != "" && time2.Text != "")
                {


                    DateTime dt = Convert.ToDateTime(time1.Text);
                    DateTime dt2 = Convert.ToDateTime(time2.Text);

                    string sql = String.Format(@"select * from DB_PqInfo where testTime between '{0}' and '{1}'", dt.ToString("t"), dt2.ToString("t"));

                    dataReader = SqliteHelper.ExecuteReader(sql);
                    lvTestInfoSearch.Items.Clear();
                    while (dataReader.Read())
                    {
                        Fill();
                    }
                    dataReader.Close();
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();
                }
            }
            if (VaildataInput())
            {
                if (time1.Text != "" && time2.Text != "" && date.Text != "")
                {
                    DateTime dt = Convert.ToDateTime(time1.Text);
                    DateTime dt2 = Convert.ToDateTime(time2.Text);


                    DateTime date2 = Convert.ToDateTime(date.Text);

                    string sql = String.Format(@"select * from DB_PqInfo where testDate = '{0}'and testTime between '{1}' and '{2}' ", date2.ToString("d"), dt.ToString("t"), dt2.ToString("t"));

                    dataReader = SqliteHelper.ExecuteReader(sql);
                    lvTestInfoSearch.Items.Clear();
                    while (dataReader.Read())
                    {
                        Fill();
                    }
                    dataReader.Close();
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();
                }
            }
            if (VaildataInput())
            {
                if (time1.Text == "" && time2.Text == "" && date.Text != "")
                {
                    DateTime date2 = Convert.ToDateTime(date.Text);

                    string sql = String.Format(@"select * from DB_PqInfo where testDate = '{0}'", date2.ToString("d"));

                    dataReader = SqliteHelper.ExecuteReader(sql);
                    lvTestInfoSearch.Items.Clear();
                    while (dataReader.Read())
                    {
                        Fill();
                    }
                    dataReader.Close();
                    SQLiteConnection conn = SqliteHelper.GetConnection();
                    conn.Close();
                }
            }
            */
         //多条件查询
            StringBuilder sql = new StringBuilder("select * from DB_PqInfo");
            List<string> wheres = new List<string>();
            if (batchId.Text != "")
            {
                wheres.Add(" eqpId like '%" + batchId.Text.Trim() + "%'");
            }
            if (sample.Text != "")
            {
                wheres.Add(" sampleId like '%" + sample.Text.Trim() + "%'");
            }

            if (Convert.ToDateTime(dateTimePicker1.Value.ToShortDateString()) > Convert.ToDateTime(dateTimePicker2.Value.ToShortDateString()))
            {
                MessageBox.Show("后面的日期不应该大于前面的日期");
            }
            else
            {
                if (dateTimePicker1.Value.ToShortDateString() != "" && dateTimePicker2.Value.ToShortDateString() != "")
                {
                    wheres.Add(" date between '" + dateTimePicker1.Value.ToShortDateString() + "' and '" + dateTimePicker2.Value.ToShortDateString() + "'");
                }
                else if (dateTimePicker1.Value.ToShortDateString() != "" && dateTimePicker2.Value.ToShortDateString() == "")
                {
                    wheres.Add(" date like '%" + dateTimePicker1.Value.ToShortDateString() + "%'");
                }
                else if (dateTimePicker1.Value.ToShortDateString() == "" && dateTimePicker2.Value.ToShortDateString() != "")
                {
                    wheres.Add(" date like '%" + dateTimePicker2.Value.ToShortDateString() + "%'");
                }
                //判断用户是否选择了条件
                if (wheres.Count > 0)
                {
                    string wh = string.Join(" and ", wheres.ToArray());
                    sql.Append(" where " + wh);
                }
                dataReader = SqliteHelper.ExecuteReader(sql.ToString());
                lvTestInfoSearch.Items.Clear();
                while (dataReader.Read())
                {
                    Fill();
                }
                dataReader.Close();
                SQLiteConnection conn = SqliteHelper.GetConnection();
                conn.Close();
            }



        }

        private void btnExp_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            FileStream fs;
            StreamWriter sw;
            DateTime dt = DateTime.Now;
            string name = dt.ToString();

            //文件命名(暂定)
            string name1 = lvTestInfoSearch.Items[0].SubItems[3].Text.Trim() + Guid.NewGuid().ToString();

            sfd.FileName = name1;
            //sfd.FileName = "hello";
            //sfd.Filter = "(*.txt)|*.txt|(*.*)|*.*";
            sfd.Filter = "CSV|*.csv";
            //CSV|*.csv
            sfd.AddExtension = true;
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {

                if (sfd.FileName.Contains(name))
                {
                    fs = new FileStream(sfd.FileName, FileMode.Append, FileAccess.Write);

                    //FileStream fs = new FileStream(sfd.FileName,FileMode.Create);
                    sw = new StreamWriter(fs);
                }
                else
                {
                    fs = new FileStream(sfd.FileName, FileMode.CreateNew, FileAccess.Write);
                    sw = new StreamWriter(fs);
                }

                try
                {
                    //string line = "";
                    //string temp = "";
                    ////int len = 0;
                    //for (int i = 0; i < lvTestInfoSearch.Items.Count; i++)
                    //{
                    //    for (int j = 0; j < lvTestInfoSearch.Items[i].SubItems.Count; j++)
                    //    {
                    //        temp = lvTestInfoSearch.Items[i].SubItems[j].Text.Trim();
                    //        //len = 3 - Encoding.Default.GetByteCount(temp) + temp.Length;
                    //        //temp = temp.PadRight(len, ' ');
                    //        line += temp;
                    //        line = line + ";";
                    //    }
                    // lvTestInfoSearch_SelectedIndexChanged(lvTestInfoSearch,e);
                    for (int i = 0; i < lvTestInfoSearch.Columns.Count; i++)
                    {
                        temp = lvTestInfoSearch.Columns[i].ToString().Split(':')[2].Split(' ')[1];
                        line += temp;
                        line = line + ";";
                    }
                    line = line + ";"+"\n\n";
                    int index = lvTestInfoSearch.Items.IndexOf(lvTestInfoSearch.FocusedItem);//获取被选中项的索引
                    for (int j = 0; j < lvTestInfoSearch.Items[index].SubItems.Count; j++)
                    {
                        temp = lvTestInfoSearch.Items[index].SubItems[j].Text.Trim();
                        line += temp;
                        line = line + ";";
                    }
                    line = line + ";";
                    //        //len = 3 - Encoding.Default.GetByteCount(temp) + temp.Length;
                    //        //temp = temp.PadRight(len, ' ');

                    sw.WriteLine(line);
                    line = "";
                    string localFilePath = sfd.FileName.ToString();
                    //textBox1.Text = localFilePath;

                    sw.Flush();
                    //}

                    MessageBox.Show("保存成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
                finally
                {
                    sw.Close();
                    fs.Close();
                }
            }
        }

        private void lvTestInfoSearch_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //打开软键盘
            try
            {
                if (!System.IO.File.Exists(Environment.SystemDirectory + "\\osk.exe"))
                {
                    MessageBox.Show("软件盘可执行文件不存在！");
                    return;
                }


                softKey = System.Diagnostics.Process.Start("C:\\Windows\\System32\\osk.exe");
                // 上面的语句在打开软键盘后，系统还没用立刻把软键盘的窗口创建出来了。所以下面的代码用循环来查询窗口是否创建，只有创建了窗口
                // FindWindow才能找到窗口句柄，才可以移动窗口的位置和设置窗口的大小。这里是关键。
                IntPtr intptr = IntPtr.Zero;
                while (IntPtr.Zero == intptr)
                {
                    System.Threading.Thread.Sleep(100);
                    intptr = FindWindow(null, "屏幕键盘");
                }


                // 获取屏幕尺寸
                int iActulaWidth = Screen.PrimaryScreen.Bounds.Width;
                int iActulaHeight = Screen.PrimaryScreen.Bounds.Height;


                // 设置软键盘的显示位置，底部居中
                int posX = (iActulaWidth - 1000) / 2;
                int posY = (iActulaHeight - 300);


                //设定键盘显示位置
                MoveWindow(intptr, posX, posY, 1000, 300, true);


                //设置软键盘到前端显示
                SetForegroundWindow(intptr);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
           
        //}
    }
}
