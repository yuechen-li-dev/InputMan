using System;

namespace InputMan.Core;

public sealed class RebindRequest
{
    public ActionMapId Map { get; init; }
    public string BindingNameOrSlot { get; init; } = string.Empty;

    /// <summary>Exclude mouse motion / deltas by default to avoid accidental bindings.</summary>
    public bool ExcludeMouseMotion { get; init; } = true;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

public readonly record struct RebindProgress(string Message, float SecondsRemaining);

public sealed class RebindResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ControlKey? BoundControl { get; init; }
}

public interface IRebindSession
{
    event Action<RebindProgress>? OnProgress;
    event Action<RebindResult>? OnCompleted;
    void Cancel();
}
