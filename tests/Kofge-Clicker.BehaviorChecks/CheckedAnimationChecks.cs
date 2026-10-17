using System.Collections;
using System.Diagnostics;
using System.Reflection;
using KofgeClicker;

internal static class CheckedAnimationChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type ClockType = typeof(AccentButton).Assembly.GetType("KofgeClicker.ButtonAnimationClock")!;

    internal static void ImmediateStateAndRetargeting()
    {
        using var host = new TestWindow();
        using var toggle = Toggle(true);
        host.Controls.Add(toggle);
        host.Show();
        Application.DoEvents();
        var changes = 0;
        toggle.CheckedChanged += (_, _) => changes++;
        var off = Fill(toggle);
        toggle.Checked = true;
        Assert(toggle.Checked && changes == 1, "Checkbox state or CheckedChanged was delayed.");
        Assert(Amount(toggle) == 0f, "Checkbox visuals changed instantly.");
        SampleAt([toggle], 75);
        var midway = Amount(toggle);
        Assert(midway > 0f && midway < 1f, "Checkbox transition did not advance.");
        toggle.Checked = false;
        Assert(!toggle.Checked && changes == 2, "Rapid reversal delayed the actual state.");
        Assert(Amount(toggle) >= midway - 0.001f, "Rapid reversal reset the visual progress.");
        Pump(360);
        Assert(Amount(toggle) == 0f && Fill(toggle) == off, "Checkbox did not return to its original fill.");
        var clicks = 0;
        toggle.Click += (_, _) => clicks++;
        Invoke(toggle, "OnClick", EventArgs.Empty);
        Assert(toggle.Checked && clicks == 1 && changes == 3, "Mouse/keyboard Click semantics changed.");
        Pump(360);
        Assert(Amount(toggle) == 1f, "Checkbox did not finish at its checked color.");
        AssertIdle();

        // An existing handler can synchronously reject the new state.
        toggle.AnimateCheckedChanges = false;
        toggle.Checked = false;
        EventHandler reject = (_, _) => { if (toggle.Checked) toggle.Checked = false; };
        toggle.CheckedChanged += reject;
        toggle.AnimateCheckedChanges = true;
        toggle.Checked = true;
        Pump(360);
        Assert(!toggle.Checked && Amount(toggle) == 0f, "Reentrant state changes left stale visuals.");
        toggle.CheckedChanged -= reject;
        AssertIdle();
    }

    internal static void SlowHandlerDoesNotConsumeVisibleTransition()
    {
        foreach (var deferred in new[] { false, true })
        {
            using var host = new TestWindow();
            using var toggle = Toggle(true);
            host.Controls.Add(toggle);
            host.Show();
            Application.DoEvents();
            // Model synchronous handlers and the app's BeginInvoke-based save queue.
            EventHandler slow = (_, _) =>
            {
                if (deferred) host.BeginInvoke(new Action(() => Thread.Sleep(380)));
                else Thread.Sleep(380);
            };
            toggle.CheckedChanged += slow;
            toggle.Checked = true;
            toggle.CheckedChanged -= slow;
            Assert(toggle.Checked, "Actual state change was deferred.");
            if (deferred) Application.DoEvents();
            Pump(70);
            Assert(Amount(toggle) > 0f && Amount(toggle) < 1f,
                "A slow state handler consumed the entire animation before it was visible.");
            Pump(310);
            Assert(Amount(toggle) == 1f, "The visible transition did not finish.");
            AssertIdle();
        }
    }

    internal static void DelayedPaintAndUnpaintedReversal()
    {
        using var host = new TestWindow();
        using var toggle = Toggle(true);
        host.Controls.Add(toggle);
        host.Show();
        Application.DoEvents();
        toggle.Checked = true;
        var clock = Clock()!;
        var tick = clock.GetType().GetMethod("OnTick", Members)!;
        tick.Invoke(clock, [null, EventArgs.Empty]);
        Thread.Sleep(350);
        tick.Invoke(clock, [null, EventArgs.Empty]);
        Assert(Amount(toggle) == 0f, "Unpainted frames advanced the transition.");
        using (Capture(toggle)) { }
        Pump(70);
        Assert(Amount(toggle) > 0f && Amount(toggle) < 1f, "The first real paint did not start the transition.");
        Pump(310);
        Assert(Amount(toggle) == 1f, "The delayed-paint transition did not finish.");
        toggle.Checked = false;
        Thread.Sleep(350);
        toggle.Checked = true;
        Assert(Amount(toggle) == 1f, "An unpainted reversal flashed an unseen intermediate state.");
        AssertIdle();
    }

    internal static void MissingPaintDoesNotKeepClockAlive()
    {
        using var host = new TestWindow();
        using var toggle = Toggle(true);
        host.Controls.Add(toggle);
        host.Show();
        Application.DoEvents();
        toggle.Checked = true;
        var animation = toggle.GetType().GetField("_checkedAnimation", Members)!.GetValue(toggle)!;
        var frame = animation.GetType().GetMethod("AdvanceFrame", Members)!;
        var timestamp = Stopwatch.GetTimestamp();
        Assert((bool)frame.Invoke(animation, [timestamp, 100L])!, "The initial animation frame was not scheduled.");
        var active = (bool)frame.Invoke(animation, [timestamp + Stopwatch.Frequency * 2, 101L])!;
        Assert(!active && Amount(toggle) == 1f, "A control without paints stayed active indefinitely.");
        AssertIdle();
    }

    internal static void SlowRadioHandlerKeepsBothTransitionsVisible()
    {
        using var host = new TestWindow();
        using var hold = new SegmentRadioButton
        {
            Text = "Hold", Bounds = new Rectangle(10, 70, 145, 40), Checked = true,
            AnimateCheckedChanges = true
        };
        using var alternate = new SegmentRadioButton
        {
            Text = "Toggle", Bounds = new Rectangle(165, 70, 145, 40), AnimateCheckedChanges = true
        };
        host.Controls.Add(hold);
        host.Controls.Add(alternate);
        host.Show();
        Application.DoEvents();
        EventHandler slow = (_, _) => Thread.Sleep(380);
        alternate.CheckedChanged += slow;
        alternate.Checked = true;
        alternate.CheckedChanged -= slow;
        Assert(alternate.Checked && !hold.Checked, "Radio-group state was delayed.");
        Pump(70);
        Assert(Amount(hold) > 0f && Amount(hold) < 1f && Amount(alternate) > 0f && Amount(alternate) < 1f,
            "The slow radio handler hid one of the two color transitions.");
        Pump(310);
        Assert(Amount(hold) == 0f && Amount(alternate) == 1f, "Radio transitions did not settle.");
        AssertIdle();
    }

    internal static void VisibleDurationAndIntermediateFrames()
    {
        using var host = new TestWindow { BackColor = UiTheme.CardInner };
        using var toggle = Toggle(true);
        host.Controls.Add(toggle);
        host.Show();
        Application.DoEvents();
        toggle.Checked = true;
        var clock = Clock()!;
        clock.GetType().GetMethod("OnTick", Members)!.Invoke(clock, [null, EventArgs.Empty]);
        using (Capture(toggle)) { }
        var animation = toggle.GetType().GetField("_checkedAnimation", Members)!.GetValue(toggle)!;
        var startedAt = (long)animation.GetType().GetField("_startedAt", Members)!.GetValue(animation)!;
        var advance = animation.GetType().GetMethod("Advance", Members)!;
        using var strip = new Bitmap((toggle.Width + 8) * 5, toggle.Height + 26);
        using var canvas = Graphics.FromImage(strip);
        canvas.Clear(UiTheme.CardInner);
        var samples = new[] { (0, 0f), (30, 0.104f), (75, 0.5f), (120, 0.896f), (150, 1f) };
        for (var index = 0; index < samples.Length; index++)
        {
            var (milliseconds, expected) = samples[index];
            advance.Invoke(animation, [startedAt + Stopwatch.Frequency * milliseconds / 1000]);
            Assert(Math.Abs(Amount(toggle) - expected) < 0.001f, "The transition did not span the full visible duration.");
            using var frame = Capture(toggle);
            var x = index * (toggle.Width + 8);
            canvas.DrawImageUnscaled(frame, x, 24);
            TextRenderer.DrawText(canvas, $"{milliseconds} ms", SystemFonts.MessageBoxFont,
                new Rectangle(x, 0, toggle.Width, 22), UiTheme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        strip.Save(Path.Combine(Path.GetTempPath(), "Kofge-Checked-Animation-frames.png"));
        toggle.Enabled = false;
        AssertIdle();
    }

    internal static void RadioGroupAndPaintMetrics()
    {
        using var host = new TestWindow();
        using var hold = new SegmentRadioButton
        {
            Text = "Hold", Bounds = new Rectangle(10, 70, 145, 40), Checked = true,
            PrimarySegment = true, AnimateCheckedChanges = true
        };
        using var alternate = new SegmentRadioButton
        {
            Text = "Toggle", Bounds = new Rectangle(165, 70, 145, 40), AnimateCheckedChanges = true
        };
        host.Controls.Add(hold);
        host.Controls.Add(alternate);
        host.Show();
        Application.DoEvents();
        alternate.Checked = true;
        Assert(alternate.Checked && !hold.Checked, "Radio-group selection was delayed or broken.");
        Assert(Amount(hold) == 1f && Amount(alternate) == 0f, "Radio visuals did not start from their previous states.");
        SampleAt([hold, alternate], 75);
        Assert(Amount(hold) < 1f && Amount(alternate) > 0f, "Radio colors did not interpolate together.");
        hold.Checked = true;
        Assert(hold.Checked && !alternate.Checked, "Radio-group reversal was delayed.");
        Pump(360);
        Assert(Amount(hold) == 1f && Amount(alternate) == 0f, "Radio animation ended at the wrong state.");
        var metrics = ClockType.GetProperty("Metrics", Members)!.GetValue(null)!;
        var snapshot = metrics.GetType().GetMethod("GetSnapshot", Members)!.Invoke(metrics, null)!;
        Assert((int)snapshot.GetType().GetProperty("PaintCount")!.GetValue(snapshot)! > 0,
            "Checkbox/radio paints were not included in diagnostics.");
        AssertIdle();
    }

    internal static void MovingGradientAndReversal()
    {
        using var host = new TestWindow();
        using var toggle = Toggle(true);
        using var radio = new SegmentRadioButton
        {
            Text = "Mode", Bounds = new Rectangle(10, 70, 145, 40), AnimateCheckedChanges = true
        };
        host.Controls.Add(toggle);
        host.Controls.Add(radio);
        host.Show();
        Application.DoEvents();
        foreach (var control in new Control[] { toggle, radio })
        {
            void SetChecked(bool value)
            {
                if (control is CheckBox checkBox) checkBox.Checked = value;
                else ((RadioButton)control).Checked = value;
            }
            object StartFrame()
            {
                var clock = Clock()!;
                clock.GetType().GetMethod("OnTick", Members)!.Invoke(clock, [null, EventArgs.Empty]);
                using (Capture(control)) { }
                return control.GetType().GetField("_checkedAnimation", Members)!.GetValue(control)!;
            }
            void AdvanceTo(object animation, int milliseconds)
            {
                var startedAt = (long)animation.GetType().GetField("_startedAt", Members)!.GetValue(animation)!;
                animation.GetType().GetMethod("Advance", Members)!.Invoke(animation,
                    [startedAt + Stopwatch.Frequency * milliseconds / 1000]);
            }
            void EqualSurface(Bitmap before, Bitmap after)
            {
                for (var x = 0; x < control.Width; x++)
                    Assert(before.GetPixel(x, 5) == after.GetPixel(x, 5), "Retargeting flashed the gradient surface.");
            }

            using var off = Capture(control);
            SetChecked(true);
            var animation = StartFrame();
            AdvanceTo(animation, 150);
            using var on = Capture(control);
            SetChecked(false);
            animation = StartFrame();
            AdvanceTo(animation, 75);
            using var halfwayOff = Capture(control);
            var left = control.Width / 4;
            var right = control.Width * 3 / 4;
            Assert(halfwayOff.GetPixel(left, 5) == off.GetPixel(left, 5)
                && halfwayOff.GetPixel(right, 5) == on.GetPixel(right, 5), "Off wave did not travel left to right.");
            SetChecked(true);
            using (var reversed = Capture(control)) EqualSurface(halfwayOff, reversed);
            SetChecked(false);
            using (var reversedAgain = Capture(control)) EqualSurface(halfwayOff, reversedAgain);
            animation = StartFrame();
            AdvanceTo(animation, 150);
            SetChecked(true);
            animation = StartFrame();
            AdvanceTo(animation, 75);
            using var halfwayOn = Capture(control);
            Assert(halfwayOn.GetPixel(left, 5) == on.GetPixel(left, 5)
                && halfwayOn.GetPixel(right, 5) == off.GetPixel(right, 5), "On wave did not travel left to right.");
            var center = halfwayOn.GetPixel(control.Width / 2, 5);
            Assert(center != on.GetPixel(control.Width / 2, 5) && center != off.GetPixel(control.Width / 2, 5),
                "The moving wave has a hard edge instead of a soft gradient.");
            control.Width += 40;
            using var resized = Capture(control);
            Assert(resized.GetPixel(control.Width / 4, 5) == on.GetPixel(left, 5)
                && resized.GetPixel(control.Width * 3 / 4, 5) == off.GetPixel(right, 5),
                "Resizing changed the gradient direction or coverage.");
            control.Enabled = false;
            Assert(Amount(control) == 1f, "Disabling did not settle the gradient.");
            AssertIdle();
        }
    }

    internal static void GroupGradientDirection()
    {
        using var window = new TestWindow();
        using var hold = new SegmentRadioButton
        {
            Text = "Hold", Bounds = new Rectangle(10, 10, 146, 40), PrimarySegment = true,
            AnimateCheckedChanges = true, FollowGroupSelectionDirection = true
        };
        using var alternate = new SegmentRadioButton
        {
            Text = "Toggle", Bounds = new Rectangle(161, 10, 146, 40),
            AnimateCheckedChanges = true, FollowGroupSelectionDirection = true
        };
        window.Controls.Add(hold);
        window.Controls.Add(alternate);
        using var holdOff = Capture(hold);
        using var alternateOff = Capture(alternate);
        hold.Checked = true;
        using var holdOn = Capture(hold);
        alternate.Checked = true;
        using var alternateOn = Capture(alternate);
        hold.Checked = true;
        window.Show();
        Application.DoEvents();
        var controls = new[] { hold, alternate };
        var off = new[] { holdOff, alternateOff };
        var on = new[] { holdOn, alternateOn };

        void StartAndAdvance(int milliseconds)
        {
            var clock = Clock()!;
            clock.GetType().GetMethod("OnTick", Members)!.Invoke(clock, [null, EventArgs.Empty]);
            foreach (var control in controls)
            {
                using (Capture(control)) { }
                var animation = control.GetType().GetField("_checkedAnimation", Members)!.GetValue(control)!;
                var startedAt = (long)animation.GetType().GetField("_startedAt", Members)!.GetValue(animation)!;
                animation.GetType().GetMethod("Advance", Members)!.Invoke(animation,
                    [startedAt + Stopwatch.Frequency * milliseconds / 1000]);
            }
        }
        void AssertDirection(bool selectRight)
        {
            for (var i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                using var actual = Capture(control);
                var changedX = control.Width * (selectRight ? 1 : 3) / 4;
                var unchangedX = control.Width * (selectRight ? 3 : 1) / 4;
                var target = control.Checked ? on[i] : off[i];
                var previous = control.Checked ? off[i] : on[i];
                Assert(actual.GetPixel(changedX, 5) == target.GetPixel(changedX, 5)
                    && actual.GetPixel(unchangedX, 5) == previous.GetPixel(unchangedX, 5),
                    $"Both mode controls must sweep {(selectRight ? "right" : "left")} together.");
            }
        }
        foreach (var selectRight in new[] { true, false })
        {
            (selectRight ? alternate : hold).Checked = true;
            Assert(hold.Checked != alternate.Checked && alternate.Checked == selectRight,
                "Directional animation changed radio selection semantics.");
            StartAndAdvance(75);
            AssertDirection(selectRight);
            StartAndAdvance(150);
        }

        // Reverse a visible, half-finished wave and preserve each spatial color sample.
        alternate.Checked = true;
        StartAndAdvance(75);
        using var beforeHold = Capture(hold);
        using var beforeAlternate = Capture(alternate);
        hold.Checked = true;
        using var afterHold = Capture(hold);
        using var afterAlternate = Capture(alternate);
        for (var x = 0; x < hold.Width; x++)
        {
            Assert(beforeHold.GetPixel(x, 5) == afterHold.GetPixel(x, 5)
                && beforeAlternate.GetPixel(x, 5) == afterAlternate.GetPixel(x, 5),
                "Changing wave direction flashed a mode's existing gradient.");
        }
        StartAndAdvance(150);
        alternate.Checked = true;
        StartAndAdvance(150);
        alternate.Enabled = false;
        hold.Checked = true;
        StartAndAdvance(75);
        using var fromDisabled = Capture(hold);
        Assert(fromDisabled.GetPixel(hold.Width * 3 / 4, 5) == holdOn.GetPixel(hold.Width * 3 / 4, 5),
            "Selecting left from a disabled right mode lost the transition direction.");
        window.Hide();
        Pump(30);
        AssertIdle();
    }

    internal static void SharedClockAndLifecycle()
    {
        using var host = new TestWindow();
        using var toggle = Toggle(true);
        using var button = new AccentButton
        {
            Bounds = new Rectangle(150, 10, 140, 40), Text = "Button", AnimateInteractions = true
        };
        host.Controls.Add(toggle);
        host.Controls.Add(button);
        host.Show();
        Application.DoEvents();
        toggle.Checked = true;
        var clock = Clock();
        Invoke(button, "OnMouseEnter", EventArgs.Empty);
        Assert(ReferenceEquals(clock, Clock()) && ActiveCount() == 2, "State animation created a separate clock.");
        toggle.Enabled = false;
        Assert(Amount(toggle) == 1f && ActiveCount() == 1, "Disabling a toggle did not snap and unregister it.");
        toggle.Enabled = true;
        toggle.Checked = false;
        toggle.Hide();
        Assert(Amount(toggle) == 0f && ActiveCount() == 1, "Hiding a toggle left an animation running.");
        host.Hide();
        Pump(60);
        AssertIdle();
        host.Show();
        toggle.Show();
        toggle.Checked = true;
        typeof(Control).GetMethod("RecreateHandle", Members)!.Invoke(toggle, null);
        Assert(Amount(toggle) == 1f, "Handle recreation left stale state feedback.");
        AssertIdle();
        toggle.Checked = false;
        toggle.AnimateCheckedChanges = false;
        AssertIdle();
        toggle.Checked = true;
        AssertIdle();
        toggle.AnimateCheckedChanges = true;
        toggle.Checked = false;
        toggle.Dispose();
        AssertIdle();
        Invoke(button, "OnMouseLeave", EventArgs.Empty);
        Pump(210);
        AssertIdle();
    }

    internal static void RenderingAndOptOut()
    {
        using var host = new TestWindow();
        using var card = new RoundedPanel
        {
            Dock = DockStyle.Fill, FillColor = UiTheme.CardInner, UseAntialiasedEdges = true
        };
        host.Controls.Add(card);
        using var legacy = Toggle(false);
        using var animated = Toggle(true);
        animated.Left = 160;
        card.Controls.Add(legacy);
        card.Controls.Add(animated);
        Assert(!legacy.AnimateCheckedChanges, "State animation was enabled globally.");
        foreach (var sliding in new[] { false, true })
        {
            legacy.UseSlidingKnob = animated.UseSlidingKnob = sliding;
            foreach (var state in new[] { false, true })
            {
                legacy.Checked = animated.Checked = state;
                using var expected = Capture(legacy);
                using var actual = Capture(animated);
                EqualPixels(expected, actual);
            }
        }
        using var radio = new SegmentRadioButton { Text = "Mode", Bounds = new Rectangle(10, 70, 140, 40) };
        card.Controls.Add(radio);
        Assert(!radio.AnimateCheckedChanges, "Radio animation was enabled globally.");
        foreach (var primary in new[] { false, true })
        {
            radio.PrimarySegment = primary;
            foreach (var state in new[] { false, true })
            {
                radio.AnimateCheckedChanges = false;
                radio.Checked = state;
                using var expected = Capture(radio);
                radio.AnimateCheckedChanges = true;
                using var actual = Capture(radio);
                EqualPixels(expected, actual);
            }
        }
        animated.UseSlidingKnob = false;
        animated.Checked = true;
        host.Show();
        Application.DoEvents();
        using var preview = Capture(card);
        Assert(preview.GetPixel(animated.Left + animated.Width / 2, animated.Top + 5).ToArgb()
            == animated.CheckedFillColor.ToArgb(), "The rendering preview did not contain visible controls.");
        preview.Save(Path.Combine(Path.GetTempPath(), "Kofge-Checked-Animation-preview.png"));
        AssertIdle();
    }

    internal static void AppDefaultsAndPageLifecycle()
    {
        foreach (var name in new[] { "CreateToggleSwitch", "CreateSegmentRadioButton" })
        {
            var factory = typeof(MainForm).GetMethod(name, Members)!;
            var option = factory.GetParameters().Single(parameter => parameter.Name == "animateCheckedChanges");
            Assert(option.HasDefaultValue && option.DefaultValue is true,
                $"{name} does not enable state transitions for all app callers.");
        }

        using var window = new TestWindow();
        using var host = new AtomicPageHost { Dock = DockStyle.Fill };
        window.Controls.Add(host);
        using var compact = Toggle(true);
        compact.Width = 64;
        compact.Height = 32;
        using var optionToggle = Toggle(true);
        using var mode = new SegmentRadioButton
        {
            Text = "Locked", Bounds = new Rectangle(10, 10, 118, 40), AnimateCheckedChanges = true
        };
        using var otherMode = new SegmentRadioButton
        {
            Text = "Amplified", Bounds = new Rectangle(135, 10, 136, 40), AnimateCheckedChanges = true
        };
        var controls = new ButtonBase[] { compact, mode, optionToggle };
        foreach (var control in controls)
        {
            var page = new Panel { BackColor = UiTheme.CardInner };
            page.Controls.Add(control);
            host.AddPage(page);
        }
        host.Pages[1].Controls.Add(otherMode);
        window.Show();
        Application.DoEvents();
        for (var index = 0; index < controls.Length; index++)
        {
            host.SelectedIndex = index;
            var control = controls[index];
            if (control is CheckBox checkBox) checkBox.Checked = true;
            else ((RadioButton)control).Checked = true;
            var clock = Clock()!;
            var tick = clock.GetType().GetMethod("OnTick", Members)!;
            tick.Invoke(clock, [null, EventArgs.Empty]);
            using (Capture(control)) { }
            var animation = control.GetType().GetField("_checkedAnimation", Members)!.GetValue(control)!;
            var startedAt = (long)animation.GetType().GetField("_startedAt", Members)!.GetValue(animation)!;
            animation.GetType().GetMethod("Advance", Members)!.Invoke(animation,
                [startedAt + Stopwatch.Frequency * 75 / 1000]);
            Assert(Amount(control) > 0f && Amount(control) < 1f, "A tab control did not animate.");
            host.SelectedIndex = (index + 1) % controls.Length;
            // Atomic redraw can defer child visibility notifications until the next clock tick.
            tick.Invoke(clock, [null, EventArgs.Empty]);
            Assert(Amount(control) == 1f, "A hidden tab retained an unfinished wave.");
            AssertIdle();
            if (control is CheckBox hiddenCheckBox) hiddenCheckBox.Checked = false;
            else ((RadioButton)control).Checked = false;
            Assert(Amount(control) == 0f, "Updating a hidden tab animated instead of settling.");
            AssertIdle();
        }
    }

    internal static void TourOptionsAndDisposal()
    {
        var values = new Dictionary<string, bool> { ["runAsAdministrator"] = true };
        var writes = 0;
        var type = typeof(MainForm).Assembly.GetType("KofgeClicker.FirstRunTourDialog")!;
        using var tour = (Form)Activator.CreateInstance(type, Members, null,
        [
            new Action<int>(_ => { }), new Action<string>(_ => { }),
            new Func<string, string>(_ => "F12"), new Action<string>(_ => { }),
            new Func<string>(() => "Hold"),
            new Func<string, bool>(key => values.GetValueOrDefault(key)),
            new Action<string, bool>((key, value) => { values[key] = value; writes++; })
        ], null)!;
        type.GetMethod("ShowStep", Members)!.Invoke(tour, [6]);
        var controls = ((IEnumerable<Control>)type.GetField("_dynamicControls", Members)!.GetValue(tour)!)
            .OfType<ToggleSwitchCheckBox>().ToArray();
        Assert(controls.Length == 5 && controls.All(control => control.AnimateCheckedChanges),
            "Tour option toggles do not all enable state animation.");
        var toggle = controls[0];
        Assert(toggle.Checked && Amount(toggle) == 1f && writes == 0,
            "Initializing tour controls changed settings or animated hidden controls.");
        using var window = new TestWindow();
        window.Controls.Add(toggle);
        toggle.Location = new Point(10, 10);
        window.Show();
        Application.DoEvents();
        toggle.Checked = false;
        Assert(!values["runAsAdministrator"] && writes == 1, "Tour animation delayed the option callback.");
        SampleAt([toggle], 75);
        Assert(Amount(toggle) > 0f && Amount(toggle) < 1f, "The tour option did not animate.");
        type.GetMethod("ShowStep", Members)!.Invoke(tour, [0]);
        Assert(controls.All(control => control.IsDisposed), "Changing tour steps retained old controls.");
        AssertIdle();
    }

    private static ToggleSwitchCheckBox Toggle(bool animated) => new()
    {
        Bounds = new Rectangle(10, 10, 112, 40), UseSlidingKnob = false,
        CheckedFillColor = Color.FromArgb(56, 136, 78), CheckedBorderColor = Color.FromArgb(91, 170, 111),
        UncheckedFillColor = Color.FromArgb(145, 59, 63), UncheckedBorderColor = Color.FromArgb(190, 90, 94),
        AnimateCheckedChanges = animated
    };

    private static void SampleAt(IEnumerable<Control> controls, int milliseconds)
    {
        var clock = Clock()!;
        clock.GetType().GetMethod("OnTick", Members)!.Invoke(clock, [null, EventArgs.Empty]);
        foreach (var control in controls)
        {
            using (Capture(control)) { }
            var animation = control.GetType().GetField("_checkedAnimation", Members)!.GetValue(control)!;
            var startedAt = (long)animation.GetType().GetField("_startedAt", Members)!.GetValue(animation)!;
            animation.GetType().GetMethod("Advance", Members)!.Invoke(animation,
                [startedAt + Stopwatch.Frequency * milliseconds / 1000]);
        }
    }

    private static float Amount(Control control)
    {
        var animation = control.GetType().GetField("_checkedAnimation", Members)!.GetValue(control)!;
        return (float)animation.GetType().GetProperty("Amount", Members)!.GetValue(animation)!;
    }
    private static object? Clock() => ClockType.GetField("_current", Members)!.GetValue(null);
    private static int ActiveCount() => Clock() is { } clock
        ? (int)clock.GetType().GetField("_active", Members)!.GetValue(clock)!.GetType()
            .GetProperty("Count")!.GetValue(clock.GetType().GetField("_active", Members)!.GetValue(clock))! : 0;
    private static void AssertIdle()
    {
        Assert(ActiveCount() == 0, "State animation remained registered while idle.");
        if (Clock() is not { } clock) return;
        Assert(!((System.Windows.Forms.Timer)clock.GetType().GetField("_timer", Members)!.GetValue(clock)!).Enabled,
            "The shared timer kept running while idle.");
        Assert(((ICollection)clock.GetType().GetField("_frame", Members)!.GetValue(clock)!).Count == 0,
            "The shared frame buffer retained controls.");
    }
    private static void Invoke(Control owner, string method, params object[] arguments) =>
        owner.GetType().GetMethod(method, Members)!.Invoke(owner, arguments);
    private static Bitmap Capture(Control control)
    {
        var image = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(image, control.ClientRectangle);
        return image;
    }
    private static Color Fill(Control control)
    {
        using var image = Capture(control);
        return image.GetPixel(control.Width / 2, 5);
    }
    private static void EqualPixels(Bitmap expected, Bitmap actual)
    {
        for (var y = 0; y < expected.Height; y++)
            for (var x = 0; x < expected.Width; x++)
                Assert(expected.GetPixel(x, y) == actual.GetPixel(x, y), $"State animation changed final pixel at {x},{y}.");
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        Application.DoEvents();
    }
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private sealed class TestWindow : Form
    {
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 120);
            ClientSize = new Size(340, 145);
        }
        protected override bool ShowWithoutActivation => true;
    }
}
