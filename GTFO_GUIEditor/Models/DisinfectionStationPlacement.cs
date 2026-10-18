using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class DisinfectionStationPlacement : INotifyPropertyChanged
{
    private double _placementStartWeight;
    private double _placementMiddleWeight;
    private double _placementEndWeight;

    public double PlacementStartWeight
    {
        get => _placementStartWeight;
        set => SetField(ref _placementStartWeight, value);
    }

    public double PlacementMiddleWeight
    {
        get => _placementMiddleWeight;
        set => SetField(ref _placementMiddleWeight, value);
    }

    public double PlacementEndWeight
    {
        get => _placementEndWeight;
        set => SetField(ref _placementEndWeight, value);
    }

    public DisinfectionStationPlacement Clone() => new()
    {
        PlacementStartWeight = PlacementStartWeight,
        PlacementMiddleWeight = PlacementMiddleWeight,
        PlacementEndWeight = PlacementEndWeight
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
