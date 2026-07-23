using Hyunmui.TSCPrinter.Enums;
using System;
using System.Drawing;

namespace Hyunmui.TSCPrinter
{
    public static class BitmapDithering
    {
        private static readonly int[,] BayerMatrix4x4 =
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };

        public static Bitmap Apply(Bitmap source, DitheringMode mode)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            switch (mode)
            {
                case DitheringMode.None:
                    return new Bitmap(source);
                case DitheringMode.Halftone:
                    return ApplyHalftone(source);
                case DitheringMode.ErrorDiffusion:
                    return ApplyErrorDiffusion(source);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "지원하지 않는 디더링 방식입니다.");
            }
        }

        private static Bitmap ApplyHalftone(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height);

            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var sourceColor = source.GetPixel(x, y);
                    var threshold = (BayerMatrix4x4[y % 4, x % 4] + 0.5) * 255 / 16;
                    var tone = GetLuminance(sourceColor) < threshold ? 0 : 255;
                    result.SetPixel(x, y, Color.FromArgb(sourceColor.A, tone, tone, tone));
                }
            }

            return result;
        }

        private static Bitmap ApplyErrorDiffusion(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height);
            var currentErrors = new double[source.Width + 2];
            var nextErrors = new double[source.Width + 2];

            for (var y = 0; y < source.Height; y++)
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var sourceColor = source.GetPixel(x, y);
                    var luminance = GetLuminance(sourceColor) + currentErrors[x + 1];
                    var tone = luminance < 128 ? 0 : 255;
                    var error = luminance - tone;

                    result.SetPixel(x, y, Color.FromArgb(sourceColor.A, tone, tone, tone));

                    currentErrors[x + 2] += error * 7 / 16;
                    nextErrors[x] += error * 3 / 16;
                    nextErrors[x + 1] += error * 5 / 16;
                    nextErrors[x + 2] += error / 16;
                }

                var previousErrors = currentErrors;
                currentErrors = nextErrors;
                nextErrors = previousErrors;
                Array.Clear(nextErrors, 0, nextErrors.Length);
            }

            return result;
        }

        private static double GetLuminance(Color color)
        {
            return color.R * 0.3 + color.G * 0.59 + color.B * 0.11;
        }
    }
}
