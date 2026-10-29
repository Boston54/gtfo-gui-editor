using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly List<IDatablockImporter> _importers = new();
    private readonly Dictionary<Type, object> _datablocks = new();

    public DatablockManager()
    {
        RegisterDatablockCollection<AlarmBlock>();
        RegisterDatablockCollection<ComplexResourceSetBlock>();
        RegisterDatablockCollection<ConsumableDistributionBlock>();
        RegisterDatablockCollection<FogSettingsBlock>();
        RegisterDatablockCollection<LightSettingsBlock>();
        RegisterDatablockCollection<LevelLayoutBlock>();
        RegisterDatablockCollection<RundownBlock>();
        RegisterDatablockCollection<EnemyEntry>();
        RegisterDatablockCollection<ChainedPuzzleTypeEntry>();
        RegisterDefaultImporters();
        RegisterDefaultExporters();
    }

    private void RegisterDatablockCollection<T>() where T : class
    {
        _datablocks[typeof(T)] = new ObservableCollection<T>();
    }

    /// <inheritdoc />
    public ObservableCollection<T> Get<T>() where T : class
    {
        if (_datablocks.TryGetValue(typeof(T), out var collection))
        {
            return (ObservableCollection<T>)collection;
        }

        throw new KeyNotFoundException($"No master datablock collection is registered for {typeof(T).Name}.");
    }

    /// <inheritdoc />
    public void LoadAll(string folderPath)
    {
        ImportAll(folderPath);
    }

    /// <inheritdoc />
    public void ImportAll(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Datablock folder cannot be null or whitespace.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Datablock folder not found: {folderPath}");
        }

        var context = new ImportContext(folderPath);
        foreach (var importer in _importers)
        {
            importer.Import(context);
        }
    }

    /// <inheritdoc />
    public Task ImportAllAsync(string folderPath)
    {
        return Task.Run(() => ImportAll(folderPath));
    }

    private static string? FindFile(string folderPath, params string[] names)
    {
        foreach (var name in names)
        {
            var direct = Path.Combine(folderPath, name);
            if (File.Exists(direct))
            {
                return direct;
            }

            var match = Directory.EnumerateFiles(folderPath, name, SearchOption.AllDirectories).FirstOrDefault();
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source) where T : class
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void LoadLevelLayouts(string folderPath)
    {
        var path = FindFile(folderPath, "LevelLayoutDataBlock.json", "GameData_LevelLayoutDataBlock_bin.json");
        if (path == null || !File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var file = JsonSerializer.Deserialize<LevelLayoutDataBlockFile>(json, options);
            if (file?.Blocks != null)
            {
                ReplaceCollection(Get<LevelLayoutBlock>(), file.Blocks);
            }
        }
        catch
        {
            // Keep the existing collection when an optional source file is invalid.
        }
    }

    private sealed class LevelLayoutDataBlockFile
    {
        [JsonPropertyName("Blocks")]
        public List<LevelLayoutBlock>? Blocks { get; set; }
    }

    private void RegisterDefaultImporters()
    {
        // Source blocks are imported before blocks synthesized from them.
        _importers.Add(new CoreDataBlockImporter());
        _importers.Add(new LevelLayoutDataBlockImporter());
        _importers.Add(new AlarmDataBlockImporter());
    }

    /// <summary>
    /// Registers a custom datablock importer for extensible import pipelines.
    /// </summary>
    public void RegisterImporter(IDatablockImporter importer)
    {
        ArgumentNullException.ThrowIfNull(importer);
        _importers.Add(importer);
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
