using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class PixelDataMarkerServiceTest
    {
        private DicomFile CreateTestDicomFile(int rows, int cols, int bitsAllocated,
            int bitsStored, string photometric)
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)cols);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)bitsAllocated);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)bitsStored);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)(bitsStored - 1));
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, photometric);
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
            dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.1");
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
#pragma warning restore CS0618

            int bytesPerPixel = bitsAllocated / 8;
            byte[] pixelBuffer = new byte[rows * cols * bytesPerPixel];
            ushort midValue = (ushort)((1 << bitsStored) / 2);
            if (bytesPerPixel == 2)
            {
                for (int i = 0; i < rows * cols; i++)
                    System.BitConverter.GetBytes(midValue).CopyTo(pixelBuffer, i * 2);
            }
            else
            {
                for (int i = 0; i < pixelBuffer.Length; i++)
                    pixelBuffer[i] = (byte)(midValue & 0xFF);
            }

            // Use DicomPixelData API to properly add pixel data
            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.AddFrame(new MemoryByteBuffer(pixelBuffer));

            var dcmFile = new DicomFile(dataset);
            return dcmFile;
        }

        [TestMethod]
        public void CorrectMarkers_12Bit_Mono2_ModifiesPixelData()
        {
            var dcmFile = CreateTestDicomFile(256, 256, 16, 12, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 10, Y = 10, Width = 30, Height = 20 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 50, Y = 50, Text = "L", FontSize = 24 }
                },
                GenerateNewSopInstanceUid = false
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);

#pragma warning disable CS0618
            var desc = modifiedFile.Dataset.Get<string>(DicomTag.DerivationDescription, "");
            Assert.IsTrue(desc.Contains("L/R marker corrected"));
            Assert.IsTrue(modifiedFile.Dataset.Contains(DicomTag.DerivationCodeSequence));
#pragma warning restore CS0618
        }

        [TestMethod]
        public void CorrectMarkers_8Bit_Mono1_ModifiesPixelData()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 8, 8, "MONOCHROME1");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 20, Height = 15 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 80, Y = 80, Text = "R", FontSize = 16 }
                },
                GenerateNewSopInstanceUid = true
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(response.NewSopInstanceUid);
        }

        [TestMethod]
        public void CorrectMarkers_16Bit_14Stored_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 14, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 0, Y = 0, Width = 10, Height = 10 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "L", FontSize = 12 }
                },
                GenerateNewSopInstanceUid = false
            };

            var service = new PixelDataMarkerService();
            var (response, _) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
        }

        [TestMethod]
        public void CorrectMarkers_BigEndianInput_WorksAndPreservesOutput()
        {
            var littleEndian = CreateTestDicomFile(32, 32, 16, 12, "MONOCHROME2");
            var transcoder = new DicomTranscoder(
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ExplicitVRBigEndian);
            var bigEndian = transcoder.Transcode(littleEndian);
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 2, Y = 2, Width = 8, Height = 8 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 12, Y = 12, Text = "L", FontSize = 20 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(bigEndian, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
            Assert.IsTrue(modifiedFile.Dataset.Contains(DicomTag.PixelData));
        }

        /// <summary>
        /// 測試沒有 PixelData 的 DICOM（例如 SR）應回傳失敗
        /// </summary>
        [TestMethod]
        public void CorrectMarkers_NoPixelData_ReturnsFail()
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.88.11"); // SR
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
#pragma warning restore CS0618
            var dcmFile = new DicomFile(dataset);

            var request = new CorrectLRMarkerRequest
            {
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion> { new() { X = 0, Y = 0, Width = 10, Height = 10 } }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsFalse(response.Success);
            Assert.IsTrue(response.Message.Contains("pixel data"));
            Assert.IsNull(modifiedFile);
        }

        /// <summary>
        /// 測試彩色影像 (SamplesPerPixel=3) 應回傳失敗
        /// </summary>
        [TestMethod]
        public void CorrectMarkers_ColorImage_ReturnsFail()
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)64);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)64);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
            dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
            dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.7");
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
#pragma warning restore CS0618

            byte[] pixelBuffer = new byte[64 * 64 * 3];
            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.AddFrame(new MemoryByteBuffer(pixelBuffer));
            var dcmFile = new DicomFile(dataset);

            var request = new CorrectLRMarkerRequest
            {
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion> { new() { X = 0, Y = 0, Width = 10, Height = 10 } }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsFalse(response.Success);
            Assert.IsTrue(response.Message.Contains("SamplesPerPixel"));
            Assert.IsNull(modifiedFile);
        }

        /// <summary>
        /// 測試多 frame 影像：只修改 frame 0，其他 frame 保留
        /// </summary>
        [TestMethod]
        public void CorrectMarkers_MultiFrame_PreservesOtherFrames()
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)4);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)4);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
            dataset.AddOrUpdate(DicomTag.NumberOfFrames, "3");
            dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.7");
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
#pragma warning restore CS0618

            // 3 frames: frame0=100, frame1=150, frame2=200
            var pixelData = DicomPixelData.Create(dataset, true);
            byte[] f0 = new byte[16]; System.Array.Fill<byte>(f0, 100);
            byte[] f1 = new byte[16]; System.Array.Fill<byte>(f1, 150);
            byte[] f2 = new byte[16]; System.Array.Fill<byte>(f2, 200);
            pixelData.AddFrame(new MemoryByteBuffer(f0));
            pixelData.AddFrame(new MemoryByteBuffer(f1));
            pixelData.AddFrame(new MemoryByteBuffer(f2));

            var dcmFile = new DicomFile(dataset);

            var request = new CorrectLRMarkerRequest
            {
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion> { new() { X = 0, Y = 0, Width = 2, Height = 2 } },
                GenerateNewSopInstanceUid = false
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);

            var resultPixelData = DicomPixelData.Create(modifiedFile.Dataset);
            Assert.AreEqual(3, resultPixelData.NumberOfFrames, "All 3 frames should be preserved");

            // Frame 1 and 2 should be untouched
            var resultF1 = resultPixelData.GetFrame(1);
            Assert.AreEqual(150, resultF1.Data[0], "Frame 1 should be preserved");
            var resultF2 = resultPixelData.GetFrame(2);
            Assert.AreEqual(200, resultF2.Data[0], "Frame 2 should be preserved");

            // Frame 0 pixel (0,0) should be 0 (background, MONOCHROME2)
            var resultF0 = resultPixelData.GetFrame(0);
            Assert.AreEqual(0, resultF0.Data[0], "Frame 0 covered pixel should be background");
        }

        /// <summary>
        /// 測試 BitsAllocated=32 應回傳失敗
        /// </summary>
        [TestMethod]
        public void CorrectMarkers_32BitAllocated_ReturnsFail()
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)4);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)4);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)32);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)32);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)31);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)1);
            dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
            dataset.AddOrUpdate(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.7");
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
#pragma warning restore CS0618

            byte[] pixelBuffer = new byte[4 * 4 * 4]; // 32-bit = 4 bytes per pixel
            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.AddFrame(new MemoryByteBuffer(pixelBuffer));
            var dcmFile = new DicomFile(dataset);

            var request = new CorrectLRMarkerRequest
            {
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion> { new() { X = 0, Y = 0, Width = 2, Height = 2 } }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsFalse(response.Success);
            Assert.IsTrue(response.Message.Contains("BitsAllocated"));
            Assert.IsNull(modifiedFile);
        }
    }
}
