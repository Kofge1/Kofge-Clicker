namespace KofgeClicker;

internal enum ReviewPromptChoice
{
    Later,
    LeaveReview,
    NeverShow
}

internal sealed class ReviewPromptDialog : Form
{
    internal ReviewPromptDialog()
    {
        Text = LocalizationService.Get("ReviewPrompt.Title");
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(680, 286);
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
            Left = 30,
            Top = 24,
            Width = 620,
            Height = 34,
            AutoSize = false,
            Text = LocalizationService.Get("ReviewPrompt.Title"),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var divider = new Panel
        {
            Left = 30,
            Top = 68,
            Width = 620,
            Height = 1,
            BackColor = UiTheme.BorderSoft
        };

        var infoMark = new Label
        {
            Left = 32,
            Top = 94,
            Width = 42,
            Height = 42,
            AutoSize = false,
            Text = "i",
            BackColor = UiTheme.AccentSecondary,
            ForeColor = Color.White,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var messageLabel = new Label
        {
            Left = 92,
            Top = 88,
            Width = 556,
            Height = 96,
            AutoSize = false,
            Text = LocalizationService.Get("ReviewPrompt.Text"),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 13f),
            TextAlign = ContentAlignment.TopLeft
        };

        var leaveReviewButton = CreateButton(
            30,
            190,
            LocalizationService.Get("ReviewPrompt.LeaveReview"),
            primary: true,
            ReviewPromptChoice.LeaveReview);
        var laterButton = CreateButton(
            236,
            150,
            LocalizationService.Get("ReviewPrompt.Later"),
            primary: false,
            ReviewPromptChoice.Later);
        var neverButton = CreateButton(
            402,
            248,
            LocalizationService.Get("ReviewPrompt.Never"),
            primary: false,
            ReviewPromptChoice.NeverShow);

        shell.Controls.Add(titleLabel);
        shell.Controls.Add(divider);
        shell.Controls.Add(infoMark);
        shell.Controls.Add(messageLabel);
        shell.Controls.Add(leaveReviewButton);
        shell.Controls.Add(laterButton);
        shell.Controls.Add(neverButton);
        Controls.Add(shell);

        CancelButton = laterButton;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Choice = ReviewPromptChoice.Later;
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

    internal ReviewPromptChoice Choice { get; private set; } = ReviewPromptChoice.Later;

    private AccentButton CreateButton(
        int left,
        int width,
        string text,
        bool primary,
        ReviewPromptChoice choice)
    {
        var button = new AccentButton
        {
            Left = left,
            Top = 220,
            Width = width,
            Height = 40,
            Text = text,
            Primary = primary
        };
        button.Click += (_, _) =>
        {
            Choice = choice;
            Close();
        };
        return button;
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
