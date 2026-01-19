using System.Text.Json;
using System.Text.Json.Serialization;
using InputMan.Core.Serialization.Converters;

namespace InputMan.Core.Serialization;

/// <summary>
/// Central place to configure System.Text.Json options for InputMan profile serialization.
/// </summary>
public static class InputProfileJsonOptions
{
    /// <summary>
    /// Shared options instance for profile load/save.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        // Web defaults:
        // - camelCase property names
        // - relaxed escaping
        // - etc.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            // Helps when you add new fields later; old JSON won't explode.
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Prefer readable enums in JSON (e.g., "Keyboard" instead of 1)
        options.Converters.Add(new JsonStringEnumConverter());

        // ID wrappers -> strings
        IdConverters.AddAll(options);

        // Core input primitives
        options.Converters.Add(new ControlKeyJsonConverter());
        options.Converters.Add(new BindingOutputJsonConverter());

        return options;
    }
}
