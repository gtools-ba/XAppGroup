using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XAppGroup
{
    public static partial class SelfTest
    {
        static void RunInteractionTests(List<string> log, string[] apps, Action<string, Func<bool>> Check)
        {
            Check("Empty library disables group commands without exceptions", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings();
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var newBtn = FindButtonByAutomationId(m, "FooterNewBtn");
                var editBtn = FindButtonByAutomationId(m, "FooterEditBtn");
                var dupBtn = FindButtonByAutomationId(m, "FooterDuplicateBtn");
                var shortBtn = FindButtonByAutomationId(m, "FooterShortcutBtn");
                var delBtn = FindButtonByAutomationId(m, "FooterDeleteBtn");
                var testBtn = FindButtonByAutomationId(m, "FooterTestBtn");
                var hint = FindTextByAutomationId(m, "FooterSelectionHint");
                var groupButtons = new[] { editBtn, dupBtn, shortBtn, delBtn, testBtn };

                bool statesOk = m.GroupsList.Items.Count == 0 &&
                                groupButtons.All(b => b != null && !b.IsEnabled) &&
                                newBtn != null && newBtn.IsEnabled &&
                                hint != null && hint.Visibility == Visibility.Visible;

                bool noThrow = true;
                try
                {
                    foreach (var method in new[] { "Edit", "Duplicate", "Shortcut", "Delete", "Test" })
                        InvokePrivate(m, method);
                }
                catch (Exception ex)
                {
                    noThrow = false;
                    var inner = ex.GetBaseException();
                    log.Add($"INFO EMPTY_COMMAND_ERROR {inner.GetType().Name}: {inner.Message}");
                }

                log.Add($"INFO EMPTY_LIBRARY Groups=0 Items=0 GroupCommandsEnabled={groupButtons.Count(b => b != null && b.IsEnabled)} NewEnabled={newBtn?.IsEnabled} HintVisible={hint?.Visibility} NoThrow={noThrow}");
                m.Close();
                return statesOk && noThrow;
            });

            Check("Card keeps only icon and launch actions", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Card A", Paths = new List<string> { apps[0], apps[1] } },
                        new GroupData { GroupName = "Card Empty", Paths = new List<string>() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);
                m.GroupsList.SelectedItem = s.Groups[0];
                Pump(m);
                var item = GetItemContainer(m.GroupsList, s.Groups[0]);
                Pump(m);

                var icon = FindButtonByTag(item, "GroupIconHandle");
                var launch = FindVisibleButtonByTag(item, "CardLaunchAll");
                string iconName = icon != null ? AutomationProperties.GetName(icon) : "<none>";
                string launchName = launch != null ? AutomationProperties.GetName(launch) : "<none>";

                var allButtons = FindVisual<Button>(item).ToList();
                string[] removedTags = { "CardLauncher", "CardMore", "CardAddApp" };
                int removed = allButtons.Count(b => removedTags.Contains(b.Tag as string));
                var pills = allButtons.Where(b => (b.Tag as string) == "CardAppPill").ToList();
                bool pillIconsOnly = pills.Count == 2 &&
                                     pills.All(p => !FindVisual<TextBlock>(p).Any()) &&
                                     pills.Select(AutomationProperties.GetName)
                                          .SequenceEqual(new[] { "Launch notepad", "Launch calc" });
                bool namesOk = iconName == "Open launcher for Card A. Drag to pin." &&
                               launchName == "Launch all apps in Card A";
                bool tipsOk = (icon?.ToolTip as string) == "Open launcher. Drag to pin to the taskbar or desktop." &&
                              !string.IsNullOrEmpty(launch?.ToolTip as string) &&
                              icon != null && icon.Cursor == Cursors.Hand;
                bool invokeOk = icon != null && launch != null && HasInvokePattern(icon) && HasInvokePattern(launch);
                bool noPinConst = typeof(Lucide).GetField("Pin", BindingFlags.Public | BindingFlags.Static) == null;
                bool noOldTags = allButtons.All(b =>
                {
                    var t = b.Tag as string;
                    return t != "CardPopup" && t != "CardEdit";
                });

                // The group icon still opens the launcher popup and never the Edit dialog.
                icon?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, icon));
                Pump(m);
                var popup = Application.Current.Windows.OfType<PopupWindow>().LastOrDefault();
                bool iconClickOk = popup != null && !Application.Current.Windows.OfType<EditWindow>().Any(w => w.IsVisible);
                popup?.Close();

                log.Add($"INFO CARD_ACTIONS Icon='{iconName}' Launch='{launchName}' RemovedButtons={removed} AppIcons={pills.Count} IconsOnly={pillIconsOnly} Invoke={invokeOk} NoPinConst={noPinConst} IconClickPopup={iconClickOk}");
                m.Close();
                return namesOk && tipsOk && invokeOk && removed == 0 && pillIconsOnly && noPinConst && noOldTags && iconClickOk;
            });

            Check("Empty card replaces Launch with an Add apps button", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Has Apps", Paths = new List<string> { apps[0] } },
                        new GroupData { GroupName = "Empty Card", Paths = new List<string>() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);
                m.GroupsList.SelectedItem = s.Groups[1];
                Pump(m);
                var item = GetItemContainer(m.GroupsList, s.Groups[1]);
                Pump(m);

                var add = FindButtonByTag(item, "CardAddAppsEmpty");
                var launchBtn = FindButtonByTag(item, "CardLaunchAll");
                var launchHost = launchBtn != null ? FindAncestor<Grid>(launchBtn) : null;
                string name = add != null ? AutomationProperties.GetName(add) : "<none>";
                int visibleAddButtons = FindVisual<Button>(item).Count(b =>
                    AutomationProperties.GetName(b) == "Add apps to Empty Card" && b.Visibility == Visibility.Visible);
                bool ok = add != null && add.Visibility == Visibility.Visible &&
                          launchHost != null && launchHost.Visibility == Visibility.Hidden &&
                          visibleAddButtons == 1 &&
                          name == "Add apps to Empty Card" && HasInvokePattern(add) &&
                          !string.IsNullOrEmpty(add.ToolTip as string);

                log.Add($"INFO EMPTY_CARD AddVisible={add?.Visibility} LaunchHost={launchHost?.Visibility} VisibleAddButtons={visibleAddButtons} AddName='{name}' Invoke={(add != null && HasInvokePattern(add))}");
                m.Close();
                return ok;
            });

            Check("Cards show eight app icons then a +N overflow badge", () =>
            {
                Theme.Apply("Dark");
                var many = Enumerable.Range(0, 12).Select(i => apps[i % apps.Length]).ToList();
                var s = new Settings
                {
                    Groups =
                    {
                        new GroupData { GroupName = "Twelve", Paths = many },
                        new GroupData { GroupName = "Empty", Paths = new List<string>() }
                    }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var itemMany = GetItemContainer(m.GroupsList, s.Groups[0]);
                var itemEmpty = GetItemContainer(m.GroupsList, s.Groups[1]);
                Pump(m);
                var pills = FindVisual<Button>(itemMany).Where(b => (b.Tag as string) == "CardAppPill").ToList();
                var overflow = FindVisual<TextBlock>(itemMany).Where(t => t.Text == "+4").ToList();
                int emptyPills = FindVisual<Button>(itemEmpty).Count(b => (b.Tag as string) == "CardAppPill");

                bool dataOk = s.Groups[0].AppPills.Count == 8 && s.Groups[0].OverflowCountText == "+4" &&
                              s.Groups[1].AppPills.Count == 0 && s.Groups[1].OverflowCountText == "";
                bool uiOk = pills.Count == 8 && overflow.Count == 1 && emptyPills == 0 &&
                            pills.All(p => !FindVisual<TextBlock>(p).Any());

                log.Add($"INFO CARD_OVERFLOW DataPills={s.Groups[0].AppPills.Count} DataOverflow='{s.Groups[0].OverflowCountText}' UIPills={pills.Count} UIBadge={overflow.Count} EmptyPills={emptyPills}");
                m.Close();
                return dataOk && uiOk;
            });

            Check("Add apps opens Edit in Applications mode with Add focused", () =>
            {
                Theme.Apply("Dark");
                var g3 = new GroupData { GroupName = "Edit Apps", Paths = new List<string> { apps[0] } };
                var ew = new EditWindow(g3, true);
                ew.Show();
                Pump(ew);

                var add = FindButtonByAutomationId(ew, "EditAddAppBtn");
                var list = FindVisual<ListBox>(ew).FirstOrDefault(l => AutomationProperties.GetAutomationId(l) == "EditApplicationsList");
                bool titleOk = ew.Title == "Edit Group — Applications";
                bool addOk = add != null && AutomationProperties.GetName(add) == "Add applications";
                bool listOk = list != null && list.Visibility == Visibility.Visible && list.ActualHeight > 0 &&
                              list.SelectedIndex == 0;
                bool focusOk = add != null && (add.IsKeyboardFocused || add.IsKeyboardFocusWithin);

                log.Add($"INFO EDIT_APPS Title='{ew.Title}' AddName='{(add != null ? AutomationProperties.GetName(add) : "<none>")}' ListVisible={list?.Visibility} ListHeight={list?.ActualHeight:F1} ListSelected={list?.SelectedIndex} AddFocused={focusOk}");
                ew.Close();
                return titleOk && addOk && listOk && focusOk;
            });

            Check("Header right buttons are Donate and GitHub only", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings
                {
                    Groups = { new GroupData { GroupName = "Header Test", Paths = new List<string> { apps[0] } } }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                var donate = FindButtonByAutomationId(m, "HeaderDonateBtn");
                var github = FindButtonByAutomationId(m, "HeaderGitHubBtn");
                bool noCaption = !FindVisual<FrameworkElement>(m).Any(e =>
                {
                    string tip = e.ToolTip as string;
                    return tip == "Minimize Window" || tip == "Maximize / Restore Window";
                });
                string dn = donate != null ? AutomationProperties.GetName(donate) : "<none>";
                string gn = github != null ? AutomationProperties.GetName(github) : "<none>";
                bool tips = donate != null && github != null &&
                            (donate.ToolTip as string) == "Donate" && (github.ToolTip as string) == "GitHub";
                bool ok = donate != null && github != null && dn == "Donate" && gn == "GitHub" &&
                          tips && noCaption && HasInvokePattern(donate) && HasInvokePattern(github);

                log.Add($"INFO HEADER_ACTIONS Donate='{dn}' GitHub='{gn}' Tooltips={tips} CaptionButtons={!noCaption} Invoke={(donate != null && HasInvokePattern(donate) && HasInvokePattern(github))}");
                m.Close();
                return ok;
            });

            Check("Footer commands stay icons at 720 and 896 DIP, labeled above", () =>
            {
                Theme.Apply("Dark");
                var s = new Settings
                {
                    Groups = { new GroupData { GroupName = "Footer Test", Paths = new List<string> { apps[0] } } }
                };
                var m = new MainWindow(s, () => { });
                m.Show();
                Pump(m);

                string[] ids = { "FooterEditBtn", "FooterDuplicateBtn", "FooterShortcutBtn", "FooterDeleteBtn" };
                var cmd = ids.Select(id => FindButtonByAutomationId(m, id)).ToList();
                var newBtn = FindButtonByAutomationId(m, "FooterNewBtn");

                bool IconMode() => cmd.All(b => b != null &&
                    !FindVisual<ContentPresenter>(b).Any() &&
                    !string.IsNullOrEmpty(AutomationProperties.GetName(b)) &&
                    !string.IsNullOrEmpty(b.ToolTip as string) &&
                    b.ActualWidth <= 50);
                bool LabeledMode() => cmd.All(b => b != null && FindVisual<ContentPresenter>(b).Any());
                bool NewLabeled() => newBtn != null && FindVisual<ContentPresenter>(newBtn).Any();

                m.Width = 720; m.UpdateResponsiveLayout(720, 520); Pump(m);
                bool icons720 = IconMode() && NewLabeled();
                string widths720 = string.Join(",", cmd.Select(b => b != null ? b.ActualWidth.ToString("F0") : "?"));

                m.Width = 896; m.UpdateResponsiveLayout(896, 740); Pump(m);
                bool icons896 = IconMode() && NewLabeled();
                string widths896 = string.Join(",", cmd.Select(b => b != null ? b.ActualWidth.ToString("F0") : "?"));

                m.Width = 1024; m.UpdateResponsiveLayout(1024, 740); Pump(m);
                bool labeled1024 = LabeledMode() && NewLabeled();

                m.Width = 896; m.UpdateResponsiveLayout(896, 740); Pump(m);
                bool restored896 = IconMode() && NewLabeled();
                string names = string.Join(",", cmd.Select(b => b != null ? AutomationProperties.GetName(b) : "<none>"));

                log.Add($"INFO FOOTER_RESPONSIVE Icons720={icons720} Widths720='{widths720}' Icons896={icons896} Widths896='{widths896}' Labeled1024={labeled1024} Restored896={restored896} Names='{names}'");
                m.Close();
                return icons720 && icons896 && labeled1024 && restored896;
            });

            Check("Popup app-label toggle controls popup names and persists", () =>
            {
                Theme.Apply("Dark");
                var group = new GroupData
                {
                    GroupName = "Label Test", GroupCol = 2, ShowLabels = true,
                    Paths = new List<string> { apps[0], apps[1] }
                };

                // The Edit dialog checkbox reflects the per-group setting in both states.
                var ewOn = new EditWindow(group); ewOn.Show(); Pump(ewOn);
                var cbOn = FindVisual<CheckBox>(ewOn).FirstOrDefault(c => (c.Content as string) == "Show app labels in popup");
                var ewOff = new EditWindow(new GroupData
                {
                    GroupName = "No Labels", GroupCol = 2, ShowLabels = false,
                    Paths = new List<string> { apps[0] }
                });
                ewOff.Show(); Pump(ewOff);
                var cbOff = FindVisual<CheckBox>(ewOff).FirstOrDefault(c => (c.Content as string) == "Show app labels in popup");
                bool checkboxOk = cbOn != null && cbOn.IsChecked == true && cbOff != null && cbOff.IsChecked == false;
                ewOn.Close(); ewOff.Close();

                // PopupWindow draws the name TextBlocks only when the setting is on.
                var pwOn = new PopupWindow(group); pwOn.Show(); Pump(pwOn);
                var labelsOn = FindVisual<TextBlock>(pwOn)
                    .Where(t => t.FontSize == 11 && (t.Text == "notepad" || t.Text == "calc")).ToList();
                int imagesOn = FindVisual<Image>(pwOn).Count(i => i.Width == 32);
                pwOn.Close();

                group.ShowLabels = false;
                var pwOff = new PopupWindow(group); pwOff.Show(); Pump(pwOff);
                var labelsOff = FindVisual<TextBlock>(pwOff)
                    .Where(t => t.FontSize == 11 && (t.Text == "notepad" || t.Text == "calc")).ToList();
                int imagesOff = FindVisual<Image>(pwOff).Count(i => i.Width == 32);
                pwOff.Close();

                // The flag survives a settings roundtrip.
                var st = new Settings(); st.Groups.Add(group); Store.Save(st);
                bool persisted = Store.Load().Groups[0].ShowLabels == false;
                group.ShowLabels = true;

                bool labelsOk = labelsOn.Count == 2 && labelsOff.Count == 0 && imagesOn == 2 && imagesOff == 2;
                log.Add($"INFO POPUP_LABELS Checkbox={checkboxOk} LabelsOn={labelsOn.Count} LabelsOff={labelsOff.Count} ImagesOn={imagesOn} ImagesOff={imagesOff} Persisted={persisted}");
                return checkboxOk && labelsOk && persisted;
            });

            Check("SettingsWindow builds and persists settings + group Hotkey", () =>
            {
                Theme.Apply("Dark");
                var st = new Settings
                {
                    Theme = "Dark",
                    PopupTheme = "Light Glass",
                    AccentColorBackground = true,
                    ShowTrayIcon = false,
                    ShowGroupsInTray = true,
                    AnimateWindow = false,
                    AnimateContent = false,
                    EnableLaunchAll = false,
                    GrayscaleIcon = true,
                    CheckUpdates = true,
                    Groups = { new GroupData { GroupName = "Hotkey Group", Hotkey = "Ctrl + Alt + 7", Paths = new List<string> { apps[0] } } }
                };
                Settings.Current = st;
                Store.Save(st);
                var loaded = Store.Load();
                bool persisted = loaded.PopupTheme == "Light Glass" &&
                                 loaded.AccentColorBackground &&
                                 loaded.ShowTrayIcon == false &&
                                 loaded.ShowGroupsInTray &&
                                 loaded.AnimateWindow == false &&
                                 loaded.AnimateContent == false &&
                                 loaded.EnableLaunchAll == false &&
                                 loaded.GrayscaleIcon &&
                                 loaded.CheckUpdates &&
                                 loaded.Groups.Count == 1 &&
                                 loaded.Groups[0].Hotkey == "Ctrl + Alt + 7" &&
                                 loaded.Groups[0].HotkeyBadge == "Ctrl + Alt + 7";

                var sw = new SettingsWindow(loaded, () => { });
                sw.Show();
                Pump(sw);
                bool hasTitle = sw.Title == "Settings";
                sw.Close();
                Settings.Current = null;
                return persisted && hasTitle;
            });

            Check("AppConfig creates appconfig.json and SupportDialog builds cleanly", () =>
            {
                var cfg = AppConfig.Load();
                bool cfgOk = File.Exists(AppConfig.ConfigPath) &&
                             !string.IsNullOrWhiteSpace(cfg.GitHubUrl) &&
                             cfg.SupportUrl.Contains("ko-fi.com");
                var sd = new SupportDialog();
                sd.Show();
                Pump(sd);
                bool dlgOk = sd.Title == "Support Us" && sd.ActualWidth >= 380;
                sd.Close();
                return cfgOk && dlgOk;
            });

            Check("Unified Brand Squircle builds and XAppGroup.ico exists with multi-res", () =>
            {
                var squircle = Icons.BuildBrandSquircle(20.0);
                bool squircleOk = squircle != null && squircle.Child is Grid g && g.Children.Count == 4;

                string ico = Path.Combine(AppContext.BaseDirectory, "XAppGroup.ico");
                bool icoOk = File.Exists(ico) && new FileInfo(ico).Length > 10000;
                return squircleOk && icoOk;
            });
        }
    }
}
