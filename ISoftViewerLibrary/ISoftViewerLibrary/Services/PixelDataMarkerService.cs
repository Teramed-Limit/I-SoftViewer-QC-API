using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using Efferent.Native.Codec;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DicomOperators;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Services
{
    /// <summary>
    /// L/R 標記修正服務：讀取 DICOM 影像，修改 pixel data，保留原始 bit depth 與 transfer syntax
    /// </summary>
    public class PixelDataMarkerService
    {
        private const string OrgRootUid = "1.3.6.1.4.1.54514";
        private static long _uidCounter;

        private static readonly object _initLock = new();
        private static bool _transcoderInitialized;

        /// <summary>
        /// 確保 NativeTranscoderManager 已註冊（支援 JPEG 2000、RLE 等壓縮格式）
        /// </summary>
        private static void EnsureTranscoderInitialized()
        {
            if (_transcoderInitialized) return;
            lock (_initLock)
            {
                if (_transcoderInitialized) return;
                try
                {
                    TranscoderManager.SetImplementation(new NativeTranscoderManager());
                    _transcoderInitialized = true;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("Failed to initialize NativeTranscoderManager: {Error}", ex.Message);
                    _transcoderInitialized = true; // 避免重複嘗試
                }
            }
        }

        /// <summary>
        /// 完整流程：修改並回傳修改後的 DicomFile
        /// </summary>
        public (CorrectLRMarkerResponse response, DicomFile? modifiedFile) CorrectMarkersAndGetFile(
            DicomFile dcmFile, CorrectLRMarkerRequest request)
        {
            try
            {
                var dataset = dcmFile.Dataset;

                // 0. 驗證：必須有 PixelData
                if (!dataset.Contains(DicomTag.PixelData))
                {
                    return (new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = "DICOM file does not contain pixel data"
                    }, null);
                }

                // 0b. 驗證：只支援 grayscale (SamplesPerPixel=1)
                //     彩色影像的 PlanarConfiguration 和多通道處理超出此功能範圍
#pragma warning disable CS0618
                int samplesPerPixel = dataset.Contains(DicomTag.SamplesPerPixel)
                    ? dataset.Get<int>(DicomTag.SamplesPerPixel) : 1;
#pragma warning restore CS0618
                if (samplesPerPixel != 1)
                {
                    return (new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = $"Only grayscale images (SamplesPerPixel=1) are supported, got SamplesPerPixel={samplesPerPixel}"
                    }, null);
                }

                var originalSyntax = dcmFile.FileMetaInfo.TransferSyntax;
                bool isCompressed = originalSyntax.IsEncapsulated;
                bool isBigEndian = originalSyntax == DicomTransferSyntax.ExplicitVRBigEndian;
                bool needsTranscode = isCompressed || isBigEndian;

                // 1. 如果是壓縮格式或 Big Endian，先轉成 Explicit VR Little Endian
                DicomFile workingFile = dcmFile;
                if (needsTranscode)
                {
                    EnsureTranscoderInitialized();
                    workingFile = dcmFile.Clone(DicomTransferSyntax.ExplicitVRLittleEndian);
                    dataset = workingFile.Dataset;
                }

                // 2. 讀取 pixel 資訊
                var pixelInfo = DicomPixelInfo.FromDataset(dataset);

                // 2b. 驗證 BitsAllocated 支援範圍
                if (pixelInfo.BitsAllocated != 8 && pixelInfo.BitsAllocated != 16)
                {
                    return (new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = $"Unsupported BitsAllocated={pixelInfo.BitsAllocated}, only 8 and 16 are supported"
                    }, null);
                }

                // 3. 取得 pixel buffer — 保留所有 frame，只修改第一個
                var pixelData = DicomPixelData.Create(dataset);
                int totalFrames = pixelData.NumberOfFrames;

                var frameData = pixelData.GetFrame(0);
                byte[] buffer = new byte[frameData.Size];
                Array.Copy(frameData.Data, buffer, buffer.Length);

                // 保留其他 frame 的資料
                List<byte[]> otherFrames = new();
                for (int f = 1; f < totalFrames; f++)
                {
                    var otherFrame = pixelData.GetFrame(f);
                    byte[] otherBuffer = new byte[otherFrame.Size];
                    Array.Copy(otherFrame.Data, otherBuffer, otherBuffer.Length);
                    otherFrames.Add(otherBuffer);
                }

                // 4. 建立 buffer 操作器
                var bufferOp = new PixelBufferOperator(buffer, pixelInfo);

                // 5. 填充遮蓋區域
                if (request.CoverRegions != null)
                {
                    foreach (var region in request.CoverRegions)
                    {
                        bufferOp.FillRegion(region.X, region.Y, region.Width, region.Height);
                    }
                }

                // 6. 繪製新標記
                if (request.NewMarkers != null)
                {
                    foreach (var marker in request.NewMarkers)
                    {
                        if (string.IsNullOrEmpty(marker.Text))
                            continue;
                        if (marker.FontSize <= 0)
                            marker.FontSize = 24;

                        var (mask, maskWidth, maskHeight) =
                            TextMaskRenderer.RenderMask(marker.Text, marker.FontSize);
                        bufferOp.ApplyMask(mask, maskWidth, maskHeight, marker.X, marker.Y);
                    }
                }

                // 7. 將修改後的 buffer 寫回 pixel data（保留所有 frame）
                dataset.Remove(DicomTag.PixelData);
                var newPixelData = DicomPixelData.Create(dataset, true);
                newPixelData.AddFrame(new MemoryByteBuffer(buffer));
                foreach (var otherFrame in otherFrames)
                {
                    newPixelData.AddFrame(new MemoryByteBuffer(otherFrame));
                }

                // 8. 如果原本是壓縮或 Big Endian，還原回原始 Transfer Syntax
                if (needsTranscode)
                {
                    try
                    {
                        workingFile = workingFile.Clone(originalSyntax);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Warning("Cannot transcode back to {Syntax}, keeping ExplicitVRLittleEndian: {Error}",
                            originalSyntax, ex.Message);
                    }
                }

                // 9. 更新修改紀錄 Tags
                UpdateModificationTags(workingFile, request, pixelInfo);

                // 10. 處理 SOP Instance UID
                string newSopInstanceUid = null;
                if (request.GenerateNewSopInstanceUid)
                {
                    var newDataset = workingFile.Dataset;
                    newSopInstanceUid = GenerateUidWithRoot(OrgRootUid);
#pragma warning disable CS0618
                    newDataset.AddOrUpdate(DicomTag.SOPInstanceUID, newSopInstanceUid);
#pragma warning restore CS0618
                    workingFile.FileMetaInfo.MediaStorageSOPInstanceUID =
                        new DicomUID(newSopInstanceUid, "SOP Instance UID", DicomUidType.SOPInstance);
                }

                return (new CorrectLRMarkerResponse
                {
                    Success = true,
                    Message = "L/R marker corrected successfully",
                    NewSopInstanceUid = newSopInstanceUid
                }, workingFile);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to correct L/R markers");
                return (new CorrectLRMarkerResponse
                {
                    Success = false,
                    Message = $"Failed to correct markers: {ex.Message}"
                }, null);
            }
        }

        private void UpdateModificationTags(DicomFile dcmFile, CorrectLRMarkerRequest request,
            DicomPixelInfo pixelInfo)
        {
            var dataset = dcmFile.Dataset;
#pragma warning disable CS0618
            dataset.AddOrUpdate(DicomTag.DerivationDescription,
                "Pixel data modified: L/R marker corrected");

            var derivationCodeItem = new DicomDataset
            {
                { DicomTag.CodeValue, "113040" },
                { DicomTag.CodingSchemeDesignator, "DCM" },
                { DicomTag.CodeMeaning, "Image Processing Operation" }
            };
            dataset.AddOrUpdate(new DicomSequence(DicomTag.DerivationCodeSequence, derivationCodeItem));

            string comment = $"L/R marker corrected on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            dataset.AddOrUpdate(DicomTag.ImageComments, comment);

            string imageType = dataset.Contains(DicomTag.ImageType)
                ? dataset.Get<string>(DicomTag.ImageType) : "DERIVED\\PRIMARY";
            if (!imageType.Contains("DERIVED"))
            {
                imageType = "DERIVED\\" + imageType;
                dataset.AddOrUpdate(DicomTag.ImageType, imageType);
            }

            // 更新 Window Center/Level：確保前景/背景極值在窗口內可見
            // 如果原始 WC/WL 窗口太窄，新寫入的 0 或 MaxPixelValue 在 viewer 上會看不到
            UpdateWindowLevel(dataset, pixelInfo);
#pragma warning restore CS0618
        }

        /// <summary>
        /// 擴展 Window Center/Width 以確保修改後的前景/背景像素值在窗口範圍內可見
        /// </summary>
        private void UpdateWindowLevel(DicomDataset dataset, DicomPixelInfo pixelInfo)
        {
#pragma warning disable CS0618
            if (!dataset.Contains(DicomTag.WindowCenter) || !dataset.Contains(DicomTag.WindowWidth))
                return;

            // 計算實際寫入的像素值範圍（考慮 BitShift）
            double bgRaw = pixelInfo.BackgroundValue >> pixelInfo.BitShift;
            double fgRaw = pixelInfo.ForegroundValue >> pixelInfo.BitShift;
            if (pixelInfo.IsSigned)
            {
                bgRaw = (short)pixelInfo.BackgroundValue >> pixelInfo.BitShift;
                fgRaw = (short)pixelInfo.ForegroundValue >> pixelInfo.BitShift;
            }
            double minNeeded = Math.Min(bgRaw, fgRaw);
            double maxNeeded = Math.Max(bgRaw, fgRaw);

            try
            {
                double wc = dataset.Get<double>(DicomTag.WindowCenter);
                double ww = dataset.Get<double>(DicomTag.WindowWidth);

                double winMin = wc - ww / 2.0;
                double winMax = wc + ww / 2.0;

                // 如果當前窗口已經包含前景/背景值範圍，不需要修改
                if (winMin <= minNeeded && winMax >= maxNeeded)
                    return;

                // 擴展窗口以包含前景/背景值
                double newMin = Math.Min(winMin, minNeeded);
                double newMax = Math.Max(winMax, maxNeeded);
                double newWw = newMax - newMin;
                double newWc = newMin + newWw / 2.0;

                dataset.AddOrUpdate(DicomTag.WindowCenter, newWc.ToString("F1"));
                dataset.AddOrUpdate(DicomTag.WindowWidth, newWw.ToString("F1"));

                Serilog.Log.Information(
                    "Window Level updated: WC {OldWC}→{NewWC}, WW {OldWW}→{NewWW}",
                    wc, newWc, ww, newWw);
            }
            catch (Exception ex)
            {
                // WC/WL 可能有多值（多 frame）或格式異常，不影響主流程
                Serilog.Log.Warning("Cannot update Window Center/Width: {Error}", ex.Message);
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// 以組織 Root UID 為前綴，產生唯一的 DICOM UID。
        /// 格式：{rootUid}.{timestamp}.{counter}
        /// </summary>
        private static string GenerateUidWithRoot(string rootUid)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long counter = Interlocked.Increment(ref _uidCounter);

            // DICOM UID 最長 64 字元
            string uid = $"{rootUid}.{timestamp}.{counter}";
            if (uid.Length > 64)
            {
                uid = uid[..64];
            }

            return uid;
        }
    }
}
