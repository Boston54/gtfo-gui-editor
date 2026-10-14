using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class CandidateZonePlacement : INotifyPropertyChanged
{
    private string _dimensionIndex = "Reality";
    private int _zone = 0;
    private double _placementStartWeight = 0.0;
    private double _placementMiddleWeight = 0.0;
    private double _placementEndWeight = 0.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("DimensionIndex")]
    public string DimensionIndex
    {
        get => _dimensionIndex;
        set
        {
            if (SetField(ref _dimensionIndex, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("Zone")]
    public int Zone
    {
        get => _zone;
        set
        {
            if (SetField(ref _zone, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("PlacementStartWeight")]
    public double PlacementStartWeight
    {
        get => _placementStartWeight;
        set
        {
            if (SetField(ref _placementStartWeight, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("PlacementMiddleWeight")]
    public double PlacementMiddleWeight
    {
        get => _placementMiddleWeight;
        set
        {
            if (SetField(ref _placementMiddleWeight, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("PlacementEndWeight")]
    public double PlacementEndWeight
    {
        get => _placementEndWeight;
        set
        {
            if (SetField(ref _placementEndWeight, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonIgnore]
    public string DisplayTitle => $"Zone {_zone} ({_dimensionIndex}) - Weights [{_placementStartWeight:0.##}/{_placementMiddleWeight:0.##}/{_placementEndWeight:0.##}]";

    public CandidateZonePlacement Clone()
    {
        return new CandidateZonePlacement
        {
            DimensionIndex = DimensionIndex,
            Zone = Zone,
            PlacementStartWeight = PlacementStartWeight,
            PlacementMiddleWeight = PlacementMiddleWeight,
            PlacementEndWeight = PlacementEndWeight
        };
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
