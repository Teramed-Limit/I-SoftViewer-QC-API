namespace ISoftViewerLibrary.Models.DTOs
{
    public class DicomDestinationNode : JsonDatasetBase
    {
        // Logical name of the DICOM destination node
        public string LogicalName { get; set; }

        // AE title of the DICOM destination node
        public string AETitle { get; set; }

        // Sending AE title of the DICOM destination node
        public string SendingAETitle { get; set; }

        // Host name of the DICOM destination node
        public string HostName { get; set; }

        // IP address of the DICOM destination node
        public string IPAddress { get; set; }

        // Port number of the DICOM destination node
        public int Port { get; set; }

        // Description of the DICOM destination node
        public string Description { get; set; }

        // Routing rule pattern of the DICOM destination node
        public string RoutingRulePattern { get; set; }

        // Creation date and time of the DICOM destination node
        public string CreateDateTime { get; set; }

        // User who created the DICOM destination node
        public string CreateUser { get; set; }

        // Modification date and time of the DICOM destination node
        public string ModifiedDateTime { get; set; }

        // User who modified the DICOM destination node
        public string ModifiedUser { get; set; }
    }
}