using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace XAppGroup
{
    public static class DocScreenshots
    {
        public static void Capture(string targetDir = null)
        {
            string outDir = targetDir;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(AppContext.BaseDirectory, "..", "Docs", "Admin", "assets");
                if (!Directory.Exists(outDir))
                {
                    outDir = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Admin", "assets");
                }
            }
            outDir = Path.GetFullPath(outDir);
            Directory.CreateDirectory(outDir);

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            string notepad = Path.Combine(sys32, "notepad.exe");
            string cmd = Path.Combine(sys32, "cmd.exe");
            string powershell = Path.Combine(sys32, "WindowsPowerShell", "v1.0", "powershell.exe");
            string taskmgr = Path.Combine(sys32, "Taskmgr.exe");
            string regedit = Path.Combine(winDir, "regedit.exe");
            string mspaint = Path.Combine(sys32, "mspaint.exe");
            string calc = Path.Combine(sys32, "calc.exe");
            string explorer = Path.Combine(winDir, "explorer.exe");
            string mstsc = Path.Combine(sys32, "mstsc.exe");
            string write = Path.Combine(winDir, "write.exe");

            var gDev = new GroupData
            {
                Id = "dev-tools",
                GroupName = "Developer Tools",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + D",
                CustomSummary = "Integrated terminal environments, text editors, process manager, and registry tools.",
                Paths = new List<string> { powershell, cmd, notepad, taskmgr, regedit }
            };

            var gCreative = new GroupData
            {
                Id = "creative-studio",
                GroupName = "Design & Creative Studio",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + C",
                CustomSummary = "Digital illustration, graphics editing, quick document authoring, and color tools.",
                Paths = new List<string> { mspaint, write, calc, notepad }
            };

            var gProductivity = new GroupData
            {
                Id = "productivity-office",
                GroupName = "Productivity & Office",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + P",
                CustomSummary = "Daily workspace stack: document notes, file explorer, calculator, and remote desktop.",
                Paths = new List<string> { notepad, explorer, calc, mstsc }
            };

            var gUtilities = new GroupData
            {
                Id = "system-utilities",
                GroupName = "System Utilities",
                GroupCol = 2,
                Hotkey = "Ctrl + Alt + S",
                CustomSummary = "Diagnostics, system performance monitors, shell consoles, and configuration tools.",
                Paths = new List<string> { taskmgr, powershell, cmd, regedit }
            };

            var sampleSettings = new Settings
            {
                Theme = "Dark Acrylic",
                PopupTheme = "Dark Acrylic",
                AnimateWindow = false,
                AnimateContent = false,
                Groups = new ObservableCollection<GroupData> { gDev, gCreative, gProductivity, gUtilities }
            };
            Settings.Current = sampleSettings;

            // 1. Capture MainWindow in Dark Acrylic
            Theme.Apply("Dark");
            var mainWin = new MainWindow(sampleSettings, () => { });
            mainWin.Width = 920;
            mainWin.Height = 680;
            mainWin.Show();
            Pump(mainWin);
            SaveWindow(mainWin, Path.Combine(outDir, "main_screen.png"));
            mainWin.Close();

            // 2. Capture PopupWindow flyout for Developer Tools (no animations, instant full opacity)
            Theme.Apply("Dark");
            var pop = new PopupWindow(gDev);
            pop.Show();
            Pump(pop, 300);
            SaveWindow(pop, Path.Combine(outDir, "popup_flyout.png"));
            pop.Close();

            // 3. Capture EditWindow in Dark Acrylic
            Theme.Apply("Dark");
            var editDark = new EditWindow(gDev);
            editDark.Show();
            Pump(editDark);
            SaveWindow(editDark, Path.Combine(outDir, "edit_dark.png"));
            editDark.Close();

            // 4. Capture EditWindow in Light Glass
            Theme.Apply("Light Glass");
            var editLight = new EditWindow(gCreative);
            editLight.Show();
            Pump(editLight);
            SaveWindow(editLight, Path.Combine(outDir, "edit_light.png"));
            editLight.Close();

            Console.WriteLine("CAPTURED_TO: " + outDir);
        }

        private static void Pump(Window win, int waitMs = 100)
        {
            win.UpdateLayout();
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            }
            Thread.Sleep(waitMs);
            win.UpdateLayout();
        }

        private static void SaveWindow(Window win, string file)
        {
            var target = (win.Content as FrameworkElement) ?? (FrameworkElement)win;
            target.Opacity = 1.0;
            target.UpdateLayout();

            int w = Math.Max(1, (int)Math.Ceiling(target.ActualWidth > 0 ? target.ActualWidth : win.Width));
            int h = Math.Max(1, (int)Math.Ceiling(target.ActualHeight > 0 ? target.ActualHeight : win.Height));

            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(target);

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = new FileStream(file, FileMode.Create))
            {
                enc.Save(fs);
            }
        }
    }
}
