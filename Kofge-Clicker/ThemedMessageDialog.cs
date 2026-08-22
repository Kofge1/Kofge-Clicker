namespace KofgeClicker;

public sealed class ThemedMessageDialog : Form
{
    private ThemedMessageDialog(string title, string message, bool confirmation)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(560, 260);
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextPrimary;
        KeyPreview = true;

        var shell = new RoundedPanel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = ClientSize.Height,
            Radius = 22,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = Color.FromArgb(83, 103, 143),
            DrawShadow = false,
            UseAntialiasedEdges = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var titleLabel = new Label
        {
            Left = 28,
            Top = 22,
            Width = 504,
            Height = 30,
            AutoSize = false,
            Text = title,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var divider = new Panel
        {
            Left = 28,
            Top = 62,
            Width = 504,
            Height = 1,
            BackColor = UiTheme.BorderSoft
        };

        var warningMark = new Label
        {
            Left = 30,
            Top = 84,
            Width = 42,
            Height = 42,
            AutoSize = false,
            Text = "!",
            BackColor = UiTheme.AccentSecondary,
            ForeColor = Color.White,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var messageLabel = new Label
        {
            Left = 90,
            Top = 78,
            Width = 442,
            Height = 102,
            AutoSize = false,
            Text = message,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 13f, FontStyle.Regular),
            TextAlign = ContentAlignment.TopLeft
        };

        var primaryButton = new AccentButton
        {
            Left = confirmation ? 118 : 202,
            Top = 202,
            Width = 156,
            Height = 38,
            Text = confirmation
                ? LocalizationService.Get("Common.Yes")
                : LocalizationService.Get("Common.Ok"),
            Primary = true,
            DialogResult = confirmation ? DialogResult.Yes : DialogResult.OK
        };

        shell.Controls.Add(titleLabel);
        shell.Controls.Add(divider);
        shell.Controls.Add(warningMark);
        shell.Controls.Add(messageLabel);
        shell.Controls.Add(primaryButton);
        AccentButton? secondaryButton = null;
        if (confirmation)
        {
            secondaryButton = new AccentButton
            {
                Left = 286,
                Top = 202,
                Width = 156,
                Height = 38,
                Text = LocalizationService.Get("Common.No"),
                Primary = false,
                DialogResult = DialogResult.No
            };
            shell.Controls.Add(secondaryButton);
        }

        Controls.Add(shell);

        AcceptButton = primaryButton;
        CancelButton = secondaryButton ?? primaryButton;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = confirmation ? DialogResult.No : DialogResult.OK;
                Close();
            }
        };
        Shown += (_, _) =>
        {
            WindowPlacement.ClampToWorkingArea(this);
            ApplyRoundedRegion();
        };
        SizeChanged += (_, _) => ApplyRoundedRegion();
    }

    public static void Show(IWin32Window owner, string title, string message)
    {
        using var dialog = new ThemedMessageDialog(title, message, confirmation: false);
        ShowWithOwner(dialog, owner);
    }

    public static void Show(string title, string message)
    {
        using var dialog = new ThemedMessageDialog(title, message, confirmation: false);
        dialog.ShowDialog();
    }

    public static bool Confirm(IWin32Window owner, string title, string message)
    {
        using var dialog = new ThemedMessageDialog(title, message, confirmation: true);
        return ShowWithOwner(dialog, owner) == DialogResult.Yes;
    }

    private static DialogResult ShowWithOwner(Form dialog, IWin32Window owner)
    {
        if (owner is Form ownerForm
            && (!ownerForm.Visible || ownerForm.WindowState == FormWindowState.Minimized))
        {
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.TopMost = true;
            return dialog.ShowDialog();
        }

        return dialog.ShowDialog(owner);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(UiTheme.AppBackground);
    }

    private void ApplyRoundedRegion()
    {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
        {
            return;
        }

        using var path = UiTheme.CreateRoundedRectPath(
            new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
            22f);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}
