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
    /// <summary>
    /// If true, bypass JSON profile loading and always use the code-defined default profile.
    /// Handy for toggling in Game Studio while iterating.
    /// </summary>
    public bool UseCodeProfile { get; set; } = true;

    /// <summary>
    /// If true, ensure a user profile exists on disk so rebinding can persist.
    /// When UseCodeProfile=true, this will only seed the file if missing.
    /// </summary>
    public bool SeedUserProfileIfMissing { get; set; } = true;
    public override void Start()
    {
        // 1. Create profile storage (uses default paths)
        var storage = StrideProfileStorage.CreateDefault(
            appName: "ThirdPersonPlatformerInputManDemo",
            defaultProfileFactory: DefaultPlatformerProfile.Create);

        InputProfile profile;

        if (UseCodeProfile)
        {
            // Code-first override (skip JSON entirely) for debug
            profile = DefaultPlatformerProfile.Create();

            // Optional: seed a writable user profile for rebinding persistence (never overwrite)
            if (SeedUserProfileIfMissing && !storage.ProfileExists())
                storage.SaveProfile(profile);
        }
        else
        {
            // 2. Normal path: JSON-first (user profile, then bundled, then code fallback)
            if (SeedUserProfileIfMissing)
                storage.EnsureUserProfileExists();

            // 3. Load Profile
            profile = storage.LoadProfile();
        }

        // 4. Install InputMan system with both maps active
        var inputSystem = new StrideInputManSystem(
            Game.Services,
            profile,
            new ActionMapId("UI"),
            new ActionMapId("Gameplay"));

        Game.GameSystems.Add(inputSystem);

        Log.Info("InputMan installed successfully");
    }
}