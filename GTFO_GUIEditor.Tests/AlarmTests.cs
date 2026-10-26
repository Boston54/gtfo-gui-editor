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

        // ChainedPuzzle 513 is "R1A1 ZONE_50 (Main)" with SurvivalWaveSettings=1 (Apex) and SurvivalWavePopulation=1 (Baseline)
        var alarm513 = alarms.FirstOrDefault(a => a.PersistentId == 513);
        Assert.NotNull(alarm513);
        Assert.Equal("R1A1 ZONE_50 (Main)", alarm513.Name);
        Assert.Equal("Class II Alarm", alarm513.PublicAlarmName);
        Assert.True(alarm513.SpawnsEnemies);
        Assert.Equal(0.0, alarm513.WantedDistanceFromStartPos);
        Assert.Equal(10.0, alarm513.WantedDistanceBetweenPuzzleComponents);
        Assert.Equal(2, alarm513.ChainedPuzzle.Count);
        Assert.Equal((uint)6, alarm513.ChainedPuzzle[0].PuzzleType);
        Assert.Equal((uint)4, alarm513.ChainedPuzzle[1].PuzzleType);

        // Survival Wave Settings from Apex (1)
        Assert.Equal(3.0, alarm513.PauseBeforeStart);
        Assert.Equal(5.0, alarm513.PauseBetweenGroups);
        Assert.Equal(3.0, alarm513.WavePauseMin_AtCost);
        Assert.Equal(10.0, alarm513.WavePauseMax_AtCost);
        Assert.Equal(3.0, alarm513.WavePauseMin);
        Assert.Equal(30.0, alarm513.WavePauseMax);
        Assert.Equal(WaveFilterType.Exclude, alarm513.FilterType);
        Assert.True(alarm513.FilterWeakling);
        Assert.False(alarm513.FilterStandard);
        Assert.Equal(1.0, alarm513.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.1, alarm513.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.False(alarm513.OverrideWaveSpawnType);
        Assert.Equal(SurvivalWaveSpawnType.InRelationToClosestAlivePlayer, alarm513.SurvivalWaveSpawnType);
        Assert.Equal(-1.0, alarm513.PopulationPointsTotal);
        Assert.Equal(17.0, alarm513.PopulationPointsPerWaveStart);
        Assert.Equal(25.0, alarm513.PopulationPointsPerWaveEnd);
        Assert.Equal(5.0, alarm513.PopulationPointsMinPerGroup);
        Assert.Equal(5.0, alarm513.PopulationPointsPerGroupStart);
        Assert.Equal(10.0, alarm513.PopulationPointsPerGroupEnd);
        Assert.Equal(200.0, alarm513.PopulationRampOverTime);

        // Wave roles from Baseline population (1)
        Assert.Equal((uint)21, alarm513.WaveRoleWeakling);
        Assert.Equal((uint)13, alarm513.WaveRoleStandard);
        Assert.Equal((uint)11, alarm513.WaveRoleSpecial);
        Assert.Equal((uint)16, alarm513.WaveRoleMiniBoss);
        Assert.Equal((uint)18, alarm513.WaveRoleBoss);
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
        Assert.Equal("Class II Alarm", alarm.PublicAlarmName);
        Assert.Equal((uint)21, alarm.WaveRoleWeakling);
        Assert.Equal((uint)13, alarm.WaveRoleStandard);
        Assert.Equal((uint)11, alarm.WaveRoleSpecial);
        Assert.Equal((uint)16, alarm.WaveRoleMiniBoss);
        Assert.Equal((uint)18, alarm.WaveRoleBoss);
        Assert.Equal(3.0, alarm.PauseBeforeStart);
        Assert.Equal(5.0, alarm.PauseBetweenGroups);
        Assert.Equal(3.0, alarm.WavePauseMin_AtCost);
        Assert.Equal(10.0, alarm.WavePauseMax_AtCost);
        Assert.Equal(3.0, alarm.WavePauseMin);
        Assert.Equal(30.0, alarm.WavePauseMax);
        Assert.Equal(1.0, alarm.ChanceToRandomizeSpawnDirectionPerWave);
        Assert.Equal(0.1, alarm.ChanceToRandomizeSpawnDirectionPerGroup);
        Assert.Equal(-1.0, alarm.PopulationPointsTotal);
        Assert.Equal(17.0, alarm.PopulationPointsPerWaveStart);
        Assert.Equal(25.0, alarm.PopulationPointsPerWaveEnd);
        Assert.Equal(5.0, alarm.PopulationPointsMinPerGroup);
        Assert.Equal(5.0, alarm.PopulationPointsPerGroupStart);
        Assert.Equal(10.0, alarm.PopulationPointsPerGroupEnd);
        Assert.Equal(200.0, alarm.PopulationRampOverTime);
        Assert.True(alarm.FilterWeakling);
        Assert.False(alarm.FilterStandard);
        Assert.False(alarm.FilterSpecial);
        Assert.False(alarm.FilterMiniBoss);
        Assert.False(alarm.FilterBoss);
        Assert.Single(alarm.PopulationFilter);
        Assert.Contains("Weakling", alarm.PopulationFilter);
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
        Assert.Empty(vm.Alarms);
        Assert.NotEmpty(vm.Enemies);
        Assert.Null(vm.SelectedAlarm);
        Assert.False(vm.HasSelectedAlarm);
        Assert.Contains("0 alarms loaded", vm.StatusText);
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
        vm.NewAlarmPublicAlarmName = "Class IV Alarm";
        created = vm.ConfirmCreateAlarm();
        Assert.True(created);
        Assert.False(vm.IsCreatingAlarm);
        Assert.Equal(2, vm.Alarms.Count);
        Assert.NotNull(vm.SelectedAlarm);
        Assert.Equal((uint)555, vm.SelectedAlarm.PersistentId);
        Assert.Equal("New Valid Alarm", vm.SelectedAlarm.Name);
        Assert.Equal("Class IV Alarm", vm.SelectedAlarm.PublicAlarmName);
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
    public void LoadVanillaAlarms_ResolvesChainedPuzzleAndCorruptedPopulation()
    {
        var alarms = AlarmService.LoadVanillaAlarms();
        Assert.NotNull(alarms);

        // ChainedPuzzle 545: R1C2 ZONE_3 (Main), SurvivalWaveSettings=228, SurvivalWavePopulation=47
        var alarm545 = alarms.FirstOrDefault(a => a.PersistentId == 545);
        Assert.NotNull(alarm545);
        Assert.Equal("R1C2 ZONE_3 (Main)", alarm545.Name);
        Assert.Equal("Class III Alarm", alarm545.PublicAlarmName);
        Assert.True(alarm545.SpawnsEnemies);
        Assert.Equal(0.0, alarm545.WantedDistanceFromStartPos);
        Assert.Equal(11.0, alarm545.WantedDistanceBetweenPuzzleComponents);
        Assert.Equal(3, alarm545.ChainedPuzzle.Count);

        // Population 47 wave roles
        Assert.Equal((uint)30, alarm545.WaveRoleWeakling);
        Assert.Equal((uint)30, alarm545.WaveRoleStandard);
        Assert.Equal((uint)30, alarm545.WaveRoleSpecial);
        Assert.Equal((uint)30, alarm545.WaveRoleMiniBoss);
        Assert.Equal((uint)30, alarm545.WaveRoleBoss);

        // SurvivalWaveSettings 228
        Assert.Equal(5.0, alarm545.PauseBetweenGroups);
        Assert.Equal(15.0, alarm545.PopulationPointsPerWaveStart);
        Assert.Equal(25.0, alarm545.PopulationPointsPerWaveEnd);

        // ChainedPuzzle 514: R1A1 ZONE_51 (Main), TriggerAlarmOnActivate=false, SurvivalWaveSettings=0, SurvivalWavePopulation=0
        var scanOnly = alarms.FirstOrDefault(a => a.PersistentId == 514);
        Assert.NotNull(scanOnly);
        Assert.Equal("R1A1 ZONE_51 (Main)", scanOnly.Name);
        Assert.Equal("Scan", scanOnly.PublicAlarmName);
        Assert.False(scanOnly.SpawnsEnemies);
        Assert.Single(scanOnly.ChainedPuzzle);
        Assert.Equal((uint)6, scanOnly.ChainedPuzzle[0].PuzzleType);
        Assert.Equal(0.0, scanOnly.WantedDistanceFromStartPos);
        Assert.Equal(1.0, scanOnly.WantedDistanceBetweenPuzzleComponents);
    }

    [Fact]
    public void AlarmsViewModel_CreateAlarmDialog_ImportFromVanillaTemplate()
    {
        var vm = new AlarmsViewModel();
        Assert.NotEmpty(vm.AvailableTemplates);

        var template = vm.AvailableTemplates.FirstOrDefault(a => a.PersistentId == 513);
        Assert.NotNull(template);

        vm.OpenCreateAlarmDialog();
        Assert.True(vm.IsCreatingAlarm);

        vm.IsCreatingFromVanilla = true;
        vm.SelectedVanillaTemplate = template;

        Assert.Equal("R1A1 ZONE_50 (Main)", vm.NewAlarmName);
        Assert.Equal("Class II Alarm", vm.NewAlarmPublicAlarmName);

        vm.NewAlarmPersistentId = 7777;
        vm.NewAlarmName = "My Custom Portal Alarm";

        bool created = vm.ConfirmCreateAlarm();
        Assert.True(created);
        Assert.False(vm.IsCreatingAlarm);

        var createdAlarm = vm.SelectedAlarm;
        Assert.NotNull(createdAlarm);
        Assert.Equal((uint)7777, createdAlarm.PersistentId);
        Assert.Equal("My Custom Portal Alarm", createdAlarm.Name);
        Assert.Equal("Class II Alarm", createdAlarm.PublicAlarmName);
        Assert.True(createdAlarm.SpawnsEnemies);
        Assert.Equal(0.0, createdAlarm.WantedDistanceFromStartPos);
        Assert.Equal(10.0, createdAlarm.WantedDistanceBetweenPuzzleComponents);
        Assert.Equal(2, createdAlarm.ChainedPuzzle.Count);
        Assert.Equal((uint)6, createdAlarm.ChainedPuzzle[0].PuzzleType);
        Assert.Equal((uint)4, createdAlarm.ChainedPuzzle[1].PuzzleType);
        Assert.Equal((uint)21, createdAlarm.WaveRoleWeakling);
        Assert.Equal((uint)13, createdAlarm.WaveRoleStandard);
        Assert.Equal(5.0, createdAlarm.PauseBetweenGroups);
    }

    [Fact]
    public void AlarmsViewModel_CreateAlarmDialog_DuplicateFromExistingAlarm()
    {
        var existing = AlarmService.CreateFreshAlarm(101, "My_Existing_Alarm");
        existing.ChainedPuzzle.Clear();
        existing.PublicAlarmName = "Class VI Alarm";
        existing.PauseBeforeStart = 8.5;
        existing.ChainedPuzzle.Add(new ChainedPuzzleItem { PuzzleType = 22 });

        var vm = new AlarmsViewModel(new[] { existing });
        Assert.True(vm.HasExistingAlarms);

        vm.OpenCreateAlarmDialog();
        Assert.True(vm.IsCreatingAlarm);
        Assert.True(vm.IsCreatingFresh);

        vm.IsCreatingFromExisting = true;
        Assert.True(vm.IsCreatingFromExisting);
        Assert.False(vm.IsCreatingFresh);
        Assert.False(vm.IsCreatingFromVanilla);

        Assert.Equal(existing, vm.SelectedExistingAlarm);
        Assert.Equal("My_Existing_Alarm_Copy", vm.NewAlarmName);
        Assert.Equal("Class VI Alarm", vm.NewAlarmPublicAlarmName);

        vm.NewAlarmPersistentId = 202;
        vm.NewAlarmName = "Duplicated Alarm Custom";

        bool created = vm.ConfirmCreateAlarm();
        Assert.True(created);
        Assert.False(vm.IsCreatingAlarm);
        Assert.Equal(2, vm.Alarms.Count);

        var duplicated = vm.SelectedAlarm;
        Assert.NotNull(duplicated);
        Assert.Equal((uint)202, duplicated.PersistentId);
        Assert.Equal("Duplicated Alarm Custom", duplicated.Name);
        Assert.Equal("Class VI Alarm", duplicated.PublicAlarmName);
        Assert.Equal(8.5, duplicated.PauseBeforeStart);
        Assert.Single(duplicated.ChainedPuzzle);
        Assert.Equal((uint)22, duplicated.ChainedPuzzle[0].PuzzleType);
    }

    [Fact]
    public void AlarmsViewModel_OpenDuplicateAlarmDialog_PreselectsExistingAlarm()
    {
        var existing = AlarmService.CreateFreshAlarm(300, "Base Alarm");
        existing.PublicAlarmName = "Class I Alarm";

        var vm = new AlarmsViewModel(new[] { existing });
        vm.OpenDuplicateAlarmDialog(existing);

        Assert.True(vm.IsCreatingAlarm);
        Assert.True(vm.IsCreatingFromExisting);
        Assert.False(vm.IsCreatingFresh);
        Assert.Equal(existing, vm.SelectedExistingAlarm);
        Assert.Equal("Base Alarm_Copy", vm.NewAlarmName);
        Assert.Equal("Class I Alarm", vm.NewAlarmPublicAlarmName);
    }

    [Fact]
    public void MainWindowViewModel_ContainsAlarmsViewModel()
    {
        var mainVm = new MainWindowViewModel();
        Assert.NotNull(mainVm.Alarms);
        Assert.NotNull(mainVm.Sounds);
        Assert.NotNull(mainVm.LevelLayout);
    }

    [Fact]
    public void AlarmsViewModel_ChainedPuzzle_AddAndRemoveItems()
    {
        var alarm = AlarmService.CreateFreshAlarm(1, "Test Alarm");
        alarm.ChainedPuzzle.Clear();
        var types = new List<ChainedPuzzleTypeEntry>
        {
            new() { PersistentId = 6, Name = "SecurityScan_Big_RequireAll" },
            new() { PersistentId = 22, Name = "SecurityScan_Big_Moving" }
        };
        var vm = new AlarmsViewModel(new[] { alarm }, chainedPuzzleTypes: types);

        Assert.Equal(alarm, vm.SelectedAlarm);
        Assert.NotNull(vm.SelectedAlarm);
        Assert.Empty(vm.SelectedAlarm.ChainedPuzzle);

        vm.AddChainedPuzzleItem();
        Assert.Single(vm.SelectedAlarm.ChainedPuzzle);
        Assert.Equal((uint)6, vm.SelectedAlarm.ChainedPuzzle[0].PuzzleType);

        vm.AddChainedPuzzleItem();
        Assert.Equal(2, vm.SelectedAlarm.ChainedPuzzle.Count);

        var itemToRemove = vm.SelectedAlarm.ChainedPuzzle[0];
        vm.RemoveChainedPuzzleItem(itemToRemove);
        Assert.Single(vm.SelectedAlarm.ChainedPuzzle);

        Assert.NotNull(vm.RemoveChainedPuzzleItemCommand);
        Assert.NotNull(vm.AddChainedPuzzleItemCommand);

        // Test command execution
        var secondItemToRemove = vm.SelectedAlarm.ChainedPuzzle[0];
        vm.RemoveChainedPuzzleItemCommand.Execute(secondItemToRemove);
        Assert.Empty(vm.SelectedAlarm.ChainedPuzzle);
    }

    [Fact]
    public void AlarmsViewModel_DefaultConstructor_LoadsChainedPuzzleTypesFromDataBlock()
    {
        var vm = new AlarmsViewModel();
        Assert.NotEmpty(vm.ChainedPuzzleTypes);

        // Verify key vanilla chained puzzle types from ChainedPuzzleTypeDataBlock.json are present
        Assert.Contains(vm.ChainedPuzzleTypes, t => t.PersistentId == 2);
        Assert.Contains(vm.ChainedPuzzleTypes, t => t.PersistentId == 6);
        Assert.Contains(vm.ChainedPuzzleTypes, t => t.PersistentId == 22);
        Assert.Contains(vm.ChainedPuzzleTypes, t => t.PersistentId == 56);
    }

    [Fact]
    public void AlarmBlock_SpawnsEnemies_CanBeToggled()
    {
        var alarm = AlarmService.CreateFreshAlarm(1, "Test Alarm");
        Assert.True(alarm.SpawnsEnemies);

        alarm.SpawnsEnemies = false;
        Assert.False(alarm.SpawnsEnemies);

        var clone = alarm.Clone();
        Assert.False(clone.SpawnsEnemies);
    }

    [Fact]
    public void AlarmBlock_DistanceSettings_GetSetAndClone()
    {
        var alarm = new AlarmBlock
        {
            WantedDistanceFromStartPos = 12.5,
            WantedDistanceBetweenPuzzleComponents = 24.0
        };

        Assert.Equal(12.5, alarm.WantedDistanceFromStartPos);
        Assert.Equal(24.0, alarm.WantedDistanceBetweenPuzzleComponents);

        var clone = alarm.Clone();
        Assert.Equal(12.5, clone.WantedDistanceFromStartPos);
        Assert.Equal(24.0, clone.WantedDistanceBetweenPuzzleComponents);
    }

    [Fact]
    public void AlarmBlock_DistanceSettings_ProjectSerializationRoundtrip()
    {
        string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gtfo_alarm_test_{System.Guid.NewGuid():N}.rundown");
        try
        {
            var mainVm = new MainWindowViewModel();
            var customAlarm = AlarmService.CreateFreshAlarm(777, "Custom Distance Alarm");
            customAlarm.WantedDistanceFromStartPos = 15.5;
            customAlarm.WantedDistanceBetweenPuzzleComponents = 30.25;

            mainVm.Alarms.Alarms.Add(customAlarm);

            RundownProjectService.SaveProject(mainVm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var loadedVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(loadedVm, loadedProject);

            var loadedAlarm = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 777);
            Assert.NotNull(loadedAlarm);
            Assert.Equal("Custom Distance Alarm", loadedAlarm.Name);
            Assert.Equal(15.5, loadedAlarm.WantedDistanceFromStartPos);
            Assert.Equal(30.25, loadedAlarm.WantedDistanceBetweenPuzzleComponents);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExport_ExportsAlarmDistanceSettings()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gtfo_alarm_export_{System.Guid.NewGuid():N}");
        try
        {
            var mainVm = new MainWindowViewModel();
            var customAlarm = AlarmService.CreateFreshAlarm(99999, "Export Distance Alarm");
            customAlarm.WantedDistanceFromStartPos = 7.5;
            customAlarm.WantedDistanceBetweenPuzzleComponents = 14.25;

            mainVm.Alarms.Alarms.Clear();
            mainVm.Alarms.Alarms.Add(customAlarm);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string chainedPuzzlePath = System.IO.Path.Combine(tempDir, "GameData_ChainedPuzzleDataBlock_bin.json");
            Assert.True(System.IO.File.Exists(chainedPuzzlePath));

            var jsonDoc = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(chainedPuzzlePath))?.AsObject();
            Assert.NotNull(jsonDoc);

            var blocks = jsonDoc["Blocks"]?.AsArray();
            Assert.NotNull(blocks);

            var exportedBlock = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 99999)?.AsObject();
            Assert.NotNull(exportedBlock);
            Assert.Equal(7.5, exportedBlock["WantedDistanceFromStartPos"]?.GetValue<double>());
            Assert.Equal(14.25, exportedBlock["WantedDistanceBetweenPuzzleComponents"]?.GetValue<double>());
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
    [Fact]
    public void AlarmBlock_IsErrorAndDisableSurvivalWaveOnComplete_PropertyLinkAndClone()
    {
        var alarm = new AlarmBlock();
        Assert.False(alarm.IsError);
        Assert.True(alarm.DisableSurvivalWaveOnComplete);

        alarm.IsError = true;
        Assert.True(alarm.IsError);
        Assert.False(alarm.DisableSurvivalWaveOnComplete);

        alarm.DisableSurvivalWaveOnComplete = true;
        Assert.False(alarm.IsError);
        Assert.True(alarm.DisableSurvivalWaveOnComplete);

        alarm.IsError = true;
        var clone = alarm.Clone();
        Assert.True(clone.IsError);
        Assert.False(clone.DisableSurvivalWaveOnComplete);
    }

    [Fact]
    public void AlarmBlock_UseRandomPositionsAndTriggerAlarm_GetSetAndClone()
    {
        var alarm = new AlarmBlock();
        Assert.False(alarm.UseRandomPositions);
        Assert.True(alarm.SpawnsEnemies);
        Assert.True(alarm.TriggerAlarmOnActivate);

        alarm.UseRandomPositions = true;
        alarm.SpawnsEnemies = false;
        Assert.True(alarm.UseRandomPositions);
        Assert.False(alarm.SpawnsEnemies);
        Assert.False(alarm.TriggerAlarmOnActivate);

        alarm.TriggerAlarmOnActivate = true;
        Assert.True(alarm.SpawnsEnemies);
        Assert.True(alarm.TriggerAlarmOnActivate);

        var clone = alarm.Clone();
        Assert.True(clone.UseRandomPositions);
        Assert.True(clone.SpawnsEnemies);
        Assert.True(clone.TriggerAlarmOnActivate);
    }

    [Fact]
    public void AlarmService_LoadVanillaAlarms_ParsesIsErrorAndUseRandomPositions()
    {
        var alarms = AlarmService.LoadVanillaAlarms();
        Assert.NotNull(alarms);

        // ChainedPuzzle 627: R2E1 ZONE_602 (Main) has DisableSurvivalWaveOnComplete=false -> IsError=true
        var errorAlarm = alarms.FirstOrDefault(a => a.PersistentId == 627);
        Assert.NotNull(errorAlarm);
        Assert.True(errorAlarm.IsError);
        Assert.False(errorAlarm.DisableSurvivalWaveOnComplete);
        Assert.True(errorAlarm.SpawnsEnemies);

        // ChainedPuzzle 545: R1C2 ZONE_3 (Main) has UseRandomPositions=true, DisableSurvivalWaveOnComplete=true -> IsError=false
        var randomPosAlarm = alarms.FirstOrDefault(a => a.PersistentId == 545);
        Assert.NotNull(randomPosAlarm);
        Assert.True(randomPosAlarm.UseRandomPositions);
        Assert.False(randomPosAlarm.IsError);
        Assert.True(randomPosAlarm.DisableSurvivalWaveOnComplete);
    }

    [Fact]
    public void AlarmBlock_NewSettings_ProjectSerializationRoundtrip()
    {
        string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gtfo_alarm_test_{System.Guid.NewGuid():N}.rundown");
        try
        {
            var mainVm = new MainWindowViewModel();
            var customAlarm = AlarmService.CreateFreshAlarm(8888, "Custom Advanced Alarm");
            customAlarm.IsError = true;
            customAlarm.UseRandomPositions = true;
            customAlarm.SpawnsEnemies = false;

            mainVm.Alarms.Alarms.Add(customAlarm);

            RundownProjectService.SaveProject(mainVm, tempFile);

            var loadedProject = RundownProjectService.LoadProject(tempFile);
            var loadedVm = new MainWindowViewModel();
            RundownProjectService.ApplyProject(loadedVm, loadedProject);

            var loadedAlarm = loadedVm.Alarms.Alarms.FirstOrDefault(a => a.PersistentId == 8888);
            Assert.NotNull(loadedAlarm);
            Assert.Equal("Custom Advanced Alarm", loadedAlarm.Name);
            Assert.True(loadedAlarm.IsError);
            Assert.False(loadedAlarm.DisableSurvivalWaveOnComplete);
            Assert.True(loadedAlarm.UseRandomPositions);
            Assert.False(loadedAlarm.SpawnsEnemies);
            Assert.False(loadedAlarm.TriggerAlarmOnActivate);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }

    [Fact]
    public void DatablockExport_ExportsIsErrorAndUseRandomPositions()
    {
        string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gtfo_alarm_export_{System.Guid.NewGuid():N}");
        try
        {
            var mainVm = new MainWindowViewModel();
            var errorAlarm = AlarmService.CreateFreshAlarm(70001, "Export Error Alarm");
            errorAlarm.IsError = true;
            errorAlarm.UseRandomPositions = true;
            errorAlarm.SpawnsEnemies = true;

            var normalAlarm = AlarmService.CreateFreshAlarm(70002, "Export Normal Alarm");
            normalAlarm.IsError = false;
            normalAlarm.UseRandomPositions = false;
            normalAlarm.SpawnsEnemies = false;

            mainVm.Alarms.Alarms.Clear();
            mainVm.Alarms.Alarms.Add(errorAlarm);
            mainVm.Alarms.Alarms.Add(normalAlarm);

            var result = DatablockExportService.ExportAll(mainVm, tempDir);
            Assert.True(result.Success, result.Message);

            string chainedPuzzlePath = System.IO.Path.Combine(tempDir, "GameData_ChainedPuzzleDataBlock_bin.json");
            Assert.True(System.IO.File.Exists(chainedPuzzlePath));

            var jsonDoc = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(chainedPuzzlePath))?.AsObject();
            Assert.NotNull(jsonDoc);

            var blocks = jsonDoc["Blocks"]?.AsArray();
            Assert.NotNull(blocks);

            var expErrorBlock = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 70001)?.AsObject();
            Assert.NotNull(expErrorBlock);
            Assert.False(expErrorBlock["DisableSurvivalWaveOnComplete"]?.GetValue<bool>());
            Assert.True(expErrorBlock["UseRandomPositions"]?.GetValue<bool>());
            Assert.True(expErrorBlock["TriggerAlarmOnActivate"]?.GetValue<bool>());

            var expNormalBlock = blocks.FirstOrDefault(b => b?["persistentID"]?.GetValue<uint>() == 70002)?.AsObject();
            Assert.NotNull(expNormalBlock);
            Assert.True(expNormalBlock["DisableSurvivalWaveOnComplete"]?.GetValue<bool>());
            Assert.False(expNormalBlock["UseRandomPositions"]?.GetValue<bool>());
            Assert.False(expNormalBlock["TriggerAlarmOnActivate"]?.GetValue<bool>());
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
