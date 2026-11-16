// P-EyeRest - nhắc nghỉ mắt bằng overlay mờ, click-through, không cướp focus.
// Copyright (C) 2026 Phúc
// SPDX-License-Identifier: GPL-3.0-or-later
//
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License as published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version. This program is distributed in the hope that it
// will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. You should
// have received a copy of the GNU General Public License along with this program (file LICENSE).
// If not, see <https://www.gnu.org/licenses/>.
//
// Mốc thời gian:
//   - Nghỉ ngắn: quy tắc 20-20-20 (AOA): mỗi 20 phút nhìn xa ~6 m trong 20 giây.
//   - Nghỉ dài: AOA khuyên nghỉ màn hình 15 phút sau 2 giờ dùng liên tục.
//   - Bài đưa mắt theo hướng: chỉ để thư giãn; bằng chứng yếu hơn nhìn xa / chớp mắt.
// Build: build.bat (csc .NET Framework 4.x, C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// Version: nơi khai báo duy nhất, hiện trong Properties > Details của exe, menu khay và hộp Giới thiệu.
[assembly: AssemblyTitle(Program.AppName + " - nhắc nghỉ mắt")]
[assembly: AssemblyProduct(Program.AppName)]
[assembly: AssemblyCompany(Program.Author)]
[assembly: AssemblyCopyright("© 2026 " + Program.Author)]
[assembly: AssemblyVersion("1.2.0")]
[assembly: AssemblyFileVersion("1.2.0")]

static class Program
{
    public const string AppName = "P-EyeRest", Author = "Phúc";

    public static string VersionText()
    {
        var v = typeof(Program).Assembly.GetName().Version;
        return string.Format("v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
    }

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [STAThread]
    static void Main(string[] args)
    {
        bool created;
        using (var mutex = new Mutex(true, "Local\\P-EyeRest_SingleInstance", out created))
        {
            if (!created) return;
            // Per-monitor v2 (Win10 1703+) để overlay nét trên màn hình khác DPI; máy cũ hơn → system-aware.
            try { if (!SetProcessDpiAwarenessContext(new IntPtr(-4))) SetProcessDPIAware(); }
            catch (EntryPointNotFoundException) { SetProcessDPIAware(); }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool test = Array.IndexOf(args, "--test") >= 0;
            var cfg = test ? Config.Test() : Config.Default();
            if (!test) cfg.LoadSettings();
            Application.Run(new EyeRestApp(cfg));
            GC.KeepAlive(mutex);
        }
    }
}

class Config
{
    // Khóa trong settings.ini (chỉnh qua menu khay "Thời gian nhắc").
    public const string KeyMicroEvery = "micro_every_min", KeyMicroSec = "micro_sec", KeyLongEvery = "long_every_min",
                        KeyLongMin = "long_min", KeyAwayAfter = "away_after_min", KeyDeferFullscreen = "defer_fullscreen",
                        KeyDeferMax = "defer_max_min";

    public int MicroEverySec, MicroDurSec, LongEverySec, LongDurSec;   // LongEverySec = 0: tắt nghỉ dài
    public int IdleResetMicroSec, IdleResetLongSec, SoftAwaySec, StepSec;   // SoftAwaySec = 0: chỉ tính rời máy khi khóa/tắt màn
    public bool DeferFullscreen = true;   // hoãn nhắc khi đang có app toàn màn hình hoặc trình chiếu
    public int DeferMaxSec = 30 * 60;     // hoãn quá mốc này thì vẫn nhắc; 0 = hoãn không giới hạn

    public static Config Default()
    {
        return new Config
        {
            MicroEverySec = 20 * 60, MicroDurSec = 20,
            LongEverySec = 120 * 60, LongDurSec = 15 * 60,
            IdleResetMicroSec = 5 * 60,   // rời máy >= 5 phút: coi như đã nghỉ ngắn
            IdleResetLongSec = 15 * 60,   // rời máy >= thời lượng nghỉ dài: coi như đã nghỉ dài
            SoftAwaySec = 15 * 60,        // màn còn sáng: không chạm máy >= 15 phút và không có video → rời máy
            StepSec = 5
        };
    }

    // Đọc mốc thời gian người dùng chọn; giá trị thiếu hoặc ngoài khoảng → giữ mặc định ở Default().
    public void LoadSettings()
    {
        MicroEverySec = (int)(Settings.Get(KeyMicroEvery, 20, 5, 120) * 60);
        MicroDurSec = (int)Settings.Get(KeyMicroSec, 20, 10, 120);
        LongEverySec = (int)(Settings.Get(KeyLongEvery, 120, 0, 480) * 60);
        LongDurSec = (int)(Settings.Get(KeyLongMin, 15, 2, 60) * 60);
        SoftAwaySec = (int)(Settings.Get(KeyAwayAfter, 15, 0, 120) * 60);
        DeferFullscreen = Settings.Get(KeyDeferFullscreen, 1, 0, 1) >= 0.5f;
        DeferMaxSec = (int)(Settings.Get(KeyDeferMax, 30, 0, 240) * 60);
        IdleResetLongSec = Math.Max(LongDurSec, IdleResetMicroSec);
    }

    // --test: rút ngắn chu kỳ để kiểm thử.
    public static Config Test()
    {
        return new Config
        {
            MicroEverySec = 10, MicroDurSec = 8,
            LongEverySec = 40, LongDurSec = 40,
            IdleResetMicroSec = 3600, IdleResetLongSec = 3600, SoftAwaySec = 3600,
            StepSec = 2
        };
    }
}

enum Mode { Far, Look, Circle, Blink, Closed }

// Khởi động cùng Windows: shortcut trong thư mục Startup của người dùng (không cần quyền admin, không ghi registry).
static class Startup
{
    public static string LinkPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), Program.AppName + ".lnk");

    public static bool IsEnabled { get { return File.Exists(LinkPath); } }

    public static void Enable()
    {
        WithShortcut(delegate (object lnk, Type t)
        {
            string exe = Application.ExecutablePath;
            t.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { exe });
            t.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(exe) });
            t.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { Program.AppName + " - nhắc nghỉ mắt" });
            t.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        });
    }

    public static void Disable()
    {
        if (File.Exists(LinkPath)) File.Delete(LinkPath);
    }

    // Exe bị chuyển chỗ thì shortcut cũ trỏ sai: lúc mở app, nếu đang bật mà sai đích thì ghi lại.
    // Lỗi ở đây chỉ làm shortcut không được cập nhật, không ảnh hưởng chức năng nhắc nghỉ, nên bỏ qua.
    public static void RefreshTarget()
    {
        if (!IsEnabled) return;
        try
        {
            string target = null;
            WithShortcut(delegate (object lnk, Type t)
            {
                target = (string)t.InvokeMember("TargetPath", BindingFlags.GetProperty, null, lnk, null);
            });
            if (!string.Equals(target, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase)) Enable();
        }
        catch (COMException) { }
        catch (TargetInvocationException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    static void WithShortcut(Action<object, Type> body)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell", true);
        object shell = Activator.CreateInstance(shellType);
        object lnk = null;
        try
        {
            lnk = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { LinkPath });
            body(lnk, lnk.GetType());
        }
        finally
        {
            if (lnk != null) Marshal.ReleaseComObject(lnk);
            Marshal.ReleaseComObject(shell);
        }
    }
}

// Lựa chọn của người dùng, lưu ở %APPDATA%\P-EyeRest\settings.ini, mỗi dòng "khóa=số" (ví dụ "fill=0.7").
static class Settings
{
    static string filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Program.AppName, "settings.ini");
    static Dictionary<string, float> values;   // nạp từ file ở lần Get/Set đầu tiên

    public static string FilePath
    {
        get { return filePath; }
        set { filePath = value; values = null; }
    }

    // Giá trị trong file nếu hợp lệ và nằm trong [min, max]; ngược lại trả về def.
    public static float Get(string key, float def, float min, float max)
    {
        float v;
        return Values().TryGetValue(key, out v) && v >= min && v <= max ? v : def;
    }

    public static void Set(string key, float value)
    {
        Values()[key] = value;
        Save();
    }

    static Dictionary<string, float> Values()
    {
        if (values != null) return values;
        values = new Dictionary<string, float>();
        try
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                var kv = line.Split(new[] { '=' }, 2);
                float v;
                if (kv.Length == 2 && float.TryParse(kv[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    values[kv[0].Trim()] = v;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return values;
    }

    // Ghi file tạm cùng thư mục rồi thay thế, để file không bị hỏng nửa chừng.
    static void Save()
    {
        var keys = new List<string>(values.Keys);
        keys.Sort(StringComparer.Ordinal);
        var sb = new System.Text.StringBuilder();
        foreach (var k in keys) sb.Append(k).Append('=').Append(values[k].ToString("0.###", CultureInfo.InvariantCulture)).Append("\r\n");

        string tmp = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(tmp, sb.ToString());
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}

// Tín hiệu "người dùng có đang nhìn màn hình không", lấy từ API Windows có sẵn.
class Presence : NativeWindow, IDisposable
{
    const int WM_POWERBROADCAST = 0x218, PBT_POWERSETTINGCHANGE = 0x8013;
    const int QUNS_NOT_PRESENT = 1, QUNS_BUSY = 2, QUNS_RUNNING_D3D_FULL_SCREEN = 3, QUNS_PRESENTATION_MODE = 4, QUNS_APP = 7;
    const int SystemExecutionState = 16;
    const uint ES_DISPLAY_REQUIRED = 0x2;
    static Guid GUID_SESSION_DISPLAY_STATUS = new Guid("2B84C20E-AD23-4DDF-93DB-05FFBD7EFCA5");

    [DllImport("user32.dll")] static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, int flags);
    [DllImport("user32.dll")] static extern bool UnregisterPowerSettingNotification(IntPtr handle);
    [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int state);
    [DllImport("powrprof.dll")] static extern uint CallNtPowerInformation(int level, IntPtr inBuf, uint inLen, out uint outBuf, uint outLen);

    readonly IntPtr notifyHandle;
    bool displayOff;

    public Presence()
    {
        CreateHandle(new CreateParams());   // cửa sổ ẩn để nhận WM_POWERBROADCAST
        notifyHandle = RegisterPowerSettingNotification(Handle, ref GUID_SESSION_DISPLAY_STATUS, 0 /*DEVICE_NOTIFY_WINDOW_HANDLE*/);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_POWERBROADCAST && m.WParam.ToInt64() == PBT_POWERSETTINGCHANGE && m.LParam != IntPtr.Zero)
        {
            // POWERBROADCAST_SETTING: Guid (16 byte) + DataLength (4 byte) + Data (DWORD MONITOR_DISPLAY_STATE)
            var guid = (Guid)Marshal.PtrToStructure(m.LParam, typeof(Guid));
            if (guid == GUID_SESSION_DISPLAY_STATUS) displayOff = Marshal.ReadInt32(m.LParam, 20) == 0;
            m.Result = new IntPtr(1);
            return;
        }
        base.WndProc(ref m);
    }

    public bool DisplayOff { get { return displayOff; } }

    public static int NotificationState()
    {
        int st;
        return SHQueryUserNotificationState(out st) == 0 ? st : 0;
    }

    // Chắc chắn không nhìn màn hình: tắt màn, khóa máy, screensaver, phiên Fast User Switching khác.
    public bool HardAway { get { return displayOff || NotificationState() == QUNS_NOT_PRESENT; } }

    // App toàn màn hình (game, video F11), DirectX độc quyền, chế độ trình chiếu hoặc app Store toàn màn hình.
    public static bool Fullscreen()
    {
        int st = NotificationState();
        return st == QUNS_BUSY || st == QUNS_RUNNING_D3D_FULL_SCREEN || st == QUNS_PRESENTATION_MODE || st == QUNS_APP;
    }

    // Có app đang yêu cầu giữ màn hình sáng. Trình duyệt và trình phát video bật cờ này khi phát video,
    // kể cả video tắt tiếng; phát nhạc không có hình thì không bật (đã đo với Edge).
    public static bool DisplayRequired()
    {
        uint st;
        return CallNtPowerInformation(SystemExecutionState, IntPtr.Zero, 0, out st, 4) == 0 && (st & ES_DISPLAY_REQUIRED) != 0;
    }

    public static bool Watching() { return Fullscreen() || DisplayRequired(); }

    // softAwaySec = 0: chỉ tính rời máy khi Windows báo rõ (khóa, screensaver, tắt màn).
    public static bool IsAway(uint idleSec, bool hardAway, bool watching, int softAwaySec)
    {
        return hardAway || (softAwaySec > 0 && !watching && idleSec >= softAwaySec);
    }

    public void Dispose()
    {
        if (notifyHandle != IntPtr.Zero) UnregisterPowerSettingNotification(notifyHandle);
        DestroyHandle();
    }
}

class Step
{
    public string Title, Sub; public Mode Mode; public float Dx, Dy; public int Sec;
    public Step(string title, string sub, Mode mode, float dx, float dy, int sec)
    { Title = title; Sub = sub; Mode = mode; Dx = dx; Dy = dy; Sec = sec; }
}

class EyeRestApp : ApplicationContext
{
    enum State { Working, Micro, Long }

    [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    readonly Config cfg;
    readonly NotifyIcon tray;
    readonly List<Overlay> overlays = new List<Overlay>();   // mỗi màn hình một overlay
    readonly System.Windows.Forms.Timer tick;
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly List<Step> longSteps;
    readonly ToolStripMenuItem pauseItem;
    readonly List<ToolStripMenuItem> sizeItems = new List<ToolStripMenuItem>();
    static readonly string[] SizeNames = { "Nhỏ (50%)", "Vừa (70%)", "Lớn (85%)", "Gần toàn màn (92%)" };
    static readonly float[] SizeValues = { 0.5f, 0.7f, 0.85f, 0.92f };
    readonly List<ToolStripMenuItem> opacityItems = new List<ToolStripMenuItem>();
    static readonly string[] OpacityNames = { "Rất mờ (35%)", "Mờ (55%)", "Vừa (75%)", "Đậm (90%)" };
    static readonly float[] OpacityValues = { 0.35f, 0.55f, 0.75f, 0.9f };
    const double DimRatio = 0.53;   // pha "Rời màn hình" mờ hơn: 75% → 40%
    const int PreviewSec = 6;
    // Độ đậm < 1 để WinForms giữ cửa sổ layered (click-through).
    const string KeyFill = "fill", KeyOpacity = "opacity";

    float fill = Settings.Get(KeyFill, 0.7f, 0.3f, 0.95f);
    float opacity = Settings.Get(KeyOpacity, 0.75f, 0.2f, 0.9f);
    long previewUntilMs = -1;
    State state = State.Working;
    double microAcc, longAcc, breakElapsed;
    long lastMs, pauseUntilMs = -1, deferSinceMs = -1;
    bool away, deferred;
    readonly Presence presence = new Presence();
    DateTime lastWall = DateTime.UtcNow;

    public EyeRestApp(Config config)
    {
        cfg = config;
        EnsureOverlays();
        longSteps = BuildLongSteps(cfg.StepSec);

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem(Program.AppName + " " + Program.VersionText()) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Nghỉ ngắn ngay (20-20-20)", null, delegate { StartBreak(State.Micro); });
        menu.Items.Add("Nghỉ dài ngay (bài mắt + rời màn hình)", null, delegate { StartBreak(State.Long); });
        menu.Items.Add("Bỏ qua lần nghỉ đang hiện", null, delegate { if (state != State.Working) EndBreak(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Xem thử overlay", null, delegate { Preview(); });
        menu.Items.Add(ChoiceMenu("Kích thước overlay", SizeNames, SizeValues, sizeItems, fill, SetFill));
        menu.Items.Add(ChoiceMenu("Độ đậm overlay", OpacityNames, OpacityValues, opacityItems, opacity, SetOpacity));
        menu.Items.Add(TimingMenu());
        menu.Items.Add(new ToolStripSeparator());
        pauseItem = new ToolStripMenuItem("Tạm dừng 1 giờ", null, delegate { TogglePause(); });
        menu.Items.Add(pauseItem);
        var startupItem = new ToolStripMenuItem("Khởi động cùng Windows");
        startupItem.Click += delegate { ToggleStartup(); startupItem.Checked = Startup.IsEnabled; };
        menu.Opening += delegate { startupItem.Checked = Startup.IsEnabled; };   // người dùng có thể xóa shortcut bằng tay
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Giới thiệu " + Program.AppName, null, delegate { ShowAbout(); });
        menu.Items.Add("Thoát", null, delegate { Quit(); });

        tray = new NotifyIcon { Icon = LoadTrayIcon(), ContextMenuStrip = menu, Visible = true, Text = Program.AppName };
        Startup.RefreshTarget();

        tick = new System.Windows.Forms.Timer { Interval = 500 };
        tick.Tick += delegate { OnTick(); };
        lastMs = clock.ElapsedMilliseconds;
        tick.Start();
    }

    static List<Step> BuildLongSteps(int s)
    {
        const float d = 0.7071f;
        const string keep = "Giữ đầu yên, chỉ đưa mắt";
        return new List<Step>
        {
            new Step("Nhìn lên", keep, Mode.Look, 0, -1, s),
            new Step("Nhìn xuống", keep, Mode.Look, 0, 1, s),
            new Step("Nhìn sang trái", keep, Mode.Look, -1, 0, s),
            new Step("Nhìn sang phải", keep, Mode.Look, 1, 0, s),
            new Step("Nhìn chéo lên trái", keep, Mode.Look, -d, -d, s),
            new Step("Nhìn chéo xuống phải", keep, Mode.Look, d, d, s),
            new Step("Nhìn chéo lên phải", keep, Mode.Look, d, -d, s),
            new Step("Nhìn chéo xuống trái", keep, Mode.Look, -d, d, s),
            new Step("Đảo tròn mắt chậm", "Theo chiều kim đồng hồ", Mode.Circle, 0, 0, s * 2),
            new Step("Chớp mắt nhẹ, liên tục", "Làm ẩm lại bề mặt mắt", Mode.Blink, 0, 0, s * 2),
            new Step("Nhắm mắt, thả lỏng", "Có thể úp hai lòng bàn tay lên mắt", Mode.Closed, 0, 0, s * 4),
        };
    }

    static uint IdleSeconds()
    {
        var li = new LASTINPUTINFO();
        li.cbSize = (uint)Marshal.SizeOf(li);
        if (!GetLastInputInfo(ref li)) return 0;
        return unchecked((uint)Environment.TickCount - li.dwTime) / 1000;
    }

    void OnTick()
    {
        long now = clock.ElapsedMilliseconds;
        DateTime wall = DateTime.UtcNow;
        double dt = (now - lastMs) / 1000.0;
        double gap = (wall - lastWall).TotalSeconds;
        lastMs = now; lastWall = wall;

        if (previewUntilMs >= 0)
        {
            if (now >= previewUntilMs) { previewUntilMs = -1; foreach (var o in overlays) o.HideCue(); }
            else ShowPreviewFrame();
        }

        // Máy sleep/hibernate: timer không tick nên idle không bắt được; khoảng trống đồng hồ tường = đã rời máy.
        if (gap >= cfg.IdleResetMicroSec)
        {
            microAcc = 0;
            if (gap >= cfg.IdleResetLongSec) longAcc = 0;
            if (state != State.Working) EndBreak();
            UpdateTooltip();
            return;
        }

        if (state == State.Working)
        {
            if (pauseUntilMs >= 0)
            {
                if (now < pauseUntilMs) { UpdateTooltip(); return; }
                Resume();
            }
            // Không chạm phím/chuột chưa chắc là nghỉ (đang xem video, đang đọc).
            // Chỉ tính là rời máy khi Windows báo rõ (khóa, screensaver, tắt màn) hoặc idle lâu mà không có dấu hiệu đang xem.
            uint idle = IdleSeconds();
            away = Presence.IsAway(idle, presence.HardAway, Presence.Watching(), cfg.SoftAwaySec);
            if (away)
            {
                if (idle >= cfg.IdleResetMicroSec) microAcc = 0;
                if (idle >= cfg.IdleResetLongSec) longAcc = 0;
            }
            else
            {
                microAcc += dt;
                if (cfg.LongEverySec > 0) longAcc += dt;
            }

            bool longDue = cfg.LongEverySec > 0 && longAcc >= cfg.LongEverySec;
            bool microDue = microAcc >= cfg.MicroEverySec;
            // Đang chơi game, trình chiếu, xem video toàn màn hình: chờ thoát toàn màn hình rồi mới nhắc,
            // nhưng hoãn quá DeferMaxSec thì vẫn nhắc.
            if ((longDue || microDue) && cfg.DeferFullscreen && Presence.Fullscreen())
            {
                if (deferSinceMs < 0) deferSinceMs = now;
                deferred = cfg.DeferMaxSec == 0 || now - deferSinceMs < cfg.DeferMaxSec * 1000L;
            }
            else { deferSinceMs = -1; deferred = false; }
            if (!deferred)
            {
                deferSinceMs = -1;
                if (longDue) StartBreak(State.Long);
                else if (microDue) StartBreak(State.Micro);
            }
            UpdateTooltip();
            return;
        }

        breakElapsed += dt;
        int dur = state == State.Micro ? cfg.MicroDurSec : cfg.LongDurSec;
        if (breakElapsed >= dur) { EndBreak(); return; }
        ShowBreakFrame(dur);
    }

    void StartBreak(State kind)
    {
        state = kind;
        breakElapsed = 0;
        previewUntilMs = -1;
        EnsureOverlays();
        ShowBreakFrame(kind == State.Micro ? cfg.MicroDurSec : cfg.LongDurSec);
        foreach (var o in overlays) o.ShowCue();
    }

    // Hiện overlay vài giây để xem kích thước; không ảnh hưởng bộ đếm nghỉ.
    void Preview()
    {
        if (state != State.Working) return;   // đang nghỉ thì overlay đã hiện sẵn
        EnsureOverlays();
        previewUntilMs = clock.ElapsedMilliseconds + PreviewSec * 1000L;
        ShowPreviewFrame();
        foreach (var o in overlays) o.ShowCue();
    }

    void ShowPreviewFrame()
    {
        int remain = Math.Max(0, (int)Math.Ceiling((previewUntilMs - clock.ElapsedMilliseconds) / 1000.0));
        Cue("Xem thử overlay", "Kích thước " + Math.Round(fill * 100) + "% màn hình · độ đậm " + Math.Round(opacity * 100) + "%",
            Mode.Far, 0, 0, remain, 1 - remain / (double)PreviewSec, opacity);
    }

    static ToolStripMenuItem ChoiceMenu(string title, string[] names, float[] values, List<ToolStripMenuItem> items,
                                        float current, Action<float> onPick)
    {
        var menu = new ToolStripMenuItem(title);
        for (int i = 0; i < values.Length; i++)
        {
            float v = values[i];
            var item = new ToolStripMenuItem(names[i], null, delegate { onPick(v); });
            item.Tag = v;
            items.Add(item);
            menu.DropDownItems.Add(item);
        }
        UpdateChecks(items, current);
        return menu;
    }

    static void UpdateChecks(List<ToolStripMenuItem> items, float current)
    {
        foreach (var it in items) it.Checked = Math.Abs((float)it.Tag - current) < 0.005f;
    }

    // Submenu "Thời gian nhắc": mỗi lựa chọn ghi vào settings.ini rồi nạp lại Config, có hiệu lực ngay.
    ToolStripMenuItem TimingMenu()
    {
        var menu = new ToolStripMenuItem("Thời gian nhắc");
        menu.DropDownItems.Add(TimingChoice("Nghỉ ngắn mỗi", Config.KeyMicroEvery, cfg.MicroEverySec / 60f,
            new[] { "15 phút", "20 phút (mặc định)", "30 phút", "45 phút" }, new[] { 15f, 20f, 30f, 45f }));
        menu.DropDownItems.Add(TimingChoice("Nghỉ ngắn trong", Config.KeyMicroSec, cfg.MicroDurSec,
            new[] { "20 giây (mặc định)", "30 giây", "60 giây" }, new[] { 20f, 30f, 60f }));
        menu.DropDownItems.Add(TimingChoice("Nghỉ dài mỗi", Config.KeyLongEvery, cfg.LongEverySec / 60f,
            new[] { "1 giờ", "1 giờ 30 phút", "2 giờ (mặc định)", "3 giờ", "Tắt nghỉ dài" }, new[] { 60f, 90f, 120f, 180f, 0f }));
        menu.DropDownItems.Add(TimingChoice("Nghỉ dài trong", Config.KeyLongMin, cfg.LongDurSec / 60f,
            new[] { "5 phút", "10 phút", "15 phút (mặc định)" }, new[] { 5f, 10f, 15f }));
        menu.DropDownItems.Add(TimingChoice("Coi là rời máy khi không chạm máy", Config.KeyAwayAfter, cfg.SoftAwaySec / 60f,
            new[] { "10 phút", "15 phút (mặc định)", "30 phút", "Không bao giờ (chỉ khi khóa máy, tắt màn hình)" },
            new[] { 10f, 15f, 30f, 0f }));
        menu.DropDownItems.Add(new ToolStripSeparator());
        var deferItem = new ToolStripMenuItem("Hoãn nhắc khi đang toàn màn hình") { Checked = cfg.DeferFullscreen };
        var deferMax = TimingChoice("Hoãn tối đa", Config.KeyDeferMax, cfg.DeferMaxSec / 60f,
            new[] { "15 phút", "30 phút (mặc định)", "60 phút", "Không giới hạn" }, new[] { 15f, 30f, 60f, 0f });
        deferMax.Enabled = cfg.DeferFullscreen;
        deferItem.Click += delegate
        {
            deferItem.Checked = !deferItem.Checked;
            deferMax.Enabled = deferItem.Checked;
            Settings.Set(Config.KeyDeferFullscreen, deferItem.Checked ? 1 : 0);
            cfg.LoadSettings();
        };
        menu.DropDownItems.Add(deferItem);
        menu.DropDownItems.Add(deferMax);
        return menu;
    }

    ToolStripMenuItem TimingChoice(string title, string key, float current, string[] names, float[] values)
    {
        var items = new List<ToolStripMenuItem>();
        return ChoiceMenu(title, names, values, items, current, delegate (float v)
        {
            Settings.Set(key, v);
            cfg.LoadSettings();
            UpdateChecks(items, v);
            UpdateTooltip();
        });
    }

    void SetFill(float f)
    {
        fill = f;
        UpdateChecks(sizeItems, fill);
        ApplyLook();
    }

    void SetOpacity(float o)
    {
        opacity = o;
        UpdateChecks(opacityItems, opacity);
        ApplyLook();
    }

    // Lưu lựa chọn rồi cho xem ngay: đang nghỉ thì cập nhật overlay tại chỗ, không thì hiện xem thử.
    void ApplyLook()
    {
        Settings.Set(KeyFill, fill);
        Settings.Set(KeyOpacity, opacity);
        EnsureOverlays();
        if (state == State.Working) { Preview(); return; }
        ShowBreakFrame(state == State.Micro ? cfg.MicroDurSec : cfg.LongDurSec);
        foreach (var o in overlays) o.ShowCue();
    }

    // Tạo lại overlay khi số màn hình, vùng làm việc hoặc kích thước chọn thay đổi.
    void EnsureOverlays()
    {
        var screens = Screen.AllScreens;
        bool same = overlays.Count == screens.Length && (overlays.Count == 0 || overlays[0].Fill == fill);
        for (int i = 0; same && i < screens.Length; i++) same = overlays[i].Area == screens[i].WorkingArea;
        if (same) return;
        foreach (var o in overlays) o.Close();
        overlays.Clear();
        foreach (var sc in screens) overlays.Add(new Overlay(sc.WorkingArea, fill));
    }

    void Cue(string title, string sub, Mode mode, float dx, float dy, int remain, double progress, double opacity)
    {
        foreach (var o in overlays)
        {
            o.TargetOpacity = opacity;
            o.SetCue(title, sub, mode, dx, dy, remain, progress);
        }
    }

    void ShowBreakFrame(int dur)
    {
        int remain = (int)Math.Ceiling(dur - breakElapsed);
        double progress = breakElapsed / dur;
        if (state == State.Micro)
        {
            Cue("Nhìn xa ≥ 6 m", "Thả lỏng mắt, chớp mắt chậm vài lần", Mode.Far, 0, 0, remain, progress, opacity);
            return;
        }
        double t = breakElapsed;
        foreach (var st in longSteps)
        {
            if (t < st.Sec) { Cue(st.Title, st.Sub, st.Mode, st.Dx, st.Dy, remain, progress, opacity); return; }
            t -= st.Sec;
        }
        // Pha dài: mờ hơn để ít che nội dung.
        Cue("Rời màn hình, nhìn ra xa", "Đứng dậy đi lại. Tự ẩn khi hết giờ", Mode.Far, 0, 0, remain, progress, opacity * DimRatio);
    }

    void EndBreak()
    {
        if (state == State.Long) longAcc = 0;
        microAcc = 0;
        state = State.Working;
        foreach (var o in overlays) o.HideCue();
        UpdateTooltip();
    }

    void TogglePause()
    {
        if (pauseUntilMs >= 0) { Resume(); return; }
        if (state != State.Working) EndBreak();
        pauseUntilMs = clock.ElapsedMilliseconds + 3600 * 1000L;
        pauseItem.Text = "Tiếp tục";
        UpdateTooltip();
    }

    void Resume()
    {
        pauseUntilMs = -1;
        deferSinceMs = -1;
        microAcc = 0;
        pauseItem.Text = "Tạm dừng 1 giờ";
    }

    void UpdateTooltip()
    {
        string text;
        if (pauseUntilMs >= 0)
            text = Program.AppName + " - tạm dừng, còn " + Fmt((pauseUntilMs - clock.ElapsedMilliseconds) / 1000.0);
        else if (away && state == State.Working)
            text = Program.AppName + " - đang rời máy, dừng đếm";
        else if (deferred && state == State.Working)
            text = Program.AppName + " - hoãn vì toàn màn hình"
                 + (cfg.DeferMaxSec > 0 ? ", nhắc sau " + Fmt(cfg.DeferMaxSec - (clock.ElapsedMilliseconds - deferSinceMs) / 1000.0) : "");
        else
            text = Program.AppName + " - ngắn sau " + Fmt(cfg.MicroEverySec - microAcc) + " | dài "
                 + (cfg.LongEverySec > 0 ? "sau " + Fmt(cfg.LongEverySec - longAcc) : "tắt");
        if (text.Length > 63) text = text.Substring(0, 63);
        tray.Text = text;
    }

    static string Fmt(double sec)
    {
        int s = Math.Max(0, (int)Math.Ceiling(sec));
        return s >= 3600 ? string.Format("{0}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60)
                         : string.Format("{0}:{1:00}", s / 60, s % 60);
    }

    void ToggleStartup()
    {
        try
        {
            if (Startup.IsEnabled) Startup.Disable(); else Startup.Enable();
        }
        catch (Exception ex)
        {
            if (!(ex is IOException || ex is UnauthorizedAccessException || ex is COMException || ex is TargetInvocationException)) throw;
            MessageBox.Show("Không đổi được chế độ khởi động cùng Windows:\n" + ex.Message, Program.AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static void ShowAbout()
    {
        MessageBox.Show(
            Program.AppName + " " + Program.VersionText() + "\n" +
            "Nhắc nghỉ mắt cho Windows.\n" +
            "Tác giả: " + Program.Author + "\n" +
            "Mã nguồn mở, giấy phép GPL-3.0-or-later.\n\n" +
            "• Mặc định mỗi 20 phút: nhìn xa ≥ 6 m trong 20 giây (quy tắc 20-20-20).\n" +
            "• Mặc định mỗi 2 giờ: nghỉ 15 phút, kèm bài đưa mắt thư giãn.\n" +
            "• Chỉnh các mốc này trong menu khay > Thời gian nhắc.\n" +
            "• Overlay mờ, chuột bấm xuyên qua, không chiếm bàn phím.\n" +
            "• Tự dừng đếm khi khóa máy, tắt màn hoặc rời máy lâu; hoãn nhắc khi đang toàn màn hình.\n\n" +
            "Cài đặt lưu tại:\n" + Settings.FilePath,
            "Giới thiệu " + Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Icon khay lấy từ EyeRest.ico nhúng trong exe (build.bat: /resource), chọn đúng cỡ icon nhỏ của hệ thống.
    static Icon LoadTrayIcon()
    {
        using (var s = typeof(EyeRestApp).Assembly.GetManifestResourceStream("EyeRest.ico"))
            return s != null ? new Icon(s, SystemInformation.SmallIconSize) : SystemIcons.Application;
    }

    void Quit()
    {
        tick.Stop();
        presence.Dispose();
        tray.Visible = false;
        foreach (var o in overlays) o.Close();
        ExitThread();
    }
}

class Overlay : Form
{
    const int WS_EX_TOPMOST = 0x8, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80;
    const int WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x8000000;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    public double TargetOpacity;      // app đặt qua Cue(); luôn < 1 để giữ cửa sổ layered
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    public readonly Rectangle Area;
    public readonly float Fill;       // tỉ lệ vùng làm việc mà overlay chiếm (chọn trong menu khay)
    readonly float u;                 // 1% chiều cao overlay, mọi kích thước vẽ tính theo đơn vị này
    readonly System.Windows.Forms.Timer anim;
    readonly Stopwatch animClock = Stopwatch.StartNew();
    readonly Font titleFont, subFont, countFont;
    readonly StringFormat centered = new StringFormat { Alignment = StringAlignment.Center };
    readonly Rectangle eyeBox;
    readonly Color accent = Color.FromArgb(120, 200, 255);

    string title = "", sub = "";
    Mode mode; float dx, dy; int remain; double progress;
    long cueStartMs;
    bool fadingOut;

    public Overlay(Rectangle workingArea, float fill)
    {
        Area = workingArea;
        Fill = fill;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(22, 26, 34);
        DoubleBuffered = true;
        Opacity = 0;

        // Kích thước lấy từ pixel thật của màn hình (process per-monitor DPI) nên tự đúng ở mọi mức scale.
        int w = (int)(Area.Width * Fill), h = (int)(Area.Height * Fill);
        Bounds = new Rectangle(Area.Left + (Area.Width - w) / 2, Area.Top + (Area.Height - h) / 2, w, h);
        u = h / 100f;
        titleFont = new Font("Segoe UI Semibold", 7f * u, GraphicsUnit.Pixel);
        subFont = new Font("Segoe UI", 3.4f * u, GraphicsUnit.Pixel);
        countFont = new Font("Segoe UI Semibold", 6f * u, GraphicsUnit.Pixel);
        var eyeC = EyeCenter();
        eyeBox = Rectangle.Round(new RectangleF(eyeC.X - 24 * u, eyeC.Y - 18 * u, 48 * u, 36 * u));
        using (var path = RoundRect(new RectangleF(0, 0, w, h), 3 * u))
            Region = new Region(path);

        anim = new System.Windows.Forms.Timer { Interval = 33 };
        anim.Tick += delegate { Animate(); };
    }

    PointF EyeCenter() { return new PointF(ClientSize.Width / 2f, 34 * u); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            anim.Dispose(); titleFont.Dispose(); subFont.Dispose(); countFont.Dispose(); centered.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void SetCue(string t, string sb, Mode m, float x, float y, int rem, double prog)
    {
        if (t != title || m != mode) cueStartMs = animClock.ElapsedMilliseconds;
        title = t; sub = sb; mode = m; dx = x; dy = y; remain = rem; progress = prog;
        Invalidate();
    }

    public void ShowCue()
    {
        fadingOut = false;
        if (!Visible) { Opacity = 0; Show(); }
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        anim.Start();
    }

    public void HideCue()
    {
        if (Visible) fadingOut = true;
    }

    void Animate()
    {
        if (fadingOut)
        {
            double o = Opacity - 0.06;
            if (o <= 0) { Opacity = 0; Hide(); anim.Stop(); fadingOut = false; return; }
            Opacity = o;
        }
        else if (Math.Abs(Opacity - TargetOpacity) > 0.01)
        {
            Opacity = Opacity < TargetOpacity ? Math.Min(TargetOpacity, Opacity + 0.06)
                                              : Math.Max(TargetOpacity, Opacity - 0.03);
        }
        Invalidate(eyeBox);   // chỉ vẽ lại vùng con mắt cho nhẹ CPU; chữ vẽ lại qua SetCue
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        float t = (animClock.ElapsedMilliseconds - cueStartMs) / 1000f;

        DrawEye(g, EyeCenter(), t);
        if (!e.ClipRectangle.IntersectsWith(new Rectangle(0, eyeBox.Bottom, ClientSize.Width, ClientSize.Height))) return;

        float W = ClientSize.Width, cx = W / 2f;
        using (var white = new SolidBrush(Color.FromArgb(240, 244, 250)))
        using (var grey = new SolidBrush(Color.FromArgb(170, 180, 195)))
        using (var acc = new SolidBrush(accent))
        {
            g.DrawString(title, titleFont, white, cx, 55 * u, centered);
            g.DrawString(sub, subFont, grey, cx, 66 * u, centered);
            string cnt = remain >= 60 ? string.Format("{0}:{1:00}", remain / 60, remain % 60) : remain.ToString();
            g.DrawString(cnt, countFont, acc, cx, 73 * u, centered);

            float barW = W * 0.4f, barX = cx - barW / 2f;
            using (var track = new SolidBrush(Color.FromArgb(50, 58, 72)))
                g.FillRectangle(track, barX, 86 * u, barW, 0.8f * u);
            g.FillRectangle(acc, barX, 86 * u, (float)(barW * Math.Min(1, progress)), 0.8f * u);
        }
    }

    void DrawEye(Graphics g, PointF c, float t)
    {
        float w = 16 * u, h = 9.5f * u, irisR = 5.2f * u, maxOff = 6 * u;
        using (var outline = new Pen(accent, 0.6f * u))
        {
            bool closed = mode == Mode.Closed || (mode == Mode.Blink && (t % 0.8f) > 0.55f);
            if (closed)
            {
                g.DrawArc(outline, c.X - w, c.Y - h * 1.2f, w * 2, h * 2, 30, 120);
                return;
            }
            using (var eye = new GraphicsPath())
            {
                eye.AddBezier(c.X - w, c.Y, c.X - w / 2, c.Y - h * 1.3f, c.X + w / 2, c.Y - h * 1.3f, c.X + w, c.Y);
                eye.AddBezier(c.X + w, c.Y, c.X + w / 2, c.Y + h * 1.3f, c.X - w / 2, c.Y + h * 1.3f, c.X - w, c.Y);
                using (var fill = new SolidBrush(Color.FromArgb(225, 232, 242))) g.FillPath(fill, eye);

                float ox = 0, oy = 0;
                if (mode == Mode.Look)
                {
                    float k = Math.Min(1f, t / 0.6f);
                    k = 1 - (1 - k) * (1 - k);
                    ox = dx * maxOff * k; oy = dy * maxOff * 0.55f * k;
                }
                else if (mode == Mode.Circle)
                {
                    double a = t * 2 * Math.PI / 3.0;
                    ox = (float)Math.Cos(a) * maxOff * 0.8f; oy = (float)Math.Sin(a) * maxOff * 0.45f;
                }
                var clip = g.Clip;
                g.SetClip(eye, CombineMode.Intersect);
                using (var iris = new SolidBrush(Color.FromArgb(40, 110, 170)))
                    g.FillEllipse(iris, c.X + ox - irisR, c.Y + oy - irisR, irisR * 2, irisR * 2);
                using (var pupil = new SolidBrush(Color.FromArgb(15, 20, 30)))
                    g.FillEllipse(pupil, c.X + ox - irisR / 2, c.Y + oy - irisR / 2, irisR, irisR);
                g.Clip = clip;
                g.DrawPath(outline, eye);
            }

            if (mode == Mode.Far)
            {
                float p = t % 2f / 2f;
                int alpha = (int)(160 * (1 - p));
                using (var ring = new Pen(Color.FromArgb(alpha, accent), 0.35f * u))
                {
                    float r = w + 1.2f * u + p * 5f * u;
                    g.DrawEllipse(ring, c.X - r, c.Y - r * 0.75f, r * 2, r * 1.5f);
                }
            }
        }
    }

    static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
