namespace KofgeClicker;

internal readonly record struct MacroTimelineEdit(
    IReadOnlyList<MacroEvent> Events,
    long[] Offsets);

internal readonly record struct MacroSleepChange(
    MacroEvent Event,
    long BeforeMicroseconds,
    long AfterMicroseconds);

internal readonly record struct MacroCoordinateChange(
    MacroEvent Event,
    int BeforeX,
    int BeforeY,
    int AfterX,
    int AfterY);

internal readonly record struct MacroDeletedEvent(
    int Index,
    MacroEvent Event,
    long SleepBeforeMicroseconds);

internal sealed class MacroPairValidationResult
{
    internal MacroPairValidationResult(HashSet<MacroEvent> problemEvents)
    {
        ProblemEvents = problemEvents;
    }

    internal HashSet<MacroEvent> ProblemEvents { get; }
    internal int ProblemCount => ProblemEvents.Count;
}

internal static class MacroJournalEditor
{
    internal const int MaximumSleepMilliseconds = 600_000;
    internal const long MaximumDurationMicroseconds = MacroDefinition.MaximumDurationMicroseconds;

    internal static long[] CreateOffsets(MacroDefinition macro)
    {
        var offsets = new long[macro.Events.Count];
        long previous = 0;
        for (var index = 0; index < offsets.Length; index++)
        {
            var offset = macro.Events[index].OffsetMicroseconds;
            if (offset < previous || offset > MaximumDurationMicroseconds)
            {
                throw new InvalidDataException("The recording contains invalid event timing.");
            }

            offsets[index] = offset;
            previous = offset;
        }

        return offsets;
    }

    internal static long GetSleepBefore(IReadOnlyList<long> offsets, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= offsets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return offsets[index] - (index == 0 ? 0 : offsets[index - 1]);
    }

    internal static bool SetSleepBeforeMany(
        long[] offsets, IReadOnlyList<int> indices, long sleepMicroseconds)
    {
        if (sleepMicroseconds < 0 || sleepMicroseconds > MaximumSleepMilliseconds * 1_000L)
        {
            throw new ArgumentOutOfRangeException(nameof(sleepMicroseconds));
        }

        long totalShift = 0;
        var previousIndex = -1;
        var changed = false;
        foreach (var index in indices)
        {
            if (index <= previousIndex || index >= offsets.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(indices));
            }

            var shift = sleepMicroseconds - GetSleepBefore(offsets, index);
            totalShift += shift;
            changed |= shift != 0;
            previousIndex = index;
        }

        if (!changed)
        {
            return false;
        }

        if (totalShift > 0 && offsets[^1] > MaximumDurationMicroseconds - totalShift)
        {
            throw new InvalidDataException("The edited recording would be too long.");
        }

        long cumulativeShift = 0;
        long previousOffset = 0;
        var selectedPosition = 0;
        for (var eventIndex = 0; eventIndex < offsets.Length; eventIndex++)
        {
            var originalOffset = offsets[eventIndex];
            if (selectedPosition < indices.Count && eventIndex == indices[selectedPosition])
            {
                cumulativeShift += sleepMicroseconds - (originalOffset - previousOffset);
                selectedPosition++;
            }

            offsets[eventIndex] = originalOffset + cumulativeShift;
            previousOffset = originalOffset;
        }

        return true;
    }

    internal static long[] ApplySleepChanges(
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<long> offsets,
        IReadOnlyList<MacroSleepChange> changes,
        bool useAfterValues)
    {
        if (events.Count != offsets.Count)
        {
            throw new ArgumentException("The event and timing counts do not match.", nameof(offsets));
        }

        var replacements = new Dictionary<MacroEvent, long>(changes.Count);
        foreach (var change in changes)
        {
            replacements[change.Event] = useAfterValues
                ? change.AfterMicroseconds
                : change.BeforeMicroseconds;
        }

        var sleeps = new long[events.Count];
        var matched = 0;
        for (var index = 0; index < events.Count; index++)
        {
            if (replacements.TryGetValue(events[index], out var replacement))
            {
                sleeps[index] = replacement;
                matched++;
            }
            else
            {
                sleeps[index] = GetSleepBefore(offsets, index);
            }
        }

        if (matched != replacements.Count)
        {
            throw new InvalidDataException("An edited event is no longer present in the recording.");
        }

        return CreateOffsetsFromSleeps(sleeps);
    }

    internal static void ApplyCoordinateChanges(
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<MacroCoordinateChange> changes,
        bool useAfterValues)
    {
        var availableEvents = events.ToHashSet();
        foreach (var change in changes)
        {
            if (!availableEvents.Contains(change.Event))
            {
                throw new InvalidDataException("An edited event is no longer present in the recording.");
            }

            change.Event.X = useAfterValues ? change.AfterX : change.BeforeX;
            change.Event.Y = useAfterValues ? change.AfterY : change.BeforeY;
        }
    }

    internal static MacroTimelineEdit DeleteEvents(
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<long> offsets,
        IReadOnlyList<int> indices)
    {
        if (events.Count != offsets.Count)
        {
            throw new ArgumentException("The event and timing counts do not match.", nameof(offsets));
        }

        var deleted = new bool[events.Count];
        var previousIndex = -1;
        foreach (var index in indices)
        {
            if (index <= previousIndex || index >= events.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(indices));
            }

            deleted[index] = true;
            previousIndex = index;
        }

        var remainingCount = events.Count - indices.Count;
        var remainingEvents = new List<MacroEvent>(remainingCount);
        var remainingOffsets = new long[remainingCount];
        long newOffset = 0;
        var destinationIndex = 0;
        for (var sourceIndex = 0; sourceIndex < events.Count; sourceIndex++)
        {
            if (deleted[sourceIndex])
            {
                continue;
            }

            newOffset += GetSleepBefore(offsets, sourceIndex);
            remainingEvents.Add(events[sourceIndex]);
            remainingOffsets[destinationIndex++] = newOffset;
        }

        return new MacroTimelineEdit(remainingEvents, remainingOffsets);
    }

    internal static MacroTimelineEdit RestoreEvents(
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<long> offsets,
        IReadOnlyList<MacroDeletedEvent> deletedEvents)
    {
        if (events.Count != offsets.Count)
        {
            throw new ArgumentException("The event and timing counts do not match.", nameof(offsets));
        }

        var restoredCount = events.Count + deletedEvents.Count;
        var restoredEvents = new List<MacroEvent>(restoredCount);
        var sleeps = new long[restoredCount];
        var currentIndex = 0;
        var deletedIndex = 0;
        var previousDeletedIndex = -1;

        foreach (var deleted in deletedEvents)
        {
            if (deleted.Index <= previousDeletedIndex || deleted.Index >= restoredCount)
            {
                throw new ArgumentOutOfRangeException(nameof(deletedEvents));
            }

            previousDeletedIndex = deleted.Index;
        }

        for (var destinationIndex = 0; destinationIndex < restoredCount; destinationIndex++)
        {
            if (deletedIndex < deletedEvents.Count &&
                deletedEvents[deletedIndex].Index == destinationIndex)
            {
                var deleted = deletedEvents[deletedIndex++];
                restoredEvents.Add(deleted.Event);
                sleeps[destinationIndex] = deleted.SleepBeforeMicroseconds;
                continue;
            }

            if (currentIndex >= events.Count)
            {
                throw new InvalidDataException("The deleted events cannot be restored.");
            }

            restoredEvents.Add(events[currentIndex]);
            sleeps[destinationIndex] = GetSleepBefore(offsets, currentIndex);
            currentIndex++;
        }

        if (currentIndex != events.Count || deletedIndex != deletedEvents.Count)
        {
            throw new InvalidDataException("The deleted events cannot be restored.");
        }

        return new MacroTimelineEdit(restoredEvents, CreateOffsetsFromSleeps(sleeps));
    }

    internal static MacroTimelineEdit DeleteEventsByReference(
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<long> offsets,
        IReadOnlyList<MacroDeletedEvent> deletedEvents)
    {
        var targets = deletedEvents.Select(item => item.Event).ToHashSet();
        var indices = new List<int>(targets.Count);
        for (var index = 0; index < events.Count; index++)
        {
            if (targets.Contains(events[index]))
            {
                indices.Add(index);
            }
        }

        if (indices.Count != targets.Count)
        {
            throw new InvalidDataException("An event selected for deletion is no longer present.");
        }

        return DeleteEvents(events, offsets, indices);
    }

    internal static MacroPairValidationResult ValidateInputPairs(IReadOnlyList<MacroEvent> events)
    {
        var pressedKeys = new Dictionary<string, MacroEvent>(StringComparer.Ordinal);
        var pressedMouseButtons = new Dictionary<string, MacroEvent>(StringComparer.OrdinalIgnoreCase);
        var problems = new HashSet<MacroEvent>();

        foreach (var macroEvent in events)
        {
            switch (macroEvent.Type)
            {
                case MacroEventType.Key:
                    UpdatePressedState(
                        pressedKeys,
                        GetKeyId(macroEvent),
                        macroEvent,
                        problems);
                    break;
                case MacroEventType.MouseButton:
                    UpdatePressedState(
                        pressedMouseButtons,
                        NormalizeMouseToken(macroEvent.Token),
                        macroEvent,
                        problems);
                    break;
            }
        }

        problems.UnionWith(pressedKeys.Values);
        problems.UnionWith(pressedMouseButtons.Values);
        return new MacroPairValidationResult(problems);
    }

    private static void UpdatePressedState(
        IDictionary<string, MacroEvent> pressed,
        string inputId,
        MacroEvent macroEvent,
        ISet<MacroEvent> problems)
    {
        if (macroEvent.IsDown)
        {
            pressed[inputId] = macroEvent;
            return;
        }

        if (!pressed.Remove(inputId))
        {
            problems.Add(macroEvent);
        }
    }

    private static string GetKeyId(MacroEvent macroEvent)
    {
        var extended = (macroEvent.Flags & NativeMethods.LlkhfExtended) != 0 ? 1 : 0;
        return macroEvent.ScanCode > 0
            ? $"S:{macroEvent.ScanCode}:{extended}"
            : $"V:{macroEvent.VirtualKey}:{extended}";
    }

    private static string NormalizeMouseToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "LButton";
        }

        return token switch
        {
            "Right" => "RButton",
            "Middle" => "MButton",
            "XButton1" => "XButton1",
            "XButton2" => "XButton2",
            _ => token.Equals("RButton", StringComparison.OrdinalIgnoreCase)
                ? "RButton"
                : token.Equals("MButton", StringComparison.OrdinalIgnoreCase)
                    ? "MButton"
                    : token.Equals("XButton1", StringComparison.OrdinalIgnoreCase)
                        ? "XButton1"
                        : token.Equals("XButton2", StringComparison.OrdinalIgnoreCase)
                            ? "XButton2"
                            : "LButton"
        };
    }

    private static long[] CreateOffsetsFromSleeps(IReadOnlyList<long> sleeps)
    {
        var offsets = new long[sleeps.Count];
        long offset = 0;
        for (var index = 0; index < sleeps.Count; index++)
        {
            var sleep = sleeps[index];
            if (sleep < 0 || offset > MaximumDurationMicroseconds - sleep)
            {
                throw new InvalidDataException("The edited recording would be too long.");
            }

            offset += sleep;
            offsets[index] = offset;
        }

        return offsets;
    }

    internal static MacroDefinition CreateEditedCopy(
        MacroDefinition source,
        IReadOnlyList<MacroEvent> events,
        IReadOnlyList<long> offsets)
    {
        if (events.Count != offsets.Count)
        {
            throw new ArgumentException("The event and timing counts do not match.", nameof(offsets));
        }

        var edited = new MacroDefinition
        {
            FormatVersion = source.FormatVersion,
            Id = source.Id,
            Name = source.Name,
            UpdatedUtc = DateTime.UtcNow,
            RepeatCount = source.RepeatCount,
            RepeatDelayMilliseconds = source.RepeatDelayMilliseconds,
            RepeatIndefinitely = source.RepeatIndefinitely,
            PlaybackStartDelaySeconds = source.PlaybackStartDelaySeconds,
            RecordedScreenLeft = source.RecordedScreenLeft,
            RecordedScreenTop = source.RecordedScreenTop,
            RecordedScreenWidth = source.RecordedScreenWidth,
            RecordedScreenHeight = source.RecordedScreenHeight,
            Events = new List<MacroEvent>(events.Count)
        };

        for (var index = 0; index < events.Count; index++)
        {
            var item = events[index];
            edited.Events.Add(new MacroEvent
            {
                Type = item.Type,
                OffsetMicroseconds = offsets[index],
                Token = item.Token,
                IsDown = item.IsDown,
                X = item.X,
                Y = item.Y,
                WheelDelta = item.WheelDelta,
                VirtualKey = item.VirtualKey,
                ScanCode = item.ScanCode,
                Flags = item.Flags,
                Text = item.Text
            });
        }

        return edited;
    }
}
