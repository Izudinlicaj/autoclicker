# AutoClicker

Ultimate portable autoclicker and key spammer for Windows.

[VirusTotal](https://www.virustotal.com/gui/file/f44abf95645e141df9661ba18632d35a3d0602de77d9b9e8d5b76b2eac935859)
[Tria.ge](https://tria.ge/260212-a7wjsaew8a/behavioral1)

## Features

- **Hardware-level input** via SendInput — works on games, browsers, Flash, everything
- **Mouse clicking** — Left, Right, Middle, X1 (Back), X2 (Forward)
- **Single, Double, Triple** click modes
- **Keyboard spamming** — any key or combination of keys in sequence
- **Key hold mode** — hold keys down continuously
- **Scroll wheel spamming** — up/down with custom delta and speed
- **Sub-millisecond precision** — microsecond timing via busy-wait + NtSetTimerResolution
- **Speed presets** — Slow, Normal, Fast, Ultra, MAXIMUM, Custom
- **Click at cursor or fixed coordinates** with fullscreen crosshair picker
- **Burst mode** — stop after N actions
- **Anti-detection jitter** — randomized intervals to look human
- **Global hotkey** (F1-F12) — works in fullscreen games and when minimized
- **Profile save/load/delete** — portable .acp files stored next to the exe
- **System tray** — minimize to tray, balloon notifications
- **Dark mode UI** — full dark theme
- **Live actions-per-second counter**
- **100% portable** — single .exe, zero installation, zero dependencies
- **~50KB file size**

## Download

Grab `AutoClicker.exe` from [Releases](../../releases).

No installation needed. Just run it.

## Build From Source

You don't need Visual Studio. The compiler is already on your Windows PC:

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:AutoClicker.exe AutoClickerPro.cs


With custom icon:
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:icon.ico /out:AutoClickerPro.exe AutoClickerPro.cs

## Usage

1. Run `AutoClicker.exe`
2. Configure speed, button, position in the **Mouse** tab
3. Configure key spamming in the **Keyboard** tab
4. Adjust burst mode, jitter, hotkey in the **Advanced** tab
5. Press **F6** (or your chosen hotkey) to start/stop
6. Works globally — even in fullscreen games

## Keyboard Keys Supported

A-Z, 0-9, F1-F12, SPACE, ENTER, TAB, ESC, SHIFT, CTRL, ALT,
UP, DOWN, LEFT, RIGHT, BACKSPACE, DELETE, INSERT, HOME, END,
PAGEUP, PAGEDOWN, NUM0-NUM9, CAPSLOCK, NUMLOCK, and more. 
(Might have missed some but most are supported really.)
Separate multiple keys with commas: `A, SPACE, ENTER`

## Requirements

- Windows 7 / 8 / 10 / 11
- .NET Framework 4.x (preinstalled on all modern Windows)

## License

MIT License — free for personal and commercial use.
