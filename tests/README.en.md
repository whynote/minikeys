[中文](README.md)

The tests use the .NET Framework compiler included with Windows and download no dependencies. Run `.\build.ps1` from the project directory first.

Run checks that do not require an interactive desktop:

```powershell
.\tests\run.ps1 -ModelOnly
```

This mode checks key parsing, settings persistence, v1 settings migration, language selection, toolbar icons, layout, fading, and saving settings while minimized. It creates hidden forms without displaying a window, moving the pointer, or injecting input. Vertical layouts cover widths from 40 to 284px and toolbar wrapping boundaries. A regression check switches the narrow vertical layout back to four columns and verifies that all toolbar icons immediately return to one row. Controls must remain separate and inside the form; key tiles must be at least 24×24px.

Fade checks call the update method with controlled timestamps to verify the four-second delay, gradual transition, 65% opacity floor, and full opacity on hover, interaction, an open settings dialog, or disabling fading. Hidden component checks exercise `WindowState`, `RestoreBounds`, and saved settings. They do not establish that real taskbar restoration or desktop input focus works.

The tests render the form and its actual child controls with `DrawToBitmap`, generating `docs/images/v1.1.0.png` for the default horizontal layout and `docs/images/v1.1.0-vertical.png` for the minimum vertical layout. These are component previews, not desktop screenshots; they do not show desktop transparency composition.

Run full tests on an interactive desktop with a Chinese Pinyin input method:

```powershell
.\tests\run.ps1
```

Full mode runs the checks above, then opens its own temporary text window. Real mouse and keyboard input verify focus retention, 3/E/D/C, Shift, Chinese Pinyin composition, native border resizing, moving, pinning, minimizing, and input after programmatic restoration. It closes after about eight seconds. Do not use the mouse or keyboard during this run. The active input method must support converting `ce` to a Chinese character; the test does not change the system input method. If the temporary input window cannot obtain foreground focus, the test fails before sending input. Restoring by clicking the taskbar button still requires a manual check.

All test settings are written to `tests\tmp-<random-id>` and removed afterward. Production settings and existing documents are not changed. Full mode also updates `tests/preview.png`.
