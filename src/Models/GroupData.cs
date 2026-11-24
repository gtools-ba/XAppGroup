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
    public class AppPillItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public BitmapSource Icon { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsMissing => !string.IsNullOrEmpty(FullPath) && !File.Exists(FullPath) && !Directory.Exists(FullPath);

        [System.Text.Json.Serialization.JsonIgnore]
        public string ToolTipText
        {
            get
            {
                var displayName = !string.IsNullOrEmpty(Name) ? Name : Path.GetFileNameWithoutExtension(FullPath);
                if (string.IsNullOrEmpty(displayName)) displayName = FullPath ?? "";
                return IsMissing ? displayName + " (Missing file)" : displayName;
            }
        }
    }

    /// <summary>A named group of apps (mirrors AppGroup.GroupData: name, layout, items).</summary>
    public class GroupData : INotifyPropertyChanged
    {
        /// <summary>Fixed number of app icons shown on a home card; the rest become a +N badge.</summary>
        public const int CardVisibleAppCount = 8;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private BitmapSource _cachedIconSource;
        private int _cachedIconPathsHash;
        private string _cachedIconPath;

        private List<BitmapSource> _cachedAppIcons;
        private int _cachedAppIconsHash;

        private List<AppPillItem> _cachedAppPills;
        private int _cachedAppPillsHash;

        private string _id = Guid.NewGuid().ToString("N");
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _groupName = "New Group";
        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        private int _groupCol = 3;
        public int GroupCol
        {
            get => _groupCol;
            set => SetProperty(ref _groupCol, value);
        }

        private bool _showLabels;
        public bool ShowLabels
        {
            get => _showLabels;
            set => SetProperty(ref _showLabels, value);
        }

        private bool _showOnTray;
        public bool ShowOnTray
        {
            get => _showOnTray;
            set => SetProperty(ref _showOnTray, value);
        }

        private string _sortMode = "Manual"; // Manual | Alphabetical
        public string SortMode
        {
            get => _sortMode;
            set => SetProperty(ref _sortMode, value);
        }

        private List<string> _paths = new List<string>();
        public List<string> Paths
        {
            get => _paths;
            set
            {
                if (SetProperty(ref _paths, value))
                {
                    InvalidateIcons();
                }
            }
        }

        private string _iconPath = "";
        /// <summary>Optional custom icon file (.ico/.png). When empty or missing, the generated group icon is used.</summary>
        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (SetProperty(ref _iconPath, value ?? ""))
                {
                    InvalidateIcons();
                }
            }
        }

        /// <summary>Path of the .ico that BuildGroupIcon generates for this group (get-only, not persisted).</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string GeneratedIconPath => Path.Combine(Store.IconDir, Id + ".ico");

        /// <summary>Icon shown in the UI (custom icon if set, else generated). Cached in memory, recomputed only when inputs change.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public BitmapSource IconSource
        {
            get
            {
                int pathsHash = Paths.Count;
                for (int i = 0; i < Math.Min(4, Paths.Count); i++) pathsHash = HashCode.Combine(pathsHash, Paths[i]);
                if (_cachedIconSource == null || _cachedIconPathsHash != pathsHash || _cachedIconPath != IconPath)
                {
                    _cachedIconPathsHash = pathsHash;
                    _cachedIconPath = IconPath;
                    _cachedIconSource = Icons.GroupIcon(this);
                }
                return _cachedIconSource;
            }
        }

        /// <summary>Small per-app icons (up to 6) shown in the main list. Cached in memory, recomputed only when Paths change.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public List<BitmapSource> AppIcons
        {
            get
            {
                int pathsHash = Paths.Count;
                for (int i = 0; i < Math.Min(6, Paths.Count); i++) pathsHash = HashCode.Combine(pathsHash, Paths[i]);
                if (_cachedAppIcons == null || _cachedAppIconsHash != pathsHash)
                {
                    _cachedAppIconsHash = pathsHash;
                    _cachedAppIcons = OrderedPaths().Take(6).Select(p => Icons.Get(p)).ToList();
                }
                return _cachedAppIcons;
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<AppPillItem> AppPills
        {
            get
            {
                int pathsHash = Paths.Count;
                for (int i = 0; i < Math.Min(CardVisibleAppCount, Paths.Count); i++) pathsHash = HashCode.Combine(pathsHash, Paths[i]);
                if (_cachedAppPills == null || _cachedAppPillsHash != pathsHash)
                {
                    _cachedAppPillsHash = pathsHash;
                    _cachedAppPills = OrderedPaths().Take(CardVisibleAppCount).Select(p => new AppPillItem
                    {
                        Name = Path.GetFileNameWithoutExtension(p),
                        FullPath = p,
                        Icon = Icons.Get(p)
                    }).ToList();
                }
                return _cachedAppPills;
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string AppCountBadge => Paths.Count == 1 ? "1 app" : $"{Paths.Count} apps";

        private string _hotkey = "";
        public string Hotkey
        {
            get => _hotkey;
            set
            {
                if (SetProperty(ref _hotkey, value ?? ""))
                    OnPropertyChanged(nameof(HotkeyBadge));
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string HotkeyBadge
        {
            get
            {
                if (!string.IsNullOrEmpty(_hotkey)) return _hotkey;
                return $"Win + Shift + {Math.Max(1, GroupCol)}";
            }
            set => Hotkey = value;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string LayoutBadge => $"{SortMode} · {GroupCol} col{(GroupCol == 1 ? "" : "s")}" + (ShowOnTray ? " · Tray" : "");

        private string _customSummary = "";
        [System.Text.Json.Serialization.JsonIgnore]
        public string CustomSummary
        {
            get => _customSummary;
            set { if (SetProperty(ref _customSummary, value ?? "")) OnPropertyChanged(nameof(SummaryText)); }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string SummaryText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_customSummary)) return _customSummary;
                if (Paths == null || Paths.Count == 0)
                    return "No launchers configured yet — click + Add App or Edit to populate this workspace group.";
                var names = OrderedPaths().Take(5).Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
                return "Primary workspace stack: " + string.Join(", ", names) + (Paths.Count > 5 ? $" & {Paths.Count - 5} more." : ".");
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string OverflowCountText => Paths != null && Paths.Count > CardVisibleAppCount
            ? $"+{Paths.Count - CardVisibleAppCount}" : "";

        int AccentPaletteIndex
        {
            get
            {
                if (GroupName != null)
                {
                    if (GroupName.Contains("1") || GroupName.Contains("Productivity", StringComparison.OrdinalIgnoreCase)) return 0;
                    if (GroupName.Contains("2") || GroupName.Contains("Dev", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (GroupName.Contains("3") || GroupName.Contains("Design", StringComparison.OrdinalIgnoreCase) || GroupName.Contains("Creative", StringComparison.OrdinalIgnoreCase)) return 2;
                }
                return Math.Abs((GroupCol - 1) % 3);
            }
        }

        int ColorIndex
        {
            get
            {
                int hash = 17;
                string key = !string.IsNullOrEmpty(GroupName) ? GroupName : (Id ?? "");
                foreach (char ch in key) hash = hash * 31 + ch;
                return Math.Abs(hash) % 5;
            }
        }

        static readonly string[] LightSquircles = new[]
        {
            "#FEF3C7", // Soft warm amber
            "#E0F2FE", // Soft sky blue
            "#DCFCE7", // Soft mint emerald
            "#EDE9FE", // Soft lavender violet
            "#FFE4E6", // Soft rose coral
        };

        static readonly string[] LightSquircleBorders = new[]
        {
            "#FCD34D", // Amber border
            "#BAE6FD", // Sky border
            "#BBF7D0", // Emerald border
            "#DDD6FE", // Violet border
            "#FECDD3", // Rose border
        };

        static readonly string[] DarkSquircles = new[]
        {
            "#28251E", // Obsidian Amber
            "#16222F", // Dark Sky
            "#15261F", // Dark Emerald
            "#241D30", // Dark Violet
            "#2C1A20", // Dark Rose
        };

        static readonly string[] DarkSquircleBorders = new[]
        {
            "#4DF59E0B",
            "#3838BDF8",
            "#3834D399",
            "#38A78BFA",
            "#38FB7185",
        };

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Brush SquircleBg
        {
            get
            {
                int idx = ColorIndex;
                string hex = Theme.Dark ? DarkSquircles[idx] : LightSquircles[idx];
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Brush SquircleBorder
        {
            get
            {
                int idx = ColorIndex;
                string hex = Theme.Dark ? DarkSquircleBorders[idx] : LightSquircleBorders[idx];
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
        }

        public void RefreshThemeBrushes()
        {
            InvalidateIcons();
            OnPropertyChanged(nameof(SquircleBg));
            OnPropertyChanged(nameof(SquircleBorder));
            OnPropertyChanged(nameof(BadgeBg));
            OnPropertyChanged(nameof(BadgeBorder));
            OnPropertyChanged(nameof(BadgeFg));
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Brush BadgeBg
        {
            get
            {
                string hex = Theme.Dark ? "#26F59E0B" : "#FEF3C7";
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Brush BadgeBorder
        {
            get
            {
                string hex = Theme.Dark ? "#4DF59E0B" : "#FCD34D";
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Brush BadgeFg
        {
            get
            {
                string hex = Theme.Dark ? "#FBBF24" : "#B45309";
                return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
        }

        /// <summary>Explicitly clears memory caches and cached disk .ico so next access regenerates fresh icons.</summary>
        public void InvalidateIcons()
        {
            _cachedIconSource = null;
            _cachedIconPathsHash = 0;
            _cachedIconPath = null;
            _cachedAppIcons = null;
            _cachedAppIconsHash = 0;
            _cachedAppPills = null;
            _cachedAppPillsHash = 0;
            OnPropertyChanged(nameof(IconSource));
            OnPropertyChanged(nameof(AppIcons));
            OnPropertyChanged(nameof(AppPills));
            OnPropertyChanged(nameof(AppCountBadge));
            OnPropertyChanged(nameof(LayoutBadge));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(OverflowCountText));
            OnPropertyChanged(nameof(GeneratedIconPath));
            OnPropertyChanged(nameof(Paths));
        }

        public IEnumerable<string> OrderedPaths() =>
            SortMode == "Alphabetical"
                ? Paths.OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
                : Paths;
    }
}
