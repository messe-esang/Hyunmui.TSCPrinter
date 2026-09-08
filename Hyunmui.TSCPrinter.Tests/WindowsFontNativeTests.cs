using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    [CollectionDefinition("Windows font GDI", DisableParallelization = true)]
    public class WindowsFontGdiCollection { }

    [Collection("Windows font GDI")]
    [Trait("Category", "WindowsGdi")]
    public class WindowsFontNativeTests
    {
        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr process, uint flags);

        private static byte[] Render(int rotation, bool unicode, int x = 200, int y = 200)
        {
            var result = new List<byte>();
            var request = new WindowsFontRequest
            {
                X = x, Y = y, Height = 24, Rotation = rotation, Style = 0,
                FaceName = "Malgun Gothic", Content = unicode ? "가나다 A123" : "Font ABC 123", Unicode = unicode
            };
            WindowsFontCommand.Send(request, new ethernet.EthernetFontGdi(), (data, offset, count) =>
            {
                result.AddRange(data.Skip(offset).Take(count));
                return count;
            });
            return result.ToArray();
        }

        private static byte[] Payload(byte[] packet)
        {
            var comma = -1;
            for (var index = 0; index < 5; index++) comma = Array.IndexOf(packet, (byte)',', comma + 1);
            Assert.True(comma > 0);
            var fields = Encoding.ASCII.GetString(packet, 0, comma).Split(',');
            Assert.Equal("1", fields[4]);
            var size = int.Parse(fields[2], CultureInfo.InvariantCulture) * int.Parse(fields[3], CultureInfo.InvariantCulture);
            Assert.Equal(comma + 1 + size + 2, packet.Length);
            Assert.Equal(new byte[] { 13, 10 }, packet.Skip(packet.Length - 2));
            return packet.Skip(comma + 1).Take(size).ToArray();
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(90, false)]
        [InlineData(180, false)]
        [InlineData(270, false)]
        [InlineData(0, true)]
        [InlineData(90, true)]
        [InlineData(180, true)]
        [InlineData(270, true)]
        public void Native_RenderedTextContainsInkAndBackgroundWithConsistentPacketSize(int rotation, bool unicode)
        {
            var data = Payload(Render(rotation, unicode));
            Assert.Contains(data, value => value != byte.MaxValue);
            Assert.Contains(data, value => value != 0);
        }

        [Fact]
        public void Native_ClippedThenNormalOutputIsStableAndGdiHandlesStayBounded()
        {
            for (var index = 0; index < 8; index++) Render(index % 4 * 90, true);
            using var process = Process.GetCurrentProcess();
            var before = GetGuiResources(process.Handle, 0);
            Assert.True(before > 0);
            var expected = Render(0, true);
            for (var index = 0; index < 32; index++)
            {
                Payload(Render(index % 4 * 90, true));
                Render(0, false, -9, -2);
                Assert.Equal(expected, Render(0, true));
            }
            var after = GetGuiResources(process.Handle, 0);
            Assert.InRange((long)after - before, -8, 8);
        }

        [Fact]
        public async Task Native_OverlappingRenderersDoNotMixTheirBitmaps()
        {
            var expectedFirst = Render(0, false);
            var expectedSecond = Render(90, true);
            await Task.WhenAll(
                Task.Run(() => { for (var index = 0; index < 12; index++) Assert.Equal(expectedFirst, Render(0, false)); }),
                Task.Run(() => { for (var index = 0; index < 12; index++) Assert.Equal(expectedSecond, Render(90, true)); }));
        }
    }
}
