using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Log = Serilog.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LRMarkerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomOperationNodeService _dicomOperationNodeService;
        private readonly IDcmUnitOfWork _netUnitOfWork;
        private readonly IDcmRepository _dcmRepository;

        private static readonly ConcurrentDictionary<string, FileLockEntry> _fileLocks = new();
        private static readonly object _fileLockRefSync = new();

        private sealed class FileLockEntry
        {
            public object SyncRoot { get; } = new();
            public int RefCount { get; set; }
        }

        public LRMarkerController(
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

        [HttpPost("correct")]
        [HttpPost("/api/dicom/correct-lr-marker")]
        public ActionResult<CorrectLRMarkerResponse> CorrectLRMarker([FromBody] CorrectLRMarkerRequest request)
        {
            FileLockEntry fileLockEntry = null;
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.SopInstanceUid))
                {
                    return BadRequest(new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = "SOP Instance UID is required"
                    });
                }

                if (request.CoverRegions == null || request.CoverRegions.Count == 0)
                {
                    return BadRequest(new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = "At least one cover region is required"
                    });
                }

                var where = new List<PairDatas>
                {
                    new() { Name = "SOPInstanceUID", Value = request.SopInstanceUid }
                };
                var dcmFileList = _dicomImagePathService.Get(where).ToList();

                if (!dcmFileList.Any())
                {
                    return NotFound(new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = $"DICOM file not found for SOP Instance UID: {request.SopInstanceUid}"
                    });
                }

                string dcmFilePath = dcmFileList.First().ImageFullPath;
                if (string.IsNullOrEmpty(dcmFilePath) || !System.IO.File.Exists(dcmFilePath))
                {
                    return NotFound(new CorrectLRMarkerResponse
                    {
                        Success = false,
                        Message = $"DICOM file does not exist at path: {dcmFilePath}"
                    });
                }

                fileLockEntry = AcquireFileLock(request.SopInstanceUid);
                lock (fileLockEntry.SyncRoot)
                {
                    var dcmFile = DicomFile.Open(dcmFilePath, FileReadOption.ReadAll);

                    if (!ValidateUidConsistency(dcmFile.Dataset, request, out string uidMismatchMessage))
                    {
                        return BadRequest(new CorrectLRMarkerResponse
                        {
                            Success = false,
                            Message = uidMismatchMessage
                        });
                    }

                    var service = new PixelDataMarkerService();
                    var (response, modifiedFile) = service.CorrectMarkersAndGetFile(dcmFile, request);

                    if (!response.Success || modifiedFile == null)
                    {
                        return BadRequest(response);
                    }

                    if (request.SendToPacs)
                    {
                        if (!TrySendToPacs(modifiedFile.Dataset, request.CStoreNodeName, out string cstoreMessage))
                        {
                            return StatusCode(502, new CorrectLRMarkerResponse
                            {
                                Success = false,
                                Message = $"L/R marker corrected but failed to send to PACS: {cstoreMessage}"
                            });
                        }

                        response.SentToPacs = true;
                    }

                    string backupPath = dcmFilePath + ".bak";
                    System.IO.File.Copy(dcmFilePath, backupPath, overwrite: true);

                    string tempPath = dcmFilePath + ".tmp";
                    try
                    {
                        modifiedFile.Save(tempPath);
                        System.IO.File.Delete(dcmFilePath);
                        System.IO.File.Move(tempPath, dcmFilePath);

                        if (System.IO.File.Exists(backupPath))
                        {
                            System.IO.File.Delete(backupPath);
                        }
                    }
                    catch
                    {
                        if (System.IO.File.Exists(backupPath) && !System.IO.File.Exists(dcmFilePath))
                        {
                            System.IO.File.Move(backupPath, dcmFilePath);
                        }

                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }

                        throw;
                    }

                    Log.Information("L/R marker corrected for SOP Instance UID: {SopInstanceUid}", request.SopInstanceUid);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error correcting L/R marker for SOP Instance UID: {SopInstanceUid}", request?.SopInstanceUid);
                return StatusCode(500, new CorrectLRMarkerResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
            finally
            {
                if (fileLockEntry != null && request != null && !string.IsNullOrWhiteSpace(request.SopInstanceUid))
                {
                    ReleaseFileLock(request.SopInstanceUid, fileLockEntry);
                }
            }
        }

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
    }
}
