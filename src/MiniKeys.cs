using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Forms;

namespace MiniKeys
{
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
        internal static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniKeys", "settings.json");

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
                else throw new ArgumentException("不支持这个按键：" + token + "。请用空格分隔，例如：3 E D C Space");
            }
            if (result.Count == 0 || result.Count > 24) throw new ArgumentException("请设置 1 到 24 个按键，用空格分隔。");
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
            float size = Toolbar ? 12 : Math.Min(28, Math.Max(10, Math.Min(Height * 0.32f, Width / (Text.Length > 2 ? Text.Length * 0.62f : 2.7f))));
            using (var font = new Font("Microsoft YaHei UI", size, Toolbar ? FontStyle.Regular : FontStyle.Bold, GraphicsUnit.Pixel))
                TextRenderer.DrawText(e.Graphics, Text, font, ClientRectangle, Highlight ? Color.FromArgb(39, 95, 202) : Color.FromArgb(43, 52, 69), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
        private Tile pin, configure, close;
        private bool ready;

        internal KeyboardWindow()
        {
            Text = "迷你补键盘"; BackColor = Color.FromArgb(245, 247, 251);
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true; TopMost = settings.Pinned;
            pin = new Tile(settings.Pinned ? "已置顶" : "置顶", true, TogglePin) { Highlight = settings.Pinned };
            configure = new Tile("设置", true, Configure);
            close = new Tile("×", true, Close);
            Controls.AddRange(new Control[] { pin, configure, close });
            hints.SetToolTip(pin, "点击切换窗口置顶"); hints.SetToolTip(configure, "自定义按键和排列"); hints.SetToolTip(close, "退出迷你补键盘");
            RebuildKeys();
            Size = new Size(Math.Max(MinimumSize.Width, Math.Min(1600, settings.Width)), Math.Max(MinimumSize.Height, Math.Min(1000, settings.Height)));
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = settings.X == -1 && settings.Y == -1 ? new Point(area.Right - Width - 40, area.Bottom - Height - 60) : new Point(settings.X, settings.Y);
            area = Screen.FromRectangle(Bounds).WorkingArea;
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
            saveTimer.Tick += delegate { saveTimer.Stop(); Save(); };
            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && e.Y < 30) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
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
                    if (!Native.SendKey(captured)) MessageBox.Show("未能发送按键。请先点击目标输入框。若目标程序以管理员身份运行，也需要以管理员身份运行补键盘。", "迷你补键盘", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                hints.SetToolTip(tile, "发送 " + spec.Name + "（跟随当前输入法；可配合实体 Shift / Ctrl）");
                keys.Add(tile); Controls.Add(tile);
            }
            int cols = Math.Min(settings.Columns, keys.Count), rows = (keys.Count + cols - 1) / cols;
            MinimumSize = new Size(Math.Max(196, 22 + cols * 32), 38 + rows * 30);
            PerformLayout();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (pin == null) return;
            close.SetBounds(Width - 29, 6, 22, 22); configure.SetBounds(Width - 68, 6, 36, 22); pin.SetBounds(Width - 123, 6, 52, 22);
            if (keys.Count == 0) return;
            int cols = Math.Min(settings.Columns, keys.Count), rows = (keys.Count + cols - 1) / cols;
            int availableWidth = ClientSize.Width - 16, availableHeight = ClientSize.Height - 42;
            for (int i = 0; i < keys.Count; i++)
            {
                int col = i % cols, row = i / cols;
                int x = 8 + col * availableWidth / cols, y = 34 + row * availableHeight / rows;
                keys[i].SetBounds(x, y, (col + 1) * availableWidth / cols - col * availableWidth / cols - 4, (row + 1) * availableHeight / rows - row * availableHeight / rows - 4);
            }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var font = new Font("Microsoft YaHei UI", 11, FontStyle.Regular, GraphicsUnit.Pixel))
                TextRenderer.DrawText(e.Graphics, "补键 ···", font, new Rectangle(11, 8, 62, 20), Color.FromArgb(117, 126, 142), TextFormatFlags.VerticalCenter);
            using (var pen = new Pen(Color.FromArgb(196, 204, 216)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                e.Graphics.DrawLine(pen, Width - 10, Height - 4, Width - 4, Height - 10);
                e.Graphics.DrawLine(pen, Width - 6, Height - 4, Width - 4, Height - 6);
            }
        }
        private void TogglePin() { settings.Pinned = TopMost = !TopMost; pin.Text = TopMost ? "已置顶" : "置顶"; pin.Highlight = TopMost; pin.Invalidate(); Save(); }
        private void Configure()
        {
            using (var dialog = new SettingsDialog(settings.Keys, settings.Columns))
            {
                dialog.TopMost = true;
                if (dialog.ShowDialog() != DialogResult.OK) return;
                settings.Keys = dialog.KeyText; settings.Columns = dialog.Columns;
                RebuildKeys();
                if (!Save()) MessageBox.Show("设置已应用，但无法保存到：\n" + Settings.FilePath, "迷你补键盘");
            }
        }
        private bool Save()
        {
            settings.X = Left; settings.Y = Top; settings.Width = Width; settings.Height = Height;
            return settings.Save();
        }
        protected override void OnLocationChanged(EventArgs e) { base.OnLocationChanged(e); ScheduleSave(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); ScheduleSave(); }
        private void ScheduleSave() { if (ready) { saveTimer.Stop(); saveTimer.Start(); } }
        protected override void OnFormClosed(FormClosedEventArgs e) { Save(); saveTimer.Dispose(); hints.Dispose(); base.OnFormClosed(e); }
    }

    internal sealed class SettingsDialog : Form
    {
        private readonly TextBox keyText = new TextBox();
        private readonly NumericUpDown columns = new NumericUpDown { Minimum = 1, Maximum = 12 };
        internal string KeyText { get { return keyText.Text.Trim(); } }
        internal int Columns { get { return (int)columns.Value; } }
        internal SettingsDialog(string keys, int count)
        {
            Text = "自定义补键"; Font = new Font("Microsoft YaHei UI", 9);
            ClientSize = new Size(430, 266); FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            Controls.Add(new Label { Text = "要显示的按键（用空格分隔，可调整顺序）", AutoSize = true, Location = new Point(18, 18) });
            keyText.Text = keys; keyText.SetBounds(20, 46, 388, 28); Controls.Add(keyText);
            Controls.Add(new Label { Text = "支持 A–Z、0–9、F1–F12、标点，以及：\nSpace  Enter  Backspace  Tab  Esc  Delete\nLeft  Right  Up  Down  Home  End  PageUp  PageDown", AutoSize = true, Location = new Point(18, 86) });
            Controls.Add(new Label { Text = "每行按键数", AutoSize = true, Location = new Point(18, 166) });
            columns.Value = count; columns.SetBounds(112, 162, 70, 26); Controls.Add(columns);
            Controls.Add(new Label { Text = "1 = 竖排；4 = 四键横排", AutoSize = true, Location = new Point(194, 166) });
            var reset = new Button { Text = "恢复 3 E D C", Location = new Point(20, 218), Size = new Size(116, 30) };
            reset.Click += delegate { keyText.Text = "3 E D C"; columns.Value = 4; };
            var ok = new Button { Text = "保存", Location = new Point(244, 218), Size = new Size(78, 30) };
            ok.Click += delegate
            {
                try { KeyMap.Parse(KeyText); DialogResult = DialogResult.OK; }
                catch (ArgumentException error) { MessageBox.Show(this, error.Message, "检查按键", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(330, 218), Size = new Size(78, 30) };
            Controls.AddRange(new Control[] { reset, ok, cancel }); AcceptButton = ok; CancelButton = cancel;
        }
    }
}
