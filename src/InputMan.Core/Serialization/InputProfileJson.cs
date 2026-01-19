using System;
using System.IO;
using System.Text.Json;
using InputMan.Core.Validation;

namespace InputMan.Core.Serialization;

public static class InputProfileJson
{
    public static InputProfile Load(string json)
    {
        if (json is null) throw new ArgumentNullException(nameof(json));

        var profile = JsonSerializer.Deserialize<InputProfile>(json, InputProfileJsonOptions.Default)
            ?? throw new JsonException("Failed to deserialize InputProfile (result was null).");

        InputProfileValidator.Validate(profile);
        return profile;
    }

    public static InputProfile LoadFromFile(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var json = File.ReadAllText(path);
        return Load(json);
    }

    public static string Save(InputProfile profile, bool indented = true)
    {
        // Keep API flexible: allow callers to request compact output.
        var opts = indented ? InputProfileJsonOptions.Default : CreateCompactOptions();
        return JsonSerializer.Serialize(profile, opts);
    }

    public static void SaveToFile(InputProfile profile, string path, bool indented = true)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        File.WriteAllText(path, Save(profile, indented));
    }

    private static JsonSerializerOptions CreateCompactOptions()
    {
        // Clone so we don't mutate shared options.
        var opts = new JsonSerializerOptions(InputProfileJsonOptions.Default)
        {
            WriteIndented = false
        };
        return opts;
    }
}
