using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class RundownProjectFile
{
    [JsonPropertyName("fileVersion")] public int FileVersion { get; set; } = 1;

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modifiedAt")] public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("rundown")] public RundownBlock Rundown { get; set; } = new();

    [JsonPropertyName("levels")] public List<LevelLayoutBlockData> Levels { get; set; } = new();

    [JsonPropertyName("alarms")] public List<AlarmBlock> Alarms { get; set; } = new();

    [JsonPropertyName("lighting")] public List<LightSettingsBlock> Lighting { get; set; } = new();

    [JsonPropertyName("complexResourceSets")]
    public List<ComplexResourceSetBlock> ComplexResourceSets { get; set; } = new();

    [JsonPropertyName("fogSettings")] public List<FogSettingsBlock> FogSettings { get; set; } = new();
}

public class LevelLayoutBlockData
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("persistentID")] public uint PersistentId { get; set; }

    [JsonPropertyName("ComplexResourceSet")]
    public uint ComplexResourceSet { get; set; }

    [JsonPropertyName("ElevatorTile")] public string ElevatorTile { get; set; } = string.Empty;

    [JsonPropertyName("internalEnabled")] public bool InternalEnabled { get; set; } = true;

    [JsonPropertyName("Dimensions")] public List<DimensionLayoutBlock> Dimensions { get; set; } = new();

    [JsonPropertyName("mainSector")] public SectorSettingsData MainSector { get; set; } = new();

    [JsonPropertyName("secondarySector")] public SectorSettingsData SecondarySector { get; set; } = new();

    [JsonPropertyName("overloadSector")] public SectorSettingsData OverloadSector { get; set; } = new();

    [JsonPropertyName("gridState")] public LevelLayoutGridStateData GridState { get; set; } = new();
}

public class SectorSettingsData
{
    [JsonPropertyName("artifactAmountMultiplier")]
    public double ArtifactAmountMultiplier { get; set; } = 1.0;

    [JsonPropertyName("zoneAliasStart")] public int ZoneAliasStart { get; set; } = 0;

    [JsonPropertyName("winCondition")] public WinCondition WinCondition { get; set; } = WinCondition.GoToElevator;

    [JsonPropertyName("exitZone")] public int ExitZone { get; set; } = 0;

    [JsonPropertyName("objectiveType")]
    public ObjectiveType ObjectiveType { get; set; } = ObjectiveType.HSU_FindTakeSample;

    [JsonPropertyName("hsuPlacementZone")] public int HsuPlacementZone { get; set; } = 0;

    [JsonPropertyName("hsuPlacementStartWeight")]
    public double HsuPlacementStartWeight { get; set; } = 0.0;

    [JsonPropertyName("hsuPlacementMiddleWeight")]
    public double HsuPlacementMiddleWeight { get; set; } = 0.0;

    [JsonPropertyName("hsuPlacementEndWeight")]
    public double HsuPlacementEndWeight { get; set; } = 0.0;

    [JsonPropertyName("reactorPlacementZone")]
    public int ReactorPlacementZone { get; set; } = 0;

    [JsonPropertyName("reactorStartupStages")]
    public List<ReactorStartupStage>? ReactorStartupStages { get; set; }

    [JsonPropertyName("chainedPuzzleToActive")]
    public uint ChainedPuzzleToActive { get; set; } = 0;

    [JsonPropertyName("chainedPuzzleMidObjective")]
    public uint ChainedPuzzleMidObjective { get; set; } = 0;

    [JsonPropertyName("doNotSolveObjectiveOnReactorComplete")]
    public bool DoNotSolveObjectiveOnReactorComplete { get; set; } = false;

    [JsonPropertyName("specialTerminalCommandZone")]
    public int SpecialTerminalCommandZone { get; set; } = 0;

    [JsonPropertyName("specialTerminalCommand")]
    public string SpecialTerminalCommand { get; set; } = string.Empty;

    [JsonPropertyName("specialTerminalCommandDesc")]
    public string SpecialTerminalCommandDesc { get; set; } = string.Empty;

    [JsonPropertyName("postCommandOutput")]
    public string PostCommandOutput { get; set; } = string.Empty;

    [JsonPropertyName("specialCommandRule")]
    public SpecialCommandRule SpecialCommandRule { get; set; } = SpecialCommandRule.Normal;

    [JsonPropertyName("gatherItem")]
    public GatherSmallItemType GatherItem { get; set; } = GatherSmallItemType.PersonnelID;

    [JsonPropertyName("retrieveItem")]
    public RetrieveBigItemType RetrieveItem { get; set; } = RetrieveBigItemType.PowerCell;

    [JsonPropertyName("header")] public string Header { get; set; } = string.Empty;

    [JsonPropertyName("mainObjective")] public string MainObjective { get; set; } = string.Empty;

    [JsonPropertyName("findLocationInfo")] public string FindLocationInfo { get; set; } = string.Empty;

    [JsonPropertyName("findLocationInfoHelp")] public string FindLocationInfoHelp { get; set; } = string.Empty;

    [JsonPropertyName("goToZone")] public string GoToZone { get; set; } = string.Empty;

    [JsonPropertyName("inZoneFindItem")] public string InZoneFindItem { get; set; } = string.Empty;

    [JsonPropertyName("solveItem")] public string SolveItem { get; set; } = string.Empty;

    [JsonPropertyName("wavesOnGotoWin")] public GenericWaveSettings? WavesOnGotoWin { get; set; }

    [JsonPropertyName("waveOnGotoWinTrigger")]
    public WaveOnGotoWinTrigger WaveOnGotoWinTrigger { get; set; } = WaveOnGotoWinTrigger.OnObjectiveCompleted;

    [JsonPropertyName("stopAllWavesBeforeGotoWin")]
    public bool StopAllWavesBeforeGotoWin { get; set; } = false;

    [JsonPropertyName("wavesOnElevatorLand")]
    public GenericWaveSettings? WavesOnElevatorLand { get; set; }

    [JsonPropertyName("fogTransitionDataOnGotoWin")]
    public uint FogTransitionDataOnGotoWin { get; set; } = 0;

    [JsonPropertyName("fogTransitionDataOnGotoWinDuration")]
    public double FogTransitionDataOnGotoWinDuration { get; set; } = 0.0;

    [JsonPropertyName("gatherItemPlacements")]
    public List<GatherItemPlacement>? GatherItemPlacements { get; set; }

    [JsonPropertyName("gatherMaxPerZone")] public int GatherMaxPerZone { get; set; } = 0;

    [JsonPropertyName("retrieveItemPlacements")]
    public List<GatherItemPlacement>? RetrieveItemPlacements { get; set; }

    [JsonPropertyName("retrieveMaxPerZone")]
    public int RetrieveMaxPerZone { get; set; } = 0;

    [JsonPropertyName("distributeGeneratorPlacements")]
    public List<DistributeGeneratorPlacement>? DistributeGeneratorPlacements { get; set; }

    [JsonPropertyName("uplinkNumberOfVerificationRounds")]
    public int UplinkNumberOfVerificationRounds { get; set; } = 1;

    [JsonPropertyName("terminalUplinkPlacements")]
    public List<GatherItemPlacement>? TerminalUplinkPlacements { get; set; }

    [JsonPropertyName("centralGeneratorClusterZone")]
    public int CentralGeneratorClusterZone { get; set; } = 0;

    [JsonPropertyName("centralGeneratorClusterFogTransitions")]
    public List<CentralGeneratorFogTransitionStage>? CentralGeneratorClusterFogTransitions { get; set; }

    [JsonPropertyName("centralGeneratorClusterPowerCellPlacements")]
    public List<GatherItemPlacement>? CentralGeneratorClusterPowerCellPlacements { get; set; }

    [JsonPropertyName("activateSmallHsuZone")]
    public int ActivateSmallHsuZone { get; set; } = 0;

    [JsonPropertyName("itemFromStart")]
    public RetrieveBigItemType ItemFromStart { get; set; } = RetrieveBigItemType.PowerCell;

    [JsonPropertyName("itemAfterActivation")]
    public RetrieveBigItemType ItemAfterActivation { get; set; } = RetrieveBigItemType.PowerCell;

    [JsonPropertyName("bringItemInElevator")]
    public bool BringItemInElevator { get; set; } = false;

    [JsonPropertyName("stopEnemyWavesOnActivation")]
    public bool StopEnemyWavesOnActivation { get; set; } = false;

    [JsonPropertyName("objectiveCompleteAfterInsertion")]
    public bool ObjectiveCompleteAfterInsertion { get; set; } = false;

    [JsonPropertyName("requireItemAfterActivationInExitScan")]
    public bool RequireItemAfterActivationInExitScan { get; set; } = false;

    [JsonPropertyName("timeToActivate")] public int TimeToActivate { get; set; } = 0;

    [JsonPropertyName("timeToSurvive")] public int TimeToSurvive { get; set; } = 0;

    [JsonPropertyName("timerTitle")] public string TimerTitle { get; set; } = string.Empty;

    [JsonPropertyName("timerToActivateTitle")]
    public string TimerToActivateTitle { get; set; } = string.Empty;

    [JsonPropertyName("gatherTerminalSpawnCount")]
    public int GatherTerminalSpawnCount { get; set; } = 1;

    [JsonPropertyName("gatherTerminalRequiredCount")]
    public int GatherTerminalRequiredCount { get; set; } = 1;

    [JsonPropertyName("gatherTerminalCommand")]
    public string GatherTerminalCommand { get; set; } = string.Empty;

    [JsonPropertyName("gatherTerminalCommandHelp")]
    public string GatherTerminalCommandHelp { get; set; } = string.Empty;

    [JsonPropertyName("gatherTerminalDownloadingText")]
    public string GatherTerminalDownloadingText { get; set; } = string.Empty;

    [JsonPropertyName("gatherTerminalDownloadCompleteText")]
    public string GatherTerminalDownloadCompleteText { get; set; } = string.Empty;

    [JsonPropertyName("gatherTerminalDownloadTime")]
    public double GatherTerminalDownloadTime { get; set; } = 0.0;

    [JsonPropertyName("gatherTerminalPlacements")]
    public List<GatherItemPlacement>? GatherTerminalPlacements { get; set; }

    [JsonPropertyName("timedTerminalSequenceMainLocation")]
    public CandidateZonePlacement? TimedTerminalSequenceMainLocation { get; set; }

    [JsonPropertyName("timedTerminalSequenceNumberOfRounds")]
    public int TimedTerminalSequenceNumberOfRounds { get; set; } = 1;

    [JsonPropertyName("timedTerminalSequenceTimePerRound")]
    public double TimedTerminalSequenceTimePerRound { get; set; } = 0.0;

    [JsonPropertyName("timedTerminalSequenceTimeForConfirmation")]
    public double TimedTerminalSequenceTimeForConfirmation { get; set; } = 0.0;

    [JsonPropertyName("timedTerminalSequenceSourceTerminalWorldEventObjectFilter")]
    public string TimedTerminalSequenceSourceTerminalWorldEventObjectFilter { get; set; } = "MainTerminal";

    [JsonPropertyName("timedTerminalSequenceNumberOfTerminals")]
    public int TimedTerminalSequenceNumberOfTerminals { get; set; } = 1;

    [JsonPropertyName("timedTerminalSequencePlacements")]
    public List<GatherItemPlacement>? TimedTerminalSequencePlacements { get; set; }
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
}

public class ZoneSettingsData
{
    [JsonPropertyName("zoneIndex")] public int ZoneIndex { get; set; }

    [JsonPropertyName("lighting")] public uint Lighting { get; set; }

    [JsonPropertyName("medKitUses")] public int MedKitUses { get; set; }

    [JsonPropertyName("ammoKitUses")] public int AmmoKitUses { get; set; }

    [JsonPropertyName("toolKitUses")] public int ToolKitUses { get; set; }

    [JsonPropertyName("disinfectKitUses")] public int DisinfectKitUses { get; set; }

    [JsonPropertyName("bulkheadKeys")] public int BulkheadKeys { get; set; }

    [JsonPropertyName("containsDC")] public bool ContainsDC { get; set; }

    [JsonPropertyName("containsDisinfectStation")]
    public bool ContainsDisinfectStation { get; set; }

    [JsonPropertyName("powerCells")] public int PowerCells { get; set; }

    [JsonPropertyName("fogTurbines")] public int FogTurbines { get; set; }

    [JsonPropertyName("dcOpensSecondary")] public bool DcOpensSecondary { get; set; }

    [JsonPropertyName("dcOpensOverload")] public bool DcOpensOverload { get; set; }

    [JsonPropertyName("dcOpenedMainDoorIds")]
    public List<string> DcOpenedMainDoorIds { get; set; } = new();
}

public class RecentFileItem
{
    [JsonPropertyName("filePath")] public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("lastOpened")] public DateTime LastOpened { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public bool IsPlaceholder { get; set; }
}