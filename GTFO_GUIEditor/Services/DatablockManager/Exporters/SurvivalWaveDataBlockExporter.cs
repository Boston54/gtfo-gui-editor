namespace GTFO_GUIEditor.Services;

public class SurvivalWaveDataBlockExporter : IDatablockExporter
{
    public void Export(ExportContext context)
    {
        // 1. EnemyDataBlock
        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_EnemyDataBlock_bin.json",
            context.VanillaEnemyBlocks);
        context.DatablocksExportedCount++;

        // 2. EnemyGroupDataBlock
        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_EnemyGroupDataBlock_bin.json",
            context.EnemyGroupBlocks.Count > 0 ? context.EnemyGroupBlocks : context.VanillaEnemyGroupBlocks);
        context.DatablocksExportedCount++;

        // 3. SurvivalWavePopulationDataBlock
        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_SurvivalWavePopulationDataBlock_bin.json",
            context.SurvivalWavePopulationBlocks.Count > 0
                ? context.SurvivalWavePopulationBlocks
                : context.VanillaSurvivalWavePopulationBlocks);
        context.DatablocksExportedCount++;

        // 4. SurvivalWaveSettingsDataBlock
        ExportContext.WriteDatablockFile(
            context.ExportRootPath,
            "GameData_SurvivalWaveSettingsDataBlock_bin.json",
            context.SurvivalWaveSettingsBlocks.Count > 0
                ? context.SurvivalWaveSettingsBlocks
                : context.VanillaSurvivalWaveSettingsBlocks);
        context.DatablocksExportedCount++;
    }
}
