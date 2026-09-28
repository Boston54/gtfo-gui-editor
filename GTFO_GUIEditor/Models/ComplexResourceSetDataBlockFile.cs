using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ComplexResourceSetDataBlockFile
{
    [JsonPropertyName("Headers")]
    public List<HeaderItem> Headers { get; set; } = new();

    [JsonPropertyName("Blocks")]
    public List<ComplexResourceSetBlock> Blocks { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
