using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private string? _currentFilePath;
    private string _statusMessage = string.Empty;
    private ObservableCollection<RecentFileItem> _recentFiles = new();

    public SoundsViewModel Sounds { get; }
    public GeomorphsViewModel Geomorphs { get; }
    public ComplexResourceSetsViewModel ComplexResourceSets { get; }
    public AlarmsViewModel Alarms { get; }
    public LightingViewModel Lighting { get; }
    public FogSettingsViewModel FogSettings { get; }
    public LevelLayoutViewModel LevelLayout { get; }
    public RundownViewModel Rundown { get; }

    public ObservableCollection<RecentFileItem> RecentFiles
    {
        get => _recentFiles;
        private set => SetField(ref _recentFiles, value);
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (SetField(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(CurrentFileTitle));
                OnPropertyChanged(nameof(HasCurrentFilePath));
            }
        }
    }

    public bool HasCurrentFilePath => !string.IsNullOrWhiteSpace(CurrentFilePath);

    public string CurrentFileTitle => string.IsNullOrWhiteSpace(CurrentFilePath)
        ? "Untitled Rundown"
        : Path.GetFileName(CurrentFilePath);

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public MainWindowViewModel()
        : this(new AvaloniaFileDialogService())
    {
    }

    public MainWindowViewModel(IFileDialogService? fileDialogService)
    {
        _fileDialogService = fileDialogService ?? new AvaloniaFileDialogService();

        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        ComplexResourceSets = new ComplexResourceSetsViewModel();
        Alarms = new AlarmsViewModel();
        Lighting = new LightingViewModel();
        FogSettings = new FogSettingsViewModel();
        LevelLayout = new LevelLayoutViewModel(ComplexResourceSets.Blocks, null, Lighting.Blocks, Alarms.Alarms, FogSettings.Blocks);
        Rundown = new RundownViewModel(Services.RundownService.LoadVanillaBlocks(), LevelLayout.Levels, ComplexResourceSets.Blocks);

        RefreshRecentFiles();
    }

    public void RefreshRecentFiles()
    {
        var list = RundownProjectService.LoadRecentFiles();
        if (list.Count == 0)
        {
            RecentFiles = new ObservableCollection<RecentFileItem>
            {
                new() { DisplayName = "(No recent files)", IsPlaceholder = true }
            };
        }
        else
        {
            RecentFiles = new ObservableCollection<RecentFileItem>(list);
        }
    }

    public void NewRundown()
    {
        CurrentFilePath = null;

        Alarms.Alarms.Clear();
        foreach (var alarm in Services.AlarmService.LoadVanillaAlarms())
        {
            Alarms.Alarms.Add(alarm.Clone());
        }
        if (Alarms.Alarms.Count > 0) Alarms.SelectedAlarm = Alarms.Alarms[0];

        Lighting.Blocks.Clear();
        foreach (var light in Services.LightSettingsService.LoadVanillaBlocks())
        {
            Lighting.Blocks.Add(light.Clone());
        }
        if (Lighting.Blocks.Count > 0) Lighting.SelectedBlock = Lighting.Blocks[0];

        FogSettings.Blocks.Clear();
        foreach (var fog in Services.FogSettingsService.LoadVanillaBlocks())
        {
            FogSettings.Blocks.Add(fog.Clone());
        }
        if (FogSettings.Blocks.Count > 0) FogSettings.SelectedBlock = FogSettings.Blocks[0];

        ComplexResourceSets.Blocks.Clear();
        foreach (var set in Services.ComplexResourceSetService.LoadVanillaBlocks())
        {
            ComplexResourceSets.Blocks.Add(set.Clone());
        }
        if (ComplexResourceSets.Blocks.Count > 0) ComplexResourceSets.SelectedBlock = ComplexResourceSets.Blocks[0];

        LevelLayout.Levels.Clear();
        var defaultLevel = new LevelLayoutBlock
        {
            Name = "Level A1",
            PersistentId = 1,
            ComplexResourceSet = ComplexResourceSets.Blocks.FirstOrDefault()?.PersistentId ?? 1,
            ElevatorTile = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab",
            InternalEnabled = true
        };
        LevelLayout.Levels.Add(defaultLevel);
        LevelLayout.SelectedLevel = defaultLevel;

        var freshRundown = Services.RundownService.LoadVanillaBlocks().FirstOrDefault()?.Clone()
            ?? Services.RundownService.CreateFreshBlock(1, "New Rundown");
        Rundown.SelectedRundown = freshRundown;

        StatusMessage = "Created new rundown.";
    }

    public async Task SaveRundownAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            await SaveAsRundownAsync();
            return;
        }

        try
        {
            RundownProjectService.SaveProject(this, CurrentFilePath);
            RundownProjectService.AddRecentFile(CurrentFilePath);
            RefreshRecentFiles();
            StatusMessage = $"Saved: {Path.GetFileName(CurrentFilePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    public async Task SaveAsRundownAsync()
    {
        string defaultName = !string.IsNullOrWhiteSpace(Rundown.SelectedRundown?.Name)
            ? $"{Rundown.SelectedRundown.Name}.rundown"
            : "MyRundown.rundown";

        string? selectedPath = await _fileDialogService.ShowSaveFileDialogAsync(
            defaultFileName: defaultName,
            defaultDirectory: _fileDialogService.DefaultRundownsDirectory,
            defaultExtension: "rundown");

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        try
        {
            CurrentFilePath = selectedPath;
            RundownProjectService.SaveProject(this, selectedPath);
            RundownProjectService.AddRecentFile(selectedPath);
            RefreshRecentFiles();
            StatusMessage = $"Saved: {Path.GetFileName(selectedPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    public async Task OpenRundownAsync()
    {
        string? selectedPath = await _fileDialogService.ShowOpenFileDialogAsync(
            defaultDirectory: _fileDialogService.DefaultRundownsDirectory,
            filter: "rundown");

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        await OpenFileDirectAsync(selectedPath);
    }

    public async Task OpenRecentFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (!File.Exists(filePath))
        {
            StatusMessage = $"File not found: {filePath}";
            RefreshRecentFiles();
            return;
        }

        await OpenFileDirectAsync(filePath);
    }

    public Task OpenFileDirectAsync(string filePath)
    {
        try
        {
            var project = RundownProjectService.LoadProject(filePath);
            RundownProjectService.ApplyProject(this, project, filePath);
            RefreshRecentFiles();
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    public void SaveRundown() => _ = SaveRundownAsync();
    public void SaveAsRundown() => _ = SaveAsRundownAsync();
    public void OpenRundown() => _ = OpenRundownAsync();
    public void OpenRecentFile(string? filePath) => _ = OpenRecentFileAsync(filePath);
}
