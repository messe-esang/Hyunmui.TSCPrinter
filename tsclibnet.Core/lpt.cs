// Decompiled with JetBrains decompiler
// Type: TSCSDK.lpt
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using Microsoft.Win32.SafeHandles;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;



namespace TSCSDK
{
    public class lpt
    {
        public const short FILE_ATTRIBUTE_NORMAL = 128;
        public const short INVALID_HANDLE_VALUE = -1;
        public const uint GENERIC_READ = 2147483648;
        public const uint GENERIC_WRITE = 1073741824;
        public const uint CREATE_NEW = 1;
        public const uint CREATE_ALWAYS = 2;
        public const uint OPEN_EXISTING = 3;
        private SafeFileHandle ptr;
        private FileStream lptstream;
        private readonly IWindowsFontGdi fontGdi;

        public lpt() : this(null, new WindowsFontGdi()) { }

        internal lpt(FileStream stream, IWindowsFontGdi native)
        {
            this.lptstream = stream;
            this.fontGdi = native;
        }
        private static byte[] CRLF_byte = new byte[2]
        {
      (byte) 13,
      (byte) 10
        };
        private static byte[] buffer = new byte[2048];
        private const int OUT_DEFAULT_PRECIS = 0;
        private const int CLIP_DEFAULT_PRECIS = 0;
        private const int BUFFER_WIDTH = 2400;
        private const int BUFFER_HEIGHT = 2400;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
          string lpFileName,
          uint dwDesiredAccess,
          uint dwShareMode,
          IntPtr lpSecurityAttributes,
          uint dwCreationDisposition,
          uint dwFlagsAndAttributes,
          IntPtr hTemplateFile);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] lpt.LOGFONT lplf);

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

        public bool openport(string port)
        {
            this.ptr = lpt.CreateFile(port, 1073741824U, 0U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
            this.lptstream = new FileStream(this.ptr, FileAccess.ReadWrite);
            return port != null;
        }

        public void sendcommand(string command)
        {
            if (this.lptstream == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            lpt.buffer = Encoding.ASCII.GetBytes(command);
            this.lptstream.Write(lpt.buffer, 0, lpt.buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public void sendcommand(string[] command)
        {
            for (int index = 0; index < command.Length; ++index)
            {
                if (command[index] != "")
                {
                    this.sendcommand(Encoding.Default.GetBytes(command[index]));
                    this.sendcommand(lpt.CRLF_byte);
                }
            }
        }

        public int sendcommand_hex(string hex_data)
        {
            if (this.lptstream == null)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return 0;
            }
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
            this.sendcommand(Encoding.ASCII.GetBytes(s));
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
            this.sendcommand(command);
            return 1;
        }

        public void sendcommand_utf8(string command)
        {
            if (this.lptstream == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            lpt.buffer = Encoding.UTF8.GetBytes(command);
            this.lptstream.Write(lpt.buffer, 0, lpt.buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public void sendcommand_gb2312(string command)
        {
            if (this.lptstream == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            lpt.buffer = Encoding.GetEncoding("gb2312").GetBytes(command);
            this.lptstream.Write(lpt.buffer, 0, lpt.buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public void sendcommand_big5(string command)
        {
            if (this.lptstream == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            lpt.buffer = Encoding.GetEncoding("big5").GetBytes(command);
            this.lptstream.Write(lpt.buffer, 0, lpt.buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public int sendcommandNOCRLF(string command)
        {
            if (this.lptstream == null)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return 0;
            }
            lpt.buffer = Encoding.UTF8.GetBytes(command);
            this.lptstream.Write(lpt.buffer, 0, lpt.buffer.Length);
            return 1;
        }

        public void sendcommand(byte[] command)
        {
            if (this.lptstream == null)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            this.lptstream.Write(command, 0, command.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public void sendcommandNOCRLF(byte[] command)
        {
            this.lptstream.Write(command, 0, command.Length);
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
            this.lptstream.Write(bytes1, 0, bytes1.Length);
            this.lptstream.Write(bytes2, 0, bytes2.Length);
            this.lptstream.Write(bytes3, 0, bytes3.Length);
            this.lptstream.Write(bytes4, 0, bytes4.Length);
        }

        public void clearbuffer()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("CLS\r\n");
            this.lptstream.Write(bytes, 0, bytes.Length);
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
            this.lptstream.Write(bytes, 0, bytes.Length);
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
            this.lptstream.Write(bytes, 0, bytes.Length);
        }

        public void printlabel(string a, string b)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("PRINT " + a + ", " + b + "\r\n");
            this.lptstream.Write(bytes, 0, bytes.Length);
        }

        public void formfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("FORMFEED\r\n");
            this.lptstream.Write(bytes, 0, bytes.Length);
        }

        public void nobackfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("SET TEAR OFF\r\n");
            this.lptstream.Write(bytes, 0, bytes.Length);
        }

        public int downloadfile(string filename, string downloadname)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                this.lptstream.Write(bytes, 0, bytes.Length);
                this.lptstream.Write(buffer, 0, buffer.Length);
                this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
            }
            catch (IOException ex)
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
                this.lptstream.Write(bytes, 0, bytes.Length);
                this.lptstream.Write(buffer, 0, buffer.Length);
                this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
            }
            catch (IOException ex)
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
            this.lptstream.Write(bytes, 0, bytes.Length);
            this.lptstream.Write(buffer, 0, buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public void downloadbmp(string filename, string imagename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            this.lptstream.Write(bytes, 0, bytes.Length);
            this.lptstream.Write(buffer, 0, buffer.Length);
            this.lptstream.Write(lpt.CRLF_byte, 0, lpt.CRLF_byte.Length);
        }

        public string about()
        {
            return "This is .NET SDL V1.0";
        }

        public void printerrestart()
        {
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 82
            };
            this.lptstream.Write(buffer, 0, buffer.Length);
        }

        public void closeport() => this.lptstream.Close();

        public void closeport(int delay)
        {
            Thread.Sleep(delay);
            this.lptstream.Close();
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
                X = x, Y = y, Height = fontheight, Rotation = rotation,
                Style = fontstyle, FaceName = szFaceName, Content = content, Unicode = false
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
                X = x, Y = y, Height = fontheight, Rotation = rotation,
                Style = fontstyle, FaceName = szFaceName, Content = content, Unicode = true
            });
        }

        private void SendWindowsFont(WindowsFontRequest request)
        {
            var stream = this.lptstream;
            WindowsFontCommand.Send(request, this.fontGdi, (packet, offset, count) =>
            {
                stream.Write(packet, offset, count);
                return count;
            });
        }
        public void printphoto(int xpoint, int ypoint, string filename)
        {
            BitmapCommand.SendFile(xpoint, ypoint, filename,
                header => this.sendcommandNOCRLF(header), data => this.sendcommand(data));
        }

        public void sendpicture(int xpoint, int ypoint, string filename)
        {
            BitmapCommand.SendFile(xpoint, ypoint, filename,
                header => this.sendcommandNOCRLF(header), data => this.sendcommand(data));
        }

        public void sendpicture(int xpoint, int ypoint, Bitmap original_picture)
        {
            BitmapCommand.Send(xpoint, ypoint, original_picture,
                header => this.sendcommandNOCRLF(header), data => this.sendcommand(data));
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
                    this.sendcommand(numArray);
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
                    this.sendcommandNOCRLF(numArray);
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
            byte[] command = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 82
            };
            this.sendcommand("WLAN DEFAULT\r\n");
            this.sendcommand(command);
            return "1";
        }

        public string WiFi_SSID(string SSID)
        {
            this.sendcommand("WLAN SSID \"" + SSID + "\"\r\n");
            return "1";
        }

        public string WiFi_WPA(string WPA)
        {
            this.sendcommand("WLAN WPA \"" + WPA + "\"\r\n");
            return "1";
        }

        public string WiFi_WEP(int number, string WEP)
        {
            this.sendcommand("WLAN WEP " + number.ToString() + ",\"" + WEP + "\"\r\n");
            return "1";
        }

        public string WiFi_DHCP()
        {
            this.sendcommand("WLAN DHCP\r\n");
            return "1";
        }

        public string WiFi_Port(int port)
        {
            this.sendcommand("WLAN PORT " + port.ToString() + "\r\n");
            return "1";
        }

        public string WiFi_StaticIP(string ip, string mask, string gateway)
        {
            this.sendcommand("WLAN IP \"" + ip + "\",\"" + mask + "\",\"" + gateway + "\"\r\n");
            return "1";
        }

        public void send_bitmap(int x_axis, int y_axis, Bitmap bitmap_file)
        {
            this.sendpicture(x_axis, y_axis, bitmap_file);
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

            public static implicit operator System.Drawing.Rectangle(lpt.RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator lpt.RECT(System.Drawing.Rectangle r) => new lpt.RECT(r);

            public static bool operator ==(lpt.RECT r1, lpt.RECT r2) => r1.Equals(r2);

            public static bool operator !=(lpt.RECT r1, lpt.RECT r2) => !r1.Equals(r2);

            public bool Equals(lpt.RECT r)
            {
                return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
            }

            public override bool Equals(object obj)
            {
                switch (obj)
                {
                    case lpt.RECT r1:
                        return this.Equals(r1);
                    case System.Drawing.Rectangle r2:
                        return this.Equals(new lpt.RECT(r2));
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
