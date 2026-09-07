using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TSCSDK;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class EthernetConnectionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SeparateInstancesKeepConcurrentCommandsOnTheirOwnConnection(bool explicitDelay)
        {
            using var firstPeer = new LoopbackPrinter();
            using var secondPeer = new LoopbackPrinter();
            var first = new ethernet();
            var second = new ethernet();
            Open(first, firstPeer.Port, explicitDelay);
            using var firstClient = await firstPeer.Accept();
            Open(second, secondPeer.Port, explicitDelay);
            using var secondClient = await secondPeer.Accept();
            try
            {
                await Task.WhenAll(
                    Task.Run(() => { for (var i = 0; i < 20; i++) first.sendcommand("FIRST"); }),
                    Task.Run(() => { for (var i = 0; i < 20; i++) second.sendcommand("SECOND"); }));

                await AssertWire(firstClient, string.Concat(Enumerable.Repeat("FIRST\r\n", 20)));
                await AssertWire(secondClient, string.Concat(Enumerable.Repeat("SECOND\r\n", 20)));
            }
            finally
            {
                first.closeport();
                second.closeport();
            }
        }

        [Fact]
        public async Task ClosingOneInstanceDoesNotCloseAnotherInstance()
        {
            using var firstPeer = new LoopbackPrinter();
            using var secondPeer = new LoopbackPrinter();
            var first = new ethernet();
            var second = new ethernet();
            Open(first, firstPeer.Port);
            using var firstClient = await firstPeer.Accept();
            Open(second, secondPeer.Port);
            using var secondClient = await secondPeer.Accept();
            first.closeport();
            try
            {
                second.sendcommand("STILL OPEN");
                await AssertWire(secondClient, "STILL OPEN\r\n");
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                Assert.Equal(0, await firstClient.GetStream().ReadAsync(new byte[1], timeout.Token));
            }
            finally { second.closeport(); }
        }

        [Fact]
        public async Task ClosedInstanceReconnectsWithoutChangingAnotherInstance()
        {
            using var firstPeer = new LoopbackPrinter();
            using var secondPeer = new LoopbackPrinter();
            using var replacementPeer = new LoopbackPrinter();
            var first = new ethernet();
            var second = new ethernet();
            Open(first, firstPeer.Port);
            using var firstClient = await firstPeer.Accept();
            Open(second, secondPeer.Port);
            using var secondClient = await secondPeer.Accept();
            first.closeport();
            Open(first, replacementPeer.Port);
            using var replacementClient = await replacementPeer.Accept();
            try
            {
                first.sendcommandNOCRLF("RECONNECTED");
                second.sendcommand(new byte[] { 0, 127, 255 });
                await AssertWire(replacementClient, "RECONNECTED");
                await AssertWire(secondClient, new byte[] { 0, 127, 255, 13, 10 });
            }
            finally
            {
                first.closeport();
                second.closeport();
            }
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(5, false)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(5, true)]
        public async Task NumberedPortConnectsToItsRequestedEndpointWithoutADefaultConnection(int slot, bool explicitDelay)
        {
            using var peer = new LoopbackPrinter();
            var transport = new ethernet();
            var result = explicitDelay
                ? transport.openport_mult(slot, "127.0.0.1", peer.Port, 200)
                : transport.openport_mult(slot, "127.0.0.1", peer.Port);
            try
            {
                Assert.Equal(1, result);
                using var client = await peer.Accept();
                Assert.Equal(peer.Port, ((IPEndPoint)client.Client.LocalEndPoint!).Port);
            }
            finally { Assert.Equal(1, transport.closeport_mult(slot, 0)); }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task SameNumberedSlotBelongsToItsOwnInstance(int slot)
        {
            using var firstPeer = new LoopbackPrinter();
            using var secondPeer = new LoopbackPrinter();
            var first = new ethernet();
            var second = new ethernet();
            Assert.Equal(1, first.openport_mult(slot, "127.0.0.1", firstPeer.Port, 200));
            using var firstClient = await firstPeer.Accept();
            Assert.Equal(1, second.openport_mult(slot, "127.0.0.1", secondPeer.Port, 200));
            using var secondClient = await secondPeer.Accept();
            try
            {
                Assert.Equal(1, first.sendcommand_mult(slot, "FIRST"));
                Assert.Equal(1, second.sendcommand_mult(slot, new byte[] { 0, 255 }));
                await AssertWire(firstClient, "FIRST\r\n");
                await AssertWire(secondClient, new byte[] { 0, 255, 13, 10 });
                Assert.Equal(1, first.closeport_mult(slot, 0));
                Assert.Equal(1, second.sendcommand_mult(slot, "SECOND"));
                await AssertWire(secondClient, "SECOND\r\n");
            }
            finally
            {
                first.closeport_mult(slot, 0);
                second.closeport_mult(slot, 0);
            }
        }

        [Fact]
        public async Task DefaultAndFiveNumberedSlotsKeepTheirOwnCommandsAndStatusResponses()
        {
            var peers = Enumerable.Range(0, 6).Select(_ => new LoopbackPrinter()).ToArray();
            var clients = new List<TcpClient>();
            var transport = new ethernet();
            try
            {
                Open(transport, peers[0].Port);
                clients.Add(await peers[0].Accept());
                for (var slot = 1; slot <= 5; slot++)
                {
                    Assert.Equal(1, transport.openport_mult(slot, "127.0.0.1", peers[slot].Port, 200));
                    clients.Add(await peers[slot].Accept());
                }
                transport.sendcommand("DEFAULT");
                await AssertWire(clients[0], "DEFAULT\r\n");
                for (var slot = 1; slot <= 5; slot++)
                {
                    Assert.Equal(1, transport.sendcommand_mult(slot, "SLOT " + slot));
                    await AssertWire(clients[slot], "SLOT " + slot + "\r\n");
                    Assert.Equal(1, transport.sendcommand_mult(slot, new byte[] { 0, (byte)slot, 255 }));
                    await AssertWire(clients[slot], new byte[] { 0, (byte)slot, 255, 13, 10 });
                    await clients[slot].GetStream().WriteAsync(new byte[] { (byte)slot });
                    Assert.Equal((byte)slot, transport.printerstatus_mult(slot));
                    await AssertWire(clients[slot], new byte[] { 27, 33, 63 });
                    await clients[slot].GetStream().WriteAsync(new byte[] { 8 });
                    Assert.Equal("08", transport.printerstatus_string_mult(slot));
                    await AssertWire(clients[slot], new byte[] { 27, 33, 63 });
                }
                await clients[0].GetStream().WriteAsync(new byte[] { 8 });
                Assert.Equal((byte)8, transport.printerstatus());
                await AssertWire(clients[0], new byte[] { 27, 33, 63 });
            }
            finally
            {
                transport.closeport();
                for (var slot = 1; slot < clients.Count; slot++) transport.closeport_mult(slot, 0);
                foreach (var client in clients) client.Dispose();
                foreach (var peer in peers) peer.Dispose();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void InvalidSlotPreservesLegacyFailureResults(int slot)
        {
            var transport = new ethernet();
            Assert.Equal(0, transport.openport_mult(slot, "invalid", -1));
            Assert.Equal(0, transport.openport_mult(slot, "invalid", -1, 0));
            Assert.Equal(0, transport.sendcommand_mult(slot, "IGNORED"));
            Assert.Equal(0, transport.sendcommand_mult(slot, new byte[] { 1 }));
            Assert.Equal(0, transport.closeport_mult(slot, 0));
            Assert.Equal((byte)99, transport.printerstatus_mult(slot));
        }

        [Theory]
        [InlineData("full", false, "\u001b!S", "_ABCD", "ABCD")]
        [InlineData("full", true, "\u001b!S", "_ABCD", "ABCD")]
        [InlineData("codepage", true, "~!I", "UTF-8", "UTF-8")]
        [InlineData("mileage", false, "~!@", "123", "123")]
        [InlineData("mileage", true, "~!@", "123", "123")]
        [InlineData("name", false, "~!T", "PRINTER", "PRINTER")]
        [InlineData("name", true, "~!T", "PRINTER", "PRINTER")]
        [InlineData("file", false, "~!F", "LABEL.BMP", "LABEL.BMP")]
        [InlineData("file", true, "~!F", "LABEL.BMP", "LABEL.BMP")]
        [InlineData("memory", false, "~!T", "1024", "1024")]
        [InlineData("memory", true, "~!T", "1024", "1024")]
        [InlineData("serial", false, "OUT _SERIAL$\r\n", "SERIAL123", "SERIAL123")]
        [InlineData("serial", true, "OUT _SERIAL$\r\n", "SERIAL123", "SERIAL123")]
        [InlineData("status", false, "\u001b!?", "\b", "08")]
        [InlineData("status", true, "\u001b!?", "\b", "08")]
        public async Task MetadataRequestsUseTheirOwnConnection(string operation, bool delay, string command, string response, string expected)
        {
            using var peer = new LoopbackPrinter();
            using var otherPeer = new LoopbackPrinter();
            var transport = new ethernet();
            var other = new ethernet();
            Open(transport, peer.Port);
            using var client = await peer.Accept();
            Open(other, otherPeer.Port);
            using var otherClient = await otherPeer.Accept();
            try
            {
                await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes(response));
                var actual = (operation, delay) switch
                {
                    ("full", false) => transport.printerfullstatus(),
                    ("full", true) => transport.printerfullstatus(0),
                    ("codepage", true) => transport.printercodepage(0),
                    ("mileage", false) => transport.printermileage(),
                    ("mileage", true) => transport.printermileage(0),
                    ("name", false) => transport.printername(),
                    ("name", true) => transport.printername(0),
                    ("file", false) => transport.printerfile(),
                    ("file", true) => transport.printerfile(0),
                    ("memory", false) => transport.printermemory(),
                    ("memory", true) => transport.printermemory(0),
                    ("serial", false) => transport.printerserial(),
                    ("serial", true) => transport.printerserial(0),
                    ("status", false) => transport.printerstatus_string(),
                    ("status", true) => transport.printerstatus_string(0),
                    _ => throw new ArgumentOutOfRangeException(nameof(operation))
                };
                Assert.Equal(expected, actual);
                await AssertWire(client, command);
                other.sendcommand("OTHER");
                await AssertWire(otherClient, "OTHER\r\n");
            }
            finally
            {
                transport.closeport(0);
                other.closeport();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SettingsRequestsPreserveFramingAndReadUntilAcknowledgement(bool twoDelays)
        {
            using var peer = new LoopbackPrinter();
            var transport = new ethernet();
            Open(transport, peer.Port);
            using var client = await peer.Accept();
            try
            {
                await client.GetStream().WriteAsync(new byte[] { 79, 75, 6 });
                Assert.Equal("OK", twoDelays
                    ? transport.printersetting("APP", "SEC", "KEY", 0, 0)
                    : transport.printersetting("APP", "SEC", "KEY", 0));
                await AssertWire(client, "OUT GETSETTING$(\"APP\",\"SEC\",\"KEY\")\r\nOUT CHR$(06)\r\n");
            }
            finally { transport.closeport(); }
        }

        [Fact]
        public async Task CommandEncodingsAndDownloadFramesRemainByteExact()
        {
            using var peer = new LoopbackPrinter();
            var transport = new ethernet();
            Open(transport, peer.Port);
            using var client = await peer.Accept();
            var imagePath = Path.GetTempFileName();
            try
            {
                transport.sendcommand_utf8("가");
                await AssertWire(client, new byte[] { 0xea, 0xb0, 0x80, 13, 10 });
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                transport.sendcommand_gb2312("中");
                await AssertWire(client, new byte[] { 0xd6, 0xd0, 13, 10 });
                transport.sendcommand_big5("中");
                await AssertWire(client, new byte[] { 0xa4, 0xa4, 13, 10 });
                transport.sendcommandNOCRLF(new byte[] { 0, 255 });
                await AssertWire(client, new byte[] { 0, 255 });
                transport.setup("50", "30", "4", "8", "0", "2", "1");
                await AssertWire(client, "SIZE 50 mm,30 mm\r\nSPEED 4\r\nDENSITY 8\r\nGAP 2 mm, 1 mm\r\n");
                transport.setup("50", "30", "4", "8", "1", "2", "1");
                await AssertWire(client, "SIZE 50 mm,30 mm\r\nSPEED 4\r\nDENSITY 8\r\nBLINE 2 mm, 1 mm\r\n");
                transport.clearbuffer();
                await AssertWire(client, "CLS\r\n");
                transport.barcode("1", "2", "128", "30", "1", "0", "2", "4", "ABC");
                await AssertWire(client, "BARCODE 1,2,\"128\",30,1,0,2,4,\"ABC\"\r\n");
                transport.printerfont("1", "2", "3", "0", "1", "1", "TEXT");
                await AssertWire(client, "TEXT 1,2,\"3\",0,1,1,\"TEXT\"\r\n");
                transport.printlabel("1", "2");
                transport.formfeed();
                transport.nobackfeed();
                await AssertWire(client, "PRINT 1, 2\r\nFORMFEED\r\nSET TEAR OFF\r\n");
                await File.WriteAllBytesAsync(imagePath, new byte[] { 0, 127, 255 });
                Assert.Equal(1, transport.downloadfile(imagePath, "A.BIN"));
                await AssertDownload(client, "DOWNLOAD F,\"A.BIN\",3,");
                Assert.Equal(1, transport.downloadfile(imagePath, "E", "B.BIN"));
                await AssertDownload(client, "DOWNLOAD E,\"B.BIN\",3,");
                transport.downloadpcx(imagePath, "C.PCX");
                await AssertDownload(client, "DOWNLOAD F,\"C.PCX\",3,");
                transport.downloadbmp(imagePath, "D.BMP");
                await AssertDownload(client, "DOWNLOAD F,\"D.BMP\",3,");
                transport.printerrestart();
                await AssertWire(client, new byte[] { 27, 33, 82 });
                Assert.Equal("1", transport.WiFi_SSID("QA"));
                Assert.Equal("1", transport.WiFi_WPA("TEST"));
                Assert.Equal("1", transport.WiFi_WEP(1, "KEY"));
                Assert.Equal("1", transport.WiFi_DHCP());
                Assert.Equal("1", transport.WiFi_Port(9100));
                Assert.Equal("1", transport.WiFi_StaticIP("127.0.0.1", "255.0.0.0", "127.0.0.1"));
                await AssertWire(client, "WLAN SSID \"QA\"\r\n\r\nWLAN WPA \"TEST\"\r\n\r\nWLAN WEP 1,\"KEY\"\r\n\r\nWLAN DHCP\r\n\r\nWLAN PORT 9100\r\n\r\nWLAN IP \"127.0.0.1\",\"255.0.0.0\",\"127.0.0.1\"\r\n\r\n");
                Assert.Equal("1", transport.WiFi_Default());
                await AssertWire(client, Encoding.ASCII.GetBytes("WLAN DEFAULT\r\n\r\n").Concat(new byte[] { 27, 33, 82, 13, 10 }).ToArray());
            }
            finally
            {
                transport.closeport();
                File.Delete(imagePath);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MetadataQueryFinishesWhenThePeerEndsItsResponse(bool hasResponse)
        {
            using var peer = new LoopbackPrinter();
            var transport = new ethernet();
            Open(transport, peer.Port);
            using var client = await peer.Accept();
            var stream = client.GetStream();
            if (hasResponse) await stream.WriteAsync(Encoding.ASCII.GetBytes("PRINTER"));
            client.Client.Shutdown(SocketShutdown.Send);
            var query = Task.Run(() => transport.printername(0));
            try
            {
                Assert.Equal(hasResponse ? "PRINTER" : "-1", await query.WaitAsync(TimeSpan.FromSeconds(3)));
                await AssertWire(stream, Encoding.ASCII.GetBytes("~!T"));
            }
            finally
            {
                transport.closeport();
                await query;
            }
        }

        private static Task AssertDownload(TcpClient client, string header) =>
            AssertWire(client, Encoding.ASCII.GetBytes(header).Concat(new byte[] { 0, 127, 255, 13, 10 }).ToArray());

        private static void Open(ethernet transport, int port, bool explicitDelay = false)
        {
            Assert.True(explicitDelay
                ? transport.openport("127.0.0.1", port, 200)
                : transport.openport("127.0.0.1", port));
        }

        private static Task AssertWire(TcpClient client, string expected) =>
            AssertWire(client, Encoding.ASCII.GetBytes(expected));

        private static Task AssertWire(TcpClient client, byte[] expected) => AssertWire(client.GetStream(), expected);

        private static async Task AssertWire(Stream stream, byte[] expected)
        {
            var received = new byte[expected.Length];
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await stream.ReadExactlyAsync(received, timeout.Token);
            Assert.Equal(expected, received);
        }

        private sealed class LoopbackPrinter : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

            public LoopbackPrinter() => listener.Start();

            public async Task<TcpClient> Accept()
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                return await listener.AcceptTcpClientAsync(timeout.Token);
            }

            public void Dispose() => listener.Stop();
        }
    }
}
