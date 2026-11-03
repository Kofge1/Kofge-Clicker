# Pixel UI Preview

The pixel style is available in the main v1.1.0 executable through Options →
Appearance, alongside Classic (the default). Restart to apply a selection.
This optional build remains an isolated visual-test executable.

```powershell
dotnet publish Kofge-Clicker/Kofge-Clicker.csproj -c Release -r win-x64 --self-contained true -p:PixelUiPreview=true -o publish-pixel-preview
```

Run `Kofge-Clicker.PixelPreview.exe` after closing the normal application.
The preview shares the single-instance guard to avoid competing input hooks.
Its settings and recordings live in `%LOCALAPPDATA%/Kofge-Clicker.PixelPreview`,
not the normal application's data directory. Automatic updates and legacy-data
migration are disabled. Windows startup registration uses a separate
`Kofge-Clicker.PixelPreview` shortcut/registry name and does not clean up stable
or legacy startup entries. The administrator option uses the preview's isolated
settings and requests elevation on its next launch, never for the stable EXE.
The clicker itself still sends real input if enabled.

All tabs, dialogs, first-run tour pages, notifications, tooltips, menus and the
macro journal receive the pixel skin. Existing controls and input behavior are
retained. Main builds activate it from the saved appearance preference without
changing data folders or disabling updates. The UI performance option,
badge and sampling classes have been removed; the shared animation clock and
directional transitions remain, without a new animation timer.

Text uses Monocraft's more detailed Cyrillic glyphs at the existing font sizes.
Each grapheme is rasterized separately on the native 9 px grid and enlarged
once from a fixed origin. Text is assembled at integer positions without
rescaling the whole line, so repeated letters keep identical pixels across
control widths, alignments and string positions. 85% horizontal advances keep
long captions inside the existing controls. Caches are bounded to 512 glyph
images, 256 complete text bitmaps (at most 16 MiB) and 256 measurement results
for reuse. Larger text surfaces are drawn without retaining their bitmaps.
Text is aligned using visible glyph bounds rather than font line boxes.
Multiline blocks keep at least a 6 px foreground gap between lines, and pixel
outlines stay inside the final drawable row/column of each control.
The status label alone opts into a square whole-pixel grid (2x2 at its current
font setting). Glyph widths and spaces use integer proportional advances;
there is no horizontal squeezing or fractional cell sizing. Explicit status
lines are ellipsized independently if too long, preserving the two-line footer
and its original text data. Other controls use the shared body-text renderer.
The complete status text is offset 2 px down inside its unchanged label bounds.

The behavior-check project also supports a PixelUiPreview build. Select the
`pixel-all-*` checks to render every window, tab and tour page in RU/EN. Those
checks use a temporary data directory and suppress global input hooks, normal
Shown handlers, activation and window opacity. Set `KOFGE_PIXEL_SURFACE_SNAPSHOTS`
to an output directory to save the off-screen renders for visual inspection.

Font: Monocraft, SIL Open Font License 1.1. The unmodified font and license are
bundled as resources in the executable. Source: IdreesInc/Monocraft, tag v4.2.1,
`dist/Monocraft-ttf/Monocraft.ttf`. No system font installation is performed.
See `Kofge-Clicker/Assets/PixelPreview/Monocraft-OFL.txt` for the full license.
