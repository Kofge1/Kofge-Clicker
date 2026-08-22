namespace KofgeClicker;

internal sealed class FirstRunTourDialog : Form
{
    private const int CornerRadius = 22;
    private readonly Action<int> _selectStep;
    private readonly Action<string> _requestHotkeyCapture;
    private readonly Func<string, string> _getHotkeyDisplay;
    private readonly Action<string> _selectClickerMode;
    private readonly Func<string> _getClickerMode;
    private readonly Func<string, bool> _getOptionValue;
    private readonly Action<string, bool> _setOptionValue;
    private readonly RoundedPanel _shell;
    private readonly RoundedPanel _content;
    private readonly Label _stepLabel;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _progressLabel;
    private readonly AccentButton _backButton;
    private readonly AccentButton _nextButton;
    private readonly AccentButton _skipButton;
    private readonly System.Windows.Forms.Timer _hotkeyRefreshTimer;
    private readonly List<Control> _dynamicControls = [];
    private int _currentStep;

    internal FirstRunTourDialog(
        Action<int> selectStep,
        Action<string> requestHotkeyCapture,
        Func<string, string> getHotkeyDisplay,
        Action<string> selectClickerMode,
        Func<string> getClickerMode,
        Func<string, bool> getOptionValue,
        Action<string, bool> setOptionValue)
    {
        _selectStep = selectStep;
        _requestHotkeyCapture = requestHotkeyCapture;
        _getHotkeyDisplay = getHotkeyDisplay;
        _selectClickerMode = selectClickerMode;
        _getClickerMode = getClickerMode;
        _getOptionValue = getOptionValue;
        _setOptionValue = setOptionValue;

        Text = LocalizationService.Get("Tour.WelcomeTitle");
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(760, 530);
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextPrimary;
        KeyPreview = true;

        _shell = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = CornerRadius,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = Color.FromArgb(83, 103, 143),
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        _stepLabel = new Label
        {
            Left = 32,
            Top = 24,
            Width = 250,
            Height = 26,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.AccentBorder,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _progressLabel = new Label
        {
            Left = 610,
            Top = 24,
            Width = 118,
            Height = 26,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 11.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };

        _titleLabel = new Label
        {
            Left = 32,
            Top = 58,
            Width = 696,
            Height = 38,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 18.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _descriptionLabel = new Label
        {
            Left = 32,
            Top = 101,
            Width = 696,
            Height = 82,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 12.5f),
            TextAlign = ContentAlignment.TopLeft
        };

        _content = new RoundedPanel
        {
            Left = 30,
            Top = 190,
            Width = 700,
            Height = 250,
            Radius = 17,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        _backButton = CreateButton(30, 460, 142, LocalizationService.Get("Tour.Back"), false, (_, _) => ShowStep(_currentStep - 1));
        _skipButton = CreateButton(184, 460, 250, LocalizationService.Get("Tour.Skip"), false, (_, _) => CompleteTour());
        _nextButton = CreateButton(568, 460, 162, LocalizationService.Get("Tour.Start"), true, NextStepClick);

        _shell.Controls.Add(_stepLabel);
        _shell.Controls.Add(_progressLabel);
        _shell.Controls.Add(_titleLabel);
        _shell.Controls.Add(_descriptionLabel);
        _shell.Controls.Add(_content);
        _shell.Controls.Add(_backButton);
        _shell.Controls.Add(_skipButton);
        _shell.Controls.Add(_nextButton);
        Controls.Add(_shell);

        _hotkeyRefreshTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _hotkeyRefreshTimer.Tick += (_, _) => RefreshHotkeyValues();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                CompleteTour();
            }
        };
        Shown += (_, _) =>
        {
            WindowPlacement.ClampToWorkingArea(this);
            ApplyRoundedRegion();
            ShowStep(0);
            _hotkeyRefreshTimer.Start();
        };
        SizeChanged += (_, _) => ApplyRoundedRegion();
        FormClosed += (_, _) => _hotkeyRefreshTimer.Stop();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeyRefreshTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private AccentButton CreateButton(int left, int top, int width, string text, bool primary, EventHandler onClick)
    {
        var button = new AccentButton
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 40,
            Text = text,
            Primary = primary
        };
        button.Click += onClick;
        return button;
    }

    private void ShowStep(int step)
    {
        _currentStep = Math.Clamp(step, 0, 6);
        ClearDynamicControls();

        if (_currentStep == 0)
        {
            ShowWelcome();
            return;
        }

        _selectStep(_currentStep);
        _stepLabel.Text = LocalizationService.Get("Tour.Step", _currentStep, 6);
        _progressLabel.Text = $"{_currentStep} / 6";
        _backButton.Visible = true;
        _skipButton.Visible = true;
        _nextButton.Text = _currentStep == 6
            ? LocalizationService.Get("Tour.Finish")
            : LocalizationService.Get("Tour.Next");
        _nextButton.Click -= CompleteTourClick;
        if (_currentStep == 6)
        {
            _nextButton.Click -= NextStepClick;
            _nextButton.Click += CompleteTourClick;
        }
        else
        {
            _nextButton.Click -= CompleteTourClick;
            _nextButton.Click -= NextStepClick;
            _nextButton.Click += NextStepClick;
        }

        switch (_currentStep)
        {
            case 1:
                ShowMainHotkeyStep();
                break;
            case 2:
                ShowModesStep();
                break;
            case 3:
                ShowPatternsStep();
                break;
            case 4:
                ShowServiceHotkeysStep();
                break;
            case 5:
                ShowStartupAndTrayStep();
                break;
            case 6:
                ShowTargetWindowStep();
                break;
        }
    }

    private void ShowWelcome()
    {
        _stepLabel.Text = LocalizationService.Get("Tour.WelcomeBadge");
        _progressLabel.Text = string.Empty;
        _titleLabel.Text = LocalizationService.Get("Tour.WelcomeTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.WelcomeText");
        _backButton.Visible = false;
        _skipButton.Visible = true;
        _nextButton.Text = LocalizationService.Get("Tour.Start");
        _nextButton.Click -= CompleteTourClick;
        _nextButton.Click -= NextStepClick;
        _nextButton.Click += NextStepClick;

        AddFeatureRow(38, "01", LocalizationService.Get("Tour.WelcomeHotkeys"));
        AddFeatureRow(100, "02", LocalizationService.Get("Tour.WelcomeBehavior"));
        AddFeatureRow(162, "03", LocalizationService.Get("Tour.WelcomeOptions"));
    }

    private void ShowMainHotkeyStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.MainHotkeyTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.MainHotkeyText");
        AddInfoText(LocalizationService.Get("Tour.CurrentBinding"), 26, 28, 190);
        AddHotkeyValue("triggerKey", 224, 24, 190);
        AddCaptureButton("triggerKey", 430, 22, LocalizationService.Get("Tour.ChangeBinding"));
        AddHint(LocalizationService.Get("Tour.MainHotkeyHint"), 26, 92, 640, 92);
    }

    private void ShowModesStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.ModesTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.ModesText");
        var selectedMode = _getClickerMode();
        AddModeCard(
            20,
            "hold",
            LocalizationService.Get("Clicker.Hold"),
            LocalizationService.Get("Tour.HoldText"),
            selectedMode == "hold");
        AddModeCard(
            132,
            "toggle",
            LocalizationService.Get("Clicker.Toggle"),
            LocalizationService.Get("Tour.ToggleText"),
            selectedMode == "toggle");
    }

    private void ShowPatternsStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.PatternsTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.PatternsText");
        AddPatternColumn(20, LocalizationService.Get("Pattern.Standard"), LocalizationService.Get("Tour.PatternStandard"));
        AddPatternColumn(188, LocalizationService.Get("Pattern.Burst"), LocalizationService.Get("Tour.PatternBurst"));
        AddPatternColumn(356, LocalizationService.Get("Pattern.DoubleClick"), LocalizationService.Get("Tour.PatternDouble"));
        AddPatternColumn(524, LocalizationService.Get("Pattern.HoldThenBurst"), LocalizationService.Get("Tour.PatternHoldBurst"));
        AddHint(LocalizationService.Get("Tour.PatternRateText"), 22, 172, 650, 48);
    }

    private void ShowServiceHotkeysStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.ServiceHotkeysTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.ServiceHotkeysText");
        AddHotkeyRow(29, "panicHotkey", LocalizationService.Get("Hotkeys.PanicStop"));
        AddHotkeyRow(79, "showWindowHotkey", LocalizationService.Get("Hotkeys.ShowWindow"));
        AddHotkeyRow(129, "togglePowerHotkey", LocalizationService.Get("Hotkeys.ToggleEnabled"));
        AddHotkeyRow(179, "profileHotkey", LocalizationService.Get("Hotkeys.NextProfile"));
    }

    private void ShowStartupAndTrayStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.StartupTrayTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.StartupTrayText");
        AddOptionToggleRow(6, "runAsAdministrator", LocalizationService.Get("Options.RunAsAdministrator"), LocalizationService.Get("Tour.StartupAdminText"));
        AddOptionToggleRow(52, "startHidden", LocalizationService.Get("Options.StartHidden"), LocalizationService.Get("Tour.StartupHiddenText"));
        AddOptionToggleRow(98, "runOnStartup", LocalizationService.Get("Options.RunOnStartup"), LocalizationService.Get("Tour.StartupWindowsText"));
        AddOptionToggleRow(144, "minimizeToTray", LocalizationService.Get("Options.MinimizeToTray"), LocalizationService.Get("Tour.StartupMinimizeText"));
        AddOptionToggleRow(190, "closeToTray", LocalizationService.Get("Options.CloseToTray"), LocalizationService.Get("Tour.StartupCloseText"));
    }

    private void ShowTargetWindowStep()
    {
        _titleLabel.Text = LocalizationService.Get("Tour.TargetWindowTitle");
        _descriptionLabel.Text = LocalizationService.Get("Tour.TargetWindowText");
        AddOptionRow(8, LocalizationService.Get("Options.RestrictWindow"), LocalizationService.Get("Tooltips.OptionTargetOnly"));
        AddOptionRow(66, LocalizationService.Get("Options.WindowTarget"), LocalizationService.Get("Tooltips.OptionTargetList"));
        AddOptionRow(124, LocalizationService.Get("Buttons.Refresh"), LocalizationService.Get("Tooltips.OptionRefreshWindows"));
        AddOptionRow(182, LocalizationService.Get("Tour.TargetFocusTitle"), LocalizationService.Get("Tour.TargetFocusText"));
    }

    private void AddFeatureRow(int top, string number, string text)
    {
        const int rowHeight = 50;
        var numberLabel = CreateDynamicLabel(number, 20, top, 44, rowHeight, UiTheme.AccentBorder, 13f, true);
        numberLabel.TextAlign = ContentAlignment.MiddleCenter;
        var textLabel = CreateDynamicLabel(text, 76, top, 606, rowHeight, UiTheme.TextPrimary, 12.5f, false);
        textLabel.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void AddInfoText(string text, int left, int top, int width)
    {
        CreateDynamicLabel(text, left, top, width, 40, UiTheme.TextSoft, 12.5f, false);
    }

    private void AddHotkeyValue(string target, int left, int top, int width)
    {
        var value = new InfoPill
        {
            Left = left,
            Top = top,
            Width = width,
            Height = 40,
            Text = _getHotkeyDisplay(target),
            Tag = target
        };
        AddDynamicControl(value);
    }

    private void AddCaptureButton(string target, int left, int top, string text)
    {
        var button = new AccentButton
        {
            Left = left,
            Top = top,
            Width = 236,
            Height = 42,
            Text = text,
            Primary = true
        };
        button.Click += (_, _) => _requestHotkeyCapture(target);
        AddDynamicControl(button);
    }

    private void AddHint(string text, int left, int top, int width, int height)
    {
        var label = CreateDynamicLabel(text, left, top, width, height, UiTheme.TextMuted, 12f, false);
        label.TextAlign = ContentAlignment.TopLeft;
    }

    private void AddModeCard(int top, string mode, string title, string text, bool selected)
    {
        var card = new RoundedPanel
        {
            Left = 18,
            Top = top,
            Width = 664,
            Height = 98,
            Radius = 14,
            FillColor = selected ? Color.FromArgb(43, 58, 91) : UiTheme.SurfaceAlt,
            BackColor = selected ? Color.FromArgb(43, 58, 91) : UiTheme.SurfaceAlt,
            BorderColor = selected ? UiTheme.AccentBorder : UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true,
            Cursor = Cursors.Hand
        };
        var titleLabel = new Label
        {
            Left = 18,
            Top = 12,
            Width = 190,
            Height = 26,
            Text = title,
            BackColor = Color.Transparent,
            ForeColor = selected ? UiTheme.TextPrimary : UiTheme.AccentBorder,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 13f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var textLabel = new Label
        {
            Left = 18,
            Top = 40,
            Width = 626,
            Height = 48,
            Text = text,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            Cursor = Cursors.Hand
        };
        EventHandler selectMode = (_, _) =>
        {
            _selectClickerMode(mode);
            ShowStep(2);
        };
        card.Click += selectMode;
        titleLabel.Click += selectMode;
        textLabel.Click += selectMode;
        card.Controls.Add(titleLabel);
        card.Controls.Add(textLabel);
        AddDynamicControl(card);
    }

    private void AddPatternColumn(int left, string title, string text)
    {
        var titleLabel = CreateDynamicLabel(title, left, 18, 148, 46, UiTheme.AccentBorder, 11.5f, true);
        titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        var textLabel = CreateDynamicLabel(text, left, 68, 148, 88, UiTheme.TextSoft, 10.5f, false);
        textLabel.TextAlign = ContentAlignment.TopCenter;
    }

    private void AddHotkeyRow(int top, string target, string label)
    {
        CreateDynamicLabel(label, 20, top, 218, 42, UiTheme.TextPrimary, 11.5f, false);
        AddHotkeyValue(target, 244, top + 1, 190);
        AddCaptureButton(target, 450, top, LocalizationService.Get("Buttons.Bind"));
        if (_dynamicControls[^1] is AccentButton button)
        {
            button.Width = 214;
        }
    }

    private void AddOptionRow(int top, string title, string text, int rowHeight = 56)
    {
        CreateDynamicLabel(title, 20, top, 202, rowHeight, UiTheme.AccentBorder, 11.5f, true);
        var description = CreateDynamicLabel(text, 232, top, 448, rowHeight, UiTheme.TextSoft, 10.8f, false);
        description.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void AddOptionToggleRow(int top, string target, string title, string text)
    {
        CreateDynamicLabel(title, 20, top, 164, 44, UiTheme.AccentBorder, 10.8f, true);
        var description = CreateDynamicLabel(text, 190, top, 354, 44, UiTheme.TextSoft, 10.2f, false);
        description.TextAlign = ContentAlignment.MiddleLeft;

        var toggle = new ToggleSwitchCheckBox
        {
            Left = 566,
            Top = top + 4,
            Width = 112,
            Height = 36,
            UseSlidingKnob = false,
            CheckedFillColor = Color.FromArgb(56, 136, 78),
            CheckedBorderColor = Color.FromArgb(91, 170, 111),
            UncheckedFillColor = Color.FromArgb(145, 59, 63),
            UncheckedBorderColor = Color.FromArgb(190, 90, 94),
            BackColor = UiTheme.Surface,
            Checked = _getOptionValue(target),
            Tag = target
        };
        toggle.CheckedChanged += (_, _) => _setOptionValue(target, toggle.Checked);
        AddDynamicControl(toggle);
    }

    private Label CreateDynamicLabel(
        string text,
        int left,
        int top,
        int width,
        int height,
        Color color,
        float fontSize,
        bool bold)
    {
        var label = new Label
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            AutoSize = false,
            Text = text,
            BackColor = Color.Transparent,
            ForeColor = color,
            Font = UiTheme.CreateFont("Segoe UI" + (bold ? " Semibold" : string.Empty), fontSize, bold ? FontStyle.Bold : FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        AddDynamicControl(label);
        return label;
    }

    private void AddDynamicControl(Control control)
    {
        _dynamicControls.Add(control);
        _content.Controls.Add(control);
    }

    private void ClearDynamicControls()
    {
        foreach (var control in _dynamicControls)
        {
            _content.Controls.Remove(control);
            control.Dispose();
        }
        _dynamicControls.Clear();
    }

    private void RefreshHotkeyValues()
    {
        foreach (var control in _dynamicControls)
        {
            if (control is InfoPill value && value.Tag is string target)
            {
                var updatedText = _getHotkeyDisplay(target);
                if (!string.Equals(value.Text, updatedText, StringComparison.Ordinal))
                {
                    value.Text = updatedText;
                    value.Invalidate();
                    value.Update();
                }
            }
        }
    }

    private void NextStepClick(object? sender, EventArgs e) => ShowStep(_currentStep + 1);

    private void CompleteTourClick(object? sender, EventArgs e) => CompleteTour();

    private void CompleteTour()
    {
        DialogResult = DialogResult.OK;
        Close();
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
