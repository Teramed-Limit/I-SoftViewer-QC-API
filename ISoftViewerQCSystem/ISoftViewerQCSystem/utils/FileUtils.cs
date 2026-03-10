using System.IO;

namespace ISoftViewerQCSystem.utils
{
    public class FileUtils
    {
        public static string ConvertToWebPath(string filePath, string ext)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            var directory = Path.GetDirectoryName(filePath) ?? "";
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(filePath) + ext)
                .Replace('\\', '/');
        }
    }
}