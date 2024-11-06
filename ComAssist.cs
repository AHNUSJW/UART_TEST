using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ComAssist
{
    public partial class ComAssist : Form
    {
        private delegate void freshHandler();//定义委托
        private event freshHandler myUpdate;//定义事件

        private PAGE myPage = PAGE.NORM; //工具模式
        private MODE myMode = MODE.SIGNLE; //发送模式用
        private Boolean comBusy = false; //串口忙碌
        private StringBuilder sbRX = new StringBuilder(); //接收缓冲

        private Int32 index = 0; //循环发送的指针
        private List<TxBullet> myBullet = new List<TxBullet>(); //循环处理
        private TxBytes myBytes = new TxBytes(); //编码模式的发送处理
        private Byte canDataLen = 8; //默认的CAN数据长度

        private StringBuilder sbReceiverTab1 = new StringBuilder(); //接收到的所有数据
        private StringBuilder sbReceiverTab2 = new StringBuilder(); //接收到的所有数据
        private StringBuilder sbReceiverTab3 = new StringBuilder(); //接收到的所有数据
        private Int32 txCounterTab1 = 0; //发送计数用
        private Int32 txCounterTab2 = 0; //发送计数用
        private Int32 txCounterTab3 = 0; //发送计数用

        private Boolean isClosing = false; //串口正在关闭标记
        private Boolean isListening = false; //串口正在监听标记

        private XET myXET = new XET();

        public ComAssist()
        {
            InitializeComponent();
        }

        //String转Hex
        private string ConvertStringToHex(string strASCII, string separator = null)
        {
            StringBuilder sbHex = new StringBuilder();
            foreach (char chr in strASCII)
            {
                sbHex.Append(String.Format("{0:X2}", Convert.ToInt32(chr)));
                sbHex.Append(separator ?? string.Empty);
            }
            return sbHex.ToString();
        }

        //Hex转String
        private string ConvertHexToString(string HexValue, string separator = null)
        {
            HexValue = string.IsNullOrEmpty(separator) ? HexValue : HexValue.Replace(separator, string.Empty);
            StringBuilder sbStrValue = new StringBuilder();
            while (HexValue.Length > 0)
            {
                try
                {
                    sbStrValue.Append(Convert.ToChar(Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString());
                }
                catch
                {
                    return sbStrValue.ToString();
                }
                HexValue = HexValue.Substring(2);
            }
            return sbStrValue.ToString();
        }

        //保存函数
        private void SaveSerialPortSetting()
        {
            FileStream meFS = new FileStream("com.ini", FileMode.OpenOrCreate, FileAccess.Write);
            TextWriter meWrite = new StreamWriter(meFS);
            meWrite.WriteLine("PortName=" + serialPort1.PortName.ToString());
            meWrite.WriteLine("BaudRate=" + serialPort1.BaudRate.ToString());
            meWrite.WriteLine("DataBits=" + serialPort1.DataBits.ToString());
            meWrite.WriteLine("StopBits=" + serialPort1.StopBits.ToString());
            meWrite.WriteLine("Parity=" + serialPort1.Parity.ToString());
            meWrite.Close();
            meFS.Close();
        }

        //打开串口
        private void SerialPortOpen()
        {
            try
            {
                serialPort1.Open();
                labelStatus.Text = serialPort1.PortName + " is opened";
                OpenToolStripMenuItem.BackColor = Color.Green;
                OpenToolStripMenuItem.Text = "关 闭";
                toolStripComboBox1.Enabled = false;
            }
            catch
            {
                timer1.Enabled = false;

                labelStatus.Text = serialPort1.PortName + " cannot open";
                OpenToolStripMenuItem.BackColor = Color.Firebrick;
                OpenToolStripMenuItem.Text = "打 开";
                toolStripComboBox1.Enabled = true;

                myMode = MODE.SIGNLE;
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
            }
        }

        //关闭串口
        private void SerialPortClose()
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    timer1.Enabled = false;

                    //取消异步任务
                    //https://www.cnblogs.com/wucy/p/15128365.html
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.Cancel();

                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();

                    //串口在收发字节时关闭会死锁,
                    //https://blog.csdn.net/guomei1345/article/details/80736721
                    //https://blog.csdn.net/sinat_23338865/article/details/52596818
                    serialPort1.Close();

                    labelStatus.Text = serialPort1.PortName + " is closed";
                    OpenToolStripMenuItem.BackColor = Color.Firebrick;
                    OpenToolStripMenuItem.Text = "打 开";
                    toolStripComboBox1.Enabled = true;

                    myMode = MODE.SIGNLE;
                    button9.BackColor = button11.BackColor;
                    button10.BackColor = button11.BackColor;
                    button16.BackColor = button11.BackColor;
                }
                catch
                {
                    labelStatus.Text = serialPort1.PortName + " cannot close";
                    OpenToolStripMenuItem.BackColor = Color.Firebrick;
                    OpenToolStripMenuItem.Text = "关 闭";
                    toolStripComboBox1.Enabled = true;
                }
            }
        }

        //刷新按钮
        private void FreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //刷串口
            toolStripComboBox1.Items.Clear();
            toolStripComboBox1.Items.AddRange(SerialPort.GetPortNames());
            //无串口
            if (toolStripComboBox1.Items.Count == 0)
            {
                toolStripComboBox1.Text = "";
            }
            //有可用串口
            else
            {
                toolStripComboBox1.Text = serialPort1.PortName;
                if (toolStripComboBox1.SelectedIndex < 0)
                {
                    //
                    toolStripComboBox1.SelectedIndex = 0;

                    SerialPortClose();
                    //
                    serialPort1.PortName = toolStripComboBox1.Text;
                    SaveSerialPortSetting();
                }
            }
        }

        //打开按钮
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (toolStripComboBox1.Text != "")
            {
                if (serialPort1.IsOpen == false)
                {
                    isClosing = false;
                    serialPort1.PortName = toolStripComboBox1.Text;
                    SerialPortOpen();
                }
                else
                {
                    //断开串口时，isClosing标记更新
                    isClosing = true;

                    //处理当前在消息队列中的所有 Windows 消息
                    //防止界面停止响应
                    //https://blog.csdn.net/sinat_23338865/article/details/52596818
                    while (isListening)
                    {
                        Application.DoEvents();
                    }

                    //关闭串口
                    SerialPortClose();
                    isClosing = false;
                }
            }
            else
            {
                FreshToolStripMenuItem_Click(null, null);
            }
        }

        //设置
        private void SetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ComSetting myComSetting = new ComSetting();
            //
            {
                myComSetting.myCOM = new SerialPort();
                myComSetting.myCOM.BaudRate = serialPort1.BaudRate;
                myComSetting.myCOM.DataBits = serialPort1.DataBits;
                myComSetting.myCOM.StopBits = serialPort1.StopBits;
                myComSetting.myCOM.Parity = serialPort1.Parity;
            }
            //
            myComSetting.StartPosition = FormStartPosition.CenterParent;
            myComSetting.ShowDialog();
            //
            if (myComSetting.isSave)
            {
                if (serialPort1.IsOpen)
                {
                    //断开串口时，isClosing标记更新
                    isClosing = true;

                    //处理当前在消息队列中的所有 Windows 消息
                    //防止界面停止响应
                    //https://blog.csdn.net/sinat_23338865/article/details/52596818
                    while (isListening)
                    {
                        Application.DoEvents();
                    }

                    //关闭串口
                    SerialPortClose();
                    isClosing = false;

                    serialPort1.BaudRate = myComSetting.myCOM.BaudRate;
                    serialPort1.DataBits = myComSetting.myCOM.DataBits;
                    serialPort1.StopBits = myComSetting.myCOM.StopBits;
                    serialPort1.Parity = myComSetting.myCOM.Parity;
                    SerialPortOpen();
                }
                else
                {
                    serialPort1.BaudRate = myComSetting.myCOM.BaudRate;
                    serialPort1.DataBits = myComSetting.myCOM.DataBits;
                    serialPort1.StopBits = myComSetting.myCOM.StopBits;
                    serialPort1.Parity = myComSetting.myCOM.Parity;
                }
                //
                SaveSerialPortSetting();
            }
        }

        byte[] myGetBytes(String str)
        {
            UInt16 i = 0;

            Byte[] mbt = new Byte[str.Length];

            foreach (Byte x in str)
            {
                mbt[i++] = x;
            }

            return mbt;
        }

        //
        private void textBox_crc_update(object sender, EventArgs e)
        {
            String temp;
            Boolean XHXL;

            //无校验
            if (comboBox_crc.SelectedIndex == XET.XX_null)
            {
                textBox_crc.Text = "";
                textBox_crc.BackColor = button11.BackColor;
                return;
            }

            //取得字符串
            if (radioButton3.Checked)
            {
                if (textBoxTab1_hex.TextLength > 0)
                {
                    temp = ConvertHexToString(textBoxTab1_hex.Text, " ");
                }
                else
                {
                    textBox_crc.Text = "";
                    textBox_crc.BackColor = button11.BackColor;
                    return;
                }
            }
            else
            {
                if (textBoxTab1_ascii.TextLength > 0)
                {
                    temp = textBoxTab1_ascii.Text;
                }
                else
                {
                    textBox_crc.Text = "";
                    textBox_crc.BackColor = button11.BackColor;
                    return;
                }
            }

            //转成字节
            Byte[] mb = myGetBytes(temp);

            //高低位
            if (comboBox_XHXL.SelectedIndex == 0)
            {
                XHXL = true;
            }
            else
            {
                XHXL = false;
            }

            //校验码
            switch (comboBox_crc.SelectedIndex)
            {
                case XET.XX_LRC:
                    textBox_crc.Text = myXET.AP_LRC(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_BCC:
                    textBox_crc.Text = myXET.AP_BCC(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_XMODEN:
                    textBox_crc.Text = myXET.AP_CRC16_XMODEN(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_MODBUS:
                    textBox_crc.Text = myXET.AP_CRC16_MODBUS(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_MAXIM:
                    textBox_crc.Text = myXET.AP_CRC16_MAXIM(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_X25:
                    textBox_crc.Text = myXET.AP_CRC16_X25(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_IBM:
                    textBox_crc.Text = myXET.AP_CRC16_IBM(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_CCITT:
                    textBox_crc.Text = myXET.AP_CRC16_CCITT(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_CCITT_FALSE:
                    textBox_crc.Text = myXET.AP_CRC16_CCITT_FALSE(mb, mb.Length, XHXL).ToString("X4");
                    break;
                case XET.XX_USB:
                    textBox_crc.Text = myXET.AP_CRC16_USB(mb, mb.Length, XHXL).ToString("X4");
                    break;
            }

            //
            textBox_crc.BackColor = Color.LightGreen;
        }

        //
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //myPage
            switch (tabControl1.SelectedIndex)
            {
                default:
                case 0: myPage = PAGE.NORM; break;
                case 1: myPage = PAGE.CODE; break;
                case 2: myPage = PAGE.CAN; break;
            }
        }

        //初始化加载
        private void ComAssist_Load(object sender, EventArgs e)
        {
            //
            comboBox_XHXL.SelectedIndex = 0;
            comboBox_crc.SelectedIndex = 0;

            //
            radioButton1.Checked = true;
            radioButton3.Checked = true;
            radioButton6.Checked = true;
            button15_Click(null, null);
            button18_Click(null, null);
            button24_Click(null, null);
            button26_Click(null, null);
            button28_Click(null, null);
            button21_Click(null, null);
            myUpdate += update_ReceiveBox;

            //默认初始化
            serialPort1.PortName = "COM18";
            serialPort1.BaudRate = Convert.ToInt32(BAUT.B115200); //波特率
            serialPort1.DataBits = Convert.ToInt32("8"); //数据位
            serialPort1.StopBits = StopBits.One; //停止位
            serialPort1.Parity = Parity.None; //校验位
            serialPort1.ReceivedBytesThreshold = 1; //接收即通知
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

            //读取文件或创建文件
            if (File.Exists("com.ini"))
            {
                String[] meLines = File.ReadAllLines("com.ini");
                foreach (String line in meLines)
                {
                    if (line.Length > 6)
                    {
                        switch (line.Substring(0, line.IndexOf('=')))
                        {
                            case "PortName": serialPort1.PortName = Convert.ToString(line.Substring(line.IndexOf('=') + 1)); break;
                            case "BaudRate": serialPort1.BaudRate = Convert.ToInt32(line.Substring(line.IndexOf('=') + 1)); break;
                            case "DataBits": serialPort1.DataBits = Convert.ToInt32(line.Substring(line.IndexOf('=') + 1)); break;
                            case "StopBits": serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), line.Substring(line.IndexOf('=') + 1)); break;
                            case "Parity": serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), line.Substring(line.IndexOf('=') + 1)); break;
                        }
                    }
                }
            }
            else
            {
                SaveSerialPortSetting();
            }

            //刷新串口
            FreshToolStripMenuItem_Click(null, null);
        }

        //
        private void ComAssist_FormClosing(object sender, FormClosingEventArgs e)
        {
            //串口发送停止
            if (serialPort1.IsOpen)
            {
                //断开串口时，isClosing标记更新
                isClosing = true;

                //处理当前在消息队列中的所有 Windows 消息
                //防止界面停止响应
                //https://blog.csdn.net/sinat_23338865/article/details/52596818
                while (isListening)
                {
                    Application.DoEvents();
                }

                //关闭串口
                SerialPortClose();
                isClosing = false;
            }
        }

        //串口通讯响应
        private void update_ReceiveBox()
        {
            //其它线程的操作请求
            if (this.InvokeRequired)
            {
                try
                {
                    freshHandler meDelegate = new freshHandler(update_ReceiveBox);
                    this.Invoke(meDelegate, new object[] { });
                }
                catch
                {

                }
            }
            //本线程的操作请求
            else
            {
                switch (myPage)
                {
                    default:

                    case PAGE.NORM:
                        //高频、大量地构建字符串时，Append方法的性能高于直接使用+或+=
                        sbReceiverTab1.Append(sbRX);
                        //
                        if (radioButton1.Checked)
                        {
                            textBoxTab1_receiver.AppendText(ConvertStringToHex(sbRX.ToString(), " "));
                        }
                        else
                        {
                            textBoxTab1_receiver.AppendText(sbRX.ToString());
                        }
                        //
                        textBoxTab1_receiver.ScrollToCaret();
                        //
                        label1.Text = "Rx: " + sbReceiverTab1.Length.ToString();
                        //
                        Thread.Sleep(100);
                        comBusy = false;
                        break;

                    case PAGE.CODE:
                        //高频、大量地构建字符串时，Append方法的性能高于直接使用+或+=
                        sbReceiverTab2.Append(sbRX);
                        //
                        if (radioButton6.Checked)
                        {
                            textBoxTab2_receiver.AppendText(ConvertStringToHex(sbRX.ToString(), " "));
                        }
                        else
                        {
                            textBoxTab2_receiver.AppendText(sbRX.ToString());
                        }
                        //
                        textBoxTab2_receiver.ScrollToCaret();
                        //
                        label5.Text = "Rx: " + sbReceiverTab2.Length.ToString();
                        //
                        Thread.Sleep(100);
                        comBusy = false;
                        break;

                    case PAGE.CAN:
                        //高频、大量地构建字符串时，Append方法的性能高于直接使用+或+=
                        sbReceiverTab3.Append(sbRX);
                        //
                        if (comboBox_all_id.Text == "ALL ID")
                        {
                            String temp = ConvertStringToHex(sbRX.ToString(), " ");
                            if (textBoxTab3_receiver.TextLength > 3)
                            {
                                if (temp.Length > 3)
                                {
                                    if ((textBoxTab3_receiver.Text.Substring(textBoxTab3_receiver.TextLength - 3) == "0D ") && (temp.Substring(0, 3) == "0A "))
                                    {
                                        temp = temp.Insert(3, "\r\n");
                                    }
                                }
                            }
                            textBoxTab3_receiver.AppendText(temp.Replace("0D 0A ", "0D 0A \r\n"));
                            textBoxTab3_receiver.ScrollToCaret();
                        }
                        else
                        {
                            String id = ConvertHexToString(comboBox_all_id.Text.Replace("0x", ""));
                            switch ((Byte)sbRX[0])
                            {
                                case 0x50:
                                case 0x51:
                                case 0x52:
                                case 0x53:
                                case 0x54:
                                case 0x55:
                                case 0x56:
                                case 0x57:
                                case 0x58:
                                    if (sbRX.Length > 2)
                                    {
                                        if (sbRX.ToString().Substring(1, 2) == id)
                                        {
                                            textBoxTab3_receiver.AppendText(ConvertStringToHex(sbRX.ToString(), " ") + "\r\n");
                                        }
                                    }
                                    break;
                                case 0x60:
                                case 0x61:
                                case 0x62:
                                case 0x63:
                                case 0x64:
                                case 0x65:
                                case 0x66:
                                case 0x67:
                                case 0x68:
                                    if (sbRX.Length > 4)
                                    {
                                        if (sbRX.ToString().Substring(1, 4) == id)
                                        {
                                            textBoxTab3_receiver.AppendText(ConvertStringToHex(sbRX.ToString(), " ") + "\r\n");
                                        }
                                    }
                                    break;
                            }
                        }
                        //
                        label14.Text = "Rx: " + sbReceiverTab3.Length.ToString();
                        //
                        Thread.Sleep(100);
                        comBusy = false;
                        break;
                }
            }
        }

        //接收触发函数,实际会由串口线程创建
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (isClosing)
            {
                return;
            }

            try
            {
                //
                isListening = true;
                comBusy = true;

                sbRX.Remove(0, sbRX.Length);

                //读
                for (int i = serialPort1.BytesToRead; i > 0; i--)
                {
                    if (isClosing == false)
                    {
                        sbRX.Append((Char)serialPort1.ReadByte());
                    }
                    else
                        return;
                }

                //委托
                if (myUpdate != null)
                {
                    myUpdate();
                }
            }
            finally
            {
                isListening = false;
            }
        }

        //用Byte发送
        private void SerialPortWrite(String str)
        {
            Int32 i = 0;
            Byte[] buff = new byte[str.Length];
            foreach (char chr in str)
            {
                buff[i++] = (Byte)chr;
            }
            serialPort1.Write(buff, 0, str.Length);
        }

        // <summary>
        // 基本工具
        // </summary>

        //HEX字符
        private void byteTab1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar != 8) && (e.KeyChar != 0x20))
            {
                if (e.KeyChar < '0')
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > '9') && (e.KeyChar < 'A'))
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > 'F') && (e.KeyChar < 'a'))
                {
                    e.Handled = true;
                    return;
                }
                if (e.KeyChar > 'f')
                {
                    e.Handled = true;
                    return;
                }

                if (textBoxTab1_hex.TextLength > 0)
                {
                    if (textBoxTab1_hex.SelectionStart == textBoxTab1_hex.TextLength)
                    {
                        if (((textBoxTab1_hex.TextLength + 1) % 3) == 0)
                        {
                            textBoxTab1_hex.AppendText(" ");
                        }
                    }
                }
            }
        }

        //复制粘贴
        private void copyTab1_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C))
            {
                try
                {
                    Clipboard.SetText(((TextBox)sender).SelectedText.Trim()); //Ctrl+C 复制
                }
                catch (Exception)
                {
                    e.Handled = true;
                }
            }
            if (e.KeyData == (Keys.Control | Keys.V))
            {
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        string tmp = ConvertHexToString(Clipboard.GetText().Trim(), " ");
                        ((TextBox)sender).SelectedText = ConvertStringToHex(tmp, " "); //Ctrl+V 粘贴
                    }
                    catch (Exception)
                    {
                        e.Handled = true;
                    }
                }
            }
            if (e.KeyData == (Keys.Control | Keys.X))
            {
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        ((TextBox)sender).Cut();
                    }
                    catch (Exception)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        //周期输入
        private void digitTab1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (myMode != MODE.SIGNLE)
            {
                e.Handled = true;
                return;
            }

            //删除键
            if (e.KeyChar == 8)
            {
                return;
            }

            //只允许输入数字和删除键
            if ((e.KeyChar < '0') || (e.KeyChar > '9'))
            {
                e.Handled = true;
                return;
            }

            //限制长度
            if ((((TextBox)sender).TextLength > 6) && (((TextBox)sender).SelectionLength == 0))
            {
                e.Handled = true;
                return;
            }
        }

        //鼠标滚动
        private void textBoxTabx_receive_MouseWheel(object sender, MouseEventArgs e)
        {
            int maxJump = ((TextBox)sender).Size.Width;

            if (e.Delta > 0)
            {
                if (((TextBox)sender).SelectionStart > maxJump)
                {
                    ((TextBox)sender).SelectionStart -= maxJump;
                }
                else
                {
                    ((TextBox)sender).SelectionStart = 0;
                }
                ((TextBox)sender).ScrollToCaret();
                ((TextBox)sender).TabStop = false;
            }
            else
            {
                ((TextBox)sender).SelectionStart += maxJump;
                ((TextBox)sender).ScrollToCaret();
                ((TextBox)sender).TabStop = false;
            }
        }

        //清除接收
        private void button1_Click(object sender, EventArgs e)
        {
            sbRX.Remove(0, sbRX.Length);
            sbReceiverTab1.Remove(0, sbReceiverTab1.Length);
            textBoxTab1_receiver.Text = "";
            textBoxTab1_receiver.ScrollToCaret();
            label1.Text = "Rx: ";
        }

        //保存接收到文件
        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "文本文件(*.txt)|*.txt"; //设置要选择的文件的类型
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    File.Delete(fileDialog.FileName);
                }
                FileStream meFS = new FileStream(fileDialog.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                TextWriter meWrite = new StreamWriter(meFS);
                if (radioButton1.Checked)
                {
                    String myStr = textBoxTab1_receiver.Text;
                    while (myStr.Length > 66)
                    {
                        meWrite.WriteLine(myStr.Substring(0, 66));
                        myStr = myStr.Remove(0, 66);
                    }
                    meWrite.WriteLine(myStr);
                }
                else
                {
                    meWrite.Write(textBoxTab1_receiver.Text);
                }
                meWrite.Close();
                meFS.Close();
            }
        }

        //复制接收到剪贴板
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBoxTab1_receiver.TextLength > 0)
            {
                Clipboard.SetText(textBoxTab1_receiver.Text.Trim());
            }
        }

        //显示HEX
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                if (sbReceiverTab1.Length > 0)
                {
                    textBoxTab1_receiver.Text = ConvertStringToHex(sbReceiverTab1.ToString(), " ");
                }
            }
        }

        //显示ASCII
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                textBoxTab1_receiver.Text = sbReceiverTab1.ToString();
            }
        }

        //HEX转ASCII
        private void button4_Click(object sender, EventArgs e)
        {
            if (textBoxTab1_hex.TextLength > 0)
            {
                textBoxTab1_ascii.Text = ConvertHexToString(textBoxTab1_hex.Text, " ");
            }
        }

        //ASCII转HEX
        private void button5_Click(object sender, EventArgs e)
        {
            if (textBoxTab1_ascii.TextLength > 0)
            {
                textBoxTab1_hex.Text = ConvertStringToHex(textBoxTab1_ascii.Text, " ");
            }
        }

        //清除发送
        private void button6_Click(object sender, EventArgs e)
        {
            //
            textBoxTab1_hex.Text = "";
            textBoxTab1_hex.ScrollToCaret();
            //
            textBoxTab1_ascii.Text = "";
            textBoxTab1_ascii.ScrollToCaret();
            //
            textBox4.Text = "";
            textBox4.ScrollToCaret();
            //
            txCounterTab1 = 0;
            label2.Text = "Tx: ";

            //
            textBox_crc.Text = "";
        }

        //导入循环
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "文本文件(*.txt)|*.txt"; //设置要选择的文件的类型
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                String[] meLines = File.ReadAllLines(fileDialog.FileName, System.Text.Encoding.Default);
                Int16 num = 20;
                if (meLines[0] == "ComString:")
                {
                    foreach (String line in meLines)
                    {
                        if (line.Length > 6)
                        {
                            if (line == "ComString:")
                            {
                                myBullet.Clear();
                                continue;
                            }
                            if (line.IndexOf('=') < 0)
                            {
                                continue;
                            }
                            switch (line.Substring(0, line.IndexOf('=')))
                            {
                                case "BaudRate": serialPort1.BaudRate = Convert.ToInt32(line.Substring(line.IndexOf('=') + 1)); break;
                                case "DataBits": serialPort1.DataBits = Convert.ToInt32(line.Substring(line.IndexOf('=') + 1)); break;
                                case "StopBits": serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), line.Substring(line.IndexOf('=') + 1)); break;
                                case "Parity": serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), line.Substring(line.IndexOf('=') + 1)); break;
                                //
                                case "HEX0": myBullet.Add(new TxBullet(0, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX1": myBullet.Add(new TxBullet(1, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX2": myBullet.Add(new TxBullet(2, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX3": myBullet.Add(new TxBullet(3, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX4": myBullet.Add(new TxBullet(4, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX5": myBullet.Add(new TxBullet(5, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX6": myBullet.Add(new TxBullet(6, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX7": myBullet.Add(new TxBullet(7, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX8": myBullet.Add(new TxBullet(8, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX9": myBullet.Add(new TxBullet(9, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX10": myBullet.Add(new TxBullet(10, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX11": myBullet.Add(new TxBullet(11, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX12": myBullet.Add(new TxBullet(12, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX13": myBullet.Add(new TxBullet(13, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX14": myBullet.Add(new TxBullet(14, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX15": myBullet.Add(new TxBullet(15, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX16": myBullet.Add(new TxBullet(16, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX17": myBullet.Add(new TxBullet(17, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX18": myBullet.Add(new TxBullet(18, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                case "HEX19": myBullet.Add(new TxBullet(19, "HEX", line.Substring(line.IndexOf('=') + 1))); break;
                                //
                                case "ASC0": myBullet.Add(new TxBullet(0, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC1": myBullet.Add(new TxBullet(1, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC2": myBullet.Add(new TxBullet(2, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC3": myBullet.Add(new TxBullet(3, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC4": myBullet.Add(new TxBullet(4, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC5": myBullet.Add(new TxBullet(5, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC6": myBullet.Add(new TxBullet(6, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC7": myBullet.Add(new TxBullet(7, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC8": myBullet.Add(new TxBullet(8, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC9": myBullet.Add(new TxBullet(9, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC10": myBullet.Add(new TxBullet(10, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC11": myBullet.Add(new TxBullet(11, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC12": myBullet.Add(new TxBullet(12, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC13": myBullet.Add(new TxBullet(13, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC14": myBullet.Add(new TxBullet(14, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC15": myBullet.Add(new TxBullet(15, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC16": myBullet.Add(new TxBullet(16, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC17": myBullet.Add(new TxBullet(17, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC18": myBullet.Add(new TxBullet(18, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                case "ASC19": myBullet.Add(new TxBullet(19, "ASCII", line.Substring(line.IndexOf('=') + 1))); break;
                                //
                                default:
                                    if (line.Contains("HEX"))
                                    {
                                        myBullet.Add(new TxBullet(num, "HEX", line.Substring(line.IndexOf('=') + 1)));
                                        num++;
                                    }
                                    else if (line.Contains("ASC"))
                                    {
                                        myBullet.Add(new TxBullet(num, "ASCII", line.Substring(line.IndexOf('=') + 1)));
                                        num++;
                                    }
                                    break;
                            }
                        }
                    }

                    comboBox1.Items.Clear();
                    for (int i = 0; i < myBullet.Count; i++)
                    {
                        if (myBullet[i].Item < 10)
                        {
                            comboBox1.Items.Add("[ " + myBullet[i].Item.ToString() + "] " + myBullet[i].Text);
                        }
                        else
                        {
                            comboBox1.Items.Add("[" + myBullet[i].Item.ToString() + "] " + myBullet[i].Text);
                        }
                    }
                    comboBox1.SelectedIndex = 0;
                }
            }
        }

        //编辑循环
        private void button8_Click(object sender, EventArgs e)
        {
            ComString myComString = new ComString();
            //
            myComString.myBullet = this.myBullet;
            //
            myComString.StartPosition = FormStartPosition.CenterParent;
            myComString.ShowDialog();
            //
            this.myBullet = myComString.myBullet;
            if (myBullet.Count > 0)
            {
                comboBox1.Items.Clear();
                for (int i = 0; i < myBullet.Count; i++)
                {
                    if (myBullet[i].Item < 10)
                    {
                        comboBox1.Items.Add("[ " + myBullet[i].Item.ToString() + "] " + myBullet[i].Text);
                    }
                    else
                    {
                        comboBox1.Items.Add("[" + myBullet[i].Item.ToString() + "] " + myBullet[i].Text);
                    }
                }
                comboBox1.SelectedIndex = 0;
            }
        }

        //选择循环指令
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            String str1 = "\\r";
            String str2 = "\\n";
            String temp;
            Int32 idex;
            if (myBullet.Count > 0)
            {
                comboBox_XHXL.SelectedIndex = 0;
                comboBox_crc.SelectedIndex = 0;
                textBox_crc.Text = "";

                if (myBullet[comboBox1.SelectedIndex].isHEX)
                {
                    String str;
                    Boolean XHXL;
                    Byte crcType;

                    temp = myBullet[comboBox1.SelectedIndex].Text;

                    //确定高低位
                    if (temp.Contains("XH XL"))
                    {
                        str = "XH XL"; //****
                        XHXL = true;   //****
                        crcType = XET.XX_LRC;
                    }
                    else if (temp.Contains("XL XH"))
                    {
                        str = "XL XH"; //****
                        XHXL = false;  //****
                        crcType = XET.XX_LRC;
                    }
                    else
                    {
                        str = "XH XL";
                        XHXL = true;
                        crcType = XET.XX_null; //****
                    }

                    //确定校验方式
                    if (crcType > XET.XX_null)
                    {
                        if (temp.Contains("LRC")) crcType = XET.XX_LRC;
                        else if (temp.Contains("BCC")) crcType = XET.XX_BCC;
                        else if (temp.Contains("XMODEN")) crcType = XET.XX_XMODEN;
                        else if (temp.Contains("MODBUS")) crcType = XET.XX_MODBUS;
                        else if (temp.Contains("MAXIM")) crcType = XET.XX_MAXIM;
                        else if (temp.Contains("X25")) crcType = XET.XX_X25;
                        else if (temp.Contains("IBM")) crcType = XET.XX_IBM;
                        else if (temp.Contains("CCITT_FALSE")) crcType = XET.XX_CCITT_FALSE;//在CCITT之前
                        else if (temp.Contains("CCITT")) crcType = XET.XX_CCITT;
                        else if (temp.Contains("USB")) crcType = XET.XX_USB;
                        else crcType = XET.XX_null;
                    }

                    //删除注释
                    idex = temp.IndexOf("///");

                    //删除结尾
                    if (idex < 0)
                    {
                        temp = temp.Trim();
                    }
                    else
                    {
                        temp = temp.Substring(0, temp.IndexOf("///")).Trim();
                    }

                    //完成校验
                    if (crcType > XET.XX_null)
                    {
                        Byte[] mb = myGetBytes(ConvertHexToString(temp, " ").Replace(str, ""));

                        switch (crcType)
                        {
                            case XET.XX_LRC:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_LRC(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_BCC:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_BCC(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_XMODEN:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_XMODEN(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_MODBUS:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_MODBUS(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_MAXIM:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_MAXIM(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_X25:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_X25(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_IBM:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_IBM(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_CCITT:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_CCITT(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_CCITT_FALSE:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_CCITT_FALSE(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                            case XET.XX_USB:
                                textBoxTab1_hex.Text = temp.Replace(str, myXET.AP_CRC16_USB(mb, mb.Length, XHXL).ToString("X4").Insert(2, " "));
                                break;
                        }
                    }
                    else
                    {
                        textBoxTab1_hex.Text = temp;
                    }

                    radioButton3.Checked = true;
                }
                else
                {
                    temp = myBullet[comboBox1.SelectedIndex].Text;

                    idex = temp.IndexOf("///");

                    if (idex < 0)
                    {
                        textBoxTab1_ascii.Text = temp.Trim().Replace(str1, "\r").Replace(str2, "\n");
                    }
                    else
                    {
                        textBoxTab1_ascii.Text = temp.Substring(0, idex).Trim().Replace(str1, "\r").Replace(str2, "\n");
                    }

                    radioButton4.Checked = true;
                }
            }
        }

        //周期循环
        private void button9_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (serialPort1.IsOpen)
            {
                if (myMode == MODE.NEXT)
                {
                    myMode = MODE.SIGNLE;
                    button9.BackColor = button11.BackColor;
                }
                else
                {
                    if (myBullet.Count > 0)//有数据发送
                    {
                        //
                        index = 0;

                        //
                        if (textBox4.TextLength > 0)//有定时时间
                        {
                            //
                            button10.BackColor = button11.BackColor;
                            button16.BackColor = button11.BackColor;
                            //
                            myMode = MODE.NEXT;
                            button9.BackColor = Color.Green;
                            timer1.Interval = Convert.ToInt32(textBox4.Text);
                            timer1.Enabled = true;
                            //
                            timer1_Tick(null, null);
                        }
                    }
                }
            }
        }

        //周期发送
        private void button10_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (serialPort1.IsOpen)
            {
                if (myMode == MODE.REPEAT)
                {
                    myMode = MODE.SIGNLE;
                    button10.BackColor = button11.BackColor;
                }
                else
                {
                    if (radioButton3.Checked)
                    {
                        if (textBoxTab1_hex.TextLength > 0)//有数据发送
                        {
                            if (textBox4.TextLength > 0)//有定时时间
                            {
                                //
                                button9.BackColor = button11.BackColor;
                                button16.BackColor = button11.BackColor;
                                //
                                myMode = MODE.REPEAT;
                                button10.BackColor = Color.Green;
                                timer1.Interval = Convert.ToInt32(textBox4.Text);
                                timer1.Enabled = true;
                                //
                                timer1_Tick(null, null);
                            }
                        }
                    }
                    else
                    {
                        if (textBoxTab1_ascii.TextLength > 0)//有数据发送
                        {
                            if (textBox4.TextLength > 0)//有定时时间
                            {
                                //
                                button9.BackColor = button11.BackColor;
                                button16.BackColor = button11.BackColor;
                                //
                                myMode = MODE.REPEAT;
                                button10.BackColor = Color.Green;
                                timer1.Interval = Convert.ToInt32(textBox4.Text);
                                timer1.Enabled = true;
                                //
                                timer1_Tick(null, null);
                            }
                        }
                    }
                }
            }
        }

        //单发送
        private void button11_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (serialPort1.IsOpen)
            {
                if (radioButton3.Checked)
                {
                    if (textBoxTab1_hex.TextLength > 0)
                    {
                        button9.BackColor = button11.BackColor;
                        button10.BackColor = button11.BackColor;
                        button16.BackColor = button11.BackColor;
                        //
                        String temp = ConvertHexToString(textBoxTab1_hex.Text + textBox_crc.Text, " ");
                        txCounterTab1 += temp.Length;
                        label2.Text = "Tx: " + txCounterTab1.ToString();
                        SerialPortWrite(temp);
                    }
                }
                else
                {
                    if (textBoxTab1_ascii.TextLength > 0)
                    {
                        button9.BackColor = button11.BackColor;
                        button10.BackColor = button11.BackColor;
                        button16.BackColor = button11.BackColor;
                        //
                        String temp = textBoxTab1_ascii.Text + ConvertHexToString(textBox_crc.Text, null);
                        txCounterTab1 += temp.Length;
                        label2.Text = "Tx: " + txCounterTab1.ToString();
                        SerialPortWrite(temp);
                    }
                }
            }
        }

        //定时器
        private void timer1_Tick(object sender, EventArgs e)
        {
            if ((comBusy) || (serialPort1.BytesToRead > 0) || (serialPort1.BytesToWrite > 0))
            {
                return;
            }
            else
            {
                timer1.Enabled = false;
            }

            if (serialPort1.IsOpen)
            {
                String temp = "";
                Int32 idex;

                switch (myMode)
                {
                    case MODE.REPEAT:
                        if (radioButton3.Checked)
                        {
                            temp = ConvertHexToString(textBoxTab1_hex.Text + textBox_crc.Text, " ");
                        }
                        else
                        {
                            temp = textBoxTab1_ascii.Text + ConvertHexToString(textBox_crc.Text, null);
                        }
                        txCounterTab1 += temp.Length;
                        label2.Text = "Tx: " + txCounterTab1.ToString();
                        SerialPortWrite(temp);
                        timer1.Enabled = true;
                        break;
                    case MODE.NEXT:
                        //
                        comboBox_XHXL.SelectedIndex = 0;
                        comboBox_crc.SelectedIndex = 0;
                        textBox_crc.Text = "";
                        //
                        if (myBullet[index].isHEX)
                        {
                            String str;
                            Boolean XHXL;
                            Byte crcType;

                            temp = myBullet[index].Text;

                            //确定高低位
                            if (temp.Contains("XH XL"))
                            {
                                str = "XH XL"; //****
                                XHXL = true;   //****
                                crcType = XET.XX_LRC;
                            }
                            else if (temp.Contains("XL XH"))
                            {
                                str = "XL XH"; //****
                                XHXL = false;  //****
                                crcType = XET.XX_LRC;
                            }
                            else
                            {
                                str = "XH XL";
                                XHXL = true;
                                crcType = XET.XX_null; //****
                            }

                            //确定校验方式
                            if (crcType > XET.XX_null)
                            {
                                if (temp.Contains("LRC")) crcType = XET.XX_LRC;
                                else if (temp.Contains("BCC")) crcType = XET.XX_BCC;
                                else if (temp.Contains("XMODEN")) crcType = XET.XX_XMODEN;
                                else if (temp.Contains("MODBUS")) crcType = XET.XX_MODBUS;
                                else if (temp.Contains("MAXIM")) crcType = XET.XX_MAXIM;
                                else if (temp.Contains("X25")) crcType = XET.XX_X25;
                                else if (temp.Contains("IBM")) crcType = XET.XX_IBM;
                                else if (temp.Contains("CCITT_FALSE")) crcType = XET.XX_CCITT_FALSE;//在CCITT之前
                                else if (temp.Contains("CCITT")) crcType = XET.XX_CCITT;
                                else if (temp.Contains("USB")) crcType = XET.XX_USB;
                                else crcType = XET.XX_null;
                            }

                            //删除注释
                            idex = temp.IndexOf("///");

                            //删除结尾
                            if (idex < 0)
                            {
                                temp = temp.Trim();
                            }
                            else
                            {
                                temp = temp.Substring(0, temp.IndexOf("///")).Trim();
                            }

                            //完成校验
                            if (crcType > XET.XX_null)
                            {
                                Byte[] mb = myGetBytes(ConvertHexToString(temp, " ").Replace(str, ""));

                                switch (crcType)
                                {
                                    case XET.XX_LRC:
                                        temp = temp.Replace(str, myXET.AP_LRC(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_BCC:
                                        temp = temp.Replace(str, myXET.AP_BCC(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_XMODEN:
                                        temp = temp.Replace(str, myXET.AP_CRC16_XMODEN(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_MODBUS:
                                        temp = temp.Replace(str, myXET.AP_CRC16_MODBUS(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_MAXIM:
                                        temp = temp.Replace(str, myXET.AP_CRC16_MAXIM(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_X25:
                                        temp = temp.Replace(str, myXET.AP_CRC16_X25(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_IBM:
                                        temp = temp.Replace(str, myXET.AP_CRC16_IBM(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_CCITT:
                                        temp = temp.Replace(str, myXET.AP_CRC16_CCITT(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_CCITT_FALSE:
                                        temp = temp.Replace(str, myXET.AP_CRC16_CCITT_FALSE(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                    case XET.XX_USB:
                                        temp = temp.Replace(str, myXET.AP_CRC16_USB(mb, mb.Length, XHXL).ToString("X4"));
                                        break;
                                }
                            }

                            //
                            temp = ConvertHexToString(temp, " ");
                        }
                        else
                        {
                            temp = myBullet[index].Text;

                            idex = temp.IndexOf("///");

                            if (idex < 0)
                            {
                                temp = temp.Trim();
                            }
                            else
                            {
                                temp = temp.Substring(0, temp.IndexOf("///")).Trim();
                            }
                        }
                        //
                        if ((++index) >= myBullet.Count) index = 0;
                        //
                        txCounterTab1 += temp.Length;
                        label2.Text = "Tx: " + txCounterTab1.ToString();
                        //
                        textBoxTab1_hex.Text = ConvertStringToHex(temp, " ");
                        textBoxTab1_ascii.Text = temp.ToString();
                        //
                        SerialPortWrite(temp);
                        timer1.Enabled = true;
                        break;
                    case MODE.ACODE:
                        for (Int16 i = 0; i < myBytes.Count; i++)
                        {
                            temp += (char)myBytes.txBytes[i];
                        }
                        //
                        myBytes.AutoUpdate();
                        //
                        txCounterTab2 += temp.Length;
                        label8.Text = "Tx: " + txCounterTab2.ToString();
                        //
                        textBoxTab2_hex.Text = ConvertStringToHex(temp, "  ");
                        //
                        SerialPortWrite(temp);
                        timer1.Enabled = true;
                        break;
                }
            }
        }

        // <summary>
        // 编码
        // </summary>
        //

        //HEX字符
        private void byteTab2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (button16.BackColor == Color.Green)
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar != 8)
            {
                if (e.KeyChar < '0')
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > '9') && (e.KeyChar < 'A'))
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > 'F') && (e.KeyChar < 'a'))
                {
                    e.Handled = true;
                    return;
                }
                if (e.KeyChar > 'f')
                {
                    e.Handled = true;
                    return;
                }
            }

            if (((TextBox)sender).TextLength > 1)
            {
                //可替换
                if (((TextBox)sender).SelectionLength > 0)
                {
                    return;
                }
                //可删除
                if (e.KeyChar != 8)
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (((TextBox)sender).TextLength > 0)
            {
                switch (((TextBox)sender).Name)
                {
                    default:
                    case "textBox_bit00": textBox_bit01.Focus(); break;
                    case "textBox_bit01": textBox_bit02.Focus(); break;
                    case "textBox_bit02": textBox_bit03.Focus(); break;
                    case "textBox_bit03": textBox_bit04.Focus(); break;
                    case "textBox_bit04": textBox_bit05.Focus(); break;
                    case "textBox_bit05": textBox_bit06.Focus(); break;
                    case "textBox_bit06": textBox_bit07.Focus(); break;
                    case "textBox_bit07": textBox_bit08.Focus(); break;
                    case "textBox_bit08": textBox_bit09.Focus(); break;
                    case "textBox_bit09": textBox_bit10.Focus(); break;
                    case "textBox_bit10": textBox_bit11.Focus(); break;
                    case "textBox_bit11": textBox_bit12.Focus(); break;
                    case "textBox_bit12": textBox_bit13.Focus(); break;
                    case "textBox_bit13": textBox_bit14.Focus(); break;
                    case "textBox_bit14": textBox_bit15.Focus(); break;
                    case "textBox_bit15": button16.Focus(); break;
                }
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            sbRX.Remove(0, sbRX.Length);
            sbReceiverTab2.Remove(0, sbReceiverTab2.Length);
            textBoxTab2_receiver.Text = "";
            textBoxTab2_receiver.ScrollToCaret();
            label5.Text = "Rx: ";
        }

        private void button13_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "文本文件(*.txt)|*.txt"; //设置要选择的文件的类型
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    File.Delete(fileDialog.FileName);
                }
                FileStream meFS = new FileStream(fileDialog.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                TextWriter meWrite = new StreamWriter(meFS);
                if (radioButton1.Checked)
                {
                    String myStr = textBoxTab2_receiver.Text;
                    while (myStr.Length > 66)
                    {
                        meWrite.WriteLine(myStr.Substring(0, 66));
                        myStr = myStr.Remove(0, 66);
                    }
                    meWrite.WriteLine(myStr);
                }
                else
                {
                    meWrite.Write(textBoxTab2_receiver.Text);
                }
                meWrite.Close();
                meFS.Close();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (textBoxTab2_receiver.TextLength > 0)
            {
                Clipboard.SetText(textBoxTab2_receiver.Text.Trim());
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
                if (sbReceiverTab2.Length > 0)
                {
                    textBoxTab2_receiver.Text = ConvertStringToHex(sbReceiverTab2.ToString(), " ");
                }
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                textBoxTab2_receiver.Text = sbReceiverTab2.ToString();
            }
        }

        private void radioButtonTab2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_bit00_fix.Checked) myBytes.txMark[0] = MARK.FIX;
            else if (radioButton_bit00_up.Checked) myBytes.txMark[0] = MARK.UP;
            else if (radioButton_bit00_left.Checked) myBytes.txMark[0] = MARK.LEFT;
            else if (radioButton_bit00_right.Checked) myBytes.txMark[0] = MARK.RIGHT;

            if (radioButton_bit01_fix.Checked) myBytes.txMark[1] = MARK.FIX;
            else if (radioButton_bit01_up.Checked) myBytes.txMark[1] = MARK.UP;
            else if (radioButton_bit01_left.Checked) myBytes.txMark[1] = MARK.LEFT;
            else if (radioButton_bit01_right.Checked) myBytes.txMark[1] = MARK.RIGHT;

            if (radioButton_bit02_fix.Checked) myBytes.txMark[2] = MARK.FIX;
            else if (radioButton_bit02_up.Checked) myBytes.txMark[2] = MARK.UP;
            else if (radioButton_bit02_left.Checked) myBytes.txMark[2] = MARK.LEFT;
            else if (radioButton_bit02_right.Checked) myBytes.txMark[2] = MARK.RIGHT;

            if (radioButton_bit03_fix.Checked) myBytes.txMark[3] = MARK.FIX;
            else if (radioButton_bit03_up.Checked) myBytes.txMark[3] = MARK.UP;
            else if (radioButton_bit03_left.Checked) myBytes.txMark[3] = MARK.LEFT;
            else if (radioButton_bit03_right.Checked) myBytes.txMark[3] = MARK.RIGHT;

            if (radioButton_bit04_fix.Checked) myBytes.txMark[4] = MARK.FIX;
            else if (radioButton_bit04_up.Checked) myBytes.txMark[4] = MARK.UP;
            else if (radioButton_bit04_left.Checked) myBytes.txMark[4] = MARK.LEFT;
            else if (radioButton_bit04_right.Checked) myBytes.txMark[4] = MARK.RIGHT;

            if (radioButton_bit05_fix.Checked) myBytes.txMark[5] = MARK.FIX;
            else if (radioButton_bit05_up.Checked) myBytes.txMark[5] = MARK.UP;
            else if (radioButton_bit05_left.Checked) myBytes.txMark[5] = MARK.LEFT;
            else if (radioButton_bit05_right.Checked) myBytes.txMark[5] = MARK.RIGHT;

            if (radioButton_bit06_fix.Checked) myBytes.txMark[6] = MARK.FIX;
            else if (radioButton_bit06_up.Checked) myBytes.txMark[6] = MARK.UP;
            else if (radioButton_bit06_left.Checked) myBytes.txMark[6] = MARK.LEFT;
            else if (radioButton_bit06_right.Checked) myBytes.txMark[6] = MARK.RIGHT;

            if (radioButton_bit07_fix.Checked) myBytes.txMark[7] = MARK.FIX;
            else if (radioButton_bit07_up.Checked) myBytes.txMark[7] = MARK.UP;
            else if (radioButton_bit07_left.Checked) myBytes.txMark[7] = MARK.LEFT;
            else if (radioButton_bit07_right.Checked) myBytes.txMark[7] = MARK.RIGHT;

            if (radioButton_bit08_fix.Checked) myBytes.txMark[8] = MARK.FIX;
            else if (radioButton_bit08_up.Checked) myBytes.txMark[8] = MARK.UP;
            else if (radioButton_bit08_left.Checked) myBytes.txMark[8] = MARK.LEFT;
            else if (radioButton_bit08_right.Checked) myBytes.txMark[8] = MARK.RIGHT;

            if (radioButton_bit09_fix.Checked) myBytes.txMark[9] = MARK.FIX;
            else if (radioButton_bit09_up.Checked) myBytes.txMark[9] = MARK.UP;
            else if (radioButton_bit09_left.Checked) myBytes.txMark[9] = MARK.LEFT;
            else if (radioButton_bit09_right.Checked) myBytes.txMark[9] = MARK.RIGHT;

            if (radioButton_bit10_fix.Checked) myBytes.txMark[10] = MARK.FIX;
            else if (radioButton_bit10_up.Checked) myBytes.txMark[10] = MARK.UP;
            else if (radioButton_bit10_left.Checked) myBytes.txMark[10] = MARK.LEFT;
            else if (radioButton_bit10_right.Checked) myBytes.txMark[10] = MARK.RIGHT;

            if (radioButton_bit11_fix.Checked) myBytes.txMark[11] = MARK.FIX;
            else if (radioButton_bit11_up.Checked) myBytes.txMark[11] = MARK.UP;
            else if (radioButton_bit11_left.Checked) myBytes.txMark[11] = MARK.LEFT;
            else if (radioButton_bit11_right.Checked) myBytes.txMark[11] = MARK.RIGHT;

            if (radioButton_bit12_fix.Checked) myBytes.txMark[12] = MARK.FIX;
            else if (radioButton_bit12_up.Checked) myBytes.txMark[12] = MARK.UP;
            else if (radioButton_bit12_left.Checked) myBytes.txMark[12] = MARK.LEFT;
            else if (radioButton_bit12_right.Checked) myBytes.txMark[12] = MARK.RIGHT;

            if (radioButton_bit13_fix.Checked) myBytes.txMark[13] = MARK.FIX;
            else if (radioButton_bit13_up.Checked) myBytes.txMark[13] = MARK.UP;
            else if (radioButton_bit13_left.Checked) myBytes.txMark[13] = MARK.LEFT;
            else if (radioButton_bit13_right.Checked) myBytes.txMark[13] = MARK.RIGHT;

            if (radioButton_bit14_fix.Checked) myBytes.txMark[14] = MARK.FIX;
            else if (radioButton_bit14_up.Checked) myBytes.txMark[14] = MARK.UP;
            else if (radioButton_bit14_left.Checked) myBytes.txMark[14] = MARK.LEFT;
            else if (radioButton_bit14_right.Checked) myBytes.txMark[14] = MARK.RIGHT;

            if (radioButton_bit15_fix.Checked) myBytes.txMark[15] = MARK.FIX;
            else if (radioButton_bit15_up.Checked) myBytes.txMark[15] = MARK.UP;
            else if (radioButton_bit15_left.Checked) myBytes.txMark[15] = MARK.LEFT;
            else if (radioButton_bit15_right.Checked) myBytes.txMark[15] = MARK.RIGHT;
        }

        private void textBoxTab2_TextChanged(object sender, EventArgs e)
        {
            myBytes.Count = 16;
            if (textBox_bit15.TextLength > 0) myBytes.txBytes[15] = Convert.ToByte(Convert.ToUInt32(textBox_bit15.Text, 16)); else myBytes.Count = 15;
            if (textBox_bit14.TextLength > 0) myBytes.txBytes[14] = Convert.ToByte(Convert.ToUInt32(textBox_bit14.Text, 16)); else myBytes.Count = 14;
            if (textBox_bit13.TextLength > 0) myBytes.txBytes[13] = Convert.ToByte(Convert.ToUInt32(textBox_bit13.Text, 16)); else myBytes.Count = 13;
            if (textBox_bit12.TextLength > 0) myBytes.txBytes[12] = Convert.ToByte(Convert.ToUInt32(textBox_bit12.Text, 16)); else myBytes.Count = 12;
            if (textBox_bit11.TextLength > 0) myBytes.txBytes[11] = Convert.ToByte(Convert.ToUInt32(textBox_bit11.Text, 16)); else myBytes.Count = 11;
            if (textBox_bit10.TextLength > 0) myBytes.txBytes[10] = Convert.ToByte(Convert.ToUInt32(textBox_bit10.Text, 16)); else myBytes.Count = 10;
            if (textBox_bit09.TextLength > 0) myBytes.txBytes[09] = Convert.ToByte(Convert.ToUInt32(textBox_bit09.Text, 16)); else myBytes.Count = 9;
            if (textBox_bit08.TextLength > 0) myBytes.txBytes[08] = Convert.ToByte(Convert.ToUInt32(textBox_bit08.Text, 16)); else myBytes.Count = 8;
            if (textBox_bit07.TextLength > 0) myBytes.txBytes[07] = Convert.ToByte(Convert.ToUInt32(textBox_bit07.Text, 16)); else myBytes.Count = 7;
            if (textBox_bit06.TextLength > 0) myBytes.txBytes[06] = Convert.ToByte(Convert.ToUInt32(textBox_bit06.Text, 16)); else myBytes.Count = 6;
            if (textBox_bit05.TextLength > 0) myBytes.txBytes[05] = Convert.ToByte(Convert.ToUInt32(textBox_bit05.Text, 16)); else myBytes.Count = 5;
            if (textBox_bit04.TextLength > 0) myBytes.txBytes[04] = Convert.ToByte(Convert.ToUInt32(textBox_bit04.Text, 16)); else myBytes.Count = 4;
            if (textBox_bit03.TextLength > 0) myBytes.txBytes[03] = Convert.ToByte(Convert.ToUInt32(textBox_bit03.Text, 16)); else myBytes.Count = 3;
            if (textBox_bit02.TextLength > 0) myBytes.txBytes[02] = Convert.ToByte(Convert.ToUInt32(textBox_bit02.Text, 16)); else myBytes.Count = 2;
            if (textBox_bit01.TextLength > 0) myBytes.txBytes[01] = Convert.ToByte(Convert.ToUInt32(textBox_bit01.Text, 16)); else myBytes.Count = 1;
            if (textBox_bit00.TextLength > 0) myBytes.txBytes[00] = Convert.ToByte(Convert.ToUInt32(textBox_bit00.Text, 16)); else myBytes.Count = 0;
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (button16.BackColor != Color.Green)
            {
                textBox_bit00.Text = "";
                textBox_bit01.Text = "";
                textBox_bit02.Text = "";
                textBox_bit03.Text = "";
                textBox_bit04.Text = "";
                textBox_bit05.Text = "";
                textBox_bit06.Text = "";
                textBox_bit07.Text = "";
                textBox_bit08.Text = "";
                textBox_bit09.Text = "";
                textBox_bit10.Text = "";
                textBox_bit11.Text = "";
                textBox_bit12.Text = "";
                textBox_bit13.Text = "";
                textBox_bit14.Text = "";
                textBox_bit15.Text = "";
                textBoxTab2_hex.Text = "";
                textBox6.Text = "1";
                txCounterTab2 = 0;
                label8.Text = "Tx: ";
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (button16.BackColor != Color.Green)
            {
                radioButton_bit00_fix.Checked = true;
                radioButton_bit01_fix.Checked = true;
                radioButton_bit02_fix.Checked = true;
                radioButton_bit03_fix.Checked = true;
                radioButton_bit04_fix.Checked = true;
                radioButton_bit05_fix.Checked = true;
                radioButton_bit06_fix.Checked = true;
                radioButton_bit07_fix.Checked = true;
                radioButton_bit08_fix.Checked = true;
                radioButton_bit09_fix.Checked = true;
                radioButton_bit10_fix.Checked = true;
                radioButton_bit11_fix.Checked = true;
                radioButton_bit12_fix.Checked = true;
                radioButton_bit13_fix.Checked = true;
                radioButton_bit14_fix.Checked = true;
                radioButton_bit15_fix.Checked = true;
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (serialPort1.IsOpen)
            {
                if (myMode == MODE.ACODE)
                {
                    myMode = MODE.SIGNLE;
                    button16.BackColor = button11.BackColor;
                }
                else
                {
                    button9.BackColor = button11.BackColor;
                    button10.BackColor = button11.BackColor;

                    if (textBox_bit00.TextLength > 0)//有数据发送
                    {
                        //
                        if (textBox6.TextLength > 0)//有定时时间
                        {
                            //
                            radioButtonTab2_CheckedChanged(null, null);
                            textBoxTab2_TextChanged(null, null);
                            //
                            myMode = MODE.ACODE;
                            button16.BackColor = Color.Green;
                            timer1.Interval = Convert.ToInt32(textBox6.Text);
                            timer1.Enabled = true;
                        }
                    }
                }
            }
        }

        // <summary>
        // CAN
        // </summary>

        //CAN ID
        private void CanidTab3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8)
            {
                if (e.KeyChar < '0')
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > '9') && (e.KeyChar < 'A'))
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > 'F') && (e.KeyChar < 'a'))
                {
                    e.Handled = true;
                    return;
                }
                if (e.KeyChar > 'f')
                {
                    e.Handled = true;
                    return;
                }

                if (textBox1.TextLength > 0)
                {
                    if (comboBox_can_mode.SelectedIndex == 0)
                    {
                        if (textBox1.SelectionLength == 0)
                        {
                            if (Convert.ToInt32((textBox1.Text + e.KeyChar), 16) > 0x7FF)
                            {
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (textBox1.SelectionLength == 0)
                        {
                            if (Convert.ToInt64((textBox1.Text + e.KeyChar), 16) > 0x1FFFFFFF)
                            {
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        //CAN ID
        private void textBoxTab3_IdLeave(object sender, EventArgs e)
        {
            if (textBox1.TextLength > 0)
            {
                if (comboBox_can_mode.SelectedIndex == 0)
                {
                    if (Convert.ToInt32(((TextBox)sender).Text, 16) > 0x7FF)
                    {
                        ((TextBox)sender).Text = "7FF";
                    }
                }
                else
                {
                    if (Convert.ToInt64(((TextBox)sender).Text, 16) > 0x1FFFFFFF)
                    {
                        ((TextBox)sender).Text = "1FFFFFFF";
                    }
                }
            }
        }

        //HEX字符
        private void byteTab3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8)
            {
                if (e.KeyChar < '0')
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > '9') && (e.KeyChar < 'A'))
                {
                    e.Handled = true;
                    return;
                }
                if ((e.KeyChar > 'F') && (e.KeyChar < 'a'))
                {
                    e.Handled = true;
                    return;
                }
                if (e.KeyChar > 'f')
                {
                    e.Handled = true;
                    return;
                }
            }

            if (((TextBox)sender).TextLength > 1)
            {
                //可替换
                if (((TextBox)sender).SelectionLength > 0)
                {
                    return;
                }
                //可删除
                if (e.KeyChar != 8)
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (((TextBox)sender).TextLength > 0)
            {
                switch (((TextBox)sender).Name)
                {
                    default:
                    case "textBoxTab3_bit0": textBoxTab3_bit1.Focus(); break;
                    case "textBoxTab3_bit1": textBoxTab3_bit2.Focus(); break;
                    case "textBoxTab3_bit2": textBoxTab3_bit3.Focus(); break;
                    case "textBoxTab3_bit3": textBoxTab3_bit4.Focus(); break;
                    case "textBoxTab3_bit4": textBoxTab3_bit5.Focus(); break;
                    case "textBoxTab3_bit5": textBoxTab3_bit6.Focus(); break;
                    case "textBoxTab3_bit6": textBoxTab3_bit7.Focus(); break;
                    case "textBoxTab3_bit7": button22.Focus(); break;
                }
            }
        }

        private void comboBox_can_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_can_mode.SelectedIndex == 0)
            {
                textBox_ID.Text = " STD ID (0-7FF) :";
            }
            else
            {
                textBox_ID.Text = " EXT ID (0-1FFFFFFF) :";
            }
        }

        private void comboBox_can_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_can_frame.SelectedIndex == 0)
            {
                switch (comboBox_can_data.SelectedIndex)
                {
                    default:
                    case 0:
                        textBoxTab3_bit0.ReadOnly = true; textBoxTab3_bit0.Text = "";
                        textBoxTab3_bit1.ReadOnly = true; textBoxTab3_bit1.Text = "";
                        textBoxTab3_bit2.ReadOnly = true; textBoxTab3_bit2.Text = "";
                        textBoxTab3_bit3.ReadOnly = true; textBoxTab3_bit3.Text = "";
                        textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 1:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = true; textBoxTab3_bit1.Text = "";
                        textBoxTab3_bit2.ReadOnly = true; textBoxTab3_bit2.Text = "";
                        textBoxTab3_bit3.ReadOnly = true; textBoxTab3_bit3.Text = "";
                        textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 2:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = true; textBoxTab3_bit2.Text = "";
                        textBoxTab3_bit3.ReadOnly = true; textBoxTab3_bit3.Text = "";
                        textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 3:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = true; textBoxTab3_bit3.Text = "";
                        textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 4:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = false;
                        textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 5:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = false;
                        textBoxTab3_bit4.ReadOnly = false;
                        textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 6:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = false;
                        textBoxTab3_bit4.ReadOnly = false;
                        textBoxTab3_bit5.ReadOnly = false;
                        textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 7:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = false;
                        textBoxTab3_bit4.ReadOnly = false;
                        textBoxTab3_bit5.ReadOnly = false;
                        textBoxTab3_bit6.ReadOnly = false;
                        textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
                        break;
                    case 8:
                        textBoxTab3_bit0.ReadOnly = false;
                        textBoxTab3_bit1.ReadOnly = false;
                        textBoxTab3_bit2.ReadOnly = false;
                        textBoxTab3_bit3.ReadOnly = false;
                        textBoxTab3_bit4.ReadOnly = false;
                        textBoxTab3_bit5.ReadOnly = false;
                        textBoxTab3_bit6.ReadOnly = false;
                        textBoxTab3_bit7.ReadOnly = false;
                        break;
                }
            }
            else
            {
                textBoxTab3_bit0.ReadOnly = true; textBoxTab3_bit0.Text = "";
                textBoxTab3_bit1.ReadOnly = true; textBoxTab3_bit1.Text = "";
                textBoxTab3_bit2.ReadOnly = true; textBoxTab3_bit2.Text = "";
                textBoxTab3_bit3.ReadOnly = true; textBoxTab3_bit3.Text = "";
                textBoxTab3_bit4.ReadOnly = true; textBoxTab3_bit4.Text = "";
                textBoxTab3_bit5.ReadOnly = true; textBoxTab3_bit5.Text = "";
                textBoxTab3_bit6.ReadOnly = true; textBoxTab3_bit6.Text = "";
                textBoxTab3_bit7.ReadOnly = true; textBoxTab3_bit7.Text = "";
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            sbRX.Remove(0, sbRX.Length);
            sbReceiverTab3.Remove(0, sbReceiverTab3.Length);
            textBoxTab3_receiver.Text = "";
            textBoxTab3_receiver.ScrollToCaret();
            label14.Text = "Rx: ";
        }

        private void comboBox2_Click(object sender, EventArgs e)
        {
            if (comBusy) return;

            comboBox_all_id.Items.Clear();
            comboBox_all_id.Items.Add("ALL ID");

            bool repeat;
            Int32 id;
            String str;
            System.IO.StringReader sr = new System.IO.StringReader(sbReceiverTab3.ToString());
            while ((str = sr.ReadLine()) != null)
            {
                switch ((Byte)str[0])
                {
                    case 0x50:
                    case 0x51:
                    case 0x52:
                    case 0x53:
                    case 0x54:
                    case 0x55:
                    case 0x56:
                    case 0x57:
                    case 0x58:
                        if (str.Length > 2)
                        {
                            repeat = false;
                            id = (str[1] << 8) + str[2];
                            str = "0x" + id.ToString("X4");
                            foreach (String tmp in comboBox_all_id.Items)
                            {
                                if (tmp == str) repeat = true;
                            }
                            if (repeat == false)
                            {
                                comboBox_all_id.Items.Add(str);
                            }
                        }
                        break;
                    case 0x60:
                    case 0x61:
                    case 0x62:
                    case 0x63:
                    case 0x64:
                    case 0x65:
                    case 0x66:
                    case 0x67:
                    case 0x68:
                        if (str.Length > 4)
                        {
                            repeat = false;
                            id = (str[1] << 24) + (str[2] << 16) + (str[3] << 8) + str[4];
                            str = "0x" + id.ToString("X8");
                            foreach (String tmp in comboBox_all_id.Items)
                            {
                                if (tmp == str) repeat = true;
                            }
                            if (repeat == false)
                            {
                                comboBox_all_id.Items.Add(str);
                            }
                        }
                        break;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            //筛选ID
            if (comBusy) return;

            String id;
            String str;
            textBoxTab3_receiver.Text = "";

            if (comboBox_all_id.Text == "ALL ID")
            {
                str = ConvertStringToHex(sbReceiverTab3.ToString(), " ");
                textBoxTab3_receiver.AppendText(str.Replace("0D 0A ", "0D 0A \r\n"));
            }
            else
            {
                id = ConvertHexToString(comboBox_all_id.Text.Replace("0x", ""));
                System.IO.StringReader sr = new System.IO.StringReader(sbReceiverTab3.ToString());
                while ((str = sr.ReadLine()) != null)
                {
                    switch ((Byte)str[0])
                    {
                        case 0x50:
                        case 0x51:
                        case 0x52:
                        case 0x53:
                        case 0x54:
                        case 0x55:
                        case 0x56:
                        case 0x57:
                        case 0x58:
                            if (str.Length > 2)
                            {
                                if (str.Substring(1, 2) == id)
                                {
                                    textBoxTab3_receiver.AppendText(ConvertStringToHex(str, " ") + "0D 0A \r\n");
                                }
                            }
                            break;
                        case 0x60:
                        case 0x61:
                        case 0x62:
                        case 0x63:
                        case 0x64:
                        case 0x65:
                        case 0x66:
                        case 0x67:
                        case 0x68:
                            if (str.Length > 4)
                            {
                                if (str.Substring(1, 4) == id)
                                {
                                    textBoxTab3_receiver.AppendText(ConvertStringToHex(str, " ") + "0D 0A \r\n");
                                }
                            }
                            break;
                    }
                }
            }

            textBoxTab3_receiver.ScrollToCaret();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "文本文件(*.txt)|*.txt"; //设置要选择的文件的类型
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    File.Delete(fileDialog.FileName);
                }
                FileStream meFS = new FileStream(fileDialog.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                TextWriter meWrite = new StreamWriter(meFS);
                meWrite.Write(textBoxTab3_receiver.Text);
                meWrite.Close();
                meFS.Close();
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (textBoxTab3_receiver.TextLength > 0)
            {
                Clipboard.SetText(textBoxTab3_receiver.Text.Trim());
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            comboBox_usb_baud.Text = "115200";
            comboBox_usb_data.Text = "8";
            comboBox_usb_stop.Text = "1";
            comboBox_usb_crc.Text = "None";
        }

        private void button23_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x20;
            temp += (Char)comboBox_usb_baud.SelectedIndex;
            temp += (Char)comboBox_usb_stop.SelectedIndex;
            temp += (Char)comboBox_usb_crc.SelectedIndex;
            temp += (Char)(comboBox_usb_data.SelectedIndex + 5);

            switch (canDataLen)
            {
                case 0x00:
                    temp = "";
                    temp += (Char)0x20;
                    temp += (Char)comboBox_usb_baud.SelectedIndex;
                    temp += (Char)comboBox_usb_stop.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x01:
                    temp = "";
                    temp += (Char)0x20;
                    temp += (Char)comboBox_usb_baud.SelectedIndex;
                    temp += (Char)comboBox_usb_stop.SelectedIndex;
                    temp += (Char)comboBox_usb_crc.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x02: temp += "\r\n"; break;
                case 0x03: temp += " \r\n"; break;
                case 0x04: temp += "  \r\n"; break;
                case 0x05: temp += "   \r\n"; break;
                case 0x06: temp += "    \r\n"; break;
                case 0x07: temp += "     \r\n"; break;
                case 0x08: temp += "      \r\n"; break;
                case 0x10: temp += "\r\n"; break;
                case 0x11: temp += " \r\n"; break;
                case 0x12: temp += "  \r\n"; break;
                case 0x13: temp += "   \r\n"; break;
                case 0x14: temp += "    \r\n"; break;
                case 0x15: temp += "     \r\n"; break;
                case 0x16: temp += "      \r\n"; break;
                case 0x17: temp += "       \r\n"; break;
                case 0x18: temp += "        \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                button23.BackColor = Color.Green;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            comboBox_uart_baud.Text = "115200";
            comboBox_uart_data.Text = "8";
            comboBox_uart_stop.Text = "1";
            comboBox_uart_crc.Text = "None";
        }

        private void button25_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x40;
            temp += (Char)comboBox_uart_baud.SelectedIndex;
            temp += (Char)comboBox_uart_stop.SelectedIndex;
            temp += (Char)comboBox_uart_crc.SelectedIndex;
            temp += (Char)(comboBox_uart_data.SelectedIndex + 5);

            switch (canDataLen)
            {
                case 0x00:
                    temp = "";
                    temp += (Char)0x40;
                    temp += (Char)comboBox_uart_baud.SelectedIndex;
                    temp += (Char)comboBox_uart_stop.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x01:
                    temp = "";
                    temp += (Char)0x40;
                    temp += (Char)comboBox_uart_baud.SelectedIndex;
                    temp += (Char)comboBox_uart_stop.SelectedIndex;
                    temp += (Char)comboBox_uart_crc.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x02: temp += "\r\n"; break;
                case 0x03: temp += " \r\n"; break;
                case 0x04: temp += "  \r\n"; break;
                case 0x05: temp += "   \r\n"; break;
                case 0x06: temp += "    \r\n"; break;
                case 0x07: temp += "     \r\n"; break;
                case 0x08: temp += "      \r\n"; break;
                case 0x10: temp += "\r\n"; break;
                case 0x11: temp += " \r\n"; break;
                case 0x12: temp += "  \r\n"; break;
                case 0x13: temp += "   \r\n"; break;
                case 0x14: temp += "    \r\n"; break;
                case 0x15: temp += "     \r\n"; break;
                case 0x16: temp += "      \r\n"; break;
                case 0x17: temp += "       \r\n"; break;
                case 0x18: temp += "        \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                button25.BackColor = Color.Green;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            canDataLen = 8;
            comboBox_can_mode.Text = "标准帧";
            comboBox_can_frame.Text = "数据帧";
            comboBox_can_baud.Text = "1M BPS";
            comboBox_can_data.Text = "8 bit";
        }

        private void button27_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x30;
            temp += (Char)comboBox_can_mode.SelectedIndex;
            temp += (Char)comboBox_can_frame.SelectedIndex;
            temp += (Char)comboBox_can_baud.SelectedIndex;
            temp += (Char)comboBox_can_data.SelectedIndex;

            switch (canDataLen)
            {
                case 0x00:
                    temp = "";
                    temp += (Char)0x30;
                    temp += (Char)comboBox_can_mode.SelectedIndex;
                    temp += (Char)comboBox_can_frame.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x01:
                    temp = "";
                    temp += (Char)0x30;
                    temp += (Char)comboBox_can_mode.SelectedIndex;
                    temp += (Char)comboBox_can_frame.SelectedIndex;
                    temp += (Char)comboBox_can_baud.SelectedIndex;
                    temp += "\r\n";
                    break;
                case 0x02: temp += "\r\n"; break;
                case 0x03: temp += " \r\n"; break;
                case 0x04: temp += "  \r\n"; break;
                case 0x05: temp += "   \r\n"; break;
                case 0x06: temp += "    \r\n"; break;
                case 0x07: temp += "     \r\n"; break;
                case 0x08: temp += "      \r\n"; break;
                case 0x10: temp += "\r\n"; break;
                case 0x11: temp += " \r\n"; break;
                case 0x12: temp += "  \r\n"; break;
                case 0x13: temp += "   \r\n"; break;
                case 0x14: temp += "    \r\n"; break;
                case 0x15: temp += "     \r\n"; break;
                case 0x16: temp += "      \r\n"; break;
                case 0x17: temp += "       \r\n"; break;
                case 0x18: temp += "        \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                button27.BackColor = Color.Green;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
                //
                if (temp.Length > 6)
                {
                    if (comboBox_can_mode.SelectedIndex == 0)
                    {
                        canDataLen = (Byte)comboBox_can_data.SelectedIndex;
                    }
                    else
                    {
                        canDataLen = (Byte)(comboBox_can_data.SelectedIndex + 0x10);
                    }
                }
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x80;

            switch (canDataLen)
            {
                case 0x00: temp += "  \r\n"; break;
                case 0x01: temp += "   \r\n"; break;
                case 0x02: temp += "    \r\n"; break;
                case 0x03: temp += "     \r\n"; break;
                case 0x04: temp += "      \r\n"; break;
                case 0x05: temp += "       \r\n"; break;
                case 0x06: temp += "        \r\n"; break;
                case 0x07: temp += "         \r\n"; break;
                case 0x08: temp += "          \r\n"; break;
                case 0x10: temp += "    \r\n"; break;
                case 0x11: temp += "     \r\n"; break;
                case 0x12: temp += "      \r\n"; break;
                case 0x13: temp += "       \r\n"; break;
                case 0x14: temp += "        \r\n"; break;
                case 0x15: temp += "         \r\n"; break;
                case 0x16: temp += "          \r\n"; break;
                case 0x17: temp += "           \r\n"; break;
                case 0x18: temp += "            \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);

                //断开串口时，isClosing标记更新
                isClosing = true;

                //处理当前在消息队列中的所有 Windows 消息
                //防止界面停止响应
                //https://blog.csdn.net/sinat_23338865/article/details/52596818
                while (isListening)
                {
                    Application.DoEvents();
                }

                //关闭串口
                SerialPortClose();
                isClosing = false;
            }
        }

        private void button30_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x10;

            switch (canDataLen)
            {
                case 0x00: temp += "  \r\n"; break;
                case 0x01: temp += "   \r\n"; break;
                case 0x02: temp += "    \r\n"; break;
                case 0x03: temp += "     \r\n"; break;
                case 0x04: temp += "      \r\n"; break;
                case 0x05: temp += "       \r\n"; break;
                case 0x06: temp += "        \r\n"; break;
                case 0x07: temp += "         \r\n"; break;
                case 0x08: temp += "          \r\n"; break;
                case 0x10: temp += "    \r\n"; break;
                case 0x11: temp += "     \r\n"; break;
                case 0x12: temp += "      \r\n"; break;
                case 0x13: temp += "       \r\n"; break;
                case 0x14: temp += "        \r\n"; break;
                case 0x15: temp += "         \r\n"; break;
                case 0x16: temp += "          \r\n"; break;
                case 0x17: temp += "           \r\n"; break;
                case 0x18: temp += "            \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
                //
                button28_Click(null, null);
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            String temp = "";
            temp += (Char)0x70;

            switch (canDataLen)
            {
                case 0x00: temp += "  \r\n"; break;
                case 0x01: temp += "   \r\n"; break;
                case 0x02: temp += "    \r\n"; break;
                case 0x03: temp += "     \r\n"; break;
                case 0x04: temp += "      \r\n"; break;
                case 0x05: temp += "       \r\n"; break;
                case 0x06: temp += "        \r\n"; break;
                case 0x07: temp += "         \r\n"; break;
                case 0x08: temp += "          \r\n"; break;
                case 0x10: temp += "    \r\n"; break;
                case 0x11: temp += "     \r\n"; break;
                case 0x12: temp += "      \r\n"; break;
                case 0x13: temp += "       \r\n"; break;
                case 0x14: temp += "        \r\n"; break;
                case 0x15: temp += "         \r\n"; break;
                case 0x16: temp += "          \r\n"; break;
                case 0x17: temp += "           \r\n"; break;
                case 0x18: temp += "            \r\n"; break;
            }

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBoxTab3_bit0.Text = "";
            textBoxTab3_bit1.Text = "";
            textBoxTab3_bit2.Text = "";
            textBoxTab3_bit3.Text = "";
            textBoxTab3_bit4.Text = "";
            textBoxTab3_bit5.Text = "";
            textBoxTab3_bit6.Text = "";
            textBoxTab3_bit7.Text = "";
            textBoxTab3_hex.Text = "";
            txCounterTab3 = 0;
            label22.Text = "Tx: ";
        }

        private Char GetCanByte(TextBox sender)
        {
            if (sender.TextLength > 0)
            {
                return ((Char)(Convert.ToInt32(sender.Text, 16)));
            }
            else
            {
                return ((Char)0x00);
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            Int32 id = 0;
            String temp = "";

            if (comboBox_can_mode.SelectedIndex == 0)
            {
                temp += (Char)0x50;
                if (textBox1.TextLength > 0)
                {
                    id = Convert.ToInt32(textBox1.Text, 16);
                    temp += (Char)((id >> 8) & 0xFF);
                    temp += (Char)(id & 0xFF);
                }
                else
                {
                    temp += (Char)0x00;
                    temp += (Char)0x00;
                }
            }
            else
            {
                temp += (Char)0x60;
                if (textBox1.TextLength > 0)
                {
                    id = Convert.ToInt32(textBox1.Text, 16);
                    temp += (Char)((id >> 24) & 0xFF);
                    temp += (Char)((id >> 16) & 0xFF);
                    temp += (Char)((id >> 8) & 0xFF);
                    temp += (Char)(id & 0xFF);
                }
                else
                {
                    temp += (Char)0x00;
                    temp += (Char)0x00;
                    temp += (Char)0x00;
                    temp += (Char)0x00;
                }
            }

            if (comboBox_can_frame.SelectedIndex == 0)
            {
                switch (comboBox_can_data.SelectedIndex)
                {
                    case 1:
                        temp += GetCanByte(textBoxTab3_bit0);
                        break;
                    case 2:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        break;
                    case 3:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        break;
                    case 4:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        temp += GetCanByte(textBoxTab3_bit3);
                        break;
                    case 5:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        temp += GetCanByte(textBoxTab3_bit3);
                        temp += GetCanByte(textBoxTab3_bit4);
                        break;
                    case 6:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        temp += GetCanByte(textBoxTab3_bit3);
                        temp += GetCanByte(textBoxTab3_bit4);
                        temp += GetCanByte(textBoxTab3_bit5);
                        break;
                    case 7:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        temp += GetCanByte(textBoxTab3_bit3);
                        temp += GetCanByte(textBoxTab3_bit4);
                        temp += GetCanByte(textBoxTab3_bit5);
                        temp += GetCanByte(textBoxTab3_bit6);
                        break;
                    case 8:
                        temp += GetCanByte(textBoxTab3_bit0);
                        temp += GetCanByte(textBoxTab3_bit1);
                        temp += GetCanByte(textBoxTab3_bit2);
                        temp += GetCanByte(textBoxTab3_bit3);
                        temp += GetCanByte(textBoxTab3_bit4);
                        temp += GetCanByte(textBoxTab3_bit5);
                        temp += GetCanByte(textBoxTab3_bit6);
                        temp += GetCanByte(textBoxTab3_bit7);
                        break;
                }
            }

            temp += "\r\n";

            if (serialPort1.IsOpen)
            {
                button9.BackColor = button11.BackColor;
                button10.BackColor = button11.BackColor;
                button16.BackColor = button11.BackColor;
                //
                txCounterTab3 += temp.Length;
                label22.Text = "Tx: " + txCounterTab3.ToString();
                textBoxTab3_hex.Text = ConvertStringToHex(temp, " ");
                SerialPortWrite(temp);
            }
        }
    }
}

public enum BAUT : Int32
{
    B1200 = 1200,
    B2400 = 2400,
    B4800 = 4800,
    B9600 = 9600,
    B14400 = 14400,
    B19200 = 19200,
    B38400 = 38400,
    B57600 = 57600,
    B115200 = 115200,
    B230400 = 230400,
    B256000 = 256000,
}

//通用，编码，CAN，三模式切换
public enum PAGE : Byte
{
    NORM = 0,
    CODE = 1,
    CAN = 2,
}

//单发送，周期发送，循环发送，自动发送，串口模式
public enum MODE : Byte
{
    SIGNLE = 0,
    REPEAT = 1,
    NEXT = 2,
    ACODE = 3,
}

//编码模式里面的定时发送用
public enum MARK : Byte
{
    FIX = 0,
    UP = 1,
    LEFT = 2,
    RIGHT = 3,
}

//循环发送用
public class TxBullet
{
    public Boolean isHEX;
    public Int16 Item;
    public String Text;

    public TxBullet(Int16 number, String text, String hexString)
    {
        Item = number;
        Text = hexString;
        if (text == "HEX")
        {
            isHEX = true;
        }
        else
        {
            isHEX = false;
        }
    }
}

public class TxBytes
{
    public Byte[] txBytes;
    public MARK[] txMark;
    public Int16 Count;

    public TxBytes()
    {
        txBytes = new Byte[16];
        txMark = new MARK[16];
        Count = 0;
        txMark[0] = MARK.FIX;
        txMark[1] = MARK.FIX;
        txMark[2] = MARK.FIX;
        txMark[3] = MARK.FIX;
        txMark[4] = MARK.FIX;
        txMark[5] = MARK.FIX;
        txMark[6] = MARK.FIX;
        txMark[7] = MARK.FIX;
        txMark[8] = MARK.FIX;
        txMark[9] = MARK.FIX;
        txMark[10] = MARK.FIX;
        txMark[11] = MARK.FIX;
        txMark[12] = MARK.FIX;
        txMark[13] = MARK.FIX;
        txMark[14] = MARK.FIX;
        txMark[15] = MARK.FIX;
        txBytes[0] = 0;
        txBytes[1] = 0;
        txBytes[2] = 0;
        txBytes[3] = 0;
        txBytes[4] = 0;
        txBytes[5] = 0;
        txBytes[6] = 0;
        txBytes[7] = 0;
        txBytes[8] = 0;
        txBytes[9] = 0;
        txBytes[10] = 0;
        txBytes[11] = 0;
        txBytes[12] = 0;
        txBytes[13] = 0;
        txBytes[14] = 0;
        txBytes[15] = 0;
    }

    private void StepLeft(Int16 index)
    {
        index--;
        if (index < 0) return;
        if (index >= Count) return;
        if (txMark[index] == MARK.LEFT)
        {
            txBytes[index]++;
            if (txBytes[index] == 0)
            {
                StepLeft(index);
            }
        }
    }

    private void StepRight(Int16 index)
    {
        index++;
        if (index < 0) return;
        if (index >= Count) return;
        if (txMark[index] == MARK.RIGHT)
        {
            txBytes[index]++;
            if (txBytes[index] == 0)
            {
                StepRight(index);
            }
        }
    }

    public void AutoUpdate()
    {
        for (Int16 i = 0; i < Count; i++)
        {
            if (txMark[i] == MARK.UP)
            {
                txBytes[i]++;
                if (txBytes[i] == 0)
                {
                    StepLeft(i);
                    StepRight(i);
                }
            }
        }
    }
}

//自增发送
//固定
//自增
//前进位
//后进位
//循环

//CAN工具

//RemoveRepeatItem
