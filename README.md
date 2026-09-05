# InputMan

InputMan is an engine-agnostic .NET input mapping library. It turns portable keyboard, mouse, and gamepad controls into typed logical actions and axes, with deterministic frame snapshots, priority/consumption, chords, processors, and runtime rebinding.

InputMan does not own gameplay commands. Engine adapters feed physical snapshots; application controllers lower logical input into semantic intents.

## Packages

- `InputMan.Core` — engine-free controls, profiles, maps, processing, immutable `InputFrame`, and rebinding.
- `InputMan.Toml` — preferred human-editable format and layered user/bundled/code-default storage.
- `InputMan.MonoGameConn` — optional legacy MonoGame adapter.
- `InputMan.StrideConn` — optional legacy Stride adapter; retained, but no longer the primary documentation path.
- `InputMan.Aurelian` lives in the Aurelian integration repository because it references Aurelian host/composition contracts.

## Modern authoring

```csharp
public static readonly ActionMapId Gameplay = new("Gameplay");
public static readonly ActionId Interact = new("Interact");
public static readonly AxisId MoveX = new("MoveX");
public static readonly AxisId MoveY = new("MoveY");
public static readonly Axis2Id Move = new("Move");

InputProfile profile = Input.Profile(
    [
        Input.Map(
            Gameplay,
            priority: 10,
            bindings:
            [
                .. Input.Wasd(MoveX, MoveY),
                .. Input.GamepadLeftStick(MoveX, MoveY, deadzone: 0.15f),
                Bind.Action(
                    Controls.Key(KeyboardKey.E),
                    Interact,
                    name: "Interact.Keyboard"),
                Bind.Action(
                    Controls.Gamepad(GamepadButton.South),
                    Interact,
                    name: "Interact.Gamepad"),
            ],
            canConsume: false),
    ],
    [Input.Axis2(Move, MoveX, MoveY)]);
```

Game code reads only logical IDs:

```csharp
InputFrame frame = engine.CurrentFrame;
Vector2 movement = frame.GetAxis2(Move);
if (frame.WasPressed(Interact))
{
    // Lower to an application-owned semantic intent.
}
```

## Deterministic frame law

Adapters submit one current physical `InputSnapshot` per tick. Core derives `Pressed`, `Held`, and `Released` from the previous and current snapshots; callback ordering is never semantic. Multiple bindings for one action combine with logical OR. Axis contributions sum in deterministic map/binding order; ordinary axes clamp to `[-1, 1]`, while any axis driven by a delta binding remains unclamped. `Axis2` reads its declared X/Y logical axes.

On focus loss, call `ResetOnFocusLoss()`. It clears physical history, releases held logical actions, zeros axes, and cancels active rebinding. A reconnecting device starts from clean adapter state.

## Maps and UI capture

Higher priority maps evaluate first. A consuming binding can block its physical control, logical action, or both from all lower maps. Application policy activates contexts:

```csharp
input.SetMaps(Ui, Gameplay); // UI bindings consume shared controls.
input.SetMaps(Gameplay);     // gameplay resumes.
```

Machina remains the focus/capture authority. The Aurelian adapter translates that state into map activation rather than duplicating focus rules in game code.

## TOML

TOML is the preferred persistence format. Runtime authority remains the validated `InputProfile`.

```toml
formatVersion = 1

[options]
axisEpsilon = 0.0001

[[maps]]
id = "Gameplay"
priority = 10
canConsume = false

[[maps.bindings]]
name = "Interact.Keyboard"
trigger = "Button"
control = "Keyboard.0.E"
edge = "Pressed"
output = "Action"
action = "Interact"
consume = "None"
```

`LayeredTomlProfileStorage` resolves a complete profile in this order: user TOML, bundled TOML, typed code default. Writes are canonical and atomic. `formatVersion = 1` is required; unsupported versions fail deterministically.

The JSON serializer remains available as an optional legacy import/serializer for v0.1 profiles. It is not the default. See [migration-v0.2.md](docs/migration-v0.2.md).

## Rebinding

`RebindingManager` retains begin/progress/cancel/complete/persist behavior. `RebindRequest` accepts adapter-provided button and axis candidates, forbidden controls, chord modifiers, and explicit `RebindConflictPolicy.Allow`, `Reject`, or `ReplaceExisting`. A rebind candidate frame is consumed before normal maps evaluate.

`BindingPrompts.ForAction` exposes stable portable labels such as `Keyboard.0.E` and `Gamepad.0.South`; graphical glyph selection remains a UI concern.

## Replay and haptics

Logical frames are inspectable for tests/debugging. Gameplay replay begins after the application lowers them to semantic intents. Raw callbacks and `InputFrame` are not gameplay replay authority.

Haptics are intentionally outside InputMan Core: rumble is device output, not physical-to-logical input mapping. A small Aurelian device-output contract can be added when a real gameplay consumer requires it.
