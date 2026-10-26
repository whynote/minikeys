# 迷你补键盘 · MiniKeys

**把坏掉的几个按键，变成随手可点的悬浮按钮。**

[English documentation](README.en.md) · [下载最新版本](https://github.com/whynote/minikeys/releases/latest) · [中文版本说明](CHANGELOG.md) · [English release notes](CHANGELOG.en.md)

MiniKeys 是一个轻量的 Windows 悬浮补键工具。默认只显示 **3、E、D、C**，也可以换成自己需要的按键。开会或临时打字时，把它放在屏幕一角，用鼠标补上实体键盘失灵的按键。

![迷你补键盘 v1.0.0 默认四键界面](docs/images/v1.0.0.png)

> v1.0.0 的软件界面为中文，提供完整中英文说明。英文用户可参考 [按钮对照与操作说明](README.en.md#using-the-chinese-interface)。

## 下载与开始使用

1. 打开 [Releases 下载页](https://github.com/whynote/minikeys/releases/latest)，下载便携版 ZIP 并解压（首发包为 `MiniKeys-v1.0.0-win-portable.zip`）。
2. 双击 `MiniKeys.exe`。无需安装，默认置顶。
3. 先点击要输入的聊天框、文档或其他输入位置，再点击浮窗上的按键。

程序发送真实的按键事件，字母会交给当前输入法处理：中文模式下可继续打拼音，英文模式下输入英文字母。按钮显示的是大写键名，输入大小写跟随实体键盘的 Shift / Caps Lock 状态。

适用于 Windows 10 / 11，使用 Windows 的 .NET Framework 4 系列运行环境，不依赖 Java、Python 或第三方包。**已在 Windows 10 实测中文拼音、焦点保持、移动和缩放；Windows 11 尚未实测。**

## 可以做什么

- **只保留需要的键**：自定义 1–24 个按键及顺序，每行 1–12 个。
- **悬浮置顶**：默认置顶，点击“已置顶 / 置顶”切换状态。
- **保持输入焦点**：点击补键时，原输入框继续接收按键。
- **拖动与缩放**：拖顶部空白处移动；拖窗口边缘或右下角缩放，按钮随之调整。
- **记住配置**：下次启动恢复按键、排列、位置、尺寸和置顶状态。

## 自定义按键

点击“设置”，在输入框中填写用空格分隔的按键，例如：

```text
3 E D C Space Backspace
```

“每行按键数”设为 `1` 即竖排，设为 `4` 即默认四键横排，设为 `2` 即两列。点击“保存”应用；“恢复 3 E D C”可恢复默认按键和排列。

支持以下按键，名称不区分大小写：

| 类型 | 可填写的名称 |
| --- | --- |
| 字母、数字 | `A`–`Z`、`0`–`9` |
| 功能键 | `F1`–`F12` |
| 编辑键 | `Space`、`Enter`、`Backspace`、`Tab`、`Esc`、`Delete` |
| 方向与导航 | `Left`、`Right`、`Up`、`Down`、`Home`、`End`、`PageUp`、`PageDown` |
| 英文标点 | `-`、`=`、`[`、`]`、`;`、`'`、`,`、`.`、`/`、`\`、反引号 |

标点结果跟随当前键盘布局及输入法。每个按钮发送单个按键，可配合实体键盘上的 Shift / Ctrl 使用。

## 常见操作与问题

| 需要做什么 | 操作 |
| --- | --- |
| 移动浮窗 | 拖动顶部“补键 ···”所在的空白区域 |
| 调整大小 | 拖动窗口边缘或右下角 |
| 取消或恢复置顶 | 点击“已置顶 / 置顶” |
| 找回被挡住的窗口 | 点击任务栏中的程序图标 |
| 退出 | 点击右上角 `×` |
| 恢复全部默认设置 | 退出后删除 `%LOCALAPPDATA%\MiniKeys\settings.json`，再启动 |

如果点击按键没有输入，先点击目标输入框。若目标程序以管理员身份运行，补键盘也需要以相同权限运行。

程序通过 Windows `SendInput` 发送按下与松开事件，不联网，不读取或修改剪贴板，不监听或记录输入内容。配置只保存在本机的 `%LOCALAPPDATA%\MiniKeys\settings.json`。

## 从源码编译

在项目目录运行 PowerShell：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

构建脚本使用 Windows 自带的 .NET Framework C# 编译器，生成 `MiniKeys.exe`。源码位于 [src/MiniKeys.cs](src/MiniKeys.cs)，测试说明见 [tests/README.md](tests/README.md)。

版本变化见 [CHANGELOG.md](CHANGELOG.md)。项目采用 [MIT 许可证](LICENSE)。
