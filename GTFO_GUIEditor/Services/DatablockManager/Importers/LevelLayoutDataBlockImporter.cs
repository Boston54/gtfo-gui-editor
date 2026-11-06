using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class LevelLayoutDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var importedLevels = new List<LevelLayoutBlock>();
        var importedLayoutIds = new HashSet<uint>();

        // Find all expeditions from loaded rundown datablocks
        var expeditionsToProcess = GetExpeditionsToProcess(context);

        foreach (var exp in expeditionsToProcess)
        {
            uint mainLayoutId = exp.LevelLayoutData;
            string levelName = exp.Descriptive.PublicName;
            if (string.IsNullOrWhiteSpace(levelName))
            {
                levelName = !string.IsNullOrWhiteSpace(exp.Descriptive.Prefix) ? exp.Descriptive.Prefix : $"Level_{mainLayoutId}";
            }

            var missingConfigs = new List<string>();

            // 1. Check Main Sector LGTuner config
            string? mainLgTunerFile = context.FindLgTunerConfigFile(mainLayoutId.ToString());
            if (mainLgTunerFile == null && !string.IsNullOrWhiteSpace(exp.Descriptive.PublicName))
            {
                mainLgTunerFile = context.FindLgTunerConfigFile(exp.Descriptive.PublicName);
            }
            if (mainLgTunerFile == null && !string.IsNullOrWhiteSpace(exp.Descriptive.Prefix))
            {
                mainLgTunerFile = context.FindLgTunerConfigFile(exp.Descriptive.Prefix);
            }
            if (mainLgTunerFile == null && context.LevelLayoutBlocks.TryGetValue(mainLayoutId, out var layoutBlock) && layoutBlock != null)
            {
                string layoutBlockName = layoutBlock["name"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(layoutBlockName))
                {
                    mainLgTunerFile = context.FindLgTunerConfigFile(layoutBlockName);
                }
            }

            // Fallback: If layout ID was from a stale template or mismatched, check if there is an unused LevelLayoutBlock in context
            if (mainLgTunerFile == null && !context.LevelLayoutBlocks.ContainsKey(mainLayoutId))
            {
                var matchingFallbackKvp = context.LevelLayoutBlocks.FirstOrDefault(kvp => context.FindLgTunerConfigFile(kvp.Key.ToString()) != null);
                if (matchingFallbackKvp.Value != null)
                {
                    mainLayoutId = matchingFallbackKvp.Key;
                    exp.LevelLayoutData = mainLayoutId;
                    mainLgTunerFile = context.FindLgTunerConfigFile(mainLayoutId.ToString());
                }
            }

            if (mainLgTunerFile == null || !File.Exists(mainLgTunerFile))
            {
                missingConfigs.Add($"Main Sector (Layout ID: {mainLayoutId})");
            }

            // 2. Check Secondary Sector LGTuner config
            if (exp.SecondaryLayerEnabled && exp.SecondaryLayout > 0)
            {
                string? secLgTunerFile = context.FindLgTunerConfigFile(exp.SecondaryLayout.ToString());
                if (secLgTunerFile == null || !File.Exists(secLgTunerFile))
                {
                    missingConfigs.Add($"Secondary Sector (Layout ID: {exp.SecondaryLayout})");
                }
            }

            // 3. Check Overload / Third Sector LGTuner config
            if (exp.ThirdLayerEnabled && exp.ThirdLayout > 0)
            {
                string? thirdLgTunerFile = context.FindLgTunerConfigFile(exp.ThirdLayout.ToString());
                if (thirdLgTunerFile == null || !File.Exists(thirdLgTunerFile))
                {
                    missingConfigs.Add($"Overload Sector (Layout ID: {exp.ThirdLayout})");
                }
            }

            // 4. Check Dimensions LGTuner configs
            CheckDimensionConfigsForExpedition(exp, context, missingConfigs);

            if (missingConfigs.Count > 0)
            {
                var failure = new LevelImportFailure
                {
                    LevelName = levelName,
                    Prefix = exp.Descriptive.Prefix,
                    PublicName = exp.Descriptive.PublicName,
                    MainLayoutId = mainLayoutId,
                    MissingConfigs = missingConfigs
                };

                context.FailedLevels.Add(failure);
                context.SkippedLevels.Add(failure.FormattedDescription);
                context.Warnings.Add(failure.FormattedDescription);
                continue;
            }

            try
            {
                var level = ImportSingleLevel(exp, mainLgTunerFile!, context);
                if (level != null)
                {
                    importedLevels.Add(level);
                    importedLayoutIds.Add(level.PersistentId);
                    context.LevelsImportedCount++;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to import level '{levelName}': {ex.Message}";
                context.Warnings.Add(errorMsg);
            }
        }

        // LevelLayout datablocks may be imported without a rundown/GameSetup reference.
        // In that case, import each layout that has a matching LevelGen_<layout ID>.json file.
        foreach (var layout in context.LevelLayoutBlocks)
        {
            if (importedLayoutIds.Contains(layout.Key)) continue;

            string? lgTunerFile = context.FindLgTunerConfigFile(layout.Key.ToString());
            if (lgTunerFile == null && layout.Value?["name"] != null)
            {
                lgTunerFile = context.FindLgTunerConfigFile(layout.Value["name"]!.ToString());
            }
            if (lgTunerFile == null || !File.Exists(lgTunerFile)) continue;

            string levelName = layout.Value?["name"]?.ToString() ?? $"Level_{layout.Key}";
            var fallbackExpedition = new ExpeditionBlock
            {
                Enabled = true,
                LevelLayoutData = layout.Key,
                Descriptive = new ExpeditionDescriptiveData
                {
                    Prefix = "EXP",
                    PublicName = levelName
                }
            };

            try
            {
                var level = ImportSingleLevel(fallbackExpedition, lgTunerFile, context);
                if (level != null)
                {
                    importedLevels.Add(level);
                    importedLayoutIds.Add(level.PersistentId);
                    context.LevelsImportedCount++;
                }
            }
            catch (Exception ex)
            {
                context.Warnings.Add($"Failed to import level '{levelName}': {ex.Message}");
            }
        }

        context.MainVm.LevelLayout.Levels.Clear();
        foreach (var level in importedLevels)
        {
            context.MainVm.LevelLayout.Levels.Add(level);
        }

        if (context.MainVm.LevelLayout.Levels.Count > 0)
        {
            context.MainVm.LevelLayout.SelectedLevel = context.MainVm.LevelLayout.Levels[0];
        }
    }

    private void CheckDimensionConfigsForExpedition(ExpeditionBlock exp, ImportContext context, List<string> missingConfigs)
    {
        var checkedLayoutIds = new HashSet<uint>();

        if (exp.DimensionDatas.HasValue && exp.DimensionDatas.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in exp.DimensionDatas.Value.EnumerateArray())
            {
                string dimIndex = "Dimension_1";
                uint dimDataId = 0;

                if (item.TryGetProperty("DimensionIndex", out var diProp))
                {
                    dimIndex = diProp.GetString() ?? "Dimension_1";
                }
                if (item.TryGetProperty("DimensionData", out var ddProp))
                {
                    if (ddProp.ValueKind == JsonValueKind.Number)
                    {
                        ddProp.TryGetUInt32(out dimDataId);
                    }
                }

                if (dimDataId > 0 && context.DimensionBlocks.TryGetValue(dimDataId, out var dimBlock) && dimBlock != null)
                {
                    var inner = dimBlock["DimensionData"] as JsonObject;
                    string dimName = dimBlock["name"]?.ToString() ?? dimIndex;
                    uint dimLayoutId = inner?["LevelLayoutData"]?.GetValue<uint>() ?? dimBlock["LevelLayoutData"]?.GetValue<uint>() ?? 0;

                    if (dimLayoutId > 0 && checkedLayoutIds.Add(dimLayoutId))
                    {
                        string? dimLgTunerPath = context.FindLgTunerConfigFile(dimLayoutId.ToString());
                        if (dimLgTunerPath == null && !string.IsNullOrWhiteSpace(dimName))
                        {
                            dimLgTunerPath = context.FindLgTunerConfigFile(dimName);
                        }

                        if (dimLgTunerPath == null || !File.Exists(dimLgTunerPath))
                        {
                            missingConfigs.Add($"Dimension '{dimName}' (Layout ID: {dimLayoutId})");
                        }
                    }
                }
            }
        }
    }

    private List<ExpeditionBlock> GetExpeditionsToProcess(ImportContext context)
    {
        var result = new List<ExpeditionBlock>();

        // Try to get active rundown from GameSetup or first RundownBlock
        JsonObject? activeRundownObj = null;
        if (context.GameSetupBlocks.Values.FirstOrDefault() is JsonObject gameSetup &&
            gameSetup["RundownIdsToLoad"] is JsonArray rundownIds && rundownIds.Count > 0)
        {
            uint targetRundownId = rundownIds[0]?.GetValue<uint>() ?? 1;
            activeRundownObj = context.RundownBlocks.GetValueOrDefault(targetRundownId);
        }

        if (activeRundownObj == null && context.RundownBlocks.Count > 0)
        {
            activeRundownObj = context.RundownBlocks.Values.FirstOrDefault();
        }

        var processedLayoutIds = new HashSet<uint>();

        if (activeRundownObj != null)
        {
            ExtractTierExpeditions(activeRundownObj["TierA"] as JsonArray, result, context);
            ExtractTierExpeditions(activeRundownObj["TierB"] as JsonArray, result, context);
            ExtractTierExpeditions(activeRundownObj["TierC"] as JsonArray, result, context);
            ExtractTierExpeditions(activeRundownObj["TierD"] as JsonArray, result, context);
            ExtractTierExpeditions(activeRundownObj["TierE"] as JsonArray, result, context);

            foreach (var exp in result)
            {
                if (exp.LevelLayoutData > 0) processedLayoutIds.Add(exp.LevelLayoutData);
                if (exp.SecondaryLayout > 0) processedLayoutIds.Add(exp.SecondaryLayout);
                if (exp.ThirdLayout > 0) processedLayoutIds.Add(exp.ThirdLayout);

                var dimDatas = GetDimensionDatasFromExpedition(exp, context);
                foreach (var dim in dimDatas)
                {
                    if (dim.LevelLayoutData > 0) processedLayoutIds.Add(dim.LevelLayoutData);
                }
            }
        }

        // Also check if there are LevelLayoutBlocks not covered by the expeditions (or if no active rundown)
        foreach (var kvp in context.LevelLayoutBlocks)
        {
            if (processedLayoutIds.Contains(kvp.Key)) continue;

            // Check if this layout ID is a secondary/third layout or dimension layout of an existing expedition
            bool isSubSector = result.Any(e => e.SecondaryLayout == kvp.Key || e.ThirdLayout == kvp.Key ||
                GetDimensionDatasFromExpedition(e, context).Any(d => d.LevelLayoutData == kvp.Key));
            if (isSubSector) continue;

            // Check if this layout ID is referenced by any DimensionDataBlock
            bool isDimBlock = context.DimensionBlocks.Values.Any(dObj =>
            {
                var inner = dObj?["DimensionData"] as JsonObject;
                uint dimLayId = inner?["LevelLayoutData"]?.GetValue<uint>() ?? dObj?["LevelLayoutData"]?.GetValue<uint>() ?? 0;
                return dimLayId == kvp.Key;
            });
            if (isDimBlock) continue;

            // Only add if an LGTuner config file actually exists for this layout!
            string? cfgFile = context.FindLgTunerConfigFile(kvp.Key.ToString());
            if (cfgFile == null && kvp.Value?["name"] != null)
            {
                cfgFile = context.FindLgTunerConfigFile(kvp.Value["name"]!.ToString());
            }

            if (cfgFile != null)
            {
                result.Add(new ExpeditionBlock
                {
                    Enabled = true,
                    LevelLayoutData = kvp.Key,
                    Descriptive = new ExpeditionDescriptiveData
                    {
                        Prefix = "EXP",
                        PublicName = kvp.Value?["name"]?.ToString() ?? $"Level_{kvp.Key}"
                    }
                });
                processedLayoutIds.Add(kvp.Key);
            }
        }

        return result;
    }

    private void ExtractTierExpeditions(JsonArray? tierArray, List<ExpeditionBlock> target, ImportContext context)
    {
        if (tierArray == null) return;

        foreach (var node in tierArray)
        {
            if (node is not JsonObject expObj) continue;

            bool enabled = expObj["Enabled"]?.GetValue<bool>() ?? true;
            uint mainLayout = expObj["LevelLayoutData"]?.GetValue<uint>() ?? 0;
            uint secLayout = expObj["SecondaryLayout"]?.GetValue<uint>() ?? 0;
            uint thirdLayout = expObj["ThirdLayout"]?.GetValue<uint>() ?? 0;
            bool secEnabled = expObj["SecondaryLayerEnabled"]?.GetValue<bool>() ?? false;
            bool thirdEnabled = expObj["ThirdLayerEnabled"]?.GetValue<bool>() ?? false;

            var descData = new ExpeditionDescriptiveData();
            if (expObj["Descriptive"] is JsonObject descObj)
            {
                string rawPrefix = descObj["Prefix"]?.ToString() ?? string.Empty;
                if (uint.TryParse(rawPrefix, out uint prefixId) && prefixId > 0)
                {
                    string res = context.ResolveText(prefixId);
                    descData.Prefix = !string.IsNullOrEmpty(res) ? res : rawPrefix;
                }
                else
                {
                    descData.Prefix = rawPrefix;
                }

                string rawPublic = descObj["PublicName"]?.ToString() ?? string.Empty;
                if (uint.TryParse(rawPublic, out uint pubId) && pubId > 0)
                {
                    string res = context.ResolveText(pubId);
                    descData.PublicName = !string.IsNullOrEmpty(res) ? res : rawPublic;
                }
                else
                {
                    descData.PublicName = rawPublic;
                }

                descData.IsExtraExpedition = descObj["IsExtraExpedition"]?.GetValue<bool>() ?? false;
            }

            var expData = new ExpeditionSettings();
            if (expObj["Expedition"] is JsonObject innerExpObj)
            {
                expData.ComplexResourceData = innerExpObj["ComplexResourceData"]?.GetValue<uint>() ?? 1;
                expData.LightSettings = innerExpObj["LightSettings"]?.GetValue<uint>() ?? 1;
                expData.FogSettings = innerExpObj["FogSettings"]?.GetValue<uint>() ?? 1;
                expData.ExpeditionBalance = innerExpObj["ExpeditionBalance"]?.GetValue<uint>() ?? 1;
            }

            uint mainWardenObj = 0;
            if (expObj["MainLayerData"]?["ObjectiveData"] is JsonObject mObj)
            {
                mainWardenObj = mObj["DataBlockId"]?.GetValue<uint>() ?? 0;
            }
            if (mainWardenObj == 0 && expObj["Expedition"] is JsonObject innerExp)
            {
                mainWardenObj = innerExp["WardenObjective"]?.GetValue<uint>() ?? 0;
            }

            var buildSec = new BuildFromData();
            if (expObj["BuildSecondaryFrom"] is JsonObject bSecObj)
            {
                buildSec.LayerType = bSecObj["LayerType"]?.ToString() ?? "MainLayer";
                buildSec.Zone = bSecObj["Zone"]?.ToString() ?? "Zone_0";
            }

            var buildThird = new BuildFromData();
            if (expObj["BuildThirdFrom"] is JsonObject bThirdObj)
            {
                buildThird.LayerType = bThirdObj["LayerType"]?.ToString() ?? "MainLayer";
                buildThird.Zone = bThirdObj["Zone"]?.ToString() ?? "Zone_0";
            }

            string accStr = expObj["Accessibility"]?.ToString() ?? "Normal";
            Enum.TryParse<ExpeditionAccessibility>(accStr, true, out var accessibility);

            var expBlock = new ExpeditionBlock
            {
                Enabled = enabled,
                LevelLayoutData = mainLayout,
                SecondaryLayout = secLayout,
                ThirdLayout = thirdLayout,
                SecondaryLayerEnabled = secEnabled,
                ThirdLayerEnabled = thirdEnabled,
                Descriptive = descData,
                Expedition = expData,
                BuildSecondaryFrom = buildSec,
                BuildThirdFrom = buildThird,
                Accessibility = accessibility
            };

            expBlock.MainLayerData.ObjectiveData.DataBlockId = mainWardenObj;

            if (expObj["MainLayerData"] is JsonObject mainLayerObj)
            {
                expBlock.MainLayerData.BulkheadDoorControllerPlacements = DeserializeJsonElement(mainLayerObj["BulkheadDoorControllerPlacements"]);
                expBlock.MainLayerData.BulkheadKeyPlacements = DeserializeJsonElement(mainLayerObj["BulkheadKeyPlacements"]);
                expBlock.MainLayerData.ZonesWithBulkheadEntrance = DeserializeJsonElement(mainLayerObj["ZonesWithBulkheadEntrance"]);
                expBlock.MainLayerData.ArtifactData = DeserializeJsonElement(mainLayerObj["ArtifactData"]);
                expBlock.MainLayerData.ChainedObjectiveData = DeserializeJsonElement(mainLayerObj["ChainedObjectiveData"]);
            }

            if (expObj["SecondaryLayerData"] is JsonObject secLayerObj)
            {
                expBlock.SecondaryLayerData.BulkheadDoorControllerPlacements = DeserializeJsonElement(secLayerObj["BulkheadDoorControllerPlacements"]);
                expBlock.SecondaryLayerData.BulkheadKeyPlacements = DeserializeJsonElement(secLayerObj["BulkheadKeyPlacements"]);
                expBlock.SecondaryLayerData.ZonesWithBulkheadEntrance = DeserializeJsonElement(secLayerObj["ZonesWithBulkheadEntrance"]);
                expBlock.SecondaryLayerData.ArtifactData = DeserializeJsonElement(secLayerObj["ArtifactData"]);
                expBlock.SecondaryLayerData.ChainedObjectiveData = DeserializeJsonElement(secLayerObj["ChainedObjectiveData"]);
            }

            if (expObj["ThirdLayerData"] is JsonObject thirdLayerObj)
            {
                expBlock.ThirdLayerData.BulkheadDoorControllerPlacements = DeserializeJsonElement(thirdLayerObj["BulkheadDoorControllerPlacements"]);
                expBlock.ThirdLayerData.BulkheadKeyPlacements = DeserializeJsonElement(thirdLayerObj["BulkheadKeyPlacements"]);
                expBlock.ThirdLayerData.ZonesWithBulkheadEntrance = DeserializeJsonElement(thirdLayerObj["ZonesWithBulkheadEntrance"]);
                expBlock.ThirdLayerData.ArtifactData = DeserializeJsonElement(thirdLayerObj["ArtifactData"]);
                expBlock.ThirdLayerData.ChainedObjectiveData = DeserializeJsonElement(thirdLayerObj["ChainedObjectiveData"]);
            }

            if (expObj["SecondaryLayerData"]?["ObjectiveData"] is JsonObject secObjNode)
            {
                expBlock.SecondaryLayerData.ObjectiveData.DataBlockId = secObjNode["DataBlockId"]?.GetValue<uint>() ?? 0;
            }
            else if (expObj["Expedition"] is JsonObject innerE2)
            {
                expBlock.SecondaryLayerData.ObjectiveData.DataBlockId = innerE2["CustomWardenObjectiveSecondary"]?.GetValue<uint>() ?? 0;
            }

            if (expObj["ThirdLayerData"]?["ObjectiveData"] is JsonObject thirdObjNode)
            {
                expBlock.ThirdLayerData.ObjectiveData.DataBlockId = thirdObjNode["DataBlockId"]?.GetValue<uint>() ?? 0;
            }
            else if (expObj["Expedition"] is JsonObject innerE3)
            {
                expBlock.ThirdLayerData.ObjectiveData.DataBlockId = innerE3["CustomWardenObjectiveThird"]?.GetValue<uint>() ?? 0;
            }

            if (expObj["DimensionDatas"] != null)
            {
                expBlock.DimensionDatas = DeserializeJsonElement(expObj["DimensionDatas"]);
            }

            target.Add(expBlock);
        }
    }

    private static JsonElement? DeserializeJsonElement(JsonNode? node)
    {
        if (node == null) return null;
        return JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());
    }

    private LevelLayoutBlock ImportSingleLevel(ExpeditionBlock exp, string mainLgTunerPath, ImportContext context)
    {
        string levelName = string.Empty;
        if (context.LevelLayoutBlocks.TryGetValue(exp.LevelLayoutData, out var layoutObj) && layoutObj != null)
        {
            levelName = layoutObj["name"]?.ToString() ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(levelName) || levelName.StartsWith("LevelLayout_"))
        {
            levelName = exp.Descriptive.PublicName;
        }
        if (string.IsNullOrWhiteSpace(levelName))
        {
            levelName = !string.IsNullOrWhiteSpace(exp.Descriptive.Prefix) ? exp.Descriptive.Prefix : $"Level_{exp.LevelLayoutData}";
        }

        var level = new LevelLayoutBlock
        {
            Name = levelName,
            PersistentId = exp.LevelLayoutData
        };

        // 1. Complex Resource Set & Elevator Tile
        uint crsId = exp.Expedition.ComplexResourceData > 0 ? exp.Expedition.ComplexResourceData : 1;
        if (context.ComplexResourceSetBlocks.TryGetValue(crsId, out var crsObj) && crsObj != null)
        {
            string ctStr = crsObj["ComplexType"]?.ToString() ?? "Tech";
            if (Enum.TryParse<ComplexType>(ctStr, true, out var ct))
            {
                level.ComplexType = ct;
            }

            if (crsObj["ElevatorShafts_1x1"] is JsonArray elevArr && elevArr.Count > 0 &&
                elevArr[0] is JsonObject elevObj && elevObj.TryGetPropertyValue("Prefab", out var pNode) && pNode != null)
            {
                level.ElevatorTile = pNode.ToString();
            }
        }

        // 2. Expedition Balance
        uint expBalId = exp.Expedition.ExpeditionBalance > 0 ? exp.Expedition.ExpeditionBalance : 1;
        if (context.ExpeditionBalanceBlocks.TryGetValue(expBalId, out var expBalObj) && expBalObj != null)
        {
            level.ExpeditionBalance = DeserializeExpeditionBalance(expBalObj);
        }

        // 3. Dimensions
        if (exp.DimensionDatas.HasValue && exp.DimensionDatas.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in exp.DimensionDatas.Value.EnumerateArray())
            {
                if (item.TryGetProperty("DimensionIndex", out var diProp) &&
                    string.Equals(diProp.GetString(), "ARENA_DIMENSION", StringComparison.OrdinalIgnoreCase))
                {
                    bool arenaEnabled = true;
                    if (item.TryGetProperty("Enabled", out var enProp))
                    {
                        arenaEnabled = enProp.GetBoolean();
                    }
                    level.SnatcherBoxes = arenaEnabled;
                }
            }
        }

        var dimDatas = GetDimensionDatasFromExpedition(exp, context);
        foreach (var dimItem in dimDatas)
        {
            level.Dimensions.Add(dimItem);
        }

        // 4. Objectives
        ImportObjectives(level, exp, context);

        // 5. Grid State & Geomorphs
        var gridState = level.GridState;
        gridState.ImportStateData(new LevelLayoutGridStateData());

        // Load Main LGTuner config
        string mainLgTunerJson = File.ReadAllText(mainLgTunerPath);
        var mainLgTunerObj = JsonNode.Parse(mainLgTunerJson) as JsonObject;
        var mainGeoOverrides = mainLgTunerObj?["GeoOverrides"] as JsonArray;
        var mainZoneOverrides = mainLgTunerObj?["ZoneOverrides"] as JsonArray;

        // Place Elevator Root Tile
        string elevatorTile = level.ElevatorTile;
        if (string.IsNullOrWhiteSpace(elevatorTile))
        {
            elevatorTile = "Assets/AssetPrefabs/Complex/Tech/Geomorphs/ElevatorShaft_01.prefab";
        }
        gridState.InitializeStartingTile(elevatorTile, 0, 0);

        var generatedRooms = new HashSet<(string geoId, int areaIdx)>();
        if (gridState.PlacedGeomorphs.Count > 0)
        {
            generatedRooms.Add((gridState.PlacedGeomorphs[0].Id, 0));
        }

        // Place Main Geomorphs from LGTuner GeoOverrides
        if (mainGeoOverrides != null)
        {
            foreach (var gNode in mainGeoOverrides)
            {
                if (gNode is not JsonObject gObj) continue;
                PlaceGeomorphFromOverride(gridState, gObj);
            }
        }

        // Place Secondary Geomorphs if Secondary layout is enabled
        JsonArray? secZoneOverrides = null;
        if (exp.SecondaryLayerEnabled && exp.SecondaryLayout > 0)
        {
            string? secLgTunerPath = context.FindLgTunerConfigFile(exp.SecondaryLayout.ToString());
            if (secLgTunerPath != null && File.Exists(secLgTunerPath))
            {
                string secLgTunerJson = File.ReadAllText(secLgTunerPath);
                var secLgTunerObj = JsonNode.Parse(secLgTunerJson) as JsonObject;
                var secGeoOverrides = secLgTunerObj?["GeoOverrides"] as JsonArray;
                secZoneOverrides = secLgTunerObj?["ZoneOverrides"] as JsonArray;

                if (secGeoOverrides != null)
                {
                    foreach (var gNode in secGeoOverrides)
                    {
                        if (gNode is not JsonObject gObj) continue;
                        PlaceGeomorphFromOverride(gridState, gObj);
                    }
                }
            }
        }

        // Place Overload Geomorphs if Third layout is enabled
        JsonArray? thirdZoneOverrides = null;
        if (exp.ThirdLayerEnabled && exp.ThirdLayout > 0)
        {
            string? thirdLgTunerPath = context.FindLgTunerConfigFile(exp.ThirdLayout.ToString());
            if (thirdLgTunerPath != null && File.Exists(thirdLgTunerPath))
            {
                string thirdLgTunerJson = File.ReadAllText(thirdLgTunerPath);
                var thirdLgTunerObj = JsonNode.Parse(thirdLgTunerJson) as JsonObject;
                var thirdGeoOverrides = thirdLgTunerObj?["GeoOverrides"] as JsonArray;
                thirdZoneOverrides = thirdLgTunerObj?["ZoneOverrides"] as JsonArray;

                if (thirdGeoOverrides != null)
                {
                    foreach (var gNode in thirdGeoOverrides)
                    {
                        if (gNode is not JsonObject gObj) continue;
                        PlaceGeomorphFromOverride(gridState, gObj);
                    }
                }
            }
        }

        // 6. Process Sector Zones & Area Overrides (Room creation & Sector/Zone mapping)
        var sectorZoneList = new List<(SectorType Sector, JsonArray ZonesArray, JsonArray? ZoneOverridesArray)>();
        var zoneEntranceDoorMap = new Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>();

        if (context.LevelLayoutBlocks.TryGetValue(exp.LevelLayoutData, out var mainLayoutObj) &&
            mainLayoutObj?["Zones"] is JsonArray mainZones)
        {
            level.MainSector.ZoneAliasStart = mainLayoutObj["ZoneAliasStart"]?.GetValue<int>() ?? 0;
            sectorZoneList.Add((SectorType.Main, mainZones, mainZoneOverrides));
            ProcessSectorZoneSettingsAndRooms(gridState, SectorType.Main, mainZones, mainZoneOverrides, generatedRooms, context, zoneEntranceDoorMap);
        }

        if (exp.SecondaryLayerEnabled && exp.SecondaryLayout > 0 &&
            context.LevelLayoutBlocks.TryGetValue(exp.SecondaryLayout, out var secLayoutObj) &&
            secLayoutObj?["Zones"] is JsonArray secZones)
        {
            level.SecondarySector.ZoneAliasStart = secLayoutObj["ZoneAliasStart"]?.GetValue<int>() ?? 100;
            sectorZoneList.Add((SectorType.Secondary, secZones, secZoneOverrides));
            ProcessSectorZoneSettingsAndRooms(gridState, SectorType.Secondary, secZones, secZoneOverrides, generatedRooms, context, zoneEntranceDoorMap);
        }

        if (exp.ThirdLayerEnabled && exp.ThirdLayout > 0 &&
            context.LevelLayoutBlocks.TryGetValue(exp.ThirdLayout, out var thirdLayoutObj) &&
            thirdLayoutObj?["Zones"] is JsonArray thirdZones)
        {
            level.OverloadSector.ZoneAliasStart = thirdLayoutObj["ZoneAliasStart"]?.GetValue<int>() ?? 200;
            sectorZoneList.Add((SectorType.Overload, thirdZones, thirdZoneOverrides));
            ProcessSectorZoneSettingsAndRooms(gridState, SectorType.Overload, thirdZones, thirdZoneOverrides, generatedRooms, context, zoneEntranceDoorMap);
        }

        // 7. Auto-connect adjacent external doors
        ConnectAdjacentDoors(gridState);

        // 8. Configure Zone Security Doors, Blood Doors, Alarms, and Progression Puzzles
        foreach (var (sector, zonesArray, _) in sectorZoneList)
        {
            ConfigureZoneDoors(gridState, sector, zonesArray, context, zoneEntranceDoorMap);
        }

        // 9. Mark Bulkhead Doors (Secondary / Overload)
        MarkBulkheadDoors(gridState, exp, zoneEntranceDoorMap);

        // 10. Import Door Controller Placements (DCs)
        ImportDCsForSector(gridState, SectorType.Main, exp.MainLayerData.BulkheadDoorControllerPlacements);
        ImportDCsForSector(gridState, SectorType.Secondary, exp.SecondaryLayerData.BulkheadDoorControllerPlacements);
        ImportDCsForSector(gridState, SectorType.Overload, exp.ThirdLayerData.BulkheadDoorControllerPlacements);

        // 11. Import Bulkhead Keys
        ImportBulkheadKeysForSector(level.MainSector, exp.MainLayerData.BulkheadKeyPlacements);
        ImportBulkheadKeysForSector(level.SecondarySector, exp.SecondaryLayerData.BulkheadKeyPlacements);
        ImportBulkheadKeysForSector(level.OverloadSector, exp.ThirdLayerData.BulkheadKeyPlacements);

        // 12. Import Artifact Data Multipliers
        ImportArtifactDataForSector(level.MainSector, exp.MainLayerData.ArtifactData);
        ImportArtifactDataForSector(level.SecondarySector, exp.SecondaryLayerData.ArtifactData);
        ImportArtifactDataForSector(level.OverloadSector, exp.ThirdLayerData.ArtifactData);

        // 13. Update state & highlights
        gridState.UpdateRoomNumbers();
        gridState.RecalculateSecurityDoorConstraints();
        gridState.UpdateRoomHighlightStates();

        return level;
    }

    private List<DimensionLayoutBlock> GetDimensionDatasFromExpedition(ExpeditionBlock exp, ImportContext context)
    {
        var result = new List<DimensionLayoutBlock>();
        var seenIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (exp.DimensionDatas.HasValue && exp.DimensionDatas.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in exp.DimensionDatas.Value.EnumerateArray())
            {
                string dimIndex = "Dimension_1";
                uint dimDataId = 0;
                bool enabled = true;

                if (item.TryGetProperty("DimensionIndex", out var diProp))
                {
                    dimIndex = diProp.GetString() ?? "Dimension_1";
                }
                if (item.TryGetProperty("DimensionData", out var ddProp))
                {
                    if (ddProp.ValueKind == JsonValueKind.Number)
                    {
                        ddProp.TryGetUInt32(out dimDataId);
                    }
                }
                if (item.TryGetProperty("Enabled", out var enProp))
                {
                    enabled = enProp.GetBoolean();
                }

                if (string.Equals(dimIndex, "ARENA_DIMENSION", StringComparison.OrdinalIgnoreCase) || dimDataId == 14)
                {
                    continue;
                }

                if (dimDataId > 0 && context.DimensionBlocks.TryGetValue(dimDataId, out var dimBlock) && dimBlock != null)
                {
                    var inner = dimBlock["DimensionData"] as JsonObject;
                    string dimName = dimBlock["name"]?.ToString() ?? dimIndex;
                    string dimGeomorph = inner?["DimensionGeomorph"]?.ToString() ?? dimBlock["DimensionGeomorph"]?.ToString() ?? string.Empty;
                    uint dimLayoutId = inner?["LevelLayoutData"]?.GetValue<uint>() ?? dimBlock["LevelLayoutData"]?.GetValue<uint>() ?? 0;
                    uint dimResourceSetId = inner?["DimensionResourceSetID"]?.GetValue<uint>() ?? 0;

                    var complexType = ComplexType.Mining;
                    if (!string.IsNullOrWhiteSpace(dimGeomorph))
                    {
                        if (dimGeomorph.Contains("Tech", StringComparison.OrdinalIgnoreCase)) complexType = ComplexType.Tech;
                        else if (dimGeomorph.Contains("Service", StringComparison.OrdinalIgnoreCase)) complexType = ComplexType.Service;
                    }

                    string tileCat = dimGeomorph.Contains("Elevator", StringComparison.OrdinalIgnoreCase) ? "Elevator Shaft" : "Dimension Geomorph";

                    var dimLayoutBlock = new DimensionLayoutBlock
                    {
                        DimensionIndex = dimIndex,
                        Name = dimName,
                        InternalEnabled = enabled,
                        ComplexType = complexType,
                        ComplexResourceSet = dimResourceSetId,
                        DimensionGeomorph = dimGeomorph,
                        LevelLayoutData = dimLayoutId,
                        TileCategory = tileCat
                    };

                    PopulateDimensionFields(dimLayoutBlock, inner, context);

                    BuildDimensionGridState(dimLayoutBlock, context);
                    result.Add(dimLayoutBlock);
                    seenIndexes.Add(dimIndex);
                }
            }
        }

        if (result.Count > 0) return result;

        // Fallback: Check DimensionBlocks where DimensionData.LevelLayoutData matches
        foreach (var dimBlock in context.DimensionBlocks.Values)
        {
            if (dimBlock == null) continue;
            uint pid = dimBlock["persistentID"]?.GetValue<uint>() ?? 0;
            if (pid == 14) continue;
            var innerData = dimBlock["DimensionData"] as JsonObject;
            uint targetLayoutId = innerData?["LevelLayoutData"]?.GetValue<uint>() ?? 0;

            if (targetLayoutId == exp.LevelLayoutData && targetLayoutId > 0)
            {
                string dimName = dimBlock["name"]?.ToString() ?? $"Dimension_{dimBlock["persistentID"]}";
                string dimGeomorph = innerData?["DimensionGeomorph"]?.ToString() ?? string.Empty;
                var dimLayoutBlock = new DimensionLayoutBlock
                {
                    Name = dimName,
                    DimensionGeomorph = dimGeomorph,
                    LevelLayoutData = targetLayoutId
                };
                PopulateDimensionFields(dimLayoutBlock, innerData, context);
                BuildDimensionGridState(dimLayoutBlock, context);
                result.Add(dimLayoutBlock);
            }
        }

        return result;
    }

    private void PopulateDimensionFields(DimensionLayoutBlock dim, JsonObject? inner, ImportContext context)
    {
        if (inner == null) return;

        dim.ForbidWaveSpawning = inner["ForbidWaveSpawning"]?.GetValue<bool>() ?? false;

        string? linkedStr = inner["LinkedToLayer"]?.ToString();
        if (string.Equals(linkedStr, "SecondaryLayer", StringComparison.OrdinalIgnoreCase) || string.Equals(linkedStr, "Secondary", StringComparison.OrdinalIgnoreCase))
        {
            dim.LinkedToLayer = DimensionLinkedToLayer.Secondary;
        }
        else if (string.Equals(linkedStr, "ThirdLayer", StringComparison.OrdinalIgnoreCase) || string.Equals(linkedStr, "Overload", StringComparison.OrdinalIgnoreCase))
        {
            dim.LinkedToLayer = DimensionLinkedToLayer.Overload;
        }
        else
        {
            dim.LinkedToLayer = DimensionLinkedToLayer.Main;
        }

        string? objTypeStr = inner["ObjectiveType"]?.ToString();
        if (Enum.TryParse<DimensionObjectiveType>(objTypeStr, true, out var parsedObjType))
        {
            dim.ObjectiveType = parsedObjType;
        }

        dim.SoundEventOnWarpTo = inner["SoundEventOnWarpTo"]?.GetValue<uint>() ?? 0;
        dim.DisableVFXEventOnWarp = inner["DisableVFXEventOnWarp"]?.GetValue<bool>() ?? inner["DisableVFXOnWarp"]?.GetValue<bool>() ?? false;
        dim.ForbidCarryItemWarps = inner["ForbidCarryItemWarps"]?.GetValue<bool>() ?? false;
        dim.LeaveDeployablesOnWarp = inner["LeaveDeployablesOnWarp"]?.GetValue<bool>() ?? false;

        dim.DimensionFogData = inner["DimensionFogData"]?.GetValue<uint>() ?? 21;
        dim.EnvironmentWetness = inner["EnvironmentWetness"]?.GetValue<double>() ?? 0.0;
        if (inner["DustColor"] is JsonObject dustColorObj)
        {
            dim.DustColor.A = dustColorObj["a"]?.GetValue<double>() ?? 0.5686275;
            dim.DustColor.R = dustColorObj["r"]?.GetValue<double>() ?? 0.496;
            dim.DustColor.G = dustColorObj["g"]?.GetValue<double>() ?? 0.496;
            dim.DustColor.B = dustColorObj["b"]?.GetValue<double>() ?? 0.496;
        }
        dim.DustAlphaBoost = inner["DustAlphaBoost"]?.GetValue<double>() ?? 0.0;
        dim.DustTurbulence = inner["DustTurbulence"]?.GetValue<double>() ?? 1.0;

        dim.IsStaticDimension = inner["IsStaticDimension"]?.GetValue<bool>() ?? false;
        dim.StaticZoneSeed = inner["StaticZoneSeed"]?.GetValue<int>() ?? 0;
        dim.StaticMarkerSeed = inner["StaticMarkerSeed"]?.GetValue<int>() ?? 0;
        dim.StaticAliasOverride = inner["StaticAliasOverride"]?.GetValue<int>() ?? -1;
        dim.StaticConsumableDistributionInZone = inner["StaticConsumableDistributionInZone"]?.GetValue<uint>() ?? 0;

        double healthMulti = inner["StaticHealthMulti"]?.GetValue<double>() ?? 0.0;
        dim.StaticHealthUses = (int)Math.Round(healthMulti / 0.2);

        double toolMulti = inner["StaticToolAmmoMulti"]?.GetValue<double>() ?? 0.0;
        dim.StaticToolUses = (int)Math.Round(toolMulti / 0.2);

        double ammoMulti = inner["StaticWeaponAmmoMulti"]?.GetValue<double>() ?? inner["StaticAmmoMulti"]?.GetValue<double>() ?? 0.0;
        dim.StaticAmmoUses = (int)Math.Round(ammoMulti / 0.2);

        double disinfMulti = inner["StaticDisinfectionMulti"]?.GetValue<double>() ?? 0.0;
        dim.StaticDisinfectUses = (int)Math.Round(disinfMulti / 0.2);

        if (inner["StaticTerminalPlacements"] is JsonArray terminalsArray)
        {
            ImportTerminalPlacements(terminalsArray, dim.StaticTerminalPlacements, context);
        }

        dim.IsOutside = inner["IsOutside"]?.GetValue<bool>() ?? false;
        dim.LightAzimuth = inner["LightAzimuth"]?.GetValue<double>() ?? -92.9;
        dim.LightElevation = inner["LightElevation"]?.GetValue<double>() ?? 30.0;
        dim.LightIntensity = inner["LightIntensity"]?.GetValue<double>() ?? 1.0;
        dim.AmbientIntensity = inner["AmbientIntensity"]?.GetValue<double>() ?? 1.0;
        dim.ReflectionsIntensity = inner["ReflectionsIntensity"]?.GetValue<double>() ?? 1.0;
        dim.GodrayRange = inner["GodrayRange"]?.GetValue<double>() ?? 8000.0;
        dim.GodrayExponent = inner["GodrayExponent"]?.GetValue<double>() ?? 1.0;
        dim.AtmosphereDensity = inner["AtmosphereDensity"]?.GetValue<double>() ?? 1.0;
        dim.Exposure = inner["Exposure"]?.GetValue<double>() ?? 10.0;

        uint atmoId = inner["AtmosphereData"]?.GetValue<uint>() ?? 0;
        if (atmoId > 0 && context.AtmosphereBlocks.TryGetValue(atmoId, out var atmoBlock))
        {
            if (atmoBlock["LightColor"] is JsonObject lcObj)
            {
                dim.AtmosphereLightColor.A = lcObj["a"]?.GetValue<double>() ?? 1.0;
                dim.AtmosphereLightColor.R = lcObj["r"]?.GetValue<double>() ?? 1.0;
                dim.AtmosphereLightColor.G = lcObj["g"]?.GetValue<double>() ?? 1.0;
                dim.AtmosphereLightColor.B = lcObj["b"]?.GetValue<double>() ?? 1.0;
            }
            if (atmoBlock["PlanetAlbedo"] is JsonObject paObj)
            {
                dim.AtmospherePlanetAlbedo.A = paObj["a"]?.GetValue<double>() ?? 1.0;
                dim.AtmospherePlanetAlbedo.R = paObj["r"]?.GetValue<double>() ?? 0.2;
                dim.AtmospherePlanetAlbedo.G = paObj["g"]?.GetValue<double>() ?? 0.0627451;
                dim.AtmospherePlanetAlbedo.B = paObj["b"]?.GetValue<double>() ?? 0.0;
            }
        }

        int cloudsVal = inner["CloudsData"]?.GetValue<int>() ?? 1;
        dim.CloudsData = (DimensionCloudsType)cloudsVal;
        dim.CloudsCoverage = inner["CloudsCoverage"]?.GetValue<double>() ?? 0.5;
        dim.CloudsDensity = inner["CloudsDensity"]?.GetValue<double>() ?? 0.5;
        dim.CloudsSharpness = inner["CloudsSharpness"]?.GetValue<double>() ?? 0.0;
        dim.CloudsShadowOpacity = inner["CloudsShadowOpacity"]?.GetValue<double>() ?? 1.0;
        dim.CloudsTimescale = inner["CloudsTimescale"]?.GetValue<double>() ?? 0.0;
        dim.CloudsCrawling = inner["CloudsCrawling"]?.GetValue<double>() ?? 0.1;
        dim.CloudsFade = inner["CloudsFade"]?.GetValue<double>() ?? 0.1;
        dim.Sandstorm = inner["Sandstorm"]?.GetValue<bool>() ?? false;
        dim.SandstormEdgeA = inner["SandstormEdgeA"]?.GetValue<double>() ?? 0.4;
        dim.SandstormEdgeB = inner["SandstormEdgeB"]?.GetValue<double>() ?? 0.6;
        dim.SandstormMinFog = inner["SandstormMinFog"]?.GetValue<double>() ?? 0.01;
    }

    public static void ImportTerminalPlacements(JsonArray termArr, ObservableCollection<TerminalPlacement> targetCollection, ImportContext context)
    {
        targetCollection.Clear();
        foreach (var tNode in termArr)
        {
            if (tNode is not JsonObject tObj) continue;
            var termItem = new TerminalPlacement();
            if (tObj["PlacementWeights"] is JsonObject pwObj)
            {
                termItem.PlacementStartWeight = pwObj["Start"]?.GetValue<double>() ?? 0;
                termItem.PlacementMiddleWeight = pwObj["Middle"]?.GetValue<double>() ?? 0;
                termItem.PlacementEndWeight = pwObj["End"]?.GetValue<double>() ?? 0;
            }

            termItem.AreaSeedOffset = tObj["AreaSeedOffset"]?.GetValue<int>() ?? 0;
            termItem.MarkerSeedOffset = tObj["MarkerSeedOffset"]?.GetValue<int>() ?? 0;

            if (tObj["StartingStateData"] is JsonObject stateObj)
            {
                string stateStr = stateObj["StartingState"]?.ToString() ?? "Sleeping";
                if (Enum.TryParse<TerminalStartingState>(stateStr, true, out var tState))
                {
                    termItem.StartingState = tState;
                }
                termItem.UseCustomInfoText = stateObj["UseCustomInfoText"]?.GetValue<bool>() ?? false;
                uint infoTextId = stateObj["CustomInfoText"]?.GetValue<uint>() ?? 0;
                termItem.CustomInfoText = context.ResolveText(infoTextId);
                termItem.KeepShowingLocalLogCount = stateObj["KeepShowingLocalLogCount"]?.GetValue<bool>() ?? false;
                termItem.PasswordProtected = stateObj["PasswordProtected"]?.GetValue<bool>() ?? false;
                termItem.GeneratePassword = stateObj["GeneratePassword"]?.GetValue<bool>() ?? false;
                termItem.PasswordHintText = stateObj["PasswordHintText"]?.ToString() ?? string.Empty;
                termItem.PasswordPartCount = stateObj["PasswordPartCount"]?.GetValue<int>() ?? 1;
                termItem.ShowPasswordLength = stateObj["ShowPasswordLength"]?.GetValue<bool>() ?? true;
                termItem.ShowPasswordPartPositions = stateObj["ShowPasswordPartPositions"]?.GetValue<bool>() ?? false;

                if (stateObj["TerminalZoneSelectionDatas"] is JsonArray zoneSelArr)
                {
                    termItem.TerminalZoneSelectionDatas.Clear();
                    foreach (var zsNode in zoneSelArr)
                    {
                        if (zsNode is not JsonObject zsObj) continue;
                        string zStr = zsObj["ZoneIndex"]?.ToString() ?? "Zone_0";
                        int zIdx = 0;
                        if (zStr.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase) && int.TryParse(zStr.Substring(5), out int parsedZ))
                        {
                            zIdx = parsedZ;
                        }
                        int tIdx = zsObj["ItemIndex"]?.GetValue<int>() ?? 0;
                        termItem.TerminalZoneSelectionDatas.Add(new TerminalZoneSelectionData { Zone = zIdx, Terminal = tIdx });
                    }
                }
            }

            if (tObj["UniqueCommands"] is JsonArray uqArr)
            {
                termItem.UniqueCommands.Clear();
                foreach (var uqNode in uqArr)
                {
                    if (uqNode is not JsonObject uqObj) continue;
                    var cmdItem = new TerminalUniqueCommand
                    {
                        Command = uqObj["Command"]?.ToString() ?? string.Empty,
                        CommandDesc = uqObj["CommandDesc"]?.ToString() ?? string.Empty
                    };
                    if (uqObj["PostCommandOutputs"] is JsonArray postArr)
                    {
                        foreach (var pNode in postArr)
                        {
                            if (pNode is not JsonObject pObj) continue;
                            string ltStr = pObj["LineType"]?.ToString() ?? "Normal";
                            Enum.TryParse<TerminalLineType>(ltStr, true, out var parsedLt);
                            double time = pObj["Time"]?.GetValue<double>() ?? 0.0;
                            uint outId = pObj["Output"]?.GetValue<uint>() ?? 0;
                            cmdItem.PostCommandOutputs.Add(new TerminalPostCommandOutput
                            {
                                LineType = parsedLt,
                                Time = time,
                                Output = context.ResolveText(outId)
                            });
                        }
                    }
                    if (uqObj["CommandEvents"] is JsonArray ceArr)
                    {
                        ImportWardenEvents(ceArr, cmdItem.CommandEvents, context);
                    }
                    termItem.UniqueCommands.Add(cmdItem);
                }
            }

            if (tObj["LocalLogFiles"] is JsonArray logsArr)
            {
                termItem.LocalLogFiles.Clear();
                foreach (var lNode in logsArr)
                {
                    if (lNode is not JsonObject lObj) continue;
                    var logItem = new TerminalLocalLogFile
                    {
                        FileName = lObj["FileName"]?.ToString() ?? string.Empty
                    };
                    uint contentId = lObj["FileContent"]?.GetValue<uint>() ?? 0;
                    logItem.FileContent = context.ResolveText(contentId);
                    termItem.LocalLogFiles.Add(logItem);
                }
            }

            targetCollection.Add(termItem);
        }
    }

    private void BuildDimensionGridState(DimensionLayoutBlock dim, ImportContext context)
    {
        var gridState = dim.GridState;
        gridState.ImportStateData(new LevelLayoutGridStateData());

        if (string.IsNullOrWhiteSpace(dim.DimensionGeomorph))
        {
            return;
        }

        gridState.InitializeStartingTile(dim.DimensionGeomorph, 0, 0);

        if (dim.LevelLayoutData == 0)
        {
            return;
        }

        string? dimLgTunerPath = context.FindLgTunerConfigFile(dim.LevelLayoutData.ToString());
        if (dimLgTunerPath == null || !File.Exists(dimLgTunerPath))
        {
            return;
        }

        string dimLgTunerJson = File.ReadAllText(dimLgTunerPath);
        var dimLgTunerObj = JsonNode.Parse(dimLgTunerJson) as JsonObject;
        var dimGeoOverrides = dimLgTunerObj?["GeoOverrides"] as JsonArray;
        var dimZoneOverrides = dimLgTunerObj?["ZoneOverrides"] as JsonArray;

        var generatedRooms = new HashSet<(string geoId, int areaIdx)>();
        if (gridState.PlacedGeomorphs.Count > 0)
        {
            generatedRooms.Add((gridState.PlacedGeomorphs[0].Id, 0));
        }

        if (dimGeoOverrides != null)
        {
            foreach (var gNode in dimGeoOverrides)
            {
                if (gNode is not JsonObject gObj) continue;
                PlaceGeomorphFromOverride(gridState, gObj);
            }
        }

        var dimZoneEntranceDoorMap = new Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>();

        if (context.LevelLayoutBlocks.TryGetValue(dim.LevelLayoutData, out var dimLayoutObj) &&
            dimLayoutObj?["Zones"] is JsonArray dimZones)
        {
            ProcessSectorZoneSettingsAndRooms(gridState, SectorType.Main, dimZones, dimZoneOverrides, generatedRooms, context, dimZoneEntranceDoorMap);
            ConnectAdjacentDoors(gridState);
            ConfigureZoneDoors(gridState, SectorType.Main, dimZones, context, dimZoneEntranceDoorMap);
        }
        else
        {
            ConnectAdjacentDoors(gridState);
        }

        gridState.UpdateRoomNumbers();
        gridState.RecalculateSecurityDoorConstraints();
        gridState.UpdateRoomHighlightStates();
    }

    private void ImportDCsForSector(LevelLayoutGridState gridState, SectorType sector, JsonElement? dcPlacements)
    {
        if (!dcPlacements.HasValue || dcPlacements.Value.ValueKind != JsonValueKind.Array) return;
        foreach (var item in dcPlacements.Value.EnumerateArray())
        {
            int zoneNum = -1;
            if (item.TryGetProperty("ZoneIndex", out var zProp))
            {
                if (zProp.ValueKind == JsonValueKind.Number && zProp.TryGetInt32(out int zVal))
                {
                    zoneNum = zVal;
                }
                else if (zProp.ValueKind == JsonValueKind.String)
                {
                    string s = zProp.GetString() ?? string.Empty;
                    if (s.StartsWith("Zone_") && int.TryParse(s.Substring("Zone_".Length), out int zn))
                    {
                        zoneNum = zn;
                    }
                    else if (int.TryParse(s, out int zn2))
                    {
                        zoneNum = zn2;
                    }
                }
            }
            if (zoneNum >= 0)
            {
                var zs = gridState.GetOrCreateZoneSettings(sector, zoneNum);
                zs.ContainsDC = true;
            }
        }
    }

    private void ImportBulkheadKeysForSector(SectorSettings sectorSettings, JsonElement? keyPlacements)
    {
        if (!keyPlacements.HasValue || keyPlacements.Value.ValueKind != JsonValueKind.Array) return;
        sectorSettings.BulkheadKeyPlacements.Clear();
        foreach (var item in keyPlacements.Value.EnumerateArray())
        {
            var gatherItem = new GatherItemPlacement();
            gatherItem.CandidateZones.Clear();

            IEnumerable<JsonElement>? candidates = null;
            if (item.ValueKind == JsonValueKind.Array)
            {
                candidates = item.EnumerateArray();
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                if (item.TryGetProperty("ZonePlacementData", out var zpProp) && zpProp.ValueKind == JsonValueKind.Array)
                {
                    candidates = zpProp.EnumerateArray();
                }
                else if (item.TryGetProperty("CandidateZones", out var czProp) && czProp.ValueKind == JsonValueKind.Array)
                {
                    candidates = czProp.EnumerateArray();
                }
            }

            if (candidates != null)
            {
                foreach (var czObj in candidates)
                {
                    string dimIdx = "Reality";
                    int zoneNum = 0;
                    double startW = 0.0, midW = 0.0, endW = 0.0;

                    if (czObj.TryGetProperty("DimensionIndex", out var dProp))
                    {
                        if (dProp.ValueKind == JsonValueKind.String)
                        {
                            dimIdx = dProp.GetString() ?? "Reality";
                        }
                        else if (dProp.ValueKind == JsonValueKind.Number && dProp.TryGetInt32(out int dNum))
                        {
                            dimIdx = dNum == 0 ? "Reality" : $"Dimension_{dNum}";
                        }
                    }

                    if (czObj.TryGetProperty("LocalIndex", out var lProp) ||
                        czObj.TryGetProperty("ZoneIndex", out lProp) ||
                        czObj.TryGetProperty("Zone", out lProp))
                    {
                        if (lProp.ValueKind == JsonValueKind.Number && lProp.TryGetInt32(out int zNum))
                        {
                            zoneNum = zNum;
                        }
                        else if (lProp.ValueKind == JsonValueKind.String)
                        {
                            string s = lProp.GetString() ?? string.Empty;
                            if (s.StartsWith("Zone_") && int.TryParse(s.Substring("Zone_".Length), out int zn))
                            {
                                zoneNum = zn;
                            }
                            else if (int.TryParse(s, out int zn2))
                            {
                                zoneNum = zn2;
                            }
                        }
                    }

                    if (czObj.TryGetProperty("Weights", out var wProp))
                    {
                        if (wProp.TryGetProperty("Start", out var sw) && sw.TryGetDouble(out double dsw)) startW = dsw;
                        if (wProp.TryGetProperty("Middle", out var mw) && mw.TryGetDouble(out double dmw)) midW = dmw;
                        if (wProp.TryGetProperty("End", out var ew) && ew.TryGetDouble(out double dew)) endW = dew;
                    }

                    gatherItem.CandidateZones.Add(new CandidateZonePlacement
                    {
                        DimensionIndex = dimIdx,
                        Zone = zoneNum,
                        PlacementStartWeight = startW,
                        PlacementMiddleWeight = midW,
                        PlacementEndWeight = endW
                    });
                }
            }

            if (gatherItem.CandidateZones.Count > 0)
            {
                sectorSettings.BulkheadKeyPlacements.Add(gatherItem);
            }
        }
    }

    private void ImportArtifactDataForSector(SectorSettings sectorSettings, JsonElement? artifactData)
    {
        if (!artifactData.HasValue || artifactData.Value.ValueKind != JsonValueKind.Object) return;
        if (artifactData.Value.TryGetProperty("ArtifactAmountMulti", out var multiProp) && multiProp.TryGetDouble(out double multi))
        {
            sectorSettings.ArtifactAmountMultiplier = multi;
        }
    }

    private void PlaceGeomorphFromOverride(LevelLayoutGridState gridState, JsonObject gObj)
    {
        int posX = gObj["Position"]?["x"]?.GetValue<int>() ?? 0;
        int posZ = gObj["Position"]?["z"]?.GetValue<int>() ?? 0;
        int gridX = posX;
        int gridY = -posZ;

        string prefab = gObj["Geomorph"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prefab)) return;

        double rotAngle = 0.0;
        var rotNode = gObj["Rotation"];
        if (rotNode != null)
        {
            if (int.TryParse(rotNode.ToString(), out int rInt))
            {
                rotAngle = (rInt % 4) * 90.0;
            }
            else
            {
                string rStr = rotNode.ToString();
                rotAngle = rStr.ToLowerInvariant() switch
                {
                    "right" => 90.0,
                    "backwards" => 180.0,
                    "left" => 270.0,
                    _ => 0.0
                };
            }
        }

        int altVal = gObj["Altitude"]?.GetValue<int>() ?? 0;
        var alt = altVal < 0 ? GeomorphAltitude.Low : (altVal > 0 ? GeomorphAltitude.High : GeomorphAltitude.Mid);

        if (gridX == 0 && gridY == 0 && gridState.PlacedGeomorphs.Count > 0)
        {
            var root = gridState.PlacedGeomorphs[0];
            root.RotationAngle = rotAngle;
            root.Altitude = alt;
            return;
        }

        // Avoid duplicate placements at same cell
        if (gridState.PlacedGeomorphs.Any(g => g.GridX == gridX && g.GridY == gridY))
        {
            return;
        }

        var geoNode = gridState.CreateGeomorphNode(prefab, gridX, gridY, initialAreaIndex: -1, rotationAngle: rotAngle, altitude: alt);
        gridState.PlacedGeomorphs.Add(geoNode);
    }

    private void ProcessSectorZoneSettingsAndRooms(
        LevelLayoutGridState gridState,
        SectorType sector,
        JsonArray zonesArray,
        JsonArray? zoneOverridesArray,
        HashSet<(string geoId, int areaIdx)> generatedRooms,
        ImportContext context,
        Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>? zoneEntranceDoorMap = null)
    {
        for (int zIdx = 0; zIdx < zonesArray.Count; zIdx++)
        {
            if (zonesArray[zIdx] is not JsonObject zoneObj) continue;

            string localIndexStr = zoneObj["LocalIndex"]?.ToString() ?? $"Zone_{zIdx}";
            int zoneNumber = zIdx;
            if (localIndexStr.StartsWith("Zone_") && int.TryParse(localIndexStr.Substring("Zone_".Length), out int zn))
            {
                zoneNumber = zn;
            }

            // Populate ZoneSettings
            var zs = gridState.GetOrCreateZoneSettings(sector, zoneNumber);
            zs.SubSeed = zoneObj["SubSeed"]?.GetValue<int>() ?? 0;
            zs.MarkerSubSeed = zoneObj["MarkerSubSeed"]?.GetValue<int>() ?? 0;
            zs.LightingSubSeed = zoneObj["LightsSubSeed"]?.GetValue<int>() ?? 0;
            zs.DcScanSeed = zoneObj["BulkheadDCScanSeed"]?.GetValue<int>() ?? 0;
            zs.Lighting = zoneObj["LightSettings"]?.GetValue<uint>() ?? 0;
            zs.ConsumableDistributionInZone = zoneObj["ConsumableDistributionInZone"]?.GetValue<uint>() ?? 0;

            // Enemy Spawning
            if (zoneObj["EnemySpawningInZone"] is JsonArray esArr)
            {
                zs.EnemySpawningInZone.Clear();
                foreach (var esNode in esArr)
                {
                    if (esNode is not JsonObject esObj) continue;
                    var esItem = new EnemySpawningInZoneItem();
                    esItem.GroupType = esObj["GroupType"]?.GetValue<string>() ?? "Hibernate";
                    esItem.Difficulty = esObj["Difficulty"]?.GetValue<string>() ?? "Easy";
                    string distStr = esObj["Distribution"]?.GetValue<string>() ?? "Rel_Value";
                    if (Enum.TryParse<EnemyZoneDistribution>(distStr, true, out var distEnum))
                    {
                        esItem.Distribution = distEnum;
                    }
                    esItem.DistributionValue = esObj["DistributionValue"]?.GetValue<double>() ?? 0.0;
                    zs.EnemySpawningInZone.Add(esItem);
                }
            }

            zs.EnemyRespawning = zoneObj["EnemyRespawning"]?.GetValue<bool>() ?? false;
            zs.RequireOtherZone = zoneObj["EnemyRespawnRequireOtherZone"]?.GetValue<bool>() ?? true;
            zs.RoomDistance = zoneObj["EnemyRespawnRoomDistance"]?.GetValue<int>() ?? 2;
            zs.TimeInterval = zoneObj["EnemyRespawnTimeInterval"]?.GetValue<double>() ?? 10.0;
            zs.CountMultiplier = zoneObj["EnemyRespawnCountMultiplier"]?.GetValue<double>() ?? 1.0;
            zs.HSUClusters = zoneObj["HSUClustersInZone"]?.GetValue<int>() ?? 0;
            zs.CorpseClusters = zoneObj["CorpseClustersInZone"]?.GetValue<int>() ?? 0;
            zs.ResourceContainerClusters = zoneObj["ResourceContainerClustersInZone"]?.GetValue<int>() ?? 0;
            zs.ContainsGenCluster = (zoneObj["GeneratorClustersInZone"]?.GetValue<int>() ?? 0) > 0;
            zs.PlayScannerVoiceAudio = zoneObj["PlayScannerVoiceAudio"]?.GetValue<bool>() ?? true;

            string spawnersStr = zoneObj["GroundSpawnersInZone"]?.ToString() ?? "None_0";
            if (Enum.TryParse<ZoneDistributionAmount>(spawnersStr, true, out var spawners)) zs.GroundSpawnersInZone = spawners;

            string hsusStr = zoneObj["HSUsInZone"]?.ToString() ?? "None_0";
            if (Enum.TryParse<ZoneDistributionAmount>(hsusStr, true, out var hsus)) zs.HSUsInZone = hsus;

            string deconStr = zoneObj["DeconUnitsInZone"]?.ToString() ?? "None_0";
            if (Enum.TryParse<ZoneDistributionAmount>(deconStr, true, out var decon)) zs.DeconUnitsInZone = decon;

            double healthMulti = zoneObj["HealthMulti"]?.GetValue<double>() ?? 0.0;
            zs.MedKitUses = (int)Math.Round(healthMulti / 0.25);
            if (zoneObj["HealthPlacement"] is JsonObject hp)
            {
                zs.MedKitPlacementStart = hp["Start"]?.GetValue<double>() ?? 0;
                zs.MedKitPlacementMiddle = hp["Middle"]?.GetValue<double>() ?? 0;
                zs.MedKitPlacementEnd = hp["End"]?.GetValue<double>() ?? 0;
            }

            double ammoMulti = zoneObj["WeaponAmmoMulti"]?.GetValue<double>() ?? 0.0;
            zs.AmmoKitUses = (int)Math.Round(ammoMulti / 0.25);
            if (zoneObj["WeaponAmmoPlacement"] is JsonObject wp)
            {
                zs.AmmoKitPlacementStart = wp["Start"]?.GetValue<double>() ?? 0;
                zs.AmmoKitPlacementMiddle = wp["Middle"]?.GetValue<double>() ?? 0;
                zs.AmmoKitPlacementEnd = wp["End"]?.GetValue<double>() ?? 0;
            }

            double toolMulti = zoneObj["ToolAmmoMulti"]?.GetValue<double>() ?? 0.0;
            zs.ToolKitUses = (int)Math.Round(toolMulti / 0.25);
            if (zoneObj["ToolAmmoPlacement"] is JsonObject tp)
            {
                zs.ToolKitPlacementStart = tp["Start"]?.GetValue<double>() ?? 0;
                zs.ToolKitPlacementMiddle = tp["Middle"]?.GetValue<double>() ?? 0;
                zs.ToolKitPlacementEnd = tp["End"]?.GetValue<double>() ?? 0;
            }

            double disMulti = zoneObj["DisinfectionMulti"]?.GetValue<double>() ?? 0.0;
            zs.DisinfectKitUses = (int)Math.Round(disMulti / 0.25);
            if (zoneObj["DisinfectionPlacement"] is JsonObject dp)
            {
                zs.DisinfectKitPlacementStart = dp["Start"]?.GetValue<double>() ?? 0;
                zs.DisinfectKitPlacementMiddle = dp["Middle"]?.GetValue<double>() ?? 0;
                zs.DisinfectKitPlacementEnd = dp["End"]?.GetValue<double>() ?? 0;
            }

            // Exclude list
            if (zoneObj["EnemyRespawnExcludeList"] is JsonArray excArr)
            {
                zs.ExcludeList.Clear();
                foreach (var excNode in excArr)
                {
                    if (excNode != null && uint.TryParse(excNode.ToString(), out uint excId))
                    {
                        zs.ExcludeList.Add(new EnemyExcludeItem { EnemyId = excId });
                    }
                }
            }

            // Terminals
            if (zoneObj["TerminalPlacements"] is JsonArray termArr)
            {
                ImportTerminalPlacements(termArr, zs.TerminalPlacements, context);
            }

            // Disinfection stations
            if (zoneObj["DisinfectionStationPlacements"] is JsonArray disArr)
            {
                zs.DisinfectionStationPlacements.Clear();
                foreach (var dsNode in disArr)
                {
                    if (dsNode is not JsonObject dsObj) continue;
                    var dsItem = new DisinfectionStationPlacement();
                    if (dsObj["PlacementWeights"] is JsonObject pwObj)
                    {
                        dsItem.PlacementStartWeight = pwObj["Start"]?.GetValue<double>() ?? 0;
                        dsItem.PlacementMiddleWeight = pwObj["Middle"]?.GetValue<double>() ?? 0;
                        dsItem.PlacementEndWeight = pwObj["End"]?.GetValue<double>() ?? 0;
                    }
                    zs.DisinfectionStationPlacements.Add(dsItem);
                }
            }

            // Generate and assign rooms to this zone from AreaOverrides
            if (zoneOverridesArray != null && zIdx < zoneOverridesArray.Count &&
                zoneOverridesArray[zIdx] is JsonObject zOverObj &&
                zOverObj["AreaOverrides"] is JsonArray areaOverrides)
            {
                foreach (var aNode in areaOverrides)
                {
                    if (aNode is not JsonObject aObj) continue;
                    int prevX = aObj["PreviousCellPosition"]?["x"]?.GetValue<int>() ?? 0;
                    int prevZ = aObj["PreviousCellPosition"]?["z"]?.GetValue<int>() ?? 0;
                    int prevGridX = prevX;
                    int prevGridY = -prevZ;
                    int prevArea = aObj["PreviousCellInternalAreaID"]?.GetValue<int>() ?? 0;
                    int expanderId = aObj["LG_ExpanderID"]?.GetValue<int>() ?? 0;

                    var (targetGeo, targetArea) = ResolveTargetRoom(
                        gridState,
                        prevGridX,
                        prevGridY,
                        prevArea,
                        expanderId,
                        generatedRooms);

                    if (targetGeo != null && targetArea >= 0)
                    {
                        var parentGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == prevGridX && g.GridY == prevGridY);
                        if (parentGeo != null && zoneEntranceDoorMap != null && !zoneEntranceDoorMap.ContainsKey((sector, zoneNumber)))
                        {
                            var parentSec = parentGeo.GetRoomSector(prevArea);
                            int parentZone = parentGeo.GetRoomZone(prevArea);

                            if (parentSec != sector || parentZone != zoneNumber)
                            {
                                zoneEntranceDoorMap[(sector, zoneNumber)] = (prevGridX, prevGridY, prevArea, expanderId, targetGeo.Id, targetArea);
                            }
                        }

                        generatedRooms.Add((targetGeo.Id, targetArea));
                        targetGeo.GenerateRoom(targetArea);
                        targetGeo.SetRoomSector(targetArea, sector);
                        targetGeo.SetRoomZone(targetArea, zoneNumber);

                        var newDoors = gridState.GenerateDoorsForRoom(targetGeo, targetArea);
                        foreach (var d in newDoors)
                        {
                            if (!gridState.PlacedDoors.Contains(d))
                            {
                                gridState.PlacedDoors.Add(d);
                            }
                        }
                    }
                }
            }
        }
    }

    private (PlacedGeomorphNode? TargetGeo, int TargetArea) ResolveTargetRoom(
        LevelLayoutGridState gridState,
        int prevGridX,
        int prevGridY,
        int prevArea,
        int expanderId,
        HashSet<(string geoId, int areaIdx)> generatedRooms)
    {
        var rootGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == 0 && g.GridY == 0)
                      ?? gridState.PlacedGeomorphs.FirstOrDefault();

        // Special case for root room (elevator shaft)
        if (prevGridX == 0 && prevGridY == 0 && prevArea == 0 && expanderId == 0 && rootGeo != null)
        {
            if (!generatedRooms.Contains((rootGeo.Id, 0)))
            {
                return (rootGeo, 0);
            }
        }

        var parentGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == prevGridX && g.GridY == prevGridY);
        if (parentGeo == null)
        {
            return (null, -1);
        }

        // 1. Check if expander is internal within parentGeo
        if (parentGeo.Metadata?.AreaDatas != null && prevArea >= 0 && prevArea < parentGeo.Metadata.AreaDatas.Count)
        {
            var areaData = parentGeo.Metadata.AreaDatas[prevArea];
            if (areaData.ExpanderDatas != null && expanderId >= 0 && expanderId < areaData.ExpanderDatas.Count)
            {
                var expData = areaData.ExpanderDatas[expanderId];
                if (expData.AreaTo >= 0)
                {
                    return (parentGeo, expData.AreaTo);
                }
            }
        }

        // 2. Expander is external: find door on parentGeo and find matching door on adjacent geomorph
        PlacedDoorNode? doorOnParent = parentGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == prevArea && d.ExpanderIndex == expanderId)
                                       ?? parentGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.ExpanderIndex == expanderId);

        double worldX = 0, worldY = 0;
        if (doorOnParent != null)
        {
            worldX = doorOnParent.WorldX;
            worldY = doorOnParent.WorldY;
        }
        else if (parentGeo.Metadata?.AreaDatas != null && prevArea >= 0 && prevArea < parentGeo.Metadata.AreaDatas.Count)
        {
            var areaData = parentGeo.Metadata.AreaDatas[prevArea];
            if (areaData.ExpanderDatas != null && expanderId >= 0 && expanderId < areaData.ExpanderDatas.Count)
            {
                var exp = areaData.ExpanderDatas[expanderId];
                var (locX, locY) = LevelLayoutGridState.RotateLocalOffset(exp.ExpanderPosition.X, -exp.ExpanderPosition.Z, parentGeo.RotationAngle);
                worldX = parentGeo.WorldX + locX;
                worldY = parentGeo.WorldY + locY;
            }
        }

        // Find facing door on any adjacent geomorph
        foreach (var otherGeo in gridState.PlacedGeomorphs)
        {
            if (otherGeo == parentGeo) continue;

            if (otherGeo.Metadata?.AreaDatas != null)
            {
                for (int a = 0; a < otherGeo.Metadata.AreaDatas.Count; a++)
                {
                    var ad = otherGeo.Metadata.AreaDatas[a];
                    if (ad.ExpanderDatas == null) continue;
                    for (int e = 0; e < ad.ExpanderDatas.Count; e++)
                    {
                        var exp = ad.ExpanderDatas[e];
                        if (exp.AreaTo < 0) // External expander
                        {
                            var (locX, locY) = LevelLayoutGridState.RotateLocalOffset(exp.ExpanderPosition.X, -exp.ExpanderPosition.Z, otherGeo.RotationAngle);
                            double oWorldX = otherGeo.WorldX + locX;
                            double oWorldY = otherGeo.WorldY + locY;

                            if (Math.Abs(oWorldX - worldX) < 1.0 && Math.Abs(oWorldY - worldY) < 1.0)
                            {
                                return (otherGeo, a);
                            }
                        }
                    }
                }
            }
        }

        // Fallback: If not found, check if parentGeo itself has ungenerated prevArea
        if (!generatedRooms.Contains((parentGeo.Id, prevArea)))
        {
            return (parentGeo, prevArea);
        }

        return (null, -1);
    }

    private PlacedDoorNode? ResolveEntranceDoor(
        LevelLayoutGridState gridState,
        SectorType sector,
        int zoneNumber,
        Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>? zoneEntranceDoorMap)
    {
        if (zoneEntranceDoorMap != null && zoneEntranceDoorMap.TryGetValue((sector, zoneNumber), out var info))
        {
            var parentGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.GridX == info.PrevGridX && g.GridY == info.PrevGridY);
            var targetGeo = gridState.PlacedGeomorphs.FirstOrDefault(g => g.Id == info.TargetGeoId);

            if (parentGeo != null && targetGeo != null)
            {
                if (parentGeo == targetGeo)
                {
                    // Internal door within same geomorph
                    var door = targetGeo.Doors.FirstOrDefault(d => d.IsInternal &&
                        d.ExpanderIndex == info.ExpanderId &&
                        ((d.AreaFrom == info.PrevArea && d.AreaTo == info.TargetArea) || (d.AreaFrom == info.TargetArea && d.AreaTo == info.PrevArea)))
                        ?? targetGeo.Doors.FirstOrDefault(d => d.IsInternal &&
                        ((d.AreaFrom == info.PrevArea && d.AreaTo == info.TargetArea) || (d.AreaFrom == info.TargetArea && d.AreaTo == info.PrevArea)))
                        ?? targetGeo.Doors.FirstOrDefault(d => d.IsInternal && d.ExpanderIndex == info.ExpanderId);

                    if (door != null) return door;
                }
                else
                {
                    // External door between parentGeo and targetGeo
                    var doorOnParent = parentGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == info.PrevArea && d.ExpanderIndex == info.ExpanderId)
                        ?? parentGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.ExpanderIndex == info.ExpanderId)
                        ?? parentGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == info.PrevArea);

                    if (doorOnParent != null)
                    {
                        return doorOnParent;
                    }

                    var doorOnTarget = targetGeo.Doors.FirstOrDefault(d => !d.IsInternal && d.AreaFrom == info.TargetArea);
                    if (doorOnTarget != null) return doorOnTarget;
                }
            }
        }

        // Fallback
        return FindEnteringDoorForZone(gridState, sector, zoneNumber);
    }

    private void ConfigureZoneDoors(
        LevelLayoutGridState gridState,
        SectorType sector,
        JsonArray zonesArray,
        ImportContext context,
        Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>? zoneEntranceDoorMap = null)
    {
        for (int zIdx = 0; zIdx < zonesArray.Count; zIdx++)
        {
            if (zonesArray[zIdx] is not JsonObject zoneObj) continue;

            string localIndexStr = zoneObj["LocalIndex"]?.ToString() ?? $"Zone_{zIdx}";
            int zoneNumber = zIdx;
            if (localIndexStr.StartsWith("Zone_") && int.TryParse(localIndexStr.Substring("Zone_".Length), out int zn))
            {
                zoneNumber = zn;
            }

            uint chainedPuzzle = zoneObj["ChainedPuzzleToEnter"]?.GetValue<uint>() ?? 0;
            string gateStr = zoneObj["SecurityGateToEnter"]?.ToString() ?? "None";
            bool isSecurity = chainedPuzzle > 0 || gateStr.Equals("Security", StringComparison.OrdinalIgnoreCase) || gateStr.Equals("Apex", StringComparison.OrdinalIgnoreCase);
            bool isApex = gateStr.Equals("Apex", StringComparison.OrdinalIgnoreCase);

            var activeWaveObj = zoneObj["ActiveEnemyWave"] as JsonObject;
            bool hasBloodDoor = activeWaveObj?["HasActiveEnemyWave"]?.GetValue<bool>() ?? false;
            uint frontGroup = activeWaveObj?["EnemyGroupInfrontOfDoor"]?.GetValue<uint>() ?? 0;
            uint areaGroup = activeWaveObj?["EnemyGroupInArea"]?.GetValue<uint>() ?? 0;
            if (frontGroup > 0 || areaGroup > 0)
            {
                hasBloodDoor = true;
            }

            var progPuzzleObj = zoneObj["ProgressionPuzzleToEnter"] as JsonObject;
            string progType = progPuzzleObj?["PuzzleType"]?.ToString() ?? "None";
            uint progTextId = progPuzzleObj?["CustomText"]?.GetValue<uint>() ?? 0;
            int progCount = progPuzzleObj?["PlacementCount"]?.GetValue<int>() ?? 1;

            bool turnOffAlarmOnTerminal = zoneObj["TurnOffAlarmOnTerminal"]?.GetValue<bool>() ?? false;
            int terminalZone = 0;
            int terminalIndex = 0;
            if (zoneObj["TerminalPuzzleZone"] is JsonObject tpzObj)
            {
                string localIdx = tpzObj["LocalIndex"]?.ToString() ?? "Zone_0";
                if (localIdx.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase) && int.TryParse(localIdx.Substring(5), out int tz))
                {
                    terminalZone = tz;
                }
                terminalIndex = tpzObj["TerminalIndex"]?.GetValue<int>() ?? 0;
            }

            var enteringDoor = ResolveEntranceDoor(gridState, sector, zoneNumber, zoneEntranceDoorMap);
            if (enteringDoor != null)
            {
                if (isSecurity)
                {
                    enteringDoor.IsSecurityDoor = true;
                    enteringDoor.IsConnected = true;
                    enteringDoor.IsBlocked = false;
                    if (enteringDoor.ConnectedDoor != null)
                    {
                        enteringDoor.ConnectedDoor.IsSecurityDoor = true;
                        enteringDoor.ConnectedDoor.IsConnected = true;
                        enteringDoor.ConnectedDoor.IsBlocked = false;
                    }
                }

                enteringDoor.IsApexDoor = isApex;
                enteringDoor.ChainedPuzzleToSolve = chainedPuzzle;
                enteringDoor.HasBloodDoor = hasBloodDoor;
                enteringDoor.TurnOffAlarmOnTerminal = turnOffAlarmOnTerminal;
                enteringDoor.TerminalZone = terminalZone;
                enteringDoor.TerminalIndex = terminalIndex;

                if (enteringDoor.ConnectedDoor != null)
                {
                    enteringDoor.ConnectedDoor.IsApexDoor = isApex;
                    enteringDoor.ConnectedDoor.ChainedPuzzleToSolve = chainedPuzzle;
                    enteringDoor.ConnectedDoor.HasBloodDoor = hasBloodDoor;
                    enteringDoor.ConnectedDoor.TurnOffAlarmOnTerminal = turnOffAlarmOnTerminal;
                    enteringDoor.ConnectedDoor.TerminalZone = terminalZone;
                    enteringDoor.ConnectedDoor.TerminalIndex = terminalIndex;
                }

                if (zoneObj["EventsOnTerminalDeactivateAlarm"] is JsonArray deactEventsArr)
                {
                    ImportWardenEvents(deactEventsArr, enteringDoor.EventsOnTerminalDeactivateAlarm, context);
                    if (enteringDoor.ConnectedDoor != null)
                    {
                        ImportWardenEvents(deactEventsArr, enteringDoor.ConnectedDoor.EventsOnTerminalDeactivateAlarm, context);
                    }
                }

                if (zoneObj["EventsOnPortalWarp"] is JsonArray pwArr)
                {
                    ImportWardenEvents(pwArr, enteringDoor.EventsOnPortalWarp, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(pwArr, enteringDoor.ConnectedDoor.EventsOnPortalWarp, context);
                }
                if (zoneObj["EventsOnApproachDoor"] is JsonArray appArr)
                {
                    ImportWardenEvents(appArr, enteringDoor.EventsOnApproachDoor, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(appArr, enteringDoor.ConnectedDoor.EventsOnApproachDoor, context);
                }
                if (zoneObj["EventsOnUnlockDoor"] is JsonArray unlArr)
                {
                    ImportWardenEvents(unlArr, enteringDoor.EventsOnUnlockDoor, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(unlArr, enteringDoor.ConnectedDoor.EventsOnUnlockDoor, context);
                }
                if (zoneObj["EventsOnOpenDoor"] is JsonArray opArr)
                {
                    ImportWardenEvents(opArr, enteringDoor.EventsOnOpenDoor, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(opArr, enteringDoor.ConnectedDoor.EventsOnOpenDoor, context);
                }
                if (zoneObj["EventsOnDoorScanStart"] is JsonArray dssArr)
                {
                    ImportWardenEvents(dssArr, enteringDoor.EventsOnDoorScanStart, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(dssArr, enteringDoor.ConnectedDoor.EventsOnDoorScanStart, context);
                }
                if (zoneObj["EventsOnDoorScanDone"] is JsonArray dsdArr)
                {
                    ImportWardenEvents(dsdArr, enteringDoor.EventsOnDoorScanDone, context);
                    if (enteringDoor.ConnectedDoor != null) ImportWardenEvents(dsdArr, enteringDoor.ConnectedDoor.EventsOnDoorScanDone, context);
                }

                if (frontGroup > 0 && context.EnemyGroupBlocks.TryGetValue(frontGroup, out var fgObj))
                {
                    enteringDoor.EnemyGroupInFrontOfDoor.Clear();
                    PopulateEnemyGroupItems(fgObj, enteringDoor.EnemyGroupInFrontOfDoor);
                    if (enteringDoor.ConnectedDoor != null)
                    {
                        enteringDoor.ConnectedDoor.EnemyGroupInFrontOfDoor.Clear();
                        PopulateEnemyGroupItems(fgObj, enteringDoor.ConnectedDoor.EnemyGroupInFrontOfDoor);
                    }
                }

                if (areaGroup > 0 && context.EnemyGroupBlocks.TryGetValue(areaGroup, out var agObj))
                {
                    enteringDoor.EnemyGroupInArea.Clear();
                    PopulateEnemyGroupItems(agObj, enteringDoor.EnemyGroupInArea);
                    if (enteringDoor.ConnectedDoor != null)
                    {
                        enteringDoor.ConnectedDoor.EnemyGroupInArea.Clear();
                        PopulateEnemyGroupItems(agObj, enteringDoor.ConnectedDoor.EnemyGroupInArea);
                    }
                }

                if (!progType.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<ProgressionPuzzleType>(progType, true, out var pType))
                    {
                        enteringDoor.ProgressionPuzzleToEnter = pType;
                        if (enteringDoor.ConnectedDoor != null)
                        {
                            enteringDoor.ConnectedDoor.ProgressionPuzzleToEnter = pType;
                        }
                    }
                    enteringDoor.ProgressionPuzzleCustomText = context.ResolveText(progTextId);
                    enteringDoor.ProgressionPuzzlePlacementCount = progCount;

                    if (enteringDoor.ConnectedDoor != null)
                    {
                        enteringDoor.ConnectedDoor.ProgressionPuzzleCustomText = enteringDoor.ProgressionPuzzleCustomText;
                        enteringDoor.ConnectedDoor.ProgressionPuzzlePlacementCount = progCount;
                    }

                    if (progPuzzleObj?["ZonePlacementData"] is JsonArray zpArr)
                    {
                        enteringDoor.ProgressionPuzzleCandidateZones.Clear();
                        foreach (var zpNode in zpArr)
                        {
                            if (zpNode is not JsonObject zpObj) continue;
                            int targetZone = 0;
                            var localIndexNode = zpObj["LocalIndex"];
                            if (localIndexNode != null)
                            {
                                try
                                {
                                    targetZone = localIndexNode.GetValue<int>();
                                }
                                catch
                                {
                                    string localIndex = localIndexNode.ToString();
                                    if (localIndex.StartsWith("Zone_", StringComparison.OrdinalIgnoreCase))
                                    {
                                        int.TryParse(localIndex.Substring("Zone_".Length), out targetZone);
                                    }
                                }
                            }
                            var cand = new CandidateZonePlacement
                            {
                                Zone = targetZone,
                                PlacementStartWeight = zpObj["Weights"]?["Start"]?.GetValue<double>() ?? 0,
                                PlacementMiddleWeight = zpObj["Weights"]?["Middle"]?.GetValue<double>() ?? 0,
                                PlacementEndWeight = zpObj["Weights"]?["End"]?.GetValue<double>() ?? 0
                            };
                            enteringDoor.ProgressionPuzzleCandidateZones.Add(cand);
                        }

                        if (enteringDoor.ConnectedDoor != null)
                        {
                            enteringDoor.ConnectedDoor.ProgressionPuzzleCandidateZones.Clear();
                            foreach (var c in enteringDoor.ProgressionPuzzleCandidateZones)
                            {
                                enteringDoor.ConnectedDoor.ProgressionPuzzleCandidateZones.Add(c.Clone());
                            }
                        }
                    }
                }
            }
        }
    }

    private PlacedDoorNode? FindEnteringDoorForZone(LevelLayoutGridState gridState, SectorType sector, int zoneNumber)
    {
        var candidates = new List<PlacedDoorNode>();

        // Check external doors
        foreach (var door in gridState.PlacedDoors)
        {
            if (door.IsInternal || door.ConnectedDoor == null) continue;

            var myGeo = door.Geomorph;
            var otherGeo = door.ConnectedDoor.Geomorph;
            if (myGeo == null || otherGeo == null) continue;

            if (!myGeo.IsRoomGenerated(door.AreaFrom) || !otherGeo.IsRoomGenerated(door.ConnectedDoor.AreaFrom))
                continue;

            int myZone = myGeo.GetRoomZone(door.AreaFrom);
            var mySector = myGeo.GetRoomSector(door.AreaFrom);

            int otherZone = otherGeo.GetRoomZone(door.ConnectedDoor.AreaFrom);
            var otherSector = otherGeo.GetRoomSector(door.ConnectedDoor.AreaFrom);

            if (mySector == sector && myZone == zoneNumber)
            {
                if (otherSector == sector && otherZone < zoneNumber)
                {
                    candidates.Add(door);
                }
                else if (sector != SectorType.Main && zoneNumber == 0 && otherSector == SectorType.Main)
                {
                    candidates.Add(door);
                }
            }
        }

        // Check internal doors within same geomorph
        foreach (var door in gridState.PlacedDoors)
        {
            if (!door.IsInternal || door.Geomorph == null) continue;

            var geo = door.Geomorph;
            if (!geo.IsRoomGenerated(door.AreaFrom) || !geo.IsRoomGenerated(door.AreaTo))
                continue;

            var sFrom = geo.GetRoomSector(door.AreaFrom);
            int zFrom = geo.GetRoomZone(door.AreaFrom);

            var sTo = geo.GetRoomSector(door.AreaTo);
            int zTo = geo.GetRoomZone(door.AreaTo);

            if (sFrom == sector && zFrom == zoneNumber)
            {
                if (sTo == sector && zTo < zoneNumber)
                {
                    candidates.Add(door);
                }
                else if (sector != SectorType.Main && zoneNumber == 0 && sTo == SectorType.Main)
                {
                    candidates.Add(door);
                }
            }
            else if (sTo == sector && zTo == zoneNumber)
            {
                if (sFrom == sector && zFrom < zoneNumber)
                {
                    candidates.Add(door);
                }
                else if (sector != SectorType.Main && zoneNumber == 0 && sFrom == SectorType.Main)
                {
                    candidates.Add(door);
                }
            }
        }

        var configuredDoor = candidates.FirstOrDefault(d => d.IsSecurityDoor || d.IsApexDoor || d.HasBloodDoor || d.ChainedPuzzleToSolve > 0 || d.ProgressionPuzzleToEnter != ProgressionPuzzleType.None || d.TurnOffAlarmOnTerminal || d.EventsOnTerminalDeactivateAlarm.Count > 0 ||
            (d.ConnectedDoor != null && (d.ConnectedDoor.IsSecurityDoor || d.ConnectedDoor.IsApexDoor || d.ConnectedDoor.HasBloodDoor || d.ConnectedDoor.ChainedPuzzleToSolve > 0 || d.ConnectedDoor.ProgressionPuzzleToEnter != ProgressionPuzzleType.None || d.ConnectedDoor.TurnOffAlarmOnTerminal || d.ConnectedDoor.EventsOnTerminalDeactivateAlarm.Count > 0)));

        if (configuredDoor != null) return configuredDoor;

        return candidates.FirstOrDefault();
    }

    private void PopulateEnemyGroupItems(JsonObject groupObj, ObservableCollection<EnemyGroupItem> target)
    {
        double maxScore = 10.0;
        if (groupObj.TryGetPropertyValue("MaxScore", out var msNode) && msNode != null)
        {
            if (double.TryParse(msNode.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ms) && ms > 0)
            {
                maxScore = ms;
            }
        }

        if (groupObj["Roles"] is not JsonArray rolesArray || rolesArray.Count == 0)
        {
            if (groupObj["Groups"] is JsonArray gArr && gArr.Count > 0)
            {
                foreach (var gNode in gArr)
                {
                    if (gNode is not JsonObject gItem) continue;
                    uint enemyId = gItem["GroupType"]?.GetValue<uint>() ?? gItem["Enemy"]?.GetValue<uint>() ?? 0;
                    if (enemyId == 0) enemyId = 24;
                    int count = (int)Math.Max(1, Math.Round(gItem["DistributionValue"]?.GetValue<double>() ?? 1.0));
                    target.Add(new EnemyGroupItem { EnemyId = enemyId, Count = count });
                }
            }
            return;
        }

        var parsedRoles = new List<(uint EnemyId, double Cost, bool IsForceOne, double? ExplicitDistVal, double ParsedWeight)>();

        foreach (var rNode in rolesArray)
        {
            if (rNode is not JsonObject rObj) continue;

            uint enemyId = 0;
            if (rObj.TryGetPropertyValue("Enemy", out var enemyNode) && enemyNode != null)
            {
                uint.TryParse(enemyNode.ToString(), out enemyId);
            }

            if (enemyId == 0)
            {
                string roleStr = rObj["Role"]?.ToString() ?? "Melee";
                enemyId = ResolveEnemyIdFromRole(roleStr);
            }

            double cost = GetEnemyCost(enemyId);

            double? explicitDistVal = null;
            if (rObj.TryGetPropertyValue("DistributionValue", out var dvNode) && dvNode != null)
            {
                if (double.TryParse(dvNode.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dv) && dv > 0)
                {
                    explicitDistVal = dv;
                }
            }

            string distStr = rObj["Distribution"]?.ToString() ?? "Rel_100";
            bool isForceOne = distStr.Equals("Force_One", StringComparison.OrdinalIgnoreCase);

            double parsedWeight = 1.0;
            if (distStr.Equals("Rel_100", StringComparison.OrdinalIgnoreCase)) parsedWeight = 1.0;
            else if (distStr.Equals("Rel_75", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.75;
            else if (distStr.Equals("Rel_50", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.50;
            else if (distStr.Equals("Rel_25", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.25;
            else if (distStr.Equals("Rel_15", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.15;
            else if (distStr.Equals("Rel_10", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.10;
            else if (distStr.Equals("Rel_05", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.05;
            else if (distStr.Equals("Rel_0", StringComparison.OrdinalIgnoreCase)) parsedWeight = 0.0;
            else if (double.TryParse(distStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double dw) && dw > 0) parsedWeight = dw;

            parsedRoles.Add((enemyId, cost, isForceOne, explicitDistVal, parsedWeight));
        }

        if (parsedRoles.Count == 0) return;

        double forceOneCost = parsedRoles.Where(r => r.IsForceOne).Sum(r => r.Cost);
        double remainingScore = Math.Max(0, maxScore - forceOneCost);

        var relativeRoles = parsedRoles.Where(r => !r.IsForceOne).ToList();
        double sumWeights = relativeRoles.Sum(r => r.ExplicitDistVal ?? r.ParsedWeight);

        var counts = new Dictionary<uint, int>();

        foreach (var r in parsedRoles)
        {
            if (r.IsForceOne)
            {
                counts[r.EnemyId] = counts.GetValueOrDefault(r.EnemyId, 0) + 1;
            }
            else
            {
                double proportion;
                if (r.ExplicitDistVal.HasValue)
                {
                    proportion = r.ExplicitDistVal.Value;
                }
                else
                {
                    proportion = sumWeights > 0 ? (r.ParsedWeight / sumWeights) : (1.0 / relativeRoles.Count);
                }

                double points = proportion * (forceOneCost > 0 ? remainingScore : maxScore);
                int count = (int)Math.Max(1, Math.Round(points / r.Cost, MidpointRounding.AwayFromZero));
                counts[r.EnemyId] = counts.GetValueOrDefault(r.EnemyId, 0) + count;
            }
        }

        foreach (var kvp in counts)
        {
            target.Add(new EnemyGroupItem
            {
                EnemyId = kvp.Key,
                Count = kvp.Value
            });
        }
    }

    private static uint ResolveEnemyIdFromRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return 24;
        string r = role.Trim();
        if (r.Equals("Melee", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Striker", StringComparison.OrdinalIgnoreCase))
            return 24;
        if (r.Equals("Ranged", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Shooter", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Weakling", StringComparison.OrdinalIgnoreCase))
            return 26;
        if (r.Equals("MeleeLarge", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("LargeMelee", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BigMelee", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BigStriker", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Hunter", StringComparison.OrdinalIgnoreCase))
            return 35;
        if (r.Equals("RangedLarge", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("LargeRanged", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BigRanged", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BigShooter", StringComparison.OrdinalIgnoreCase))
            return 36;
        if (r.Equals("Boss", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Tank", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("MegaBoss", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Mother", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Birther", StringComparison.OrdinalIgnoreCase))
            return 40;
        if (r.Equals("MiniBoss", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Scout", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Detect", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BirtherBoss", StringComparison.OrdinalIgnoreCase))
            return 38;
        if (r.Equals("Shadow", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ShadowMelee", StringComparison.OrdinalIgnoreCase))
            return 41;
        if (r.Equals("ShadowLarge", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("ShadowMeleeLarge", StringComparison.OrdinalIgnoreCase))
            return 42;
        if (r.Equals("ShadowRanged", StringComparison.OrdinalIgnoreCase))
            return 43;

        if (uint.TryParse(r, out uint parsedId) && parsedId > 0)
            return parsedId;

        return 24;
    }

    private static double GetEnemyCost(uint enemyId)
    {
        return enemyId switch
        {
            13 => 1.0, // Striker_Wave
            21 => 1.0, // Shooter_Wave
            11 => 1.0, // Shooter_Wave_Spread
            16 => 4.0, // Striker_Big_Wave
            18 => 10.0, // Tank
            24 => 1.0, // Striker
            26 => 1.0, // Shooter
            35 => 4.0, // Big Striker
            36 => 4.0, // Big Shooter
            38 => 5.0, // Scout
            40 => 10.0, // Mother / Tank
            41 => 1.0, // Shadow
            42 => 4.0, // Big Shadow
            43 => 1.0, // Shadow Shooter
            44 => 4.0, // Big Shadow Shooter
            47 => 10.0, // Birther
            51 => 1.0, // Flyer
            _ => 1.0
        };
    }

    private void ConnectAdjacentDoors(LevelLayoutGridState gridState)
    {
        for (int i = 0; i < gridState.PlacedDoors.Count; i++)
        {
            var doorA = gridState.PlacedDoors[i];
            if (doorA.IsInternal) continue;

            for (int j = i + 1; j < gridState.PlacedDoors.Count; j++)
            {
                var doorB = gridState.PlacedDoors[j];
                if (doorB.IsInternal) continue;

                if (Math.Abs(doorA.WorldX - doorB.WorldX) < 1.0 &&
                    Math.Abs(doorA.WorldY - doorB.WorldY) < 1.0)
                {
                    doorA.IsConnected = true;
                    doorA.ConnectedDoor = doorB;
                    doorB.IsConnected = true;
                    doorB.ConnectedDoor = doorA;
                    break;
                }
            }
        }
    }

    private void MarkBulkheadDoors(
        LevelLayoutGridState gridState,
        ExpeditionBlock exp,
        Dictionary<(SectorType Sector, int ZoneNumber), (int PrevGridX, int PrevGridY, int PrevArea, int ExpanderId, string? TargetGeoId, int TargetArea)>? zoneEntranceDoorMap = null)
    {
        if (exp.SecondaryLayerEnabled)
        {
            var secDoor = ResolveEntranceDoor(gridState, SectorType.Secondary, 0, zoneEntranceDoorMap);
            if (secDoor != null)
            {
                secDoor.IsSecondaryDoor = true;
                secDoor.IsConnected = true;
                secDoor.IsBlocked = false;
                if (secDoor.ConnectedDoor != null)
                {
                    secDoor.ConnectedDoor.IsSecondaryDoor = true;
                    secDoor.ConnectedDoor.IsConnected = true;
                    secDoor.ConnectedDoor.IsBlocked = false;
                }
            }
            else
            {
                foreach (var door in gridState.PlacedDoors)
                {
                    if (door.IsInternal)
                    {
                        var geo = door.Geomorph;
                        if (geo != null && geo.IsRoomGenerated(door.AreaFrom) && geo.IsRoomGenerated(door.AreaTo))
                        {
                            var sFrom = geo.GetRoomSector(door.AreaFrom);
                            var sTo = geo.GetRoomSector(door.AreaTo);
                            if ((sFrom == SectorType.Main && sTo == SectorType.Secondary) ||
                                (sFrom == SectorType.Secondary && sTo == SectorType.Main))
                            {
                                door.IsSecondaryDoor = true;
                                door.IsConnected = true;
                                door.IsBlocked = false;
                                break;
                            }
                        }
                    }
                    else if (door.ConnectedDoor != null)
                    {
                        var myGeo = door.Geomorph;
                        var otherGeo = door.ConnectedDoor.Geomorph;
                        if (myGeo != null && otherGeo != null &&
                            myGeo.IsRoomGenerated(door.AreaFrom) && otherGeo.IsRoomGenerated(door.ConnectedDoor.AreaFrom))
                        {
                            var mySec = myGeo.GetRoomSector(door.AreaFrom);
                            var otherSec = otherGeo.GetRoomSector(door.ConnectedDoor.AreaFrom);

                            if ((mySec == SectorType.Main && otherSec == SectorType.Secondary) ||
                                (mySec == SectorType.Secondary && otherSec == SectorType.Main))
                            {
                                door.IsSecondaryDoor = true;
                                door.IsConnected = true;
                                door.IsBlocked = false;
                                door.ConnectedDoor.IsSecondaryDoor = true;
                                door.ConnectedDoor.IsConnected = true;
                                door.ConnectedDoor.IsBlocked = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        if (exp.ThirdLayerEnabled)
        {
            var thirdDoor = ResolveEntranceDoor(gridState, SectorType.Overload, 0, zoneEntranceDoorMap);
            if (thirdDoor != null)
            {
                thirdDoor.IsOverloadDoor = true;
                thirdDoor.IsConnected = true;
                thirdDoor.IsBlocked = false;
                if (thirdDoor.ConnectedDoor != null)
                {
                    thirdDoor.ConnectedDoor.IsOverloadDoor = true;
                    thirdDoor.ConnectedDoor.IsConnected = true;
                    thirdDoor.ConnectedDoor.IsBlocked = false;
                }
            }
            else
            {
                foreach (var door in gridState.PlacedDoors)
                {
                    if (door.IsInternal)
                    {
                        var geo = door.Geomorph;
                        if (geo != null && geo.IsRoomGenerated(door.AreaFrom) && geo.IsRoomGenerated(door.AreaTo))
                        {
                            var sFrom = geo.GetRoomSector(door.AreaFrom);
                            var sTo = geo.GetRoomSector(door.AreaTo);
                            if ((sFrom == SectorType.Main && sTo == SectorType.Overload) ||
                                (sFrom == SectorType.Overload && sTo == SectorType.Main))
                            {
                                door.IsOverloadDoor = true;
                                door.IsConnected = true;
                                door.IsBlocked = false;
                                break;
                            }
                        }
                    }
                    else if (door.ConnectedDoor != null)
                    {
                        var myGeo = door.Geomorph;
                        var otherGeo = door.ConnectedDoor.Geomorph;
                        if (myGeo != null && otherGeo != null &&
                            myGeo.IsRoomGenerated(door.AreaFrom) && otherGeo.IsRoomGenerated(door.ConnectedDoor.AreaFrom))
                        {
                            var mySec = myGeo.GetRoomSector(door.AreaFrom);
                            var otherSec = otherGeo.GetRoomSector(door.ConnectedDoor.AreaFrom);

                            if ((mySec == SectorType.Main && otherSec == SectorType.Overload) ||
                                (mySec == SectorType.Overload && otherSec == SectorType.Main))
                            {
                                door.IsOverloadDoor = true;
                                door.IsConnected = true;
                                door.IsBlocked = false;
                                door.ConnectedDoor.IsOverloadDoor = true;
                                door.ConnectedDoor.IsConnected = true;
                                door.ConnectedDoor.IsBlocked = false;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void ImportObjectives(LevelLayoutBlock level, ExpeditionBlock exp, ImportContext context)
    {
        // Main Sector Objective
        uint mainObjId = exp.MainLayerData.ObjectiveData.DataBlockId;
        if (mainObjId > 0 && context.WardenObjectiveBlocks.TryGetValue(mainObjId, out var mainObjJson) && mainObjJson != null)
        {
            level.MainSector.Objectives.Clear();
            var mainObj = DeserializeWardenObjective(mainObjJson, context);
            mainObj.ObjectiveNumber = 1;
            level.MainSector.Objectives.Add(mainObj);
        }

        // Chained Objectives in Main Sector
        if (exp.MainLayerData.ChainedObjectiveData != null &&
            exp.MainLayerData.ChainedObjectiveData.Value.ValueKind == JsonValueKind.Array)
        {
            int objNum = 2;
            foreach (var elem in exp.MainLayerData.ChainedObjectiveData.Value.EnumerateArray())
            {
                if (elem.TryGetProperty("DataBlockId", out var dbIdProp) && dbIdProp.TryGetUInt32(out uint chainedDbId))
                {
                    if (chainedDbId > 0 && context.WardenObjectiveBlocks.TryGetValue(chainedDbId, out var chainedObjJson) && chainedObjJson != null)
                    {
                        var chainedObj = DeserializeWardenObjective(chainedObjJson, context);
                        chainedObj.ObjectiveNumber = objNum++;
                        level.MainSector.Objectives.Add(chainedObj);
                    }
                }
            }
        }

        // Secondary Sector Objective
        if (exp.SecondaryLayerEnabled || exp.SecondaryLayerData.ObjectiveData.DataBlockId > 0)
        {
            uint secObjId = exp.SecondaryLayerData.ObjectiveData.DataBlockId;
            if (secObjId > 0 && context.WardenObjectiveBlocks.TryGetValue(secObjId, out var secObjJson) && secObjJson != null)
            {
                level.SecondarySector.Objectives.Clear();
                var secObj = DeserializeWardenObjective(secObjJson, context);
                secObj.ObjectiveNumber = 1;
                level.SecondarySector.Objectives.Add(secObj);
            }
        }

        // Overload Sector Objective
        if (exp.ThirdLayerEnabled || exp.ThirdLayerData.ObjectiveData.DataBlockId > 0)
        {
            uint thirdObjId = exp.ThirdLayerData.ObjectiveData.DataBlockId;
            if (thirdObjId > 0 && context.WardenObjectiveBlocks.TryGetValue(thirdObjId, out var thirdObjJson) && thirdObjJson != null)
            {
                level.OverloadSector.Objectives.Clear();
                var thirdObj = DeserializeWardenObjective(thirdObjJson, context);
                thirdObj.ObjectiveNumber = 1;
                level.OverloadSector.Objectives.Add(thirdObj);
            }
        }
    }

    private WardenObjective DeserializeWardenObjective(JsonObject obj, ImportContext context)
    {
        var objective = new WardenObjective();

        string typeStr = obj["Type"]?.ToString() ?? "HSU_FindTakeSample";
        if (Enum.TryParse<ObjectiveType>(typeStr, true, out var objType))
        {
            objective.ObjectiveType = objType;
        }

        objective.Header = context.ResolveText(obj["Header"]?.GetValue<uint>() ?? 0);
        objective.MainObjective = context.ResolveText(obj["MainObjective"]?.GetValue<uint>() ?? 0);
        objective.FindLocationInfo = context.ResolveText(obj["FindLocationInfo"]?.GetValue<uint>() ?? 0);
        objective.FindLocationInfoHelp = context.ResolveText(obj["FindLocationInfoHelp"]?.GetValue<uint>() ?? 0);
        objective.GoToZone = context.ResolveText(obj["GoToZone"]?.GetValue<uint>() ?? 0);
        objective.InZoneFindItem = context.ResolveText(obj["InZoneFindItem"]?.GetValue<uint>() ?? 0);
        objective.SolveItem = context.ResolveText(obj["SolveItem"]?.GetValue<uint>() ?? 0);

        objective.StopAllWavesBeforeGotoWin = obj["StopAllWavesBeforeGotoWin"]?.GetValue<bool>() ?? false;
        string waveTriggerStr = obj["WaveOnGotoWinTrigger"]?.ToString() ?? "OnObjectiveCompleted";
        if (Enum.TryParse<WaveOnGotoWinTrigger>(waveTriggerStr, true, out var wTrigger))
        {
            objective.WaveOnGotoWinTrigger = wTrigger;
        }

        objective.FogTransitionDataOnGotoWin = obj["FogTransitionDataOnGotoWin"]?.GetValue<uint>() ?? 0;
        objective.FogTransitionDataOnGotoWinDuration = obj["FogTransitionDurationOnGotoWin"]?.GetValue<double>() ?? 0;
        objective.ChainedPuzzleToActive = obj["ChainedPuzzleToActive"]?.GetValue<uint>() ?? 0;
        objective.ChainedPuzzleMidObjective = obj["ChainedPuzzleMidObjective"]?.GetValue<uint>() ?? 0;

        objective.DoNotSolveObjectiveOnReactorComplete = obj["DoNotSolveObjectiveOnReactorComplete"]?.GetValue<bool>() ?? false;
        objective.SpecialTerminalCommand = obj["SpecialTerminalCommand"]?.ToString() ?? string.Empty;
        objective.SpecialTerminalCommandDesc = obj["SpecialTerminalCommandDesc"]?.ToString() ?? string.Empty;

        if (obj["PostCommandOutput"] is JsonArray postArr)
        {
            objective.PostCommandOutput = string.Join(Environment.NewLine, postArr.Select(n => n?.ToString() ?? ""));
        }

        string cmdRuleStr = obj["SpecialCommandRule"]?.ToString() ?? "Normal";
        if (Enum.TryParse<SpecialCommandRule>(cmdRuleStr, true, out var cmdRule))
        {
            objective.SpecialCommandRule = cmdRule;
        }

        // Gather small items
        int gatherItemId = obj["Gather_ItemId"]?.GetValue<int>() ?? 0;
        if (Enum.IsDefined(typeof(GatherSmallItemType), gatherItemId))
        {
            objective.GatherItem = (GatherSmallItemType)gatherItemId;
        }
        objective.GatherMaxPerZone = obj["Gather_MaxPerZone"]?.GetValue<int>() ?? 0;
        int gatherReqCount = obj["Gather_RequiredCount"]?.GetValue<int>() ?? 0;
        if (gatherReqCount > 0)
        {
            objective.GatherItemPlacements.Clear();
            for (int i = 0; i < gatherReqCount; i++)
            {
                objective.GatherItemPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1 });
            }
        }

        // Retrieve big items
        if (obj["Retrieve_Items"] is JsonArray retArr && retArr.Count > 0)
        {
            int retItemId = retArr[0]?.GetValue<int>() ?? 0;
            if (Enum.IsDefined(typeof(RetrieveBigItemType), retItemId))
            {
                objective.RetrieveItem = (RetrieveBigItemType)retItemId;
            }
            objective.RetrieveItemPlacements.Clear();
            for (int i = 0; i < retArr.Count; i++)
            {
                objective.RetrieveItemPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1 });
            }
        }

        // Reactor
        if (obj["ReactorWaves"] is JsonArray rWaves)
        {
            objective.ReactorStartupStages.Clear();
            foreach (var rwNode in rWaves)
            {
                if (rwNode is not JsonObject rwObj) continue;
                string zfStr = rwObj["ZoneForVerification"]?.ToString() ?? "Zone_0";
                int zNum = 0;
                if (zfStr.StartsWith("Zone_") && int.TryParse(zfStr.Substring("Zone_".Length), out int zn))
                {
                    zNum = zn;
                }

                objective.ReactorStartupStages.Add(new ReactorStartupStage
                {
                    Warmup = rwObj["Warmup"]?.GetValue<double>() ?? 0,
                    WarmupFail = rwObj["WarmupFail"]?.GetValue<double>() ?? 0,
                    Wave = rwObj["Wave"]?.GetValue<double>() ?? 0,
                    Verify = rwObj["Verify"]?.GetValue<double>() ?? 0,
                    VerifyFail = rwObj["VerifyFail"]?.GetValue<double>() ?? 0,
                    VerifyInOtherZone = rwObj["VerifyInOtherZone"]?.GetValue<bool>() ?? false,
                    ZoneForVerification = zNum
                });
            }
        }

        // Uplink
        objective.UplinkNumberOfVerificationRounds = obj["Uplink_NumberOfVerificationRounds"]?.GetValue<int>() ?? 1;
        int uplinkTerms = obj["Uplink_NumberOfTerminals"]?.GetValue<int>() ?? 1;
        if (uplinkTerms > 0)
        {
            objective.TerminalUplinkPlacements.Clear();
            for (int i = 0; i < uplinkTerms; i++)
            {
                objective.TerminalUplinkPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Terminal" });
            }
        }

        // Power cells distribution
        int powerCellsCount = obj["PowerCellsToDistribute"]?.GetValue<int>() ?? 0;
        if (powerCellsCount > 0)
        {
            objective.DistributeGeneratorPlacements.Clear();
            for (int i = 0; i < powerCellsCount; i++)
            {
                objective.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement { GeneratorNumber = i + 1 });
            }
        }

        // Central generator cluster
        int genCount = obj["CentralPowerGenClustser_NumberOfGenerators"]?.GetValue<int>() ?? 0;
        if (genCount > 0)
        {
            objective.DistributeGeneratorPlacements.Clear();
            for (int i = 0; i < genCount; i++)
            {
                objective.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement { GeneratorNumber = i + 1 });
            }
        }

        int clusterPowerCells = obj["CentralPowerGenClustser_NumberOfPowerCells"]?.GetValue<int>() ?? 0;
        if (clusterPowerCells > 0)
        {
            objective.CentralGeneratorClusterPowerCellPlacements.Clear();
            for (int i = 0; i < clusterPowerCells; i++)
            {
                objective.CentralGeneratorClusterPowerCellPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Power Cell" });
            }
        }

        if (obj["CentralPowerGenClustser_FogDataSteps"] is JsonArray fogSteps)
        {
            objective.CentralGeneratorClusterFogTransitions.Clear();
            int stepNum = 1;
            foreach (var fsNode in fogSteps)
            {
                if (fsNode is not JsonObject fsObj) continue;
                objective.CentralGeneratorClusterFogTransitions.Add(new CentralGeneratorFogTransitionStage
                {
                    StageNumber = stepNum++,
                    FogSetting = fsObj["m_fogDataId"]?.GetValue<uint>() ?? 0,
                    TransitionDuration = fsObj["m_transitionToTime"]?.GetValue<double>() ?? 0
                });
            }
        }

        // Small HSU
        int hsuStartItem = obj["ActivateHSU_ItemFromStart"]?.GetValue<int>() ?? 0;
        if (Enum.IsDefined(typeof(RetrieveBigItemType), hsuStartItem)) objective.ItemFromStart = (RetrieveBigItemType)hsuStartItem;

        int hsuAfterItem = obj["ActivateHSU_ItemAfterActivation"]?.GetValue<int>() ?? 0;
        if (Enum.IsDefined(typeof(RetrieveBigItemType), hsuAfterItem)) objective.ItemAfterActivation = (RetrieveBigItemType)hsuAfterItem;

        objective.BringItemInElevator = obj["ActivateHSU_BringItemInElevator"]?.GetValue<bool>() ?? true;
        objective.StopEnemyWavesOnActivation = obj["ActivateHSU_StopEnemyWavesOnActivation"]?.GetValue<bool>() ?? false;
        objective.ObjectiveCompleteAfterInsertion = obj["ActivateHSU_ObjectiveCompleteAfterInsertion"]?.GetValue<bool>() ?? false;
        objective.RequireItemAfterActivationInExitScan = obj["ActivateHSU_RequireItemAfterActivationInExitScan"]?.GetValue<bool>() ?? false;

        // Survival
        objective.TimeToActivate = (int)(obj["Survival_TimeToActivate"]?.GetValue<double>() ?? 0);
        objective.TimeToSurvive = (int)(obj["Survival_TimeToSurvive"]?.GetValue<double>() ?? 0);
        objective.TimerTitle = context.ResolveText(obj["Survival_TimerTitle"]?.GetValue<uint>() ?? 0);
        objective.TimerToActivateTitle = context.ResolveText(obj["Survival_TimerToActivateTitle"]?.GetValue<uint>() ?? 0);

        // Gather Terminal
        objective.GatherTerminalRequiredCount = obj["GatherTerminal_RequiredCount"]?.GetValue<int>() ?? 0;
        objective.GatherTerminalCommand = obj["GatherTerminal_Command"]?.ToString() ?? string.Empty;
        objective.GatherTerminalCommandHelp = obj["GatherTerminal_CommandHelp"]?.ToString() ?? string.Empty;
        objective.GatherTerminalDownloadingText = obj["GatherTerminal_DownloadingText"]?.ToString() ?? string.Empty;
        objective.GatherTerminalDownloadCompleteText = obj["GatherTerminal_DownloadCompleteText"]?.ToString() ?? string.Empty;
        objective.GatherTerminalDownloadTime = obj["GatherTerminal_DownloadTime"]?.GetValue<double>() ?? -1.0;

        int gatherTermSpawns = obj["GatherTerminal_SpawnCount"]?.GetValue<int>() ?? 0;
        if (gatherTermSpawns > 0)
        {
            objective.GatherTerminalPlacements.Clear();
            for (int i = 0; i < gatherTermSpawns; i++)
            {
                objective.GatherTerminalPlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Terminal" });
            }
        }

        // Timed Terminal Sequence
        objective.TimedTerminalSequenceNumberOfRounds = obj["TimedTerminalSequence_NumberOfRounds"]?.GetValue<int>() ?? 3;
        objective.TimedTerminalSequenceTimePerRound = obj["TimedTerminalSequence_TimePerRound"]?.GetValue<double>() ?? 90.0;
        objective.TimedTerminalSequenceTimeForConfirmation = obj["TimedTerminalSequence_TimeForConfirmation"]?.GetValue<double>() ?? 10.0;
        objective.TimedTerminalSequenceSourceTerminalWorldEventObjectFilter = obj["TimedTerminalSequence_SourceTerminalWorldEventObjectFilter"]?.ToString() ?? string.Empty;

        int timedTermCount = obj["TimedTerminalSequence_NumberOfTerminals"]?.GetValue<int>() ?? 1;
        if (timedTermCount > 0)
        {
            objective.TimedTerminalSequencePlacements.Clear();
            for (int i = 0; i < timedTermCount; i++)
            {
                objective.TimedTerminalSequencePlacements.Add(new GatherItemPlacement { ItemNumber = i + 1, ItemPrefix = "Terminal" });
            }
        }

        // Waves & Events
        ImportGenericWave(obj["WavesOnElevatorLand"] as JsonArray, objective.WavesOnElevatorLand, context);
        ImportGenericWave(obj["WavesOnActivate"] as JsonArray, objective.WavesOnActivate, context);
        ImportGenericWave(obj["WavesOnGotoWin"] as JsonArray, objective.WavesOnGotoWin, context);

        ImportWardenEvents(obj["EventsOnElevatorLand"] as JsonArray, objective.EventsOnElevatorLand, context);
        ImportWardenEvents(obj["EventsOnActivate"] as JsonArray, objective.EventsOnActivate, context);
        ImportWardenEvents(obj["EventsOnGotoWin"] as JsonArray, objective.EventsOnGotoWin, context);
        ImportWardenEvents(obj["TimedTerminalSequence_EventsOnSequenceStart"] as JsonArray, objective.EventsOnSequenceStart, context);
        ImportWardenEvents(obj["TimedTerminalSequence_EventsOnSequenceDone"] as JsonArray, objective.EventsOnSequenceDone, context);
        ImportWardenEvents(obj["TimedTerminalSequence_EventsOnSequenceFail"] as JsonArray, objective.EventsOnSequenceFail, context);

        return objective;
    }

    private void ImportGenericWave(JsonArray? waveArray, GenericWaveSettings target, ImportContext context)
    {
        if (waveArray == null || waveArray.Count == 0)
        {
            target.Enabled = false;
            return;
        }

        if (waveArray[0] is not JsonObject waveRef)
        {
            target.Enabled = false;
            return;
        }

        target.Enabled = true;

        uint waveSettingsId = waveRef["WaveSettings"]?.GetValue<uint>() ?? 0;
        uint popId = waveRef["WavePopulation"]?.GetValue<uint>() ?? 0;

        if (popId > 0 && context.SurvivalWavePopulationBlocks.TryGetValue(popId, out var popObj) && popObj != null)
        {
            target.WaveRoleWeakling = popObj["WaveRoleWeakling"]?.GetValue<uint>() ?? 21;
            target.WaveRoleStandard = popObj["WaveRoleStandard"]?.GetValue<uint>() ?? 13;
            target.WaveRoleSpecial = popObj["WaveRoleSpecial"]?.GetValue<uint>() ?? 11;
            target.WaveRoleMiniBoss = popObj["WaveRoleMiniBoss"]?.GetValue<uint>() ?? 16;
            target.WaveRoleBoss = popObj["WaveRoleBoss"]?.GetValue<uint>() ?? 18;
        }

        if (waveSettingsId > 0 && context.SurvivalWaveSettingsBlocks.TryGetValue(waveSettingsId, out var waveObj) && waveObj != null)
        {
            target.PauseBeforeStart = waveObj["m_pauseBeforeStart"]?.GetValue<double>() ?? 3.0;
            target.PauseBetweenGroups = waveObj["m_pauseBetweenGroups"]?.GetValue<double>() ?? 5.0;
            target.WavePauseMin_AtCost = waveObj["m_wavePauseMin_atCost"]?.GetValue<double>() ?? 3.0;
            target.WavePauseMax_AtCost = waveObj["m_wavePauseMax_atCost"]?.GetValue<double>() ?? 10.0;
            target.WavePauseMin = waveObj["m_wavePauseMin"]?.GetValue<double>() ?? 3.0;
            target.WavePauseMax = waveObj["m_wavePauseMax"]?.GetValue<double>() ?? 30.0;

            if (waveObj["m_populationFilter"] is JsonArray filterArr)
            {
                target.FilterWeakling = false;
                target.FilterStandard = false;
                target.FilterSpecial = false;
                target.FilterMiniBoss = false;
                target.FilterBoss = false;

                foreach (var fn in filterArr)
                {
                    string fStr = fn?.ToString() ?? "";
                    if (fStr.Equals("Weakling", StringComparison.OrdinalIgnoreCase)) target.FilterWeakling = true;
                    if (fStr.Equals("Standard", StringComparison.OrdinalIgnoreCase)) target.FilterStandard = true;
                    if (fStr.Equals("Special", StringComparison.OrdinalIgnoreCase)) target.FilterSpecial = true;
                    if (fStr.Equals("MiniBoss", StringComparison.OrdinalIgnoreCase)) target.FilterMiniBoss = true;
                    if (fStr.Equals("Boss", StringComparison.OrdinalIgnoreCase)) target.FilterBoss = true;
                }
            }

            string filterTypeStr = waveObj["m_filterType"]?.ToString() ?? "Exclude";
            if (Enum.TryParse<WaveFilterType>(filterTypeStr, true, out var fType)) target.FilterType = fType;

            target.ChanceToRandomizeSpawnDirectionPerWave = waveObj["m_chanceToRandomizeSpawnDirectionPerWave"]?.GetValue<double>() ?? 1.0;
            target.ChanceToRandomizeSpawnDirectionPerGroup = waveObj["m_chanceToRandomizeSpawnDirectionPerGroup"]?.GetValue<double>() ?? 0.1;
            target.OverrideWaveSpawnType = waveObj["m_overrideWaveSpawnType"]?.GetValue<bool>() ?? false;

            string spawnTypeStr = waveObj["m_survivalWaveSpawnType"]?.ToString() ?? "InRelationToClosestAlivePlayer";
            if (Enum.TryParse<SurvivalWaveSpawnType>(spawnTypeStr, true, out var sType)) target.SurvivalWaveSpawnType = sType;

            target.PopulationPointsTotal = waveObj["m_populationPointsTotal"]?.GetValue<double>() ?? -1;
            target.PopulationPointsPerWaveStart = waveObj["m_populationPointsPerWaveStart"]?.GetValue<double>() ?? 17.0;
            target.PopulationPointsPerWaveEnd = waveObj["m_populationPointsPerWaveEnd"]?.GetValue<double>() ?? 25.0;
            target.PopulationPointsMinPerGroup = waveObj["m_populationPointsMinPerGroup"]?.GetValue<double>() ?? 5.0;
            target.PopulationPointsPerGroupStart = waveObj["m_populationPointsPerGroupStart"]?.GetValue<double>() ?? 5.0;
            target.PopulationPointsPerGroupEnd = waveObj["m_populationPointsPerGroupEnd"]?.GetValue<double>() ?? 10.0;
            target.PopulationRampOverTime = waveObj["m_populationRampOverTime"]?.GetValue<double>() ?? 200.0;
        }
    }

    private static uint GetUintValue(JsonNode? node)
    {
        if (node == null) return 0;
        if (uint.TryParse(node.ToString(), out uint val)) return val;
        return 0;
    }

    public static void ImportWardenEvents(JsonArray? eventsArray, ObservableCollection<WardenEvent> target, ImportContext context)
    {
        if (eventsArray == null) return;
        target.Clear();

        foreach (var evNode in eventsArray)
        {
            if (evNode is not JsonObject evObj) continue;

            string typeStr = evObj["Type"]?.ToString() ?? "None";
            Enum.TryParse<EventType>(typeStr, true, out var evType);

            string triggerStr = evObj["Trigger"]?.ToString() ?? "OnStart";
            Enum.TryParse<EventTrigger>(triggerStr, true, out var evTrigger);

            string layerStr = evObj["Layer"]?.ToString() ?? "MainLayer";
            var layer = layerStr switch
            {
                "SecondaryLayer" => SectorType.Secondary,
                "ThirdLayer" => SectorType.Overload,
                _ => SectorType.Main
            };

            string localIndexStr = evObj["LocalIndex"]?.ToString() ?? "Zone_0";
            int zone = 0;
            if (localIndexStr.StartsWith("Zone_") && int.TryParse(localIndexStr.Substring("Zone_".Length), out int zn))
            {
                zone = zn;
            }

            var pos = new Vector3Data(0, 0, 0);
            if (evObj["Position"] is JsonObject posObj)
            {
                pos = new Vector3Data(
                    posObj["x"]?.GetValue<double>() ?? 0,
                    posObj["y"]?.GetValue<double>() ?? 0,
                    posObj["z"]?.GetValue<double>() ?? 0);
            }

            uint chainPuzzle = GetUintValue(evObj["ChainPuzzle"]);
            int specialNumber = 0;
            if (evObj["SpecialNumber"] != null && int.TryParse(evObj["SpecialNumber"]!.ToString(), out int snVal))
            {
                specialNumber = snVal;
            }

            if (chainPuzzle == 0 && specialNumber > 0 && (evType == EventType.AddChainPuzzleToSecurityDoor || evType == EventType.ForceResetChainPuzzle || evType == EventType.ForceCompleteChainPuzzle))
            {
                chainPuzzle = (uint)specialNumber;
            }

            var wardenEvent = new WardenEvent
            {
                Type = evType,
                Trigger = evTrigger,
                ChainPuzzle = chainPuzzle,
                UseStaticBioscanPoints = evObj["UseStaticBioscanPoints"]?.GetValue<bool>() ?? false,
                Layer = layer,
                Dimension = evObj["DimensionIndex"]?.ToString() ?? "Reality",
                Zone = zone,
                Delay = evObj["Delay"]?.GetValue<double>() ?? 0,
                Duration = evObj["Duration"]?.GetValue<double>() ?? 0,
                ClearDimension = evObj["ClearDimension"]?.GetValue<bool>() ?? false,
                WardenIntel = context.ResolveText(GetUintValue(evObj["WardenIntel"])),
                CustomSubObjectiveHeader = context.ResolveText(GetUintValue(evObj["CustomSubObjectiveHeader"])),
                CustomSubObjective = context.ResolveText(GetUintValue(evObj["CustomSubObjective"])),
                SoundID = GetUintValue(evObj["SoundID"]),
                SoundSubtitle = context.ResolveText(GetUintValue(evObj["SoundSubtitle"])),
                DialogueID = GetUintValue(evObj["DialogueID"]),
                FogSetting = GetUintValue(evObj["FogSetting"]),
                FogTransitionDuration = (int)(evObj["FogTransitionDuration"]?.GetValue<double>() ?? 0),
                EnemyID = GetUintValue(evObj["EnemyID"]),
                Position = pos,
                Count = evObj["Count"]?.GetValue<int>() ?? 0,
                Enabled = evObj["Enabled"]?.GetValue<bool>() ?? true,
                WorldEventObjectFilter = evObj["WorldEventObjectFilter"]?.ToString() ?? string.Empty,
                CleanUpEnemiesBehind = evObj["CleanUpEnemiesBehind"]?.GetValue<bool>() ?? false,
                SpecialText = context.ResolveText(GetUintValue(evObj["SpecialText"])),
                SpecialNumber = specialNumber,
                SpecialBool = evObj["SpecialBool"]?.GetValue<bool>() ?? false
            };

            // Reactor object
            if (evObj["Reactor"] is JsonObject reactorObj)
            {
                string stateStr = reactorObj["State"]?.ToString() ?? "Intro";
                Enum.TryParse<AwoReactorState>(stateStr, true, out var rState);
                wardenEvent.Reactor.State = rState;
                wardenEvent.Reactor.Wave = reactorObj["Wave"]?.GetValue<int>() ?? 1;
                wardenEvent.Reactor.Progress = reactorObj["Progress"]?.GetValue<double>() ?? 0.0;
            }

            // Countdown object
            if (evObj["Countdown"] is JsonObject cdObj)
            {
                wardenEvent.Countdown.CanShowHours = cdObj["CanShowHours"]?.GetValue<bool>() ?? true;
                wardenEvent.Countdown.TitleText = context.ResolveText(GetUintValue(cdObj["TitleText"]));
                wardenEvent.Countdown.TimerColor = cdObj["TimerColor"]?.ToString() ?? "red";

                if (cdObj["EventsOnProgress"] is JsonArray progressArr)
                {
                    wardenEvent.Countdown.EventsOnProgress.Clear();
                    foreach (var pNode in progressArr)
                    {
                        if (pNode is not JsonObject pObj) continue;
                        var pItem = new AwoCountdownProgressEvent
                        {
                            Progress = pObj["Progress"]?.GetValue<double>() ?? 0.0
                        };
                        if (pObj["Events"] is JsonArray peArr)
                        {
                            ImportWardenEvents(peArr, pItem.Events, context);
                        }
                        wardenEvent.Countdown.EventsOnProgress.Add(pItem);
                    }
                }

                if (cdObj["EventsOnDone"] is JsonArray doneArr)
                {
                    ImportWardenEvents(doneArr, wardenEvent.Countdown.EventsOnDone, context);
                }
            }

            // SetZoneLight object
            if (evObj["SetZoneLight"] is JsonObject lightObj)
            {
                string typeLightStr = lightObj["Type"]?.ToString() ?? "SetZoneLightData";
                Enum.TryParse<AwoZoneLightType>(typeLightStr, true, out var lType);
                wardenEvent.SetZoneLight.Type = lType;
                wardenEvent.SetZoneLight.LightDataID = lightObj["LightDataID"]?.GetValue<uint>() ?? 0;
                wardenEvent.SetZoneLight.TransitionDuration = lightObj["TransitionDuration"]?.GetValue<double>() ?? 1.0;
                wardenEvent.SetZoneLight.Seed = lightObj["Seed"]?.GetValue<int>() ?? 0;
            }

            // CleanupEnemies object
            if (evObj["CleanupEnemies"] is JsonObject cleanObj)
            {
                string cleanTypeStr = cleanObj["Type"]?.ToString() ?? "Despawn";
                Enum.TryParse<AwoCleanupType>(cleanTypeStr, true, out var cType);
                wardenEvent.CleanupEnemies.Type = cType;
                wardenEvent.CleanupEnemies.AreaIndex = cleanObj["AreaIndex"]?.GetValue<int>() ?? -1;
                wardenEvent.CleanupEnemies.IncludeHibernate = cleanObj["IncludeHibernate"]?.GetValue<bool>() ?? true;
                wardenEvent.CleanupEnemies.IncludeAggressive = cleanObj["IncludeAggressive"]?.GetValue<bool>() ?? true;
                wardenEvent.CleanupEnemies.IncludeScout = cleanObj["IncludeScout"]?.GetValue<bool>() ?? true;

                wardenEvent.CleanupEnemies.AreaBlacklist.Clear();
                if (cleanObj["AreaBlacklist"] is JsonArray cblArr)
                {
                    foreach (var item in cblArr)
                    {
                        int area = item?.GetValue<int>() ?? 0;
                        wardenEvent.CleanupEnemies.AreaBlacklist.Add(new AwoAreaItem { AreaIndex = area });
                    }
                }

                wardenEvent.CleanupEnemies.ExcludeEnemyIDs.Clear();
                if (cleanObj["ExcludeEnemyID"] is JsonArray exArr)
                {
                    foreach (var item in exArr)
                    {
                        uint eId = item?.GetValue<uint>() ?? 0;
                        wardenEvent.CleanupEnemies.ExcludeEnemyIDs.Add(new AwoEnemyIdItem { EnemyID = eId });
                    }
                }

                wardenEvent.CleanupEnemies.IncludeOnlyIDs.Clear();
                if (cleanObj["IncludeOnlyID"] is JsonArray incArr)
                {
                    foreach (var item in incArr)
                    {
                        uint eId = item?.GetValue<uint>() ?? 0;
                        wardenEvent.CleanupEnemies.IncludeOnlyIDs.Add(new AwoEnemyIdItem { EnemyID = eId });
                    }
                }
            }

            // SpawnHibernates object
            if (evObj["SpawnHibernates"] is JsonObject hibObj)
            {
                wardenEvent.SpawnHibernates.AreaIndex = hibObj["AreaIndex"]?.GetValue<int>() ?? -1;
                wardenEvent.SpawnHibernates.EnemyID = hibObj["EnemyID"]?.GetValue<uint>() ?? 0;
                wardenEvent.SpawnHibernates.PlacementRandomWeight = hibObj["PlacementRandomWeight"]?.GetValue<double>() ?? 0.25;
                wardenEvent.SpawnHibernates.PlacementScore = hibObj["PlacementScore"]?.GetValue<double>() ?? 3.0;
                wardenEvent.SpawnHibernates.ResetPlacementInfo = hibObj["ResetPlacementInfo"]?.GetValue<bool>() ?? false;
                wardenEvent.SpawnHibernates.RandomizeRotation = hibObj["RandomizeRotation"]?.GetValue<bool>() ?? false;

                if (hibObj["Position"] is JsonObject hibPos)
                {
                    wardenEvent.SpawnHibernates.Position = new Vector3Data(
                        hibPos["x"]?.GetValue<double>() ?? 0,
                        hibPos["y"]?.GetValue<double>() ?? 0,
                        hibPos["z"]?.GetValue<double>() ?? 0);
                }

                if (hibObj["Rotation"] is JsonObject hibRot)
                {
                    wardenEvent.SpawnHibernates.Rotation = new Vector3Data(
                        hibRot["x"]?.GetValue<double>() ?? 0,
                        hibRot["y"]?.GetValue<double>() ?? 0,
                        hibRot["z"]?.GetValue<double>() ?? 0);
                }

                wardenEvent.SpawnHibernates.AreaBlacklist.Clear();
                if (hibObj["AreaBlacklist"] is JsonArray hibBlArr)
                {
                    foreach (var item in hibBlArr)
                    {
                        int area = item?.GetValue<int>() ?? 0;
                        wardenEvent.SpawnHibernates.AreaBlacklist.Add(new AwoAreaItem { AreaIndex = area });
                    }
                }
            }

            // SpawnScouts object
            if (evObj["SpawnScouts"] is JsonObject scoutObj)
            {
                wardenEvent.SpawnScouts.AreaIndex = scoutObj["AreaIndex"]?.GetValue<int>() ?? -1;
                int gt = 3;
                if (scoutObj["GroupType"] != null)
                {
                    string gtStr = scoutObj["GroupType"]!.ToString();
                    if (int.TryParse(gtStr, out int gtInt))
                    {
                        gt = gtInt;
                    }
                    else if (Enum.TryParse<ScoutType>(gtStr.Replace(" ", "_"), true, out var parsedGt))
                    {
                        gt = parsedGt.ToGroupTypeAndDifficulty().GroupType;
                    }
                }
                int diff = scoutObj["Difficulty"]?.GetValue<int>() ?? 0;
                wardenEvent.SpawnScouts.GroupType = gt;
                wardenEvent.SpawnScouts.Difficulty = diff;
                wardenEvent.SpawnScouts.ScoutType = ScoutTypeExtensions.FromGroupTypeAndDifficulty(gt, diff);

                wardenEvent.SpawnScouts.AreaBlacklist.Clear();
                if (scoutObj["AreaBlacklist"] is JsonArray scoutBlArr)
                {
                    foreach (var item in scoutBlArr)
                    {
                        int area = item?.GetValue<int>() ?? 0;
                        wardenEvent.SpawnScouts.AreaBlacklist.Add(new AwoAreaItem { AreaIndex = area });
                    }
                }
            }

            // AddTerminalCommand object
            if (evObj["AddTerminalCommand"] is JsonObject atcObj)
            {
                wardenEvent.AddTerminalCommand.TerminalIndex = atcObj["TerminalIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.AddTerminalCommand.CommandNumber = atcObj["CommandNumber"]?.GetValue<int>() ?? 0;
                wardenEvent.AddTerminalCommand.Command = atcObj["Command"]?.ToString() ?? string.Empty;
                wardenEvent.AddTerminalCommand.CommandDesc = context.ResolveText(atcObj["CommandDesc"]?.GetValue<uint>() ?? 0);
                wardenEvent.AddTerminalCommand.AutoIndentCommandDesc = atcObj["AutoIndentCommandDesc"]?.GetValue<bool>() ?? false;
                wardenEvent.AddTerminalCommand.ProgressWaitBeforeEvents = atcObj["ProgressWaitBeforeEvents"]?.GetValue<bool>() ?? false;

                if (atcObj["SpecialCommandRule"] != null)
                {
                    string ruleStr = atcObj["SpecialCommandRule"]!.ToString();
                    if (Enum.TryParse<SpecialCommandRule>(ruleStr, true, out var parsedRule))
                    {
                        wardenEvent.AddTerminalCommand.SpecialCommandRule = parsedRule;
                    }
                    else if (int.TryParse(ruleStr, out int ruleInt) && Enum.IsDefined(typeof(SpecialCommandRule), ruleInt))
                    {
                        wardenEvent.AddTerminalCommand.SpecialCommandRule = (SpecialCommandRule)ruleInt;
                    }
                }

                wardenEvent.AddTerminalCommand.PostCommandOutputs.Clear();
                if (atcObj["PostCommandOutputs"] is JsonArray pcoArr)
                {
                    foreach (var pNode in pcoArr)
                    {
                        if (pNode is not JsonObject pObj) continue;
                        string ltStr = pObj["LineType"]?.ToString() ?? "Normal";
                        Enum.TryParse<TerminalLineType>(ltStr, true, out var parsedLt);
                        wardenEvent.AddTerminalCommand.PostCommandOutputs.Add(new TerminalPostCommandOutput
                        {
                            LineType = parsedLt,
                            Time = pObj["Time"]?.GetValue<double>() ?? 0.0,
                            Output = context.ResolveText(pObj["Output"]?.GetValue<uint>() ?? 0)
                        });
                    }
                }

                if (atcObj["CommandEvents"] is JsonArray cmdEventsArr)
                {
                    ImportWardenEvents(cmdEventsArr, wardenEvent.AddTerminalCommand.CommandEvents, context);
                }
            }

            // HideTerminalCommand object
            if (evObj["HideTerminalCommand"] is JsonObject htcObj)
            {
                wardenEvent.HideTerminalCommand.TerminalIndex = htcObj["TerminalIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.HideTerminalCommand.CommandNumber = htcObj["CommandNumber"]?.GetValue<int>() ?? 0;
                wardenEvent.HideTerminalCommand.DeleteCommand = htcObj["DeleteCommand"]?.GetValue<bool>() ?? false;

                if (htcObj["CommandEnum"] != null)
                {
                    string ceStr = htcObj["CommandEnum"]!.ToString();
                    if (Enum.TryParse<TERM_Command>(ceStr, true, out var parsedCe))
                    {
                        wardenEvent.HideTerminalCommand.CommandEnum = parsedCe;
                    }
                    else if (uint.TryParse(ceStr, out uint ceUint) && Enum.IsDefined(typeof(TERM_Command), ceUint))
                    {
                        wardenEvent.HideTerminalCommand.CommandEnum = (TERM_Command)ceUint;
                    }
                }
                wardenEvent.HideTerminalCommand.IsAwoCommand = wardenEvent.HideTerminalCommand.CommandNumber > 0;
            }

            // UnhideTerminalCommand object
            if (evObj["UnhideTerminalCommand"] is JsonObject uhtcObj)
            {
                wardenEvent.UnhideTerminalCommand.TerminalIndex = uhtcObj["TerminalIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.UnhideTerminalCommand.CommandNumber = uhtcObj["CommandNumber"]?.GetValue<int>() ?? 0;

                if (uhtcObj["CommandEnum"] != null)
                {
                    string ceStr = uhtcObj["CommandEnum"]!.ToString();
                    if (Enum.TryParse<TERM_Command>(ceStr, true, out var parsedCe))
                    {
                        wardenEvent.UnhideTerminalCommand.CommandEnum = parsedCe;
                    }
                    else if (uint.TryParse(ceStr, out uint ceUint) && Enum.IsDefined(typeof(TERM_Command), ceUint))
                    {
                        wardenEvent.UnhideTerminalCommand.CommandEnum = (TERM_Command)ceUint;
                    }
                }
                wardenEvent.UnhideTerminalCommand.IsAwoCommand = wardenEvent.UnhideTerminalCommand.CommandNumber > 0;
            }

            // ActiveEnemyWave object
            if (evObj["ActiveEnemyWave"] is JsonObject aewObj)
            {
                wardenEvent.ActiveEnemyWave.HasActiveEnemyWave = aewObj["HasActiveEnemyWave"]?.GetValue<bool>() ?? false;
                wardenEvent.ActiveEnemyWave.EnemyGroupInfrontOfDoor = aewObj["EnemyGroupInfrontOfDoor"]?.GetValue<uint>() ?? 0;
                wardenEvent.ActiveEnemyWave.EnemyGroupInArea = aewObj["EnemyGroupInArea"]?.GetValue<uint>() ?? 0;
                wardenEvent.ActiveEnemyWave.EnemyGroupsInArea = aewObj["EnemyGroupsInArea"]?.GetValue<uint>() ?? 0;
            }

            // GiveResource object
            if (evObj["GiveResource"] is JsonObject grObj)
            {
                wardenEvent.GiveResource.Health = grObj["Health"]?.GetValue<double>() ?? 0.0;
                wardenEvent.GiveResource.MainAmmo = grObj["MainAmmo"]?.GetValue<double>() ?? 0.0;
                wardenEvent.GiveResource.SpecialAmmo = grObj["SpecialAmmo"]?.GetValue<double>() ?? 0.0;
                wardenEvent.GiveResource.ToolAmmo = grObj["ToolAmmo"]?.GetValue<double>() ?? 0.0;
                wardenEvent.GiveResource.FullTeamOverflow = grObj["FullTeamOverflow"]?.GetValue<bool>() ?? true;
                wardenEvent.GiveResource.UseLocation = grObj["UseLocation"]?.GetValue<bool>() ?? false;
                wardenEvent.GiveResource.IncludeSupplyEfficiency = grObj["IncludeSupplyEfficiency"]?.GetValue<bool>() ?? false;

                if (grObj["PlayerFilter"] is JsonArray pfArr)
                {
                    var pList = new List<int>();
                    foreach (var item in pfArr)
                    {
                        pList.Add(item?.GetValue<int>() ?? 0);
                    }
                    wardenEvent.GiveResource.SetPlayerFilter(pList);
                }
            }

            // NestedEvent object
            if (evObj["NestedEvent"] is JsonObject nestedObj)
            {
                string nTypeStr = nestedObj["Type"]?.ToString() ?? "ActivateAll";
                Enum.TryParse<AwoNestedEventType>(nTypeStr, true, out var nType);
                wardenEvent.NestedEvent.Type = nType;
                wardenEvent.NestedEvent.MaxRandomEvents = nestedObj["MaxRandomEvents"]?.GetValue<int>() ?? 1;
                wardenEvent.NestedEvent.AllowRepeatsInRandom = nestedObj["AllowRepeatsInRandom"]?.GetValue<bool>() ?? false;

                if (nestedObj["PlayerFilter"] is JsonArray pfArr)
                {
                    var pList = new List<int>();
                    foreach (var item in pfArr) pList.Add(item?.GetValue<int>() ?? 0);
                    wardenEvent.NestedEvent.SetPlayerFilter(pList);
                }

                if (nestedObj["EventsToActivate"] is JsonArray eaArr)
                {
                    ImportWardenEvents(eaArr, wardenEvent.NestedEvent.EventsToActivate, context);
                }

                if (nestedObj["WheelOfEvents"] is JsonArray wheelArr)
                {
                    wardenEvent.NestedEvent.WheelOfEvents.Clear();
                    foreach (var wNode in wheelArr)
                    {
                        if (wNode is not JsonObject wObj) continue;
                        var wItem = new AwoWheelOfEventItem
                        {
                            DebugName = wObj["DebugName"]?.ToString() ?? string.Empty,
                            Weight = wObj["Weight"]?.GetValue<double>() ?? 0.0,
                            RepeatCount = wObj["RepeatCount"]?.GetValue<int>() ?? 1,
                            IsInfinite = wObj["IsInfinite"]?.GetValue<bool>() ?? false
                        };
                        if (wObj["Events"] is JsonArray weArr)
                        {
                            ImportWardenEvents(weArr, wItem.Events, context);
                        }
                        wardenEvent.NestedEvent.WheelOfEvents.Add(wItem);
                    }
                }
            }

            // EventLoop object
            if (evObj["EventLoop"] is JsonObject elObj)
            {
                wardenEvent.EventLoop.LoopIndex = elObj["LoopIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.EventLoop.LoopDelay = elObj["LoopDelay"]?.GetValue<double>() ?? 1.0;
                wardenEvent.EventLoop.LoopCount = elObj["LoopCount"]?.GetValue<int>() ?? -1;
                if (elObj["EventsToActivate"] is JsonArray elaArr)
                {
                    ImportWardenEvents(elaArr, wardenEvent.EventLoop.EventsToActivate, context);
                }
            }

            // TeleportPlayer object
            if (evObj["TeleportPlayer"] is JsonObject tpObj)
            {
                wardenEvent.TeleportPlayer.FlashTeleport = tpObj["FlashTeleport"]?.GetValue<bool>() ?? false;
                wardenEvent.TeleportPlayer.WarpSentries = tpObj["WarpSentries"]?.GetValue<bool>() ?? true;
                wardenEvent.TeleportPlayer.WarpBigPickups = tpObj["WarpBigPickups"]?.GetValue<bool>() ?? true;
                wardenEvent.TeleportPlayer.SendBigPickupsToHost = tpObj["SendBigPickupsToHost"]?.GetValue<bool>() ?? false;
                wardenEvent.TeleportPlayer.FullTeamOverflow = tpObj["FullTeamOverflow"]?.GetValue<bool>() ?? false;

                if (tpObj["FromLocation"] is JsonObject flObj)
                {
                    wardenEvent.TeleportPlayer.FromLocation.Enabled = flObj["Enabled"]?.GetValue<bool>() ?? false;
                    wardenEvent.TeleportPlayer.FromLocation.DimensionIndex = flObj["DimensionIndex"]?.GetValue<int>() ?? 0;
                    int layerInt = flObj["Layer"]?.GetValue<int>() ?? 0;
                    wardenEvent.TeleportPlayer.FromLocation.Layer = (SectorType)layerInt;
                    wardenEvent.TeleportPlayer.FromLocation.LocalIndex = flObj["LocalIndex"]?.GetValue<int>() ?? 0;
                }

                if (tpObj["TPData"] is JsonArray tpdArr)
                {
                    wardenEvent.TeleportPlayer.TPData.Clear();
                    foreach (var tNode in tpdArr)
                    {
                        if (tNode is not JsonObject tObj) continue;
                        var tItem = new AwoTPDataItem
                        {
                            PlayerIndex = tObj["PlayerIndex"]?.GetValue<int>() ?? 0,
                            DimensionIndex = tObj["DimensionIndex"]?.GetValue<int>() ?? 0,
                            WorldEventObjectFilter = tObj["WorldEventObjectFilter"]?.ToString() ?? string.Empty,
                            PlayWarpAnimation = tObj["PlayWarpAnimation"]?.GetValue<bool>() ?? false,
                            FlashDuration = tObj["FlashDuration"]?.GetValue<double>() ?? 0.0
                        };
                        if (tObj["Position"] is JsonObject tPos)
                        {
                            tItem.Position = new Vector3Data(
                                tPos["x"]?.GetValue<double>() ?? 0,
                                tPos["y"]?.GetValue<double>() ?? 0,
                                tPos["z"]?.GetValue<double>() ?? 0);
                        }
                        if (tObj["LookDirectionV3"] is JsonObject ldPos)
                        {
                            tItem.UseLookDirectionVector = true;
                            tItem.LookDirectionV3 = new Vector3Data(
                                ldPos["x"]?.GetValue<double>() ?? 0,
                                ldPos["y"]?.GetValue<double>() ?? 0,
                                ldPos["z"]?.GetValue<double>() ?? 0);
                        }
                        else if (tObj["LookDirection"] != null)
                        {
                            tItem.UseLookDirectionVector = false;
                            int ldInt = tObj["LookDirection"]?.GetValue<int>() ?? 0;
                            tItem.LookDirection = (AwoTeleportLookDirection)ldInt;
                        }
                        wardenEvent.TeleportPlayer.TPData.Add(tItem);
                    }
                }
            }

            // InfectPlayer object
            if (evObj["InfectPlayer"] is JsonObject infObj)
            {
                wardenEvent.InfectPlayer.FullTeamOverflow = infObj["FullTeamOverflow"]?.GetValue<bool>() ?? true;
                wardenEvent.InfectPlayer.InfectionAmount = infObj["InfectionAmount"]?.GetValue<double>() ?? 0.0;
                wardenEvent.InfectPlayer.InfectOverTime = infObj["InfectOverTime"]?.GetValue<bool>() ?? false;
                wardenEvent.InfectPlayer.Interval = infObj["Interval"]?.GetValue<double>() ?? 1.0;
                wardenEvent.InfectPlayer.UseZone = infObj["UseZone"]?.GetValue<bool>() ?? false;

                if (infObj["PlayerFilter"] is JsonArray pfArr)
                {
                    var pList = new List<int>();
                    foreach (var item in pfArr) pList.Add(item?.GetValue<int>() ?? 0);
                    wardenEvent.InfectPlayer.SetPlayerFilter(pList);
                }
            }

            // DamagePlayer object
            if (evObj["DamagePlayer"] is JsonObject dmgObj)
            {
                wardenEvent.DamagePlayer.FullTeamOverflow = dmgObj["FullTeamOverflow"]?.GetValue<bool>() ?? true;
                wardenEvent.DamagePlayer.DamageAmount = dmgObj["DamageAmount"]?.GetValue<double>() ?? 0.0;
                wardenEvent.DamagePlayer.DealPercentageDamage = dmgObj["DealPercentageDamage"]?.GetValue<bool>() ?? false;
                wardenEvent.DamagePlayer.DamageOverTime = dmgObj["DamageOverTime"]?.GetValue<bool>() ?? false;
                wardenEvent.DamagePlayer.Interval = dmgObj["Interval"]?.GetValue<double>() ?? 1.0;
                wardenEvent.DamagePlayer.UseZone = dmgObj["UseZone"]?.GetValue<bool>() ?? false;

                if (dmgObj["PlayerFilter"] is JsonArray pfArr)
                {
                    var pList = new List<int>();
                    foreach (var item in pfArr) pList.Add(item?.GetValue<int>() ?? 0);
                    wardenEvent.DamagePlayer.SetPlayerFilter(pList);
                }
            }

            // RevivePlayer object
            if (evObj["RevivePlayer"] is JsonObject revObj)
            {
                wardenEvent.RevivePlayer.FullTeamOverflow = revObj["FullTeamOverflow"]?.GetValue<bool>() ?? true;
                if (revObj["PlayerFilter"] is JsonArray pfArr)
                {
                    var pList = new List<int>();
                    foreach (var item in pfArr) pList.Add(item?.GetValue<int>() ?? 0);
                    wardenEvent.RevivePlayer.SetPlayerFilter(pList);
                }
            }

            // AdjustTimer object
            if (evObj["AdjustTimer"] is JsonObject adjObj)
            {
                wardenEvent.AdjustTimer.Duration = adjObj["Duration"]?.GetValue<double>() ?? 0.0;
                wardenEvent.AdjustTimer.Speed = adjObj["Speed"]?.GetValue<double>() ?? 0.0;
                wardenEvent.AdjustTimer.UpdateTitleText = adjObj["UpdateTitleText"]?.GetValue<bool>() ?? false;
                wardenEvent.AdjustTimer.TitleText = context.ResolveText(GetUintValue(adjObj["TitleText"]));
                wardenEvent.AdjustTimer.UpdateBodyText = adjObj["UpdateBodyText"]?.GetValue<bool>() ?? false;
                wardenEvent.AdjustTimer.BodyText = context.ResolveText(GetUintValue(adjObj["BodyText"]));
                wardenEvent.AdjustTimer.UpdateColor = adjObj["UpdateColor"]?.GetValue<bool>() ?? false;
                wardenEvent.AdjustTimer.TimerColor = adjObj["TimerColor"]?.ToString() ?? "red";
                wardenEvent.AdjustTimer.UpdateSpecialHud = adjObj["UpdateSpecialHud"]?.GetValue<bool>() ?? false;
                wardenEvent.AdjustTimer.SpecialHudIndex = adjObj["SpecialHudIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.AdjustTimer.SpecialHudTimeModifier = adjObj["SpecialHudTimeModifier"]?.GetValue<double>() ?? 0.0;
            }

            // Countup object
            if (evObj["Countup"] is JsonObject cntObj)
            {
                wardenEvent.Countup.StartValue = cntObj["StartValue"]?.GetValue<double>() ?? 0.0;
                wardenEvent.Countup.Speed = cntObj["Speed"]?.GetValue<double>() ?? 1.0;
                wardenEvent.Countup.TitleText = context.ResolveText(GetUintValue(cntObj["TitleText"]));
                wardenEvent.Countup.BodyText = cntObj["BodyText"]?.ToString() ?? "[COUNTUP]";
                wardenEvent.Countup.TimerColor = cntObj["TimerColor"]?.ToString() ?? "red";
                wardenEvent.Countup.DecimalPoints = cntObj["DecimalPoints"]?.GetValue<int>() ?? 0;

                if (cntObj["EventsOnProgress"] is JsonArray progressArr)
                {
                    wardenEvent.Countup.EventsOnProgress.Clear();
                    foreach (var pNode in progressArr)
                    {
                        if (pNode is not JsonObject pObj) continue;
                        var pItem = new AwoCountdownProgressEvent
                        {
                            Progress = pObj["Progress"]?.GetValue<double>() ?? 0.0
                        };
                        if (pObj["Events"] is JsonArray peArr)
                        {
                            ImportWardenEvents(peArr, pItem.Events, context);
                        }
                        wardenEvent.Countup.EventsOnProgress.Add(pItem);
                    }
                }

                if (cntObj["EventsOnDone"] is JsonArray doneArr)
                {
                    ImportWardenEvents(doneArr, wardenEvent.Countup.EventsOnDone, context);
                }
            }

            // NavMarker object
            if (evObj["NavMarker"] is JsonObject nmObj)
            {
                wardenEvent.NavMarker.Index = nmObj["Index"]?.GetValue<int>() ?? 0;
                wardenEvent.NavMarker.Style = nmObj["Style"]?.GetValue<int>() ?? 10;
                wardenEvent.NavMarker.Title = context.ResolveText(GetUintValue(nmObj["Title"]));
                wardenEvent.NavMarker.Color = nmObj["Color"]?.ToString() ?? "#B36FF6";
                wardenEvent.NavMarker.UsePin = nmObj["UsePin"]?.GetValue<bool>() ?? true;
            }

            // Portal object
            if (evObj["Portal"] is JsonObject portObj)
            {
                wardenEvent.Portal.TargetDimension = portObj["TargetDimension"]?.GetValue<int>() ?? 1;
                wardenEvent.Portal.TeleportDelay = portObj["TeleportDelay"]?.GetValue<double>() ?? 5.0;
                wardenEvent.Portal.PreventPortalWarpTeamEvent = portObj["PreventPortalWarpTeamEvent"]?.GetValue<bool>() ?? false;
            }

            // SuccessScreen object
            if (evObj["SuccessScreen"] is JsonObject ssObj)
            {
                wardenEvent.SuccessScreen.HasSuccessScreen = true;
                string ssTypeStr = ssObj["Type"]?.ToString() ?? "SetSuccessScreen";
                Enum.TryParse<AwoSuccessScreenType>(ssTypeStr, true, out var ssType);
                wardenEvent.SuccessScreen.Type = ssType;
                wardenEvent.SuccessScreen.CustomSuccessScreen = ssObj["CustomSuccessScreen"]?.GetValue<uint>() ?? 0;
                wardenEvent.SuccessScreen.OverrideMusic = ssObj["OverrideMusic"]?.GetValue<uint>() ?? 0;
            }

            // CameraShake object
            if (evObj["CameraShake"] is JsonObject csObj)
            {
                wardenEvent.CameraShake.Radius = csObj["Radius"]?.GetValue<double>() ?? 0.0;
                wardenEvent.CameraShake.Amplitude = csObj["Amplitude"]?.GetValue<double>() ?? 0.0;
                wardenEvent.CameraShake.Frequency = csObj["Frequency"]?.GetValue<double>() ?? 0.0;
                wardenEvent.CameraShake.Directional = csObj["Directional"]?.GetValue<bool>() ?? true;
            }

            // MultiProgression object
            if (evObj["MultiProgression"] is JsonArray mpArr)
            {
                wardenEvent.MultiProgression.Items.Clear();
                foreach (var itemNode in mpArr)
                {
                    if (itemNode is not JsonObject itemObj) continue;
                    int layerInt = itemObj["Layer"]?.GetValue<int>() ?? 0;
                    wardenEvent.MultiProgression.Items.Add(new AwoMultiProgressionItem
                    {
                        CustomSubObjectiveHeader = context.ResolveText(GetUintValue(itemObj["CustomSubObjectiveHeader"])),
                        CustomSubObjective = context.ResolveText(GetUintValue(itemObj["CustomSubObjective"])),
                        Index = itemObj["Index"]?.GetValue<int>() ?? 0,
                        Priority = itemObj["Priority"]?.GetValue<int>() ?? 1,
                        Layer = (SectorType)layerInt,
                        IsLayerIndependent = itemObj["IsLayerIndependent"]?.GetValue<bool>() ?? true,
                        OverrideTag = itemObj["OverrideTag"]?.GetValue<uint>() ?? 0
                    });
                }
            }

            // WaveRoarSound object
            if (evObj["WaveRoarSound"] is JsonObject wrsObj)
            {
                string rSoundStr = wrsObj["RoarSound"]?.ToString() ?? "Striker";
                Enum.TryParse<AwoRoarSound>(rSoundStr, true, out var rSound);
                string rSizeStr = wrsObj["RoarSize"]?.ToString() ?? "Small";
                Enum.TryParse<AwoRoarSize>(rSizeStr, true, out var rSize);
                wardenEvent.WaveRoarSound.RoarSound = rSound;
                wardenEvent.WaveRoarSound.RoarSize = rSize;
                wardenEvent.WaveRoarSound.IsOutside = wrsObj["IsOutside"]?.GetValue<bool>() ?? false;
            }

            // CustomHud object
            if (evObj["CustomHud"] is JsonObject chObj)
            {
                wardenEvent.CustomHud.TitleText = context.ResolveText(GetUintValue(chObj["TitleText"]));
                wardenEvent.CustomHud.BodyText = context.ResolveText(GetUintValue(chObj["BodyText"]));
            }

            // SpecialHud object
            if (evObj["SpecialHud"] is JsonObject shObj)
            {
                string shTypeStr = shObj["Type"]?.ToString() ?? "StartTimer";
                Enum.TryParse<AwoSpecialHudType>(shTypeStr, true, out var shType);
                string shStyleStr = shObj["Style"]?.ToString() ?? "Default";
                Enum.TryParse<AwoSpecialHudStyle>(shStyleStr, true, out var shStyle);
                wardenEvent.SpecialHud.Type = shType;
                wardenEvent.SpecialHud.Index = shObj["Index"]?.GetValue<int>() ?? 0;
                wardenEvent.SpecialHud.Message = context.ResolveText(GetUintValue(shObj["Message"]));
                wardenEvent.SpecialHud.Style = shStyle;
                wardenEvent.SpecialHud.Priority = shObj["Priority"]?.GetValue<int>() ?? -2;
                wardenEvent.SpecialHud.ShowTimeInProgressBar = shObj["ShowTimeInProgressBar"]?.GetValue<bool>() ?? true;
                wardenEvent.SpecialHud.InvertProgress = shObj["InvertProgress"]?.GetValue<bool>() ?? false;

                if (shObj["EventsOnProgress"] is JsonArray progressArr)
                {
                    wardenEvent.SpecialHud.EventsOnProgress.Clear();
                    foreach (var pNode in progressArr)
                    {
                        if (pNode is not JsonObject pObj) continue;
                        var pItem = new AwoCountdownProgressEvent
                        {
                            Progress = pObj["Progress"]?.GetValue<double>() ?? 0.0
                        };
                        if (pObj["Events"] is JsonArray peArr)
                        {
                            ImportWardenEvents(peArr, pItem.Events, context);
                        }
                        wardenEvent.SpecialHud.EventsOnProgress.Add(pItem);
                    }
                }

                if (shObj["EventsOnDone"] is JsonArray doneArr)
                {
                    ImportWardenEvents(doneArr, wardenEvent.SpecialHud.EventsOnDone, context);
                }
            }

            // PlayerDialogue object
            if (evObj["PlayerDialogue"] is JsonObject pdObj)
            {
                string pdTypeStr = pdObj["Type"]?.ToString() ?? "Closest";
                Enum.TryParse<AwoPlayerDialogueType>(pdTypeStr, true, out var pdType);
                string pdIntStr = pdObj["IntensityState"]?.ToString() ?? "Exploration";
                Enum.TryParse<AwoDialogueIntensityState>(pdIntStr, true, out var pdInt);
                wardenEvent.PlayerDialogue.Type = pdType;
                wardenEvent.PlayerDialogue.CharacterID = pdObj["CharacterID"]?.GetValue<int>() ?? 0;
                wardenEvent.PlayerDialogue.IntensityState = pdInt;
            }

            // SetTerminalLog object
            if (evObj["SetTerminalLog"] is JsonObject stlObj)
            {
                wardenEvent.SetTerminalLog.TerminalIndex = stlObj["TerminalIndex"]?.GetValue<int>() ?? 0;
                string tlTypeStr = stlObj["Type"]?.ToString() ?? "Add";
                Enum.TryParse<AwoTerminalLogType>(tlTypeStr, true, out var tlType);
                wardenEvent.SetTerminalLog.Type = tlType;
                wardenEvent.SetTerminalLog.FileName = stlObj["FileName"]?.ToString() ?? string.Empty;
                wardenEvent.SetTerminalLog.FileContent = context.ResolveText(GetUintValue(stlObj["FileContent"]));
                wardenEvent.SetTerminalLog.FileContentOriginalLanguage = stlObj["FileContentOriginalLanguage"]?.GetValue<int>() ?? 1;
                wardenEvent.SetTerminalLog.AttachedAudioFile = stlObj["AttachedAudioFile"]?.GetValue<uint>() ?? 0;
                wardenEvent.SetTerminalLog.AttachedAudioByteSize = stlObj["AttachedAudioByteSize"]?.GetValue<uint>() ?? 0;
                wardenEvent.SetTerminalLog.PlayerDialogToTriggerAfterAudio = stlObj["PlayerDialogToTriggerAfterAudio"]?.GetValue<uint>() ?? 0;
                wardenEvent.SetTerminalLog.TargetDimensionIndex = stlObj["TargetDimensionIndex"]?.GetValue<int>() ?? 0;
                int tLayerInt = stlObj["TargetLayer"]?.GetValue<int>() ?? 0;
                wardenEvent.SetTerminalLog.TargetLayer = (SectorType)tLayerInt;
                wardenEvent.SetTerminalLog.TargetLocalIndex = stlObj["TargetLocalIndex"]?.GetValue<int>() ?? 0;
                wardenEvent.SetTerminalLog.TargetTerminalIndex = stlObj["TargetTerminalIndex"]?.GetValue<int>() ?? 0;

                if (stlObj["EventsOnFileRead"] is JsonArray efrArr)
                {
                    ImportWardenEvents(efrArr, wardenEvent.SetTerminalLog.EventsOnFileRead, context);
                }
            }

            // ObjectiveItems object
            if (evObj["ObjectiveItems"] is JsonArray objItemsArr)
            {
                wardenEvent.ObjectiveItems.Items.Clear();
                foreach (var itemNode in objItemsArr)
                {
                    if (itemNode is not JsonObject itemObj) continue;
                    string tagTypeStr = itemObj["TagType"]?.ToString() ?? "Custom";
                    Enum.TryParse<AwoPocketItemTagType>(tagTypeStr, true, out var tagType);
                    wardenEvent.ObjectiveItems.Items.Add(new AwoPocketItem
                    {
                        Index = itemObj["Index"]?.GetValue<int>() ?? 0,
                        Count = itemObj["Count"]?.GetValue<int>() ?? 1,
                        IsOnTop = itemObj["IsOnTop"]?.GetValue<bool>() ?? true,
                        ItemName = context.ResolveText(GetUintValue(itemObj["ItemName"])),
                        TagType = tagType,
                        PlayerIndex = itemObj["PlayerIndex"]?.GetValue<int>() ?? 0,
                        CustomTag = itemObj["CustomTag"]?.ToString() ?? string.Empty
                    });
                }
            }

            // EnvironmentData object
            if (evObj["EnvironmentData"] is JsonObject envObj)
            {
                wardenEvent.EnvironmentData.EnvironmentWetness = ParseEnvironmentValue(envObj["EnvironmentWetness"]);
                wardenEvent.EnvironmentData.UpdateColor = envObj["UpdateColor"]?.GetValue<bool>() ?? false;
                wardenEvent.EnvironmentData.DustColor = envObj["DustColor"]?.ToString() ?? "white";
                wardenEvent.EnvironmentData.DustAlphaBoost = ParseEnvironmentValue(envObj["DustAlphaBoost"]);
                wardenEvent.EnvironmentData.DustTurbulence = ParseEnvironmentValue(envObj["DustTurbulence"]);
            }

            // FadeScreen object
            if (evObj["FadeScreen"] is JsonObject fsObj)
            {
                wardenEvent.FadeScreen.FadeColor = fsObj["FadeColor"]?.ToString() ?? "black";
                wardenEvent.FadeScreen.CurveTimeStart = fsObj["CurveTimeStart"]?.GetValue<double>() ?? 0.0;
                wardenEvent.FadeScreen.CurveValueStart = fsObj["CurveValueStart"]?.GetValue<double>() ?? 0.0;
                wardenEvent.FadeScreen.CurveTimeEnd = fsObj["CurveTimeEnd"]?.GetValue<double>() ?? 1.0;
                wardenEvent.FadeScreen.CurveValueEnd = fsObj["CurveValueEnd"]?.GetValue<double>() ?? 1.0;
                wardenEvent.FadeScreen.FadeSpeed = fsObj["FadeSpeed"]?.GetValue<double>() ?? 1.0;
            }

            target.Add(wardenEvent);
        }
    }

    private ExpeditionBalanceSettings DeserializeExpeditionBalance(JsonObject obj)
    {
        var settings = new ExpeditionBalanceSettings();

        settings.HealthPerZone = obj["HealthPerZone"]?.GetValue<double>() ?? 0.0;
        settings.DisinfectionPerZone = obj["DisinfectionPerZone"]?.GetValue<double>() ?? 0.0;
        settings.WeaponAmmoPerZone = obj["WeaponAmmoPerZone"]?.GetValue<double>() ?? 0.0;
        settings.ToolAmmoPerZone = obj["ToolAmmoPerZone"]?.GetValue<double>() ?? 0.0;
        settings.CommodityValuePerZone = obj["CommodityValuePerZone"]?.GetValue<double>() ?? 0.0;
        settings.ChanceToPutCommodityInResourceContainer = obj["ChanceToPutCommodityInResourceContainer"]?.GetValue<double>() ?? 0.0;
        settings.ChanceToPutArtifactInResourceContainer = obj["ChanceToPutArtifactInResourceContainer"]?.GetValue<double>() ?? 0.0;
        settings.ChanceToSpawnCommodityLargePack = obj["ChanceToSpawnCommodityLargePack"]?.GetValue<double>() ?? 0.0;
        settings.ChanceToSpawnCommodityMediumPack = obj["ChanceToSpawnCommodityMediumPack"]?.GetValue<double>() ?? 0.0;
        settings.ChanceToReUseResourceContainer = obj["ChanceToReUseResourceContainer"]?.GetValue<double>() ?? 0.0;
        settings.MaxPacksPerResourceContainer = (int)(obj["MaxPacksPerResourceContainer"]?.GetValue<double>() ?? 0);
        settings.EmptyWeakResourceContainersPerZone = obj["EmptyWeakResourceContainersPerZone"]?.GetValue<double>() ?? 0.0;
        settings.EmptySecureResourceContainersPerZone = obj["EmptySecureResourceContainersPerZone"]?.GetValue<double>() ?? 0.0;
        settings.LootPerZone = obj["LootPerZone"]?.GetValue<double>() ?? 0.0;
        settings.AirPerZone = obj["AirPerZone"]?.GetValue<double>() ?? 0.0;
        settings.AirPerZoneInNoAir = obj["AirPerZoneInNoAir"]?.GetValue<double>() ?? 0.0;
        settings.TerminalsPerZone = obj["TerminalsPerZone"]?.GetValue<double>() ?? 0.0;

        settings.EnemyPatrolGroupsPerZone = (int)(obj["EnemyPatrolGroupsPerZone"]?.GetValue<double>() ?? 0);
        settings.StaticEnemiesMaxPerZone = (int)(obj["StaticEnemiesMaxPerZone"]?.GetValue<double>() ?? 0);
        settings.StaticEnemiesMaxSmallArea = (int)(obj["StaticEnemiesMaxSmallArea"]?.GetValue<double>() ?? 0);
        settings.StaticEnemiesMaxMediumArea = (int)(obj["StaticEnemiesMaxMediumArea"]?.GetValue<double>() ?? 0);
        settings.StaticEnemiesMaxLargeArea = (int)(obj["StaticEnemiesMaxLargeArea"]?.GetValue<double>() ?? 0);
        settings.StaticEnemiesMaxHugeArea = (int)(obj["StaticEnemiesMaxHugeArea"]?.GetValue<double>() ?? 0);
        settings.EnemyPopulationPerZone = obj["EnemyPopulationPerZone"]?.GetValue<double>() ?? 0.0;
        settings.VoxelCoverageAreaMultiplier = obj["VoxelCoverageAreaMultiplier"]?.GetValue<double>() ?? 1.0;
        settings.VoxelCoverageAreaScoringRandomMultiplier = obj["VoxelCoverageAreaScoringRandomMultiplier"]?.GetValue<double>() ?? 1.0;
        settings.ArtifactsPerSegment = (int)(obj["ArtifactsPerSegment"]?.GetValue<double>() ?? 0);
        settings.ArtifactsPerLayer = (int)(obj["ArtifactsPerLayer"]?.GetValue<double>() ?? 0);
        settings.WeakDoor4x4Health = obj["WeakDoor4x4Health"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoor8x4Health = obj["WeakDoor8x4Health"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorChanceLockWeightNoLock = obj["WeakDoorChanceLockWeightNoLock"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorChanceLockWeightMeleeLock = obj["WeakDoorChanceLockWeightMeleeLock"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorChanceLockWeightHackableLock = obj["WeakDoorChanceLockWeightHackableLock"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorUnlockedChanceForOpen = obj["WeakDoorUnlockedChanceForOpen"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorOpenChanceForWallRemoverUsed = obj["WeakDoorOpenChanceForWallRemoverUsed"]?.GetValue<double>() ?? 0.0;
        settings.WeakDoorLockHealth = obj["WeakDoorLockHealth"]?.GetValue<double>() ?? 0.0;
        settings.WeakResourceContainerWithPackChanceForLocked = obj["WeakResourceContainerWithPackChanceForLocked"]?.GetValue<double>() ?? 0.0;
        settings.GlueVolumeToDoorHealthConversion = obj["GlueVolumeToDoorHealthConversion"]?.GetValue<double>() ?? 0.0;
        settings.GlueVolumeForDoorGlueMaxState = obj["GlueVolumeForDoorGlueMaxState"]?.GetValue<double>() ?? 0.0;

        if (obj["TentacleTraps"] is JsonObject ttObj)
        {
            settings.TentacleTraps = new TentacleTrapsSettings
            {
                Health = ttObj["Health"]?.GetValue<double>() ?? 10.0,
                AttackDamage = ttObj["AttackDamage"]?.GetValue<double>() ?? 1.0,
                MaxPerZone = (int)(ttObj["MaxPerZone"]?.GetValue<double>() ?? 3),
                MaxSmallArea = (int)(ttObj["MaxSmallArea"]?.GetValue<double>() ?? 1),
                MaxMediumArea = (int)(ttObj["MaxMediumArea"]?.GetValue<double>() ?? 1),
                MaxLargeArea = (int)(ttObj["MaxLargeArea"]?.GetValue<double>() ?? 1),
                MaxHugeArea = (int)(ttObj["MaxHugeArea"]?.GetValue<double>() ?? 1)
            };
        }

        if (obj["ParasiteNests"] is JsonObject pnObj)
        {
            settings.ParasiteNests = new ParasiteNestsSettings
            {
                Health = pnObj["Health"]?.GetValue<double>() ?? 20.0,
                AttackDamage = pnObj["AttackDamage"]?.GetValue<double>() ?? 1.0,
                MaxPerZone = (int)(pnObj["MaxPerZone"]?.GetValue<double>() ?? 2),
                MaxSmallArea = (int)(pnObj["MaxSmallArea"]?.GetValue<double>() ?? 1),
                MaxMediumArea = (int)(pnObj["MaxMediumArea"]?.GetValue<double>() ?? 1),
                MaxLargeArea = (int)(pnObj["MaxLargeArea"]?.GetValue<double>() ?? 1),
                MaxHugeArea = (int)(pnObj["MaxHugeArea"]?.GetValue<double>() ?? 1)
            };
        }

        if (obj["ResourcePackSizes"] is JsonArray rpsArr)
        {
            settings.ResourcePackSizes = rpsArr
                .Where(n => n != null)
                .Select(n => n!.GetValue<double>())
                .ToList();
        }

        return settings;
    }

    private static string ParseEnvironmentValue(JsonNode? node)
    {
        if (node == null) return "Unchanged";

        if (node is JsonValue val)
        {
            if (val.TryGetValue<double>(out double dVal))
            {
                return dVal.ToString(CultureInfo.InvariantCulture);
            }
            if (val.TryGetValue<string>(out string? sVal))
            {
                return string.IsNullOrWhiteSpace(sVal) ? "Unchanged" : sVal;
            }
        }

        return node.ToString() ?? "Unchanged";
    }
}
