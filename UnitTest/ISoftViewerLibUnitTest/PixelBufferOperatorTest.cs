using System;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DicomOperators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class PixelBufferOperatorTest
    {
        [TestMethod]
        public void FillRegion_8Bit_Mono2_FillsWithZero()
        {
            var info = new DicomPixelInfo
            {
                Rows = 4, Columns = 4,
                BitsAllocated = 8, BitsStored = 8, HighBit = 7,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            byte[] buffer = new byte[16];
            for (int i = 0; i < 16; i++) buffer[i] = 128;

            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(1, 1, 2, 2);

            Assert.AreEqual(0, buffer[5]);   // Row 1, Col 1
            Assert.AreEqual(0, buffer[6]);   // Row 1, Col 2
            Assert.AreEqual(0, buffer[9]);   // Row 2, Col 1
            Assert.AreEqual(0, buffer[10]);  // Row 2, Col 2
            Assert.AreEqual(128, buffer[0]); // Untouched
            Assert.AreEqual(128, buffer[3]);
            Assert.AreEqual(128, buffer[15]);
        }

        [TestMethod]
        public void FillRegion_16Bit_12Stored_Mono2_FillsWithZero()
        {
            var info = new DicomPixelInfo
            {
                Rows = 4, Columns = 4,
                BitsAllocated = 16, BitsStored = 12, HighBit = 11,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            byte[] buffer = new byte[32];
            for (int i = 0; i < 16; i++)
                BitConverter.GetBytes((ushort)2048).CopyTo(buffer, i * 2);

            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(0, 0, 2, 2);

            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 0));
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 2));
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 8));
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 10));
            Assert.AreEqual((ushort)2048, BitConverter.ToUInt16(buffer, 4)); // Untouched
        }

        [TestMethod]
        public void FillRegion_16Bit_12Stored_Mono1_FillsWithMax()
        {
            var info = new DicomPixelInfo
            {
                Rows = 2, Columns = 2,
                BitsAllocated = 16, BitsStored = 12, HighBit = 11,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME1",
                SamplesPerPixel = 1
            };
            byte[] buffer = new byte[8];
            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(0, 0, 1, 1);

            Assert.AreEqual((ushort)4095, BitConverter.ToUInt16(buffer, 0));
        }

        [TestMethod]
        public void ApplyMask_16Bit_12Stored_Mono2_WritesForeground()
        {
            var info = new DicomPixelInfo
            {
                Rows = 4, Columns = 4,
                BitsAllocated = 16, BitsStored = 12, HighBit = 11,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            byte[] buffer = new byte[32];
            var op = new PixelBufferOperator(buffer, info);

            byte[] mask = { 255, 0, 0, 255 };
            op.ApplyMask(mask, 2, 2, targetX: 1, targetY: 1);

            Assert.AreEqual((ushort)4095, BitConverter.ToUInt16(buffer, (1 * 4 + 1) * 2));
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, (1 * 4 + 2) * 2));
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, (2 * 4 + 1) * 2));
            Assert.AreEqual((ushort)4095, BitConverter.ToUInt16(buffer, (2 * 4 + 2) * 2));
        }

        /// <summary>
        /// 測試 HighBit 高位對齊：BitsAllocated=16, BitsStored=12, HighBit=15
        /// 像素值需要左移 4 位 (BitShift = 15+1-12 = 4)
        /// </summary>
        [TestMethod]
        public void FillRegion_HighBitAligned_ShiftsCorrectly()
        {
            var info = new DicomPixelInfo
            {
                Rows = 2, Columns = 2,
                BitsAllocated = 16, BitsStored = 12, HighBit = 15,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            // BitShift = 15+1-12 = 4
            // Background (MONO2) = 0 << 4 = 0
            // Foreground (MONO2) = 4095 << 4 = 65520 (0xFFF0)
            Assert.AreEqual(4, info.BitShift);
            Assert.AreEqual((ushort)0, info.BackgroundValue);
            Assert.AreEqual((ushort)65520, info.ForegroundValue);

            byte[] buffer = new byte[8];
            for (int i = 0; i < 4; i++)
                BitConverter.GetBytes((ushort)32768).CopyTo(buffer, i * 2);

            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(0, 0, 1, 1);
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 0));

            byte[] mask = { 255 };
            op.ApplyMask(mask, 1, 1, targetX: 1, targetY: 0);
            Assert.AreEqual((ushort)65520, BitConverter.ToUInt16(buffer, 2));
        }

        /// <summary>
        /// 測試 Signed pixel (PixelRepresentation=1)
        /// 16-bit signed: 黑色 = -32768 (0x8000), 白色 = 32767 (0x7FFF)
        /// </summary>
        [TestMethod]
        public void FillRegion_Signed16Bit_UsesSignedRange()
        {
            var info = new DicomPixelInfo
            {
                Rows = 2, Columns = 2,
                BitsAllocated = 16, BitsStored = 16, HighBit = 15,
                PixelRepresentation = 1, // signed
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            // Signed MONO2: Background(黑) = -32768 = 0x8000, Foreground(白) = 32767 = 0x7FFF
            Assert.IsTrue(info.IsSigned);
            Assert.AreEqual((ushort)0x8000, info.BackgroundValue);
            Assert.AreEqual((ushort)0x7FFF, info.ForegroundValue);

            byte[] buffer = new byte[8];
            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(0, 0, 1, 1);

            // 背景色 = 0x8000
            Assert.AreEqual((ushort)0x8000, BitConverter.ToUInt16(buffer, 0));
            // 未修改的像素仍為 0
            Assert.AreEqual((ushort)0, BitConverter.ToUInt16(buffer, 2));
        }

        /// <summary>
        /// 測試 Signed + MONOCHROME1（反轉）
        /// </summary>
        [TestMethod]
        public void FillRegion_SignedMono1_InvertedRange()
        {
            var info = new DicomPixelInfo
            {
                Rows = 2, Columns = 2,
                BitsAllocated = 16, BitsStored = 16, HighBit = 15,
                PixelRepresentation = 1,
                PhotometricInterpretation = "MONOCHROME1",
                SamplesPerPixel = 1
            };
            // Signed MONO1: Background(黑) = 32767 = 0x7FFF, Foreground(白) = -32768 = 0x8000
            Assert.AreEqual((ushort)0x7FFF, info.BackgroundValue);
            Assert.AreEqual((ushort)0x8000, info.ForegroundValue);

            byte[] buffer = new byte[8];
            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(0, 0, 1, 1);
            Assert.AreEqual((ushort)0x7FFF, BitConverter.ToUInt16(buffer, 0));
        }

        [TestMethod]
        public void FillRegion_OutOfBounds_ClipsToImage()
        {
            var info = new DicomPixelInfo
            {
                Rows = 4, Columns = 4,
                BitsAllocated = 8, BitsStored = 8, HighBit = 7,
                PixelRepresentation = 0,
                PhotometricInterpretation = "MONOCHROME2",
                SamplesPerPixel = 1
            };
            byte[] buffer = new byte[16];
            for (int i = 0; i < 16; i++) buffer[i] = 100;

            var op = new PixelBufferOperator(buffer, info);
            op.FillRegion(3, 3, 5, 5);

            Assert.AreEqual(0, buffer[15]);    // (3,3) filled
            Assert.AreEqual(100, buffer[10]);  // (2,2) untouched
        }
    }
}
