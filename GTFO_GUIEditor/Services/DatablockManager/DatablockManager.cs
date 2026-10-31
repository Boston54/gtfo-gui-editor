using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Datablock Manager coordinating the complete translation of editor models into MTFO-compliant game datablocks.
/// Handles object-oriented, modular exporter pipelines for extensible datablock export.
/// </summary>
public class DatablockManager : IDatablockManager
{
    private static readonly Lazy<DatablockManager> _lazyInstance = new(() => new DatablockManager());
    public static DatablockManager Instance => _lazyInstance.Value;

    private readonly List<IDatablockExporter> _exporters = new();

    public DatablockManager()
    {
        RegisterDefaultExporters();
    }

    /// <inheritdoc />
    public ImportResult ImportDatablocks(MainWindowViewModel vm, string inputDirectory)
    {
        var importer = new DatablockImportService();
        return importer.Import(vm, inputDirectory);
    }

    /// <inheritdoc />
    public Task<ImportResult> ImportDatablocksAsync(MainWindowViewModel vm, string inputDirectory)
    {
        return Task.Run(() => ImportDatablocks(vm, inputDirectory));
    }

    private void RegisterDefaultExporters()
    {
        // Register exporters in dependency order
        _exporters.Add(new ChainedPuzzleDataBlockExporter());
        _exporters.Add(new ExpeditionBalanceDataBlockExporter());
        _exporters.Add(new ComplexResourceSetDataBlockExporter());
        _exporters.Add(new WardenObjectiveDataBlockExporter());
        _exporters.Add(new LevelLayoutDataBlockExporter());
        _exporters.Add(new LightSettingsDataBlockExporter());
        _exporters.Add(new FogSettingsDataBlockExporter());
        _exporters.Add(new ConsumableDistributionDataBlockExporter());
        _exporters.Add(new SurvivalWaveDataBlockExporter());
        _exporters.Add(new RundownDataBlockExporter());
        _exporters.Add(new GameSetupDataBlockExporter());
        _exporters.Add(new TextDataBlockExporter()); // Text exporter runs last to capture all synthesized strings
    }

    /// <summary>
    /// Registers a custom datablock exporter for extensible export pipelines.
    /// </summary>
    public void RegisterExporter(IDatablockExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        _exporters.Add(exporter);
    }

    /// <inheritdoc />
    public ExportResult ExportDatablocks(MainWindowViewModel vm, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(vm);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return new ExportResult
            {
                Success = false,
                Message = "Export output directory cannot be empty."
            };
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);
            var context = new ExportContext(vm, outputDirectory);

            foreach (var exporter in _exporters)
            {
                exporter.Export(context);
            }

            return new ExportResult
            {
                Success = true,
                DatablocksExportedCount = context.DatablocksExportedCount,
                LevelGenFilesExportedCount = context.LevelGenFilesExportedCount,
                OutputDirectory = outputDirectory,
                Message = $"Successfully exported {context.DatablocksExportedCount} datablocks and {context.LevelGenFilesExportedCount} level generation schemas to MTFO structure."
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                OutputDirectory = outputDirectory,
                Message = $"Export failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public Task<ExportResult> ExportDatablocksAsync(MainWindowViewModel vm, string outputDirectory)
    {
        return Task.Run(() => ExportDatablocks(vm, outputDirectory));
    }

    /// <inheritdoc />
    public (string LayerType, string Zone)? ResolveBuildFrom(LevelLayoutGridState? gridState, DoorType targetDoorType)
    {
        return DoorGraphAnalyzer.ResolveBuildFrom(gridState, targetDoorType);
    }

    /// <inheritdoc />
    public List<string> GetBulkheadZones(LevelLayoutGridState? gridState, SectorType sectorType)
    {
        return DoorGraphAnalyzer.GetBulkheadZones(gridState, sectorType);
    }

    /// <inheritdoc />
    public List<string> GetMainBulkheadZones(LevelLayoutGridState? gridState)
    {
        return DoorGraphAnalyzer.GetMainBulkheadZones(gridState);
    }

    /// <inheritdoc />
    public List<string> GetSecondaryBulkheadZones(LevelLayoutGridState? gridState)
    {
        return DoorGraphAnalyzer.GetSecondaryBulkheadZones(gridState);
    }
}
