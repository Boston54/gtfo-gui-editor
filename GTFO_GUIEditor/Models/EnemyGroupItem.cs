using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class EnemyGroupItem : INotifyPropertyChanged
{
    private uint _enemyId;
    private int _count = 1;

    [JsonPropertyName("enemyId")]
    public uint EnemyId
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    [JsonPropertyName("count")]
    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    public EnemyGroupItem Clone() => new() { EnemyId = EnemyId, Count = Count };

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
