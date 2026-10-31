using System.Collections.Generic;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// System responsible for translating editor state and view models into game datablocks and LGTuner configurations.
/// Completely separates editor domain models from serialized game datablocks.
/// </summary>
public interface IDatablockManager
{
    /// <summary>
    /// Exports all datablocks and configuration files to the specified output directory synchronously.
    /// </summary>
    ExportResult ExportDatablocks(MainWindowViewModel vm, string outputDirectory);

    /// <summary>
    /// Exports all datablocks and configuration files to the specified output directory asynchronously.
    /// </summary>
    Task<ExportResult> ExportDatablocksAsync(MainWindowViewModel vm, string outputDirectory);

    /// <summary>
    /// Imports datablocks and configuration files from the specified input directory into the editor viewmodels.
    /// </summary>
    ImportResult ImportDatablocks(MainWindowViewModel vm, string inputDirectory);

    /// <summary>
    /// Imports datablocks and configuration files from the specified input directory into the editor viewmodels asynchronously.
    /// </summary>
    Task<ImportResult> ImportDatablocksAsync(MainWindowViewModel vm, string inputDirectory);

    /// <summary>
    /// Resolves the source layer and zone for bulkhead door connections.
    /// </summary>
    (string LayerType, string Zone)? ResolveBuildFrom(LevelLayoutGridState? gridState, DoorType targetDoorType);

    /// <summary>
    /// Finds all zones in the specified sector that contain bulkhead door entrances.
    /// </summary>
    List<string> GetBulkheadZones(LevelLayoutGridState? gridState, SectorType sectorType);

    /// <summary>
    /// Finds all zones in the main sector that contain bulkhead door entrances.
    /// </summary>
    List<string> GetMainBulkheadZones(LevelLayoutGridState? gridState);

    /// <summary>
    /// Finds all zones in the secondary sector that contain bulkhead door entrances.
    /// </summary>
    List<string> GetSecondaryBulkheadZones(LevelLayoutGridState? gridState);
}
