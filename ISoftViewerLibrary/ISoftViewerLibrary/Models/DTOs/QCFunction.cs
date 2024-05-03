using System.ComponentModel.DataAnnotations;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class QCFunction : JsonDatasetBase
    {
        [Required]
        public string FunctionName { get; set; }

        public string Description { get; set; }

        public string CorrespondElementId { get; set; }
    }
}
