using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ObjectiveTextDefaults
{
    [JsonPropertyName("Header")]
    public string Header { get; set; } = string.Empty;

    [JsonPropertyName("MainObjective")]
    public string MainObjective { get; set; } = string.Empty;

    [JsonPropertyName("FindLocationInfo")]
    public string FindLocationInfo { get; set; } = string.Empty;

    [JsonPropertyName("FindLocationInfoHelp")]
    public string FindLocationInfoHelp { get; set; } = string.Empty;

    [JsonPropertyName("GoToZone")]
    public string GoToZone { get; set; } = string.Empty;

    [JsonPropertyName("InZoneFindItem")]
    public string InZoneFindItem { get; set; } = string.Empty;

    [JsonPropertyName("SolveItem")]
    public string SolveItem { get; set; } = string.Empty;

    public ObjectiveTextDefaults Clone() => new()
    {
        Header = Header,
        MainObjective = MainObjective,
        FindLocationInfo = FindLocationInfo,
        FindLocationInfoHelp = FindLocationInfoHelp,
        GoToZone = GoToZone,
        InZoneFindItem = InZoneFindItem,
        SolveItem = SolveItem
    };
}
