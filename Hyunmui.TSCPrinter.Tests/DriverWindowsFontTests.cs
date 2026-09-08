using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class DriverWindowsFontTests
    {
        [Theory]
        [InlineData(false, 0, 40, 50, 2, 2)]
        [InlineData(true, 90, 40, 34, 1, 16)]
        [InlineData(false, 180, 24, 48, 2, 2)]
        [InlineData(true, 270, 38, 50, 1, 16)]
        public void PublicFontMethodsPreserveRequestAndRawBitmapProtocol(bool unicode, int rotation, int x, int y, int width, int rows)
        {
            var gdi = new FakeFontGdi();
            var printer = new MemoryPrinter(gdi);
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            if (unicode) sut.windowsfontunicode(40, 50, 23, rotation, 2, 1, "Test face", "한글");
            else sut.windowsfont(40, 50, 23, rotation, 2, 1, "Test face", "Latin");

            Assert.Equal(unicode, gdi.Request!.Unicode);
            Assert.Equal(unicode ? "한글" : "Latin", gdi.Request.Content);
            Assert.Equal(23, gdi.Font!.lfHeight);
            Assert.Equal(rotation * 10, gdi.Font.lfEscapement);
            Assert.Equal(700, gdi.Font.lfWeight);
            Assert.Equal(0, gdi.Font.lfUnderline); // Existing public parameter remains ignored.
            Assert.Equal("Test face", gdi.Font.lfFaceName);
            var payload = Enumerable.Range(0, rows).SelectMany(row => Enumerable.Range(0, width).Select(column => (byte)(row * 8 + column)));
            Assert.Equal(Packet($"BITMAP {x},{y},{width},{rows},1,", payload), printer.Bytes);
            Assert.Single(printer.Handles);
            Assert.Empty(gdi.Owned);
        }

        [Fact]
        public void PartialWritesUseRemainingBytesAndOneCapturedPrinterHandle()
        {
            var gdi = new FakeFontGdi();
            var printer = new MemoryPrinter(gdi) { MaxWrite = 3 };
            var handle = new IntPtr(44);
            var handleReads = 0;
            printer.BeforeWrite = () => handle = new IntPtr(99);
            var sut = new driver(gdi, () => { handleReads++; return handle; }, printer.Write);
            sut.windowsfont(40, 50, 20, 0, 0, 0, "Test face", "Text");

            Assert.Equal(Packet("BITMAP 40,50,2,2,1,", new byte[] { 0, 1, 8, 9 }), printer.Bytes);
            Assert.True(printer.Handles.Count > 1);
            Assert.All(printer.Handles, value => Assert.Equal(new IntPtr(44), value));
            Assert.Equal(1, handleReads);
            Assert.Equal(400, gdi.Font!.lfWeight);
        }

        [Fact]
        public void ClippedCallDoesNotPoisonNextPublicFontCall()
        {
            var gdi = new FakeFontGdi { Size = new ethernet.SIZE { cx = 24, cy = 3 } };
            var printer = new MemoryPrinter(gdi);
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            sut.windowsfont(-1, -1, 20, 0, 0, 0, "Test face", "First");
            Assert.Equal(Packet("BITMAP 0,0,2,2,1,", new byte[] { 9, 10, 17, 18 }), printer.Bytes);
            printer.Bytes.Clear();
            sut.windowsfontunicode(4, 5, 20, 0, 0, 0, "Test face", "Second");
            Assert.Equal(Packet("BITMAP 4,5,3,3,1,", new byte[] { 0, 1, 2, 8, 9, 10, 16, 17, 18 }), printer.Bytes);
            Assert.Empty(gdi.Owned);
        }

        [Fact]
        public void EmptyAndFullyClippedCallsNeverWrite()
        {
            var gdi = new FakeFontGdi();
            var printer = new MemoryPrinter(gdi);
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            sut.windowsfont(0, 0, 20, 0, 0, 0, "Test face", "");
            Assert.Null(gdi.Request);
            sut.windowsfontunicode(int.MinValue, int.MinValue, 20, 0, 0, 0, "Test face", "Text");
            Assert.Empty(printer.Bytes);
            Assert.Empty(printer.Handles);
            Assert.Empty(gdi.Owned);
        }

        [Fact]
        public void FailedNativeWriteIsSurfacedAfterGdiCleanupWithoutRetrying()
        {
            var gdi = new FakeFontGdi();
            var printer = new MemoryPrinter(gdi) { Succeed = false };
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            Assert.Throws<Win32Exception>(() => sut.windowsfont(0, 0, 20, 0, 0, 0, "Test face", "Text"));
            Assert.Single(printer.Handles);
            Assert.Empty(printer.Bytes);
            Assert.Empty(gdi.Owned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void InvalidNativeWrittenCountsDoNotLoopOrOverrun(int written)
        {
            var gdi = new FakeFontGdi();
            var printer = new MemoryPrinter(gdi) { ReportWritten = written };
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            Assert.Throws<IOException>(() => sut.windowsfontunicode(0, 0, 20, 0, 0, 0, "Test face", "Text"));
            Assert.Single(printer.Handles);
            Assert.Empty(gdi.Owned);
        }

        [Fact]
        public void RendererFailureCleansResourcesAndNeverCallsSpoolWriter()
        {
            var gdi = new FakeFontGdi { FailDraw = true };
            var printer = new MemoryPrinter(gdi);
            var sut = new driver(gdi, () => new IntPtr(44), printer.Write);
            Assert.Throws<InvalidOperationException>(() => sut.windowsfont(0, 0, 20, 0, 0, 0, "Test face", "Text"));
            Assert.Empty(printer.Handles);
            Assert.Empty(gdi.Owned);
        }

        [Fact]
        public async Task OverlappingDriversDoNotMixFontStateOrPayload()
        {
            using var barrier = new Barrier(2);
            var firstGdi = new FakeFontGdi { BeforeRead = () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10))) };
            var secondGdi = new FakeFontGdi { BaseByte = 64, BeforeRead = () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10))) };
            var first = new MemoryPrinter(firstGdi);
            var second = new MemoryPrinter(secondGdi);
            await Task.WhenAll(
                Task.Run(() => new driver(firstGdi, () => new IntPtr(1), first.Write).windowsfont(-1, 0, 20, 0, 0, 0, "First", "First")),
                Task.Run(() => new driver(secondGdi, () => new IntPtr(2), second.Write).windowsfontunicode(40, 50, 20, 0, 0, 0, "Second", "Second")));
            Assert.Equal(Packet("BITMAP 0,0,1,2,1,", new byte[] { 1, 9 }), first.Bytes);
            Assert.Equal(Packet("BITMAP 40,50,2,2,1,", new byte[] { 64, 65, 72, 73 }), second.Bytes);
            Assert.Empty(firstGdi.Owned);
            Assert.Empty(secondGdi.Owned);
        }

        private static byte[] Packet(string header, IEnumerable<byte> payload) =>
            Encoding.ASCII.GetBytes(header).Concat(payload).Concat(new byte[] { 13, 10 }).ToArray();

        private sealed class MemoryPrinter
        {
            private readonly FakeFontGdi gdi;
            internal readonly List<byte> Bytes = new List<byte>();
            internal readonly List<IntPtr> Handles = new List<IntPtr>();
            internal int MaxWrite = int.MaxValue;
            internal int? ReportWritten;
            internal bool Succeed = true;
            internal Action BeforeWrite = () => { };
            internal MemoryPrinter(FakeFontGdi native) { gdi = native; }
            internal bool Write(IntPtr handle, byte[] bytes, int count, out int written)
            {
                Assert.Empty(gdi.Owned);
                Assert.Equal(bytes.Length, count);
                Handles.Add(handle);
                BeforeWrite();
                written = ReportWritten ?? Math.Min(count, MaxWrite);
                if (Succeed && ReportWritten == null) Bytes.AddRange(bytes.Take(written));
                return Succeed;
            }
        }

        private sealed class FakeFontGdi : IWindowsFontGdi
        {
            internal readonly HashSet<IntPtr> Owned = new HashSet<IntPtr>();
            internal ethernet.SIZE Size = new ethernet.SIZE { cx = 16, cy = 2 };
            internal ethernet.LOGFONT? Font;
            internal WindowsFontRequest? Request;
            internal byte BaseByte;
            internal bool FailDraw;
            internal Action BeforeRead = () => { };
            private IntPtr Acquire(int value) { var handle = new IntPtr(value); Assert.True(Owned.Add(handle)); return handle; }
            public IntPtr GetScreenDc() => Acquire(1);
            public IntPtr CreateMemoryDc(IntPtr screenDc) => Acquire(2);
            public IntPtr CreateFont(ethernet.LOGFONT font) { Font = font; return Acquire(3); }
            public IntPtr CreateBitmap() => Acquire(4);
            public IntPtr SelectObject(IntPtr dc, IntPtr value) => new IntPtr(value.ToInt32() == 3 ? 13 : value.ToInt32() == 4 ? 14 : value.ToInt32() - 10);
            public bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size) { Request = request; size = Size; return true; }
            public bool SetColors(IntPtr dc) => true;
            public bool Clear(IntPtr dc) => true;
            public bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y) => !FailDraw;
            public int ReadBitmap(IntPtr bitmap, byte[] buffer)
            {
                BeforeRead();
                for (var row = 0; row < 32; row++)
                    for (var column = 0; column < 8; column++)
                        buffer[row * WindowsFontGeometry.Stride + column] = (byte)(BaseByte + row * 8 + column);
                return buffer.Length;
            }
            public bool DeleteObject(IntPtr value) { Assert.True(Owned.Remove(value)); return true; }
            public bool DeleteDc(IntPtr dc) { Assert.True(Owned.Remove(dc)); return true; }
            public bool ReleaseScreenDc(IntPtr dc) { Assert.True(Owned.Remove(dc)); return true; }
        }
    }
}
