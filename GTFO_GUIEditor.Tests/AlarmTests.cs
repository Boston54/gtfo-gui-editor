using System.Collections.Generic;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class AlarmTests
{
    [Fact]
    public void LoadEnemies_LoadsEnemiesFromDataBlock()
    {
        var enemies = AlarmService.LoadEnemies();
        Assert.NotNull(enemies);
        Assert.NotEmpty(enemies);

        var striker = enemies.FirstOrDefault(e => e.PersistentId == 13);
        Assert.NotNull(striker);
        Assert.Equal("Striker_Wave", striker.Name);
        Assert.Equal("Standard", striker.EnemyType);
        Assert.Contains("[13] Striker_Wave", striker.DisplayText);
    }

    [Fact]
    public void LoadVanillaAlarms_ParsesSettingsAndPopulations()
    {
        var alarms = AlarmService.LoadVanillaAlarms();
        Assert.NotNull(alarms);
        Assert.NotEmpty(alarms);

        var apex = alarms.FirstOrDefault(a => a.PersistentId == 1);
        Assert.NotNull(apex);
        Assert.Equal("Apex", apex.Name);
        Assert.Equal(3.0, apex.PauseBeforeStart);
        Assert.Equal(5.0, apex.PauseBetweenGroups);
        Assert.Equal(3.0, apex.WavePauseMin_AtCost);
        Assert.Equal(10.0, apex.WavePauseMax_AtCost);
        Assert.Equal(3.0, apex.WavePauseMin);
        Assert.Equal(30.0, apex.WavePauseMax);
        Assert.Equal(WaveFilterType.Exclude, apex.FilterType);
        Assert.True(apex.FilterWeakling);
        Assert.False(apex.FilterStandard);
        Assert.Equal(1.0, apex.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.1, apex.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.False(apex.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InRelationToClosestAlivePlayer, apex.SurvivalWaveSpawnType);
        Assert.Equal(-1.0, apex.PopulationPointsTotal);
        Assert.Equal(17.0, apex.PopulationPointsPerWaveStart);
        Assert.Equal(25.0, apex.PopulationPointsPerWaveEnd);
        Assert.Equal(5.0, apex.PopulationPointsMinPerGroup);
        Assert.Equal(5.0, apex.PopulationPointsPerGroupStart);
        Assert.Equal(10.0, apex.PopulationPointsPerGroupEnd);
        Assert.Equal(200.0, apex.PopulationRampOverTime);

        // Check wave roles from baseline population
        Assert.Equal((uint)21, apex.WaveRoleWeakling);
        Assert.Equal((uint)13, apex.WaveRoleStandard);
        Assert.Equal((uint)11, apex.WaveRoleSpecial);
        Assert.Equal((uint)16, apex.WaveRoleMiniBoss);
        Assert.Equal((uint)18, apex.WaveRoleBoss);
    }

    [Fact]
    public void AlarmBlock_PopulationFilter_SynchronizesWithCheckboxes()
    {
        var alarm = new AlarmBlock();

        alarm.FilterWeakling = true;
        alarm.FilterBoss = true;

        var filters = alarm.PopulationFilter;
        Assert.Equal(2, filters.Count);
        Assert.Contains("Weakling", filters);
        Assert.Contains("Boss", filters);

        alarm.PopulationFilter = new List<string> { "Standard", "Special", "MiniBoss" };
        Assert.False(alarm.FilterWeakling);
        Assert.True(alarm.FilterStandard);
        Assert.True(alarm.FilterSpecial);
        Assert.True(alarm.FilterMiniBoss);
        Assert.False(alarm.FilterBoss);
    }

    [Fact]
    public void CreateFreshAlarm_InitializesCorrectDefaults()
    {
        var alarm = AlarmService.CreateFreshAlarm(500, "Custom_Test_Alarm");
        Assert.Equal((uint)500, alarm.PersistentId);
        Assert.Equal("Custom_Test_Alarm", alarm.Name);
        Assert.Equal((uint)0, alarm.WaveRoleWeakling);
        Assert.Equal((uint)0, alarm.WaveRoleStandard);
        Assert.Equal((uint)0, alarm.WaveRoleSpecial);
        Assert.Equal((uint)0, alarm.WaveRoleMiniBoss);
        Assert.Equal((uint)0, alarm.WaveRoleBoss);
        Assert.Equal(0.0, alarm.PauseBeforeStart);
        Assert.Equal(0.0, alarm.PauseBetweenGroups);
        Assert.Equal(0.0, alarm.WavePauseMin_AtCost);
        Assert.Equal(0.0, alarm.WavePauseMax_AtCost);
        Assert.Equal(0.0, alarm.WavePauseMin);
        Assert.Equal(0.0, alarm.WavePauseMax);
        Assert.Equal(0.0, alarm.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.0, alarm.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.Equal(0.0, alarm.PopulationPointsTotal);
        Assert.Equal(0.0, alarm.PopulationPointsPerWaveStart);
        Assert.Equal(0.0, alarm.PopulationPointsPerWaveEnd);
        Assert.Equal(0.0, alarm.PopulationPointsMinPerGroup);
        Assert.Equal(0.0, alarm.PopulationPointsPerGroupStart);
        Assert.Equal(0.0, alarm.PopulationPointsPerGroupEnd);
        Assert.Equal(0.0, alarm.PopulationRampOverTime);
        Assert.False(alarm.FilterWeakling);
        Assert.False(alarm.FilterStandard);
        Assert.False(alarm.FilterSpecial);
        Assert.False(alarm.FilterMiniBoss);
        Assert.False(alarm.FilterBoss);
        Assert.Empty(alarm.PopulationFilter);
        Assert.False(alarm.OverrideWaveSpawnType);
        Assert.Equal(WaveFilterType.Exclude, alarm.FilterType);
        Assert.Equal(SurvivalWaveSpawnType.InRelationToClosestAlivePlayer, alarm.SurvivalWaveSpawnType);
    }

    [Fact]
    public void CreateAlarmFromTemplate_ClonesPropertiesDeeply()
    {
        var template = new AlarmBlock
        {
            PersistentId = 1,
            Name = "TemplateAlarm",
            WaveRoleWeakling = 99,
            PauseBeforeStart = 15.0,
            FilterType = WaveFilterType.Include,
            FilterMiniBoss = true,
            SurvivalWaveSpawnType = SurvivalWaveSpawnType.OnSpawnPoints
        };

        var cloned = AlarmService.CreateAlarmFromTemplate(template, 999, "ClonedAlarm");

        Assert.Equal((uint)999, cloned.PersistentId);
        Assert.Equal("ClonedAlarm", cloned.Name);
        Assert.Equal((uint)99, cloned.WaveRoleWeakling);
        Assert.Equal(15.0, cloned.PauseBeforeStart);
        Assert.Equal(WaveFilterType.Include, cloned.FilterType);
        Assert.True(cloned.FilterMiniBoss);
        Assert.Equal(SurvivalWaveSpawnType.OnSpawnPoints, cloned.SurvivalWaveSpawnType);

        // Modifying clone does not mutate template
        cloned.WaveRoleWeakling = 123;
        Assert.Equal((uint)99, template.WaveRoleWeakling);
    }

    [Fact]
    public void AlarmsViewModel_InitializesWithAlarmsAndEnemies()
    {
        var vm = new AlarmsViewModel();
        Assert.NotEmpty(vm.Alarms);
        Assert.NotEmpty(vm.Enemies);
        Assert.NotNull(vm.SelectedAlarm);
        Assert.True(vm.HasSelectedAlarm);
        Assert.Contains("alarms loaded", vm.StatusText);
    }

    [Fact]
    public void AlarmsViewModel_DuplicatePersistentIdWarning_TriggersWhenDuplicateExists()
    {
        var alarm1 = AlarmService.CreateFreshAlarm(100, "Alarm 1");
        var alarm2 = AlarmService.CreateFreshAlarm(200, "Alarm 2");
        var vm = new AlarmsViewModel(new[] { alarm1, alarm2 });

        Assert.False(vm.HasDuplicatePersistentId);
        Assert.Empty(vm.DuplicatePersistentIdWarning);

        alarm2.PersistentId = 100;
        vm.SelectedAlarm = alarm2;

        Assert.True(vm.HasDuplicatePersistentId);
        Assert.Contains("Warning: Persistent ID 100 is used by 2 alarms", vm.DuplicatePersistentIdWarning);
    }

    [Fact]
    public void AlarmsViewModel_CreateAlarmDialog_ValidatesAndAddsAlarm()
    {
        var alarm1 = AlarmService.CreateFreshAlarm(10, "Existing Alarm");
        var vm = new AlarmsViewModel(new[] { alarm1 });

        vm.OpenCreateAlarmDialog();
        Assert.True(vm.IsCreatingAlarm);
        Assert.True(vm.IsCreatingFresh);

        // Validation: Empty Name
        vm.NewAlarmName = "   ";
        bool created = vm.ConfirmCreateAlarm();
        Assert.False(created);
        Assert.True(vm.HasCreationErrorMessage);
        Assert.Contains("cannot be empty", vm.CreationErrorMessage);

        // Validation: Duplicate ID
        vm.NewAlarmName = "New Valid Alarm";
        vm.NewAlarmPersistentId = 10;
        created = vm.ConfirmCreateAlarm();
        Assert.False(created);
        Assert.Contains("already exists", vm.CreationErrorMessage);

        // Success creation
        vm.NewAlarmPersistentId = 555;
        created = vm.ConfirmCreateAlarm();
        Assert.True(created);
        Assert.False(vm.IsCreatingAlarm);
        Assert.Equal(2, vm.Alarms.Count);
        Assert.NotNull(vm.SelectedAlarm);
        Assert.Equal((uint)555, vm.SelectedAlarm.PersistentId);
        Assert.Equal("New Valid Alarm", vm.SelectedAlarm.Name);
    }

    [Fact]
    public void AlarmsViewModel_DeleteAlarmDialog_RemovesAlarm()
    {
        var alarm1 = AlarmService.CreateFreshAlarm(1, "Alarm 1");
        var alarm2 = AlarmService.CreateFreshAlarm(2, "Alarm 2");
        var vm = new AlarmsViewModel(new[] { alarm1, alarm2 });

        vm.SelectedAlarm = alarm1;
        vm.RequestDeleteSelectedAlarm();

        Assert.True(vm.IsConfirmingDelete);
        Assert.Equal(alarm1, vm.AlarmToDelete);
        Assert.Contains("Alarm 1", vm.DeleteConfirmationMessage);

        bool deleted = vm.ConfirmDeleteAlarm();

        Assert.True(deleted);
        Assert.False(vm.IsConfirmingDelete);
        Assert.Single(vm.Alarms);
        Assert.Equal(alarm2, vm.SelectedAlarm);
    }

    [Fact]
    public void LoadVanillaAlarms_ResolvesWaveRolesByThemeAndId()
    {
        var alarms = AlarmService.LoadVanillaAlarms();
        Assert.NotNull(alarms);

        // Apex [1] uses Baseline population
        var apex = alarms.FirstOrDefault(a => a.PersistentId == 1);
        Assert.NotNull(apex);
        Assert.Equal((uint)21, apex.WaveRoleWeakling);
        Assert.Equal((uint)13, apex.WaveRoleStandard);

        // Apex_Bullrush [228] resolves to Bullrush population
        var apexBullrush = alarms.FirstOrDefault(a => a.PersistentId == 228);
        Assert.NotNull(apexBullrush);
        Assert.Equal("Apex_Bullrush", apexBullrush.Name);
        Assert.Equal((uint)30, apexBullrush.WaveRoleWeakling);
        Assert.Equal((uint)30, apexBullrush.WaveRoleStandard);
        Assert.Equal((uint)39, apexBullrush.WaveRoleMiniBoss);

        // Trickle 4-45 SSpB [15] resolves properly to BullrushBigs
        var trickle = alarms.FirstOrDefault(a => a.PersistentId == 15);
        Assert.NotNull(trickle);
        Assert.Equal((uint)39, trickle.WaveRoleWeakling);
    }

    [Fact]
    public void MainWindowViewModel_ContainsAlarmsViewModel()
    {
        var mainVm = new MainWindowViewModel();
        Assert.NotNull(mainVm.Alarms);
        Assert.NotNull(mainVm.Sounds);
        Assert.NotNull(mainVm.ComplexResourceSets);
    }
}
