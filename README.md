# Kofge-Clicker

### Advanced Windows Auto Clicker with Humanized Clicking, Profiles and Window Targeting

Free • Portable • Open Source • Built with C#

[![Download](https://img.shields.io/badge/Download-Latest%20Release-success?style=for-the-badge)](https://github.com/Kofge1/AutoClicker/releases/latest)

![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011-blue?style=for-the-badge) ![.NET](https://img.shields.io/badge/.NET-8-purple?style=for-the-badge) ![License](https://img.shields.io/github/license/Kofge1/AutoClicker?style=for-the-badge)

<img width="1100" height="635" alt="image" src="https://github.com/user-attachments/assets/8e742f98-5788-48d9-b2d3-e4b8ee8b6faa" />

Kofge-Clicker is a free and open-source Windows auto clicker built for precision, flexibility and ease of use.

It combines humanized clicking, multiple click patterns, advanced hotkeys, profile management and window targeting in a lightweight portable application.

## Gallery

### Click Patterns
<img width="1100" height="635" alt="image" src="https://github.com/user-attachments/assets/9d49ba13-e825-4379-8bac-a7fc4b733624" />

### Hotkeys
<img width="1100" height="635" alt="image" src="https://github.com/user-attachments/assets/78c61178-bf15-4b57-beba-bca1798ccf39" />

### Profiles
<img width="1100" height="635" alt="image" src="https://github.com/user-attachments/assets/4d64f11b-662b-4cb4-837e-dd705a803971" />

### Window Targeting & Options
<img width="1100" height="635" alt="image" src="https://github.com/user-attachments/assets/37683f8d-3425-43a3-96de-7d0569d7bf09" />

## Features

### Clicking
- Adjustable CPS (1–100)
- Humanized Clicking
- Standard
- Burst
- Double
- Hold+Burst

---

### Profiles
- Save unlimited profiles
- Switch instantly

---

### Automation
- Window Targeting
- Toggle Mode
- Hold Mode
- Left / Right Mouse

---

### Hotkeys
- Panic Stop
- Toggle
- Show Window
- Next Profile

## Download

[![Download Latest Release](https://img.shields.io/github/v/release/Kofge1/AutoClicker?label=Download%20Latest%20Release&style=for-the-badge)](https://github.com/Kofge1/AutoClicker/releases/latest)

Download the latest Windows build from the GitHub Releases page:

➡️ **[Download Kofge-Clicker](https://github.com/Kofge1/AutoClicker/releases/latest)**

## Build From Source

Requirements:

- Windows 10 or Windows 11.
- .NET 8 SDK.

Build:

```powershell
dotnet build .\Kofge-Clicker\Kofge-Clicker.csproj
```

Publish a self-contained Windows executable:

```powershell
dotnet publish .\Kofge-Clicker\Kofge-Clicker.csproj -c Release -r win-x64 -o .\publish
```

## Data Location

Kofge-Clicker stores profiles, settings and logs here:

```text
%LocalAppData%\Kofge-Clicker
```

This folder contains:

- `settings.ini` for settings and profiles.
- `startup.log` for startup diagnostics.
- `input-diagnostics.log` for input/click diagnostics when events are recorded.

## Support

This project is currently free to use.

If you want to support development, you can donate via USDT:

```text
ERC20 - 0x5701793453c1d73a527af74f9b615717052c4738
TRC20 - TMTvgkSzEARmZ81HG2SE7nRf2KbC63tcBJ
```
**[Donation Alerts](https://www.donationalerts.com/r/kofge)**

Please double-check the network before sending. Transfers sent through the wrong network may be lost.

## Important Note

Use this tool only in apps, games and workflows where automation is allowed. Some games, apps and online services restrict auto clickers, macros or automated input.
