using System;

namespace InputMan.Core;

/// <summary>
/// Interface for loading and saving input profiles.
/// Implementations can use JSON, TOML, XML, or any serialization format.
/// Implementations handle file paths, directories, and I/O.
/// </summary>
public interface IProfileStorage
{
    /// <summary>
    /// Load an input profile from storage.
    /// Should return a default profile if none exists.
    /// </summary>
    /// <returns>The loaded profile, or a default profile if none exists.</returns>
    InputProfile LoadProfile();

    /// <summary>
    /// Save an input profile to storage.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the profile cannot be saved (e.g., disk full, permissions).
    /// </exception>
    void SaveProfile(InputProfile profile);

    /// <summary>
    /// Check if a saved profile exists.
    /// Useful for determining if this is a first-time user.
    /// </summary>
    /// <returns>True if a saved profile exists, false otherwise.</returns>
    bool ProfileExists();
}