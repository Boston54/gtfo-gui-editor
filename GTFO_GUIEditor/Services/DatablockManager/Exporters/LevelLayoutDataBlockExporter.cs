using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LevelLayoutDataBlockExporter : IDatablockExporter
{
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

    public void Export(ExportContext context)
    {
        Directory.CreateDirectory(context.LgTunerDirectory);
        Directory.CreateDirectory(context.SchemasDirectory);

        // Write schema file in .schemas
        File.WriteAllText(Path.Combine(context.SchemasDirectory, "LGTunerAdvanced.json"), LGTunerAdvancedSchema);

        var levelLayoutBlocks = new List<JsonObject>();
        var dimensionBlocks = new List<JsonObject>();

        foreach (var level in context.MainVm.LevelLayout.Levels)
        {
            if (level == null) continue;

            var gridState = level.GridState ?? new LevelLayoutGridState();

            // Build tree of room connections via BFS starting from root geomorph
            var roomParentInfo = new Dictionary<(string geoId, int areaIdx), (int prevX, int prevZ, int prevArea, int expanderId)>();
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

                                roomParentInfo[neighborKey] = (currGeo.GridX, -currGeo.GridY, currArea, expanderId);
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
                                roomParentInfo[neighborKey] = (currGeo.GridX, -currGeo.GridY, d.AreaFrom, d.ExpanderIndex);
                                queue.Enqueue((otherGeo, otherDoor.AreaFrom));
                            }
                        }
                    }
                }
            }

            // Determine sectors to export for this level
            var sectorConfigs = new List<(SectorType Sector, uint LayoutId, string Name, SectorSettings? Settings, List<int> Zones, bool IsMain)>();

            uint mainLayoutId = level.PersistentId > 0 ? level.PersistentId : context.LevelLayoutAlloc.Allocate();
            context.LevelLayoutAlloc.RegisterExisting(mainLayoutId);
            context.LayoutIds[level] = mainLayoutId;

            var mainZones = gridState.GetZonesForSector(SectorType.Main);
            if (mainZones.Count == 0) mainZones.Add(0);
            sectorConfigs.Add((SectorType.Main, mainLayoutId, level.Name, level.MainSector, mainZones, true));

            var secZones = gridState.GetZonesForSector(SectorType.Secondary);
            bool secExists = secZones.Count > 0 || level.HasSecondaryDoor || gridState.HasSecondaryDoor ||
                             (context.MainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.SecondaryLayerEnabled) == true);
            if (secExists)
            {
                uint secLayoutId = context.LevelLayoutAlloc.Allocate();
                context.SecondaryLayoutIds[level] = secLayoutId;
                if (secZones.Count == 0) secZones.Add(0);
                sectorConfigs.Add((SectorType.Secondary, secLayoutId, $"{level.Name}_Secondary", level.SecondarySector, secZones, false));
            }

            var thirdZones = gridState.GetZonesForSector(SectorType.Overload);
            bool thirdExists = thirdZones.Count > 0 || level.HasOverloadDoor || gridState.HasOverloadDoor ||
                               (context.MainVm.Rundown.SelectedRundown?.Expeditions.Any(e => e.LevelLayoutData == level.PersistentId && e.ThirdLayerEnabled) == true);
            if (thirdExists)
            {
                uint thirdLayoutId = context.LevelLayoutAlloc.Allocate();
                context.ThirdLayoutIds[level] = thirdLayoutId;
                if (thirdZones.Count == 0) thirdZones.Add(0);
                sectorConfigs.Add((SectorType.Overload, thirdLayoutId, $"{level.Name}_Third", level.OverloadSector, thirdZones, false));
            }

            // Build LevelLayoutDataBlock entries and LevelGen override configs for each sector
            foreach (var sectorCfg in sectorConfigs)
            {
                var sectorZones = sectorCfg.Zones;
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
                            uint frontId = context.SynthesizeEnemyGroup(enteringDoor.EnemyGroupInFrontOfDoor,
                                $"InFront_{sectorCfg.LayoutId}_Z{zoneNumber}");
                            uint areaId = context.SynthesizeEnemyGroup(enteringDoor.EnemyGroupInArea,
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
                                    ? context.SynthesizeText(enteringDoor.ProgressionPuzzleCustomText, "Progression Puzzle Custom Text")
                                    : 0;
                            progPuzzleObj["PlacementCount"] = Math.Max(1, enteringDoor.ProgressionPuzzlePlacementCount);

                            var puzzlePlacementsArray = new JsonArray();
                            foreach (var cz in enteringDoor.ProgressionPuzzleCandidateZones)
                            {
                                if (cz == null) continue;
                                puzzlePlacementsArray.Add(new JsonObject
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
                            progPuzzleObj["ZonePlacementData"] = puzzlePlacementsArray;
                        }

                        if (enteringDoor.IsSecurityDoor || enteringDoor.IsApexDoor || enteringDoor.ChainedPuzzleToSolve > 0)
                        {
                            securityGateToEnter = enteringDoor.IsApexDoor ? "Apex" : "Security";
                            chainedPuzzleToEnter = enteringDoor.ChainedPuzzleToSolve;
                        }
                    }

                    // Terminal placements
                    var terminalsArray = new JsonArray();
                    foreach (var tp in zoneSettings.TerminalPlacements)
                    {
                        if (tp == null) continue;

                        var zoneSelections = new JsonArray();
                        foreach (var zs in tp.TerminalZoneSelectionDatas)
                        {
                            if (zs == null) continue;
                            zoneSelections.Add(new JsonObject
                            {
                                ["ZoneIndex"] = $"Zone_{zs.Zone}",
                                ["ItemIndex"] = zs.Terminal
                            });
                        }

                        var uniqueCommands = new JsonArray();
                        foreach (var uc in tp.UniqueCommands)
                        {
                            if (uc == null) continue;
                            var cmdObj = new JsonObject
                            {
                                ["Command"] = uc.Command ?? string.Empty,
                                ["CommandDesc"] = uc.CommandDesc ?? string.Empty,
                                ["CommandRule"] = "Normal",
                                ["SpecialCommand"] = "None"
                            };

                            var postOutputs = new JsonArray();
                            foreach (var po in uc.PostCommandOutputs)
                            {
                                postOutputs.Add(new JsonObject
                                {
                                    ["LineType"] = po.LineType.ToString(),
                                    ["Time"] = po.Time,
                                    ["Output"] = context.SynthesizeText(po.Output, "Post Command Output")
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
                                ? context.SynthesizeText(tp.CustomInfoText, "Terminal Custom Info")
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
                        : (context.MainVm.Lighting.Blocks.FirstOrDefault()?.PersistentId ?? 1);

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
                        ["Altitude"] = "Mid",
                        ["AltitudeData"] = new JsonObject
                        {
                            ["AllowedAltitude"] = "Mid",
                            ["IncludeInBuildFromAggregation"] = false
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
                            ["ZoneIndex"] = "Zone_0",
                            ["ItemIndex"] = 0
                        },
                        ["TerminalZoneSelectionData"] = new JsonObject
                        {
                            ["ZoneIndex"] = "Zone_0",
                            ["ItemIndex"] = 0
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
                        ["HealthPlacement"] = new JsonObject
                        {
                            ["Start"] = zoneSettings.MedKitPlacementStart,
                            ["Middle"] = zoneSettings.MedKitPlacementMiddle,
                            ["End"] = zoneSettings.MedKitPlacementEnd
                        },
                        ["WeaponAmmoMulti"] = zoneSettings.AmmoKitUses * 0.25,
                        ["WeaponAmmoPlacement"] = new JsonObject
                        {
                            ["Start"] = zoneSettings.AmmoKitPlacementStart,
                            ["Middle"] = zoneSettings.AmmoKitPlacementMiddle,
                            ["End"] = zoneSettings.AmmoKitPlacementEnd
                        },
                        ["ToolAmmoMulti"] = zoneSettings.ToolKitUses * 0.25,
                        ["ToolAmmoPlacement"] = new JsonObject
                        {
                            ["Start"] = zoneSettings.ToolKitPlacementStart,
                            ["Middle"] = zoneSettings.ToolKitPlacementMiddle,
                            ["End"] = zoneSettings.ToolKitPlacementEnd
                        },
                        ["DisinfectionMulti"] = zoneSettings.DisinfectKitUses * 0.25,
                        ["DisinfectionPlacement"] = new JsonObject
                        {
                            ["Start"] = zoneSettings.DisinfectKitPlacementStart,
                            ["Middle"] = zoneSettings.DisinfectKitPlacementMiddle,
                            ["End"] = zoneSettings.DisinfectKitPlacementEnd
                        },
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
                File.WriteAllText(Path.Combine(context.LgTunerDirectory, levelGenFileName),
                    levelGenOverrideObj.ToJsonString(ExportContext.JsonOptions));
                context.LevelGenFilesExportedCount++;
            }

            // Compile Dimensions & Dimension ComplexResourceSets
            foreach (var dim in level.Dimensions)
            {
                uint dimId = context.DimensionAlloc.Allocate();

                var dimGeos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(dim.DimensionGeomorph))
                {
                    dimGeos.Add(dim.DimensionGeomorph);
                }

                uint dimSetId = context.ComplexResourceAlloc.Allocate();

                var dimObj = new JsonObject
                {
                    ["DimensionData"] = new JsonObject
                    {
                        ["LevelLayoutData"] = dim.LevelLayoutData > 0 ? dim.LevelLayoutData : mainLayoutId,
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
            var vanillaDim = ExportContext.LoadVanillaBlocksArray("DimensionDataBlock.json");
            dimensionBlocks.AddRange(vanillaDim);
        }

        context.LevelLayoutBlocks.Clear();
        context.LevelLayoutBlocks.AddRange(levelLayoutBlocks);

        context.DimensionBlocks.Clear();
        context.DimensionBlocks.AddRange(dimensionBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_LevelLayoutDataBlock_bin.json",
            context.LevelLayoutBlocks);
        context.DatablocksExportedCount++;

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_DimensionDataBlock_bin.json",
            context.DimensionBlocks);
        context.DatablocksExportedCount++;
    }

    private static string ExtractComplexName(string prefabPath, ComplexType fallback)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return fallback.ToString();
        if (prefabPath.Contains("Mining", StringComparison.OrdinalIgnoreCase)) return "Mining";
        if (prefabPath.Contains("Service", StringComparison.OrdinalIgnoreCase)) return "Service";
        if (prefabPath.Contains("Tech", StringComparison.OrdinalIgnoreCase)) return "Tech";
        return fallback.ToString();
    }
}
