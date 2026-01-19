using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InputMan.Core.Serialization.Converters;

/// <summary>
/// Serialize ID wrapper types (ActionId, AxisId, Axis2Id, ActionMapId) as plain JSON strings.
/// </summary>
public static class IdConverters
{
    /// <summary>
    /// Helper to register all ID converters on a JsonSerializerOptions.
    /// </summary>
    public static JsonSerializerOptions AddAll(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Converters.Add(new ActionIdJsonConverter());
        options.Converters.Add(new AxisIdJsonConverter());
        options.Converters.Add(new Axis2IdJsonConverter());
        options.Converters.Add(new ActionMapIdJsonConverter());

        return options;
    }
}

public sealed class ActionIdJsonConverter : JsonConverter<ActionId>
{
    public override ActionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonReadHelpers.ReadStringOrThrow(ref reader, nameof(ActionId)));

    public override void Write(Utf8JsonWriter writer, ActionId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Name);
}

public sealed class AxisIdJsonConverter : JsonConverter<AxisId>
{
    public override AxisId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonReadHelpers.ReadStringOrThrow(ref reader, nameof(AxisId)));

    public override void Write(Utf8JsonWriter writer, AxisId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Name);
}

public sealed class Axis2IdJsonConverter : JsonConverter<Axis2Id>
{
    public override Axis2Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonReadHelpers.ReadStringOrThrow(ref reader, nameof(Axis2Id)));

    public override void Write(Utf8JsonWriter writer, Axis2Id value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Name);
}

public sealed class ActionMapIdJsonConverter : JsonConverter<ActionMapId>
{
    public override ActionMapId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonReadHelpers.ReadStringOrThrow(ref reader, nameof(ActionMapId)));

    public override void Write(Utf8JsonWriter writer, ActionMapId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Name);
}

internal static class JsonReadHelpers
{
    public static string ReadStringOrThrow(ref Utf8JsonReader reader, string typeName)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s;

            throw new JsonException($"{typeName} cannot be null/empty.");
        }

        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException($"{typeName} cannot be null.");

        throw new JsonException($"Expected JSON string for {typeName}, got {reader.TokenType}.");
    }
}
