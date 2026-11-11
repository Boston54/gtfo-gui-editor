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
}
