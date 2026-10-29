using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Loads all supported datablocks from a datablocks folder.
    /// </summary>
    void LoadAll(string folderPath);

    /// <summary>
    /// Imports all supported datablock JSON files through the registered importer pipeline.
    /// </summary>
    void ImportAll(string folderPath);

    /// <summary>
    /// Imports all supported datablock JSON files asynchronously.
    /// </summary>
    Task ImportAllAsync(string folderPath);

    /// <summary>
    /// Gets the master collection for a datablock model type.
    /// </summary>
    ObservableCollection<T> Get<T>() where T : class;

    /// <summary>
    /// Exports all datablocks and configuration files to the specified output directory synchronously.
    /// </summary>
    ExportResult ExportDatablocks(MainWindowViewModel vm, string outputDirectory);

    /// <summary>
    /// Exports all datablocks and configuration files to the specified output directory asynchronously.
    /// </summary>
    Task<ExportResult> ExportDatablocksAsync(MainWindowViewModel vm, string outputDirectory);

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
