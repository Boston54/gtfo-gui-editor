using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

public class ExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DatablocksExportedCount { get; set; }
    public int LevelGenFilesExportedCount { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
}

public class PersistentIdAllocator
{
    private readonly HashSet<uint> _usedIds = new();
    private uint _nextOffset = 100000;

    public PersistentIdAllocator(uint startingOffset = 100000)
    {
        _nextOffset = startingOffset;
    }

    public void RegisterExisting(uint id)
    {
        if (id > 0)
        {
            _usedIds.Add(id);
        }
    }

    public void RegisterExisting(IEnumerable<uint> ids)
    {
        foreach (var id in ids)
        {
            RegisterExisting(id);
        }
    }

    public uint Allocate(uint? requestedId = null)
    {
        if (requestedId.HasValue && requestedId.Value > 0 && !_usedIds.Contains(requestedId.Value))
        {
            _usedIds.Add(requestedId.Value);
            return requestedId.Value;
        }

        while (_usedIds.Contains(_nextOffset) || _nextOffset == 0)
        {
            _nextOffset++;
        }

        uint allocated = _nextOffset++;
        _usedIds.Add(allocated);
        return allocated;
    }
}

public static class DatablockExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    public const string LGTunerAdvancedSchema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""title"": ""LevelGenerationOverride"",
  ""type"": ""object"",
  ""properties"": {
    ""$schema"": {
      ""type"": ""string""
    },
    ""ExtraComplexResourceToLoad"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/$defs/Complex""
      }
    },
    ""GeoOverrides"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/$defs/GeoOverride""
      }
    },
    ""LevelLayoutID"": {
      ""type"": ""integer"",
      ""format"": ""uint32"",
      ""minimum"": 0
    },
    ""ZoneOverrides"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/$defs/ZoneOverride""
      }
    }
  },
  ""required"": [
    ""LevelLayoutID"",
    ""ZoneOverrides"",
    ""GeoOverrides"",
    ""ExtraComplexResourceToLoad""
  ],
  ""$defs"": {
    ""AreaOverride"": {
      ""type"": ""object"",
      ""properties"": {
        ""LG_ExpanderID"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        },
        ""PreviousCellInternalAreaID"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        },
        ""PreviousCellPosition"": {
          ""$ref"": ""#/$defs/LGGridPosition""
        }
      },
      ""required"": [
        ""PreviousCellPosition"",
        ""PreviousCellInternalAreaID"",
        ""LG_ExpanderID""
      ]
    },
    ""Complex"": {
      ""type"": ""string"",
      ""enum"": [
        ""Mining"",
        ""Service"",
        ""Tech""
      ]
    },
    ""GeoOverride"": {
      ""type"": ""object"",
      ""properties"": {
        ""Altitude"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        },
        ""Geomorph"": {
          ""type"": ""string""
        },
        ""Position"": {
          ""$ref"": ""#/$defs/LGGridPosition""
        },
        ""Rotation"": {
          ""$ref"": ""#/$defs/GeoRotation""
        }
      },
      ""required"": [
        ""Position"",
        ""Geomorph"",
        ""Rotation"",
        ""Altitude""
      ]
    },
    ""GeoRotation"": {
      ""oneOf"": [
        {
          ""type"": ""string"",
          ""enum"": [
            ""Forwards"",
            ""Right"",
            ""Backwards"",
            ""Left""
          ]
        },
        {
          ""type"": ""integer""
        }
      ]
    },
    ""LGGridPosition"": {
      ""type"": ""object"",
      ""properties"": {
        ""x"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        },
        ""z"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        }
      },
      ""required"": [
        ""x"",
        ""z""
      ]
    },
    ""ZoneOverride"": {
      ""type"": ""object"",
      ""properties"": {
        ""AreaOverrides"": {
          ""type"": ""array"",
          ""items"": {
            ""$ref"": ""#/$defs/AreaOverride""
          }
        }
      },
      ""required"": [
        ""AreaOverrides""
      ]
    }
  }
}";

    public static ExportResult ExportAll(MainWindowViewModel mainVm, string exportRootPath)
    {
        if (string.IsNullOrWhiteSpace(exportRootPath))
        {
            return new ExportResult { Success = false, Message = "Export root directory cannot be empty." };
        }

        try
        {
            string lgTunerDir = Path.Combine(exportRootPath, "Custom", "LGTunerAdvanced");
            string schemaDir = Path.Combine(exportRootPath, ".schemas");

            Directory.CreateDirectory(exportRootPath);
            Directory.CreateDirectory(lgTunerDir);
            Directory.CreateDirectory(schemaDir);

            // Write schema file in .schemas
            File.WriteAllText(Path.Combine(schemaDir, "LGTunerAdvanced.json"), LGTunerAdvancedSchema);

            // Allocators per datablock type
            var textAlloc = new PersistentIdAllocator(100000);
            var enemyGroupAlloc = new PersistentIdAllocator(100000);
            var survivalWaveSettingsAlloc = new PersistentIdAllocator(100000);
            var survivalWavePopulationAlloc = new PersistentIdAllocator(100000);
            var chainedPuzzleAlloc = new PersistentIdAllocator(100000);
            var wardenObjectiveAlloc = new PersistentIdAllocator(100000);
            var dimensionAlloc = new PersistentIdAllocator(100000);
            var levelLayoutAlloc = new PersistentIdAllocator(100000);
            var lightSettingsAlloc = new PersistentIdAllocator(100000);
            var fogSettingsAlloc = new PersistentIdAllocator(100000);
            var complexResourceAlloc = new PersistentIdAllocator(100000);
            var rundownAlloc = new PersistentIdAllocator(100000);

            // 1. Load vanilla blocks to preserve/reference
            var vanillaTextBlocks = LoadVanillaBlocksArray("TextDataBlock.json");
            RegisterExistingIds(vanillaTextBlocks, textAlloc);

            var vanillaEnemyBlocks = LoadVanillaBlocksArray("EnemyDataBlock.json");
            var vanillaChainedPuzzleTypeBlocks = LoadVanillaBlocksArray("ChainedPuzzleTypeDataBlock.json");
            var vanillaExpeditionBalanceBlocks = LoadVanillaBlocksArray("ExpeditionBalanceDataBlock.json");

            var vanillaChainedPuzzleBlocks = LoadVanillaBlocksArray("ChainedPuzzleDataBlock.json");
            RegisterExistingIds(vanillaChainedPuzzleBlocks, chainedPuzzleAlloc);

            var vanillaEnemyGroupBlocks = LoadVanillaBlocksArray("EnemyGroupDataBlock.json");
            RegisterExistingIds(vanillaEnemyGroupBlocks, enemyGroupAlloc);

            var vanillaSurvivalWaveSettingsBlocks = LoadVanillaBlocksArray("SurvivalWaveSettingsDataBlock.json");
            RegisterExistingIds(vanillaSurvivalWaveSettingsBlocks, survivalWaveSettingsAlloc);

            var vanillaSurvivalWavePopulationBlocks = LoadVanillaBlocksArray("SurvivalWavePopulationDataBlock.json");
            RegisterExistingIds(vanillaSurvivalWavePopulationBlocks, survivalWavePopulationAlloc);

            // State containers for generated datablocks
            var textBlocks = new List<JsonObject>(vanillaTextBlocks);
            var enemyGroupBlocks = new List<JsonObject>();
            var survivalWaveSettingsBlocks = new List<JsonObject>();
            var survivalWavePopulationBlocks = new List<JsonObject>();
            var chainedPuzzleBlocks = new List<JsonObject>(vanillaChainedPuzzleBlocks);
            var wardenObjectiveBlocks = new List<JsonObject>();
            var levelLayoutBlocks = new List<JsonObject>();
            var dimensionBlocks = new List<JsonObject>();

            // Enemy info cache for blood door score calculation
            var enemyCosts = LoadEnemyCosts(vanillaEnemyBlocks);

            // Helper text synthesis function
            uint SynthesizeText(string? text, string description = "")
            {
                if (string.IsNullOrWhiteSpace(text)) return 0;
                if (uint.TryParse(text, out uint existingId) && existingId > 0)
                {
                    return existingId;
                }

                uint newId = textAlloc.Allocate();
                var textObj = new JsonObject
                {
                    ["SkipLocalization"] = true,
                    ["MachineTranslation"] = false,
                    ["English"] = text,
                    ["Description"] = description,
                    ["CharacterMetaData"] = 1,
                    ["ExportVersion"] = 1,
                    ["ImportVersion"] = 1,
                    ["name"] = $"CustomText_{newId}",
                    ["internalEnabled"] = true,
                    ["persistentID"] = newId
                };
                textBlocks.Add(textObj);
                return newId;
            }

            // Helper Blood Door group synthesis function
            var bloodGroupCache = new Dictionary<string, uint>();

            uint SynthesizeEnemyGroup(IEnumerable<EnemyGroupItem> items, string groupSuffix)
            {
                var itemList = items?.Where(i => i != null && i.Count > 0).ToList() ?? new List<EnemyGroupItem>();
                if (itemList.Count == 0) return 0;

                string sig = string.Join(";", itemList.OrderBy(i => i.EnemyId).Select(i => $"{i.EnemyId}:{i.Count}"));
                if (bloodGroupCache.TryGetValue(sig, out uint cachedId))
                {
                    return cachedId;
                }

                double totalScore = 0;
                foreach (var item in itemList)
                {
                    double cost = enemyCosts.TryGetValue(item.EnemyId, out double c) ? c : 1.0;
                    totalScore += item.Count * cost;
                }

                if (totalScore <= 0) totalScore = 1.0;

                var rolesArray = new JsonArray();
                foreach (var item in itemList)
                {
                    double cost = enemyCosts.TryGetValue(item.EnemyId, out double c) ? c : 1.0;
                    double itemScore = item.Count * cost;
                    string dist = "Rel_100";

                    if (itemList.Count == 1 && item.Count == 1)
                    {
                        dist = "Force_One";
                    }
                    else
                    {
                        double prop = itemScore / totalScore;
                        dist = MapToDistributionEnum(prop);
                    }

                    string roleName = GetEnemyRoleName(item.EnemyId);
                    rolesArray.Add(new JsonObject
                    {
                        ["Role"] = roleName,
                        ["Distribution"] = dist
                    });
                }

                uint newGroupId = enemyGroupAlloc.Allocate();
                var groupObj = new JsonObject
                {
                    ["Type"] = "Hunter",
                    ["Difficulty"] = "Easy",
                    ["SpawnPlacementType"] = "Default",
                    ["MaxScore"] = totalScore,
                    ["ScoreInAreaPaddingMulti"] = 1.0,
                    ["RelativeWeight"] = 1.0,
                    ["Roles"] = rolesArray,
                    ["name"] = $"Generated_BloodDoor_{groupSuffix}_{newGroupId}",
                    ["internalEnabled"] = true,
                    ["persistentID"] = newGroupId
                };

                enemyGroupBlocks.Add(groupObj);
                bloodGroupCache[sig] = newGroupId;
                return newGroupId;
            }

            // 2. Synthesize Alarms (SurvivalWaveSettings, SurvivalWavePopulation, ChainedPuzzle)
            var alarmMapping = new Dictionary<uint, uint>(); // Alarm persistentId -> ChainedPuzzle persistentId
            foreach (var alarm in mainVm.Alarms.Alarms)
            {
                if (alarm == null) continue;

                uint popId = 0;
                uint waveSettingsId = 0;

                if (alarm.SpawnsEnemies)
                {
                    popId = survivalWavePopulationAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
                    var popObj = new JsonObject
                    {
                        ["WaveRoleWeakling"] = alarm.WaveRoleWeakling,
                        ["WaveRoleStandard"] = alarm.WaveRoleStandard,
                        ["WaveRoleSpecial"] = alarm.WaveRoleSpecial,
                        ["WaveRoleMiniBoss"] = alarm.WaveRoleMiniBoss,
                        ["WaveRoleBoss"] = alarm.WaveRoleBoss,
                        ["name"] = string.IsNullOrWhiteSpace(alarm.Name)
                            ? $"SurvivalWavePopulation_{popId}"
                            : $"{alarm.Name}_Pop",
                        ["internalEnabled"] = true,
                        ["persistentID"] = popId
                    };
                    survivalWavePopulationBlocks.Add(popObj);

                    var popFilterArray = new JsonArray();
                    if (alarm.FilterWeakling) popFilterArray.Add("Weakling");
                    if (alarm.FilterStandard) popFilterArray.Add("Standard");
                    if (alarm.FilterSpecial) popFilterArray.Add("Special");
                    if (alarm.FilterMiniBoss) popFilterArray.Add("MiniBoss");
                    if (alarm.FilterBoss) popFilterArray.Add("Boss");

                    waveSettingsId =
                        survivalWaveSettingsAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
                    var waveObj = new JsonObject
                    {
                        ["m_pauseBeforeStart"] = alarm.PauseBeforeStart,
                        ["m_pauseBetweenGroups"] = alarm.PauseBetweenGroups,
                        ["m_wavePauseMin_atCost"] = alarm.WavePauseMin_AtCost,
                        ["m_wavePauseMax_atCost"] = alarm.WavePauseMax_AtCost,
                        ["m_wavePauseMin"] = alarm.WavePauseMin,
                        ["m_wavePauseMax"] = alarm.WavePauseMax,
                        ["m_populationFilter"] = popFilterArray,
                        ["m_filterType"] = alarm.FilterType.ToString(),
                        ["m_chanceToRandomizeSpawnDirectionPerWave"] = alarm.ChanceToRandomizeSpawnDirectionPerWave,
                        ["m_chanceToRandomizeSpawnDirectionPerGroup"] = alarm.ChanceToRandomizeSpawnDirectionPerGroup,
                        ["m_overrideWaveSpawnType"] = alarm.OverrideWaveSpawnType,
                        ["m_survivalWaveSpawnType"] = alarm.SurvivalWaveSpawnType.ToString(),
                        ["m_populationPointsTotal"] = alarm.PopulationPointsTotal,
                        ["m_populationPointsPerWaveStart"] = alarm.PopulationPointsPerWaveStart,
                        ["m_populationPointsPerWaveEnd"] = alarm.PopulationPointsPerWaveEnd,
                        ["m_populationPointsMinPerGroup"] = alarm.PopulationPointsMinPerGroup,
                        ["m_populationPointsPerGroupStart"] = alarm.PopulationPointsPerGroupStart,
                        ["m_populationPointsPerGroupEnd"] = alarm.PopulationPointsPerGroupEnd,
                        ["m_populationRampOverTime"] = alarm.PopulationRampOverTime,
                        ["name"] = string.IsNullOrWhiteSpace(alarm.Name)
                            ? $"SurvivalWaveSettings_{waveSettingsId}"
                            : $"{alarm.Name}_Settings",
                        ["internalEnabled"] = true,
                        ["persistentID"] = waveSettingsId
                    };
                    survivalWaveSettingsBlocks.Add(waveObj);
                }

                var puzzleSteps = new JsonArray();
                if (alarm.ChainedPuzzle != null && alarm.ChainedPuzzle.Count > 0)
                {
                    foreach (var step in alarm.ChainedPuzzle)
                    {
                        puzzleSteps.Add(new JsonObject { ["PuzzleType"] = step.PuzzleType });
                    }
                }
                else
                {
                    puzzleSteps.Add(new JsonObject { ["PuzzleType"] = 6 });
                }

                uint puzzleId = chainedPuzzleAlloc.Allocate(alarm.PersistentId > 0 ? alarm.PersistentId : null);
                var puzzleObj = new JsonObject
                {
                    ["PublicAlarmName"] = string.IsNullOrWhiteSpace(alarm.PublicAlarmName)
                        ? "Alarm"
                        : alarm.PublicAlarmName,
                    ["TriggerAlarmOnActivate"] = alarm.SpawnsEnemies,
                    ["SurvivalWaveSettings"] = waveSettingsId,
                    ["SurvivalWavePopulation"] = popId,
                    ["SurvivalWaveAreaDistance"] = 2,
                    ["DisableSurvivalWaveOnComplete"] = true,
                    ["UseRandomPositions"] = false,
                    ["WantedDistanceFromStartPos"] = 0.0,
                    ["WantedDistanceBetweenPuzzleComponents"] = 1.0,
                    ["ChainedPuzzle"] = puzzleSteps,
                    ["OnlyShowHUDWhenPlayerIsClose"] = false,
                    ["AlarmSoundStart"] = 3339129407,
                    ["AlarmSoundStop"] = 42633153,
                    ["name"] = string.IsNullOrWhiteSpace(alarm.Name) ? $"ChainedPuzzle_{puzzleId}" : alarm.Name,
                    ["internalEnabled"] = true,
                    ["persistentID"] = puzzleId
                };
                chainedPuzzleBlocks.Add(puzzleObj);
                alarmMapping[alarm.PersistentId] = puzzleId;
            }

            // 3. Compile Level Layouts and Warden Objectives
            int levelGenFilesCount = 0;
            var levelMainObjectiveId = new Dictionary<uint, uint>();
            var levelMainObjectiveIds = new Dictionary<uint, List<uint>>();
            var levelSecondaryObjectiveId = new Dictionary<uint, uint>();
            var levelThirdObjectiveId = new Dictionary<uint, uint>();
            var levelSecondaryLayoutId = new Dictionary<uint, uint>();
            var levelThirdLayoutId = new Dictionary<uint, uint>();

            foreach (var level in mainVm.LevelLayout.Levels)
            {
                if (level == null) continue;

                var gridState = level.GridState ?? new LevelLayoutGridState();

                // Build tree of room connections via BFS starting from root geomorph (at 0,0 or first placed)
                var roomParentInfo =
                    new Dictionary<(string geoId, int areaIdx), (int prevX, int prevZ, int prevArea, int expanderId)>();
                if (gridState.PlacedGeomorphs.Count > 0)
                {
                    var rootGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == 0 && g.GridY == 0) ??
                                  gridState.PlacedGeomorphs[0];
                    var visitedRooms = new HashSet<(string geoId, int areaIdx)>();
                    var queue = new Queue<(PlacedGeomorphNode geo, int areaIdx)>();

                    var rootKey = (rootGeo.Id, 0);
                    visitedRooms.Add(rootKey);
                    roomParentInfo[rootKey] = (rootGeo.GridX, -rootGeo.GridY, 0, 0);
                    queue.Enqueue((rootGeo, 0));

                    while (queue.Count > 0)
                    {
                        var (currGeo, currArea) = queue.Dequeue();

                        // Internal doors in same geomorph
                        foreach (var d in gridState.PlacedDoors.Where(d =>
                                     d.IsInternal && d.Geomorph == currGeo && (d.IsConnected || d.IsZoneBoundary)))
                        {
                            int neighborArea = -1;
                            if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                            else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                            if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                            {
                                var neighborKey = (currGeo.Id, neighborArea);
                                if (!visitedRooms.Contains(neighborKey))
                                {
                                    visitedRooms.Add(neighborKey);

                                    // Resolve the expander index on currArea leading to neighborArea
                                    int expanderId = -1;
                                    if (currGeo.Metadata?.AreaDatas != null && currArea >= 0 &&
                                        currArea < currGeo.Metadata.AreaDatas.Count)
                                    {
                                        var expanders = currGeo.Metadata.AreaDatas[currArea].ExpanderDatas;
                                        if (expanders != null)
                                        {
                                            expanderId = expanders.FindIndex(e => e.AreaTo == neighborArea);
                                        }
                                    }

                                    if (expanderId < 0)
                                    {
                                        expanderId = d.ExpanderIndex;
                                    }

                                    roomParentInfo[neighborKey] = (currGeo.GridX, -currGeo.GridY, currArea,
                                        expanderId);
                                    queue.Enqueue((currGeo, neighborArea));
                                }
                            }
                        }

                        // External doors to adjacent geomorphs
                        foreach (var d in gridState.PlacedDoors.Where(d =>
                                     !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea &&
                                     d.ConnectedDoor != null &&
                                     (d.IsConnected || d.IsZoneBoundary || d.ConnectedDoor.IsZoneBoundary)))
                        {
                            var otherDoor = d.ConnectedDoor!;
                            var otherGeo = otherDoor.Geomorph;
                            if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                            {
                                var neighborKey = (otherGeo.Id, otherDoor.AreaFrom);
                                if (!visitedRooms.Contains(neighborKey))
                                {
                                    visitedRooms.Add(neighborKey);
                                    roomParentInfo[neighborKey] = (currGeo.GridX, -currGeo.GridY, d.AreaFrom,
                                        d.ExpanderIndex);
                                    queue.Enqueue((otherGeo, otherDoor.AreaFrom));
                                }
                            }
                        }
                    }
                }

                // Compile Warden Objectives for sectors
                var mainObjIds = new List<uint>();
                if (level.MainSector != null && level.MainSector.Objectives.Count > 0)
                {
                    foreach (var obj in level.MainSector.Objectives)
                    {
                        uint objId = wardenObjectiveAlloc.Allocate();
                        wardenObjectiveBlocks.Add(CompileWardenObjective(obj, objId, SynthesizeText));
                        mainObjIds.Add(objId);
                    }
                }
                else
                {
                    uint objId = wardenObjectiveAlloc.Allocate();
                    wardenObjectiveBlocks.Add(CompileWardenObjective(level.MainSector?.PrimaryObjective ?? new WardenObjective(), objId, SynthesizeText));
                    mainObjIds.Add(objId);
                }

                levelMainObjectiveIds[level.PersistentId] = mainObjIds;
                levelMainObjectiveId[level.PersistentId] = mainObjIds[0];

                if (level.SecondarySector != null)
                {
                    uint secObjId = wardenObjectiveAlloc.Allocate();
                    wardenObjectiveBlocks.Add(CompileWardenObjective(level.SecondarySector.PrimaryObjective, secObjId, SynthesizeText));
                    levelSecondaryObjectiveId[level.PersistentId] = secObjId;
                }

                if (level.OverloadSector != null)
                {
                    uint thirdObjId = wardenObjectiveAlloc.Allocate();
                    wardenObjectiveBlocks.Add(CompileWardenObjective(level.OverloadSector.PrimaryObjective, thirdObjId, SynthesizeText));
                    levelThirdObjectiveId[level.PersistentId] = thirdObjId;
                }

                // Determine sectors to export for this level
                var sectorConfigs = new List<(SectorType Sector, uint LayoutId, string Name, SectorSettings? Settings, List<int> Zones, bool IsMain)>();

                uint mainLayoutId = level.PersistentId > 0 ? level.PersistentId : levelLayoutAlloc.Allocate();
                levelLayoutAlloc.RegisterExisting(mainLayoutId);
                var mainZones = gridState.GetZonesForSector(SectorType.Main);
                if (mainZones.Count == 0) mainZones.Add(0);
                sectorConfigs.Add((SectorType.Main, mainLayoutId, level.Name, level.MainSector, mainZones, true));

                var secZones = gridState.GetZonesForSector(SectorType.Secondary);
                bool secExists = secZones.Count > 0 || level.HasSecondaryDoor || gridState.HasSecondaryDoor ||
                                 (mainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.SecondaryLayerEnabled) == true);
                if (secExists)
                {
                    uint secLayoutId = levelLayoutAlloc.Allocate();
                    levelSecondaryLayoutId[level.PersistentId] = secLayoutId;
                    if (secZones.Count == 0) secZones.Add(0);
                    sectorConfigs.Add((SectorType.Secondary, secLayoutId, $"{level.Name}_Secondary", level.SecondarySector, secZones, false));
                }

                var thirdZones = gridState.GetZonesForSector(SectorType.Overload);
                bool thirdExists = thirdZones.Count > 0 || level.HasOverloadDoor || gridState.HasOverloadDoor ||
                                   (mainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.ThirdLayerEnabled) == true);
                if (thirdExists)
                {
                    uint thirdLayoutId = levelLayoutAlloc.Allocate();
                    levelThirdLayoutId[level.PersistentId] = thirdLayoutId;
                    if (thirdZones.Count == 0) thirdZones.Add(0);
                    sectorConfigs.Add((SectorType.Overload, thirdLayoutId, $"{level.Name}_Third", level.OverloadSector, thirdZones, false));
                }

                // Build LevelLayoutDataBlock entries and LevelGen override configs for each sector
                foreach (var sectorCfg in sectorConfigs)
                {
                    var sectorZones = sectorCfg.Zones;
                    var sectorZonesSet = sectorZones.ToHashSet();
                    var zonesArray = new JsonArray();
                    var zoneOverridesList = new List<JsonObject>();

                    for (int zIdx = 0; zIdx < sectorZones.Count; zIdx++)
                    {
                        int zoneNumber = sectorZones[zIdx];
                        var zoneSettings = gridState.GetOrCreateZoneSettings(sectorCfg.Sector, zoneNumber);

                        // Find entering door for blood door / security settings
                        var enteringDoor = gridState.PlacedDoors.FirstOrDefault(d =>
                            d.Geomorph != null &&
                            d.Geomorph.GeneratedRooms.Any(a => d.Geomorph.GetRoomSector(a) == sectorCfg.Sector && d.Geomorph.GetRoomZone(a) == zoneNumber) &&
                            d.IsConnected);

                        var activeEnemyWaveObj = new JsonObject
                        {
                            ["HasActiveEnemyWave"] = false,
                            ["EnemyGroupInfrontOfDoor"] = 0,
                            ["EnemyGroupInArea"] = 0,
                            ["EnemyGroupsInArea"] = 1
                        };

                        uint chainedPuzzleToEnter = 0;
                        string securityGateToEnter = "Security";

                        var progPuzzleObj = new JsonObject
                        {
                            ["PuzzleType"] = "None",
                            ["CustomText"] = 0,
                            ["PlacementCount"] = 1,
                            ["ZonePlacementData"] = new JsonArray()
                        };

                        if (enteringDoor != null)
                        {
                            if (enteringDoor.HasBloodDoor)
                            {
                                uint frontId = SynthesizeEnemyGroup(enteringDoor.EnemyGroupInFrontOfDoor,
                                    $"InFront_{sectorCfg.LayoutId}_Z{zoneNumber}");
                                uint areaId = SynthesizeEnemyGroup(enteringDoor.EnemyGroupInArea,
                                    $"InArea_{sectorCfg.LayoutId}_Z{zoneNumber}");

                                activeEnemyWaveObj["HasActiveEnemyWave"] = true;
                                activeEnemyWaveObj["EnemyGroupInfrontOfDoor"] = frontId;
                                activeEnemyWaveObj["EnemyGroupInArea"] = areaId;
                                activeEnemyWaveObj["EnemyGroupsInArea"] = 1;
                            }

                            if (enteringDoor.ProgressionPuzzleToEnter != ProgressionPuzzleType.None)
                            {
                                progPuzzleObj["PuzzleType"] = enteringDoor.ProgressionPuzzleToEnter.ToString();
                                progPuzzleObj["CustomText"] =
                                    !string.IsNullOrWhiteSpace(enteringDoor.ProgressionPuzzleCustomText)
                                        ? SynthesizeText(enteringDoor.ProgressionPuzzleCustomText,
                                            "Progression Puzzle Custom Text")
                                        : 0;
                                progPuzzleObj["PlacementCount"] = enteringDoor.ProgressionPuzzleToEnter ==
                                                                  ProgressionPuzzleType.PowerGenerator_And_PowerCell
                                    ? enteringDoor.ProgressionPuzzlePlacementCount
                                    : 1;

                                var zonePlacementArray = new JsonArray();
                                if (enteringDoor.ProgressionPuzzleToEnter == ProgressionPuzzleType.Keycard_SecurityBox ||
                                    enteringDoor.ProgressionPuzzleToEnter ==
                                    ProgressionPuzzleType.PowerGenerator_And_PowerCell)
                                {
                                    foreach (var cz in enteringDoor.ProgressionPuzzleCandidateZones)
                                    {
                                        if (cz == null) continue;
                                        zonePlacementArray.Add(new JsonObject
                                        {
                                            ["DimensionIndex"] = string.IsNullOrEmpty(cz.DimensionIndex)
                                                ? "Reality"
                                                : cz.DimensionIndex,
                                            ["LocalIndex"] = $"Zone_{cz.Zone}",
                                            ["Weights"] = new JsonObject
                                            {
                                                ["Start"] = cz.PlacementStartWeight,
                                                ["Middle"] = cz.PlacementMiddleWeight,
                                                ["End"] = cz.PlacementEndWeight
                                            }
                                        });
                                    }
                                }

                                progPuzzleObj["ZonePlacementData"] = zonePlacementArray;
                            }

                            if (enteringDoor.IsSecurityDoor)
                            {
                                securityGateToEnter = enteringDoor.IsApexDoor ? "Apex" : "Security";
                                if (enteringDoor.ChainedPuzzleToSolve > 0)
                                {
                                    chainedPuzzleToEnter =
                                        alarmMapping.TryGetValue(enteringDoor.ChainedPuzzleToSolve, out uint cpId)
                                            ? cpId
                                            : enteringDoor.ChainedPuzzleToSolve;
                                }
                            }
                        }

                        // Terminal placements preserving ordering and custom text synthesis
                        var terminalsArray = new JsonArray();
                        foreach (var tp in zoneSettings.TerminalPlacements)
                        {
                            if (tp == null) continue;
                            var zoneSelections = new JsonArray();
                            foreach (var zs in tp.TerminalZoneSelectionDatas)
                            {
                                zoneSelections.Add(new JsonObject
                                {
                                    ["Zone"] = zs.Zone,
                                    ["Terminal"] = zs.Terminal
                                });
                            }

                            var uniqueCommands = new JsonArray();
                            foreach (var uc in tp.UniqueCommands)
                            {
                                var cmdObj = new JsonObject
                                {
                                    ["Command"] = uc.Command,
                                    ["CommandDesc"] = SynthesizeText(uc.CommandDesc, "Terminal Command Desc")
                                };
                                var postOutputs = new JsonArray();
                                foreach (var po in uc.PostCommandOutputs)
                                {
                                    postOutputs.Add(new JsonObject
                                    {
                                        ["LineType"] = po.LineType.ToString(),
                                        ["Time"] = po.Time,
                                        ["Output"] = SynthesizeText(po.Output, "Post Command Output")
                                    });
                                }

                                cmdObj["PostCommandOutputs"] = postOutputs;
                                uniqueCommands.Add(cmdObj);
                            }

                            var startingStateData = new JsonObject
                            {
                                ["StartingState"] = tp.StartingState.ToString(),
                                ["UseCustomInfoText"] = tp.UseCustomInfoText,
                                ["CustomInfoText"] = tp.UseCustomInfoText
                                    ? SynthesizeText(tp.CustomInfoText, "Terminal Custom Info")
                                    : 0,
                                ["KeepShowingLocalLogCount"] = tp.KeepShowingLocalLogCount,
                                ["AudioEventEnter"] = 0,
                                ["AudioEventExit"] = 0,
                                ["PasswordProtected"] = tp.PasswordProtected,
                                ["PasswordHintText"] = !string.IsNullOrWhiteSpace(tp.PasswordHintText)
                                    ? tp.PasswordHintText
                                    : "Password Required.",
                                ["GeneratePassword"] = tp.GeneratePassword,
                                ["PasswordPartCount"] = tp.PasswordPartCount > 0 ? tp.PasswordPartCount : 1,
                                ["ShowPasswordLength"] = tp.ShowPasswordLength,
                                ["ShowPasswordPartPositions"] = tp.ShowPasswordPartPositions,
                                ["TerminalZoneSelectionDatas"] = zoneSelections
                            };

                            var tObj = new JsonObject
                            {
                                ["PlacementWeights"] = new JsonObject
                                {
                                    ["Start"] = tp.PlacementStartWeight,
                                    ["Middle"] = tp.PlacementMiddleWeight,
                                    ["End"] = tp.PlacementEndWeight
                                },
                                ["AreaSeedOffset"] = tp.AreaSeedOffset,
                                ["MarkerSeedOffset"] = tp.MarkerSeedOffset,
                                ["LocalLogFiles"] = new JsonArray(),
                                ["UniqueCommands"] = uniqueCommands,
                                ["StartingStateData"] = startingStateData
                            };

                            terminalsArray.Add(tObj);
                        }

                        // Enemy respawning exclude list
                        var excludeArray = new JsonArray();
                        foreach (var ex in zoneSettings.ExcludeList)
                        {
                            if (ex != null && ex.EnemyId > 0)
                            {
                                excludeArray.Add(new JsonObject { ["EnemyID"] = ex.EnemyId });
                            }
                        }

                        // Disinfection station placements
                        var disinfPlacementsArray = new JsonArray();
                        foreach (var dsp in zoneSettings.DisinfectionStationPlacements)
                        {
                            if (dsp == null) continue;
                            disinfPlacementsArray.Add(new JsonObject
                            {
                                ["PlacementWeights"] = new JsonObject
                                {
                                    ["Start"] = dsp.PlacementStartWeight,
                                    ["Middle"] = dsp.PlacementMiddleWeight,
                                    ["End"] = dsp.PlacementEndWeight
                                },
                                ["AreaSeedOffset"] = 0,
                                ["MarkerSeedOffset"] = 0
                            });
                        }

                        string corpsesStr = zoneSettings.CorpsesInZone switch
                        {
                            0 => "None_0",
                            <= 2 => "Pair_2",
                            <= 5 => "Few_5",
                            <= 10 => "Some_10",
                            <= 15 => "SomeMore_15",
                            <= 20 => "Many_20",
                            <= 30 => "Alot_30",
                            _ => "Tons_50"
                        };

                        string subComplexStr = level.ComplexType switch
                        {
                            ComplexType.Mining => "DigSite",
                            ComplexType.Tech => "Lab",
                            ComplexType.Service => "Storage",
                            _ => "Lab"
                        };

                        uint lightSettingsId = zoneSettings.Lighting > 0
                            ? zoneSettings.Lighting
                            : (mainVm.Lighting.Blocks.FirstOrDefault()?.PersistentId ?? 1);

                        var zoneObj = new JsonObject
                        {
                            ["LocalIndex"] = $"Zone_{zoneNumber}",
                            ["AliasOverride"] = -1,
                            ["OverrideAliasPrefix"] = false,
                            ["SubSeed"] = zoneSettings.SubSeed > 0 ? zoneSettings.SubSeed : (50 + zoneNumber),
                            ["MarkerSubSeed"] = zoneSettings.MarkerSubSeed > 0
                                ? zoneSettings.MarkerSubSeed
                                : (2 + zoneNumber),
                            ["LightsSubSeed"] = zoneSettings.LightingSubSeed,
                            ["BulkheadDCScanSeed"] = zoneSettings.DcScanSeed,
                            ["SubComplex"] = subComplexStr,
                            ["IgnoreRandomGeomorphRotation"] = false,
                            ["CoverageMinMax"] = new JsonObject { ["x"] = 1.0, ["y"] = 1.0 },
                            ["BuildFromLocalIndex"] = zIdx == 0 ? $"Zone_{sectorZones[0]}" : $"Zone_{sectorZones[zIdx - 1]}",
                            ["StartPosition"] = "From_AverageCenter",
                            ["StartPosition_IndexWeight"] = 0.0,
                            ["StartExpansion"] = "Towards_Forward",
                            ["ZoneExpansion"] = "Expansional",
                            ["LightSettings"] = lightSettingsId,
                            ["AltitudeData"] = new JsonObject
                            {
                                ["AllowedZoneAltitude"] = "Unchanged",
                                ["ChanceToChange"] = 0.5
                            },
                            ["EventsOnEnter"] = new JsonArray(),
                            ["EventsOnPortalWarp"] = new JsonArray(),
                            ["EventsOnApproachDoor"] = new JsonArray(),
                            ["EventsOnUnlockDoor"] = new JsonArray(),
                            ["EventsOnOpenDoor"] = new JsonArray(),
                            ["EventsOnDoorScanStart"] = new JsonArray(),
                            ["EventsOnDoorScanDone"] = new JsonArray(),
                            ["ProgressionPuzzleToEnter"] = progPuzzleObj,
                            ["ChainedPuzzleToEnter"] = chainedPuzzleToEnter,
                            ["IsCheckpointDoor"] = false,
                            ["PlayScannerVoiceAudio"] = true,
                            ["SkipAutomaticProgressionObjective"] = false,
                            ["SecurityGateToEnter"] = securityGateToEnter,
                            ["UseStaticBioscanPointsInZone"] = false,
                            ["TurnOffAlarmOnTerminal"] = false,
                            ["TerminalPuzzleZone"] = new JsonObject
                            {
                                ["LocalIndex"] = "Zone_0",
                                ["SeedType"] = "SessionSeed",
                                ["TerminalIndex"] = 0,
                                ["StaticSeed"] = 0
                            },
                            ["EventsOnTerminalDeactivateAlarm"] = new JsonArray(),
                            ["ActiveEnemyWave"] = activeEnemyWaveObj,
                            ["EnemySpawningInZone"] = new JsonArray(),
                            ["EnemyRespawning"] = zoneSettings.EnemyRespawning,
                            ["EnemyRespawnRequireOtherZone"] = zoneSettings.RequireOtherZone,
                            ["EnemyRespawnRoomDistance"] = zoneSettings.RoomDistance,
                            ["EnemyRespawnTimeInterval"] = zoneSettings.TimeInterval,
                            ["EnemyRespawnCountMultiplier"] = zoneSettings.CountMultiplier,
                            ["EnemyRespawnExcludeList"] = excludeArray,
                            ["HSUClustersInZone"] = zoneSettings.HSUClusters,
                            ["CorpseClustersInZone"] = zoneSettings.CorpseClusters,
                            ["ResourceContainerClustersInZone"] = zoneSettings.ResourceContainerClusters,
                            ["GeneratorClustersInZone"] = zoneSettings.ContainsGenCluster ? 1 : 0,
                            ["CorpsesInZone"] = corpsesStr,
                            ["GroundSpawnersInZone"] = zoneSettings.GroundSpawnersInZone.ToString(),
                            ["HSUsInZone"] = zoneSettings.HSUsInZone.ToString(),
                            ["DeconUnitsInZone"] = zoneSettings.DeconUnitsInZone.ToString(),
                            ["AllowSmallPickupsAllocation"] = true,
                            ["AllowResourceContainerAllocation"] = true,
                            ["ForceBigPickupsAllocation"] = false,
                            ["ConsumableDistributionInZone"] = zoneSettings.ConsumableDistributionInZone,
                            ["BigPickupDistributionInZone"] = 0,
                            ["TerminalPlacements"] = terminalsArray,
                            ["ForbidTerminalsInZone"] = false,
                            ["PowerGeneratorPlacements"] = new JsonArray(),
                            ["DisinfectionStationPlacements"] = disinfPlacementsArray,
                            ["DumbwaiterPlacements"] = new JsonArray(),
                            ["HealthMulti"] = zoneSettings.MedKitUses * 0.25,
                            ["HealthPlacement"] = new JsonObject { ["Start"] = 0.0, ["Middle"] = 0.0, ["End"] = 0.0 },
                            ["WeaponAmmoMulti"] = zoneSettings.AmmoKitUses * 0.25,
                            ["WeaponAmmoPlacement"] = new JsonObject { ["Start"] = 0.0, ["Middle"] = 0.0, ["End"] = 0.0 },
                            ["ToolAmmoMulti"] = zoneSettings.ToolKitUses * 0.25,
                            ["ToolAmmoPlacement"] = new JsonObject { ["Start"] = 0.0, ["Middle"] = 0.0, ["End"] = 0.0 },
                            ["DisinfectionMulti"] = zoneSettings.DisinfectKitUses * 0.25,
                            ["DisinfectionPlacement"] = new JsonObject { ["Start"] = 0.0, ["Middle"] = 0.0, ["End"] = 0.0 },
                            ["StaticSpawnDataContainers"] = new JsonArray()
                        };

                        zonesArray.Add(zoneObj);

                        // AreaOverrides for LGTunerAdvanced
                        var areaOverridesArray = new JsonArray();
                        var roomsInZone = gridState.PlacedRooms.Where(r => r.Sector == sectorCfg.Sector && r.Zone == zoneNumber).ToList();

                        foreach (var room in roomsInZone)
                        {
                            var geo = room.Geomorph;
                            if (geo == null) continue;

                            if (roomParentInfo.TryGetValue((geo.Id, room.AreaIndex), out var parentInfo))
                            {
                                areaOverridesArray.Add(new JsonObject
                                {
                                    ["PreviousCellPosition"] = new JsonObject
                                    {
                                        ["x"] = parentInfo.prevX,
                                        ["z"] = parentInfo.prevZ
                                    },
                                    ["PreviousCellInternalAreaID"] = parentInfo.prevArea,
                                    ["LG_ExpanderID"] = parentInfo.expanderId
                                });
                            }
                            else
                            {
                                var connDoor = geo.Doors.FirstOrDefault(d =>
                                    d.IsConnected && d.ConnectedDoor != null && d.ConnectedDoor.Geomorph != null &&
                                    (d.ConnectedDoor.Geomorph.GridX != geo.GridX ||
                                     d.ConnectedDoor.Geomorph.GridY != geo.GridY));
                                if (connDoor != null && connDoor.ConnectedDoor != null &&
                                    connDoor.ConnectedDoor.Geomorph != null)
                                {
                                    var prevGeo = connDoor.ConnectedDoor.Geomorph;
                                    areaOverridesArray.Add(new JsonObject
                                    {
                                        ["PreviousCellPosition"] = new JsonObject
                                        {
                                            ["x"] = prevGeo.GridX,
                                            ["z"] = -prevGeo.GridY
                                        },
                                        ["PreviousCellInternalAreaID"] = connDoor.ConnectedDoor.AreaFrom,
                                        ["LG_ExpanderID"] = connDoor.ConnectedDoor.ExpanderIndex
                                    });
                                }
                                else
                                {
                                    areaOverridesArray.Add(new JsonObject
                                    {
                                        ["PreviousCellPosition"] = new JsonObject
                                        {
                                            ["x"] = geo.GridX,
                                            ["z"] = -geo.GridY
                                        },
                                        ["PreviousCellInternalAreaID"] = room.AreaIndex,
                                        ["LG_ExpanderID"] = 0
                                    });
                                }
                            }
                        }

                        if (areaOverridesArray.Count == 0)
                        {
                            areaOverridesArray.Add(new JsonObject
                            {
                                ["PreviousCellPosition"] = new JsonObject { ["x"] = 0, ["z"] = 0 },
                                ["PreviousCellInternalAreaID"] = 0,
                                ["LG_ExpanderID"] = 0
                            });
                        }

                        zoneOverridesList.Add(new JsonObject
                        {
                            ["AreaOverrides"] = areaOverridesArray
                        });
                    }

                    var layoutObj = new JsonObject
                    {
                        ["ZoneAliasStart"] = sectorCfg.Settings?.ZoneAliasStart > 0 ? sectorCfg.Settings.ZoneAliasStart : 0,
                        ["Zones"] = zonesArray,
                        ["name"] = sectorCfg.Name,
                        ["internalEnabled"] = level.InternalEnabled,
                        ["persistentID"] = sectorCfg.LayoutId
                    };
                    levelLayoutBlocks.Add(layoutObj);

                    // Compile LGTunerAdvanced schema for this sector level layout
                    var geoOverridesArray = new JsonArray();
                    var usedComplexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var sectorGeos = gridState.PlacedGeomorphs
                        .Where(g => g.GeneratedRooms.Any(a => g.GetRoomSector(a) == sectorCfg.Sector) ||
                                    g.Rooms.Any(r => r.Sector == sectorCfg.Sector) ||
                                    gridState.PlacedRooms.Any(r => r.Sector == sectorCfg.Sector && r.Geomorph == g))
                        .Distinct()
                        .ToList();

                    foreach (var geoNode in sectorGeos)
                    {
                        if (geoNode == null) continue;

                        string compName = ExtractComplexName(geoNode.PrefabPath, level.ComplexType);
                        if (!string.IsNullOrWhiteSpace(compName) && !string.Equals(compName, level.ComplexType.ToString(),
                                StringComparison.OrdinalIgnoreCase))
                        {
                            usedComplexes.Add(compName);
                        }

                        // Elevator tile is at (0, 0) and is not part of GeoOverrides
                        if (sectorCfg.IsMain && geoNode.GridX == 0 && geoNode.GridY == 0)
                        {
                            continue;
                        }

                        int altVal = geoNode.Altitude switch
                        {
                            GeomorphAltitude.High => 1,
                            GeomorphAltitude.Low => -1,
                            _ => 0
                        };

                        int rotVal = (int)Math.Round((((geoNode.RotationAngle % 360) + 360) % 360) / 90.0) % 4;

                        geoOverridesArray.Add(new JsonObject
                        {
                            ["Position"] = new JsonObject
                            {
                                ["x"] = geoNode.GridX,
                                ["z"] = -geoNode.GridY
                            },
                            ["Rotation"] = rotVal,
                            ["Geomorph"] = geoNode.PrefabPath,
                            ["Altitude"] = altVal
                        });
                    }

                    var extraComplexArray = new JsonArray();
                    foreach (var comp in usedComplexes.OrderBy(c => c))
                    {
                        extraComplexArray.Add(comp);
                    }

                    var zoneOverridesArray = new JsonArray();
                    foreach (var zo in zoneOverridesList)
                    {
                        zoneOverridesArray.Add(zo);
                    }

                    var levelGenOverrideObj = new JsonObject
                    {
                        ["$schema"] = "../../.schemas/LGTunerAdvanced.json",
                        ["LevelLayoutID"] = sectorCfg.LayoutId,
                        ["ZoneOverrides"] = zoneOverridesArray,
                        ["GeoOverrides"] = geoOverridesArray,
                        ["ExtraComplexResourceToLoad"] = extraComplexArray
                    };

                    string levelGenFileName = $"LevelGen_{sectorCfg.LayoutId}.json";
                    File.WriteAllText(Path.Combine(lgTunerDir, levelGenFileName),
                        levelGenOverrideObj.ToJsonString(JsonOptions));
                    levelGenFilesCount++;
                }
            }

            // 4. Synthesize Complex Resource Sets per level / dimension
            var complexBlocks = new List<JsonObject>();
            var geoEntries = GeomorphService.LoadGeomorphs();
            var geoLookup = geoEntries
                .Where(g => !string.IsNullOrEmpty(g.PrefabPath))
                .GroupBy(g => g.PrefabPath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var levelMainComplexResourceSetId = new Dictionary<uint, uint>();

            foreach (var level in mainVm.LevelLayout.Levels)
            {
                var mainGeos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(level.ElevatorTile))
                {
                    mainGeos.Add(level.ElevatorTile);
                }

                foreach (var gNode in level.GridState.PlacedGeomorphs)
                {
                    if (!string.IsNullOrWhiteSpace(gNode.PrefabPath))
                    {
                        mainGeos.Add(gNode.PrefabPath);
                    }
                }

                uint mainSetId = complexResourceAlloc.Allocate();
                levelMainComplexResourceSetId[level.PersistentId] = mainSetId;

                var mainSetObj = BuildSynthesizedComplexResourceSet(
                    mainSetId,
                    $"Generated_ComplexSet_{level.Name}_Main_{mainSetId}",
                    level.ComplexType,
                    mainGeos,
                    geoLookup
                );
                complexBlocks.Add(mainSetObj);
            }

            // Compile Dimensions & Dimension ComplexResourceSets
            foreach (var level in mainVm.LevelLayout.Levels)
            {
                foreach (var dim in level.Dimensions)
                {
                    uint dimId = dimensionAlloc.Allocate();

                    var dimGeos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(dim.DimensionGeomorph))
                    {
                        dimGeos.Add(dim.DimensionGeomorph);
                    }

                    uint dimSetId = complexResourceAlloc.Allocate();
                    var dimSetObj = BuildSynthesizedComplexResourceSet(
                        dimSetId,
                        $"Generated_ComplexSet_{level.Name}_{dim.Name}_{dimSetId}",
                        dim.ComplexType,
                        dimGeos,
                        geoLookup
                    );
                    complexBlocks.Add(dimSetObj);

                    var dimObj = new JsonObject
                    {
                        ["DimensionData"] = new JsonObject
                        {
                            ["LevelLayoutData"] = dim.LevelLayoutData > 0 ? dim.LevelLayoutData : level.PersistentId,
                            ["DimensionGeomorph"] = dim.DimensionGeomorph,
                            ["DimensionResourceSetID"] = dimSetId,
                            ["DimensionFogData"] = 0,
                            ["IsStaticDimension"] = false
                        },
                        ["name"] = dim.Name,
                        ["internalEnabled"] = true,
                        ["persistentID"] = dimId
                    };
                    dimensionBlocks.Add(dimObj);
                }
            }

            if (dimensionBlocks.Count == 0)
            {
                var vanillaDim = LoadVanillaBlocksArray("DimensionDataBlock.json");
                dimensionBlocks.AddRange(vanillaDim);
            }

            // 5. Compile RundownDataBlock & GameSetupDataBlock
            var rundownBlocks = new List<JsonObject>();
            var rundown = mainVm.Rundown.SelectedRundown;
            uint exportedRundownId = 1;
            if (rundown != null)
            {
                uint rId = rundown.PersistentId > 0 ? rundown.PersistentId : rundownAlloc.Allocate();
                rundownAlloc.RegisterExisting(rId);
                exportedRundownId = rId;

                var rObj = new JsonObject
                {
                    ["NeverShowRundownTree"] = rundown.NeverShowRundownTree,
                    ["UseTierUnlockRequirements"] = rundown.UseTierUnlockRequirements,
                    ["ReqToReachTierA"] = SerializeTierRequirements(rundown.ReqToReachTierA),
                    ["ReqToReachTierB"] = SerializeTierRequirements(rundown.ReqToReachTierB),
                    ["ReqToReachTierC"] = SerializeTierRequirements(rundown.ReqToReachTierC),
                    ["ReqToReachTierD"] = SerializeTierRequirements(rundown.ReqToReachTierD),
                    ["ReqToReachTierE"] = SerializeTierRequirements(rundown.ReqToReachTierE),
                    ["StorytellingData"] = new JsonObject
                    {
                        ["Title"] = !string.IsNullOrWhiteSpace(rundown.StorytellingData?.Title)
                            ? SynthesizeText(rundown.StorytellingData.Title, "Rundown Title")
                            : 0,
                        ["TextLog"] = rundown.StorytellingData?.TextLog ?? 0,
                        ["TextLogPos"] = new JsonObject
                        {
                            ["x"] = rundown.StorytellingData?.TextLogPos?.X ?? 0.0,
                            ["y"] = rundown.StorytellingData?.TextLogPos?.Y ?? 0.0
                        },
                        ["Visuals"] = new JsonObject
                        {
                            ["ColorBackground"] = SerializeColor(rundown.StorytellingData?.Visuals?.ColorBackground),
                            ["TierAVisuals"] = SerializeTierVisuals(rundown.StorytellingData?.Visuals?.TierAVisuals),
                            ["TierBVisuals"] = SerializeTierVisuals(rundown.StorytellingData?.Visuals?.TierBVisuals),
                            ["TierCVisuals"] = SerializeTierVisuals(rundown.StorytellingData?.Visuals?.TierCVisuals),
                            ["TierDVisuals"] = SerializeTierVisuals(rundown.StorytellingData?.Visuals?.TierDVisuals),
                            ["TierEVisuals"] = SerializeTierVisuals(rundown.StorytellingData?.Visuals?.TierEVisuals)
                        },
                        ["SurfaceIconPosition"] = new JsonObject
                        {
                            ["x"] = rundown.StorytellingData?.SurfaceIconPosition?.X ?? 0.0,
                            ["y"] = rundown.StorytellingData?.SurfaceIconPosition?.Y ?? 0.0
                        },
                        ["SurfaceDescription"] =
                            !string.IsNullOrWhiteSpace(rundown.StorytellingData?.SurfaceDescription)
                                ? SynthesizeText(rundown.StorytellingData.SurfaceDescription, "Surface Description")
                                : 0,
                        ["ExternalExpTitle"] = rundown.StorytellingData?.ExternalExpTitle ?? 0
                    },
                    ["VanityItemLayerDropDataBlock"] = rundown.VanityItemLayerDropDataBlock,
                    ["TierA"] = SerializeTierExpeditions(rundown.TierA, SynthesizeText, levelMainComplexResourceSetId,
                        levelMainObjectiveId, levelMainObjectiveIds, levelSecondaryObjectiveId, levelThirdObjectiveId,
                        levelSecondaryLayoutId, levelThirdLayoutId,
                        mainVm.LevelLayout?.Levels),
                    ["TierB"] = SerializeTierExpeditions(rundown.TierB, SynthesizeText, levelMainComplexResourceSetId,
                        levelMainObjectiveId, levelMainObjectiveIds, levelSecondaryObjectiveId, levelThirdObjectiveId,
                        levelSecondaryLayoutId, levelThirdLayoutId,
                        mainVm.LevelLayout?.Levels),
                    ["TierC"] = SerializeTierExpeditions(rundown.TierC, SynthesizeText, levelMainComplexResourceSetId,
                        levelMainObjectiveId, levelMainObjectiveIds, levelSecondaryObjectiveId, levelThirdObjectiveId,
                        levelSecondaryLayoutId, levelThirdLayoutId,
                        mainVm.LevelLayout?.Levels),
                    ["TierD"] = SerializeTierExpeditions(rundown.TierD, SynthesizeText, levelMainComplexResourceSetId,
                        levelMainObjectiveId, levelMainObjectiveIds, levelSecondaryObjectiveId, levelThirdObjectiveId,
                        levelSecondaryLayoutId, levelThirdLayoutId,
                        mainVm.LevelLayout?.Levels),
                    ["TierE"] = SerializeTierExpeditions(rundown.TierE, SynthesizeText, levelMainComplexResourceSetId,
                        levelMainObjectiveId, levelMainObjectiveIds, levelSecondaryObjectiveId, levelThirdObjectiveId,
                        levelSecondaryLayoutId, levelThirdLayoutId,
                        mainVm.LevelLayout?.Levels),
                    ["name"] = rundown.Name,
                    ["internalEnabled"] = rundown.InternalEnabled,
                    ["persistentID"] = rId
                };
                rundownBlocks.Add(rObj);
            }

            var gameSetupBlocks = new List<JsonObject>();
            var vanillaGameSetup = LoadVanillaBlocksArray("GameSetupDataBlock.json");
            if (vanillaGameSetup.Count > 0)
            {
                foreach (var setupBlock in vanillaGameSetup)
                {
                    setupBlock["RundownIdsToLoad"] = new JsonArray { exportedRundownId };
                    gameSetupBlocks.Add(setupBlock);
                }
            }
            else
            {
                var setupBlock = new JsonObject
                {
                    ["RundownIdsToLoad"] = new JsonArray { exportedRundownId },
                    ["StartupScreenToLoad"] = "StartupScreenData_3",
                    ["name"] = "Default",
                    ["internalEnabled"] = true,
                    ["persistentID"] = 1
                };
                gameSetupBlocks.Add(setupBlock);
            }

            // 6. Compile Lighting & Fog
            var lightBlocks = SerializeLightBlocks(mainVm.Lighting.Blocks);
            var fogBlocks = SerializeFogBlocks(mainVm.FogSettings.Blocks);

            // Always explicitly ensure vanilla Light ID 1 is included in LightSettingsDataBlock
            if (!lightBlocks.Any(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 1))
            {
                var vanillaLight = LoadVanillaBlocksArray("LightSettingsDataBlock.json");
                var vanillaLight1 = vanillaLight.FirstOrDefault(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 1);
                if (vanillaLight1 != null)
                {
                    lightBlocks.Add(vanillaLight1);
                }
                else
                {
                    var defaultLight = EditorDefaultsService.CreateDefaultLightBlock(1, "BlueToOrange_1");
                    lightBlocks.AddRange(SerializeLightBlocks(new[] { defaultLight }));
                }
            }

            // Always explicitly ensure vanilla Fog ID 1 is included in FogSettingsDataBlock
            if (!fogBlocks.Any(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 1))
            {
                var vanillaFog = LoadVanillaBlocksArray("FogSettingsDataBlock.json");
                var vanillaFog1 = vanillaFog.FirstOrDefault(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 1);
                if (vanillaFog1 != null)
                {
                    fogBlocks.Add(vanillaFog1);
                }
                else
                {
                    fogBlocks.Add(new JsonObject
                    {
                        ["Enabled"] = true,
                        ["FogColor"] = new JsonObject
                        {
                            ["a"] = 0.0,
                            ["r"] = 0.9117647,
                            ["g"] = 0.967139959,
                            ["b"] = 1.0
                        },
                        ["FogDensity"] = 0.02,
                        ["FogAmbience"] = 0.0,
                        ["DensityNoiseDirection"] = new JsonObject
                        {
                            ["x"] = 1.48,
                            ["y"] = 0.0,
                            ["z"] = 0.0
                        },
                        ["DensityNoiseSpeed"] = 0.009,
                        ["DensityNoiseScale"] = 0.06,
                        ["DensityHeightAltitude"] = -0.91,
                        ["DensityHeightRange"] = 1.01,
                        ["DensityHeightMaxBoost"] = 0.09,
                        ["Infection"] = 0.0,
                        ["name"] = "Fog_medium_white_alt",
                        ["internalEnabled"] = true,
                        ["persistentID"] = 1
                    });
                }
            }

            // Always explicitly ensure vanilla Fog ID 21 (Fog_ELEVATOR) is included in FogSettingsDataBlock
            if (!fogBlocks.Any(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 21))
            {
                var vanillaFog = LoadVanillaBlocksArray("FogSettingsDataBlock.json");
                var vanillaFog21 = vanillaFog.FirstOrDefault(b =>
                    b.TryGetPropertyValue("persistentID", out var pid) && pid != null &&
                    uint.TryParse(pid.ToString(), out uint id) && id == 21);
                if (vanillaFog21 != null)
                {
                    fogBlocks.Add(vanillaFog21);
                }
                else
                {
                    fogBlocks.Add(new JsonObject
                    {
                        ["Enabled"] = true,
                        ["FogColor"] = new JsonObject
                        {
                            ["a"] = 0.0,
                            ["r"] = 0.8632076,
                            ["g"] = 0.991971433,
                            ["b"] = 1.0
                        },
                        ["FogDensity"] = 0.0,
                        ["FogAmbience"] = 0.0,
                        ["DensityNoiseDirection"] = new JsonObject
                        {
                            ["x"] = 0.0,
                            ["y"] = 0.0,
                            ["z"] = 0.0
                        },
                        ["DensityNoiseSpeed"] = 0.0,
                        ["DensityNoiseScale"] = 0.131,
                        ["DensityHeightAltitude"] = 0.0,
                        ["DensityHeightRange"] = 0.0,
                        ["DensityHeightMaxBoost"] = 0.0,
                        ["Infection"] = 0.0,
                        ["name"] = "Fog_ELEVATOR",
                        ["internalEnabled"] = true,
                        ["persistentID"] = 21
                    });
                }
            }

            // 6. Write all datablocks
            int datablocksCount = 0;

            WriteDatablockFile(exportRootPath, "GameData_ChainedPuzzleDataBlock_bin.json", chainedPuzzleBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_ChainedPuzzleTypeDataBlock_bin.json",
                vanillaChainedPuzzleTypeBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_ComplexResourceSetDataBlock_bin.json", complexBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_DimensionDataBlock_bin.json", dimensionBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_EnemyDataBlock_bin.json", vanillaEnemyBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_EnemyGroupDataBlock_bin.json",
                enemyGroupBlocks.Count > 0 ? enemyGroupBlocks : vanillaEnemyGroupBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_ExpeditionBalanceDataBlock_bin.json",
                vanillaExpeditionBalanceBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_FogSettingsDataBlock_bin.json", fogBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_GameSetupDataBlock_bin.json", gameSetupBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_LevelLayoutDataBlock_bin.json", levelLayoutBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_LightSettingsDataBlock_bin.json", lightBlocks);
            datablocksCount++;
            var vanillaRundownHeaders = LoadVanillaHeadersArray("RundownDataBlock.json");
            WriteDatablockFile(exportRootPath, "GameData_RundownDataBlock_bin.json", rundownBlocks,
                vanillaRundownHeaders);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_SurvivalWavePopulationDataBlock_bin.json",
                survivalWavePopulationBlocks.Count > 0
                    ? survivalWavePopulationBlocks
                    : vanillaSurvivalWavePopulationBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_SurvivalWaveSettingsDataBlock_bin.json",
                survivalWaveSettingsBlocks.Count > 0 ? survivalWaveSettingsBlocks : vanillaSurvivalWaveSettingsBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_TextDataBlock_bin.json", textBlocks);
            datablocksCount++;
            WriteDatablockFile(exportRootPath, "GameData_WardenObjectiveDataBlock_bin.json", wardenObjectiveBlocks);
            datablocksCount++;
            var vanillaConsumableDistributionBlocks = LoadVanillaBlocksArray("ConsumableDistributionDataBlock.json");
            var consumableBlocks = SerializeConsumableDistributionBlocks(mainVm.Consumables.Blocks);
            var vanillaConsumableHeaders = LoadVanillaHeadersArray("ConsumableDistributionDataBlock.json");
            WriteDatablockFile(exportRootPath, "GameData_ConsumableDistributionDataBlock_bin.json",
                consumableBlocks.Count > 0 ? consumableBlocks : vanillaConsumableDistributionBlocks,
                vanillaConsumableHeaders);
            datablocksCount++;

            return new ExportResult
            {
                Success = true,
                DatablocksExportedCount = datablocksCount,
                LevelGenFilesExportedCount = levelGenFilesCount,
                OutputDirectory = exportRootPath,
                Message =
                    $"Successfully exported {datablocksCount} datablocks and {levelGenFilesCount} level generation schemas to MTFO structure."
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    private static void WriteDatablockFile(string outputDir, string fileName, IEnumerable<JsonObject> blocks,
        JsonArray? headers = null)
    {
        var root = new JsonObject();
        if (headers != null)
        {
            root["Headers"] = headers;
        }

        root["Blocks"] = new JsonArray(blocks.Select(b => JsonNode.Parse(b.ToJsonString())!).ToArray());

        string filePath = Path.Combine(outputDir, fileName);
        File.WriteAllText(filePath, root.ToJsonString(JsonOptions));
    }

    private static JsonArray? LoadVanillaHeadersArray(string fileName)
    {
        string dir = AlarmService.GetDatablocksDirectory();
        string fullPath = Path.Combine(dir, fileName);
        if (!File.Exists(fullPath)) return null;

        try
        {
            string json = File.ReadAllText(fullPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Headers", out var headersEl) &&
                headersEl.ValueKind == JsonValueKind.Array)
            {
                var node = JsonNode.Parse(headersEl.GetRawText())?.AsArray();
                return node;
            }
        }
        catch
        {
        }

        return null;
    }

    private static List<JsonObject> LoadVanillaBlocksArray(string fileName)
    {
        var list = new List<JsonObject>();
        string dir = AlarmService.GetDatablocksDirectory();
        string fullPath = Path.Combine(dir, fileName);
        if (!File.Exists(fullPath)) return list;

        try
        {
            string json = File.ReadAllText(fullPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Blocks", out var blocksElement) &&
                blocksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in blocksElement.EnumerateArray())
                {
                    var node = JsonNode.Parse(el.GetRawText())?.AsObject();
                    if (node != null) list.Add(node);
                }
            }
        }
        catch
        {
        }

        return list;
    }

    private static void RegisterExistingIds(IEnumerable<JsonObject> blocks, PersistentIdAllocator allocator)
    {
        foreach (var b in blocks)
        {
            if (b.TryGetPropertyValue("persistentID", out var idNode) && idNode != null)
            {
                if (uint.TryParse(idNode.ToString(), out uint id))
                {
                    allocator.RegisterExisting(id);
                }
            }
        }
    }

    private static Dictionary<uint, double> LoadEnemyCosts(IEnumerable<JsonObject> enemyBlocks)
    {
        var costs = new Dictionary<uint, double>
        {
            [24] = 1.0, // Striker
            [26] = 1.0, // Shooter
            [35] = 4.0, // Big Striker
            [36] = 4.0, // Big Shooter
            [40] = 10.0, // Mother / Tank
            [41] = 1.0, // Shadow
            [42] = 4.0, // Big Shadow
            [38] = 5.0, // Scout
        };

        foreach (var eb in enemyBlocks)
        {
            if (eb.TryGetPropertyValue("persistentID", out var idNode) && idNode != null &&
                uint.TryParse(idNode.ToString(), out uint id))
            {
                if (!costs.ContainsKey(id))
                {
                    costs[id] = 1.0;
                }
            }
        }

        return costs;
    }

    private static string GetEnemyRoleName(uint enemyId)
    {
        return enemyId switch
        {
            24 => "Melee",
            26 => "Ranged",
            35 => "MeleeLarge",
            36 => "RangedLarge",
            40 => "Boss",
            41 => "Melee",
            42 => "MeleeLarge",
            38 => "MiniBoss",
            _ => "Melee"
        };
    }

    private static string MapToDistributionEnum(double proportion)
    {
        if (proportion >= 0.875) return "Rel_100";
        if (proportion >= 0.625) return "Rel_75";
        if (proportion >= 0.375) return "Rel_50";
        if (proportion >= 0.20) return "Rel_25";
        if (proportion >= 0.125) return "Rel_15";
        if (proportion >= 0.075) return "Rel_10";
        return "Rel_05";
    }

    private static string ExtractComplexName(string prefabPath, ComplexType fallback)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return fallback.ToString();
        if (prefabPath.Contains("Mining", StringComparison.OrdinalIgnoreCase)) return "Mining";
        if (prefabPath.Contains("Service", StringComparison.OrdinalIgnoreCase)) return "Service";
        if (prefabPath.Contains("Tech", StringComparison.OrdinalIgnoreCase)) return "Tech";
        return fallback.ToString();
    }

    private static JsonArray SerializeWardenEvents(IEnumerable<WardenEvent>? events,
        Func<string?, string, uint> textSynth)
    {
        var array = new JsonArray();
        if (events == null) return array;

        foreach (var ev in events)
        {
            if (ev == null) continue;
            string layer = ev.Layer switch
            {
                SectorType.Secondary => "SecondaryLayer",
                SectorType.Overload => "ThirdLayer",
                _ => "MainLayer"
            };

            array.Add(new JsonObject
            {
                ["Type"] = ev.Type.ToString(),
                ["Condition"] = new JsonObject
                {
                    ["ConditionIndex"] = -1,
                    ["IsTrue"] = false
                },
                ["Trigger"] = ev.Trigger.ToString(),
                ["ChainPuzzle"] = ev.ChainPuzzle,
                ["UseStaticBioscanPoints"] = ev.UseStaticBioscanPoints,
                ["Layer"] = layer,
                ["DimensionIndex"] = string.IsNullOrWhiteSpace(ev.Dimension) ? "Reality" : ev.Dimension,
                ["LocalIndex"] = $"Zone_{ev.Zone}",
                ["Delay"] = ev.Delay,
                ["Duration"] = ev.Duration,
                ["ClearDimension"] = ev.ClearDimension,
                ["WardenIntel"] = string.IsNullOrWhiteSpace(ev.WardenIntel)
                    ? 0
                    : textSynth(ev.WardenIntel, "Warden Intel"),
                ["CustomSubObjectiveHeader"] = string.IsNullOrWhiteSpace(ev.CustomSubObjectiveHeader)
                    ? 0
                    : textSynth(ev.CustomSubObjectiveHeader, "Sub Objective Header"),
                ["CustomSubObjective"] = string.IsNullOrWhiteSpace(ev.CustomSubObjective)
                    ? 0
                    : textSynth(ev.CustomSubObjective, "Sub Objective"),
                ["SoundID"] = ev.SoundID,
                ["SoundSubtitle"] = string.IsNullOrWhiteSpace(ev.SoundSubtitle)
                    ? 0
                    : textSynth(ev.SoundSubtitle, "Sound Subtitle"),
                ["DialogueID"] = 0,
                ["FogSetting"] = ev.FogSetting,
                ["FogTransitionDuration"] = (double)ev.FogTransitionDuration,
                ["EnemyWaveData"] = new JsonObject
                {
                    ["WaveSettings"] = 0,
                    ["WavePopulation"] = 0,
                    ["AreaDistance"] = 2,
                    ["SpawnDelay"] = 0.0,
                    ["TriggerAlarm"] = false,
                    ["IntelMessage"] = 0
                },
                ["EnemyID"] = ev.EnemyID,
                ["Position"] = new JsonObject
                {
                    ["x"] = ev.Position.X,
                    ["y"] = ev.Position.Y,
                    ["z"] = ev.Position.Z
                },
                ["Count"] = ev.Count,
                ["Enabled"] = ev.Enabled,
                ["TerminalCommand"] = "None",
                ["TerminalCommandRule"] = "Normal",
                ["SustainedEventSlotIndex"] = 0,
                ["SustainedEventStateCount"] = 0,
                ["SustainedEventStateDuration"] = 0.0,
                ["SustainedEventDelay"] = 0.0
            });
        }

        return array;
    }

    private static JsonObject CompileWardenObjective(SectorSettings sector, uint persistentId,
        Func<string?, string, uint> textSynth) =>
        CompileWardenObjective(sector.PrimaryObjective, persistentId, textSynth);

    private static JsonObject CompileWardenObjective(WardenObjective sector, uint persistentId,
        Func<string?, string, uint> textSynth)
    {
        var retrieveItemsArray = new JsonArray();
        if (sector.ObjectiveType == ObjectiveType.RetrieveBigItems)
        {
            int itemId = (int)sector.RetrieveItem;
            int count = Math.Max(1, sector.RetrieveItemPlacements.Count);
            for (int i = 0; i < count; i++)
            {
                retrieveItemsArray.Add(itemId);
            }
        }

        var postCommandOutputArray = new JsonArray();
        if (!string.IsNullOrWhiteSpace(sector.PostCommandOutput))
        {
            var lines = sector.PostCommandOutput.Split(new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                postCommandOutputArray.Add(line);
            }
        }

        var centralFogSteps = new JsonArray();
        if (sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster &&
            sector.CentralGeneratorClusterFogTransitions != null)
        {
            foreach (var step in sector.CentralGeneratorClusterFogTransitions)
            {
                if (step == null) continue;
                centralFogSteps.Add(new JsonObject
                {
                    ["m_fogDataId"] = step.FogSetting,
                    ["m_transitionToTime"] = step.TransitionDuration
                });
            }
        }

        var reactorWaves = new JsonArray();
        if ((sector.ObjectiveType == ObjectiveType.Reactor_Startup ||
             sector.ObjectiveType == ObjectiveType.Reactor_Shutdown) && sector.ReactorStartupStages != null)
        {
            foreach (var stage in sector.ReactorStartupStages)
            {
                if (stage == null) continue;
                reactorWaves.Add(new JsonObject
                {
                    ["Warmup"] = stage.Warmup,
                    ["WarmupFail"] = stage.WarmupFail,
                    ["Wave"] = stage.Wave,
                    ["Verify"] = stage.Verify,
                    ["VerifyFail"] = stage.VerifyFail,
                    ["VerifyInOtherZone"] = stage.VerifyInOtherZone,
                    ["ZoneForVerification"] = $"Zone_{stage.ZoneForVerification}",
                    ["EnemyWaves"] = new JsonArray(),
                    ["Events"] = new JsonArray()
                });
            }
        }

        return new JsonObject
        {
            ["Type"] = sector.ObjectiveType.ToString(),
            ["Header"] = textSynth(sector.Header, "Objective Header"),
            ["MainObjective"] = textSynth(sector.MainObjective, "Main Objective"),
            ["WardenObjectiveSpecialUpdateType"] = "None",
            ["GenericItemFromStart"] = 0,
            ["DoNotMarkPickupItemsAsWardenObjectives"] = false,
            ["OverrideNoRequiredItemsForExit"] = false,
            ["FindLocationInfo"] = textSynth(sector.FindLocationInfo, "Find Location Info"),
            ["FindLocationInfoHelp"] = textSynth(sector.FindLocationInfoHelp, "Find Location Info Help"),
            ["GoToZone"] = textSynth(sector.GoToZone, "Go To Zone"),
            ["GoToZoneHelp"] = 0,
            ["InZoneFindItem"] = textSynth(sector.InZoneFindItem, "In Zone Find Item"),
            ["InZoneFindItemHelp"] = 0,
            ["SolveItem"] = textSynth(sector.SolveItem, "Solve Item"),
            ["SolveItemHelp"] = 0,
            ["GoToWinCondition_Elevator"] = textSynth("Return to the elevator", "Win Condition Elevator"),
            ["GoToWinConditionHelp_Elevator"] = 0,
            ["GoToWinCondition_CustomGeo"] = textSynth("Reach the extraction zone", "Win Condition Geo"),
            ["GoToWinConditionHelp_CustomGeo"] = 0,
            ["GoToWinCondition_ToMainLayer"] = 0,
            ["GoToWinConditionHelp_ToMainLayer"] = 0,
            ["ShowHelpDelay"] = 180.0,
            ["WavesOnElevatorLand"] = new JsonArray(),
            ["EventsOnElevatorLand"] = SerializeWardenEvents(sector.EventsOnElevatorLand, textSynth),
            ["WaveOnElevatorWardenIntel"] = 0,
            ["FogTransitionDataOnElevatorLand"] = 0,
            ["FogTransitionDurationOnElevatorLand"] = 0.0,
            ["OnActivateOnSolveItem"] = false,
            ["WavesOnActivate"] = new JsonArray(),
            ["EventsOnActivate"] =
                SerializeWardenEvents(
                    sector.EventsOnActivate.Count > 0 ? sector.EventsOnActivate : sector.EventsOnActivation, textSynth),
            ["StopAllWavesBeforeGotoWin"] = sector.StopAllWavesBeforeGotoWin,
            ["WavesOnGotoWin"] = new JsonArray(),
            ["WaveOnGotoWinTrigger"] = sector.WaveOnGotoWinTrigger.ToString(),
            ["EventsOnGotoWin"] = SerializeWardenEvents(sector.EventsOnGotoWin, textSynth),
            ["EventsOnGotoWinTrigger"] = "OnObjectiveCompleted",
            ["FogTransitionDataOnGotoWin"] = sector.FogTransitionDataOnGotoWin,
            ["FogTransitionDurationOnGotoWin"] = sector.FogTransitionDataOnGotoWinDuration,
            ["ChainedPuzzleToActive"] = sector.ChainedPuzzleToActive,
            ["ChainedPuzzleMidObjective"] = sector.ChainedPuzzleMidObjective,
            ["ChainedPuzzleAtExit"] = 11,
            ["ChainedPuzzleAtExitScanSpeedMultiplier"] = 1.0,
            ["Gather_RequiredCount"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems
                ? Math.Max(1, sector.GatherItemPlacements.Count)
                : -1,
            ["Gather_ItemId"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems ? (int)sector.GatherItem : 0,
            ["Gather_SpawnCount"] = sector.ObjectiveType == ObjectiveType.GatherSmallItems
                ? Math.Max(1, sector.GatherItemPlacements.Count)
                : 0,
            ["Gather_MaxPerZone"] =
                sector.ObjectiveType == ObjectiveType.GatherSmallItems ? sector.GatherMaxPerZone : 0,
            ["Retrieve_Items"] = retrieveItemsArray,
            ["ReactorWaves"] = reactorWaves,
            ["LightsOnFromBeginning"] = false,
            ["LightsOnDuringIntro"] = false,
            ["LightsOnWhenStartupComplete"] = false,
            ["DoNotSolveObjectiveOnReactorComplete"] = sector.DoNotSolveObjectiveOnReactorComplete,
            ["SpecialTerminalCommand"] = sector.SpecialTerminalCommand ?? string.Empty,
            ["SpecialTerminalCommandDesc"] = sector.SpecialTerminalCommandDesc ?? string.Empty,
            ["PostCommandOutput"] = postCommandOutputArray,
            ["SpecialCommandRule"] = sector.SpecialCommandRule.ToString(),
            ["PowerCellsToDistribute"] = sector.ObjectiveType == ObjectiveType.DistributePowerCells
                ? sector.DistributeGeneratorPlacements.Count
                : 0,
            ["Uplink_NumberOfVerificationRounds"] =
                (sector.ObjectiveType == ObjectiveType.TerminalUplink ||
                 sector.ObjectiveType == ObjectiveType.CorruptedTerminalUplink)
                    ? sector.UplinkNumberOfVerificationRounds
                    : 0,
            ["Uplink_NumberOfTerminals"] =
                (sector.ObjectiveType == ObjectiveType.TerminalUplink ||
                 sector.ObjectiveType == ObjectiveType.CorruptedTerminalUplink)
                    ? sector.TerminalUplinkPlacements.Count
                    : 1,
            ["Uplink_WaveSpawnType"] = "InSuppliedCourseNodeZone",
            ["CentralPowerGenClustser_NumberOfGenerators"] =
                sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster
                    ? sector.DistributeGeneratorPlacements.Count
                    : 0,
            ["CentralPowerGenClustser_NumberOfPowerCells"] =
                sector.ObjectiveType == ObjectiveType.CentralGeneratorCluster
                    ? sector.CentralGeneratorClusterPowerCellPlacements.Count
                    : 4,
            ["CentralPowerGenClustser_FogDataSteps"] = centralFogSteps,
            ["ActivateHSU_ItemFromStart"] =
                sector.ObjectiveType == ObjectiveType.ActivateSmallHSU ? (int)sector.ItemFromStart : 0,
            ["ActivateHSU_ItemAfterActivation"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? (int)sector.ItemAfterActivation
                : 0,
            ["ActivateHSU_BringItemInElevator"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.BringItemInElevator
                : true,
            ["ActivateHSU_MarkItemInElevatorAsWardenObjective"] = false,
            ["ActivateHSU_StopEnemyWavesOnActivation"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.StopEnemyWavesOnActivation
                : false,
            ["ActivateHSU_ObjectiveCompleteAfterInsertion"] = sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                ? sector.ObjectiveCompleteAfterInsertion
                : false,
            ["ActivateHSU_RequireItemAfterActivationInExitScan"] =
                sector.ObjectiveType == ObjectiveType.ActivateSmallHSU
                    ? sector.RequireItemAfterActivationInExitScan
                    : false,
            ["ActivateHSU_Events"] = new JsonArray(),
            ["Survival_TimeToActivate"] =
                sector.ObjectiveType == ObjectiveType.Survival ? (double)sector.TimeToActivate : 0.0,
            ["Survival_TimeToSurvive"] =
                sector.ObjectiveType == ObjectiveType.Survival ? (double)sector.TimeToSurvive : 0.0,
            ["Survival_TimerTitle"] =
                (sector.ObjectiveType == ObjectiveType.Survival && !string.IsNullOrWhiteSpace(sector.TimerTitle))
                    ? textSynth(sector.TimerTitle, "Survival Timer Title")
                    : 0,
            ["Survival_TimerToActivateTitle"] =
                (sector.ObjectiveType == ObjectiveType.Survival &&
                 !string.IsNullOrWhiteSpace(sector.TimerToActivateTitle))
                    ? textSynth(sector.TimerToActivateTitle, "Survival Timer To Activate Title")
                    : 0,
            ["GatherTerminal_SpawnCount"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalPlacements.Count
                : 0,
            ["GatherTerminal_RequiredCount"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalRequiredCount
                : 0,
            ["GatherTerminal_Command"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalCommand ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_CommandHelp"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalCommandHelp ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadingText"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalDownloadingText ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadCompleteText"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? (sector.GatherTerminalDownloadCompleteText ?? string.Empty)
                : string.Empty,
            ["GatherTerminal_DownloadTime"] = sector.ObjectiveType == ObjectiveType.GatherTerminal
                ? sector.GatherTerminalDownloadTime
                : -1.0,
            ["TimedTerminalSequence_NumberOfRounds"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceNumberOfRounds
                : 3,
            ["TimedTerminalSequence_NumberOfTerminals"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequencePlacements.Count
                : 1,
            ["TimedTerminalSequence_TimePerRound"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceTimePerRound
                : 90.0,
            ["TimedTerminalSequence_TimeForConfirmation"] = sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                ? sector.TimedTerminalSequenceTimeForConfirmation
                : 10.0,
            ["TimedTerminalSequence_UseFilterForSourceTerminalPicking"] = false,
            ["TimedTerminalSequence_SourceTerminalWorldEventObjectFilter"] =
                sector.ObjectiveType == ObjectiveType.TimedTerminalSequence
                    ? (sector.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter ?? string.Empty)
                    : string.Empty,
            ["TimedTerminalSequence_EventsOnSequenceStart"] =
                SerializeWardenEvents(sector.EventsOnSequenceStart, textSynth),
            ["TimedTerminalSequence_EventsOnSequenceDone"] =
                SerializeWardenEvents(sector.EventsOnSequenceDone, textSynth),
            ["TimedTerminalSequence_EventsOnSequenceFail"] =
                SerializeWardenEvents(sector.EventsOnSequenceFail, textSynth),
            ["name"] = $"Objective_{persistentId}",
            ["internalEnabled"] = true,
            ["persistentID"] = persistentId
        };
    }

    private static JsonObject SerializeTierRequirements(TierRequirements? req)
    {
        req ??= new TierRequirements();
        return new JsonObject
        {
            ["MainSectors"] = req.MainSectors,
            ["SecondarySectors"] = req.SecondarySectors,
            ["ThirdSectors"] = req.ThirdSectors,
            ["AllClearedSectors"] = req.AllClearedSectors
        };
    }

    private static JsonObject SerializeColor(ColorData? c)
    {
        c ??= new ColorData(1.0, 1.0, 1.0, 1.0);
        return new JsonObject
        {
            ["a"] = c.A,
            ["r"] = c.R,
            ["g"] = c.G,
            ["b"] = c.B
        };
    }

    private static JsonObject SerializeTierVisuals(TierVisuals? v)
    {
        v ??= new TierVisuals();
        return new JsonObject
        {
            ["Color"] = SerializeColor(v.Color),
            ["Scale"] = v.Scale,
            ["ScaleYModifier"] = v.ScaleYModifier
        };
    }

    private static JsonArray SerializeTierExpeditions(
        IEnumerable<ExpeditionBlock>? expeditions,
        Func<string?, string, uint> textSynth,
        Dictionary<uint, uint> levelMainComplexResourceSetId,
        Dictionary<uint, uint> levelMainObjectiveId,
        Dictionary<uint, List<uint>> levelMainObjectiveIds,
        Dictionary<uint, uint> levelSecondaryObjectiveId,
        Dictionary<uint, uint> levelThirdObjectiveId,
        Dictionary<uint, uint> levelSecondaryLayoutId,
        Dictionary<uint, uint> levelThirdLayoutId,
        IEnumerable<LevelLayoutBlock>? levels)
    {
        var array = new JsonArray();
        if (expeditions == null) return array;

        var levelList = levels?.ToList() ?? new List<LevelLayoutBlock>();

        foreach (var exp in expeditions)
        {
            if (exp == null) continue;

            var matchingLevel = levelList.FirstOrDefault(l => l.PersistentId == exp.LevelLayoutData) ??
                                levelList.FirstOrDefault();
            uint expLevelLayoutId = exp.LevelLayoutData > 0
                ? exp.LevelLayoutData
                : (matchingLevel?.PersistentId ?? 0);

            uint complexSetId = 1;
            if (expLevelLayoutId > 0 &&
                levelMainComplexResourceSetId.TryGetValue(expLevelLayoutId, out var allocatedId))
            {
                complexSetId = allocatedId;
            }
            else if (matchingLevel != null &&
                     levelMainComplexResourceSetId.TryGetValue(matchingLevel.PersistentId, out var matchAllocId))
            {
                complexSetId = matchAllocId;
            }
            else if (exp.Expedition?.ComplexResourceData > 0)
            {
                complexSetId = exp.Expedition.ComplexResourceData;
            }

            uint defaultMainObjId = 0;
            List<uint>? defaultMainObjIds = null;
            if (expLevelLayoutId > 0 && levelMainObjectiveId.TryGetValue(expLevelLayoutId, out var mObjId))
            {
                defaultMainObjId = mObjId;
                if (levelMainObjectiveIds.TryGetValue(expLevelLayoutId, out var mObjIds))
                {
                    defaultMainObjIds = mObjIds;
                }
            }
            else if (matchingLevel != null && levelMainObjectiveId.TryGetValue(matchingLevel.PersistentId, out var matchMObjId))
            {
                defaultMainObjId = matchMObjId;
                if (levelMainObjectiveIds.TryGetValue(matchingLevel.PersistentId, out var matchMObjIds))
                {
                    defaultMainObjIds = matchMObjIds;
                }
            }

            uint defaultSecObjId = 0;
            if (expLevelLayoutId > 0 && levelSecondaryObjectiveId.TryGetValue(expLevelLayoutId, out var sObjId))
            {
                defaultSecObjId = sObjId;
            }
            else if (matchingLevel != null && levelSecondaryObjectiveId.TryGetValue(matchingLevel.PersistentId, out var matchSObjId))
            {
                defaultSecObjId = matchSObjId;
            }

            uint defaultThirdObjId = 0;
            if (expLevelLayoutId > 0 && levelThirdObjectiveId.TryGetValue(expLevelLayoutId, out var tObjId))
            {
                defaultThirdObjId = tObjId;
            }
            else if (matchingLevel != null && levelThirdObjectiveId.TryGetValue(matchingLevel.PersistentId, out var matchTObjId))
            {
                defaultThirdObjId = matchTObjId;
            }

            bool secEnabled = exp.SecondaryLayerEnabled;
            bool thirdEnabled = exp.ThirdLayerEnabled;

            string secLayerType = NormalizeLayerTypeString(exp.BuildSecondaryFrom?.LayerType);
            string secZone = NormalizeZoneString(exp.BuildSecondaryFrom?.Zone);

            string thirdLayerType = NormalizeLayerTypeString(exp.BuildThirdFrom?.LayerType);
            string thirdZone = NormalizeZoneString(exp.BuildThirdFrom?.Zone);

            var mainBulkheadZones = new List<string>();
            var secBulkheadZones = new List<string>();
            var thirdBulkheadZones = new List<string>();

            if (matchingLevel != null && matchingLevel.GridState != null)
            {
                mainBulkheadZones.AddRange(GetMainBulkheadZones(matchingLevel.GridState));
                secBulkheadZones.AddRange(GetSecondaryBulkheadZones(matchingLevel.GridState));
                thirdBulkheadZones.AddRange(GetBulkheadZones(matchingLevel.GridState, SectorType.Overload));

                if (matchingLevel.HasSecondaryDoor || matchingLevel.GridState.HasSecondaryDoor ||
                    levelSecondaryLayoutId.ContainsKey(expLevelLayoutId) ||
                    levelSecondaryLayoutId.ContainsKey(matchingLevel.PersistentId))
                {
                    secEnabled = true;
                    var resolvedSec = ResolveBuildFrom(matchingLevel.GridState, DoorType.SecondaryDoor);
                    if (resolvedSec.HasValue)
                    {
                        secLayerType = resolvedSec.Value.LayerType;
                        secZone = resolvedSec.Value.Zone;
                    }
                }
                else if (!exp.SecondaryLayerEnabled)
                {
                    secEnabled = false;
                }

                if (matchingLevel.HasOverloadDoor || matchingLevel.GridState.HasOverloadDoor ||
                    levelThirdLayoutId.ContainsKey(expLevelLayoutId) ||
                    levelThirdLayoutId.ContainsKey(matchingLevel.PersistentId))
                {
                    thirdEnabled = true;
                    var resolvedThird = ResolveBuildFrom(matchingLevel.GridState, DoorType.OverloadDoor);
                    if (resolvedThird.HasValue)
                    {
                        thirdLayerType = resolvedThird.Value.LayerType;
                        thirdZone = resolvedThird.Value.Zone;
                    }
                }
                else if (!exp.ThirdLayerEnabled)
                {
                    thirdEnabled = false;
                }
            }

            uint secLayoutId = 0;
            if (secEnabled)
            {
                if (levelSecondaryLayoutId.TryGetValue(expLevelLayoutId, out var sId) && sId > 0) secLayoutId = sId;
                else if (matchingLevel != null && levelSecondaryLayoutId.TryGetValue(matchingLevel.PersistentId, out var msId) && msId > 0) secLayoutId = msId;
                else if (exp.SecondaryLayout > 0) secLayoutId = exp.SecondaryLayout;
            }

            uint thirdLayoutId = 0;
            if (thirdEnabled)
            {
                if (levelThirdLayoutId.TryGetValue(expLevelLayoutId, out var tId) && tId > 0) thirdLayoutId = tId;
                else if (matchingLevel != null && levelThirdLayoutId.TryGetValue(matchingLevel.PersistentId, out var mtId) && mtId > 0) thirdLayoutId = mtId;
                else if (exp.ThirdLayout > 0) thirdLayoutId = exp.ThirdLayout;
            }

            var expObj = new JsonObject
            {
                ["Enabled"] = exp.Enabled,
                ["ExcludeFromMatchmaking"] = exp.ExcludeFromMatchmaking,
                ["IsSinglePlayer"] = exp.IsSinglePlayer,
                ["SkipLobby"] = exp.SkipLobby,
                ["PutIconAboveTier"] = exp.PutIconAboveTier,
                ["DisablePlayerVoicelines"] = exp.DisablePlayerVoicelines,
                ["ExcludeFromProgression"] = exp.ExcludeFromProgression,
                ["HideOnLocked"] = exp.HideOnLocked,
                ["HideRolePlayDataOnLocked"] = exp.HideRolePlayDataOnLocked,
                ["Accessibility"] = exp.Accessibility.ToString(),
                ["UnlockedByExpedition"] = new JsonObject
                {
                    ["Tier"] = exp.UnlockedByExpedition?.Tier ?? "TierA",
                    ["Exp"] = exp.UnlockedByExpedition?.Exp ?? "Exp1"
                },
                ["CustomProgressionLock"] = SerializeTierRequirements(exp.CustomProgressionLock),
                ["Descriptive"] = new JsonObject
                {
                    ["Prefix"] = exp.Descriptive?.Prefix ?? "A",
                    ["PublicName"] = exp.Descriptive?.PublicName ?? "",
                    ["IsExtraExpedition"] = exp.Descriptive?.IsExtraExpedition ?? false,
                    ["SkipExpNumberInName"] = exp.Descriptive?.SkipExpNumberInName ?? false,
                    ["UseCustomMatchmakingTier"] = exp.Descriptive?.UseCustomMatchmakingTier ?? false,
                    ["CustomMatchmakingTier"] = exp.Descriptive?.CustomMatchmakingTier ?? "TierA",
                    ["ProgressionVisualStyle"] = exp.Descriptive?.ProgressionVisualStyle ?? "Normal",
                    ["ExpeditionDepth"] = exp.Descriptive?.ExpeditionDepth ?? 0,
                    ["EstimatedDuration"] = exp.Descriptive?.EstimatedDuration ?? 900,
                    ["ExpeditionDescription"] = !string.IsNullOrWhiteSpace(exp.Descriptive?.ExpeditionDescription)
                        ? textSynth(exp.Descriptive.ExpeditionDescription, "Expedition Description")
                        : 0,
                    ["RoleplayedWardenIntel"] = !string.IsNullOrWhiteSpace(exp.Descriptive?.RoleplayedWardenIntel)
                        ? textSynth(exp.Descriptive.RoleplayedWardenIntel, "Roleplayed Warden Intel")
                        : 0
                },
                ["Seeds"] = new JsonObject
                {
                    ["BuildSeed"] = exp.Seeds?.BuildSeed ?? 0,
                    ["FunctionMarkerOffset"] = exp.Seeds?.FunctionMarkerOffset ?? 0,
                    ["StandardMarkerOffset"] = exp.Seeds?.StandardMarkerOffset ?? 0,
                    ["LightJobSeedOffset"] = exp.Seeds?.LightJobSeedOffset ?? 0
                },
                ["Expedition"] = new JsonObject
                {
                    ["ComplexResourceData"] = complexSetId,
                    ["MLSLevelKit"] = exp.Expedition?.MLSLevelKit ?? 0,
                    ["LightSettings"] = exp.Expedition?.LightSettings ?? 0,
                    ["FogSettings"] = exp.Expedition?.FogSettings ?? 0,
                    ["EnemyPopulation"] = exp.Expedition?.EnemyPopulation ?? 1,
                    ["ExpeditionBalance"] = exp.Expedition?.ExpeditionBalance ?? 1,
                    ["ScoutWaveSettings"] = exp.Expedition?.ScoutWaveSettings ?? 3,
                    ["ScoutWavePopulation"] = exp.Expedition?.ScoutWavePopulation ?? 1,
                    ["EnvironmentWetness"] = exp.Expedition?.EnvironmentWetness ?? 0.0,
                    ["DustColor"] = SerializeColor(exp.Expedition?.DustColor ?? new ColorData(1.0, 0.5, 0.5, 0.5)),
                    ["DustTurbulence"] = exp.Expedition?.DustTurbulence ?? 1.0
                },
                ["VanityItemsDropData"] = SerializeVanityItemsDropData(exp.VanityItemsDropData),
                ["UseGearPicker"] = exp.UseGearPicker
            };

            if (exp.UseGearPicker || exp.GearPicker.HasValue)
            {
                expObj["GearPicker"] = SerializeGearPicker(exp.GearPicker);
            }

            expObj["LevelLayoutData"] = expLevelLayoutId;
            expObj["MainLayerData"] = SerializeLayerData(exp.MainLayerData, defaultMainObjId, mainBulkheadZones, matchingLevel?.MainSector, matchingLevel?.GridState, SectorType.Main, defaultMainObjIds);
            expObj["SecondaryLayerEnabled"] = secEnabled;
            expObj["SecondaryLayout"] = secLayoutId;
            expObj["BuildSecondaryFrom"] = new JsonObject
            {
                ["LayerType"] = secLayerType,
                ["Zone"] = secZone
            };
            expObj["SecondaryLayerData"] =
                SerializeLayerData(exp.SecondaryLayerData, defaultSecObjId, secBulkheadZones, matchingLevel?.SecondarySector, matchingLevel?.GridState, SectorType.Secondary);
            expObj["ThirdLayerEnabled"] = thirdEnabled;
            expObj["ThirdLayout"] = thirdLayoutId;
            expObj["BuildThirdFrom"] = new JsonObject
            {
                ["LayerType"] = thirdLayerType,
                ["Zone"] = thirdZone
            };
            expObj["ThirdLayerData"] = SerializeLayerData(exp.ThirdLayerData, defaultThirdObjId, thirdBulkheadZones, matchingLevel?.OverloadSector, matchingLevel?.GridState, SectorType.Overload);
            expObj["DimensionDatas"] = ConvertToJsonArrayOrEmpty(exp.DimensionDatas);
            expObj["SoundEventOnWarpToReality"] = exp.SoundEventOnWarpToReality;
            expObj["SpecialOverrideData"] = new JsonObject
            {
                ["WeakResourceContainerWithPackChanceForLocked"] =
                    exp.SpecialOverrideData?.WeakResourceContainerWithPackChanceForLocked ?? -1.0,
                ["InfectionLevelAtExpeditionStart"] = exp.SpecialOverrideData?.InfectionLevelAtExpeditionStart ?? 0.0,
                ["HealthLevelAtExpeditionStart"] = exp.SpecialOverrideData?.HealthLevelAtExpeditionStart ?? 1.0,
                ["StandardAmmoAtExpeditionStart"] = exp.SpecialOverrideData?.StandardAmmoAtExpeditionStart ?? 1.0,
                ["SpecialAmmoAtExpeditionStart"] = exp.SpecialOverrideData?.SpecialAmmoAtExpeditionStart ?? 1.0,
                ["ToolAmmoAtExpeditionStart"] = exp.SpecialOverrideData?.ToolAmmoAtExpeditionStart ?? 1.0,
                ["ShowCreditsAfterSuccess"] = exp.SpecialOverrideData?.ShowCreditsAfterSuccess ?? false
            };
            expObj["HasExternalStyle"] = exp.HasExternalStyle;
            expObj["HasStoryStyle"] = exp.HasStoryStyle || exp.IsStory;

            array.Add(expObj);
        }

        return array;
    }

    public static string NormalizeZoneString(string? zone)
    {
        if (string.IsNullOrWhiteSpace(zone)) return "Zone_0";
        zone = zone.Trim();
        if (zone.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase))
        {
            string numPart = zone.Substring(5);
            if (int.TryParse(numPart, out int parsed))
            {
                return $"Zone_{parsed}";
            }

            return zone;
        }

        if (int.TryParse(zone, out int num))
        {
            return $"Zone_{num}";
        }

        return zone;
    }

    public static string NormalizeLayerTypeString(string? layerType)
    {
        if (string.IsNullOrWhiteSpace(layerType)) return "MainLayer";
        layerType = layerType.Trim();
        if (layerType.Equals("Main", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("MainLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("MainSector", StringComparison.OrdinalIgnoreCase))
            return "MainLayer";
        if (layerType.Equals("Secondary", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("SecondaryLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("SecondarySector", StringComparison.OrdinalIgnoreCase))
            return "SecondaryLayer";
        if (layerType.Equals("Third", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("ThirdLayer", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("ThirdSector", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("Overload", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("OverloadSector", StringComparison.OrdinalIgnoreCase) ||
            layerType.Equals("OverloadLayer", StringComparison.OrdinalIgnoreCase))
            return "ThirdLayer";
        return layerType;
    }

    public static (string LayerType, string Zone)? ResolveBuildFrom(LevelLayoutGridState? gridState,
        DoorType targetDoorType)
    {
        if (gridState == null || gridState.PlacedGeomorphs.Count == 0) return null;

        var rootGeo = gridState.PlacedGeomorphs[0];
        var queue = new Queue<(PlacedGeomorphNode geo, int area, SectorType sector)>();
        var visited = new HashSet<(PlacedGeomorphNode geo, int area)>();

        if (rootGeo.IsRoomGenerated(0))
        {
            visited.Add((rootGeo, 0));
            queue.Enqueue((rootGeo, 0, SectorType.Main));
        }

        while (queue.Count > 0)
        {
            var (currGeo, currArea, currSector) = queue.Dequeue();
            int currZone = currGeo.GetRoomZone(currArea);

            // 1. Internal doors
            foreach (var d in gridState.PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
            {
                int neighborArea = -1;
                if (d.AreaFrom == currArea) neighborArea = d.AreaTo;
                else if (d.AreaTo == currArea) neighborArea = d.AreaFrom;

                if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                {
                    bool isTarget = (targetDoorType == DoorType.SecondaryDoor &&
                                     (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)) ||
                                    (targetDoorType == DoorType.OverloadDoor &&
                                     (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor));

                    if (isTarget)
                    {
                        string layerType = currSector switch
                        {
                            SectorType.Main => "MainLayer",
                            SectorType.Secondary => "SecondaryLayer",
                            SectorType.Overload => "ThirdLayer",
                            _ => "MainLayer"
                        };
                        return (layerType, $"Zone_{currZone}");
                    }

                    var neighbor = (currGeo, neighborArea);
                    if (visited.Add(neighbor))
                    {
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)
                            nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor)
                            nextSector = SectorType.Overload;
                        queue.Enqueue((currGeo, neighborArea, nextSector));
                    }
                }
            }

            // 2. External doors
            foreach (var d in gridState.PlacedDoors.Where(d =>
                         !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected &&
                         d.ConnectedDoor != null))
            {
                var otherDoor = d.ConnectedDoor!;
                var otherGeo = otherDoor.Geomorph;
                if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                {
                    bool isTarget = (targetDoorType == DoorType.SecondaryDoor && (d.IsSecondaryDoor ||
                                        otherDoor.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor ||
                                        otherDoor.DoorType == DoorType.SecondaryDoor)) ||
                                    (targetDoorType == DoorType.OverloadDoor && (d.IsOverloadDoor ||
                                        otherDoor.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor ||
                                        otherDoor.DoorType == DoorType.OverloadDoor));

                    if (isTarget)
                    {
                        string layerType = currSector switch
                        {
                            SectorType.Main => "MainLayer",
                            SectorType.Secondary => "SecondaryLayer",
                            SectorType.Overload => "ThirdLayer",
                            _ => "MainLayer"
                        };
                        return (layerType, $"Zone_{currZone}");
                    }

                    var neighbor = (otherGeo, otherDoor.AreaFrom);
                    if (visited.Add(neighbor))
                    {
                        SectorType nextSector = currSector;
                        if (d.IsSecondaryDoor || otherDoor.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor ||
                            otherDoor.DoorType == DoorType.SecondaryDoor) nextSector = SectorType.Secondary;
                        else if (d.IsOverloadDoor || otherDoor.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor ||
                                 otherDoor.DoorType == DoorType.OverloadDoor) nextSector = SectorType.Overload;
                        queue.Enqueue((otherGeo, otherDoor.AreaFrom, nextSector));
                    }
                }
            }
        }

        // Direct fallback if BFS didn't reach the door
        var directDoor = gridState.PlacedDoors.FirstOrDefault(d =>
            (targetDoorType == DoorType.SecondaryDoor && (d.IsSecondaryDoor || d.DoorType == DoorType.SecondaryDoor)) ||
            (targetDoorType == DoorType.OverloadDoor && (d.IsOverloadDoor || d.DoorType == DoorType.OverloadDoor)));

        if (directDoor != null && directDoor.Geomorph != null)
        {
            int z = directDoor.Geomorph.GetRoomZone(directDoor.AreaFrom);
            var sec = gridState.GetSectorForZone(z);
            string layer = sec switch
            {
                SectorType.Main => "MainLayer",
                SectorType.Secondary => "SecondaryLayer",
                SectorType.Overload => "ThirdLayer",
                _ => "MainLayer"
            };
            return (layer, $"Zone_{z}");
        }

        return null;
    }

    public static List<string> GetBulkheadZones(LevelLayoutGridState? gridState, SectorType sectorType)
    {
        var list = new List<string>();
        if (gridState == null || gridState.PlacedGeomorphs.Count == 0) return list;

        var queue = new Queue<(PlacedGeomorphNode geo, int area, SectorType sector, int zone)>();
        var visited = new HashSet<(string geoId, int area)>();

        var rootGeo = gridState.PlacedGeomorphs[0];
        if (rootGeo.IsRoomGenerated(0))
        {
            var sec = rootGeo.GetRoomSector(0);
            var z = rootGeo.GetRoomZone(0);
            visited.Add((rootGeo.Id, 0));
            queue.Enqueue((rootGeo, 0, sec, z));
        }

        void ProcessQueue()
        {
            while (queue.Count > 0)
            {
                var (currGeo, currArea, currSector, currZone) = queue.Dequeue();

                // 1. Internal doors
                foreach (var d in gridState.PlacedDoors.Where(d => d.IsInternal && d.Geomorph == currGeo && d.IsConnected))
                {
                    int neighborArea = (d.AreaFrom == currArea) ? d.AreaTo : (d.AreaTo == currArea ? d.AreaFrom : -1);
                    if (neighborArea >= 0 && currGeo.IsRoomGenerated(neighborArea))
                    {
                        var neighborKey = (currGeo.Id, neighborArea);
                        bool isUnvisited = !visited.Contains(neighborKey);
                        var neighborSector = currGeo.GetRoomSector(neighborArea);
                        var neighborZone = currGeo.GetRoomZone(neighborArea);

                        bool isMainDoor = d.IsMainDoor || d.DoorType == DoorType.MainDoor;
                        if (isMainDoor && isUnvisited)
                        {
                            if (neighborSector == sectorType)
                            {
                                list.Add($"Zone_{neighborZone}");
                            }
                        }

                        if (visited.Add(neighborKey))
                        {
                            queue.Enqueue((currGeo, neighborArea, neighborSector, neighborZone));
                        }
                    }
                }

                // 2. External doors
                foreach (var d in gridState.PlacedDoors.Where(d =>
                             !d.IsInternal && d.Geomorph == currGeo && d.AreaFrom == currArea && d.IsConnected &&
                             d.ConnectedDoor != null))
                {
                    var otherDoor = d.ConnectedDoor!;
                    var otherGeo = otherDoor.Geomorph;
                    if (otherGeo != null && otherGeo.IsRoomGenerated(otherDoor.AreaFrom))
                    {
                        var neighborKey = (otherGeo.Id, otherDoor.AreaFrom);
                        bool isUnvisited = !visited.Contains(neighborKey);
                        var neighborSector = otherGeo.GetRoomSector(otherDoor.AreaFrom);
                        var neighborZone = otherGeo.GetRoomZone(otherDoor.AreaFrom);

                        bool isMainDoor = d.IsMainDoor || otherDoor.IsMainDoor ||
                                          d.DoorType == DoorType.MainDoor ||
                                          otherDoor.DoorType == DoorType.MainDoor;
                        if (isMainDoor && isUnvisited)
                        {
                            if (neighborSector == sectorType)
                            {
                                list.Add($"Zone_{neighborZone}");
                            }
                        }

                        if (visited.Add(neighborKey))
                        {
                            queue.Enqueue((otherGeo, otherDoor.AreaFrom, neighborSector, neighborZone));
                        }
                    }
                }
            }
        }

        ProcessQueue();

        // Check any unvisited disconnected rooms
        foreach (var geo in gridState.PlacedGeomorphs)
        {
            foreach (var area in geo.GeneratedRooms)
            {
                var key = (geo.Id, area);
                if (!visited.Contains(key))
                {
                    var sec = geo.GetRoomSector(area);
                    var z = geo.GetRoomZone(area);
                    visited.Add(key);
                    queue.Enqueue((geo, area, sec, z));
                    ProcessQueue();
                }
            }
        }

        return list.Distinct().ToList();
    }

    public static List<string> GetMainBulkheadZones(LevelLayoutGridState? gridState)
    {
        return GetBulkheadZones(gridState, SectorType.Main);
    }

    public static List<string> GetSecondaryBulkheadZones(LevelLayoutGridState? gridState)
    {
        return GetBulkheadZones(gridState, SectorType.Secondary);
    }

    private static JsonObject SerializeLayerData(
        ExpeditionLayerData? layer,
        uint defaultObjectiveId,
        IEnumerable<string>? autoBulkheadZones = null,
        SectorSettings? sectorSettings = null,
        LevelLayoutGridState? gridState = null,
        SectorType sectorType = SectorType.Main,
        List<uint>? chainedObjectiveIds = null)
    {
        layer ??= new ExpeditionLayerData();
        uint objId = layer.ObjectiveData?.DataBlockId > 0 ? layer.ObjectiveData.DataBlockId : defaultObjectiveId;

        var bulkheadEntrances = ConvertToJsonArrayOrEmpty(layer.ZonesWithBulkheadEntrance);
        if (autoBulkheadZones != null)
        {
            var existingSet = bulkheadEntrances.Select(n => n?.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var z in autoBulkheadZones.Distinct())
            {
                if (!existingSet.Contains(z))
                {
                    bulkheadEntrances.Add(z);
                }
            }
        }

        JsonArray dcPlacementsArray;
        if (gridState != null)
        {
            var dcZones = gridState.PlacedGeomorphs
                .SelectMany(g => g.GeneratedRooms.Where(a => g.GetRoomSector(a) == sectorType).Select(a => g.GetRoomZone(a)))
                .Concat(gridState.PlacedRooms.Where(r => r.Sector == sectorType).Select(r => r.Zone))
                .Concat(gridState.ZoneSettingsMap.Keys.Where(k => k.Sector == sectorType).Select(k => k.ZoneIndex))
                .Distinct()
                .Where(z => gridState.GetOrCreateZoneSettings(sectorType, z).ContainsDC)
                .OrderBy(z => z)
                .ToList();

            dcPlacementsArray = new JsonArray();
            foreach (int z in dcZones)
            {
                var zs = gridState.GetOrCreateZoneSettings(sectorType, z);
                dcPlacementsArray.Add(new JsonObject
                {
                    ["ZoneIndex"] = $"Zone_{z}",
                    ["PlacementWeights"] = new JsonObject
                    {
                        ["Start"] = 0.0,
                        ["Middle"] = 0.0,
                        ["End"] = 0.0
                    },
                    ["AreaSeedOffset"] = 0,
                    ["MarkerSeedOffset"] = 0
                });
            }

            if (dcPlacementsArray.Count == 0 && layer.BulkheadDoorControllerPlacements.HasValue)
            {
                dcPlacementsArray = ConvertToJsonArrayOrEmpty(layer.BulkheadDoorControllerPlacements);
            }
        }
        else
        {
            dcPlacementsArray = ConvertToJsonArrayOrEmpty(layer.BulkheadDoorControllerPlacements);
        }

        JsonArray keyPlacementsArray;
        if (sectorSettings != null && sectorSettings.BulkheadKeyPlacements.Count > 0)
        {
            keyPlacementsArray = new JsonArray();
            foreach (var item in sectorSettings.BulkheadKeyPlacements)
            {
                var candidatesArray = new JsonArray();
                foreach (var cz in item.CandidateZones)
                {
                    candidatesArray.Add(new JsonObject
                    {
                        ["DimensionIndex"] = string.IsNullOrWhiteSpace(cz.DimensionIndex) ? "Reality" : cz.DimensionIndex,
                        ["LocalIndex"] = $"Zone_{cz.Zone}",
                        ["Weights"] = new JsonObject
                        {
                            ["Start"] = cz.PlacementStartWeight,
                            ["Middle"] = cz.PlacementMiddleWeight,
                            ["End"] = cz.PlacementEndWeight
                        }
                    });
                }
                keyPlacementsArray.Add(candidatesArray);
            }
        }
        else
        {
            keyPlacementsArray = ConvertToJsonArrayOrEmpty(layer.BulkheadKeyPlacements);
        }

        string winCondition = sectorSettings?.PrimaryObjective != null
            ? sectorSettings.PrimaryObjective.WinCondition.ToString()
            : (!string.IsNullOrWhiteSpace(layer.ObjectiveData?.WinCondition)
                ? layer.ObjectiveData.WinCondition
                : "GoToElevator");

        double artifactMultiplier = sectorSettings != null
            ? sectorSettings.ArtifactAmountMultiplier
            : (layer.ArtifactData.HasValue && layer.ArtifactData.Value.TryGetProperty("ArtifactAmountMulti", out var am)
                ? (am.TryGetDouble(out var v) ? v : 1.0)
                : 1.0);

        var artifactDataNode = SerializeArtifactData(layer.ArtifactData);
        if (artifactDataNode is JsonObject artObj)
        {
            artObj["ArtifactAmountMulti"] = artifactMultiplier;
        }

        JsonArray chainedObjectivesArray;
        if (sectorType == SectorType.Main && sectorSettings != null)
        {
            chainedObjectivesArray = new JsonArray();
            if (sectorSettings.Objectives.Count > 1)
            {
                for (int i = 1; i < sectorSettings.Objectives.Count; i++)
                {
                    var chainedObj = sectorSettings.Objectives[i];
                    uint chainedId = (chainedObjectiveIds != null && chainedObjectiveIds.Count > i)
                        ? chainedObjectiveIds[i]
                        : 0;

                    chainedObjectivesArray.Add(new JsonObject
                    {
                        ["DataBlockId"] = chainedId,
                        ["WinCondition"] = chainedObj.WinCondition.ToString(),
                        ["ZonePlacementDatas"] = new JsonArray()
                    });
                }
            }
        }
        else if (sectorType != SectorType.Main)
        {
            chainedObjectivesArray = new JsonArray();
        }
        else
        {
            chainedObjectivesArray = ConvertToJsonArrayOrEmpty(layer.ChainedObjectiveData);
        }

        return new JsonObject
        {
            ["ZonesWithBulkheadEntrance"] = bulkheadEntrances,
            ["BulkheadDoorControllerPlacements"] = dcPlacementsArray,
            ["BulkheadKeyPlacements"] = keyPlacementsArray,
            ["ObjectiveData"] = new JsonObject
            {
                ["DataBlockId"] = objId,
                ["WinCondition"] = winCondition,
                ["ZonePlacementDatas"] = ConvertToJsonArrayOrEmpty(layer.ObjectiveData?.ZonePlacementDatas)
            },
            ["ChainedObjectiveData"] = chainedObjectivesArray,
            ["ArtifactData"] = artifactDataNode
        };
    }

    private static JsonNode SerializeVanityItemsDropData(JsonElement? element)
    {
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object)
        {
            var node = JsonNode.Parse(element.Value.GetRawText());
            if (node != null) return node;
        }

        return new JsonObject
        {
            ["Groups"] = new JsonArray()
        };
    }

    private static JsonNode SerializeGearPicker(JsonElement? element)
    {
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object)
        {
            var node = JsonNode.Parse(element.Value.GetRawText());
            if (node != null) return node;
        }

        return new JsonObject
        {
            ["StandardGear"] = 2,
            ["SpecialGear"] = 6,
            ["ToolGear"] = 12,
            ["MeleeGear"] = 29
        };
    }

    private static JsonArray ConvertToJsonArrayOrEmpty(JsonElement? element)
    {
        if (element.HasValue && element.Value.ValueKind == JsonValueKind.Array)
        {
            var node = JsonNode.Parse(element.Value.GetRawText());
            if (node is JsonArray arr) return arr;
        }

        return new JsonArray();
    }

    private static JsonNode SerializeArtifactData(JsonElement? element)
    {
        if (element.HasValue && (element.Value.ValueKind == JsonValueKind.Object ||
                                 element.Value.ValueKind == JsonValueKind.Array))
        {
            var node = JsonNode.Parse(element.Value.GetRawText());
            if (node != null) return node;
        }

        return new JsonObject
        {
            ["ArtifactAmountMulti"] = 1.0,
            ["ArtifactLayerDistributionDataID"] = 0,
            ["ArtifactZoneDistributions"] = new JsonArray()
        };
    }

    private static JsonObject BuildSynthesizedComplexResourceSet(
        uint persistentId,
        string name,
        ComplexType complexType,
        IEnumerable<string> prefabPaths,
        Dictionary<string, GeomorphEntry> geoLookup)
    {
        var geos1x1 = new JsonArray();
        var geos2x2 = new JsonArray();
        var elevators = new JsonArray();
        var customObjectives = new JsonArray();

        var subComplexCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var prefab in prefabPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(prefab)) continue;

            string subComplex = "DigSite";
            string shard = "S1";

            if (geoLookup.TryGetValue(prefab, out var entry))
            {
                if (!string.IsNullOrWhiteSpace(entry.SubComplex))
                    subComplex = entry.SubComplex;
                else if (!string.IsNullOrWhiteSpace(entry.Category))
                    subComplex = entry.Category;
            }
            else
            {
                subComplex = complexType == ComplexType.Mining ? "DigSite" :
                    complexType == ComplexType.Tech ? "Lab" : "Floodways";
            }

            subComplexCounts[subComplex] = subComplexCounts.GetValueOrDefault(subComplex, 0) + 1;

            var tileObj = new JsonObject
            {
                ["Prefab"] = prefab,
                ["SubComplex"] = subComplex,
                ["Shard"] = shard
            };

            bool isElevator = prefab.Contains("elevator", StringComparison.OrdinalIgnoreCase);
            bool isCustomObjective = prefab.Contains("objective", StringComparison.OrdinalIgnoreCase) ||
                                     prefab.Contains("custom", StringComparison.OrdinalIgnoreCase);
            bool is2x2 = prefab.Contains("128x128", StringComparison.OrdinalIgnoreCase) ||
                         prefab.Contains("128x64", StringComparison.OrdinalIgnoreCase) ||
                         prefab.Contains("64x128", StringComparison.OrdinalIgnoreCase);

            if (isElevator)
            {
                elevators.Add(tileObj);
            }
            else if (isCustomObjective)
            {
                customObjectives.Add(tileObj);
            }
            else if (is2x2)
            {
                geos2x2.Add(tileObj);
            }
            else
            {
                geos1x1.Add(tileObj);
            }
        }

        string primarySubComplex = subComplexCounts.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        if (string.IsNullOrWhiteSpace(primarySubComplex))
        {
            primarySubComplex = complexType == ComplexType.Mining ? "DigSite" :
                complexType == ComplexType.Tech ? "Lab" : "Floodways";
        }

        var templateObj = ComplexResourceSetService.LoadTemplateJsonObject(complexType);
        JsonObject resultObj;
        if (templateObj != null)
        {
            resultObj = JsonNode.Parse(templateObj.ToJsonString())?.AsObject() ?? new JsonObject();
        }
        else
        {
            resultObj = new JsonObject
            {
                ["ComplexType"] = complexType.ToString(),
                ["PrimareSubComplexUsed"] = primarySubComplex,
                ["BundleName"] = $"Complex_{complexType}",
                ["LevelGenConfig"] = new JsonObject
                {
                    ["GridSize"] = 40,
                    ["CellDimension"] = 64.0,
                    ["AltitudeOffset"] = 6.0,
                    ["TransitionDirection"] = "FloorUp",
                    ["LevelProgression"] = "StartLevel"
                },
                ["RandomizeGeomorphOrder"] = true
            };
        }

        resultObj["name"] = name;
        resultObj["internalEnabled"] = true;
        resultObj["persistentID"] = persistentId;
        if (!string.IsNullOrWhiteSpace(primarySubComplex))
        {
            resultObj["PrimareSubComplexUsed"] = primarySubComplex;
        }

        resultObj["GeomorphTiles_1x1"] = geos1x1;
        resultObj["GeomorphTiles_2x2"] = geos2x2;
        resultObj["ElevatorShafts_1x1"] = elevators;
        resultObj["CustomGeomorphs_Objective_1x1"] = customObjectives;

        return resultObj;
    }

    private static List<JsonObject> SerializeLightBlocks(IEnumerable<LightSettingsBlock> blocks)
    {
        var list = new List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            var cats = new JsonArray();
            foreach (var c in b.LightCategorySettings)
            {
                cats.Add(new JsonObject
                {
                    ["On"] = c.On,
                    ["Category"] = c.Category.ToString(),
                    ["Chance"] = c.Chance,
                    ["ChanceBroken"] = c.ChanceBroken,
                    ["Color"] = new JsonObject
                    {
                        ["a"] = c.Color.A,
                        ["r"] = c.Color.R,
                        ["g"] = c.Color.G,
                        ["b"] = c.Color.B
                    },
                    ["IntensityMul"] = c.IntensityMul,
                    ["Weight"] = c.Weight
                });
            }

            list.Add(new JsonObject
            {
                ["LightCategorySettings"] = cats,
                ["name"] = b.Name,
                ["internalEnabled"] = b.InternalEnabled,
                ["persistentID"] = b.PersistentId
            });
        }

        return list;
    }

    private static List<JsonObject> SerializeFogBlocks(IEnumerable<FogSettingsBlock> blocks)
    {
        var list = new List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            list.Add(new JsonObject
            {
                ["Enabled"] = b.Enabled,
                ["FogColor"] = new JsonObject
                {
                    ["a"] = b.FogColor?.A ?? 0.0,
                    ["r"] = b.FogColor?.R ?? 0.0,
                    ["g"] = b.FogColor?.G ?? 0.0,
                    ["b"] = b.FogColor?.B ?? 0.0
                },
                ["FogDensity"] = b.FogDensity,
                ["FogAmbience"] = b.FogAmbience,
                ["DensityNoiseDirection"] = new JsonObject
                {
                    ["x"] = b.DensityNoiseDirection?.X ?? 0.0,
                    ["y"] = b.DensityNoiseDirection?.Y ?? 0.0,
                    ["z"] = b.DensityNoiseDirection?.Z ?? 0.0
                },
                ["DensityNoiseSpeed"] = b.DensityNoiseSpeed,
                ["DensityNoiseScale"] = b.DensityNoiseScale,
                ["DensityHeightAltitude"] = b.DensityHeightAltitude,
                ["DensityHeightRange"] = b.DensityHeightRange,
                ["DensityHeightMaxBoost"] = b.DensityHeightMaxBoost,
                ["Infection"] = b.Infection,
                ["name"] = b.Name,
                ["internalEnabled"] = b.InternalEnabled,
                ["persistentID"] = b.PersistentId
            });
        }

        return list;
    }

    private static List<JsonObject> SerializeComplexResourceSets(IEnumerable<ComplexResourceSetBlock> blocks)
    {
        var list = new List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            var node = JsonSerializer.SerializeToNode(b, JsonOptions)?.AsObject();
            if (node != null)
            {
                list.Add(node);
            }
        }

        return list;
    }

    private static List<JsonObject> SerializeConsumableDistributionBlocks(IEnumerable<ConsumableDistributionBlock> blocks)
    {
        var list = new List<JsonObject>();
        foreach (var b in blocks)
        {
            if (b == null) continue;
            var spawnData = new JsonArray();
            foreach (var item in b.SpawnData)
            {
                if (item == null) continue;
                spawnData.Add(new JsonObject
                {
                    ["ItemID"] = item.ItemId,
                    ["Weight"] = item.Weight
                });
            }

            list.Add(new JsonObject
            {
                ["SpawnsPerZone"] = b.SpawnsPerZone,
                ["ChanceToSpawnInResourceContainer"] = b.ChanceToSpawnInResourceContainer,
                ["SpawnData"] = spawnData,
                ["name"] = b.Name,
                ["internalEnabled"] = b.InternalEnabled,
                ["persistentID"] = b.PersistentId
            });
        }

        return list;
    }
}