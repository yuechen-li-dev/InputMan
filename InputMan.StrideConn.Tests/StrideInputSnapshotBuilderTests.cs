using System;
using InputMan.Core;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Input;
using Xunit;

namespace InputMan.StrideConn.Tests;

public sealed class StrideInputSnapshotBuilderTests
{
    [Fact]
    public void Keyboard_W_Down_Is_Stored_As_ButtonTrue()
    {
        var input = new FakeStrideInputSource();
        input.SetKeyDown(Keys.W, true);

        var wKey = new ControlKey(DeviceKind.Keyboard, DeviceIndex: 0, Code: (int)Keys.W);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: new[] { wKey },
            watchedAxes: Array.Empty<ControlKey>());

        Assert.True(snap.TryGetButton(wKey, out var down));
        Assert.True(down);
    }

    [Fact]
    public void MouseDelta_Maps_To_AxisValues()
    {
        var input = new FakeStrideInputSource
        {
            MouseDelta = new Vector2(12f, -3f)
        };

        var dx = new ControlKey(DeviceKind.Mouse, DeviceIndex: 0, Code: StrideControlCodes.MouseDeltaX);
        var dy = new ControlKey(DeviceKind.Mouse, DeviceIndex: 0, Code: StrideControlCodes.MouseDeltaY);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: Array.Empty<ControlKey>(),
            watchedAxes: new[] { dx, dy });

        Assert.True(snap.TryGetAxis(dx, out var x));
        Assert.True(snap.TryGetAxis(dy, out var y));
        Assert.Equal(12f, x);
        Assert.Equal(-3f, y);
    }

    [Fact]
    public void MouseWheelDelta_Maps_To_AxisValue()
    {
        var input = new FakeStrideInputSource
        {
            MouseWheelDelta = 1.5f
        };

        var wheel = new ControlKey(DeviceKind.Mouse, DeviceIndex: 0, Code: StrideControlCodes.MouseWheelDelta);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: Array.Empty<ControlKey>(),
            watchedAxes: new[] { wheel });

        Assert.True(snap.TryGetAxis(wheel, out var v));
        Assert.Equal(1.5f, v);
    }

    [Fact]
    public void GamepadRightStick_Maps_To_AxisValues()
    {
        var input = new FakeStrideInputSource();

        input.SetGamePadState(0, new GamePadState
        {
            RightThumb = new Vector2(0.25f, -0.5f)
        });

        var rx = new ControlKey(DeviceKind.Gamepad, DeviceIndex: 0, Code: StrideControlCodes.GamepadRightX);
        var ry = new ControlKey(DeviceKind.Gamepad, DeviceIndex: 0, Code: StrideControlCodes.GamepadRightY);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: Array.Empty<ControlKey>(),
            watchedAxes: new[] { rx, ry });

        Assert.True(snap.TryGetAxis(rx, out var x));
        Assert.True(snap.TryGetAxis(ry, out var y));
        Assert.Equal(0.25f, x, 5);
        Assert.Equal(-0.5f, y, 5);
    }

    [Fact]
    public void GamepadButtonA_Down_Is_Stored_As_ButtonTrue()
    {
        var input = new FakeStrideInputSource();

        input.SetGamePadState(0, new GamePadState
        {
            Buttons = GamePadButton.A
        });

        // IMPORTANT: gamepad button codes are the int value of Stride.Input.GamePadButton
        var aKey = new ControlKey(DeviceKind.Gamepad, DeviceIndex: 0, Code: (int)GamePadButton.A);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: new[] { aKey },
            watchedAxes: Array.Empty<ControlKey>());

        Assert.True(snap.TryGetButton(aKey, out var down));
        Assert.True(down);
    }

    [Fact]
    public void Snapshot_Is_Sparse_Zero_Axes_Are_Omitted()
    {
        var input = new FakeStrideInputSource
        {
            MouseDelta = Vector2.Zero
        };

        var dx = new ControlKey(DeviceKind.Mouse, DeviceIndex: 0, Code: StrideControlCodes.MouseDeltaX);

        var snap = StrideInputSnapshotBuilder.Build(
            input,
            watchedButtons: Array.Empty<ControlKey>(),
            watchedAxes: new[] { dx });

        // Because the builder is sparse, a zero axis should not be stored at all.
        Assert.False(snap.TryGetAxis(dx, out _));
    }
}
