using System.Diagnostics;

namespace KofgeClicker;

internal readonly record struct MacroRecorderSnapshot(
    bool IsRecording,
    long ElapsedMicroseconds,
    int EventCount,
    IReadOnlyList<MacroEvent> RecentEvents);

internal sealed class MacroRecorder
{
    private const long MouseMoveSampleIntervalMicroseconds = 16_000;
    internal const int MaximumEventCount = MacroDefinition.MaximumEventCount;

    private readonly object _sync = new();
    private readonly List<MacroEvent> _events = [];
    private long _startedAt;
    private long _lastMouseMoveAt = long.MinValue;
    private bool _recordMouseMovement = true;
    private volatile bool _isRecording;

    internal bool IsRecording => _isRecording;

    internal void Start(bool recordMouseMovement = true)
    {
        lock (_sync)
        {
            _events.Clear();
            _startedAt = Stopwatch.GetTimestamp();
            _lastMouseMoveAt = long.MinValue;
            _recordMouseMovement = recordMouseMovement;
            _isRecording = true;
        }
    }

    internal void Record(ObservedInputEvent input)
    {
        if (!_isRecording)
        {
            return;
        }

        lock (_sync)
        {
            if (!_isRecording || _events.Count >= MaximumEventCount)
            {
                return;
            }

            if (!_recordMouseMovement && input.Kind == ObservedInputKind.MouseMove)
            {
                return;
            }

            var offset = GetElapsedMicroseconds();
            var macroEvent = ConvertEvent(input, offset);
            if (macroEvent.Type == MacroEventType.Key &&
                macroEvent.IsDown &&
                IsModifierVirtualKey(macroEvent.VirtualKey) &&
                IsKeyDown(macroEvent.VirtualKey))
            {
                return;
            }

            if (macroEvent.Type == MacroEventType.MouseMove &&
                _lastMouseMoveAt != long.MinValue &&
                offset - _lastMouseMoveAt < MouseMoveSampleIntervalMicroseconds)
            {
                if (_events.Count > 0 && _events[^1].Type == MacroEventType.MouseMove)
                {
                    _events[^1] = macroEvent;
                }

                return;
            }

            _events.Add(macroEvent);
            if (macroEvent.Type == MacroEventType.MouseMove)
            {
                _lastMouseMoveAt = offset;
            }
        }
    }

    internal void EnsureKeyDown(uint virtualKey)
    {
        lock (_sync)
        {
            if (!_isRecording || _events.Count >= MaximumEventCount)
            {
                return;
            }

            if (IsKeyDown(virtualKey))
            {
                return;
            }

            var mappedScanCode = NativeMethods.MapVirtualKey(
                virtualKey,
                NativeMethods.MapvkVkToVscEx);
            _events.Add(new MacroEvent
            {
                Type = MacroEventType.Key,
                OffsetMicroseconds = GetElapsedMicroseconds(),
                Token = NormalizeKeyToken(HotkeyHelper.FromVirtualKey((int)virtualKey)),
                IsDown = true,
                VirtualKey = virtualKey,
                ScanCode = mappedScanCode & 0xFF,
                Flags = (mappedScanCode & 0xFF00) != 0
                    ? NativeMethods.LlkhfExtended
                    : 0
            });
        }
    }

    internal MacroRecorderSnapshot GetSnapshot(int recentEventCount = 6)
    {
        lock (_sync)
        {
            var recent = _events
                .Skip(Math.Max(0, _events.Count - recentEventCount))
                .ToArray();
            return new MacroRecorderSnapshot(
                _isRecording,
                _isRecording ? GetElapsedMicroseconds() : GetRecordedDuration(),
                _events.Count,
                recent);
        }
    }

    internal MacroDefinition Stop(string id, string name)
    {
        lock (_sync)
        {
            _isRecording = false;
            var virtualScreen = SystemInformation.VirtualScreen;
            return new MacroDefinition
            {
                Id = id,
                Name = name,
                UpdatedUtc = DateTime.UtcNow,
                RecordedScreenLeft = virtualScreen.Left,
                RecordedScreenTop = virtualScreen.Top,
                RecordedScreenWidth = virtualScreen.Width,
                RecordedScreenHeight = virtualScreen.Height,
                Events = [.. _events]
            };
        }
    }

    internal void DiscardTrailingHotkey(string storedHotkey)
    {
        if (!HotkeyChord.TryParse(storedHotkey, out var chord))
        {
            return;
        }

        var modifierTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (chord.Ctrl)
        {
            modifierTokens.Add("Ctrl");
        }

        if (chord.Shift)
        {
            modifierTokens.Add("Shift");
        }

        if (chord.Alt)
        {
            modifierTokens.Add("Alt");
        }

        lock (_sync)
        {
            if (!_isRecording || _events.Count == 0)
            {
                return;
            }

            var cutoff = Math.Max(0, GetElapsedMicroseconds() - 1_000_000);
            var triggerIndex = -1;
            for (var index = _events.Count - 1; index >= 0; index--)
            {
                var item = _events[index];
                if (item.OffsetMicroseconds < cutoff)
                {
                    break;
                }

                if (item.Type is MacroEventType.Key or MacroEventType.MouseButton &&
                    item.IsDown &&
                    item.Token.Equals(chord.PrimaryToken, StringComparison.OrdinalIgnoreCase))
                {
                    triggerIndex = index;
                    break;
                }
            }

            if (triggerIndex < 0)
            {
                return;
            }

            var removableTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                chord.PrimaryToken
            };
            var removalStart = triggerIndex;
            for (var index = triggerIndex - 1; index >= 0; index--)
            {
                var item = _events[index];
                if (item.Type is not (MacroEventType.Key or MacroEventType.MouseButton))
                {
                    continue;
                }

                if (item.IsDown && modifierTokens.Contains(item.Token))
                {
                    removableTokens.Add(item.Token);
                    removalStart = index;
                    continue;
                }

                break;
            }

            for (var index = _events.Count - 1; index >= removalStart; index--)
            {
                var item = _events[index];
                if (item.Type is MacroEventType.Key or MacroEventType.MouseButton &&
                    removableTokens.Contains(item.Token))
                {
                    _events.RemoveAt(index);
                }
            }
        }
    }

    internal void Cancel()
    {
        lock (_sync)
        {
            _isRecording = false;
            _events.Clear();
        }
    }

    private long GetElapsedMicroseconds()
    {
        var ticks = Stopwatch.GetTimestamp() - _startedAt;
        return (long)(ticks * (1_000_000d / Stopwatch.Frequency));
    }

    private long GetRecordedDuration()
    {
        return _events.Count == 0 ? 0 : _events[^1].OffsetMicroseconds;
    }

    private static MacroEvent ConvertEvent(ObservedInputEvent input, long offset)
    {
        return new MacroEvent
        {
            Type = input.Kind switch
            {
                ObservedInputKind.Key => MacroEventType.Key,
                ObservedInputKind.MouseButton => MacroEventType.MouseButton,
                ObservedInputKind.MouseMove => MacroEventType.MouseMove,
                ObservedInputKind.MouseWheel => MacroEventType.MouseWheel,
                _ => MacroEventType.MouseHorizontalWheel
            },
            OffsetMicroseconds = offset,
            Token = input.Kind == ObservedInputKind.Key
                ? NormalizeKeyToken(input.Token)
                : input.Token,
            IsDown = input.IsDown,
            X = input.X,
            Y = input.Y,
            WheelDelta = input.WheelDelta,
            VirtualKey = input.VirtualKey,
            ScanCode = input.ScanCode,
            Flags = input.Flags,
            Text = input.Text
        };
    }

    private bool IsKeyDown(uint virtualKey)
    {
        for (var index = _events.Count - 1; index >= 0; index--)
        {
            var item = _events[index];
            if (item.Type == MacroEventType.Key && item.VirtualKey == virtualKey)
            {
                return item.IsDown;
            }
        }

        return false;
    }

    private static bool IsModifierVirtualKey(uint virtualKey)
    {
        return virtualKey is
            NativeMethods.VkShift or
            NativeMethods.VkControl or
            NativeMethods.VkMenu or
            NativeMethods.VkLShift or
            NativeMethods.VkRShift or
            NativeMethods.VkLControl or
            NativeMethods.VkRControl or
            NativeMethods.VkLMenu or
            NativeMethods.VkRMenu or
            NativeMethods.VkLWin or
            NativeMethods.VkRWin;
    }

    private static string NormalizeKeyToken(string token)
    {
        return token switch
        {
            "ControlKey" or "LControlKey" or "RControlKey" => "Ctrl",
            "ShiftKey" or "LShiftKey" or "RShiftKey" => "Shift",
            "Menu" or "LMenu" or "RMenu" => "Alt",
            _ => token
        };
    }
}
