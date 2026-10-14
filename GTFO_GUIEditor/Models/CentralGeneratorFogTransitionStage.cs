using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class CentralGeneratorFogTransitionStage : INotifyPropertyChanged
{
    private int _stageNumber = 1;
    private uint _fogSetting = 0;
    private double _transitionDuration = 0.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("StageNumber")]
    public int StageNumber
    {
        get => _stageNumber;
        set
        {
            if (SetField(ref _stageNumber, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("FogSetting")]
    public uint FogSetting
    {
        get => _fogSetting;
        set
        {
            if (SetField(ref _fogSetting, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("TransitionDuration")]
    public double TransitionDuration
    {
        get => _transitionDuration;
        set
        {
            if (SetField(ref _transitionDuration, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonIgnore]
    public string DisplayTitle => $"Stage {_stageNumber} (Fog: {_fogSetting}, Duration: {_transitionDuration:0.##}s)";

    public CentralGeneratorFogTransitionStage Clone()
    {
        return new CentralGeneratorFogTransitionStage
        {
            StageNumber = StageNumber,
            FogSetting = FogSetting,
            TransitionDuration = TransitionDuration
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
