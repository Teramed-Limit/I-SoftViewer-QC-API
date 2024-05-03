using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Utils
{
    public class ZipArchiver
    {
        private MemoryStream ZipFileMemoryStream { get; set; }

        public async Task<MemoryStream> Zip(IEnumerable<string> filePaths)
        {
            ZipFileMemoryStream = new MemoryStream();
            using (var archive = new ZipArchive(ZipFileMemoryStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                foreach (var filePath in filePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    var entry = archive.CreateEntry(fileName);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(filePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            ZipFileMemoryStream.Seek(0, SeekOrigin.Begin);
            return ZipFileMemoryStream;
        }
    }
}