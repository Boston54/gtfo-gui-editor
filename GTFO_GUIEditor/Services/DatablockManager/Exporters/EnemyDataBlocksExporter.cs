using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class EnemyDataBlocksExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var groupBlocks = SerializeGroups(context, context.MainVm.Enemies.Groups);
        foreach (var synthesized in context.EnemyGroupBlocks)
        {
            uint id = synthesized["persistentID"]?.GetValue<uint>() ?? 0;
            if (id > 0 && !groupBlocks.Any(b => b["persistentID"]?.GetValue<uint>() == id))
            {
                var clone = JsonNode.Parse(synthesized.ToJsonString());
                if (clone is JsonObject cloneObj) groupBlocks.Add(cloneObj);
            }
        }

        foreach (var vanilla in context.VanillaEnemyGroupBlocks)
        {
            uint id = vanilla["persistentID"]?.GetValue<uint>() ?? 0;
            if (!groupBlocks.Any(b => b["persistentID"]?.GetValue<uint>() == id))
            {
                var clone = JsonNode.Parse(vanilla.ToJsonString());
                if (clone is JsonObject cloneObj) groupBlocks.Add(cloneObj);
            }
        }
        context.EnemyGroupBlocks.Clear();
        context.EnemyGroupBlocks.AddRange(groupBlocks);
        ExportContext.WriteDatablockFile(context.ExportRootPath, "GameData_EnemyGroupDataBlock_bin.json",
            context.EnemyGroupBlocks);

        var enemyBlocks = new List<JsonObject>();
        foreach (var vanilla in context.VanillaEnemyBlocks)
        {
            var clone = JsonNode.Parse(vanilla.ToJsonString())?.AsObject();
            if (clone != null) enemyBlocks.Add(clone);
        }

        // Imported enemy records are not exposed for editing, but must be carried
        // through so custom enemy definitions retain their complete schema.
        foreach (var imported in context.MainVm.Enemies.ImportedEnemyDataBlocks.Values)
        {
            uint id = imported["persistentID"]?.GetValue<uint>() ?? 0;
            if (id == 0) continue;

            int existingIndex = enemyBlocks.FindIndex(block =>
                block["persistentID"]?.GetValue<uint>() == id);
            var clone = JsonNode.Parse(imported.ToJsonString())?.AsObject();
            if (clone == null) continue;

            if (existingIndex >= 0)
                enemyBlocks[existingIndex] = clone;
            else
                enemyBlocks.Add(clone);
        }

        context.EnemyBlocks.Clear();
        context.EnemyBlocks.AddRange(enemyBlocks);
        foreach (var enemyBlock in context.EnemyBlocks)
        {
            EnsureModelDataVectorMetrics(enemyBlock);
        }
        ExportContext.WriteDatablockFile(context.ExportRootPath, "GameData_EnemyDataBlock_bin.json",
            context.EnemyBlocks,
            ExportContext.LoadVanillaHeadersArray("EnemyDataBlock.json"));

        var populationBlocks = new List<JsonObject>();
        foreach (var vanilla in context.VanillaEnemyPopulationBlocks)
        {
            var clone = JsonNode.Parse(vanilla.ToJsonString())?.AsObject();
            if (clone != null) populationBlocks.Add(clone);
        }

        var populationRoles = new JsonArray();
        foreach (var role in context.MainVm.Enemies.Population.RoleDatas)
        {
            populationRoles.Add(new JsonObject
            {
                ["Role"] = role.Role,
                ["Difficulty"] = role.Difficulty,
                ["Enemy"] = role.Enemy,
                ["Cost"] = role.Cost,
                ["Weight"] = role.Weight
            });
        }

        if (populationBlocks.Count == 0)
        {
            populationBlocks.Add(new JsonObject
            {
                ["RoleDatas"] = populationRoles,
                ["name"] = "DefaultPop",
                ["internalEnabled"] = true,
                ["persistentID"] = 1
            });
        }
        else
        {
            populationBlocks[0]["RoleDatas"] = populationRoles;
        }

        context.EnemyPopulationBlocks.Clear();
        context.EnemyPopulationBlocks.AddRange(populationBlocks);
        ExportContext.WriteDatablockFile(context.ExportRootPath, "GameData_EnemyPopulationDataBlock_bin.json",
            context.EnemyPopulationBlocks,
            ExportContext.LoadVanillaHeadersArray("EnemyPopulationDataBlock.json"));

        context.DatablocksExportedCount += 3;
    }

    private static List<JsonObject> SerializeGroups(ExportContext context, IEnumerable<EnemyGroupBlock> groups)
    {
        var list = new List<JsonObject>();
        foreach (var group in groups)
        {
            uint id = group.PersistentId > 0 ? group.PersistentId : context.EnemyGroupAlloc.Allocate();
            var roles = new JsonArray();
            foreach (var role in group.Roles)
            {
                roles.Add(new JsonObject
                {
                    ["Role"] = role.Role,
                    ["Distribution"] = role.Distribution
                });
            }

            list.Add(new JsonObject
            {
                ["Type"] = string.IsNullOrWhiteSpace(group.Type) ? "Hibernate" : group.Type,
                ["Difficulty"] = group.Difficulty,
                ["SpawnPlacementType"] = group.SpawnPlacementType,
                ["MaxScore"] = group.MaxScore,
                ["ScoreInAreaPaddingMulti"] = group.ScoreInAreaPaddingMulti,
                ["RelativeWeight"] = group.RelativeWeight,
                ["Roles"] = roles,
                ["name"] = group.Name,
                ["internalEnabled"] = true,
                ["persistentID"] = id
            });
        }
        return list;
    }

    private static void EnsureModelDataVectorMetrics(JsonObject enemyBlock)
    {
        if (enemyBlock["ModelDatas"] is not JsonArray modelDatas) return;

        foreach (var modelData in modelDatas)
        {
            AddVectorMetrics(modelData);
        }
    }

    private static void AddVectorMetrics(JsonNode? node, string? propertyName = null)
    {
        if (node is JsonObject obj)
        {
            bool hasX = TryGetDouble(obj["x"], out double x);
            bool hasY = TryGetDouble(obj["y"], out double y);
            bool hasZ = TryGetDouble(obj["z"], out double z);

            if (hasX && hasY)
            {
                if (!hasZ) z = 0.0;
                double sqrMagnitude = (x * x) + (y * y) + (z * z);

                obj["magnitude"] ??= System.Math.Sqrt(sqrMagnitude);
                obj["sqrMagnitude"] ??= sqrMagnitude;

                if (IsScaleProperty(propertyName))
                {
                    EnsureNormalized(obj, x, y, z, hasZ, sqrMagnitude);
                }
            }

            foreach (var property in obj.ToList())
            {
                AddVectorMetrics(property.Value, property.Key);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                AddVectorMetrics(child, propertyName);
            }
        }
    }

    private static bool IsScaleProperty(string? propertyName)
    {
        return propertyName is "NeckScale" or "HeadScale" or "ChestScale" or
            "ArmScale" or "LegScale" or "SizeRange";
    }

    private static void EnsureNormalized(
        JsonObject obj,
        double x,
        double y,
        double z,
        bool hasZ,
        double sqrMagnitude)
    {
        if (obj["normalized"] is JsonObject existingNormalized)
        {
            ReorderNormalized(obj, existingNormalized);
            return;
        }

        double magnitude = System.Math.Sqrt(sqrMagnitude);
        double normalizedX = magnitude > double.Epsilon ? x / magnitude : 0.0;
        double normalizedY = magnitude > double.Epsilon ? y / magnitude : 0.0;
        double normalizedZ = magnitude > double.Epsilon ? z / magnitude : 0.0;
        double normalizedSqrMagnitude = (normalizedX * normalizedX) +
            (normalizedY * normalizedY) + (hasZ ? normalizedZ * normalizedZ : 0.0);

        var normalized = new JsonObject
        {
            ["x"] = normalizedX,
            ["y"] = normalizedY
        };

        if (hasZ)
        {
            normalized["z"] = normalizedZ;
        }

        normalized["magnitude"] = System.Math.Sqrt(normalizedSqrMagnitude);
        normalized["sqrMagnitude"] = normalizedSqrMagnitude;

        obj["normalized"] = normalized;
    }

    private static void ReorderNormalized(JsonObject parent, JsonObject existing)
    {
        var reordered = new JsonObject();
        AddClonedProperty(existing, reordered, "x");
        AddClonedProperty(existing, reordered, "y");
        AddClonedProperty(existing, reordered, "z");
        AddClonedProperty(existing, reordered, "magnitude");
        AddClonedProperty(existing, reordered, "sqrMagnitude");

        foreach (var property in existing)
        {
            if (property.Key is "x" or "y" or "z" or "magnitude" or "sqrMagnitude")
            {
                continue;
            }

            reordered[property.Key] = CloneNode(property.Value);
        }

        parent["normalized"] = reordered;
    }

    private static void AddClonedProperty(JsonObject source, JsonObject destination, string propertyName)
    {
        if (source.TryGetPropertyValue(propertyName, out var value))
        {
            destination[propertyName] = CloneNode(value);
        }
    }

    private static JsonNode? CloneNode(JsonNode? node)
    {
        return node == null ? null : JsonNode.Parse(node.ToJsonString());
    }

    private static bool TryGetDouble(JsonNode? node, out double value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<double>(out value))
        {
            return true;
        }

        value = 0.0;
        return false;
    }
}
