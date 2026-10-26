# Release history

[中文](CHANGELOG.md) · [Back to the English guide](README.en.md)

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
