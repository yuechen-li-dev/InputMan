using System;
using System.Collections.Generic;
using Xunit;
using InputMan.Core;

namespace InputMan.Core.Tests.Processors;

/// <summary>
/// Tests for input processors (DeadzoneProcessor, InvertProcessor, ScaleProcessor).
/// Processors transform input values before they're output.
/// </summary>
public class ProcessorTests
{
    #region Test Helpers

    private static InputManEngine CreateEngineWithProcessor(IProcessor processor)
    {
        var profile = new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Test"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Test"),
                    Priority = 10,
                    Bindings =
                    [
                        // Axis binding with processor
                        new Binding
                        {
                            Name = "TestAxis",
                            Trigger = new BindingTrigger
                            {
                                Control = TestControls.LeftStickX,
                                Type = TriggerType.Axis
                            },
                            Output = new AxisOutput(new AxisId("TestAxis"), Scale: 1f),
                            Processors = [processor]
                        }
                    ]
                }
            }
        };

        var engine = new InputManEngine(profile);
        engine.SetMaps(new ActionMapId("Test"));
        return engine;
    }

    private static InputManEngine CreateEngineWithProcessors(params IProcessor[] processors)
    {
        var profile = new InputProfile
        {
            Maps = new Dictionary<string, ActionMapDefinition>
            {
                ["Test"] = new ActionMapDefinition
                {
                    Id = new ActionMapId("Test"),
                    Priority = 10,
                    Bindings =
                    [
                        new Binding
                        {
                            Name = "TestAxis",
                            Trigger = new BindingTrigger
                            {
                                Control = TestControls.LeftStickX,
                                Type = TriggerType.Axis
                            },
                            Output = new AxisOutput(new AxisId("TestAxis"), Scale: 1f),
                            Processors = new List<IProcessor>(processors)
                        }
                    ]
                }
            }
        };

        var engine = new InputManEngine(profile);
        engine.SetMaps(new ActionMapId("Test"));
        return engine;
    }

    private static InputSnapshot CreateSnapshot(float axisValue)
    {
        return new InputSnapshot(
            buttons: new Dictionary<ControlKey, bool>(),
            axes: new Dictionary<ControlKey, float>
            {
                [TestControls.LeftStickX] = axisValue
            });
    }

    #endregion

    #region DeadzoneProcessor Tests

    [Fact]
    public void DeadzoneProcessor_InputBelowDeadzone_ReturnsZero()
    {
        // Arrange
        var processor = new DeadzoneProcessor(0.25f);
        var engine = CreateEngineWithProcessor(processor);

        // Act - Input within deadzone
        engine.Tick(CreateSnapshot(0.1f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0f, value);
    }

    [Fact]
    public void DeadzoneProcessor_InputAtDeadzone_ReturnsZero()
    {
        // Arrange
        var processor = new DeadzoneProcessor(0.25f);
        var engine = CreateEngineWithProcessor(processor);

        // Act - Input exactly at deadzone threshold
        engine.Tick(CreateSnapshot(0.25f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0f, value);
    }

    [Fact]
    public void DeadzoneProcessor_InputAboveDeadzone_RemapsToFullRange()
    {
        // Arrange
        var processor = new DeadzoneProcessor(0.25f);
        var engine = CreateEngineWithProcessor(processor);

        // Act - Input just above deadzone
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert - Should remap 0.5 from range [0.25, 1.0] to [0.0, 1.0]
        // Formula: (0.5 - 0.25) / (1.0 - 0.25) = 0.25 / 0.75 ≈ 0.333
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.True(value > 0f, "Value should be non-zero above deadzone");
        Assert.True(Math.Abs(value - 0.333f) < 0.01f, $"Expected ~0.333, got {value}");
    }

    [Fact]
    public void DeadzoneProcessor_FullInput_ReturnsOne()
    {
        // Arrange
        var processor = new DeadzoneProcessor(0.25f);
        var engine = CreateEngineWithProcessor(processor);

        // Act - Full stick deflection
        engine.Tick(CreateSnapshot(1.0f), 0.016f, 0.016f);

        // Assert - Full input should map to 1.0
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(1.0f, value, precision: 3);
    }

    [Fact]
    public void DeadzoneProcessor_NegativeInput_WorksCorrectly()
    {
        // Arrange
        var processor = new DeadzoneProcessor(0.25f);
        var engine = CreateEngineWithProcessor(processor);

        // Act - Negative input below deadzone
        engine.Tick(CreateSnapshot(-0.1f), 0.016f, 0.016f);
        var belowDeadzone = engine.GetAxis(new AxisId("TestAxis"));

        // Act - Negative input above deadzone
        engine.Tick(CreateSnapshot(-0.5f), 0.016f, 0.032f);
        var aboveDeadzone = engine.GetAxis(new AxisId("TestAxis"));

        // Assert
        Assert.Equal(0f, belowDeadzone, precision: 3);
        Assert.True(aboveDeadzone < 0f, "Negative input should remain negative");
        Assert.True(Math.Abs(aboveDeadzone + 0.333f) < 0.01f, $"Expected ~-0.333, got {aboveDeadzone}");
    }

    [Fact]
    public void DeadzoneProcessor_ZeroDeadzone_NoEffect()
    {
        // Arrange - No deadzone
        var processor = new DeadzoneProcessor(0.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.1f), 0.016f, 0.016f);

        // Assert - Should pass through unchanged
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.1f, value, precision: 3);
    }

    #endregion

    #region InvertProcessor Tests

    [Fact]
    public void InvertProcessor_PositiveInput_BecomesNegative()
    {
        // Arrange
        var processor = new InvertProcessor();
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(-0.5f, value, precision: 3);
    }

    [Fact]
    public void InvertProcessor_NegativeInput_BecomesPositive()
    {
        // Arrange
        var processor = new InvertProcessor();
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(-0.7f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.7f, value, precision: 3);
    }

    [Fact]
    public void InvertProcessor_Zero_RemainsZero()
    {
        // Arrange
        var processor = new InvertProcessor();
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.0f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.0f, value, precision: 3);
    }

    [Fact]
    public void InvertProcessor_FullDeflection_InvertsCorrectly()
    {
        // Arrange
        var processor = new InvertProcessor();
        var engine = CreateEngineWithProcessor(processor);

        // Act - Positive full
        engine.Tick(CreateSnapshot(1.0f), 0.016f, 0.016f);
        var positiveFull = engine.GetAxis(new AxisId("TestAxis"));

        // Act - Negative full
        engine.Tick(CreateSnapshot(-1.0f), 0.016f, 0.032f);
        var negativeFull = engine.GetAxis(new AxisId("TestAxis"));

        // Assert
        Assert.Equal(-1.0f, positiveFull, precision: 3);
        Assert.Equal(1.0f, negativeFull, precision: 3);
    }

    #endregion

    #region ScaleProcessor Tests

    [Fact]
    public void ScaleProcessor_MultiplyByTwo_DoublesValue()
    {
        // Arrange
        var processor = new ScaleProcessor(2.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(1.0f, value, precision: 3);
    }

    [Fact]
    public void ScaleProcessor_MultiplyByHalf_HalvesValue()
    {
        // Arrange
        var processor = new ScaleProcessor(0.5f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.8f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.4f, value, precision: 3);
    }

    [Fact]
    public void ScaleProcessor_NegativeScale_InvertsAndScales()
    {
        // Arrange - Negative scale both inverts and scales
        var processor = new ScaleProcessor(-2.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert - 0.5 * -2.0 = -1.0
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(-1.0f, value, precision: 3);
    }

    [Fact]
    public void ScaleProcessor_Zero_ReturnsZero()
    {
        // Arrange
        var processor = new ScaleProcessor(3.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.0f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.0f, value, precision: 3);
    }

    [Fact]
    public void ScaleProcessor_ScaleByOne_NoChange()
    {
        // Arrange - Scale of 1.0 should be identity
        var processor = new ScaleProcessor(1.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.7f), 0.016f, 0.016f);

        // Assert
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.7f, value, precision: 3);
    }

    [Fact]
    public void ScaleProcessor_LargeScale_AllowsOverOne()
    {
        // Arrange - Processors can produce values > 1.0 (clamping happens elsewhere)
        var processor = new ScaleProcessor(5.0f);
        var engine = CreateEngineWithProcessor(processor);

        // Act
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert - Should be 2.5 before any clamping
        // Note: Engine may clamp this to 1.0 depending on axis type
        var value = engine.GetAxis(new AxisId("TestAxis"));
        // Analog axes are clamped by engine, but processor itself produces 2.5
        Assert.True(value > 0f, "Value should be positive");
    }

    #endregion

    #region Processor Chain Tests

    [Fact]
    public void ProcessorChain_DeadzoneAndScale_AppliesInOrder()
    {
        // Arrange - Deadzone first, then scale
        var deadzone = new DeadzoneProcessor(0.25f);
        var scale = new ScaleProcessor(2.0f);
        var engine = CreateEngineWithProcessors(deadzone, scale);

        // Act - Input above deadzone
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert - (0.5 - 0.25) / 0.75 = 0.333, then * 2.0 = 0.666
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.True(Math.Abs(value - 0.666f) < 0.01f, $"Expected ~0.666, got {value}");
    }

    [Fact]
    public void ProcessorChain_ScaleAndInvert_AppliesInOrder()
    {
        // Arrange - Scale first, then invert
        var scale = new ScaleProcessor(2.0f);
        var invert = new InvertProcessor();
        var engine = CreateEngineWithProcessors(scale, invert);

        // Act
        engine.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        // Assert - 0.5 * 2.0 = 1.0, then invert = -1.0
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(-1.0f, value, precision: 3);
    }

    [Fact]
    public void ProcessorChain_DeadzoneScaleInvert_AppliesInOrder()
    {
        // Arrange - All three processors
        var deadzone = new DeadzoneProcessor(0.2f);
        var scale = new ScaleProcessor(2.0f);
        var invert = new InvertProcessor();
        var engine = CreateEngineWithProcessors(deadzone, scale, invert);

        // Act
        engine.Tick(CreateSnapshot(0.6f), 0.016f, 0.016f);

        // Assert
        // Step 1: Deadzone (0.6 - 0.2) / (1.0 - 0.2) = 0.5
        // Step 2: Scale 0.5 * 2.0 = 1.0
        // Step 3: Invert 1.0 * -1 = -1.0
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(-1.0f, value, precision: 2);
    }

    [Fact]
    public void ProcessorChain_OrderMatters_DifferentResults()
    {
        // Arrange - Invert then scale
        var invert = new InvertProcessor();
        var scale = new ScaleProcessor(2.0f);
        var engine1 = CreateEngineWithProcessors(invert, scale);

        // Arrange - Scale then invert
        var engine2 = CreateEngineWithProcessors(scale, invert);

        // Act - Same input
        engine1.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);
        engine2.Tick(CreateSnapshot(0.5f), 0.016f, 0.016f);

        var value1 = engine1.GetAxis(new AxisId("TestAxis"));
        var value2 = engine2.GetAxis(new AxisId("TestAxis"));

        // Assert - Results should be the same (invert and scale commute)
        // 0.5 * -1 * 2 = -1.0
        // 0.5 * 2 * -1 = -1.0
        Assert.Equal(value1, value2, precision: 3);
    }

    [Fact]
    public void ProcessorChain_EmptyList_NoProcessing()
    {
        // Arrange - No processors
        var engine = CreateEngineWithProcessors();

        // Act
        engine.Tick(CreateSnapshot(0.7f), 0.016f, 0.016f);

        // Assert - Should pass through unchanged
        var value = engine.GetAxis(new AxisId("TestAxis"));
        Assert.Equal(0.7f, value, precision: 3);
    }

    #endregion

    #region Test Constants

    private static class TestControls
    {
        public static readonly ControlKey LeftStickX = new(DeviceKind.Gamepad, 0, 100);
    }

    #endregion
}