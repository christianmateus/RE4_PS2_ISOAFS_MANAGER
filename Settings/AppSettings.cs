using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FerramentaAFS.Settings
{
    public sealed class AppSettings
    {
        public string Language { get; set; } = "pt-BR";
        public bool ShowSuccessMessages { get; set; } = true;
        public List<string> RecentFiles { get; set; } = new List<string>();

        static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FerramentaAFS", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
