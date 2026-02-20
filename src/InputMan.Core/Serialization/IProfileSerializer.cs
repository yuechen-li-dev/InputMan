namespace InputMan.Core.Serialization;

/// <summary>
/// Interface for profile serializers (JSON, TOML, XML, etc.).
/// </summary>
public interface IProfileSerializer
{
    string FileExtension { get; }
    string Serialize(InputProfile profile);
    InputProfile Deserialize(string content);
}

public class JsonProfileSerializer : IProfileSerializer
{
    public string FileExtension => "json";

    public string Serialize(InputProfile profile)
        => InputProfileJson.Save(profile, indented: true);

    public InputProfile Deserialize(string content)
        => InputProfileJson.Load(content);
}