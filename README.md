# Kofge-Clicker

### Kofge-Clicker — Free & Open-Source for Windows

Kofge-Clicker is a fast and customizable Windows auto clicker with humanized clicking, multiple click patterns, advanced hotkeys, profiles and window targeting.

**100% Free • No Ads • No Subscriptions • No Feature Limits • Portable • Open Source**

[![Download](https://img.shields.io/badge/Download-Latest%20Release-success?style=for-the-badge)](https://github.com/Kofge1/Kofge-Clicker/releases/latest)
[![Latest Release](https://img.shields.io/github/v/release/Kofge1/Kofge-Clicker?style=for-the-badge)](https://github.com/Kofge1/Kofge-Clicker/releases/latest)

![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-blue?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8-purple?style=for-the-badge)
![License](https://img.shields.io/github/license/Kofge1/Kofge-Clicker?style=for-the-badge)

<img width="1100" height="635" alt="Kofge-Clicker main interface" src="https://github.com/user-attachments/assets/8e742f98-5788-48d9-b2d3-e4b8ee8b6faa" />

Kofge-Clicker is a free and open-source auto clicker for Windows built for precision, flexibility and ease of use.

It combines configurable clicking, humanized timing, multiple click patterns, global hotkeys, profile management and window targeting in a lightweight single-file application.

[Download](#download) • [Features](#features) • [Security & Transparency](#security--transparency) • [Build from Source](#build-from-source) • [Feedback](#feedback)

---

## Why Kofge-Clicker?

- **Completely free** — no paid edition, subscriptions or locked features.
- **No advertisements** — Kofge-Clicker is focused only on its functionality.
- **Open source** — the complete source code is available in this repository.
- **Portable** — distributed as a single self-contained Windows executable.
- **Flexible** — profiles, hotkeys, click patterns and window targeting can be configured for different use cases.
- **Actively developed** — new releases, fixes and usability improvements are published through GitHub Releases.
- **English and Russian interface** — language can be switched inside Kofge-Clicker.

## Features

### Clicking

- Adjustable click rate from **1 to 100 CPS**.
- Multiple click patterns and timing behaviors.
- Humanized click timing options.
- Configurable mouse-button behavior and activation modes.
- Built-in click test with a live CPS counter.

### Hotkeys

- Global hotkeys that work while Kofge-Clicker is not focused.
- Keyboard and mouse buttons can be used in hotkey combinations.
- Modifier-key support.
- Hotkey conflict detection.
- Self-generated input is distinguished from physical input so Kofge-Clicker does not trigger itself.

### Profiles

- Save multiple configurations for different tasks or applications.
- Quickly switch between profiles.
- Keep click settings, hotkeys and other options organized instead of reconfiguring them every time.

### Window Targeting

- Bind clicking behavior to a selected application window.
- Useful when you want Kofge-Clicker to operate with a specific target instead of relying only on the currently focused window.
- Optional administrator mode is available for compatibility with applications running with elevated privileges.

### Convenience

- English and Russian interface.
- Automatic language detection on first launch.
- Built-in update checking through GitHub Releases.
- Update notifications and in-app update support.
- System tray support.
- Hover explanations for settings and controls.

## Download

The official builds are published only through this repository's **GitHub Releases** page.

### [Download the latest Kofge-Clicker release](https://github.com/Kofge1/Kofge-Clicker/releases/latest)

**Supported systems:**

- Windows 10 x64
- Windows 11 x64

Kofge-Clicker is published as a **self-contained single-file executable**, so a separate .NET installation is not required for the official release.

> [!IMPORTANT]
> For safety, download Kofge-Clicker only from the official `Kofge1/Kofge-Clicker` GitHub repository and its Releases page.

## Gallery

<details>
<summary><strong>Click Patterns</strong></summary>
<br>
<img width="1100" height="635" alt="Kofge-Clicker click patterns" src="https://github.com/user-attachments/assets/9d49ba13-e825-4379-8bac-a7fc4b733624" />
</details>

<details>
<summary><strong>Hotkeys</strong></summary>
<br>
<img width="1100" height="635" alt="Kofge-Clicker hotkey settings" src="https://github.com/user-attachments/assets/78c61178-bf15-4b57-beba-bca1798ccf39" />
</details>

<details>
<summary><strong>Profiles</strong></summary>
<br>
<img width="1100" height="635" alt="Kofge-Clicker profiles" src="https://github.com/user-attachments/assets/4d64f11b-662b-4cb4-837e-dd705a803971" />
</details>

<details>
<summary><strong>Window Targeting & Options</strong></summary>
<br>
<img width="1100" height="635" alt="Kofge-Clicker window targeting and options" src="https://github.com/user-attachments/assets/37683f8d-3425-43a3-96de-7d0569d7bf09" />
</details>

## Security & Transparency

Kofge-Clicker is open source. You can inspect the source code in this repository, review how input handling and updates work, or build Kofge-Clicker yourself.

### Update security

Kofge-Clicker's updater is designed to use the official GitHub release channel.

Before applying an update, Kofge-Clicker validates the downloaded executable using available release information, including:

- The expected GitHub download source.
- The expected file size when available.
- The **SHA-256 digest** provided by GitHub when available.
- The executable format and application version.
- Protection against installing the same or an older version over the current one.

GitHub also displays a SHA-256 digest for release assets, allowing users to verify downloaded files independently.

### Local data

Kofge-Clicker does not require a user account and contains no advertising. Settings, logs and related local data are stored on the user's computer under the Kofge-Clicker application-data directory.

### Administrator mode

Running Kofge-Clicker as administrator is **optional**.

Windows applications running with elevated privileges can require another application to run at the same privilege level for some kinds of input interaction. For that reason, Kofge-Clicker includes an optional **Run as Administrator** setting.

Kofge-Clicker does not request elevation for normal use unless that option is enabled or an update needs permission to replace a protected executable.

### Global hotkeys and input hooks

To detect configured hotkeys while another application is focused, Kofge-Clicker uses standard Windows low-level keyboard and mouse hooks.

Generated mouse input is marked so Kofge-Clicker can distinguish its own input from physical input and avoid triggering itself.

These mechanisms are part of Kofge-Clicker's core clicking and global-hotkey functionality and can be reviewed directly in the source code.

## Windows SmartScreen / Browser Warnings

Kofge-Clicker is a relatively new Windows application distributed as a standalone executable. New or uncommon executable files can sometimes be shown as unrecognized by Windows SmartScreen or flagged by a browser for additional checking while reputation is still being established.

A reputation warning is not the same thing as a malware detection. You should still verify that the file was downloaded from the official repository before running it.

For maximum transparency, you can:

1. Download only from the official GitHub Releases page.
2. Check the SHA-256 digest shown by GitHub for the release asset.
3. Review the source code in this repository.
4. Build Kofge-Clicker from source yourself.

More information about SmartScreen application reputation is available in the [official Microsoft documentation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Build from Source

### Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

Clone the repository:

```bash
git clone https://github.com/Kofge1/Kofge-Clicker.git
cd Kofge-Clicker
```

Build a development version:

```bash
dotnet build Kofge-Clicker/Kofge-Clicker.csproj -c Release
```

Publish a self-contained Windows x64 executable:

```bash
dotnet publish Kofge-Clicker/Kofge-Clicker.csproj -c Release -r win-x64 --self-contained true
```

Kofge-Clicker targets **.NET 8 for Windows** and is configured to publish as a self-contained single-file application.

## Releases & Changelog

Version history, release notes and official executable downloads are available on the [GitHub Releases](https://github.com/Kofge1/Kofge-Clicker/releases) page.

## Feedback

Kofge-Clicker is actively developed, and feedback is welcome.

- No GitHub account? [Leave a public review on the Kofge-Clicker website](https://kofge1.github.io/Kofge-Clicker/#reviews)
- Found a bug? [Open an issue](https://github.com/Kofge1/Kofge-Clicker/issues/new/choose)
- Have an idea or question? [Start a discussion](https://github.com/Kofge1/Kofge-Clicker/discussions)
- Community poll: [English](https://github.com/Kofge1/Kofge-Clicker/discussions/3) • [Русский](https://github.com/Kofge1/Kofge-Clicker/discussions/4)

## License

Kofge-Clicker is released under the [MIT License](LICENSE).

---

If Kofge-Clicker is useful to you, consider starring the repository. It helps other people discover the project and gives useful feedback that continued development is worthwhile.
