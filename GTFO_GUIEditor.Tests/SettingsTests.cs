using System;
using System.IO;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Media;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class SettingsTests : IDisposable
{
    private readonly string _tempSettingsPath;

    public SettingsTests()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"gtfo_settings_test_{Guid.NewGuid():N}.json");
        EditorSettingsManager.CustomSettingsConfigPath = _tempSettingsPath;
        EditorSettingsManager.Instance.ReloadSettings();
    }

    public void Dispose()
    {
        EditorSettingsManager.CustomSettingsConfigPath = null;
        EditorSettingsManager.Instance.Settings.Appearance.ResetToDefaults();
        EditorSettingsManager.Instance.Settings.General.ResetToDefaults();
        EditorSettingsManager.Instance.ReloadSettings();
        if (File.Exists(_tempSettingsPath))
        {
            try
            {
                File.Delete(_tempSettingsPath);
            }
            catch
            {
                // Ignore cleanup error
            }
        }
    }

    [Fact]
    public void GeneralSettings_HasExpectedDefaults()
    {
        var settings = new GeneralSettings();

        Assert.Equal(StartupProjectMode.PreviousOpenedProject, settings.StartupProject);
        Assert.Equal(DataFlowMode.RundownFile, settings.DataFlow);
        Assert.True(settings.IsPreviousOpenedProjectStartup);
        Assert.True(settings.IsRundownDataFlow);
        Assert.Null(settings.StartupProjectPath);
    }

    [Fact]
    public void GeneralSettings_DataFlowChangeClearsStartupProjectPath()
    {
        var settings = new GeneralSettings
        {
            StartupProject = StartupProjectMode.SetProject,
            StartupProjectPath = "/tmp/startup-project"
        };

        settings.DataFlow = DataFlowMode.Datablocks;

        Assert.Null(settings.StartupProjectPath);

        settings.StartupProjectPath = "/tmp/startup-folder";
        settings.DataFlow = DataFlowMode.RundownFile;

        Assert.Null(settings.StartupProjectPath);
    }

    [Fact]
    public void GeneralSettings_AutosaveDefaultsToTwoMinutes()
    {
        var settings = new GeneralSettings();

        Assert.Equal(2, settings.AutosaveIntervalMinutes);
    }

    [Fact]
    public void SwitchingDataFlow_PreservesDirtyStateAndUsesMatchingQueue()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);

        vm.Settings.General.DataFlow = DataFlowMode.RundownFile;
        vm.MarkProjectEdited();
        Assert.True(vm.IsDirty);

        vm.Settings.General.DataFlow = DataFlowMode.Datablocks;
        Assert.True(vm.IsDirty);
        Assert.NotEmpty(vm.EditedDatablockFiles);

        vm.Settings.General.DataFlow = DataFlowMode.RundownFile;
        Assert.True(vm.IsDirty);
        Assert.Empty(vm.EditedDatablockFiles);
    }

    [Fact]
    public void DatablockSavedQueue_IsAuthoritativeForUnsavedChanges()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        vm.Settings.General.DataFlow = DataFlowMode.Datablocks;
        DatablockSaveQueue.Instance.Clear();

        vm.MarkProjectEdited();
        Assert.True(vm.HasUnsavedChanges);

        DatablockSaveQueue.Instance.Clear();
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public void DatablockQueue_EditDuringSave_RemainsUnsaved()
    {
        DatablockSaveQueue.Instance.Clear();
        DatablockSaveQueue.Instance.MarkDirty(DatablockSaveQueue.AllDatablockFiles);
        var snapshot = DatablockSaveQueue.Instance.TakeRevisionSnapshot();

        DatablockSaveQueue.Instance.MarkDirty(DatablockSaveQueue.AllDatablockFiles);
        foreach (var item in snapshot)
        {
            DatablockSaveQueue.Instance.MarkSaved(item.Key, item.Value);
        }

        Assert.NotEmpty(DatablockSaveQueue.Instance.Files);
        Assert.True(DatablockSaveQueue.Instance.Count > 0);
    }

    [Fact]
    public void OpeningRundown_ClearsOutstandingEdits()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"open_clean_test_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            vm.MarkProjectEdited();
            Assert.True(vm.HasUnsavedChanges);
            Assert.Contains("Unsaved changes", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);

            RundownProjectService.SaveProject(vm, filePath);
            vm.OpenFileDirectAsync(filePath).GetAwaiter().GetResult();

            Assert.False(vm.IsDirty);
            Assert.Empty(vm.EditedDatablockFiles);
            Assert.False(vm.HasUnsavedChanges);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void ReopeningDatablocksOnStartup_DoesNotLeaveDatablocksQueuedForSave()
    {
        string datablockPath = Path.Combine(Path.GetTempPath(), $"reopen_datablocks_test_{Guid.NewGuid():N}");
        try
        {
            var sourceVm = new MainWindowViewModel(autoReopenLastRundown: false);
            var exportResult = DatablockManager.Instance.ExportDatablocks(sourceVm, datablockPath);
            Assert.True(exportResult.Success, exportResult.Message);

            sourceVm.Settings.General.DataFlow = DataFlowMode.Datablocks;
            sourceVm.Settings.General.LastOpenedDatablockPath = datablockPath;
            EditorSettingsManager.Instance.SaveSettings();
            DatablockSaveQueue.Instance.Clear();

            var reopenedVm = new MainWindowViewModel(autoReopenLastRundown: true);

            Assert.Equal(DataFlowMode.Datablocks, reopenedVm.Settings.General.DataFlow);
            Assert.Equal(datablockPath, reopenedVm.CurrentDatablocksPath);
            Assert.StartsWith("Reopened datablocks:", reopenedVm.StatusMessage, StringComparison.Ordinal);
            Assert.False(reopenedVm.IsDirty);
            Assert.False(reopenedVm.HasUnsavedChanges, reopenedVm.StatusMessage);
            Assert.Empty(reopenedVm.EditedDatablockFiles);
        }
        finally
        {
            if (Directory.Exists(datablockPath))
            {
                Directory.Delete(datablockPath, true);
            }
        }
    }

    [Fact]
    public void ReopeningDatablocksInMainWindow_DoesNotShowUnsavedChanges()
    {
        string datablockPath = Path.Combine(Path.GetTempPath(), $"reopen_datablocks_window_test_{Guid.NewGuid():N}");
        try
        {
            var sourceVm = new MainWindowViewModel(autoReopenLastRundown: false);
            var exportResult = DatablockManager.Instance.ExportDatablocks(sourceVm, datablockPath);
            Assert.True(exportResult.Success, exportResult.Message);

            sourceVm.Settings.General.DataFlow = DataFlowMode.Datablocks;
            sourceVm.Settings.General.LastOpenedDatablockPath = datablockPath;
            EditorSettingsManager.Instance.SaveSettings();
            DatablockSaveQueue.Instance.Clear();

            if (Application.Current == null)
            {
                AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
            }

            var window = new MainWindow();
            window.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();

            var reopenedVm = Assert.IsType<MainWindowViewModel>(window.DataContext);
            Assert.False(reopenedVm.StatusMessage.Contains("unsaved changes", StringComparison.OrdinalIgnoreCase));
            Assert.False(reopenedVm.HasUnsavedChanges);
            Assert.Empty(reopenedVm.EditedDatablockFiles);
        }
        finally
        {
            if (Directory.Exists(datablockPath))
            {
                Directory.Delete(datablockPath, true);
            }
        }
    }

    [Fact]
    public void LevelLayoutViewState_DoesNotMarkProjectEdited()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);

        vm.LevelLayout.GridState.Pan(25, -10);
        vm.LevelLayout.GridState.Zoom = 2.5;
        vm.LevelLayout.GridState.ViewportWidth = 1000;
        vm.LevelLayout.GridState.ViewportHeight = 700;
        vm.LevelLayout.GridState.HoveredRoom = null;

        Assert.False(vm.IsDirty);
        Assert.Empty(vm.EditedDatablockFiles);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task ExpandingNonFirstAlarm_DoesNotMarkProjectEdited_ButFieldChangesDo()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"expand_alarm_clean_test_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            vm.Alarms.Alarms.Add(new AlarmBlock { Name = "First alarm" });
            vm.Alarms.Alarms.Add(new AlarmBlock { Name = "Second alarm" });

            vm.CurrentFilePath = filePath;
            await vm.SaveRundownAsync();
            Assert.False(vm.IsDirty);

            await vm.OpenFileDirectAsync(filePath);
            Assert.False(vm.IsDirty);
            Assert.False(vm.HasUnsavedChanges);

            AlarmBlock secondAlarm = vm.Alarms.Alarms[1];
            vm.Alarms.ToggleAlarmExpanded(secondAlarm);

            Assert.False(vm.IsDirty);
            Assert.False(vm.HasUnsavedChanges);

            secondAlarm.PublicAlarmName = "Changed name";

            Assert.True(vm.IsDirty);
            Assert.True(vm.HasUnsavedChanges);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task HoveringLevelLayoutRoom_DoesNotMarkProjectEdited()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"hover_room_clean_test_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            var level = vm.LevelLayout.SelectedLevel!;
            var room = new PlacedRoomNode();
            level.GridState.PlacedRooms.Add(room);

            vm.CurrentFilePath = filePath;
            await vm.SaveRundownAsync();
            Assert.False(vm.IsDirty);

            level.GridState.HoveredRoom = room;

            Assert.True(room.IsHovered);
            Assert.False(vm.IsDirty);
            Assert.False(vm.HasUnsavedChanges);

            level.GridState.HoveredRoom = null;
            Assert.False(room.IsHovered);
            Assert.False(vm.IsDirty);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void NavigationSearchAndRoomDialog_DontMarkProjectEdited()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);

        vm.OpenGeneralSettings();
        vm.SelectedSettingsTabIndex = 1;
        vm.CloseSettings();
        vm.Enemies.PopulationSearchText = "striker";
        vm.Enemies.GroupSearchText = "group";
        vm.Geomorphs.SearchText = "mining";
        vm.Geomorphs.SelectedComplexTabIndex = 1;
        vm.Sounds.SearchText = "event";
        if (vm.Documentation.DisplayTabs.Count > 1)
        {
            vm.Documentation.SelectedSection = vm.Documentation.DisplayTabs[1];
        }
        vm.Rundown.OpenRundownSettings();
        vm.Rundown.CloseRundownSettings();

        var room = new PlacedRoomNode { Zone = 7 };
        vm.LevelLayout.OnRoomClicked(room);
        Assert.True(vm.LevelLayout.IsEditingRoom);
        Assert.NotNull(vm.LevelLayout.ActiveZoneSettings);
        vm.LevelLayout.CloseEditRoomDialog();

        Assert.False(vm.IsDirty);
        Assert.False(vm.HasUnsavedChanges);

        vm.LevelLayout.OnRoomClicked(room);
        vm.LevelLayout.EditingZoneLighting = 12;
        Assert.True(vm.IsDirty);
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task OpeningAndCancellingRoomExpansion_DoesNotDirtyButGeneratingRoomDoes()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"door_dialog_clean_test_{Guid.NewGuid():N}.rundown");
        try
        {
            var vm = new MainWindowViewModel(autoReopenLastRundown: false);
            vm.LevelLayout.GridState.Clear();
            vm.LevelLayout.GridState.InitializeStartingTile(
                "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab",
                0,
                0);

            vm.CurrentFilePath = filePath;
            await vm.SaveRundownAsync();
            Assert.False(vm.IsDirty);

            var door = vm.LevelLayout.GridState.PlacedDoors.First(d => d.IsInternal && !d.IsConnected);
            vm.LevelLayout.OnDoorClicked(door);

            Assert.True(vm.LevelLayout.IsExpandingRoom);
            Assert.False(vm.IsDirty);
            Assert.False(vm.HasUnsavedChanges);

            vm.LevelLayout.CancelRoomExpansion();
            Assert.False(vm.IsDirty);

            vm.LevelLayout.OnDoorClicked(door);
            Assert.True(vm.LevelLayout.ConfirmRoomExpansion());
            Assert.True(vm.IsDirty);
            Assert.True(vm.HasUnsavedChanges);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public void CloseSettings_FallsBackToPreviousProjectWhenSetProjectHasNoPath()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        vm.Settings.General.StartupProject = StartupProjectMode.SetProject;
        vm.Settings.General.StartupProjectPath = null;

        vm.CloseSettings();

        Assert.Equal(StartupProjectMode.PreviousOpenedProject, vm.Settings.General.StartupProject);
        Assert.False(vm.IsShowingSettings);
    }

    [Fact]
    public void AppearanceSettings_HasCorrectDefaultColors()
    {
        var settings = new AppearanceSettings();

        Assert.Equal("#FFFF00", settings.UnexpandedDoorColor);
        Assert.Equal("#9D00FF", settings.ConnectedDoorColor);
        Assert.Equal("#FF0000", settings.SecurityDoorColor);
        Assert.Equal("#CCCCCC", settings.MainDoorColor);
        Assert.Equal("#FFA200", settings.SecondaryDoorColor);
        Assert.Equal("#00F2FF", settings.OverloadDoorColor);
        Assert.Equal("#757575", settings.BlockedDoorColor);
    }

    [Fact]
    public void AppearanceSettings_ColorProperties_SyncWithHexStrings()
    {
        var settings = new AppearanceSettings();

        settings.UnexpandedColor = Color.FromRgb(10, 20, 30);
        Assert.Equal("#0A141E", settings.UnexpandedDoorColor);

        settings.ConnectedColor = Color.FromRgb(255, 0, 128);
        Assert.Equal("#FF0080", settings.ConnectedDoorColor);

        settings.SecurityDoorColor = "#112233";
        Assert.Equal(Color.FromRgb(17, 34, 51), settings.SecurityColor);

        settings.MainDoorColor = "#445566";
        Assert.Equal(Color.FromRgb(68, 85, 102), settings.MainColor);

        settings.SecondaryDoorColor = "#778899";
        Assert.Equal(Color.FromRgb(119, 136, 153), settings.SecondaryColor);

        settings.OverloadDoorColor = "#AABBCC";
        Assert.Equal(Color.FromRgb(170, 187, 204), settings.OverloadColor);

        settings.BlockedDoorColor = "#DDEEFF";
        Assert.Equal(Color.FromRgb(221, 238, 255), settings.BlockedColor);
    }

    [Fact]
    public void AppearanceSettings_ResetToDefaults_RestoresOriginalColors()
    {
        var settings = new AppearanceSettings
        {
            UnexpandedDoorColor = "#000000",
            ConnectedDoorColor = "#111111",
            SecurityDoorColor = "#222222",
            MainDoorColor = "#333333",
            SecondaryDoorColor = "#444444",
            OverloadDoorColor = "#555555",
            BlockedDoorColor = "#666666"
        };

        settings.ResetToDefaults();

        Assert.Equal(AppearanceSettings.DefaultUnexpandedDoorColor, settings.UnexpandedDoorColor);
        Assert.Equal(AppearanceSettings.DefaultConnectedDoorColor, settings.ConnectedDoorColor);
        Assert.Equal(AppearanceSettings.DefaultSecurityDoorColor, settings.SecurityDoorColor);
        Assert.Equal(AppearanceSettings.DefaultMainDoorColor, settings.MainDoorColor);
        Assert.Equal(AppearanceSettings.DefaultSecondaryDoorColor, settings.SecondaryDoorColor);
        Assert.Equal(AppearanceSettings.DefaultOverloadDoorColor, settings.OverloadDoorColor);
        Assert.Equal(AppearanceSettings.DefaultBlockedDoorColor, settings.BlockedDoorColor);
    }

    [Fact]
    public void EditorSettingsManager_PersistsSettingsBetweenLoads()
    {
        var manager = EditorSettingsManager.Instance;
        manager.Settings.Appearance.UnexpandedDoorColor = "#123456";
        manager.Settings.Appearance.ConnectedDoorColor = "#654321";
        manager.SaveSettings();

        Assert.True(File.Exists(_tempSettingsPath));

        // Reload fresh from disk
        manager.ReloadSettings();
        Assert.Equal("#123456", manager.Settings.Appearance.UnexpandedDoorColor);
        Assert.Equal("#654321", manager.Settings.Appearance.ConnectedDoorColor);
    }

    [Fact]
    public void PlacedDoorNode_DoorBrush_ReflectsAppearanceSettings()
    {
        var appearance = EditorSettingsManager.Instance.Settings.Appearance;
        appearance.ResetToDefaults();

        var door = new PlacedDoorNode();

        // Default Unexpanded
        Assert.Equal("#FFFF00", door.DoorBrush);

        // Connected
        door.IsConnected = true;
        Assert.Equal("#9D00FF", door.DoorBrush);

        // Security
        door.IsSecurityDoor = true;
        Assert.Equal("#FF0000", door.DoorBrush);

        // Main
        door.IsMainDoor = true;
        Assert.Equal("#CCCCCC", door.DoorBrush);

        // Secondary
        door.IsSecondaryDoor = true;
        Assert.Equal("#FFA200", door.DoorBrush);

        // Overload
        door.IsOverloadDoor = true;
        Assert.Equal("#00F2FF", door.DoorBrush);

        // Blocked
        door.IsBlocked = true;
        Assert.Equal("#757575", door.DoorBrush);

        // Modify settings
        appearance.BlockedDoorColor = "#101010";
        appearance.OverloadDoorColor = "#202020";
        appearance.SecondaryDoorColor = "#303030";
        appearance.MainDoorColor = "#404040";
        appearance.SecurityDoorColor = "#505050";
        appearance.ConnectedDoorColor = "#606060";
        appearance.UnexpandedDoorColor = "#707070";

        door.IsBlocked = true;
        Assert.Equal("#101010", door.DoorBrush);

        door.IsBlocked = false;
        door.IsOverloadDoor = true;
        Assert.Equal("#202020", door.DoorBrush);

        door.IsSecondaryDoor = true;
        Assert.Equal("#303030", door.DoorBrush);

        door.IsMainDoor = true;
        Assert.Equal("#404040", door.DoorBrush);

        door.IsSecurityDoor = true;
        Assert.Equal("#505050", door.DoorBrush);

        door.DoorType = DoorType.RoomDoor;
        door.IsConnected = true;
        Assert.Equal("#606060", door.DoorBrush);

        door.IsConnected = false;
        Assert.Equal("#707070", door.DoorBrush);
    }

    [Fact]
    public void MainWindowViewModel_SettingsDialogCommands_WorkCorrectly()
    {
        var vm = new MainWindowViewModel();

        Assert.False(vm.IsShowingSettings);

        vm.OpenGeneralSettings();
        Assert.True(vm.IsShowingSettings);
        Assert.Equal(0, vm.SelectedSettingsTabIndex);

        vm.OpenAppearanceSettings();
        Assert.True(vm.IsShowingSettings);
        Assert.Equal(1, vm.SelectedSettingsTabIndex);

        vm.Settings.Appearance.UnexpandedDoorColor = "#111111";
        vm.ResetSettingsToDefaults(); // resets tab 1 (Appearance)
        Assert.Equal("#FFFF00", vm.Settings.Appearance.UnexpandedDoorColor);

        vm.SelectedSettingsTabIndex = 0;
        vm.Settings.General.DataFlow = DataFlowMode.Datablocks;
        vm.ResetSettingsToDefaults(); // resets tab 0 (General)
        Assert.Equal(DataFlowMode.RundownFile, vm.Settings.General.DataFlow);

        vm.CloseSettings();
        Assert.False(vm.IsShowingSettings);
    }

    [Fact]
    public void LevelLayoutGridState_DoorLegendProperties_WorkWithCustomColors()
    {
        var grid = new LevelLayoutGridState();
        var appearance = EditorSettingsManager.Instance.Settings.Appearance;
        appearance.ResetToDefaults();

        var door = new PlacedDoorNode { IsConnected = false, IsBlocked = false };
        grid.PlacedDoors.Add(door);

        Assert.True(grid.HasUnexpandedDoors);
        Assert.False(grid.HasConnectedDoors);

        // Customize unexpanded color
        appearance.UnexpandedDoorColor = "#123456";
        grid.RefreshDoorBrushes();

        Assert.Equal("#123456", door.DoorBrush);
        Assert.True(grid.HasUnexpandedDoors);

        door.IsConnected = true;
        Assert.False(grid.HasUnexpandedDoors);
        Assert.True(grid.HasConnectedDoors);

        door.IsSecurityDoor = true;
        Assert.False(grid.HasConnectedDoors);
        Assert.True(grid.HasSecurityDoors);

        door.IsMainDoor = true;
        Assert.False(grid.HasSecurityDoors);
        Assert.True(grid.HasMainDoors);

        door.IsSecondaryDoor = true;
        Assert.False(grid.HasMainDoors);
        Assert.True(grid.HasSecondaryDoors);

        door.IsOverloadDoor = true;
        Assert.False(grid.HasSecondaryDoors);
        Assert.True(grid.HasOverloadDoors);

        door.IsBlocked = true;
        Assert.False(grid.HasOverloadDoors);
        Assert.True(grid.HasBlockedDoors);
    }

    [Fact]
    public void LevelLayoutViewModel_AppearanceUpdates_TriggerDoorBrushRefresh()
    {
        var layoutVm = new LevelLayoutViewModel();
        var level = EditorDefaultsService.CreateDefaultLevel(1);
        layoutVm.Levels.Add(level);
        layoutVm.SelectedLevel = level;

        var door = new PlacedDoorNode { IsConnected = false };
        level.GridState.PlacedDoors.Add(door);

        Assert.Equal("#FFFF00", door.DoorBrush);

        layoutVm.Appearance.UnexpandedDoorColor = "#AABBCC";
        Assert.Equal("#AABBCC", door.DoorBrush);
    }

    [Fact]
    public void GradientKeyframe_Properties_SyncAndClampCorrectly()
    {
        var kf = new GradientKeyframe(-0.5, "FF0000");
        Assert.Equal(0.0, kf.Position);
        Assert.Equal("#FF0000", kf.ColorHex);
        Assert.Equal(Color.FromRgb(255, 0, 0), kf.Color);

        kf.Position = 1.5;
        Assert.Equal(1.0, kf.Position);

        kf.Position = 0.42;
        Assert.Equal(0.42, kf.Position);

        kf.Color = Color.FromRgb(0, 128, 255);
        Assert.Equal("#0080FF", kf.ColorHex);

        kf.ColorHex = "#123456";
        Assert.Equal(Color.FromRgb(18, 52, 86), kf.Color);
    }

    [Fact]
    public void AppearanceSettings_DefaultHeightmapGradient_HasExpectedKeyframes()
    {
        var settings = new AppearanceSettings();

        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.Equal(0.0, settings.HeightmapGradient[0].Position);
        Assert.Equal("#182230", settings.HeightmapGradient[0].ColorHex);
        Assert.Equal(1.0, settings.HeightmapGradient[1].Position);
        Assert.Equal("#82A5CD", settings.HeightmapGradient[1].ColorHex);
        Assert.False(settings.CanRemoveKeyframes);
    }

    [Fact]
    public void AppearanceSettings_EvaluateHeightmapColor_InterpolatesLinearGradient()
    {
        var settings = new AppearanceSettings();

        // At t = 0.0 -> #182230 (R:24, G:34, B:48)
        var c0 = settings.EvaluateHeightmapColor(0.0);
        Assert.Equal(210, c0.A);
        Assert.Equal(24, c0.R);
        Assert.Equal(34, c0.G);
        Assert.Equal(48, c0.B);

        // At t = 1.0 -> #82A5CD (R:130, G:165, B:205)
        var c1 = settings.EvaluateHeightmapColor(1.0);
        Assert.Equal(210, c1.A);
        Assert.Equal(130, c1.R);
        Assert.Equal(165, c1.G);
        Assert.Equal(205, c1.B);

        // At t = 0.5 -> midpoint
        var cMid = settings.EvaluateHeightmapColor(0.5);
        Assert.Equal(210, cMid.A);
        Assert.Equal(77, cMid.R); // 24 + 0.5 * (130 - 24) = 77
        Assert.Equal(100, cMid.G); // 34 + 0.5 * (165 - 34) = 99.5 -> 100
        Assert.Equal(126, cMid.B); // 48 + 0.5 * (205 - 48) = 126.5 -> 126 (round to even)

        // Clamping below 0 and above 1
        var cBelow = settings.EvaluateHeightmapColor(-0.5);
        Assert.Equal(c0, cBelow);

        var cAbove = settings.EvaluateHeightmapColor(1.5);
        Assert.Equal(c1, cAbove);
    }

    [Fact]
    public void AppearanceSettings_AddAndRemoveKeyframes_UpdatesCollectionAndCanRemove()
    {
        var settings = new AppearanceSettings();
        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.False(settings.CanRemoveKeyframes);

        // Add 3rd keyframe -> placed at midpoint (0.5)
        settings.AddKeyframe();
        Assert.Equal(3, settings.HeightmapGradient.Count);
        Assert.True(settings.CanRemoveKeyframes);

        var middleKf = settings.HeightmapGradient[2];
        Assert.Equal(0.5, middleKf.Position);

        // Add 4th keyframe -> placed in largest gap
        settings.AddKeyframe();
        Assert.Equal(4, settings.HeightmapGradient.Count);

        // Remove keyframe
        settings.RemoveKeyframe(middleKf);
        Assert.Equal(3, settings.HeightmapGradient.Count);
        Assert.DoesNotContain(middleKf, settings.HeightmapGradient);

        // Removing down to 2 is allowed
        var anotherKf = settings.HeightmapGradient[2];
        settings.RemoveKeyframe(anotherKf);
        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.False(settings.CanRemoveKeyframes);

        // Attempting to remove below 2 is ignored
        settings.RemoveKeyframe(settings.HeightmapGradient[0]);
        Assert.Equal(2, settings.HeightmapGradient.Count);
    }

    [Fact]
    public void AppearanceSettings_ResetToDefaults_RestoresHeightmapGradient()
    {
        var settings = new AppearanceSettings();
        settings.AddKeyframe();
        settings.HeightmapGradient[0].ColorHex = "#FF0000";

        Assert.Equal(3, settings.HeightmapGradient.Count);
        Assert.Equal("#FF0000", settings.HeightmapGradient[0].ColorHex);

        settings.ResetToDefaults();

        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.Equal("#182230", settings.HeightmapGradient[0].ColorHex);
        Assert.Equal(0.0, settings.HeightmapGradient[0].Position);
        Assert.Equal("#82A5CD", settings.HeightmapGradient[1].ColorHex);
        Assert.Equal(1.0, settings.HeightmapGradient[1].Position);
    }

    [Fact]
    public void EditorSettingsManager_PersistsHeightmapGradientBetweenLoads()
    {
        var manager = EditorSettingsManager.Instance;
        manager.Settings.Appearance.HeightmapGradient.Clear();
        manager.Settings.Appearance.HeightmapGradient.Add(new GradientKeyframe(0.0, "#000000"));
        manager.Settings.Appearance.HeightmapGradient.Add(new GradientKeyframe(0.5, "#FF00FF"));
        manager.Settings.Appearance.HeightmapGradient.Add(new GradientKeyframe(1.0, "#FFFFFF"));
        manager.SaveSettings();

        manager.ReloadSettings();

        Assert.Equal(3, manager.Settings.Appearance.HeightmapGradient.Count);
        Assert.Equal("#000000", manager.Settings.Appearance.HeightmapGradient[0].ColorHex);
        Assert.Equal(0.0, manager.Settings.Appearance.HeightmapGradient[0].Position);
        Assert.Equal("#FF00FF", manager.Settings.Appearance.HeightmapGradient[1].ColorHex);
        Assert.Equal(0.5, manager.Settings.Appearance.HeightmapGradient[1].Position);
        Assert.Equal("#FFFFFF", manager.Settings.Appearance.HeightmapGradient[2].ColorHex);
        Assert.Equal(1.0, manager.Settings.Appearance.HeightmapGradient[2].Position);
    }

    [Fact]
    public void RoomSvgService_GetColorForHeight_UsesConfiguredGradient()
    {
        var appearance = EditorSettingsManager.Instance.Settings.Appearance;
        appearance.ResetToDefaults();

        // Total height 0 (center) with default offset 6 -> t = 0.5
        var cMid = RoomSvgService.GetColorForHeight(0.0, 6.0);
        Assert.Equal(77, cMid.R);
        Assert.Equal(100, cMid.G);
        Assert.Equal(126, cMid.B);

        // Customize keyframes
        appearance.HeightmapGradient[0].ColorHex = "#FF0000"; // Red at low
        appearance.HeightmapGradient[1].ColorHex = "#0000FF"; // Blue at high

        // Total height at lowest: -6 * 1.5 = -9 -> t = 0 -> Red
        var cLow = RoomSvgService.GetColorForHeight(-9.0, 6.0);
        Assert.Equal(255, cLow.R);
        Assert.Equal(0, cLow.G);
        Assert.Equal(0, cLow.B);

        // Total height at highest: +6 * 1.5 = +9 -> t = 1 -> Blue
        var cHigh = RoomSvgService.GetColorForHeight(9.0, 6.0);
        Assert.Equal(0, cHigh.R);
        Assert.Equal(0, cHigh.G);
        Assert.Equal(255, cHigh.B);
    }

    [Fact]
    public void MainWindowViewModel_HeightmapGradientCommands_AddAndRemove()
    {
        var vm = new MainWindowViewModel();
        vm.Settings.Appearance.ResetToDefaults();

        Assert.Equal(2, vm.Settings.Appearance.HeightmapGradient.Count);

        vm.AddHeightmapGradientKeyframe();
        Assert.Equal(3, vm.Settings.Appearance.HeightmapGradient.Count);

        var added = vm.Settings.Appearance.HeightmapGradient[2];
        vm.RemoveHeightmapGradientKeyframe(added);
        Assert.Equal(2, vm.Settings.Appearance.HeightmapGradient.Count);
    }

    [Fact]
    public void AppearanceSettings_IndividualDoorColorResets_RestoresSpecificColors()
    {
        var settings = new AppearanceSettings();

        // Alter all colors
        settings.UnexpandedDoorColor = "#111111";
        settings.ConnectedDoorColor = "#222222";
        settings.SecurityDoorColor = "#333333";
        settings.MainDoorColor = "#444444";
        settings.SecondaryDoorColor = "#555555";
        settings.OverloadDoorColor = "#666666";
        settings.BlockedDoorColor = "#777777";

        // Reset Unexpanded only
        settings.ResetUnexpandedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultUnexpandedDoorColor, settings.UnexpandedDoorColor);
        Assert.Equal("#222222", settings.ConnectedDoorColor);

        // Reset Connected only
        settings.ResetConnectedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultConnectedDoorColor, settings.ConnectedDoorColor);
        Assert.Equal("#333333", settings.SecurityDoorColor);

        // Reset Security only
        settings.ResetSecurityDoorColor();
        Assert.Equal(AppearanceSettings.DefaultSecurityDoorColor, settings.SecurityDoorColor);

        // Reset Main only
        settings.ResetMainDoorColor();
        Assert.Equal(AppearanceSettings.DefaultMainDoorColor, settings.MainDoorColor);

        // Reset Secondary only
        settings.ResetSecondaryDoorColor();
        Assert.Equal(AppearanceSettings.DefaultSecondaryDoorColor, settings.SecondaryDoorColor);

        // Reset Overload only
        settings.ResetOverloadDoorColor();
        Assert.Equal(AppearanceSettings.DefaultOverloadDoorColor, settings.OverloadDoorColor);

        // Reset Blocked only
        settings.ResetBlockedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultBlockedDoorColor, settings.BlockedDoorColor);
    }

    [Fact]
    public void MainWindowViewModel_IndividualDoorColorResets_CallThroughToAppearance()
    {
        var vm = new MainWindowViewModel();
        var app = vm.Settings.Appearance;

        app.UnexpandedDoorColor = "#111111";
        app.ConnectedDoorColor = "#222222";
        app.SecurityDoorColor = "#333333";
        app.MainDoorColor = "#444444";
        app.SecondaryDoorColor = "#555555";
        app.OverloadDoorColor = "#666666";
        app.BlockedDoorColor = "#777777";

        vm.ResetUnexpandedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultUnexpandedDoorColor, app.UnexpandedDoorColor);

        vm.ResetConnectedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultConnectedDoorColor, app.ConnectedDoorColor);

        vm.ResetSecurityDoorColor();
        Assert.Equal(AppearanceSettings.DefaultSecurityDoorColor, app.SecurityDoorColor);

        vm.ResetMainDoorColor();
        Assert.Equal(AppearanceSettings.DefaultMainDoorColor, app.MainDoorColor);

        vm.ResetSecondaryDoorColor();
        Assert.Equal(AppearanceSettings.DefaultSecondaryDoorColor, app.SecondaryDoorColor);

        vm.ResetOverloadDoorColor();
        Assert.Equal(AppearanceSettings.DefaultOverloadDoorColor, app.OverloadDoorColor);

        vm.ResetBlockedDoorColor();
        Assert.Equal(AppearanceSettings.DefaultBlockedDoorColor, app.BlockedDoorColor);
    }

    [Fact]
    public void AppearanceSettings_SelectedKeyframe_TracksSelectionAndUpdatesOnAddRemove()
    {
        var settings = new AppearanceSettings();
        settings.ResetToDefaults();

        Assert.NotNull(settings.SelectedKeyframe);
        Assert.Equal(settings.HeightmapGradient[0], settings.SelectedKeyframe);
        Assert.True(settings.HasSelectedKeyframe);
        Assert.True(settings.HeightmapGradient[0].IsSelected);
        Assert.False(settings.HeightmapGradient[1].IsSelected);

        // Change selected keyframe
        settings.SelectedKeyframe = settings.HeightmapGradient[1];
        Assert.False(settings.HeightmapGradient[0].IsSelected);
        Assert.True(settings.HeightmapGradient[1].IsSelected);

        // Add keyframe selects newly added keyframe
        settings.AddKeyframe();
        Assert.Equal(3, settings.HeightmapGradient.Count);
        var added = settings.HeightmapGradient[2];
        Assert.Equal(added, settings.SelectedKeyframe);
        Assert.True(added.IsSelected);

        // Remove selected keyframe selects first remaining keyframe
        settings.RemoveSelectedKeyframe();
        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.NotNull(settings.SelectedKeyframe);
        Assert.Contains(settings.SelectedKeyframe, settings.HeightmapGradient);
    }

    [Fact]
    public void AppearanceSettings_ResetHeightmapGradient_RestoresDefaultKeyframes()
    {
        var settings = new AppearanceSettings();
        settings.AddKeyframe();
        settings.AddKeyframe();
        Assert.Equal(4, settings.HeightmapGradient.Count);

        settings.HeightmapGradient[0].ColorHex = "#FF00FF";
        settings.HeightmapGradient[0].Position = 0.25;

        // Changing a door color should remain unaffected when resetting gradient
        settings.UnexpandedDoorColor = "#123456";

        settings.ResetHeightmapGradient();

        Assert.Equal(2, settings.HeightmapGradient.Count);
        Assert.Equal(0.0, settings.HeightmapGradient[0].Position);
        Assert.Equal(AppearanceSettings.DefaultHeightmapLowColor, settings.HeightmapGradient[0].ColorHex);
        Assert.Equal(1.0, settings.HeightmapGradient[1].Position);
        Assert.Equal(AppearanceSettings.DefaultHeightmapHighColor, settings.HeightmapGradient[1].ColorHex);
        Assert.Equal("#123456", settings.UnexpandedDoorColor);
    }

    [Fact]
    public void MainWindowViewModel_ResetHeightmapGradient_DelegatesToAppearance()
    {
        var vm = new MainWindowViewModel();
        vm.Settings.Appearance.ResetToDefaults();
        vm.AddHeightmapGradientKeyframe();
        Assert.Equal(3, vm.Settings.Appearance.HeightmapGradient.Count);

        vm.ResetHeightmapGradient();
        Assert.Equal(2, vm.Settings.Appearance.HeightmapGradient.Count);
        Assert.Equal(0.0, vm.Settings.Appearance.HeightmapGradient[0].Position);
        Assert.Equal(AppearanceSettings.DefaultHeightmapLowColor, vm.Settings.Appearance.HeightmapGradient[0].ColorHex);
        Assert.Equal(1.0, vm.Settings.Appearance.HeightmapGradient[1].Position);
        Assert.Equal(AppearanceSettings.DefaultHeightmapHighColor, vm.Settings.Appearance.HeightmapGradient[1].ColorHex);
    }

    [Fact]
    public void MainWindowViewModel_RemoveSelectedHeightmapGradientKeyframe_RemovesSelectedKeyframe()
    {
        var vm = new MainWindowViewModel();
        vm.Settings.Appearance.ResetToDefaults();
        vm.AddHeightmapGradientKeyframe();
        Assert.Equal(3, vm.Settings.Appearance.HeightmapGradient.Count);

        var selected = vm.Settings.Appearance.SelectedKeyframe;
        Assert.NotNull(selected);

        vm.RemoveSelectedHeightmapGradientKeyframe();
        Assert.Equal(2, vm.Settings.Appearance.HeightmapGradient.Count);
        Assert.DoesNotContain(selected, vm.Settings.Appearance.HeightmapGradient);
    }
}
