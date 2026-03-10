using Dicom;

namespace ISoftViewerLibrary.Model.DicomOperator
{
    /// <summary>
    /// 儲存 DICOM 影像的像素資訊，用於 pixel data 操作
    /// </summary>
    public class DicomPixelInfo
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int BitsAllocated { get; set; }
        public int BitsStored { get; set; }
        public int HighBit { get; set; }
        public int PixelRepresentation { get; set; }
        public string PhotometricInterpretation { get; set; } = "MONOCHROME2";
        public int SamplesPerPixel { get; set; } = 1;

        public int BytesPerPixel => BitsAllocated / 8 * SamplesPerPixel;

        /// <summary>
        /// 有效像素值的最大值 (根據 BitsStored)
        /// 例如: 10-bit → 1023, 12-bit → 4095, 16-bit → 65535
        /// </summary>
        public ushort MaxPixelValue => (ushort)((1 << BitsStored) - 1);

        /// <summary>
        /// 像素值是否需要位移（HighBit 高位對齊的情況）
        /// 標準情況: HighBit = BitsStored - 1（低位對齊，shift = 0）
        /// 非標準:   HighBit = BitsAllocated - 1（高位對齊，需要左移）
        /// </summary>
        public int BitShift => HighBit + 1 - BitsStored;

        /// <summary>
        /// 是否為 signed 像素表示 (PixelRepresentation = 1)
        /// </summary>
        public bool IsSigned => PixelRepresentation == 1;

        /// <summary>
        /// 背景色（用於遮蓋舊標記，視覺上的「黑色」）
        /// </summary>
        public ushort BackgroundValue
        {
            get
            {
                ushort blackValue;
                if (IsSigned)
                {
                    // Signed: 最小值 = -(2^(BitsStored-1))，存為 two's complement
                    // 例如 16-bit signed: -32768 = 0x8000
                    short minSigned = (short)(-(1 << (BitsStored - 1)));
                    blackValue = (ushort)minSigned;
                }
                else
                {
                    blackValue = 0;
                }

                ushort whiteValue = IsSigned
                    ? (ushort)(short)((1 << (BitsStored - 1)) - 1)
                    : MaxPixelValue;

                // MONOCHROME2: 最小值=黑, MONOCHROME1: 最大值=黑
                ushort value = PhotometricInterpretation == "MONOCHROME2" ? blackValue : whiteValue;
                return (ushort)(value << BitShift);
            }
        }

        /// <summary>
        /// 前景色（用於繪製新標記文字，視覺上的「白色」）
        /// </summary>
        public ushort ForegroundValue
        {
            get
            {
                ushort whiteValue = IsSigned
                    ? (ushort)(short)((1 << (BitsStored - 1)) - 1)
                    : MaxPixelValue;

                ushort blackValue;
                if (IsSigned)
                {
                    short minSigned = (short)(-(1 << (BitsStored - 1)));
                    blackValue = (ushort)minSigned;
                }
                else
                {
                    blackValue = 0;
                }

                // MONOCHROME2: 最大值=白, MONOCHROME1: 最小值=白
                ushort value = PhotometricInterpretation == "MONOCHROME2" ? whiteValue : blackValue;
                return (ushort)(value << BitShift);
            }
        }

        public static DicomPixelInfo FromDataset(DicomDataset dataset)
        {
#pragma warning disable CS0618
            return new DicomPixelInfo
            {
                Rows = dataset.Get<int>(DicomTag.Rows),
                Columns = dataset.Get<int>(DicomTag.Columns),
                BitsAllocated = dataset.Get<int>(DicomTag.BitsAllocated),
                BitsStored = dataset.Get<int>(DicomTag.BitsStored),
                HighBit = dataset.Get<int>(DicomTag.HighBit),
                PixelRepresentation = dataset.Contains(DicomTag.PixelRepresentation)
                    ? dataset.Get<int>(DicomTag.PixelRepresentation) : 0,
                PhotometricInterpretation = dataset.Contains(DicomTag.PhotometricInterpretation)
                    ? dataset.Get<string>(DicomTag.PhotometricInterpretation) : "MONOCHROME2",
                SamplesPerPixel = dataset.Contains(DicomTag.SamplesPerPixel)
                    ? dataset.Get<int>(DicomTag.SamplesPerPixel) : 1
            };
#pragma warning restore CS0618
        }
    }
}
