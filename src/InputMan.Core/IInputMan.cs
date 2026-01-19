using System;
using System.Numerics;

namespace InputMan.Core;

public interface IInputMan
{
    long FrameIndex { get; }
    float DeltaTimeSeconds { get; }

    bool IsDown(ActionId action);
    bool WasPressed(ActionId action);
    bool WasReleased(ActionId action);

    float GetAxis(AxisId axis);
    Vector2 GetAxis2(Axis2Id axis2);

    void PushMap(ActionMapId map, int? priorityOverride = null);
    void PopMap(ActionMapId map);
    void SetMaps(params ActionMapId[] maps);

    event Action<ActionEvent>? OnAction;
    event Action<AxisEvent>? OnAxis;

    // Rebinding
    IRebindSession StartRebind(RebindRequest request);

    InputProfile ExportProfile();
    void ImportProfile(InputProfile profile);
}
