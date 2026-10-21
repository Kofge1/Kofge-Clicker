using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using KofgeClicker;

internal static class BackgroundInputChecks
{
    private static readonly Assembly App = typeof(HotkeyHelper).Assembly;
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;

    internal static void ChildButtonActivation()
    {
        using var probe = new InjectionProbe();
        foreach (var style in new[] { FlatStyle.System, FlatStyle.Standard })
        {
            using var target = new TestWindow();
            using var button = new Button { Bounds = new Rectangle(25, 25, 120, 40), FlatStyle = style };
            target.Controls.Add(button);
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            target.Show();
            using var cover = new TestWindow { Location = target.Location };
            cover.Show();
            cover.BringToFront();
            Application.DoEvents();
            var foreground = GetForegroundWindow();
            var macro = Macro(target);
            AddClick(macro, button.PointToScreen(new Point(20, 20)));
            Play(macro, target.Handle);
            Assert(clicks == 1, $"Covered {style} button received {clicks} clicks instead of one.");
            Assert(GetForegroundWindow() == foreground, "A standard button changed the foreground window.");
        }
        Assert(probe.Count == 0, "Background clicks attempted global input.");
    }

    internal static void KeyboardTargetIsolation()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        using var edit = new TextBox { Bounds = new Rectangle(20, 20, 200, 80), Multiline = true };
        target.Controls.Add(edit);
        target.Show();
        using var other = new TestWindow();
        using var otherEdit = new TextBox { Text = "untouched", Width = 200 };
        other.Controls.Add(otherEdit);
        other.Show();
        other.Activate();
        otherEdit.Focus();
        Application.DoEvents();
        var macro = Macro(target);
        AddClick(macro, edit.PointToScreen(new Point(8, 8)));
        AddKey(macro, Keys.F, "ф");
        AddKey(macro, Keys.A, "A");
        AddKey(macro, Keys.Back);
        AddKey(macro, Keys.Enter);
        AddKey(macro, Keys.F);
        var foreground = GetForegroundWindow();
        Play(macro, target.Handle);
        Assert(edit.Text.StartsWith("ф\r\n", StringComparison.Ordinal) && edit.Text.Length == 4,
            $"Target text was not reproduced exactly: {edit.Text.Replace("\r\n", "<enter>")}");
        Assert(otherEdit.Text == "untouched", "Keyboard input reached a different window on the same thread.");
        Assert(GetForegroundWindow() == foreground, "Background playback changed the foreground window.");
        Assert(probe.Count == 0, "Background typing attempted global input.");
    }

    internal static void KeyboardFailureNoGlobalFallback()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        using var edit = new ClosingEdit { Bounds = new Rectangle(20, 20, 200, 60) };
        target.Controls.Add(edit);
        target.Show();
        var macro = Macro(target);
        AddClick(macro, edit.PointToScreen(new Point(8, 8)));
        AddKey(macro, Keys.F, "f");
        Play(macro, target.Handle, "Failed");
        Assert(edit.DisposedOnKey, "Failure test did not close the keyboard destination.");
        Assert(probe.Count == 0, "Failed background delivery fell back to global input.");
    }

    internal static void ModifiersAndRelease()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        using var other = new TestWindow();
        target.Show();
        other.Show();
        other.Activate();
        var macro = Macro(target);
        AddKey(macro, Keys.LControlKey, down: true);
        AddKey(macro, Keys.F, "f");
        AddKey(macro, Keys.LControlKey, down: false);
        AddKey(macro, Keys.LWin, down: true);
        AddKey(macro, Keys.R);
        AddKey(macro, Keys.LWin, down: false);
        AddKey(macro, Keys.F, "f", down: true);
        Play(macro, target.Handle);
        Assert(target.KeyDowns == 5 && target.KeyUps == 5,
            $"Modifier/automatic release counts: {target.KeyDowns}/{target.KeyUps}.");
        Assert(other.KeyDowns == 0 && other.KeyUps == 0 && other.Characters == 0,
            "An unrelated focused window received background key messages.");
        Assert(target.Characters == 1, "A modifier chord inserted unintended text.");
        Assert(probe.Count == 0, "Background modifiers attempted global input.");
    }

    internal static void MissingTargetFailsClosed()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        var macro = Macro(target);
        AddKey(macro, Keys.F, "f");
        Play(macro, IntPtr.Zero, "Failed");
        target.Show();
        var handle = target.Handle;
        target.Close();
        Play(macro, handle, "Failed");
        Assert(probe.Count == 0, "A missing target caused global input.");
    }

    internal static void ButtonCancellation()
    {
        using var target = new TestWindow();
        using var button = new Button { Bounds = new Rectangle(20, 20, 150, 40) };
        target.Controls.Add(button);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        target.Show();
        var macro = Macro(target);
        AddClick(macro, button.PointToScreen(new Point(10, 10)));
        var events = (IList)Get(macro, "Events");
        Set(events[1]!, "OffsetMicroseconds", 2_000_000L);
        using var player = (IDisposable)New("MacroPlayer");
        var task = (Task)Call(player, "PlayAsync", macro, target.Handle)!;
        WaitFor(() => (int)Get(Call(player, "GetSnapshot")!, "CompletedEventCount") >= 1);
        Call(player, "Stop");
        WaitFor(() => task.IsCompleted);
        Assert(Get(Get(task, "Result"), "Status").ToString() == "Cancelled", "Playback did not cancel.");
        Assert(clicks == 0, "Cancellation activated a pending standard button.");
    }

    internal static void NestedControls()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        using var group = new GroupBox { Bounds = new Rectangle(10, 10, 250, 130) };
        using var button = new Button { Bounds = new Rectangle(20, 25, 160, 32) };
        using var check = new CheckBox { Bounds = new Rectangle(20, 70, 160, 32) };
        group.Controls.Add(button);
        group.Controls.Add(check);
        target.Controls.Add(group);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        target.Show();
        using var cover = new TestWindow { Location = target.Location };
        cover.Show();
        cover.BringToFront();
        Application.DoEvents();
        var macro = Macro(target);
        AddClick(macro, button.PointToScreen(new Point(10, 10)));
        AddClick(macro, check.PointToScreen(new Point(10, 10)));
        Play(macro, target.Handle);
        Assert(clicks == 1 && check.Checked, "Nested button or checkbox did not activate exactly once.");
        Assert(probe.Count == 0, "Nested controls attempted global input.");
    }

    internal static void MinimizedTargetReleasesKeys()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        target.Show();
        var macro = Macro(target);
        AddKey(macro, Keys.F, down: true);
        AddKey(macro, Keys.F, down: false);
        Set(((IList)Get(macro, "Events"))[1]!, "OffsetMicroseconds", 200_000L);
        using var player = (IDisposable)New("MacroPlayer");
        var task = (Task)Call(player, "PlayAsync", macro, target.Handle)!;
        WaitFor(() => target.KeyDowns == 1);
        target.WindowState = FormWindowState.Minimized;
        WaitFor(() => task.IsCompleted);
        Assert(Get(Get(task, "Result"), "Status").ToString() == "Failed", "Minimized target continued playback.");
        Assert(target.KeyUps == 1, "Minimizing the target prevented safe key release.");
        Assert(probe.Count == 0, "Minimized target caused global input.");
    }

    internal static void ReturnToNormalPlayback()
    {
        using var probe = new InjectionProbe();
        using var target = new TestWindow();
        target.Show();
        using var player = (IDisposable)New("MacroPlayer");
        var macro = Macro(target);
        AddKey(macro, Keys.LControlKey);
        var background = (Task)Call(player, "PlayAsync", macro, target.Handle)!;
        WaitFor(() => background.IsCompleted);
        Assert(probe.Count == 0 && target.KeyDowns == 1 && target.KeyUps == 1,
            "Background modifier was not isolated.");
        Set(macro, "BackgroundMousePlayback", false);
        var normal = (Task)Call(player, "PlayAsync", macro, IntPtr.Zero)!;
        WaitFor(() => normal.IsCompleted);
        Application.DoEvents();
        Assert(Get(Get(normal, "Result"), "Status").ToString() == "Completed", "Normal playback failed.");
        var arguments = new object?[] { ((IList)Get(macro, "Events"))[0], null };
        var transport = player.GetType().GetMethod("ResolveKeyboardTransport", Members)!.Invoke(player, arguments);
        Assert(transport!.ToString() == "SendInput", "Normal playback did not restore its original system-key transport.");
        Assert(target.KeyDowns == 1 && target.KeyUps == 1, "Background destination leaked into normal playback.");
    }

    internal static void HelpLayoutAndMigration()
    {
        using var font = UiTheme.CreateFont("Segoe UI", 12.5f);
        foreach (var language in new[] { "English", "Russian" })
        {
            var localization = Type("LocalizationService");
            var defaults = (IDictionary)localization.GetField(language, Members)!.GetValue(null)!;
            var legacy = (IDictionary)localization.GetField($"Legacy{language}Labels", Members)!.GetValue(null)!;
            var text = (string)defaults["Macros.BackgroundMouseHelpText"]!;
            var measured = TextRenderer.MeasureText(text, font, new Size(550, int.MaxValue), TextFormatFlags.WordBreak);
            Assert(measured.Height <= 420, $"{language} help is clipped: {measured.Height}px.");
            foreach (var key in new[] { "Macros.BackgroundMouseHelpText", "Tooltips.MacroBackgroundMouse" })
            {
                Assert(legacy.Contains(key) && !Equals(legacy[key], defaults[key]),
                    $"{language} old background-input wording cannot be migrated: {key}.");
            }
        }
    }

    internal static void MouseTimeoutReleasesButton()
    {
        using var target = new TestWindow();
        using var receiver = new SlowMouseReceiver { Bounds = new Rectangle(20, 20, 180, 80) };
        target.Controls.Add(receiver);
        target.Show();
        var macro = Macro(target);
        AddClick(macro, receiver.PointToScreen(new Point(10, 10)));
        Play(macro, target.Handle, "Failed");
        Assert(receiver.Downs == 1, "The timeout test did not deliver a mouse press.");
        Assert(receiver.Ups == 1, "A timed-out background mouse press was not released.");
    }

    internal static void CoordinateOverflow()
    {
        var scale = Type("MacroPlayer").GetMethod("ScaleCoordinate", Members)!;
        int Scale(int value, int origin, int length, int destination, int destinationLength) =>
            (int)scale.Invoke(null, new object[] { value, origin, length, destination, destinationLength })!;
        Assert(Scale(int.MaxValue, -1920, 3840, -1920, 3840) == 1919,
            "A large positive coordinate wrapped to the opposite screen edge.");
        Assert(Scale(int.MinValue, 1920, 1920, 0, 1920) == 0,
            "A large negative coordinate wrapped to the opposite screen edge.");
        Assert(Scale(int.MaxValue, 0, 1920, 0, 3840) == 3839,
            "An out-of-range coordinate overflowed when scaling to a larger window.");
        Assert(Scale(150, 100, 101, 200, 201) == 300,
            "Ordinary coordinate scaling changed.");
    }

    private static object Macro(Form target)
    {
        var macro = New("MacroDefinition");
        var origin = target.PointToScreen(Point.Empty);
        Set(macro, "BackgroundMousePlayback", true);
        Set(macro, "RecordedTargetClientLeft", origin.X);
        Set(macro, "RecordedTargetClientTop", origin.Y);
        Set(macro, "RecordedTargetClientWidth", target.ClientSize.Width);
        Set(macro, "RecordedTargetClientHeight", target.ClientSize.Height);
        return macro;
    }

    private static void AddClick(object macro, Point point)
    {
        foreach (var down in new[] { true, false })
        {
            var item = New("MacroEvent");
            Set(item, "Type", Enum.Parse(Type("MacroEventType"), "MouseButton"));
            Set(item, "Token", "LButton");
            Set(item, "IsDown", down);
            Set(item, "X", point.X);
            Set(item, "Y", point.Y);
            Add(macro, item);
        }
    }

    private static void AddKey(object macro, Keys key, string text = "", bool? down = null)
    {
        foreach (var isDown in down.HasValue ? new[] { down.Value } : new[] { true, false })
        {
            var item = New("MacroEvent");
            Set(item, "Type", Enum.Parse(Type("MacroEventType"), "Key"));
            Set(item, "VirtualKey", (uint)key);
            Set(item, "Token", key.ToString());
            Set(item, "IsDown", isDown);
            Set(item, "Text", isDown ? text : "");
            Add(macro, item);
        }
    }

    private static void Add(object macro, object item)
    {
        var events = (IList)Get(macro, "Events");
        Set(item, "OffsetMicroseconds", events.Count * 1_000L);
        events.Add(item);
    }

    private static void WaitFor(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            Assert(elapsed.Elapsed < TimeSpan.FromSeconds(5), "Playback timed out.");
            Application.DoEvents();
            Thread.Yield();
        }
    }

    private static void Play(object macro, IntPtr target, string status = "Completed")
    {
        using var player = (IDisposable)New("MacroPlayer");
        var task = (Task)Call(player, "PlayAsync", macro, target)!;
        WaitFor(() => task.IsCompleted);
        task.GetAwaiter().GetResult();
        Application.DoEvents();
        Assert(Get(Get(task, "Result"), "Status").ToString() == status,
            $"Expected {status}: {Get(task, "Result")}");
    }

    private sealed class ClosingEdit : TextBox
    {
        internal bool DisposedOnKey;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x100)
            {
                DisposedOnKey = true;
                Dispose();
                return;
            }
            base.WndProc(ref message);
        }
    }

    private sealed class SlowMouseReceiver : Control
    {
        internal int Downs;
        internal int Ups;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x201)
            {
                Downs++;
                Thread.Sleep(160);
                return;
            }
            if (message.Msg == 0x202)
            {
                Ups++;
                return;
            }
            base.WndProc(ref message);
        }
    }

    private sealed class TestWindow : Form
    {
        internal int KeyDowns;
        internal int KeyUps;
        internal int Characters;
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(200, 200);
            ClientSize = new Size(280, 160);
            Text = "Background input check";
        }

        protected override bool ShowWithoutActivation => true;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg is 0x100 or 0x104) KeyDowns++;
            if (message.Msg is 0x101 or 0x105) KeyUps++;
            if (message.Msg is 0x102 or 0x106) Characters++;
            // Observe messages without dispatching system shortcuts in a test window.
            if (message.Msg is >= 0x100 and <= 0x106) return;
            base.WndProc(ref message);
        }
    }

    private sealed class InjectionProbe : IDisposable
    {
        private delegate IntPtr Hook(int code, IntPtr message, IntPtr data);
        private readonly Hook _keyboard;
        private readonly Hook _mouse;
        private readonly IntPtr _keyboardHandle;
        private readonly IntPtr _mouseHandle;
        internal int Count;

        internal InjectionProbe()
        {
            _keyboard = (code, message, data) => Observe(code, message, data, 16);
            _mouse = (code, message, data) => Observe(code, message, data, 24);
            var module = GetModuleHandle(null);
            _keyboardHandle = SetWindowsHookEx(13, _keyboard, module, 0);
            _mouseHandle = SetWindowsHookEx(14, _mouse, module, 0);
            Assert(_keyboardHandle != IntPtr.Zero && _mouseHandle != IntPtr.Zero, "Cannot monitor input injection.");
        }

        private IntPtr Observe(int code, IntPtr message, IntPtr data, int extraInfoOffset)
        {
            if (code >= 0 && unchecked((nuint)Marshal.ReadIntPtr(data, extraInfoOffset)) == (nuint)0xAC10C11C)
            {
                Count++;
                return (IntPtr)1; // Block regressions from typing into the user's real foreground app.
            }
            return CallNextHookEx(IntPtr.Zero, code, message, data);
        }

        public void Dispose()
        {
            if (_keyboardHandle != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHandle);
            if (_mouseHandle != IntPtr.Zero) UnhookWindowsHookEx(_mouseHandle);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, Hook callback, IntPtr module, uint thread);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? module);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private static Type Type(string name) => App.GetType($"KofgeClicker.{name}", true)!;
    private static object New(string name) => Activator.CreateInstance(Type(name), true)!;
    private static object? Call(object target, string method, params object?[] args) =>
        target.GetType().GetMethod(method, Members)!.Invoke(target, args);
    private static object Get(object target, string property) =>
        target.GetType().GetProperty(property, Members)!.GetValue(target)!;
    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property, Members)!.SetValue(target, value);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
