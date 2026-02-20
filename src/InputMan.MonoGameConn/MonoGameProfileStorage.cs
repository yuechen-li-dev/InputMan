using InputMan.Core;
using InputMan.Core.Serialization;

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
}
