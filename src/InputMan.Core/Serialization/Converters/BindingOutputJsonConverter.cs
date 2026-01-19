using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InputMan.Core.Serialization.Converters;

public sealed class BindingOutputJsonConverter : JsonConverter<BindingOutput>
{
    public override BindingOutput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected object for {nameof(BindingOutput)}.");

        string? kind = null;

        ActionId? action = null;
        AxisId? axis = null;
        float scale = 1f;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected property name while reading {nameof(BindingOutput)}.");

            var prop = reader.GetString();
            if (!reader.Read())
                throw new JsonException($"Unexpected end while reading {nameof(BindingOutput)}.");

            switch (prop)
            {
                case "kind":
                case "Kind":
                    kind = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    if (string.IsNullOrWhiteSpace(kind))
                        throw new JsonException("BindingOutput.kind must be a non-empty string.");
                    break;

                case "action":
                case "Action":
                    action = JsonSerializer.Deserialize<ActionId>(ref reader, options);
                    break;

                case "axis":
                case "Axis":
                    axis = JsonSerializer.Deserialize<AxisId>(ref reader, options);
                    break;

                case "scale":
                case "Scale":
                    scale = ReadFloat(ref reader, "scale");
                    break;

                default:
                    // Forward-compatible: ignore unknown fields
                    reader.Skip();
                    break;
            }
        }

        if (kind is null)
            throw new JsonException("BindingOutput missing required field 'kind'.");

        if (kind.Equals("Action", StringComparison.OrdinalIgnoreCase))
        {
            if (action is null)
                throw new JsonException("BindingOutput(kind=Action) missing required field 'action'.");
            return new ActionOutput(action.Value);
        }

        if (kind.Equals("Axis", StringComparison.OrdinalIgnoreCase))
        {
            if (axis is null)
                throw new JsonException("BindingOutput(kind=Axis) missing required field 'axis'.");
            return new AxisOutput(axis.Value, scale);
        }

        throw new JsonException($"Unknown BindingOutput.kind '{kind}'. Supported: 'Action', 'Axis'.");
    }

    public override void Write(Utf8JsonWriter writer, BindingOutput value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case ActionOutput a:
                writer.WriteString("kind", "Action");
                writer.WritePropertyName("action");
                JsonSerializer.Serialize(writer, a.Action, options);
                break;

            case AxisOutput ax:
                writer.WriteString("kind", "Axis");
                writer.WritePropertyName("axis");
                JsonSerializer.Serialize(writer, ax.Axis, options);
                if (ax.Scale != 1f)
                    writer.WriteNumber("scale", ax.Scale);
                break;

            default:
                throw new NotSupportedException($"Unsupported {nameof(BindingOutput)} type '{value.GetType().Name}'.");
        }

        writer.WriteEndObject();
    }

    private static float ReadFloat(ref Utf8JsonReader reader, string fieldName)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var f))
            throw new JsonException($"BindingOutput.{fieldName} must be a number.");
        return f;
    }
}
