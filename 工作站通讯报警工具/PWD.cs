using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace 工作站通讯报警工具
{
    public partial class PWD : Form
    {
        public PWD()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pwd = ConfigurationManager.AppSettings["pwd"];
            if (!string.IsNullOrEmpty(pwd))
            {
                if (textBox1.Text != pwd)
                {
                    MessageBox.Show("授权码错误！请重试。");
                    textBox1.Focus();
                    textBox1.SelectAll();
                    return;
                }
                else
                {
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                }
            }
        }
    }
}
