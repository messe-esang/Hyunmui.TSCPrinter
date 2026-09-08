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
        private readonly SerialQuery serialQuery = CreateSerialQuery();

        internal comport(SerialQuery query) : this()
        {
            serialQuery = query;
        }

        private static SerialQuery CreateSerialQuery() => new SerialQuery(() => new SerialQueryPort(_serialPort));

        private static string CRLF = "\r\n";
        private static byte[] CRLF_byte = new byte[2]
        {
      (byte) 13,
      (byte) 10
        };
        private const int OUT_DEFAULT_PRECIS = 0;
        private const int CLIP_DEFAULT_PRECIS = 0;
        private const int BUFFER_WIDTH = 2400;
        private const int BUFFER_HEIGHT = 2400;

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] comport.LOGFONT lplf);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(
          IntPtr hdc,
          int nLeftRect,
          int nTopRect,
          int nRightRect,
          int nBottomRect);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC([In] IntPtr hdc);

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
            return CreateSerialQuery().QueryFramed(command);
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
            CreateSerialQuery().ReadLegacy(null);
        }

        public static void Read_judge()
        {
            CreateSerialQuery().ReadLegacy("ENDLINE\r\n");
        }

        public static void Read_judge_fixedstring()
        {
            CreateSerialQuery().ReadLegacy("\u0006");
        }

        public byte printerstatus()
        {
            return serialQuery.QueryStatus();
        }

        public string printerfullstatus()
        {
            return serialQuery.QueryFullStatus();
        }

        public string printercodepage()
        {
            return serialQuery.QueryLine("~!I");
        }

        public string printername()
        {
            return serialQuery.QueryLine("~!T");
        }

        public string printermileage()
        {
            return serialQuery.QueryLine("~!@");
        }

        public string printermemory()
        {
            return serialQuery.QueryLine("~!A");
        }

        public string printerfile()
        {
            return serialQuery.QueryLine("~!F");
        }

        public string printerserial()
        {
            return serialQuery.QueryLine("OUT _SERIAL$\r\n");
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

        private readonly IWindowsFontGdi fontGdi;
        private readonly Func<Action<byte[], int, int>> captureFontWriter;

        public comport() : this(new WindowsFontGdi(), CaptureSerialFontWriter)
        {
        }

        internal comport(IWindowsFontGdi native, Func<Action<byte[], int, int>> captureWriter)
        {
            fontGdi = native;
            captureFontWriter = captureWriter;
        }

        private static Action<byte[], int, int> CaptureSerialFontWriter()
        {
            // Capture at font-call time, not construction; the public port lifecycle remains shared.
            var port = _serialPort;
            return (packet, offset, count) => port.Write(packet, offset, count);
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
            SendWindowsFont(new WindowsFontRequest
            {
                X = x,
                Y = y,
                Height = fontheight,
                Rotation = rotation,
                Style = fontstyle,
                FaceName = szFaceName,
                Content = content,
                Unicode = false
            });
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
            SendWindowsFont(new WindowsFontRequest
            {
                X = x,
                Y = y,
                Height = fontheight,
                Rotation = rotation,
                Style = fontstyle,
                FaceName = szFaceName,
                Content = content,
                Unicode = true
            });
        }

        private void SendWindowsFont(WindowsFontRequest request)
        {
            // fontunderline stays ignored. The renderer includes exactly one trailing CRLF.
            var write = captureFontWriter();
            WindowsFontCommand.Send(request, fontGdi, (packet, offset, count) =>
            {
                write(packet, offset, count);
                return count; // SerialPort.Write either completes or throws; never retry it here.
            });
        }

        public void printphoto(int xpoint, int ypoint, string filename)
        {
            BitmapCommand.SendFile(xpoint, ypoint, filename,
                header => comport.sendcommandNOCRLF(header), data => comport.sendcommand(data));
        }

        public void sendpicture(int xpoint, int ypoint, string filename)
        {
            BitmapCommand.SendFile(xpoint, ypoint, filename,
                header => comport.sendcommandNOCRLF(header), data => comport.sendcommand(data));
        }

        public void sendpicture(int xpoint, int ypoint, Bitmap original_picture)
        {
            BitmapCommand.Send(xpoint, ypoint, original_picture,
                header => comport.sendcommandNOCRLF(header), data => comport.sendcommand(data));
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
