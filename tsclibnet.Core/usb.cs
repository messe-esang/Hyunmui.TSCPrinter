// Decompiled with JetBrains decompiler
// Type: TSCSDK.usb
// Assembly: tsclibnet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A64385FF-5635-48AA-8C98-BF7EE2302ADD
// Assembly location: C:\workspaces\drivers\tsc-printer\TSC C# SDK 20210323\x64\tsclibnet.dll

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


namespace TSCSDK
{
    public class usb
    {
        private static Guid guidHID = Guid.Empty;
        private const uint INFINITE = 4294967295;
        private const uint WAIT_ABANDONED = 128;
        private const uint WAIT_IO_COMPLETION = 192;
        private const uint WAIT_OBJECT_0 = 0;
        private const uint WAIT_TIMEOUT = 258;
        private const uint WAIT_FAILED = 4294967295;
        private static Guid GUID_DEVINTERFACE_USB_HUB = new Guid("f18a0e88-c30c-11d0-8815-00a0c906bed8");
        private static Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");
        private static Guid GUID_DEVINTERFACE_USB_HOST_CONTROLLER = new Guid("3ABF6F2D-71C4-462a-8A92-1E6861E6AF27");
        private static Guid GUID_USB_WMI_STD_DATA = new Guid("4E623B20-CB14-11D1-B331-00A0C959BBD2");
        private static Guid GUID_USB_WMI_STD_NOTIFICATION = new Guid("4E623B20-CB14-11D1-B331-00A0C959BBD2");
        private static Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
        private static Guid guid = new Guid("{A5DCBF10-6530-11D2-901F-00C04FB951ED}");
        private static Guid deviceguid = new Guid("{36FC9E60-C465-11CF-8056-444553540000}");
        private static Guid GUID_DEVINTERFACE_USBPRINT = new Guid(685215661, (short)23058, (short)4561, (byte)174, (byte)91, (byte)0, (byte)0, (byte)248, (byte)3, (byte)168, (byte)194);
        private static Guid search_device = new Guid(2782707472U, (ushort)25904, (ushort)4562, (byte)144, (byte)31, (byte)0, (byte)192, (byte)79, (byte)185, (byte)81, (byte)237);
        private const int DIGCF_PRESENT = 2;
        private const int DIGCF_DEVICEINTERFACE = 16;
        private static string VID = "";
        private static string PID = "";
        private static int HidHandle = 0;
        private static int HidHandle1 = 0;
        private static int HidHandle2 = 0;
        private static int HidHandle3 = 0;
        private static int HidHandle4 = 0;
        private static int HidHandle5 = 0;
        private const uint GENERIC_READ = 2147483648;
        private const uint GENERIC_WRITE = 1073741824;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;
        private const int OPEN_EXISTING = 3;
        private static byte[] CRLF_byte = new byte[2]
        {
      (byte) 13,
      (byte) 10
        };
        private static byte[] readbuffer = new byte[1024];
        public static string[] usbpathlist = new string[1024];
        public static string[] usbdevicename = new string[1024];
        private static uint UsbWaitTime = 2000;
        private static string byte_to_string = "";
        private static string hexString = "";
        private static ArrayList HIDUSBAddress = new ArrayList();
        private static IntPtr PnPHandle;
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
        private static string[] diag_array = new string[1024];
        private static byte[] load_buffer = new byte[1024];
        private const uint FILE_ATTRIBUTE_NORMAL = 128;
        private const uint FILE_FLAG_SEQUENTIAL_SCAN = 134217728;
        private const uint FILE_FLAG_OVERLAPPED = 1073741824;

        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(ref Guid HidGuid);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
          ref Guid lpGuid,
          IntPtr Enumerator,
          IntPtr hwndParent,
          usb.ClassDevsFlags Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
          IntPtr hDevInfo,
          IntPtr devInfo,
          ref Guid interfaceClassGuid,
          uint memberIndex,
          ref usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
          IntPtr deviceInfoSet,
          ref usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
          IntPtr deviceInterfaceDetailData,
          uint deviceInterfaceDetailDataSize,
          ref uint requiredSize,
          IntPtr deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
          IntPtr deviceInfoSet,
          ref usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
          ref usb.PSP_DEVICE_INTERFACE_DETAIL_DATA myPSP_DEVICE_INTERFACE_DETAIL_DATA,
          uint deviceInterfaceDetailDataSize,
          ref uint requiredSize,
          usb.SP_DEVINFO_DATA deviceInfoData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int CreateFile(
          string lpFileName,
          uint dwDesiredAccess,
          uint dwShareMode,
          IntPtr lpSecurityAttributes,
          uint dwCreationDisposition,
          uint dwFlagsAndAttributes,
          IntPtr hTemplateFile);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int SetupDiEnumDeviceInfo(
          IntPtr hFile,
          int Index,
          ref usb.SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int SetupDiGetDeviceRegistryProperty(
          IntPtr DeviceInfoSet,
          ref usb.SP_DEVINFO_DATA DeviceInfoData,
          usb.RegPropertyType Property,
          IntPtr PropertyRegDataType,
          ref usb.DATA_BUFFER PropertyBuffer,
          int PropertyBufferSize,
          IntPtr RequiredSize);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
          IntPtr hFile,
          byte[] lpBuffer,
          uint nNumberOfBytesToRead,
          ref uint lpNumberOfBytesRead,
          IntPtr lpOverlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
          IntPtr hFile,
          byte[] lpBuffer,
          uint nNumberOfBytesToRead,
          ref uint lpNumberOfBytesRead,
          ref NativeOverlapped lpOverlapped);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(
          IntPtr hFile,
          byte[] lpBuffer,
          uint nNumberOfBytesToWrite,
          ref uint lpNumberOfBytesRead,
          ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateEvent(
          IntPtr lpEventAttributes,
          bool bManualReset,
          bool bInitialState,
          string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResult(
          IntPtr hFile,
          [In] ref NativeOverlapped lpOverlapped,
          out uint lpNumberOfBytesTransferred,
          bool bWait);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelIo(IntPtr hFile);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFontIndirect([MarshalAs(UnmanagedType.LPStruct), In] usb.LOGFONT lplf);

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
        private static extern int FillRect(IntPtr hDC, [In] ref usb.RECT lprc, IntPtr hbr);

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
          out usb.SIZE lpSize);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32W(
          IntPtr hdc,
          string lpWString,
          int cbString,
          out usb.SIZE lpSize);

        public bool openport()
        {
            int num1 = 0;
            usb.HidD_GetHidGuid(ref usb.guidHID);
            uint requiredSize = 0;
            usb.PnPHandle = usb.SetupDiGetClassDevs(ref usb.GUID_DEVINTERFACE_USBPRINT, IntPtr.Zero, IntPtr.Zero, usb.ClassDevsFlags.DIGCF_PRESENT | usb.ClassDevsFlags.DIGCF_DEVICEINTERFACE);
            usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new usb.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf((object)deviceInterfaceData);
            usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)num1, ref deviceInterfaceData);
            usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)num1, ref deviceInterfaceData);
            usb.SP_DEVINFO_DATA DeviceInfoData = new usb.SP_DEVINFO_DATA();
            DeviceInfoData.cbSize = (uint)Marshal.SizeOf((object)DeviceInfoData);
            usb.SetupDiEnumDeviceInfo(usb.PnPHandle, num1, ref DeviceInfoData);
            usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
            Marshal.GetLastWin32Error();
            IntPtr num2 = Marshal.AllocCoTaskMem((int)requiredSize);
            switch (IntPtr.Size)
            {
                case 4:
                    Marshal.WriteInt32(num2, 4 + Marshal.SystemDefaultCharSize);
                    break;
                case 8:
                    Marshal.WriteInt32(num2, 8);
                    break;
                default:
                    throw new NotSupportedException("Architecture not supported.");
            }
            usb.SP_DEVICE_INTERFACE_DETAIL_DATA structure = new usb.SP_DEVICE_INTERFACE_DETAIL_DATA();
            structure.cbSize = (uint)Marshal.SizeOf((object)structure);
            if (!usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, num2, requiredSize, ref requiredSize, IntPtr.Zero))
                return false;
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DEVICEDESC);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASS);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASSGUID);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DRIVER);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_MFG);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_FRIENDLYNAME);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_LOWERFILTERS);
            string stringAuto = Marshal.PtrToStringAuto(new IntPtr(num2.ToInt64() + 4L));
            for (int index = 0; index < 8; ++index)
                usb.VID += stringAuto[index + 8].ToString();
            for (int index = 0; index < 8; ++index)
                usb.PID += stringAuto[index + 17].ToString();
            usb.HidHandle = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
            return usb.HidHandle != -1;
        }

        public bool openport_overlapped()
        {
            int num1 = 0;
            usb.HidD_GetHidGuid(ref usb.guidHID);
            uint requiredSize = 0;
            usb.PnPHandle = usb.SetupDiGetClassDevs(ref usb.GUID_DEVINTERFACE_USBPRINT, IntPtr.Zero, IntPtr.Zero, usb.ClassDevsFlags.DIGCF_PRESENT | usb.ClassDevsFlags.DIGCF_DEVICEINTERFACE);
            usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new usb.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf((object)deviceInterfaceData);
            usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)num1, ref deviceInterfaceData);
            usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)num1, ref deviceInterfaceData);
            usb.SP_DEVINFO_DATA DeviceInfoData = new usb.SP_DEVINFO_DATA();
            DeviceInfoData.cbSize = (uint)Marshal.SizeOf((object)DeviceInfoData);
            usb.SetupDiEnumDeviceInfo(usb.PnPHandle, num1, ref DeviceInfoData);
            usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
            Marshal.GetLastWin32Error();
            IntPtr num2 = Marshal.AllocCoTaskMem((int)requiredSize);
            switch (IntPtr.Size)
            {
                case 4:
                    Marshal.WriteInt32(num2, 4 + Marshal.SystemDefaultCharSize);
                    break;
                case 8:
                    Marshal.WriteInt32(num2, 8);
                    break;
                default:
                    throw new NotSupportedException("Architecture not supported.");
            }
            usb.SP_DEVICE_INTERFACE_DETAIL_DATA structure = new usb.SP_DEVICE_INTERFACE_DETAIL_DATA();
            structure.cbSize = (uint)Marshal.SizeOf((object)structure);
            if (!usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, num2, requiredSize, ref requiredSize, IntPtr.Zero))
                return false;
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DEVICEDESC);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASS);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASSGUID);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DRIVER);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_MFG);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_FRIENDLYNAME);
            usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_LOWERFILTERS);
            string stringAuto = Marshal.PtrToStringAuto(new IntPtr(num2.ToInt32() + 4));
            for (int index = 0; index < 8; ++index)
                usb.VID += stringAuto[index + 8].ToString();
            for (int index = 0; index < 8; ++index)
                usb.PID += stringAuto[index + 17].ToString();
            usb.HidHandle = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 1073741952U, IntPtr.Zero);
            return usb.HidHandle != -1;
        }

        public int openport_mult(int portnumber)
        {
            int Index = 0;
            usb.HidD_GetHidGuid(ref usb.guidHID);
            int num1 = 1;
            uint requiredSize = 0;
            usb.PnPHandle = usb.SetupDiGetClassDevs(ref usb.GUID_DEVINTERFACE_USBPRINT, IntPtr.Zero, IntPtr.Zero, usb.ClassDevsFlags.DIGCF_PRESENT | usb.ClassDevsFlags.DIGCF_DEVICEINTERFACE);
            usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new usb.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf((object)deviceInterfaceData);
            for (int memberIndex = 0; usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)memberIndex, ref deviceInterfaceData); ++memberIndex)
            {
                usb.SP_DEVINFO_DATA DeviceInfoData = new usb.SP_DEVINFO_DATA();
                DeviceInfoData.cbSize = (uint)Marshal.SizeOf((object)DeviceInfoData);
                usb.SetupDiEnumDeviceInfo(usb.PnPHandle, Index, ref DeviceInfoData);
                usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
                Marshal.GetLastWin32Error();
                IntPtr num2 = Marshal.AllocCoTaskMem((int)requiredSize);
                switch (IntPtr.Size)
                {
                    case 4:
                        Marshal.WriteInt32(num2, 4 + Marshal.SystemDefaultCharSize);
                        break;
                    case 8:
                        Marshal.WriteInt32(num2, 8);
                        break;
                    default:
                        throw new NotSupportedException("Architecture not supported.");
                }
                usb.SP_DEVICE_INTERFACE_DETAIL_DATA structure = new usb.SP_DEVICE_INTERFACE_DETAIL_DATA();
                structure.cbSize = (uint)Marshal.SizeOf((object)structure);
                if (usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, num2, requiredSize, ref requiredSize, IntPtr.Zero))
                {
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DEVICEDESC);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASS);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASSGUID);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DRIVER);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_MFG);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_FRIENDLYNAME);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_LOWERFILTERS);
                    string stringAuto = Marshal.PtrToStringAuto(new IntPtr(num2.ToInt32() + 4));
                    char ch;
                    for (int index = 0; index < 8; ++index)
                    {
                        string vid = usb.VID;
                        ch = stringAuto[index + 8];
                        string str = ch.ToString();
                        usb.VID = vid + str;
                    }
                    for (int index = 0; index < 8; ++index)
                    {
                        string pid = usb.PID;
                        ch = stringAuto[index + 17];
                        string str = ch.ToString();
                        usb.PID = pid + str;
                    }
                    switch (num1)
                    {
                        case 1:
                            usb.HidHandle1 = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
                            break;
                        case 2:
                            usb.HidHandle2 = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
                            break;
                        case 3:
                            usb.HidHandle3 = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
                            break;
                        case 4:
                            usb.HidHandle4 = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
                            break;
                        case 5:
                            usb.HidHandle5 = usb.CreateFile(stringAuto, 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
                            break;
                    }
                    if (usb.HidHandle1 == -1 || usb.HidHandle2 == -1 || usb.HidHandle3 == -1 || usb.HidHandle4 == -1 || usb.HidHandle5 == -1)
                        return 0;
                    ++num1;
                }
            }
            return 1;
        }

        public string traceUSB_string()
        {
            usb.usbpathlist = new string[1024];
            string str1 = "";
            usb.usbdevicename = new string[1024];
            int Index = 0;
            usb.HidD_GetHidGuid(ref usb.guidHID);
            int index1 = 0;
            uint requiredSize = 0;
            usb.PnPHandle = usb.SetupDiGetClassDevs(ref usb.GUID_DEVINTERFACE_USBPRINT, IntPtr.Zero, IntPtr.Zero, usb.ClassDevsFlags.DIGCF_PRESENT | usb.ClassDevsFlags.DIGCF_DEVICEINTERFACE);
            usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new usb.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf((object)deviceInterfaceData);
            for (int memberIndex = 0; usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)memberIndex, ref deviceInterfaceData); ++memberIndex)
            {
                usb.VID = "";
                usb.PID = "";
                usb.SP_DEVINFO_DATA DeviceInfoData = new usb.SP_DEVINFO_DATA();
                DeviceInfoData.cbSize = (uint)Marshal.SizeOf((object)DeviceInfoData);
                usb.SetupDiEnumDeviceInfo(usb.PnPHandle, Index, ref DeviceInfoData);
                usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
                Marshal.GetLastWin32Error();
                IntPtr num = Marshal.AllocCoTaskMem((int)requiredSize);
                switch (IntPtr.Size)
                {
                    case 4:
                        Marshal.WriteInt32(num, 4 + Marshal.SystemDefaultCharSize);
                        break;
                    case 8:
                        Marshal.WriteInt32(num, 8);
                        break;
                    default:
                        throw new NotSupportedException("Architecture not supported.");
                }
                usb.SP_DEVICE_INTERFACE_DETAIL_DATA structure = new usb.SP_DEVICE_INTERFACE_DETAIL_DATA();
                structure.cbSize = (uint)Marshal.SizeOf((object)structure);
                if (usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, num, requiredSize, ref requiredSize, IntPtr.Zero))
                {
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DEVICEDESC);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASS);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASSGUID);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DRIVER);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_MFG);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_FRIENDLYNAME);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_LOWERFILTERS);
                    string stringAuto = Marshal.PtrToStringAuto(new IntPtr(num.ToInt32() + 4));
                    char ch;
                    for (int index2 = 0; index2 < 8; ++index2)
                    {
                        string vid = usb.VID;
                        ch = stringAuto[index2 + 8];
                        string str2 = ch.ToString();
                        usb.VID = vid + str2;
                    }
                    for (int index3 = 0; index3 < 8; ++index3)
                    {
                        string pid = usb.PID;
                        ch = stringAuto[index3 + 17];
                        string str3 = ch.ToString();
                        usb.PID = pid + str3;
                    }
                    if (usb.VID == "vid_1203")
                    {
                        usb.usbpathlist[index1] = stringAuto;
                        usb.usbdevicename[index1] = this.pid2name(usb.PID);
                        str1 = str1 + usb.usbdevicename[index1] + ";";
                        ++index1;
                    }
                }
            }
            return str1 == "" ? "no device" : str1;
        }

        public string[] traceUSB_array()
        {
            usb.usbpathlist = new string[1024];
            usb.usbdevicename = new string[1024];
            int Index = 0;
            usb.HidD_GetHidGuid(ref usb.guidHID);
            int index1 = 0;
            uint requiredSize = 0;
            usb.PnPHandle = usb.SetupDiGetClassDevs(ref usb.GUID_DEVINTERFACE_USBPRINT, IntPtr.Zero, IntPtr.Zero, usb.ClassDevsFlags.DIGCF_PRESENT | usb.ClassDevsFlags.DIGCF_DEVICEINTERFACE);
            usb.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new usb.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf((object)deviceInterfaceData);
            for (int memberIndex = 0; usb.SetupDiEnumDeviceInterfaces(usb.PnPHandle, IntPtr.Zero, ref usb.GUID_DEVINTERFACE_USBPRINT, (uint)memberIndex, ref deviceInterfaceData); ++memberIndex)
            {
                usb.VID = "";
                usb.PID = "";
                usb.SP_DEVINFO_DATA DeviceInfoData = new usb.SP_DEVINFO_DATA();
                DeviceInfoData.cbSize = (uint)Marshal.SizeOf((object)DeviceInfoData);
                usb.SetupDiEnumDeviceInfo(usb.PnPHandle, Index, ref DeviceInfoData);
                usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
                Marshal.GetLastWin32Error();
                IntPtr num = Marshal.AllocCoTaskMem((int)requiredSize);
                switch (IntPtr.Size)
                {
                    case 4:
                        Marshal.WriteInt32(num, 4 + Marshal.SystemDefaultCharSize);
                        break;
                    case 8:
                        Marshal.WriteInt32(num, 8);
                        break;
                    default:
                        throw new NotSupportedException("Architecture not supported.");
                }
                usb.SP_DEVICE_INTERFACE_DETAIL_DATA structure = new usb.SP_DEVICE_INTERFACE_DETAIL_DATA();
                structure.cbSize = (uint)Marshal.SizeOf((object)structure);
                if (usb.SetupDiGetDeviceInterfaceDetail(usb.PnPHandle, ref deviceInterfaceData, num, requiredSize, ref requiredSize, IntPtr.Zero))
                {
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DEVICEDESC);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASS);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_CLASSGUID);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_DRIVER);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_MFG);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_FRIENDLYNAME);
                    usb.GetRegistryProperty(usb.PnPHandle, ref DeviceInfoData, usb.RegPropertyType.SPDRP_LOWERFILTERS);
                    string stringAuto = Marshal.PtrToStringAuto(new IntPtr(num.ToInt32() + 4));
                    char ch;
                    for (int index2 = 0; index2 < 8; ++index2)
                    {
                        string vid = usb.VID;
                        ch = stringAuto[index2 + 8];
                        string str = ch.ToString();
                        usb.VID = vid + str;
                    }
                    for (int index3 = 0; index3 < 8; ++index3)
                    {
                        string pid = usb.PID;
                        ch = stringAuto[index3 + 17];
                        string str = ch.ToString();
                        usb.PID = pid + str;
                    }
                    if (usb.VID == "vid_1203")
                    {
                        usb.usbpathlist[index1] = stringAuto;
                        usb.usbdevicename[index1] = this.pid2name(usb.PID);
                        ++index1;
                    }
                }
            }
            return usb.usbdevicename;
        }

        private string pid2name(string Printer_PID)
        {
            switch (Printer_PID)
            {
                case "pid_0243":
                    return "TTP-2410MT";
                case "pid_0244":
                    return "TTP-346MT";
                case "pid_0245":
                    return "TTP-644MT";
                case "pid_0240":
                    return "TTP-2410MU";
                case "pid_0241":
                    return "TTP-346MU";
                case "pid_0242":
                    return "TTP-644MU";
                case "pid_0246":
                    return "TTP-2610MT";
                case "pid_0247":
                    return "TTP-368MT";
                case "pid_0248":
                    return "TTP-286MT";
                case "pid_0249":
                    return "TTP-384MT";
                case "pid_0172":
                    return "TTP-244 Pro";
                case "pid_0136":
                    return "TA210";
                case "pid_0137":
                    return "TA310";
                case "pid_0262":
                    return "TC200";
                case "pid_0263":
                    return "TC300";
                case "pid_0260":
                    return "TC210";
                case "pid_0261":
                    return "TC310";
                case "pid_0250":
                    return "DA200";
                case "pid_0251":
                    return "DA300";
                case "pid_0134":
                    return "TTP-244M Pro";
                case "pid_0135":
                    return "TTP-342M Pro";
                case "pid_0130":
                    return "TTP-243 Pro";
                case "pid_0131":
                    return "TTP-342 Pro";
                case "pid_0128":
                    return "TDP-247";
                case "pid_0129":
                    return "TDP-345";
                case "pid_0126":
                    return "TTP-247";
                case "pid_0127":
                    return "TTP-345";
                case "pid_0230":
                    return "TX200";
                case "pid_0231":
                    return "TX300";
                case "pid_0232":
                    return "TX600";
                case "pid_0223":
                    return "MX240P";
                case "pid_0224":
                    return "MX340P";
                case "pid_0225":
                    return "MX640P";
                case "pid_0280":
                    return "MH240";
                case "pid_0281":
                    return "MH340";
                case "pid_0282":
                    return "MH640";
                case "pid_0170":
                    return "ME240";
                case "pid_0171":
                    return "ME340";
                case "pid_0030":
                    return "Alpha-2R";
                case "pid_0011":
                    return "Alpha-3R";
                case "pid_0020":
                    return "Alpha-4L";
                default:
                    return "unknown";
            }
        }

        public string openportweb(int usb_array_number)
        {
            if (usb.HidHandle == -1)
                return "0";
            usb.HidHandle = usb.CreateFile(usb.usbpathlist[usb_array_number], 3221225472U, 3U, IntPtr.Zero, 3U, 0U, IntPtr.Zero);
            return "1";
        }

        public static string GetRegistryProperty(
          IntPtr PnPHandle,
          ref usb.SP_DEVINFO_DATA DeviceInfoData,
          usb.RegPropertyType Property)
        {
            usb.DATA_BUFFER PropertyBuffer = new usb.DATA_BUFFER();
            usb.SetupDiGetDeviceRegistryProperty(PnPHandle, ref DeviceInfoData, Property, IntPtr.Zero, ref PropertyBuffer, 1024, IntPtr.Zero);
            return PropertyBuffer.Buffer;
        }

        public void closeport() => usb.CloseHandle((IntPtr)usb.HidHandle);

        public void closeport_mult(int portnumber)
        {
            switch (portnumber)
            {
                case 1:
                    usb.CloseHandle((IntPtr)usb.HidHandle1);
                    break;
                case 2:
                    usb.CloseHandle((IntPtr)usb.HidHandle2);
                    break;
                case 3:
                    usb.CloseHandle((IntPtr)usb.HidHandle3);
                    break;
                case 4:
                    usb.CloseHandle((IntPtr)usb.HidHandle4);
                    break;
                case 5:
                    usb.CloseHandle((IntPtr)usb.HidHandle5);
                    break;
            }
        }

        public void closeport(int delay)
        {
            Thread.Sleep(delay);
            usb.CloseHandle((IntPtr)usb.HidHandle);
        }

        public void sendcommand(string command)
        {
            usb.WriteToStream(Encoding.Default.GetBytes(command));
            usb.WriteToStream(usb.CRLF_byte);
        }

        public void sendcommand(string[] command)
        {
            for (int index = 0; index < command.Length; ++index)
            {
                if (command[index] != "")
                {
                    this.sendcommand(Encoding.Default.GetBytes(command[index]));
                    this.sendcommand(usb.CRLF_byte);
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

        public void sendcommand_utf8(string command)
        {
            usb.WriteToStream(Encoding.UTF8.GetBytes(command));
            usb.WriteToStream(usb.CRLF_byte);
        }

        public void sendcommand_gb2312(string command)
        {
            usb.WriteToStream(Encoding.GetEncoding("gb2312").GetBytes(command));
            usb.WriteToStream(usb.CRLF_byte);
        }

        public void sendcommand_big5(string command)
        {
            usb.WriteToStream(Encoding.GetEncoding("big5").GetBytes(command));
            usb.WriteToStream(usb.CRLF_byte);
        }

        public string sendcommand_getstring(string command)
        {
            byte[] bytes1 = Encoding.Default.GetBytes(command);
            byte[] bytes2 = Encoding.Default.GetBytes("OUT \"ENDLINE\"\r\n");
            this.sendcommand(bytes1);
            this.sendcommand(bytes2);
            usb.ReadToStream(100, "ENDLINE\r\n");
            return usb.byte_to_string;
        }

        public string sendcommand_getstring(byte[] command)
        {
            byte[] command1 = command;
            byte[] bytes = Encoding.Default.GetBytes("OUT \"ENDLINE\"\r\n");
            this.sendcommand(command1);
            this.sendcommand(bytes);
            usb.ReadToStream(100, "ENDLINE\r\n");
            return usb.byte_to_string;
        }

        public void sendcommand_mult(int portnumber, string command)
        {
            switch (portnumber)
            {
                case 1:
                    byte[] bytes1 = Encoding.Default.GetBytes(command);
                    usb.WriteToStream(usb.HidHandle1, bytes1);
                    usb.WriteToStream(usb.HidHandle1, usb.CRLF_byte);
                    break;
                case 2:
                    byte[] bytes2 = Encoding.Default.GetBytes(command);
                    usb.WriteToStream(usb.HidHandle2, bytes2);
                    usb.WriteToStream(usb.HidHandle2, usb.CRLF_byte);
                    break;
                case 3:
                    byte[] bytes3 = Encoding.Default.GetBytes(command);
                    usb.WriteToStream(usb.HidHandle3, bytes3);
                    usb.WriteToStream(usb.HidHandle3, usb.CRLF_byte);
                    break;
                case 4:
                    byte[] bytes4 = Encoding.Default.GetBytes(command);
                    usb.WriteToStream(usb.HidHandle4, bytes4);
                    usb.WriteToStream(usb.HidHandle4, usb.CRLF_byte);
                    break;
                case 5:
                    byte[] bytes5 = Encoding.Default.GetBytes(command);
                    usb.WriteToStream(usb.HidHandle5, bytes5);
                    usb.WriteToStream(usb.HidHandle5, usb.CRLF_byte);
                    break;
            }
        }

        public void sendcommandNOCRLF(string command)
        {
            usb.WriteToStream(Encoding.Default.GetBytes(command));
        }

        public void sendcommand(byte[] command)
        {
            usb.WriteToStream(command);
            usb.WriteToStream(usb.CRLF_byte);
        }

        public void sendcommand_mult(int portnumber, byte[] command)
        {
            switch (portnumber)
            {
                case 1:
                    usb.WriteToStream(usb.HidHandle1, command);
                    usb.WriteToStream(usb.HidHandle1, usb.CRLF_byte);
                    break;
                case 2:
                    usb.WriteToStream(usb.HidHandle2, command);
                    usb.WriteToStream(usb.HidHandle2, usb.CRLF_byte);
                    break;
                case 3:
                    usb.WriteToStream(usb.HidHandle3, command);
                    usb.WriteToStream(usb.HidHandle3, usb.CRLF_byte);
                    break;
                case 4:
                    usb.WriteToStream(usb.HidHandle4, command);
                    usb.WriteToStream(usb.HidHandle4, usb.CRLF_byte);
                    break;
                case 5:
                    usb.WriteToStream(usb.HidHandle5, command);
                    usb.WriteToStream(usb.HidHandle5, usb.CRLF_byte);
                    break;
            }
        }

        public void sendcommandNOCRLF(byte[] command) => usb.WriteToStream(command);

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
            usb.WriteToStream(bytes1);
            usb.WriteToStream(bytes2);
            usb.WriteToStream(bytes3);
            usb.WriteToStream(bytes4);
        }

        public void clearbuffer() => usb.WriteToStream(Encoding.ASCII.GetBytes("CLS\r\n"));

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
            usb.WriteToStream(Encoding.ASCII.GetBytes("BARCODE " + x + "," + y + ",\"" + type + "\"," + height + "," + readable + "," + rotation + "," + narrow + "," + wide + ",\"" + code + "\"\r\n"));
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
            usb.WriteToStream(Encoding.ASCII.GetBytes("TEXT " + x + "," + y + ",\"" + fonttype + "\"," + rotation + "," + xmul + "," + ymul + ",\"" + text + "\"\r\n"));
        }

        public void printlabel(string a, string b)
        {
            usb.WriteToStream(Encoding.ASCII.GetBytes("PRINT " + a + ", " + b + "\r\n"));
        }

        public void formfeed() => usb.WriteToStream(Encoding.ASCII.GetBytes("FORMFEED\r\n"));

        public void nobackfeed() => usb.WriteToStream(Encoding.ASCII.GetBytes("SET TEAR OFF\r\n"));

        public int downloadfile(string filename, string downloadname)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            byte[] bytes = Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + downloadname + "\"," + (object)length + ",");
            try
            {
                usb.WriteToStream(bytes);
                usb.WriteToStream(buffer);
                usb.WriteToStream(usb.CRLF_byte);
            }
            catch (Exception ex)
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
                usb.WriteToStream(bytes);
                usb.WriteToStream(buffer);
                usb.WriteToStream(usb.CRLF_byte);
            }
            catch (Exception ex)
            {
                return 0;
            }
            return 1;
        }

        public void downloadpcx(string filename, string imagename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            usb.WriteToStream(Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ","));
            usb.WriteToStream(buffer);
            usb.WriteToStream(usb.CRLF_byte);
        }

        public void downloadbmp(string filename, string imagename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            long length = (long)buffer.Length;
            usb.WriteToStream(Encoding.ASCII.GetBytes("DOWNLOAD F,\"" + imagename + "\"," + (object)length + ","));
            usb.WriteToStream(buffer);
            usb.WriteToStream(usb.CRLF_byte);
        }

        public string about()
        {
            return "This is .NET SDL V1.0";
        }

        public string about(string information)
        {
            return information;
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

        public byte printerstatus()
        {
            byte[] numArray = new byte[256];
            this.sendcommand(new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            });
            usb.ReadToStream();
            return usb.readbuffer[0];
        }

        public byte printerstatus_mult(int portnumber)
        {
            byte[] numArray = new byte[256];
            byte[] command = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            switch (portnumber)
            {
                case 1:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle1);
                    return usb.readbuffer[0];
                case 2:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle2);
                    return usb.readbuffer[0];
                case 3:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle3);
                    return usb.readbuffer[0];
                case 4:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle4);
                    return usb.readbuffer[0];
                case 5:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle5);
                    return usb.readbuffer[0];
                default:
                    return 99;
            }
        }

        public string printerstatus_string()
        {
            byte[] numArray = new byte[256];
            this.sendcommand(new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            });
            usb.ReadToStream();
            if (usb.readbuffer[0] == (byte)0)
                return "00";
            if (usb.readbuffer[0] == (byte)1)
                return "01";
            if (usb.readbuffer[0] == (byte)2)
                return "02";
            if (usb.readbuffer[0] == (byte)3)
                return "03";
            if (usb.readbuffer[0] == (byte)4)
                return "04";
            if (usb.readbuffer[0] == (byte)5)
                return "05";
            if (usb.readbuffer[0] == (byte)8)
                return "08";
            if (usb.readbuffer[0] == (byte)9)
                return "09";
            if (usb.readbuffer[0] == (byte)10)
                return "0A";
            if (usb.readbuffer[0] == (byte)11)
                return "0B";
            if (usb.readbuffer[0] == (byte)12)
                return "0C";
            if (usb.readbuffer[0] == (byte)13)
                return "0D";
            if (usb.readbuffer[0] == (byte)16)
                return "10";
            if (usb.readbuffer[0] == (byte)32)
                return "20";
            return usb.readbuffer[0] == (byte)128 ? "80" : "other error";
        }

        public string printerstatus_string_mult(int portnumber)
        {
            byte[] numArray = new byte[256];
            byte[] command = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 63
            };
            switch (portnumber)
            {
                case 1:
                    this.sendcommand_mult(portnumber, command);
                    if (usb.ReadToStream(usb.HidHandle1))
                        break;
                    break;
                case 2:
                    this.sendcommand_mult(portnumber, command);
                    if (usb.ReadToStream(usb.HidHandle2))
                        break;
                    break;
                case 3:
                    this.sendcommand_mult(portnumber, command);
                    if (usb.ReadToStream(usb.HidHandle3))
                        break;
                    break;
                case 4:
                    this.sendcommand_mult(portnumber, command);
                    if (usb.ReadToStream(usb.HidHandle4))
                        break;
                    break;
                case 5:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle5);
                    break;
            }
            if (usb.readbuffer[0] == (byte)0)
                return "00";
            if (usb.readbuffer[0] == (byte)1)
                return "01";
            if (usb.readbuffer[0] == (byte)2)
                return "02";
            if (usb.readbuffer[0] == (byte)3)
                return "03";
            if (usb.readbuffer[0] == (byte)4)
                return "04";
            if (usb.readbuffer[0] == (byte)5)
                return "05";
            if (usb.readbuffer[0] == (byte)8)
                return "08";
            if (usb.readbuffer[0] == (byte)9)
                return "09";
            if (usb.readbuffer[0] == (byte)10)
                return "0A";
            if (usb.readbuffer[0] == (byte)11)
                return "0B";
            if (usb.readbuffer[0] == (byte)12)
                return "0C";
            if (usb.readbuffer[0] == (byte)13)
                return "0D";
            if (usb.readbuffer[0] == (byte)16)
                return "10";
            if (usb.readbuffer[0] == (byte)32)
                return "20";
            return usb.readbuffer[0] == (byte)128 ? "80" : "other error";
        }

        public string printersetting(string app, string sec, string key)
        {
            byte[] numArray = new byte[256];
            this.sendcommand("OUT GETSETTING$(\"" + app + "\",\"" + sec + "\",\"" + key + "\")");
            usb.ReadToStream(usb.HidHandle);
            var rawText = Encoding.ASCII.GetString(usb.readbuffer);
            if (rawText.Contains("\r\n"))
                return rawText.Substring(0, rawText.IndexOf("\r\n"));
            else
                return string.Empty;
        }

        public string printersetting_mult(int portnumber, string app, string sec, string key)
        {
            byte[] numArray = new byte[256];
            string command = "OUT GETSETTING$(\"" + app + "\",\"" + sec + "\",\"" + key + "\")";
            switch (portnumber)
            {
                case 1:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle1);
                    return Encoding.ASCII.GetString(usb.readbuffer);
                case 2:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle2);
                    return Encoding.ASCII.GetString(usb.readbuffer);
                case 3:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle3);
                    return Encoding.ASCII.GetString(usb.readbuffer);
                case 4:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle4);
                    return Encoding.ASCII.GetString(usb.readbuffer);
                case 5:
                    this.sendcommand_mult(portnumber, command);
                    usb.ReadToStream(usb.HidHandle5);
                    return Encoding.ASCII.GetString(usb.readbuffer);
                default:
                    return "error";
            }
        }

        public string printerfullstatus()
        {
            usb.byte_to_string = "";
            byte[] command = new byte[3]
            {
        (byte) 27,
        (byte) 33,
        (byte) 83
            };
            string oldValue = "\r\n";
            return this.sendcommand_getstring(command).Replace(oldValue, "");
        }

        public string printercodepage()
        {
            byte[] numArray = new byte[256];
            string str = "~!I\r\n";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public string printermileage()
        {
            byte[] numArray = new byte[256];
            string str = "~!@";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public string printername()
        {
            byte[] numArray = new byte[256];
            string str = "~!T";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public string printerfile()
        {
            byte[] numArray = new byte[256];
            string str = "~!F";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public string printermemory()
        {
            byte[] numArray = new byte[256];
            string str = "~!T";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public string printerserial()
        {
            byte[] numArray = new byte[256];
            string str = "OUT _SERIAL$\r\n";
            Encoding.ASCII.GetBytes(str);
            string oldValue = "\r\n";
            return this.sendcommand_getstring(str).Replace(oldValue, "");
        }

        public static int WriteToStream(byte[] buffer)
        {
            uint lpNumberOfBytesTransferred = 0;
            uint lpNumberOfBytesRead = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, true, false, (string)null);
            if (!usb.WriteFile((IntPtr)usb.HidHandle, buffer, (uint)buffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, 2000U))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                    case uint.MaxValue:
                        break;
                    case 258:
                        throw new TscException("WAIT_TIMEOUT");
                    default:
                        usb.CancelIo((IntPtr)usb.HidHandle);
                        break;
                }
            }
            else if ((long)buffer.Length != (long)lpNumberOfBytesRead)
                ;
            usb.CloseHandle(lpOverlapped.EventHandle);
            return 1;
        }

        public static int WriteToStream(int portnumber, byte[] buffer)
        {
            uint lpNumberOfBytesTransferred = 0;
            uint lpNumberOfBytesRead = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, true, false, (string)null);
            if (!usb.WriteFile((IntPtr)portnumber, buffer, (uint)buffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, 2000U))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                    case uint.MaxValue:
                        break;
                    case 258:
                        throw new TscException("WAIT_TIMEOUT");
                    default:
                        usb.CancelIo((IntPtr)usb.HidHandle);
                        break;
                }
            }
            else if ((long)buffer.Length != (long)lpNumberOfBytesRead)
                ;
            usb.CloseHandle(lpOverlapped.EventHandle);
            return 1;
        }

        private static int WriteToStream(byte[] buffer, int length)
        {
            uint lpNumberOfBytesTransferred = 0;
            uint lpNumberOfBytesRead = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, true, false, (string)null);
            if (!usb.WriteFile((IntPtr)usb.HidHandle, buffer, (uint)length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, 2000U))
                {
                    case 0:
                        usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false);
                        throw new TscException("WAIT_OBJECT_0");
                    case 258:
                        throw new TscException("WAIT_TIMEOUT");
                }
            }
            else if ((long)buffer.Length != (long)lpNumberOfBytesRead)
                ;
            usb.CloseHandle(lpOverlapped.EventHandle);
            return 1;
        }

        private static bool ReadToStream()
        {
            usb.readbuffer = new byte[1024];
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, false, false, (string)null);
            uint lpNumberOfBytesRead = 0;
            uint lpNumberOfBytesTransferred = 0;
            Thread.Sleep(100);
            if (!usb.ReadFile((IntPtr)usb.HidHandle, usb.readbuffer, (uint)usb.readbuffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, usb.UsbWaitTime))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                        usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, true);
                        break;
                    case 258:
                    case uint.MaxValue:
                        break;
                    default:
                        usb.CancelIo((IntPtr)usb.HidHandle);
                        lpNumberOfBytesTransferred = 0U;
                        break;
                }
            }
            usb.CloseHandle(lpOverlapped.EventHandle);
            usb.byte_to_string = Encoding.ASCII.GetString(usb.readbuffer, 0, usb.readbuffer.Length);
            return lpNumberOfBytesTransferred == 0U;
        }

        private static bool ReadToStream(int delay, byte judgement)
        {
            usb.readbuffer = new byte[1024];
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, false, false, (string)null);
            uint lpNumberOfBytesRead = 0;
            uint lpNumberOfBytesTransferred = 0;
            bool flag = false;
            usb.byte_to_string = "";
            do
            {
                Thread.Sleep(delay);
                if (!usb.ReadFile((IntPtr)usb.HidHandle, usb.readbuffer, (uint)usb.readbuffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
                {
                    switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, usb.UsbWaitTime))
                    {
                        case 0:
                            if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                                break;
                            break;
                        case 1:
                            usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, true);
                            break;
                        case 258:
                        case uint.MaxValue:
                            break;
                        default:
                            usb.CancelIo((IntPtr)usb.HidHandle);
                            lpNumberOfBytesTransferred = 0U;
                            break;
                    }
                }
                for (int index = 0; (long)index <= (long)(lpNumberOfBytesRead - 1U); ++index)
                {
                    if ((int)usb.readbuffer[index] == (int)judgement)
                    {
                        flag = true;
                        break;
                    }
                    usb.byte_to_string += Convert.ToChar(usb.readbuffer[index]).ToString();
                }
                if (lpNumberOfBytesRead == 0U)
                {
                    usb.CloseHandle(lpOverlapped.EventHandle);
                    return true;
                }
            }
            while (lpNumberOfBytesRead == 0U || !flag);
            usb.CloseHandle(lpOverlapped.EventHandle);
            return false;
        }

        private static bool ReadToStream(int delay, string judgement)
        {
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, false, false, (string)null);
            uint lpNumberOfBytesRead = 0;
            uint lpNumberOfBytesTransferred = 0;
            usb.byte_to_string = "";
            double totalSeconds = DateTime.Now.Subtract(DateTime.Now).TotalSeconds;
label_1:
            usb.readbuffer = new byte[1024];
            Thread.Sleep(delay);
            if (!usb.ReadFile((IntPtr)usb.HidHandle, usb.readbuffer, (uint)usb.readbuffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, usb.UsbWaitTime))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                        usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, true);
                        break;
                    case 258:
                    case uint.MaxValue:
                        break;
                    default:
                        usb.CancelIo((IntPtr)usb.HidHandle);
                        lpNumberOfBytesTransferred = 0U;
                        break;
                }
            }
            if (totalSeconds > 5.0)
            {
                usb.byte_to_string = "time out";
                return true;
            }
            for (int index = 0; (long)index <= (long)lpNumberOfBytesRead; ++index)
            {
                if (usb.byte_to_string.Contains(judgement))
                {
                    usb.byte_to_string = usb.byte_to_string.Replace("\0", "");
                    usb.byte_to_string = usb.byte_to_string.Replace(judgement, "");
                    return true;
                }
                usb.byte_to_string += Convert.ToChar(usb.readbuffer[index]).ToString();
            }
            goto label_1;
        }

        private static bool ReadToStream(int portnumber)
        {
            usb.readbuffer = new byte[1024];
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, false, false, (string)null);
            uint lpNumberOfBytesRead = 0;
            uint lpNumberOfBytesTransferred = 0;
            Thread.Sleep(1000);
            if (!usb.ReadFile((IntPtr)portnumber, usb.readbuffer, (uint)usb.readbuffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, usb.UsbWaitTime))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)portnumber, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                        usb.GetOverlappedResult((IntPtr)portnumber, ref lpOverlapped, out lpNumberOfBytesTransferred, true);
                        break;
                    case 258:
                    case uint.MaxValue:
                        break;
                    default:
                        usb.CancelIo((IntPtr)portnumber);
                        lpNumberOfBytesTransferred = 0U;
                        break;
                }
            }
            usb.CloseHandle(lpOverlapped.EventHandle);
            usb.byte_to_string = Encoding.ASCII.GetString(usb.readbuffer, 0, usb.readbuffer.Length);
            return lpNumberOfBytesTransferred == 0U;
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
            usb.LOGFONT lplf = new usb.LOGFONT();
            usb.SIZE lpSize = new usb.SIZE();
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
            IntPtr dc = usb.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = usb.CreateCompatibleDC(dc);
            IntPtr bitmap = usb.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            usb.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = usb.CreateFontIndirect(lplf);
            IntPtr hgdiobj = usb.SelectObject(compatibleDc, fontIndirect);
            usb.GetTextExtentPoint32(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)usb.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)usb.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            usb.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            usb.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new usb.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            usb.FillRect(compatibleDc, ref rect, IntPtr.Zero);
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
            usb.TextOut_X_start = num3;
            usb.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : usb.iBitmapHeight;
            usb.TextOut(compatibleDc, usb.TextOut_X_start, usb.TextOut_Y_start, content, content.Length);
            usb.GetBitmapBits(bitmap, 5760000, usb.buf);
            if (!usb.DeleteObject(usb.SelectObject(compatibleDc, hgdiobj)))
            {
                //// int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!usb.DeleteDC(compatibleDc))
            {
                //// int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!usb.DeleteObject(bitmap))
            {
                //// int num6 = (int)MessageBox.Show("hBitmap=0", "title");
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
            usb.iBitmapX = num7;
            usb.iBitmapY = rotation == 0 || rotation == 270 ? y : y - usb.iBitmapHeight;
            if (usb.iBitmapY < 0)
            {
                usb.iTop -= usb.iBitmapY;
                usb.iBitmapY = 0;
            }
            if (usb.iBitmapX < 0)
            {
                usb.imgShiftX -= (usb.iBitmapX - 7) / 8;
                usb.iBitmapX = 0;
            }
            usb.WriteToStream(Encoding.UTF8.GetBytes("BITMAP " + (object)usb.iBitmapX + "," + (object)usb.iBitmapY + "," + (object)(usb.iBitmapWidth - usb.imgShiftX) + "," + (object)(usb.iBitmapHeight - usb.iTop) + ",1,"));
            GC.Collect();
            Encoding.Unicode.GetChars(usb.buf);
            for (int iTop = usb.iTop; iTop < usb.iBitmapHeight; ++iTop)
            {
                int imgShiftX = usb.imgShiftX;
                while (imgShiftX < usb.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(usb.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    usb.WriteToStream(numArray2, usb.iBitmapWidth);
                    imgShiftX += usb.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            usb.WriteToStream(usb.CRLF_byte);
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
            usb.LOGFONT lplf = new usb.LOGFONT();
            usb.SIZE lpSize = new usb.SIZE();
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
            IntPtr dc = usb.GetDC(IntPtr.Zero);
            IntPtr compatibleDc = usb.CreateCompatibleDC(dc);
            IntPtr bitmap = usb.CreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
            usb.SelectObject(compatibleDc, bitmap);
            IntPtr fontIndirect = usb.CreateFontIndirect(lplf);
            IntPtr hgdiobj = usb.SelectObject(compatibleDc, fontIndirect);
            usb.GetTextExtentPoint32W(compatibleDc, content, content.Length, out lpSize);
            int num1 = (int)usb.SetTextColor(compatibleDc, ColorTranslator.ToWin32(Color.Black));
            int num2 = (int)usb.SetBkColor(compatibleDc, ColorTranslator.ToWin32(Color.White));
            usb.iBitmapWidth = rotation == 0 || rotation == 180 ? (lpSize.cx + 7) / 8 : (lpSize.cy + 7) / 8;
            usb.iBitmapHeight = rotation == 90 || rotation == 270 ? lpSize.cx : lpSize.cy;
            var rect = new usb.RECT()
            {
                Left = 0,
                Top = 0,
                Right = rotation == 0 || rotation == 180 ? lpSize.cx + 16 : lpSize.cy + 16,
                Bottom = rotation == 90 || rotation == 270 ? lpSize.cx + 16 : lpSize.cy + 16
            };
            usb.FillRect(compatibleDc, ref rect, IntPtr.Zero);
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
            usb.TextOut_X_start = num3;
            usb.TextOut_Y_start = rotation == 0 || rotation == 270 ? 0 : usb.iBitmapHeight;
            usb.TextOutW(compatibleDc, usb.TextOut_X_start, usb.TextOut_Y_start, content, content.Length);
            usb.GetBitmapBits(bitmap, 5760000, usb.buf);
            if (!usb.DeleteObject(usb.SelectObject(compatibleDc, hgdiobj)))
            {
                //// int num4 = (int)MessageBox.Show("Select hFont=0", "title");
            }
            if (!usb.DeleteDC(compatibleDc))
            {
                //// int num5 = (int)MessageBox.Show("hdcMem=0", "title");
            }
            if (!usb.DeleteObject(bitmap))
            {
                //// int num6 = (int)MessageBox.Show("hBitmap=0", "title");
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
            usb.iBitmapX = num7;
            usb.iBitmapY = rotation == 0 || rotation == 270 ? y : y - usb.iBitmapHeight;
            if (usb.iBitmapY < 0)
            {
                usb.iTop -= usb.iBitmapY;
                usb.iBitmapY = 0;
            }
            if (usb.iBitmapX < 0)
            {
                usb.imgShiftX -= (usb.iBitmapX - 7) / 8;
                usb.iBitmapX = 0;
            }
            usb.WriteToStream(Encoding.UTF8.GetBytes("BITMAP " + (object)usb.iBitmapX + "," + (object)usb.iBitmapY + "," + (object)(usb.iBitmapWidth - usb.imgShiftX) + "," + (object)(usb.iBitmapHeight - usb.iTop) + ",1,"));
            GC.Collect();
            Encoding.Unicode.GetChars(usb.buf);
            for (int iTop = usb.iTop; iTop < usb.iBitmapHeight; ++iTop)
            {
                int imgShiftX = usb.imgShiftX;
                while (imgShiftX < usb.iBitmapWidth)
                {
                    byte[] numArray1 = new byte[300];
                    Marshal.SizeOf((object)numArray1[0]);
                    int length = numArray1.Length;
                    IntPtr num8 = Marshal.AllocHGlobal(5760000);
                    Marshal.Copy(usb.buf, iTop * 300, num8, 5760000 - iTop * 300);
                    byte[] numArray2 = new byte[300];
                    Marshal.Copy(num8, numArray2, 0, 300);
                    usb.WriteToStream(numArray2, usb.iBitmapWidth);
                    imgShiftX += usb.iBitmapWidth;
                    Marshal.FreeHGlobal(num8);
                    GC.Collect();
                }
            }
            usb.WriteToStream(usb.CRLF_byte);
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
            this.sendcommand(usb.CRLF_byte);
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
            this.sendcommand(usb.CRLF_byte);
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
            this.sendcommand(usb.CRLF_byte);
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
            if (usb.HidHandle == -1)
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
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN SSID \"" + SSID + "\"\r\n");
            return "1";
        }

        public string WiFi_WPA(string WPA)
        {
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN WPA \"" + WPA + "\"\r\n");
            return "1";
        }

        public string WiFi_WEP(int number, string WEP)
        {
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN WEP " + number.ToString() + ",\"" + WEP + "\"\r\n");
            return "1";
        }

        public string WiFi_DHCP()
        {
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN DHCP\r\n");
            return "1";
        }

        public string WiFi_Port(int port)
        {
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN PORT " + port.ToString() + "\r\n");
            return "1";
        }

        public string WiFi_StaticIP(string ip, string mask, string gateway)
        {
            if (usb.HidHandle == -1)
                return "-1";
            this.sendcommand("WLAN IP \"" + ip + "\",\"" + mask + "\",\"" + gateway + "\"\r\n");
            return "1";
        }

        public void send_bitmap(int x_axis, int y_axis, Bitmap bitmap_file)
        {
            this.sendpicture(x_axis, y_axis, bitmap_file);
        }

        private static bool ReadToStream_overlapped(int delay, string judge)
        {
            usb.readbuffer = new byte[1024];
            usb.diag_array = new string[1024];
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            lpOverlapped.EventHandle = usb.CreateEvent(IntPtr.Zero, false, false, (string)null);
            uint lpNumberOfBytesRead = 0;
            uint lpNumberOfBytesTransferred = 0;
            usb.byte_to_string = "";
label_1:
            Thread.Sleep(delay);
            if (!usb.ReadFile((IntPtr)usb.HidHandle, usb.readbuffer, (uint)usb.readbuffer.Length, ref lpNumberOfBytesRead, ref lpOverlapped))
            {
                switch (usb.WaitForSingleObject(lpOverlapped.EventHandle, usb.UsbWaitTime))
                {
                    case 0:
                        if (usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, false))
                            break;
                        break;
                    case 1:
                        usb.GetOverlappedResult((IntPtr)usb.HidHandle, ref lpOverlapped, out lpNumberOfBytesTransferred, true);
                        break;
                    case 258:
                    case uint.MaxValue:
                        break;
                    default:
                        usb.CancelIo((IntPtr)usb.HidHandle);
                        lpNumberOfBytesTransferred = 0U;
                        break;
                }
            }
            for (int index = 0; (long)index < (long)lpNumberOfBytesTransferred; ++index)
            {
                usb.byte_to_string += Convert.ToChar(usb.readbuffer[index]).ToString().Replace("\0", "");
                if (usb.byte_to_string.Contains(judge))
                {
                    usb.CloseHandle(lpOverlapped.EventHandle);
                    return false;
                }
            }
            goto label_1;
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
            this.sendcommand("DIAGNOSTIC INTERFACE USB\r\n");
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
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            public UIntPtr reserved;
        }

        public struct PSP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        public enum DIGCF
        {
            DIGCF_DEFAULT = 1,
            DIGCF_PRESENT = 2,
            DIGCF_ALLCLASSES = 4,
            DIGCF_PROFILE = 8,
            DIGCF_DEVICEINTERFACE = 16, // 0x00000010
        }

        public struct DATA_BUFFER
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string Buffer;
        }

        [Flags]
        public enum ClassDevsFlags
        {
            DIGCF_DEFAULT = 1,
            DIGCF_PRESENT = 2,
            DIGCF_ALLCLASSES = 4,
            DIGCF_PROFILE = 8,
            DIGCF_DEVICEINTERFACE = 16, // 0x00000010
        }

        public enum RegPropertyType
        {
            SPDRP_DEVICEDESC = 0,
            SPDRP_HARDWAREID = 1,
            SPDRP_COMPATIBLEIDS = 2,
            SPDRP_UNUSED0 = 3,
            SPDRP_SERVICE = 4,
            SPDRP_UNUSED1 = 5,
            SPDRP_UNUSED2 = 6,
            SPDRP_CLASS = 7,
            SPDRP_CLASSGUID = 8,
            SPDRP_DRIVER = 9,
            SPDRP_CONFIGFLAGS = 10, // 0x0000000A
            SPDRP_MFG = 11, // 0x0000000B
            SPDRP_FRIENDLYNAME = 12, // 0x0000000C
            SPDRP_LOCATION_INFORMATION = 13, // 0x0000000D
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 14, // 0x0000000E
            SPDRP_CAPABILITIES = 15, // 0x0000000F
            SPDRP_UI_NUMBER = 16, // 0x00000010
            SPDRP_UPPERFILTERS = 17, // 0x00000011
            SPDRP_LOWERFILTERS = 18, // 0x00000012
            SPDRP_BUSTYPEGUID = 19, // 0x00000013
            SPDRP_LEGACYBUSTYPE = 20, // 0x00000014
            SPDRP_BUSNUMBER = 21, // 0x00000015
            SPDRP_ENUMERATOR_NAME = 22, // 0x00000016
            SPDRP_SECURITY = 23, // 0x00000017
            SPDRP_SECURITY_SDS = 24, // 0x00000018
            SPDRP_DEVTYPE = 25, // 0x00000019
            SPDRP_EXCLUSIVE = 26, // 0x0000001A
            SPDRP_CHARACTERISTICS = 27, // 0x0000001B
            SPDRP_ADDRESS = 28, // 0x0000001C
            SPDRP_UI_NUMBER_DESC_FORMAT = 30, // 0x0000001E
            SPDRP_MAXIMUM_PROPERTY = 31, // 0x0000001F
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

            public static implicit operator System.Drawing.Rectangle(usb.RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator usb.RECT(System.Drawing.Rectangle r) => new usb.RECT(r);

            public static bool operator ==(usb.RECT r1, usb.RECT r2) => r1.Equals(r2);

            public static bool operator !=(usb.RECT r1, usb.RECT r2) => !r1.Equals(r2);

            public bool Equals(usb.RECT r)
            {
                return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
            }

            public override bool Equals(object obj)
            {
                switch (obj)
                {
                    case usb.RECT r1:
                        return this.Equals(r1);
                    case System.Drawing.Rectangle r2:
                        return this.Equals(new usb.RECT(r2));
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
