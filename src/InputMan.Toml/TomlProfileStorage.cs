using InputMan.Core;

namespace InputMan.Toml;

public sealed class TomlProfileStorage(string path, Func<InputProfile> defaultProfileFactory) : IProfileStorage
{
    private readonly string path = Path.GetFullPath(path);
    private readonly Func<InputProfile> defaultProfileFactory = defaultProfileFactory ?? throw new ArgumentNullException(nameof(defaultProfileFactory));

    public bool ProfileExists() => File.Exists(path);
    public InputProfile LoadProfile() => ProfileExists() ? InputProfileToml.LoadFromFile(path) : defaultProfileFactory();
    public void SaveProfile(InputProfile profile) => InputProfileToml.SaveToFile(profile, path);
}

/// <summary>User profile wins as a whole, then bundled profile, then typed code defaults.</summary>
public sealed class LayeredTomlProfileStorage(
    string userPath,
    string? bundledPath,
    Func<InputProfile> codeDefaultFactory) : IProfileStorage
{
    private readonly string userPath = Path.GetFullPath(userPath);
    private readonly string? bundledPath = bundledPath is null ? null : Path.GetFullPath(bundledPath);
    private readonly Func<InputProfile> codeDefaultFactory = codeDefaultFactory ?? throw new ArgumentNullException(nameof(codeDefaultFactory));

    public bool ProfileExists() => File.Exists(userPath);

    public InputProfile LoadProfile()
    {
        if (File.Exists(userPath)) return InputProfileToml.LoadFromFile(userPath);
        if (bundledPath is not null && File.Exists(bundledPath)) return InputProfileToml.LoadFromFile(bundledPath);
        return codeDefaultFactory();
    }

    public void SaveProfile(InputProfile profile) => InputProfileToml.SaveToFile(profile, userPath);
}
