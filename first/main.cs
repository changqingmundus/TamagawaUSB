using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TamagawaUSB
{
    public partial class btnClickThis : Form
    {
        public btnClickThis()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           // 获取所有可用串口名称（例如 "COM1", "COM2" 等）
           string[] ports = System.IO.Ports.SerialPort.GetPortNames();
           // 将串口数组直接添加到 ComboBox 的下拉项中[reference:1]
           comboBox1.Items.AddRange(ports);

        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
           // 确保用户确实选择了一个项，而不是清空了选择
           if (comboBox1.SelectedItem != null)
           {
               string selectedPort = comboBox1.SelectedItem.ToString();
           }
        }
        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            // 刷新串口列表
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(ports);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            // 判断当前串口是否已经打开
            if (SerialPortManager.sp != null && SerialPortManager.sp.IsOpen)
            {
                // === 关闭串口 ===
                SerialPortManager.ClosePort();
                // 更新按钮文本和状态
                button4.Text = "打开串口";
                button4.BackColor = SystemColors.Control;
                // 启用下拉框选择
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                //MessageBox.Show("串口已关闭", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // === 打开串口 ===
                // 检查是否选择了串口
                if (comboBox1.SelectedItem == null)
                {
                    MessageBox.Show("请先选择串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // 安全获取波特率
                if (!int.TryParse(comboBox2.Text, out int baudRate))
                {
                    MessageBox.Show("波特率无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string comPort = comboBox1.SelectedItem.ToString();
                bool success = SerialPortManager.OpenPort(comPort, baudRate);
                if (success)
                {
                    button4.Text = "关闭串口";
                    button4.BackColor = Color.LightGreen;
                    // 禁用下拉框
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    //MessageBox.Show($"串口 {comPort} 打开成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"串口 {comPort} 打开失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            textBox1.AppendText("【已清除】" + Environment.NewLine);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            int bytesToRead = SerialPortManager.sp.BytesToRead;
            try
            {
                // 发送读命令 0x02
                byte[] data = new byte[] { 0x02 };
                SerialPortManager.WriteData(data);
                // 等待编码器应答，最多等待 100ms（防止卡死）
                System.Threading.Thread.Sleep(20);
                byte[] buffer = new byte[bytesToRead];
                SerialPortManager.sp.Read(buffer, 0, bytesToRead);
                string hex = BitConverter.ToString(buffer);
                textBox1.AppendText("收到原始数据: " + hex + Environment.NewLine);

            }
            catch (Exception ex)
            {
                MessageBox.Show("通信错误：" + ex.Message);
            }
        }

        
        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxBaud(object sender, EventArgs e)
        {

        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // 确保在UI线程上更新控件
                if (textBox1.InvokeRequired)
                {
                    textBox1.Invoke(new Action(() => sp_DataReceived(sender, e)));
                    return;
                }

                int bytesToRead = SerialPortManager.sp.BytesToRead;
                if (bytesToRead >= 5)  // 多摩川帧固定5字节
                {
                    byte[] buffer = new byte[5];
                    SerialPortManager.sp.Read(buffer, 0, 5);  // 只读5字节（按帧读取）
                    // 显示原始十六进制
                    string hexString = BitConverter.ToString(buffer);
                    textBox1.AppendText("收到: " + hexString + Environment.NewLine);

                    // 解析5字节帧
                    var result = TamagawaDecoder.ParseFrame(buffer);
                    if (result != null)
                    {
                        textBox1.AppendText($"  绝对位置 (十进制): {result.Position}" + Environment.NewLine);
                        textBox1.AppendText($"  状态: SV={result.SV} MV={result.MV} 警告={result.Warning} 电池={result.Battery}" + Environment.NewLine);
                        textBox1.AppendText($"  CRC校验: 通过" + Environment.NewLine);

                        // 如果有波形控件，可以绘制
                        // chart1.Series["Position"].Points.AddY(result.Position);
                    }
                    else
                    {
                        textBox1.AppendText("  解析失败（CRC错误）" + Environment.NewLine);
                    }
                }
                else
                {
                    // 数据不足5字节时，可以选择等待或清空缓冲区（避免卡死）
                    // 这里简单提示
                    textBox1.AppendText("未收到完整帧（仅 " + bytesToRead + " 字节）" + Environment.NewLine);
                    // 可选：清空不完整数据
                    if (bytesToRead > 0)
                        SerialPortManager.sp.Read(new byte[bytesToRead], 0, bytesToRead);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("通信错误：" + ex.Message);
            }
        }
    }
    public static class SerialPortManager
    {
        public static SerialPort sp;
        public static bool OpenPort(string comPort, int baudRate)
        {
            try
            {
                if (sp != null && sp.IsOpen)
                    sp.Close();
                sp = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One);
                sp.Handshake = Handshake.None;   // 统一设置
                sp.Open();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("打开串口失败: " + ex.Message);
                return false;
            }
        }
        public static void ClosePort()
        {
            if (sp != null && sp.IsOpen)
            {
                sp.Close();
                sp.Dispose(); // 可选，释放资源
                sp = null;
            }
        }
        public static void WriteData(byte[] data)
        {
            if (sp != null && sp.IsOpen)
            {
                sp.Write(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("串口未打开");
            }
        }
    }
    public static class TamagawaDecoder
    {
        public class DecodeResult
        {
            public int Position { get; set; }
            public bool SV { get; set; }
            public bool MV { get; set; }
            public bool Warning { get; set; }
            public bool Battery { get; set; }
            public bool Error { get; set; }
            public bool Parity { get; set; }
        }

        public static DecodeResult ParseFrame(byte[] buf)
        {
            if (buf.Length < 5) return null;

            byte sf = buf[0];
            int position = (buf[1] << 16) | (buf[2] << 8) | buf[3];
            byte recvCrc = buf[4];
            byte calcCrc = ComputeCrc8(new byte[] { sf, buf[1], buf[2], buf[3] });
            if (calcCrc != recvCrc) return null;

            return new DecodeResult
            {
                Position = position,
                SV = (sf & 0x80) != 0,
                MV = (sf & 0x40) != 0,
                Warning = (sf & 0x08) != 0,
                Battery = (sf & 0x04) != 0,
                Error = (sf & 0x02) != 0,
                Parity = (sf & 0x01) != 0
            };
        }

        private static byte ComputeCrc8(byte[] data)
        {
            byte crc = 0xFF;
            const byte poly = 0x31;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x80) != 0)
                        crc = (byte)((crc << 1) ^ poly);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }
    }
}