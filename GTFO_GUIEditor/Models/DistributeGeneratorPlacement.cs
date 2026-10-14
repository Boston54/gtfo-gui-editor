using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class DistributeGeneratorPlacement : INotifyPropertyChanged
{
    private int _generatorNumber = 1;
    private int _zone = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("GeneratorNumber")]
    public int GeneratorNumber
    {
        get => _generatorNumber;
        set
        {
            if (SetField(ref _generatorNumber, value))
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

    [JsonIgnore]
    public string DisplayTitle => $"Generator {_generatorNumber} (Zone {_zone})";

    public DistributeGeneratorPlacement Clone()
    {
        return new DistributeGeneratorPlacement
        {
            GeneratorNumber = GeneratorNumber,
            Zone = Zone
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
