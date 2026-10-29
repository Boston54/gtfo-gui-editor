using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class AlarmDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        var settings = context.FindFile(
            "SurvivalWaveSettingsDataBlock.json",
            "GameData_SurvivalWaveSettingsDataBlock_bin.json");
        var population = context.FindFile(
            "SurvivalWavePopulationDataBlock.json",
            "GameData_SurvivalWavePopulationDataBlock_bin.json");
        var chainedPuzzles = context.FindFile(
            "ChainedPuzzleDataBlock.json",
            "GameData_ChainedPuzzleDataBlock_bin.json");

        var alarms = AlarmService.LoadVanillaAlarms(settings, population, chainedPuzzles);
        var target = DatablockManager.Instance.Get<AlarmBlock>();
        target.Clear();
        foreach (var alarm in alarms)
        {
            target.Add(alarm);
        }

        var enemyPath = context.FindFile("EnemyDataBlock.json", "GameData_EnemyDataBlock_bin.json");
        if (context.TryDeserializeFile<EnemyDataBlockFile>(enemyPath, out var enemyFile) && enemyFile != null)
        {
            var enemies = DatablockManager.Instance.Get<EnemyEntry>();
            enemies.Clear();
            foreach (var enemy in enemyFile.Blocks) enemies.Add(enemy);
        }

        var puzzleTypePath = context.FindFile(
            "ChainedPuzzleTypeDataBlock.json",
            "GameData_ChainedPuzzleTypeDataBlock_bin.json");
        if (context.TryDeserializeFile<ChainedPuzzleTypeDataBlockFile>(puzzleTypePath, out var puzzleFile) && puzzleFile != null)
        {
            var puzzles = DatablockManager.Instance.Get<ChainedPuzzleTypeEntry>();
            puzzles.Clear();
            foreach (var puzzle in puzzleFile.Blocks) puzzles.Add(puzzle);
        }
    }

    private sealed class ChainedPuzzleTypeDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public System.Collections.Generic.List<ChainedPuzzleTypeEntry> Blocks { get; set; } = new();
    }
}
