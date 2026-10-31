using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class RundownDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var rundownBlocks = new List<JsonObject>();
        var rundown = context.MainVm.Rundown.SelectedRundown;

        if (rundown != null)
        {
            uint rId = rundown.PersistentId > 0 ? rundown.PersistentId : context.RundownAlloc.Allocate();
            context.RundownAlloc.RegisterExisting(rId);

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
                        ? context.SynthesizeText(rundown.StorytellingData.Title, "Rundown Title")
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
                            ? context.SynthesizeText(rundown.StorytellingData.SurfaceDescription, "Surface Description")
                            : 0,
                    ["ExternalExpTitle"] = rundown.StorytellingData?.ExternalExpTitle ?? 0
                },
                ["VanityItemLayerDropDataBlock"] = rundown.VanityItemLayerDropDataBlock,
                ["TierA"] = SerializeTierExpeditions(rundown.TierA, context),
                ["TierB"] = SerializeTierExpeditions(rundown.TierB, context),
                ["TierC"] = SerializeTierExpeditions(rundown.TierC, context),
                ["TierD"] = SerializeTierExpeditions(rundown.TierD, context),
                ["TierE"] = SerializeTierExpeditions(rundown.TierE, context),
                ["name"] = rundown.Name,
                ["internalEnabled"] = rundown.InternalEnabled,
                ["persistentID"] = rId
            };

            rundownBlocks.Add(rObj);
        }

        context.RundownBlocks.Clear();
        context.RundownBlocks.AddRange(rundownBlocks);

        var vanillaRundownHeaders = ExportContext.LoadVanillaHeadersArray("RundownDataBlock.json");

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_RundownDataBlock_bin.json",
            context.RundownBlocks,
            vanillaRundownHeaders);

        context.DatablocksExportedCount++;
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
        ExportContext context)
    {
        var array = new JsonArray();
        if (expeditions == null) return array;

        var levelList = context.MainVm.LevelLayout.Levels.ToList();

        foreach (var exp in expeditions)
        {
            if (exp == null) continue;

            var matchingLevel = levelList.FirstOrDefault(l => l.PersistentId == exp.LevelLayoutData) ??
                                levelList.FirstOrDefault();
            uint expLevelLayoutId = exp.LevelLayoutData > 0
                ? exp.LevelLayoutData
                : (matchingLevel?.PersistentId ?? 0);

            uint complexSetId = 1;
            if (matchingLevel != null && context.LevelCrsIds.TryGetValue(matchingLevel, out var allocCrsId))
            {
                complexSetId = allocCrsId;
            }
            else if (exp.Expedition?.ComplexResourceData > 0)
            {
                complexSetId = exp.Expedition.ComplexResourceData;
            }

            uint expBalId = 1;
            if (matchingLevel != null && context.LevelExpeditionBalanceIds.TryGetValue(matchingLevel, out var allocBalId))
            {
                expBalId = allocBalId;
            }
            else if (exp.Expedition?.ExpeditionBalance > 0)
            {
                expBalId = exp.Expedition.ExpeditionBalance;
            }

            uint defaultMainObjId = 0;
            List<uint>? defaultMainObjIds = null;
            uint defaultSecObjId = 0;
            uint defaultThirdObjId = 0;

            if (matchingLevel != null && context.ObjectiveIds.TryGetValue(matchingLevel, out var objTuple))
            {
                defaultMainObjId = objTuple.Main;
                defaultSecObjId = objTuple.Secondary;
                defaultThirdObjId = objTuple.Overload;
                defaultMainObjIds = objTuple.Chained;
            }

            bool secEnabled = exp.SecondaryLayerEnabled;
            bool thirdEnabled = exp.ThirdLayerEnabled;

            string secLayerType = DoorGraphAnalyzer.NormalizeLayerTypeString(exp.BuildSecondaryFrom?.LayerType);
            string secZone = DoorGraphAnalyzer.NormalizeZoneString(exp.BuildSecondaryFrom?.Zone);

            string thirdLayerType = DoorGraphAnalyzer.NormalizeLayerTypeString(exp.BuildThirdFrom?.LayerType);
            string thirdZone = DoorGraphAnalyzer.NormalizeZoneString(exp.BuildThirdFrom?.Zone);

            var mainBulkheadZones = new List<string>();
            var secBulkheadZones = new List<string>();
            var thirdBulkheadZones = new List<string>();

            if (matchingLevel != null && matchingLevel.GridState != null)
            {
                mainBulkheadZones.AddRange(DoorGraphAnalyzer.GetMainBulkheadZones(matchingLevel.GridState));
                secBulkheadZones.AddRange(DoorGraphAnalyzer.GetSecondaryBulkheadZones(matchingLevel.GridState));
                thirdBulkheadZones.AddRange(DoorGraphAnalyzer.GetBulkheadZones(matchingLevel.GridState, SectorType.Overload));

                if (matchingLevel.HasSecondaryDoor || matchingLevel.GridState.HasSecondaryDoor)
                {
                    secEnabled = true;
                    var resolvedSec = DoorGraphAnalyzer.ResolveBuildFrom(matchingLevel.GridState, DoorType.SecondaryDoor);
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

                if (matchingLevel.HasOverloadDoor || matchingLevel.GridState.HasOverloadDoor)
                {
                    thirdEnabled = true;
                    var resolvedThird = DoorGraphAnalyzer.ResolveBuildFrom(matchingLevel.GridState, DoorType.OverloadDoor);
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
                if (matchingLevel != null && context.SecondaryLayoutIds.TryGetValue(matchingLevel, out var allocSecId))
                {
                    secLayoutId = allocSecId;
                }
                else
                {
                    secLayoutId = exp.SecondaryLayout > 0 ? exp.SecondaryLayout : 0;
                }
            }

            uint thirdLayoutId = 0;
            if (thirdEnabled)
            {
                if (matchingLevel != null && context.ThirdLayoutIds.TryGetValue(matchingLevel, out var allocThirdId))
                {
                    thirdLayoutId = allocThirdId;
                }
                else
                {
                    thirdLayoutId = exp.ThirdLayout > 0 ? exp.ThirdLayout : 0;
                }
            }

            var expNode = JsonSerializer.SerializeToNode(exp, ExportContext.JsonOptions);
            var expObj = expNode?.AsObject() ?? new JsonObject();

            if (expObj.TryGetPropertyValue("Expedition", out var expSettingsNode) && expSettingsNode is JsonObject expSettingsObj)
            {
                expSettingsObj["ComplexResourceData"] = complexSetId;
                expSettingsObj["ExpeditionBalance"] = expBalId;
            }

            if (expLevelLayoutId > 0)
            {
                expObj["LevelLayoutData"] = expLevelLayoutId;
            }

            if (expObj.TryGetPropertyValue("Descriptive", out var descNode) && descNode is JsonObject descObj)
            {
                if (!string.IsNullOrWhiteSpace(exp.Descriptive?.PublicName))
                {
                    descObj["PublicName"] = context.SynthesizeText(exp.Descriptive.PublicName, "Expedition Public Name");
                }
            }

            expObj["MainLayerData"] = SerializeLayerData(
                exp.MainLayerData,
                defaultMainObjId,
                mainBulkheadZones,
                matchingLevel?.MainSector,
                matchingLevel?.GridState,
                SectorType.Main,
                defaultMainObjIds);

            expObj["SecondaryLayerEnabled"] = secEnabled;
            expObj["SecondaryLayerData"] = SerializeLayerData(
                exp.SecondaryLayerData,
                defaultSecObjId,
                secBulkheadZones,
                matchingLevel?.SecondarySector,
                matchingLevel?.GridState,
                SectorType.Secondary);
            expObj["SecondaryLayout"] = secLayoutId;
            expObj["BuildSecondaryFrom"] = new JsonObject
            {
                ["LayerType"] = secLayerType,
                ["Zone"] = secZone
            };

            expObj["ThirdLayerEnabled"] = thirdEnabled;
            expObj["ThirdLayerData"] = SerializeLayerData(
                exp.ThirdLayerData,
                defaultThirdObjId,
                thirdBulkheadZones,
                matchingLevel?.OverloadSector,
                matchingLevel?.GridState,
                SectorType.Overload);
            expObj["ThirdLayout"] = thirdLayoutId;
            expObj["BuildThirdFrom"] = new JsonObject
            {
                ["LayerType"] = thirdLayerType,
                ["Zone"] = thirdZone
            };

            if (matchingLevel != null && matchingLevel.Dimensions.Count > 0)
            {
                var dimDatasArray = new JsonArray();
                foreach (var dim in matchingLevel.Dimensions)
                {
                    uint dimPersistentId = context.DimensionIds.TryGetValue(dim, out uint pid) ? pid : 0;
                    if (dimPersistentId > 0)
                    {
                        dimDatasArray.Add(new JsonObject
                        {
                            ["DimensionIndex"] = dim.DimensionIndex,
                            ["DimensionData"] = dimPersistentId,
                            ["Enabled"] = dim.InternalEnabled
                        });
                    }
                }
                if (dimDatasArray.Count > 0)
                {
                    expObj["DimensionDatas"] = dimDatasArray;
                }
            }
            else if (exp.DimensionDatas.HasValue)
            {
                expObj["DimensionDatas"] = ConvertToJsonArrayOrEmpty(exp.DimensionDatas);
            }

            array.Add(expObj);
        }

        return array;
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
}
