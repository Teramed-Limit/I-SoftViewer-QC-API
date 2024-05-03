using System.ComponentModel.DataAnnotations;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class UserRole : JsonDatasetBase
    {
        [Required]
        public string RoleName { get; set; }

        public string Description { get; set; }
    }

    public class RoleFunction : JsonDatasetBase
    {
        public string RoleName { get; set; }

        public string FunctionName { get; set; }

        public string Description { get; set; }

        public string CorrespondElementId { get; set; }
    }
}
