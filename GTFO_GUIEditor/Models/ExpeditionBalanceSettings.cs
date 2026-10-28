using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace GTFO_GUIEditor.Models;

public class TentacleTrapsSettings : INotifyPropertyChanged
{
    private double _health = 10.0;
    private double _attackDamage = 1.0;
    private int _maxPerZone = 3;
    private int _maxSmallArea = 1;
    private int _maxMediumArea = 1;
    private int _maxLargeArea = 1;
    private int _maxHugeArea = 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Health")]
    public double Health
    {
        get => _health;
        set => SetField(ref _health, value);
    }

    [JsonPropertyName("AttackDamage")]
    public double AttackDamage
    {
        get => _attackDamage;
        set => SetField(ref _attackDamage, value);
    }

    [JsonPropertyName("MaxPerZone")]
    public int MaxPerZone
    {
        get => _maxPerZone;
        set => SetField(ref _maxPerZone, value);
    }

    [JsonPropertyName("MaxSmallArea")]
    public int MaxSmallArea
    {
        get => _maxSmallArea;
        set => SetField(ref _maxSmallArea, value);
    }

    [JsonPropertyName("MaxMediumArea")]
    public int MaxMediumArea
    {
        get => _maxMediumArea;
        set => SetField(ref _maxMediumArea, value);
    }

    [JsonPropertyName("MaxLargeArea")]
    public int MaxLargeArea
    {
        get => _maxLargeArea;
        set => SetField(ref _maxLargeArea, value);
    }

    [JsonPropertyName("MaxHugeArea")]
    public int MaxHugeArea
    {
        get => _maxHugeArea;
        set => SetField(ref _maxHugeArea, value);
    }

    public TentacleTrapsSettings Clone()
    {
        return new TentacleTrapsSettings
        {
            Health = Health,
            AttackDamage = AttackDamage,
            MaxPerZone = MaxPerZone,
            MaxSmallArea = MaxSmallArea,
            MaxMediumArea = MaxMediumArea,
            MaxLargeArea = MaxLargeArea,
            MaxHugeArea = MaxHugeArea
        };
    }

    public JsonObject ToExportJsonObject()
    {
        return new JsonObject
        {
            ["Health"] = Health,
            ["AttackDamage"] = AttackDamage,
            ["MaxPerZone"] = MaxPerZone,
            ["MaxSmallArea"] = MaxSmallArea,
            ["MaxMediumArea"] = MaxMediumArea,
            ["MaxLargeArea"] = MaxLargeArea,
            ["MaxHugeArea"] = MaxHugeArea
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

public class ParasiteNestsSettings : INotifyPropertyChanged
{
    private double _health = 20.0;
    private double _attackDamage = 2.0;
    private int _maxPerZone = 7;
    private int _maxSmallArea = 2;
    private int _maxMediumArea = 3;
    private int _maxLargeArea = 4;
    private int _maxHugeArea = 4;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("Health")]
    public double Health
    {
        get => _health;
        set => SetField(ref _health, value);
    }

    [JsonPropertyName("AttackDamage")]
    public double AttackDamage
    {
        get => _attackDamage;
        set => SetField(ref _attackDamage, value);
    }

    [JsonPropertyName("MaxPerZone")]
    public int MaxPerZone
    {
        get => _maxPerZone;
        set => SetField(ref _maxPerZone, value);
    }

    [JsonPropertyName("MaxSmallArea")]
    public int MaxSmallArea
    {
        get => _maxSmallArea;
        set => SetField(ref _maxSmallArea, value);
    }

    [JsonPropertyName("MaxMediumArea")]
    public int MaxMediumArea
    {
        get => _maxMediumArea;
        set => SetField(ref _maxMediumArea, value);
    }

    [JsonPropertyName("MaxLargeArea")]
    public int MaxLargeArea
    {
        get => _maxLargeArea;
        set => SetField(ref _maxLargeArea, value);
    }

    [JsonPropertyName("MaxHugeArea")]
    public int MaxHugeArea
    {
        get => _maxHugeArea;
        set => SetField(ref _maxHugeArea, value);
    }

    public ParasiteNestsSettings Clone()
    {
        return new ParasiteNestsSettings
        {
            Health = Health,
            AttackDamage = AttackDamage,
            MaxPerZone = MaxPerZone,
            MaxSmallArea = MaxSmallArea,
            MaxMediumArea = MaxMediumArea,
            MaxLargeArea = MaxLargeArea,
            MaxHugeArea = MaxHugeArea
        };
    }

    public JsonObject ToExportJsonObject()
    {
        return new JsonObject
        {
            ["Health"] = Health,
            ["AttackDamage"] = AttackDamage,
            ["MaxPerZone"] = MaxPerZone,
            ["MaxSmallArea"] = MaxSmallArea,
            ["MaxMediumArea"] = MaxMediumArea,
            ["MaxLargeArea"] = MaxLargeArea,
            ["MaxHugeArea"] = MaxHugeArea
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

public class ExpeditionBalanceSettings : INotifyPropertyChanged
{
    // Resource Containers section
    private double _chanceToReUseResourceContainer = 0.5;
    private int _maxPacksPerResourceContainer = 4;
    private double _emptyWeakResourceContainersPerZone = 0.0;
    private double _emptySecureResourceContainersPerZone = 3.0;
    private double _weakResourceContainerWithPackChanceForLocked = 0.75;

    // Weak Doors section
    private double _weakDoor4x4Health = 7.0;
    private double _weakDoor8x4Health = 12.0;
    private double _weakDoorChanceLockWeightNoLock = 3.0;
    private double _weakDoorChanceLockWeightMeleeLock = 1.0;
    private double _weakDoorChanceLockWeightHackableLock = 1.0;
    private double _weakDoorLockHealth = 15.0;
    private double _glueVolumeToDoorHealthConversion = 0.5;
    private double _glueVolumeForDoorGlueMaxState = 27.0;

    // Artifacts section
    private int _artifactsPerSegment = 1;
    private int _artifactsPerLayer = 30;
    private double _chanceToPutArtifactInResourceContainer = 0.15;

    // Remaining vanilla datablock fields (persistentID 1 defaults)
    private double _healthPerZone = 1.0;
    private double _disinfectionPerZone = 1.0;
    private double _weaponAmmoPerZone = 1.0;
    private double _toolAmmoPerZone = 1.0;
    private double _commodityValuePerZone = 0.0;
    private double _chanceToPutCommodityInResourceContainer = 0.7;
    private double _chanceToSpawnCommodityLargePack = 0.3;
    private double _chanceToSpawnCommodityMediumPack = 0.5;
    private double _lootPerZone = 3.0;
    private double _airPerZone = 1.0;
    private double _airPerZoneInNoAir = 5.0;
    private double _terminalsPerZone = 1.0;
    private TentacleTrapsSettings _tentacleTraps = new();
    private ParasiteNestsSettings _parasiteNests = new();
    private int _enemyPatrolGroupsPerZone = 5;
    private int _staticEnemiesMaxPerZone = 10;
    private int _staticEnemiesMaxSmallArea = 1;
    private int _staticEnemiesMaxMediumArea = 2;
    private int _staticEnemiesMaxLargeArea = 3;
    private int _staticEnemiesMaxHugeArea = 5;
    private double _enemyPopulationPerZone = 25.0;
    private double _voxelCoverageAreaMultiplier = 0.037;
    private double _voxelCoverageAreaScoringRandomMultiplier = 0.25;
    private double _weakDoorUnlockedChanceForOpen = 0.0;
    private double _weakDoorOpenChanceForWallRemoverUsed = 0.0;
    private List<double> _resourcePackSizes = new() { 0.6, 1.0, 0.4 };

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Resource Containers ---
    [JsonPropertyName("ChanceToReUseResourceContainer")]
    public double ChanceToReUseResourceContainer
    {
        get => _chanceToReUseResourceContainer;
        set => SetField(ref _chanceToReUseResourceContainer, value);
    }

    [JsonPropertyName("MaxPacksPerResourceContainer")]
    public int MaxPacksPerResourceContainer
    {
        get => _maxPacksPerResourceContainer;
        set => SetField(ref _maxPacksPerResourceContainer, value);
    }

    [JsonPropertyName("EmptyWeakResourceContainersPerZone")]
    public double EmptyWeakResourceContainersPerZone
    {
        get => _emptyWeakResourceContainersPerZone;
        set => SetField(ref _emptyWeakResourceContainersPerZone, value);
    }

    [JsonPropertyName("EmptySecureResourceContainersPerZone")]
    public double EmptySecureResourceContainersPerZone
    {
        get => _emptySecureResourceContainersPerZone;
        set => SetField(ref _emptySecureResourceContainersPerZone, value);
    }

    [JsonPropertyName("WeakResourceContainerWithPackChanceForLocked")]
    public double WeakResourceContainerWithPackChanceForLocked
    {
        get => _weakResourceContainerWithPackChanceForLocked;
        set => SetField(ref _weakResourceContainerWithPackChanceForLocked, value);
    }

    // --- Weak Doors ---
    [JsonPropertyName("WeakDoor4x4Health")]
    public double WeakDoor4x4Health
    {
        get => _weakDoor4x4Health;
        set => SetField(ref _weakDoor4x4Health, value);
    }

    [JsonPropertyName("WeakDoor8x4Health")]
    public double WeakDoor8x4Health
    {
        get => _weakDoor8x4Health;
        set => SetField(ref _weakDoor8x4Health, value);
    }

    [JsonPropertyName("WeakDoorChanceLockWeightNoLock")]
    public double WeakDoorChanceLockWeightNoLock
    {
        get => _weakDoorChanceLockWeightNoLock;
        set => SetField(ref _weakDoorChanceLockWeightNoLock, value);
    }

    [JsonPropertyName("WeakDoorChanceLockWeightMeleeLock")]
    public double WeakDoorChanceLockWeightMeleeLock
    {
        get => _weakDoorChanceLockWeightMeleeLock;
        set => SetField(ref _weakDoorChanceLockWeightMeleeLock, value);
    }

    [JsonIgnore]
    public double WeakDoorChanceLockWeightMelee
    {
        get => WeakDoorChanceLockWeightMeleeLock;
        set => WeakDoorChanceLockWeightMeleeLock = value;
    }

    [JsonPropertyName("WeakDoorChanceLockWeightHackableLock")]
    public double WeakDoorChanceLockWeightHackableLock
    {
        get => _weakDoorChanceLockWeightHackableLock;
        set => SetField(ref _weakDoorChanceLockWeightHackableLock, value);
    }

    [JsonPropertyName("WeakDoorLockHealth")]
    public double WeakDoorLockHealth
    {
        get => _weakDoorLockHealth;
        set => SetField(ref _weakDoorLockHealth, value);
    }

    [JsonPropertyName("GlueVolumeToDoorHealthConversion")]
    public double GlueVolumeToDoorHealthConversion
    {
        get => _glueVolumeToDoorHealthConversion;
        set => SetField(ref _glueVolumeToDoorHealthConversion, value);
    }

    [JsonPropertyName("GlueVolumeForDoorGlueMaxState")]
    public double GlueVolumeForDoorGlueMaxState
    {
        get => _glueVolumeForDoorGlueMaxState;
        set => SetField(ref _glueVolumeForDoorGlueMaxState, value);
    }

    // --- Artifacts ---
    [JsonPropertyName("ArtifactsPerSegment")]
    public int ArtifactsPerSegment
    {
        get => _artifactsPerSegment;
        set => SetField(ref _artifactsPerSegment, value);
    }

    [JsonPropertyName("ArtifactsPerLayer")]
    public int ArtifactsPerLayer
    {
        get => _artifactsPerLayer;
        set => SetField(ref _artifactsPerLayer, value);
    }

    [JsonPropertyName("ChanceToPutArtifactInResourceContainer")]
    public double ChanceToPutArtifactInResourceContainer
    {
        get => _chanceToPutArtifactInResourceContainer;
        set => SetField(ref _chanceToPutArtifactInResourceContainer, value);
    }

    // --- Remaining Defaults ---
    [JsonPropertyName("HealthPerZone")]
    public double HealthPerZone
    {
        get => _healthPerZone;
        set => SetField(ref _healthPerZone, value);
    }

    [JsonPropertyName("DisinfectionPerZone")]
    public double DisinfectionPerZone
    {
        get => _disinfectionPerZone;
        set => SetField(ref _disinfectionPerZone, value);
    }

    [JsonPropertyName("WeaponAmmoPerZone")]
    public double WeaponAmmoPerZone
    {
        get => _weaponAmmoPerZone;
        set => SetField(ref _weaponAmmoPerZone, value);
    }

    [JsonPropertyName("ToolAmmoPerZone")]
    public double ToolAmmoPerZone
    {
        get => _toolAmmoPerZone;
        set => SetField(ref _toolAmmoPerZone, value);
    }

    [JsonPropertyName("CommodityValuePerZone")]
    public double CommodityValuePerZone
    {
        get => _commodityValuePerZone;
        set => SetField(ref _commodityValuePerZone, value);
    }

    [JsonPropertyName("ChanceToPutCommodityInResourceContainer")]
    public double ChanceToPutCommodityInResourceContainer
    {
        get => _chanceToPutCommodityInResourceContainer;
        set => SetField(ref _chanceToPutCommodityInResourceContainer, value);
    }

    [JsonPropertyName("ChanceToSpawnCommodityLargePack")]
    public double ChanceToSpawnCommodityLargePack
    {
        get => _chanceToSpawnCommodityLargePack;
        set => SetField(ref _chanceToSpawnCommodityLargePack, value);
    }

    [JsonPropertyName("ChanceToSpawnCommodityMediumPack")]
    public double ChanceToSpawnCommodityMediumPack
    {
        get => _chanceToSpawnCommodityMediumPack;
        set => SetField(ref _chanceToSpawnCommodityMediumPack, value);
    }

    [JsonPropertyName("LootPerZone")]
    public double LootPerZone
    {
        get => _lootPerZone;
        set => SetField(ref _lootPerZone, value);
    }

    [JsonPropertyName("AirPerZone")]
    public double AirPerZone
    {
        get => _airPerZone;
        set => SetField(ref _airPerZone, value);
    }

    [JsonPropertyName("AirPerZoneInNoAir")]
    public double AirPerZoneInNoAir
    {
        get => _airPerZoneInNoAir;
        set => SetField(ref _airPerZoneInNoAir, value);
    }

    [JsonPropertyName("TerminalsPerZone")]
    public double TerminalsPerZone
    {
        get => _terminalsPerZone;
        set => SetField(ref _terminalsPerZone, value);
    }

    [JsonPropertyName("TentacleTraps")]
    public TentacleTrapsSettings TentacleTraps
    {
        get => _tentacleTraps;
        set => SetField(ref _tentacleTraps, value ?? new TentacleTrapsSettings());
    }

    [JsonPropertyName("ParasiteNests")]
    public ParasiteNestsSettings ParasiteNests
    {
        get => _parasiteNests;
        set => SetField(ref _parasiteNests, value ?? new ParasiteNestsSettings());
    }

    [JsonPropertyName("EnemyPatrolGroupsPerZone")]
    public int EnemyPatrolGroupsPerZone
    {
        get => _enemyPatrolGroupsPerZone;
        set => SetField(ref _enemyPatrolGroupsPerZone, value);
    }

    [JsonPropertyName("StaticEnemiesMaxPerZone")]
    public int StaticEnemiesMaxPerZone
    {
        get => _staticEnemiesMaxPerZone;
        set => SetField(ref _staticEnemiesMaxPerZone, value);
    }

    [JsonPropertyName("StaticEnemiesMaxSmallArea")]
    public int StaticEnemiesMaxSmallArea
    {
        get => _staticEnemiesMaxSmallArea;
        set => SetField(ref _staticEnemiesMaxSmallArea, value);
    }

    [JsonPropertyName("StaticEnemiesMaxMediumArea")]
    public int StaticEnemiesMaxMediumArea
    {
        get => _staticEnemiesMaxMediumArea;
        set => SetField(ref _staticEnemiesMaxMediumArea, value);
    }

    [JsonPropertyName("StaticEnemiesMaxLargeArea")]
    public int StaticEnemiesMaxLargeArea
    {
        get => _staticEnemiesMaxLargeArea;
        set => SetField(ref _staticEnemiesMaxLargeArea, value);
    }

    [JsonPropertyName("StaticEnemiesMaxHugeArea")]
    public int StaticEnemiesMaxHugeArea
    {
        get => _staticEnemiesMaxHugeArea;
        set => SetField(ref _staticEnemiesMaxHugeArea, value);
    }

    [JsonPropertyName("EnemyPopulationPerZone")]
    public double EnemyPopulationPerZone
    {
        get => _enemyPopulationPerZone;
        set => SetField(ref _enemyPopulationPerZone, value);
    }

    [JsonPropertyName("VoxelCoverageAreaMultiplier")]
    public double VoxelCoverageAreaMultiplier
    {
        get => _voxelCoverageAreaMultiplier;
        set => SetField(ref _voxelCoverageAreaMultiplier, value);
    }

    [JsonPropertyName("VoxelCoverageAreaScoringRandomMultiplier")]
    public double VoxelCoverageAreaScoringRandomMultiplier
    {
        get => _voxelCoverageAreaScoringRandomMultiplier;
        set => SetField(ref _voxelCoverageAreaScoringRandomMultiplier, value);
    }

    [JsonPropertyName("WeakDoorUnlockedChanceForOpen")]
    public double WeakDoorUnlockedChanceForOpen
    {
        get => _weakDoorUnlockedChanceForOpen;
        set => SetField(ref _weakDoorUnlockedChanceForOpen, value);
    }

    [JsonPropertyName("WeakDoorOpenChanceForWallRemoverUsed")]
    public double WeakDoorOpenChanceForWallRemoverUsed
    {
        get => _weakDoorOpenChanceForWallRemoverUsed;
        set => SetField(ref _weakDoorOpenChanceForWallRemoverUsed, value);
    }

    [JsonPropertyName("ResourcePackSizes")]
    public List<double> ResourcePackSizes
    {
        get => _resourcePackSizes;
        set => SetField(ref _resourcePackSizes, value ?? new List<double>());
    }

    public ExpeditionBalanceSettings Clone()
    {
        return new ExpeditionBalanceSettings
        {
            ChanceToReUseResourceContainer = ChanceToReUseResourceContainer,
            MaxPacksPerResourceContainer = MaxPacksPerResourceContainer,
            EmptyWeakResourceContainersPerZone = EmptyWeakResourceContainersPerZone,
            EmptySecureResourceContainersPerZone = EmptySecureResourceContainersPerZone,
            WeakResourceContainerWithPackChanceForLocked = WeakResourceContainerWithPackChanceForLocked,

            WeakDoor4x4Health = WeakDoor4x4Health,
            WeakDoor8x4Health = WeakDoor8x4Health,
            WeakDoorChanceLockWeightNoLock = WeakDoorChanceLockWeightNoLock,
            WeakDoorChanceLockWeightMeleeLock = WeakDoorChanceLockWeightMeleeLock,
            WeakDoorChanceLockWeightHackableLock = WeakDoorChanceLockWeightHackableLock,
            WeakDoorLockHealth = WeakDoorLockHealth,
            GlueVolumeToDoorHealthConversion = GlueVolumeToDoorHealthConversion,
            GlueVolumeForDoorGlueMaxState = GlueVolumeForDoorGlueMaxState,

            ArtifactsPerSegment = ArtifactsPerSegment,
            ArtifactsPerLayer = ArtifactsPerLayer,
            ChanceToPutArtifactInResourceContainer = ChanceToPutArtifactInResourceContainer,

            HealthPerZone = HealthPerZone,
            DisinfectionPerZone = DisinfectionPerZone,
            WeaponAmmoPerZone = WeaponAmmoPerZone,
            ToolAmmoPerZone = ToolAmmoPerZone,
            CommodityValuePerZone = CommodityValuePerZone,
            ChanceToPutCommodityInResourceContainer = ChanceToPutCommodityInResourceContainer,
            ChanceToSpawnCommodityLargePack = ChanceToSpawnCommodityLargePack,
            ChanceToSpawnCommodityMediumPack = ChanceToSpawnCommodityMediumPack,
            LootPerZone = LootPerZone,
            AirPerZone = AirPerZone,
            AirPerZoneInNoAir = AirPerZoneInNoAir,
            TerminalsPerZone = TerminalsPerZone,
            TentacleTraps = TentacleTraps.Clone(),
            ParasiteNests = ParasiteNests.Clone(),
            EnemyPatrolGroupsPerZone = EnemyPatrolGroupsPerZone,
            StaticEnemiesMaxPerZone = StaticEnemiesMaxPerZone,
            StaticEnemiesMaxSmallArea = StaticEnemiesMaxSmallArea,
            StaticEnemiesMaxMediumArea = StaticEnemiesMaxMediumArea,
            StaticEnemiesMaxLargeArea = StaticEnemiesMaxLargeArea,
            StaticEnemiesMaxHugeArea = StaticEnemiesMaxHugeArea,
            EnemyPopulationPerZone = EnemyPopulationPerZone,
            VoxelCoverageAreaMultiplier = VoxelCoverageAreaMultiplier,
            VoxelCoverageAreaScoringRandomMultiplier = VoxelCoverageAreaScoringRandomMultiplier,
            WeakDoorUnlockedChanceForOpen = WeakDoorUnlockedChanceForOpen,
            WeakDoorOpenChanceForWallRemoverUsed = WeakDoorOpenChanceForWallRemoverUsed,
            ResourcePackSizes = new List<double>(ResourcePackSizes)
        };
    }

    public JsonObject ToExportJsonObject(uint persistentId, string name, bool internalEnabled = true)
    {
        return new JsonObject
        {
            ["HealthPerZone"] = HealthPerZone,
            ["DisinfectionPerZone"] = DisinfectionPerZone,
            ["WeaponAmmoPerZone"] = WeaponAmmoPerZone,
            ["ToolAmmoPerZone"] = ToolAmmoPerZone,
            ["CommodityValuePerZone"] = CommodityValuePerZone,
            ["ChanceToPutCommodityInResourceContainer"] = ChanceToPutCommodityInResourceContainer,
            ["ChanceToPutArtifactInResourceContainer"] = ChanceToPutArtifactInResourceContainer,
            ["ChanceToSpawnCommodityLargePack"] = ChanceToSpawnCommodityLargePack,
            ["ChanceToSpawnCommodityMediumPack"] = ChanceToSpawnCommodityMediumPack,
            ["ChanceToReUseResourceContainer"] = ChanceToReUseResourceContainer,
            ["MaxPacksPerResourceContainer"] = MaxPacksPerResourceContainer,
            ["EmptyWeakResourceContainersPerZone"] = EmptyWeakResourceContainersPerZone,
            ["EmptySecureResourceContainersPerZone"] = EmptySecureResourceContainersPerZone,
            ["LootPerZone"] = LootPerZone,
            ["AirPerZone"] = AirPerZone,
            ["AirPerZoneInNoAir"] = AirPerZoneInNoAir,
            ["TerminalsPerZone"] = TerminalsPerZone,
            ["TentacleTraps"] = TentacleTraps?.ToExportJsonObject() ?? new TentacleTrapsSettings().ToExportJsonObject(),
            ["ParasiteNests"] = ParasiteNests?.ToExportJsonObject() ?? new ParasiteNestsSettings().ToExportJsonObject(),
            ["EnemyPatrolGroupsPerZone"] = EnemyPatrolGroupsPerZone,
            ["StaticEnemiesMaxPerZone"] = StaticEnemiesMaxPerZone,
            ["StaticEnemiesMaxSmallArea"] = StaticEnemiesMaxSmallArea,
            ["StaticEnemiesMaxMediumArea"] = StaticEnemiesMaxMediumArea,
            ["StaticEnemiesMaxLargeArea"] = StaticEnemiesMaxLargeArea,
            ["StaticEnemiesMaxHugeArea"] = StaticEnemiesMaxHugeArea,
            ["EnemyPopulationPerZone"] = EnemyPopulationPerZone,
            ["VoxelCoverageAreaMultiplier"] = VoxelCoverageAreaMultiplier,
            ["VoxelCoverageAreaScoringRandomMultiplier"] = VoxelCoverageAreaScoringRandomMultiplier,
            ["ArtifactsPerSegment"] = ArtifactsPerSegment,
            ["ArtifactsPerLayer"] = ArtifactsPerLayer,
            ["WeakDoor4x4Health"] = WeakDoor4x4Health,
            ["WeakDoor8x4Health"] = WeakDoor8x4Health,
            ["WeakDoorChanceLockWeightNoLock"] = WeakDoorChanceLockWeightNoLock,
            ["WeakDoorChanceLockWeightMeleeLock"] = WeakDoorChanceLockWeightMeleeLock,
            ["WeakDoorChanceLockWeightHackableLock"] = WeakDoorChanceLockWeightHackableLock,
            ["WeakDoorUnlockedChanceForOpen"] = WeakDoorUnlockedChanceForOpen,
            ["WeakDoorOpenChanceForWallRemoverUsed"] = WeakDoorOpenChanceForWallRemoverUsed,
            ["WeakDoorLockHealth"] = WeakDoorLockHealth,
            ["WeakResourceContainerWithPackChanceForLocked"] = WeakResourceContainerWithPackChanceForLocked,
            ["ResourcePackSizes"] = new JsonArray(ResourcePackSizes.Select(s => (JsonNode)s).ToArray()),
            ["GlueVolumeToDoorHealthConversion"] = GlueVolumeToDoorHealthConversion,
            ["GlueVolumeForDoorGlueMaxState"] = GlueVolumeForDoorGlueMaxState,
            ["name"] = name,
            ["internalEnabled"] = internalEnabled,
            ["persistentID"] = persistentId
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
