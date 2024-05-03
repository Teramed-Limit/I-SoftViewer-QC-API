namespace ISoftViewerLibrary.Models.DTOs
{
    public class DicomOperationNodes : JsonDatasetBase
    {
        public string Name { get; set; }

        public string OperationType { get; set; }

        public string AETitle { get; set; }

        public string RemoteAETitle { get; set; }
        
        public string IPAddress { get; set; }

        public int Port { get; set; }

        public string MoveAETitle { get; set; }

        public string Description { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public int Enable { get; set; }
        
        public string CFindReqField { get; set; }
        
        public string MappingField { get; set; }
    }
}