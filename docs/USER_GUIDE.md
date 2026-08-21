# Kofge-Clicker User Guide

[Русское руководство](USER_GUIDE_RU.md) | [Download the latest release](https://github.com/Kofge1/Kofge-Clicker/releases/latest) | [Report a bug](https://github.com/Kofge1/Kofge-Clicker/issues/new/choose)

This guide explains how to install and configure Kofge-Clicker and how to use its modes, patterns, hotkeys, profiles, target-window restriction and startup options.

> [!IMPORTANT]
> Use automated clicking only where it is allowed. Rules can differ between applications, games and online services.

## Contents

- [Quick start](#quick-start)
- [States and indicators](#states-and-indicators)
- [Clicker tab](#clicker-tab)
- [Pattern tab](#pattern-tab)
- [Mouse tab](#mouse-tab)
- [Hotkeys tab](#hotkeys-tab)
- [Profiles tab](#profiles-tab)
- [Options tab](#options-tab)
- [Example configurations](#example-configurations)
- [Updates and local files](#updates-and-local-files)
- [Troubleshooting](#troubleshooting)
- [Frequently asked questions](#frequently-asked-questions)

## Quick start

1. Download `Kofge-Clicker.exe` from the [latest official release](https://github.com/Kofge1/Kofge-Clicker/releases/latest).
2. Run the file. The first launch of the self-contained EXE can take a little longer than later launches.
3. To change the interface language, use the `EN` or `RU` button in the bottom-left corner and restart when prompted.
4. Open the **Clicker** tab, select **Bind**, and press the key or mouse button you want to use.
5. Choose **Hold** or **Toggle** mode.
6. Select a CPS value. `10-15 CPS` is a practical starting point for testing.
7. Open the **Mouse** tab and choose the left or right mouse button.
8. Select **Apply**, then turn **Clicker state** on.
9. Use the assigned key: hold it in **Hold** mode or press it to start and stop in **Toggle** mode.

### New-user defaults

| Setting | Default value |
|---|---|
| Clicker state | OFF |
| Mode | Hold |
| Activation key | F2 |
| CPS | 15 |
| Human clicks | OFF |
| Mouse button | Left |
| Pattern | Standard |
| Rate behavior | Locked |
| Panic Stop | F12 |
| Show Window | F10 |
| Toggle Clicker | F7 |
| Next Profile | F9 |
| Target window | Any window, restriction disabled |
| Startup and tray options | Disabled |

## States and indicators

Kofge-Clicker distinguishes between being ready and actively sending clicks.

- **Red cross**: the clicker is disabled.
- **Green check mark**: the clicker is enabled and ready but is not currently clicking.
- **Yellow lightning bolt**: clicks are being sent right now.

The same status is shown in the system tray and taskbar. The bottom status panel also displays the active profile, activation key, mouse button, CPS, pattern and rate behavior.

## Clicker tab

### Clicker state

This is the main safety switch. While it is off, the activation key cannot start clicking.

An enabled clicker is not necessarily clicking. Actual clicking depends on the selected mode and activation key.

### Mode

**Hold**

- Clicks continue only while the activation key is held.
- Releasing the key stops clicks immediately.
- Best for short actions and precise control.

**Toggle**

- The first press starts clicking.
- The next press stops clicking.
- The key does not need to remain held.
- Keep an easy-to-reach Panic Stop key configured.

### Human clicks

Adds small random timing variations and occasional short pauses. As a result, live CPS may temporarily differ from the selected target.

Enable this when you want a less uniform sequence. Leave it disabled when exact, repeatable timing is more important.

### Activation key

1. Select **Bind**.
2. Wait for the input prompt.
3. Press a keyboard key, mouse button, or a combination using `Ctrl`, `Shift` or `Alt`.

Example: `Ctrl + LButton` requires holding `Ctrl` and the left mouse button.

### CPS

CPS means clicks per second. Kofge-Clicker supports values from `1` to `100`.

A high value does not guarantee that every click will be processed by the target application. Windows scheduling, computer performance, pattern delays and the target application can reduce the effective rate.

## Pattern tab

A pattern controls what happens during one click cycle.

### Standard

Sends one regular click per CPS tick. This is the most predictable pattern and the best starting point.

### Triple Click

Sends three clicks in one pattern cycle. **Gap** controls the delay between clicks in the group.

### Double Click

Sends two clicks in one pattern cycle. **Gap** controls the delay between them.

### Custom

Lets you configure all available pattern values:

- **Clicks**: number of clicks in the cycle.
- **Gap**: delay between grouped clicks.
- **Hold**: duration of the first held mouse press.
- **Press**: additional delay after pressing the selected mouse button.
- **Release**: additional delay after releasing the selected mouse button.

### Rate behavior

**Locked**

Kofge-Clicker accounts for the number of clicks in the pattern and tries to keep total output close to the selected CPS. This is the more predictable option.

**Amplified**

The pattern can add clicks above the base rate. The effective number of clicks can be noticeably higher than the selected CPS.

> [!TIP]
> If you are unsure, start with **Standard + Locked**.

## Mouse tab

Choose the mouse button Kofge-Clicker will press:

- **Left**: regular left click.
- **Right**: regular right click.

### Built-in click test

The test area counts both LMB and RMB regardless of the selected clicker button and displays live CPS.

Use it to verify:

1. Whether the activation key starts the clicker.
2. Whether the selected mouse button performs the expected action.
3. How closely the effective CPS follows the selected target.

Use **Reset Test** to clear the counter.

## Hotkeys tab

### Panic Stop

Immediately stops clicking, releases held mouse buttons and closes Kofge-Clicker. Default: `F12`.

### Show Window

Shows or hides the main window independently of the tray settings. Default: `F10`.

### Toggle Clicker

Changes the main clicker state without opening the window. Default: `F7`.

### Next Profile

Cycles through profiles from top to bottom and returns to the first after the last. Default: `F9`.

### Binding rules

- Hotkeys must not conflict with each other.
- Keyboard keys, side mouse buttons and modifier combinations are supported.
- Service hotkeys cannot use bare LMB, RMB or MMB without a modifier.
- If a hotkey requires `Ctrl`, `Shift` or `Alt`, that modifier must be held.
- An additional held modifier does not prevent the configured hotkey from working.
- **Reset All Hotkeys** restores safe defaults.

## Profiles tab

Profiles store separate configurations for different tasks.

A profile includes the main click settings, CPS, mode, pattern, mouse button, human clicks, hotkeys and target-window selection.

### Available actions

- **New**: creates a new profile.
- **Rename**: changes the active profile name.
- **Duplicate**: creates a copy of the current profile.
- **Delete**: removes the active profile. The only remaining profile cannot be deleted.
- **Export**: saves the profile to a separate file.
- **Import**: adds a profile from an exported file.
- **Set Startup**: chooses the profile loaded on the next launch.

### Recommended workflow

1. Create a profile with a descriptive name.
2. Configure the **Clicker**, **Pattern**, **Mouse** and **Hotkeys** tabs.
3. Select a target window if needed.
4. Select **Apply**.
5. Use **Set Startup** if this profile should load first.

## Options tab

### Run as administrator

The next launch will use administrator rights. This can be needed when the target application also runs elevated.

> [!IMPORTANT]
> Many games also require Kofge-Clicker to run as administrator. If clicking works on the desktop but not inside a game, enable this option first and restart the application.

Restart Kofge-Clicker after changing this setting. Windows may display a UAC prompt depending on system configuration.

### Launch hidden in tray

The next launch starts without showing the main window. Kofge-Clicker remains available through the system tray and the **Show Window** hotkey.

### Run on startup

Starts Kofge-Clicker automatically after signing in to Windows.

### Minimize (-) to tray

The window minimize button hides Kofge-Clicker in the system tray instead of minimizing it normally.

### Close window to tray

The window close button hides Kofge-Clicker in the tray instead of exiting. Use the tray exit command or Panic Stop to close it completely.

### Target window

This restriction allows clicking only while the selected application is focused.

1. Start the application you want to target.
2. Select **Refresh**.
3. Choose the application by its name and icon.
4. Enable **Only while selected window is focused**.
5. Select **Apply**.

Clicks pause when focus moves to another window. They become available again after returning to the selected application according to the current activation mode.

> [!NOTE]
> Target Window is not background clicking. Kofge-Clicker does not send clicks to a minimized or inactive application.

The list contains applications with visible top-level windows. Background services without a window are excluded. Multiple windows belonging to the same executable can be combined into one entry.

## Example configurations

### Simple repeated clicking

- Mode: **Hold**
- CPS: `10-15`
- Human clicks: optional
- Pattern: **Standard**
- Rate behavior: **Locked**

### Long-running task with manual start and stop

- Mode: **Toggle**
- CPS: `5-15`
- Pattern: **Standard**
- Rate behavior: **Locked**
- Panic Stop: keep it assigned to an easy-to-reach key

### Less uniform timing

- Mode: either
- CPS: moderate
- Human clicks: **ON**
- Pattern: **Standard**
- Rate behavior: **Locked**

### Settings for one application only

- Create a dedicated profile.
- Select the application under **Target Window**.
- Enable the target restriction.
- If the application runs as administrator, enable the same mode for Kofge-Clicker and restart it.

## Updates and local files

### Updates

Kofge-Clicker checks the official GitHub Releases channel. When a newer version is available, the application offers to download it. The same or an older version is not installed over the current version.

### Data location

```text
%LocalAppData%\Kofge-Clicker
```

Usually this expands to:

```text
C:\Users\YOUR_NAME\AppData\Local\Kofge-Clicker
```

Main files:

- `settings.ini`: settings and profiles.
- `startup.log`: startup diagnostics.
- `input-diagnostics.log`: input and clicking diagnostics.
- `Languages`: custom localization files when present.

The exact path depends on the Windows account name and is displayed on the **Profiles** tab.

### Backup

Close Kofge-Clicker and copy `%LocalAppData%\Kofge-Clicker` to a safe location.

For a single profile, use **Export** and **Import** instead.

## Troubleshooting

### The clicker is enabled but does not click

1. Check the indicator: a green check means ready, not actively clicking.
2. Press or hold the activation key according to the selected mode.
3. Open the **Mouse** tab and test clicking in the built-in test area.
4. If a target window is enabled, return to that application or temporarily disable the restriction.
5. Check whether the target application runs as administrator.
6. Assign a different activation key and test again.

### It works on the desktop but not in an application or game

- Try matching privilege levels: run both normally or both as administrator.
- Some applications and games block synthetic input.
- Full-screen modes, security software or online-service rules can also restrict operation.
- Check the rules of a game or server before using automated input.

### A hotkey does not work

1. Select **Bind** and assign it again.
2. Make sure it does not conflict with another Kofge-Clicker function.
3. Check combinations using `Ctrl`, `Shift` and `Alt`.
4. For diagnosis, try a standard function key such as `F2`.

### The target application is missing from the list

1. Start the application and wait for its window to appear.
2. Select **Refresh**.
3. Confirm that it has a visible window and is not only a background process.
4. If Windows blocks process metadata access, the technical `.exe` name can be shown instead.

### Kofge-Clicker is running but its window is missing

- Use the **Show Window** hotkey. The default is `F10`.
- Check the Kofge-Clicker icon in the system tray.
- If hidden startup is enabled, this behavior is expected.

### Reset all settings

1. Close Kofge-Clicker.
2. Open `%LocalAppData%\Kofge-Clicker`.
3. Back up `settings.ini`.
4. Rename or delete `settings.ini`.
5. Start Kofge-Clicker. Default settings will be created.

### Windows shows SmartScreen or antivirus warnings

Download the EXE only from the [official GitHub Releases](https://github.com/Kofge1/Kofge-Clicker/releases). Kofge-Clicker uses global input hooks and generates synthetic mouse input, so heuristic scanners can treat a new unsigned executable cautiously.

You can compare the SHA-256 digest on the release page, inspect the source code, or build the application yourself.

## Frequently asked questions

### Does Kofge-Clicker click in the background?

No. Target Window limits clicks to the selected application while it is focused. It does not receive clicks while minimized or inactive.

### Why is live CPS different from the selected value?

Human clicks, the selected pattern, rate behavior, configured delays, Windows load and the target application's input handling can all affect effective CPS.

### Can I bind a side mouse button?

Yes. Side mouse buttons can be used for the activation key and service hotkeys.

### Is a separate .NET installation required?

No. The official self-contained EXE includes the required runtime.

### Why is the EXE relatively large?

The official build contains the required .NET runtime in one file so it can run without separately installed dependencies.

### Is Kofge-Clicker portable?

The EXE can be moved, but settings and profiles are stored separately under `%LocalAppData%\Kofge-Clicker`.

### How do I completely uninstall it?

1. Disable **Run on startup** on the **Options** tab.
2. Exit Kofge-Clicker.
3. Delete `Kofge-Clicker.exe`.
4. Optionally delete `%LocalAppData%\Kofge-Clicker` to remove settings and logs.

## Feedback

- [Report a bug](https://github.com/Kofge1/Kofge-Clicker/issues/new/choose)
- [Suggest an improvement or ask a question](https://github.com/Kofge1/Kofge-Clicker/discussions)
- [Leave a public review without a GitHub account](https://kofge1.github.io/Kofge-Clicker/#reviews)
