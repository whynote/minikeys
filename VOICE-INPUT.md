# 语音输入 · Voice input

[English](#english) · [中文主页](README.md)

从 v1.2.0 起，只需一个 `MiniKeys.exe`。点击齿轮，在设置中选择“微信语音输入”或“Windows 微软语音输入”，保存后应用；无需更换程序或重新编译。默认选择微信。界面语言可独立切换为中文或 English，默认中文。

麦克风位于工具栏最左侧；窗口缩窄时，工具图标按顺序折行，麦克风始终在第一位。旧浮窗仍在运行时，请先退出旧窗口再打开新版。

## 微信语音输入（默认）

打开并登录支持语音输入的电脑版微信，保持微信的默认 **Ctrl + Win** 语音快捷键，点击想输入文字的位置，然后：

1. 点击麦克风：模拟按住左 Ctrl + 左 Win。按钮变红，表示 MiniKeys 正保持快捷键，并不代表已读取到微信的录音状态。
2. 说话。
3. 再次点击麦克风：释放 Win 和 Ctrl，交由微信结束此次语音输入。

这是“点一次开始保持、再点一次释放”，不需要一直按住鼠标。快捷键来源见微信官方 [4.1.8 更新日志](https://weixin.qq.com/updates?platform=windows&version=4.1.8)。如果更改过微信的快捷键，需要恢复为此组合。

保持期间，浮窗不会淡化。点击普通补键、图钉、设置、最小化、移动/缩放或正常退出时，会先释放本程序保持的组合键。此时 Ctrl / Win 实际处于按下状态；操作实体键盘上的其他按键或快捷键前，请先点麦克风结束。

此前的本地微信语音行为已由用户亲测可用。本次发布未重新触发语音或录音。

## Windows 微软语音输入

在同一个程序的设置中选择“Windows 微软语音输入”。先点击目标输入框，再点击麦克风。每次点击都完整发送一次 **Win + H**，包含按下和释放，不会持续按住修饰键。Windows 自己管理监听和停止状态；按钮不模拟“正在录音”的指示。

使用方法见[微软帮助](https://support.microsoft.com/en-us/accessibility/windows/use-voice-typing-to-talk-instead-of-type-on-your-pc)。微软[官方博客](https://blogs.windows.com/windows-insider/2019/04/26/announcing-windows-10-insider-preview-build-18885/)说明再次按 Win + H 可停止听写，具体行为由系统版本决定。本次发布未实测 Windows 语音输入。

## 语言、布局与淡化

- 默认中文界面，可在设置中切换 English；保存后立即生效并记住选择。界面语言不决定语音方式或识别语言，识别语言由微信或 Windows 管理。
- 默认四键竖排（一列），初始尺寸 60 × 244 像素；可拖动移动或缩放。
- 闲置淡化默认开启：鼠标离开 4 秒后开始淡化，回来或交互时恢复清晰。
- “淡化后不透明度”可设为 **20%–100%**，默认 **30% 不透明度**。数值越大越清楚，100% 表示始终清晰。
- 已保存的配置继续沿用；上述默认值用于首次运行或缺少对应配置的情况。

## 构建与验证

```powershell
.\build.ps1
```

无参数统一构建 `MiniKeys.exe`，语音方式和界面语言均在程序设置中切换。语音识别由微信或 Windows 执行；MiniKeys 只发送快捷键，不访问麦克风，也不发送聊天消息。Windows 11 尚未实测。其他验证范围见 [tests/README.md](tests/README.md)。

---

## English

Starting with v1.2.0, there is a single `MiniKeys.exe`. Click the **gear**, select **WeChat voice input** or **Windows voice input**, and save. No separate executable or build is needed. WeChat is the default. Interface language is a separate setting: Chinese by default, with English available in settings.

The microphone is the leftmost toolbar control. In narrow layouts, it remains first as icons wrap. Close the old floating window before launching the updated executable.

### WeChat voice input (default)

Open and log in to a compatible desktop WeChat version, using its default **Ctrl + Win** voice shortcut. Focus the target text field, then:

1. Click the microphone to hold Left Ctrl + Left Win. Red means MiniKeys is holding the shortcut, not that it has detected WeChat's recording state.
2. Speak.
3. Click the microphone again to release Win and Ctrl, allowing WeChat to finish voice input.

You do not need to hold the mouse button. See WeChat's [official 4.1.8 notes](https://weixin.qq.com/updates?platform=windows&version=4.1.8) for the shortcut. If you changed the shortcut in WeChat, restore this combination.

Fading is suspended while the shortcut is held. Keys are released before other keypad actions, settings, pinning, minimizing, moving/resizing, and normal exit. Release the shortcut before typing physical keys or using other keyboard shortcuts.

The user verified the previous local WeChat behavior. Voice input and recording were not triggered again for this release.

### Windows voice input

Select **Windows voice input** in settings, focus the target text field, and click the microphone. Each click sends a complete **Win + H** shortcut, including press and release events. Modifiers are not held between clicks. Windows manages listening state; MiniKeys does not infer or display a recording state.

See [Microsoft's guide](https://support.microsoft.com/en-us/accessibility/windows/use-voice-typing-to-talk-instead-of-type-on-your-pc). Microsoft's [official blog](https://blogs.windows.com/windows-insider/2019/04/26/announcing-windows-10-insider-preview-build-18885/) describes pressing Win + H again to stop dictation; behavior depends on the Windows version. Windows voice input has not been tested for this release.

### Language, layout, and fading

- The interface defaults to Chinese. Select **English** and save to apply immediately and remember the choice. Interface language does not determine the voice method or recognition language; WeChat or Windows manages recognition language.
- The default layout is four keys in one column, initially **60 × 244 pixels**. Drag to move or resize it.
- Idle fading is enabled by default. It starts after the pointer has been outside for **4 seconds**; hovering or interacting restores full opacity.
- Set **idle opacity** from **20% to 100%**, default **30% opacity**. A higher value keeps the window clearer; 100% keeps it fully opaque.
- Existing saved preferences are retained. Defaults apply on first launch or when the relevant setting is missing.

### Build and validation

Run `./build.ps1` without arguments to build the unified `MiniKeys.exe`. Choose the voice input method and interface language in settings. WeChat or Windows performs speech recognition; MiniKeys only sends shortcuts. It does not access the microphone or send chat messages. Windows 11 has not been tested. See the [English test guide](tests/README.en.md) for other validation details.
