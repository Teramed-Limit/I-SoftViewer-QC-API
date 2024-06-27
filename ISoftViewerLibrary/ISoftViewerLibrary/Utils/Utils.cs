using System;
using System.IO;
using System.Linq;

namespace ISoftViewerLibrary.Utils;

public static class UidUtils
{
    public static string GenerateStudyInstanceUID()
    {
        string timespan = DateTime.Now.ToString("fff");
        Random rnd = new Random();
        int randomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
        return "1.3.6.1.4.1.54514." + DateTime.Now.ToString("yyyyMMddhhmmss") + ".1." + Convert.ToString(randomValue) +
               ".1" + timespan;
    }

    public static string GenerateSeriesInstanceUID(string studyInstanceUid, int seriesIdx)
    {
        return studyInstanceUid + "." + Convert.ToString(seriesIdx + 1);
    }

    public static string GenerateSopInstanceUID(string seriesInstanceUid, int instanceNumber)
    {
        return seriesInstanceUid + "." + Convert.ToString(instanceNumber + 1);
    }
}