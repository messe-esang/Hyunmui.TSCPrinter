using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class BitmapTransportTests
    {
        [Fact]
        public void BorrowedBitmapKeepsPixelsAndLegacyWireBytes()
        {
            using var source = CreateSource();
            using var output = new TemporaryFile();
            using var stream = new FileStream(output.Path, FileMode.Create, FileAccess.ReadWrite);
            var transport = CreateFileTransport(stream);

            transport.sendpicture(3, 5, source);

            Assert.Equal(Color.Red.ToArgb(), source.GetPixel(2, 0).ToArgb());
            stream.Position = 0;
            using var captured = new MemoryStream();
            stream.CopyTo(captured);
            var expected = Encoding.UTF8.GetBytes("BITMAP 3,5,2,2, 0,")
                .Concat(new byte[] { 0x53, 0x7f, 0xff, 0xff, 13, 10, 13, 10, 13, 10 }).ToArray();
            Assert.Equal(expected, captured.ToArray());
        }

        internal static Bitmap CreateSource()
        {
            var source = new Bitmap(9, 2);
            using var graphics = Graphics.FromImage(source);
            graphics.Clear(Color.White);
            var colors = new[] { Color.Black, Color.White, Color.Red, Color.Lime, Color.Blue,
                Color.FromArgb(127, 127, 127), Color.FromArgb(129, 129, 129), Color.White, Color.Black };
            for (var x = 0; x < colors.Length; x++) source.SetPixel(x, 0, colors[x]);
            return source;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void FileOverloadsReleaseTheImageWhenTheStreamSucceedsOrFails(bool photo, bool failWrite)
        {
            using var image = new TemporaryFile();
            using (var source = CreateSource()) source.Save(image.Path, ImageFormat.Png);
            using var output = new TemporaryFile();
            using var stream = new FileStream(output.Path, FileMode.Create, FileAccess.ReadWrite);
            var transport = CreateFileTransport(stream);
            if (failWrite) stream.Dispose();

            var error = Record.Exception(() =>
            {
                if (photo) transport.printphoto(3, 5, image.Path);
                else transport.sendpicture(3, 5, image.Path);
            });

            if (failWrite) Assert.IsType<ObjectDisposedException>(error);
            else
            {
                Assert.Null(error);
                Assert.Equal(28, stream.Length);
            }
            using var exclusive = new FileStream(image.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void UnopenedSerialAndTcpTransportsReleaseTheirImageFile(bool serial, bool photo)
        {
            using var image = new TemporaryFile();
            using (var source = CreateSource()) source.Save(image.Path, ImageFormat.Png);
            // No connection is opened: these transports fail at their missing connection.
            Action send = serial
                ? () => { if (photo) new comport().printphoto(3, 5, image.Path); else new comport().sendpicture(3, 5, image.Path); }
            : () => { if (photo) new ethernet().printphoto(3, 5, image.Path); else new ethernet().sendpicture(3, 5, image.Path); };

            Assert.Throws<NullReferenceException>(send);

            using var exclusive = new FileStream(image.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnopenedSerialAndTcpTransportsKeepTheBorrowedBitmap(bool serial)
        {
            using var source = CreateSource();
            Action send = serial
                ? () => new comport().sendpicture(3, 5, source)
                : () => new ethernet().sendpicture(3, 5, source);

            Assert.Throws<NullReferenceException>(send);

            Assert.Equal(Color.Red.ToArgb(), source.GetPixel(2, 0).ToArgb());
        }

        internal static lpt CreateFileTransport(FileStream stream)
        {
            var transport = new lpt();
            // Replace only this instance's stream with an owned file; no printer is opened.
            typeof(lpt).GetField("lptstream", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(transport, stream);
            return transport;
        }

        internal sealed class TemporaryFile : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tsc-bitmap-" + Guid.NewGuid().ToString("N") + ".tmp");

            public void Dispose() => File.Delete(Path);
        }
    }
}
