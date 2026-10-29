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
    private string? _lastOpenFolderDirectory;
    private string? _lastSaveFolderDirectory;
    private string _statusMessage = string.Empty;
    private ObservableCollection<RecentFileItem> _recentFiles = new();

    public DocumentationViewModel Documentation { get; }
    public SoundsViewModel Sounds { get; }
    public GeomorphsViewModel Geomorphs { get; }
    public AlarmsViewModel Alarms { get; }
    public ConsumablesViewModel Consumables { get; }
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

    public string? LastSaveFolderDirectory
    {
        get => _lastSaveFolderDirectory;
        set => SetField(ref _lastSaveFolderDirectory, value);
    }

    public string? LastOpenFolderDirectory
    {
        get => _lastOpenFolderDirectory;
        set => SetField(ref _lastOpenFolderDirectory, value);
    }

    public string CurrentFileTitle => string.IsNullOrWhiteSpace(CurrentFilePath)
        ? "Untitled Rundown"
        : Path.GetFileName(CurrentFilePath);

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public MainWindowViewModel()
        : this(new AvaloniaFileDialogService(), autoOpenLastFolder: true)
    {
    }

    public MainWindowViewModel(bool autoOpenLastFolder)
        : this(new AvaloniaFileDialogService(), autoOpenLastFolder)
    {
    }

    public MainWindowViewModel(IFileDialogService? fileDialogService, bool autoOpenLastFolder = true)
    {
        _fileDialogService = fileDialogService ?? new AvaloniaFileDialogService();

        try
        {
            DatablockManager.Instance.LoadAll(DatablockManager.Instance.GetVanillaDatablocksDirectory());
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load datablocks: {ex.Message}";
        }

        Documentation = new DocumentationViewModel();
        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        Alarms = new AlarmsViewModel();
        Consumables = new ConsumablesViewModel();
        Lighting = new LightingViewModel();
        FogSettings = new FogSettingsViewModel();
        LevelLayout = new LevelLayoutViewModel(
            null,
            DatablockManager.Instance.Get<LevelLayoutBlock>(),
            Lighting.Blocks,
            Alarms.Alarms,
            FogSettings.Blocks,
            Consumables.Blocks);
        Rundown = new RundownViewModel(Services.RundownService.CreateFreshBlock(1, "New Rundown"), LevelLayout.Levels);

        RefreshRecentFiles();
        LastOpenFolderDirectory = LoadLastOpenFolder();
        LastSaveFolderDirectory = LastOpenFolderDirectory;
        if (autoOpenLastFolder && !string.IsNullOrWhiteSpace(LastOpenFolderDirectory))
        {
            TryOpenLastFolder();
        }
    }

    public bool TryOpenLastFolder()
    {
        if (string.IsNullOrWhiteSpace(LastOpenFolderDirectory) || !Directory.Exists(LastOpenFolderDirectory))
        {
            return false;
        }

        try
        {
            DatablockManager.Instance.ImportAll(LastOpenFolderDirectory);
            RefreshSelectionsAfterImport();
            StatusMessage = $"Opened folder: {Path.GetFileName(LastOpenFolderDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open last folder: {ex.Message}";
            return false;
        }
    }

    private static string LastOpenFolderConfigPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(appData, "GTFO_GUIEditor", "last-open-folder.txt");
        }
    }

    private static string? LoadLastOpenFolder()
    {
        try
        {
            string configPath = LastOpenFolderConfigPath;
            if (!File.Exists(configPath))
            {
                return null;
            }

            string folder = File.ReadAllText(configPath).Trim();
            return Directory.Exists(folder) ? folder : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLastOpenFolder(string folderPath)
    {
        try
        {
            string configPath = LastOpenFolderConfigPath;
            string? directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(configPath, folderPath);
        }
        catch
        {
            // Ignore failures to persist the last opened folder.
        }
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
        LastSaveFolderDirectory = null;

        Alarms.Alarms.Clear();
        Alarms.SelectedAlarm = null;

        Consumables.Blocks.Clear();
        Consumables.SelectedBlock = null;

        Lighting.Blocks.Clear();
        Lighting.SelectedBlock = null;

        FogSettings.Blocks.Clear();
        FogSettings.SelectedBlock = null;

        LevelLayout.Levels.Clear();
        var defaultLevel = EditorDefaultsService.CreateDefaultLevel(1);
        LevelLayout.Levels.Add(defaultLevel);
        LevelLayout.SelectedLevel = defaultLevel;

        var freshRundown = Services.RundownService.CreateFreshBlock(1, "New Rundown");
        Rundown.SelectedRundown = freshRundown;

        StatusMessage = "Created new rundown.";
    }

    public async Task ExportRundownAsync()
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

    public async Task ImportRundownAsync()
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
            RundownProjectService.AddRecentFile(filePath);
            RefreshRecentFiles();
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    public async Task SaveFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(LastSaveFolderDirectory) || !Directory.Exists(LastSaveFolderDirectory))
        {
            await SaveFolderToAsync();
            return;
        }

        try
        {
            var result = DatablockExportService.SaveAll(this, LastSaveFolderDirectory);
            if (result.Success)
            {
                StatusMessage = $"Saved folder: {result.DatablocksExportedCount} datablocks & {result.LevelGenFilesExportedCount} level schemas to: {Path.GetFileName(LastSaveFolderDirectory)}";
            }
            else
            {
                StatusMessage = $"Save folder error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save folder error: {ex.Message}";
        }
    }

    public async Task SaveFolderToAsync()
    {
        string initialDir = !string.IsNullOrWhiteSpace(LastSaveFolderDirectory) && Directory.Exists(LastSaveFolderDirectory)
            ? LastSaveFolderDirectory
            : _fileDialogService.DefaultRundownsDirectory;

        string? selectedFolder = await _fileDialogService.ShowOpenFolderDialogAsync(
            defaultDirectory: initialDir,
            title: "Select Mod / Export Destination Directory");

        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        try
        {
            LastSaveFolderDirectory = selectedFolder;
            var result = DatablockExportService.SaveAll(this, selectedFolder);
            if (result.Success)
            {
                StatusMessage = $"Exported {result.DatablocksExportedCount} datablocks & {result.LevelGenFilesExportedCount} level schemas to: {Path.GetFileName(selectedFolder)}";
            }
            else
            {
                StatusMessage = $"Save folder error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save folder error: {ex.Message}";
        }
    }

    public async Task OpenFolderAsync()
    {
            string initialDir = !string.IsNullOrWhiteSpace(LastOpenFolderDirectory) && Directory.Exists(LastOpenFolderDirectory)
                ? LastOpenFolderDirectory
                : DatablockManager.Instance.GetVanillaDatablocksDirectory();

            string? selectedFolder = await _fileDialogService.ShowOpenFolderDialogAsync(
                defaultDirectory: initialDir,
                title: "Select Datablock Import Folder");

            if (string.IsNullOrWhiteSpace(selectedFolder))
            {
                return;
            }

            try
            {
                LastOpenFolderDirectory = selectedFolder;
                LastSaveFolderDirectory = selectedFolder;
                SaveLastOpenFolder(selectedFolder);
                DatablockManager.Instance.ImportAll(selectedFolder);
                RefreshSelectionsAfterImport();
                StatusMessage = $"Opened folder: {Path.GetFileName(selectedFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Open folder error: {ex.Message}";
            }
    }

    private void RefreshSelectionsAfterImport()
    {
            Alarms.SelectedAlarm = Alarms.Alarms.FirstOrDefault();
            Consumables.SelectedBlock = Consumables.Blocks.FirstOrDefault();
            Lighting.SelectedBlock = Lighting.Blocks.FirstOrDefault();
            FogSettings.SelectedBlock = FogSettings.Blocks.FirstOrDefault();
            LevelLayout.SelectedLevel = LevelLayout.Levels.FirstOrDefault();
    }

    public void OpenRecentFile(string? filePath) => _ = OpenRecentFileAsync(filePath);
    public void OpenFolder() => _ = OpenFolderAsync();
    public void SaveFolder() => _ = SaveFolderAsync();
    public void SaveFolderTo() => _ = SaveFolderToAsync();
    public void ImportRundown() => _ = ImportRundownAsync();
    public void ExportRundown() => _ = ExportRundownAsync();
}
