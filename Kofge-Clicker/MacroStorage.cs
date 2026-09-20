using System.Text.Json;
using System.Text.Json.Serialization;

namespace KofgeClicker;

internal sealed class MacroStorage
{
    private const long MaximumFileSizeBytes = 128L * 1024 * 1024;
    private const int MaximumIdLength = 64;
    private const int MaximumNameLength = 128;
    private const int MaximumTokenLength = 64;
    private const int MaximumTextLength = 64;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _directory;

    internal MacroStorage(string directory)
    {
        _directory = directory;
    }

    internal IReadOnlyList<MacroDefinition> LoadAll()
    {
        if (!Directory.Exists(_directory))
        {
            return [];
        }

        var macros = new List<MacroDefinition>();
        var loadedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var file = new FileInfo(path);
                if (file.Length <= 0 || file.Length > MaximumFileSizeBytes)
                {
                    throw new InvalidDataException("Macro file size is invalid.");
                }

                using var stream = File.OpenRead(path);
                var macro = JsonSerializer.Deserialize<MacroDefinition>(stream, JsonOptions);
                ValidateAndNormalize(macro);
                if (!loadedIds.Add(macro!.Id))
                {
                    throw new InvalidDataException("A duplicate macro identifier was found.");
                }

                macros.Add(macro);
            }
            catch (Exception ex)
            {
                InputDiagnostics.Write($"MacroLoadFailed file={Path.GetFileName(path)} error={ex.GetType().Name}");
            }
        }

        return macros
            .OrderByDescending(macro => macro.UpdatedUtc)
            .ToArray();
    }

    internal void Save(MacroDefinition macro)
    {
        ValidateAndNormalize(macro);
        Directory.CreateDirectory(_directory);
        var destination = GetMacroPath(macro.Id);
        var temporary = destination + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(macro, JsonOptions));
            if (new FileInfo(temporary).Length > MaximumFileSizeBytes)
            {
                throw new InvalidDataException("Macro file is too large.");
            }

            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    internal void Delete(string macroId)
    {
        var path = GetMacroPath(macroId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetMacroPath(string macroId)
    {
        var safeId = new string(macroId.Where(char.IsLetterOrDigit).ToArray());
        if (safeId.Length == 0)
        {
            throw new InvalidDataException("Macro identifier is invalid.");
        }

        return Path.Combine(_directory, $"{safeId}.json");
    }

    private static void ValidateAndNormalize(MacroDefinition? macro)
    {
        if (macro is null || macro.FormatVersion != 1)
        {
            throw new InvalidDataException("Macro format is unsupported.");
        }

        macro.Id = macro.Id?.Trim() ?? string.Empty;
        macro.Name = macro.Name?.Trim() ?? string.Empty;
        if (macro.Id.Length is 0 or > MaximumIdLength || !macro.Id.All(char.IsLetterOrDigit))
        {
            throw new InvalidDataException("Macro identifier is invalid.");
        }

        if (macro.Name.Length is 0 or > MaximumNameLength)
        {
            throw new InvalidDataException("Macro name is invalid.");
        }

        macro.Events ??= [];
        if (macro.Events.Count > MacroDefinition.MaximumEventCount)
        {
            throw new InvalidDataException("Macro contains too many events.");
        }

        long previousOffset = 0;
        foreach (var macroEvent in macro.Events)
        {
            if (macroEvent is null || !Enum.IsDefined(macroEvent.Type))
            {
                throw new InvalidDataException("Macro contains an unsupported event.");
            }

            if (macroEvent.OffsetMicroseconds < previousOffset ||
                macroEvent.OffsetMicroseconds > MacroDefinition.MaximumDurationMicroseconds)
            {
                throw new InvalidDataException("Macro event timing is invalid.");
            }

            previousOffset = macroEvent.OffsetMicroseconds;
            macroEvent.Token ??= string.Empty;
            macroEvent.Text ??= string.Empty;
            if (macroEvent.Token.Length > MaximumTokenLength || macroEvent.Text.Length > MaximumTextLength)
            {
                throw new InvalidDataException("Macro event data is too long.");
            }

            switch (macroEvent.Type)
            {
                case MacroEventType.Key:
                    if (string.IsNullOrWhiteSpace(macroEvent.Token) ||
                        macroEvent.VirtualKey > byte.MaxValue ||
                        macroEvent.ScanCode > byte.MaxValue)
                    {
                        throw new InvalidDataException("Macro contains an invalid keyboard event.");
                    }

                    break;
                case MacroEventType.MouseButton:
                    macroEvent.Token = NormalizeMouseButtonToken(macroEvent.Token);
                    break;
                case MacroEventType.MouseMove:
                case MacroEventType.MouseWheel:
                case MacroEventType.MouseHorizontalWheel:
                    macroEvent.Token = string.Empty;
                    break;
            }
        }

        macro.RepeatCount = Math.Clamp(macro.RepeatCount, 1, MacroDefinition.MaximumRepeatCount);
        macro.RepeatDelayMilliseconds = Math.Clamp(
            macro.RepeatDelayMilliseconds,
            0,
            MacroDefinition.MaximumRepeatDelayMilliseconds);
        macro.PlaybackStartDelaySeconds = Math.Clamp(
            macro.PlaybackStartDelaySeconds,
            0,
            MacroDefinition.MaximumPlaybackStartDelaySeconds);
    }

    private static string NormalizeMouseButtonToken(string token)
    {
        if (token.Equals("LButton", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("Left", StringComparison.OrdinalIgnoreCase))
        {
            return "LButton";
        }

        if (token.Equals("RButton", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("Right", StringComparison.OrdinalIgnoreCase))
        {
            return "RButton";
        }

        if (token.Equals("MButton", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("Middle", StringComparison.OrdinalIgnoreCase))
        {
            return "MButton";
        }

        if (token.Equals("XButton1", StringComparison.OrdinalIgnoreCase))
        {
            return "XButton1";
        }

        if (token.Equals("XButton2", StringComparison.OrdinalIgnoreCase))
        {
            return "XButton2";
        }

        throw new InvalidDataException("Macro contains an invalid mouse button event.");
    }
}
