using System.Collections.Generic;
using System.Xml.Serialization;

// 定義 MatchKey 類別
public class MatchKey
{
    [XmlAttribute("FieldName")] public string FieldName { get; set; }

    [XmlAttribute("DicomTag")] public string DicomTag { get; set; }

    [XmlAttribute("Format")] public string Format { get; set; }

    [XmlAttribute("Value")] public string Value { get; set; }
}

// 定義 WorklistMatchKeys 類別，包含一個 MatchKey 列表
[XmlRoot("WorklistMatchKeys")]
public class WorklistMatchKeys
{
    [XmlElement("MatchKey")] public List<MatchKey> MatchKeys { get; set; }
}