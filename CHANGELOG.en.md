# Release history

[中文](CHANGELOG.md) · [Back to the English guide](README.en.md)

## v1.2.0 · 2026-10-27

One application with selectable voice input, interface language, and fade opacity.

- Added a leftmost microphone control; narrow layouts keep it first as toolbar icons wrap.
- Unified into `MiniKeys.exe`. Select WeChat or Windows voice input in settings; WeChat is the default. No separate downloads or builds are needed.
- In WeChat mode, click to hold Ctrl + Win and click again to release. Keys are released before other keys, window actions, and normal exit; fading is suspended while held. Windows mode sends a complete Win + H shortcut on each click, leaving listening state to Windows.
- Added manual interface language selection. Chinese is the default; select English and save to apply immediately and remember the choice. Interface language and voice input method are separate settings.
- Idle fading is enabled by default. Set faded opacity from 20% to 100%, with a default of 30% opacity. Higher values keep the window clearer.
- Default to a one-column four-key layout, initially 60 × 244 pixels. Existing saved preferences are retained.
- Replaced the settings icon with a closed eight-tooth gear.
- Updated the Chinese home page, English guide, and bilingual voice input notes. The unified download is `MiniKeys-v1.2.0-win-portable.zip`.

Voice validation: the user verified the previous local WeChat behavior. Voice input and recording were not triggered again for this release. Windows voice input and Windows 11 have not been tested. See the [voice input guide](VOICE-INPUT.md) and [English test guide](tests/README.en.md).

## v1.1.0 · 2026-10-27

A smaller, quieter floating keypad with an English interface.

- Replaced the text pin control with a pin icon; blue means always on top is enabled. Settings, minimize, and close also use icons.
- Toolbar icons wrap in narrow windows. A small drag grip replaces the title text, allowing the four-key vertical layout to shrink to 40 pixels wide.
- Added idle fading: after the pointer has been outside the window for 4 seconds, smoothly fade over about 0.4 seconds to 65% opacity. Hover or interact to restore 100% immediately. Disable the feature in settings if preferred.
- Added minimize to the taskbar, preserving the previous window position and size on restore. After restoring, click the target text field before using the keypad.
- Automatically use Chinese for Chinese Windows display languages, and English for all other languages.
- Retain v1.0.0 keys, layout, and window settings on upgrade. Idle fading is enabled by default for older configurations.
- Updated the Chinese and English guides and release history.

## v1.0.0 · 2026-10-27

First public release: a small floating Windows keypad for replacing a few broken physical keys.

- Default keys: `3 E D C`; customize 1–24 keys and their order.
- Choose 1–12 columns for a horizontal row, a vertical stack, or a grid.
- Always on top by default, with a pin toggle.
- Preserve the target text field's focus while clicking keys. Use the active input method, including Chinese Pinyin.
- Drag to move and resize the window; keys scale with it.
- Automatically save and restore keys, layout, position, size, and the pinned state.
- Portable, with no installation or third-party dependencies. Settings remain local.
- Chinese home page, a complete English guide, and bilingual release notes. The application interface in this version remains Chinese.

Validation: Chinese Pinyin input, focus preservation, physical Shift combinations, moving, resizing, and settings persistence have been tested on Windows 10. Windows 11 has not been tested.
