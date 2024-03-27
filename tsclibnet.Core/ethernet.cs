// Decompiled with JetBrains decompiler
// Type: TSCSDK.ethernet
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;



namespace TSCSDK
{
    public class ethernet
    {
        private static Socket tempSocket = (Socket)null;
        private static Socket tempSocket1 = (Socket)null;
        private static Socket tempSocket2 = (Socket)null;
        private static Socket tempSocket3 = (Socket)null;
        private static Socket tempSocket4 = (Socket)null;
        private static Socket tempSocket5 = (Socket)null;
        private static EndPoint ipe = (EndPoint)null;
        private static EndPoint ipe1 = (EndPoint)null;
        private static EndPoint ipe2 = (EndPoint)null;
        private static EndPoint ipe3 = (EndPoint)null;
        private static EndPoint ipe4 = (EndPoint)null;
        private static EndPoint ipe5 = (EndPoint)null;
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
        private int sleep_time;
        private int file_total_length;
        private static string byte_to_string = "";
        private static string read_string = "";
        private static string[] diag_array = new string[1024];
        private static byte[] load_buffer = new byte[1024];

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] ethernet.LOGFONT lplf);

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
        private static extern int FillRect(IntPtr hDC, [In] ref ethernet.RECT lprc, IntPtr hbr);

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
          out ethernet.SIZE lpSize);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32W(
          IntPtr hdc,
          string lpWString,
          int cbString,
          out ethernet.SIZE lpSize);

        public bool openport(string ipaddress, int port)
        {
            ethernet.ipe = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
            ethernet.tempSocket = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ethernet.tempSocket.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
            ethernet.tempSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
            ethernet.tempSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
            Thread.Sleep(200);
            return ethernet.tempSocket.Connected;
        }

        public bool openport(string ipaddress, int port, int delay)
        {
            ethernet.ipe = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
            ethernet.tempSocket = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ethernet.tempSocket.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
            ethernet.tempSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            ethernet.tempSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
            Thread.Sleep(delay);
            return ethernet.tempSocket.Connected;
        }

        public int openport_mult(int port, string ipaddress, int portnumber)
        {
            switch (port)
            {
                case 1:
                    ethernet.ipe1 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
                    ethernet.tempSocket1 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket1.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket1.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket1.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    return 1;
                case 2:
                    ethernet.ipe2 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
                    ethernet.tempSocket2 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket2.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    return 1;
                case 3:
                    ethernet.ipe3 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
                    ethernet.tempSocket3 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket3.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket3.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket3.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    return 1;
                case 4:
                    ethernet.ipe4 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
                    ethernet.tempSocket4 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket4.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    return 1;
                case 5:
                    ethernet.ipe5 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), port);
                    ethernet.tempSocket5 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket5.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket5.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket5.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    return 1;
                default:
                    return 0;
            }
        }

        public int openport_mult(int port, string ipaddress, int portnumber, int delay)
        {
            switch (port)
            {
                case 1:
                    ethernet.ipe1 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), portnumber);
                    ethernet.tempSocket1 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket1.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket1.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket1.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    Thread.Sleep(delay);
                    return 1;
                case 2:
                    ethernet.ipe2 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), portnumber);
                    ethernet.tempSocket2 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket2.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    Thread.Sleep(delay);
                    return 1;
                case 3:
                    ethernet.ipe3 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), portnumber);
                    ethernet.tempSocket3 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket3.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket3.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket3.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    Thread.Sleep(delay);
                    return 1;
                case 4:
                    ethernet.ipe4 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), portnumber);
                    ethernet.tempSocket4 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket4.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    Thread.Sleep(delay);
                    return 1;
                case 5:
                    ethernet.ipe5 = (EndPoint)new IPEndPoint(IPAddress.Parse(ipaddress), portnumber);
                    ethernet.tempSocket5 = new Socket(ethernet.ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    ethernet.tempSocket5.BeginConnect(ethernet.ipe, (AsyncCallback)null, (object)null);
                    ethernet.tempSocket5.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    ethernet.tempSocket5.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
                    Thread.Sleep(delay);
                    return 1;
                default:
                    return 0;
            }
        }

        public void closeport() => ethernet.tempSocket.Close();

        public void closeport(int delay)
        {
            Thread.Sleep(delay);
            ethernet.tempSocket.Close();
        }

        public int closeport_mult(int portnumber, int delay)
        {
            switch (portnumber)
            {
                case 1:
                    Thread.Sleep(delay);
                    ethernet.tempSocket1.Close();
                    return 1;
                case 2:
                    Thread.Sleep(delay);
                    ethernet.tempSocket2.Close();
                    return 1;
                case 3:
                    Thread.Sleep(delay);
                    ethernet.tempSocket3.Close();
                    return 1;
                case 4:
                    Thread.Sleep(delay);
                    ethernet.tempSocket4.Close();
                    return 1;
                case 5:
                    Thread.Sleep(delay);
                    ethernet.tempSocket5.Close();
                    return 1;
                default:
                    return 0;
            }
        }

        public void sendcommand(string command)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
        }

        public int sendcommand_hex(string hex_data)
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

        public void sendcommand(string[] command)
        {
            for (int index = 0; index < command.Length; ++index)
            {
                if (command[index] != "")
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(command[index]);
                    Encoding.ASCII.GetBytes(ethernet.CRLF);
                    this.sendcommand(bytes);
                    this.sendcommand(ethernet.CRLF_byte);
                }
            }
        }

        public void sendcommand_utf8(string command)
        {
            byte[] bytes1 = Encoding.UTF8.GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
        }

        public void sendcommand_gb2312(string command)
        {
            byte[] bytes1 = Encoding.GetEncoding("gb2312").GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
        }

        public void sendcommand_big5(string command)
        {
            byte[] bytes1 = Encoding.GetEncoding("big5").GetBytes(command);
            byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
        }

        public string sendcommand_getstring(string command)
        {
            byte[] numArray = new byte[256];
            byte[] bytes1 = Encoding.ASCII.GetBytes(command);
            Encoding.ASCII.GetBytes(ethernet.CRLF);
            byte[] bytes2 = Encoding.Default.GetBytes("OUT \"ENDLINE\"\r\n");
            this.sendcommand(bytes1);
            this.sendcommand(bytes2);
            this.ReadToStream(1000, "ENDLINE\r\n");
            return ethernet.byte_to_string;
        }

        public int sendcommand_mult(int portnumber, string command)
        {
            switch (portnumber)
            {
                case 1:
                    byte[] bytes1 = Encoding.ASCII.GetBytes(command);
                    byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket1.Send(bytes1, bytes1.Length, SocketFlags.None);
                    ethernet.tempSocket1.Send(bytes2, bytes2.Length, SocketFlags.None);
                    return 1;
                case 2:
                    byte[] bytes3 = Encoding.ASCII.GetBytes(command);
                    byte[] bytes4 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket2.Send(bytes3, bytes3.Length, SocketFlags.None);
                    ethernet.tempSocket2.Send(bytes4, bytes4.Length, SocketFlags.None);
                    return 1;
                case 3:
                    byte[] bytes5 = Encoding.ASCII.GetBytes(command);
                    byte[] bytes6 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket3.Send(bytes5, bytes5.Length, SocketFlags.None);
                    ethernet.tempSocket3.Send(bytes6, bytes6.Length, SocketFlags.None);
                    return 1;
                case 4:
                    byte[] bytes7 = Encoding.ASCII.GetBytes(command);
                    byte[] bytes8 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket4.Send(bytes7, bytes7.Length, SocketFlags.None);
                    ethernet.tempSocket4.Send(bytes8, bytes8.Length, SocketFlags.None);
                    return 1;
                case 5:
                    byte[] bytes9 = Encoding.ASCII.GetBytes(command);
                    byte[] bytes10 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket5.Send(bytes9, bytes9.Length, SocketFlags.None);
                    ethernet.tempSocket5.Send(bytes10, bytes10.Length, SocketFlags.None);
                    return 1;
                default:
                    return 0;
            }
        }

        public int sendcommandNOCRLF(string command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(command);
            Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            return 1;
        }

        public void sendcommand(byte[] command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(command, command.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
        }

        public int sendcommand_mult(int portnumber, byte[] command)
        {
            switch (portnumber)
            {
                case 1:
                    byte[] bytes1 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket1.Send(command, command.Length, SocketFlags.None);
                    ethernet.tempSocket1.Send(bytes1, bytes1.Length, SocketFlags.None);
                    return 1;
                case 2:
                    byte[] bytes2 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket2.Send(command, command.Length, SocketFlags.None);
                    ethernet.tempSocket2.Send(bytes2, bytes2.Length, SocketFlags.None);
                    return 1;
                case 3:
                    byte[] bytes3 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket3.Send(command, command.Length, SocketFlags.None);
                    ethernet.tempSocket3.Send(bytes3, bytes3.Length, SocketFlags.None);
                    return 1;
                case 4:
                    byte[] bytes4 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket4.Send(command, command.Length, SocketFlags.None);
                    ethernet.tempSocket4.Send(bytes4, bytes4.Length, SocketFlags.None);
                    return 1;
                case 5:
                    byte[] bytes5 = Encoding.ASCII.GetBytes(ethernet.CRLF);
                    ethernet.tempSocket5.Send(command, command.Length, SocketFlags.None);
                    ethernet.tempSocket5.Send(bytes5, bytes5.Length, SocketFlags.None);
                    return 1;
                default:
                    return 0;
            }
        }

        public void sendcommandNOCRLF(byte[] command)
        {
            Encoding.ASCII.GetBytes(ethernet.CRLF);
            ethernet.tempSocket.Send(command, command.Length, SocketFlags.None);
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
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes3, bytes3.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes4, bytes4.Length, SocketFlags.None);
        }

        public void clearbuffer()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("CLS\r\n");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
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
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
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
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
        }

        public void printlabel(string a, string b)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("PRINT " + a + ", " + b + "\r\n");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
        }

        public void formfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("FORMFEED\r\n");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
        }

        public void nobackfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("SET TEAR OFF\r\n");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
        }

        public int downloadfile(string filename, string downloadname)
        {
            byte[] buffer = System.IO.File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
                ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
                ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                return 0;
            }
            return 1;
        }

        public int downloadfile(string filename, string location, string downloadname)
        {
            byte[] buffer = System.IO.File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD " + location + ",\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
                ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
                ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                return 0;
            }
            return 1;
        }

        public void downloadpcx(string filename, string imagename)
        {
            byte[] buffer = System.IO.File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
        }

        public void downloadbmp(string filename, string imagename)
        {
            byte[] buffer = System.IO.File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
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
            ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
        }

        public byte printerstatus()
        {
            byte[] buffer1 = new byte[256];
            byte[] buffer2 = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            if (!ethernet.tempSocket.Connected)
                return 99;
            ethernet.tempSocket.Send(buffer2, buffer2.Length, SocketFlags.None);
            Thread.Sleep(1000);
            do
                ;
            while (ethernet.tempSocket.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            return buffer1[0];
        }

        public string printerstatus_string()
        {
            byte[] buffer1 = new byte[256];
            byte[] buffer2 = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            if (!ethernet.tempSocket.Connected)
                return "error";
            ethernet.tempSocket.Send(buffer2, buffer2.Length, SocketFlags.None);
            Thread.Sleep(1000);
            do
                ;
            while (ethernet.tempSocket.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            if (buffer1[0] == (byte)0)
                return "00";
            if (buffer1[0] == (byte)1)
                return "01";
            if (buffer1[0] == (byte)2)
                return "02";
            if (buffer1[0] == (byte)3)
                return "03";
            if (buffer1[0] == (byte)4)
                return "04";
            if (buffer1[0] == (byte)5)
                return "05";
            if (buffer1[0] == (byte)8)
                return "08";
            if (buffer1[0] == (byte)9)
                return "09";
            if (buffer1[0] == (byte)10)
                return "0A";
            if (buffer1[0] == (byte)11)
                return "0B";
            if (buffer1[0] == (byte)12)
                return "0C";
            if (buffer1[0] == (byte)13)
                return "0D";
            if (buffer1[0] == (byte)16)
                return "10";
            if (buffer1[0] == (byte)32)
                return "20";
            return buffer1[0] == (byte)128 ? "80" : "other error";
        }

        public string printerstatus_string(int delay)
        {
            byte[] buffer1 = new byte[256];
            byte[] buffer2 = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            if (!ethernet.tempSocket.Connected)
                return "error";
            ethernet.tempSocket.Send(buffer2, buffer2.Length, SocketFlags.None);
            Thread.Sleep(delay);
            do
                ;
            while (ethernet.tempSocket.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            if (buffer1[0] == (byte)0)
                return "00";
            if (buffer1[0] == (byte)1)
                return "01";
            if (buffer1[0] == (byte)2)
                return "02";
            if (buffer1[0] == (byte)3)
                return "03";
            if (buffer1[0] == (byte)4)
                return "04";
            if (buffer1[0] == (byte)5)
                return "05";
            if (buffer1[0] == (byte)8)
                return "08";
            if (buffer1[0] == (byte)9)
                return "09";
            if (buffer1[0] == (byte)10)
                return "0A";
            if (buffer1[0] == (byte)11)
                return "0B";
            if (buffer1[0] == (byte)12)
                return "0C";
            if (buffer1[0] == (byte)13)
                return "0D";
            if (buffer1[0] == (byte)16)
                return "10";
            if (buffer1[0] == (byte)32)
                return "20";
            return buffer1[0] == (byte)128 ? "80" : "other error";
        }

        public byte printerstatus_mult(int portnumber)
        {
            switch (portnumber)
            {
                case 1:
                    byte[] buffer1 = new byte[256];
                    byte[] buffer2 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket1.Connected)
                        return 99;
                    ethernet.tempSocket1.Send(buffer2, buffer2.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    do
                        ;
                    while (ethernet.tempSocket1.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket1.Poll(5000, SelectMode.SelectRead));
                    return buffer1[0];
                case 2:
                    byte[] buffer3 = new byte[256];
                    byte[] buffer4 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket2.Connected)
                        return 99;
                    ethernet.tempSocket2.Send(buffer4, buffer4.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    do
                        ;
                    while (ethernet.tempSocket2.ReceiveFrom(buffer3, ref ethernet.ipe) > 0 && ethernet.tempSocket2.Poll(5000, SelectMode.SelectRead));
                    return buffer3[0];
                case 3:
                    byte[] buffer5 = new byte[256];
                    byte[] buffer6 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket3.Connected)
                        return 99;
                    ethernet.tempSocket3.Send(buffer6, buffer6.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    do
                        ;
                    while (ethernet.tempSocket3.ReceiveFrom(buffer5, ref ethernet.ipe) > 0 && ethernet.tempSocket3.Poll(5000, SelectMode.SelectRead));
                    return buffer5[0];
                case 4:
                    byte[] buffer7 = new byte[256];
                    byte[] buffer8 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket4.Connected)
                        return 99;
                    ethernet.tempSocket4.Send(buffer8, buffer8.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    do
                        ;
                    while (ethernet.tempSocket4.ReceiveFrom(buffer7, ref ethernet.ipe) > 0 && ethernet.tempSocket4.Poll(5000, SelectMode.SelectRead));
                    return buffer7[0];
                case 5:
                    byte[] buffer9 = new byte[256];
                    byte[] buffer10 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket5.Connected)
                        return 99;
                    ethernet.tempSocket5.Send(buffer10, buffer10.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    do
                        ;
                    while (ethernet.tempSocket5.ReceiveFrom(buffer9, ref ethernet.ipe) > 0 && ethernet.tempSocket5.Poll(5000, SelectMode.SelectRead));
                    return buffer9[0];
                default:
                    return 99;
            }
        }

        public string printerstatus_string_mult(int portnumber)
        {
            byte[] buffer1 = new byte[256];
            switch (portnumber)
            {
                case 1:
                    buffer1 = new byte[256];
                    byte[] buffer2 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket1.Connected)
                        return "error";
                    ethernet.tempSocket1.Send(buffer2, buffer2.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    while (ethernet.tempSocket1.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket1.Poll(5000, SelectMode.SelectRead))
                        ;
                    break;
                case 2:
                    buffer1 = new byte[256];
                    byte[] buffer3 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket2.Connected)
                        return "error";
                    ethernet.tempSocket2.Send(buffer3, buffer3.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    while (ethernet.tempSocket2.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket2.Poll(5000, SelectMode.SelectRead))
                        ;
                    break;
                case 3:
                    buffer1 = new byte[256];
                    byte[] buffer4 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket3.Connected)
                        return "error";
                    ethernet.tempSocket3.Send(buffer4, buffer4.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    while (ethernet.tempSocket3.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket3.Poll(5000, SelectMode.SelectRead))
                        ;
                    break;
                case 4:
                    buffer1 = new byte[256];
                    byte[] buffer5 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket4.Connected)
                        return "error";
                    ethernet.tempSocket4.Send(buffer5, buffer5.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    while (ethernet.tempSocket4.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket4.Poll(5000, SelectMode.SelectRead))
                        ;
                    break;
                case 5:
                    buffer1 = new byte[256];
                    byte[] buffer6 = new byte[3]
                    {
            (byte) 27,
            (byte) 33,
            (byte) 63
                    };
                    if (!ethernet.tempSocket5.Connected)
                        return "error";
                    ethernet.tempSocket5.Send(buffer6, buffer6.Length, SocketFlags.None);
                    Thread.Sleep(1000);
                    while (ethernet.tempSocket5.ReceiveFrom(buffer1, ref ethernet.ipe) > 0 && ethernet.tempSocket5.Poll(5000, SelectMode.SelectRead))
                        ;
                    break;
            }
            if (buffer1[0] == (byte)0)
                return "00";
            if (buffer1[0] == (byte)1)
                return "01";
            if (buffer1[0] == (byte)2)
                return "02";
            if (buffer1[0] == (byte)3)
                return "03";
            if (buffer1[0] == (byte)4)
                return "04";
            if (buffer1[0] == (byte)5)
                return "05";
            if (buffer1[0] == (byte)8)
                return "08";
            if (buffer1[0] == (byte)9)
                return "09";
            if (buffer1[0] == (byte)10)
                return "0A";
            if (buffer1[0] == (byte)11)
                return "0B";
            if (buffer1[0] == (byte)12)
                return "0C";
            if (buffer1[0] == (byte)13)
                return "0D";
            if (buffer1[0] == (byte)16)
                return "10";
            if (buffer1[0] == (byte)32)
                return "20";
            return buffer1[0] == (byte)128 ? "80" : "other error";
        }

        public string printersetting(string app, string sec, string key, int delay)
        {
            string s = "OUT GETSETTING$(\"" + app + "\",\"" + sec + "\",\"" + key + "\")";
            byte[] bytes1 = Encoding.Default.GetBytes("OUT CHR$(06)");
            byte[] bytes2 = Encoding.ASCII.GetBytes(s);
            byte judgement = 6;
            if (!ethernet.tempSocket.Connected)
                return "error";
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            if (this.ReadToStream(delay, judgement))
                ;
            return ethernet.byte_to_string;
        }

        public string printersetting(string app, string sec, string key, int delay1, int delay2)
        {
            string s = "OUT GETSETTING$(\"" + app + "\",\"" + sec + "\",\"" + key + "\")";
            byte[] bytes1 = Encoding.Default.GetBytes("OUT CHR$(06)");
            byte[] bytes2 = Encoding.ASCII.GetBytes(s);
            byte judgement = 6;
            if (!ethernet.tempSocket.Connected)
                return "error";
            ethernet.tempSocket.Send(bytes2, bytes2.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            ethernet.tempSocket.Send(bytes1, bytes1.Length, SocketFlags.None);
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
            Thread.Sleep(delay1);
            if (this.ReadToStream(delay2, judgement))
                ;
            return ethernet.byte_to_string;
        }

        public string printersetting_mult(int portnumber, string app, string sec, string key)
        {
            byte[] numArray1 = new byte[256];
            byte[] bytes = Encoding.ASCII.GetBytes("OUT GETSETTING$(\"" + app + "\",\"" + sec + "\",\"" + key + "\")");
            byte judgement = 6;
            switch (portnumber)
            {
                case 1:
                    byte[] numArray2 = new byte[256];
                    if (!ethernet.tempSocket1.Connected)
                        return "error";
                    ethernet.tempSocket1.Send(bytes, bytes.Length, SocketFlags.None);
                    if (this.ReadToStream(1000, judgement))
                        break;
                    break;
                case 2:
                    byte[] numArray3 = new byte[256];
                    if (!ethernet.tempSocket2.Connected)
                        return "error";
                    ethernet.tempSocket2.Send(bytes, bytes.Length, SocketFlags.None);
                    if (this.ReadToStream(1000, judgement))
                        break;
                    break;
                case 3:
                    byte[] numArray4 = new byte[256];
                    if (!ethernet.tempSocket3.Connected)
                        return "error";
                    ethernet.tempSocket3.Send(bytes, bytes.Length, SocketFlags.None);
                    if (this.ReadToStream(1000, judgement))
                        break;
                    break;
                case 4:
                    byte[] numArray5 = new byte[256];
                    if (!ethernet.tempSocket4.Connected)
                        return "error";
                    ethernet.tempSocket4.Send(bytes, bytes.Length, SocketFlags.None);
                    if (this.ReadToStream(1000, judgement))
                        break;
                    break;
                case 5:
                    byte[] numArray6 = new byte[256];
                    if (!ethernet.tempSocket5.Connected)
                        return "error";
                    ethernet.tempSocket5.Send(bytes, bytes.Length, SocketFlags.None);
                    if (this.ReadToStream(1000, judgement))
                        break;
                    break;
            }
            return ethernet.byte_to_string;
        }

        public string printerfullstatus()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 83
            };
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str = (str + Encoding.ASCII.GetString(numArray, 0, count)).Substring(1, 4);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printerfullstatus(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] buffer = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 83
            };
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(buffer, buffer.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str = (str + Encoding.ASCII.GetString(numArray, 0, count)).Substring(1, 4);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printercodepage()
        {
            byte[] numArray = new byte[256];
            this.sendcommand(Encoding.ASCII.GetBytes("~!I"));
            this.ReadToStream(200);
            return ethernet.byte_to_string;
        }

        public string printercodepage(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!I");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printermileage()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!@");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printermileage(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!@");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printername()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!T");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printername(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!T");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printerfile()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!F");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printerfile(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!F");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printermemory()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!T");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printermemory(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("~!T");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printerserial()
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("OUT _SERIAL$\r\n");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(1000);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        public string printerserial(int delay)
        {
            byte[] numArray = new byte[256];
            string str = "";
            byte[] bytes = Encoding.ASCII.GetBytes("OUT _SERIAL$\r\n");
            if (!ethernet.tempSocket.Connected)
                return "-1";
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            Thread.Sleep(delay);
            try
            {
                do
                {
                    int count;
                    do
                    {
                        count = ethernet.tempSocket.Receive(numArray, numArray.Length, SocketFlags.None);
                    }
                    while (count <= 0);
                    str += Encoding.ASCII.GetString(numArray, 0, count);
                }
                while (ethernet.tempSocket.Poll(5000, SelectMode.SelectRead));
            }
            catch
            {
                return "-1";
            }
            return str;
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            ((Socket)result.AsyncState).EndReceive(result);
            result.AsyncWaitHandle.Close();
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
            ethernet.LOGFONT lplf = new ethernet.LOGFONT();
            ethernet.SIZE lpSize = new ethernet.SIZE();
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
            IntPtr dc = ethernet.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = ethernet.CreateCompatibleDC(dc);
            IntPtr bitmap = ethernet.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            ethernet.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = ethernet.CreateFontIndirect(lplf);
            IntPtr hgdiobj = ethernet.SelectObject(compatibleDc, fontIndirect);
            ethernet.GetTextExtentPoint32(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)ethernet.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)ethernet.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            ethernet.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            ethernet.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new ethernet.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            ethernet.FillRect(compatibleDc, ref rect, IntPtr.Zero);
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
            ethernet.TextOut_X_start = num3;
            ethernet.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : ethernet.iBitmapHeight;
            ethernet.TextOut(compatibleDc, ethernet.TextOut_X_start, ethernet.TextOut_Y_start, content, content.Length);
            ethernet.GetBitmapBits(bitmap, 5760000, ethernet.buf);
            if (!ethernet.DeleteObject(ethernet.SelectObject(compatibleDc, hgdiobj)))
            {
                // int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!ethernet.DeleteDC(compatibleDc))
            {
                // int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!ethernet.DeleteObject(bitmap))
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
            ethernet.iBitmapX = num7;
            ethernet.iBitmapY = rotation == 0 || rotation == 270 ? y : y - ethernet.iBitmapHeight;
            if (ethernet.iBitmapY < 0)
            {
                ethernet.iTop -= ethernet.iBitmapY;
                ethernet.iBitmapY = 0;
            }
            if (ethernet.iBitmapX < 0)
            {
                ethernet.imgShiftX -= (ethernet.iBitmapX - 7) / 8;
                ethernet.iBitmapX = 0;
            }
            byte[] bytes = Encoding.UTF8.GetBytes("BITMAP " + (object)ethernet.iBitmapX + "," + (object)ethernet.iBitmapY + "," + (object)(ethernet.iBitmapWidth - ethernet.imgShiftX) + "," + (object)(ethernet.iBitmapHeight - ethernet.iTop) + ",1,");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            GC.Collect();
            Encoding.Unicode.GetChars(ethernet.buf);
            for (int iTop = ethernet.iTop; iTop < ethernet.iBitmapHeight; ++iTop)
            {
                int imgShiftX = ethernet.imgShiftX;
                while (imgShiftX < ethernet.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(ethernet.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    ethernet.tempSocket.Send(numArray2, ethernet.iBitmapWidth, SocketFlags.None);
                    imgShiftX += ethernet.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
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
            ethernet.LOGFONT lplf = new ethernet.LOGFONT();
            ethernet.SIZE lpSize = new ethernet.SIZE();
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
            IntPtr dc = ethernet.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = ethernet.CreateCompatibleDC(dc);
            IntPtr bitmap = ethernet.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            ethernet.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = ethernet.CreateFontIndirect(lplf);
            IntPtr hgdiobj = ethernet.SelectObject(compatibleDc, fontIndirect);
            ethernet.GetTextExtentPoint32W(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)ethernet.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)ethernet.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            ethernet.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            ethernet.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new ethernet.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            ethernet.FillRect(compatibleDc, ref rect, IntPtr.Zero);
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
            ethernet.TextOut_X_start = num3;
            ethernet.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : ethernet.iBitmapHeight;
            ethernet.TextOutW(compatibleDc, ethernet.TextOut_X_start, ethernet.TextOut_Y_start, content, content.Length);
            ethernet.GetBitmapBits(bitmap, 5760000, ethernet.buf);
            if (!ethernet.DeleteObject(ethernet.SelectObject(compatibleDc, hgdiobj)))
            {
                // int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!ethernet.DeleteDC(compatibleDc))
            {
                // int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!ethernet.DeleteObject(bitmap))
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
            ethernet.iBitmapX = num7;
            ethernet.iBitmapY = rotation == 0 || rotation == 270 ? y : y - ethernet.iBitmapHeight;
            if (ethernet.iBitmapY < 0)
            {
                ethernet.iTop -= ethernet.iBitmapY;
                ethernet.iBitmapY = 0;
            }
            if (ethernet.iBitmapX < 0)
            {
                ethernet.imgShiftX -= (ethernet.iBitmapX - 7) / 8;
                ethernet.iBitmapX = 0;
            }
            byte[] bytes = Encoding.UTF8.GetBytes("BITMAP " + (object)ethernet.iBitmapX + "," + (object)ethernet.iBitmapY + "," + (object)(ethernet.iBitmapWidth - ethernet.imgShiftX) + "," + (object)(ethernet.iBitmapHeight - ethernet.iTop) + ",1,");
            ethernet.tempSocket.Send(bytes, bytes.Length, SocketFlags.None);
            GC.Collect();
            Encoding.Unicode.GetChars(ethernet.buf);
            for (int iTop = ethernet.iTop; iTop < ethernet.iBitmapHeight; ++iTop)
            {
                int imgShiftX = ethernet.imgShiftX;
                while (imgShiftX < ethernet.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(ethernet.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    ethernet.tempSocket.Send(numArray2, ethernet.iBitmapWidth, SocketFlags.None);
                    imgShiftX += ethernet.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            ethernet.tempSocket.Send(ethernet.CRLF_byte, ethernet.CRLF_byte.Length, SocketFlags.None);
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
            this.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap3.Width + 7) / 8) + "," + (object)bitmap3.Height + ", 0,");
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
            this.sendcommand(command);
            this.sendcommand(ethernet.CRLF_byte);
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
            this.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap3.Width + 7) / 8) + "," + (object)bitmap3.Height + ", 0,");
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
            this.sendcommand(command);
            this.sendcommand(ethernet.CRLF_byte);
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
            this.sendcommandNOCRLF("BITMAP " + (object)xpoint + "," + (object)ypoint + "," + (object)((bitmap2.Width + 7) / 8) + "," + (object)bitmap2.Height + ", 0,");
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
            this.sendcommand(command);
            this.sendcommand(ethernet.CRLF_byte);
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
            if (ethernet.tempSocket == null)
                return "-1";
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
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN SSID \"" + SSID + "\"\r\n");
            return "1";
        }

        public string WiFi_WPA(string WPA)
        {
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN WPA \"" + WPA + "\"\r\n");
            return "1";
        }

        public string WiFi_WEP(int number, string WEP)
        {
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN WEP " + number.ToString() + ",\"" + WEP + "\"\r\n");
            return "1";
        }

        public string WiFi_DHCP()
        {
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN DHCP\r\n");
            return "1";
        }

        public string WiFi_Port(int port)
        {
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN PORT " + port.ToString() + "\r\n");
            return "1";
        }

        public string WiFi_StaticIP(string ip, string mask, string gateway)
        {
            if (ethernet.tempSocket == null)
                return "-1";
            this.sendcommand("WLAN IP \"" + ip + "\",\"" + mask + "\",\"" + gateway + "\"\r\n");
            return "1";
        }

        public void send_bitmap(int x_axis, int y_axis, Bitmap bitmap_file)
        {
            this.sendpicture(x_axis, y_axis, bitmap_file);
        }

        private bool ReadToStream(int delay)
        {
            byte[] buffer = new byte[1024];
            ethernet.byte_to_string = "";
            Thread.Sleep(delay);
            if (!ethernet.tempSocket.Connected)
                return false;
label_1:
            try
            {
                int num = ethernet.tempSocket.Receive(buffer, buffer.Length, SocketFlags.None);
                if (num <= 0)
                    return true;
                for (int index = 0; index <= num - 1; ++index)
                    ethernet.byte_to_string += Convert.ToChar(buffer[index]).ToString();
                goto label_1;
            }
            catch
            {
                return false;
            }
        }

        private bool ReadToStream(int delay, byte judgement)
        {
            byte[] buffer = new byte[1024];
            ethernet.byte_to_string = "";
            if (!ethernet.tempSocket.Connected)
                return false;
label_1:
            try
            {
                int num;
                do
                {
                    Thread.Sleep(delay);
                    num = ethernet.tempSocket.Receive(buffer, buffer.Length, SocketFlags.None);
                }
                while (num <= 0);
                for (int index = 0; index <= num - 1; ++index)
                {
                    if ((int)buffer[index] == (int)judgement)
                        return true;
                    ethernet.byte_to_string += Convert.ToChar(buffer[index]).ToString();
                }
                goto label_1;
            }
            catch
            {
                return false;
            }
        }

        private bool ReadToStream(int delay, string judgement)
        {
            byte[] buffer = new byte[1024];
            ethernet.byte_to_string = "";
            Thread.Sleep(delay);
            if (!ethernet.tempSocket.Connected)
                return false;
label_1:
            try
            {
                int num;
                do
                {
                    num = ethernet.tempSocket.Receive(buffer, buffer.Length, SocketFlags.None);
                }
                while (num <= 0);
                for (int index = 0; index <= num; ++index)
                {
                    if (ethernet.byte_to_string.Contains(judgement))
                    {
                        ethernet.byte_to_string = ethernet.byte_to_string.Replace(judgement, "");
                        return true;
                    }
                    ethernet.byte_to_string += Convert.ToChar(buffer[index]).ToString();
                }
                goto label_1;
            }
            catch
            {
                return false;
            }
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
            this.sendcommand("DIAGNOSTIC INTERFACE NET\r\n");
            string str = "-1";
            switch (type)
            {
                case 0:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBSERIAL\r\n");
                    break;
                case 1:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBVOLTAGE\r\n");
                    break;
                case 2:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBREMCAPCITY\r\n");
                    break;
                case 3:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBTEMPERATURE\r\n");
                    break;
                case 4:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBDISCYCLE\r\n");
                    break;
                case 5:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBMANUDATE\r\n");
                    break;
                case 6:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBREPLACECOUNT\r\n");
                    break;
                case 7:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBLIFE\r\n");
                    break;
                case 8:
                    str = this.sendcommand_getstring("DIAGNOSTIC REPORT SMBSOH\r\n");
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

            public static implicit operator System.Drawing.Rectangle(ethernet.RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator ethernet.RECT(System.Drawing.Rectangle r)
            {
                return new ethernet.RECT(r);
            }

            public static bool operator ==(ethernet.RECT r1, ethernet.RECT r2) => r1.Equals(r2);

            public static bool operator !=(ethernet.RECT r1, ethernet.RECT r2) => !r1.Equals(r2);

            public bool Equals(ethernet.RECT r)
            {
                return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
            }

            public override bool Equals(object obj)
            {
                switch (obj)
                {
                    case ethernet.RECT r1:
                        return this.Equals(r1);
                    case System.Drawing.Rectangle r2:
                        return this.Equals(new ethernet.RECT(r2));
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
