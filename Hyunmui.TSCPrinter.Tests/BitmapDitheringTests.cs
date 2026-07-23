using Hyunmui.TSCPrinter.Enums;
using System.Collections.Generic;
using System.Drawing;
using Xunit;

namespace Hyunmui.TSCPrinter.Tests
{
    public class BitmapDitheringTests
    {
        [Fact]
        public void Apply_None_PreservesOriginalPixelValues()
        {
            using var source = new Bitmap(1, 1);
            source.SetPixel(0, 0, Color.FromArgb(255, 64, 128, 192));

            using var result = BitmapDithering.Apply(source, DitheringMode.None);

            Assert.NotSame(source, result);
            Assert.Equal(source.GetPixel(0, 0), result.GetPixel(0, 0));
        }

        [Fact]
        public void Apply_Halftone_ConvertsMidGrayToBlackAndWhitePattern()
        {
            using var source = CreateSolidBitmap(4, 4, Color.FromArgb(128, 128, 128));

            using var result = BitmapDithering.Apply(source, DitheringMode.Halftone);

            AssertBlackAndWhiteMix(result);
        }

        [Fact]
        public void Apply_ErrorDiffusion_ConvertsMidGrayToBlackAndWhitePattern()
        {
            using var source = CreateSolidBitmap(8, 8, Color.FromArgb(128, 128, 128));

            using var result = BitmapDithering.Apply(source, DitheringMode.ErrorDiffusion);

            AssertBlackAndWhiteMix(result);
        }

        [Fact]
        public void SetupOptions_DefaultsToNoDithering()
        {
            var options = new TSCPrinterSetupOptions();

            Assert.Equal(DitheringMode.None, options.DitheringMode);
        }

        private static Bitmap CreateSolidBitmap(int width, int height, Color color)
        {
            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(color);
            return bitmap;
        }

        private static void AssertBlackAndWhiteMix(Bitmap bitmap)
        {
            var tones = new HashSet<int>();

            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    Assert.Equal(color.R, color.G);
                    Assert.Equal(color.G, color.B);
                    Assert.Contains(color.R, new[] { 0, 255 });
                    tones.Add(color.R);
                }
            }

            Assert.Equal(2, tones.Count);
            Assert.Contains(0, tones);
            Assert.Contains(255, tones);
        }
    }
}
