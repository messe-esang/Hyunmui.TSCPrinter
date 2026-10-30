using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TSCSDK
{
    internal sealed class DriverAnsiEncoding
    {
        internal delegate int ConvertBytes(uint codePage, uint flags, string text, int characters,
            byte[] bytes, int capacity, IntPtr defaultCharacter, IntPtr usedDefaultCharacter);

        private const uint Utf8CodePage = 65001;
        private const uint NoBestFitCharacters = 0x400;
        private readonly Func<uint> getCodePage;
        private readonly ConvertBytes convert;
        private readonly Func<int> getError;

        internal DriverAnsiEncoding() : this(GetACP, WideCharToMultiByte, Marshal.GetLastWin32Error) { }

        internal DriverAnsiEncoding(Func<uint> getCodePage, ConvertBytes convert, Func<int> getError)
        {
            this.getCodePage = getCodePage;
            this.convert = convert;
            this.getError = getError;
        }

        internal byte[] GetBytes(string command)
        {
            var characters = command.Length;
            if (characters == 0) return Array.Empty<byte>();
            var codePage = getCodePage();
            // Marshal's ANSI conversion disables best-fit mapping. UTF-8 has no best-fit
            // mapping and Windows requires flags 0 (or WC_ERR_INVALID_CHARS) for that ACP.
            var flags = codePage == Utf8CodePage ? 0 : NoBestFitCharacters;
            var count = convert(codePage, flags, command, characters, null, 0, IntPtr.Zero, IntPtr.Zero);
            if (count <= 0) throw new Win32Exception(getError(), "Cannot measure the ANSI printer command.");
            var bytes = new byte[count];
            var written = convert(codePage, flags, command, characters, bytes, count, IntPtr.Zero, IntPtr.Zero);
            if (written <= 0) throw new Win32Exception(getError(), "Cannot encode the ANSI printer command.");
            if (written != count) throw new InvalidOperationException("ANSI conversion returned an inconsistent byte count.");
            return bytes;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetACP();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern int WideCharToMultiByte(uint codePage, uint flags, string text, int characters,
            [Out] byte[] bytes, int capacity, IntPtr defaultCharacter, IntPtr usedDefaultCharacter);
    }
}
