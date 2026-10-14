using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ReactorStartupTests
{
    [Fact]
    public void ReactorStartupStage_DefaultValues_AndCloning()
    {
        var stage = new ReactorStartupStage();

        Assert.Equal(20.0, stage.Warmup);
        Assert.Equal(20.0, stage.WarmupFail);
        Assert.Equal(60.0, stage.Wave);
        Assert.Equal(45.0, stage.Verify);
        Assert.Equal(45.0, stage.VerifyFail);
        Assert.False(stage.VerifyInOtherZone);
        Assert.Equal(0, stage.ZoneForVerification);
        Assert.Equal(1, stage.StageNumber);

        // Customize stage
        stage.Warmup = 30.0;
        stage.WarmupFail = 15.0;
        stage.Wave = 90.0;
        stage.Verify = 40.0;
        stage.VerifyFail = 25.0;
        stage.VerifyInOtherZone = true;
        stage.ZoneForVerification = 2;
        stage.WaveRoleWeakling = 101;
        stage.WaveRoleStandard = 102;
        stage.WaveRoleSpecial = 103;
        stage.WaveRoleMiniBoss = 104;
        stage.WaveRoleBoss = 105;
        stage.PauseBeforeStart = 5.0;
        stage.PauseBetweenGroups = 2.0;
        stage.WavePauseMin = 3.0;
        stage.WavePauseMax = 8.0;
        stage.WavePauseMin_AtCost = 10.0;
        stage.WavePauseMax_AtCost = 25.0;
        stage.FilterType = WaveFilterType.Include;
        stage.FilterStandard = true;
        stage.FilterSpecial = true;
        stage.ChanceToRandomizeSpawnDirectionPerWave = 0.5;
        stage.ChanceToRandomizeSpawnDirectionPerGroup = 0.25;
        stage.OverrideWaveSpawnType = true;
        stage.SurvivalWaveSpawnType = SurvivalWaveSpawnType.InSuppliedCourseNodeZone;
        stage.PopulationPointsTotal = 100.0;
        stage.PopulationPointsPerWaveStart = 10.0;
        stage.PopulationPointsPerWaveEnd = 20.0;
        stage.PopulationPointsMinPerGroup = 4.0;
        stage.PopulationPointsPerGroupStart = 2.0;
        stage.PopulationPointsPerGroupEnd = 6.0;
        stage.PopulationRampOverTime = 60.0;

        var cloned = stage.Clone();

        Assert.Equal(30.0, cloned.Warmup);
        Assert.Equal(15.0, cloned.WarmupFail);
        Assert.Equal(90.0, cloned.Wave);
        Assert.Equal(40.0, cloned.Verify);
        Assert.Equal(25.0, cloned.VerifyFail);
        Assert.True(cloned.VerifyInOtherZone);
        Assert.Equal(2, cloned.ZoneForVerification);
        Assert.Equal((uint)101, cloned.WaveRoleWeakling);
        Assert.Equal((uint)102, cloned.WaveRoleStandard);
        Assert.Equal((uint)103, cloned.WaveRoleSpecial);
        Assert.Equal((uint)104, cloned.WaveRoleMiniBoss);
        Assert.Equal((uint)105, cloned.WaveRoleBoss);
        Assert.Equal(5.0, cloned.PauseBeforeStart);
        Assert.Equal(2.0, cloned.PauseBetweenGroups);
        Assert.Equal(3.0, cloned.WavePauseMin);
        Assert.Equal(8.0, cloned.WavePauseMax);
        Assert.Equal(10.0, cloned.WavePauseMin_AtCost);
        Assert.Equal(25.0, cloned.WavePauseMax_AtCost);
        Assert.Equal(WaveFilterType.Include, cloned.FilterType);
        Assert.True(cloned.FilterStandard);
        Assert.True(cloned.FilterSpecial);
        Assert.False(cloned.FilterWeakling);
        Assert.Equal(0.5, cloned.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.25, cloned.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.True(cloned.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InSuppliedCourseNodeZone, cloned.SurvivalWaveSpawnType);
        Assert.Equal(100.0, cloned.PopulationPointsTotal);
        Assert.Equal(10.0, cloned.PopulationPointsPerWaveStart);
        Assert.Equal(20.0, cloned.PopulationPointsPerWaveEnd);
        Assert.Equal(4.0, cloned.PopulationPointsMinPerGroup);
        Assert.Equal(2.0, cloned.PopulationPointsPerGroupStart);
        Assert.Equal(6.0, cloned.PopulationPointsPerGroupEnd);
        Assert.Equal(60.0, cloned.PopulationRampOverTime);

        // Verify independent instance
        cloned.Warmup = 99.0;
        Assert.Equal(30.0, stage.Warmup);
    }

    [Fact]
    public void ReactorStartupStage_PopulationFilter_SyncsWithBooleans()
    {
        var stage = new ReactorStartupStage();

        stage.FilterWeakling = true;
        stage.FilterBoss = true;

        Assert.Contains("Weakling", stage.PopulationFilter);
        Assert.Contains("Boss", stage.PopulationFilter);
        Assert.DoesNotContain("Standard", stage.PopulationFilter);

        stage.PopulationFilter = new List<string> { "Standard", "Special", "MiniBoss" };

        Assert.False(stage.FilterWeakling);
        Assert.True(stage.FilterStandard);
        Assert.True(stage.FilterSpecial);
        Assert.True(stage.FilterMiniBoss);
        Assert.False(stage.FilterBoss);
    }

    [Fact]
    public void ReactorStartupStage_DisplayTitle_UpdatesWithProperties()
    {
        var stage = new ReactorStartupStage { StageNumber = 2, Wave = 75.0, Verify = 30.0 };

        Assert.Equal("Stage 2 (Wave: 75s, Verify: 30s)", stage.DisplayTitle);

        stage.StageNumber = 3;
        Assert.Equal("Stage 3 (Wave: 75s, Verify: 30s)", stage.DisplayTitle);

        stage.Wave = 120.0;
        Assert.Equal("Stage 3 (Wave: 120s, Verify: 30s)", stage.DisplayTitle);

        stage.Verify = 50.0;
        Assert.Equal("Stage 3 (Wave: 120s, Verify: 50s)", stage.DisplayTitle);
    }

    [Fact]
    public void SectorSettings_ReactorStartup_ClonesStagesCorrectly()
    {
        var settings = new SectorSettings
        {
            ObjectiveType = ObjectiveType.Reactor_Startup,
            ReactorPlacementZone = 1
        };

        var stage1 = new ReactorStartupStage { Wave = 50.0 };
        var stage2 = new ReactorStartupStage { Wave = 100.0, VerifyInOtherZone = true, ZoneForVerification = 3 };

        settings.ReactorStartupStages.Add(stage1);
        settings.ReactorStartupStages.Add(stage2);

        var clone = settings.Clone();

        Assert.Equal(ObjectiveType.Reactor_Startup, clone.ObjectiveType);
        Assert.Equal(1, clone.ReactorPlacementZone);
        Assert.Equal(2, clone.ReactorStartupStages.Count);
        Assert.Equal(50.0, clone.ReactorStartupStages[0].Wave);
        Assert.Equal(100.0, clone.ReactorStartupStages[1].Wave);
        Assert.True(clone.ReactorStartupStages[1].VerifyInOtherZone);
        Assert.Equal(3, clone.ReactorStartupStages[1].ZoneForVerification);

        // Check independence
        clone.ReactorStartupStages[0].Wave = 999.0;
        Assert.Equal(50.0, settings.ReactorStartupStages[0].Wave);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorStartup_SwitchObjective_AddsDefaultStage()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();

        Assert.True(vm.IsHsuObjective);
        Assert.False(vm.IsReactorObjective);

        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;

        Assert.False(vm.IsHsuObjective);
        Assert.True(vm.IsReactorObjective);
        Assert.NotNull(vm.EditingSectorSettings);
        Assert.Single(vm.EditingSectorSettings.ReactorStartupStages);
        Assert.NotNull(vm.SelectedReactorStage);
        Assert.Equal(1, vm.SelectedReactorStage.StageNumber);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorStartup_StageManagement_AddRemoveMove()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;

        Assert.Single(vm.EditingSectorSettings!.ReactorStartupStages);

        // Add 2 more stages
        vm.AddReactorStage();
        vm.SelectedReactorStage!.Wave = 90.0;

        vm.AddReactorStage();
        vm.SelectedReactorStage!.Wave = 120.0;

        Assert.Equal(3, vm.EditingSectorSettings.ReactorStartupStages.Count);
        Assert.Equal(1, vm.EditingSectorSettings.ReactorStartupStages[0].StageNumber);
        Assert.Equal(2, vm.EditingSectorSettings.ReactorStartupStages[1].StageNumber);
        Assert.Equal(3, vm.EditingSectorSettings.ReactorStartupStages[2].StageNumber);
        Assert.Equal(120.0, vm.SelectedReactorStage.Wave);

        // Move stage 3 up to position 2
        var stage3 = vm.SelectedReactorStage;
        vm.MoveReactorStageUp(stage3);

        Assert.Equal(120.0, vm.EditingSectorSettings.ReactorStartupStages[1].Wave);
        Assert.Equal(90.0, vm.EditingSectorSettings.ReactorStartupStages[2].Wave);
        Assert.Equal(1, vm.EditingSectorSettings.ReactorStartupStages[0].StageNumber);
        Assert.Equal(2, vm.EditingSectorSettings.ReactorStartupStages[1].StageNumber);
        Assert.Equal(3, vm.EditingSectorSettings.ReactorStartupStages[2].StageNumber);

        // Move stage down back to position 3
        vm.MoveReactorStageDown(stage3);
        Assert.Equal(90.0, vm.EditingSectorSettings.ReactorStartupStages[1].Wave);
        Assert.Equal(120.0, vm.EditingSectorSettings.ReactorStartupStages[2].Wave);

        // Remove stage 2
        var stage2 = vm.EditingSectorSettings.ReactorStartupStages[1];
        vm.RemoveReactorStage(stage2);

        Assert.Equal(2, vm.EditingSectorSettings.ReactorStartupStages.Count);
        Assert.Equal(1, vm.EditingSectorSettings.ReactorStartupStages[0].StageNumber);
        Assert.Equal(2, vm.EditingSectorSettings.ReactorStartupStages[1].StageNumber);
        Assert.Equal(120.0, vm.EditingSectorSettings.ReactorStartupStages[1].Wave);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorStartup_CopyPasteStage()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;

        var initialStage = vm.SelectedReactorStage!;
        initialStage.Wave = 88.0;
        initialStage.Verify = 44.0;
        initialStage.VerifyInOtherZone = true;
        initialStage.ZoneForVerification = 2;
        initialStage.WaveRoleMiniBoss = 55;

        Assert.False(vm.HasCopiedReactorStage);

        vm.CopyReactorStage(initialStage);

        Assert.True(vm.HasCopiedReactorStage);

        vm.PasteReactorStage();

        Assert.Equal(2, vm.EditingSectorSettings!.ReactorStartupStages.Count);
        var pastedStage = vm.EditingSectorSettings.ReactorStartupStages[1];

        Assert.Equal(88.0, pastedStage.Wave);
        Assert.Equal(44.0, pastedStage.Verify);
        Assert.True(pastedStage.VerifyInOtherZone);
        Assert.Equal(2, pastedStage.ZoneForVerification);
        Assert.Equal((uint)55, pastedStage.WaveRoleMiniBoss);
        Assert.Equal(2, pastedStage.StageNumber);

        // Check independence
        pastedStage.Wave = 999.0;
        Assert.Equal(88.0, initialStage.Wave);
    }

    [Fact]
    public void LevelLayoutViewModel_ReactorStartup_ZoneSelection()
    {
        var vm = new LevelLayoutViewModel();
        vm.OpenMainSectorSettings();
        vm.SelectedSectorObjectiveType = ObjectiveType.Reactor_Startup;

        Assert.NotEmpty(vm.AvailableObjectiveZones);

        // Select Reactor Placement Zone
        var targetZone = vm.AvailableObjectiveZones.FirstOrDefault();
        if (targetZone != null)
        {
            vm.SelectedReactorPlacementZoneOption = targetZone;
            Assert.Equal(targetZone.ZoneIndex, vm.EditingSectorSettings!.ReactorPlacementZone);
        }

        // Verification Zone on Selected Stage
        if (vm.SelectedReactorStage != null && targetZone != null)
        {
            vm.SelectedReactorStage.VerifyInOtherZone = true;
            vm.SelectedStageVerificationZoneOption = targetZone;
            Assert.Equal(targetZone.ZoneIndex, vm.SelectedReactorStage.ZoneForVerification);
        }
    }

    [Fact]
    public void RundownProjectService_ReactorStartup_SaveAndLoad_PreservesAllData()
    {
        var mainVm = new MainWindowViewModel();
        var level = mainVm.LevelLayout.Levels[0];

        level.MainSector.ObjectiveType = ObjectiveType.Reactor_Startup;
        level.MainSector.ReactorPlacementZone = 1;
        level.MainSector.ReactorStartupStages.Clear();

        var stage1 = new ReactorStartupStage
        {
            Warmup = 25.0,
            WarmupFail = 15.0,
            Wave = 75.0,
            Verify = 35.0,
            VerifyFail = 20.0,
            VerifyInOtherZone = false,
            ZoneForVerification = 0,
            WaveRoleWeakling = 21,
            WaveRoleStandard = 13,
            WaveRoleSpecial = 11,
            WaveRoleMiniBoss = 16,
            WaveRoleBoss = 18,
            PauseBeforeStart = 3.0,
            PauseBetweenGroups = 1.5,
            WavePauseMin = 2.0,
            WavePauseMax = 5.0,
            WavePauseMin_AtCost = 10.0,
            WavePauseMax_AtCost = 20.0,
            FilterType = WaveFilterType.Exclude,
            FilterWeakling = true,
            FilterBoss = true,
            ChanceToRandomizeSpawnDirectionPerWave = 0.4,
            ChanceToRandomizeSpawnDirectionPerGroup = 0.2,
            OverrideWaveSpawnType = true,
            SurvivalWaveSpawnType = SurvivalWaveSpawnType.InSuppliedCourseNode,
            PopulationPointsTotal = 150.0,
            PopulationPointsPerWaveStart = 15.0,
            PopulationPointsPerWaveEnd = 30.0,
            PopulationPointsMinPerGroup = 3.0,
            PopulationPointsPerGroupStart = 2.0,
            PopulationPointsPerGroupEnd = 5.0,
            PopulationRampOverTime = 45.0
        };

        var stage2 = new ReactorStartupStage
        {
            Warmup = 40.0,
            WarmupFail = 30.0,
            Wave = 120.0,
            Verify = 60.0,
            VerifyFail = 40.0,
            VerifyInOtherZone = true,
            ZoneForVerification = 2,
            WaveRoleStandard = 13,
            WaveRoleMiniBoss = 16,
            FilterType = WaveFilterType.Include,
            FilterStandard = true,
            FilterMiniBoss = true
        };

        level.MainSector.ReactorStartupStages.Add(stage1);
        level.MainSector.ReactorStartupStages.Add(stage2);

        var project = RundownProjectService.ExportProject(mainVm);

        var loadedVm = new MainWindowViewModel();
        RundownProjectService.ApplyProject(loadedVm, project);

        var loadedLevel = loadedVm.LevelLayout.Levels[0];
        Assert.Equal(ObjectiveType.Reactor_Startup, loadedLevel.MainSector.ObjectiveType);
        Assert.Equal(1, loadedLevel.MainSector.ReactorPlacementZone);
        Assert.Equal(2, loadedLevel.MainSector.ReactorStartupStages.Count);

        var loadedStage1 = loadedLevel.MainSector.ReactorStartupStages[0];
        Assert.Equal(25.0, loadedStage1.Warmup);
        Assert.Equal(15.0, loadedStage1.WarmupFail);
        Assert.Equal(75.0, loadedStage1.Wave);
        Assert.Equal(35.0, loadedStage1.Verify);
        Assert.Equal(20.0, loadedStage1.VerifyFail);
        Assert.False(loadedStage1.VerifyInOtherZone);
        Assert.Equal(0, loadedStage1.ZoneForVerification);
        Assert.Equal((uint)21, loadedStage1.WaveRoleWeakling);
        Assert.Equal((uint)13, loadedStage1.WaveRoleStandard);
        Assert.Equal((uint)11, loadedStage1.WaveRoleSpecial);
        Assert.Equal((uint)16, loadedStage1.WaveRoleMiniBoss);
        Assert.Equal((uint)18, loadedStage1.WaveRoleBoss);
        Assert.Equal(3.0, loadedStage1.PauseBeforeStart);
        Assert.Equal(1.5, loadedStage1.PauseBetweenGroups);
        Assert.Equal(2.0, loadedStage1.WavePauseMin);
        Assert.Equal(5.0, loadedStage1.WavePauseMax);
        Assert.Equal(10.0, loadedStage1.WavePauseMin_AtCost);
        Assert.Equal(20.0, loadedStage1.WavePauseMax_AtCost);
        Assert.Equal(WaveFilterType.Exclude, loadedStage1.FilterType);
        Assert.True(loadedStage1.FilterWeakling);
        Assert.True(loadedStage1.FilterBoss);
        Assert.False(loadedStage1.FilterStandard);
        Assert.Equal(0.4, loadedStage1.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.2, loadedStage1.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.True(loadedStage1.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InSuppliedCourseNode, loadedStage1.SurvivalWaveSpawnType);
        Assert.Equal(150.0, loadedStage1.PopulationPointsTotal);
        Assert.Equal(15.0, loadedStage1.PopulationPointsPerWaveStart);
        Assert.Equal(30.0, loadedStage1.PopulationPointsPerWaveEnd);
        Assert.Equal(3.0, loadedStage1.PopulationPointsMinPerGroup);
        Assert.Equal(2.0, loadedStage1.PopulationPointsPerGroupStart);
        Assert.Equal(5.0, loadedStage1.PopulationPointsPerGroupEnd);
        Assert.Equal(45.0, loadedStage1.PopulationRampOverTime);

        var loadedStage2 = loadedLevel.MainSector.ReactorStartupStages[1];
        Assert.Equal(40.0, loadedStage2.Warmup);
        Assert.Equal(30.0, loadedStage2.WarmupFail);
        Assert.Equal(120.0, loadedStage2.Wave);
        Assert.Equal(60.0, loadedStage2.Verify);
        Assert.Equal(40.0, loadedStage2.VerifyFail);
        Assert.True(loadedStage2.VerifyInOtherZone);
        Assert.Equal(2, loadedStage2.ZoneForVerification);
        Assert.Equal(WaveFilterType.Include, loadedStage2.FilterType);
        Assert.True(loadedStage2.FilterStandard);
        Assert.True(loadedStage2.FilterMiniBoss);
    }
}
