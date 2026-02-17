using InputMan.Core;
using InputMan.Core.Serialization;
using System;
using System.IO;

namespace InputMan.StrideConn;

/// <summary>
/// Stride-specific implementation of IProfileStorage.
/// Handles profile loading/saving with configurable paths and serialization.
/// Default uses JSON serialization, but can be extended for TOML, XML, etc.
/// </summary>
/// <remarks>
/// Create a new StrideProfileStorage with custom paths and serialization.
/// </remarks>
/// <param name="userProfilePath">Path to user's writable profile (for rebinds).</param>
/// <param name="defaultProfileFactory">Factory function to create default profile when none exists.</param>
/// <param name="serializer">Optional serializer (defaults to JSON). Can use TOML, XML, etc.</param>
/// <param name="bundledProfilePath">Optional path to bundled default profile (shipped with game).</param>
public class StrideProfileStorage(
    string userProfilePath,
    Func<InputProfile> defaultProfileFactory,
    IProfileSerializer? serializer = null,
    string? bundledProfilePath = null) : IProfileStorage
{
    private readonly string _userProfilePath = userProfilePath ?? throw new ArgumentNullException(nameof(userProfilePath));
    private readonly string? _bundledProfilePath = bundledProfilePath;
    private readonly Func<InputProfile> _defaultProfileFactory = defaultProfileFactory ?? throw new ArgumentNullException(nameof(defaultProfileFactory));
    private readonly IProfileSerializer _serializer = serializer ?? new JsonProfileSerializer();

    /// <summary>
    /// Convenience constructor using standard paths for a Stride game.
    /// User profile: %LocalAppData%/[appName]/profile.[ext]
    /// Bundled profile: Resources/Input/profile.[ext]
    /// </summary>
    public static StrideProfileStorage CreateDefault(
        string appName,
        Func<InputProfile> defaultProfileFactory,
        IProfileSerializer? serializer = null)
    {
        serializer ??= new JsonProfileSerializer();

        var userProfilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            $"profile.{serializer.FileExtension}");

        var bundledProfilePath = Path.Combine(
            "Resources",
            "Input",
            $"profile.{serializer.FileExtension}");

        return new StrideProfileStorage(
            userProfilePath,
            defaultProfileFactory,
            serializer,
            bundledProfilePath);
    }

    public InputProfile LoadProfile()
    {
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
    }

    public void SaveProfile(InputProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_userProfilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize and save
            var content = _serializer.Serialize(profile);
            File.WriteAllText(_userProfilePath, content);

            System.Diagnostics.Debug.WriteLine($"Saved profile to: {_userProfilePath}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to save profile to {_userProfilePath}: {ex.Message}",
                ex);
        }
    }

    public bool ProfileExists()
    {
        return File.Exists(_userProfilePath);
    }

    /// <summary>
    /// Ensure a user profile exists. If not, save the default profile.
    /// Useful for first-time setup.
    /// </summary>
    public void EnsureUserProfileExists()
    {
        if (ProfileExists())
            return;

        var defaultProfile = _defaultProfileFactory();
        SaveProfile(defaultProfile);
    }
}

/// <summary>
/// Interface for profile serializers (JSON, TOML, XML, etc.).
/// Allows swapping serialization formats without changing storage logic.
/// </summary>
public interface IProfileSerializer
{
    /// <summary>
    /// File extension for this serializer (e.g., "json", "toml").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Serialize a profile to a string.
    /// </summary>
    string Serialize(InputProfile profile);

    /// <summary>
    /// Deserialize a profile from a string.
    /// </summary>
    InputProfile Deserialize(string content);
}

/// <summary>
/// JSON serializer implementation (default).
/// Uses InputMan.Core's built-in JSON serialization.
/// </summary>
public class JsonProfileSerializer : IProfileSerializer
{
    public string FileExtension => "json";

    public string Serialize(InputProfile profile)
    {
        return InputProfileJson.Save(profile, indented: true);
    }

    public InputProfile Deserialize(string content)
    {
        return InputProfileJson.Load(content);
    }
}