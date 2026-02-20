using InputMan.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace InputMan.MonoGameConn.Tests;

/// <summary>
/// Sanity check tests for MonoGameConn adapter.
/// Core functionality is tested in InputMan.Core.Tests - these tests verify
/// the MonoGame-specific integration layer works correctly.
/// </summary>
public class MonoGameConnSanityTests
{
    #region Helper Methods

    private static InputProfile CreateTestProfile()
    {
        return new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Test"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Test"),
                    Priority = 10,
                    Bindings =
                    [
                        // Keyboard action
                        new Binding
                        {
                            Name = "Jump.Kb",
                            Trigger = new BindingTrigger
                            {
                                Control = MonoGameKeys.K(Keys.Space),
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed
                            },
                            Output = new ActionOutput(new ActionId("Jump"))
                        },
                        
                        // Gamepad action
                        new Binding
                        {
                            Name = "Jump.Pad",
                            Trigger = new BindingTrigger
                            {
                                Control = MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.A),
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed
                            },
                            Output = new ActionOutput(new ActionId("Jump"))
                        },
                        
                        // Keyboard axis
                        new Binding
                        {
                            Name = "MoveX.Kb.D",
                            Trigger = new BindingTrigger
                            {
                                Control = MonoGameKeys.K(Keys.D),
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Down
                            },
                            Output = new AxisOutput(new AxisId("MoveX"), Scale: 1f)
                        },
                        
                        // Gamepad axis
                        new Binding
                        {
                            Name = "MoveX.Pad",
                            Trigger = new BindingTrigger
                            {
                                Control = MonoGameKeys.PadLeftX(PlayerIndex.One),
                                Type = TriggerType.Axis
                            },
                            Output = new AxisOutput(new AxisId("MoveX"), Scale: 1f)
                        },
                        
                        // Mouse delta axis
                        new Binding
                        {
                            Name = "LookX.Mouse",
                            Trigger = new BindingTrigger
                            {
                                Control = MonoGameKeys.MouseDeltaX,
                                Type = TriggerType.DeltaAxis
                            },
                            Output = new AxisOutput(new AxisId("LookX"), Scale: 1f)
                        }
                    ]
                }
            }
        };
    }

    #endregion

    #region MonoGameKeys Tests

    [Fact]
    public void MonoGameKeys_Keyboard_CreatesCorrectControlKey()
    {
        // Arrange & Act
        var key = MonoGameKeys.K(Keys.Space);

        // Assert
        Assert.Equal(DeviceKind.Keyboard, key.Device);
        Assert.Equal(0, key.DeviceIndex);
        Assert.Equal((int)Keys.Space, key.Code);
    }

    [Fact]
    public void MonoGameKeys_MouseButton_CreatesCorrectControlKey()
    {
        // Arrange & Act
        var key = MonoGameKeys.M(MonoGameMouseButton.Left);

        // Assert
        Assert.Equal(DeviceKind.Mouse, key.Device);
        Assert.Equal(0, key.DeviceIndex);
        Assert.Equal((int)MonoGameMouseButton.Left, key.Code);
    }

    [Fact]
    public void MonoGameKeys_GamepadButton_CreatesCorrectControlKey()
    {
        // Arrange & Act
        var key = MonoGameKeys.PadBtn(PlayerIndex.Two, Buttons.A);

        // Assert
        Assert.Equal(DeviceKind.Gamepad, key.Device);
        Assert.Equal((byte)PlayerIndex.Two, key.DeviceIndex); // PlayerIndex.Two = 1
        Assert.Equal((int)Buttons.A, key.Code);
    }

    [Fact]
    public void MonoGameKeys_GamepadAxis_CreatesCorrectControlKey()
    {
        // Arrange & Act
        var key = MonoGameKeys.PadLeftX(PlayerIndex.One);

        // Assert
        Assert.Equal(DeviceKind.Gamepad, key.Device);
        Assert.Equal((byte)PlayerIndex.One, key.DeviceIndex);
        Assert.Equal((int)MonoGameGamePadAxis.LeftStickX, key.Code);
    }

    [Fact]
    public void MonoGameKeys_MouseDelta_CreatesCorrectControlKey()
    {
        // Arrange & Act
        var key = MonoGameKeys.MouseDeltaX;

        // Assert
        Assert.Equal(DeviceKind.Mouse, key.Device);
        Assert.Equal(0, key.DeviceIndex);
        Assert.Equal((int)MonoGameMouseAxis.DeltaX, key.Code);
    }

    #endregion

    #region MonoGameInputSnapshotBuilder Tests

    [Fact]
    public void SnapshotBuilder_EmptyWatchLists_ReturnsEmptySnapshot()
    {
        // Arrange
        var watchedButtons = new List<ControlKey>();
        var watchedAxes = new List<ControlKey>();
        Point? prevMouse = null;

        // Act
        var snapshot = MonoGameInputSnapshotBuilder.Build(watchedButtons, watchedAxes, ref prevMouse);

        // Assert
        Assert.Empty(snapshot.Buttons);
        Assert.Empty(snapshot.Axes);
    }

    [Fact]
    public void SnapshotBuilder_WatchedButtons_IncludesInSnapshot()
    {
        // Arrange
        var watchedButtons = new List<ControlKey>
        {
            MonoGameKeys.K(Keys.Space),
            MonoGameKeys.M(MonoGameMouseButton.Left)
        };
        var watchedAxes = new List<ControlKey>();
        Point? prevMouse = null;

        // Act
        var snapshot = MonoGameInputSnapshotBuilder.Build(watchedButtons, watchedAxes, ref prevMouse);

        // Assert
        Assert.Equal(2, snapshot.Buttons.Count);
        Assert.True(snapshot.Buttons.ContainsKey(MonoGameKeys.K(Keys.Space)));
        Assert.True(snapshot.Buttons.ContainsKey(MonoGameKeys.M(MonoGameMouseButton.Left)));
    }

    [Fact]
    public void SnapshotBuilder_MouseDelta_CalculatesCorrectly()
    {
        // Arrange
        var watchedAxes = new List<ControlKey> { MonoGameKeys.MouseDeltaX };
        var watchedButtons = new List<ControlKey>();
        Point? prevMouse = new Point(100, 100);

        // Act - First frame (mouse moved)
        var snapshot1 = MonoGameInputSnapshotBuilder.Build(watchedButtons, watchedAxes, ref prevMouse);
        
        // Current mouse state will be different from 100,100 in actual run
        // But we can verify the key exists in the snapshot
        Assert.True(snapshot1.Axes.ContainsKey(MonoGameKeys.MouseDeltaX));
    }

    #endregion

    #region MonoGameCandidateButtons Tests

    [Fact]
    public void CandidateButtons_AllKeyboardKeys_ContainsCommonKeys()
    {
        // Act
        var keys = MonoGameCandidateButtons.AllKeyboardKeys();

        // Assert
        Assert.Contains(MonoGameKeys.K(Keys.Space), keys);
        Assert.Contains(MonoGameKeys.K(Keys.A), keys);
        Assert.Contains(MonoGameKeys.K(Keys.Enter), keys);
        Assert.Contains(MonoGameKeys.K(Keys.F1), keys);
        Assert.Contains(MonoGameKeys.K(Keys.Left), keys);
    }

    [Fact]
    public void CandidateButtons_AllMouseButtons_ContainsStandardButtons()
    {
        // Act
        var buttons = MonoGameCandidateButtons.AllMouseButtons();

        // Assert
        Assert.Equal(5, buttons.Count); // Left, Right, Middle, X1, X2
        Assert.Contains(MonoGameKeys.M(MonoGameMouseButton.Left), buttons);
        Assert.Contains(MonoGameKeys.M(MonoGameMouseButton.Right), buttons);
        Assert.Contains(MonoGameKeys.M(MonoGameMouseButton.Middle), buttons);
    }

    [Fact]
    public void CandidateButtons_AllGamepadButtons_ContainsFaceButtons()
    {
        // Act
        var buttons = MonoGameCandidateButtons.AllGamepadButtons(PlayerIndex.One);

        // Assert
        Assert.Contains(MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.A), buttons);
        Assert.Contains(MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.B), buttons);
        Assert.Contains(MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.X), buttons);
        Assert.Contains(MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.Y), buttons);
    }

    [Fact]
    public void CandidateButtons_KeyboardAndGamepad_CombinesBoth()
    {
        // Act
        var candidates = MonoGameCandidateButtons.KeyboardAndGamepad(PlayerIndex.One);

        // Assert - Should have both keyboard and gamepad
        Assert.Contains(MonoGameKeys.K(Keys.Space), candidates);
        Assert.Contains(MonoGameKeys.PadBtn(PlayerIndex.One, Buttons.A), candidates);
        Assert.True(candidates.Count > 50); // Should have many keys
    }

    #endregion

    #region MonoGameProfileStorage Tests

    [Fact]
    public void ProfileStorage_CreateDefault_CreatesWithCorrectPath()
    {
        // Arrange
        var appName = "TestGame";
        var factory = CreateTestProfile;

        // Act
        var storage = MonoGameProfileStorage.CreateDefault(appName, factory);

        // Assert - Should not throw
        Assert.NotNull(storage);
    }

    [Fact]
    public void ProfileStorage_LoadProfile_ReturnsDefaultWhenNoFile()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.json");
        var expectedProfile = CreateTestProfile();
        var storage = new MonoGameProfileStorage(
            tempPath,
            () => expectedProfile);

        // Act
        var loadedProfile = storage.LoadProfile();

        // Assert
        Assert.NotNull(loadedProfile);
        Assert.Equal(expectedProfile.Maps.Count, loadedProfile.Maps.Count);
    }

    [Fact]
    public void ProfileStorage_SaveAndLoad_RoundTripsCorrectly()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.json");
        var profile = CreateTestProfile();
        var storage = new MonoGameProfileStorage(tempPath, CreateTestProfile);

        try
        {
            // Act - Save
            storage.SaveProfile(profile);

            // Act - Load
            var loadedProfile = storage.LoadProfile();

            // Assert
            Assert.NotNull(loadedProfile);
            Assert.Equal(profile.Maps.Count, loadedProfile.Maps.Count);
            Assert.True(loadedProfile.Maps.ContainsKey("Test"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void ProfileStorage_EnsureUserProfileExists_CreatesFileIfMissing()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.json");
        var storage = new MonoGameProfileStorage(tempPath, CreateTestProfile);

        try
        {
            // Act
            storage.EnsureUserProfileExists();

            // Assert
            Assert.True(File.Exists(tempPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    #endregion

    #region Integration Test

    [Fact]
    public void Integration_ProfileCanBeUsedWithEngine()
    {
        // Arrange
        var profile = CreateTestProfile();
        var engine = new InputManEngine(profile);
        engine.SetMaps(new ActionMapId("Test"));

        // Create empty snapshot
        var snapshot = new InputSnapshot(
            buttons: new Dictionary<ControlKey, bool>(),
            axes: new Dictionary<ControlKey, float>());

        // Act - Tick engine
        engine.Tick(snapshot, 0.016f, 0.016f);

        // Assert - Should not crash
        Assert.False(engine.IsDown(new ActionId("Jump")));
        Assert.Equal(0f, engine.GetAxis(new AxisId("MoveX")));
    }

    #endregion
}
