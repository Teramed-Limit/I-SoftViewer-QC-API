using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ISoftViewerQCSystem.Hubs.Dtos
{
    public class Viewport
    {
        [JsonPropertyName("scale")] public double Scale { get; set; }

        [JsonPropertyName("translation")] public Translation Translation { get; set; }

        [JsonPropertyName("voi")] public Voi Voi { get; set; }

        [JsonPropertyName("invert")] public bool Invert { get; set; }

        [JsonPropertyName("pixelReplication")] public bool PixelReplication { get; set; }

        [JsonPropertyName("rotation")] public double Rotation { get; set; }

        [JsonPropertyName("hflip")] public bool Hflip { get; set; }

        [JsonPropertyName("vflip")] public bool Vflip { get; set; }
        
        // [JsonPropertyName("displayedArea")] public bool DisplayedArea { get; set; }

        [JsonPropertyName("labelmap")] public bool Labelmap { get; set; }

        // [JsonPropertyName("colormap")] public LUT Colormap { get; set; }
        
        // [JsonPropertyName("modalityLUT")] public LUT ModalityLUT { get; set; }
        
        // [JsonPropertyName("voiLUT")] public LUT VoiLUT { get; set; }
    }

    public class Translation
    {
        [JsonPropertyName("x")] public double X { get; set; }

        [JsonPropertyName("y")] public double Y { get; set; }
    }

    public class Voi
    {
        [JsonPropertyName("windowWidth")] public double WindowWidth { get; set; }

        [JsonPropertyName("windowCenter")] public double WindowCenter { get; set; }
    }

    public class LUT
    {
        [JsonPropertyName("firstValueMapped")] public int FirstValueMapped { get; set; }
        [JsonPropertyName("numBitsPerEntry")] public int NumBitsPerEntry { get; set; }
        [JsonPropertyName("lut")] public object[] Lut { get; set; }
    }
}