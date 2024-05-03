using System.Collections.Generic;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class DicomTagData
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public string Tag { get; set; }
        public ushort Group { get; set; }
        public ushort Element { get; set; }
        public string Name { get; set; }
        public string VR { get; set; }
        public string Length { get; set; }
        public string Value { get; set; }
    }

    public class EditableDicomTagData : DicomTagData
    {
        public bool Editable { get; set; }
    }

    public class DicomTagImageData
    {
        public List<EditableDicomTagData> DicomTagDatas { get; set; }
        public string ImagePath { get; set; }
    }

    public class ModifyDicomTagData : DicomTagData
    {
        public List<SearchImagePathView> DicomImage  { get; set; }
    }
}