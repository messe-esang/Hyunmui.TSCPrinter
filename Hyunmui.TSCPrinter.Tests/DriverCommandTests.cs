using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    [CollectionDefinition("Driver command handle selection", DisableParallelization = true)]
    public class DriverCommandHandleCollection { }

    [Collection("Driver command handle selection")]
    public class DriverCommandHandleTests
    {
        [Fact]
        public void ProductionSelectorUsesEachActualHandleAndRejectsInvalidPorts()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var fields = Enumerable.Range(0, 6).Select(port => typeof(driver).GetField(
                port == 0 ? "hPrinter" : "hPrinter" + port, flags)
                ?? throw new InvalidOperationException("Missing driver printer handle.")).ToArray();
            var saved = fields.Select(field => field.GetValue(null)).ToArray();
            var selector = typeof(driver).GetMethod("GetCommandPrinter", flags)
                ?? throw new InvalidOperationException("Missing production command selector.");
            try
            {
                for (var port = 0; port < fields.Length; port++)
                    fields[port].SetValue(null, new IntPtr(9100 + port));
                for (var port = 0; port < fields.Length; port++)
                    Assert.Equal(new IntPtr(9100 + port), selector.Invoke(null, new object[] { port }));
                Assert.NotEqual(selector.Invoke(null, new object[] { 1 }), selector.Invoke(null, new object[] { 2 }));
                foreach (var port in new[] { -1, 6 })
                {
                    var error = Assert.Throws<TargetInvocationException>(() => selector.Invoke(null, new object[] { port }));
                    Assert.IsType<ArgumentOutOfRangeException>(error.InnerException);
                }
            }
            finally
            {
                for (var port = 0; port < fields.Length; port++) fields[port].SetValue(null, saved[port]);
            }
        }
    }

    public class DriverCommandTests
    {
        [Theory]
        [InlineData("ansi", true)]
        [InlineData("utf8", true)]
        [InlineData("bytes", true)]
        [InlineData("binary", true)]
        [InlineData("multiAnsi", true)]
        [InlineData("multiBytes", true)]
        [InlineData("noAnsi", false)]
        [InlineData("noBytes", false)]
        public void PublicCommandsPreserveBytesAndLineEndings(string method, bool appendNewLine)
        {
            var printer = new MemoryPrinter { MaxWrite = 1 };
            var sut = CreateDriver(printer);
            Assert.True(Send(sut, method));
            var expected = method == "utf8" ? Encoding.UTF8.GetBytes("한\0A")
                : method.Contains("Ansi") || method == "ansi" ? new byte[] { 0x81, 0x40, 0, 65 }
                : new byte[] { 0, 255, 65 };
            Assert.Equal(appendNewLine ? expected.Concat(new byte[] { 13, 10 }) : expected, printer.Bytes);
            Assert.Single(printer.Started);
            Assert.All(printer.WrittenHandles, handle => Assert.Equal(printer.Started[0], handle));
            Assert.Equal(method.StartsWith("multi") ? 2 : 0, Assert.Single(printer.RequestedPorts));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void BothMultiPortOverloadsSelectRequestedPort(int port)
        {
            var printer = new MemoryPrinter();
            var sut = CreateDriver(printer);
            Assert.True(sut.sendcommand_mult(port, "한\0A"));
            Assert.True(sut.sendcommand_mult(port, new byte[] { 1 }));
            Assert.Equal(new[] { port, port }, printer.RequestedPorts);
            Assert.All(printer.Started.Concat(printer.WrittenHandles), value => Assert.Equal(new IntPtr(100 + port), value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(6)]
        public void InvalidPortDoesNotEncodeStartOrWrite(int port)
        {
            var writer = new DriverCommandWriter(_ => throw new Exception("handle"),
                _ => throw new Exception("start"), UnexpectedWrite, _ => throw new Exception("encode"));
            var sut = new driver(writer);
            Assert.False(sut.sendcommand_mult(port, "ignored"));
            Assert.False(sut.sendcommand_mult(port, new byte[] { 1 }));
        }

        [Theory]
        [InlineData("ansi")]
        [InlineData("utf8")]
        [InlineData("bytes")]
        [InlineData("binary")]
        [InlineData("multiAnsi")]
        [InlineData("multiBytes")]
        [InlineData("noAnsi")]
        [InlineData("noBytes")]
        public void StartFailureReturnsFailureWithoutWrites(string method)
        {
            var printer = new MemoryPrinter { StartSucceeds = false };
            Assert.False(Send(CreateDriver(printer), method));
            Assert.Single(printer.Started);
            Assert.Empty(printer.WrittenHandles);
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 0)]
        [InlineData(true, -1)]
        [InlineData(true, 100)]
        public void InvalidWriteStopsWithoutRetryOrTrailingNewLine(bool succeeds, int written)
        {
            var writes = 0;
            var writer = new DriverCommandWriter(_ => new IntPtr(10), _ => true,
                (IntPtr handle, byte[] bytes, int count, out int result) =>
                {
                    writes++;
                    Assert.Equal(new byte[] { 1, 2 }, bytes);
                    result = written;
                    return succeeds;
                }, Encoding.ASCII.GetBytes);
            Assert.False(new driver(writer).sendcommand(new byte[] { 1, 2 }));
            Assert.Equal(1, writes);
        }

        [Theory]
        [InlineData("ansi")]
        [InlineData("utf8")]
        [InlineData("bytes")]
        [InlineData("binary")]
        [InlineData("multiAnsi")]
        [InlineData("multiBytes")]
        [InlineData("noAnsi")]
        [InlineData("noBytes")]
        public void NativeWriteFailureIsReportedByPublicReturnShape(string method)
        {
            var printer = new MemoryPrinter { FailOnWrite = 1 };
            Assert.False(Send(CreateDriver(printer), method));
            Assert.Single(printer.WrittenHandles);
            Assert.Empty(printer.Bytes);
        }

        [Fact]
        public void FailureAfterPartialPrefixDoesNotReplayOrAppendNewLine()
        {
            var printer = new MemoryPrinter { MaxWrite = 1, FailOnWrite = 2 };
            Assert.False(CreateDriver(printer).sendcommand(new byte[] { 5, 6, 7 }));
            Assert.Equal(new byte[] { 5 }, printer.Bytes);
            Assert.Equal(2, printer.WrittenHandles.Count);
        }

        [Fact]
        public void NoCrLfStringUsesMinusOneForNativeFailure()
        {
            Assert.Equal(-1, CreateDriver(new MemoryPrinter { StartSucceeds = false }).sendcommandNOCRLF("한\0A"));
            Assert.Equal(-1, CreateDriver(new MemoryPrinter { FailOnWrite = 1 }).sendcommandNOCRLF("한\0A"));
        }

        [Fact]
        public void TrailingNewLineFailureIsNotReportedAsSuccess()
        {
            var printer = new MemoryPrinter { FailOnWrite = 2 };
            Assert.False(CreateDriver(printer).sendbinary(new byte[] { 5 }));
            Assert.Equal(new byte[] { 5 }, printer.Bytes);
        }

        [Fact]
        public void InputBytesAndHandleAreCapturedBeforeStartPageCanChangeThem()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var handle = new IntPtr(7);
            var printer = new MemoryPrinter { MaxWrite = 1 };
            var reads = 0;
            var writer = new DriverCommandWriter(_ => { reads++; return handle; }, captured =>
            {
                handle = new IntPtr(99);
                bytes[1] = 200;
                return printer.Start(captured);
            }, printer.Write, Encoding.ASCII.GetBytes);
            Assert.True(new driver(writer).sendcommandNOCRLF(bytes));
            Assert.Equal(new byte[] { 1, 2, 3 }, printer.Bytes);
            Assert.All(printer.WrittenHandles, value => Assert.Equal(new IntPtr(7), value));
            Assert.Equal(1, reads);
        }

        [Fact]
        public void EmptyCommandsStillStartOnePageAndOnlyAppendRequestedNewLine()
        {
            var printer = new MemoryPrinter();
            var sut = CreateDriver(printer);
            Assert.True(sut.sendcommand(Array.Empty<byte>()));
            Assert.True(sut.sendcommandNOCRLF(Array.Empty<byte>()));
            Assert.Equal(new byte[] { 13, 10 }, printer.Bytes);
            Assert.Equal(2, printer.Started.Count);
        }

        [Fact]
        public void StringArrayPreservesExistingExtraLineEndingsAndPageStarts()
        {
            var printer = new MemoryPrinter();
            CreateDriver(printer).sendcommand(new[] { "", "A", "" });
            Assert.Equal(Encoding.Default.GetBytes("A\r\n\r\n\r\n"), printer.Bytes);
            Assert.Equal(2, printer.Started.Count);
        }

        [Theory]
        [InlineData("gb2312")]
        [InlineData("big5")]
        public void ExplicitCodePageChoiceAndProviderErrorsRemainUnchanged(string name)
        {
            var printer = new MemoryPrinter();
            var sut = CreateDriver(printer);
            Func<bool> send = name == "big5" ? () => sut.sendcommand_big5("中文") : () => sut.sendcommand_gb2312("中文");
            Encoding encoding;
            try { encoding = Encoding.GetEncoding(name); }
            catch (ArgumentException)
            {
                Assert.Throws<ArgumentException>(() => { send(); });
                Assert.Empty(printer.Started);
                return;
            }
            Assert.True(send());
            Assert.Equal(encoding.GetBytes("中文").Concat(new byte[] { 13, 10 }), printer.Bytes);
        }

        [Fact]
        public async Task OverlappingDriverCallsKeepIndependentPayloads()
        {
            using var barrier = new Barrier(2);
            var first = new MemoryPrinter();
            var second = new MemoryPrinter();
            driver CreateConcurrent(MemoryPrinter printer) => new driver(new DriverCommandWriter(_ => new IntPtr(10), handle =>
            {
                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
                return printer.Start(handle);
            }, printer.Write, Encoding.ASCII.GetBytes));
            await Task.WhenAll(Task.Run(() => Assert.True(CreateConcurrent(first).sendcommand("FIRST"))),
                Task.Run(() => Assert.True(CreateConcurrent(second).sendcommand("SECOND"))));
            Assert.Equal(Encoding.ASCII.GetBytes("FIRST\r\n"), first.Bytes);
            Assert.Equal(Encoding.ASCII.GetBytes("SECOND\r\n"), second.Bytes);
        }

        private static driver CreateDriver(MemoryPrinter printer) => new driver(new DriverCommandWriter(printer.GetHandle,
            printer.Start, printer.Write, command =>
            {
                Assert.Equal("한\0A", command);
                return new byte[] { 0x81, 0x40, 0, 65 };
            }));

        private static bool Send(driver sut, string method) => method switch
        {
            "ansi" => sut.sendcommand("한\0A"),
            "utf8" => sut.sendcommand_utf8("한\0A"),
            "bytes" => sut.sendcommand(new byte[] { 0, 255, 65 }),
            "binary" => sut.sendbinary(new byte[] { 0, 255, 65 }),
            "multiAnsi" => sut.sendcommand_mult(2, "한\0A"),
            "multiBytes" => sut.sendcommand_mult(2, new byte[] { 0, 255, 65 }),
            "noAnsi" => sut.sendcommandNOCRLF("한\0A") == 1,
            "noBytes" => sut.sendcommandNOCRLF(new byte[] { 0, 255, 65 }),
            _ => throw new ArgumentException(method)
        };

        private static bool UnexpectedWrite(IntPtr handle, byte[] bytes, int count, out int written)
            => throw new Exception("write");

        private sealed class MemoryPrinter
        {
            internal readonly List<int> RequestedPorts = new();
            internal readonly List<IntPtr> Started = new();
            internal readonly List<IntPtr> WrittenHandles = new();
            internal readonly List<byte> Bytes = new();
            internal bool StartSucceeds = true;
            internal int MaxWrite = int.MaxValue;
            internal int FailOnWrite;
            internal IntPtr GetHandle(int port) { RequestedPorts.Add(port); return new IntPtr(100 + port); }
            internal bool Start(IntPtr handle) { Started.Add(handle); return StartSucceeds; }
            internal bool Write(IntPtr handle, byte[] bytes, int count, out int written)
            {
                WrittenHandles.Add(handle);
                Assert.Equal(bytes.Length, count);
                written = 0;
                if (WrittenHandles.Count == FailOnWrite) return false;
                written = Math.Min(count, MaxWrite);
                Bytes.AddRange(bytes.Take(written));
                return true;
            }
        }
    }
}
