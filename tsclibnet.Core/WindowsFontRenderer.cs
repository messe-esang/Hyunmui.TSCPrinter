using System;

namespace TSCSDK
{
    internal interface IWindowsFontGdi
    {
        IntPtr GetScreenDc();
        IntPtr CreateMemoryDc(IntPtr screenDc);
        IntPtr CreateFont(ethernet.LOGFONT font);
        IntPtr CreateBitmap();
        IntPtr SelectObject(IntPtr dc, IntPtr value);
        bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size);
        bool SetColors(IntPtr dc);
        bool Clear(IntPtr dc);
        bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y);
        int ReadBitmap(IntPtr bitmap, byte[] buffer);
        bool DeleteObject(IntPtr value);
        bool DeleteDc(IntPtr dc);
        bool ReleaseScreenDc(IntPtr dc);
    }

    internal static class WindowsFontRenderer
    {
        internal static byte[] Render(WindowsFontRequest request, IWindowsFontGdi native)
        {
            var resources = new FontResources(native);
            var failed = false;
            try
            {
                resources.ScreenDc = RequireHandle(native.GetScreenDc(), "GetDC");
                resources.MemoryDc = RequireHandle(native.CreateMemoryDc(resources.ScreenDc), "CreateCompatibleDC");
                resources.Font = RequireHandle(native.CreateFont(CreateLogFont(request)), "CreateFontIndirect");
                resources.OldFont = RequireHandle(native.SelectObject(resources.MemoryDc, resources.Font), "SelectObject(font)");
                ethernet.SIZE size;
                Require(native.Measure(resources.MemoryDc, request, out size), "GetTextExtentPoint32");
                var geometry = WindowsFontGeometry.Create(request, size);
                if (geometry.RowBytes == 0 || geometry.Rows == 0) return Array.Empty<byte>();
                resources.Bitmap = RequireHandle(native.CreateBitmap(), "CreateBitmap");
                resources.OldBitmap = RequireHandle(native.SelectObject(resources.MemoryDc, resources.Bitmap), "SelectObject(bitmap)");
                Require(native.SetColors(resources.MemoryDc), "SetTextColor/SetBkColor");
                Require(native.Clear(resources.MemoryDc), "FillRect");
                Require(native.Draw(resources.MemoryDc, request, geometry.DrawX, geometry.DrawY), "TextOut");
                var buffer = new byte[WindowsFontGeometry.BufferLength];
                Require(native.ReadBitmap(resources.Bitmap, buffer) == buffer.Length, "GetBitmapBits");
                return geometry.CreatePacket(buffer);
            }
            catch
            {
                failed = true;
                throw;
            }
            finally
            {
                // Release GDI resources synchronously, before any transport callback can run.
                resources.Release(failed);
            }
        }

        private static ethernet.LOGFONT CreateLogFont(WindowsFontRequest request)
        {
            return new ethernet.LOGFONT
            {
                lfWidth = 0,
                lfEscapement = request.Rotation * 10,
                lfOrientation = 0,
                lfCharSet = 1,
                lfOutPrecision = 0,
                lfClipPrecision = 0,
                lfQuality = 1,
                lfPitchAndFamily = 26,
                lfFaceName = request.FaceName,
                lfHeight = request.Height,
                lfItalic = 0,
                lfUnderline = 0,
                lfStrikeOut = 0,
                lfWeight = request.Style < 2 ? 400 : 700
            };
        }

        private static bool IsHandle(IntPtr handle) => handle != IntPtr.Zero && handle != new IntPtr(-1);

        private static IntPtr RequireHandle(IntPtr handle, string operation)
        {
            Require(IsHandle(handle), operation);
            return handle;
        }

        private static void Require(bool success, string operation)
        {
            if (!success) throw new InvalidOperationException("GDI operation failed: " + operation);
        }

        private sealed class FontResources
        {
            private readonly IWindowsFontGdi native;
            internal IntPtr ScreenDc;
            internal IntPtr MemoryDc;
            internal IntPtr Font;
            internal IntPtr Bitmap;
            internal IntPtr OldFont;
            internal IntPtr OldBitmap;

            internal FontResources(IWindowsFontGdi native) { this.native = native; }

            internal void Release(bool preserveOriginalFailure)
            {
                var restored = true;
                if (IsHandle(OldFont)) restored &= IsHandle(native.SelectObject(MemoryDc, OldFont));
                if (IsHandle(OldBitmap)) restored &= IsHandle(native.SelectObject(MemoryDc, OldBitmap));
                var success = restored;
                // A failed restore leaves an owned object selected. Delete its DC first in that case.
                var dcDeleted = !IsHandle(MemoryDc);
                if (!restored && !dcDeleted) dcDeleted = native.DeleteDc(MemoryDc);
                if (restored || dcDeleted)
                {
                    if (IsHandle(Font)) success &= native.DeleteObject(Font);
                    if (IsHandle(Bitmap)) success &= native.DeleteObject(Bitmap);
                }
                if (!dcDeleted) success &= native.DeleteDc(MemoryDc);
                if (IsHandle(ScreenDc)) success &= native.ReleaseScreenDc(ScreenDc);
                if (!preserveOriginalFailure) Require(success, "font resource cleanup");
            }
        }
    }
}
