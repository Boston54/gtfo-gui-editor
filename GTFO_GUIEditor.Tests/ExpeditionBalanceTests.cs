using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ExpeditionBalanceTests
{
    [Fact]
    public void ExpeditionBalanceSettings_Defaults_MatchVanillaPersistentId1()
    {
        var settings = new ExpeditionBalanceSettings();

        // Resource Containers
        Assert.Equal(0.5, settings.ChanceToReUseResourceContainer);
        Assert.Equal(4, settings.MaxPacksPerResourceContainer);
        Assert.Equal(0.0, settings.EmptyWeakResourceContainersPerZone);
        Assert.Equal(3.0, settings.EmptySecureResourceContainersPerZone);
        Assert.Equal(0.75, settings.WeakResourceContainerWithPackChanceForLocked);

        // Weak Doors
        Assert.Equal(7.0, settings.WeakDoor4x4Health);
        Assert.Equal(12.0, settings.WeakDoor8x4Health);
        Assert.Equal(3.0, settings.WeakDoorChanceLockWeightNoLock);
        Assert.Equal(1.0, settings.WeakDoorChanceLockWeightMelee);
        Assert.Equal(1.0, settings.WeakDoorChanceLockWeightMeleeLock);
        Assert.Equal(1.0, settings.WeakDoorChanceLockWeightHackableLock);
        Assert.Equal(15.0, settings.WeakDoorLockHealth);
        Assert.Equal(0.5, settings.GlueVolumeToDoorHealthConversion);
        Assert.Equal(27.0, settings.GlueVolumeForDoorGlueMaxState);

        // Artifacts
        Assert.Equal(1, settings.ArtifactsPerSegment);
        Assert.Equal(30, settings.ArtifactsPerLayer);
        Assert.Equal(0.15, settings.ChanceToPutArtifactInResourceContainer);

        // Remaining defaults
        Assert.Equal(1.0, settings.HealthPerZone);
        Assert.Equal(1.0, settings.DisinfectionPerZone);
        Assert.Equal(1.0, settings.WeaponAmmoPerZone);
        Assert.Equal(1.0, settings.ToolAmmoPerZone);
        Assert.Equal(0.0, settings.CommodityValuePerZone);
        Assert.Equal(0.7, settings.ChanceToPutCommodityInResourceContainer);
        Assert.Equal(0.3, settings.ChanceToSpawnCommodityLargePack);
        Assert.Equal(0.5, settings.ChanceToSpawnCommodityMediumPack);
        Assert.Equal(3.0, settings.LootPerZone);
        Assert.Equal(1.0, settings.AirPerZone);
        Assert.Equal(5.0, settings.AirPerZoneInNoAir);
        Assert.Equal(1.0, settings.TerminalsPerZone);

        Assert.NotNull(settings.TentacleTraps);
        Assert.Equal(10.0, settings.TentacleTraps.Health);
        Assert.Equal(1.0, settings.TentacleTraps.AttackDamage);
        Assert.Equal(3, settings.TentacleTraps.MaxPerZone);
        Assert.Equal(1, settings.TentacleTraps.MaxSmallArea);
        Assert.Equal(1, settings.TentacleTraps.MaxMediumArea);
        Assert.Equal(1, settings.TentacleTraps.MaxLargeArea);
        Assert.Equal(1, settings.TentacleTraps.MaxHugeArea);

        Assert.NotNull(settings.ParasiteNests);
        Assert.Equal(20.0, settings.ParasiteNests.Health);
        Assert.Equal(2.0, settings.ParasiteNests.AttackDamage);
        Assert.Equal(7, settings.ParasiteNests.MaxPerZone);
        Assert.Equal(2, settings.ParasiteNests.MaxSmallArea);
        Assert.Equal(3, settings.ParasiteNests.MaxMediumArea);
        Assert.Equal(4, settings.ParasiteNests.MaxLargeArea);
        Assert.Equal(4, settings.ParasiteNests.MaxHugeArea);

        Assert.Equal(5, settings.EnemyPatrolGroupsPerZone);
        Assert.Equal(10, settings.StaticEnemiesMaxPerZone);
        Assert.Equal(1, settings.StaticEnemiesMaxSmallArea);
        Assert.Equal(2, settings.StaticEnemiesMaxMediumArea);
        Assert.Equal(3, settings.StaticEnemiesMaxLargeArea);
        Assert.Equal(5, settings.StaticEnemiesMaxHugeArea);
        Assert.Equal(25.0, settings.EnemyPopulationPerZone);
        Assert.Equal(0.037, settings.VoxelCoverageAreaMultiplier);
        Assert.Equal(0.25, settings.VoxelCoverageAreaScoringRandomMultiplier);
        Assert.Equal(0.0, settings.WeakDoorUnlockedChanceForOpen);
        Assert.Equal(0.0, settings.WeakDoorOpenChanceForWallRemoverUsed);
        Assert.Equal(new List<double> { 0.6, 1.0, 0.4 }, settings.ResourcePackSizes);
    }

    [Fact]
    public void ExpeditionBalanceSettings_Clone_CreatesDeepCopy()
    {
        var original = new ExpeditionBalanceSettings
        {
            ChanceToReUseResourceContainer = 0.8,
            MaxPacksPerResourceContainer = 6,
            WeakDoor4x4Health = 14.0,
            ArtifactsPerSegment = 3
        };
        original.ResourcePackSizes.Add(0.2);
        original.TentacleTraps.Health = 25.0;

        var clone = original.Clone();

        Assert.Equal(0.8, clone.ChanceToReUseResourceContainer);
        Assert.Equal(6, clone.MaxPacksPerResourceContainer);
        Assert.Equal(14.0, clone.WeakDoor4x4Health);
        Assert.Equal(3, clone.ArtifactsPerSegment);
        Assert.Equal(4, clone.ResourcePackSizes.Count);
        Assert.Equal(25.0, clone.TentacleTraps.Health);

        // Verify modifying clone doesn't affect original
        clone.ChanceToReUseResourceContainer = 0.1;
        clone.ResourcePackSizes.Clear();
        clone.TentacleTraps.Health = 5.0;

        Assert.Equal(0.8, original.ChanceToReUseResourceContainer);
        Assert.Equal(4, original.ResourcePackSizes.Count);
        Assert.Equal(25.0, original.TentacleTraps.Health);
    }

    [Fact]
    public void LevelLayoutViewModel_CreateAndEditLevel_UpdatesExpeditionBalance()
    {
        var vm = new LevelLayoutViewModel(null, new List<LevelLayoutBlock>());

        // 1. Create level with custom ExpeditionBalance values
        vm.OpenCreateLevelDialog();
        vm.NewLevelName = "Expedition Balance Test Level";
        vm.NewLevelPersistentId = 444;
        vm.EditingExpeditionBalance.ChanceToReUseResourceContainer = 0.95;
        vm.EditingExpeditionBalance.MaxPacksPerResourceContainer = 8;
        vm.EditingExpeditionBalance.WeakDoor4x4Health = 18.0;
        vm.EditingExpeditionBalance.ArtifactsPerLayer = 50;

        bool created = vm.ConfirmCreateLevel();
        Assert.True(created);

        var level = vm.Levels.FirstOrDefault(l => l.PersistentId == 444);
        Assert.NotNull(level);
        Assert.Equal(0.95, level.ExpeditionBalance.ChanceToReUseResourceContainer);
        Assert.Equal(8, level.ExpeditionBalance.MaxPacksPerResourceContainer);
        Assert.Equal(18.0, level.ExpeditionBalance.WeakDoor4x4Health);
        Assert.Equal(50, level.ExpeditionBalance.ArtifactsPerLayer);

        // 2. Edit existing level
        vm.SelectedLevel = level;
        vm.OpenEditLevelDialog();

        Assert.Equal(0.95, vm.EditingExpeditionBalance.ChanceToReUseResourceContainer);
        Assert.Equal(8, vm.EditingExpeditionBalance.MaxPacksPerResourceContainer);

        vm.EditingExpeditionBalance.ChanceToReUseResourceContainer = 0.33;
        vm.EditingExpeditionBalance.WeakDoorChanceLockWeightNoLock = 9.0;
        vm.EditingExpeditionBalance.ChanceToPutArtifactInResourceContainer = 0.45;

        bool edited = vm.ConfirmCreateLevel();
        Assert.True(edited);

        Assert.Equal(0.33, level.ExpeditionBalance.ChanceToReUseResourceContainer);
        Assert.Equal(9.0, level.ExpeditionBalance.WeakDoorChanceLockWeightNoLock);
        Assert.Equal(0.45, level.ExpeditionBalance.ChanceToPutArtifactInResourceContainer);
    }

    [Fact]
    public void RundownProjectService_SaveAndLoad_PreservesExpeditionBalance()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"GTFO_ExpeditionBalance_Test_{Guid.NewGuid():N}.json");
        try
        {
            var mainVm = new MainWindowViewModel(autoReopenLastRundown: false);
            mainVm.LevelLayout.Levels.Clear();

            var level = new LevelLayoutBlock
            {
                Name = "Expedition_Bal_Level",
                PersistentId = 555,
                ExpeditionBalance = new ExpeditionBalanceSettings
                {
                    ChanceToReUseResourceContainer = 0.88,
                    MaxPacksPerResourceContainer = 7,
                    EmptyWeakResourceContainersPerZone = 1.5,
                    EmptySecureResourceContainersPerZone = 4.5,
                    WeakResourceContainerWithPackChanceForLocked = 0.9,
                    WeakDoor4x4Health = 11.0,
                    WeakDoor8x4Health = 22.0,
                    WeakDoorChanceLockWeightNoLock = 5.0,
                    WeakDoorChanceLockWeightMeleeLock = 2.0,
                    WeakDoorChanceLockWeightHackableLock = 3.0,
                    WeakDoorLockHealth = 30.0,
                    GlueVolumeToDoorHealthConversion = 0.8,
                    GlueVolumeForDoorGlueMaxState = 50.0,
                    ArtifactsPerSegment = 4,
                    ArtifactsPerLayer = 42,
                    ChanceToPutArtifactInResourceContainer = 0.35
                }
            };
            mainVm.LevelLayout.Levels.Add(level);

            RundownProjectService.SaveProject(mainVm, tempFile);

            var loadVm = new MainWindowViewModel(autoReopenLastRundown: false);
            RundownProjectService.LoadProject(loadVm, tempFile);

            Assert.Single(loadVm.LevelLayout.Levels);
            var loadedLevel = loadVm.LevelLayout.Levels[0];
            Assert.Equal("Expedition_Bal_Level", loadedLevel.Name);
            Assert.Equal(555u, loadedLevel.PersistentId);

            var b = loadedLevel.ExpeditionBalance;
            Assert.NotNull(b);
            Assert.Equal(0.88, b.ChanceToReUseResourceContainer);
            Assert.Equal(7, b.MaxPacksPerResourceContainer);
            Assert.Equal(1.5, b.EmptyWeakResourceContainersPerZone);
            Assert.Equal(4.5, b.EmptySecureResourceContainersPerZone);
            Assert.Equal(0.9, b.WeakResourceContainerWithPackChanceForLocked);
            Assert.Equal(11.0, b.WeakDoor4x4Health);
            Assert.Equal(22.0, b.WeakDoor8x4Health);
            Assert.Equal(5.0, b.WeakDoorChanceLockWeightNoLock);
            Assert.Equal(2.0, b.WeakDoorChanceLockWeightMelee);
            Assert.Equal(2.0, b.WeakDoorChanceLockWeightMeleeLock);
            Assert.Equal(3.0, b.WeakDoorChanceLockWeightHackableLock);
            Assert.Equal(30.0, b.WeakDoorLockHealth);
            Assert.Equal(0.8, b.GlueVolumeToDoorHealthConversion);
            Assert.Equal(50.0, b.GlueVolumeForDoorGlueMaxState);
            Assert.Equal(4, b.ArtifactsPerSegment);
            Assert.Equal(42, b.ArtifactsPerLayer);
            Assert.Equal(0.35, b.ChanceToPutArtifactInResourceContainer);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void DatablockExportService_SynthesizesExpeditionBalancePerLevel_AndLinksToExpedition()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"GTFO_ExpeditionBalanceExport_Test_{Guid.NewGuid():N}");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            vm.LevelLayout.Levels.Clear();

            var level1 = new LevelLayoutBlock
            {
                Name = "LevelAlpha",
                PersistentId = 101,
                ExpeditionBalance = new ExpeditionBalanceSettings
                {
                    ChanceToReUseResourceContainer = 0.65,
                    WeakDoor4x4Health = 9.5,
                    ArtifactsPerLayer = 45
                }
            };

            var level2 = new LevelLayoutBlock
            {
                Name = "LevelBeta",
                PersistentId = 102,
                ExpeditionBalance = new ExpeditionBalanceSettings
                {
                    ChanceToReUseResourceContainer = 0.12,
                    WeakDoor4x4Health = 3.5,
                    ArtifactsPerLayer = 15
                }
            };

            vm.LevelLayout.Levels.Add(level1);
            vm.LevelLayout.Levels.Add(level2);

            // Configure rundown with expeditions linking to these levels
            vm.Rundown.SelectedRundown = new RundownBlock
            {
                PersistentId = 1,
                Name = "Test Rundown",
                TierA = new ObservableCollection<ExpeditionBlock>
                {
                    new ExpeditionBlock
                    {
                        LevelLayoutData = 101,
                        Descriptive = new DescriptiveData { Prefix = "A", Name = "Level 1" }
                    },
                    new ExpeditionBlock
                    {
                        LevelLayoutData = 102,
                        Descriptive = new DescriptiveData { Prefix = "A", Name = "Level 2" }
                    }
                }
            };

            var exportResult = DatablockExportService.ExportAll(vm, tempDir);
            Assert.True(exportResult.Success, exportResult.Message);

            // 1. Verify ExpeditionBalanceDataBlock json
            string expBalPath = Path.Combine(tempDir, "GameData_ExpeditionBalanceDataBlock_bin.json");
            Assert.True(File.Exists(expBalPath));

            string expBalJson = File.ReadAllText(expBalPath);
            using var expBalDoc = JsonDocument.Parse(expBalJson);
            var blocks = expBalDoc.RootElement.GetProperty("Blocks").EnumerateArray().ToList();

            Assert.Equal(2, blocks.Count);

            var block1 = blocks.FirstOrDefault(b => b.GetProperty("WeakDoor4x4Health").GetDouble() == 9.5);
            Assert.True(block1.ValueKind != JsonValueKind.Undefined, "Block for LevelAlpha not found");
            Assert.Equal(0.65, block1.GetProperty("ChanceToReUseResourceContainer").GetDouble());
            Assert.Equal(45, block1.GetProperty("ArtifactsPerLayer").GetInt32());
            uint block1Id = block1.GetProperty("persistentID").GetUInt32();
            Assert.True(block1Id >= 100000);

            // Check persistentID 1 defaults in block1
            Assert.Equal(1.0, block1.GetProperty("HealthPerZone").GetDouble());
            Assert.Equal(10.0, block1.GetProperty("TentacleTraps").GetProperty("Health").GetDouble());
            Assert.Equal(20.0, block1.GetProperty("ParasiteNests").GetProperty("Health").GetDouble());

            var block2 = blocks.FirstOrDefault(b => b.GetProperty("WeakDoor4x4Health").GetDouble() == 3.5);
            Assert.True(block2.ValueKind != JsonValueKind.Undefined, "Block for LevelBeta not found");
            Assert.Equal(0.12, block2.GetProperty("ChanceToReUseResourceContainer").GetDouble());
            Assert.Equal(15, block2.GetProperty("ArtifactsPerLayer").GetInt32());
            uint block2Id = block2.GetProperty("persistentID").GetUInt32();
            Assert.True(block2Id >= 100000);
            Assert.NotEqual(block1Id, block2Id);

            // 2. Verify RundownDataBlock references the synthesized ExpeditionBalance persistent IDs
            string rundownPath = Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json");
            Assert.True(File.Exists(rundownPath));

            string rundownJson = File.ReadAllText(rundownPath);
            using var rundownDoc = JsonDocument.Parse(rundownJson);
            var rundownBlocks = rundownDoc.RootElement.GetProperty("Blocks").EnumerateArray().ToList();
            Assert.Single(rundownBlocks);

            var tierAExpeditions = rundownBlocks[0].GetProperty("TierA").EnumerateArray().ToList();
            Assert.Equal(2, tierAExpeditions.Count);

            uint exp1BalId = tierAExpeditions[0].GetProperty("Expedition").GetProperty("ExpeditionBalance").GetUInt32();
            uint exp2BalId = tierAExpeditions[1].GetProperty("Expedition").GetProperty("ExpeditionBalance").GetUInt32();

            Assert.Equal(block1Id, exp1BalId);
            Assert.Equal(block2Id, exp2BalId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
