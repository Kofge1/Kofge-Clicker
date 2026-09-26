using System.Text.Json.Serialization;

namespace KofgeClicker;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MacroEventType
{
    Key,
    MouseButton,
    MouseMove,
    MouseWheel,
    MouseHorizontalWheel
}

internal sealed class MacroEvent
{
    public MacroEventType Type { get; set; }
    public long OffsetMicroseconds { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsDown { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int WheelDelta { get; set; }
    public uint VirtualKey { get; set; }
    public uint ScanCode { get; set; }
    public uint Flags { get; set; }
    public string Text { get; set; } = string.Empty;
}

internal sealed class MacroDefinition
{
    internal const int MaximumEventCount = 250_000;
    internal const long MaximumDurationMicroseconds = 24L * 60 * 60 * 1_000_000;
    internal const int MaximumRepeatCount = 999;
    internal const int MaximumRepeatDelayMilliseconds = 600_000;
    internal const int MaximumPlaybackStartDelaySeconds = 10;

    public int FormatVersion { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public int RepeatCount { get; set; } = 1;
    public int RepeatDelayMilliseconds { get; set; }
    public bool RepeatIndefinitely { get; set; }
    public int PlaybackStartDelaySeconds { get; set; } = 3;
    public bool BackgroundMousePlayback { get; set; }
    public int RecordedScreenLeft { get; set; }
    public int RecordedScreenTop { get; set; }
    public int RecordedScreenWidth { get; set; }
    public int RecordedScreenHeight { get; set; }
    public int RecordedTargetClientLeft { get; set; }
    public int RecordedTargetClientTop { get; set; }
    public int RecordedTargetClientWidth { get; set; }
    public int RecordedTargetClientHeight { get; set; }
    public List<MacroEvent> Events { get; set; } = [];
}
