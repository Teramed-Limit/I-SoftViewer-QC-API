using System.Collections.Generic;

namespace ISoftViewerLibrary.Models.DTOs.PacsServer
{
    public class SvrConfigurationsV2 : JsonDatasetBase
    {
        public string SysConfigName { get; set; }
        public string Value { get; set; }
    }
}