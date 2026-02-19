namespace InputMan.Core;

/// <summary>Logical action id (e.g. "Jump", "Pause").</summary> public readonly record struct ActionId(string Name) {     public override string ToString() => Name; }

/// <summary>Logical axis id (e.g. "MoveX", "LookY").</summary>F public readonly record struct AxisId(string Name) {     public override string ToString() => Name; }

/// <summary>Logical 2D axis id (e.g. "Move", "Look").</summary> public readonly record struct Axis2Id(string Name) {     public override string ToString() => Name; }

/// <summary>Logical action map id (e.g. "Gameplay", "UI").</summary> public readonly record struct ActionMapId(string Name) {     public override string ToString() => Name; } 