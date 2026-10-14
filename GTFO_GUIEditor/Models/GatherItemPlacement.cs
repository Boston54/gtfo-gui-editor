using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class GatherItemPlacement : INotifyPropertyChanged
{
    private int _itemNumber = 1;
    private string _itemPrefix = "Item";
    private ObservableCollection<CandidateZonePlacement> _candidateZones = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public GatherItemPlacement()
    {
        _candidateZones.CollectionChanged += CandidateZones_CollectionChanged;
        EnsureAtLeastOneCandidateZone();
    }

    private void EnsureAtLeastOneCandidateZone()
    {
        if (_candidateZones.Count == 0)
        {
            var defaultZone = new CandidateZonePlacement
            {
                DimensionIndex = "Reality",
                Zone = 0,
                PlacementStartWeight = 0.0,
                PlacementMiddleWeight = 0.0,
                PlacementEndWeight = 0.0
            };
            defaultZone.PropertyChanged += CandidateZone_PropertyChanged;
            _candidateZones.Add(defaultZone);
        }
    }

    private void CandidateZones_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CandidateZonePlacement item in e.NewItems)
            {
                item.PropertyChanged += CandidateZone_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (CandidateZonePlacement item in e.OldItems)
            {
                item.PropertyChanged -= CandidateZone_PropertyChanged;
            }
        }
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DimensionIndex));
        OnPropertyChanged(nameof(Zone));
        OnPropertyChanged(nameof(PlacementStartWeight));
        OnPropertyChanged(nameof(PlacementMiddleWeight));
        OnPropertyChanged(nameof(PlacementEndWeight));
    }

    private void CandidateZone_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DimensionIndex));
        OnPropertyChanged(nameof(Zone));
        OnPropertyChanged(nameof(PlacementStartWeight));
        OnPropertyChanged(nameof(PlacementMiddleWeight));
        OnPropertyChanged(nameof(PlacementEndWeight));
    }

    [JsonIgnore]
    public int ItemNumber
    {
        get => _itemNumber;
        set
        {
            if (SetField(ref _itemNumber, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonIgnore]
    public string ItemPrefix
    {
        get => _itemPrefix;
        set
        {
            if (SetField(ref _itemPrefix, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("CandidateZones")]
    public ObservableCollection<CandidateZonePlacement> CandidateZones
    {
        get => _candidateZones;
        set
        {
            var nonNullValue = value ?? new ObservableCollection<CandidateZonePlacement>();
            if (_candidateZones != null)
            {
                _candidateZones.CollectionChanged -= CandidateZones_CollectionChanged;
                foreach (var z in _candidateZones)
                {
                    z.PropertyChanged -= CandidateZone_PropertyChanged;
                }
            }
            _candidateZones = nonNullValue;
            _candidateZones.CollectionChanged += CandidateZones_CollectionChanged;
            foreach (var z in _candidateZones)
            {
                z.PropertyChanged += CandidateZone_PropertyChanged;
            }
            EnsureAtLeastOneCandidateZone();
            OnPropertyChanged(nameof(CandidateZones));
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(DimensionIndex));
            OnPropertyChanged(nameof(Zone));
            OnPropertyChanged(nameof(PlacementStartWeight));
            OnPropertyChanged(nameof(PlacementMiddleWeight));
            OnPropertyChanged(nameof(PlacementEndWeight));
        }
    }

    [JsonIgnore]
    public string DisplayTitle
    {
        get
        {
            if (_candidateZones.Count == 0)
                return $"{_itemPrefix} {_itemNumber} (Unassigned)";
            if (_candidateZones.Count == 1)
                return $"{_itemPrefix} {_itemNumber} ({_candidateZones[0].DimensionIndex}, Zone {_candidateZones[0].Zone})";
            return $"{_itemPrefix} {_itemNumber} ({_candidateZones.Count} Candidate Zones: {string.Join(", ", _candidateZones.Select(z => $"Z{z.Zone}"))})";
        }
    }

    [JsonPropertyName("DimensionIndex")]
    public string DimensionIndex
    {
        get => _candidateZones.FirstOrDefault()?.DimensionIndex ?? "Reality";
        set
        {
            EnsureAtLeastOneCandidateZone();
            if (_candidateZones[0].DimensionIndex != value)
            {
                _candidateZones[0].DimensionIndex = value;
                OnPropertyChanged(nameof(DimensionIndex));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("Zone")]
    public int Zone
    {
        get => _candidateZones.FirstOrDefault()?.Zone ?? 0;
        set
        {
            EnsureAtLeastOneCandidateZone();
            if (_candidateZones[0].Zone != value)
            {
                _candidateZones[0].Zone = value;
                OnPropertyChanged(nameof(Zone));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    [JsonPropertyName("PlacementStartWeight")]
    public double PlacementStartWeight
    {
        get => _candidateZones.FirstOrDefault()?.PlacementStartWeight ?? 0.0;
        set
        {
            EnsureAtLeastOneCandidateZone();
            if (_candidateZones[0].PlacementStartWeight != value)
            {
                _candidateZones[0].PlacementStartWeight = value;
                OnPropertyChanged(nameof(PlacementStartWeight));
            }
        }
    }

    [JsonPropertyName("PlacementMiddleWeight")]
    public double PlacementMiddleWeight
    {
        get => _candidateZones.FirstOrDefault()?.PlacementMiddleWeight ?? 0.0;
        set
        {
            EnsureAtLeastOneCandidateZone();
            if (_candidateZones[0].PlacementMiddleWeight != value)
            {
                _candidateZones[0].PlacementMiddleWeight = value;
                OnPropertyChanged(nameof(PlacementMiddleWeight));
            }
        }
    }

    [JsonPropertyName("PlacementEndWeight")]
    public double PlacementEndWeight
    {
        get => _candidateZones.FirstOrDefault()?.PlacementEndWeight ?? 0.0;
        set
        {
            EnsureAtLeastOneCandidateZone();
            if (_candidateZones[0].PlacementEndWeight != value)
            {
                _candidateZones[0].PlacementEndWeight = value;
                OnPropertyChanged(nameof(PlacementEndWeight));
            }
        }
    }

    public GatherItemPlacement Clone()
    {
        var clone = new GatherItemPlacement
        {
            ItemNumber = ItemNumber,
            ItemPrefix = ItemPrefix
        };
        clone.CandidateZones.Clear();
        foreach (var candidate in CandidateZones)
        {
            clone.CandidateZones.Add(candidate.Clone());
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
