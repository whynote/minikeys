# MiniKeys · 迷你补键盘

**A small floating keypad for the few keys your physical keyboard can no longer type.**

[中文主页](README.md) · [Download the latest release](https://github.com/whynote/minikeys/releases/latest) · [Release history](CHANGELOG.en.md)

MiniKeys is a lightweight Windows utility that puts a few customizable keys on screen. It starts with **3, E, D, and C**, stays on top, and lets you keep typing in your current application by clicking those keys.

![MiniKeys v1.1.0 with the default four keys](docs/images/v1.1.0.png)

The narrowest tested four-key vertical layout (40 × 222 pixels):

![MiniKeys v1.1.0 in a narrow vertical layout](docs/images/v1.1.0-vertical.png)

> **v1.1.0 includes an English interface.** MiniKeys follows the Windows display language: Chinese for Chinese-language systems, English for all other languages. No language selector is needed.

## Download and run

1. Open the [latest release](https://github.com/whynote/minikeys/releases/latest), download `MiniKeys-v1.1.0-win-portable.zip`, and extract it.
2. Run `MiniKeys.exe`. No installation is required. The window stays on top by default.
3. Click the text field you want to type into, then click a key in MiniKeys.

MiniKeys sends keyboard events to your current application without taking focus away from its text field. It works with the active keyboard layout and input method, including Chinese Pinyin. Letter labels are uppercase key names; letter case follows your physical Shift / Caps Lock state.

Designed for **Windows 10 / 11** with the **.NET Framework 4 family** runtime. No Java, Python, or third-party packages are required. Chinese Pinyin input, focus preservation, moving, and resizing have been tested on Windows 10. **Windows 11 has not been tested.**

## Features

- Choose **1–24 keys**, in your preferred order, with **1–12 columns**.
- A compact icon toolbar: pin, settings, minimize, and close. A blue pin means always on top is enabled.
- Gently fade to **65% opacity** after the pointer has been outside the window for **4 seconds**. Hover or interact to restore full opacity immediately. Disable idle fading in settings if preferred.
- Click keys while preserving the target application's input focus.
- Shrink a vertical layout into a narrow strip; the four-key layout has been verified at **40 × 222 pixels**. Toolbar icons wrap as the window gets narrower.
- Drag the small grip at the top to move the window; drag an edge or corner to resize it. Keys resize with the window.
- Minimize to the taskbar and restore the previous position and size.
- Automatically restore keys, layout, position, size, the pinned state, and the fade setting on the next launch.

## Window controls

The toolbar uses icons, with tooltips in the detected interface language. When you make the window very narrow, the icons wrap onto additional rows.

| Icon | Action | State |
| --- | --- | --- |
| Pin | Toggle always on top | Blue means pinned |
| Gear | Open settings | Customize keys, columns, and idle fading |
| Short horizontal line | Minimize to the taskbar | Click the taskbar icon to restore |
| Cross | Exit MiniKeys | Your settings are saved |
| Small grip at the top | Drag to move the window | Drag an edge or corner to resize instead |

If another application covers MiniKeys after you unpin it, click its taskbar icon to bring it back. Restoring from the taskbar retains the previous position and size. **After restoring, click the target text field before clicking a MiniKeys button.**

### Idle fading

With idle fading enabled, leave the pointer outside the window for 4 seconds. MiniKeys then fades smoothly over about 0.4 seconds to 65% opacity, so the keys remain visible. Moving the pointer back into the window or interacting restores 100% opacity immediately. The option is on by default and can be turned off in settings.

### Customize keys

Click the **gear** icon to open settings. The text field near the top contains your key names, separated by spaces. For example:

```text
3 E D C Space Backspace
```

Set **keys per row** to `1` for a vertical stack, `4` for the default four-key horizontal row, or `2` for two columns. Save to apply. The reset button restores `3 E D C` in four columns.

Key names are case-insensitive. Supported names:

| Type | Names |
| --- | --- |
| Letters and digits | `A`–`Z`, `0`–`9` |
| Function keys | `F1`–`F12` |
| Editing | `Space`, `Enter`, `Backspace`, `Tab`, `Esc`, `Delete` |
| Arrows and navigation | `Left`, `Right`, `Up`, `Down`, `Home`, `End`, `PageUp`, `PageDown` |
| Punctuation keys | `-`, `=`, `[`, `]`, `;`, `'`, `,`, `.`, `/`, `\`, and the backtick key |

Punctuation output follows your active keyboard layout and input method. Each button sends one key; you can hold physical Shift / Ctrl while clicking.

## Settings and troubleshooting

Settings are stored locally at:

```text
%LOCALAPPDATA%\MiniKeys\settings.json
```

To reset everything, close MiniKeys, delete that file, and launch it again.

To upgrade from v1.0.0, close the old application and run the new version. Existing key and window settings are retained; idle fading is enabled by default for older configurations. The [v1.0.0 release and its original Chinese-interface guide](https://github.com/whynote/minikeys/releases/tag/v1.0.0) remain available.

If a key does not reach your application, click the target text field first. If that application runs as administrator, run MiniKeys with the same privileges.

MiniKeys uses Windows `SendInput` to send key-down and key-up events. It does not connect to the network, read or modify your clipboard, or monitor or log what you type.

## Build from source

Open PowerShell in the project directory and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The script uses the Windows .NET Framework C# compiler and creates `MiniKeys.exe`. The source is in [src/MiniKeys.cs](src/MiniKeys.cs). See [tests/README.md](tests/README.md) for the test commands.

See [CHANGELOG.en.md](CHANGELOG.en.md) for version history. This project uses the [MIT license](LICENSE).

## Validation scope

v1.1.0 passes 44 automated checks that do not interact with the desktop, covering settings migration, icons and compact layouts, fading and restoration, and saving normal bounds while minimized. The images above are rendered from the application's controls. Real mouse input and Chinese Pinyin were verified on Windows 10 for v1.0.0; the full desktop input test and focus behavior after an actual taskbar restore have not been rerun for this release. See the [English test guide](tests/README.en.md).
