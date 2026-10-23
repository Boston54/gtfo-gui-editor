using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class ConsumableDistributionBlock : INotifyPropertyChanged
{
    private uint _persistentId;
    private string _name = string.Empty;
    private bool _internalEnabled = true;
    private int _spawnsPerZone = 5;
    private double _chanceToSpawnInResourceContainer = 0.8;
    private ObservableCollection<ConsumableDistributionItem> _spawnData = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConsumableDistributionBlock()
    {
    }

    [JsonPropertyName("persistentID")]
    public uint PersistentId
    {
        get => _persistentId;
        set
        {
            if (SetField(ref _persistentId, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled
    {
        get => _internalEnabled;
        set => SetField(ref _internalEnabled, value);
    }

    [JsonPropertyName("SpawnsPerZone")]
    public int SpawnsPerZone
    {
        get => _spawnsPerZone;
        set => SetField(ref _spawnsPerZone, value);
    }

    [JsonPropertyName("ChanceToSpawnInResourceContainer")]
    public double ChanceToSpawnInResourceContainer
    {
        get => _chanceToSpawnInResourceContainer;
        set => SetField(ref _chanceToSpawnInResourceContainer, value);
    }

    [JsonPropertyName("SpawnData")]
    public ObservableCollection<ConsumableDistributionItem> SpawnData
    {
        get => _spawnData;
        set => SetField(ref _spawnData, value);
    }

    [JsonIgnore]
    public string DisplayText => $"[{PersistentId}] {Name}";

    public ConsumableDistributionBlock Clone()
    {
        return new ConsumableDistributionBlock
        {
            PersistentId = PersistentId,
            Name = Name,
            InternalEnabled = InternalEnabled,
            SpawnsPerZone = SpawnsPerZone,
            ChanceToSpawnInResourceContainer = ChanceToSpawnInResourceContainer,
            SpawnData = new ObservableCollection<ConsumableDistributionItem>(
                SpawnData.Select(i => i.Clone()))
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

public class ConsumableDistributionItem : INotifyPropertyChanged
{
    private Consumables _item = Consumables.Glowstick;
    private double _weight = 1.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConsumableDistributionItem()
    {
    }

    public ConsumableDistributionItem(Consumables item, double weight)
    {
        _item = item;
        _weight = weight;
    }

    [JsonPropertyName("ItemID")]
    public uint ItemId
    {
        get => (uint)_item;
        set
        {
            if (Enum.IsDefined(typeof(Consumables), (int)value))
            {
                Item = (Consumables)value;
            }
            else
            {
                Item = (Consumables)value;
            }
        }
    }

    [JsonIgnore]
    public Consumables Item
    {
        get => _item;
        set
        {
            if (SetField(ref _item, value))
            {
                OnPropertyChanged(nameof(ItemId));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("Weight")]
    public double Weight
    {
        get => _weight;
        set => SetField(ref _weight, value);
    }

    [JsonIgnore]
    public string DisplayName => Item.ToString();

    public ConsumableDistributionItem Clone()
    {
        return new ConsumableDistributionItem
        {
            Item = Item,
            Weight = Weight
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
