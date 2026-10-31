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
    private string? _lastExportDirectory;
    private string _statusMessage = string.Empty;
    private ObservableCollection<RecentFileItem> _recentFiles = new();
    private bool _isShowingImportNotification;
    private string _importNotificationTitle = string.Empty;
    private string _importNotificationMessage = string.Empty;
    private ObservableCollection<LevelImportFailure> _importFailedLevels = new();

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

    public bool IsShowingImportNotification
    {
        get => _isShowingImportNotification;
        set => SetField(ref _isShowingImportNotification, value);
    }

    public string ImportNotificationTitle
    {
        get => _importNotificationTitle;
        set => SetField(ref _importNotificationTitle, value);
    }

    public string ImportNotificationMessage
    {
        get => _importNotificationMessage;
        set => SetField(ref _importNotificationMessage, value);
    }

    public ObservableCollection<LevelImportFailure> ImportFailedLevels
    {
        get => _importFailedLevels;
        set => SetField(ref _importFailedLevels, value);
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

    public string? LastExportDirectory
    {
        get => _lastExportDirectory;
        set => SetField(ref _lastExportDirectory, value);
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
        : this(new AvaloniaFileDialogService(), autoReopenLastRundown: true)
    {
    }

    public MainWindowViewModel(bool autoReopenLastRundown)
        : this(new AvaloniaFileDialogService(), autoReopenLastRundown)
    {
    }

    public MainWindowViewModel(IFileDialogService? fileDialogService, bool autoReopenLastRundown = true)
    {
        _fileDialogService = fileDialogService ?? new AvaloniaFileDialogService();

        Documentation = new DocumentationViewModel();
        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        Alarms = new AlarmsViewModel();
        Consumables = new ConsumablesViewModel();
        Lighting = new LightingViewModel();
        FogSettings = new FogSettingsViewModel();
        LevelLayout = new LevelLayoutViewModel(null, Lighting.Blocks, Alarms.Alarms, FogSettings.Blocks, Consumables.Blocks);
        Rundown = new RundownViewModel(Services.RundownService.CreateFreshBlock(1, "New Rundown"), LevelLayout.Levels);

        RefreshRecentFiles();

        if (autoReopenLastRundown)
        {
            TryReopenLastRundown();
        }
    }

    public bool TryReopenLastRundown()
    {
        var recentFiles = RundownProjectService.LoadRecentFiles();
        var lastFile = recentFiles.FirstOrDefault(f => !f.IsPlaceholder && !string.IsNullOrWhiteSpace(f.FilePath) && File.Exists(f.FilePath));
        if (lastFile == null)
        {
            return false;
        }

        try
        {
            var project = RundownProjectService.LoadProject(lastFile.FilePath);
            RundownProjectService.ApplyProject(this, project, lastFile.FilePath);
            RefreshRecentFiles();
            StatusMessage = $"Reopened: {Path.GetFileName(lastFile.FilePath)}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to reopen last rundown: {ex.Message}";
            return false;
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
        LastExportDirectory = null;

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

    public async Task QuickExportDatablocksAsync()
    {
        if (string.IsNullOrWhiteSpace(LastExportDirectory))
        {
            await ExportDatablocksToAsync();
            return;
        }

        try
        {
            var result = DatablockExportService.ExportAll(this, LastExportDirectory);
            if (result.Success)
            {
                StatusMessage = $"Exported {result.DatablocksExportedCount} datablocks & {result.LevelGenFilesExportedCount} level schemas to: {Path.GetFileName(LastExportDirectory)}";
            }
            else
            {
                StatusMessage = $"Export error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    public async Task ExportDatablocksToAsync()
    {
        string initialDir = !string.IsNullOrWhiteSpace(LastExportDirectory) && Directory.Exists(LastExportDirectory)
            ? LastExportDirectory
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
            LastExportDirectory = selectedFolder;
            var result = DatablockExportService.ExportAll(this, selectedFolder);
            if (result.Success)
            {
                StatusMessage = $"Exported {result.DatablocksExportedCount} datablocks & {result.LevelGenFilesExportedCount} level schemas to: {Path.GetFileName(selectedFolder)}";
            }
            else
            {
                StatusMessage = $"Export error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    public async Task ImportDatablocksFromAsync()
    {
        string initialDir = !string.IsNullOrWhiteSpace(LastExportDirectory) && Directory.Exists(LastExportDirectory)
            ? LastExportDirectory
            : _fileDialogService.DefaultRundownsDirectory;

        string? selectedFolder = await _fileDialogService.ShowOpenFolderDialogAsync(
            defaultDirectory: initialDir,
            title: "Select Directory to Import Datablocks From");

        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        try
        {
            var result = DatablockManager.Instance.ImportDatablocks(this, selectedFolder);
            HandleImportResult(result);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
    }

    public void HandleImportResult(ImportResult result)
    {
        if (result.FailedLevels.Count > 0)
        {
            ImportFailedLevels.Clear();
            foreach (var failure in result.FailedLevels)
            {
                ImportFailedLevels.Add(failure);
            }

            ImportNotificationTitle = "Level Import Failure";
            ImportNotificationMessage = $"{result.FailedLevels.Count} level(s) failed to load due to missing LGTunerAdvanced configuration file(s).";
            IsShowingImportNotification = true;
        }

        if (result.Success)
        {
            StatusMessage = result.Message;
        }
        else
        {
            StatusMessage = $"Import error: {result.Message}";
        }
    }

    public void CloseImportNotification()
    {
        IsShowingImportNotification = false;
    }

    public ImportResult ImportDatablocks(string inputDirectory)
    {
        var result = DatablockManager.Instance.ImportDatablocks(this, inputDirectory);
        HandleImportResult(result);
        return result;
    }

    public async Task<ImportResult> ImportDatablocksAsync(string inputDirectory)
    {
        var result = await DatablockManager.Instance.ImportDatablocksAsync(this, inputDirectory);
        HandleImportResult(result);
        return result;
    }

    public void SaveRundown() => _ = SaveRundownAsync();
    public void SaveAsRundown() => _ = SaveAsRundownAsync();
    public void OpenRundown() => _ = OpenRundownAsync();
    public void OpenRecentFile(string? filePath) => _ = OpenRecentFileAsync(filePath);
    public void QuickExportDatablocks() => _ = QuickExportDatablocksAsync();
    public void ExportDatablocksTo() => _ = ExportDatablocksToAsync();
    public void ExportDatablocks() => _ = QuickExportDatablocksAsync();
    public void ImportDatablocksFrom() => _ = ImportDatablocksFromAsync();
    public void ImportProject() => _ = ImportDatablocksFromAsync();
}
