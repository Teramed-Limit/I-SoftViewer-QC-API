namespace ISoftViewerLibrary.Models.DTOs
{
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
    }
}
