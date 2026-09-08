using System;
using System.IO;
using System.Text;

namespace TSCSDK
{
    internal sealed class WindowsFontRequest
    {
        internal int X { get; set; }
        internal int Y { get; set; }
        internal int Height { get; set; }
        internal int Rotation { get; set; }
        internal int Style { get; set; }
        internal string FaceName { get; set; }
        internal string Content { get; set; }
        internal bool Unicode { get; set; }
    }

    internal sealed class WindowsFontGeometry
    {
        internal const int CanvasSize = 2400;
        internal const int Stride = CanvasSize / 8;
        internal const int BufferLength = Stride * CanvasSize;
        internal int DrawX { get; private set; }
        internal int DrawY { get; private set; }
        internal long X { get; private set; }
        internal long Y { get; private set; }
        internal int RowBytes { get; private set; }
        internal int Rows { get; private set; }

        internal static WindowsFontGeometry Create(WindowsFontRequest request, ethernet.SIZE size)
        {
            // Preserve the legacy geometry fallback for angles other than the four right angles.
            var horizontal = request.Rotation == 0 || request.Rotation == 180;
            var sideways = request.Rotation == 90 || request.Rotation == 270;
            var width = horizontal ? size.cx : size.cy;
            var height = sideways ? size.cx : size.cy;
            if (width < 0 || height < 0 || width > CanvasSize || height > CanvasSize)
                throw new ArgumentOutOfRangeException(nameof(size), "Measured font bitmap exceeds the 2400 by 2400 canvas.");

            var drawX = request.Rotation == 0 || request.Rotation == 90 ? 0
                : request.Rotation == 180 ? size.cx : size.cy;
            return new WindowsFontGeometry
            {
                DrawX = drawX,
                DrawY = request.Rotation == 0 || request.Rotation == 270 ? 0 : height,
                X = (long)request.X - drawX,
                Y = (long)request.Y - (request.Rotation == 0 || request.Rotation == 270 ? 0 : height),
                RowBytes = (width + 7) / 8,
                Rows = height
            };
        }

        internal byte[] CreatePacket(byte[] bitmap)
        {
            if (bitmap == null || bitmap.Length != BufferLength)
                throw new ArgumentException("Expected one 2400 by 2400 monochrome canvas.", nameof(bitmap));
            var skipBytes = (int)Math.Min(RowBytes, X < 0 ? (-X + 7) / 8 : 0);
            var skipRows = (int)Math.Min(Rows, Y < 0 ? -Y : 0);
            var width = RowBytes - skipBytes;
            var height = Rows - skipRows;
            if (width == 0 || height == 0) return Array.Empty<byte>();

            var header = Encoding.ASCII.GetBytes(FormattableString.Invariant($"BITMAP {Math.Max(0, X)},{Math.Max(0, Y)},{width},{height},1,"));
            var packet = new byte[header.Length + width * height + 2];
            Buffer.BlockCopy(header, 0, packet, 0, header.Length);
            for (var row = 0; row < height; row++)
                Buffer.BlockCopy(bitmap, (row + skipRows) * Stride + skipBytes, packet, header.Length + row * width, width);
            packet[packet.Length - 2] = 13;
            packet[packet.Length - 1] = 10;
            return packet;
        }
    }

    internal static class WindowsFontCommand
    {
        internal static void Send(WindowsFontRequest request, IWindowsFontGdi native, Func<byte[], int, int, int> write)
        {
            if (request.Content == null) throw new ArgumentNullException(nameof(request.Content));
            if (request.Content.Length == 0) return;
            var packet = WindowsFontRenderer.Render(request, native);
            var offset = 0;
            while (offset < packet.Length)
            {
                var written = write(packet, offset, packet.Length - offset);
                if (written <= 0 || written > packet.Length - offset)
                    throw new IOException("The font bitmap transport did not make valid progress.");
                offset += written;
            }
        }
    }
}
