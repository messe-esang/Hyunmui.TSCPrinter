using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class ComportWindowsFontTests
    {
        [Theory]
        [InlineData(false, 0, "BITMAP 40,50,2,3,1,", 0, 0)]
        [InlineData(true, 0, "BITMAP 40,50,2,3,1,", 0, 0)]
        [InlineData(false, 90, "BITMAP 40,34,1,16,1,", 0, 16)]
        [InlineData(true, 90, "BITMAP 40,34,1,16,1,", 0, 16)]
        [InlineData(false, 180, "BITMAP 24,47,2,3,1,", 16, 3)]
        [InlineData(true, 180, "BITMAP 24,47,2,3,1,", 16, 3)]
        [InlineData(false, 270, "BITMAP 37,50,1,16,1,", 3, 0)]
        [InlineData(true, 270, "BITMAP 37,50,1,16,1,", 3, 0)]
        public void PublicEntriesPreserveRequestGeometryRawBytesAndOneTerminator(bool unicode, int rotation, string header, int drawX, int drawY)
        {
            var native = new FakeGdi();
            var writes = new List<byte[]>();
            var printer = Printer(native, writes);
            Print(printer, unicode, rotation);
            Assert.Single(writes);
            var expectedPayload = new List<byte>();
            var rows = rotation == 0 || rotation == 180 ? 3 : 16;
            var width = rotation == 0 || rotation == 180 ? 2 : 1;
            for (var row = 0; row < rows; row++)
                for (var column = 0; column < width; column++) expectedPayload.Add((byte)(0x40 + row * 2 + column));
            Assert.Equal(Encoding.ASCII.GetBytes(header).Concat(expectedPayload).Concat(new byte[] { 13, 10 }), writes[0]);
            Assert.Equal(unicode, native.Request!.Unicode);
            Assert.Equal("가나다 ABC", native.Request.Content);
            Assert.Equal(24, native.Font.lfHeight);
            Assert.Equal(700, native.Font.lfWeight);
            Assert.Equal(rotation * 10, native.Font.lfEscapement);
            Assert.Equal("Test face", native.Font.lfFaceName);
            Assert.Equal(0, native.Font.lfUnderline);
            Assert.Equal((drawX, drawY), native.Origin);
            Assert.Empty(native.Owned);
        }

        [Fact]
        public void ClippedCallDoesNotChangeFollowingNormalPacket()
        {
            var native = new FakeGdi();
            var writes = new List<byte[]>();
            var printer = Printer(native, writes);
            printer.windowsfont(-1, -1, 24, 0, 0, 0, "Test", "clip");
            Assert.Equal(Encoding.ASCII.GetBytes("BITMAP 0,0,1,2,1,").Concat(new byte[] { 0x43, 0x45, 13, 10 }), writes.Single());
            writes.Clear();
            Print(printer, false, 0);
            var freshWrites = new List<byte[]>();
            Print(Printer(new FakeGdi(), freshWrites), false, 0);
            Assert.Equal(freshWrites.Single(), writes.Single());
        }

        [Fact]
        public void WriterIsCapturedOncePerCallBeforeRenderingAndExceptionIsNotRetried()
        {
            var native = new FakeGdi();
            var writes = new List<byte[]>();
            var captures = 0;
            Action<byte[], int, int> currentWriter = (data, offset, count) => writes.Add(data.Skip(offset).Take(count).ToArray());
            var printer = new comport(native, () => { captures++; return currentWriter; });
            var failure = new IOException("serial write failed");
            var failedWrites = 0;
            native.BeforeRead = () => currentWriter = (_, _, _) => { failedWrites++; Assert.Empty(native.Owned); throw failure; };
            Print(printer, false, 0);
            Assert.Single(writes);
            Assert.Equal(1, captures);
            Assert.Same(failure, Assert.Throws<IOException>(() => Print(printer, true, 0)));
            Assert.Equal(2, captures);
            Assert.Equal(1, failedWrites);
            Assert.Single(writes);
            Assert.Empty(native.Owned);
        }

        [Fact]
        public void EmptyAndFullyClippedTextNeverWriteAndNullFailsBeforeGdi()
        {
            var native = new FakeGdi();
            var writes = new List<byte[]>();
            var printer = Printer(native, writes);
            printer.windowsfont(0, 0, 24, 0, 0, 0, "Test", "");
            Assert.Equal(0, native.Acquisitions);
            Assert.Throws<ArgumentNullException>(() => printer.windowsfontunicode(0, 0, 24, 0, 0, 0, "Test", null!));
            Assert.Equal(0, native.Acquisitions);
            printer.windowsfont(-100, -100, 24, 0, 0, 0, "Test", "clip");
            Assert.Empty(writes);
            Assert.Empty(native.Owned);
        }

        [Fact]
        public async Task IndependentPrintersDoNotMixRenderingState()
        {
            using var barrier = new Barrier(2);
            async Task<byte[]> Render(byte seed, bool unicode)
            {
                return await Task.Run(() =>
                {
                    var native = new FakeGdi { Seed = seed, BeforeRead = () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(5))) };
                    var writes = new List<byte[]>();
                    Print(Printer(native, writes), unicode, 0);
                    Assert.Empty(native.Owned);
                    return writes.Single();
                });
            }
            var packets = await Task.WhenAll(Render(0x40, false), Render(0x60, true));
            Assert.Equal(Encoding.ASCII.GetBytes("BITMAP 40,50,2,3,1,").Concat(new byte[] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 13, 10 }), packets[0]);
            Assert.Equal(Encoding.ASCII.GetBytes("BITMAP 40,50,2,3,1,").Concat(new byte[] { 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 13, 10 }), packets[1]);
        }

        private static comport Printer(FakeGdi native, List<byte[]> writes) => new comport(native, () => (data, offset, count) =>
        {
            Assert.Empty(native.Owned);
            writes.Add(data.Skip(offset).Take(count).ToArray());
        });

        private static void Print(comport printer, bool unicode, int rotation)
        {
            if (unicode) printer.windowsfontunicode(40, 50, 24, rotation, 2, 1, "Test face", "가나다 ABC");
            else printer.windowsfont(40, 50, 24, rotation, 2, 1, "Test face", "가나다 ABC");
        }

        private sealed class FakeGdi : IWindowsFontGdi
        {
            internal readonly HashSet<int> Owned = new HashSet<int>();
            private readonly HashSet<int> selected = new HashSet<int>();
            internal WindowsFontRequest? Request;
            internal ethernet.LOGFONT Font = new ethernet.LOGFONT();
            internal (int, int) Origin;
            internal Action BeforeRead = () => { };
            internal byte Seed = 0x40;
            internal int Acquisitions;
            private IntPtr Acquire(int id) { Acquisitions++; Assert.True(Owned.Add(id)); return new IntPtr(id); }
            public IntPtr GetScreenDc() => Acquire(1);
            public IntPtr CreateMemoryDc(IntPtr screenDc) => Acquire(2);
            public IntPtr CreateFont(ethernet.LOGFONT font) { Font = font; return Acquire(3); }
            public IntPtr CreateBitmap() => Acquire(4);
            public IntPtr SelectObject(IntPtr dc, IntPtr value)
            {
                var id = value.ToInt32();
                if (id == 3 || id == 4) { selected.Add(id); return new IntPtr(id + 10); }
                Assert.True(selected.Remove(id - 10));
                return new IntPtr(id - 10);
            }
            public bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size) { Request = request; size = new ethernet.SIZE { cx = 16, cy = 3 }; return true; }
            public bool SetColors(IntPtr dc) => true;
            public bool Clear(IntPtr dc) => true;
            public bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y) { Assert.Same(Request, request); Origin = (x, y); return true; }
            public int ReadBitmap(IntPtr bitmap, byte[] buffer)
            {
                BeforeRead();
                for (var row = 0; row < 16; row++)
                    for (var column = 0; column < 2; column++) buffer[row * 300 + column] = (byte)(Seed + row * 2 + column);
                return buffer.Length;
            }
            public bool DeleteObject(IntPtr value) { Assert.DoesNotContain(value.ToInt32(), selected); Assert.True(Owned.Remove(value.ToInt32())); return true; }
            public bool DeleteDc(IntPtr dc) { Assert.Empty(selected); Assert.True(Owned.Remove(dc.ToInt32())); return true; }
            public bool ReleaseScreenDc(IntPtr dc) { Assert.True(Owned.Remove(dc.ToInt32())); return true; }
        }
    }
}
