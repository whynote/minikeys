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
        Set(settings, "Keys", "E 3 Backspace"); Set(settings, "Columns", 2);
        Set(settings, "X", 173); Set(settings, "Y", 241); Set(settings, "Width", 347); Set(settings, "Height", 183); Set(settings, "Pinned", false);
        Set(settings, "FadeEnabled", false);
        Check((bool)Call(Type("Settings"), settings, "Save"), "save to isolated configuration path");
        var loaded = Call(Type("Settings"), null, "Load");
        Check(new[] { "Keys", "Columns", "X", "Y", "Width", "Height", "Pinned", "FadeEnabled" }.All(n => Object.Equals(Get(settings, n), Get(loaded, n))), "keys, layout, bounds, pin and fade settings persist");
        File.WriteAllText((string)settingsPath.GetValue(null), "{\"Keys\":\"3 E D C\",\"Columns\":1,\"X\":173,\"Y\":241,\"Width\":196,\"Height\":158,\"Pinned\":true}");
        loaded = Call(Type("Settings"), null, "Load");
        Check((bool)Get(loaded, "FadeEnabled") && (int)Get(loaded, "Columns") == 1, "v1 configuration enables the new fade default without changing layout");
        File.WriteAllText((string)settingsPath.GetValue(null), "{invalid json");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C" && (int)Get(loaded, "Columns") == 4, "corrupt configuration restores defaults");
        File.WriteAllText((string)settingsPath.GetValue(null), "null");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C", "null configuration restores defaults");
        File.Delete((string)settingsPath.GetValue(null));
    }

    private static Control[] KeyTiles(Form keyboard)
    {
        return ((IEnumerable)Get(keyboard, "keys")).Cast<Control>().ToArray();
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
            Check((string)Call(Type("Ui"), null, "Text", "中文", "English") == "English", "non-Chinese UI culture uses English");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            Check((string)Call(Type("Ui"), null, "Text", "中文", "English") == "中文", "Chinese UI culture uses Chinese");
            using (var keyboard = (Form)Activator.CreateInstance(Type("KeyboardWindow"), true))
            {
                // Creating handles and bitmaps does not show or activate this window.
                IntPtr handle = keyboard.Handle;
                var tools = new[] { "pin", "configure", "minimize", "close" }.Select(n => (Control)Get(keyboard, n)).ToArray();
                Check(tools.Select(c => (string)Get(c, "Icon")).SequenceEqual(new[] { "pin", "settings", "minimize", "close" }), "toolbar exposes four stable icon controls");
                Check(tools.All(c => !String.IsNullOrEmpty(c.AccessibleName) && !c.TabStop), "icon controls have accessible labels and do not take keyboard focus");
                Check((Native.GetWindowLong(handle, -20) & 0x08000000) != 0 && keyboard.ShowInTaskbar, "hidden window retains no-activate behavior and a taskbar entry");
                CheckLayout(keyboard, "default horizontal layout");
                Preview(keyboard, "v1.1.0.png");

                object settings = Get(keyboard, "settings");
                Set(settings, "Columns", 1);
                Call(Type("KeyboardWindow"), keyboard, "RebuildKeys");
                foreach (int width in new[] { 40, 55, 56, 77, 78, 99, 100, 196, 284 })
                {
                    keyboard.Width = width;
                    keyboard.Height = keyboard.MinimumSize.Height;
                    CheckLayout(keyboard, "vertical layout at requested width " + width);
                }
                keyboard.Width = keyboard.MinimumSize.Width;
                keyboard.Height = keyboard.MinimumSize.Height;
                Check(keyboard.Width <= 44, "four-key vertical layout can be narrowed to at most 44 pixels");
                Preview(keyboard, "v1.1.0-vertical.png");

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
                Set(keyboard, "lastActivity", now);
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(3.9));
                Check(keyboard.Opacity == 1, "fade waits for four seconds of inactivity");
                Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(4));
                Check(keyboard.Opacity > 0.65 && keyboard.Opacity < 1, "fade begins gradually instead of jumping to the target");
                for (int i = 1; i <= 20; i++) Call(Type("KeyboardWindow"), keyboard, "UpdateFade", now.AddSeconds(4 + i * 0.05));
                Check(Math.Abs(keyboard.Opacity - 0.65) < 0.005, "fade stops at a visible 65 percent opacity");
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

                Rectangle normalBounds = new Rectangle(173, 241, keyboard.Width, keyboard.Height);
                keyboard.Bounds = normalBounds;
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
            File.Delete((string)settingsPath.GetValue(null));
        }
        finally { Thread.CurrentThread.CurrentUICulture = originalCulture; }
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
                keyTiles = keyboard.Controls.Cast<Control>().Where(c => new[] { "3", "E", "D", "C" }.Contains(c.Text)).OrderBy(c => c.Left).ToArray();
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
                    Native.Drag(new Point(keyboard.Left + 45, keyboard.Top + 17), 40, 25);
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
                    Check(keyboard.WindowState == FormWindowState.Minimized, "minimize icon minimizes the displayed keyboard");
                    Check((bool)Call(Type("KeyboardWindow"), keyboard, "Save"), "displayed minimized keyboard saves successfully");
                    var saved = Call(Type("Settings"), null, "Load");
                    Check((int)Get(saved, "X") == beforeDrag.X && (int)Get(saved, "Y") == beforeDrag.Y && (int)Get(saved, "Width") == beforeDrag.Width && (int)Get(saved, "Height") == beforeDrag.Height, "native minimized coordinates do not replace normal saved bounds");
                    keyboard.WindowState = FormWindowState.Normal;
                }
                else if (step == 26)
                {
                    Check(keyboard.Bounds == beforeDrag, "displayed keyboard restores the original bounds");
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
