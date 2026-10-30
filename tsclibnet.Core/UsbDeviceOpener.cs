using System;
using System.Runtime.InteropServices;

namespace TSCSDK
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UsbDeviceInterface
    {
        internal uint Size;
        internal Guid ClassGuid;
        internal uint Flags;
        internal UIntPtr Reserved;
    }

    internal interface IUsbDiscoveryNative
    {
        IntPtr GetDevices(out int error);
        bool GetFirstInterface(IntPtr devices, ref UsbDeviceInterface data, out int error);
        bool GetDetail(IntPtr devices, ref UsbDeviceInterface data, IntPtr buffer, uint capacity, out uint required, out int error);
        bool DestroyDevices(IntPtr devices, out int error);
        int OpenDevice(string path, uint flags, out int error);
    }

    internal sealed class UsbDeviceOpener
    {
        private readonly IUsbDiscoveryNative native;
        private readonly Func<int, IntPtr> allocate;
        private readonly Action<IntPtr> free;

        internal UsbDeviceOpener() : this(new UsbDiscoveryNative(), Marshal.AllocCoTaskMem, Marshal.FreeCoTaskMem) { }

        internal UsbDeviceOpener(IUsbDiscoveryNative native, Func<int, IntPtr> allocate, Action<IntPtr> free)
        {
            this.native = native;
            this.allocate = allocate;
            this.free = free;
        }

        // False means discovery failed and the caller must preserve its old handle.
        // True means CreateFile was attempted, including its legacy -1 failure result.
        internal bool TryOpen(bool overlapped, out int handle)
        {
            handle = -1;
            string path;
            if (!TryDiscover(out path)) return false;
            int error;
            handle = native.OpenDevice(path, overlapped ? 0x40000080U : 0U, out error);
            return true;
        }

        private bool TryDiscover(out string path)
        {
            path = null;
            int error;
            var devices = native.GetDevices(out error);
            if (devices == new IntPtr(-1) || devices == IntPtr.Zero) return false;
            Exception primary = null;
            try
            {
                var data = new UsbDeviceInterface { Size = (uint)Marshal.SizeOf(typeof(UsbDeviceInterface)) };
                if (!native.GetFirstInterface(devices, ref data, out error)) return false;
                uint required;
                var probe = native.GetDetail(devices, ref data, IntPtr.Zero, 0, out required, out error);
                if (probe || error != 122 || !ValidSize(required)) return false;
                return TryReadPath(devices, ref data, required, out path);
            }
            catch (Exception failure)
            {
                primary = failure;
                throw;
            }
            finally
            {
                try
                {
                    if (!native.DestroyDevices(devices, out error))
                        throw new TscException("SetupDiDestroyDeviceInfoList failed with native error " + error);
                }
                catch (Exception cleanupFailure)
                {
                    if (primary == null) throw;
                    primary.Data["UsbDiscoveryCleanupError"] = cleanupFailure;
                }
            }
        }

        private bool TryReadPath(IntPtr devices, ref UsbDeviceInterface data, uint capacity, out string path)
        {
            path = null;
            var buffer = allocate((int)capacity);
            if (buffer == IntPtr.Zero) throw new OutOfMemoryException("USB interface detail allocation failed.");
            Exception primary = null;
            try
            {
                // Initialize every character as nonzero so an absent terminator cannot
                // accidentally consume an uninitialized zero beyond native output.
                var initial = new byte[(int)capacity];
                for (var index = 0; index < initial.Length; index++) initial[index] = 0xff;
                Marshal.Copy(initial, 0, buffer, initial.Length);
                Marshal.WriteInt32(buffer, DetailHeaderSize(IntPtr.Size));
                uint returned;
                int error;
                if (!native.GetDetail(devices, ref data, buffer, capacity, out returned, out error)
                    || !ValidSize(returned) || returned > capacity) return false;
                path = ReadPath(buffer, returned);
                return path != null;
            }
            catch (Exception failure)
            {
                primary = failure;
                throw;
            }
            finally
            {
                try { free(buffer); }
                catch (Exception cleanupFailure)
                {
                    if (primary == null) throw;
                    primary.Data["UsbDiscoveryBufferCleanupError"] = cleanupFailure;
                }
            }
        }

        internal static int DetailHeaderSize(int pointerSize)
        {
            if (pointerSize == 4) return 6;
            if (pointerSize == 8) return 8;
            throw new NotSupportedException("Unsupported USB discovery pointer size.");
        }

        private static bool ValidSize(uint size)
        {
            return size >= DetailHeaderSize(IntPtr.Size) && size <= int.MaxValue && (size - 4) % 2 == 0;
        }

        internal static string ReadPath(IntPtr buffer, uint size)
        {
            var characters = new char[((int)size - 4) / 2];
            Marshal.Copy(IntPtr.Add(buffer, 4), characters, 0, characters.Length);
            var terminator = Array.IndexOf(characters, '\0');
            return terminator <= 0 ? null : new string(characters, 0, terminator);
        }
    }

    internal sealed class UsbDiscoveryNative : IUsbDiscoveryNative
    {
        private static readonly Guid UsbPrint = new Guid("28d78fad-5a12-11d1-ae5b-0000f803a8c2");

        public IntPtr GetDevices(out int error)
        {
            var classGuid = UsbPrint;
            var result = SetupDiGetClassDevsW(ref classGuid, IntPtr.Zero, IntPtr.Zero, 0x12);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public bool GetFirstInterface(IntPtr devices, ref UsbDeviceInterface data, out int error)
        {
            var classGuid = UsbPrint;
            var result = SetupDiEnumDeviceInterfaces(devices, IntPtr.Zero, ref classGuid, 0, ref data);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public bool GetDetail(IntPtr devices, ref UsbDeviceInterface data, IntPtr buffer, uint capacity, out uint required, out int error)
        {
            var result = SetupDiGetDeviceInterfaceDetailW(devices, ref data, buffer, capacity, out required, IntPtr.Zero);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public bool DestroyDevices(IntPtr devices, out int error)
        {
            var result = SetupDiDestroyDeviceInfoList(devices);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public int OpenDevice(string path, uint flags, out int error)
        {
            var result = CreateFileW(path, 0xc0000000U, 3, IntPtr.Zero, 3, flags, IntPtr.Zero);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, IntPtr enumerator, IntPtr window, uint flags);

        [DllImport("setupapi.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr devices, IntPtr device, ref Guid classGuid, uint index, ref UsbDeviceInterface data);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr devices, ref UsbDeviceInterface data, IntPtr detail, uint size, out uint required, IntPtr info);

        [DllImport("setupapi.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr devices);

        // Preserve the existing SDK's integer device-handle ABI.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern int CreateFileW(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    }
}
