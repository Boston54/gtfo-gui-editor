using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ItemDataBlockEntry
{
    [JsonPropertyName("persistentID")]
    public int PersistentId { get; set; }

    [JsonPropertyName("publicName")]
    public string PublicName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayName =>
        $"{(string.IsNullOrWhiteSpace(PublicName) ? Name : PublicName)} ({Name}) [{PersistentId}]";
}
