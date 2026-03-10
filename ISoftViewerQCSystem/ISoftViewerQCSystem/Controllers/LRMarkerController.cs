using ISoftViewerLibrary.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ISoftViewerQCSystem.Services;
using Log = Serilog.Log;
using System;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LRMarkerController : ControllerBase
    {
        private readonly LRMarkerCorrectionService _service;

        public LRMarkerController(LRMarkerCorrectionService service)
        {
            _service = service;
        }

        [HttpPost("correct")]
        [HttpPost("/api/dicom/correct-lr-marker")]
        public ActionResult<CorrectLRMarkerResponse> CorrectLRMarker([FromBody] CorrectLRMarkerRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SopInstanceUid))
            {
                return BadRequest(new CorrectLRMarkerResponse
                {
                    Success = false,
                    Message = "SOP Instance UID is required"
                });
            }

            try
            {
                var result = _service.CorrectMarker(request);
                return ToActionResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error correcting L/R marker for SOP Instance UID: {SopInstanceUid}", request.SopInstanceUid);
                return StatusCode(500, new CorrectLRMarkerResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        [HttpPost("restore")]
        [HttpPost("/api/dicom/restore-lr-marker")]
        public ActionResult<RestoreLRMarkerResponse> RestoreLRMarker([FromBody] RestoreLRMarkerRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SopInstanceUid))
            {
                return BadRequest(new RestoreLRMarkerResponse
                {
                    Success = false,
                    Message = "SOP Instance UID is required"
                });
            }

            try
            {
                var result = _service.RestoreMarker(request);
                return ToActionResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error restoring original image for SOP Instance UID: {SopInstanceUid}", request.SopInstanceUid);
                return StatusCode(500, new RestoreLRMarkerResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        private ActionResult<T> ToActionResult<T>(LRMarkerResult<T> result) where T : class
        {
            return result.ErrorType switch
            {
                LRMarkerErrorType.None => Ok(result.Data),
                LRMarkerErrorType.NotFound => NotFound(result.Data),
                LRMarkerErrorType.ValidationError => BadRequest(result.Data),
                LRMarkerErrorType.PacsError => StatusCode(502, result.Data),
                _ => StatusCode(500, result.Data),
            };
        }
    }
}
