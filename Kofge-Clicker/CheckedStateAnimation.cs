using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KofgeClicker;

internal interface IUiAnimationFrame
{
    bool AdvanceFrame(long timestamp, long frameId);
}

internal sealed class CheckedStateAnimation : IUiAnimationFrame, IDisposable
{
    private const int DurationMilliseconds = 150;
    private const int UnpaintedTimeoutMilliseconds = 1000;
    private const int GradientStops = 17;
    private readonly ButtonBase _owner;
    private readonly float[] _startProfile = new float[GradientStops];
    private readonly Color[] _gradientColors = new Color[GradientStops];
    private readonly ColorBlend _gradientBlend = new(GradientStops)
    {
        Positions = Enumerable.Range(0, GradientStops).Select(i => i / (float)(GradientStops - 1)).ToArray()
    };
    private float _start;
    private float _target;
    private float _progress;
    private long _startedAt;
    private long _pendingFrameId;
    private long _firstFrameRequestedAt;
    private bool _active;
    private bool _waitingForFirstPaint;
    private bool _sweepFromRight;
    private bool _nextSweepFromRight;
    private bool _disposed;

    internal float Amount { get; private set; }

    internal CheckedStateAnimation(ButtonBase owner)
    {
        if (owner is not CheckBox && owner is not RadioButton && owner is not AccentButton)
            throw new ArgumentException("A checkbox, radio button, or accent button is required.", nameof(owner));
        _owner = owner;
        Amount = _target = ReadChecked() ? 1f : 0f;
        if (_owner is CheckBox checkBox)
            checkBox.CheckedChanged += OnCheckedChanged;
        else if (_owner is RadioButton radioButton)
            radioButton.CheckedChanged += OnCheckedChanged;
        _owner.EnabledChanged += OnAvailabilityChanged;
        _owner.VisibleChanged += OnAvailabilityChanged;
        _owner.HandleDestroyed += OnHandleDestroyed;
        _owner.Disposed += OnDisposed;
    }

    private bool ReadChecked() => _owner switch
    {
        CheckBox checkBox => checkBox.Checked,
        RadioButton radioButton => radioButton.Checked,
        AccentButton button => button.Primary,
        _ => throw new InvalidOperationException("Unsupported state-animation owner.")
    };

    internal void RetargetPrimary(bool fromRight, bool animate)
    {
        PrepareDirection(fromRight);
        if (animate)
            OnCheckedChanged(_owner, EventArgs.Empty);
        else
        {
            SnapToChecked();
            _owner.Invalidate();
        }
    }

    private bool CanAnimate => !_disposed && !_owner.IsDisposed && !_owner.Disposing
        && _owner.IsHandleCreated && _owner.Visible && _owner.Enabled
        && _owner.FindForm() is not { WindowState: FormWindowState.Minimized };

    private void OnCheckedChanged(object? sender, EventArgs e)
    {
        var target = ReadChecked() ? 1f : 0f;
        PrepareGroupDirection(target);
        if (!CanAnimate)
        {
            SnapToChecked();
            return;
        }
        if (_target == target)
            return;

        // Preserve the displayed wave when a click reverses an unfinished transition.
        for (var i = 0; i < GradientStops; i++)
            _startProfile[i] = CoverageAtStop(i);
        _sweepFromRight = _nextSweepFromRight;
        _progress = 0f;
        _start = Amount;
        _target = target;
        _waitingForFirstPaint = true;
        _pendingFrameId = 0;
        _firstFrameRequestedAt = 0;
        _active = Math.Abs(_target - Amount) > 0.0001f;
        if (_active)
            ButtonAnimationClock.Add(this);
        else
            SnapToChecked();
        _owner.Invalidate();
    }

    internal void PrepareDirection(bool fromRight) => _nextSweepFromRight = fromRight;

    private void PrepareGroupDirection(float target)
    {
        if (target != 0f || _owner is not SegmentRadioButton { FollowGroupSelectionDirection: true } source
            || source.Parent is null)
            return;

        // WinForms checks the destination before unchecking the previous group member.
        foreach (Control sibling in source.Parent.Controls)
        {
            if (sibling is not SegmentRadioButton { Checked: true, FollowGroupSelectionDirection: true } selected
                || ReferenceEquals(source, selected))
                continue;
            _nextSweepFromRight = selected.Left < source.Left;
            selected.PrepareCheckedTransitionDirection(_nextSweepFromRight);
            break;
        }
    }

    private void Advance(long timestamp)
    {
        if (!_active || _waitingForFirstPaint)
            return;
        var progress = (float)Math.Clamp(
            Stopwatch.GetElapsedTime(_startedAt, timestamp).TotalMilliseconds / DurationMilliseconds, 0, 1);
        var eased = progress * progress * (3f - 2f * progress);
        _progress = eased;
        Amount = _start + (_target - _start) * eased;
        if (progress >= 1f)
        {
            Amount = _target;
            _active = false;
        }
    }

    public bool AdvanceFrame(long timestamp, long frameId)
    {
        if (!CanAnimate)
        {
            SnapToChecked();
            return false;
        }
        if (_waitingForFirstPaint)
        {
            if (_firstFrameRequestedAt == 0)
                _firstFrameRequestedAt = timestamp;
            else if (Stopwatch.GetElapsedTime(_firstFrameRequestedAt, timestamp).TotalMilliseconds >= UnpaintedTimeoutMilliseconds)
            {
                // A fully clipped control may never receive a paint despite being Visible.
                SnapToChecked();
                return false;
            }
        }
        Advance(timestamp);
        _pendingFrameId = frameId;
        _owner.Invalidate();
        return _active;
    }

    internal void RecordPaint()
    {
        if (_disposed || _pendingFrameId == 0)
            return;
        var now = Stopwatch.GetTimestamp();
        if (_active && _waitingForFirstPaint)
        {
            // Input handlers and queued saves must not consume the visible transition.
            _startedAt = now;
            _waitingForFirstPaint = false;
            _firstFrameRequestedAt = 0;
        }
        ButtonAnimationClock.Metrics.RecordPaint(_pendingFrameId, now);
        _pendingFrameId = 0;
    }

    private float CoverageAtStop(int index)
    {
        if (!_active)
            return Amount;
        const float feather = 0.4f;
        var x = index / (float)(GradientStops - 1);
        if (_sweepFromRight)
            x = 1f - x;
        var coverage = Math.Clamp((_progress * (1f + feather) - x) / feather, 0f, 1f);
        coverage = coverage * coverage * (3f - 2f * coverage);
        return _startProfile[index] + (_target - _startProfile[index]) * coverage;
    }

    internal Brush CreateBrush(RectangleF bounds, Color from, Color to)
    {
        if (!_active)
            return new SolidBrush(Blend(from, to, Amount));

        var uniform = true;
        for (var i = 0; i < GradientStops; i++)
        {
            _gradientColors[i] = Blend(from, to, CoverageAtStop(i));
            uniform &= _gradientColors[i] == _gradientColors[0];
        }
        if (uniform)
            return new SolidBrush(_gradientColors[0]);

        _gradientBlend.Colors = _gradientColors;
        return new LinearGradientBrush(bounds, from, to, LinearGradientMode.Horizontal)
        {
            InterpolationColors = _gradientBlend,
            WrapMode = WrapMode.TileFlipX
        };
    }

    private void SnapToChecked()
    {
        Amount = _target = ReadChecked() ? 1f : 0f;
        _active = false;
        _waitingForFirstPaint = false;
        _pendingFrameId = 0;
        _firstFrameRequestedAt = 0;
        ButtonAnimationClock.Remove(this);
    }

    private void OnAvailabilityChanged(object? sender, EventArgs e)
    {
        if (!CanAnimate)
            SnapToChecked();
    }

    private void OnHandleDestroyed(object? sender, EventArgs e) => SnapToChecked();
    private void OnDisposed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _active = false;
        _waitingForFirstPaint = false;
        _pendingFrameId = 0;
        _firstFrameRequestedAt = 0;
        ButtonAnimationClock.Remove(this);
        if (_owner is CheckBox checkBox)
            checkBox.CheckedChanged -= OnCheckedChanged;
        else if (_owner is RadioButton radioButton)
            radioButton.CheckedChanged -= OnCheckedChanged;
        _owner.EnabledChanged -= OnAvailabilityChanged;
        _owner.VisibleChanged -= OnAvailabilityChanged;
        _owner.HandleDestroyed -= OnHandleDestroyed;
        _owner.Disposed -= OnDisposed;
    }

    internal static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)Math.Round(from.A + (to.A - from.A) * amount),
        (int)Math.Round(from.R + (to.R - from.R) * amount),
        (int)Math.Round(from.G + (to.G - from.G) * amount),
        (int)Math.Round(from.B + (to.B - from.B) * amount));
}
