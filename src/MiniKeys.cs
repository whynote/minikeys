using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Forms;

namespace MiniKeys
{
    internal static class Ui
    {
        internal static string Text(string chinese, string english)
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? chinese : english;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var instance = new Mutex(true, "Local\\MiniKeys.Desktop", out created))
            {
                if (!created) return;
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new KeyboardWindow());
            }
        }
    }

    [DataContract]
    internal sealed class Settings
    {
        [DataMember] public string Keys = "3 E D C";
        [DataMember] public int Columns = 4;
        [DataMember] public int X = -1;
        [DataMember] public int Y = -1;
        [DataMember] public int Width = 284;
        [DataMember] public int Height = 104;
        [DataMember] public bool Pinned = true;
        [DataMember] public bool FadeEnabled = true;
        internal static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniKeys", "settings.json");

        [OnDeserializing]
        private void BeforeLoad(StreamingContext context) { FadeEnabled = true; }

        internal static Settings Load()
        {
            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var settings = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
                    if (settings == null) throw new SerializationException("Empty settings");
                    KeyMap.Parse(settings.Keys);
                    settings.Columns = Math.Max(1, Math.Min(12, settings.Columns));
                    return settings;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is SerializationException || ex is ArgumentException)) throw;
                return new Settings();
            }
        }

        internal bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temporary = FilePath + ".tmp";
                using (var stream = File.Create(temporary)) new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, this);
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException)) throw;
                return false;
            }
        }
    }

    internal sealed class KeySpec
    {
        internal string Name;
        internal ushort Code;
        internal bool Extended;
        internal KeySpec(string name, ushort code, bool extended = false) { Name = name; Code = code; Extended = extended; }
    }

    internal static class KeyMap
    {
        private static readonly Dictionary<string, KeySpec> Special = new Dictionary<string, KeySpec>(StringComparer.OrdinalIgnoreCase)
        {
            {"Space", new KeySpec("Space", 0x20)}, {"Enter", new KeySpec("Enter", 0x0D)},
            {"Backspace", new KeySpec("Backspace", 0x08)}, {"Tab", new KeySpec("Tab", 0x09)},
            {"Esc", new KeySpec("Esc", 0x1B)}, {"Delete", new KeySpec("Delete", 0x2E, true)},
            {"Left", new KeySpec("Left", 0x25, true)}, {"Up", new KeySpec("Up", 0x26, true)},
            {"Right", new KeySpec("Right", 0x27, true)}, {"Down", new KeySpec("Down", 0x28, true)},
            {"Home", new KeySpec("Home", 0x24, true)}, {"End", new KeySpec("End", 0x23, true)},
            {"PageUp", new KeySpec("PageUp", 0x21, true)}, {"PageDown", new KeySpec("PageDown", 0x22, true)},
            {"-", new KeySpec("-", 0xBD)}, {"=", new KeySpec("=", 0xBB)},
            {"[", new KeySpec("[", 0xDB)}, {"]", new KeySpec("]", 0xDD)},
            {";", new KeySpec(";", 0xBA)}, {"'", new KeySpec("'", 0xDE)},
            {",", new KeySpec(",", 0xBC)}, {".", new KeySpec(".", 0xBE)},
            {"/", new KeySpec("/", 0xBF)}, {"\\", new KeySpec("\\", 0xDC)}, {"`", new KeySpec("`", 0xC0)}
        };

        internal static List<KeySpec> Parse(string text)
        {
            var result = new List<KeySpec>();
            foreach (string token in (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = token.ToUpperInvariant();
                KeySpec special;
                int number;
                if (name.Length == 1 && ((name[0] >= 'A' && name[0] <= 'Z') || (name[0] >= '0' && name[0] <= '9')))
                    result.Add(new KeySpec(name, name[0]));
                else if (Special.TryGetValue(token, out special)) result.Add(special);
                else if (name.StartsWith("F") && int.TryParse(name.Substring(1), out number) && number >= 1 && number <= 12)
                    result.Add(new KeySpec("F" + number, (ushort)(0x6F + number)));
                else throw new ArgumentException(Ui.Text("不支持这个按键：", "Unsupported key: ") + token + Ui.Text("。请用空格分隔，例如：3 E D C Space", ". Separate keys with spaces, e.g. 3 E D C Space"));
            }
            if (result.Count == 0 || result.Count > 24) throw new ArgumentException(Ui.Text("请设置 1 到 24 个按键，用空格分隔。", "Enter 1 to 24 key names, separated by spaces."));
            return result;
        }
    }

    internal static class Native
    {
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, IntPtr processId);
        [DllImport("user32.dll")] internal static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll")] internal static extern uint MapVirtualKeyEx(uint code, uint type, IntPtr layout);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, INPUT[] inputs, int size);
        [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] internal struct INPUT { internal uint type; internal INPUTUNION data; }
        [StructLayout(LayoutKind.Explicit)] internal struct INPUTUNION
        {
            [FieldOffset(0)] internal KEYBDINPUT keyboard;
            [FieldOffset(0)] internal MOUSEINPUT mouse;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct KEYBDINPUT { internal ushort vk, scan; internal uint flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct MOUSEINPUT { internal int dx, dy; internal uint data, flags, time; internal UIntPtr extra; }

        internal static bool SendKey(KeySpec key)
        {
            IntPtr target = GetForegroundWindow();
            if (target == IntPtr.Zero) return false;
            uint scan = MapVirtualKeyEx(key.Code, 0, GetKeyboardLayout(GetWindowThreadProcessId(target, IntPtr.Zero)));
            if (scan == 0) return false;
            uint flags = 0x0008U | (key.Extended ? 0x0001U : 0);
            var events = new INPUT[2];
            events[0].type = events[1].type = 1;
            events[0].data.keyboard = new KEYBDINPUT { scan = (ushort)scan, flags = flags };
            events[1].data.keyboard = new KEYBDINPUT { scan = (ushort)scan, flags = flags | 0x0002U };
            return SendInput(2, events, Marshal.SizeOf(typeof(INPUT))) == 2;
        }
    }

    // Non-selectable controls preserve the foreground application's caret and IME composition.
    internal sealed class Tile : Control
    {
        internal bool Toolbar;
        internal bool Highlight;
        internal string Icon;
        private bool hovered, pressed;
        internal Tile(string text, bool toolbar, Action action)
        {
            Text = text; Toolbar = toolbar; Cursor = Cursors.Hand; TabStop = false;
            AccessibleName = text; AccessibleRole = AccessibleRole.PushButton;
            SetStyle(ControlStyles.Selectable, false);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Click += delegate { action(); };
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rectangle = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = Rounded(rectangle, Toolbar ? 5 : 9))
            using (var fill = new SolidBrush(pressed ? Color.FromArgb(199, 217, 255) : hovered ? Color.FromArgb(226, 235, 252) : Toolbar ? Color.FromArgb(245, 247, 251) : Color.White))
            using (var border = new Pen(Toolbar ? Color.FromArgb(245, 247, 251) : Color.FromArgb(220, 225, 233)))
            {
                e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path);
            }
            if (Icon != null) { DrawIcon(e.Graphics); return; }
            float size = Toolbar ? 12 : Math.Min(28, Math.Max(9, Math.Min(Height * 0.38f, Width / (Text.Length > 2 ? Text.Length * 0.62f : 2.3f))));
            using (var font = new Font("Microsoft YaHei UI", size, Toolbar ? FontStyle.Regular : FontStyle.Bold, GraphicsUnit.Pixel))
                TextRenderer.DrawText(e.Graphics, Text, font, ClientRectangle, Highlight ? Color.FromArgb(39, 95, 202) : Color.FromArgb(43, 52, 69), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        private void DrawIcon(Graphics graphics)
        {
            Color color = Highlight ? Color.FromArgb(39, 95, 202) : Color.FromArgb(79, 90, 108);
            using (var pen = new Pen(color, 1.5f))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;
                var state = graphics.Save();
                graphics.TranslateTransform(Width / 2f, Height / 2f);
                if (Icon == "pin")
                {
                    if (!Highlight) graphics.RotateTransform(35);
                    graphics.DrawLine(pen, -3, -6, 3, -6);
                    graphics.DrawLine(pen, -2, -5, -2, -1); graphics.DrawLine(pen, 2, -5, 2, -1);
                    graphics.DrawLines(pen, new[] { new Point(-2, -1), new Point(-5, 2), new Point(5, 2), new Point(2, -1) });
                    graphics.DrawLine(pen, 0, 2, 0, 7);
                }
                else if (Icon == "settings")
                {
                    graphics.DrawEllipse(pen, -4, -4, 8, 8); graphics.DrawEllipse(pen, -1, -1, 2, 2);
                    for (int angle = 0; angle < 360; angle += 45)
                    {
                        double radians = angle * Math.PI / 180;
                        graphics.DrawLine(pen, (float)(Math.Cos(radians) * 5), (float)(Math.Sin(radians) * 5), (float)(Math.Cos(radians) * 7), (float)(Math.Sin(radians) * 7));
                    }
                }
                else if (Icon == "minimize") graphics.DrawLine(pen, -5, 2, 5, 2);
                else if (Icon == "close") { graphics.DrawLine(pen, -4, -4, 4, 4); graphics.DrawLine(pen, -4, 4, 4, -4); }
                graphics.Restore(state);
            }
        }
        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = Math.Max(2, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90); path.CloseFigure();
            return path;
        }
    }

    internal sealed class KeyboardWindow : Form
    {
        private readonly Settings settings = Settings.Load();
        private readonly List<Tile> keys = new List<Tile>();
        private readonly ToolTip hints = new ToolTip();
        private readonly System.Windows.Forms.Timer saveTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer { Interval = 50 };
        private Tile pin, configure, minimize, close;
        private bool ready;
        private bool layingOut, interacting, dialogOpen;
        private DateTime lastActivity = DateTime.UtcNow;

        internal KeyboardWindow()
        {
            Text = Ui.Text("迷你补键盘", "MiniKeys"); BackColor = Color.FromArgb(245, 247, 251);
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true; TopMost = settings.Pinned;
            pin = new Tile("", true, TogglePin) { Icon = "pin", Highlight = settings.Pinned, AccessibleName = "Pin" };
            configure = new Tile("", true, Configure) { Icon = "settings", AccessibleName = "Settings" };
            minimize = new Tile("", true, delegate { TouchActivity(); WindowState = FormWindowState.Minimized; }) { Icon = "minimize", AccessibleName = "Minimize" };
            close = new Tile("", true, Close) { Icon = "close", AccessibleName = "Close" };
            Controls.AddRange(new Control[] { pin, configure, minimize, close });
            foreach (var tool in new[] { pin, configure, minimize, close }) tool.MouseEnter += delegate { TouchActivity(); };
            UpdatePinHint(); hints.SetToolTip(configure, Ui.Text("自定义按键和排列", "Customize keys and layout"));
            hints.SetToolTip(minimize, Ui.Text("最小化到任务栏", "Minimize to taskbar")); hints.SetToolTip(close, Ui.Text("退出迷你补键盘", "Exit MiniKeys"));
            RebuildKeys();
            Size = new Size(Math.Max(MinimumSize.Width, Math.Min(1600, settings.Width)), Math.Max(MinimumSize.Height, Math.Min(1000, settings.Height)));
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = settings.X == -1 && settings.Y == -1 ? new Point(area.Right - Width - 40, area.Bottom - Height - 60) : new Point(settings.X, settings.Y);
            area = Screen.FromRectangle(Bounds).WorkingArea;
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
            saveTimer.Tick += delegate { saveTimer.Stop(); Save(); };
            fadeTimer.Tick += delegate { UpdateFade(DateTime.UtcNow); };
            fadeTimer.Start();
            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                TouchActivity();
                if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
            };
            ready = true;
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x00040000; return p; }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21) { m.Result = (IntPtr)3; return; } // MA_NOACTIVATE
            if (m.Msg == 0x84)
            {
                Point p = PointToClient(new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16))));
                bool left = p.X < 5, right = p.X >= Width - 6, top = p.Y < 5, bottom = p.Y >= Height - 6;
                int hit = bottom ? (right ? 17 : left ? 16 : 15) : top ? (right ? 14 : left ? 13 : 12) : left ? 10 : right ? 11 : 1;
                m.Result = (IntPtr)hit; return;
            }
            base.WndProc(ref m);
        }
        private void RebuildKeys()
        {
            foreach (var tile in keys) { Controls.Remove(tile); tile.Dispose(); }
            keys.Clear();
            foreach (var spec in KeyMap.Parse(settings.Keys))
            {
                var captured = spec;
                var tile = new Tile(spec.Name, false, delegate
                {
                    TouchActivity();
                    if (!Native.SendKey(captured)) MessageBox.Show(Ui.Text("未能发送按键。请先点击目标输入框。若目标程序以管理员身份运行，也需要以管理员身份运行补键盘。", "Could not send the key. Click the target text field first. If the target runs as administrator, run MiniKeys with the same privileges."), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                tile.MouseEnter += delegate { TouchActivity(); };
                hints.SetToolTip(tile, Ui.Text("发送 ", "Send ") + spec.Name + Ui.Text("（跟随当前输入法；可配合实体 Shift / Ctrl）", " (uses the active input method; supports physical Shift / Ctrl)"));
                keys.Add(tile); Controls.Add(tile);
            }
            PerformLayout();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (pin == null || keys.Count == 0 || layingOut || WindowState != FormWindowState.Normal) return;
            layingOut = true;
            try
            {
            int cols = Math.Min(settings.Columns, keys.Count), rows = (keys.Count + cols - 1) / cols;
            int minWidth = Math.Max(40, 12 + cols * 28);
            int toolCols = Math.Min(4, Math.Max(1, (Math.Max(ClientSize.Width, minWidth) - 12) / 22));
            int toolRows = (4 + toolCols - 1) / toolCols;
            int keyTop = 12 + toolRows * 22 + 4;
            MinimumSize = new Size(minWidth, keyTop + rows * 28 + 6);
            var tools = new[] { pin, configure, minimize, close };
            int toolLeft = Math.Max(6, ClientSize.Width - 6 - toolCols * 22);
            for (int i = 0; i < tools.Length; i++) tools[i].SetBounds(toolLeft + i % toolCols * 22, 12 + i / toolCols * 22, 20, 20);
            int availableWidth = ClientSize.Width - 12, availableHeight = ClientSize.Height - keyTop - 6;
            for (int i = 0; i < keys.Count; i++)
            {
                int col = i % cols, row = i / cols;
                int x = 6 + col * availableWidth / cols, y = keyTop + row * availableHeight / rows;
                keys[i].SetBounds(x, y, (col + 1) * availableWidth / cols - col * availableWidth / cols - 4, (row + 1) * availableHeight / rows - row * availableHeight / rows - 4);
            }
            Invalidate();
            }
            finally { layingOut = false; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(196, 204, 216)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.DrawLine(pen, Width / 2 - 7, 7, Width / 2 + 7, 7);
                e.Graphics.DrawLine(pen, Width - 10, Height - 4, Width - 4, Height - 10);
                e.Graphics.DrawLine(pen, Width - 6, Height - 4, Width - 4, Height - 6);
            }
        }
        private void UpdatePinHint() { hints.SetToolTip(pin, TopMost ? Ui.Text("已置顶，点击取消", "Pinned: click to unpin") : Ui.Text("点击置顶", "Click to pin on top")); }
        private void TogglePin() { TouchActivity(); settings.Pinned = TopMost = !TopMost; pin.Highlight = TopMost; pin.Invalidate(); UpdatePinHint(); Save(); }
        internal void TouchActivity() { lastActivity = DateTime.UtcNow; if (Opacity != 1) Opacity = 1; }
        internal void UpdateFade(DateTime now)
        {
            if (!settings.FadeEnabled || WindowState != FormWindowState.Normal || interacting || dialogOpen || Bounds.Contains(Cursor.Position))
            {
                lastActivity = now; if (Opacity != 1) Opacity = 1; return;
            }
            if ((now - lastActivity).TotalSeconds >= 4) Opacity = Math.Max(0.65, Opacity - 0.05);
        }
        private void Configure()
        {
            TouchActivity(); dialogOpen = true;
            try
            {
            using (var dialog = new SettingsDialog(settings.Keys, settings.Columns, settings.FadeEnabled))
            {
                dialog.TopMost = true;
                if (dialog.ShowDialog() != DialogResult.OK) return;
                settings.Keys = dialog.KeyText; settings.Columns = dialog.Columns; settings.FadeEnabled = dialog.FadeEnabled;
                RebuildKeys();
                if (!Save()) MessageBox.Show(Ui.Text("设置已应用，但无法保存到：\n", "Settings applied, but could not save to:\n") + Settings.FilePath, Text);
            }
            }
            finally { dialogOpen = false; TouchActivity(); }
        }
        private bool Save()
        {
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            settings.X = bounds.Left; settings.Y = bounds.Top; settings.Width = bounds.Width; settings.Height = bounds.Height;
            return settings.Save();
        }
        protected override void OnLocationChanged(EventArgs e) { base.OnLocationChanged(e); ScheduleSave(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); ScheduleSave(); }
        private void ScheduleSave() { if (ready) { saveTimer.Stop(); saveTimer.Start(); } }
        protected override void OnResizeBegin(EventArgs e) { interacting = true; TouchActivity(); base.OnResizeBegin(e); }
        protected override void OnResizeEnd(EventArgs e) { interacting = false; TouchActivity(); base.OnResizeEnd(e); }
        protected override void OnFormClosed(FormClosedEventArgs e) { Save(); saveTimer.Dispose(); fadeTimer.Dispose(); hints.Dispose(); base.OnFormClosed(e); }
    }

    internal sealed class SettingsDialog : Form
    {
        private readonly TextBox keyText = new TextBox();
        private readonly NumericUpDown columns = new NumericUpDown { Minimum = 1, Maximum = 12 };
        private readonly CheckBox fade = new CheckBox { AutoSize = true };
        internal string KeyText { get { return keyText.Text.Trim(); } }
        internal int Columns { get { return (int)columns.Value; } }
        internal bool FadeEnabled { get { return fade.Checked; } }
        internal SettingsDialog(string keys, int count, bool fadeEnabled)
        {
            Text = Ui.Text("自定义补键", "MiniKeys settings"); Font = new Font("Microsoft YaHei UI", 9);
            ClientSize = new Size(430, 302); FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            Controls.Add(new Label { Text = Ui.Text("要显示的按键（用空格分隔，可调整顺序）", "Keys to display (separated by spaces, in order)"), AutoSize = true, Location = new Point(18, 18) });
            keyText.Text = keys; keyText.SetBounds(20, 46, 388, 28); Controls.Add(keyText);
            Controls.Add(new Label { Text = Ui.Text("支持 A–Z、0–9、F1–F12、标点，以及：\n", "A–Z, 0–9, F1–F12, punctuation, and:\n") + "Space  Enter  Backspace  Tab  Esc  Delete\nLeft  Right  Up  Down  Home  End  PageUp  PageDown", AutoSize = true, Location = new Point(18, 86) });
            Controls.Add(new Label { Text = Ui.Text("每行按键数", "Keys per row"), AutoSize = true, Location = new Point(18, 166) });
            columns.Value = count; columns.SetBounds(112, 162, 70, 26); Controls.Add(columns);
            Controls.Add(new Label { Text = Ui.Text("1 = 竖排；4 = 四键横排", "1 = vertical; 4 = horizontal"), AutoSize = true, Location = new Point(194, 166) });
            fade.Text = Ui.Text("闲置 4 秒后淡化至 65%，鼠标回来恢复", "Fade to 65% after 4s away; restore on hover"); fade.Checked = fadeEnabled; fade.Location = new Point(20, 206); Controls.Add(fade);
            var reset = new Button { Text = Ui.Text("恢复 3 E D C", "Reset 3 E D C"), Location = new Point(20, 254), Size = new Size(116, 30) };
            reset.Click += delegate { keyText.Text = "3 E D C"; columns.Value = 4; };
            var ok = new Button { Text = Ui.Text("保存", "Save"), Location = new Point(244, 254), Size = new Size(78, 30) };
            ok.Click += delegate
            {
                try { KeyMap.Parse(KeyText); DialogResult = DialogResult.OK; }
                catch (ArgumentException error) { MessageBox.Show(this, error.Message, Ui.Text("检查按键", "Check key names"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            var cancel = new Button { Text = Ui.Text("取消", "Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(330, 254), Size = new Size(78, 30) };
            Controls.AddRange(new Control[] { reset, ok, cancel }); AcceptButton = ok; CancelButton = cancel;
        }
    }
}
