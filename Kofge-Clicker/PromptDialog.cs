namespace KofgeClicker;

public sealed class PromptDialog : Form
{
    private readonly TextBox _input;

    public string InputText => _input.Text;

    private PromptDialog(string title, string prompt, string initialValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(520, 246);
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextPrimary;
        KeyPreview = true;

        var shell = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 22,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = Color.FromArgb(83, 103, 143),
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        var titleLabel = new Label
        {
            Left = 28,
            Top = 20,
            Width = 464,
            Height = 30,
            AutoSize = false,
            Text = title,
            ForeColor = UiTheme.TextPrimary,
            BackColor = Color.Transparent,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var divider = new Panel
        {
            Left = 28,
            Top = 60,
            Width = 464,
            Height = 1,
            BackColor = UiTheme.BorderSoft
        };

        var label = new Label
        {
            Left = 30,
            Top = 76,
            Width = 460,
            Height = 24,
            AutoSize = false,
            Text = prompt,
            ForeColor = UiTheme.TextSoft,
            BackColor = Color.Transparent,
            Font = UiTheme.CreateFont("Segoe UI", 12.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var inputShell = new RoundedPanel
        {
            Left = 28,
            Top = 108,
            Width = 464,
            Height = 42,
            Radius = 12,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        _input = new TextBox
        {
            Left = 12,
            Top = 10,
            Width = 440,
            Height = 24,
            Text = initialValue,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = UiTheme.CreateFont("Segoe UI", 13f)
        };
        inputShell.Controls.Add(_input);

        var okButton = new AccentButton
        {
            Text = LocalizationService.Get("Common.Ok"),
            Left = 92,
            Top = 178,
            Width = 160,
            Height = 40,
            Primary = true,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new AccentButton
        {
            Text = LocalizationService.Get("Common.Cancel"),
            Left = 268,
            Top = 178,
            Width = 160,
            Height = 40,
            Primary = false,
            DialogResult = DialogResult.Cancel
        };

        shell.Controls.Add(titleLabel);
        shell.Controls.Add(divider);
        shell.Controls.Add(label);
        shell.Controls.Add(inputShell);
        shell.Controls.Add(okButton);
        shell.Controls.Add(cancelButton);
        Controls.Add(shell);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            WindowPlacement.ClampToWorkingArea(this);
            ApplyRoundedRegion();
            _input.Focus();
            _input.SelectAll();
        };
        SizeChanged += (_, _) => ApplyRoundedRegion();
    }

    public static (DialogResult Result, string Value) Show(IWin32Window owner, string title, string prompt, string initialValue)
    {
        using var dialog = new PromptDialog(title, prompt, initialValue);
        var result = dialog.ShowDialog(owner);
        return (result, dialog.InputText);
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
