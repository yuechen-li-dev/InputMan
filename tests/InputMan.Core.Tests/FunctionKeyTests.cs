using Xunit;

namespace InputMan.Core.Tests;

public sealed class FunctionKeyTests
{
    [Theory]
    [InlineData(KeyboardKey.F1, 58)]
    [InlineData(KeyboardKey.F2, 59)]
    [InlineData(KeyboardKey.F3, 60)]
    [InlineData(KeyboardKey.F4, 61)]
    [InlineData(KeyboardKey.F5, 62)]
    [InlineData(KeyboardKey.F6, 63)]
    [InlineData(KeyboardKey.F7, 64)]
    [InlineData(KeyboardKey.F8, 65)]
    [InlineData(KeyboardKey.F9, 66)]
    [InlineData(KeyboardKey.F10, 67)]
    [InlineData(KeyboardKey.F11, 68)]
    [InlineData(KeyboardKey.F12, 69)]
    public void FunctionKeysHaveStablePortableIdentities(KeyboardKey key, int code)
    {
        ControlKey control = Controls.Key(key);
        Assert.Equal(DeviceKind.Keyboard, control.Device);
        Assert.Equal(code, control.Code);
        Assert.Equal(key, Enum.Parse<KeyboardKey>(key.ToString()));
        Assert.Equal(1, Enum.GetValues<KeyboardKey>().Count(value => (int)value == code));
    }
}
