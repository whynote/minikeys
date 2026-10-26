using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
            ModelTests();
            if (!args.Contains("--model-only"))
            {
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
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
        Check((bool)Call(Type("Settings"), settings, "Save"), "save to isolated configuration path");
        var loaded = Call(Type("Settings"), null, "Load");
        Check(new[] { "Keys", "Columns", "X", "Y", "Width", "Height", "Pinned" }.All(n => Object.Equals(Get(settings, n), Get(loaded, n))), "keys, layout, bounds and pin setting persist");
        File.WriteAllText((string)settingsPath.GetValue(null), "{invalid json");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C" && (int)Get(loaded, "Columns") == 4, "corrupt configuration restores defaults");
        File.WriteAllText((string)settingsPath.GetValue(null), "null");
        loaded = Call(Type("Settings"), null, "Load");
        Check((string)Get(loaded, "Keys") == "3 E D C", "null configuration restores defaults");
        File.Delete((string)settingsPath.GetValue(null));
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
                else if (step == 22) Native.Click(keyboard.Controls.Cast<Control>().Single(c => c.Text == "已置顶"));
                else if (step == 23)
                {
                    var saved = Call(Type("Settings"), null, "Load");
                    Check(!keyboard.TopMost && !(bool)Get(saved, "Pinned"), "pin toggle removes topmost state and persists it");
                    Native.Click(keyboard.Controls.Cast<Control>().Single(c => c.Text == "置顶"));
                }
                else if (step == 24)
                {
                    Check(keyboard.TopMost, "pin toggle restores topmost state");
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
