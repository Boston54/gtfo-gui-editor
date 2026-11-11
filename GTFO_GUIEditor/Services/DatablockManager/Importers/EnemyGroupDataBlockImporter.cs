using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class EnemyGroupDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var groups = new ObservableCollection<EnemyGroupBlock>();
        foreach (var entry in context.EnemyGroupBlocks.Values)
        {
            var group = new EnemyGroupBlock
            {
                Type = entry["Type"]?.GetValue<string>() ?? "Hibernate",
                Difficulty = entry["Difficulty"]?.GetValue<string>() ?? "Easy",
                SpawnPlacementType = entry["SpawnPlacementType"]?.GetValue<string>() ?? "Default",
                MaxScore = entry["MaxScore"]?.GetValue<double>() ?? 0,
                ScoreInAreaPaddingMulti = entry["ScoreInAreaPaddingMulti"]?.GetValue<double>() ?? 3,
                RelativeWeight = entry["RelativeWeight"]?.GetValue<double>() ?? 1,
                Name = entry["name"]?.GetValue<string>() ?? string.Empty,
                InternalEnabled = entry["internalEnabled"]?.GetValue<bool>() ?? true,
                PersistentId = entry["persistentID"]?.GetValue<uint>() ?? 0
            };
            if (entry["Roles"] is JsonArray roles)
            {
                foreach (var role in roles.OfType<JsonObject>())
                    group.Roles.Add(new EnemyGroupRole
                    {
                        Role = role["Role"]?.GetValue<string>() ?? string.Empty,
                        Distribution = role["Distribution"]?.GetValue<string>() ?? "Rel_100"
                    });
            }
            groups.Add(group);
        }

        context.MainVm.Enemies.Groups.Clear();
        foreach (var group in groups) context.MainVm.Enemies.Groups.Add(group);
        context.MainVm.Enemies.RefreshSpawningOptions();
    }
}
