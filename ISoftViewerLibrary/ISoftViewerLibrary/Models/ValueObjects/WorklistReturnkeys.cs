using System.Collections.Generic;
using System.Xml.Serialization;

// Define a class for the main XML structure
[XmlRoot("WorklistReturnkeys")]
public class WorklistReturnkeys
{
    // List of ReturnKey items
    [XmlElement("ReturnKey")] public List<ReturnKey> ReturnKeys { get; set; }

    // List of ReturnKeySQ items
    [XmlElement("ReturnKeySQ")] public List<ReturnKeySQ> ReturnKeySQs { get; set; }
}

// Define a class for individual ReturnKey elements
public class ReturnKey
{
    // 沒用，Tag的名稱
    [XmlAttribute("FieldName")] public string FieldName { get; set; }
    // 目標DicomTag，例如"0008,0020"這種形式
    [XmlAttribute("DicomTag")] public string DicomTag { get; set; }
    // 塞值給Dataset，如果是{studyDescription}這種形式，就是會拿參數來塞值，目前只有以下參數
    // PatientID
    // PatientName
    // StudyInstanceUID
    // AccessionNumber
    // StudyDate
    // PerformingPhysicianName
    // StudyDescription
    // Modality
    // RequestedProcedureID
    [XmlAttribute("Value")] public string Value { get; set; }
    // 沒用
    [XmlAttribute("Format")] public string Format { get; set; }
}

// Define a class for ReturnKeySQ elements
public class ReturnKeySQ
{
    [XmlAttribute("FieldName")] public string FieldName { get; set; }

    [XmlAttribute("DicomTag")] public string DicomTag { get; set; }

    [XmlAttribute("Format")] public string Format { get; set; }

    // List of nested ReturnKey items
    [XmlElement("ReturnKey")] public List<ReturnKey> ReturnKeys { get; set; }

    // List of nested ReturnKeySubSQ items
    [XmlElement("ReturnKeySQ")] public List<ReturnKeySQ> ReturnKeySQs { get; set; }
}

// Define a class for ReturnKeySubSQ elements
// public class ReturnKeySubSQ
// {
//     [XmlAttribute("FieldName")] public string FieldName { get; set; }
//
//     [XmlAttribute("DicomTag")] public string DicomTag { get; set; }
//
//     [XmlAttribute("Value")] public string Value { get; set; }
//
//     [XmlAttribute("Format")] public string Format { get; set; }
//
//     // List of nested ReturnKey items
//     [XmlElement("ReturnKey")] public List<ReturnKey> ReturnKeys { get; set; }
// }