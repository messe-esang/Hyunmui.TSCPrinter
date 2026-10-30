using System;
using System.Drawing;

namespace TSCSDK
{
    internal static class BitmapCommand
    {
        internal static void SendFile(int x, int y, string filename, Action<string> writeHeader, Action<byte[]> writeData)
        {
            using (var source = new Bitmap(filename))
            {
                Send(x, y, source, writeHeader, writeData);
            }
        }

        // The caller owns source. Only the grayscale working image belongs to this method.
        internal static void Send(int x, int y, Bitmap source, Action<string> writeHeader, Action<byte[]> writeData)
        {
            using (var grayscale = new Bitmap(source.Width, source.Height))
            {
                for (var column = 0; column < source.Width; column++)
                {
                    for (var row = 0; row < source.Height; row++)
                    {
                        var pixel = source.GetPixel(column, row);
                        var tone = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
                        grayscale.SetPixel(column, row, Color.FromArgb(pixel.A, tone, tone, tone));
                    }
                }

                var rowBytes = (grayscale.Width + 7) / 8;
                writeHeader($"BITMAP {x},{y},{rowBytes},{grayscale.Height}, 0,");
                var command = new byte[rowBytes * grayscale.Height];
                for (var index = 0; index < command.Length; index++) command[index] = byte.MaxValue;
                for (var row = 0; row < grayscale.Height; row++)
                {
                    for (var column = 0; column < grayscale.Width; column++)
                    {
                        var pixel = grayscale.GetPixel(column, row);
                        if ((pixel.R + pixel.G + pixel.B) / 3 < 128)
                            command[row * rowBytes + column / 8] ^= (byte)(128 >> column % 8);
                    }
                }

                writeData(command);
                // Keep the legacy transport calls, including each transport's own CRLF behavior.
                writeData(new byte[] { 13, 10 });
            }
        }
    }
}
