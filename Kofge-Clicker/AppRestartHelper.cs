using System.Diagnostics;

namespace KofgeClicker;

internal static class AppRestartHelper
{
    private const string RestartArgument = "--restart-after";

    internal static void WaitForPreviousInstanceIfRequested(string[] args, Action<string> log)
    {
        var argumentIndex = Array.FindIndex(
            args,
            argument => string.Equals(argument, RestartArgument, StringComparison.OrdinalIgnoreCase));
        if (argumentIndex < 0
            || argumentIndex + 1 >= args.Length
            || !int.TryParse(args[argumentIndex + 1], out var processId)
            || processId <= 0)
        {
            return;
        }

        try
        {
            using var previousProcess = Process.GetProcessById(processId);
            previousProcess.WaitForExit(10_000);
            log($"Language restart wait completed for PID {processId}");
        }
        catch (ArgumentException)
        {
        }
        catch (Exception ex)
        {
            log($"Language restart wait failed: {ex.Message}");
        }
    }

    internal static bool TryStartRestart(int currentProcessId)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(processPath)
            {
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
            startInfo.ArgumentList.Add(RestartArgument);
            startInfo.ArgumentList.Add(currentProcessId.ToString());
            return Process.Start(startInfo) is not null;
        }
        catch
        {
            return false;
        }
    }
}
