using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ZonePlacementWeights : INotifyPropertyChanged
{
    private double _start;
    private double _middle;
    private double _end;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double Start { get => _start; set => SetField(ref _start, value); }
    public double Middle { get => _middle; set => SetField(ref _middle, value); }
    public double End { get => _end; set => SetField(ref _end, value); }

    public ZonePlacementWeights Clone() => new()
    {
        Start = Start,
        Middle = Middle,
        End = End
    };

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class ZonePlacementData : INotifyPropertyChanged
{
    private int _dimensionIndex;
    private int _localIndex;
    private ZonePlacementWeights _weights = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public int DimensionIndex { get => _dimensionIndex; set => SetField(ref _dimensionIndex, value); }
    public int LocalIndex { get => _localIndex; set => SetField(ref _localIndex, value); }
    public ZonePlacementWeights Weights { get => _weights; set => SetField(ref _weights, value ?? new ZonePlacementWeights()); }

    public ZonePlacementData Clone() => new()
    {
        DimensionIndex = DimensionIndex,
        LocalIndex = LocalIndex,
        Weights = Weights?.Clone() ?? new ZonePlacementWeights()
    };

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class LevelObjectiveRefData
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WinCondition WinCondition { get; set; } = WinCondition.GoToElevator;

    [JsonPropertyName("ZonePlacementDatas")]
    public ObservableCollection<ObservableCollection<ZonePlacementData>> ZonePlacementDatas { get; set; } = new();

    public LevelObjectiveRefData Clone() => new()
    {
        WinCondition = WinCondition,
        ZonePlacementDatas = new ObservableCollection<ObservableCollection<ZonePlacementData>>(
            ZonePlacementDatas.Select(group => new ObservableCollection<ZonePlacementData>(
                group.Select(placement => placement?.Clone() ?? new ZonePlacementData()))))
    };
}
