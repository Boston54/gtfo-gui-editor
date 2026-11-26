using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class DatablockManagerTests
{
    [Fact]
    public void DatablockManager_Instance_IsNotNullAndImplementsInterface()
    {
        IDatablockManager manager = DatablockManager.Instance;
        Assert.NotNull(manager);
    }

    [Fact]
    public void ExportDatablocks_ExportsAllStandardMTFOFiles()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        string tempDir = Path.Combine(Path.GetTempPath(), $"datablock_manager_test_{Guid.NewGuid():N}");

        try
        {
            var manager = DatablockManager.Instance;
            var result = manager.ExportDatablocks(vm, tempDir);

            Assert.True(result.Success, result.Message);
            Assert.True(result.DatablocksExportedCount > 0);
            Assert.True(result.LevelGenFilesExportedCount > 0);
            Assert.Equal(tempDir, result.OutputDirectory);

            // Verify core datablock output files exist
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_LevelLayoutDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_WardenObjectiveDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_ChainedPuzzleDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_ComplexResourceSetDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_LightSettingsDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_FogSettingsDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_ConsumableDistributionDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_SurvivalWaveSettingsDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_SurvivalWavePopulationDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_EnemyDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_EnemyGroupDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_GameSetupDataBlock_bin.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_TextDataBlock_bin.json")));

            // Verify LGTuner config; schemas are intentionally not exported.
            string lgTunerDir = Path.Combine(tempDir, "Custom", "LGTunerAdvanced");
            Assert.True(Directory.Exists(lgTunerDir));
            Assert.False(Directory.Exists(Path.Combine(tempDir, ".schemas")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ExportDatablocks_WhenRootIsUnderPlugins_PlacesCustomAtPluginRoot()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        string tempRoot = Path.Combine(Path.GetTempPath(), $"datablock_plugins_test_{Guid.NewGuid():N}");
        string exportDir = Path.Combine(tempRoot, "plugins", "folder2");

        try
        {
            var result = DatablockManager.Instance.ExportDatablocks(vm, exportDir);

            Assert.True(result.Success, result.Message);
            Assert.True(File.Exists(Path.Combine(exportDir, "GameData_SurvivalWavePopulationDataBlock_bin.json")));
            Assert.True(Directory.Exists(Path.Combine(tempRoot, "plugins", "Custom", "LGTunerAdvanced")));
            Assert.False(Directory.Exists(Path.Combine(exportDir, "Custom")));
            Assert.False(Directory.Exists(Path.Combine(tempRoot, ".schemas")));

            var importedVm = new MainWindowViewModel(autoReopenLastRundown: false);
            var importResult = DatablockManager.Instance.ImportDatablocks(importedVm, exportDir);
            Assert.True(importResult.Success, importResult.Message);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task ExportDatablocksAsync_RunsAsynchronouslyAndProducesValidOutput()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        string tempDir = Path.Combine(Path.GetTempPath(), $"datablock_manager_async_test_{Guid.NewGuid():N}");

        try
        {
            var manager = DatablockManager.Instance;
            var result = await manager.ExportDatablocksAsync(vm, tempDir);

            Assert.True(result.Success, result.Message);
            Assert.True(File.Exists(Path.Combine(tempDir, "GameData_RundownDataBlock_bin.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ExportDatablocks_ReturnsFailure_WhenOutputDirectoryIsEmpty()
    {
        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        var manager = DatablockManager.Instance;

        var result = manager.ExportDatablocks(vm, "");
        Assert.False(result.Success);
        Assert.Contains("cannot be empty", result.Message);
    }

    private class CustomTestExporter : IDatablockExporter
    {
        public bool HasRun { get; private set; }

        public void Export(ExportContext context)
        {
            HasRun = true;
            string customFilePath = Path.Combine(context.ExportRootPath, "Custom_Plugin_Export.json");
            var customObj = new JsonObject
            {
                ["PluginName"] = "TestModularPlugin",
                ["Success"] = true
            };
            File.WriteAllText(customFilePath, customObj.ToJsonString());
        }
    }

    [Fact]
    public void DatablockManager_AllowsRegisteringCustomExporters_ForExtensiblePipelines()
    {
        var manager = new DatablockManager();
        var customExporter = new CustomTestExporter();
        manager.RegisterExporter(customExporter);

        var vm = new MainWindowViewModel(autoReopenLastRundown: false);
        string tempDir = Path.Combine(Path.GetTempPath(), $"custom_exporter_test_{Guid.NewGuid():N}");

        try
        {
            var result = manager.ExportDatablocks(vm, tempDir);
            Assert.True(result.Success);
            Assert.True(customExporter.HasRun);
            Assert.True(File.Exists(Path.Combine(tempDir, "Custom_Plugin_Export.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void DoorGraphAnalyzer_NormalizesZoneAndLayerStrings()
    {
        Assert.Equal("Zone_0", DoorGraphAnalyzer.NormalizeZoneString(null));
        Assert.Equal("Zone_0", DoorGraphAnalyzer.NormalizeZoneString(""));
        Assert.Equal("Zone_5", DoorGraphAnalyzer.NormalizeZoneString("5"));
        Assert.Equal("Zone_10", DoorGraphAnalyzer.NormalizeZoneString("Zone_10"));

        Assert.Equal("MainLayer", DoorGraphAnalyzer.NormalizeLayerTypeString(null));
        Assert.Equal("MainLayer", DoorGraphAnalyzer.NormalizeLayerTypeString("Main"));
        Assert.Equal("SecondaryLayer", DoorGraphAnalyzer.NormalizeLayerTypeString("Secondary"));
        Assert.Equal("ThirdLayer", DoorGraphAnalyzer.NormalizeLayerTypeString("Overload"));
        Assert.Equal("ThirdLayer", DoorGraphAnalyzer.NormalizeLayerTypeString("Third"));
    }
}
