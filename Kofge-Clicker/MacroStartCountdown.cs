namespace KofgeClicker;

internal static class MacroStartCountdown
{
    internal static async Task RunAsync(
        MacroDefinition macro,
        Action<int> showRemainingSeconds,
        CancellationToken cancellationToken)
    {
        // Retain the existing JSON field so older recordings keep their setting.
        var seconds = Math.Clamp(macro.PlaybackStartDelaySeconds,
            0, MacroDefinition.MaximumPlaybackStartDelaySeconds);
        for (; seconds > 0; seconds--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            showRemainingSeconds(seconds);
            await Task.Delay(1000, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
