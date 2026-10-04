using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class DimensionOption : INotifyPropertyChanged
{
    private string _dimensionIndex = "Reality";
    private string _displayName = "Reality";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DimensionIndex
    {
        get => _dimensionIndex;
        set => SetField(ref _dimensionIndex, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public DimensionOption()
    {
    }

    public DimensionOption(string dimensionIndex, string displayName)
    {
        _dimensionIndex = dimensionIndex;
        _displayName = displayName;
    }

    public override string ToString() => DisplayName;

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
