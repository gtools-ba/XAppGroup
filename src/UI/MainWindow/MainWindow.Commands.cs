using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using IOPath = System.IO.Path;
using WpfPath = System.Windows.Shapes.Path;

namespace XAppGroup
{
    public partial class MainWindow : Window
    {
        void ApplySearchFilter(string query)
        {
            bool hasQuery = !string.IsNullOrWhiteSpace(query);
            string q = hasQuery ? query.Trim() : null;

            // Keep one collection view over the real ObservableCollection. Filtering never replaces the
            // ItemsSource, so selection identity (GroupData.Id) survives every keystroke.
            _suppressSelectionMemory = true;
            try
            {
                _groupsView.Filter = hasQuery
                    ? new Predicate<object>(o => MatchesFilter(o as GroupData, q))
                    : null;
                _groupsView.Refresh();
            }
            finally
            {
                _suppressSelectionMemory = false;
            }
            RestoreSelectionFromRemembered();
            UpdateCommandState();
        }

        static bool MatchesFilter(GroupData g, string q)
        {
            if (g == null || q == null) return false;
            if (g.GroupName != null && g.GroupName.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
            return g.Paths != null && g.Paths.Any(p => !string.IsNullOrEmpty(p) &&
                IOPath.GetFileNameWithoutExtension(p).Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        void UpdateStats()
        {
            int gCount = _s?.Groups?.Count ?? 0;
            int lCount = _s?.Groups?.Sum(x => x.Paths?.Count ?? 0) ?? 0;
            _groupCountText.Text = gCount.ToString();
            _launcherCountText.Text = lCount.ToString();
        }

        void Refresh()
        {
            _suppressSelectionMemory = true;
            try
            {
                _groupsView.Refresh();
            }
            finally
            {
                _suppressSelectionMemory = false;
            }
            RestoreSelectionFromRemembered();
            UpdateStats();
            UpdateCommandState();
        }

        /// <summary>Re-applies the remembered selection if that group is present in the current view.</summary>
        void RestoreSelectionFromRemembered()
        {
            GroupData target = null;
            if (!string.IsNullOrEmpty(_rememberedGroupId))
            {
                foreach (var g in _s.Groups)
                {
                    if (string.Equals(g.Id, _rememberedGroupId, StringComparison.OrdinalIgnoreCase))
                    {
                        target = g;
                        break;
                    }
                }
            }

            if (target != null && _groupsView.Contains(target))
            {
                if (!ReferenceEquals(_list.SelectedItem, target)) _list.SelectedItem = target;
                _list.ScrollIntoView(target);
            }
            else if (_list.SelectedItem != null)
            {
                _list.SelectedItem = null;
            }
        }

        /// <summary>Selects the group and keeps it visible in the list viewport.</summary>
        void SelectAndReveal(GroupData g)
        {
            if (g == null)
            {
                UpdateCommandState();
                return;
            }

            _rememberedGroupId = g.Id;
            if (_groupsView.Contains(g))
            {
                if (!ReferenceEquals(_list.SelectedItem, g)) _list.SelectedItem = g;
                _list.ScrollIntoView(g);
                Dispatcher.InvokeAsync(() =>
                {
                    if (_groupsView.Contains(g))
                    {
                        _list.ScrollIntoView(g);
                        _list.UpdateLayout();
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else if (_list.SelectedItem != null)
            {
                _list.SelectedItem = null;
            }
            UpdateCommandState();
        }

        /// <summary>Single path that keeps the footer commands truthful for the current selection.</summary>
        void UpdateCommandState()
        {
            bool hasSelection = Sel != null;
            SetCommandEnabled(_footerNewBtn, true); // New is always available.
            SetCommandEnabled(_footerEditBtn, hasSelection);
            SetCommandEnabled(_footerDuplicateBtn, hasSelection);
            SetCommandEnabled(_footerShortcutBtn, hasSelection);
            SetCommandEnabled(_footerDeleteBtn, hasSelection);
            SetCommandEnabled(_footerTestBtn, hasSelection);

            if (_footerSelectionHint != null)
            {
                _footerSelectionHint.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        static void SetCommandEnabled(Button b, bool enabled)
        {
            if (b == null) return;
            b.IsEnabled = enabled;
            // Disabled state is local so it cannot be overridden by the template's hover trigger.
            b.Opacity = enabled ? 1.0 : 0.45;
            b.Cursor = enabled ? Cursors.Hand : Cursors.Arrow;
        }

        bool TrySelectFocusedItem()
        {
            GroupData target = null;
            if (Keyboard.FocusedElement is DependencyObject d)
            {
                var cur = d;
                while (cur != null && cur is not ListBoxItem) cur = VisualTreeHelper.GetParent(cur);
                if (cur is ListBoxItem lbi && lbi.DataContext is GroupData g) target = g;
            }
            if (target == null && _list.IsKeyboardFocusWithin && _list.Items.CurrentItem is GroupData current)
                target = current;
            if (target != null && !ReferenceEquals(_list.SelectedItem, target))
            {
                _list.SelectedItem = target;
                return true;
            }
            return false;
        }

        GroupData Sel => _list.SelectedItem as GroupData;
        bool _isEditing;

        public void EditGroupById(string id)
        {
            if (_isEditing) return;
            var g = _s.Groups.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
                 ?? _s.Groups.FirstOrDefault(x => string.Equals(x.GroupName, id, StringComparison.OrdinalIgnoreCase))
                 ?? Sel
                 ?? _s.Groups.FirstOrDefault();
            if (g == null) return;
            SelectAndReveal(g);
            EditGroup(g);
        }

        void EditGroup(GroupData g)
        {
            EditGroup(g, false);
        }

        void EditGroup(GroupData g, bool applicationsFocus)
        {
            if (g == null || _isEditing) return;
            _isEditing = true;
            try
            {
                JumpListHelper.Update(g);
                if (new EditWindow(g, applicationsFocus) { Owner = this }.ShowDialog() == true)
                {
                    _changed();
                    UpdateStats();
                    JumpListHelper.Update(g);
                    // A rename can move the group in/out of an active search filter.
                    _suppressSelectionMemory = true;
                    try
                    {
                        _groupsView.Refresh();
                    }
                    finally
                    {
                        _suppressSelectionMemory = false;
                    }
                    RestoreSelectionFromRemembered();
                    UpdateCommandState();
                }
            }
            finally
            {
                _isEditing = false;
            }
        }

        void New()
        {
            if (_isEditing) return;
            var g = new GroupData { GroupName = "Group " + (_s.Groups.Count + 1) };
            _isEditing = true;
            try
            {
                if (new EditWindow(g) { Owner = this }.ShowDialog() == true)
                {
                    _s.Groups.Add(g);
                    // The new group must be selected and visible, even if a filter was active.
                    if (!_groupsView.Contains(g) && !string.IsNullOrWhiteSpace(_searchBox.Text))
                        _searchBox.Text = string.Empty;
                    SelectAndReveal(g);
                    _changed();
                    UpdateStats();
                    JumpListHelper.Update(g);
                }
            }
            finally
            {
                _isEditing = false;
            }
        }

        void Edit()
        {
            EditGroup(Sel);
        }

        void Duplicate()
        {
            var g = Sel; if (g == null) return;
            var copy = new GroupData
            {
                GroupName = g.GroupName + " - Copy", GroupCol = g.GroupCol, ShowLabels = g.ShowLabels,
                ShowOnTray = g.ShowOnTray, SortMode = g.SortMode, Paths = g.Paths.ToList(), IconPath = g.IconPath
            };
            _s.Groups.Add(copy);
            if (!_groupsView.Contains(copy) && !string.IsNullOrWhiteSpace(_searchBox.Text))
                _searchBox.Text = string.Empty;
            SelectAndReveal(copy);
            _changed();
            UpdateStats();
            JumpListHelper.Update(copy);
        }

        void Delete()
        {
            var g = Sel; if (g == null) return;
            if (ThemedDialog.Show(this, "Delete group \"" + g.GroupName + "\"?", "Delete Group", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            var lnk = IOPath.Combine(Store.ShortcutDir, Launcher.Sanitize(g.GroupName) + ".lnk");
            TaskbarHelper.Unpin(g);
            TaskbarHelper.Unpin(lnk);
            JumpListHelper.DeleteGroupJumpList(g.Id);
            var removed = Launcher.DeleteShortcut(g);
            g.Hotkey = "";
            RemoveGroupSelectNearest(g);
            _changed();
            UpdateStats();
            ThemedDialog.Show(this,
                $"Group \"{g.GroupName}\" deleted, unpinned from taskbar, and cleaned up.",
                "Group Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Removes a group and selects the group that took its visible index; when the removed group was
        /// last, selects the new last group. Selection is only empty when the library is empty (or when
        /// an active search filter no longer matches any group; the nearest Id is still remembered).
        /// Returns the newly selected group, or null.
        /// </summary>
        GroupData RemoveGroupSelectNearest(GroupData g)
        {
            if (g == null) return null;
            int visibleIndex = _list.Items.IndexOf(g);
            int groupIndex = _s.Groups.IndexOf(g);

            _s.Groups.Remove(g);

            GroupData next = null;
            int count = _list.Items.Count;
            if (count > 0)
            {
                int idx = visibleIndex >= 0
                    ? Math.Min(visibleIndex, count - 1)
                    : Math.Min(Math.Max(groupIndex, 0), count - 1);
                next = _list.Items[idx] as GroupData;
            }

            if (next != null)
            {
                SelectAndReveal(next);
            }
            else if (_s.Groups.Count > 0)
            {
                int idx = Math.Min(Math.Max(groupIndex, 0), _s.Groups.Count - 1);
                _rememberedGroupId = _s.Groups[idx].Id;
                _list.SelectedItem = null;
            }
            else
            {
                _rememberedGroupId = null;
                _list.SelectedItem = null;
            }
            UpdateCommandState();
            return next;
        }

        void Shortcut()
        {
            var g = Sel; if (g == null) return;
            try
            {
                var lnk = ShortcutHelper.Create(g, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
                ThemedDialog.Show(this, "Created shortcut on Desktop:\n" + lnk, "Desktop Shortcut", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { ThemedDialog.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        void Test()
        {
            var g = Sel; if (g == null) return;
            new PopupWindow(g).Show();
        }

        void OpenSettings()
        {
            var dlg = new SettingsWindow(_s, () =>
            {
                _changed();
                if (_themeSelector != null)
                {
                    string expected = _s.Theme == "Light" ? "Light Glass" : (_s.Theme == "Dark" ? "Dark Acrylic" : "System (Mica)");
                    if ((_themeSelector.SelectedItem as string) != expected)
                        _themeSelector.SelectedItem = expected;
                }
            })
            {
                Owner = this
            };
            dlg.ShowDialog();
        }
    }
}
