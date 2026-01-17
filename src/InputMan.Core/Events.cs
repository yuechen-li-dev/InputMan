namespace InputMan.Core;

public enum ActionPhase : byte
{
    Pressed = 1,
    Released = 2,
}

public readonly record struct ActionEvent(ActionId Action, ActionPhase Phase, long FrameIndex, float TimeSeconds);

public readonly record struct AxisEvent(AxisId Axis, float Value, long FrameIndex, float TimeSeconds);
