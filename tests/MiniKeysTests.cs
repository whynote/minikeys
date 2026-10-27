using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Thread = System.Threading.Thread;

internal static class MiniKeysTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static Assembly AppAssembly;
    internal static string Scratch;
    internal static int Checks;
    private static object originalSettingsPath;
    private static FieldInfo settingsPath;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Console.Error.WriteLine("Usage: MiniKeysTests.exe <MiniKeys.exe> [--model-only]"); return 2; }
        Scratch = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Scratch);
        try
        {
            AppAssembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            settingsPath = Type("Settings").GetField("FilePath", Members);
            originalSettingsPath = settingsPath.GetValue(null);
            settingsPath.SetValue(null, Path.Combine(Scratch, "settings.json"));
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            IconTests(args[0]);
            ModelTests();
            ComponentTests();
            if (!args.Contains("--model-only"))
            {
                using (var host = new InputHost())
                {
                    Application.Run(host);
                    if (host.Error != null) throw host.Error;
                }
            }
            Console.WriteLine("PASS: " + Checks + " checks");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL: " + e);
            return 1;
        }
        finally
        {
            if (settingsPath != null) settingsPath.SetValue(null, originalSettingsPath);
            // Only the uniquely named test directory created above is removed.
            if (Directory.Exists(Scratch)) Directory.Delete(Scratch, true);
        }
    }

    internal static Type Type(string name) { return AppAssembly.GetType("MiniKeys." + name, true); }
    internal static object Call(Type type, object instance, string name, params object[] args) { return type.GetMethod(name, Members).Invoke(instance, args); }
    internal static void Set(object instance, string name, object value) { instance.GetType().GetField(name, Members).SetValue(instance, value); }
    internal static object Get(object instance, string name) { return instance.GetType().GetField(name, Members).GetValue(instance); }
    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Checks++;
        Console.WriteLine("PASS: " + message);
    }
    internal static IList Parse(string text) { return (IList)Call(Type("KeyMap"), null, "Parse", text); }

    private static void IconTests(string executablePath)
    {
        using (Stream resource = AppAssembly.GetManifestResourceStream("MiniKeys.AppIcon.ico"))
        {
            Check(resource != null, "custom application icon is embedded in the assembly");
            using (var icon = new Icon(resource))
                Check(icon.Width > 0 && icon.Height > 0, "embedded icon can be loaded without an external asset file");
        }
        using (Icon associated = Icon.ExtractAssociatedIcon(Path.GetFullPath(executablePath)))
            Check(associated != null && associated.Width > 0, "Windows can extract the executable application icon");
    }

    private static bool IconDisposed(Icon icon)
    {
        try { return icon.Handle == IntPtr.Zero; }
        catch (ObjectDisposedException) { return true; }
    }

    private static void ModelTests()
    {
        Check(String.Join(" ", Parse("3 E D C").Cast<object>().Select(k => (string)Get(k, "Name"))) == "3 E D C", "default key order");
        var custom = Parse("e Space PageDown F12");
        Check((string)Get(custom[0], "Name") == "E" && (ushort)Get(custom[3], "Code") == 0x7B && (bool)Get(custom[2], "Extended"), "custom key names and extended navigation mapping");
        foreach (string invalid in new[] { "", "Ctrl+E", String.Join(" ", Enumerable.Repeat("E", 25)) })
        {
            bool rejected = false;
            try { Parse(invalid); } catch (TargetInvocationException e) { rejected = e.InnerException is ArgumentException; }
            Check(rejected, "invalid key setting rejected: " + (invalid == "" ? "empty" : invalid));
        }
        var settings = Activator.CreateInstance(Type("Settings"), true);
        Check((int)Get(settings, "Columns") == 1 && (int)Get(settings, "Width") == 60 && (int)Get(settings, "Height") == 244, "new settings use the compact vertical layout");
        Check((string)Get(settings, "Language") == "zh" && (string)Get(settings, "VoiceInput") == "wechat", "new settings default to Chinese UI and WeChat voice input");
        Check((bool)Get(settings, "FadeEnabled") && (int)Get(settings, "FadeOpacityPercent") == 30, "new settings enable idle fading at 30 percent opacity");
        Set(settings, "Keys", "E 3 Backspace"); Set(settings, "Columns", 2);
        Set(settings, "X", 173); Set(settings, "Y", 241); Set(settings, "Width", 347); Set(settings, "Height", 183); Set(settings, "Pinned", false);
        Set(settings, "FadeEnabled", false); Set(settings, "FadeOpacityPercent", 70);
        Set(settings, "VoiceInput", "windows"); Set(settings, "Language", "en");
        Check((bool)Call(Type("Settings"), settings, "Save"), "save to isolated configuration path");
        var loaded = Call(Type("Settings"), null, "Load");
        Check(new[] { "Keys", "Columns", "X", "Y", "Width", "Height", "Pinned", "FadeEnabled", "FadeOpacityPercent", "VoiceInput", "Language" }.All(n => Object.Equals(Get(settings, n), Get(loaded, n))), "keys, layout, bounds, pin, fade, voice mode and UI language persist");
        File.WriteAllText((string)settingsPath.GetValue(null), "{\"Keys\":\"3 E D C\",\"Columns\":1,\"X\":173,\"Y\":241,\"Width\":196,\"Height\":158,\"Pinned\":true}");
        loaded = Call(Type("Settings"), null, "Load");
        Check((bool)Get(loaded, "FadeEnabled") && (int)Get(loaded, "FadeOpacityPercent") == 30 && (string)Get(loaded, "Language") == "zh" && (string)Get(loaded, "VoiceInput") == "wechat", "v1 configuration receives defaults for missing fade, voice and language fields");
        Check((int)Get(loaded, "Columns") == 1 && (int)Get(loaded, "Width") == 196 && (int)Get(loaded, "Height") == 158, "v1 configuration keeps its existing layout and bounds");
        File.WriteAllText((string)settingsPath.GetValue(null), "{\"Keys\":\"3 E D C\",\"Columns\":4,\"Width\":284,\"Height\":104,\"FadeEnabled\":false,\"FadeOpacityPercent\":65,\"VoiceInput\":\"windows\"}");
        loaded = Call(Type("Settings"), null, "Load");
        Check(!(bool)Get(loaded, "FadeEnabled") && (int)Get(loaded, "FadeOpacityPercent") == 65 && (string)Get(loaded, "VoiceInput") == "windows" && (int)Get(loaded, "Columns") == 4, "upgrade preserves existing fade, voice and horizontal layout choices");
        foreach (int opacity in new[] { -5, 120 })
        {
            File.WriteAllText((string)settingsPath.GetValue(null), "{\"Keys\":\"3 E D C\",\"FadeOpacityPercent\":" + opacity + ",\"Language\":\"unknown\",\"VoiceInput\":\"unknown\"}");
            loaded = Call(Type("Settings"), null, "Load");
            Check((int)Get(loaded, "FadeOpacityPercent") == Math.Max(1, Math.Min(100, opacity)) && (string)Get(loaded, "Language") == "zh" && (string)Get(loaded, "VoiceInput") == "wechat", "invalid language and voice settings fall back while opacity is clamped: " + opacity);
        }
        File.WriteAllText((string)settingsPath.GetValue(null), "{invalid json");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C" && (int)Get(loaded, "Columns") == 1 && (string)Get(loaded, "Language") == "zh", "corrupt configuration restores defaults");
        File.WriteAllText((string)settingsPath.GetValue(null), "null");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C", "null configuration restores defaults");
        File.Delete((string)settingsPath.GetValue(null));
    }

    private static Control[] KeyTiles(Form keyboard)
    {
        return ((IEnumerable)Get(keyboard, "keys")).Cast<Control>().ToArray();
    }

    private static void CheckToolWindowStyle(Form keyboard, string description)
    {
        int style = Native.GetWindowLong(keyboard.Handle, -20);
        Check((style & 0x08000000) != 0 && (style & 0x00000080) != 0 && (style & 0x00040000) == 0 && !keyboard.ShowInTaskbar, description);
    }

    private static void CheckLayout(Form keyboard, string description)
    {
        keyboard.PerformLayout();
        var tiles = keyboard.Controls.Cast<Control>().ToArray();
        bool contained = tiles.All(c => keyboard.ClientRectangle.Contains(c.Bounds) && c.Width > 0 && c.Height > 0);
        bool separate = !tiles.Where((a, i) => tiles.Skip(i + 1).Any(b => a.Bounds.IntersectsWith(b.Bounds))).Any();
        Check(contained && separate && KeyTiles(keyboard).All(c => c.Width >= 24 && c.Height >= 24), description + " keeps every tool and key inside the window, separate and usable (" + keyboard.Width + "x" + keyboard.Height + ")");
    }

    private static void Preview(Form keyboard, string filename)
    {
        string directory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "docs", "images"));
        Directory.CreateDirectory(directory);
        using (var bitmap = new Bitmap(keyboard.Width, keyboard.Height))
        {
            keyboard.DrawToBitmap(bitmap, keyboard.ClientRectangle);
            // A hidden form omits children from its WM_PRINT pass. Render each
            // actual control into the same layout without displaying the form.
            using (var graphics = Graphics.FromImage(bitmap))
            foreach (Control control in keyboard.Controls)
            using (var tile = new Bitmap(control.Width, control.Height))
            {
                control.DrawToBitmap(tile, control.ClientRectangle);
                graphics.DrawImageUnscaled(tile, control.Location);
            }
            bitmap.Save(Path.Combine(directory, filename));
        }
    }

    private static void ComponentTests()
    {
        CultureInfo originalCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Type("Ui").GetField("Language", Members).SetValue(null, "zh");
            Check((string)Call(Type("Ui"), null, "Text", "中文", "English") == "中文", "Chinese UI selection is independent of Windows UI culture");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            Type("Ui").GetField("Language", Members).SetValue(null, "en");
            Check((string)Call(Type("Ui"), null, "Text", "中文", "English") == "English", "English UI selection is independent of Windows UI culture");
            bool trayDisposed = false;
            ContextMenuStrip trayMenuForDisposal = null;
            Icon appIconForDisposal = null;
            using (var keyboard = (Form)Activator.CreateInstance(Type("KeyboardWindow"), true))
            {
                // Creating handles and bitmaps does not show or activate this window.
                IntPtr handle = keyboard.Handle;
                var tools = new[] { "voice", "pin", "configure", "minimize", "close" }.Select(n => (Control)Get(keyboard, n)).ToArray();
                Check(tools.Select(c => (string)Get(c, "Icon")).SequenceEqual(new[] { "microphone", "pin", "settings", "minimize", "close" }), "toolbar exposes microphone, pin, settings, minimize and close icons in order");
                Check(tools.All(c => !String.IsNullOrEmpty(c.AccessibleName) && !c.TabStop), "icon controls have accessible labels and do not take keyboard focus");
                CheckToolWindowStyle(keyboard, "native tool window keeps no-activate behavior without an application taskbar entry");
                var trayField = Type("KeyboardWindow").GetFields(Members).Single(f => typeof(NotifyIcon).IsAssignableFrom(f.FieldType));
                var tray = (NotifyIcon)trayField.GetValue(keyboard);
                trayMenuForDisposal = tray.ContextMenuStrip;
                tray.Disposed += delegate { trayDisposed = true; };
                Check(tray.Icon != null && !tray.Visible, "hidden component prepares a tray icon without displaying it");
                appIconForDisposal = (Icon)Get(keyboard, "appIcon");
                Check(Object.ReferenceEquals(keyboard.Icon, appIconForDisposal) && Object.ReferenceEquals(tray.Icon, appIconForDisposal) && !Object.ReferenceEquals(appIconForDisposal, SystemIcons.Application), "window and tray share the embedded custom icon instead of the system application icon");
                Check(trayMenuForDisposal != null && trayMenuForDisposal.Items.OfType<ToolStripMenuItem>().Count() == 2, "tray menu provides restore and exit entries");
                Check(keyboard.Text == "迷你补键盘", "keyboard loads the default Chinese UI selection");
                CheckLayout(keyboard, "default vertical layout");
                Preview(keyboard, "v1.2.0.png");

                object settings = Get(keyboard, "settings");
                var hints = (ToolTip)Get(keyboard, "hints");
                string englishTrayMenu = null;
                foreach (string language in new[] { "en", "zh" })
                {
                    Set(settings, "Language", language);
                    Call(Type("KeyboardWindow"), keyboard, "ApplyLanguage");
                    Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                    Check(keyboard.Text == (language == "en" ? "MiniKeys" : "迷你补键盘") && hints.GetToolTip(tools[2]) == (language == "en" ? "Customize keys and layout" : "自定义按键和排列"), "language changes refresh the caption and settings tooltip immediately: " + language);
                    Check(hints.GetToolTip(tools[0]).StartsWith(language == "en" ? "WeChat:" : "微信语音：") && hints.GetToolTip(KeyTiles(keyboard)[0]).StartsWith(language == "en" ? "Send " : "发送 "), "language changes refresh voice and key tooltips without activating voice: " + language);
                    var menuEntries = trayMenuForDisposal.Items.OfType<ToolStripMenuItem>().ToArray();
                    Check(!String.IsNullOrEmpty(tray.Text) && menuEntries.All(item => !String.IsNullOrEmpty(item.Text)), "tray tooltip and menu entries have localized labels: " + language);
                    string currentTrayMenu = String.Join("|", menuEntries.Select(item => item.Text));
                    if (language == "en") englishTrayMenu = currentTrayMenu;
                    else Check(currentTrayMenu != englishTrayMenu, "changing UI language also refreshes the tray menu");
                }
                Set(settings, "Columns", 1);
                Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                foreach (int width in new[] { 40, 55, 56, 77, 78, 99, 100, 121, 122, 196, 284 })
                {
                    keyboard.Width = width;
                    keyboard.Height = keyboard.MinimumSize.Height;
                    CheckLayout(keyboard, "vertical layout at requested width " + width);
                }
                keyboard.Width = keyboard.MinimumSize.Width;
                keyboard.Height = keyboard.MinimumSize.Height;
                Check(keyboard.Width <= 44, "four-key vertical layout can be narrowed to at most 44 pixels");
                Preview(keyboard, "v1.2.0-vertical.png");

                Set(settings, "Columns", 4);
                Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                Check(tools.Select(c => c.Top).Distinct().Count() == 1, "switching a 40px vertical layout to four columns immediately places all toolbar icons in one row");
                keyboard.Height = keyboard.MinimumSize.Height;
                CheckLayout(keyboard, "vertical-to-horizontal minimum layout");

                Set(settings, "Keys", String.Join(" ", Enumerable.Repeat("E", 24)));
                Set(settings, "Columns", 12);
                Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                keyboard.Size = keyboard.MinimumSize;
                CheckLayout(keyboard, "24-key, 12-column minimum layout");
                Set(settings, "Keys", "3 E D C");
                Set(settings, "Columns", 4);
                Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                keyboard.Width = 284; keyboard.Height = 104;

                // Move this hidden test form away from the cursor, without moving the cursor.
                keyboard.Location = new Point(-16000, -16000);
                DateTime now = DateTime.UtcNow;
                Set(settings, "FadeEnabled", true);
                Set(settings, "FadeOpacityPercent", 30);
                Set(keyboard, "lastActivity", now);
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(3.9));
                Check(keyboard.Opacity == 1, "fade waits for four seconds of inactivity");
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(4));
                Check(keyboard.Opacity > 0.30 && keyboard.Opacity < 1, "fade begins gradually instead of jumping to the target");
                for (int i = 1; i <= 20; i++) Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(4 + i * 0.05));
                Check(Math.Abs(keyboard.Opacity - 0.30) < 0.005, "fade stops at the default 30 percent opacity");
                foreach (int opacity in new[] { 1, 20, 65, 70, 100 })
                {
                    Set(settings, "FadeOpacityPercent", opacity); keyboard.Opacity = 1;
                    Set(keyboard, "lastActivity", now);
                    for (int i = 0; i <= 20; i++) Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(4 + i * 0.05));
                    Check(Math.Abs(keyboard.Opacity - opacity / 100.0) < 0.005, "fade honors configured opacity: " + opacity + " percent");
                }
                Call(Type("KeyboardWindow"), keyboard, "TouchActivity");
                Check(keyboard.Opacity == 1, "activity immediately restores full opacity");
                keyboard.Location = new Point(Cursor.Position.X - 10, Cursor.Position.Y - 10);
                keyboard.Opacity = 0.65; Set(keyboard, "lastActivity", now.AddMinutes(-1));
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now);
                Check(keyboard.Opacity == 1, "cursor presence over any part of the window restores full opacity");
                keyboard.Location = new Point(-16000, -16000);
                foreach (string state in new[] { "interacting", "dialogOpen" })
                {
                    keyboard.Opacity = 0.65; Set(keyboard, "lastActivity", now.AddMinutes(-1)); Set(keyboard, state, true);
                    Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now);
                    Check(keyboard.Opacity == 1, state + " suspends fading and restores opacity");
                    Set(keyboard, state, false);
                }
                Set(settings, "FadeEnabled", false); keyboard.Opacity = 0.65;
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now);
                Check(keyboard.Opacity == 1, "disabling fade restores and retains full opacity");
                CheckToolWindowStyle(keyboard, "opacity updates preserve the tool-window and no-activate styles");

                Rectangle normalBounds = new Rectangle(173, 241, keyboard.Width, keyboard.Height);
                keyboard.Bounds = normalBounds;
                Call(Type("KeyboardWindow"), keyboard, "MinimizeToTray");
                Check(!keyboard.Visible && keyboard.WindowState == FormWindowState.Normal && keyboard.Bounds == normalBounds, "tray minimize hides the component without changing its normal bounds or state");
                Check(!tray.Visible, "tray minimize does not display a notification icon for an unshown component");
                object traySaved = Call(Type("Settings"), null, "Load");
                Check((int)Get(traySaved, "X") == normalBounds.X && (int)Get(traySaved, "Y") == normalBounds.Y && (int)Get(traySaved, "Width") == normalBounds.Width && (int)Get(traySaved, "Height") == normalBounds.Height, "tray minimize saves the normal position and size");
                Call(Type("KeyboardWindow"), keyboard, "Save");
                keyboard.WindowState = FormWindowState.Minimized;
                Check(keyboard.WindowState == FormWindowState.Minimized, "hidden component accepts minimized state");
                keyboard.Opacity = 0.65;
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", DateTime.UtcNow);
                Check(keyboard.Opacity == 1, "minimized state suspends fading");
                Rectangle restore = keyboard.RestoreBounds;
                Check(restore == normalBounds, "minimized component retains normal restore bounds");
                Check((bool)Call(Type("KeyboardWindow"), keyboard, "Save"), "settings save succeeds while minimized");
                object saved = Call(Type("Settings"), null, "Load");
                Check((int)Get(saved, "X") == restore.X && (int)Get(saved, "Y") == restore.Y && (int)Get(saved, "Width") == restore.Width && (int)Get(saved, "Height") == restore.Height, "minimized save persists normal restore position and size");
                keyboard.WindowState = FormWindowState.Normal;
                Check(keyboard.Bounds == normalBounds, "component restores its original position and size");
                Check(!keyboard.Visible, "component checks never display their window");
                keyboard.Close();
            }
            Check(trayDisposed && trayMenuForDisposal.IsDisposed, "closing the component disposes its tray icon and menu");
            Check(IconDisposed(appIconForDisposal), "closing the component releases its owned application icon");
            using (var directDisposeKeyboard = (Form)Activator.CreateInstance(Type("KeyboardWindow"), true))
            {
                var directTrayField = Type("KeyboardWindow").GetFields(Members).Single(f => typeof(NotifyIcon).IsAssignableFrom(f.FieldType));
                var directTray = (NotifyIcon)directTrayField.GetValue(directDisposeKeyboard);
                var directMenu = directTray.ContextMenuStrip;
                var directAppIcon = (Icon)Get(directDisposeKeyboard, "appIcon");
                bool directTrayDisposed = false;
                directTray.Disposed += delegate { directTrayDisposed = true; };
                Check(!directDisposeKeyboard.Visible && !directTray.Visible, "direct-dispose component creates neither a displayed window nor a tray icon");
                directDisposeKeyboard.Dispose();
                Check(directDisposeKeyboard.IsDisposed && directTrayDisposed && directMenu.IsDisposed, "direct Dispose releases the tray icon and menu without requiring Close");
                Check(IconDisposed(directAppIcon), "direct Dispose also releases its owned application icon");
            }
            File.Delete((string)settingsPath.GetValue(null));
            SettingsDialogTests();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalCulture;
            Type("Ui").GetField("Language", Members).SetValue(null, "zh");
        }
    }

    private static void SettingsDialogTests()
    {
        foreach (string language in new[] { "zh", "en" })
        {
            object settings = Activator.CreateInstance(Type("Settings"), true);
            Set(settings, "Language", language);
            Type("Ui").GetField("Language", Members).SetValue(null, language);
            using (var dialog = (Form)Activator.CreateInstance(Type("SettingsDialog"), Members, null, new[] { settings }, CultureInfo.InvariantCulture))
            {
                IntPtr handle = dialog.Handle;
                Check(dialog.Text == (language == "zh" ? "自定义补键" : "MiniKeys settings"), "settings dialog uses the selected UI language: " + language);
                Check((string)Type("SettingsDialog").GetProperty("Language", Members).GetValue(dialog, null) == language, "settings language selector starts from the saved choice: " + language);
                Check((string)Type("SettingsDialog").GetProperty("VoiceInput", Members).GetValue(dialog, null) == "wechat", "settings default to WeChat voice input: " + language);
                Check((bool)Type("SettingsDialog").GetProperty("FadeEnabled", Members).GetValue(dialog, null) && (int)Type("SettingsDialog").GetProperty("FadeOpacityPercent", Members).GetValue(dialog, null) == 30, "settings show enabled fading at 30 percent opacity: " + language);
                var opacityControl = (NumericUpDown)Get(dialog, "fadeOpacity");
                Check(opacityControl.Minimum == 1 && opacityControl.Maximum == 100, "settings allow the full 1 to 100 percent opacity range: " + language);
                Check(dialog.Controls.Cast<Control>().All(c => dialog.ClientRectangle.Contains(c.Bounds)), "settings controls fit inside the dialog: " + language);
                ((ComboBox)Get(dialog, "language")).SelectedIndex = language == "en" ? 0 : 1;
                Check((string)Type("SettingsDialog").GetProperty("Language", Members).GetValue(dialog, null) == (language == "en" ? "zh" : "en") && (string)Get(settings, "Language") == language && (string)Type("Ui").GetField("Language", Members).GetValue(null) == language, "language dropdown changes are staged until settings are saved: " + language);
                Check(!dialog.Visible, "settings component never displays its window: " + language);
            }
        }
    }

    private sealed class InputHost : Form
    {
        private readonly TextBox editor = new TextBox { Multiline = true, Font = new Font("Consolas", 20), Location = new Point(20, 60), Size = new Size(540, 100) };
        private readonly Timer timer = new Timer { Interval = 250 };
        private Form keyboard;
        private Control[] keyTiles;
        private int step;
        private int focusWaitTicks;
        internal Exception Error;
        private Point oldCursor;
        private bool shiftDown;
        private Rectangle beforeDrag;

        internal InputHost()
        {
            Text = "MiniKeys isolated input verification";
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Left + 70, Screen.PrimaryScreen.WorkingArea.Top + 70);
            ClientSize = new Size(580, 190);
            Controls.Add(new Label { Text = "Temporary test input only. Closes after verification.", AutoSize = true, Location = new Point(20, 20) });
            Controls.Add(editor);
            timer.Tick += Tick;
            Shown += delegate
            {
                oldCursor = Cursor.Position;
                keyboard = (Form)Activator.CreateInstance(Type("KeyboardWindow"), true);
                keyboard.Location = new Point(Left, Bottom + 25);
                keyboard.Show();
                keyTiles = keyboard.Controls.Cast<Control>().Where(c => new[] { "3", "E", "D", "C" }.Contains(c.Text)).OrderBy(c => c.Top).ThenBy(c => c.Left).ToArray();
                Activate(); editor.Focus();
                Native.SetForegroundWindow(Handle);
                timer.Start();
            };
            FormClosed += delegate
            {
                timer.Stop(); timer.Dispose();
                if (shiftDown) Native.Shift(false);
                if (keyboard != null) { keyboard.Close(); keyboard.Dispose(); }
                Cursor.Position = oldCursor;
            };
        }

        private void Tick(object sender, EventArgs e)
        {
            try
            {
                if (step == 0)
                {
                    if (Native.GetForegroundWindow() != Handle || !editor.Focused)
                    {
                        if (++focusWaitTicks < 120) return;
                        throw new InvalidOperationException("Test host could not obtain foreground focus; no input was sent.");
                    }
                    Check(keyTiles.Length == 4, "window exposes exactly the default four key tiles");
                    Check((Native.GetWindowLong(keyboard.Handle, -20) & 0x08000000) != 0, "WS_EX_NOACTIVATE is set");
                    Check(Native.SendMessage(keyboard.Handle, 0x21, Handle, (IntPtr)(1 | (0x201 << 16))) == (IntPtr)3, "WM_MOUSEACTIVATE returns MA_NOACTIVATE");
                    Check(Native.GetForegroundWindow() == Handle && editor.Focused, "isolated text box has foreground focus before clicks");
                }
                else if (step >= 1 && step <= 4) Native.Click(keyTiles[step - 1]);
                else if (step == 5)
                {
                    SendKey("Enter"); // The installed Chinese IME commits its raw composition on Enter.
                }
                else if (step == 6)
                {
                    Check(editor.Text.TrimEnd('\r', '\n') == "3edc", "real mouse clicks type 3edc and Enter commits its composition (actual: " + editor.Text.TrimEnd('\r', '\n') + ")");
                    Check(Native.GetForegroundWindow() == Handle && editor.Focused, "key clicks preserve target window and caret focus");
                    editor.Clear();
                    Native.Shift(true); shiftDown = true;
                }
                else if (step == 7) Native.Click(keyTiles[1]);
                else if (step == 8) { Native.Shift(false); shiftDown = false; }
                else if (step == 9) SendKey("Enter");
                else if (step == 10)
                {
                    Check(editor.Text.TrimEnd('\r', '\n') == "E", "scan-code input respects held Shift (actual: " + editor.Text.TrimEnd('\r', '\n') + ")");
                    Check(Native.GetForegroundWindow() == Handle && editor.Focused, "Shift click preserves focus");
                    editor.Clear();
                }
                else if (step == 11) Native.Click(keyTiles[3]);
                else if (step == 12) Native.Click(keyTiles[1]);
                else if (step == 13) SendKey("Space");
                else if (step == 14)
                {
                    Check(editor.Text.Any(c => c >= '\u4e00' && c <= '\u9fff'), "C/E mouse clicks feed Chinese IME and Space commits a Chinese character (actual: " + editor.Text + ")");
                    Check(Native.GetForegroundWindow() == Handle && editor.Focused, "Chinese IME composition preserves target focus");
                    Check(Native.SendMessage(keyboard.Handle, 0x84, IntPtr.Zero, Native.Pack(keyboard.Right - 2, keyboard.Bottom - 2)) == (IntPtr)17, "bottom-right border exposes native resize hit target");
                    using (var bitmap = new Bitmap(keyboard.Width, keyboard.Height))
                    {
                        keyboard.DrawToBitmap(bitmap, keyboard.ClientRectangle);
                        bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.png"));
                    }
                    beforeDrag = keyboard.Bounds;
                    Native.Drag(new Point(keyboard.Right - 2, keyboard.Bottom - 2), 45, 30);
                }
                else if (step == 16)
                {
                    Check(keyboard.Width > beforeDrag.Width + 10 && keyboard.Height > beforeDrag.Height + 10, "native mouse drag resizes the bottom-right border");
                    beforeDrag = keyboard.Bounds;
                    Native.Drag(new Point(keyboard.Left + keyboard.Width / 2, keyboard.Top + 7), 40, 25);
                }
                else if (step == 18)
                {
                    Check(keyboard.Left > beforeDrag.Left + 10 && keyboard.Top > beforeDrag.Top + 10 && keyboard.Size == beforeDrag.Size, "native mouse drag moves the title strip without resizing");
                }
                else if (step == 21)
                {
                    var saved = Call(Type("Settings"), null, "Load");
                    Check((int)Get(saved, "X") == keyboard.Left && (int)Get(saved, "Y") == keyboard.Top && (int)Get(saved, "Width") == keyboard.Width && (int)Get(saved, "Height") == keyboard.Height, "moving and resizing are saved after debounce");
                }
                else if (step == 22) Native.Click((Control)Get(keyboard, "pin"));
                else if (step == 23)
                {
                    var saved = Call(Type("Settings"), null, "Load");
                    Check(!keyboard.TopMost && !(bool)Get(saved, "Pinned"), "pin toggle removes topmost state and persists it");
                    Native.Click((Control)Get(keyboard, "pin"));
                }
                else if (step == 24)
                {
                    Check(keyboard.TopMost, "pin toggle restores topmost state");
                    beforeDrag = keyboard.Bounds;
                    Native.Click((Control)Get(keyboard, "minimize"));
                }
                else if (step == 25)
                {
                    Check(!keyboard.Visible && keyboard.WindowState == FormWindowState.Normal, "minimize icon hides the displayed keyboard in the tray");
                    Check((bool)Call(Type("KeyboardWindow"), keyboard, "Save"), "displayed minimized keyboard saves successfully");
                    var saved = Call(Type("Settings"), null, "Load");
                    Check((int)Get(saved, "X") == beforeDrag.X && (int)Get(saved, "Y") == beforeDrag.Y && (int)Get(saved, "Width") == beforeDrag.Width && (int)Get(saved, "Height") == beforeDrag.Height, "native minimized coordinates do not replace normal saved bounds");
                    Call(Type("KeyboardWindow"), keyboard, "RestoreFromTray");
                }
                else if (step == 26)
                {
                    Check(keyboard.Visible && keyboard.Bounds == beforeDrag, "displayed keyboard restores the original bounds from the tray");
                    Check(Native.GetForegroundWindow() == Handle && editor.Focused, "programmatic restore preserves target focus");
                    editor.Clear();
                    Native.Click(keyTiles[0]);
                }
                else if (step == 27) SendKey("Enter");
                else if (step == 28)
                {
                    Check(editor.Text.TrimEnd('\r', '\n') == "3", "keyboard sends input after minimizing and restoring");
                    Close();
                }
                step++;
            }
            catch (Exception exception) { Error = exception; Close(); }
        }

        private static void SendKey(string key)
        {
            Check((bool)Call(Type("Native"), null, "SendKey", Parse(key)[0]), "send test " + key + " through the app input function");
        }
    }

    private static class Native
    {
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")] internal static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] input, int size);
        [StructLayout(LayoutKind.Sequential)] private struct INPUT { internal uint type; internal UNION data; }
        [StructLayout(LayoutKind.Explicit)] private struct UNION { [FieldOffset(0)] internal MOUSE mouse; [FieldOffset(0)] internal KEY keyboard; }
        [StructLayout(LayoutKind.Sequential)] private struct MOUSE { internal int dx, dy; internal uint mouseData, flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] private struct KEY { internal ushort vk, scan; internal uint flags, time; internal UIntPtr extra; }
        internal static IntPtr Pack(int x, int y) { return (IntPtr)((y << 16) | (x & 0xffff)); }
        internal static void Click(Control control)
        {
            var point = control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
            Cursor.Position = point;
            var inputs = new INPUT[2];
            inputs[0].data.mouse.flags = 0x0002;
            inputs[1].data.mouse.flags = 0x0004;
            if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) != 2) throw new InvalidOperationException("Unable to inject isolated test mouse click");
        }
        internal static void Shift(bool down)
        {
            var input = new INPUT[1]; input[0].type = 1;
            input[0].data.keyboard.vk = 0x10; input[0].data.keyboard.flags = down ? 0U : 2U;
            if (SendInput(1, input, Marshal.SizeOf(typeof(INPUT))) != 1) throw new InvalidOperationException("Unable to inject test Shift state");
        }
        internal static void Drag(Point start, int dx, int dy)
        {
            Cursor.Position = start;
            var inputs = new INPUT[3];
            inputs[0].data.mouse.flags = 0x0002;
            inputs[1].data.mouse.flags = 0x0001; inputs[1].data.mouse.dx = dx; inputs[1].data.mouse.dy = dy;
            inputs[2].data.mouse.flags = 0x0004;
            if (SendInput(3, inputs, Marshal.SizeOf(typeof(INPUT))) != 3) throw new InvalidOperationException("Unable to inject isolated test drag");
        }
    }
}
