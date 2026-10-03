# Behavior Checks

Run on Windows with the .NET 8 SDK:

```powershell
dotnet run --project tests/Kofge-Clicker.BehaviorChecks/Kofge-Clicker.BehaviorChecks.csproj -c Release
```

The checks cover hotkey mapping, background mouse modifiers, minimum press
duration, repeats, cancellation, macro compatibility and storage validation,
journal edits, recording without mouse movement, and INI persistence.

Background playback uses a temporary test window without moving the physical
cursor. Storage checks use temporary directories and preserve application data.
Keep the mouse still while the checks run: the cursor-position assertion also
detects physical mouse movement and cannot distinguish it from application input.
The runner returns a nonzero exit code if a check fails.
