namespace KofgeClicker;

public sealed partial class MainForm
{
    private const string FirstRunTourSettingKey = "FirstRunTourCompleted";

    private void QueueFirstRunTour()
    {
        if (_firstRunTourHandled
            || _firstRunTourQueued
            || !_startupCompleted
            || IsDisposed
            || !Visible
            || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        _firstRunTourQueued = true;
        BeginInvoke(new Action(ShowFirstRunTourIfNeeded));
    }

    private void ShowFirstRunTourIfNeeded()
    {
        _firstRunTourQueued = false;
        if (_firstRunTourHandled
            || IsDisposed
            || !Visible
            || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        if (_ini.ReadBool("App", FirstRunTourSettingKey, false))
        {
            _firstRunTourHandled = true;
            QueueWhatsNewDialog();
            return;
        }

        _firstRunTourHandled = true;
        using var dialog = new FirstRunTourDialog(
            SelectTourStep,
            StartRecordHotkeyFor,
            GetTourHotkeyDisplay,
            SelectTourClickerMode,
            GetTourClickerMode,
            GetTourOptionValue,
            SetTourOptionValue);
        dialog.ShowDialog(this);

        StopRecordingHotkey();
        _ini.UpdateSections(
        [
            ("App", new List<KeyValuePair<string, string>>
            {
                new(FirstRunTourSettingKey, "1"),
                new("LastSeenWhatsNewVersion", AppVersion.Display)
            })
        ], flushToDisk: false);
        _pageHost.SelectedIndex = 0;
        Activate();
    }

    private void SelectTourStep(int stepNumber)
    {
        var pageIndex = stepNumber switch
        {
            1 or 2 => 0,
            3 => 1,
            4 => 3,
            5 or 6 => 5,
            _ => 0
        };

        if (_pageHost.SelectedIndex != pageIndex)
        {
            _pageHost.SelectedIndex = pageIndex;
        }
    }

    private string GetTourHotkeyDisplay(string targetName)
    {
        if (string.Equals(_recordingTargetName, targetName, StringComparison.Ordinal))
        {
            return L("Hotkeys.RecordingPrompt");
        }

        return FormatHotkeyDisplay(GetEffectiveHotkeyForTarget(targetName));
    }

    private void SelectTourClickerMode(string mode)
    {
        var normalizedMode = NormalizeMode(mode);
        var targetButton = normalizedMode == "toggle" ? _rbToggle : _rbHold;
        if (!targetButton.Checked)
        {
            targetButton.Checked = true;
        }
    }

    private string GetTourClickerMode() => NormalizeMode(_settings.CurrentMode);

    private bool GetTourOptionValue(string target)
    {
        return target switch
        {
            "runAsAdministrator" => _settings.RunAsAdministrator,
            "startHidden" => _settings.StartMinimized,
            "runOnStartup" => _settings.RunOnWindowsStartup,
            "minimizeToTray" => _settings.MinimizeToTrayOnMinimize,
            "closeToTray" => _settings.CloseToTrayOnClose,
            _ => false
        };
    }

    private void SetTourOptionValue(string target, bool value)
    {
        _suppressUiEvents = true;
        try
        {
            switch (target)
            {
                case "runAsAdministrator":
                    _settings.RunAsAdministrator = value;
                    _chkRunAsAdministrator.Checked = value;
                    break;
                case "startHidden":
                    _settings.StartMinimized = value;
                    _chkStartMinimized.Checked = value;
                    break;
                case "runOnStartup":
                    _settings.RunOnWindowsStartup = value;
                    _chkRunOnStartup.Checked = value;
                    break;
                case "minimizeToTray":
                    _settings.MinimizeToTrayOnMinimize = value;
                    _chkMinimizeToTray.Checked = value;
                    break;
                case "closeToTray":
                    _settings.CloseToTrayOnClose = value;
                    _chkCloseToTray.Checked = value;
                    break;
                default:
                    return;
            }
        }
        finally
        {
            _suppressUiEvents = false;
        }

        SaveWindowAndTraySettings(syncStartupShortcut: target == "runOnStartup");
    }
}
