namespace KofgeClicker;

static class Program
{
    private static readonly string StartupLogPath = AppPaths.StartupLogPath;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Log("Main start");
            if (AutoUpdater.TryHandleInstallerMode(args, Log))
            {
                return;
            }

            AppRestartHelper.WaitForPreviousInstanceIfRequested(args, Log);

            if (AdminLaunchHelper.RelaunchAsAdministratorIfRequested(Log))
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Log("After ApplicationConfiguration.Initialize");
            var languageIni = new IniFile(AppPaths.SettingsPath);
            LocalizationService.Initialize(StartupLanguageResolver.Resolve(languageIni));
            AutoUpdater.CleanupStaleFiles();
            using var singleInstance = SingleInstanceGuard.TryAcquire();
            if (singleInstance is null)
            {
                Log("Duplicate instance blocked");
                ThemedMessageDialog.Show("Kofge-Clicker", LocalizationService.Get("App.AlreadyRunning"));
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += (_, _) => ReleaseMouseButtonsSafely("ProcessExit");
            Application.ApplicationExit += (_, _) => ReleaseMouseButtonsSafely("ApplicationExit");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Log($"UnhandledException: {e.ExceptionObject}");
                ReleaseMouseButtonsSafely("UnhandledException", forcePrimaryButtons: true);
            };
            Application.ThreadException += (_, e) =>
            {
                Log($"ThreadException: {e.Exception}");
                ReleaseMouseButtonsSafely("ThreadException", forcePrimaryButtons: true);
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log($"UnobservedTaskException: {e.Exception}");
                ReleaseMouseButtonsSafely("UnobservedTaskException", forcePrimaryButtons: true);
                e.SetObserved();
            };

            ReleaseMouseButtonsSafely("Startup");
            using var timerScope = new TimerResolutionScope(1);
            Log("After TimerResolutionScope");
            using var form = new MainForm();
            Log("MainForm constructed");
            Application.Run(form);
            Log("Application.Run finished");
        }
        catch (Exception ex)
        {
            Log($"Fatal: {ex}");
            ReleaseMouseButtonsSafely("FatalCatch", forcePrimaryButtons: true);
            throw;
        }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                StartupLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static void ReleaseMouseButtonsSafely(string source, bool forcePrimaryButtons = false)
    {
        try
        {
            Log($"ReleaseMouseButtons: {source}");
            MouseButtonSafety.ReleaseAllPressedButtons();
            if (forcePrimaryButtons)
            {
                MouseButtonSafety.ForceReleasePrimaryButtons();
                MouseButtonSafety.ForceReleaseSideButtons();
            }
        }
        catch
        {
        }
    }
}
