# InputMan

**A powerful, flexible input management library for modern .NET game engines with first-class rebinding support.**

[![NuGet](https://img.shields.io/badge/nuget-v0.1.0-blue)]() 
[![License](https://img.shields.io/badge/license-MIT-green)](https://github.com/yuechen-li-dev/InputMan/blob/master/LICENSE.txt)

InputMan provides a modern, engine-agnostic input system with action maps, priority-based consumption, and seamless runtime rebinding. Perfect for games that need professional input handling.

## ⚠️ Pre-Release Notice

**This is v0.1.0** - the initial public release. The API is functional and tested, but may evolve before v1.0 based on community feedback. Please report issues and suggestions on GitHub!

---

## ✨ Features

- **🎮 Action Maps** - Layer map based inputs with priority-based consumption (UI blocks gameplay, etc.)
- **🔄 Runtime Rebinding** - Full-featured RebindingManager with automatic profile saving
- **🎯 Multiple Input Types** - Buttons, axes, delta axes, and 2D axes
- **🎹 Chord Bindings** - Modifier keys (Shift+W for sprint, Ctrl+S for save, etc.)
- **⚙️ Processors** - Built-in deadzone, invert, and scale processors
- **🎨 Engine-Agnostic Core** - Use with any engine (Stride adapter included)
- **💾 Pluggable Serialization** - JSON by default, others (TOML, XML, binary, etc.) easy to add
- **🎛️ Consumption Control** - Higher-priority maps can block lower ones
- **🔌 Type-safe IDs** - Prefer static readonly IDs; strings are allowed for quick prototyping

---

## 📦 Installation

### NuGet Packages

```bash
# Core library (engine-agnostic)
dotnet add package InputMan.Core

# Stride engine adapter
dotnet add package InputMan.StrideConn
```

### Package Manager Console

```
Install-Package InputMan.Core
Install-Package InputMan.StrideConn
```

---

## 🚀 Quick Start (Stride Engine)

### Step 1: Install InputMan (30 seconds)

Create `InstallInputMan.cs` in your project:

```csharp
using InputMan.Core;
using InputMan.StrideConn;
using Stride.Engine;

public class InstallInputMan : StartupScript
{
    public override void Start()
    {
        // Create profile storage
        var storage = StrideProfileStorage.CreateDefault(
            appName: "MyGame",
            defaultProfileFactory: MyGameProfile.Create);

        // Load profile (user > bundled > code default)
        var profile = storage.LoadProfile();

        // Install InputMan system
        var inputSystem = new StrideInputManSystem(
            Game.Services, 
            profile,
            new ActionMapId("Gameplay"));

        Game.GameSystems.Add(inputSystem);
    }
}
```

**Drag this script onto your Game Manager entity in the scene.**

### Step 2: Read Input in Your Scripts

```csharp
using InputMan.Core;
using Stride.Engine;

public class PlayerController : SyncScript
{
    private IInputMan _input;

    public override void Start()
    {
        _input = Game.Services.GetService<IInputMan>();
    }

    public override void Update()
    {
        // Check if jump was just pressed
        if (_input.WasPressed(new ActionId("Jump")))
        {
            Log.Info("Player jumped!");
        }
    }
}
```

**That's it!** You now have a working input system. 🎉

---

## 📚 Core Concepts

### Actions vs Axes

**Actions** are discrete events (pressed, held, released):
```csharp
var jumpAction = new ActionId("Jump");

// Was it just pressed this frame?
if (_input.WasPressed(jumpAction)) { }

// Is it currently held down?
if (_input.IsDown(jumpAction)) { }

// Was it just released this frame?
if (_input.WasReleased(jumpAction)) { }
```

**Axes** are continuous values (-1 to +1 for sticks, unbounded for mouse):
```csharp
var moveXAxis = new AxisId("MoveX");

// Get current value
float horizontal = _input.GetAxis(moveXAxis);
```

**Axis2** combines two axes into a Vector2:
```csharp
var moveAxis = new Axis2Id("Move");

// Get both X and Y at once
Vector2 movement = _input.GetAxis2(moveAxis);
```

### Action Maps

Action maps let you organize inputs into logical groups with priorities:

```csharp
var profile = new InputProfile
{
    Maps = new Dictionary<string, ActionMapDefinition>
    {
        // UI map - highest priority (100)
        ["UI"] = new ActionMapDefinition
        {
            Id = new ActionMapId("UI"),
            Priority = 100,  // Higher number = evaluated first
            CanConsume = true,  // Can block lower-priority maps
            Bindings = [ /* UI bindings */ ]
        },
        
        // Gameplay map - lower priority (10)
        ["Gameplay"] = new ActionMapDefinition
        {
            Id = new ActionMapId("Gameplay"),
            Priority = 10,
            CanConsume = false,
            Bindings = [ /* Gameplay bindings */ ]
        }
    }
};
```

**Activate maps at runtime:**
```csharp
// Show pause menu - UI blocks gameplay
_input.SetMaps(new ActionMapId("UI"));

// Resume - both active, UI has priority
_input.SetMaps(
    new ActionMapId("UI"),
    new ActionMapId("Gameplay"));
```

### Bindings

Bindings connect physical controls to actions/axes:

```csharp
using static InputMan.Core.Bind;
using static InputMan.StrideConn.StrideKeys;

var bindings = new List<Binding>
{
    // Button -> Action
    Action(K(Keys.Space), new ActionId("Jump"), ButtonEdge.Pressed),
    
    // Button -> Axis (WASD movement)
    ButtonAxis(K(Keys.W), new AxisId("MoveY"), +1.0f),
    ButtonAxis(K(Keys.S), new AxisId("MoveY"), -1.0f),
    
    // Analog Stick -> Axis
    Axis(PadLeftX(0), new AxisId("MoveX"), scale: 1.0f),
    
    // Mouse Delta -> Axis (camera look)
    DeltaAxis(MouseDeltaX, new AxisId("LookX"), scale: 1.0f),
};
```

### Chord Bindings (Modifier Keys)

Create modifier-based bindings for advanced controls:

```csharp
// Sprint with Shift+W
ActionChord(K(Keys.W), Sprint, ButtonEdge.Down, 
    name: "Sprint.Kb", 
    modifiers: K(Keys.LeftShift))

// Quick save with Ctrl+S
ActionChord(K(Keys.S), QuickSave, ButtonEdge.Pressed,
    name: "QuickSave.Kb",
    modifiers: K(Keys.LeftControl))

// Multiple modifiers: Ctrl+Shift+P
ActionChord(K(Keys.P), DebugPanel, ButtonEdge.Pressed,
    name: "Debug.Kb",
    modifiers: new[] { K(Keys.LeftControl), K(Keys.LeftShift) })
```

**How chords work:**
- ALL modifier keys must be held simultaneously with the primary key
- Release any modifier and the action deactivates (perfect for sprint)
- Works with `ButtonEdge.Down`, `Pressed`, and `Released`

---

## 🎮 Complete Example: Third-Person Controller

### Define Your Profile

```csharp
using InputMan.Core;
using InputMan.StrideConn;
using static InputMan.Core.Bind;
using static InputMan.StrideConn.StrideKeys;

public static class MyGameProfile
{
    // Define IDs
    public static readonly ActionId Jump = new("Jump");
    public static readonly ActionId Sprint = new("Sprint");
    public static readonly AxisId MoveX = new("MoveX");
    public static readonly AxisId MoveY = new("MoveY");
    public static readonly Axis2Id Move = new("Move");
    
    public static InputProfile Create()
    {
        var deadzone = new DeadzoneProcessor(0.15f);
        
        var gameplay = new ActionMapDefinition
        {
            Id = new ActionMapId("Gameplay"),
            Priority = 10,
            Bindings =
            [
                // WASD Movement
                ButtonAxis(K(Keys.W), MoveY, +1f, name: "MoveFwd.Kb"),
                ButtonAxis(K(Keys.S), MoveY, -1f, name: "MoveBack.Kb"),
                ButtonAxis(K(Keys.A), MoveX, -1f, name: "MoveLeft.Kb"),
                ButtonAxis(K(Keys.D), MoveX, +1f, name: "MoveRight.Kb"),
                
                // Sprint (Shift+W)
                ActionChord(K(Keys.W), Sprint, ButtonEdge.Down, 
                    name: "Sprint.Kb", modifiers: K(Keys.LeftShift)),
                
                // Gamepad left stick (with deadzone)
                Axis(PadLeftX(0), MoveX, scale: 1f, processors: deadzone),
                Axis(PadLeftY(0), MoveY, scale: 1f, processors: deadzone),
                
                // Jump
                Action(K(Keys.Space), Jump, ButtonEdge.Pressed, name: "Jump.Kb"),
                Action(PadBtn(0, GamePadButton.A), Jump, ButtonEdge.Pressed),
            ]
        };
        
        return new InputProfile
        {
            Maps = new() { ["Gameplay"] = gameplay },
            Axis2 = new()
            {
                ["Move"] = new Axis2Definition 
                { 
                    Id = Move, 
                    X = MoveX, 
                    Y = MoveY 
                }
            }
        };
    }
}
```

### Use in Your Controller

```csharp
public class PlayerController : SyncScript
{
    private IInputMan _input;
    
    public float MoveSpeed = 5f;
    public float SprintSpeed = 10f;
    public float JumpForce = 10f;

    public override void Start()
    {
        _input = Game.Services.GetService<IInputMan>();
    }

    public override void Update()
    {
        float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        
        // Sprint when Shift+W is held
        var speed = _input.IsDown(MyGameProfile.Sprint) ? SprintSpeed : MoveSpeed;
        
        // Get movement input
        var moveInput = _input.GetAxis2(MyGameProfile.Move);
        var movement = new Vector3(moveInput.X, 0, moveInput.Y) * speed * dt;
        
        Entity.Transform.Position += movement;
        
        // Jump
        if (_input.WasPressed(MyGameProfile.Jump))
        {
            Jump();
        }
    }
}
```

---

## 🔄 Runtime Rebinding

InputMan includes a powerful `RebindingManager` that handles all rebinding logic for you. It's clean, reusable, and works with any UI system.

### Using RebindingManager (Recommended)

The easiest way to add rebinding to your game:

```csharp
using InputMan.Core;
using InputMan.StrideConn;
using Stride.Engine;

public class SettingsMenu : SyncScript
{
    private IInputMan _inputMan;
    private RebindingManager _rebindManager;

    public override void Start()
    {
        _inputMan = Game.Services.GetService<IInputMan>();
        
        // Create storage for saving rebinds
        var storage = StrideProfileStorage.CreateDefault(
            appName: "MyGame",
            defaultProfileFactory: MyGameProfile.Create);
        
        // Create rebinding manager
        _rebindManager = new RebindingManager(_inputMan, storage);
        
        // Subscribe to status updates for UI feedback
        _rebindManager.OnStatusChanged += message => 
        {
            UpdateUI(message); // Show "Press a key..." etc.
        };
        
        _rebindManager.OnCompleted += success =>
        {
            if (success)
                ShowMessage("Binding saved!");
            else
                ShowMessage("Binding failed");
        };
    }

    public void OnRebindJumpButtonClicked()
    {
        // Build candidate buttons (what keys are allowed)
        var candidates = StrideCandidateButtons.KeyboardAndGamepad();
        
        // Start rebinding
        _rebindManager.StartRebind(
            bindingName: "Jump.Kb",
            map: new ActionMapId("Gameplay"),
            candidateButtons: candidates,
            forbiddenControls: new HashSet<ControlKey>
            {
                new(DeviceKind.Keyboard, 0, (int)Keys.Escape) // Reserve Escape
            },
            disallowConflicts: true);
    }
    
    public void OnCancelButtonClicked()
    {
        _rebindManager.CancelRebind();
    }
}
```

**That's it!** RebindingManager handles:
- ✅ Session management
- ✅ Progress tracking
- ✅ Profile saving
- ✅ Event notifications
- ✅ Error handling

### Candidate Buttons (Stride)

`StrideCandidateButtons` provides helpers for building button lists:

```csharp
// Keyboard + Gamepad (most common for gameplay)
var candidates = StrideCandidateButtons.KeyboardAndGamepad();

// Keyboard + Mouse (for aim/look controls)
var candidates = StrideCandidateButtons.KeyboardAndMouse();

// All devices
var candidates = StrideCandidateButtons.AllDevices();

// Just keyboard
var candidates = StrideCandidateButtons.AllKeyboardKeys();

// Just mouse
var candidates = StrideCandidateButtons.AllMouseButtons();

// Smart: auto-detect from RebindRequest
var request = RebindPresets.GameplayButton(map, "Jump.Kb");
var candidates = StrideCandidateButtons.ForRequest(request);
```

---

## ⚙️ Processors

Transform input values with processors:

```csharp
var binding = new Binding
{
    Trigger = new BindingTrigger
    {
        Control = PadLeftX(0),
        Type = TriggerType.Axis
    },
    Output = new AxisOutput(new AxisId("MoveX"), Scale: 1f),
    
    // Apply processors in order
    Processors = new List<IProcessor>
    {
        new DeadzoneProcessor(0.15f),  // Ignore small stick drift
        new ScaleProcessor(2.0f),      // Double sensitivity
        new InvertProcessor()          // Flip direction
    }
};
```

**Built-in Processors:**
- `DeadzoneProcessor(float deadzone)` - Ignore input below threshold, remap above
- `ScaleProcessor(float scale)` - Multiply input value
- `InvertProcessor()` - Negate input value

**Custom Processors:**
```csharp
public class ClampProcessor : IProcessor
{
    private readonly float _min, _max;
    
    public ClampProcessor(float min, float max)
    {
        _min = min;
        _max = max;
    }
    
    public float Process(float value)
    {
        return Math.Clamp(value, _min, _max);
    }
}
```

---

## 🎯 Advanced: Consumption

Control how maps interact with priority and consumption:

```csharp
var uiMap = new ActionMapDefinition
{
    Id = new ActionMapId("UI"),
    Priority = 100,
    CanConsume = true,  // This map can consume inputs
    Bindings =
    [
        new Binding
        {
            Trigger = new BindingTrigger 
            { 
                Control = K(Keys.Escape), 
                Type = TriggerType.Button 
            },
            Output = new ActionOutput(new ActionId("CloseMenu")),
            Consume = ConsumeMode.All  // Consume both control AND action
        }
    ]
};

var gameplayMap = new ActionMapDefinition
{
    Id = new ActionMapId("Gameplay"),
    Priority = 10,
    CanConsume = false,  // Lower priority, can't consume
    Bindings = [ /* ... */ ]
};
```

**Consume Modes:**
- `ConsumeMode.None` - Don't consume (multiple maps can read same input)
- `ConsumeMode.ControlOnly` - Consume the physical control (Escape key blocked)
- `ConsumeMode.ActionOnly` - Consume the action (CloseMenu fires only once)
- `ConsumeMode.All` - Consume both control and action

**Example: Pause Menu**
```csharp
// When pause menu opens
_input.SetMaps(
    new ActionMapId("UI"),         // Priority 100, will consume Escape
    new ActionMapId("Gameplay"));  // Priority 10, won't see Escape

// When menu closes
_input.SetMaps(new ActionMapId("Gameplay"));  // Full control restored
```

---

## 🔧 Troubleshooting

### "IInputMan not found"

Make sure `InstallInputMan` runs before other scripts:
```csharp
// In InstallInputMan.cs
public class InstallInputMan : StartupScript  // ← StartupScript runs early
{
    // ...
}
```

### Input not responding

Check that your map is activated:
```csharp
public override void Start()
{
    _input = Game.Services.GetService<IInputMan>();
    
    // Make sure to activate your map!
    _input.SetMaps(new ActionMapId("Gameplay"));
}
```

### Rebinding doesn't work

Use `RebindingManager` and provide candidate buttons:
```csharp
var candidates = StrideCandidateButtons.KeyboardAndGamepad();
_rebindManager.StartRebind("Jump.Kb", map, candidates);
```

### Profile changes not saving

Make sure you're using `RebindingManager` with `IProfileStorage`:
```csharp
// RebindingManager auto-saves on successful rebind
var storage = StrideProfileStorage.CreateDefault("MyGame", MyGameProfile.Create);
var rebindManager = new RebindingManager(_inputMan, storage);
```

---

## 📖 API Reference

### IInputMan Interface

```csharp
// State queries
bool IsDown(ActionId action);
bool WasPressed(ActionId action);
bool WasReleased(ActionId action);
float GetAxis(AxisId axis);
Vector2 GetAxis2(Axis2Id axis2);

// Map management
void PushMap(ActionMapId map, int? priorityOverride = null);
void PopMap(ActionMapId map);
void SetMaps(params ActionMapId[] maps);

// Rebinding
IRebindSession StartRebind(RebindRequest request);

// Profile management
InputProfile ExportProfile();
void ImportProfile(InputProfile profile);

// Events
event Action<ActionEvent> OnAction;
event Action<AxisEvent> OnAxis;

// Frame info
long FrameIndex { get; }
float DeltaTimeSeconds { get; }
```

### RebindingManager (Core)

```csharp
// Constructor
RebindingManager(IInputMan inputMan, IProfileStorage storage);

// Properties
bool IsRebinding { get; }
string StatusMessage { get; }

// Methods
void StartRebind(string bindingName, ActionMapId map, 
    IReadOnlyList<ControlKey> candidateButtons,
    IReadOnlySet<ControlKey>? forbiddenControls = null,
    bool disallowConflicts = true);
void CancelRebind();

// Events
event Action<string> OnStatusChanged;
event Action<bool> OnCompleted;
```

### StrideCandidateButtons (StrideConn)

```csharp
List<ControlKey> AllKeyboardKeys();
List<ControlKey> AllMouseButtons();
List<ControlKey> KeyboardAndGamepad();
List<ControlKey> KeyboardAndMouse();
List<ControlKey> AllDevices();
List<ControlKey> ForRequest(RebindRequest request);
```

### Bind Helpers

```csharp
// Button -> Action
Binding Action(ControlKey key, ActionId action, ButtonEdge edge = Pressed)

// Button -> Action with Modifiers (Chord)
Binding ActionChord(ControlKey key, ActionId action, ButtonEdge edge = Down,
    ConsumeMode consume = None, string? name = null, params ControlKey[] modifiers)

// Button -> Axis (WASD-style)
Binding ButtonAxis(ControlKey key, AxisId axis, float scale)

// Analog -> Axis (sticks, triggers)
Binding Axis(ControlKey key, AxisId axis, float scale = 1f, 
    float threshold = 0f, ConsumeMode consume = None, string? name = null,
    params IProcessor[] processors)

// Delta -> Axis (mouse movement)
Binding DeltaAxis(ControlKey key, AxisId axis, float scale = 1f)
```

---

## 🎓 Learning Resources

### Sample Projects

Check out the **ThirdPersonPlatformer** demo in the repository for a complete working example with:
- ✅ WASD + Gamepad movement
- ✅ Mouse + Stick camera control
- ✅ Sprint with Shift+W (chord binding)
- ✅ Runtime rebinding with RebindingManager
- ✅ Pause menu with map switching
- ✅ Profile saving/loading with StrideProfileStorage

---

## 📄 License

MIT License - see LICENSE file for details

---

## 🤝 Contributing

This is v0.1.0 - feedback and contributions are welcome! Please:
- Report bugs and issues on GitHub
- Suggest features and improvements
- Share your use cases and experiences

**Made with ❤️ for game developers**
