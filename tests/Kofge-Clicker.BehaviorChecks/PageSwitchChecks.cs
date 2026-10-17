using System.Diagnostics;
using System.Runtime.InteropServices;
using KofgeClicker;

internal static class PageSwitchChecks
{
    internal static void CompletePageRedraw()
    {
        using var window = new TestWindow();
        using var shell = new RoundedPanel
        {
            Dock = DockStyle.Fill, UseAntialiasedEdges = true, CacheBackgroundDrawing = true
        };
        using var host = new AtomicPageHost { Bounds = new Rectangle(10, 70, 700, 330) };
        using var header = new AccentButton
        {
            Bounds = new Rectangle(15, 10, 130, 45), Text = "Tab", AnimateInteractions = true,
            AnimatePrimaryChanges = true
        };
        window.Controls.Add(shell);
        shell.Controls.Add(host);
        shell.Controls.Add(header);
        var contentPaints = new int[3];
        for (var pageIndex = 0; pageIndex < 3; pageIndex++)
        {
            var capturedIndex = pageIndex;
            var page = new Panel { BackColor = UiTheme.CardOuter };
            var card = new RoundedPanel
            {
                Bounds = new Rectangle(20, 20, 650, 290), UseAntialiasedEdges = true,
                CacheBackgroundDrawing = true
            };
            for (var index = 0; index < 24; index++)
            {
                var label = SurfaceLabelChecks.Create();
                label.Bounds = new Rectangle(10 + index % 3 * 210, 10 + index / 3 * 33, 200, 28);
                label.BackColor = Color.Transparent;
                label.Text = $"Page {pageIndex}, control {index}";
                label.Paint += (_, _) => contentPaints[capturedIndex]++;
                card.Controls.Add(label);
            }
            page.Controls.Add(card);
            host.AddPage(page);
        }
        window.Show();
        Pump(100);
        using var headerPaints = new PaintProbe(header);
        using var hostMessages = new PaintProbe(host);
        var changes = 0;
        host.SelectedIndexChanged += (_, _) => changes++;
        for (var index = 0; index < 30; index++)
        {
            var next = (index + 1) % host.PageCount;
            var before = contentPaints[next];
            host.SelectedIndex = next;
            Application.DoEvents();
            Assert(contentPaints[next] > before, "Selected page content was not painted.");
            Assert(ReadPixel(host.Pages[next].Controls[0], 320, 275).ToArgb() == UiTheme.CardInner.ToArgb(),
                "Selected page card has an incorrect rendered background.");
            Assert(ReadPixel(host.Pages[next], 690, 320).ToArgb() == UiTheme.CardOuter.ToArgb(),
                "Selected page has an incorrect rendered background.");
            for (var page = 0; page < host.PageCount; page++)
            {
                Assert(host.Pages[page].Visible == (page == host.SelectedIndex), "Wrong page visibility.");
                if (page == host.SelectedIndex)
                    Assert(IsWindowVisible(host.Pages[page].Handle), "Selected page is hidden at the native window level.");
            }
        }
        Console.WriteLine($"INFO page-redraw: 30 switches verified; header paints: {headerPaints.Count}");
        Assert(changes == 30, "Page change events were lost or duplicated.");
        Assert(hostMessages.RedrawBlocks == 0, "Redraw was disabled on the page host while changing child windows.");
        host.SelectedIndex = host.SelectedIndex;
        Assert(changes == 30, "Selecting the current page raised an extra event.");
        var paintsBeforeAnimation = headerPaints.Count;
        for (var index = 0; index < 12; index++)
        {
            header.Primary = !header.Primary;
            host.SelectedIndex = (host.SelectedIndex + 1) % host.PageCount;
            Pump(25);
        }
        Pump(190);
        Assert(headerPaints.Count > paintsBeforeAnimation, "Animation did not paint during page switching.");
        host.Hide();
        host.SelectedIndex = (host.SelectedIndex + 1) % host.PageCount;
        Assert(!host.Visible, "Switching pages showed an explicitly hidden page host.");
        host.Show();
        window.Hide();
        host.SelectedIndex = (host.SelectedIndex + 1) % host.PageCount;
        Assert(!window.Visible && !host.Visible, "Switching a hidden page showed the window.");
        window.Show();
        Pump(50);
        Assert(host.Pages[host.SelectedIndex].Visible, "Selected page did not return after showing the window.");
        var primary = header.Primary;
        header.Primary = !primary;
        Pump(190);
        Assert(headerPaints.Count > 0, "Tab animations stopped painting after switching pages.");
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    private static Color ReadPixel(Control control, int x, int y)
    {
        using var image = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(image, control.ClientRectangle);
        return image.GetPixel(x, y);
    }

    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
    }

    private sealed class TestWindow : Form
    {
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 120);
            ClientSize = new Size(730, 420);
        }
        protected override bool ShowWithoutActivation => true;
    }

    private sealed class PaintProbe : NativeWindow, IDisposable
    {
        internal int Count { get; private set; }
        internal int RedrawBlocks { get; private set; }
        internal PaintProbe(Control control) => AssignHandle(control.Handle);
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x000F) Count++;
            if (m.Msg == 0x000B && m.WParam == IntPtr.Zero) RedrawBlocks++;
            base.WndProc(ref m);
        }
        public void Dispose() => ReleaseHandle();
    }
}
