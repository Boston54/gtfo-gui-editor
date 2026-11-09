using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private string? _currentFilePath;
    private string? _lastExportDirectory;
    private string? _currentDatablocksPath;
    private string _statusMessage = string.Empty;
    private ObservableCollection<RecentFileItem> _recentFiles = new();
    private bool _isShowingImportNotification;
    private string _importNotificationTitle = string.Empty;
    private string _importNotificationMessage = string.Empty;
    private ObservableCollection<LevelImportFailure> _importFailedLevels = new();
    private bool _isShowingSettings;
    private int _selectedSettingsTabIndex;
    private bool _isLoadingSetProjectSelection;
    private bool _isDirty;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly System.Threading.Timer _autosaveTimer;
    private DateTime _lastAutosaveUtc = DateTime.UtcNow;
    private readonly HashSet<INotifyPropertyChanged> _dirtyPropertySubscribers = new();
    private readonly HashSet<INotifyCollectionChanged> _dirtyCollectionSubscribers = new();
    private bool _isLoadingProject;
    private long _dirtyRevision;
    private System.Threading.Timer? _statusFadeTimer;
    private long _statusFadeGeneration;
    private double _statusOpacity = 1;

    public DocumentationViewModel Documentation { get; }
    public SoundsViewModel Sounds { get; }
    public GeomorphsViewModel Geomorphs { get; }
    public AlarmsViewModel Alarms { get; }
    public ConsumablesViewModel Consumables { get; }
    public LightingViewModel Lighting { get; }
    public FogSettingsViewModel FogSettings { get; }
    public EnemiesViewModel Enemies { get; }
    public LevelLayoutViewModel LevelLayout { get; }
    public RundownViewModel Rundown { get; }
    public ModdedViewModel Modded { get; }

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

    public bool IsShowingSettings
    {
        get => _isShowingSettings;
        set => SetField(ref _isShowingSettings, value);
    }

    public int SelectedSettingsTabIndex
    {
        get => _selectedSettingsTabIndex;
        set => SetField(ref _selectedSettingsTabIndex, value);
    }

    public EditorSettings Settings => EditorSettingsManager.Instance.Settings;
    public bool IsDatablockDataFlow => Settings.General.DataFlow == DataFlowMode.Datablocks;
    public string SaveMenuHeader => "Save";
    public string SaveAsMenuHeader => "Save As";
    public string OpenMenuHeader => "Open";
    public string ImportMenuHeader => "Import Project";
    public string ExportMenuHeader => "Export";
    public string QuickExportMenuHeader => "Quick Export";
    public string ExportToMenuHeader => "Export To";

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

    public string? CurrentDatablocksPath
    {
        get => _currentDatablocksPath;
        set
        {
            if (SetField(ref _currentDatablocksPath, value))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    LastExportDirectory = value;
                    Settings.General.LastOpenedDatablockPath = value;
                    EditorSettingsManager.Instance.SaveSettings();
                }
            }
        }
    }

    public string CurrentFileTitle => string.IsNullOrWhiteSpace(CurrentFilePath)
        ? "Untitled Rundown"
        : Path.GetFileName(CurrentFilePath);

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            SetField(ref _statusMessage, value);

            _statusFadeTimer?.Dispose();
            _statusFadeTimer = null;
            long generation = unchecked(++_statusFadeGeneration);
            StatusOpacity = 1;

            if (string.IsNullOrWhiteSpace(value) ||
                value.Contains("unsaved changes", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _statusFadeTimer = new System.Threading.Timer(
                _ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (generation != _statusFadeGeneration)
                    {
                        return;
                    }

                    _statusFadeTimer?.Dispose();
                    var fadeStartedAt = DateTime.UtcNow;
                    _statusFadeTimer = new System.Threading.Timer(
                        fadeState => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (generation != _statusFadeGeneration)
                            {
                                return;
                            }

                            double elapsed = (DateTime.UtcNow - fadeStartedAt).TotalMilliseconds;
                            StatusOpacity = Math.Max(0, 1 - elapsed / 2000);
                            if (elapsed >= 2000)
                            {
                                StatusMessage = string.Empty;
                            }
                        }),
                        null,
                        TimeSpan.FromMilliseconds(50),
                        TimeSpan.FromMilliseconds(50));
                }),
                null,
                TimeSpan.FromSeconds(3),
                Timeout.InfiniteTimeSpan);
        }
    }

    public double StatusOpacity
    {
        get => _statusOpacity;
        set => SetField(ref _statusOpacity, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set => SetField(ref _isDirty, value);
    }

    public bool HasUnsavedChanges =>
        Settings.General.DataFlow == DataFlowMode.Datablocks
            ? DatablockSaveQueue.Instance.Count > 0
            : IsDirty;

    public IReadOnlyCollection<string> EditedDatablockFiles => DatablockSaveQueue.Instance.Files;

    private void MarkProjectClean()
    {
        _dirtyRevision = 0;
        IsDirty = false;
        DatablockSaveQueue.Instance.Clear();
    }

    public void MarkProjectEdited()
    {
        if (_isLoadingProject)
        {
            return;
        }
        IsDirty = true;
        unchecked { _dirtyRevision++; }
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            DatablockSaveQueue.Instance.MarkDirty(DatablockSaveQueue.AllDatablockFiles);
            StatusMessage = $"Unsaved changes — {DatablockSaveQueue.Instance.Count} datablock files in save queue";
        }
        else
        {
            StatusMessage = "Unsaved changes — rundown file has changes";
        }
    }

    private void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "SelectedAlarm"
            or "SelectedBlock"
            or "IsExpanded"
            or "SearchText"
            or "HasSelectedAlarm"
            or "HasSelectedBlock"
            or "StatusText"
            or "DisplayText"
            or "SelectedCategory"
            or "CurrentCategorySettings"
            or "HasCurrentCategorySettings"
            or "FilteredAlarms"
            or "FilteredBlocks")
        {
            return;
        }

        if (sender != Settings && sender != Settings.General && sender != Settings.Appearance)
        {
            MarkProjectEdited();
        }
    }

    public MainWindowViewModel()
        : this(new AvaloniaFileDialogService(), autoReopenLastRundown: false)
    {
    }

    public MainWindowViewModel(bool autoReopenLastRundown)
        : this(new AvaloniaFileDialogService(), autoReopenLastRundown)
    {
    }

    public MainWindowViewModel(IFileDialogService? fileDialogService, bool autoReopenLastRundown = false)
    {
        _fileDialogService = fileDialogService ?? new AvaloniaFileDialogService();

        Documentation = new DocumentationViewModel();
        Sounds = new SoundsViewModel();
        Geomorphs = new GeomorphsViewModel();
        Alarms = new AlarmsViewModel();
        Consumables = new ConsumablesViewModel();
        Lighting = new LightingViewModel();
        FogSettings = new FogSettingsViewModel();
        Enemies = new EnemiesViewModel();
        LevelLayout = new LevelLayoutViewModel(null, Lighting.Blocks, Alarms.Alarms, FogSettings.Blocks, Consumables.Blocks);
        var defaultRundown = Services.RundownService.CreateFreshBlock(1, "New Rundown");
        if (defaultRundown.TierA.Count == 0)
        {
            defaultRundown.TierA.Add(EditorDefaultsService.CreateDefaultExpedition("A", 1, LevelLayout.Levels.FirstOrDefault()?.PersistentId ?? 1));
        }
        Rundown = new RundownViewModel(defaultRundown, LevelLayout.Levels);
        Modded = new ModdedViewModel();
        Settings.General.PropertyChanged += OnGeneralSettingsPropertyChanged;

        Documentation.PropertyChanged += OnProjectPropertyChanged;
        Sounds.PropertyChanged += OnProjectPropertyChanged;
        Geomorphs.PropertyChanged += OnProjectPropertyChanged;
        Alarms.PropertyChanged += OnProjectPropertyChanged;
        Consumables.PropertyChanged += OnProjectPropertyChanged;
        Lighting.PropertyChanged += OnProjectPropertyChanged;
        FogSettings.PropertyChanged += OnProjectPropertyChanged;
        Enemies.PropertyChanged += OnProjectPropertyChanged;
        LevelLayout.PropertyChanged += OnProjectPropertyChanged;
        Rundown.PropertyChanged += OnProjectPropertyChanged;
        Modded.PropertyChanged += OnProjectPropertyChanged;

        _autosaveTimer = new System.Threading.Timer(
            _ => _ = RunAutosaveAsync(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));

        RefreshRecentFiles();

        if (autoReopenLastRundown)
        {
            ApplyStartupProjectSetting();
        }

        AttachDirtyTracking(this);
        MarkProjectClean();
        DatablockSaveQueue.Instance.Clear();
    }

    private void AttachDirtyTracking(object? root)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        TraverseDirtyObject(root, visited);
    }

    private void TraverseDirtyObject(object? value, HashSet<object> visited)
    {
        if (value == null || !visited.Add(value))
        {
            return;
        }

        if (value is EditorSettings || value is GeneralSettings)
        {
            return;
        }

        if (value is INotifyPropertyChanged notifyPropertyChanged &&
            _dirtyPropertySubscribers.Add(notifyPropertyChanged))
        {
            if (value is not MainWindowViewModel)
            {
                notifyPropertyChanged.PropertyChanged += DirtyPropertyChanged;
            }
        }

        if (value is INotifyCollectionChanged notifyCollectionChanged &&
            _dirtyCollectionSubscribers.Add(notifyCollectionChanged))
        {
            if (!ReferenceEquals(notifyCollectionChanged, RecentFiles))
            {
                notifyCollectionChanged.CollectionChanged += DirtyCollectionChanged;
            }
        }

        if (value is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                if (item != null && IsProjectObject(item.GetType()))
                {
                    TraverseDirtyObject(item, visited);
                }
            }
        }

        Type type = value.GetType();
        if (!IsProjectObject(type))
        {
            return;
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 ||
                !IsProjectObject(property.PropertyType))
            {
                continue;
            }

            try
            {
                TraverseDirtyObject(property.GetValue(value), visited);
            }
            catch
            {
                // A property getter can be unavailable while the UI is changing state.
            }
        }
    }

    private static bool IsProjectObject(Type type) =>
        type.Namespace?.StartsWith("GTFO_GUIEditor", StringComparison.Ordinal) == true;

    private void DirtyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is EditorSettings || sender is GeneralSettings)
        {
            return;
        }

        if (e.PropertyName is "SelectedAlarm"
            or "SelectedBlock"
            or "IsExpanded"
            or "SearchText"
            or "HasSelectedAlarm"
            or "HasSelectedBlock"
            or "StatusText"
            or "DisplayText"
            or "SelectedCategory"
            or "CurrentCategorySettings"
            or "HasCurrentCategorySettings"
            or "FilteredAlarms"
            or "FilteredBlocks")
        {
            return;
        }

        if (sender is LevelLayoutGridState &&
            (e.PropertyName == nameof(LevelLayoutGridState.Zoom) ||
             e.PropertyName == nameof(LevelLayoutGridState.ZoomPercentage) ||
             e.PropertyName == nameof(LevelLayoutGridState.PanX) ||
             e.PropertyName == nameof(LevelLayoutGridState.PanY) ||
             e.PropertyName == nameof(LevelLayoutGridState.ViewportWidth) ||
             e.PropertyName == nameof(LevelLayoutGridState.ViewportHeight) ||
             e.PropertyName == nameof(LevelLayoutGridState.HoveredRoom)))
        {
            return;
        }

        if (sender != null)
        {
            AttachDirtyTracking(sender);
        }
        MarkProjectEdited();
    }

    private void DirtyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender == Alarms.FilteredAlarms ||
            sender == Consumables.FilteredBlocks ||
            sender == Lighting.FilteredBlocks ||
            sender == FogSettings.FilteredBlocks)
        {
            return;
        }

        MarkProjectEdited();
        if (e.NewItems != null)
        {
            foreach (object? item in e.NewItems)
            {
                AttachDirtyTracking(item);
            }
        }
    }

    private void OnGeneralSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettings.DataFlow) ||
            e.PropertyName == nameof(GeneralSettings.IsRundownDataFlow) ||
            e.PropertyName == nameof(GeneralSettings.IsDatablocksDataFlow))
        {
            OnPropertyChanged(nameof(IsDatablockDataFlow));
            OnPropertyChanged(nameof(SaveMenuHeader));
            OnPropertyChanged(nameof(SaveAsMenuHeader));
            OnPropertyChanged(nameof(OpenMenuHeader));
            OnPropertyChanged(nameof(ImportMenuHeader));
            OnPropertyChanged(nameof(ExportMenuHeader));
            OnPropertyChanged(nameof(QuickExportMenuHeader));
            OnPropertyChanged(nameof(ExportToMenuHeader));
            _lastAutosaveUtc = DateTime.UtcNow;
            if (Settings.General.DataFlow == DataFlowMode.Datablocks)
            {
                if (IsDirty)
                {
                    DatablockSaveQueue.Instance.MarkDirty(DatablockSaveQueue.AllDatablockFiles);
                }
                else
                {
                    DatablockSaveQueue.Instance.Clear();
                }
            }
            else
            {
                DatablockSaveQueue.Instance.Clear();
            }
        }

        if (e.PropertyName == nameof(GeneralSettings.WindowDisplayMode) ||
            e.PropertyName == nameof(GeneralSettings.IsWindowed) ||
            e.PropertyName == nameof(GeneralSettings.IsWindowedFullscreen) ||
            e.PropertyName == nameof(GeneralSettings.IsFullscreen))
        {
            ApplyWindowDisplayMode();
        }

        if (e.PropertyName == nameof(GeneralSettings.StartupProjectPath) &&
            !_isLoadingSetProjectSelection &&
            Settings.General.StartupProject == StartupProjectMode.SetProject &&
            !string.IsNullOrWhiteSpace(Settings.General.StartupProjectPath))
        {
            _isLoadingSetProjectSelection = true;
            TryOpenStartupProject(Settings.General.StartupProjectPath);
            _isLoadingSetProjectSelection = false;
        }
    }
    public void ApplyStartupProjectSetting()
    {
        _isLoadingProject = true;
        try
        {
            switch (Settings.General.StartupProject)
            {
                case StartupProjectMode.NewProject:
                    break;
                case StartupProjectMode.SetProject:
                    if (!string.IsNullOrWhiteSpace(Settings.General.StartupProjectPath))
                    {
                        TryOpenStartupProject(Settings.General.StartupProjectPath);
                    }
                    break;
                case StartupProjectMode.PreviousOpenedProject:
                default:
                    TryReopenLastRundown();
                    break;
            }
        }
        finally
        {
            _isLoadingProject = false;
            MarkProjectClean();
        }
    }

    public void TryOpenStartupProject(string path)
    {
        _ = OpenStartupProjectAsync(path);
    }

    private async Task OpenStartupProjectAsync(string path)
    {
        _isLoadingProject = true;
        try
        {
            if (File.Exists(path))
            {
                await OpenFileDirectAsync(path);
                return;
            }

            if (Directory.Exists(path))
            {
                if (Settings.General.DataFlow == DataFlowMode.Datablocks)
                {
                    try
                    {
                        var result = DatablockManager.Instance.ImportDatablocks(this, path);
                        HandleImportResult(result);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error loading startup folder: {ex.Message}";
                    }
                    return;
                }

                string? rundownFile = Directory.EnumerateFiles(path, "*.rundown", SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(rundownFile))
                {
                    await OpenFileDirectAsync(rundownFile);
                }
                else
                {
                    StatusMessage = $"No .rundown file found in startup folder: {path}";
                }
            }
        }
        finally
        {
            _isLoadingProject = false;
            MarkProjectClean();
        }
    }

    public bool TryReopenLastRundown()
    {
        bool previousLoading = _isLoadingProject;
        _isLoadingProject = true;
        MarkProjectClean();
        try
        {
            if (Settings.General.DataFlow == DataFlowMode.Datablocks)
            {
                string? lastDatablockPath = Settings.General.LastOpenedDatablockPath;
                if (!string.IsNullOrWhiteSpace(lastDatablockPath) && Directory.Exists(lastDatablockPath))
                {
                    try
                    {
                        CurrentDatablocksPath = lastDatablockPath;
                        var result = DatablockManager.Instance.ImportDatablocks(this, lastDatablockPath);
                        HandleImportResult(result);
                        StatusMessage = $"Reopened datablocks: {Path.GetFileName(lastDatablockPath)}";
                        return result.Success;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Failed to reopen last datablocks: {ex.Message}";
                        return false;
                    }
                }
                return false;
            }

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
        finally
        {
            _isLoadingProject = previousLoading;
            if (!previousLoading)
            {
                AttachDirtyTracking(this);
                MarkProjectClean();
            }
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
        CurrentDatablocksPath = null;
        LastExportDirectory = null;

        Alarms.Alarms.Clear();
        Alarms.SelectedAlarm = null;

        Consumables.Blocks.Clear();
        Consumables.SelectedBlock = null;

        Lighting.Blocks.Clear();
        Lighting.SelectedBlock = null;

        FogSettings.Blocks.Clear();
        FogSettings.EnsureElevatorFogConfig();
        FogSettings.SelectedBlock = FogSettings.Blocks.FirstOrDefault();

        LevelLayout.Levels.Clear();
        var defaultLevel = EditorDefaultsService.CreateDefaultLevel(1);
        LevelLayout.Levels.Add(defaultLevel);
        LevelLayout.SelectedLevel = defaultLevel;

        var freshRundown = Services.RundownService.CreateFreshBlock(1, "New Rundown");
        if (freshRundown.TierA.Count == 0)
        {
            freshRundown.TierA.Add(EditorDefaultsService.CreateDefaultExpedition("A", 1, defaultLevel.PersistentId));
        }
        Rundown.SelectedRundown = freshRundown;

        Modded.Reset();

        MarkProjectClean();
        StatusMessage = "Created new rundown.";
    }

    public async Task SaveRundownAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            long revision = _dirtyRevision;
            DataFlowMode dataFlow = Settings.General.DataFlow;
            if (dataFlow == DataFlowMode.Datablocks)
            {
                if (!string.IsNullOrWhiteSpace(CurrentDatablocksPath) && Directory.Exists(CurrentDatablocksPath))
                {
                    await SaveDatablocksToCurrentFolderAsync();
                }
                else
                {
                    await ExportDatablocksFolderAsync();
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                await SaveAsRundownAsync();
                return;
            }

            string filePath = CurrentFilePath;
            var projectSnapshot = await ExportProjectForSaveAsync();
            await Task.Run(() =>
            {
                RundownProjectService.SaveProject(projectSnapshot, filePath);
                RundownProjectService.AddRecentFile(filePath);
            });
            Settings.General.LastOpenedRundownPath = filePath;
            EditorSettingsManager.Instance.SaveSettings();
            RefreshRecentFiles();
            if (_dirtyRevision == revision)
            {
                MarkProjectClean();
            }
            StatusMessage = $"Saved: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private async Task RunAutosaveAsync()
    {
        if (!IsDirty)
        {
            return;
        }

        if (Settings.General.DataFlow == DataFlowMode.RundownFile &&
            string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            return;
        }

        if (Settings.General.DataFlow == DataFlowMode.Datablocks &&
            (string.IsNullOrWhiteSpace(CurrentDatablocksPath) || !Directory.Exists(CurrentDatablocksPath)))
        {
            return;
        }

        int intervalMinutes = Settings.General.AutosaveIntervalMinutes;
        if (intervalMinutes <= 0)
        {
            return;
        }

        if (DateTime.UtcNow - _lastAutosaveUtc < TimeSpan.FromMinutes(intervalMinutes))
        {
            return;
        }

        _lastAutosaveUtc = DateTime.UtcNow;
        await SaveRundownAsync();
    }

    private async Task<RundownProjectFile> ExportProjectForSaveAsync()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return RundownProjectService.ExportProject(this);
        }

        return await Dispatcher.UIThread.InvokeAsync(() => RundownProjectService.ExportProject(this));
    }

    public async Task SaveAsRundownAsync()
    {
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            await ExportDatablocksFolderAsync();
            return;
        }

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
            long revision = _dirtyRevision;
            CurrentFilePath = selectedPath;
            var projectSnapshot = await ExportProjectForSaveAsync();
            await Task.Run(() => RundownProjectService.SaveProject(projectSnapshot, selectedPath));
            RundownProjectService.AddRecentFile(selectedPath);
            Settings.General.LastOpenedRundownPath = selectedPath;
            EditorSettingsManager.Instance.SaveSettings();
            RefreshRecentFiles();
            if (_dirtyRevision == revision)
            {
                MarkProjectClean();
            }
            StatusMessage = $"Saved: {Path.GetFileName(selectedPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    public async Task OpenRundownAsync()
    {
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            await ImportDatablocksFolderAsync();
            return;
        }

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
        bool previousLoading = _isLoadingProject;
        _isLoadingProject = true;
        try
        {
            var project = RundownProjectService.LoadProject(filePath);
            RundownProjectService.ApplyProject(this, project, filePath);
            Settings.General.LastOpenedRundownPath = filePath;
            EditorSettingsManager.Instance.SaveSettings();
            RefreshRecentFiles();
            AttachDirtyTracking(this);
            MarkProjectClean();
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
        finally
        {
            _isLoadingProject = previousLoading;
            if (!previousLoading)
            {
                AttachDirtyTracking(this);
                MarkProjectClean();
            }
        }
        return Task.CompletedTask;
    }

    public async Task QuickExportDatablocksAsync()
    {
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            await SaveAsRundownFileAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(LastExportDirectory))
        {
            await ExportDatablocksToAsync();
            return;
        }

        try
        {
            var result = await Task.Run(() => DatablockExportService.ExportAll(this, LastExportDirectory));
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
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            await SaveAsRundownFileAsync();
            return;
        }

        await ExportDatablocksFolderAsync();
    }

    private async Task ExportDatablocksFolderAsync()
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

        CurrentDatablocksPath = selectedFolder;
        try
        {
            LastExportDirectory = selectedFolder;
            DatablockSaveQueue.Instance.MarkDirty(DatablockSaveQueue.AllDatablockFiles);
            var result = await Task.Run(() => DatablockExportService.ExportAll(this, selectedFolder));
            if (result.Success)
            {
                DatablockSaveQueue.Instance.Clear();
                if (Settings.General.DataFlow == DataFlowMode.Datablocks)
                {
                    MarkProjectClean();
                }
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

    private async Task SaveDatablocksToCurrentFolderAsync()
    {
        try
        {
            var snapshot = DatablockSaveQueue.Instance.TakeRevisionSnapshot();
            var result = await Task.Run(() => DatablockExportService.ExportAll(this, CurrentDatablocksPath!));
            if (result.Success)
            {
                foreach (var item in snapshot)
                {
                    DatablockSaveQueue.Instance.MarkSaved(item.Key, item.Value);
                }
                if (DatablockSaveQueue.Instance.Count == 0)
                {
                    if (Settings.General.DataFlow == DataFlowMode.Datablocks)
                    {
                        IsDirty = false;
                        _dirtyRevision = 0;
                    }
                    StatusMessage = $"Saved datablocks to: {Path.GetFileName(CurrentDatablocksPath)}";
                }
                else if (Settings.General.DataFlow == DataFlowMode.Datablocks)
                {
                    StatusMessage = $"Unsaved changes — {DatablockSaveQueue.Instance.Count} datablock files remain in save queue";
                }
            }
            else
            {
                StatusMessage = $"Save error: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }

    public async Task ImportDatablocksFromAsync()
    {
        if (Settings.General.DataFlow == DataFlowMode.Datablocks)
        {
            string? rundownPath = await _fileDialogService.ShowOpenFileDialogAsync(
                defaultDirectory: _fileDialogService.DefaultRundownsDirectory,
                filter: "rundown");
            if (!string.IsNullOrWhiteSpace(rundownPath))
            {
                await OpenFileDirectAsync(rundownPath);
            }
            return;
        }

        await ImportDatablocksFolderAsync();
    }

    private async Task ImportDatablocksFolderAsync()
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

        bool previousLoading = _isLoadingProject;
        _isLoadingProject = true;
        try
        {
            CurrentDatablocksPath = selectedFolder;
            var result = DatablockManager.Instance.ImportDatablocks(this, selectedFolder);
            HandleImportResult(result);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
        finally
        {
            _isLoadingProject = previousLoading;
            MarkProjectClean();
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
            AttachDirtyTracking(this);
            MarkProjectClean();
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

    private async Task SaveAsRundownFileAsync()
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
            var projectSnapshot = await ExportProjectForSaveAsync();
            await Task.Run(() => RundownProjectService.SaveProject(projectSnapshot, selectedPath));
            RundownProjectService.AddRecentFile(selectedPath);
            Settings.General.LastOpenedRundownPath = selectedPath;
            EditorSettingsManager.Instance.SaveSettings();
            RefreshRecentFiles();
            MarkProjectClean();
            StatusMessage = $"Saved rundown: {Path.GetFileName(selectedPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving rundown: {ex.Message}";
        }
    }

    public async Task ChooseStartupProjectFileAsync()
    {
        string? path = await _fileDialogService.ShowOpenFileDialogAsync(
            defaultDirectory: Settings.General.StartupProjectPath ?? _fileDialogService.DefaultRundownsDirectory,
            filter: "rundown");
        if (!string.IsNullOrWhiteSpace(path))
        {
            _isLoadingSetProjectSelection = true;
            Settings.General.StartupProject = StartupProjectMode.SetProject;
            Settings.General.StartupProjectPath = path;
            _isLoadingSetProjectSelection = false;
            EditorSettingsManager.Instance.SaveSettings();
            TryOpenStartupProject(path);
        }
    }

    public async Task ChooseStartupProjectFolderAsync()
    {
        string? path = await _fileDialogService.ShowOpenFolderDialogAsync(
            defaultDirectory: Settings.General.StartupProjectPath,
            title: "Select Startup Project Folder");
        if (!string.IsNullOrWhiteSpace(path))
        {
            _isLoadingSetProjectSelection = true;
            Settings.General.StartupProject = StartupProjectMode.SetProject;
            Settings.General.StartupProjectPath = path;
            _isLoadingSetProjectSelection = false;
            EditorSettingsManager.Instance.SaveSettings();
            TryOpenStartupProject(path);
        }
    }

    public void ChooseStartupProjectFile() => _ = ChooseStartupProjectFileAsync();
    public void ChooseStartupProjectFolder() => _ = ChooseStartupProjectFolderAsync();
    public void ResetGeneralSettingsToDefaults() => Settings.General.ResetToDefaults();

    public void OpenGeneralSettings()
    {
        SelectedSettingsTabIndex = 0;
        IsShowingSettings = true;
    }

    public void OpenAppearanceSettings()
    {
        SelectedSettingsTabIndex = 1;
        IsShowingSettings = true;
    }

    public void OpenSettings() => OpenGeneralSettings();
    public void QuitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    public void ApplyWindowDisplayMode()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Window window)
        {
            return;
        }

        ApplyWindowDisplayMode(window);
    }

    public void ApplyWindowDisplayMode(Window window)
    {
        switch (Settings.General.WindowDisplayMode)
        {
            case WindowDisplayMode.WindowedFullscreen:
                window.SystemDecorations = SystemDecorations.None;
                window.WindowState = WindowState.Maximized;
                break;
            case WindowDisplayMode.Fullscreen:
                window.SystemDecorations = SystemDecorations.None;
                window.WindowState = WindowState.FullScreen;
                break;
            case WindowDisplayMode.Windowed:
            default:
                window.WindowState = WindowState.Normal;
                window.SystemDecorations = SystemDecorations.Full;
                break;
        }
    }

    public void CloseSettings()
    {
        var general = Settings.General;
        if (general.StartupProject == StartupProjectMode.SetProject &&
            string.IsNullOrWhiteSpace(general.StartupProjectPath))
        {
            general.StartupProject = StartupProjectMode.PreviousOpenedProject;
        }

        EditorSettingsManager.Instance.SaveSettings();
        IsShowingSettings = false;
    }

    public void ResetSettingsToDefaults()
    {
        if (SelectedSettingsTabIndex == 0)
        {
            ResetGeneralSettingsToDefaults();
        }
        else
        {
            ResetAppearanceSettingsToDefaults();
        }
    }

    public void ResetAppearanceSettingsToDefaults()
    {
        Settings.Appearance.ResetToDefaults();
    }

    public void ResetUnexpandedDoorColor() => Settings.Appearance.ResetUnexpandedDoorColor();
    public void ResetConnectedDoorColor() => Settings.Appearance.ResetConnectedDoorColor();
    public void ResetSecurityDoorColor() => Settings.Appearance.ResetSecurityDoorColor();
    public void ResetMainDoorColor() => Settings.Appearance.ResetMainDoorColor();
    public void ResetSecondaryDoorColor() => Settings.Appearance.ResetSecondaryDoorColor();
    public void ResetOverloadDoorColor() => Settings.Appearance.ResetOverloadDoorColor();
    public void ResetBlockedDoorColor() => Settings.Appearance.ResetBlockedDoorColor();
    public void ResetHeightmapGradient() => Settings.Appearance.ResetHeightmapGradient();

    public void AddHeightmapGradientKeyframe()
    {
        Settings.Appearance.AddKeyframe();
    }

    public void RemoveHeightmapGradientKeyframe(GradientKeyframe? keyframe)
    {
        Settings.Appearance.RemoveKeyframe(keyframe);
    }

    public void RemoveSelectedHeightmapGradientKeyframe()
    {
        Settings.Appearance.RemoveSelectedKeyframe();
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
