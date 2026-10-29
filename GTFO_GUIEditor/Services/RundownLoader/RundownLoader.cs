using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Services;

/// <summary>
/// Dedicated Rundown Loader system that serves as the single point of communication
/// between .rundown files and the rest of the application.
/// Manages minimal, automated saving and loading without per-field mapping boilerplate.
/// </summary>
public class RundownLoader : IRundownLoader
{
    private static readonly Lazy<RundownLoader> _lazyInstance = new(() => new RundownLoader());
    public static RundownLoader Instance => _lazyInstance.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string? CustomRecentFilesConfigPath { get; set; }

    private static string RecentFilesConfigPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CustomRecentFilesConfigPath))
            {
                return CustomRecentFilesConfigPath;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dir = Path.Combine(appData, "GTFO_GUIEditor");
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // Ignore directory creation failure
                }
            }

            return Path.Combine(dir, "recent_files.json");
        }
    }

    /// <inheritdoc />
    public RundownProjectFile ExportProject(MainWindowViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        var project = new RundownProjectFile
        {
            FileVersion = 1,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            LastExportDirectory = vm.LastExportDirectory,
            Rundown = vm.Rundown.SelectedRundown?.Clone() ?? new RundownBlock(),
            Alarms = vm.Alarms.Alarms.Select(a => a.Clone()).ToList(),
            Lighting = vm.Lighting.Blocks.Select(b => b.Clone()).ToList(),
            FogSettings = vm.FogSettings.Blocks.Select(b => b.Clone()).ToList(),
            ConsumableDistributions = vm.Consumables.Blocks.Select(b => b.Clone()).ToList()
        };

        foreach (var level in vm.LevelLayout.Levels)
        {
            var levelData = new LevelLayoutBlockData
            {
                Name = level.Name,
                PersistentId = level.PersistentId,
                ComplexType = level.ComplexType,
                ComplexResourceSet = level.ComplexResourceSet,
                ElevatorTile = level.ElevatorTile,
                FogSettings = level.FogSettings,
                InternalEnabled = level.InternalEnabled,
                Dimensions = level.Dimensions.Select(d => d.Clone()).ToList(),
                MainSector = level.MainSector.Clone(),
                SecondarySector = level.SecondarySector.Clone(),
                OverloadSector = level.OverloadSector.Clone(),
                ExpeditionBalance = level.ExpeditionBalance.Clone(),
                GridState = level.GridState.ExportStateData()
            };
            project.Levels.Add(levelData);
        }

        return project;
    }

    /// <inheritdoc />
    public void SaveProject(MainWindowViewModel vm, string filePath)
    {
        var project = ExportProject(vm);
        SaveProject(project, filePath);
    }

    /// <inheritdoc />
    public void SaveProject(RundownProjectFile project, string filePath)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        project.ModifiedAt = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <inheritdoc />
    public RundownProjectFile LoadProject(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Rundown project file not found: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<RundownProjectFile>(json, JsonOptions);
        if (project == null)
        {
            throw new InvalidOperationException($"Failed to deserialize rundown project file: {filePath}");
        }

        return project;
    }

    /// <inheritdoc />
    public void ApplyProject(MainWindowViewModel vm, RundownProjectFile project, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(vm);
        ArgumentNullException.ThrowIfNull(project);

        vm.CurrentFilePath = filePath;
        vm.LastExportDirectory = project.LastExportDirectory;

        // Apply Rundown
        vm.Rundown.SelectedRundown = project.Rundown?.Clone() ?? Services.RundownService.CreateFreshBlock(1, "New Rundown");

        // Apply Alarms
        vm.Alarms.Alarms.Clear();
        if (project.Alarms != null)
        {
            foreach (var alarm in project.Alarms)
            {
                vm.Alarms.Alarms.Add(alarm.Clone());
            }
        }
        vm.Alarms.SelectedAlarm = vm.Alarms.Alarms.FirstOrDefault();

        // Apply Lighting
        vm.Lighting.Blocks.Clear();
        if (project.Lighting != null)
        {
            foreach (var light in project.Lighting)
            {
                vm.Lighting.Blocks.Add(light.Clone());
            }
        }
        vm.Lighting.SelectedBlock = vm.Lighting.Blocks.FirstOrDefault();

        // Apply Fog
        vm.FogSettings.Blocks.Clear();
        if (project.FogSettings != null)
        {
            foreach (var fog in project.FogSettings)
            {
                vm.FogSettings.Blocks.Add(fog.Clone());
            }
        }
        vm.FogSettings.SelectedBlock = vm.FogSettings.Blocks.FirstOrDefault();

        // Apply Consumables
        vm.Consumables.Blocks.Clear();
        if (project.ConsumableDistributions != null)
        {
            foreach (var consumable in project.ConsumableDistributions)
            {
                vm.Consumables.Blocks.Add(consumable.Clone());
            }
        }
        vm.Consumables.SelectedBlock = vm.Consumables.Blocks.FirstOrDefault();

        // Apply Level Layouts
        vm.LevelLayout.Levels.Clear();
        if (project.Levels != null && project.Levels.Count > 0)
        {
            foreach (var levelData in project.Levels)
            {
                var level = new LevelLayoutBlock
                {
                    Name = levelData.Name,
                    PersistentId = levelData.PersistentId,
                    ComplexType = levelData.ComplexType,
                    ComplexResourceSet = levelData.ComplexResourceSet,
                    ElevatorTile = levelData.ElevatorTile,
                    FogSettings = levelData.FogSettings,
                    InternalEnabled = levelData.InternalEnabled,
                    Dimensions = new ObservableCollection<DimensionLayoutBlock>(
                        levelData.Dimensions?.Select(d => d.Clone()) ?? Enumerable.Empty<DimensionLayoutBlock>()),
                    MainSector = levelData.MainSector?.Clone() ?? new SectorSettings { ZoneAliasStart = 0 },
                    SecondarySector = levelData.SecondarySector?.Clone() ?? new SectorSettings { ZoneAliasStart = 100 },
                    OverloadSector = levelData.OverloadSector?.Clone() ?? new SectorSettings { ZoneAliasStart = 200 },
                    ExpeditionBalance = levelData.ExpeditionBalance?.Clone() ?? new ExpeditionBalanceSettings()
                };

                if (levelData.GridState != null)
                {
                    level.GridState.ImportStateData(levelData.GridState);
                }

                vm.LevelLayout.Levels.Add(level);
            }
        }
        else
        {
            var defaultLevel = EditorDefaultsService.CreateDefaultLevel(1);
            vm.LevelLayout.Levels.Add(defaultLevel);
        }

        vm.LevelLayout.SelectedLevel = vm.LevelLayout.Levels.FirstOrDefault();
    }

    /// <inheritdoc />
    public List<RecentFileItem> LoadRecentFiles()
    {
        try
        {
            string configPath = RecentFilesConfigPath;
            if (!File.Exists(configPath))
            {
                return new List<RecentFileItem>();
            }

            string json = File.ReadAllText(configPath);
            var list = JsonSerializer.Deserialize<List<RecentFileItem>>(json, JsonOptions);
            return list?.Where(item => !string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath)).ToList()
                   ?? new List<RecentFileItem>();
        }
        catch
        {
            return new List<RecentFileItem>();
        }
    }

    /// <inheritdoc />
    public void SaveRecentFiles(List<RecentFileItem> files)
    {
        try
        {
            string configPath = RecentFilesConfigPath;
            string? dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(files, JsonOptions);
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Silently ignore failures to persist recent files history
        }
    }

    /// <inheritdoc />
    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var list = LoadRecentFiles();

            list.RemoveAll(item => string.Equals(item.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

            list.Insert(0, new RecentFileItem
            {
                FilePath = fullPath,
                DisplayName = Path.GetFileName(fullPath),
                LastOpened = DateTime.UtcNow
            });

            if (list.Count > 10)
            {
                list = list.Take(10).ToList();
            }

            SaveRecentFiles(list);
        }
        catch
        {
            // Silently ignore recent file tracking errors
        }
    }
}
