using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal enum MacroPlaybackStatus
{
    Completed,
    Cancelled,
    Failed
}

internal readonly record struct MacroPlaybackResult(
    MacroPlaybackStatus Status,
    string? ErrorMessage = null);

internal readonly record struct MacroPlayerSnapshot(
    bool IsPlaying,
    long ElapsedMicroseconds,
    int CompletedEventCount,
    int EventCount,
    int CurrentRepeat,
    int RepeatCount,
    bool RepeatIndefinitely);

internal sealed class MacroPlayer : IDisposable
{
    // Frame-polled applications can miss a press that lasts less than one typical
    // 60 Hz frame, even though SendInput accepted both state changes.
    private const long MinimumKeyboardPressDurationMicroseconds = 16_000;
    private const long MinimumMousePressDurationMicroseconds = 20_000;
    private const int FinalInputProcessingDelayMilliseconds = 75;

    private readonly record struct InputTransitionState(long Timestamp, bool IsDown);

    private enum KeyboardPlaybackTransport
    {
        SendInput,
        WindowMessage
    }

    private readonly record struct PressedKeyState(
        MacroEvent SourceEvent,
        KeyboardPlaybackTransport Transport,
        IntPtr TargetWindow);

    private readonly record struct PressedMouseState(
        bool Background,
        IntPtr TargetWindow,
        NativeMethods.Point ClientPoint);

    private readonly object _stateSync = new();
    private readonly object _sendSync = new();
    private readonly Dictionary<string, PressedKeyState> _pressedKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PressedMouseState> _pressedMouseButtons = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _playbackCts;
    private Task<MacroPlaybackResult>? _playbackTask;
    private long _startedAt;
    private long _finishedElapsedMicroseconds;
    private int _completedEventCount;
    private int _eventCount;
    private int _currentRepeat;
    private int _repeatCount;
    private bool _repeatIndefinitely;
    private bool _firstKeyboardEventLogged;
    private volatile bool _isPlaying;
    private bool _disposed;

    internal bool IsPlaying => _isPlaying;

    internal Task<MacroPlaybackResult> PlayAsync(
        MacroDefinition macro,
        IntPtr backgroundMouseTarget = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_stateSync)
        {
            if (_isPlaying || (_playbackTask is not null && !_playbackTask.IsCompleted))
            {
                throw new InvalidOperationException("A macro is already playing.");
            }

            var events = macro.Events.ToArray();
            var cancellation = new CancellationTokenSource();
            _playbackCts = cancellation;
            _startedAt = Stopwatch.GetTimestamp();
            _finishedElapsedMicroseconds = 0;
            _completedEventCount = 0;
            _eventCount = events.Length;
            _currentRepeat = 1;
            _repeatCount = Math.Clamp(macro.RepeatCount, 1, MacroDefinition.MaximumRepeatCount);
            _repeatIndefinitely = macro.RepeatIndefinitely;
            _firstKeyboardEventLogged = false;
            _isPlaying = true;
            _playbackTask = Task.Run(
                () => PlayCore(
                    macro,
                    events,
                    _repeatCount,
                    Math.Clamp(
                        macro.RepeatDelayMilliseconds,
                        0,
                        MacroDefinition.MaximumRepeatDelayMilliseconds),
                    _repeatIndefinitely,
                    backgroundMouseTarget,
                    cancellation),
                CancellationToken.None);
            return _playbackTask;
        }
    }

    internal MacroPlayerSnapshot GetSnapshot()
    {
        var elapsed = _isPlaying
            ? GetElapsedMicroseconds(_startedAt)
            : Interlocked.Read(ref _finishedElapsedMicroseconds);
        return new MacroPlayerSnapshot(
            _isPlaying,
            elapsed,
            Volatile.Read(ref _completedEventCount),
            Volatile.Read(ref _eventCount),
            Volatile.Read(ref _currentRepeat),
            Volatile.Read(ref _repeatCount),
            _repeatIndefinitely);
    }

    internal void Stop()
    {
        lock (_stateSync)
        {
            if (!_isPlaying)
            {
                return;
            }

            _playbackCts?.Cancel();
        }

        // Wait for an in-flight send, then release everything before returning to the caller.
        lock (_sendSync)
        {
            ReleasePressedInputs();
        }
    }

    private MacroPlaybackResult PlayCore(
        MacroDefinition macro,
        IReadOnlyList<MacroEvent> events,
        int repeatCount,
        int repeatDelayMilliseconds,
        bool repeatIndefinitely,
        IntPtr backgroundMouseTarget,
        CancellationTokenSource cancellation)
    {
        var timerResolutionEnabled = NativeMethods.TimeBeginPeriod(1) == 0;
        try
        {
            var inputTransitions = new Dictionary<string, InputTransitionState>(
                StringComparer.OrdinalIgnoreCase);
            for (var repeat = 1;
                 repeatIndefinitely || repeat <= repeatCount;
                 repeat = repeat == int.MaxValue ? 1 : repeat + 1)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                Volatile.Write(ref _currentRepeat, repeat);
                Volatile.Write(ref _completedEventCount, 0);
                var repeatStartedAt = Stopwatch.GetTimestamp();
                for (var index = 0; index < events.Count; index++)
                {
                    var macroEvent = events[index];
                    var targetTimestamp = AddMicroseconds(
                        repeatStartedAt,
                        macroEvent.OffsetMicroseconds);
                    var transitionId = GetInputTransitionId(macroEvent);
                    if (transitionId is not null &&
                        !macroEvent.IsDown &&
                        inputTransitions.TryGetValue(transitionId, out var previousTransition) &&
                        previousTransition.IsDown)
                    {
                        targetTimestamp = Math.Max(
                            targetTimestamp,
                            AddMicroseconds(
                                previousTransition.Timestamp,
                                GetMinimumPressDuration(macroEvent)));
                    }

                    WaitUntil(targetTimestamp, cancellation.Token);

                    lock (_sendSync)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        SendEvent(macro, macroEvent, backgroundMouseTarget);
                        if (transitionId is not null)
                        {
                            inputTransitions[transitionId] = new InputTransitionState(
                                Stopwatch.GetTimestamp(),
                                macroEvent.IsDown);
                        }

                        Volatile.Write(ref _completedEventCount, index + 1);
                    }
                }

                lock (_sendSync)
                {
                    ReleasePressedInputs();
                }

                inputTransitions.Clear();

                if (!repeatIndefinitely && repeat >= repeatCount)
                {
                    if (events.Count > 0 &&
                        cancellation.Token.WaitHandle.WaitOne(FinalInputProcessingDelayMilliseconds))
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                    }

                    break;
                }

                if (repeatDelayMilliseconds > 0 &&
                    cancellation.Token.WaitHandle.WaitOne(repeatDelayMilliseconds))
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                }
            }

            return new MacroPlaybackResult(MacroPlaybackStatus.Completed);
        }
        catch (OperationCanceledException)
        {
            return new MacroPlaybackResult(MacroPlaybackStatus.Cancelled);
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroPlaybackFailed error={ex.GetType().Name}");
            return new MacroPlaybackResult(MacroPlaybackStatus.Failed, ex.Message);
        }
        finally
        {
            lock (_sendSync)
            {
                ReleasePressedInputs();
            }

            Interlocked.Exchange(ref _finishedElapsedMicroseconds, GetElapsedMicroseconds(_startedAt));
            _isPlaying = false;
            if (timerResolutionEnabled)
            {
                _ = NativeMethods.TimeEndPeriod(1);
            }

            lock (_stateSync)
            {
                if (ReferenceEquals(_playbackCts, cancellation))
                {
                    _playbackCts = null;
                }
            }

            cancellation.Dispose();
        }
    }

    private static void WaitUntil(long targetTimestamp, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var remainingTicks = targetTimestamp - Stopwatch.GetTimestamp();
            var remaining = (long)(remainingTicks * (1_000_000d / Stopwatch.Frequency));
            if (remaining <= 0)
            {
                return;
            }

            if (remaining > 3_000)
            {
                var waitMilliseconds = (int)Math.Min(int.MaxValue, Math.Max(1, remaining / 1_000 - 1));
                if (token.WaitHandle.WaitOne(waitMilliseconds))
                {
                    token.ThrowIfCancellationRequested();
                }
            }
            else
            {
                Thread.SpinWait(64);
            }
        }
    }

    private static long AddMicroseconds(long timestamp, long microseconds)
    {
        return timestamp + (long)Math.Ceiling(microseconds * (Stopwatch.Frequency / 1_000_000d));
    }

    private static string? GetInputTransitionId(MacroEvent macroEvent)
    {
        return macroEvent.Type switch
        {
            MacroEventType.Key => $"K:{GetKeyId(macroEvent)}",
            MacroEventType.MouseButton => $"M:{NormalizeMouseToken(macroEvent.Token)}",
            _ => null
        };
    }

    private static long GetMinimumPressDuration(MacroEvent macroEvent)
    {
        return macroEvent.Type == MacroEventType.MouseButton
            ? MinimumMousePressDurationMicroseconds
            : MinimumKeyboardPressDurationMicroseconds;
    }

    private void SendEvent(
        MacroDefinition macro,
        MacroEvent macroEvent,
        IntPtr backgroundMouseTarget)
    {
        var useBackgroundMouse = backgroundMouseTarget != IntPtr.Zero;
        switch (macroEvent.Type)
        {
            case MacroEventType.Key:
                SendKeyboardEvent(macroEvent);
                break;
            case MacroEventType.MouseButton:
                SendMouseButtonEvent(macro, macroEvent, backgroundMouseTarget);
                break;
            case MacroEventType.MouseMove:
                if (!useBackgroundMouse)
                {
                    SendMouseMove(macro, macroEvent.X, macroEvent.Y);
                }

                break;
            case MacroEventType.MouseWheel:
                if (useBackgroundMouse)
                {
                    SendBackgroundWheelEvent(backgroundMouseTarget, macro, macroEvent, horizontal: false);
                }
                else
                {
                    SendMouseActionAt(
                        macro,
                        macroEvent,
                        NativeMethods.MouseeventfWheel,
                        unchecked((uint)macroEvent.WheelDelta));
                }

                break;
            case MacroEventType.MouseHorizontalWheel:
                if (useBackgroundMouse)
                {
                    SendBackgroundWheelEvent(backgroundMouseTarget, macro, macroEvent, horizontal: true);
                }
                else
                {
                    SendMouseActionAt(
                        macro,
                        macroEvent,
                        NativeMethods.MouseeventfHWheel,
                        unchecked((uint)macroEvent.WheelDelta));
                }

                break;
        }
    }

    private void SendKeyboardEvent(MacroEvent macroEvent)
    {
        var keyId = GetKeyId(macroEvent);
        var wasPressed = _pressedKeys.TryGetValue(keyId, out var pressedState);
        if (macroEvent.IsDown && wasPressed && IsModifierKey(macroEvent.VirtualKey))
        {
            return;
        }

        var transport = wasPressed
            ? pressedState.Transport
            : ResolveKeyboardTransport(macroEvent, out pressedState);
        var targetWindow = pressedState.TargetWindow;

        if (transport == KeyboardPlaybackTransport.WindowMessage &&
            !TrySendKeyboardMessage(targetWindow, macroEvent, wasPressed))
        {
            transport = KeyboardPlaybackTransport.SendInput;
            targetWindow = IntPtr.Zero;
            SendKeyboardInput(macroEvent);
        }
        else if (transport == KeyboardPlaybackTransport.SendInput)
        {
            SendKeyboardInput(macroEvent);
        }

        if (!_firstKeyboardEventLogged)
        {
            _firstKeyboardEventLogged = true;
            InputDiagnostics.Write(
                $"MacroKeyboardPlayback mode={GetKeyboardTransportName(transport, macroEvent)} " +
                $"vk={macroEvent.VirtualKey} scan={macroEvent.ScanCode} target=0x{targetWindow.ToInt64():X}");
        }

        if (macroEvent.IsDown)
        {
            _pressedKeys[keyId] = new PressedKeyState(macroEvent, transport, targetWindow);
        }
        else
        {
            _pressedKeys.Remove(keyId);
        }
    }

    private static void SendKeyboardInput(MacroEvent macroEvent)
    {
        var useScanCode = macroEvent.VirtualKey == 0 && macroEvent.ScanCode > 0;
        var flags = useScanCode ? NativeMethods.KeyeventfScanCode : 0;
        if ((macroEvent.Flags & NativeMethods.LlkhfExtended) != 0)
        {
            flags |= NativeMethods.KeyeventfExtendedKey;
        }

        if (!macroEvent.IsDown)
        {
            flags |= NativeMethods.KeyeventfKeyUp;
        }

        var input = new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = useScanCode ? (ushort)0 : (ushort)macroEvent.VirtualKey,
                    WScan = useScanCode ? (ushort)macroEvent.ScanCode : (ushort)0,
                    DwFlags = flags,
                    DwExtraInfo = NativeMethods.KofgeClickerExtraInfo
                }
            }
        };
        SendInput(ref input);
    }

    private KeyboardPlaybackTransport ResolveKeyboardTransport(
        MacroEvent macroEvent,
        out PressedKeyState state)
    {
        if (RequiresSystemKeyboardTransport(macroEvent))
        {
            state = new PressedKeyState(
                new MacroEvent(),
                KeyboardPlaybackTransport.SendInput,
                IntPtr.Zero);
            return KeyboardPlaybackTransport.SendInput;
        }

        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var targetWindow = foregroundWindow;
        if (foregroundWindow != IntPtr.Zero)
        {
            var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
            var threadInfo = new NativeMethods.GuiThreadInfo
            {
                CbSize = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
            };
            if (threadId != 0 &&
                NativeMethods.GetGUIThreadInfo(threadId, ref threadInfo) &&
                threadInfo.HwndFocus != IntPtr.Zero)
            {
                targetWindow = threadInfo.HwndFocus;
            }

            var rootClass = NativeMethods.GetWindowClass(foregroundWindow);
            var targetClass = NativeMethods.GetWindowClass(targetWindow);
            if (SupportsKeyboardMessages(
                rootClass,
                targetClass,
                foregroundWindow != targetWindow,
                macroEvent))
            {
                state = new PressedKeyState(
                    new MacroEvent(),
                    KeyboardPlaybackTransport.WindowMessage,
                    targetWindow);
                return KeyboardPlaybackTransport.WindowMessage;
            }
        }

        state = new PressedKeyState(
            new MacroEvent(),
            KeyboardPlaybackTransport.SendInput,
            IntPtr.Zero);
        return KeyboardPlaybackTransport.SendInput;
    }

    private bool RequiresSystemKeyboardTransport(MacroEvent macroEvent)
    {
        if (IsModifierKey(macroEvent.VirtualKey) || IsSystemOnlyKey(macroEvent.VirtualKey))
        {
            return true;
        }

        var pressedVirtualKeys = _pressedKeys.Values
            .Select(value => value.SourceEvent.VirtualKey)
            .ToArray();
        if (pressedVirtualKeys.Any(IsControlKey) ||
            pressedVirtualKeys.Any(IsAltKey) ||
            pressedVirtualKeys.Any(IsWindowsKey))
        {
            return true;
        }

        return string.IsNullOrEmpty(macroEvent.Text) && pressedVirtualKeys.Any(IsShiftKey);
    }

    private static bool IsModifierKey(uint virtualKey)
    {
        return IsShiftKey(virtualKey) ||
            IsControlKey(virtualKey) ||
            IsAltKey(virtualKey) ||
            IsWindowsKey(virtualKey);
    }

    private static bool IsShiftKey(uint virtualKey)
    {
        return virtualKey is NativeMethods.VkShift or 0xA0u or 0xA1u;
    }

    private static bool IsControlKey(uint virtualKey)
    {
        return virtualKey is NativeMethods.VkControl or 0xA2u or 0xA3u;
    }

    private static bool IsWindowsKey(uint virtualKey)
    {
        return virtualKey is NativeMethods.VkLWin or NativeMethods.VkRWin;
    }

    private static bool IsSystemOnlyKey(uint virtualKey)
    {
        return virtualKey is NativeMethods.VkPrintScreen or NativeMethods.VkPause or NativeMethods.VkApps;
    }

    private static bool SupportsKeyboardMessages(
        string rootClass,
        string targetClass,
        bool hasFocusedChild,
        MacroEvent macroEvent)
    {
        if (rootClass.Equals("LWJGL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrEmpty(macroEvent.Text))
        {
            return false;
        }

        if (rootClass.StartsWith("Chrome_WidgetWin_", StringComparison.OrdinalIgnoreCase) ||
            rootClass.StartsWith("MozillaWindowClass", StringComparison.OrdinalIgnoreCase) ||
            rootClass.StartsWith("HwndWrapper[", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return hasFocusedChild &&
            (targetClass.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
             targetClass.Contains("RichEdit", StringComparison.OrdinalIgnoreCase) ||
             targetClass.StartsWith("WindowsForms10.", StringComparison.OrdinalIgnoreCase));
    }

    private bool TrySendKeyboardMessage(IntPtr targetWindow, MacroEvent macroEvent, bool wasPressed)
    {
        if (targetWindow == IntPtr.Zero || macroEvent.VirtualKey <= 0)
        {
            return false;
        }

        var altActive = IsAltKey(macroEvent.VirtualKey) ||
            _pressedKeys.Values.Any(value => IsAltKey(value.SourceEvent.VirtualKey));
        var message = macroEvent.IsDown
            ? altActive ? NativeMethods.WmSysKeyDown : NativeMethods.WmKeyDown
            : altActive ? NativeMethods.WmSysKeyUp : NativeMethods.WmKeyUp;
        var lParam = BuildKeyboardMessageLParam(macroEvent, wasPressed, altActive);
        if (!NativeMethods.TrySendMessage(
            targetWindow,
            (uint)message,
            (nuint)macroEvent.VirtualKey,
            lParam))
        {
            return false;
        }

        if (!macroEvent.IsDown || string.IsNullOrEmpty(macroEvent.Text))
        {
            return true;
        }

        var characterMessage = altActive ? NativeMethods.WmSysChar : NativeMethods.WmChar;
        foreach (var character in macroEvent.Text)
        {
            if (!NativeMethods.TrySendMessage(
                targetWindow,
                (uint)characterMessage,
                character,
                lParam))
            {
                InputDiagnostics.Write(
                    $"MacroCharacterPlaybackFailed codepoint={(int)character} target=0x{targetWindow.ToInt64():X}");
                return false;
            }
        }

        return true;
    }

    private static nint BuildKeyboardMessageLParam(
        MacroEvent macroEvent,
        bool wasPressed,
        bool altActive)
    {
        long value = 1;
        value |= ((long)macroEvent.ScanCode & 0xFF) << 16;
        if ((macroEvent.Flags & NativeMethods.LlkhfExtended) != 0)
        {
            value |= 1L << 24;
        }

        if (altActive)
        {
            value |= 1L << 29;
        }

        if (wasPressed || !macroEvent.IsDown)
        {
            value |= 1L << 30;
        }

        if (!macroEvent.IsDown)
        {
            value |= 1L << 31;
        }

        return (nint)value;
    }

    private static bool IsAltKey(uint virtualKey)
    {
        return virtualKey is NativeMethods.VkMenu or 0xA4u or 0xA5u;
    }

    private static string GetKeyboardTransportName(
        KeyboardPlaybackTransport transport,
        MacroEvent macroEvent)
    {
        if (transport == KeyboardPlaybackTransport.WindowMessage)
        {
            return "window-message";
        }

        return macroEvent.VirtualKey == 0 && macroEvent.ScanCode > 0
            ? "scan"
            : "virtual-key";
    }

    private void SendMouseButtonEvent(
        MacroDefinition macro,
        MacroEvent macroEvent,
        IntPtr backgroundMouseTarget)
    {
        var token = NormalizeMouseToken(macroEvent.Token);
        if (backgroundMouseTarget != IntPtr.Zero)
        {
            SendBackgroundMouseButtonEvent(backgroundMouseTarget, macro, macroEvent, token);
            return;
        }

        var (flags, mouseData) = GetMouseButtonInput(token, macroEvent.IsDown);
        SendMouseActionAt(macro, macroEvent, flags, mouseData);
        if (macroEvent.IsDown)
        {
            _pressedMouseButtons[token] = new PressedMouseState(
                Background: false,
                IntPtr.Zero,
                default);
        }
        else
        {
            _pressedMouseButtons.Remove(token);
        }
    }

    private void SendBackgroundMouseButtonEvent(
        IntPtr rootTarget,
        MacroDefinition macro,
        MacroEvent macroEvent,
        string token)
    {
        PressedMouseState state;
        if (!macroEvent.IsDown &&
            _pressedMouseButtons.TryGetValue(token, out var pressedState) &&
            pressedState.Background &&
            NativeMethods.IsWindow(pressedState.TargetWindow))
        {
            state = pressedState;
        }
        else
        {
            state = ResolveBackgroundMouseState(rootTarget, macro, macroEvent);
        }

        if (macroEvent.IsDown)
        {
            SendBackgroundMouseMove(state.TargetWindow, state.ClientPoint);
        }

        var message = GetBackgroundMouseButtonMessage(token, macroEvent.IsDown);
        var wParam = BuildBackgroundMouseButtonWParam(token, macroEvent.IsDown);
        SendBackgroundMouseMessage(
            state.TargetWindow,
            message,
            wParam,
            PackPoint(state.ClientPoint));

        if (macroEvent.IsDown)
        {
            _pressedMouseButtons[token] = state;
        }
        else
        {
            _pressedMouseButtons.Remove(token);
        }
    }

    private void SendBackgroundWheelEvent(
        IntPtr rootTarget,
        MacroDefinition macro,
        MacroEvent macroEvent,
        bool horizontal)
    {
        var state = ResolveBackgroundMouseState(rootTarget, macro, macroEvent, out var screenPoint);
        SendBackgroundMouseMove(state.TargetWindow, state.ClientPoint);
        var message = horizontal ? NativeMethods.WmMouseHWheel : NativeMethods.WmMouseWheel;
        var wParam = GetBackgroundMouseKeyState() |
            ((uint)(ushort)macroEvent.WheelDelta << 16);
        SendBackgroundMouseMessage(
            state.TargetWindow,
            (uint)message,
            wParam,
            PackPoint(screenPoint));
    }

    private static PressedMouseState ResolveBackgroundMouseState(
        IntPtr rootTarget,
        MacroDefinition macro,
        MacroEvent macroEvent)
    {
        return ResolveBackgroundMouseState(rootTarget, macro, macroEvent, out _);
    }

    private static PressedMouseState ResolveBackgroundMouseState(
        IntPtr rootTarget,
        MacroDefinition macro,
        MacroEvent macroEvent,
        out NativeMethods.Point screenPoint)
    {
        if (!NativeMethods.IsWindow(rootTarget) ||
            !NativeMethods.TryGetClientScreenBounds(rootTarget, out var targetBounds))
        {
            throw new Win32Exception("The selected background target window is no longer available.");
        }

        screenPoint = GetBackgroundScreenPoint(macro, macroEvent, targetBounds);
        var messageTarget = FindBackgroundMessageTarget(rootTarget, screenPoint);
        var clientPoint = screenPoint;
        if (!NativeMethods.ScreenToClient(messageTarget, ref clientPoint))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to map the background click coordinates.");
        }

        return new PressedMouseState(
            Background: true,
            messageTarget,
            clientPoint);
    }

    private static NativeMethods.Point GetBackgroundScreenPoint(
        MacroDefinition macro,
        MacroEvent macroEvent,
        Rectangle targetBounds)
    {
        int x;
        int y;
        if (macro.RecordedTargetClientWidth > 1 && macro.RecordedTargetClientHeight > 1)
        {
            x = ScaleCoordinate(
                macroEvent.X,
                macro.RecordedTargetClientLeft,
                macro.RecordedTargetClientWidth,
                targetBounds.Left,
                targetBounds.Width);
            y = ScaleCoordinate(
                macroEvent.Y,
                macro.RecordedTargetClientTop,
                macro.RecordedTargetClientHeight,
                targetBounds.Top,
                targetBounds.Height);
        }
        else
        {
            var currentScreen = SystemInformation.VirtualScreen;
            x = ScaleCoordinate(
                macroEvent.X,
                macro.RecordedScreenLeft,
                macro.RecordedScreenWidth,
                currentScreen.Left,
                currentScreen.Width);
            y = ScaleCoordinate(
                macroEvent.Y,
                macro.RecordedScreenTop,
                macro.RecordedScreenHeight,
                currentScreen.Top,
                currentScreen.Height);
            x = Math.Clamp(x, targetBounds.Left, targetBounds.Right - 1);
            y = Math.Clamp(y, targetBounds.Top, targetBounds.Bottom - 1);
        }

        return new NativeMethods.Point { X = x, Y = y };
    }

    private static IntPtr FindBackgroundMessageTarget(
        IntPtr rootTarget,
        NativeMethods.Point screenPoint)
    {
        var current = rootTarget;
        for (var depth = 0; depth < 12; depth++)
        {
            var clientPoint = screenPoint;
            if (!NativeMethods.ScreenToClient(current, ref clientPoint))
            {
                break;
            }

            var child = NativeMethods.ChildWindowFromPointEx(
                current,
                clientPoint,
                NativeMethods.CwpSkipInvisible |
                NativeMethods.CwpSkipDisabled |
                NativeMethods.CwpSkipTransparent);
            if (child == IntPtr.Zero || child == current)
            {
                break;
            }

            current = child;
        }

        return current;
    }

    private void SendBackgroundMouseMove(
        IntPtr targetWindow,
        NativeMethods.Point clientPoint)
    {
        SendBackgroundMouseMessage(
            targetWindow,
            NativeMethods.WmMouseMove,
            GetBackgroundMouseKeyState(),
            PackPoint(clientPoint));
    }

    private static void SendBackgroundMouseMessage(
        IntPtr targetWindow,
        uint message,
        uint wParam,
        nint lParam)
    {
        if (!NativeMethods.IsWindow(targetWindow) ||
            !NativeMethods.TrySendMessage(targetWindow, message, wParam, lParam))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to send a background mouse event.");
        }
    }

    private uint BuildBackgroundMouseButtonWParam(string token, bool isDown)
    {
        var keyState = GetBackgroundMouseKeyState(token, isDown);
        return token switch
        {
            "XButton1" => keyState | (NativeMethods.XButton1MouseData << 16),
            "XButton2" => keyState | (NativeMethods.XButton2MouseData << 16),
            _ => keyState
        };
    }

    private uint GetBackgroundMouseKeyState(
        string? changingToken = null,
        bool changingDown = false)
    {
        var state = 0u;
        foreach (var pair in _pressedMouseButtons)
        {
            if (!pair.Value.Background ||
                (!changingDown && pair.Key.Equals(changingToken, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            state |= GetMouseKeyStateFlag(pair.Key);
        }

        if (changingDown && changingToken is not null)
        {
            state |= GetMouseKeyStateFlag(changingToken);
        }

        return state;
    }

    private static uint GetMouseKeyStateFlag(string token)
    {
        return token switch
        {
            "RButton" => 0x0002,
            "MButton" => 0x0010,
            "XButton1" => 0x0020,
            "XButton2" => 0x0040,
            _ => 0x0001
        };
    }

    private static uint GetBackgroundMouseButtonMessage(string token, bool isDown)
    {
        return token switch
        {
            "RButton" => (uint)(isDown ? NativeMethods.WmRButtonDown : NativeMethods.WmRButtonUp),
            "MButton" => (uint)(isDown ? NativeMethods.WmMButtonDown : NativeMethods.WmMButtonUp),
            "XButton1" or "XButton2" => (uint)(isDown ? NativeMethods.WmXButtonDown : NativeMethods.WmXButtonUp),
            _ => (uint)(isDown ? NativeMethods.WmLButtonDown : NativeMethods.WmLButtonUp)
        };
    }

    private static nint PackPoint(NativeMethods.Point point)
    {
        return unchecked((nint)(uint)((ushort)point.X | ((uint)(ushort)point.Y << 16)));
    }

    private static void SendMouseMove(MacroDefinition macro, int recordedX, int recordedY)
    {
        var input = CreateMouseMoveInput(macro, recordedX, recordedY);
        SendInput(ref input);
    }

    private static NativeMethods.Input CreateMouseMoveInput(
        MacroDefinition macro,
        int recordedX,
        int recordedY)
    {
        var currentScreen = SystemInformation.VirtualScreen;
        var x = ScaleCoordinate(
            recordedX,
            macro.RecordedScreenLeft,
            macro.RecordedScreenWidth,
            currentScreen.Left,
            currentScreen.Width);
        var y = ScaleCoordinate(
            recordedY,
            macro.RecordedScreenTop,
            macro.RecordedScreenHeight,
            currentScreen.Top,
            currentScreen.Height);
        var normalizedX = NormalizeAbsoluteCoordinate(x, currentScreen.Left, currentScreen.Width);
        var normalizedY = NormalizeAbsoluteCoordinate(y, currentScreen.Top, currentScreen.Height);
        return CreateMouseInput(
            NativeMethods.MouseeventfMove |
            NativeMethods.MouseeventfAbsolute |
            NativeMethods.MouseeventfVirtualDesk,
            0,
            normalizedX,
            normalizedY);
    }

    private static void SendMouseActionAt(
        MacroDefinition macro,
        MacroEvent macroEvent,
        uint flags,
        uint mouseData)
    {
        var inputs = new[]
        {
            CreateMouseMoveInput(macro, macroEvent.X, macroEvent.Y),
            CreateMouseInput(flags, mouseData)
        };
        SendInputs(inputs);
    }

    private static void SendMouseInput(uint flags, uint mouseData, int dx = 0, int dy = 0)
    {
        var input = CreateMouseInput(flags, mouseData, dx, dy);
        SendInput(ref input);
    }

    private static NativeMethods.Input CreateMouseInput(
        uint flags,
        uint mouseData,
        int dx = 0,
        int dy = 0)
    {
        return new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            U = new NativeMethods.InputUnion
            {
                Mi = new NativeMethods.MouseInput
                {
                    Dx = dx,
                    Dy = dy,
                    MouseData = mouseData,
                    DwFlags = flags,
                    DwExtraInfo = NativeMethods.KofgeClickerExtraInfo
                }
            }
        };
    }

    private static void SendInput(ref NativeMethods.Input input)
    {
        var sent = NativeMethods.SendInput(1, ref input, Marshal.SizeOf<NativeMethods.Input>());
        if (sent != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to replay a recorded input event.");
        }
    }

    private static void SendInputs(NativeMethods.Input[] inputs)
    {
        var sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());
        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to replay recorded input events.");
        }
    }

    private void ReleasePressedInputs()
    {
        foreach (var pair in _pressedKeys.ToArray())
        {
            var key = pair.Value;
            try
            {
                var release = new MacroEvent
                {
                    Type = MacroEventType.Key,
                    IsDown = false,
                    VirtualKey = key.SourceEvent.VirtualKey,
                    ScanCode = key.SourceEvent.ScanCode,
                    Flags = key.SourceEvent.Flags
                };
                SendKeyboardEvent(release);
            }
            catch (Exception ex)
            {
                InputDiagnostics.Write(
                    $"MacroKeyReleaseFailed key={key.SourceEvent.Token} error={ex.GetType().Name}");
            }
            finally
            {
                _pressedKeys.Remove(pair.Key);
            }
        }

        foreach (var pair in _pressedMouseButtons.ToArray())
        {
            try
            {
                if (pair.Value.Background && NativeMethods.IsWindow(pair.Value.TargetWindow))
                {
                    SendBackgroundMouseMessage(
                        pair.Value.TargetWindow,
                        GetBackgroundMouseButtonMessage(pair.Key, isDown: false),
                        BuildBackgroundMouseButtonWParam(pair.Key, isDown: false),
                        PackPoint(pair.Value.ClientPoint));
                }
                else if (!pair.Value.Background)
                {
                    var (flags, mouseData) = GetMouseButtonInput(pair.Key, isDown: false);
                    SendMouseInput(flags, mouseData);
                }
            }
            catch (Exception ex)
            {
                InputDiagnostics.Write($"MacroMouseReleaseFailed button={pair.Key} error={ex.GetType().Name}");
            }
            finally
            {
                _pressedMouseButtons.Remove(pair.Key);
            }
        }
    }

    private static (uint Flags, uint MouseData) GetMouseButtonInput(string token, bool isDown)
    {
        return token switch
        {
            "RButton" => (isDown ? NativeMethods.MouseeventfRightDown : NativeMethods.MouseeventfRightUp, 0),
            "MButton" => (isDown ? NativeMethods.MouseeventfMiddleDown : NativeMethods.MouseeventfMiddleUp, 0),
            "XButton1" => (isDown ? NativeMethods.MouseeventfXDown : NativeMethods.MouseeventfXUp, NativeMethods.XButton1MouseData),
            "XButton2" => (isDown ? NativeMethods.MouseeventfXDown : NativeMethods.MouseeventfXUp, NativeMethods.XButton2MouseData),
            _ => (isDown ? NativeMethods.MouseeventfLeftDown : NativeMethods.MouseeventfLeftUp, 0)
        };
    }

    private static string NormalizeMouseToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "LButton";
        }

        return token switch
        {
            "Right" => "RButton",
            "Middle" => "MButton",
            "XButton1" => "XButton1",
            "XButton2" => "XButton2",
            _ => token.Equals("RButton", StringComparison.OrdinalIgnoreCase)
                ? "RButton"
                : token.Equals("MButton", StringComparison.OrdinalIgnoreCase)
                    ? "MButton"
                    : token.Equals("XButton1", StringComparison.OrdinalIgnoreCase)
                        ? "XButton1"
                        : token.Equals("XButton2", StringComparison.OrdinalIgnoreCase)
                            ? "XButton2"
                            : "LButton"
        };
    }

    private static string GetKeyId(MacroEvent macroEvent)
    {
        var extended = (macroEvent.Flags & NativeMethods.LlkhfExtended) != 0 ? 1 : 0;
        return macroEvent.ScanCode > 0
            ? $"S:{macroEvent.ScanCode}:{extended}"
            : $"V:{macroEvent.VirtualKey}:{extended}";
    }

    private static int ScaleCoordinate(
        int value,
        int recordedOrigin,
        int recordedLength,
        int currentOrigin,
        int currentLength)
    {
        if (currentLength <= 1)
        {
            return currentOrigin;
        }

        if (recordedLength <= 1)
        {
            return Math.Clamp(value, currentOrigin, currentOrigin + currentLength - 1);
        }

        var ratio = (value - recordedOrigin) / (double)(recordedLength - 1);
        var scaled = currentOrigin + (int)Math.Round(ratio * (currentLength - 1));
        return Math.Clamp(scaled, currentOrigin, currentOrigin + currentLength - 1);
    }

    private static int NormalizeAbsoluteCoordinate(int value, int origin, int length)
    {
        return length <= 1
            ? 0
            : (int)Math.Round((value - origin) * 65_535d / (length - 1));
    }

    private static long GetElapsedMicroseconds(long startedAt)
    {
        var ticks = Stopwatch.GetTimestamp() - startedAt;
        return (long)(ticks * (1_000_000d / Stopwatch.Frequency));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
