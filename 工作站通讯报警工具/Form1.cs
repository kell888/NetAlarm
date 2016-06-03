using System;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using System.Configuration;
using System.IO;
using System.Media;
using System.Collections.Generic;

namespace 工作站通讯报警工具
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SoundPlayer player;
        string interval = ConfigurationManager.AppSettings["interval"];
        const string KeyName = "NetworkDisconnected";
        private bool loadOver;
        private bool sure;
        static Form1 instance;

        public static Form1 GetInstance()
        {
            if (instance == null || instance.IsDisposed)
            {
                instance = new Form1();
            }
            if (!instance.Visible)
                instance.Show();
            instance.WindowState = FormWindowState.Normal;
            instance.BringToFront();
            instance.Focus();
            return instance;
        }

        private void Test()
        {
            string ips = ConfigurationManager.AppSettings["IPAddrs"];
            if (string.IsNullOrEmpty(ips))
            {
                MessageBox.Show("尚未配置工作站IP！请系统管理员配置。");
                return;
            }
            string ipAddrs = ips.ToString();
            string[] ipss = ipAddrs.Split('|');
            if (ipss.Length == 0)
            {
                MessageBox.Show("尚未配置工作站IP！请系统管理员配置。");
                return;
            }
            else
            {
                notifyIcon1.Icon = 工作站通讯报警工具.Properties.Resources.connect;
                System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                for (int i = 0; i < ipss.Length; i++)
                {
                    string ipAddr = ipss[i];
                    int timeout = 2000;
                    string pingTimeout = ConfigurationManager.AppSettings["pingTimeout"];
                    if (!string.IsNullOrEmpty(pingTimeout))
                        timeout = int.Parse(pingTimeout) * 1000;
                    System.Net.NetworkInformation.PingReply pr = ping.Send(ipAddr, timeout);//ping超时
                    if (pr.Status != System.Net.NetworkInformation.IPStatus.Success)
                    {
                        Alarm(ipAddr);
                        notifyIcon1.Icon = 工作站通讯报警工具.Properties.Resources.disconnect;
                    }
                }
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (loadOver)
            {
                UpdateLoop(checkBox2.Checked);
            }
        }

        private void UpdateLoop(bool loop)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(Application.ExecutablePath + ".config");
            XmlNode xNode;
            XmlElement xElem;
            xNode = xDoc.SelectSingleNode("//appSettings");
            xElem = (XmlElement)xNode.SelectSingleNode("//add[@key='loop']");
            xElem.SetAttribute("value", loop ? "1" : "0");
            xDoc.Save(Application.ExecutablePath + ".config");
        }
        bool fault;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!fault)
            {
                PWD pwd = new PWD();
                if (pwd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    MessageBox.Show("要输入授权码才能操作！");
                    fault = true;
                    checkBox1.Checked = !checkBox1.Checked;
                    fault = false;
                    return;
                }
                string keyValue = Application.ExecutablePath;
                if (checkBox1.Checked)
                {
                    if (!IsExistKey(KeyName))
                        WriteKey(KeyName, keyValue);
                }
                else
                {
                    DeleteKey(KeyName);
                }
            }
        }
        //判断是否已经存在此键值,此处可以在Form_Load中来使用。
        //如果存在，[开机启动]前面可以打上对钩
        //如果不存在，[开机启动]前面可以去掉钩
        private bool IsExistKey(string keyName)
        {
            bool _exist = false;

            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey runs = hklm.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            //注意此处用的是GetValueNames()
            string[] runsName = runs.GetValueNames();
            foreach (string strName in runsName)
            {
                if (strName.ToUpper() == keyName.ToUpper())
                {
                    _exist = true;
                    return _exist;
                }
            }

            return _exist;
        }

        //写入键值
        private bool WriteKey(string keyName, string keyValue)
        {
            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey run = hklm.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //将我们的程序加进去
                run.SetValue(keyName, keyValue);

                //注意，一定要关闭，注册表应用。
                hklm.Close();

                return true;
            }
            catch //这是捕获异常的 
            {
                return false;
            }
        }

        //删除键值
        private void DeleteKey(string keyName)
        {
            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey runs = hklm.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            try
            {
                //注意此处用的是GetValueNames()
                string[] runsName = runs.GetValueNames();
                foreach (string strName in runsName)
                {
                    if (strName.ToUpper() == keyName.ToUpper())
                        runs.DeleteValue(strName, false);
                }
            }
            catch { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sure)
            {
                PWD pwd = new PWD();
                if (pwd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    MessageBox.Show("要输入授权码才能操作！");
                    sure = false;
                    e.Cancel = true;
                    return;
                }
                if (MessageBox.Show("确定要退出报警工具吗？", "退出提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    notifyIcon1.Dispose();
                    Environment.Exit(0);
                }
                else
                {
                    sure = false;
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
            }
        }

        private void Alarm(string ipAddr)
        {
            this.button1.Text = "确认报警(" + ipAddr + "已断开)";
            ShowUI();
            player.Stop();
            if (!File.Exists(player.SoundLocation))
            {
                player.SoundLocation = Application.StartupPath + "\\disconnected.wav";
            }
            if (!File.Exists(player.SoundLocation))
            {
                if (MessageBox.Show("系统找不到disconnected.wav报警声音文件！请联系管理员。" + Environment.NewLine + "或者您可以自行指定一个声音文件(*.wav)来作为报警声" + Environment.NewLine + "【确定】选定一个声音文件" + Environment.NewLine + "【取消】联系管理员来处理", "选定声音", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "声音文件(*.wav)|*.wav";
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.Copy(ofd.FileName, Application.StartupPath + "\\disconnected.wav", true);
                        player.SoundLocation = Application.StartupPath + "\\disconnected.wav";
                        if (checkBox2.Checked)
                            player.PlayLooping();
                        else
                            player.Play();
                    }
                    else
                    {
                        MessageBox.Show("您不指定disconnected.wav声音文件，无法通过声音来进行报警！请尽快联系到系统管理员，以免因为没有报警声音而导致无法及时报警！");
                    }
                }
                else
                {
                    MessageBox.Show("请尽快联系到系统管理员，以免因为没有报警声音而导致无法及时报警！");
                }
            }
            else
            {
                if (checkBox2.Checked)
                    player.PlayLooping();
                else
                    player.Play();
            }
        }

        private void 退出报警工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sure = true;
            this.Close();
        }

        private void 显示工具界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowUI();
        }

        private void ShowUI()
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.BringToFront();
            this.Focus();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowUI();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            player.Stop();
            button1.Text = "确认报警";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = IsExistKey(KeyName);
            string loop = ConfigurationManager.AppSettings["loop"];
            if (!string.IsNullOrEmpty(loop) && loop == "1")
            {
                checkBox2.Checked = true;
            }
            else
            {
                checkBox2.Checked = false;
            }
            loadOver = true;
            if (!File.Exists(Application.StartupPath + "\\disconnected.wav"))
            {
                if (MessageBox.Show("系统找不到报警声音文件！请联系管理员。" + Environment.NewLine + "或者您可以自行指定一个声音文件(*.wav)来作为报警声" + Environment.NewLine + "【确定】选定一个声音文件" + Environment.NewLine + "【取消】联系管理员来处理", "选定声音", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "声音文件(*.wav)|*.wav";
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        File.Copy(ofd.FileName, Application.StartupPath + "\\disconnected.wav", true);
                    }
                    else
                    {
                        MessageBox.Show("您不指定disconnected.wav声音文件，无法通过声音来进行报警！请尽快联系到系统管理员，以免因为没有报警声音而导致无法及时报警！");
                    }
                }
                else
                {
                    MessageBox.Show("请尽快联系到系统管理员，以免因为没有报警声音而导致无法及时报警！");
                }
            }
            player = new SoundPlayer(Application.StartupPath + "\\disconnected.wav");
            int inter = 10000;
            if (!string.IsNullOrEmpty(interval))
            {
                int RET;
                if (int.TryParse(interval, out RET))
                    inter = RET * 1000;
            }
            timer1.Interval = inter;
            timer1.Start();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!fault)
            {
                if (EnableAlarm(checkBox3))
                {
                    checkBox1.Enabled = checkBox2.Enabled = checkBox3.Checked;
                }
                else
                {
                    fault = true;
                    checkBox3.Checked = !checkBox3.Checked;
                    fault = false;
                }
            }
        }

        private bool EnableAlarm(object o)
        {
            PWD pwd = new PWD();
            if (pwd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("要输入授权码才能操作！");
                return false;
            }
            if (o is CheckBox)
            {
                CheckBox cb = o as CheckBox;
                if (cb.Checked)
                {
                    timer1.Start();
                    启用声音报警ToolStripMenuItem.Text = "禁用声音报警";
                }
                else
                {
                    timer1.Stop();
                    启用声音报警ToolStripMenuItem.Text = "启用声音报警";
                }
            }
            else if (o is ToolStripMenuItem)
            {
                ToolStripMenuItem t = o as ToolStripMenuItem;
                if (t.Text == "启用声音报警")
                {
                    timer1.Start();
                    button1.Text = t.Text = "禁用声音报警";
                }
                else
                {
                    timer1.Stop();
                    button1.Text = t.Text = "启用声音报警";
                }
            }
            return true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (this.InvokeRequired)
                this.Invoke(new MethodInvoker(Test));
            else
                Test();
            timer1.Start();
        }
    }
}
