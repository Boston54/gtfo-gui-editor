using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Media;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;

namespace GTFO_GUIEditor.ViewModels;

public class LevelLayoutViewModel : ViewModelBase
{
    private ObservableCollection<LevelLayoutBlock> _levels;
    private LevelLayoutBlock? _selectedLevel;
    private string _statusText = string.Empty;

    // Grid Canvas State
    private LevelLayoutGridState _gridState = new();

    // ComplexResourceSets list
    private ObservableCollection<ComplexResourceSetBlock> _availableComplexResourceSets;

    // Dimension selection state
    private ObservableCollection<DimensionOption> _availableDimensions = new();
    private DimensionOption? _selectedDimension;
    private string _activeDimensionIndex = "Reality";

    // Dimension prompt/configuration modal state
    private bool _isConfiguringDimension;
    private DimensionOption? _promptingDimensionOption;
    private DimensionOption? _previousDimensionOption;
    private string _dimensionCustomName = string.Empty;
    private ComplexType _dimensionSelectedComplexType = ComplexType.Mining;
    private ComplexResourceSetBlock? _dimensionSelectedComplexResourceSet;

    private ObservableCollection<string> _availableTileSources =
        new() { "Elevator Shafts", "Dimension Geomorphs (Custom Objective 1x1)" };

    private string _selectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
    private ObservableCollection<ElevatorTileOption> _availableDimensionTiles = new();
    private ElevatorTileOption? _selectedDimensionTile;
    private string _dimensionPromptErrorMessage = string.Empty;

    // Create Level modal state
    private bool _isCreatingLevel;
    private string _newLevelName = string.Empty;
    private uint _newLevelPersistentId;
    private ComplexType _selectedComplexType = ComplexType.Mining;
    private ComplexResourceSetBlock? _selectedComplexResourceSet;
    private ObservableCollection<ElevatorTileOption> _availableElevatorTiles = new();
    private ElevatorTileOption? _selectedElevatorTile;
    private string _creationErrorMessage = string.Empty;

    // Delete Level modal state
    private bool _isConfirmingDelete;
    private LevelLayoutBlock? _levelToDelete;

    // Delete Dimension modal state
    private bool _isConfirmingDeleteDimension;
    private string? _dimensionIndexToDelete;

    // Door Expansion / Geomorph Picker modal state
    private bool _isPickingGeomorphForDoor;
    private PlacedDoorNode? _selectedDoorForExpansion;
    private string _expansionDirectionText = string.Empty;
    private string _expansionTargetCellText = string.Empty;
    private string _expansionSourceGeoText = string.Empty;
    private string _geomorphFilterText = string.Empty;
    private ObservableCollection<GeomorphOption> _availableExpansionGeomorphs = new();
    private GeomorphOption? _selectedExpansionGeomorph;
    private ObservableCollection<RotationOption> _availableExpansionRotations = new();
    private RotationOption? _selectedExpansionRotation;
    private Geometry? _selectedExpansionGeomorphPreviewGeometry;
    private ObservableCollection<AltitudeOption> _availableExpansionAltitudes = new();
    private AltitudeOption? _selectedExpansionAltitude;
    private LightSettingsBlock? _selectedExpansionLighting;
    private DoorType _selectedExpansionDoorType = DoorType.RoomDoor;
    private string _expansionErrorMessage = string.Empty;

    // Room Expansion modal state
    private bool _isExpandingRoom;
    private PlacedDoorNode? _selectedRoomDoorForExpansion;
    private string _roomExpansionGeomorphName = string.Empty;
    private int _roomExpansionFromArea;
    private int _roomExpansionTargetArea;
    private DoorType _selectedRoomExpansionDoorType = DoorType.RoomDoor;
    private string _roomExpansionErrorMessage = string.Empty;

    // Edit Room modal state
    private bool _isEditingRoom;
    private PlacedRoomNode? _editingRoom;

    private ObservableCollection<LightSettingsBlock> _availableLightingConfigs = new();
    private ObservableCollection<AlarmBlock> _availableAlarms = new();
    private ObservableCollection<FogSettingsBlock> _availableFogSettings = new();
    private ObservableCollection<FogTransitionOption> _availableFogTransitionOptions = new();

    public LevelLayoutGridState GridState => _selectedLevel?.GridState ?? _gridState;

    public ObservableCollection<LevelLayoutBlock> Levels
    {
        get => _levels;
        private set => SetField(ref _levels, value);
    }

    public ObservableCollection<ComplexResourceSetBlock> AvailableComplexResourceSets
    {
        get => _availableComplexResourceSets;
        private set => SetField(ref _availableComplexResourceSets, value);
    }

    public ObservableCollection<ComplexType> AvailableComplexTypes { get; } = new(new[] { ComplexType.Mining, ComplexType.Tech, ComplexType.Service });

    public ComplexType SelectedComplexType
    {
        get => _selectedComplexType;
        set
        {
            if (SetField(ref _selectedComplexType, value))
            {
                UpdateElevatorTilesForSelectedSet();
            }
        }
    }

    public ComplexType DimensionSelectedComplexType
    {
        get => _dimensionSelectedComplexType;
        set
        {
            if (SetField(ref _dimensionSelectedComplexType, value))
            {
                UpdateDimensionTilesForSelectedSet();
            }
        }
    }

    public ObservableCollection<LightSettingsBlock> AvailableLightingConfigs
    {
        get => _availableLightingConfigs;
        private set => SetField(ref _availableLightingConfigs, value);
    }

    public ObservableCollection<AlarmBlock> AvailableAlarms
    {
        get => _availableAlarms;
        private set => SetField(ref _availableAlarms, value);
    }

    public ObservableCollection<FogSettingsBlock> AvailableFogSettings
    {
        get => _availableFogSettings;
        set
        {
            if (_availableFogSettings != null)
            {
                _availableFogSettings.CollectionChanged -= AvailableFogSettings_CollectionChanged;
                foreach (var fog in _availableFogSettings)
                {
                    fog.PropertyChanged -= FogBlock_PropertyChanged;
                }
            }

            _availableFogSettings = value ?? new ObservableCollection<FogSettingsBlock>();
            _availableFogSettings.CollectionChanged += AvailableFogSettings_CollectionChanged;
            foreach (var fog in _availableFogSettings)
            {
                fog.PropertyChanged += FogBlock_PropertyChanged;
            }

            OnPropertyChanged();
            RefreshFogTransitionOptions();
        }
    }

    public ObservableCollection<FogTransitionOption> AvailableFogTransitionOptions => _availableFogTransitionOptions;

    public ObservableCollection<ElevatorTileOption> AvailableElevatorTiles
    {
        get => _availableElevatorTiles;
        private set => SetField(ref _availableElevatorTiles, value);
    }

    public ObservableCollection<DimensionOption> AvailableDimensions
    {
        get => _availableDimensions;
        private set => SetField(ref _availableDimensions, value);
    }

    public DimensionOption? SelectedDimension
    {
        get => _selectedDimension;
        set
        {
            if (_selectedDimension == value) return;
            var prev = _selectedDimension;
            if (SetField(ref _selectedDimension, value))
            {
                OnDimensionSelectionChanged(prev);
            }
        }
    }

    public LevelLayoutViewModel()
        : this(null, null, null, null, null)
    {
    }

    public LevelLayoutViewModel(
        ObservableCollection<ComplexResourceSetBlock>? complexResourceSets = null,
        IEnumerable<LevelLayoutBlock>? initialLevels = null,
        ObservableCollection<LightSettingsBlock>? lightingConfigs = null,
        ObservableCollection<AlarmBlock>? alarms = null,
        ObservableCollection<FogSettingsBlock>? fogSettings = null)
    {
        if (complexResourceSets != null)
        {
            _availableComplexResourceSets = complexResourceSets;
        }
        else
        {
            var vanillaBlocks = ComplexResourceSetService.LoadVanillaBlocks();
            _availableComplexResourceSets = new ObservableCollection<ComplexResourceSetBlock>(vanillaBlocks);
        }

        if (lightingConfigs != null)
        {
            _availableLightingConfigs = lightingConfigs;
        }
        else
        {
            var vanillaLighting = LightSettingsService.LoadVanillaBlocks();
            _availableLightingConfigs = new ObservableCollection<LightSettingsBlock>(vanillaLighting);
        }

        if (alarms != null)
        {
            _availableAlarms = alarms;
        }
        else
        {
            var vanillaAlarms = AlarmService.LoadVanillaAlarms();
            _availableAlarms = new ObservableCollection<AlarmBlock>(vanillaAlarms);
        }

        if (fogSettings != null)
        {
            _availableFogSettings = fogSettings;
        }
        else
        {
            var vanillaFog = FogSettingsService.LoadVanillaBlocks();
            _availableFogSettings = new ObservableCollection<FogSettingsBlock>(vanillaFog);
        }

        _availableFogSettings.CollectionChanged += AvailableFogSettings_CollectionChanged;
        foreach (var fog in _availableFogSettings)
        {
            fog.PropertyChanged += FogBlock_PropertyChanged;
        }

        RefreshFogTransitionOptions();

        var loadedLevels = initialLevels?.ToList() ??
                           LevelLayoutService.GetInitialLevels(_availableComplexResourceSets);
        _levels = new ObservableCollection<LevelLayoutBlock>(loadedLevels);
        _levels.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            UpdateStatusText();
        };

        _gridState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LevelLayoutGridState.HasGridSizeWarning) ||
                e.PropertyName == nameof(LevelLayoutGridState.GridSizeWarningMessage))
            {
                OnPropertyChanged(nameof(HasGridSizeWarning));
                OnPropertyChanged(nameof(GridSizeWarningMessage));
            }
        };

        foreach (var set in _availableComplexResourceSets)
        {
            if (set.LevelGenConfig != null)
            {
                set.LevelGenConfig.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LevelGenConfig.GridSize) ||
                        e.PropertyName == nameof(LevelGenConfig.CellDimension) ||
                        e.PropertyName == nameof(LevelGenConfig.AltitudeOffset))
                    {
                        var activeSet = GetActiveComplexResourceSet();
                        if (activeSet == set)
                        {
                            _gridState.GridSize = set.LevelGenConfig.GridSize > 0 ? set.LevelGenConfig.GridSize : 40;
                            _gridState.CellDimension = set.LevelGenConfig.CellDimension > 0
                                ? set.LevelGenConfig.CellDimension
                                : 64.0;
                            _gridState.AltitudeOffset = set.LevelGenConfig.AltitudeOffset > 0
                                ? set.LevelGenConfig.AltitudeOffset
                                : 6.0;
                            OnPropertyChanged(nameof(HasGridSizeWarning));
                            OnPropertyChanged(nameof(GridSizeWarningMessage));
                        }
                    }
                };
            }
        }

        if (_levels.Count > 0)
        {
            SelectedLevel = _levels[0];
        }
        else
        {
            RefreshAvailableDimensions();
        }

        UpdateStatusText();
        UpdateActiveDimensionView();
    }

    public void RefreshAvailableDimensions()
    {
        _availableDimensions.Clear();
        _availableDimensions.Add(new DimensionOption("Reality", "Reality"));

        if (_selectedLevel != null)
        {
            foreach (var dim in _selectedLevel.Dimensions)
            {
                string displayName = !string.IsNullOrWhiteSpace(dim.Name) ? dim.Name : dim.DimensionIndex;
                _availableDimensions.Add(new DimensionOption(dim.DimensionIndex, displayName));
            }
        }

        _selectedDimension = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == _activeDimensionIndex)
                             ?? _availableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");
        OnPropertyChanged(nameof(SelectedDimension));
        OnPropertyChanged(nameof(CanAddDimension));
    }

    private void AvailableFogSettings_CollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (FogSettingsBlock fog in e.NewItems)
            {
                fog.PropertyChanged += FogBlock_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (FogSettingsBlock fog in e.OldItems)
            {
                fog.PropertyChanged -= FogBlock_PropertyChanged;
            }
        }

        RefreshFogTransitionOptions();
    }

    private void FogBlock_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FogSettingsBlock.Name) ||
            e.PropertyName == nameof(FogSettingsBlock.PersistentId) ||
            e.PropertyName == nameof(FogSettingsBlock.DisplayText))
        {
            RefreshFogTransitionOptions();
        }
    }

    public void RefreshFogTransitionOptions()
    {
        _availableFogTransitionOptions.Clear();
        _availableFogTransitionOptions.Add(new FogTransitionOption(0, "Do not transition"));
        if (_availableFogSettings != null)
        {
            foreach (var fog in _availableFogSettings)
            {
                _availableFogTransitionOptions.Add(new FogTransitionOption(fog.PersistentId, fog.DisplayText));
            }
        }

        OnPropertyChanged(nameof(AvailableFogTransitionOptions));
    }

    private void InitializeAvailableDimensions()
    {
        RefreshAvailableDimensions();
    }

    public LevelLayoutBlock? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (_selectedLevel != null)
            {
                _selectedLevel.PropertyChanged -= SelectedLevel_PropertyChanged;
            }

            if (SetField(ref _selectedLevel, value))
            {
                if (_selectedLevel != null)
                {
                    _selectedLevel.PropertyChanged += SelectedLevel_PropertyChanged;
                }

                _isConfirmingDeleteDimension = false;
                _dimensionIndexToDelete = null;
                OnPropertyChanged(nameof(IsConfirmingDeleteDimension));

                _activeDimensionIndex = "Reality";
                RefreshAvailableDimensions();

                UpdateStatusText();
                UpdateActiveDimensionView();
                OnPropertyChanged(nameof(HasSelectedLevel));
                OnPropertyChanged(nameof(HasMainSector));
                OnPropertyChanged(nameof(HasSecondarySector));
                OnPropertyChanged(nameof(HasOverloadSector));
                OnPropertyChanged(nameof(HasDuplicatePersistentId));
                OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
            }
        }
    }

    private void SelectedLevel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LevelLayoutBlock.PersistentId))
        {
            OnPropertyChanged(nameof(HasDuplicatePersistentId));
            OnPropertyChanged(nameof(DuplicatePersistentIdWarning));
        }
        else if (e.PropertyName == nameof(LevelLayoutBlock.Name))
        {
            UpdateStatusText();
        }
        else if (e.PropertyName == nameof(LevelLayoutBlock.ElevatorTile) ||
                 e.PropertyName == nameof(LevelLayoutBlock.ComplexResourceSet))
        {
            UpdateActiveDimensionView();
        }
        else if (e.PropertyName == nameof(LevelLayoutBlock.HasSecondaryDoor) ||
                 e.PropertyName == nameof(LevelLayoutBlock.HasOverloadDoor))
        {
            OnPropertyChanged(nameof(CanAddSecondaryDoor));
            OnPropertyChanged(nameof(CanAddOverloadDoor));
            OnPropertyChanged(nameof(HasSecondaryDoor));
            OnPropertyChanged(nameof(HasOverloadDoor));
            OnPropertyChanged(nameof(HasSectorDoors));
            OnPropertyChanged(nameof(HasSecondarySector));
            OnPropertyChanged(nameof(HasOverloadSector));
        }
    }

    public bool HasSelectedLevel => _selectedLevel != null;

    public bool HasDuplicatePersistentId
    {
        get
        {
            if (_selectedLevel == null) return false;
            return _levels.Count(l => l.PersistentId == _selectedLevel.PersistentId) > 1;
        }
    }

    public string DuplicatePersistentIdWarning
    {
        get
        {
            if (_selectedLevel == null) return string.Empty;
            int count = _levels.Count(l => l.PersistentId == _selectedLevel.PersistentId);
            if (count > 1)
            {
                return
                    $"Warning: Persistent ID {_selectedLevel.PersistentId} is shared by {count} levels. IDs must be unique.";
            }

            return string.Empty;
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    private void UpdateStatusText()
    {
        if (_levels.Count == 0)
        {
            StatusText = "No levels";
        }
        else if (_selectedLevel != null)
        {
            string dimInfo = _activeDimensionIndex == "Reality"
                ? "Reality"
                : (_selectedDimension?.DisplayName ?? _activeDimensionIndex);
            StatusText =
                $"Selected [{_selectedLevel.PersistentId}] {_selectedLevel.Name} - {dimInfo} ({_levels.Count} total)";
        }
        else
        {
            StatusText = $"{_levels.Count} level{(_levels.Count == 1 ? "" : "s")}";
        }
    }

    // --- Dimension Navigation & Active Dimension View ---

    public string ActiveDimensionIndex => _activeDimensionIndex;

    public string ActiveDimensionDisplayName
    {
        get
        {
            if (IsViewingReality) return "Reality";
            if (_selectedLevel != null)
            {
                var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
                if (dim != null && !string.IsNullOrWhiteSpace(dim.Name))
                {
                    return dim.Name;
                }
            }

            var opt = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == _activeDimensionIndex);
            return opt?.DisplayName ?? _activeDimensionIndex;
        }
    }

    public string ActiveDimensionTitle => $"Dimension: {ActiveDimensionDisplayName}";

    public string ActiveDimensionName
    {
        get
        {
            if (IsViewingReality) return "Reality";
            if (_selectedLevel != null)
            {
                var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
                if (dim != null)
                {
                    return dim.Name;
                }
            }

            return string.Empty;
        }
        set
        {
            if (_selectedLevel == null || IsViewingReality) return;
            var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
            if (dim != null && dim.Name != value)
            {
                dim.Name = value ?? string.Empty;
                var opt = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == _activeDimensionIndex);
                if (opt != null)
                {
                    opt.DisplayName = !string.IsNullOrWhiteSpace(value) ? value : dim.DimensionIndex;
                }

                OnPropertyChanged(nameof(ActiveDimensionName));
                OnPropertyChanged(nameof(ActiveDimensionDisplayName));
                OnPropertyChanged(nameof(ActiveDimensionTitle));
                UpdateStatusText();
            }
        }
    }

    public bool CanEditActiveDimensionName => HasSelectedLevel && !IsViewingReality;

    public bool IsViewingReality => string.Equals(_activeDimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase);

    public bool IsActiveDimensionConfigured
    {
        get
        {
            if (_selectedLevel == null) return false;
            if (IsViewingReality) return true;
            return _selectedLevel.HasDimension(_activeDimensionIndex);
        }
    }

    public ComplexType ActiveComplexType
    {
        get
        {
            if (_selectedLevel == null) return ComplexType.Mining;
            if (IsViewingReality) return _selectedLevel.ComplexType;
            var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
            return dim?.ComplexType ?? _selectedLevel.ComplexType;
        }
    }

    public string ActiveTilesetName => ActiveComplexType.ToString();

    public uint ActiveComplexResourceSetId
    {
        get
        {
            if (_selectedLevel == null) return 0;
            if (IsViewingReality) return _selectedLevel.ComplexResourceSet;
            var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
            return dim?.ComplexResourceSet ?? 0;
        }
    }

    public string ActiveComplexResourceSetName
    {
        get
        {
            uint id = ActiveComplexResourceSetId;
            var set = _availableComplexResourceSets.FirstOrDefault(s => s.PersistentId == id);
            return set != null ? set.DisplayText : $"[ID {id}]";
        }
    }

    public string ActiveDimensionGeomorphPrefab
    {
        get
        {
            if (_selectedLevel == null) return string.Empty;
            if (IsViewingReality) return _selectedLevel.ElevatorTile;
            var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
            return dim?.DimensionGeomorph ?? string.Empty;
        }
    }

    public string ActiveDimensionGeomorphFriendlyName
    {
        get
        {
            string prefab = ActiveDimensionGeomorphPrefab;
            return string.IsNullOrWhiteSpace(prefab) ? "None" : LevelLayoutService.GetFriendlyElevatorName(prefab);
        }
    }

    public string ActiveDimensionTileCategory
    {
        get
        {
            if (_selectedLevel == null) return string.Empty;
            if (IsViewingReality) return "Elevator Shaft";
            var dim = _selectedLevel.GetDimension(_activeDimensionIndex);
            return dim?.TileCategory ?? "Dimension Geomorph";
        }
    }

    public bool CanAddDimension => HasSelectedLevel && (_selectedLevel?.Dimensions.Count ?? 0) < 20;

    public bool CanEditActiveDimension => HasSelectedLevel;

    public bool CanDeleteActiveDimension => HasSelectedLevel && !IsViewingReality &&
                                            (_selectedLevel?.HasDimension(_activeDimensionIndex) ?? false);

    public void OpenAddDimensionDialog()
    {
        if (_selectedLevel == null || !CanAddDimension) return;

        int nextIndex = 1;
        for (int i = 1; i <= 20; i++)
        {
            if (!_selectedLevel.HasDimension($"Dimension_{i}"))
            {
                nextIndex = i;
                break;
            }
        }

        string dimKey = $"Dimension_{nextIndex}";
        string defaultName = $"Dimension {nextIndex}";
        var newOpt = new DimensionOption(dimKey, defaultName);
        OpenPromptDimensionDialog(newOpt, _selectedDimension, isNew: true);
    }

    private void OnDimensionSelectionChanged(DimensionOption? previousDimension)
    {
        if (_selectedDimension == null) return;

        if (_selectedLevel == null)
        {
            _activeDimensionIndex = _selectedDimension.DimensionIndex;
            UpdateActiveDimensionView();
            UpdateStatusText();
            return;
        }

        if (string.Equals(_selectedDimension.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase))
        {
            _activeDimensionIndex = "Reality";
            UpdateActiveDimensionView();
            UpdateStatusText();
        }
        else
        {
            if (_selectedLevel.HasDimension(_selectedDimension.DimensionIndex))
            {
                _activeDimensionIndex = _selectedDimension.DimensionIndex;
                UpdateActiveDimensionView();
                UpdateStatusText();
            }
            else
            {
                OpenPromptDimensionDialog(_selectedDimension, previousDimension, isNew: true);
            }
        }
    }

    private void UpdateActiveDimensionView()
    {
        OnPropertyChanged(nameof(ActiveDimensionIndex));
        OnPropertyChanged(nameof(ActiveDimensionDisplayName));
        OnPropertyChanged(nameof(ActiveDimensionName));
        OnPropertyChanged(nameof(ActiveDimensionTitle));
        OnPropertyChanged(nameof(CanEditActiveDimensionName));
        OnPropertyChanged(nameof(IsViewingReality));
        OnPropertyChanged(nameof(IsActiveDimensionConfigured));
        OnPropertyChanged(nameof(ActiveComplexType));
        OnPropertyChanged(nameof(ActiveTilesetName));
        OnPropertyChanged(nameof(ActiveComplexResourceSetId));
        OnPropertyChanged(nameof(ActiveComplexResourceSetName));
        OnPropertyChanged(nameof(ActiveDimensionGeomorphPrefab));
        OnPropertyChanged(nameof(ActiveDimensionGeomorphFriendlyName));
        OnPropertyChanged(nameof(ActiveDimensionTileCategory));
        OnPropertyChanged(nameof(CanAddDimension));
        OnPropertyChanged(nameof(CanEditActiveDimension));
        OnPropertyChanged(nameof(CanDeleteActiveDimension));
        OnPropertyChanged(nameof(DimensionDeleteConfirmationMessage));

        UpdateGridStateTile();
    }

    public ComplexResourceSetBlock? GetActiveComplexResourceSet()
    {
        uint id = ActiveComplexResourceSetId;
        return _availableComplexResourceSets?.FirstOrDefault(s => s.PersistentId == id);
    }

    public bool HasGridSizeWarning => _gridState.HasGridSizeWarning;
    public string GridSizeWarningMessage => _gridState.GridSizeWarningMessage;

    private void UpdateGridStateTile()
    {
        if (_selectedLevel == null)
        {
            _gridState.Clear();
            OnPropertyChanged(nameof(HasGridSizeWarning));
            OnPropertyChanged(nameof(GridSizeWarningMessage));
            return;
        }

        var activeSet = GetActiveComplexResourceSet();
        if (activeSet?.LevelGenConfig != null)
        {
            GridState.GridSize = activeSet.LevelGenConfig.GridSize > 0 ? activeSet.LevelGenConfig.GridSize : 40;
            GridState.CellDimension = activeSet.LevelGenConfig.CellDimension > 0
                ? activeSet.LevelGenConfig.CellDimension
                : 64.0;
            GridState.AltitudeOffset = activeSet.LevelGenConfig.AltitudeOffset > 0
                ? activeSet.LevelGenConfig.AltitudeOffset
                : 6.0;
        }
        else
        {
            GridState.GridSize = 40;
            GridState.CellDimension = 64.0;
            GridState.AltitudeOffset = 6.0;
        }

        string startingPrefab = ActiveDimensionGeomorphPrefab;
        if (string.IsNullOrWhiteSpace(startingPrefab) && IsViewingReality)
        {
            startingPrefab = _selectedLevel.ElevatorTile;
        }

        if (GridState.PlacedGeomorphs.Count == 0 && !string.IsNullOrWhiteSpace(startingPrefab))
        {
            GridState.InitializeStartingTile(startingPrefab, 0, 0);
        }

        OnPropertyChanged(nameof(GridState));
        OnPropertyChanged(nameof(HasGridSizeWarning));
        OnPropertyChanged(nameof(GridSizeWarningMessage));
        OnPropertyChanged(nameof(CanAddSecondaryDoor));
        OnPropertyChanged(nameof(CanAddOverloadDoor));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
        OnPropertyChanged(nameof(HasSectorDoors));
        OnPropertyChanged(nameof(HasSecondarySector));
        OnPropertyChanged(nameof(HasOverloadSector));
    }

    public void Pan(double deltaX, double deltaY)
    {
        GridState.Pan(deltaX, deltaY);
    }

    public void ZoomAt(double deltaWheel, double mouseX, double mouseY)
    {
        GridState.ZoomAt(deltaWheel, mouseX, mouseY);
    }

    public void ZoomIn()
    {
        GridState.ZoomIn();
    }

    public void ZoomOut()
    {
        GridState.ZoomOut();
    }

    public void ResetView()
    {
        GridState.ResetView();
    }

    // --- Door Expansion & Geomorph Picking ---

    public bool IsPickingGeomorphForDoor
    {
        get => _isPickingGeomorphForDoor;
        set => SetField(ref _isPickingGeomorphForDoor, value);
    }

    public PlacedDoorNode? SelectedDoorForExpansion
    {
        get => _selectedDoorForExpansion;
        set => SetField(ref _selectedDoorForExpansion, value);
    }

    public string ExpansionDirectionText
    {
        get => _expansionDirectionText;
        set => SetField(ref _expansionDirectionText, value);
    }

    public string ExpansionTargetCellText
    {
        get => _expansionTargetCellText;
        set => SetField(ref _expansionTargetCellText, value);
    }

    public string ExpansionSourceGeoText
    {
        get => _expansionSourceGeoText;
        set => SetField(ref _expansionSourceGeoText, value);
    }

    public string GeomorphFilterText
    {
        get => _geomorphFilterText;
        set
        {
            if (SetField(ref _geomorphFilterText, value))
            {
                OnPropertyChanged(nameof(FilteredExpansionGeomorphs));
                if (SelectedExpansionGeomorph != null &&
                    !FilteredExpansionGeomorphs.Contains(SelectedExpansionGeomorph))
                {
                    SelectedExpansionGeomorph = FilteredExpansionGeomorphs.FirstOrDefault();
                }
            }
        }
    }

    public ObservableCollection<GeomorphOption> AvailableExpansionGeomorphs
    {
        get => _availableExpansionGeomorphs;
        set => SetField(ref _availableExpansionGeomorphs, value);
    }

    public IEnumerable<GeomorphOption> FilteredExpansionGeomorphs
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_geomorphFilterText))
            {
                return _availableExpansionGeomorphs;
            }

            return _availableExpansionGeomorphs.Where(g =>
                g.DisplayName.Contains(_geomorphFilterText, StringComparison.OrdinalIgnoreCase) ||
                g.PrefabPath.Contains(_geomorphFilterText, StringComparison.OrdinalIgnoreCase) ||
                g.Category.Contains(_geomorphFilterText, StringComparison.OrdinalIgnoreCase));
        }
    }

    public Geometry? SelectedExpansionGeomorphPreviewGeometry
    {
        get => _selectedExpansionGeomorphPreviewGeometry;
        private set => SetField(ref _selectedExpansionGeomorphPreviewGeometry, value);
    }

    public bool HasSelectedExpansionGeomorphPreview => SelectedExpansionGeomorphPreviewGeometry != null;

    public GeomorphOption? SelectedExpansionGeomorph
    {
        get => _selectedExpansionGeomorph;
        set
        {
            if (SetField(ref _selectedExpansionGeomorph, value))
            {
                UpdateAvailableExpansionRotations();
                UpdateSelectedExpansionGeomorphPreview();
            }
        }
    }

    public ObservableCollection<RotationOption> AvailableExpansionRotations
    {
        get => _availableExpansionRotations;
        set => SetField(ref _availableExpansionRotations, value);
    }

    public RotationOption? SelectedExpansionRotation
    {
        get => _selectedExpansionRotation;
        set
        {
            if (SetField(ref _selectedExpansionRotation, value))
            {
                UpdateSelectedExpansionGeomorphPreview();
            }
        }
    }

    public void UpdateSelectedExpansionGeomorphPreview()
    {
        if (_selectedExpansionGeomorph == null || string.IsNullOrWhiteSpace(_selectedExpansionGeomorph.PrefabPath))
        {
            SelectedExpansionGeomorphPreviewGeometry = null;
            OnPropertyChanged(nameof(HasSelectedExpansionGeomorphPreview));
            return;
        }

        double rot = _selectedExpansionRotation?.RotationAngle ?? 0.0;
        SelectedExpansionGeomorphPreviewGeometry = RoomSvgService.GenerateGeomorphGeometry(_selectedExpansionGeomorph.PrefabPath, rot);
        OnPropertyChanged(nameof(HasSelectedExpansionGeomorphPreview));
    }

    public bool HasAvailableRotations => _availableExpansionRotations.Count > 0;

    public ObservableCollection<AltitudeOption> AvailableExpansionAltitudes
    {
        get => _availableExpansionAltitudes;
        set => SetField(ref _availableExpansionAltitudes, value);
    }

    public AltitudeOption? SelectedExpansionAltitude
    {
        get => _selectedExpansionAltitude;
        set => SetField(ref _selectedExpansionAltitude, value);
    }

    public LightSettingsBlock? SelectedExpansionLighting
    {
        get => _selectedExpansionLighting;
        set => SetField(ref _selectedExpansionLighting, value);
    }

    private void InitializeExpansionAltitudes(GeomorphAltitude defaultAltitude = GeomorphAltitude.Mid)
    {
        _availableExpansionAltitudes.Clear();
        _availableExpansionAltitudes.Add(new AltitudeOption { Altitude = GeomorphAltitude.High, DisplayText = "High" });
        _availableExpansionAltitudes.Add(new AltitudeOption { Altitude = GeomorphAltitude.Mid, DisplayText = "Mid" });
        _availableExpansionAltitudes.Add(new AltitudeOption { Altitude = GeomorphAltitude.Low, DisplayText = "Low" });
        SelectedExpansionAltitude = _availableExpansionAltitudes.FirstOrDefault(a => a.Altitude == defaultAltitude)
                                    ?? _availableExpansionAltitudes.FirstOrDefault(a =>
                                        a.Altitude == GeomorphAltitude.Mid);
    }

    private void UpdateAvailableExpansionRotations()
    {
        _availableExpansionRotations.Clear();
        if (_selectedExpansionGeomorph != null && _selectedDoorForExpansion != null)
        {
            var validRotations =
                GridState.GetValidRotationsForGeomorph(_selectedDoorForExpansion,
                    _selectedExpansionGeomorph.PrefabPath);
            foreach (var rot in validRotations)
            {
                _availableExpansionRotations.Add(new RotationOption
                {
                    RotationAngle = rot,
                    DisplayText = $"{rot:0}°"
                });
            }
        }

        OnPropertyChanged(nameof(AvailableExpansionRotations));
        OnPropertyChanged(nameof(HasAvailableRotations));

        // Preserve current rotation selection if valid, else pick first valid
        if (_selectedExpansionRotation != null)
        {
            var matching = _availableExpansionRotations.FirstOrDefault(r =>
                Math.Abs(r.RotationAngle - _selectedExpansionRotation.RotationAngle) < 0.1);
            SelectedExpansionRotation = matching ?? _availableExpansionRotations.FirstOrDefault();
        }
        else
        {
            SelectedExpansionRotation = _availableExpansionRotations.FirstOrDefault();
        }

        if (_availableExpansionRotations.Count == 0 && _selectedExpansionGeomorph != null)
        {
            ExpansionErrorMessage = "Selected geomorph has no door facing the expansion direction.";
        }
        else if (ExpansionErrorMessage == "Selected geomorph has no door facing the expansion direction.")
        {
            ExpansionErrorMessage = string.Empty;
        }

        UpdateSelectedExpansionGeomorphPreview();
    }

    public DoorType SelectedExpansionDoorType
    {
        get => _selectedExpansionDoorType;
        set
        {
            if (SetField(ref _selectedExpansionDoorType, value))
            {
                OnPropertyChanged(nameof(IsExpansionRoomDoor));
                OnPropertyChanged(nameof(IsExpansionSecurityDoor));
                OnPropertyChanged(nameof(IsExpansionMainDoor));
                OnPropertyChanged(nameof(IsExpansionSecondaryDoor));
                OnPropertyChanged(nameof(IsExpansionOverloadDoor));
                OnPropertyChanged(nameof(IsExpansionToNewZone));
                OnPropertyChanged(nameof(ShowExpansionLighting));
            }
        }
    }

    public bool IsExpansionToNewZone => SelectedExpansionDoorType != DoorType.RoomDoor;
    public bool ShowExpansionLighting => IsExpansionToNewZone;

    public bool IsExpansionRoomDoor
    {
        get => _selectedExpansionDoorType == DoorType.RoomDoor;
        set
        {
            if (value) SelectedExpansionDoorType = DoorType.RoomDoor;
        }
    }

    public bool IsExpansionSecurityDoor
    {
        get => _selectedExpansionDoorType == DoorType.SecurityDoor;
        set
        {
            if (value) SelectedExpansionDoorType = DoorType.SecurityDoor;
        }
    }

    public bool IsExpansionMainDoor
    {
        get => _selectedExpansionDoorType == DoorType.MainDoor;
        set
        {
            if (value) SelectedExpansionDoorType = DoorType.MainDoor;
        }
    }

    public bool IsExpansionSecondaryDoor
    {
        get => _selectedExpansionDoorType == DoorType.SecondaryDoor;
        set
        {
            if (value) SelectedExpansionDoorType = DoorType.SecondaryDoor;
        }
    }

    public bool IsExpansionOverloadDoor
    {
        get => _selectedExpansionDoorType == DoorType.OverloadDoor;
        set
        {
            if (value) SelectedExpansionDoorType = DoorType.OverloadDoor;
        }
    }

    public string ExpansionErrorMessage
    {
        get => _expansionErrorMessage;
        set
        {
            if (SetField(ref _expansionErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasExpansionErrorMessage));
            }
        }
    }

    public bool HasExpansionErrorMessage => !string.IsNullOrWhiteSpace(_expansionErrorMessage);

    // --- Room Expansion in Geomorph State & Properties ---

    public bool IsExpandingRoom
    {
        get => _isExpandingRoom;
        set => SetField(ref _isExpandingRoom, value);
    }

    public PlacedDoorNode? SelectedRoomDoorForExpansion
    {
        get => _selectedRoomDoorForExpansion;
        set => SetField(ref _selectedRoomDoorForExpansion, value);
    }

    public string RoomExpansionGeomorphName
    {
        get => _roomExpansionGeomorphName;
        set => SetField(ref _roomExpansionGeomorphName, value);
    }

    public int RoomExpansionFromArea
    {
        get => _roomExpansionFromArea;
        set => SetField(ref _roomExpansionFromArea, value);
    }

    public int RoomExpansionTargetArea
    {
        get => _roomExpansionTargetArea;
        set => SetField(ref _roomExpansionTargetArea, value);
    }

    private LightSettingsBlock? _selectedRoomExpansionLighting;

    public LightSettingsBlock? SelectedRoomExpansionLighting
    {
        get => _selectedRoomExpansionLighting;
        set => SetField(ref _selectedRoomExpansionLighting, value);
    }

    public DoorType SelectedRoomExpansionDoorType
    {
        get => _selectedRoomExpansionDoorType;
        set
        {
            if (SetField(ref _selectedRoomExpansionDoorType, value))
            {
                OnPropertyChanged(nameof(IsRoomExpansionRoomDoor));
                OnPropertyChanged(nameof(IsRoomExpansionSecurityDoor));
                OnPropertyChanged(nameof(IsRoomExpansionMainDoor));
                OnPropertyChanged(nameof(IsRoomExpansionSecondaryDoor));
                OnPropertyChanged(nameof(IsRoomExpansionOverloadDoor));
                OnPropertyChanged(nameof(IsRoomExpansionToNewZone));
                OnPropertyChanged(nameof(ShowRoomExpansionLighting));
            }
        }
    }

    public bool IsRoomExpansionToNewZone => SelectedRoomExpansionDoorType != DoorType.RoomDoor ||
                                            (SelectedRoomDoorForExpansion?.IsZoneBoundary == true);

    public bool ShowRoomExpansionLighting => IsRoomExpansionToNewZone;

    public bool IsRoomExpansionRoomDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.RoomDoor;
        set
        {
            if (value) SelectedRoomExpansionDoorType = DoorType.RoomDoor;
        }
    }

    public bool IsRoomExpansionSecurityDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.SecurityDoor;
        set
        {
            if (value) SelectedRoomExpansionDoorType = DoorType.SecurityDoor;
        }
    }

    public bool IsRoomExpansionMainDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.MainDoor;
        set
        {
            if (value) SelectedRoomExpansionDoorType = DoorType.MainDoor;
        }
    }

    public bool IsRoomExpansionSecondaryDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.SecondaryDoor;
        set
        {
            if (value) SelectedRoomExpansionDoorType = DoorType.SecondaryDoor;
        }
    }

    public bool IsRoomExpansionOverloadDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.OverloadDoor;
        set
        {
            if (value) SelectedRoomExpansionDoorType = DoorType.OverloadDoor;
        }
    }

    public bool CanAddSecondaryDoor => !GridState.HasSecondaryDoor;
    public bool CanAddOverloadDoor => !GridState.HasOverloadDoor;

    public bool HasSecondaryDoor => GridState.HasSecondaryDoor;
    public bool HasOverloadDoor => GridState.HasOverloadDoor;
    public bool HasSectorDoors => HasSecondaryDoor || HasOverloadDoor;

    public bool HasMainSector => HasSelectedLevel;

    public bool HasSecondarySector =>
        HasSelectedLevel && (SelectedLevel?.HasSecondaryDoor == true || GridState.HasSecondaryDoor);

    public bool HasOverloadSector =>
        HasSelectedLevel && (SelectedLevel?.HasOverloadDoor == true || GridState.HasOverloadDoor);

    public string RoomExpansionErrorMessage
    {
        get => _roomExpansionErrorMessage;
        set
        {
            if (SetField(ref _roomExpansionErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasRoomExpansionErrorMessage));
            }
        }
    }

    public bool HasRoomExpansionErrorMessage => !string.IsNullOrWhiteSpace(_roomExpansionErrorMessage);

    public void OnDoorClicked(PlacedDoorNode? door)
    {
        if (door == null) return;

        if (door.IsBlocked)
        {
            StatusText = "Door is blocked by a zone / security door constraint.";
            return;
        }

        if (door.IsInternal)
        {
            // Room door: prompt to expand the room in the geomorph if not yet generated
            if (!door.IsConnected)
            {
                OpenRoomExpansionDialog(door);
            }
            else
            {
                if (door.IsSecondaryDoor)
                    StatusText = $"Room Door [Area {door.AreaFrom} <-> Area {door.AreaTo}] is a Secondary Door.";
                else if (door.IsOverloadDoor)
                    StatusText = $"Room Door [Area {door.AreaFrom} <-> Area {door.AreaTo}] is an Overload Door.";
                else if (door.IsMainDoor)
                    StatusText = $"Room Door [Area {door.AreaFrom} <-> Area {door.AreaTo}] is a Main Door.";
                else if (door.IsSecurityDoor)
                    StatusText = $"Room Door [Area {door.AreaFrom} <-> Area {door.AreaTo}] is a Security Door.";
                else
                    StatusText = $"Room Door [Area {door.AreaFrom} <-> Area {door.AreaTo}] is connected.";
            }
        }
        else
        {
            // Geomorph door: prompt user to pick a geomorph
            if (!door.IsConnected)
            {
                OpenDoorExpansionDialog(door);
            }
            else
            {
                if (door.IsSecondaryDoor)
                    StatusText = "Geomorph door is a Secondary Door.";
                else if (door.IsOverloadDoor)
                    StatusText = "Geomorph door is an Overload Door.";
                else if (door.IsMainDoor)
                    StatusText = "Geomorph door is a Main Door.";
                else if (door.IsSecurityDoor)
                    StatusText = "Geomorph door is a Security Door.";
                else
                    StatusText = "Geomorph door is connected.";
            }
        }
    }

    public void OpenRoomExpansionDialog(PlacedDoorNode door)
    {
        if (door.Geomorph == null) return;
        SelectedRoomDoorForExpansion = door;
        RoomExpansionGeomorphName = door.Geomorph.DisplayName;

        int targetArea = (!door.Geomorph.IsRoomGenerated(door.AreaFrom)) ? door.AreaFrom : door.AreaTo;
        int sourceArea = (!door.Geomorph.IsRoomGenerated(door.AreaFrom)) ? door.AreaTo : door.AreaFrom;

        RoomExpansionFromArea = sourceArea;
        RoomExpansionTargetArea = targetArea;
        SelectedRoomExpansionDoorType = DoorType.RoomDoor;
        SelectedRoomExpansionLighting = AvailableLightingConfigs.FirstOrDefault();
        RoomExpansionErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanAddSecondaryDoor));
        OnPropertyChanged(nameof(CanAddOverloadDoor));
        OnPropertyChanged(nameof(IsRoomExpansionToNewZone));
        OnPropertyChanged(nameof(ShowRoomExpansionLighting));
        IsExpandingRoom = true;
    }

    public bool ConfirmRoomExpansion()
    {
        if (SelectedRoomDoorForExpansion == null)
        {
            RoomExpansionErrorMessage = "No door selected for expansion.";
            return false;
        }

        if (SelectedRoomExpansionDoorType == DoorType.SecondaryDoor && GridState.HasSecondaryDoor)
        {
            RoomExpansionErrorMessage = "A Secondary Door has already been added to this level layout.";
            return false;
        }

        if (SelectedRoomExpansionDoorType == DoorType.OverloadDoor && GridState.HasOverloadDoor)
        {
            RoomExpansionErrorMessage = "An Overload Door has already been added to this level layout.";
            return false;
        }

        var door = SelectedRoomDoorForExpansion;
        uint? lighting = IsRoomExpansionToNewZone ? _selectedRoomExpansionLighting?.PersistentId : null;
        int generatedArea = GridState.GenerateRoomFromDoor(door, SelectedRoomExpansionDoorType, lighting);
        IsExpandingRoom = false;

        if (generatedArea >= 0)
        {
            string doorTypeStr = SelectedRoomExpansionDoorType switch
            {
                DoorType.SecondaryDoor => "Secondary Door",
                DoorType.OverloadDoor => "Overload Door",
                DoorType.MainDoor => "Main Door",
                DoorType.SecurityDoor => "Security Door",
                _ => "Room Door"
            };
            StatusText =
                $"Generated Area {generatedArea} in {door.Geomorph?.DisplayName ?? "geomorph"} connected via {doorTypeStr}.";
        }
        else
        {
            StatusText = $"Room door connected [Area {door.AreaFrom} <-> Area {door.AreaTo}].";
        }

        SelectedRoomDoorForExpansion = null;
        return true;
    }

    public void CancelRoomExpansion()
    {
        IsExpandingRoom = false;
        SelectedRoomDoorForExpansion = null;
        RoomExpansionErrorMessage = string.Empty;
    }

    // --- Edit Room Modal State & Properties ---

    public bool IsEditingRoom
    {
        get => _isEditingRoom;
        set => SetField(ref _isEditingRoom, value);
    }

    public PlacedRoomNode? EditingRoom
    {
        get => _editingRoom;
        set
        {
            if (SetField(ref _editingRoom, value))
            {
                if (value?.Geomorph != null && value.Geomorph.Lighting > 0 && ActiveZoneSettings != null &&
                    ActiveZoneSettings.Lighting == 0)
                {
                    ActiveZoneSettings.Lighting = value.Geomorph.Lighting;
                }

                OnPropertyChanged(nameof(EditingRoomTitle));
                OnPropertyChanged(nameof(EditingRoomGeomorphName));
                OnPropertyChanged(nameof(EditingRoomAreaIndex));
                OnPropertyChanged(nameof(EditingRoomZone));
                OnPropertyChanged(nameof(EditingRoomZoneDisplayName));
                OnPropertyChanged(nameof(EditingRoomAreaSize));
                OnPropertyChanged(nameof(EditingRoomGeomorphAltitude));
                OnPropertyChanged(nameof(SelectedGeomorphAltitudeOption));
                OnPropertyChanged(nameof(SelectedGeomorphLightingOption));
                OnPropertyChanged(nameof(EditingRoomGeomorphLighting));
                OnPropertyChanged(nameof(ActiveZoneSettings));
                OnPropertyChanged(nameof(SelectedZoneLightingOption));
                OnPropertyChanged(nameof(EditingZoneLighting));
                OnPropertyChanged(nameof(HasSecondaryDoor));
                OnPropertyChanged(nameof(HasOverloadDoor));
                OnPropertyChanged(nameof(HasSectorDoors));
                UpdateAvailableDcMainDoors();
            }
        }
    }

    public string EditingRoomTitle => EditingRoom != null
        ? $"Edit Room - {EditingRoom.DisplayName} (Area {EditingRoom.AreaIndex})"
        : "Edit Room";

    public string EditingRoomGeomorphName => EditingRoom?.Geomorph?.DisplayName ?? "Unknown";
    public int EditingRoomAreaIndex => EditingRoom?.AreaIndex ?? 0;
    public int EditingRoomZone => EditingRoom?.Zone ?? 0;

    public string EditingRoomZoneDisplayName
    {
        get
        {
            if (EditingRoom == null || SelectedLevel == null)
            {
                return $"Zone {EditingRoomZone}";
            }

            return SelectedLevel.GetZoneDisplayName(EditingRoomZone);
        }
    }

    public string EditingRoomAreaSize => EditingRoom != null ? $"{EditingRoom.Area:F1}" : "0.0";

    // --- Zone Settings for Editing Room ---
    public ZoneSettings? ActiveZoneSettings =>
        EditingRoom != null ? GridState.GetOrCreateZoneSettings(EditingRoom.Zone) : null;

    public LightSettingsBlock? SelectedZoneLightingOption
    {
        get => AvailableLightingConfigs.FirstOrDefault(l => l.PersistentId == (ActiveZoneSettings?.Lighting ?? 0));
        set
        {
            if (value != null && ActiveZoneSettings != null && ActiveZoneSettings.Lighting != value.PersistentId)
            {
                ActiveZoneSettings.Lighting = value.PersistentId;
                OnPropertyChanged(nameof(SelectedZoneLightingOption));
                OnPropertyChanged(nameof(EditingZoneLighting));
            }
        }
    }

    public uint EditingZoneLighting
    {
        get => ActiveZoneSettings?.Lighting ?? 0;
        set
        {
            if (ActiveZoneSettings != null && ActiveZoneSettings.Lighting != value)
            {
                ActiveZoneSettings.Lighting = value;
                OnPropertyChanged(nameof(EditingZoneLighting));
                OnPropertyChanged(nameof(SelectedZoneLightingOption));
            }
        }
    }

    private ObservableCollection<DcMainDoorOption> _availableDcMainDoors = new();
    public ObservableCollection<DcMainDoorOption> AvailableDcMainDoors => _availableDcMainDoors;
    public bool HasAvailableDcMainDoors => _availableDcMainDoors.Count > 0;

    public void UpdateAvailableDcMainDoors()
    {
        _availableDcMainDoors.Clear();
        if (EditingRoom == null)
        {
            OnPropertyChanged(nameof(HasAvailableDcMainDoors));
            return;
        }

        var currentZone = ActiveZoneSettings;
        if (currentZone == null)
        {
            OnPropertyChanged(nameof(HasAvailableDcMainDoors));
            return;
        }

        var seen = new HashSet<string>();
        int mainDoorIndex = 1;
        bool hasSelected = false;
        bool isUpdatingSelection = false;

        foreach (var door in GridState.PlacedDoors.Where(d => d.IsMainDoor))
        {
            string pairKey = door.ConnectedDoor != null
                ? (string.Compare(door.Id, door.ConnectedDoor.Id, StringComparison.Ordinal) < 0
                    ? $"{door.Id}_{door.ConnectedDoor.Id}"
                    : $"{door.ConnectedDoor.Id}_{door.Id}")
                : door.Id;

            if (seen.Add(pairKey))
            {
                string label;
                if (door.IsInternal)
                {
                    label = $"Main Door {mainDoorIndex} [Area {door.AreaFrom} <-> Area {door.AreaTo}]";
                }
                else
                {
                    var geoName = door.Geomorph?.DisplayName ?? "Geomorph";
                    label = $"Main Door {mainDoorIndex} ({door.Direction}) - {geoName}";
                }

                bool isMatch = currentZone.DcOpenedMainDoorIds.Contains(door.Id) || (door.ConnectedDoor != null &&
                    currentZone.DcOpenedMainDoorIds.Contains(door.ConnectedDoor.Id));
                bool isSelected = false;
                if (isMatch && !hasSelected)
                {
                    isSelected = true;
                    hasSelected = true;
                }

                var opt = new DcMainDoorOption
                {
                    DoorId = door.Id,
                    DisplayName = label,
                    Door = door,
                    IsSelected = isSelected
                };
                opt.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DcMainDoorOption.IsSelected))
                    {
                        if (isUpdatingSelection) return;

                        if (opt.IsSelected)
                        {
                            isUpdatingSelection = true;
                            try
                            {
                                foreach (var other in _availableDcMainDoors)
                                {
                                    if (other != opt && other.IsSelected)
                                    {
                                        other.IsSelected = false;
                                    }
                                }

                                currentZone.DcOpenedMainDoorIds.Clear();
                                currentZone.DcOpenedMainDoorIds.Add(opt.DoorId);
                            }
                            finally
                            {
                                isUpdatingSelection = false;
                            }
                        }
                        else
                        {
                            currentZone.DcOpenedMainDoorIds.Remove(opt.DoorId);
                            if (opt.Door?.ConnectedDoor != null)
                                currentZone.DcOpenedMainDoorIds.Remove(opt.Door.ConnectedDoor.Id);
                        }
                    }
                };
                _availableDcMainDoors.Add(opt);
                mainDoorIndex++;
            }
        }

        if (currentZone.DcOpenedMainDoorIds.Count > 1)
        {
            var selectedOpt = _availableDcMainDoors.FirstOrDefault(o => o.IsSelected);
            currentZone.DcOpenedMainDoorIds.Clear();
            if (selectedOpt != null)
            {
                currentZone.DcOpenedMainDoorIds.Add(selectedOpt.DoorId);
            }
        }

        if (!GridState.HasSecondaryDoor && currentZone.DcOpensSecondary)
        {
            currentZone.DcOpensSecondary = false;
        }

        if (!GridState.HasOverloadDoor && currentZone.DcOpensOverload)
        {
            currentZone.DcOpensOverload = false;
        }

        OnPropertyChanged(nameof(HasAvailableDcMainDoors));
        OnPropertyChanged(nameof(HasSecondaryDoor));
        OnPropertyChanged(nameof(HasOverloadDoor));
        OnPropertyChanged(nameof(HasSectorDoors));
        OnPropertyChanged(nameof(HasSecondarySector));
        OnPropertyChanged(nameof(HasOverloadSector));
    }

    private ObservableCollection<AltitudeOption> _availableGeomorphAltitudes = new()
    {
        new AltitudeOption { Altitude = GeomorphAltitude.High, DisplayText = "High" },
        new AltitudeOption { Altitude = GeomorphAltitude.Mid, DisplayText = "Mid" },
        new AltitudeOption { Altitude = GeomorphAltitude.Low, DisplayText = "Low" }
    };

    public ObservableCollection<AltitudeOption> AvailableGeomorphAltitudes
    {
        get => _availableGeomorphAltitudes;
        set => SetField(ref _availableGeomorphAltitudes, value);
    }

    public AltitudeOption? SelectedGeomorphAltitudeOption
    {
        get => AvailableGeomorphAltitudes.FirstOrDefault(a =>
            a.Altitude == (EditingRoom?.Geomorph?.Altitude ?? GeomorphAltitude.Mid));
        set
        {
            if (value != null && EditingRoom?.Geomorph != null && EditingRoom.Geomorph.Altitude != value.Altitude)
            {
                EditingRoom.Geomorph.Altitude = value.Altitude;
                OnPropertyChanged(nameof(SelectedGeomorphAltitudeOption));
                OnPropertyChanged(nameof(EditingRoomGeomorphAltitude));
            }
        }
    }

    public GeomorphAltitude EditingRoomGeomorphAltitude => EditingRoom?.Geomorph?.Altitude ?? GeomorphAltitude.Mid;

    public LightSettingsBlock? SelectedGeomorphLightingOption
    {
        get => AvailableLightingConfigs.FirstOrDefault(l => l.PersistentId == (EditingRoom?.Geomorph?.Lighting ?? 0));
        set
        {
            if (value != null && EditingRoom?.Geomorph != null && EditingRoom.Geomorph.Lighting != value.PersistentId)
            {
                EditingRoom.Geomorph.Lighting = value.PersistentId;
                OnPropertyChanged(nameof(SelectedGeomorphLightingOption));
                OnPropertyChanged(nameof(EditingRoomGeomorphLighting));
            }
        }
    }

    public uint EditingRoomGeomorphLighting
    {
        get => EditingRoom?.Geomorph?.Lighting ?? 0;
        set
        {
            if (EditingRoom?.Geomorph != null && EditingRoom.Geomorph.Lighting != value)
            {
                EditingRoom.Geomorph.Lighting = value;
                OnPropertyChanged(nameof(EditingRoomGeomorphLighting));
                OnPropertyChanged(nameof(SelectedGeomorphLightingOption));
            }
        }
    }

    // --- Layout Deletion Confirmation State & Properties ---
    private bool _isConfirmingLayoutDeletion;
    private string _layoutDeletionTitle = string.Empty;
    private string _layoutDeletionMessage = string.Empty;
    private string _layoutDeletionWarning = string.Empty;
    private bool _hasLayoutDeletionWarning;

    private enum LayoutDeletionType
    {
        Geomorph,
        Room
    }

    private LayoutDeletionType _pendingLayoutDeletionType;
    private PlacedGeomorphNode? _pendingDeleteGeomorph;
    private PlacedRoomNode? _pendingDeleteRoom;

    public bool IsConfirmingLayoutDeletion
    {
        get => _isConfirmingLayoutDeletion;
        set => SetField(ref _isConfirmingLayoutDeletion, value);
    }

    public string LayoutDeletionTitle
    {
        get => _layoutDeletionTitle;
        set => SetField(ref _layoutDeletionTitle, value);
    }

    public string LayoutDeletionMessage
    {
        get => _layoutDeletionMessage;
        set => SetField(ref _layoutDeletionMessage, value);
    }

    public string LayoutDeletionWarning
    {
        get => _layoutDeletionWarning;
        set => SetField(ref _layoutDeletionWarning, value);
    }

    public bool HasLayoutDeletionWarning
    {
        get => _hasLayoutDeletionWarning;
        set => SetField(ref _hasLayoutDeletionWarning, value);
    }

    public void RequestDeleteGeomorph()
    {
        if (EditingRoom?.Geomorph == null) return;
        var geo = EditingRoom.Geomorph;
        var disconnected = GridState.GetDisconnectedGeomorphsOnDelete(geo);

        LayoutDeletionTitle = "Confirm Delete Geomorph";
        LayoutDeletionMessage = $"Are you sure you want to delete geomorph '{geo.DisplayName}'?";

        if (disconnected.Count > 0)
        {
            LayoutDeletionWarning =
                $"Warning: Deleting this geomorph will also delete {disconnected.Count} other geomorph(s) that will no longer have a path back to the elevator:\n" +
                string.Join("\n", disconnected.Select(g => $"• {g.DisplayName} (Cell {g.GridX}, {g.GridY})"));
            HasLayoutDeletionWarning = true;
        }
        else
        {
            LayoutDeletionWarning = string.Empty;
            HasLayoutDeletionWarning = false;
        }

        _pendingLayoutDeletionType = LayoutDeletionType.Geomorph;
        _pendingDeleteGeomorph = geo;
        _pendingDeleteRoom = null;
        IsConfirmingLayoutDeletion = true;
    }

    public void RequestDeleteRoom()
    {
        if (EditingRoom == null) return;
        var room = EditingRoom;
        var (disconnectedRooms, disconnectedGeos) = GridState.GetDisconnectedRoomsOnDelete(room);

        LayoutDeletionTitle = "Confirm Delete Room";
        LayoutDeletionMessage = $"Are you sure you want to delete room '{room.DisplayName}' (Area {room.AreaIndex})?";

        if (disconnectedRooms.Count > 0 || disconnectedGeos.Count > 0)
        {
            var warningItems = new List<string>();
            foreach (var r in disconnectedRooms)
            {
                warningItems.Add($"• Area {r.AreaIndex} in {r.Geomorph?.DisplayName ?? "Unknown"}");
            }

            LayoutDeletionWarning =
                $"Warning: Deleting this room will also delete {disconnectedRooms.Count} other room(s) that will no longer have a path back to the elevator:\n" +
                string.Join("\n", warningItems);
            HasLayoutDeletionWarning = true;
        }
        else
        {
            LayoutDeletionWarning = string.Empty;
            HasLayoutDeletionWarning = false;
        }

        _pendingLayoutDeletionType = LayoutDeletionType.Room;
        _pendingDeleteRoom = room;
        _pendingDeleteGeomorph = null;
        IsConfirmingLayoutDeletion = true;
    }

    public void ConfirmLayoutDeletion()
    {
        if (_pendingLayoutDeletionType == LayoutDeletionType.Geomorph && _pendingDeleteGeomorph != null)
        {
            var name = _pendingDeleteGeomorph.DisplayName;
            GridState.DeleteGeomorph(_pendingDeleteGeomorph);
            StatusText = $"Deleted geomorph '{name}'.";
        }
        else if (_pendingLayoutDeletionType == LayoutDeletionType.Room && _pendingDeleteRoom != null)
        {
            var name = _pendingDeleteRoom.DisplayName;
            var area = _pendingDeleteRoom.AreaIndex;
            GridState.DeleteRoom(_pendingDeleteRoom);
            StatusText = $"Deleted room '{name}' (Area {area}).";
        }

        IsConfirmingLayoutDeletion = false;
        _pendingDeleteGeomorph = null;
        _pendingDeleteRoom = null;
        CloseEditRoomDialog();
    }

    public void CancelLayoutDeletion()
    {
        IsConfirmingLayoutDeletion = false;
        _pendingDeleteGeomorph = null;
        _pendingDeleteRoom = null;
    }

    public void OnRoomClicked(PlacedRoomNode? room)
    {
        if (room == null) return;
        EditingRoom = room;
        IsEditingRoom = true;
    }

    public void CloseEditRoomDialog()
    {
        IsEditingRoom = false;
        EditingRoom = null;
    }

    public void OpenDoorExpansionDialog(PlacedDoorNode door)
    {
        SelectedDoorForExpansion = door;
        var (targetX, targetY, dirName) = GridState.GetExpansionTargetCell(door);
        ExpansionDirectionText = dirName;
        ExpansionTargetCellText = $"Cell ({targetX}, {targetY})";
        ExpansionSourceGeoText = door.Geomorph?.DisplayName ?? "Starting Elevator";
        SelectedExpansionDoorType = DoorType.RoomDoor;
        ExpansionErrorMessage = string.Empty;
        GeomorphFilterText = string.Empty;
        OnPropertyChanged(nameof(CanAddSecondaryDoor));
        OnPropertyChanged(nameof(CanAddOverloadDoor));

        _availableExpansionGeomorphs.Clear();

        var addedPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Get the active complex / tileset
        ComplexType activeTileset = ActiveComplexType;
        string activeComplexName = activeTileset.ToString();

        var matchingGeos = GeomorphService.GetGeomorphsForTileset(activeComplexName);
        if (matchingGeos.Count == 0)
        {
            var vanillaGeos = GeomorphService.LoadGeomorphs();
            matchingGeos = vanillaGeos
                .Where(g => g.Complex.Equals(activeComplexName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingGeos.Count == 0)
            {
                matchingGeos = vanillaGeos;
            }
        }

        // Filter geomorphs to those that have valid rotation(s) for expanding from this door
        foreach (var geo in matchingGeos)
        {
            if (string.IsNullOrWhiteSpace(geo.PrefabPath)) continue;
            if (!addedPrefabs.Add(geo.PrefabPath)) continue;

            var validRotations = GridState.GetValidRotationsForGeomorph(door, geo.PrefabPath);
            if (validRotations.Count > 0)
            {
                var svgGeom = RoomSvgService.GenerateGeomorphGeometry(geo.PrefabPath, 0.0);
                _availableExpansionGeomorphs.Add(new GeomorphOption
                {
                    DisplayName = geo.DisplayName,
                    PrefabPath = geo.PrefabPath,
                    Category = geo.Category,
                    Complex = geo.Complex,
                    ImageName = geo.ImageName,
                    SvgGeometry = svgGeom
                });
            }
        }

        // If no geomorphs had valid rotations, fallback to matching ones so UI isn't empty
        if (_availableExpansionGeomorphs.Count == 0)
        {
            foreach (var geo in matchingGeos)
            {
                if (string.IsNullOrWhiteSpace(geo.PrefabPath)) continue;
                if (!addedPrefabs.Add(geo.PrefabPath)) continue;

                var svgGeom = RoomSvgService.GenerateGeomorphGeometry(geo.PrefabPath, 0.0);
                _availableExpansionGeomorphs.Add(new GeomorphOption
                {
                    DisplayName = geo.DisplayName,
                    PrefabPath = geo.PrefabPath,
                    Category = geo.Category,
                    Complex = geo.Complex,
                    ImageName = geo.ImageName,
                    SvgGeometry = svgGeom
                });
            }
        }

        OnPropertyChanged(nameof(FilteredExpansionGeomorphs));
        SelectedExpansionGeomorph = _availableExpansionGeomorphs.FirstOrDefault();
        UpdateAvailableExpansionRotations();
        UpdateSelectedExpansionGeomorphPreview();
        InitializeExpansionAltitudes(door.Altitude);
        SelectedExpansionLighting = AvailableLightingConfigs.FirstOrDefault();
        IsPickingGeomorphForDoor = true;
    }

    public bool ConfirmDoorExpansion()
    {
        if (_selectedDoorForExpansion == null)
        {
            ExpansionErrorMessage = "No door selected for expansion.";
            return false;
        }

        if (_selectedExpansionGeomorph == null)
        {
            ExpansionErrorMessage = "Please select a geomorph to generate.";
            return false;
        }

        if (_selectedExpansionRotation == null)
        {
            ExpansionErrorMessage = "No valid rotation available for this geomorph in this direction.";
            return false;
        }

        if (SelectedExpansionDoorType == DoorType.SecondaryDoor && GridState.HasSecondaryDoor)
        {
            ExpansionErrorMessage = "A Secondary Door has already been added to this level layout.";
            return false;
        }

        if (SelectedExpansionDoorType == DoorType.OverloadDoor && GridState.HasOverloadDoor)
        {
            ExpansionErrorMessage = "An Overload Door has already been added to this level layout.";
            return false;
        }

        var (targetX, targetY, _) = GridState.GetExpansionTargetCell(_selectedDoorForExpansion);
        if (targetX < GridState.MinGridCoord || targetX > GridState.MaxGridCoord ||
            targetY < GridState.MinGridCoord || targetY > GridState.MaxGridCoord)
        {
            ExpansionErrorMessage =
                $"Cannot expand beyond the {GridState.GridSize}x{GridState.GridSize} grid boundaries.";
            return false;
        }

        var existing = GridState.GetGeomorphAt(targetX, targetY);
        if (existing != null)
        {
            ExpansionErrorMessage = $"Cell ({targetX}, {targetY}) is already occupied by \"{existing.DisplayName}\".";
            return false;
        }

        double rotationAngle = _selectedExpansionRotation.RotationAngle;
        GeomorphAltitude altitude = _selectedExpansionAltitude?.Altitude ?? _selectedDoorForExpansion.Altitude;
        uint lighting = IsExpansionToNewZone ? (_selectedExpansionLighting?.PersistentId ?? 0) : 0;
        var placedGeo = GridState.ExpandFromDoor(_selectedDoorForExpansion, _selectedExpansionGeomorph.PrefabPath,
            SelectedExpansionDoorType, rotationAngle, altitude, lighting);
        if (placedGeo == null)
        {
            ExpansionErrorMessage = "Failed to place geomorph at the target cell.";
            return false;
        }

        string doorTypeStr = SelectedExpansionDoorType switch
        {
            DoorType.SecondaryDoor => " (Secondary Door)",
            DoorType.OverloadDoor => " (Overload Door)",
            DoorType.SecurityDoor => " (Security Door)",
            _ => "."
        };
        StatusText =
            $"Placed \"{_selectedExpansionGeomorph.DisplayName}\" (Rotation {rotationAngle:0}°, Altitude {altitude}) at cell ({targetX}, {targetY}){doorTypeStr}";

        IsPickingGeomorphForDoor = false;
        _selectedDoorForExpansion = null;
        return true;
    }

    public void CancelDoorExpansion()
    {
        IsPickingGeomorphForDoor = false;
        _selectedDoorForExpansion = null;
        ExpansionErrorMessage = string.Empty;
    }

    public void ToggleSecurityDoor(PlacedDoorNode? door)
    {
        if (door == null) return;
        _gridState.ToggleSecurityDoor(door);
    }

    public void RequestEditActiveDimension()
    {
        if (_selectedLevel == null || _selectedDimension == null) return;
        OpenPromptDimensionDialog(_selectedDimension, _selectedDimension, isNew: false);
    }

    // --- Delete Dimension Modal ---

    public bool IsConfirmingDeleteDimension
    {
        get => _isConfirmingDeleteDimension;
        set => SetField(ref _isConfirmingDeleteDimension, value);
    }

    public string? DimensionIndexToDelete
    {
        get => _dimensionIndexToDelete;
        set
        {
            if (SetField(ref _dimensionIndexToDelete, value))
            {
                OnPropertyChanged(nameof(DimensionDeleteConfirmationMessage));
            }
        }
    }

    public string DimensionDeleteConfirmationMessage
    {
        get
        {
            string targetIndex = _dimensionIndexToDelete ?? _activeDimensionIndex;
            string displayName = targetIndex;
            if (_selectedLevel != null)
            {
                var dim = _selectedLevel.GetDimension(targetIndex);
                if (dim != null && !string.IsNullOrWhiteSpace(dim.Name))
                {
                    displayName = dim.Name;
                }
            }

            return $"Are you sure you want to delete dimension \"{displayName}\"?";
        }
    }

    public void RequestDeleteActiveDimension()
    {
        if (_selectedLevel == null || IsViewingReality || !_selectedLevel.HasDimension(_activeDimensionIndex)) return;
        _dimensionIndexToDelete = _activeDimensionIndex;
        OnPropertyChanged(nameof(DimensionDeleteConfirmationMessage));
        IsConfirmingDeleteDimension = true;
    }

    public bool ConfirmDeleteDimension()
    {
        if (_selectedLevel == null || IsViewingReality)
        {
            IsConfirmingDeleteDimension = false;
            _dimensionIndexToDelete = null;
            return false;
        }

        string dimIndex = _dimensionIndexToDelete ?? _activeDimensionIndex;
        var dim = _selectedLevel.GetDimension(dimIndex);
        string deletedName = dim != null && !string.IsNullOrWhiteSpace(dim.Name) ? dim.Name : dimIndex;
        _selectedLevel.RemoveDimension(dimIndex);

        var opt = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == dimIndex);
        if (opt != null)
        {
            _availableDimensions.Remove(opt);
        }

        _activeDimensionIndex = "Reality";
        _selectedDimension = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");
        OnPropertyChanged(nameof(SelectedDimension));
        OnPropertyChanged(nameof(CanAddDimension));

        UpdateActiveDimensionView();
        UpdateStatusText();
        StatusText = $"Removed {deletedName} from [{_selectedLevel.PersistentId}] {_selectedLevel.Name}";

        IsConfirmingDeleteDimension = false;
        _dimensionIndexToDelete = null;
        return true;
    }

    public void CancelDeleteDimension()
    {
        IsConfirmingDeleteDimension = false;
        _dimensionIndexToDelete = null;
    }

    public void DeleteActiveDimension()
    {
        ConfirmDeleteDimension();
    }

    // --- Configure / Prompt Dimension Modal ---

    public bool IsConfiguringDimension
    {
        get => _isConfiguringDimension;
        set => SetField(ref _isConfiguringDimension, value);
    }

    public DimensionOption? PromptingDimensionOption
    {
        get => _promptingDimensionOption;
        set
        {
            if (SetField(ref _promptingDimensionOption, value))
            {
                OnPropertyChanged(nameof(PromptDimensionTitle));
                OnPropertyChanged(nameof(IsPromptingReality));
                OnPropertyChanged(nameof(PromptStartingTileLabel));
            }
        }
    }

    public string DimensionCustomName
    {
        get => _dimensionCustomName;
        set => SetField(ref _dimensionCustomName, value);
    }

    public bool IsPromptingReality => string.Equals(_promptingDimensionOption?.DimensionIndex, "Reality",
        StringComparison.OrdinalIgnoreCase);

    public string PromptStartingTileLabel =>
        IsPromptingReality ? "Starting Elevator Tile:" : "Starting Tile / DimensionGeomorph:";

    public string PromptDimensionTitle => _promptingDimensionOption != null
        ? (_promptingDimensionOption.DimensionIndex == "Reality"
            ? "Edit Dimension: Reality"
            : $"Configure Dimension - {_promptingDimensionOption.DisplayName}")
        : "Configure Dimension";

    public ComplexResourceSetBlock? DimensionSelectedComplexResourceSet
    {
        get => _dimensionSelectedComplexResourceSet;
        set
        {
            if (SetField(ref _dimensionSelectedComplexResourceSet, value))
            {
                UpdateDimensionTilesForSelectedSet();
            }
        }
    }

    public ObservableCollection<string> AvailableTileSources => _availableTileSources;

    public string SelectedTileSource
    {
        get => _selectedTileSource;
        set
        {
            string targetValue = IsPromptingReality ? "Elevator Shafts" : value;
            if (SetField(ref _selectedTileSource, targetValue))
            {
                UpdateDimensionTilesForSelectedSet();
            }
        }
    }

    public ObservableCollection<ElevatorTileOption> AvailableDimensionTiles
    {
        get => _availableDimensionTiles;
        private set => SetField(ref _availableDimensionTiles, value);
    }

    public ElevatorTileOption? SelectedDimensionTile
    {
        get => _selectedDimensionTile;
        set => SetField(ref _selectedDimensionTile, value);
    }

    public string DimensionPromptErrorMessage
    {
        get => _dimensionPromptErrorMessage;
        set
        {
            if (SetField(ref _dimensionPromptErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasDimensionPromptErrorMessage));
            }
        }
    }

    public bool HasDimensionPromptErrorMessage => !string.IsNullOrWhiteSpace(_dimensionPromptErrorMessage);

    public static List<ElevatorTileOption> GetElevatorTilesForComplex(ComplexType complexType)
    {
        var options = new List<ElevatorTileOption>();
        var allGeos = GeomorphService.LoadGeomorphs();
        var complexName = complexType.ToString();

        var matching = allGeos.Where(g =>
            g.Complex.Equals(complexName, StringComparison.OrdinalIgnoreCase) &&
            (g.SubComplex.Contains("Elevator", StringComparison.OrdinalIgnoreCase) ||
             g.Category.Contains("Elevator", StringComparison.OrdinalIgnoreCase) ||
             g.PrefabPath.Contains("elevator", StringComparison.OrdinalIgnoreCase) ||
             g.PrefabName.Contains("elevator", StringComparison.OrdinalIgnoreCase))
        ).ToList();

        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var geo in matching)
        {
            if (!string.IsNullOrWhiteSpace(geo.PrefabPath) && added.Add(geo.PrefabPath))
            {
                options.Add(new ElevatorTileOption
                {
                    Prefab = geo.PrefabPath,
                    DisplayName = geo.DisplayName,
                    Category = "Elevator Shaft"
                });
            }
        }

        if (options.Count == 0)
        {
            var vanillaSets = ComplexResourceSetService.LoadVanillaBlocks();
            var set = vanillaSets.FirstOrDefault(s => s.ComplexType == complexType) ?? vanillaSets.FirstOrDefault();
            if (set != null)
            {
                foreach (var shaft in set.ElevatorShafts_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(shaft.Prefab) && added.Add(shaft.Prefab))
                    {
                        options.Add(new ElevatorTileOption
                        {
                            Prefab = shaft.Prefab,
                            DisplayName = LevelLayoutService.GetFriendlyElevatorName(shaft.Prefab),
                            Category = "Elevator Shaft"
                        });
                    }
                }
            }
        }

        return options;
    }

    public static List<ElevatorTileOption> GetObjectiveTilesForComplex(ComplexType complexType)
    {
        var options = new List<ElevatorTileOption>();
        var allGeos = GeomorphService.LoadGeomorphs();
        var complexName = complexType.ToString();

        var matching = allGeos.Where(g =>
            g.Complex.Equals(complexName, StringComparison.OrdinalIgnoreCase) &&
            (g.Category.Contains("Objective", StringComparison.OrdinalIgnoreCase) ||
             g.Category.Contains("Custom", StringComparison.OrdinalIgnoreCase) ||
             g.SubComplex.Contains("Objective", StringComparison.OrdinalIgnoreCase) ||
             g.PrefabPath.Contains("objective", StringComparison.OrdinalIgnoreCase))
        ).ToList();

        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var geo in matching)
        {
            if (!string.IsNullOrWhiteSpace(geo.PrefabPath) && added.Add(geo.PrefabPath))
            {
                options.Add(new ElevatorTileOption
                {
                    Prefab = geo.PrefabPath,
                    DisplayName = geo.DisplayName,
                    Category = "Dimension Geomorph"
                });
            }
        }

        if (options.Count == 0)
        {
            var vanillaSets = ComplexResourceSetService.LoadVanillaBlocks();
            var set = vanillaSets.FirstOrDefault(s => s.ComplexType == complexType) ?? vanillaSets.FirstOrDefault();
            if (set != null)
            {
                foreach (var objGeo in set.CustomGeomorphs_Objective_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(objGeo.Prefab) && added.Add(objGeo.Prefab))
                    {
                        options.Add(new ElevatorTileOption
                        {
                            Prefab = objGeo.Prefab,
                            DisplayName = LevelLayoutService.GetFriendlyElevatorName(objGeo.Prefab),
                            Category = "Dimension Geomorph"
                        });
                    }
                }
            }
        }

        return options;
    }

    public void OpenPromptDimensionDialog(DimensionOption targetDimension, DimensionOption? previousDimension = null,
        bool isNew = false)
    {
        _promptingDimensionOption = targetDimension;
        _previousDimensionOption =
            previousDimension ?? _availableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");

        bool isReality = string.Equals(targetDimension.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase);

        if (isReality)
        {
            DimensionCustomName = "Reality";

            _availableTileSources.Clear();
            _availableTileSources.Add("Elevator Shafts");

            if (_selectedLevel != null)
            {
                DimensionSelectedComplexType = _selectedLevel.ComplexType;
                DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                                                          .FirstOrDefault(s =>
                                                              s.PersistentId == _selectedLevel.ComplexResourceSet)
                                                      ?? _availableComplexResourceSets.FirstOrDefault();

                SelectedTileSource = "Elevator Shafts";
                UpdateDimensionTilesForSelectedSet();

                SelectedDimensionTile = _availableDimensionTiles.FirstOrDefault(t =>
                                            string.Equals(t.Prefab, _selectedLevel.ElevatorTile,
                                                StringComparison.OrdinalIgnoreCase))
                                        ?? _availableDimensionTiles.FirstOrDefault();
            }
        }
        else
        {
            _availableTileSources.Clear();
            _availableTileSources.Add("Elevator Shafts");
            _availableTileSources.Add("Dimension Geomorphs (Custom Objective 1x1)");

            var existing = _selectedLevel?.GetDimension(targetDimension.DimensionIndex);
            if (existing != null && _selectedLevel != null &&
                _selectedLevel.HasDimension(targetDimension.DimensionIndex) && !isNew)
            {
                DimensionCustomName = existing.Name;
                DimensionSelectedComplexType = existing.ComplexType;

                DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                                                          .FirstOrDefault(s =>
                                                              s.PersistentId == existing.ComplexResourceSet)
                                                      ?? _availableComplexResourceSets.FirstOrDefault();

                SelectedTileSource = existing.TileCategory == "Elevator Shaft"
                    ? "Elevator Shafts"
                    : "Dimension Geomorphs (Custom Objective 1x1)";

                UpdateDimensionTilesForSelectedSet();

                SelectedDimensionTile = _availableDimensionTiles.FirstOrDefault(t =>
                                            string.Equals(t.Prefab, existing.DimensionGeomorph,
                                                StringComparison.OrdinalIgnoreCase))
                                        ?? _availableDimensionTiles.FirstOrDefault();
            }
            else
            {
                DimensionCustomName = targetDimension.DisplayName;
                DimensionSelectedComplexType = _selectedLevel?.ComplexType ?? ComplexType.Mining;

                if (_selectedLevel != null)
                {
                    DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                                                              .FirstOrDefault(s =>
                                                                  s.PersistentId == _selectedLevel.ComplexResourceSet)
                                                          ?? _availableComplexResourceSets.FirstOrDefault();
                }
                else
                {
                    DimensionSelectedComplexResourceSet = _availableComplexResourceSets.FirstOrDefault();
                }

                SelectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
                UpdateDimensionTilesForSelectedSet();
                if (_availableDimensionTiles.Count == 0)
                {
                    SelectedTileSource = "Elevator Shafts";
                    UpdateDimensionTilesForSelectedSet();
                }
            }
        }

        DimensionPromptErrorMessage = string.Empty;
        IsConfiguringDimension = true;
        OnPropertyChanged(nameof(PromptDimensionTitle));
        OnPropertyChanged(nameof(IsPromptingReality));
        OnPropertyChanged(nameof(PromptStartingTileLabel));
    }

    private void UpdateDimensionTilesForSelectedSet()
    {
        _availableDimensionTiles.Clear();

        if (_selectedTileSource == "Elevator Shafts" || IsPromptingReality)
        {
            if (_dimensionSelectedComplexResourceSet != null && _dimensionSelectedComplexResourceSet.ElevatorShafts_1x1.Count > 0)
            {
                foreach (var shaft in _dimensionSelectedComplexResourceSet.ElevatorShafts_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(shaft.Prefab))
                    {
                        _availableDimensionTiles.Add(new ElevatorTileOption
                        {
                            Prefab = shaft.Prefab,
                            DisplayName = LevelLayoutService.GetFriendlyElevatorName(shaft.Prefab),
                            Category = "Elevator Shaft"
                        });
                    }
                }
            }
            else
            {
                var options = GetElevatorTilesForComplex(_dimensionSelectedComplexType);
                foreach (var opt in options)
                {
                    _availableDimensionTiles.Add(opt);
                }
            }
        }
        else
        {
            if (_dimensionSelectedComplexResourceSet != null && _dimensionSelectedComplexResourceSet.CustomGeomorphs_Objective_1x1.Count > 0)
            {
                foreach (var objGeo in _dimensionSelectedComplexResourceSet.CustomGeomorphs_Objective_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(objGeo.Prefab))
                    {
                        _availableDimensionTiles.Add(new ElevatorTileOption
                        {
                            Prefab = objGeo.Prefab,
                            DisplayName = LevelLayoutService.GetFriendlyElevatorName(objGeo.Prefab),
                            Category = "Dimension Geomorph"
                        });
                    }
                }
            }
            else
            {
                var options = GetObjectiveTilesForComplex(_dimensionSelectedComplexType);
                foreach (var opt in options)
                {
                    _availableDimensionTiles.Add(opt);
                }
            }
        }

        SelectedDimensionTile = _availableDimensionTiles.FirstOrDefault();
    }

    public bool ConfirmPromptDimension()
    {
        if (_selectedLevel == null)
        {
            DimensionPromptErrorMessage = "No level selected.";
            return false;
        }

        if (_promptingDimensionOption == null)
        {
            DimensionPromptErrorMessage = "No dimension selected.";
            return false;
        }

        if (SelectedDimensionTile == null || string.IsNullOrWhiteSpace(SelectedDimensionTile.Prefab))
        {
            DimensionPromptErrorMessage = "Please select a starting tile / DimensionGeomorph.";
            return false;
        }

        bool isReality = string.Equals(_promptingDimensionOption.DimensionIndex, "Reality",
            StringComparison.OrdinalIgnoreCase);

        if (isReality)
        {
            _selectedLevel.ComplexType = DimensionSelectedComplexType;
            if (DimensionSelectedComplexResourceSet != null)
            {
                _selectedLevel.ComplexResourceSet = DimensionSelectedComplexResourceSet.PersistentId;
            }
            _selectedLevel.ElevatorTile = SelectedDimensionTile.Prefab;
            _activeDimensionIndex = "Reality";
        }
        else
        {
            string tileCategory = SelectedTileSource == "Elevator Shafts" ? "Elevator Shaft" : "Dimension Geomorph";
            string dimName = string.IsNullOrWhiteSpace(DimensionCustomName)
                ? _promptingDimensionOption.DimensionIndex
                : DimensionCustomName.Trim();

            uint compSetId = DimensionSelectedComplexResourceSet?.PersistentId ?? 0;
            if (compSetId == 0)
            {
                var matchingSet = _availableComplexResourceSets.FirstOrDefault(s => s.ComplexType == DimensionSelectedComplexType);
                compSetId = matchingSet?.PersistentId ?? 1;
            }

            var dimBlock = new DimensionLayoutBlock
            {
                DimensionIndex = _promptingDimensionOption.DimensionIndex,
                Name = dimName,
                ComplexType = DimensionSelectedComplexType,
                ComplexResourceSet = compSetId,
                DimensionGeomorph = SelectedDimensionTile.Prefab,
                TileCategory = tileCategory,
                InternalEnabled = true
            };

            _selectedLevel.SetDimension(dimBlock);
            _activeDimensionIndex = _promptingDimensionOption.DimensionIndex;

            var existingOpt = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == dimBlock.DimensionIndex);
            if (existingOpt != null)
            {
                existingOpt.DisplayName = dimBlock.Name;
            }
            else
            {
                var newOpt = new DimensionOption(dimBlock.DimensionIndex, dimBlock.Name);
                _availableDimensions.Add(newOpt);
            }
        }

        IsConfiguringDimension = false;
        DimensionPromptErrorMessage = string.Empty;

        _selectedDimension = _availableDimensions.FirstOrDefault(d => d.DimensionIndex == _activeDimensionIndex);
        OnPropertyChanged(nameof(SelectedDimension));
        OnPropertyChanged(nameof(CanAddDimension));

        UpdateActiveDimensionView();
        UpdateStatusText();
        StatusText =
            $"Configured {ActiveDimensionDisplayName} for [{_selectedLevel.PersistentId}] {_selectedLevel.Name}";
        return true;
    }

    public void CancelPromptDimension()
    {
        IsConfiguringDimension = false;
        DimensionPromptErrorMessage = string.Empty;

        _selectedDimension = _previousDimensionOption != null && _availableDimensions.Contains(_previousDimensionOption)
            ? _previousDimensionOption
            : _availableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");
        OnPropertyChanged(nameof(SelectedDimension));
        _activeDimensionIndex = _selectedDimension?.DimensionIndex ?? "Reality";
        UpdateActiveDimensionView();
    }

    // --- Create Level Modal ---

    public bool IsCreatingLevel
    {
        get => _isCreatingLevel;
        set => SetField(ref _isCreatingLevel, value);
    }

    public string NewLevelName
    {
        get => _newLevelName;
        set => SetField(ref _newLevelName, value);
    }

    public uint NewLevelPersistentId
    {
        get => _newLevelPersistentId;
        set => SetField(ref _newLevelPersistentId, value);
    }

    public ComplexResourceSetBlock? SelectedComplexResourceSet
    {
        get => _selectedComplexResourceSet;
        set
        {
            if (SetField(ref _selectedComplexResourceSet, value))
            {
                UpdateElevatorTilesForSelectedSet();
            }
        }
    }

    public ElevatorTileOption? SelectedElevatorTile
    {
        get => _selectedElevatorTile;
        set => SetField(ref _selectedElevatorTile, value);
    }

    public string CreationErrorMessage
    {
        get => _creationErrorMessage;
        set
        {
            if (SetField(ref _creationErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasCreationErrorMessage));
            }
        }
    }

    public bool HasCreationErrorMessage => !string.IsNullOrWhiteSpace(_creationErrorMessage);

    public void OpenCreateLevelDialog()
    {
        uint randomId = GenerateUniquePersistentId();
        NewLevelPersistentId = randomId;
        NewLevelName = $"Level_Custom_{randomId}";

        SelectedComplexType = _selectedLevel?.ComplexType ?? ComplexType.Mining;
        SelectedComplexResourceSet = _availableComplexResourceSets
                                         .FirstOrDefault(s => s.ComplexType == SelectedComplexType)
                                     ?? _availableComplexResourceSets.FirstOrDefault();

        UpdateElevatorTilesForSelectedSet();

        CreationErrorMessage = string.Empty;
        IsCreatingLevel = true;
    }

    private void UpdateElevatorTilesForSelectedSet()
    {
        _availableElevatorTiles.Clear();

        if (_selectedComplexResourceSet != null && _selectedComplexResourceSet.ElevatorShafts_1x1.Count > 0)
        {
            foreach (var shaft in _selectedComplexResourceSet.ElevatorShafts_1x1)
            {
                if (!string.IsNullOrWhiteSpace(shaft.Prefab))
                {
                    _availableElevatorTiles.Add(new ElevatorTileOption
                    {
                        Prefab = shaft.Prefab,
                        DisplayName = LevelLayoutService.GetFriendlyElevatorName(shaft.Prefab),
                        Category = "Elevator Shaft"
                    });
                }
            }
        }
        else
        {
            var options = GetElevatorTilesForComplex(_selectedComplexType);
            foreach (var opt in options)
            {
                _availableElevatorTiles.Add(opt);
            }
        }

        SelectedElevatorTile = _availableElevatorTiles.FirstOrDefault();
    }

    private uint GenerateUniquePersistentId()
    {
        var rng = new Random();
        for (int i = 0; i < 1000; i++)
        {
            uint candidate = (uint)rng.Next(1000, 999999);
            if (!_levels.Any(l => l.PersistentId == candidate))
            {
                return candidate;
            }
        }

        return (uint)(_levels.Count > 0 ? _levels.Max(l => l.PersistentId) + 1 : 1);
    }

    public bool ConfirmCreateLevel()
    {
        if (string.IsNullOrWhiteSpace(NewLevelName))
        {
            CreationErrorMessage = "Level Name cannot be empty.";
            return false;
        }

        if (NewLevelPersistentId == 0)
        {
            CreationErrorMessage = "Persistent ID must be greater than 0.";
            return false;
        }

        if (_levels.Any(l => l.PersistentId == NewLevelPersistentId))
        {
            CreationErrorMessage = $"A level with Persistent ID {NewLevelPersistentId} already exists.";
            return false;
        }

        if (SelectedElevatorTile == null || string.IsNullOrWhiteSpace(SelectedElevatorTile.Prefab))
        {
            CreationErrorMessage = "Please select a starting elevator tile.";
            return false;
        }

        uint compSetId = SelectedComplexResourceSet?.PersistentId ?? 0;
        if (compSetId == 0)
        {
            var matchingSet = _availableComplexResourceSets.FirstOrDefault(s => s.ComplexType == SelectedComplexType);
            compSetId = matchingSet?.PersistentId ?? 1;
        }

        var newLevel = LevelLayoutService.CreateFreshLevel(
            NewLevelPersistentId,
            NewLevelName.Trim(),
            SelectedComplexType,
            SelectedElevatorTile.Prefab,
            compSetId
        );

        _levels.Add(newLevel);
        SelectedLevel = newLevel;

        IsCreatingLevel = false;
        CreationErrorMessage = string.Empty;
        UpdateStatusText();
        StatusText = $"Created level [{newLevel.PersistentId}] {newLevel.Name}";
        return true;
    }

    public void CancelCreateLevel()
    {
        IsCreatingLevel = false;
        CreationErrorMessage = string.Empty;
    }

    // --- Delete Level Modal ---

    public bool IsConfirmingDelete
    {
        get => _isConfirmingDelete;
        set => SetField(ref _isConfirmingDelete, value);
    }

    public LevelLayoutBlock? LevelToDelete
    {
        get => _levelToDelete;
        set
        {
            if (SetField(ref _levelToDelete, value))
            {
                OnPropertyChanged(nameof(DeleteConfirmationMessage));
            }
        }
    }

    public string DeleteConfirmationMessage
    {
        get
        {
            var target = _levelToDelete ?? _selectedLevel;
            if (target == null) return "Are you sure you want to delete this level?";
            return $"Are you sure you want to delete level [{target.PersistentId}] \"{target.Name}\"?";
        }
    }

    public void RequestDeleteSelectedLevel()
    {
        if (_selectedLevel == null) return;
        LevelToDelete = _selectedLevel;
        IsConfirmingDelete = true;
    }

    public bool ConfirmDeleteLevel()
    {
        var target = _levelToDelete ?? _selectedLevel;
        if (target == null)
        {
            IsConfirmingDelete = false;
            return false;
        }

        int index = _levels.IndexOf(target);
        _levels.Remove(target);

        if (_levels.Count > 0)
        {
            int nextIndex = Math.Clamp(index, 0, _levels.Count - 1);
            SelectedLevel = _levels[nextIndex];
        }
        else
        {
            SelectedLevel = null;
        }

        IsConfirmingDelete = false;
        LevelToDelete = null;
        UpdateStatusText();
        StatusText = $"Deleted level [{target.PersistentId}] \"{target.Name}\"";
        return true;
    }

    public void CancelDeleteLevel()
    {
        IsConfirmingDelete = false;
        LevelToDelete = null;
    }

    // --- Sector Settings Modal ---

    private bool _isEditingSector;
    private SectorType _editingSectorType = SectorType.Main;
    private SectorSettings? _editingSectorSettings;

    public bool IsEditingSector
    {
        get => _isEditingSector;
        set => SetField(ref _isEditingSector, value);
    }

    public SectorType EditingSectorType
    {
        get => _editingSectorType;
        set
        {
            if (SetField(ref _editingSectorType, value))
            {
                OnPropertyChanged(nameof(EditingSectorTitle));
                OnPropertyChanged(nameof(IsMainSector));
            }
        }
    }

    public bool IsMainSector => EditingSectorType == SectorType.Main;
    public string EditingSectorTitle => $"Sector Settings - {EditingSectorType}";

    public SectorSettings? EditingSectorSettings
    {
        get => _editingSectorSettings;
        set => SetField(ref _editingSectorSettings, value);
    }

    public ObservableCollection<WinCondition> AvailableWinConditions { get; } = new(Enum.GetValues<WinCondition>());
    public ObservableCollection<ObjectiveType> AvailableObjectiveTypes { get; } = new(Enum.GetValues<ObjectiveType>());

    public ObservableCollection<ZoneOption> AvailableMainExitZones { get; } = new();
    public ObservableCollection<ZoneOption> AvailableObjectiveZones { get; } = new();

    public ObservableCollection<EnemyEntry> Enemies { get; } = new(AlarmService.LoadEnemies());
    public static SurvivalWaveSpawnType[] AvailableWaveSpawnTypes => Enum.GetValues<SurvivalWaveSpawnType>();
    public static WaveFilterType[] AvailableFilterTypes => Enum.GetValues<WaveFilterType>();
    public static WaveOnGotoWinTrigger[] AvailableWaveOnGotoWinTriggers => Enum.GetValues<WaveOnGotoWinTrigger>();
    public SurvivalWaveSpawnType[] WaveSpawnTypes => AvailableWaveSpawnTypes;
    public WaveFilterType[] FilterTypes => AvailableFilterTypes;
    public WaveOnGotoWinTrigger[] WaveOnGotoWinTriggers => AvailableWaveOnGotoWinTriggers;

    private ReactorStartupStage? _selectedReactorStage;
    private ReactorStartupStage? _copiedReactorStage;
    private GatherItemPlacement? _selectedGatherItemPlacement;
    private CandidateZonePlacement? _selectedGatherCandidateZonePlacement;
    private GatherItemPlacement? _selectedRetrieveItemPlacement;
    private CandidateZonePlacement? _selectedRetrieveCandidateZonePlacement;
    private DistributeGeneratorPlacement? _selectedDistributeGeneratorPlacement;
    private GatherItemPlacement? _selectedTerminalUplinkPlacement;
    private CandidateZonePlacement? _selectedTerminalUplinkCandidateZonePlacement;
    private CentralGeneratorFogTransitionStage? _selectedCentralGeneratorFogTransitionStage;
    private GatherItemPlacement? _selectedCentralGeneratorPowerCellPlacement;
    private CandidateZonePlacement? _selectedCentralGeneratorCandidateZonePlacement;
    private GatherItemPlacement? _selectedGatherTerminalPlacement;
    private CandidateZonePlacement? _selectedGatherTerminalCandidateZonePlacement;
    private GatherItemPlacement? _selectedTimedTerminalSequencePlacement;
    private CandidateZonePlacement? _selectedTimedTerminalSequenceCandidateZonePlacement;

    public ReactorStartupStage? SelectedReactorStage
    {
        get => _selectedReactorStage;
        set
        {
            if (_selectedReactorStage != null)
            {
                _selectedReactorStage.PropertyChanged -= SelectedReactorStage_PropertyChanged;
            }

            if (SetField(ref _selectedReactorStage, value))
            {
                if (_selectedReactorStage != null)
                {
                    _selectedReactorStage.PropertyChanged += SelectedReactorStage_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedReactorStage));
                OnPropertyChanged(nameof(SelectedStageVerificationZoneOption));
            }
        }
    }

    public bool HasSelectedReactorStage => _selectedReactorStage != null;
    public bool HasCopiedReactorStage => _copiedReactorStage != null;

    private void SelectedReactorStage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReactorStartupStage.ZoneForVerification))
        {
            OnPropertyChanged(nameof(SelectedStageVerificationZoneOption));
        }
    }

    public WinCondition SelectedSectorWinCondition
    {
        get => EditingSectorSettings?.WinCondition ?? WinCondition.GoToElevator;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.WinCondition != value)
            {
                EditingSectorSettings.WinCondition = value;
                OnPropertyChanged(nameof(SelectedSectorWinCondition));
                OnPropertyChanged(nameof(IsExitGeoWinCondition));
            }
        }
    }

    public bool IsExitGeoWinCondition =>
        (EditingSectorSettings?.WinCondition ?? WinCondition.GoToElevator) == WinCondition.GoToExitGeo;

    public ObjectiveType SelectedSectorObjectiveType
    {
        get => EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.ObjectiveType != value)
            {
                EditingSectorSettings.ObjectiveType = value;
                EditingSectorSettings.ApplyDefaultsForObjectiveType(value);
                OnPropertyChanged(nameof(SelectedSectorObjectiveType));
                OnPropertyChanged(nameof(IsHsuObjective));
                OnPropertyChanged(nameof(IsReactorObjective));
                OnPropertyChanged(nameof(IsReactorShutdownObjective));
                OnPropertyChanged(nameof(IsGatherSmallItemsObjective));
                OnPropertyChanged(nameof(IsClearAPathObjective));
                OnPropertyChanged(nameof(IsSpecialTerminalCommandObjective));
                OnPropertyChanged(nameof(IsRetrieveBigItemsObjective));
                OnPropertyChanged(nameof(IsDistributePowerCellsObjective));
                OnPropertyChanged(nameof(IsTerminalUplinkObjective));
                OnPropertyChanged(nameof(IsCentralGeneratorClusterObjective));
                OnPropertyChanged(nameof(IsActivateSmallHsuObjective));
                OnPropertyChanged(nameof(IsSurvivalObjective));
                OnPropertyChanged(nameof(IsGatherTerminalObjective));
                OnPropertyChanged(nameof(IsCorruptedTerminalUplinkObjective));
                OnPropertyChanged(nameof(IsTimedTerminalSequenceObjective));
                if (value == ObjectiveType.Reactor_Startup)
                {
                    if (EditingSectorSettings.ReactorStartupStages.Count == 0)
                    {
                        EditingSectorSettings.ReactorStartupStages.Add(new ReactorStartupStage());
                    }

                    UpdateStageNumbers();
                    if (SelectedReactorStage == null)
                    {
                        SelectedReactorStage = EditingSectorSettings.ReactorStartupStages.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.GatherSmallItems)
                {
                    if (EditingSectorSettings.GatherItemPlacements.Count == 0)
                    {
                        EditingSectorSettings.GatherItemPlacements.Add(new GatherItemPlacement());
                    }

                    RenumberGatherItemPlacements();
                    if (SelectedGatherItemPlacement == null)
                    {
                        SelectedGatherItemPlacement = EditingSectorSettings.GatherItemPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.RetrieveBigItems)
                {
                    if (EditingSectorSettings.RetrieveItemPlacements.Count == 0)
                    {
                        EditingSectorSettings.RetrieveItemPlacements.Add(new GatherItemPlacement());
                    }

                    RenumberRetrieveItemPlacements();
                    if (SelectedRetrieveItemPlacement == null)
                    {
                        SelectedRetrieveItemPlacement = EditingSectorSettings.RetrieveItemPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.DistributePowerCells)
                {
                    if (EditingSectorSettings.DistributeGeneratorPlacements.Count == 0)
                    {
                        EditingSectorSettings.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement());
                    }

                    RenumberDistributeGeneratorPlacements();
                    if (SelectedDistributeGeneratorPlacement == null)
                    {
                        SelectedDistributeGeneratorPlacement =
                            EditingSectorSettings.DistributeGeneratorPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.TerminalUplink)
                {
                    if (EditingSectorSettings.TerminalUplinkPlacements.Count == 0)
                    {
                        EditingSectorSettings.TerminalUplinkPlacements.Add(new GatherItemPlacement
                            { ItemNumber = 1, ItemPrefix = "Terminal" });
                    }

                    RenumberTerminalUplinkPlacements();
                    if (SelectedTerminalUplinkPlacement == null)
                    {
                        SelectedTerminalUplinkPlacement =
                            EditingSectorSettings.TerminalUplinkPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.CentralGeneratorCluster)
                {
                    if (EditingSectorSettings.CentralGeneratorClusterFogTransitions.Count == 0)
                    {
                        EditingSectorSettings.CentralGeneratorClusterGeneratorCount = 4;
                    }

                    RenumberCentralGeneratorFogTransitions();
                    if (SelectedCentralGeneratorFogTransitionStage == null)
                    {
                        SelectedCentralGeneratorFogTransitionStage =
                            EditingSectorSettings.CentralGeneratorClusterFogTransitions.FirstOrDefault();
                    }

                    if (EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count == 0)
                    {
                        EditingSectorSettings.CentralGeneratorClusterPowerCellCount = 4;
                    }

                    RenumberCentralGeneratorPowerCellPlacements();
                    if (SelectedCentralGeneratorPowerCellPlacement == null)
                    {
                        SelectedCentralGeneratorPowerCellPlacement = EditingSectorSettings
                            .CentralGeneratorClusterPowerCellPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.GatherTerminal)
                {
                    if (EditingSectorSettings.GatherTerminalPlacements.Count == 0)
                    {
                        EditingSectorSettings.GatherTerminalPlacements.Add(new GatherItemPlacement
                            { ItemNumber = 1, ItemPrefix = "Terminal" });
                    }

                    RenumberGatherTerminalPlacements();
                    if (SelectedGatherTerminalPlacement == null)
                    {
                        SelectedGatherTerminalPlacement =
                            EditingSectorSettings.GatherTerminalPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.CorruptedTerminalUplink)
                {
                    if (EditingSectorSettings.TerminalUplinkPlacements.Count == 0)
                    {
                        EditingSectorSettings.TerminalUplinkPlacements.Add(new GatherItemPlacement
                            { ItemNumber = 1, ItemPrefix = "Terminal" });
                    }

                    RenumberTerminalUplinkPlacements();
                    if (SelectedTerminalUplinkPlacement == null)
                    {
                        SelectedTerminalUplinkPlacement =
                            EditingSectorSettings.TerminalUplinkPlacements.FirstOrDefault();
                    }
                }
                else if (value == ObjectiveType.TimedTerminalSequence)
                {
                    if (EditingSectorSettings.TimedTerminalSequencePlacements.Count == 0)
                    {
                        EditingSectorSettings.TimedTerminalSequencePlacements.Add(new GatherItemPlacement
                            { ItemNumber = 1, ItemPrefix = "Terminal" });
                    }

                    RenumberTimedTerminalSequencePlacements();
                    if (SelectedTimedTerminalSequencePlacement == null)
                    {
                        SelectedTimedTerminalSequencePlacement =
                            EditingSectorSettings.TimedTerminalSequencePlacements.FirstOrDefault();
                    }
                }
            }
        }
    }

    public bool IsHsuObjective => (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
                                  ObjectiveType.HSU_FindTakeSample;

    public bool IsReactorObjective => (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
                                      ObjectiveType.Reactor_Startup;

    public bool IsReactorShutdownObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.Reactor_Shutdown;

    public bool IsGatherSmallItemsObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.GatherSmallItems;

    public bool IsClearAPathObjective => (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
                                         ObjectiveType.ClearAPath;

    public bool IsSpecialTerminalCommandObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
        ObjectiveType.SpecialTerminalCommand;

    public bool IsRetrieveBigItemsObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.RetrieveBigItems;

    public bool IsDistributePowerCellsObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
        ObjectiveType.DistributePowerCells;

    public bool IsTerminalUplinkObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.TerminalUplink ||
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.CorruptedTerminalUplink;

    public bool IsCentralGeneratorClusterObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
        ObjectiveType.CentralGeneratorCluster;

    public bool IsActivateSmallHsuObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.ActivateSmallHSU;

    public bool IsSurvivalObjective => (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) ==
                                       ObjectiveType.Survival;

    public bool IsGatherTerminalObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.GatherTerminal;

    public bool IsCorruptedTerminalUplinkObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.CorruptedTerminalUplink;

    public bool IsTimedTerminalSequenceObjective =>
        (EditingSectorSettings?.ObjectiveType ?? ObjectiveType.HSU_FindTakeSample) == ObjectiveType.TimedTerminalSequence;

    public ZoneOption? SelectedActivateSmallHsuZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (EditingSectorSettings?.ActivateSmallHsuZone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (EditingSectorSettings != null && value != null)
            {
                EditingSectorSettings.ActivateSmallHsuZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedActivateSmallHsuZoneOption));
            }
        }
    }

    public RetrieveBigItemOption? SelectedItemFromStartOption
    {
        get => AvailableRetrieveBigItemOptionsList.FirstOrDefault(o =>
                   o.Type == (EditingSectorSettings?.ItemFromStart ?? RetrieveBigItemType.PowerCell)) ??
               AvailableRetrieveBigItemOptionsList.FirstOrDefault();
        set
        {
            if (EditingSectorSettings != null && value != null)
            {
                EditingSectorSettings.ItemFromStart = value.Type;
                OnPropertyChanged(nameof(SelectedItemFromStartOption));
            }
        }
    }

    public RetrieveBigItemOption? SelectedItemAfterActivationOption
    {
        get => AvailableRetrieveBigItemOptionsList.FirstOrDefault(o =>
                   o.Type == (EditingSectorSettings?.ItemAfterActivation ?? RetrieveBigItemType.PowerCell)) ??
               AvailableRetrieveBigItemOptionsList.FirstOrDefault();
        set
        {
            if (EditingSectorSettings != null && value != null)
            {
                EditingSectorSettings.ItemAfterActivation = value.Type;
                OnPropertyChanged(nameof(SelectedItemAfterActivationOption));
            }
        }
    }

    public DistributeGeneratorPlacement? SelectedDistributeGeneratorPlacement
    {
        get => _selectedDistributeGeneratorPlacement;
        set
        {
            if (_selectedDistributeGeneratorPlacement != null)
            {
                _selectedDistributeGeneratorPlacement.PropertyChanged -=
                    SelectedDistributeGeneratorPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedDistributeGeneratorPlacement, value))
            {
                if (_selectedDistributeGeneratorPlacement != null)
                {
                    _selectedDistributeGeneratorPlacement.PropertyChanged +=
                        SelectedDistributeGeneratorPlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedDistributeGeneratorPlacement));
                OnPropertyChanged(nameof(SelectedDistributeGeneratorPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedDistributeGeneratorPlacement => _selectedDistributeGeneratorPlacement != null;

    private void SelectedDistributeGeneratorPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DistributeGeneratorPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedDistributeGeneratorPlacementZoneOption));
        }
    }

    public ZoneOption? SelectedDistributeGeneratorPlacementZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedDistributeGeneratorPlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedDistributeGeneratorPlacement != null &&
                SelectedDistributeGeneratorPlacement.Zone != value.ZoneIndex)
            {
                SelectedDistributeGeneratorPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedDistributeGeneratorPlacementZoneOption));
            }
        }
    }

    public int DistributeGeneratorCount
    {
        get => EditingSectorSettings?.DistributeGeneratorCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.DistributeGeneratorCount = value;
                RenumberDistributeGeneratorPlacements();
                if (SelectedDistributeGeneratorPlacement == null ||
                    !EditingSectorSettings.DistributeGeneratorPlacements.Contains(SelectedDistributeGeneratorPlacement))
                {
                    SelectedDistributeGeneratorPlacement =
                        EditingSectorSettings.DistributeGeneratorPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(DistributeGeneratorCount));
            }
        }
    }

    public void RenumberDistributeGeneratorPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.DistributeGeneratorPlacements.Count; i++)
        {
            EditingSectorSettings.DistributeGeneratorPlacements[i].GeneratorNumber = i + 1;
        }
    }

    public void AddDistributeGeneratorPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.DistributeGeneratorPlacements.Count + 1;
        var newPlacement = new DistributeGeneratorPlacement
        {
            GeneratorNumber = nextNum,
            Zone = 0
        };
        EditingSectorSettings.DistributeGeneratorPlacements.Add(newPlacement);
        SelectedDistributeGeneratorPlacement = newPlacement;
        OnPropertyChanged(nameof(DistributeGeneratorCount));
    }

    public void RemoveDistributeGeneratorPlacement(DistributeGeneratorPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.DistributeGeneratorPlacements.Count <= 1) return;
        placement ??= SelectedDistributeGeneratorPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.DistributeGeneratorPlacements.IndexOf(placement);
        EditingSectorSettings.DistributeGeneratorPlacements.Remove(placement);
        RenumberDistributeGeneratorPlacements();

        if (EditingSectorSettings.DistributeGeneratorPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.DistributeGeneratorPlacements.Count - 1);
            SelectedDistributeGeneratorPlacement = EditingSectorSettings.DistributeGeneratorPlacements[newIndex];
        }
        else
        {
            SelectedDistributeGeneratorPlacement = null;
        }

        OnPropertyChanged(nameof(DistributeGeneratorCount));
    }

    public int UplinkNumberOfTerminals
    {
        get => EditingSectorSettings?.UplinkNumberOfTerminals ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.UplinkNumberOfTerminals = value;
                RenumberTerminalUplinkPlacements();
                if (SelectedTerminalUplinkPlacement == null ||
                    !EditingSectorSettings.TerminalUplinkPlacements.Contains(SelectedTerminalUplinkPlacement))
                {
                    SelectedTerminalUplinkPlacement = EditingSectorSettings.TerminalUplinkPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(UplinkNumberOfTerminals));
            }
        }
    }

    public int UplinkNumberOfVerificationRounds
    {
        get => EditingSectorSettings?.UplinkNumberOfVerificationRounds ?? 1;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.UplinkNumberOfVerificationRounds != value)
            {
                EditingSectorSettings.UplinkNumberOfVerificationRounds = value;
                OnPropertyChanged(nameof(UplinkNumberOfVerificationRounds));
            }
        }
    }

    public GatherItemPlacement? SelectedTerminalUplinkPlacement
    {
        get => _selectedTerminalUplinkPlacement;
        set
        {
            if (_selectedTerminalUplinkPlacement != null)
            {
                _selectedTerminalUplinkPlacement.PropertyChanged -= SelectedTerminalUplinkPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedTerminalUplinkPlacement, value))
            {
                if (_selectedTerminalUplinkPlacement != null)
                {
                    _selectedTerminalUplinkPlacement.PropertyChanged += SelectedTerminalUplinkPlacement_PropertyChanged;
                }

                SelectedTerminalUplinkCandidateZonePlacement =
                    _selectedTerminalUplinkPlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedTerminalUplinkPlacement));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedTerminalUplinkPlacement => _selectedTerminalUplinkPlacement != null;

    private void SelectedTerminalUplinkPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedTerminalUplinkPlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedTerminalUplinkPlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedTerminalUplinkPlacement != null && !string.Equals(
                    SelectedTerminalUplinkPlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedTerminalUplinkPlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedTerminalUplinkPlacementZoneOption
    {
        get =>
            AvailableObjectiveZones.FirstOrDefault(z => z.ZoneIndex == (SelectedTerminalUplinkPlacement?.Zone ?? 0)) ??
            AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedTerminalUplinkPlacement != null &&
                SelectedTerminalUplinkPlacement.Zone != value.ZoneIndex)
            {
                SelectedTerminalUplinkPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
            }
        }
    }

    public CandidateZonePlacement? SelectedTerminalUplinkCandidateZonePlacement
    {
        get => _selectedTerminalUplinkCandidateZonePlacement;
        set
        {
            if (_selectedTerminalUplinkCandidateZonePlacement != null)
            {
                _selectedTerminalUplinkCandidateZonePlacement.PropertyChanged -=
                    SelectedTerminalUplinkCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedTerminalUplinkCandidateZonePlacement, value))
            {
                if (_selectedTerminalUplinkCandidateZonePlacement != null)
                {
                    _selectedTerminalUplinkCandidateZonePlacement.PropertyChanged +=
                        SelectedTerminalUplinkCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedTerminalUplinkCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedTerminalUplinkCandidateZonePlacement =>
        _selectedTerminalUplinkCandidateZonePlacement != null;

    private void SelectedTerminalUplinkCandidateZonePlacement_PropertyChanged(object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedTerminalUplinkCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedTerminalUplinkCandidateZonePlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedTerminalUplinkCandidateZonePlacement != null && !string.Equals(
                    SelectedTerminalUplinkCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedTerminalUplinkCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedTerminalUplinkCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedTerminalUplinkCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedTerminalUplinkCandidateZonePlacement != null &&
                SelectedTerminalUplinkCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedTerminalUplinkCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedTerminalUplinkCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedTerminalUplinkPlacementZoneOption));
            }
        }
    }

    public void RenumberTerminalUplinkPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.TerminalUplinkPlacements.Count; i++)
        {
            EditingSectorSettings.TerminalUplinkPlacements[i].ItemNumber = i + 1;
            EditingSectorSettings.TerminalUplinkPlacements[i].ItemPrefix = "Terminal";
        }
    }

    public void AddTerminalUplinkPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.TerminalUplinkPlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            ItemPrefix = "Terminal",
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.TerminalUplinkPlacements.Add(newPlacement);
        SelectedTerminalUplinkPlacement = newPlacement;
        OnPropertyChanged(nameof(UplinkNumberOfTerminals));
    }

    public void RemoveTerminalUplinkPlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.TerminalUplinkPlacements.Count <= 1) return;
        placement ??= SelectedTerminalUplinkPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.TerminalUplinkPlacements.IndexOf(placement);
        EditingSectorSettings.TerminalUplinkPlacements.Remove(placement);
        RenumberTerminalUplinkPlacements();

        if (EditingSectorSettings.TerminalUplinkPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.TerminalUplinkPlacements.Count - 1);
            SelectedTerminalUplinkPlacement = EditingSectorSettings.TerminalUplinkPlacements[newIndex];
        }
        else
        {
            SelectedTerminalUplinkPlacement = null;
        }

        OnPropertyChanged(nameof(UplinkNumberOfTerminals));
    }

    public void AddTerminalUplinkCandidateZone()
    {
        if (SelectedTerminalUplinkPlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedTerminalUplinkPlacement.CandidateZones.Add(newCandidate);
        SelectedTerminalUplinkCandidateZonePlacement = newCandidate;
    }

    public void RemoveTerminalUplinkCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedTerminalUplinkPlacement == null ||
            SelectedTerminalUplinkPlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedTerminalUplinkCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedTerminalUplinkPlacement.CandidateZones.IndexOf(candidate);
        SelectedTerminalUplinkPlacement.CandidateZones.Remove(candidate);
        if (SelectedTerminalUplinkPlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedTerminalUplinkPlacement.CandidateZones.Count - 1);
            SelectedTerminalUplinkCandidateZonePlacement = SelectedTerminalUplinkPlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedTerminalUplinkCandidateZonePlacement = null;
        }
    }

    public ZoneOption? SelectedCentralGeneratorClusterZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (EditingSectorSettings?.CentralGeneratorClusterZone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.CentralGeneratorClusterZone != value.ZoneIndex)
            {
                EditingSectorSettings.CentralGeneratorClusterZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedCentralGeneratorClusterZoneOption));
            }
        }
    }

    public int CentralGeneratorClusterGeneratorCount
    {
        get => EditingSectorSettings?.CentralGeneratorClusterGeneratorCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.CentralGeneratorClusterGeneratorCount = value;
                RenumberCentralGeneratorFogTransitions();
                if (SelectedCentralGeneratorFogTransitionStage == null ||
                    !EditingSectorSettings.CentralGeneratorClusterFogTransitions.Contains(
                        SelectedCentralGeneratorFogTransitionStage))
                {
                    SelectedCentralGeneratorFogTransitionStage =
                        EditingSectorSettings.CentralGeneratorClusterFogTransitions.FirstOrDefault();
                }

                OnPropertyChanged(nameof(CentralGeneratorClusterGeneratorCount));
            }
        }
    }

    public CentralGeneratorFogTransitionStage? SelectedCentralGeneratorFogTransitionStage
    {
        get => _selectedCentralGeneratorFogTransitionStage;
        set
        {
            if (_selectedCentralGeneratorFogTransitionStage != null)
            {
                _selectedCentralGeneratorFogTransitionStage.PropertyChanged -=
                    SelectedCentralGeneratorFogTransitionStage_PropertyChanged;
            }

            if (SetField(ref _selectedCentralGeneratorFogTransitionStage, value))
            {
                if (_selectedCentralGeneratorFogTransitionStage != null)
                {
                    _selectedCentralGeneratorFogTransitionStage.PropertyChanged +=
                        SelectedCentralGeneratorFogTransitionStage_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedCentralGeneratorFogTransitionStage));
                OnPropertyChanged(nameof(SelectedCentralGeneratorFogTransitionOption));
            }
        }
    }

    public bool HasSelectedCentralGeneratorFogTransitionStage => _selectedCentralGeneratorFogTransitionStage != null;

    private void SelectedCentralGeneratorFogTransitionStage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CentralGeneratorFogTransitionStage.FogSetting))
        {
            OnPropertyChanged(nameof(SelectedCentralGeneratorFogTransitionOption));
        }
    }

    public FogTransitionOption? SelectedCentralGeneratorFogTransitionOption
    {
        get => AvailableFogTransitionOptions.FirstOrDefault(f =>
                   f.PersistentId == (SelectedCentralGeneratorFogTransitionStage?.FogSetting ?? 0)) ??
               AvailableFogTransitionOptions.FirstOrDefault();
        set
        {
            if (value != null && SelectedCentralGeneratorFogTransitionStage != null &&
                SelectedCentralGeneratorFogTransitionStage.FogSetting != value.PersistentId)
            {
                SelectedCentralGeneratorFogTransitionStage.FogSetting = value.PersistentId;
                OnPropertyChanged(nameof(SelectedCentralGeneratorFogTransitionOption));
            }
        }
    }

    public void RenumberCentralGeneratorFogTransitions()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.CentralGeneratorClusterFogTransitions.Count; i++)
        {
            EditingSectorSettings.CentralGeneratorClusterFogTransitions[i].StageNumber = i + 1;
        }
    }

    public int CentralGeneratorClusterPowerCellCount
    {
        get => EditingSectorSettings?.CentralGeneratorClusterPowerCellCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.CentralGeneratorClusterPowerCellCount = value;
                RenumberCentralGeneratorPowerCellPlacements();
                if (SelectedCentralGeneratorPowerCellPlacement == null ||
                    !EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Contains(
                        SelectedCentralGeneratorPowerCellPlacement))
                {
                    SelectedCentralGeneratorPowerCellPlacement =
                        EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
            }
        }
    }

    public GatherItemPlacement? SelectedCentralGeneratorPowerCellPlacement
    {
        get => _selectedCentralGeneratorPowerCellPlacement;
        set
        {
            if (_selectedCentralGeneratorPowerCellPlacement != null)
            {
                _selectedCentralGeneratorPowerCellPlacement.PropertyChanged -=
                    SelectedCentralGeneratorPowerCellPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedCentralGeneratorPowerCellPlacement, value))
            {
                if (_selectedCentralGeneratorPowerCellPlacement != null)
                {
                    _selectedCentralGeneratorPowerCellPlacement.PropertyChanged +=
                        SelectedCentralGeneratorPowerCellPlacement_PropertyChanged;
                }

                SelectedCentralGeneratorCandidateZonePlacement =
                    _selectedCentralGeneratorPowerCellPlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedCentralGeneratorPowerCellPlacement));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedCentralGeneratorPowerCellPlacement => _selectedCentralGeneratorPowerCellPlacement != null;

    private void SelectedCentralGeneratorPowerCellPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedCentralGeneratorPowerCellPlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedCentralGeneratorPowerCellPlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedCentralGeneratorPowerCellPlacement != null && !string.Equals(
                    SelectedCentralGeneratorPowerCellPlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedCentralGeneratorPowerCellPlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedCentralGeneratorPowerCellPlacementZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedCentralGeneratorPowerCellPlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedCentralGeneratorPowerCellPlacement != null &&
                SelectedCentralGeneratorPowerCellPlacement.Zone != value.ZoneIndex)
            {
                SelectedCentralGeneratorPowerCellPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
            }
        }
    }

    public CandidateZonePlacement? SelectedCentralGeneratorCandidateZonePlacement
    {
        get => _selectedCentralGeneratorCandidateZonePlacement;
        set
        {
            if (_selectedCentralGeneratorCandidateZonePlacement != null)
            {
                _selectedCentralGeneratorCandidateZonePlacement.PropertyChanged -=
                    SelectedCentralGeneratorCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedCentralGeneratorCandidateZonePlacement, value))
            {
                if (_selectedCentralGeneratorCandidateZonePlacement != null)
                {
                    _selectedCentralGeneratorCandidateZonePlacement.PropertyChanged +=
                        SelectedCentralGeneratorCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedCentralGeneratorCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedCentralGeneratorCandidateZonePlacement =>
        _selectedCentralGeneratorCandidateZonePlacement != null;

    private void SelectedCentralGeneratorCandidateZonePlacement_PropertyChanged(object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedCentralGeneratorCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedCentralGeneratorCandidateZonePlacement?.DimensionIndex,
                   StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedCentralGeneratorCandidateZonePlacement != null && !string.Equals(
                    SelectedCentralGeneratorCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedCentralGeneratorCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedCentralGeneratorCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedCentralGeneratorCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedCentralGeneratorCandidateZonePlacement != null &&
                SelectedCentralGeneratorCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedCentralGeneratorCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedCentralGeneratorCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacementZoneOption));
            }
        }
    }

    public void RenumberCentralGeneratorPowerCellPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count; i++)
        {
            EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements[i].ItemNumber = i + 1;
            EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements[i].ItemPrefix = "Power Cell";
        }
    }

    public void AddCentralGeneratorPowerCellPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            ItemPrefix = "Power Cell",
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Add(newPlacement);
        SelectedCentralGeneratorPowerCellPlacement = newPlacement;
        OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
    }

    public void RemoveCentralGeneratorPowerCellPlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null ||
            EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count <= 1) return;
        placement ??= SelectedCentralGeneratorPowerCellPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.IndexOf(placement);
        EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Remove(placement);
        RenumberCentralGeneratorPowerCellPlacements();

        if (EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0,
                EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count - 1);
            SelectedCentralGeneratorPowerCellPlacement =
                EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements[newIndex];
        }
        else
        {
            SelectedCentralGeneratorPowerCellPlacement = null;
        }

        OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
    }

    public void AddCentralGeneratorCandidateZone()
    {
        if (SelectedCentralGeneratorPowerCellPlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedCentralGeneratorPowerCellPlacement.CandidateZones.Add(newCandidate);
        SelectedCentralGeneratorCandidateZonePlacement = newCandidate;
    }

    public void RemoveCentralGeneratorCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedCentralGeneratorPowerCellPlacement == null ||
            SelectedCentralGeneratorPowerCellPlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedCentralGeneratorCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedCentralGeneratorPowerCellPlacement.CandidateZones.IndexOf(candidate);
        SelectedCentralGeneratorPowerCellPlacement.CandidateZones.Remove(candidate);
        if (SelectedCentralGeneratorPowerCellPlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedCentralGeneratorPowerCellPlacement.CandidateZones.Count - 1);
            SelectedCentralGeneratorCandidateZonePlacement =
                SelectedCentralGeneratorPowerCellPlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedCentralGeneratorCandidateZonePlacement = null;
        }
    }

    public int GatherTerminalSpawnCount
    {
        get => EditingSectorSettings?.GatherTerminalSpawnCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.GatherTerminalSpawnCount = value;
                RenumberGatherTerminalPlacements();
                if (SelectedGatherTerminalPlacement == null ||
                    !EditingSectorSettings.GatherTerminalPlacements.Contains(SelectedGatherTerminalPlacement))
                {
                    SelectedGatherTerminalPlacement = EditingSectorSettings.GatherTerminalPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(GatherTerminalSpawnCount));
            }
        }
    }

    public GatherItemPlacement? SelectedGatherTerminalPlacement
    {
        get => _selectedGatherTerminalPlacement;
        set
        {
            if (_selectedGatherTerminalPlacement != null)
            {
                _selectedGatherTerminalPlacement.PropertyChanged -= SelectedGatherTerminalPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedGatherTerminalPlacement, value))
            {
                if (_selectedGatherTerminalPlacement != null)
                {
                    _selectedGatherTerminalPlacement.PropertyChanged += SelectedGatherTerminalPlacement_PropertyChanged;
                }

                SelectedGatherTerminalCandidateZonePlacement =
                    _selectedGatherTerminalPlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedGatherTerminalPlacement));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedGatherTerminalPlacement => _selectedGatherTerminalPlacement != null;

    private void SelectedGatherTerminalPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedGatherTerminalPlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedGatherTerminalPlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherTerminalPlacement != null && !string.Equals(
                    SelectedGatherTerminalPlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedGatherTerminalPlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedGatherTerminalPlacementZoneOption
    {
        get =>
            AvailableObjectiveZones.FirstOrDefault(z => z.ZoneIndex == (SelectedGatherTerminalPlacement?.Zone ?? 0)) ??
            AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherTerminalPlacement != null &&
                SelectedGatherTerminalPlacement.Zone != value.ZoneIndex)
            {
                SelectedGatherTerminalPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
            }
        }
    }

    public CandidateZonePlacement? SelectedGatherTerminalCandidateZonePlacement
    {
        get => _selectedGatherTerminalCandidateZonePlacement;
        set
        {
            if (_selectedGatherTerminalCandidateZonePlacement != null)
            {
                _selectedGatherTerminalCandidateZonePlacement.PropertyChanged -=
                    SelectedGatherTerminalCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedGatherTerminalCandidateZonePlacement, value))
            {
                if (_selectedGatherTerminalCandidateZonePlacement != null)
                {
                    _selectedGatherTerminalCandidateZonePlacement.PropertyChanged +=
                        SelectedGatherTerminalCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedGatherTerminalCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedGatherTerminalCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedGatherTerminalCandidateZonePlacement =>
        _selectedGatherTerminalCandidateZonePlacement != null;

    private void SelectedGatherTerminalCandidateZonePlacement_PropertyChanged(object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedGatherTerminalCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedGatherTerminalCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedGatherTerminalCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedGatherTerminalCandidateZonePlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherTerminalCandidateZonePlacement != null && !string.Equals(
                    SelectedGatherTerminalCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedGatherTerminalCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedGatherTerminalCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedGatherTerminalCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedGatherTerminalCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherTerminalCandidateZonePlacement != null &&
                SelectedGatherTerminalCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedGatherTerminalCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedGatherTerminalCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
            }
        }
    }

    public void RenumberGatherTerminalPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.GatherTerminalPlacements.Count; i++)
        {
            EditingSectorSettings.GatherTerminalPlacements[i].ItemNumber = i + 1;
            EditingSectorSettings.GatherTerminalPlacements[i].ItemPrefix = "Terminal";
        }
    }

    public void AddGatherTerminalPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.GatherTerminalPlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            ItemPrefix = "Terminal",
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.GatherTerminalPlacements.Add(newPlacement);
        SelectedGatherTerminalPlacement = newPlacement;
        OnPropertyChanged(nameof(GatherTerminalSpawnCount));
    }

    public void RemoveGatherTerminalPlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.GatherTerminalPlacements.Count <= 1) return;
        placement ??= SelectedGatherTerminalPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.GatherTerminalPlacements.IndexOf(placement);
        EditingSectorSettings.GatherTerminalPlacements.Remove(placement);
        RenumberGatherTerminalPlacements();

        if (EditingSectorSettings.GatherTerminalPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.GatherTerminalPlacements.Count - 1);
            SelectedGatherTerminalPlacement = EditingSectorSettings.GatherTerminalPlacements[newIndex];
        }
        else
        {
            SelectedGatherTerminalPlacement = null;
        }

        OnPropertyChanged(nameof(GatherTerminalSpawnCount));
    }

    public void AddGatherTerminalCandidateZone()
    {
        if (SelectedGatherTerminalPlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedGatherTerminalPlacement.CandidateZones.Add(newCandidate);
        SelectedGatherTerminalCandidateZonePlacement = newCandidate;
    }

    public void RemoveGatherTerminalCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedGatherTerminalPlacement == null ||
            SelectedGatherTerminalPlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedGatherTerminalCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedGatherTerminalPlacement.CandidateZones.IndexOf(candidate);
        SelectedGatherTerminalPlacement.CandidateZones.Remove(candidate);
        if (SelectedGatherTerminalPlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedGatherTerminalPlacement.CandidateZones.Count - 1);
            SelectedGatherTerminalCandidateZonePlacement =
                SelectedGatherTerminalPlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedGatherTerminalCandidateZonePlacement = null;
        }
    }

    public DimensionOption? SelectedTimedTerminalSequenceMainDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   EditingSectorSettings?.TimedTerminalSequenceMainLocation?.DimensionIndex,
                   StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null && !string.Equals(
                    EditingSectorSettings.TimedTerminalSequenceMainLocation.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditingSectorSettings.TimedTerminalSequenceMainLocation.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceMainDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedTimedTerminalSequenceMainZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (EditingSectorSettings?.TimedTerminalSequenceMainLocation?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.TimedTerminalSequenceMainLocation.Zone != value.ZoneIndex)
            {
                EditingSectorSettings.TimedTerminalSequenceMainLocation.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceMainZoneOption));
            }
        }
    }

    public int TimedTerminalSequenceNumberOfTerminals
    {
        get => EditingSectorSettings?.TimedTerminalSequenceNumberOfTerminals ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.TimedTerminalSequenceNumberOfTerminals = value;
                RenumberTimedTerminalSequencePlacements();
                if (SelectedTimedTerminalSequencePlacement == null ||
                    !EditingSectorSettings.TimedTerminalSequencePlacements.Contains(
                        SelectedTimedTerminalSequencePlacement))
                {
                    SelectedTimedTerminalSequencePlacement =
                        EditingSectorSettings.TimedTerminalSequencePlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
            }
        }
    }

    public GatherItemPlacement? SelectedTimedTerminalSequencePlacement
    {
        get => _selectedTimedTerminalSequencePlacement;
        set
        {
            if (_selectedTimedTerminalSequencePlacement != null)
            {
                _selectedTimedTerminalSequencePlacement.PropertyChanged -=
                    SelectedTimedTerminalSequencePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedTimedTerminalSequencePlacement, value))
            {
                if (_selectedTimedTerminalSequencePlacement != null)
                {
                    _selectedTimedTerminalSequencePlacement.PropertyChanged +=
                        SelectedTimedTerminalSequencePlacement_PropertyChanged;
                }

                SelectedTimedTerminalSequenceCandidateZonePlacement =
                    _selectedTimedTerminalSequencePlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedTimedTerminalSequencePlacement));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
            }
        }
    }

    public bool HasSelectedTimedTerminalSequencePlacement => _selectedTimedTerminalSequencePlacement != null;

    private void SelectedTimedTerminalSequencePlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
        }
    }

    public DimensionOption? SelectedTimedTerminalSequencePlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedTimedTerminalSequencePlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedTimedTerminalSequencePlacement != null && !string.Equals(
                    SelectedTimedTerminalSequencePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedTimedTerminalSequencePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedTimedTerminalSequencePlacementZoneOption
    {
        get =>
            AvailableObjectiveZones.FirstOrDefault(z =>
                z.ZoneIndex == (SelectedTimedTerminalSequencePlacement?.Zone ?? 0)) ??
            AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedTimedTerminalSequencePlacement != null &&
                SelectedTimedTerminalSequencePlacement.Zone != value.ZoneIndex)
            {
                SelectedTimedTerminalSequencePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
            }
        }
    }

    public CandidateZonePlacement? SelectedTimedTerminalSequenceCandidateZonePlacement
    {
        get => _selectedTimedTerminalSequenceCandidateZonePlacement;
        set
        {
            if (_selectedTimedTerminalSequenceCandidateZonePlacement != null)
            {
                _selectedTimedTerminalSequenceCandidateZonePlacement.PropertyChanged -=
                    SelectedTimedTerminalSequenceCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedTimedTerminalSequenceCandidateZonePlacement, value))
            {
                if (_selectedTimedTerminalSequenceCandidateZonePlacement != null)
                {
                    _selectedTimedTerminalSequenceCandidateZonePlacement.PropertyChanged +=
                        SelectedTimedTerminalSequenceCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedTimedTerminalSequenceCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
            }
        }
    }

    public bool HasSelectedTimedTerminalSequenceCandidateZonePlacement =>
        _selectedTimedTerminalSequenceCandidateZonePlacement != null;

    private void SelectedTimedTerminalSequenceCandidateZonePlacement_PropertyChanged(object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
        }
    }

    public DimensionOption? SelectedTimedTerminalSequenceCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedTimedTerminalSequenceCandidateZonePlacement?.DimensionIndex,
                   StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedTimedTerminalSequenceCandidateZonePlacement != null && !string.Equals(
                    SelectedTimedTerminalSequenceCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedTimedTerminalSequenceCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedTimedTerminalSequenceCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedTimedTerminalSequenceCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedTimedTerminalSequenceCandidateZonePlacement != null &&
                SelectedTimedTerminalSequenceCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedTimedTerminalSequenceCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
            }
        }
    }

    public void RenumberTimedTerminalSequencePlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.TimedTerminalSequencePlacements.Count; i++)
        {
            EditingSectorSettings.TimedTerminalSequencePlacements[i].ItemNumber = i + 1;
            EditingSectorSettings.TimedTerminalSequencePlacements[i].ItemPrefix = "Terminal";
        }
    }

    public void AddTimedTerminalSequencePlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.TimedTerminalSequencePlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            ItemPrefix = "Terminal",
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.TimedTerminalSequencePlacements.Add(newPlacement);
        SelectedTimedTerminalSequencePlacement = newPlacement;
        OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
    }

    public void RemoveTimedTerminalSequencePlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.TimedTerminalSequencePlacements.Count <= 1) return;
        placement ??= SelectedTimedTerminalSequencePlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.TimedTerminalSequencePlacements.IndexOf(placement);
        EditingSectorSettings.TimedTerminalSequencePlacements.Remove(placement);
        RenumberTimedTerminalSequencePlacements();

        if (EditingSectorSettings.TimedTerminalSequencePlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.TimedTerminalSequencePlacements.Count - 1);
            SelectedTimedTerminalSequencePlacement = EditingSectorSettings.TimedTerminalSequencePlacements[newIndex];
        }
        else
        {
            SelectedTimedTerminalSequencePlacement = null;
        }

        OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
    }

    public void AddTimedTerminalSequenceCandidateZone()
    {
        if (SelectedTimedTerminalSequencePlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedTimedTerminalSequencePlacement.CandidateZones.Add(newCandidate);
        SelectedTimedTerminalSequenceCandidateZonePlacement = newCandidate;
    }

    public void RemoveTimedTerminalSequenceCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedTimedTerminalSequencePlacement == null ||
            SelectedTimedTerminalSequencePlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedTimedTerminalSequenceCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedTimedTerminalSequencePlacement.CandidateZones.IndexOf(candidate);
        SelectedTimedTerminalSequencePlacement.CandidateZones.Remove(candidate);
        if (SelectedTimedTerminalSequencePlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedTimedTerminalSequencePlacement.CandidateZones.Count - 1);
            SelectedTimedTerminalSequenceCandidateZonePlacement =
                SelectedTimedTerminalSequencePlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedTimedTerminalSequenceCandidateZonePlacement = null;
        }
    }

    public ObservableCollection<SpecialCommandRule> AvailableSpecialCommandRules { get; } =
        new(Enum.GetValues<SpecialCommandRule>());

    public SpecialCommandRule[] SpecialCommandRules => Enum.GetValues<SpecialCommandRule>();

    public ZoneOption? SelectedSpecialTerminalCommandZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (EditingSectorSettings?.SpecialTerminalCommandZone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.SpecialTerminalCommandZone != value.ZoneIndex)
            {
                EditingSectorSettings.SpecialTerminalCommandZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedSpecialTerminalCommandZoneOption));
            }
        }
    }

    public SpecialCommandRule SelectedSpecialCommandRule
    {
        get => EditingSectorSettings?.SpecialCommandRule ?? SpecialCommandRule.Normal;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.SpecialCommandRule != value)
            {
                EditingSectorSettings.SpecialCommandRule = value;
                OnPropertyChanged(nameof(SelectedSpecialCommandRule));
            }
        }
    }

    public static List<GatherSmallItemOption> AvailableGatherSmallItemOptionsList { get; } = new()
    {
        new(GatherSmallItemType.PersonnelID, "PersonnelID (128)"),
        new(GatherSmallItemType.PartialDecoder, "PartialDecoder (129)"),
        new(GatherSmallItemType.HardDrive, "HardDrive (147)"),
        new(GatherSmallItemType.Microdrive, "Microdrive (183)"),
        new(GatherSmallItemType.GLP_1, "GLP-1 (149)"),
        new(GatherSmallItemType.OSIP, "OSIP (150)"),
        new(GatherSmallItemType.GLP_2, "GLP-2 (169)"),
        new(GatherSmallItemType.DataCubeTampered, "DataCubeTampered (168)"),
        new(GatherSmallItemType.DataCube, "DataCube (165)"),
        new(GatherSmallItemType.DataCubes, "DataCubes (179)"),
        new(GatherSmallItemType.PlantSample, "PlantSample (153)"),
        new(GatherSmallItemType.MemoryStick, "MemoryStick (171)")
    };

    public List<GatherSmallItemOption> AvailableGatherSmallItemOptions => AvailableGatherSmallItemOptionsList;
    public List<GatherSmallItemOption> GatherSmallItemOptions => AvailableGatherSmallItemOptionsList;

    public GatherSmallItemOption? SelectedGatherSmallItemOption
    {
        get => AvailableGatherSmallItemOptions.FirstOrDefault(o =>
                   o.Type == (EditingSectorSettings?.GatherItem ?? GatherSmallItemType.PersonnelID)) ??
               AvailableGatherSmallItemOptions.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null && EditingSectorSettings.GatherItem != value.Type)
            {
                EditingSectorSettings.GatherItem = value.Type;
                OnPropertyChanged(nameof(SelectedGatherSmallItemOption));
                OnPropertyChanged(nameof(SelectedGatherItem));
            }
        }
    }

    public GatherSmallItemType SelectedGatherItem
    {
        get => EditingSectorSettings?.GatherItem ?? GatherSmallItemType.PersonnelID;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.GatherItem != value)
            {
                EditingSectorSettings.GatherItem = value;
                OnPropertyChanged(nameof(SelectedGatherItem));
                OnPropertyChanged(nameof(SelectedGatherSmallItemOption));
            }
        }
    }

    public int GatherMaxPerZone
    {
        get => EditingSectorSettings?.GatherMaxPerZone ?? 0;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.GatherMaxPerZone != value)
            {
                EditingSectorSettings.GatherMaxPerZone = Math.Max(0, value);
                OnPropertyChanged(nameof(GatherMaxPerZone));
            }
        }
    }

    public int GatherSpawnCount
    {
        get => EditingSectorSettings?.GatherSpawnCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.GatherSpawnCount = value;
                RenumberGatherItemPlacements();
                if (SelectedGatherItemPlacement == null ||
                    !EditingSectorSettings.GatherItemPlacements.Contains(SelectedGatherItemPlacement))
                {
                    SelectedGatherItemPlacement = EditingSectorSettings.GatherItemPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(GatherSpawnCount));
            }
        }
    }

    public GatherItemPlacement? SelectedGatherItemPlacement
    {
        get => _selectedGatherItemPlacement;
        set
        {
            if (_selectedGatherItemPlacement != null)
            {
                _selectedGatherItemPlacement.PropertyChanged -= SelectedGatherItemPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedGatherItemPlacement, value))
            {
                if (_selectedGatherItemPlacement != null)
                {
                    _selectedGatherItemPlacement.PropertyChanged += SelectedGatherItemPlacement_PropertyChanged;
                }

                SelectedGatherCandidateZonePlacement = _selectedGatherItemPlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedGatherItemPlacement));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedGatherItemPlacement => _selectedGatherItemPlacement != null;

    public CandidateZonePlacement? SelectedGatherCandidateZonePlacement
    {
        get => _selectedGatherCandidateZonePlacement;
        set
        {
            if (_selectedGatherCandidateZonePlacement != null)
            {
                _selectedGatherCandidateZonePlacement.PropertyChanged -=
                    SelectedGatherCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedGatherCandidateZonePlacement, value))
            {
                if (_selectedGatherCandidateZonePlacement != null)
                {
                    _selectedGatherCandidateZonePlacement.PropertyChanged +=
                        SelectedGatherCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedGatherCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedGatherCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedGatherCandidateZonePlacement => _selectedGatherCandidateZonePlacement != null;

    private void SelectedGatherCandidateZonePlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedGatherCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedGatherCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedGatherCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedGatherCandidateZonePlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherCandidateZonePlacement != null && !string.Equals(
                    SelectedGatherCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedGatherCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedGatherCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedGatherCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedGatherCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherCandidateZonePlacement != null &&
                SelectedGatherCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedGatherCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedGatherCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
            }
        }
    }

    public void AddGatherCandidateZone()
    {
        if (SelectedGatherItemPlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedGatherItemPlacement.CandidateZones.Add(newCandidate);
        SelectedGatherCandidateZonePlacement = newCandidate;
    }

    public void RemoveGatherCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedGatherItemPlacement == null || SelectedGatherItemPlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedGatherCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedGatherItemPlacement.CandidateZones.IndexOf(candidate);
        SelectedGatherItemPlacement.CandidateZones.Remove(candidate);
        if (SelectedGatherItemPlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedGatherItemPlacement.CandidateZones.Count - 1);
            SelectedGatherCandidateZonePlacement = SelectedGatherItemPlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedGatherCandidateZonePlacement = null;
        }
    }

    private void SelectedGatherItemPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedGatherItemPlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedGatherItemPlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherItemPlacement != null && !string.Equals(
                    SelectedGatherItemPlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedGatherItemPlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedGatherItemPlacementZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z => z.ZoneIndex == (SelectedGatherItemPlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedGatherItemPlacement != null &&
                SelectedGatherItemPlacement.Zone != value.ZoneIndex)
            {
                SelectedGatherItemPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
            }
        }
    }

    public void RenumberGatherItemPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.GatherItemPlacements.Count; i++)
        {
            EditingSectorSettings.GatherItemPlacements[i].ItemNumber = i + 1;
        }
    }

    public void AddGatherItemPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.GatherItemPlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.GatherItemPlacements.Add(newPlacement);
        SelectedGatherItemPlacement = newPlacement;
        OnPropertyChanged(nameof(GatherSpawnCount));
    }

    public void RemoveGatherItemPlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.GatherItemPlacements.Count <= 1) return;
        placement ??= SelectedGatherItemPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.GatherItemPlacements.IndexOf(placement);
        EditingSectorSettings.GatherItemPlacements.Remove(placement);
        RenumberGatherItemPlacements();

        if (EditingSectorSettings.GatherItemPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.GatherItemPlacements.Count - 1);
            SelectedGatherItemPlacement = EditingSectorSettings.GatherItemPlacements[newIndex];
        }
        else
        {
            SelectedGatherItemPlacement = null;
        }

        OnPropertyChanged(nameof(GatherSpawnCount));
    }

    public static List<RetrieveBigItemOption> AvailableRetrieveBigItemOptionsList { get; } = new()
    {
        new(RetrieveBigItemType.PowerCell, "PowerCell (131)"),
        new(RetrieveBigItemType.FogTurbine, "FogTurbine (133)"),
        new(RetrieveBigItemType.HSUNeonateStage1, "HSUNeonateStage1 (137)"),
        new(RetrieveBigItemType.HSUNeonateStage2, "HSUNeonateStage2 (141)"),
        new(RetrieveBigItemType.HSUNeonateStage3, "HSUNeonateStage3 (143)"),
        new(RetrieveBigItemType.HSUNeonateStage4, "HSUNeonateStage4 (145)"),
        new(RetrieveBigItemType.HSUNeonateStage5, "HSUNeonateStage5 (170)"),
        new(RetrieveBigItemType.MWP, "MWP (164)"),
        new(RetrieveBigItemType.DataSphere, "DataSphere (151)"),
        new(RetrieveBigItemType.PortalKey, "PortalKey (166)"),
        new(RetrieveBigItemType.CargoCrate, "CargoCrate (138)"),
        new(RetrieveBigItemType.CryoHardcase, "CryoHardcase (148)"),
        new(RetrieveBigItemType.CargoCrateHighSecurity, "CargoCrateHighSecurity (154)"),
        new(RetrieveBigItemType.CargoCrateHighSecurityOpen, "CargoCrateHighSecurityOpen (155)"),
        new(RetrieveBigItemType.SedativesCase, "SedativesCase (173)")
    };

    public List<RetrieveBigItemOption> AvailableRetrieveBigItemOptions => AvailableRetrieveBigItemOptionsList;
    public List<RetrieveBigItemOption> RetrieveBigItemOptions => AvailableRetrieveBigItemOptionsList;

    public RetrieveBigItemOption? SelectedRetrieveBigItemOption
    {
        get => AvailableRetrieveBigItemOptions.FirstOrDefault(o =>
                   o.Type == (EditingSectorSettings?.RetrieveItem ?? RetrieveBigItemType.PowerCell)) ??
               AvailableRetrieveBigItemOptions.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null && EditingSectorSettings.RetrieveItem != value.Type)
            {
                EditingSectorSettings.RetrieveItem = value.Type;
                OnPropertyChanged(nameof(SelectedRetrieveBigItemOption));
                OnPropertyChanged(nameof(SelectedRetrieveItem));
            }
        }
    }

    public RetrieveBigItemType SelectedRetrieveItem
    {
        get => EditingSectorSettings?.RetrieveItem ?? RetrieveBigItemType.PowerCell;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.RetrieveItem != value)
            {
                EditingSectorSettings.RetrieveItem = value;
                OnPropertyChanged(nameof(SelectedRetrieveItem));
                OnPropertyChanged(nameof(SelectedRetrieveBigItemOption));
            }
        }
    }

    public int RetrieveMaxPerZone
    {
        get => EditingSectorSettings?.RetrieveMaxPerZone ?? 0;
        set
        {
            if (EditingSectorSettings != null && EditingSectorSettings.RetrieveMaxPerZone != value)
            {
                EditingSectorSettings.RetrieveMaxPerZone = Math.Max(0, value);
                OnPropertyChanged(nameof(RetrieveMaxPerZone));
            }
        }
    }

    public int RetrieveSpawnCount
    {
        get => EditingSectorSettings?.RetrieveSpawnCount ?? 1;
        set
        {
            if (EditingSectorSettings != null && value >= 1)
            {
                EditingSectorSettings.RetrieveSpawnCount = value;
                RenumberRetrieveItemPlacements();
                if (SelectedRetrieveItemPlacement == null ||
                    !EditingSectorSettings.RetrieveItemPlacements.Contains(SelectedRetrieveItemPlacement))
                {
                    SelectedRetrieveItemPlacement = EditingSectorSettings.RetrieveItemPlacements.FirstOrDefault();
                }

                OnPropertyChanged(nameof(RetrieveSpawnCount));
            }
        }
    }

    public GatherItemPlacement? SelectedRetrieveItemPlacement
    {
        get => _selectedRetrieveItemPlacement;
        set
        {
            if (_selectedRetrieveItemPlacement != null)
            {
                _selectedRetrieveItemPlacement.PropertyChanged -= SelectedRetrieveItemPlacement_PropertyChanged;
            }

            if (SetField(ref _selectedRetrieveItemPlacement, value))
            {
                if (_selectedRetrieveItemPlacement != null)
                {
                    _selectedRetrieveItemPlacement.PropertyChanged += SelectedRetrieveItemPlacement_PropertyChanged;
                }

                SelectedRetrieveCandidateZonePlacement =
                    _selectedRetrieveItemPlacement?.CandidateZones.FirstOrDefault();
                OnPropertyChanged(nameof(HasSelectedRetrieveItemPlacement));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedRetrieveItemPlacement => _selectedRetrieveItemPlacement != null;

    public CandidateZonePlacement? SelectedRetrieveCandidateZonePlacement
    {
        get => _selectedRetrieveCandidateZonePlacement;
        set
        {
            if (_selectedRetrieveCandidateZonePlacement != null)
            {
                _selectedRetrieveCandidateZonePlacement.PropertyChanged -=
                    SelectedRetrieveCandidateZonePlacement_PropertyChanged;
            }

            if (SetField(ref _selectedRetrieveCandidateZonePlacement, value))
            {
                if (_selectedRetrieveCandidateZonePlacement != null)
                {
                    _selectedRetrieveCandidateZonePlacement.PropertyChanged +=
                        SelectedRetrieveCandidateZonePlacement_PropertyChanged;
                }

                OnPropertyChanged(nameof(HasSelectedRetrieveCandidateZonePlacement));
                OnPropertyChanged(nameof(SelectedRetrieveCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedRetrieveCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
            }
        }
    }

    public bool HasSelectedRetrieveCandidateZonePlacement => _selectedRetrieveCandidateZonePlacement != null;

    private void SelectedRetrieveCandidateZonePlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CandidateZonePlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedRetrieveCandidateDimensionOption));
            OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(CandidateZonePlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedRetrieveCandidateZoneOption));
            OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedRetrieveCandidateDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedRetrieveCandidateZonePlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedRetrieveCandidateZonePlacement != null && !string.Equals(
                    SelectedRetrieveCandidateZonePlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedRetrieveCandidateZonePlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedRetrieveCandidateDimensionOption));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedRetrieveCandidateZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedRetrieveCandidateZonePlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedRetrieveCandidateZonePlacement != null &&
                SelectedRetrieveCandidateZonePlacement.Zone != value.ZoneIndex)
            {
                SelectedRetrieveCandidateZonePlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedRetrieveCandidateZoneOption));
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
            }
        }
    }

    public void AddRetrieveCandidateZone()
    {
        if (SelectedRetrieveItemPlacement == null) return;
        var newCandidate = new CandidateZonePlacement
        {
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        SelectedRetrieveItemPlacement.CandidateZones.Add(newCandidate);
        SelectedRetrieveCandidateZonePlacement = newCandidate;
    }

    public void RemoveRetrieveCandidateZone(CandidateZonePlacement? candidate = null)
    {
        if (SelectedRetrieveItemPlacement == null || SelectedRetrieveItemPlacement.CandidateZones.Count <= 1) return;
        candidate ??= SelectedRetrieveCandidateZonePlacement;
        if (candidate == null) return;
        int index = SelectedRetrieveItemPlacement.CandidateZones.IndexOf(candidate);
        SelectedRetrieveItemPlacement.CandidateZones.Remove(candidate);
        if (SelectedRetrieveItemPlacement.CandidateZones.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, SelectedRetrieveItemPlacement.CandidateZones.Count - 1);
            SelectedRetrieveCandidateZonePlacement = SelectedRetrieveItemPlacement.CandidateZones[newIndex];
        }
        else
        {
            SelectedRetrieveCandidateZonePlacement = null;
        }
    }

    private void SelectedRetrieveItemPlacement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GatherItemPlacement.DimensionIndex))
        {
            OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
        }
        else if (e.PropertyName == nameof(GatherItemPlacement.Zone))
        {
            OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
        }
    }

    public DimensionOption? SelectedRetrieveItemPlacementDimensionOption
    {
        get => AvailableDimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex,
                   SelectedRetrieveItemPlacement?.DimensionIndex, StringComparison.OrdinalIgnoreCase)) ??
               AvailableDimensions.FirstOrDefault();
        set
        {
            if (value != null && SelectedRetrieveItemPlacement != null && !string.Equals(
                    SelectedRetrieveItemPlacement.DimensionIndex, value.DimensionIndex,
                    StringComparison.OrdinalIgnoreCase))
            {
                SelectedRetrieveItemPlacement.DimensionIndex = value.DimensionIndex;
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
            }
        }
    }

    public ZoneOption? SelectedRetrieveItemPlacementZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z => z.ZoneIndex == (SelectedRetrieveItemPlacement?.Zone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedRetrieveItemPlacement != null &&
                SelectedRetrieveItemPlacement.Zone != value.ZoneIndex)
            {
                SelectedRetrieveItemPlacement.Zone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
            }
        }
    }

    public void RenumberRetrieveItemPlacements()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.RetrieveItemPlacements.Count; i++)
        {
            EditingSectorSettings.RetrieveItemPlacements[i].ItemNumber = i + 1;
        }
    }

    public void AddRetrieveItemPlacement()
    {
        if (EditingSectorSettings == null) return;
        int nextNum = EditingSectorSettings.RetrieveItemPlacements.Count + 1;
        var newPlacement = new GatherItemPlacement
        {
            ItemNumber = nextNum,
            DimensionIndex = "Reality",
            Zone = 0,
            PlacementStartWeight = 0.0,
            PlacementMiddleWeight = 0.0,
            PlacementEndWeight = 0.0
        };
        EditingSectorSettings.RetrieveItemPlacements.Add(newPlacement);
        SelectedRetrieveItemPlacement = newPlacement;
        OnPropertyChanged(nameof(RetrieveSpawnCount));
    }

    public void RemoveRetrieveItemPlacement(GatherItemPlacement? placement = null)
    {
        if (EditingSectorSettings == null || EditingSectorSettings.RetrieveItemPlacements.Count <= 1) return;
        placement ??= SelectedRetrieveItemPlacement;
        if (placement == null) return;

        int index = EditingSectorSettings.RetrieveItemPlacements.IndexOf(placement);
        EditingSectorSettings.RetrieveItemPlacements.Remove(placement);
        RenumberRetrieveItemPlacements();

        if (EditingSectorSettings.RetrieveItemPlacements.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.RetrieveItemPlacements.Count - 1);
            SelectedRetrieveItemPlacement = EditingSectorSettings.RetrieveItemPlacements[newIndex];
        }
        else
        {
            SelectedRetrieveItemPlacement = null;
        }

        OnPropertyChanged(nameof(RetrieveSpawnCount));
    }

    public AlarmBlock? SelectedAlarmBeforeShutdownOption
    {
        get =>
            AvailableAlarms.FirstOrDefault(a =>
                a.PersistentId == (EditingSectorSettings?.ChainedPuzzleToActive ?? 0)) ??
            AvailableAlarms.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.ChainedPuzzleToActive != value.PersistentId)
            {
                EditingSectorSettings.ChainedPuzzleToActive = value.PersistentId;
                OnPropertyChanged(nameof(SelectedAlarmBeforeShutdownOption));
            }
        }
    }

    public AlarmBlock? SelectedAlarmAfterShutdownOption
    {
        get => AvailableAlarms.FirstOrDefault(a =>
                   a.PersistentId == (EditingSectorSettings?.ChainedPuzzleMidObjective ?? 0)) ??
               AvailableAlarms.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.ChainedPuzzleMidObjective != value.PersistentId)
            {
                EditingSectorSettings.ChainedPuzzleMidObjective = value.PersistentId;
                OnPropertyChanged(nameof(SelectedAlarmAfterShutdownOption));
            }
        }
    }

    public ZoneOption? SelectedExitZoneOption
    {
        get => AvailableMainExitZones.FirstOrDefault(z => z.ZoneIndex == (EditingSectorSettings?.ExitZone ?? 0)) ??
               AvailableMainExitZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null && EditingSectorSettings.ExitZone != value.ZoneIndex)
            {
                EditingSectorSettings.ExitZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedExitZoneOption));
            }
        }
    }

    public ZoneOption? SelectedHsuPlacementZoneOption
    {
        get =>
            AvailableObjectiveZones.FirstOrDefault(z =>
                z.ZoneIndex == (EditingSectorSettings?.HsuPlacementZone ?? 0)) ??
            AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.HsuPlacementZone != value.ZoneIndex)
            {
                EditingSectorSettings.HsuPlacementZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedHsuPlacementZoneOption));
            }
        }
    }

    public ZoneOption? SelectedReactorPlacementZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (EditingSectorSettings?.ReactorPlacementZone ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && EditingSectorSettings != null &&
                EditingSectorSettings.ReactorPlacementZone != value.ZoneIndex)
            {
                EditingSectorSettings.ReactorPlacementZone = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedReactorPlacementZoneOption));
            }
        }
    }

    public ZoneOption? SelectedStageVerificationZoneOption
    {
        get => AvailableObjectiveZones.FirstOrDefault(z =>
                   z.ZoneIndex == (SelectedReactorStage?.ZoneForVerification ?? 0)) ??
               AvailableObjectiveZones.FirstOrDefault();
        set
        {
            if (value != null && SelectedReactorStage != null &&
                SelectedReactorStage.ZoneForVerification != value.ZoneIndex)
            {
                SelectedReactorStage.ZoneForVerification = value.ZoneIndex;
                OnPropertyChanged(nameof(SelectedStageVerificationZoneOption));
            }
        }
    }

    public void UpdateStageNumbers()
    {
        if (EditingSectorSettings == null) return;
        for (int i = 0; i < EditingSectorSettings.ReactorStartupStages.Count; i++)
        {
            EditingSectorSettings.ReactorStartupStages[i].StageNumber = i + 1;
        }
    }

    public void AddReactorStage()
    {
        if (EditingSectorSettings == null) return;
        var newStage = new ReactorStartupStage();
        EditingSectorSettings.ReactorStartupStages.Add(newStage);
        UpdateStageNumbers();
        SelectedReactorStage = newStage;
    }

    public void RemoveReactorStage(ReactorStartupStage? stage = null)
    {
        if (EditingSectorSettings == null) return;
        stage ??= SelectedReactorStage;
        if (stage == null) return;

        int index = EditingSectorSettings.ReactorStartupStages.IndexOf(stage);
        EditingSectorSettings.ReactorStartupStages.Remove(stage);
        UpdateStageNumbers();

        if (EditingSectorSettings.ReactorStartupStages.Count > 0)
        {
            int newIndex = Math.Clamp(index, 0, EditingSectorSettings.ReactorStartupStages.Count - 1);
            SelectedReactorStage = EditingSectorSettings.ReactorStartupStages[newIndex];
        }
        else
        {
            SelectedReactorStage = null;
        }
    }

    public void MoveReactorStageUp(ReactorStartupStage? stage = null)
    {
        if (EditingSectorSettings == null) return;
        stage ??= SelectedReactorStage;
        if (stage == null) return;

        int index = EditingSectorSettings.ReactorStartupStages.IndexOf(stage);
        if (index > 0)
        {
            EditingSectorSettings.ReactorStartupStages.Move(index, index - 1);
            UpdateStageNumbers();
            SelectedReactorStage = stage;
        }
    }

    public void MoveReactorStageDown(ReactorStartupStage? stage = null)
    {
        if (EditingSectorSettings == null) return;
        stage ??= SelectedReactorStage;
        if (stage == null) return;

        int index = EditingSectorSettings.ReactorStartupStages.IndexOf(stage);
        if (index >= 0 && index < EditingSectorSettings.ReactorStartupStages.Count - 1)
        {
            EditingSectorSettings.ReactorStartupStages.Move(index, index + 1);
            UpdateStageNumbers();
            SelectedReactorStage = stage;
        }
    }

    public void CopyReactorStage(ReactorStartupStage? stage = null)
    {
        stage ??= SelectedReactorStage;
        if (stage == null) return;

        _copiedReactorStage = stage.Clone();
        OnPropertyChanged(nameof(HasCopiedReactorStage));
    }

    public void PasteReactorStage()
    {
        if (EditingSectorSettings == null || _copiedReactorStage == null) return;

        var pasted = _copiedReactorStage.Clone();
        int index = SelectedReactorStage != null
            ? EditingSectorSettings.ReactorStartupStages.IndexOf(SelectedReactorStage) + 1
            : EditingSectorSettings.ReactorStartupStages.Count;

        if (index < 0 || index > EditingSectorSettings.ReactorStartupStages.Count)
        {
            index = EditingSectorSettings.ReactorStartupStages.Count;
        }

        EditingSectorSettings.ReactorStartupStages.Insert(index, pasted);
        UpdateStageNumbers();
        SelectedReactorStage = pasted;
    }

    private void PopulateSectorZoneOptions()
    {
        AvailableMainExitZones.Clear();
        if (SelectedLevel != null)
        {
            var mainZones = GridState.GetZonesForSector(SectorType.Main);
            if (mainZones.Count == 0) mainZones.Add(0);
            foreach (int z in mainZones)
            {
                AvailableMainExitZones.Add(new ZoneOption
                {
                    ZoneIndex = z,
                    DisplayName = SelectedLevel.MainSector.GetZoneDisplayName(z)
                });
            }
        }

        AvailableObjectiveZones.Clear();
        if (SelectedLevel != null && EditingSectorSettings != null)
        {
            var sectorZones = GridState.GetZonesForSector(EditingSectorType);
            if (sectorZones.Count == 0) sectorZones.Add(0);
            foreach (int z in sectorZones)
            {
                AvailableObjectiveZones.Add(new ZoneOption
                {
                    ZoneIndex = z,
                    DisplayName = EditingSectorSettings.GetZoneDisplayName(z)
                });
            }
        }

        OnPropertyChanged(nameof(SelectedExitZoneOption));
        OnPropertyChanged(nameof(SelectedHsuPlacementZoneOption));
        OnPropertyChanged(nameof(SelectedReactorPlacementZoneOption));
        OnPropertyChanged(nameof(SelectedStageVerificationZoneOption));
        OnPropertyChanged(nameof(SelectedSpecialTerminalCommandZoneOption));
        OnPropertyChanged(nameof(SelectedActivateSmallHsuZoneOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequenceMainZoneOption));
        OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
        OnPropertyChanged(nameof(SelectedGatherTerminalCandidateZoneOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequenceCandidateZoneOption));
    }

    private void EditingSectorSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SectorSettings.ZoneAliasStart))
        {
            PopulateSectorZoneOptions();
            OnPropertyChanged(nameof(EditingRoomZoneDisplayName));
        }
        else if (e.PropertyName == nameof(SectorSettings.WinCondition))
        {
            OnPropertyChanged(nameof(SelectedSectorWinCondition));
            OnPropertyChanged(nameof(IsExitGeoWinCondition));
        }
        else if (e.PropertyName == nameof(SectorSettings.ExitZone))
        {
            OnPropertyChanged(nameof(SelectedExitZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.ObjectiveType))
        {
            OnPropertyChanged(nameof(SelectedSectorObjectiveType));
            OnPropertyChanged(nameof(IsHsuObjective));
            OnPropertyChanged(nameof(IsReactorObjective));
            OnPropertyChanged(nameof(IsReactorShutdownObjective));
            OnPropertyChanged(nameof(IsGatherSmallItemsObjective));
            OnPropertyChanged(nameof(IsClearAPathObjective));
            OnPropertyChanged(nameof(IsSpecialTerminalCommandObjective));
            OnPropertyChanged(nameof(IsRetrieveBigItemsObjective));
            OnPropertyChanged(nameof(IsDistributePowerCellsObjective));
            OnPropertyChanged(nameof(IsTerminalUplinkObjective));
            OnPropertyChanged(nameof(IsCentralGeneratorClusterObjective));
            OnPropertyChanged(nameof(IsActivateSmallHsuObjective));
            OnPropertyChanged(nameof(IsSurvivalObjective));
            OnPropertyChanged(nameof(IsGatherTerminalObjective));
            OnPropertyChanged(nameof(IsCorruptedTerminalUplinkObjective));
            OnPropertyChanged(nameof(IsTimedTerminalSequenceObjective));
            if (EditingSectorSettings?.ObjectiveType == ObjectiveType.Reactor_Startup)
            {
                if (EditingSectorSettings.ReactorStartupStages.Count == 0)
                {
                    EditingSectorSettings.ReactorStartupStages.Add(new ReactorStartupStage());
                }

                UpdateStageNumbers();
                if (SelectedReactorStage == null)
                {
                    SelectedReactorStage = EditingSectorSettings.ReactorStartupStages.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.GatherSmallItems)
            {
                if (EditingSectorSettings.GatherItemPlacements.Count == 0)
                {
                    EditingSectorSettings.GatherItemPlacements.Add(new GatherItemPlacement());
                }

                RenumberGatherItemPlacements();
                if (SelectedGatherItemPlacement == null)
                {
                    SelectedGatherItemPlacement = EditingSectorSettings.GatherItemPlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.RetrieveBigItems)
            {
                if (EditingSectorSettings.RetrieveItemPlacements.Count == 0)
                {
                    EditingSectorSettings.RetrieveItemPlacements.Add(new GatherItemPlacement());
                }

                RenumberRetrieveItemPlacements();
                if (SelectedRetrieveItemPlacement == null)
                {
                    SelectedRetrieveItemPlacement = EditingSectorSettings.RetrieveItemPlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.DistributePowerCells)
            {
                if (EditingSectorSettings.DistributeGeneratorPlacements.Count == 0)
                {
                    EditingSectorSettings.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement());
                }

                RenumberDistributeGeneratorPlacements();
                if (SelectedDistributeGeneratorPlacement == null)
                {
                    SelectedDistributeGeneratorPlacement =
                        EditingSectorSettings.DistributeGeneratorPlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.TerminalUplink ||
                     EditingSectorSettings?.ObjectiveType == ObjectiveType.CorruptedTerminalUplink)
            {
                if (EditingSectorSettings.TerminalUplinkPlacements.Count == 0)
                {
                    EditingSectorSettings.TerminalUplinkPlacements.Add(new GatherItemPlacement
                        { ItemNumber = 1, ItemPrefix = "Terminal" });
                }

                RenumberTerminalUplinkPlacements();
                if (SelectedTerminalUplinkPlacement == null)
                {
                    SelectedTerminalUplinkPlacement = EditingSectorSettings.TerminalUplinkPlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.GatherTerminal)
            {
                if (EditingSectorSettings.GatherTerminalPlacements.Count == 0)
                {
                    EditingSectorSettings.GatherTerminalPlacements.Add(new GatherItemPlacement
                        { ItemNumber = 1, ItemPrefix = "Terminal" });
                }

                RenumberGatherTerminalPlacements();
                if (SelectedGatherTerminalPlacement == null)
                {
                    SelectedGatherTerminalPlacement = EditingSectorSettings.GatherTerminalPlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.TimedTerminalSequence)
            {
                if (EditingSectorSettings.TimedTerminalSequencePlacements.Count == 0)
                {
                    EditingSectorSettings.TimedTerminalSequencePlacements.Add(new GatherItemPlacement
                        { ItemNumber = 1, ItemPrefix = "Terminal" });
                }

                RenumberTimedTerminalSequencePlacements();
                if (SelectedTimedTerminalSequencePlacement == null)
                {
                    SelectedTimedTerminalSequencePlacement =
                        EditingSectorSettings.TimedTerminalSequencePlacements.FirstOrDefault();
                }
            }
            else if (EditingSectorSettings?.ObjectiveType == ObjectiveType.CentralGeneratorCluster)
            {
                if (EditingSectorSettings.CentralGeneratorClusterFogTransitions.Count == 0)
                {
                    EditingSectorSettings.CentralGeneratorClusterGeneratorCount = 4;
                }

                RenumberCentralGeneratorFogTransitions();
                if (SelectedCentralGeneratorFogTransitionStage == null)
                {
                    SelectedCentralGeneratorFogTransitionStage =
                        EditingSectorSettings.CentralGeneratorClusterFogTransitions.FirstOrDefault();
                }

                if (EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count == 0)
                {
                    EditingSectorSettings.CentralGeneratorClusterPowerCellCount = 4;
                }

                RenumberCentralGeneratorPowerCellPlacements();
                if (SelectedCentralGeneratorPowerCellPlacement == null)
                {
                    SelectedCentralGeneratorPowerCellPlacement =
                        EditingSectorSettings.CentralGeneratorClusterPowerCellPlacements.FirstOrDefault();
                }
            }

            OnPropertyChanged(nameof(SelectedAlarmBeforeShutdownOption));
            OnPropertyChanged(nameof(SelectedAlarmAfterShutdownOption));
            OnPropertyChanged(nameof(SelectedSpecialTerminalCommandZoneOption));
            OnPropertyChanged(nameof(SelectedSpecialCommandRule));
            OnPropertyChanged(nameof(SelectedGatherSmallItemOption));
            OnPropertyChanged(nameof(SelectedGatherItem));
            OnPropertyChanged(nameof(GatherSpawnCount));
            OnPropertyChanged(nameof(SelectedRetrieveBigItemOption));
            OnPropertyChanged(nameof(SelectedRetrieveItem));
            OnPropertyChanged(nameof(RetrieveSpawnCount));
            OnPropertyChanged(nameof(DistributeGeneratorCount));
            OnPropertyChanged(nameof(UplinkNumberOfTerminals));
            OnPropertyChanged(nameof(UplinkNumberOfVerificationRounds));
            OnPropertyChanged(nameof(CentralGeneratorClusterGeneratorCount));
            OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
            OnPropertyChanged(nameof(SelectedCentralGeneratorClusterZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.HsuPlacementZone))
        {
            OnPropertyChanged(nameof(SelectedHsuPlacementZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.ReactorPlacementZone))
        {
            OnPropertyChanged(nameof(SelectedReactorPlacementZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.SpecialTerminalCommandZone))
        {
            OnPropertyChanged(nameof(SelectedSpecialTerminalCommandZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.SpecialCommandRule))
        {
            OnPropertyChanged(nameof(SelectedSpecialCommandRule));
        }
        else if (e.PropertyName == nameof(SectorSettings.ChainedPuzzleToActive))
        {
            OnPropertyChanged(nameof(SelectedAlarmBeforeShutdownOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.ChainedPuzzleMidObjective))
        {
            OnPropertyChanged(nameof(SelectedAlarmAfterShutdownOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.GatherItem))
        {
            OnPropertyChanged(nameof(SelectedGatherSmallItemOption));
            OnPropertyChanged(nameof(SelectedGatherItem));
        }
        else if (e.PropertyName == nameof(SectorSettings.RetrieveItem))
        {
            OnPropertyChanged(nameof(SelectedRetrieveBigItemOption));
            OnPropertyChanged(nameof(SelectedRetrieveItem));
        }
        else if (e.PropertyName == nameof(SectorSettings.ActivateSmallHsuZone))
        {
            OnPropertyChanged(nameof(SelectedActivateSmallHsuZoneOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.ItemFromStart))
        {
            OnPropertyChanged(nameof(SelectedItemFromStartOption));
        }
        else if (e.PropertyName == nameof(SectorSettings.ItemAfterActivation))
        {
            OnPropertyChanged(nameof(SelectedItemAfterActivationOption));
        }
    }

    public void OpenSectorSettings(SectorType sectorType)
    {
        if (_selectedLevel == null) return;
        if (_editingSectorSettings != null)
        {
            _editingSectorSettings.PropertyChanged -= EditingSectorSettings_PropertyChanged;
        }

        if (_selectedReactorStage != null)
        {
            _selectedReactorStage.PropertyChanged -= SelectedReactorStage_PropertyChanged;
        }

        if (_selectedGatherItemPlacement != null)
        {
            _selectedGatherItemPlacement.PropertyChanged -= SelectedGatherItemPlacement_PropertyChanged;
        }

        if (_selectedRetrieveItemPlacement != null)
        {
            _selectedRetrieveItemPlacement.PropertyChanged -= SelectedRetrieveItemPlacement_PropertyChanged;
        }

        if (_selectedDistributeGeneratorPlacement != null)
        {
            _selectedDistributeGeneratorPlacement.PropertyChanged -=
                SelectedDistributeGeneratorPlacement_PropertyChanged;
        }

        if (_selectedTerminalUplinkPlacement != null)
        {
            _selectedTerminalUplinkPlacement.PropertyChanged -= SelectedTerminalUplinkPlacement_PropertyChanged;
        }

        if (_selectedCentralGeneratorFogTransitionStage != null)
        {
            _selectedCentralGeneratorFogTransitionStage.PropertyChanged -=
                SelectedCentralGeneratorFogTransitionStage_PropertyChanged;
        }

        if (_selectedCentralGeneratorPowerCellPlacement != null)
        {
            _selectedCentralGeneratorPowerCellPlacement.PropertyChanged -=
                SelectedCentralGeneratorPowerCellPlacement_PropertyChanged;
        }

        EditingSectorType = sectorType;
        EditingSectorSettings = _selectedLevel.GetSectorSettings(sectorType);
        if (_editingSectorSettings != null)
        {
            _editingSectorSettings.PropertyChanged += EditingSectorSettings_PropertyChanged;
            if (_editingSectorSettings.ObjectiveType == ObjectiveType.Reactor_Startup &&
                _editingSectorSettings.ReactorStartupStages.Count == 0)
            {
                _editingSectorSettings.ReactorStartupStages.Add(new ReactorStartupStage());
            }

            UpdateStageNumbers();
            SelectedReactorStage = _editingSectorSettings.ReactorStartupStages.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.GatherSmallItems &&
                _editingSectorSettings.GatherItemPlacements.Count == 0)
            {
                _editingSectorSettings.GatherItemPlacements.Add(new GatherItemPlacement());
            }

            RenumberGatherItemPlacements();
            SelectedGatherItemPlacement = _editingSectorSettings.GatherItemPlacements.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.RetrieveBigItems &&
                _editingSectorSettings.RetrieveItemPlacements.Count == 0)
            {
                _editingSectorSettings.RetrieveItemPlacements.Add(new GatherItemPlacement());
            }

            RenumberRetrieveItemPlacements();
            SelectedRetrieveItemPlacement = _editingSectorSettings.RetrieveItemPlacements.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.DistributePowerCells &&
                _editingSectorSettings.DistributeGeneratorPlacements.Count == 0)
            {
                _editingSectorSettings.DistributeGeneratorPlacements.Add(new DistributeGeneratorPlacement());
            }

            RenumberDistributeGeneratorPlacements();
            SelectedDistributeGeneratorPlacement =
                _editingSectorSettings.DistributeGeneratorPlacements.FirstOrDefault();

            if ((_editingSectorSettings.ObjectiveType == ObjectiveType.TerminalUplink ||
                 _editingSectorSettings.ObjectiveType == ObjectiveType.CorruptedTerminalUplink) &&
                _editingSectorSettings.TerminalUplinkPlacements.Count == 0)
            {
                _editingSectorSettings.TerminalUplinkPlacements.Add(new GatherItemPlacement
                    { ItemNumber = 1, ItemPrefix = "Terminal" });
            }

            RenumberTerminalUplinkPlacements();
            SelectedTerminalUplinkPlacement = _editingSectorSettings.TerminalUplinkPlacements.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.GatherTerminal &&
                _editingSectorSettings.GatherTerminalPlacements.Count == 0)
            {
                _editingSectorSettings.GatherTerminalPlacements.Add(new GatherItemPlacement
                    { ItemNumber = 1, ItemPrefix = "Terminal" });
            }

            RenumberGatherTerminalPlacements();
            SelectedGatherTerminalPlacement = _editingSectorSettings.GatherTerminalPlacements.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.TimedTerminalSequence &&
                _editingSectorSettings.TimedTerminalSequencePlacements.Count == 0)
            {
                _editingSectorSettings.TimedTerminalSequencePlacements.Add(new GatherItemPlacement
                    { ItemNumber = 1, ItemPrefix = "Terminal" });
            }

            RenumberTimedTerminalSequencePlacements();
            SelectedTimedTerminalSequencePlacement = _editingSectorSettings.TimedTerminalSequencePlacements.FirstOrDefault();

            if (_editingSectorSettings.ObjectiveType == ObjectiveType.CentralGeneratorCluster)
            {
                if (_editingSectorSettings.CentralGeneratorClusterFogTransitions.Count == 0)
                {
                    _editingSectorSettings.CentralGeneratorClusterGeneratorCount = 4;
                }

                RenumberCentralGeneratorFogTransitions();
                SelectedCentralGeneratorFogTransitionStage =
                    _editingSectorSettings.CentralGeneratorClusterFogTransitions.FirstOrDefault();

                if (_editingSectorSettings.CentralGeneratorClusterPowerCellPlacements.Count == 0)
                {
                    _editingSectorSettings.CentralGeneratorClusterPowerCellCount = 4;
                }

                RenumberCentralGeneratorPowerCellPlacements();
                SelectedCentralGeneratorPowerCellPlacement =
                    _editingSectorSettings.CentralGeneratorClusterPowerCellPlacements.FirstOrDefault();
            }
        }
        else
        {
            SelectedReactorStage = null;
            SelectedGatherItemPlacement = null;
            SelectedRetrieveItemPlacement = null;
            SelectedDistributeGeneratorPlacement = null;
            SelectedTerminalUplinkPlacement = null;
            SelectedCentralGeneratorFogTransitionStage = null;
            SelectedCentralGeneratorPowerCellPlacement = null;
        }

        PopulateSectorZoneOptions();
        OnPropertyChanged(nameof(EditingSectorTitle));
        OnPropertyChanged(nameof(IsMainSector));
        OnPropertyChanged(nameof(SelectedSectorWinCondition));
        OnPropertyChanged(nameof(IsExitGeoWinCondition));
        OnPropertyChanged(nameof(SelectedSectorObjectiveType));
        OnPropertyChanged(nameof(IsHsuObjective));
        OnPropertyChanged(nameof(IsReactorObjective));
        OnPropertyChanged(nameof(IsReactorShutdownObjective));
        OnPropertyChanged(nameof(IsGatherSmallItemsObjective));
        OnPropertyChanged(nameof(IsClearAPathObjective));
        OnPropertyChanged(nameof(IsSpecialTerminalCommandObjective));
        OnPropertyChanged(nameof(IsRetrieveBigItemsObjective));
        OnPropertyChanged(nameof(IsDistributePowerCellsObjective));
        OnPropertyChanged(nameof(IsTerminalUplinkObjective));
        OnPropertyChanged(nameof(IsCentralGeneratorClusterObjective));
        OnPropertyChanged(nameof(IsActivateSmallHsuObjective));
        OnPropertyChanged(nameof(IsSurvivalObjective));
        OnPropertyChanged(nameof(IsGatherTerminalObjective));
        OnPropertyChanged(nameof(IsCorruptedTerminalUplinkObjective));
        OnPropertyChanged(nameof(IsTimedTerminalSequenceObjective));
        OnPropertyChanged(nameof(SelectedExitZoneOption));
        OnPropertyChanged(nameof(SelectedHsuPlacementZoneOption));
        OnPropertyChanged(nameof(SelectedReactorPlacementZoneOption));
        OnPropertyChanged(nameof(SelectedStageVerificationZoneOption));
        OnPropertyChanged(nameof(SelectedSpecialTerminalCommandZoneOption));
        OnPropertyChanged(nameof(SelectedActivateSmallHsuZoneOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequenceMainDimensionOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequenceMainZoneOption));
        OnPropertyChanged(nameof(SelectedItemFromStartOption));
        OnPropertyChanged(nameof(SelectedItemAfterActivationOption));
        OnPropertyChanged(nameof(SelectedSpecialCommandRule));
        OnPropertyChanged(nameof(SelectedAlarmBeforeShutdownOption));
        OnPropertyChanged(nameof(SelectedAlarmAfterShutdownOption));
        OnPropertyChanged(nameof(SelectedReactorStage));
        OnPropertyChanged(nameof(HasSelectedReactorStage));
        OnPropertyChanged(nameof(SelectedGatherSmallItemOption));
        OnPropertyChanged(nameof(SelectedGatherItem));
        OnPropertyChanged(nameof(GatherSpawnCount));
        OnPropertyChanged(nameof(SelectedGatherItemPlacement));
        OnPropertyChanged(nameof(HasSelectedGatherItemPlacement));
        OnPropertyChanged(nameof(SelectedGatherItemPlacementDimensionOption));
        OnPropertyChanged(nameof(SelectedGatherItemPlacementZoneOption));
        OnPropertyChanged(nameof(GatherTerminalSpawnCount));
        OnPropertyChanged(nameof(SelectedGatherTerminalPlacement));
        OnPropertyChanged(nameof(HasSelectedGatherTerminalPlacement));
        OnPropertyChanged(nameof(SelectedGatherTerminalPlacementDimensionOption));
        OnPropertyChanged(nameof(SelectedGatherTerminalPlacementZoneOption));
        OnPropertyChanged(nameof(TimedTerminalSequenceNumberOfTerminals));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacement));
        OnPropertyChanged(nameof(HasSelectedTimedTerminalSequencePlacement));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementDimensionOption));
        OnPropertyChanged(nameof(SelectedTimedTerminalSequencePlacementZoneOption));
        OnPropertyChanged(nameof(SelectedRetrieveBigItemOption));
        OnPropertyChanged(nameof(SelectedRetrieveItem));
        OnPropertyChanged(nameof(RetrieveSpawnCount));
        OnPropertyChanged(nameof(SelectedRetrieveItemPlacement));
        OnPropertyChanged(nameof(HasSelectedRetrieveItemPlacement));
        OnPropertyChanged(nameof(SelectedRetrieveItemPlacementDimensionOption));
        OnPropertyChanged(nameof(SelectedRetrieveItemPlacementZoneOption));
        OnPropertyChanged(nameof(DistributeGeneratorCount));
        OnPropertyChanged(nameof(SelectedDistributeGeneratorPlacement));
        OnPropertyChanged(nameof(HasSelectedDistributeGeneratorPlacement));
        OnPropertyChanged(nameof(UplinkNumberOfTerminals));
        OnPropertyChanged(nameof(UplinkNumberOfVerificationRounds));
        OnPropertyChanged(nameof(SelectedTerminalUplinkPlacement));
        OnPropertyChanged(nameof(HasSelectedTerminalUplinkPlacement));
        OnPropertyChanged(nameof(SelectedCentralGeneratorClusterZoneOption));
        OnPropertyChanged(nameof(CentralGeneratorClusterGeneratorCount));
        OnPropertyChanged(nameof(SelectedCentralGeneratorFogTransitionStage));
        OnPropertyChanged(nameof(HasSelectedCentralGeneratorFogTransitionStage));
        OnPropertyChanged(nameof(CentralGeneratorClusterPowerCellCount));
        OnPropertyChanged(nameof(SelectedCentralGeneratorPowerCellPlacement));
        OnPropertyChanged(nameof(HasSelectedCentralGeneratorPowerCellPlacement));
        IsEditingSector = true;
    }

    public void OpenMainSectorSettings() => OpenSectorSettings(SectorType.Main);
    public void OpenSecondarySectorSettings() => OpenSectorSettings(SectorType.Secondary);
    public void OpenOverloadSectorSettings() => OpenSectorSettings(SectorType.Overload);

    public void CloseSectorSettings()
    {
        if (_editingSectorSettings != null)
        {
            _editingSectorSettings.PropertyChanged -= EditingSectorSettings_PropertyChanged;
        }

        if (_selectedReactorStage != null)
        {
            _selectedReactorStage.PropertyChanged -= SelectedReactorStage_PropertyChanged;
        }

        if (_selectedGatherItemPlacement != null)
        {
            _selectedGatherItemPlacement.PropertyChanged -= SelectedGatherItemPlacement_PropertyChanged;
        }

        if (_selectedRetrieveItemPlacement != null)
        {
            _selectedRetrieveItemPlacement.PropertyChanged -= SelectedRetrieveItemPlacement_PropertyChanged;
        }

        if (_selectedDistributeGeneratorPlacement != null)
        {
            _selectedDistributeGeneratorPlacement.PropertyChanged -=
                SelectedDistributeGeneratorPlacement_PropertyChanged;
        }

        if (_selectedTerminalUplinkPlacement != null)
        {
            _selectedTerminalUplinkPlacement.PropertyChanged -= SelectedTerminalUplinkPlacement_PropertyChanged;
        }

        if (_selectedCentralGeneratorFogTransitionStage != null)
        {
            _selectedCentralGeneratorFogTransitionStage.PropertyChanged -=
                SelectedCentralGeneratorFogTransitionStage_PropertyChanged;
        }

        if (_selectedCentralGeneratorPowerCellPlacement != null)
        {
            _selectedCentralGeneratorPowerCellPlacement.PropertyChanged -=
                SelectedCentralGeneratorPowerCellPlacement_PropertyChanged;
        }

        if (_selectedGatherTerminalPlacement != null)
        {
            _selectedGatherTerminalPlacement.PropertyChanged -= SelectedGatherTerminalPlacement_PropertyChanged;
        }

        if (_selectedGatherTerminalCandidateZonePlacement != null)
        {
            _selectedGatherTerminalCandidateZonePlacement.PropertyChanged -=
                SelectedGatherTerminalCandidateZonePlacement_PropertyChanged;
        }

        if (_selectedTimedTerminalSequencePlacement != null)
        {
            _selectedTimedTerminalSequencePlacement.PropertyChanged -=
                SelectedTimedTerminalSequencePlacement_PropertyChanged;
        }

        if (_selectedTimedTerminalSequenceCandidateZonePlacement != null)
        {
            _selectedTimedTerminalSequenceCandidateZonePlacement.PropertyChanged -=
                SelectedTimedTerminalSequenceCandidateZonePlacement_PropertyChanged;
        }

        IsEditingSector = false;
        EditingSectorSettings = null;
        SelectedReactorStage = null;
        SelectedGatherItemPlacement = null;
        SelectedRetrieveItemPlacement = null;
        SelectedDistributeGeneratorPlacement = null;
        SelectedTerminalUplinkPlacement = null;
        SelectedCentralGeneratorFogTransitionStage = null;
        SelectedCentralGeneratorPowerCellPlacement = null;
        SelectedGatherTerminalPlacement = null;
        SelectedGatherTerminalCandidateZonePlacement = null;
        SelectedTimedTerminalSequencePlacement = null;
        SelectedTimedTerminalSequenceCandidateZonePlacement = null;
    }
}