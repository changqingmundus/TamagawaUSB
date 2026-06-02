using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagawaUSB
{
        public static class SerialPortManager
        {
            public static SerialPort sp;
            public static bool OpenPort(string comPort, int baudRate, SerialDataReceivedEventHandler dataReceivedHandler)
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
}
