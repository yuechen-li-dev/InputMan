using Stride.Input;
using Stride.Core.Mathematics;

namespace InputMan.StrideConn
{
    public interface IStrideInputSource
    {
        bool IsKeyDown(Keys key);
        bool IsMouseButtonDown(MouseButton button);

        Vector2 MouseDelta { get; }
        float MouseWheelDelta { get; }

        bool TryGetGamePadState(int index, out GamePadState state);
    }
}