using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class DriverAnsiEncodingTests
    {
        [Theory]
        [InlineData(949, 0x400)]
        [InlineData(1252, 0x400)]
        [InlineData(65001, 0)]
        public void TwoPassConversionUsesExactCharacterAndByteCountsIncludingEmbeddedNull(uint codePage, uint expectedFlags)
        {
            var calls = 0;
            var reads = 0;
            var expected = new byte[] { 0x81, 0x40, 0, 65 };
            var sut = new DriverAnsiEncoding(() => { reads++; return codePage; },
                (page, flags, text, characters, bytes, capacity, defaultCharacter, usedDefault) =>
                {
                    calls++;
                    Assert.Equal(codePage, page);
                    Assert.Equal(expectedFlags, flags);
                    Assert.Equal("한\0A", text);
                    Assert.Equal(3, characters);
                    Assert.Equal(IntPtr.Zero, defaultCharacter);
                    Assert.Equal(IntPtr.Zero, usedDefault);
                    if (calls == 1) { Assert.Null(bytes); Assert.Equal(0, capacity); }
                    else { Assert.Equal(expected.Length, capacity); expected.CopyTo(bytes, 0); }
                    return expected.Length;
                }, () => throw new Exception("unexpected error lookup"));
            Assert.Equal(expected, sut.GetBytes("한\0A"));
            Assert.Equal(2, calls);
            Assert.Equal(1, reads);
        }

        [Fact]
        public void EmptyInputDoesNotCallNativeConversion()
        {
            var sut = new DriverAnsiEncoding(() => throw new Exception("code page"),
                (_, _, _, _, _, _, _, _) => throw new Exception("convert"), () => throw new Exception("error"));
            Assert.Empty(sut.GetBytes(""));
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(-1, 0, 1)]
        [InlineData(4, 0, 2)]
        [InlineData(4, -1, 2)]
        public void NativeConversionErrorsStopAtFailingPass(int measured, int converted, int expectedCalls)
        {
            var calls = 0;
            var sut = new DriverAnsiEncoding(() => 949,
                (_, _, _, _, _, _, _, _) => ++calls == 1 ? measured : converted, () => 87);
            var exception = Assert.Throws<Win32Exception>(() => sut.GetBytes("한글"));
            Assert.Equal(87, exception.NativeErrorCode);
            Assert.Equal(expectedCalls, calls);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public void InconsistentConversionCountCannotReturnUninitializedOrTruncatedBytes(int converted)
        {
            var calls = 0;
            var sut = new DriverAnsiEncoding(() => 949,
                (_, _, _, _, _, _, _, _) => ++calls == 1 ? 4 : converted, () => 0);
            Assert.Throws<InvalidOperationException>(() => sut.GetBytes("한글"));
        }
    }

    public class DriverAnsiNativeTests
    {
        [Theory]
        [InlineData("ASCII")]
        [InlineData("한글 café")]
        [InlineData("한\0SUFFIX")]
        [InlineData("\0")]
        [InlineData("")]
        public void WindowsAnsiConversionMatchesMarshalBytesWithoutCallingPrinter(string text)
        {
            var actual = new DriverAnsiEncoding().GetBytes(text);
            // Separate segments let us measure Marshal's NUL-terminated allocations while
            // retaining embedded NULs in the expected command. No printer/GDI API is used.
            var expected = text.Split('\0').SelectMany((part, index) =>
            {
                var pointer = Marshal.StringToCoTaskMemAnsi(part);
                try
                {
                    var count = 0;
                    while (Marshal.ReadByte(pointer, count) != 0) count++;
                    var bytes = new byte[count + (index == 0 ? 0 : 1)];
                    Marshal.Copy(pointer, bytes, index == 0 ? 0 : 1, count);
                    return bytes;
                }
                finally { Marshal.FreeCoTaskMem(pointer); }
            }).ToArray();
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
            if (text.Contains("SUFFIX")) Assert.Equal(Encoding.ASCII.GetBytes("SUFFIX"), actual.Skip(actual.Length - 6));
        }
    }
}
