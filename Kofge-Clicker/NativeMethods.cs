using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KofgeClicker;

internal static class NativeMethods
{
    internal const int WhKeyboardLl = 13;
    internal const int WhMouseLl = 14;

    internal const int WmKeyDown = 0x0100;
    internal const int WmKeyUp = 0x0101;
    internal const int WmSysKeyDown = 0x0104;
    internal const int WmSysKeyUp = 0x0105;
    internal const int WmLButtonDown = 0x0201;
    internal const int WmLButtonUp = 0x0202;
    internal const int WmRButtonDown = 0x0204;
    internal const int WmRButtonUp = 0x0205;
    internal const int WmMButtonDown = 0x0207;
    internal const int WmMButtonUp = 0x0208;
    internal const int WmXButtonDown = 0x020B;
    internal const int WmXButtonUp = 0x020C;
    internal const int WmSetRedraw = 0x000B;
    private const int WmGetIcon = 0x007F;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int IconSmall2 = 2;
    private const int GclpHIcon = -14;
    private const int GclpHIconSm = -34;
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint SmtoAbortIfHung = 0x0002;

    internal const int LlkhfInjected = 0x10;
    internal const int LlmhfInjected = 0x00000001;

    internal const uint MouseeventfLeftDown = 0x0002;
    internal const uint MouseeventfLeftUp = 0x0004;
    internal const uint MouseeventfRightDown = 0x0008;
    internal const uint MouseeventfRightUp = 0x0010;
    internal const uint MouseeventfMiddleDown = 0x0020;
    internal const uint MouseeventfMiddleUp = 0x0040;
    internal const uint MouseeventfXDown = 0x0080;
    internal const uint MouseeventfXUp = 0x0100;
    internal const uint XButton1MouseData = 0x0001;
    internal const uint XButton2MouseData = 0x0002;

    internal const int VkLButton = 0x01;
    internal const int VkRButton = 0x02;
    internal const int VkCancel = 0x03;
    internal const int VkMButton = 0x04;
    internal const int VkXButton1 = 0x05;
    internal const int VkXButton2 = 0x06;
    internal const int VkBack = 0x08;
    internal const int VkTab = 0x09;
    internal const int VkReturn = 0x0D;
    internal const int VkShift = 0x10;
    internal const int VkControl = 0x11;
    internal const int VkMenu = 0x12;
    internal const int VkPause = 0x13;
    internal const int VkCapsLock = 0x14;
    internal const int VkEscape = 0x1B;
    internal const int VkSpace = 0x20;
    internal const int VkPageUp = 0x21;
    internal const int VkPageDown = 0x22;
    internal const int VkEnd = 0x23;
    internal const int VkHome = 0x24;
    internal const int VkLeft = 0x25;
    internal const int VkUp = 0x26;
    internal const int VkRight = 0x27;
    internal const int VkDown = 0x28;
    internal const int VkPrintScreen = 0x2C;
    internal const int VkInsert = 0x2D;
    internal const int VkDelete = 0x2E;
    internal const int Vk0 = 0x30;
    internal const int Vk9 = 0x39;
    internal const int VkA = 0x41;
    internal const int VkZ = 0x5A;
    internal const int VkLWin = 0x5B;
    internal const int VkRWin = 0x5C;
    internal const int VkApps = 0x5D;
    internal const int VkNumpad0 = 0x60;
    internal const int VkNumpad9 = 0x69;
    internal const int VkF1 = 0x70;
    internal const int VkF24 = 0x87;
    internal const int VkNumLock = 0x90;
    internal const int VkScroll = 0x91;
    internal const int VkOemSemicolon = 0xBA;
    internal const int VkOemPlus = 0xBB;
    internal const int VkOemComma = 0xBC;
    internal const int VkOemMinus = 0xBD;
    internal const int VkOemPeriod = 0xBE;
    internal const int VkOemQuestion = 0xBF;
    internal const int VkOemTilde = 0xC0;
    internal const int VkOemOpenBrackets = 0xDB;
    internal const int VkOemPipe = 0xDC;
    internal const int VkOemCloseBrackets = 0xDD;
    internal const int VkOemQuotes = 0xDE;
    internal const int VkOem8 = 0xDF;
    internal const int VkOemBackslash = 0xE2;
    internal const int GwlExstyle = -20;
    internal const int WmNclbuttonDown = 0x00A1;
    internal const int WmNclbuttonUp = 0x00A2;
    internal const int WmSyscommand = 0x0112;
    internal const int ScMinimize = 0xF020;
    internal const int ScRestore = 0xF120;
    internal const int ScClose = 0xF060;
    internal const int HtMinButton = 8;
    internal const int SwShow = 5;
    internal const int SwRestore = 9;
    internal const uint WsExAppwindow = 0x00040000;
    internal const uint WsExToolwindow = 0x00000080;
    internal const uint WsExLayered = 0x00080000;
    internal const uint SwpNosize = 0x0001;
    internal const uint SwpNomove = 0x0002;
    internal const uint SwpNozorder = 0x0004;
    internal const uint SwpFramechanged = 0x0020;
    internal const uint SwpNoactivate = 0x0010;
    internal const uint RdwInvalidate = 0x0001;
    internal const uint RdwErase = 0x0004;
    internal const uint RdwAllChildren = 0x0080;
    internal const uint RdwUpdateNow = 0x0100;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRoundSmall = 3;
    internal static readonly IntPtr HwndTopmost = new(-1);
    internal static readonly IntPtr HwndNotopmost = new(-2);
    internal static readonly nuint KofgeClickerExtraInfo = unchecked((nuint)0xAC10C11C);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Kbdllhookstruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Msllhookstruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WindowInfo
    {
        public int Size;
        public RECT Window;
        public RECT Client;
        public uint Style;
        public uint ExStyle;
        public uint WindowStatus;
        public uint CxWindowBorders;
        public uint CyWindowBorders;
        public ushort AtomWindowType;
        public ushort CreatorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder returnedString, int size, string filePath);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool WritePrivateProfileString(string section, string? key, string? value, string filePath);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr processHandle, uint flags, StringBuilder exeName, ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    internal static extern nint DefWindowProc(IntPtr hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, ref Input pInputs, int cbSize);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    internal static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    internal static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    internal static extern nint SendMessage(IntPtr hWnd, int msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        nint wParam,
        nint lParam,
        uint flags,
        uint timeout,
        out nint result);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW", SetLastError = true)]
    private static extern nint GetClassLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RedrawWindow(IntPtr hWnd, IntPtr updateRect, IntPtr updateRegion, uint flags);

    [DllImport("kernel32.dll")]
    internal static extern bool QueryPerformanceFrequency(out long lpFrequency);

    [DllImport("kernel32.dll")]
    internal static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    internal static extern uint TimeBeginPeriod(uint period);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    internal static extern uint TimeEndPeriod(uint period);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    internal static bool TryEnableSmallRoundedCorners(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return false;
        }

        try
        {
            var preference = DwmwcpRoundSmall;
            return DwmSetWindowAttribute(
                hwnd,
                DwmwaWindowCornerPreference,
                ref preference,
                Marshal.SizeOf<int>()) >= 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    internal static string GetWindowTitle(IntPtr hwnd)
    {
        var builder = new StringBuilder(512);
        _ = GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString().Trim();
    }

    internal static string GetWindowClass(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        _ = GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString().Trim();
    }

    internal static string GetWindowProcessName(IntPtr hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName + ".exe";
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static (string DisplayName, Image? Icon) GetWindowProcessPresentation(IntPtr hwnd, string fallbackExe)
    {
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return (fallbackExe, null);
        }

        var fallbackName = Path.GetFileNameWithoutExtension(fallbackExe).Trim();
        var displayName = fallbackName.Length > 0 ? fallbackName : fallbackExe;
        Image? icon = null;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            if (process.ProcessName.Trim().Length > 0)
            {
                displayName = process.ProcessName;
            }
        }
        catch
        {
        }

        if (TryGetProcessExecutablePath(pid, out var executablePath))
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                var description = versionInfo.FileDescription?.Trim() ?? string.Empty;
                var productName = versionInfo.ProductName?.Trim() ?? string.Empty;
                displayName = description.Length > 0
                    ? description
                    : productName.Length > 0
                        ? productName
                        : Path.GetFileNameWithoutExtension(executablePath);

                using var associatedIcon = Icon.ExtractAssociatedIcon(executablePath);
                icon = associatedIcon?.ToBitmap();
            }
            catch
            {
            }
        }

        icon ??= TryGetWindowIcon(hwnd);
        return (displayName.Length > 0 ? displayName : fallbackExe, icon);
    }

    private static bool TryGetProcessExecutablePath(uint processId, out string executablePath)
    {
        executablePath = string.Empty;
        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var capacity = 1024u;
            var path = new StringBuilder((int)capacity);
            if (!QueryFullProcessImageName(processHandle, 0, path, ref capacity))
            {
                return false;
            }

            executablePath = path.ToString().Trim();
            return executablePath.Length > 0;
        }
        finally
        {
            _ = CloseHandle(processHandle);
        }
    }

    private static Image? TryGetWindowIcon(IntPtr hwnd)
    {
        var iconHandle = TryGetWindowIconHandle(hwnd, IconSmall2);
        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = TryGetWindowIconHandle(hwnd, IconSmall);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = TryGetWindowIconHandle(hwnd, IconBig);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = GetClassLongPtr(hwnd, GclpHIconSm);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = GetClassLongPtr(hwnd, GclpHIcon);
        }

        if (iconHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var source = Icon.FromHandle(iconHandle);
            using var clone = (Icon)source.Clone();
            return clone.ToBitmap();
        }
        catch
        {
            return null;
        }
    }

    private static nint TryGetWindowIconHandle(IntPtr hwnd, int iconType)
    {
        return SendMessageTimeout(
                hwnd,
                WmGetIcon,
                iconType,
                0,
                SmtoAbortIfHung,
                75,
                out var result) != 0
            ? result
            : 0;
    }

    internal static bool IsPressed(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }
}
