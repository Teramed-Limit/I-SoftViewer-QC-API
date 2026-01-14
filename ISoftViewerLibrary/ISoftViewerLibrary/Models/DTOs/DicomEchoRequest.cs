namespace ISoftViewerLibrary.Models.DTOs
{
    public class DicomEchoRequest
    {

        public string AETitle { get; set; }

        public string RemoteAETitle { get; set; }

        public string IPAddress { get; set; }

        public int Port { get; set; }
    }
}