using System;
using System.Collections.Generic;
using System.Linq;

namespace InputMan.Core.Validation;

public static class InputProfileValidator
{
    public static void Validate(InputProfile profile)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        // 1) Maps
        if (profile.Maps is null)
            throw new InvalidOperationException("InputProfile.Maps is null.");

        // Allow empty maps (maybe user is building incrementally), but usually it's a mistake.
        // If you want to enforce non-empty, uncomment:
        // if (profile.Maps.Count == 0) throw new InvalidOperationException("InputProfile.Maps is empty.");

        var mapIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (mapName, mapDef) in profile.Maps)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                throw new InvalidOperationException("InputProfile.Maps contains an empty key.");

            if (mapDef is null)
                throw new InvalidOperationException($"InputProfile.Maps[\"{mapName}\"] is null.");

            // MapDef.Id must match the dictionary key (this keeps JSON tidy and avoids confusion)
            if (!string.Equals(mapDef.Id.Name, mapName, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Map key \"{mapName}\" does not match mapDef.Id \"{mapDef.Id.Name}\".");

            if (!mapIds.Add(mapDef.Id.Name))
                throw new InvalidOperationException($"Duplicate map id \"{mapDef.Id.Name}\".");

            if (mapDef.Bindings is null)
                throw new InvalidOperationException($"Map \"{mapName}\" has Bindings=null.");

            ValidateBindings(mapDef);
        }

        // 2) Axis2
        // Keep this permissive: Axis2 can be empty.
        if (profile.Axis2 is null)
            throw new InvalidOperationException("InputProfile.Axis2 is null.");

        foreach (var (axis2Name, axis2Def) in profile.Axis2)
        {
            if (string.IsNullOrWhiteSpace(axis2Name))
                throw new InvalidOperationException("InputProfile.Axis2 contains an empty key.");

            if (axis2Def is null)
                throw new InvalidOperationException($"InputProfile.Axis2[\"{axis2Name}\"] is null.");

            // Axis2Def.Id should match key (same reason as maps)
            if (!string.Equals(axis2Def.Id.Name, axis2Name, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Axis2 key \"{axis2Name}\" does not match axis2Def.Id \"{axis2Def.Id.Name}\".");

            // Optional sanity: X/Y axis ids must not be default
            if (axis2Def.X.Equals(default(AxisId)) || axis2Def.Y.Equals(default))
                throw new InvalidOperationException($"Axis2 \"{axis2Name}\" has default X or Y AxisId.");
        }
    }

    private static void ValidateBindings(ActionMapDefinition map)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < map.Bindings.Count; i++)
        {
            var b = map.Bindings[i];
            if (b is null)
                throw new InvalidOperationException($"Map \"{map.Id.Name}\" has a null binding at index {i}.");

            if (string.IsNullOrWhiteSpace(b.Name))
                throw new InvalidOperationException($"Map \"{map.Id.Name}\" binding[{i}] has empty Name.");

            //if (!seenNames.Add(b.Name))
                //throw new InvalidOperationException($"Map \"{map.Id.Name}\" has duplicate binding name \"{b.Name}\".");

            if (b.Trigger is null)
                throw new InvalidOperationException($"Binding \"{b.Name}\" has Trigger=null.");

            ValidateTrigger(b.Name, b.Trigger);

            if (b.Output is null)
                throw new InvalidOperationException($"Binding \"{b.Name}\" has Output=null.");

            ValidateOutputMatchesTrigger(b.Name, b.Trigger, b.Output);

            // Consume sanity: no validation needed beyond enum correctness.
        }
    }

    private static void ValidateTrigger(string bindingName, BindingTrigger t)
    {
        // DeviceKind is byte and default(DeviceKind)=0 => invalid / uninitialized
        if (t.Control.Device == 0)
            throw new InvalidOperationException($"Binding \"{bindingName}\" has Control.Device=0 (uninitialized).");

        // DeviceIndex is byte; any value is fine, but you can constrain if you want.

        if (t.Type == TriggerType.Axis || t.Type == TriggerType.DeltaAxis)
        {
            if (t.Threshold < 0f)
                throw new InvalidOperationException($"Binding \"{bindingName}\" has negative Threshold.");
        }

        // ButtonEdge is only meaningful for button triggers; we don't hard-enforce it.
    }

    private static void ValidateOutputMatchesTrigger(string bindingName, BindingTrigger t, BindingOutput output)
    {
        // Your engine currently treats Axis and DeltaAxis triggers as requiring AxisOutput.
        // And Button triggers as requiring ActionOutput.
        switch (t.Type)
        {
            case TriggerType.Button:
                
                // Valid: ActionOutput (normal action buttons)
                // Valid: AxisOutput (ButtonAxis: buttons contribute to an axis while held)
                if (output is not ActionOutput && output is not AxisOutput)
                    throw new InvalidOperationException(
                        $"Binding \"{bindingName}\" is TriggerType.Button but Output is {output.GetType().Name}. " +
                        $"Expected ActionOutput or AxisOutput (ButtonAxis).");
                break;
                

            case TriggerType.Axis:
            case TriggerType.DeltaAxis:
                if (output is not AxisOutput)
                    throw new InvalidOperationException(
                        $"Binding \"{bindingName}\" is TriggerType.{t.Type} but Output is {output.GetType().Name}. Expected AxisOutput.");
                break;

            default:
                throw new InvalidOperationException(
                    $"Binding \"{bindingName}\" has unknown TriggerType {(int)t.Type}.");
        }
    }
}
