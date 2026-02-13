using InputMan.Core;
using InputMan.Core.Serialization;
using InputMan.StrideConn;
using Stride.Engine;
using System;
using System.IO;

namespace ThirdPersonPlatformerInputManDemo;

/// <summary>
/// Startup script that initializes the InputMan system.
/// Loads profiles in priority order: user overrides > bundled defaults > code fallback.
/// </summary>
public sealed class InstallInputMan : StartupScript
{
    /// <summary>
    /// Which maps should be active when the game starts.
    /// Default: Gameplay + UI
    /// </summary>
    private ActionMapId[] InitialMaps { get; init; } =
    [
        new ActionMapId("UI"),
        new ActionMapId("Gameplay")
    ];

    public override void Start()
    {
        // 1. Load profile using clear priority order
        var profile = ProfileLoader.LoadProfile();

        // 2. Ensure user profile exists for rebinding
        ProfileLoader.EnsureUserProfileExists(profile);

        // 3. Install InputMan system
        var inputSystem = new StrideInputManSystem(
            Game.Services,
            profile,
            InitialMaps);

        Game.GameSystems.Add(inputSystem);
    }
}

/// <summary>
/// Handles profile loading and persistence.
/// Centralizes all file path and loading logic.
/// </summary>
public static class ProfileLoader
{
    private static readonly string UserProfilePath = GetUserProfilePath();
    private static readonly string BundledProfilePath = GetBundledProfilePath();

    /// <summary>
    /// Loads the best available profile in priority order:
    /// 1. User profile (rebinds, custom settings)
    /// 2. Bundled profile (shipped with game)
    /// 3. Code-defined default (embedded fallback)
    /// </summary>
    public static InputProfile LoadProfile()
    {
        // User profile has highest priority (contains rebinds)
        if (File.Exists(UserProfilePath))
        {
            try
            {
                return LoadFromFile(UserProfilePath);
            }
            catch (Exception ex)
            {
                // Corrupt user profile - fall through to defaults
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load user profile: {ex.Message}");
            }
        }

        // Bundled profile (shipped JSON)
        if (File.Exists(BundledProfilePath))
        {
            try
            {
                return LoadFromFile(BundledProfilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load bundled profile: {ex.Message}");
            }
        }

        // Fallback: code-defined default
        return DefaultPlatformerProfile.Create();
    }

    /// <summary>
    /// Ensures a user profile file exists. If not, creates one from the given profile.
    /// This gives rebinding a writable location.
    /// </summary>
    public static void EnsureUserProfileExists(InputProfile profile)
    {
        if (File.Exists(UserProfilePath))
            return;

        var dir = Path.GetDirectoryName(UserProfilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        SaveUserProfile(profile);
    }

    /// <summary>
    /// Saves a profile to the user's writable location.
    /// </summary>
    public static void SaveUserProfile(InputProfile profile)
    {
        var dir = Path.GetDirectoryName(UserProfilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(UserProfilePath, InputProfileJson.Save(profile));
        System.Diagnostics.Debug.WriteLine($"Saved profile: {UserProfilePath}");
    }

    private static InputProfile LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return InputProfileJson.Load(json);
    }

    private static string GetUserProfilePath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(
            appData,
            "ThirdPersonPlatformerInputManDemo",
            "profile.json");
    }

    private static string GetBundledProfilePath()
    {
        return Path.Combine("Resources", "Input", "profile.json");
    }
}