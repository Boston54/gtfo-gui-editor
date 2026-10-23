using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ConsumableDistributionDataBlockFile
{
    [JsonPropertyName("Headers")]
    public List<HeaderItem> Headers { get; set; } = new();

    [JsonPropertyName("Blocks")]
    public List<ConsumableDistributionBlock> Blocks { get; set; } = new();

    [JsonPropertyName("LastPersistentID")]
    public uint LastPersistentId { get; set; }
}
