using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenieSupervisor.Data_Augmentation
{
    public class HelperImagingclass
    {
        public static PixelFormat ConvertToWpfPixelFormat(System.Drawing.Imaging.PixelFormat pf)
        {
            switch (pf)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return PixelFormats.Bgr24;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return PixelFormats.Bgra32;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return PixelFormats.Bgr32;

                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    return PixelFormats.Gray8;

                case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                    return PixelFormats.BlackWhite;

                default:
                    // fallback (WPF does not support all formats)
                    return PixelFormats.Bgra32;
            }
        }

        public static (int bitDepth, System.Drawing.Imaging.PixelFormat pixelFormat) GetOriginalImageFormat(string path)
        {
            using (var bmp = new Bitmap(path))
            {
                int depth = Image.GetPixelFormatSize(bmp.PixelFormat);
                return (depth, bmp.PixelFormat);
            }
        }

        public static BmpBitmapEncoder RenderBmpBitmapImage(FrameworkElement augmentCanvas, PixelFormat targetPixelFormat)
        {
            System.Windows.Size size = new System.Windows.Size(augmentCanvas.Width, augmentCanvas.Height);

            augmentCanvas.Measure(size);
            augmentCanvas.Arrange(new Rect(size));

            // Step 1: Render ALWAYS as 32-bit
            RenderTargetBitmap renderBitmap =
                new RenderTargetBitmap(
                    (int)size.Width,
                    (int)size.Height,
                    96d,
                    96d,
                    PixelFormats.Pbgra32);

            renderBitmap.Render(augmentCanvas);

            // Step 2: Convert to 24-bit BMP
            FormatConvertedBitmap converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = renderBitmap;
            converted.DestinationFormat = targetPixelFormat; // 24-bit
            converted.EndInit();

            // Step 3: Encode
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(converted));

            return encoder;
        }
    }
}
