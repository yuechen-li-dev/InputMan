using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InputMan.Core.Serialization.Converters;

public sealed class ControlKeyJsonConverter : JsonConverter<ControlKey>
{
    public override ControlKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected object for {nameof(ControlKey)}.");

        DeviceKind? device = null;
        byte? deviceIndex = null;
        int? code = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected property name while reading {nameof(ControlKey)}.");

            var prop = reader.GetString();
            if (!reader.Read())
                throw new JsonException($"Unexpected end while reading {nameof(ControlKey)}.");

            switch (prop)
            {
                case "device":
                case "Device":
                    device = ReadDeviceKind(ref reader);
                    break;

                case "deviceIndex":
                case "DeviceIndex":
                    deviceIndex = ReadByte(ref reader, "deviceIndex");
                    break;

                case "code":
                case "Code":
                    code = ReadInt(ref reader, "code");
                    break;

                default:
                    // Skip unknown fields for forward compatibility
                    reader.Skip();
                    break;
            }
        }

        if (device is null) throw new JsonException($"{nameof(ControlKey)} missing required field 'device'.");
        if (deviceIndex is null) throw new JsonException($"{nameof(ControlKey)} missing required field 'deviceIndex'.");
        if (code is null) throw new JsonException($"{nameof(ControlKey)} missing required field 'code'.");

        return new ControlKey(device.Value, deviceIndex.Value, code.Value);
    }

    public override void Write(Utf8JsonWriter writer, ControlKey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("device", value.Device.ToString());
        writer.WriteNumber("deviceIndex", value.DeviceIndex);
        writer.WriteNumber("code", value.Code);
        writer.WriteEndObject();
    }

    private static DeviceKind ReadDeviceKind(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ParseDeviceKindName(reader.GetString()),
            JsonTokenType.Number => (DeviceKind)ReadInt32Checked(ref reader, "device"),
            _ => throw new JsonException("ControlKey.device must be a string or number.")
        };
    }

    private static DeviceKind ParseDeviceKindName(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException("ControlKey.device cannot be null/empty.");

        if (Enum.TryParse<DeviceKind>(s, ignoreCase: true, out var kind))
            return kind;

        throw new JsonException($"Unknown DeviceKind '{s}'.");
    }

    private static byte ReadByte(ref Utf8JsonReader reader, string fieldName)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var i))
            throw new JsonException($"ControlKey.{fieldName} must be a number.");

        if (i < byte.MinValue || i > byte.MaxValue)
            throw new JsonException($"ControlKey.{fieldName} out of range: {i}.");

        return (byte)i;
    }

    private static int ReadInt(ref Utf8JsonReader reader, string fieldName)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var i))
            throw new JsonException($"ControlKey.{fieldName} must be a number.");

        return i;
    }

    private static int ReadInt32Checked(ref Utf8JsonReader reader, string fieldName)
    {
        if (!reader.TryGetInt32(out var i))
            throw new JsonException($"ControlKey.{fieldName} must be an int32.");

        return i;
    }
}