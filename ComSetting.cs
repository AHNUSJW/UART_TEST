using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ComAssist
{
    public partial class ComSetting : Form
    {
        public ComSetting()
        {
            InitializeComponent();
        }

        public Boolean isSave;
        public SerialPort myCOM;

        private void button1_Click(object sender, EventArgs e)
        {
            //
            myCOM.BaudRate = Convert.ToInt32(BAUT.B115200); //波特率
            myCOM.DataBits = Convert.ToInt32("8"); //数据位
            myCOM.StopBits = StopBits.One; //停止位
            myCOM.Parity = Parity.None; //校验位
            //
            comboBox1.Text = myCOM.BaudRate.ToString();
            comboBox2.Text = myCOM.DataBits.ToString();
            comboBox3.SelectedIndex = (Byte)myCOM.StopBits - 1;
            comboBox4.SelectedIndex = (Byte)myCOM.Parity;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            myCOM.BaudRate = Convert.ToInt32(comboBox1.Text);
            myCOM.DataBits = Convert.ToInt32(comboBox2.Text);
            myCOM.StopBits = (StopBits)comboBox3.SelectedIndex + 1;
            myCOM.Parity = (Parity)comboBox4.SelectedIndex;
            isSave = true;
            this.Close();
        }

        private void ComSetting_Load(object sender, EventArgs e)
        {
            isSave = false;
            comboBox1.Text = myCOM.BaudRate.ToString();
            comboBox2.Text = myCOM.DataBits.ToString();
            comboBox3.SelectedIndex = (Byte)myCOM.StopBits - 1;
            comboBox4.SelectedIndex = (Byte)myCOM.Parity;
        }
    }
}
