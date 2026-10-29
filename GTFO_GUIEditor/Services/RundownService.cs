using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class FlexibleUIntConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out uint u)) return u;
            if (reader.TryGetInt64(out long l)) return (uint)l;
            if (reader.TryGetDouble(out double d)) return (uint)d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (uint.TryParse(str, out uint val)) return val;
            return 0;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out int i)) return i;
            if (reader.TryGetInt64(out long l)) return (int)l;
            if (reader.TryGetDouble(out double d)) return (int)d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (int.TryParse(str, out int val)) return val;
            return 0;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetDouble(out double d)) return d;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val)) return val;
            return 0.0;
        }
        return 0.0;
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleTextConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str)) return string.Empty;

            if (uint.TryParse(str, out uint id) && id > 0)
            {
                if (TextService.TryGetEnglishText(id, out string text))
                {
                    return text;
                }
            }

            return str;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            uint id = 0;
            if (reader.TryGetUInt32(out uint u))
            {
                id = u;
            }
            else if (reader.TryGetInt64(out long l) && l > 0)
            {
                id = (uint)l;
            }

            if (id > 0)
            {
                if (TextService.TryGetEnglishText(id, out string text))
                {
                    return text;
                }
            }

            return string.Empty;
        }

        return string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? string.Empty);
    }
}

public static class RundownService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new FlexibleUIntConverter(),
            new FlexibleIntConverter(),
            new FlexibleDoubleConverter(),
            new FlexibleTextConverter()
        }
    };

    public static RundownBlock CreateFreshBlock(uint id, string name)
    {
        return EditorDefaultsService.CreateDefaultRundownBlock(id, name);
    }

    public static RundownBlock CreateBlockFromTemplate(RundownBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
