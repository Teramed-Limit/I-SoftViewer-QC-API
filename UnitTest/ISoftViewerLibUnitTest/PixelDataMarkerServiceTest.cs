using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using ISoftViewerLibrary.Services;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class PixelDataMarkerServiceTest
    {
        private DicomFile CreateTestDicomFile(int rows, int cols, int bitsAllocated,
            int bitsStored, string photometric, int pixelRepresentation = 0)
        {
            var dataset = new DicomDataset();
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)cols);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)bitsAllocated);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)bitsStored);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)(bitsStored - 1));
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)pixelRepresentation);
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

        /// <summary>
        /// 將 DICOM 檔案壓縮為指定的 Transfer Syntax
        /// </summary>
        private DicomFile CompressTo(DicomFile source, DicomTransferSyntax syntax)
        {
            // 確保 NativeTranscoderManager 已載入
            try
            {
                Efferent.Native.Codec.NativeTranscoderManager mgr = new();
                TranscoderManager.SetImplementation(mgr);
            }
            catch { /* already set */ }

            return source.Clone(syntax);
        }

        #region 基本格式測試

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

        #endregion

        #region 10-bit 格式測試

        [TestMethod]
        public void CorrectMarkers_10Bit_Mono2_Works()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 16, 10, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 10, Y = 10, Width = 20, Height = 20 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 60, Y = 60, Text = "R", FontSize = 20 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);

            // 驗證 cover region 像素被設為背景值 (MONOCHROME2 = 0)
            var pixelData = DicomPixelData.Create(modifiedFile.Dataset);
            var frame = pixelData.GetFrame(0);
            // pixel (10, 10) = index 10*128+10 = 1290, 16-bit = byte offset 2580
            int idx = (10 * 128 + 10) * 2;
            ushort coveredPixel = System.BitConverter.ToUInt16(frame.Data, idx);
            Assert.AreEqual(0, coveredPixel, "Covered pixel should be background (0) for MONOCHROME2");
        }

        [TestMethod]
        public void CorrectMarkers_10Bit_Mono1_Works()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 16, 10, "MONOCHROME1");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 15, Height = 15 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 80, Y = 80, Text = "L", FontSize = 18 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);

            // MONOCHROME1: 背景值 = maxPixelValue (1023 for 10-bit)
            var pixelData = DicomPixelData.Create(modifiedFile.Dataset);
            var frame = pixelData.GetFrame(0);
            int idx = (5 * 128 + 5) * 2;
            ushort coveredPixel = System.BitConverter.ToUInt16(frame.Data, idx);
            Assert.AreEqual(1023, coveredPixel,
                "Covered pixel should be max value (1023) for 10-bit MONOCHROME1");
        }

        #endregion

        #region 壓縮格式測試

        [TestMethod]
        public void CorrectMarkers_JPEG2000Lossless_DecompressAndRecompress()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME2");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.JPEG2000Lossless);

            // 確認已壓縮
            Assert.IsTrue(compressed.FileMetaInfo.TransferSyntax.IsEncapsulated,
                "Source should be encapsulated");

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 20, Height = 20 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 35, Y = 35, Text = "L", FontSize = 14 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
            Assert.IsTrue(modifiedFile.Dataset.Contains(DicomTag.PixelData));
        }

        [TestMethod]
        public void CorrectMarkers_JPEG2000Lossy_DecompressAndRecompress()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME2");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.JPEG2000Lossy);

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
                    new() { X = 30, Y = 30, Text = "R", FontSize = 16 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_RLELossless_DecompressAndRecompress()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME2");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.RLELossless);

            Assert.IsTrue(compressed.FileMetaInfo.TransferSyntax.IsEncapsulated,
                "Source should be RLE encapsulated");

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 10, Y = 10, Width = 15, Height = 15 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 40, Y = 40, Text = "L", FontSize = 12 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_JPEG2000_8Bit_Mono1_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 8, 8, "MONOCHROME1");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.JPEG2000Lossless);

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 0, Y = 0, Width = 20, Height = 20 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "R", FontSize = 14 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_RLE_14Bit_Mono2_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 14, "MONOCHROME2");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.RLELossless);

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 10, Height = 10 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "L", FontSize = 18 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        #endregion

        #region Markers-only 測試（無 Cover Region）

        [TestMethod]
        public void CorrectMarkers_MarkersOnly_NoCoverRegion_Works()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 16, 12, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>(), // 空的
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
        }

        [TestMethod]
        public void CorrectMarkers_MarkersOnly_NullCoverRegions_Works()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 16, 12, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = null, // null
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "R", FontSize = 20 }
                },
                GenerateNewSopInstanceUid = false
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_CoverOnly_NoMarkers_Works()
        {
            var dcmFile = CreateTestDicomFile(128, 128, 16, 12, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 10, Y = 10, Width = 30, Height = 20 }
                },
                NewMarkers = new List<NewMarker>(), // 空的
                GenerateNewSopInstanceUid = false
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        #endregion

        #region Big Endian 測試

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

        #endregion

        #region Signed pixel representation 測試

        [TestMethod]
        public void CorrectMarkers_12Bit_Signed_Mono2_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME2", pixelRepresentation: 1);
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 15, Height = 15 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "L", FontSize = 16 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_14Bit_Signed_Mono1_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 14, "MONOCHROME1", pixelRepresentation: 1);
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
                    new() { X = 30, Y = 30, Text = "R", FontSize = 14 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        #endregion

        #region 壓縮格式 + 不同 bit depth 組合測試

        [TestMethod]
        public void CorrectMarkers_JPEG2000_10Bit_Mono2_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 10, "MONOCHROME2");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.JPEG2000Lossless);

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 5, Y = 5, Width = 10, Height = 10 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 30, Y = 30, Text = "L", FontSize = 14 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_JPEG2000_14Bit_Mono1_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 14, "MONOCHROME1");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.JPEG2000Lossless);

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 0, Y = 0, Width = 20, Height = 20 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 35, Y = 35, Text = "R", FontSize = 16 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        [TestMethod]
        public void CorrectMarkers_RLE_10Bit_Mono1_Works()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 10, "MONOCHROME1");
            var compressed = CompressTo(dcmFile, DicomTransferSyntax.RLELossless);

            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 10, Y = 10, Width = 10, Height = 10 }
                },
                NewMarkers = new List<NewMarker>
                {
                    new() { X = 40, Y = 40, Text = "L", FontSize = 20 }
                }
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(compressed, request);

            Assert.IsTrue(response.Success, response.Message);
            Assert.IsNotNull(modifiedFile);
        }

        #endregion

        #region 錯誤處理測試

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

            byte[] pixelBuffer = new byte[4 * 4 * 4];
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

        #endregion

        #region Multi-frame 測試

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

            var resultF1 = resultPixelData.GetFrame(1);
            Assert.AreEqual(150, resultF1.Data[0], "Frame 1 should be preserved");
            var resultF2 = resultPixelData.GetFrame(2);
            Assert.AreEqual(200, resultF2.Data[0], "Frame 2 should be preserved");

            var resultF0 = resultPixelData.GetFrame(0);
            Assert.AreEqual(0, resultF0.Data[0], "Frame 0 covered pixel should be background");
        }

        #endregion

        #region 像素值驗證測試

        [TestMethod]
        public void CorrectMarkers_12Bit_Mono2_CoverRegionPixelsAreZero()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME2");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 0, Y = 0, Width = 5, Height = 5 }
                },
                NewMarkers = new List<NewMarker>()
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success);

            var pixelData = DicomPixelData.Create(modifiedFile.Dataset);
            var frame = pixelData.GetFrame(0);

            // 驗證 cover region 內所有像素都為 0 (MONOCHROME2 背景)
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    int idx = (y * 64 + x) * 2;
                    ushort pixel = System.BitConverter.ToUInt16(frame.Data, idx);
                    Assert.AreEqual(0, pixel,
                        $"Pixel ({x},{y}) in cover region should be 0 for MONOCHROME2");
                }
            }

            // 驗證 cover region 外的像素仍為原始值
            int outsideIdx = (10 * 64 + 10) * 2;
            ushort outsidePixel = System.BitConverter.ToUInt16(frame.Data, outsideIdx);
            Assert.AreNotEqual(0, outsidePixel, "Pixel outside cover region should be unchanged");
        }

        [TestMethod]
        public void CorrectMarkers_12Bit_Mono1_CoverRegionPixelsAreMaxValue()
        {
            var dcmFile = CreateTestDicomFile(64, 64, 16, 12, "MONOCHROME1");
            var request = new CorrectLRMarkerRequest
            {
                StudyInstanceUid = "1.2.3",
                SeriesInstanceUid = "1.2.3",
                SopInstanceUid = "1.2.3",
                CoverRegions = new List<CoverRegion>
                {
                    new() { X = 0, Y = 0, Width = 5, Height = 5 }
                },
                NewMarkers = new List<NewMarker>()
            };

            var service = new PixelDataMarkerService();
            var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

            Assert.IsTrue(response.Success);

            var pixelData = DicomPixelData.Create(modifiedFile.Dataset);
            var frame = pixelData.GetFrame(0);

            // MONOCHROME1: 背景 = maxPixelValue (4095 for 12-bit)
            int idx = (0 * 64 + 0) * 2;
            ushort pixel = System.BitConverter.ToUInt16(frame.Data, idx);
            Assert.AreEqual(4095, pixel,
                "Covered pixel should be max value (4095) for 12-bit MONOCHROME1");
        }

        #endregion
    }
}
