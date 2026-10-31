using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class DimensionLayoutBlock : INotifyPropertyChanged
{
    private string _dimensionIndex = "Dimension_1";
    private string _name = "Dimension 1";
    private bool _internalEnabled = true;
    private ComplexType _complexType = ComplexType.Mining;
    private uint _complexResourceSet;
    private string _dimensionGeomorph = string.Empty;
    private uint _levelLayoutData;
    private string _tileCategory = "Dimension Geomorph";
    private LevelLayoutGridState? _gridState;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("DimensionIndex")]
    public string DimensionIndex
    {
        get => _dimensionIndex;
        set
        {
            if (SetField(ref _dimensionIndex, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

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
        set
        {
            if (SetField(ref _complexResourceSet, value))
            {
                OnPropertyChanged(nameof(DimensionResourceSetID));
            }
        }
    }

    [JsonPropertyName("DimensionResourceSetID")]
    public uint DimensionResourceSetID
    {
        get => _complexResourceSet;
        set
        {
            if (SetField(ref _complexResourceSet, value))
            {
                OnPropertyChanged(nameof(ComplexResourceSet));
            }
        }
    }

    [JsonPropertyName("DimensionGeomorph")]
    public string DimensionGeomorph
    {
        get => _dimensionGeomorph;
        set => SetField(ref _dimensionGeomorph, value);
    }

    [JsonPropertyName("LevelLayoutData")]
    public uint LevelLayoutData
    {
        get => _levelLayoutData;
        set => SetField(ref _levelLayoutData, value);
    }

    [JsonPropertyName("TileCategory")]
    public string TileCategory
    {
        get => _tileCategory;
        set => SetField(ref _tileCategory, value);
    }

    [JsonPropertyName("gridState")]
    public LevelLayoutGridStateData? GridStateData
    {
        get => _gridState?.ExportStateData();
        set
        {
            if (value != null)
            {
                GridState.ImportStateData(value);
            }
        }
    }

    [JsonIgnore]
    public LevelLayoutGridState GridState
    {
        get
        {
            if (_gridState == null)
            {
                _gridState = new LevelLayoutGridState();
                if (!string.IsNullOrWhiteSpace(DimensionGeomorph))
                {
                    _gridState.InitializeStartingTile(DimensionGeomorph, 0, 0);
                }
            }
            return _gridState;
        }
        set => SetField(ref _gridState, value);
    }

    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Name)
        ? DimensionIndex
        : Name;

    public DimensionLayoutBlock Clone()
    {
        var clone = new DimensionLayoutBlock
        {
            DimensionIndex = DimensionIndex,
            Name = Name,
            InternalEnabled = InternalEnabled,
            ComplexType = ComplexType,
            ComplexResourceSet = ComplexResourceSet,
            DimensionGeomorph = DimensionGeomorph,
            LevelLayoutData = LevelLayoutData,
            TileCategory = TileCategory
        };
        if (_gridState != null)
        {
            clone.GridState = _gridState.Clone();
        }
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
