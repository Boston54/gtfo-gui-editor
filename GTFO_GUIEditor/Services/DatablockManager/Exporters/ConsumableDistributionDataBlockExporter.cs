using System.Collections.Generic;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class ConsumableDistributionDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        var vanillaConsumableDistributionBlocks = ExportContext.LoadVanillaBlocksArray("ConsumableDistributionDataBlock.json");
        var consumableBlocks = SerializeConsumableDistributionBlocks(context.MainVm.Consumables.Blocks);
        var vanillaConsumableHeaders = ExportContext.LoadVanillaHeadersArray("ConsumableDistributionDataBlock.json");

        if (consumableBlocks.Count == 0)
        {
            consumableBlocks.AddRange(vanillaConsumableDistributionBlocks);
        }

        context.ConsumableDistributionBlocks.Clear();
        context.ConsumableDistributionBlocks.AddRange(consumableBlocks);

        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_ConsumableDistributionDataBlock_bin.json",
            context.ConsumableDistributionBlocks,
            vanillaConsumableHeaders);

        context.DatablocksExportedCount++;
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
