using InputMan.Core;
using InputMan.MonoGameConn;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using static InputMan.Core.Bind;
using static InputMan.MonoGameConn.MonoGameKeys;

namespace Platformer2D;

/// <summary>
/// Input profile for Platformer2D game.
/// Supports keyboard and gamepad controls.
/// 
/// ORIGINAL CONTROLS:
/// - Movement: A/D or Arrow Left/Right (NOT WASD!)
/// - Jump: Space, Up, or W
/// </summary>
public static class Platformer2DProfile
{
    // Action IDs
    public static readonly ActionId Jump = new("Jump");
    public static readonly ActionId RebindJump = new("RebindJump");

    // Axis IDs
    public static readonly AxisId MoveX = new("MoveX");

    public static InputProfile Create()
    {
        var gameplay = new ActionMapDefinition
        {
            Id = new ActionMapId("Gameplay"),
            Priority = 10,
            CanConsume = false,
            Bindings =
            [
                // ==================== Keyboard Movement ====================
                // Left/Right with A/D (original controls)
                ButtonAxis(K(Keys.A), MoveX, -1f, name: "MoveLeft.Kb.A"),
                ButtonAxis(K(Keys.D), MoveX, +1f, name: "MoveRight.Kb.D"),
                
                // Left/Right with Arrow keys
                ButtonAxis(K(Keys.Left), MoveX, -1f, name: "MoveLeft.Kb.Arrow"),
                ButtonAxis(K(Keys.Right), MoveX, +1f, name: "MoveRight.Kb.Arrow"),
                
                // ==================== Keyboard Jump ====================
                // Jump with Space or Up arrow (removed W to avoid conflicts for now)
                Action(K(Keys.Space), Jump, ButtonEdge.Down, name: "Jump.Kb.Space"),
                Action(K(Keys.Up), Jump, ButtonEdge.Down, name: "Jump.Kb.Up"),
                
                // ==================== Gamepad ====================
                // Movement (left stick with deadzone)
                Axis(PadLeftX(PlayerIndex.One), MoveX, scale: 1f,
                    name: "Move.Pad.LeftStick",
                    processors: new DeadzoneProcessor(0.15f)),
                
                // Jump (A button) - ButtonEdge.Down for variable jump height
                Action(PadBtn(PlayerIndex.One, Buttons.A), Jump, ButtonEdge.Down,
                    name: "Jump.Pad.A"),
                
                // ==================== Rebinding Hotkey ====================
                // Press J to start rebinding Jump
                Action(K(Keys.J), RebindJump, ButtonEdge.Pressed, name: "RebindJump.Kb"),
            ]
        };

        return new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Gameplay"] = gameplay
            }
        };
    }
}