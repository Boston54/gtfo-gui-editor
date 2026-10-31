using System;
using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ObjectiveHeaderAndWavesTests
{
    [Fact]
    public void GenericWaveSettings_DefaultValues_AndCloning()
    {
        var wave = new GenericWaveSettings();

        // Wave Roles
        Assert.Equal(21u, wave.WaveRoleWeakling);
        Assert.Equal(13u, wave.WaveRoleStandard);
        Assert.Equal(11u, wave.WaveRoleSpecial);
        Assert.Equal(16u, wave.WaveRoleMiniBoss);
        Assert.Equal(18u, wave.WaveRoleBoss);

        // Timing
        Assert.Equal(3.0, wave.PauseBeforeStart);
        Assert.Equal(5.0, wave.PauseBetweenGroups);
        Assert.Equal(3.0, wave.WavePauseMin_AtCost);
        Assert.Equal(10.0, wave.WavePauseMax_AtCost);
        Assert.Equal(3.0, wave.WavePauseMin);
        Assert.Equal(30.0, wave.WavePauseMax);

        // Filters
        Assert.Equal(WaveFilterType.Exclude, wave.FilterType);
        Assert.True(wave.FilterWeakling);
        Assert.False(wave.FilterStandard);
        Assert.False(wave.FilterSpecial);
        Assert.False(wave.FilterMiniBoss);
        Assert.False(wave.FilterBoss);
        Assert.Contains("Weakling", wave.PopulationFilter);

        // Spawn directions
        Assert.Equal(1.0, wave.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.1, wave.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.False(wave.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InRelationToClosestAlivePlayer, wave.SurvivalWaveSpawnType);

        // Population points
        Assert.Equal(-1.0, wave.PopulationPointsTotal);
        Assert.Equal(17.0, wave.PopulationPointsPerWaveStart);
        Assert.Equal(25.0, wave.PopulationPointsPerWaveEnd);
        Assert.Equal(5.0, wave.PopulationPointsMinPerGroup);
        Assert.Equal(5.0, wave.PopulationPointsPerGroupStart);
        Assert.Equal(10.0, wave.PopulationPointsPerGroupEnd);
        Assert.Equal(200.0, wave.PopulationRampOverTime);

        // Set values and test clone
        wave.WaveRoleWeakling = 12;
        wave.WaveRoleStandard = 34;
        wave.WaveRoleSpecial = 56;
        wave.WaveRoleMiniBoss = 78;
        wave.WaveRoleBoss = 90;

        wave.PauseBeforeStart = 5.0;
        wave.PauseBetweenGroups = 10.0;
        wave.WavePauseMin_AtCost = 15.0;
        wave.WavePauseMax_AtCost = 20.0;
        wave.WavePauseMin = 25.0;
        wave.WavePauseMax = 30.0;

        wave.FilterType = WaveFilterType.Include;
        wave.FilterWeakling = true;
        wave.FilterSpecial = true;
        Assert.Contains("Weakling", wave.PopulationFilter);
        Assert.Contains("Special", wave.PopulationFilter);
        Assert.DoesNotContain("Standard", wave.PopulationFilter);

        wave.ChanceToRandomizeSpawnDirectionPerWave = 0.5;
        wave.ChanceToRandomizeSpawnDirectionPerGroup = 0.75;
        wave.OverrideWaveSpawnType = true;
        wave.SurvivalWaveSpawnType = SurvivalWaveSpawnType.InSuppliedCourseNodeZone;

        wave.PopulationPointsTotal = 100.0;
        wave.PopulationPointsPerWaveStart = 10.0;
        wave.PopulationPointsPerWaveEnd = 20.0;
        wave.PopulationPointsMinPerGroup = 5.0;
        wave.PopulationPointsPerGroupStart = 2.0;
        wave.PopulationPointsPerGroupEnd = 4.0;
        wave.PopulationRampOverTime = 60.0;

        var cloned = wave.Clone();
        Assert.Equal(12u, cloned.WaveRoleWeakling);
        Assert.Equal(34u, cloned.WaveRoleStandard);
        Assert.Equal(56u, cloned.WaveRoleSpecial);
        Assert.Equal(78u, cloned.WaveRoleMiniBoss);
        Assert.Equal(90u, cloned.WaveRoleBoss);

        Assert.Equal(5.0, cloned.PauseBeforeStart);
        Assert.Equal(10.0, cloned.PauseBetweenGroups);
        Assert.Equal(15.0, cloned.WavePauseMin_AtCost);
        Assert.Equal(20.0, cloned.WavePauseMax_AtCost);
        Assert.Equal(25.0, cloned.WavePauseMin);
        Assert.Equal(30.0, cloned.WavePauseMax);

        Assert.Equal(WaveFilterType.Include, cloned.FilterType);
        Assert.True(cloned.FilterWeakling);
        Assert.False(cloned.FilterStandard);
        Assert.True(cloned.FilterSpecial);
        Assert.False(cloned.FilterMiniBoss);
        Assert.False(cloned.FilterBoss);

        Assert.Equal(0.5, cloned.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.75, cloned.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.True(cloned.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InSuppliedCourseNodeZone, cloned.SurvivalWaveSpawnType);

        Assert.Equal(100.0, cloned.PopulationPointsTotal);
        Assert.Equal(10.0, cloned.PopulationPointsPerWaveStart);
        Assert.Equal(20.0, cloned.PopulationPointsPerWaveEnd);
        Assert.Equal(5.0, cloned.PopulationPointsMinPerGroup);
        Assert.Equal(2.0, cloned.PopulationPointsPerGroupStart);
        Assert.Equal(4.0, cloned.PopulationPointsPerGroupEnd);
        Assert.Equal(60.0, cloned.PopulationRampOverTime);
    }

    [Fact]
    public void SectorSettings_ObjectiveHeaderAndWaves_Defaults_AndCloning()
    {
        var sector = new SectorSettings();
        var expectedDefaults = ObjectiveDefaultsService.GetDefaults(ObjectiveType.HSU_FindTakeSample);
        Assert.Equal(expectedDefaults.Header, sector.Header);
        Assert.Equal(expectedDefaults.MainObjective, sector.MainObjective);
        Assert.Equal(WaveOnGotoWinTrigger.OnObjectiveCompleted, sector.WaveOnGotoWinTrigger);
        Assert.False(sector.StopAllWavesBeforeGotoWin);
        Assert.NotNull(sector.WavesOnGotoWin);
        Assert.NotNull(sector.WavesOnElevatorLand);

        sector.Header = "Gather GLP-1 canisters";
        sector.MainObjective = "Gather [COUNT_REQUIRED] GLP-1 canisters and return to entrance.";
        sector.WaveOnGotoWinTrigger = WaveOnGotoWinTrigger.WhenExitScanMakesProgress;
        sector.StopAllWavesBeforeGotoWin = true;
        sector.WavesOnGotoWin.WaveRoleStandard = 15;
        sector.WavesOnElevatorLand.WaveRoleWeakling = 22;
        sector.WavesOnActivate.WaveRoleSpecial = 33;

        var cloned = sector.Clone();
        Assert.Equal("Gather GLP-1 canisters", cloned.Header);
        Assert.Equal("Gather [COUNT_REQUIRED] GLP-1 canisters and return to entrance.", cloned.MainObjective);
        Assert.Equal(WaveOnGotoWinTrigger.WhenExitScanMakesProgress, cloned.WaveOnGotoWinTrigger);
        Assert.True(cloned.StopAllWavesBeforeGotoWin);
        Assert.Equal(15u, cloned.WavesOnGotoWin.WaveRoleStandard);
        Assert.Equal(22u, cloned.WavesOnElevatorLand.WaveRoleWeakling);
        Assert.Equal(33u, cloned.WavesOnActivate.WaveRoleSpecial);

        // Independent clone
        cloned.Header = "New Header";
        cloned.WavesOnGotoWin.WaveRoleStandard = 99;
        cloned.WavesOnActivate.WaveRoleSpecial = 77;
        Assert.Equal("Gather GLP-1 canisters", sector.Header);
        Assert.Equal(15u, sector.WavesOnGotoWin.WaveRoleStandard);
        Assert.Equal(33u, sector.WavesOnActivate.WaveRoleSpecial);
    }

    [Fact]
    public void LevelLayoutViewModel_IsMainSector_TogglesCorrectly()
    {
        var vm = new LevelLayoutViewModel();

        vm.OpenMainSectorSettings();
        Assert.True(vm.IsMainSector);
        Assert.Equal(SectorType.Main, vm.EditingSectorType);

        vm.OpenSecondarySectorSettings();
        Assert.False(vm.IsMainSector);
        Assert.Equal(SectorType.Secondary, vm.EditingSectorType);

        vm.OpenOverloadSectorSettings();
        Assert.False(vm.IsMainSector);
        Assert.Equal(SectorType.Overload, vm.EditingSectorType);

        vm.OpenSectorSettings(SectorType.Main);
        Assert.True(vm.IsMainSector);
    }

    [Fact]
    public void LevelLayoutViewModel_AvailableWaveOnGotoWinTriggers_AreProvided()
    {
        var triggers = LevelLayoutViewModel.AvailableWaveOnGotoWinTriggers;
        Assert.Contains(WaveOnGotoWinTrigger.OnObjectiveCompleted, triggers);
        Assert.Contains(WaveOnGotoWinTrigger.WhenExitScanMakesProgress, triggers);
    }

    [Fact]
    public void RundownProjectService_RoundTrips_ObjectiveHeaderAndWaves()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rundown_obj_waves_{Guid.NewGuid()}.rundown");
        try
        {
            var vm = new MainWindowViewModel();
            var layoutVm = vm.LevelLayout;

            var level = layoutVm.SelectedLevel;
            Assert.NotNull(level);

            var mainSector = level.GetSectorSettings(SectorType.Main);
            mainSector.Header = "Test Header Main";
            mainSector.MainObjective = "Test Main Objective Desc";
            mainSector.WaveOnGotoWinTrigger = WaveOnGotoWinTrigger.WhenExitScanMakesProgress;
            mainSector.StopAllWavesBeforeGotoWin = true;
            mainSector.WavesOnGotoWin.WaveRoleWeakling = 101;
            mainSector.WavesOnGotoWin.PauseBeforeStart = 12.5;
            mainSector.WavesOnGotoWin.FilterType = WaveFilterType.Include;
            mainSector.WavesOnGotoWin.FilterBoss = true;
            mainSector.WavesOnElevatorLand.WaveRoleBoss = 202;
            mainSector.WavesOnElevatorLand.PopulationPointsTotal = 50.0;
            mainSector.WavesOnActivate.WaveRoleMiniBoss = 303;
            mainSector.WavesOnActivate.PopulationPointsTotal = 75.0;

            var secondarySector = level.GetSectorSettings(SectorType.Secondary);
            secondarySector.Header = "Secondary Header";
            secondarySector.MainObjective = "Secondary Main Objective";
            secondarySector.WaveOnGotoWinTrigger = WaveOnGotoWinTrigger.OnObjectiveCompleted;
            secondarySector.StopAllWavesBeforeGotoWin = false;
            secondarySector.WavesOnActivate.WaveRoleStandard = 404;

            RundownProjectService.SaveProject(vm, tempFile);

            var project = RundownProjectService.LoadProject(tempFile);
            var newVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(newVm, project, tempFile);

            var loadedLevel = newVm.LevelLayout.SelectedLevel;
            Assert.NotNull(loadedLevel);

            var loadedMain = loadedLevel.GetSectorSettings(SectorType.Main);
            Assert.Equal("Test Header Main", loadedMain.Header);
            Assert.Equal("Test Main Objective Desc", loadedMain.MainObjective);
            Assert.Equal(WaveOnGotoWinTrigger.WhenExitScanMakesProgress, loadedMain.WaveOnGotoWinTrigger);
            Assert.True(loadedMain.StopAllWavesBeforeGotoWin);
            Assert.Equal(101u, loadedMain.WavesOnGotoWin.WaveRoleWeakling);
            Assert.Equal(12.5, loadedMain.WavesOnGotoWin.PauseBeforeStart);
            Assert.Equal(WaveFilterType.Include, loadedMain.WavesOnGotoWin.FilterType);
            Assert.True(loadedMain.WavesOnGotoWin.FilterBoss);
            Assert.Equal(202u, loadedMain.WavesOnElevatorLand.WaveRoleBoss);
            Assert.Equal(50.0, loadedMain.WavesOnElevatorLand.PopulationPointsTotal);
            Assert.Equal(303u, loadedMain.WavesOnActivate.WaveRoleMiniBoss);
            Assert.Equal(75.0, loadedMain.WavesOnActivate.PopulationPointsTotal);

            var loadedSecondary = loadedLevel.GetSectorSettings(SectorType.Secondary);
            Assert.Equal("Secondary Header", loadedSecondary.Header);
            Assert.Equal("Secondary Main Objective", loadedSecondary.MainObjective);
            Assert.Equal(WaveOnGotoWinTrigger.OnObjectiveCompleted, loadedSecondary.WaveOnGotoWinTrigger);
            Assert.False(loadedSecondary.StopAllWavesBeforeGotoWin);
            Assert.Equal(404u, loadedSecondary.WavesOnActivate.WaveRoleStandard);
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
    public void DatablockExportService_ExportsGenericWaveLists_InWardenObjective()
    {
        var tempExportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_waves_{Guid.NewGuid()}");
        try
        {
            var vm = new MainWindowViewModel();
            var layoutVm = vm.LevelLayout;
            var level = layoutVm.SelectedLevel;
            Assert.NotNull(level);

            var mainSector = level.GetSectorSettings(SectorType.Main);
            mainSector.WavesOnGotoWin.Enabled = true;
            mainSector.WavesOnGotoWin.WaveRoleStandard = 10;
            mainSector.WavesOnGotoWin.PopulationPointsTotal = 100.0;
            mainSector.WavesOnGotoWin.PauseBeforeStart = 4.0;

            mainSector.WavesOnActivate.Enabled = true;
            mainSector.WavesOnActivate.WaveRoleWeakling = 20;
            mainSector.WavesOnActivate.PopulationPointsTotal = 50.0;

            mainSector.WavesOnElevatorLand.Enabled = true;
            mainSector.WavesOnElevatorLand.WaveRoleMiniBoss = 30;
            mainSector.WavesOnElevatorLand.PopulationPointsTotal = 25.0;

            var result = DatablockExportService.ExportAll(vm, tempExportDir);
            Assert.True(result.Success, result.Message);

            string wardenPath = Path.Combine(tempExportDir, "GameData_WardenObjectiveDataBlock_bin.json");
            Assert.True(File.Exists(wardenPath));
            string json = File.ReadAllText(wardenPath);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var blocks = doc.RootElement.GetProperty("Blocks");
            var firstBlock = blocks.EnumerateArray().First();

            var wavesOnGotoWin = firstBlock.GetProperty("WavesOnGotoWin");
            Assert.True(wavesOnGotoWin.GetArrayLength() > 0);
            var gotoWinWave = wavesOnGotoWin.EnumerateArray().First();
            Assert.True(gotoWinWave.GetProperty("WaveSettings").GetUInt32() > 0);
            Assert.True(gotoWinWave.GetProperty("WavePopulation").GetUInt32() > 0);
            Assert.Equal(4.0, gotoWinWave.GetProperty("SpawnDelay").GetDouble());

            var wavesOnActivate = firstBlock.GetProperty("WavesOnActivate");
            Assert.True(wavesOnActivate.GetArrayLength() > 0);
            var activateWave = wavesOnActivate.EnumerateArray().First();
            Assert.True(activateWave.GetProperty("WaveSettings").GetUInt32() > 0);
            Assert.True(activateWave.GetProperty("WavePopulation").GetUInt32() > 0);

            var wavesOnElevatorLand = firstBlock.GetProperty("WavesOnElevatorLand");
            Assert.True(wavesOnElevatorLand.GetArrayLength() > 0);
            var elevatorWave = wavesOnElevatorLand.EnumerateArray().First();
            Assert.True(elevatorWave.GetProperty("WaveSettings").GetUInt32() > 0);
            Assert.True(elevatorWave.GetProperty("WavePopulation").GetUInt32() > 0);
        }
        finally
        {
            if (Directory.Exists(tempExportDir))
            {
                Directory.Delete(tempExportDir, true);
            }
        }
    }

    [Fact]
    public void WardenEvent_SpawnEnemyWave_AndObjectives_ShareAlarmDefaults()
    {
        var alarmDefaults = EditorDefaultsService.GetDefaults().AlarmsWindow;
        var ev = new WardenEvent { Type = EventType.SpawnEnemyWave };
        var obj = new WardenObjective();

        Assert.Equal(alarmDefaults.WaveRoles.WaveRoleWeakling, ev.EnemyWaveData.WaveRoleWeakling);
        Assert.Equal(alarmDefaults.WaveRoles.WaveRoleStandard, ev.EnemyWaveData.WaveRoleStandard);
        Assert.Equal(alarmDefaults.WaveRoles.WaveRoleSpecial, ev.EnemyWaveData.WaveRoleSpecial);
        Assert.Equal(alarmDefaults.WaveRoles.WaveRoleMiniBoss, ev.EnemyWaveData.WaveRoleMiniBoss);
        Assert.Equal(alarmDefaults.WaveRoles.WaveRoleBoss, ev.EnemyWaveData.WaveRoleBoss);

        Assert.Equal(alarmDefaults.PopulationFilters.FilterWeakling, ev.EnemyWaveData.FilterWeakling);
        Assert.Equal(alarmDefaults.PopulationFilters.FilterStandard, ev.EnemyWaveData.FilterStandard);
        Assert.Equal(alarmDefaults.PopulationFilters.FilterSpecial, ev.EnemyWaveData.FilterSpecial);
        Assert.Equal(alarmDefaults.PopulationFilters.FilterMiniBoss, ev.EnemyWaveData.FilterMiniBoss);
        Assert.Equal(alarmDefaults.PopulationFilters.FilterBoss, ev.EnemyWaveData.FilterBoss);
        Assert.Equal(alarmDefaults.PopulationFilters.FilterType, ev.EnemyWaveData.FilterType);

        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PauseBeforeStart, ev.EnemyWaveData.PauseBeforeStart);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PauseBetweenGroups, ev.EnemyWaveData.PauseBetweenGroups);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.WavePauseMin, ev.EnemyWaveData.WavePauseMin);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.WavePauseMax, ev.EnemyWaveData.WavePauseMax);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.WavePauseMin_AtCost, ev.EnemyWaveData.WavePauseMin_AtCost);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.WavePauseMax_AtCost, ev.EnemyWaveData.WavePauseMax_AtCost);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PopulationPointsTotal, ev.EnemyWaveData.PopulationPointsTotal);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PopulationPointsPerWaveStart, ev.EnemyWaveData.PopulationPointsPerWaveStart);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PopulationPointsPerWaveEnd, ev.EnemyWaveData.PopulationPointsPerWaveEnd);
        Assert.Equal(alarmDefaults.WaveSettingsAndTiming.PopulationRampOverTime, ev.EnemyWaveData.PopulationRampOverTime);

        // Same for objective waves
        Assert.Equal(ev.EnemyWaveData.WaveRoleWeakling, obj.WavesOnElevatorLand.WaveRoleWeakling);
        Assert.Equal(ev.EnemyWaveData.WaveRoleWeakling, obj.WavesOnActivate.WaveRoleWeakling);
        Assert.Equal(ev.EnemyWaveData.WaveRoleWeakling, obj.WavesOnGotoWin.WaveRoleWeakling);
    }

    [Fact]
    public void DatablockExportService_ExportsWorldEventObjectFilter_OnWardenEvents()
    {
        var tempExportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_events_{Guid.NewGuid()}");
        try
        {
            var vm = new MainWindowViewModel();
            var layoutVm = vm.LevelLayout;
            var level = layoutVm.SelectedLevel;
            Assert.NotNull(level);

            var mainSector = level.GetSectorSettings(SectorType.Main);
            mainSector.EventsOnActivate.Add(new WardenEvent
            {
                Type = EventType.SetNavMarker,
                WorldEventObjectFilter = "Evt_TestNavMarkerFilter"
            });

            var result = DatablockExportService.ExportAll(vm, tempExportDir);
            Assert.True(result.Success, result.Message);

            string wardenPath = Path.Combine(tempExportDir, "GameData_WardenObjectiveDataBlock_bin.json");
            Assert.True(File.Exists(wardenPath));
            string json = File.ReadAllText(wardenPath);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var blocks = doc.RootElement.GetProperty("Blocks");
            var firstBlock = blocks.EnumerateArray().First();

            var eventsOnActivate = firstBlock.GetProperty("EventsOnActivate");
            Assert.True(eventsOnActivate.GetArrayLength() > 0);
            var matchedEvent = eventsOnActivate.EnumerateArray()
                .FirstOrDefault(e => e.GetProperty("Type").GetString() == "SetNavMarker");
            Assert.Equal("Evt_TestNavMarkerFilter", matchedEvent.GetProperty("WorldEventObjectFilter").GetString());
        }
        finally
        {
            if (Directory.Exists(tempExportDir))
            {
                Directory.Delete(tempExportDir, true);
            }
        }
    }
}
