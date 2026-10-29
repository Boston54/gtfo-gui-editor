using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private readonly Dictionary<string, List<JsonObject>> _rawBlocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonArray> _rawHeaders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ComplexType, JsonObject> _complexResourceTemplates = new();

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
        LoadComplexResourceTemplates();
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
    public LevelLayoutBlock? GetLevelLayout(uint persistentId)
    {
        return Get<LevelLayoutBlock>().FirstOrDefault(level => level.PersistentId == persistentId);
    }

    /// <inheritdoc />
    public LevelLayoutGridState? GetLevelLayoutGridState(uint persistentId)
    {
        return GetLevelLayout(persistentId)?.GridState;
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

        var vanillaFolder = GetVanillaDatablocksDirectory();
        if (!PathsEqual(vanillaFolder, folderPath))
        {
            ImportFromFolder(vanillaFolder);
        }

        ImportFromFolder(folderPath);
    }

    private void ImportFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        CacheRawDatablocks(folderPath);
        var context = new ImportContext(folderPath);
        foreach (var importer in _importers)
        {
            importer.Import(context);
        }
    }

    private static bool PathsEqual(string first, string second)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public string GetVanillaDatablocksDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Vanilla", "Datablocks");
        string[] candidates =
        {
            outputDir,
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Vanilla", "Datablocks"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate)) return candidate;
        }

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Data", "Vanilla", "Datablocks"),
                Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data", "Vanilla", "Datablocks")
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }
            dir = dir.Parent;
        }

        return outputDir;
    }

    private static string NormalizeDatablockFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (name.StartsWith("GameData_", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring("GameData_".Length);
        }
        if (name.EndsWith("_bin.json", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - "_bin.json".Length) + ".json";
        }
        if (name.Equals("ConsumableDistributionDatablock.json", StringComparison.OrdinalIgnoreCase))
        {
            name = "ConsumableDistributionDataBlock.json";
        }
        return name;
    }

    private void CacheRawDatablocks(string folderPath)
    {
        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(filePath));
                var root = document.RootElement;
                var normalizedName = NormalizeDatablockFileName(filePath);

                if (root.TryGetProperty("Blocks", out var blocksElement) &&
                    blocksElement.ValueKind == JsonValueKind.Array &&
                    blocksElement.GetArrayLength() > 0)
                {
                    var blocks = new List<JsonObject>();
                    foreach (var element in blocksElement.EnumerateArray())
                    {
                        var node = JsonNode.Parse(element.GetRawText())?.AsObject();
                        if (node != null) blocks.Add(JsonNode.Parse(node.ToJsonString())!.AsObject());
                    }
                    if (blocks.Count > 0)
                    {
                        _rawBlocks[normalizedName] = blocks;
                        _rawBlocks[Path.GetFileName(filePath)] = blocks;
                    }
                }

                if (root.TryGetProperty("Headers", out var headersElement) &&
                    headersElement.ValueKind == JsonValueKind.Array)
                {
                    var headers = JsonNode.Parse(headersElement.GetRawText())?.AsArray();
                    if (headers != null)
                    {
                        _rawHeaders[normalizedName] = JsonNode.Parse(headers.ToJsonString())!.AsArray();
                        _rawHeaders[Path.GetFileName(filePath)] = JsonNode.Parse(headers.ToJsonString())!.AsArray();
                    }
                }
            }
            catch
            {
                // Invalid optional datablocks retain the vanilla cache.
            }
        }
    }

    public List<JsonObject> GetRawBlocks(params string[] names)
    {
        foreach (var name in names)
        {
            if (!_rawBlocks.TryGetValue(name, out var blocks))
            {
                _rawBlocks.TryGetValue(NormalizeDatablockFileName(name), out blocks);
            }
            if (blocks != null)
            {
                return blocks.Select(block => JsonNode.Parse(block.ToJsonString())!.AsObject()).ToList();
            }
        }
        return new List<JsonObject>();
    }

    public JsonArray? GetRawHeaders(params string[] names)
    {
        foreach (var name in names)
        {
            if (!_rawHeaders.TryGetValue(name, out var headers))
            {
                _rawHeaders.TryGetValue(NormalizeDatablockFileName(name), out headers);
            }
            if (headers != null) return JsonNode.Parse(headers.ToJsonString())!.AsArray();
        }
        return null;
    }

    public bool TryDeserializeRawBlocks<T>(out T? value, params string[] names) where T : class
    {
        value = null;
        var blocks = GetRawBlocks(names);
        if (blocks.Count == 0) return false;

        try
        {
            var wrapper = new JsonObject
            {
                ["Blocks"] = JsonSerializer.SerializeToNode(blocks)
            };
            value = JsonSerializer.Deserialize<T>(wrapper.ToJsonString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new JsonStringEnumConverter() }
            });
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    private void LoadComplexResourceTemplates()
    {
        string[] candidates =
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Templates", "ComplexResourceSet.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Templates", "ComplexResourceSet.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "Templates", "ComplexResourceSet.json")
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                var root = JsonNode.Parse(File.ReadAllText(candidate))?.AsObject();
                if (root == null) return;
                foreach (var complexType in Enum.GetValues<ComplexType>())
                {
                    var key = root.FirstOrDefault(pair =>
                        string.Equals(pair.Key, complexType.ToString(), StringComparison.OrdinalIgnoreCase)).Value;
                    if (key is JsonObject template)
                    {
                        _complexResourceTemplates[complexType] =
                            JsonNode.Parse(template.ToJsonString())!.AsObject();
                    }
                }
                return;
            }
            catch
            {
                return;
            }
        }
    }

    public JsonObject? GetComplexResourceTemplate(ComplexType complexType)
    {
        return _complexResourceTemplates.TryGetValue(complexType, out var template)
            ? JsonNode.Parse(template.ToJsonString())!.AsObject()
            : null;
    }

    /// <inheritdoc />
    public Task ImportAllAsync(string folderPath)
    {
        return Task.Run(() => ImportAll(folderPath));
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
