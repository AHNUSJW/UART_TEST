using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ComAssist
{
    public partial class ComString : Form
    {
        public ComString()
        {
            InitializeComponent();
        }

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

        private void button_Click(object sender, EventArgs e)
        {
            if (((Button)sender).Text == "HEX")
            {
                ((Button)sender).Text = "ASCII";
            }
            else
            {
                ((Button)sender).Text = "HEX";
            }
        }

        private bool textBox_isASCII(object sender)
        {
            switch (((TextBox)sender).Name)
            {
                case "textBox0": if (button0.Text == "HEX") { return false; } else { return true; }
                case "textBox1": if (button1.Text == "HEX") { return false; } else { return true; }
                case "textBox2": if (button2.Text == "HEX") { return false; } else { return true; }
                case "textBox3": if (button3.Text == "HEX") { return false; } else { return true; }
                case "textBox4": if (button4.Text == "HEX") { return false; } else { return true; }
                case "textBox5": if (button5.Text == "HEX") { return false; } else { return true; }
                case "textBox6": if (button6.Text == "HEX") { return false; } else { return true; }
                case "textBox7": if (button7.Text == "HEX") { return false; } else { return true; }
                case "textBox8": if (button8.Text == "HEX") { return false; } else { return true; }
                case "textBox9": if (button9.Text == "HEX") { return false; } else { return true; }
                case "textBox10": if (button10.Text == "HEX") { return false; } else { return true; }
                case "textBox11": if (button11.Text == "HEX") { return false; } else { return true; }
                case "textBox12": if (button12.Text == "HEX") { return false; } else { return true; }
                case "textBox13": if (button13.Text == "HEX") { return false; } else { return true; }
                case "textBox14": if (button14.Text == "HEX") { return false; } else { return true; }
                case "textBox15": if (button15.Text == "HEX") { return false; } else { return true; }
                case "textBox16": if (button16.Text == "HEX") { return false; } else { return true; }
                case "textBox17": if (button17.Text == "HEX") { return false; } else { return true; }
                case "textBox18": if (button18.Text == "HEX") { return false; } else { return true; }
                case "textBox19": if (button19.Text == "HEX") { return false; } else { return true; }
                default: return true;
            }
        }

        //
        private void byte_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (textBox_isASCII(sender)) return;
            if (e.KeyChar == 8) return;
            if (e.KeyChar == ' ') return;

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

            if (((TextBox)sender).TextLength > 0)
            {
                if (((TextBox)sender).SelectionStart == ((TextBox)sender).TextLength)
                {
                    if (((((TextBox)sender).TextLength + 1) % 3) == 0)
                    {
                        ((TextBox)sender).AppendText(" ");
                    }
                }
            }
        }

        //
        private void copy_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (textBox_isASCII(sender)) return;

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
                return;
            }
            if (e.KeyData == (Keys.Control | Keys.V))
            {
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        string temp = ConvertHexToString(Clipboard.GetText().Trim(), " ");
                        ((TextBox)sender).SelectedText = ConvertStringToHex(temp, " "); //Ctrl+V 粘贴
                    }
                    catch (Exception)
                    {
                        e.Handled = true;
                    }
                }
                return;
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
                return;
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string tmp;
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
                meWrite.WriteLine("ComString:");
                if (textBox0.TextLength > 0)
                {
                    if (button0.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox0.Text, " ");
                        meWrite.WriteLine("HEX0=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC0=" + textBox0.Text);
                    }
                }
                if (textBox1.TextLength > 0)
                {
                    if (button1.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox1.Text, " ");
                        meWrite.WriteLine("HEX1=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC1=" + textBox1.Text);
                    }
                }
                if (textBox2.TextLength > 0)
                {
                    if (button2.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox2.Text, " ");
                        meWrite.WriteLine("HEX2=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC2=" + textBox2.Text);
                    }
                }
                if (textBox3.TextLength > 0)
                {
                    if (button3.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox3.Text, " ");
                        meWrite.WriteLine("HEX3=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC3=" + textBox3.Text);
                    }
                }
                if (textBox4.TextLength > 0)
                {
                    if (button4.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox4.Text, " ");
                        meWrite.WriteLine("HEX4=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC4=" + textBox4.Text);
                    }
                }
                if (textBox5.TextLength > 0)
                {
                    if (button5.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox5.Text, " ");
                        meWrite.WriteLine("HEX5=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC5=" + textBox5.Text);
                    }
                }
                if (textBox6.TextLength > 0)
                {
                    if (button6.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox6.Text, " ");
                        meWrite.WriteLine("HEX6=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC6=" + textBox6.Text);
                    }
                }
                if (textBox7.TextLength > 0)
                {
                    if (button7.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox7.Text, " ");
                        meWrite.WriteLine("HEX7=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC7=" + textBox7.Text);
                    }
                }
                if (textBox8.TextLength > 0)
                {
                    if (button8.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox8.Text, " ");
                        meWrite.WriteLine("HEX8=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC8=" + textBox8.Text);
                    }
                }
                if (textBox9.TextLength > 0)
                {
                    if (button9.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox9.Text, " ");
                        meWrite.WriteLine("HEX9=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC9=" + textBox9.Text);
                    }
                }
                if (textBox10.TextLength > 0)
                {
                    if (button10.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox10.Text, " ");
                        meWrite.WriteLine("HEX10=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC10=" + textBox10.Text);
                    }
                }
                if (textBox11.TextLength > 0)
                {
                    if (button11.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox11.Text, " ");
                        meWrite.WriteLine("HEX11=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC11=" + textBox11.Text);
                    }
                }
                if (textBox12.TextLength > 0)
                {
                    if (button12.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox12.Text, " ");
                        meWrite.WriteLine("HEX12=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC12=" + textBox12.Text);
                    }
                }
                if (textBox13.TextLength > 0)
                {
                    if (button13.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox13.Text, " ");
                        meWrite.WriteLine("HEX13=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC13=" + textBox13.Text);
                    }
                }
                if (textBox14.TextLength > 0)
                {
                    if (button14.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox14.Text, " ");
                        meWrite.WriteLine("HEX14=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC14=" + textBox14.Text);
                    }
                }
                if (textBox15.TextLength > 0)
                {
                    if (button15.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox15.Text, " ");
                        meWrite.WriteLine("HEX15=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC15=" + textBox15.Text);
                    }
                }
                if (textBox16.TextLength > 0)
                {
                    if (button16.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox16.Text, " ");
                        meWrite.WriteLine("HEX16=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC16=" + textBox16.Text);
                    }
                }
                if (textBox17.TextLength > 0)
                {
                    if (button17.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox17.Text, " ");
                        meWrite.WriteLine("HEX17=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC17=" + textBox17.Text);
                    }
                }
                if (textBox18.TextLength > 0)
                {
                    if (button18.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox18.Text, " ");
                        meWrite.WriteLine("HEX18=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC18=" + textBox18.Text);
                    }
                }
                if (textBox19.TextLength > 0)
                {
                    if (button19.Text == "HEX")
                    {
                        tmp = ConvertHexToString(textBox19.Text, " ");
                        meWrite.WriteLine("HEX19=" + ConvertStringToHex(tmp, " "));
                    }
                    else
                    {
                        meWrite.WriteLine("ASC19=" + textBox19.Text);
                    }
                }
                meWrite.Close();
                meFS.Close();
            }
        }

        private void LoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "文本文件(*.txt)|*.txt"; //设置要选择的文件的类型
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                String[] meLines = File.ReadAllLines(fileDialog.FileName, System.Text.Encoding.Default);
                if (meLines[0] == "ComString:")
                {
                    foreach (String line in meLines)
                    {
                        if (line == "ComString:")
                        {
                            continue;
                        }
                        if (line.IndexOf('=') < 0)
                        {
                            continue;
                        }
                        switch (line.Substring(0, line.IndexOf('=')))
                        {
                            //
                            case "HEX0": textBox0.Text = line.Substring(line.IndexOf('=') + 1); button0.Text = "HEX"; break;
                            case "HEX1": textBox1.Text = line.Substring(line.IndexOf('=') + 1); button1.Text = "HEX"; break;
                            case "HEX2": textBox2.Text = line.Substring(line.IndexOf('=') + 1); button2.Text = "HEX"; break;
                            case "HEX3": textBox3.Text = line.Substring(line.IndexOf('=') + 1); button3.Text = "HEX"; break;
                            case "HEX4": textBox4.Text = line.Substring(line.IndexOf('=') + 1); button4.Text = "HEX"; break;
                            case "HEX5": textBox5.Text = line.Substring(line.IndexOf('=') + 1); button5.Text = "HEX"; break;
                            case "HEX6": textBox6.Text = line.Substring(line.IndexOf('=') + 1); button6.Text = "HEX"; break;
                            case "HEX7": textBox7.Text = line.Substring(line.IndexOf('=') + 1); button7.Text = "HEX"; break;
                            case "HEX8": textBox8.Text = line.Substring(line.IndexOf('=') + 1); button8.Text = "HEX"; break;
                            case "HEX9": textBox9.Text = line.Substring(line.IndexOf('=') + 1); button9.Text = "HEX"; break;
                            case "HEX10": textBox10.Text = line.Substring(line.IndexOf('=') + 1); button10.Text = "HEX"; break;
                            case "HEX11": textBox11.Text = line.Substring(line.IndexOf('=') + 1); button11.Text = "HEX"; break;
                            case "HEX12": textBox12.Text = line.Substring(line.IndexOf('=') + 1); button12.Text = "HEX"; break;
                            case "HEX13": textBox13.Text = line.Substring(line.IndexOf('=') + 1); button13.Text = "HEX"; break;
                            case "HEX14": textBox14.Text = line.Substring(line.IndexOf('=') + 1); button14.Text = "HEX"; break;
                            case "HEX15": textBox15.Text = line.Substring(line.IndexOf('=') + 1); button15.Text = "HEX"; break;
                            case "HEX16": textBox16.Text = line.Substring(line.IndexOf('=') + 1); button16.Text = "HEX"; break;
                            case "HEX17": textBox17.Text = line.Substring(line.IndexOf('=') + 1); button17.Text = "HEX"; break;
                            case "HEX18": textBox18.Text = line.Substring(line.IndexOf('=') + 1); button18.Text = "HEX"; break;
                            case "HEX19": textBox19.Text = line.Substring(line.IndexOf('=') + 1); button19.Text = "HEX"; break;
                            //
                            case "ASC0": textBox0.Text = line.Substring(line.IndexOf('=') + 1); button0.Text = "ASCII"; break;
                            case "ASC1": textBox1.Text = line.Substring(line.IndexOf('=') + 1); button1.Text = "ASCII"; break;
                            case "ASC2": textBox2.Text = line.Substring(line.IndexOf('=') + 1); button2.Text = "ASCII"; break;
                            case "ASC3": textBox3.Text = line.Substring(line.IndexOf('=') + 1); button3.Text = "ASCII"; break;
                            case "ASC4": textBox4.Text = line.Substring(line.IndexOf('=') + 1); button4.Text = "ASCII"; break;
                            case "ASC5": textBox5.Text = line.Substring(line.IndexOf('=') + 1); button5.Text = "ASCII"; break;
                            case "ASC6": textBox6.Text = line.Substring(line.IndexOf('=') + 1); button6.Text = "ASCII"; break;
                            case "ASC7": textBox7.Text = line.Substring(line.IndexOf('=') + 1); button7.Text = "ASCII"; break;
                            case "ASC8": textBox8.Text = line.Substring(line.IndexOf('=') + 1); button8.Text = "ASCII"; break;
                            case "ASC9": textBox9.Text = line.Substring(line.IndexOf('=') + 1); button9.Text = "ASCII"; break;
                            case "ASC10": textBox10.Text = line.Substring(line.IndexOf('=') + 1); button10.Text = "ASCII"; break;
                            case "ASC11": textBox11.Text = line.Substring(line.IndexOf('=') + 1); button11.Text = "ASCII"; break;
                            case "ASC12": textBox12.Text = line.Substring(line.IndexOf('=') + 1); button12.Text = "ASCII"; break;
                            case "ASC13": textBox13.Text = line.Substring(line.IndexOf('=') + 1); button13.Text = "ASCII"; break;
                            case "ASC14": textBox14.Text = line.Substring(line.IndexOf('=') + 1); button14.Text = "ASCII"; break;
                            case "ASC15": textBox15.Text = line.Substring(line.IndexOf('=') + 1); button15.Text = "ASCII"; break;
                            case "ASC16": textBox16.Text = line.Substring(line.IndexOf('=') + 1); button16.Text = "ASCII"; break;
                            case "ASC17": textBox17.Text = line.Substring(line.IndexOf('=') + 1); button17.Text = "ASCII"; break;
                            case "ASC18": textBox18.Text = line.Substring(line.IndexOf('=') + 1); button18.Text = "ASCII"; break;
                            case "ASC19": textBox19.Text = line.Substring(line.IndexOf('=') + 1); button19.Text = "ASCII"; break;
                            //
                            default: break;
                        }
                    }
                }
            }
        }

        //
        private void FormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string temp;

            if (button0.Text == "HEX") { temp = ConvertHexToString(textBox0.Text.Trim(), " "); textBox0.Text = ConvertStringToHex(temp, " "); }
            if (button1.Text == "HEX") { temp = ConvertHexToString(textBox1.Text.Trim(), " "); textBox1.Text = ConvertStringToHex(temp, " "); }
            if (button2.Text == "HEX") { temp = ConvertHexToString(textBox2.Text.Trim(), " "); textBox2.Text = ConvertStringToHex(temp, " "); }
            if (button3.Text == "HEX") { temp = ConvertHexToString(textBox3.Text.Trim(), " "); textBox3.Text = ConvertStringToHex(temp, " "); }
            if (button4.Text == "HEX") { temp = ConvertHexToString(textBox4.Text.Trim(), " "); textBox4.Text = ConvertStringToHex(temp, " "); }
            if (button5.Text == "HEX") { temp = ConvertHexToString(textBox5.Text.Trim(), " "); textBox5.Text = ConvertStringToHex(temp, " "); }
            if (button6.Text == "HEX") { temp = ConvertHexToString(textBox6.Text.Trim(), " "); textBox6.Text = ConvertStringToHex(temp, " "); }
            if (button7.Text == "HEX") { temp = ConvertHexToString(textBox7.Text.Trim(), " "); textBox7.Text = ConvertStringToHex(temp, " "); }
            if (button8.Text == "HEX") { temp = ConvertHexToString(textBox8.Text.Trim(), " "); textBox8.Text = ConvertStringToHex(temp, " "); }
            if (button9.Text == "HEX") { temp = ConvertHexToString(textBox9.Text.Trim(), " "); textBox9.Text = ConvertStringToHex(temp, " "); }
            if (button10.Text == "HEX") { temp = ConvertHexToString(textBox10.Text.Trim(), " "); textBox10.Text = ConvertStringToHex(temp, " "); }
            if (button11.Text == "HEX") { temp = ConvertHexToString(textBox11.Text.Trim(), " "); textBox11.Text = ConvertStringToHex(temp, " "); }
            if (button12.Text == "HEX") { temp = ConvertHexToString(textBox12.Text.Trim(), " "); textBox12.Text = ConvertStringToHex(temp, " "); }
            if (button13.Text == "HEX") { temp = ConvertHexToString(textBox13.Text.Trim(), " "); textBox13.Text = ConvertStringToHex(temp, " "); }
            if (button14.Text == "HEX") { temp = ConvertHexToString(textBox14.Text.Trim(), " "); textBox14.Text = ConvertStringToHex(temp, " "); }
            if (button15.Text == "HEX") { temp = ConvertHexToString(textBox15.Text.Trim(), " "); textBox15.Text = ConvertStringToHex(temp, " "); }
            if (button16.Text == "HEX") { temp = ConvertHexToString(textBox16.Text.Trim(), " "); textBox16.Text = ConvertStringToHex(temp, " "); }
            if (button17.Text == "HEX") { temp = ConvertHexToString(textBox17.Text.Trim(), " "); textBox17.Text = ConvertStringToHex(temp, " "); }
            if (button18.Text == "HEX") { temp = ConvertHexToString(textBox18.Text.Trim(), " "); textBox18.Text = ConvertStringToHex(temp, " "); }
            if (button19.Text == "HEX") { temp = ConvertHexToString(textBox19.Text.Trim(), " "); textBox19.Text = ConvertStringToHex(temp, " "); }
        }

        public List<TxBullet> myBullet = new List<TxBullet>();

        //
        private void ComString_FormClosing(object sender, FormClosingEventArgs e)
        {
            myBullet.Clear();
            if (textBox0.TextLength > 0) myBullet.Add(new TxBullet(0, button0.Text, textBox0.Text));
            if (textBox1.TextLength > 0) myBullet.Add(new TxBullet(1, button1.Text, textBox1.Text));
            if (textBox2.TextLength > 0) myBullet.Add(new TxBullet(2, button2.Text, textBox2.Text));
            if (textBox3.TextLength > 0) myBullet.Add(new TxBullet(3, button3.Text, textBox3.Text));
            if (textBox4.TextLength > 0) myBullet.Add(new TxBullet(4, button4.Text, textBox4.Text));
            if (textBox5.TextLength > 0) myBullet.Add(new TxBullet(5, button5.Text, textBox5.Text));
            if (textBox6.TextLength > 0) myBullet.Add(new TxBullet(6, button6.Text, textBox6.Text));
            if (textBox7.TextLength > 0) myBullet.Add(new TxBullet(7, button7.Text, textBox7.Text));
            if (textBox8.TextLength > 0) myBullet.Add(new TxBullet(8, button8.Text, textBox8.Text));
            if (textBox9.TextLength > 0) myBullet.Add(new TxBullet(9, button9.Text, textBox9.Text));
            if (textBox10.TextLength > 0) myBullet.Add(new TxBullet(10, button10.Text, textBox10.Text));
            if (textBox11.TextLength > 0) myBullet.Add(new TxBullet(11, button11.Text, textBox11.Text));
            if (textBox12.TextLength > 0) myBullet.Add(new TxBullet(12, button12.Text, textBox12.Text));
            if (textBox13.TextLength > 0) myBullet.Add(new TxBullet(13, button13.Text, textBox13.Text));
            if (textBox14.TextLength > 0) myBullet.Add(new TxBullet(14, button14.Text, textBox14.Text));
            if (textBox15.TextLength > 0) myBullet.Add(new TxBullet(15, button15.Text, textBox15.Text));
            if (textBox16.TextLength > 0) myBullet.Add(new TxBullet(16, button16.Text, textBox16.Text));
            if (textBox17.TextLength > 0) myBullet.Add(new TxBullet(17, button17.Text, textBox17.Text));
            if (textBox18.TextLength > 0) myBullet.Add(new TxBullet(18, button18.Text, textBox18.Text));
            if (textBox19.TextLength > 0) myBullet.Add(new TxBullet(19, button19.Text, textBox19.Text));
        }

        private void ComString_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < myBullet.Count; i++)
            {
                switch (myBullet[i].Item)
                {
                    case 0: if (myBullet[i].isHEX) { button0.Text = "HEX"; } else { button0.Text = "ASCII"; }; textBox0.Text = myBullet[i].Text; break;
                    case 1: if (myBullet[i].isHEX) { button1.Text = "HEX"; } else { button1.Text = "ASCII"; }; textBox1.Text = myBullet[i].Text; break;
                    case 2: if (myBullet[i].isHEX) { button2.Text = "HEX"; } else { button2.Text = "ASCII"; }; textBox2.Text = myBullet[i].Text; break;
                    case 3: if (myBullet[i].isHEX) { button3.Text = "HEX"; } else { button3.Text = "ASCII"; }; textBox3.Text = myBullet[i].Text; break;
                    case 4: if (myBullet[i].isHEX) { button4.Text = "HEX"; } else { button4.Text = "ASCII"; }; textBox4.Text = myBullet[i].Text; break;
                    case 5: if (myBullet[i].isHEX) { button5.Text = "HEX"; } else { button5.Text = "ASCII"; }; textBox5.Text = myBullet[i].Text; break;
                    case 6: if (myBullet[i].isHEX) { button6.Text = "HEX"; } else { button6.Text = "ASCII"; }; textBox6.Text = myBullet[i].Text; break;
                    case 7: if (myBullet[i].isHEX) { button7.Text = "HEX"; } else { button7.Text = "ASCII"; }; textBox7.Text = myBullet[i].Text; break;
                    case 8: if (myBullet[i].isHEX) { button8.Text = "HEX"; } else { button8.Text = "ASCII"; }; textBox8.Text = myBullet[i].Text; break;
                    case 9: if (myBullet[i].isHEX) { button9.Text = "HEX"; } else { button9.Text = "ASCII"; }; textBox9.Text = myBullet[i].Text; break;
                    case 10: if (myBullet[i].isHEX) { button10.Text = "HEX"; } else { button10.Text = "ASCII"; }; textBox10.Text = myBullet[i].Text; break;
                    case 11: if (myBullet[i].isHEX) { button11.Text = "HEX"; } else { button11.Text = "ASCII"; }; textBox11.Text = myBullet[i].Text; break;
                    case 12: if (myBullet[i].isHEX) { button12.Text = "HEX"; } else { button12.Text = "ASCII"; }; textBox12.Text = myBullet[i].Text; break;
                    case 13: if (myBullet[i].isHEX) { button13.Text = "HEX"; } else { button13.Text = "ASCII"; }; textBox13.Text = myBullet[i].Text; break;
                    case 14: if (myBullet[i].isHEX) { button14.Text = "HEX"; } else { button14.Text = "ASCII"; }; textBox14.Text = myBullet[i].Text; break;
                    case 15: if (myBullet[i].isHEX) { button15.Text = "HEX"; } else { button15.Text = "ASCII"; }; textBox15.Text = myBullet[i].Text; break;
                    case 16: if (myBullet[i].isHEX) { button16.Text = "HEX"; } else { button16.Text = "ASCII"; }; textBox16.Text = myBullet[i].Text; break;
                    case 17: if (myBullet[i].isHEX) { button17.Text = "HEX"; } else { button17.Text = "ASCII"; }; textBox17.Text = myBullet[i].Text; break;
                    case 18: if (myBullet[i].isHEX) { button18.Text = "HEX"; } else { button18.Text = "ASCII"; }; textBox18.Text = myBullet[i].Text; break;
                    case 19: if (myBullet[i].isHEX) { button19.Text = "HEX"; } else { button19.Text = "ASCII"; }; textBox19.Text = myBullet[i].Text; break;
                }
            }
        }
    }
}
