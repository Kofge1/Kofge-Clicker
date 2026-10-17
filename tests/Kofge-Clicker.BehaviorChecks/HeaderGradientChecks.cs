using System.Collections;
using System.Diagnostics;
using System.Reflection;
using KofgeClicker;

internal static class HeaderGradientChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type ClockType = typeof(AccentButton).Assembly.GetType("KofgeClicker.ButtonAnimationClock")!;

    internal static void DirectionsAndImmediatePages()
    {
        using var window = new TestWindow();
        using var header = new Panel { Bounds = new Rectangle(0, 0, 940, 60), BackColor = UiTheme.CardOuter };
        using var host = new AtomicPageHost { Bounds = new Rectangle(0, 70, 940, 260) };
        window.Controls.Add(header);
        window.Controls.Add(host);
        var buttons = new List<AccentButton>();
        var names = new[] { "Clicker", "Pattern", "Macros", "Mouse", "Keys", "Profiles", "Options" };
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var button = CreateButton();
            button.Text = names[i];
            button.Bounds = new Rectangle(10 + i * 130, 8, 120, 48);
            button.Primary = i == 0;
            button.Click += (_, _) => host.SelectedIndex = index;
            buttons.Add(button);
            header.Controls.Add(button);
            host.AddPage(new Panel { BackColor = UiTheme.CardInner, Text = names[i] });
        }
        var apply = typeof(MainForm).GetMethod("ApplyTabHeaderSelection", Members)!;
        host.SelectedIndexChanged += (_, _) => apply.Invoke(null, [buttons, host.SelectedIndex]);
        window.Show();
        Application.DoEvents();
        foreach (var button in buttons)
            Field(button, "_interactionAnimation")!.GetType().GetMethod("Reset", Members)!
                .Invoke(Field(button, "_interactionAnimation"), [button.Primary]);

        using var preview = new Bitmap(header.Width, (header.Height + 24) * 3);
        using var canvas = Graphics.FromImage(preview);
        canvas.Clear(UiTheme.CardOuter);
        SaveRow(0, "Initial");
        foreach (var next in new[] { 6, 0, 1, 5, 2 })
        {
            var previous = host.SelectedIndex;
            using var beforeOld = Capture(buttons[previous]);
            using var beforeNew = Capture(buttons[next]);
            host.SelectedIndex = next;
            Assert(buttons.Count(button => button.Primary) == 1 && buttons[next].Primary,
                "Header selection was delayed or selected multiple tabs.");
            Assert(host.Pages.Select((page, index) => page.Visible == (index == next)).All(value => value),
                "The gradient delayed page visibility changes.");
            using (var current = Capture(buttons[previous])) EqualPixels(beforeOld, current);
            using (var current = Capture(buttons[next])) EqualPixels(beforeNew, current);
            AdvanceTo(buttons, 75);
            foreach (var index in new[] { previous, next })
            {
                var button = buttons[index];
                using var frame = Capture(button);
                var changedX = button.Width * (next > previous ? 1 : 3) / 4;
                var unchangedX = button.Width * (next > previous ? 3 : 1) / 4;
                var target = button.Primary ? UiTheme.AccentSecondary : UiTheme.Surface;
                var source = button.Primary ? UiTheme.Surface : UiTheme.AccentSecondary;
                Assert(frame.GetPixel(changedX, 5).ToArgb() == target.ToArgb()
                    && frame.GetPixel(unchangedX, 5).ToArgb() == source.ToArgb(),
                    "The header wave did not follow the selected tab's direction on both buttons.");
            }
            if (next == 6) SaveRow(1, "Moving right: 75 ms");
            if (next == 0) SaveRow(2, "Moving left: 75 ms");
            using var beforeRepeated = Capture(buttons[next]);
            apply.Invoke(null, [buttons, next]);
            using (var current = Capture(buttons[next])) EqualPixels(beforeRepeated, current);
            AdvanceTo(buttons, 150);
            Tick();
            AssertIdle();
        }
        preview.Save(Path.Combine(Path.GetTempPath(), "Kofge-Header-Gradient-preview.png"));

        host.SelectedIndex = 6;
        AdvanceTo(buttons, 75);
        var beforeReverse = buttons.Select(Capture).ToArray();
        try
        {
            host.SelectedIndex = 0;
            for (var i = 0; i < buttons.Count; i++)
            {
                using var current = Capture(buttons[i]);
                EqualPixels(beforeReverse[i], current);
            }
        }
        finally
        {
            foreach (var image in beforeReverse) image.Dispose();
        }
        Assert((bool)Field(Gradient(buttons[0]), "_sweepFromRight")!
            && (bool)Field(Gradient(buttons[6]), "_sweepFromRight")!,
            "Rapid reversal lost the last selected tab's direction.");
        AdvanceTo(buttons, 150);
        Tick();
        var clicks = 0;
        buttons[4].Click += (_, _) => clicks++;
        buttons[4].PerformClick();
        Assert(clicks == 1 && host.SelectedIndex == 4, "A tab click was delayed or duplicated.");
        window.Hide();
        Tick();
        AssertIdle();

        void SaveRow(int row, string title)
        {
            var y = row * (header.Height + 24);
            TextRenderer.DrawText(canvas, title, SystemFonts.MessageBoxFont,
                new Point(10, y + 3), UiTheme.TextPrimary);
            using var image = Capture(header);
            canvas.DrawImageUnscaled(image, 0, y + 24);
        }
    }

    internal static void LifecycleAndSettledParity()
    {
        using var window = new TestWindow();
        foreach (var interactions in new[] { false, true })
        foreach (var ring in new[] { false, true })
        foreach (var primary in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        {
            using var legacy = CreateButton();
            using var gradient = CreateButton();
            legacy.AnimatePrimaryGradient = false;
            legacy.AnimateInteractions = gradient.AnimateInteractions = interactions;
            legacy.UseFilledBorderRing = gradient.UseFilledBorderRing = ring;
            legacy.Primary = gradient.Primary = primary;
            legacy.Enabled = gradient.Enabled = enabled;
            window.Controls.Add(legacy);
            window.Controls.Add(gradient);
            foreach (var stage in new[] { "idle", "hover", "press" })
            {
                if (stage == "hover")
                {
                    Invoke(legacy, "OnMouseEnter", EventArgs.Empty);
                    Invoke(gradient, "OnMouseEnter", EventArgs.Empty);
                }
                if (stage == "press")
                {
                    Invoke(legacy, "OnKeyDown", new KeyEventArgs(Keys.Space));
                    Invoke(gradient, "OnKeyDown", new KeyEventArgs(Keys.Space));
                }
                using var expected = Capture(legacy);
                using var actual = Capture(gradient);
                EqualPixels(expected, actual);
            }
        }
        AssertIdle();
        using var button = CreateButton();
        window.Controls.Add(button);
        window.Show();
        Application.DoEvents();
        button.Primary = true;
        Thread.Sleep(220);
        Assert(Amount(button) == 0f, "Page work consumed the transition before its first paint.");
        AdvanceTo([button], 75);
        Assert(Amount(button) > 0f && Amount(button) < 1f, "The visible header transition did not advance.");
        Assert(!((System.Windows.Forms.Timer)Field(button, "_primaryTimer")!).Enabled,
            "The gradient started the legacy per-button primary timer.");
        button.Enabled = false;
        Assert(Amount(button) == 1f, "Disabling a header button did not settle its state.");
        AssertIdle();
        button.Enabled = true;
        button.Primary = false;
        button.Hide();
        Assert(Amount(button) == 0f, "Hiding a header button did not settle its state.");
        AssertIdle();
        button.Show();
        button.Primary = true;
        Invoke(button, "RecreateHandle");
        Assert(Amount(button) == 1f, "Recreating a header handle left a partial gradient.");
        AssertIdle();
        button.Primary = false;
        button.AnimatePrimaryGradient = false;
        AssertIdle();
        button.AnimatePrimaryGradient = true;
        button.Primary = true;
        button.Dispose();
        AssertIdle();
        using var ordinary = new AccentButton();
        Assert(!ordinary.AnimatePrimaryGradient, "Header gradients were enabled on ordinary buttons.");
    }

    private static AccentButton CreateButton() => new()
    {
        Text = "Tab", Bounds = new Rectangle(10, 10, 150, 48), AnimateInteractions = true,
        AnimatePrimaryChanges = true, AnimatePrimaryGradient = true, UseFilledBorderRing = true
    };

    private static void AdvanceTo(IEnumerable<AccentButton> buttons, int milliseconds)
    {
        Tick();
        foreach (var button in buttons)
        {
            using (Capture(button)) { }
            var animation = Gradient(button);
            var startedAt = (long)Field(animation, "_startedAt")!;
            Invoke(animation, "Advance", startedAt + Stopwatch.Frequency * milliseconds / 1000);
        }
    }
    private static object Gradient(AccentButton button) => Field(button, "_primaryGradient")!;
    private static float Amount(AccentButton button) => (float)Gradient(button).GetType().GetProperty("Amount", Members)!.GetValue(Gradient(button))!;
    private static object Clock() => ClockType.GetField("_current", Members)!.GetValue(null)!;
    private static void Tick() => Invoke(Clock(), "OnTick", null!, EventArgs.Empty);
    private static object? Field(object owner, string name) => owner.GetType().GetField(name, Members)!.GetValue(owner);
    private static void Invoke(object owner, string name, params object[] arguments) =>
        owner.GetType().GetMethod(name, Members)!.Invoke(owner, arguments);
    private static Bitmap Capture(Control control)
    {
        var image = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(image, control.ClientRectangle);
        return image;
    }
    private static void EqualPixels(Bitmap expected, Bitmap actual)
    {
        for (var y = 0; y < expected.Height; y++)
            for (var x = 0; x < expected.Width; x++)
                Assert(expected.GetPixel(x, y) == actual.GetPixel(x, y), $"Header pixels changed at {x},{y}.");
    }
    private static void AssertIdle()
    {
        var active = Field(Clock(), "_active")!;
        Assert((int)active.GetType().GetProperty("Count")!.GetValue(active)! == 0,
            "A header animation stayed registered while idle.");
        Assert(!((System.Windows.Forms.Timer)Field(Clock(), "_timer")!).Enabled, "The shared clock ran while idle.");
        Assert(((ICollection)Field(Clock(), "_frame")!).Count == 0, "The frame buffer retained header controls.");
    }
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private sealed class TestWindow : Form
    {
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(80, 120);
            ClientSize = new Size(940, 340);
            BackColor = UiTheme.CardOuter;
        }
        protected override bool ShowWithoutActivation => true;
    }
}
