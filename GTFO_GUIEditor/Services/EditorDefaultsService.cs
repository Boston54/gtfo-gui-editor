using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class EditorDefaultsService
{
    private static readonly object _lock = new();
    private static EditorDefaultsRoot? _cachedDefaults;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static string GetDataDirectory()
    {
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        if (Directory.Exists(outputDir)) return outputDir;

        string cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (Directory.Exists(cwdDir)) return cwdDir;

        string cwdProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data");
        if (Directory.Exists(cwdProjectDir)) return cwdProjectDir;

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data");
            if (Directory.Exists(candidateProject)) return candidateProject;

            string candidateVanilla = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(candidateVanilla)) return candidateVanilla;

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidateProject = Path.Combine(dir.FullName, "GTFO_GUIEditor", "Data");
            if (Directory.Exists(candidateProject)) return candidateProject;

            string candidateVanilla = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(candidateVanilla)) return candidateVanilla;

            dir = dir.Parent;
        }

        return outputDir;
    }

    public static string? FindDefaultsFile()
    {
        string dir = GetDataDirectory();
        string directPath = Path.Combine(dir, "EditorDefaults.json");
        if (File.Exists(directPath)) return directPath;

        string cwdVanilla = Path.Combine(Directory.GetCurrentDirectory(), "Data", "EditorDefaults.json");
        if (File.Exists(cwdVanilla)) return cwdVanilla;

        string cwdProjectData = Path.Combine(Directory.GetCurrentDirectory(), "GTFO_GUIEditor", "Data", "EditorDefaults.json");
        if (File.Exists(cwdProjectData)) return cwdProjectData;

        return null;
    }

    public static void Initialize(string? filePath = null)
    {
        lock (_lock)
        {
            _cachedDefaults = new EditorDefaultsRoot();

            filePath ??= FindDefaultsFile();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            try
            {
                string json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<EditorDefaultsRoot>(json, JsonOptions);
                if (loaded != null)
                {
                    _cachedDefaults = loaded;
                }
            }
            catch
            {
                // Fallback to fresh defaults
            }
        }
    }

    public static EditorDefaultsRoot GetDefaults()
    {
        lock (_lock)
        {
            if (_cachedDefaults == null)
            {
                Initialize();
            }

            return _cachedDefaults ?? new EditorDefaultsRoot();
        }
    }

    public static void ResetCache()
    {
        lock (_lock)
        {
            _cachedDefaults = null;
        }
    }

    // --- Level Defaults ---
    public static LevelLayoutBlock CreateDefaultLevel(uint persistentId = 1, string? name = null, ComplexType? complexType = null, string? elevatorTile = null)
    {
        var defaults = GetDefaults().LevelLayoutWindow.LevelSettingsDialog;
        var complex = complexType ?? defaults.ComplexType;
        var elev = elevatorTile;
        if (string.IsNullOrWhiteSpace(elev))
        {
            var elevatorOptions = GeomorphService.GetElevatorOptionsForComplex(complex);
            elev = elevatorOptions.FirstOrDefault()?.Prefab ?? "Assets/AssetPrefabs/Complex/Mining/Geomorphs/geo_32x32_elevator_shaft_dig_site_01.prefab";
        }

        return new LevelLayoutBlock
        {
            PersistentId = persistentId,
            Name = name ?? (defaults.Name == "My New Level" ? $"Level {persistentId}" : defaults.Name),
            InternalEnabled = defaults.InternalEnabled,
            ComplexType = complex,
            ElevatorTile = elev,
            FogSettings = defaults.FogSettings
        };
    }

    // --- Security Door Defaults ---
    public static void ApplySecurityDoorDefaults(PlacedDoorNode door)
    {
        var d = GetDefaults().LevelLayoutWindow.SecurityDoorSettings;
        door.IsApexDoor = d.IsApexDoor;
        door.ChainedPuzzleToSolve = d.ChainedPuzzleToSolve;
        door.ProgressionPuzzleToEnter = d.ProgressionPuzzleToEnter;
        door.ProgressionPuzzleCustomText = d.ProgressionPuzzleCustomText ?? string.Empty;
        door.ProgressionPuzzlePlacementCount = d.ProgressionPuzzlePlacementCount > 0 ? d.ProgressionPuzzlePlacementCount : 1;
        door.ProgressionPuzzleCandidateZones = new ObservableCollection<CandidateZonePlacement>(d.ProgressionPuzzleCandidateZones.Select(c => c.Clone()));
        door.TurnOffAlarmOnTerminal = d.TurnOffAlarmOnTerminal;
        door.TerminalZone = d.TerminalZone;
        door.TerminalIndex = d.TerminalIndex;
        door.AliasOverride = d.AliasOverride;
        door.AliasPrefixShortOverride = d.AliasPrefixShortOverride ?? string.Empty;
        door.HasBloodDoor = d.HasBloodDoor;
        door.EnemyGroupInFrontOfDoor = new ObservableCollection<EnemyGroupItem>(d.EnemyGroupInFrontOfDoor.Select(e => e.Clone()));
        door.EnemyGroupInArea = new ObservableCollection<EnemyGroupItem>(d.EnemyGroupInArea.Select(e => e.Clone()));

        door.EventsOnApproachDoor = new ObservableCollection<WardenEvent>(d.DoorEvents.EventsOnApproachDoor.Select(e => e.Clone()));
        door.EventsOnUnlockDoor = new ObservableCollection<WardenEvent>(d.DoorEvents.EventsOnUnlockDoor.Select(e => e.Clone()));
        door.EventsOnOpenDoor = new ObservableCollection<WardenEvent>(d.DoorEvents.EventsOnOpenDoor.Select(e => e.Clone()));
        door.EventsOnPortalWarp = new ObservableCollection<WardenEvent>(d.DoorEvents.EventsOnPortalWarp.Select(e => e.Clone()));
    }

    // --- Zone Settings Defaults ---
    public static void ApplyZoneSettingsDefaults(ZoneSettings zone)
    {
        var d = GetDefaults().LevelLayoutWindow.ZoneSettings;

        // General and Seeds
        zone.SubSeed = d.GeneralAndSeeds.SubSeed;
        zone.MarkerSubSeed = d.GeneralAndSeeds.MarkerSubSeed;
        zone.LightingSubSeed = d.GeneralAndSeeds.LightingSubSeed;
        zone.DcScanSeed = d.GeneralAndSeeds.DcScanSeed;

        // Enemy Respawning
        zone.EnemyRespawning = d.EnemyRespawning.EnemyRespawning;
        zone.RequireOtherZone = d.EnemyRespawning.RequireOtherZone;
        zone.RoomDistance = d.EnemyRespawning.RoomDistance;
        zone.TimeInterval = d.EnemyRespawning.TimeInterval;
        zone.CountMultiplier = d.EnemyRespawning.CountMultiplier;
        zone.ExcludeList = new ObservableCollection<EnemyExcludeItem>(d.EnemyRespawning.ExcludeList.Select(e => e.Clone()));

        // Resource Containers
        zone.MedKitUses = d.ResourceContainers.MedKitUses;
        zone.MedKitPlacementStart = d.ResourceContainers.MedKitPlacementStart;
        zone.MedKitPlacementMiddle = d.ResourceContainers.MedKitPlacementMiddle;
        zone.MedKitPlacementEnd = d.ResourceContainers.MedKitPlacementEnd;

        zone.AmmoKitUses = d.ResourceContainers.AmmoKitUses;
        zone.AmmoKitPlacementStart = d.ResourceContainers.AmmoKitPlacementStart;
        zone.AmmoKitPlacementMiddle = d.ResourceContainers.AmmoKitPlacementMiddle;
        zone.AmmoKitPlacementEnd = d.ResourceContainers.AmmoKitPlacementEnd;

        zone.ToolKitUses = d.ResourceContainers.ToolKitUses;
        zone.ToolKitPlacementStart = d.ResourceContainers.ToolKitPlacementStart;
        zone.ToolKitPlacementMiddle = d.ResourceContainers.ToolKitPlacementMiddle;
        zone.ToolKitPlacementEnd = d.ResourceContainers.ToolKitPlacementEnd;

        zone.DisinfectKitUses = d.ResourceContainers.DisinfectKitUses;
        zone.DisinfectKitPlacementStart = d.ResourceContainers.DisinfectKitPlacementStart;
        zone.DisinfectKitPlacementMiddle = d.ResourceContainers.DisinfectKitPlacementMiddle;
        zone.DisinfectKitPlacementEnd = d.ResourceContainers.DisinfectKitPlacementEnd;

        zone.PowerCells = d.ResourceContainers.PowerCells;
        zone.FogTurbines = d.ResourceContainers.FogTurbines;
    }

    public static ZoneSettings CreateDefaultZoneSettings(int zoneNumber = 0)
    {
        var zone = new ZoneSettings { ZoneIndex = zoneNumber };
        ApplyZoneSettingsDefaults(zone);
        return zone;
    }

    // --- Geomorph Defaults ---
    public static void ApplyGeomorphDefaults(PlacedGeomorphNode geomorph)
    {
        var d = GetDefaults().LevelLayoutWindow.GeomorphSettings;
        geomorph.Altitude = d.Altitude;
        geomorph.RotationAngle = d.RotationAngle;
    }

    // --- Terminal Defaults ---
    public static TerminalPlacement CreateDefaultTerminalPlacement()
    {
        var d = GetDefaults().LevelLayoutWindow.ZoneSettings.Terminals;
        return new TerminalPlacement
        {
            PlacementStartWeight = d.PlacementStartWeight,
            PlacementMiddleWeight = d.PlacementMiddleWeight,
            PlacementEndWeight = d.PlacementEndWeight,
            AreaSeedOffset = d.AreaSeedOffset,
            MarkerSeedOffset = d.MarkerSeedOffset,
            StartingState = d.StartingState,
            KeepShowingLocalLogCount = d.KeepShowingLocalLogCount,
            UseCustomInfoText = d.UseCustomInfoText,
            CustomInfoText = d.CustomInfoText ?? string.Empty,
            PasswordProtected = d.PasswordProtected,
            GeneratePassword = d.GeneratePassword,
            ShowPasswordLength = d.ShowPasswordLength,
            ShowPasswordPartPositions = d.ShowPasswordPartPositions,
            PasswordHintText = d.PasswordHintText ?? string.Empty,
            PasswordPartCount = d.PasswordPartCount > 0 ? d.PasswordPartCount : 1,
            TerminalZoneSelectionDatas = new ObservableCollection<TerminalZoneSelectionData>(d.TerminalZoneSelectionDatas.Select(t => t.Clone())),
            UniqueCommands = new ObservableCollection<TerminalUniqueCommand>(d.UniqueCommands.Select(u => u.Clone()))
        };
    }

    // --- Sector and Warden Objective Defaults ---
    public static void ApplySectorCommonDefaults(SectorSettings sector)
    {
        var d = GetDefaults().LevelLayoutWindow.SectorAndWardenObjectives.SectorCommon;
        sector.ZoneAliasStart = d.ZoneAliasStart;
        sector.ArtifactAmountMultiplier = d.ArtifactAmountMultiplier;
        if (sector.PrimaryObjective != null)
        {
            ApplySectorCommonDefaults(sector.PrimaryObjective);
        }
    }

    public static void ApplySectorCommonDefaults(WardenObjective objective)
    {
        var d = GetDefaults().LevelLayoutWindow.SectorAndWardenObjectives.SectorCommon;
        objective.WinCondition = d.WinCondition;
        objective.ExitZone = d.ExitZone;
        objective.ChainedPuzzleToActive = d.ChainedPuzzleToActive;
        objective.ChainedPuzzleMidObjective = d.ChainedPuzzleMidObjective;
        objective.FogTransitionDataOnGotoWin = d.FogTransitionDataOnGotoWin;
        objective.FogTransitionDataOnGotoWinDuration = d.FogTransitionDataOnGotoWinDuration;
        objective.StopAllWavesBeforeGotoWin = d.StopAllWavesBeforeGotoWin;
        objective.WaveOnGotoWinTrigger = d.WaveOnGotoWinTrigger;

        ApplyGenericWaveDefaults(objective.WavesOnGotoWin);
        ApplyGenericWaveDefaults(objective.WavesOnElevatorLand);
        ApplyGenericWaveDefaults(objective.WavesOnActivate);
    }

    public static void ApplyGenericWaveDefaults(GenericWaveSettings wave)
    {
        var dAlarms = GetDefaults().AlarmsWindow;
        var dRoles = dAlarms.WaveRoles;
        var dFilters = dAlarms.PopulationFilters;
        var dTiming = dAlarms.WaveSettingsAndTiming;

        wave.WaveRoleWeakling = dRoles.WaveRoleWeakling;
        wave.WaveRoleStandard = dRoles.WaveRoleStandard;
        wave.WaveRoleSpecial = dRoles.WaveRoleSpecial;
        wave.WaveRoleMiniBoss = dRoles.WaveRoleMiniBoss;
        wave.WaveRoleBoss = dRoles.WaveRoleBoss;
        wave.FilterWeakling = dFilters.FilterWeakling;
        wave.FilterStandard = dFilters.FilterStandard;
        wave.FilterSpecial = dFilters.FilterSpecial;
        wave.FilterMiniBoss = dFilters.FilterMiniBoss;
        wave.FilterBoss = dFilters.FilterBoss;
        wave.FilterType = dFilters.FilterType;
        wave.PauseBeforeStart = dTiming.PauseBeforeStart;
        wave.PauseBetweenGroups = dTiming.PauseBetweenGroups;
        wave.WavePauseMin_AtCost = dTiming.WavePauseMin_AtCost;
        wave.WavePauseMax_AtCost = dTiming.WavePauseMax_AtCost;
        wave.WavePauseMin = dTiming.WavePauseMin;
        wave.WavePauseMax = dTiming.WavePauseMax;
        wave.ChanceToRandomizeSpawnDirectionPerWave = dTiming.ChanceToRandomizeSpawnDirectionPerWave;
        wave.ChanceToRandomizeSpawnDirectionPerGroup = dTiming.ChanceToRandomizeSpawnDirectionPerGroup;
        wave.OverrideWaveSpawnType = dTiming.OverrideWaveSpawnType;
        wave.SurvivalWaveSpawnType = dTiming.SurvivalWaveSpawnType;
        wave.PopulationPointsTotal = dTiming.PopulationPointsTotal;
        wave.PopulationPointsPerWaveStart = dTiming.PopulationPointsPerWaveStart;
        wave.PopulationPointsPerWaveEnd = dTiming.PopulationPointsPerWaveEnd;
        wave.PopulationPointsMinPerGroup = dTiming.PopulationPointsMinPerGroup;
        wave.PopulationPointsPerGroupStart = dTiming.PopulationPointsPerGroupStart;
        wave.PopulationPointsPerGroupEnd = dTiming.PopulationPointsPerGroupEnd;
        wave.PopulationRampOverTime = dTiming.PopulationRampOverTime;
    }

    // --- Alarms Defaults ---
    public static void ApplyAlarmDefaults(AlarmBlock alarm)
    {
        var dAlarms = GetDefaults().AlarmsWindow;
        var dRoles = dAlarms.WaveRoles;
        var dFilters = dAlarms.PopulationFilters;
        var dTiming = dAlarms.WaveSettingsAndTiming;

        alarm.SpawnsEnemies = dAlarms.SpawnsEnemies;
        if (dAlarms.ChainedPuzzle != null && dAlarms.ChainedPuzzle.Count > 0)
        {
            alarm.ChainedPuzzle = new ObservableCollection<ChainedPuzzleItem>(dAlarms.ChainedPuzzle.Select(p => new ChainedPuzzleItem { PuzzleType = p }));
        }

        alarm.PublicAlarmName = dTiming.PublicAlarmName;
        alarm.WaveRoleWeakling = dRoles.WaveRoleWeakling;
        alarm.WaveRoleStandard = dRoles.WaveRoleStandard;
        alarm.WaveRoleSpecial = dRoles.WaveRoleSpecial;
        alarm.WaveRoleMiniBoss = dRoles.WaveRoleMiniBoss;
        alarm.WaveRoleBoss = dRoles.WaveRoleBoss;
        alarm.FilterWeakling = dFilters.FilterWeakling;
        alarm.FilterStandard = dFilters.FilterStandard;
        alarm.FilterSpecial = dFilters.FilterSpecial;
        alarm.FilterMiniBoss = dFilters.FilterMiniBoss;
        alarm.FilterBoss = dFilters.FilterBoss;
        alarm.FilterType = dFilters.FilterType;
        alarm.PauseBeforeStart = dTiming.PauseBeforeStart;
        alarm.PauseBetweenGroups = dTiming.PauseBetweenGroups;
        alarm.WavePauseMin_AtCost = dTiming.WavePauseMin_AtCost;
        alarm.WavePauseMax_AtCost = dTiming.WavePauseMax_AtCost;
        alarm.WavePauseMin = dTiming.WavePauseMin;
        alarm.WavePauseMax = dTiming.WavePauseMax;
        alarm.ChanceToRandomizeSpawnDirectionPerWave = dTiming.ChanceToRandomizeSpawnDirectionPerWave;
        alarm.ChanceToRandomizeSpawnDirectionPerGroup = dTiming.ChanceToRandomizeSpawnDirectionPerGroup;
        alarm.OverrideWaveSpawnType = dTiming.OverrideWaveSpawnType;
        alarm.SurvivalWaveSpawnType = dTiming.SurvivalWaveSpawnType;
        alarm.PopulationPointsTotal = dTiming.PopulationPointsTotal;
        alarm.PopulationPointsPerWaveStart = dTiming.PopulationPointsPerWaveStart;
        alarm.PopulationPointsPerWaveEnd = dTiming.PopulationPointsPerWaveEnd;
        alarm.PopulationPointsMinPerGroup = dTiming.PopulationPointsMinPerGroup;
        alarm.PopulationPointsPerGroupStart = dTiming.PopulationPointsPerGroupStart;
        alarm.PopulationPointsPerGroupEnd = dTiming.PopulationPointsPerGroupEnd;
        alarm.PopulationRampOverTime = dTiming.PopulationRampOverTime;
    }

    public static AlarmBlock CreateDefaultAlarmBlock(uint persistentId = 1, string? name = null)
    {
        var alarm = new AlarmBlock
        {
            PersistentId = persistentId,
            Name = name ?? $"Alarm_Custom_{persistentId}",
            InternalEnabled = true
        };
        ApplyAlarmDefaults(alarm);
        return alarm;
    }

    // --- Lighting Defaults ---
    public static void ApplyLightBlockDefaults(LightSettingsBlock light)
    {
        var dBlock = GetDefaults().LightingWindow.LightBlockDefaults;
        var dCategories = GetDefaults().LightingWindow.LightCategoryDefaults;

        light.Name = dBlock.Name;
        light.InternalEnabled = dBlock.InternalEnabled;

        if (dCategories != null && dCategories.Count > 0)
        {
            light.LightCategorySettings = new ObservableCollection<LightCategorySettingItem>(
                dCategories.Select(kvp =>
                {
                    var item = kvp.Value.Clone();
                    if (Enum.TryParse<LightCategory>(kvp.Key, true, out var cat))
                    {
                        item.Category = cat;
                    }
                    return item;
                }));
        }
    }

    public static LightSettingsBlock CreateDefaultLightBlock(uint persistentId = 1, string? name = null)
    {
        var block = new LightSettingsBlock
        {
            PersistentId = persistentId,
            Name = name ?? (persistentId == 1 ? GetDefaults().LightingWindow.LightBlockDefaults.Name : $"Lighting Configuration {persistentId}"),
            InternalEnabled = GetDefaults().LightingWindow.LightBlockDefaults.InternalEnabled
        };
        ApplyLightBlockDefaults(block);
        if (name != null) block.Name = name;
        return block;
    }

    // --- Fog Settings Defaults ---
    public static void ApplyFogBlockDefaults(FogSettingsBlock fog)
    {
        var d = GetDefaults().FogSettingsWindow.FogBlockDefaults;

        fog.Name = d.Name;
        fog.InternalEnabled = d.InternalEnabled;
        fog.FogDensity = d.FogDensity;
        if (d.FogColor != null)
        {
            fog.FogColor = new LightColor { R = d.FogColor.R, G = d.FogColor.G, B = d.FogColor.B, A = d.FogColor.A };
        }
        if (d.DensityNoiseDirection != null)
        {
            fog.DensityNoiseDirection = new Vector3Data(d.DensityNoiseDirection.X, d.DensityNoiseDirection.Y, d.DensityNoiseDirection.Z);
        }
        fog.Infection = d.Infection;
        fog.DensityNoiseSpeed = d.DensityNoiseSpeed;
        fog.DensityNoiseScale = d.DensityNoiseScale;
        fog.DensityHeightAltitude = d.DensityHeightAltitude;
        fog.DensityHeightRange = d.DensityHeightRange;
        fog.DensityHeightMaxBoost = d.DensityHeightMaxBoost;
    }

    public static FogSettingsBlock CreateDefaultFogBlock(uint persistentId = 1, string? name = null)
    {
        var fog = new FogSettingsBlock
        {
            PersistentId = persistentId,
            Name = name ?? (persistentId == 1 ? GetDefaults().FogSettingsWindow.FogBlockDefaults.Name : $"Fog Setting {persistentId}"),
            InternalEnabled = GetDefaults().FogSettingsWindow.FogBlockDefaults.InternalEnabled
        };
        ApplyFogBlockDefaults(fog);
        if (name != null) fog.Name = name;
        return fog;
    }

    // --- Rundown Defaults ---
    public static void ApplyRundownDefaults(RundownBlock rundown)
    {
        var dSettings = GetDefaults().RundownWindow.RundownSettings;
        var dStory = GetDefaults().RundownWindow.Storytelling;
        var dReqs = GetDefaults().RundownWindow.TierUnlockRequirements;

        rundown.Name = dSettings.Name;
        rundown.InternalEnabled = dSettings.InternalEnabled;
        rundown.NeverShowRundownTree = dSettings.NeverShowRundownTree;
        rundown.UseTierUnlockRequirements = dSettings.UseTierUnlockRequirements;
        if (dStory != null)
        {
            rundown.StorytellingData = new StorytellingData
            {
                Title = dStory.Title ?? string.Empty,
                TextLog = dStory.TextLog,
                Visuals = dStory.Visuals != null ? dStory.Visuals.Clone() : new RundownVisuals()
            };
        }

        if (dReqs != null)
        {
            if (dReqs.TryGetValue("ReqToReachTierA", out var rA)) rundown.ReqToReachTierA = rA.Clone();
            if (dReqs.TryGetValue("ReqToReachTierB", out var rB)) rundown.ReqToReachTierB = rB.Clone();
            if (dReqs.TryGetValue("ReqToReachTierC", out var rC)) rundown.ReqToReachTierC = rC.Clone();
            if (dReqs.TryGetValue("ReqToReachTierD", out var rD)) rundown.ReqToReachTierD = rD.Clone();
            if (dReqs.TryGetValue("ReqToReachTierE", out var rE)) rundown.ReqToReachTierE = rE.Clone();
        }
    }

    public static RundownBlock CreateDefaultRundownBlock(uint persistentId = 1, string? name = null)
    {
        var rundown = new RundownBlock
        {
            PersistentId = persistentId,
            Name = name ?? GetDefaults().RundownWindow.RundownSettings.Name,
            InternalEnabled = GetDefaults().RundownWindow.RundownSettings.InternalEnabled
        };
        ApplyRundownDefaults(rundown);
        if (name != null) rundown.Name = name;
        return rundown;
    }

    public static ExpeditionBlock CreateDefaultExpedition(string tierPrefix = "A", int index = 1, uint levelLayoutId = 1)
    {
        var d = GetDefaults().RundownWindow.ExpeditionDefaults;

        var exp = new ExpeditionBlock
        {
            Accessibility = d.Accessibility,
            LevelLayoutData = levelLayoutId > 0 ? levelLayoutId : d.LevelLayoutData,
            SecondaryLayout = d.SecondaryLayout,
            ThirdLayout = d.ThirdLayout,
            Descriptive = new ExpeditionDescriptiveData
            {
                Prefix = $"{tierPrefix}",
                PublicName = $"Level {tierPrefix}{index}",
                RoleplayedWardenIntel = d.Descriptive.RoleplayedWardenIntel ?? string.Empty,
                ExpeditionDescription = d.Descriptive.ExpeditionDescription ?? string.Empty,
                ExpeditionDepth = 500 + index * 100,
                EstimatedDuration = 900,
                ProgressionVisualStyle = "Normal",
                SkipExpNumberInName = false
            },
            MainLayerData = d.MainLayerData.Clone(),
            SecondaryLayerData = d.SecondaryLayerData.Clone(),
            ThirdLayerData = d.ThirdLayerData.Clone()
        };
        exp.Expedition.FogSettings = d.FogSettings;
        exp.Expedition.LightSettings = d.LightSettings;
        exp.Expedition.ComplexResourceData = d.ComplexResourceData;
        return exp;
    }
}
