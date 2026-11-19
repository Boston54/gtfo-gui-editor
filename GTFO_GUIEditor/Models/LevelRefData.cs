using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

/// <summary>
/// Settings that belong to a level regardless of which rundown tier contains it.
/// </summary>
public class LevelRefData
{
    [JsonPropertyName("persistentID")]
    public uint PersistentId { get; set; }

    [JsonPropertyName("playerStartingState")]
    public SpecialOverrideData PlayerStartingState { get; set; } = new();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("wardenIntel")]
    public string WardenIntel { get; set; } = string.Empty;

    [JsonPropertyName("fogSetting")]
    public uint FogSetting { get; set; }

    [JsonPropertyName("defaultLightSetting")]
    public uint DefaultLightSetting { get; set; }

    [JsonPropertyName("mainObjectiveID")]
    public uint MainObjectiveId { get; set; }

    [JsonPropertyName("secondaryObjectiveID")]
    public uint SecondaryObjectiveId { get; set; }

    [JsonPropertyName("overloadObjectiveID")]
    public uint OverloadObjectiveId { get; set; }

    [JsonPropertyName("mainObjective")]
    public LevelObjectiveRefData MainObjective { get; set; } = new();

    [JsonPropertyName("secondaryObjective")]
    public LevelObjectiveRefData SecondaryObjective { get; set; } = new();

    [JsonPropertyName("overloadObjective")]
    public LevelObjectiveRefData OverloadObjective { get; set; } = new();

    public LevelRefData Clone() => new()
    {
        PersistentId = PersistentId,
        PlayerStartingState = PlayerStartingState?.Clone() ?? new SpecialOverrideData(),
        Description = Description,
        WardenIntel = WardenIntel,
        FogSetting = FogSetting,
        DefaultLightSetting = DefaultLightSetting,
        MainObjectiveId = MainObjectiveId,
        SecondaryObjectiveId = SecondaryObjectiveId,
        OverloadObjectiveId = OverloadObjectiveId,
        MainObjective = MainObjective?.Clone() ?? new LevelObjectiveRefData(),
        SecondaryObjective = SecondaryObjective?.Clone() ?? new LevelObjectiveRefData(),
        OverloadObjective = OverloadObjective?.Clone() ?? new LevelObjectiveRefData()
    };
}
