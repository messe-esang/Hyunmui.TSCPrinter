using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class UsbWindowsFontTests
    {
        [Theory]
        [InlineData(false, 0, "BITMAP 40,50,2,2,1,")]
        [InlineData(true, 0, "BITMAP 40,50,2,2,1,")]
        [InlineData(false, 90, "BITMAP 40,34,1,16,1,")]
        [InlineData(true, 90, "BITMAP 40,34,1,16,1,")]
        [InlineData(false, 180, "BITMAP 24,48,2,2,1,")]
        [InlineData(true, 180, "BITMAP 24,48,2,2,1,")]
        [InlineData(false, 270, "BITMAP 38,50,1,16,1,")]
        [InlineData(true, 270, "BITMAP 38,50,1,16,1,")]
        public void PublicFontMethodsWriteExactPacketAfterNativeCleanup(bool unicode, int rotation, string header)
        {
            var native = new FakeGdi();
            var stream = new Kernel { OnWrite = () => Assert.Equal(0, native.OwnedHandles) };
            var transport = new usb(native, new UsbRawWriter(stream), () => 71);
            Send(transport, unicode, rotation, 40, 50);
            var sideways = rotation == 90 || rotation == 270;
            var payload = Enumerable.Range(0, sideways ? 16 : 2)
                .SelectMany(row => Enumerable.Range(0, sideways ? 1 : 2).Select(column => (byte)(0x10 + row * 8 + column)));
            Assert.Equal(Encoding.ASCII.GetBytes(header).Concat(payload).Concat(new byte[] { 13, 10 }), stream.Bytes);
            Assert.Equal(unicode, native.Unicode);
            Assert.Equal(24, native.Font.lfHeight);
            Assert.Equal(rotation * 10, native.Font.lfEscapement);
            Assert.Equal(700, native.Font.lfWeight);
            Assert.Equal(0, native.Font.lfUnderline);
            Assert.Equal("Test font", native.Font.lfFaceName);
            Assert.All(stream.Handles, handle => Assert.Equal((IntPtr)71, handle));
            Assert.Equal(1, stream.WriteCalls);
        }

        [Fact]
        public void NegativeThenNormalCallsAndSeparateInstancesKeepIndependentPayloads()
        {
            var firstStream = new Kernel();
            var secondStream = new Kernel();
            var first = new usb(new FakeGdi(), new UsbRawWriter(firstStream), () => 71);
            var second = new usb(new FakeGdi { Seed = 0x40 }, new UsbRawWriter(secondStream), () => 72);
            Send(first, false, 0, -1, -1);
            Send(second, true, 0, 3, 5);
            Send(first, true, 0, 3, 5);
            Assert.Equal(Packet("BITMAP 0,0,1,1,1,", 0x19).Concat(Packet("BITMAP 3,5,2,2,1,", 0x10, 0x11, 0x18, 0x19)), firstStream.Bytes);
            Assert.Equal(Packet("BITMAP 3,5,2,2,1,", 0x40, 0x41, 0x48, 0x49), secondStream.Bytes);
        }

        [Fact]
        public void HandleCapturedBeforeRenderingAndPublicStatusRemainsOne()
        {
            var native = new FakeGdi();
            var stream = new Kernel();
            var handle = 71;
            native.OnDraw = () => handle = 72;
            var writer = new UsbRawWriter(stream);
            Send(new usb(native, writer, () => handle), false, 0, 3, 5);
            Assert.All(stream.Handles, value => Assert.Equal((IntPtr)71, value));
            Assert.Equal(1, usb.WriteStatus((IntPtr)72, new byte[] { 1, 2, 3 }, writer));
        }

        [Fact]
        public void TransportFailureOccursAfterGdiCleanup()
        {
            var native = new FakeGdi();
            var stream = new Kernel { Error = 5, OnWrite = () => Assert.Equal(0, native.OwnedHandles) };
            Assert.Throws<TscException>(() => Send(new usb(native, new UsbRawWriter(stream), () => 71), true, 0, 3, 5));
            Assert.Equal(1, stream.Closed);
        }

        [Fact]
        public void NativeFailureEmptyAndClippedTextDoNotStartIo()
        {
            var native = new FakeGdi { FailDraw = true };
            var stream = new Kernel();
            var transport = new usb(native, new UsbRawWriter(stream), () => 71);
            Assert.Throws<InvalidOperationException>(() => Send(transport, true, 0, 3, 5));
            native.FailDraw = false;
            transport.windowsfont(3, 5, 24, 0, 0, 0, "Test font", "");
            Send(transport, true, 0, -16, -2);
            Assert.Equal(0, stream.WriteCalls);
            Assert.Equal(0, native.OwnedHandles);
        }

        private static byte[] Packet(string header, params byte[] payload) => Encoding.ASCII.GetBytes(header).Concat(payload).Concat(new byte[] { 13, 10 }).ToArray();
        private static void Send(usb transport, bool unicode, int rotation, int x, int y)
        {
            if (unicode) transport.windowsfontunicode(x, y, 24, rotation, 3, 1, "Test font", "테스트");
            else transport.windowsfont(x, y, 24, rotation, 3, 1, "Test font", "Text");
        }
        private sealed class Kernel : IUsbWriteKernel
        {
            internal readonly List<byte> Bytes = new();
            internal readonly List<IntPtr> Handles = new();
            internal Action OnWrite = () => { };
            internal int WriteCalls;
            internal int Closed;
            internal int Error;
            public IntPtr CreateEvent(out int error) { error = 0; return (IntPtr)99; }
            public UsbIoResult Write(IntPtr handle, IntPtr buffer, uint count, IntPtr overlapped)
            {
                WriteCalls++;
                Handles.Add(handle);
                OnWrite();
                var bytes = new byte[count];
                Marshal.Copy(buffer, bytes, 0, bytes.Length);
                Bytes.AddRange(bytes);
                return new UsbIoResult(Error == 0, Error == 0 ? count : 0, Error);
            }
            public uint Wait(IntPtr eventHandle, uint milliseconds, out int error) => throw new InvalidOperationException("Unexpected wait");
            public UsbIoResult Complete(IntPtr handle, IntPtr overlapped) => throw new InvalidOperationException("Unexpected completion");
            public UsbIoResult Cancel(IntPtr handle, IntPtr overlapped) => throw new InvalidOperationException("Unexpected cancel");
            public void CloseEvent(IntPtr eventHandle) { Closed++; }
        }

        private sealed class FakeGdi : IWindowsFontGdi
        {
            internal int OwnedHandles;
            internal int CreateCalls;
            internal byte Seed = 0x10;
            internal bool Unicode;
            internal bool FailDraw;
            internal ethernet.LOGFONT Font = new ethernet.LOGFONT();
            internal Action OnDraw = () => { };

            private IntPtr Handle(int id) { OwnedHandles++; CreateCalls++; return new IntPtr(id); }
            public IntPtr GetScreenDc() => Handle(1);
            public IntPtr CreateMemoryDc(IntPtr screenDc) => Handle(2);
            public IntPtr CreateFont(ethernet.LOGFONT font) { Font = font; return Handle(3); }
            public IntPtr CreateBitmap() => Handle(4);
            public IntPtr SelectObject(IntPtr dc, IntPtr value) => value == new IntPtr(3) ? new IntPtr(11) : new IntPtr(12);
            public bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size) { Unicode = request.Unicode; size = new ethernet.SIZE { cx = 16, cy = 2 }; return true; }
            public bool SetColors(IntPtr dc) => true;
            public bool Clear(IntPtr dc) => true;
            public bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y) { Assert.Equal(Unicode, request.Unicode); OnDraw(); return !FailDraw; }
            public int ReadBitmap(IntPtr bitmap, byte[] buffer)
            {
                for (var row = 0; row < 32; row++)
                    for (var column = 0; column < 8; column++) buffer[row * WindowsFontGeometry.Stride + column] = (byte)(Seed + row * 8 + column);
                return buffer.Length;
            }
            public bool DeleteObject(IntPtr value) { OwnedHandles--; return true; }
            public bool DeleteDc(IntPtr dc) { OwnedHandles--; return true; }
            public bool ReleaseScreenDc(IntPtr dc) { OwnedHandles--; return true; }
        }
    }
}
