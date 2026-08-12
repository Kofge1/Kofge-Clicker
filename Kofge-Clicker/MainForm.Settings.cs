using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace KofgeClicker;

public sealed partial class MainForm
{
    private void LoadSettings()
    {
        _settings.LanguageCode = LocalizationService.NormalizeLanguageCode(
            _ini.ReadString("Main", "Language", LocalizationService.CurrentLanguageCode));
        _profiles.Clear();
        if (!File.Exists(_settingsPath))
        {
            _profiles.Add(new ProfileInfo { Id = DefaultProfileId, Name = "Default" });
            _activeProfileId = DefaultProfileId;
            _defaultProfileId = DefaultProfileId;
            SanitizeLoadedSettings();
            UpdateInterval();
            RememberCurrentHotkeysAsValid();
            return;
        }

        _ini.NormalizeSection("Main");
        _settings.AutoEnabled = _ini.ReadBool("Main", "AutoEnabled", false);
        _settings.CurrentMode = _ini.ReadString("Main", "Mode", "hold");
        _settings.TriggerKey = _ini.ReadString("Main", "Hotkey", "F2");
        _settings.PanicHotkey = _ini.ReadString("Main", "PanicHotkey", "F12");
        _settings.ShowWindowHotkey = _ini.ReadString("Main", "ShowWindowHotkey", "F10");
        _settings.TogglePowerHotkey = _ini.ReadString("Main", "TogglePowerHotkey", "F7");
        _settings.ProfileHotkey = _ini.ReadString("Main", "ProfileHotkey", "F9");
        _settings.StartMinimized = _ini.ReadBool("Main", "StartMinimized");
        _settings.MinimizeToTrayOnMinimize = _ini.ReadBool("Main", "MinimizeToTrayOnMinimize");
        _settings.RememberLastProfile = false;
        _settings.RunOnWindowsStartup = _ini.ReadBool("Main", "RunOnWindowsStartup");
        _settings.RunAsAdministrator = _ini.ReadBool("Main", "RunAsAdministrator");
        _settings.CloseToTrayOnClose = _ini.ReadBool("Main", "CloseToTrayOnClose", false);
        _settings.RestrictToFocusedWindow = _ini.ReadBool("Main", "RestrictToFocusedWindow");
        _settings.TargetWindowTitle = _ini.ReadString("Main", "TargetWindowTitle", "");
        _settings.TargetWindowClass = _ini.ReadString("Main", "TargetWindowClass", "");
        _settings.TargetWindowExe = _ini.ReadString("Main", "TargetWindowExe", "");
        _settings.ClickButton = _ini.ReadString("Main", "ClickButton", "Left");
        _settings.ClickPattern = _ini.ReadString("Main", "ClickPattern", "Standard");
        _settings.ClickRateMode = _ini.ReadString("Main", "ClickRateMode", "Ordinary");
        _settings.BurstClickCount = _ini.ReadInt("Main", "BurstClickCount", 3);
        _settings.BurstGapMs = _ini.ReadInt("Main", "BurstGapMs", 14);
        _settings.HoldThenBurstHoldMs = _ini.ReadInt("Main", "HoldThenBurstHoldMs", 70);
        _settings.PressDelayMs = _ini.ReadInt("Main", "PressDelayMs", 0);
        _settings.ReleaseDelayMs = _ini.ReadInt("Main", "ReleaseDelayMs", 0);
        _settings.Cps = _ini.ReadInt("Main", "CPS", 15);
        _settings.HumanizedCpsEnabled = _ini.ReadBool("Main", "HumanizedCpsEnabled");
        _settings.HumanizedPreset = _ini.ReadString("Main", "HumanizedPreset", InferPresetFromLegacyRange("Main"));
        SanitizeLoadedSettings();

        _profiles.AddRange(LoadProfilesMetadata());
        _activeProfileId = GetStoredActiveProfileId();
        _defaultProfileId = GetStoredDefaultProfileId();

        if (_profiles.Count == 0)
        {
            _profiles.Add(new ProfileInfo { Id = DefaultProfileId, Name = "Default" });
            _activeProfileId = DefaultProfileId;
            _defaultProfileId = DefaultProfileId;
            SaveProfileSettings(_activeProfileId);
        }

        if (!ProfileExists(_defaultProfileId))
        {
            _defaultProfileId = _profiles[0].Id;
        }

        if (!ProfileExists(_activeProfileId))
        {
            _activeProfileId = _profiles[0].Id;
        }

        var startupProfileId = _settings.RememberLastProfile ? _activeProfileId : _defaultProfileId;
        if (!ProfileExists(startupProfileId))
        {
            startupProfileId = _profiles[0].Id;
        }

        _activeProfileId = startupProfileId;
        LoadProfileSettings(_activeProfileId);
        UpdateInterval();
        SyncStartupShortcut();
        RememberCurrentHotkeysAsValid();
    }

    private void SaveSettings(bool syncStartupShortcut = true)
    {
        _ini.UpdateSection("Main",
        [
            new("Language", _settings.LanguageCode),
            new("AutoEnabled", _settings.AutoEnabled ? "1" : "0"),
            new("Mode", _settings.CurrentMode),
            new("Hotkey", _settings.TriggerKey),
            new("PanicHotkey", _settings.PanicHotkey),
            new("ShowWindowHotkey", _settings.ShowWindowHotkey),
            new("TogglePowerHotkey", _settings.TogglePowerHotkey),
            new("ProfileHotkey", _settings.ProfileHotkey),
            new("StartMinimized", _settings.StartMinimized ? "1" : "0"),
            new("MinimizeToTrayOnMinimize", _settings.MinimizeToTrayOnMinimize ? "1" : "0"),
            new("RememberLastProfile", _settings.RememberLastProfile ? "1" : "0"),
            new("RunOnWindowsStartup", _settings.RunOnWindowsStartup ? "1" : "0"),
            new("RunAsAdministrator", _settings.RunAsAdministrator ? "1" : "0"),
            new("CloseToTrayOnClose", _settings.CloseToTrayOnClose ? "1" : "0"),
            new("RestrictToFocusedWindow", _settings.RestrictToFocusedWindow ? "1" : "0"),
            new("TargetWindowTitle", _settings.TargetWindowTitle),
            new("TargetWindowClass", _settings.TargetWindowClass),
            new("TargetWindowExe", _settings.TargetWindowExe),
            new("ClickButton", _settings.ClickButton),
            new("ClickPattern", _settings.ClickPattern),
            new("ClickRateMode", _settings.ClickRateMode),
            new("BurstClickCount", _settings.BurstClickCount.ToString()),
            new("BurstGapMs", _settings.BurstGapMs.ToString()),
            new("HoldThenBurstHoldMs", _settings.HoldThenBurstHoldMs.ToString()),
            new("PressDelayMs", _settings.PressDelayMs.ToString()),
            new("ReleaseDelayMs", _settings.ReleaseDelayMs.ToString()),
            new("CPS", _settings.Cps.ToString()),
            new("HumanizedCpsEnabled", _settings.HumanizedCpsEnabled ? "1" : "0"),
            new("HumanizedPreset", _settings.HumanizedPreset)
        ]);
        if (syncStartupShortcut)
        {
            SyncStartupShortcut();
        }

        WriteProfilesMetadata();
        SaveProfileSettings(_activeProfileId);
    }

    private void QueueSettingsSave(bool syncStartupShortcut = false)
    {
        if (IsDisposed)
        {
            return;
        }

        _queuedStartupShortcutSync |= syncStartupShortcut;
        if (_settingsSaveQueued)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            var shouldSyncWithoutHandle = _queuedStartupShortcutSync;
            _queuedStartupShortcutSync = false;
            SaveSettings(shouldSyncWithoutHandle);
            return;
        }

        _settingsSaveQueued = true;
        BeginInvoke(new Action(() =>
        {
            _settingsSaveQueued = false;
            var shouldSync = _queuedStartupShortcutSync;
            _queuedStartupShortcutSync = false;

            if (!IsDisposed)
            {
                SaveSettings(shouldSync);
            }
        }));
    }

    private void SaveWindowAndTraySettings(bool syncStartupShortcut = false)
    {
        _ini.UpdateSection("Main",
        [
            new("Language", _settings.LanguageCode),
            new("StartMinimized", _settings.StartMinimized ? "1" : "0"),
            new("MinimizeToTrayOnMinimize", _settings.MinimizeToTrayOnMinimize ? "1" : "0"),
            new("RememberLastProfile", _settings.RememberLastProfile ? "1" : "0"),
            new("RunOnWindowsStartup", _settings.RunOnWindowsStartup ? "1" : "0"),
            new("RunAsAdministrator", _settings.RunAsAdministrator ? "1" : "0"),
            new("CloseToTrayOnClose", _settings.CloseToTrayOnClose ? "1" : "0")
        ]);

        if (syncStartupShortcut)
        {
            SyncStartupShortcut();
        }
    }

    private void SaveModeSetting()
    {
        _ini.WriteString("Main", "Mode", _settings.CurrentMode);
        _ini.WriteString(GetProfileSectionName(_activeProfileId), "Mode", _settings.CurrentMode);
    }

    private void SaveAutoEnabledSetting()
    {
        _ini.WriteBool("Main", "AutoEnabled", _settings.AutoEnabled);
        _ini.WriteBool(GetProfileSectionName(_activeProfileId), "AutoEnabled", _settings.AutoEnabled);
    }

    private void SaveHumanizedSetting()
    {
        _ini.WriteBool("Main", "HumanizedCpsEnabled", _settings.HumanizedCpsEnabled);
        _ini.WriteString("Main", "HumanizedPreset", _settings.HumanizedPreset);
        var section = GetProfileSectionName(_activeProfileId);
        _ini.WriteBool(section, "HumanizedCpsEnabled", _settings.HumanizedCpsEnabled);
        _ini.WriteString(section, "HumanizedPreset", _settings.HumanizedPreset);
    }

    private void QueueDeferredUiAction(Func<bool> isQueued, Action<bool> setQueued, Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (isQueued())
        {
            return;
        }

        if (!IsHandleCreated)
        {
            action();
            return;
        }

        setQueued(true);
        BeginInvoke(new Action(() =>
        {
            setQueued(false);
            if (!IsDisposed)
            {
                action();
            }
        }));
    }

    private void QueueAutoEnabledSettingSave()
    {
        QueueDeferredUiAction(
            () => _autoEnabledSettingSaveQueued,
            value => _autoEnabledSettingSaveQueued = value,
            SaveAutoEnabledSetting);
    }

    private void QueueHumanizedSettingSave()
    {
        QueueDeferredUiAction(
            () => _humanizedSettingSaveQueued,
            value => _humanizedSettingSaveQueued = value,
            SaveHumanizedSetting);
    }

    private void QueueModeSettingSave()
    {
        QueueDeferredUiAction(
            () => _modeSettingSaveQueued,
            value => _modeSettingSaveQueued = value,
            SaveModeSetting);
    }

    private void SaveClickButtonSetting()
    {
        _ini.WriteString("Main", "ClickButton", _settings.ClickButton);
        _ini.WriteString(GetProfileSectionName(_activeProfileId), "ClickButton", _settings.ClickButton);
    }

    private void QueueClickButtonSettingSave()
    {
        QueueDeferredUiAction(
            () => _clickButtonSaveQueued,
            value => _clickButtonSaveQueued = value,
            SaveClickButtonSetting);
    }

    private void SaveClickPatternSetting()
    {
        _ini.WriteString("Main", "ClickPattern", _settings.ClickPattern);
        _ini.WriteString(GetProfileSectionName(_activeProfileId), "ClickPattern", _settings.ClickPattern);
    }

    private void QueueClickPatternSettingSave()
    {
        QueueDeferredUiAction(
            () => _clickPatternSaveQueued,
            value => _clickPatternSaveQueued = value,
            SaveClickPatternSetting);
    }

    private void SaveClickRateModeSetting()
    {
        _ini.WriteString("Main", "ClickRateMode", _settings.ClickRateMode);
        _ini.WriteString(GetProfileSectionName(_activeProfileId), "ClickRateMode", _settings.ClickRateMode);
    }

    private void SavePatternNumbersSettings()
    {
        _ini.WriteInt("Main", "BurstClickCount", _settings.BurstClickCount);
        _ini.WriteInt("Main", "BurstGapMs", _settings.BurstGapMs);
        _ini.WriteInt("Main", "HoldThenBurstHoldMs", _settings.HoldThenBurstHoldMs);
        _ini.WriteInt("Main", "PressDelayMs", _settings.PressDelayMs);
        _ini.WriteInt("Main", "ReleaseDelayMs", _settings.ReleaseDelayMs);

        var section = GetProfileSectionName(_activeProfileId);
        _ini.WriteInt(section, "BurstClickCount", _settings.BurstClickCount);
        _ini.WriteInt(section, "BurstGapMs", _settings.BurstGapMs);
        _ini.WriteInt(section, "HoldThenBurstHoldMs", _settings.HoldThenBurstHoldMs);
        _ini.WriteInt(section, "PressDelayMs", _settings.PressDelayMs);
        _ini.WriteInt(section, "ReleaseDelayMs", _settings.ReleaseDelayMs);
    }

    private void QueuePatternNumbersSettingsSave()
    {
        QueueDeferredUiAction(
            () => _patternNumbersSaveQueued,
            value => _patternNumbersSaveQueued = value,
            SavePatternNumbersSettings);
    }

    private void QueueClickRateModeSettingSave()
    {
        QueueDeferredUiAction(
            () => _clickRateModeSaveQueued,
            value => _clickRateModeSaveQueued = value,
            SaveClickRateModeSetting);
    }

    private void QueueTrayMenuRefresh()
    {
        QueueDeferredUiAction(
            () => _trayMenuRefreshQueued,
            value => _trayMenuRefreshQueued = value,
            RefreshTrayMenu);
    }

    private void SaveProfileSettings(string profileId)
    {
        var section = GetProfileSectionName(profileId);
        _ini.WriteSection(section,
        [
            new("Name", GetProfileNameById(profileId)),
            new("AutoEnabled", _settings.AutoEnabled ? "1" : "0"),
            new("Mode", _settings.CurrentMode),
            new("Hotkey", _settings.TriggerKey),
            new("PanicHotkey", _settings.PanicHotkey),
            new("ShowWindowHotkey", _settings.ShowWindowHotkey),
            new("TogglePowerHotkey", _settings.TogglePowerHotkey),
            new("ProfileHotkey", _settings.ProfileHotkey),
            new("CloseToTrayOnClose", _settings.CloseToTrayOnClose ? "1" : "0"),
            new("RestrictToFocusedWindow", _settings.RestrictToFocusedWindow ? "1" : "0"),
            new("TargetWindowTitle", _settings.TargetWindowTitle),
            new("TargetWindowClass", _settings.TargetWindowClass),
            new("TargetWindowExe", _settings.TargetWindowExe),
            new("ClickButton", _settings.ClickButton),
            new("ClickPattern", _settings.ClickPattern),
            new("ClickRateMode", _settings.ClickRateMode),
            new("BurstClickCount", _settings.BurstClickCount.ToString()),
            new("BurstGapMs", _settings.BurstGapMs.ToString()),
            new("HoldThenBurstHoldMs", _settings.HoldThenBurstHoldMs.ToString()),
            new("PressDelayMs", _settings.PressDelayMs.ToString()),
            new("ReleaseDelayMs", _settings.ReleaseDelayMs.ToString()),
            new("CPS", _settings.Cps.ToString()),
            new("HumanizedCpsEnabled", _settings.HumanizedCpsEnabled ? "1" : "0"),
            new("HumanizedPreset", _settings.HumanizedPreset)
        ]);
    }

    private void LoadProfileSettings(string profileId)
    {
        var section = GetProfileSectionName(profileId);
        _settings.AutoEnabled = _ini.ReadBool(section, "AutoEnabled", _ini.ReadBool("Main", "AutoEnabled", false));
        _settings.CurrentMode = _ini.ReadString(section, "Mode", _ini.ReadString("Main", "Mode", "hold"));
        _settings.TriggerKey = _ini.ReadString(section, "Hotkey", _ini.ReadString("Main", "Hotkey", "F2"));
        _settings.PanicHotkey = _ini.ReadString(section, "PanicHotkey", _ini.ReadString("Main", "PanicHotkey", "F12"));
        _settings.ShowWindowHotkey = _ini.ReadString(section, "ShowWindowHotkey", _ini.ReadString("Main", "ShowWindowHotkey", "F10"));
        _settings.TogglePowerHotkey = _ini.ReadString(section, "TogglePowerHotkey", _ini.ReadString("Main", "TogglePowerHotkey", "F7"));
        _settings.ProfileHotkey = _ini.ReadString(section, "ProfileHotkey", _ini.ReadString("Main", "ProfileHotkey", "F9"));
        _settings.CloseToTrayOnClose = _ini.ReadBool(section, "CloseToTrayOnClose", _ini.ReadBool("Main", "CloseToTrayOnClose", false));
        _settings.RestrictToFocusedWindow = _ini.ReadBool(section, "RestrictToFocusedWindow", _ini.ReadBool("Main", "RestrictToFocusedWindow"));
        _settings.TargetWindowTitle = _ini.ReadString(section, "TargetWindowTitle", _ini.ReadString("Main", "TargetWindowTitle", ""));
        _settings.TargetWindowClass = _ini.ReadString(section, "TargetWindowClass", _ini.ReadString("Main", "TargetWindowClass", ""));
        _settings.TargetWindowExe = _ini.ReadString(section, "TargetWindowExe", _ini.ReadString("Main", "TargetWindowExe", ""));
        _settings.ClickButton = _ini.ReadString(section, "ClickButton", _ini.ReadString("Main", "ClickButton", "Left"));
        _settings.ClickPattern = _ini.ReadString(section, "ClickPattern", _ini.ReadString("Main", "ClickPattern", "Standard"));
        _settings.ClickRateMode = _ini.ReadString(section, "ClickRateMode", _ini.ReadString("Main", "ClickRateMode", "Ordinary"));
        _settings.BurstClickCount = _ini.ReadInt(section, "BurstClickCount", _ini.ReadInt("Main", "BurstClickCount", 3));
        _settings.BurstGapMs = _ini.ReadInt(section, "BurstGapMs", _ini.ReadInt("Main", "BurstGapMs", 14));
        _settings.HoldThenBurstHoldMs = _ini.ReadInt(section, "HoldThenBurstHoldMs", _ini.ReadInt("Main", "HoldThenBurstHoldMs", 70));
        _settings.PressDelayMs = _ini.ReadInt(section, "PressDelayMs", _ini.ReadInt("Main", "PressDelayMs", 0));
        _settings.ReleaseDelayMs = _ini.ReadInt(section, "ReleaseDelayMs", _ini.ReadInt("Main", "ReleaseDelayMs", 0));
        _settings.Cps = _ini.ReadInt(section, "CPS", _ini.ReadInt("Main", "CPS", 15));
        _settings.HumanizedCpsEnabled = _ini.ReadBool(section, "HumanizedCpsEnabled", _ini.ReadBool("Main", "HumanizedCpsEnabled"));
        _settings.HumanizedPreset = _ini.ReadString(section, "HumanizedPreset", InferPresetFromLegacyRange(section));
        SanitizeLoadedSettings();
        ResetHumanizedEngine();
        UpdateInterval();
        RememberCurrentHotkeysAsValid();
    }

    private List<ProfileInfo> LoadProfilesMetadata()
    {
        var list = new List<ProfileInfo>();
        var count = _ini.ReadInt("Profiles", "Count", 0);
        for (var i = 1; i <= count; i++)
        {
            var profileId = _ini.ReadString("Profiles", $"Profile{i}", "").Trim();
            if (profileId.Length == 0)
            {
                continue;
            }

            if (FindProfileIndexById(profileId, list) > 0)
            {
                continue;
            }

            var section = GetProfileSectionName(profileId);
            var profileName = NormalizeProfileName(_ini.ReadString(section, "Name", profileId));
            if (profileName.Length == 0)
            {
                profileName = $"Profile {i}";
            }

            list.Add(new ProfileInfo { Id = profileId, Name = profileName });
        }

        return list;
    }

    private void WriteProfilesMetadata()
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("ActiveId", _activeProfileId),
            new("DefaultId", _defaultProfileId),
            new("Count", _profiles.Count.ToString())
        };
        for (var i = 0; i < _profiles.Count; i++)
        {
            var profile = _profiles[i];
            values.Add(new KeyValuePair<string, string>($"Profile{i + 1}", profile.Id));
        }

        _ini.WriteSection("Profiles", values);
    }

    private string GetStoredActiveProfileId() => _ini.ReadString("Profiles", "ActiveId", DefaultProfileId);

    private string GetStoredDefaultProfileId() => _ini.ReadString("Profiles", "DefaultId", GetStoredActiveProfileId());

    private void ApplySettingsToUi()
    {
        _suppressUiEvents = true;
        try
        {
            _chkEnabled.Checked = _settings.AutoEnabled;
            _txtTriggerHotkey.Text = FormatHotkeyDisplay(GetEffectiveTriggerKey(_settings.TriggerKey));
            _rbHold.Checked = NormalizeMode(_settings.CurrentMode) == "hold";
            _rbToggle.Checked = NormalizeMode(_settings.CurrentMode) == "toggle";
            _txtPanicHotkey.Text = FormatHotkeyDisplay(GetEffectivePanicHotkey(_settings.PanicHotkey));
            _txtShowWindowHotkey.Text = FormatHotkeyDisplay(GetEffectiveShowWindowHotkey(_settings.ShowWindowHotkey));
            _txtTogglePowerHotkey.Text = FormatHotkeyDisplay(GetEffectiveTogglePowerHotkey(_settings.TogglePowerHotkey));
            _txtProfileHotkey.Text = FormatHotkeyDisplay(GetEffectiveProfileHotkey(_settings.ProfileHotkey));
            _trkCps.Value = ClampCps(_settings.Cps);
            _txtCps.Text = _settings.Cps.ToString();
            _lblCpsValue.Text = _settings.Cps.ToString();
            _chkHumanized.Checked = _settings.HumanizedCpsEnabled;
            _rbPresetStable.Checked = _settings.HumanizedPreset == "Stable";
            _rbPresetNatural.Checked = _settings.HumanizedPreset == "Natural";
            _rbPresetAggressive.Checked = _settings.HumanizedPreset == "Aggressive";
            _cmbPattern.SelectedIndex = NormalizeClickPattern(_settings.ClickPattern) switch
            {
                "Burst" => 1,
                "Double Click" => 2,
                "Hold then Burst" => 3,
                _ => 0
            };
            _cmbClickButton.SelectedIndex = NormalizeClickButton(_settings.ClickButton) == "Right" ? 1 : 0;
            _rbRateLocked.Checked = NormalizeClickRateMode(_settings.ClickRateMode) == "Ordinary";
            _rbRateAmplified.Checked = NormalizeClickRateMode(_settings.ClickRateMode) == "Amplified";
            _chkStartMinimized.Checked = _settings.StartMinimized;
            _chkRunAsAdministrator.Checked = _settings.RunAsAdministrator;
            _chkRunOnStartup.Checked = _settings.RunOnWindowsStartup;
            _chkRememberProfile.Checked = false;
            _chkMinimizeToTray.Checked = _settings.MinimizeToTrayOnMinimize;
            _chkCloseToTray.Checked = _settings.CloseToTrayOnClose;
            _chkRestrictWindow.Checked = _settings.RestrictToFocusedWindow;
            _btnLanguageToggle.Text = _settings.LanguageCode == LocalizationService.RussianLanguageCode ? "RU" : "EN";
            RefreshProfileControls();
            UpdatePatternControls();
            UpdateHumanizedControls();
            UpdateTargetWindowControls(true);
            RefreshAllHotkeyDisplays();
        }
        finally
        {
            _suppressUiEvents = false;
        }
    }

    private void ApplySettings(bool showConfirmation = false)
    {
        var unsafeServiceHotkeys = false;
        var newKey = NormalizeStoredHotkey(_settings.TriggerKey, "F2");
        var newPanicKey = NormalizeStoredHotkey(_settings.PanicHotkey, "F12");
        var newShowWindowKey = NormalizeStoredHotkey(_settings.ShowWindowHotkey, "F10");
        var newTogglePowerKey = NormalizeStoredHotkey(_settings.TogglePowerHotkey, "F7");
        var newProfileKey = NormalizeStoredHotkey(_settings.ProfileHotkey, "F9");
        var newMode = NormalizeMode(_settings.CurrentMode);

        if (IsRestrictedBareServiceMouseHotkey(newPanicKey)
            || IsRestrictedBareServiceMouseHotkey(newShowWindowKey)
            || IsRestrictedBareServiceMouseHotkey(newTogglePowerKey)
            || IsRestrictedBareServiceMouseHotkey(newProfileKey))
        {
            var repaired = RepairUnsafeServiceHotkeys(newKey, newPanicKey, newShowWindowKey, newTogglePowerKey, newProfileKey);
            newKey = repaired.main;
            newPanicKey = repaired.panic;
            newShowWindowKey = repaired.show;
            newTogglePowerKey = repaired.toggle;
            newProfileKey = repaired.profile;
            unsafeServiceHotkeys = true;
        }

        _settings.TriggerKey = newKey;
        _settings.PanicHotkey = newPanicKey;
        _settings.ShowWindowHotkey = newShowWindowKey;
        _settings.TogglePowerHotkey = newTogglePowerKey;
        _settings.ProfileHotkey = newProfileKey;

        if (TryFindAnyHotkeyConflict(
            out var firstTarget,
            out var firstHotkey,
            out var secondTarget,
            out var secondHotkey))
        {
            ShowHotkeyValidationMessage(L(
                "Validation.HotkeyConflict",
                FormatHotkeyDisplay(firstHotkey),
                GetHotkeyTargetDisplayName(firstTarget),
                GetHotkeyTargetDisplayName(secondTarget),
                FormatHotkeyDisplay(secondHotkey)));
            _settings.TriggerKey = _lastValidTriggerKey;
            _settings.PanicHotkey = _lastValidPanicHotkey;
            _settings.ShowWindowHotkey = _lastValidShowWindowHotkey;
            _settings.TogglePowerHotkey = _lastValidTogglePowerHotkey;
            _settings.ProfileHotkey = _lastValidProfileHotkey;
            _settings.CurrentMode = _lastValidMode;
            ApplySettingsToUi();
            return;
        }

        _settings.CurrentMode = newMode;
        RememberCurrentHotkeysAsValid();
        ApplySettingsToUi();
        SaveSettings();
        RefreshProfileControls();
        UpdateStatus();

        if (unsafeServiceHotkeys)
        {
            ShowProfileMessage(L("Validation.UnsafeMouseHotkeys"));
        }
        else if (showConfirmation)
        {
            _saveConfirmationToast.Show(_btnApply, L("Settings.Saved"));
        }
    }

    private void RememberCurrentHotkeysAsValid()
    {
        _lastValidTriggerKey = GetEffectiveTriggerKey(_settings.TriggerKey);
        _lastValidPanicHotkey = GetEffectivePanicHotkey(_settings.PanicHotkey);
        _lastValidShowWindowHotkey = GetEffectiveShowWindowHotkey(_settings.ShowWindowHotkey);
        _lastValidTogglePowerHotkey = GetEffectiveTogglePowerHotkey(_settings.TogglePowerHotkey);
        _lastValidProfileHotkey = GetEffectiveProfileHotkey(_settings.ProfileHotkey);
        _lastValidMode = NormalizeMode(_settings.CurrentMode);
    }

    private void UpdateFromSlider()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.Cps = ClampCps(_trkCps.Value);
        _txtCps.Text = _settings.Cps.ToString();
        _lblCpsValue.Text = _settings.Cps.ToString();
        UpdateInterval();
        _pendingCpsCommit = true;
    }

    private void CommitSliderCpsIfPending()
    {
        if (!_pendingCpsCommit)
        {
            return;
        }

        _pendingCpsCommit = false;
        SaveSettings();
        UpdateStatus();
    }

    private void OnCpsTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        UpdateFromEdit();
    }

    private void BeginCpsEdit()
    {
        _txtCps.Text = _settings.Cps.ToString();
        _lblCpsValue.Visible = false;
        _txtCps.Visible = true;
        _txtCps.Focus();
        _txtCps.SelectAll();
    }

    private void CommitCpsEditIfVisible()
    {
        if (!_txtCps.Visible)
        {
            return;
        }

        UpdateFromEdit();
    }

    private void UpdateFromEdit()
    {
        if (!_txtCps.Visible)
        {
            return;
        }

        if (_txtCps.Text.Length == 0)
        {
            _txtCps.Text = _settings.Cps.ToString();
            _txtCps.Visible = false;
            _lblCpsValue.Visible = true;
            return;
        }

        if (int.TryParse(_txtCps.Text, out var value))
        {
            _settings.Cps = ClampCps(value);
            _trkCps.Value = _settings.Cps;
            _txtCps.Text = _settings.Cps.ToString();
            _lblCpsValue.Text = _settings.Cps.ToString();
            UpdateInterval();
            SaveSettings();
            UpdateStatus();
            _txtCps.Visible = false;
            _lblCpsValue.Visible = true;
            return;
        }

        _txtCps.Text = _settings.Cps.ToString();
        _lblCpsValue.Text = _settings.Cps.ToString();
        _txtCps.Visible = false;
        _lblCpsValue.Visible = true;
    }

    private void OnEnabledChange()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        ApplyAutoEnabledState(_chkEnabled.Checked, updateCheckbox: false);
    }

    private void OnModeChanged()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var newMode = _rbToggle.Checked ? "toggle" : "hold";
        if (NormalizeMode(_settings.CurrentMode) == newMode)
        {
            return;
        }

        _settings.CurrentMode = newMode;
        QueueModeSettingSave();
        UpdateStatus(refreshTrayMenu: false);
        QueueTrayMenuRefresh();
    }

    private void OnHumanizedToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var newHumanizedEnabled = _chkHumanized.Checked;
        var modeChanged = _settings.HumanizedCpsEnabled != newHumanizedEnabled;
        var presetChanged = _settings.HumanizedPreset != "Aggressive";
        if (!modeChanged && !presetChanged)
        {
            return;
        }

        _settings.HumanizedCpsEnabled = newHumanizedEnabled;
        _settings.HumanizedPreset = "Aggressive";
        ResetHumanizedEngine();
        UpdateHumanizedControls();
        QueueHumanizedSettingSave();
        UpdateStatus(refreshTrayMenu: false);
    }

    private void OnClickButtonChanged()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var newClickButton = _cmbClickButton.SelectedIndex == 1 ? "Right" : "Left";
        if (_settings.ClickButton == newClickButton)
        {
            return;
        }

        ReleaseClickerMouseState(ClickStopReason.ConfigurationChanged);
        _settings.ClickButton = newClickButton;
        QueueClickButtonSettingSave();
        UpdateStatus(refreshTrayMenu: false);
    }

    private void OnClickPatternChanged()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var newPattern = _cmbPattern.SelectedIndex switch
        {
            1 => "Burst",
            2 => "Double Click",
            3 => "Hold then Burst",
            _ => "Standard"
        };
        if (_settings.ClickPattern == newPattern)
        {
            return;
        }

        _settings.ClickPattern = newPattern;
        UpdatePatternControls();
        QueueClickPatternSettingSave();
        UpdateStatus(refreshTrayMenu: false);
    }

    private void UpdatePatternNumber(string fieldName)
    {
        var oldBurstClickCount = _settings.BurstClickCount;
        var oldBurstGapMs = _settings.BurstGapMs;
        var oldHoldThenBurstHoldMs = _settings.HoldThenBurstHoldMs;
        var oldPressDelayMs = _settings.PressDelayMs;
        var oldReleaseDelayMs = _settings.ReleaseDelayMs;

        switch (fieldName)
        {
            case "burstCount":
                _settings.BurstClickCount = ReadPatternNumber(_txtBurstCount, _settings.BurstClickCount, ClampBurstClickCount);
                break;
            case "burstGap":
                _settings.BurstGapMs = ReadPatternNumber(_txtBurstGap, _settings.BurstGapMs, ClampPatternDelay);
                break;
            case "holdBurst":
                _settings.HoldThenBurstHoldMs = ReadPatternNumber(_txtHoldBurst, _settings.HoldThenBurstHoldMs, ClampPatternDelay);
                break;
            case "pressDelay":
                _settings.PressDelayMs = ReadPatternNumber(_txtPressDelay, _settings.PressDelayMs, ClampPatternDelay);
                break;
            case "releaseDelay":
                _settings.ReleaseDelayMs = ReadPatternNumber(_txtReleaseDelay, _settings.ReleaseDelayMs, ClampPatternDelay);
                break;
        }

        UpdatePatternControls();
        var patternNumbersChanged = _settings.BurstClickCount != oldBurstClickCount
            || _settings.BurstGapMs != oldBurstGapMs
            || _settings.HoldThenBurstHoldMs != oldHoldThenBurstHoldMs
            || _settings.PressDelayMs != oldPressDelayMs
            || _settings.ReleaseDelayMs != oldReleaseDelayMs;
        if (!patternNumbersChanged)
        {
            return;
        }

        QueuePatternNumbersSettingsSave();
    }

    private static int ReadPatternNumber(Control box, int currentValue, Func<int, int> clamp)
    {
        if (!int.TryParse(box.Text.Trim(), out var value))
        {
            box.Text = currentValue.ToString();
            return currentValue;
        }

        value = clamp(value);
        box.Text = value.ToString();
        return value;
    }

    private void UpdatePatternControls()
    {
        _txtBurstCount.Text = _settings.ClickPattern switch
        {
            "Burst" => "3",
            "Double Click" => "2",
            _ => _settings.BurstClickCount.ToString()
        };
        _txtBurstGap.Text = _settings.BurstGapMs.ToString();
        _txtHoldBurst.Text = _settings.HoldThenBurstHoldMs.ToString();
        _txtPressDelay.Text = _settings.PressDelayMs.ToString();
        _txtReleaseDelay.Text = _settings.ReleaseDelayMs.ToString();
        _txtBurstCount.Enabled = _settings.ClickPattern == "Hold then Burst";
        _txtBurstGap.Enabled = _settings.ClickPattern != "Standard";
        _txtHoldBurst.Enabled = _settings.ClickPattern == "Hold then Burst";
        _txtPressDelay.Enabled = true;
        _txtReleaseDelay.Enabled = true;
        _lblPatternHelp.Text = CustomPatternHelpText();
    }

    private void OnClickRateModeChanged()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var newRateMode = NormalizeClickRateMode(_rbRateLocked.Checked ? "Ordinary" : "Amplified");
        if (NormalizeClickRateMode(_settings.ClickRateMode) == newRateMode)
        {
            return;
        }

        _settings.ClickRateMode = newRateMode;
        _lblPatternHelp.Text = CustomPatternHelpText();
        QueueClickRateModeSettingSave();
        UpdateStatus(refreshTrayMenu: false);
        QueueTrayMenuRefresh();
    }

    private void SelectHumanizedPreset(string preset)
    {
        if (_suppressUiEvents)
        {
            return;
        }

        var normalizedPreset = NormalizeHumanizedPreset(preset);
        if (_settings.HumanizedPreset == normalizedPreset)
        {
            return;
        }

        _settings.HumanizedPreset = normalizedPreset;
        ResetHumanizedEngine();
        UpdateHumanizedControls();
        QueueHumanizedSettingSave();
        UpdateStatus(refreshTrayMenu: false);
    }

    private void UpdateHumanizedControls()
    {
        _settings.HumanizedPreset = "Aggressive";
        _rbPresetStable.Enabled = false;
        _rbPresetNatural.Enabled = false;
        _rbPresetAggressive.Enabled = false;
        _rbPresetStable.Visible = false;
        _rbPresetNatural.Visible = false;
        _rbPresetAggressive.Visible = false;
        _humanizedPresetGroup.Visible = false;
        _humanizedPresetGroup.Enabled = false;
    }

    private void RefreshProfileControls()
    {
        _suppressUiEvents = true;
        try
        {
            var selectedName = GetActiveProfileName();
            var profileNames = _profiles.Select(p => p.Name).ToArray();
            _cmbProfiles.SetItems(profileNames);
            if (profileNames.Length > 0)
            {
                _cmbProfiles.SelectedItem = selectedName;
                if (_cmbProfiles.SelectedIndex < 0)
                {
                    _cmbProfiles.SelectedIndex = 0;
                }
            }

            _settings.RememberLastProfile = false;
            _lblStartupProfile.Text = L("Profiles.Startup", GetProfileNameById(_defaultProfileId));
            _lblStartupProfile.Top = 166;
            _lblStartupProfile.Width = 760;

            _btnSetStartup.Text = L("Buttons.SetStartup");
            _btnSetStartup.Enabled = true;
            _btnSetStartup.Visible = true;

            _btnRememberProfileFlag.Visible = false;
            _btnRememberProfileValue.Visible = false;
            _btnRememberProfileFlag.Text = L("Profiles.Remember");
            _btnRememberProfileValue.Text = L("Profiles.LastUsed");
        }
        finally
        {
            _suppressUiEvents = false;
        }
    }

    private void OnProfileSelected()
    {
        if (_suppressUiEvents || _cmbProfiles.SelectedItem is not string selectedName || selectedName.Length == 0)
        {
            return;
        }

        SwitchToProfileByName(selectedName);
    }

    private void CreateProfile()
    {
        var result = PromptDialog.Show(this, L("Profiles.NewTitle"), L("Profiles.NewPrompt"), BuildNewProfileName());
        if (result.Result != DialogResult.OK)
        {
            return;
        }

        var profileName = NormalizeProfileName(result.Value);
        if (profileName.Length == 0)
        {
            ShowProfileMessage(L("Profiles.EmptyName"));
            return;
        }

        if (FindProfileIndexByName(profileName) > 0)
        {
            ShowProfileMessage(L("Profiles.AlreadyExists"));
            return;
        }

        SaveSettings(syncStartupShortcut: false);
        var profileId = GenerateProfileId(profileName);
        _profiles.Add(new ProfileInfo { Id = profileId, Name = profileName });
        _activeProfileId = profileId;
        SaveProfileSettings(profileId);
        WriteProfilesMetadata();
        ApplySettingsToUi();
        UpdateStatus();
    }

    private void DuplicateProfile()
    {
        SaveSettings(syncStartupShortcut: false);
        StopClicking(ClickStopReason.ProfileChange);
        var sourceName = GetActiveProfileName();
        var duplicateName = BuildDuplicateProfileName(sourceName);
        var profileId = GenerateProfileId(duplicateName);
        CopyProfileSectionData(_settingsPath, GetProfileSectionName(_activeProfileId), _settingsPath, GetProfileSectionName(profileId), duplicateName);
        _profiles.Add(new ProfileInfo { Id = profileId, Name = duplicateName });
        _activeProfileId = profileId;
        LoadProfileSettings(_activeProfileId);
        WriteProfilesMetadata();
        ApplySettingsToUi();
        UpdateStatus();
    }

    private void RenameProfile()
    {
        var oldName = GetActiveProfileName();
        var result = PromptDialog.Show(this, L("Profiles.RenameTitle"), L("Profiles.RenamePrompt"), oldName);
        if (result.Result != DialogResult.OK)
        {
            return;
        }

        var newName = NormalizeProfileName(result.Value);
        if (newName.Length == 0)
        {
            ShowProfileMessage(L("Profiles.EmptyName"));
            return;
        }

        var existingIndex = FindProfileIndexByName(newName);
        if (existingIndex > 0 && _profiles[existingIndex - 1].Id != _activeProfileId)
        {
            ShowProfileMessage(L("Profiles.AlreadyExists"));
            return;
        }

        if (newName == oldName)
        {
            return;
        }

        var profile = GetProfileById(_activeProfileId);
        if (profile is null)
        {
            return;
        }

        profile.Name = newName;
        SaveSettings(syncStartupShortcut: false);
        RefreshProfileControls();
        UpdateStatus();
    }

    private void DeleteProfile()
    {
        if (_profiles.Count <= 1)
        {
            ShowProfileMessage(L("Profiles.MustRemain"));
            return;
        }

        var profileName = GetActiveProfileName();
        if (!ThemedMessageDialog.Confirm(
                this,
                L("Profiles.DeleteTitle"),
                L("Profiles.DeleteQuestion", profileName)))
        {
            return;
        }

        StopClicking(ClickStopReason.ProfileChange);
        var deleteIndex = GetProfileIndexById(_activeProfileId);
        var deleteId = _activeProfileId;
        _profiles.RemoveAt(deleteIndex - 1);
        _ini.DeleteSection(GetProfileSectionName(deleteId));
        _activeProfileId = _profiles[0].Id;
        if (deleteId == _defaultProfileId)
        {
            _defaultProfileId = _activeProfileId;
        }

        LoadProfileSettings(_activeProfileId);
        SaveSettings(syncStartupShortcut: false);
        ApplySettingsToUi();
        UpdateStatus();
    }

    private void ExportProfile()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "INI Files (*.ini)|*.ini",
            FileName = BuildProfileExportFileName(GetActiveProfileName())
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveSettings();
        ExportProfileToFile(dialog.FileName, GetActiveProfileName());
        ShowProfileMessage(L("Profiles.Exported"));
    }

    private void ImportProfile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "INI Files (*.ini)|*.ini"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var importIni = new IniFile(dialog.FileName);
        var profileName = NormalizeProfileName(importIni.ReadString("ProfileExport", "Name", ""));
        if (profileName.Length == 0)
        {
            ShowProfileMessage(L("Profiles.InvalidFile"));
            return;
        }

        SaveSettings();
        StopClicking(ClickStopReason.ProfileChange);
        profileName = MakeUniqueProfileName(profileName);
        var profileId = GenerateProfileId(profileName);
        CopyProfileSectionData(dialog.FileName, "ProfileExport", _settingsPath, GetProfileSectionName(profileId), profileName);
        _profiles.Add(new ProfileInfo { Id = profileId, Name = profileName });
        _activeProfileId = profileId;
        LoadProfileSettings(_activeProfileId);
        SaveSettings();
        ApplySettingsToUi();
        UpdateStatus();
    }

    private void SetCurrentProfileAsDefault()
    {
        _defaultProfileId = _activeProfileId;
        SaveSettings();
        RefreshProfileControls();
        ShowProfileMessage(L("Profiles.StartupUpdated"));
    }

    private void OnCloseToTrayToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.CloseToTrayOnClose = _chkCloseToTray.Checked;
        SaveWindowAndTraySettings();
    }

    private void OnStartMinimizedToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.StartMinimized = _chkStartMinimized.Checked;
        SaveWindowAndTraySettings();
    }

    private void OnMinimizeToTrayToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.MinimizeToTrayOnMinimize = _chkMinimizeToTray.Checked;
        SaveWindowAndTraySettings();
    }

    private void OnRememberLastProfileToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.RememberLastProfile = _chkRememberProfile.Checked;
        SaveWindowAndTraySettings();
        RefreshProfileControls();
    }

    private void OnRunOnStartupToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.RunOnWindowsStartup = _chkRunOnStartup.Checked;
        SaveWindowAndTraySettings(syncStartupShortcut: true);
    }

    private void OnRunAsAdministratorToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.RunAsAdministrator = _chkRunAsAdministrator.Checked;
        SaveWindowAndTraySettings();
        ThemedMessageDialog.Show(this, L("Common.RestartRequired"), L("Options.AdminRestart"));
    }

    private void OnLanguageToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        _settings.LanguageCode = _settings.LanguageCode == LocalizationService.RussianLanguageCode
            ? LocalizationService.DefaultLanguageCode
            : LocalizationService.RussianLanguageCode;
        _btnLanguageToggle.Text = _settings.LanguageCode == LocalizationService.RussianLanguageCode ? "RU" : "EN";
        SaveWindowAndTraySettings();
        if (!ThemedMessageDialog.Confirm(this, L("Common.RestartRequired"), L("Options.LanguageRestartQuestion")))
        {
            return;
        }

        RestartAfterLanguageChange();
    }

    private void RestartAfterLanguageChange()
    {
        if (_isActive || _mouseButtonHeldByClicker.Length > 0 || MouseButtonSafety.HasPressedButtons)
        {
            StopClicking(ClickStopReason.Shutdown, updateStatus: false);
        }

        SaveSettings(syncStartupShortcut: false);
        if (!AppRestartHelper.TryStartRestart(Environment.ProcessId))
        {
            ThemedMessageDialog.Show(this, L("Common.RestartFailed"), L("Options.LanguageRestartFailed"));
            return;
        }

        _allowClose = true;
        _trayIcon.Visible = false;
        Close();
    }

    private void OnRestrictWindowToggle()
    {
        if (_suppressUiEvents)
        {
            return;
        }

        if (_chkRestrictWindow.Checked && !HasCapturedTargetWindow())
        {
            _chkRestrictWindow.Checked = false;
            _settings.RestrictToFocusedWindow = false;
            ShowProfileMessage(L("Options.SelectTargetFirst"));
            return;
        }

        _settings.RestrictToFocusedWindow = _chkRestrictWindow.Checked;
        SaveSettings();
        UpdateStatus();
    }

    private void OnTargetWindowSelected()
    {
        if (_targetWindowListBusy || _cmbTargetWindow.SelectedIndex < 0)
        {
            return;
        }

        SetSelectedTargetWindow(_cmbTargetWindow.SelectedIndex);
    }

    private void RefreshTargetWindowList()
    {
        _availableTargetWindows.Clear();
        var choices = new List<string> { L("Options.AnyWindow") };
        var selectedIndex = 0;

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (hwnd == Handle || !NativeMethods.IsWindowVisible(hwnd))
            {
                return true;
            }

            var windowTitle = NativeMethods.GetWindowTitle(hwnd);
            var windowExe = NativeMethods.GetWindowProcessName(hwnd);
            var windowClass = NativeMethods.GetWindowClass(hwnd);

            if (windowTitle.Length == 0 && windowExe.Length == 0)
            {
                return true;
            }

            var matchTitle = string.Empty;
            var matchClass = string.Empty;
            var matchExe = string.Empty;

            if (windowExe.Length > 0)
            {
                matchExe = windowExe;
            }
            else if (windowClass.Length > 0)
            {
                matchClass = windowClass;
            }
            else
            {
                matchTitle = windowTitle;
            }

            if (HasTargetWindowEntry(_availableTargetWindows, matchTitle, matchClass, matchExe))
            {
                return true;
            }

            var entry = new TargetWindowInfo
            {
                Title = matchTitle,
                Class = matchClass,
                Exe = matchExe,
                Display = BuildTargetWindowChoiceLabel(windowTitle, windowClass, windowExe, matchTitle, matchClass, matchExe)
            };
            _availableTargetWindows.Add(entry);
            choices.Add(entry.Display);

            if (IsSameTargetWindow(matchTitle, matchClass, matchExe, _settings.TargetWindowTitle, _settings.TargetWindowClass, _settings.TargetWindowExe))
            {
                selectedIndex = choices.Count - 1;
            }

            return true;
        }, IntPtr.Zero);

        if (selectedIndex == 0 && HasCapturedTargetWindow())
        {
            var savedEntry = new TargetWindowInfo
            {
                Title = _settings.TargetWindowTitle,
                Class = _settings.TargetWindowClass,
                Exe = _settings.TargetWindowExe,
                Display = L("Options.SavedTarget", FormatTargetWindowDisplay())
            };
            _availableTargetWindows.Add(savedEntry);
            choices.Add(savedEntry.Display);
            selectedIndex = choices.Count - 1;
        }

        for (var i = 0; i < choices.Count; i++)
        {
            choices[i] = StripLegacyAllWindowsSuffix(choices[i]);
        }

        _targetWindowListBusy = true;
        try
        {
            _cmbTargetWindow.SetItems(choices);
            if (choices.Count > 0)
            {
                _cmbTargetWindow.SelectedIndex = Math.Min(selectedIndex, choices.Count - 1);
            }
        }
        finally
        {
            _targetWindowListBusy = false;
        }
    }

    private void SetSelectedTargetWindow(int selectedIndex)
    {
        if (selectedIndex <= 0)
        {
            ClearTargetWindowSelection();
            return;
        }

        var entryIndex = selectedIndex - 1;
        if (entryIndex >= _availableTargetWindows.Count)
        {
            ClearTargetWindowSelection();
            return;
        }

        var entry = _availableTargetWindows[entryIndex];
        _settings.TargetWindowTitle = entry.Title;
        _settings.TargetWindowClass = entry.Class;
        _settings.TargetWindowExe = entry.Exe;
        _settings.RestrictToFocusedWindow = true;
        UpdateTargetWindowControls(false);
        SaveSettings();
        UpdateStatus();
    }

    private void ClearTargetWindowSelection()
    {
        _settings.TargetWindowTitle = string.Empty;
        _settings.TargetWindowClass = string.Empty;
        _settings.TargetWindowExe = string.Empty;
        _settings.RestrictToFocusedWindow = false;
        UpdateTargetWindowControls(false);
        SaveSettings();
        UpdateStatus();
    }

    private void UpdateTargetWindowControls(bool refreshList)
    {
        _suppressUiEvents = true;
        try
        {
            _chkRestrictWindow.Checked = _settings.RestrictToFocusedWindow;
            _lblTargetWindow.Text = L("Options.TargetWindow", FormatTargetWindowDisplay());
            if (refreshList)
            {
                RefreshTargetWindowList();
            }
        }
        finally
        {
            _suppressUiEvents = false;
        }
    }


}
