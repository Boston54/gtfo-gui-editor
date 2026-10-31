using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class RundownDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        JsonObject? targetRundownObj = null;
        uint rundownId = 1;

        if (context.GameSetupBlocks.Values.FirstOrDefault() is JsonObject gameSetup &&
            gameSetup["RundownIdsToLoad"] is JsonArray rundownIds && rundownIds.Count > 0)
        {
            rundownId = rundownIds[0]?.GetValue<uint>() ?? 1;
            context.RundownBlocks.TryGetValue(rundownId, out targetRundownObj);
        }

        if (targetRundownObj == null && context.RundownBlocks.Count > 0)
        {
            var kvp = context.RundownBlocks.First();
            rundownId = kvp.Key;
            targetRundownObj = kvp.Value;
        }

        if (targetRundownObj == null)
        {
            return;
        }

        var rundown = new RundownBlock
        {
            PersistentId = rundownId,
            Name = targetRundownObj["name"]?.ToString() ?? $"Rundown_{rundownId}",
            InternalEnabled = targetRundownObj["internalEnabled"]?.GetValue<bool>() ?? true,
            NeverShowRundownTree = targetRundownObj["NeverShowRundownTree"]?.GetValue<bool>() ?? false,
            UseTierUnlockRequirements = targetRundownObj["UseTierUnlockRequirements"]?.GetValue<bool>() ?? false,
            VanityItemLayerDropDataBlock = targetRundownObj["VanityItemLayerDropDataBlock"]?.GetValue<uint>() ?? 0
        };

        // Tier Requirements
        rundown.ReqToReachTierA = DeserializeTierRequirements(targetRundownObj["ReqToReachTierA"] as JsonObject);
        rundown.ReqToReachTierB = DeserializeTierRequirements(targetRundownObj["ReqToReachTierB"] as JsonObject);
        rundown.ReqToReachTierC = DeserializeTierRequirements(targetRundownObj["ReqToReachTierC"] as JsonObject);
        rundown.ReqToReachTierD = DeserializeTierRequirements(targetRundownObj["ReqToReachTierD"] as JsonObject);
        rundown.ReqToReachTierE = DeserializeTierRequirements(targetRundownObj["ReqToReachTierE"] as JsonObject);

        // Storytelling Data
        if (targetRundownObj["StorytellingData"] is JsonObject storyObj)
        {
            rundown.StorytellingData = new StorytellingData
            {
                Title = context.ResolveText(storyObj["Title"]?.GetValue<uint>() ?? 0),
                TextLog = storyObj["TextLog"]?.GetValue<uint>() ?? 0,
                SurfaceDescription = context.ResolveText(storyObj["SurfaceDescription"]?.GetValue<uint>() ?? 0),
                ExternalExpTitle = storyObj["ExternalExpTitle"]?.GetValue<uint>() ?? 0
            };

            if (storyObj["TextLogPos"] is JsonObject tlPos)
            {
                rundown.StorytellingData.TextLogPos = new Vector2Data
                {
                    X = tlPos["x"]?.GetValue<double>() ?? 0,
                    Y = tlPos["y"]?.GetValue<double>() ?? 0
                };
            }

            if (storyObj["SurfaceIconPosition"] is JsonObject siPos)
            {
                rundown.StorytellingData.SurfaceIconPosition = new Vector2Data
                {
                    X = siPos["x"]?.GetValue<double>() ?? 0,
                    Y = siPos["y"]?.GetValue<double>() ?? 0
                };
            }

            if (storyObj["Visuals"] is JsonObject visualsObj)
            {
                rundown.StorytellingData.Visuals = new RundownVisuals
                {
                    ColorBackground = DeserializeColor(visualsObj["ColorBackground"] as JsonObject),
                    TierAVisuals = DeserializeTierVisuals(visualsObj["TierAVisuals"] as JsonObject),
                    TierBVisuals = DeserializeTierVisuals(visualsObj["TierBVisuals"] as JsonObject),
                    TierCVisuals = DeserializeTierVisuals(visualsObj["TierCVisuals"] as JsonObject),
                    TierDVisuals = DeserializeTierVisuals(visualsObj["TierDVisuals"] as JsonObject),
                    TierEVisuals = DeserializeTierVisuals(visualsObj["TierEVisuals"] as JsonObject)
                };
            }
        }

        // Tiers A-E Expeditions
        rundown.TierA = DeserializeTierExpeditions(targetRundownObj["TierA"] as JsonArray, context);
        rundown.TierB = DeserializeTierExpeditions(targetRundownObj["TierB"] as JsonArray, context);
        rundown.TierC = DeserializeTierExpeditions(targetRundownObj["TierC"] as JsonArray, context);
        rundown.TierD = DeserializeTierExpeditions(targetRundownObj["TierD"] as JsonArray, context);
        rundown.TierE = DeserializeTierExpeditions(targetRundownObj["TierE"] as JsonArray, context);

        context.MainVm.Rundown.SelectedRundown = rundown;
    }

    private TierRequirements DeserializeTierRequirements(JsonObject? obj)
    {
        if (obj == null) return new TierRequirements();

        return new TierRequirements
        {
            MainSectors = obj["MainSectors"]?.GetValue<int>() ?? 0,
            SecondarySectors = obj["SecondarySectors"]?.GetValue<int>() ?? 0,
            ThirdSectors = obj["ThirdSectors"]?.GetValue<int>() ?? 0,
            AllClearedSectors = obj["AllClearedSectors"]?.GetValue<int>() ?? obj["AllClearSectors"]?.GetValue<int>() ?? 0
        };
    }

    private ColorData DeserializeColor(JsonObject? obj)
    {
        if (obj == null) return new ColorData(1.0, 1.0, 1.0, 1.0);

        return new ColorData(
            obj["a"]?.GetValue<double>() ?? 1.0,
            obj["r"]?.GetValue<double>() ?? 1.0,
            obj["g"]?.GetValue<double>() ?? 1.0,
            obj["b"]?.GetValue<double>() ?? 1.0);
    }

    private TierVisuals DeserializeTierVisuals(JsonObject? obj)
    {
        if (obj == null) return new TierVisuals();

        return new TierVisuals
        {
            Color = DeserializeColor(obj["Color"] as JsonObject),
            Scale = obj["Scale"]?.GetValue<double>() ?? 0.65,
            ScaleYModifier = obj["ScaleYModifier"]?.GetValue<double>() ?? 1.0
        };
    }

    private List<ExpeditionBlock> DeserializeTierExpeditions(JsonArray? tierArray, ImportContext context)
    {
        var result = new List<ExpeditionBlock>();
        if (tierArray == null) return result;

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
                expData.MLSLevelKit = innerExpObj["MLSLevelKit"]?.GetValue<uint>() ?? 0;
                expData.LightSettings = innerExpObj["LightSettings"]?.GetValue<uint>() ?? 1;
                expData.FogSettings = innerExpObj["FogSettings"]?.GetValue<uint>() ?? 1;
                expData.EnemyPopulation = innerExpObj["EnemyPopulation"]?.GetValue<uint>() ?? 0;
                expData.ExpeditionBalance = innerExpObj["ExpeditionBalance"]?.GetValue<uint>() ?? 1;
                expData.ScoutWaveSettings = innerExpObj["ScoutWaveSettings"]?.GetValue<uint>() ?? 0;
                expData.ScoutWavePopulation = innerExpObj["ScoutWavePopulation"]?.GetValue<uint>() ?? 0;
                expData.EnvironmentWetness = innerExpObj["EnvironmentWetness"]?.GetValue<double>() ?? 0.0;
                expData.DustTurbulence = innerExpObj["DustTurbulence"]?.GetValue<double>() ?? 1.0;

                if (innerExpObj["DustColor"] is JsonObject dustObj)
                {
                    expData.DustColor = DeserializeColor(dustObj);
                }
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

            result.Add(expBlock);
        }

        return result;
    }

    private static JsonElement? DeserializeJsonElement(JsonNode? node)
    {
        if (node == null) return null;
        return JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());
    }
}
