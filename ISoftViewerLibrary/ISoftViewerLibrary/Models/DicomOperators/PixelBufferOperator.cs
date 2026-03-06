using System;

namespace ISoftViewerLibrary.Model.DicomOperator
{
    /// <summary>
    /// 直接操作 DICOM 原始 pixel buffer，支援 8/10/12/14/16-bit
    /// </summary>
    public class PixelBufferOperator
    {
        private readonly byte[] _buffer;
        private readonly DicomPixelInfo _info;

        public PixelBufferOperator(byte[] buffer, DicomPixelInfo info)
        {
            _buffer = buffer;
            _info = info;
        }

        /// <summary>
        /// 用背景色填充指定矩形區域（遮蓋舊標記）
        /// </summary>
        public void FillRegion(int x, int y, int width, int height)
        {
            ushort fillValue = _info.BackgroundValue;
            int x2 = Math.Min(x + width, _info.Columns);
            int y2 = Math.Min(y + height, _info.Rows);
            x = Math.Max(x, 0);
            y = Math.Max(y, 0);

            if (_info.BitsAllocated <= 8)
            {
                byte fill8 = (byte)fillValue;
                for (int row = y; row < y2; row++)
                {
                    for (int col = x; col < x2; col++)
                    {
                        int offset = (row * _info.Columns + col) * _info.SamplesPerPixel;
                        _buffer[offset] = fill8;
                    }
                }
            }
            else // 16-bit allocated (covers 10, 12, 14, 16 stored)
            {
                byte lo = (byte)(fillValue & 0xFF);
                byte hi = (byte)(fillValue >> 8);
                for (int row = y; row < y2; row++)
                {
                    for (int col = x; col < x2; col++)
                    {
                        int offset = (row * _info.Columns + col) * _info.SamplesPerPixel * 2;
                        _buffer[offset] = lo;
                        _buffer[offset + 1] = hi;
                    }
                }
            }
        }

        /// <summary>
        /// 將文字遮罩（grayscale 8-bit）套用到指定位置，白色像素寫入前景色
        /// </summary>
        public void ApplyMask(byte[] mask, int maskWidth, int maskHeight,
            int targetX, int targetY, byte threshold = 128)
        {
            ushort fgValue = _info.ForegroundValue;

            for (int my = 0; my < maskHeight; my++)
            {
                int imgY = targetY + my;
                if (imgY < 0 || imgY >= _info.Rows) continue;

                for (int mx = 0; mx < maskWidth; mx++)
                {
                    int imgX = targetX + mx;
                    if (imgX < 0 || imgX >= _info.Columns) continue;

                    byte maskPixel = mask[my * maskWidth + mx];
                    if (maskPixel < threshold) continue;

                    if (_info.BitsAllocated <= 8)
                    {
                        int offset = (imgY * _info.Columns + imgX) * _info.SamplesPerPixel;
                        _buffer[offset] = (byte)fgValue;
                    }
                    else
                    {
                        int offset = (imgY * _info.Columns + imgX) * _info.SamplesPerPixel * 2;
                        _buffer[offset] = (byte)(fgValue & 0xFF);
                        _buffer[offset + 1] = (byte)(fgValue >> 8);
                    }
                }
            }
        }
    }
}
