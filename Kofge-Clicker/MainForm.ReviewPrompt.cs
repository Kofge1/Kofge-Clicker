using System.Diagnostics;

namespace KofgeClicker;

public sealed partial class MainForm
{
    private const string ReviewPromptSection = "ReviewPrompt";
    private const int FirstReviewPromptLaunch = 5;
    private const int ReviewPromptLaterDelayLaunches = 10;
    private const int ReviewPromptDelayMs = 8000;
    private readonly CancellationTokenSource _reviewPromptCancellation = new();
    private bool _reviewLaunchRecorded;
    private bool _reviewPromptQueued;
    private bool _reviewPromptHandledThisSession;
    private bool _reviewPromptSuppressedThisSession;
    private volatile bool _startupUpdateCheckCompleted;
    private volatile bool _newerUpdateAvailableThisSession;

    private async void QueueReviewPromptIfEligible()
    {
        if (_reviewLaunchRecorded
            || _reviewPromptQueued
            || _reviewPromptHandledThisSession
            || _reviewPromptSuppressedThisSession
            || IsDisposed
            || !Visible
            || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        _reviewLaunchRecorded = true;
        if (_ini.ReadBool(ReviewPromptSection, "NeverShow", false))
        {
            _reviewPromptHandledThisSession = true;
            return;
        }

        var launchCount = Math.Max(0, _ini.ReadInt(ReviewPromptSection, "EligibleLaunchCount", 0)) + 1;
        var nextPromptLaunch = Math.Max(
            FirstReviewPromptLaunch,
            _ini.ReadInt(ReviewPromptSection, "NextPromptLaunch", FirstReviewPromptLaunch));
        _ini.UpdateSection(
            ReviewPromptSection,
            [
                new("EligibleLaunchCount", launchCount.ToString()),
                new("NextPromptLaunch", nextPromptLaunch.ToString())
            ]);

        if (launchCount < nextPromptLaunch)
        {
            return;
        }

        _reviewPromptQueued = true;
        try
        {
            await Task.Delay(ReviewPromptDelayMs, _reviewPromptCancellation.Token);
            if (!CanShowReviewPrompt())
            {
                return;
            }

            _reviewPromptHandledThisSession = true;
            using var dialog = new ReviewPromptDialog();
            dialog.ShowDialog(this);

            switch (dialog.Choice)
            {
                case ReviewPromptChoice.LeaveReview:
                    _ini.WriteBool(ReviewPromptSection, "NeverShow", true);
                    OpenReviewPage();
                    break;
                case ReviewPromptChoice.NeverShow:
                    _ini.WriteBool(ReviewPromptSection, "NeverShow", true);
                    break;
                default:
                    _ini.WriteInt(
                        ReviewPromptSection,
                        "NextPromptLaunch",
                        launchCount + ReviewPromptLaterDelayLaunches);
                    break;
            }

            Activate();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _reviewPromptQueued = false;
        }
    }

    private bool CanShowReviewPrompt()
    {
        return !IsDisposed
            && !Disposing
            && Visible
            && WindowState != FormWindowState.Minimized
            && Form.ActiveForm == this
            && !_settings.AutoEnabled
            && !_isActive
            && !_isClickingInCurrentContext
            && _recordingTargetName is null
            && _mouseButtonHeldByClicker.Length == 0
            && !MouseButtonSafety.HasPressedButtons
            && _startupUpdateCheckCompleted
            && !_newerUpdateAvailableThisSession;
    }

    private void SuppressReviewPromptForCurrentSession()
    {
        _reviewPromptSuppressedThisSession = true;
    }

    private static void OpenReviewPage()
    {
        var url = LocalizationService.CurrentLanguageCode == LocalizationService.RussianLanguageCode
            ? "https://kofge1.github.io/Kofge-Clicker/ru/#reviews"
            : "https://kofge1.github.io/Kofge-Clicker/#reviews";

        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void CancelReviewPrompt()
    {
        try
        {
            _reviewPromptCancellation.Cancel();
        }
        catch
        {
        }

        _reviewPromptCancellation.Dispose();
    }
}
