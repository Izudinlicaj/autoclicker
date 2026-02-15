/*
    AutoClicker Pro v2.0
    Ultimate portable autoclicker + key spammer for Windows
    
    Compile with icon:
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:icon.ico /out:"%USERPROFILE%\Desktop\AutoClickerPro.exe" "%USERPROFILE%\Desktop\AutoClickerPro.cs"
    
    Compile without icon:
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:"%USERPROFILE%\Desktop\AutoClickerPro.exe" "%USERPROFILE%\Desktop\AutoClickerPro.cs"
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

// ============================================================
// WINAPI
// ============================================================

public static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("ntdll.dll")]
    public static extern int NtSetTimerResolution(int DesiredResolution, bool SetResolution, out int CurrentResolution);

    [DllImport("winmm.dll")]
    public static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    public static extern uint timeEndPeriod(uint uPeriod);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    public const int INPUT_MOUSE = 0;
    public const int INPUT_KEYBOARD = 1;

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_XDOWN = 0x0080;
    public const uint MOUSEEVENTF_XUP = 0x0100;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    public const uint MOUSEEVENTF_MOVE = 0x0001;

    public const uint KEYEVENTF_KEYDOWN = 0x0000;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    public const uint MOD_NONE = 0x0000;
    public const int WM_HOTKEY = 0x0312;

    public const uint VK_F1 = 0x70;
    public const uint VK_F2 = 0x71;
    public const uint VK_F3 = 0x72;
    public const uint VK_F4 = 0x73;
    public const uint VK_F5 = 0x74;
    public const uint VK_F6 = 0x75;
    public const uint VK_F7 = 0x76;
    public const uint VK_F8 = 0x77;
    public const uint VK_F9 = 0x78;
    public const uint VK_F10 = 0x79;
    public const uint VK_F11 = 0x7A;
    public const uint VK_F12 = 0x7B;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public INPUTUNION union;
    }
}

// ============================================================
// INPUT ENGINE
// ============================================================

public static class InputEngine
{
    public enum MouseButton { Left, Right, Middle, X1, X2 }
    public enum ClickType { Single, Double, Triple }

    public static void SendClick(MouseButton button, ClickType clickType, bool useFixedPos, int fixedX, int fixedY)
    {
        uint downFlag, upFlag;
        uint mouseData = 0;

        switch (button)
        {
            case MouseButton.Right:
                downFlag = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_RIGHTUP;
                break;
            case MouseButton.Middle:
                downFlag = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                break;
            case MouseButton.X1:
                downFlag = NativeMethods.MOUSEEVENTF_XDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_XUP;
                mouseData = 0x0001;
                break;
            case MouseButton.X2:
                downFlag = NativeMethods.MOUSEEVENTF_XDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_XUP;
                mouseData = 0x0002;
                break;
            default:
                downFlag = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                upFlag = NativeMethods.MOUSEEVENTF_LEFTUP;
                break;
        }

        if (useFixedPos)
        {
            int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            int absX = (int)((fixedX * 65535.0) / (screenW - 1));
            int absY = (int)((fixedY * 65535.0) / (screenH - 1));

            NativeMethods.INPUT moveInput = new NativeMethods.INPUT();
            moveInput.type = NativeMethods.INPUT_MOUSE;
            moveInput.union.mi.dx = absX;
            moveInput.union.mi.dy = absY;
            moveInput.union.mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE;
            NativeMethods.SendInput(1, new NativeMethods.INPUT[] { moveInput }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
            Thread.SpinWait(500);
        }

        int count = 1;
        if (clickType == ClickType.Double) count = 2;
        if (clickType == ClickType.Triple) count = 3;

        for (int i = 0; i < count; i++)
        {
            NativeMethods.INPUT inputDown = new NativeMethods.INPUT();
            inputDown.type = NativeMethods.INPUT_MOUSE;
            inputDown.union.mi.dwFlags = downFlag;
            inputDown.union.mi.mouseData = mouseData;

            NativeMethods.INPUT inputUp = new NativeMethods.INPUT();
            inputUp.type = NativeMethods.INPUT_MOUSE;
            inputUp.union.mi.dwFlags = upFlag;
            inputUp.union.mi.mouseData = mouseData;

            NativeMethods.SendInput(2, new NativeMethods.INPUT[] { inputDown, inputUp }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
            if (i < count - 1) Thread.SpinWait(5000);
        }
    }

    public static void SendScrollWheel(int delta)
    {
        NativeMethods.INPUT input = new NativeMethods.INPUT();
        input.type = NativeMethods.INPUT_MOUSE;
        input.union.mi.dwFlags = NativeMethods.MOUSEEVENTF_WHEEL;
        input.union.mi.mouseData = (uint)delta;
        NativeMethods.SendInput(1, new NativeMethods.INPUT[] { input }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }

    public static void SendKeyPress(ushort vkCode)
    {
        NativeMethods.INPUT inputDown = new NativeMethods.INPUT();
        inputDown.type = NativeMethods.INPUT_KEYBOARD;
        inputDown.union.ki.wVk = vkCode;
        inputDown.union.ki.dwFlags = NativeMethods.KEYEVENTF_KEYDOWN;

        NativeMethods.INPUT inputUp = new NativeMethods.INPUT();
        inputUp.type = NativeMethods.INPUT_KEYBOARD;
        inputUp.union.ki.wVk = vkCode;
        inputUp.union.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

        NativeMethods.SendInput(2, new NativeMethods.INPUT[] { inputDown, inputUp }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }

    public static void SendKeyDown(ushort vkCode)
    {
        NativeMethods.INPUT inputDown = new NativeMethods.INPUT();
        inputDown.type = NativeMethods.INPUT_KEYBOARD;
        inputDown.union.ki.wVk = vkCode;
        inputDown.union.ki.dwFlags = NativeMethods.KEYEVENTF_KEYDOWN;
        NativeMethods.SendInput(1, new NativeMethods.INPUT[] { inputDown }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }

    public static void SendKeyUp(ushort vkCode)
    {
        NativeMethods.INPUT inputUp = new NativeMethods.INPUT();
        inputUp.type = NativeMethods.INPUT_KEYBOARD;
        inputUp.union.ki.wVk = vkCode;
        inputUp.union.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
        NativeMethods.SendInput(1, new NativeMethods.INPUT[] { inputUp }, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }
}

// ============================================================
// PROFILE
// ============================================================

public class Profile
{
    public string Name = "Default";
    public int Hours = 0, Minutes = 0, Seconds = 0, Milliseconds = 100;
    public int Microseconds = 0;
    public int MouseButtonIndex = 0;
    public int ClickTypeIndex = 0;
    public bool UseFixedPosition = false;
    public int FixedX = 0, FixedY = 0;
    public bool UseBurstMode = false;
    public int BurstCount = 10;
    public bool UseJitter = false;
    public int JitterMs = 20;
    public int HotkeyIndex = 5;
    public bool EnableKeySpam = false;
    public string KeySpamList = "";
    public int KeySpamIntervalMs = 50;
    public bool KeyHoldMode = false;
    public int SpeedPreset = 3; // custom
    public bool EnableMouseClick = true;
    public bool EnableScrollSpam = false;
    public int ScrollDelta = 120;
    public int ScrollIntervalMs = 100;

    public string Serialize()
    {
        return string.Join("|", new string[] {
            Name, Hours.ToString(), Minutes.ToString(), Seconds.ToString(),
            Milliseconds.ToString(), MouseButtonIndex.ToString(), ClickTypeIndex.ToString(),
            UseFixedPosition.ToString(), FixedX.ToString(), FixedY.ToString(),
            UseBurstMode.ToString(), BurstCount.ToString(),
            UseJitter.ToString(), JitterMs.ToString(), HotkeyIndex.ToString(),
            EnableKeySpam.ToString(), KeySpamList, KeySpamIntervalMs.ToString(),
            KeyHoldMode.ToString(), SpeedPreset.ToString(), Microseconds.ToString(),
            EnableMouseClick.ToString(), EnableScrollSpam.ToString(),
            ScrollDelta.ToString(), ScrollIntervalMs.ToString()
        });
    }

    public static Profile Deserialize(string data)
    {
        Profile p = new Profile();
        string[] parts = data.Split('|');
        if (parts.Length >= 15)
        {
            p.Name = parts[0];
            int.TryParse(parts[1], out p.Hours);
            int.TryParse(parts[2], out p.Minutes);
            int.TryParse(parts[3], out p.Seconds);
            int.TryParse(parts[4], out p.Milliseconds);
            int.TryParse(parts[5], out p.MouseButtonIndex);
            int.TryParse(parts[6], out p.ClickTypeIndex);
            bool.TryParse(parts[7], out p.UseFixedPosition);
            int.TryParse(parts[8], out p.FixedX);
            int.TryParse(parts[9], out p.FixedY);
            bool.TryParse(parts[10], out p.UseBurstMode);
            int.TryParse(parts[11], out p.BurstCount);
            bool.TryParse(parts[12], out p.UseJitter);
            int.TryParse(parts[13], out p.JitterMs);
            int.TryParse(parts[14], out p.HotkeyIndex);
        }
        if (parts.Length >= 21)
        {
            bool.TryParse(parts[15], out p.EnableKeySpam);
            p.KeySpamList = parts[16];
            int.TryParse(parts[17], out p.KeySpamIntervalMs);
            bool.TryParse(parts[18], out p.KeyHoldMode);
            int.TryParse(parts[19], out p.SpeedPreset);
            int.TryParse(parts[20], out p.Microseconds);
        }
        if (parts.Length >= 25)
        {
            bool.TryParse(parts[21], out p.EnableMouseClick);
            bool.TryParse(parts[22], out p.EnableScrollSpam);
            int.TryParse(parts[23], out p.ScrollDelta);
            int.TryParse(parts[24], out p.ScrollIntervalMs);
        }
        return p;
    }
}

public static class ProfileManager
{
    private static string GetProfileDir()
    {
        string dir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "profiles");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    public static void Save(Profile p)
    {
        File.WriteAllText(Path.Combine(GetProfileDir(), p.Name + ".acp"), p.Serialize());
    }

    public static Profile Load(string name)
    {
        string path = Path.Combine(GetProfileDir(), name + ".acp");
        if (File.Exists(path)) return Profile.Deserialize(File.ReadAllText(path));
        return new Profile();
    }

    public static string[] GetProfileNames()
    {
        List<string> names = new List<string>();
        foreach (string f in Directory.GetFiles(GetProfileDir(), "*.acp"))
            names.Add(Path.GetFileNameWithoutExtension(f));
        return names.ToArray();
    }

    public static void Delete(string name)
    {
        string path = Path.Combine(GetProfileDir(), name + ".acp");
        if (File.Exists(path)) File.Delete(path);
    }
}

// ============================================================
// COORDINATE PICKER
// ============================================================

public class CoordinatePickerForm : Form
{
    public int PickedX = 0, PickedY = 0;
    public bool WasPicked = false;
    private Label coordLabel;

    public CoordinatePickerForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.Opacity = 0.25;
        this.BackColor = Color.Black;
        this.Cursor = Cursors.Cross;

        coordLabel = new Label();
        coordLabel.AutoSize = true;
        coordLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        coordLabel.ForeColor = Color.Lime;
        coordLabel.BackColor = Color.FromArgb(200, 0, 0, 0);
        coordLabel.Padding = new Padding(12, 8, 12, 8);
        coordLabel.Text = "Click to pick coordinates | ESC to cancel";
        coordLabel.Location = new Point(20, 20);
        this.Controls.Add(coordLabel);

        this.MouseMove += (s, e) => { coordLabel.Text = "X: " + e.X + "  Y: " + e.Y + "  | Click to select | ESC cancel"; };
        this.MouseClick += (s, e) => { PickedX = e.X; PickedY = e.Y; WasPicked = true; this.Close(); };
        this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { WasPicked = false; this.Close(); } };
    }
}

// ============================================================
// DARK MODE COLORS
// ============================================================

public static class DarkTheme
{
    public static readonly Color BG = Color.FromArgb(30, 30, 30);
    public static readonly Color BG2 = Color.FromArgb(40, 40, 40);
    public static readonly Color BG3 = Color.FromArgb(50, 50, 50);
    public static readonly Color FG = Color.FromArgb(220, 220, 220);
    public static readonly Color FG2 = Color.FromArgb(160, 160, 160);
    public static readonly Color Accent = Color.FromArgb(0, 150, 136);
    public static readonly Color AccentHover = Color.FromArgb(0, 180, 160);
    public static readonly Color Red = Color.FromArgb(220, 60, 60);
    public static readonly Color Green = Color.FromArgb(46, 180, 100);
    public static readonly Color Blue = Color.FromArgb(60, 140, 200);
    public static readonly Color Orange = Color.FromArgb(230, 150, 30);
    public static readonly Color GroupBorder = Color.FromArgb(70, 70, 70);
    public static readonly Color InputBG = Color.FromArgb(55, 55, 55);
    public static readonly Color InputBorder = Color.FromArgb(80, 80, 80);

    public static void Apply(Control control)
    {
        control.BackColor = BG;
        control.ForeColor = FG;
        ApplyRecursive(control);
    }

    private static void ApplyRecursive(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is GroupBox)
            {
                c.ForeColor = Accent;
                c.BackColor = BG2;
            }
            else if (c is Button)
            {
                // Keep custom button colors
            }
            else if (c is TextBox || c is NumericUpDown || c is ComboBox)
            {
                c.BackColor = InputBG;
                c.ForeColor = FG;
            }
            else if (c is CheckBox || c is RadioButton)
            {
                c.ForeColor = FG;
                c.BackColor = Color.Transparent;
            }
            else if (c is Label)
            {
                if (c.BackColor != Color.Transparent && c.BackColor != BG && c.BackColor != BG2 && c.BackColor != BG3)
                {
                    // keep special label colors
                }
                else
                {
                    c.ForeColor = FG;
                }
            }
            else if (c is Panel)
            {
                c.BackColor = BG3;
            }
            else if (c is TabControl || c is TabPage)
            {
                c.BackColor = BG;
                c.ForeColor = FG;
            }

            if (c.Controls.Count > 0) ApplyRecursive(c);
        }
    }
}

// ============================================================
// KEY NAME HELPER
// ============================================================

public static class KeyHelper
{
    public static readonly Dictionary<string, ushort> NameToVK = new Dictionary<string, ushort>();
    public static readonly Dictionary<ushort, string> VKToName = new Dictionary<ushort, string>();

    static KeyHelper()
    {
        // Letters
        for (int i = 0; i < 26; i++)
        {
            string name = ((char)('A' + i)).ToString();
            ushort vk = (ushort)(0x41 + i);
            NameToVK[name] = vk;
            VKToName[vk] = name;
        }
        // Numbers
        for (int i = 0; i <= 9; i++)
        {
            string name = i.ToString();
            ushort vk = (ushort)(0x30 + i);
            NameToVK[name] = vk;
            VKToName[vk] = name;
        }
        // F keys
        for (int i = 1; i <= 12; i++)
        {
            string name = "F" + i;
            ushort vk = (ushort)(0x6F + i);
            NameToVK[name] = vk;
            VKToName[vk] = name;
        }
        // Special keys
        AddKey("SPACE", 0x20); AddKey("ENTER", 0x0D); AddKey("TAB", 0x09);
        AddKey("ESC", 0x1B); AddKey("BACKSPACE", 0x08); AddKey("DELETE", 0x2E);
        AddKey("INSERT", 0x2D); AddKey("HOME", 0x24); AddKey("END", 0x23);
        AddKey("PAGEUP", 0x21); AddKey("PAGEDOWN", 0x22);
        AddKey("UP", 0x26); AddKey("DOWN", 0x28); AddKey("LEFT", 0x25); AddKey("RIGHT", 0x27);
        AddKey("SHIFT", 0x10); AddKey("CTRL", 0x11); AddKey("ALT", 0x12);
        AddKey("CAPSLOCK", 0x14); AddKey("NUMLOCK", 0x90);
        AddKey("NUM0", 0x60); AddKey("NUM1", 0x61); AddKey("NUM2", 0x62);
        AddKey("NUM3", 0x63); AddKey("NUM4", 0x64); AddKey("NUM5", 0x65);
        AddKey("NUM6", 0x66); AddKey("NUM7", 0x67); AddKey("NUM8", 0x68);
        AddKey("NUM9", 0x69);
        AddKey("MULTIPLY", 0x6A); AddKey("ADD", 0x6B); AddKey("SUBTRACT", 0x6D);
        AddKey("DECIMAL", 0x6E); AddKey("DIVIDE", 0x6F);
        AddKey("SEMICOLON", 0xBA); AddKey("EQUALS", 0xBB); AddKey("COMMA", 0xBC);
        AddKey("MINUS", 0xBD); AddKey("PERIOD", 0xBE); AddKey("SLASH", 0xBF);
        AddKey("BACKSLASH", 0xDC); AddKey("LBRACKET", 0xDB); AddKey("RBRACKET", 0xDD);
        AddKey("QUOTE", 0xDE); AddKey("TILDE", 0xC0);
    }

    private static void AddKey(string name, ushort vk)
    {
        NameToVK[name] = vk;
        VKToName[vk] = name;
    }

    public static ushort Parse(string name)
    {
        name = name.Trim().ToUpper();
        if (NameToVK.ContainsKey(name)) return NameToVK[name];
        if (name.Length == 1) return (ushort)name[0];
        return 0;
    }

    public static string GetAllKeysHelp()
    {
        return "A-Z, 0-9, F1-F12, SPACE, ENTER, TAB, ESC, SHIFT, CTRL, ALT,\n" +
               "UP, DOWN, LEFT, RIGHT, BACKSPACE, DELETE, INSERT, HOME, END,\n" +
               "PAGEUP, PAGEDOWN, NUM0-NUM9, CAPSLOCK, NUMLOCK,\n" +
               "SEMICOLON, COMMA, PERIOD, SLASH, BACKSLASH, MINUS, EQUALS,\n" +
               "LBRACKET, RBRACKET, QUOTE, TILDE\n\n" +
               "Separate multiple keys with commas: A, SPACE, ENTER";
    }
}

// ============================================================
// MAIN FORM
// ============================================================

public class AutoClickerForm : Form
{
    // Tabs
    private TabControl tabControl;
    private TabPage tabMouse, tabKeyboard, tabAdvanced, tabProfiles, tabAbout;

    // Mouse tab
    private NumericUpDown nudHours, nudMinutes, nudSeconds, nudMilliseconds, nudMicroseconds;
    private ComboBox cmbMouseButton, cmbClickType, cmbSpeedPreset;
    private RadioButton rbCursorPos, rbFixedPos;
    private NumericUpDown nudFixedX, nudFixedY;
    private Button btnPickCoords;
    private CheckBox chkEnableMouseClick;
    private CheckBox chkEnableScroll;
    private NumericUpDown nudScrollDelta, nudScrollIntervalMs;

    // Keyboard tab
    private CheckBox chkEnableKeySpam;
    private TextBox txtKeyList;
    private NumericUpDown nudKeyIntervalMs;
    private CheckBox chkKeyHoldMode;
    private Label lblKeyHelp;

    // Advanced tab
    private CheckBox chkBurstMode;
    private NumericUpDown nudBurstCount;
    private CheckBox chkJitter;
    private NumericUpDown nudJitterMs;
    private ComboBox cmbHotkey;
    private CheckBox chkAlwaysOnTop;

    // Profiles tab
    private ComboBox cmbProfiles;
    private Button btnSaveProfile, btnLoadProfile, btnDeleteProfile;
    private TextBox txtProfileName;

    // Bottom bar
    private Button btnStart, btnStop;
    private Label lblStatus, lblClickCount, lblActionsPerSec;
    private Panel statusPanel;

    // Tray
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    // State
    private Thread clickThread, keyThread, scrollThread;
    private volatile bool isRunning = false;
    private long totalActions = 0;
    private System.Windows.Forms.Timer uiTimer;
    private Random rng = new Random();
    private Stopwatch apsStopwatch = new Stopwatch();
    private long lastActionCount = 0;
    private double currentAPS = 0;

    // Hotkey
    private const int HOTKEY_TOGGLE = 1;
    private uint currentHotkeyVK = NativeMethods.VK_F6;

    private static readonly string[] HotkeyNames = {
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };
    private static readonly uint[] HotkeyVKs = {
        NativeMethods.VK_F1, NativeMethods.VK_F2, NativeMethods.VK_F3, NativeMethods.VK_F4,
        NativeMethods.VK_F5, NativeMethods.VK_F6, NativeMethods.VK_F7, NativeMethods.VK_F8,
        NativeMethods.VK_F9, NativeMethods.VK_F10, NativeMethods.VK_F11, NativeMethods.VK_F12
    };

    // Speed presets (in microseconds)
    private static readonly string[] SpeedPresetNames = {
        "Slow (500ms)", "Normal (100ms)", "Fast (10ms)", "Ultra (1ms)", "MAXIMUM (sub-ms)", "Custom"
    };
    private static readonly long[] SpeedPresetUs = {
        500000, 100000, 10000, 1000, 100, -1
    };

    public AutoClickerForm()
    {
        BuildUI();
        DarkTheme.Apply(this);
        RegisterGlobalHotkey();
        SetupTray();
        SetupUITimer();
        EnableHighResTimer();
    }

    private void EnableHighResTimer()
    {
        NativeMethods.timeBeginPeriod(1);
        int cur;
        NativeMethods.NtSetTimerResolution(5000, true, out cur); // 0.5ms resolution
    }

    // ---- UI HELPERS ----
    private Label MakeLabel(string text, int x, int y, float size = 9f, FontStyle style = FontStyle.Regular)
    {
        Label l = new Label();
        l.Text = text;
        l.AutoSize = true;
        l.Location = new Point(x, y);
        l.Font = new Font("Segoe UI", size, style);
        l.ForeColor = DarkTheme.FG;
        l.BackColor = Color.Transparent;
        return l;
    }

    private GroupBox MakeGroup(string title, int x, int y, int w, int h)
    {
        GroupBox gb = new GroupBox();
        gb.Text = title;
        gb.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        gb.Location = new Point(x, y);
        gb.Size = new Size(w, h);
        gb.ForeColor = DarkTheme.Accent;
        gb.BackColor = DarkTheme.BG2;
        return gb;
    }

    private Button MakeButton(string text, int x, int y, int w, int h, Color bg, EventHandler click)
    {
        Button b = new Button();
        b.Text = text;
        b.Location = new Point(x, y);
        b.Size = new Size(w, h);
        b.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = bg;
        b.ForeColor = Color.White;
        b.Cursor = Cursors.Hand;
        b.Click += click;
        return b;
    }

    // ============================================================
    // BUILD UI
    // ============================================================

    private void BuildUI()
    {
        this.Text = "AutoClicker Pro v2.0";
        this.Size = new Size(550, 620);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.TopMost = true;
        this.BackColor = DarkTheme.BG;
        this.ForeColor = DarkTheme.FG;
        this.Font = new Font("Segoe UI", 9);

        // Tab control
        tabControl = new TabControl();
        tabControl.Location = new Point(10, 10);
        tabControl.Size = new Size(520, 440);
        tabControl.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        // ---- MOUSE TAB ----
        tabMouse = new TabPage("  Mouse  ");
        tabMouse.BackColor = DarkTheme.BG;
        BuildMouseTab();
        tabControl.TabPages.Add(tabMouse);

        // ---- KEYBOARD TAB ----
        tabKeyboard = new TabPage("  Keyboard  ");
        tabKeyboard.BackColor = DarkTheme.BG;
        BuildKeyboardTab();
        tabControl.TabPages.Add(tabKeyboard);

        // ---- ADVANCED TAB ----
        tabAdvanced = new TabPage("  Advanced  ");
        tabAdvanced.BackColor = DarkTheme.BG;
        BuildAdvancedTab();
        tabControl.TabPages.Add(tabAdvanced);

        // ---- PROFILES TAB ----
        tabProfiles = new TabPage("  Profiles  ");
        tabProfiles.BackColor = DarkTheme.BG;
        BuildProfilesTab();
        tabControl.TabPages.Add(tabProfiles);

        // ---- ABOUT TAB ----
        tabAbout = new TabPage("  About  ");
        tabAbout.BackColor = DarkTheme.BG;
        BuildAboutTab();
        tabControl.TabPages.Add(tabAbout);

        this.Controls.Add(tabControl);

        // ---- STATUS PANEL ----
        statusPanel = new Panel();
        statusPanel.Location = new Point(10, 455);
        statusPanel.Size = new Size(520, 40);
        statusPanel.BackColor = DarkTheme.BG3;

        lblStatus = new Label() { Text = "⏹  STOPPED", AutoSize = true, Location = new Point(10, 10) };
        lblStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        lblStatus.ForeColor = DarkTheme.Red;
        lblStatus.BackColor = Color.Transparent;
        statusPanel.Controls.Add(lblStatus);

        lblClickCount = new Label() { Text = "Actions: 0", AutoSize = true, Location = new Point(230, 12) };
        lblClickCount.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        lblClickCount.ForeColor = DarkTheme.FG;
        lblClickCount.BackColor = Color.Transparent;
        statusPanel.Controls.Add(lblClickCount);

        lblActionsPerSec = new Label() { Text = "0 /sec", AutoSize = true, Location = new Point(420, 12) };
        lblActionsPerSec.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        lblActionsPerSec.ForeColor = DarkTheme.Orange;
        lblActionsPerSec.BackColor = Color.Transparent;
        statusPanel.Controls.Add(lblActionsPerSec);

        this.Controls.Add(statusPanel);

        // ---- START/STOP ----
        btnStart = new Button();
        btnStart.Text = "▶  START";
        btnStart.Location = new Point(10, 500);
        btnStart.Size = new Size(255, 50);
        btnStart.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        btnStart.FlatStyle = FlatStyle.Flat;
        btnStart.FlatAppearance.BorderSize = 0;
        btnStart.BackColor = DarkTheme.Green;
        btnStart.ForeColor = Color.White;
        btnStart.Cursor = Cursors.Hand;
        btnStart.Click += (s, e) => StartAll();
        this.Controls.Add(btnStart);

        btnStop = new Button();
        btnStop.Text = "⏹  STOP";
        btnStop.Location = new Point(275, 500);
        btnStop.Size = new Size(255, 50);
        btnStop.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        btnStop.FlatStyle = FlatStyle.Flat;
        btnStop.FlatAppearance.BorderSize = 0;
        btnStop.BackColor = DarkTheme.Red;
        btnStop.ForeColor = Color.White;
        btnStop.Cursor = Cursors.Hand;
        btnStop.Enabled = false;
        btnStop.Click += (s, e) => StopAll();
        this.Controls.Add(btnStop);

        // Footer
        Label lblFooter = MakeLabel("Hotkey: F6 (change in Advanced tab)  |  Minimize to tray", 10, 558, 7.5f, FontStyle.Italic);
        lblFooter.ForeColor = DarkTheme.FG2;
        this.Controls.Add(lblFooter);
    }

    private void BuildMouseTab()
    {
        // Enable mouse clicking
        chkEnableMouseClick = new CheckBox() { Text = "Enable Mouse Clicking", Location = new Point(15, 12), AutoSize = true, Checked = true };
        chkEnableMouseClick.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        chkEnableMouseClick.ForeColor = DarkTheme.Accent;
        tabMouse.Controls.Add(chkEnableMouseClick);

        // Speed preset
        GroupBox gbSpeed = MakeGroup("Speed", 10, 40, 495, 65);

        gbSpeed.Controls.Add(MakeLabel("Preset:", 15, 25));
        cmbSpeedPreset = new ComboBox() { Location = new Point(70, 22), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSpeedPreset.Font = new Font("Segoe UI", 9);
        cmbSpeedPreset.Items.AddRange(SpeedPresetNames);
        cmbSpeedPreset.SelectedIndex = 5; // Custom
        cmbSpeedPreset.SelectedIndexChanged += CmbSpeedPreset_Changed;
        cmbSpeedPreset.BackColor = DarkTheme.InputBG;
        cmbSpeedPreset.ForeColor = DarkTheme.FG;
        gbSpeed.Controls.Add(cmbSpeedPreset);

        Label lblSpeedInfo = MakeLabel("Sub-ms uses busy-wait CPU spinning for maximum speed", 240, 25, 7.5f, FontStyle.Italic);
        lblSpeedInfo.ForeColor = DarkTheme.FG2;
        gbSpeed.Controls.Add(lblSpeedInfo);

        tabMouse.Controls.Add(gbSpeed);

        // Custom interval
        GroupBox gbInterval = MakeGroup("Custom Interval", 10, 110, 495, 70);

        gbInterval.Controls.Add(MakeLabel("H:", 15, 28));
        nudHours = new NumericUpDown() { Location = new Point(32, 25), Width = 50, Maximum = 99, Value = 0 };
        gbInterval.Controls.Add(nudHours);

        gbInterval.Controls.Add(MakeLabel("M:", 90, 28));
        nudMinutes = new NumericUpDown() { Location = new Point(110, 25), Width = 50, Maximum = 59, Value = 0 };
        gbInterval.Controls.Add(nudMinutes);

        gbInterval.Controls.Add(MakeLabel("S:", 168, 28));
        nudSeconds = new NumericUpDown() { Location = new Point(185, 25), Width = 50, Maximum = 59, Value = 0 };
        gbInterval.Controls.Add(nudSeconds);

        gbInterval.Controls.Add(MakeLabel("Ms:", 243, 28));
        nudMilliseconds = new NumericUpDown() { Location = new Point(268, 25), Width = 60, Maximum = 999, Minimum = 0, Value = 100 };
        gbInterval.Controls.Add(nudMilliseconds);

        gbInterval.Controls.Add(MakeLabel("\u00B5s:", 338, 28));
        nudMicroseconds = new NumericUpDown() { Location = new Point(365, 25), Width = 70, Maximum = 999, Minimum = 0, Value = 0 };
        gbInterval.Controls.Add(nudMicroseconds);

        Label lblUsHelp = MakeLabel("(\u00B5s = microseconds, 1000\u00B5s = 1ms)", 15, 48, 7.5f, FontStyle.Italic);
        lblUsHelp.ForeColor = DarkTheme.FG2;
        gbInterval.Controls.Add(lblUsHelp);

        tabMouse.Controls.Add(gbInterval);

        // Click options
        GroupBox gbClick = MakeGroup("Click Options", 10, 185, 495, 60);

        gbClick.Controls.Add(MakeLabel("Button:", 15, 25));
        cmbMouseButton = new ComboBox() { Location = new Point(70, 22), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbMouseButton.Items.AddRange(new string[] { "Left", "Right", "Middle", "X1 (Back)", "X2 (Forward)" });
        cmbMouseButton.SelectedIndex = 0;
        cmbMouseButton.BackColor = DarkTheme.InputBG;
        cmbMouseButton.ForeColor = DarkTheme.FG;
        gbClick.Controls.Add(cmbMouseButton);

        gbClick.Controls.Add(MakeLabel("Type:", 200, 25));
        cmbClickType = new ComboBox() { Location = new Point(245, 22), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbClickType.Items.AddRange(new string[] { "Single", "Double", "Triple" });
        cmbClickType.SelectedIndex = 0;
        cmbClickType.BackColor = DarkTheme.InputBG;
        cmbClickType.ForeColor = DarkTheme.FG;
        gbClick.Controls.Add(cmbClickType);

        tabMouse.Controls.Add(gbClick);

        // Position
        GroupBox gbPos = MakeGroup("Click Position", 10, 250, 495, 75);

        rbCursorPos = new RadioButton() { Text = "Current cursor position", Location = new Point(15, 22), AutoSize = true, Checked = true };
        rbCursorPos.ForeColor = DarkTheme.FG;
        gbPos.Controls.Add(rbCursorPos);

        rbFixedPos = new RadioButton() { Text = "Fixed:", Location = new Point(15, 46), AutoSize = true };
        rbFixedPos.ForeColor = DarkTheme.FG;
        gbPos.Controls.Add(rbFixedPos);

        gbPos.Controls.Add(MakeLabel("X:", 80, 48));
        nudFixedX = new NumericUpDown() { Location = new Point(100, 45), Width = 70, Maximum = 9999 };
        gbPos.Controls.Add(nudFixedX);

        gbPos.Controls.Add(MakeLabel("Y:", 180, 48));
        nudFixedY = new NumericUpDown() { Location = new Point(200, 45), Width = 70, Maximum = 9999 };
        gbPos.Controls.Add(nudFixedY);

        btnPickCoords = MakeButton("Pick Location", 290, 43, 110, 28, DarkTheme.Blue, BtnPickCoords_Click);
        gbPos.Controls.Add(btnPickCoords);

        tabMouse.Controls.Add(gbPos);

        // Scroll spam
        GroupBox gbScroll = MakeGroup("Scroll Wheel Spam", 10, 330, 495, 65);

        chkEnableScroll = new CheckBox() { Text = "Enable scroll spamming", Location = new Point(15, 22), AutoSize = true };
        chkEnableScroll.ForeColor = DarkTheme.FG;
        gbScroll.Controls.Add(chkEnableScroll);

        gbScroll.Controls.Add(MakeLabel("Delta:", 230, 25));
        nudScrollDelta = new NumericUpDown() { Location = new Point(275, 22), Width = 70, Maximum = 9999, Minimum = -9999, Value = 120 };
        gbScroll.Controls.Add(nudScrollDelta);

        gbScroll.Controls.Add(MakeLabel("Ms:", 355, 25));
        nudScrollIntervalMs = new NumericUpDown() { Location = new Point(385, 22), Width = 70, Maximum = 99999, Minimum = 1, Value = 100 };
        gbScroll.Controls.Add(nudScrollIntervalMs);

        Label lblScrollHelp = MakeLabel("Delta: 120=scroll up, -120=scroll down. Larger = faster scroll.", 15, 45, 7.5f, FontStyle.Italic);
        lblScrollHelp.ForeColor = DarkTheme.FG2;
        gbScroll.Controls.Add(lblScrollHelp);

        tabMouse.Controls.Add(gbScroll);
    }

    private void BuildKeyboardTab()
    {
        chkEnableKeySpam = new CheckBox() { Text = "Enable Keyboard Spamming", Location = new Point(15, 15), AutoSize = true };
        chkEnableKeySpam.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        chkEnableKeySpam.ForeColor = DarkTheme.Accent;
        tabKeyboard.Controls.Add(chkEnableKeySpam);

        // Key list
        GroupBox gbKeys = MakeGroup("Keys to Spam", 10, 45, 495, 110);

        gbKeys.Controls.Add(MakeLabel("Keys (comma separated):", 15, 22));
        txtKeyList = new TextBox() { Location = new Point(15, 42), Width = 460, Text = "SPACE" };
        txtKeyList.Font = new Font("Consolas", 11);
        txtKeyList.BackColor = DarkTheme.InputBG;
        txtKeyList.ForeColor = Color.Lime;
        gbKeys.Controls.Add(txtKeyList);

        Label lblExample = MakeLabel("Examples: A | SPACE | A, W, S, D | ENTER, TAB | F, SPACE, SHIFT", 15, 68, 8f, FontStyle.Italic);
        lblExample.ForeColor = DarkTheme.Orange;
        gbKeys.Controls.Add(lblExample);

        Label lblSeq = MakeLabel("Multiple keys = pressed in sequence, one after another, repeating", 15, 85, 7.5f, FontStyle.Italic);
        lblSeq.ForeColor = DarkTheme.FG2;
        gbKeys.Controls.Add(lblSeq);

        tabKeyboard.Controls.Add(gbKeys);

        // Key options
        GroupBox gbKeyOpt = MakeGroup("Key Options", 10, 160, 495, 80);

        gbKeyOpt.Controls.Add(MakeLabel("Interval between keys:", 15, 25));
        nudKeyIntervalMs = new NumericUpDown() { Location = new Point(175, 22), Width = 80, Maximum = 99999, Minimum = 1, Value = 50 };
        gbKeyOpt.Controls.Add(nudKeyIntervalMs);
        gbKeyOpt.Controls.Add(MakeLabel("ms", 260, 25));

        chkKeyHoldMode = new CheckBox() { Text = "Hold mode (key stays down, releases on stop)", Location = new Point(15, 50), AutoSize = true };
        chkKeyHoldMode.ForeColor = DarkTheme.FG;
        gbKeyOpt.Controls.Add(chkKeyHoldMode);

        tabKeyboard.Controls.Add(gbKeyOpt);

        // Supported keys help
        GroupBox gbHelp = MakeGroup("Supported Keys", 10, 245, 495, 150);

        lblKeyHelp = new Label();
        lblKeyHelp.Text = KeyHelper.GetAllKeysHelp();
        lblKeyHelp.Location = new Point(15, 20);
        lblKeyHelp.Size = new Size(465, 120);
        lblKeyHelp.Font = new Font("Consolas", 8);
        lblKeyHelp.ForeColor = DarkTheme.FG2;
        lblKeyHelp.BackColor = Color.Transparent;
        gbHelp.Controls.Add(lblKeyHelp);

        tabKeyboard.Controls.Add(gbHelp);
    }

    private void BuildAdvancedTab()
    {
        // Burst mode
        GroupBox gbBurst = MakeGroup("Burst Mode", 10, 10, 495, 65);

        chkBurstMode = new CheckBox() { Text = "Stop after", Location = new Point(15, 25), AutoSize = true };
        chkBurstMode.ForeColor = DarkTheme.FG;
        gbBurst.Controls.Add(chkBurstMode);

        nudBurstCount = new NumericUpDown() { Location = new Point(110, 23), Width = 100, Maximum = 9999999, Minimum = 1, Value = 10 };
        gbBurst.Controls.Add(nudBurstCount);

        gbBurst.Controls.Add(MakeLabel("total actions (clicks + keypresses combined)", 218, 25));

        tabAdvanced.Controls.Add(gbBurst);

        // Jitter
        GroupBox gbJitter = MakeGroup("Anti-Detection Jitter", 10, 80, 495, 65);

        chkJitter = new CheckBox() { Text = "Add random jitter +/-", Location = new Point(15, 25), AutoSize = true };
        chkJitter.ForeColor = DarkTheme.FG;
        gbJitter.Controls.Add(chkJitter);

        nudJitterMs = new NumericUpDown() { Location = new Point(185, 23), Width = 70, Maximum = 10000, Minimum = 1, Value = 20 };
        gbJitter.Controls.Add(nudJitterMs);

        gbJitter.Controls.Add(MakeLabel("ms  (randomizes interval to look human)", 260, 25));

        tabAdvanced.Controls.Add(gbJitter);

        // Hotkey
        GroupBox gbHotkey = MakeGroup("Global Hotkey", 10, 150, 495, 65);

        gbHotkey.Controls.Add(MakeLabel("Toggle Start/Stop:", 15, 28));
        cmbHotkey = new ComboBox() { Location = new Point(145, 25), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbHotkey.Items.AddRange(HotkeyNames);
        cmbHotkey.SelectedIndex = 5;
        cmbHotkey.BackColor = DarkTheme.InputBG;
        cmbHotkey.ForeColor = DarkTheme.FG;
        cmbHotkey.SelectedIndexChanged += CmbHotkey_Changed;
        gbHotkey.Controls.Add(cmbHotkey);

        Label lblHkInfo = MakeLabel("Works globally — even in fullscreen games and when minimized to tray", 240, 28, 7.5f, FontStyle.Italic);
        lblHkInfo.ForeColor = DarkTheme.FG2;
        gbHotkey.Controls.Add(lblHkInfo);

        tabAdvanced.Controls.Add(gbHotkey);

        // Always on top
        GroupBox gbMisc = MakeGroup("Miscellaneous", 10, 220, 495, 60);

        chkAlwaysOnTop = new CheckBox() { Text = "Always on top", Location = new Point(15, 25), AutoSize = true, Checked = true };
        chkAlwaysOnTop.ForeColor = DarkTheme.FG;
        chkAlwaysOnTop.CheckedChanged += (s, e) => { this.TopMost = chkAlwaysOnTop.Checked; };
        gbMisc.Controls.Add(chkAlwaysOnTop);

        tabAdvanced.Controls.Add(gbMisc);

        // Performance info
        GroupBox gbPerf = MakeGroup("Performance Notes", 10, 285, 495, 110);

        Label lblPerf = new Label();
        lblPerf.Text =
            "• SendInput (hardware-level) — games and browsers see real input\n" +
            "• Sub-millisecond timing uses CPU busy-wait for maximum precision\n" +
            "• Thread priority set to Highest for consistent timing\n" +
            "• Timer resolution set to 0.5ms via NtSetTimerResolution\n" +
            "• WARNING: Sub-ms speeds will use significant CPU\n" +
            "• Anti-cheat software may flag any autoclicker regardless of method";
        lblPerf.Location = new Point(15, 20);
        lblPerf.Size = new Size(465, 85);
        lblPerf.Font = new Font("Segoe UI", 8);
        lblPerf.ForeColor = DarkTheme.FG2;
        lblPerf.BackColor = Color.Transparent;
        gbPerf.Controls.Add(lblPerf);

        tabAdvanced.Controls.Add(gbPerf);
    }

    private void BuildProfilesTab()
    {
        GroupBox gbSave = MakeGroup("Save Profile", 10, 10, 495, 65);

        gbSave.Controls.Add(MakeLabel("Profile Name:", 15, 28));
        txtProfileName = new TextBox() { Location = new Point(115, 25), Width = 200, Text = "Default" };
        txtProfileName.BackColor = DarkTheme.InputBG;
        txtProfileName.ForeColor = DarkTheme.FG;
        gbSave.Controls.Add(txtProfileName);

        btnSaveProfile = MakeButton("Save Profile", 330, 23, 140, 28, DarkTheme.Green, BtnSaveProfile_Click);
        gbSave.Controls.Add(btnSaveProfile);

        tabProfiles.Controls.Add(gbSave);

        GroupBox gbLoad = MakeGroup("Load / Delete Profile", 10, 80, 495, 65);

        gbLoad.Controls.Add(MakeLabel("Select:", 15, 28));
        cmbProfiles = new ComboBox() { Location = new Point(65, 25), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbProfiles.BackColor = DarkTheme.InputBG;
        cmbProfiles.ForeColor = DarkTheme.FG;
        gbLoad.Controls.Add(cmbProfiles);

        btnLoadProfile = MakeButton("Load", 280, 23, 80, 28, DarkTheme.Blue, BtnLoadProfile_Click);
        gbLoad.Controls.Add(btnLoadProfile);

        btnDeleteProfile = MakeButton("Delete", 370, 23, 80, 28, DarkTheme.Red, BtnDeleteProfile_Click);
        gbLoad.Controls.Add(btnDeleteProfile);

        tabProfiles.Controls.Add(gbLoad);

        Label lblProfInfo = MakeLabel("Profiles are saved as .acp files next to the .exe (portable)", 15, 155, 8f, FontStyle.Italic);
        lblProfInfo.ForeColor = DarkTheme.FG2;
        tabProfiles.Controls.Add(lblProfInfo);

        RefreshProfileList();
    }

    private void BuildAboutTab()
    {
        Label lblTitle = MakeLabel("AutoClicker Pro", 15, 20, 18, FontStyle.Bold);
        lblTitle.ForeColor = DarkTheme.Accent;
        tabAbout.Controls.Add(lblTitle);

        Label lblVer = MakeLabel("Version 2.0", 15, 55, 12, FontStyle.Regular);
        tabAbout.Controls.Add(lblVer);

        string aboutText =
            "Ultimate portable autoclicker and key spammer for Windows.\n\n" +
            "Features:\n" +
            "  • Hardware-level input via SendInput (works on games, browsers, Flash, everything)\n" +
            "  • Mouse clicking: Left, Right, Middle, X1, X2 buttons\n" +
            "  • Single, Double, Triple click modes\n" +
            "  • Click at cursor position or fixed coordinates\n" +
            "  • Coordinate picker (fullscreen crosshair overlay)\n" +
            "  • Keyboard spamming: any key or combination of keys\n" +
            "  • Key hold mode (hold keys down continuously)\n" +
            "  • Scroll wheel spamming (up/down, custom speed)\n" +
            "  • Speed from hours down to sub-millisecond (microseconds)\n" +
            "  • Speed presets: Slow, Normal, Fast, Ultra, MAXIMUM\n" +
            "  • Burst mode (stop after N actions)\n" +
            "  • Anti-detection random jitter\n" +
            "  • Global hotkey (F1-F12, works in fullscreen)\n" +
            "  • Profile save/load/delete (portable .acp files)\n" +
            "  • System tray with minimize\n" +
            "  • Dark mode UI\n" +
            "  • Live actions-per-second counter\n" +
            "  • High-resolution timer (0.5ms via NtSetTimerResolution)\n" +
            "  • 100% portable single .exe (~50KB)\n" +
            "  • Zero dependencies, zero installation\n\n" +
            "Built with C# and WinForms. Compiled with .NET Framework 4.x (built into Windows).\n\n" +
            "Open source — MIT License";

        Label lblAbout = new Label();
        lblAbout.Text = aboutText;
        lblAbout.Location = new Point(15, 85);
        lblAbout.Size = new Size(490, 310);
        lblAbout.Font = new Font("Segoe UI", 8.5f);
        lblAbout.ForeColor = DarkTheme.FG2;
        lblAbout.BackColor = Color.Transparent;
        tabAbout.Controls.Add(lblAbout);
    }

    // ---- SPEED PRESETS ----
    private void CmbSpeedPreset_Changed(object sender, EventArgs e)
    {
        int idx = cmbSpeedPreset.SelectedIndex;
        if (idx < 0 || idx >= SpeedPresetUs.Length) return;
        long us = SpeedPresetUs[idx];
        if (us < 0) return; // Custom — don't change values

        nudHours.Value = 0;
        nudMinutes.Value = 0;
        nudSeconds.Value = 0;
        nudMilliseconds.Value = Math.Min((us / 1000), 999);
        nudMicroseconds.Value = us % 1000;
    }

    // ---- TRAY ----
    private void SetupTray()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.BackColor = DarkTheme.BG2;
        trayMenu.ForeColor = DarkTheme.FG;
        trayMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
        trayMenu.Items.Add("Start/Stop", null, (s, e) => ToggleAll());
        trayMenu.Items.Add("-");
        trayMenu.Items.Add("Exit", null, (s, e) => { StopAll(); Application.Exit(); });

        trayIcon = new NotifyIcon();
        trayIcon.Text = "AutoClicker Pro v2.0";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

        // Create icon
        Bitmap bmp = new Bitmap(32, 32);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(DarkTheme.BG);
            // Mouse shape
            using (SolidBrush b = new SolidBrush(DarkTheme.Accent))
            {
                g.FillEllipse(b, 4, 2, 24, 28);
            }
            // Click indicator
            using (SolidBrush b = new SolidBrush(Color.White))
            {
                g.FillEllipse(b, 10, 5, 12, 12);
            }
            // Divider
            using (Pen p = new Pen(DarkTheme.BG, 2))
            {
                g.DrawLine(p, 16, 2, 16, 16);
                g.DrawLine(p, 4, 16, 28, 16);
            }
        }
        trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());

        // Also set form icon
        try { this.Icon = trayIcon.Icon; } catch { }

        this.Resize += (s, e) =>
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.ShowBalloonTip(1500, "AutoClicker Pro", "Minimized to tray. Hotkey still active!", ToolTipIcon.Info);
            }
        };
    }

    // ---- UI TIMER ----
    private void SetupUITimer()
    {
        apsStopwatch.Start();
        uiTimer = new System.Windows.Forms.Timer();
        uiTimer.Interval = 100;
        uiTimer.Tick += (s, e) =>
        {
            long count = Interlocked.Read(ref totalActions);
            lblClickCount.Text = "Actions: " + count.ToString("N0");

            // Calculate APS
            double elapsed = apsStopwatch.Elapsed.TotalSeconds;
            if (elapsed >= 0.5)
            {
                long delta = count - lastActionCount;
                currentAPS = delta / elapsed;
                lastActionCount = count;
                apsStopwatch.Restart();
            }

            if (isRunning)
            {
                lblActionsPerSec.Text = currentAPS.ToString("N1") + " /sec";
            }
            else
            {
                lblActionsPerSec.Text = "0 /sec";
            }
        };
        uiTimer.Start();
    }

    // ---- HOTKEY ----
    private void RegisterGlobalHotkey()
    {
        NativeMethods.RegisterHotKey(this.Handle, HOTKEY_TOGGLE, NativeMethods.MOD_NONE, currentHotkeyVK);
    }

    private void UnregisterGlobalHotkey()
    {
        NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_TOGGLE);
    }

    private void CmbHotkey_Changed(object sender, EventArgs e)
    {
        UnregisterGlobalHotkey();
        currentHotkeyVK = HotkeyVKs[cmbHotkey.SelectedIndex];
        RegisterGlobalHotkey();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_TOGGLE)
        {
            ToggleAll();
        }
        base.WndProc(ref m);
    }

    // ---- COORDINATE PICKER ----
    private void BtnPickCoords_Click(object sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
        Thread.Sleep(400);
        CoordinatePickerForm picker = new CoordinatePickerForm();
        picker.ShowDialog();
        if (picker.WasPicked)
        {
            nudFixedX.Value = picker.PickedX;
            nudFixedY.Value = picker.PickedY;
            rbFixedPos.Checked = true;
        }
        this.WindowState = FormWindowState.Normal;
    }

    // ---- PROFILES ----
    private void RefreshProfileList()
    {
        cmbProfiles.Items.Clear();
        foreach (string n in ProfileManager.GetProfileNames()) cmbProfiles.Items.Add(n);
        if (cmbProfiles.Items.Count > 0) cmbProfiles.SelectedIndex = 0;
    }

    private Profile GatherProfile()
    {
        Profile p = new Profile();
        p.Name = txtProfileName.Text.Trim();
        if (p.Name == "") p.Name = "Default";
        p.Hours = (int)nudHours.Value;
        p.Minutes = (int)nudMinutes.Value;
        p.Seconds = (int)nudSeconds.Value;
        p.Milliseconds = (int)nudMilliseconds.Value;
        p.Microseconds = (int)nudMicroseconds.Value;
        p.MouseButtonIndex = cmbMouseButton.SelectedIndex;
        p.ClickTypeIndex = cmbClickType.SelectedIndex;
        p.UseFixedPosition = rbFixedPos.Checked;
        p.FixedX = (int)nudFixedX.Value;
        p.FixedY = (int)nudFixedY.Value;
        p.UseBurstMode = chkBurstMode.Checked;
        p.BurstCount = (int)nudBurstCount.Value;
        p.UseJitter = chkJitter.Checked;
        p.JitterMs = (int)nudJitterMs.Value;
        p.HotkeyIndex = cmbHotkey.SelectedIndex;
        p.EnableKeySpam = chkEnableKeySpam.Checked;
        p.KeySpamList = txtKeyList.Text;
        p.KeySpamIntervalMs = (int)nudKeyIntervalMs.Value;
        p.KeyHoldMode = chkKeyHoldMode.Checked;
        p.SpeedPreset = cmbSpeedPreset.SelectedIndex;
        p.EnableMouseClick = chkEnableMouseClick.Checked;
        p.EnableScrollSpam = chkEnableScroll.Checked;
        p.ScrollDelta = (int)nudScrollDelta.Value;
        p.ScrollIntervalMs = (int)nudScrollIntervalMs.Value;
        return p;
    }

    private void ApplyProfile(Profile p)
    {
        txtProfileName.Text = p.Name;
        nudHours.Value = Clamp(p.Hours, nudHours);
        nudMinutes.Value = Clamp(p.Minutes, nudMinutes);
        nudSeconds.Value = Clamp(p.Seconds, nudSeconds);
        nudMilliseconds.Value = Clamp(p.Milliseconds, nudMilliseconds);
        nudMicroseconds.Value = Clamp(p.Microseconds, nudMicroseconds);
        cmbMouseButton.SelectedIndex = Math.Min(p.MouseButtonIndex, cmbMouseButton.Items.Count - 1);
        cmbClickType.SelectedIndex = Math.Min(p.ClickTypeIndex, cmbClickType.Items.Count - 1);
        if (p.UseFixedPosition) rbFixedPos.Checked = true; else rbCursorPos.Checked = true;
        nudFixedX.Value = Clamp(p.FixedX, nudFixedX);
        nudFixedY.Value = Clamp(p.FixedY, nudFixedY);
        chkBurstMode.Checked = p.UseBurstMode;
        nudBurstCount.Value = Clamp(p.BurstCount, nudBurstCount);
        chkJitter.Checked = p.UseJitter;
        nudJitterMs.Value = Clamp(p.JitterMs, nudJitterMs);
        cmbHotkey.SelectedIndex = Math.Min(p.HotkeyIndex, cmbHotkey.Items.Count - 1);
        chkEnableKeySpam.Checked = p.EnableKeySpam;
        txtKeyList.Text = p.KeySpamList;
        nudKeyIntervalMs.Value = Clamp(p.KeySpamIntervalMs, nudKeyIntervalMs);
        chkKeyHoldMode.Checked = p.KeyHoldMode;
        cmbSpeedPreset.SelectedIndex = Math.Min(p.SpeedPreset, cmbSpeedPreset.Items.Count - 1);
        chkEnableMouseClick.Checked = p.EnableMouseClick;
        chkEnableScroll.Checked = p.EnableScrollSpam;
        nudScrollDelta.Value = Clamp(p.ScrollDelta, nudScrollDelta);
        nudScrollIntervalMs.Value = Clamp(p.ScrollIntervalMs, nudScrollIntervalMs);
    }

    private decimal Clamp(int val, NumericUpDown nud)
    {
        return Math.Max(nud.Minimum, Math.Min(nud.Maximum, val));
    }

    private void BtnSaveProfile_Click(object sender, EventArgs e)
    {
        Profile p = GatherProfile();
        ProfileManager.Save(p);
        RefreshProfileList();
        MessageBox.Show("Profile '" + p.Name + "' saved!", "AutoClicker Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnLoadProfile_Click(object sender, EventArgs e)
    {
        if (cmbProfiles.SelectedItem == null) return;
        ApplyProfile(ProfileManager.Load(cmbProfiles.SelectedItem.ToString()));
    }

    private void BtnDeleteProfile_Click(object sender, EventArgs e)
    {
        if (cmbProfiles.SelectedItem == null) return;
        string name = cmbProfiles.SelectedItem.ToString();
        if (MessageBox.Show("Delete '" + name + "'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            ProfileManager.Delete(name);
            RefreshProfileList();
        }
    }

    // ============================================================
    // ENGINE — START / STOP
    // ============================================================

    private long GetIntervalUs()
    {
        return (long)nudHours.Value * 3600000000L +
               (long)nudMinutes.Value * 60000000L +
               (long)nudSeconds.Value * 1000000L +
               (long)nudMilliseconds.Value * 1000L +
               (long)nudMicroseconds.Value;
    }

    private void ToggleAll()
    {
        if (isRunning) StopAll(); else StartAll();
    }

    private void StartAll()
    {
        if (isRunning) return;
        isRunning = true;
        totalActions = 0;
        lastActionCount = 0;
        currentAPS = 0;
        apsStopwatch.Restart();

        bool enableMouse = chkEnableMouseClick.Checked;
        bool enableKeys = chkEnableKeySpam.Checked;
        bool enableScroll = chkEnableScroll.Checked;
        bool burst = chkBurstMode.Checked;
        int burstCount = (int)nudBurstCount.Value;
        bool jitter = chkJitter.Checked;
        int jitterMs = (int)nudJitterMs.Value;

        // Mouse thread
        if (enableMouse)
        {
            long intervalUs = GetIntervalUs();
            InputEngine.MouseButton button = (InputEngine.MouseButton)cmbMouseButton.SelectedIndex;
            InputEngine.ClickType clickType = (InputEngine.ClickType)cmbClickType.SelectedIndex;
            bool useFixed = rbFixedPos.Checked;
            int fx = (int)nudFixedX.Value;
            int fy = (int)nudFixedY.Value;

            clickThread = new Thread(() =>
            {
                Stopwatch sw = new Stopwatch();
                while (isRunning)
                {
                    if (burst && Interlocked.Read(ref totalActions) >= burstCount)
                    {
                        this.BeginInvoke(new Action(() => StopAll()));
                        break;
                    }

                    InputEngine.SendClick(button, clickType, useFixed, fx, fy);
                    Interlocked.Increment(ref totalActions);

                    long waitUs = intervalUs;
                    if (jitter)
                    {
                        long jitUs = (long)rng.Next(-jitterMs * 1000, jitterMs * 1000 + 1);
                        waitUs = Math.Max(0, waitUs + jitUs);
                    }

                    PrecisionWait(waitUs, sw);
                }
            });
            clickThread.IsBackground = true;
            clickThread.Priority = ThreadPriority.Highest;
            clickThread.Start();
        }

        // Keyboard thread
        if (enableKeys)
        {
            string keyListStr = txtKeyList.Text;
            int keyIntervalMs = (int)nudKeyIntervalMs.Value;
            bool holdMode = chkKeyHoldMode.Checked;

            keyThread = new Thread(() =>
            {
                string[] keyNames = keyListStr.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                List<ushort> vks = new List<ushort>();
                foreach (string kn in keyNames)
                {
                    ushort vk = KeyHelper.Parse(kn);
                    if (vk != 0) vks.Add(vk);
                }

                if (vks.Count == 0) return;

                if (holdMode)
                {
                    // Press all keys down
                    foreach (ushort vk in vks) InputEngine.SendKeyDown(vk);

                    while (isRunning)
                    {
                        if (burst && Interlocked.Read(ref totalActions) >= (int)nudBurstCount.Value)
                        {
                            break;
                        }
                        Thread.Sleep(50);
                    }

                    // Release all keys
                    foreach (ushort vk in vks) InputEngine.SendKeyUp(vk);
                }
                else
                {
                    Stopwatch sw = new Stopwatch();
                    int keyIndex = 0;
                    while (isRunning)
                    {
                        if (burst && Interlocked.Read(ref totalActions) >= (int)nudBurstCount.Value)
                        {
                            this.BeginInvoke(new Action(() => StopAll()));
                            break;
                        }

                        InputEngine.SendKeyPress(vks[keyIndex]);
                        Interlocked.Increment(ref totalActions);
                        keyIndex = (keyIndex + 1) % vks.Count;

                        PrecisionWait((long)keyIntervalMs * 1000L, sw);
                    }
                }
            });
            keyThread.IsBackground = true;
            keyThread.Priority = ThreadPriority.Highest;
            keyThread.Start();
        }

        // Scroll thread
        if (enableScroll)
        {
            int scrollDelta = (int)nudScrollDelta.Value;
            int scrollIntervalMs = (int)nudScrollIntervalMs.Value;

            scrollThread = new Thread(() =>
            {
                Stopwatch sw = new Stopwatch();
                while (isRunning)
                {
                    if (burst && Interlocked.Read(ref totalActions) >= (int)nudBurstCount.Value)
                    {
                        this.BeginInvoke(new Action(() => StopAll()));
                        break;
                    }

                    InputEngine.SendScrollWheel(scrollDelta);
                    Interlocked.Increment(ref totalActions);

                    PrecisionWait((long)scrollIntervalMs * 1000L, sw);
                }
            });
            scrollThread.IsBackground = true;
            scrollThread.Priority = ThreadPriority.AboveNormal;
            scrollThread.Start();
        }

        // Update UI
        lblStatus.Text = "▶  RUNNING";
        lblStatus.ForeColor = DarkTheme.Green;
        btnStart.Enabled = false;
        btnStop.Enabled = true;
        trayIcon.Text = "AutoClicker Pro - RUNNING";
    }

    private void StopAll()
    {
        if (!isRunning) return;
        isRunning = false;

        if (clickThread != null && clickThread.IsAlive) clickThread.Join(1000);
        if (keyThread != null && keyThread.IsAlive) keyThread.Join(1000);
        if (scrollThread != null && scrollThread.IsAlive) scrollThread.Join(1000);

        clickThread = null;
        keyThread = null;
        scrollThread = null;

        lblStatus.Text = "⏹  STOPPED";
        lblStatus.ForeColor = DarkTheme.Red;
        btnStart.Enabled = true;
        btnStop.Enabled = false;
        trayIcon.Text = "AutoClicker Pro - STOPPED";
    }

    // ---- HIGH PRECISION WAIT ----
    private void PrecisionWait(long microseconds, Stopwatch sw)
    {
        if (microseconds <= 0) return;

        if (microseconds > 15000) // > 15ms — safe to use Sleep for most of it
        {
            long sleepMs = (microseconds / 1000) - 5; // Sleep all but last 5ms
            if (sleepMs > 0) Thread.Sleep((int)sleepMs);

            // Busy-wait the remainder
            sw.Restart();
            long remainUs = microseconds - (sleepMs * 1000);
            long remainTicks = (remainUs * Stopwatch.Frequency) / 1000000;
            while (sw.ElapsedTicks < remainTicks && isRunning)
            {
                Thread.SpinWait(10);
            }
        }
        else if (microseconds > 1000) // 1ms - 15ms — hybrid
        {
            sw.Restart();
            long halfTicks = (microseconds * Stopwatch.Frequency) / 2000000;
            // Sleep 1ms then spin
            Thread.Sleep(1);
            long targetTicks = (microseconds * Stopwatch.Frequency) / 1000000;
            while (sw.ElapsedTicks < targetTicks && isRunning)
            {
                Thread.SpinWait(10);
            }
        }
        else // < 1ms — pure busy-wait
        {
            sw.Restart();
            long targetTicks = (microseconds * Stopwatch.Frequency) / 1000000;
            while (sw.ElapsedTicks < targetTicks && isRunning)
            {
                Thread.SpinWait(1);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopAll();
        UnregisterGlobalHotkey();
        NativeMethods.timeEndPeriod(1);
        if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
        base.OnFormClosing(e);
    }
}

// ============================================================
// ENTRY POINT
// ============================================================

public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new AutoClickerForm());
    }
}