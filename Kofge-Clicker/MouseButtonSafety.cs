using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal static class MouseButtonSafety
{
    private const int ReleaseSendAttempts = 3;
    private static readonly HashSet<string> ClickerPressedButtons = new(StringComparer.Ordinal);
    private static readonly object Sync = new();

    internal static bool TryPressButton(string buttonName)
    {
        var normalized = NormalizeButtonName(buttonName);
        if (normalized.Length == 0)
        {
            return false;
        }

        lock (Sync)
        {
            if (!SendMouseButton(normalized, isDown: true, attempts: 1))
            {
                return false;
            }

            ClickerPressedButtons.Add(normalized);
            return true;
        }
    }

    internal static bool ReleaseButton(string buttonName)
    {
        var normalized = NormalizeButtonName(buttonName);
        if (normalized.Length == 0)
        {
            return false;
        }

        lock (Sync)
        {
            if (!ClickerPressedButtons.Contains(normalized))
            {
                return true;
            }

            if (!SendMouseButton(normalized, isDown: false, attempts: ReleaseSendAttempts))
            {
                return false;
            }

            ClickerPressedButtons.Remove(normalized);
            return true;
        }
    }

    internal static void ReleaseAllPressedButtons()
    {
        lock (Sync)
        {
            foreach (var button in ClickerPressedButtons.ToArray())
            {
                if (SendMouseButton(button, isDown: false, attempts: ReleaseSendAttempts))
                {
                    ClickerPressedButtons.Remove(button);
                }
            }
        }
    }

    internal static bool HasPressedButtons
    {
        get
        {
            lock (Sync)
            {
                return ClickerPressedButtons.Count > 0;
            }
        }
    }

    internal static void ReleaseAllPressedButtonsExcept(string? preservedButton)
    {
        var normalizedPreserved = NormalizeButtonName(preservedButton);
        lock (Sync)
        {
            foreach (var button in ClickerPressedButtons.ToArray())
            {
                if (!string.Equals(button, normalizedPreserved, StringComparison.Ordinal)
                    && SendMouseButton(button, isDown: false, attempts: ReleaseSendAttempts))
                {
                    ClickerPressedButtons.Remove(button);
                }
            }
        }
    }

    internal static bool ForceReleaseButton(string buttonName)
    {
        var normalized = NormalizeButtonName(buttonName);
        if (normalized.Length == 0)
        {
            return false;
        }

        lock (Sync)
        {
            if (!SendMouseButton(normalized, isDown: false, attempts: ReleaseSendAttempts))
            {
                return false;
            }

            ClickerPressedButtons.Remove(normalized);
            return true;
        }
    }

    internal static void ForceReleasePrimaryButtons()
    {
        _ = ForceReleaseButton("Left");
        _ = ForceReleaseButton("Right");
    }

    internal static void ForceReleaseSideButtons()
    {
        _ = ForceReleaseButton("XButton1");
        _ = ForceReleaseButton("XButton2");
    }

    private static string NormalizeButtonName(string? buttonName)
    {
        return buttonName switch
        {
            "Right" or "RButton" => "Right",
            "Left" or "LButton" => "Left",
            "Middle" or "MButton" => "Middle",
            "XButton1" => "XButton1",
            "XButton2" => "XButton2",
            _ => string.Empty
        };
    }

    private static bool SendMouseButton(string normalizedButton, bool isDown, int attempts)
    {
        var (flags, mouseData) = normalizedButton switch
        {
            "Right" => (isDown ? NativeMethods.MouseeventfRightDown : NativeMethods.MouseeventfRightUp, 0U),
            "Middle" => (isDown ? NativeMethods.MouseeventfMiddleDown : NativeMethods.MouseeventfMiddleUp, 0U),
            "XButton1" => (isDown ? NativeMethods.MouseeventfXDown : NativeMethods.MouseeventfXUp, NativeMethods.XButton1MouseData),
            "XButton2" => (isDown ? NativeMethods.MouseeventfXDown : NativeMethods.MouseeventfXUp, NativeMethods.XButton2MouseData),
            _ => (isDown ? NativeMethods.MouseeventfLeftDown : NativeMethods.MouseeventfLeftUp, 0U)
        };

        return SendMouse(flags, mouseData, attempts);
    }

    private static bool SendMouse(uint flags, uint mouseData, int attempts)
    {
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var input = new NativeMethods.Input
            {
                Type = 0,
                U = new NativeMethods.InputUnion
                {
                    Mi = new NativeMethods.MouseInput
                    {
                        MouseData = mouseData,
                        DwFlags = flags,
                        DwExtraInfo = NativeMethods.KofgeClickerExtraInfo
                    }
                }
            };

            var sendStartedAt = Stopwatch.GetTimestamp();
            var sent = NativeMethods.SendInput(1, ref input, Marshal.SizeOf<NativeMethods.Input>());
            var sendElapsedMs = Stopwatch.GetElapsedTime(sendStartedAt).TotalMilliseconds;
            if (sendElapsedMs >= 20)
            {
                InputDiagnostics.Write($"SlowSafetySendInput flags={flags} elapsedMs={sendElapsedMs:F2}");
            }

            if (sent == 1)
            {
                return true;
            }

            InputDiagnostics.Write($"SafetySendInputFailed flags={flags} attempt={attempt}/{attempts} sent={sent} error={Marshal.GetLastWin32Error()}");
            if (attempt < attempts)
            {
                Thread.Sleep(1);
            }
        }

        return false;
    }
}
