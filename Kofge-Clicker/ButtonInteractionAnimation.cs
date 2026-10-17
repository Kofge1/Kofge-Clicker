using System.Diagnostics;

namespace KofgeClicker;

internal sealed class ButtonInteractionAnimation : IDisposable, IUiAnimationFrame
{
    private readonly AccentButton _owner;
    private Transition _hover;
    private Transition _press;
    private Transition _primary;
    private bool _disposed;
    private long _pendingFrameId;

    internal float HoverAmount => _hover.Value;
    internal float PressAmount => _press.Value;
    internal float PrimaryAmount => _primary.Value;
    internal bool IsAnimating => _hover.IsActive || _press.IsActive || _primary.IsActive;

    internal ButtonInteractionAnimation(AccentButton owner, bool primary)
    {
        _owner = owner;
        _primary.Snap(primary ? 1f : 0f);
    }

    internal void SetHovered(bool hovered)
    {
        _hover.MoveTo(hovered ? 1f : 0f, hovered ? 130 : 170);
        RefreshRegistration();
    }

    internal void SetPressed(bool pressed)
    {
        if (pressed)
            _press.Snap(1f);
        else
            _press.MoveTo(0f, 120);
        RefreshRegistration();
    }

    internal void SetPrimary(bool primary, bool animate)
    {
        if (animate)
            _primary.MoveTo(primary ? 1f : 0f, 150);
        else
            _primary.Snap(primary ? 1f : 0f);
        RefreshRegistration();
    }

    internal void Reset(bool primary)
    {
        _pendingFrameId = 0;
        _hover.Snap(0f);
        _press.Snap(0f);
        _primary.Snap(primary ? 1f : 0f);
        ButtonAnimationClock.Remove(this);
    }

    internal bool AdvanceFrame(long timestamp, long frameId)
    {
        if (!CanAnimate())
        {
            Reset(_owner.Primary);
            return false;
        }

        _hover.Advance(timestamp);
        _press.Advance(timestamp);
        _primary.Advance(timestamp);
        _pendingFrameId = frameId;
        _owner.Invalidate();
        return IsAnimating;
    }

    bool IUiAnimationFrame.AdvanceFrame(long timestamp, long frameId) => AdvanceFrame(timestamp, frameId);

    internal void RecordPaint()
    {
        if (_disposed || _pendingFrameId == 0)
            return;
        ButtonAnimationClock.Metrics.RecordPaint(_pendingFrameId, Stopwatch.GetTimestamp());
        _pendingFrameId = 0;
    }

    private bool CanAnimate() => !_disposed && !_owner.IsDisposed && !_owner.Disposing
        && _owner.IsHandleCreated && _owner.Visible && _owner.Enabled
        && _owner.FindForm() is not { WindowState: FormWindowState.Minimized };

    private void RefreshRegistration()
    {
        if (_disposed)
            return;

        if (!CanAnimate())
        {
            _hover.Snap(_hover.Target);
            _press.Snap(_press.Target);
            _primary.Snap(_primary.Target);
            ButtonAnimationClock.Remove(this);
        }
        else if (IsAnimating)
        {
            ButtonAnimationClock.Add(this);
        }
        else
        {
            ButtonAnimationClock.Remove(this);
        }

        if (!_owner.IsDisposed && !_owner.Disposing)
            _owner.Invalidate();
    }

    public void Dispose()
    {
        _disposed = true;
        _pendingFrameId = 0;
        ButtonAnimationClock.Remove(this);
    }

    private struct Transition
    {
        private float _startValue;
        private long _startedAt;
        private int _durationMs;
        internal float Value { get; private set; }
        internal float Target { get; private set; }
        internal bool IsActive { get; private set; }

        internal void Snap(float value)
        {
            Value = Target = value;
            IsActive = false;
        }

        internal void MoveTo(float target, int durationMs)
        {
            if (Target == target)
                return;

            var now = Stopwatch.GetTimestamp();
            Advance(now);
            _startValue = Value;
            Target = target;
            _startedAt = now;
            _durationMs = durationMs;
            IsActive = Math.Abs(Target - Value) > 0.0001f;
            if (!IsActive)
                Snap(target);
        }

        internal void Advance(long timestamp)
        {
            if (!IsActive)
                return;

            var progress = (float)Math.Clamp(
                Stopwatch.GetElapsedTime(_startedAt, timestamp).TotalMilliseconds / _durationMs, 0, 1);
            var eased = 1f - MathF.Pow(1f - progress, 3);
            Value = _startValue + ((Target - _startValue) * eased);
            if (progress >= 1f)
                Snap(Target);
        }
    }
}

internal sealed class ButtonAnimationClock
{
    [ThreadStatic]
    private static ButtonAnimationClock? _current;
    private readonly HashSet<IUiAnimationFrame> _active = new();
    private readonly List<IUiAnimationFrame> _frame = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly UiFrameMetrics _metrics = new();
    private long _frameId;
    private bool _advancingFrame;

    internal static UiFrameMetrics Metrics => (_current ??= new ButtonAnimationClock())._metrics;

    private ButtonAnimationClock()
    {
        _timer.Tick += OnTick;
        Application.ThreadExit += OnThreadExit;
    }

    internal static void Add(IUiAnimationFrame animation)
    {
        var clock = _current ??= new ButtonAnimationClock();
        if (clock._active.Add(animation) && clock._active.Count == 1)
            clock._metrics.BeginSession(clock._frameId + 1);
        clock._timer.Start();
    }

    internal static void Remove(IUiAnimationFrame animation)
    {
        if (_current is not { } clock)
            return;
        clock._active.Remove(animation);
        if (clock._active.Count == 0)
        {
            clock._timer.Stop();
            clock._metrics.EndSession();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_advancingFrame)
            return;
        _advancingFrame = true;
        try
        {
            var timestamp = Stopwatch.GetTimestamp();
            var frameId = ++_frameId;
            // Reuse a snapshot; hiding or disposing a button can change the active set.
            _frame.AddRange(_active);
            foreach (var animation in _frame)
            {
                if (!animation.AdvanceFrame(timestamp, frameId))
                    _active.Remove(animation);
            }
            if (_active.Count == 0)
            {
                _timer.Stop();
                _metrics.EndSession();
            }
        }
        finally
        {
            _frame.Clear();
            _advancingFrame = false;
        }
    }

    private void OnThreadExit(object? sender, EventArgs e)
    {
        if (Environment.CurrentManagedThreadId != _threadId)
            return;
        _timer.Stop();
        _timer.Dispose();
        _active.Clear();
        Application.ThreadExit -= OnThreadExit;
        _current = null;
    }
}
