using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TSCSDK
{
    internal sealed class WindowsFontGdi : IWindowsFontGdi
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "CreateBitmap")]
        private static extern IntPtr NativeCreateBitmap(
          int nWidth,
          int nHeight,
          uint cPlanes,
          uint cBitsPerPel,
          IntPtr lpvBits);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int objectIndex);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern uint SetBkColor(IntPtr hdc, int crColor);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hDC, [In] ref ethernet.RECT lprc, IntPtr hbr);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern bool TextOut(
          IntPtr hdc,
          int nXStart,
          int nYStart,
          string lpString,
          int cbString);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern bool TextOutW(
          IntPtr hdc,
          int nXStart,
          int nYStart,
          string lpWString,
          int cbString);

        [DllImport("gdi32.dll")]
        private static extern int GetBitmapBits(IntPtr hbmp, int cbBuffer, [Out] byte[] lpvBits);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32(
          IntPtr hdc,
          string lpString,
          int cbString,
          out ethernet.SIZE lpSize);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32W(
          IntPtr hdc,
          string lpWString,
          int cbString,
          out ethernet.SIZE lpSize);

        public IntPtr GetScreenDc() => GetDC(IntPtr.Zero);
        public IntPtr CreateMemoryDc(IntPtr screenDc) => CreateCompatibleDC(screenDc);
        public IntPtr CreateFont(ethernet.LOGFONT font) => ethernet.CreateFontIndirect(font);
        public IntPtr CreateBitmap() => NativeCreateBitmap(2400, 2400, 1U, 1U, IntPtr.Zero);
        public IntPtr SelectObject(IntPtr dc, IntPtr value) => ethernet.SelectObject(dc, value);
        public bool Measure(IntPtr dc, WindowsFontRequest request, out ethernet.SIZE size)
        {
            return request.Unicode
                ? GetTextExtentPoint32W(dc, request.Content, request.Content.Length, out size)
                : GetTextExtentPoint32(dc, request.Content, request.Content.Length, out size);
        }
        public bool SetColors(IntPtr dc)
        {
            return SetTextColor(dc, ColorTranslator.ToWin32(Color.Black)) != uint.MaxValue
                && SetBkColor(dc, ColorTranslator.ToWin32(Color.White)) != uint.MaxValue;
        }
        public bool Clear(IntPtr dc)
        {
            var brush = GetStockObject(0); // Borrowed WHITE_BRUSH; never delete a stock object.
            var rect = new ethernet.RECT { Left = 0, Top = 0, Right = 2400, Bottom = 2400 };
            return brush != IntPtr.Zero && FillRect(dc, ref rect, brush) != 0;
        }
        public bool Draw(IntPtr dc, WindowsFontRequest request, int x, int y)
        {
            return request.Unicode
                ? TextOutW(dc, x, y, request.Content, request.Content.Length)
                : TextOut(dc, x, y, request.Content, request.Content.Length);
        }
        public int ReadBitmap(IntPtr bitmap, byte[] buffer) => GetBitmapBits(bitmap, buffer.Length, buffer);
        public bool DeleteObject(IntPtr value) => ethernet.DeleteObject(value);
        public bool DeleteDc(IntPtr dc) => ethernet.DeleteDC(dc);
        public bool ReleaseScreenDc(IntPtr dc) => ReleaseDC(IntPtr.Zero, dc) != 0;
    }
}
