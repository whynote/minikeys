[中文](README.md)

The tests use the .NET Framework compiler included with Windows and download no dependencies. Run `.\build.ps1` from the project directory first.

Run checks that do not require an interactive desktop:

```powershell
.\tests\run.ps1 -ModelOnly
```

This mode checks key parsing, settings persistence, migration, UI language selection, the five toolbar icons, layout, fading, and saving settings while minimized. Fresh settings use Chinese, WeChat voice input, a 60×244px vertical layout, and enabled idle fading at 30% opacity. Existing preferences must be retained. It creates hidden forms without displaying a window, moving the pointer, injecting input, or triggering voice shortcuts. Vertical layouts cover widths from 40 to 284px and toolbar wrapping boundaries. A regression check switches the narrow vertical layout back to four columns and verifies that all toolbar icons immediately return to one row. Controls must remain separate and inside the form; key tiles must be at least 24×24px.

Fade checks call the update method with controlled timestamps to verify the four-second delay, gradual transition, the 30% default and custom 20%/65%/70%/100% opacity, and full opacity on hover, interaction, an open settings dialog, or disabling fading. Chinese and English UI selections are checked independently of the Windows display language. Settings dialogs are checked for the saved language choice, defaults, and control bounds. Hidden component checks exercise `WindowState`, `RestoreBounds`, and saved settings. They do not establish that real taskbar restoration, desktop input focus, or voice input works.

The tests render the form and its actual child controls with `DrawToBitmap`, generating `docs/images/v1.2.0.png` for the default vertical layout and `docs/images/v1.2.0-vertical.png` for the minimum vertical layout. These are component previews, not desktop screenshots; they do not show desktop transparency composition. Chinese and English settings are verified through hidden component assertions. No settings image is generated because hidden native combo boxes do not render reliably.

Run full tests on an interactive desktop with a Chinese Pinyin input method:

```powershell
.\tests\run.ps1
```

Full mode runs the checks above, then opens its own temporary text window. Real mouse and keyboard input verify focus retention, 3/E/D/C, Shift, Chinese Pinyin composition, native border resizing, moving, pinning, minimizing, and input after programmatic restoration. It closes after about eight seconds and does not click the microphone or test voice input. Do not use the mouse or keyboard during this run. The active input method must support converting `ce` to a Chinese character; the test does not change the system input method. If the temporary input window cannot obtain foreground focus, the test fails before sending input. Restoring by clicking the taskbar button still requires a manual check.

All test settings are written to `tests\tmp-<random-id>` and removed afterward. Production settings and existing documents are not changed. Full mode also updates `tests/preview.png`.
