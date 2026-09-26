namespace KofgeClicker;

internal sealed class MacroBackgroundHelpDialog : Form
{
    private MacroBackgroundHelpDialog()
    {
        Text = LocalizationService.Get("Macros.BackgroundMouseHelpTitle");
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(680, 520);
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
            Left = 32,
            Top = 24,
            Width = 616,
            Height = 34,
            AutoSize = false,
            Text = Text,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var divider = new Panel
        {
            Left = 32,
            Top = 70,
            Width = 616,
            Height = 1,
            BackColor = UiTheme.BorderSoft
        };

        var infoMark = new Label
        {
            Left = 34,
            Top = 94,
            Width = 46,
            Height = 46,
            AutoSize = false,
            Text = "?",
            BackColor = UiTheme.AccentSecondary,
            ForeColor = Color.White,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 21f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var messageLabel = new Label
        {
            Left = 98,
            Top = 90,
            Width = 550,
            Height = 340,
            AutoSize = false,
            Text = LocalizationService.Get("Macros.BackgroundMouseHelpText"),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 12.5f, FontStyle.Regular),
            TextAlign = ContentAlignment.TopLeft
        };

        var closeButton = new AccentButton
        {
            Left = 252,
            Top = 458,
            Width = 176,
            Height = 40,
            Text = LocalizationService.Get("WhatsNew.Continue"),
            Primary = true,
            DialogResult = DialogResult.OK
        };

        shell.Controls.Add(titleLabel);
        shell.Controls.Add(divider);
        shell.Controls.Add(infoMark);
        shell.Controls.Add(messageLabel);
        shell.Controls.Add(closeButton);
        Controls.Add(shell);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.OK;
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

    internal static void ShowFor(IWin32Window owner)
    {
        using var dialog = new MacroBackgroundHelpDialog();
        dialog.ShowDialog(owner);
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
