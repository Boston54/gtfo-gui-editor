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

        // EnemyDataBlock is imported but intentionally exported as a stub.
        ExportContext.WriteDatablockFile(context.ExportRootPath, "GameData_EnemyDataBlock_bin.json",
            new List<JsonObject>());

        context.DatablocksExportedCount += 2;
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
                ["Type"] = "genericType",
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
