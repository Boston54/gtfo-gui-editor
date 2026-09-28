using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class LevelGenConfig : INotifyPropertyChanged
{
    private int _gridSize = 40;
    private double _cellDimension = 64.0;
    private double _altitudeOffset = 6.0;
    private TransitionDirection _transitionDirection = TransitionDirection.FloorUp;
    private LevelProgression _levelProgression = LevelProgression.StartLevel;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("GridSize")]
    public int GridSize
    {
        get => _gridSize;
        set => SetField(ref _gridSize, value);
    }

    [JsonPropertyName("CellDimension")]
    public double CellDimension
    {
        get => _cellDimension;
        set => SetField(ref _cellDimension, value);
    }

    [JsonPropertyName("AltitudeOffset")]
    public double AltitudeOffset
    {
        get => _altitudeOffset;
        set => SetField(ref _altitudeOffset, value);
    }

    [JsonPropertyName("TransitionDirection")]
    public TransitionDirection TransitionDirection
    {
        get => _transitionDirection;
        set => SetField(ref _transitionDirection, value);
    }

    [JsonPropertyName("LevelProgression")]
    public LevelProgression LevelProgression
    {
        get => _levelProgression;
        set => SetField(ref _levelProgression, value);
    }

    public LevelGenConfig Clone()
    {
        return new LevelGenConfig
        {
            GridSize = GridSize,
            CellDimension = CellDimension,
            AltitudeOffset = AltitudeOffset,
            TransitionDirection = TransitionDirection,
            LevelProgression = LevelProgression
        };
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
