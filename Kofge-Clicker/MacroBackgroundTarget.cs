using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal sealed class MacroBackgroundTarget
{
    private readonly IntPtr _root;
    private readonly uint _threadId;
    private readonly uint _processId;
    private IntPtr _lastMouseTarget;

    internal MacroBackgroundTarget(IntPtr root)
    {
        _root = root;
        _threadId = NativeMethods.GetWindowThreadProcessId(root, out _processId);
        Validate();
    }

    internal void Validate(bool requireReady = true)
    {
        var threadId = NativeMethods.GetWindowThreadProcessId(_root, out var processId);
        if (_threadId == 0 || threadId != _threadId || processId != _processId ||
            !NativeMethods.IsWindow(_root) ||
            (requireReady && (!NativeMethods.IsWindowVisible(_root) || NativeMethods.IsIconic(_root))))
        {
            throw new Win32Exception("The background target window is closed, hidden or minimized.");
        }
    }

    internal bool Contains(IntPtr window)
    {
        return window != IntPtr.Zero && NativeMethods.IsWindow(window) &&
            (window == _root || NativeMethods.IsChild(_root, window));
    }

    internal void RememberMouseTarget(IntPtr window)
    {
        if (Contains(window)) _lastMouseTarget = window;
    }

    internal IntPtr ResolveKeyboardWindow()
    {
        Validate();
        // A background click need not change Windows keyboard focus. Keep its
        // destination locally instead of consulting the foreground application.
        if (Contains(_lastMouseTarget)) return _lastMouseTarget;

        var info = new NativeMethods.GuiThreadInfo
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
        };
        return NativeMethods.GetGUIThreadInfo(_threadId, ref info) && Contains(info.HwndFocus)
            ? info.HwndFocus
            : _root;
    }

    internal static bool SupportsButtonClick(IntPtr window)
    {
        var name = NativeMethods.GetWindowClass(window);
        if (!name.Equals("Button", StringComparison.OrdinalIgnoreCase) &&
            !name.StartsWith("WindowsForms10.BUTTON.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Group boxes are also native BUTTON windows, but are not clickable.
        var style = (long)NativeMethods.GetWindowLongPtr(window, -16) & 0xF;
        return style is 0 or 1 or 2 or 3 or 4 or 5 or 6 or 9 or 11;
    }

    internal static bool SupportsEditCaret(IntPtr window)
    {
        var name = NativeMethods.GetWindowClass(window);
        // RichEdit uses a different, pointer-based EM_CHARFROMPOS contract.
        return name.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("WindowsForms10.EDIT.", StringComparison.OrdinalIgnoreCase);
    }

    internal static void SetEditCaret(IntPtr window, NativeMethods.Point point)
    {
        var packedPoint = (nint)((uint)(ushort)point.X | ((uint)(ushort)point.Y << 16));
        if (!NativeMethods.TrySendMessageWithResult(window, NativeMethods.EmCharFromPos,
                0, packedPoint, out var result) || unchecked((uint)(long)result) == uint.MaxValue)
        {
            throw new Win32Exception("Unable to locate the background text insertion point.");
        }

        var index = (nuint)((long)result & 0xFFFF);
        if (!NativeMethods.TrySendMessage(window, NativeMethods.EmSetSel, index, (nint)index))
        {
            throw new Win32Exception("Unable to set the background text insertion point.");
        }
    }
}
