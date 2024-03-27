// Decompiled with JetBrains decompiler
// Type: TSCSDK.comport
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;



namespace TSCSDK
{
    public class comport
    {
        private static SerialPort _serialPort;
        private static Thread receivethread = (Thread)null;
        private static string receivestring;
        private static int receivestatus = 0;
        private static string CRLF = "\r\n";
        private static byte[] CRLF_byte = new byte[2]
        {
      (byte) 13,
      (byte) 10
        };
        private static int iTop = 0;
        private static int iBitmapWidth;
        private static int iBitmapHeight;
        private static int iBitmapX;
        private static int iBitmapY;
        private static int TextOut_X_start;
        private static int TextOut_Y_start;
        private static byte[] buf = new byte[5760000];
        private static int imgShiftX = 0;
        private const int OUT_DEFAULT_PRECIS = 0;
        private const int CLIP_DEFAULT_PRECIS = 0;
        private const int BUFFER_WIDTH = 2400;
        private const int BUFFER_HEIGHT = 2400;
        private static string byte_to_string = "";
        private static byte[] readbuffer = new byte[1024];
        private static string read_data = "";

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] comport.LOGFONT lplf);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(
          int nWidth,
          int nHeight,
          uint cPlanes,
          uint cBitsPerPel,
          IntPtr lpvBits);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern uint SetBkColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(
          IntPtr hdc,
          int nLeftRect,
          int nTopRect,
          int nRightRect,
          int nBottomRect);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hDC, [In] ref comport.RECT lprc, IntPtr hbr);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern bool TextOut(
          IntPtr hdc,
          int nXStart,
          int nYStart,
          string lpString,
          int cbString);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern bool TextOutW(
          IntPtr hdc,
          int nXStart,
          int nYStart,
          string lpWString,
          int cbString);

        [DllImport("gdi32.dll")]
        private static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, [Out] byte[] lpvBits);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32(
          IntPtr hdc,
          string lpString,
          int cbString,
          out comport.SIZE lpSize);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32W(
          IntPtr hdc,
          string lpWString,
          int cbString,
          out comport.SIZE lpSize);

        public bool openport(
          string portnumber,
          string baudrate,
          string parity,
          string databit,
          string stopbit)
        {
            comport._serialPort = new SerialPort(portnumber);
            comport._serialPort.BaudRate = int.Parse(baudrate);
            if (parity.ToUpper() == "N")
                comport._serialPort.Parity = Parity.None;
            else if (parity.ToUpper() == "O")
                comport._serialPort.Parity = Parity.Odd;
            else if (parity.ToUpper() == "E")
                comport._serialPort.Parity = Parity.Even;
            if (databit.ToUpper() == "8")
                comport._serialPort.DataBits = 8;
            else if (databit.ToUpper() == "7")
                comport._serialPort.DataBits = 7;
            if (stopbit.ToUpper() == "1")
                comport._serialPort.StopBits = StopBits.One;
            else if (stopbit.ToUpper() == "1.5")
                comport._serialPort.StopBits = StopBits.OnePointFive;
            else if (stopbit.ToUpper() == "2")
                comport._serialPort.StopBits = StopBits.Two;
            comport._serialPort.Handshake = Handshake.XOnXOff;
            comport._serialPort.RtsEnable = true;
            comport._serialPort.ReadTimeout = 2000;
            comport._serialPort.WriteTimeout = 2000;
            try
            {
                comport._serialPort.Open();
                return true;
            }
            catch (IOException ex)
            {
                return false;
            }
        }

        public bool openport_read(
          string portnumber,
          string baudrate,
          string parity,
          string databit,
          string stopbit)
        {
            comport._serialPort = new SerialPort(portnumber);
            comport._serialPort.BaudRate = int.Parse(baudrate);
            if (parity.ToUpper() == "N")
                comport._serialPort.Parity = Parity.None;
            else if (parity.ToUpper() == "O")
                comport._serialPort.Parity = Parity.Odd;
            else if (parity.ToUpper() == "E")
                comport._serialPort.Parity = Parity.Even;
            if (databit.ToUpper() == "8")
                comport._serialPort.DataBits = 8;
            else if (databit.ToUpper() == "7")
                comport._serialPort.DataBits = 7;
            if (stopbit.ToUpper() == "1")
                comport._serialPort.StopBits = StopBits.One;
            else if (stopbit.ToUpper() == "1.5")
                comport._serialPort.StopBits = StopBits.OnePointFive;
            else if (stopbit.ToUpper() == "2")
                comport._serialPort.StopBits = StopBits.Two;
            comport._serialPort.Handshake = Handshake.XOnXOff;
            comport._serialPort.RtsEnable = true;
            comport._serialPort.DataReceived += new SerialDataReceivedEventHandler(comport.DataReceivedHandler);
            comport._serialPort.ReadTimeout = 2000;
            comport._serialPort.WriteTimeout = 2000;
            try
            {
                comport._serialPort.Open();
                return true;
            }
            catch (IOException ex)
            {
                return false;
            }
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            comport.read_data = ((SerialPort)sender).ReadExisting();
        }

        public static void sendcommand(string command)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
        }

        public static int sendcommand_hex(string hex_data)
        {
            string str = "";
            string s = "";
            int num = 0;
            int startIndex1 = 0;
            for (int startIndex2 = 0; startIndex2 < hex_data.Length; ++startIndex2)
            {
                str += hex_data.Substring(startIndex2, 1);
                ++num;
                if (num == 2)
                {
                    char ch = Convert.ToChar(Convert.ToUInt32(str.Substring(startIndex1, 2), 16));
                    s += string.Format("{0:X}", (object)ch);
                    num = 0;
                    startIndex1 += 2;
                }
            }
            comport.sendcommand(Encoding.ASCII.GetBytes(s));
            return 1;
        }

        public int sendcommand_hex(string[] hex_data)
        {
            string s = "";
            for (int index = 0; index < hex_data.Length; ++index)
            {
                char ch = Convert.ToChar(Convert.ToUInt32(hex_data[index].Substring(0, 2), 16));
                s += string.Format("{0:X}", (object)ch);
            }
            Encoding.ASCII.GetBytes(s);
            return 1;
        }

        public int sendASCtoHEX(string hexString)
        {
            byte[] command = new byte[hexString.Length / 2];
            for (int startIndex = 0; startIndex < hexString.Length; startIndex += 2)
                command[startIndex / 2] = Convert.ToByte(hexString.Substring(startIndex, 2), 16);
            comport.sendcommand(command);
            return 1;
        }

        public static void sendcommand(string[] command)
        {
            for (int index = 0; index < command.Length; ++index)
            {
                if (command[index] != "")
                {
                    byte[] bytes1 = Encoding.ASCII.GetBytes(command[index]);
                    byte[] bytes2 = Encoding.ASCII.GetBytes(comport.CRLF);
                    comport._serialPort.Write(bytes1, 0, bytes1.Length);
                    comport._serialPort.Write(bytes2, 0, bytes2.Length);
                }
            }
        }

        public static void sendcommand_utf8(string command)
        {
            byte[] bytes1 = Encoding.UTF8.GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
        }

        public static void sendcommand_gb2312(string command)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(command);
            byte[] bytes2 = Encoding.GetEncoding("gb2312").GetBytes(command);
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
        }

        public static void sendcommand_big5(string command)
        {
            byte[] bytes1 = Encoding.GetEncoding("big5").GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
        }

        public static string sendcommand_getstring(string command)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(comport.CRLF);
            byte[] bytes3 = Encoding.Default.GetBytes("OUT \"ENDLINE\"\r\n");
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
            comport._serialPort.Write(bytes3, 0, bytes3.Length);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.Read_judge));
            comport.receivethread.Start();
            do
            {
                Thread.Sleep(100);
            }
            while (comport.receivestatus != 1);
            return comport.byte_to_string;
        }

        public static int sendcommandNOCRLF(string command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(command);
            Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(bytes, 0, bytes.Length);
            return 1;
        }

        public static void sendcommand(byte[] command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(command, 0, command.Length);
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public static void sendcommandNOCRLF(byte[] command)
        {
            Encoding.ASCII.GetBytes(comport.CRLF);
            comport._serialPort.Write(command, 0, command.Length);
        }

        public void closeport() => comport._serialPort.Close();

        public void closeport(int delay)
        {
            Thread.Sleep(delay);
            comport._serialPort.Close();
        }

        public void setup(
          string width,
          string height,
          string speed,
          string density,
          string sensor,
          string vertical,
          string offset)
        {
            string s1 = "SIZE " + width + " mm," + height + " mm\r\n";
            string s2 = "SPEED " + speed + "\r\n";
            string s3 = "DENSITY " + density + "\r\n";
            string s4 = "";
            switch (sensor)
            {
                case "0":
                    s4 = "GAP " + vertical + " mm, " + offset + " mm\r\n";
                    break;
                case "1":
                    s4 = "BLINE " + vertical + " mm, " + offset + " mm\r\n";
                    break;
            }
            byte[] bytes1 = Encoding.ASCII.GetBytes(s1);
            byte[] bytes2 = Encoding.ASCII.GetBytes(s2);
            byte[] bytes3 = Encoding.ASCII.GetBytes(s3);
            byte[] bytes4 = Encoding.ASCII.GetBytes(s4);
            comport._serialPort.Write(bytes1, 0, bytes1.Length);
            comport._serialPort.Write(bytes2, 0, bytes2.Length);
            comport._serialPort.Write(bytes3, 0, bytes3.Length);
            comport._serialPort.Write(bytes4, 0, bytes4.Length);
        }

        public void clearbuffer()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("CLS\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public void barcode(
          string x,
          string y,
          string type,
          string height,
          string readable,
          string rotation,
          string narrow,
          string wide,
          string code)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("BARCODE " + x + "," + y + ",\"" + type + "\"," + height + "," + readable + "," + rotation + "," + narrow + "," + wide + ",\"" + code + "\"\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public void printerfont(
          string x,
          string y,
          string fonttype,
          string rotation,
          string xmul,
          string ymul,
          string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("TEXT " + x + "," + y + ",\"" + fonttype + "\"," + rotation + "," + xmul + "," + ymul + ",\"" + text + "\"\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public void printlabel(string a, string b)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("PRINT " + a + ", " + b + "\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public void formfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("FORMFEED\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public void nobackfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("SET TEAR OFF\r\n");
            comport._serialPort.Write(bytes, 0, bytes.Length);
        }

        public int downloadfile(string filename, string downloadname)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                comport._serialPort.Write(bytes, 0, bytes.Length);
                comport._serialPort.Write(buffer, 0, buffer.Length);
                comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
            }
            catch (TimeoutException ex)
            {
                return 0;
            }
            return 1;
        }

        public int downloadfile(string filename, string location, string downloadname)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD " + location + ",\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                comport._serialPort.Write(bytes, 0, bytes.Length);
                comport._serialPort.Write(buffer, 0, buffer.Length);
                comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
            }
            catch (TimeoutException ex)
            {
                return 0;
            }
            return 1;
        }

        public void downloadpcx(string filename, string imagename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            comport._serialPort.Write(bytes, 0, bytes.Length);
            comport._serialPort.Write(buffer, 0, buffer.Length);
            comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
        }

        public void downloadbmp(string filename, string imagename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            comport._serialPort.Write(bytes, 0, bytes.Length);
            comport._serialPort.Write(buffer, 0, buffer.Length);
            comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
        }

        public string about()
        {
            return "This is .NET SDL V1.0";
        }

        public static void readstream()
        {
            byte[] numArray = new byte[256];
            string str = "";
label_1:
            try
            {
                int count = comport._serialPort.Read(numArray, 0, numArray.Length);
                if (count > 0)
                {
                    comport.receivestring = str + Encoding.ASCII.GetString(numArray, 0, count);
                    comport.receivestatus = 1;
                }
                else
                    goto label_1;
            }
            catch (TimeoutException ex)
            {
            }
        }

        public static void Read_judge()
        {
            string oldValue = "ENDLINE\r\n";
            comport.byte_to_string = "";
            comport.readbuffer = new byte[1024];
            comport.receivestatus = 0;
label_1:
            try
            {
                int num = comport._serialPort.Read(comport.readbuffer, 0, comport.readbuffer.Length);
                if (num > 0)
                {
                    for (int index = 0; index <= num; ++index)
                    {
                        if (comport.byte_to_string.Contains(oldValue))
                        {
                            comport.receivestatus = 1;
                            comport.byte_to_string = comport.byte_to_string.Replace(oldValue, "");
                            return;
                        }
                        comport.byte_to_string += Convert.ToChar(comport.readbuffer[index]).ToString();
                    }
                    goto label_1;
                }
                else
                    goto label_1;
            }
            catch (TimeoutException ex)
            {
            }
        }

        public static void Read_judge_fixedstring()
        {
            byte num1 = 6;
            byte[] buffer = new byte[256];
            comport.readbuffer = new byte[1024];
label_1:
            try
            {
                int num2 = comport._serialPort.Read(buffer, 0, buffer.Length);
                if (num2 > 0)
                {
                    for (int index = 0; index <= num2 - 1; ++index)
                    {
                        if ((int)comport.readbuffer[index] == (int)num1)
                        {
                            comport.receivestatus = 1;
                            return;
                        }
                        comport.byte_to_string += Convert.ToChar(comport.readbuffer[index]).ToString();
                    }
                    goto label_1;
                }
                else
                    goto label_1;
            }
            catch (TimeoutException ex)
            {
            }
        }

        public byte printerstatus()
        {
            byte[] numArray = new byte[256];
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            string str = "";
            comport._serialPort.Write(buffer, 0, buffer.Length);
            Thread.Sleep(300);
label_1:
            try
            {
                int count = comport._serialPort.Read(numArray, 0, numArray.Length);
                if (count > 0)
                {
                    comport.receivestring = str + Encoding.ASCII.GetString(numArray, 0, count);
                    return numArray[0];
                }
                goto label_1;
            }
            catch (TimeoutException ex)
            {
            }
            return numArray[0];
        }

        public string printerfullstatus()
        {
            byte[] numArray = new byte[256];
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 83
            };
            comport._serialPort.Write(buffer, 0, buffer.Length);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printercodepage()
        {
            byte[] numArray = new byte[256];
            string str = "~!I";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printername()
        {
            byte[] numArray = new byte[256];
            string str = "~!T";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printermileage()
        {
            byte[] numArray = new byte[256];
            string str = "~!@";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printermemory()
        {
            byte[] numArray = new byte[256];
            string str = "~!A";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printerfile()
        {
            byte[] numArray = new byte[256];
            string str = "~!F";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public string printerserial()
        {
            byte[] numArray = new byte[256];
            string str = "OUT _SERIAL$\r\n";
            Encoding.ASCII.GetBytes(str);
            comport._serialPort.WriteLine(str);
            Thread.Sleep(300);
            comport.receivethread = new Thread(new ThreadStart(comport.readstream));
            comport.receivethread.Start();
            do
                ;
            while (comport.receivestatus != 1);
            return comport.receivestring;
        }

        public void printerrestart()
        {
            byte[] numArray = new byte[256];
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 82
            };
            comport._serialPort.Write(buffer, 0, buffer.Length);
        }

        public void windowsfont(
          int x,
          int y,
          int fontheight,
          int rotation,
          int fontstyle,
          int fontunderline,
          string szFaceName,
          string content)
        {
            comport.LOGFONT lplf = new comport.LOGFONT();
            comport.SIZE lpSize = new comport.SIZE();
            lplf.lfWidth = 0;
            lplf.lfEscapement = 0;
            lplf.lfOrientation = 0;
            lplf.lfCharSet = (byte)1;
            lplf.lfOutPrecision = (byte)0;
            lplf.lfClipPrecision = (byte)0;
            lplf.lfQuality = (byte)1;
            lplf.lfPitchAndFamily = (byte)26;
            lplf.lfFaceName = szFaceName;
            lplf.lfHeight = fontheight;
            lplf.lfItalic = (byte)0;
            lplf.lfUnderline = (byte)0;
            lplf.lfStrikeOut = (byte)0;
            lplf.lfWeight = fontstyle < 2 ? 400 : 700;
            lplf.lfEscapement = rotation * 10;
            IntPtr dc = comport.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = comport.CreateCompatibleDC(dc);
            IntPtr bitmap = comport.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            comport.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = comport.CreateFontIndirect(lplf);
            IntPtr hgdiobj = comport.SelectObject(compatibleDc, fontIndirect);
            comport.GetTextExtentPoint32(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)comport.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)comport.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            comport.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            comport.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new comport.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            comport.FillRect(compatibleDc, ref rect, IntPtr.Zero);
            int num3;
            switch (rotation)
            {
                case 0:
                case 90:
                    num3 = 0;
                    break;
                case 180:
                    num3 = lpSize.cx;
                    break;
                default:
                    num3 = lpSize.cy;
                    break;
            }
            comport.TextOut_X_start = num3;
            comport.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : comport.iBitmapHeight;
            comport.TextOut(compatibleDc, comport.TextOut_X_start, comport.TextOut_Y_start, content, content.Length);
            comport.GetBitmapBits(bitmap, 5760000, comport.buf);
            if (!comport.DeleteObject(comport.SelectObject(compatibleDc, hgdiobj)))
            {
                // int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!comport.DeleteDC(compatibleDc))
            {
                // int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!comport.DeleteObject(bitmap))
            {
                // int num6 = (int)MessageBox.Show("hBitmap=0", "title");
            }
            int num7;
            switch (rotation)
            {
                case 0:
                case 90:
                    num7 = x;
                    break;
                case 180:
                    num7 = x - lpSize.cx;
                    break;
                default:
                    num7 = x - lpSize.cy;
                    break;
            }
            comport.iBitmapX = num7;
            comport.iBitmapY = rotation == 0 || rotation == 270 ? y : y - comport.iBitmapHeight;
            if (comport.iBitmapY < 0)
            {
                comport.iTop -= comport.iBitmapY;
                comport.iBitmapY = 0;
            }
            if (comport.iBitmapX < 0)
            {
                comport.imgShiftX -= (comport.iBitmapX - 7) / 8;
                comport.iBitmapX = 0;
            }
            byte[] bytes = Encoding.UTF8.GetBytes("BITMAP " + (object)comport.iBitmapX + "," + (object)comport.iBitmapY + "," + (object)(comport.iBitmapWidth - comport.imgShiftX) + "," + (object)(comport.iBitmapHeight - comport.iTop) + ",1,");
            comport._serialPort.Write(bytes, 0, bytes.Length);
            GC.Collect();
            Encoding.Unicode.GetChars(comport.buf);
            for (int iTop = comport.iTop; iTop < comport.iBitmapHeight; ++iTop)
            {
                int imgShiftX = comport.imgShiftX;
                while (imgShiftX < comport.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(comport.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    comport._serialPort.Write(numArray2, 0, comport.iBitmapWidth);
                    imgShiftX += comport.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
            Marshal.Release(bitmap);
            Marshal.Release(compatibleDc);
            Marshal.Release(dc);
            GC.Collect();
        }

        public void windowsfontunicode(
          int x,
          int y,
          int fontheight,
          int rotation,
          int fontstyle,
          int fontunderline,
          string szFaceName,
          string content)
        {
            comport.LOGFONT lplf = new comport.LOGFONT();
            comport.SIZE lpSize = new comport.SIZE();
            lplf.lfWidth = 0;
            lplf.lfEscapement = 0;
            lplf.lfOrientation = 0;
            lplf.lfCharSet = (byte)1;
            lplf.lfOutPrecision = (byte)0;
            lplf.lfClipPrecision = (byte)0;
            lplf.lfQuality = (byte)1;
            lplf.lfPitchAndFamily = (byte)26;
            lplf.lfFaceName = szFaceName;
            lplf.lfHeight = fontheight;
            lplf.lfItalic = (byte)0;
            lplf.lfUnderline = (byte)0;
            lplf.lfStrikeOut = (byte)0;
            lplf.lfWeight = fontstyle < 2 ? 400 : 700;
            lplf.lfEscapement = rotation * 10;
            IntPtr dc = comport.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = comport.CreateCompatibleDC(dc);
            IntPtr bitmap = comport.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            comport.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = comport.CreateFontIndirect(lplf);
            IntPtr hgdiobj = comport.SelectObject(compatibleDc, fontIndirect);
            comport.GetTextExtentPoint32W(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)comport.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)comport.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            comport.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            comport.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new comport.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            comport.FillRect(compatibleDc, ref rect, IntPtr.Zero);
            int num3;
            switch (rotation)
            {
                case 0:
                case 90:
                    num3 = 0;
                    break;
                case 180:
                    num3 = lpSize.cx;
                    break;
                default:
                    num3 = lpSize.cy;
                    break;
            }
            comport.TextOut_X_start = num3;
            comport.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : comport.iBitmapHeight;
            comport.TextOutW(compatibleDc, comport.TextOut_X_start, comport.TextOut_Y_start, content, content.Length);
            comport.GetBitmapBits(bitmap, 5760000, comport.buf);
            if (!comport.DeleteObject(comport.SelectObject(compatibleDc, hgdiobj)))
            {
                // int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!comport.DeleteDC(compatibleDc))
            {
                // int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!comport.DeleteObject(bitmap))
            {
                // int num6 = (int)MessageBox.Show("hBitmap=0", "title");
            }
            int num7;
            switch (rotation)
            {
                case 0:
                case 90:
                    num7 = x;
                    break;
                case 180:
                    num7 = x - lpSize.cx;
                    break;
                default:
                    num7 = x - lpSize.cy;
                    break;
            }
            comport.iBitmapX = num7;
            comport.iBitmapY = rotation == 0 || rotation == 270 ? y : y - comport.iBitmapHeight;
            if (comport.iBitmapY < 0)
            {
                comport.iTop -= comport.iBitmapY;
                comport.iBitmapY = 0;
            }
            if (comport.iBitmapX < 0)
            {
                comport.imgShiftX -= (comport.iBitmapX - 7) / 8;
                comport.iBitmapX = 0;
            }
            byte[] bytes = Encoding.UTF8.GetBytes("BITMAP " + (object)comport.iBitmapX + "," + (object)comport.iBitmapY + "," + (object)(comport.iBitmapWidth - comport.imgShiftX) + "," + (object)(comport.iBitmapHeight - comport.iTop) + ",1,");
            comport._serialPort.Write(bytes, 0, bytes.Length);
            GC.Collect();
            Encoding.Unicode.GetChars(comport.buf);
            for (int iTop = comport.iTop; iTop < comport.iBitmapHeight; ++iTop)
            {
                int imgShiftX = comport.imgShiftX;
                while (imgShiftX < comport.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(comport.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    comport._serialPort.Write(numArray2, 0, comport.iBitmapWidth);
                    imgShiftX += comport.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            comport._serialPort.Write(comport.CRLF_byte, 0, comport.CRLF_byte.Length);
            Marshal.Release(bitmap);
            Marshal.Release(compatibleDc);
            Marshal.Release(dc);
            GC.Collect();
        }

        public void printphoto(int xpoint, int ypoint, string filename)
        {
            Bitmap bitmap1 = new Bitmap(filename);
            Bitmap bitmap2 = new Bitmap(bitmap1.Width, bitmap1.Height);
            for (int x = 0; x < bitmap1.Width; ++x)
            {
                for (int y = 0; y < bitmap1.Height; ++y)
                {
                    Color pixel = bitmap1.GetPixel(x, y);
                    int num = (int)((double)pixel.R * 0.3 + (double)pixel.G * 0.59 + (double)pixel.B * 0.11);
                    bitmap2.SetPixel(x, y, Color.FromArgb((int)pixel.A, num, num, num));
                }
            }
            Bitmap bitmap3 = bitmap2;
            Bitmap bitmap4 = new Bitmap(bitmap3.Width, bitmap3.Height);
            comport.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap3.Width + 7) / 8) + "," + (object)bitmap3.Height + ", 0,");
            byte[] command = new byte[(bitmap3.Width + 7) / 8 * bitmap3.Height];
            for (int index = 0; index < command.Length; ++index)
                command[index] = byte.MaxValue;
            for (int y = 0; y < bitmap3.Height; ++y)
            {
                for (int x = 0; x < bitmap3.Width; ++x)
                {
                    Color pixel = bitmap3.GetPixel(x, y);
                    int r = (int)pixel.R;
                    byte g = pixel.G;
                    byte b = pixel.B;
                    int num = (int)g;
                    if ((r + num + (int)b) / 3 < 128)
                        command[y * ((bitmap3.Width + 7) / 8) + x / 8] ^= (byte)(128 >> x % 8);
                }
            }
            comport.sendcommand(command);
            comport.sendcommand(comport.CRLF_byte);
        }

        public void sendpicture(int xpoint, int ypoint, string filename)
        {
            Bitmap bitmap1 = new Bitmap(filename);
            Bitmap bitmap2 = new Bitmap(bitmap1.Width, bitmap1.Height);
            for (int x = 0; x < bitmap1.Width; ++x)
            {
                for (int y = 0; y < bitmap1.Height; ++y)
                {
                    Color pixel = bitmap1.GetPixel(x, y);
                    int num = (int)((double)pixel.R * 0.3 + (double)pixel.G * 0.59 + (double)pixel.B * 0.11);
                    bitmap2.SetPixel(x, y, Color.FromArgb((int)pixel.A, num, num, num));
                }
            }
            Bitmap bitmap3 = bitmap2;
            Bitmap bitmap4 = new Bitmap(bitmap3.Width, bitmap3.Height);
            comport.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap3.Width + 7) / 8) + "," + (object)bitmap3.Height + ", 0,");
            byte[] command = new byte[(bitmap3.Width + 7) / 8 * bitmap3.Height];
            for (int index = 0; index < command.Length; ++index)
                command[index] = byte.MaxValue;
            for (int y = 0; y < bitmap3.Height; ++y)
            {
                for (int x = 0; x < bitmap3.Width; ++x)
                {
                    Color pixel = bitmap3.GetPixel(x, y);
                    int r = (int)pixel.R;
                    byte g = pixel.G;
                    byte b = pixel.B;
                    int num = (int)g;
                    if ((r + num + (int)b) / 3 < 128)
                        command[y * ((bitmap3.Width + 7) / 8) + x / 8] ^= (byte)(128 >> x % 8);
                }
            }
            comport.sendcommand(command);
            comport.sendcommand(comport.CRLF_byte);
        }

        public void sendpicture(int xpoint, int ypoint, Bitmap original_picture)
        {
            Bitmap bitmap1 = new Bitmap(original_picture.Width, original_picture.Height);
            for (int x = 0; x < original_picture.Width; ++x)
            {
                for (int y = 0; y < original_picture.Height; ++y)
                {
                    Color pixel = original_picture.GetPixel(x, y);
                    int num = (int)((double)pixel.R * 0.3 + (double)pixel.G * 0.59 + (double)pixel.B * 0.11);
                    bitmap1.SetPixel(x, y, Color.FromArgb((int)pixel.A, num, num, num));
                }
            }
            Bitmap bitmap2 = bitmap1;
            Bitmap bitmap3 = new Bitmap(bitmap2.Width, bitmap2.Height);
            comport.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap2.Width + 7) / 8) + "," + (object)bitmap2.Height + ", 0,");
            byte[] command = new byte[(bitmap2.Width + 7) / 8 * bitmap2.Height];
            for (int index = 0; index < command.Length; ++index)
                command[index] = byte.MaxValue;
            for (int y = 0; y < bitmap2.Height; ++y)
            {
                for (int x = 0; x < bitmap2.Width; ++x)
                {
                    Color pixel = bitmap2.GetPixel(x, y);
                    int r = (int)pixel.R;
                    byte g = pixel.G;
                    byte b = pixel.B;
                    int num = (int)g;
                    if ((r + num + (int)b) / 3 < 128)
                        command[y * ((bitmap2.Width + 7) / 8) + x / 8] ^= (byte)(128 >> x % 8);
                }
            }
            comport.sendcommand(command);
            comport.sendcommand(comport.CRLF_byte);
        }

        public int sendfile(string path)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte[] numArray = new byte[fileStream.Length];
                    int length = (int)fileStream.Length;
                    int offset = 0;
                    do
                        ;
                    while (length > 0 && fileStream.Read(numArray, offset, length) != 0);
                    comport.sendcommand(numArray);
                }
            }
            catch (FileNotFoundException ex)
            {
                return 0;
            }
            return 1;
        }

        public int sendfile_NOCRLF(string path)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte[] numArray = new byte[fileStream.Length];
                    int length = (int)fileStream.Length;
                    int offset = 0;
                    do
                        ;
                    while (length > 0 && fileStream.Read(numArray, offset, length) != 0);
                    comport.sendcommandNOCRLF(numArray);
                }
            }
            catch (FileNotFoundException ex)
            {
                return 0;
            }
            return 1;
        }

        public string search_folder_filename(string condition)
        {
            foreach (string file in Directory.GetFiles(Environment.CurrentDirectory, condition))
            {
                if (file == "" || file == null)
                    return "-1";
                this.sendfile(file);
            }
            return "1";
        }

        public string WiFi_Default()
        {
            if (comport._serialPort == null)
                return "-1";
            byte[] command = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 82
            };
            comport.sendcommand("WLAN DEFAULT\r\n");
            comport.sendcommand(command);
            return "1";
        }

        public string WiFi_SSID(string SSID)
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN SSID \"" + SSID + "\"\r\n");
            return "1";
        }

        public string WiFi_WPA(string WPA)
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN WPA \"" + WPA + "\"\r\n");
            return "1";
        }

        public string WiFi_WEP(int number, string WEP)
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN WEP " + number.ToString() + ",\"" + WEP + "\"\r\n");
            return "1";
        }

        public string WiFi_DHCP()
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN DHCP\r\n");
            return "1";
        }

        public string WiFi_Port(int port)
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN PORT " + port.ToString() + "\r\n");
            return "1";
        }

        public string WiFi_StaticIP(string ip, string mask, string gateway)
        {
            if (comport._serialPort == null)
                return "-1";
            comport.sendcommand("WLAN IP \"" + ip + "\",\"" + mask + "\",\"" + gateway + "\"\r\n");
            return "1";
        }

        public void send_bitmap(int x_axis, int y_axis, Bitmap bitmap_file)
        {
            this.sendpicture(x_axis, y_axis, bitmap_file);
        }

        private byte[] bit_array2byte_array(byte[] data)
        {
            int length = (data.Length + 7) / 8;
            byte[] numArray = new byte[length];
            for (int index = 0; index < length; ++index)
                numArray[index] = (byte)0;
            for (int index = 0; index <= data.Length - 1; ++index)
            {
                if (data[index] == (byte)1)
                    numArray[index / 8] ^= (byte)(128 >> index % 8);
            }
            return numArray;
        }

        public string SMBStatus_usb(int type)
        {
            comport.sendcommand("DIAGNOSTIC INTERFACE COM\r\n");
            string str = "-1";
            switch (type)
            {
                case 0:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBSERIAL\r\n");
                    break;
                case 1:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBVOLTAGE\r\n");
                    break;
                case 2:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBREMCAPCITY\r\n");
                    break;
                case 3:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBTEMPERATURE\r\n");
                    break;
                case 4:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBDISCYCLE\r\n");
                    break;
                case 5:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBMANUDATE\r\n");
                    break;
                case 6:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBREPLACECOUNT\r\n");
                    break;
                case 7:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBLIFE\r\n");
                    break;
                case 8:
                    str = comport.sendcommand_getstring("DIAGNOSTIC REPORT SMBSOH\r\n");
                    break;
            }
            return str.Substring(7, str.Length - 8);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class LOGFONT
        {
            public const int LF_FACESIZE = 32;
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        public struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r)
              : this(r.Left, r.Top, r.Right, r.Bottom)
            {
            }

            public int X
            {
                get => this.Left;
                set
                {
                    this.Right -= this.Left - value;
                    this.Left = value;
                }
            }

            public int Y
            {
                get => this.Top;
                set
                {
                    this.Bottom -= this.Top - value;
                    this.Top = value;
                }
            }

            public int Height
            {
                get => this.Bottom - this.Top;
                set => this.Bottom = value + this.Top;
            }

            public int Width
            {
                get => this.Right - this.Left;
                set => this.Right = value + this.Left;
            }

            public Point Location
            {
                get => new Point(this.Left, this.Top);
                set
                {
                    this.X = value.X;
                    this.Y = value.Y;
                }
            }

            public Size Size
            {
                get => new Size(this.Width, this.Height);
                set
                {
                    this.Width = value.Width;
                    this.Height = value.Height;
                }
            }

            public static implicit operator System.Drawing.Rectangle(comport.RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator comport.RECT(System.Drawing.Rectangle r)
            {
                return new comport.RECT(r);
            }

            public static bool operator ==(comport.RECT r1, comport.RECT r2) => r1.Equals(r2);

            public static bool operator !=(comport.RECT r1, comport.RECT r2) => !r1.Equals(r2);

            public bool Equals(comport.RECT r)
            {
                return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
            }

            public override bool Equals(object obj)
            {
                switch (obj)
                {
                    case comport.RECT r1:
                        return this.Equals(r1);
                    case System.Drawing.Rectangle r2:
                        return this.Equals(new comport.RECT(r2));
                    default:
                        return false;
                }
            }

            public override int GetHashCode() => ((System.Drawing.Rectangle)this).GetHashCode();

            public override string ToString()
            {
                return string.Format((IFormatProvider)CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", (object)this.Left, (object)this.Top, (object)this.Right, (object)this.Bottom);
            }
        }
    }
}
