namespace KofgeClicker;

public sealed partial class MainForm
{
    private void ConfigureHoverTooltips()
    {
        RegisterTextTooltip("Tabs.Clicker", "Tooltips.TabClicker");
        RegisterTextTooltip("Tabs.Pattern", "Tooltips.TabPattern");
        RegisterTextTooltip("Tabs.Mouse", "Tooltips.TabMouse");
        RegisterTextTooltip("Tabs.Hotkey", "Tooltips.TabHotkey");
        RegisterTextTooltip("Tabs.Profiles", "Tooltips.TabProfiles");
        RegisterTextTooltip("Tabs.Options", "Tooltips.TabOptions");

        RegisterTextTooltip("Clicker.Enabled", "Tooltips.ClickerEnabled", _chkEnabled);
        RegisterTextTooltip("Clicker.Mode", "Tooltips.ClickerMode");
        RegisterTextTooltip("Clicker.Hotkey", "Tooltips.ClickerHotkey", _txtTriggerHotkey);
        RegisterTextTooltip("Buttons.Bind", "Tooltips.BindHotkey");
        RegisterTooltip("Tooltips.ClickerCps", _trkCps, _txtCps, _lblCpsValue);
        RegisterTextTooltip(
            "Clicker.Humanized",
            "Tooltips.ClickerHumanized",
            _chkHumanized,
            _rbPresetStable,
            _rbPresetNatural,
            _rbPresetAggressive);

        RegisterTextTooltip("Pattern.Pattern", "Tooltips.PatternType", _cmbPattern);
        RegisterTextTooltip("Pattern.Clicks", "Tooltips.PatternClicks", _txtBurstCount);
        RegisterTextTooltip("Pattern.GapMs", "Tooltips.PatternGap", _txtBurstGap);
        RegisterTextTooltip("Pattern.HoldMs", "Tooltips.PatternHold", _txtHoldBurst);
        RegisterTextTooltip("Pattern.PressMs", "Tooltips.PatternPress", _txtPressDelay);
        RegisterTextTooltip("Pattern.ReleaseMs", "Tooltips.PatternRelease", _txtReleaseDelay);
        RegisterTextTooltip("Pattern.RateBehavior", "Tooltips.PatternRate", _rbRateLocked, _rbRateAmplified);

        // Register these last because the Russian Hold label is also used by a pattern field.
        RegisterTooltip("Tooltips.ClickerModeHold", _rbHold);
        RegisterTooltip("Tooltips.ClickerModeToggle", _rbToggle);

        RegisterTextTooltip("Mouse.Mouse", "Tooltips.MouseButton", _cmbClickButton);
        RegisterTooltip("Tooltips.MouseTestBothButtons", _clickTestSurface);
        RegisterTooltip("Tooltips.MouseTestReset", _btnResetClickTest);

        RegisterTextTooltip("Hotkeys.PanicStop", "Tooltips.HotkeyPanic", _txtPanicHotkey);
        RegisterTextTooltip("Hotkeys.ShowWindow", "Tooltips.HotkeyWindow", _txtShowWindowHotkey);
        RegisterTextTooltip("Hotkeys.ToggleEnabled", "Tooltips.HotkeyEnabled", _txtTogglePowerHotkey);
        RegisterTextTooltip("Hotkeys.NextProfile", "Tooltips.HotkeyProfile", _txtProfileHotkey);
        RegisterTextTooltip("Buttons.ResetHotkeys", "Tooltips.ResetHotkeys");

        RegisterTextTooltip("Profiles.Current", "Tooltips.ProfileCurrent", _cmbProfiles);
        RegisterTextTooltip("Buttons.New", "Tooltips.ProfileNew");
        RegisterTextTooltip("Buttons.Rename", "Tooltips.ProfileRename");
        RegisterTextTooltip("Buttons.Duplicate", "Tooltips.ProfileDuplicate");
        RegisterTextTooltip("Buttons.Delete", "Tooltips.ProfileDelete");
        RegisterTextTooltip("Buttons.Export", "Tooltips.ProfileExport");
        RegisterTextTooltip("Buttons.Import", "Tooltips.ProfileImport");
        RegisterTextTooltip("Buttons.SetStartup", "Tooltips.ProfileStartup", _lblStartupProfile);

        RegisterTextTooltip("Options.RunAsAdministrator", "Tooltips.OptionAdministrator", _chkRunAsAdministrator);
        RegisterTextTooltip("Options.StartHidden", "Tooltips.OptionStartHidden", _chkStartMinimized);
        RegisterTextTooltip("Options.RunOnStartup", "Tooltips.OptionWindowsStartup", _chkRunOnStartup);
        RegisterTextTooltip("Options.MinimizeToTray", "Tooltips.OptionMinimizeTray", _chkMinimizeToTray);
        RegisterTextTooltip("Options.CloseToTray", "Tooltips.OptionCloseTray", _chkCloseToTray);
        RegisterTextTooltip("Options.RestrictWindow", "Tooltips.OptionTargetOnly", _chkRestrictWindow);
        RegisterTooltip("Tooltips.OptionTargetList", _cmbTargetWindow, _lblTargetWindow);
        RegisterTextTooltip("Buttons.Refresh", "Tooltips.OptionRefreshWindows", _btnRefreshWindows);
        RegisterTooltip("Tooltips.LanguageToggle", _btnLanguageToggle);

        RegisterTextTooltip("Buttons.Apply", "Tooltips.Apply");
        RegisterTextTooltip("Buttons.Close", "Tooltips.Close");
        RegisterTooltip("Tooltips.Status", _lblStatus, _statusCard);
    }

    private void RegisterTextTooltip(string visibleTextKey, string tooltipKey, params Control[] relatedControls)
    {
        var visibleText = L(visibleTextKey);
        var message = L(tooltipKey);
        foreach (var control in EnumerateControls(this))
        {
            if (string.Equals(control.Text, visibleText, StringComparison.Ordinal))
            {
                _hoverTooltips.SetTooltip(control, message);
            }
        }

        foreach (var control in relatedControls)
        {
            _hoverTooltips.SetTooltip(control, message);
        }
    }

    private void RegisterTooltip(string tooltipKey, params Control[] controls)
    {
        var message = L(tooltipKey);
        foreach (var control in controls)
        {
            _hoverTooltips.SetTooltip(control, message);
        }
    }

    private static IEnumerable<Control> EnumerateControls(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in EnumerateControls(child))
            {
                yield return descendant;
            }
        }
    }
}
