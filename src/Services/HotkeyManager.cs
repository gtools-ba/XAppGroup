using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace XAppGroup
{
    /// <summary>
    /// Registers global Win32 hotkeys (RegisterHotKey) on a hidden message-only window
    /// so configured group shortcuts work system-wide even when MainWindow is closed to tray.
    /// </summary>
    public static class HotkeyManager
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static HwndSource _source;
        private static readonly Dictionary<int, GroupData> _registered = new Dictionary<int, GroupData>();
        private static Action<GroupData> _onTriggered;
        private static int _nextId = 0x1000;

        private static void EnsureWindow()
        {
            if (_source != null && !_source.IsDisposed) return;
            var p = new HwndSourceParameters("XAppGroupHotkeyHost")
            {
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                WindowStyle = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            _source = new HwndSource(p);
            _source.AddHook(WndProc);
        }

        public static void Sync(IEnumerable<GroupData> groups, Action<GroupData> onTriggered)
        {
            _onTriggered = onTriggered;
            EnsureWindow();
            UnregisterAll();

            if (groups == null || _source == null) return;
            foreach (var g in groups)
            {
                if (g == null || string.IsNullOrWhiteSpace(g.Hotkey)) continue;
                if (!TryParseHotkey(g.Hotkey, out uint mods, out uint vk)) continue;

                int id = _nextId++;
                if (RegisterHotKey(_source.Handle, id, mods | MOD_NOREPEAT, vk))
                {
                    _registered[id] = g;
                }
                else
                {
                    Debug.WriteLine($"[HotkeyManager] Failed to register '{g.Hotkey}' for '{g.GroupName}' (err={Marshal.GetLastWin32Error()})");
                }
            }
        }

        public static bool TryParseHotkey(string hotkey, out uint modifiers, out uint virtualKey)
        {
            modifiers = 0;
            virtualKey = 0;
            if (string.IsNullOrWhiteSpace(hotkey)) return false;

            var parts = hotkey.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            Key parsedKey = Key.None;
            foreach (var raw in parts)
            {
                var token = raw.Trim();
                if (string.Equals(token, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "Control", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_CONTROL;
                else if (string.Equals(token, "Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_ALT;
                else if (string.Equals(token, "Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_SHIFT;
                else if (string.Equals(token, "Win", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(token, "Windows", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_WIN;
                else
                {
                    if (token.Length == 1 && char.IsDigit(token[0]))
                    {
                        parsedKey = (Key)((int)Key.D0 + (token[0] - '0'));
                    }
                    else if (token.StartsWith("Num", StringComparison.OrdinalIgnoreCase) && token.Length == 4 && char.IsDigit(token[3]))
                    {
                        parsedKey = (Key)((int)Key.NumPad0 + (token[3] - '0'));
                    }
                    else if (Enum.TryParse(token, ignoreCase: true, out Key k))
                    {
                        parsedKey = k;
                    }
                }
            }

            if (modifiers == 0 || parsedKey == Key.None) return false;
            virtualKey = (uint)KeyInterop.VirtualKeyFromKey(parsedKey);
            return virtualKey != 0;
        }

        private static void UnregisterAll()
        {
            if (_source != null && !_source.IsDisposed)
            {
                foreach (var id in _registered.Keys)
                {
                    try { UnregisterHotKey(_source.Handle, id); } catch { }
                }
            }
            _registered.Clear();
        }

        public static void Dispose()
        {
            UnregisterAll();
            if (_source != null)
            {
                try
                {
                    _source.RemoveHook(WndProc);
                    _source.Dispose();
                }
                catch { }
                _source = null;
            }
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registered.TryGetValue(id, out var group))
                {
                    handled = true;
                    try { _onTriggered?.Invoke(group); }
                    catch (Exception ex) { Debug.WriteLine($"[HotkeyManager] Trigger error: {ex.Message}"); }
                }
            }
            return IntPtr.Zero;
        }
    }
}
