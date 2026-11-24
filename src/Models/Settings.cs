using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    public class Settings
    {
        public const string AppVersion = "1.2.0";
        public static Settings Current { get; set; }

        // General
        public bool RunAtStartup { get; set; } = false;
        public bool ShowTrayIcon { get; set; } = true;
        public bool ShowGroupsInTray { get; set; } = true;

        // Appearance
        public string Theme { get; set; } = "System"; // System | Light | Dark
        public string PopupTheme { get; set; } = "Windows mode"; // Windows mode | Dark Acrylic | Light Glass
        public bool AccentColorBackground { get; set; } = false;

        // Animation
        public bool AnimateWindow { get; set; } = true;
        public bool AnimateContent { get; set; } = true;

        // Others
        public bool EnableLaunchAll { get; set; } = true;
        public bool GrayscaleIcon { get; set; } = false;

        // About / Updates
        public bool CheckUpdates { get; set; } = false;
        public int LaunchCount { get; set; } = 0;
        public DateTime? LastSupportPromptUtc { get; set; } = null;

        public ObservableCollection<GroupData> Groups { get; set; } = new ObservableCollection<GroupData>();
    }

    /// <summary>JSON persistence, like AppGroup's appgroups.json in the user's config folder.</summary>
    public static class Store
    {
        public static readonly string Dir =
            Environment.GetEnvironmentVariable("XAPPGROUP_DATA") ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XAppGroup");
        public static readonly string File_ = Path.Combine(Dir, "appgroups.json");
        public static readonly string ShortcutDir = Path.Combine(Dir, "Shortcuts");
        public static readonly string IconDir = Path.Combine(Dir, "Icons");
        static readonly JsonSerializerOptions Opts = new JsonSerializerOptions { WriteIndented = true };

        public static Settings Load()
        {
            try
            {
                if (File.Exists(File_))
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(File_)) ?? new Settings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Store] Load failed from '{File_}': {ex.Message}");
            }
            return new Settings();
        }

        public static void Save(Settings s)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(File_, JsonSerializer.Serialize(s, Opts));
        }
    }
}
