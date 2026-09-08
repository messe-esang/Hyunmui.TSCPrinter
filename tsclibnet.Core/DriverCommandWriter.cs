using System;

namespace TSCSDK
{
    internal sealed class DriverCommandWriter
    {
        internal delegate bool PrinterWrite(IntPtr printer, byte[] bytes, int count, out int written);

        private readonly Func<int, IntPtr> getPrinter;
        private readonly Func<IntPtr, bool> startPage;
        private readonly PrinterWrite write;
        private readonly Func<string, byte[]> encodeAnsi;

        internal DriverCommandWriter(Func<int, IntPtr> getPrinter, Func<IntPtr, bool> startPage,
            PrinterWrite write, Func<string, byte[]> encodeAnsi)
        {
            this.getPrinter = getPrinter;
            this.startPage = startPage;
            this.write = write;
            this.encodeAnsi = encodeAnsi;
        }

        internal bool SendAnsi(int port, string command, bool appendNewLine)
        {
            return Send(port, encodeAnsi(command), appendNewLine);
        }

        internal bool Send(int port, byte[] command, bool appendNewLine)
        {
            // Own the payload for this call, including while a native write is in progress.
            var bytes = (byte[])command.Clone();
            var printer = getPrinter(port);
            if (!startPage(printer)) return false;
            return WriteAll(printer, bytes) && (!appendNewLine || WriteAll(printer, new byte[] { 13, 10 }));
        }

        private bool WriteAll(IntPtr printer, byte[] bytes)
        {
            var offset = 0;
            while (offset < bytes.Length)
            {
                var remaining = bytes.Length - offset;
                var pending = bytes;
                if (offset != 0)
                {
                    pending = new byte[remaining];
                    Buffer.BlockCopy(bytes, offset, pending, 0, remaining);
                }
                if (!write(printer, pending, remaining, out var written) || written <= 0 || written > remaining)
                    return false;
                offset += written;
            }
            return true;
        }
    }
}
