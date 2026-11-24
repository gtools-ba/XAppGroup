using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace XAppGroup
{
    /// <summary>
    /// External user-editable config file (appconfig.json next to XAppGroup.exe)
    /// for GitHub repository URL, Ko-fi support URL, and Support Us prompt cadence.
    /// </summary>
    public class AppConfig
    {
        public string GitHubUrl { get; set; } = "https://github.com/gtools-ba/XAppGroup";
        public string SupportUrl { get; set; } = "https://ko-fi.com/xgroup";
        public bool SupportPromptEnabled { get; set; } = true;
        public int SupportPromptEveryNLaunches { get; set; } = 5;
        public int SupportPromptCooldownDays { get; set; } = 3;

        private static readonly JsonSerializerOptions Opts = new JsonSerializerOptions { WriteIndented = true };
        private static AppConfig _current;

        public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "appconfig.json");

        public static AppConfig Current => _current ?? (_current = Load());

        public static AppConfig Reload()
        {
            _current = Load();
            return _current;
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
                    if (loaded != null) return loaded;
                }
                else
                {
                    var def = new AppConfig();
                    Save(def);
                    return def;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppConfig] Load error: {ex.Message}");
            }
            return new AppConfig();
        }

        public static void Save(AppConfig cfg)
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(cfg ?? new AppConfig(), Opts));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppConfig] Save error: {ex.Message}");
            }
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url.Trim()) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppConfig] OpenUrl error: {ex.Message}");
            }
        }
    }
}
