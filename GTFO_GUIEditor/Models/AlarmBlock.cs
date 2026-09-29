using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class AlarmBlock : INotifyPropertyChanged
{
    private uint _persistentId;
    private string _name = "";
    private bool _internalEnabled = true;

    // Wave Roles (Enemy References)
    private uint _waveRoleWeakling = 0;
    private uint _waveRoleStandard = 0;
    private uint _waveRoleSpecial = 0;
    private uint _waveRoleMiniBoss = 0;
    private uint _waveRoleBoss = 0;

    // Survival Wave Settings
    private double _pauseBeforeStart = 0;
    private double _pauseBetweenGroups = 0;
    private double _wavePauseMin_atCost = 0;
    private double _wavePauseMax_atCost = 0;
    private double _wavePauseMin = 0;
    private double _wavePauseMax = 0;

    // Population Filters
    private WaveFilterType _filterType = WaveFilterType.Exclude;
    private bool _filterWeakling;
    private bool _filterStandard;
    private bool _filterSpecial;
    private bool _filterMiniBoss;
    private bool _filterBoss;

    // Spawn Direction & Spawn Type
    private double _chanceToRandomizeSpawnDirectionPerWave = 0;
    private double _chanceToRandomizeSpawnDirectionPerGroup = 0;
    private bool _overrideWaveSpawnType;
    private SurvivalWaveSpawnType _survivalWaveSpawnType = SurvivalWaveSpawnType.InRelationToClosestAlivePlayer;

    // Population Points & Ramp
    private double _populationPointsTotal = 0;
    private double _populationPointsPerWaveStart = 0;
    private double _populationPointsPerWaveEnd = 0;
    private double _populationPointsMinPerGroup = 0;
    private double _populationPointsPerGroupStart = 0;
    private double _populationPointsPerGroupEnd = 0;
    private double _populationRampOverTime = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    // Wave Roles
    [JsonPropertyName("WaveRoleWeakling")]
    public uint WaveRoleWeakling
    {
        get => _waveRoleWeakling;
        set => SetField(ref _waveRoleWeakling, value);
    }

    [JsonPropertyName("WaveRoleStandard")]
    public uint WaveRoleStandard
    {
        get => _waveRoleStandard;
        set => SetField(ref _waveRoleStandard, value);
    }

    [JsonPropertyName("WaveRoleSpecial")]
    public uint WaveRoleSpecial
    {
        get => _waveRoleSpecial;
        set => SetField(ref _waveRoleSpecial, value);
    }

    [JsonPropertyName("WaveRoleMiniBoss")]
    public uint WaveRoleMiniBoss
    {
        get => _waveRoleMiniBoss;
        set => SetField(ref _waveRoleMiniBoss, value);
    }

    [JsonPropertyName("WaveRoleBoss")]
    public uint WaveRoleBoss
    {
        get => _waveRoleBoss;
        set => SetField(ref _waveRoleBoss, value);
    }

    // Timing
    [JsonPropertyName("m_pauseBeforeStart")]
    public double PauseBeforeStart
    {
        get => _pauseBeforeStart;
        set => SetField(ref _pauseBeforeStart, value);
    }

    [JsonPropertyName("m_pauseBetweenGroups")]
    public double PauseBetweenGroups
    {
        get => _pauseBetweenGroups;
        set => SetField(ref _pauseBetweenGroups, value);
    }

    [JsonPropertyName("m_wavePauseMin_atCost")]
    public double WavePauseMin_AtCost
    {
        get => _wavePauseMin_atCost;
        set => SetField(ref _wavePauseMin_atCost, value);
    }

    [JsonPropertyName("m_wavePauseMax_atCost")]
    public double WavePauseMax_AtCost
    {
        get => _wavePauseMax_atCost;
        set => SetField(ref _wavePauseMax_atCost, value);
    }

    [JsonPropertyName("m_wavePauseMin")]
    public double WavePauseMin
    {
        get => _wavePauseMin;
        set => SetField(ref _wavePauseMin, value);
    }

    [JsonPropertyName("m_wavePauseMax")]
    public double WavePauseMax
    {
        get => _wavePauseMax;
        set => SetField(ref _wavePauseMax, value);
    }

    // Filtering
    [JsonPropertyName("m_filterType")]
    public WaveFilterType FilterType
    {
        get => _filterType;
        set => SetField(ref _filterType, value);
    }

    [JsonPropertyName("m_populationFilter")]
    public List<string> PopulationFilter
    {
        get
        {
            var list = new List<string>();
            if (_filterWeakling) list.Add("Weakling");
            if (_filterStandard) list.Add("Standard");
            if (_filterSpecial) list.Add("Special");
            if (_filterMiniBoss) list.Add("MiniBoss");
            if (_filterBoss) list.Add("Boss");
            return list;
        }
        set
        {
            var set = new HashSet<string>(value ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            FilterWeakling = set.Contains("Weakling");
            FilterStandard = set.Contains("Standard");
            FilterSpecial = set.Contains("Special");
            FilterMiniBoss = set.Contains("MiniBoss");
            FilterBoss = set.Contains("Boss");
            OnPropertyChanged(nameof(PopulationFilter));
        }
    }

    [JsonIgnore]
    public bool FilterWeakling
    {
        get => _filterWeakling;
        set
        {
            if (SetField(ref _filterWeakling, value))
            {
                OnPropertyChanged(nameof(PopulationFilter));
            }
        }
    }

    [JsonIgnore]
    public bool FilterStandard
    {
        get => _filterStandard;
        set
        {
            if (SetField(ref _filterStandard, value))
            {
                OnPropertyChanged(nameof(PopulationFilter));
            }
        }
    }

    [JsonIgnore]
    public bool FilterSpecial
    {
        get => _filterSpecial;
        set
        {
            if (SetField(ref _filterSpecial, value))
            {
                OnPropertyChanged(nameof(PopulationFilter));
            }
        }
    }

    [JsonIgnore]
    public bool FilterMiniBoss
    {
        get => _filterMiniBoss;
        set
        {
            if (SetField(ref _filterMiniBoss, value))
            {
                OnPropertyChanged(nameof(PopulationFilter));
            }
        }
    }

    [JsonIgnore]
    public bool FilterBoss
    {
        get => _filterBoss;
        set
        {
            if (SetField(ref _filterBoss, value))
            {
                OnPropertyChanged(nameof(PopulationFilter));
            }
        }
    }

    // Spawn Direction & Spawn Type
    [JsonPropertyName("m_chanceToRandomizeSpawnDirectionPerWave")]
    public double ChanceToRandomizeSpawnDirectionPerWave
    {
        get => _chanceToRandomizeSpawnDirectionPerWave;
        set => SetField(ref _chanceToRandomizeSpawnDirectionPerWave, value);
    }

    [JsonPropertyName("m_chanceToRandomizeSpawnDirectionPerGroup")]
    public double ChanceToRandomizeSpawnDirectionPerGroup
    {
        get => _chanceToRandomizeSpawnDirectionPerGroup;
        set => SetField(ref _chanceToRandomizeSpawnDirectionPerGroup, value);
    }

    [JsonPropertyName("m_overrideWaveSpawnType")]
    public bool OverrideWaveSpawnType
    {
        get => _overrideWaveSpawnType;
        set => SetField(ref _overrideWaveSpawnType, value);
    }

    [JsonPropertyName("m_survivalWaveSpawnType")]
    public SurvivalWaveSpawnType SurvivalWaveSpawnType
    {
        get => _survivalWaveSpawnType;
        set => SetField(ref _survivalWaveSpawnType, value);
    }

    // Population Points & Ramp
    [JsonPropertyName("m_populationPointsTotal")]
    public double PopulationPointsTotal
    {
        get => _populationPointsTotal;
        set => SetField(ref _populationPointsTotal, value);
    }

    [JsonPropertyName("m_populationPointsPerWaveStart")]
    public double PopulationPointsPerWaveStart
    {
        get => _populationPointsPerWaveStart;
        set => SetField(ref _populationPointsPerWaveStart, value);
    }

    [JsonPropertyName("m_populationPointsPerWaveEnd")]
    public double PopulationPointsPerWaveEnd
    {
        get => _populationPointsPerWaveEnd;
        set => SetField(ref _populationPointsPerWaveEnd, value);
    }

    [JsonPropertyName("m_populationPointsMinPerGroup")]
    public double PopulationPointsMinPerGroup
    {
        get => _populationPointsMinPerGroup;
        set => SetField(ref _populationPointsMinPerGroup, value);
    }

    [JsonPropertyName("m_populationPointsPerGroupStart")]
    public double PopulationPointsPerGroupStart
    {
        get => _populationPointsPerGroupStart;
        set => SetField(ref _populationPointsPerGroupStart, value);
    }

    [JsonPropertyName("m_populationPointsPerGroupEnd")]
    public double PopulationPointsPerGroupEnd
    {
        get => _populationPointsPerGroupEnd;
        set => SetField(ref _populationPointsPerGroupEnd, value);
    }

    [JsonPropertyName("m_populationRampOverTime")]
    public double PopulationRampOverTime
    {
        get => _populationRampOverTime;
        set => SetField(ref _populationRampOverTime, value);
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public string DisplayText => $"[{PersistentId}] {Name}";

    public AlarmBlock Clone()
    {
        return new AlarmBlock
        {
            PersistentId = PersistentId,
            Name = Name,
            InternalEnabled = InternalEnabled,

            WaveRoleWeakling = WaveRoleWeakling,
            WaveRoleStandard = WaveRoleStandard,
            WaveRoleSpecial = WaveRoleSpecial,
            WaveRoleMiniBoss = WaveRoleMiniBoss,
            WaveRoleBoss = WaveRoleBoss,

            PauseBeforeStart = PauseBeforeStart,
            PauseBetweenGroups = PauseBetweenGroups,
            WavePauseMin_AtCost = WavePauseMin_AtCost,
            WavePauseMax_AtCost = WavePauseMax_AtCost,
            WavePauseMin = WavePauseMin,
            WavePauseMax = WavePauseMax,

            FilterType = FilterType,
            FilterWeakling = FilterWeakling,
            FilterStandard = FilterStandard,
            FilterSpecial = FilterSpecial,
            FilterMiniBoss = FilterMiniBoss,
            FilterBoss = FilterBoss,

            ChanceToRandomizeSpawnDirectionPerWave = ChanceToRandomizeSpawnDirectionPerWave,
            ChanceToRandomizeSpawnDirectionPerGroup = ChanceToRandomizeSpawnDirectionPerGroup,
            OverrideWaveSpawnType = OverrideWaveSpawnType,
            SurvivalWaveSpawnType = SurvivalWaveSpawnType,

            PopulationPointsTotal = PopulationPointsTotal,
            PopulationPointsPerWaveStart = PopulationPointsPerWaveStart,
            PopulationPointsPerWaveEnd = PopulationPointsPerWaveEnd,
            PopulationPointsMinPerGroup = PopulationPointsMinPerGroup,
            PopulationPointsPerGroupStart = PopulationPointsPerGroupStart,
            PopulationPointsPerGroupEnd = PopulationPointsPerGroupEnd,
            PopulationRampOverTime = PopulationRampOverTime,

            ExtensionData = ExtensionData != null ? new Dictionary<string, JsonElement>(ExtensionData) : null
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
