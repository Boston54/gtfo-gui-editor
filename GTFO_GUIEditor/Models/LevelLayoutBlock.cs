using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class LevelLayoutBlock : INotifyPropertyChanged
{
    private string _name = "New Level";
    private bool _internalEnabled = true;
    private uint _persistentId;
    private ComplexType _complexType = ComplexType.Mining;
    private uint _complexResourceSet;
    private string _elevatorTile = string.Empty;
    private uint _fogSettings;
    private ObservableCollection<DimensionLayoutBlock> _dimensions = new();
    private LevelLayoutGridState? _gridState;
    private SectorSettings _mainSector = new() { ArtifactAmountMultiplier = 1.0, ZoneAliasStart = 0 };
    private SectorSettings _secondarySector = new() { ArtifactAmountMultiplier = 1.0, ZoneAliasStart = 100 };
    private SectorSettings _overloadSector = new() { ArtifactAmountMultiplier = 1.0, ZoneAliasStart = 200 };

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled
    {
        get => _internalEnabled;
        set => SetField(ref _internalEnabled, value);
    }

    [JsonPropertyName("persistentID")]
    public uint PersistentId
    {
        get => _persistentId;
        set
        {
            if (SetField(ref _persistentId, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("ComplexType")]
    public ComplexType ComplexType
    {
        get => _complexType;
        set => SetField(ref _complexType, value);
    }

    [JsonPropertyName("ComplexResourceSet")]
    public uint ComplexResourceSet
    {
        get => _complexResourceSet;
        set => SetField(ref _complexResourceSet, value);
    }

    [JsonPropertyName("FogSettings")]
    public uint FogSettings
    {
        get => _fogSettings;
        set => SetField(ref _fogSettings, value);
    }

    [JsonPropertyName("ElevatorTile")]
    public string ElevatorTile
    {
        get => _elevatorTile;
        set
        {
            if (SetField(ref _elevatorTile, value))
            {
                OnPropertyChanged(nameof(DimensionGeomorph));
            }
        }
    }

    [JsonPropertyName("DimensionGeomorph")]
    public string DimensionGeomorph
    {
        get => _elevatorTile;
        set => ElevatorTile = value;
    }

    [JsonPropertyName("Dimensions")]
    public ObservableCollection<DimensionLayoutBlock> Dimensions
    {
        get => _dimensions;
        set => SetField(ref _dimensions, value ?? new ObservableCollection<DimensionLayoutBlock>());
    }

    [JsonPropertyName("MainSector")]
    public SectorSettings MainSector
    {
        get => _mainSector;
        set => SetField(ref _mainSector, value ?? new SectorSettings());
    }

    [JsonPropertyName("SecondarySector")]
    public SectorSettings SecondarySector
    {
        get => _secondarySector;
        set => SetField(ref _secondarySector, value ?? new SectorSettings());
    }

    [JsonPropertyName("OverloadSector")]
    public SectorSettings OverloadSector
    {
        get => _overloadSector;
        set => SetField(ref _overloadSector, value ?? new SectorSettings());
    }

    public SectorSettings GetSectorSettings(SectorType sectorType)
    {
        return sectorType switch
        {
            SectorType.Main => MainSector,
            SectorType.Secondary => SecondarySector,
            SectorType.Overload => OverloadSector,
            _ => MainSector
        };
    }

    public string GetZoneDisplayName(int zoneIndex)
    {
        SectorType sector = GridState.GetSectorForZone(zoneIndex);
        SectorSettings settings = GetSectorSettings(sector);
        return settings.GetZoneDisplayName(zoneIndex);
    }

    public string GetZoneDisplayName(SectorType sector, int zoneIndex)
    {
        SectorSettings settings = GetSectorSettings(sector);
        return settings.GetZoneDisplayName(zoneIndex);
    }

    [JsonIgnore]
    public LevelLayoutGridState GridState
    {
        get
        {
            if (_gridState == null)
            {
                _gridState = new LevelLayoutGridState();
                _gridState.PropertyChanged += GridState_PropertyChanged;
                if (!string.IsNullOrWhiteSpace(ElevatorTile))
                {
                    _gridState.InitializeStartingTile(ElevatorTile, 0, 0);
                }
            }
            return _gridState;
        }
        set
        {
            if (_gridState != null)
            {
                _gridState.PropertyChanged -= GridState_PropertyChanged;
            }
            _gridState = value;
            if (_gridState != null)
            {
                _gridState.PropertyChanged += GridState_PropertyChanged;
            }
            OnPropertyChanged(nameof(GridState));
            OnPropertyChanged(nameof(HasSecondaryDoor));
            OnPropertyChanged(nameof(HasOverloadDoor));
        }
    }

    private void GridState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LevelLayoutGridState.HasSecondaryDoor))
        {
            OnPropertyChanged(nameof(HasSecondaryDoor));
        }
        else if (e.PropertyName == nameof(LevelLayoutGridState.HasOverloadDoor))
        {
            OnPropertyChanged(nameof(HasOverloadDoor));
        }
    }

    [JsonIgnore]
    public bool HasSecondaryDoor => GridState.HasSecondaryDoor;

    [JsonIgnore]
    public bool HasOverloadDoor => GridState.HasOverloadDoor;

    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Name)
        ? $"Level {PersistentId}"
        : Name;

    public bool HasDimension(string dimensionIndex)
    {
        if (string.IsNullOrWhiteSpace(dimensionIndex) || string.Equals(dimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _dimensions.Any(d => string.Equals(d.DimensionIndex, dimensionIndex, StringComparison.OrdinalIgnoreCase));
    }

    public DimensionLayoutBlock? GetDimension(string dimensionIndex)
    {
        if (string.IsNullOrWhiteSpace(dimensionIndex) || string.Equals(dimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase))
        {
            return new DimensionLayoutBlock
            {
                DimensionIndex = "Reality",
                Name = "Reality",
                ComplexType = ComplexType,
                ComplexResourceSet = ComplexResourceSet,
                DimensionGeomorph = ElevatorTile,
                LevelLayoutData = PersistentId,
                InternalEnabled = InternalEnabled
            };
        }

        return _dimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex, dimensionIndex, StringComparison.OrdinalIgnoreCase));
    }

    public void SetDimension(DimensionLayoutBlock dimension)
    {
        if (dimension == null) return;

        if (string.Equals(dimension.DimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase))
        {
            ComplexType = dimension.ComplexType;
            ComplexResourceSet = dimension.ComplexResourceSet;
            ElevatorTile = dimension.DimensionGeomorph;
            return;
        }

        var existing = _dimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex, dimension.DimensionIndex, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            int index = _dimensions.IndexOf(existing);
            _dimensions[index] = dimension;
        }
        else
        {
            _dimensions.Add(dimension);
        }
    }

    public bool RemoveDimension(string dimensionIndex)
    {
        if (string.IsNullOrWhiteSpace(dimensionIndex) || string.Equals(dimensionIndex, "Reality", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var existing = _dimensions.FirstOrDefault(d => string.Equals(d.DimensionIndex, dimensionIndex, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return _dimensions.Remove(existing);
        }
        return false;
    }

    public LevelLayoutBlock Clone()
    {
        var clone = new LevelLayoutBlock
        {
            Name = Name,
            InternalEnabled = InternalEnabled,
            PersistentId = PersistentId,
            ComplexType = ComplexType,
            ComplexResourceSet = ComplexResourceSet,
            FogSettings = FogSettings,
            ElevatorTile = ElevatorTile,
            Dimensions = new ObservableCollection<DimensionLayoutBlock>(_dimensions.Select(d => d.Clone())),
            MainSector = MainSector.Clone(),
            SecondarySector = SecondarySector.Clone(),
            OverloadSector = OverloadSector.Clone()
        };
        return clone;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
