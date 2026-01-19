using System.Collections.Generic;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Input;

namespace InputMan.StrideConn.Tests;

public sealed class FakeStrideInputSource : IStrideInputSource
{
    private readonly HashSet<Keys> _keysDown = new();
    private readonly HashSet<MouseButton> _mouseButtonsDown = new();
    private readonly Dictionary<int, GamePadState> _padStates = new();

    public Vector2 MouseDelta { get; set; }
    public float MouseWheelDelta { get; set; }

    public void SetKeyDown(Keys key, bool down)
    {
        if (down) _keysDown.Add(key);
        else _keysDown.Remove(key);
    }

    public void SetMouseButtonDown(MouseButton button, bool down)
    {
        if (down) _mouseButtonsDown.Add(button);
        else _mouseButtonsDown.Remove(button);
    }

    public void SetGamePadState(int index, GamePadState state)
    {
        _padStates[index] = state;
    }

    public bool IsKeyDown(Keys key) => _keysDown.Contains(key);

    public bool IsMouseButtonDown(MouseButton button) => _mouseButtonsDown.Contains(button);

    public bool TryGetGamePadState(int index, out GamePadState state)
        => _padStates.TryGetValue(index, out state);
}
