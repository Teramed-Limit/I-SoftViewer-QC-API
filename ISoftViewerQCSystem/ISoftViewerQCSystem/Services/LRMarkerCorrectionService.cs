using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using Microsoft.Extensions.Configuration;
using Log = Serilog.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerQCSystem.Services
{
    /// <summary>
    /// LR Marker 修正與還原的業務邏輯服務。
    /// 負責 DICOM 檔案操作、備份管理、路徑推導、C-Store 發送。
    /// </summary>
    public class LRMarkerCorrectionService
    {
        private readonly IConfiguration _configuration;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomOperationNodeService _dicomOperationNodeService;
        private readonly IDcmUnitOfWork _netUnitOfWork;
        private readonly IDcmRepository _dcmRepository;

        private static readonly ConcurrentDictionary<string, FileLockEntry> _fileLocks = new();
        private static readonly object _fileLockRefSync = new();

        /// <summary>
        /// 記錄每個 Series 已發出的最大 ImageNumber，避免 PACS 尚未寫入 DB 時重複取號。
        /// Key = SeriesInstanceUID, Value = 已發出的最大 ImageNumber。
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _issuedImageNumbers = new();

        private sealed class FileLockEntry
        {
            public object SyncRoot { get; } = new();
            public int RefCount { get; set; }
        }

        public LRMarkerCorrectionService(
            IConfiguration configuration,
            DicomImagePathViewService dicomImagePathService,
            DicomOperationNodeService dicomOperationNodeService,
            IDcmUnitOfWork netUnitOfWork,
            IDcmRepository dcmRepository)
        {
            _configuration = configuration;
            _dicomImagePathService = dicomImagePathService;
            _dicomOperationNodeService = dicomOperationNodeService;
            _netUnitOfWork = netUnitOfWork;
            _dcmRepository = dcmRepository;
        }

        /// <summary>
        /// 執行 LR Marker 修正。
        /// </summary>
        public LRMarkerResult<CorrectLRMarkerResponse> CorrectMarker(CorrectLRMarkerRequest request)
        {
            if (!TryResolveDicomPath(request.SopInstanceUid, out string dcmFilePath, out SearchImagePathView originalRecord, out string pathError))
            {
                return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                    LRMarkerErrorType.NotFound,
                    new CorrectLRMarkerResponse { Success = false, Message = pathError });
            }

            var fileLockEntry = AcquireFileLock(request.SopInstanceUid);
            try
            {
                lock (fileLockEntry.SyncRoot)
                {
                    string originalBackupPath = GetOriginalBackupPath(dcmFilePath);
                    PreserveOriginalIfNeeded(dcmFilePath, originalBackupPath);

                    var dcmFile = DicomFile.Open(dcmFilePath, FileReadOption.ReadAll);

                    if (!ValidateUidConsistency(dcmFile.Dataset, request, out string uidMismatch))
                    {
                        return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                            LRMarkerErrorType.ValidationError,
                            new CorrectLRMarkerResponse { Success = false, Message = uidMismatch });
                    }

                    var pixelService = new PixelDataMarkerService();
                    var (response, modifiedFile) = pixelService.CorrectMarkersAndGetFile(dcmFile, request);

                    if (!response.Success || modifiedFile == null)
                    {
                        return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                            LRMarkerErrorType.ValidationError, response);
                    }

                    if (request.GenerateNewSopInstanceUid)
                    {
                        return HandleNewSopGeneration(request, response, modifiedFile, dcmFilePath, originalRecord);
                    }

                    return HandleInPlaceCorrection(request, response, modifiedFile, dcmFilePath);
                }
            }
            finally
            {
                ReleaseFileLock(request.SopInstanceUid, fileLockEntry);
            }
        }

        /// <summary>
        /// 還原 LR Marker 至原始狀態。
        /// </summary>
        public LRMarkerResult<RestoreLRMarkerResponse> RestoreMarker(RestoreLRMarkerRequest request)
        {
            if (!TryResolveDicomPath(request.SopInstanceUid, out string dcmFilePath, out _, out string pathError))
            {
                return LRMarkerResult<RestoreLRMarkerResponse>.Fail(
                    LRMarkerErrorType.NotFound,
                    new RestoreLRMarkerResponse { Success = false, Message = pathError });
            }

            var fileLockEntry = AcquireFileLock(request.SopInstanceUid);
            try
            {
                lock (fileLockEntry.SyncRoot)
                {
                    string originalBackupPath = GetOriginalBackupPath(dcmFilePath);
                    if (!File.Exists(originalBackupPath))
                    {
                        return LRMarkerResult<RestoreLRMarkerResponse>.Fail(
                            LRMarkerErrorType.NotFound,
                            new RestoreLRMarkerResponse { Success = false, Message = "Original unmodified image backup not found" });
                    }

                    if (!RestoreFromOriginal(dcmFilePath, originalBackupPath, out string restoreError))
                    {
                        return LRMarkerResult<RestoreLRMarkerResponse>.Fail(
                            LRMarkerErrorType.InternalError,
                            new RestoreLRMarkerResponse { Success = false, Message = restoreError });
                    }

                    var response = new RestoreLRMarkerResponse
                    {
                        Success = true,
                        Message = "Image restored from original unmodified backup"
                    };

                    // C-STORE 還原後的影像到本機 PACS
                    var restoredFile = DicomFile.Open(dcmFilePath, FileReadOption.ReadAll);
                    if (!TrySendToPacs(restoredFile.Dataset, null, out string localCstoreMsg))
                    {
                        return LRMarkerResult<RestoreLRMarkerResponse>.Fail(
                            LRMarkerErrorType.PacsError,
                            new RestoreLRMarkerResponse { Success = false, Message = $"Image restored locally but failed to send to local PACS: {localCstoreMsg}" });
                    }

                    if (request.SendToPacs)
                    {
                        if (!TrySendToPacs(restoredFile.Dataset, request.CStoreNodeName, out string extCstoreMsg))
                        {
                            return LRMarkerResult<RestoreLRMarkerResponse>.Fail(
                                LRMarkerErrorType.PacsError,
                                new RestoreLRMarkerResponse { Success = false, Message = $"Image restored but failed to send to external PACS: {extCstoreMsg}" });
                        }

                        response.SentToPacs = true;
                    }

                    return LRMarkerResult<RestoreLRMarkerResponse>.Ok(response);
                }
            }
            finally
            {
                ReleaseFileLock(request.SopInstanceUid, fileLockEntry);
            }
        }

        #region Private — Correction Strategies

        private LRMarkerResult<CorrectLRMarkerResponse> HandleNewSopGeneration(
            CorrectLRMarkerRequest request,
            CorrectLRMarkerResponse response,
            DicomFile modifiedFile,
            string dcmFilePath,
            SearchImagePathView originalRecord)
        {
            // 查詢同 series 下目前最大的 ImageNumber，新影像 = max + 1
            int nextImageNumber = GetNextImageNumber(originalRecord.SeriesInstanceUID);

            // 更新 DICOM InstanceNumber tag
            modifiedFile.Dataset.AddOrUpdate(DicomTag.InstanceNumber, nextImageNumber.ToString());

            // 儲存修正後的影像到與原始影像相同的目錄
            string originalDir = Path.GetDirectoryName(dcmFilePath)!;
            string newFileName = response.NewSopInstanceUid + ".dcm";
            string newFilePath = Path.Combine(originalDir, newFileName);

            modifiedFile.Save(newFilePath);

            // 推導新影像的 HTTP 路徑
            response.NewImagePath = BuildNewImagePathInfo(originalRecord, dcmFilePath, newFileName, response.NewSopInstanceUid, nextImageNumber);

            // C-Store 到本機 PACS 以註冊新影像到 DB
            if (!TrySendToPacs(modifiedFile.Dataset, null, out string localCstoreMsg))
            {
                Log.Warning("C-Store to local PACS failed for new image {NewSopInstanceUid}: {Message}",
                    response.NewSopInstanceUid, localCstoreMsg);
            }

            // 若使用者也勾選了 SendToPacs，額外發送到指定的外部 PACS
            if (request.SendToPacs)
            {
                if (!TrySendToPacs(modifiedFile.Dataset, request.CStoreNodeName, out string extCstoreMsg))
                {
                    return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                        LRMarkerErrorType.PacsError,
                        new CorrectLRMarkerResponse { Success = false, Message = $"New image saved locally but failed to send to external PACS: {extCstoreMsg}" });
                }

                response.SentToPacs = true;
            }

            Log.Information(
                "L/R marker corrected with new SOP Instance UID: {NewSopInstanceUid} (original {OriginalSopInstanceUid} preserved), saved to {NewFilePath}",
                response.NewSopInstanceUid, request.SopInstanceUid, newFilePath);

            return LRMarkerResult<CorrectLRMarkerResponse>.Ok(response);
        }

        private LRMarkerResult<CorrectLRMarkerResponse> HandleInPlaceCorrection(
            CorrectLRMarkerRequest request,
            CorrectLRMarkerResponse response,
            DicomFile modifiedFile,
            string dcmFilePath)
        {
            // 覆蓋原始檔案（保留備份供 Restore 使用）
            SaveFileWithBackup(modifiedFile, dcmFilePath);

            // C-STORE 修正後的影像到本機 PACS
            if (!TrySendToPacs(modifiedFile.Dataset, null, out string localCstoreMsg))
            {
                return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                    LRMarkerErrorType.PacsError,
                    new CorrectLRMarkerResponse { Success = false, Message = $"L/R marker corrected but failed to send to local PACS: {localCstoreMsg}" });
            }

            if (request.SendToPacs)
            {
                if (!TrySendToPacs(modifiedFile.Dataset, request.CStoreNodeName, out string extCstoreMsg))
                {
                    return LRMarkerResult<CorrectLRMarkerResponse>.Fail(
                        LRMarkerErrorType.PacsError,
                        new CorrectLRMarkerResponse { Success = false, Message = $"L/R marker corrected but failed to send to external PACS: {extCstoreMsg}" });
                }

                response.SentToPacs = true;
            }

            Log.Information("L/R marker corrected in-place for SOP Instance UID: {SopInstanceUid}", request.SopInstanceUid);

            return LRMarkerResult<CorrectLRMarkerResponse>.Ok(response);
        }

        #endregion

        #region Private — File Operations

        private static void SaveFileWithBackup(DicomFile modifiedFile, string dcmFilePath)
        {
            string backupPath = dcmFilePath + ".bak";
            File.Copy(dcmFilePath, backupPath, overwrite: true);

            string tempPath = dcmFilePath + ".tmp";
            try
            {
                modifiedFile.Save(tempPath);
                File.Delete(dcmFilePath);
                File.Move(tempPath, dcmFilePath);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch
            {
                if (File.Exists(backupPath) && !File.Exists(dcmFilePath))
                {
                    File.Move(backupPath, dcmFilePath);
                }

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }

        private static string GetOriginalBackupPath(string dcmFilePath)
        {
            return dcmFilePath + ".lrmarker.original";
        }

        private static void PreserveOriginalIfNeeded(string dcmFilePath, string originalBackupPath)
        {
            if (!File.Exists(originalBackupPath))
            {
                File.Copy(dcmFilePath, originalBackupPath, overwrite: false);
            }
        }

        private static bool RestoreFromOriginal(string targetPath, string originalBackupPath, out string message)
        {
            message = string.Empty;
            string backupPath = targetPath + ".restore.bak";
            string tempPath = targetPath + ".restore.tmp";

            try
            {
                File.Copy(targetPath, backupPath, overwrite: true);
                File.Copy(originalBackupPath, tempPath, overwrite: true);

                File.Delete(targetPath);
                File.Move(tempPath, targetPath);

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (File.Exists(backupPath) && !File.Exists(targetPath))
                {
                    File.Move(backupPath, targetPath);
                }

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                message = $"Failed to restore from original backup: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region Private — Path Resolution & URL Building

        private bool TryResolveDicomPath(string sopInstanceUid, out string dcmFilePath, out SearchImagePathView originalRecord, out string errorMessage)
        {
            dcmFilePath = string.Empty;
            originalRecord = null;
            errorMessage = string.Empty;

            var where = new List<PairDatas>
            {
                new() { Name = "SOPInstanceUID", Value = sopInstanceUid }
            };
            var dcmFileList = _dicomImagePathService.Get(where).ToList();

            if (!dcmFileList.Any())
            {
                errorMessage = $"DICOM file not found for SOP Instance UID: {sopInstanceUid}";
                return false;
            }

            originalRecord = dcmFileList.First();
            dcmFilePath = originalRecord.ImageFullPath;
            if (string.IsNullOrEmpty(dcmFilePath) || !File.Exists(dcmFilePath))
            {
                errorMessage = $"DICOM file does not exist at path: {dcmFilePath}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查詢同 Series 下目前最大的 ImageNumber，結合 in-memory 已發出記錄，回傳 max + 1。
        /// 避免 PACS 尚未寫入 DB 時連續請求取到重複號碼。
        /// </summary>
        private int GetNextImageNumber(string seriesInstanceUid)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SeriesInstanceUID", Value = seriesInstanceUid }
            };
            var images = _dicomImagePathService.Get(where).ToList();
            int dbMax = images.Count > 0 ? images.Max(x => x.ImageNumber) : 0;

            int nextNumber = _issuedImageNumbers.AddOrUpdate(
                seriesInstanceUid,
                _ => dbMax + 1,
                (_, memoryMax) => Math.Max(dbMax, memoryMax) + 1);

            return nextNumber;
        }

        private NewImagePathInfo BuildNewImagePathInfo(
            SearchImagePathView originalRecord,
            string dcmFilePath,
            string newFileName,
            string? newSopInstanceUid,
            int imageNumber)
        {
            // 從 ImageFullPath 去掉 StoragePath 前綴得到相對目錄
            string storagePath = originalRecord.StoragePath ?? "";
            string relativePath = dcmFilePath.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase)
                ? dcmFilePath[storagePath.Length..]
                : dcmFilePath;

            string relativeDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
            string virtualFilePath = _configuration.GetSection("VirtualFilePath").Value ?? "";

            string fullHttpPath = string.IsNullOrEmpty(relativeDir)
                ? virtualFilePath.TrimEnd('/') + "/" + newFileName
                : virtualFilePath.TrimEnd('/') + "/" + relativeDir.Trim('/') + "/" + newFileName;

            return new NewImagePathInfo
            {
                SopInstanceUID = newSopInstanceUid ?? "",
                SopClassUID = originalRecord.SOPClassUID ?? "",
                ImageNumber = imageNumber.ToString(),
                ImageDate = originalRecord.ImageDate ?? "",
                ImageTime = originalRecord.ImageTime ?? "",
                FilePath = newFileName,
                StorageDeviceID = originalRecord.StorageDeviceID ?? "",
                ImageStatus = originalRecord.ImageStatus ?? "",
                PatientId = originalRecord.PatientId ?? "",
                PatientsName = originalRecord.PatientsName ?? "",
                StudyInstanceUID = originalRecord.StudyInstanceUID ?? "",
                StudyDate = originalRecord.StudyDate ?? "",
                StudyTime = originalRecord.StudyTime ?? "",
                AccessionNumber = originalRecord.AccessionNumber ?? "",
                StudyDescription = originalRecord.StudyDescription ?? "",
                SeriesModality = originalRecord.SeriesModality ?? "",
                BodyPartExamined = originalRecord.BodyPartExamined,
                PatientPosition = originalRecord.PatientPosition ?? "",
                StoragePath = originalRecord.StoragePath ?? "",
                HttpFilePath = fullHttpPath,
                StorageDescription = originalRecord.StorageDescription ?? "",
                SeriesInstanceUID = originalRecord.SeriesInstanceUID ?? "",
                Annotations = "[]",
                KeyImage = false,
            };
        }

        #endregion

        #region Private — UID Validation

        private static bool ValidateUidConsistency(DicomDataset dataset, CorrectLRMarkerRequest request, out string message)
        {
            message = string.Empty;
#pragma warning disable CS0618
            if (!string.IsNullOrWhiteSpace(request.StudyInstanceUid))
            {
                string studyUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                if (!string.Equals(studyUid, request.StudyInstanceUid, StringComparison.Ordinal))
                {
                    message = $"StudyInstanceUid mismatch. Request={request.StudyInstanceUid}, Dataset={studyUid}";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SeriesInstanceUid))
            {
                string seriesUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
                if (!string.Equals(seriesUid, request.SeriesInstanceUid, StringComparison.Ordinal))
                {
                    message = $"SeriesInstanceUid mismatch. Request={request.SeriesInstanceUid}, Dataset={seriesUid}";
                    return false;
                }
            }
#pragma warning restore CS0618

            return true;
        }

        #endregion

        #region Private — PACS Communication

        private bool TrySendToPacs(DicomDataset dataset, string? cStoreNodeName, out string message)
        {
            message = string.Empty;
            try
            {
                DicomOperationNodes node = string.IsNullOrWhiteSpace(cStoreNodeName)
                    ? _dicomOperationNodeService.GetLocalCStoreNode()
                    : _dicomOperationNodeService.GetOperationNode("C-STORE", cStoreNodeName);

                _dcmRepository.DicomDatasets.Add(dataset);
                _netUnitOfWork.RegisterRepository(_dcmRepository);
                _netUnitOfWork.Begin(
                    node.IPAddress,
                    node.Port,
                    node.AETitle,
                    node.RemoteAETitle,
                    DcmServiceUserType.dsutStore,
                    null
                );

                bool success = _netUnitOfWork.Commit().GetAwaiter().GetResult();
                if (!success)
                {
                    message = _netUnitOfWork.Message;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        #endregion

        #region Private — File Locking

        private static FileLockEntry AcquireFileLock(string sopInstanceUid)
        {
            lock (_fileLockRefSync)
            {
                var entry = _fileLocks.GetOrAdd(sopInstanceUid, _ => new FileLockEntry());
                entry.RefCount++;
                return entry;
            }
        }

        private static void ReleaseFileLock(string sopInstanceUid, FileLockEntry entry)
        {
            lock (_fileLockRefSync)
            {
                entry.RefCount--;
                if (entry.RefCount <= 0)
                {
                    _fileLocks.TryRemove(new KeyValuePair<string, FileLockEntry>(sopInstanceUid, entry));
                }
            }
        }

        #endregion
    }
}
