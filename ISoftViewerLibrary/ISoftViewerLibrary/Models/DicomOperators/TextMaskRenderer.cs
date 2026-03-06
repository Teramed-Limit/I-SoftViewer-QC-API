using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace ISoftViewerLibrary.Model.DicomOperator
{
    /// <summary>
    /// 使用 System.Drawing 產生文字遮罩 bitmap（黑底白字 grayscale）
    /// </summary>
    public static class TextMaskRenderer
    {
        /// <summary>
        /// 產生文字遮罩
        /// </summary>
        /// <param name="text">文字內容 (L 或 R)</param>
        /// <param name="fontSize">字體大小</param>
        /// <returns>(mask: 8-bit grayscale byte array, width, height)</returns>
        public static (byte[] mask, int width, int height) RenderMask(string text, int fontSize)
        {
#pragma warning disable CA1416
            using var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

            SizeF textSize;
            using (var tempBmp = new Bitmap(1, 1))
            using (var tempG = Graphics.FromImage(tempBmp))
            {
                textSize = tempG.MeasureString(text, font);
            }

            int width = (int)Math.Ceiling(textSize.Width);
            int height = (int)Math.Ceiling(textSize.Height);
            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString(text, font, Brushes.White, 0, 0);
            }

            byte[] mask = new byte[width * height];
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* line = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixel = line + x * 3;
                        mask[y * width + x] = pixel[2]; // R channel
                    }
                    line += bmpData.Stride;
                }
            }

            bitmap.UnlockBits(bmpData);
            return (mask, width, height);
#pragma warning restore CA1416
        }
    }
}
