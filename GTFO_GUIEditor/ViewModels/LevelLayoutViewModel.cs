using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    private ComplexResourceSetBlock? _dimensionSelectedComplexResourceSet;
    private ObservableCollection<string> _availableTileSources = new() { "Elevator Shafts", "Dimension Geomorphs (Custom Objective 1x1)" };
    private string _selectedTileSource = "Dimension Geomorphs (Custom Objective 1x1)";
    private ObservableCollection<ElevatorTileOption> _availableDimensionTiles = new();
    private ElevatorTileOption? _selectedDimensionTile;
    private string _dimensionPromptErrorMessage = string.Empty;

    // Create Level modal state
    private bool _isCreatingLevel;
    private string _newLevelName = string.Empty;
    private uint _newLevelPersistentId;
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
        : this(null, null)
    {
    }

    public LevelLayoutViewModel(
        ObservableCollection<ComplexResourceSetBlock>? complexResourceSets = null,
        IEnumerable<LevelLayoutBlock>? initialLevels = null)
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

        var loadedLevels = initialLevels?.ToList() ?? LevelLayoutService.GetInitialLevels(_availableComplexResourceSets);
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
                        e.PropertyName == nameof(LevelGenConfig.CellDimension))
                    {
                        var activeSet = GetActiveComplexResourceSet();
                        if (activeSet == set)
                        {
                            _gridState.GridSize = set.LevelGenConfig.GridSize > 0 ? set.LevelGenConfig.GridSize : 40;
                            _gridState.CellDimension = set.LevelGenConfig.CellDimension > 0 ? set.LevelGenConfig.CellDimension : 64.0;
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
                return $"Warning: Persistent ID {_selectedLevel.PersistentId} is shared by {count} levels. IDs must be unique.";
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
            string dimInfo = _activeDimensionIndex == "Reality" ? "Reality" : (_selectedDimension?.DisplayName ?? _activeDimensionIndex);
            StatusText = $"Selected [{_selectedLevel.PersistentId}] {_selectedLevel.Name} - {dimInfo} ({_levels.Count} total)";
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

    public bool CanDeleteActiveDimension => HasSelectedLevel && !IsViewingReality && (_selectedLevel?.HasDimension(_activeDimensionIndex) ?? false);

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
            GridState.CellDimension = activeSet.LevelGenConfig.CellDimension > 0 ? activeSet.LevelGenConfig.CellDimension : 64.0;
        }
        else
        {
            GridState.GridSize = 40;
            GridState.CellDimension = 64.0;
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
                if (SelectedExpansionGeomorph != null && !FilteredExpansionGeomorphs.Contains(SelectedExpansionGeomorph))
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

    public GeomorphOption? SelectedExpansionGeomorph
    {
        get => _selectedExpansionGeomorph;
        set
        {
            if (SetField(ref _selectedExpansionGeomorph, value))
            {
                UpdateAvailableExpansionRotations();
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
        set => SetField(ref _selectedExpansionRotation, value);
    }

    public bool HasAvailableRotations => _availableExpansionRotations.Count > 0;

    private void UpdateAvailableExpansionRotations()
    {
        _availableExpansionRotations.Clear();
        if (_selectedExpansionGeomorph != null && _selectedDoorForExpansion != null)
        {
            var validRotations = GridState.GetValidRotationsForGeomorph(_selectedDoorForExpansion, _selectedExpansionGeomorph.PrefabPath);
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
            var matching = _availableExpansionRotations.FirstOrDefault(r => Math.Abs(r.RotationAngle - _selectedExpansionRotation.RotationAngle) < 0.1);
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
                OnPropertyChanged(nameof(IsExpansionSecondaryDoor));
                OnPropertyChanged(nameof(IsExpansionOverloadDoor));
            }
        }
    }

    public bool IsExpansionRoomDoor
    {
        get => _selectedExpansionDoorType == DoorType.RoomDoor;
        set { if (value) SelectedExpansionDoorType = DoorType.RoomDoor; }
    }

    public bool IsExpansionSecurityDoor
    {
        get => _selectedExpansionDoorType == DoorType.SecurityDoor;
        set { if (value) SelectedExpansionDoorType = DoorType.SecurityDoor; }
    }

    public bool IsExpansionSecondaryDoor
    {
        get => _selectedExpansionDoorType == DoorType.SecondaryDoor;
        set { if (value) SelectedExpansionDoorType = DoorType.SecondaryDoor; }
    }

    public bool IsExpansionOverloadDoor
    {
        get => _selectedExpansionDoorType == DoorType.OverloadDoor;
        set { if (value) SelectedExpansionDoorType = DoorType.OverloadDoor; }
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

    public DoorType SelectedRoomExpansionDoorType
    {
        get => _selectedRoomExpansionDoorType;
        set
        {
            if (SetField(ref _selectedRoomExpansionDoorType, value))
            {
                OnPropertyChanged(nameof(IsRoomExpansionRoomDoor));
                OnPropertyChanged(nameof(IsRoomExpansionSecurityDoor));
                OnPropertyChanged(nameof(IsRoomExpansionSecondaryDoor));
                OnPropertyChanged(nameof(IsRoomExpansionOverloadDoor));
            }
        }
    }

    public bool IsRoomExpansionRoomDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.RoomDoor;
        set { if (value) SelectedRoomExpansionDoorType = DoorType.RoomDoor; }
    }

    public bool IsRoomExpansionSecurityDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.SecurityDoor;
        set { if (value) SelectedRoomExpansionDoorType = DoorType.SecurityDoor; }
    }

    public bool IsRoomExpansionSecondaryDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.SecondaryDoor;
        set { if (value) SelectedRoomExpansionDoorType = DoorType.SecondaryDoor; }
    }

    public bool IsRoomExpansionOverloadDoor
    {
        get => _selectedRoomExpansionDoorType == DoorType.OverloadDoor;
        set { if (value) SelectedRoomExpansionDoorType = DoorType.OverloadDoor; }
    }

    public bool CanAddSecondaryDoor => !GridState.HasSecondaryDoor;
    public bool CanAddOverloadDoor => !GridState.HasOverloadDoor;

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
        RoomExpansionErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CanAddSecondaryDoor));
        OnPropertyChanged(nameof(CanAddOverloadDoor));
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
        int generatedArea = GridState.GenerateRoomFromDoor(door, SelectedRoomExpansionDoorType);
        IsExpandingRoom = false;

        if (generatedArea >= 0)
        {
            string doorTypeStr = SelectedRoomExpansionDoorType switch
            {
                DoorType.SecondaryDoor => "Secondary Door",
                DoorType.OverloadDoor => "Overload Door",
                DoorType.SecurityDoor => "Security Door",
                _ => "Room Door"
            };
            StatusText = $"Generated Area {generatedArea} in {door.Geomorph?.DisplayName ?? "geomorph"} connected via {doorTypeStr}.";
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

        // Populate available geomorphs from the active complex resource set
        _availableExpansionGeomorphs.Clear();

        uint activeSetId = ActiveComplexResourceSetId;
        var activeSet = AvailableComplexResourceSets.FirstOrDefault(s => s.PersistentId == activeSetId);

        var addedPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCategoryPrefabs(IEnumerable<PrefabItem>? items, string categoryName)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Prefab)) continue;
                if (addedPrefabs.Add(item.Prefab))
                {
                    string displayName = LevelLayoutService.GetFriendlyElevatorName(item.Prefab);
                    _availableExpansionGeomorphs.Add(new GeomorphOption
                    {
                        DisplayName = displayName,
                        PrefabPath = item.Prefab,
                        Category = categoryName
                    });
                }
            }
        }

        if (activeSet != null)
        {
            AddCategoryPrefabs(activeSet.GeomorphTiles_1x1, "Geomorphs 1x1");
            AddCategoryPrefabs(activeSet.GeomorphTiles_2x1, "Geomorphs 2x1");
            AddCategoryPrefabs(activeSet.GeomorphTiles_2x2, "Geomorphs 2x2");
            AddCategoryPrefabs(activeSet.CustomGeomorphs_Objective_1x1, "Objective 1x1");
            AddCategoryPrefabs(activeSet.CustomGeomorphs_Challenge_1x1, "Challenge 1x1");
            AddCategoryPrefabs(activeSet.CustomGeomorphs_Exit_1x1, "Exit 1x1");
            AddCategoryPrefabs(activeSet.ElevatorShafts_1x1, "Elevator Shafts");
        }

        // If no geomorphs in the active set, load from vanilla geomorph service
        if (_availableExpansionGeomorphs.Count == 0)
        {
            var vanillaGeos = GeomorphService.LoadGeomorphs();
            foreach (var geo in vanillaGeos)
            {
                if (string.IsNullOrWhiteSpace(geo.PrefabPath)) continue;
                if (addedPrefabs.Add(geo.PrefabPath))
                {
                    _availableExpansionGeomorphs.Add(new GeomorphOption
                    {
                        DisplayName = geo.DisplayName,
                        PrefabPath = geo.PrefabPath,
                        Category = geo.Category
                    });
                }
            }
        }

        OnPropertyChanged(nameof(FilteredExpansionGeomorphs));
        SelectedExpansionGeomorph = _availableExpansionGeomorphs.FirstOrDefault();
        UpdateAvailableExpansionRotations();
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
            ExpansionErrorMessage = $"Cannot expand beyond the {GridState.GridSize}x{GridState.GridSize} grid boundaries.";
            return false;
        }

        var existing = GridState.GetGeomorphAt(targetX, targetY);
        if (existing != null)
        {
            ExpansionErrorMessage = $"Cell ({targetX}, {targetY}) is already occupied by \"{existing.DisplayName}\".";
            return false;
        }

        double rotationAngle = _selectedExpansionRotation.RotationAngle;
        var placedGeo = GridState.ExpandFromDoor(_selectedDoorForExpansion, _selectedExpansionGeomorph.PrefabPath, SelectedExpansionDoorType, rotationAngle);
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
        StatusText = $"Placed \"{_selectedExpansionGeomorph.DisplayName}\" (Rotation {rotationAngle:0}°) at cell ({targetX}, {targetY}){doorTypeStr}";

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

    public bool IsPromptingReality => string.Equals(_promptingDimensionOption?.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase);

    public string PromptStartingTileLabel => IsPromptingReality ? "Starting Elevator Tile:" : "Starting Tile / DimensionGeomorph:";

    public string PromptDimensionTitle => _promptingDimensionOption != null
        ? (_promptingDimensionOption.DimensionIndex == "Reality" ? "Edit Dimension: Reality" : $"Configure Dimension - {_promptingDimensionOption.DisplayName}")
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

    public void OpenPromptDimensionDialog(DimensionOption targetDimension, DimensionOption? previousDimension = null, bool isNew = false)
    {
        _promptingDimensionOption = targetDimension;
        _previousDimensionOption = previousDimension ?? _availableDimensions.FirstOrDefault(d => d.DimensionIndex == "Reality");

        bool isReality = string.Equals(targetDimension.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase);

        if (isReality)
        {
            DimensionCustomName = "Reality";

            _availableTileSources.Clear();
            _availableTileSources.Add("Elevator Shafts");

            if (_selectedLevel != null)
            {
                DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                    .FirstOrDefault(s => s.PersistentId == _selectedLevel.ComplexResourceSet)
                    ?? _availableComplexResourceSets.FirstOrDefault();

                SelectedTileSource = "Elevator Shafts";
                UpdateDimensionTilesForSelectedSet();

                SelectedDimensionTile = _availableDimensionTiles.FirstOrDefault(t => string.Equals(t.Prefab, _selectedLevel.ElevatorTile, StringComparison.OrdinalIgnoreCase))
                    ?? _availableDimensionTiles.FirstOrDefault();
            }
        }
        else
        {
            _availableTileSources.Clear();
            _availableTileSources.Add("Elevator Shafts");
            _availableTileSources.Add("Dimension Geomorphs (Custom Objective 1x1)");

            var existing = _selectedLevel?.GetDimension(targetDimension.DimensionIndex);
            if (existing != null && _selectedLevel != null && _selectedLevel.HasDimension(targetDimension.DimensionIndex) && !isNew)
            {
                DimensionCustomName = existing.Name;

                DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                    .FirstOrDefault(s => s.PersistentId == existing.ComplexResourceSet)
                    ?? _availableComplexResourceSets.FirstOrDefault();

                SelectedTileSource = existing.TileCategory == "Elevator Shaft"
                    ? "Elevator Shafts"
                    : "Dimension Geomorphs (Custom Objective 1x1)";

                UpdateDimensionTilesForSelectedSet();

                SelectedDimensionTile = _availableDimensionTiles.FirstOrDefault(t => string.Equals(t.Prefab, existing.DimensionGeomorph, StringComparison.OrdinalIgnoreCase))
                    ?? _availableDimensionTiles.FirstOrDefault();
            }
            else
            {
                DimensionCustomName = targetDimension.DisplayName;

                if (_selectedLevel != null)
                {
                    DimensionSelectedComplexResourceSet = _availableComplexResourceSets
                        .FirstOrDefault(s => s.PersistentId == _selectedLevel.ComplexResourceSet)
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

        if (_dimensionSelectedComplexResourceSet != null)
        {
            if (_selectedTileSource == "Elevator Shafts" || IsPromptingReality)
            {
                foreach (var shaft in _dimensionSelectedComplexResourceSet.ElevatorShafts_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(shaft.Prefab))
                    {
                        string friendlyName = LevelLayoutService.GetFriendlyElevatorName(shaft.Prefab);
                        _availableDimensionTiles.Add(new ElevatorTileOption
                        {
                            Prefab = shaft.Prefab,
                            DisplayName = friendlyName,
                            Category = "Elevator Shaft"
                        });
                    }
                }
            }
            else
            {
                foreach (var objGeo in _dimensionSelectedComplexResourceSet.CustomGeomorphs_Objective_1x1)
                {
                    if (!string.IsNullOrWhiteSpace(objGeo.Prefab))
                    {
                        string friendlyName = LevelLayoutService.GetFriendlyElevatorName(objGeo.Prefab);
                        _availableDimensionTiles.Add(new ElevatorTileOption
                        {
                            Prefab = objGeo.Prefab,
                            DisplayName = friendlyName,
                            Category = "Dimension Geomorph"
                        });
                    }
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

        if (DimensionSelectedComplexResourceSet == null)
        {
            DimensionPromptErrorMessage = "Please select a Complex Resource Set.";
            return false;
        }

        if (SelectedDimensionTile == null || string.IsNullOrWhiteSpace(SelectedDimensionTile.Prefab))
        {
            DimensionPromptErrorMessage = "Please select a starting tile / DimensionGeomorph.";
            return false;
        }

        bool isReality = string.Equals(_promptingDimensionOption.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase);

        if (isReality)
        {
            _selectedLevel.ComplexResourceSet = DimensionSelectedComplexResourceSet.PersistentId;
            _selectedLevel.ElevatorTile = SelectedDimensionTile.Prefab;
            _activeDimensionIndex = "Reality";
        }
        else
        {
            string tileCategory = SelectedTileSource == "Elevator Shafts" ? "Elevator Shaft" : "Dimension Geomorph";
            string dimName = string.IsNullOrWhiteSpace(DimensionCustomName)
                ? _promptingDimensionOption.DimensionIndex
                : DimensionCustomName.Trim();

            var dimBlock = new DimensionLayoutBlock
            {
                DimensionIndex = _promptingDimensionOption.DimensionIndex,
                Name = dimName,
                ComplexResourceSet = DimensionSelectedComplexResourceSet.PersistentId,
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
        StatusText = $"Configured {ActiveDimensionDisplayName} for [{_selectedLevel.PersistentId}] {_selectedLevel.Name}";
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

        if (_selectedLevel != null)
        {
            SelectedComplexResourceSet = _availableComplexResourceSets
                .FirstOrDefault(s => s.PersistentId == _selectedLevel.ComplexResourceSet)
                ?? _availableComplexResourceSets.FirstOrDefault();
        }
        else
        {
            SelectedComplexResourceSet = _availableComplexResourceSets.FirstOrDefault();
        }

        UpdateElevatorTilesForSelectedSet();

        CreationErrorMessage = string.Empty;
        IsCreatingLevel = true;
    }

    private void UpdateElevatorTilesForSelectedSet()
    {
        _availableElevatorTiles.Clear();

        if (_selectedComplexResourceSet != null)
        {
            foreach (var shaft in _selectedComplexResourceSet.ElevatorShafts_1x1)
            {
                if (!string.IsNullOrWhiteSpace(shaft.Prefab))
                {
                    string friendlyName = LevelLayoutService.GetFriendlyElevatorName(shaft.Prefab);
                    _availableElevatorTiles.Add(new ElevatorTileOption
                    {
                        Prefab = shaft.Prefab,
                        DisplayName = friendlyName,
                        Category = "Elevator Shaft"
                    });
                }
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

        if (SelectedComplexResourceSet == null)
        {
            CreationErrorMessage = "Please select a Complex Resource Set.";
            return false;
        }

        if (SelectedElevatorTile == null || string.IsNullOrWhiteSpace(SelectedElevatorTile.Prefab))
        {
            CreationErrorMessage = "Please select a starting elevator tile.";
            return false;
        }

        var newLevel = LevelLayoutService.CreateFreshLevel(
            NewLevelPersistentId,
            NewLevelName.Trim(),
            SelectedComplexResourceSet.PersistentId,
            SelectedElevatorTile.Prefab
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
}
