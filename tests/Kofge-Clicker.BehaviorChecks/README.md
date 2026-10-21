# Behavior Checks

Run on Windows with the .NET 8 SDK:

```powershell
dotnet run --project tests/Kofge-Clicker.BehaviorChecks/Kofge-Clicker.BehaviorChecks.csproj -c Release
```

The checks cover hotkey mapping, background mouse modifiers, minimum press
duration, repeats, cancellation, macro compatibility and storage validation,
journal edits, recording without mouse movement, and INI persistence.
Background-input checks cover target-only keyboard/text delivery, missing/closed/
minimized targets, standard and nested controls, cancellation, and cleanup after
a timed-out mouse press. These test windows do not establish game compatibility.
Audit checks cover extreme coordinates, journal select-all/batch editing/undo/
redo/reset without mutating the source, update versions and unassigned hotkeys.
Button prototype checks cover hover reversal, press/click behavior, a shared
animation clock, idle shutdown, disabling/hiding/disposal, and legacy opt-out.
The reusable animation frame buffer is checked for nested ticks, registration
changes during a tick, and release of control references after rendering.
Checked-state prototype checks cover immediate checkbox/radio events, smooth
reversal, radio-group exclusivity, reentrant state changes, shared scheduling
with buttons, hidden/disabled/disposed controls, handle recreation, diagnostics,
and pixel-identical settled rendering (including sliding-knob legacy controls).
Main-window toggle/radio factories enable the checked-state prototype across
all tabs, as do the tour's option toggles. Standalone controls still allow opt-out.
Checks verify factory defaults, page switching mid-transition, initialization
of hidden controls, and disposal of animated tour controls using in-memory options.
Checked-state timing starts at the first scheduled paint, not at the input event.
Regression checks cover slow synchronous/queued handlers, delayed painting,
reversal before the first frame, and idle shutdown if a clipped control never paints.
The checked-state color curve is sampled at 0/30/75/120/150 ms so that half of
the color change happens at the midpoint, rather than almost all of it at the start.
The fill and border use a soft left-to-right color wave by default.
The Clicker Hold/Toggle pair follows the selected mode's position: selecting
the right mode sweeps right, selecting the left mode sweeps left on both controls.
Pixel checks cover both checkbox/radio waves, uninterrupted rapid reversals,
resizing during the transition, and a soft rather than hard transition edge.
Header buttons opt into the same 150 ms wave, with direction taken from the
previous and next selected tab. Content still switches synchronously.
Header checks exercise the actual selection helper with adjacent/distant tabs,
rapid reversals, repeated selection, immediate clicks/page visibility, first-paint
timing, idle/hide/disable/handle/disposal cleanup, and legacy pixel parity for
hover/press/disabled states with both border styles. Ordinary buttons opt out.
Tooltip checks verify that mouse activation is refused and that repeated
show/fade/hide cycles preserve the owner window and keyboard focus.
Frame diagnostics checks cover completed paints versus timer ticks, shared-frame
deduplication, frame-time math, idle/hidden shutdown, and animation during a macro.
The optional UI performance display defaults off and persists as a global Main
setting, not a per-profile option. Checks cover old INI defaults, round-trips,
hidden refresh timers, localized option/release-note text fit and version parity.
Page-switch checks verify page-content paint events, native visibility, bitmap
rendering, change notifications, hidden-window behavior, and continued animation.
They also reject redraw suppression on the page host while its child windows
change visibility. Bitmap rendering does not validate desktop compositor output.
Background-cache checks compare cached and direct pixels (including clipped,
translated paints), surface reuse, size/color/radius/shadow/DPI changes, resource
disposal, and live child controls. A warmed A/B tab-switch comparison reports
timing without asserting hardware-dependent performance or monitor FPS.
Only main-window rounded panels opt into this cache; dialogs remain unchanged.
Surface-label checks compare the fast path with normal transparent-label painting,
including corners, borders, shadows, disabled/RTL/wrapped text, changed colors,
translucency and background images. Labels outside a flat opaque panel fall back
to normal painting. The tab-switch comparison also measures this fast path.

Background playback uses a temporary test window without moving the physical
cursor. Storage checks use temporary directories and preserve application data.
Keep the mouse still while the checks run: the cursor-position assertion also
detects physical mouse movement and cannot distinguish it from application input.
Do not switch applications during the tooltip focus check either.
Individual check names may be supplied after `--` to run a subset; unknown names
are rejected rather than reporting success without running the requested check.
The runner returns a nonzero exit code if a check fails.
