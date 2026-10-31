using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class ConsumableDistributionDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var blocks = new ObservableCollection<ConsumableDistributionBlock>();

        foreach (var kvp in context.ConsumableDistributionBlocks)
        {
            var obj = kvp.Value;
            if (obj == null) continue;

            uint persistentId = kvp.Key;
            string name = obj["name"]?.ToString() ?? $"Consumables_{persistentId}";
            bool internalEnabled = obj["internalEnabled"]?.GetValue<bool>() ?? true;
            int spawnsPerZone = obj["SpawnsPerZone"]?.GetValue<int>() ?? 5;
            double chance = obj["ChanceToSpawnInResourceContainer"]?.GetValue<double>() ?? 0.8;

            var block = new ConsumableDistributionBlock
            {
                PersistentId = persistentId,
                Name = name,
                InternalEnabled = internalEnabled,
                SpawnsPerZone = spawnsPerZone,
                ChanceToSpawnInResourceContainer = chance,
                SpawnData = new ObservableCollection<ConsumableDistributionItem>()
            };

            if (obj["SpawnData"] is JsonArray spawnArray)
            {
                foreach (var itemNode in spawnArray)
                {
                    if (itemNode is not JsonObject itemObj) continue;

                    uint itemId = itemObj["ItemID"]?.GetValue<uint>() ?? 0;
                    double weight = itemObj["Weight"]?.GetValue<double>() ?? 1.0;

                    block.SpawnData.Add(new ConsumableDistributionItem
                    {
                        ItemId = itemId,
                        Weight = weight
                    });
                }
            }

            blocks.Add(block);
        }

        context.MainVm.Consumables.Blocks.Clear();
        foreach (var b in blocks)
        {
            context.MainVm.Consumables.Blocks.Add(b);
        }
        if (context.MainVm.Consumables.Blocks.Count > 0)
        {
            context.MainVm.Consumables.SelectedBlock = context.MainVm.Consumables.Blocks[0];
        }
    }
}
