using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class RundownProjectFile
{
    [JsonPropertyName("fileVersion")] public int FileVersion { get; set; } = 1;

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modifiedAt")] public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastExportDirectory")] public string? LastExportDirectory { get; set; }

    [JsonPropertyName("rundown")] public RundownBlock Rundown { get; set; } = new();

    [JsonPropertyName("levels")] public List<LevelLayoutBlockData> Levels { get; set; } = new();

    [JsonPropertyName("alarms")] public List<AlarmBlock> Alarms { get; set; } = new();

    [JsonPropertyName("lighting")] public List<LightSettingsBlock> Lighting { get; set; } = new();

    [JsonPropertyName("complexResourceSets")]
    public List<ComplexResourceSetBlock> ComplexResourceSets { get; set; } = new();

    [JsonPropertyName("fogSettings")] public List<FogSettingsBlock> FogSettings { get; set; } = new();

    [JsonPropertyName("consumableDistributions")]
    public List<ConsumableDistributionBlock> ConsumableDistributions { get; set; } = new();
}

public class LevelLayoutBlockData
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("persistentID")] public uint PersistentId { get; set; }

    [JsonPropertyName("complexType")] public ComplexType ComplexType { get; set; } = ComplexType.Mining;

    [JsonPropertyName("ComplexResourceSet")]
    public uint ComplexResourceSet { get; set; }

    [JsonPropertyName("ElevatorTile")] public string ElevatorTile { get; set; } = string.Empty;

    [JsonPropertyName("fogSettings")] public uint FogSettings { get; set; }

    [JsonPropertyName("internalEnabled")] public bool InternalEnabled { get; set; } = true;

    [JsonPropertyName("Dimensions")] public List<DimensionLayoutBlock> Dimensions { get; set; } = new();

    [JsonPropertyName("mainSector")] public SectorSettings MainSector { get; set; } = new() { ZoneAliasStart = 0 };

    [JsonPropertyName("secondarySector")] public SectorSettings SecondarySector { get; set; } = new() { ZoneAliasStart = 100 };

    [JsonPropertyName("overloadSector")] public SectorSettings OverloadSector { get; set; } = new() { ZoneAliasStart = 200 };

    [JsonPropertyName("expeditionBalance")] public ExpeditionBalanceSettings? ExpeditionBalance { get; set; }

    [JsonPropertyName("gridState")] public LevelLayoutGridStateData GridState { get; set; } = new();
}

public class LevelLayoutGridStateData
{
    [JsonPropertyName("gridSize")] public int GridSize { get; set; } = 40;

    [JsonPropertyName("cellDimension")] public double CellDimension { get; set; } = 64.0;

    [JsonPropertyName("altitudeOffset")] public double AltitudeOffset { get; set; } = 6.0;

    [JsonPropertyName("zoom")] public double Zoom { get; set; } = 4.0;

    [JsonPropertyName("panX")] public double PanX { get; set; } = 400.0;

    [JsonPropertyName("panY")] public double PanY { get; set; } = 300.0;

    [JsonPropertyName("currentStartingPrefab")]
    public string CurrentStartingPrefab { get; set; } = string.Empty;

    [JsonPropertyName("nextZoneIndex")] public int NextZoneIndex { get; set; } = 1;

    [JsonPropertyName("placedGeomorphs")] public List<PlacedGeomorphNodeData> PlacedGeomorphs { get; set; } = new();

    [JsonPropertyName("placedDoors")] public List<PlacedDoorNodeData> PlacedDoors { get; set; } = new();

    [JsonPropertyName("zoneSettings")] public List<ZoneSettingsData> ZoneSettings { get; set; } = new();
}

public class PlacedGeomorphNodeData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("prefabPath")] public string PrefabPath { get; set; } = string.Empty;

    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("gridX")] public int GridX { get; set; }

    [JsonPropertyName("gridY")] public int GridY { get; set; }

    [JsonPropertyName("rotationAngle")] public double RotationAngle { get; set; }

    [JsonPropertyName("altitude")] public GeomorphAltitude Altitude { get; set; } = GeomorphAltitude.Mid;

    [JsonPropertyName("altitudeOffset")] public double AltitudeOffset { get; set; } = 6.0;

    [JsonPropertyName("lighting")] public uint Lighting { get; set; }

    [JsonPropertyName("generatedRooms")] public List<int> GeneratedRooms { get; set; } = new();

    [JsonPropertyName("roomZones")] public Dictionary<int, int> RoomZones { get; set; } = new();

    [JsonPropertyName("roomSectors")] public Dictionary<int, SectorType> RoomSectors { get; set; } = new();
}

public class PlacedDoorNodeData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("geomorphId")] public string GeomorphId { get; set; } = string.Empty;

    [JsonPropertyName("expanderIndex")] public int ExpanderIndex { get; set; }

    [JsonPropertyName("areaFrom")] public int AreaFrom { get; set; }

    [JsonPropertyName("areaTo")] public int AreaTo { get; set; } = -1;

    [JsonPropertyName("localX")] public double LocalX { get; set; }

    [JsonPropertyName("localY")] public double LocalY { get; set; }

    [JsonPropertyName("worldX")] public double WorldX { get; set; }

    [JsonPropertyName("worldY")] public double WorldY { get; set; }

    [JsonPropertyName("direction")] public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("rotationAngle")] public double RotationAngle { get; set; }

    [JsonPropertyName("altitude")] public GeomorphAltitude Altitude { get; set; } = GeomorphAltitude.Mid;

    [JsonPropertyName("isSecurityDoor")] public bool IsSecurityDoor { get; set; }

    [JsonPropertyName("isMainDoor")] public bool IsMainDoor { get; set; }

    [JsonPropertyName("isSecondaryDoor")] public bool IsSecondaryDoor { get; set; }

    [JsonPropertyName("isOverloadDoor")] public bool IsOverloadDoor { get; set; }

    [JsonPropertyName("isConnected")] public bool IsConnected { get; set; }

    [JsonPropertyName("isBlocked")] public bool IsBlocked { get; set; }

    [JsonPropertyName("connectedDoorId")] public string? ConnectedDoorId { get; set; }

    [JsonPropertyName("chainedPuzzleToSolve")] public uint ChainedPuzzleToSolve { get; set; }

    [JsonPropertyName("aliasOverride")] public int AliasOverride { get; set; }

    [JsonPropertyName("aliasPrefixShortOverride")] public string AliasPrefixShortOverride { get; set; } = string.Empty;
    [JsonPropertyName("turnOffAlarmOnTerminal")] public bool TurnOffAlarmOnTerminal { get; set; }
    [JsonPropertyName("terminalZone")] public int TerminalZone { get; set; }
    [JsonPropertyName("terminalIndex")] public int TerminalIndex { get; set; }
    [JsonPropertyName("isApexDoor")] public bool IsApexDoor { get; set; }
    [JsonPropertyName("hasBloodDoor")] public bool HasBloodDoor { get; set; }
    [JsonPropertyName("progressionPuzzleToEnter")] public ProgressionPuzzleType ProgressionPuzzleToEnter { get; set; } = ProgressionPuzzleType.None;
    [JsonPropertyName("progressionPuzzleCustomText")] public string ProgressionPuzzleCustomText { get; set; } = string.Empty;
    [JsonPropertyName("progressionPuzzlePlacementCount")] public int ProgressionPuzzlePlacementCount { get; set; } = 1;
    [JsonPropertyName("progressionPuzzleCandidateZones")] public List<CandidateZonePlacement> ProgressionPuzzleCandidateZones { get; set; } = new();
    [JsonPropertyName("enemyGroupInArea")] public List<EnemyGroupItem> EnemyGroupInArea { get; set; } = new();
    [JsonPropertyName("enemyGroupInFrontOfDoor")] public List<EnemyGroupItem> EnemyGroupInFrontOfDoor { get; set; } = new();
    [JsonPropertyName("eventsOnPortalWarp")] public List<WardenEvent> EventsOnPortalWarp { get; set; } = new();
    [JsonPropertyName("eventsOnApproachDoor")] public List<WardenEvent> EventsOnApproachDoor { get; set; } = new();
    [JsonPropertyName("eventsOnUnlockDoor")] public List<WardenEvent> EventsOnUnlockDoor { get; set; } = new();
    [JsonPropertyName("eventsOnOpenDoor")] public List<WardenEvent> EventsOnOpenDoor { get; set; } = new();
    [JsonPropertyName("eventsOnDoorScanStart")] public List<WardenEvent> EventsOnDoorScanStart { get; set; } = new();
    [JsonPropertyName("eventsOnDoorScanDone")] public List<WardenEvent> EventsOnDoorScanDone { get; set; } = new();
    [JsonPropertyName("eventsOnBossDeath")] public List<WardenEvent> EventsOnBossDeath { get; set; } = new();
    [JsonPropertyName("eventsOnTrigger")] public List<WardenEvent> EventsOnTrigger { get; set; } = new();
}

public class ZoneSettingsData
{
    [JsonPropertyName("sector")] public SectorType Sector { get; set; } = SectorType.Main;

    [JsonPropertyName("zoneIndex")] public int ZoneIndex { get; set; }

    [JsonPropertyName("lighting")] public uint Lighting { get; set; }

    [JsonPropertyName("consumableDistributionInZone")] public uint ConsumableDistributionInZone { get; set; }

    [JsonPropertyName("subSeed")] public int SubSeed { get; set; }
    [JsonPropertyName("markerSubSeed")] public int MarkerSubSeed { get; set; }
    [JsonPropertyName("lightingSubSeed")] public int LightingSubSeed { get; set; }
    [JsonPropertyName("dcScanSeed")] public int DcScanSeed { get; set; }

    [JsonPropertyName("enemyRespawning")] public bool EnemyRespawning { get; set; }
    [JsonPropertyName("requireOtherZone")] public bool RequireOtherZone { get; set; } = true;
    [JsonPropertyName("roomDistance")] public int RoomDistance { get; set; } = 2;
    [JsonPropertyName("timeInterval")] public double TimeInterval { get; set; } = 10.0;
    [JsonPropertyName("countMultiplier")] public double CountMultiplier { get; set; } = 1.0;
    [JsonPropertyName("excludeList")] public List<EnemyExcludeItem> ExcludeList { get; set; } = new();

    [JsonPropertyName("medKitUses")] public int MedKitUses { get; set; }
    [JsonPropertyName("medKitPlacementStart")] public double MedKitPlacementStart { get; set; }
    [JsonPropertyName("medKitPlacementMiddle")] public double MedKitPlacementMiddle { get; set; }
    [JsonPropertyName("medKitPlacementEnd")] public double MedKitPlacementEnd { get; set; }

    [JsonPropertyName("ammoKitUses")] public int AmmoKitUses { get; set; }
    [JsonPropertyName("ammoKitPlacementStart")] public double AmmoKitPlacementStart { get; set; }
    [JsonPropertyName("ammoKitPlacementMiddle")] public double AmmoKitPlacementMiddle { get; set; }
    [JsonPropertyName("ammoKitPlacementEnd")] public double AmmoKitPlacementEnd { get; set; }

    [JsonPropertyName("toolKitUses")] public int ToolKitUses { get; set; }
    [JsonPropertyName("toolKitPlacementStart")] public double ToolKitPlacementStart { get; set; }
    [JsonPropertyName("toolKitPlacementMiddle")] public double ToolKitPlacementMiddle { get; set; }
    [JsonPropertyName("toolKitPlacementEnd")] public double ToolKitPlacementEnd { get; set; }

    [JsonPropertyName("disinfectKitUses")] public int DisinfectKitUses { get; set; }
    [JsonPropertyName("disinfectKitPlacementStart")] public double DisinfectKitPlacementStart { get; set; }
    [JsonPropertyName("disinfectKitPlacementMiddle")] public double DisinfectKitPlacementMiddle { get; set; }
    [JsonPropertyName("disinfectKitPlacementEnd")] public double DisinfectKitPlacementEnd { get; set; }

    [JsonPropertyName("containsDC")] public bool ContainsDC { get; set; }

    [JsonPropertyName("containsGenCluster")]
    public bool ContainsGenCluster { get; set; }

    [JsonPropertyName("powerCells")] public int PowerCells { get; set; }

    [JsonPropertyName("fogTurbines")] public int FogTurbines { get; set; }

    [JsonPropertyName("dcOpensSecondary")] public bool DcOpensSecondary { get; set; }

    [JsonPropertyName("dcOpensOverload")] public bool DcOpensOverload { get; set; }

    [JsonPropertyName("dcOpenedMainDoorIds")]
    public List<string> DcOpenedMainDoorIds { get; set; } = new();

    [JsonPropertyName("hsuClusters")] public int HSUClusters { get; set; }
    [JsonPropertyName("corpseClusters")] public int CorpseClusters { get; set; }
    [JsonPropertyName("resourceContainerClusters")] public int ResourceContainerClusters { get; set; }
    [JsonPropertyName("corpsesInZone")] public int CorpsesInZone { get; set; }
    [JsonPropertyName("groundSpawnersInZone")] public ZoneDistributionAmount GroundSpawnersInZone { get; set; } = ZoneDistributionAmount.None_0;
    [JsonPropertyName("hsusInZone")] public ZoneDistributionAmount HSUsInZone { get; set; } = ZoneDistributionAmount.None_0;
    [JsonPropertyName("deconUnitsInZone")] public ZoneDistributionAmount DeconUnitsInZone { get; set; } = ZoneDistributionAmount.None_0;
    [JsonPropertyName("bigPickupDistributionInZone")] public List<BigPickupDistributionItem> BigPickupDistributionInZone { get; set; } = new();
    [JsonPropertyName("disinfectionStationPlacements")] public List<DisinfectionStationPlacement> DisinfectionStationPlacements { get; set; } = new();
    [JsonPropertyName("terminalPlacements")] public List<TerminalPlacement> TerminalPlacements { get; set; } = new();
}

public class RecentFileItem
{
    [JsonPropertyName("filePath")] public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("lastOpened")] public DateTime LastOpened { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public bool IsPlaceholder { get; set; }
}