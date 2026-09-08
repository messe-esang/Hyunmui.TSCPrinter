using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TSCSDK
{
    internal struct UsbIoResult
    {
        internal UsbIoResult(bool success, uint count, int error)
        {
            Success = success;
            Count = count;
            Error = error;
        }

        internal bool Success { get; }
        internal uint Count { get; }
        internal int Error { get; }
    }

    internal interface IUsbWriteKernel
    {
        IntPtr CreateEvent(out int error);
        UsbIoResult Write(IntPtr handle, IntPtr buffer, uint count, IntPtr overlapped);
        uint Wait(IntPtr eventHandle, uint milliseconds, out int error);
        UsbIoResult Complete(IntPtr handle, IntPtr overlapped);
        UsbIoResult Cancel(IntPtr handle, IntPtr overlapped);
        void CloseEvent(IntPtr eventHandle);
    }

    // The event and request storage belong to one write, never to the device handle.
    [StructLayout(LayoutKind.Sequential)]
    internal struct UsbOverlapped
    {
        internal UIntPtr Internal;
        internal UIntPtr InternalHigh;
        internal uint Offset;
        internal uint OffsetHigh;
        internal IntPtr EventHandle;
    }

    internal sealed class UsbRawWriter
    {
        internal const int IoPending = 997;
        internal const int IoIncomplete = 996;
        internal const uint Timeout = 258;
        internal const uint Infinite = uint.MaxValue;

        private readonly IUsbWriteKernel kernel;
        private readonly object faultLock = new object();
        private readonly List<Request> quarantine = new List<Request>();

        internal UsbRawWriter(IUsbWriteKernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        internal int Write(IntPtr handle, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Write(handle, buffer, 0, buffer.Length);
        }

        internal int Write(IntPtr handle, byte[] buffer, int start, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (start < 0 || count < 0 || start > buffer.Length - count)
                throw new ArgumentOutOfRangeException(nameof(count));
            CheckFault();
            var owned = new byte[count];
            Array.Copy(buffer, start, owned, 0, count);
            var offset = 0;
            while (offset < owned.Length)
            {
                CheckFault();
                offset += WriteRequest(handle, owned, offset);
            }
            return offset;
        }

        private void CheckFault()
        {
            lock (faultLock)
            {
                if (quarantine.Count != 0)
                    throw new TscException("USB writer is faulted after unproven I/O completion; restart the process before writing again.");
            }
        }

        private int WriteRequest(IntPtr handle, byte[] buffer, int offset)
        {
            var request = new Request(buffer, kernel);
            Exception primaryFailure = null;
            try
            {
                var result = kernel.Write(handle, IntPtr.Add(request.Buffer, offset), (uint)(buffer.Length - offset), request.Overlapped);
                if (!result.Success)
                {
                    if (result.Error != IoPending) throw Failure("WriteFile", result.Error);
                    request.Pending = true;
                    result = AwaitCompletion(handle, request, kernel);
                }
                if (result.Count == 0 || result.Count > buffer.Length - offset)
                    throw new TscException("WriteFile returned an invalid byte count: " + result.Count);
                return (int)result.Count;
            }
            catch (Exception failure)
            {
                primaryFailure = failure;
                if (request.Pending)
                {
                    lock (faultLock) quarantine.Add(request);
                    failure.Data["UsbWriteCompletion"] = "Unproven completion: request storage retained; USB writer faulted until process restart.";
                }
                throw;
            }
            finally
            {
                request.ReleaseIfComplete(primaryFailure);
            }
        }

        private static UsbIoResult AwaitCompletion(IntPtr handle, Request request, IUsbWriteKernel kernel)
        {
            int error;
            var wait = kernel.Wait(request.Event, 2000, out error);
            if (wait == 0)
            {
                request.Pending = false; // Only this private event can signal completion of this request.
                var result = kernel.Complete(handle, request.Overlapped);
                if (!result.Success)
                {
                    if (result.Error == IoIncomplete) request.Pending = true;
                    throw Failure("GetOverlappedResult", result.Error);
                }
                return result;
            }

            var failure = wait == Timeout ? new TscException("WAIT_TIMEOUT") : Failure("WaitForSingleObject", error);
            // Cancellation is a request, not completion, including ERROR_NOT_FOUND races.
            try
            {
                kernel.Cancel(handle, request.Overlapped);
                var drained = kernel.Wait(request.Event, Infinite, out error);
                if (drained == 0)
                {
                    request.Pending = false;
                    var completion = kernel.Complete(handle, request.Overlapped);
                    if (!completion.Success && completion.Error == IoIncomplete) request.Pending = true;
                }
            }
            catch (Exception cleanupFailure)
            {
                request.Pending = true;
                failure.Data["UsbWriteDrainError"] = cleanupFailure;
            }
            throw failure;
        }

        private static TscException Failure(string operation, int error)
        {
            return new TscException(operation + " failed with native error " + error);
        }

        private sealed class Request
        {
            private readonly IUsbWriteKernel kernel;
            private GCHandle pin;
            internal IntPtr Buffer { get; private set; }
            internal IntPtr Overlapped { get; private set; }
            internal IntPtr Event { get; private set; }
            internal bool Pending { get; set; }

            internal Request(byte[] buffer, IUsbWriteKernel kernel)
            {
                this.kernel = kernel;
                try
                {
                    int error;
                    Event = kernel.CreateEvent(out error);
                    if (Event == IntPtr.Zero) throw Failure("CreateEvent", error);
                    pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Buffer = pin.AddrOfPinnedObject();
                    Overlapped = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UsbOverlapped)));
                    Marshal.StructureToPtr(new UsbOverlapped { EventHandle = Event }, Overlapped, false);
                }
                catch
                {
                    ReleaseIfComplete();
                    throw;
                }
            }

            internal void ReleaseIfComplete(Exception primaryFailure = null)
            {
                // Fault containment: quarantined requests retain their pin, event and
                // OVERLAPPED until process exit; an uncertain driver may still access them.
                if (Pending) return;
                if (Overlapped != IntPtr.Zero) Marshal.FreeHGlobal(Overlapped);
                if (pin.IsAllocated) pin.Free();
                if (Event == IntPtr.Zero) return;
                try
                {
                    kernel.CloseEvent(Event);
                }
                catch (Exception cleanupFailure)
                {
                    if (primaryFailure == null) throw;
                    primaryFailure.Data["UsbWriteCloseError"] = cleanupFailure;
                }
            }
        }
    }

    internal sealed class UsbWriteKernel : IUsbWriteKernel
    {
        public IntPtr CreateEvent(out int error)
        {
            var result = CreateEventW(IntPtr.Zero, true, false, null);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public UsbIoResult Write(IntPtr handle, IntPtr buffer, uint count, IntPtr overlapped)
        {
            uint transferred;
            var success = WriteFile(handle, buffer, count, out transferred, overlapped);
            return new UsbIoResult(success, transferred, Marshal.GetLastWin32Error());
        }

        public uint Wait(IntPtr eventHandle, uint milliseconds, out int error)
        {
            var result = WaitForSingleObject(eventHandle, milliseconds);
            error = Marshal.GetLastWin32Error();
            return result;
        }

        public UsbIoResult Complete(IntPtr handle, IntPtr overlapped)
        {
            uint transferred;
            var success = GetOverlappedResult(handle, overlapped, out transferred, false);
            return new UsbIoResult(success, transferred, Marshal.GetLastWin32Error());
        }

        public UsbIoResult Cancel(IntPtr handle, IntPtr overlapped)
        {
            var success = CancelIoEx(handle, overlapped);
            return new UsbIoResult(success, 0, Marshal.GetLastWin32Error());
        }

        public void CloseEvent(IntPtr eventHandle)
        {
            if (!CloseHandle(eventHandle))
                throw new TscException("CloseHandle(event) failed with native error " + Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr CreateEventW(IntPtr attributes, [MarshalAs(UnmanagedType.Bool)] bool manualReset,
            [MarshalAs(UnmanagedType.Bool)] bool initialState, string name);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(IntPtr handle, IntPtr buffer, uint count, out uint transferred, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOverlappedResult(IntPtr handle, IntPtr overlapped, out uint transferred,
            [MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelIoEx(IntPtr handle, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
    }

}
