using Xunit;
using InputMan.Core.Rebind;

namespace InputMan.Core.Tests;

/// <summary>
/// Tests for runtime rebinding functionality.
/// Covers session lifecycle, conflict detection, and guardrails.
/// </summary>
public class RebindingTests
{
    #region Test Helpers

    private static InputManEngine CreateEngine()
    {
        var profile = new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Gameplay"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Gameplay"),
                    Priority = 10,
                    Bindings =
                    [
                        // Jump bound to Space initially
                        new Binding
                        {
                            Name = "Jump.Kb",
                            Trigger = new BindingTrigger
                            {
                                Control = TestControls.Space,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed
                            },
                            Output = new ActionOutput(new ActionId("Jump"))
                        },
                        // Fire bound to LeftMouse
                        new Binding
                        {
                            Name = "Fire.Mouse",
                            Trigger = new BindingTrigger
                            {
                                Control = TestControls.LeftMouse,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed
                            },
                            Output = new ActionOutput(new ActionId("Fire"))
                        }
                    ]
                },
                ["UI"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("UI"),
                    Priority = 100,
                    Bindings =
                    [
                        // Confirm bound to Enter
                        new Binding
                        {
                            Name = "Confirm.Kb",
                            Trigger = new BindingTrigger
                            {
                                Control = TestControls.Enter,
                                Type = TriggerType.Button,
                                ButtonEdge = ButtonEdge.Pressed
                            },
                            Output = new ActionOutput(new ActionId("Confirm"))
                        }
                    ]
                }
            }
        };

        return new InputManEngine(profile);
    }

    private static InputSnapshot CreateSnapshot(params (ControlKey control, bool down)[] buttons)
    {
        var buttonDict = new Dictionary<ControlKey, bool>();
        foreach (var (control, down) in buttons)
        {
            buttonDict[control] = down;
        }

        // Use constructor that accepts dictionaries
        return new InputSnapshot(
            buttons: buttonDict,
            axes: new Dictionary<ControlKey, float>());
    }

    #endregion

    #region Session Lifecycle Tests

    [Fact]
    public void StartRebind_ValidBinding_CreatesSession()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J, TestControls.K }
        };

        // Act
        var session = engine.StartRebind(request);

        // Assert
        Assert.NotNull(session);
        Assert.True(engine.IsRebinding);
        Assert.Equal(request, session.Request);
    }

    [Fact]
    public void StartRebind_InvalidMap_ThrowsException()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("NonExistentMap"),
            BindingNameOrSlot = "Jump.Kb"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => engine.StartRebind(request));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void StartRebind_InvalidBindingName_ThrowsException()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "NonExistentBinding"
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => engine.StartRebind(request));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void StartRebind_CancelsExistingSession()
    {
        // Arrange
        var engine = CreateEngine();
        var request1 = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J }
        };
        var request2 = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Fire.Mouse",
            CandidateButtons = new[] { TestControls.K }
        };

        bool firstSessionCanceled = false;

        // Act
        var session1 = engine.StartRebind(request1);
        session1.OnCompleted += r => firstSessionCanceled = !r.Succeeded;

        var session2 = engine.StartRebind(request2);

        // Assert
        Assert.True(firstSessionCanceled, "First session should be canceled");
        Assert.Equal(request2, session2.Request);
    }

    [Fact]
    public void CancelRebind_ActiveSession_CompletesWithFailure()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J }
        };

        bool completed = false;
        bool succeeded = true;
        string? error = null;

        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            completed = true;
            succeeded = r.Succeeded;
            error = r.Error;
        };

        // Act
        session.Cancel();

        // Assert
        Assert.True(completed);
        Assert.False(succeeded);
        Assert.Contains("Canceled", error);
        Assert.False(engine.IsRebinding);
    }

    #endregion

    #region Successful Rebinding Tests

    [Fact]
    public void Rebind_ValidKey_SucceedsAndUpdatesBinding()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J, TestControls.K },
            Timeout = TimeSpan.FromSeconds(10)
        };

        bool completed = false;
        bool succeeded = false;
        ControlKey? boundControl = null;

        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            completed = true;
            succeeded = r.Succeeded;
            boundControl = r.BoundControl;
        };

        // Tick once to seed (ignore already-held keys)
        var snapshot1 = CreateSnapshot();
        engine.Tick(snapshot1, 0.016f, 0.016f);

        // Act - Press J key
        var snapshot2 = CreateSnapshot((TestControls.J, true));
        engine.Tick(snapshot2, 0.016f, 0.032f);

        // Assert
        Assert.True(completed, "Rebind should complete");
        Assert.True(succeeded, "Rebind should succeed");
        Assert.Equal(TestControls.J, boundControl);
        Assert.False(engine.IsRebinding);

        // Verify binding was actually updated in the profile
        var profile = engine.ExportProfile();
        var binding = profile.Maps["Gameplay"].Bindings.First(b => b.Name == "Jump.Kb");
        Assert.Equal(TestControls.J, binding.Trigger.Control);
    }

    [Fact]
    public void Rebind_IgnoresAlreadyHeldKeys()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J, TestControls.K }
        };

        bool completed = false;

        var session = engine.StartRebind(request);
        session.OnCompleted += r => completed = true;

        // Act - J is already held when session starts
        var snapshotHeld = CreateSnapshot((TestControls.J, true));
        engine.Tick(snapshotHeld, 0.016f, 0.016f); // Seed frame

        // Still holding J in next frame - should be ignored
        engine.Tick(snapshotHeld, 0.016f, 0.032f);

        // Assert - Should NOT have completed
        Assert.False(completed, "Should ignore already-held key");
        Assert.True(engine.IsRebinding);
    }

    [Fact]
    public void Rebind_NewKeyPress_CapturesCorrectly()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J, TestControls.K }
        };

        ControlKey? boundControl = null;
        var session = engine.StartRebind(request);
        session.OnCompleted += r => boundControl = r.BoundControl;

        // J is held during seed
        var snapshot1 = CreateSnapshot((TestControls.J, true));
        engine.Tick(snapshot1, 0.016f, 0.016f);

        // J released
        var snapshot2 = CreateSnapshot();
        engine.Tick(snapshot2, 0.016f, 0.032f);

        // Act - K pressed (new key, should capture)
        var snapshot3 = CreateSnapshot((TestControls.K, true));
        engine.Tick(snapshot3, 0.016f, 0.048f);

        // Assert
        Assert.Equal(TestControls.K, boundControl);
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public void Rebind_ConflictInSameMap_FailsWithError()
    {
        // Arrange
        var engine = CreateEngine();

        // Try to rebind Jump to LeftMouse (which is already used by Fire)
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.LeftMouse },
            DisallowConflictsInSameMap = true
        };

        bool completed = false;
        bool succeeded = true;
        string? error = null;

        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            completed = true;
            succeeded = r.Succeeded;
            error = r.Error;
        };

        // Seed
        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act - Press LeftMouse (conflicting key)
        engine.Tick(CreateSnapshot((TestControls.LeftMouse, true)), 0.016f, 0.032f);

        // Assert
        Assert.True(completed);
        Assert.False(succeeded);
        Assert.Contains("already bound", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rebind_ConflictInDifferentMap_SucceedsWhenCrossMapCheckDisabled()
    {
        // Arrange
        var engine = CreateEngine();

        // Rebind Jump to Enter (which is used in UI map)
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.Enter },
            DisallowConflictsInSameMap = true,  // Only check same map
            DisallowConflictsAcrossAllMaps = false
        };

        bool succeeded = false;
        var session = engine.StartRebind(request);
        session.OnCompleted += r => succeeded = r.Succeeded;

        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act
        engine.Tick(CreateSnapshot((TestControls.Enter, true)), 0.016f, 0.032f);

        // Assert - Should succeed because Enter is in a different map
        Assert.True(succeeded);
    }

    [Fact]
    public void Rebind_ConflictAcrossAllMaps_FailsWhenEnabled()
    {
        // Arrange
        var engine = CreateEngine();

        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.Enter },
            DisallowConflictsAcrossAllMaps = true  // Check ALL maps
        };

        bool succeeded = true;
        string? error = null;
        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            succeeded = r.Succeeded;
            error = r.Error;
        };

        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act
        engine.Tick(CreateSnapshot((TestControls.Enter, true)), 0.016f, 0.032f);

        // Assert - Should fail because Enter is used in UI map
        Assert.False(succeeded);
        Assert.Contains("already bound", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rebind_SameKey_AllowedForSameBinding()
    {
        // Arrange
        var engine = CreateEngine();

        // Rebind Jump back to Space (its current binding)
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.Space },
            DisallowConflictsInSameMap = true
        };

        bool succeeded = false;
        var session = engine.StartRebind(request);
        session.OnCompleted += r => succeeded = r.Succeeded;

        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act - Rebind to Space (same key it already has)
        engine.Tick(CreateSnapshot((TestControls.Space, true)), 0.016f, 0.032f);

        // Assert - Should succeed (binding to itself is allowed)
        Assert.True(succeeded);
    }

    #endregion

    #region Guardrail Tests

    [Fact]
    public void Rebind_ForbiddenControl_IsIgnored()
    {
        // Arrange
        var engine = CreateEngine();

        // Test with J forbidden - it should be silently ignored
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J, TestControls.K },
            ForbiddenControls = new HashSet<ControlKey> { TestControls.J } // Forbid J
        };

        bool completed = false;
        ControlKey? boundControl = null;
        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            completed = true;
            boundControl = r.BoundControl;
        };

        // Seed frame
        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act - Press forbidden J key (should be ignored)
        engine.Tick(CreateSnapshot((TestControls.J, true)), 0.016f, 0.032f);

        // Assert - Should NOT have completed (J was ignored)
        Assert.False(completed, "Rebind should not complete when pressing forbidden key");
        Assert.True(engine.IsRebinding, "Session should still be active");

        // Now press allowed K key
        engine.Tick(CreateSnapshot((TestControls.K, true)), 0.016f, 0.048f);

        // Should complete successfully with K
        Assert.True(completed, "Rebind should complete with allowed key");
        Assert.Equal(TestControls.K, boundControl);
    }


    [Fact]
    public void Rebind_AllowedDevices_RejectsOtherDevices()
    {
        // Arrange
        var engine = CreateEngine();

        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.LeftMouse, TestControls.J },
            AllowedDevices = new HashSet<DeviceKind> { DeviceKind.Keyboard } // Only keyboard
        };

        bool completed = false;
        var session = engine.StartRebind(request);
        session.OnCompleted += r => completed = r.Succeeded;

        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);

        // Act - Try mouse button (not allowed)
        engine.Tick(CreateSnapshot((TestControls.LeftMouse, true)), 0.016f, 0.032f);

        // Assert - Should not complete (mouse not allowed)
        Assert.False(completed);
        Assert.True(engine.IsRebinding);

        // Now try keyboard (allowed)
        engine.Tick(CreateSnapshot((TestControls.J, true)), 0.016f, 0.048f);
        Assert.True(completed);
    }

    [Fact]
    public void Rebind_Timeout_FailsAfterDuration()
    {
        // Arrange
        var engine = CreateEngine();

        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J },
            Timeout = TimeSpan.FromSeconds(1) // 1 second timeout
        };

        bool completed = false;
        bool succeeded = true;
        string? error = null;
        var session = engine.StartRebind(request);
        session.OnCompleted += r =>
        {
            completed = true;
            succeeded = r.Succeeded;
            error = r.Error;
        };

        engine.Tick(CreateSnapshot(), 0.016f, 0.0f);

        // Act - Wait past timeout without pressing anything
        engine.Tick(CreateSnapshot(), 0.5f, 0.5f);
        Assert.False(completed); // Not yet

        engine.Tick(CreateSnapshot(), 0.6f, 1.1f); // Past 1 second

        // Assert
        Assert.True(completed);
        Assert.False(succeeded);
        Assert.Contains("Timed out", error);
    }

    #endregion

    #region Progress Event Tests

    [Fact]
    public void Rebind_ProgressEvent_FiresEachFrame()
    {
        // Arrange
        var engine = CreateEngine();
        var request = new RebindRequest
        {
            Map = new ActionMapId("Gameplay"),
            BindingNameOrSlot = "Jump.Kb",
            CandidateButtons = new[] { TestControls.J },
            Timeout = TimeSpan.FromSeconds(5)
        };

        int progressCount = 0;
        float lastRemaining = 0f;

        var session = engine.StartRebind(request);
        session.OnProgress += p =>
        {
            progressCount++;
            lastRemaining = p.SecondsRemaining;
        };

        // Act - Tick multiple frames without completing
        engine.Tick(CreateSnapshot(), 0.016f, 0.0f);
        engine.Tick(CreateSnapshot(), 0.016f, 0.016f);
        engine.Tick(CreateSnapshot(), 0.016f, 0.032f);

        // Assert
        Assert.True(progressCount >= 2, "Progress should fire multiple times");
        Assert.True(lastRemaining < 5.0f, "Remaining time should decrease");
        Assert.True(lastRemaining > 4.9f, "But not decrease too much in short time");
    }

    #endregion

    #region Test Constants

    private static class TestControls
    {
        public static readonly ControlKey Space = new(DeviceKind.Keyboard, 0, 32);
        public static readonly ControlKey J = new(DeviceKind.Keyboard, 0, 74);
        public static readonly ControlKey K = new(DeviceKind.Keyboard, 0, 75);
        public static readonly ControlKey Enter = new(DeviceKind.Keyboard, 0, 13);
        public static readonly ControlKey Escape = new(DeviceKind.Keyboard, 0, 27);
        public static readonly ControlKey LeftMouse = new(DeviceKind.Mouse, 0, 0);
    }

    #endregion
}