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
        Assert.Equal(0u, wave.WaveRoleWeakling);
        Assert.Equal(0u, wave.WaveRoleStandard);
        Assert.Equal(0u, wave.WaveRoleSpecial);
        Assert.Equal(0u, wave.WaveRoleMiniBoss);
        Assert.Equal(0u, wave.WaveRoleBoss);

        // Timing
        Assert.Equal(0.0, wave.PauseBeforeStart);
        Assert.Equal(0.0, wave.PauseBetweenGroups);
        Assert.Equal(0.0, wave.WavePauseMin_AtCost);
        Assert.Equal(0.0, wave.WavePauseMax_AtCost);
        Assert.Equal(0.0, wave.WavePauseMin);
        Assert.Equal(0.0, wave.WavePauseMax);

        // Filters
        Assert.Equal(WaveFilterType.Exclude, wave.FilterType);
        Assert.False(wave.FilterWeakling);
        Assert.False(wave.FilterStandard);
        Assert.False(wave.FilterSpecial);
        Assert.False(wave.FilterMiniBoss);
        Assert.False(wave.FilterBoss);
        Assert.Empty(wave.PopulationFilter);

        // Spawn directions
        Assert.Equal(0.0, wave.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.0, wave.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.False(wave.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InRelationToClosestAlivePlayer, wave.SurvivalWaveSpawnType);

        // Population points
        Assert.Equal(0.0, wave.PopulationPointsTotal);
        Assert.Equal(0.0, wave.PopulationPointsPerWaveStart);
        Assert.Equal(0.0, wave.PopulationPointsPerWaveEnd);
        Assert.Equal(0.0, wave.PopulationPointsMinPerGroup);
        Assert.Equal(0.0, wave.PopulationPointsPerGroupStart);
        Assert.Equal(0.0, wave.PopulationPointsPerGroupEnd);
        Assert.Equal(0.0, wave.PopulationRampOverTime);

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

        var cloned = sector.Clone();
        Assert.Equal("Gather GLP-1 canisters", cloned.Header);
        Assert.Equal("Gather [COUNT_REQUIRED] GLP-1 canisters and return to entrance.", cloned.MainObjective);
        Assert.Equal(WaveOnGotoWinTrigger.WhenExitScanMakesProgress, cloned.WaveOnGotoWinTrigger);
        Assert.True(cloned.StopAllWavesBeforeGotoWin);
        Assert.Equal(15u, cloned.WavesOnGotoWin.WaveRoleStandard);
        Assert.Equal(22u, cloned.WavesOnElevatorLand.WaveRoleWeakling);

        // Independent clone
        cloned.Header = "New Header";
        cloned.WavesOnGotoWin.WaveRoleStandard = 99;
        Assert.Equal("Gather GLP-1 canisters", sector.Header);
        Assert.Equal(15u, sector.WavesOnGotoWin.WaveRoleStandard);
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

            var secondarySector = level.GetSectorSettings(SectorType.Secondary);
            secondarySector.Header = "Secondary Header";
            secondarySector.MainObjective = "Secondary Main Objective";
            secondarySector.WaveOnGotoWinTrigger = WaveOnGotoWinTrigger.OnObjectiveCompleted;
            secondarySector.StopAllWavesBeforeGotoWin = false;

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

            var loadedSecondary = loadedLevel.GetSectorSettings(SectorType.Secondary);
            Assert.Equal("Secondary Header", loadedSecondary.Header);
            Assert.Equal("Secondary Main Objective", loadedSecondary.MainObjective);
            Assert.Equal(WaveOnGotoWinTrigger.OnObjectiveCompleted, loadedSecondary.WaveOnGotoWinTrigger);
            Assert.False(loadedSecondary.StopAllWavesBeforeGotoWin);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
