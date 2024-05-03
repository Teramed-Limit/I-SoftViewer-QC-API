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
    [XmlAttribute("FieldName")] public string FieldName { get; set; }

    [XmlAttribute("DicomTag")] public string DicomTag { get; set; }

    [XmlAttribute("Value")] public string Value { get; set; }

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
    [XmlElement("ReturnKeySQ")] public List<ReturnKeySQ> ReturnKeySubSQs { get; set; }
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