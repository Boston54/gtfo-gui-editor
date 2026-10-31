using System;
using System.Collections.Generic;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Service orchestrating the complete datablock & config import pipeline.
/// </summary>
public class DatablockImportService
{
    private readonly List<IDatablockImporter> _importers = new()
    {
        new LightSettingsDataBlockImporter(),
        new FogSettingsDataBlockImporter(),
        new ConsumableDistributionDataBlockImporter(),
        new ChainedPuzzleDataBlockImporter(),
        new LevelLayoutDataBlockImporter(),
        new RundownDataBlockImporter()
    };

    public ImportResult Import(MainWindowViewModel mainVm, string importRootPath)
    {
        if (mainVm == null) throw new ArgumentNullException(nameof(mainVm));
        if (string.IsNullOrWhiteSpace(importRootPath)) throw new ArgumentException("Import path cannot be null or empty", nameof(importRootPath));

        var context = new ImportContext(mainVm, importRootPath);

        foreach (var importer in _importers)
        {
            try
            {
                importer.Import(context);
            }
            catch (Exception ex)
            {
                context.Warnings.Add($"Importer '{importer.GetType().Name}' encountered an error: {ex.Message}");
            }
        }

        var result = new ImportResult
        {
            Success = context.LevelsImportedCount > 0 || context.DatablocksImportedCount > 0,
            InputDirectory = importRootPath,
            DatablocksImportedCount = context.DatablocksImportedCount,
            LevelsImportedCount = context.LevelsImportedCount,
            SkippedLevels = context.SkippedLevels,
            Warnings = context.Warnings,
            FailedLevels = context.FailedLevels
        };

        if (context.FailedLevels.Count > 0)
        {
            mainVm.ImportFailedLevels.Clear();
            foreach (var failure in context.FailedLevels)
            {
                mainVm.ImportFailedLevels.Add(failure);
            }
            mainVm.ImportNotificationTitle = "Level Import Failure";
            mainVm.ImportNotificationMessage = $"{context.FailedLevels.Count} level(s) could not be loaded due to missing LGTunerAdvanced configuration file(s).";
            mainVm.IsShowingImportNotification = true;
        }

        if (result.Success)
        {
            result.Message = $"Successfully imported {result.LevelsImportedCount} level(s) and {result.DatablocksImportedCount} datablock file(s).";
            if (result.SkippedLevels.Count > 0)
            {
                result.Message += $" {result.SkippedLevels.Count} level(s) skipped.";
            }
        }
        else
        {
            result.Message = "No valid datablocks or levels could be imported from the specified directory.";
        }

        return result;
    }
}
