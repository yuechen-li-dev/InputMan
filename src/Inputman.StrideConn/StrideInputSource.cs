using Stride.Core.Mathematics;
using Stride.Input;

public sealed class StrideInputSource : IStrideInputSource
{
    private readonly InputManager _input;
    public StrideInputSource(InputManager input) => _input = input;

    public bool IsKeyDown(Keys key) => _input.IsKeyDown(key);
    public bool IsMouseButtonDown(MouseButton button) => _input.IsMouseButtonDown(button);

    public Vector2 MouseDelta => _input.MouseDelta;
    public float MouseWheelDelta => _input.MouseWheelDelta;

    public bool TryGetGamePadState(int index, out GamePadState state)
    {
        var pad = _input.GetGamePadByIndex(index);
        if (pad != null)
        {
            state = pad.State;
            return true;
        }

        // fallback if needed
        foreach (var gp in _input.GamePads)
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
