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
        public int Speed { get; set; } = 7;
        public int Density { get; set; } = 9;
        public SensorType SensorType { get; set; } = SensorType.Gap;
        /// <summary>
        /// 단위: mm
        /// </summary>
        public decimal GapBlackLineHeight { get; set; } = 3;
        /// <summary>
        /// 단위: mm
        /// </summary>
        public decimal GapBlackLineOffset { get; set; } = 0;
        /// <summary>
        /// 단위: mm
        /// </summary>
        public decimal Offset { get; set; }
        public PrintDirection PrintDirection { get; set; } = PrintDirection.Normal;
        public bool UseCutter { get; set; } = true;
        public int ReferenceX { get; set; }
        public int ReferenceY { get; set; }
        public int PrintCount { get; set; } = 1;
        public int CopyCount { get; set; } = 1;
    }
}
