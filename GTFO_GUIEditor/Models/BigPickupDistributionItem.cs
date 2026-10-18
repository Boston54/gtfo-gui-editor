using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTFO_GUIEditor.Models;

public class BigPickupDistributionItem : INotifyPropertyChanged
{
    private RetrieveBigItemType _itemType = RetrieveBigItemType.PowerCell;

    public RetrieveBigItemType ItemType
    {
        get => _itemType;
        set => SetField(ref _itemType, value);
    }

    public BigPickupDistributionItem Clone() => new() { ItemType = ItemType };

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
