namespace KofgeClicker;

public sealed partial class MainForm
{
    private CancellationTokenSource? _updateCancellation;

    private void StartUpdateCheck()
    {
        _updateCancellation?.Cancel();
        _updateCancellation?.Dispose();
        _updateCancellation = new CancellationTokenSource();
        _ = CheckForUpdatesOnStartupAsync(_updateCancellation.Token);
    }

    private async Task CheckForUpdatesOnStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var update = await UpdateChecker.CheckForUpdateAsync(AppVersion.Display, cancellationToken);
            if (update is null || IsDisposed || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            AutoUpdater.Log($"Newer release found: current={AppVersion.Display}, latest={update.TagName}");
            var stagedPath = await AutoUpdater.StageUpdateAsync(update, cancellationToken);
            if (string.IsNullOrWhiteSpace(stagedPath)
                || IsDisposed
                || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            BeginInvoke(new Action(() => ShowDownloadedUpdate(update, stagedPath)));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AutoUpdater.Log($"Update check/download failed: {ex.Message}");
        }
    }

    private void ShowDownloadedUpdate(UpdateInfo update, string stagedPath)
    {
        if (IsDisposed || !UpdateChecker.IsNewerVersion(update.TagName, AppVersion.Display))
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            L("Update.ReadyText", update.TagName),
            L("Update.ReadyTitle"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button1);

        if (answer == DialogResult.Yes)
        {
            InstallDownloadedUpdate(stagedPath);
        }
    }

    private void InstallDownloadedUpdate(string stagedPath)
    {
        var targetPath = Environment.ProcessPath;
        if (_isActive || _mouseButtonHeldByClicker.Length > 0 || MouseButtonSafety.HasPressedButtons)
        {
            StopClicking(ClickStopReason.Shutdown, updateStatus: false);
        }

        SaveSettings(syncStartupShortcut: false);
        if (string.IsNullOrWhiteSpace(targetPath)
            || !AutoUpdater.StartInstaller(stagedPath, targetPath, Environment.ProcessId))
        {
            MessageBox.Show(
                this,
                L("Update.FailedText"),
                L("Update.FailedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _allowClose = true;
        _trayIcon.Visible = false;
        Close();
    }

    private void CancelUpdateWork()
    {
        try
        {
            _updateCancellation?.Cancel();
        }
        catch
        {
        }

        _updateCancellation?.Dispose();
        _updateCancellation = null;
    }
}
