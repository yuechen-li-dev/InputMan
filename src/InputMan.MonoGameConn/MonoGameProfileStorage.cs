using InputMan.Core;
using InputMan.Core.Serialization;
using System;
using System.IO;

namespace InputMan.MonoGameConn;

/// <summary>
/// Profile storage adapter for MonoGame using standard application data paths.
/// </summary>
public sealed class MonoGameProfileStorage : IProfileStorage
{
    private readonly string _userProfilePath;
    private readonly string? _bundledProfilePath;
    private readonly Func<InputProfile> _defaultProfileFactory;
    private readonly IProfileSerializer _serializer;

    /// <summary>
    /// Creates a MonoGameProfileStorage with custom paths.
    /// </summary>
    public MonoGameProfileStorage(
        string userProfilePath,
        Func<InputProfile> defaultProfileFactory,
        IProfileSerializer? serializer = null,
        string? bundledProfilePath = null)
    {
        _userProfilePath = userProfilePath ?? throw new ArgumentNullException(nameof(userProfilePath));
        _defaultProfileFactory = defaultProfileFactory ?? throw new ArgumentNullException(nameof(defaultProfileFactory));
        _serializer = serializer ?? new JsonProfileSerializer();
        _bundledProfilePath = bundledProfilePath;
    }

    /// <summary>
    /// Creates a MonoGameProfileStorage using standard paths in LocalApplicationData.
    /// </summary>
    /// <param name="appName">Application name (used for folder name).</param>
    /// <param name="defaultProfileFactory">Factory to create default profile.</param>
    /// <param name="serializer">Optional custom serializer (defaults to JSON).</param>
    public static MonoGameProfileStorage CreateDefault(
        string appName,
        Func<InputProfile> defaultProfileFactory,
        IProfileSerializer? serializer = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, appName);
        var userPath = Path.Combine(appFolder, $"profile.{(serializer ?? new JsonProfileSerializer()).FileExtension}");

        return new MonoGameProfileStorage(
            userProfilePath: userPath,
            defaultProfileFactory: defaultProfileFactory,
            serializer: serializer);
    }

    public InputProfile LoadProfile()
    {
#if DEBUG
        // In debug builds, always use code-defined profile to avoid stale cached profiles during development
        System.Diagnostics.Debug.WriteLine("DEBUG MODE: Loading profile from code (skipping disk)");
        return _defaultProfileFactory();
#else
        // Priority 1: User profile (contains rebinds)
        if (File.Exists(_userProfilePath))
        {
            try
            {
                var content = File.ReadAllText(_userProfilePath);
                return _serializer.Deserialize(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load user profile from {_userProfilePath}: {ex.Message}");
                // Fall through to next option
            }
        }

        // Priority 2: Bundled profile (shipped with game)
        if (_bundledProfilePath != null && File.Exists(_bundledProfilePath))
        {
            try
            {
                var content = File.ReadAllText(_bundledProfilePath);
                return _serializer.Deserialize(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load bundled profile from {_bundledProfilePath}: {ex.Message}");
                // Fall through to default
            }
        }

        // Priority 3: Code-defined default
        return _defaultProfileFactory();
#endif
    }

    public void SaveProfile(InputProfile profile)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_userProfilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var content = _serializer.Serialize(profile);
            File.WriteAllText(_userProfilePath, content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to save profile to {_userProfilePath}: {ex.Message}");
            throw;
        }
    }

    public void EnsureUserProfileExists()
    {
        if (!File.Exists(_userProfilePath))
        {
            var profile = LoadProfile(); // Gets default or bundled
            SaveProfile(profile);
        }
    }
    public bool ProfileExists()
    {
        return File.Exists(_userProfilePath);
    }
}
