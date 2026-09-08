using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TSCSDK
{
    internal interface ISerialQueryPort
    {
        object SyncRoot { get; }
        int ReadTimeout { get; set; }
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
        void WriteLine(string command);
    }

    internal sealed class SerialQueryPort : ISerialQueryPort
    {
        private static readonly ConditionalWeakTable<SerialPort, object> QueryLocks = new ConditionalWeakTable<SerialPort, object>();
        private readonly SerialPort port;
        internal SerialQueryPort(SerialPort port)
        {
            this.port = port;
            SyncRoot = QueryLocks.GetValue(port, _ => new object());
        }
        public object SyncRoot { get; }
        public int ReadTimeout { get => port.ReadTimeout; set => port.ReadTimeout = value; }
        public int Read(byte[] buffer, int offset, int count) => port.Read(buffer, offset, count);
        public void Write(byte[] buffer, int offset, int count) => port.Write(buffer, offset, count);
        public void WriteLine(string command) => port.WriteLine(command);
    }

    internal sealed class SerialQuery
    {
        private readonly Func<ISerialQueryPort> capturePort;
        private readonly Action<int> delay;
        private readonly Func<long> milliseconds;

        internal SerialQuery(Func<ISerialQueryPort> capturePort)
            : this(capturePort, Thread.Sleep, () => (long)(Stopwatch.GetTimestamp() * (1000d / Stopwatch.Frequency))) { }

        internal SerialQuery(Func<ISerialQueryPort> capturePort, Action<int> delay, Func<long> milliseconds)
        {
            this.capturePort = capturePort;
            this.delay = delay;
            this.milliseconds = milliseconds;
        }

        internal string QueryLine(string command) => Execute(port => port.WriteLine(command), ReadAscii, true);
        internal string QueryFullStatus() => Execute(port => Write(port, new byte[] { 27, 33, 83 }), ReadAscii, true);

        internal byte QueryStatus()
        {
            return Execute(port => Write(port, new byte[] { 27, 33, 63 }), ReadStatus, true);
        }

        internal string QueryFramed(string command)
        {
            return Execute(port =>
            {
                Write(port, Encoding.ASCII.GetBytes(command));
                Write(port, new byte[] { 13, 10 });
                Write(port, Encoding.Default.GetBytes("OUT \"ENDLINE\"\r\n"));
            }, read => ReadUntil(read, "ENDLINE\r\n", 1024), true);
        }

        internal void ReadLegacy(string terminator)
        {
            var bufferSize = terminator == "\u0006" ? 256 : 1024;
            try
            {
                Execute(port => { /* Legacy readers send no request. */ },
                    read => terminator == null ? ReadAscii(read) : ReadUntil(read, terminator, bufferSize), false);
            }
            catch (TimeoutException)
            {
                // These public void readers historically return when a read times out.
            }
        }

        private T Execute<T>(Action<ISerialQueryPort> request, Func<Func<byte[], int>, T> receive, bool waitForResponse)
        {
            var port = capturePort();
            lock (port.SyncRoot)
            {
                request(port);
                if (waitForResponse) delay(300);
                return Receive(port, receive);
            }
        }

        private T Receive<T>(ISerialQueryPort port, Func<Func<byte[], int>, T> receive)
        {
            var originalTimeout = port.ReadTimeout;
            var started = milliseconds();
            var timeoutChanged = false;
            Exception primaryError = null;
            try
            {
                return receive(buffer =>
                {
                    if (originalTimeout != SerialPort.InfiniteTimeout)
                    {
                        var remaining = originalTimeout - (milliseconds() - started);
                        if (remaining <= 0) throw new TimeoutException("The serial response did not complete before its read deadline.");
                        port.ReadTimeout = (int)remaining;
                        timeoutChanged = true;
                    }
                    var count = port.Read(buffer, 0, buffer.Length);
                    if (count < 0 || count > buffer.Length) throw new IOException("The serial reader returned an invalid byte count.");
                    if (originalTimeout != SerialPort.InfiniteTimeout && milliseconds() - started >= originalTimeout)
                        throw new TimeoutException("The serial response did not complete before its read deadline.");
                    return count;
                });
            }
            catch (Exception error)
            {
                primaryError = error;
                throw;
            }
            finally
            {
                if (timeoutChanged)
                {
                    try { port.ReadTimeout = originalTimeout; }
                    catch (Exception) when (primaryError != null)
                    {
                        // Preserve the primary read/conversion failure if the port closed during cleanup.
                    }
                }
            }
        }

        private static void Write(ISerialQueryPort port, byte[] bytes) => port.Write(bytes, 0, bytes.Length);
        private static string ReadAscii(Func<byte[], int> read) => Encoding.ASCII.GetString(ReadFirst(read));

        private static byte ReadStatus(Func<byte[], int> read)
        {
            try { return ReadFirst(read)[0]; }
            catch (TimeoutException) { return 0; }
        }

        private static byte[] ReadFirst(Func<byte[], int> read)
        {
            var buffer = new byte[256];
            int count;
            do { count = read(buffer); } while (count == 0);
            var result = new byte[count];
            Buffer.BlockCopy(buffer, 0, result, 0, count);
            return result;
        }

        private static string ReadUntil(Func<byte[], int> read, string terminator, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            var result = new StringBuilder();
            var prefixes = BuildPrefixes(terminator);
            var matched = 0;
            while (true)
            {
                var count = read(buffer);
                for (var index = 0; index < count; index++)
                {
                    var character = (char)buffer[index];
                    result.Append(character); // Preserve legacy byte-to-char mapping, including 0x80..0xff.
                    while (matched > 0 && character != terminator[matched]) matched = prefixes[matched - 1];
                    if (character == terminator[matched]) matched++;
                    if (matched == terminator.Length)
                    {
                        result.Length -= terminator.Length;
                        // Like the legacy readers, bytes already consumed after the terminator
                        // in this native read are discarded, not retained for another request.
                        return result.ToString();
                    }
                }
            }
        }

        private static int[] BuildPrefixes(string terminator)
        {
            var prefixes = new int[terminator.Length];
            var matched = 0;
            for (var index = 1; index < terminator.Length; index++)
            {
                while (matched > 0 && terminator[index] != terminator[matched]) matched = prefixes[matched - 1];
                if (terminator[index] == terminator[matched]) matched++;
                prefixes[index] = matched;
            }
            return prefixes;
        }
    }
}
