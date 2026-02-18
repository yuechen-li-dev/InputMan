using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InputMan.Core.Serialization.Converters;

/// <summary>
/// Custom converter for IProcessor interface to support polymorphic deserialization.
/// Uses a "kind" discriminator field to determine which concrete processor type to instantiate.
/// </summary>
public sealed class ProcessorJsonConverter : JsonConverter<IProcessor>
{
    public override IProcessor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected object for {nameof(IProcessor)}.");

        string? kind = null;
        float? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected property name while reading {nameof(IProcessor)}.");

            var prop = reader.GetString();
            if (!reader.Read())
                throw new JsonException($"Unexpected end while reading {nameof(IProcessor)}.");

            switch (prop)
            {
                case "kind":
                case "Kind":
                    kind = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    if (string.IsNullOrWhiteSpace(kind))
                        throw new JsonException("IProcessor.kind must be a non-empty string.");
                    break;

                case "value":
                case "Value":
                case "scale":
                case "Scale":
                case "deadzone":
                case "Deadzone":
                    value = ReadFloat(ref reader);
                    break;

                default:
                    // Forward-compatible: ignore unknown fields
                    reader.Skip();
                    break;
            }
        }

        if (kind is null)
            throw new JsonException("IProcessor missing required field 'kind'.");

        return kind.ToLowerInvariant() switch
        {
            "deadzone" => CreateDeadzone(value),
            "scale" => CreateScale(value),
            "invert" => new InvertProcessor(),
            _ => throw new JsonException($"Unknown IProcessor.kind '{kind}'. Supported: 'Deadzone', 'Scale', 'Invert'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, IProcessor value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case DeadzoneProcessor d:
                writer.WriteString("kind", "Deadzone");
                writer.WriteNumber("deadzone", d.Deadzone);
                break;

            case ScaleProcessor s:
                writer.WriteString("kind", "Scale");
                writer.WriteNumber("scale", s.Scale);
                break;

            case InvertProcessor:
                writer.WriteString("kind", "Invert");
                break;

            default:
                throw new NotSupportedException($"Unsupported {nameof(IProcessor)} type '{value.GetType().Name}'.");
        }

        writer.WriteEndObject();
    }

    private static float ReadFloat(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var f))
            throw new JsonException("IProcessor numeric field must be a number.");
        return f;
    }

    private static DeadzoneProcessor CreateDeadzone(float? value)
    {
        if (value is null)
            throw new JsonException("DeadzoneProcessor missing required field 'deadzone'.");
        return new DeadzoneProcessor(value.Value);
    }

    private static ScaleProcessor CreateScale(float? value)
    {
        if (value is null)
            throw new JsonException("ScaleProcessor missing required field 'scale'.");
        return new ScaleProcessor(value.Value);
    }
}
