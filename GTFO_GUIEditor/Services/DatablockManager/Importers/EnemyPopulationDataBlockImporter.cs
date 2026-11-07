using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class EnemyPopulationDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var population = new EnemyPopulationBlock();
        var source = context.EnemyPopulationBlocks.FirstOrDefault();
        if (source?["RoleDatas"] is JsonArray roles)
        {
            foreach (var node in roles.OfType<JsonObject>())
            {
                population.RoleDatas.Add(new EnemyRoleData
                {
                    Role = node["Role"]?.GetValue<string>() ?? string.Empty,
                    Difficulty = node["Difficulty"]?.GetValue<string>() ?? "Easy",
                    Enemy = node["Enemy"]?.GetValue<uint>() ?? 0,
                    Cost = node["Cost"]?.GetValue<double>() ?? 1.0,
                    Weight = node["Weight"]?.GetValue<double>() ?? 1.0
                });
            }
        }

        context.MainVm.Enemies.PopulationBlocks.Clear();
        context.MainVm.Enemies.PopulationBlocks.Add(population);
        context.MainVm.Enemies.Population = population;
        foreach (var role in population.RoleDatas)
            role.SelectedEnemy = context.MainVm.Enemies.Enemies.FirstOrDefault(e => e.PersistentId == role.Enemy);
        context.MainVm.Enemies.RefreshSpawningOptions();
    }
}
