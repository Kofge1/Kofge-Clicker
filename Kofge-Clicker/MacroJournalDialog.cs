using System.Globalization;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal sealed class MacroJournalDialog : Form
{
    private const int MaximumHistoryEntries = 100;

    private enum JournalFilter
    {
        All,
        Keyboard,
        MouseButtons,
        MouseMovement,
        MouseWheel
    }

    private interface IJournalHistoryEntry
    {
        int FocusIndex { get; }
        MacroTimelineEdit Undo(IReadOnlyList<MacroEvent> events, IReadOnlyList<long> offsets);
        MacroTimelineEdit Redo(IReadOnlyList<MacroEvent> events, IReadOnlyList<long> offsets);
    }

    private sealed record SleepHistoryEntry(
        MacroSleepChange[] Changes,
        int FocusIndex) : IJournalHistoryEntry
    {
        public MacroTimelineEdit Undo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            return new MacroTimelineEdit(
                events,
                MacroJournalEditor.ApplySleepChanges(events, offsets, Changes, useAfterValues: false));
        }

        public MacroTimelineEdit Redo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            return new MacroTimelineEdit(
                events,
                MacroJournalEditor.ApplySleepChanges(events, offsets, Changes, useAfterValues: true));
        }
    }

    private sealed record CoordinateHistoryEntry(
        MacroCoordinateChange[] Changes,
        int FocusIndex) : IJournalHistoryEntry
    {
        public MacroTimelineEdit Undo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            MacroJournalEditor.ApplyCoordinateChanges(events, Changes, useAfterValues: false);
            return new MacroTimelineEdit(events, offsets.ToArray());
        }

        public MacroTimelineEdit Redo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            MacroJournalEditor.ApplyCoordinateChanges(events, Changes, useAfterValues: true);
            return new MacroTimelineEdit(events, offsets.ToArray());
        }
    }

    private sealed record DeleteHistoryEntry(
        MacroDeletedEvent[] DeletedEvents,
        int FocusIndex) : IJournalHistoryEntry
    {
        public MacroTimelineEdit Undo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            return MacroJournalEditor.RestoreEvents(events, offsets, DeletedEvents);
        }

        public MacroTimelineEdit Redo(
            IReadOnlyList<MacroEvent> events,
            IReadOnlyList<long> offsets)
        {
            return MacroJournalEditor.DeleteEventsByReference(events, offsets, DeletedEvents);
        }
    }

    private readonly MacroDefinition _source;
    private readonly MacroStorage _storage;
    private readonly HoverTooltipService _hoverTooltips;
    private readonly List<MacroEvent> _events;
    private readonly long[] _originalOffsets;
    private readonly Dictionary<MacroEvent, long> _originalSleeps = [];
    private readonly Dictionary<MacroEvent, Point> _originalCoordinates = [];
    private readonly Func<MacroEvent, string> _formatAction;
    private readonly MacroJournalListView _timeline;
    private readonly ThemedVerticalScrollBar _timelineScrollbar;
    private readonly PillDropdown _filterDropdown;
    private readonly Font _headerFont = UiTheme.CreateFont("Segoe UI Semibold", 11.5f, FontStyle.Bold);
    private readonly SolidBrush _headerBackgroundBrush = new(UiTheme.SurfaceAlt);
    private readonly SolidBrush _rowBackgroundBrush = new(UiTheme.Surface);
    private readonly SolidBrush _selectedRowBackgroundBrush = new(UiTheme.AccentSecondary);
    private readonly Pen _rowDividerPen = new(UiTheme.BorderSoft);
    private readonly TextBox _sleepInput;
    private readonly TextBox _xInput;
    private readonly TextBox _yInput;
    private readonly AccentButton _applyButton;
    private readonly AccentButton _applyCoordinatesButton;
    private readonly AccentButton _deleteButton;
    private readonly AccentButton _undoButton;
    private readonly AccentButton _redoButton;
    private readonly AccentButton _resetButton;
    private readonly AccentButton _saveButton;
    private readonly Label _summaryLabel;
    private readonly Label _statusLabel;
    private readonly HashSet<MacroEvent> _editedSleepEvents = [];
    private readonly HashSet<MacroEvent> _editedCoordinateEvents = [];
    private readonly List<IJournalHistoryEntry> _history = [];
    private long[] _offsets;
    private int[] _visibleIndices = [];
    private int[] _selectedIndices = [];
    private MacroPairValidationResult _pairValidation = new([]);
    private JournalFilter _filter;
    private int _historyPosition;
    private bool _changingFilter;
    private bool _selectionUpdateQueued;
    private bool _suppressSelectionEvents;
    private bool _settingSleepText;
    private bool _settingCoordinateText;
    private bool _sleepEdited;
    private bool _xEdited;
    private bool _yEdited;
    private bool _saving;
    private bool _syncingTimelineScroll;

    internal MacroDefinition? SavedMacro { get; private set; }

    internal MacroJournalDialog(
        MacroDefinition source,
        MacroStorage storage,
        Func<MacroEvent, string> formatAction)
    {
        _source = source;
        _storage = storage;
        _formatAction = formatAction;
        _events = CloneEvents(source.Events);
        _visibleIndices = Enumerable.Range(0, _events.Count).ToArray();
        _pairValidation = MacroJournalEditor.ValidateInputPairs(_events);
        _originalOffsets = MacroJournalEditor.CreateOffsets(source);
        _offsets = (long[])_originalOffsets.Clone();
        InitializeOriginalEventState(_events);

        Text = LocalizationService.Get("Macros.JournalTitle");
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(920, 675);
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextPrimary;
        KeyPreview = true;

        var shell = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Radius = 22,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = Color.FromArgb(83, 103, 143),
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        var title = CreateLabel(
            LocalizationService.Get("Macros.JournalTitle"),
            28, 18, 864, 33, UiTheme.TextPrimary, 17f, bold: true);
        var subtitle = CreateLabel(
            LocalizationService.Get("Macros.JournalSubtitle"),
            28, 58, 864, 40, UiTheme.TextSoft, 12f);

        var listFrame = new RoundedPanel
        {
            Left = 28,
            Top = 108,
            Width = 864,
            Height = 332,
            Radius = 16,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        var filterLabel = CreateLabel(
            LocalizationService.Get("Macros.JournalFilter"),
            12, 8, 70, 34, UiTheme.TextSoft, 11.5f);
        _filterDropdown = new PillDropdown
        {
            Left = 82,
            Top = 7,
            Width = 270,
            Height = 36,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            Radius = 10
        };
        _filterDropdown.SetItems(
        [
            LocalizationService.Get("Macros.JournalFilterAll"),
            LocalizationService.Get("Macros.JournalFilterKeyboard"),
            LocalizationService.Get("Macros.JournalFilterMouseButtons"),
            LocalizationService.Get("Macros.JournalFilterMovement"),
            LocalizationService.Get("Macros.JournalFilterWheel")
        ]);
        _filterDropdown.SelectedIndex = 0;
        _filterDropdown.SelectedIndexChanged += FilterChanged;

        _undoButton = CreateButton(
            LocalizationService.Get("Macros.JournalUndo"),
            570, 6, 128, primary: false, (_, _) => UndoJournalChange());
        _undoButton.Height = 36;
        _redoButton = CreateButton(
            LocalizationService.Get("Macros.JournalRedo"),
            712, 6, 128, primary: false, (_, _) => RedoJournalChange());
        _redoButton.Height = 36;

        var timelineViewport = new Panel
        {
            Left = 12,
            Top = 50,
            Width = 816,
            Height = 270,
            BackColor = UiTheme.Surface
        };
        var nativeScrollbarMaskWidth = Math.Max(
            24,
            SystemInformation.VerticalScrollBarWidth + 7);
        _timeline = new MacroJournalListView
        {
            Left = 0,
            Top = 0,
            Width = timelineViewport.Width,
            Height = timelineViewport.Height,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 11.5f),
            View = View.Details,
            VirtualMode = true,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = true,
            OwnerDraw = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BorderStyle = BorderStyle.None
        };
        _timeline.Columns.Add(LocalizationService.Get("Macros.SleepColumnNumber"), 50);
        _timeline.Columns.Add(LocalizationService.Get("Macros.SleepColumnTime"), 100);
        _timeline.Columns.Add(LocalizationService.Get("Macros.SleepColumnDelay"), 100);
        _timeline.Columns.Add("X", 65);
        _timeline.Columns.Add("Y", 65);
        _timeline.Columns.Add(
            LocalizationService.Get("Macros.SleepColumnAction"),
            timelineViewport.Width - nativeScrollbarMaskWidth - 380);
        _timeline.RetrieveVirtualItem += RetrieveEvent;
        _timeline.DrawColumnHeader += DrawColumnHeader;
        _timeline.DrawItem += (_, _) => { };
        _timeline.DrawSubItem += DrawSubItem;
        _timeline.SelectedIndexChanged += QueueSelectionUpdate;
        _timeline.KeyDown += TimelineKeyDown;
        _timeline.VirtualListSize = _visibleIndices.Length;
        _timelineScrollbar = new ThemedVerticalScrollBar
        {
            Left = 836,
            Top = 50,
            Width = 16,
            Height = 270
        };
        _timelineScrollbar.ValueChanged += TimelineScrollbarValueChanged;
        _timeline.ScrollPositionChanged += (_, _) => UpdateTimelineScrollbar();
        var nativeScrollbarHeaderMask = new Panel
        {
            Left = timelineViewport.Width - nativeScrollbarMaskWidth,
            Top = 0,
            Width = nativeScrollbarMaskWidth,
            Height = 24,
            BackColor = UiTheme.SurfaceAlt
        };
        var nativeScrollbarBodyMask = new Panel
        {
            Left = timelineViewport.Width - nativeScrollbarMaskWidth,
            Top = 24,
            Width = nativeScrollbarMaskWidth,
            Height = timelineViewport.Height - 24,
            BackColor = UiTheme.Surface
        };
        listFrame.Controls.Add(filterLabel);
        listFrame.Controls.Add(_filterDropdown);
        listFrame.Controls.Add(_undoButton);
        listFrame.Controls.Add(_redoButton);
        timelineViewport.Controls.Add(_timeline);
        timelineViewport.Controls.Add(nativeScrollbarHeaderMask);
        timelineViewport.Controls.Add(nativeScrollbarBodyMask);
        nativeScrollbarHeaderMask.BringToFront();
        nativeScrollbarBodyMask.BringToFront();
        listFrame.Controls.Add(timelineViewport);
        listFrame.Controls.Add(_timelineScrollbar);

        var editFrame = new RoundedPanel
        {
            Left = 28,
            Top = 451,
            Width = 864,
            Height = 101,
            Radius = 14,
            FillColor = UiTheme.Surface,
            BackColor = UiTheme.Surface,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };

        var editLabel = CreateLabel(
            LocalizationService.Get("Macros.JournalSleepLabel"),
            17, 12, 86, 28, UiTheme.TextPrimary, 12f);
        var inputFrame = new RoundedPanel
        {
            Left = 108,
            Top = 10,
            Width = 100,
            Height = 38,
            Radius = 10,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };
        _sleepInput = new TextBox
        {
            Left = 8,
            Top = 8,
            Width = 84,
            Height = 23,
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            MaxLength = 12,
            BackColor = UiTheme.CardOuter,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 13f)
        };
        _sleepInput.TextChanged += (_, _) =>
        {
            if (_settingSleepText)
            {
                return;
            }

            _sleepEdited = true;
            UpdateButtons();
        };
        inputFrame.Controls.Add(_sleepInput);

        _applyButton = CreateButton(
            LocalizationService.Get("Macros.SleepApply"),
            218, 10, 160, primary: false, (_, _) => ApplyCurrentSleep());

        var coordinateLabel = CreateLabel(
            LocalizationService.Get("Macros.JournalCoordinates"),
            390, 12, 110, 28, UiTheme.TextPrimary, 12f);
        var xFrame = CreateCoordinateInputFrame("X", 505, out _xInput);
        var yFrame = CreateCoordinateInputFrame("Y", 598, out _yInput);
        _xInput.TextChanged += (_, _) => CoordinateInputChanged(isX: true);
        _yInput.TextChanged += (_, _) => CoordinateInputChanged(isX: false);
        _applyCoordinatesButton = CreateButton(
            LocalizationService.Get("Macros.JournalApplyCoordinates"),
            692, 10, 160, primary: false, (_, _) => ApplyCurrentCoordinates());

        _deleteButton = CreateButton(
            LocalizationService.Get("Macros.JournalDelete"),
            615, 55, 229, primary: false, (_, _) => DeleteSelectedEvents());
        var helpLabel = CreateLabel(
            LocalizationService.Get("Macros.JournalEditorHint"),
            17, 57, 580, 34, UiTheme.TextSoft, 10.5f);

        editFrame.Controls.Add(editLabel);
        editFrame.Controls.Add(inputFrame);
        editFrame.Controls.Add(_applyButton);
        editFrame.Controls.Add(coordinateLabel);
        editFrame.Controls.Add(xFrame);
        editFrame.Controls.Add(yFrame);
        editFrame.Controls.Add(_applyCoordinatesButton);
        editFrame.Controls.Add(_deleteButton);
        editFrame.Controls.Add(helpLabel);

        _summaryLabel = CreateLabel(string.Empty, 29, 559, 863, 24, UiTheme.TextSoft, 11.5f);
        _statusLabel = CreateLabel(string.Empty, 29, 585, 863, 24, UiTheme.TextSoft, 11.5f);
        _resetButton = CreateButton(
            LocalizationService.Get("Macros.JournalReset"),
            438, 621, 150, primary: false, (_, _) => ResetChanges());
        _saveButton = CreateButton(
            LocalizationService.Get("Macros.SleepSave"),
            604, 621, 140, primary: true, async (_, _) => await SaveChangesAsync());
        var cancelButton = CreateButton(
            LocalizationService.Get("Common.Cancel"),
            760, 621, 132, primary: false, (_, _) => Close());

        shell.Controls.Add(title);
        shell.Controls.Add(subtitle);
        shell.Controls.Add(listFrame);
        shell.Controls.Add(editFrame);
        shell.Controls.Add(_summaryLabel);
        shell.Controls.Add(_statusLabel);
        shell.Controls.Add(_resetButton);
        shell.Controls.Add(_saveButton);
        shell.Controls.Add(cancelButton);
        Controls.Add(shell);

        _hoverTooltips = new HoverTooltipService(this);
        _hoverTooltips.SetTooltip(_filterDropdown, LocalizationService.Get("Tooltips.JournalFilter"));
        _hoverTooltips.SetTooltip(_timeline, LocalizationService.Get("Tooltips.JournalTimeline"));
        _hoverTooltips.SetTooltip(_timelineScrollbar, LocalizationService.Get("Tooltips.JournalScrollbar"));
        _hoverTooltips.SetTooltip(_undoButton, LocalizationService.Get("Tooltips.JournalUndo"));
        _hoverTooltips.SetTooltip(_redoButton, LocalizationService.Get("Tooltips.JournalRedo"));
        _hoverTooltips.SetTooltip(_sleepInput, LocalizationService.Get("Tooltips.JournalSleep"));
        _hoverTooltips.SetTooltip(_applyButton, LocalizationService.Get("Tooltips.JournalApplySleep"));
        _hoverTooltips.SetTooltip(_xInput, LocalizationService.Get("Tooltips.JournalX"));
        _hoverTooltips.SetTooltip(_yInput, LocalizationService.Get("Tooltips.JournalY"));
        _hoverTooltips.SetTooltip(_applyCoordinatesButton, LocalizationService.Get("Tooltips.JournalApplyCoordinates"));
        _hoverTooltips.SetTooltip(_deleteButton, LocalizationService.Get("Tooltips.JournalDelete"));
        _hoverTooltips.SetTooltip(_resetButton, LocalizationService.Get("Tooltips.JournalReset"));
        _hoverTooltips.SetTooltip(_saveButton, LocalizationService.Get("Tooltips.JournalSave"));
        _hoverTooltips.SetTooltip(cancelButton, LocalizationService.Get("Tooltips.JournalCancel"));

        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            WindowPlacement.ClampToWorkingArea(this);
            ApplyRoundedRegion();
            SelectTimelineIndex(0);
            UpdateTimelineScrollbar();
            UpdateJournalStatus();
        };
        SizeChanged += (_, _) => ApplyRoundedRegion();
        UpdateJournalStatus();
    }

    private static Label CreateLabel(
        string text, int left, int top, int width, int height,
        Color color, float fontSize, bool bold = false)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = color,
            Font = UiTheme.CreateFont(
                bold ? "Segoe UI Semibold" : "Segoe UI",
                fontSize,
                bold ? FontStyle.Bold : FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static AccentButton CreateButton(
        string text, int left, int top, int width, bool primary, EventHandler onClick)
    {
        var button = new AccentButton
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 38,
            Primary = primary,
            UseFilledBorderRing = true
        };
        button.Click += onClick;
        return button;
    }

    private static RoundedPanel CreateCoordinateInputFrame(
        string axis,
        int left,
        out TextBox input)
    {
        var frame = new RoundedPanel
        {
            Left = left,
            Top = 10,
            Width = 88,
            Height = 38,
            Radius = 10,
            FillColor = UiTheme.CardOuter,
            BackColor = UiTheme.CardOuter,
            BorderColor = UiTheme.BorderSoft,
            DrawShadow = false,
            UseAntialiasedEdges = true
        };
        var axisLabel = CreateLabel(
            axis, 7, 7, 17, 24, UiTheme.TextSoft, 11.5f, bold: true);
        input = new TextBox
        {
            Left = 25,
            Top = 8,
            Width = 55,
            Height = 23,
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            MaxLength = 11,
            BackColor = UiTheme.CardOuter,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.CreateFont("Segoe UI", 12.5f),
            Enabled = false
        };
        frame.Controls.Add(axisLabel);
        frame.Controls.Add(input);
        return frame;
    }

    private void RetrieveEvent(object? sender, RetrieveVirtualItemEventArgs e)
    {
        var index = _visibleIndices[e.ItemIndex];
        var item = new ListViewItem((index + 1).ToString(CultureInfo.CurrentCulture));
        item.SubItems.Add(FormatTime(_offsets[index]));
        item.SubItems.Add(FormatSleep(MacroJournalEditor.GetSleepBefore(_offsets, index)));
        var macroEvent = _events[index];
        item.SubItems.Add(SupportsCoordinates(macroEvent)
            ? macroEvent.X.ToString(CultureInfo.CurrentCulture)
            : "-");
        item.SubItems.Add(SupportsCoordinates(macroEvent)
            ? macroEvent.Y.ToString(CultureInfo.CurrentCulture)
            : "-");
        item.SubItems.Add(_formatAction(macroEvent));
        e.Item = item;
    }

    private static string FormatTime(long offsetMicroseconds)
    {
        var duration = TimeSpan.FromTicks(offsetMicroseconds * 10);
        if (duration.Days > 0)
        {
            return duration.ToString(@"d\.hh\:mm\:ss\.fff", CultureInfo.CurrentCulture);
        }

        return duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss\.fff", CultureInfo.CurrentCulture)
            : duration.ToString(@"m\:ss\.fff", CultureInfo.CurrentCulture);
    }

    private static string FormatSleep(long microseconds)
    {
        return (microseconds / 1_000m).ToString("0.###", CultureInfo.CurrentCulture);
    }

    private void DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        e.Graphics.FillRectangle(_headerBackgroundBrush, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            e.Header?.Text ?? string.Empty,
            _headerFont,
            Rectangle.Inflate(e.Bounds, -10, 0),
            UiTheme.TextSoft,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private void DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        var selected = e.Item?.Selected == true;
        var modelIndex = e.ItemIndex >= 0 && e.ItemIndex < _visibleIndices.Length
            ? _visibleIndices[e.ItemIndex]
            : -1;
        var hasPairWarning = modelIndex >= 0 &&
            _pairValidation.ProblemEvents.Contains(_events[modelIndex]);
        e.Graphics.FillRectangle(
            selected ? _selectedRowBackgroundBrush : _rowBackgroundBrush,
            e.Bounds);
        e.Graphics.DrawLine(
            _rowDividerPen,
            e.Bounds.Left,
            e.Bounds.Bottom - 1,
            e.Bounds.Right,
            e.Bounds.Bottom - 1);
        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem?.Text ?? string.Empty,
            e.Item?.Font ?? UiTheme.SmallFont,
            Rectangle.Inflate(e.Bounds, -10, 0),
            hasPairWarning && !selected ? Color.FromArgb(245, 190, 110) :
                selected ? UiTheme.TextPrimary : UiTheme.TextSoft,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private void QueueSelectionUpdate(object? sender, EventArgs e)
    {
        if (_suppressSelectionEvents || _selectionUpdateQueued ||
            !IsHandleCreated || IsDisposed || Disposing)
        {
            return;
        }

        _selectionUpdateQueued = true;
        BeginInvoke(SynchronizeSelection);
    }

    private void SynchronizeSelection()
    {
        _selectionUpdateQueued = false;
        if (IsDisposed || Disposing)
        {
            return;
        }

        var selectedIndices = GetSelectedIndices();
        if (selectedIndices.SequenceEqual(_selectedIndices))
        {
            return;
        }

        _selectedIndices = selectedIndices;
        if (_sleepEdited || _xEdited || _yEdited)
        {
            UpdateJournalStatus();
            return;
        }

        SynchronizeEditorsFromSelection();
        UpdateJournalStatus();
    }

    private int[] GetSelectedIndices()
    {
        var selectedIndices = new int[_timeline.SelectedIndices.Count];
        for (var index = 0; index < selectedIndices.Length; index++)
        {
            selectedIndices[index] = _visibleIndices[_timeline.SelectedIndices[index]];
        }

        Array.Sort(selectedIndices);
        return selectedIndices;
    }

    private void SetSleepInput(string text, string placeholder = "")
    {
        _settingSleepText = true;
        try
        {
            _sleepInput.Text = text;
            _sleepInput.PlaceholderText = placeholder;
            _sleepEdited = false;
        }
        finally
        {
            _settingSleepText = false;
        }

        UpdateButtons();
    }

    private void SetCoordinateInputs(
        string xText,
        string yText,
        bool enabled,
        string xPlaceholder = "",
        string yPlaceholder = "")
    {
        _settingCoordinateText = true;
        try
        {
            _xInput.Text = xText;
            _xInput.PlaceholderText = xPlaceholder;
            _yInput.Text = yText;
            _yInput.PlaceholderText = yPlaceholder;
            _xInput.Enabled = enabled && !_saving;
            _yInput.Enabled = enabled && !_saving;
            _xEdited = false;
            _yEdited = false;
        }
        finally
        {
            _settingCoordinateText = false;
        }

        UpdateButtons();
    }

    private void CoordinateInputChanged(bool isX)
    {
        if (_settingCoordinateText)
        {
            return;
        }

        if (isX)
        {
            _xEdited = true;
        }
        else
        {
            _yEdited = true;
        }

        UpdateButtons();
    }

    private void SynchronizeEditorsFromSelection()
    {
        var firstSleep = _selectedIndices.Length == 0
            ? 0
            : MacroJournalEditor.GetSleepBefore(_offsets, _selectedIndices[0]);
        var mixedSleep = _selectedIndices.Any(index =>
            MacroJournalEditor.GetSleepBefore(_offsets, index) != firstSleep);
        SetSleepInput(
            _selectedIndices.Length == 0 || mixedSleep ? string.Empty : FormatSleep(firstSleep),
            mixedSleep ? LocalizationService.Get("Macros.SleepMixedValues") : string.Empty);

        SynchronizeCoordinateInputsFromSelection();
    }

    private void SynchronizeCoordinateInputsFromSelection()
    {
        var coordinateIndices = GetSelectedCoordinateIndices();
        if (coordinateIndices.Length == 0)
        {
            SetCoordinateInputs(string.Empty, string.Empty, enabled: false);
            return;
        }

        var firstEvent = _events[coordinateIndices[0]];
        var mixedX = coordinateIndices.Any(index => _events[index].X != firstEvent.X);
        var mixedY = coordinateIndices.Any(index => _events[index].Y != firstEvent.Y);
        var mixedPlaceholder = LocalizationService.Get("Macros.CoordinateMixedValues");
        SetCoordinateInputs(
            mixedX ? string.Empty : firstEvent.X.ToString(CultureInfo.CurrentCulture),
            mixedY ? string.Empty : firstEvent.Y.ToString(CultureInfo.CurrentCulture),
            enabled: true,
            mixedX ? mixedPlaceholder : string.Empty,
            mixedY ? mixedPlaceholder : string.Empty);
    }

    private int[] GetSelectedCoordinateIndices()
    {
        return _selectedIndices
            .Where(index => SupportsCoordinates(_events[index]))
            .ToArray();
    }

    private static bool SupportsCoordinates(MacroEvent macroEvent)
    {
        return macroEvent.Type != MacroEventType.Key;
    }

    private bool ApplyCurrentSleep()
    {
        if (_selectedIndices.Length == 0 || !_sleepEdited)
        {
            return true;
        }

        var text = _sleepInput.Text.Trim();
        const NumberStyles sleepNumberStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;
        if ((!decimal.TryParse(text, sleepNumberStyle, CultureInfo.CurrentCulture, out var milliseconds) &&
             !decimal.TryParse(text, sleepNumberStyle, CultureInfo.InvariantCulture, out milliseconds)) ||
            milliseconds < 0 || milliseconds > MacroJournalEditor.MaximumSleepMilliseconds)
        {
            ShowError(LocalizationService.Get(
                "Macros.SleepInvalid", MacroJournalEditor.MaximumSleepMilliseconds));
            _sleepInput.Focus();
            return false;
        }

        try
        {
            var microseconds = (long)Math.Round(milliseconds * 1_000m, 0, MidpointRounding.AwayFromZero);
            var changes = _selectedIndices
                .Select(index => new MacroSleepChange(
                    _events[index],
                    MacroJournalEditor.GetSleepBefore(_offsets, index),
                    microseconds))
                .Where(change => change.BeforeMicroseconds != change.AfterMicroseconds)
                .ToArray();
            if (MacroJournalEditor.SetSleepBeforeMany(_offsets, _selectedIndices, microseconds))
            {
                AddHistory(new SleepHistoryEntry(changes, _selectedIndices[0]));
                RefreshEditedSleepEvents();
                RefreshPairValidation();
                RedrawVisibleRows();
            }
        }
        catch (InvalidDataException)
        {
            ShowError(LocalizationService.Get("Macros.SleepTooLong"));
            return false;
        }

        SetSleepInput(FormatSleep(
            MacroJournalEditor.GetSleepBefore(_offsets, _selectedIndices[0])));
        UpdateJournalStatus();
        return true;
    }

    private bool ApplyCurrentCoordinates()
    {
        var coordinateIndices = GetSelectedCoordinateIndices();
        if (coordinateIndices.Length == 0 || (!_xEdited && !_yEdited))
        {
            return true;
        }

        int x = 0;
        int y = 0;
        if (_xEdited && !int.TryParse(
                _xInput.Text.Trim(),
                NumberStyles.Integer,
                CultureInfo.CurrentCulture,
                out x))
        {
            ShowError(LocalizationService.Get("Macros.CoordinateInvalid", "X"));
            _xInput.Focus();
            return false;
        }

        if (_yEdited && !int.TryParse(
                _yInput.Text.Trim(),
                NumberStyles.Integer,
                CultureInfo.CurrentCulture,
                out y))
        {
            ShowError(LocalizationService.Get("Macros.CoordinateInvalid", "Y"));
            _yInput.Focus();
            return false;
        }

        var changes = coordinateIndices
            .Select(index =>
            {
                var macroEvent = _events[index];
                return new MacroCoordinateChange(
                    macroEvent,
                    macroEvent.X,
                    macroEvent.Y,
                    _xEdited ? x : macroEvent.X,
                    _yEdited ? y : macroEvent.Y);
            })
            .Where(change => change.BeforeX != change.AfterX || change.BeforeY != change.AfterY)
            .ToArray();
        if (changes.Length > 0)
        {
            MacroJournalEditor.ApplyCoordinateChanges(_events, changes, useAfterValues: true);
            AddHistory(new CoordinateHistoryEntry(changes, coordinateIndices[0]));
            RefreshEditedCoordinateEvents();
            RedrawVisibleRows();
        }

        SynchronizeCoordinateInputsFromSelection();
        UpdateJournalStatus();
        return true;
    }

    private bool ApplyPendingChanges()
    {
        return ApplyCurrentSleep() && ApplyCurrentCoordinates();
    }

    private void DeleteSelectedEvents()
    {
        if (_saving || _selectedIndices.Length == 0)
        {
            return;
        }

        var firstDeletedIndex = _selectedIndices[0];
        var deletedEvents = _selectedIndices
            .Select(index => new MacroDeletedEvent(
                index,
                _events[index],
                MacroJournalEditor.GetSleepBefore(_offsets, index)))
            .ToArray();
        var edited = MacroJournalEditor.DeleteEvents(_events, _offsets, _selectedIndices);
        AddHistory(new DeleteHistoryEntry(deletedEvents, firstDeletedIndex));
        ReplaceTimeline(edited.Events, edited.Offsets);
        RefreshEditedSleepEvents();
        RefreshEditedCoordinateEvents();
        RefreshPairValidation();
        SelectTimelineIndex(Math.Min(firstDeletedIndex, _events.Count - 1));
        UpdateJournalStatus();
    }

    private void ResetChanges()
    {
        if (_saving || !HasUnsavedChanges())
        {
            return;
        }

        var resetEvents = CloneEvents(_source.Events);
        InitializeOriginalEventState(resetEvents);
        ReplaceTimeline(resetEvents, (long[])_originalOffsets.Clone());
        _editedSleepEvents.Clear();
        _editedCoordinateEvents.Clear();
        _history.Clear();
        _historyPosition = 0;
        RefreshPairValidation();
        SelectTimelineIndex(0);
        UpdateJournalStatus();
    }

    private void ReplaceTimeline(IReadOnlyList<MacroEvent> events, long[] offsets)
    {
        _suppressSelectionEvents = true;
        try
        {
            _timeline.SelectedIndices.Clear();
            _timeline.VirtualListSize = 0;
            if (!ReferenceEquals(events, _events))
            {
                _events.Clear();
                _events.AddRange(events);
            }

            _offsets = offsets;
            _selectedIndices = [];
            SetSleepInput(string.Empty);
            SetCoordinateInputs(string.Empty, string.Empty, enabled: false);
            RebuildVisibleIndices();
            _timeline.VirtualListSize = _visibleIndices.Length;
            _timeline.Invalidate();
            UpdateTimelineScrollbar();
        }
        finally
        {
            _suppressSelectionEvents = false;
        }
    }

    private void SelectTimelineIndex(int index)
    {
        if (index < 0 || index >= _events.Count || _visibleIndices.Length == 0)
        {
            _selectedIndices = [];
            SetSleepInput(string.Empty);
            SetCoordinateInputs(string.Empty, string.Empty, enabled: false);
            return;
        }

        var visibleIndex = Array.BinarySearch(_visibleIndices, index);
        if (visibleIndex < 0)
        {
            visibleIndex = Math.Min(~visibleIndex, _visibleIndices.Length - 1);
        }

        _timeline.Items[visibleIndex].Selected = true;
        _timeline.EnsureVisible(visibleIndex);
        UpdateTimelineScrollbar();
        QueueSelectionUpdate(null, EventArgs.Empty);
    }

    private void TimelineScrollbarValueChanged(object? sender, EventArgs e)
    {
        if (_syncingTimelineScroll || _timeline.VirtualListSize == 0)
        {
            return;
        }

        var index = Math.Clamp(
            _timelineScrollbar.Value,
            0,
            _timeline.VirtualListSize - 1);
        _timeline.ScrollToTopIndex(index);
    }

    private void UpdateTimelineScrollbar()
    {
        var visibleRows = _timeline.GetVisibleRowCount();
        if (visibleRows <= 0)
        {
            var viewportHeight = _timeline.Parent?.ClientSize.Height ?? _timeline.ClientSize.Height;
            visibleRows = Math.Max(1, (viewportHeight - 26) / 24);
        }

        var maximum = Math.Max(0, _timeline.VirtualListSize - visibleRows);
        var topIndex = 0;
        if (_timeline.VirtualListSize > 0)
        {
            try
            {
                topIndex = Math.Clamp(_timeline.TopItem?.Index ?? 0, 0, maximum);
            }
            catch (InvalidOperationException)
            {
                topIndex = 0;
            }
        }

        _syncingTimelineScroll = true;
        try
        {
            _timelineScrollbar.SetScrollInfo(maximum, visibleRows, topIndex);
        }
        finally
        {
            _syncingTimelineScroll = false;
        }
    }

    private void TimelineKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A)
        {
            SelectAllVisibleEvents();
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            UndoJournalChange();
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            RedoJournalChange();
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedEvents();
        }
        else
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void SelectAllVisibleEvents()
    {
        if (_saving || _timeline.VirtualListSize == 0)
        {
            return;
        }

        _suppressSelectionEvents = true;
        try
        {
            _timeline.SelectAllItems();
            _selectedIndices = _visibleIndices.ToArray();
        }
        finally
        {
            _suppressSelectionEvents = false;
        }

        SynchronizeEditorsFromSelection();
        UpdateJournalStatus();
    }

    private void RedrawVisibleRows()
    {
        if (_timeline.VirtualListSize == 0)
        {
            return;
        }

        var first = _timeline.TopItem?.Index ?? 0;
        var last = Math.Min(_timeline.VirtualListSize - 1, first + 30);
        _timeline.RedrawItems(first, last, invalidateOnly: true);
    }

    private void FilterChanged(object? sender, EventArgs e)
    {
        if (_changingFilter || _saving)
        {
            return;
        }

        var requestedFilter = (JournalFilter)_filterDropdown.SelectedIndex;
        if (requestedFilter == _filter)
        {
            return;
        }

        if (!ApplyPendingChanges())
        {
            _changingFilter = true;
            try
            {
                _filterDropdown.SelectedIndex = (int)_filter;
            }
            finally
            {
                _changingFilter = false;
            }

            return;
        }

        _filter = requestedFilter;
        ReplaceTimeline(_events, _offsets);
        SelectTimelineIndex(_visibleIndices.Length == 0 ? -1 : _visibleIndices[0]);
        UpdateJournalStatus();
    }

    private void RebuildVisibleIndices()
    {
        if (_filter == JournalFilter.All)
        {
            _visibleIndices = Enumerable.Range(0, _events.Count).ToArray();
            return;
        }

        var indices = new List<int>();
        for (var index = 0; index < _events.Count; index++)
        {
            if (MatchesFilter(_events[index], _filter))
            {
                indices.Add(index);
            }
        }

        _visibleIndices = [.. indices];
    }

    private static bool MatchesFilter(MacroEvent macroEvent, JournalFilter filter)
    {
        return filter switch
        {
            JournalFilter.Keyboard => macroEvent.Type == MacroEventType.Key,
            JournalFilter.MouseButtons => macroEvent.Type == MacroEventType.MouseButton,
            JournalFilter.MouseMovement => macroEvent.Type == MacroEventType.MouseMove,
            JournalFilter.MouseWheel => macroEvent.Type is
                MacroEventType.MouseWheel or MacroEventType.MouseHorizontalWheel,
            _ => true
        };
    }

    private void AddHistory(IJournalHistoryEntry entry)
    {
        if (_historyPosition < _history.Count)
        {
            _history.RemoveRange(_historyPosition, _history.Count - _historyPosition);
        }

        _history.Add(entry);
        _historyPosition = _history.Count;
        if (_history.Count <= MaximumHistoryEntries)
        {
            return;
        }

        var excess = _history.Count - MaximumHistoryEntries;
        _history.RemoveRange(0, excess);
        _historyPosition -= excess;
    }

    private void UndoJournalChange()
    {
        if (_saving)
        {
            return;
        }

        if (_sleepEdited || _xEdited || _yEdited)
        {
            SynchronizeEditorsFromSelection();
            UpdateJournalStatus();
            return;
        }

        if (_historyPosition <= 0)
        {
            return;
        }

        var entry = _history[--_historyPosition];
        ApplyHistoryResult(entry.Undo(_events, _offsets), entry.FocusIndex);
    }

    private void RedoJournalChange()
    {
        if (_saving || _sleepEdited || _xEdited || _yEdited ||
            _historyPosition >= _history.Count)
        {
            return;
        }

        var entry = _history[_historyPosition++];
        ApplyHistoryResult(entry.Redo(_events, _offsets), entry.FocusIndex);
    }

    private void ApplyHistoryResult(MacroTimelineEdit edited, int focusIndex)
    {
        ReplaceTimeline(edited.Events, edited.Offsets);
        RefreshEditedSleepEvents();
        RefreshEditedCoordinateEvents();
        RefreshPairValidation();
        SelectTimelineIndex(Math.Min(focusIndex, _events.Count - 1));
        UpdateJournalStatus();
    }

    private void RefreshEditedSleepEvents()
    {
        _editedSleepEvents.Clear();
        for (var index = 0; index < _events.Count; index++)
        {
            var macroEvent = _events[index];
            if (_originalSleeps.TryGetValue(macroEvent, out var originalSleep) &&
                originalSleep != MacroJournalEditor.GetSleepBefore(_offsets, index))
            {
                _editedSleepEvents.Add(macroEvent);
            }
        }
    }

    private void RefreshEditedCoordinateEvents()
    {
        _editedCoordinateEvents.Clear();
        foreach (var macroEvent in _events)
        {
            if (_originalCoordinates.TryGetValue(macroEvent, out var original) &&
                (macroEvent.X != original.X || macroEvent.Y != original.Y))
            {
                _editedCoordinateEvents.Add(macroEvent);
            }
        }
    }

    private void RefreshPairValidation()
    {
        _pairValidation = MacroJournalEditor.ValidateInputPairs(_events);
    }

    private async Task SaveChangesAsync()
    {
        if (_saving || !ApplyPendingChanges() || !HasAppliedChanges())
        {
            return;
        }

        _saving = true;
        SetEditorEnabled(false);
        _statusLabel.ForeColor = UiTheme.TextSoft;
        _statusLabel.Text = LocalizationService.Get("Macros.Saving");
        try
        {
            var edited = MacroJournalEditor.CreateEditedCopy(_source, _events, _offsets);
            await Task.Run(() => _storage.Save(edited));
            SavedMacro = edited;
            _saving = false;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            InputDiagnostics.Write($"MacroJournalSaveFailed id={_source.Id} error={ex.GetType().Name}");
            ShowError(LocalizationService.Get("Macros.SaveFailed"));
        }
        finally
        {
            _saving = false;
            if (!IsDisposed && !Disposing)
            {
                SetEditorEnabled(true);
                UpdateButtons();
            }
        }
    }

    private bool HasAppliedChanges()
    {
        return _editedSleepEvents.Count > 0 ||
            _editedCoordinateEvents.Count > 0 ||
            _events.Count != _source.Events.Count;
    }

    private bool HasUnsavedChanges()
    {
        return _sleepEdited || _xEdited || _yEdited || HasAppliedChanges();
    }

    private void UpdateJournalStatus()
    {
        var duration = _offsets.Length == 0 ? 0 : _offsets[^1];
        _summaryLabel.Text = LocalizationService.Get(
            "Macros.JournalFilteredSummary",
            _selectedIndices.Length,
            _visibleIndices.Length,
            _events.Count,
            FormatTime(duration));

        var deletedCount = _source.Events.Count - _events.Count;
        var changeText = HasAppliedChanges()
            ? LocalizationService.Get(
                "Macros.JournalDetailedChanges",
                _editedSleepEvents.Count,
                _editedCoordinateEvents.Count,
                deletedCount)
            : LocalizationService.Get("Macros.JournalNoChanges");
        if (_pairValidation.ProblemCount > 0)
        {
            _statusLabel.ForeColor = Color.FromArgb(245, 190, 110);
            _statusLabel.Text = LocalizationService.Get(
                "Macros.JournalPairWarning", changeText, _pairValidation.ProblemCount);
        }
        else
        {
            _statusLabel.ForeColor = UiTheme.TextSoft;
            _statusLabel.Text = changeText;
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var coordinatesPending = _xEdited || _yEdited;
        _applyButton.Enabled = !_saving && _selectedIndices.Length > 0 && _sleepEdited;
        _applyCoordinatesButton.Enabled = !_saving &&
            GetSelectedCoordinateIndices().Length > 0 &&
            coordinatesPending;
        _deleteButton.Enabled = !_saving && _selectedIndices.Length > 0;
        _undoButton.Enabled = !_saving &&
            (_sleepEdited || coordinatesPending || _historyPosition > 0);
        _redoButton.Enabled = !_saving &&
            !_sleepEdited && !coordinatesPending &&
            _historyPosition < _history.Count;
        _resetButton.Enabled = !_saving && HasUnsavedChanges();
        _saveButton.Enabled = !_saving && HasUnsavedChanges();
    }

    private void SetEditorEnabled(bool enabled)
    {
        _timeline.Enabled = enabled;
        _filterDropdown.Enabled = enabled;
        _sleepInput.Enabled = enabled;
        var hasCoordinates = GetSelectedCoordinateIndices().Length > 0;
        _xInput.Enabled = enabled && hasCoordinates;
        _yInput.Enabled = enabled && hasCoordinates;
        if (!enabled)
        {
            _applyButton.Enabled = false;
            _applyCoordinatesButton.Enabled = false;
            _deleteButton.Enabled = false;
            _undoButton.Enabled = false;
            _redoButton.Enabled = false;
            _resetButton.Enabled = false;
            _saveButton.Enabled = false;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.A) && _timeline.ContainsFocus)
        {
            SelectAllVisibleEvents();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Z))
        {
            UndoJournalChange();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Y))
        {
            RedoJournalChange();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowError(string message)
    {
        _statusLabel.ForeColor = Color.FromArgb(245, 150, 153);
        _statusLabel.Text = message;
    }

    private void InitializeOriginalEventState(IReadOnlyList<MacroEvent> events)
    {
        if (events.Count != _originalOffsets.Length)
        {
            throw new InvalidDataException("The event and timing counts do not match.");
        }

        _originalSleeps.Clear();
        _originalCoordinates.Clear();
        for (var index = 0; index < events.Count; index++)
        {
            var macroEvent = events[index];
            _originalSleeps[macroEvent] =
                MacroJournalEditor.GetSleepBefore(_originalOffsets, index);
            _originalCoordinates[macroEvent] = new Point(macroEvent.X, macroEvent.Y);
        }
    }

    private static List<MacroEvent> CloneEvents(IReadOnlyList<MacroEvent> events)
    {
        var clones = new List<MacroEvent>(events.Count);
        foreach (var item in events)
        {
            clones.Add(new MacroEvent
            {
                Type = item.Type,
                OffsetMicroseconds = item.OffsetMicroseconds,
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

        return clones;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_saving)
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    private void ApplyRoundedRegion()
    {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
        {
            return;
        }

        using var path = UiTheme.CreateRoundedRectPath(
            new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
            22f);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hoverTooltips.Dispose();
            _headerFont.Dispose();
            _headerBackgroundBrush.Dispose();
            _rowBackgroundBrush.Dispose();
            _selectedRowBackgroundBrush.Dispose();
            _rowDividerPen.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class MacroJournalListView : ListView
    {
        private const int LvmFirst = 0x1000;
        private const int LvmScroll = LvmFirst + 20;
        private const int LvmSetItemState = LvmFirst + 43;
        private const int LvmGetCountPerPage = LvmFirst + 40;
        private const uint LvisSelected = 0x0002;
        private const int WmVScroll = 0x0115;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ListViewItemState
        {
            public uint Mask;
            public int Item;
            public int SubItem;
            public uint State;
            public uint StateMask;
            public IntPtr Text;
            public int TextLength;
            public int Image;
            public IntPtr Parameter;
            public int Indent;
            public int GroupId;
            public uint ColumnCount;
            public IntPtr Columns;
            public IntPtr ColumnFormats;
            public int Group;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern nint SendMessage(
            IntPtr window,
            int message,
            nint parameter,
            ref ListViewItemState itemState);

        internal event EventHandler? ScrollPositionChanged;

        internal MacroJournalListView()
        {
            DoubleBuffered = true;
        }

        internal int GetVisibleRowCount()
        {
            return IsHandleCreated
                ? Math.Max(0, (int)NativeMethods.SendMessage(Handle, LvmGetCountPerPage, 0, 0))
                : 0;
        }

        internal void SelectAllItems()
        {
            if (!IsHandleCreated || VirtualListSize == 0)
            {
                return;
            }

            var itemState = new ListViewItemState
            {
                State = LvisSelected,
                StateMask = LvisSelected
            };
            _ = SendMessage(Handle, LvmSetItemState, -1, ref itemState);
        }

        internal void ScrollToTopIndex(int index)
        {
            if (!IsHandleCreated || VirtualListSize == 0)
            {
                return;
            }

            var targetIndex = Math.Clamp(index, 0, VirtualListSize - 1);
            var currentIndex = TopItem?.Index ?? 0;
            var rowDelta = targetIndex - currentIndex;
            if (rowDelta == 0)
            {
                return;
            }

            var rowHeight = Font.Height + 5;
            try
            {
                rowHeight = Math.Max(1, GetItemRect(currentIndex).Height);
            }
            catch (ArgumentException)
            {
                // The fallback height is sufficient while the virtual item is being recreated.
            }

            var pixelDelta = Math.Clamp(
                (long)rowDelta * rowHeight,
                int.MinValue,
                int.MaxValue);
            NativeMethods.SendMessage(Handle, LvmScroll, 0, (nint)pixelDelta);
        }

        protected override void WndProc(ref Message message)
        {
            var scrollChanged = message.Msg is
                WmVScroll or
                NativeMethods.WmMouseWheel or
                NativeMethods.WmKeyDown or
                LvmScroll;
            base.WndProc(ref message);

            if (scrollChanged)
            {
                ScrollPositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private sealed class ThemedVerticalScrollBar : Control
    {
        private const int TrackInset = 3;
        private const int MinimumThumbHeight = 32;
        private int _maximum;
        private int _largeChange = 1;
        private int _value;
        private int _dragOffset;
        private bool _dragging;
        private bool _hoveringThumb;

        internal event EventHandler? ValueChanged;

        internal int Value => _value;

        internal ThemedVerticalScrollBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = UiTheme.Surface;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        internal void SetScrollInfo(int maximum, int largeChange, int value)
        {
            var nextMaximum = Math.Max(0, maximum);
            var nextLargeChange = Math.Max(1, largeChange);
            var nextValue = Math.Clamp(value, 0, nextMaximum);
            var nextEnabled = nextMaximum > 0;
            if (_maximum == nextMaximum &&
                _largeChange == nextLargeChange &&
                _value == nextValue &&
                Enabled == nextEnabled)
            {
                return;
            }

            _maximum = nextMaximum;
            _largeChange = nextLargeChange;
            _value = nextValue;
            Enabled = nextEnabled;
            Invalidate();
        }

        internal void ScrollByWheel(int delta)
        {
            if (!Enabled || delta == 0)
            {
                return;
            }

            var notches = Math.Max(1, Math.Abs(delta) / SystemInformation.MouseWheelScrollDelta);
            var configuredLines = SystemInformation.MouseWheelScrollLines;
            var step = configuredLines < 0
                ? _largeChange
                : Math.Max(1, configuredLines) * notches;
            SetValue(_value - (Math.Sign(delta) * step));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UiTheme.ConfigureRoundedControlGraphics(e.Graphics, this);

            var track = GetTrackBounds();
            using (var trackPath = UiTheme.CreateRoundedRectPath(track, track.Width / 2f))
            using (var trackBrush = new SolidBrush(UiTheme.SurfaceAlt))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            var thumb = GetThumbBounds();
            var thumbColor = !Enabled
                ? UiTheme.BorderSoft
                : _dragging || _hoveringThumb
                    ? UiTheme.Accent
                    : UiTheme.AccentSecondary;
            using var thumbPath = UiTheme.CreateRoundedRectPath(thumb, thumb.Width / 2f);
            using var thumbBrush = new SolidBrush(thumbColor);
            e.Graphics.FillPath(thumbBrush, thumbPath);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left)
            {
                return;
            }

            var thumb = GetThumbBounds();
            if (thumb.Contains(e.Location))
            {
                _dragging = true;
                _dragOffset = e.Y - thumb.Top;
                Capture = true;
                Invalidate();
                return;
            }

            SetValue(_value + (e.Y < thumb.Top ? -_largeChange : _largeChange));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                var track = GetTrackBounds();
                var thumb = GetThumbBounds();
                var travel = track.Height - thumb.Height;
                if (travel > 0)
                {
                    var thumbTop = Math.Clamp(e.Y - _dragOffset, track.Top, track.Bottom - thumb.Height);
                    SetValue((int)Math.Round((thumbTop - track.Top) / (double)travel * _maximum));
                }

                return;
            }

            var hovering = GetThumbBounds().Contains(e.Location);
            if (hovering != _hoveringThumb)
            {
                _hoveringThumb = hovering;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _dragging)
            {
                _dragging = false;
                Capture = false;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_dragging && _hoveringThumb)
            {
                _hoveringThumb = false;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollByWheel(e.Delta);
        }

        private Rectangle GetTrackBounds()
        {
            return new Rectangle(
                TrackInset,
                TrackInset,
                Math.Max(1, Width - (TrackInset * 2)),
                Math.Max(1, Height - (TrackInset * 2)));
        }

        private Rectangle GetThumbBounds()
        {
            var track = GetTrackBounds();
            if (_maximum <= 0)
            {
                return track;
            }

            var thumbHeight = Math.Max(
                MinimumThumbHeight,
                (int)Math.Round(track.Height * (_largeChange / (double)(_maximum + _largeChange))));
            thumbHeight = Math.Min(track.Height, thumbHeight);
            var travel = track.Height - thumbHeight;
            var thumbTop = track.Top + (int)Math.Round((_value / (double)_maximum) * travel);
            return new Rectangle(track.Left, thumbTop, track.Width, thumbHeight);
        }

        private void SetValue(int value)
        {
            var clamped = Math.Clamp(value, 0, _maximum);
            if (_value == clamped)
            {
                return;
            }

            _value = clamped;
            Invalidate();
            Update();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
