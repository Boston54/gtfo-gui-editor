using System.IO;
using System.Linq;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class EditorDefaultsTests
{
    [Fact]
    public void EditorDefaultsService_LoadsFromDataDirectory_Successfully()
    {
        EditorDefaultsService.ResetCache();
        var defaults = EditorDefaultsService.GetDefaults();

        Assert.NotNull(defaults);
        Assert.NotNull(defaults.LevelLayoutWindow);
        Assert.NotNull(defaults.AlarmsWindow);
        Assert.NotNull(defaults.LightingWindow);
        Assert.NotNull(defaults.FogSettingsWindow);
        Assert.NotNull(defaults.RundownWindow);

        // Level settings defaults
        Assert.Equal("My New Level", defaults.LevelLayoutWindow.LevelSettingsDialog.Name);
        Assert.Equal(ComplexType.Mining, defaults.LevelLayoutWindow.LevelSettingsDialog.ComplexType);

        // Security door settings defaults
        Assert.Equal(ProgressionPuzzleType.None, defaults.LevelLayoutWindow.SecurityDoorSettings.ProgressionPuzzleToEnter);
        Assert.Equal(1, defaults.LevelLayoutWindow.SecurityDoorSettings.ProgressionPuzzlePlacementCount);

        // Alarms defaults
        Assert.Equal("Class II Alarm", defaults.AlarmsWindow.WaveSettingsAndTiming.PublicAlarmName);
        Assert.Equal((uint)21, defaults.AlarmsWindow.WaveRoles.WaveRoleWeakling);
        Assert.Equal((uint)13, defaults.AlarmsWindow.WaveRoles.WaveRoleStandard);

        // Fog defaults
        Assert.Equal(0.02, defaults.FogSettingsWindow.FogBlockDefaults.FogDensity);

        // Rundown defaults
        Assert.Equal("New Rundown", defaults.RundownWindow.RundownSettings.Name);
        Assert.True(defaults.RundownWindow.RundownSettings.UseTierUnlockRequirements);
    }

    [Fact]
    public void CreateDefaultLevel_AppliesEditorDefaults()
    {
        var level = EditorDefaultsService.CreateDefaultLevel(42);

        Assert.Equal((uint)42, level.PersistentId);
        Assert.Equal("Level 42", level.Name);
        Assert.Equal(ComplexType.Mining, level.ComplexType);
        Assert.Equal((uint)0, level.FogSettings);
        Assert.NotEmpty(level.ElevatorTile);
    }

    [Fact]
    public void CreateDefaultAlarmBlock_AppliesEditorDefaults()
    {
        var alarm = EditorDefaultsService.CreateDefaultAlarmBlock(101, "MyAlarm");

        Assert.Equal((uint)101, alarm.PersistentId);
        Assert.Equal("MyAlarm", alarm.Name);
        Assert.Equal("Class II Alarm", alarm.PublicAlarmName);
        Assert.Equal((uint)21, alarm.WaveRoleWeakling);
        Assert.Equal((uint)13, alarm.WaveRoleStandard);
        Assert.Equal(3.0, alarm.PauseBeforeStart);
        Assert.Equal(5.0, alarm.PauseBetweenGroups);
    }

    [Fact]
    public void CreateDefaultLightBlock_AppliesEditorDefaults()
    {
        var light = EditorDefaultsService.CreateDefaultLightBlock(202, "MyLighting");

        Assert.Equal((uint)202, light.PersistentId);
        Assert.Equal("MyLighting", light.Name);
        Assert.NotEmpty(light.LightCategorySettings);
        var gen = light.LightCategorySettings.FirstOrDefault(c => c.Category == LightCategory.General);
        Assert.NotNull(gen);
        Assert.Equal(0.5, gen.Chance);
    }

    [Fact]
    public void CreateDefaultFogBlock_AppliesEditorDefaults()
    {
        var fog = EditorDefaultsService.CreateDefaultFogBlock(303, "MyFog");

        Assert.Equal((uint)303, fog.PersistentId);
        Assert.Equal("MyFog", fog.Name);
        Assert.Equal(0.02, fog.FogDensity);
        Assert.NotNull(fog.FogColor);
        Assert.Equal(0.2, fog.FogColor.R);
    }

    [Fact]
    public void CreateDefaultExpedition_AppliesEditorDefaults()
    {
        var exp = EditorDefaultsService.CreateDefaultExpedition("B", 2, 5);

        Assert.Equal((uint)5, exp.LevelLayoutData);
        Assert.Equal("B", exp.Descriptive.Prefix);
        Assert.Equal("Level B2", exp.Descriptive.PublicName);
        Assert.Equal(ExpeditionAccessibility.Normal, exp.Accessibility);
        Assert.False(exp.SecondaryLayerEnabled);
        Assert.False(exp.ThirdLayerEnabled);
    }

    [Fact]
    public void DatablockExport_SynthesizesComplexResourceSetsFromPlacedGeomorphs()
    {
        var mainVm = new MainWindowViewModel();

        // Add a custom level with placed geomorphs
        var level = EditorDefaultsService.CreateDefaultLevel(10, "ComplexExportLevel");
        var geoPrefab = "Assets/AssetPrefabs/Complex/Mining/Geomorphs/Digsite/geo_64x64_mining_dig_site_AS_01.prefab";
        var geoNode = level.GridState.CreateGeomorphNode(
            prefabPath: geoPrefab,
            gridX: 0,
            gridY: 0,
            initialAreaIndex: 0,
            rotationAngle: 0.0,
            altitude: GeomorphAltitude.Mid,
            lighting: 1);
        level.GridState.PlacedGeomorphs.Add(geoNode);

        mainVm.LevelLayout.Levels.Clear();
        mainVm.LevelLayout.Levels.Add(level);
        mainVm.LevelLayout.SelectedLevel = level;

        string exportDir = Path.Combine(Path.GetTempPath(), $"gtfo_export_complex_test_{Path.GetRandomFileName()}");
        try
        {
            var result = DatablockExportService.ExportAll(mainVm, exportDir);
            Assert.True(result.Success, result.Message);

            string complexJsonPath = Path.Combine(exportDir, "GameData_ComplexResourceSetDataBlock_bin.json");
            Assert.True(File.Exists(complexJsonPath));

            string complexJson = File.ReadAllText(complexJsonPath);
            Assert.Contains("Generated_ComplexSet_ComplexExportLevel_Main", complexJson);
            Assert.Contains(geoPrefab, complexJson);
        }
        finally
        {
            if (Directory.Exists(exportDir))
            {
                Directory.Delete(exportDir, true);
            }
        }
    }
}
