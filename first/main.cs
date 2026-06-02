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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WPF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TamagawaUSB
{
    public partial class btnClickThis : Form
    {
        private DialControl dialControl1;   // 成员变量声明（正确位置)
        private Timer continuousTimer;   // 用于连续读取的定时器
        private bool showDebugInfo = true; // 是否输出调试信息到文本框

        public btnClickThis()
        {
            InitializeComponent();
            elementHost1.Width = 300;
            elementHost1.Height = 300;
            dialControl1 = new WPF.DialControl();
            elementHost1.Child = dialControl1;
            elementHost1.Location = new Point(12, 221);
            elementHost1.BringToFront();
            this.Controls.Add(elementHost1);

            // 根据你的需要调整（毫秒）
            continuousTimer = new Timer();
            continuousTimer.Interval = 100; 
            continuousTimer.Tick += ContinuousTimer_Tick;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // 获取所有可用串口名称（例如 "COM1", "COM2" 等）
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            // 将串口数组直接添加到 ComboBox 的下拉项中[reference:1]
            comboBox1.Items.AddRange(ports);

        }
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            continuousTimer.Stop();
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
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void comboBoxBaud(object sender, EventArgs e)
        {

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
                bool success = SerialPortManager.OpenPort(comPort, baudRate, null);
                if (success)
                {
                    button4.Text = "关闭串口";
                    button4.BackColor = Color.LightGreen;
                    // 禁用下拉框
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
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
        private void button1_Click(object sender, EventArgs e)
        {
            // 临时保存并覆盖显示标志，确保这次读取会输出信息
            bool oldFlag = showDebugInfo;
            showDebugInfo = true;
            PerformSingleRead();
            showDebugInfo = oldFlag;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (SerialPortManager.sp == null || !SerialPortManager.sp.IsOpen)
            {
                MessageBox.Show("请先打开串口！");
                return;
            }

            try
            {
                // 清空接收緩衝區
                SerialPortManager.sp.DiscardInBuffer();

                // 發送 DATA_ID_1
                byte[] cmd = new byte[] { 0x8A };

                SerialPortManager.WriteData(cmd);

                textBox1.AppendText(
                    "TX: " + BitConverter.ToString(cmd) +
                    Environment.NewLine);

                // 等待回應
                System.Threading.Thread.Sleep(50);

                int count = SerialPortManager.sp.BytesToRead;

                if (count < 6)
                {
                    textBox1.AppendText(
                        $"接受数据错误" +
                        Environment.NewLine);

                    return;
                }

                byte[] rx = new byte[6];

                SerialPortManager.sp.Read(rx, 0, 6);

                textBox1.AppendText(
                    "RX: " + BitConverter.ToString(rx) +
                    Environment.NewLine);

                byte cf = rx[0];
                byte sf = rx[1];

                uint abm =
                     ((uint)rx[2] << 16) |
                     ((uint)rx[3] << 8) |
                     rx[4];

                abm >>= 8;

                byte recvCrc = rx[5];

                byte calcCrc = CalcCRC(new byte[]
                {
                  rx[0],
                  rx[1],
                  rx[2],
                  rx[3],
                  rx[4]
                });

                textBox1.AppendText(
                    $"ABM:{abm}/4095 " +
                    $"CRC:{(recvCrc == calcCrc ? "PASS" : "FAIL")}" +
                    Environment.NewLine
              );
            }
            catch (Exception ex)
            {
                MessageBox.Show("通讯错误：" + ex.Message);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (SerialPortManager.sp == null || !SerialPortManager.sp.IsOpen)
            {
                MessageBox.Show("请先打开串口！");
                return;
            }

            try
            {
                // 清空接收缓存
                SerialPortManager.sp.DiscardInBuffer();

                // 发送 0x1A
                byte[] cmd = new byte[] { 0x1A };

                SerialPortManager.WriteData(cmd);

                textBox1.AppendText(
                    "TX: " +
                    BitConverter.ToString(cmd) +
                    Environment.NewLine);

                System.Threading.Thread.Sleep(100);

                int count = SerialPortManager.sp.BytesToRead;

                if (count <= 0)
                {
                    textBox1.AppendText(
                        "未收到数据" +
                        Environment.NewLine);
                    return;
                }

                byte[] rx = new byte[count];

                SerialPortManager.sp.Read(rx, 0, count);

                textBox1.AppendText(
                    "RX: " +
                    BitConverter.ToString(rx) +
                    Environment.NewLine);

                if (rx.Length < 11)
                {
                    textBox1.AppendText(
                        $"接受数据错误" +
                        Environment.NewLine);
                    return;
                }

                byte cf = rx[0];
                byte sf = rx[1];

                uint abs =
                     ((uint)rx[2] << 16) |
                     ((uint)rx[3] << 8) |
                     rx[4];

                abs &= 0x7FFFF;

                byte enid = rx[5];

                uint abm =
                    ((uint)rx[6] << 16) |
                    ((uint)rx[7] << 8) |
                    rx[8];

                byte almc = rx[9];

                byte recvCrc = rx[10];

                byte calcCrc = CalcCRC(new byte[]
                {
                   rx[0],rx[1],rx[2],rx[3],
                   rx[4],rx[5],rx[6],rx[7],
                   rx[8],rx[9]
                });

                textBox1.AppendText(
                   $"ABS:{abs}(0x{abs:X6}) " +
                   $"ENID:0x{enid:X2} " +
                   $"ABM:{abm}/4095 " +
                   $"ALMC:0x{almc:X2} " +
                   $"CRC:{(recvCrc == calcCrc ? "PASS" : "FAIL")}" +
                   Environment.NewLine
                );

                double angle =
                    abs * 360.0 / 524288.0;
                    dialControl1.UpdateAngle(angle);

                textBox1.AppendText(
                    $"Angle : {angle:F4}°" +
                    Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        private byte CalcCRC(byte[] data)
        {
            byte crc = 0;

            foreach (byte b in data)
            {
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x80) != 0)
                        crc = (byte)(((crc << 1) ^ 0x01) & 0xFF);
                    else
                        crc = (byte)((crc << 1) & 0xFF);
                }
            }

            return crc;
        }
        private void ContinuousTimer_Tick(object sender, EventArgs e)
        {
            // 临时关闭文本显示，避免大量输出
            bool oldShowFlag = showDebugInfo;
            showDebugInfo = false;

            PerformSingleRead();   // 复用单次读取逻辑，但内部会检查 showDebugInfo 标志

            showDebugInfo = oldShowFlag;
        }
        private void PerformSingleRead()
        {
            if (SerialPortManager.sp == null || !SerialPortManager.sp.IsOpen)
            {
                if (showDebugInfo)
                    MessageBox.Show("请先打开串口！");
                return;
            }

            try
            {
                SerialPortManager.sp.DiscardInBuffer();
                byte[] cmd = new byte[] { 0x02 };
                SerialPortManager.WriteData(cmd);

                if (showDebugInfo)
                    textBox1.AppendText("TX: " + BitConverter.ToString(cmd) + Environment.NewLine);

                System.Threading.Thread.Sleep(50);

                int count = SerialPortManager.sp.BytesToRead;
                if (count < 6)
                {
                    if (showDebugInfo)
                        textBox1.AppendText("接受数据错误" + Environment.NewLine);
                    return;
                }

                byte[] rx = new byte[6];
                SerialPortManager.sp.Read(rx, 0, 6);

                if (showDebugInfo)
                    textBox1.AppendText("RX: " + BitConverter.ToString(rx) + Environment.NewLine);

                byte cf = rx[0];
                byte sf = rx[1];

                uint abs = ((uint)rx[2] << 16) | ((uint)rx[3] << 8) | rx[4];
                abs &= 0x7FFFF;

                byte recvCrc = rx[5];
                byte calcCrc = CalcCRC(new byte[] { rx[0], rx[1], rx[2], rx[3], rx[4] });

                if (showDebugInfo)
                {
                    textBox1.AppendText($"ABS:{abs}(0x{abs:X6}) CRC:{(recvCrc == calcCrc ? "PASS" : "FAIL")}" + Environment.NewLine);
                }

                double angle = abs * 360.0 / 524288.0;

                // 更新仪表盘控件（确保 UI 线程安全）
                /*if (dialControl1.InvokeRequired)
                    dialControl1.Invoke(new Action(() => dialControl1.UpdateAngle(angle)));
                else
                    dialControl1.UpdateAngle(angle);*/

                // 不需要任何 Invoke 或 Dispatcher
                dialControl1.UpdateAngle(angle);

                if (showDebugInfo)
                {
                    textBox1.AppendText($"Angle   : {angle:F4}°" + Environment.NewLine);
                    textBox1.AppendText(Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                if (showDebugInfo)
                    MessageBox.Show("通讯错误：" + ex.Message);
            }
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                // 开启连续读取
                showDebugInfo = false;           // 连续模式下不输出文本
                continuousTimer.Start();
            }
            else
            {
                // 关闭连续读取
                continuousTimer.Stop();
                showDebugInfo = true;            // 恢复显示
            }
        }
    }
}