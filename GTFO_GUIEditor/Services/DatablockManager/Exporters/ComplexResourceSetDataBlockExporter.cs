using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class ComplexResourceSetDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var complexBlocks = new List<JsonObject>();
        var geoEntries = GeomorphService.LoadGeomorphs();
        var geoLookup = geoEntries
            .Where(e => !string.IsNullOrEmpty(e.PrefabPath))
            .GroupBy(e => e.PrefabPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var level in context.MainVm.LevelLayout.Levels)
        {
            if (level == null) continue;

            uint crsId = level.ComplexResourceSet > 0
                ? level.ComplexResourceSet
                : context.ComplexResourceAlloc.Allocate(level.PersistentId > 0 ? level.PersistentId : null);

            context.LevelCrsIds[level] = crsId;

            var gridState = level.GridState ?? new LevelLayoutGridState();
            var usedPrefabs = gridState.PlacedGeomorphs
                .Select(g => g.PrefabPath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (!string.IsNullOrWhiteSpace(level.ElevatorTile))
            {
                usedPrefabs.Add(level.ElevatorTile);
            }

            foreach (var dim in level.Dimensions)
            {
                if (!string.IsNullOrWhiteSpace(dim.DimensionGeomorph))
                {
                    usedPrefabs.Add(dim.DimensionGeomorph);
                }
            }

            var crsObj = BuildSynthesizedComplexResourceSet(
                crsId,
                string.IsNullOrWhiteSpace(level.Name) ? $"CRS_{crsId}" : $"Generated_ComplexSet_{level.Name}_Main",
                level.ComplexType,
                usedPrefabs,
                geoLookup);

            complexBlocks.Add(crsObj);
        }

        context.ComplexResourceSetBlocks.Clear();
        context.ComplexResourceSetBlocks.AddRange(complexBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_ComplexResourceSetDataBlock_bin.json",
            context.ComplexResourceSetBlocks);

        context.DatablocksExportedCount++;
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
}
