using Hyunmui.TSCPrinter.Enums;
using System;
using System.Drawing;
using System.Reflection;
using TSCSDK;

namespace Hyunmui.TSCPrinter
{
    public class TSCPrinter
    {
        public usb Device { get; private set; }

        public TSCPrinter(usb device)
        {
            Device = device;
        }

        public void Print(Bitmap bitmap, TSCPrinterSetupOptions options)
        {
            if (!Device.openport())
                throw new TscException("프린터 포트를 열 수 없습니다.");

            // Auto rotation 기능 적용
            if (options.AutoRotation)
            {
                var labelRatio = (float)options.LabelWidthMillimeter / options.LabelHeightMillimeter;
                var imageRatio = (float)bitmap.Width / bitmap.Height;

                bool isRatioMismatch =
                    (labelRatio >= 1 && imageRatio < 1) ||
                    (labelRatio < 1 && imageRatio >= 1);

                if (isRatioMismatch)
                {
                    switch (options.PrintDirection)
                    {
                        case PrintDirection.NormalMirror:
                        case PrintDirection.ReverseMirror:
                            bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        default:
                            bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                    }
                }
            }

            Setup(options);
            Device.sendpicture(0, 0, bitmap);
            Device.printlabel(options.PrintCount.ToString(), options.CopyCount.ToString());
            Device.clearbuffer();
            Device.closeport();
        }

        protected void Setup(TSCPrinterSetupOptions options)
        {
            // 버퍼 초기화
            Device.clearbuffer();

            // 기본 설정
            Device.setup(options.LabelWidthMillimeter.ToString(),
                options.LabelHeightMillimeter.ToString(),
                options.Speed.ToString(),
                options.Density.ToString(),
                ((int)options.SensorType).ToString(),
                options.GapBlackLineHeight.ToString(),
                options.GapBlackLineOffset.ToString());

            // 방향
            switch (options.PrintDirection)
            {
                case PrintDirection.Reverse:
                    Device.sendcommand("DIRECTION 0,0");
                    break;
                case PrintDirection.ReverseMirror:
                    Device.sendcommand("DIRECTION 0,1");
                    break;
                case PrintDirection.NormalMirror:
                    Device.sendcommand("DIRECTION 1,1");
                    break;
                default:
                    Device.sendcommand("DIRECTION 1,0");
                    break;
            }

            // 커터
            Device.sendcommand($"SET CUTTER {(options.UseCutter ? "ON" : "OFF")}");

            // Offset
            Device.sendcommand($"OFFSET {options.Offset} mm");

            // Ribbon on
            Device.sendcommand($"SET RIBBON {(options.UseRibbon ? "ON" : "OFF")}");

            // Reference X/Y
            Device.sendcommand($"REFERENCE {options.ReferenceX.ToDots(Dpi)},{options.ReferenceY.ToDots(Dpi)}");
        }

        public void FormFeed(TSCPrinterSetupOptions options)
        {
            if (!Device.openport())
                throw new TscException("프린터 포트를 열 수 없습니다.");

            // 기본 설정
            Device.setup(options.LabelWidthMillimeter.ToString(),
                options.LabelHeightMillimeter.ToString(),
                "7",
                options.Density.ToString(),
                ((int)options.SensorType).ToString(),
                options.GapBlackLineHeight.ToString(),
                options.GapBlackLineOffset.ToString());

            // 커터
            Device.sendcommand($"SET CUTTER {(options.UseCutter ? "ON" : "OFF")}");

            // Ribbon on
            Device.sendcommand($"SET RIBBON {(options.UseRibbon ? "ON" : "OFF")}");

            Device.formfeed();
            Device.sendcommand("FEED 80");

            Device.clearbuffer();

            Device.closeport();
        }

        private int _dpi;
        public int Dpi
        {
            get
            {
                if (_dpi == default)
                {
                    Device.openport();
                    var dpi = Device.printersetting("SYSTEM", "INFORMATION", "DPI");
                    if (!int.TryParse(dpi, out _dpi))
                        _dpi = 200;
                }

                return _dpi;
            }
        }
    }
}
