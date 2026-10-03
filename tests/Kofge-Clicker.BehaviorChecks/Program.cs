using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using KofgeClicker;

internal static class Program
{
    private static readonly Assembly App = typeof(HotkeyHelper).Assembly;
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;
    private static int _failed;

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Check("numpad-hotkey-roundtrip", VerifyNumpadHotkeys);
        Check("extended-hotkey-roundtrip", VerifyExtendedHotkeys);
        Check("background-mouse-modifiers", VerifyBackgroundModifiers);
        Check("macro-json-compatibility", VerifyJsonCompatibility);
        Check("background-final-press-duration", VerifyFinalPressDuration);
        Check("background-repeat-delivery", VerifyRepeats);
        Check("background-cancellation-releases-button", VerifyCancellation);
        Check("journal-batch-edit-and-restore", VerifyJournalEdits);
        Check("macro-storage-validation", VerifyStorage);
        Check("record-without-mouse-movement", VerifyRecording);
        Check("settings-section-roundtrip", VerifyIniSettings);
        return _failed == 0 ? 0 : 1;
    }

    private static void Check(string name, Action check)
    {
        try
        {
            check();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FAIL {name}: {ex.GetBaseException().Message}");
        }
    }

    private static void VerifyNumpadHotkeys()
    {
        for (var digit = 0; digit <= 9; digit++)
        {
            var key = (int)Keys.NumPad0 + digit;
            var token = HotkeyHelper.FromVirtualKey(key);
            Assert(HotkeyHelper.ToVirtualKey(token) == key, $"Cannot resolve {token}.");
            Assert(HotkeyHelper.NormalizePrimaryToken($"numpad{digit}") == token,
                $"Cannot normalize numpad{digit}.");
        }
    }

    private static void VerifyExtendedHotkeys()
    {
        foreach (var key in new[] { Keys.Multiply, Keys.Add, Keys.Subtract, Keys.Decimal,
                     Keys.Divide, Keys.LWin, Keys.RWin, Keys.VolumeUp, Keys.MediaPlayPause })
        {
            var token = HotkeyHelper.FromVirtualKey((int)key);
            Assert(HotkeyHelper.ToVirtualKey(token) == (int)key, $"Cannot resolve {token}.");
        }

        Assert(HotkeyHelper.ToVirtualKey("F25") is null, "Accepted an invalid function key.");
        Assert(HotkeyHelper.ToVirtualKey("UnknownKey") is null, "Accepted an unknown key.");
        Assert(!HotkeyChord.TryParse("None", out _), "An empty binding became active.");
    }

    private static void VerifyBackgroundModifiers()
    {
        using var player = (IDisposable)New("MacroPlayer");
        var keys = (IDictionary)player.GetType().GetField("_pressedKeys", Members)!.GetValue(player)!;
        var stateType = player.GetType().GetNestedType("PressedKeyState", BindingFlags.NonPublic)!;
        var transportType = player.GetType().GetNestedType("KeyboardPlaybackTransport", BindingFlags.NonPublic)!;
        object State(Keys key)
        {
            var source = New("MacroEvent");
            Set(source, "VirtualKey", (uint)key);
            return Activator.CreateInstance(stateType, Members, null,
                [source, Enum.Parse(transportType, "SendInput"), IntPtr.Zero], null)!;
        }

        uint Flags() => (uint)Call(player, "GetBackgroundMouseKeyState", null, false)!;
        Assert(Flags() == 0, "Unexpected modifier before playback.");
        keys.Add("ctrl", State(Keys.LControlKey));
        Assert(Flags() == 0x0008, "Recorded Ctrl is missing from background mouse flags.");
        keys.Add("shift", State(Keys.RShiftKey));
        Assert(Flags() == 0x000C, "Recorded Shift is missing from background mouse flags.");
        var clickFlags = (uint)Call(player, "BuildBackgroundMouseButtonWParam", "LButton", true)!;
        Assert(clickFlags == 0x000D, "Mouse-button flags lost the recorded modifiers.");
        keys.Clear();
        Assert(Flags() == 0, "Released modifiers remained in background mouse flags.");
    }

    private static void VerifyJsonCompatibility()
    {
        var type = Type("MacroDefinition");
        var legacy = JsonSerializer.Deserialize(
            "{\"FormatVersion\":1,\"Name\":\"legacy\",\"Events\":[]}", type)!;
        Assert(!(bool)Get(legacy, "BackgroundMousePlayback"), "Legacy macro enabled background mode.");
        Assert((int)Get(legacy, "PlaybackStartDelaySeconds") == 3, "Legacy start delay changed.");
        Set(legacy, "BackgroundMousePlayback", true);
        Set(legacy, "RecordedTargetClientWidth", 800);
        Set(legacy, "RecordedTargetClientHeight", 600);
        var copy = JsonSerializer.Deserialize(JsonSerializer.Serialize(legacy, type), type)!;
        Assert((bool)Get(copy, "BackgroundMousePlayback"), "Background setting was lost.");
        Assert((int)Get(copy, "RecordedTargetClientWidth") == 800 &&
            (int)Get(copy, "RecordedTargetClientHeight") == 600, "Target geometry was lost.");
    }

    private static void VerifyFinalPressDuration()
    {
        using var receiver = new MouseReceiver();
        receiver.Show();
        Application.DoEvents();
        var macro = CreateMouseMacro(receiver);
        using var player = (IDisposable)New("MacroPlayer");
        var cursorBefore = Cursor.Position;
        var task = (Task)Call(player, "PlayAsync", macro, receiver.Handle)!;
        Pump(task);
        Assert(receiver.DownCount == 1 && receiver.UpCount == 1,
            $"Unexpected background press/release counts: {receiver.DownCount}/{receiver.UpCount}.");
        var duration = Stopwatch.GetElapsedTime(receiver.DownTimestamp, receiver.UpTimestamp);
        Assert(duration.TotalMilliseconds >= 15,
            $"Final press was released after only {duration.TotalMilliseconds:F2} ms.");
        Assert(Cursor.Position == cursorBefore, "Background playback moved the physical cursor.");
        Assert(Get(Get(task, "Result"), "Status").ToString() == "Completed", "Playback did not complete.");
    }

    private static object CreateMouseMacro(MouseReceiver receiver)
    {
        var point = receiver.PointToScreen(new Point(20, 20));
        var macro = New("MacroDefinition");
        Set(macro, "RecordedTargetClientLeft", receiver.PointToScreen(Point.Empty).X);
        Set(macro, "RecordedTargetClientTop", receiver.PointToScreen(Point.Empty).Y);
        Set(macro, "RecordedTargetClientWidth", receiver.ClientSize.Width);
        Set(macro, "RecordedTargetClientHeight", receiver.ClientSize.Height);
        var press = New("MacroEvent");
        Set(press, "Type", Enum.Parse(Type("MacroEventType"), "MouseButton"));
        Set(press, "Token", "LButton");
        Set(press, "IsDown", true);
        Set(press, "X", point.X);
        Set(press, "Y", point.Y);
        ((IList)Get(macro, "Events")).Add(press);
        return macro;
    }

    private static void VerifyRepeats()
    {
        using var receiver = new MouseReceiver();
        receiver.Show();
        var macro = CreateMouseMacro(receiver);
        Set(macro, "RepeatCount", 3);
        var release = New("MacroEvent");
        var press = ((IList)Get(macro, "Events"))[0]!;
        Set(release, "Type", Get(press, "Type"));
        Set(release, "Token", "LButton");
        Set(release, "X", Get(press, "X"));
        Set(release, "Y", Get(press, "Y"));
        Set(release, "OffsetMicroseconds", 1_000L);
        ((IList)Get(macro, "Events")).Add(release);
        using var player = (IDisposable)New("MacroPlayer");
        var task = (Task)Call(player, "PlayAsync", macro, receiver.Handle)!;
        Pump(task);
        Assert(receiver.DownCount == 3 && receiver.UpCount == 3, "A repeated click was lost.");
        Assert(Get(Get(task, "Result"), "Status").ToString() == "Completed", "Repeated playback failed.");
    }

    private static void VerifyCancellation()
    {
        using var receiver = new MouseReceiver();
        receiver.Show();
        var macro = CreateMouseMacro(receiver);
        var tail = New("MacroEvent");
        Set(tail, "Type", Enum.Parse(Type("MacroEventType"), "MouseMove"));
        Set(tail, "OffsetMicroseconds", 2_000_000L);
        ((IList)Get(macro, "Events")).Add(tail);
        using var player = (IDisposable)New("MacroPlayer");
        var task = (Task)Call(player, "PlayAsync", macro, receiver.Handle)!;
        var wait = Stopwatch.StartNew();
        while (receiver.DownCount == 0)
        {
            Assert(wait.Elapsed < TimeSpan.FromSeconds(5), "The first click timed out.");
            Application.DoEvents();
            Thread.Yield();
        }
        Pump(Task.Run(() => Call(player, "Stop")));
        Pump(task);
        Assert(receiver.UpCount == 1, "Cancellation left the mouse button pressed.");
        Assert(Get(Get(task, "Result"), "Status").ToString() == "Cancelled", "Playback ignored cancellation.");
    }

    private static void VerifyJournalEdits()
    {
        var macro = New("MacroDefinition");
        var events = (IList)Get(macro, "Events");
        for (var index = 0; index < 4; index++)
        {
            var item = New("MacroEvent");
            Set(item, "Type", Enum.Parse(Type("MacroEventType"), "MouseMove"));
            Set(item, "OffsetMicroseconds", (index + 1) * 1_000L);
            events.Add(item);
        }
        var offsets = (long[])Static("MacroJournalEditor", "CreateOffsets", macro)!;
        Static("MacroJournalEditor", "SetSleepBeforeMany", offsets, new[] { 1, 3 }, 5_000L);
        Assert(offsets.SequenceEqual(new long[] { 1_000, 6_000, 7_000, 12_000 }), "Batch Sleep changed other pauses.");
        var deleted = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Type("MacroDeletedEvent")))!;
        foreach (var index in new[] { 1, 3 })
            deleted.Add(New("MacroDeletedEvent", index, events[index], 5_000L));
        var removed = Static("MacroJournalEditor", "DeleteEvents", events, offsets, new[] { 1, 3 })!;
        var restored = Static("MacroJournalEditor", "RestoreEvents", Get(removed, "Events"),
            Get(removed, "Offsets"), deleted)!;
        Assert(((long[])Get(restored, "Offsets")).SequenceEqual(offsets), "Undo deletion lost timing.");
        var restoredEvents = ((IEnumerable)Get(restored, "Events")).Cast<object>().ToArray();
        Assert(restoredEvents.SequenceEqual(events.Cast<object>()), "Undo deletion changed event order.");
        // The 24-hour limit must fail atomically, without partially editing the timeline.
        var nearLimit = new long[] { 86_399_999_000L, 86_400_000_000L };
        var before = nearLimit.ToArray();
        AssertThrows<InvalidDataException>(() => Static("MacroJournalEditor", "SetSleepBeforeMany",
            nearLimit, new[] { 1 }, 2_000L));
        Assert(nearLimit.SequenceEqual(before), "A rejected edit partially changed the timeline.");
    }

    private static void VerifyStorage()
    {
        WithTemporaryDirectory(directory =>
        {
            var storage = New("MacroStorage", directory);
            var macro = New("MacroDefinition");
            Set(macro, "Name", "Storage check");
            Set(macro, "BackgroundMousePlayback", true);
            Call(storage, "Save", macro);
            var loaded = ((IEnumerable)Call(storage, "LoadAll")!).Cast<object>().ToArray();
            Assert(loaded.Length == 1 && (bool)Get(loaded[0], "BackgroundMousePlayback"), "Macro settings did not survive storage.");
            var invalid = New("MacroDefinition");
            Set(invalid, "Id", "../outside");
            Set(invalid, "Name", "Invalid");
            AssertThrows<InvalidDataException>(() => Call(storage, "Save", invalid));
            var invalidEvent = New("MacroEvent");
            Set(invalidEvent, "OffsetMicroseconds", -1L);
            ((IList)Get(macro, "Events")).Add(invalidEvent);
            AssertThrows<InvalidDataException>(() => Call(storage, "Save", macro));
            Assert(((IEnumerable)Call(storage, "LoadAll")!).Cast<object>().Count() == 1,
                "A rejected save damaged the existing macro.");
        });
    }

    private static void VerifyRecording()
    {
        var recorder = New("MacroRecorder");
        Call(recorder, "Start", false, null);
        object Input(string kind, string token, bool down, int x, int y) => New("ObservedInputEvent",
            Enum.Parse(Type("ObservedInputKind"), kind), token, down, x, y, 0,
            0u, 0u, 0u, 0u, false, "");
        Call(recorder, "Record", Input("MouseMove", "", false, 10, 20));
        Call(recorder, "Record", Input("MouseButton", "LButton", true, 13, 29));
        Call(recorder, "Record", Input("MouseMove", "", false, 30, 40));
        Call(recorder, "Record", Input("MouseButton", "LButton", false, 13, 29));
        var recorded = Call(recorder, "Stop", "test", "Recording check")!;
        var events = ((IEnumerable)Get(recorded, "Events")).Cast<object>().ToArray();
        Assert(events.Length == 2 && events.All(item => Get(item, "Type").ToString() == "MouseButton"),
            "Movement was included despite the setting.");
        Assert(events.All(item => (int)Get(item, "X") == 13 && (int)Get(item, "Y") == 29),
            "Click coordinates were lost.");
    }

    private static void VerifyIniSettings()
    {
        WithTemporaryDirectory(directory =>
        {
            var ini = new IniFile(Path.Combine(directory, "settings.ini"));
            ini.WriteString("Main", "Hotkey", "None");
            ini.WriteString("Profile_Test", "Name", "Test profile");
            ini.WriteString("Main", "CPS", "100");
            Assert(ini.ReadString("Main", "Hotkey") == "None", "Writing another setting restored an empty binding.");
            Assert(ini.ReadInt("Main", "CPS") == 100, "CPS did not round-trip.");
            Assert(ini.ReadString("Profile_Test", "Name") == "Test profile", "Writing Main damaged a profile.");
            ini.DeleteSection("Main");
            Assert(ini.ReadString("Profile_Test", "Name") == "Test profile", "Deleting Main damaged another section.");
        });
    }

    private static void WithTemporaryDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"Kofge-BehaviorChecks-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try { action(directory); }
        finally { Directory.Delete(directory, true); }
    }

    private static void AssertThrows<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (Exception ex) when (ex.GetBaseException() is T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Pump(Task task)
    {
        var elapsed = Stopwatch.StartNew();
        while (!task.IsCompleted)
        {
            Assert(elapsed.Elapsed < TimeSpan.FromSeconds(5), "Playback timed out.");
            Application.DoEvents();
            Thread.Yield();
        }
        task.GetAwaiter().GetResult();
        Application.DoEvents();
    }

    private sealed class MouseReceiver : Form
    {
        internal long DownTimestamp;
        internal long UpTimestamp;
        internal int DownCount;
        internal int UpCount;

        internal MouseReceiver()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(120, 120);
            ClientSize = new Size(100, 80);
            Text = "Kofge-Clicker behavior check";
        }

        protected override bool ShowWithoutActivation => true;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0201)
            {
                DownTimestamp = Stopwatch.GetTimestamp();
                DownCount++;
            }
            else if (message.Msg == 0x0202)
            {
                UpTimestamp = Stopwatch.GetTimestamp();
                UpCount++;
            }
            base.WndProc(ref message);
        }
    }

    private static Type Type(string name) => App.GetType($"KofgeClicker.{name}", true)!;
    private static object New(string name, params object?[] arguments) =>
        Activator.CreateInstance(Type(name), Members, null, arguments, null)!;
    private static object? Call(object target, string method, params object?[] arguments) =>
        target.GetType().GetMethod(method, Members)!.Invoke(target, arguments);
    private static object? Static(string type, string method, params object?[] arguments) =>
        Type(type).GetMethod(method, Members)!.Invoke(null, arguments);
    private static object Get(object target, string property) =>
        target.GetType().GetProperty(property, Members)!.GetValue(target)!;
    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property, Members)!.SetValue(target, value);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
