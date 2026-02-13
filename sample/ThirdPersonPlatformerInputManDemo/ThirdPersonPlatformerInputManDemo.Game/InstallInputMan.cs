using InputMan.Core;
using InputMan.StrideConn;
using Stride.Engine;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Startup script that initializes the InputMan system.
/// Simple and clean.
/// </summary>
public sealed class InstallInputMan : StartupScript
{
    public override void Start()
    {
        // 1. Create profile storage (uses default paths)
        var storage = StrideProfileStorage.CreateDefault(
            appName: "ThirdPersonPlatformerInputManDemo",
            defaultProfileFactory: DefaultPlatformerProfile.Create);

        // 2. Ensure user profile exists (for rebinding)
        storage.EnsureUserProfileExists();

        // 3. Load profile
        var profile = storage.LoadProfile();

        // 4. Install InputMan system with both maps active
        var inputSystem = new StrideInputManSystem(
            Game.Services,
            profile,
            new ActionMapId("UI"),
            new ActionMapId("Gameplay"));

        Game.GameSystems.Add(inputSystem);

        Log.Info("✅ InputMan installed successfully");
    }
}