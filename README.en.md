# MiniKeys · 迷你补键盘

**A small floating keypad for the few keys your physical keyboard can no longer type.**

[中文主页](README.md) · [Download the latest release](https://github.com/whynote/minikeys/releases/latest) · [Release history](CHANGELOG.en.md)

MiniKeys is a lightweight Windows utility that puts a few customizable keys on screen. It starts with **3, E, D, and C**, stays on top, and lets you keep typing in your current application by clicking those keys.

![MiniKeys v1.0.0 with the default four keys](docs/images/v1.0.0.png)

> The **v1.0.0 interface is in Chinese**. This English guide explains every control. An English interface is not included in this release.

## Download and run

1. Open the [latest release](https://github.com/whynote/minikeys/releases/latest), download the portable ZIP, and extract it. The first-release package is `MiniKeys-v1.0.0-win-portable.zip`.
2. Run `MiniKeys.exe`. No installation is required. The window stays on top by default.
3. Click the text field you want to type into, then click a key in MiniKeys.

MiniKeys sends keyboard events to your current application without taking focus away from its text field. It works with the active keyboard layout and input method, including Chinese Pinyin. Letter labels are uppercase key names; letter case follows your physical Shift / Caps Lock state.

Designed for **Windows 10 / 11** with the **.NET Framework 4 family** runtime. No Java, Python, or third-party packages are required. Chinese Pinyin input, focus preservation, moving, and resizing have been tested on Windows 10. **Windows 11 has not been tested.**

## Features

- Choose **1–24 keys**, in your preferred order, with **1–12 columns**.
- Keep the keypad on top, or turn that option off.
- Click keys while preserving the target application's input focus.
- Drag the top bar to move the window; drag an edge or corner to resize it. Keys resize with the window.
- Automatically restore keys, layout, position, size, and the pinned state on the next launch.

## Using the Chinese interface

The screenshot above shows the default layout. The toolbar controls are at the top right.

| Chinese label | Meaning | Action |
| --- | --- | --- |
| `已置顶` | Pinned / always on top | Click to turn always-on-top off |
| `置顶` | Pin / always on top is off | Click to turn always-on-top on |
| `设置` | Settings | Customize keys and columns |
| `×` | Close | Exit MiniKeys |
| `补键 ···` | Keypad / drag area | Drag the empty area around this label to move the window |

Drag a window edge or the bottom-right corner to resize. If another application covers MiniKeys after you unpin it, click its taskbar icon to bring it back.

### Customize keys

Click **设置** (Settings). The text field near the top contains your key names, separated by spaces. For example:

```text
3 E D C Space Backspace
```

The numeric field labeled **每行按键数** means **keys per row**. Set it to `1` for a vertical stack, `4` for the default four-key horizontal row, or `2` for two columns.

| Settings label | Meaning |
| --- | --- |
| `要显示的按键（用空格分隔，可调整顺序）` | Keys to display, separated by spaces, in the desired order |
| `每行按键数` | Keys per row / number of columns |
| `恢复 3 E D C` | Restore the default keys and four-column layout |
| `保存` | Save and apply |
| `取消` | Cancel |

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

If a key does not reach your application, click the target text field first. If that application runs as administrator, run MiniKeys with the same privileges.

MiniKeys uses Windows `SendInput` to send key-down and key-up events. It does not connect to the network, read or modify your clipboard, or monitor or log what you type.

## Build from source

Open PowerShell in the project directory and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The script uses the Windows .NET Framework C# compiler and creates `MiniKeys.exe`. The source is in [src/MiniKeys.cs](src/MiniKeys.cs). See [tests/README.md](tests/README.md) for the test commands.

See [CHANGELOG.en.md](CHANGELOG.en.md) for version history. This project uses the [MIT license](LICENSE).
