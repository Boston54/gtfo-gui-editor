using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class EnemyExcludeItem : INotifyPropertyChanged
{
    private uint _enemyId;

    public uint EnemyId
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    public EnemyExcludeItem Clone() => new() { EnemyId = EnemyId };

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
