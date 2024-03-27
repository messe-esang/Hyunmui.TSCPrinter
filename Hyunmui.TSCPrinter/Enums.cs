using System;
using System.Collections.Generic;
using System.Text;

namespace Hyunmui.TSCPrinter.Enums
{
    public enum MeasurementUnit
    {
        inch, mm, dot
    }

    public enum PrintDirection
    {
        Normal, Reverse, NormalMirror, ReverseMirror
    }

    public enum CountryCode
    {
        USA = 001,
        CanadianFrench = 002,
        SpanishLatin = 003,
        Dutch = 031,
        Belgian = 032,
        French = 033,
        Spanish = 034,
        Hungarian = 036,
        Yugoslavian = 038,
        Italian = 039,
        Switzerland = 041,
        Slovak = 042,
        UnitedKingdom = 044,
        Danish = 045,
        Swedish = 046,
        Norwegian = 047,
        Polish = 048,
        German = 049,
        Brazil = 055,
        English = 061,
        Portuguese = 351,
        Finnish = 358,
    }

    public enum SelfTestPage
    {
        Default,
        Pattern,
        Ethernet,
        WLan,
        RS232,
        System,
        Printer,
        Z
    }

    public enum GraphicMode
    {
        Overwrite, Or, Xor
    }

    public enum TSCPrinterStatus
    {
        Ok = 0x00,
        HeadOpened = 0x01,
        PaperJam = 0x02,
        PaperJamAndHeadOpened = 0x03,
        OutOfPaper = 0x04,
        OutOfPaperAndHeadOpened = 0x05,
        OutOfRibbon = 0x08,
        OutOfRibbonAndHeadOpened = 0x09,
        OutOfRibbonAndPaperJam = 0x0A,
        OutOfRibbonPaperJamAndHeadOpened = 0x0B,
        OutOfRibbonAndOutOfPaper = 0x0C,
        OutOfRibbonOutOfPaperAndHeadOpened = 0x0D,
        Pause = 0x10,
        Printing = 0x20,
        OtherError = 0x80,
    }

    public enum SensorType
    {
        Gap, BlackLine
    }
}
