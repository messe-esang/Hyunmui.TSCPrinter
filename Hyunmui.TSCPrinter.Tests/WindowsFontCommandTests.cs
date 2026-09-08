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
    public class WindowsFontCommandTests
    {
        private static WindowsFontRequest Request(int rotation = 0) => new WindowsFontRequest
        {
            X = 40,
            Y = 50,
            Height = 20,
            Rotation = rotation,
            Style = 2,
            FaceName = "Test font",
            Content = "Text"
        };

        private static byte[] Canvas(byte first = 0x10)
        {
            var result = new byte[WindowsFontGeometry.BufferLength];
            for (var row = 0; row < 32; row++)
                for (var column = 0; column < 8; column++)
                    result[row * WindowsFontGeometry.Stride + column] = (byte)(first + row * 8 + column);
            return result;
        }

        private static void AssertPacket(byte[] packet, string header, params byte[] payload)
        {
            Assert.Equal(Encoding.ASCII.GetBytes(header).Concat(payload).Concat(new byte[] { 13, 10 }), packet);
        }

        [Theory]
        [InlineData(0, 40, 50, 0, 0, 2, 2)]
        [InlineData(90, 40, 34, 0, 16, 1, 16)]
        [InlineData(180, 24, 48, 16, 2, 2, 2)]
        [InlineData(270, 38, 50, 2, 0, 1, 16)]
        public void RightAngleGeometryKeepsCoordinatesAndRowOrder(int rotation, int x, int y, int drawX, int drawY, int rowBytes, int rows)
        {
            var geometry = WindowsFontGeometry.Create(Request(rotation), new ethernet.SIZE { cx = 16, cy = 2 });
            Assert.Equal(drawX, geometry.DrawX);
            Assert.Equal(drawY, geometry.DrawY);
            var expected = Enumerable.Range(0, rows).SelectMany(row => Enumerable.Range(0, rowBytes).Select(column => (byte)(0x10 + row * 8 + column))).ToArray();
            AssertPacket(geometry.CreatePacket(Canvas()), $"BITMAP {x},{y},{rowBytes},{rows},1,", expected);
        }

        [Fact]
        public void NegativePositionCropsRowOffsetAndLengthWithoutLeakingIntoNextCall()
        {
            var request = Request();
            request.X = -1;
            request.Y = -1;
            var clipped = WindowsFontGeometry.Create(request, new ethernet.SIZE { cx = 24, cy = 3 });
            AssertPacket(clipped.CreatePacket(Canvas()), "BITMAP 0,0,2,2,1,", 0x19, 0x1a, 0x21, 0x22);
            var normal = WindowsFontGeometry.Create(Request(), new ethernet.SIZE { cx = 16, cy = 2 });
            AssertPacket(normal.CreatePacket(Canvas(0x40)), "BITMAP 40,50,2,2,1,", 0x40, 0x41, 0x48, 0x49);
            AssertPacket(clipped.CreatePacket(Canvas()), "BITMAP 0,0,2,2,1,", 0x19, 0x1a, 0x21, 0x22);
        }

        [Theory]
        [InlineData(-24, 0)]
        [InlineData(0, -3)]
        [InlineData(int.MinValue, int.MinValue)]
        public void FullyClippedOutputProducesNoPacket(int x, int y)
        {
            var request = Request();
            request.X = x;
            request.Y = y;
            Assert.Empty(WindowsFontGeometry.Create(request, new ethernet.SIZE { cx = 24, cy = 3 }).CreatePacket(Canvas()));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 4)]
        [InlineData(16, 0)]
        public void ZeroMeasuredDimensionsDoNotAllocateBitmapOrWrite(int width, int height)
        {
            var native = new FakeGdi { Size = new ethernet.SIZE { cx = width, cy = height } };
            WindowsFontCommand.Send(Request(), native, (_, _, _) => throw new Exception("No writes expected"));
            Assert.DoesNotContain("bitmap", native.Calls);
            Assert.Empty(native.Owned);
        }

        [Theory]
        [InlineData(2401, 1, 0)]
        [InlineData(1, 2401, 0)]
        [InlineData(2401, 1, 90)]
        [InlineData(-1, 2, 0)]
        public void InvalidExtentFailsBeforeAllocationAndTransport(int width, int height, int rotation)
        {
            var native = new FakeGdi { Size = new ethernet.SIZE { cx = width, cy = height } };
            Assert.Throws<ArgumentOutOfRangeException>(() => WindowsFontCommand.Send(Request(rotation), native, (_, _, _) => throw new Exception("No writes expected")));
            Assert.DoesNotContain("bitmap", native.Calls);
            Assert.Empty(native.Owned);
        }

        [Fact]
        public void EmptyContentDoesNotAcquireNativeResources()
        {
            var request = Request();
            request.Content = "";
            var native = new FakeGdi();
            WindowsFontCommand.Send(request, native, (_, _, _) => throw new Exception("No writes expected"));
            Assert.Empty(native.Calls);
            request.Content = null;
            Assert.Throws<ArgumentNullException>(() => WindowsFontCommand.Send(request, native, (_, _, _) => 0));
            Assert.Empty(native.Calls);
        }

        [Fact]
        public void FullCanvasBoundaryAndInvalidBufferAreChecked()
        {
            var geometry = WindowsFontGeometry.Create(Request(), new ethernet.SIZE { cx = 2400, cy = 2400 });
            var packet = geometry.CreatePacket(Canvas());
            Assert.Equal(Encoding.ASCII.GetByteCount("BITMAP 40,50,300,2400,1,") + 720000 + 2, packet.Length);
            Assert.Throws<ArgumentException>(() => geometry.CreatePacket(new byte[3]));
        }

        [Theory]
        [InlineData(false, 0, 400)]
        [InlineData(true, 3, 700)]
        public void RendererKeepsLegacyFontAndUnicodeChoices(bool unicode, int style, int weight)
        {
            var request = Request(45);
            request.Unicode = unicode;
            request.Style = style;
            var native = new FakeGdi();
            WindowsFontCommand.Send(request, native, (_, _, count) => count);
            Assert.Equal(unicode, native.Unicode);
            Assert.Equal(450, native.Font.lfEscapement);
            Assert.Equal(weight, native.Font.lfWeight);
            Assert.Equal(0, native.Font.lfUnderline);
            Assert.Equal(0, native.Font.lfItalic);
            Assert.Equal("Test font", native.Font.lfFaceName);
            Assert.Equal(20, native.Font.lfHeight);
            Assert.Equal((2, 2), native.DrawOrigin);
            Assert.Empty(native.Owned);
        }

        [Theory]
        [InlineData("screen")]
        [InlineData("memory")]
        [InlineData("font")]
        [InlineData("selectFont")]
        [InlineData("measure")]
        [InlineData("bitmap")]
        [InlineData("selectBitmap")]
        [InlineData("colors")]
        [InlineData("clear")]
        [InlineData("draw")]
        [InlineData("read")]
        public void EveryNativeFailureReleasesAllAcquiredHandlesOnTheCallingThread(string failure)
        {
            var native = new FakeGdi { Failure = failure };
            Assert.Throws<InvalidOperationException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => throw new Exception("No writes expected")));
            Assert.Empty(native.Owned);
            Assert.Empty(native.Selected);
            Assert.Equal(native.Calls.Count, native.ThreadIds.Count(id => id == Environment.CurrentManagedThreadId));
        }

        [Theory]
        [InlineData("restoreFont")]
        [InlineData("restoreBitmap")]
        public void FailedRestoreDeletesDcBeforeItsStillSelectedObjects(string failure)
        {
            var native = new FakeGdi { Failure = failure };
            Assert.Throws<InvalidOperationException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => throw new Exception("No writes expected")));
            Assert.Empty(native.Owned);
            Assert.True(native.Calls.IndexOf("deleteDc") < native.Calls.IndexOf("deleteFont"));
        }

        [Theory]
        [InlineData("deleteFont")]
        [InlineData("deleteBitmap")]
        [InlineData("deleteDc")]
        [InlineData("releaseScreen")]
        public void FailedCleanupStillAttemptsRemainingReleasesAndPreventsTransport(string failure)
        {
            var native = new FakeGdi { Failure = failure };
            Assert.Throws<InvalidOperationException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => throw new Exception("No writes expected")));
            Assert.Contains("deleteFont", native.Calls);
            Assert.Contains("deleteBitmap", native.Calls);
            Assert.Contains("deleteDc", native.Calls);
            Assert.Contains("releaseScreen", native.Calls);
        }

        [Fact]
        public void CleanupFailureDoesNotReplaceTheOriginalRenderFailure()
        {
            var native = new FakeGdi { Failure = "draw", CleanupFailure = "releaseScreen" };
            var error = Assert.Throws<InvalidOperationException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => throw new Exception("No writes expected")));
            Assert.Contains("TextOut", error.Message);
            Assert.Contains("deleteFont", native.Calls);
            Assert.Contains("deleteBitmap", native.Calls);
            Assert.Contains("deleteDc", native.Calls);
            Assert.Contains("releaseScreen", native.Calls);
        }

        [Fact]
        public void PartialWritesCompleteOnePacketAfterGdiCleanup()
        {
            var native = new FakeGdi();
            var written = new List<byte>();
            WindowsFontCommand.Send(Request(), native, (data, offset, count) =>
            {
                Assert.Empty(native.Owned);
                var size = Math.Min(3, count);
                written.AddRange(data.Skip(offset).Take(size));
                return size;
            });
            AssertPacket(written.ToArray(), "BITMAP 40,50,2,2,1,", 0x10, 0x11, 0x18, 0x19);
            Assert.True(native.Calls.IndexOf("restoreFont") < native.Calls.IndexOf("deleteFont"));
            Assert.True(native.Calls.IndexOf("restoreBitmap") < native.Calls.IndexOf("deleteBitmap"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void InvalidTransportProgressFailsWithoutLeakingHandles(int count)
        {
            var native = new FakeGdi();
            Assert.Throws<IOException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => count));
            Assert.Empty(native.Owned);
        }

        [Fact]
        public void TransportExceptionIsPreservedAfterCleanup()
        {
            var native = new FakeGdi();
            var failure = new IOException("transport");
            Assert.Same(failure, Assert.Throws<IOException>(() => WindowsFontCommand.Send(Request(), native, (_, _, _) => throw failure)));
            Assert.Empty(native.Owned);
        }

        [Fact]
        public async Task ConcurrentRenderersKeepTheirOwnBuffersAndPackets()
        {
            using var barrier = new Barrier(2);
            async Task<byte[]> Render(byte seed, bool unicode)
            {
                return await Task.Run(() =>
                {
                    var native = new FakeGdi { Buffer = Canvas(seed), BeforeRead = () => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(5))) };
                    var request = Request();
                    request.Unicode = unicode;
                    var result = new List<byte>();
                    WindowsFontCommand.Send(request, native, (data, offset, count) => { result.AddRange(data.Skip(offset).Take(count)); return count; });
                    Assert.Empty(native.Owned);
                    return result.ToArray();
                });
            }
            var packets = await Task.WhenAll(Render(0x10, false), Render(0x40, true));
            AssertPacket(packets[0], "BITMAP 40,50,2,2,1,", 0x10, 0x11, 0x18, 0x19);
            AssertPacket(packets[1], "BITMAP 40,50,2,2,1,", 0x40, 0x41, 0x48, 0x49);
        }

        private sealed class FakeGdi : IWindowsFontGdi
        {
            internal string Failure = "";
            internal string CleanupFailure = "";
            internal ethernet.SIZE Size = new ethernet.SIZE { cx = 16, cy = 2 };
            internal byte[] Buffer = Canvas();
            internal Action BeforeRead = () => { };
            internal readonly List<string> Calls = new List<string>();
            internal readonly List<int> ThreadIds = new List<int>();
            internal readonly HashSet<int> Owned = new HashSet<int>();
            internal readonly HashSet<int> Selected = new HashSet<int>();
            internal ethernet.LOGFONT Font = new ethernet.LOGFONT();
            internal bool Unicode;
            internal (int, int) DrawOrigin;

            private bool Step(string operation)
            {
                Calls.Add(operation);
                ThreadIds.Add(Environment.CurrentManagedThreadId);
                return Failure != operation && CleanupFailure != operation;
            }
            private IntPtr Acquire(string operation, int handle)
            {
                if (!Step(operation)) return IntPtr.Zero;
                Assert.True(Owned.Add(handle));
                return new IntPtr(handle);
            }
            public IntPtr GetScreenDc() => Acquire("screen", 1);
            public IntPtr CreateMemoryDc(IntPtr screenDc) { Assert.Equal(new IntPtr(1), screenDc); return Acquire("memory", 2); }
            public IntPtr CreateFont(ethernet.LOGFONT font) { Font = font; return Acquire("font", 3); }
            public IntPtr CreateBitmap() => Acquire("bitmap", 4);
            public IntPtr SelectObject(IntPtr dc, IntPtr value)
            {
                Assert.Equal(new IntPtr(2), dc);
                var id = value.ToInt32();
                var name = id == 3 ? "selectFont" : id == 4 ? "selectBitmap" : id == 11 ? "restoreFont" : "restoreBitmap";
                if (!Step(name)) return new IntPtr(-1);
                if (id == 3 || id == 4) { Selected.Add(id); return new IntPtr(id == 3 ? 11 : 12); }
                Selected.Remove(id == 11 ? 3 : 4);
                return new IntPtr(id == 11 ? 3 : 4);
            }
            public bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size) { size = Size; Unicode = request.Unicode; return Step("measure"); }
            public bool SetColors(IntPtr dc) => Step("colors");
            public bool Clear(IntPtr dc) => Step("clear");
            public bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y) { DrawOrigin = (x, y); Assert.Equal(Unicode, request.Unicode); return Step("draw"); }
            public int ReadBitmap(IntPtr bitmap, byte[] buffer)
            {
                if (!Step("read")) return buffer.Length - 1;
                BeforeRead?.Invoke();
                System.Buffer.BlockCopy(Buffer, 0, buffer, 0, Buffer.Length);
                return Buffer.Length;
            }
            public bool DeleteObject(IntPtr value)
            {
                var id = value.ToInt32();
                Assert.DoesNotContain(id, Selected);
                if (!Step(id == 3 ? "deleteFont" : "deleteBitmap")) return false;
                Assert.True(Owned.Remove(id));
                return true;
            }
            public bool DeleteDc(IntPtr dc)
            {
                if (!Step("deleteDc")) return false;
                Selected.Clear();
                Assert.True(Owned.Remove(2));
                return true;
            }
            public bool ReleaseScreenDc(IntPtr dc)
            {
                if (!Step("releaseScreen")) return false;
                Assert.Equal(new IntPtr(1), dc);
                Assert.True(Owned.Remove(1));
                return true;
            }
        }
    }
}
