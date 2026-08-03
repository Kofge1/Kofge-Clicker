using System.Collections.Concurrent;
using System.Text;

namespace KofgeClicker;

internal static class InputDiagnostics
{
    private const long MaxLogBytes = 256 * 1024;
    private static readonly ConcurrentQueue<string> PendingLines = new();
    private static readonly AutoResetEvent PendingSignal = new(false);
    private static readonly string LogPath = AppPaths.InputDiagnosticsLogPath;

    static InputDiagnostics()
    {
        var writerThread = new Thread(WriterLoop)
        {
            IsBackground = true,
            Name = "Kofge-Clicker diagnostics"
        };
        writerThread.Start();
    }

    internal static void Write(string message)
    {
        PendingLines.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        PendingSignal.Set();
    }

    private static void WriterLoop()
    {
        while (true)
        {
            PendingSignal.WaitOne();
            Thread.Sleep(8);
            try
            {
                var batch = new StringBuilder();
                while (PendingLines.TryDequeue(out var line))
                {
                    batch.Append(line);
                }

                if (batch.Length == 0)
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? AppContext.BaseDirectory);
                RotateIfNeeded();
                File.AppendAllText(LogPath, batch.ToString());
            }
            catch
            {
            }
        }
    }

    private static void RotateIfNeeded()
    {
        var file = new FileInfo(LogPath);
        if (!file.Exists || file.Length <= MaxLogBytes)
        {
            return;
        }

        var oldPath = Path.ChangeExtension(LogPath, ".old.log");
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }

        File.Move(LogPath, oldPath);
    }
}
