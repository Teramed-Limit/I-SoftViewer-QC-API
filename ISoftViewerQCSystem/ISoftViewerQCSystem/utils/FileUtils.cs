using System.IO;

namespace ISoftViewerQCSystem.utils
{
    public class FileUtils
    {
        public static string ConvertToWebPath(string filePath, string ext)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            return Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ext);
        }
    }
}