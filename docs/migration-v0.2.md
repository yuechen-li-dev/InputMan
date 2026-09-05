# Migrating from InputMan 0.1 to 0.2

InputMan 0.2 preserves the useful semantics but changes the preferred surface.

- Replace engine enum helpers in new profiles with `Controls.Key`, `Controls.Mouse`, and `Controls.Gamepad` using Core-owned enums.
- Prefer `Input.Profile`, `Input.Map`, `Input.Wasd`, and `Input.GamepadLeftStick` over manually assembled dictionaries and DTOs.
- Read the immutable `InputManEngine.CurrentFrame` at the controller boundary. Existing `IInputMan` polling methods remain available.
- Save new user profiles with `InputMan.Toml`. JSON remains an optional one-step import: load with `InputProfileJson.Load`, then save with `InputProfileToml.Save`.
- Replace boolean conflict policy at new call sites with `RebindConflictPolicy`. The legacy scope booleans remain for source compatibility.
- Call `ResetOnFocusLoss` from adapters. Do not manufacture key-up callback sequences.

Stride and MonoGame adapters remain buildable compatibility packages. Their historical numeric control identities are supported for existing profiles, but portable Core control names are preferred for new profiles.
