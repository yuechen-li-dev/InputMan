# Changelog

All notable changes to InputMan will be documented in this file.

## [Unreleased]

### Added
- `KeyboardKey` punctuation and navigation keys (`Comma`, `Period`, `Semicolon`, `Apostrophe`, `Minus`, `Equal`, `Backslash`, `Slash`, brackets, `Grave`, `Backspace`, `Tab`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, `Number9`, `Number0`, `LeftMeta`, `RightMeta`), using USB HID usage codes like the existing members.
- `InputOptions.ExclusiveModifiers`: application-style exact chord matching. When set, a button binding fires only while exactly its modifiers (from that set) are held, and only on its own control's press edge, so `Ctrl+K` no longer also fires `K` and releasing `Ctrl` while holding `K` fires nothing. Empty by default, so existing profiles keep permissive chords.

### Changed
- With exclusive modifiers, rebinding conflict checks compare control and modifier set together (`K` and `Ctrl+K` do not conflict), and `ReplaceExisting` swaps the whole shortcut.

## [0.1.1] - 2026-02-19

### Fixed
- Quick Start section in README now includes complete profile creation step
- Added missing `MyGameProfile.cs` example with Jump bindings

### Changed
- Improved Quick Start flow with clearer 3-step process

## [0.1.0] - 2026-02-19

### Added
- Initial public release
- Action maps with priority-based consumption
- Runtime rebinding with RebindingManager
- Chord/modifier key bindings
- Input processors (Deadzone, Scale, Invert)
- Stride engine adapter (StrideConn)
- JSON profile serialization
- Complete test suite (Rebinding, Processors, Chords)
