using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class EnemyEntry
{
    [JsonPropertyName("persistentID")]
    public uint PersistentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("EnemyType")]
    public string EnemyType { get; set; } = string.Empty;

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled { get; set; } = true;

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (PersistentId == 0) return "(None)";
            if (string.IsNullOrWhiteSpace(EnemyType)) return $"[{PersistentId}] {Name}";
            return $"[{PersistentId}] {Name} ({EnemyType})";
        }
    }

    public override string ToString() => DisplayText;
}

public class EnemyDataBlockFile
{
    [JsonPropertyName("Headers")]
    public List<HeaderItem> Headers { get; set; } = new();

    [JsonPropertyName("Blocks")]
    public List<EnemyEntry> Blocks { get; set; } = new();
}
