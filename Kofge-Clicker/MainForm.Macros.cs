namespace KofgeClicker;

public sealed partial class MainForm
{
    private bool _closeRequestedDuringMacroSave;

    private void BuildMacroTab()
    {
        var tab = CreateTabPage(L("Tabs.Macros"));
        var card = CreateCard(
            tab,
            StandardTabCardLeft,
            StandardTabCardTop,
            StandardTabCardWidth,
            StandardTabCardHeight,
            L("Macros.Title"));

        _lblSkipMacroMouseMovement = new Label
        {
            Text = L("Macros.SkipMouseMovement"),
            Left = 510,
            Top = 10,
            Width = 318,
            Height = 32,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(_lblSkipMacroMouseMovement);
        _chkSkipMacroMouseMovement = CreateToggleSwitch(
            840, 10, 74, card, (_, _) => OnSkipMacroMouseMovementChanged());
        _chkSkipMacroMouseMovement.Height = 32;
        _chkSkipMacroMouseMovement.Font = UiTheme.CreateFont("Segoe UI Semibold", 11f, FontStyle.Bold);
        ConfigureOnOffToggle(_chkSkipMacroMouseMovement);
        _lblSkipMacroMouseMovement.BringToFront();
        _chkSkipMacroMouseMovement.BringToFront();

        card.Controls.Add(CreateLabel(L("Macros.Current"), 20, 76, 128));
        _cmbMacros = CreatePillDropdown(164, 68, 270, card, []);
        _cmbMacros.SelectedIndexChanged += (_, _) => RefreshSelectedMacroUi();
        _btnCreateMacro = CreateButton(
            L("Macros.Create"),
            450,
            69,
            100,
            card,
            (_, _) => CreateMacro(),
            primary: true);
        _btnRecordMacro = CreateButton(
            L("Macros.Record"),
            560,
            69,
            110,
            card,
            (_, _) => BeginMacroRecording(),
            primary: true);
        _btnPlayMacro = CreateButton(
            L("Macros.Play"),
            680,
            69,
            110,
            card,
            (_, _) => BeginMacroPlayback(),
            primary: true);
        _btnStopMacro = CreateButton(
            L("Macros.Stop"),
            800,
            69,
            115,
            card,
            (_, _) => StopOrCancelMacroActivity());

        _macroEventPreview = new MacroEventPreview
        {
            Left = 20,
            Top = 123,
            Width = 570,
            Height = 210
        };
        card.Controls.Add(_macroEventPreview);
        _btnRenameMacro = CreateButton(
            L("Buttons.Rename"),
            20,
            166,
            166,
            _macroEventPreview,
            (_, _) => RenameMacro());
        _btnDuplicateMacro = CreateButton(
            L("Buttons.Duplicate"),
            194,
            166,
            166,
            _macroEventPreview,
            (_, _) => DuplicateMacro());
        _btnDeleteMacro = CreateButton(
            L("Buttons.Delete"),
            368,
            166,
            182,
            _macroEventPreview,
            (_, _) => DeleteMacro());
        _btnMacroJournal = CreateButton(
            L("Macros.Journal"),
            376,
            6,
            174,
            _macroEventPreview,
            (_, _) => OpenMacroJournal());
        if (_btnMacroJournal is AccentButton journalButton)
        {
            journalButton.UseFilledBorderRing = true;
        }

        var statePanel = new RoundedPanel
        {
            Left = 610,
            Top = 123,
            Width = 305,
            Height = 210,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.CardInner,
            BorderColor = UiTheme.BorderSoft,
            Radius = 16,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        _lblMacroState = new Label
        {
            Left = 20,
            Top = 17,
            Width = 265,
            Height = 28,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.AccentBorder,
            Font = UiTheme.CreateFont("Segoe UI Semibold", 13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _lblMacroStats = new Label
        {
            Left = 20,
            Top = 53,
            Width = 265,
            Height = 27,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _lblMacroHint = new Label
        {
            Left = 20,
            Top = 163,
            Width = 265,
            Height = 35,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            TextAlign = ContentAlignment.TopLeft
        };

        _lblMacroRepeat = CreateMacroSettingLabel(L("Macros.Repeats"), 20, 89, 80);
        _txtMacroRepeatCount = CreatePillValueEditor(102, 85, 48, statePanel);
        ConfigureMacroValueEditor(_txtMacroRepeatCount);
        _txtMacroRepeatCount.ValueCommitted += (_, _) => CommitMacroPlaybackOptions();

        _lblMacroLoop = CreateMacroSettingLabel(L("Macros.Loop"), 157, 89, 50);
        _chkMacroRepeatForever = CreateToggleSwitch(
            211,
            85,
            74,
            statePanel,
            (_, _) => CommitMacroPlaybackOptions());
        _chkMacroRepeatForever.Height = 32;
        _chkMacroRepeatForever.Font = UiTheme.CreateFont("Segoe UI Semibold", 11f, FontStyle.Bold);
        ConfigureOnOffToggle(_chkMacroRepeatForever);

        _lblMacroRepeatDelay = CreateMacroSettingLabel(L("Macros.RepeatDelay"), 20, 130, 68);
        _txtMacroRepeatDelay = CreatePillValueEditor(92, 126, 70, statePanel);
        ConfigureMacroValueEditor(_txtMacroRepeatDelay);
        _txtMacroRepeatDelay.ValueCommitted += (_, _) => CommitMacroPlaybackOptions();
        _lblMacroMilliseconds = CreateMacroSettingLabel(L("Macros.MillisecondsShort"), 170, 130, 35);

        _lblMacroStartDelay = CreateMacroSettingLabel(L("Macros.StartDelay"), 20, 169, 136);
        _txtMacroStartDelay = CreatePillValueEditor(160, 165, 50, statePanel);
        ConfigureMacroValueEditor(_txtMacroStartDelay);
        _txtMacroStartDelay.ValueCommitted += (_, _) => CommitMacroPlaybackOptions();
        _lblMacroSeconds = CreateMacroSettingLabel(L("Macros.SecondsShort"), 218, 169, 40);

        statePanel.Controls.Add(_lblMacroState);
        statePanel.Controls.Add(_lblMacroStats);
        statePanel.Controls.Add(_lblMacroRepeat);
        statePanel.Controls.Add(_lblMacroLoop);
        statePanel.Controls.Add(_lblMacroRepeatDelay);
        statePanel.Controls.Add(_lblMacroMilliseconds);
        statePanel.Controls.Add(_lblMacroStartDelay);
        statePanel.Controls.Add(_lblMacroSeconds);
        statePanel.Controls.Add(_lblMacroHint);
        card.Controls.Add(statePanel);
        _pageHost.AddPage(tab);
    }

    private void LoadMacros()
    {
        _macros.Clear();
        _macros.AddRange(_macroStorage.LoadAll());
        RefreshMacroList();
    }

    private void RefreshMacroList(string? selectId = null)
    {
        if (selectId is null && _cmbMacros.SelectedIndex >= 0 &&
            _cmbMacros.SelectedIndex < _macros.Count)
        {
            selectId = _macros[_cmbMacros.SelectedIndex].Id;
        }

        _cmbMacros.SetItems(_macros.Select(macro => macro.Name));
        if (_macros.Count > 0)
        {
            var selectedIndex = selectId is null
                ? 0
                : _macros.FindIndex(macro => macro.Id.Equals(selectId, StringComparison.OrdinalIgnoreCase));
            _cmbMacros.SelectedIndex = Math.Max(0, selectedIndex);
        }

        RefreshSelectedMacroUi();
    }

    private MacroDefinition? GetSelectedMacro()
    {
        var index = _cmbMacros.SelectedIndex;
        return index >= 0 && index < _macros.Count ? _macros[index] : null;
    }

    private void OpenMacroJournal()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress)
        {
            return;
        }

        var selected = GetSelectedMacro();
        if (selected is null)
        {
            return;
        }

        try
        {
            using var dialog = new MacroJournalDialog(
                selected,
                _macroStorage,
                FormatMacroAction);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SavedMacro is not { } edited)
            {
                return;
            }

            var index = _macros.FindIndex(macro => macro.Id == edited.Id);
            if (index >= 0)
            {
                _macros[index] = edited;
            }

            RefreshMacroList(edited.Id);
            _notificationToast.Show(L("Macros.JournalSaved"), edited.Name);
        }
        catch (InvalidDataException ex)
        {
            InputDiagnostics.Write($"MacroJournalOpenFailed id={selected.Id} error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.JournalTitle"), L("Macros.JournalInvalidRecording"));
        }
    }

    private MacroDefinition? CreateMacro()
    {
        var suggestedName = L("Macros.DefaultName", _macros.Count + 1);
        var (result, value) = PromptDialog.Show(
            this,
            L("Macros.CreateTitle"),
            L("Macros.CreatePrompt"),
            suggestedName);
        if (result != DialogResult.OK)
        {
            return null;
        }

        var name = value.Trim();
        if (name.Length == 0)
        {
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.EmptyName"));
            return null;
        }

        if (_macros.Any(macro => macro.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
        {
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.AlreadyExists"));
            return null;
        }

        var macro = new MacroDefinition { Name = name };
        try
        {
            _macroStorage.Save(macro);
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroCreateFailed error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
            return null;
        }

        _macros.Insert(0, macro);
        RefreshMacroList(macro.Id);
        return macro;
    }

    private void RenameMacro()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress || GetSelectedMacro() is not { } macro)
        {
            return;
        }

        var (result, value) = PromptDialog.Show(
            this,
            L("Macros.RenameTitle"),
            L("Macros.RenamePrompt"),
            macro.Name);
        if (result != DialogResult.OK)
        {
            return;
        }

        var name = value.Trim();
        if (!ValidateMacroName(name, macro.Id))
        {
            return;
        }

        if (macro.Name.Equals(name, StringComparison.CurrentCulture))
        {
            return;
        }

        var previousName = macro.Name;
        var previousUpdatedUtc = macro.UpdatedUtc;
        macro.Name = name;
        macro.UpdatedUtc = DateTime.UtcNow;
        try
        {
            _macroStorage.Save(macro);
            RefreshMacroList(macro.Id);
            _notificationToast.Show(L("Macros.Renamed"), macro.Name);
        }
        catch (Exception ex)
        {
            macro.Name = previousName;
            macro.UpdatedUtc = previousUpdatedUtc;
            InputDiagnostics.Write($"MacroRenameFailed id={macro.Id} error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
        }
    }

    private void DuplicateMacro()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress || GetSelectedMacro() is not { } source)
        {
            return;
        }

        var duplicate = CloneMacro(source);
        duplicate.Id = Guid.NewGuid().ToString("N");
        duplicate.Name = BuildUniqueMacroCopyName(source.Name);
        duplicate.UpdatedUtc = DateTime.UtcNow;
        try
        {
            _macroStorage.Save(duplicate);
            _macros.Insert(0, duplicate);
            RefreshMacroList(duplicate.Id);
            _notificationToast.Show(L("Macros.Duplicated"), duplicate.Name);
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroDuplicateFailed source={source.Id} error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
        }
    }

    private void DeleteMacro()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress || GetSelectedMacro() is not { } macro)
        {
            return;
        }

        if (!ThemedMessageDialog.Confirm(
                this,
                L("Macros.DeleteTitle"),
                L("Macros.DeleteQuestion", macro.Name)))
        {
            return;
        }

        try
        {
            _macroStorage.Delete(macro.Id);
            _macros.Remove(macro);
            RefreshMacroList(_macros.FirstOrDefault()?.Id);
            _notificationToast.Show(L("Macros.Deleted"), macro.Name);
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroDeleteFailed id={macro.Id} error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.DeleteFailed"));
        }
    }

    private bool ValidateMacroName(string name, string? currentMacroId = null)
    {
        if (name.Length == 0)
        {
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.EmptyName"));
            return false;
        }

        if (_macros.Any(macro =>
                !macro.Id.Equals(currentMacroId, StringComparison.OrdinalIgnoreCase) &&
                macro.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
        {
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.AlreadyExists"));
            return false;
        }

        return true;
    }

    private string BuildUniqueMacroCopyName(string sourceName)
    {
        var baseName = L("Macros.CopyName", sourceName);
        var candidate = baseName;
        var suffix = 2;
        while (_macros.Any(macro => macro.Name.Equals(candidate, StringComparison.CurrentCultureIgnoreCase)))
        {
            candidate = $"{baseName} {suffix++}";
        }

        return candidate;
    }

    private static MacroDefinition CloneMacro(MacroDefinition source)
    {
        return new MacroDefinition
        {
            FormatVersion = source.FormatVersion,
            Name = source.Name,
            RepeatCount = source.RepeatCount,
            RepeatDelayMilliseconds = source.RepeatDelayMilliseconds,
            RepeatIndefinitely = source.RepeatIndefinitely,
            PlaybackStartDelaySeconds = source.PlaybackStartDelaySeconds,
            RecordedScreenLeft = source.RecordedScreenLeft,
            RecordedScreenTop = source.RecordedScreenTop,
            RecordedScreenWidth = source.RecordedScreenWidth,
            RecordedScreenHeight = source.RecordedScreenHeight,
            Events = source.Events.Select(item => new MacroEvent
            {
                Type = item.Type,
                OffsetMicroseconds = item.OffsetMicroseconds,
                Token = item.Token,
                IsDown = item.IsDown,
                X = item.X,
                Y = item.Y,
                WheelDelta = item.WheelDelta,
                VirtualKey = item.VirtualKey,
                ScanCode = item.ScanCode,
                Flags = item.Flags,
                Text = item.Text
            }).ToList()
        };
    }

    private async void BeginMacroRecording()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress)
        {
            return;
        }

        if (_isActive)
        {
            StopClicking(ClickStopReason.ServiceHotkey, updateStatus: false);
        }

        var macro = GetSelectedMacro();
        if (macro is null)
        {
            ShowFromTray();
            macro = CreateMacro();
        }

        if (macro is null)
        {
            return;
        }

        if (_recordingTargetName is not null)
        {
            StopRecordingHotkey();
        }

        if (_isActive)
        {
            StopClicking(ClickStopReason.ConfigurationChanged, updateStatus: false);
            UpdateStatus();
        }

        var countdown = new CancellationTokenSource();
        _macroCountdownCts = countdown;
        RefreshMacroControls();
        try
        {
            for (var seconds = 3; seconds > 0; seconds--)
            {
                _lblMacroState.Text = L("Macros.StartsIn", seconds);
                _lblMacroStats.Text = L("Macros.Stats", FormatMacroDuration(0), 0);
                _notificationToast.ShowCountdown(L("Macros.RecordingCountdown", seconds));
                await Task.Delay(1000, countdown.Token);
            }

            countdown.Token.ThrowIfCancellationRequested();
            _macroRecorder.Start(recordMouseMovement: !_settings.SkipMacroMouseMovement);
            CaptureHeldModifiersAtRecordingStart();
            _macroUiTimer.Start();
            InputDiagnostics.Write($"MacroRecordingStarted id={macro.Id}");
            RefreshMacroActivityUi();
            _notificationToast.Show(
                L("Macros.RecordingStarted"),
                FormatHotkeyDisplay(GetEffectiveMacroStopHotkey(_settings.MacroStopHotkey)));
            HideToTray(true);
        }
        catch (OperationCanceledException)
        {
            _notificationToast.Dismiss();
            if (!IsDisposed && !Disposing)
            {
                _lblMacroState.Text = L("Macros.Ready");
            }
        }
        finally
        {
            if (ReferenceEquals(_macroCountdownCts, countdown))
            {
                _macroCountdownCts = null;
            }

            countdown.Dispose();
            if (!IsDisposed && !Disposing)
            {
                RefreshMacroControls();
            }
        }
    }

    private void CaptureHeldModifiersAtRecordingStart()
    {
        int[] modifierKeys =
        [
            NativeMethods.VkLControl,
            NativeMethods.VkRControl,
            NativeMethods.VkLShift,
            NativeMethods.VkRShift,
            NativeMethods.VkLMenu,
            NativeMethods.VkRMenu,
            NativeMethods.VkLWin,
            NativeMethods.VkRWin
        ];

        foreach (var virtualKey in modifierKeys)
        {
            if (NativeMethods.IsPressed(virtualKey))
            {
                _macroRecorder.EnsureKeyDown((uint)virtualKey);
            }
        }
    }

    private async void BeginMacroPlayback()
    {
        if (IsMacroSessionActive() || _macroSaveInProgress)
        {
            return;
        }

        if (_isActive)
        {
            StopClicking(ClickStopReason.ServiceHotkey, updateStatus: false);
        }

        var macro = GetSelectedMacro();
        if (macro is null || macro.Events.Count == 0)
        {
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.NothingToPlay"));
            return;
        }

        CommitMacroPlaybackOptions();

        if (_recordingTargetName is not null)
        {
            StopRecordingHotkey();
        }

        if (_isActive)
        {
            StopClicking(ClickStopReason.ConfigurationChanged, updateStatus: false);
            UpdateStatus();
        }

        var countdown = new CancellationTokenSource();
        _macroPlaybackCountdownCts = countdown;
        RefreshMacroControls();
        var shouldStart = false;
        try
        {
            MinimizeToTaskbarForMacroPlayback();
            var startDelaySeconds = Math.Clamp(
                macro.PlaybackStartDelaySeconds,
                0,
                MacroDefinition.MaximumPlaybackStartDelaySeconds);
            for (var seconds = startDelaySeconds; seconds > 0; seconds--)
            {
                _lblMacroState.Text = L("Macros.PlaybackStartsIn", seconds);
                _lblMacroStats.Text = L(
                    "Macros.PlaybackProgress",
                    FormatMacroDuration(0),
                    0,
                    macro.Events.Count);
                _notificationToast.ShowCountdown(L("Macros.PlaybackCountdown", seconds));
                await Task.Delay(1000, countdown.Token);
            }

            countdown.Token.ThrowIfCancellationRequested();
            shouldStart = true;
        }
        catch (OperationCanceledException)
        {
            _notificationToast.Dismiss();
            InputDiagnostics.Write($"MacroPlaybackCountdownCancelled id={macro.Id}");
        }
        finally
        {
            if (ReferenceEquals(_macroPlaybackCountdownCts, countdown))
            {
                _macroPlaybackCountdownCts = null;
            }

            countdown.Dispose();
            if (!IsDisposed && !Disposing)
            {
                RefreshMacroControls();
            }
        }

        if (!shouldStart || IsDisposed || Disposing)
        {
            if (!IsDisposed && !Disposing)
            {
                RefreshSelectedMacroUi();
            }

            return;
        }

        var foregroundWindow = NativeMethods.GetForegroundWindow();
        InputDiagnostics.Write(
            $"MacroPlaybackStarted id={macro.Id} events={macro.Events.Count} keys={macro.Events.Count(item => item.Type == MacroEventType.Key)} textKeys={macro.Events.Count(item => item.Type == MacroEventType.Key && !string.IsNullOrEmpty(item.Text))} mouseButtons={macro.Events.Count(item => item.Type == MacroEventType.MouseButton)} mouseMoves={macro.Events.Count(item => item.Type == MacroEventType.MouseMove)} wheels={macro.Events.Count(item => item.Type is MacroEventType.MouseWheel or MacroEventType.MouseHorizontalWheel)} foregroundProcess={NativeMethods.GetWindowProcessName(foregroundWindow)} foregroundClass={NativeMethods.GetWindowClass(foregroundWindow)}");
        var playbackTask = _macroPlayer.PlayAsync(macro);
        _macroUiTimer.Start();
        RefreshMacroActivityUi();
        _notificationToast.Show(
            L("Macros.PlaybackStarted"),
            FormatHotkeyDisplay(GetEffectiveMacroStopHotkey(_settings.MacroStopHotkey)));
        var result = await playbackTask;
        if (IsDisposed || Disposing)
        {
            return;
        }

        _macroUiTimer.Stop();
        RefreshSelectedMacroUi();
        switch (result.Status)
        {
            case MacroPlaybackStatus.Completed:
                InputDiagnostics.Write($"MacroPlaybackCompleted id={macro.Id} events={macro.Events.Count}");
                _notificationToast.Show(L("Macros.PlaybackCompleted"), macro.Name);
                break;
            case MacroPlaybackStatus.Cancelled:
                InputDiagnostics.Write($"MacroPlaybackCancelled id={macro.Id}");
                _notificationToast.Show(L("Macros.PlaybackStopped"));
                break;
            default:
                InputDiagnostics.Write($"MacroPlaybackError id={macro.Id}");
                ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.PlaybackFailed"));
                break;
        }
    }

    private void StopOrCancelMacroActivity()
    {
        if (_macroCountdownCts is not null)
        {
            _macroCountdownCts.Cancel();
            return;
        }

        if (_macroPlaybackCountdownCts is not null)
        {
            _macroPlaybackCountdownCts.Cancel();
            return;
        }

        if (_macroPlayer.IsPlaying)
        {
            _macroPlayer.Stop();
            return;
        }

        StopMacroRecording(showWindow: false);
    }

    private async void StopMacroRecording(bool showWindow)
    {
        if (!_macroRecorder.IsRecording)
        {
            return;
        }

        var selected = GetSelectedMacro();
        if (selected is null)
        {
            _macroRecorder.Cancel();
            RefreshSelectedMacroUi();
            return;
        }

        _macroUiTimer.Stop();
        var recorded = _macroRecorder.Stop(selected.Id, selected.Name);
        recorded.RepeatCount = selected.RepeatCount;
        recorded.RepeatDelayMilliseconds = selected.RepeatDelayMilliseconds;
        recorded.RepeatIndefinitely = selected.RepeatIndefinitely;
        recorded.PlaybackStartDelaySeconds = selected.PlaybackStartDelaySeconds;
        _macroSaveInProgress = true;
        if (showWindow)
        {
            ShowFromTray();
        }

        _lblMacroState.Text = L("Macros.Saving");
        RefreshMacroControls();
        try
        {
            await Task.Run(() => _macroStorage.Save(recorded));
            if (IsDisposed || Disposing)
            {
                return;
            }

            var index = _macros.FindIndex(macro => macro.Id == recorded.Id);
            if (index >= 0)
            {
                _macros[index] = recorded;
            }

            InputDiagnostics.Write(
                $"MacroRecordingSaved id={recorded.Id} events={recorded.Events.Count} keys={recorded.Events.Count(item => item.Type == MacroEventType.Key)} textKeys={recorded.Events.Count(item => item.Type == MacroEventType.Key && !string.IsNullOrEmpty(item.Text))} mouseButtons={recorded.Events.Count(item => item.Type == MacroEventType.MouseButton)} mouseMoves={recorded.Events.Count(item => item.Type == MacroEventType.MouseMove)} wheels={recorded.Events.Count(item => item.Type is MacroEventType.MouseWheel or MacroEventType.MouseHorizontalWheel)} durationUs={GetMacroDuration(recorded)}");
            RefreshMacroList(recorded.Id);
            _notificationToast.Show(L("Macros.RecordingSaved"), recorded.Name);
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroSaveFailed id={recorded.Id} error={ex.GetType().Name}");
            _lblMacroState.Text = L("Macros.SaveFailed");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
        }
        finally
        {
            _macroSaveInProgress = false;
            if (!IsDisposed && !Disposing)
            {
                if (_closeRequestedDuringMacroSave)
                {
                    _closeRequestedDuringMacroSave = false;
                    _allowClose = true;
                    Close();
                }
                else
                {
                    RefreshMacroControls();
                }
            }
        }
    }

    private void CancelMacroActivityForShutdown()
    {
        _macroUiTimer.Stop();
        _macroCountdownCts?.Cancel();
        _macroPlaybackCountdownCts?.Cancel();
        _macroRecorder.Cancel();
        _macroPlayer.Stop();
    }

    private void OnMacroInputObserved(ObservedInputEvent input)
    {
        _macroRecorder.Record(input);
    }

    private void RefreshMacroActivityUi()
    {
        var snapshot = _macroRecorder.GetSnapshot();
        if (snapshot.IsRecording)
        {
            SetMacroPlaybackSettingsVisible(false);
            _lblMacroState.Text = L("Macros.Recording");
            _lblMacroStats.Text = L(
                "Macros.Stats",
                FormatMacroDuration(snapshot.ElapsedMicroseconds),
                snapshot.EventCount);
            _macroEventPreview.SetContent(
                L("Macros.LatestActions"),
                snapshot.RecentEvents.Select(FormatMacroPreviewLine).ToArray());
            _lblMacroHint.Text = L(
                "Macros.RecordingHint",
                FormatHotkeyDisplay(GetEffectiveMacroStopHotkey(_settings.MacroStopHotkey)));
            RefreshMacroControls();

            if (snapshot.EventCount >= MacroRecorder.MaximumEventCount)
            {
                StopMacroRecording(showWindow: true);
            }

            return;
        }

        var playback = _macroPlayer.GetSnapshot();
        if (playback.IsPlaying)
        {
            SetMacroPlaybackSettingsVisible(false);
            var selected = GetSelectedMacro();
            var completedEvents = selected?.Events
                .Take(playback.CompletedEventCount)
                .TakeLast(6)
                .Select(FormatMacroPreviewLine)
                .ToArray() ?? [];
            _lblMacroState.Text = playback.RepeatIndefinitely
                ? L("Macros.PlayingLoop", playback.CurrentRepeat)
                : L("Macros.PlayingRepeat", playback.CurrentRepeat, playback.RepeatCount);
            _lblMacroStats.Text = L(
                "Macros.PlaybackProgress",
                FormatMacroDuration(playback.ElapsedMicroseconds),
                playback.CompletedEventCount,
                playback.EventCount);
            _macroEventPreview.SetContent(
                L("Macros.PlaybackActions"),
                completedEvents.Length == 0 ? [L("Macros.WaitingForFirstAction")] : completedEvents);
            _lblMacroHint.Text = L(
                "Macros.PlaybackHint",
                FormatHotkeyDisplay(GetEffectiveMacroStopHotkey(_settings.MacroStopHotkey)));
            RefreshMacroControls();
            return;
        }

        _macroUiTimer.Stop();
    }

    private void RefreshSelectedMacroUi()
    {
        if (_macroRecorder.IsRecording || _macroPlayer.IsPlaying)
        {
            RefreshMacroActivityUi();
            return;
        }

        var selected = GetSelectedMacro();
        SetMacroPlaybackSettingsVisible(true);
        SyncMacroPlaybackOptions(selected);
        _lblMacroHint.Visible = false;
        if (selected is null)
        {
            _lblMacroState.Text = L("Macros.NoMacros");
            _lblMacroStats.Text = L("Macros.Stats", FormatMacroDuration(0), 0);
            _macroEventPreview.SetContent(L("Macros.LatestActions"), [L("Macros.EmptyPreview")]);
        }
        else
        {
            var recent = selected.Events
                .Skip(Math.Max(0, selected.Events.Count - 6))
                .Select(FormatMacroPreviewLine)
                .ToArray();
            _lblMacroState.Text = selected.Events.Count == 0 ? L("Macros.Ready") : L("Macros.Recorded");
            _lblMacroStats.Text = L(
                "Macros.Stats",
                FormatMacroDuration(GetMacroDuration(selected)),
                selected.Events.Count);
            _macroEventPreview.SetContent(
                L("Macros.LatestActions"),
                recent.Length == 0 ? [L("Macros.EmptyPreview")] : recent);
        }

        _lblMacroHint.Text = L(
            "Macros.SettingsHint",
            FormatHotkeyDisplay(GetEffectiveMacroStopHotkey(_settings.MacroStopHotkey)));
        RefreshMacroControls();
    }

    private void RefreshMacroControls()
    {
        var countingDown = _macroCountdownCts is not null || _macroPlaybackCountdownCts is not null;
        var recording = _macroRecorder.IsRecording;
        var playing = _macroPlayer.IsPlaying;
        var hasMacro = GetSelectedMacro() is not null;
        var hasRecordedActions = GetSelectedMacro()?.Events.Count > 0;
        var busy = countingDown || recording || playing || _macroSaveInProgress;
        _cmbMacros.Enabled = !busy && _macros.Count > 0;
        _btnCreateMacro.Enabled = !busy;
        _btnRecordMacro.Enabled = hasMacro && !busy;
        _btnPlayMacro.Enabled = hasRecordedActions && !busy;
        _btnStopMacro.Enabled = countingDown || recording || playing;
        _btnMacroJournal.Enabled = hasMacro && !busy;
        _btnRenameMacro.Enabled = hasMacro && !busy;
        _btnDuplicateMacro.Enabled = hasMacro && !busy;
        _btnDeleteMacro.Enabled = hasMacro && !busy;
        _chkSkipMacroMouseMovement.Enabled = !busy;
        _chkMacroRepeatForever.Enabled = hasMacro && !busy;
        _txtMacroRepeatCount.Enabled = hasMacro && !busy && !_chkMacroRepeatForever.Checked;
        _txtMacroRepeatDelay.Enabled = hasMacro && !busy;
        _txtMacroStartDelay.Enabled = hasMacro && !busy;
    }

    private void OnSkipMacroMouseMovementChanged()
    {
        if (_suppressUiEvents ||
            _chkSkipMacroMouseMovement.Checked == _settings.SkipMacroMouseMovement)
        {
            return;
        }

        var skipMovement = _chkSkipMacroMouseMovement.Checked;
        try
        {
            _ini.UpdateSections(
            [
                ("Main", new List<KeyValuePair<string, string>>
                {
                    new("SkipMacroMouseMovement", skipMovement ? "1" : "0")
                })
            ], flushToDisk: false);
            _settings.SkipMacroMouseMovement = skipMovement;
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroMovementSettingSaveFailed error={ex.GetType().Name}");
            _suppressUiEvents = true;
            try
            {
                _chkSkipMacroMouseMovement.Checked = _settings.SkipMacroMouseMovement;
            }
            finally
            {
                _suppressUiEvents = false;
            }

            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
        }
    }

    private Label CreateMacroSettingLabel(string text, int left, int top, int width)
    {
        var label = new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = UiTheme.TextSoft,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        return label;
    }

    private static void ConfigureMacroValueEditor(PillValueEditor editor)
    {
        editor.Height = 32;
        editor.Font = UiTheme.CreateFont("Segoe UI", 11.5f);
        editor.Radius = 9;
    }

    private void SetMacroPlaybackSettingsVisible(bool visible)
    {
        _lblMacroRepeat.Visible = visible;
        _txtMacroRepeatCount.Visible = visible;
        _lblMacroLoop.Visible = visible;
        _chkMacroRepeatForever.Visible = visible;
        _lblMacroRepeatDelay.Visible = visible;
        _txtMacroRepeatDelay.Visible = visible;
        _lblMacroMilliseconds.Visible = visible;
        _lblMacroStartDelay.Visible = visible;
        _txtMacroStartDelay.Visible = visible;
        _lblMacroSeconds.Visible = visible;
        _lblMacroHint.Visible = !visible;
        _lblMacroHint.Top = visible ? 163 : 91;
        _lblMacroHint.Height = visible ? 35 : 98;
    }

    private void SyncMacroPlaybackOptions(MacroDefinition? macro)
    {
        _syncingMacroPlaybackOptions = true;
        try
        {
            _txtMacroRepeatCount.Text = (macro?.RepeatCount ?? 1).ToString();
            _txtMacroRepeatDelay.Text = (macro?.RepeatDelayMilliseconds ?? 0).ToString();
            _txtMacroStartDelay.Text = (macro?.PlaybackStartDelaySeconds ?? 3).ToString();
            _chkMacroRepeatForever.Checked = macro?.RepeatIndefinitely ?? false;
        }
        finally
        {
            _syncingMacroPlaybackOptions = false;
        }
    }

    private void CommitMacroPlaybackOptions()
    {
        if (_syncingMacroPlaybackOptions || _macroSaveInProgress || IsMacroSessionActive())
        {
            return;
        }

        var macro = GetSelectedMacro();
        if (macro is null)
        {
            return;
        }

        var repeatCount = ParseMacroSetting(
            _txtMacroRepeatCount.Text,
            macro.RepeatCount,
            1,
            MacroDefinition.MaximumRepeatCount);
        var repeatDelay = ParseMacroSetting(
            _txtMacroRepeatDelay.Text,
            macro.RepeatDelayMilliseconds,
            0,
            MacroDefinition.MaximumRepeatDelayMilliseconds);
        var repeatIndefinitely = _chkMacroRepeatForever.Checked;
        var startDelay = ParseMacroSetting(
            _txtMacroStartDelay.Text,
            macro.PlaybackStartDelaySeconds,
            0,
            MacroDefinition.MaximumPlaybackStartDelaySeconds);
        var changed = macro.RepeatCount != repeatCount
            || macro.RepeatDelayMilliseconds != repeatDelay
            || macro.RepeatIndefinitely != repeatIndefinitely
            || macro.PlaybackStartDelaySeconds != startDelay;

        if (!changed)
        {
            SyncMacroPlaybackOptions(macro);
            RefreshMacroControls();
            return;
        }

        var previousRepeatCount = macro.RepeatCount;
        var previousRepeatDelay = macro.RepeatDelayMilliseconds;
        var previousRepeatIndefinitely = macro.RepeatIndefinitely;
        var previousStartDelay = macro.PlaybackStartDelaySeconds;
        var previousUpdatedUtc = macro.UpdatedUtc;
        macro.RepeatCount = repeatCount;
        macro.RepeatDelayMilliseconds = repeatDelay;
        macro.RepeatIndefinitely = repeatIndefinitely;
        macro.PlaybackStartDelaySeconds = startDelay;
        macro.UpdatedUtc = DateTime.UtcNow;
        try
        {
            _macroStorage.Save(macro);
        }
        catch (Exception ex)
        {
            macro.RepeatCount = previousRepeatCount;
            macro.RepeatDelayMilliseconds = previousRepeatDelay;
            macro.RepeatIndefinitely = previousRepeatIndefinitely;
            macro.PlaybackStartDelaySeconds = previousStartDelay;
            macro.UpdatedUtc = previousUpdatedUtc;
            InputDiagnostics.Write($"MacroSettingsSaveFailed id={macro.Id} error={ex.GetType().Name}");
            ThemedMessageDialog.Show(this, L("Macros.Title"), L("Macros.SaveFailed"));
        }

        SyncMacroPlaybackOptions(macro);
        RefreshMacroControls();
    }

    private static int ParseMacroSetting(string text, int fallback, int minimum, int maximum)
    {
        return int.TryParse(text, out var value)
            ? Math.Clamp(value, minimum, maximum)
            : Math.Clamp(fallback, minimum, maximum);
    }

    private bool IsMacroSessionActive()
    {
        return _macroCountdownCts is not null
            || _macroPlaybackCountdownCts is not null
            || _macroRecorder.IsRecording
            || _macroPlayer.IsPlaying;
    }

    private bool IsMacroPlaybackSessionActive()
    {
        return _macroPlaybackCountdownCts is not null || _macroPlayer.IsPlaying;
    }

    private string FormatMacroPreviewLine(MacroEvent macroEvent)
    {
        var time = macroEvent.OffsetMicroseconds / 1_000_000d;
        return $"{time,7:0.000}s  {FormatMacroAction(macroEvent)}";
    }

    private string FormatMacroAction(MacroEvent macroEvent)
    {
        return macroEvent.Type switch
        {
            MacroEventType.Key => L(
                "Macros.EventKey",
                FormatHotkeyDisplay(macroEvent.Token),
                L(macroEvent.IsDown ? "Macros.Pressed" : "Macros.Released")),
            MacroEventType.MouseButton => L(
                "Macros.EventMouseButton",
                FormatHotkeyDisplay(macroEvent.Token),
                L(macroEvent.IsDown ? "Macros.Pressed" : "Macros.Released")),
            MacroEventType.MouseMove => L("Macros.EventMove", macroEvent.X, macroEvent.Y),
            MacroEventType.MouseWheel => L("Macros.EventWheel", macroEvent.WheelDelta),
            _ => L("Macros.EventHorizontalWheel", macroEvent.WheelDelta)
        };
    }

    private static long GetMacroDuration(MacroDefinition macro)
    {
        return macro.Events.Count == 0 ? 0 : macro.Events[^1].OffsetMicroseconds;
    }

    private static string FormatMacroDuration(long microseconds)
    {
        var span = TimeSpan.FromTicks(Math.Max(0, microseconds) * 10);
        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss\.fff")
            : span.ToString(@"m\:ss\.fff");
    }
}
