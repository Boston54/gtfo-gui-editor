using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public class AlarmDataBlockImporter : IDatablockImporter
{
    public void Import(ImportContext context)
    {
        DatablockManager.Instance.TryDeserializeRawBlocks<AlarmService.SurvivalWavePopulationDataBlockFile>(
            out var populationFile,
            "SurvivalWavePopulationDataBlock.json",
            "GameData_SurvivalWavePopulationDataBlock_bin.json");
        DatablockManager.Instance.TryDeserializeRawBlocks<AlarmService.SurvivalWaveSettingsDataBlockFile>(
            out var settingsFile,
            "SurvivalWaveSettingsDataBlock.json",
            "GameData_SurvivalWaveSettingsDataBlock_bin.json");
        DatablockManager.Instance.TryDeserializeRawBlocks<AlarmService.ChainedPuzzleDataBlockFile>(
            out var puzzleFile,
            "ChainedPuzzleDataBlock.json",
            "GameData_ChainedPuzzleDataBlock_bin.json");
        var alarms = AlarmService.BuildAlarms(populationFile, settingsFile, puzzleFile);
        var target = DatablockManager.Instance.Get<AlarmBlock>();
        if (alarms.Count > 0)
        {
            target.Clear();
            foreach (var alarm in alarms)
            {
                target.Add(alarm);
            }
        }

        var enemyPath = context.FindFile("EnemyDataBlock.json", "GameData_EnemyDataBlock_bin.json");
        if (context.TryDeserializeFile<EnemyDataBlockFile>(enemyPath, out var enemyFile) && enemyFile != null)
        {
            var enemies = DatablockManager.Instance.Get<EnemyEntry>();
            if (enemyFile.Blocks.Count > 0)
            {
                enemies.Clear();
                foreach (var enemy in enemyFile.Blocks) enemies.Add(enemy);
            }
        }

        var puzzleTypePath = context.FindFile(
            "ChainedPuzzleTypeDataBlock.json",
            "GameData_ChainedPuzzleTypeDataBlock_bin.json");
        if (context.TryDeserializeFile<ChainedPuzzleTypeDataBlockFile>(puzzleTypePath, out var puzzleTypeFile) && puzzleTypeFile != null)
        {
            var puzzles = DatablockManager.Instance.Get<ChainedPuzzleTypeEntry>();
            if (puzzleTypeFile.Blocks.Count > 0)
            {
                puzzles.Clear();
                foreach (var puzzle in puzzleTypeFile.Blocks) puzzles.Add(puzzle);
            }
        }
    }

    private sealed class ChainedPuzzleTypeDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public System.Collections.Generic.List<ChainedPuzzleTypeEntry> Blocks { get; set; } = new();
    }
}
