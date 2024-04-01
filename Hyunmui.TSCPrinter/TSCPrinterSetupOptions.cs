using Hyunmui.TSCPrinter.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hyunmui.TSCPrinter
{
    public class TSCPrinterSetupOptions
    {
        public int LabelWidthMillimeter { get; set; }
        public int LabelHeightMillimeter { get; set; }
        public decimal SpeedInchPerSec { get; set; } = 7;
        public int Density { get; set; } = 9;
        public SensorType SensorType { get; set; } = SensorType.Gap;
        public decimal GapBlackLineHeight { get; set; } = 3;
        public decimal GapBlackLineOffset { get; set; } = 0;
        public decimal Offset { get; set; }
        public PrintDirection PrintDirection { get; set; } = PrintDirection.Normal;
        public bool UseCutter { get; set; } = true;
        public int PrintCount { get; set; } = 1;
        public int CopyCount { get; set; } = 1;
        public int ReferenceMillimeterX { get; set; } = 0;
        public int ReferenceMillimeterY { get; set; } = 0;
    }
}
