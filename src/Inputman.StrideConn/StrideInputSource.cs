using Stride.Core.Mathematics;
using Stride.Input;

public sealed class StrideInputSource(InputManager input) : IStrideInputSource
{
    public bool IsKeyDown(Keys key) => input.IsKeyDown(key);
    public bool IsMouseButtonDown(MouseButton button) => input.IsMouseButtonDown(button);

    public Vector2 MouseDelta => input.MouseDelta;
    public float MouseWheelDelta => input.MouseWheelDelta;

    public bool TryGetGamePadState(int index, out GamePadState state)
    {
        var pad = input.GetGamePadByIndex(index);
        if (pad != null)
        {
            state = pad.State;
            return true;
        }

        // fallback if needed
        foreach (var gp in input.GamePads)
        {
            if (gp.Index == index)
            {
                state = gp.State;
                return true;
            }
        }

        state = default;
        return false;
    }
}
