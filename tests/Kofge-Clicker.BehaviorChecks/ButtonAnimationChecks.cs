using System.Diagnostics;
using System.Reflection;
using KofgeClicker;

internal static class ButtonAnimationChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;

    internal static void HoverRetargeting()
    {
        using var host = new TestWindow();
        using var button = AddButton(host);
        host.Show();
        Application.DoEvents();
        var idleColor = ReadFill(button);
        Invoke(button, "OnMouseEnter", EventArgs.Empty);
        Assert(Amount(button, "HoverAmount") < 1f, "Hover changed instantly.");
        Pump(55);
        var midway = Amount(button, "HoverAmount");
        Assert(midway > 0f && midway < 1f, "Hover did not interpolate.");
        Invoke(button, "OnMouseLeave", EventArgs.Empty);
        Assert(Amount(button, "HoverAmount") >= midway - 0.001f, "Retargeting reset the current color.");
        Pump(220);
        Assert(Amount(button, "HoverAmount") == 0f, "Hover did not finish at zero.");
        AssertClockStopped();
        Invoke(button, "OnMouseEnter", EventArgs.Empty);
        Pump(180);
        Assert(ReadFill(button) != idleColor, "Hover did not change the rendered fill.");
        AssertClockStopped();
    }

    internal static void PressAndClick()
    {
        using var host = new TestWindow();
        using var button = AddButton(host);
        host.Show();
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        Invoke(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Assert(Amount(button, "PressAmount") == 1f, "Press feedback was delayed.");
        // Lost focus must not leave keyboard-press feedback stuck.
        button.Capture = false;
        Invoke(button, "OnLostFocus", EventArgs.Empty);
        Pump(180);
        Assert(Amount(button, "PressAmount") == 0f, "Release feedback became stuck.");
        button.PerformClick();
        Assert(clicks == 1, "Animation changed or delayed Click delivery.");
        AssertClockStopped();
    }

    internal static void SharedClockAndLifecycle()
    {
        using var host = new TestWindow();
        using var first = AddButton(host);
        using var second = AddButton(host);
        second.Left = 170;
        host.Show();
        Invoke(first, "OnMouseEnter", EventArgs.Empty);
        var clock = Clock();
        Invoke(second, "OnMouseEnter", EventArgs.Empty);
        Assert(ReferenceEquals(clock, Clock()) && ActiveCount() == 2,
            "Buttons did not share one animation clock.");
        first.Enabled = false;
        Assert(Amount(first, "HoverAmount") == 0f && ActiveCount() == 1,
            "Disabling a button did not cancel its animation.");
        host.Hide();
        // Parent visibility can settle after the child's notification; the next frame cancels it.
        Pump(60);
        AssertClockStopped("after hiding the form");
        host.Show();
        Invoke(second, "OnMouseEnter", EventArgs.Empty);
        second.Dispose();
        AssertClockStopped("after disposing the active button");
    }

    internal static void PrimaryAndOptOut()
    {
        using var host = new TestWindow();
        using var button = AddButton(host);
        button.AnimatePrimaryChanges = true;
        host.Show();
        button.Primary = true;
        Assert(Amount(button, "PrimaryAmount") < 1f, "Primary state changed instantly.");
        Pump(60);
        button.Primary = false;
        Pump(220);
        Assert(Amount(button, "PrimaryAmount") == 0f, "Primary transition did not finish.");
        Invoke(button, "OnMouseEnter", EventArgs.Empty);
        button.AnimateInteractions = false;
        AssertClockStopped();
        button.AnimatePrimaryChanges = false;
        button.Primary = true;
        Assert((float)Field(button, "_primaryAmount")! == 1f, "Opt-out damaged legacy button rendering.");
        using var legacy = new AccentButton();
        Assert(!legacy.AnimateInteractions, "Prototype was enabled for all buttons.");
    }

    internal static void ReusableFrameBufferAndReentrancy()
    {
        using var host = new TestWindow();
        using var first = AddButton(host);
        using var second = AddButton(host);
        second.Left = 170;
        host.Show();
        Application.DoEvents();
        Invoke(first, "OnMouseEnter", EventArgs.Empty);
        Invoke(second, "OnMouseEnter", EventArgs.Empty);
        var clock = Clock();
        var tick = clock.GetType().GetMethod("OnTick", Members)!;
        var buffer = Field(clock, "_frame");
        var frameId = (long)Field(clock, "_frameId")!;
        var attempted = false;
        InvalidateEventHandler nested = (_, _) =>
        {
            if (attempted) return;
            attempted = true;
            tick.Invoke(clock, [null, EventArgs.Empty]);
            second.Enabled = false;
        };
        first.Invalidated += nested;
        tick.Invoke(clock, [null, EventArgs.Empty]);
        first.Invalidated -= nested;
        Assert(attempted, "The reentrancy path was not exercised.");
        Assert((long)Field(clock, "_frameId")! == frameId + 1, "A nested tick advanced a second frame.");
        Assert(ReferenceEquals(buffer, Field(clock, "_frame")), "The reusable frame buffer was replaced.");
        Assert(((System.Collections.ICollection)buffer!).Count == 0, "The frame buffer retained buttons.");
        Assert(ActiveCount() == 1, "Changing active buttons during a frame damaged registration.");
        first.Enabled = false;
        AssertClockStopped();
    }

    private static AccentButton AddButton(Form host)
    {
        var button = new AccentButton
        {
            Text = "Preview",
            Location = new Point(10, 10),
            Size = new Size(150, 42),
            AnimateInteractions = true
        };
        host.Controls.Add(button);
        return button;
    }

    private static Color ReadFill(AccentButton button)
    {
        using var image = new Bitmap(button.Width, button.Height);
        button.DrawToBitmap(image, button.ClientRectangle);
        return image.GetPixel(button.Width / 2, 5);
    }

    private static float Amount(AccentButton button, string property)
    {
        var animation = Field(button, "_interactionAnimation")!;
        return (float)animation.GetType().GetProperty(property, Members)!.GetValue(animation)!;
    }

    private static object? Field(object target, string field) =>
        target.GetType().GetField(field, Members)!.GetValue(target);

    private static object Clock() => typeof(AccentButton).Assembly.GetType("KofgeClicker.ButtonAnimationClock")!
        .GetField("_current", Members)!.GetValue(null)!;

    private static int ActiveCount() => (int)Field(Clock(), "_active")!.GetType()
        .GetProperty("Count")!.GetValue(Field(Clock(), "_active"))!;

    private static void AssertClockStopped(string stage = "after finishing")
    {
        Assert(ActiveCount() == 0, $"{ActiveCount()} buttons remained registered {stage}.");
        Assert(!((System.Windows.Forms.Timer)Field(Clock(), "_timer")!).Enabled,
            "The animation timer kept running while idle.");
        Assert(((System.Collections.ICollection)Field(Clock(), "_frame")!).Count == 0,
            "The frame buffer retained button references after a frame.");
    }

    private static void Invoke(Control target, string method, object argument) =>
        target.GetType().GetMethod(method, Members)!.Invoke(target, [argument]);

    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        Application.DoEvents();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestWindow : Form
    {
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 120);
            ClientSize = new Size(340, 90);
        }

        protected override bool ShowWithoutActivation => true;
    }
}
