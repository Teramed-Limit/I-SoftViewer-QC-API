using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ISoftViewerLibrary.Utils
{
    public static class Environment
    {
        private static void LoadAppSettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                // 轉成Dictionary
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            else
            {
                throw new FileNotFoundException($"Configuration file '{path}' not found.");
            }
        }
    }
}