namespace Hyunmui.TSCPrinter
{
    public static class Extensions
    {
        public static int ToDots(this int millimeter, int dpi)
        {
            return (int)(millimeter * dpi / 25.4m);
        }
    }
}