using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class TerminalPostCommandOutput : INotifyPropertyChanged
{
    private TerminalLineType _lineType = TerminalLineType.Normal;
    private double _time;
    private string _output = string.Empty;

    public TerminalLineType LineType
    {
        get => _lineType;
        set => SetField(ref _lineType, value);
    }

    public double Time
    {
        get => _time;
        set => SetField(ref _time, value);
    }

    public string Output
    {
        get => _output;
        set => SetField(ref _output, value);
    }

    public TerminalPostCommandOutput Clone() => new()
    {
        LineType = LineType,
        Time = Time,
        Output = Output
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
