// Decompiled with JetBrains decompiler
// Type: TSCSDK.driver
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;



namespace TSCSDK
{
    [ComVisible(true)]
    [Guid("5855B358-A362-4813-B0A7-4A38391662EE")]
    public class driver
    {
        private static IntPtr hPrinter;
        private static IntPtr hPrinter1;
        private static IntPtr hPrinter2;
        private static IntPtr hPrinter3;
        private static IntPtr hPrinter4;
        private static IntPtr hPrinter5;
        private static byte[] CRLF_byte = new byte[2]
        {
      (byte) 13,
      (byte) 10
        };
        private static int dwWritten = 0;

        private readonly DriverCommandWriter commandWriter = new DriverCommandWriter(
            GetCommandPrinter, StartPagePrinter, WritePrinter, new DriverAnsiEncoding().GetBytes);

        internal driver(DriverCommandWriter writer) : this()
        {
            commandWriter = writer;
        }

        private static IntPtr GetCommandPrinter(int port)
        {
            switch (port)
            {
                case 0: return hPrinter;
                case 1: return hPrinter1;
                case 2: return hPrinter2;
                case 3: return hPrinter3;
                case 4: return hPrinter4;
                case 5: return hPrinter5;
                default: throw new ArgumentOutOfRangeException(nameof(port));
            }
        }
        internal delegate bool FontPrinterWrite(IntPtr printer, byte[] bytes, int count, out int written);
        private readonly IWindowsFontGdi fontGdi;
        private readonly Func<IntPtr> fontPrinterHandle;
        private readonly FontPrinterWrite fontPrinterWrite;

        public driver() : this(new WindowsFontGdi(), () => hPrinter, WritePrinter) { }

        internal driver(IWindowsFontGdi native, Func<IntPtr> printerHandle, FontPrinterWrite write)
        {
            fontGdi = native;
            fontPrinterHandle = printerHandle;
            fontPrinterWrite = write;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [MarshalAs(UnmanagedType.LPStruct), In] driver.DOCINFOA di);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool WritePrinter(
          IntPtr hPrinter,
          byte[] Bytes,
          int dwCount,
          out int dwWritten);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool ReadPrinter(
          IntPtr hPrinter,
          IntPtr pBuf,
          int cbBuf,
          out int pNoBytesRead);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool ReadPrinter(
          IntPtr hPrinter,
          byte[] Bytes,
          int cbBuf,
          out int pNoBytesRead);

        [DllImport("winspool.Drv", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern bool ReadPrinter(
          IntPtr hPrinter,
          out StringBuilder data,
          int cbBuf,
          out int pNoBytesRead);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] driver.LOGFONT lplf);

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

        public bool openport(string szPrinterName)
        {
            driver.hPrinter = new IntPtr(0);
            driver.DOCINFOA di = new driver.DOCINFOA();
            bool flag = false;
            di.pDocName = "My C#.NET RAW Document";
            di.pDataType = "RAW";
            if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter, 1, di) && driver.StartPagePrinter(driver.hPrinter))
                return true;
            if (!flag)
                Marshal.GetLastWin32Error();
            return false;
        }

        public bool openport_mult(int portnumber, string szPrinterName)
        {
            switch (portnumber)
            {
                case 1:
                    driver.hPrinter1 = new IntPtr(0);
                    driver.DOCINFOA di1 = new driver.DOCINFOA();
                    bool flag1 = false;
                    di1.pDocName = "My C#.NET RAW Document";
                    di1.pDataType = "RAW";
                    if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter1, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter1, 1, di1) && driver.StartPagePrinter(driver.hPrinter1))
                        return true;
                    if (!flag1)
                        Marshal.GetLastWin32Error();
                    return false;
                case 2:
                    driver.hPrinter2 = new IntPtr(0);
                    driver.DOCINFOA di2 = new driver.DOCINFOA();
                    bool flag2 = false;
                    di2.pDocName = "My C#.NET RAW Document";
                    di2.pDataType = "RAW";
                    if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter2, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter2, 1, di2) && driver.StartPagePrinter(driver.hPrinter2))
                        return true;
                    if (!flag2)
                        Marshal.GetLastWin32Error();
                    return false;
                case 3:
                    driver.hPrinter3 = new IntPtr(0);
                    driver.DOCINFOA di3 = new driver.DOCINFOA();
                    bool flag3 = false;
                    di3.pDocName = "My C#.NET RAW Document";
                    di3.pDataType = "RAW";
                    if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter3, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter3, 1, di3) && driver.StartPagePrinter(driver.hPrinter3))
                        return true;
                    if (!flag3)
                        Marshal.GetLastWin32Error();
                    return false;
                case 4:
                    driver.hPrinter4 = new IntPtr(0);
                    driver.DOCINFOA di4 = new driver.DOCINFOA();
                    bool flag4 = false;
                    di4.pDocName = "My C#.NET RAW Document";
                    di4.pDataType = "RAW";
                    if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter4, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter4, 1, di4) && driver.StartPagePrinter(driver.hPrinter4))
                        return true;
                    if (!flag4)
                        Marshal.GetLastWin32Error();
                    return false;
                case 5:
                    driver.hPrinter5 = new IntPtr(0);
                    driver.DOCINFOA di5 = new driver.DOCINFOA();
                    bool flag5 = false;
                    di5.pDocName = "My C#.NET RAW Document";
                    di5.pDataType = "RAW";
                    if (driver.OpenPrinter(szPrinterName.Normalize(), out driver.hPrinter5, IntPtr.Zero) && driver.StartDocPrinter(driver.hPrinter5, 1, di5) && driver.StartPagePrinter(driver.hPrinter5))
                        return true;
                    if (!flag5)
                        Marshal.GetLastWin32Error();
                    return false;
                default:
                    return false;
            }
        }

        public bool driver_status(string printerName)
        {
            using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(string.Format("SELECT * from Win32_Printer WHERE Name LIKE '%{0}'", (object)printerName)))
            {
                using (ManagementObjectCollection objectCollection = managementObjectSearcher.Get())
                {
                    try
                    {
                        foreach (ManagementBaseObject managementBaseObject in objectCollection)
                        {
                            foreach (PropertyData property in managementBaseObject.Properties)
                            {
                                if (property.Name == "WorkOffline")
                                    return property.Value.ToString() == "False";
                            }
                        }
                    }
                    catch (ManagementException ex)
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public string show_install_driver()
        {
            string str = "";
            for (int index = 0; index < PrinterSettings.InstalledPrinters.Count; ++index)
            {
                string installedPrinter = PrinterSettings.InstalledPrinters[index];
                str = str + installedPrinter + ";";
            }
            return str;
        }

        public bool sendcommand(string command)
        {
            return commandWriter.SendAnsi(0, command, true);
        }

        public void sendcommand(string[] command)
        {
            for (int index = 0; index < command.Length; ++index)
            {
                if (command[index] != "")
                {
                    this.sendcommand(Encoding.Default.GetBytes(command[index]));
                    this.sendcommand(driver.CRLF_byte);
                }
            }
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

        public bool sendcommand_utf8(string command)
        {
            return commandWriter.Send(0, Encoding.UTF8.GetBytes(command), true);
        }

        public bool sendcommand_gb2312(string command)
        {
            return commandWriter.Send(0, Encoding.GetEncoding("gb2312").GetBytes(command), true);
        }

        public bool sendcommand_big5(string command)
        {
            return commandWriter.Send(0, Encoding.GetEncoding("big5").GetBytes(command), true);
        }

        public bool sendcommand_mult(int portnumber, string command)
        {
            if (portnumber < 1 || portnumber > 5) return false;
            return commandWriter.SendAnsi(portnumber, command, true);
        }

        public int sendcommandNOCRLF(string command)
        {
            return commandWriter.SendAnsi(0, command, false) ? 1 : -1;
        }

        public bool sendbinary(byte[] command)
        {
            return sendcommand(command);
        }

        public bool sendcommand(byte[] command)
        {
            return commandWriter.Send(0, command, true);
        }

        public bool sendcommand_mult(int portnumber, byte[] command)
        {
            if (portnumber < 1 || portnumber > 5) return false;
            return commandWriter.Send(portnumber, command, true);
        }

        public bool sendcommandNOCRLF(byte[] command)
        {
            return commandWriter.Send(0, command, false);
        }

        public bool closeport()
        {
            driver.EndDocPrinter(driver.hPrinter);
            driver.EndPagePrinter(driver.hPrinter);
            driver.ClosePrinter(driver.hPrinter);
            return true;
        }

        public bool closeport_mult(int portnumber)
        {
            switch (portnumber)
            {
                case 1:
                    driver.EndDocPrinter(driver.hPrinter1);
                    driver.EndPagePrinter(driver.hPrinter1);
                    driver.ClosePrinter(driver.hPrinter1);
                    return true;
                case 2:
                    driver.EndDocPrinter(driver.hPrinter2);
                    driver.EndPagePrinter(driver.hPrinter2);
                    driver.ClosePrinter(driver.hPrinter2);
                    return true;
                case 3:
                    driver.EndDocPrinter(driver.hPrinter3);
                    driver.EndPagePrinter(driver.hPrinter3);
                    driver.ClosePrinter(driver.hPrinter3);
                    return true;
                case 4:
                    driver.EndDocPrinter(driver.hPrinter4);
                    driver.EndPagePrinter(driver.hPrinter4);
                    driver.ClosePrinter(driver.hPrinter4);
                    return true;
                case 5:
                    driver.EndDocPrinter(driver.hPrinter5);
                    driver.EndPagePrinter(driver.hPrinter5);
                    driver.ClosePrinter(driver.hPrinter5);
                    return true;
                default:
                    return false;
            }
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
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes1, bytes1.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, bytes2, bytes2.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, bytes3, bytes3.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, bytes4, bytes4.Length, out driver.dwWritten);
        }

        public void clearbuffer()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("CLS\r\n");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
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
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
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
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
        }

        public void printlabel(string a, string b)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("PRINT " + a + ", " + b + "\r\n");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
        }

        public void formfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("FORMFEED\r\n");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
        }

        public void nobackfeed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("SET TEAR OFF\r\n");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
        }

        public int downloadpcx(string filename, string imagename)
        {
            byte[] Bytes = File.ReadAllBytes(filename);
            long length = (long)Bytes.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return 0;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, Bytes, Bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, driver.CRLF_byte, driver.CRLF_byte.Length, out driver.dwWritten);
            return 1;
        }

        public int downloadbmp(string filename, string imagename)
        {
            byte[] Bytes = File.ReadAllBytes(filename);
            long length = (long)Bytes.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ",");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return 0;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, Bytes, Bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, driver.CRLF_byte, driver.CRLF_byte.Length, out driver.dwWritten);
            return 1;
        }

        public int downloadfile(string filename, string downloadname)
        {
            byte[] Bytes = File.ReadAllBytes(filename);
            long length = (long)Bytes.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + downloadname + "\"," + (object)length + ",");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return 0;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, Bytes, Bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, driver.CRLF_byte, driver.CRLF_byte.Length, out driver.dwWritten);
            return 1;
        }

        public int downloadfile(string filename, string location, string downloadname)
        {
            byte[] Bytes = File.ReadAllBytes(filename);
            long length = (long)Bytes.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD " + location + ",\"" + downloadname + "\"," + (object)length + ",");
            if (!driver.StartPagePrinter(driver.hPrinter))
                return 0;
            driver.WritePrinter(driver.hPrinter, bytes, bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, Bytes, Bytes.Length, out driver.dwWritten);
            driver.WritePrinter(driver.hPrinter, driver.CRLF_byte, driver.CRLF_byte.Length, out driver.dwWritten);
            return 1;
        }

        public string about()
        {
            return "This is .NET SDK V1.0A";
        }

        public void printerrestart()
        {
            this.sendcommand(new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 82
            });
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
            // The openport/closeport caller owns this job and page. Capture its handle once;
            // do not route through sendcommand, which starts a page and appends CRLF.
            // fontunderline remains ignored, as in both public legacy implementations.
            var printer = fontPrinterHandle();
            WindowsFontCommand.Send(request, fontGdi,
                (packet, offset, count) => WriteFontPacket(printer, packet, offset, count));
        }

        private int WriteFontPacket(IntPtr printer, byte[] packet, int offset, int count)
        {
            // The byte[] WritePrinter overload starts at index zero. After a partial write,
            // copy only the remaining range; no pinned or unmanaged allocation is retained.
            var bytes = packet;
            if (offset != 0)
            {
                bytes = new byte[count];
                Buffer.BlockCopy(packet, offset, bytes, 0, count);
            }
            int written;
            if (!fontPrinterWrite(printer, bytes, count, out written))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "WritePrinter failed while sending a font bitmap.");
            return written;
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
                return -1;
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
                return -1;
            }
            return 1;
        }

        public int search_folder_filename(string condition)
        {
            foreach (string file in Directory.GetFiles(Environment.CurrentDirectory, condition))
            {
                if (file == "" || file == null)
                    return -1;
                this.sendfile(file);
            }
            return 1;
        }

        public void send_bitmap(int x_axis, int y_axis, Bitmap bitmap_file)
        {
            this.sendpicture(x_axis, y_axis, bitmap_file);
        }

        [StructLayout(LayoutKind.Sequential)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
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

            public static implicit operator System.Drawing.Rectangle(driver.RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator driver.RECT(System.Drawing.Rectangle r) => new driver.RECT(r);

            public static bool operator ==(driver.RECT r1, driver.RECT r2) => r1.Equals(r2);

            public static bool operator !=(driver.RECT r1, driver.RECT r2) => !r1.Equals(r2);

            public bool Equals(driver.RECT r)
            {
                return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
            }

            public override bool Equals(object obj)
            {
                switch (obj)
                {
                    case driver.RECT r1:
                        return this.Equals(r1);
                    case System.Drawing.Rectangle r2:
                        return this.Equals(new driver.RECT(r2));
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
