namespace KofgeClicker;

public sealed partial class MainForm
{
    private void QueueWhatsNewDialog()
    {
        if (_whatsNewDialogHandled
            || _whatsNewDialogQueued
            || !_startupCompleted
            || IsDisposed
            || !Visible
            || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        _whatsNewDialogQueued = true;
        BeginInvoke(new Action(ShowWhatsNewDialogIfNeeded));
    }

    private void ShowWhatsNewDialogIfNeeded()
    {
        _whatsNewDialogQueued = false;
        if (_whatsNewDialogHandled
            || IsDisposed
            || !Visible
            || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        const string settingKey = "LastSeenWhatsNewVersion";
        var lastSeenVersion = _ini.ReadString("App", settingKey, string.Empty);
        if (UpdateChecker.VersionsEqual(lastSeenVersion, AppVersion.Display)
            || UpdateChecker.IsNewerVersion(lastSeenVersion, AppVersion.Display))
        {
            _whatsNewDialogHandled = true;
            QueueReviewPromptIfEligible();
            return;
        }

        _whatsNewDialogHandled = true;

        WhatsNewItem[] items =
        [
            new(L("WhatsNew.ReviewPromptTitle"), L("WhatsNew.ReviewPromptText")),
            new(L("WhatsNew.ReviewLinkTitle"), L("WhatsNew.ReviewLinkText"))
        ];

        using var dialog = new WhatsNewDialog(AppVersion.Display, items);
        dialog.ShowDialog(this);
        _ini.WriteString("App", settingKey, AppVersion.Display);
        Activate();
    }
}
