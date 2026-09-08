using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class ComportQueryTests
    {
        [Theory]
        [InlineData("full", "\u001b!S")]
        [InlineData("codepage", "~!I\n")]
        [InlineData("name", "~!T\n")]
        [InlineData("mileage", "~!@\n")]
        [InlineData("memory", "~!A\n")]
        [InlineData("file", "~!F\n")]
        [InlineData("serial", "OUT _SERIAL$\r\n\n")]
        public void PublicQueriesPreserveRequestBytesNewlineAndDelay(string method, string expected)
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(new byte[] { 65, 0x80, 0, 66 });
            Assert.Equal("A?\0B", Invoke(fixture.Printer, method));
            Assert.Equal(Encoding.ASCII.GetBytes(expected), fixture.Port.Written);
            Assert.Equal(new[] { 300 }, fixture.Delays);
            Assert.Equal(1, fixture.Captures);
            Assert.Equal(2000, fixture.Port.ReadTimeout);
        }

        [Fact]
        public void PublicStatusPreservesRequestAndFirstByte()
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(new byte[] { 0x81, 42 });
            Assert.Equal(0x81, fixture.Printer.printerstatus());
            Assert.Equal(new byte[] { 27, 33, 63 }, fixture.Port.Written);
            Assert.Equal(new[] { 300 }, fixture.Delays);
        }

        [Fact]
        public void PublicStatusPropagatesWriteTimeoutButReturnsZeroForReadTimeout()
        {
            var fixture = new Fixture();
            var writeError = new TimeoutException("write failed");
            fixture.Port.OnWrite = () => throw writeError;
            Assert.Same(writeError, Assert.Throws<TimeoutException>(() => fixture.Printer.printerstatus()));
            Assert.Equal(0, fixture.Port.Reads);
            Assert.Empty(fixture.Delays);
            fixture.Port.OnWrite = null;
            Assert.Equal(0, fixture.Printer.printerstatus());
            Assert.Equal(1, fixture.Port.Reads);
        }

        [Fact]
        public void FramedQueryPreservesCommandsAndByteCharactersAcrossChunks()
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(new byte[] { 0x80, 0, 65 });
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("ENDLI"));
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("NE\r\nignored"));
            Assert.Equal("\u0080\0A", fixture.Query.QueryFramed("TEST\r\n"));
            Assert.Equal(Encoding.ASCII.GetBytes("TEST\r\n\r\nOUT \"ENDLINE\"\r\n"), fixture.Port.Written);
            Assert.Equal(new[] { 300 }, fixture.Delays);
            Assert.Equal(3, fixture.Port.Reads);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void EndlineIsRecognizedAtEveryChunkBoundary(int split)
        {
            var fixture = new Fixture();
            const string marker = "ENDLINE\r\n";
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("DATA" + marker.Substring(0, split)));
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes(marker.Substring(split)));
            Assert.Equal("DATA", fixture.Query.QueryFramed("QUERY"));
        }

        [Fact]
        public void FullBufferAndOverlappingMarkerPrefixesDoNotCorruptResult()
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(Enumerable.Repeat((byte)'A', 1024).ToArray());
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("ENDLINENDLINE\r\n"));
            Assert.Equal(new string('A', 1024) + "ENDLIN", fixture.Query.QueryFramed("QUERY"));
        }

        [Fact]
        public void RepeatedQueriesHaveIndependentResultsAndDiscardConsumedSuffix()
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("ONEENDLINE\r\nCONSUMED"));
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("TWOENDLINE\r\n"));
            Assert.Equal("ONE", fixture.Query.QueryFramed("FIRST"));
            Assert.Equal("TWO", fixture.Query.QueryFramed("SECOND"));
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("THREE"));
            Assert.Equal("THREE", fixture.Printer.printername());
        }

        [Theory]
        [InlineData("stream")]
        [InlineData("framed")]
        [InlineData("ack")]
        public void LegacyVoidReadersConsumeRealBufferWithoutCommandsOrDelay(string method)
        {
            var fixture = new Fixture();
            fixture.Port.Chunks.Enqueue(method == "ack" ? new byte[] { 255, 6, 42 }
                : Encoding.ASCII.GetBytes(method == "framed" ? "DATAENDLINE\r\nTAIL" : "DATA"));
            fixture.ReadLegacy(method);
            Assert.Equal(1, fixture.Port.Reads);
            Assert.Equal(method == "framed" ? 1024 : 256, Assert.Single(fixture.Port.ReadCapacities));
            Assert.Empty(fixture.Port.Written);
            Assert.Empty(fixture.Delays);
        }

        [Fact]
        public void QueryTimeoutIsReportedAndLegacyTimeoutContractsArePreserved()
        {
            var fixture = new Fixture();
            Assert.Throws<TimeoutException>(() => fixture.Printer.printername());
            Assert.Throws<TimeoutException>(() => fixture.Query.QueryFramed("QUERY"));
            Assert.Equal(0, fixture.Printer.printerstatus());
            fixture.ReadLegacy("stream");
            fixture.ReadLegacy("framed");
            fixture.ReadLegacy("ack");
            Assert.Equal(2000, fixture.Port.ReadTimeout);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TotalDeadlineBoundsRepeatedChunksAndZeroReads(bool zeroReads)
        {
            var fixture = new Fixture();
            fixture.Port.OnRead = buffer =>
            {
                fixture.Now += 700;
                buffer[0] = (byte)'X';
                return zeroReads ? 0 : 1;
            };
            Assert.Throws<TimeoutException>(() => fixture.Query.QueryFramed("QUERY"));
            Assert.Equal(3, fixture.Port.Reads);
            Assert.Equal(new[] { 2000, 1300, 600, 2000 }, fixture.Port.TimeoutAssignments);
        }

        [Fact]
        public void ExplicitInfiniteTimeoutRemainsInfiniteAcrossChunks()
        {
            var fixture = new Fixture();
            fixture.Port.Timeout = SerialPort.InfiniteTimeout;
            fixture.Port.OnRead = buffer =>
            {
                fixture.Now += 100000;
                var bytes = Encoding.ASCII.GetBytes(fixture.Port.Reads == 1 ? "A" : "ENDLINE\r\n");
                bytes.CopyTo(buffer, 0);
                return bytes.Length;
            };
            Assert.Equal("A", fixture.Query.QueryFramed("QUERY"));
            Assert.Empty(fixture.Port.TimeoutAssignments);
        }

        [Fact]
        public void CleanupCannotMaskPrimaryFailureAndRestoredSessionCanBeUsedAgain()
        {
            var fixture = new Fixture();
            var primary = new IOException("read failed");
            fixture.Port.OnRead = _ => throw primary;
            fixture.Port.OnTimeoutSet = value => { if (fixture.Port.Reads > 0) throw new InvalidOperationException("closed"); };
            Assert.Same(primary, Assert.Throws<IOException>(() => fixture.Printer.printername()));
            fixture.Port.OnRead = null;
            fixture.Port.OnTimeoutSet = null;
            fixture.Port.Chunks.Enqueue(Encoding.ASCII.GetBytes("OK"));
            Assert.Equal("OK", fixture.Printer.printername());
        }

        [Theory]
        [InlineData("stream")]
        [InlineData("framed")]
        [InlineData("ack")]
        public void LegacyReadersDoNotSwallowNonTimeoutErrors(string method)
        {
            var fixture = new Fixture();
            fixture.Port.OnRead = _ => throw new IOException("read failed");
            Assert.Throws<IOException>(() => fixture.ReadLegacy(method));
            Assert.Equal(2000, fixture.Port.ReadTimeout);
        }

        [Fact]
        public void CapturedPortRemainsSelectedWhenDefaultPortChangesDuringRequest()
        {
            var fixture = new Fixture();
            var original = fixture.Port;
            original.Chunks.Enqueue(Encoding.ASCII.GetBytes("ORIGINAL"));
            original.OnWrite = () => fixture.Port = new FakePort();
            Assert.Equal("ORIGINAL", fixture.Printer.printerfullstatus());
            Assert.Equal(1, original.Reads);
            Assert.Equal(0, fixture.Port.Reads);
            Assert.Equal(1, fixture.Captures);
        }

        [Fact]
        public async Task QueriesOnSamePortCannotInterleaveRequestsAndReads()
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var secondStarted = new ManualResetEventSlim();
            var port = new FakePort();
            port.OnRead = buffer =>
            {
                if (port.Reads == 1) { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); }
                buffer[0] = (byte)'A';
                return 1;
            };
            var query = new SerialQuery(() => port, _ => { }, () => 0);
            var first = Task.Run(() => query.QueryLine("FIRST"));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            var second = Task.Run(() => { secondStarted.Set(); return query.QueryLine("SECOND"); });
            try
            {
                Assert.True(secondStarted.Wait(TimeSpan.FromSeconds(10)));
                Assert.False(second.IsCompleted);
                Assert.Equal(new[] { "FIRST" }, port.Lines);
            }
            finally { release.Set(); }
            Assert.Equal(new[] { "A", "A" }, await Task.WhenAll(first, second));
            Assert.Equal(new[] { "FIRST", "SECOND" }, port.Lines);
        }

        [Fact]
        public void ProductionAdaptersShareGateBySerialPortIdentityWithoutOpeningPort()
        {
            using var first = new SerialPort();
            using var second = new SerialPort();
            Assert.Same(new SerialQueryPort(first).SyncRoot, new SerialQueryPort(first).SyncRoot);
            Assert.NotSame(new SerialQueryPort(first).SyncRoot, new SerialQueryPort(second).SyncRoot);
            Assert.False(first.IsOpen);
            Assert.False(second.IsOpen);
        }

        private static string Invoke(comport printer, string method) => method switch
        {
            "full" => printer.printerfullstatus(),
            "codepage" => printer.printercodepage(),
            "name" => printer.printername(),
            "mileage" => printer.printermileage(),
            "memory" => printer.printermemory(),
            "file" => printer.printerfile(),
            "serial" => printer.printerserial(),
            _ => throw new ArgumentException(method)
        };


        private sealed class Fixture
        {
            internal readonly SerialQuery Query;
            internal comport Printer => new comport(Query);
            internal FakePort Port = new FakePort();
            internal long Now;
            internal int Captures;
            internal readonly List<int> Delays = new();
            internal Fixture()
            {
                Query = new SerialQuery(() => { Captures++; return Port; }, value => { Delays.Add(value); Now += value; }, () => Now);
            }
            internal void ReadLegacy(string method) => Query.ReadLegacy(method == "stream" ? null : method == "framed" ? "ENDLINE\r\n" : "\u0006");
        }

        private sealed class FakePort : ISerialQueryPort
        {
            public object SyncRoot { get; } = new object();
            internal int Timeout = 2000;
            internal int Reads;
            internal Func<byte[], int>? OnRead;
            internal Action<int>? OnTimeoutSet;
            internal Action? OnWrite;
            internal readonly Queue<byte[]> Chunks = new();
            internal readonly List<int> TimeoutAssignments = new();
            internal readonly List<int> ReadCapacities = new();
            internal readonly List<byte> Written = new();
            internal readonly List<string> Lines = new();
            public int ReadTimeout
            {
                get => Timeout;
                set { OnTimeoutSet?.Invoke(value); Timeout = value; TimeoutAssignments.Add(value); }
            }
            public int Read(byte[] buffer, int offset, int count)
            {
                Reads++;
                ReadCapacities.Add(count);
                if (OnRead != null) return OnRead(buffer);
                if (Chunks.Count == 0) throw new TimeoutException("No response.");
                var bytes = Chunks.Dequeue();
                Assert.True(bytes.Length <= count);
                bytes.CopyTo(buffer, offset);
                return bytes.Length;
            }
            public void Write(byte[] buffer, int offset, int count)
            {
                Written.AddRange(buffer.Skip(offset).Take(count));
                OnWrite?.Invoke();
            }
            public void WriteLine(string command)
            {
                Lines.Add(command);
                var bytes = Encoding.ASCII.GetBytes(command + "\n");
                Write(bytes, 0, bytes.Length);
            }
        }
    }
}
