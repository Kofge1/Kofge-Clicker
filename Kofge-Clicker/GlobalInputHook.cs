using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace KofgeClicker;

public sealed class GlobalInputEventArgs : EventArgs
{
    public required string Token { get; init; }
    public required bool IsDown { get; init; }
    public required bool IsInjected { get; init; }
    public required bool IsSelfGenerated { get; init; }
    public required bool WasAlreadyDown { get; init; }
    public required bool Ctrl { get; init; }
    public required bool Shift { get; init; }
    public required bool Alt { get; init; }
}

internal enum ObservedInputKind
{
    Key,
    MouseButton,
    MouseMove,
    MouseWheel,
    MouseHorizontalWheel
}

internal readonly record struct ObservedInputEvent(
    ObservedInputKind Kind,
    string Token,
    bool IsDown,
    int X,
    int Y,
    int WheelDelta,
    uint VirtualKey,
    uint ScanCode,
    uint Flags,
    uint MessageTime,
    bool IsInjected,
    string Text = "");

public sealed class GlobalInputHook : IDisposable
{
    private readonly NativeMethods.HookProc _keyboardProc;
    private readonly NativeMethods.HookProc _mouseProc;
    private readonly ConcurrentDictionary<string, byte> _downTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly byte[] _translationKeyboardState = new byte[256];
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private bool _disposed;

    public event EventHandler<GlobalInputEventArgs>? InputChanged;
    public event Action<string, uint, int, int>? MouseDownObserved;
    internal event Action<ObservedInputEvent>? InputObserved;
    public Func<string, bool, bool, bool, bool, bool>? ShouldSuppressMouseInput { get; set; }

    public GlobalInputHook()
    {
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero)
        {
            return;
        }

        UninstallHooks();
        Array.Clear(_translationKeyboardState);
        _ = NativeMethods.GetKeyboardState(_translationKeyboardState);
        var module = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _keyboardProc, module, 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            throw new GlobalInputHookException("keyboard", Marshal.GetLastWin32Error());
        }

        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _mouseProc, module, 0);
        if (_mouseHook == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            UninstallHooks();
            throw new GlobalInputHookException("mouse", error);
        }
    }

    public bool IsTokenDown(string token)
    {
        var normalizedToken = HotkeyHelper.NormalizePrimaryToken(token);
        return _downTokens.ContainsKey(normalizedToken);
    }

    public void ClearTokenDownState(string token)
    {
        var normalizedToken = HotkeyHelper.NormalizePrimaryToken(token);
        _downTokens.TryRemove(normalizedToken, out _);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.Kbdllhookstruct>(lParam);
            var message = unchecked((int)wParam);
            var isDown = message is NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown;
            var isUp = message is NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp;
            if (isDown || isUp)
            {
                var isSelfGenerated = hookStruct.DwExtraInfo == NativeMethods.KofgeClickerExtraInfo;
                if (isSelfGenerated)
                {
                    return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
                }

                var token = HotkeyHelper.FromVirtualKey((int)hookStruct.VkCode);
                if (HotkeyHelper.IsModifierToken(token))
                {
                    token = token switch
                    {
                        "ControlKey" => "Ctrl",
                        "ShiftKey" => "Shift",
                        "Menu" => "Alt",
                        _ => token
                    };
                }

                UpdateTranslationKeyboardState(hookStruct.VkCode, isDown);

                InputObserved?.Invoke(new ObservedInputEvent(
                    ObservedInputKind.Key,
                    token,
                    isDown,
                    0,
                    0,
                    0,
                    hookStruct.VkCode,
                    hookStruct.ScanCode,
                    hookStruct.Flags,
                    hookStruct.Time,
                    (hookStruct.Flags & NativeMethods.LlkhfInjected) != 0,
                    isDown ? TranslateKeyToText(hookStruct) : string.Empty));

                Publish(
                    token,
                    isDown,
                    (hookStruct.Flags & NativeMethods.LlkhfInjected) != 0,
                    false);
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private string TranslateKeyToText(NativeMethods.Kbdllhookstruct key)
    {
        if (IsPressed(_translationKeyboardState, NativeMethods.VkControl) ||
            IsPressed(_translationKeyboardState, NativeMethods.VkMenu) ||
            IsPressed(_translationKeyboardState, NativeMethods.VkLWin) ||
            IsPressed(_translationKeyboardState, NativeMethods.VkRWin))
        {
            return string.Empty;
        }

        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var threadId = foregroundWindow == IntPtr.Zero
            ? 0
            : NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
        var keyboardLayout = NativeMethods.GetKeyboardLayout(threadId);
        var buffer = new StringBuilder(8);
        const uint doNotChangeKeyboardState = 0x04;
        var characterCount = NativeMethods.ToUnicodeEx(
            key.VkCode,
            key.ScanCode,
            _translationKeyboardState,
            buffer,
            buffer.Capacity,
            doNotChangeKeyboardState,
            keyboardLayout);
        if (characterCount <= 0)
        {
            return string.Empty;
        }

        var text = buffer.ToString(0, Math.Min(characterCount, buffer.Length));
        return text.Any(char.IsControl) ? string.Empty : text;
    }

    private void UpdateTranslationKeyboardState(uint virtualKey, bool isDown)
    {
        if (virtualKey >= _translationKeyboardState.Length)
        {
            return;
        }

        var keyIndex = (int)virtualKey;
        var wasDown = (_translationKeyboardState[keyIndex] & 0x80) != 0;
        if (isDown)
        {
            _translationKeyboardState[keyIndex] |= 0x80;
        }
        else
        {
            _translationKeyboardState[keyIndex] &= 0x7F;
        }

        if (isDown && !wasDown && virtualKey is NativeMethods.VkCapsLock or NativeMethods.VkNumLock or NativeMethods.VkScroll)
        {
            _translationKeyboardState[keyIndex] ^= 0x01;
        }

        var genericModifier = virtualKey switch
        {
            0xA0 or 0xA1 => NativeMethods.VkShift,
            0xA2 or 0xA3 => NativeMethods.VkControl,
            0xA4 or 0xA5 => NativeMethods.VkMenu,
            _ => 0
        };
        if (genericModifier != 0)
        {
            var leftKey = genericModifier switch
            {
                NativeMethods.VkShift => 0xA0,
                NativeMethods.VkControl => 0xA2,
                _ => 0xA4
            };
            var rightKey = leftKey + 1;
            var modifierDown =
                (_translationKeyboardState[leftKey] & 0x80) != 0 ||
                (_translationKeyboardState[rightKey] & 0x80) != 0;
            if (modifierDown)
            {
                _translationKeyboardState[genericModifier] |= 0x80;
            }
            else
            {
                _translationKeyboardState[genericModifier] &= 0x7F;
            }
        }
    }

    private static bool IsPressed(byte[] keyboardState, int virtualKey)
    {
        return (keyboardState[virtualKey] & 0x80) != 0;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.Msllhookstruct>(lParam);
            var message = unchecked((int)wParam);
            var isInjected = (hookStruct.Flags & NativeMethods.LlmhfInjected) != 0;
            var isSelfGenerated = hookStruct.DwExtraInfo == NativeMethods.KofgeClickerExtraInfo;
            if (isSelfGenerated)
            {
                return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
            }

            if (message == NativeMethods.WmMouseMove)
            {
                InputObserved?.Invoke(new ObservedInputEvent(
                    ObservedInputKind.MouseMove,
                    string.Empty,
                    false,
                    hookStruct.Pt.X,
                    hookStruct.Pt.Y,
                    0,
                    0,
                    0,
                    hookStruct.Flags,
                    hookStruct.Time,
                    isInjected));
            }
            else if (message is NativeMethods.WmMouseWheel or NativeMethods.WmMouseHWheel)
            {
                var wheelDelta = unchecked((short)((hookStruct.MouseData >> 16) & 0xFFFF));
                InputObserved?.Invoke(new ObservedInputEvent(
                    message == NativeMethods.WmMouseWheel
                        ? ObservedInputKind.MouseWheel
                        : ObservedInputKind.MouseHorizontalWheel,
                    string.Empty,
                    false,
                    hookStruct.Pt.X,
                    hookStruct.Pt.Y,
                    wheelDelta,
                    0,
                    0,
                    hookStruct.Flags,
                    hookStruct.Time,
                    isInjected));
            }

            string? token = message switch
            {
                NativeMethods.WmLButtonDown or NativeMethods.WmLButtonUp => "LButton",
                NativeMethods.WmRButtonDown or NativeMethods.WmRButtonUp => "RButton",
                NativeMethods.WmMButtonDown or NativeMethods.WmMButtonUp => "MButton",
                NativeMethods.WmXButtonDown or NativeMethods.WmXButtonUp => ((hookStruct.MouseData >> 16) & 0xFFFF) == 1 ? "XButton1" : "XButton2",
                _ => null
            };

            if (token is not null)
            {
                var isDown = message is NativeMethods.WmLButtonDown or NativeMethods.WmRButtonDown or NativeMethods.WmMButtonDown or NativeMethods.WmXButtonDown;
                if (isDown)
                {
                    MouseDownObserved?.Invoke(token, hookStruct.Time, hookStruct.Pt.X, hookStruct.Pt.Y);
                }

                InputObserved?.Invoke(new ObservedInputEvent(
                    ObservedInputKind.MouseButton,
                    token,
                    isDown,
                    hookStruct.Pt.X,
                    hookStruct.Pt.Y,
                    0,
                    0,
                    0,
                    hookStruct.Flags,
                    hookStruct.Time,
                    isInjected));

                var ctrl = NativeMethods.IsPressed(NativeMethods.VkControl);
                var shift = NativeMethods.IsPressed(NativeMethods.VkShift);
                var alt = NativeMethods.IsPressed(NativeMethods.VkMenu);
                Publish(
                    token,
                    isDown,
                    isInjected,
                    false);

                if (!isInjected &&
                    ShouldSuppressMouseInput?.Invoke(token, isDown, ctrl, shift, alt) == true)
                {
                    return (IntPtr)1;
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void Publish(string token, bool isDown, bool injected, bool selfGenerated)
    {
        var normalizedToken = token switch
        {
            "ControlKey" or "LControlKey" or "RControlKey" => "Ctrl",
            "ShiftKey" or "LShiftKey" or "RShiftKey" => "Shift",
            "Menu" or "LMenu" or "RMenu" => "Alt",
            _ => HotkeyHelper.NormalizePrimaryToken(token)
        };

        var wasAlreadyDown = _downTokens.ContainsKey(normalizedToken);
        if (!selfGenerated)
        {
            if (isDown)
            {
                _downTokens.TryAdd(normalizedToken, 0);
            }
            else
            {
                _downTokens.TryRemove(normalizedToken, out _);
            }
        }

        InputChanged?.Invoke(this, new GlobalInputEventArgs
        {
            Token = normalizedToken,
            IsDown = isDown,
            IsInjected = injected,
            IsSelfGenerated = selfGenerated,
            WasAlreadyDown = wasAlreadyDown,
            Ctrl = NativeMethods.IsPressed(NativeMethods.VkControl),
            Shift = NativeMethods.IsPressed(NativeMethods.VkShift),
            Alt = NativeMethods.IsPressed(NativeMethods.VkMenu)
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UninstallHooks();
        _disposed = true;
    }

    private void UninstallHooks()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }
}

internal sealed class GlobalInputHookException : Win32Exception
{
    internal GlobalInputHookException(string hookName, int errorCode)
        : base(errorCode, $"Unable to install the global {hookName} input hook.")
    {
    }
}
