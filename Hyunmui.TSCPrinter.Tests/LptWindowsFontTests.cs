using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class LptWindowsFontTests
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
            using var stream = new MemoryFileStream { OnWrite = () => Assert.Equal(0, native.OwnedHandles) };
            var transport = new lpt(stream, native);
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
            Assert.True(stream.CanWrite);
            Assert.Equal(1, stream.WriteCalls);
        }

        [Fact]
        public void NegativeThenNormalCallsAndSeparateInstancesKeepIndependentPayloads()
        {
            using var firstStream = new MemoryFileStream();
            using var secondStream = new MemoryFileStream();
            var first = new lpt(firstStream, new FakeGdi());
            var second = new lpt(secondStream, new FakeGdi { Seed = 0x40 });
            Send(first, false, 0, -1, -1);
            Send(second, true, 0, 3, 5);
            Send(first, true, 0, 3, 5);
            Assert.Equal(Packet("BITMAP 0,0,1,1,1,", 0x19).Concat(Packet("BITMAP 3,5,2,2,1,", 0x10, 0x11, 0x18, 0x19)), firstStream.Bytes);
            Assert.Equal(Packet("BITMAP 3,5,2,2,1,", 0x40, 0x41, 0x48, 0x49), secondStream.Bytes);
        }

        [Fact]
        public void StreamIsCapturedBeforeRenderingAndRemainsOwnedByTheTransport()
        {
            var native = new FakeGdi();
            using var original = new MemoryFileStream();
            using var replacement = new MemoryFileStream();
            var transport = new lpt(original, native);
            native.OnDraw = () => typeof(lpt).GetField("lptstream", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(transport, replacement);
            Send(transport, false, 0, 3, 5);
            Assert.Equal(Packet("BITMAP 3,5,2,2,1,", 0x10, 0x11, 0x18, 0x19), original.Bytes);
            Assert.Empty(replacement.Bytes);
            Assert.True(original.CanWrite);
            Assert.True(replacement.CanWrite);
        }

        [Fact]
        public void StreamExceptionIsPreservedAfterAllGdiResourcesAreReleased()
        {
            var native = new FakeGdi();
            var failure = new IOException("captured write failure");
            using var stream = new MemoryFileStream { OnWrite = () => { Assert.Equal(0, native.OwnedHandles); throw failure; } };
            var error = Assert.Throws<IOException>(() => Send(new lpt(stream, native), false, 0, 3, 5));
            Assert.Same(failure, error);
            Assert.Empty(stream.Bytes);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void ClosedStreamKeepsItsObjectDisposedException()
        {
            var native = new FakeGdi();
            using var stream = new MemoryFileStream();
            var transport = new lpt(stream, native);
            stream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => Send(transport, true, 0, 3, 5));
            Assert.Equal(0, native.OwnedHandles);
        }

        [Fact]
        public void NativeFailureDoesNotWriteOrCloseTheStream()
        {
            var native = new FakeGdi { FailDraw = true };
            using var stream = new MemoryFileStream();
            Assert.Throws<InvalidOperationException>(() => Send(new lpt(stream, native), true, 0, 3, 5));
            Assert.Equal(0, native.OwnedHandles);
            Assert.Equal(0, stream.WriteCalls);
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void EmptyOrFullyClippedTextDoesNotWrite()
        {
            var native = new FakeGdi();
            using var stream = new MemoryFileStream();
            var transport = new lpt(stream, native);
            transport.windowsfont(3, 5, 24, 0, 0, 0, "Test font", "");
            Assert.Equal(0, native.CreateCalls);
            Send(transport, true, 0, -16, -2);
            Assert.Equal(0, stream.WriteCalls);
            Assert.Equal(0, native.OwnedHandles);
        }

        [Theory]
        [InlineData("ascii")]
        [InlineData("utf8")]
        [InlineData("gb2312")]
        [InlineData("big5")]
        [InlineData("noCrLf")]
        public void StringCommandsPreserveConfiguredEncodingAndFraming(string method)
        {
            using var stream = new MemoryFileStream();
            var transport = new lpt(stream, new FakeGdi());
            const string command = "A\0한中文\r\n";
            var encoding = GetCommandEncoding(method);
            if (encoding == null)
            {
                Assert.Throws<ArgumentException>(() => SendStringCommand(transport, method, command));
                Assert.Equal(0, stream.WriteCalls);
                return;
            }
            Assert.Equal(1, SendStringCommand(transport, method, command));
            var expected = encoding.GetBytes(command);
            Assert.Equal(method == "noCrLf" ? expected : expected.Concat(new byte[] { 13, 10 }), stream.Bytes);
            Assert.Equal(method == "noCrLf" ? 1 : 2, stream.WriteCalls);
            Assert.True(stream.CanWrite);
        }

        [Theory]
        [InlineData("ascii")]
        [InlineData("utf8")]
        [InlineData("gb2312")]
        [InlineData("big5")]
        [InlineData("noCrLf")]
        public void StringCommandsPreserveEncodingAndWriteFailures(string method)
        {
            using var stream = new MemoryFileStream();
            var transport = new lpt(stream, new FakeGdi());
            var failure = new IOException("write failure");
            stream.OnWrite = () => throw failure;
            if (GetCommandEncoding(method) == null)
            {
                Assert.Throws<ArgumentException>(() => SendStringCommand(transport, method, "A"));
                Assert.Equal(0, stream.WriteCalls);
            }
            else
            {
                Assert.Same(failure, Assert.Throws<IOException>(() => SendStringCommand(transport, method, "A")));
                Assert.Equal(1, stream.WriteCalls);
                Assert.Empty(stream.Bytes);
            }
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void InterleavedStringCommandsKeepIndependentInstanceBytes()
        {
            using var firstStream = new MemoryFileStream();
            using var secondStream = new MemoryFileStream();
            var first = new lpt(firstStream, new FakeGdi());
            var second = new lpt(secondStream, new FakeGdi());
            var reentered = false;
            firstStream.OnWrite = () =>
            {
                if (reentered) return;
                reentered = true;
                Assert.Equal(1, second.sendcommandNOCRLF("longer 한글 command"));
            };
            first.sendcommand("A");
            first.sendcommand_utf8("한");
            Assert.Equal(Encoding.UTF8.GetBytes("A\r\n한\r\n"), firstStream.Bytes);
            Assert.Equal(Encoding.UTF8.GetBytes("longer 한글 command"), secondStream.Bytes);
        }

        private static Encoding? GetCommandEncoding(string method)
        {
            if (method == "ascii") return Encoding.ASCII;
            if (method == "utf8" || method == "noCrLf") return Encoding.UTF8;
            try { return Encoding.GetEncoding(method); }
            catch (ArgumentException) { return null; }
        }

        private static int SendStringCommand(lpt transport, string method, string command)
        {
            switch (method)
            {
                case "ascii": transport.sendcommand(command); break;
                case "utf8": transport.sendcommand_utf8(command); break;
                case "gb2312": transport.sendcommand_gb2312(command); break;
                case "big5": transport.sendcommand_big5(command); break;
                case "noCrLf": return transport.sendcommandNOCRLF(command);
                default: throw new ArgumentOutOfRangeException(nameof(method));
            }
            return 1;
        }

        private static byte[] Packet(string header, params byte[] payload) => Encoding.ASCII.GetBytes(header).Concat(payload).Concat(new byte[] { 13, 10 }).ToArray();

        private static void Send(lpt transport, bool unicode, int rotation, int x, int y)
        {
            if (unicode) transport.windowsfontunicode(x, y, 24, rotation, 3, 1, "Test font", "테스트");
            else transport.windowsfont(x, y, 24, rotation, 3, 1, "Test font", "Text");
        }

        private sealed class MemoryFileStream : FileStream
        {
            private readonly string path;
            private readonly MemoryStream capture = new MemoryStream();
            internal Action OnWrite = () => { };
            internal int WriteCalls;
            internal byte[] Bytes => capture.ToArray();

            internal MemoryFileStream() : this(Path.GetTempFileName()) { }
            private MemoryFileStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite) { this.path = path; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteCalls++;
                OnWrite();
                capture.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) capture.Dispose();
                base.Dispose(disposing);
                if (disposing) File.Delete(path);
            }
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
