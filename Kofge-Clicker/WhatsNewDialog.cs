namespace KofgeClicker;

internal sealed record WhatsNewItem(string Title, string Description);

internal sealed class WhatsNewDialog : Form
{
    private const int CornerRadius = 22;

    internal WhatsNewDialog(string version, IReadOnlyList<WhatsNewItem> items)
    {
        Text = LocalizationService.Get("WhatsNew.Title", version);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(700, 520);
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextPrimary;
        KeyPreview = true;

        var shell = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = CornerRadius,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = Color.FromArgb(83, 103, 143),
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        var titleLabel = new Label
        {
            Left = 30,
            Top = 24,
            Width = 500,
            Height = 32,
            AutoSize = false,
            Text = LocalizationService.Get("WhatsNew.Title", version),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 19f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var versionLabel = new Label
        {
            Left = 574,
            Top = 26,
            Width = 96,
            Height = 28,
            AutoSize = false,
            Text = version,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.AccentBorder,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };

        var subtitleLabel = new Label
        {
            Left = 30,
            Top = 62,
            Width = 640,
            Height = 24,
            AutoSize = false,
            Text = LocalizationService.Get("WhatsNew.Subtitle"),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 12.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var content = new RoundedPanel
        {
            Left = 28,
            Top = 102,
            Width = 644,
            Height = 330,
            Radius = 17,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        for (var index = 0; index < items.Count; index++)
        {
            AddItem(content, items[index], index);
        }

        var continueButton = new AccentButton
        {
            Left = 254,
            Top = 460,
            Width = 192,
            Height = 40,
            Text = LocalizationService.Get("WhatsNew.Continue"),
            Primary = true,
            DialogResult = DialogResult.OK
        };

        shell.Controls.Add(titleLabel);
        shell.Controls.Add(versionLabel);
        shell.Controls.Add(subtitleLabel);
        shell.Controls.Add(content);
        shell.Controls.Add(continueButton);
        Controls.Add(shell);

        AcceptButton = continueButton;
        CancelButton = continueButton;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        Shown += (_, _) => ApplyRoundedRegion();
        SizeChanged += (_, _) => ApplyRoundedRegion();
    }

    private static void AddItem(Control parent, WhatsNewItem item, int index)
    {
        const int rowHeight = 64;
        var top = 5 + (index * rowHeight);

        if (index > 0)
        {
            parent.Controls.Add(new Panel
            {
                Left = 62,
                Top = top,
                Width = 552,
                Height = 1,
                BackColor = Color.FromArgb(52, 62, 91)
            });
        }

        var numberLabel = new Label
        {
            Left = 18,
            Top = top + 12,
            Width = 32,
            Height = 30,
            AutoSize = false,
            Text = (index + 1).ToString("00"),
            BackColor = Color.Transparent,
            ForeColor = UiTheme.AccentBorder,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var titleLabel = new Label
        {
            Left = 68,
            Top = top + 7,
            Width = 548,
            Height = 22,
            AutoSize = false,
            Text = item.Title,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var descriptionLabel = new Label
        {
            Left = 68,
            Top = top + 28,
            Width = 548,
            Height = 34,
            AutoSize = false,
            Text = item.Description,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            TextAlign = ContentAlignment.TopLeft
        };

        parent.Controls.Add(numberLabel);
        parent.Controls.Add(titleLabel);
        parent.Controls.Add(descriptionLabel);
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
            CornerRadius);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}
