namespace ISoftViewerLibrary.Models.DTOs
{
    /// <summary>
    /// LR Marker 操作的錯誤類型，供 Controller 映射為對應的 HTTP 狀態碼。
    /// </summary>
    public enum LRMarkerErrorType
    {
        None,
        NotFound,
        ValidationError,
        PacsError,
        InternalError
    }

    /// <summary>
    /// LR Marker Service 的統一回傳結果，將業務邏輯與 HTTP 層解耦。
    /// </summary>
    public class LRMarkerResult<T> where T : class
    {
        public T? Data { get; init; }
        public LRMarkerErrorType ErrorType { get; init; }

        public bool IsSuccess => ErrorType == LRMarkerErrorType.None;

        public static LRMarkerResult<T> Ok(T data) => new() { Data = data, ErrorType = LRMarkerErrorType.None };
        public static LRMarkerResult<T> Fail(LRMarkerErrorType errorType, T data) => new() { Data = data, ErrorType = errorType };
    }

    public class CorrectLRMarkerRequest
    {
        public string StudyInstanceUid { get; set; }
        public string SeriesInstanceUid { get; set; }
        public string SopInstanceUid { get; set; }
        public List<CoverRegion> CoverRegions { get; set; } = new();
        public List<NewMarker> NewMarkers { get; set; } = new();
        public bool GenerateNewSopInstanceUid { get; set; }
        public bool SendToPacs { get; set; }
        public string? CStoreNodeName { get; set; }
    }

    public class CoverRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class NewMarker
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; } = "L";
        public int FontSize { get; set; } = 24;
    }

    public class CorrectLRMarkerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? NewSopInstanceUid { get; set; }
        public bool SentToPacs { get; set; }

        /// <summary>
        /// 當 GenerateNewSopInstanceUid 為 true 且新影像已寫入 DB 時，
        /// 回傳新影像的完整路徑資訊，讓前端可以直接加入 viewport 而不需重新查詢。
        /// </summary>
        public NewImagePathInfo? NewImagePath { get; set; }
    }

    /// <summary>
    /// 新產生影像的路徑資訊，對應前端 DicomImagePath 介面。
    /// </summary>
    public class NewImagePathInfo
    {
        public string ImageFullPath { get; set; } = "";
        public string SopInstanceUID { get; set; } = "";
        public string SopClassUID { get; set; } = "";
        public string ImageNumber { get; set; } = "";
        public string ImageDate { get; set; } = "";
        public string ImageTime { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string StorageDeviceID { get; set; } = "";
        public string ImageStatus { get; set; } = "";
        public string PatientId { get; set; } = "";
        public string PatientsName { get; set; } = "";
        public string StudyInstanceUID { get; set; } = "";
        public string StudyDate { get; set; } = "";
        public string StudyTime { get; set; } = "";
        public string AccessionNumber { get; set; } = "";
        public string StudyDescription { get; set; } = "";
        public string SeriesModality { get; set; } = "";
        public string? BodyPartExamined { get; set; }
        public string PatientPosition { get; set; } = "";
        public string StoragePath { get; set; } = "";
        public string HttpFilePath { get; set; } = "";
        public string StorageDescription { get; set; } = "";
        public string SeriesInstanceUID { get; set; } = "";
        public string Annotations { get; set; } = "[]";
        public bool KeyImage { get; set; }
    }

    public class RestoreLRMarkerRequest
    {
        public string StudyInstanceUid { get; set; }
        public string SeriesInstanceUid { get; set; }
        public string SopInstanceUid { get; set; }
        public bool SendToPacs { get; set; }
        public string? CStoreNodeName { get; set; }
    }

    public class RestoreLRMarkerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool SentToPacs { get; set; }
    }
}
