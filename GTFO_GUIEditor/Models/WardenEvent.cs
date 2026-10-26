using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.ViewModels;

namespace GTFO_GUIEditor.Models;

public static class WardenEventVisibilityConfig
{
    public static readonly HashSet<EventType> ChainPuzzleTypes = new()
    {
        EventType.ActivateChainedPuzzle
    };

    public static readonly HashSet<EventType> UseStaticBioscanPointsTypes = new()
    {
        EventType.ActivateChainedPuzzle
    };

    public static readonly HashSet<EventType> DurationTypes = new()
    {
        EventType.AddToTimer,
        EventType.DimensionFlashTeam,
        EventType.LightsInZone
    };

    public static readonly HashSet<EventType> ClearDimensionTypes = new()
    {
        EventType.DimensionWarpTeam
    };

    public static readonly HashSet<EventType> CustomSubObjectiveTypes = new()
    {
        EventType.UpdateCustomSubObjective
    };

    public static readonly HashSet<EventType> FogSettingTypes = new()
    {
        EventType.SetFogSetting
    };

    public static readonly HashSet<EventType> FogTransitionDurationTypes = new()
    {
        EventType.SetFogSetting
    };

    public static readonly HashSet<EventType> EnemyWaveDataTypes = new()
    {
        EventType.SpawnEnemyWave
    };

    public static readonly HashSet<EventType> EnemyIdTypes = new()
    {
        EventType.SpawnEnemyOnPoint
    };

    public static readonly HashSet<EventType> PositionTypes = new()
    {
        EventType.LightsInZone
    };

    public static readonly HashSet<EventType> CountTypes = new()
    {
        EventType.SpawnEnemyOnPoint
    };

    public static readonly HashSet<EventType> WorldEventObjectFilterTypes = new()
    {
        EventType.SpawnEnemyOnPoint,
        EventType.SetNavMarker
    };

    public static bool IsChainPuzzleVisible(EventType type) => ChainPuzzleTypes.Contains(type);
    public static bool IsUseStaticBioscanPointsVisible(EventType type) => UseStaticBioscanPointsTypes.Contains(type);
    public static bool IsDurationVisible(EventType type) => DurationTypes.Contains(type);
    public static bool IsClearDimensionVisible(EventType type) => ClearDimensionTypes.Contains(type);
    public static bool IsCustomSubObjectiveVisible(EventType type) => CustomSubObjectiveTypes.Contains(type);
    public static bool IsFogSettingVisible(EventType type) => FogSettingTypes.Contains(type);
    public static bool IsFogTransitionDurationVisible(EventType type) => FogTransitionDurationTypes.Contains(type);
    public static bool IsEnemyWaveDataVisible(EventType type) => EnemyWaveDataTypes.Contains(type);
    public static bool IsEnemyIdVisible(EventType type) => EnemyIdTypes.Contains(type);
    public static bool IsPositionVisible(EventType type) => PositionTypes.Contains(type);
    public static bool IsCountVisible(EventType type) => CountTypes.Contains(type);
    public static bool IsWorldEventObjectFilterVisible(EventType type) => WorldEventObjectFilterTypes.Contains(type);
}

public class WardenEvent : INotifyPropertyChanged
{
    private EventType _type = EventType.None;
    private EventTrigger _trigger = EventTrigger.None;
    private uint _chainPuzzle;
    private bool _useStaticBioscanPoints;
    private SectorType _layer = SectorType.Main;
    private string _dimension = "Reality";
    private int _zone;
    private double _delay;
    private double _duration;
    private bool _clearDimension;
    private string _wardenIntel = string.Empty;
    private string _customSubObjectiveHeader = string.Empty;
    private string _customSubObjective = string.Empty;
    private uint _soundId;
    private string _soundSubtitle = string.Empty;
    private uint _fogSetting;
    private int _fogTransitionDuration;
    private GenericWaveSettings _enemyWaveData = new();
    private uint _enemyId;
    private Vector3Data _position = new();
    private int _count = 1;
    private bool _enabled = true;
    private string _worldEventObjectFilter = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WardenEvent()
    {
        _enemyWaveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
        _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
    }

    [JsonPropertyName("Type")]
    public EventType Type
    {
        get => _type;
        set
        {
            if (SetField(ref _type, value))
            {
                NotifyVisibilityChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("Trigger")]
    public EventTrigger Trigger
    {
        get => _trigger;
        set => SetField(ref _trigger, value);
    }

    [JsonPropertyName("ChainPuzzle")]
    public uint ChainPuzzle
    {
        get => _chainPuzzle;
        set => SetField(ref _chainPuzzle, value);
    }

    [JsonPropertyName("UseStaticBioscanPoints")]
    public bool UseStaticBioscanPoints
    {
        get => _useStaticBioscanPoints;
        set => SetField(ref _useStaticBioscanPoints, value);
    }

    [JsonPropertyName("Layer")]
    public SectorType Layer
    {
        get => _layer;
        set
        {
            if (SetField(ref _layer, value))
            {
                OnPropertyChanged(nameof(AvailableZones));
            }
        }
    }

    [JsonPropertyName("Dimension")]
    public string Dimension
    {
        get => _dimension;
        set
        {
            if (SetField(ref _dimension, value ?? "Reality"))
            {
                OnPropertyChanged(nameof(AvailableZones));
            }
        }
    }

    [JsonPropertyName("Zone")]
    public int Zone
    {
        get => _zone;
        set => SetField(ref _zone, value);
    }

    [JsonIgnore]
    public IEnumerable<ZoneOption> AvailableZones
    {
        get
        {
            var level = LevelLayoutViewModel.Instance?.SelectedLevel;
            var list = new List<ZoneOption>();
            bool isReality = string.IsNullOrWhiteSpace(Dimension) || string.Equals(Dimension, "Reality", StringComparison.OrdinalIgnoreCase);

            if (level != null && isReality)
            {
                var sectorZones = level.GridState.GetZonesForSector(Layer);
                if (sectorZones.Count == 0)
                {
                    sectorZones.Add(0);
                }

                var sectorSettings = level.GetSectorSettings(Layer);
                foreach (int z in sectorZones)
                {
                    list.Add(new ZoneOption
                    {
                        ZoneIndex = z,
                        DisplayName = sectorSettings.GetZoneDisplayName(z)
                    });
                }
            }
            else if (level != null && !isReality)
            {
                list.Add(new ZoneOption
                {
                    ZoneIndex = 0,
                    DisplayName = "Zone 0"
                });
            }
            else
            {
                list.Add(new ZoneOption
                {
                    ZoneIndex = 0,
                    DisplayName = "Zone 0"
                });
            }

            if (!list.Any(zo => zo.ZoneIndex == Zone))
            {
                string disp = level != null && isReality
                    ? level.GetSectorSettings(Layer).GetZoneDisplayName(Zone)
                    : $"Zone {Zone}";
                list.Add(new ZoneOption
                {
                    ZoneIndex = Zone,
                    DisplayName = disp
                });
                list = list.OrderBy(zo => zo.ZoneIndex).ToList();
            }

            return list;
        }
    }

    [JsonPropertyName("Delay")]
    public double Delay
    {
        get => _delay;
        set => SetField(ref _delay, value);
    }

    [JsonPropertyName("Duration")]
    public double Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }

    [JsonPropertyName("ClearDimension")]
    public bool ClearDimension
    {
        get => _clearDimension;
        set => SetField(ref _clearDimension, value);
    }

    [JsonPropertyName("WardenIntel")]
    public string WardenIntel
    {
        get => _wardenIntel;
        set => SetField(ref _wardenIntel, value ?? string.Empty);
    }

    [JsonPropertyName("CustomSubObjectiveHeader")]
    public string CustomSubObjectiveHeader
    {
        get => _customSubObjectiveHeader;
        set => SetField(ref _customSubObjectiveHeader, value ?? string.Empty);
    }

    [JsonPropertyName("CustomSubObjective")]
    public string CustomSubObjective
    {
        get => _customSubObjective;
        set => SetField(ref _customSubObjective, value ?? string.Empty);
    }

    [JsonPropertyName("SoundID")]
    public uint SoundID
    {
        get => _soundId;
        set => SetField(ref _soundId, value);
    }

    [JsonPropertyName("SoundSubtitle")]
    public string SoundSubtitle
    {
        get => _soundSubtitle;
        set => SetField(ref _soundSubtitle, value ?? string.Empty);
    }

    [JsonPropertyName("FogSetting")]
    public uint FogSetting
    {
        get => _fogSetting;
        set => SetField(ref _fogSetting, value);
    }

    [JsonPropertyName("FogTransitionDuration")]
    public int FogTransitionDuration
    {
        get => _fogTransitionDuration;
        set => SetField(ref _fogTransitionDuration, value);
    }

    [JsonPropertyName("EnemyWaveData")]
    public GenericWaveSettings EnemyWaveData
    {
        get => _enemyWaveData;
        set
        {
            var old = _enemyWaveData;
            if (SetField(ref _enemyWaveData, value ?? new GenericWaveSettings()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
                _enemyWaveData.PropertyChanged += (s, e) => OnPropertyChanged(nameof(EnemyWaveData));
            }
        }
    }

    [JsonPropertyName("EnemyID")]
    public uint EnemyID
    {
        get => _enemyId;
        set => SetField(ref _enemyId, value);
    }

    [JsonPropertyName("Position")]
    public Vector3Data Position
    {
        get => _position;
        set
        {
            var old = _position;
            if (SetField(ref _position, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Position));
                _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
            }
        }
    }

    [JsonPropertyName("Count")]
    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    [JsonPropertyName("Enabled")]
    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    [JsonPropertyName("WorldEventObjectFilter")]
    public string WorldEventObjectFilter
    {
        get => _worldEventObjectFilter;
        set => SetField(ref _worldEventObjectFilter, value ?? string.Empty);
    }

    [JsonIgnore]
    public bool IsChainPuzzleVisible => WardenEventVisibilityConfig.IsChainPuzzleVisible(Type);

    [JsonIgnore]
    public bool IsUseStaticBioscanPointsVisible => WardenEventVisibilityConfig.IsUseStaticBioscanPointsVisible(Type);

    [JsonIgnore]
    public bool IsDurationVisible => WardenEventVisibilityConfig.IsDurationVisible(Type);

    [JsonIgnore]
    public bool IsClearDimensionVisible => WardenEventVisibilityConfig.IsClearDimensionVisible(Type);

    [JsonIgnore]
    public bool IsCustomSubObjectiveVisible => WardenEventVisibilityConfig.IsCustomSubObjectiveVisible(Type);

    [JsonIgnore]
    public bool IsCustomSubObjectiveHeaderVisible => WardenEventVisibilityConfig.IsCustomSubObjectiveVisible(Type);

    [JsonIgnore]
    public bool IsFogSettingVisible => WardenEventVisibilityConfig.IsFogSettingVisible(Type);

    [JsonIgnore]
    public bool IsFogTransitionDurationVisible => WardenEventVisibilityConfig.IsFogTransitionDurationVisible(Type);

    [JsonIgnore]
    public bool IsEnemyWaveDataVisible => WardenEventVisibilityConfig.IsEnemyWaveDataVisible(Type);

    [JsonIgnore]
    public bool IsEnemyIdVisible => WardenEventVisibilityConfig.IsEnemyIdVisible(Type);

    [JsonIgnore]
    public bool IsPositionVisible => WardenEventVisibilityConfig.IsPositionVisible(Type);

    [JsonIgnore]
    public bool IsCountVisible => WardenEventVisibilityConfig.IsCountVisible(Type);

    [JsonIgnore]
    public bool IsWorldEventObjectFilterVisible => WardenEventVisibilityConfig.IsWorldEventObjectFilterVisible(Type);

    [JsonIgnore]
    public string DisplayName => $"Event: {Type}" + (Delay > 0 ? $" (Delay: {Delay}s)" : string.Empty);

    public void NotifyVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsChainPuzzleVisible));
        OnPropertyChanged(nameof(IsUseStaticBioscanPointsVisible));
        OnPropertyChanged(nameof(IsDurationVisible));
        OnPropertyChanged(nameof(IsClearDimensionVisible));
        OnPropertyChanged(nameof(IsCustomSubObjectiveVisible));
        OnPropertyChanged(nameof(IsCustomSubObjectiveHeaderVisible));
        OnPropertyChanged(nameof(IsFogSettingVisible));
        OnPropertyChanged(nameof(IsFogTransitionDurationVisible));
        OnPropertyChanged(nameof(IsEnemyWaveDataVisible));
        OnPropertyChanged(nameof(IsEnemyIdVisible));
        OnPropertyChanged(nameof(IsPositionVisible));
        OnPropertyChanged(nameof(IsCountVisible));
        OnPropertyChanged(nameof(IsWorldEventObjectFilterVisible));
    }

    public WardenEvent Clone()
    {
        return new WardenEvent
        {
            Type = Type,
            Trigger = Trigger,
            ChainPuzzle = ChainPuzzle,
            UseStaticBioscanPoints = UseStaticBioscanPoints,
            Layer = Layer,
            Dimension = Dimension,
            Zone = Zone,
            Delay = Delay,
            Duration = Duration,
            ClearDimension = ClearDimension,
            WardenIntel = WardenIntel,
            CustomSubObjectiveHeader = CustomSubObjectiveHeader,
            CustomSubObjective = CustomSubObjective,
            SoundID = SoundID,
            SoundSubtitle = SoundSubtitle,
            FogSetting = FogSetting,
            FogTransitionDuration = FogTransitionDuration,
            EnemyWaveData = EnemyWaveData?.Clone() ?? new GenericWaveSettings(),
            EnemyID = EnemyID,
            Position = Position?.Clone() ?? new Vector3Data(),
            Count = Count,
            Enabled = Enabled,
            WorldEventObjectFilter = WorldEventObjectFilter
        };
    }

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
