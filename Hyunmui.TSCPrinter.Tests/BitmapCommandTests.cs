using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class BitmapCommandTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void FileIsReleasedAfterSuccessOrAnyWriteFailure(int failureAt)
        {
            using var file = new BitmapTransportTests.TemporaryFile();
            using (var source = BitmapTransportTests.CreateSource()) source.Save(file.Path, ImageFormat.Png);
            var calls = new List<object>();
            var failure = new IOException("Simulated transport failure");
            void Write(object value)
            {
                calls.Add(value);
                if (calls.Count - 1 == failureAt) throw failure;
            }

            var result = Record.Exception(() => BitmapCommand.SendFile(3, 5, file.Path, Write, Write));

            if (failureAt < 0)
            {
                Assert.Null(result);
                Assert.Equal("BITMAP 3,5,2,2, 0,", calls[0]);
                Assert.Equal(new byte[] { 0x53, 0x7f, 0xff, 0xff }, Assert.IsType<byte[]>(calls[1]));
                Assert.Equal(new byte[] { 13, 10 }, Assert.IsType<byte[]>(calls[2]));
            }
            else
            {
                Assert.Same(failure, result);
                Assert.Equal(failureAt + 1, calls.Count);
            }

            using var exclusive = new FileStream(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void TransportFailureDoesNotDisposeOrChangeTheCallerBitmap(int failureAt)
        {
            using var source = BitmapTransportTests.CreateSource();
            var calls = 0;
            var failure = new IOException("Simulated transport failure");
            void Write(object value)
            {
                if (calls++ == failureAt) throw failure;
            }

            var result = Record.Exception(() => BitmapCommand.Send(3, 5, source, Write, Write));

            Assert.Same(failure, result);
            Assert.Equal(Color.Red.ToArgb(), source.GetPixel(2, 0).ToArgb());
            source.SetPixel(2, 0, Color.Blue);
            Assert.Equal(Color.Blue.ToArgb(), source.GetPixel(2, 0).ToArgb());
        }

        [Theory]
        [InlineData(1, new byte[] { 0x7f })]
        [InlineData(8, new byte[] { 0x00 })]
        [InlineData(9, new byte[] { 0x00, 0x7f })]
        public void BlackPixelsKeepMostSignificantBitOrderAndWhiteRowPadding(int width, byte[] expected)
        {
            using var source = new Bitmap(width, 1);
            using (var graphics = Graphics.FromImage(source)) graphics.Clear(Color.Black);
            var headers = new List<string>();
            var data = new List<byte[]>();

            BitmapCommand.Send(0, 0, source, headers.Add, data.Add);

            Assert.Single(headers);
            Assert.Equal(2, data.Count);
            Assert.Equal(expected, data[0]);
            Assert.Equal(new byte[] { 13, 10 }, data[1]);
        }

        [Fact]
        public void InvalidImageDoesNotSendPartialPrinterCommandsOrHoldTheFile()
        {
            using var file = new BitmapTransportTests.TemporaryFile();
            File.WriteAllText(file.Path, "This is not an image.");
            var calls = new List<object>();

            var error = Record.Exception(() => BitmapCommand.SendFile(0, 0, file.Path, calls.Add, calls.Add));

            Assert.IsType<ArgumentException>(error);
            Assert.Empty(calls);
            using var exclusive = new FileStream(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }
    }
}
