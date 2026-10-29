using System.Collections.Generic;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Facade providing backward compatibility for existing datablock export calls by delegating to DatablockManager.
/// </summary>
public static class DatablockExportService
{
    public const string LGTunerAdvancedSchema = LevelLayoutDataBlockExporter.LGTunerAdvancedSchema;

    public static ExportResult ExportAll(MainWindowViewModel mainVm, string exportRootPath)
    {
        return DatablockManager.Instance.ExportDatablocks(mainVm, exportRootPath);
    }

    public static Task<ExportResult> ExportAllAsync(MainWindowViewModel mainVm, string exportRootPath)
    {
        return DatablockManager.Instance.ExportDatablocksAsync(mainVm, exportRootPath);
    }

    public static string NormalizeZoneString(string? zone)
    {
        return DoorGraphAnalyzer.NormalizeZoneString(zone);
    }

    public static string NormalizeLayerTypeString(string? layerType)
    {
        return DoorGraphAnalyzer.NormalizeLayerTypeString(layerType);
    }

    public static (string LayerType, string Zone)? ResolveBuildFrom(LevelLayoutGridState? gridState, DoorType targetDoorType)
    {
        return DoorGraphAnalyzer.ResolveBuildFrom(gridState, targetDoorType);
    }

    public static List<string> GetBulkheadZones(LevelLayoutGridState? gridState, SectorType sectorType)
    {
        return DoorGraphAnalyzer.GetBulkheadZones(gridState, sectorType);
    }

    public static List<string> GetMainBulkheadZones(LevelLayoutGridState? gridState)
    {
        return DoorGraphAnalyzer.GetMainBulkheadZones(gridState);
    }

    public static List<string> GetSecondaryBulkheadZones(LevelLayoutGridState? gridState)
    {
        return DoorGraphAnalyzer.GetSecondaryBulkheadZones(gridState);
    }
}
