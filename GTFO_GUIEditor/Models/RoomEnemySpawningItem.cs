using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class RoomEnemySpawningItem : INotifyPropertyChanged
{
    private uint _enemyId = 28; // Default to Striker
    private int _count = 1;
    private double _placementRandomWeight = 0.25;
    private double _placementScore = 3.0;
    private bool _resetPlacementInfo = false;
    private Vector3Data _position = new();
    private Vector3Data _rotation = new();
    private bool _randomizeRotation = false;

    public RoomEnemySpawningItem()
    {
        _position.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Position));
        _rotation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Rotation));
    }

    public uint EnemyId
    {
        get => _enemyId;
        set
        {
            if (SetField(ref _enemyId, value))
            {
                OnPropertyChanged(nameof(IsScout));
                OnPropertyChanged(nameof(ScoutType));
            }
        }
    }

    public int Count
    {
        get => _count;
        set => SetField(ref _count, Math.Max(1, value));
    }

    public double PlacementRandomWeight
    {
        get => _placementRandomWeight;
        set => SetField(ref _placementRandomWeight, value);
    }

    public double PlacementScore
    {
        get => _placementScore;
        set => SetField(ref _placementScore, value);
    }

    public bool ResetPlacementInfo
    {
        get => _resetPlacementInfo;
        set => SetField(ref _resetPlacementInfo, value);
    }

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

    public Vector3Data Rotation
    {
        get => _rotation;
        set
        {
            var old = _rotation;
            if (SetField(ref _rotation, value ?? new Vector3Data()))
            {
                if (old != null) old.PropertyChanged -= (s, e) => OnPropertyChanged(nameof(Rotation));
                _rotation.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Rotation));
            }
        }
    }

    public bool RandomizeRotation
    {
        get => _randomizeRotation;
        set => SetField(ref _randomizeRotation, value);
    }

    [JsonIgnore]
    public bool IsScout => IsScoutId(EnemyId);

    [JsonIgnore]
    public ScoutType ScoutType => GetScoutTypeFromEnemyId(EnemyId);

    public static bool IsScoutId(uint id) => id is 20 or 40 or 41 or 54 or 56;

    public static ScoutType GetScoutTypeFromEnemyId(uint enemyId) => enemyId switch
    {
        41 => ScoutType.Charger_Scout,
        40 => ScoutType.Shadow_Scout,
        54 => ScoutType.Zoomer_Scout,
        56 => ScoutType.Nightmare_Scout,
        _ => ScoutType.Scout
    };

    public static uint GetEnemyIdFromScoutType(ScoutType scoutType) => scoutType switch
    {
        ScoutType.Charger_Scout => 41,
        ScoutType.Shadow_Scout => 40,
        ScoutType.Zoomer_Scout => 54,
        ScoutType.Nightmare_Scout => 56,
        _ => 20
    };

    public RoomEnemySpawningItem Clone()
    {
        return new RoomEnemySpawningItem
        {
            EnemyId = EnemyId,
            Count = Count,
            PlacementRandomWeight = PlacementRandomWeight,
            PlacementScore = PlacementScore,
            ResetPlacementInfo = ResetPlacementInfo,
            Position = Position.Clone(),
            Rotation = Rotation.Clone(),
            RandomizeRotation = RandomizeRotation
        };
    }

    public RoomEnemySpawningItemData ToData()
    {
        return new RoomEnemySpawningItemData
        {
            EnemyId = EnemyId,
            Count = Count,
            PlacementRandomWeight = PlacementRandomWeight,
            PlacementScore = PlacementScore,
            ResetPlacementInfo = ResetPlacementInfo,
            Position = Position.Clone(),
            Rotation = Rotation.Clone(),
            RandomizeRotation = RandomizeRotation
        };
    }

    public static RoomEnemySpawningItem FromData(RoomEnemySpawningItemData data)
    {
        return new RoomEnemySpawningItem
        {
            EnemyId = data.EnemyId,
            Count = data.Count > 0 ? data.Count : 1,
            PlacementRandomWeight = data.PlacementRandomWeight,
            PlacementScore = data.PlacementScore,
            ResetPlacementInfo = data.ResetPlacementInfo,
            Position = data.Position?.Clone() ?? new Vector3Data(),
            Rotation = data.Rotation?.Clone() ?? new Vector3Data(),
            RandomizeRotation = data.RandomizeRotation
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
