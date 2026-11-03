namespace KofgeClicker;

public sealed partial class MainForm
{
    private void ApplyPixelPreview()
    {
        BackColor = PixelUiPreview.Background;
        StylePixelPanel(_tabBodyShell, PixelUiPreview.Frame);
        StylePixelPanel(_clickerCard, PixelUiPreview.Card);
        StylePixelPanel(_statusCard, PixelUiPreview.Frame);
        _tabHeader.BackColor = PixelUiPreview.Frame;
        _pageHost.BackColor = PixelUiPreview.Frame;
        if (_clickerCard.Parent is Control page) page.BackColor = PixelUiPreview.Frame;

        foreach (var control in EnumerateControls(_clickerCard))
        {
            PixelUiPreview.Mark(control);
            control.ForeColor = PixelUiPreview.Text;
            if (control is Panel) control.BackColor = control.Height == 1 ? PixelUiPreview.Border : PixelUiPreview.Card;
            if (control is Label label)
            {
                label.Font = PixelUiPreview.Font(label == _lblCpsValue ? 32 : label.Top == 10 ? 24 : 20);
                if (label is SurfaceLabel surface) surface.UseSolidPanelBackground = true;
            }
            else if (control is AccentButton button)
            {
                button.Font = PixelUiPreview.Font(button.Width < 110 ? 16 : 20);
                button.UseFilledBorderRing = true;
            }
            else if (control is ToggleSwitchCheckBox toggle)
            {
                toggle.Font = PixelUiPreview.Font(20);
                toggle.CheckedFillColor = PixelUiPreview.Accent;
                toggle.CheckedBorderColor = PixelUiPreview.AccentEdge;
                toggle.UncheckedFillColor = UiPalette.ToggleOff;
                toggle.UncheckedBorderColor = PixelUiPreview.Border;
            }
            else if (control is SegmentRadioButton) control.Font = PixelUiPreview.Font(20);
            else if (control is InfoPill pill)
            {
                pill.FillColor = PixelUiPreview.Surface;
                pill.BorderColor = PixelUiPreview.Border;
                pill.Font = PixelUiPreview.Font(20);
            }
        }

        // Keep the native editor's line metrics; the display label uses the pixel font.
        _txtCps.BackColor = PixelUiPreview.Card;
        _txtCps.AutoSize = false;
        _txtCps.Bounds = _lblCpsValue.Bounds;
        foreach (var button in _tabButtons.Concat(new[] { (AccentButton)_btnApply, (AccentButton)_btnClose, _btnLanguageToggle }))
        {
            PixelUiPreview.Mark(button);
            button.Font = PixelUiPreview.Font(20);
            button.UseFilledBorderRing = true;
        }
        _lblStatus.Font = PixelUiPreview.Font(16);
        _lblVersion.Font = PixelUiPreview.Font(24);
        PixelUiPreview.MarkUniformText(_lblStatus);
        ((SurfaceLabel)_lblStatus).PixelTextOffset = new Point(0, 2);
        PixelUiPreview.Mark(_lblVersion);
        _lblVersion.ForeColor = Color.FromArgb(158, 207, 231);
        PixelUiPreview.ApplyTree(this);
        ((AccentButton)_btnMacroBackgroundHelp).UsePixelHelpGlyph = true;
        PixelUiPreview.ApplyMenu(_trayMenu);
        _tabHeader.BringToFront();
        Invalidate(true);
    }

    private static void StylePixelPanel(RoundedPanel panel, Color fill)
    {
        panel.CacheBackgroundDrawing = false;
        panel.FillColor = fill;
        panel.BackColor = fill;
        panel.BorderColor = PixelUiPreview.Border;
        panel.DrawShadow = false;
        PixelUiPreview.Mark(panel);
        panel.CacheBackgroundDrawing = true;
    }
}
