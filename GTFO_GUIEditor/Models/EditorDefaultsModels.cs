using System.Collections.Generic;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Models;

public class EditorDefaultsRoot
{
    [JsonPropertyName("LevelLayoutWindow")]
    public LevelLayoutWindowDefaults LevelLayoutWindow { get; set; } = new();

    [JsonPropertyName("AlarmsWindow")]
    public AlarmsWindowDefaults AlarmsWindow { get; set; } = new();

    [JsonPropertyName("LightingWindow")]
    public LightingWindowDefaults LightingWindow { get; set; } = new();

    [JsonPropertyName("FogSettingsWindow")]
    public FogSettingsWindowDefaults FogSettingsWindow { get; set; } = new();

    [JsonPropertyName("RundownWindow")]
    public RundownWindowDefaults RundownWindow { get; set; } = new();
}

public class LevelLayoutWindowDefaults
{
    [JsonPropertyName("LevelSettingsDialog")]
    public LevelSettingsDialogDefaults LevelSettingsDialog { get; set; } = new();

    [JsonPropertyName("SecurityDoorSettings")]
    public SecurityDoorSettingsDefaults SecurityDoorSettings { get; set; } = new();

    [JsonPropertyName("ZoneSettings")]
    public ZoneSettingsDefaults ZoneSettings { get; set; } = new();

    [JsonPropertyName("GeomorphSettings")]
    public GeomorphSettingsDefaults GeomorphSettings { get; set; } = new();

    [JsonPropertyName("SectorAndWardenObjectives")]
    public SectorAndWardenObjectivesDefaults SectorAndWardenObjectives { get; set; } = new();
}

public class LevelSettingsDialogDefaults
{
    public string Name { get; set; } = "My New Level";
    public bool InternalEnabled { get; set; } = true;
    public ComplexType ComplexType { get; set; } = ComplexType.Mining;
    public uint FogSettings { get; set; } = 0;
    public string DimensionGeomorph { get; set; } = string.Empty;
}

public class SecurityDoorSettingsDefaults
{
    public string DoorType { get; set; } = "Security";
    public bool IsSecurityDoor { get; set; } = true;
    public bool IsApexDoor { get; set; } = false;
    public uint ChainedPuzzleToSolve { get; set; } = 0;
    public ProgressionPuzzleType ProgressionPuzzleToEnter { get; set; } = ProgressionPuzzleType.None;
    public string ProgressionPuzzleCustomText { get; set; } = string.Empty;
    public int ProgressionPuzzlePlacementCount { get; set; } = 1;
    public List<CandidateZonePlacement> ProgressionPuzzleCandidateZones { get; set; } = new();
    public bool TurnOffAlarmOnTerminal { get; set; } = false;
    public int TerminalZone { get; set; } = 0;
    public int TerminalIndex { get; set; } = 0;
    public int AliasOverride { get; set; } = -1;
    public string AliasPrefixShortOverride { get; set; } = string.Empty;
    public bool HasBloodDoor { get; set; } = false;
    public List<EnemyGroupItem> EnemyGroupInFrontOfDoor { get; set; } = new();
    public List<EnemyGroupItem> EnemyGroupInArea { get; set; } = new();
    public DoorEventsDefaults DoorEvents { get; set; } = new();
}

public class DoorEventsDefaults
{
    public List<WardenEvent> EventsOnApproachDoor { get; set; } = new();
    public List<WardenEvent> EventsOnUnlockDoor { get; set; } = new();
    public List<WardenEvent> EventsOnOpenDoor { get; set; } = new();
    public List<WardenEvent> EventsOnPortalWarp { get; set; } = new();
}

public class ZoneSettingsDefaults
{
    [JsonPropertyName("GeneralAndSeeds")]
    public GeneralAndSeedsDefaults GeneralAndSeeds { get; set; } = new();

    [JsonPropertyName("ExpansionAndAltitude")]
    public ExpansionAndAltitudeDefaults ExpansionAndAltitude { get; set; } = new();

    [JsonPropertyName("EnemyRespawning")]
    public EnemyRespawningDefaults EnemyRespawning { get; set; } = new();

    [JsonPropertyName("ResourceContainers")]
    public ResourceContainersDefaults ResourceContainers { get; set; } = new();

    [JsonPropertyName("Terminals")]
    public TerminalsDefaults Terminals { get; set; } = new();

    [JsonPropertyName("Events")]
    public ZoneEventsDefaults Events { get; set; } = new();
}

public class GeneralAndSeedsDefaults
{
    public int SubSeed { get; set; } = 0;
    public int MarkerSubSeed { get; set; } = 0;
    public int LightingSubSeed { get; set; } = 0;
    public int DcScanSeed { get; set; } = 0;
    public int BulkheadDCScanSeed { get; set; } = 0;
    public int AliasOverride { get; set; } = -1;
    public bool OverrideAliasPrefix { get; set; } = false;
    public string SubComplex { get; set; } = "Lab";
}

public class ExpansionAndAltitudeDefaults
{
    public string AllowedZoneAltitude { get; set; } = "Unchanged";
    public double ChanceToChange { get; set; } = 0;
    public string StartPosition { get; set; } = "From_AverageCenter";
    public double StartPosition_IndexWeight { get; set; } = 0.0;
    public string StartExpansion { get; set; } = "Towards_Forward";
    public string ZoneExpansion { get; set; } = "Expansional";
    public uint LightSettings { get; set; } = 0;
}

public class EnemyRespawningDefaults
{
    public bool EnemyRespawning { get; set; } = false;
    public bool RequireOtherZone { get; set; } = false;
    public int RoomDistance { get; set; } = 2;
    public double TimeInterval { get; set; } = 10.0;
    public double CountMultiplier { get; set; } = 1.0;
    public List<EnemyExcludeItem> ExcludeList { get; set; } = new();
}

public class ResourceContainersDefaults
{
    public int MedKitUses { get; set; } = 0;
    public double MedKitPlacementStart { get; set; } = 0.0;
    public double MedKitPlacementMiddle { get; set; } = 0.0;
    public double MedKitPlacementEnd { get; set; } = 0.0;

    public int AmmoKitUses { get; set; } = 0;
    public double AmmoKitPlacementStart { get; set; } = 0.0;
    public double AmmoKitPlacementMiddle { get; set; } = 0.0;
    public double AmmoKitPlacementEnd { get; set; } = 0.0;

    public int ToolKitUses { get; set; } = 0;
    public double ToolKitPlacementStart { get; set; } = 0.0;
    public double ToolKitPlacementMiddle { get; set; } = 0.0;
    public double ToolKitPlacementEnd { get; set; } = 0.0;

    public int DisinfectKitUses { get; set; } = 0;
    public double DisinfectKitPlacementStart { get; set; } = 0.0;
    public double DisinfectKitPlacementMiddle { get; set; } = 0.0;
    public double DisinfectKitPlacementEnd { get; set; } = 0.0;

    public int PowerCells { get; set; } = 0;
    public int FogTurbines { get; set; } = 0;
}

public class TerminalsDefaults
{
    public double PlacementStartWeight { get; set; } = 0.33;
    public double PlacementMiddleWeight { get; set; } = 0.34;
    public double PlacementEndWeight { get; set; } = 0.33;
    public int AreaSeedOffset { get; set; } = 0;
    public int MarkerSeedOffset { get; set; } = 0;
    public TerminalStartingState StartingState { get; set; } = TerminalStartingState.Awake;
    public bool KeepShowingLocalLogCount { get; set; } = false;
    public bool UseCustomInfoText { get; set; } = false;
    public string CustomInfoText { get; set; } = string.Empty;
    public bool PasswordProtected { get; set; } = false;
    public bool GeneratePassword { get; set; } = false;
    public bool ShowPasswordLength { get; set; } = false;
    public bool ShowPasswordPartPositions { get; set; } = false;
    public string PasswordHintText { get; set; } = string.Empty;
    public int PasswordPartCount { get; set; } = 1;
    public List<TerminalZoneSelectionData> TerminalZoneSelectionDatas { get; set; } = new();
    public List<TerminalUniqueCommand> UniqueCommands { get; set; } = new();
}

public class ZoneEventsDefaults
{
    public List<WardenEvent> EventsOnEnter { get; set; } = new();
    public List<WardenEvent> EventsOnPortalWarp { get; set; } = new();
    public List<WardenEvent> EventsOnApproachDoor { get; set; } = new();
    public List<WardenEvent> EventsOnUnlockDoor { get; set; } = new();
    public List<WardenEvent> EventsOnOpenDoor { get; set; } = new();
}

public class GeomorphSettingsDefaults
{
    public GeomorphAltitude Altitude { get; set; } = GeomorphAltitude.Mid;
    public double RotationAngle { get; set; } = 0;
}

public class SectorAndWardenObjectivesDefaults
{
    [JsonPropertyName("SectorCommon")]
    public SectorCommonDefaults SectorCommon { get; set; } = new();

    [JsonPropertyName("ObjectiveTypes")]
    public Dictionary<string, object> ObjectiveTypes { get; set; } = new();
}

public class SectorCommonDefaults
{
    public int ZoneAliasStart { get; set; } = 100;
    public WinCondition WinCondition { get; set; } = WinCondition.GoToElevator;
    public int ExitZone { get; set; } = 0;
    public int BulkheadDoorZone { get; set; } = 0;
    public int BulkheadDCZone { get; set; } = 0;
    public string BulkheadKeysPlacement { get; set; } = "None";
    public List<object> BulkheadKeyZones { get; set; } = new();
    public uint ChainedPuzzleToActive { get; set; } = 0;
    public uint ChainedPuzzleMidObjective { get; set; } = 0;
    public double ArtifactAmountMultiplier { get; set; } = 1.0;
    public uint FogTransitionDataOnGotoWin { get; set; } = 0;
    public double FogTransitionDataOnGotoWinDuration { get; set; } = 0.0;
    public bool StopAllWavesBeforeGotoWin { get; set; } = false;
    public WaveOnGotoWinTrigger WaveOnGotoWinTrigger { get; set; } = WaveOnGotoWinTrigger.OnObjectiveCompleted;
}

public class AlarmsWindowDefaults
{
    public bool SpawnsEnemies { get; set; } = true;
    public List<uint> ChainedPuzzle { get; set; } = new() { 6 };

    [JsonPropertyName("WaveRoles")]
    public WaveRolesDefaults WaveRoles { get; set; } = new();

    [JsonPropertyName("PopulationFilters")]
    public PopulationFiltersDefaults PopulationFilters { get; set; } = new();

    [JsonPropertyName("WaveSettingsAndTiming")]
    public WaveSettingsAndTimingDefaults WaveSettingsAndTiming { get; set; } = new();
}

public class WaveRolesDefaults
{
    public uint WaveRoleWeakling { get; set; } = 21;
    public uint WaveRoleStandard { get; set; } = 13;
    public uint WaveRoleSpecial { get; set; } = 11;
    public uint WaveRoleMiniBoss { get; set; } = 16;
    public uint WaveRoleBoss { get; set; } = 18;
}

public class PopulationFiltersDefaults
{
    public bool FilterWeakling { get; set; } = true;
    public bool FilterStandard { get; set; } = false;
    public bool FilterSpecial { get; set; } = false;
    public bool FilterMiniBoss { get; set; } = false;
    public bool FilterBoss { get; set; } = false;
    public WaveFilterType FilterType { get; set; } = WaveFilterType.Exclude;
}

public class WaveSettingsAndTimingDefaults
{
    public string PublicAlarmName { get; set; } = "Class II Alarm";
    public double PauseBeforeStart { get; set; } = 3.0;
    public double PauseBetweenGroups { get; set; } = 5.0;
    public double WavePauseMin_AtCost { get; set; } = 3.0;
    public double WavePauseMax_AtCost { get; set; } = 10.0;
    public double WavePauseMin { get; set; } = 3.0;
    public double WavePauseMax { get; set; } = 30.0;
    public double ChanceToRandomizeSpawnDirectionPerWave { get; set; } = 1.0;
    public double ChanceToRandomizeSpawnDirectionPerGroup { get; set; } = 0.1;
    public bool OverrideWaveSpawnType { get; set; } = false;
    public SurvivalWaveSpawnType SurvivalWaveSpawnType { get; set; } = SurvivalWaveSpawnType.InRelationToClosestAlivePlayer;
    public int PopulationPointsTotal { get; set; } = -1;
    public double PopulationPointsPerWaveStart { get; set; } = 17.0;
    public double PopulationPointsPerWaveEnd { get; set; } = 25.0;
    public double PopulationPointsMinPerGroup { get; set; } = 5.0;
    public double PopulationPointsPerGroupStart { get; set; } = 5.0;
    public double PopulationPointsPerGroupEnd { get; set; } = 10.0;
    public double PopulationRampOverTime { get; set; } = 200.0;
}

public class LightingWindowDefaults
{
    [JsonPropertyName("LightBlockDefaults")]
    public LightBlockDefaults LightBlockDefaults { get; set; } = new();

    [JsonPropertyName("LightCategoryDefaults")]
    public Dictionary<string, LightCategorySettingItem> LightCategoryDefaults { get; set; } = new()
    {
        ["General"] = new LightCategorySettingItem { Category = LightCategory.General, On = true, Chance = 0.5, ChanceBroken = 0.0, IntensityMul = 0.7, Weight = 1.0, Color = new LightColor { R = 0.62, G = 0.69, B = 0.72, A = 0.0 } },
        ["Special"] = new LightCategorySettingItem { Category = LightCategory.Special, On = true, Chance = 0.5, ChanceBroken = 0.0, IntensityMul = 0.8, Weight = 1.0, Color = new LightColor { R = 0.75, G = 0.58, B = 0.47, A = 0.0 } },
        ["Emergency"] = new LightCategorySettingItem { Category = LightCategory.Emergency, On = true, Chance = 0.0, ChanceBroken = 0.0, IntensityMul = 1.0, Weight = 1.0, Color = new LightColor { R = 1.0, G = 0.27, B = 0.27, A = 0.0 } },
        ["Independent"] = new LightCategorySettingItem { Category = LightCategory.Independent, On = true, Chance = 0.5, ChanceBroken = 0.0, IntensityMul = 1.0, Weight = 0.8, Color = new LightColor { R = 0.74, G = 0.53, B = 0.51, A = 0.0 } },
        ["Door"] = new LightCategorySettingItem { Category = LightCategory.Door, On = true, Chance = 0.5, ChanceBroken = 0.0, IntensityMul = 1.0, Weight = 1.0, Color = new LightColor { R = 0.44, G = 0.65, B = 0.73, A = 0.0 } },
        ["Sign"] = new LightCategorySettingItem { Category = LightCategory.Sign, On = true, Chance = 1.0, ChanceBroken = 0.0, IntensityMul = 1.0, Weight = 1.0, Color = new LightColor { R = 0.44, G = 0.65, B = 0.73, A = 0.0 } },
        ["DoorImportant"] = new LightCategorySettingItem { Category = LightCategory.DoorImportant, On = true, Chance = 1.0, ChanceBroken = 0.0, IntensityMul = 1.0, Weight = 1.0, Color = new LightColor { R = 0.51, G = 0.64, B = 0.68, A = 0.0 } }
    };
}

public class LightBlockDefaults
{
    public string Name { get; set; } = "New Lighting Configuration";
    public bool InternalEnabled { get; set; } = true;
    public uint PersistentId { get; set; } = 1;
}

public class FogSettingsWindowDefaults
{
    [JsonPropertyName("FogBlockDefaults")]
    public FogBlockDefaults FogBlockDefaults { get; set; } = new();
}

public class FogBlockDefaults
{
    public string Name { get; set; } = "New Fog Setting";
    public bool InternalEnabled { get; set; } = true;
    public double FogDensity { get; set; } = 0.02;
    public LightColor FogColor { get; set; } = new() { R = 0.2, G = 0.3, B = 0.25, A = 1.0 };
    public Vector3Data DensityNoiseDirection { get; set; } = new(1.48, 0.0, 0.0);
    public double Infection { get; set; } = 0.0;
    public double DensityNoiseSpeed { get; set; } = 0.009;
    public double DensityNoiseScale { get; set; } = 0.06;
    public double DensityHeightAltitude { get; set; } = -0.91;
    public double DensityHeightRange { get; set; } = 1.01;
    public double DensityHeightMaxBoost { get; set; } = 0.09;
}

public class RundownWindowDefaults
{
    [JsonPropertyName("RundownSettings")]
    public RundownSettingsDefaults RundownSettings { get; set; } = new();

    [JsonPropertyName("Storytelling")]
    public StorytellingDefaults Storytelling { get; set; } = new();

    [JsonPropertyName("TierUnlockRequirements")]
    public Dictionary<string, TierRequirements> TierUnlockRequirements { get; set; } = new();

    [JsonPropertyName("ExpeditionDefaults")]
    public ExpeditionDefaults ExpeditionDefaults { get; set; } = new();
}

public class RundownSettingsDefaults
{
    public string Name { get; set; } = "New Rundown";
    public bool InternalEnabled { get; set; } = true;
    public bool NeverShowRundownTree { get; set; } = false;
    public bool UseTierUnlockRequirements { get; set; } = true;
}

public class StorytellingDefaults
{
    public string Title { get; set; } = "RUNDOWN EXAMPLE\nTITLE: EXAMPLE";
    public uint TextLog { get; set; } = 0;
    public RundownVisuals Visuals { get; set; } = new();
}

public class ExpeditionDefaults
{
    public ExpeditionAccessibility Accessibility { get; set; } = ExpeditionAccessibility.Normal;
    public uint LevelLayoutData { get; set; } = 1;
    public uint SecondaryLayout { get; set; } = 0;
    public uint ThirdLayout { get; set; } = 0;
    public uint FogSettings { get; set; } = 0;
    public uint LightSettings { get; set; } = 1;
    public uint ComplexResourceData { get; set; } = 1;
    public ExpeditionDescriptiveData Descriptive { get; set; } = new()
    {
        Prefix = "A",
        PublicName = "Expedition A",
        RoleplayedWardenIntel = string.Empty,
        ExpeditionDescription = string.Empty
    };
    public ExpeditionLayerData MainLayerData { get; set; } = new();
    public ExpeditionLayerData SecondaryLayerData { get; set; } = new();
    public ExpeditionLayerData ThirdLayerData { get; set; } = new();
}
