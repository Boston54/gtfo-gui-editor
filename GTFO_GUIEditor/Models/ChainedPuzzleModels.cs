using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ChainedPuzzleTypeEntry
{
    [JsonPropertyName("persistentID")]
    public uint PersistentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Prefab")]
    public string Prefab { get; set; } = string.Empty;

    public string DisplayText => !string.IsNullOrEmpty(Name) ? $"{Name} (ID: {PersistentId})" : $"ID: {PersistentId}";
}

[JsonConverter(typeof(ChainedPuzzleItemJsonConverter))]
public class ChainedPuzzleItem : INotifyPropertyChanged
{
    private uint _puzzleType = 6;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("PuzzleType")]
    public uint PuzzleType
    {
        get => _puzzleType;
        set
        {
            if (value == 0 && _puzzleType != 0)
            {
                return;
            }
            SetField(ref _puzzleType, value);
        }
    }

    public ChainedPuzzleItem Clone()
    {
        return new ChainedPuzzleItem
        {
            PuzzleType = PuzzleType
        };
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public class ChainedPuzzleItemJsonConverter : JsonConverter<ChainedPuzzleItem>
{
    public override ChainedPuzzleItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return new ChainedPuzzleItem { PuzzleType = reader.GetUInt32() };
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            if (uint.TryParse(reader.GetString(), out uint val))
            {
                return new ChainedPuzzleItem { PuzzleType = val };
            }
            return new ChainedPuzzleItem();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            uint puzzleType = 6;
            if (root.TryGetProperty("PuzzleType", out var prop) ||
                root.TryGetProperty("puzzleType", out prop) ||
                root.TryGetProperty("persistentID", out prop) ||
                root.TryGetProperty("PersistentId", out prop) ||
                root.TryGetProperty("id", out prop) ||
                root.TryGetProperty("ID", out prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    puzzleType = prop.GetUInt32();
                }
                else if (prop.ValueKind == JsonValueKind.String && uint.TryParse(prop.GetString(), out uint pVal))
                {
                    puzzleType = pVal;
                }
            }

            return new ChainedPuzzleItem { PuzzleType = puzzleType };
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing ChainedPuzzleItem.");
    }

    public override void Write(Utf8JsonWriter writer, ChainedPuzzleItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("PuzzleType", value.PuzzleType);
        writer.WriteEndObject();
    }
}
