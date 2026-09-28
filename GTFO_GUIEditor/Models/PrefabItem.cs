using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class PrefabItem : INotifyPropertyChanged
{
    private string _prefab = string.Empty;
    private SubComplexType _subComplex = SubComplexType.All;
    private ShardType _shard = ShardType.S1;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Prefab")]
    public string Prefab
    {
        get => _prefab;
        set => SetField(ref _prefab, value);
    }

    [JsonPropertyName("SubComplex")]
    public SubComplexType SubComplex
    {
        get => _subComplex;
        set => SetField(ref _subComplex, value);
    }

    [JsonPropertyName("Shard")]
    public ShardType Shard
    {
        get => _shard;
        set => SetField(ref _shard, value);
    }

    public PrefabItem Clone()
    {
        return new PrefabItem
        {
            Prefab = Prefab,
            SubComplex = SubComplex,
            Shard = Shard
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
