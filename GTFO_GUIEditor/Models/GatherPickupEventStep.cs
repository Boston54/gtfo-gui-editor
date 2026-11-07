using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class GatherPickupEventStep : INotifyPropertyChanged
{
    private int _pickupNumber = 1;
    private ObservableCollection<WardenEvent> _events = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public GatherPickupEventStep()
    {
        _events.CollectionChanged += Events_CollectionChanged;
    }

    [JsonPropertyName("PickupNumber")]
    public int PickupNumber
    {
        get => _pickupNumber;
        set
        {
            if (SetField(ref _pickupNumber, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("Events")]
    public ObservableCollection<WardenEvent> Events
    {
        get => _events;
        set
        {
            var nonNull = value ?? new ObservableCollection<WardenEvent>();
            if (!ReferenceEquals(_events, nonNull))
            {
                _events.CollectionChanged -= Events_CollectionChanged;
                _events = nonNull;
                _events.CollectionChanged += Events_CollectionChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    private void Events_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayTitle));
    }

    [JsonIgnore]
    public string DisplayTitle
    {
        get
        {
            string suffix = (PickupNumber % 100) switch
            {
                11 or 12 or 13 => $"{PickupNumber}th",
                _ => (PickupNumber % 10) switch
                {
                    1 => $"{PickupNumber}st",
                    2 => $"{PickupNumber}nd",
                    3 => $"{PickupNumber}rd",
                    _ => $"{PickupNumber}th"
                }
            };
            int count = Events?.Count ?? 0;
            return $"Pickup #{PickupNumber} ({suffix} Item) - {count} Event{(count == 1 ? "" : "s")}";
        }
    }

    public GatherPickupEventStep Clone()
    {
        var clone = new GatherPickupEventStep
        {
            PickupNumber = PickupNumber
        };
        clone.Events.Clear();
        foreach (var ev in Events)
        {
            clone.Events.Add(ev.Clone());
        }
        return clone;
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
