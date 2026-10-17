using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using KofgeClicker;

internal static class TooltipChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic |
        BindingFlags.Public;

    internal static void RefusesMouseActivation()
    {
        using var popup = (Form)Activator.CreateInstance(
            typeof(AccentButton).Assembly.GetType("KofgeClicker.HoverTooltipWindow")!, true)!;
        var result = SendMessage(popup.Handle, 0x0021, IntPtr.Zero,
            (nint)((0x0201 << 16) | 1));
        Assert(result == (nint)3, $"Tooltip accepted mouse activation: {result}.");
    }

    internal static void OwnerAndFocus()
    {
        using var owner = new TestWindow();
        using var input = new TextBox { Left = 8, Top = 8, Width = 180 };
        owner.Controls.Add(input);
        owner.Show();
        SetActiveWindow(owner.Handle);
        input.Focus();
        var active = GetActiveWindow();
        var focus = GetFocus();
        Assert(active == owner.Handle && focus == input.Handle, "Cannot establish test-window focus.");

        var serviceType = typeof(AccentButton).Assembly.GetType("KofgeClicker.HoverTooltipService")!;
        using var service = (IDisposable)Activator.CreateInstance(serviceType, Members, null, [owner], null)!;
        Assert(GetActiveWindow() == active && GetFocus() == focus,
            "Constructing the tooltip service changed focus.");
        var show = serviceType.GetMethod("ShowPopup", Members)!;
        var hide = serviceType.GetMethod("HideImmediate", Members)!;
        var popup = (Form)serviceType.GetField("_popup", Members)!.GetValue(service)!;
        for (var index = 0; index < 3; index++)
        {
            show.Invoke(service, [input, "Tooltip focus regression check"]);
            Assert(GetActiveWindow() == active && GetFocus() == focus,
                $"Showing tooltip changed focus: active={GetActiveWindow()}, focus={GetFocus()}, owner={owner.Handle}, popup={popup.Handle}, expectedFocus={focus}.");
            Pump(300);
            Assert(popup.Visible, "Tooltip did not become visible.");
            Assert(GetActiveWindow() == active && GetFocus() == focus,
                $"Fading tooltip changed focus: active={GetActiveWindow()}, focus={GetFocus()}, owner={owner.Handle}, popup={popup.Handle}, expectedFocus={focus}.");
            Assert(ReferenceEquals(popup.Owner, owner), "Tooltip is not attached to its owner.");
            Assert(GetWindow(popup.Handle, 4) == owner.Handle, "Native tooltip owner is incorrect.");
            hide.Invoke(service, null);
            Assert(!popup.Visible, "Tooltip did not hide.");
            Assert(GetActiveWindow() == active && GetFocus() == focus,
                "Hiding the tooltip changed focus.");
        }
        show.Invoke(service, [input, "Hide with owner"]);
        owner.Hide();
        Pump(30);
        Assert(!popup.Visible, "Tooltip remained visible after hiding its owner.");
    }

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
            ClientSize = new Size(260, 90);
        }
        protected override bool ShowWithoutActivation => true;
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(IntPtr window, int message, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr window);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);
}
