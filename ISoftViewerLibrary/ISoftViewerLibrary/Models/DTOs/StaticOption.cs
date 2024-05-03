namespace ISoftViewerLibrary.Models.DTOs
{
    public class StaticOption : JsonDatasetBase
    {
        public int Id { get; set; }

        public string Label { get; set; }

        public string Value { get; set; }

        public string Type { get; set; }

        public int IsDefault { get; set; }
    }
}
