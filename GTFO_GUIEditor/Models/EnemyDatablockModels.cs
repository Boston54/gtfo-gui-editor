using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class EnemyRoleData : INotifyPropertyChanged
{
    private string _role = string.Empty;
    private string _difficulty = "Easy";
    private uint _enemy;
    private double _cost = 1.0;
    private double _weight = 1.0;
    private EnemyEntry? _selectedEnemy;
    public ObservableCollection<EnemyEntry> EnemyEntries { get; } = new();

    [JsonPropertyName("Role")]
    public string Role { get => _role; set => SetField(ref _role, value); }

    [JsonPropertyName("Difficulty")]
    public string Difficulty { get => _difficulty; set => SetField(ref _difficulty, value); }

    [JsonPropertyName("Enemy")]
    public uint Enemy { get => _enemy; set => SetField(ref _enemy, value); }

    [JsonPropertyName("Cost")]
    public double Cost { get => _cost; set => SetField(ref _cost, value); }

    [JsonPropertyName("Weight")]
    public double Weight { get => _weight; set => SetField(ref _weight, value); }

    [JsonIgnore]
    public EnemyEntry? SelectedEnemy
    {
        get => _selectedEnemy;
        set
        {
            if (!SetField(ref _selectedEnemy, value)) return;
            if (value != null) Enemy = value.PersistentId;
        }
    }

    public EnemyRoleData Clone() => new()
    {
        Role = Role,
        Difficulty = Difficulty,
        Enemy = Enemy,
        Cost = Cost,
        Weight = Weight
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

public class EnemyPopulationBlock : INotifyPropertyChanged
{
    private bool _isExpanded;

    [JsonPropertyName("RoleDatas")]
    public ObservableCollection<EnemyRoleData> RoleDatas { get; set; } = new();

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    [JsonIgnore]
    public string DisplayText => "Enemy Population";

    public EnemyPopulationBlock Clone()
    {
        var clone = new EnemyPopulationBlock();
        foreach (var role in RoleDatas) clone.RoleDatas.Add(role.Clone());
        return clone;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class EnemyGroupRole : INotifyPropertyChanged
{
    private string _role = string.Empty;
    private string _distribution = "Rel_100";
    public ObservableCollection<string> AvailableRoles { get; } = new();

    [JsonPropertyName("Role")]
    public string Role { get => _role; set => SetField(ref _role, value); }

    [JsonPropertyName("Distribution")]
    public string Distribution { get => _distribution; set => SetField(ref _distribution, value); }

    public EnemyGroupRole Clone() => new() { Role = Role, Distribution = Distribution };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

public class EnemyGroupBlock : INotifyPropertyChanged
{
    private string _type = "genericType";
    private string _difficulty = "Easy";
    private string _spawnPlacementType = "Default";
    private double _maxScore;
    private double _scoreInAreaPaddingMulti = 3.0;
    private double _relativeWeight = 1.0;
    private string _name = string.Empty;
    private bool _internalEnabled = true;
    private uint _persistentId;
    private bool _isExpanded;
    [JsonIgnore]
    public ObservableCollection<string> AvailableRoles { get; } = new();

    [JsonPropertyName("Type")]
    public string Type { get => _type; set => SetField(ref _type, value); }

    [JsonPropertyName("Difficulty")]
    public string Difficulty { get => _difficulty; set => SetField(ref _difficulty, value); }

    [JsonPropertyName("SpawnPlacementType")]
    public string SpawnPlacementType { get => _spawnPlacementType; set => SetField(ref _spawnPlacementType, value); }

    [JsonPropertyName("MaxScore")]
    public double MaxScore { get => _maxScore; set => SetField(ref _maxScore, value); }

    [JsonPropertyName("ScoreInAreaPaddingMulti")]
    public double ScoreInAreaPaddingMulti { get => _scoreInAreaPaddingMulti; set => SetField(ref _scoreInAreaPaddingMulti, value); }

    [JsonPropertyName("RelativeWeight")]
    public double RelativeWeight { get => _relativeWeight; set => SetField(ref _relativeWeight, value); }

    [JsonPropertyName("Roles")]
    public ObservableCollection<EnemyGroupRole> Roles { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get => _name; set => SetField(ref _name, value); }

    [JsonPropertyName("internalEnabled")]
    public bool InternalEnabled { get => _internalEnabled; set => SetField(ref _internalEnabled, value); }

    [JsonPropertyName("persistentID")]
    public uint PersistentId { get => _persistentId; set => SetField(ref _persistentId, value); }

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Name) ? "Enemy Group" : Name;

    public EnemyGroupBlock Clone()
    {
        var clone = new EnemyGroupBlock
        {
            Type = "genericType",
            Difficulty = Difficulty,
            SpawnPlacementType = SpawnPlacementType,
            MaxScore = MaxScore,
            ScoreInAreaPaddingMulti = ScoreInAreaPaddingMulti,
            RelativeWeight = RelativeWeight,
            Name = Name,
            InternalEnabled = InternalEnabled,
            PersistentId = PersistentId
        };
        foreach (var role in Roles) clone.Roles.Add(role.Clone());
        return clone;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
