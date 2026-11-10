using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class EnemyDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        context.MainVm.Enemies.ImportedEnemyDataBlocks.Clear();
        var enemies = new ObservableCollection<EnemyEntry>();
        enemies.Add(new EnemyEntry { PersistentId = 0, Name = "None", EnemyType = "" });
        foreach (var enemy in AlarmService.LoadEnemies().Where(e => e.PersistentId != 0))
            if (!enemies.Any(e => e.PersistentId == enemy.PersistentId)) enemies.Add(enemy);
        foreach (var entry in context.EnemyBlocks.Values)
        {
            uint persistentId = entry["persistentID"]?.GetValue<uint>() ?? 0;
            if (persistentId != 0)
            {
                context.MainVm.Enemies.ImportedEnemyDataBlocks[persistentId] =
                    JsonNode.Parse(entry.ToJsonString())!.AsObject();
            }

            var enemy = new EnemyEntry
            {
                PersistentId = persistentId,
                Name = entry["name"]?.GetValue<string>() ?? string.Empty,
                EnemyType = entry["EnemyType"]?.GetValue<string>() ?? string.Empty,
                InternalEnabled = entry["internalEnabled"]?.GetValue<bool>() ?? true
            };
            var existing = enemies.FirstOrDefault(e => e.PersistentId == enemy.PersistentId);
            if (existing != null) enemies.Remove(existing);
            if (enemy.PersistentId != 0) enemies.Add(enemy);
        }

        context.MainVm.Enemies.Enemies.Clear();
        foreach (var enemy in enemies) context.MainVm.Enemies.Enemies.Add(enemy);

        context.MainVm.Enemies.RefreshSpawningOptions();
    }
}
