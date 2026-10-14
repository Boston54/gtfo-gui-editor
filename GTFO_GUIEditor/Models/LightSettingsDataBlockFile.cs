using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class LightSettingsDataBlockFile
{
    [JsonPropertyName("Headers")]
    public List<HeaderItem> Headers { get; set; } = new();

    [JsonPropertyName("Blocks")]
    public List<LightSettingsBlock> Blocks { get; set; } = new();
}
